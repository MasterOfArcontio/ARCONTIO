namespace Arcontio.View.ArcGraph
{
    // =============================================================================
    // ArcGraphMinimalRuntimeSceneWrapperDiagnostics
    // =============================================================================
    /// <summary>
    /// <para>
    /// Diagnostica del wrapper Unity che accende il percorso runtime minimo ArcGraph.
    /// </para>
    ///
    /// <para><b>Principio architetturale: confine scena sottile e ispezionabile</b></para>
    /// <para>
    /// Questa struttura descrive se il wrapper scena era abilitato, se aveva gli
    /// adapter richiesti, se ha chiamato il coordinator passivo e se ha inoltrato
    /// la render queue actor/object al wrapper interattivo. Non contiene riferimenti
    /// a GameObject, World, MapGridData o altri oggetti mutabili: espone solo esiti
    /// copiati, contatori e ragioni leggibili.
    /// </para>
    ///
    /// <para><b>Struttura interna:</b></para>
    /// <list type="bullet">
    ///   <item><b>Wrapper gate</b>: stato di abilitazione e modalita' Update.</item>
    ///   <item><b>Adapter gate</b>: presenza del runtime adapter MapGrid e del wrapper interattivo opzionale.</item>
    ///   <item><b>Coordinator gate</b>: esecuzione del coordinator minimo e relativa diagnostica.</item>
    ///   <item><b>Renderer gate</b>: inoltro opzionale verso renderer terrain e NPC runtime.</item>
    ///   <item><b>Interaction push</b>: inoltro opzionale della queue actor/object al sistema input ArcGraph.</item>
    ///   <item><b>Reason</b>: ragione sintetica del frame wrapper.</item>
    /// </list>
    /// </summary>
    public readonly struct ArcGraphMinimalRuntimeSceneWrapperDiagnostics
    {
        public readonly bool HasRuntimeMapAdapter;
        public readonly bool HasInteractionWrapper;
        public readonly bool HasTerrainRenderer;
        public readonly bool HasNpcRenderer;
        public readonly bool IsWrapperEnabled;
        public readonly bool ProcessesInUpdate;
        public readonly bool RefreshesSnapshots;
        public readonly bool BuildsActorObjectQueue;
        public readonly bool RendersTerrainRuntime;
        public readonly bool RendersNpcRuntime;
        public readonly bool PushesQueueToInteractionWrapper;
        public readonly bool EnablesInteractionWrapperAfterPush;
        public readonly bool DidBuildContext;
        public readonly bool DidProcessCoordinator;
        public readonly bool DidRenderTerrainRuntime;
        public readonly bool DidRenderNpcRuntime;
        public readonly bool DidPushQueueToInteractionWrapper;
        public readonly ArcGraphMinimalRuntimeCoordinatorDiagnostics CoordinatorDiagnostics;
        public readonly ArcGraphTerrainRuntimeSceneRendererDiagnostics TerrainRendererDiagnostics;
        public readonly ArcGraphNpcRuntimeSceneRendererDiagnostics NpcRendererDiagnostics;
        public readonly string Reason;

        // =============================================================================
        // ArcGraphMinimalRuntimeSceneWrapperDiagnostics
        // =============================================================================
        /// <summary>
        /// <para>
        /// Costruisce un risultato diagnostico immutabile del wrapper scena.
        /// </para>
        ///
        /// <para><b>Snapshot diagnostico</b></para>
        /// <para>
        /// I valori vengono copiati al momento del frame. Il chiamante puo' quindi
        /// leggere l'esito in Inspector o in log senza ottenere accesso a sorgenti
        /// vive della simulazione.
        /// </para>
        /// </summary>
        public ArcGraphMinimalRuntimeSceneWrapperDiagnostics(
            bool hasRuntimeMapAdapter,
            bool hasInteractionWrapper,
            bool hasTerrainRenderer,
            bool hasNpcRenderer,
            bool isWrapperEnabled,
            bool processesInUpdate,
            bool refreshesSnapshots,
            bool buildsActorObjectQueue,
            bool rendersTerrainRuntime,
            bool rendersNpcRuntime,
            bool pushesQueueToInteractionWrapper,
            bool enablesInteractionWrapperAfterPush,
            bool didBuildContext,
            bool didProcessCoordinator,
            bool didRenderTerrainRuntime,
            bool didRenderNpcRuntime,
            bool didPushQueueToInteractionWrapper,
            ArcGraphMinimalRuntimeCoordinatorDiagnostics coordinatorDiagnostics,
            ArcGraphTerrainRuntimeSceneRendererDiagnostics terrainRendererDiagnostics,
            ArcGraphNpcRuntimeSceneRendererDiagnostics npcRendererDiagnostics,
            string reason)
        {
            HasRuntimeMapAdapter = hasRuntimeMapAdapter;
            HasInteractionWrapper = hasInteractionWrapper;
            HasTerrainRenderer = hasTerrainRenderer;
            HasNpcRenderer = hasNpcRenderer;
            IsWrapperEnabled = isWrapperEnabled;
            ProcessesInUpdate = processesInUpdate;
            RefreshesSnapshots = refreshesSnapshots;
            BuildsActorObjectQueue = buildsActorObjectQueue;
            RendersTerrainRuntime = rendersTerrainRuntime;
            RendersNpcRuntime = rendersNpcRuntime;
            PushesQueueToInteractionWrapper = pushesQueueToInteractionWrapper;
            EnablesInteractionWrapperAfterPush = enablesInteractionWrapperAfterPush;
            DidBuildContext = didBuildContext;
            DidProcessCoordinator = didProcessCoordinator;
            DidRenderTerrainRuntime = didRenderTerrainRuntime;
            DidRenderNpcRuntime = didRenderNpcRuntime;
            DidPushQueueToInteractionWrapper = didPushQueueToInteractionWrapper;
            CoordinatorDiagnostics = coordinatorDiagnostics;
            TerrainRendererDiagnostics = terrainRendererDiagnostics;
            NpcRendererDiagnostics = npcRendererDiagnostics;
            Reason = string.IsNullOrWhiteSpace(reason) ? "None" : reason;
        }
    }
}
