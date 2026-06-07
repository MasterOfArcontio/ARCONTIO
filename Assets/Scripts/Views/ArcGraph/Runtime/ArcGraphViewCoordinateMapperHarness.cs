namespace Arcontio.View.ArcGraph
{
    // =============================================================================
    // ArcGraphViewCoordinateMapperHarness
    // =============================================================================
    /// <summary>
    /// <para>
    /// Harness statico per validare la conversione coordinate ArcGraph.
    /// </para>
    ///
    /// <para><b>Principio architetturale: picking testabile senza camera</b></para>
    /// <para>
    /// La conversione screen/viewport/cella viene verificata senza <c>Camera</c>,
    /// senza <c>Mouse.current</c> e senza scena. Questo garantisce che il futuro
    /// bridge Unity debba solo fornire pixel viewport corretti.
    /// </para>
    ///
    /// <para><b>Struttura interna:</b></para>
    /// <list type="bullet">
    ///   <item><b>RunDefaultSmoke</b>: verifica centro viewport, fuori viewport e zoom 2.</item>
    ///   <item><b>Fail</b>: restituisce motivo esplicito.</item>
    /// </list>
    /// </summary>
    public static class ArcGraphViewCoordinateMapperHarness
    {
        // =============================================================================
        // RunDefaultSmoke
        // =============================================================================
        /// <summary>
        /// <para>
        /// Esegue una smoke validation sulla conversione coordinate default.
        /// </para>
        ///
        /// <para><b>Casi verificati</b></para>
        /// <para>
        /// Il test controlla che il centro viewport a zoom 1 cada nella cella
        /// centrale, che un punto fuori viewport fallisca e che dopo zoom 2 il
        /// rettangolo visibile sia `150x150`.
        /// </para>
        /// </summary>
        public static bool RunDefaultSmoke(out string failureReason)
        {
            failureReason = string.Empty;

            var config = ArcGraphMapViewConfig.CreateDefaultV033();
            var state = ArcGraphViewState.CreateDefault(config);

            var center = ArcGraphViewCoordinateMapper.ResolveCellFromViewportPoint(
                config,
                state,
                500f,
                500f,
                1000,
                1000);

            if (!center.IsValid)
                return Fail("Expected center viewport point to resolve.", out failureReason);

            if (center.Cell.X != 125 || center.Cell.Y != 125)
                return Fail("Expected zoom 1 center to resolve to cell 125,125.", out failureReason);

            var outside = ArcGraphViewCoordinateMapper.ResolveCellFromViewportPoint(
                config,
                state,
                1000f,
                500f,
                1000,
                1000);

            if (outside.IsValid || outside.Reason != "PointOutsideViewport")
                return Fail("Expected right exclusive edge to be outside viewport.", out failureReason);

            state.SetZoomLevel(2, config);
            var zoom2 = ArcGraphViewCoordinateMapper.ResolveCellFromViewportPoint(
                config,
                state,
                0f,
                0f,
                1000,
                1000);

            if (!zoom2.IsValid)
                return Fail("Expected zoom 2 bottom-left to resolve.", out failureReason);

            if (zoom2.VisibleRect.Width != 150 || zoom2.VisibleRect.Height != 150)
                return Fail("Expected zoom 2 visible rect to be 150x150.", out failureReason);

            if (zoom2.Cell.X != 50 || zoom2.Cell.Y != 50)
                return Fail("Expected zoom 2 bottom-left to resolve to cell 50,50.", out failureReason);

            return true;
        }

        private static bool Fail(string reason, out string failureReason)
        {
            failureReason = reason;
            return false;
        }
    }
}
