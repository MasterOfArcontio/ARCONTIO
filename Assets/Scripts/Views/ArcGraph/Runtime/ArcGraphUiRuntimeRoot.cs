using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.Events;
#if ENABLE_INPUT_SYSTEM
using UnityEngine.InputSystem.UI;
#endif
using UnityEngine.UI;

namespace Arcontio.View.ArcGraph
{
    // =============================================================================
    // ArcGraphUiRuntimeRoot
    // =============================================================================
    /// <summary>
    /// <para>
    /// Root runtime provvisorio della UI definitiva ArcGraph.
    /// </para>
    ///
    /// <para><b>Principio architetturale: UI come view-side shell, non simulazione</b></para>
    /// <para>
    /// Questo componente costruisce solo una gerarchia UGUI stabile: Canvas,
    /// pannelli principali e archetipi visuali minimi. Non legge <c>World</c>, non
    /// legge <c>SimulationHost</c>, non invia comandi, non seleziona NPC, non
    /// modifica job e non sostituisce ancora i controller autorizzati. I contenuti
    /// sono placeholder strutturali per verificare layout, densita' e gerarchia.
    /// </para>
    ///
    /// <para><b>Struttura interna:</b></para>
    /// <list type="bullet">
    ///   <item><b>Canvas</b>: overlay UGUI screen-space con scaler 1920x1080.</item>
    ///   <item><b>ArcUIRoot</b>: root full-screen dei blocchi definitivi.</item>
    ///   <item><b>TopBar/BottomActionBar</b>: cornici fisse che delimitano la viewport mappa.</item>
    ///   <item><b>TopBar</b>: stato globale simulazione, ancora non bindato a ViewModel.</item>
    ///   <item><b>MapViewport</b>: rettangolo UI che delimita la camera mappa ArcGraph.</item>
    ///   <item><b>RightInspector</b>: inspector tabbed pronto per target diversi.</item>
    ///   <item><b>BottomActionBar</b>: categorie operative principali.</item>
    ///   <item><b>ActionPanel</b>: pannello contestuale aperto dalla categoria attiva.</item>
    ///   <item><b>OverlayRoot/DebugRoot</b>: root separati per overlay e debug.</item>
    /// </list>
    /// </summary>
    public sealed class ArcGraphUiRuntimeRoot : MonoBehaviour
    {
        private const string CanvasName = "ArcUIRoot_AutoCanvas";
        private const string RootName = "ArcUIRoot";

        private const float TopBarHeight = 44f;
        private const float RightInspectorWidth = 350f;
        private const float BottomActionBarHeight = 92f;
        private const float ActionPanelTopY = 600f;
        private const float ActionPanelHeight = ActionPanelTopY - BottomActionBarHeight;
        private const float BiospherePanelPadding = 5f;
        private const float BiosphereLeftColumnWidth = 100f;
        private const float BiosphereColumnGap = 5f;
        private const float OuterMargin = 0f;
        private const float BiosphereToolbarHeight = 20f;
        private const float BiosphereChipHeight = 18f;
        private const string RuntimeUiSchemaMarkerName = "ArcUIRuntimeSchema_BiosphereGraphs_v4";
        private const string BiosphereWorldGraphsGroupKey = "biosphere_world_graphs";
        private const string BiosphereAreaGraphsGroupKey = "biosphere_area_graphs";
        private static readonly Color TopButtonNormalColor = ColorFromHex("#17212B", 0.92f);
        private static readonly Color TopButtonActiveColor = ColorFromHex("#324557", 0.96f);
        private static readonly Color TopButtonDebugActiveColor = ColorFromHex("#24465A", 0.98f);
        private static readonly Color TopButtonDisabledColor = ColorFromHex("#0B1117", 0.72f);

        [SerializeField] private bool uiEnabled = true;
        [SerializeField] private bool buildOnStart;
        [SerializeField] private bool logDiagnostics;

        private GameObject _canvasRoot;
        private GameObject _uiRoot;
        private RectTransform _mapViewport;
        private RectTransform _rightInspector;
        private RectTransform _actionPanel;
        private RectTransform _actionPanelGroupRoot;
        private RectTransform _actionPanelContentRoot;
        private RectTransform _overlayRoot;
        private RectTransform _debugRoot;
        private readonly Vector3[] _mapViewportCorners = new Vector3[4];
        private readonly List<Button> _visualOverlayButtons = new List<Button>(8);
        private readonly List<string> _visualOverlayButtonKeys = new List<string>(8);
        private readonly List<Button> _biosphereGroupButtons = new List<Button>(2);
        private readonly List<string> _biosphereGroupKeys = new List<string>(2);
        private readonly HashSet<string> _hiddenBiosphereSeriesKeys = new HashSet<string>();
        private Button _fovViewModeButton;
        private Button _pauseSimulationButton;
        private Button _resumeSimulationButton;
        private Button _speedSimulationButton;
        private Button _biosphereDebugMultiplierButton;
        private Button _biosphereDebugGoStopButton;
        private TextMeshProUGUI _dayLabel;
        private TextMeshProUGUI _monthLabel;
        private TextMeshProUGUI _yearLabel;
        private TextMeshProUGUI _seasonLabel;
        private TextMeshProUGUI _timeLabel;
        private TextMeshProUGUI _temperatureLabel;
        private TextMeshProUGUI _humidityLabel;
        private TextMeshProUGUI _weatherLabel;
        private TextMeshProUGUI _speedSimulationLabel;
        private TextMeshProUGUI _biosphereDebugMultiplierLabel;
        private TextMeshProUGUI _biosphereDebugGoStopLabel;
        private ArcUiSimulationControlController _simulationControlController;
        private ArcUiBiosphereRuntimeSnapshotProvider _biosphereSnapshotProvider;
        private ArcUiVisualOverlayController _visualOverlayController;
        private UnityAction<string, bool> _visualOverlayStateChanged;
        private string _activeBiosphereGroupKey = BiosphereWorldGraphsGroupKey;
        private ArcUiBiosphereGraphScope _activeBiosphereGraphScope = ArcUiBiosphereGraphScope.World;
        private ArcUiBiosphereGraphBucket _activeBiosphereGraphBucket = ArcUiBiosphereGraphBucket.Days;
        private int _selectedBiosphereAreaId;
        private string _biosphereLegendSignature = string.Empty;
        private ArcUiBiosphereGraphCanvas _biosphereGraphCanvas;

        public GameObject RootGameObject => _uiRoot;
        public bool IsBuilt => _uiRoot != null;

        // =============================================================================
        // OnEnable
        // =============================================================================
        /// <summary>
        /// <para>
        /// Controlla al rientro del componente se la UI runtime gia' esistente e'
        /// ancora compatibile con lo schema corrente.
        /// </para>
        ///
        /// <para><b>Principio architetturale: tolleranza ai reload del Play Mode</b></para>
        /// <para>
        /// Unity puo' mantenere riferimenti a GameObject runtime dopo un reload degli
        /// script. In quel caso <c>Start</c> potrebbe non ricostruire subito la shell,
        /// mentre <c>OnEnable</c> e' il punto piu' pulito per invalidare la vista
        /// transiente senza coinvolgere logica simulativa.
        /// </para>
        /// </summary>
        private void OnEnable()
        {
            if (_uiRoot != null && !IsRuntimeUiSchemaCurrent())
                BuildRuntimeUi();
        }

        // =============================================================================
        // Start
        // =============================================================================
        /// <summary>
        /// <para>
        /// Costruisce opzionalmente la shell UI in <c>Start</c>.
        /// </para>
        ///
        /// <para><b>Default controllato dall'auto-installer</b></para>
        /// <para>
        /// Nel runtime ArcGraph attuale la costruzione viene invocata
        /// esplicitamente da <c>ArcGraphRuntimeSceneAutoInstaller</c>. Il flag resta
        /// disponibile per test manuali in scena senza introdurre dipendenze.
        /// </para>
        /// </summary>
        private void Start()
        {
            if (buildOnStart)
                BuildRuntimeUi();
        }

        // =============================================================================
        // Update
        // =============================================================================
        /// <summary>
        /// <para>
        /// Mantiene aggiornata la TopBar rispetto allo snapshot del controller.
        /// </para>
        ///
        /// <para><b>Refresh visuale, non polling del World</b></para>
        /// <para>
        /// La pausa puo' cambiare anche tramite input globale del
        /// <c>SimulationHost</c>. La UI non legge l'host: aggiorna solo le etichette
        /// e gli stati dei pulsanti a partire dallo snapshot gia' filtrato dal
        /// controller.
        /// </para>
        /// </summary>
        private void Update()
        {
            if (_simulationControlController != null)
                RefreshSimulationControlTopBar();

            if (_biosphereGraphCanvas != null
                && _actionPanel != null
                && _actionPanel.gameObject.activeSelf)
                RefreshBiosphereGraphPanel();
        }

        // =============================================================================
        // BuildRuntimeUi
        // =============================================================================
        /// <summary>
        /// <para>
        /// Crea la gerarchia UI definitiva minima per ArcGraph.
        /// </para>
        ///
        /// <para><b>Sequenza intenzionale</b></para>
        /// <para>
        /// Prima assicuriamo l'EventSystem, poi creiamo il Canvas, quindi i root
        /// principali in ordine di responsabilita'. Ogni blocco resta un
        /// GameObject separato e nominato, cosi' potra' essere sostituito piu'
        /// avanti da prefab reali senza cambiare la semantica della gerarchia.
        /// </para>
        /// </summary>
        [ContextMenu("ArcGraph/Build Runtime UI Root")]
        public void BuildRuntimeUi()
        {
            if (_uiRoot != null)
            {
                if (IsRuntimeUiSchemaCurrent())
                {
                    _uiRoot.SetActive(uiEnabled);
                    return;
                }

                DestroyRuntimeUiHierarchyForSchemaRebuild();
            }

            EnsureEventSystemExists();
            CreateCanvasRoot();
            CreateUiRoot();
            CreateRuntimeUiSchemaMarker();
            BuildMapViewport();
            BuildTopBar();
            BuildRightInspector();
            BuildActionPanel();
            BuildBottomActionBar();
            BuildOverlayRoots();

            _uiRoot.SetActive(uiEnabled);

            if (logDiagnostics)
                Debug.Log("[ArcGraphUiRuntimeRoot] ArcGraph UI shell runtime costruita.");
        }

        // =============================================================================
        // SetUiEnabled
        // =============================================================================
        /// <summary>
        /// <para>
        /// Abilita o disabilita l'intera shell UI ArcGraph.
        /// </para>
        /// </summary>
        public void SetUiEnabled(bool enabled)
        {
            uiEnabled = enabled;

            if (_uiRoot != null)
                _uiRoot.SetActive(enabled);
        }

        // =============================================================================
        // SetFovViewModeClicked
        // =============================================================================
        /// <summary>
        /// <para>
        /// Collega il pulsante FOV della barra visualizzazione a un controller esterno.
        /// </para>
        ///
        /// <para><b>Principio architetturale: UI senza accesso al World</b></para>
        /// <para>
        /// La UI non sa cosa sia il FOV, non legge il <c>World</c> e non disegna
        /// overlay. Espone soltanto l'evento del pulsante a un controller autorizzato
        /// creato dall'auto-installer ArcGraph.
        /// </para>
        /// </summary>
        public void SetFovViewModeClicked(UnityAction action)
        {
            if (_fovViewModeButton == null)
                return;

            _fovViewModeButton.onClick.RemoveAllListeners();

            if (action != null)
                _fovViewModeButton.onClick.AddListener(action);
        }

        // =============================================================================
        // SetVisualOverlayController
        // =============================================================================
        /// <summary>
        /// <para>
        /// Collega la TopBar al controller dei toggle visuali indipendenti.
        /// </para>
        ///
        /// <para><b>Principio architetturale: TopBar -> richiesta overlay -> controller</b></para>
        /// <para>
        /// La TopBar non accende renderer e non legge il mondo. I pulsanti overlay
        /// producono solo richieste verso <see cref="ArcUiVisualOverlayController"/>.
        /// I passaggi successivi collegheranno lo stato del controller ai consumer
        /// ArcGraph gia' esistenti.
        /// </para>
        /// </summary>
        public void SetVisualOverlayController(ArcUiVisualOverlayController controller)
        {
            _visualOverlayController = controller;
            WireVisualOverlayButtons();
            RefreshVisualOverlayButtons();
        }

        // =============================================================================
        // SetVisualOverlayStateChanged
        // =============================================================================
        /// <summary>
        /// <para>
        /// Registra una callback esterna per notificare i cambi stato overlay.
        /// </para>
        ///
        /// <para><b>Boundary UI controllato</b></para>
        /// <para>
        /// La callback riceve solo chiave overlay e stato booleano. Non riceve
        /// renderer, <c>World</c>, oggetti selezionati o strutture runtime mutabili.
        /// </para>
        /// </summary>
        public void SetVisualOverlayStateChanged(UnityAction<string, bool> action)
        {
            _visualOverlayStateChanged = action;
        }

        // =============================================================================
        // SetSimulationControlController
        // =============================================================================
        /// <summary>
        /// <para>
        /// Collega la TopBar al controller autorizzato di controllo simulazione.
        /// </para>
        ///
        /// <para><b>Principio architetturale: UI senza SimulationHost diretto</b></para>
        /// <para>
        /// Il componente UI riceve solo il controller. Non conosce
        /// <c>SimulationHost</c>, non legge tick e non decide come applicare pausa o
        /// velocita'. I pulsanti producono intenzioni verso il controller.
        /// </para>
        /// </summary>
        public void SetSimulationControlController(ArcUiSimulationControlController controller)
        {
            _simulationControlController = controller;
            WireSimulationControlButtons();
            RefreshSimulationControlTopBar();
        }

        // =============================================================================
        // SetBiosphereRuntimeSnapshotProvider
        // =============================================================================
        /// <summary>
        /// <para>
        /// Collega alla shell UI il provider read-only dei grafici Biosfera.
        /// </para>
        ///
        /// <para><b>Principio architetturale: UI alimentata da snapshot</b></para>
        /// <para>
        /// Il root non riceve <c>SimulationHost</c> e non attraversa <c>World</c>.
        /// Chiede al provider un ViewModel gia' derivato ogni volta che deve
        /// ridisegnare il pannello Biosfera.
        /// </para>
        /// </summary>
        public void SetBiosphereRuntimeSnapshotProvider(ArcUiBiosphereRuntimeSnapshotProvider provider)
        {
            _biosphereSnapshotProvider = provider;
            RefreshBiosphereGraphPanel();
        }

        // =============================================================================
        // RefreshSimulationControlTopBar
        // =============================================================================
        /// <summary>
        /// <para>
        /// Aggiorna lo stato visuale minimo dei pulsanti pausa/play/velocita'.
        /// </para>
        /// </summary>
        public void RefreshSimulationControlTopBar()
        {
            if (_simulationControlController == null)
                return;

            ArcUiSimulationControlState state = _simulationControlController.BuildStateSnapshot();

            if (_pauseSimulationButton != null)
                _pauseSimulationButton.interactable =
                    (!state.HasRuntimeHost || !state.IsPaused) && !state.BiosphereDebugFastForwardActive;

            if (_resumeSimulationButton != null)
                _resumeSimulationButton.interactable =
                    (!state.HasRuntimeHost || state.IsPaused) && !state.BiosphereDebugFastForwardActive;

            if (_speedSimulationButton != null)
                _speedSimulationButton.interactable = !state.BiosphereDebugFastForwardActive;

            if (_biosphereDebugMultiplierButton != null)
                _biosphereDebugMultiplierButton.interactable = !state.BiosphereDebugFastForwardActive;

            if (_speedSimulationLabel != null)
                _speedSimulationLabel.text = "x" + state.SpeedMultiplier.ToString(System.Globalization.CultureInfo.InvariantCulture);

            if (_biosphereDebugMultiplierLabel != null)
                _biosphereDebugMultiplierLabel.text =
                    "Bio x" + state.BiosphereDebugFastForwardMultiplier.ToString(System.Globalization.CultureInfo.InvariantCulture);

            if (_biosphereDebugGoStopLabel != null)
                _biosphereDebugGoStopLabel.text = state.BiosphereDebugFastForwardActive ? "Stop" : "Go";

            if (_dayLabel != null)
                _dayLabel.text = state.DayLabel;

            if (_monthLabel != null)
                _monthLabel.text = state.MonthLabel;

            if (_yearLabel != null)
                _yearLabel.text = state.YearLabel;

            if (_seasonLabel != null)
                _seasonLabel.text = state.SeasonLabel;

            if (_timeLabel != null)
                _timeLabel.text = state.TimeLabel;

            if (_temperatureLabel != null)
                _temperatureLabel.text = state.TemperatureLabel;

            if (_humidityLabel != null)
                _humidityLabel.text = state.HumidityLabel;

            if (_weatherLabel != null)
                _weatherLabel.text = state.WeatherLabel;

            ApplyTopButtonVisualState(
                _pauseSimulationButton,
                state.IsPaused && !state.BiosphereDebugFastForwardActive,
                _pauseSimulationButton == null || _pauseSimulationButton.interactable,
                false);
            ApplyTopButtonVisualState(
                _resumeSimulationButton,
                !state.IsPaused && !state.BiosphereDebugFastForwardActive,
                _resumeSimulationButton == null || _resumeSimulationButton.interactable,
                false);
            ApplyTopButtonVisualState(
                _speedSimulationButton,
                state.SpeedMultiplier > 1 && !state.BiosphereDebugFastForwardActive,
                _speedSimulationButton == null || _speedSimulationButton.interactable,
                false);
            ApplyTopButtonVisualState(
                _biosphereDebugMultiplierButton,
                false,
                _biosphereDebugMultiplierButton == null || _biosphereDebugMultiplierButton.interactable,
                false);
            ApplyTopButtonVisualState(
                _biosphereDebugGoStopButton,
                state.BiosphereDebugFastForwardActive,
                _biosphereDebugGoStopButton == null || _biosphereDebugGoStopButton.interactable,
                state.BiosphereDebugFastForwardActive);
        }

        // =============================================================================
        // TryGetOverlayRoot
        // =============================================================================
        /// <summary>
        /// <para>
        /// Restituisce il root UGUI dedicato agli overlay runtime ArcGraph.
        /// </para>
        ///
        /// <para><b>Principio architetturale: overlay UI senza accesso alla simulazione</b></para>
        /// <para>
        /// I consumer visuali, come menu contestuali, tooltip e highlight UI, devono
        /// potersi ancorare a una radice comune senza conoscere la gerarchia interna
        /// della shell. Il metodo espone solo un <c>RectTransform</c> view-side:
        /// non trasporta dati world, non crea comandi e non autorizza mutazioni.
        /// </para>
        /// </summary>
        public bool TryGetOverlayRoot(out RectTransform overlayRoot)
        {
            overlayRoot = _overlayRoot;
            return overlayRoot != null;
        }

        // =============================================================================
        // TryGetRightInspectorRoot
        // =============================================================================
        /// <summary>
        /// <para>
        /// Restituisce il pannello UGUI dedicato al RightInspector ArcGraph.
        /// </para>
        ///
        /// <para><b>Principio architetturale: contenitore esposto, contenuto delegato</b></para>
        /// <para>
        /// La shell UI crea la geometria stabile del pannello, ma non decide quali
        /// dati mostrare. Il contenuto viene popolato da controller/view dedicate,
        /// cosi' il root layout resta responsabile solo di posizione, dimensione e
        /// blocco input sopra la viewport.
        /// </para>
        /// </summary>
        public bool TryGetRightInspectorRoot(out RectTransform rightInspector)
        {
            rightInspector = _rightInspector;
            return rightInspector != null;
        }

        // =============================================================================
        // TryApplyMapViewportToCamera
        // =============================================================================
        /// <summary>
        /// <para>
        /// Applica alla camera Unity il rettangolo occupato dal blocco UI
        /// <c>MapViewport</c>.
        /// </para>
        ///
        /// <para><b>Principio architetturale: viewport UI come confine visuale</b></para>
        /// <para>
        /// La UI non decide cosa disegnare e non legge la simulazione. Espone pero'
        /// il rettangolo visuale autorizzato per la mappa, cosi' camera, picking e
        /// pannelli usano lo stesso confine e la vista ArcGraph non finisce sotto
        /// TopBar, inspector o pannelli operativi.
        /// </para>
        /// </summary>
        public bool TryApplyMapViewportToCamera(Camera camera)
        {
            if (camera == null ||
                !TryResolveMapViewportScreenRect(out Rect screenRect) ||
                Screen.width <= 0 ||
                Screen.height <= 0)
            {
                return false;
            }

            Rect normalizedRect = new Rect(
                screenRect.xMin / Screen.width,
                screenRect.yMin / Screen.height,
                screenRect.width / Screen.width,
                screenRect.height / Screen.height);

            camera.rect = normalizedRect;
            return true;
        }

        // =============================================================================
        // TryResolveMapViewportScreenRect
        // =============================================================================
        /// <summary>
        /// <para>
        /// Restituisce il rettangolo pixel screen-space del viewport mappa.
        /// </para>
        /// </summary>
        public bool TryResolveMapViewportScreenRect(out Rect screenRect)
        {
            screenRect = default;

            if (_mapViewport == null)
                return false;

            _mapViewport.GetWorldCorners(_mapViewportCorners);
            Vector2 bottomLeft = RectTransformUtility.WorldToScreenPoint(null, _mapViewportCorners[0]);
            Vector2 topRight = RectTransformUtility.WorldToScreenPoint(null, _mapViewportCorners[2]);

            float xMin = Mathf.Clamp(Mathf.Min(bottomLeft.x, topRight.x), 0f, Screen.width);
            float xMax = Mathf.Clamp(Mathf.Max(bottomLeft.x, topRight.x), 0f, Screen.width);
            float yMin = Mathf.Clamp(Mathf.Min(bottomLeft.y, topRight.y), 0f, Screen.height);
            float yMax = Mathf.Clamp(Mathf.Max(bottomLeft.y, topRight.y), 0f, Screen.height);

            if (xMax <= xMin || yMax <= yMin)
                return false;

            screenRect = Rect.MinMaxRect(xMin, yMin, xMax, yMax);
            return true;
        }

        private void CreateCanvasRoot()
        {
            _canvasRoot = new GameObject(CanvasName, typeof(RectTransform));
            _canvasRoot.transform.SetParent(transform, false);

            Canvas canvas = _canvasRoot.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.sortingOrder = 12000;

            CanvasScaler scaler = _canvasRoot.AddComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1920f, 1080f);
            scaler.matchWidthOrHeight = 0.5f;

            _canvasRoot.AddComponent<GraphicRaycaster>();

            RectTransform canvasRect = (RectTransform)_canvasRoot.transform;
            StretchFull(canvasRect);
        }

        private void CreateUiRoot()
        {
            RectTransform root = CreateRect(RootName, _canvasRoot.transform);
            StretchFull(root);
            _uiRoot = root.gameObject;
        }

        // =============================================================================
        // IsRuntimeUiSchemaCurrent
        // =============================================================================
        /// <summary>
        /// <para>
        /// Verifica se la gerarchia UI runtime gia' presente appartiene allo schema
        /// corrente.
        /// </para>
        ///
        /// <para><b>Principio architetturale: rebuild esplicito delle view transienti</b></para>
        /// <para>
        /// La UI ArcGraph viene generata a runtime e puo' sopravvivere ai reload
        /// script del Play Mode. Il marker evita che una gerarchia vecchia continui
        /// a essere riutilizzata dopo una patch: se il marker manca, la shell e'
        /// considerata obsoleta e viene ricostruita.
        /// </para>
        ///
        /// <para><b>Struttura interna:</b></para>
        /// <list type="bullet">
        ///   <item><b>Marker</b>: GameObject inattivo sotto <c>ArcUIRoot</c>.</item>
        ///   <item><b>Controllo locale</b>: nessuna lettura di World o SimulationHost.</item>
        /// </list>
        /// </summary>
        private bool IsRuntimeUiSchemaCurrent()
        {
            return _uiRoot != null
                && _uiRoot.transform.Find(RuntimeUiSchemaMarkerName) != null;
        }

        // =============================================================================
        // CreateRuntimeUiSchemaMarker
        // =============================================================================
        /// <summary>
        /// <para>
        /// Scrive nella gerarchia UI un marker tecnico invisibile usato solo per
        /// riconoscere lo schema runtime corrente.
        /// </para>
        /// </summary>
        private void CreateRuntimeUiSchemaMarker()
        {
            RectTransform marker = CreateRect(RuntimeUiSchemaMarkerName, _uiRoot.transform);
            StretchFull(marker);
            marker.gameObject.SetActive(false);
        }

        // =============================================================================
        // DestroyRuntimeUiHierarchyForSchemaRebuild
        // =============================================================================
        /// <summary>
        /// <para>
        /// Elimina la vecchia shell UI generata a runtime e ripulisce i riferimenti
        /// C# prima di ricostruirla.
        /// </para>
        ///
        /// <para><b>Struttura interna:</b></para>
        /// <list type="bullet">
        ///   <item><b>Gerarchia</b>: distrugge il canvas auto-generato quando esiste.</item>
        ///   <item><b>Riferimenti</b>: azzera campi e liste che puntavano ai vecchi pulsanti.</item>
        ///   <item><b>Play Mode</b>: usa <c>Destroy</c> durante il runtime e <c>DestroyImmediate</c> fuori runtime.</item>
        /// </list>
        /// </summary>
        private void DestroyRuntimeUiHierarchyForSchemaRebuild()
        {
            GameObject hierarchyRoot = _canvasRoot != null ? _canvasRoot : ResolveCanvasRootFromUiRoot();

            if (hierarchyRoot != null)
            {
                if (Application.isPlaying)
                    Destroy(hierarchyRoot);
                else
                    DestroyImmediate(hierarchyRoot);
            }

            ResetRuntimeUiReferences();
        }

        private GameObject ResolveCanvasRootFromUiRoot()
        {
            if (_uiRoot == null || _uiRoot.transform.parent == null)
                return null;

            GameObject candidate = _uiRoot.transform.parent.gameObject;
            return candidate.name == CanvasName ? candidate : _uiRoot;
        }

        private void ResetRuntimeUiReferences()
        {
            _canvasRoot = null;
            _uiRoot = null;
            _mapViewport = null;
            _rightInspector = null;
            _actionPanel = null;
            _actionPanelGroupRoot = null;
            _actionPanelContentRoot = null;
            _overlayRoot = null;
            _debugRoot = null;
            _visualOverlayButtons.Clear();
            _visualOverlayButtonKeys.Clear();
            _biosphereGroupButtons.Clear();
            _biosphereGroupKeys.Clear();
            _fovViewModeButton = null;
            _pauseSimulationButton = null;
            _resumeSimulationButton = null;
            _speedSimulationButton = null;
            _biosphereDebugMultiplierButton = null;
            _biosphereDebugGoStopButton = null;
            _dayLabel = null;
            _monthLabel = null;
            _yearLabel = null;
            _seasonLabel = null;
            _timeLabel = null;
            _temperatureLabel = null;
            _humidityLabel = null;
            _weatherLabel = null;
            _speedSimulationLabel = null;
            _biosphereDebugMultiplierLabel = null;
            _biosphereDebugGoStopLabel = null;
            _biosphereLegendSignature = string.Empty;
            _biosphereGraphCanvas = null;
        }

        private void BuildMapViewport()
        {
            RectTransform viewport = CreateRect("MapViewport", _uiRoot.transform);
            SetAnchors(
                viewport,
                Vector2.zero,
                Vector2.one,
                new Vector2(0f, BottomActionBarHeight),
                new Vector2(0f, -TopBarHeight));
            _mapViewport = viewport;
        }

        private void BuildTopBar()
        {
            RectTransform panel = CreatePanel(
                "TopBar",
                _uiRoot.transform,
                new Vector2(0f, 1f),
                new Vector2(1f, 1f),
                new Vector2(0f, -TopBarHeight),
                new Vector2(0f, 0f));

            HorizontalLayoutGroup layout = panel.gameObject.AddComponent<HorizontalLayoutGroup>();
            layout.padding = new RectOffset(10, 10, 6, 6);
            layout.spacing = 6f;
            layout.childAlignment = TextAnchor.MiddleLeft;
            layout.childControlWidth = false;
            layout.childControlHeight = false;
            layout.childForceExpandWidth = false;
            layout.childForceExpandHeight = false;

            _dayLabel = CreateTopBarText(panel, "Giorno --");
            _monthLabel = CreateTopBarText(panel, "Mese --");
            _yearLabel = CreateTopBarText(panel, "Anno ----");
            _seasonLabel = CreateTopBarText(panel, "Stagione --");
            _timeLabel = CreateTopBarText(panel, "--:--");
            _temperatureLabel = CreateTopBarText(panel, "-- C");
            _humidityLabel = CreateTopBarText(panel, "-- %");
            _weatherLabel = CreateTopBarText(panel, "Meteo --");
            BuildVisualOverlayButtons(panel);
            _pauseSimulationButton = CreateTopBarButton(panel, "Pausa");
            _resumeSimulationButton = CreateTopBarButton(panel, "Play");
            _speedSimulationButton = CreateTopBarButton(panel, "x1");
            _speedSimulationLabel = _speedSimulationButton != null
                ? _speedSimulationButton.GetComponentInChildren<TextMeshProUGUI>()
                : null;
            _biosphereDebugMultiplierButton = CreateTopBarButton(panel, "Bio x50", 76f);
            _biosphereDebugMultiplierLabel = _biosphereDebugMultiplierButton != null
                ? _biosphereDebugMultiplierButton.GetComponentInChildren<TextMeshProUGUI>()
                : null;
            _biosphereDebugGoStopButton = CreateTopBarButton(panel, "Go", 52f);
            _biosphereDebugGoStopLabel = _biosphereDebugGoStopButton != null
                ? _biosphereDebugGoStopButton.GetComponentInChildren<TextMeshProUGUI>()
                : null;
            WireSimulationControlButtons();
            RefreshSimulationControlTopBar();
            WireVisualOverlayButtons();
            RefreshVisualOverlayButtons();
        }

        // =============================================================================
        // BuildVisualOverlayButtons
        // =============================================================================
        /// <summary>
        /// <para>
        /// Crea il gruppo compatto di icone overlay dentro la TopBar.
        /// </para>
        ///
        /// <para>
        /// Non viene creato un root permanente separato. Le icone vivono nella
        /// TopBar, come previsto dalla direzione UI corrente.
        /// </para>
        /// </summary>
        private void BuildVisualOverlayButtons(RectTransform parent)
        {
            _visualOverlayButtons.Clear();
            _visualOverlayButtonKeys.Clear();

            RectTransform group = CreateRect("VisualOverlayButtons", parent);
            group.anchorMin = new Vector2(0f, 0.5f);
            group.anchorMax = new Vector2(0f, 0.5f);
            group.pivot = new Vector2(0f, 0.5f);
            group.sizeDelta = new Vector2(132f, 28f);

            LayoutElement groupLayout = group.gameObject.AddComponent<LayoutElement>();
            groupLayout.minWidth = 132f;
            groupLayout.preferredWidth = 132f;
            groupLayout.minHeight = 28f;
            groupLayout.preferredHeight = 28f;

            HorizontalLayoutGroup layout = group.gameObject.AddComponent<HorizontalLayoutGroup>();
            layout.spacing = 4f;
            layout.childAlignment = TextAnchor.MiddleCenter;
            layout.childControlWidth = false;
            layout.childControlHeight = false;
            layout.childForceExpandWidth = false;
            layout.childForceExpandHeight = false;

            CreateVisualOverlayButton(group, ArcUiVisualOverlayCatalog.LandmarksKey, "LM", 30f);
            CreateVisualOverlayButton(group, ArcUiVisualOverlayCatalog.NpcLineOfSightKey, "LOS", 36f);
            CreateVisualOverlayButton(group, ArcUiVisualOverlayCatalog.PathfindingKey, "PATH", 54f);
        }

        private void BuildRightInspector()
        {
            RectTransform panel = CreatePanel(
                "RightInspector",
                _uiRoot.transform,
                new Vector2(1f, 0f),
                new Vector2(1f, 1f),
                new Vector2(-RightInspectorWidth, BottomActionBarHeight),
                new Vector2(0f, -TopBarHeight));
            _rightInspector = panel;
            panel.gameObject.AddComponent<RectMask2D>();

            VerticalLayoutGroup layout = panel.gameObject.AddComponent<VerticalLayoutGroup>();
            layout.padding = new RectOffset(12, 12, 12, 12);
            layout.spacing = 10f;
            layout.childControlWidth = true;
            layout.childControlHeight = false;
            layout.childForceExpandWidth = true;
            layout.childForceExpandHeight = false;

            BuildInspectorHeader(panel);
            BuildInspectorTabs(panel);
            BuildInspectorContent(panel);
            panel.gameObject.SetActive(false);
        }

        private void BuildInspectorHeader(RectTransform parent)
        {
            RectTransform header = CreatePanelBlock(parent, "Header", 104f);
            HorizontalLayoutGroup layout = header.gameObject.AddComponent<HorizontalLayoutGroup>();
            layout.padding = new RectOffset(8, 8, 8, 8);
            layout.spacing = 10f;
            layout.childAlignment = TextAnchor.MiddleLeft;
            layout.childControlWidth = false;
            layout.childControlHeight = true;

            RectTransform portrait = CreatePanelBlock(header, "Portrait", 72f, 72f);
            CreateText(portrait, "?", 24, FontStyles.Bold, TextAlignmentOptions.Center);

            RectTransform textRoot = CreateRect("HeaderText", header);
            LayoutElement textLayout = textRoot.gameObject.AddComponent<LayoutElement>();
            textLayout.preferredWidth = 210f;
            textLayout.flexibleWidth = 1f;

            VerticalLayoutGroup textLayoutGroup = textRoot.gameObject.AddComponent<VerticalLayoutGroup>();
            textLayoutGroup.spacing = 2f;
            textLayoutGroup.childControlWidth = true;
            textLayoutGroup.childControlHeight = false;
            textLayoutGroup.childForceExpandWidth = true;
            textLayoutGroup.childForceExpandHeight = false;

            CreateFlowText(textRoot, "Nessuna selezione", 16, FontStyles.Bold);
            CreateFlowText(textRoot, "Target: --", 12, FontStyles.Normal);
            CreateFlowText(textRoot, "Categoria: --", 12, FontStyles.Normal);
        }

        private void BuildInspectorTabs(RectTransform parent)
        {
            RectTransform tabs = CreateRect("TabBar", parent);
            LayoutElement tabsLayout = tabs.gameObject.AddComponent<LayoutElement>();
            tabsLayout.preferredHeight = 34f;

            HorizontalLayoutGroup layout = tabs.gameObject.AddComponent<HorizontalLayoutGroup>();
            layout.spacing = 4f;
            layout.childAlignment = TextAnchor.MiddleLeft;
            layout.childControlWidth = false;
            layout.childControlHeight = true;

            CreateTabButton(tabs, "Info", active: true);
            CreateTabButton(tabs, "Job", active: false);
            CreateTabButton(tabs, "Goal", active: false);
            CreateTabButton(tabs, "Memoria", active: false);
            CreateTabButton(tabs, "Credenze", active: false);
            CreateTabButton(tabs, "Eventi", active: false);
            CreateTabButton(tabs, "Debug", active: false);
        }

        private void BuildInspectorContent(RectTransform parent)
        {
            RectTransform content = CreatePanelBlock(parent, "ContentRoot", 420f);
            VerticalLayoutGroup layout = content.gameObject.AddComponent<VerticalLayoutGroup>();
            layout.padding = new RectOffset(10, 10, 10, 10);
            layout.spacing = 8f;
            layout.childControlWidth = true;
            layout.childControlHeight = false;
            layout.childForceExpandWidth = true;
            layout.childForceExpandHeight = false;

            CreateSectionTitle(content, "STATO");
            CreateInfoRow(content, "Salute", "--");
            CreateInfoRow(content, "Fame", "--");
            CreateInfoRow(content, "Riposo", "--");
            CreateInfoRow(content, "Morale", "--");
            CreateSectionTitle(content, "POSIZIONE");
            CreateInfoRow(content, "X", "--");
            CreateInfoRow(content, "Y", "--");
            CreateInfoRow(content, "Area", "--");
        }

        private void BuildActionPanel()
        {
            RectTransform panel = CreatePanel(
                "ActionPanel",
                _uiRoot.transform,
                new Vector2(0f, 0f),
                new Vector2(1f, 0f),
                new Vector2(0f, BottomActionBarHeight),
                new Vector2(-RightInspectorWidth, BottomActionBarHeight + ActionPanelHeight));
            _actionPanel = panel;
            panel.gameObject.AddComponent<RectMask2D>();

            HorizontalLayoutGroup layout = panel.gameObject.AddComponent<HorizontalLayoutGroup>();
            layout.padding = new RectOffset(10, 10, 10, 10);
            layout.spacing = 12f;
            layout.childControlWidth = false;
            layout.childControlHeight = true;
            layout.childForceExpandWidth = false;
            layout.childForceExpandHeight = true;

            BuildConstructionActionPanelContent();
            panel.gameObject.SetActive(false);
        }

        private void BuildConstructionActionPanelContent()
        {
            if (_actionPanel == null)
                return;

            HorizontalLayoutGroup panelLayout = _actionPanel.GetComponent<HorizontalLayoutGroup>();
            if (panelLayout != null)
                panelLayout.enabled = true;

            ClearChildren(_actionPanel);
            _biosphereGraphCanvas = null;
            _actionPanelGroupRoot = null;
            _actionPanelContentRoot = null;

            BuildActionPanelTabs(_actionPanel);
            BuildOperationGrid(_actionPanel);
        }

        private void BuildActionPanelTabs(RectTransform parent)
        {
            RectTransform tabs = CreateRect("CategoryTabs", parent);
            _actionPanelGroupRoot = tabs;
            LayoutElement tabsLayout = tabs.gameObject.AddComponent<LayoutElement>();
            tabsLayout.preferredWidth = 150f;

            VerticalLayoutGroup layout = tabs.gameObject.AddComponent<VerticalLayoutGroup>();
            layout.spacing = 4f;
            layout.childControlWidth = true;
            layout.childControlHeight = false;
            layout.childForceExpandWidth = true;
            layout.childForceExpandHeight = false;

            CreateCategoryButton(tabs, "Strutture", active: true);
            CreateCategoryButton(tabs, "Mobili", active: false);
            CreateCategoryButton(tabs, "Produzione", active: false);
            CreateCategoryButton(tabs, "Depositi", active: false);
            CreateCategoryButton(tabs, "Difesa", active: false);
            CreateCategoryButton(tabs, "Decorazioni", active: false);
        }

        private void BuildOperationGrid(RectTransform parent)
        {
            RectTransform gridRoot = CreateRect("OperationGrid", parent);
            _actionPanelContentRoot = gridRoot;
            LayoutElement gridLayout = gridRoot.gameObject.AddComponent<LayoutElement>();
            gridLayout.flexibleWidth = 1f;

            GridLayoutGroup grid = gridRoot.gameObject.AddComponent<GridLayoutGroup>();
            grid.cellSize = new Vector2(104f, 78f);
            grid.spacing = new Vector2(10f, 10f);
            grid.constraint = GridLayoutGroup.Constraint.FixedRowCount;
            grid.constraintCount = 2;

            CreateOperationButton(gridRoot, "Muro");
            CreateOperationButton(gridRoot, "Porta");
            CreateOperationButton(gridRoot, "Finestra");
            CreateOperationButton(gridRoot, "Letto");
            CreateOperationButton(gridRoot, "Tavolo");
            CreateOperationButton(gridRoot, "Sedia");
            CreateOperationButton(gridRoot, "Torcia");
            CreateOperationButton(gridRoot, "Armadio");
            CreateOperationButton(gridRoot, "Magazzino");
            CreateOperationButton(gridRoot, "Forno");
            CreateOperationButton(gridRoot, "Cucina");
            CreateOperationButton(gridRoot, "Forgia");
        }

        // =============================================================================
        // RebuildBiosphereActionPanel
        // =============================================================================
        /// <summary>
        /// <para>
        /// Ricostruisce l'ActionPanel nella modalita' grafici Biosfera.
        /// </para>
        ///
        /// <para><b>Principio architetturale: debug UI confinata</b></para>
        /// <para>
        /// Il pannello riceve soltanto ViewModel read-only. Non avanza il tempo,
        /// non modifica World e non invia comandi ad ArcGraph.
        /// </para>
        /// </summary>
        private void RebuildBiosphereActionPanel()
        {
            if (_actionPanel == null)
                return;

            HorizontalLayoutGroup panelLayout = _actionPanel.GetComponent<HorizontalLayoutGroup>();
            if (panelLayout != null)
                panelLayout.enabled = false;

            ClearChildren(_actionPanel);
            _biosphereGroupButtons.Clear();
            _biosphereGroupKeys.Clear();
            _actionPanelGroupRoot = null;
            _actionPanelContentRoot = null;

            RectTransform leftColumn = CreateRect("BiosphereLeftColumn", _actionPanel);
            _actionPanelGroupRoot = leftColumn;
            SetAnchors(
                leftColumn,
                new Vector2(0f, 0f),
                new Vector2(0f, 1f),
                new Vector2(BiospherePanelPadding, BiospherePanelPadding),
                new Vector2(BiospherePanelPadding + BiosphereLeftColumnWidth, -BiospherePanelPadding));

            VerticalLayoutGroup leftLayout = leftColumn.gameObject.AddComponent<VerticalLayoutGroup>();
            leftLayout.spacing = 4f;
            leftLayout.childControlWidth = true;
            leftLayout.childControlHeight = false;
            leftLayout.childForceExpandWidth = true;
            leftLayout.childForceExpandHeight = false;

            RectTransform content = CreateRect("BiosphereContent", _actionPanel);
            _actionPanelContentRoot = content;
            SetAnchors(
                content,
                new Vector2(0f, 0f),
                new Vector2(1f, 1f),
                new Vector2(BiospherePanelPadding + BiosphereLeftColumnWidth + BiosphereColumnGap, BiospherePanelPadding),
                new Vector2(-BiospherePanelPadding, -BiospherePanelPadding));

            BuildBiosphereActionPanelContent();
        }

        private void CreateBiosphereGroupButton(RectTransform parent, string groupKey, string label, bool active)
        {
            Button button = CreateParameterChip(parent, label, true, 54f, BiosphereChipHeight, 7);
            ApplyButtonColor(button, active);
            string capturedGroupKey = groupKey;
            _biosphereGroupButtons.Add(button);
            _biosphereGroupKeys.Add(capturedGroupKey);
            button.onClick.AddListener(() => SelectBiosphereGroup(capturedGroupKey));
        }

        private void SelectBiosphereGroup(string groupKey)
        {
            _activeBiosphereGroupKey = string.IsNullOrWhiteSpace(groupKey)
                ? BiosphereWorldGraphsGroupKey
                : groupKey.Trim();
            _activeBiosphereGraphScope = _activeBiosphereGroupKey == BiosphereAreaGraphsGroupKey
                ? ArcUiBiosphereGraphScope.BiologicalArea
                : ArcUiBiosphereGraphScope.World;

            RefreshBiosphereGroupButtons();
            BuildBiosphereActionPanelContent();
        }

        private void RefreshBiosphereGroupButtons()
        {
            for (int i = 0; i < _biosphereGroupButtons.Count; i++)
            {
                bool active = i < _biosphereGroupKeys.Count && _biosphereGroupKeys[i] == _activeBiosphereGroupKey;
                ApplyButtonColor(_biosphereGroupButtons[i], active);
            }
        }

        private void BuildBiosphereActionPanelContent()
        {
            if (_actionPanelContentRoot == null)
                return;

            ClearChildren(_actionPanelContentRoot);
            if (_actionPanelGroupRoot != null)
                ClearChildren(_actionPanelGroupRoot);

            _biosphereGroupButtons.Clear();
            _biosphereGroupKeys.Clear();

            ArcUiBiosphereGraphViewModel viewModel = BuildActiveBiosphereGraphViewModel();
            _biosphereLegendSignature = BuildBiosphereLegendSignature(viewModel);

            BuildBiosphereLeftColumn(_actionPanelGroupRoot, viewModel);
            BuildBiosphereToolbar(_actionPanelContentRoot, viewModel);
            BuildBiosphereGraphBlock(_actionPanelContentRoot, viewModel);
        }

        private void BuildBiosphereLeftColumn(RectTransform parent, ArcUiBiosphereGraphViewModel viewModel)
        {
            if (parent == null)
                return;

            Button days = CreateParameterChip(parent, "Giorni", true, -1f, BiosphereChipHeight, 7);
            ApplyButtonColor(days, _activeBiosphereGraphBucket == ArcUiBiosphereGraphBucket.Days);
            days.onClick.AddListener(() => SelectBiosphereBucket(ArcUiBiosphereGraphBucket.Days));

            Button months = CreateParameterChip(parent, "Mesi", true, -1f, BiosphereChipHeight, 7);
            ApplyButtonColor(months, _activeBiosphereGraphBucket == ArcUiBiosphereGraphBucket.Months);
            months.onClick.AddListener(() => SelectBiosphereBucket(ArcUiBiosphereGraphBucket.Months));

            if (_activeBiosphereGraphScope == ArcUiBiosphereGraphScope.BiologicalArea)
            {
                Button area = CreateParameterChip(parent, ResolveBiosphereAreaLabel(viewModel), true, -1f, BiosphereChipHeight, 7);
                area.onClick.AddListener(() => SelectNextBiosphereArea(viewModel));
            }
        }

        private void BuildBiosphereToolbar(RectTransform parent, ArcUiBiosphereGraphViewModel viewModel)
        {
            RectTransform toolbar = CreateRect("BiosphereToolbar", parent);
            SetAnchors(
                toolbar,
                new Vector2(0f, 1f),
                new Vector2(1f, 1f),
                new Vector2(0f, -BiosphereToolbarHeight),
                new Vector2(0f, 0f));

            HorizontalLayoutGroup layout = toolbar.gameObject.AddComponent<HorizontalLayoutGroup>();
            layout.spacing = 3f;
            layout.childControlWidth = false;
            layout.childControlHeight = true;
            layout.childForceExpandWidth = false;
            layout.childForceExpandHeight = false;

            CreateBiosphereGroupButton(toolbar, BiosphereWorldGraphsGroupKey, "Mondo", _activeBiosphereGroupKey == BiosphereWorldGraphsGroupKey);
            CreateBiosphereGroupButton(toolbar, BiosphereAreaGraphsGroupKey, "Area", _activeBiosphereGroupKey == BiosphereAreaGraphsGroupKey);

            ArcUiBiosphereGraphSeries[] series = viewModel.Series ?? new ArcUiBiosphereGraphSeries[0];
            for (int i = 0; i < series.Length; i++)
            {
                ArcUiBiosphereGraphSeries captured = series[i];
                Button chip = CreateLegendChip(
                    toolbar,
                    ResolveBiosphereLegendLabel(viewModel, captured),
                    captured.Color,
                    captured.Visible);
                chip.onClick.AddListener(() => ToggleBiosphereSeries(captured.Key));
            }

            if (viewModel.Scope == ArcUiBiosphereGraphScope.World)
            {
                string weatherKey = "world_weather";
                bool visible = !_hiddenBiosphereSeriesKeys.Contains(ArcUiOperationDefinition.NormalizeKey(weatherKey));
                Button weather = CreateLegendChip(
                    toolbar,
                    viewModel.Bucket == ArcUiBiosphereGraphBucket.Months
                        ? "Meteo (giorni/mese)"
                        : "Meteo (giorni)",
                    ColorFromHex("#D8C85A", 1f),
                    visible);
                weather.onClick.AddListener(() => ToggleBiosphereSeries(weatherKey));
            }
        }

        private void BuildBiosphereGraphBlock(RectTransform parent, ArcUiBiosphereGraphViewModel viewModel)
        {
            RectTransform block = CreateRect("BiosphereGraphBlock", parent);
            SetAnchors(
                block,
                new Vector2(0f, 0f),
                new Vector2(1f, 1f),
                new Vector2(0f, 0f),
                new Vector2(0f, -BiosphereToolbarHeight));
            Image image = block.gameObject.AddComponent<Image>();
            image.raycastTarget = false;
            image.color = ColorFromHex("#18222C", 0.82f);

            RectTransform graphRoot = CreateRect("BiosphereGraphCanvas", block);
            StretchFull(graphRoot);
            _biosphereGraphCanvas = graphRoot.gameObject.AddComponent<ArcUiBiosphereGraphCanvas>();
            _biosphereGraphCanvas.raycastTarget = false;
            _biosphereGraphCanvas.SetViewModel(viewModel);

            CreateGraphOverlayLabel(
                block,
                "BiosphereGraphYUnit",
                ResolveBiosphereYUnitLabel(viewModel),
                new Vector2(0f, 1f),
                new Vector2(0f, 1f),
                new Vector2(42f, -26f),
                new Vector2(250f, -6f),
                TextAlignmentOptions.Left);
            CreateGraphOverlayLabel(
                block,
                "BiosphereGraphXUnit",
                ResolveBiosphereXUnitLabel(viewModel),
                new Vector2(1f, 0f),
                new Vector2(1f, 0f),
                new Vector2(-170f, 4f),
                new Vector2(-12f, 24f),
                TextAlignmentOptions.Right);
        }

        private ArcUiBiosphereGraphViewModel BuildActiveBiosphereGraphViewModel()
        {
            if (_biosphereSnapshotProvider == null)
                return ArcUiBiosphereGraphViewModel.Empty();

            ArcUiBiosphereGraphViewModel viewModel = _biosphereSnapshotProvider.BuildGraphViewModel(
                _activeBiosphereGraphScope,
                _activeBiosphereGraphBucket,
                _selectedBiosphereAreaId,
                _hiddenBiosphereSeriesKeys);

            if (_selectedBiosphereAreaId <= 0 && viewModel.SelectedAreaId > 0)
                _selectedBiosphereAreaId = viewModel.SelectedAreaId;

            return viewModel;
        }

        private void SelectBiosphereBucket(ArcUiBiosphereGraphBucket bucket)
        {
            _activeBiosphereGraphBucket = bucket;
            BuildBiosphereActionPanelContent();
        }

        private void SelectNextBiosphereArea(ArcUiBiosphereGraphViewModel viewModel)
        {
            ArcUiBiosphereAreaOption[] areas = viewModel.AreaOptions ?? new ArcUiBiosphereAreaOption[0];
            if (areas.Length == 0)
                return;

            int index = 0;
            for (int i = 0; i < areas.Length; i++)
            {
                if (areas[i].AreaId == viewModel.SelectedAreaId)
                {
                    index = i;
                    break;
                }
            }

            _selectedBiosphereAreaId = areas[(index + 1) % areas.Length].AreaId;
            BuildBiosphereActionPanelContent();
        }

        private void ToggleBiosphereSeries(string key)
        {
            string normalized = ArcUiOperationDefinition.NormalizeKey(key);
            if (_hiddenBiosphereSeriesKeys.Contains(normalized))
                _hiddenBiosphereSeriesKeys.Remove(normalized);
            else
                _hiddenBiosphereSeriesKeys.Add(normalized);

            BuildBiosphereActionPanelContent();
        }

        private void RefreshBiosphereGraphPanel()
        {
            if (_biosphereGraphCanvas == null)
                return;

            ArcUiBiosphereGraphViewModel viewModel = BuildActiveBiosphereGraphViewModel();
            string signature = BuildBiosphereLegendSignature(viewModel);
            if (_biosphereLegendSignature != signature)
            {
                BuildBiosphereActionPanelContent();
                return;
            }

            _biosphereGraphCanvas.SetViewModel(viewModel);
        }

        private static string ResolveBiosphereAreaLabel(ArcUiBiosphereGraphViewModel viewModel)
        {
            ArcUiBiosphereAreaOption[] areas = viewModel.AreaOptions ?? new ArcUiBiosphereAreaOption[0];
            for (int i = 0; i < areas.Length; i++)
                if (areas[i].AreaId == viewModel.SelectedAreaId)
                    return areas[i].Label;

            return "Nessuna area";
        }

        // =============================================================================
        // ResolveBiosphereLegendLabel
        // =============================================================================
        /// <summary>
        /// <para>
        /// Costruisce il testo leggibile della legenda Biosfera aggiungendo l'unita'
        /// operativa della serie visualizzata.
        /// </para>
        ///
        /// <para><b>Principio architetturale: UI come spiegazione del dato, non sorgente del dato</b></para>
        /// <para>
        /// Il metodo non interpreta la simulazione e non cambia i valori del grafico:
        /// riceve serie gia' risolte dal ViewModel read-only e aggiunge solo una
        /// descrizione compatta per evitare legenda muta o ambigua.
        /// </para>
        ///
        /// <para><b>Struttura interna:</b></para>
        /// <list type="bullet">
        ///   <item><b>Serie mondo</b>: temperatura e umidita' sono indici normalizzati 0-1.</item>
        ///   <item><b>Serie area</b>: piante e vegetazione sono conteggi fisici o celle diffuse.</item>
        /// </list>
        /// </summary>
        private static string ResolveBiosphereLegendLabel(
            ArcUiBiosphereGraphViewModel viewModel,
            ArcUiBiosphereGraphSeries series)
        {
            string label = string.IsNullOrWhiteSpace(series.Label) ? series.Key : series.Label.Trim();
            string key = ArcUiOperationDefinition.NormalizeKey(series.Key);
            if (key.StartsWith("world_temperature", System.StringComparison.Ordinal))
                return label + " (indice 0-1)";
            if (key.StartsWith("world_humidity", System.StringComparison.Ordinal))
                return label + " (indice 0-1)";
            if (key.StartsWith("plant_", System.StringComparison.Ordinal))
                return label + " (piante)";
            if (key.StartsWith("vegetation_", System.StringComparison.Ordinal))
                return label + " (celle)";

            return viewModel.Scope == ArcUiBiosphereGraphScope.BiologicalArea
                ? label + " (conteggio)"
                : label;
        }

        // =============================================================================
        // ResolveBiosphereYUnitLabel
        // =============================================================================
        /// <summary>
        /// <para>
        /// Restituisce l'etichetta dell'asse verticale del grafico Biosfera.
        /// </para>
        /// </summary>
        private static string ResolveBiosphereYUnitLabel(ArcUiBiosphereGraphViewModel viewModel)
        {
            return viewModel.Scope == ArcUiBiosphereGraphScope.BiologicalArea
                ? "Y: conteggio piante / celle"
                : "Y: indice normalizzato 0-1";
        }

        // =============================================================================
        // ResolveBiosphereXUnitLabel
        // =============================================================================
        /// <summary>
        /// <para>
        /// Restituisce l'etichetta dell'asse orizzontale in base al bucket temporale
        /// selezionato dall'operatore.
        /// </para>
        /// </summary>
        private static string ResolveBiosphereXUnitLabel(ArcUiBiosphereGraphViewModel viewModel)
        {
            return viewModel.Bucket == ArcUiBiosphereGraphBucket.Months
                ? "X: mesi"
                : "X: giorni";
        }

        private static string BuildBiosphereLegendSignature(ArcUiBiosphereGraphViewModel viewModel)
        {
            ArcUiBiosphereGraphSeries[] series = viewModel.Series ?? new ArcUiBiosphereGraphSeries[0];
            string signature = viewModel.Scope.ToString() + "|" + viewModel.Bucket.ToString() + "|" + viewModel.SelectedAreaId.ToString();
            for (int i = 0; i < series.Length; i++)
                signature += "|" + series[i].Key;

            if (viewModel.Scope == ArcUiBiosphereGraphScope.World)
                signature += "|world_weather";

            return signature;
        }

        private void BuildBottomActionBar()
        {
            RectTransform panel = CreatePanel(
                "BottomActionBar",
                _uiRoot.transform,
                new Vector2(0f, 0f),
                new Vector2(1f, 0f),
                new Vector2(0f, 0f),
                new Vector2(0f, BottomActionBarHeight));

            HorizontalLayoutGroup layout = panel.gameObject.AddComponent<HorizontalLayoutGroup>();
            layout.padding = new RectOffset(8, 8, 8, 8);
            layout.spacing = 6f;
            layout.childControlWidth = true;
            layout.childControlHeight = true;
            layout.childForceExpandWidth = true;
            layout.childForceExpandHeight = true;

            CreateBottomActionButton(panel, "Costruisci", active: true);
            CreateBottomActionButton(panel, "Inserisci", active: false);
            Button biosphereButton = CreateBottomActionButton(panel, "Biosfera v0.63", active: false);
            biosphereButton.onClick.AddListener(ToggleBiosphereActionPanel);
            CreateBottomActionButton(panel, "Gestisci lavori", active: false);
            CreateBottomActionButton(panel, "Zone", active: false);
            CreateBottomActionButton(panel, "Oggetti", active: false);
            CreateBottomActionButton(panel, "NPC", active: false);
            CreateBottomActionButton(panel, "Istituzioni", active: false);
            CreateBottomActionButton(panel, "Ricerca", active: false);
        }

        private void ToggleBiosphereActionPanel()
        {
            if (_actionPanel == null)
                return;

            bool switchFromConstruction = _biosphereGraphCanvas == null;
            bool nextVisible = !_actionPanel.gameObject.activeSelf || switchFromConstruction;
            _actionPanel.gameObject.SetActive(nextVisible);

            if (nextVisible)
                RebuildBiosphereActionPanel();
            else
                _biosphereGraphCanvas = null;
        }

        private void BuildOverlayRoots()
        {
            _overlayRoot = CreateRect("OverlayRoot", _uiRoot.transform);
            StretchFull(_overlayRoot);
            _overlayRoot.SetSiblingIndex(1);

            _debugRoot = CreateRect("DebugRoot", _uiRoot.transform);
            StretchFull(_debugRoot);
            _debugRoot.gameObject.SetActive(true);
        }

        private static RectTransform CreatePanel(
            string name,
            Transform parent,
            Vector2 anchorMin,
            Vector2 anchorMax,
            Vector2 offsetMin,
            Vector2 offsetMax)
        {
            RectTransform rect = CreateRect(name, parent);
            SetAnchors(rect, anchorMin, anchorMax, offsetMin, offsetMax);

            Image image = rect.gameObject.AddComponent<Image>();
            image.raycastTarget = true;
            image.color = ColorFromHex("#111820", 0.90f);

            return rect;
        }

        private static RectTransform CreatePanelBlock(
            RectTransform parent,
            string name,
            float preferredHeight,
            float preferredWidth = -1f)
        {
            RectTransform rect = CreateRect(name, parent);

            Image image = rect.gameObject.AddComponent<Image>();
            image.raycastTarget = true;
            image.color = ColorFromHex("#18222C", 0.82f);

            LayoutElement layout = rect.gameObject.AddComponent<LayoutElement>();
            layout.minHeight = preferredHeight;
            layout.preferredHeight = preferredHeight;
            if (preferredWidth > 0f)
            {
                layout.minWidth = preferredWidth;
                layout.preferredWidth = preferredWidth;
            }

            return rect;
        }

        private static Button CreateIconButton(RectTransform parent, string label)
        {
            RectTransform button = CreateButtonShell(parent, "ArcButton_ViewMode_" + label, 42f, 30f, false);
            CreateText(button, label, 10, FontStyles.Bold, TextAlignmentOptions.Center);
            return button.GetComponent<Button>();
        }

        private void CreateVisualOverlayButton(
            RectTransform parent,
            string overlayKey,
            string label,
            float preferredWidth)
        {
            RectTransform button = CreateButtonShell(
                parent,
                "ArcButton_VisualOverlay_" + SanitizeName(label),
                preferredWidth,
                28f,
                false);
            CreateText(button, label, 9, FontStyles.Bold, TextAlignmentOptions.Center);

            Button component = button.GetComponent<Button>();
            _visualOverlayButtons.Add(component);
            _visualOverlayButtonKeys.Add(ArcUiOperationDefinition.NormalizeKey(overlayKey));
        }

        private static TextMeshProUGUI CreateTopBarText(RectTransform parent, string label)
        {
            RectTransform textRoot = CreateRect("ArcInfoRow_Top_" + SanitizeName(label), parent);
            LayoutElement layout = textRoot.gameObject.AddComponent<LayoutElement>();
            layout.minWidth = 58f;
            layout.preferredWidth = 76f;
            layout.flexibleWidth = 0f;

            return CreateText(textRoot, label, 12, FontStyles.Bold, TextAlignmentOptions.Center);
        }

        // =============================================================================
        // WireSimulationControlButtons
        // =============================================================================
        /// <summary>
        /// <para>
        /// Collega i pulsanti TopBar agli handler locali che parlano col controller.
        /// </para>
        /// </summary>
        private void WireSimulationControlButtons()
        {
            if (_pauseSimulationButton != null)
            {
                _pauseSimulationButton.onClick.RemoveAllListeners();
                if (_simulationControlController != null)
                    _pauseSimulationButton.onClick.AddListener(OnPauseSimulationClicked);
            }

            if (_resumeSimulationButton != null)
            {
                _resumeSimulationButton.onClick.RemoveAllListeners();
                if (_simulationControlController != null)
                    _resumeSimulationButton.onClick.AddListener(OnResumeSimulationClicked);
            }

            if (_speedSimulationButton != null)
            {
                _speedSimulationButton.onClick.RemoveAllListeners();
                if (_simulationControlController != null)
                    _speedSimulationButton.onClick.AddListener(OnSpeedSimulationClicked);
            }

            if (_biosphereDebugMultiplierButton != null)
            {
                _biosphereDebugMultiplierButton.onClick.RemoveAllListeners();
                if (_simulationControlController != null)
                    _biosphereDebugMultiplierButton.onClick.AddListener(OnBiosphereDebugMultiplierClicked);
            }

            if (_biosphereDebugGoStopButton != null)
            {
                _biosphereDebugGoStopButton.onClick.RemoveAllListeners();
                if (_simulationControlController != null)
                    _biosphereDebugGoStopButton.onClick.AddListener(OnBiosphereDebugGoStopClicked);
            }
        }

        // =============================================================================
        // WireVisualOverlayButtons
        // =============================================================================
        /// <summary>
        /// <para>
        /// Collega i pulsanti overlay al controller UI dedicato.
        /// </para>
        /// </summary>
        private void WireVisualOverlayButtons()
        {
            int count = _visualOverlayButtons.Count;
            for (int i = 0; i < count; i++)
            {
                Button button = _visualOverlayButtons[i];
                if (button == null)
                    continue;

                button.onClick.RemoveAllListeners();
                if (_visualOverlayController == null)
                    continue;

                string overlayKey = i < _visualOverlayButtonKeys.Count
                    ? _visualOverlayButtonKeys[i]
                    : string.Empty;
                button.onClick.AddListener(() => OnVisualOverlayClicked(overlayKey));
            }
        }

        // =============================================================================
        // RefreshVisualOverlayButtons
        // =============================================================================
        /// <summary>
        /// <para>
        /// Aggiorna lo stato cromatico dei toggle overlay.
        /// </para>
        /// </summary>
        public void RefreshVisualOverlayButtons()
        {
            ArcUiVisualOverlayState state = _visualOverlayController != null
                ? _visualOverlayController.BuildStateSnapshot()
                : ArcUiVisualOverlayState.Empty();

            int count = _visualOverlayButtons.Count;
            for (int i = 0; i < count; i++)
            {
                Button button = _visualOverlayButtons[i];
                string overlayKey = i < _visualOverlayButtonKeys.Count
                    ? _visualOverlayButtonKeys[i]
                    : string.Empty;
                bool enabled = state.IsEnabled(overlayKey);
                ApplyTopButtonVisualState(button, enabled, button == null || button.interactable, false);
            }
        }

        // =============================================================================
        // OnVisualOverlayClicked
        // =============================================================================
        /// <summary>
        /// <para>
        /// Inoltra al controller la richiesta di toggle overlay prodotta dalla TopBar.
        /// </para>
        /// </summary>
        private void OnVisualOverlayClicked(string overlayKey)
        {
            _visualOverlayController?.Apply(ArcUiVisualOverlayRequest.Toggle(overlayKey, "ArcTopBar"));
            RefreshVisualOverlayButtons();

            ArcUiVisualOverlayState state = _visualOverlayController != null
                ? _visualOverlayController.BuildStateSnapshot()
                : ArcUiVisualOverlayState.Empty();
            NotifyVisualOverlayStateChanged(state);
        }

        private void NotifyVisualOverlayStateChanged(ArcUiVisualOverlayState state)
        {
            if (_visualOverlayStateChanged == null)
                return;

            int count = _visualOverlayButtonKeys.Count;
            for (int i = 0; i < count; i++)
            {
                string overlayKey = _visualOverlayButtonKeys[i];
                if (string.IsNullOrEmpty(overlayKey))
                    continue;

                _visualOverlayStateChanged.Invoke(overlayKey, state.IsEnabled(overlayKey));
            }
        }

        // =============================================================================
        // OnPauseSimulationClicked
        // =============================================================================
        /// <summary>
        /// <para>
        /// Inoltra al controller la richiesta di pausa prodotta dalla TopBar.
        /// </para>
        /// </summary>
        private void OnPauseSimulationClicked()
        {
            _simulationControlController?.RequestPause("ArcTopBar");
            RefreshSimulationControlTopBar();
        }

        // =============================================================================
        // OnResumeSimulationClicked
        // =============================================================================
        /// <summary>
        /// <para>
        /// Inoltra al controller la richiesta di ripresa prodotta dalla TopBar.
        /// </para>
        /// </summary>
        private void OnResumeSimulationClicked()
        {
            _simulationControlController?.RequestResume("ArcTopBar");
            RefreshSimulationControlTopBar();
        }

        // =============================================================================
        // OnSpeedSimulationClicked
        // =============================================================================
        /// <summary>
        /// <para>
        /// Inoltra al controller la richiesta di ciclo velocita' prodotta dalla TopBar.
        /// </para>
        /// </summary>
        private void OnSpeedSimulationClicked()
        {
            _simulationControlController?.CycleSpeed("ArcTopBar");
            RefreshSimulationControlTopBar();
        }

        // =============================================================================
        // OnBiosphereDebugMultiplierClicked
        // =============================================================================
        /// <summary>
        /// <para>
        /// Inoltra al controller il cambio moltiplicatore del fast-forward Biosfera.
        /// </para>
        /// </summary>
        private void OnBiosphereDebugMultiplierClicked()
        {
            _simulationControlController?.CycleBiosphereDebugFastForwardMultiplier("ArcTopBar");
            RefreshSimulationControlTopBar();
        }

        // =============================================================================
        // OnBiosphereDebugGoStopClicked
        // =============================================================================
        /// <summary>
        /// <para>
        /// Inoltra al controller l'avvio o lo stop del fast-forward Biosfera.
        /// </para>
        /// </summary>
        private void OnBiosphereDebugGoStopClicked()
        {
            _simulationControlController?.ToggleBiosphereDebugFastForward("ArcTopBar");
            RefreshSimulationControlTopBar();
        }

        private static Button CreateTopBarButton(RectTransform parent, string label)
        {
            return CreateTopBarButton(parent, label, 64f);
        }

        private static Button CreateTopBarButton(RectTransform parent, string label, float preferredWidth)
        {
            RectTransform button = CreateButtonShell(parent, "ArcButton_Top_" + SanitizeName(label), preferredWidth, 30f, false);
            CreateText(button, label, 12, FontStyles.Bold, TextAlignmentOptions.Center);
            return button.GetComponent<Button>();
        }

        // =============================================================================
        // ApplyTopButtonVisualState
        // =============================================================================
        /// <summary>
        /// <para>
        /// Applica lo stato cromatico coerente ai pulsanti della TopBar.
        /// </para>
        ///
        /// <para><b>Principio architetturale: stato visuale derivato dallo snapshot</b></para>
        /// <para>
        /// Il metodo non decide pause, velocita' o fast-forward. Riceve solo il
        /// risultato gia' calcolato dal controller e aggiorna il colore del bottone,
        /// mantenendo la TopBar una view UGUI.
        /// </para>
        /// </summary>
        private static void ApplyTopButtonVisualState(
            Button button,
            bool active,
            bool enabled,
            bool debugActive)
        {
            if (button == null || button.targetGraphic == null)
                return;

            Color color = !enabled
                ? TopButtonDisabledColor
                : debugActive
                    ? TopButtonDebugActiveColor
                    : active
                        ? TopButtonActiveColor
                        : TopButtonNormalColor;

            button.targetGraphic.color = color;

            ColorBlock colors = button.colors;
            colors.normalColor = color;
            colors.highlightedColor = enabled
                ? ColorFromHex("#41566A", 0.98f)
                : TopButtonDisabledColor;
            colors.pressedColor = enabled
                ? ColorFromHex("#51708B", 1f)
                : TopButtonDisabledColor;
            colors.selectedColor = active || debugActive
                ? color
                : TopButtonActiveColor;
            colors.disabledColor = TopButtonDisabledColor;
            button.colors = colors;
        }

        private static void CreateTabButton(RectTransform parent, string label, bool active)
        {
            RectTransform button = CreateButtonShell(parent, "ArcTabButton_" + SanitizeName(label), 42f, 30f, active);
            CreateText(button, label, 9, FontStyles.Bold, TextAlignmentOptions.Center);
        }

        private static Button CreateCategoryButton(RectTransform parent, string label, bool active)
        {
            RectTransform button = CreateButtonShell(parent, "ArcButton_Category_" + SanitizeName(label), -1f, 28f, active);
            CreateText(button, label, 12, FontStyles.Bold, TextAlignmentOptions.Left);
            return button.GetComponent<Button>();
        }

        private static void CreateOperationButton(RectTransform parent, string label)
        {
            RectTransform button = CreateButtonShell(parent, "ArcOperationButton_" + SanitizeName(label), 104f, 78f, false);

            RectTransform iconSlot = CreateRect("IconSlot", button);
            SetAnchors(iconSlot, new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(-22f, -42f), new Vector2(22f, -10f));
            Image iconImage = iconSlot.gameObject.AddComponent<Image>();
            iconImage.raycastTarget = false;
            iconImage.color = ColorFromHex("#9AA7B2", 0.72f);

            RectTransform labelRoot = CreateRect("Label", button);
            SetAnchors(labelRoot, new Vector2(0f, 0f), new Vector2(1f, 0f), new Vector2(4f, 4f), new Vector2(-4f, 24f));
            CreateText(labelRoot, label, 10, FontStyles.Bold, TextAlignmentOptions.Center);
        }

        private static Button CreateBottomActionButton(RectTransform parent, string label, bool active)
        {
            RectTransform button = CreateButtonShell(parent, "ArcButton_Bottom_" + SanitizeName(label), -1f, 72f, active);
            CreateText(button, label, 13, FontStyles.Bold, TextAlignmentOptions.Center);
            return button.GetComponent<Button>();
        }

        private static Button CreateParameterChip(RectTransform parent, string label, bool interactable)
        {
            return CreateParameterChip(parent, label, interactable, 118f, 30f, 9);
        }

        private static Button CreateParameterChip(
            RectTransform parent,
            string label,
            bool interactable,
            float preferredWidth,
            float preferredHeight,
            int fontSize)
        {
            RectTransform button = CreateButtonShell(
                parent,
                "ArcParam_" + SanitizeName(label),
                preferredWidth,
                preferredHeight,
                false);
            Button component = button.GetComponent<Button>();
            component.interactable = interactable;
            CreateText(button, label, fontSize, FontStyles.Bold, TextAlignmentOptions.Center);
            return component;
        }

        // =============================================================================
        // CreateLegendChip
        // =============================================================================
        /// <summary>
        /// <para>
        /// Crea un chip di legenda compatto con swatch colorato e testo con unita'.
        /// </para>
        ///
        /// <para><b>Principio architetturale: legenda come contratto visivo locale</b></para>
        /// <para>
        /// Il colore arriva dalla serie del ViewModel e resta confinato alla UI: non
        /// viene usato per dedurre tipo di dato, scala o comportamento simulativo.
        /// </para>
        ///
        /// <para><b>Struttura interna:</b></para>
        /// <list type="bullet">
        ///   <item><b>Shell</b>: normale bottone togglable della toolbar Biosfera.</item>
        ///   <item><b>Swatch</b>: piccolo campione cromatico della linea o del meteo.</item>
        ///   <item><b>Label</b>: testo ellittico con unita' di misura.</item>
        /// </list>
        /// </summary>
        private static Button CreateLegendChip(
            RectTransform parent,
            string label,
            Color swatchColor,
            bool active)
        {
            RectTransform button = CreateButtonShell(
                parent,
                "ArcLegend_" + SanitizeName(label),
                156f,
                BiosphereChipHeight,
                false);
            Button component = button.GetComponent<Button>();
            component.interactable = true;
            ApplySeriesButtonColor(component, swatchColor, active);

            RectTransform swatch = CreateRect("Swatch", button);
            SetAnchors(
                swatch,
                new Vector2(0f, 0.5f),
                new Vector2(0f, 0.5f),
                new Vector2(8f, -5f),
                new Vector2(18f, 5f));
            Image swatchImage = swatch.gameObject.AddComponent<Image>();
            swatchImage.raycastTarget = false;
            swatchImage.color = active ? swatchColor : ColorFromHex("#4A5158", 0.9f);

            RectTransform labelRoot = CreateRect("LegendLabel", button);
            SetAnchors(
                labelRoot,
                new Vector2(0f, 0f),
                new Vector2(1f, 1f),
                new Vector2(23f, 1f),
                new Vector2(-5f, -1f));
            TextMeshProUGUI text = CreateText(labelRoot, label, 8, FontStyles.Bold, TextAlignmentOptions.MidlineLeft);
            text.overflowMode = TextOverflowModes.Ellipsis;
            text.enableWordWrapping = false;
            return component;
        }

        // =============================================================================
        // CreateGraphOverlayLabel
        // =============================================================================
        /// <summary>
        /// <para>
        /// Crea una label non interattiva sopra il grafico per indicare unita' e scala
        /// degli assi senza alterare il renderer texture-based del grafico.
        /// </para>
        /// </summary>
        private static TextMeshProUGUI CreateGraphOverlayLabel(
            RectTransform parent,
            string name,
            string label,
            Vector2 anchorMin,
            Vector2 anchorMax,
            Vector2 offsetMin,
            Vector2 offsetMax,
            TextAlignmentOptions alignment)
        {
            RectTransform root = CreateRect(name, parent);
            SetAnchors(root, anchorMin, anchorMax, offsetMin, offsetMax);
            TextMeshProUGUI text = CreateText(root, label, 8, FontStyles.Bold, alignment);
            text.raycastTarget = false;
            text.overflowMode = TextOverflowModes.Ellipsis;
            text.enableWordWrapping = false;
            text.color = ColorFromHex("#DDE6EE", 0.82f);
            return text;
        }

        private static void ApplyButtonColor(Button button, bool active)
        {
            if (button == null)
                return;

            Image image = button.targetGraphic as Image;
            if (image != null)
                image.color = active ? ColorFromHex("#324557", 0.94f) : ColorFromHex("#17212B", 0.92f);

            ColorBlock colors = button.colors;
            colors.normalColor = active ? ColorFromHex("#324557", 0.94f) : ColorFromHex("#17212B", 0.92f);
            colors.selectedColor = ColorFromHex("#324557", 1f);
            button.colors = colors;
        }

        private static void ApplySeriesButtonColor(Button button, Color seriesColor, bool active)
        {
            if (button == null)
                return;

            Color normal = active
                ? new Color(seriesColor.r, seriesColor.g, seriesColor.b, 0.94f)
                : ColorFromHex("#17212B", 0.92f);

            Image image = button.targetGraphic as Image;
            if (image != null)
                image.color = normal;

            ColorBlock colors = button.colors;
            colors.normalColor = normal;
            colors.highlightedColor = new Color(seriesColor.r, seriesColor.g, seriesColor.b, 1f);
            colors.pressedColor = ColorFromHex("#DDE6EE", 1f);
            colors.selectedColor = normal;
            colors.disabledColor = ColorFromHex("#0B1117", 0.72f);
            button.colors = colors;
        }

        private static void ClearChildren(RectTransform root)
        {
            if (root == null)
                return;

            for (int i = root.childCount - 1; i >= 0; i--)
                Destroy(root.GetChild(i).gameObject);
        }

        private static RectTransform CreateButtonShell(
            RectTransform parent,
            string name,
            float preferredWidth,
            float preferredHeight,
            bool active)
        {
            RectTransform rect = CreateRect(name, parent);

            Image image = rect.gameObject.AddComponent<Image>();
            image.raycastTarget = true;
            image.color = active
                ? ColorFromHex("#324557", 0.94f)
                : ColorFromHex("#17212B", 0.92f);

            Button button = rect.gameObject.AddComponent<Button>();
            button.transition = Selectable.Transition.ColorTint;
            button.targetGraphic = image;
            button.interactable = true;

            ColorBlock colors = button.colors;
            colors.normalColor = image.color;
            colors.highlightedColor = ColorFromHex("#41566A", 0.98f);
            colors.pressedColor = ColorFromHex("#51708B", 1f);
            colors.selectedColor = ColorFromHex("#324557", 1f);
            colors.disabledColor = ColorFromHex("#0B1117", 0.72f);
            button.colors = colors;

            LayoutElement layout = rect.gameObject.AddComponent<LayoutElement>();
            if (preferredWidth > 0f)
            {
                layout.minWidth = preferredWidth;
                layout.preferredWidth = preferredWidth;
            }
            if (preferredHeight > 0f)
            {
                layout.minHeight = preferredHeight;
                layout.preferredHeight = preferredHeight;
            }

            rect.sizeDelta = new Vector2(
                preferredWidth > 0f ? preferredWidth : rect.sizeDelta.x,
                preferredHeight > 0f ? preferredHeight : rect.sizeDelta.y);

            return rect;
        }

        private static void CreateSectionTitle(RectTransform parent, string label)
        {
            RectTransform title = CreateRect("SectionTitle_" + SanitizeName(label), parent);
            LayoutElement layout = title.gameObject.AddComponent<LayoutElement>();
            layout.preferredHeight = 22f;
            CreateText(title, label, 12, FontStyles.Bold, TextAlignmentOptions.Left);
        }

        private static void CreateInfoRow(RectTransform parent, string label, string value)
        {
            RectTransform row = CreateRect("ArcInfoRow_" + SanitizeName(label), parent);
            LayoutElement rowLayout = row.gameObject.AddComponent<LayoutElement>();
            rowLayout.preferredHeight = 24f;

            HorizontalLayoutGroup layout = row.gameObject.AddComponent<HorizontalLayoutGroup>();
            layout.childControlWidth = true;
            layout.childControlHeight = true;
            layout.childForceExpandWidth = true;
            layout.childForceExpandHeight = true;

            RectTransform labelRoot = CreateRect("Label", row);
            LayoutElement labelLayout = labelRoot.gameObject.AddComponent<LayoutElement>();
            labelLayout.flexibleWidth = 1f;
            CreateText(labelRoot, label, 12, FontStyles.Normal, TextAlignmentOptions.Left);

            RectTransform valueRoot = CreateRect("Value", row);
            LayoutElement valueLayout = valueRoot.gameObject.AddComponent<LayoutElement>();
            valueLayout.preferredWidth = 72f;
            CreateText(valueRoot, value, 12, FontStyles.Bold, TextAlignmentOptions.Right);
        }

        private static void CreateFlowText(
            RectTransform parent,
            string label,
            int fontSize,
            FontStyles style)
        {
            RectTransform root = CreateRect("Text_" + SanitizeName(label), parent);
            LayoutElement layout = root.gameObject.AddComponent<LayoutElement>();
            layout.preferredHeight = fontSize + 8f;
            CreateText(root, label, fontSize, style, TextAlignmentOptions.Left);
        }

        private static TextMeshProUGUI CreateText(
            RectTransform parent,
            string text,
            int fontSize,
            FontStyles fontStyle,
            TextAlignmentOptions alignment)
        {
            RectTransform textRoot = CreateRect("Text", parent);
            StretchFull(textRoot, new Vector2(6f, 2f), new Vector2(-6f, -2f));

            TextMeshProUGUI label = textRoot.gameObject.AddComponent<TextMeshProUGUI>();
            label.raycastTarget = false;
            label.text = text;
            label.fontSize = fontSize;
            label.fontStyle = fontStyle;
            label.alignment = alignment;
            label.enableWordWrapping = false;
            label.overflowMode = TextOverflowModes.Ellipsis;
            label.color = ColorFromHex("#DDE6EE", 1f);
            ArcGraphUiFontProvider.ApplyOfficialFont(label);

            return label;
        }

        private static RectTransform CreateRect(string name, Transform parent)
        {
            var go = new GameObject(name, typeof(RectTransform));
            go.transform.SetParent(parent, false);
            return (RectTransform)go.transform;
        }

        private static void StretchFull(RectTransform rect)
        {
            StretchFull(rect, Vector2.zero, Vector2.zero);
        }

        private static void StretchFull(RectTransform rect, Vector2 offsetMin, Vector2 offsetMax)
        {
            SetAnchors(rect, Vector2.zero, Vector2.one, offsetMin, offsetMax);
        }

        private static void SetAnchors(
            RectTransform rect,
            Vector2 anchorMin,
            Vector2 anchorMax,
            Vector2 offsetMin,
            Vector2 offsetMax)
        {
            rect.anchorMin = anchorMin;
            rect.anchorMax = anchorMax;
            rect.offsetMin = offsetMin;
            rect.offsetMax = offsetMax;
            rect.localScale = Vector3.one;
        }

        private static void EnsureEventSystemExists()
        {
            // La UI UGUI richiede un EventSystem in scena. Usiamo l'API moderna
            // Unity 6000 per non introdurre warning diagnostici gia' noti.
            EventSystem existing = Object.FindFirstObjectByType<EventSystem>();
            if (existing == null)
            {
                var eventSystemGo = new GameObject("EventSystem");
                existing = eventSystemGo.AddComponent<EventSystem>();
            }

            BaseInputModule[] modules = existing.GetComponents<BaseInputModule>();
            if (modules != null && modules.Length > 0)
                return;

#if ENABLE_INPUT_SYSTEM
            existing.gameObject.AddComponent<InputSystemUIInputModule>();
#else
            existing.gameObject.AddComponent<StandaloneInputModule>();
#endif
        }

        private static string SanitizeName(string value)
        {
            if (string.IsNullOrWhiteSpace(value))
                return "Empty";

            return value
                .Replace(" ", string.Empty)
                .Replace("/", string.Empty)
                .Replace("-", string.Empty)
                .Replace(":", string.Empty);
        }

        private static Color ColorFromHex(string hex, float alpha)
        {
            if (ColorUtility.TryParseHtmlString(hex, out Color color))
            {
                color.a = alpha;
                return color;
            }

            return new Color(1f, 1f, 1f, alpha);
        }
    }
}
