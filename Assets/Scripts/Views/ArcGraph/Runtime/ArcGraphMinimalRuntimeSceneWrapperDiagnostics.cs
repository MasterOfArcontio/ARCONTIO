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
    ///   <item><b>Interaction push</b>: inoltro opzionale della queue actor/object al sistema input ArcGraph.</item>
    ///   <item><b>Reason</b>: ragione sintetica del frame wrapper.</item>
    /// </list>
    /// </summary>
    public readonly struct ArcGraphMinimalRuntimeSceneWrapperDiagnostics
    {
        public readonly bool HasRuntimeMapAdapter;
        public readonly bool HasInteractionWrapper;
        public readonly bool IsWrapperEnabled;
        public readonly bool ProcessesInUpdate;
        public readonly bool RefreshesSnapshots;
        public readonly bool BuildsActorObjectQueue;
        public readonly bool PushesQueueToInteractionWrapper;
        public readonly bool EnablesInteractionWrapperAfterPush;
        public readonly bool DidBuildContext;
        public readonly bool DidProcessCoordinator;
        public readonly bool DidPushQueueToInteractionWrapper;
        public readonly ArcGraphMinimalRuntimeCoordinatorDiagnostics CoordinatorDiagnostics;
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
            bool isWrapperEnabled,
            bool processesInUpdate,
            bool refreshesSnapshots,
            bool buildsActorObjectQueue,
            bool pushesQueueToInteractionWrapper,
            bool enablesInteractionWrapperAfterPush,
            bool didBuildContext,
            bool didProcessCoordinator,
            bool didPushQueueToInteractionWrapper,
            ArcGraphMinimalRuntimeCoordinatorDiagnostics coordinatorDiagnostics,
            string reason)
        {
            HasRuntimeMapAdapter = hasRuntimeMapAdapter;
            HasInteractionWrapper = hasInteractionWrapper;
            IsWrapperEnabled = isWrapperEnabled;
            ProcessesInUpdate = processesInUpdate;
            RefreshesSnapshots = refreshesSnapshots;
            BuildsActorObjectQueue = buildsActorObjectQueue;
            PushesQueueToInteractionWrapper = pushesQueueToInteractionWrapper;
            EnablesInteractionWrapperAfterPush = enablesInteractionWrapperAfterPush;
            DidBuildContext = didBuildContext;
            DidProcessCoordinator = didProcessCoordinator;
            DidPushQueueToInteractionWrapper = didPushQueueToInteractionWrapper;
            CoordinatorDiagnostics = coordinatorDiagnostics;
            Reason = string.IsNullOrWhiteSpace(reason) ? "None" : reason;
        }
    }
}
