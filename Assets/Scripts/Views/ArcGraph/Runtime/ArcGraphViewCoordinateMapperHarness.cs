namespace Arcontio.View.ArcGraph
{
    // =============================================================================
    // ArcGraphViewCoordinateMapperHarness
    // =============================================================================
    /// <summary>
    /// <para>
    /// Harness statico per validare la conversione coordinate ArcGraph senza livelli zoom.
    /// </para>
    /// </summary>
    public static class ArcGraphViewCoordinateMapperHarness
    {
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
                return Fail("Expected center to resolve to cell 125,125.", out failureReason);

            var outside = ArcGraphViewCoordinateMapper.ResolveCellFromViewportPoint(
                config,
                state,
                1000f,
                500f,
                1000,
                1000);

            if (outside.IsValid || outside.Reason != "PointOutsideViewport")
                return Fail("Expected right exclusive edge to be outside viewport.", out failureReason);

            return true;
        }

        private static bool Fail(string reason, out string failureReason)
        {
            failureReason = reason;
            return false;
        }
    }
}
