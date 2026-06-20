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
    ///   <item><b>VisibleCount</b>: item visibili nel profilo full-detail.</item>
    ///   <item><b>AggregatedCount</b>: item aggregati nel profilo full-detail.</item>
    ///   <item><b>AnimatedCount</b>: item animabili nel profilo full-detail.</item>
    ///   <item><b>HiddenCount</b>: item nascosti per snapshot non validi.</item>
    /// </list>
    /// </summary>
    public readonly struct ArcGraphVegetationRenderQueueHarnessResult
    {
        public readonly bool Passed;
        public readonly string Reason;
        public readonly int VisibleCount;
        public readonly int AggregatedCount;
        public readonly int AnimatedCount;
        public readonly int HiddenCount;

        public ArcGraphVegetationRenderQueueHarnessResult(
            bool passed,
            string reason,
            int visibleCount,
            int aggregatedCount,
            int animatedCount,
            int hiddenCount)
        {
            Passed = passed;
            Reason = string.IsNullOrWhiteSpace(reason) ? "None" : reason;
            VisibleCount = visibleCount;
            AggregatedCount = aggregatedCount;
            AnimatedCount = animatedCount;
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
    ///   <item><b>RunDefaultSmoke</b>: scenario minimo con profilo full-detail.</item>
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
        /// species key che deve restare nascosto. Nel profilo full-detail gli item
        /// visibili possono essere animati da ArcGraph.
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

            ArcGraphZoomLodProfile profile = ArcGraphZoomLodPolicy.ResolveFullDetail();

            var builder = new ArcGraphVegetationRenderQueueBuilder();
            var items = new List<ArcGraphVegetationRenderItem>();
            ArcGraphVegetationRenderQueueDiagnostics diagnostics = builder.Build(
                layer,
                profile,
                items);

            bool allAnimated = items.Count == 2
                               && items[0].AllowsSpriteAnimation
                               && items[1].AllowsSpriteAnimation;

            bool passed = layer.CellCount == 3
                          && diagnostics.SnapshotCount == 3
                          && diagnostics.VisibleItemCount == 2
                          && diagnostics.HiddenItemCount == 1
                          && diagnostics.AggregatedItemCount == 0
                          && diagnostics.AnimatedItemCount == 2
                          && allAnimated;

            return new ArcGraphVegetationRenderQueueHarnessResult(
                passed,
                passed ? "VegetationRenderQueueSmokePassed" : "VegetationRenderQueueSmokeFailed",
                diagnostics.VisibleItemCount,
                diagnostics.AggregatedItemCount,
                diagnostics.AnimatedItemCount,
                diagnostics.HiddenItemCount);
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
