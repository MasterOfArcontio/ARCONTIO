using System.Collections.Generic;

namespace Arcontio.View.ArcGraph
{
    // =============================================================================
    // ArcGraphWeatherRenderQueueBuilder
    // =============================================================================
    /// <summary>
    /// <para>
    /// Builder passivo che trasforma lo snapshot meteo ArcGraph in overlay
    /// renderizzabile.
    /// </para>
    ///
    /// <para><b>Principio architetturale: meteo renderizzabile senza WeatherSystem</b></para>
    /// <para>
    /// Il builder legge solo <c>ArcGraphWeatherLayer</c>, il contratto ambientale
    /// visuale e un profilo LOD gia' risolto. Produce al massimo un item overlay
    /// value-only per un futuro renderer, ma non genera pioggia, non accumula neve,
    /// non muove vento, non aggiorna temperatura/umidita' e non crea particelle.
    /// </para>
    ///
    /// <para><b>Struttura interna:</b></para>
    /// <list type="bullet">
    ///   <item><b>Build</b>: popola una lista overlay con zero o un item meteo.</item>
    ///   <item><b>CreateItem</b>: converte snapshot + LOD + contratto in item value-only.</item>
    ///   <item><b>ResolveOverlayKey</b>: produce chiave testuale, non asset reale.</item>
    ///   <item><b>ResolveStableOverlayId</b>: crea id deterministico per sort key.</item>
    /// </list>
    /// </summary>
    public sealed class ArcGraphWeatherRenderQueueBuilder
    {
        private const int WeatherVisualLayerOrder = 100;

        // =============================================================================
        // Build
        // =============================================================================
        /// <summary>
        /// <para>
        /// Costruisce una queue meteo a partire dal layer meteo ArcGraph.
        /// </para>
        ///
        /// <para><b>Output controllato</b></para>
        /// <para>
        /// Il metodo legge il solo snapshot corrente, applica contratto e LOD, poi
        /// produce un item nel target se visibile. Gli item nascosti possono essere
        /// esclusi dal target ma restano contati nella diagnostica.
        /// </para>
        ///
        /// <para><b>Struttura interna:</b></para>
        /// <list type="bullet">
        ///   <item><b>weatherLayer</b>: cache snapshot globale da leggere.</item>
        ///   <item><b>lodProfile</b>: policy visuale gia' risolta.</item>
        ///   <item><b>visibleZLevel</b>: livello Z correntemente renderizzato.</item>
        ///   <item><b>target</b>: lista render item da popolare.</item>
        ///   <item><b>includeHiddenItems</b>: se true, conserva anche item nascosti.</item>
        /// </list>
        /// </summary>
        public ArcGraphWeatherRenderQueueDiagnostics Build(
            ArcGraphWeatherLayer weatherLayer,
            ArcGraphZoomLodProfile lodProfile,
            IList<ArcGraphWeatherRenderItem> target,
            int visibleZLevel = ArcGraphZLevelPolicy.DefaultVisibleZLevel,
            bool clearTarget = true,
            bool includeHiddenItems = false)
        {
            if (target == null)
            {
                return new ArcGraphWeatherRenderQueueDiagnostics(
                    hasSnapshot: false,
                    activeSnapshotCount: 0,
                    visibleItemCount: 0,
                    hiddenItemCount: 0,
                    animatedItemCount: 0,
                    maxIntensity01: 0f,
                    reason: "TargetMissing");
            }

            if (clearTarget)
                target.Clear();

            if (weatherLayer == null)
            {
                return new ArcGraphWeatherRenderQueueDiagnostics(
                    hasSnapshot: false,
                    activeSnapshotCount: 0,
                    visibleItemCount: 0,
                    hiddenItemCount: 0,
                    animatedItemCount: 0,
                    maxIntensity01: 0f,
                    reason: "WeatherLayerMissing");
            }

            if (!ArcGraphEnvironmentVisualContractCatalog.TryGetDefaultContract(
                    ArcGraphLayerId.Weather,
                    out var contract))
            {
                return new ArcGraphWeatherRenderQueueDiagnostics(
                    hasSnapshot: weatherLayer.HasWeatherSnapshot,
                    activeSnapshotCount: 0,
                    visibleItemCount: 0,
                    hiddenItemCount: 0,
                    animatedItemCount: 0,
                    maxIntensity01: 0f,
                    reason: "WeatherContractMissing");
            }

            ArcGraphWeatherRenderItem item = CreateItem(
                weatherLayer.CurrentWeather,
                weatherLayer.HasWeatherSnapshot,
                lodProfile,
                contract,
                visibleZLevel);

            int activeSnapshotCount = weatherLayer.HasWeatherSnapshot && weatherLayer.CurrentWeather.IsActive ? 1 : 0;
            int visibleCount = item.IsVisible ? 1 : 0;
            int hiddenCount = item.IsVisible ? 0 : 1;
            int animatedCount = item.IsVisible && item.IsAnimated ? 1 : 0;

            if (item.IsVisible || includeHiddenItems)
                target.Add(item);

            return new ArcGraphWeatherRenderQueueDiagnostics(
                weatherLayer.HasWeatherSnapshot,
                activeSnapshotCount,
                visibleCount,
                hiddenCount,
                animatedCount,
                item.Intensity01,
                item.IsVisible ? "WeatherQueueBuilt" : item.HiddenReason);
        }

        private static ArcGraphWeatherRenderItem CreateItem(
            ArcGraphWeatherVisualSnapshot snapshot,
            bool hasSnapshot,
            ArcGraphZoomLodProfile lodProfile,
            ArcGraphEnvironmentVisualLayerContract contract,
            int visibleZLevel)
        {
            bool isVisible = true;
            string hiddenReason = "None";

            if (!contract.IsEnvironmentLayer || contract.LayerId != ArcGraphLayerId.Weather)
            {
                isVisible = false;
                hiddenReason = "InvalidWeatherContract";
            }
            else if (!contract.RequiresExternalSnapshots)
            {
                isVisible = false;
                hiddenReason = "WeatherMustUseExternalSnapshots";
            }
            else if (!contract.AllowsGlobalOverlay)
            {
                isVisible = false;
                hiddenReason = "WeatherGlobalOverlayNotAllowed";
            }
            else if (!hasSnapshot)
            {
                isVisible = false;
                hiddenReason = "WeatherSnapshotMissing";
            }
            else if (!snapshot.IsActive)
            {
                isVisible = false;
                hiddenReason = "WeatherInactive";
            }
            else if (string.IsNullOrWhiteSpace(snapshot.WeatherKey))
            {
                isVisible = false;
                hiddenReason = "MissingWeatherKey";
            }
            else if (snapshot.Intensity01 <= 0f)
            {
                isVisible = false;
                hiddenReason = "EmptyWeatherIntensity";
            }
            else if (snapshot.AffectedZLevel != visibleZLevel)
            {
                isVisible = false;
                hiddenReason = "WeatherZLevelHidden";
            }
            else if (!lodProfile.ShowWeatherOverlay)
            {
                isVisible = false;
                hiddenReason = "WeatherOverlayHiddenByLod";
            }

            bool allowsAnimation = isVisible
                                   && contract.AllowsArcGraphSpriteAnimation
                                   && lodProfile.AllowsSpriteAnimation;

            string overlayKey = isVisible
                ? ResolveOverlayKey(snapshot, lodProfile)
                : string.Empty;

            var sortKey = ArcGraphRenderSortKey.FromCell(
                new ArcGraphCellCoord(0, 0, visibleZLevel),
                WeatherVisualLayerOrder,
                ArcGraphRenderItemKind.Weather,
                ResolveStableOverlayId(snapshot));

            return new ArcGraphWeatherRenderItem(
                snapshot.WeatherKey,
                snapshot.Intensity01,
                snapshot.AffectedZLevel,
                overlayKey,
                allowsAnimation,
                contract.AllowsGlobalOverlay,
                isVisible,
                hiddenReason,
                sortKey);
        }

        private static string ResolveOverlayKey(
            ArcGraphWeatherVisualSnapshot snapshot,
            ArcGraphZoomLodProfile lodProfile)
        {
            string weather = SanitizeKeyPart(snapshot.WeatherKey);

            return "ArcGraph/Weather/" + weather;
        }

        private static int ResolveStableOverlayId(ArcGraphWeatherVisualSnapshot snapshot)
        {
            unchecked
            {
                int hash = 29;
                hash = (hash * 31) + snapshot.AffectedZLevel;
                hash = (hash * 31) + (int)(snapshot.Intensity01 * 1000f);
                hash = (hash * 31) + StableStringHash(SanitizeKeyPart(snapshot.WeatherKey));
                return hash & int.MaxValue;
            }
        }

        private static int StableStringHash(string value)
        {
            unchecked
            {
                int hash = 31;
                if (string.IsNullOrEmpty(value))
                    return hash;

                for (int i = 0; i < value.Length; i++)
                    hash = (hash * 31) + value[i];

                return hash;
            }
        }

        private static string SanitizeKeyPart(string value)
        {
            if (string.IsNullOrWhiteSpace(value))
                return "unknown";

            return value.Trim().Replace(' ', '_');
        }
    }
}
