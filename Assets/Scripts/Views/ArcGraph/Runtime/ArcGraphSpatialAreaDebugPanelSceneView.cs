using System.Text;
using Arcontio.Core;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Arcontio.View.ArcGraph
{
    // =============================================================================
    // ArcGraphSpatialAreaDebugPanelSceneView
    // =============================================================================
    /// <summary>
    /// <para>
    /// Pannello diagnostico UGUI sempre visibile per il modulo WorldSpatialAreas e
    /// per i landmark di supporto open-space.
    /// </para>
    ///
    /// <para><b>Principio architetturale: ArcGraph consumer di snapshot World</b></para>
    /// <para>
    /// La view non calcola flood-fill, non ricostruisce landmark e non legge
    /// provider o registry mutabili. Chiede al runtime context il <c>World</c> e
    /// consuma un solo snapshot data-only prodotto da
    /// <see cref="World.BuildSpatialAreaDebugSnapshot"/>.
    /// </para>
    ///
    /// <para><b>Struttura interna:</b></para>
    /// <list type="bullet">
    ///   <item><b>SetRuntimeContextProvider</b>: assegna il provider ArcGraph autorizzato.</item>
    ///   <item><b>SetUiRoot</b>: aggancia il pannello all'overlay UGUI ArcGraph.</item>
    ///   <item><b>Update</b>: aggiorna testo a cadenza leggera senza mutare il World.</item>
    ///   <item><b>RefreshText</b>: formatta conteggi, config e prime righe S#.</item>
    /// </list>
    /// </summary>
    public sealed class ArcGraphSpatialAreaDebugPanelSceneView : MonoBehaviour
    {
        private const float PanelWidth = 282f;
        private const float PanelHeight = 152f;
        private const float PanelOffsetX = 8f;
        private const float PanelOffsetY = -56f;
        private const float RefreshIntervalSeconds = 0.25f;
        private const int MaxSupportRows = 7;
        private const int MaxDiagnosticRows = 3;

        [SerializeField] private bool panelVisible = true;
        [SerializeField] private ArcGraphRuntimeContextProvider runtimeContextProvider;

        private readonly StringBuilder _builder = new(512);
        private ArcGraphUiRuntimeRoot _uiRoot;
        private RectTransform _root;
        private CanvasGroup _canvasGroup;
        private TextMeshProUGUI _label;
        private float _nextRefreshTime;

        // =============================================================================
        // SetRuntimeContextProvider
        // =============================================================================
        /// <summary>
        /// <para>
        /// Assegna il provider runtime ArcGraph da cui ottenere il World corrente.
        /// </para>
        /// </summary>
        public void SetRuntimeContextProvider(ArcGraphRuntimeContextProvider provider)
        {
            runtimeContextProvider = provider;
            _nextRefreshTime = 0f;
        }

        // =============================================================================
        // SetUiRoot
        // =============================================================================
        /// <summary>
        /// <para>
        /// Assegna la root UI ArcGraph su cui creare il pannello diagnostico.
        /// </para>
        /// </summary>
        public void SetUiRoot(ArcGraphUiRuntimeRoot uiRoot)
        {
            _uiRoot = uiRoot;
            _root = null;
            _label = null;
            _canvasGroup = null;
            _nextRefreshTime = 0f;
        }

        // =============================================================================
        // SetPanelVisible
        // =============================================================================
        /// <summary>
        /// <para>
        /// Abilita o disabilita la visibilita' del pannello senza distruggerlo.
        /// </para>
        /// </summary>
        public void SetPanelVisible(bool visible)
        {
            panelVisible = visible;
            ApplyVisibility();
        }

        private void Update()
        {
            if (!panelVisible || !TryEnsurePanel())
            {
                ApplyVisibility();
                return;
            }

            ApplyVisibility();
            if (Time.unscaledTime < _nextRefreshTime)
                return;

            _nextRefreshTime = Time.unscaledTime + RefreshIntervalSeconds;
            RefreshText();
        }

        private bool TryEnsurePanel()
        {
            if (_root != null && _label != null)
                return true;

            if (_uiRoot == null || !_uiRoot.TryGetOverlayRoot(out RectTransform overlayRoot) || overlayRoot == null)
                return false;

            _root = CreateRect("ArcGraphSpatialAreaDebugPanel", overlayRoot);
            _root.anchorMin = new Vector2(0f, 1f);
            _root.anchorMax = new Vector2(0f, 1f);
            _root.pivot = new Vector2(0f, 1f);
            _root.anchoredPosition = new Vector2(PanelOffsetX, PanelOffsetY);
            _root.sizeDelta = new Vector2(PanelWidth, PanelHeight);

            _canvasGroup = _root.gameObject.AddComponent<CanvasGroup>();
            _canvasGroup.blocksRaycasts = false;
            _canvasGroup.interactable = false;

            Image background = _root.gameObject.AddComponent<Image>();
            background.raycastTarget = false;
            background.color = new Color(0.02f, 0.04f, 0.06f, 0.82f);

            _label = CreateText("ArcGraphSpatialAreaDebugText", _root);
            _label.rectTransform.anchorMin = new Vector2(0f, 0f);
            _label.rectTransform.anchorMax = new Vector2(1f, 1f);
            _label.rectTransform.offsetMin = new Vector2(8f, 6f);
            _label.rectTransform.offsetMax = new Vector2(-8f, -6f);
            _label.enableWordWrapping = false;
            _label.overflowMode = TextOverflowModes.Truncate;
            _label.fontSize = 10f;
            _label.alignment = TextAlignmentOptions.TopLeft;
            _label.color = new Color(0.88f, 0.94f, 0.98f, 1f);
            _label.raycastTarget = false;

            return true;
        }

        private void RefreshText()
        {
            World world = ResolveWorld();
            _builder.Clear();

            if (world == null)
            {
                _builder.AppendLine("AREA: NO WORLD");
                _builder.AppendLine("Support LM: n/a");
                _label.text = _builder.ToString();
                return;
            }

            WorldSpatialAreaDebugSnapshot snapshot = world.BuildSpatialAreaDebugSnapshot();
            _builder.Append("AREA: ");
            _builder.AppendLine(snapshot.HasErrors ? "ERROR" : "OK");
            _builder.Append("Open/Room/Corr/Invalid: ");
            _builder.Append(snapshot.OpenAreaCount);
            _builder.Append('/');
            _builder.Append(snapshot.ClosedRoomCount);
            _builder.Append('/');
            _builder.Append(snapshot.CorridorCount);
            _builder.Append('/');
            _builder.AppendLine(snapshot.InvalidDiagnosticCount.ToString());
            _builder.Append("Support cfg: spacing ");
            _builder.Append(snapshot.SupportLandmarkSpacingCells);
            _builder.Append(" radius ");
            _builder.Append(snapshot.SupportLandmarkCoverageRadiusCells);
            _builder.Append(" x");
            _builder.AppendLine(snapshot.SupportLandmarkCoverageMultiplier.ToString());
            _builder.Append("Support LM: ");
            _builder.AppendLine(snapshot.SupportLandmarkCount.ToString());

            AppendSupportRows(snapshot);
            AppendDiagnostics(snapshot);

            _label.text = _builder.ToString();
        }

        private void AppendSupportRows(WorldSpatialAreaDebugSnapshot snapshot)
        {
            if (snapshot.SupportLandmarkCount <= 0)
            {
                _builder.Append("reason: ");
                _builder.AppendLine(snapshot.SupportLandmarkZeroReason);
                return;
            }

            int count = snapshot.SupportLandmarks.Length < MaxSupportRows
                ? snapshot.SupportLandmarks.Length
                : MaxSupportRows;
            for (int i = 0; i < count; i++)
            {
                WorldSupportLandmarkDebugEntry entry = snapshot.SupportLandmarks[i];
                _builder.Append("S#");
                _builder.Append(entry.NodeId);
                _builder.Append(" (");
                _builder.Append(entry.CellX);
                _builder.Append(',');
                _builder.Append(entry.CellY);
                _builder.Append(") area#");
                _builder.Append(entry.AreaId);
                _builder.Append(' ');
                _builder.AppendLine(entry.AreaKind.ToString());
            }

            if (snapshot.SupportLandmarks.Length > count)
            {
                _builder.Append("+");
                _builder.Append(snapshot.SupportLandmarks.Length - count);
                _builder.AppendLine(" altri S#");
            }
        }

        private void AppendDiagnostics(WorldSpatialAreaDebugSnapshot snapshot)
        {
            if (snapshot.Diagnostics == null || snapshot.Diagnostics.Length == 0)
                return;

            int count = snapshot.Diagnostics.Length < MaxDiagnosticRows
                ? snapshot.Diagnostics.Length
                : MaxDiagnosticRows;
            for (int i = 0; i < count; i++)
            {
                _builder.Append("diag: ");
                _builder.AppendLine(snapshot.Diagnostics[i]);
            }
        }

        private World ResolveWorld()
        {
            ArcGraphRuntimeContext context = runtimeContextProvider != null
                ? runtimeContextProvider.BuildTerrainRuntimeContext()
                : null;
            return context?.World;
        }

        private void ApplyVisibility()
        {
            if (_canvasGroup == null)
                return;

            _canvasGroup.alpha = panelVisible ? 1f : 0f;
            _canvasGroup.blocksRaycasts = false;
            _canvasGroup.interactable = false;
        }

        private static RectTransform CreateRect(string name, Transform parent)
        {
            var go = new GameObject(name, typeof(RectTransform));
            RectTransform rect = go.GetComponent<RectTransform>();
            rect.SetParent(parent, false);
            return rect;
        }

        private static TextMeshProUGUI CreateText(string name, Transform parent)
        {
            var go = new GameObject(name, typeof(RectTransform), typeof(TextMeshProUGUI));
            RectTransform rect = go.GetComponent<RectTransform>();
            rect.SetParent(parent, false);
            return go.GetComponent<TextMeshProUGUI>();
        }
    }
}
