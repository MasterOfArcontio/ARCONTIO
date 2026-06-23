using System.Collections.Generic;
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
        private const float TabCellWidth = 74f;
        private const float TabCellHeight = 30f;
        private const float TabSpacing = 4f;
        private const int TabColumnsPerRow = 4;
        private const float RuntimeRefreshIntervalSeconds = 0.5f;
        private const float CompactRowSpacing = 2f;
        private const float CompactTextRowHeight = 20f;
        private const float CompactSectionHeight = 20f;
        private const float CompactBarRowHeight = 31f;
        private const float CompactBarHeaderHeight = 16f;
        private const float CompactBarHeight = 7f;
        private const float CompactMetricRowHeight = 40f;
        private const float CompactMetricTitleHeight = 14f;
        private const float CompactMetricBodyHeight = 22f;
        private const float CompactExpandableHeight = 24f;
        private const float CompactTimelineHeight = 20f;
        private const float CompactDetailIndent = 14f;
        private const float DenseRowSpacing = 0f;
        private const float DenseTextRowHeight = 16f;
        private const float DenseSectionHeight = 16f;
        private const float DenseBarRowHeight = 25f;
        private const float DenseBarHeaderHeight = 14f;
        private const float DenseBarHeight = 4f;
        private const float DenseMetricRowHeight = 32f;
        private const float DenseMetricTitleHeight = 12f;
        private const float DenseMetricBodyHeight = 18f;
        private const float DenseExpandableHeight = 20f;
        private const float DenseTimelineHeight = 16f;
        private const float DenseDetailIndent = 9f;
        private const int DenseFontSize = 9;
        private const int DenseToggleFontSize = 11;

        [SerializeField] private bool inspectorEnabled = true;

        private ArcGraphUiRuntimeRoot _uiRoot;
        private ArcGraphUiSelectionSceneConsumer _selectionConsumer;
        private ArcUiSelectionActionController _selectionActionController;
        private ArcUiInspectionController _inspectionController;
        private readonly ArcUiInspectorViewModelFactory _viewModelFactory = new();
        private readonly ArcUiInspectorRuntimeSnapshotProvider _runtimeSnapshotProvider = new();
        private RectTransform _panelRoot;
        private RectTransform _headerRoot;
        private RectTransform _tabRoot;
        private RectTransform _contentRoot;
        private RectTransform _contentScrollContent;
        private ScrollRect _contentScrollRect;
        private LayoutElement _tabLayout;
        private TextMeshProUGUI _titleText;
        private TextMeshProUGUI _subtitleText;
        private ArcUiInspectorViewModel _currentViewModel = ArcUiInspectorViewModel.Empty();
        private string _activeTabKey = string.Empty;
        private ArcGraphRightInspectorMode _lastMode = ArcGraphRightInspectorMode.Hidden;
        private ArcUiSelectionTarget _lastTarget = ArcUiSelectionTarget.None("right_inspector");
        private float _lastRuntimeRefreshTime = -999f;
        private readonly Dictionary<string, bool> _expandedRows = new();

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

                if (ShouldRefreshRuntimeInspector(target))
                {
                    ApplyContext(mode, target, actionRequest, true, false);
                    _lastRuntimeRefreshTime = Time.unscaledTime;
                }

                return;
            }

            _lastMode = mode;
            _lastTarget = target;
            ApplyContext(mode, target, actionRequest);
            _lastRuntimeRefreshTime = Time.unscaledTime;
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
        // SetRuntimeContextProvider
        // =============================================================================
        /// <summary>
        /// <para>
        /// Assegna il provider runtime autorizzato usato dalla factory per creare
        /// snapshot read-only dell'inspector.
        /// </para>
        /// </summary>
        public void SetRuntimeContextProvider(ArcGraphRuntimeContextProvider provider)
        {
            _runtimeSnapshotProvider.SetRuntimeContextProvider(provider);
            _viewModelFactory.SetRuntimeSnapshotProvider(_runtimeSnapshotProvider);
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
            layout.spacing = 5f;
            layout.childControlWidth = true;
            layout.childControlHeight = true;
            layout.childForceExpandWidth = true;
            layout.childForceExpandHeight = false;

            _headerRoot = CreatePanelBlock(_panelRoot, "Header", 112f);
            _tabRoot = CreateRect("TabBar", _panelRoot);
            _tabLayout = _tabRoot.gameObject.AddComponent<LayoutElement>();
            _tabLayout.preferredHeight = TabCellHeight;

            GridLayoutGroup tabGroup = _tabRoot.gameObject.AddComponent<GridLayoutGroup>();
            tabGroup.cellSize = new Vector2(TabCellWidth, TabCellHeight);
            tabGroup.spacing = new Vector2(TabSpacing, TabSpacing);
            tabGroup.childAlignment = TextAnchor.UpperLeft;
            tabGroup.constraint = GridLayoutGroup.Constraint.FixedColumnCount;
            tabGroup.constraintCount = TabColumnsPerRow;

            _contentRoot = CreatePanelBlock(_panelRoot, "ContentRoot", 560f);
            BuildContentScroll(_contentRoot);
            _panelRoot.gameObject.SetActive(false);
            return true;
        }

        private void ApplyContext(
            ArcGraphRightInspectorMode mode,
            ArcUiSelectionTarget target,
            ArcUiSelectionActionRequest actionRequest,
            bool preserveActiveTab = false,
            bool resetScrollToTop = true)
        {
            ArcUiInspectorViewModel viewModel = actionRequest.IsValid
                ? _viewModelFactory.BuildForAction(actionRequest)
                : _viewModelFactory.BuildForSelection(target);

            _currentViewModel = viewModel;
            _activeTabKey = preserveActiveTab && ContainsTab(viewModel, _activeTabKey)
                ? _activeTabKey
                : ResolveInitialActiveTabKey(viewModel);
            RenderViewModel(mode, viewModel, resetScrollToTop);

            if (_inspectionController != null)
                _inspectionController.Set(viewModel);
        }

        private void RenderViewModel(
            ArcGraphRightInspectorMode mode,
            ArcUiInspectorViewModel viewModel,
            bool resetScrollToTop)
        {
            BuildHeader(mode, viewModel);
            BuildTabs(viewModel);
            BuildRows(viewModel, resetScrollToTop);
        }

        private void BuildHeader(
            ArcGraphRightInspectorMode mode,
            ArcUiInspectorViewModel viewModel)
        {
            ClearChildren(_headerRoot);

            if (viewModel.Target.Kind == ArcUiSelectionTargetKind.Npc)
            {
                BuildNpcHeader(mode, viewModel);
                return;
            }

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
                18,
                FontStyles.Bold,
                TextAlignmentOptions.Left);
            _subtitleText = CreateText(
                _headerRoot,
                ResolveModeLabel(mode),
                13,
                FontStyles.Normal,
                TextAlignmentOptions.Left);
            CreateText(
                _headerRoot,
                ResolveTargetKindLabel(viewModel.Target.Kind),
                13,
                FontStyles.Normal,
                TextAlignmentOptions.Left);
        }

        private void BuildNpcHeader(
            ArcGraphRightInspectorMode mode,
            ArcUiInspectorViewModel viewModel)
        {
            VerticalLayoutGroup rootLayout = GetOrAddVerticalLayout(_headerRoot);
            rootLayout.padding = new RectOffset(10, 10, 8, 8);
            rootLayout.spacing = 0f;
            rootLayout.childControlWidth = true;
            rootLayout.childControlHeight = true;
            rootLayout.childForceExpandWidth = true;
            rootLayout.childForceExpandHeight = false;

            RectTransform row = CreateRect("NpcHeaderRow", _headerRoot);
            LayoutElement rowLayout = row.gameObject.AddComponent<LayoutElement>();
            rowLayout.preferredHeight = 92f;
            rowLayout.flexibleHeight = 0f;

            HorizontalLayoutGroup horizontal = row.gameObject.AddComponent<HorizontalLayoutGroup>();
            horizontal.padding = new RectOffset(0, 0, 0, 0);
            horizontal.spacing = 10f;
            horizontal.childControlWidth = true;
            horizontal.childControlHeight = true;
            horizontal.childForceExpandWidth = false;
            horizontal.childForceExpandHeight = false;

            CreateNpcPortraitSlot(row);

            RectTransform details = CreateRect("NpcHeaderDetails", row);
            LayoutElement detailsLayout = details.gameObject.AddComponent<LayoutElement>();
            detailsLayout.flexibleWidth = 1f;
            detailsLayout.preferredHeight = 84f;
            detailsLayout.flexibleHeight = 0f;

            VerticalLayoutGroup detailsGroup = details.gameObject.AddComponent<VerticalLayoutGroup>();
            detailsGroup.padding = new RectOffset(0, 0, 0, 0);
            detailsGroup.spacing = 0f;
            detailsGroup.childControlWidth = true;
            detailsGroup.childControlHeight = true;
            detailsGroup.childForceExpandWidth = true;
            detailsGroup.childForceExpandHeight = false;

            _titleText = CreateText(
                details,
                string.IsNullOrWhiteSpace(viewModel.Title) ? "NPC" : viewModel.Title,
                18,
                FontStyles.Bold,
                TextAlignmentOptions.Left);
            _subtitleText = CreateText(
                details,
                ResolveModeLabel(mode),
                13,
                FontStyles.Normal,
                TextAlignmentOptions.Left);
            CreateText(
                details,
                "Id " + (string.IsNullOrWhiteSpace(viewModel.Target.Id) ? "--" : viewModel.Target.Id),
                13,
                FontStyles.Normal,
                TextAlignmentOptions.Left);
            CreateText(
                details,
                FormatHeaderCell(viewModel.Target.Cell),
                12,
                FontStyles.Normal,
                TextAlignmentOptions.Left);
        }

        private void BuildTabs(ArcUiInspectorViewModel viewModel)
        {
            ClearChildren(_tabRoot);
            ApplyTabLayoutHeight(viewModel.HasTabs ? viewModel.Tabs.Length : 0);

            if (!viewModel.HasTabs)
                return;

            for (int i = 0; i < viewModel.Tabs.Length; i++)
            {
                ArcUiInspectorTab tab = viewModel.Tabs[i];
                bool active = string.Equals(
                    tab.TabKey,
                    _activeTabKey,
                    System.StringComparison.Ordinal);

                string tabKey = tab.TabKey;
                Button button = CreateTabButton(_tabRoot, tab.Label, active);
                button.onClick.AddListener(() => SelectTab(tabKey));
            }
        }

        private void BuildRows(
            ArcUiInspectorViewModel viewModel,
            bool resetScrollToTop)
        {
            RectTransform rowsRoot = _contentScrollContent != null
                ? _contentScrollContent
                : _contentRoot;

            ClearChildren(rowsRoot);

            VerticalLayoutGroup layout = GetOrAddVerticalLayout(rowsRoot);
            layout.padding = new RectOffset(10, 10, 10, 10);
            layout.spacing = CompactRowSpacing;
            layout.childControlWidth = true;
            layout.childControlHeight = true;
            layout.childForceExpandWidth = true;
            layout.childForceExpandHeight = false;

            if (!TryResolveActiveTab(viewModel, _activeTabKey, out ArcUiInspectorTab activeTab))
                return;

            CreateSectionTitle(rowsRoot, string.IsNullOrWhiteSpace(activeTab.Label) ? "INFO" : activeTab.Label.ToUpperInvariant(), false);

            for (int i = 0; i < activeTab.Rows.Length; i++)
            {
                ArcUiInspectorRow row = activeTab.Rows[i];
                CreateInspectorRow(rowsRoot, row);
            }

            FinalizeScrollContentLayout(rowsRoot, resetScrollToTop);
        }

        private void FinalizeScrollContentLayout(
            RectTransform rowsRoot,
            bool resetScrollToTop)
        {
            if (rowsRoot == null)
                return;

            // Lo ScrollRect di Unity puo' lasciare il Content a zero altezza se
            // aspettiamo soltanto il ContentSizeFitter. Qui rendiamo esplicita
            // l'altezza preferita prodotta dalle righe appena create, cosi' il
            // RectMask2D della viewport non taglia tutto il contenuto.
            LayoutRebuilder.ForceRebuildLayoutImmediate(rowsRoot);

            float preferredHeight = LayoutUtility.GetPreferredHeight(rowsRoot);
            if (preferredHeight <= 1f)
                preferredHeight = EstimateChildrenPreferredHeight(rowsRoot);

            rowsRoot.SetSizeWithCurrentAnchors(
                RectTransform.Axis.Vertical,
                Mathf.Max(1f, preferredHeight));

            if (resetScrollToTop)
            {
                rowsRoot.anchoredPosition = Vector2.zero;
                if (_contentScrollRect != null)
                    _contentScrollRect.verticalNormalizedPosition = 1f;
            }

            LayoutRebuilder.ForceRebuildLayoutImmediate(rowsRoot);
        }

        private static float EstimateChildrenPreferredHeight(RectTransform root)
        {
            VerticalLayoutGroup layout = root != null ? root.GetComponent<VerticalLayoutGroup>() : null;
            float height = layout != null ? layout.padding.top + layout.padding.bottom : 0f;
            int visibleChildren = 0;

            if (root == null)
                return height;

            for (int i = 0; i < root.childCount; i++)
            {
                RectTransform child = root.GetChild(i) as RectTransform;
                if (child == null || !child.gameObject.activeSelf)
                    continue;

                float childHeight = LayoutUtility.GetPreferredHeight(child);
                if (childHeight <= 0f)
                {
                    LayoutElement element = child.GetComponent<LayoutElement>();
                    childHeight = element != null && element.preferredHeight > 0f
                        ? element.preferredHeight
                        : Mathf.Max(1f, child.rect.height);
                }

                height += childHeight;
                visibleChildren++;
            }

            if (layout != null && visibleChildren > 1)
                height += layout.spacing * (visibleChildren - 1);

            return height;
        }

        private static bool TryResolveActiveTab(
            ArcUiInspectorViewModel viewModel,
            string activeTabKey,
            out ArcUiInspectorTab activeTab)
        {
            activeTab = default;

            if (!viewModel.HasTabs)
                return false;

            for (int i = 0; i < viewModel.Tabs.Length; i++)
            {
                ArcUiInspectorTab tab = viewModel.Tabs[i];
                if (string.Equals(tab.TabKey, activeTabKey, System.StringComparison.Ordinal))
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
            _lastRuntimeRefreshTime = -999f;
            _inspectionController?.Clear();

            if (_panelRoot != null && _panelRoot.gameObject.activeSelf)
                _panelRoot.gameObject.SetActive(false);
        }

        private bool ShouldRefreshRuntimeInspector(ArcUiSelectionTarget target)
        {
            // Per ora solo la scheda NPC legge dati runtime che cambiano nel tempo
            // mentre la selezione resta la stessa: bisogni, azione corrente, job ed
            // explainability. Oggetti e muri restano statici finche' non colleghiamo
            // i rispettivi snapshot nello step successivo.
            if (target.Kind != ArcUiSelectionTargetKind.Npc)
                return false;

            return Time.unscaledTime - _lastRuntimeRefreshTime >= RuntimeRefreshIntervalSeconds;
        }

        // =============================================================================
        // SelectTab
        // =============================================================================
        /// <summary>
        /// <para>
        /// Cambia la tab visualizzata senza interrogare la simulazione.
        /// </para>
        ///
        /// <para><b>Principio architetturale: tab switching locale</b></para>
        /// <para>
        /// Il click su una tab opera solo sul ViewModel gia' consegnato
        /// all'inspector. Non rilegge il World, non ricostruisce target e non
        /// produce comandi: aggiorna soltanto la sezione read-only visibile.
        /// </para>
        /// </summary>
        private void SelectTab(string tabKey)
        {
            if (!_currentViewModel.HasTabs || !ContainsTab(_currentViewModel, tabKey))
                return;

            _activeTabKey = tabKey;
            _currentViewModel = new ArcUiInspectorViewModel(
                _currentViewModel.Title,
                _currentViewModel.Target,
                _currentViewModel.Tabs,
                _activeTabKey);

            BuildTabs(_currentViewModel);
            BuildRows(_currentViewModel, true);
            _inspectionController?.Set(_currentViewModel);
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

        private static string ResolveInitialActiveTabKey(ArcUiInspectorViewModel viewModel)
        {
            // Se la factory ha indicato una tab iniziale valida la rispettiamo.
            // In caso contrario usiamo la prima tab disponibile, cosi' il
            // pannello resta robusto anche con ViewModel incompleti o provvisori.
            if (!viewModel.HasTabs)
                return string.Empty;

            if (ContainsTab(viewModel, viewModel.ActiveTabKey))
                return viewModel.ActiveTabKey;

            return viewModel.Tabs[0].TabKey;
        }

        private static bool ContainsTab(
            ArcUiInspectorViewModel viewModel,
            string tabKey)
        {
            // Il controllo resta locale all'array del ViewModel corrente: non
            // cerchiamo tab in registry globali e non chiediamo dati aggiuntivi
            // alla simulazione.
            if (string.IsNullOrWhiteSpace(tabKey) || !viewModel.HasTabs)
                return false;

            for (int i = 0; i < viewModel.Tabs.Length; i++)
            {
                if (string.Equals(viewModel.Tabs[i].TabKey, tabKey, System.StringComparison.Ordinal))
                    return true;
            }

            return false;
        }

        private void ApplyTabLayoutHeight(int tabCount)
        {
            if (_tabLayout == null)
                return;

            // Il pannello usa due righe al massimo per questa fase. In futuro,
            // se le tab diventeranno molte, potremo introdurre scroll orizzontale
            // o gruppi secondari senza cambiare il contratto ViewModel.
            int rowCount = Mathf.Max(1, Mathf.CeilToInt(tabCount / (float)TabColumnsPerRow));
            rowCount = Mathf.Min(2, rowCount);
            _tabLayout.preferredHeight = rowCount * TabCellHeight + (rowCount - 1) * TabSpacing;
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

        private static string FormatHeaderCell(ArcGraphCellCoord cell)
        {
            return "col " + cell.X + " | riga " + cell.Y + " | z " + cell.Z;
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

        private void BuildContentScroll(RectTransform root)
        {
            // Il RightInspector deve poter ospitare liste Memory/Belief/Job senza
            // uscire dal pannello. Lo scroll vive dentro ContentRoot e non cambia
            // il contratto dei dati: le righe restano gia' preparate dal ViewModel.
            _contentScrollRect = root.gameObject.AddComponent<ScrollRect>();
            _contentScrollRect.horizontal = false;
            _contentScrollRect.vertical = true;
            _contentScrollRect.movementType = ScrollRect.MovementType.Clamped;
            _contentScrollRect.scrollSensitivity = 24f;

            RectTransform viewport = CreateRect("Viewport", root);
            StretchFull(viewport, new Vector2(0f, 0f), new Vector2(-10f, 0f));
            Image viewportImage = viewport.gameObject.AddComponent<Image>();
            viewportImage.raycastTarget = true;
            viewportImage.color = ColorFromHex("#000000", 0.01f);
            viewport.gameObject.AddComponent<RectMask2D>();

            _contentScrollContent = CreateRect("Content", viewport);
            _contentScrollContent.anchorMin = new Vector2(0f, 1f);
            _contentScrollContent.anchorMax = new Vector2(1f, 1f);
            _contentScrollContent.pivot = new Vector2(0.5f, 1f);
            _contentScrollContent.offsetMin = Vector2.zero;
            _contentScrollContent.offsetMax = Vector2.zero;

            VerticalLayoutGroup layout = _contentScrollContent.gameObject.AddComponent<VerticalLayoutGroup>();
            layout.childControlWidth = true;
            layout.childControlHeight = true;
            layout.childForceExpandWidth = true;
            layout.childForceExpandHeight = false;
            layout.spacing = CompactRowSpacing;
            layout.padding = new RectOffset(8, 8, 6, 10);

            ContentSizeFitter fitter = _contentScrollContent.gameObject.AddComponent<ContentSizeFitter>();
            fitter.verticalFit = ContentSizeFitter.FitMode.PreferredSize;

            Scrollbar scrollbar = CreateVerticalScrollbar(root);
            _contentScrollRect.viewport = viewport;
            _contentScrollRect.content = _contentScrollContent;
            _contentScrollRect.verticalScrollbar = scrollbar;
            _contentScrollRect.verticalScrollbarVisibility = ScrollRect.ScrollbarVisibility.Permanent;
            _contentScrollRect.verticalScrollbarSpacing = 2f;
        }

        private static Scrollbar CreateVerticalScrollbar(RectTransform parent)
        {
            RectTransform root = CreateRect("VerticalScrollbar", parent);
            root.anchorMin = new Vector2(1f, 0f);
            root.anchorMax = new Vector2(1f, 1f);
            root.pivot = new Vector2(1f, 0.5f);
            root.sizeDelta = new Vector2(8f, 0f);
            root.anchoredPosition = new Vector2(-1f, 0f);

            Image background = root.gameObject.AddComponent<Image>();
            background.raycastTarget = true;
            background.color = ColorFromHex("#0B1118", 0.82f);

            // La Scrollbar UGUI lavora meglio quando la maniglia vive dentro una
            // sliding area dedicata: in questo modo Unity puo' ridimensionarla in
            // base al rapporto tra viewport e contenuto scrollabile.
            RectTransform slidingArea = CreateRect("SlidingArea", root);
            StretchFull(slidingArea, new Vector2(1f, 1f), new Vector2(-1f, -1f));

            RectTransform handle = CreateRect("Handle", slidingArea);
            StretchFull(handle, Vector2.zero, Vector2.zero);
            Image handleImage = handle.gameObject.AddComponent<Image>();
            handleImage.raycastTarget = true;
            handleImage.color = ColorFromHex("#526777", 0.95f);

            Scrollbar scrollbar = root.gameObject.AddComponent<Scrollbar>();
            scrollbar.direction = Scrollbar.Direction.BottomToTop;
            scrollbar.targetGraphic = handleImage;
            scrollbar.handleRect = handle;
            scrollbar.size = 0.15f;
            scrollbar.value = 1f;
            return scrollbar;
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

        private static void CreateNpcPortraitSlot(RectTransform parent)
        {
            // Lo slot e' volutamente solo un riquadro UGUI. Quando arriveranno le
            // foto segnaletiche o gli sprite portrait, potremo sostituire il testo
            // interno con un'immagine senza cambiare il contratto ViewModel.
            RectTransform portrait = CreateRect("NpcPortraitSlot", parent);
            LayoutElement layout = portrait.gameObject.AddComponent<LayoutElement>();
            layout.minWidth = 56f;
            layout.preferredWidth = 56f;
            layout.preferredHeight = 84f;
            layout.flexibleWidth = 0f;
            layout.flexibleHeight = 0f;

            Image image = portrait.gameObject.AddComponent<Image>();
            image.raycastTarget = false;
            image.color = ColorFromHex("#0D151D", 0.96f);

            Outline outline = portrait.gameObject.AddComponent<Outline>();
            outline.effectColor = ColorFromHex("#405566", 0.92f);
            outline.effectDistance = new Vector2(1f, -1f);

            TextMeshProUGUI label = CreateText(
                portrait,
                "PORTRAIT",
                8,
                FontStyles.Normal,
                TextAlignmentOptions.Center);
            label.color = ColorFromHex("#7E91A0", 1f);
        }

        private static Button CreateTabButton(
            RectTransform parent,
            string label,
            bool active)
        {
            // Il bottone e' creato come componente UGUI standard. Non contiene
            // logica propria: il listener viene assegnato dal chiamante, che
            // conosce la chiave stabile della tab.
            RectTransform button = CreateRect("ArcTabButton_" + SanitizeName(label), parent);
            Image image = button.gameObject.AddComponent<Image>();
            image.raycastTarget = true;
            image.color = active
                ? ColorFromHex("#324557", 0.94f)
                : ColorFromHex("#17212B", 0.92f);

            Button buttonComponent = button.gameObject.AddComponent<Button>();
            LayoutElement layout = button.gameObject.AddComponent<LayoutElement>();
            layout.preferredWidth = TabCellWidth;
            layout.preferredHeight = TabCellHeight;
            CreateText(button, label, 10, FontStyles.Bold, TextAlignmentOptions.Center);
            return buttonComponent;
        }

        private static void CreateSectionTitle(
            RectTransform parent,
            string label,
            bool dense)
        {
            RectTransform title = CreateRect("SectionTitle_" + SanitizeName(label), parent);
            LayoutElement layout = title.gameObject.AddComponent<LayoutElement>();
            layout.preferredHeight = dense ? DenseSectionHeight : CompactSectionHeight;
            CreateText(title, label, dense ? DenseFontSize : 11, FontStyles.Bold, TextAlignmentOptions.Left);
        }

        private void CreateInspectorRow(
            RectTransform parent,
            ArcUiInspectorRow row,
            bool dense = false)
        {
            // Il renderer sceglie solo il widget UGUI in base al tipo gia'
            // dichiarato nel ViewModel. Non deduce dati dalla simulazione e non
            // modifica il contenuto ricevuto.
            switch (row.Kind)
            {
                case ArcUiInspectorRowKind.Section:
                    CreateSectionTitle(parent, row.Label, dense);
                    break;

                case ArcUiInspectorRowKind.Bar:
                    CreateBarRow(parent, row, dense);
                    break;

                case ArcUiInspectorRowKind.IconMetrics:
                    CreateIconMetricsRow(parent, row, dense);
                    break;

                case ArcUiInspectorRowKind.Expandable:
                    CreateExpandableRow(parent, row, dense);
                    break;

                case ArcUiInspectorRowKind.Timeline:
                    CreateTimelineRow(parent, row, dense);
                    break;

                default:
                    CreateInfoRow(parent, row.Label, row.Value, dense);
                    break;
            }
        }

        private static void CreateBarRow(
            RectTransform parent,
            ArcUiInspectorRow row,
            bool dense)
        {
            RectTransform root = CreateRect("ArcBarRow_" + SanitizeName(row.RowKey), parent);
            LayoutElement rootLayout = root.gameObject.AddComponent<LayoutElement>();
            rootLayout.preferredHeight = dense ? DenseBarRowHeight : CompactBarRowHeight;

            VerticalLayoutGroup layout = root.gameObject.AddComponent<VerticalLayoutGroup>();
            layout.spacing = dense ? DenseRowSpacing : 1f;
            layout.childControlWidth = true;
            layout.childControlHeight = true;
            layout.childForceExpandWidth = true;
            layout.childForceExpandHeight = false;

            RectTransform header = CreateRect("Header", root);
            LayoutElement headerLayout = header.gameObject.AddComponent<LayoutElement>();
            headerLayout.preferredHeight = dense ? DenseBarHeaderHeight : CompactBarHeaderHeight;

            HorizontalLayoutGroup headerGroup = header.gameObject.AddComponent<HorizontalLayoutGroup>();
            headerGroup.childControlWidth = true;
            headerGroup.childControlHeight = true;
            headerGroup.childForceExpandWidth = true;
            headerGroup.childForceExpandHeight = true;

            RectTransform labelRoot = CreateRect("Label", header);
            LayoutElement labelLayout = labelRoot.gameObject.AddComponent<LayoutElement>();
            labelLayout.flexibleWidth = 1f;
            CreateText(labelRoot, row.Label, dense ? DenseFontSize : 10, FontStyles.Normal, TextAlignmentOptions.Left);

            RectTransform valueRoot = CreateRect("Value", header);
            LayoutElement valueLayout = valueRoot.gameObject.AddComponent<LayoutElement>();
            valueLayout.preferredWidth = dense ? 46f : 56f;
            TextMeshProUGUI value = CreateText(valueRoot, row.Value, dense ? DenseFontSize : 10, FontStyles.Bold, TextAlignmentOptions.Right);
            value.color = ColorForSeverity(row.Severity, 1f);

            RectTransform barRoot = CreateRect("Bar", root);
            LayoutElement barLayout = barRoot.gameObject.AddComponent<LayoutElement>();
            barLayout.preferredHeight = dense ? DenseBarHeight : CompactBarHeight;
            Image barBackground = barRoot.gameObject.AddComponent<Image>();
            barBackground.raycastTarget = false;
            barBackground.color = ColorFromHex("#0B1118", 0.95f);

            RectTransform fill = CreateRect("Fill", barRoot);
            fill.anchorMin = Vector2.zero;
            fill.anchorMax = new Vector2(Mathf.Clamp01(row.NumericValue01), 1f);
            fill.offsetMin = Vector2.zero;
            fill.offsetMax = Vector2.zero;
            Image fillImage = fill.gameObject.AddComponent<Image>();
            fillImage.raycastTarget = false;
            fillImage.color = ColorForSeverity(row.Severity, 0.94f);

            CreateBarMarker(barRoot, row.AlertMarker01, "#D6A33A");
            CreateBarMarker(barRoot, row.CriticalMarker01, "#D85D5D");
        }

        private static void CreateBarMarker(
            RectTransform parent,
            float marker01,
            string colorHex)
        {
            if (marker01 < 0f)
                return;

            RectTransform marker = CreateRect("Marker", parent);
            marker.anchorMin = new Vector2(Mathf.Clamp01(marker01), 0f);
            marker.anchorMax = new Vector2(Mathf.Clamp01(marker01), 1f);
            marker.pivot = new Vector2(0.5f, 0.5f);
            marker.sizeDelta = new Vector2(2f, 0f);
            marker.anchoredPosition = Vector2.zero;

            Image image = marker.gameObject.AddComponent<Image>();
            image.raycastTarget = false;
            image.color = ColorFromHex(colorHex, 1f);
        }

        private static void CreateIconMetricsRow(
            RectTransform parent,
            ArcUiInspectorRow row,
            bool dense)
        {
            RectTransform root = CreateRect("ArcIconMetricsRow_" + SanitizeName(row.RowKey), parent);
            LayoutElement rootLayout = root.gameObject.AddComponent<LayoutElement>();
            rootLayout.preferredHeight = dense ? DenseMetricRowHeight : CompactMetricRowHeight;

            VerticalLayoutGroup rootGroup = root.gameObject.AddComponent<VerticalLayoutGroup>();
            rootGroup.spacing = dense ? DenseRowSpacing : 1f;
            rootGroup.childControlWidth = true;
            rootGroup.childControlHeight = true;
            rootGroup.childForceExpandWidth = true;
            rootGroup.childForceExpandHeight = false;

            RectTransform title = CreateRect("Title", root);
            LayoutElement titleLayout = title.gameObject.AddComponent<LayoutElement>();
            titleLayout.preferredHeight = dense ? DenseMetricTitleHeight : CompactMetricTitleHeight;
            TextMeshProUGUI titleText = CreateText(title, row.Label, dense ? DenseFontSize : 9, FontStyles.Bold, TextAlignmentOptions.Left);
            titleText.color = ColorFromHex("#A9B8C4", 1f);

            RectTransform metricsRoot = CreateRect("Metrics", root);
            LayoutElement metricsLayout = metricsRoot.gameObject.AddComponent<LayoutElement>();
            metricsLayout.preferredHeight = dense ? DenseMetricBodyHeight : CompactMetricBodyHeight;

            HorizontalLayoutGroup metricsGroup = metricsRoot.gameObject.AddComponent<HorizontalLayoutGroup>();
            metricsGroup.spacing = dense ? 1f : 3f;
            metricsGroup.childControlWidth = true;
            metricsGroup.childControlHeight = true;
            metricsGroup.childForceExpandWidth = true;
            metricsGroup.childForceExpandHeight = true;

            for (int i = 0; i < row.Metrics.Length; i++)
                CreateMetricCell(metricsRoot, row.Metrics[i], dense);
        }

        private static void CreateMetricCell(
            RectTransform parent,
            ArcUiInspectorMetric metric,
            bool dense)
        {
            RectTransform cell = CreateRect("Metric_" + SanitizeName(metric.Label), parent);
            Image background = cell.gameObject.AddComponent<Image>();
            background.raycastTarget = false;
            background.color = ColorFromHex("#111A23", 0.92f);

            LayoutElement cellLayout = cell.gameObject.AddComponent<LayoutElement>();
            cellLayout.flexibleWidth = 1f;

            HorizontalLayoutGroup group = cell.gameObject.AddComponent<HorizontalLayoutGroup>();
            group.padding = dense
                ? new RectOffset(1, 2, 1, 1)
                : new RectOffset(2, 3, 2, 2);
            group.spacing = dense ? 1f : 2f;
            group.childControlWidth = true;
            group.childControlHeight = true;
            group.childForceExpandWidth = false;
            group.childForceExpandHeight = true;

            RectTransform iconRoot = CreateRect("Icon", cell);
            LayoutElement iconLayout = iconRoot.gameObject.AddComponent<LayoutElement>();
            iconLayout.preferredWidth = dense ? 10f : 14f;
            Image iconBackground = iconRoot.gameObject.AddComponent<Image>();
            iconBackground.raycastTarget = false;
            iconBackground.color = ColorForSeverity(metric.Severity, 0.28f);

            string iconText = string.IsNullOrEmpty(metric.IconKey) ? "*" : metric.IconKey.Substring(0, 1).ToUpperInvariant();
            TextMeshProUGUI iconLabel = CreateText(iconRoot, iconText, dense ? DenseFontSize : 9, FontStyles.Bold, TextAlignmentOptions.Center);
            iconLabel.color = ColorForSeverity(metric.Severity, 1f);

            RectTransform valueRoot = CreateRect("Value", cell);
            LayoutElement valueLayout = valueRoot.gameObject.AddComponent<LayoutElement>();
            valueLayout.flexibleWidth = 1f;
            string valueText = string.IsNullOrEmpty(metric.Label)
                ? metric.Value
                : metric.Label + " " + metric.Value;
            TextMeshProUGUI value = CreateText(valueRoot, valueText, dense ? DenseFontSize : 9, FontStyles.Bold, TextAlignmentOptions.Left);
            value.color = ColorForSeverity(metric.Severity, 1f);
        }

        private void CreateExpandableRow(
            RectTransform parent,
            ArcUiInspectorRow row,
            bool dense)
        {
            bool expanded = IsRowExpanded(row);

            RectTransform root = CreateRect("ArcExpandableRow_" + SanitizeName(row.RowKey), parent);
            LayoutElement rootLayout = root.gameObject.AddComponent<LayoutElement>();
            rootLayout.preferredHeight = expanded
                ? ResolveExpandableHeight(dense) + EstimateRowsHeight(row.Details, true)
                : ResolveExpandableHeight(dense);

            VerticalLayoutGroup rootGroup = root.gameObject.AddComponent<VerticalLayoutGroup>();
            rootGroup.spacing = dense ? DenseRowSpacing : 2f;
            rootGroup.childControlWidth = true;
            rootGroup.childControlHeight = true;
            rootGroup.childForceExpandWidth = true;
            rootGroup.childForceExpandHeight = false;

            RectTransform header = CreateRect("Header", root);
            LayoutElement headerLayout = header.gameObject.AddComponent<LayoutElement>();
            headerLayout.preferredHeight = ResolveExpandableHeight(dense);
            Image headerImage = header.gameObject.AddComponent<Image>();
            headerImage.raycastTarget = true;
            headerImage.color = row.IsSelected
                ? ColorFromHex("#1E4A37", 0.9f)
                : ColorFromHex("#111A23", 0.92f);

            HorizontalLayoutGroup headerGroup = header.gameObject.AddComponent<HorizontalLayoutGroup>();
            headerGroup.padding = dense
                ? new RectOffset(2, 3, 1, 1)
                : new RectOffset(3, 4, 2, 2);
            headerGroup.spacing = dense ? 2f : 3f;
            headerGroup.childControlWidth = true;
            headerGroup.childControlHeight = true;
            headerGroup.childForceExpandWidth = false;
            headerGroup.childForceExpandHeight = true;

            Button toggleButton = CreateSmallButton(header, expanded ? "-" : "+", dense);
            string rowKey = row.RowKey;
            toggleButton.onClick.AddListener(() => ToggleRowExpanded(rowKey));

            RectTransform labelRoot = CreateRect("Label", header);
            LayoutElement labelLayout = labelRoot.gameObject.AddComponent<LayoutElement>();
            labelLayout.flexibleWidth = 1f;
            TextMeshProUGUI label = CreateText(labelRoot, row.Label, dense ? DenseFontSize : 9, FontStyles.Bold, TextAlignmentOptions.Left);
            label.color = row.IsSelected ? ColorFromHex("#9FE0B8", 1f) : ColorFromHex("#DDE6EE", 1f);

            RectTransform valueRoot = CreateRect("Value", header);
            LayoutElement valueLayout = valueRoot.gameObject.AddComponent<LayoutElement>();
            valueLayout.preferredWidth = dense ? 54f : 70f;
            TextMeshProUGUI value = CreateText(valueRoot, row.Value, dense ? DenseFontSize : 9, FontStyles.Bold, TextAlignmentOptions.Right);
            value.color = ColorForSeverity(row.Severity, 1f);

            if (!expanded)
                return;

            CreateDetailRows(root, row.Details);
        }

        private static Button CreateSmallButton(
            RectTransform parent,
            string label,
            bool dense)
        {
            RectTransform button = CreateRect("SmallButton_" + SanitizeName(label), parent);
            LayoutElement layout = button.gameObject.AddComponent<LayoutElement>();
            layout.preferredWidth = dense ? 15f : 16f;
            layout.preferredHeight = dense ? 15f : 16f;

            Image image = button.gameObject.AddComponent<Image>();
            image.raycastTarget = true;
            image.color = ColorFromHex("#22313D", 0.96f);

            Button buttonComponent = button.gameObject.AddComponent<Button>();
            CreateText(button, label, dense ? DenseToggleFontSize : 10, FontStyles.Bold, TextAlignmentOptions.Center);
            return buttonComponent;
        }

        private void CreateDetailRows(
            RectTransform parent,
            ArcUiInspectorRow[] rows)
        {
            RectTransform detailsRoot = CreateRect("Details", parent);
            LayoutElement detailsLayout = detailsRoot.gameObject.AddComponent<LayoutElement>();
            detailsLayout.preferredHeight = EstimateRowsHeight(rows, true);

            VerticalLayoutGroup detailsGroup = detailsRoot.gameObject.AddComponent<VerticalLayoutGroup>();
            detailsGroup.padding = new RectOffset((int)DenseDetailIndent, 0, 0, 0);
            detailsGroup.spacing = DenseRowSpacing;
            detailsGroup.childControlWidth = true;
            detailsGroup.childControlHeight = true;
            detailsGroup.childForceExpandWidth = true;
            detailsGroup.childForceExpandHeight = false;

            if (rows.Length == 0)
            {
                CreateInfoRow(detailsRoot, "Dettagli", "--", true);
                return;
            }

            for (int i = 0; i < rows.Length; i++)
                CreateInspectorRow(detailsRoot, rows[i], true);
        }

        private static float EstimateRowsHeight(
            ArcUiInspectorRow[] rows,
            bool dense)
        {
            if (rows == null || rows.Length == 0)
                return ResolveTextRowHeight(dense);

            float height = 0f;
            for (int i = 0; i < rows.Length; i++)
                height += EstimateRowHeight(rows[i], dense) + ResolveRowSpacing(dense);

            return Mathf.Max(ResolveTextRowHeight(dense), height);
        }

        private static float EstimateRowHeight(
            ArcUiInspectorRow row,
            bool dense)
        {
            return row.Kind switch
            {
                ArcUiInspectorRowKind.Section => dense ? DenseSectionHeight : CompactSectionHeight,
                ArcUiInspectorRowKind.Bar => dense ? DenseBarRowHeight : CompactBarRowHeight,
                ArcUiInspectorRowKind.IconMetrics => dense ? DenseMetricRowHeight : CompactMetricRowHeight,
                ArcUiInspectorRowKind.Expandable => ResolveExpandableHeight(dense)
                    + (row.Details.Length > 0 ? EstimateRowsHeight(row.Details, true) : 0f),
                ArcUiInspectorRowKind.Timeline => dense ? DenseTimelineHeight : CompactTimelineHeight,
                _ => ResolveTextRowHeight(dense)
            };
        }

        private static void CreateTimelineRow(
            RectTransform parent,
            ArcUiInspectorRow row,
            bool dense)
        {
            RectTransform root = CreateRect("ArcTimelineRow_" + SanitizeName(row.RowKey), parent);
            LayoutElement rootLayout = root.gameObject.AddComponent<LayoutElement>();
            rootLayout.preferredHeight = dense ? DenseTimelineHeight : CompactTimelineHeight;

            Image image = root.gameObject.AddComponent<Image>();
            image.raycastTarget = false;
            image.color = ColorFromHex("#0F1821", 0.72f);

            HorizontalLayoutGroup layout = root.gameObject.AddComponent<HorizontalLayoutGroup>();
            layout.padding = dense
                ? new RectOffset(1, 2, 0, 0)
                : new RectOffset(3, 4, 1, 1);
            layout.childControlWidth = true;
            layout.childControlHeight = true;
            layout.childForceExpandWidth = true;
            layout.childForceExpandHeight = true;

            RectTransform labelRoot = CreateRect("Label", root);
            LayoutElement labelLayout = labelRoot.gameObject.AddComponent<LayoutElement>();
            labelLayout.preferredWidth = dense ? 48f : 62f;
            TextMeshProUGUI label = CreateText(labelRoot, row.Label, dense ? DenseFontSize : 9, FontStyles.Normal, TextAlignmentOptions.Left);
            label.color = ColorForSeverity(row.Severity, 1f);

            RectTransform valueRoot = CreateRect("Value", root);
            LayoutElement valueLayout = valueRoot.gameObject.AddComponent<LayoutElement>();
            valueLayout.flexibleWidth = 1f;
            CreateText(valueRoot, row.Value, dense ? DenseFontSize : 9, FontStyles.Normal, TextAlignmentOptions.Left);
        }

        private void ToggleRowExpanded(string rowKey)
        {
            if (string.IsNullOrWhiteSpace(rowKey))
                return;

            _expandedRows.TryGetValue(rowKey, out bool expanded);
            _expandedRows[rowKey] = !expanded;
            BuildRows(_currentViewModel, false);
        }

        private bool IsRowExpanded(ArcUiInspectorRow row)
        {
            if (string.IsNullOrWhiteSpace(row.RowKey))
                return false;

            return _expandedRows.TryGetValue(row.RowKey, out bool expanded) && expanded;
        }

        private static Color ColorForSeverity(
            ArcUiInspectorSeverity severity,
            float alpha)
        {
            string hex = severity switch
            {
                ArcUiInspectorSeverity.Good => "#64B486",
                ArcUiInspectorSeverity.Warning => "#D6A33A",
                ArcUiInspectorSeverity.Danger => "#D85D5D",
                ArcUiInspectorSeverity.Info => "#6EA8C8",
                ArcUiInspectorSeverity.Muted => "#80909C",
                _ => "#DDE6EE"
            };

            return ColorFromHex(hex, alpha);
        }

        private static void CreateInfoRow(
            RectTransform parent,
            string label,
            string value,
            bool dense)
        {
            RectTransform row = CreateRect("ArcInfoRow_" + SanitizeName(label), parent);
            LayoutElement rowLayout = row.gameObject.AddComponent<LayoutElement>();
            rowLayout.preferredHeight = ResolveTextRowHeight(dense);

            HorizontalLayoutGroup layout = row.gameObject.AddComponent<HorizontalLayoutGroup>();
            layout.childControlWidth = true;
            layout.childControlHeight = true;
            layout.childForceExpandWidth = true;
            layout.childForceExpandHeight = true;

            RectTransform labelRoot = CreateRect("Label", row);
            LayoutElement labelLayout = labelRoot.gameObject.AddComponent<LayoutElement>();
            labelLayout.flexibleWidth = 1f;
            CreateText(labelRoot, label, dense ? DenseFontSize : 10, FontStyles.Normal, TextAlignmentOptions.Left);

            RectTransform valueRoot = CreateRect("Value", row);
            LayoutElement valueLayout = valueRoot.gameObject.AddComponent<LayoutElement>();
            valueLayout.preferredWidth = dense ? 98f : 132f;
            CreateText(valueRoot, value, dense ? DenseFontSize : 10, FontStyles.Bold, TextAlignmentOptions.Right);
        }

        private static float ResolveRowSpacing(bool dense)
        {
            return dense ? DenseRowSpacing : CompactRowSpacing;
        }

        private static float ResolveTextRowHeight(bool dense)
        {
            return dense ? DenseTextRowHeight : CompactTextRowHeight;
        }

        private static float ResolveExpandableHeight(bool dense)
        {
            return dense ? DenseExpandableHeight : CompactExpandableHeight;
        }

        private static TextMeshProUGUI CreateText(
            RectTransform parent,
            string text,
            int fontSize,
            FontStyles fontStyle,
            TextAlignmentOptions alignment)
        {
            RectTransform textRoot = CreateRect("Text", parent);
            StretchFull(textRoot, new Vector2(4f, 0f), new Vector2(-4f, 0f));

            TextMeshProUGUI label = textRoot.gameObject.AddComponent<TextMeshProUGUI>();
            label.raycastTarget = false;
            label.text = text;
            label.fontSize = fontSize;
            label.fontStyle = fontStyle;
            label.alignment = alignment;
            label.enableWordWrapping = false;
            label.overflowMode = TextOverflowModes.Ellipsis;
            label.extraPadding = true;
            label.color = ColorFromHex("#DDE6EE", 1f);
            ArcGraphUiFontProvider.ApplyOfficialFont(label);

            LayoutElement layout = textRoot.gameObject.AddComponent<LayoutElement>();
            layout.preferredHeight = Mathf.Max(fontSize + 5f, 14f);
            layout.flexibleHeight = 0f;
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
