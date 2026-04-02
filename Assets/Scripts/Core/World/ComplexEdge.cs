// =============================================================================
// ComplexEdge.cs
// Namespace: Arcontio.Core
// Patch: 0.02.09.A
// =============================================================================
//
// MOTIVAZIONE
// ─────────────────────────────────────────────────────────────────────────────
// Il sistema landmark originale (Day3) apprende solo edge "semplici":
// connessioni tra due nodi con un singolo valore CostCells, valide solo se
// esistono nel LandmarkRegistry globale (edge del bootstrap Day2).
//
// Questo lascia scoperto il caso più interessante: due nodi non sono
// connessi nel registry globale, ma l'NPC ha fisicamente percorso un
// corridoio che li collega. Quel corridoio esiste, l'NPC lo sa, ma
// non può usarlo nel planning perché non ha un edge.
//
// ComplexEdge risolve questo: quando un NPC percorre un tratto tra due
// nodi landmark (anche non adiacenti nel registry), registra il percorso
// compresso in segmenti direzione/lunghezza. L'edge vive solo nella memoria
// soggettiva dell'NPC (NpcComplexEdgeMemory, separata da NpcLandmarkMemory).
//
// STRUTTURA DI UN COMPLEX EDGE
// ─────────────────────────────────────────────────────────────────────────────
// EdgeKey (A, B):
//   identificatore non orientato — (min(A,B), max(A,B)) — uguale a NpcLandmarkMemory.
//
// Segments: List<PathSegment>
//   Il percorso compresso in segmenti direzione/lunghezza.
//   Esempio: [East:5, South:2, East:3] invece di 10 coordinate.
//   Compattato con run-length encoding cardinale durante la registrazione.
//
// BaseCost: int
//   Somma delle lunghezze dei segmenti (celle totali). Usato dall'A* come
//   costo dell'edge (analogo a CostCells negli edge semplici).
//
// Confidence: float [0,1]
//   Quanto l'NPC "si fida" di questo percorso. Inizia a 0.5 alla prima
//   registrazione, si avvicina a 1.0 con le conferme successive.
//   Decresce con il tempo se non viene rinforzato (TickMaintenance).
//
// LastSeenTick: long
//   Tick dell'ultima volta che questo edge è stato percorso o confermato.
//
// Flags: ComplexEdgeFlags
//   Proprietà del tratto: RequiresDoorOpen, NarrowCorridor, Risky.
//   Il rilevamento automatico è previsto per una patch successiva.
//   Per ora possono essere settati da sistemi esterni (es. dopo un fallimento
//   di navigazione, si marca Risky).
//
// RECORDING
// ─────────────────────────────────────────────────────────────────────────────
// Il recording avviene in NpcLandmarkMemory tramite tre operazioni:
//   StartPathRecording(fromNodeId, fromX, fromY)
//     → apre un buffer di posizioni, salvando il nodo di partenza.
//   RecordStep(toX, toY)
//     → aggiunge ogni passo al buffer mentre l'NPC cammina.
//   TryCompletePathRecording(toNodeId, toX, toY, nowTick, out ComplexEdge)
//     → quando l'NPC raggiunge un nodo landmark, chiude il buffer,
//        comprime in segmenti e produce il ComplexEdge.
//
// Il recording viene avviato da World.NotifyNpcMovedForLandmarkLearning
// ogni volta che l'NPC raggiunge un nodo landmark.
// =============================================================================

using System;
using System.Collections.Generic;

namespace Arcontio.Core
{
    // =========================================================================
    // FLAGS DI PROPRIETÀ DEL TRATTO
    // =========================================================================

    /// <summary>
    /// Flags che descrivono le proprietà di un ComplexEdge.
    ///
    /// <para>
    /// Questi flag vengono impostati da sistemi esterni (es. dopo un fallimento
    /// di navigazione) o da future logiche di rilevamento automatico.
    /// Non vengono impostati automaticamente durante il recording.
    /// </para>
    /// </summary>
    [Flags]
    public enum ComplexEdgeFlags : byte
    {
        /// <summary>Nessuna proprietà speciale.</summary>
        None            = 0,

        /// <summary>
        /// Il tratto attraversa una porta che deve essere aperta.
        /// Rilevamento futuro: se durante il playback l'NPC deve aprire una porta,
        /// il sistema setta questo flag automaticamente.
        /// </summary>
        RequiresDoorOpen = 1 << 0,

        /// <summary>
        /// Il corridoio è stretto (larghezza 1 cella).
        /// Rilevamento futuro: durante il recording, se tutti i passi sono
        /// su un unico asse senza alternative cardinali libere.
        /// </summary>
        NarrowCorridor  = 1 << 1,

        /// <summary>
        /// Il tratto è marcato come rischioso (es. l'NPC è stato attaccato
        /// mentre lo percorreva, o ha fallito più volte la navigazione).
        /// Settato esternamente dalle Rule o dal sistema di failure learning.
        /// </summary>
        Risky           = 1 << 2,
    }

    // =========================================================================
    // SEGMENTO DIREZIONE/LUNGHEZZA
    // =========================================================================

    /// <summary>
    /// Un segmento di percorso: direzione cardinale + numero di passi consecutivi
    /// nella stessa direzione.
    ///
    /// <para>
    /// Esempio: {Direction=East, Length=5} significa "5 passi verso Est".
    /// </para>
    ///
    /// <para>
    /// La sequenza di PathSegment rappresenta il percorso compresso con
    /// run-length encoding cardinale: passi consecutivi nella stessa direzione
    /// vengono fusi in un unico segmento.
    /// </para>
    ///
    /// <para>
    /// Proprietà fondamentale per la comunicazione NPC→NPC futura:
    /// la descrizione è relativa alle direzioni cardinali, non alle coordinate
    /// assolute della mappa. Un NPC può raccontare "vai Est 5, poi Sud 2"
    /// senza dover condividere coordinate assolute.
    /// </para>
    /// </summary>
    [Serializable]
    public struct PathSegment
    {
        /// <summary>Direzione cardinale del segmento.</summary>
        public CardinalDirection Direction;

        /// <summary>
        /// Numero di celle consecutive in questa direzione (minimo 1).
        /// </summary>
        public int Length;

        public PathSegment(CardinalDirection dir, int len)
        {
            Direction = dir;
            Length    = len;
        }

        public override string ToString() => $"{Direction}:{Length}";
    }

    // =========================================================================
    // COMPLEX EDGE
    // =========================================================================

    /// <summary>
    /// <b>ComplexEdge</b> — edge soggettivo con percorso compresso, imparato
    /// dall'NPC camminando tra due nodi landmark non adiacenti nel registry globale.
    ///
    /// <para>
    /// Vive esclusivamente nella memoria soggettiva dell'NPC
    /// (<see cref="NpcComplexEdgeMemory"/>), non nel <see cref="LandmarkRegistry"/>.
    /// </para>
    ///
    /// <para><b>Patch:</b> 0.02.09.A</para>
    /// </summary>
    [Serializable]
    public sealed class ComplexEdge
    {
        // ── IDENTITÀ ──────────────────────────────────────────────────────────

        /// <summary>
        /// Identificatore non orientato dell'edge.
        /// Sempre (min(A,B), max(A,B)) per coerenza con NpcLandmarkMemory.EdgeKey.
        /// </summary>
        public readonly NpcLandmarkMemory.EdgeKey Key;

        // ── PERCORSO ──────────────────────────────────────────────────────────

        /// <summary>
        /// Il percorso compresso in segmenti direzione/lunghezza.
        ///
        /// <para>
        /// Prodotto da <see cref="NpcComplexEdgeMemory.CompressPathToSegments"/>
        /// con run-length encoding cardinale: passi consecutivi nella stessa
        /// direzione vengono fusi in un unico <see cref="PathSegment"/>.
        /// </para>
        ///
        /// <para>
        /// Questo è anche il formato per la comunicazione futura NPC→NPC:
        /// la sequenza di segmenti può essere serializzata in un SymbolicToken.
        /// </para>
        /// </summary>
        public readonly List<PathSegment> Segments;

        /// <summary>
        /// Costo base in celle (somma delle lunghezze dei segmenti).
        /// Usato dall'A* come costo dell'edge — equivalente a CostCells negli
        /// edge semplici di <see cref="NpcLandmarkMemory"/>.
        /// </summary>
        public int BaseCost;

        // ── METADATI ──────────────────────────────────────────────────────────

        /// <summary>
        /// Confidenza dell'NPC in questo edge [0, 1].
        ///
        /// <para>
        /// Inizia a 0.5 alla prima registrazione (incertezza moderata).
        /// Si avvicina a 1.0 ad ogni conferma (stesso edge percorso di nuovo).
        /// Decresce lentamente nel tempo se non viene rinforzato.
        /// </para>
        ///
        /// <para>
        /// Nella formula di costo A*: un edge con confidence bassa riceve
        /// una penalità analoga agli edge semplici di NpcLandmarkMemory
        /// (reliabilityPenalty = (1 - confidence) * 2).
        /// </para>
        /// </summary>
        public float Confidence;

        /// <summary>
        /// Tick dell'ultima percorrenza o conferma di questo edge.
        /// Usato da TickMaintenance per il decadimento della confidence
        /// e per l'eviction se l'edge non viene usato per troppo tempo.
        /// </summary>
        public long LastSeenTick;

        // ── PROPRIETÀ DEL TRATTO ──────────────────────────────────────────────

        /// <summary>
        /// Proprietà descrittive del tratto.
        /// Settabili da sistemi esterni (Rule, failure learning).
        /// </summary>
        public ComplexEdgeFlags Flags;

        // ── COSTRUTTORE ───────────────────────────────────────────────────────

        /// <summary>
        /// Crea un nuovo ComplexEdge dal percorso compresso.
        /// </summary>
        public ComplexEdge(
            NpcLandmarkMemory.EdgeKey key,
            List<PathSegment> segments,
            long nowTick)
        {
            Key         = key;
            Segments    = segments ?? new List<PathSegment>();
            LastSeenTick = nowTick;
            Confidence  = 0.5f;   // prima registrazione: incertezza moderata
            Flags       = ComplexEdgeFlags.None;

            // Calcola BaseCost come somma delle lunghezze
            BaseCost = 0;
            for (int i = 0; i < Segments.Count; i++)
                BaseCost += Segments[i].Length;
        }

        // ── METODI ────────────────────────────────────────────────────────────

        /// <summary>
        /// Rafforza la confidence dell'edge dopo una percorrenza confermata.
        ///
        /// <para>
        /// Formula: confidence += (1 - confidence) * 0.3f
        /// (avvicinamento asintotico a 1.0, mai raggiunto esattamente).
        /// </para>
        /// </summary>
        public void Reinforce(long nowTick)
        {
            Confidence   = Confidence + (1f - Confidence) * 0.3f;
            LastSeenTick = nowTick;
        }

        /// <summary>
        /// Aggiorna LastSeenTick e aggiorna il percorso se il nuovo path
        /// è significativamente diverso (BaseCost molto migliore).
        ///
        /// <para>
        /// Politica conservativa: aggiorna i segmenti solo se il nuovo
        /// percorso è più corto di almeno 2 celle (evita instabilità).
        /// </para>
        /// </summary>
        public void UpdateIfBetter(List<PathSegment> newSegments, int newCost, long nowTick)
        {
            if (newCost < BaseCost - 2)
            {
                // Percorso migliorato: aggiorna segmenti e costo
                Segments.Clear();
                Segments.AddRange(newSegments);
                BaseCost = newCost;
            }
            Reinforce(nowTick);
        }

        /// <summary>
        /// Imposta il flag indicato.
        /// </summary>
        public void SetFlag(ComplexEdgeFlags flag)   => Flags |= flag;

        /// <summary>
        /// Rimuove il flag indicato.
        /// </summary>
        public void ClearFlag(ComplexEdgeFlags flag) => Flags &= ~flag;

        /// <summary>True se il flag indicato è attivo.</summary>
        public bool HasFlag(ComplexEdgeFlags flag)   => (Flags & flag) != 0;
    }

    // =========================================================================
    // RECORDING STATE (temporaneo durante la camminata)
    // =========================================================================

    /// <summary>
    /// Stato temporaneo del recording di un percorso tra landmark.
    ///
    /// <para>
    /// Viene creato quando l'NPC calpesta un nodo landmark (StartPathRecording)
    /// e distrutto (con produzione di un ComplexEdge) quando raggiunge il
    /// nodo successivo (TryCompletePathRecording).
    /// </para>
    ///
    /// <para>
    /// Se l'NPC viene interrotto (intent cancellato, stuck, morte) prima
    /// di raggiungere il nodo successivo, il recording viene scartato.
    /// </para>
    /// </summary>
    internal sealed class PathRecordingState
    {
        /// <summary>Nodo di partenza del recording.</summary>
        public readonly int FromNodeId;

        /// <summary>
        /// Posizioni accumulate passo per passo (inclusa la cella di partenza).
        /// Lista di (X, Y) come int packed: (X << 16) | (Y & 0xFFFF).
        /// </summary>
        public readonly List<int> Steps;

        /// <summary>
        /// Budget massimo di passi prima di scartare il recording.
        /// Evita che un recording molto lungo (NPC che vaga a lungo) produca
        /// un edge inutilizzabile per l'A* (costo troppo alto).
        /// </summary>
        public readonly int MaxSteps;

        /// <summary>True se il buffer ha superato MaxSteps e il recording è invalido.</summary>
        public bool Overflowed;

        public PathRecordingState(int fromNodeId, int startX, int startY, int maxSteps)
        {
            FromNodeId = fromNodeId;
            MaxSteps   = maxSteps;
            Steps      = new List<int>(maxSteps + 4);
            Overflowed = false;
            // Includi la cella di partenza come primo step
            Steps.Add(Pack(startX, startY));
        }

        /// <summary>Aggiunge un passo al buffer. Se supera MaxSteps, marca come overflow.</summary>
        public void AddStep(int x, int y)
        {
            if (Overflowed) return;
            if (Steps.Count >= MaxSteps)
            {
                Overflowed = true;
                return;
            }
            Steps.Add(Pack(x, y));
        }

        /// <summary>Impacchetta (X, Y) in un int per efficienza.</summary>
        public static int Pack(int x, int y) => (x << 16) | (y & 0xFFFF);

        /// <summary>Estrae X da un int packed.</summary>
        public static int UnpackX(int packed) => packed >> 16;

        /// <summary>Estrae Y da un int packed (con estensione del segno).</summary>
        public static int UnpackY(int packed) => (short)(packed & 0xFFFF);
    }
}
