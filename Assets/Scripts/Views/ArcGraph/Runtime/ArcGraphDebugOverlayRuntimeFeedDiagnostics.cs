namespace Arcontio.View.ArcGraph
{
    // =============================================================================
    // ArcGraphDebugOverlayRuntimeFeedDiagnostics
    // =============================================================================
    /// <summary>
    /// <para>
    /// Diagnostica sintetica del feed runtime debug ArcGraph.
    /// </para>
    ///
    /// <para><b>Principio architetturale: rendere verificabile il ponte runtime</b></para>
    /// <para>
    /// Il feed e' il primo punto che puo' leggere il <c>World</c> come sorgente
    /// view/debug read-only. Questa diagnostica separa quindi le tre fasi: dati
    /// richiesti dal mondo, snapshot ArcGraph prodotto e queue finale normalizzata.
    /// Non misura rendering Unity e non rappresenta stato simulativo.
    /// </para>
    ///
    /// <para><b>Struttura interna:</b></para>
    /// <list type="bullet">
    ///   <item><b>WasWorldProvided</b>: indica se il feed ha ricevuto una sorgente runtime.</item>
    ///   <item><b>ActiveNpcId</b>: NPC usato per il debug landmark soggettivo.</item>
    ///   <item><b>Landmark*/Gvd*</b>: richieste e tentativi realmente eseguiti.</item>
    ///   <item><b>SnapshotItemCount</b>: item inseriti nello snapshot ArcGraph.</item>
    ///   <item><b>QueueDiagnostics</b>: conteggi finali della queue debug.</item>
    ///   <item><b>Reason</b>: motivo sintetico dell'esito.</item>
    /// </list>
    /// </summary>
    public readonly struct ArcGraphDebugOverlayRuntimeFeedDiagnostics
    {
        public readonly bool WasWorldProvided;
        public readonly int ActiveNpcId;
        public readonly bool LandmarkRequested;
        public readonly bool LandmarkAttempted;
        public readonly bool GvdRequested;
        public readonly bool GvdAttempted;
        public readonly int LandmarkSourceNodeCount;
        public readonly int LandmarkSourceEdgeCount;
        public readonly int GvdSourceDtCellCount;
        public readonly int GvdSourceRawCellCount;
        public readonly int SnapshotItemCount;
        public readonly ArcGraphDebugOverlayQueueDiagnostics QueueDiagnostics;
        public readonly string Reason;

        public int QueueItemCount => QueueDiagnostics.TotalItemCount;
        public int VisibleItemCount => QueueDiagnostics.VisibleItemCount;

        public ArcGraphDebugOverlayRuntimeFeedDiagnostics(
            bool wasWorldProvided,
            int activeNpcId,
            bool landmarkRequested,
            bool landmarkAttempted,
            bool gvdRequested,
            bool gvdAttempted,
            int landmarkSourceNodeCount,
            int landmarkSourceEdgeCount,
            int gvdSourceDtCellCount,
            int gvdSourceRawCellCount,
            int snapshotItemCount,
            ArcGraphDebugOverlayQueueDiagnostics queueDiagnostics,
            string reason)
        {
            WasWorldProvided = wasWorldProvided;
            ActiveNpcId = activeNpcId;
            LandmarkRequested = landmarkRequested;
            LandmarkAttempted = landmarkAttempted;
            GvdRequested = gvdRequested;
            GvdAttempted = gvdAttempted;
            LandmarkSourceNodeCount = NormalizeCount(landmarkSourceNodeCount);
            LandmarkSourceEdgeCount = NormalizeCount(landmarkSourceEdgeCount);
            GvdSourceDtCellCount = NormalizeCount(gvdSourceDtCellCount);
            GvdSourceRawCellCount = NormalizeCount(gvdSourceRawCellCount);
            SnapshotItemCount = NormalizeCount(snapshotItemCount);
            QueueDiagnostics = queueDiagnostics;
            Reason = string.IsNullOrWhiteSpace(reason) ? "None" : reason;
        }

        private static int NormalizeCount(int value)
        {
            return value < 0 ? 0 : value;
        }
    }
}
