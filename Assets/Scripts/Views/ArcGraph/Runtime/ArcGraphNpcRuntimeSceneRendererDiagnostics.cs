namespace Arcontio.View.ArcGraph
{
    // =============================================================================
    // ArcGraphNpcRuntimeSceneRendererDiagnostics
    // =============================================================================
    /// <summary>
    /// <para>
    /// Diagnostica sintetica dell'ultimo passaggio del renderer runtime NPC.
    /// </para>
    ///
    /// <para><b>Principio architetturale: rendering spiegabile</b></para>
    /// <para>
    /// Il renderer NPC deve sempre poter spiegare perche' ha disegnato, riusato,
    /// creato, disattivato o saltato gli sprite. La diagnostica resta value-only e
    /// non contiene riferimenti a <c>GameObject</c>, <c>SpriteRenderer</c> o asset.
    /// </para>
    ///
    /// <para><b>Struttura interna:</b></para>
    /// <list type="bullet">
    ///   <item><b>RendererEnabled</b>: gate principale del renderer.</item>
    ///   <item><b>ContractSafe</b>: validita' del contratto runtime.</item>
    ///   <item><b>HasQueue</b>: presenza della queue actor/object.</item>
    ///   <item><b>HasSpriteResolver</b>: presenza di un resolver sprite scene-side.</item>
    ///   <item><b>ActorEntryCount</b>: NPC pianificati dal piano actor/object.</item>
    ///   <item><b>RenderedActorCount</b>: NPC applicati in scena.</item>
    ///   <item><b>CreatedActorObjectCount/ReusedActorObjectCount</b>: uso del pool.</item>
    ///   <item><b>DisabledActorObjectCount</b>: handle spenti per assenza dal frame.</item>
    ///   <item><b>MissingSpriteCount</b>: richieste senza sprite risolto.</item>
    ///   <item><b>GeneratedFallbackSpriteCount</b>: sprite fallback generati dal renderer.</item>
    ///   <item><b>Reason</b>: esito principale leggibile in console.</item>
    /// </list>
    /// </summary>
    public readonly struct ArcGraphNpcRuntimeSceneRendererDiagnostics
    {
        public readonly bool RendererEnabled;
        public readonly bool HasContract;
        public readonly bool ContractSafe;
        public readonly bool HasQueue;
        public readonly bool HasSpriteResolver;
        public readonly bool BuiltPlan;
        public readonly int QueueEntryCount;
        public readonly int ActorItemCount;
        public readonly int ActorEntryCount;
        public readonly int RenderedActorCount;
        public readonly int CreatedActorObjectCount;
        public readonly int ReusedActorObjectCount;
        public readonly int DisabledActorObjectCount;
        public readonly int ActiveActorObjectCount;
        public readonly int MissingSpriteCount;
        public readonly int GeneratedFallbackSpriteCount;
        public readonly int LayeredActorCount;
        public readonly int CreatedPartRendererCount;
        public readonly int ReusedPartRendererCount;
        public readonly int MissingCatalogFrameCount;
        public readonly string Reason;

        // =============================================================================
        // ArcGraphNpcRuntimeSceneRendererDiagnostics
        // =============================================================================
        /// <summary>
        /// <para>
        /// Costruisce una diagnostica immutabile del renderer runtime NPC.
        /// </para>
        /// </summary>
        public ArcGraphNpcRuntimeSceneRendererDiagnostics(
            bool rendererEnabled,
            bool hasContract,
            bool contractSafe,
            bool hasQueue,
            bool hasSpriteResolver,
            bool builtPlan,
            int queueEntryCount,
            int actorItemCount,
            int actorEntryCount,
            int renderedActorCount,
            int createdActorObjectCount,
            int reusedActorObjectCount,
            int disabledActorObjectCount,
            int activeActorObjectCount,
            int missingSpriteCount,
            int generatedFallbackSpriteCount,
            int layeredActorCount,
            int createdPartRendererCount,
            int reusedPartRendererCount,
            int missingCatalogFrameCount,
            string reason)
        {
            RendererEnabled = rendererEnabled;
            HasContract = hasContract;
            ContractSafe = contractSafe;
            HasQueue = hasQueue;
            HasSpriteResolver = hasSpriteResolver;
            BuiltPlan = builtPlan;
            QueueEntryCount = ClampCount(queueEntryCount);
            ActorItemCount = ClampCount(actorItemCount);
            ActorEntryCount = ClampCount(actorEntryCount);
            RenderedActorCount = ClampCount(renderedActorCount);
            CreatedActorObjectCount = ClampCount(createdActorObjectCount);
            ReusedActorObjectCount = ClampCount(reusedActorObjectCount);
            DisabledActorObjectCount = ClampCount(disabledActorObjectCount);
            ActiveActorObjectCount = ClampCount(activeActorObjectCount);
            MissingSpriteCount = ClampCount(missingSpriteCount);
            GeneratedFallbackSpriteCount = ClampCount(generatedFallbackSpriteCount);
            LayeredActorCount = ClampCount(layeredActorCount);
            CreatedPartRendererCount = ClampCount(createdPartRendererCount);
            ReusedPartRendererCount = ClampCount(reusedPartRendererCount);
            MissingCatalogFrameCount = ClampCount(missingCatalogFrameCount);
            Reason = string.IsNullOrWhiteSpace(reason) ? "None" : reason;
        }

        private static int ClampCount(int value)
        {
            return value < 0 ? 0 : value;
        }
    }
}
