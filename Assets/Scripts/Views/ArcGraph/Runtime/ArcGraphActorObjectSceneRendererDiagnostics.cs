namespace Arcontio.View.ArcGraph
{
    // =============================================================================
    // ArcGraphActorObjectSceneRendererDiagnostics
    // =============================================================================
    /// <summary>
    /// <para>
    /// Diagnostica sintetica del contratto renderer scena actor/object ArcGraph.
    /// </para>
    ///
    /// <para><b>Principio architetturale: probe spiegabile prima del rendering</b></para>
    /// <para>
    /// Il futuro renderer actor/object dovra' poter spiegare se aveva una queue,
    /// se il contratto era sicuro, quante entry scene-side ha pianificato e se il
    /// resolver sprite era disponibile. Questa struttura prepara quei controlli
    /// senza creare ancora componenti Unity.
    /// </para>
    ///
    /// <para><b>Struttura interna:</b></para>
    /// <list type="bullet">
    ///   <item><b>HasQueue</b>: queue actor/object ricevuta.</item>
    ///   <item><b>HasSpriteResolver</b>: resolver scene-side dichiarato.</item>
    ///   <item><b>ContractSafe</b>: contratto compatibile con probe temporaneo.</item>
    ///   <item><b>ActorItemCount/ObjectItemCount</b>: item presenti nella queue.</item>
    ///   <item><b>QueueEntryCount</b>: entry ordinate ricevute dalla queue.</item>
    ///   <item><b>PlannedEntryCount</b>: entry scene-side generate.</item>
    ///   <item><b>Reason</b>: esito sintetico leggibile.</item>
    /// </list>
    /// </summary>
    public readonly struct ArcGraphActorObjectSceneRendererDiagnostics
    {
        public readonly bool HasQueue;
        public readonly bool HasSpriteResolver;
        public readonly bool ContractSafe;
        public readonly bool UsedRenderQueueOrder;
        public readonly bool UsedActorInterpolatedPose;
        public readonly int ActorItemCount;
        public readonly int ObjectItemCount;
        public readonly int QueueEntryCount;
        public readonly int PlannedEntryCount;
        public readonly int ActorEntryCount;
        public readonly int ObjectEntryCount;
        public readonly int MissingSpriteKeyCount;
        public readonly string Reason;

        // =============================================================================
        // ArcGraphActorObjectSceneRendererDiagnostics
        // =============================================================================
        /// <summary>
        /// <para>
        /// Costruisce una diagnostica immutabile del piano scena actor/object.
        /// </para>
        /// </summary>
        public ArcGraphActorObjectSceneRendererDiagnostics(
            bool hasQueue,
            bool hasSpriteResolver,
            bool contractSafe,
            bool usedRenderQueueOrder,
            bool usedActorInterpolatedPose,
            int actorItemCount,
            int objectItemCount,
            int queueEntryCount,
            int plannedEntryCount,
            int actorEntryCount,
            int objectEntryCount,
            int missingSpriteKeyCount,
            string reason)
        {
            HasQueue = hasQueue;
            HasSpriteResolver = hasSpriteResolver;
            ContractSafe = contractSafe;
            UsedRenderQueueOrder = usedRenderQueueOrder;
            UsedActorInterpolatedPose = usedActorInterpolatedPose;
            ActorItemCount = actorItemCount < 0 ? 0 : actorItemCount;
            ObjectItemCount = objectItemCount < 0 ? 0 : objectItemCount;
            QueueEntryCount = queueEntryCount < 0 ? 0 : queueEntryCount;
            PlannedEntryCount = plannedEntryCount < 0 ? 0 : plannedEntryCount;
            ActorEntryCount = actorEntryCount < 0 ? 0 : actorEntryCount;
            ObjectEntryCount = objectEntryCount < 0 ? 0 : objectEntryCount;
            MissingSpriteKeyCount = missingSpriteKeyCount < 0 ? 0 : missingSpriteKeyCount;
            Reason = string.IsNullOrWhiteSpace(reason) ? "None" : reason;
        }
    }
}
