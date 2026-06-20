namespace Arcontio.View.ArcGraph
{
    // =============================================================================
    // ArcGraphViewController
    // =============================================================================
    /// <summary>
    /// <para>
    /// Controller passivo minimale per il pan logico della finestra ArcGraph.
    /// </para>
    ///
    /// <para><b>Principio architetturale: nessun livello zoom nel contratto passivo</b></para>
    /// <para>
    /// Lo zoom fisico e' posseduto dal controller camera Unity. Questo controller
    /// resta solo per compatibilita' con il wrapper interattivo e applica, quando
    /// richiesto, un pan in celle sullo stato view condiviso.
    /// </para>
    /// </summary>
    public sealed class ArcGraphViewController
    {
        public ArcGraphViewControllerResult ApplyInputFrame(
            ArcGraphMapViewConfig config,
            ArcGraphViewState state,
            ArcGraphViewInputFrame input,
            int viewportPixelWidth,
            int viewportPixelHeight)
        {
            config = config ?? ArcGraphMapViewConfig.CreateDefaultV033();
            state = state ?? ArcGraphViewState.CreateDefault(config);

            bool ignoredBecausePointerOverUi = input.IsPointerOverUi;
            bool didApplyPan = false;
            bool ignoredPanBecauseViewportInvalid = false;

            if (!ignoredBecausePointerOverUi &&
                config.PanUsesMiddleMouseButton &&
                input.IsMiddleMouseHeld &&
                HasMeaningfulMouseDelta(input))
            {
                if (!TryConvertPixelDeltaToCellDelta(
                    input,
                    config,
                    viewportPixelWidth,
                    viewportPixelHeight,
                    out float deltaCellsX,
                    out float deltaCellsY))
                {
                    ignoredPanBecauseViewportInvalid = true;
                }
                else
                {
                    didApplyPan = state.ApplyPanCells(deltaCellsX, deltaCellsY, config);
                }
            }

            return new ArcGraphViewControllerResult(
                didChangeZoom: false,
                didApplyPan,
                ignoredBecausePointerOverUi,
                ignoredPanBecauseZoomDisallowsPan: false,
                ignoredPanBecauseViewportInvalid,
                state.CenterCellX,
                state.CenterCellY);
        }

        private static bool TryConvertPixelDeltaToCellDelta(
            ArcGraphViewInputFrame input,
            ArcGraphMapViewConfig config,
            int viewportPixelWidth,
            int viewportPixelHeight,
            out float deltaCellsX,
            out float deltaCellsY)
        {
            deltaCellsX = 0f;
            deltaCellsY = 0f;

            if (viewportPixelWidth <= 0 || viewportPixelHeight <= 0)
                return false;

            float visibleCellsY = config.DefaultOrthographicSize * 2f;
            float visibleCellsX = visibleCellsY * (viewportPixelWidth / (float)viewportPixelHeight);
            float cellsPerPixelX = visibleCellsX / viewportPixelWidth;
            float cellsPerPixelY = visibleCellsY / viewportPixelHeight;

            deltaCellsX = -input.MouseDeltaPixelsX * cellsPerPixelX;
            deltaCellsY = -input.MouseDeltaPixelsY * cellsPerPixelY;
            return true;
        }

        private static bool HasMeaningfulMouseDelta(ArcGraphViewInputFrame input)
        {
            return input.MouseDeltaPixelsX != 0f || input.MouseDeltaPixelsY != 0f;
        }
    }
}
