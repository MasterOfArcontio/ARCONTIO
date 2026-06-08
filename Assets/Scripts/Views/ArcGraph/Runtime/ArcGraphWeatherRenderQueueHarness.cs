using System.Collections.Generic;

namespace Arcontio.View.ArcGraph
{
    // =============================================================================
    // ArcGraphWeatherRenderQueueHarnessResult
    // =============================================================================
    /// <summary>
    /// <para>
    /// Risultato sintetico dello smoke test sulla queue meteo.
    /// </para>
    ///
    /// <para><b>Principio architetturale: verifica senza WeatherSystem e senza scena</b></para>
    /// <para>
    /// Il risultato contiene solo contatori e flag. Non contiene riferimenti a clima
    /// reale, precipitazioni produttive, particelle Unity, renderer o GameObject.
    /// Serve a validare che il Weather Renderer preparatorio trasformi snapshot in
    /// overlay visuali senza diventare un sistema ambientale.
    /// </para>
    ///
    /// <para><b>Struttura interna:</b></para>
    /// <list type="bullet">
    ///   <item><b>Passed</b>: esito complessivo.</item>
    ///   <item><b>Reason</b>: motivo sintetico.</item>
    ///   <item><b>Zoom1VisibleCount</b>: overlay visibili a zoom lontano.</item>
    ///   <item><b>Zoom1AnimatedCount</b>: overlay animabili a zoom lontano.</item>
    ///   <item><b>Zoom4VisibleCount</b>: overlay visibili a zoom vicino.</item>
    ///   <item><b>Zoom4AnimatedCount</b>: overlay animabili a zoom vicino.</item>
    ///   <item><b>InactiveHiddenCount</b>: overlay nascosti per meteo inattivo.</item>
    ///   <item><b>WrongZHiddenCount</b>: overlay nascosti per livello Z non visibile.</item>
    /// </list>
    /// </summary>
    public readonly struct ArcGraphWeatherRenderQueueHarnessResult
    {
        public readonly bool Passed;
        public readonly string Reason;
        public readonly int Zoom1VisibleCount;
        public readonly int Zoom1AnimatedCount;
        public readonly int Zoom4VisibleCount;
        public readonly int Zoom4AnimatedCount;
        public readonly int InactiveHiddenCount;
        public readonly int WrongZHiddenCount;

        public ArcGraphWeatherRenderQueueHarnessResult(
            bool passed,
            string reason,
            int zoom1VisibleCount,
            int zoom1AnimatedCount,
            int zoom4VisibleCount,
            int zoom4AnimatedCount,
            int inactiveHiddenCount,
            int wrongZHiddenCount)
        {
            Passed = passed;
            Reason = string.IsNullOrWhiteSpace(reason) ? "None" : reason;
            Zoom1VisibleCount = zoom1VisibleCount;
            Zoom1AnimatedCount = zoom1AnimatedCount;
            Zoom4VisibleCount = zoom4VisibleCount;
            Zoom4AnimatedCount = zoom4AnimatedCount;
            InactiveHiddenCount = inactiveHiddenCount;
            WrongZHiddenCount = wrongZHiddenCount;
        }
    }

    // =============================================================================
    // ArcGraphWeatherRenderQueueHarness
    // =============================================================================
    /// <summary>
    /// <para>
    /// Harness statico per validare la queue meteo passiva.
    /// </para>
    ///
    /// <para><b>Principio architetturale: render preparatorio, non sistema meteo</b></para>
    /// <para>
    /// Lo smoke test costruisce un layer meteo in memoria, vi inserisce snapshot
    /// visuali gia' decisi e verifica il builder a due LOD. Non genera meteo, non
    /// aggiorna clima e non apre scene Unity.
    /// </para>
    ///
    /// <para><b>Struttura interna:</b></para>
    /// <list type="bullet">
    ///   <item><b>RunDefaultSmoke</b>: scenario minimo con zoom 1 e zoom 4.</item>
    ///   <item><b>CreateRainSnapshot</b>: snapshot meteo attivo.</item>
    /// </list>
    /// </summary>
    public static class ArcGraphWeatherRenderQueueHarness
    {
        // =============================================================================
        // RunDefaultSmoke
        // =============================================================================
        /// <summary>
        /// <para>
        /// Esegue lo smoke test default della queue meteo.
        /// </para>
        ///
        /// <para><b>Scenario minimo</b></para>
        /// <para>
        /// Lo scenario contiene pioggia attiva sul livello visibile, meteo inattivo
        /// e neve attiva su un livello Z diverso. A zoom 1 l'overlay e' visibile ma
        /// non animato; a zoom 4 lo stesso overlay puo' essere animato da ArcGraph.
        /// </para>
        /// </summary>
        public static ArcGraphWeatherRenderQueueHarnessResult RunDefaultSmoke()
        {
            var layer = new ArcGraphWeatherLayer();
            var config = ArcGraphMapViewConfig.CreateDefaultV033();
            var zoom1 = ArcGraphZoomLodPolicy.ResolveFromZoom(config.ResolveZoomLevel(1));
            var zoom4 = ArcGraphZoomLodPolicy.ResolveFromZoom(config.ResolveZoomLevel(4));
            var builder = new ArcGraphWeatherRenderQueueBuilder();

            layer.ReplaceSnapshot(CreateRainSnapshot());

            var zoom1Items = new List<ArcGraphWeatherRenderItem>();
            ArcGraphWeatherRenderQueueDiagnostics zoom1Diagnostics = builder.Build(
                layer,
                zoom1,
                zoom1Items);

            var zoom4Items = new List<ArcGraphWeatherRenderItem>();
            ArcGraphWeatherRenderQueueDiagnostics zoom4Diagnostics = builder.Build(
                layer,
                zoom4,
                zoom4Items);

            layer.ReplaceSnapshot(ArcGraphWeatherVisualSnapshot.None());
            var inactiveItems = new List<ArcGraphWeatherRenderItem>();
            ArcGraphWeatherRenderQueueDiagnostics inactiveDiagnostics = builder.Build(
                layer,
                zoom4,
                inactiveItems,
                includeHiddenItems: true);

            layer.ReplaceSnapshot(new ArcGraphWeatherVisualSnapshot(
                weatherKey: "snow",
                intensity01: 1f,
                affectedZLevel: 1,
                isActive: true));

            var wrongZItems = new List<ArcGraphWeatherRenderItem>();
            ArcGraphWeatherRenderQueueDiagnostics wrongZDiagnostics = builder.Build(
                layer,
                zoom4,
                wrongZItems,
                visibleZLevel: ArcGraphZLevelPolicy.DefaultVisibleZLevel,
                includeHiddenItems: true);

            bool passed = zoom1Diagnostics.HasSnapshot
                          && zoom1Diagnostics.ActiveSnapshotCount == 1
                          && zoom1Diagnostics.VisibleItemCount == 1
                          && zoom1Diagnostics.AnimatedItemCount == 0
                          && zoom1Items.Count == 1
                          && zoom1Items[0].UsesSimplifiedRepresentation
                          && zoom4Diagnostics.VisibleItemCount == 1
                          && zoom4Diagnostics.AnimatedItemCount == 1
                          && zoom4Items.Count == 1
                          && !zoom4Items[0].UsesSimplifiedRepresentation
                          && inactiveDiagnostics.HiddenItemCount == 1
                          && inactiveItems.Count == 1
                          && inactiveItems[0].HiddenReason == "WeatherInactive"
                          && wrongZDiagnostics.HiddenItemCount == 1
                          && wrongZItems.Count == 1
                          && wrongZItems[0].HiddenReason == "WeatherZLevelHidden";

            return new ArcGraphWeatherRenderQueueHarnessResult(
                passed,
                passed ? "WeatherRenderQueueSmokePassed" : "WeatherRenderQueueSmokeFailed",
                zoom1Diagnostics.VisibleItemCount,
                zoom1Diagnostics.AnimatedItemCount,
                zoom4Diagnostics.VisibleItemCount,
                zoom4Diagnostics.AnimatedItemCount,
                inactiveDiagnostics.HiddenItemCount,
                wrongZDiagnostics.HiddenItemCount);
        }

        private static ArcGraphWeatherVisualSnapshot CreateRainSnapshot()
        {
            return new ArcGraphWeatherVisualSnapshot(
                weatherKey: "rain",
                intensity01: 0.8f,
                affectedZLevel: ArcGraphZLevelPolicy.DefaultVisibleZLevel,
                isActive: true);
        }
    }
}
