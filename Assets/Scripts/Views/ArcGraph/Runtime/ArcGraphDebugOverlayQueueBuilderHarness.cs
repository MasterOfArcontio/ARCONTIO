namespace Arcontio.View.ArcGraph
{
    // =============================================================================
    // ArcGraphDebugOverlayQueueBuilderHarnessResult
    // =============================================================================
    /// <summary>
    /// <para>
    /// Risultato sintetico dello smoke test del builder overlay debug.
    /// </para>
    ///
    /// <para><b>Principio architetturale: QA del builder senza scena Unity</b></para>
    /// <para>
    /// Il risultato verifica che snapshot passivi possano diventare queue debug
    /// senza renderer, asset, <c>MapGridWorldView</c> o accessi al <c>World</c>.
    /// Conta gli item e controlla alcuni campi normalizzati essenziali.
    /// </para>
    /// </summary>
    public readonly struct ArcGraphDebugOverlayQueueBuilderHarnessResult
    {
        public readonly bool Passed;
        public readonly string Reason;
        public readonly int TotalItemCount;
        public readonly int VisibleItemCount;
        public readonly int HiddenItemCount;
        public readonly int ScreenSpaceItemCount;

        public ArcGraphDebugOverlayQueueBuilderHarnessResult(
            bool passed,
            string reason,
            int totalItemCount,
            int visibleItemCount,
            int hiddenItemCount,
            int screenSpaceItemCount)
        {
            Passed = passed;
            Reason = string.IsNullOrWhiteSpace(reason) ? "None" : reason;
            TotalItemCount = totalItemCount;
            VisibleItemCount = visibleItemCount;
            HiddenItemCount = hiddenItemCount;
            ScreenSpaceItemCount = screenSpaceItemCount;
        }
    }

    // =============================================================================
    // ArcGraphDebugOverlayQueueBuilderHarness
    // =============================================================================
    /// <summary>
    /// <para>
    /// Harness statico per validare la conversione snapshot -> queue debug.
    /// </para>
    ///
    /// <para><b>Principio architetturale: builder verificabile in isolamento</b></para>
    /// <para>
    /// Lo smoke costruisce un piccolo frame con FOV, landmark, edge, DT disabilitato
    /// e HUD. Il test include gli item hidden per dimostrare che il builder conserva
    /// anche le ragioni di esclusione quando il QA lo richiede.
    /// </para>
    /// </summary>
    public static class ArcGraphDebugOverlayQueueBuilderHarness
    {
        // =============================================================================
        // RunDefaultSmoke
        // =============================================================================
        /// <summary>
        /// <para>
        /// Esegue uno smoke test minimo del builder debug.
        /// </para>
        /// </summary>
        public static ArcGraphDebugOverlayQueueBuilderHarnessResult RunDefaultSmoke()
        {
            var snapshot = new ArcGraphDebugOverlaySnapshot();
            var queue = new ArcGraphDebugOverlayQueue();
            var builder = new ArcGraphDebugOverlayQueueBuilder();

            var cell = new ArcGraphCellCoord(1, 2, ArcGraphZLevelPolicy.DefaultVisibleZLevel);
            var node = new ArcGraphCellCoord(3, 4, ArcGraphZLevelPolicy.DefaultVisibleZLevel);
            var edgeFrom = new ArcGraphCellCoord(3, 4, ArcGraphZLevelPolicy.DefaultVisibleZLevel);
            var edgeTo = new ArcGraphCellCoord(4, 4, ArcGraphZLevelPolicy.DefaultVisibleZLevel);
            var hiddenCell = new ArcGraphCellCoord(5, 5, ArcGraphZLevelPolicy.DefaultVisibleZLevel);
            var hudAnchor = new ArcGraphCellCoord(0, 0, ArcGraphZLevelPolicy.DefaultVisibleZLevel);

            snapshot.AddCell(new ArcGraphDebugCellOverlaySnapshot(
                cell,
                ArcGraphDebugOverlayKind.FovObservedCell,
                0.75f,
                0,
                null));

            snapshot.AddNode(new ArcGraphDebugNodeOverlaySnapshot(
                node,
                ArcGraphDebugOverlayKind.LandmarkKnownNode,
                42,
                "K#42",
                0.8f,
                null));

            snapshot.AddEdge(new ArcGraphDebugEdgeOverlaySnapshot(
                edgeFrom,
                edgeTo,
                ArcGraphDebugOverlayKind.LandmarkRouteEdge,
                1f,
                null,
                null));

            snapshot.AddCell(new ArcGraphDebugCellOverlaySnapshot(
                hiddenCell,
                ArcGraphDebugOverlayKind.DtHeatCell,
                0.4f,
                3,
                null,
                false));

            snapshot.AddLabel(new ArcGraphDebugLabelOverlaySnapshot(
                hudAnchor,
                ArcGraphDebugOverlayKind.PointerCellCoordsHud,
                0,
                "Cell: 1,2",
                ArcGraphDebugOverlaySpace.ScreenHud));

            ArcGraphDebugOverlayQueueDiagnostics diagnostics = builder.Build(
                snapshot,
                queue,
                true,
                true);

            bool passed = diagnostics.TotalItemCount == 5
                          && diagnostics.CellItemCount == 2
                          && diagnostics.NodeItemCount == 1
                          && diagnostics.EdgeItemCount == 1
                          && diagnostics.LabelItemCount == 1
                          && diagnostics.VisibleItemCount == 4
                          && diagnostics.HiddenItemCount == 1
                          && diagnostics.ScreenSpaceItemCount == 1
                          && queue.Cells[0].ColorKey == "debug/fov/observed"
                          && queue.Cells[1].HiddenReason == "DisabledBySnapshot"
                          && queue.Nodes[0].ColorKey == "debug/landmark/known-node"
                          && queue.Edges[0].WidthKey == "debug/edge/strong"
                          && queue.Labels[0].Space == ArcGraphDebugOverlaySpace.ScreenHud;

            return new ArcGraphDebugOverlayQueueBuilderHarnessResult(
                passed,
                passed ? "DebugOverlayQueueBuilderSmokePassed" : "DebugOverlayQueueBuilderSmokeFailed",
                diagnostics.TotalItemCount,
                diagnostics.VisibleItemCount,
                diagnostics.HiddenItemCount,
                diagnostics.ScreenSpaceItemCount);
        }
    }
}
