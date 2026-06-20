using UnityEngine;

namespace Arcontio.View.ArcGraph
{
    // =============================================================================
    // ArcGraphInteractionRenderQueueWiringProbeDiagnostics
    // =============================================================================
    /// <summary>
    /// <para>
    /// Diagnostica sintetica del ponte temporaneo tra queue actor/object ArcGraph e
    /// wrapper interattivo ArcGraph.
    /// </para>
    ///
    /// <para><b>Principio architetturale: wiring osservabile e non permanente</b></para>
    /// <para>
    /// Questo risultato serve a capire se il probe ha ricevuto provider, wrapper,
    /// world, layer actor/object e queue valida. Il dato e' intenzionalmente piatto:
    /// spiega il gate visuale senza diventare un sistema produttivo nascosto.
    /// </para>
    ///
    /// <para><b>Struttura interna:</b></para>
    /// <list type="bullet">
    ///   <item><b>HasRuntimeAdapter</b>: provider runtime ArcGraph assegnato.</item>
    ///   <item><b>HasInteractionWrapper</b>: wrapper interazione assegnato.</item>
    ///   <item><b>DidInitializeBootstrap</b>: bootstrap temporaneo riuscito.</item>
    ///   <item><b>HasActorLayer/HasObjectLayer</b>: layer necessari presenti.</item>
    ///   <item><b>QueueEntryCount</b>: entry actor/object prodotte.</item>
    ///   <item><b>DidPushToWrapper</b>: queue consegnata al wrapper.</item>
    ///   <item><b>Reason</b>: esito sintetico.</item>
    /// </list>
    /// </summary>
    public readonly struct ArcGraphInteractionRenderQueueWiringProbeDiagnostics
    {
        public readonly bool HasRuntimeAdapter;
        public readonly bool HasInteractionWrapper;
        public readonly bool HasContext;
        public readonly bool HasWorld;
        public readonly bool DidInitializeBootstrap;
        public readonly bool HasActorLayer;
        public readonly bool HasObjectLayer;
        public readonly int QueueActorCount;
        public readonly int QueueObjectCount;
        public readonly int QueueEntryCount;
        public readonly bool DidPushToWrapper;
        public readonly string Reason;

        public ArcGraphInteractionRenderQueueWiringProbeDiagnostics(
            bool hasRuntimeAdapter,
            bool hasInteractionWrapper,
            bool hasContext,
            bool hasWorld,
            bool didInitializeBootstrap,
            bool hasActorLayer,
            bool hasObjectLayer,
            int queueActorCount,
            int queueObjectCount,
            int queueEntryCount,
            bool didPushToWrapper,
            string reason)
        {
            HasRuntimeAdapter = hasRuntimeAdapter;
            HasInteractionWrapper = hasInteractionWrapper;
            HasContext = hasContext;
            HasWorld = hasWorld;
            DidInitializeBootstrap = didInitializeBootstrap;
            HasActorLayer = hasActorLayer;
            HasObjectLayer = hasObjectLayer;
            QueueActorCount = queueActorCount < 0 ? 0 : queueActorCount;
            QueueObjectCount = queueObjectCount < 0 ? 0 : queueObjectCount;
            QueueEntryCount = queueEntryCount < 0 ? 0 : queueEntryCount;
            DidPushToWrapper = didPushToWrapper;
            Reason = string.IsNullOrWhiteSpace(reason) ? "None" : reason;
        }
    }

    // =============================================================================
    // ArcGraphInteractionRenderQueueWiringProbe
    // =============================================================================
    /// <summary>
    /// <para>
    /// Probe manuale che costruisce una <c>ArcGraphRenderQueue</c> actor/object e
    /// la consegna al wrapper interattivo ArcGraph.
    /// </para>
    ///
    /// <para><b>Principio architetturale: ponte di gate, non renderer produttivo</b></para>
    /// <para>
    /// Il componente esiste per il gate visuale dei consumer modulari. Riceve da
    /// Inspector un <c>ArcGraphRuntimeContextProvider</c> e un
    /// <c>ArcGraphInteractionSceneAdapterWrapper</c>, costruisce un bootstrap
    /// ArcGraph temporaneo in memoria, produce la queue actor/object e la passa al
    /// wrapper tramite <c>SetRenderQueue</c>. Non crea sprite, non crea GameObject di
    /// mappa, non legge input fisico, non invia comandi e non salva scene.
    /// </para>
    ///
    /// <para><b>Struttura interna:</b></para>
    /// <list type="bullet">
    ///   <item><b>runtimeContextProvider</b>: sorgente esplicita del context runtime.</item>
    ///   <item><b>interactionWrapper</b>: wrapper che ricevera' la queue.</item>
    ///   <item><b>PushRenderQueueToInteractionWrapper</b>: build e consegna manuale.</item>
    ///   <item><b>ResolveLodProfile</b>: usa la policy LOD ArcGraph esistente.</item>
    ///   <item><b>BuildDiagnostics</b>: rende leggibile l'esito del gate.</item>
    /// </list>
    /// </summary>
    public sealed class ArcGraphInteractionRenderQueueWiringProbe : MonoBehaviour
    {
        [SerializeField] private ArcGraphRuntimeContextProvider runtimeContextProvider;
        [SerializeField] private ArcGraphInteractionSceneAdapterWrapper interactionWrapper;
        [SerializeField] private bool pushOnStart;
        [SerializeField] private bool enableWrapperAfterPush;
        [SerializeField] private bool logDiagnostics = true;

        private ArcGraphInteractionRenderQueueWiringProbeDiagnostics _lastDiagnostics;
        private ArcGraphRenderQueue _lastQueue;

        public ArcGraphInteractionRenderQueueWiringProbeDiagnostics LastDiagnostics => _lastDiagnostics;
        public ArcGraphRenderQueue LastQueue => _lastQueue;

        // =============================================================================
        // Start
        // =============================================================================
        /// <summary>
        /// <para>
        /// Esegue opzionalmente il push della queue a inizio scena.
        /// </para>
        ///
        /// <para><b>Default spento</b></para>
        /// <para>
        /// Il flag e' falso di default. Il probe non deve introdurre lavoro runtime
        /// automatico se l'operatore non lo abilita esplicitamente da Inspector.
        /// </para>
        /// </summary>
        private void Start()
        {
            if (!pushOnStart)
                return;

            PushRenderQueueToInteractionWrapper();
        }

        // =============================================================================
        // PushRenderQueueToInteractionWrapperFromInspector
        // =============================================================================
        /// <summary>
        /// <para>
        /// Entry point void per il context menu Unity.
        /// </para>
        /// </summary>
        [ContextMenu("ArcGraph/Push Interaction Render Queue To Wrapper")]
        public void PushRenderQueueToInteractionWrapperFromInspector()
        {
            PushRenderQueueToInteractionWrapper();
        }

        // =============================================================================
        // PushRenderQueueToInteractionWrapper
        // =============================================================================
        /// <summary>
        /// <para>
        /// Costruisce una queue actor/object temporanea e la consegna al wrapper.
        /// </para>
        ///
        /// <para><b>Sequenza controllata</b></para>
        /// <para>
        /// Il metodo verifica prima i riferimenti scena, poi costruisce il context,
        /// inizializza il bootstrap ArcGraph, recupera layer actor/object, genera la
        /// queue e solo alla fine chiama <c>SetRenderQueue</c>. Ogni uscita anticipata
        /// produce diagnostica leggibile.
        /// </para>
        /// </summary>
        public ArcGraphInteractionRenderQueueWiringProbeDiagnostics PushRenderQueueToInteractionWrapper()
        {
            if (runtimeContextProvider == null || interactionWrapper == null)
            {
                _lastQueue = null;
                _lastDiagnostics = BuildDiagnostics(
                    context: null,
                    didInitializeBootstrap: false,
                    hasActorLayer: false,
                    hasObjectLayer: false,
                    queue: null,
                    didPushToWrapper: false,
                    reason: runtimeContextProvider == null ? "RuntimeContextProviderMissing" : "InteractionWrapperMissing");
                LogLastDiagnostics();
                return _lastDiagnostics;
            }

            ArcGraphRuntimeContext context = runtimeContextProvider.BuildTerrainRuntimeContext();
            var runtime = new ArcGraphBootstrapRuntime();
            bool initialized = runtime.Initialize(
                context,
                ArcGraphBootstrapOptions.CreateDefault());

            ArcGraphActorLayer actorLayer = null;
            ArcGraphObjectLayer objectLayer = null;
            bool hasLayerStack = initialized && runtime.LayerStack != null;
            bool hasActorLayer = hasLayerStack
                                 && runtime.LayerStack.TryGetLayer<ArcGraphActorLayer>(out actorLayer);
            bool hasObjectLayer = hasLayerStack
                                  && runtime.LayerStack.TryGetLayer<ArcGraphObjectLayer>(out objectLayer);

            if (!initialized || !hasActorLayer || !hasObjectLayer)
            {
                _lastQueue = null;
                _lastDiagnostics = BuildDiagnostics(
                    context,
                    initialized,
                    hasActorLayer,
                    hasObjectLayer,
                    queue: null,
                    didPushToWrapper: false,
                    reason: !initialized ? "BootstrapInitializeFailed" : "ActorObjectLayersMissing");

                runtime.Dispose();
                LogLastDiagnostics();
                return _lastDiagnostics;
            }

            var queue = new ArcGraphRenderQueue();
            var queueBuilder = new ArcGraphRenderQueueBuilder();
            ArcGraphZoomLodProfile lodProfile = ResolveLodProfile();
            queueBuilder.Build(actorLayer, objectLayer, lodProfile, queue);

            interactionWrapper.SetConfig(ArcGraphMapViewConfig.CreateDefaultV033());
            interactionWrapper.SetRenderQueue(queue);

            if (enableWrapperAfterPush)
                interactionWrapper.SetAdapterEnabled(true);

            _lastQueue = queue;
            _lastDiagnostics = BuildDiagnostics(
                context,
                initialized,
                hasActorLayer,
                hasObjectLayer,
                queue,
                didPushToWrapper: true,
                reason: queue.Entries.Count > 0 ? "RenderQueuePushedToWrapper" : "RenderQueuePushedButEmpty");

            runtime.Dispose();
            LogLastDiagnostics();
            return _lastDiagnostics;
        }

        private ArcGraphZoomLodProfile ResolveLodProfile()
        {
            return ArcGraphZoomLodPolicy.ResolveFullDetail();
        }

        private ArcGraphInteractionRenderQueueWiringProbeDiagnostics BuildDiagnostics(
            ArcGraphRuntimeContext context,
            bool didInitializeBootstrap,
            bool hasActorLayer,
            bool hasObjectLayer,
            ArcGraphRenderQueue queue,
            bool didPushToWrapper,
            string reason)
        {
            return new ArcGraphInteractionRenderQueueWiringProbeDiagnostics(
                runtimeContextProvider != null,
                interactionWrapper != null,
                context != null,
                context != null && context.HasWorld,
                didInitializeBootstrap,
                hasActorLayer,
                hasObjectLayer,
                queue != null ? queue.ActorItems.Count : 0,
                queue != null ? queue.ObjectItems.Count : 0,
                queue != null ? queue.Entries.Count : 0,
                didPushToWrapper,
                reason);
        }

        private void LogLastDiagnostics()
        {
            if (!logDiagnostics)
                return;

            Debug.Log(
                "[ArcGraphInteractionRenderQueueWiringProbe] " + _lastDiagnostics.Reason +
                ", adapter=" + _lastDiagnostics.HasRuntimeAdapter +
                ", wrapper=" + _lastDiagnostics.HasInteractionWrapper +
                ", context=" + _lastDiagnostics.HasContext +
                ", world=" + _lastDiagnostics.HasWorld +
                ", bootstrap=" + _lastDiagnostics.DidInitializeBootstrap +
                ", actorLayer=" + _lastDiagnostics.HasActorLayer +
                ", objectLayer=" + _lastDiagnostics.HasObjectLayer +
                ", actors=" + _lastDiagnostics.QueueActorCount +
                ", objects=" + _lastDiagnostics.QueueObjectCount +
                ", entries=" + _lastDiagnostics.QueueEntryCount +
                ", pushed=" + _lastDiagnostics.DidPushToWrapper);
        }
    }
}
