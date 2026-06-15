using Arcontio.View.MapGrid;
using UnityEngine;

namespace Arcontio.View.ArcGraph
{
    // =============================================================================
    // ArcGraphPlacementCellHighlightDiagnostics
    // =============================================================================
    /// <summary>
    /// <para>
    /// Diagnostica sintetica dell'overlay cella per inserimento oggetti ArcGraph.
    /// </para>
    ///
    /// <para><b>Principio architetturale: overlay osservabile e non operativo</b></para>
    /// <para>
    /// La diagnostica dichiara se il consumer ha ricevuto un frame, se il DevTool
    /// legacy era in modalita' inserimento e se la cella visuale e' stata mostrata.
    /// Non contiene riferimenti a GameObject, comandi o stato mutabile del World.
    /// </para>
    ///
    /// <para><b>Struttura interna:</b></para>
    /// <list type="bullet">
    ///   <item><b>DidReceiveFrame</b>: il consumer ha ricevuto un frame interattivo.</item>
    ///   <item><b>HighlightEnabled</b>: gate locale del consumer.</item>
    ///   <item><b>HasDevToolsOverlay</b>: presenza del pannello DevTools legacy.</item>
    ///   <item><b>PlacementPreviewActive</b>: tool F3 compatibile con inserimento oggetti.</item>
    ///   <item><b>HasValidCell</b>: cella ArcGraph valida sotto il puntatore.</item>
    ///   <item><b>DidShowHighlight</b>: sprite overlay acceso nel frame.</item>
    /// </list>
    /// </summary>
    public readonly struct ArcGraphPlacementCellHighlightDiagnostics
    {
        public readonly bool DidReceiveFrame;
        public readonly bool HighlightEnabled;
        public readonly bool HasDevToolsOverlay;
        public readonly bool PlacementPreviewActive;
        public readonly bool HasValidCell;
        public readonly bool DidShowHighlight;
        public readonly ArcGraphCellCoord Cell;
        public readonly string Reason;

        public ArcGraphPlacementCellHighlightDiagnostics(
            bool didReceiveFrame,
            bool highlightEnabled,
            bool hasDevToolsOverlay,
            bool placementPreviewActive,
            bool hasValidCell,
            bool didShowHighlight,
            ArcGraphCellCoord cell,
            string reason)
        {
            DidReceiveFrame = didReceiveFrame;
            HighlightEnabled = highlightEnabled;
            HasDevToolsOverlay = hasDevToolsOverlay;
            PlacementPreviewActive = placementPreviewActive;
            HasValidCell = hasValidCell;
            DidShowHighlight = didShowHighlight;
            Cell = cell;
            Reason = string.IsNullOrWhiteSpace(reason) ? "None" : reason;
        }
    }

    // =============================================================================
    // ArcGraphPlacementCellHighlightSceneConsumer
    // =============================================================================
    /// <summary>
    /// <para>
    /// Consumer scena che evidenzia la cella sotto il mouse quando i DevTools sono
    /// in modalita' inserimento oggetto.
    /// </para>
    ///
    /// <para><b>Principio architetturale: ArcGraph mostra, DevTools comandano</b></para>
    /// <para>
    /// Il componente riceve un <c>ArcGraphInteractionFrame</c> gia' calcolato dal
    /// boundary ArcGraph e legge solo un flag read-only del
    /// <c>MapGridRuntimeDevToolsOverlay</c>. Non interpreta click, non piazza muri,
    /// non modifica oggetti, non accoda comandi e non interroga il World. Serve solo
    /// a rendere leggibile dove finira' l'inserimento quando il DevTool legacy e'
    /// attivo.
    /// </para>
    ///
    /// <para><b>Struttura interna:</b></para>
    /// <list type="bullet">
    ///   <item><b>ConsumeInteractionFrame</b>: accende/spegne l'highlight in base al frame.</item>
    ///   <item><b>SetDevToolsOverlay</b>: riceve il riferimento legacy dall'installer.</item>
    ///   <item><b>EnsureHighlightRenderer</b>: crea una sola sprite runtime riusabile.</item>
    ///   <item><b>HideHighlight</b>: disattiva il visual senza distruggerlo.</item>
    /// </list>
    /// </summary>
    public sealed class ArcGraphPlacementCellHighlightSceneConsumer : MonoBehaviour, IArcGraphInteractionFrameConsumer
    {
        [SerializeField] private bool highlightEnabled = true;
        [SerializeField] private MapGridRuntimeDevToolsOverlay devToolsOverlay;
        [SerializeField] private Color validPlacementColor = new Color(1f, 0.18f, 0.12f, 0.38f);
        [SerializeField] private float tileWorldSize = 1f;
        [SerializeField] private float zOffset = -0.035f;
        [SerializeField] private int sortingOrder = 30;
        [SerializeField] private Vector3 originOffset = Vector3.zero;
        [SerializeField] private string highlightObjectName = "ArcGraphPlacementCellHighlight";
        [SerializeField] private bool logDiagnostics;

        private GameObject _highlightObject;
        private SpriteRenderer _highlightRenderer;
        private Texture2D _highlightTexture;
        private Sprite _highlightSprite;
        private ArcGraphPlacementCellHighlightDiagnostics _lastDiagnostics =
            new ArcGraphPlacementCellHighlightDiagnostics(
                false,
                false,
                false,
                false,
                false,
                false,
                new ArcGraphCellCoord(0, 0, 0),
                "NotInitialized");

        public ArcGraphPlacementCellHighlightDiagnostics LastDiagnostics => _lastDiagnostics;
        public bool HighlightEnabled => highlightEnabled;

        // =============================================================================
        // ConsumeInteractionFrame
        // =============================================================================
        /// <summary>
        /// <para>
        /// Consuma il frame interattivo e aggiorna la cella evidenziata.
        /// </para>
        ///
        /// <para><b>Filtro a costo basso</b></para>
        /// <para>
        /// Il metodo esce presto quando il gate e' spento, manca il DevTool, il
        /// tool corrente non inserisce oggetti o il puntatore non produce una cella
        /// valida. Solo nel caso positivo aggiorna posizione e colore dello sprite.
        /// </para>
        /// </summary>
        public void ConsumeInteractionFrame(
            ArcGraphInteractionFrame interactionFrame,
            ArcGraphInteractionSceneAdapterDiagnostics diagnostics)
        {
            if (!highlightEnabled)
            {
                HideAndStore(interactionFrame, false, "HighlightDisabled");
                return;
            }

            if (devToolsOverlay == null)
            {
                HideAndStore(interactionFrame, false, "DevToolsOverlayMissing");
                return;
            }

            if (!devToolsOverlay.IsObjectPlacementPreviewActive)
            {
                HideAndStore(interactionFrame, false, "PlacementPreviewInactive");
                return;
            }

            if (!interactionFrame.HasValidCell
                || interactionFrame.IsPointerOverUi
                || devToolsOverlay.IsPointerOverDevToolsWindow)
            {
                HideAndStore(interactionFrame, false, "CellUnavailable");
                return;
            }

            EnsureHighlightRenderer();
            Vector3 worldPosition = ResolveWorldPosition(interactionFrame.Cell);

            _highlightObject.transform.position = worldPosition;
            _highlightObject.transform.localScale = new Vector3(tileWorldSize, tileWorldSize, 1f);
            _highlightRenderer.color = validPlacementColor;
            _highlightRenderer.sortingOrder = sortingOrder;
            _highlightObject.SetActive(true);

            StoreDiagnostics(interactionFrame, true, "PlacementCellHighlighted");
        }

        // =============================================================================
        // SetDevToolsOverlay
        // =============================================================================
        /// <summary>
        /// <para>
        /// Assegna esplicitamente il DevTools legacy da osservare in sola lettura.
        /// </para>
        /// </summary>
        public void SetDevToolsOverlay(MapGridRuntimeDevToolsOverlay overlay)
        {
            devToolsOverlay = overlay;
        }

        // =============================================================================
        // SetHighlightEnabled
        // =============================================================================
        /// <summary>
        /// <para>
        /// Abilita o disabilita il gate locale dell'overlay.
        /// </para>
        /// </summary>
        public void SetHighlightEnabled(bool enabled)
        {
            highlightEnabled = enabled;
            if (!enabled)
                HideHighlight();
        }

        // =============================================================================
        // EnableHighlightFromInspector
        // =============================================================================
        /// <summary>
        /// <para>
        /// Context menu per abilitare l'highlight durante i test manuali.
        /// </para>
        /// </summary>
        [ContextMenu("ArcGraph/Enable Placement Cell Highlight")]
        public void EnableHighlightFromInspector()
        {
            SetHighlightEnabled(true);
        }

        // =============================================================================
        // DisableHighlightFromInspector
        // =============================================================================
        /// <summary>
        /// <para>
        /// Context menu per disabilitare l'highlight durante i test manuali.
        /// </para>
        /// </summary>
        [ContextMenu("ArcGraph/Disable Placement Cell Highlight")]
        public void DisableHighlightFromInspector()
        {
            SetHighlightEnabled(false);
        }

        private void OnDestroy()
        {
            if (_highlightSprite != null)
                Destroy(_highlightSprite);

            if (_highlightTexture != null)
                Destroy(_highlightTexture);

            _highlightSprite = null;
            _highlightTexture = null;
        }

        private void EnsureHighlightRenderer()
        {
            if (_highlightRenderer != null)
                return;

            _highlightTexture = new Texture2D(1, 1)
            {
                name = "ArcGraphPlacementCellHighlightTexture",
                filterMode = FilterMode.Point,
                wrapMode = TextureWrapMode.Clamp
            };
            _highlightTexture.SetPixel(0, 0, Color.white);
            _highlightTexture.Apply();

            _highlightSprite = Sprite.Create(
                _highlightTexture,
                new Rect(0f, 0f, 1f, 1f),
                new Vector2(0.5f, 0.5f),
                1f);
            _highlightSprite.name = "ArcGraphPlacementCellHighlightSprite";

            _highlightObject = new GameObject(highlightObjectName);
            _highlightObject.transform.SetParent(transform, false);
            _highlightRenderer = _highlightObject.AddComponent<SpriteRenderer>();
            _highlightRenderer.sprite = _highlightSprite;
            _highlightRenderer.sortingOrder = sortingOrder;
            _highlightRenderer.color = validPlacementColor;
            _highlightObject.SetActive(false);
        }

        private Vector3 ResolveWorldPosition(ArcGraphCellCoord cell)
        {
            return originOffset + new Vector3(
                (cell.X + 0.5f) * tileWorldSize,
                (cell.Y + 0.5f) * tileWorldSize,
                zOffset);
        }

        private void HideAndStore(
            ArcGraphInteractionFrame interactionFrame,
            bool didShowHighlight,
            string reason)
        {
            HideHighlight();
            StoreDiagnostics(interactionFrame, didShowHighlight, reason);
        }

        private void HideHighlight()
        {
            if (_highlightObject != null)
                _highlightObject.SetActive(false);
        }

        private void StoreDiagnostics(
            ArcGraphInteractionFrame interactionFrame,
            bool didShowHighlight,
            string reason)
        {
            bool placementActive = devToolsOverlay != null && devToolsOverlay.IsObjectPlacementPreviewActive;
            _lastDiagnostics = new ArcGraphPlacementCellHighlightDiagnostics(
                true,
                highlightEnabled,
                devToolsOverlay != null,
                placementActive,
                interactionFrame.HasValidCell,
                didShowHighlight,
                interactionFrame.Cell,
                reason);

            if (!logDiagnostics)
                return;

            Debug.Log(
                "[ArcGraphPlacementCellHighlightSceneConsumer] " +
                _lastDiagnostics.Reason +
                ", enabled=" + _lastDiagnostics.HighlightEnabled +
                ", devTools=" + _lastDiagnostics.HasDevToolsOverlay +
                ", placement=" + _lastDiagnostics.PlacementPreviewActive +
                ", cell=" + _lastDiagnostics.Cell +
                ", shown=" + _lastDiagnostics.DidShowHighlight);
        }
    }
}
