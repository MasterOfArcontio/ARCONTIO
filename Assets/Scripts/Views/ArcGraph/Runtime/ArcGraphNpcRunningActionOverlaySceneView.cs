using System;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Arcontio.View.ArcGraph
{
    // =============================================================================
    // ArcGraphNpcRunningActionOverlayPreset
    // =============================================================================
    /// <summary>
    /// <para>
    /// Preset visuale del pannello UGUI che mostra la running action corrente sopra
    /// ogni NPC ArcGraph.
    /// </para>
    ///
    /// <para><b>Principio architetturale: configurazione visuale senza runtime simulativo</b></para>
    /// <para>
    /// Il preset contiene solo dimensioni, offset e colori del pannello. Non
    /// contiene riferimenti a NPC, job, command, World o store mutabili. La view
    /// consuma esclusivamente render item ArcGraph gia' derivati.
    /// </para>
    ///
    /// <para><b>Struttura interna:</b></para>
    /// <list type="bullet">
    ///   <item><b>Width/Height</b>: dimensione minima del pannello in pixel UI.</item>
    ///   <item><b>WorldOffsetY</b>: quota world sopra l'attore.</item>
    ///   <item><b>ScreenOffsetY</b>: rialzo finale in pixel UI.</item>
    ///   <item><b>Colori</b>: tint pannello, testo, track e fill barra.</item>
    /// </list>
    /// </summary>
    [Serializable]
    public struct ArcGraphNpcRunningActionOverlayPreset
    {
        public float Width;
        public float Height;
        public float WorldOffsetY;
        public float ScreenOffsetY;
        public float PanelAlpha;
        public Color PanelColor;
        public Color TextColor;
        public Color BarBackgroundColor;
        public Color BarFillColor;

        // =============================================================================
        // Default
        // =============================================================================
        /// <summary>
        /// <para>
        /// Crea il preset coerente con il nameplate compatto ArcGraph.
        /// </para>
        /// </summary>
        public static ArcGraphNpcRunningActionOverlayPreset Default()
        {
            return new ArcGraphNpcRunningActionOverlayPreset
            {
                Width = 53f,
                Height = 18f,
                WorldOffsetY = 1.45f,
                ScreenOffsetY = 30f,
                PanelAlpha = 1f,
                PanelColor = ColorFromHex("#101922", 0f),
                TextColor = ColorFromHex("#DDE6EE", 1f),
                BarBackgroundColor = ColorFromHex("#25323D", 0.92f),
                BarFillColor = ColorFromHex("#8FB7CF", 1f)
            };
        }

        // =============================================================================
        // Normalize
        // =============================================================================
        /// <summary>
        /// <para>
        /// Restituisce una copia difensiva con valori minimi stabili.
        /// </para>
        /// </summary>
        public ArcGraphNpcRunningActionOverlayPreset Normalize()
        {
            ArcGraphNpcRunningActionOverlayPreset normalized = this;
            normalized.Width = Width > 20f ? Width : 53f;
            normalized.Height = Height > 8f ? Height : 18f;
            normalized.WorldOffsetY = Mathf.Abs(WorldOffsetY) > 0.001f ? WorldOffsetY : 1.45f;
            normalized.ScreenOffsetY = Mathf.Abs(ScreenOffsetY) > 0.001f ? ScreenOffsetY : 30f;
            normalized.PanelAlpha = Mathf.Clamp01(PanelAlpha);
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
    // ArcGraphNpcRunningActionOverlaySceneView
    // =============================================================================
    /// <summary>
    /// <para>
    /// Scene view UGUI che disegna sopra ogni NPC ArcGraph una label compatta della
    /// running action e una barra del tempo residuo.
    /// </para>
    ///
    /// <para><b>Principio architetturale: overlay UI da render queue ArcGraph</b></para>
    /// <para>
    /// La view non legge il <c>World</c>, non interroga il Job Layer e non accede
    /// al <c>RunningActionStore</c>. Consuma solo gli <c>ArcGraphActorRenderItem</c>
    /// gia' prodotti dalla pipeline <c>WorldAdapter -> Snapshot -> RenderQueue</c>.
    /// Il pannello vive nell'overlay UGUI di ArcGraph, come il nameplate di
    /// selezione, e resta quindi separato dal renderer sprite NPC.
    /// </para>
    ///
    /// <para><b>Struttura interna:</b></para>
    /// <list type="bullet">
    ///   <item><b>_panelPool</b>: pannelli UGUI riusati per actor id.</item>
    ///   <item><b>Update</b>: sincronizza visibilita', contenuto e posizione.</item>
    ///   <item><b>ApplyPanel</b>: applica label, barra e dimensione dinamica.</item>
    ///   <item><b>ApplyScreenPosition</b>: converte world anchor in posizione UI.</item>
    /// </list>
    /// </summary>
    public sealed class ArcGraphNpcRunningActionOverlaySceneView : MonoBehaviour
    {
        private const float CompactTopRowHeight = 13f;
        private const float CompactVerticalPadding = 3f;
        private const float CompactHungerBarGap = 0f;
        private const float CompactHungerBarHeight = 2f;
        private const float CompactOuterHorizontalPadding = 4f;
        private const float CompactTitleHorizontalPadding = 8f;
        private const float CompactTitleFontSize = 8f;

        [SerializeField] private bool overlayEnabled = true;
        [SerializeField] private Camera sceneCamera;
        [SerializeField] private ArcGraphNpcRunningActionOverlayPreset preset =
            ArcGraphNpcRunningActionOverlayPreset.Default();

        private readonly Dictionary<int, PanelHandle> _panelPool = new();
        private ArcGraphUiRuntimeRoot _uiRoot;
        private ArcGraphRenderQueue _renderQueue;
        private RectTransform _overlayRoot;

        // =============================================================================
        // PanelHandle
        // =============================================================================
        /// <summary>
        /// <para>
        /// Handle pooled di un singolo pannello running action.
        /// </para>
        ///
        /// <para><b>Pooling per identita' actor</b></para>
        /// <para>
        /// Ogni pannello resta associato all'actor id e viene aggiornato finche'
        /// l'attore espone una running action attiva. Quando l'azione sparisce, il
        /// pannello viene spento senza distruggerlo.
        /// </para>
        ///
        /// <para><b>Struttura interna:</b></para>
        /// <list type="bullet">
        ///   <item><b>Root</b>: rect principale con pivot basso centrale.</item>
        ///   <item><b>Label</b>: testo azione gia' normalizzato nello snapshot.</item>
        ///   <item><b>BarFill</b>: fill ancorato a sinistra su Remaining01.</item>
        ///   <item><b>Touched</b>: marker frame per spegnere pannelli non aggiornati.</item>
        /// </list>
        /// </summary>
        private sealed class PanelHandle
        {
            public int ActorId;
            public RectTransform Root;
            public CanvasGroup CanvasGroup;
            public Image PanelImage;
            public TextMeshProUGUI Label;
            public RectTransform BarRoot;
            public Image BarBackground;
            public Image BarFill;
            public bool Touched;
        }

        // =============================================================================
        // SetUiRoot
        // =============================================================================
        /// <summary>
        /// <para>
        /// Assegna la root UI ArcGraph da cui recuperare l'overlay canvas.
        /// </para>
        /// </summary>
        public void SetUiRoot(ArcGraphUiRuntimeRoot uiRoot)
        {
            _uiRoot = uiRoot;
            _overlayRoot = null;
        }

        // =============================================================================
        // SetRenderQueue
        // =============================================================================
        /// <summary>
        /// <para>
        /// Assegna la render queue actor/object usata come sorgente read-only.
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
        /// Assegna la camera scena per convertire coordinate world in coordinate UI.
        /// </para>
        /// </summary>
        public void SetSceneCamera(Camera camera)
        {
            sceneCamera = camera;
        }

        // =============================================================================
        // SetOverlayEnabled
        // =============================================================================
        /// <summary>
        /// <para>
        /// Abilita o disabilita il disegno dei pannelli running action.
        /// </para>
        /// </summary>
        public void SetOverlayEnabled(bool enabled)
        {
            overlayEnabled = enabled;

            if (!enabled)
                HideAllPanels();
        }

        // =============================================================================
        // Update
        // =============================================================================
        /// <summary>
        /// <para>
        /// Aggiorna i pannelli running action leggendo solo la render queue ArcGraph.
        /// </para>
        /// </summary>
        private void Update()
        {
            if (!overlayEnabled || _renderQueue == null || !TryResolveOverlayRoot(out RectTransform root))
            {
                HideAllPanels();
                return;
            }

            foreach (var pair in _panelPool)
            {
                pair.Value.Touched = false;
            }

            IReadOnlyList<ArcGraphActorRenderItem> actors = _renderQueue.ActorItems;
            for (int i = 0; i < actors.Count; i++)
            {
                ArcGraphActorRenderItem actor = actors[i];
                if (actor.ActorId <= 0 || !actor.IsVisible || !actor.RunningActionOverlay.IsActive)
                    continue;

                PanelHandle handle = GetOrCreatePanel(actor.ActorId, root);
                ApplyPanel(handle, actor);
                ApplyScreenPosition(handle, actor);
                handle.Touched = true;
            }

            HideUntouchedPanels();
        }

        private bool TryResolveOverlayRoot(out RectTransform resolvedOverlayRoot)
        {
            resolvedOverlayRoot = _overlayRoot;
            if (resolvedOverlayRoot != null)
                return true;

            if (_uiRoot == null)
                return false;

            if (!_uiRoot.TryGetOverlayRoot(out resolvedOverlayRoot))
                return false;

            _overlayRoot = resolvedOverlayRoot;
            return true;
        }

        private PanelHandle GetOrCreatePanel(int actorId, RectTransform parent)
        {
            if (_panelPool.TryGetValue(actorId, out PanelHandle existing) && existing != null)
                return existing;

            PanelHandle handle = CreatePanel(actorId, parent);
            _panelPool[actorId] = handle;
            return handle;
        }

        private PanelHandle CreatePanel(int actorId, RectTransform parent)
        {
            RectTransform root = CreateRect("ArcGraphNpcRunningActionOverlay_" + actorId, parent);
            root.SetAsLastSibling();
            root.anchorMin = new Vector2(0.5f, 0.5f);
            root.anchorMax = new Vector2(0.5f, 0.5f);
            root.pivot = new Vector2(0.5f, 0f);

            Image panelImage = root.gameObject.AddComponent<Image>();
            panelImage.raycastTarget = false;

            CanvasGroup canvasGroup = root.gameObject.AddComponent<CanvasGroup>();
            canvasGroup.interactable = false;
            canvasGroup.blocksRaycasts = false;

            VerticalLayoutGroup layout = root.gameObject.AddComponent<VerticalLayoutGroup>();
            layout.padding = new RectOffset(2, 2, 1, 2);
            layout.spacing = CompactHungerBarGap;
            layout.childControlWidth = true;
            layout.childControlHeight = true;
            layout.childForceExpandWidth = true;
            layout.childForceExpandHeight = false;

            RectTransform labelRoot = CreateRect("ActionLabel", root);
            LayoutElement labelLayout = labelRoot.gameObject.AddComponent<LayoutElement>();
            labelLayout.minHeight = CompactTopRowHeight;
            labelLayout.preferredHeight = CompactTopRowHeight;

            TextMeshProUGUI label = CreateText(
                labelRoot,
                "azione",
                CompactTitleFontSize,
                FontStyles.Normal,
                TextAlignmentOptions.Center);

            RectTransform barRoot = CreateRect("RemainingBar", root);
            LayoutElement barLayout = barRoot.gameObject.AddComponent<LayoutElement>();
            barLayout.minHeight = CompactHungerBarHeight;
            barLayout.preferredHeight = CompactHungerBarHeight;

            Image barBackground = barRoot.gameObject.AddComponent<Image>();
            barBackground.raycastTarget = false;

            RectTransform fillRoot = CreateRect("Fill", barRoot);
            fillRoot.anchorMin = Vector2.zero;
            fillRoot.anchorMax = Vector2.one;
            fillRoot.offsetMin = Vector2.zero;
            fillRoot.offsetMax = Vector2.zero;

            Image barFill = fillRoot.gameObject.AddComponent<Image>();
            barFill.raycastTarget = false;

            return new PanelHandle
            {
                ActorId = actorId,
                Root = root,
                CanvasGroup = canvasGroup,
                PanelImage = panelImage,
                Label = label,
                BarRoot = barRoot,
                BarBackground = barBackground,
                BarFill = barFill
            };
        }

        private void ApplyPanel(
            PanelHandle handle,
            ArcGraphActorRenderItem actor)
        {
            if (handle == null || handle.Root == null)
                return;

            ArcGraphNpcRunningActionOverlayPreset safePreset = preset.Normalize();
            ArcGraphActorRunningActionOverlaySnapshot overlay = actor.RunningActionOverlay;
            string label = string.IsNullOrWhiteSpace(overlay.Label) ? "azione" : overlay.Label.Trim().ToLowerInvariant();

            handle.Label.text = label;
            handle.Label.color = safePreset.TextColor;
            handle.Label.ForceMeshUpdate(true, true);

            float labelWidth = Mathf.Ceil(handle.Label.GetPreferredValues(handle.Label.text).x);
            float contentWidth =
                CompactOuterHorizontalPadding +
                labelWidth +
                CompactTitleHorizontalPadding;
            float width = Mathf.Max(safePreset.Width, contentWidth);
            float height = Mathf.Max(
                safePreset.Height,
                CompactTopRowHeight + CompactVerticalPadding + CompactHungerBarHeight);

            handle.Root.sizeDelta = new Vector2(width, height);
            handle.CanvasGroup.alpha = safePreset.PanelAlpha;
            handle.PanelImage.color = safePreset.PanelColor;
            handle.BarBackground.color = safePreset.BarBackgroundColor;
            handle.BarFill.color = safePreset.BarFillColor;

            RectTransform fillTransform = (RectTransform)handle.BarFill.transform;
            fillTransform.anchorMin = Vector2.zero;
            fillTransform.anchorMax = new Vector2(Mathf.Clamp01(overlay.Remaining01), 1f);
            fillTransform.offsetMin = Vector2.zero;
            fillTransform.offsetMax = Vector2.zero;

            LayoutRebuilder.ForceRebuildLayoutImmediate(handle.Root);
            handle.Root.gameObject.SetActive(true);
        }

        private void ApplyScreenPosition(
            PanelHandle handle,
            ArcGraphActorRenderItem actor)
        {
            if (handle == null || handle.Root == null || _overlayRoot == null)
                return;

            Camera camera = ResolveSceneCamera();
            if (camera == null)
                return;

            ArcGraphNpcRunningActionOverlayPreset safePreset = preset.Normalize();
            Vector3 anchorWorldPosition = new Vector3(
                actor.DiscreteCell.X + 0.5f,
                actor.VisualY + safePreset.WorldOffsetY,
                actor.VisualZ);

            Vector2 screenPoint = RectTransformUtility.WorldToScreenPoint(camera, anchorWorldPosition);
            RectTransformUtility.ScreenPointToLocalPointInRectangle(
                _overlayRoot,
                screenPoint,
                null,
                out Vector2 localPoint);

            localPoint.y += safePreset.ScreenOffsetY;
            handle.Root.anchoredPosition = ClampPanelInsideMapViewport(handle, localPoint);
        }

        private Vector2 ClampPanelInsideMapViewport(
            PanelHandle handle,
            Vector2 localPoint)
        {
            if (_uiRoot == null ||
                _overlayRoot == null ||
                handle == null ||
                handle.Root == null ||
                !_uiRoot.TryResolveMapViewportScreenRect(out Rect viewportScreenRect))
            {
                return localPoint;
            }

            RectTransformUtility.ScreenPointToLocalPointInRectangle(
                _overlayRoot,
                new Vector2(viewportScreenRect.xMin, viewportScreenRect.yMin),
                null,
                out Vector2 viewportMin);
            RectTransformUtility.ScreenPointToLocalPointInRectangle(
                _overlayRoot,
                new Vector2(viewportScreenRect.xMax, viewportScreenRect.yMax),
                null,
                out Vector2 viewportMax);

            float left = Mathf.Min(viewportMin.x, viewportMax.x);
            float right = Mathf.Max(viewportMin.x, viewportMax.x);
            float bottom = Mathf.Min(viewportMin.y, viewportMax.y);
            float top = Mathf.Max(viewportMin.y, viewportMax.y);
            float width = handle.Root.rect.width > 1f ? handle.Root.rect.width : preset.Normalize().Width;
            float height = handle.Root.rect.height > 1f ? handle.Root.rect.height : preset.Normalize().Height;
            float minX = left + width * 0.5f;
            float maxX = right - width * 0.5f;
            float minY = bottom;
            float maxY = top - height;

            if (maxX < minX)
                localPoint.x = (left + right) * 0.5f;
            else
                localPoint.x = Mathf.Clamp(localPoint.x, minX, maxX);

            if (maxY < minY)
                localPoint.y = (bottom + top) * 0.5f;
            else
                localPoint.y = Mathf.Clamp(localPoint.y, minY, maxY);

            return localPoint;
        }

        private Camera ResolveSceneCamera()
        {
            if (sceneCamera != null)
                return sceneCamera;

            sceneCamera = Camera.main;
            return sceneCamera;
        }

        private void HideUntouchedPanels()
        {
            foreach (var pair in _panelPool)
            {
                PanelHandle handle = pair.Value;
                if (handle == null || handle.Touched)
                    continue;

                if (handle.Root != null)
                    handle.Root.gameObject.SetActive(false);
            }
        }

        private void HideAllPanels()
        {
            foreach (var pair in _panelPool)
            {
                PanelHandle handle = pair.Value;
                if (handle != null && handle.Root != null)
                    handle.Root.gameObject.SetActive(false);
            }
        }

        private static TextMeshProUGUI CreateText(
            RectTransform parent,
            string text,
            float fontSize,
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
            label.overflowMode = TextOverflowModes.Overflow;
            ArcGraphUiFontProvider.ApplyOfficialFont(label);
            return label;
        }

        private static RectTransform CreateRect(string name, Transform parent)
        {
            var go = new GameObject(name, typeof(RectTransform));
            go.transform.SetParent(parent, false);
            return (RectTransform)go.transform;
        }
    }
}
