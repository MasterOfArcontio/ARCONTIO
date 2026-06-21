using System;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Arcontio.View.ArcGraph
{
    // =============================================================================
    // ArcGraphSelectionActionMenuPreset
    // =============================================================================
    /// <summary>
    /// <para>
    /// Preset visuale minimale per il piccolo menu agganciato al target selezionato.
    /// </para>
    ///
    /// <para><b>Principio architetturale: stile configurabile senza logica runtime</b></para>
    /// <para>
    /// Il preset contiene solo valori di presentazione UGUI: dimensioni, offset e
    /// colori. Non contiene operation key, non contiene riferimenti a target
    /// selezionati e non conosce il mondo simulativo. Questo permette di sostituire
    /// piu' avanti il look con prefab o ScriptableObject senza cambiare il
    /// contratto del menu.
    /// </para>
    ///
    /// <para><b>Struttura interna:</b></para>
    /// <list type="bullet">
    ///   <item><b>Width/Height</b>: dimensione compatta del menu.</item>
    ///   <item><b>WorldOffsetY</b>: distanza verticale sopra actor o oggetto.</item>
    ///   <item><b>PanelAlpha</b>: trasparenza generale del blocco.</item>
    ///   <item><b>Colori</b>: palette acciaio/grigio-blu coerente con ArcGraph.</item>
    /// </list>
    /// </summary>
    [Serializable]
    public struct ArcGraphSelectionActionMenuPreset
    {
        public float Width;
        public float Height;
        public float WorldOffsetY;
        public float PanelAlpha;
        public Color PanelColor;
        public Color TextColor;
        public Color MutedTextColor;
        public Color ButtonColor;
        public Color ButtonHoverColor;
        public Color ButtonPressedColor;
        public Color DangerButtonColor;
        public Color HungerBarBackgroundColor;
        public Color HungerBarFillColor;

        // =============================================================================
        // Default
        // =============================================================================
        /// <summary>
        /// <para>
        /// Crea il preset runtime iniziale del menu contestuale.
        /// </para>
        /// </summary>
        public static ArcGraphSelectionActionMenuPreset Default()
        {
            return new ArcGraphSelectionActionMenuPreset
            {
                Width = 178f,
                Height = 70f,
                WorldOffsetY = 1.45f,
                PanelAlpha = 0.86f,
                PanelColor = ColorFromHex("#101922", 0.84f),
                TextColor = ColorFromHex("#DDE6EE", 1f),
                MutedTextColor = ColorFromHex("#9AA7B2", 1f),
                ButtonColor = ColorFromHex("#22313D", 0.92f),
                ButtonHoverColor = ColorFromHex("#33485A", 0.98f),
                ButtonPressedColor = ColorFromHex("#49647B", 1f),
                DangerButtonColor = ColorFromHex("#5A2630", 0.95f),
                HungerBarBackgroundColor = ColorFromHex("#25323D", 0.95f),
                HungerBarFillColor = ColorFromHex("#8FB7CF", 1f)
            };
        }

        // =============================================================================
        // Normalize
        // =============================================================================
        /// <summary>
        /// <para>
        /// Restituisce una copia difensiva del preset con valori numerici sicuri.
        /// </para>
        /// </summary>
        public ArcGraphSelectionActionMenuPreset Normalize()
        {
            ArcGraphSelectionActionMenuPreset normalized = this;
            normalized.Width = Width > 20f ? Width : 178f;
            normalized.Height = Height > 20f ? Height : 70f;
            normalized.WorldOffsetY = Mathf.Abs(WorldOffsetY) > 0.001f ? WorldOffsetY : 1.45f;
            normalized.PanelAlpha = Mathf.Clamp01(PanelAlpha <= 0f ? 0.86f : PanelAlpha);
            return normalized;
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

    // =============================================================================
    // ArcGraphSelectionActionMenuViewModel
    // =============================================================================
    /// <summary>
    /// <para>
    /// ViewModel leggero del menu contestuale del target selezionato.
    /// </para>
    ///
    /// <para><b>Principio architetturale: UI legge dati preparati, non runtime mutabile</b></para>
    /// <para>
    /// Il ViewModel contiene testo, stato barra fame e operation key diagnostiche
    /// derivate dal target selezionato e dalla render queue ArcGraph. Non contiene
    /// riferimenti a NPC, oggetti, componenti Unity world-side o strutture mutabili.
    /// </para>
    ///
    /// <para><b>Struttura interna:</b></para>
    /// <list type="bullet">
    ///   <item><b>Target</b>: identita' UI selezionata.</item>
    ///   <item><b>Title/Subtitle</b>: testo minimale visualizzato.</item>
    ///   <item><b>HasHungerBar</b>: abilita la barretta fame per NPC.</item>
    ///   <item><b>HungerLevel01</b>: valore normalizzato quando disponibile.</item>
    ///   <item><b>Edit/DeleteOperationKey</b>: chiavi diagnostiche, non comandi eseguiti.</item>
    /// </list>
    /// </summary>
    public readonly struct ArcGraphSelectionActionMenuViewModel
    {
        public readonly bool HasTarget;
        public readonly ArcUiSelectionTarget Target;
        public readonly string Title;
        public readonly string Subtitle;
        public readonly bool HasHungerBar;
        public readonly bool HasHungerValue;
        public readonly float HungerLevel01;
        public readonly string EditOperationKey;
        public readonly string DeleteOperationKey;

        // =============================================================================
        // ArcGraphSelectionActionMenuViewModel
        // =============================================================================
        /// <summary>
        /// <para>
        /// Costruisce il ViewModel del menu selezione.
        /// </para>
        /// </summary>
        public ArcGraphSelectionActionMenuViewModel(
            bool hasTarget,
            ArcUiSelectionTarget target,
            string title,
            string subtitle,
            bool hasHungerBar,
            bool hasHungerValue,
            float hungerLevel01,
            string editOperationKey,
            string deleteOperationKey)
        {
            HasTarget = hasTarget;
            Target = target;
            Title = string.IsNullOrWhiteSpace(title) ? "Selezione" : title.Trim();
            Subtitle = string.IsNullOrWhiteSpace(subtitle) ? string.Empty : subtitle.Trim();
            HasHungerBar = hasHungerBar;
            HasHungerValue = hasHungerValue;
            HungerLevel01 = Mathf.Clamp01(hungerLevel01);
            EditOperationKey = string.IsNullOrWhiteSpace(editOperationKey)
                ? string.Empty
                : editOperationKey.Trim();
            DeleteOperationKey = string.IsNullOrWhiteSpace(deleteOperationKey)
                ? string.Empty
                : deleteOperationKey.Trim();
        }

        // =============================================================================
        // Hidden
        // =============================================================================
        /// <summary>
        /// <para>
        /// Crea un ViewModel vuoto per menu nascosto.
        /// </para>
        /// </summary>
        public static ArcGraphSelectionActionMenuViewModel Hidden()
        {
            return new ArcGraphSelectionActionMenuViewModel(
                false,
                ArcUiSelectionTarget.None("ArcGraphSelectionActionMenuSceneView"),
                string.Empty,
                string.Empty,
                false,
                false,
                0f,
                string.Empty,
                string.Empty);
        }
    }

    // =============================================================================
    // ArcGraphSelectionActionMenuSceneView
    // =============================================================================
    /// <summary>
    /// <para>
    /// View UGUI runtime che mostra un piccolo menu sopra il target selezionato in ArcGraph.
    /// </para>
    ///
    /// <para><b>Principio architetturale: menu di selezione come consumer passivo</b></para>
    /// <para>
    /// Il componente non ascolta l'hover e non fa picking autonomo. Legge soltanto
    /// la selezione prodotta da <see cref="ArcGraphUiSelectionSceneConsumer"/> e
    /// usa la <see cref="ArcGraphRenderQueue"/> gia' preparata per posizionarsi
    /// sopra l'NPC, l'oggetto o il muro selezionato. I pulsanti Modifica/Elimina
    /// per ora registrano una richiesta diagnostica: non modificano il mondo, non
    /// cancellano oggetti e non chiamano controller simulativi.
    /// </para>
    ///
    /// <para><b>Struttura interna:</b></para>
    /// <list type="bullet">
    ///   <item><b>EnsureBuilt</b>: crea il piccolo prefab runtime dentro OverlayRoot.</item>
    ///   <item><b>Update</b>: ricostruisce ViewModel e posizione ogni frame.</item>
    ///   <item><b>TryResolveTargetAnchor</b>: ritrova il target nella render queue corrente.</item>
    ///   <item><b>OnEditClicked/OnDeleteClicked</b>: registrano operation key diagnostiche.</item>
    /// </list>
    /// </summary>
    public sealed class ArcGraphSelectionActionMenuSceneView : MonoBehaviour
    {
        [SerializeField] private bool menuEnabled = true;
        [SerializeField] private Camera sceneCamera;
        [SerializeField] private bool logRequests;
        [SerializeField] private ArcGraphSelectionActionMenuPreset preset = ArcGraphSelectionActionMenuPreset.Default();

        private ArcGraphUiRuntimeRoot _uiRoot;
        private ArcGraphUiSelectionSceneConsumer _selectionConsumer;
        private ArcGraphRenderQueue _renderQueue;
        private RectTransform _overlayRoot;
        private RectTransform _menuRoot;
        private CanvasGroup _canvasGroup;
        private Image _panelImage;
        private TextMeshProUGUI _titleText;
        private TextMeshProUGUI _subtitleText;
        private RectTransform _hungerRoot;
        private Image _hungerFill;
        private TextMeshProUGUI _hungerText;
        private Button _editButton;
        private Button _deleteButton;
        private ArcGraphSelectionActionMenuViewModel _currentViewModel =
            ArcGraphSelectionActionMenuViewModel.Hidden();
        private string _lastRequestedOperationKey = string.Empty;
        private string _lastVisibilityReason = "NotInitialized";
        private ArcUiSelectionTarget _lastRequestedTarget =
            ArcUiSelectionTarget.None("ArcGraphSelectionActionMenuSceneView");

        public ArcGraphSelectionActionMenuViewModel CurrentViewModel => _currentViewModel;
        public string LastRequestedOperationKey => _lastRequestedOperationKey;
        public string LastVisibilityReason => _lastVisibilityReason;
        public ArcUiSelectionTarget LastRequestedTarget => _lastRequestedTarget;

        // =============================================================================
        // Update
        // =============================================================================
        /// <summary>
        /// <para>
        /// Aggiorna visibilita', contenuto e posizione del menu selezione.
        /// </para>
        /// </summary>
        private void Update()
        {
            // Il menu e' intenzionalmente legato alla selezione, non all'hover. Se
            // non esiste una selezione valida, il piccolo pannello resta nascosto.
            if (!menuEnabled || _selectionConsumer == null)
            {
                HideMenu(!menuEnabled ? "MenuDisabled" : "SelectionConsumerMissing");
                return;
            }

            ArcUiSelectionTarget target = _selectionConsumer.CurrentSelection;
            if (!target.IsValid || !EnsureBuilt())
            {
                HideMenu(!target.IsValid ? "SelectionTargetMissing" : "OverlayRootMissing");
                return;
            }

            if (!TryResolveTargetAnchor(target, out Vector3 anchorWorldPosition, out ArcGraphSelectionActionMenuViewModel viewModel))
            {
                HideMenu("TargetAnchorMissing");
                return;
            }

            ApplyViewModel(viewModel);
            ApplyScreenPosition(anchorWorldPosition);
            ShowMenu("Visible");
        }

        // =============================================================================
        // SetUiRoot
        // =============================================================================
        /// <summary>
        /// <para>
        /// Assegna la shell UI ArcGraph da cui recuperare <c>OverlayRoot</c>.
        /// </para>
        /// </summary>
        public void SetUiRoot(ArcGraphUiRuntimeRoot uiRoot)
        {
            _uiRoot = uiRoot;
            _overlayRoot = null;
        }

        // =============================================================================
        // SetSelectionConsumer
        // =============================================================================
        /// <summary>
        /// <para>
        /// Assegna il consumer che possiede la selezione UI corrente.
        /// </para>
        /// </summary>
        public void SetSelectionConsumer(ArcGraphUiSelectionSceneConsumer selectionConsumer)
        {
            _selectionConsumer = selectionConsumer;
        }

        // =============================================================================
        // SetRenderQueue
        // =============================================================================
        /// <summary>
        /// <para>
        /// Assegna la render queue ArcGraph usata come snapshot visuale corrente.
        /// </para>
        /// </summary>
        public void SetRenderQueue(ArcGraphRenderQueue renderQueue)
        {
            _renderQueue = renderQueue;
        }

        // =============================================================================
        // SetSceneCamera
        // =============================================================================
        /// <summary>
        /// <para>
        /// Assegna la camera scena usata per convertire world position in screen position.
        /// </para>
        /// </summary>
        public void SetSceneCamera(Camera camera)
        {
            sceneCamera = camera;
        }

        // =============================================================================
        // SetMenuEnabled
        // =============================================================================
        /// <summary>
        /// <para>
        /// Abilita o disabilita il menu contestuale della selezione.
        /// </para>
        /// </summary>
        public void SetMenuEnabled(bool enabled)
        {
            menuEnabled = enabled;

            if (!enabled)
                HideMenu("MenuDisabled");
        }

        // =============================================================================
        // BuildHiddenMenuForRuntimeDiagnostics
        // =============================================================================
        /// <summary>
        /// <para>
        /// Costruisce subito il GameObject UGUI del menu e lo lascia nascosto.
        /// </para>
        ///
        /// <para><b>Principio architetturale: diagnostica visibile senza mutazione</b></para>
        /// <para>
        /// Il menu resta guidato dalla selezione e non interroga il mondo. Questo
        /// metodo serve solo a rendere presente in Hierarchy il nodo
        /// <c>ArcSelectionActionMenu</c> anche prima di una selezione valida, cosi'
        /// l'operatore puo' verificare cablaggio, OverlayRoot e diagnostica runtime
        /// senza aspettare che il picking produca un target.
        /// </para>
        /// </summary>
        public bool BuildHiddenMenuForRuntimeDiagnostics()
        {
            bool built = EnsureBuilt();

            if (built)
                HideMenu("SelectionTargetMissing");

            return built;
        }

        // =============================================================================
        // ApplyPreset
        // =============================================================================
        /// <summary>
        /// <para>
        /// Applica un preset visuale al menu gia' costruito o futuro.
        /// </para>
        /// </summary>
        public void ApplyPreset(ArcGraphSelectionActionMenuPreset nextPreset)
        {
            preset = nextPreset.Normalize();

            if (_menuRoot != null)
                ApplyPresetToBuiltUi();
        }

        // =============================================================================
        // LogLastRequestFromInspector
        // =============================================================================
        /// <summary>
        /// <para>
        /// Stampa in Console l'ultima richiesta diagnostica generata dai pulsanti.
        /// </para>
        /// </summary>
        [ContextMenu("ArcGraph/Log Selection Action Menu Request")]
        public void LogLastRequestFromInspector()
        {
            Debug.Log(
                "[ArcGraphSelectionActionMenuSceneView] lastOperation=" +
                _lastRequestedOperationKey +
                ", targetKind=" +
                _lastRequestedTarget.Kind +
                ", targetId=" +
                _lastRequestedTarget.Id);
        }

        // =============================================================================
        // LogVisibilityDiagnosticsFromInspector
        // =============================================================================
        /// <summary>
        /// <para>
        /// Stampa in Console lo stato essenziale usato per capire perche' il menu e'
        /// visibile o nascosto.
        /// </para>
        /// </summary>
        [ContextMenu("ArcGraph/Log Selection Action Menu Visibility")]
        public void LogVisibilityDiagnosticsFromInspector()
        {
            ArcUiSelectionTarget target = _selectionConsumer != null
                ? _selectionConsumer.CurrentSelection
                : ArcUiSelectionTarget.None("ArcGraphSelectionActionMenuSceneView");
            ArcGraphUiSelectionSceneConsumerDiagnostics selectionDiagnostics =
                _selectionConsumer != null
                    ? _selectionConsumer.LastDiagnostics
                    : default;

            Debug.Log(
                "[ArcGraphSelectionActionMenuSceneView] visibleReason=" +
                _lastVisibilityReason +
                ", enabled=" +
                menuEnabled +
                ", hasSelectionConsumer=" +
                (_selectionConsumer != null) +
                ", hasUiRoot=" +
                (_uiRoot != null) +
                ", hasOverlayRoot=" +
                (_overlayRoot != null) +
                ", hasRenderQueue=" +
                (_renderQueue != null) +
                ", targetKind=" +
                target.Kind +
                ", targetId=" +
                target.Id +
                ", targetCell=" +
                target.Cell +
                ", selectionReason=" +
                selectionDiagnostics.Reason +
                ", selectionClick=" +
                selectionDiagnostics.WasPrimaryClick +
                ", selectionPointerOverUi=" +
                selectionDiagnostics.WasPointerOverUi +
                ", selectionFrameTarget=" +
                selectionDiagnostics.FrameTargetKind +
                ", selectionFrameActorId=" +
                selectionDiagnostics.FrameActorId +
                ", selectionFrameObjectId=" +
                selectionDiagnostics.FrameObjectId +
                ", selectionFrameCell=" +
                selectionDiagnostics.HasFrameCell +
                ", selectionCell=" +
                selectionDiagnostics.Cell +
                ", selectionDidSelect=" +
                selectionDiagnostics.DidSelectTarget);
        }

        private bool EnsureBuilt()
        {
            if (_menuRoot != null)
                return true;

            if (!TryResolveOverlayRoot(out RectTransform resolvedOverlayRoot))
                return false;

            _overlayRoot = resolvedOverlayRoot;
            _menuRoot = CreateRect("ArcSelectionActionMenu", _overlayRoot);
            _menuRoot.SetAsLastSibling();
            _menuRoot.anchorMin = new Vector2(0.5f, 0.5f);
            _menuRoot.anchorMax = new Vector2(0.5f, 0.5f);
            _menuRoot.pivot = new Vector2(0.5f, 0f);

            _panelImage = _menuRoot.gameObject.AddComponent<Image>();
            _panelImage.raycastTarget = true;

            _canvasGroup = _menuRoot.gameObject.AddComponent<CanvasGroup>();
            _canvasGroup.interactable = true;
            _canvasGroup.blocksRaycasts = true;

            VerticalLayoutGroup layout = _menuRoot.gameObject.AddComponent<VerticalLayoutGroup>();
            layout.padding = new RectOffset(8, 8, 6, 6);
            layout.spacing = 4f;
            layout.childControlWidth = true;
            layout.childControlHeight = false;
            layout.childForceExpandWidth = true;
            layout.childForceExpandHeight = false;

            BuildHeader();
            BuildHungerBar();
            BuildButtonRow();
            ApplyPresetToBuiltUi();
            HideMenu("BuiltHidden");
            return true;
        }

        private bool TryResolveOverlayRoot(out RectTransform resolvedOverlayRoot)
        {
            resolvedOverlayRoot = _overlayRoot;
            if (resolvedOverlayRoot != null)
                return true;

            if (_uiRoot == null)
                return false;

            return _uiRoot.TryGetOverlayRoot(out resolvedOverlayRoot);
        }

        private void BuildHeader()
        {
            RectTransform headerRoot = CreateRect("Header", _menuRoot);
            LayoutElement headerLayout = headerRoot.gameObject.AddComponent<LayoutElement>();
            headerLayout.preferredHeight = 26f;

            VerticalLayoutGroup headerGroup = headerRoot.gameObject.AddComponent<VerticalLayoutGroup>();
            headerGroup.spacing = 0f;
            headerGroup.childControlWidth = true;
            headerGroup.childControlHeight = false;
            headerGroup.childForceExpandWidth = true;
            headerGroup.childForceExpandHeight = false;

            _titleText = CreateText(headerRoot, "Selezione", 12, FontStyles.Bold, TextAlignmentOptions.Left);
            _subtitleText = CreateText(headerRoot, string.Empty, 10, FontStyles.Normal, TextAlignmentOptions.Left);
        }

        private void BuildHungerBar()
        {
            _hungerRoot = CreateRect("HungerBar", _menuRoot);
            LayoutElement hungerLayout = _hungerRoot.gameObject.AddComponent<LayoutElement>();
            hungerLayout.preferredHeight = 12f;

            Image background = _hungerRoot.gameObject.AddComponent<Image>();
            background.raycastTarget = false;

            RectTransform fillRoot = CreateRect("Fill", _hungerRoot);
            fillRoot.anchorMin = Vector2.zero;
            fillRoot.anchorMax = new Vector2(0f, 1f);
            fillRoot.pivot = new Vector2(0f, 0.5f);
            fillRoot.offsetMin = Vector2.zero;
            fillRoot.offsetMax = Vector2.zero;
            _hungerFill = fillRoot.gameObject.AddComponent<Image>();
            _hungerFill.raycastTarget = false;

            _hungerText = CreateText(_hungerRoot, "Fame n/d", 9, FontStyles.Bold, TextAlignmentOptions.Center);
            _hungerText.raycastTarget = false;
        }

        private void BuildButtonRow()
        {
            RectTransform row = CreateRect("Actions", _menuRoot);
            LayoutElement rowLayout = row.gameObject.AddComponent<LayoutElement>();
            rowLayout.preferredHeight = 24f;

            HorizontalLayoutGroup group = row.gameObject.AddComponent<HorizontalLayoutGroup>();
            group.spacing = 4f;
            group.childControlWidth = true;
            group.childControlHeight = true;
            group.childForceExpandWidth = true;
            group.childForceExpandHeight = true;

            _editButton = CreateButton(row, "Edit", "M");
            _deleteButton = CreateButton(row, "Delete", "X");
            _editButton.onClick.AddListener(OnEditClicked);
            _deleteButton.onClick.AddListener(OnDeleteClicked);
        }

        private void ApplyPresetToBuiltUi()
        {
            ArcGraphSelectionActionMenuPreset safePreset = preset.Normalize();
            preset = safePreset;

            if (_menuRoot != null)
                _menuRoot.sizeDelta = new Vector2(safePreset.Width, safePreset.Height);

            if (_canvasGroup != null)
                _canvasGroup.alpha = safePreset.PanelAlpha;

            if (_panelImage != null)
                _panelImage.color = safePreset.PanelColor;

            ApplyTextColor(_titleText, safePreset.TextColor);
            ApplyTextColor(_subtitleText, safePreset.MutedTextColor);
            ApplyTextColor(_hungerText, safePreset.MutedTextColor);

            Image hungerBackground = _hungerRoot != null ? _hungerRoot.GetComponent<Image>() : null;
            if (hungerBackground != null)
                hungerBackground.color = safePreset.HungerBarBackgroundColor;

            if (_hungerFill != null)
                _hungerFill.color = safePreset.HungerBarFillColor;

            ApplyButtonColors(_editButton, safePreset.ButtonColor, safePreset.ButtonHoverColor, safePreset.ButtonPressedColor);
            ApplyButtonColors(_deleteButton, safePreset.DangerButtonColor, safePreset.ButtonHoverColor, safePreset.ButtonPressedColor);
        }

        private bool TryResolveTargetAnchor(
            ArcUiSelectionTarget target,
            out Vector3 anchorWorldPosition,
            out ArcGraphSelectionActionMenuViewModel viewModel)
        {
            anchorWorldPosition = default;
            viewModel = ArcGraphSelectionActionMenuViewModel.Hidden();

            // La render queue resta la sorgente preferita per seguire target mobili.
            // Se pero' la queue non e' ancora disponibile, o il target non viene
            // ritrovato per un frame, usiamo la cella copiata nel SelectionTarget:
            // cosi' il menu compare comunque e rende diagnosticabile il problema
            // senza introdurre accessi diretti al World.
            if (_renderQueue != null &&
                target.Kind == ArcUiSelectionTargetKind.Npc &&
                TryParseTargetId(target, out int actorId) &&
                TryFindActor(actorId, out ArcGraphActorRenderItem actor))
            {
                anchorWorldPosition = new Vector3(actor.VisualX, actor.VisualY + preset.Normalize().WorldOffsetY, actor.VisualZ);
                viewModel = CreateNpcViewModel(target);
                return true;
            }

            if (_renderQueue != null &&
                (target.Kind == ArcUiSelectionTargetKind.Object || target.Kind == ArcUiSelectionTargetKind.Wall) &&
                TryParseTargetId(target, out int objectId) &&
                TryFindObject(objectId, out ArcGraphObjectRenderItem obj))
            {
                float width = Mathf.Max(1, obj.FootprintWidth);
                float height = Mathf.Max(1, obj.FootprintHeight);
                anchorWorldPosition = new Vector3(
                    obj.Cell.X + width * 0.5f,
                    obj.Cell.Y + height + 0.18f,
                    obj.Cell.Z);
                viewModel = CreateObjectViewModel(target, obj);
                return true;
            }

            if (target.Kind == ArcUiSelectionTargetKind.Npc ||
                target.Kind == ArcUiSelectionTargetKind.Object ||
                target.Kind == ArcUiSelectionTargetKind.Wall)
            {
                anchorWorldPosition = new Vector3(
                    target.Cell.X + 0.5f,
                    target.Cell.Y + preset.Normalize().WorldOffsetY,
                    target.Cell.Z);
                viewModel = CreateFallbackViewModel(target);
                return true;
            }

            return false;
        }

        private ArcGraphSelectionActionMenuViewModel CreateNpcViewModel(ArcUiSelectionTarget target)
        {
            return new ArcGraphSelectionActionMenuViewModel(
                true,
                target,
                string.IsNullOrWhiteSpace(target.DisplayName) ? "NPC " + target.Id : target.DisplayName,
                "Fame n/d",
                true,
                false,
                0f,
                "edit_selected_npc",
                "delete_selected_npc");
        }

        private ArcGraphSelectionActionMenuViewModel CreateObjectViewModel(
            ArcUiSelectionTarget target,
            ArcGraphObjectRenderItem obj)
        {
            string title = string.IsNullOrWhiteSpace(target.DisplayName)
                ? obj.DefId
                : target.DisplayName;
            string fallbackTitle = target.Kind == ArcUiSelectionTargetKind.Wall ? "Muro " + target.Id : "Oggetto " + target.Id;

            return new ArcGraphSelectionActionMenuViewModel(
                true,
                target,
                string.IsNullOrWhiteSpace(title) ? fallbackTitle : title,
                target.Kind == ArcUiSelectionTargetKind.Wall ? "Muro" : "Oggetto",
                false,
                false,
                0f,
                target.Kind == ArcUiSelectionTargetKind.Wall ? "edit_selected_wall" : "edit_selected_object",
                target.Kind == ArcUiSelectionTargetKind.Wall ? "delete_selected_wall" : "delete_selected_object");
        }

        private ArcGraphSelectionActionMenuViewModel CreateFallbackViewModel(ArcUiSelectionTarget target)
        {
            if (target.Kind == ArcUiSelectionTargetKind.Npc)
                return CreateNpcViewModel(target);

            string editOperationKey = target.Kind == ArcUiSelectionTargetKind.Wall
                ? "edit_selected_wall"
                : "edit_selected_object";
            string deleteOperationKey = target.Kind == ArcUiSelectionTargetKind.Wall
                ? "delete_selected_wall"
                : "delete_selected_object";

            return new ArcGraphSelectionActionMenuViewModel(
                true,
                target,
                string.IsNullOrWhiteSpace(target.DisplayName) ? "Selezione " + target.Id : target.DisplayName,
                target.Kind == ArcUiSelectionTargetKind.Wall ? "Muro" : "Oggetto",
                false,
                false,
                0f,
                editOperationKey,
                deleteOperationKey);
        }

        private void ApplyViewModel(ArcGraphSelectionActionMenuViewModel viewModel)
        {
            _currentViewModel = viewModel;

            if (_titleText != null)
                _titleText.text = viewModel.Title;

            if (_subtitleText != null)
                _subtitleText.text = viewModel.Subtitle;

            if (_hungerRoot != null)
                _hungerRoot.gameObject.SetActive(viewModel.HasHungerBar);

            if (_hungerFill != null)
            {
                float fill01 = viewModel.HasHungerValue ? viewModel.HungerLevel01 : 0f;
                RectTransform fillTransform = (RectTransform)_hungerFill.transform;
                fillTransform.anchorMax = new Vector2(fill01, 1f);
            }

            if (_hungerText != null)
            {
                _hungerText.text = viewModel.HasHungerValue
                    ? "Fame " + Mathf.RoundToInt(viewModel.HungerLevel01 * 100f) + "%"
                    : "Fame n/d";
            }
        }

        private void ApplyScreenPosition(Vector3 anchorWorldPosition)
        {
            if (_menuRoot == null || _overlayRoot == null)
                return;

            Camera camera = ResolveSceneCamera();
            if (camera == null)
                return;

            Vector2 screenPoint = RectTransformUtility.WorldToScreenPoint(camera, anchorWorldPosition);
            RectTransformUtility.ScreenPointToLocalPointInRectangle(
                _overlayRoot,
                screenPoint,
                null,
                out Vector2 localPoint);
            _menuRoot.anchoredPosition = localPoint;
        }

        private void ShowMenu(string reason)
        {
            _lastVisibilityReason = string.IsNullOrWhiteSpace(reason) ? "Visible" : reason;

            if (_menuRoot != null && !_menuRoot.gameObject.activeSelf)
                _menuRoot.gameObject.SetActive(true);
        }

        private void HideMenu(string reason)
        {
            _lastVisibilityReason = string.IsNullOrWhiteSpace(reason) ? "Hidden" : reason;
            _currentViewModel = ArcGraphSelectionActionMenuViewModel.Hidden();

            if (_menuRoot != null && _menuRoot.gameObject.activeSelf)
                _menuRoot.gameObject.SetActive(false);
        }

        private void OnEditClicked()
        {
            StoreDiagnosticRequest(_currentViewModel.EditOperationKey, _currentViewModel.Target);
        }

        private void OnDeleteClicked()
        {
            StoreDiagnosticRequest(_currentViewModel.DeleteOperationKey, _currentViewModel.Target);
        }

        private void StoreDiagnosticRequest(
            string operationKey,
            ArcUiSelectionTarget target)
        {
            if (string.IsNullOrWhiteSpace(operationKey) || !target.IsValid)
                return;

            _lastRequestedOperationKey = operationKey;
            _lastRequestedTarget = target;

            if (logRequests)
                LogLastRequestFromInspector();
        }

        private bool TryFindActor(
            int actorId,
            out ArcGraphActorRenderItem selected)
        {
            selected = default;

            if (_renderQueue == null || _renderQueue.ActorItems == null)
                return false;

            for (int i = 0; i < _renderQueue.ActorItems.Count; i++)
            {
                ArcGraphActorRenderItem item = _renderQueue.ActorItems[i];
                if (item.ActorId == actorId && item.IsVisible)
                {
                    selected = item;
                    return true;
                }
            }

            return false;
        }

        private bool TryFindObject(
            int objectId,
            out ArcGraphObjectRenderItem selected)
        {
            selected = default;

            if (_renderQueue == null || _renderQueue.ObjectItems == null)
                return false;

            for (int i = 0; i < _renderQueue.ObjectItems.Count; i++)
            {
                ArcGraphObjectRenderItem item = _renderQueue.ObjectItems[i];
                if (item.ObjectId == objectId && item.IsVisible && !item.IsHeld)
                {
                    selected = item;
                    return true;
                }
            }

            return false;
        }

        private Camera ResolveSceneCamera()
        {
            if (sceneCamera != null)
                return sceneCamera;

            return Camera.main;
        }

        private static bool TryParseTargetId(
            ArcUiSelectionTarget target,
            out int id)
        {
            return int.TryParse(target.Id, out id);
        }

        private static Button CreateButton(
            RectTransform parent,
            string name,
            string label)
        {
            RectTransform rect = CreateRect("ArcSelectionMenuButton_" + name, parent);
            Image image = rect.gameObject.AddComponent<Image>();
            image.raycastTarget = true;

            Button button = rect.gameObject.AddComponent<Button>();
            button.targetGraphic = image;
            button.transition = Selectable.Transition.ColorTint;

            CreateText(rect, label, 11, FontStyles.Bold, TextAlignmentOptions.Center);
            return button;
        }

        private static TextMeshProUGUI CreateText(
            RectTransform parent,
            string text,
            int fontSize,
            FontStyles fontStyle,
            TextAlignmentOptions alignment)
        {
            RectTransform textRoot = CreateRect("Text", parent);
            textRoot.anchorMin = Vector2.zero;
            textRoot.anchorMax = Vector2.one;
            textRoot.offsetMin = new Vector2(3f, 1f);
            textRoot.offsetMax = new Vector2(-3f, -1f);

            TextMeshProUGUI label = textRoot.gameObject.AddComponent<TextMeshProUGUI>();
            label.raycastTarget = false;
            label.text = text;
            label.fontSize = fontSize;
            label.fontStyle = fontStyle;
            label.alignment = alignment;
            label.textWrappingMode = TextWrappingModes.NoWrap;
            label.overflowMode = TextOverflowModes.Ellipsis;
            return label;
        }

        private static RectTransform CreateRect(string name, Transform parent)
        {
            var go = new GameObject(name, typeof(RectTransform));
            go.transform.SetParent(parent, false);
            return (RectTransform)go.transform;
        }

        private static void ApplyTextColor(
            TextMeshProUGUI label,
            Color color)
        {
            if (label != null)
                label.color = color;
        }

        private static void ApplyButtonColors(
            Button button,
            Color normalColor,
            Color hoverColor,
            Color pressedColor)
        {
            if (button == null)
                return;

            Image image = button.targetGraphic as Image;
            if (image != null)
                image.color = normalColor;

            ColorBlock colors = button.colors;
            colors.normalColor = normalColor;
            colors.highlightedColor = hoverColor;
            colors.pressedColor = pressedColor;
            colors.selectedColor = hoverColor;
            colors.disabledColor = new Color(normalColor.r, normalColor.g, normalColor.b, 0.35f);
            button.colors = colors;
        }
    }
}
