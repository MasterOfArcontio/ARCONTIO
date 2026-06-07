using System.Collections.Generic;

namespace Arcontio.View.ArcGraph
{
    // =============================================================================
    // ArcGraphVegetationRenderQueueHarnessResult
    // =============================================================================
    /// <summary>
    /// <para>
    /// Risultato sintetico dello smoke test sulla queue vegetazione.
    /// </para>
    ///
    /// <para><b>Principio architetturale: verifica senza biosfera e senza scena</b></para>
    /// <para>
    /// Il risultato contiene solo contatori e flag. Non contiene riferimenti a
    /// piante runtime, seed bank, asset, sprite Unity, renderer o GameObject. Serve
    /// a validare che il Vegetation Renderer preparatorio trasformi snapshot in
    /// item visuali senza diventare un sistema ambientale.
    /// </para>
    ///
    /// <para><b>Struttura interna:</b></para>
    /// <list type="bullet">
    ///   <item><b>Passed</b>: esito complessivo.</item>
    ///   <item><b>Reason</b>: motivo sintetico.</item>
    ///   <item><b>Zoom1VisibleCount</b>: item visibili a zoom lontano.</item>
    ///   <item><b>Zoom1AggregatedCount</b>: item aggregati a zoom lontano.</item>
    ///   <item><b>Zoom4VisibleCount</b>: item visibili a zoom vicino.</item>
    ///   <item><b>Zoom4AnimatedCount</b>: item animabili a zoom vicino.</item>
    ///   <item><b>HiddenCount</b>: item nascosti per snapshot non validi.</item>
    /// </list>
    /// </summary>
    public readonly struct ArcGraphVegetationRenderQueueHarnessResult
    {
        public readonly bool Passed;
        public readonly string Reason;
        public readonly int Zoom1VisibleCount;
        public readonly int Zoom1AggregatedCount;
        public readonly int Zoom4VisibleCount;
        public readonly int Zoom4AnimatedCount;
        public readonly int HiddenCount;

        public ArcGraphVegetationRenderQueueHarnessResult(
            bool passed,
            string reason,
            int zoom1VisibleCount,
            int zoom1AggregatedCount,
            int zoom4VisibleCount,
            int zoom4AnimatedCount,
            int hiddenCount)
        {
            Passed = passed;
            Reason = string.IsNullOrWhiteSpace(reason) ? "None" : reason;
            Zoom1VisibleCount = zoom1VisibleCount;
            Zoom1AggregatedCount = zoom1AggregatedCount;
            Zoom4VisibleCount = zoom4VisibleCount;
            Zoom4AnimatedCount = zoom4AnimatedCount;
            HiddenCount = hiddenCount;
        }
    }

    // =============================================================================
    // ArcGraphVegetationRenderQueueHarness
    // =============================================================================
    /// <summary>
    /// <para>
    /// Harness statico per validare la queue vegetazione passiva.
    /// </para>
    ///
    /// <para><b>Principio architetturale: render preparatorio, non crescita piante</b></para>
    /// <para>
    /// Lo smoke test costruisce un layer vegetazione in memoria, vi inserisce
    /// snapshot visuali gia' decisi e verifica il builder a due LOD. Non genera
    /// piante, non calcola condizioni ambientali e non crea asset o scena.
    /// </para>
    ///
    /// <para><b>Struttura interna:</b></para>
    /// <list type="bullet">
    ///   <item><b>RunDefaultSmoke</b>: scenario minimo con zoom 1 e zoom 4.</item>
    ///   <item><b>CreateVegetationSnapshots</b>: due snapshot validi e uno nascosto.</item>
    /// </list>
    /// </summary>
    public static class ArcGraphVegetationRenderQueueHarness
    {
        // =============================================================================
        // RunDefaultSmoke
        // =============================================================================
        /// <summary>
        /// <para>
        /// Esegue lo smoke test default della queue vegetazione.
        /// </para>
        ///
        /// <para><b>Scenario minimo</b></para>
        /// <para>
        /// Lo scenario contiene erba e arbusto validi, piu' uno snapshot senza
        /// species key che deve restare nascosto. A zoom 1 gli item visibili sono
        /// aggregati; a zoom 4 gli stessi item possono essere animati da ArcGraph.
        /// </para>
        /// </summary>
        public static ArcGraphVegetationRenderQueueHarnessResult RunDefaultSmoke()
        {
            var renderState = new ArcGraphRenderState(
                visibleZLevel: ArcGraphZLevelPolicy.DefaultVisibleZLevel,
                tileSizeWorld: 1f,
                chunkSizeCells: 4);

            var layer = new ArcGraphVegetationLayer();
            layer.Initialize(renderState);
            layer.ReplaceSnapshots(CreateVegetationSnapshots(), renderState);

            var config = ArcGraphMapViewConfig.CreateDefaultV033();
            var zoom1 = ArcGraphZoomLodPolicy.ResolveFromZoom(config.ResolveZoomLevel(1));
            var zoom4 = ArcGraphZoomLodPolicy.ResolveFromZoom(config.ResolveZoomLevel(4));

            var builder = new ArcGraphVegetationRenderQueueBuilder();
            var zoom1Items = new List<ArcGraphVegetationRenderItem>();
            ArcGraphVegetationRenderQueueDiagnostics zoom1Diagnostics = builder.Build(
                layer,
                zoom1,
                zoom1Items);

            var zoom4Items = new List<ArcGraphVegetationRenderItem>();
            ArcGraphVegetationRenderQueueDiagnostics zoom4Diagnostics = builder.Build(
                layer,
                zoom4,
                zoom4Items);

            bool allZoom1Aggregated = zoom1Items.Count == 2
                                      && zoom1Items[0].IsAreaAggregate
                                      && zoom1Items[1].IsAreaAggregate;

            bool allZoom4Animated = zoom4Items.Count == 2
                                    && zoom4Items[0].AllowsSpriteAnimation
                                    && zoom4Items[1].AllowsSpriteAnimation;

            bool passed = layer.CellCount == 3
                          && zoom1Diagnostics.SnapshotCount == 3
                          && zoom1Diagnostics.VisibleItemCount == 2
                          && zoom1Diagnostics.HiddenItemCount == 1
                          && zoom1Diagnostics.AggregatedItemCount == 2
                          && zoom4Diagnostics.VisibleItemCount == 2
                          && zoom4Diagnostics.HiddenItemCount == 1
                          && zoom4Diagnostics.AnimatedItemCount == 2
                          && allZoom1Aggregated
                          && allZoom4Animated;

            return new ArcGraphVegetationRenderQueueHarnessResult(
                passed,
                passed ? "VegetationRenderQueueSmokePassed" : "VegetationRenderQueueSmokeFailed",
                zoom1Diagnostics.VisibleItemCount,
                zoom1Diagnostics.AggregatedItemCount,
                zoom4Diagnostics.VisibleItemCount,
                zoom4Diagnostics.AnimatedItemCount,
                zoom4Diagnostics.HiddenItemCount);
        }

        private static IEnumerable<ArcGraphVegetationVisualSnapshot> CreateVegetationSnapshots()
        {
            yield return new ArcGraphVegetationVisualSnapshot(
                new ArcGraphCellCoord(1, 1, 0),
                "grass",
                growthStage: 2,
                density01: 0.8f);

            yield return new ArcGraphVegetationVisualSnapshot(
                new ArcGraphCellCoord(2, 1, 0),
                "bush",
                growthStage: 3,
                density01: 0.6f);

            yield return new ArcGraphVegetationVisualSnapshot(
                new ArcGraphCellCoord(3, 1, 0),
                string.Empty,
                growthStage: 1,
                density01: 0.5f);
        }
    }
}
