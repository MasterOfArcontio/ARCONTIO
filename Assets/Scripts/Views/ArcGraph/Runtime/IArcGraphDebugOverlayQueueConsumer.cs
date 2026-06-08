namespace Arcontio.View.ArcGraph
{
    // =============================================================================
    // IArcGraphDebugOverlayQueueConsumer
    // =============================================================================
    /// <summary>
    /// <para>
    /// Contratto minimale per un oggetto capace di consumare una queue debug
    /// ArcGraph gia' prodotta.
    /// </para>
    ///
    /// <para><b>Principio architetturale: consumer esplicito, non ricerca scena</b></para>
    /// <para>
    /// Il coordinatore runtime debug non deve cercare componenti Unity, non deve
    /// usare <c>FindObjectOfType</c> e non deve conoscere il tipo concreto del
    /// renderer. Riceve un consumer gia' deciso dal chiamante e gli consegna la
    /// queue. In questo modo il punto di contatto tra feed e renderer resta
    /// testabile e non diventa un manager globale.
    /// </para>
    ///
    /// <para><b>Struttura interna:</b></para>
    /// <list type="bullet">
    ///   <item><b>RenderQueue</b>: consuma la queue debug normalizzata.</item>
    /// </list>
    /// </summary>
    public interface IArcGraphDebugOverlayQueueConsumer
    {
        // =============================================================================
        // RenderQueue
        // =============================================================================
        /// <summary>
        /// <para>
        /// Consuma una queue debug ArcGraph gia' pronta.
        /// </para>
        ///
        /// <para>
        /// Il consumer concreto puo' scegliere di renderizzare, loggare o ignorare
        /// la queue. Il contratto non impone Unity, GameObject, camera o asset.
        /// </para>
        /// </summary>
        void RenderQueue(ArcGraphDebugOverlayQueue queue);
    }
}
