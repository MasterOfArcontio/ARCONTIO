namespace Arcontio.View.ArcGraph
{
    // =============================================================================
    // ArcGraphViewController
    // =============================================================================
    /// <summary>
    /// <para>
    /// Controller passivo per zoom discreto e pan della finestra mappa ArcGraph.
    /// </para>
    ///
    /// <para><b>Principio architetturale: controller view senza scena</b></para>
    /// <para>
    /// Questo controller non e' un <c>MonoBehaviour</c>, non legge mouse o camera,
    /// non chiama <c>ScreenToWorldPoint</c> e non sposta oggetti Unity. Riceve un
    /// input frame astratto, una configurazione view, uno stato view e dimensioni
    /// viewport gia' note al chiamante. Il suo unico effetto e' aggiornare
    /// <c>ArcGraphViewState</c>.
    /// </para>
    ///
    /// <para><b>Struttura interna:</b></para>
    /// <list type="bullet">
    ///   <item><b>ApplyInputFrame</b>: applica zoom e pan a uno stato vista.</item>
    ///   <item><b>ConvertPixelDeltaToCellDelta</b>: traduce delta mouse in celle.</item>
    ///   <item><b>BuildResult</b>: produce diagnostica dell'aggiornamento.</item>
    /// </list>
    /// </summary>
    public sealed class ArcGraphViewController
    {
        // =============================================================================
        // ApplyInputFrame
        // =============================================================================
        /// <summary>
        /// <para>
        /// Applica un frame input allo stato view ArcGraph.
        /// </para>
        ///
        /// <para><b>Ordine intenzionale: zoom prima, pan dopo</b></para>
        /// <para>
        /// Se nello stesso frame arrivano rotellina e trascinamento, viene prima
        /// applicato lo zoom discreto e poi il pan viene convertito usando la nuova
        /// dimensione visibile. Questo evita che un singolo frame usi una scala
        /// vecchia per muovere una view appena zoomata.
        /// </para>
        ///
        /// <para><b>Struttura interna:</b></para>
        /// <list type="bullet">
        ///   <item><b>config</b>: profilo mappa/zoom, con fallback v0.33 se null.</item>
        ///   <item><b>state</b>: stato view da aggiornare; se null viene creato default.</item>
        ///   <item><b>input</b>: frame input gia' astratto da un wrapper esterno.</item>
        ///   <item><b>viewportPixelWidth/Height</b>: dimensioni viewport usate per convertire pan.</item>
        /// </list>
        /// </summary>
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
            bool didChangeZoom = false;
            bool didApplyPan = false;
            bool ignoredPanBecauseZoomDisallowsPan = false;
            bool ignoredPanBecauseViewportInvalid = false;

            // Se la UI ha priorita', il controller non consuma ne' zoom ne' pan.
            // Il risultato lo segnala, ma non modifica lo stato vista.
            if (ignoredBecausePointerOverUi)
            {
                return BuildResult(
                    state,
                    didChangeZoom,
                    didApplyPan,
                    ignoredBecausePointerOverUi,
                    ignoredPanBecauseZoomDisallowsPan,
                    ignoredPanBecauseViewportInvalid);
            }

            int beforeZoom = state.ActiveZoomLevel;
            float anchorNormalizedX = 0f;
            float anchorNormalizedY = 0f;
            float anchorCellX = 0f;
            float anchorCellY = 0f;
            bool hasPointerZoomAnchor =
                input.WheelStepDelta != 0 &&
                TryResolvePointerZoomAnchor(
                    config,
                    state,
                    input,
                    viewportPixelWidth,
                    viewportPixelHeight,
                    out anchorNormalizedX,
                    out anchorNormalizedY,
                    out anchorCellX,
                    out anchorCellY);

            if (input.WheelStepDelta != 0)
            {
                state.ApplyWheelZoom(input.WheelStepDelta, config);
                didChangeZoom = state.ActiveZoomLevel != beforeZoom;

                // Dopo il cambio zoom il rettangolo visibile cambia dimensione.
                // Se conosciamo la posizione del puntatore, spostiamo il centro
                // view in modo che la stessa coordinata mappa resti sotto quel
                // puntatore. Questo e' lo "zoom to cursor" logico di ArcGraph.
                if (didChangeZoom && hasPointerZoomAnchor)
                {
                    ApplyPointerZoomAnchor(
                        config,
                        state,
                        anchorNormalizedX,
                        anchorNormalizedY,
                        anchorCellX,
                        anchorCellY);
                }
            }

            bool shouldPan =
                config.PanUsesMiddleMouseButton &&
                input.IsMiddleMouseHeld &&
                HasMeaningfulMouseDelta(input);

            if (shouldPan)
            {
                var zoom = state.CurrentZoom(config);

                if (!zoom.AllowsPan)
                {
                    ignoredPanBecauseZoomDisallowsPan = true;
                }
                else if (!TryConvertPixelDeltaToCellDelta(
                    input,
                    zoom,
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

            return BuildResult(
                state,
                didChangeZoom,
                didApplyPan,
                ignoredBecausePointerOverUi,
                ignoredPanBecauseZoomDisallowsPan,
                ignoredPanBecauseViewportInvalid);
        }

        // =============================================================================
        // TryConvertPixelDeltaToCellDelta
        // =============================================================================
        /// <summary>
        /// <para>
        /// Converte un delta mouse in pixel in spostamento del centro view in celle.
        /// </para>
        ///
        /// <para><b>Pan come trascinamento della mappa</b></para>
        /// <para>
        /// Se il mouse si muove verso destra tenendo premuta la rotellina, la mappa
        /// viene percepita come trascinata verso destra; quindi il centro della view
        /// deve muoversi verso sinistra. Per questo la conversione applica segno
        /// negativo al delta mouse.
        /// </para>
        /// </summary>
        private static bool TryConvertPixelDeltaToCellDelta(
            ArcGraphViewInputFrame input,
            ArcGraphViewZoomLevelDefinition zoom,
            int viewportPixelWidth,
            int viewportPixelHeight,
            out float deltaCellsX,
            out float deltaCellsY)
        {
            deltaCellsX = 0f;
            deltaCellsY = 0f;

            if (viewportPixelWidth <= 0 || viewportPixelHeight <= 0)
                return false;

            float cellsPerPixelX = zoom.VisibleCellsX / (float)viewportPixelWidth;
            float cellsPerPixelY = zoom.VisibleCellsY / (float)viewportPixelHeight;

            deltaCellsX = -input.MouseDeltaPixelsX * cellsPerPixelX;
            deltaCellsY = -input.MouseDeltaPixelsY * cellsPerPixelY;
            return true;
        }

        // =============================================================================
        // TryResolvePointerZoomAnchor
        // =============================================================================
        /// <summary>
        /// <para>
        /// Calcola la coordinata mappa continua sotto il puntatore prima dello zoom.
        /// </para>
        ///
        /// <para><b>Ancora logica, non camera Unity</b></para>
        /// <para>
        /// Il controller non legge <c>Camera</c> e non chiama
        /// <c>ScreenToWorldPoint</c>. Usa solo viewport, stato view e coordinate
        /// puntatore gia' normalizzate dal wrapper. Il risultato e' una coordinata
        /// cella continua, adatta a compensare il centro dopo il cambio livello.
        /// </para>
        /// </summary>
        private static bool TryResolvePointerZoomAnchor(
            ArcGraphMapViewConfig config,
            ArcGraphViewState state,
            ArcGraphViewInputFrame input,
            int viewportPixelWidth,
            int viewportPixelHeight,
            out float normalizedX,
            out float normalizedY,
            out float anchorCellX,
            out float anchorCellY)
        {
            normalizedX = 0f;
            normalizedY = 0f;
            anchorCellX = 0f;
            anchorCellY = 0f;

            if (!input.HasPointerScreenPosition)
                return false;

            if (!ArcGraphViewCoordinateMapper.TryNormalizeViewportPoint(
                input.PointerScreenX,
                input.PointerScreenY,
                viewportPixelWidth,
                viewportPixelHeight,
                out normalizedX,
                out normalizedY,
                out _))
            {
                return false;
            }

            ArcGraphViewCellRect visibleRect = state.ResolveVisibleCellRect(config);
            if (visibleRect.IsEmpty)
                return false;

            // Usiamo coordinate continue, non un indice intero di cella. In questo
            // modo lo zoom resta ancorato anche quando il puntatore si trova dentro
            // una cella e non esattamente sul suo angolo.
            anchorCellX = visibleRect.MinX + (normalizedX * visibleRect.Width);
            anchorCellY = visibleRect.MinY + (normalizedY * visibleRect.Height);
            return true;
        }

        // =============================================================================
        // ApplyPointerZoomAnchor
        // =============================================================================
        /// <summary>
        /// <para>
        /// Sposta il centro view dopo lo zoom per mantenere stabile il puntatore.
        /// </para>
        ///
        /// <para><b>Formula dello zoom verso puntatore</b></para>
        /// <para>
        /// Se il puntatore era al 25% della viewport, la cella ancorata deve restare
        /// al 25% anche dopo che il nuovo livello zoom ha cambiato quante celle sono
        /// visibili. Per ottenere questo risultato ricostruiamo il centro dalla
        /// cella ancorata e dalla posizione normalizzata del puntatore.
        /// </para>
        /// </summary>
        private static void ApplyPointerZoomAnchor(
            ArcGraphMapViewConfig config,
            ArcGraphViewState state,
            float normalizedX,
            float normalizedY,
            float anchorCellX,
            float anchorCellY)
        {
            ArcGraphViewZoomLevelDefinition zoom = state.CurrentZoom(config);

            int visibleCellsX = System.Math.Min(config.MapWidthCells, zoom.VisibleCellsX);
            int visibleCellsY = System.Math.Min(config.MapHeightCells, zoom.VisibleCellsY);

            float centerCellX = anchorCellX - ((normalizedX - 0.5f) * visibleCellsX);
            float centerCellY = anchorCellY - ((normalizedY - 0.5f) * visibleCellsY);

            state.SetCenterCell(centerCellX, centerCellY, config);
        }

        private static bool HasMeaningfulMouseDelta(ArcGraphViewInputFrame input)
        {
            return input.MouseDeltaPixelsX != 0f || input.MouseDeltaPixelsY != 0f;
        }

        private static ArcGraphViewControllerResult BuildResult(
            ArcGraphViewState state,
            bool didChangeZoom,
            bool didApplyPan,
            bool ignoredBecausePointerOverUi,
            bool ignoredPanBecauseZoomDisallowsPan,
            bool ignoredPanBecauseViewportInvalid)
        {
            return new ArcGraphViewControllerResult(
                didChangeZoom,
                didApplyPan,
                ignoredBecausePointerOverUi,
                ignoredPanBecauseZoomDisallowsPan,
                ignoredPanBecauseViewportInvalid,
                state.ActiveZoomLevel,
                state.CenterCellX,
                state.CenterCellY);
        }
    }
}
