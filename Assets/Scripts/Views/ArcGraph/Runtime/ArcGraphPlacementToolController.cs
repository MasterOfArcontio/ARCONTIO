using Arcontio.Core;
using Arcontio.Core.Commands.DevTools;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;

namespace Arcontio.View.ArcGraph
{
    // =============================================================================
    // ArcGraphPlacementToolController
    // =============================================================================
    /// <summary>
    /// <para>
    /// Controller runtime minimale per il piazzamento oggetti ArcGraph.
    /// </para>
    ///
    /// <para><b>Principio architetturale: ArcGraph invia richieste, non muta il World</b></para>
    /// <para>
    /// Questo componente sostituisce il vecchio uso F3 agganciato a
    /// <c>MapGridRuntimeDevToolsOverlay</c> per il caso minimo di inserimento
    /// oggetti. La vista ArcGraph risolve cella e intenzione, poi invia un
    /// <see cref="DevPlaceObjectCommand"/> al <see cref="SimulationHost"/> tramite
    /// il command buffer ufficiale. Non chiama <c>World.CreateObject</c>, non
    /// modifica cache derivate e non legge strutture MapGrid.
    /// </para>
    ///
    /// <para><b>Struttura interna:</b></para>
    /// <list type="bullet">
    ///   <item><b>Input</b>: F3 apre/chiude il placement ArcGraph minimale.</item>
    ///   <item><b>Preview source</b>: espone defId e cella tramite <see cref="IArcGraphPlacementPreviewSource"/>.</item>
    ///   <item><b>Coordinate</b>: converte mouse screen-space in cella usando la camera Unity e il piano ArcGraph.</item>
    ///   <item><b>Command gateway</b>: al click sinistro accoda <see cref="DevPlaceObjectCommand"/> su <see cref="SimulationHost"/>.</item>
    /// </list>
    /// </summary>
    public sealed class ArcGraphPlacementToolController : MonoBehaviour, IArcGraphPlacementPreviewSource
    {
        private const string DefaultWallDefId = "wall_stone";
        private const string DoorWoodDefId = "door_wood";
        private const string DoorWoodLockedDefId = "door_wood_locked";
        private const string FoodStockDefId = "food_stock";

        [SerializeField] private Key toggleKey = Key.F3;
        [SerializeField] private string activeDefId = DefaultWallDefId;
        [SerializeField] private Camera sceneCamera;
        [SerializeField] private ArcGraphRuntimeContextProvider runtimeContextProvider;
        [SerializeField] private float tileWorldSize = 1f;
        [SerializeField] private Vector3 originOffset = Vector3.zero;
        [SerializeField] private bool placementEnabled;
        [SerializeField] private bool logDiagnostics;

        private bool _isPointerOverUi;
        private string _lastPlacementReason = "NotInitialized";

        public bool IsObjectPlacementPreviewActive =>
            placementEnabled && !string.IsNullOrWhiteSpace(activeDefId);

        public bool IsPointerOverPlacementUi => _isPointerOverUi;

        // =============================================================================
        // SetRuntimeContextProvider
        // =============================================================================
        /// <summary>
        /// <para>
        /// Assegna il provider read-only ArcGraph da cui recuperare dimensioni mappa
        /// e validazione bounds.
        /// </para>
        /// </summary>
        public void SetRuntimeContextProvider(ArcGraphRuntimeContextProvider provider)
        {
            runtimeContextProvider = provider;
        }

        // =============================================================================
        // SetSceneCamera
        // =============================================================================
        /// <summary>
        /// <para>
        /// Assegna la camera usata per convertire il puntatore in coordinate mondo.
        /// </para>
        /// </summary>
        public void SetSceneCamera(Camera camera)
        {
            sceneCamera = camera;
        }

        // =============================================================================
        // SetActiveDefId
        // =============================================================================
        /// <summary>
        /// <para>
        /// Permette alla futura UI operativa ArcGraph di cambiare l'oggetto
        /// selezionato senza accedere direttamente al World o ai cataloghi grafici.
        /// </para>
        /// </summary>
        public void SetActiveDefId(string defId)
        {
            activeDefId = string.IsNullOrWhiteSpace(defId)
                ? DefaultWallDefId
                : defId.Trim();
        }

        // =============================================================================
        // SetPlacementEnabled
        // =============================================================================
        /// <summary>
        /// <para>
        /// Abilita o disabilita il tool di placement ArcGraph.
        /// </para>
        /// </summary>
        public void SetPlacementEnabled(bool enabled)
        {
            placementEnabled = enabled;
        }

        // =============================================================================
        // TryGetActiveObjectPlacementPreviewDefId
        // =============================================================================
        /// <summary>
        /// <para>
        /// Espone alla preview ArcGraph il defId che verrebbe piazzato al click.
        /// </para>
        /// </summary>
        public bool TryGetActiveObjectPlacementPreviewDefId(out string defId)
        {
            defId = string.Empty;

            if (!IsObjectPlacementPreviewActive)
                return false;

            defId = activeDefId;
            return true;
        }

        // =============================================================================
        // TryGetObjectPlacementPreviewCell
        // =============================================================================
        /// <summary>
        /// <para>
        /// Espone alla preview ArcGraph la cella sotto il puntatore.
        /// </para>
        /// </summary>
        public bool TryGetObjectPlacementPreviewCell(
            out int cellX,
            out int cellY)
        {
            cellX = 0;
            cellY = 0;

            if (!IsObjectPlacementPreviewActive)
                return false;

            UpdatePointerOverUiFlag();
            if (_isPointerOverUi)
                return false;

            return TryResolvePointerCell(out cellX, out cellY, out _);
        }

        // =============================================================================
        // Update
        // =============================================================================
        /// <summary>
        /// <para>
        /// Legge input tastiera/mouse e produce comandi di placement autorizzati.
        /// </para>
        /// </summary>
        private void Update()
        {
            ResolveTransientReferences();
            HandleToggleInput();

            if (!IsObjectPlacementPreviewActive)
                return;

            UpdatePointerOverUiFlag();
            if (_isPointerOverUi)
                return;

            Mouse mouse = Mouse.current;
            if (mouse == null || !mouse.leftButton.wasPressedThisFrame)
                return;

            if (!TryResolvePointerCell(out int cellX, out int cellY, out string reason))
            {
                _lastPlacementReason = reason;
                return;
            }

            EnqueuePlacementCommand(cellX, cellY);
        }

        // =============================================================================
        // HandleToggleInput
        // =============================================================================
        /// <summary>
        /// <para>
        /// Gestisce F3 come toggle del placement ArcGraph minimale.
        /// </para>
        /// </summary>
        private void HandleToggleInput()
        {
            if (Keyboard.current == null)
                return;

            if (!Keyboard.current[toggleKey].wasPressedThisFrame)
                return;

            placementEnabled = !placementEnabled;
            _lastPlacementReason = placementEnabled
                ? "PlacementEnabled"
                : "PlacementDisabled";

            if (logDiagnostics)
                Debug.Log("[ArcGraphPlacementToolController] " + _lastPlacementReason, this);
        }

        // =============================================================================
        // EnqueuePlacementCommand
        // =============================================================================
        /// <summary>
        /// <para>
        /// Traduce il click mappa in comando Core, mantenendo la mutazione fuori
        /// dalla View.
        /// </para>
        /// </summary>
        private void EnqueuePlacementCommand(
            int cellX,
            int cellY)
        {
            SimulationHost host = SimulationHost.Instance;
            if (host == null)
            {
                _lastPlacementReason = "SimulationHostMissing";
                return;
            }

            host.EnqueueExternalCommand(BuildPlacementCommand(cellX, cellY));
            _lastPlacementReason = "PlacementCommandQueued";
        }

        // =============================================================================
        // BuildPlacementCommand
        // =============================================================================
        /// <summary>
        /// <para>
        /// Crea il comando di placement con i parametri speciali minimi per porte e
        /// cibo.
        /// </para>
        /// </summary>
        private DevPlaceObjectCommand BuildPlacementCommand(
            int cellX,
            int cellY)
        {
            string defId = string.IsNullOrWhiteSpace(activeDefId)
                ? DefaultWallDefId
                : activeDefId;

            if (defId == FoodStockDefId)
            {
                return new DevPlaceObjectCommand(
                    defId,
                    cellX,
                    cellY,
                    OwnerKind.Community,
                    0,
                    foodUnits: 1);
            }

            if (defId == DoorWoodDefId)
            {
                return new DevPlaceObjectCommand(
                    defId,
                    cellX,
                    cellY,
                    OwnerKind.Community,
                    0,
                    doorOpen: false,
                    doorLocked: false);
            }

            if (defId == DoorWoodLockedDefId)
            {
                return new DevPlaceObjectCommand(
                    defId,
                    cellX,
                    cellY,
                    OwnerKind.Community,
                    0,
                    doorOpen: false,
                    doorLocked: true);
            }

            return new DevPlaceObjectCommand(defId, cellX, cellY);
        }

        // =============================================================================
        // TryResolvePointerCell
        // =============================================================================
        /// <summary>
        /// <para>
        /// Converte il puntatore mouse nella cella ArcGraph corrispondente.
        /// </para>
        /// </summary>
        private bool TryResolvePointerCell(
            out int cellX,
            out int cellY,
            out string reason)
        {
            cellX = 0;
            cellY = 0;
            reason = "None";

            Mouse mouse = Mouse.current;
            if (mouse == null)
            {
                reason = "MouseMissing";
                return false;
            }

            Camera camera = ResolveSceneCamera();
            if (camera == null)
            {
                reason = "SceneCameraMissing";
                return false;
            }

            Vector2 screenPosition = mouse.position.ReadValue();
            float planeDistance = ResolveWorldPlaneDistance(camera);
            Vector3 worldPosition = camera.ScreenToWorldPoint(new Vector3(
                screenPosition.x,
                screenPosition.y,
                planeDistance));

            float safeTileWorldSize = tileWorldSize > 0f ? tileWorldSize : 1f;
            cellX = Mathf.FloorToInt((worldPosition.x - originOffset.x) / safeTileWorldSize);
            cellY = Mathf.FloorToInt((worldPosition.y - originOffset.y) / safeTileWorldSize);

            World world = ResolveWorld();
            if (world != null
                && (cellX < 0 || cellY < 0 || cellX >= world.MapWidth || cellY >= world.MapHeight))
            {
                reason = "CellOutOfWorld";
                return false;
            }

            reason = "CellResolved";
            return true;
        }

        // =============================================================================
        // ResolveTransientReferences
        // =============================================================================
        /// <summary>
        /// <para>
        /// Recupera riferimenti runtime quando l'installer non li ha ancora assegnati.
        /// </para>
        /// </summary>
        private void ResolveTransientReferences()
        {
            if (sceneCamera == null)
                sceneCamera = Camera.main;
        }

        // =============================================================================
        // ResolveSceneCamera
        // =============================================================================
        /// <summary>
        /// <para>
        /// Restituisce la camera scena corrente.
        /// </para>
        /// </summary>
        private Camera ResolveSceneCamera()
        {
            return sceneCamera != null ? sceneCamera : Camera.main;
        }

        // =============================================================================
        // ResolveWorld
        // =============================================================================
        /// <summary>
        /// <para>
        /// Recupera il World tramite provider ArcGraph o fallback SimulationHost.
        /// </para>
        /// </summary>
        private World ResolveWorld()
        {
            ArcGraphRuntimeContext context = runtimeContextProvider != null
                ? runtimeContextProvider.BuildTerrainRuntimeContext()
                : ArcGraphRuntimeContext.Empty();

            if (context.World != null)
                return context.World;

            SimulationHost host = SimulationHost.Instance;
            return host != null ? host.World : null;
        }

        // =============================================================================
        // UpdatePointerOverUiFlag
        // =============================================================================
        /// <summary>
        /// <para>
        /// Aggiorna il gate anti click-through sopra la UI UGUI.
        /// </para>
        /// </summary>
        private void UpdatePointerOverUiFlag()
        {
            _isPointerOverUi =
                EventSystem.current != null
                && EventSystem.current.IsPointerOverGameObject();
        }

        // =============================================================================
        // ResolveWorldPlaneDistance
        // =============================================================================
        /// <summary>
        /// <para>
        /// Calcola la distanza lungo la camera fino al piano Z usato da ArcGraph.
        /// </para>
        /// </summary>
        private float ResolveWorldPlaneDistance(Camera camera)
        {
            if (camera == null)
                return 0f;

            return originOffset.z - camera.transform.position.z;
        }
    }
}
