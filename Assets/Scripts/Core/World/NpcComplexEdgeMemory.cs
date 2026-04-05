// =============================================================================
// NpcComplexEdgeMemory.cs
// Namespace: Arcontio.Core
// Patch: 0.02.09.A
// =============================================================================
//
// RESPONSABILITÀ
// ─────────────────────────────────────────────────────────────────────────────
// Store per-NPC degli edge complessi (ComplexEdge) imparati dalla navigazione.
// Separato da NpcLandmarkMemory per tenere distinti i due tipi di edge:
//   - NpcLandmarkMemory.EdgeEntry: edge semplici (CostCells, esisti nel registry)
//   - NpcComplexEdgeMemory: edge complessi (Segments, non nel registry)
//
// GESTIONE DEL RECORDING
// ─────────────────────────────────────────────────────────────────────────────
// Questa classe gestisce il ciclo di vita del recording:
//
//   StartPathRecording(fromNodeId, fromX, fromY)
//     → apre un PathRecordingState. Se c'era già un recording attivo,
//        viene scartato (l'NPC ha visitato un nodo intermedio senza
//        completare il percorso verso il target originale).
//
//   RecordStep(toX, toY)
//     → aggiunge la cella corrente al buffer. Se supera MaxStepsPerRecording,
//        marca come overflow (il percorso è troppo lungo per essere utile).
//
//   TryCompleteRecording(toNodeId, toX, toY, nowTick, out ComplexEdge)
//     → chiude il recording, comprime in segmenti, produce il ComplexEdge.
//        Chiamato quando l'NPC calpesta un altro nodo landmark.
//
//   AbortRecording()
//     → scarta il recording corrente (intent cancellato, stuck, ecc.)
//
// COMPRESSIONE RUN-LENGTH ENCODING CARDINALE
// ─────────────────────────────────────────────────────────────────────────────
// CompressPathToSegments converte la lista di celle in PathSegment[]:
// 1. Calcola la direzione di ogni passo (differenza tra celle consecutive).
// 2. Fonde passi consecutivi nella stessa direzione in un unico segmento.
// 3. Scarta passi non cardinali (diagonali) — non dovrebbero esistere sulla
//    griglia Arcontio ma gestiamo il caso difensivamente.
//
// EVICTION E MANUTENZIONE
// ─────────────────────────────────────────────────────────────────────────────
// TickMaintenance:
//   - Decrementa la confidence degli edge non usati di recente
//     (ogni maintenancePeriodTicks tick, -confidenceDecayPerMaintenance).
//   - Rimuove gli edge con confidence < minConfidenceToKeep
//     o non visti da più di staleTicksBeforeEviction tick.
// =============================================================================

using System;
using System.Collections.Generic;

namespace Arcontio.Core
{
    /// <summary>
    /// <b>NpcComplexEdgeMemory</b> — store per-NPC degli edge complessi imparati
    /// dalla navigazione fisica tra nodi landmark.
    ///
    /// <para>
    /// Un edge complesso nasce quando un NPC percorre fisicamente un tragitto tra
    /// due nodi landmark che non hanno un edge diretto nel <see cref="LandmarkRegistry"/>
    /// globale. Il percorso viene registrato passo per passo e poi compresso in
    /// segmenti direzione/lunghezza (<see cref="PathSegment"/>).
    /// </para>
    ///
    /// <para><b>Patch:</b> 0.02.09.A</para>
    /// </summary>
    public sealed class NpcComplexEdgeMemory
    {
        // =====================================================================
        // STORE DEGLI EDGE COMPLESSI
        // =====================================================================

        /// <summary>
        /// Dizionario principale: EdgeKey → ComplexEdge.
        /// Chiave non orientata: (min(A,B), max(A,B)).
        /// </summary>
        private readonly Dictionary<NpcLandmarkMemory.EdgeKey, ComplexEdge> _edges;

        /// <summary>
        /// Numero massimo di edge complessi memorizzabili.
        /// Quando il cap è raggiunto, vengono evicti gli edge meno affidabili.
        /// </summary>
        private readonly int _maxEdges;

        // =====================================================================
        // RECORDING STATE
        // =====================================================================

        /// <summary>
        /// Recording attivo (se non null, l'NPC sta accumulando passi).
        /// Viene aperto su StartPathRecording e chiuso su TryCompleteRecording.
        /// </summary>
        private PathRecordingState _activeRecording;

        // =====================================================================
        // PARAMETRI
        // =====================================================================

        /// <summary>
        /// Numero massimo di passi per un singolo recording.
        /// Se il percorso supera questo valore, viene scartato come "troppo lungo".
        /// Default: 128 passi (copertura ~diagonal di una mappa 64x64).
        /// </summary>
        public int MaxStepsPerRecording;

        /// <summary>
        /// Tick dopo cui un edge non visto viene considerato stale e candidato
        /// all'eviction. Default: 60000 (molto alto, simile agli edge semplici).
        /// </summary>
        public int StaleTicksBeforeEviction;

        /// <summary>
        /// Confidence minima sotto cui un edge viene rimosso anche se non stale.
        /// </summary>
        public float MinConfidenceToKeep;

        /// <summary>
        /// Decadimento della confidence ad ogni ciclo di manutenzione.
        /// </summary>
        public float ConfidenceDecayPerMaintenance;

        // =====================================================================
        // COSTRUTTORE
        // =====================================================================

        public NpcComplexEdgeMemory(int maxEdges, int maxStepsPerRecording = 128)
        {
            _maxEdges              = maxEdges < 1 ? 1 : maxEdges;
            _edges                 = new Dictionary<NpcLandmarkMemory.EdgeKey, ComplexEdge>(_maxEdges + 4);
            _activeRecording       = null;
            MaxStepsPerRecording   = maxStepsPerRecording;
            StaleTicksBeforeEviction = 60000;
            MinConfidenceToKeep    = 0.05f;
            ConfidenceDecayPerMaintenance = 0.02f;
        }

        // =====================================================================
        // PROPRIETÀ
        // =====================================================================

        /// <summary>Numero di edge complessi attualmente memorizzati.</summary>
        public int Count => _edges.Count;

        /// <summary>True se è attivo un recording di percorso.</summary>
        public bool IsRecording => _activeRecording != null;

        /// <summary>
        /// ID del nodo landmark da cui è partito il recording attivo.
        /// Ritorna 0 se nessun recording è attivo.
        /// Usato da LandmarkPerceptionSystem per il Meccanismo 2 (ibrido fisico+visivo).
        /// </summary>
        public int ActiveRecordingFromNodeId => _activeRecording?.FromNodeId ?? 0;

        /// <summary>
        /// Numero di passi accumulati nel recording attivo (inclusa la cella di partenza).
        /// Ritorna 0 se nessun recording è attivo.
        /// Usato come StepCount nel costo del Meccanismo 2 (ibrido fisico+visivo).
        /// </summary>
        public int ActiveRecordingStepCount => _activeRecording?.Steps.Count ?? 0;

        /// <summary>
        /// Accesso in lettura agli edge complessi (per l'overlay e il planner).
        /// </summary>
        public IReadOnlyDictionary<NpcLandmarkMemory.EdgeKey, ComplexEdge> Edges => _edges;

        // =====================================================================
        // API DI RECORDING
        // =====================================================================

        /// <summary>
        /// Avvia la registrazione di un percorso dal nodo <paramref name="fromNodeId"/>.
        ///
        /// <para>
        /// Se era attivo un recording precedente, viene scartato silenziosamente:
        /// l'NPC ha visitato un nodo intermedio senza completare il percorso
        /// verso il target originale del recording.
        /// </para>
        ///
        /// <para>
        /// La cella di partenza (<paramref name="startX"/>, <paramref name="startY"/>)
        /// viene inclusa come primo step del buffer.
        /// </para>
        /// </summary>
        /// <param name="fromNodeId">ID del nodo landmark di partenza.</param>
        /// <param name="startX">X della cella corrispondente al nodo di partenza.</param>
        /// <param name="startY">Y della cella corrispondente al nodo di partenza.</param>
        public void StartPathRecording(int fromNodeId, int startX, int startY)
        {
            // Scarta il recording precedente se presente.
            // Non è un errore: significa che l'NPC ha visitato un terzo nodo
            // mentre stava registrando (es. era in APPROACHING e ha calpestato
            // un nodo intermedio inaspettato).
            _activeRecording = new PathRecordingState(fromNodeId, startX, startY, MaxStepsPerRecording);
        }

        /// <summary>
        /// Aggiunge un passo al recording attivo.
        ///
        /// <para>
        /// Chiamato da <c>World.NotifyNpcMovedForLandmarkLearning</c> ad ogni
        /// passo dell'NPC mentre è in corso un recording. Se non c'è recording
        /// attivo, questa chiamata è no-op.
        /// </para>
        ///
        /// <para>
        /// Se il buffer supera <see cref="MaxStepsPerRecording"/>, il recording
        /// viene marcato come overflow e sarà scartato al completamento.
        /// </para>
        /// </summary>
        public void RecordStep(int x, int y)
        {
            _activeRecording?.AddStep(x, y);
        }

        /// <summary>
        /// Tenta di completare il recording quando l'NPC raggiunge un nodo landmark.
        ///
        /// <para>
        /// Se il recording è valido (non overflow, almeno 2 passi, nodo diverso
        /// da quello di partenza), comprime il percorso in segmenti e produce
        /// un <see cref="ComplexEdge"/>. Il ComplexEdge viene poi aggiunto o
        /// aggiornato nello store tramite <see cref="LearnComplexEdge"/>.
        /// </para>
        ///
        /// <para>
        /// Il recording viene sempre chiuso dopo questa chiamata, sia in caso
        /// di successo che di fallimento.
        /// </para>
        /// </summary>
        /// <param name="toNodeId">ID del nodo landmark raggiunto.</param>
        /// <param name="nowTick">Tick corrente.</param>
        /// <param name="edge">L'edge prodotto, se la chiamata ha avuto successo.</param>
        /// <returns>True se è stato prodotto un ComplexEdge valido.</returns>
        public bool TryCompleteRecording(int toNodeId, long nowTick, out ComplexEdge edge)
        {
            edge = null;

            // Nessun recording attivo.
            if (_activeRecording == null)
                return false;

            var rec = _activeRecording;
            _activeRecording = null; // chiudi sempre il recording

            // Il nodo di arrivo deve essere diverso da quello di partenza.
            if (toNodeId == rec.FromNodeId || toNodeId == 0 || rec.FromNodeId == 0)
                return false;

            // Se il buffer ha fatto overflow, il percorso è troppo lungo.
            if (rec.Overflowed)
                return false;

            // Servono almeno 2 posizioni (partenza + almeno 1 passo).
            if (rec.Steps.Count < 2)
                return false;

            // Comprimi il percorso in segmenti.
            var segments = CompressPathToSegments(rec.Steps);
            if (segments.Count == 0)
                return false;

            // Calcola la chiave edge (non orientata).
            var key = new NpcLandmarkMemory.EdgeKey(rec.FromNodeId, toNodeId);

            // Calcola il BaseCost.
            int cost = 0;
            for (int i = 0; i < segments.Count; i++)
                cost += segments[i].Length;

            // Crea o aggiorna lo store.
            edge = LearnComplexEdge(key, segments, cost, nowTick);
            return edge != null;
        }

        /// <summary>
        /// Scarta il recording attivo senza produrre un edge.
        ///
        /// <para>
        /// Da chiamare quando l'NPC viene interrotto (intent cancellato, stuck,
        /// cambio di navigazione radicale).
        /// </para>
        /// </summary>
        public void AbortRecording()
        {
            _activeRecording = null;
        }

        // =====================================================================
        // API DI APPRENDIMENTO
        // =====================================================================

        /// <summary>
        /// Crea o aggiorna un <see cref="ComplexEdge"/> inferito visivamente
        /// (Meccanismo 2 — ibrido fisico+visivo di LandmarkPerceptionSystem).
        ///
        /// <para>
        /// A differenza di <see cref="TryCompleteRecording"/>, questo edge nasce senza
        /// segmenti fisici: il percorso non è stato percorso interamente, ma è stato
        /// stimato visivamente (StepCount dal nodo di partenza + Manhattan visivo al target).
        /// I <see cref="ComplexEdge.Segments"/> restano vuoti fino a quando l'NPC non
        /// percorre fisicamente il tratto; a quel punto <see cref="TryCompleteRecording"/>
        /// sovrascrive con i segmenti reali e confidence 0.25f.
        /// </para>
        ///
        /// <para>
        /// Se l'edge fisico già esiste (con segmenti), questa chiamata è no-op per
        /// non degradare un'informazione più precisa con una stima visiva.
        /// </para>
        /// </summary>
        /// <param name="nodeA">ID nodo di partenza (l'ultimo calpestato fisicamente).</param>
        /// <param name="nodeB">ID nodo di destinazione (visto nel FOV).</param>
        /// <param name="cost">Stima del costo (StepCount + Manhattan).</param>
        /// <param name="nowTick">Tick corrente.</param>
        /// <param name="confidence">Confidence iniziale (es. 0.15f).</param>
        public void LearnVisualEdge(int nodeA, int nodeB, int cost, long nowTick, float confidence)
        {
            if (nodeA == 0 || nodeB == 0 || nodeA == nodeB || cost < 1) return;

            var key = new NpcLandmarkMemory.EdgeKey(nodeA, nodeB);

            // Se esiste già un edge fisico (con segmenti), non degradarlo con una stima visiva.
            if (_edges.TryGetValue(key, out var existing))
            {
                if (existing.Segments != null && existing.Segments.Count > 0)
                    return; // edge fisico: non toccare
                // Edge visivo già presente: rinforza solo se il nuovo costo è migliore.
                if (cost < existing.BaseCost)
                    existing.BaseCost = cost;
                existing.LastSeenTick = nowTick;
                return;
            }

            // Edge nuovo: verifica cap.
            if (_edges.Count >= _maxEdges)
            {
                EvictLeastConfident();
                if (_edges.Count >= _maxEdges)
                    return;
            }

            var edge = new ComplexEdge(key, new List<PathSegment>(), nowTick);
            edge.BaseCost   = cost;
            edge.Confidence = confidence > 0f ? confidence : 0.01f;
            _edges[key] = edge;
        }

        /// <summary>
        /// Aggiunge o aggiorna un <see cref="ComplexEdge"/> nello store.
        ///
        /// <para>
        /// Se l'edge esiste già, chiama <see cref="ComplexEdge.UpdateIfBetter"/>:
        /// aggiorna i segmenti solo se il nuovo percorso è significativamente
        /// più corto, e rinforza la confidence in ogni caso.
        /// </para>
        ///
        /// <para>
        /// Se l'edge non esiste e il cap è raggiunto, evicta l'edge con
        /// confidence minima prima di aggiungere il nuovo.
        /// </para>
        /// </summary>
        /// <returns>L'edge aggiunto o aggiornato (mai null in caso di successo).</returns>
        private ComplexEdge LearnComplexEdge(
            NpcLandmarkMemory.EdgeKey key,
            List<PathSegment> segments,
            int cost,
            long nowTick)
        {
            if (_edges.TryGetValue(key, out var existing))
            {
                // Edge fisico in arrivo (con segmenti reali): sovrascrive sempre un
                // eventuale edge visivo precedente (senza segmenti), che era solo una stima.
                bool incomingIsPhysical = segments != null && segments.Count > 0;
                bool existingIsVisual   = existing.Segments == null || existing.Segments.Count == 0;
                if (incomingIsPhysical && existingIsVisual)
                {
                    existing.Segments.Clear();
                    existing.Segments.AddRange(segments);
                    existing.BaseCost = cost;
                    existing.Reinforce(nowTick);
                    return existing;
                }
                // Edge fisico vs fisico: aggiorna solo se il nuovo percorso è migliore.
                existing.UpdateIfBetter(segments, cost, nowTick);
                return existing;
            }

            // Edge nuovo: verifica cap.
            if (_edges.Count >= _maxEdges)
            {
                // Evicta l'edge con confidence minima.
                EvictLeastConfident();
                if (_edges.Count >= _maxEdges)
                    return null; // cap ancora pieno, impossibile aggiungere
            }

            var newEdge = new ComplexEdge(key, segments, nowTick);
            _edges[key] = newEdge;
            return newEdge;
        }

        // =====================================================================
        // QUERY
        // =====================================================================

        /// <summary>
        /// Verifica se esiste un edge complesso tra due nodi.
        /// </summary>
        public bool HasComplexEdge(int nodeA, int nodeB)
        {
            if (nodeA == 0 || nodeB == 0 || nodeA == nodeB) return false;
            return _edges.ContainsKey(new NpcLandmarkMemory.EdgeKey(nodeA, nodeB));
        }

        /// <summary>
        /// Recupera un edge complesso tra due nodi, se esiste.
        /// </summary>
        public bool TryGetComplexEdge(int nodeA, int nodeB, out ComplexEdge edge)
        {
            edge = null;
            if (nodeA == 0 || nodeB == 0 || nodeA == nodeB) return false;
            return _edges.TryGetValue(new NpcLandmarkMemory.EdgeKey(nodeA, nodeB), out edge);
        }

        /// <summary>
        /// Imposta un flag su un edge complesso esistente.
        /// Chiamato da sistemi esterni (failure learning, Rule).
        /// </summary>
        public void SetEdgeFlag(int nodeA, int nodeB, ComplexEdgeFlags flag)
        {
            if (TryGetComplexEdge(nodeA, nodeB, out var edge))
                edge.SetFlag(flag);
        }

        /// <summary>
        /// Rimuove un flag da un edge complesso esistente.
        /// </summary>
        public void ClearEdgeFlag(int nodeA, int nodeB, ComplexEdgeFlags flag)
        {
            if (TryGetComplexEdge(nodeA, nodeB, out var edge))
                edge.ClearFlag(flag);
        }

        /// <summary>
        /// Applica una penalità alla confidence di un edge complesso esistente.
        /// Chiamato da <c>World.BlacklistBlockedMacroEdge</c> quando l'NPC risulta
        /// bloccato su un macro-edge durante il Failure Ladder (Task 5).
        /// </summary>
        /// <param name="nodeA">Endpoint A dell'edge.</param>
        /// <param name="nodeB">Endpoint B dell'edge.</param>
        /// <param name="penalty">Valore da sottrarre alla confidence (clampato a 0).</param>
        public void PenalizeComplexEdge(int nodeA, int nodeB, float penalty)
        {
            if (nodeA == 0 || nodeB == 0 || nodeA == nodeB || penalty <= 0f) return;
            var key = new NpcLandmarkMemory.EdgeKey(nodeA, nodeB);
            if (!_edges.TryGetValue(key, out var e)) return;
            e.Confidence -= penalty;
            if (e.Confidence < 0f) e.Confidence = 0f;
        }

        // =====================================================================
        // HELPER PLANNER A*
        // =====================================================================

        /// <summary>
        /// Aggiunge al buffer i vicini ComplexEdge noti per il nodo
        /// <paramref name="nodeId"/>.
        ///
        /// <para>
        /// <b>Non</b> svuota il buffer prima di appendere: i vicini ComplexEdge
        /// vengono accodati ai vicini semplici già presenti (prodotti da
        /// <c>NpcLandmarkMemory.FillKnownNeighbors</c>). Il loop A* elabora
        /// poi l'intero buffer in un'unica passata, gestendo automaticamente
        /// i duplicati tramite il confronto g-score.
        /// </para>
        ///
        /// <para>
        /// Condizione di inclusione: l'altro endpoint dell'edge deve essere
        /// conosciuto dall'NPC nella sua memoria semplice
        /// (<paramref name="landmarkMem"/>.<c>ContainsLandmark</c>).
        /// Questo evita che l'A* navighi verso nodi che l'NPC non conosce.
        /// </para>
        ///
        /// <para><b>v0.03.04.b — ComplexEdge integrazione planner A*</b></para>
        /// </summary>
        /// <param name="nodeId">Nodo corrente espanso dall'A*.</param>
        /// <param name="landmarkMem">Memoria semplice dell'NPC (verifica endpoint noto).</param>
        /// <param name="outNeighbors">Buffer in output — i vicini ComplexEdge vengono appesi.</param>
        public void FillKnownComplexNeighbors(
            int nodeId,
            NpcLandmarkMemory landmarkMem,
            List<NpcLandmarkMemory.KnownNeighbor> outNeighbors)
        {
            if (outNeighbors == null || nodeId == 0 || _edges.Count == 0 || landmarkMem == null)
                return;

            foreach (var kv in _edges)
            {
                var ce = kv.Value;
                int otherNode;
                if      (ce.Key.A == nodeId) otherNode = ce.Key.B;
                else if (ce.Key.B == nodeId) otherNode = ce.Key.A;
                else continue;

                // Includi solo se l'NPC conosce anche l'endpoint di arrivo
                if (!landmarkMem.ContainsLandmark(otherNode)) continue;

                outNeighbors.Add(new NpcLandmarkMemory.KnownNeighbor(otherNode, ce.BaseCost, ce.Confidence));
            }
        }

        // =====================================================================
        // MANUTENZIONE
        // =====================================================================

        /// <summary>
        /// Ciclo di manutenzione: decadimento confidence e eviction stale.
        ///
        /// <para>
        /// Va chiamato periodicamente da NpcLandmarkMemorySystem o da
        /// NpcLandmarkMemory.TickMaintenance.
        /// </para>
        /// </summary>
        /// <param name="nowTick">Tick corrente.</param>
        public void TickMaintenance(long nowTick)
        {
            if (_edges.Count == 0) return;

            var toRemove = new List<NpcLandmarkMemory.EdgeKey>(4);

            foreach (var kv in _edges)
            {
                var e = kv.Value;

                // Decadimento della confidence nel tempo.
                e.Confidence -= ConfidenceDecayPerMaintenance;
                if (e.Confidence < 0f) e.Confidence = 0f;

                // Eviction se stale o sotto la soglia minima.
                bool isStale     = (nowTick - e.LastSeenTick) > StaleTicksBeforeEviction;
                bool tooWeakConf = e.Confidence < MinConfidenceToKeep;

                if (isStale || tooWeakConf)
                    toRemove.Add(kv.Key);
            }

            for (int i = 0; i < toRemove.Count; i++)
                _edges.Remove(toRemove[i]);
        }

        // =====================================================================
        // COMPRESSIONE RUN-LENGTH ENCODING CARDINALE
        // =====================================================================

        /// <summary>
        /// Comprime una sequenza di celle (steps) in <see cref="PathSegment"/>.
        ///
        /// <para>
        /// Algoritmo: scansiona le differenze tra celle consecutive, raggruppa
        /// passi consecutivi nella stessa direzione cardinale in un unico segmento.
        /// </para>
        ///
        /// <para>
        /// I passi non cardinali (differenza X≠0 e Y≠0 simultaneamente) vengono
        /// saltati silenziosamente: sulla griglia Arcontio non dovrebbero esistere
        /// ma li gestiamo difensivamente.
        /// </para>
        /// </summary>
        /// <param name="steps">
        /// Lista di posizioni packed come (X &lt;&lt; 16) | (Y &amp; 0xFFFF).
        /// Include la posizione di partenza come primo elemento.
        /// </param>
        /// <returns>Lista di segmenti compressi (mai null, può essere vuota).</returns>
        internal static List<PathSegment> CompressPathToSegments(List<int> steps)
        {
            var result = new List<PathSegment>(steps.Count / 2 + 1);

            if (steps.Count < 2)
                return result;

            // Direzione e lunghezza del segmento in corso
            CardinalDirection currentDir = CardinalDirection.North;
            int               currentLen = 0;

            for (int i = 1; i < steps.Count; i++)
            {
                int ax = PathRecordingState.UnpackX(steps[i - 1]);
                int ay = PathRecordingState.UnpackY(steps[i - 1]);
                int bx = PathRecordingState.UnpackX(steps[i]);
                int by = PathRecordingState.UnpackY(steps[i]);

                int dx = bx - ax;
                int dy = by - ay;

                // Verifica che sia un passo cardinale valido (esattamente 1 cella).
                // Passi non cardinali (diagonali) o fermi vengono ignorati.
                if (!TryGetDirection(dx, dy, out var dir))
                    continue;

                if (currentLen == 0)
                {
                    // Primo passo valido: inizia il primo segmento.
                    currentDir = dir;
                    currentLen = 1;
                }
                else if (dir == currentDir)
                {
                    // Stessa direzione: prolunga il segmento corrente.
                    currentLen++;
                }
                else
                {
                    // Cambio di direzione: chiudi il segmento corrente e aprine uno nuovo.
                    result.Add(new PathSegment(currentDir, currentLen));
                    currentDir = dir;
                    currentLen = 1;
                }
            }

            // Chiudi l'ultimo segmento se ce n'è uno aperto.
            if (currentLen > 0)
                result.Add(new PathSegment(currentDir, currentLen));

            return result;
        }

        /// <summary>
        /// Converte un delta (dx, dy) in una <see cref="CardinalDirection"/>.
        /// Ritorna false se il passo non è cardinale (es. diagonale o fermo).
        /// </summary>
        private static bool TryGetDirection(int dx, int dy, out CardinalDirection dir)
        {
            dir = CardinalDirection.North;

            if (dx == 0 && dy == 1)  { dir = CardinalDirection.North; return true; }
            if (dx == 0 && dy == -1) { dir = CardinalDirection.South; return true; }
            if (dx == 1 && dy == 0)  { dir = CardinalDirection.East;  return true; }
            if (dx == -1 && dy == 0) { dir = CardinalDirection.West;  return true; }

            return false; // passo non cardinale o fermo: ignorato
        }

        // =====================================================================
        // EVICTION
        // =====================================================================

        /// <summary>
        /// Rimuove l'edge con la confidence minima per fare spazio a un nuovo edge.
        /// </summary>
        private void EvictLeastConfident()
        {
            if (_edges.Count == 0) return;

            NpcLandmarkMemory.EdgeKey worstKey = default;
            float worstConf = float.MaxValue;

            foreach (var kv in _edges)
            {
                if (kv.Value.Confidence < worstConf)
                {
                    worstConf = kv.Value.Confidence;
                    worstKey  = kv.Key;
                }
            }

            _edges.Remove(worstKey);
        }
    }
}
