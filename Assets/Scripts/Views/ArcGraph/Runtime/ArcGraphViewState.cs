using System;

namespace Arcontio.View.ArcGraph
{
    // =============================================================================
    // ArcGraphViewState
    // =============================================================================
    /// <summary>
    /// <para>
    /// Stato grafico della finestra mappa ArcGraph.
    /// </para>
    ///
    /// <para><b>Principio architetturale: stato vista, non stato simulativo</b></para>
    /// <para>
    /// Questo oggetto conserva centro vista e zoom attivo. Non possiede celle,
    /// NPC, oggetti o dati di mondo; non legge input fisico e non muove camera
    /// Unity. Il futuro controller potra' aggiornarlo e poi applicare lo stato a
    /// una camera esplicita.
    /// </para>
    ///
    /// <para><b>Struttura interna:</b></para>
    /// <list type="bullet">
    ///   <item><b>CenterCellX/Y</b>: centro visuale espresso in coordinate cella.</item>
    ///   <item><b>ActiveZoomLevel</b>: livello zoom discreto corrente.</item>
    ///   <item><b>SetZoomLevel</b>: cambia livello preservando e clampando il centro.</item>
    ///   <item><b>ApplyPanCells</b>: sposta il centro se lo zoom consente pan.</item>
    ///   <item><b>ResolveVisibleCellRect</b>: calcola la finestra celle effettiva.</item>
    /// </list>
    /// </summary>
    public sealed class ArcGraphViewState
    {
        public float CenterCellX { get; private set; }
        public float CenterCellY { get; private set; }
        public int ActiveZoomLevel { get; private set; }

        // =============================================================================
        // ArcGraphViewState
        // =============================================================================
        /// <summary>
        /// <para>
        /// Costruisce uno stato vista normalizzato rispetto alla configurazione.
        /// </para>
        ///
        /// <para><b>Centro controllato</b></para>
        /// <para>
        /// Se il livello zoom mostra piu' celle della mappa, il centro viene forzato
        /// al centro mappa e il pan risulta inutile. Questo copre il caso deciso per
        /// zoom 1, dove <c>300x300</c> celle visibili coprono una mappa
        /// <c>250x250</c>.
        /// </para>
        /// </summary>
        public ArcGraphViewState(
            ArcGraphMapViewConfig config,
            float centerCellX,
            float centerCellY,
            int activeZoomLevel)
        {
            config = config ?? ArcGraphMapViewConfig.CreateDefaultV033();

            CenterCellX = centerCellX;
            CenterCellY = centerCellY;
            ActiveZoomLevel = config.ResolveZoomLevel(activeZoomLevel).Level;

            ClampCenterToMap(config);
        }

        // =============================================================================
        // CreateDefault
        // =============================================================================
        /// <summary>
        /// <para>
        /// Crea uno stato vista centrato sulla mappa.
        /// </para>
        ///
        /// <para><b>Default v0.33</b></para>
        /// <para>
        /// Il default usa il livello zoom iniziale della configurazione e punta al
        /// centro geometrico della mappa, senza assumere una camera Unity gia'
        /// presente in scena.
        /// </para>
        /// </summary>
        public static ArcGraphViewState CreateDefault(ArcGraphMapViewConfig config)
        {
            config = config ?? ArcGraphMapViewConfig.CreateDefaultV033();
            return new ArcGraphViewState(
                config,
                config.MapWidthCells * 0.5f,
                config.MapHeightCells * 0.5f,
                config.DefaultZoomLevel);
        }

        // =============================================================================
        // CurrentZoom
        // =============================================================================
        /// <summary>
        /// <para>
        /// Restituisce la definizione zoom attiva.
        /// </para>
        ///
        /// <para><b>Risoluzione tramite config</b></para>
        /// <para>
        /// Lo stato conserva solo il numero livello. La configurazione resta la fonte
        /// della dimensione visibile e della policy LOD associata.
        /// </para>
        /// </summary>
        public ArcGraphViewZoomLevelDefinition CurrentZoom(ArcGraphMapViewConfig config)
        {
            config = config ?? ArcGraphMapViewConfig.CreateDefaultV033();
            return config.ResolveZoomLevel(ActiveZoomLevel);
        }

        // =============================================================================
        // SetZoomLevel
        // =============================================================================
        /// <summary>
        /// <para>
        /// Cambia livello zoom e normalizza il centro vista.
        /// </para>
        ///
        /// <para><b>Zoom come stato visuale</b></para>
        /// <para>
        /// Il cambio zoom non modifica layer, dirty state, world, NPC o oggetti.
        /// Aggiorna solo la finestra visuale che il futuro controller applichera'
        /// alla camera.
        /// </para>
        /// </summary>
        public void SetZoomLevel(int zoomLevel, ArcGraphMapViewConfig config)
        {
            config = config ?? ArcGraphMapViewConfig.CreateDefaultV033();
            ActiveZoomLevel = config.ResolveZoomLevel(zoomLevel).Level;
            ClampCenterToMap(config);
        }

        // =============================================================================
        // SetCenterCell
        // =============================================================================
        /// <summary>
        /// <para>
        /// Imposta direttamente il centro vista in coordinate cella.
        /// </para>
        ///
        /// <para><b>Principio architetturale: centro view esplicito</b></para>
        /// <para>
        /// Questo metodo resta nel solo layer view. Serve ai controller grafici per
        /// compensare operazioni come lo zoom verso puntatore, dove il centro deve
        /// cambiare per mantenere stabile la cella sotto il mouse. Non modifica
        /// mappa, NPC, oggetti, job o stato simulativo.
        /// </para>
        /// </summary>
        public void SetCenterCell(
            float centerCellX,
            float centerCellY,
            ArcGraphMapViewConfig config)
        {
            config = config ?? ArcGraphMapViewConfig.CreateDefaultV033();

            CenterCellX = centerCellX;
            CenterCellY = centerCellY;
            ClampCenterToMap(config);
        }

        // =============================================================================
        // ApplyWheelZoom
        // =============================================================================
        /// <summary>
        /// <para>
        /// Applica uno spostamento zoom derivato dalla rotellina.
        /// </para>
        ///
        /// <para><b>Un frame input, una transizione discreta</b></para>
        /// <para>
        /// Il metodo riceve un delta logico gia' convertito. Uno scatto puo'
        /// spostare di un livello, secondo la policy di configurazione. Non legge
        /// dispositivi e non cambia camera Unity.
        /// </para>
        /// </summary>
        public void ApplyWheelZoom(int wheelStepDelta, ArcGraphMapViewConfig config)
        {
            config = config ?? ArcGraphMapViewConfig.CreateDefaultV033();
            var target = config.ResolveZoomLevelFromWheel(ActiveZoomLevel, wheelStepDelta);
            SetZoomLevel(target.Level, config);
        }

        // =============================================================================
        // ApplyPanCells
        // =============================================================================
        /// <summary>
        /// <para>
        /// Sposta il centro vista in coordinate cella.
        /// </para>
        ///
        /// <para><b>Pan come movimento della finestra, non della mappa</b></para>
        /// <para>
        /// Il pan non sposta oggetti, NPC o celle. Cambia solo il centro della view.
        /// Se il livello zoom corrente vieta pan, il metodo non modifica nulla.
        /// </para>
        /// </summary>
        public bool ApplyPanCells(
            float deltaCellsX,
            float deltaCellsY,
            ArcGraphMapViewConfig config)
        {
            config = config ?? ArcGraphMapViewConfig.CreateDefaultV033();
            var zoom = config.ResolveZoomLevel(ActiveZoomLevel);

            if (!zoom.AllowsPan)
                return false;

            CenterCellX += deltaCellsX;
            CenterCellY += deltaCellsY;
            ClampCenterToMap(config);
            return true;
        }

        // =============================================================================
        // ResolveVisibleCellRect
        // =============================================================================
        /// <summary>
        /// <para>
        /// Calcola il rettangolo celle effettivamente visibile.
        /// </para>
        ///
        /// <para><b>Clamp alla mappa reale</b></para>
        /// <para>
        /// Il livello zoom puo' chiedere piu' celle di quante la mappa contenga.
        /// In quel caso il rettangolo restituito copre l'intera mappa disponibile,
        /// senza produrre coordinate negative o fuori bounds.
        /// </para>
        /// </summary>
        public ArcGraphViewCellRect ResolveVisibleCellRect(ArcGraphMapViewConfig config)
        {
            config = config ?? ArcGraphMapViewConfig.CreateDefaultV033();
            var zoom = config.ResolveZoomLevel(ActiveZoomLevel);

            int width = Math.Min(config.MapWidthCells, zoom.VisibleCellsX);
            int height = Math.Min(config.MapHeightCells, zoom.VisibleCellsY);

            int minX = FloorToInt(CenterCellX - (width * 0.5f));
            int minY = FloorToInt(CenterCellY - (height * 0.5f));

            minX = ClampInt(minX, 0, config.MapWidthCells - width);
            minY = ClampInt(minY, 0, config.MapHeightCells - height);

            return new ArcGraphViewCellRect(
                minX,
                minY,
                minX + width,
                minY + height);
        }

        private void ClampCenterToMap(ArcGraphMapViewConfig config)
        {
            var zoom = config.ResolveZoomLevel(ActiveZoomLevel);

            CenterCellX = ClampCenterAxis(
                CenterCellX,
                config.MapWidthCells,
                zoom.VisibleCellsX);

            CenterCellY = ClampCenterAxis(
                CenterCellY,
                config.MapHeightCells,
                zoom.VisibleCellsY);
        }

        private static float ClampCenterAxis(
            float current,
            int mapCells,
            int visibleCells)
        {
            if (mapCells <= 0)
                return 0f;

            if (visibleCells >= mapCells)
                return mapCells * 0.5f;

            float halfVisible = visibleCells * 0.5f;
            float min = halfVisible;
            float max = mapCells - halfVisible;

            if (current < min) return min;
            if (current > max) return max;
            return current;
        }

        private static int FloorToInt(float value)
        {
            return (int)Math.Floor(value);
        }

        private static int ClampInt(int value, int min, int max)
        {
            if (max < min)
                return min;

            if (value < min) return min;
            if (value > max) return max;
            return value;
        }
    }
}
