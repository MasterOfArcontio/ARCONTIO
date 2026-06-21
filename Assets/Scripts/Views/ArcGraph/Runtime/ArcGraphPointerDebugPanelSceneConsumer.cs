using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Arcontio.View.ArcGraph
{
    // =============================================================================
    // ArcGraphPointerDebugPanelSceneConsumer
    // =============================================================================
    /// <summary>
    /// <para>
    /// Pannello UGUI provvisorio per visualizzare in runtime le coordinate del
    /// puntatore ArcGraph e la cella griglia risolta dalla pipeline interattiva.
    /// </para>
    ///
    /// <para><b>Principio architetturale: diagnostica view-side, non picking duplicato</b></para>
    /// <para>
    /// Questo componente non legge il mouse, non interroga la camera, non scansiona
    /// il <c>World</c> e non decide cosa sia selezionabile. Riceve soltanto
    /// l'<c>ArcGraphInteractionFrame</c> gia' prodotto dal boundary autorizzato e lo
    /// rende visibile in una piccola UI sempre accesa. Serve quindi a capire dove si
    /// rompe la catena senza introdurre una seconda logica di coordinate.
    /// </para>
    ///
    /// <para><b>Struttura interna:</b></para>
    /// <list type="bullet">
    ///   <item><b>SetUiRoot</b>: riceve la shell UGUI ArcGraph e si aggancia all'OverlayRoot.</item>
    ///   <item><b>ConsumeInteractionFrame</b>: aggiorna le due righe diagnostiche dal frame corrente.</item>
    ///   <item><b>BuildPanelIfPossible</b>: crea il pannello runtime senza prefab o scene salvate.</item>
    ///   <item><b>Format*</b>: trasforma valori grezzi in testo leggibile per il gate manuale.</item>
    /// </list>
    /// </summary>
    public sealed class ArcGraphPointerDebugPanelSceneConsumer : MonoBehaviour, IArcGraphInteractionFrameConsumer
    {
        [SerializeField] private bool panelEnabled = true;
        [SerializeField] private Vector2 panelSize = new Vector2(410f, 64f);
        [SerializeField] private Vector2 panelOffset = new Vector2(12f, -56f);

        private ArcGraphUiRuntimeRoot _uiRoot;
        private RectTransform _panel;
        private TextMeshProUGUI _viewportText;
        private TextMeshProUGUI _cellText;

        // =============================================================================
        // SetUiRoot
        // =============================================================================
        /// <summary>
        /// <para>
        /// Assegna la root UGUI ArcGraph usata per creare il pannello diagnostico.
        /// </para>
        /// </summary>
        public void SetUiRoot(ArcGraphUiRuntimeRoot uiRoot)
        {
            _uiRoot = uiRoot;
            BuildPanelIfPossible();
            ApplyPanelVisibility();
        }

        // =============================================================================
        // SetPanelEnabled
        // =============================================================================
        /// <summary>
        /// <para>
        /// Abilita o disabilita il pannello diagnostico senza distruggerlo.
        /// </para>
        /// </summary>
        public void SetPanelEnabled(bool enabled)
        {
            panelEnabled = enabled;
            ApplyPanelVisibility();
        }

        // =============================================================================
        // ConsumeInteractionFrame
        // =============================================================================
        /// <summary>
        /// <para>
        /// Aggiorna le righe del pannello usando il frame interattivo ricevuto.
        /// </para>
        ///
        /// <para><b>Uso diagnostico</b></para>
        /// <para>
        /// La prima riga mostra le coordinate fisiche del puntatore dentro la
        /// viewport camera ArcGraph. La seconda riga mostra riga e colonna della
        /// griglia se il boundary ha prodotto una cella valida; se non esiste una
        /// cella valida, la riga resta esplicitamente vuota con motivo diagnostico.
        /// </para>
        /// </summary>
        public void ConsumeInteractionFrame(
            ArcGraphInteractionFrame interactionFrame,
            ArcGraphInteractionSceneAdapterDiagnostics diagnostics)
        {
            BuildPanelIfPossible();
            ApplyPanelVisibility();

            if (_viewportText == null || _cellText == null)
                return;

            _viewportText.text = FormatViewportLine(interactionFrame);
            _cellText.text = FormatCellLine(interactionFrame, diagnostics);
        }

        private void BuildPanelIfPossible()
        {
            if (_panel != null)
                return;

            if (_uiRoot == null || !_uiRoot.TryGetOverlayRoot(out RectTransform overlayRoot))
                return;

            _panel = CreateRect("ArcGraphPointerDebugPanel", overlayRoot);
            _panel.anchorMin = new Vector2(0f, 1f);
            _panel.anchorMax = new Vector2(0f, 1f);
            _panel.pivot = new Vector2(0f, 1f);
            _panel.sizeDelta = panelSize;
            _panel.anchoredPosition = panelOffset;
            _panel.localScale = Vector3.one;

            Image background = _panel.gameObject.AddComponent<Image>();
            background.raycastTarget = false;
            background.color = ColorFromHex("#0B1117", 0.78f);

            VerticalLayoutGroup layout = _panel.gameObject.AddComponent<VerticalLayoutGroup>();
            layout.padding = new RectOffset(10, 10, 8, 8);
            layout.spacing = 2f;
            layout.childControlWidth = true;
            layout.childControlHeight = false;
            layout.childForceExpandWidth = true;
            layout.childForceExpandHeight = false;

            _viewportText = CreateLine("ViewportLine", _panel, "Viewport px: --");
            _cellText = CreateLine("CellLine", _panel, "Griglia: colonna -- | riga --");
        }

        private void ApplyPanelVisibility()
        {
            if (_panel != null)
                _panel.gameObject.SetActive(panelEnabled);
        }

        private static string FormatViewportLine(ArcGraphInteractionFrame interactionFrame)
        {
            if (!interactionFrame.Input.HasPointerScreenPosition)
                return "Viewport px: puntatore non disponibile";

            return "Viewport px: x=" +
                   Mathf.RoundToInt(interactionFrame.Input.PointerScreenX) +
                   " | y=" +
                   Mathf.RoundToInt(interactionFrame.Input.PointerScreenY);
        }

        private static string FormatCellLine(
            ArcGraphInteractionFrame interactionFrame,
            ArcGraphInteractionSceneAdapterDiagnostics diagnostics)
        {
            if (!interactionFrame.HasValidCell)
            {
                string reason = string.IsNullOrWhiteSpace(diagnostics.Reason)
                    ? interactionFrame.Reason
                    : diagnostics.Reason;
                return "Griglia: colonna -- | riga -- | cella non valida (" + reason + ")";
            }

            return "Griglia: colonna=" +
                   interactionFrame.Cell.X +
                   " | riga=" +
                   interactionFrame.Cell.Y +
                   " | z=" +
                   interactionFrame.Cell.Z;
        }

        private static TextMeshProUGUI CreateLine(
            string name,
            RectTransform parent,
            string text)
        {
            RectTransform line = CreateRect(name, parent);
            LayoutElement layout = line.gameObject.AddComponent<LayoutElement>();
            layout.preferredHeight = 22f;

            TextMeshProUGUI label = line.gameObject.AddComponent<TextMeshProUGUI>();
            label.raycastTarget = false;
            label.text = text;
            label.fontSize = 13f;
            label.fontStyle = FontStyles.Bold;
            label.alignment = TextAlignmentOptions.Left;
            label.enableWordWrapping = false;
            label.overflowMode = TextOverflowModes.Ellipsis;
            label.color = ColorFromHex("#DDE6EE", 1f);
            return label;
        }

        private static RectTransform CreateRect(string name, Transform parent)
        {
            var go = new GameObject(name, typeof(RectTransform));
            go.transform.SetParent(parent, false);
            return (RectTransform)go.transform;
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
