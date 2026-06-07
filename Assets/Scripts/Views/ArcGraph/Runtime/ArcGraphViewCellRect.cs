namespace Arcontio.View.ArcGraph
{
    // =============================================================================
    // ArcGraphViewCellRect
    // =============================================================================
    /// <summary>
    /// <para>
    /// Rettangolo discreto di celle visibili nella view ArcGraph.
    /// </para>
    ///
    /// <para><b>Principio architetturale: finestra visiva, non partizione del mondo</b></para>
    /// <para>
    /// Questo rettangolo descrive quali celle la camera/view dovrebbe mostrare.
    /// Non decide quali celle esistono, non modifica dirty state e non influenza
    /// pathfinding, percezione o simulazione.
    /// </para>
    ///
    /// <para><b>Struttura interna:</b></para>
    /// <list type="bullet">
    ///   <item><b>MinX/MinY</b>: prima cella inclusa.</item>
    ///   <item><b>MaxXExclusive/MaxYExclusive</b>: limite esclusivo.</item>
    ///   <item><b>Width/Height</b>: dimensione effettiva in celle.</item>
    /// </list>
    /// </summary>
    public readonly struct ArcGraphViewCellRect
    {
        public readonly int MinX;
        public readonly int MinY;
        public readonly int MaxXExclusive;
        public readonly int MaxYExclusive;

        public int Width => MaxXExclusive - MinX;
        public int Height => MaxYExclusive - MinY;
        public bool IsEmpty => Width <= 0 || Height <= 0;

        // =============================================================================
        // ArcGraphViewCellRect
        // =============================================================================
        /// <summary>
        /// <para>
        /// Costruisce un rettangolo celle normalizzato.
        /// </para>
        ///
        /// <para><b>Rettangolo half-open</b></para>
        /// <para>
        /// Il limite massimo e' esclusivo, cosi' i futuri loop potranno usare la
        /// forma standard <c>for x = MinX; x &lt; MaxXExclusive</c> senza ambiguita'.
        /// </para>
        /// </summary>
        public ArcGraphViewCellRect(
            int minX,
            int minY,
            int maxXExclusive,
            int maxYExclusive)
        {
            MinX = minX;
            MinY = minY;
            MaxXExclusive = maxXExclusive > minX ? maxXExclusive : minX;
            MaxYExclusive = maxYExclusive > minY ? maxYExclusive : minY;
        }

        public override string ToString()
        {
            return $"cells[{MinX},{MinY} -> {MaxXExclusive},{MaxYExclusive}]";
        }
    }
}
