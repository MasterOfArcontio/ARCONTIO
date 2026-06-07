namespace Arcontio.View.ArcGraph
{
    // =============================================================================
    // ArcGraphVegetationRenderQueueDiagnostics
    // =============================================================================
    /// <summary>
    /// <para>
    /// Diagnostica sintetica della queue vegetazione ArcGraph.
    /// </para>
    ///
    /// <para><b>Principio architetturale: osservabilita' senza renderer concreto</b></para>
    /// <para>
    /// Prima di disegnare erba o piante reali serve sapere quanti snapshot vengono
    /// trasformati in item, quanti restano nascosti, quanti potrebbero essere
    /// animati e quanti sono aggregati per zoom lontani. Questa struttura fornisce
    /// quei contatori senza scena, senza asset e senza sistemi di biosfera.
    /// </para>
    ///
    /// <para><b>Struttura interna:</b></para>
    /// <list type="bullet">
    ///   <item><b>SnapshotCount</b>: snapshot letti dal layer.</item>
    ///   <item><b>VisibleItemCount</b>: item vegetazione visibili.</item>
    ///   <item><b>HiddenItemCount</b>: item vegetazione nascosti.</item>
    ///   <item><b>AnimatedItemCount</b>: item che ammettono animazione frame-based ArcGraph.</item>
    ///   <item><b>AggregatedItemCount</b>: item rappresentati come aggregato d'area.</item>
    ///   <item><b>Reason</b>: descrizione sintetica dell'esito.</item>
    /// </list>
    /// </summary>
    public readonly struct ArcGraphVegetationRenderQueueDiagnostics
    {
        public readonly int SnapshotCount;
        public readonly int VisibleItemCount;
        public readonly int HiddenItemCount;
        public readonly int AnimatedItemCount;
        public readonly int AggregatedItemCount;
        public readonly string Reason;

        public ArcGraphVegetationRenderQueueDiagnostics(
            int snapshotCount,
            int visibleItemCount,
            int hiddenItemCount,
            int animatedItemCount,
            int aggregatedItemCount,
            string reason)
        {
            SnapshotCount = snapshotCount < 0 ? 0 : snapshotCount;
            VisibleItemCount = visibleItemCount < 0 ? 0 : visibleItemCount;
            HiddenItemCount = hiddenItemCount < 0 ? 0 : hiddenItemCount;
            AnimatedItemCount = animatedItemCount < 0 ? 0 : animatedItemCount;
            AggregatedItemCount = aggregatedItemCount < 0 ? 0 : aggregatedItemCount;
            Reason = string.IsNullOrWhiteSpace(reason) ? "None" : reason;
        }
    }
}
