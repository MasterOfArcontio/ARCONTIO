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
        private const float ActionPanelHeight = 210f;
        private const float OuterMargin = 0f;

        [SerializeField] private bool uiEnabled = true;
        [SerializeField] private bool buildOnStart;
        [SerializeField] private bool logDiagnostics;

        private GameObject _canvasRoot;
        private GameObject _uiRoot;
        private RectTransform _mapViewport;
        private RectTransform _overlayRoot;
        private readonly Vector3[] _mapViewportCorners = new Vector3[4];
        private Button _fovViewModeButton;

        public GameObject RootGameObject => _uiRoot;
        public bool IsBuilt => _uiRoot != null;

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
                _uiRoot.SetActive(uiEnabled);
                return;
            }

            EnsureEventSystemExists();
            CreateCanvasRoot();
            CreateUiRoot();
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
            layout.padding = new RectOffset(18, 18, 6, 6);
            layout.spacing = 22f;
            layout.childAlignment = TextAnchor.MiddleCenter;
            layout.childControlWidth = false;
            layout.childControlHeight = true;
            layout.childForceExpandWidth = false;
            layout.childForceExpandHeight = true;

            CreateTopBarText(panel, "Giorno --");
            CreateTopBarText(panel, "Mese --");
            CreateTopBarText(panel, "Anno ----");
            CreateTopBarText(panel, "Stagione --");
            CreateTopBarText(panel, "--:--");
            CreateTopBarText(panel, "-- C");
            CreateTopBarText(panel, "-- %");
            CreateTopBarText(panel, "Meteo --");
            CreateTopBarButton(panel, "Pausa");
            CreateTopBarButton(panel, "Play");
            CreateTopBarButton(panel, "x1");
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
                new Vector2(0f, BottomActionBarHeight + ActionPanelHeight));

            HorizontalLayoutGroup layout = panel.gameObject.AddComponent<HorizontalLayoutGroup>();
            layout.padding = new RectOffset(10, 10, 10, 10);
            layout.spacing = 12f;
            layout.childControlWidth = false;
            layout.childControlHeight = true;
            layout.childForceExpandWidth = false;
            layout.childForceExpandHeight = true;

            BuildActionPanelTabs(panel);
            BuildOperationGrid(panel);
            panel.gameObject.SetActive(false);
        }

        private void BuildActionPanelTabs(RectTransform parent)
        {
            RectTransform tabs = CreateRect("CategoryTabs", parent);
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
            CreateBottomActionButton(panel, "Gestisci lavori", active: false);
            CreateBottomActionButton(panel, "Zone", active: false);
            CreateBottomActionButton(panel, "Oggetti", active: false);
            CreateBottomActionButton(panel, "NPC", active: false);
            CreateBottomActionButton(panel, "Istituzioni", active: false);
            CreateBottomActionButton(panel, "Ricerca", active: false);
        }

        private void BuildOverlayRoots()
        {
            _overlayRoot = CreateRect("OverlayRoot", _uiRoot.transform);
            StretchFull(_overlayRoot);

            RectTransform debugRoot = CreateRect("DebugRoot", _uiRoot.transform);
            StretchFull(debugRoot);
            debugRoot.gameObject.SetActive(false);
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
            layout.preferredHeight = preferredHeight;
            if (preferredWidth > 0f)
                layout.preferredWidth = preferredWidth;

            return rect;
        }

        private static Button CreateIconButton(RectTransform parent, string label)
        {
            RectTransform button = CreateButtonShell(parent, "ArcButton_ViewMode_" + label, 42f, 30f, false);
            CreateText(button, label, 10, FontStyles.Bold, TextAlignmentOptions.Center);
            return button.GetComponent<Button>();
        }

        private static void CreateTopBarText(RectTransform parent, string label)
        {
            RectTransform textRoot = CreateRect("ArcInfoRow_Top_" + SanitizeName(label), parent);
            LayoutElement layout = textRoot.gameObject.AddComponent<LayoutElement>();
            layout.minWidth = 74f;
            layout.preferredWidth = 96f;
            layout.flexibleWidth = 0f;

            CreateText(textRoot, label, 14, FontStyles.Bold, TextAlignmentOptions.Center);
        }

        private static void CreateTopBarButton(RectTransform parent, string label)
        {
            RectTransform button = CreateButtonShell(parent, "ArcButton_Top_" + SanitizeName(label), 70f, 30f, false);
            CreateText(button, label, 12, FontStyles.Bold, TextAlignmentOptions.Center);
        }

        private static void CreateTabButton(RectTransform parent, string label, bool active)
        {
            RectTransform button = CreateButtonShell(parent, "ArcTabButton_" + SanitizeName(label), 42f, 30f, active);
            CreateText(button, label, 9, FontStyles.Bold, TextAlignmentOptions.Center);
        }

        private static void CreateCategoryButton(RectTransform parent, string label, bool active)
        {
            RectTransform button = CreateButtonShell(parent, "ArcButton_Category_" + SanitizeName(label), -1f, 28f, active);
            CreateText(button, label, 12, FontStyles.Bold, TextAlignmentOptions.Left);
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

        private static void CreateBottomActionButton(RectTransform parent, string label, bool active)
        {
            RectTransform button = CreateButtonShell(parent, "ArcButton_Bottom_" + SanitizeName(label), -1f, 72f, active);
            CreateText(button, label, 13, FontStyles.Bold, TextAlignmentOptions.Center);
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
                layout.preferredWidth = preferredWidth;
            if (preferredHeight > 0f)
                layout.preferredHeight = preferredHeight;

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
