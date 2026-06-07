namespace Arcontio.View.ArcGraph
{
    // =============================================================================
    // ArcGraphViewControllerHarness
    // =============================================================================
    /// <summary>
    /// <para>
    /// Harness statico minimale per validare il controller pan/zoom ArcGraph.
    /// </para>
    ///
    /// <para><b>Principio architetturale: testabile senza scena</b></para>
    /// <para>
    /// L'harness usa solo classi C# passive. Non crea camera, non crea oggetti Unity,
    /// non legge input fisico e non usa Resources. Serve a verificare il contratto
    /// base del controller prima del futuro wrapper Unity.
    /// </para>
    ///
    /// <para><b>Struttura interna:</b></para>
    /// <list type="bullet">
    ///   <item><b>RunDefaultSmoke</b>: verifica zoom 1, zoom 2, pan e blocco UI.</item>
    ///   <item><b>Fail</b>: produce messaggi di errore espliciti.</item>
    /// </list>
    /// </summary>
    public static class ArcGraphViewControllerHarness
    {
        // =============================================================================
        // RunDefaultSmoke
        // =============================================================================
        /// <summary>
        /// <para>
        /// Esegue una validazione smoke sui default <c>v0.33</c>.
        /// </para>
        ///
        /// <para><b>Casi verificati</b></para>
        /// <para>
        /// Il test controlla che lo zoom iniziale copra tutta la mappa, che uno
        /// scatto rotellina passi al livello 2, che il pan a livello 2 sposti il
        /// centro e che l'input sopra UI venga ignorato.
        /// </para>
        /// </summary>
        public static bool RunDefaultSmoke(out string failureReason)
        {
            failureReason = string.Empty;

            var config = ArcGraphMapViewConfig.CreateDefaultV033();
            var state = ArcGraphViewState.CreateDefault(config);
            var controller = new ArcGraphViewController();

            if (state.ActiveZoomLevel != 1)
                return Fail("Expected initial zoom level 1.", out failureReason);

            var fullRect = state.ResolveVisibleCellRect(config);
            if (fullRect.Width != 250 || fullRect.Height != 250)
                return Fail("Expected zoom 1 to clamp visible rect to full 250x250 map.", out failureReason);

            var zoomResult = controller.ApplyInputFrame(
                config,
                state,
                new ArcGraphViewInputFrame(
                    1,
                    false,
                    0f,
                    0f,
                    0f,
                    0f,
                    false,
                    false),
                1000,
                1000);

            if (!zoomResult.DidChangeZoom || state.ActiveZoomLevel != 2)
                return Fail("Expected one wheel step to move from zoom 1 to zoom 2.", out failureReason);

            float beforePanX = state.CenterCellX;
            var panResult = controller.ApplyInputFrame(
                config,
                state,
                new ArcGraphViewInputFrame(
                    0,
                    true,
                    100f,
                    0f,
                    0f,
                    0f,
                    false,
                    false),
                1000,
                1000);

            if (!panResult.DidApplyPan)
                return Fail("Expected middle mouse drag to apply pan at zoom 2.", out failureReason);

            if (state.CenterCellX >= beforePanX)
                return Fail("Expected positive mouse X drag to move view center left.", out failureReason);

            float beforeUiX = state.CenterCellX;
            var uiResult = controller.ApplyInputFrame(
                config,
                state,
                new ArcGraphViewInputFrame(
                    1,
                    true,
                    100f,
                    100f,
                    0f,
                    0f,
                    false,
                    true),
                1000,
                1000);

            if (!uiResult.IgnoredBecausePointerOverUi)
                return Fail("Expected pointer-over-UI frame to be reported as ignored.", out failureReason);

            if (state.CenterCellX != beforeUiX)
                return Fail("Expected pointer-over-UI frame to leave center unchanged.", out failureReason);

            return true;
        }

        private static bool Fail(string reason, out string failureReason)
        {
            failureReason = reason;
            return false;
        }
    }
}
