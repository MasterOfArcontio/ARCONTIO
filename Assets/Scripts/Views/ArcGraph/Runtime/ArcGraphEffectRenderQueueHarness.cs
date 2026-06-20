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
    ///   <item><b>VisibleCount</b>: item visibili nel profilo full-detail.</item>
    ///   <item><b>AnimatedCount</b>: item animabili nel profilo full-detail.</item>
    ///   <item><b>StaticSignalCount</b>: item ridotti a segnale statico.</item>
    ///   <item><b>HiddenCount</b>: item nascosti per snapshot non validi.</item>
    /// </list>
    /// </summary>
    public readonly struct ArcGraphEffectRenderQueueHarnessResult
    {
        public readonly bool Passed;
        public readonly string Reason;
        public readonly int VisibleCount;
        public readonly int AnimatedCount;
        public readonly int StaticSignalCount;
        public readonly int HiddenCount;

        public ArcGraphEffectRenderQueueHarnessResult(
            bool passed,
            string reason,
            int visibleCount,
            int animatedCount,
            int staticSignalCount,
            int hiddenCount)
        {
            Passed = passed;
            Reason = string.IsNullOrWhiteSpace(reason) ? "None" : reason;
            VisibleCount = visibleCount;
            AnimatedCount = animatedCount;
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
    ///   <item><b>RunDefaultSmoke</b>: scenario minimo con profilo full-detail.</item>
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
        /// renderizzabili. Il profilo visuale full-detail permette animazione
        /// frame-based ArcGraph quando lo snapshot e il contratto la consentono.
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

            ArcGraphZoomLodProfile profile = ArcGraphZoomLodPolicy.ResolveFullDetail();

            var builder = new ArcGraphEffectRenderQueueBuilder();
            var items = new List<ArcGraphEffectRenderItem>();
            ArcGraphEffectRenderQueueDiagnostics diagnostics = builder.Build(
                layer,
                profile,
                items);

            bool fullDetailAnimationExpected = items.Count == 2
                                               && diagnostics.AnimatedItemCount == 2
                                               && diagnostics.StaticSignalCount == 0;

            bool passed = layer.EffectCount == 4
                          && diagnostics.SnapshotCount == 4
                          && diagnostics.VisibleItemCount == 2
                          && diagnostics.HiddenItemCount == 2
                          && fullDetailAnimationExpected;

            return new ArcGraphEffectRenderQueueHarnessResult(
                passed,
                passed ? "EffectRenderQueueSmokePassed" : "EffectRenderQueueSmokeFailed",
                diagnostics.VisibleItemCount,
                diagnostics.AnimatedItemCount,
                diagnostics.StaticSignalCount,
                diagnostics.HiddenItemCount);
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
