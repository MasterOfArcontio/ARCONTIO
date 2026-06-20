using System.Collections.Generic;

namespace Arcontio.View.ArcGraph
{
    // =============================================================================
    // ArcGraphWaterRenderQueueHarnessResult
    // =============================================================================
    /// <summary>
    /// <para>
    /// Risultato sintetico dello smoke test sulla queue acqua.
    /// </para>
    ///
    /// <para><b>Principio architetturale: verifica senza idraulica e senza scena</b></para>
    /// <para>
    /// Il risultato contiene solo contatori e flag. Non contiene riferimenti a
    /// sorgenti, fiumi, pressione, pathfinding, sprite Unity, renderer o GameObject.
    /// Serve a validare che il Water Renderer preparatorio trasformi snapshot in
    /// item visuali senza diventare un sistema fisico dell'acqua.
    /// </para>
    ///
    /// <para><b>Struttura interna:</b></para>
    /// <list type="bullet">
    ///   <item><b>Passed</b>: esito complessivo.</item>
    ///   <item><b>Reason</b>: motivo sintetico.</item>
    ///   <item><b>VisibleCount</b>: item visibili nel profilo full-detail.</item>
    ///   <item><b>AnimatedCount</b>: item animabili nel profilo full-detail.</item>
    ///   <item><b>MaxDepthLevel</b>: profondita' visuale massima.</item>
    ///   <item><b>HiddenCount</b>: item nascosti per snapshot non validi.</item>
    /// </list>
    /// </summary>
    public readonly struct ArcGraphWaterRenderQueueHarnessResult
    {
        public readonly bool Passed;
        public readonly string Reason;
        public readonly int VisibleCount;
        public readonly int AnimatedCount;
        public readonly int MaxDepthLevel;
        public readonly int HiddenCount;

        public ArcGraphWaterRenderQueueHarnessResult(
            bool passed,
            string reason,
            int visibleCount,
            int animatedCount,
            int maxDepthLevel,
            int hiddenCount)
        {
            Passed = passed;
            Reason = string.IsNullOrWhiteSpace(reason) ? "None" : reason;
            VisibleCount = visibleCount;
            AnimatedCount = animatedCount;
            MaxDepthLevel = maxDepthLevel;
            HiddenCount = hiddenCount;
        }
    }

    // =============================================================================
    // ArcGraphWaterRenderQueueHarness
    // =============================================================================
    /// <summary>
    /// <para>
    /// Harness statico per validare la queue acqua passiva.
    /// </para>
    ///
    /// <para><b>Principio architetturale: render preparatorio, non flusso acqua</b></para>
    /// <para>
    /// Lo smoke test costruisce un layer acqua in memoria, vi inserisce snapshot
    /// visuali gia' decisi e verifica il builder a due LOD. Non genera fiumi, non
    /// muove acqua e non modifica celle.
    /// </para>
    ///
    /// <para><b>Struttura interna:</b></para>
    /// <list type="bullet">
    ///   <item><b>RunDefaultSmoke</b>: scenario minimo con profilo full-detail.</item>
    ///   <item><b>CreateWaterSnapshots</b>: due snapshot visibili e uno nascosto.</item>
    /// </list>
    /// </summary>
    public static class ArcGraphWaterRenderQueueHarness
    {
        // =============================================================================
        // RunDefaultSmoke
        // =============================================================================
        /// <summary>
        /// <para>
        /// Esegue lo smoke test default della queue acqua.
        /// </para>
        ///
        /// <para><b>Scenario minimo</b></para>
        /// <para>
        /// Lo scenario contiene acqua bassa animabile, acqua profonda statica e una
        /// cella con profondita' zero che deve restare nascosta. Nel profilo
        /// full-detail resta animabile solo la cella che lo dichiarava nello
        /// snapshot.
        /// </para>
        /// </summary>
        public static ArcGraphWaterRenderQueueHarnessResult RunDefaultSmoke()
        {
            var renderState = new ArcGraphRenderState(
                visibleZLevel: ArcGraphZLevelPolicy.DefaultVisibleZLevel,
                tileSizeWorld: 1f,
                chunkSizeCells: 4);

            var layer = new ArcGraphWaterLayer();
            layer.Initialize(renderState);
            layer.ReplaceSnapshots(CreateWaterSnapshots(), renderState);

            ArcGraphZoomLodProfile profile = ArcGraphZoomLodPolicy.ResolveFullDetail();

            var builder = new ArcGraphWaterRenderQueueBuilder();
            var items = new List<ArcGraphWaterRenderItem>();
            ArcGraphWaterRenderQueueDiagnostics diagnostics = builder.Build(
                layer,
                profile,
                items);

            bool animationExpected = diagnostics.AnimatedItemCount == 1
                                     && items.Count == 2
                                     && items[0].IsAnimated;

            bool passed = layer.CellCount == 3
                          && diagnostics.SnapshotCount == 3
                          && diagnostics.VisibleItemCount == 2
                          && diagnostics.HiddenItemCount == 1
                          && diagnostics.AnimatedItemCount == 1
                          && diagnostics.MaxDepthLevel == 3
                          && animationExpected;

            return new ArcGraphWaterRenderQueueHarnessResult(
                passed,
                passed ? "WaterRenderQueueSmokePassed" : "WaterRenderQueueSmokeFailed",
                diagnostics.VisibleItemCount,
                diagnostics.AnimatedItemCount,
                diagnostics.MaxDepthLevel,
                diagnostics.HiddenItemCount);
        }

        private static IEnumerable<ArcGraphWaterVisualSnapshot> CreateWaterSnapshots()
        {
            yield return new ArcGraphWaterVisualSnapshot(
                new ArcGraphCellCoord(1, 1, 0),
                depthLevel: 1,
                spriteKey: "ArcGraph/Water/shallow",
                isAnimated: true);

            yield return new ArcGraphWaterVisualSnapshot(
                new ArcGraphCellCoord(2, 1, 0),
                depthLevel: 3,
                spriteKey: "ArcGraph/Water/deep",
                isAnimated: false);

            yield return new ArcGraphWaterVisualSnapshot(
                new ArcGraphCellCoord(3, 1, 0),
                depthLevel: 0,
                spriteKey: "ArcGraph/Water/empty",
                isAnimated: true);
        }
    }
}
