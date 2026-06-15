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
    ///   <item><b>terrainRenderer</b>: renderer runtime terrain opzionale e gated.</item>
    ///   <item><b>npcRenderer</b>: renderer runtime NPC opzionale e gated.</item>
    ///   <item><b>objectRenderer</b>: renderer runtime oggetti opzionale e gated.</item>
    ///   <item><b>interactionWrapper</b>: consumer opzionale della queue actor/object per input e hover.</item>
    ///   <item><b>Gate di esecuzione</b>: wrapper spento di default, Update spento di default.</item>
    ///   <item><b>_coordinator</b>: orchestratore C# passivo del bootstrap e della render queue.</item>
    ///   <item><b>ProcessFrame</b>: singolo punto manuale/automatico che costruisce context, snapshot e queue.</item>
    /// </list>
    /// </summary>
    public sealed class ArcGraphMinimalRuntimeSceneWrapper : MonoBehaviour
    {
        [SerializeField] private ArcGraphTerrainRuntimeMapGridAdapter runtimeMapAdapter;
        [SerializeField] private ArcGraphTerrainRuntimeSceneRenderer terrainRenderer;
        [SerializeField] private ArcGraphNpcRuntimeSceneRenderer npcRenderer;
        [SerializeField] private ArcGraphObjectRuntimeSceneRenderer objectRenderer;
        [SerializeField] private ArcGraphInteractionSceneAdapterWrapper interactionWrapper;
        [SerializeField] private bool wrapperEnabled;
        [SerializeField] private bool processInUpdate;
        [SerializeField] private bool refreshSnapshots = true;
        [SerializeField] private bool buildActorObjectQueue = true;
        [SerializeField] private bool renderTerrainRuntime;
        [SerializeField] private bool renderNpcRuntime;
        [SerializeField] private bool renderObjectRuntime;
        [SerializeField] private bool enableTerrainRendererBeforeRender;
        [SerializeField] private bool enableNpcRendererBeforeRender;
        [SerializeField] private bool enableObjectRendererBeforeRender;
        [SerializeField] private bool pushQueueToInteractionWrapper;
        [SerializeField] private bool enableInteractionWrapperAfterPush;
        [SerializeField] private bool logDiagnostics = true;
        [SerializeField] private int zoomLevel = 4;

        private readonly ArcGraphMinimalRuntimeCoordinator _coordinator = new();
        private ArcGraphMinimalRuntimeSceneWrapperDiagnostics _lastDiagnostics;
        private ArcGraphTerrainRuntimeSceneRendererDiagnostics _lastTerrainRendererDiagnostics;
        private ArcGraphNpcRuntimeSceneRendererDiagnostics _lastNpcRendererDiagnostics;
        private ArcGraphObjectRuntimeSceneRendererDiagnostics _lastObjectRendererDiagnostics;
        private long _sourceFrameIndex;

        public ArcGraphMinimalRuntimeSceneWrapperDiagnostics LastDiagnostics => _lastDiagnostics;
        public ArcGraphMinimalRuntimeCoordinatorDiagnostics LastCoordinatorDiagnostics => _coordinator.LastDiagnostics;
        public ArcGraphTerrainRuntimeSceneRendererDiagnostics LastTerrainRendererDiagnostics => _lastTerrainRendererDiagnostics;
        public ArcGraphNpcRuntimeSceneRendererDiagnostics LastNpcRendererDiagnostics => _lastNpcRendererDiagnostics;
        public ArcGraphObjectRuntimeSceneRendererDiagnostics LastObjectRendererDiagnostics => _lastObjectRendererDiagnostics;
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
        // SetRuntimeRendering
        // =============================================================================
        /// <summary>
        /// <para>
        /// Configura i gate di rendering runtime terrain e NPC.
        /// </para>
        ///
        /// <para><b>Switch visuale controllato</b></para>
        /// <para>
        /// Questo metodo serve al componente di switch MapGrid/ArcGraph per
        /// preparare il wrapper senza manipolare campi serializzati in modo
        /// implicito. Il wrapper resta il solo punto che decide se inoltrare dati
        /// al renderer terrain e al renderer NPC.
        /// </para>
        /// </summary>
        public void SetRuntimeRendering(
            bool renderTerrain,
            bool renderNpc,
            bool enableTerrainBeforeRender,
            bool enableNpcBeforeRender)
        {
            SetRuntimeRendering(
                renderTerrain,
                renderNpc,
                renderObject: renderNpc,
                enableTerrainBeforeRender,
                enableNpcBeforeRender,
                enableObjectBeforeRender: enableNpcBeforeRender);
        }

        // =============================================================================
        // SetRuntimeRendering
        // =============================================================================
        /// <summary>
        /// <para>
        /// Configura i gate di rendering runtime terrain, NPC e oggetti.
        /// </para>
        ///
        /// <para><b>Queue condivisa, renderer separati</b></para>
        /// <para>
        /// Actor e oggetti vengono ancora prodotti da una sola queue ordinata, ma
        /// sono materializzati da renderer separati. Questo mantiene semplice il
        /// cablaggio F12 e separa le responsabilita' tra animazioni NPC e oggetti
        /// fisici come muri, mobili e alberi.
        /// </para>
        /// </summary>
        public void SetRuntimeRendering(
            bool renderTerrain,
            bool renderNpc,
            bool renderObject,
            bool enableTerrainBeforeRender,
            bool enableNpcBeforeRender,
            bool enableObjectBeforeRender)
        {
            renderTerrainRuntime = renderTerrain;
            renderNpcRuntime = renderNpc;
            renderObjectRuntime = renderObject;
            enableTerrainRendererBeforeRender = enableTerrainBeforeRender;
            enableNpcRendererBeforeRender = enableNpcBeforeRender;
            enableObjectRendererBeforeRender = enableObjectBeforeRender;
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
        // SetTerrainRenderer
        // =============================================================================
        /// <summary>
        /// <para>
        /// Assegna esplicitamente il renderer terrain runtime che puo' ricevere il
        /// runtime ArcGraph gia' preparato dal coordinator.
        /// </para>
        /// </summary>
        public void SetTerrainRenderer(ArcGraphTerrainRuntimeSceneRenderer renderer)
        {
            terrainRenderer = renderer;
        }

        // =============================================================================
        // SetNpcRenderer
        // =============================================================================
        /// <summary>
        /// <para>
        /// Assegna esplicitamente il renderer NPC runtime che puo' ricevere la queue
        /// actor/object prodotta dal coordinator.
        /// </para>
        /// </summary>
        public void SetNpcRenderer(ArcGraphNpcRuntimeSceneRenderer renderer)
        {
            npcRenderer = renderer;
        }

        // =============================================================================
        // SetObjectRenderer
        // =============================================================================
        /// <summary>
        /// <para>
        /// Assegna esplicitamente il renderer oggetti runtime che puo' ricevere la
        /// queue actor/object prodotta dal coordinator.
        /// </para>
        /// </summary>
        public void SetObjectRenderer(ArcGraphObjectRuntimeSceneRenderer renderer)
        {
            objectRenderer = renderer;
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
        // SetInteractionRouting
        // =============================================================================
        /// <summary>
        /// <para>
        /// Configura l'inoltro della queue actor/object verso il wrapper interattivo.
        /// </para>
        ///
        /// <para><b>Principio architetturale: interazione come uscita opzionale</b></para>
        /// <para>
        /// Il wrapper minimo puo' renderizzare terrain, NPC e oggetti senza attivare
        /// picking, HUD o overlay. Questo setter permette allo switch F12 e
        /// all'auto-installer di accendere esplicitamente il percorso interattivo
        /// quando ArcGraph e' la vista attiva, senza rendere l'interaction una
        /// responsabilita' sempre accesa del renderer.
        /// </para>
        /// </summary>
        public void SetInteractionRouting(
            bool pushQueue,
            bool enableInteractionAfterPush)
        {
            pushQueueToInteractionWrapper = pushQueue;
            enableInteractionWrapperAfterPush = enableInteractionAfterPush;
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
                    didRenderTerrainRuntime: false,
                    didRenderNpcRuntime: false,
                    didRenderObjectRuntime: false,
                    didPushQueueToInteractionWrapper: false,
                    contextWorldObjectCount: 0,
                    firstContextObjectId: -1,
                    firstContextObjectDefId: string.Empty,
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
                    didRenderTerrainRuntime: false,
                    didRenderNpcRuntime: false,
                    didRenderObjectRuntime: false,
                    didPushQueueToInteractionWrapper: false,
                    contextWorldObjectCount: 0,
                    firstContextObjectId: -1,
                    firstContextObjectDefId: string.Empty,
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

            int contextWorldObjectCount = CountContextWorldObjects(context);
            int firstContextObjectId = ResolveFirstContextObjectId(context, out string firstContextObjectDefId);
            bool renderedTerrain = TryRenderTerrainRuntime(context, coordinatorDiagnostics);
            bool renderedNpc = TryRenderNpcRuntime(coordinatorDiagnostics);
            bool renderedObject = TryRenderObjectRuntime(coordinatorDiagnostics);
            bool pushedQueue = TryPushQueueToInteractionWrapper(coordinatorDiagnostics);

            _lastDiagnostics = CreateDiagnostics(
                didBuildContext: true,
                didProcessCoordinator: true,
                didRenderTerrainRuntime: renderedTerrain,
                didRenderNpcRuntime: renderedNpc,
                didRenderObjectRuntime: renderedObject,
                didPushQueueToInteractionWrapper: pushedQueue,
                contextWorldObjectCount,
                firstContextObjectId,
                firstContextObjectDefId,
                coordinatorDiagnostics,
                ResolveReason(coordinatorDiagnostics, renderedTerrain, renderedNpc, renderedObject, pushedQueue));

            LogLastDiagnostics();
            return _lastDiagnostics;
        }

        private bool TryRenderTerrainRuntime(
            ArcGraphRuntimeContext context,
            ArcGraphMinimalRuntimeCoordinatorDiagnostics coordinatorDiagnostics)
        {
            // Il renderer terrain resta opzionale: il wrapper puo' continuare a
            // processare solo dati/snapshot/queue senza creare mesh scena.
            if (!renderTerrainRuntime)
                return false;

            if (terrainRenderer == null)
                return false;

            if (!coordinatorDiagnostics.DidInitializeRuntime)
                return false;

            if (_coordinator.Runtime == null || !_coordinator.Runtime.IsInitialized)
                return false;

            if (enableTerrainRendererBeforeRender)
                terrainRenderer.SetRendererEnabled(true);

            _lastTerrainRendererDiagnostics = terrainRenderer.RenderFromRuntime(
                context,
                _coordinator.Runtime);
            return true;
        }

        private bool TryRenderNpcRuntime(
            ArcGraphMinimalRuntimeCoordinatorDiagnostics coordinatorDiagnostics)
        {
            // Gli NPC dipendono dalla queue actor/object. Se il coordinator non ha
            // costruito la queue, il renderer non viene chiamato: evita frame ambigui
            // in cui un renderer vede dati vecchi.
            if (!renderNpcRuntime)
                return false;

            if (npcRenderer == null)
                return false;

            if (!coordinatorDiagnostics.DidBuildActorObjectQueue)
                return false;

            if (enableNpcRendererBeforeRender)
                npcRenderer.SetRendererEnabled(true);

            _lastNpcRendererDiagnostics = npcRenderer.RenderFromQueue(_coordinator.RenderQueue);
            return true;
        }

        private bool TryRenderObjectRuntime(
            ArcGraphMinimalRuntimeCoordinatorDiagnostics coordinatorDiagnostics)
        {
            // Gli oggetti dipendono dalla stessa queue actor/object degli NPC. Se
            // la queue non e' stata costruita, il renderer non viene chiamato e non
            // puo' mostrare dati vecchi.
            if (!renderObjectRuntime)
                return false;

            if (objectRenderer == null)
                return false;

            if (!coordinatorDiagnostics.DidBuildActorObjectQueue)
                return false;

            if (enableObjectRendererBeforeRender)
                objectRenderer.SetRendererEnabled(true);

            _lastObjectRendererDiagnostics = objectRenderer.RenderFromQueue(_coordinator.RenderQueue);
            return true;
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
            {
                interactionWrapper.SetAdapterEnabled(true);
                interactionWrapper.SetProcessInUpdate(true);
            }

            return true;
        }

        private ArcGraphMinimalRuntimeSceneWrapperDiagnostics CreateDiagnostics(
            bool didBuildContext,
            bool didProcessCoordinator,
            bool didRenderTerrainRuntime,
            bool didRenderNpcRuntime,
            bool didRenderObjectRuntime,
            bool didPushQueueToInteractionWrapper,
            int contextWorldObjectCount,
            int firstContextObjectId,
            string firstContextObjectDefId,
            ArcGraphMinimalRuntimeCoordinatorDiagnostics coordinatorDiagnostics,
            string reason)
        {
            // La diagnostica viene costruita in un solo punto per mantenere uguale
            // il significato dei flag in tutti i gate: spento, adapter mancante,
            // context parziale o successo.
            return new ArcGraphMinimalRuntimeSceneWrapperDiagnostics(
                runtimeMapAdapter != null,
                interactionWrapper != null,
                terrainRenderer != null,
                npcRenderer != null,
                objectRenderer != null,
                wrapperEnabled,
                processInUpdate,
                refreshSnapshots,
                buildActorObjectQueue,
                renderTerrainRuntime,
                renderNpcRuntime,
                renderObjectRuntime,
                pushQueueToInteractionWrapper,
                enableInteractionWrapperAfterPush,
                didBuildContext,
                didProcessCoordinator,
                didRenderTerrainRuntime,
                didRenderNpcRuntime,
                didRenderObjectRuntime,
                didPushQueueToInteractionWrapper,
                contextWorldObjectCount,
                firstContextObjectId,
                firstContextObjectDefId,
                coordinatorDiagnostics,
                _lastTerrainRendererDiagnostics,
                _lastNpcRendererDiagnostics,
                _lastObjectRendererDiagnostics,
                reason);
        }

        private static string ResolveReason(
            ArcGraphMinimalRuntimeCoordinatorDiagnostics coordinatorDiagnostics,
            bool renderedTerrain,
            bool renderedNpc,
            bool renderedObject,
            bool pushedQueue)
        {
            if (pushedQueue)
                return "MinimalRuntimeFrameProcessedAndQueuePushed";

            if (renderedTerrain && renderedNpc && renderedObject)
                return "MinimalRuntimeFrameProcessedAndRenderedTerrainNpcObject";

            if (renderedTerrain && renderedNpc)
                return "MinimalRuntimeFrameProcessedAndRenderedTerrainNpc";

            if (renderedTerrain && renderedObject)
                return "MinimalRuntimeFrameProcessedAndRenderedTerrainObject";

            if (renderedObject)
                return "MinimalRuntimeFrameProcessedAndRenderedObject";

            if (renderedTerrain)
                return "MinimalRuntimeFrameProcessedAndRenderedTerrain";

            if (renderedNpc)
                return "MinimalRuntimeFrameProcessedAndRenderedNpc";

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
                ", terrainRenderer=" + _lastDiagnostics.HasTerrainRenderer +
                ", npcRenderer=" + _lastDiagnostics.HasNpcRenderer +
                ", objectRenderer=" + _lastDiagnostics.HasObjectRenderer +
                ", interactionWrapper=" + _lastDiagnostics.HasInteractionWrapper +
                ", contextBuilt=" + _lastDiagnostics.DidBuildContext +
                ", coordinatorProcessed=" + _lastDiagnostics.DidProcessCoordinator +
                ", coordinatorReason=" + _lastDiagnostics.CoordinatorDiagnostics.Reason +
                ", terrainSnapshots=" + _lastDiagnostics.CoordinatorDiagnostics.TerrainSnapshotCount +
                ", actors=" + _lastDiagnostics.CoordinatorDiagnostics.QueueActorCount +
                ", objects=" + _lastDiagnostics.CoordinatorDiagnostics.QueueObjectCount +
                ", entries=" + _lastDiagnostics.CoordinatorDiagnostics.QueueEntryCount +
                ", renderedTerrain=" + _lastDiagnostics.DidRenderTerrainRuntime +
                ", terrainReason=" + _lastDiagnostics.TerrainRendererDiagnostics.Reason +
                ", renderedNpc=" + _lastDiagnostics.DidRenderNpcRuntime +
                ", npcReason=" + _lastDiagnostics.NpcRendererDiagnostics.Reason +
                ", renderedObject=" + _lastDiagnostics.DidRenderObjectRuntime +
                ", objectReason=" + _lastDiagnostics.ObjectRendererDiagnostics.Reason +
                ", contextWorldObjects=" + _lastDiagnostics.ContextWorldObjectCount +
                ", firstContextObjectId=" + _lastDiagnostics.FirstContextObjectId +
                ", firstContextObjectDefId=" + _lastDiagnostics.FirstContextObjectDefId +
                ", pushedInteraction=" + _lastDiagnostics.DidPushQueueToInteractionWrapper);
        }

        // =============================================================================
        // CountContextWorldObjects
        // =============================================================================
        /// <summary>
        /// <para>
        /// Conta gli oggetti presenti nel World ricevuto dal context del frame.
        /// </para>
        ///
        /// <para><b>Diagnostica del ponte, non lettura decisionale</b></para>
        /// <para>
        /// Questo conteggio serve solo a capire se ArcGraph sta leggendo lo stesso
        /// World che i DevTools hanno modificato con F3. Non viene usato per
        /// decidere rendering, pathfinding o logica simulativa.
        /// </para>
        /// </summary>
        private static int CountContextWorldObjects(ArcGraphRuntimeContext context)
        {
            return context?.World?.Objects != null
                ? context.World.Objects.Count
                : 0;
        }

        // =============================================================================
        // ResolveFirstContextObjectId
        // =============================================================================
        /// <summary>
        /// <para>
        /// Recupera un primo oggetto diagnostico dal World ricevuto da ArcGraph.
        /// </para>
        ///
        /// <para><b>Uso previsto</b></para>
        /// <para>
        /// Se dopo un piazzamento F3 questo valore resta <c>-1</c>, il problema e'
        /// prima della render queue: ArcGraph non sta leggendo un World con oggetti.
        /// Se invece mostra <c>wall_stone</c> ma la queue resta vuota, il problema e'
        /// nello snapshot/layer/LOD oggetti.
        /// </para>
        /// </summary>
        private static int ResolveFirstContextObjectId(
            ArcGraphRuntimeContext context,
            out string firstObjectDefId)
        {
            firstObjectDefId = string.Empty;

            if (context?.World?.Objects == null || context.World.Objects.Count <= 0)
                return -1;

            foreach (var pair in context.World.Objects)
            {
                firstObjectDefId = pair.Value != null ? pair.Value.DefId : string.Empty;
                return pair.Key;
            }

            return -1;
        }
    }
}
