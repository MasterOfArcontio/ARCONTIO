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
        [SerializeField] private bool logDiagnostics;
        [SerializeField] private bool useScreenAsViewport = true;
        [SerializeField] private int manualViewportPixelWidth = 1920;
        [SerializeField] private int manualViewportPixelHeight = 1080;
        [SerializeField] private Vector2 manualViewportOriginPixels = Vector2.zero;
        [SerializeField] private MonoBehaviour interactionConsumerBehaviour;

        private readonly ArcGraphInteractionSceneAdapterContract _contract = new();

        private ArcGraphMapViewConfig _config;
        private ArcGraphViewState _viewState;
        private ArcGraphRenderQueue _renderQueue;
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
        // SetConfig
        // =============================================================================
        /// <summary>
        /// <para>
        /// Imposta la configurazione view ArcGraph ricevuta da un producer esterno.
        /// </para>
        /// </summary>
        public void SetConfig(ArcGraphMapViewConfig config)
        {
            ArcGraphMapViewConfig previousConfig = ResolveConfig();
            int previousDefaultZoomLevel = previousConfig.DefaultZoomLevel;
            bool shouldMoveDefaultViewState =
                _viewState != null &&
                _viewState.ActiveZoomLevel == previousDefaultZoomLevel;

            _config = config;
            ArcGraphMapViewConfig currentConfig = ResolveConfig();

            if (_viewState == null)
            {
                _viewState = ArcGraphViewState.CreateDefault(currentConfig);
                return;
            }

            // Quando un producer sostituisce la configurazione provvisoria con una
            // configurazione runtime piu' specifica, lo stato vista puo' essere
            // ancora fermo sul vecchio default. In quel solo caso lo riallineiamo
            // al nuovo default; se invece l'utente ha gia' zoomato o pannato, non
            // sovrascriviamo la sua scelta a ogni frame.
            if (shouldMoveDefaultViewState &&
                previousDefaultZoomLevel != currentConfig.DefaultZoomLevel)
            {
                _viewState.SetZoomLevel(currentConfig.DefaultZoomLevel, currentConfig);
            }
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
            ResolveViewport(out int viewportWidth, out int viewportHeight);
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

                LogLastDiagnostics();
                return _lastWrapperDiagnostics;
            }

            ArcGraphMapViewConfig config = ResolveConfig();
            ArcGraphViewState viewState = EnsureViewState();
            IArcGraphInteractionFrameConsumer consumer = ResolveConsumer();

            ArcGraphViewInputFrame input = BuildInputFrame(mouse);
            var sceneFrame = new ArcGraphInteractionSceneFrame(
                input,
                viewportWidth,
                viewportHeight,
                dispatchToConsumer,
                _sourceFrameIndex++);

            ArcGraphInteractionSceneAdapterDiagnostics contractDiagnostics =
                _contract.ProcessFrame(
                    config,
                    viewState,
                    sceneFrame,
                    _renderQueue,
                    consumer);

            _lastWrapperDiagnostics = CreateWrapperDiagnostics(
                hasMouse,
                config != null,
                viewState != null,
                hasValidViewport,
                didProcessContract: true,
                contractDiagnostics,
                contractDiagnostics.Reason);

            LogLastDiagnostics();
            return _lastWrapperDiagnostics;
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

        private void ResolveViewport(out int viewportWidth, out int viewportHeight)
        {
            if (useScreenAsViewport)
            {
                viewportWidth = Screen.width;
                viewportHeight = Screen.height;
                return;
            }

            viewportWidth = manualViewportPixelWidth;
            viewportHeight = manualViewportPixelHeight;
        }

        private ArcGraphViewInputFrame BuildInputFrame(Mouse mouse)
        {
            if (mouse == null)
                return ArcGraphViewInputFrame.Empty();

            Vector2 position = mouse.position.ReadValue();
            Vector2 delta = mouse.delta.ReadValue();
            float scrollY = mouse.scroll.ReadValue().y;
            int wheelStepDelta = ResolveWheelStep(scrollY);
            bool isPointerOverUi = EventSystem.current != null && EventSystem.current.IsPointerOverGameObject();

            Vector2 viewportPoint = useScreenAsViewport
                ? position
                : position - manualViewportOriginPixels;

            return new ArcGraphViewInputFrame(
                wheelStepDelta,
                mouse.middleButton.isPressed,
                delta.x,
                delta.y,
                viewportPoint.x,
                viewportPoint.y,
                true,
                isPointerOverUi,
                mouse.leftButton.wasPressedThisFrame);
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

        private void LogLastDiagnostics()
        {
            if (!logDiagnostics)
                return;

            Debug.Log(
                "[ArcGraphInteractionSceneAdapterWrapper] " + _lastWrapperDiagnostics.Reason +
                ", enabled=" + _lastWrapperDiagnostics.IsAdapterEnabled +
                ", mouse=" + _lastWrapperDiagnostics.HasMouse +
                ", viewport=" + _lastWrapperDiagnostics.HasValidViewport +
                ", queue=" + _lastWrapperDiagnostics.HasRenderQueue +
                ", consumer=" + _lastWrapperDiagnostics.HasConsumer +
                ", processed=" + _lastWrapperDiagnostics.DidProcessContract +
                ", target=" + _lastWrapperDiagnostics.ContractDiagnostics.TargetKind +
                ", actor=" + _lastWrapperDiagnostics.ContractDiagnostics.ActorId +
                ", object=" + _lastWrapperDiagnostics.ContractDiagnostics.ObjectId);
        }
    }
}
