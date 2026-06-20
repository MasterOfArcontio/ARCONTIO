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
    ///   <item><b>VisibleCount</b>: overlay visibili nel profilo full-detail.</item>
    ///   <item><b>AnimatedCount</b>: overlay animabili nel profilo full-detail.</item>
    ///   <item><b>InactiveHiddenCount</b>: overlay nascosti per meteo inattivo.</item>
    ///   <item><b>WrongZHiddenCount</b>: overlay nascosti per livello Z non visibile.</item>
    /// </list>
    /// </summary>
    public readonly struct ArcGraphWeatherRenderQueueHarnessResult
    {
        public readonly bool Passed;
        public readonly string Reason;
        public readonly int VisibleCount;
        public readonly int AnimatedCount;
        public readonly int InactiveHiddenCount;
        public readonly int WrongZHiddenCount;

        public ArcGraphWeatherRenderQueueHarnessResult(
            bool passed,
            string reason,
            int visibleCount,
            int animatedCount,
            int inactiveHiddenCount,
            int wrongZHiddenCount)
        {
            Passed = passed;
            Reason = string.IsNullOrWhiteSpace(reason) ? "None" : reason;
            VisibleCount = visibleCount;
            AnimatedCount = animatedCount;
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
    ///   <item><b>RunDefaultSmoke</b>: scenario minimo con profilo full-detail.</item>
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
        /// e neve attiva su un livello Z diverso. Nel profilo full-detail
        /// l'overlay visibile puo' essere animato da ArcGraph.
        /// </para>
        /// </summary>
        public static ArcGraphWeatherRenderQueueHarnessResult RunDefaultSmoke()
        {
            var layer = new ArcGraphWeatherLayer();
            ArcGraphZoomLodProfile profile = ArcGraphZoomLodPolicy.ResolveFullDetail();
            var builder = new ArcGraphWeatherRenderQueueBuilder();

            layer.ReplaceSnapshot(CreateRainSnapshot());

            var items = new List<ArcGraphWeatherRenderItem>();
            ArcGraphWeatherRenderQueueDiagnostics diagnostics = builder.Build(
                layer,
                profile,
                items);

            layer.ReplaceSnapshot(ArcGraphWeatherVisualSnapshot.None());
            var inactiveItems = new List<ArcGraphWeatherRenderItem>();
            ArcGraphWeatherRenderQueueDiagnostics inactiveDiagnostics = builder.Build(
                layer,
                profile,
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
                profile,
                wrongZItems,
                visibleZLevel: ArcGraphZLevelPolicy.DefaultVisibleZLevel,
                includeHiddenItems: true);

            bool passed = diagnostics.HasSnapshot
                          && diagnostics.ActiveSnapshotCount == 1
                          && diagnostics.VisibleItemCount == 1
                          && diagnostics.AnimatedItemCount == 1
                          && items.Count == 1
                          && inactiveDiagnostics.HiddenItemCount == 1
                          && inactiveItems.Count == 1
                          && inactiveItems[0].HiddenReason == "WeatherInactive"
                          && wrongZDiagnostics.HiddenItemCount == 1
                          && wrongZItems.Count == 1
                          && wrongZItems[0].HiddenReason == "WeatherZLevelHidden";

            return new ArcGraphWeatherRenderQueueHarnessResult(
                passed,
                passed ? "WeatherRenderQueueSmokePassed" : "WeatherRenderQueueSmokeFailed",
                diagnostics.VisibleItemCount,
                diagnostics.AnimatedItemCount,
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
