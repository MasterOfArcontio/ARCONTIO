namespace Arcontio.View.ArcGraph
{
    // =============================================================================
    // ArcGraphRenderQueueEntry
    // =============================================================================
    /// <summary>
    /// <para>
    /// Entry ordinata che collega la queue globale a un item actor o object.
    /// </para>
    ///
    /// <para><b>Principio architetturale: ordine globale senza perdere payload tipizzato</b></para>
    /// <para>
    /// Actor e oggetti hanno payload diversi. La queue globale non deve schiacciare
    /// queste differenze in un blob generico. Questa entry conserva quindi solo
    /// tipo, indice nella lista tipizzata e sort key. Il renderer futuro potra'
    /// percorrere le entry ordinate e poi leggere l'item corretto dalla lista actor
    /// o object.
    /// </para>
    ///
    /// <para><b>Struttura interna:</b></para>
    /// <list type="bullet">
    ///   <item><b>Kind</b>: actor o object.</item>
    ///   <item><b>ItemIndex</b>: indice nella lista tipizzata corrispondente.</item>
    ///   <item><b>EntityId</b>: id runtime utile a debug e pareggi.</item>
    ///   <item><b>SortKey</b>: chiave ordinamento globale.</item>
    /// </list>
    /// </summary>
    public readonly struct ArcGraphRenderQueueEntry
    {
        public readonly ArcGraphRenderItemKind Kind;
        public readonly int ItemIndex;
        public readonly int EntityId;
        public readonly ArcGraphRenderSortKey SortKey;

        // =============================================================================
        // ArcGraphRenderQueueEntry
        // =============================================================================
        /// <summary>
        /// <para>
        /// Costruisce una entry di queue globale.
        /// </para>
        /// </summary>
        public ArcGraphRenderQueueEntry(
            ArcGraphRenderItemKind kind,
            int itemIndex,
            int entityId,
            ArcGraphRenderSortKey sortKey)
        {
            Kind = kind;
            ItemIndex = itemIndex < 0 ? -1 : itemIndex;
            EntityId = entityId;
            SortKey = sortKey;
        }

        public static ArcGraphRenderQueueEntry ForActor(
            int itemIndex,
            ArcGraphActorRenderItem item)
        {
            return new ArcGraphRenderQueueEntry(
                ArcGraphRenderItemKind.Actor,
                itemIndex,
                item.ActorId,
                item.SortKey);
        }

        public static ArcGraphRenderQueueEntry ForObject(
            int itemIndex,
            ArcGraphObjectRenderItem item)
        {
            return new ArcGraphRenderQueueEntry(
                ArcGraphRenderItemKind.Object,
                itemIndex,
                item.ObjectId,
                item.SortKey);
        }
    }
}
