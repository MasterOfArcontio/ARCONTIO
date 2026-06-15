namespace Arcontio.View.ArcGraph
{
    // =============================================================================
    // ArcGraphObjectRuntimeSceneRendererDiagnostics
    // =============================================================================
    /// <summary>
    /// <para>
    /// Diagnostica sintetica dell'ultimo passaggio del renderer runtime oggetti.
    /// </para>
    ///
    /// <para><b>Principio architetturale: rendering oggetti spiegabile</b></para>
    /// <para>
    /// Il renderer oggetti materializza in scena solo entry <c>Object</c> gia'
    /// presenti nella <c>ArcGraphRenderQueue</c>. Questa diagnostica espone quanti
    /// oggetti sono stati pianificati, creati, riusati, disattivati o saltati per
    /// sprite mancante, senza esporre riferimenti mutabili a GameObject o asset.
    /// </para>
    ///
    /// <para><b>Struttura interna:</b></para>
    /// <list type="bullet">
    ///   <item><b>RendererEnabled</b>: gate principale del renderer.</item>
    ///   <item><b>ContractSafe</b>: validita' del contratto runtime.</item>
    ///   <item><b>HasQueue</b>: presenza della queue actor/object.</item>
    ///   <item><b>ObjectEntryCount</b>: oggetti pianificati dal piano actor/object.</item>
    ///   <item><b>RenderedObjectCount</b>: oggetti applicati in scena.</item>
    ///   <item><b>Created/Reused/Disabled</b>: uso del pool runtime.</item>
    ///   <item><b>MissingSpriteCount</b>: sprite key non risolte dal resolver.</item>
    ///   <item><b>Reason</b>: esito leggibile in Console.</item>
    /// </list>
    /// </summary>
    public readonly struct ArcGraphObjectRuntimeSceneRendererDiagnostics
    {
        public readonly bool RendererEnabled;
        public readonly bool HasContract;
        public readonly bool ContractSafe;
        public readonly bool HasQueue;
        public readonly bool HasSpriteResolver;
        public readonly bool BuiltPlan;
        public readonly int QueueEntryCount;
        public readonly int ObjectItemCount;
        public readonly int ObjectEntryCount;
        public readonly int RenderedObjectCount;
        public readonly int CreatedObjectCount;
        public readonly int ReusedObjectCount;
        public readonly int DisabledObjectCount;
        public readonly int ActiveObjectCount;
        public readonly int MissingSpriteCount;
        public readonly string Reason;

        // =============================================================================
        // ArcGraphObjectRuntimeSceneRendererDiagnostics
        // =============================================================================
        /// <summary>
        /// <para>
        /// Costruisce una diagnostica immutabile del renderer oggetti.
        /// </para>
        /// </summary>
        public ArcGraphObjectRuntimeSceneRendererDiagnostics(
            bool rendererEnabled,
            bool hasContract,
            bool contractSafe,
            bool hasQueue,
            bool hasSpriteResolver,
            bool builtPlan,
            int queueEntryCount,
            int objectItemCount,
            int objectEntryCount,
            int renderedObjectCount,
            int createdObjectCount,
            int reusedObjectCount,
            int disabledObjectCount,
            int activeObjectCount,
            int missingSpriteCount,
            string reason)
        {
            RendererEnabled = rendererEnabled;
            HasContract = hasContract;
            ContractSafe = contractSafe;
            HasQueue = hasQueue;
            HasSpriteResolver = hasSpriteResolver;
            BuiltPlan = builtPlan;
            QueueEntryCount = ClampCount(queueEntryCount);
            ObjectItemCount = ClampCount(objectItemCount);
            ObjectEntryCount = ClampCount(objectEntryCount);
            RenderedObjectCount = ClampCount(renderedObjectCount);
            CreatedObjectCount = ClampCount(createdObjectCount);
            ReusedObjectCount = ClampCount(reusedObjectCount);
            DisabledObjectCount = ClampCount(disabledObjectCount);
            ActiveObjectCount = ClampCount(activeObjectCount);
            MissingSpriteCount = ClampCount(missingSpriteCount);
            Reason = string.IsNullOrWhiteSpace(reason) ? "None" : reason;
        }

        private static int ClampCount(int value)
        {
            return value < 0 ? 0 : value;
        }
    }
}
