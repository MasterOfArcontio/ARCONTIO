using UnityEngine;

namespace Arcontio.View.ArcGraph
{
    // =============================================================================
    // ArcGraphPointerCellHoverDiagnostics
    // =============================================================================
    /// <summary>
    /// <para>
    /// Diagnostica sintetica dell'highlight cella sotto puntatore ArcGraph.
    /// </para>
    ///
    /// <para><b>Principio architetturale: hover osservabile, non operativo</b></para>
    /// <para>
    /// Questa struttura descrive solo l'esito visuale del consumer: se ha ricevuto
    /// un frame, se il gate locale e' acceso, se la cella era valida e se lo sprite
    /// di hover e' stato mostrato. Non contiene riferimenti a comandi, GameObject
    /// esterni, NPC, oggetti runtime o stato mutabile del mondo.
    /// </para>
    ///
    /// <para><b>Struttura interna:</b></para>
    /// <list type="bullet">
    ///   <item><b>DidReceiveFrame</b>: il consumer ha ricevuto un frame interattivo.</item>
    ///   <item><b>HoverEnabled</b>: gate locale del consumer.</item>
    ///   <item><b>WasPointerOverUi</b>: la UI ha bloccato la mappa.</item>
    ///   <item><b>HasValidCell</b>: il boundary ArcGraph ha risolto una cella valida.</item>
    ///   <item><b>DidShowHover</b>: lo sprite di hover e' stato acceso nel frame.</item>
    ///   <item><b>Cell</b>: cella visuale osservata dal puntatore.</item>
    ///   <item><b>Reason</b>: motivo sintetico dell'ultimo esito.</item>
    /// </list>
    /// </summary>
    public readonly struct ArcGraphPointerCellHoverDiagnostics
    {
        public readonly bool DidReceiveFrame;
        public readonly bool HoverEnabled;
        public readonly bool WasPointerOverUi;
        public readonly bool HasValidCell;
        public readonly bool DidShowHover;
        public readonly ArcGraphCellCoord Cell;
        public readonly string Reason;

        // =============================================================================
        // ArcGraphPointerCellHoverDiagnostics
        // =============================================================================
        /// <summary>
        /// <para>
        /// Costruisce una diagnostica immutabile per l'ultimo frame hover.
        /// </para>
        /// </summary>
        public ArcGraphPointerCellHoverDiagnostics(
            bool didReceiveFrame,
            bool hoverEnabled,
            bool wasPointerOverUi,
            bool hasValidCell,
            bool didShowHover,
            ArcGraphCellCoord cell,
            string reason)
        {
            DidReceiveFrame = didReceiveFrame;
            HoverEnabled = hoverEnabled;
            WasPointerOverUi = wasPointerOverUi;
            HasValidCell = hasValidCell;
            DidShowHover = didShowHover;
            Cell = cell;
            Reason = string.IsNullOrWhiteSpace(reason) ? "None" : reason;
        }
    }

    // =============================================================================
    // ArcGraphPointerCellHoverSceneConsumer
    // =============================================================================
    /// <summary>
    /// <para>
    /// Consumer scena che evidenzia sempre la cella ArcGraph sotto il puntatore.
    /// </para>
    ///
    /// <para><b>Principio architetturale: boundary interattivo piu' camera visuale</b></para>
    /// <para>
    /// Il componente riceve un <c>ArcGraphInteractionFrame</c> gia' prodotto dal
    /// boundary ArcGraph e usa la camera scena solo per convertire il punto
    /// viewport in coordinate world-space quando lo zoom fisico e' attivo. Non legge
    /// mouse, non interroga il mondo, non seleziona target e non invia comandi. La
    /// sua unica responsabilita' e' rendere visibile la cella realmente sotto il
    /// puntatore nella vista renderizzata.
    /// </para>
    ///
    /// <para><b>Struttura interna:</b></para>
    /// <list type="bullet">
    ///   <item><b>ConsumeInteractionFrame</b>: riceve il frame e decide se mostrare l'hover.</item>
    ///   <item><b>SetHoverEnabled</b>: gate pubblico per test e cablaggio runtime.</item>
    ///   <item><b>EnsureHoverRenderer</b>: crea una sola sprite runtime riusabile.</item>
    ///   <item><b>ShowHoverAtCell</b>: posiziona l'overlay sulla cella corrente.</item>
    ///   <item><b>HideHover</b>: spegne il visual senza distruggerlo.</item>
    /// </list>
    /// </summary>
    public sealed class ArcGraphPointerCellHoverSceneConsumer : MonoBehaviour, IArcGraphInteractionFrameConsumer
    {
        [SerializeField] private bool hoverEnabled = true;
        [SerializeField] private Camera sceneCamera;
        [SerializeField] private bool preferSceneCameraCell = true;
        [SerializeField] private Color hoverColor = new Color(1f, 1f, 1f, 0.18f);
        [SerializeField] private float tileWorldSize = 1f;
        [SerializeField] private float zOffset = -0.02f;
        [SerializeField] private int sortingOrder = 210;
        [SerializeField] private Vector3 originOffset = Vector3.zero;
        [SerializeField] private string hoverObjectName = "ArcGraphPointerCellHover";
        [SerializeField] private bool logDiagnostics;

        private GameObject _hoverObject;
        private SpriteRenderer _hoverRenderer;
        private Texture2D _hoverTexture;
        private Sprite _hoverSprite;
        private ArcGraphPointerCellHoverDiagnostics _lastDiagnostics =
            new ArcGraphPointerCellHoverDiagnostics(
                false,
                false,
                false,
                false,
                false,
                new ArcGraphCellCoord(0, 0, 0),
                "NotInitialized");

        public ArcGraphPointerCellHoverDiagnostics LastDiagnostics => _lastDiagnostics;
        public bool HoverEnabled => hoverEnabled;

        // =============================================================================
        // ConsumeInteractionFrame
        // =============================================================================
        /// <summary>
        /// <para>
        /// Consuma il frame interattivo corrente e aggiorna l'highlight della cella.
        /// </para>
        ///
        /// <para><b>Semantica di hover puro</b></para>
        /// <para>
        /// Il metodo non interpreta click e non distingue tra cella vuota, NPC o
        /// oggetto. Se il boundary ha una cella valida, quella cella viene
        /// evidenziata. Se la UI blocca il puntatore o la cella non e' valida,
        /// l'highlight viene spento.
        /// </para>
        /// </summary>
        public void ConsumeInteractionFrame(
            ArcGraphInteractionFrame interactionFrame,
            ArcGraphInteractionSceneAdapterDiagnostics diagnostics)
        {
            if (!hoverEnabled)
            {
                HideAndStore(interactionFrame, false, "HoverDisabled");
                return;
            }

            if (interactionFrame.IsPointerOverUi)
            {
                HideAndStore(interactionFrame, false, "PointerOverUi");
                return;
            }

            if (!interactionFrame.HasValidCell)
            {
                if (preferSceneCameraCell &&
                    TryResolveCellFromSceneCamera(interactionFrame, out ArcGraphCellCoord cameraOnlyCell, out string cameraOnlyReason))
                {
                    ShowHoverAtCell(cameraOnlyCell);
                    StoreDiagnostics(interactionFrame, cameraOnlyCell, true, true, cameraOnlyReason);
                    return;
                }

                HideAndStore(interactionFrame, false, "CellUnavailable");
                return;
            }

            ArcGraphCellCoord cell = interactionFrame.Cell;
            string reason = "CellHoverShown";

            // Il frame interattivo storico usa un mapper logico basato sulla view.
            // Durante lo zoom fisico della camera quel mapper puo' non coincidere
            // con il pixel realmente renderizzato. Per l'hover visuale preferiamo
            // quindi la camera scena, che e' la stessa usata per mostrare la mappa.
            if (preferSceneCameraCell &&
                TryResolveCellFromSceneCamera(interactionFrame, out ArcGraphCellCoord cameraCell, out string cameraReason))
            {
                cell = cameraCell;
                reason = cameraReason;
            }

            ShowHoverAtCell(cell);
            StoreDiagnostics(interactionFrame, cell, true, true, reason);
        }

        // =============================================================================
        // SetHoverEnabled
        // =============================================================================
        /// <summary>
        /// <para>
        /// Abilita o disabilita il consumer hover cella.
        /// </para>
        /// </summary>
        public void SetHoverEnabled(bool enabled)
        {
            hoverEnabled = enabled;

            // Quando il gate viene spento il visual viene nascosto subito, cosi' non
            // resta in scena una cella evidenziata appartenente a un frame vecchio.
            if (!enabled)
                HideHover();
        }

        // =============================================================================
        // SetSceneCamera
        // =============================================================================
        /// <summary>
        /// <para>
        /// Assegna la camera usata per convertire puntatore viewport in cella mondo.
        /// </para>
        ///
        /// <para><b>Coerenza con zoom e pan fisici</b></para>
        /// <para>
        /// Il consumer non usa la camera per interrogare la simulazione. La usa solo
        /// come trasformazione visuale, cosi' l'highlight resta agganciato alla
        /// stessa immagine che l'utente vede dopo zoom e pan.
        /// </para>
        /// </summary>
        public void SetSceneCamera(Camera camera)
        {
            sceneCamera = camera;
        }

        // =============================================================================
        // EnableHoverFromInspector
        // =============================================================================
        /// <summary>
        /// <para>
        /// Context menu per abilitare l'hover cella durante test manuali.
        /// </para>
        /// </summary>
        [ContextMenu("ArcGraph/Enable Pointer Cell Hover")]
        public void EnableHoverFromInspector()
        {
            SetHoverEnabled(true);
        }

        // =============================================================================
        // DisableHoverFromInspector
        // =============================================================================
        /// <summary>
        /// <para>
        /// Context menu per disabilitare l'hover cella durante test manuali.
        /// </para>
        /// </summary>
        [ContextMenu("ArcGraph/Disable Pointer Cell Hover")]
        public void DisableHoverFromInspector()
        {
            SetHoverEnabled(false);
        }

        // =============================================================================
        // LogPointerCellHoverDiagnosticsFromInspector
        // =============================================================================
        /// <summary>
        /// <para>
        /// Stampa in Console l'ultima diagnostica dell'hover cella.
        /// </para>
        /// </summary>
        [ContextMenu("ArcGraph/Log Pointer Cell Hover Diagnostics")]
        public void LogPointerCellHoverDiagnosticsFromInspector()
        {
            Debug.Log(
                "[ArcGraphPointerCellHoverSceneConsumer] " +
                _lastDiagnostics.Reason +
                ", enabled=" + _lastDiagnostics.HoverEnabled +
                ", pointerOverUi=" + _lastDiagnostics.WasPointerOverUi +
                ", validCell=" + _lastDiagnostics.HasValidCell +
                ", cell=" + _lastDiagnostics.Cell +
                ", shown=" + _lastDiagnostics.DidShowHover);
        }

        // =============================================================================
        // OnDestroy
        // =============================================================================
        /// <summary>
        /// <para>
        /// Rilascia le risorse runtime create per lo sprite hover.
        /// </para>
        /// </summary>
        private void OnDestroy()
        {
            if (_hoverSprite != null)
                Destroy(_hoverSprite);

            if (_hoverTexture != null)
                Destroy(_hoverTexture);

            _hoverSprite = null;
            _hoverTexture = null;
        }

        // =============================================================================
        // EnsureHoverRenderer
        // =============================================================================
        /// <summary>
        /// <para>
        /// Crea lo sprite renderer runtime usato dall'overlay cella.
        /// </para>
        /// </summary>
        private void EnsureHoverRenderer()
        {
            if (_hoverRenderer != null)
                return;

            // La texture 1x1 bianca viene colorata dal renderer. In questo modo non
            // servono asset grafici, prefab o sprite importati per il primo gate.
            _hoverTexture = new Texture2D(1, 1)
            {
                name = "ArcGraphPointerCellHoverTexture",
                filterMode = FilterMode.Point,
                wrapMode = TextureWrapMode.Clamp
            };
            _hoverTexture.SetPixel(0, 0, Color.white);
            _hoverTexture.Apply();

            _hoverSprite = Sprite.Create(
                _hoverTexture,
                new Rect(0f, 0f, 1f, 1f),
                new Vector2(0.5f, 0.5f),
                1f);
            _hoverSprite.name = "ArcGraphPointerCellHoverSprite";

            _hoverObject = new GameObject(hoverObjectName);
            _hoverObject.transform.SetParent(transform, false);
            _hoverRenderer = _hoverObject.AddComponent<SpriteRenderer>();
            _hoverRenderer.sprite = _hoverSprite;
            _hoverRenderer.sortingOrder = sortingOrder;
            _hoverRenderer.color = hoverColor;
            _hoverObject.SetActive(false);
        }

        // =============================================================================
        // ShowHoverAtCell
        // =============================================================================
        /// <summary>
        /// <para>
        /// Posiziona e mostra l'overlay sopra la cella indicata dal frame.
        /// </para>
        /// </summary>
        private void ShowHoverAtCell(ArcGraphCellCoord cell)
        {
            EnsureHoverRenderer();

            // Lo sprite 1x1 viene scalato alla dimensione della tile ArcGraph e
            // centrato sulla cella. Il piccolo zOffset tiene l'overlay sopra il
            // terreno senza cambiare coordinate logiche o collisioni.
            _hoverObject.transform.localPosition = ResolveWorldPosition(cell);
            _hoverObject.transform.localScale = new Vector3(tileWorldSize, tileWorldSize, 1f);
            _hoverRenderer.color = hoverColor;
            _hoverRenderer.sortingOrder = sortingOrder;
            _hoverObject.SetActive(true);
        }

        // =============================================================================
        // TryResolveCellFromSceneCamera
        // =============================================================================
        /// <summary>
        /// <para>
        /// Converte il punto puntatore del frame nella cella realmente vista dalla
        /// camera Unity.
        /// </para>
        ///
        /// <para><b>Correzione zoom/pan</b></para>
        /// <para>
        /// Il frame input contiene coordinate relative al viewport camera. Per usare
        /// <c>ScreenToWorldPoint</c> le riportiamo nello spazio screen assoluto
        /// sommando l'origine del <c>pixelRect</c>. La conversione finale usa solo
        /// matematica view-side: non legge oggetti, NPC o World.
        /// </para>
        /// </summary>
        private bool TryResolveCellFromSceneCamera(
            ArcGraphInteractionFrame interactionFrame,
            out ArcGraphCellCoord cell,
            out string reason)
        {
            cell = interactionFrame.Cell;
            reason = "SceneCameraCellUnavailable";

            if (!interactionFrame.Input.HasPointerScreenPosition)
            {
                reason = "PointerMissing";
                return false;
            }

            Camera camera = ResolveSceneCamera();
            if (camera == null)
            {
                reason = "SceneCameraMissing";
                return false;
            }

            Rect pixelRect = camera.pixelRect;
            if (pixelRect.width <= 0f || pixelRect.height <= 0f)
            {
                reason = "SceneCameraViewportInvalid";
                return false;
            }

            Vector2 absoluteScreenPoint = new Vector2(
                interactionFrame.Input.PointerScreenX + pixelRect.x,
                interactionFrame.Input.PointerScreenY + pixelRect.y);

            if (!pixelRect.Contains(absoluteScreenPoint))
            {
                reason = "PointerOutsideSceneCamera";
                return false;
            }

            float safeTileWorldSize = tileWorldSize > 0.0001f ? tileWorldSize : 1f;
            float worldPlaneDistance = ResolveWorldPlaneDistance(camera);
            Vector3 worldPoint = camera.ScreenToWorldPoint(new Vector3(
                absoluteScreenPoint.x,
                absoluteScreenPoint.y,
                worldPlaneDistance));

            int cellX = Mathf.FloorToInt((worldPoint.x - originOffset.x) / safeTileWorldSize);
            int cellY = Mathf.FloorToInt((worldPoint.y - originOffset.y) / safeTileWorldSize);
            int cellZ = interactionFrame.HasValidCell ? interactionFrame.Cell.Z : 0;

            cell = new ArcGraphCellCoord(cellX, cellY, cellZ);
            reason = "SceneCameraCellHoverShown";
            return true;
        }

        // =============================================================================
        // ResolveSceneCamera
        // =============================================================================
        /// <summary>
        /// <para>
        /// Restituisce la camera esplicita o, in fallback, <c>Camera.main</c>.
        /// </para>
        /// </summary>
        private Camera ResolveSceneCamera()
        {
            if (sceneCamera != null)
                return sceneCamera;

            return Camera.main;
        }

        // =============================================================================
        // ResolveWorldPlaneDistance
        // =============================================================================
        /// <summary>
        /// <para>
        /// Calcola la distanza dal piano world-space usato dalla mappa ArcGraph.
        /// </para>
        /// </summary>
        private float ResolveWorldPlaneDistance(Camera camera)
        {
            if (camera == null)
                return 0f;

            float distance = originOffset.z - camera.transform.position.z;
            return Mathf.Abs(distance) > 0.001f
                ? Mathf.Abs(distance)
                : Mathf.Max(0.001f, camera.nearClipPlane);
        }

        // =============================================================================
        // ResolveWorldPosition
        // =============================================================================
        /// <summary>
        /// <para>
        /// Converte una cella ArcGraph nella posizione world-space del suo centro.
        /// </para>
        /// </summary>
        private Vector3 ResolveWorldPosition(ArcGraphCellCoord cell)
        {
            return originOffset + new Vector3(
                (cell.X + 0.5f) * tileWorldSize,
                (cell.Y + 0.5f) * tileWorldSize,
                zOffset);
        }

        // =============================================================================
        // HideAndStore
        // =============================================================================
        /// <summary>
        /// <para>
        /// Nasconde l'hover e registra una diagnostica coerente con il frame.
        /// </para>
        /// </summary>
        private void HideAndStore(
            ArcGraphInteractionFrame interactionFrame,
            bool didShowHover,
            string reason)
        {
            HideHover();
            StoreDiagnostics(
                interactionFrame,
                interactionFrame.Cell,
                interactionFrame.HasValidCell,
                didShowHover,
                reason);
        }

        // =============================================================================
        // HideHover
        // =============================================================================
        /// <summary>
        /// <para>
        /// Spegne il GameObject visuale senza distruggerlo.
        /// </para>
        /// </summary>
        private void HideHover()
        {
            if (_hoverObject != null)
                _hoverObject.SetActive(false);
        }

        // =============================================================================
        // StoreDiagnostics
        // =============================================================================
        /// <summary>
        /// <para>
        /// Salva l'esito dell'ultimo frame e opzionalmente lo scrive in Console.
        /// </para>
        /// </summary>
        private void StoreDiagnostics(
            ArcGraphInteractionFrame interactionFrame,
            ArcGraphCellCoord cell,
            bool hasValidCell,
            bool didShowHover,
            string reason)
        {
            _lastDiagnostics = new ArcGraphPointerCellHoverDiagnostics(
                true,
                hoverEnabled,
                interactionFrame.IsPointerOverUi,
                hasValidCell,
                didShowHover,
                cell,
                reason);

            if (!logDiagnostics)
                return;

            Debug.Log(
                "[ArcGraphPointerCellHoverSceneConsumer] " +
                _lastDiagnostics.Reason +
                ", enabled=" + _lastDiagnostics.HoverEnabled +
                ", pointerOverUi=" + _lastDiagnostics.WasPointerOverUi +
                ", validCell=" + _lastDiagnostics.HasValidCell +
                ", cell=" + _lastDiagnostics.Cell +
                ", shown=" + _lastDiagnostics.DidShowHover);
        }
    }
}
