namespace Arcontio.View.ArcGraph
{
    // =============================================================================
    // ArcGraphRenderState
    // =============================================================================
    /// <summary>
    /// <para>
    /// Stato minimo condiviso del sistema grafico <c>arcgraph</c>.
    /// </para>
    ///
    /// <para><b>Principio architetturale: stato grafico, non stato mondo</b></para>
    /// <para>
    /// Questo oggetto descrive come la view sta mostrando il mondo, non cosa sia
    /// vero nel mondo. Contiene il livello <c>Z</c> attivo, la dimensione chunk,
    /// il rapporto cella/world e il dirty state visuale. Non possiede NPC, oggetti,
    /// bisogni, decisioni, job o memoria.
    /// </para>
    ///
    /// <para><b>Struttura interna:</b></para>
    /// <list type="bullet">
    ///   <item><b>VisibleZLevel</b>: livello discreto attualmente mostrato.</item>
    ///   <item><b>TileSizeWorld</b>: scala cella -> world units.</item>
    ///   <item><b>ChunkSizeCells</b>: grandezza chunk grafico in celle.</item>
    ///   <item><b>Dirty</b>: registro delle porzioni da aggiornare.</item>
    /// </list>
    /// </summary>
    public sealed class ArcGraphRenderState
    {
        public int VisibleZLevel { get; private set; }
        public float TileSizeWorld { get; private set; }
        public int ChunkSizeCells { get; private set; }
        public ArcGraphDirtyState Dirty { get; } = new();

        // =============================================================================
        // ArcGraphRenderState
        // =============================================================================
        /// <summary>
        /// <para>
        /// Crea uno stato render con parametri normalizzati.
        /// </para>
        ///
        /// <para><b>Compatibilita' iniziale</b></para>
        /// <para>
        /// I default rispecchiano la MapGrid attuale: cella world pari a uno e
        /// chunk da sedici celle. Il costruttore corregge valori non validi per
        /// evitare che un renderer futuro divida per zero o crei chunk nulli.
        /// </para>
        ///
        /// <para><b>Struttura interna:</b></para>
        /// <list type="bullet">
        ///   <item><b>visibleZLevel</b>: livello mostrato.</item>
        ///   <item><b>tileSizeWorld</b>: scala cella/world normalizzata.</item>
        ///   <item><b>chunkSizeCells</b>: chunk minimo pari a una cella.</item>
        /// </list>
        /// </summary>
        public ArcGraphRenderState(int visibleZLevel = 0, float tileSizeWorld = 1f, int chunkSizeCells = 16)
        {
            VisibleZLevel = visibleZLevel;
            TileSizeWorld = tileSizeWorld > 0.0001f ? tileSizeWorld : 1f;
            ChunkSizeCells = chunkSizeCells > 0 ? chunkSizeCells : 1;
        }

        // =============================================================================
        // SetVisibleZLevel
        // =============================================================================
        /// <summary>
        /// <para>
        /// Cambia il livello <c>Z</c> visualizzato.
        /// </para>
        ///
        /// <para><b>Separazione simulazione/rendering</b></para>
        /// <para>
        /// Cambiare livello visuale non sposta entita', non cambia altitudine reale
        /// e non modifica la mappa. Segnala solo quale slice il renderer dovrebbe
        /// mostrare nel prossimo refresh.
        /// </para>
        ///
        /// <para><b>Struttura interna:</b></para>
        /// <list type="bullet">
        ///   <item><b>zLevel</b>: nuovo livello discreto visibile.</item>
        /// </list>
        /// </summary>
        public void SetVisibleZLevel(int zLevel)
        {
            VisibleZLevel = zLevel;
        }

        // =============================================================================
        // ResolveChunkCoord
        // =============================================================================
        /// <summary>
        /// <para>
        /// Converte una cella in coordinata chunk usando il chunk size corrente.
        /// </para>
        ///
        /// <para><b>Dirty chunk preparatorio</b></para>
        /// <para>
        /// Questo helper permette a un layer di marcare il chunk corrispondente a
        /// una cella senza duplicare formule tra moduli futuri. Per ora resta
        /// semplice e discreto: nessuna camera, nessun culling, nessun accesso al
        /// <c>World</c>.
        /// </para>
        ///
        /// <para><b>Struttura interna:</b></para>
        /// <list type="bullet">
        ///   <item><b>cell</b>: cella da convertire.</item>
        ///   <item><b>ChunkSizeCells</b>: divisore normalizzato.</item>
        /// </list>
        /// </summary>
        public ArcGraphChunkCoord ResolveChunkCoord(ArcGraphCellCoord cell)
        {
            return new ArcGraphChunkCoord(
                FloorDiv(cell.X, ChunkSizeCells),
                FloorDiv(cell.Y, ChunkSizeCells),
                cell.Z);
        }

        private static int FloorDiv(int value, int divisor)
        {
            int quotient = value / divisor;
            int remainder = value % divisor;
            if (remainder != 0 && ((remainder < 0) != (divisor < 0)))
                quotient--;
            return quotient;
        }
    }
}
