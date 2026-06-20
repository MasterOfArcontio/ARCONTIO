namespace Arcontio.View.ArcGraph
{
    // =============================================================================
    // ArcGraphViewControllerHarness
    // =============================================================================
    /// <summary>
    /// <para>
    /// Harness minimale del controller view senza livelli zoom.
    /// </para>
    /// </summary>
    public static class ArcGraphViewControllerHarness
    {
        public static bool Run()
        {
            var config = ArcGraphMapViewConfig.CreateDefaultV033();
            var state = ArcGraphViewState.CreateDefault(config);
            var controller = new ArcGraphViewController();

            var result = controller.ApplyInputFrame(
                config,
                state,
                new ArcGraphViewInputFrame(
                    0,
                    true,
                    10f,
                    0f,
                    100f,
                    100f,
                    true,
                    false),
                1000,
                1000);

            return result.DidApplyPan &&
                   !result.DidChangeZoom &&
                   state.CenterCellX < config.MapWidthCells * 0.5f;
        }
    }
}
