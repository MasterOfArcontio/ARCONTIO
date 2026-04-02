// =============================================================================
// FovUtils.cs
// Namespace: Arcontio.Core
// Patch: 0.02.5A
// =============================================================================
//
// MOTIVAZIONE (Patch 0.02.5A)
// ─────────────────────────────────────────────────────────────────────────────
// Prima di questa patch la logica del campo visivo era duplicata in 4 posti:
//
//   1) ObjectPerceptionSystem.IsInCone        — percezione oggetti
//   2) NpcPerceptionSystem.IsInCone           — percezione NPC
//   3) MemoryEncodingSystem.IsInCone          — encoding memoria (con BUG nei segni!)
//   4) SimulationHost.IsInCone_Debug          — snapshot debug Day8
//
// Anche Manhattan era duplicato in 5 posti (i 4 sopra + TokenEmissionPipeline
// e TokenDeliveryPipeline).
//
// Questa classe è la fonte canonica unica per tutte le query geometriche legate
// al campo visivo degli NPC. Tutti gli altri file ora delegano qui.
//
// BUG CORRETTO IN QUESTA PATCH
// ─────────────────────────────────────────────────────────────────────────────
// MemoryEncodingSystem.IsInCone aveva i segni di 'side' errati per i casi
// South (side = dx invece di side = -dx) ed East (side = dy invece di -dy).
// Poiché dopo il calcolo si prende abs(side), il risultato finale era comunque
// corretto per la simmetria del cono. Tuttavia la logica era incoerente con
// le altre implementazioni e con la definizione geometrica corretta.
// La versione canonica in questa classe usa i segni corretti in tutti i casi.
//
// PIPELINE CANONICA DI VISIONE (ARCONTIO Core Standard v1.0)
// ─────────────────────────────────────────────────────────────────────────────
//   1) Range gate  — distanza Manhattan <= visionRange
//   2) Cone gate   — IsInCone (opzionale, controllato da useCone)
//   3) LOS gate    — World.HasLineOfSight (Bresenham sull'OcclusionMap)
//
// NOTA SUI PARAMETRI DI CONO
// ─────────────────────────────────────────────────────────────────────────────
// Il parametro 'coneSlope' (alias 'coneHalfWidthPerStep') controlla la larghezza:
//   - 0.0  => solo cella direttamente davanti (linea)
//   - 0.5  => cono stretto (~27° su griglia Manhattan)
//   - 1.0  => cono ampio (~45° su griglia Manhattan)
//   - >1.0 => cono molto ampio (oltre i 45°)
//
// Il cono è discreto (griglia intera): la cella (tx, ty) è nel cono se e solo se
//   |side| <= floor(forward * coneSlope + epsilon)
// dove epsilon = 0.0001f serve a includere esattamente i bordi del cono senza
// problemi di floating point.
//
// NOTA SU IsInFront
// ─────────────────────────────────────────────────────────────────────────────
// IsInFront è la modalità "legacy" pre-cono: considera visibile solo la cella
// direttamente davanti (stessa riga o colonna, senso forward). Era usata in
// ObjectPerceptionSystem come fallback quando useCone=false.
// Mantenuta per compatibilità con le config che disattivano il cono.
// =============================================================================

using System;

namespace Arcontio.Core
{
    /// <summary>
    /// <b>FovUtils</b> — utilità statiche per il campo visivo degli NPC su griglia.
    ///
    /// <para>
    /// Questa classe centralizza tutta la geometria del FOV che prima era
    /// duplicata in <c>ObjectPerceptionSystem</c>, <c>NpcPerceptionSystem</c>,
    /// <c>MemoryEncodingSystem</c> e <c>SimulationHost</c>.
    /// </para>
    ///
    /// <para>
    /// Tutti i metodi sono statici puri (zero stato, zero allocazioni).
    /// Adatti a essere chiamati ogni tick da più System in parallelo (se mai
    /// si introducesse multi-threading nella simulazione).
    /// </para>
    ///
    /// <para><b>Patch:</b> 0.02.5A</para>
    /// </summary>
    public static class FovUtils
    {
        // =====================================================================
        // COSTANTE EPSILON
        // =====================================================================
        // Piccola tolleranza floating-point usata nel confronto del cono.
        // Serve a includere esattamente le celle sul bordo del cono senza
        // problemi di arrotondamento (es. forward=3, slope=0.333... => maxSide=0
        // invece del corretto 1 se mancasse l'epsilon).
        private const float ConeEdgeEpsilon = 0.0001f;

        // =====================================================================
        // DISTANZA MANHATTAN
        // =====================================================================

        /// <summary>
        /// Calcola la distanza di Manhattan (griglia cardinale) tra due celle.
        ///
        /// <para>
        /// Formula: |ax - bx| + |ay - by|
        /// </para>
        ///
        /// <para>
        /// Usata come gate di range prima di controllare cono e LOS,
        /// perché è O(1) e molto più economica di qualsiasi altro test.
        /// </para>
        /// </summary>
        /// <param name="ax">Coordinata X della cella A.</param>
        /// <param name="ay">Coordinata Y della cella A.</param>
        /// <param name="bx">Coordinata X della cella B.</param>
        /// <param name="by">Coordinata Y della cella B.</param>
        /// <returns>Distanza Manhattan in celle.</returns>
        public static int Manhattan(int ax, int ay, int bx, int by)
        {
            int dx = ax - bx;
            int dy = ay - by;
            if (dx < 0) dx = -dx;
            if (dy < 0) dy = -dy;
            return dx + dy;
        }

        // =====================================================================
        // TEST DI CONO
        // =====================================================================

        /// <summary>
        /// Verifica se la cella target <c>(tx, ty)</c> è all'interno del cono
        /// visivo dell'osservatore in <c>(sx, sy)</c> con orientamento <c>facing</c>.
        ///
        /// <para><b>Geometria del cono discreto:</b></para>
        /// <para>
        /// Dato l'orientamento, si decompone il vettore (dx, dy) in due componenti:
        /// <list type="bullet">
        ///   <item><c>forward</c>: quanto la cella è avanti (deve essere &gt; 0)</item>
        ///   <item><c>side</c>: quanto la cella è laterale</item>
        /// </list>
        /// La cella è nel cono se: |side| &lt;= floor(forward * coneSlope + epsilon)
        /// </para>
        ///
        /// <para><b>Mapping orientamento → assi:</b></para>
        /// <list type="table">
        ///   <item><term>North</term><description>forward = +dy, side = +dx</description></item>
        ///   <item><term>South</term><description>forward = -dy, side = -dx</description></item>
        ///   <item><term>East </term><description>forward = +dx, side = -dy</description></item>
        ///   <item><term>West </term><description>forward = -dx, side = +dy</description></item>
        /// </list>
        ///
        /// <para>
        /// NOTA: il segno di 'side' non influisce sul risultato finale (perché si
        /// usa abs(side)) ma è definito in modo coerente per evitare ambiguità
        /// future se si estende la logica a coni asimmetrici.
        /// </para>
        /// </summary>
        /// <param name="sx">X osservatore.</param>
        /// <param name="sy">Y osservatore.</param>
        /// <param name="facing">Orientamento dell'osservatore.</param>
        /// <param name="tx">X cella target.</param>
        /// <param name="ty">Y cella target.</param>
        /// <param name="coneSlope">
        /// Slope del cono (alias coneHalfWidthPerStep).
        /// 0.0 = linea, 1.0 = cono ~45° su griglia Manhattan.
        /// </param>
        /// <returns>
        /// <c>true</c> se la cella target è all'interno del cono;
        /// <c>false</c> se è dietro, laterale oltre il limite, o allo stesso punto.
        /// </returns>
        public static bool IsInCone(
            int sx, int sy,
            CardinalDirection facing,
            int tx, int ty,
            float coneSlope)
        {
            int dx = tx - sx;
            int dy = ty - sy;

            // Decomponi in forward/side in base all'orientamento.
            // IMPORTANTE: i segni di 'side' sono definiti in modo che:
            //   North: side = dx (positivo = Est rispetto all'orientamento)
            //   South: side = -dx (speculare a North)
            //   East:  side = -dy (positivo = Nord rispetto all'orientamento)
            //   West:  side = +dy (speculare a East)
            int forward, side;

            switch (facing)
            {
                case CardinalDirection.North:
                    forward = dy;
                    side    = dx;
                    break;

                case CardinalDirection.South:
                    forward = -dy;
                    side    = -dx;  // CORRETTO: speculare a North (era 'dx' in MemoryEncodingSystem — bug 0.02.5A)
                    break;

                case CardinalDirection.East:
                    forward = dx;
                    side    = -dy;  // CORRETTO: dy negato (era '+dy' in MemoryEncodingSystem — bug 0.02.5A)
                    break;

                case CardinalDirection.West:
                    forward = -dx;
                    side    = dy;
                    break;

                default:
                    // Orientamento sconosciuto: per sicurezza non consideriamo nulla nel cono.
                    return false;
            }

            // La cella deve essere davanti (forward > 0).
            // Celle allo stesso livello (forward = 0) o dietro (forward < 0) non sono nel cono.
            if (forward <= 0)
                return false;

            // Calcola il massimo scostamento laterale ammesso per questa distanza forward.
            // Usiamo Math.Floor per coerenza con la semantica intera della griglia.
            // L'epsilon evita problemi di arrotondamento sui bordi esatti del cono.
            int absSide  = side < 0 ? -side : side;
            int maxSide  = (int)Math.Floor((forward * coneSlope) + ConeEdgeEpsilon);

            return absSide <= maxSide;
        }

        // =====================================================================
        // TEST "IN FRONT" (modalità legacy senza cono)
        // =====================================================================

        /// <summary>
        /// Verifica se la cella target è direttamente davanti all'osservatore,
        /// inteso come stessa riga/colonna nel senso dell'orientamento.
        ///
        /// <para>
        /// Questa è la modalità "legacy" pre-cono, usata quando
        /// <c>useCone = false</c> nella config. Considera visibile solo la linea
        /// cardinale frontale (es. North = stessa colonna, y &gt; sy).
        /// </para>
        ///
        /// <para>
        /// In quasi tutti i casi attuali si usa <see cref="IsInCone"/> con slope
        /// opportuno. Questo metodo è mantenuto per compatibilità con le config
        /// che hanno <c>npcVisionUseCone = false</c>.
        /// </para>
        /// </summary>
        /// <param name="sx">X osservatore.</param>
        /// <param name="sy">Y osservatore.</param>
        /// <param name="facing">Orientamento dell'osservatore.</param>
        /// <param name="tx">X cella target.</param>
        /// <param name="ty">Y cella target.</param>
        /// <returns>
        /// <c>true</c> se la cella è esattamente nella direzione frontale cardinale
        /// dell'osservatore (stessa riga o colonna, senso corretto).
        /// </returns>
        public static bool IsInFront(
            int sx, int sy,
            CardinalDirection facing,
            int tx, int ty)
        {
            int dx = tx - sx;
            int dy = ty - sy;

            // La cella deve essere esattamente nella direzione frontale:
            // stesso asse ortogonale (l'altro dx o dy deve essere 0)
            // e nella direzione corretta (forward > 0).
            return facing switch
            {
                CardinalDirection.North => dy > 0 && dx == 0,
                CardinalDirection.South => dy < 0 && dx == 0,
                CardinalDirection.East  => dx > 0 && dy == 0,
                CardinalDirection.West  => dx < 0 && dy == 0,
                _                      => false
            };
        }

        // =====================================================================
        // PIPELINE COMPLETA: RANGE + CONE/FRONT + LOS
        // =====================================================================

        /// <summary>
        /// Verifica se la cella target è visibile dall'osservatore applicando
        /// l'intera pipeline canonica di Arcontio: Range → Cone → LOS.
        ///
        /// <para>
        /// Questo metodo è il punto di ingresso unico per qualsiasi sistema
        /// che deve determinare se un punto è visibile da un NPC.
        /// Evita di replicare la sequenza di gate in ogni consumer.
        /// </para>
        ///
        /// <para><b>Gate applicati in ordine:</b></para>
        /// <list type="number">
        ///   <item>
        ///     <b>Range gate:</b> Manhattan(sx,sy,tx,ty) &lt;= visionRange.
        ///     Il gate più economico — se fallisce escludiamo subito senza LOS.
        ///   </item>
        ///   <item>
        ///     <b>Cone gate:</b> se <c>useCone = true</c>, applica
        ///     <see cref="IsInCone"/>; altrimenti applica <see cref="IsInFront"/>.
        ///     Esclude celle laterali o dietro l'osservatore.
        ///   </item>
        ///   <item>
        ///     <b>LOS gate:</b> <c>world.HasLineOfSight(sx,sy,tx,ty)</c>.
        ///     Il gate più costoso (Bresenham sull'OcclusionMap) — applicato
        ///     per ultimo per minimizzare il numero di chiamate.
        ///   </item>
        /// </list>
        ///
        /// <para>
        /// NOTA: la distanza viene calcolata internamente. Se hai già la distanza
        /// disponibile, puoi usare il check Range manualmente prima di chiamare
        /// questo metodo, oppure usare direttamente <see cref="IsInCone"/> e
        /// <c>world.HasLineOfSight</c> separatamente.
        /// </para>
        /// </summary>
        /// <param name="world">Il mondo simulato (usato per il check LOS).</param>
        /// <param name="sx">X osservatore.</param>
        /// <param name="sy">Y osservatore.</param>
        /// <param name="facing">Orientamento dell'osservatore.</param>
        /// <param name="tx">X cella target.</param>
        /// <param name="ty">Y cella target.</param>
        /// <param name="visionRange">Range massimo in celle Manhattan.</param>
        /// <param name="useCone">
        /// Se true usa <see cref="IsInCone"/>; se false usa <see cref="IsInFront"/>.
        /// </param>
        /// <param name="coneSlope">
        /// Slope del cono (usato solo se <c>useCone = true</c>).
        /// </param>
        /// <returns>
        /// <c>true</c> se la cella è visibile (supera tutti e tre i gate).
        /// </returns>
        public static bool IsVisible(
            World world,
            int sx, int sy,
            CardinalDirection facing,
            int tx, int ty,
            int visionRange,
            bool useCone,
            float coneSlope)
        {
            // --- Gate 1: Range ---
            // Il più economico: se la cella è fuori range, eliminiamo subito
            // senza dover calcolare cono o LOS.
            int dist = Manhattan(sx, sy, tx, ty);
            if (dist <= 0 || dist > visionRange)
                return false;

            // --- Gate 2: Cone / Front ---
            // Cono direzionale: esclude celle laterali e dietro l'osservatore.
            if (useCone)
            {
                if (!IsInCone(sx, sy, facing, tx, ty, coneSlope))
                    return false;
            }
            else
            {
                // Modalità legacy: solo la linea frontale cardinale.
                if (!IsInFront(sx, sy, facing, tx, ty))
                    return false;
            }

            // --- Gate 3: LOS ---
            // Il più costoso (Bresenham sull'OcclusionMap): applicato per ultimo.
            if (!world.HasLineOfSight(sx, sy, tx, ty))
                return false;

            return true;
        }

        // =====================================================================
        // QUALITY SCORE
        // =====================================================================

        /// <summary>
        /// Calcola la qualità di osservazione (0..1) basata sulla distanza.
        ///
        /// <para>
        /// Formula lineare: q = 1 - (dist / visionRange), clampata a [minQuality, 1].
        /// </para>
        ///
        /// <para>
        /// Questo valore viene usato nei <c>NpcSpottedEvent</c> e
        /// <c>ObjectSpottedEvent</c> come <c>witnessQuality01</c>.
        /// Un'osservazione a distanza 0 ha qualità 1.0 (perfetta);
        /// a distanza visionRange ha qualità minQuality.
        /// </para>
        ///
        /// <para>
        /// In futuro si potrebbe pesare anche l'orientamento dell'osservatore
        /// rispetto all'oggetto osservato (frontal bonus) o condizioni di luce.
        /// </para>
        /// </summary>
        /// <param name="dist">Distanza Manhattan tra osservatore e target.</param>
        /// <param name="visionRange">Range massimo di visione.</param>
        /// <param name="minQuality">
        /// Qualità minima garantita (default 0.05f).
        /// Evita che oggetti molto lontani abbiano qualità zero.
        /// </param>
        /// <returns>Valore di qualità nell'intervallo [minQuality, 1.0f].</returns>
        public static float ObservationQuality(
            int dist,
            int visionRange,
            float minQuality = 0.05f)
        {
            if (visionRange <= 0)
                return minQuality;

            float q = 1f - (dist / (float)visionRange);
            return q < minQuality ? minQuality : q;
        }
    }
}
