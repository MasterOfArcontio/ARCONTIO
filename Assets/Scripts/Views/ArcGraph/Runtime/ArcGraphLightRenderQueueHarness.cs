using System.Collections.Generic;

namespace Arcontio.View.ArcGraph
{
    // =============================================================================
    // ArcGraphLightRenderQueueHarnessResult
    // =============================================================================
    /// <summary>
    /// <para>
    /// Risultato sintetico dello smoke test sulla queue luce.
    /// </para>
    ///
    /// <para><b>Principio architetturale: verifica senza propagazione e senza scena</b></para>
    /// <para>
    /// Il risultato contiene solo contatori e flag. Non contiene riferimenti a
    /// luci Unity, materiali, stanze, muri, occlusione, percezione NPC o sistemi di
    /// giorno/notte. Serve a validare che il Light Renderer preparatorio trasformi
    /// snapshot in item visuali senza diventare un sistema luce.
    /// </para>
    ///
    /// <para><b>Struttura interna:</b></para>
    /// <list type="bullet">
    ///   <item><b>Passed</b>: esito complessivo.</item>
    ///   <item><b>Reason</b>: motivo sintetico.</item>
    ///   <item><b>VisibleCount</b>: item luce visibili.</item>
    ///   <item><b>HiddenCount</b>: item luce nascosti.</item>
    ///   <item><b>LocalSourceCount</b>: sorgenti locali visuali.</item>
    ///   <item><b>DarkCellCount</b>: celle scure visuali.</item>
    ///   <item><b>MaxIntensity01</b>: intensita' massima incontrata.</item>
    /// </list>
    /// </summary>
    public readonly struct ArcGraphLightRenderQueueHarnessResult
    {
        public readonly bool Passed;
        public readonly string Reason;
        public readonly int VisibleCount;
        public readonly int HiddenCount;
        public readonly int LocalSourceCount;
        public readonly int DarkCellCount;
        public readonly float MaxIntensity01;

        public ArcGraphLightRenderQueueHarnessResult(
            bool passed,
            string reason,
            int visibleCount,
            int hiddenCount,
            int localSourceCount,
            int darkCellCount,
            float maxIntensity01)
        {
            Passed = passed;
            Reason = string.IsNullOrWhiteSpace(reason) ? "None" : reason;
            VisibleCount = visibleCount;
            HiddenCount = hiddenCount;
            LocalSourceCount = localSourceCount;
            DarkCellCount = darkCellCount;
            MaxIntensity01 = maxIntensity01;
        }
    }

    // =============================================================================
    // ArcGraphLightRenderQueueHarness
    // =============================================================================
    /// <summary>
    /// <para>
    /// Harness statico per validare la queue luce passiva.
    /// </para>
    ///
    /// <para><b>Principio architetturale: luce preparatoria, non illuminazione runtime</b></para>
    /// <para>
    /// Lo smoke test costruisce un layer luce in memoria, vi inserisce snapshot
    /// visuali gia' decisi e verifica il builder. Non propaga luce, non valuta
    /// stanze, non calcola giorno/notte e non modifica celle.
    /// </para>
    ///
    /// <para><b>Struttura interna:</b></para>
    /// <list type="bullet">
    ///   <item><b>RunDefaultSmoke</b>: scenario minimo con buio, luce locale e neutro.</item>
    ///   <item><b>CreateLightSnapshots</b>: snapshot passivi usati dal test.</item>
    /// </list>
    /// </summary>
    public static class ArcGraphLightRenderQueueHarness
    {
        // =============================================================================
        // RunDefaultSmoke
        // =============================================================================
        /// <summary>
        /// <para>
        /// Esegue lo smoke test default della queue luce.
        /// </para>
        ///
        /// <para><b>Scenario minimo</b></para>
        /// <para>
        /// Lo scenario contiene una cella scura, una cella con sorgente locale e
        /// una cella neutra. La cella neutra deve essere nascosta per evitare
        /// overlay inutili; buio e luce locale devono restare visibili.
        /// </para>
        /// </summary>
        public static ArcGraphLightRenderQueueHarnessResult RunDefaultSmoke()
        {
            var renderState = new ArcGraphRenderState(
                visibleZLevel: ArcGraphZLevelPolicy.DefaultVisibleZLevel,
                tileSizeWorld: 1f,
                chunkSizeCells: 4);

            var layer = new ArcGraphLightLayer();
            layer.Initialize(renderState);
            layer.ReplaceSnapshots(CreateLightSnapshots(), renderState);

            ArcGraphZoomLodProfile profile = ArcGraphZoomLodPolicy.ResolveFullDetail();

            var builder = new ArcGraphLightRenderQueueBuilder();
            var items = new List<ArcGraphLightRenderItem>();
            ArcGraphLightRenderQueueDiagnostics diagnostics = builder.Build(
                layer,
                profile,
                items);

            bool firstItemIsDark = items.Count == 2
                                   && items[0].TintKey == "ArcGraph/Light/dark";

            bool secondItemIsLocalSource = items.Count == 2
                                           && items[1].TintKey == "ArcGraph/Light/local"
                                           && items[1].HasLocalSource
                                           && items[1].AllowsLocalTint
                                           && items[1].AllowsGlobalOverlay;

            bool passed = layer.CellCount == 3
                          && diagnostics.SnapshotCount == 3
                          && diagnostics.VisibleItemCount == 2
                          && diagnostics.HiddenItemCount == 1
                          && diagnostics.LocalSourceCount == 1
                          && diagnostics.DarkCellCount == 1
                          && diagnostics.MaxIntensity01 >= 1f
                          && firstItemIsDark
                          && secondItemIsLocalSource;

            return new ArcGraphLightRenderQueueHarnessResult(
                passed,
                passed ? "LightRenderQueueSmokePassed" : "LightRenderQueueSmokeFailed",
                diagnostics.VisibleItemCount,
                diagnostics.HiddenItemCount,
                diagnostics.LocalSourceCount,
                diagnostics.DarkCellCount,
                diagnostics.MaxIntensity01);
        }

        private static IEnumerable<ArcGraphLightVisualSnapshot> CreateLightSnapshots()
        {
            yield return new ArcGraphLightVisualSnapshot(
                new ArcGraphCellCoord(1, 1, 0),
                intensity01: 0.1f,
                tintKey: string.Empty,
                hasLocalSource: false);

            yield return new ArcGraphLightVisualSnapshot(
                new ArcGraphCellCoord(2, 1, 0),
                intensity01: 1f,
                tintKey: string.Empty,
                hasLocalSource: true);

            yield return new ArcGraphLightVisualSnapshot(
                new ArcGraphCellCoord(3, 1, 0),
                intensity01: 1f,
                tintKey: string.Empty,
                hasLocalSource: false);
        }
    }
}
