using SocialViewer.UI;
using UnityEngine;

namespace Arcontio.View.ArcGraph
{
    // =============================================================================
    // ArcGraphUiSelectionSceneConsumerDiagnostics
    // =============================================================================
    /// <summary>
    /// <para>
    /// Diagnostica sintetica della selezione UI ArcGraph basata su frame
    /// interattivo, camera e render queue.
    /// </para>
    ///
    /// <para><b>Principio architetturale: selezione view-side senza World</b></para>
    /// <para>
    /// La diagnostica rende visibile se il consumer ha ricevuto un frame, se il
    /// click primario era presente, se la cella camera-aligned e' stata risolta e
    /// quale target UI e' stato selezionato. Non contiene riferimenti mutabili a NPC,
    /// oggetti, job o world runtime.
    /// </para>
    ///
    /// <para><b>Struttura interna:</b></para>
    /// <list type="bullet">
    ///   <item><b>DidReceiveFrame</b>: il consumer ha ricevuto un frame interattivo.</item>
    ///   <item><b>SelectionEnabled</b>: gate locale del consumer.</item>
    ///   <item><b>WasPrimaryClick</b>: click sinistro nel frame corrente.</item>
    ///   <item><b>HasCameraCell</b>: cella risolta dalla camera reale.</item>
    ///   <item><b>DidSelectTarget</b>: selezione UI aggiornata.</item>
    ///   <item><b>SelectedTarget</b>: target selezionato in formato ArcUiSelectionTarget.</item>
    ///   <item><b>Reason</b>: motivo sintetico dell'esito.</item>
    /// </list>
    /// </summary>
    public readonly struct ArcGraphUiSelectionSceneConsumerDiagnostics
    {
        public readonly bool DidReceiveFrame;
        public readonly bool SelectionEnabled;
        public readonly bool WasPrimaryClick;
        public readonly bool WasPointerOverUi;
        public readonly bool HasCameraCell;
        public readonly bool DidSelectTarget;
        public readonly ArcGraphCellCoord Cell;
        public readonly ArcUiSelectionTarget SelectedTarget;
        public readonly string Reason;

        // =============================================================================
        // ArcGraphUiSelectionSceneConsumerDiagnostics
        // =============================================================================
        /// <summary>
        /// <para>
        /// Costruisce una diagnostica immutabile per l'ultimo frame di selezione.
        /// </para>
        /// </summary>
        public ArcGraphUiSelectionSceneConsumerDiagnostics(
            bool didReceiveFrame,
            bool selectionEnabled,
            bool wasPrimaryClick,
            bool wasPointerOverUi,
            bool hasCameraCell,
            bool didSelectTarget,
            ArcGraphCellCoord cell,
            ArcUiSelectionTarget selectedTarget,
            string reason)
        {
            DidReceiveFrame = didReceiveFrame;
            SelectionEnabled = selectionEnabled;
            WasPrimaryClick = wasPrimaryClick;
            WasPointerOverUi = wasPointerOverUi;
            HasCameraCell = hasCameraCell;
            DidSelectTarget = didSelectTarget;
            Cell = cell;
            SelectedTarget = selectedTarget;
            Reason = string.IsNullOrWhiteSpace(reason) ? "None" : reason;
        }
    }

    // =============================================================================
    // ArcGraphUiSelectionSceneConsumer
    // =============================================================================
    /// <summary>
    /// <para>
    /// Consumer scena che trasforma click su NPC o oggetti ArcGraph in
    /// <see cref="ArcUiSelectionTarget"/>.
    /// </para>
    ///
    /// <para><b>Principio architetturale: UI selection da snapshot visuale</b></para>
    /// <para>
    /// Il componente riceve un frame interattivo gia' normalizzato, risolve la cella
    /// reale sotto il puntatore tramite la camera e cerca nella render queue ArcGraph
    /// il target visibile piu' adatto. Non legge il <c>World</c>, non modifica NPC,
    /// non modifica oggetti, non invia comandi e non apre inspector. Conserva solo
    /// una selezione UI read-only tramite <see cref="ArcUiSelectionController"/>.
    /// </para>
    ///
    /// <para><b>Struttura interna:</b></para>
    /// <list type="bullet">
    ///   <item><b>ConsumeInteractionFrame</b>: seleziona solo su click primario valido.</item>
    ///   <item><b>SetRuntimeWrapper</b>: riceve la render queue gia' costruita da ArcGraph.</item>
    ///   <item><b>SetSceneCamera</b>: riceve la camera reale per evitare sfasamenti da zoom/pan.</item>
    ///   <item><b>TryPickActor</b>: priorita' agli NPC visibili.</item>
    ///   <item><b>TryPickObject</b>: fallback su oggetti, muri e porte visibili.</item>
    /// </list>
    /// </summary>
    public sealed class ArcGraphUiSelectionSceneConsumer : MonoBehaviour, IArcGraphInteractionFrameConsumer
    {
        [SerializeField] private bool selectionEnabled = true;
        [SerializeField] private Camera sceneCamera;
        [SerializeField] private bool mirrorNpcSelectionToLegacyService = true;
        [SerializeField] private float tileWorldSize = 1f;
        [SerializeField] private Vector3 originOffset = Vector3.zero;
        [SerializeField] private bool logSelectionEvents;

        private readonly ArcUiSelectionController _selectionController = new();
        private ArcGraphRenderQueue _renderQueue;
        private ArcGraphUiSelectionSceneConsumerDiagnostics _lastDiagnostics =
            new ArcGraphUiSelectionSceneConsumerDiagnostics(
                false,
                false,
                false,
                false,
                false,
                false,
                new ArcGraphCellCoord(0, 0, 0),
                ArcUiSelectionTarget.None("arcgraph_ui_selection"),
                "NotInitialized");

        public ArcUiSelectionTarget CurrentSelection => _selectionController.Current;
        public ArcGraphUiSelectionSceneConsumerDiagnostics LastDiagnostics => _lastDiagnostics;
        public bool SelectionEnabled => selectionEnabled;

        // =============================================================================
        // ConsumeInteractionFrame
        // =============================================================================
        /// <summary>
        /// <para>
        /// Consuma il frame interattivo e aggiorna la selezione solo su click
        /// primario sopra NPC o oggetto visibile.
        /// </para>
        ///
        /// <para><b>Click come intenzione, non come comando</b></para>
        /// <para>
        /// Il click produce soltanto un <see cref="ArcUiSelectionTarget"/> locale.
        /// La modifica, eliminazione o apertura inspector verranno gestite da
        /// controller successivi e dovranno passare dai gateway autorizzati.
        /// </para>
        /// </summary>
        public void ConsumeInteractionFrame(
            ArcGraphInteractionFrame interactionFrame,
            ArcGraphInteractionSceneAdapterDiagnostics diagnostics)
        {
            if (!selectionEnabled)
            {
                StoreDiagnostics(interactionFrame, false, false, interactionFrame.Cell, "SelectionDisabled");
                return;
            }

            if (interactionFrame.IsPointerOverUi)
            {
                StoreDiagnostics(interactionFrame, false, false, interactionFrame.Cell, "PointerOverUi");
                return;
            }

            if (!interactionFrame.Input.IsPrimaryPointerPressedThisFrame)
            {
                StoreDiagnostics(interactionFrame, false, false, interactionFrame.Cell, "WaitingForPrimaryClick");
                return;
            }

            if (!TryResolveCellFromSceneCamera(interactionFrame, out ArcGraphCellCoord cell, out string cellReason))
            {
                StoreDiagnostics(interactionFrame, false, false, interactionFrame.Cell, cellReason);
                return;
            }

            ArcGraphRenderQueue queue = ResolveRenderQueue();
            if (queue == null)
            {
                StoreDiagnostics(interactionFrame, true, false, cell, "RenderQueueMissing");
                return;
            }

            if (TryPickActor(queue, cell, out ArcGraphActorRenderItem actor))
            {
                SelectActor(actor);
                StoreDiagnostics(interactionFrame, true, true, cell, "ActorSelected");
                return;
            }

            if (TryPickObject(queue, cell, out ArcGraphObjectRenderItem obj))
            {
                SelectObject(obj, cell);
                StoreDiagnostics(interactionFrame, true, true, cell, "ObjectSelected");
                return;
            }

            StoreDiagnostics(interactionFrame, true, false, cell, "ClickWithoutSelectableTarget");
        }

        // =============================================================================
        // SetSelectionEnabled
        // =============================================================================
        /// <summary>
        /// <para>
        /// Abilita o disabilita la selezione UI ArcGraph.
        /// </para>
        /// </summary>
        public void SetSelectionEnabled(bool enabled)
        {
            selectionEnabled = enabled;
        }

        // =============================================================================
        // SetSceneCamera
        // =============================================================================
        /// <summary>
        /// <para>
        /// Assegna la camera usata per convertire il puntatore in cella reale.
        /// </para>
        /// </summary>
        public void SetSceneCamera(Camera camera)
        {
            sceneCamera = camera;
        }

        // =============================================================================
        // SetRenderQueue
        // =============================================================================
        /// <summary>
        /// <para>
        /// Assegna la render queue ArcGraph corrente letta in sola osservazione.
        /// </para>
        /// </summary>
        public void SetRenderQueue(ArcGraphRenderQueue renderQueue)
        {
            _renderQueue = renderQueue;
        }

        // =============================================================================
        // ClearSelection
        // =============================================================================
        /// <summary>
        /// <para>
        /// Cancella la selezione UI locale senza modificare il mondo.
        /// </para>
        /// </summary>
        [ContextMenu("ArcGraph/Clear UI Selection")]
        public void ClearSelection()
        {
            _selectionController.Clear();

            if (mirrorNpcSelectionToLegacyService)
                NPCSelection.Clear();
        }

        // =============================================================================
        // LogSelectionDiagnosticsFromInspector
        // =============================================================================
        /// <summary>
        /// <para>
        /// Stampa in Console l'ultima diagnostica della selezione UI.
        /// </para>
        /// </summary>
        [ContextMenu("ArcGraph/Log UI Selection Diagnostics")]
        public void LogSelectionDiagnosticsFromInspector()
        {
            Debug.Log(
                "[ArcGraphUiSelectionSceneConsumer] " +
                _lastDiagnostics.Reason +
                ", enabled=" + _lastDiagnostics.SelectionEnabled +
                ", click=" + _lastDiagnostics.WasPrimaryClick +
                ", pointerOverUi=" + _lastDiagnostics.WasPointerOverUi +
                ", cameraCell=" + _lastDiagnostics.HasCameraCell +
                ", cell=" + _lastDiagnostics.Cell +
                ", selected=" + _lastDiagnostics.DidSelectTarget +
                ", targetKind=" + _lastDiagnostics.SelectedTarget.Kind +
                ", targetId=" + _lastDiagnostics.SelectedTarget.Id);
        }

        // =============================================================================
        // TryPickActor
        // =============================================================================
        /// <summary>
        /// <para>
        /// Cerca l'actor visibile piu' alto in sorting sulla cella puntata.
        /// </para>
        /// </summary>
        private static bool TryPickActor(
            ArcGraphRenderQueue queue,
            ArcGraphCellCoord cell,
            out ArcGraphActorRenderItem selected)
        {
            selected = default;

            if (queue == null || queue.ActorItems == null || queue.ActorItems.Count == 0)
                return false;

            bool hasSelected = false;
            ArcGraphRenderSortKey selectedSortKey = default;

            for (int i = 0; i < queue.ActorItems.Count; i++)
            {
                ArcGraphActorRenderItem item = queue.ActorItems[i];
                if (!item.IsVisible || !IsActorHitCell(item.DiscreteCell, cell))
                    continue;

                if (!hasSelected || item.SortKey.CompareTo(selectedSortKey) >= 0)
                {
                    selected = item;
                    selectedSortKey = item.SortKey;
                    hasSelected = true;
                }
            }

            return hasSelected;
        }

        // =============================================================================
        // TryPickObject
        // =============================================================================
        /// <summary>
        /// <para>
        /// Cerca l'oggetto visibile piu' alto in sorting sulla cella puntata.
        /// </para>
        /// </summary>
        private static bool TryPickObject(
            ArcGraphRenderQueue queue,
            ArcGraphCellCoord cell,
            out ArcGraphObjectRenderItem selected)
        {
            selected = default;

            if (queue == null || queue.ObjectItems == null || queue.ObjectItems.Count == 0)
                return false;

            bool hasSelected = false;
            ArcGraphRenderSortKey selectedSortKey = default;

            for (int i = 0; i < queue.ObjectItems.Count; i++)
            {
                ArcGraphObjectRenderItem item = queue.ObjectItems[i];
                if (!item.IsVisible || item.IsHeld || !IsObjectHitCell(item, cell))
                    continue;

                if (!hasSelected || item.SortKey.CompareTo(selectedSortKey) >= 0)
                {
                    selected = item;
                    selectedSortKey = item.SortKey;
                    hasSelected = true;
                }
            }

            return hasSelected;
        }

        // =============================================================================
        // SelectActor
        // =============================================================================
        /// <summary>
        /// <para>
        /// Converte un actor render item in selection target UI.
        /// </para>
        /// </summary>
        private void SelectActor(ArcGraphActorRenderItem actor)
        {
            var target = new ArcUiSelectionTarget(
                ArcUiSelectionTargetKind.Npc,
                actor.ActorId.ToString(),
                actor.DiscreteCell,
                "NPC " + actor.ActorId,
                "ArcGraphUiSelectionSceneConsumer");

            _selectionController.Select(target);

            if (mirrorNpcSelectionToLegacyService)
                NPCSelection.Select(actor.ActorId);

            LogSelectionIfEnabled("ActorSelected", target);
        }

        // =============================================================================
        // SelectObject
        // =============================================================================
        /// <summary>
        /// <para>
        /// Converte un object render item in selection target UI.
        /// </para>
        /// </summary>
        private void SelectObject(
            ArcGraphObjectRenderItem obj,
            ArcGraphCellCoord pointerCell)
        {
            ArcUiSelectionTargetKind targetKind = IsWall(obj)
                ? ArcUiSelectionTargetKind.Wall
                : ArcUiSelectionTargetKind.Object;

            string label = string.IsNullOrWhiteSpace(obj.DefId)
                ? "Oggetto " + obj.ObjectId
                : obj.DefId.Trim() + " #" + obj.ObjectId;

            var target = new ArcUiSelectionTarget(
                targetKind,
                obj.ObjectId.ToString(),
                pointerCell,
                label,
                "ArcGraphUiSelectionSceneConsumer");

            _selectionController.Select(target);
            LogSelectionIfEnabled("ObjectSelected", target);
        }

        private ArcGraphRenderQueue ResolveRenderQueue()
        {
            return _renderQueue;
        }

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
            reason = "SceneCameraCellResolved";
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

        private static bool IsActorHitCell(
            ArcGraphCellCoord actorCell,
            ArcGraphCellCoord pointerCell)
        {
            if (actorCell.Z != pointerCell.Z)
                return false;

            if (actorCell.X != pointerCell.X)
                return false;

            return pointerCell.Y >= actorCell.Y && pointerCell.Y <= actorCell.Y + 1;
        }

        private static bool IsObjectHitCell(
            ArcGraphObjectRenderItem item,
            ArcGraphCellCoord pointerCell)
        {
            if (item.Cell.Z != pointerCell.Z)
                return false;

            int width = item.FootprintWidth <= 0 ? 1 : item.FootprintWidth;
            int height = item.FootprintHeight <= 0 ? 1 : item.FootprintHeight;

            return pointerCell.X >= item.Cell.X &&
                   pointerCell.X < item.Cell.X + width &&
                   pointerCell.Y >= item.Cell.Y &&
                   pointerCell.Y < item.Cell.Y + height;
        }

        private static bool IsWall(ArcGraphObjectRenderItem item)
        {
            return string.Equals(item.VisualKind, "wall", System.StringComparison.OrdinalIgnoreCase);
        }

        private void StoreDiagnostics(
            ArcGraphInteractionFrame interactionFrame,
            bool hasCameraCell,
            bool didSelectTarget,
            ArcGraphCellCoord cell,
            string reason)
        {
            _lastDiagnostics = new ArcGraphUiSelectionSceneConsumerDiagnostics(
                true,
                selectionEnabled,
                interactionFrame.Input.IsPrimaryPointerPressedThisFrame,
                interactionFrame.IsPointerOverUi,
                hasCameraCell,
                didSelectTarget,
                cell,
                _selectionController.Current,
                reason);
        }

        private void LogSelectionIfEnabled(
            string reason,
            ArcUiSelectionTarget target)
        {
            if (!logSelectionEvents)
                return;

            Debug.Log(
                "[ArcGraphUiSelectionSceneConsumer] " +
                reason +
                ", targetKind=" + target.Kind +
                ", targetId=" + target.Id +
                ", cell=" + target.Cell);
        }
    }
}
