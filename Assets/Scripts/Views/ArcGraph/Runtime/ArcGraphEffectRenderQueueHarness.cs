using System.Collections.Generic;

namespace Arcontio.View.ArcGraph
{
    // =============================================================================
    // ArcGraphEffectRenderQueueHarnessResult
    // =============================================================================
    /// <summary>
    /// <para>
    /// Risultato sintetico dello smoke test sulla queue effetti.
    /// </para>
    ///
    /// <para><b>Principio architetturale: verifica senza EffectSystem e senza scena</b></para>
    /// <para>
    /// Il risultato contiene solo contatori e flag. Non contiene riferimenti a
    /// incendi reali, fumo fisico, particelle Unity, renderer o GameObject. Serve
    /// a validare che l'Effect Renderer preparatorio trasformi snapshot in item
    /// visuali senza diventare un sistema simulativo.
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
    ///   <item><b>StaticSignalCount</b>: item ridotti a segnale statico.</item>
    ///   <item><b>HiddenCount</b>: item nascosti per snapshot non validi.</item>
    /// </list>
    /// </summary>
    public readonly struct ArcGraphEffectRenderQueueHarnessResult
    {
        public readonly bool Passed;
        public readonly string Reason;
        public readonly int Zoom1VisibleCount;
        public readonly int Zoom1AnimatedCount;
        public readonly int Zoom4VisibleCount;
        public readonly int Zoom4AnimatedCount;
        public readonly int StaticSignalCount;
        public readonly int HiddenCount;

        public ArcGraphEffectRenderQueueHarnessResult(
            bool passed,
            string reason,
            int zoom1VisibleCount,
            int zoom1AnimatedCount,
            int zoom4VisibleCount,
            int zoom4AnimatedCount,
            int staticSignalCount,
            int hiddenCount)
        {
            Passed = passed;
            Reason = string.IsNullOrWhiteSpace(reason) ? "None" : reason;
            Zoom1VisibleCount = zoom1VisibleCount;
            Zoom1AnimatedCount = zoom1AnimatedCount;
            Zoom4VisibleCount = zoom4VisibleCount;
            Zoom4AnimatedCount = zoom4AnimatedCount;
            StaticSignalCount = staticSignalCount;
            HiddenCount = hiddenCount;
        }
    }

    // =============================================================================
    // ArcGraphEffectRenderQueueHarness
    // =============================================================================
    /// <summary>
    /// <para>
    /// Harness statico per validare la queue effetti passiva.
    /// </para>
    ///
    /// <para><b>Principio architetturale: render preparatorio, non sistema effetti</b></para>
    /// <para>
    /// Lo smoke test costruisce un layer effetti in memoria, vi inserisce snapshot
    /// visuali gia' decisi e verifica il builder a due LOD. Non propaga incendi,
    /// non crea particelle, non modifica celle e non apre scene Unity.
    /// </para>
    ///
    /// <para><b>Struttura interna:</b></para>
    /// <list type="bullet">
    ///   <item><b>RunDefaultSmoke</b>: scenario minimo con zoom 1 e zoom 4.</item>
    ///   <item><b>CreateEffectSnapshots</b>: effetti visibili e snapshot nascosti.</item>
    /// </list>
    /// </summary>
    public static class ArcGraphEffectRenderQueueHarness
    {
        // =============================================================================
        // RunDefaultSmoke
        // =============================================================================
        /// <summary>
        /// <para>
        /// Esegue lo smoke test default della queue effetti.
        /// </para>
        ///
        /// <para><b>Scenario minimo</b></para>
        /// <para>
        /// Lo scenario contiene una fiamma intensa, fumo debole e due snapshot non
        /// renderizzabili. A zoom 1 tutto viene ridotto a segnale statico; a zoom 4
        /// gli effetti visibili possono usare animazione frame-based ArcGraph.
        /// </para>
        /// </summary>
        public static ArcGraphEffectRenderQueueHarnessResult RunDefaultSmoke()
        {
            var renderState = new ArcGraphRenderState(
                visibleZLevel: ArcGraphZLevelPolicy.DefaultVisibleZLevel,
                tileSizeWorld: 1f,
                chunkSizeCells: 4);

            var layer = new ArcGraphEffectLayer();
            layer.Initialize(renderState);
            layer.ReplaceSnapshots(CreateEffectSnapshots(), renderState);

            var config = ArcGraphMapViewConfig.CreateDefaultV033();
            var zoom1 = ArcGraphZoomLodPolicy.ResolveFromZoom(config.ResolveZoomLevel(1));
            var zoom4 = ArcGraphZoomLodPolicy.ResolveFromZoom(config.ResolveZoomLevel(4));

            var builder = new ArcGraphEffectRenderQueueBuilder();
            var zoom1Items = new List<ArcGraphEffectRenderItem>();
            ArcGraphEffectRenderQueueDiagnostics zoom1Diagnostics = builder.Build(
                layer,
                zoom1,
                zoom1Items);

            var zoom4Items = new List<ArcGraphEffectRenderItem>();
            ArcGraphEffectRenderQueueDiagnostics zoom4Diagnostics = builder.Build(
                layer,
                zoom4,
                zoom4Items);

            bool zoom1StaticSignals = zoom1Items.Count == 2
                                      && zoom1Diagnostics.StaticSignalCount == 2
                                      && zoom1Diagnostics.AnimatedItemCount == 0;

            bool zoom4AnimationExpected = zoom4Items.Count == 2
                                          && zoom4Diagnostics.AnimatedItemCount == 2;

            bool passed = layer.EffectCount == 4
                          && zoom1Diagnostics.SnapshotCount == 4
                          && zoom1Diagnostics.VisibleItemCount == 2
                          && zoom1Diagnostics.HiddenItemCount == 2
                          && zoom4Diagnostics.VisibleItemCount == 2
                          && zoom4Diagnostics.HiddenItemCount == 2
                          && zoom1StaticSignals
                          && zoom4AnimationExpected;

            return new ArcGraphEffectRenderQueueHarnessResult(
                passed,
                passed ? "EffectRenderQueueSmokePassed" : "EffectRenderQueueSmokeFailed",
                zoom1Diagnostics.VisibleItemCount,
                zoom1Diagnostics.AnimatedItemCount,
                zoom4Diagnostics.VisibleItemCount,
                zoom4Diagnostics.AnimatedItemCount,
                zoom1Diagnostics.StaticSignalCount,
                zoom4Diagnostics.HiddenItemCount);
        }

        private static IEnumerable<ArcGraphEffectVisualSnapshot> CreateEffectSnapshots()
        {
            yield return new ArcGraphEffectVisualSnapshot(
                effectId: 1,
                cell: new ArcGraphCellCoord(1, 1, 0),
                effectKey: "fire",
                intensity01: 1f);

            yield return new ArcGraphEffectVisualSnapshot(
                effectId: 2,
                cell: new ArcGraphCellCoord(2, 1, 0),
                effectKey: "smoke",
                intensity01: 0.35f);

            yield return new ArcGraphEffectVisualSnapshot(
                effectId: 3,
                cell: new ArcGraphCellCoord(3, 1, 0),
                effectKey: string.Empty,
                intensity01: 1f);

            yield return new ArcGraphEffectVisualSnapshot(
                effectId: 4,
                cell: new ArcGraphCellCoord(4, 1, 0),
                effectKey: "spark",
                intensity01: 0f);
        }
    }
}
