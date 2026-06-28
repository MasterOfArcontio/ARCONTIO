using System.Collections.Generic;
using SocialViewer.UI;
using UnityEngine;

namespace Arcontio.View.ArcGraph
{
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
    /// Il componente riceve un frame interattivo gia' normalizzato dal boundary
    /// ArcGraph e trasforma il target actor/object contenuto nel frame in una
    /// selezione UI. Non ricalcola coordinate dalla camera, non fa un secondo
    /// picking, non legge il <c>World</c>, non modifica NPC, non modifica oggetti,
    /// non invia comandi e non apre inspector. Conserva solo una selezione UI
    /// read-only tramite <see cref="ArcUiSelectionController"/>.
    /// </para>
    ///
    /// <para><b>Struttura interna:</b></para>
    /// <list type="bullet">
    ///   <item><b>ConsumeInteractionFrame</b>: seleziona solo su click primario valido.</item>
    ///   <item><b>TrySelectFromInteractionFrame</b>: usa il target gia' risolto dal boundary.</item>
    ///   <item><b>SetRenderQueue</b>: riceve la queue solo per arricchire label e kind del target.</item>
    /// </list>
    /// </summary>
    public sealed class ArcGraphUiSelectionSceneConsumer : MonoBehaviour, IArcGraphInteractionFrameConsumer
    {
        [SerializeField] private bool selectionEnabled = true;
        [SerializeField] private bool mirrorNpcSelectionToLegacyService = true;

        private readonly ArcUiSelectionController _selectionController = new();
        private ArcGraphRenderQueue _renderQueue;
        private IReadOnlyList<ArcGraphVegetationRenderItem> _vegetationItems;

        public ArcUiSelectionTarget CurrentSelection => _selectionController.Current;
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
                return;

            if (interactionFrame.IsPointerOverUi)
                return;

            if (interactionFrame.Input.IsSecondaryPointerPressedThisFrame)
            {
                TryClearSelectionFromSecondaryClick(interactionFrame);
                return;
            }

            if (!interactionFrame.Input.IsPrimaryPointerPressedThisFrame)
                return;

            ArcGraphRenderQueue queue = ResolveRenderQueue();
            TrySelectFromInteractionFrame(interactionFrame, queue);
        }

        private void TryClearSelectionFromSecondaryClick(ArcGraphInteractionFrame interactionFrame)
        {
            if (!CurrentSelection.IsValid)
                return;

            ArcGraphRenderQueue queue = ResolveRenderQueue();
            bool hasSelectableTarget =
                interactionFrame.TargetKind == ArcGraphInteractionTargetKind.Actor ||
                interactionFrame.TargetKind == ArcGraphInteractionTargetKind.Object ||
                interactionFrame.TargetKind == ArcGraphInteractionTargetKind.Plant ||
                (interactionFrame.HasValidCell &&
                    (TryFindActorByCell(queue, interactionFrame.Cell, out _) ||
                     TryFindObjectByCell(queue, interactionFrame.Cell, out _) ||
                     TryFindPlantByCell(_vegetationItems, interactionFrame.Cell, out _)));

            if (hasSelectableTarget)
                ClearSelection();
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
        // SetVegetationItems
        // =============================================================================
        /// <summary>
        /// <para>
        /// Assegna gli item vegetazione ArcGraph correnti per arricchire la
        /// selezione delle piante fisiche.
        /// </para>
        /// </summary>
        public void SetVegetationItems(IReadOnlyList<ArcGraphVegetationRenderItem> vegetationItems)
        {
            _vegetationItems = vegetationItems;
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
        // TrySelectFromInteractionFrame
        // =============================================================================
        /// <summary>
        /// <para>
        /// Usa il target gia' risolto dal boundary interattivo come sorgente
        /// primaria della selezione UI.
        /// </para>
        ///
        /// <para><b>Principio architetturale: un solo boundary di picking</b></para>
        /// <para>
        /// L'interaction frame e' gia' il risultato autorizzato di input, viewport,
        /// view state e render queue. La selezione UI deve preferirlo, evitando un
        /// secondo hit test basato sulla camera che puo' divergere dopo pan, zoom o
        /// viewport UI. Se il frame contiene solo id ma la queue non e' disponibile,
        /// creiamo comunque un target minimale: resta read-only e non abilita
        /// mutazioni dirette.
        /// </para>
        /// </summary>
        private bool TrySelectFromInteractionFrame(
            ArcGraphInteractionFrame interactionFrame,
            ArcGraphRenderQueue queue)
        {
            if (interactionFrame.TargetKind == ArcGraphInteractionTargetKind.Actor &&
                interactionFrame.HasActor)
            {
                if (TryFindActorById(queue, interactionFrame.ActorId, out ArcGraphActorRenderItem actor))
                    SelectActor(actor);
                else
                    SelectActorId(interactionFrame.ActorId, interactionFrame.Cell);

                return true;
            }

            if (interactionFrame.TargetKind == ArcGraphInteractionTargetKind.Object &&
                interactionFrame.HasObject)
            {
                if (TryFindObjectById(queue, interactionFrame.ObjectId, out ArcGraphObjectRenderItem obj))
                    SelectObject(obj, interactionFrame.Cell);
                else
                    SelectObjectId(interactionFrame.ObjectId, interactionFrame.Cell);

                return true;
            }

            if (interactionFrame.TargetKind == ArcGraphInteractionTargetKind.Plant &&
                interactionFrame.HasPlant)
            {
                if (TryFindPlantById(_vegetationItems, interactionFrame.PlantId, out ArcGraphVegetationRenderItem plant))
                    SelectPlant(plant);
                else
                    SelectPlantId(interactionFrame.PlantId, interactionFrame.Cell);

                return true;
            }

            if (interactionFrame.HasValidCell &&
                TryFindActorByCell(queue, interactionFrame.Cell, out ArcGraphActorRenderItem actorInCell))
            {
                SelectActor(actorInCell);
                return true;
            }

            if (interactionFrame.HasValidCell &&
                TryFindObjectByCell(queue, interactionFrame.Cell, out ArcGraphObjectRenderItem objectInCell))
            {
                SelectObject(objectInCell, interactionFrame.Cell);
                return true;
            }

            if (interactionFrame.HasValidCell &&
                TryFindPlantByCell(_vegetationItems, interactionFrame.Cell, out ArcGraphVegetationRenderItem plantInCell))
            {
                SelectPlant(plantInCell);
                return true;
            }

            return false;
        }

        // =============================================================================
        // TryFindActorById
        // =============================================================================
        /// <summary>
        /// <para>
        /// Cerca nella render queue l'actor indicato dal frame interattivo.
        /// </para>
        /// </summary>
        private static bool TryFindActorById(
            ArcGraphRenderQueue queue,
            int actorId,
            out ArcGraphActorRenderItem selected)
        {
            selected = default;

            if (queue == null || queue.ActorItems == null || actorId <= 0)
                return false;

            for (int i = 0; i < queue.ActorItems.Count; i++)
            {
                ArcGraphActorRenderItem item = queue.ActorItems[i];
                if (item.ActorId == actorId && item.IsVisible)
                {
                    selected = item;
                    return true;
                }
            }

            return false;
        }

        // =============================================================================
        // TryFindObjectById
        // =============================================================================
        /// <summary>
        /// <para>
        /// Cerca nella render queue l'oggetto indicato dal frame interattivo.
        /// </para>
        /// </summary>
        private static bool TryFindObjectById(
            ArcGraphRenderQueue queue,
            int objectId,
            out ArcGraphObjectRenderItem selected)
        {
            selected = default;

            if (queue == null || queue.ObjectItems == null || objectId <= 0)
                return false;

            for (int i = 0; i < queue.ObjectItems.Count; i++)
            {
                ArcGraphObjectRenderItem item = queue.ObjectItems[i];
                if (item.ObjectId == objectId && item.IsVisible && !item.IsHeld)
                {
                    selected = item;
                    return true;
                }
            }

            return false;
        }

        // =============================================================================
        // TryFindPlantById
        // =============================================================================
        /// <summary>
        /// <para>
        /// Cerca una pianta fisica selezionabile nella lista vegetazione derivata.
        /// </para>
        /// </summary>
        private static bool TryFindPlantById(
            IReadOnlyList<ArcGraphVegetationRenderItem> vegetationItems,
            int plantId,
            out ArcGraphVegetationRenderItem selected)
        {
            selected = default;

            if (vegetationItems == null || plantId <= 0)
                return false;

            for (int i = 0; i < vegetationItems.Count; i++)
            {
                ArcGraphVegetationRenderItem item = vegetationItems[i];
                if (item.IsVisible && item.IsPhysicalPlant && item.PlantId == plantId)
                {
                    selected = item;
                    return true;
                }
            }

            return false;
        }

        // =============================================================================
        // TryFindActorByCell
        // =============================================================================
        /// <summary>
        /// <para>
        /// Cerca un NPC visibile nella cella gia' risolta dal frame interattivo.
        /// </para>
        ///
        /// <para><b>Principio architetturale: fallback sul boundary cella</b></para>
        /// <para>
        /// La pipeline interattiva ArcGraph produce <c>interactionFrame.Cell</c>
        /// come cella autorevole sotto il puntatore. Se il target prioritario non
        /// arriva gia' come <c>Actor</c>, il controller puo' usare la stessa cella
        /// per interrogare la render queue view-side, senza leggere il <c>World</c>.
        /// </para>
        /// </summary>
        private static bool TryFindActorByCell(
            ArcGraphRenderQueue queue,
            ArcGraphCellCoord cell,
            out ArcGraphActorRenderItem selected)
        {
            selected = default;

            if (queue == null || queue.ActorItems == null)
                return false;

            for (int i = 0; i < queue.ActorItems.Count; i++)
            {
                ArcGraphActorRenderItem item = queue.ActorItems[i];
                if (!item.IsVisible)
                    continue;

                if (item.DiscreteCell.Z == cell.Z &&
                    item.DiscreteCell.X == cell.X &&
                    item.DiscreteCell.Y == cell.Y)
                {
                    selected = item;
                    return true;
                }
            }

            return false;
        }

        // =============================================================================
        // TryFindObjectByCell
        // =============================================================================
        /// <summary>
        /// <para>
        /// Cerca un oggetto visibile nella cella gia' risolta dal frame interattivo.
        /// </para>
        /// </summary>
        private static bool TryFindObjectByCell(
            ArcGraphRenderQueue queue,
            ArcGraphCellCoord cell,
            out ArcGraphObjectRenderItem selected)
        {
            selected = default;

            if (queue == null || queue.ObjectItems == null)
                return false;

            for (int i = 0; i < queue.ObjectItems.Count; i++)
            {
                ArcGraphObjectRenderItem item = queue.ObjectItems[i];
                if (!item.IsVisible || item.IsHeld)
                    continue;

                if (item.Cell.Z == cell.Z &&
                    cell.X >= item.Cell.X &&
                    cell.X < item.Cell.X + item.FootprintWidth &&
                    cell.Y >= item.Cell.Y &&
                    cell.Y < item.Cell.Y + item.FootprintHeight)
                {
                    selected = item;
                    return true;
                }
            }

            return false;
        }

        // =============================================================================
        // TryFindPlantByCell
        // =============================================================================
        /// <summary>
        /// <para>
        /// Cerca una pianta fisica visibile sulla cella gia' risolta dal boundary.
        /// </para>
        /// </summary>
        private static bool TryFindPlantByCell(
            IReadOnlyList<ArcGraphVegetationRenderItem> vegetationItems,
            ArcGraphCellCoord cell,
            out ArcGraphVegetationRenderItem selected)
        {
            selected = default;

            if (vegetationItems == null)
                return false;

            for (int i = 0; i < vegetationItems.Count; i++)
            {
                ArcGraphVegetationRenderItem item = vegetationItems[i];
                if (!item.IsVisible || !item.IsPhysicalPlant)
                    continue;

                if (item.Cell.Z == cell.Z && item.Cell.X == cell.X && item.Cell.Y == cell.Y)
                {
                    selected = item;
                    return true;
                }
            }

            return false;
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

        }

        // =============================================================================
        // SelectActorId
        // =============================================================================
        /// <summary>
        /// <para>
        /// Crea una selezione NPC minimale quando il frame contiene l'id ma la queue
        /// non permette di recuperare l'item completo.
        /// </para>
        /// </summary>
        private void SelectActorId(
            int actorId,
            ArcGraphCellCoord cell)
        {
            var target = new ArcUiSelectionTarget(
                ArcUiSelectionTargetKind.Npc,
                actorId.ToString(),
                cell,
                "NPC " + actorId,
                "ArcGraphUiSelectionSceneConsumer");

            _selectionController.Select(target);

            if (mirrorNpcSelectionToLegacyService)
                NPCSelection.Select(actorId);

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
        }

        // =============================================================================
        // SelectObjectId
        // =============================================================================
        /// <summary>
        /// <para>
        /// Crea una selezione oggetto minimale quando il frame contiene l'id ma la
        /// queue non permette di recuperare defId o kind visuale.
        /// </para>
        /// </summary>
        private void SelectObjectId(
            int objectId,
            ArcGraphCellCoord cell)
        {
            var target = new ArcUiSelectionTarget(
                ArcUiSelectionTargetKind.Object,
                objectId.ToString(),
                cell,
                "Oggetto " + objectId,
                "ArcGraphUiSelectionSceneConsumer");

            _selectionController.Select(target);
        }

        // =============================================================================
        // SelectPlant
        // =============================================================================
        /// <summary>
        /// <para>
        /// Converte un item pianta fisica in selection target UI read-only.
        /// </para>
        /// </summary>
        private void SelectPlant(ArcGraphVegetationRenderItem plant)
        {
            var target = new ArcUiSelectionTarget(
                ArcUiSelectionTargetKind.Plant,
                plant.PlantId.ToString(),
                plant.Cell,
                "Pianta #" + plant.PlantId,
                "ArcGraphUiSelectionSceneConsumer");

            _selectionController.Select(target);
        }

        // =============================================================================
        // SelectPlantId
        // =============================================================================
        /// <summary>
        /// <para>
        /// Crea una selezione pianta minimale quando il frame contiene l'id ma la
        /// lista vegetazione corrente non permette di recuperare l'item completo.
        /// </para>
        /// </summary>
        private void SelectPlantId(
            int plantId,
            ArcGraphCellCoord cell)
        {
            var target = new ArcUiSelectionTarget(
                ArcUiSelectionTargetKind.Plant,
                plantId.ToString(),
                cell,
                "Pianta #" + plantId,
                "ArcGraphUiSelectionSceneConsumer");

            _selectionController.Select(target);
        }

        private ArcGraphRenderQueue ResolveRenderQueue()
        {
            return _renderQueue;
        }

        private static bool IsWall(ArcGraphObjectRenderItem item)
        {
            return string.Equals(item.VisualKind, "wall", System.StringComparison.OrdinalIgnoreCase);
        }

    }
}
