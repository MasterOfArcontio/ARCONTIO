using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;

namespace Arcontio.View.ArcGraph
{
    // =============================================================================
    // ArcGraphInteractionSceneAdapterWrapperDiagnostics
    // =============================================================================
    /// <summary>
    /// <para>
    /// Diagnostica del wrapper Unity che alimenta il contratto interattivo ArcGraph.
    /// </para>
    ///
    /// <para><b>Principio architetturale: input fisico confinato</b></para>
    /// <para>
    /// Questo risultato rende visibile se il wrapper era abilitato, se il mouse era
    /// disponibile, se il viewport era valido, se esisteva una queue actor/object e
    /// se il contratto C# e' stato processato. Il wrapper e' l'unico punto di questa
    /// catena autorizzato a leggere input fisico Unity.
    /// </para>
    ///
    /// <para><b>Struttura interna:</b></para>
    /// <list type="bullet">
    ///   <item><b>IsAdapterEnabled</b>: gate principale del wrapper.</item>
    ///   <item><b>HasMouse</b>: disponibilita' di <c>Mouse.current</c>.</item>
    ///   <item><b>HasConfig/HasViewState</b>: stato ArcGraph disponibile.</item>
    ///   <item><b>HasRenderQueue</b>: queue actor/object assegnata da un producer esterno.</item>
    ///   <item><b>HasConsumer</b>: consumer interattivo esterno disponibile.</item>
    ///   <item><b>DidProcessContract</b>: chiamata effettiva al contratto C#.</item>
    ///   <item><b>ContractDiagnostics</b>: esito del contratto passivo.</item>
    ///   <item><b>Reason</b>: ragione sintetica del frame wrapper.</item>
    /// </list>
    /// </summary>
    public readonly struct ArcGraphInteractionSceneAdapterWrapperDiagnostics
    {
        public readonly bool IsAdapterEnabled;
        public readonly bool HasMouse;
        public readonly bool HasConfig;
        public readonly bool HasViewState;
        public readonly bool HasRenderQueue;
        public readonly bool HasConsumer;
        public readonly bool UsesScreenAsViewport;
        public readonly bool HasValidViewport;
        public readonly bool DidProcessContract;
        public readonly ArcGraphInteractionSceneAdapterDiagnostics ContractDiagnostics;
        public readonly string Reason;

        public ArcGraphInteractionSceneAdapterWrapperDiagnostics(
            bool isAdapterEnabled,
            bool hasMouse,
            bool hasConfig,
            bool hasViewState,
            bool hasRenderQueue,
            bool hasConsumer,
            bool usesScreenAsViewport,
            bool hasValidViewport,
            bool didProcessContract,
            ArcGraphInteractionSceneAdapterDiagnostics contractDiagnostics,
            string reason)
        {
            IsAdapterEnabled = isAdapterEnabled;
            HasMouse = hasMouse;
            HasConfig = hasConfig;
            HasViewState = hasViewState;
            HasRenderQueue = hasRenderQueue;
            HasConsumer = hasConsumer;
            UsesScreenAsViewport = usesScreenAsViewport;
            HasValidViewport = hasValidViewport;
            DidProcessContract = didProcessContract;
            ContractDiagnostics = contractDiagnostics;
            Reason = string.IsNullOrWhiteSpace(reason) ? "None" : reason;
        }
    }

    // =============================================================================
    // ArcGraphInteractionSceneAdapterWrapper
    // =============================================================================
    /// <summary>
    /// <para>
    /// Wrapper Unity passivo per produrre frame interattivi ArcGraph da input fisico.
    /// </para>
    ///
    /// <para><b>Principio architetturale: wrapper scena, non strumento operativo</b></para>
    /// <para>
    /// Questo componente legge mouse, rotellina, tasto centrale e stato UI, poi
    /// trasforma questi dati in <c>ArcGraphInteractionSceneFrame</c> e chiama
    /// <c>ArcGraphInteractionSceneAdapterContract</c>. Non seleziona NPC, non invia
    /// comandi, non apre DevTools, non crea pannelli, non legge <c>World</c>, non
    /// legge <c>SimulationHost</c>, non cerca <c>MapGridWorldView</c> e non crea
    /// oggetti scena. Il suo output e' solo diagnostica e, se richiesto, dispatch a
    /// un consumer esplicito.
    /// </para>
    ///
    /// <para><b>Struttura interna:</b></para>
    /// <list type="bullet">
    ///   <item><b>adapterEnabled</b>: gate principale, falso di default.</item>
    ///   <item><b>processInUpdate</b>: polling opzionale, falso di default.</item>
    ///   <item><b>useScreenAsViewport</b>: usa lo schermo come viewport provvisorio.</item>
    ///   <item><b>manualViewport*</b>: viewport manuale per test futuri.</item>
    ///   <item><b>Set*</b>: ingressi espliciti da producer esterni.</item>
    ///   <item><b>ProcessCurrentFrame</b>: unica chiamata che legge input Unity.</item>
    /// </list>
    /// </summary>
    public sealed class ArcGraphInteractionSceneAdapterWrapper : MonoBehaviour
    {
        [SerializeField] private bool adapterEnabled;
        [SerializeField] private bool processInUpdate;
        [SerializeField] private bool dispatchToConsumer;
        [SerializeField] private bool viewInputEnabled = true;
        [SerializeField] private bool useScreenAsViewport = true;
        [SerializeField] private int manualViewportPixelWidth = 1920;
        [SerializeField] private int manualViewportPixelHeight = 1080;
        [SerializeField] private Vector2 manualViewportOriginPixels = Vector2.zero;
        [SerializeField] private MonoBehaviour interactionConsumerBehaviour;
        [SerializeField] private Camera sceneCamera;
        [SerializeField] private bool syncSceneCameraZoomToViewState;
        [SerializeField] private float minimumSceneCameraOrthographicSize = 0.5f;

        private readonly ArcGraphInteractionSceneAdapterContract _contract = new();

        private ArcGraphMapViewConfig _config;
        private ArcGraphViewState _viewState;
        private ArcGraphRenderQueue _renderQueue;
        private IReadOnlyList<ArcGraphVegetationRenderItem> _vegetationItems;
        private IArcGraphInteractionFrameConsumer _consumer;
        private long _sourceFrameIndex;
        private ArcGraphInteractionSceneAdapterWrapperDiagnostics _lastWrapperDiagnostics;

        public ArcGraphInteractionSceneAdapterWrapperDiagnostics LastWrapperDiagnostics => _lastWrapperDiagnostics;
        public ArcGraphInteractionSceneAdapterDiagnostics LastContractDiagnostics => _contract.LastDiagnostics;
        public ArcGraphInteractionFrame LastInteractionFrame => _contract.LastInteractionFrame;
        public ArcGraphViewState ViewState => EnsureViewState();
        public bool AdapterEnabled => adapterEnabled;

        // =============================================================================
        // Update
        // =============================================================================
        /// <summary>
        /// <para>
        /// Processa opzionalmente un frame in automatico.
        /// </para>
        ///
        /// <para><b>Default spento</b></para>
        /// <para>
        /// Sia <c>adapterEnabled</c> sia <c>processInUpdate</c> sono falsi di default.
        /// Il wrapper non introduce costo per frame se non viene esplicitamente
        /// abilitato da Inspector o da un chiamante esterno.
        /// </para>
        /// </summary>
        private void Update()
        {
            if (!processInUpdate)
                return;

            ProcessCurrentFrame();
        }

        // =============================================================================
        // SetAdapterEnabled
        // =============================================================================
        /// <summary>
        /// <para>
        /// Abilita o disabilita il gate principale del wrapper.
        /// </para>
        /// </summary>
        public void SetAdapterEnabled(bool enabled)
        {
            adapterEnabled = enabled;
        }

        // =============================================================================
        // SetProcessInUpdate
        // =============================================================================
        /// <summary>
        /// <para>
        /// Abilita o disabilita il processing automatico in <c>Update</c>.
        /// </para>
        ///
        /// <para><b>Gate esplicito del costo per frame</b></para>
        /// <para>
        /// Il wrapper puo' restare cablato in scena senza leggere input ogni frame.
        /// Lo switch visuale ArcGraph abilita questo flag solo quando la vista
        /// ArcGraph e' attiva e i consumer interattivi devono ricevere hover,
        /// selection o overlay cella.
        /// </para>
        /// </summary>
        public void SetProcessInUpdate(bool enabled)
        {
            processInUpdate = enabled;
        }

        // =============================================================================
        // SetDispatchToConsumer
        // =============================================================================
        /// <summary>
        /// <para>
        /// Abilita o disabilita la consegna del frame interattivo al consumer
        /// esterno configurato.
        /// </para>
        ///
        /// <para><b>Principio architetturale: dispatch esplicito</b></para>
        /// <para>
        /// Il wrapper puo' aggiornare soltanto pan/zoom/picking oppure puo' anche
        /// consegnare il frame a moduli runtime come selection e placement preview.
        /// Questo setter rende il passaggio dichiarato dall'auto-installer, senza
        /// dipendere da un flag Inspector fragile.
        /// </para>
        /// </summary>
        public void SetDispatchToConsumer(bool enabled)
        {
            dispatchToConsumer = enabled;
        }

        // =============================================================================
        // SetConfig
        // =============================================================================
        /// <summary>
        /// <para>
        /// Imposta la configurazione view ArcGraph ricevuta da un producer esterno.
        /// </para>
        /// </summary>
        public void SetConfig(ArcGraphMapViewConfig config)
        {
            _config = config;
            ArcGraphMapViewConfig currentConfig = ResolveConfig();

            if (_viewState == null)
            {
                _viewState = ArcGraphViewState.CreateDefault(currentConfig);
                return;
            }

            _viewState.SetCenterCell(
                _viewState.CenterCellX,
                _viewState.CenterCellY,
                currentConfig);
        }

        // =============================================================================
        // SetViewState
        // =============================================================================
        /// <summary>
        /// <para>
        /// Imposta lo stato view ArcGraph ricevuto da un producer esterno.
        /// </para>
        /// </summary>
        public void SetViewState(ArcGraphViewState viewState)
        {
            _viewState = viewState;
        }

        // =============================================================================
        // SetRenderQueue
        // =============================================================================
        /// <summary>
        /// <para>
        /// Imposta la queue actor/object prodotta da ArcGraph.
        /// </para>
        /// </summary>
        public void SetRenderQueue(ArcGraphRenderQueue renderQueue)
        {
            _renderQueue = renderQueue;
        }

        // =============================================================================
        // SetVegetationItems
        // =============================================================================
        /// <summary>
        /// <para>
        /// Imposta gli item vegetazione prodotti dal percorso ArcGraph corrente.
        /// </para>
        ///
        /// <para><b>Contratto read-only</b></para>
        /// <para>
        /// Il wrapper non costruisce e non filtra la Biosfera. Conserva solo la
        /// lista value-only ricevuta dal coordinator per permettere al boundary di
        /// riconoscere piante fisiche selezionabili.
        /// </para>
        /// </summary>
        public void SetVegetationItems(IReadOnlyList<ArcGraphVegetationRenderItem> vegetationItems)
        {
            _vegetationItems = vegetationItems;
        }

        // =============================================================================
        // SetConsumer
        // =============================================================================
        /// <summary>
        /// <para>
        /// Imposta un consumer interattivo esterno.
        /// </para>
        /// </summary>
        public void SetConsumer(IArcGraphInteractionFrameConsumer consumer)
        {
            _consumer = consumer;
        }

        // =============================================================================
        // SetSceneCamera
        // =============================================================================
        /// <summary>
        /// <para>
        /// Assegna la camera Unity osservata dal wrapper interattivo.
        /// </para>
        ///
        /// <para><b>Principio architetturale: camera come uscita visuale, non input simulativo</b></para>
        /// <para>
        /// Lo zoom fisico e' posseduto dal controller camera ArcGraph. Questo
        /// setter evita ricerche scena diffuse e permette all'auto-installer di
        /// dichiarare esplicitamente quale camera viene usata per picking e
        /// sincronizzazione confinata.
        /// </para>
        /// </summary>
        public void SetSceneCamera(Camera camera)
        {
            sceneCamera = camera;
        }

        // =============================================================================
        // SetSceneCameraZoomSyncEnabled
        // =============================================================================
        /// <summary>
        /// <para>
        /// Abilita o disabilita la sincronizzazione difensiva della camera.
        /// </para>
        ///
        /// <para><b>Principio architetturale: zoom fisico continuo</b></para>
        /// <para>
        /// La rotellina non produce piu' livelli logici dentro
        /// <c>ArcGraphViewState</c>. Questo gate resta solo come ponte difensivo
        /// per portare la camera su un valore base continuo quando un chiamante lo
        /// abilita esplicitamente.
        /// </para>
        /// </summary>
        public void SetSceneCameraZoomSyncEnabled(bool enabled)
        {
            syncSceneCameraZoomToViewState = enabled;
        }

        // =============================================================================
        // SetViewInputEnabled
        // =============================================================================
        /// <summary>
        /// <para>
        /// Abilita o disabilita l'applicazione di pan e zoom dentro il contratto
        /// interattivo.
        /// </para>
        ///
        /// <para><b>Principio architetturale: camera controller separato dal picking</b></para>
        /// <para>
        /// Durante il passaggio al COCC, il wrapper interattivo deve continuare a
        /// leggere puntatore e click per hover, HUD e selezione, ma non deve piu'
        /// cambiare lo <c>ArcGraphViewState</c> con rotellina o drag. In questo
        /// modo esiste un solo proprietario effettivo di zoom, pan e clamp camera.
        /// </para>
        /// </summary>
        public void SetViewInputEnabled(bool enabled)
        {
            viewInputEnabled = enabled;
        }

        // =============================================================================
        // ProcessCurrentFrameFromInspector
        // =============================================================================
        /// <summary>
        /// <para>
        /// Entry point manuale da Inspector per QA controllata.
        /// </para>
        /// </summary>
        [ContextMenu("ArcGraph/Process Interaction Scene Adapter Frame")]
        public void ProcessCurrentFrameFromInspector()
        {
            ProcessCurrentFrame();
        }

        // =============================================================================
        // ProcessCurrentFrame
        // =============================================================================
        /// <summary>
        /// <para>
        /// Legge l'input Unity corrente e produce un frame interattivo ArcGraph.
        /// </para>
        ///
        /// <para><b>Sequenza intenzionale</b></para>
        /// <para>
        /// Prima viene verificato il gate. Poi vengono risolti viewport e input
        /// fisico. Solo alla fine viene chiamato il contratto passivo, che aggiorna
        /// view state e boundary. Nessun tool operativo viene attivato qui.
        /// </para>
        /// </summary>
        public ArcGraphInteractionSceneAdapterWrapperDiagnostics ProcessCurrentFrame()
        {
            Mouse mouse = Mouse.current;
            bool hasMouse = mouse != null;
            ResolveViewport(
                out int viewportWidth,
                out int viewportHeight,
                out Vector2 viewportOriginPixels);
            bool hasValidViewport = viewportWidth > 0 && viewportHeight > 0;

            if (!adapterEnabled)
            {
                _lastWrapperDiagnostics = CreateWrapperDiagnostics(
                    hasMouse,
                    _config != null,
                    _viewState != null,
                    hasValidViewport,
                    didProcessContract: false,
                    default,
                    "AdapterDisabled");

                return _lastWrapperDiagnostics;
            }

            ArcGraphMapViewConfig config = ResolveConfig();
            ArcGraphViewState viewState = EnsureViewState();
            IArcGraphInteractionFrameConsumer consumer = ResolveConsumer();

            ArcGraphViewInputFrame input = BuildInputFrame(mouse, viewportOriginPixels);
            bool hasSceneResolvedCell = TryResolveSceneCameraCell(
                config,
                input,
                out ArcGraphCellCoord sceneResolvedCell);
            var sceneFrame = new ArcGraphInteractionSceneFrame(
                input,
                viewportWidth,
                viewportHeight,
                dispatchToConsumer,
                _sourceFrameIndex++,
                hasSceneResolvedCell,
                sceneResolvedCell);

            ArcGraphInteractionSceneAdapterDiagnostics contractDiagnostics =
                _contract.ProcessFrame(
                    config,
                    viewState,
                    sceneFrame,
                    _renderQueue != null ? _renderQueue.ActorItems : null,
                    _renderQueue != null ? _renderQueue.ObjectItems : null,
                    _vegetationItems,
                    consumer);

            ApplySceneCameraZoomIfEnabled(
                config,
                viewState,
                viewportHeight);

            _lastWrapperDiagnostics = CreateWrapperDiagnostics(
                hasMouse,
                config != null,
                viewState != null,
                hasValidViewport,
                didProcessContract: true,
                contractDiagnostics,
                contractDiagnostics.Reason);

            return _lastWrapperDiagnostics;
        }

        // =============================================================================
        // ApplySceneCameraZoomIfEnabled
        // =============================================================================
        /// <summary>
        /// <para>
        /// Applica alla camera Unity il valore ortografico base ArcGraph.
        /// </para>
        ///
        /// <para><b>Principio architetturale: ponte temporaneo view-state -> camera</b></para>
        /// <para>
        /// Questa funzione e' un ponte piccolo e confinato: legge solo config e
        /// stato view, applica la dimensione ortografica base e porta la camera sul
        /// centro dichiarato dallo stato. Non legge il World, non sposta NPC e non
        /// modifica dati simulativi.
        /// </para>
        /// </summary>
        private void ApplySceneCameraZoomIfEnabled(
            ArcGraphMapViewConfig config,
            ArcGraphViewState viewState,
            int viewportPixelHeight)
        {
            if (!syncSceneCameraZoomToViewState || config == null || viewState == null)
                return;

            Camera camera = ResolveSceneCamera();
            if (camera == null)
                return;

            float targetOrthographicSize = Mathf.Max(
                minimumSceneCameraOrthographicSize,
                config.DefaultOrthographicSize);

            bool didChangeOrthographicSize =
                !Mathf.Approximately(camera.orthographicSize, targetOrthographicSize);

            if (didChangeOrthographicSize)
                camera.orthographicSize = targetOrthographicSize;

            // Lo zoom-to-pointer e' gia' stato applicato al centro logico da
            // ArcGraphViewController. Qui non compensiamo una seconda volta la
            // camera: la sincronizziamo al centro finale dello ViewState, cosi'
            // terrain, muri, NPC e culling restano nello stesso sistema di
            // coordinate.
            SyncSceneCameraCenterToViewState(camera, viewState);
        }

        private Camera ResolveSceneCamera()
        {
            if (sceneCamera != null)
                return sceneCamera;

            sceneCamera = Camera.main;
            return sceneCamera;
        }

        // =============================================================================
        // SyncSceneCameraCenterToViewState
        // =============================================================================
        /// <summary>
        /// <para>
        /// Porta la camera Unity sul centro mappa dichiarato dallo stato ArcGraph.
        /// </para>
        ///
        /// <para><b>Principio architetturale: una sola fonte per il centro vista</b></para>
        /// <para>
        /// Il centro autorevole della vista e' <c>ArcGraphViewState</c>. Se la
        /// camera fisica accumula un offset indipendente, il terrain viene cullato
        /// usando una finestra e oggetti/NPC vengono osservati da un'altra. Questo
        /// metodo elimina quel disallineamento, ma resta confinato alla camera:
        /// non modifica mappa, oggetti o simulazione.
        /// </para>
        /// </summary>
        private static void SyncSceneCameraCenterToViewState(
            Camera camera,
            ArcGraphViewState viewState)
        {
            if (camera == null || viewState == null)
                return;

            Vector3 current = camera.transform.position;
            Vector3 desired = new Vector3(
                viewState.CenterCellX,
                viewState.CenterCellY,
                current.z);
            Vector3 offset = desired - current;
            offset.z = 0f;

            if (offset.sqrMagnitude < 0.000001f)
                return;

            camera.transform.position += offset;
        }

        private ArcGraphMapViewConfig ResolveConfig()
        {
            return _config ?? ArcGraphMapViewConfig.CreateDefaultV033();
        }

        private ArcGraphViewState EnsureViewState()
        {
            _viewState ??= ArcGraphViewState.CreateDefault(ResolveConfig());
            return _viewState;
        }

        private IArcGraphInteractionFrameConsumer ResolveConsumer()
        {
            if (_consumer != null)
                return _consumer;

            return interactionConsumerBehaviour as IArcGraphInteractionFrameConsumer;
        }

        private void ResolveViewport(
            out int viewportWidth,
            out int viewportHeight,
            out Vector2 viewportOriginPixels)
        {
            if (TryResolveSceneCameraPixelViewport(out Rect cameraViewport))
            {
                viewportWidth = Mathf.RoundToInt(cameraViewport.width);
                viewportHeight = Mathf.RoundToInt(cameraViewport.height);
                viewportOriginPixels = cameraViewport.position;
                return;
            }

            if (useScreenAsViewport)
            {
                viewportWidth = Screen.width;
                viewportHeight = Screen.height;
                viewportOriginPixels = Vector2.zero;
                return;
            }

            viewportWidth = manualViewportPixelWidth;
            viewportHeight = manualViewportPixelHeight;
            viewportOriginPixels = manualViewportOriginPixels;
        }

        private bool TryResolveSceneCameraPixelViewport(out Rect viewport)
        {
            viewport = default;

            Camera camera = ResolveSceneCamera();
            if (camera == null)
                return false;

            Rect pixelRect = camera.pixelRect;
            if (pixelRect.width <= 0f || pixelRect.height <= 0f)
                return false;

            viewport = pixelRect;
            return true;
        }

        private ArcGraphViewInputFrame BuildInputFrame(
            Mouse mouse,
            Vector2 viewportOriginPixels)
        {
            if (mouse == null)
                return ArcGraphViewInputFrame.Empty();

            Vector2 position = mouse.position.ReadValue();
            Vector2 delta = mouse.delta.ReadValue();
            bool isPointerOverUi = EventSystem.current != null && EventSystem.current.IsPointerOverGameObject();

            // Durante il soft retirement di MapGrid conserviamo il gesto pratico
            // gia' usato dall'operatore: RMB trascina la mappa. Il controller
            // ArcGraph riceve comunque un input astratto di pan, senza conoscere
            // quale tasto fisico lo ha prodotto.
            bool isPanButtonHeld =
                viewInputEnabled &&
                (mouse.middleButton.isPressed ||
                 mouse.rightButton.isPressed);

            Vector2 viewportPoint = position - viewportOriginPixels;

            int wheelStepDelta = viewInputEnabled
                ? ResolveWheelStep(mouse.scroll.ReadValue().y)
                : 0;

            return new ArcGraphViewInputFrame(
                wheelStepDelta,
                isPanButtonHeld,
                viewInputEnabled ? delta.x : 0f,
                viewInputEnabled ? delta.y : 0f,
                viewportPoint.x,
                viewportPoint.y,
                true,
                isPointerOverUi,
                mouse.leftButton.wasPressedThisFrame,
                mouse.rightButton.wasPressedThisFrame);
        }

        // =============================================================================
        // TryResolveSceneCameraCell
        // =============================================================================
        /// <summary>
        /// <para>
        /// Converte il puntatore del frame nella cella realmente osservata dalla
        /// camera Unity corrente.
        /// </para>
        ///
        /// <para><b>Principio architetturale: coordinate runtime unificate</b></para>
        /// <para>
        /// Il controller camera ArcGraph applica pan e zoom direttamente alla camera
        /// ortografica. Per questo il picking produttivo deve usare la stessa camera
        /// che disegna la mappa, invece di ricostruire una finestra logica parallela.
        /// Il wrapper resta il solo punto autorizzato a leggere la camera; al
        /// contratto passivo viene consegnata solo una cella value-type.
        /// </para>
        /// </summary>
        private bool TryResolveSceneCameraCell(
            ArcGraphMapViewConfig config,
            ArcGraphViewInputFrame input,
            out ArcGraphCellCoord cell)
        {
            cell = new ArcGraphCellCoord(0, 0, 0);

            if (!input.HasPointerScreenPosition || input.IsPointerOverUi)
                return false;

            Camera camera = ResolveSceneCamera();
            if (camera == null)
                return false;

            Rect pixelRect = camera.pixelRect;
            if (pixelRect.width <= 0f || pixelRect.height <= 0f)
                return false;

            Vector2 absoluteScreenPoint = new Vector2(
                input.PointerScreenX + pixelRect.x,
                input.PointerScreenY + pixelRect.y);

            if (!pixelRect.Contains(absoluteScreenPoint))
                return false;

            float worldPlaneDistance = ResolveWorldPlaneDistance(camera);
            Vector3 worldPoint = camera.ScreenToWorldPoint(new Vector3(
                absoluteScreenPoint.x,
                absoluteScreenPoint.y,
                worldPlaneDistance));

            int cellX = Mathf.FloorToInt(worldPoint.x);
            int cellY = Mathf.FloorToInt(worldPoint.y);
            int cellZ = ArcGraphZLevelPolicy.CurrentRuntimeZLevel;

            ArcGraphMapViewConfig safeConfig = config ?? ArcGraphMapViewConfig.CreateDefaultV033();
            if (cellX < 0 ||
                cellY < 0 ||
                cellX >= safeConfig.MapWidthCells ||
                cellY >= safeConfig.MapHeightCells)
            {
                return false;
            }

            cell = new ArcGraphCellCoord(cellX, cellY, cellZ);
            return true;
        }

        // =============================================================================
        // ResolveWorldPlaneDistance
        // =============================================================================
        /// <summary>
        /// <para>
        /// Calcola la distanza dal piano world-space usato da ArcGraph.
        /// </para>
        /// </summary>
        private static float ResolveWorldPlaneDistance(Camera camera)
        {
            if (camera == null)
                return 0f;

            float distance = 0f - camera.transform.position.z;
            return Mathf.Abs(distance) > 0.001f
                ? Mathf.Abs(distance)
                : Mathf.Max(0.001f, camera.nearClipPlane);
        }

        private static int ResolveWheelStep(float scrollY)
        {
            if (scrollY > 0.01f)
                return 1;

            if (scrollY < -0.01f)
                return -1;

            return 0;
        }

        private ArcGraphInteractionSceneAdapterWrapperDiagnostics CreateWrapperDiagnostics(
            bool hasMouse,
            bool hasConfig,
            bool hasViewState,
            bool hasValidViewport,
            bool didProcessContract,
            ArcGraphInteractionSceneAdapterDiagnostics contractDiagnostics,
            string reason)
        {
            IArcGraphInteractionFrameConsumer consumer = ResolveConsumer();
            return new ArcGraphInteractionSceneAdapterWrapperDiagnostics(
                adapterEnabled,
                hasMouse,
                hasConfig,
                hasViewState,
                _renderQueue != null,
                consumer != null,
                useScreenAsViewport,
                hasValidViewport,
                didProcessContract,
                contractDiagnostics,
                reason);
        }

    }
}
