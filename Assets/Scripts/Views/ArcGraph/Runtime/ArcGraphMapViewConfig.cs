namespace Arcontio.View.ArcGraph
{
    // =============================================================================
    // ArcGraphMapViewConfig
    // =============================================================================
    /// <summary>
    /// <para>
    /// Configurazione passiva della camera ArcGraph.
    /// </para>
    ///
    /// <para><b>Principio architetturale: zoom camera semplice</b></para>
    /// <para>
    /// ArcGraph non usa piu' livelli zoom discreti. La configurazione descrive solo
    /// dimensione mappa e parametri continui della camera ortografica: valore
    /// iniziale, minimo, massimo, passo rotellina, tempo di ammorbidimento e policy
    /// del pan. La scelta di sprite, LOD o animazioni non dipende piu' dallo zoom.
    /// </para>
    ///
    /// <para><b>Struttura interna:</b></para>
    /// <list type="bullet">
    ///   <item><b>MapWidthCells/MapHeightCells</b>: dimensione logica della mappa.</item>
    ///   <item><b>DefaultOrthographicSize</b>: zoom iniziale della camera.</item>
    ///   <item><b>MinOrthographicSize/MaxOrthographicSize</b>: limiti fisici dello zoom continuo.</item>
    ///   <item><b>ZoomStep</b>: variazione per scatto rotellina.</item>
    ///   <item><b>ZoomSmoothTime</b>: tempo di smoothing continuo della camera.</item>
    ///   <item><b>PanUsesMiddleMouseButton</b>: policy input per il pan.</item>
    /// </list>
    /// </summary>
    public sealed class ArcGraphMapViewConfig
    {
        public int MapWidthCells { get; }
        public int MapHeightCells { get; }
        public float DefaultOrthographicSize { get; }
        public float MinOrthographicSize { get; }
        public float MaxOrthographicSize { get; }
        public float ZoomStep { get; }
        public float ZoomSmoothTime { get; }
        public bool PanUsesMiddleMouseButton { get; }

        // =============================================================================
        // ArcGraphMapViewConfig
        // =============================================================================
        /// <summary>
        /// <para>
        /// Costruisce una configurazione camera continua normalizzata.
        /// </para>
        /// </summary>
        public ArcGraphMapViewConfig(
            int mapWidthCells,
            int mapHeightCells,
            float defaultOrthographicSize = 75f,
            float minOrthographicSize = 8f,
            float maxOrthographicSize = 150f,
            float zoomStep = 8f,
            float zoomSmoothTime = 0.20f,
            bool panUsesMiddleMouseButton = true)
        {
            MapWidthCells = mapWidthCells > 0 ? mapWidthCells : 1;
            MapHeightCells = mapHeightCells > 0 ? mapHeightCells : 1;
            MinOrthographicSize = minOrthographicSize > 0f ? minOrthographicSize : 1f;
            MaxOrthographicSize = maxOrthographicSize >= MinOrthographicSize
                ? maxOrthographicSize
                : MinOrthographicSize;
            DefaultOrthographicSize = Clamp(
                defaultOrthographicSize > 0f ? defaultOrthographicSize : 75f,
                MinOrthographicSize,
                MaxOrthographicSize);
            ZoomStep = zoomStep > 0f ? zoomStep : 1f;
            ZoomSmoothTime = zoomSmoothTime >= 0f ? zoomSmoothTime : 0.20f;
            PanUsesMiddleMouseButton = panUsesMiddleMouseButton;
        }

        // =============================================================================
        // CreateDefaultV033
        // =============================================================================
        /// <summary>
        /// <para>
        /// Crea il fallback runtime senza livelli zoom.
        /// </para>
        /// </summary>
        public static ArcGraphMapViewConfig CreateDefaultV033()
        {
            return new ArcGraphMapViewConfig(
                250,
                250,
                defaultOrthographicSize: 75f,
                minOrthographicSize: 8f,
                maxOrthographicSize: 150f,
                zoomStep: 8f,
                zoomSmoothTime: 0.20f,
                panUsesMiddleMouseButton: true);
        }

        // =============================================================================
        // WithMapDimensions
        // =============================================================================
        /// <summary>
        /// <para>
        /// Crea una copia della configurazione usando nuove dimensioni mappa.
        /// </para>
        /// </summary>
        public ArcGraphMapViewConfig WithMapDimensions(
            int mapWidthCells,
            int mapHeightCells)
        {
            return new ArcGraphMapViewConfig(
                mapWidthCells,
                mapHeightCells,
                DefaultOrthographicSize,
                MinOrthographicSize,
                MaxOrthographicSize,
                ZoomStep,
                ZoomSmoothTime,
                PanUsesMiddleMouseButton);
        }

        private static float Clamp(float value, float min, float max)
        {
            if (value < min) return min;
            if (value > max) return max;
            return value;
        }
    }
}
