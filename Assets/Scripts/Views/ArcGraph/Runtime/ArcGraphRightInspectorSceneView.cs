using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Arcontio.View.ArcGraph
{
    // =============================================================================
    // ArcGraphRightInspectorMode
    // =============================================================================
    /// <summary>
    /// <para>
    /// Modalita' contestuale del RightInspector ArcGraph.
    /// </para>
    ///
    /// <para><b>Principio architetturale: il pannello e' polifunzionale</b></para>
    /// <para>
    /// Il RightInspector non appartiene a un solo flusso. Puo' mostrare dati in
    /// sola lettura, aprire una futura modifica o preparare una conferma di
    /// eliminazione. La modalita' descrive il contesto UI, non un comando
    /// simulativo eseguito.
    /// </para>
    ///
    /// <para><b>Struttura interna:</b></para>
    /// <list type="bullet">
    ///   <item><b>Hidden</b>: nessun target valido.</item>
    ///   <item><b>View</b>: ispezione read-only del target selezionato.</item>
    ///   <item><b>EditRequested</b>: modifica richiesta, ma non ancora eseguita.</item>
    ///   <item><b>DeleteRequested</b>: eliminazione richiesta, ma non ancora eseguita.</item>
    /// </list>
    /// </summary>
    public enum ArcGraphRightInspectorMode
    {
        Hidden = 0,
        View = 1,
        EditRequested = 2,
        DeleteRequested = 3
    }

    // =============================================================================
    // ArcGraphRightInspectorSceneView
    // =============================================================================
    /// <summary>
    /// <para>
    /// View UGUI runtime del RightInspector ArcGraph.
    /// </para>
    ///
    /// <para><b>Principio architetturale: inspector come consumer di contratti UI</b></para>
    /// <para>
    /// Questo componente legge solo <see cref="ArcUiSelectionTarget"/> e
    /// <see cref="ArcUiSelectionActionRequest"/> gia' prodotti dai layer di
    /// selezione. Non legge il <c>World</c>, non interroga NPC, non modifica
    /// oggetti, non cancella muri e non invia comandi. Il suo compito e' aprire il
    /// contenitore destro con una rappresentazione minima del contesto corrente.
    /// </para>
    ///
    /// <para><b>Struttura interna:</b></para>
    /// <list type="bullet">
    ///   <item><b>Update</b>: risolve selezione o richiesta pending e aggiorna il pannello.</item>
    ///   <item><b>EnsureBuilt</b>: riusa il nodo RightInspector creato dalla shell UI.</item>
    ///   <item><b>ArcUiInspectorViewModelFactory</b>: prepara tab e righe senza accesso al World.</item>
    ///   <item><b>RenderViewModel</b>: disegna il ViewModel ricevuto dalla factory.</item>
    /// </list>
    /// </summary>
    public sealed class ArcGraphRightInspectorSceneView : MonoBehaviour
    {
        [SerializeField] private bool inspectorEnabled = true;

        private ArcGraphUiRuntimeRoot _uiRoot;
        private ArcGraphUiSelectionSceneConsumer _selectionConsumer;
        private ArcUiSelectionActionController _selectionActionController;
        private ArcUiInspectionController _inspectionController;
        private readonly ArcUiInspectorViewModelFactory _viewModelFactory = new();
        private RectTransform _panelRoot;
        private RectTransform _headerRoot;
        private RectTransform _tabRoot;
        private RectTransform _contentRoot;
        private TextMeshProUGUI _titleText;
        private TextMeshProUGUI _subtitleText;
        private ArcGraphRightInspectorMode _lastMode = ArcGraphRightInspectorMode.Hidden;
        private ArcUiSelectionTarget _lastTarget = ArcUiSelectionTarget.None("right_inspector");

        // =============================================================================
        // Update
        // =============================================================================
        /// <summary>
        /// <para>
        /// Aggiorna apertura e contenuto del RightInspector.
        /// </para>
        /// </summary>
        private void Update()
        {
            if (!inspectorEnabled)
            {
                HideInspector();
                return;
            }

            ResolveContext(
                out ArcGraphRightInspectorMode mode,
                out ArcUiSelectionTarget target,
                out ArcUiSelectionActionRequest actionRequest);

            if (mode == ArcGraphRightInspectorMode.Hidden || !target.IsValid)
            {
                HideInspector();
                return;
            }

            if (!EnsureBuilt())
                return;

            if (mode == _lastMode && SameTarget(target, _lastTarget))
            {
                if (!_panelRoot.gameObject.activeSelf)
                    _panelRoot.gameObject.SetActive(true);

                return;
            }

            _lastMode = mode;
            _lastTarget = target;
            ApplyContext(mode, target, actionRequest);
            _panelRoot.gameObject.SetActive(true);
        }

        // =============================================================================
        // SetUiRoot
        // =============================================================================
        /// <summary>
        /// <para>
        /// Assegna la root UI da cui recuperare il contenitore RightInspector.
        /// </para>
        /// </summary>
        public void SetUiRoot(ArcGraphUiRuntimeRoot uiRoot)
        {
            _uiRoot = uiRoot;
            _panelRoot = null;
        }

        // =============================================================================
        // SetSelectionConsumer
        // =============================================================================
        /// <summary>
        /// <para>
        /// Assegna il consumer che espone la selezione UI corrente.
        /// </para>
        /// </summary>
        public void SetSelectionConsumer(ArcGraphUiSelectionSceneConsumer selectionConsumer)
        {
            _selectionConsumer = selectionConsumer;
        }

        // =============================================================================
        // SetSelectionActionController
        // =============================================================================
        /// <summary>
        /// <para>
        /// Assegna il controller che contiene eventuali richieste Edit/Delete.
        /// </para>
        /// </summary>
        public void SetSelectionActionController(ArcUiSelectionActionController controller)
        {
            _selectionActionController = controller;
        }

        // =============================================================================
        // SetInspectionController
        // =============================================================================
        /// <summary>
        /// <para>
        /// Assegna il controller shell dell'inspector.
        /// </para>
        /// </summary>
        public void SetInspectionController(ArcUiInspectionController controller)
        {
            _inspectionController = controller;
        }

        // =============================================================================
        // SetInspectorEnabled
        // =============================================================================
        /// <summary>
        /// <para>
        /// Abilita o disabilita il pannello senza distruggere la gerarchia UGUI.
        /// </para>
        /// </summary>
        public void SetInspectorEnabled(bool enabled)
        {
            inspectorEnabled = enabled;

            if (!enabled)
                HideInspector();
        }

        private void ResolveContext(
            out ArcGraphRightInspectorMode mode,
            out ArcUiSelectionTarget target,
            out ArcUiSelectionActionRequest actionRequest)
        {
            mode = ArcGraphRightInspectorMode.Hidden;
            target = ArcUiSelectionTarget.None("right_inspector");
            actionRequest = default;

            ArcUiSelectionTarget currentSelection = _selectionConsumer != null
                ? _selectionConsumer.CurrentSelection
                : ArcUiSelectionTarget.None("right_inspector");

            ArcUiSelectionActionRequest pending = _selectionActionController != null
                ? _selectionActionController.Pending
                : default;

            if (pending.IsValid)
            {
                if (currentSelection.IsValid && !SameTarget(pending.Target, currentSelection))
                {
                    _selectionActionController.Clear();
                }
                else
                {
                    actionRequest = pending;
                    target = pending.Target;
                    mode = pending.IsEdit
                        ? ArcGraphRightInspectorMode.EditRequested
                        : ArcGraphRightInspectorMode.DeleteRequested;
                    return;
                }
            }

            if (!currentSelection.IsValid)
                return;

            target = currentSelection;
            mode = ArcGraphRightInspectorMode.View;
        }

        private bool EnsureBuilt()
        {
            if (_panelRoot != null)
                return true;

            if (_uiRoot == null || !_uiRoot.TryGetRightInspectorRoot(out RectTransform root))
                return false;

            _panelRoot = root;
            ClearChildren(_panelRoot);

            VerticalLayoutGroup layout = _panelRoot.GetComponent<VerticalLayoutGroup>();
            if (layout == null)
                layout = _panelRoot.gameObject.AddComponent<VerticalLayoutGroup>();

            layout.padding = new RectOffset(12, 12, 12, 12);
            layout.spacing = 8f;
            layout.childControlWidth = true;
            layout.childControlHeight = false;
            layout.childForceExpandWidth = true;
            layout.childForceExpandHeight = false;

            _headerRoot = CreatePanelBlock(_panelRoot, "Header", 82f);
            _tabRoot = CreateRect("TabBar", _panelRoot);
            LayoutElement tabLayout = _tabRoot.gameObject.AddComponent<LayoutElement>();
            tabLayout.preferredHeight = 34f;
            HorizontalLayoutGroup tabGroup = _tabRoot.gameObject.AddComponent<HorizontalLayoutGroup>();
            tabGroup.spacing = 4f;
            tabGroup.childControlWidth = false;
            tabGroup.childControlHeight = true;
            tabGroup.childForceExpandWidth = false;
            tabGroup.childForceExpandHeight = true;

            _contentRoot = CreatePanelBlock(_panelRoot, "ContentRoot", 560f);
            _panelRoot.gameObject.SetActive(false);
            return true;
        }

        private void ApplyContext(
            ArcGraphRightInspectorMode mode,
            ArcUiSelectionTarget target,
            ArcUiSelectionActionRequest actionRequest)
        {
            ArcUiInspectorViewModel viewModel = actionRequest.IsValid
                ? _viewModelFactory.BuildForAction(actionRequest)
                : _viewModelFactory.BuildForSelection(target);

            RenderViewModel(mode, viewModel);

            if (_inspectionController != null)
                _inspectionController.Set(viewModel);
        }

        private void RenderViewModel(
            ArcGraphRightInspectorMode mode,
            ArcUiInspectorViewModel viewModel)
        {
            BuildHeader(mode, viewModel);
            BuildTabs(viewModel);
            BuildRows(viewModel);
        }

        private void BuildHeader(
            ArcGraphRightInspectorMode mode,
            ArcUiInspectorViewModel viewModel)
        {
            ClearChildren(_headerRoot);

            VerticalLayoutGroup layout = GetOrAddVerticalLayout(_headerRoot);
            layout.padding = new RectOffset(10, 10, 8, 8);
            layout.spacing = 2f;
            layout.childControlWidth = true;
            layout.childControlHeight = false;
            layout.childForceExpandWidth = true;
            layout.childForceExpandHeight = false;

            _titleText = CreateText(
                _headerRoot,
                string.IsNullOrWhiteSpace(viewModel.Title) ? "Selezione" : viewModel.Title,
                17,
                FontStyles.Bold,
                TextAlignmentOptions.Left);
            _subtitleText = CreateText(
                _headerRoot,
                ResolveModeLabel(mode),
                12,
                FontStyles.Normal,
                TextAlignmentOptions.Left);
            CreateText(
                _headerRoot,
                ResolveTargetKindLabel(viewModel.Target.Kind),
                12,
                FontStyles.Normal,
                TextAlignmentOptions.Left);
        }

        private void BuildTabs(ArcUiInspectorViewModel viewModel)
        {
            ClearChildren(_tabRoot);

            if (!viewModel.HasTabs)
                return;

            for (int i = 0; i < viewModel.Tabs.Length; i++)
            {
                ArcUiInspectorTab tab = viewModel.Tabs[i];
                bool active = string.Equals(
                    tab.TabKey,
                    viewModel.ActiveTabKey,
                    System.StringComparison.Ordinal);
                CreateTabButton(_tabRoot, tab.Label, active);
            }
        }

        private void BuildRows(ArcUiInspectorViewModel viewModel)
        {
            ClearChildren(_contentRoot);

            VerticalLayoutGroup layout = GetOrAddVerticalLayout(_contentRoot);
            layout.padding = new RectOffset(10, 10, 10, 10);
            layout.spacing = 7f;
            layout.childControlWidth = true;
            layout.childControlHeight = false;
            layout.childForceExpandWidth = true;
            layout.childForceExpandHeight = false;

            if (!TryResolveActiveTab(viewModel, out ArcUiInspectorTab activeTab))
                return;

            CreateSectionTitle(_contentRoot, string.IsNullOrWhiteSpace(activeTab.Label) ? "INFO" : activeTab.Label.ToUpperInvariant());

            for (int i = 0; i < activeTab.Rows.Length; i++)
            {
                ArcUiInspectorRow row = activeTab.Rows[i];
                CreateInfoRow(_contentRoot, row.Label, row.Value);
            }
        }

        private static bool TryResolveActiveTab(
            ArcUiInspectorViewModel viewModel,
            out ArcUiInspectorTab activeTab)
        {
            activeTab = default;

            if (!viewModel.HasTabs)
                return false;

            for (int i = 0; i < viewModel.Tabs.Length; i++)
            {
                ArcUiInspectorTab tab = viewModel.Tabs[i];
                if (string.Equals(tab.TabKey, viewModel.ActiveTabKey, System.StringComparison.Ordinal))
                {
                    activeTab = tab;
                    return true;
                }
            }

            activeTab = viewModel.Tabs[0];
            return activeTab.IsValid;
        }

        private void HideInspector()
        {
            _lastMode = ArcGraphRightInspectorMode.Hidden;
            _lastTarget = ArcUiSelectionTarget.None("right_inspector");
            _inspectionController?.Clear();

            if (_panelRoot != null && _panelRoot.gameObject.activeSelf)
                _panelRoot.gameObject.SetActive(false);
        }

        private static bool SameTarget(
            ArcUiSelectionTarget left,
            ArcUiSelectionTarget right)
        {
            return left.Kind == right.Kind &&
                   string.Equals(left.Id, right.Id, System.StringComparison.Ordinal) &&
                   left.Cell.X == right.Cell.X &&
                   left.Cell.Y == right.Cell.Y &&
                   left.Cell.Z == right.Cell.Z;
        }

        private static string ResolveModeLabel(ArcGraphRightInspectorMode mode)
        {
            return mode switch
            {
                ArcGraphRightInspectorMode.EditRequested => "Modifica richiesta",
                ArcGraphRightInspectorMode.DeleteRequested => "Eliminazione richiesta",
                ArcGraphRightInspectorMode.View => "Ispezione",
                _ => "Nascosto"
            };
        }

        private static string ResolveTargetKindLabel(ArcUiSelectionTargetKind kind)
        {
            return kind switch
            {
                ArcUiSelectionTargetKind.Npc => "NPC",
                ArcUiSelectionTargetKind.Object => "Oggetto",
                ArcUiSelectionTargetKind.Wall => "Muro",
                ArcUiSelectionTargetKind.Cell => "Cella",
                ArcUiSelectionTargetKind.Plant => "Pianta",
                ArcUiSelectionTargetKind.Zone => "Zona",
                ArcUiSelectionTargetKind.Debug => "Debug",
                _ => "Nessun target"
            };
        }

        private static void ClearChildren(RectTransform root)
        {
            if (root == null)
                return;

            for (int i = root.childCount - 1; i >= 0; i--)
                Destroy(root.GetChild(i).gameObject);
        }

        private static VerticalLayoutGroup GetOrAddVerticalLayout(RectTransform root)
        {
            VerticalLayoutGroup layout = root.GetComponent<VerticalLayoutGroup>();
            if (layout == null)
                layout = root.gameObject.AddComponent<VerticalLayoutGroup>();

            return layout;
        }

        private static RectTransform CreatePanelBlock(
            RectTransform parent,
            string name,
            float preferredHeight)
        {
            RectTransform rect = CreateRect(name, parent);
            Image image = rect.gameObject.AddComponent<Image>();
            image.raycastTarget = true;
            image.color = ColorFromHex("#18222C", 0.84f);

            LayoutElement layout = rect.gameObject.AddComponent<LayoutElement>();
            layout.preferredHeight = preferredHeight;
            return rect;
        }

        private static void CreateTabButton(
            RectTransform parent,
            string label,
            bool active)
        {
            RectTransform button = CreateRect("ArcTabButton_" + SanitizeName(label), parent);
            Image image = button.gameObject.AddComponent<Image>();
            image.raycastTarget = true;
            image.color = active
                ? ColorFromHex("#324557", 0.94f)
                : ColorFromHex("#17212B", 0.92f);

            button.gameObject.AddComponent<Button>();
            LayoutElement layout = button.gameObject.AddComponent<LayoutElement>();
            layout.preferredWidth = Mathf.Max(44f, label.Length * 8f);
            layout.preferredHeight = 30f;
            CreateText(button, label, 10, FontStyles.Bold, TextAlignmentOptions.Center);
        }

        private static void CreateSectionTitle(
            RectTransform parent,
            string label)
        {
            RectTransform title = CreateRect("SectionTitle_" + SanitizeName(label), parent);
            LayoutElement layout = title.gameObject.AddComponent<LayoutElement>();
            layout.preferredHeight = 24f;
            CreateText(title, label, 12, FontStyles.Bold, TextAlignmentOptions.Left);
        }

        private static void CreateInfoRow(
            RectTransform parent,
            string label,
            string value)
        {
            RectTransform row = CreateRect("ArcInfoRow_" + SanitizeName(label), parent);
            LayoutElement rowLayout = row.gameObject.AddComponent<LayoutElement>();
            rowLayout.preferredHeight = 26f;

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
            valueLayout.preferredWidth = 172f;
            CreateText(valueRoot, value, 12, FontStyles.Bold, TextAlignmentOptions.Right);
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

        private static void StretchFull(
            RectTransform rect,
            Vector2 offsetMin,
            Vector2 offsetMax)
        {
            rect.anchorMin = Vector2.zero;
            rect.anchorMax = Vector2.one;
            rect.offsetMin = offsetMin;
            rect.offsetMax = offsetMax;
            rect.localScale = Vector3.one;
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
