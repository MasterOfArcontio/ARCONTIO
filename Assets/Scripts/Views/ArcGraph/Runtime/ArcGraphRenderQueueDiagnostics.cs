namespace Arcontio.View.ArcGraph
{
    // =============================================================================
    // ArcGraphRenderQueueDiagnostics
    // =============================================================================
    /// <summary>
    /// <para>
    /// Diagnostica sintetica di una render queue actor/object ArcGraph.
    /// </para>
    ///
    /// <para><b>Principio architetturale: osservabilita' senza renderer concreto</b></para>
    /// <para>
    /// Prima di creare sprite reali serve poter sapere quanti item sarebbero stati
    /// prodotti, quanti sarebbero visibili e quanti sono stati esclusi. Questa
    /// struttura permette QA e harness senza scena, senza asset e senza UI.
    /// </para>
    ///
    /// <para><b>Struttura interna:</b></para>
    /// <list type="bullet">
    ///   <item><b>ActorItemCount</b>: actor item prodotti.</item>
    ///   <item><b>ObjectItemCount</b>: object item prodotti.</item>
    ///   <item><b>VisibleItemCount</b>: item marcati visibili.</item>
    ///   <item><b>HiddenItemCount</b>: item marcati nascosti.</item>
    ///   <item><b>Reason</b>: descrizione breve dell'esito.</item>
    /// </list>
    /// </summary>
    public readonly struct ArcGraphRenderQueueDiagnostics
    {
        public readonly int ActorItemCount;
        public readonly int ObjectItemCount;
        public readonly int VisibleItemCount;
        public readonly int HiddenItemCount;
        public readonly string Reason;

        public int TotalItemCount => ActorItemCount + ObjectItemCount;

        // =============================================================================
        // ArcGraphRenderQueueDiagnostics
        // =============================================================================
        /// <summary>
        /// <para>
        /// Costruisce una diagnostica queue completa.
        /// </para>
        /// </summary>
        public ArcGraphRenderQueueDiagnostics(
            int actorItemCount,
            int objectItemCount,
            int visibleItemCount,
            int hiddenItemCount,
            string reason)
        {
            ActorItemCount = actorItemCount < 0 ? 0 : actorItemCount;
            ObjectItemCount = objectItemCount < 0 ? 0 : objectItemCount;
            VisibleItemCount = visibleItemCount < 0 ? 0 : visibleItemCount;
            HiddenItemCount = hiddenItemCount < 0 ? 0 : hiddenItemCount;
            Reason = string.IsNullOrWhiteSpace(reason) ? "None" : reason;
        }
    }
}
