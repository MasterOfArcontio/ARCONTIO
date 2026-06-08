using System.Collections.Generic;
using Arcontio.Core;

namespace Arcontio.View.ArcGraph
{
    // =============================================================================
    // ArcGraphDebugOverlayProducerBridgeHarnessResult
    // =============================================================================
    /// <summary>
    /// <para>
    /// Risultato sintetico dello smoke test del producer bridge debug ArcGraph.
    /// </para>
    ///
    /// <para><b>Principio architetturale: test del ponte, non del renderer</b></para>
    /// <para>
    /// Il risultato conferma che DTO landmark e GVD-DIN possano alimentare
    /// <c>ArcGraphDebugOverlaySnapshot</c> e poi una queue debug. Non verifica
    /// sprite, linee, canvas, camera o input.
    /// </para>
    /// </summary>
    public readonly struct ArcGraphDebugOverlayProducerBridgeHarnessResult
    {
        public readonly bool Passed;
        public readonly string Reason;
        public readonly int SnapshotItemCount;
        public readonly int QueueItemCount;
        public readonly int QueueVisibleItemCount;

        public ArcGraphDebugOverlayProducerBridgeHarnessResult(
            bool passed,
            string reason,
            int snapshotItemCount,
            int queueItemCount,
            int queueVisibleItemCount)
        {
            Passed = passed;
            Reason = string.IsNullOrWhiteSpace(reason) ? "None" : reason;
            SnapshotItemCount = snapshotItemCount;
            QueueItemCount = queueItemCount;
            QueueVisibleItemCount = queueVisibleItemCount;
        }
    }

    // =============================================================================
    // ArcGraphDebugOverlayProducerBridgeHarness
    // =============================================================================
    /// <summary>
    /// <para>
    /// Harness statico per validare il bridge Landmark/GVD verso ArcGraph.
    /// </para>
    ///
    /// <para><b>Principio architetturale: DTO Core come input esplicito</b></para>
    /// <para>
    /// Lo smoke costruisce liste <c>LandmarkOverlayNode</c>,
    /// <c>LandmarkOverlayEdge</c> e <c>GvdDinOverlaySnapshot</c> a mano. Questo
    /// dimostra che il bridge non ha bisogno di leggere il <c>World</c> o la view
    /// legacy per produrre uno snapshot ArcGraph.
    /// </para>
    /// </summary>
    public static class ArcGraphDebugOverlayProducerBridgeHarness
    {
        // =============================================================================
        // RunDefaultSmoke
        // =============================================================================
        /// <summary>
        /// <para>
        /// Esegue uno smoke test minimo su landmark, path e GVD-DIN.
        /// </para>
        /// </summary>
        public static ArcGraphDebugOverlayProducerBridgeHarnessResult RunDefaultSmoke()
        {
            var bridge = new ArcGraphDebugOverlayProducerBridge();
            var snapshot = new ArcGraphDebugOverlaySnapshot();
            var queue = new ArcGraphDebugOverlayQueue();
            var queueBuilder = new ArcGraphDebugOverlayQueueBuilder();

            var worldNodes = new List<LandmarkOverlayNode>
            {
                new LandmarkOverlayNode(1, 1, 0, 10, "W#10")
            };
            var worldEdges = new List<LandmarkOverlayEdge>
            {
                new LandmarkOverlayEdge(1, 1, 2, 1, 1f)
            };
            var knownNodes = new List<LandmarkOverlayNode>
            {
                new LandmarkOverlayNode(3, 1, 0, 20, "K#20")
            };
            var knownEdges = new List<LandmarkOverlayEdge>
            {
                new LandmarkOverlayEdge(3, 1, 4, 1, 0.8f)
            };
            var routeNodes = new List<LandmarkOverlayNode>
            {
                new LandmarkOverlayNode(5, 1, 0, 30, "R#30")
            };
            var routeEdges = new List<LandmarkOverlayEdge>
            {
                new LandmarkOverlayEdge(5, 1, 6, 1, 1f)
            };
            var lmPathEdges = new List<LandmarkOverlayEdge>
            {
                new LandmarkOverlayEdge(6, 1, 6, 2, 1f)
            };
            var directPathEdges = new List<LandmarkOverlayEdge>
            {
                new LandmarkOverlayEdge(6, 2, 7, 2, 1f)
            };
            var jumpPathEdges = new List<LandmarkOverlayEdge>
            {
                new LandmarkOverlayEdge(7, 2, 7, 3, 1f)
            };
            var complexEdges = new List<LandmarkOverlayEdge>
            {
                new LandmarkOverlayEdge(7, 3, 8, 3, 0.6f)
            };

            ArcGraphDebugOverlayProducerBridgeDiagnostics landmarkDiagnostics =
                bridge.FillLandmarkDebugSnapshot(
                    snapshot,
                    worldNodes,
                    worldEdges,
                    knownNodes,
                    knownEdges,
                    routeNodes,
                    routeEdges,
                    lmPathEdges,
                    directPathEdges,
                    jumpPathEdges,
                    complexEdges,
                    true);

            var gvd = new GvdDinOverlaySnapshot();
            gvd.IsValid = true;
            gvd.DtCells.Add(new GvdDinOverlayCellDt(10, 10, 4, 0.5f));
            gvd.DtCells.Add(new GvdDinOverlayCellDt(11, 10, 6, 0.75f));
            gvd.GvdRawCells.Add(new GvdDinOverlayCellGvd(12, 10, 100, 101));
            gvd.GvdNodes.Add(new LandmarkOverlayNode(13, 10, 3, 40, "G#40"));
            gvd.GvdEdges.Add(new LandmarkOverlayEdge(13, 10, 14, 10, 1f));

            ArcGraphDebugOverlayProducerBridgeDiagnostics gvdDiagnostics =
                bridge.FillGvdDinDebugSnapshot(
                    snapshot,
                    gvd);

            ArcGraphDebugOverlayQueueDiagnostics queueDiagnostics =
                queueBuilder.Build(snapshot, queue);

            bool passed = landmarkDiagnostics.AddedNodeCount == 3
                          && landmarkDiagnostics.AddedEdgeCount == 7
                          && gvdDiagnostics.AddedCellCount == 3
                          && gvdDiagnostics.AddedNodeCount == 1
                          && gvdDiagnostics.AddedEdgeCount == 1
                          && snapshot.TotalItemCount == 15
                          && queueDiagnostics.TotalItemCount == 15
                          && queueDiagnostics.VisibleItemCount == 15
                          && queue.Cells[0].Kind == ArcGraphDebugOverlayKind.DtHeatCell
                          && queue.Cells[2].Kind == ArcGraphDebugOverlayKind.GvdRawCell
                          && queue.Nodes[0].Kind == ArcGraphDebugOverlayKind.LandmarkWorldNode
                          && queue.Edges[0].Kind == ArcGraphDebugOverlayKind.LandmarkWorldEdge;

            return new ArcGraphDebugOverlayProducerBridgeHarnessResult(
                passed,
                passed ? "DebugOverlayProducerBridgeSmokePassed" : "DebugOverlayProducerBridgeSmokeFailed",
                snapshot.TotalItemCount,
                queueDiagnostics.TotalItemCount,
                queueDiagnostics.VisibleItemCount);
        }
    }
}
