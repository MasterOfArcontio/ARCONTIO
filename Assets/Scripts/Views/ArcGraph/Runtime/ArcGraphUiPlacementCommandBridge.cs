using Arcontio.Core;
using Arcontio.Core.Commands.DevTools;
using UnityEngine;
using UnityEngine.InputSystem;

namespace Arcontio.View.ArcGraph
{
    // =============================================================================
    // ArcGraphUiPlacementCommandBridge
    // =============================================================================
    /// <summary>
    /// <para>
    /// Ponte temporaneo tra la nuova UI runtime ArcGraph e il comando di placement
    /// DevTools gia' autorizzato dal runtime.
    /// </para>
    ///
    /// <para><b>Principio architetturale: UI -> request -> bridge -> command gateway</b></para>
    /// <para>
    /// Il pannello azione produce una <see cref="ArcUiPlacementRequest"/> e una
    /// preview passiva. Questo componente ascolta solo il click sulla mappa quando
    /// quella request e' valida, completa la cella target e accoda un
    /// <see cref="DevPlaceObjectCommand"/> al <see cref="SimulationHost"/>. Non
    /// chiama API del <c>World</c>, non valida il contenuto della cella, non crea
    /// oggetti direttamente e non sostituisce ancora il gateway finale della UI.
    /// </para>
    ///
    /// <para><b>Struttura interna:</b></para>
    /// <list type="bullet">
    ///   <item><b>Preview gate</b>: lavora solo con preview UI attiva, non con il fallback F3 legacy.</item>
    ///   <item><b>Input</b>: click singolo o brush su celle diverse mentre il mouse e' premuto.</item>
    ///   <item><b>Request completion</b>: aggiorna la cella della request pending.</item>
    ///   <item><b>Command mapping</b>: traduce owner, food units e stato porta verso <c>DevPlaceObjectCommand</c>.</item>
    /// </list>
    /// </summary>
    public sealed class ArcGraphUiPlacementCommandBridge : MonoBehaviour
    {
        [SerializeField] private ArcGraphUiPlacementPreviewSource placementPreviewSource;
        [SerializeField] private bool bridgeEnabled = true;

        private ArcUiPlacementController _placementController;
        private ArcGraphCellCoord _lastBrushCell = new ArcGraphCellCoord(int.MinValue, int.MinValue, int.MinValue);

        // =============================================================================
        // SetPlacementController
        // =============================================================================
        /// <summary>
        /// <para>
        /// Collega il bridge allo stato placement prodotto dal pannello UI.
        /// </para>
        /// </summary>
        public void SetPlacementController(ArcUiPlacementController controller)
        {
            _placementController = controller;
        }

        // =============================================================================
        // SetPlacementPreviewSource
        // =============================================================================
        /// <summary>
        /// <para>
        /// Collega la sorgente preview UI da cui il bridge legge la cella sotto
        /// puntatore.
        /// </para>
        /// </summary>
        public void SetPlacementPreviewSource(ArcGraphUiPlacementPreviewSource source)
        {
            placementPreviewSource = source;
        }

        // =============================================================================
        // Update
        // =============================================================================
        /// <summary>
        /// <para>
        /// Intercetta il gesto di conferma placement e lo converte in comando.
        /// </para>
        /// </summary>
        private void Update()
        {
            if (!bridgeEnabled || _placementController == null || placementPreviewSource == null)
                return;

            ArcUiPlacementRequest request = _placementController.Pending;
            if (!request.IsValid || !placementPreviewSource.HasUiPlacementPreviewActive)
            {
                ResetBrushCellIfReleased();
                return;
            }

            if (placementPreviewSource.IsPointerOverPlacementUi)
            {
                ResetBrushCellIfReleased();
                return;
            }

            Mouse mouse = Mouse.current;
            if (mouse == null)
                return;

            if (!ShouldSubmitPlacement(mouse, request.Mode))
                return;

            if (!placementPreviewSource.TryGetObjectPlacementPreviewCell(out int cellX, out int cellY))
                return;

            ArcGraphCellCoord cell = new ArcGraphCellCoord(cellX, cellY, 0);
            if (request.Mode == ArcUiPlacementMode.Brush && cell.Equals(_lastBrushCell))
                return;

            _lastBrushCell = cell;
            _placementController.SetTargetCell(cell);
            EnqueuePlacementCommand(_placementController.Pending);
        }

        // =============================================================================
        // ShouldSubmitPlacement
        // =============================================================================
        /// <summary>
        /// <para>
        /// Decide se il gesto mouse corrente deve produrre una richiesta di
        /// placement.
        /// </para>
        /// </summary>
        private bool ShouldSubmitPlacement(
            Mouse mouse,
            ArcUiPlacementMode mode)
        {
            if (mode == ArcUiPlacementMode.Brush)
            {
                if (!mouse.leftButton.isPressed)
                {
                    _lastBrushCell = new ArcGraphCellCoord(int.MinValue, int.MinValue, int.MinValue);
                    return false;
                }

                return true;
            }

            _lastBrushCell = new ArcGraphCellCoord(int.MinValue, int.MinValue, int.MinValue);
            return mouse.leftButton.wasPressedThisFrame;
        }

        // =============================================================================
        // ResetBrushCellIfReleased
        // =============================================================================
        /// <summary>
        /// <para>
        /// Pulisce la memoria della cella brush quando il mouse non sta piu'
        /// trascinando.
        /// </para>
        /// </summary>
        private void ResetBrushCellIfReleased()
        {
            Mouse mouse = Mouse.current;
            if (mouse == null || !mouse.leftButton.isPressed)
                _lastBrushCell = new ArcGraphCellCoord(int.MinValue, int.MinValue, int.MinValue);
        }

        // =============================================================================
        // EnqueuePlacementCommand
        // =============================================================================
        /// <summary>
        /// <para>
        /// Accoda il comando autorizzato al runtime, senza eseguire mutazioni locali.
        /// </para>
        /// </summary>
        private static void EnqueuePlacementCommand(ArcUiPlacementRequest request)
        {
            if (!request.IsValid || !request.HasTargetCell)
                return;

            SimulationHost host = SimulationHost.Instance;
            if (host == null)
                return;

            host.EnqueueExternalCommand(BuildPlacementCommand(request));
        }

        // =============================================================================
        // BuildPlacementCommand
        // =============================================================================
        /// <summary>
        /// <para>
        /// Traduce la request UI minima nel comando DevTools temporaneo.
        /// </para>
        /// </summary>
        private static DevPlaceObjectCommand BuildPlacementCommand(ArcUiPlacementRequest request)
        {
            string defId = string.IsNullOrWhiteSpace(request.TargetDefId)
                ? request.OperationKey
                : request.TargetDefId;

            OwnerKind ownerKind = MapOwnerKind(request.Config);
            int ownerId = ownerKind == OwnerKind.Npc ? request.Config.OwnerNpcId : 0;
            int foodUnits = request.Config.FoodUnits < 1 ? 1 : request.Config.FoodUnits;
            bool? doorOpen = null;
            bool? doorLocked = null;

            if (IsDoorDefId(defId))
            {
                doorOpen = request.Config.DoorState == ArcUiDoorPlacementState.Open;
                doorLocked = request.Config.DoorState == ArcUiDoorPlacementState.Locked;
            }

            return new DevPlaceObjectCommand(
                defId,
                request.TargetCell.X,
                request.TargetCell.Y,
                ownerKind,
                ownerId,
                foodUnits,
                doorOpen,
                doorLocked);
        }

        // =============================================================================
        // MapOwnerKind
        // =============================================================================
        /// <summary>
        /// <para>
        /// Converte il valore owner della UI nel tipo Core usato dal comando.
        /// </para>
        /// </summary>
        private static OwnerKind MapOwnerKind(ArcUiPlacementConfig config)
        {
            if (config.OwnerKind == ArcUiPlacementOwnerKind.Npc && config.OwnerNpcId > 0)
                return OwnerKind.Npc;

            return OwnerKind.Community;
        }

        // =============================================================================
        // IsDoorDefId
        // =============================================================================
        /// <summary>
        /// <para>
        /// Riconosce le porte supportate dal bridge temporaneo.
        /// </para>
        /// </summary>
        private static bool IsDoorDefId(string defId)
        {
            return defId == "door_wood" || defId == "door_wood_locked";
        }
    }
}
