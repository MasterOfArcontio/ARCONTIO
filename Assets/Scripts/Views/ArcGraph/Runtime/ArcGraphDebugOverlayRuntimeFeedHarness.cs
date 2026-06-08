using System.Collections.Generic;
using Arcontio.Core;

namespace Arcontio.View.ArcGraph
{
    // =============================================================================
    // ArcGraphDebugOverlayRuntimeFeedHarnessResult
    // =============================================================================
    /// <summary>
    /// <para>
    /// Risultato sintetico dello smoke test del feed runtime debug ArcGraph.
    /// </para>
    ///
    /// <para><b>Principio architetturale: QA del feed senza scena Unity</b></para>
    /// <para>
    /// Il risultato conferma che il feed gestisce sia sorgente runtime mancante sia
    /// DTO preparati a mano. Non verifica renderer, asset, input, camera o
    /// <c>MapGridWorldView</c>.
    /// </para>
    /// </summary>
    public readonly struct ArcGraphDebugOverlayRuntimeFeedHarnessResult
    {
        public readonly bool Passed;
        public readonly string Reason;
        public readonly string NullWorldReason;
        public readonly string PreparedReason;
        public readonly int PreparedSnapshotItemCount;
        public readonly int PreparedQueueItemCount;
        public readonly int PreparedVisibleItemCount;

        public ArcGraphDebugOverlayRuntimeFeedHarnessResult(
            bool passed,
            string reason,
            string nullWorldReason,
            string preparedReason,
            int preparedSnapshotItemCount,
            int preparedQueueItemCount,
            int preparedVisibleItemCount)
        {
            Passed = passed;
            Reason = string.IsNullOrWhiteSpace(reason) ? "None" : reason;
            NullWorldReason = string.IsNullOrWhiteSpace(nullWorldReason) ? "None" : nullWorldReason;
            PreparedReason = string.IsNullOrWhiteSpace(preparedReason) ? "None" : preparedReason;
            PreparedSnapshotItemCount = preparedSnapshotItemCount;
            PreparedQueueItemCount = preparedQueueItemCount;
            PreparedVisibleItemCount = preparedVisibleItemCount;
        }
    }

    // =============================================================================
    // ArcGraphDebugOverlayRuntimeFeedHarness
    // =============================================================================
    /// <summary>
    /// <para>
    /// Harness statico per validare il feed runtime debug ArcGraph.
    /// </para>
    ///
    /// <para><b>Principio architetturale: feed testabile senza bootstrap Unity</b></para>
    /// <para>
    /// Lo smoke esegue due controlli. Primo: <c>World</c> nullo produce una queue
    /// vuota e una ragione esplicita. Secondo: DTO landmark/GVD minimi alimentano
    /// snapshot e queue con conteggi attesi. Questo basta a provare il contratto
    /// del feed senza richiedere una scena o un mondo simulato completo.
    /// </para>
    /// </summary>
    public static class ArcGraphDebugOverlayRuntimeFeedHarness
    {
        // =============================================================================
        // RunDefaultSmoke
        // =============================================================================
        /// <summary>
        /// <para>
        /// Esegue lo smoke test minimo del feed runtime debug.
        /// </para>
        /// </summary>
        public static ArcGraphDebugOverlayRuntimeFeedHarnessResult RunDefaultSmoke()
        {
            var feed = new ArcGraphDebugOverlayRuntimeFeed();

            ArcGraphDebugOverlayRuntimeFeedDiagnostics nullWorld =
                feed.BuildFromWorld(null, 1);

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

            var gvd = new GvdDinOverlaySnapshot();
            gvd.IsValid = true;
            gvd.DtCells.Add(new GvdDinOverlayCellDt(10, 10, 4, 0.5f));
            gvd.GvdRawCells.Add(new GvdDinOverlayCellGvd(11, 10, 100, 101));
            gvd.GvdNodes.Add(new LandmarkOverlayNode(12, 10, 3, 40, "G#40"));
            gvd.GvdEdges.Add(new LandmarkOverlayEdge(12, 10, 13, 10, 1f));

            ArcGraphDebugOverlayRuntimeFeedDiagnostics prepared =
                feed.BuildFromPreparedDebugData(
                    worldNodes,
                    worldEdges,
                    knownNodes,
                    knownEdges,
                    routeNodes,
                    routeEdges,
                    lmPathEdges,
                    null,
                    null,
                    null,
                    gvd);

            bool passed = nullWorld.Reason == "WorldMissing"
                          && nullWorld.QueueItemCount == 0
                          && prepared.Reason == "PreparedDebugRuntimeFeedBuilt"
                          && prepared.SnapshotItemCount == 11
                          && prepared.QueueItemCount == 11
                          && prepared.VisibleItemCount == 11
                          && prepared.LandmarkSourceNodeCount == 3
                          && prepared.LandmarkSourceEdgeCount == 4
                          && prepared.GvdSourceDtCellCount == 1
                          && prepared.GvdSourceRawCellCount == 1
                          && feed.Queue.Cells.Count == 2
                          && feed.Queue.Nodes.Count == 4
                          && feed.Queue.Edges.Count == 5;

            return new ArcGraphDebugOverlayRuntimeFeedHarnessResult(
                passed,
                passed ? "DebugOverlayRuntimeFeedSmokePassed" : "DebugOverlayRuntimeFeedSmokeFailed",
                nullWorld.Reason,
                prepared.Reason,
                prepared.SnapshotItemCount,
                prepared.QueueItemCount,
                prepared.VisibleItemCount);
        }
    }
}
