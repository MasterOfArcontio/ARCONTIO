using UnityEngine;

namespace Arcontio.View.ArcGraph
{
    // =============================================================================
    // ArcGraphMinimalRuntimeSceneWrapper
    // =============================================================================
    /// <summary>
    /// <para>
    /// Wrapper Unity minimale per accendere il percorso runtime stabile ArcGraph.
    /// </para>
    ///
    /// <para><b>Principio architetturale: MonoBehaviour come frontiera, non come motore</b></para>
    /// <para>
    /// Il componente esiste solo per collegare oggetti scena gia' espliciti a un
    /// coordinator C# passivo. Riceve da Inspector il ponte dati
    /// <c>ArcGraphTerrainRuntimeMapGridAdapter</c> e, opzionalmente, il wrapper
    /// interattivo <c>ArcGraphInteractionSceneAdapterWrapper</c>. Non crea
    /// GameObject, non crea renderer, non legge <c>SimulationHost</c>, non cerca
    /// oggetti in scena, non invia comandi e non sostituisce ancora MapGrid come
    /// renderer produttivo.
    /// </para>
    ///
    /// <para><b>Struttura interna:</b></para>
    /// <list type="bullet">
    ///   <item><b>runtimeMapAdapter</b>: sorgente esplicita del context ArcGraph provvisorio.</item>
    ///   <item><b>interactionWrapper</b>: consumer opzionale della queue actor/object per input e hover.</item>
    ///   <item><b>Gate di esecuzione</b>: wrapper spento di default, Update spento di default.</item>
    ///   <item><b>_coordinator</b>: orchestratore C# passivo del bootstrap e della render queue.</item>
    ///   <item><b>ProcessFrame</b>: singolo punto manuale/automatico che costruisce context, snapshot e queue.</item>
    /// </list>
    /// </summary>
    public sealed class ArcGraphMinimalRuntimeSceneWrapper : MonoBehaviour
    {
        [SerializeField] private ArcGraphTerrainRuntimeMapGridAdapter runtimeMapAdapter;
        [SerializeField] private ArcGraphInteractionSceneAdapterWrapper interactionWrapper;
        [SerializeField] private bool wrapperEnabled;
        [SerializeField] private bool processInUpdate;
        [SerializeField] private bool refreshSnapshots = true;
        [SerializeField] private bool buildActorObjectQueue = true;
        [SerializeField] private bool pushQueueToInteractionWrapper;
        [SerializeField] private bool enableInteractionWrapperAfterPush;
        [SerializeField] private bool logDiagnostics = true;
        [SerializeField] private int zoomLevel = 4;

        private readonly ArcGraphMinimalRuntimeCoordinator _coordinator = new();
        private ArcGraphMinimalRuntimeSceneWrapperDiagnostics _lastDiagnostics;
        private long _sourceFrameIndex;

        public ArcGraphMinimalRuntimeSceneWrapperDiagnostics LastDiagnostics => _lastDiagnostics;
        public ArcGraphMinimalRuntimeCoordinatorDiagnostics LastCoordinatorDiagnostics => _coordinator.LastDiagnostics;
        public ArcGraphRenderQueue RenderQueue => _coordinator.RenderQueue;
        public bool WrapperEnabled => wrapperEnabled;

        // =============================================================================
        // Update
        // =============================================================================
        /// <summary>
        /// <para>
        /// Processa opzionalmente un frame ArcGraph minimo durante il loop Unity.
        /// </para>
        ///
        /// <para><b>Default senza costo continuo</b></para>
        /// <para>
        /// Il flag <c>processInUpdate</c> e' falso di default. Il wrapper puo' quindi
        /// restare in scena come cablaggio preparatorio senza produrre lavoro per
        /// frame finche' non viene acceso esplicitamente.
        /// </para>
        /// </summary>
        private void Update()
        {
            // Questo controllo e' intenzionalmente il primo del metodo: se Update
            // resta spento, non costruiamo context, non aggiorniamo snapshot e non
            // tocchiamo la queue.
            if (!processInUpdate)
                return;

            ProcessFrame();
        }

        // =============================================================================
        // OnDestroy
        // =============================================================================
        /// <summary>
        /// <para>
        /// Rilascia lo stato derivato del coordinator quando il wrapper viene distrutto.
        /// </para>
        ///
        /// <para><b>Cleanup locale</b></para>
        /// <para>
        /// Il cleanup riguarda solo cache ArcGraph possedute dal coordinator. Non
        /// distrugge GameObject di scena, non modifica MapGrid e non modifica il
        /// World simulativo.
        /// </para>
        /// </summary>
        private void OnDestroy()
        {
            _coordinator.Dispose();
        }

        // =============================================================================
        // SetWrapperEnabled
        // =============================================================================
        /// <summary>
        /// <para>
        /// Abilita o disabilita il gate principale del wrapper scena.
        /// </para>
        /// </summary>
        public void SetWrapperEnabled(bool enabled)
        {
            wrapperEnabled = enabled;
        }

        // =============================================================================
        // SetProcessInUpdate
        // =============================================================================
        /// <summary>
        /// <para>
        /// Abilita o disabilita il processing automatico in <c>Update</c>.
        /// </para>
        /// </summary>
        public void SetProcessInUpdate(bool enabled)
        {
            processInUpdate = enabled;
        }

        // =============================================================================
        // SetRuntimeMapAdapter
        // =============================================================================
        /// <summary>
        /// <para>
        /// Assegna esplicitamente l'adapter runtime MapGrid usato come sorgente dati.
        /// </para>
        /// </summary>
        public void SetRuntimeMapAdapter(ArcGraphTerrainRuntimeMapGridAdapter adapter)
        {
            runtimeMapAdapter = adapter;
        }

        // =============================================================================
        // SetInteractionWrapper
        // =============================================================================
        /// <summary>
        /// <para>
        /// Assegna esplicitamente il wrapper interattivo che puo' ricevere la queue.
        /// </para>
        /// </summary>
        public void SetInteractionWrapper(ArcGraphInteractionSceneAdapterWrapper wrapper)
        {
            interactionWrapper = wrapper;
        }

        // =============================================================================
        // ProcessFrameFromInspector
        // =============================================================================
        /// <summary>
        /// <para>
        /// Entry point manuale da Inspector per il gate runtime minimo.
        /// </para>
        /// </summary>
        [ContextMenu("ArcGraph/Process Minimal Runtime Frame")]
        public void ProcessFrameFromInspector()
        {
            ProcessFrame();
        }

        // =============================================================================
        // ProcessFrame
        // =============================================================================
        /// <summary>
        /// <para>
        /// Processa un frame del runtime minimo ArcGraph.
        /// </para>
        ///
        /// <para><b>Sequenza intenzionale</b></para>
        /// <para>
        /// Il wrapper prima controlla il proprio gate, poi richiede il context al
        /// runtime adapter, poi chiama il coordinator passivo e solo alla fine,
        /// se richiesto, inoltra la queue actor/object al wrapper interattivo. La
        /// direzione resta una sola: dati runtime gia' esistenti verso ArcGraph,
        /// non comandi ArcGraph verso la simulazione.
        /// </para>
        /// </summary>
        public ArcGraphMinimalRuntimeSceneWrapperDiagnostics ProcessFrame()
        {
            if (!wrapperEnabled)
            {
                // Anche a wrapper spento chiediamo al coordinator di processare un
                // frame disabilitato: cosi' eventuali queue derivate vengono pulite
                // e la diagnostica resta coerente.
                ArcGraphMinimalRuntimeCoordinatorDiagnostics disabledDiagnostics =
                    _coordinator.Process(ArcGraphMinimalRuntimeCoordinatorFrame.CreateDisabled());

                _lastDiagnostics = CreateDiagnostics(
                    didBuildContext: false,
                    didProcessCoordinator: true,
                    didPushQueueToInteractionWrapper: false,
                    disabledDiagnostics,
                    "WrapperDisabled");

                LogLastDiagnostics();
                return _lastDiagnostics;
            }

            if (runtimeMapAdapter == null)
            {
                // Il wrapper non cerca un adapter nella scena. Se manca il
                // riferimento esplicito, il gate fallisce in modo leggibile e senza
                // fallback nascosti.
                ArcGraphMinimalRuntimeCoordinatorDiagnostics disabledDiagnostics =
                    _coordinator.Process(ArcGraphMinimalRuntimeCoordinatorFrame.CreateDisabled());

                _lastDiagnostics = CreateDiagnostics(
                    didBuildContext: false,
                    didProcessCoordinator: true,
                    didPushQueueToInteractionWrapper: false,
                    disabledDiagnostics,
                    "RuntimeMapAdapterMissing");

                LogLastDiagnostics();
                return _lastDiagnostics;
            }

            ArcGraphRuntimeContext context = runtimeMapAdapter.BuildTerrainRuntimeContext();
            var frame = new ArcGraphMinimalRuntimeCoordinatorFrame(
                context,
                isCoordinatorEnabled: true,
                shouldRefreshSnapshots: refreshSnapshots,
                shouldBuildActorObjectQueue: buildActorObjectQueue,
                zoomLevel: zoomLevel,
                sourceTick: _sourceFrameIndex++);

            ArcGraphMinimalRuntimeCoordinatorDiagnostics coordinatorDiagnostics =
                _coordinator.Process(frame);

            bool pushedQueue = TryPushQueueToInteractionWrapper(coordinatorDiagnostics);

            _lastDiagnostics = CreateDiagnostics(
                didBuildContext: true,
                didProcessCoordinator: true,
                didPushQueueToInteractionWrapper: pushedQueue,
                coordinatorDiagnostics,
                ResolveReason(coordinatorDiagnostics, pushedQueue));

            LogLastDiagnostics();
            return _lastDiagnostics;
        }

        private bool TryPushQueueToInteractionWrapper(
            ArcGraphMinimalRuntimeCoordinatorDiagnostics coordinatorDiagnostics)
        {
            // L'inoltro della queue resta opzionale. In questo modo il wrapper puo'
            // essere usato anche solo per testare bootstrap/snapshot senza attivare
            // la catena interattiva.
            if (!pushQueueToInteractionWrapper)
                return false;

            if (interactionWrapper == null)
                return false;

            if (!coordinatorDiagnostics.DidBuildActorObjectQueue)
                return false;

            // Il wrapper interattivo riceve solo config view e queue gia' derivate.
            // Non riceve World, MapGridData o accesso agli adapter.
            interactionWrapper.SetConfig(ArcGraphMapViewConfig.CreateDefaultV033());
            interactionWrapper.SetRenderQueue(_coordinator.RenderQueue);

            if (enableInteractionWrapperAfterPush)
                interactionWrapper.SetAdapterEnabled(true);

            return true;
        }

        private ArcGraphMinimalRuntimeSceneWrapperDiagnostics CreateDiagnostics(
            bool didBuildContext,
            bool didProcessCoordinator,
            bool didPushQueueToInteractionWrapper,
            ArcGraphMinimalRuntimeCoordinatorDiagnostics coordinatorDiagnostics,
            string reason)
        {
            // La diagnostica viene costruita in un solo punto per mantenere uguale
            // il significato dei flag in tutti i gate: spento, adapter mancante,
            // context parziale o successo.
            return new ArcGraphMinimalRuntimeSceneWrapperDiagnostics(
                runtimeMapAdapter != null,
                interactionWrapper != null,
                wrapperEnabled,
                processInUpdate,
                refreshSnapshots,
                buildActorObjectQueue,
                pushQueueToInteractionWrapper,
                enableInteractionWrapperAfterPush,
                didBuildContext,
                didProcessCoordinator,
                didPushQueueToInteractionWrapper,
                coordinatorDiagnostics,
                reason);
        }

        private static string ResolveReason(
            ArcGraphMinimalRuntimeCoordinatorDiagnostics coordinatorDiagnostics,
            bool pushedQueue)
        {
            if (pushedQueue)
                return "MinimalRuntimeFrameProcessedAndQueuePushed";

            if (!coordinatorDiagnostics.DidInitializeRuntime)
                return coordinatorDiagnostics.Reason;

            if (coordinatorDiagnostics.DidBuildActorObjectQueue)
                return "MinimalRuntimeFrameProcessed";

            return coordinatorDiagnostics.Reason;
        }

        private void LogLastDiagnostics()
        {
            if (!logDiagnostics)
                return;

            Debug.Log(
                "[ArcGraphMinimalRuntimeSceneWrapper] " + _lastDiagnostics.Reason +
                " wrapperEnabled=" + _lastDiagnostics.IsWrapperEnabled +
                ", update=" + _lastDiagnostics.ProcessesInUpdate +
                ", runtimeAdapter=" + _lastDiagnostics.HasRuntimeMapAdapter +
                ", interactionWrapper=" + _lastDiagnostics.HasInteractionWrapper +
                ", contextBuilt=" + _lastDiagnostics.DidBuildContext +
                ", coordinatorProcessed=" + _lastDiagnostics.DidProcessCoordinator +
                ", coordinatorReason=" + _lastDiagnostics.CoordinatorDiagnostics.Reason +
                ", terrainSnapshots=" + _lastDiagnostics.CoordinatorDiagnostics.TerrainSnapshotCount +
                ", actors=" + _lastDiagnostics.CoordinatorDiagnostics.QueueActorCount +
                ", objects=" + _lastDiagnostics.CoordinatorDiagnostics.QueueObjectCount +
                ", entries=" + _lastDiagnostics.CoordinatorDiagnostics.QueueEntryCount +
                ", pushedInteraction=" + _lastDiagnostics.DidPushQueueToInteractionWrapper);
        }
    }
}
