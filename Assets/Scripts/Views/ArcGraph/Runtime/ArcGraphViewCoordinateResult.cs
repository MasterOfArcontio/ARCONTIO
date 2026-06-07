namespace Arcontio.View.ArcGraph
{
    // =============================================================================
    // ArcGraphViewCoordinateResult
    // =============================================================================
    /// <summary>
    /// <para>
    /// Risultato della conversione da punto schermo a cella ArcGraph.
    /// </para>
    ///
    /// <para><b>Principio architetturale: picking spiegabile</b></para>
    /// <para>
    /// La conversione coordinate e' un passaggio delicato per debug, selezione e
    /// futuri comandi umani. Il risultato espone sia la cella finale sia i motivi
    /// per cui la conversione puo' fallire, evitando che il caller confonda un
    /// click fuori viewport con una cella valida.
    /// </para>
    ///
    /// <para><b>Struttura interna:</b></para>
    /// <list type="bullet">
    ///   <item><b>IsValid</b>: conversione riuscita.</item>
    ///   <item><b>Cell</b>: cella risolta quando valida.</item>
    ///   <item><b>NormalizedX/Y</b>: posizione normalizzata nel viewport.</item>
    ///   <item><b>VisibleRect</b>: rettangolo celle usato per la conversione.</item>
    ///   <item><b>Reason</b>: motivo diagnostico.</item>
    /// </list>
    /// </summary>
    public readonly struct ArcGraphViewCoordinateResult
    {
        public readonly bool IsValid;
        public readonly ArcGraphCellCoord Cell;
        public readonly float NormalizedX;
        public readonly float NormalizedY;
        public readonly ArcGraphViewCellRect VisibleRect;
        public readonly string Reason;

        // =============================================================================
        // ArcGraphViewCoordinateResult
        // =============================================================================
        /// <summary>
        /// <para>
        /// Costruisce un risultato coordinate completo.
        /// </para>
        ///
        /// <para><b>Value object</b></para>
        /// <para>
        /// Il risultato contiene solo primitivi e value type ArcGraph. Non conserva
        /// riferimenti a camera, mouse, UI, oggetti scena o stato simulativo.
        /// </para>
        /// </summary>
        public ArcGraphViewCoordinateResult(
            bool isValid,
            ArcGraphCellCoord cell,
            float normalizedX,
            float normalizedY,
            ArcGraphViewCellRect visibleRect,
            string reason)
        {
            IsValid = isValid;
            Cell = cell;
            NormalizedX = normalizedX;
            NormalizedY = normalizedY;
            VisibleRect = visibleRect;
            Reason = reason ?? string.Empty;
        }

        public static ArcGraphViewCoordinateResult Invalid(string reason)
        {
            return new ArcGraphViewCoordinateResult(
                false,
                new ArcGraphCellCoord(0, 0, 0),
                0f,
                0f,
                new ArcGraphViewCellRect(0, 0, 0, 0),
                reason);
        }
    }
}
