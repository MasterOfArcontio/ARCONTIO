namespace Arcontio.View.ArcGraph
{
    // =============================================================================
    // ArcGraphDebugOverlayQueueDiagnostics
    // =============================================================================
    /// <summary>
    /// <para>
    /// Diagnostica sintetica di una queue overlay debug ArcGraph.
    /// </para>
    ///
    /// <para><b>Principio architetturale: contare senza renderizzare</b></para>
    /// <para>
    /// La diagnostica permette di verificare il contenuto prodotto da builder futuri
    /// senza aprire scene Unity e senza leggere <c>MapGridWorldView</c>. Conta le
    /// categorie logiche e separa esplicitamente overlay di mappa e HUD.
    /// </para>
    ///
    /// <para><b>Struttura interna:</b></para>
    /// <list type="bullet">
    ///   <item><b>CellItemCount</b>: celle debug.</item>
    ///   <item><b>NodeItemCount</b>: nodi debug.</item>
    ///   <item><b>EdgeItemCount</b>: edge debug.</item>
    ///   <item><b>LabelItemCount</b>: label e HUD.</item>
    ///   <item><b>VisibleItemCount</b>: item visibili totali.</item>
    ///   <item><b>HiddenItemCount</b>: item nascosti totali.</item>
    ///   <item><b>ScreenSpaceItemCount</b>: item non puramente map-space.</item>
    ///   <item><b>Reason</b>: esito sintetico.</item>
    /// </list>
    /// </summary>
    public readonly struct ArcGraphDebugOverlayQueueDiagnostics
    {
        public readonly int CellItemCount;
        public readonly int NodeItemCount;
        public readonly int EdgeItemCount;
        public readonly int LabelItemCount;
        public readonly int VisibleItemCount;
        public readonly int HiddenItemCount;
        public readonly int ScreenSpaceItemCount;
        public readonly string Reason;

        public int TotalItemCount => CellItemCount + NodeItemCount + EdgeItemCount + LabelItemCount;

        public ArcGraphDebugOverlayQueueDiagnostics(
            int cellItemCount,
            int nodeItemCount,
            int edgeItemCount,
            int labelItemCount,
            int visibleItemCount,
            int hiddenItemCount,
            int screenSpaceItemCount,
            string reason)
        {
            CellItemCount = NormalizeCount(cellItemCount);
            NodeItemCount = NormalizeCount(nodeItemCount);
            EdgeItemCount = NormalizeCount(edgeItemCount);
            LabelItemCount = NormalizeCount(labelItemCount);
            VisibleItemCount = NormalizeCount(visibleItemCount);
            HiddenItemCount = NormalizeCount(hiddenItemCount);
            ScreenSpaceItemCount = NormalizeCount(screenSpaceItemCount);
            Reason = string.IsNullOrWhiteSpace(reason) ? "None" : reason;
        }

        private static int NormalizeCount(int value)
        {
            return value < 0 ? 0 : value;
        }
    }
}
