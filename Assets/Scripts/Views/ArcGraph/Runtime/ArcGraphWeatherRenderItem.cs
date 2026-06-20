namespace Arcontio.View.ArcGraph
{
    // =============================================================================
    // ArcGraphWeatherRenderItem
    // =============================================================================
    /// <summary>
    /// <para>
    /// Item passivo che descrive un overlay meteo renderizzabile da ArcGraph.
    /// </para>
    ///
    /// <para><b>Principio architetturale: meteo visuale senza clima simulato</b></para>
    /// <para>
    /// L'item conserva solo dati grafici derivati da
    /// <c>ArcGraphWeatherVisualSnapshot</c>, dal contratto ambientale e dal profilo
    /// LOD. Non genera pioggia, non calcola temperatura, non aggiorna umidita', non
    /// influenza piante, acqua, pathfinding o comportamento NPC e non crea particelle
    /// Unity.
    /// </para>
    ///
    /// <para><b>Struttura interna:</b></para>
    /// <list type="bullet">
    ///   <item><b>WeatherKey</b>: tipo visuale, ad esempio rain, snow o wind.</item>
    ///   <item><b>Intensity01</b>: intensita' visuale normalizzata.</item>
    ///   <item><b>AffectedZLevel</b>: livello grafico interessato.</item>
    ///   <item><b>OverlayKey</b>: chiave overlay/atlas futura, non asset caricato.</item>
    ///   <item><b>IsAnimated</b>: indica se ArcGraph puo' scegliere frame meteo.</item>
    ///   <item><b>UsesGlobalOverlay</b>: indica che il renderer futuro lavora sopra la scena.</item>
    ///   <item><b>IsVisible/HiddenReason</b>: decisione visuale del builder.</item>
    ///   <item><b>SortKey</b>: ordinamento deterministico dell'overlay.</item>
    /// </list>
    /// </summary>
    public readonly struct ArcGraphWeatherRenderItem
    {
        public readonly string WeatherKey;
        public readonly float Intensity01;
        public readonly int AffectedZLevel;
        public readonly string OverlayKey;
        public readonly bool IsAnimated;
        public readonly bool UsesGlobalOverlay;
        public readonly bool IsVisible;
        public readonly string HiddenReason;
        public readonly ArcGraphRenderSortKey SortKey;

        // =============================================================================
        // ArcGraphWeatherRenderItem
        // =============================================================================
        /// <summary>
        /// <para>
        /// Costruisce un item overlay meteo renderizzabile.
        /// </para>
        ///
        /// <para><b>Normalizzazione difensiva</b></para>
        /// <para>
        /// Intensita' e stringhe vengono normalizzate localmente. L'item resta un
        /// payload visuale: non attiva eventi meteo, non modifica ambiente e non
        /// crea alcuna risorsa Unity.
        /// </para>
        /// </summary>
        public ArcGraphWeatherRenderItem(
            string weatherKey,
            float intensity01,
            int affectedZLevel,
            string overlayKey,
            bool isAnimated,
            bool usesGlobalOverlay,
            bool isVisible,
            string hiddenReason,
            ArcGraphRenderSortKey sortKey)
        {
            WeatherKey = weatherKey ?? string.Empty;
            Intensity01 = Clamp01(intensity01);
            AffectedZLevel = affectedZLevel;
            OverlayKey = overlayKey ?? string.Empty;
            IsAnimated = isAnimated;
            UsesGlobalOverlay = usesGlobalOverlay;
            IsVisible = isVisible;
            HiddenReason = string.IsNullOrWhiteSpace(hiddenReason) ? "None" : hiddenReason;
            SortKey = sortKey;
        }

        private static float Clamp01(float value)
        {
            if (value <= 0f)
                return 0f;

            if (value >= 1f)
                return 1f;

            return value;
        }
    }
}
