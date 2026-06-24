using UnityEngine;

namespace Arcontio.View.ArcGraph
{
    // =============================================================================
    // ArcGraphPointerHudSceneConsumer
    // =============================================================================
    /// <summary>
    /// <para>
    /// Consumer scena temporaneo per visualizzare il Pointer HUD ArcGraph.
    /// </para>
    ///
    /// <para><b>Principio architetturale: consumer UI separato dal renderer</b></para>
    /// <para>
    /// Questo componente implementa <c>IArcGraphInteractionFrameConsumer</c> e riceve
    /// frame interattivi gia' prodotti dal wrapper scena. Non legge mouse, non legge
    /// camera, non interroga il mondo, non seleziona NPC e non invia comandi. La sua
    /// unica responsabilita' e' tradurre il frame in
    /// <c>ArcGraphPointerHudSnapshot</c> e, se il gate e' attivo, mostrarlo con un HUD
    /// provvisorio.
    /// </para>
    ///
    /// <para><b>Struttura interna:</b></para>
    /// <list type="bullet">
    ///   <item><b>ConsumeInteractionFrame</b>: riceve il frame dal wrapper ArcGraph.</item>
    ///   <item><b>OnGUI</b>: disegna il testo HUD solo se abilitato.</item>
    ///   <item><b>SetHudEnabled</b>: gate pubblico per attivazione controllata.</item>
    ///   <item><b>ClearSnapshot</b>: reset manuale della diagnostica locale.</item>
    /// </list>
    /// </summary>
    public sealed class ArcGraphPointerHudSceneConsumer : MonoBehaviour, IArcGraphInteractionFrameConsumer
    {
        [SerializeField] private bool hudEnabled;
        [SerializeField] private bool drawInOnGui = true;
        [SerializeField] private bool consumeWhenHudDisabled = true;
        [SerializeField] private Camera sceneCamera;
        [SerializeField] private bool preferSceneCameraCell = true;
        [SerializeField] private float tileWorldSize = 1f;
        [SerializeField] private Vector3 originOffset = Vector3.zero;
        [SerializeField] private Rect hudRect = new Rect(150f, 62f, 420f, 30f);
        [SerializeField] private Color backgroundColor = new Color(0f, 0f, 0f, 0.60f);
        [SerializeField] private Color textColor = Color.white;

        private readonly ArcGraphPointerHudSnapshotBuilder _builder = new();

        private ArcGraphPointerHudSnapshot _lastSnapshot = ArcGraphPointerHudSnapshot.Empty("NotInitialized");
        private ArcGraphPointerHudDiagnostics _lastDiagnostics;
        private GUIStyle _boxStyle;
        private GUIStyle _labelStyle;
        private Texture2D _backgroundTexture;

        public ArcGraphPointerHudSnapshot LastSnapshot => _lastSnapshot;
        public ArcGraphPointerHudDiagnostics LastDiagnostics => _lastDiagnostics;
        public bool HudEnabled => hudEnabled;

        // =============================================================================
        // ConsumeInteractionFrame
        // =============================================================================
        /// <summary>
        /// <para>
        /// Riceve il frame interattivo corrente e aggiorna lo snapshot HUD.
        /// </para>
        ///
        /// <para><b>Consumer passivo</b></para>
        /// <para>
        /// Il metodo non interpreta click, non cambia selection e non produce comandi.
        /// Anche quando l'HUD visivo e' spento puo' consumare il frame, cosi'
        /// Inspector e diagnostica restano utili durante i test.
        /// </para>
        /// </summary>
        public void ConsumeInteractionFrame(
            ArcGraphInteractionFrame interactionFrame,
            ArcGraphInteractionSceneAdapterDiagnostics diagnostics)
        {
            if (!hudEnabled && !consumeWhenHudDisabled)
                return;

            if (preferSceneCameraCell &&
                TryResolveCellFromSceneCamera(interactionFrame, out ArcGraphCellCoord cameraCell))
            {
                interactionFrame = new ArcGraphInteractionFrame(
                    interactionFrame.Input,
                    interactionFrame.Coordinate,
                    interactionFrame.TargetKind,
                    cameraCell,
                    interactionFrame.ActorId,
                    interactionFrame.ObjectId,
                    true,
                    interactionFrame.IsPointerOverUi,
                    interactionFrame.Reason);
            }

            // Il builder resta la fonte unica del testo HUD. Il componente scena non
            // duplica logica di formattazione, cosi' il contratto data-only e la UI
            // temporanea rimangono allineati.
            _lastSnapshot = _builder.BuildWithAdapterDiagnostics(interactionFrame, diagnostics);
            _lastDiagnostics = _builder.LastDiagnostics;
        }

        // =============================================================================
        // OnGUI
        // =============================================================================
        /// <summary>
        /// <para>
        /// Disegna il Pointer HUD provvisorio.
        /// </para>
        ///
        /// <para><b>HUD temporaneo senza Canvas</b></para>
        /// <para>
        /// L'uso di <c>OnGUI</c> e' intenzionale in questo micro-step: permette un
        /// test visuale immediato senza creare Canvas, prefab, font asset o gerarchie
        /// UI persistenti. Il pannello definitivo potra' sostituire questo consumer
        /// quando la modularizzazione UI sara' pronta.
        /// </para>
        /// </summary>
        private void OnGUI()
        {
            if (!hudEnabled || !drawInOnGui)
                return;

            EnsureGuiResources();

            Color previousColor = GUI.color;
            GUI.color = Color.white;

            // Il box viene disegnato prima del testo per mantenere leggibile il
            // contenuto anche sopra tile, overlay debug e actor.
            GUI.Box(hudRect, GUIContent.none, _boxStyle);

            GUI.color = textColor;
            GUI.Label(CreateTextRect(hudRect), _lastSnapshot.DisplayText, _labelStyle);

            GUI.color = previousColor;
        }

        // =============================================================================
        // OnDestroy
        // =============================================================================
        /// <summary>
        /// <para>
        /// Rilascia la texture runtime usata dal box HUD provvisorio.
        /// </para>
        /// </summary>
        private void OnDestroy()
        {
            if (_backgroundTexture == null)
                return;

            Destroy(_backgroundTexture);
            _backgroundTexture = null;
        }

        // =============================================================================
        // SetHudEnabled
        // =============================================================================
        /// <summary>
        /// <para>
        /// Abilita o disabilita il disegno del Pointer HUD.
        /// </para>
        /// </summary>
        public void SetHudEnabled(bool enabled)
        {
            hudEnabled = enabled;
        }

        // =============================================================================
        // SetSceneCamera
        // =============================================================================
        /// <summary>
        /// <para>
        /// Assegna la camera usata per convertire il puntatore in cella renderizzata.
        /// </para>
        ///
        /// <para><b>Coerenza con hover cella</b></para>
        /// <para>
        /// Il Pointer HUD deve mostrare la stessa riga/colonna evidenziata dal
        /// consumer hover. Per questo usa la camera scena solo come trasformazione
        /// view-side, senza interrogare World o selezione.
        /// </para>
        /// </summary>
        public void SetSceneCamera(Camera camera)
        {
            sceneCamera = camera;
        }

        // =============================================================================
        // EnableHudFromInspector
        // =============================================================================
        /// <summary>
        /// <para>
        /// Context menu per attivare rapidamente il probe HUD durante i test.
        /// </para>
        /// </summary>
        [ContextMenu("ArcGraph/Enable Pointer HUD")]
        public void EnableHudFromInspector()
        {
            SetHudEnabled(true);
        }

        // =============================================================================
        // DisableHudFromInspector
        // =============================================================================
        /// <summary>
        /// <para>
        /// Context menu per spegnere rapidamente il probe HUD durante i test.
        /// </para>
        /// </summary>
        [ContextMenu("ArcGraph/Disable Pointer HUD")]
        public void DisableHudFromInspector()
        {
            SetHudEnabled(false);
        }

        // =============================================================================
        // ClearSnapshot
        // =============================================================================
        /// <summary>
        /// <para>
        /// Resetta lo snapshot locale del Pointer HUD.
        /// </para>
        /// </summary>
        [ContextMenu("ArcGraph/Clear Pointer HUD Snapshot")]
        public void ClearSnapshot()
        {
            _lastSnapshot = _builder.BuildEmpty("PointerHudCleared");
            _lastDiagnostics = _builder.LastDiagnostics;
        }

        private void EnsureGuiResources()
        {
            if (_backgroundTexture == null)
            {
                _backgroundTexture = new Texture2D(1, 1)
                {
                    name = "ArcGraphPointerHudBackground"
                };
                _backgroundTexture.SetPixel(0, 0, backgroundColor);
                _backgroundTexture.Apply();
            }

            _boxStyle ??= new GUIStyle(GUI.skin.box)
            {
                normal =
                {
                    background = _backgroundTexture
                }
            };

            _labelStyle ??= new GUIStyle(GUI.skin.label)
            {
                alignment = TextAnchor.MiddleLeft,
                fontSize = 14,
                richText = false,
                clipping = TextClipping.Clip
            };
        }

        private bool TryResolveCellFromSceneCamera(
            ArcGraphInteractionFrame interactionFrame,
            out ArcGraphCellCoord cell)
        {
            cell = interactionFrame.Cell;

            if (!interactionFrame.Input.HasPointerScreenPosition)
                return false;

            Camera camera = ResolveSceneCamera();
            if (camera == null)
                return false;

            Rect pixelRect = camera.pixelRect;
            if (pixelRect.width <= 0f || pixelRect.height <= 0f)
                return false;

            Vector2 absoluteScreenPoint = new Vector2(
                interactionFrame.Input.PointerScreenX + pixelRect.x,
                interactionFrame.Input.PointerScreenY + pixelRect.y);

            if (!pixelRect.Contains(absoluteScreenPoint))
                return false;

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
            return true;
        }

        private Camera ResolveSceneCamera()
        {
            if (sceneCamera != null)
                return sceneCamera;

            return Camera.main;
        }

        private float ResolveWorldPlaneDistance(Camera camera)
        {
            if (camera == null)
                return 0f;

            float distance = originOffset.z - camera.transform.position.z;
            return Mathf.Abs(distance) > 0.001f
                ? Mathf.Abs(distance)
                : Mathf.Max(0.001f, camera.nearClipPlane);
        }

        private static Rect CreateTextRect(Rect source)
        {
            return new Rect(
                source.x + 10f,
                source.y + 3f,
                source.width - 20f,
                source.height - 6f);
        }
    }
}
