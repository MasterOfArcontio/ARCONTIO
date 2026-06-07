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
    ///   <item><b>Zoom1VisibleCount</b>: item visibili a zoom lontano.</item>
    ///   <item><b>Zoom1AnimatedCount</b>: item animabili a zoom lontano.</item>
    ///   <item><b>Zoom4VisibleCount</b>: item visibili a zoom vicino.</item>
    ///   <item><b>Zoom4AnimatedCount</b>: item animabili a zoom vicino.</item>
    ///   <item><b>MaxDepthLevel</b>: profondita' visuale massima.</item>
    ///   <item><b>HiddenCount</b>: item nascosti per snapshot non validi.</item>
    /// </list>
    /// </summary>
    public readonly struct ArcGraphWaterRenderQueueHarnessResult
    {
        public readonly bool Passed;
        public readonly string Reason;
        public readonly int Zoom1VisibleCount;
        public readonly int Zoom1AnimatedCount;
        public readonly int Zoom4VisibleCount;
        public readonly int Zoom4AnimatedCount;
        public readonly int MaxDepthLevel;
        public readonly int HiddenCount;

        public ArcGraphWaterRenderQueueHarnessResult(
            bool passed,
            string reason,
            int zoom1VisibleCount,
            int zoom1AnimatedCount,
            int zoom4VisibleCount,
            int zoom4AnimatedCount,
            int maxDepthLevel,
            int hiddenCount)
        {
            Passed = passed;
            Reason = string.IsNullOrWhiteSpace(reason) ? "None" : reason;
            Zoom1VisibleCount = zoom1VisibleCount;
            Zoom1AnimatedCount = zoom1AnimatedCount;
            Zoom4VisibleCount = zoom4VisibleCount;
            Zoom4AnimatedCount = zoom4AnimatedCount;
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
    ///   <item><b>RunDefaultSmoke</b>: scenario minimo con zoom 1 e zoom 4.</item>
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
        /// cella con profondita' zero che deve restare nascosta. A zoom 1 le
        /// animazioni sono disabilitate dal LOD; a zoom 4 resta animabile solo la
        /// cella che lo dichiarava nello snapshot.
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

            var config = ArcGraphMapViewConfig.CreateDefaultV033();
            var zoom1 = ArcGraphZoomLodPolicy.ResolveFromZoom(config.ResolveZoomLevel(1));
            var zoom4 = ArcGraphZoomLodPolicy.ResolveFromZoom(config.ResolveZoomLevel(4));

            var builder = new ArcGraphWaterRenderQueueBuilder();
            var zoom1Items = new List<ArcGraphWaterRenderItem>();
            ArcGraphWaterRenderQueueDiagnostics zoom1Diagnostics = builder.Build(
                layer,
                zoom1,
                zoom1Items);

            var zoom4Items = new List<ArcGraphWaterRenderItem>();
            ArcGraphWaterRenderQueueDiagnostics zoom4Diagnostics = builder.Build(
                layer,
                zoom4,
                zoom4Items);

            bool zoom1UsesSimplified = zoom1Items.Count == 2
                                       && zoom1Items[0].UsesSimplifiedRepresentation
                                       && zoom1Items[1].UsesSimplifiedRepresentation;

            bool zoom4AnimationExpected = zoom4Diagnostics.AnimatedItemCount == 1
                                          && zoom4Items.Count == 2
                                          && zoom4Items[0].IsAnimated;

            bool passed = layer.CellCount == 3
                          && zoom1Diagnostics.SnapshotCount == 3
                          && zoom1Diagnostics.VisibleItemCount == 2
                          && zoom1Diagnostics.HiddenItemCount == 1
                          && zoom1Diagnostics.AnimatedItemCount == 0
                          && zoom1Diagnostics.MaxDepthLevel == 3
                          && zoom4Diagnostics.VisibleItemCount == 2
                          && zoom4Diagnostics.HiddenItemCount == 1
                          && zoom4Diagnostics.AnimatedItemCount == 1
                          && zoom4Diagnostics.MaxDepthLevel == 3
                          && zoom1UsesSimplified
                          && zoom4AnimationExpected;

            return new ArcGraphWaterRenderQueueHarnessResult(
                passed,
                passed ? "WaterRenderQueueSmokePassed" : "WaterRenderQueueSmokeFailed",
                zoom1Diagnostics.VisibleItemCount,
                zoom1Diagnostics.AnimatedItemCount,
                zoom4Diagnostics.VisibleItemCount,
                zoom4Diagnostics.AnimatedItemCount,
                zoom4Diagnostics.MaxDepthLevel,
                zoom4Diagnostics.HiddenItemCount);
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
