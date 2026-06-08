namespace Arcontio.View.ArcGraph
{
    // =============================================================================
    // ArcGraphDebugOverlayContractHarnessResult
    // =============================================================================
    /// <summary>
    /// <para>
    /// Risultato sintetico dello smoke test sui contratti debug ArcGraph.
    /// </para>
    ///
    /// <para><b>Principio architetturale: test dei dati, non del renderer</b></para>
    /// <para>
    /// Il risultato conferma che cell, node, edge e label possono convivere in una
    /// queue senza scene Unity e senza dipendenze dal Core. Non verifica colori reali,
    /// font, sprite o interazione mouse.
    /// </para>
    /// </summary>
    public readonly struct ArcGraphDebugOverlayContractHarnessResult
    {
        public readonly bool Passed;
        public readonly string Reason;
        public readonly int TotalItemCount;
        public readonly int VisibleItemCount;
        public readonly int HiddenItemCount;
        public readonly int ScreenSpaceItemCount;

        public ArcGraphDebugOverlayContractHarnessResult(
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
    // ArcGraphDebugOverlayContractHarness
    // =============================================================================
    /// <summary>
    /// <para>
    /// Harness statico per validare i contratti dati debug.
    /// </para>
    ///
    /// <para><b>Principio architetturale: migrazione preparatoria</b></para>
    /// <para>
    /// Lo smoke test costruisce una queue con esempi minimi di FOV, landmark edge,
    /// DT e HUD. Questo rappresenta il confine della <c>v0.37b</c>: dimostrare che
    /// esiste un vocabolario dati comune, prima di collegare produttori MapGrid o
    /// renderer Unity.
    /// </para>
    /// </summary>
    public static class ArcGraphDebugOverlayContractHarness
    {
        public static ArcGraphDebugOverlayContractHarnessResult RunDefaultSmoke()
        {
            var queue = new ArcGraphDebugOverlayQueue();

            var cell = new ArcGraphCellCoord(1, 2, ArcGraphZLevelPolicy.DefaultVisibleZLevel);
            var node = new ArcGraphCellCoord(3, 4, ArcGraphZLevelPolicy.DefaultVisibleZLevel);
            var edgeFrom = new ArcGraphCellCoord(3, 4, ArcGraphZLevelPolicy.DefaultVisibleZLevel);
            var edgeTo = new ArcGraphCellCoord(4, 4, ArcGraphZLevelPolicy.DefaultVisibleZLevel);
            var hudAnchor = new ArcGraphCellCoord(0, 0, ArcGraphZLevelPolicy.DefaultVisibleZLevel);

            queue.AddCell(new ArcGraphDebugCellOverlayItem(
                cell,
                ArcGraphDebugOverlayKind.FovObservedCell,
                0.75f,
                0,
                null,
                true,
                null,
                ArcGraphRenderSortKey.FromCell(cell, 110, ArcGraphRenderItemKind.Debug, 1)));

            queue.AddNode(new ArcGraphDebugNodeOverlayItem(
                node,
                ArcGraphDebugOverlayKind.LandmarkKnownNode,
                42,
                "K#42",
                0.8f,
                null,
                true,
                null,
                ArcGraphRenderSortKey.FromCell(node, 120, ArcGraphRenderItemKind.Debug, 42)));

            queue.AddEdge(new ArcGraphDebugEdgeOverlayItem(
                edgeFrom,
                edgeTo,
                ArcGraphDebugOverlayKind.LandmarkRouteEdge,
                1f,
                null,
                null,
                true,
                null,
                ArcGraphRenderSortKey.FromCell(edgeFrom, 115, ArcGraphRenderItemKind.Debug, 43)));

            queue.AddCell(new ArcGraphDebugCellOverlayItem(
                new ArcGraphCellCoord(5, 5, ArcGraphZLevelPolicy.DefaultVisibleZLevel),
                ArcGraphDebugOverlayKind.DtHeatCell,
                0.4f,
                3,
                null,
                false,
                "DebugOverlayHiddenByToggle",
                ArcGraphRenderSortKey.FromCell(new ArcGraphCellCoord(5, 5, ArcGraphZLevelPolicy.DefaultVisibleZLevel), 111, ArcGraphRenderItemKind.Debug, 2)));

            queue.AddLabel(new ArcGraphDebugLabelOverlayItem(
                hudAnchor,
                ArcGraphDebugOverlayKind.PointerCellCoordsHud,
                0,
                "Cell: 1,2",
                ArcGraphDebugOverlaySpace.ScreenHud,
                true,
                null,
                ArcGraphRenderSortKey.FromCell(hudAnchor, 130, ArcGraphRenderItemKind.Debug, 3)));

            ArcGraphDebugOverlayQueueDiagnostics diagnostics = queue.CreateDiagnostics("DebugOverlayContractSmoke");

            bool passed = diagnostics.TotalItemCount == 5
                          && diagnostics.CellItemCount == 2
                          && diagnostics.NodeItemCount == 1
                          && diagnostics.EdgeItemCount == 1
                          && diagnostics.LabelItemCount == 1
                          && diagnostics.VisibleItemCount == 4
                          && diagnostics.HiddenItemCount == 1
                          && diagnostics.ScreenSpaceItemCount == 1
                          && queue.Cells[0].ColorKey == "debug/fov/observed"
                          && queue.Nodes[0].ColorKey == "debug/landmark/known-node"
                          && queue.Edges[0].WidthKey == "debug/edge/strong"
                          && queue.Labels[0].Space == ArcGraphDebugOverlaySpace.ScreenHud;

            return new ArcGraphDebugOverlayContractHarnessResult(
                passed,
                passed ? "DebugOverlayContractSmokePassed" : "DebugOverlayContractSmokeFailed",
                diagnostics.TotalItemCount,
                diagnostics.VisibleItemCount,
                diagnostics.HiddenItemCount,
                diagnostics.ScreenSpaceItemCount);
        }
    }
}
