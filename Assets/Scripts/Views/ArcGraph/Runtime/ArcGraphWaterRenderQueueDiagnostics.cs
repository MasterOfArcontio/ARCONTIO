namespace Arcontio.View.ArcGraph
{
    // =============================================================================
    // ArcGraphWaterRenderQueueDiagnostics
    // =============================================================================
    /// <summary>
    /// <para>
    /// Diagnostica sintetica della queue acqua ArcGraph.
    /// </para>
    ///
    /// <para><b>Principio architetturale: osservabilita' senza simulazione idraulica</b></para>
    /// <para>
    /// La diagnostica conta gli snapshot acqua trasformati in item visuali, gli
    /// item nascosti e quelli animabili. Non misura flussi, pressione, evaporazione
    /// o profondita' fisica: sono responsabilita' di sistemi esterni futuri.
    /// </para>
    ///
    /// <para><b>Struttura interna:</b></para>
    /// <list type="bullet">
    ///   <item><b>SnapshotCount</b>: snapshot letti dal layer.</item>
    ///   <item><b>VisibleItemCount</b>: item acqua visibili.</item>
    ///   <item><b>HiddenItemCount</b>: item acqua nascosti.</item>
    ///   <item><b>AnimatedItemCount</b>: item che ammettono animazione frame-based ArcGraph.</item>
    ///   <item><b>MaxDepthLevel</b>: massima profondita' visuale incontrata.</item>
    ///   <item><b>Reason</b>: descrizione sintetica dell'esito.</item>
    /// </list>
    /// </summary>
    public readonly struct ArcGraphWaterRenderQueueDiagnostics
    {
        public readonly int SnapshotCount;
        public readonly int VisibleItemCount;
        public readonly int HiddenItemCount;
        public readonly int AnimatedItemCount;
        public readonly int MaxDepthLevel;
        public readonly string Reason;

        public ArcGraphWaterRenderQueueDiagnostics(
            int snapshotCount,
            int visibleItemCount,
            int hiddenItemCount,
            int animatedItemCount,
            int maxDepthLevel,
            string reason)
        {
            SnapshotCount = snapshotCount < 0 ? 0 : snapshotCount;
            VisibleItemCount = visibleItemCount < 0 ? 0 : visibleItemCount;
            HiddenItemCount = hiddenItemCount < 0 ? 0 : hiddenItemCount;
            AnimatedItemCount = animatedItemCount < 0 ? 0 : animatedItemCount;
            MaxDepthLevel = maxDepthLevel < 0 ? 0 : maxDepthLevel;
            Reason = string.IsNullOrWhiteSpace(reason) ? "None" : reason;
        }
    }
}
