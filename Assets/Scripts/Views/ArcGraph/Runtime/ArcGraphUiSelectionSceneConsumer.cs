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
    /// interattivo e render queue.
    /// </para>
    ///
    /// <para><b>Principio architetturale: selezione view-side senza World</b></para>
    /// <para>
    /// La diagnostica rende visibile se il consumer ha ricevuto un frame, se il
    /// click primario era presente, se il frame contiene una cella valida e quale
    /// target UI e' stato selezionato. Non contiene riferimenti mutabili a NPC,
    /// oggetti, job o world runtime.
    /// </para>
    ///
    /// <para><b>Struttura interna:</b></para>
    /// <list type="bullet">
    ///   <item><b>DidReceiveFrame</b>: il consumer ha ricevuto un frame interattivo.</item>
    ///   <item><b>SelectionEnabled</b>: gate locale del consumer.</item>
    ///   <item><b>WasPrimaryClick</b>: click sinistro nel frame corrente.</item>
    ///   <item><b>HasFrameCell</b>: cella gia' risolta dal boundary interattivo.</item>
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
        public readonly bool HasFrameCell;
        public readonly bool DidSelectTarget;
        public readonly ArcGraphCellCoord Cell;
        public readonly ArcGraphInteractionTargetKind FrameTargetKind;
        public readonly int FrameActorId;
        public readonly int FrameObjectId;
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
            bool hasFrameCell,
            bool didSelectTarget,
            ArcGraphCellCoord cell,
            ArcGraphInteractionTargetKind frameTargetKind,
            int frameActorId,
            int frameObjectId,
            ArcUiSelectionTarget selectedTarget,
            string reason)
        {
            DidReceiveFrame = didReceiveFrame;
            SelectionEnabled = selectionEnabled;
            WasPrimaryClick = wasPrimaryClick;
            WasPointerOverUi = wasPointerOverUi;
            HasFrameCell = hasFrameCell;
            DidSelectTarget = didSelectTarget;
            Cell = cell;
            FrameTargetKind = frameTargetKind;
            FrameActorId = frameActorId;
            FrameObjectId = frameObjectId;
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
                ArcGraphInteractionTargetKind.None,
                -1,
                -1,
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

            ArcGraphRenderQueue queue = ResolveRenderQueue();
            if (TrySelectFromInteractionFrame(interactionFrame, queue))
            {
                StoreDiagnostics(
                    interactionFrame,
                    interactionFrame.HasValidCell,
                    true,
                    interactionFrame.Cell,
                    "InteractionFrameTargetSelected");
                return;
            }

            StoreDiagnostics(
                interactionFrame,
                interactionFrame.HasValidCell,
                false,
                interactionFrame.Cell,
                "ClickWithoutSelectableFrameTarget");
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
                ", frameTarget=" + _lastDiagnostics.FrameTargetKind +
                ", frameActorId=" + _lastDiagnostics.FrameActorId +
                ", frameObjectId=" + _lastDiagnostics.FrameObjectId +
                ", frameCell=" + _lastDiagnostics.HasFrameCell +
                ", cell=" + _lastDiagnostics.Cell +
                ", selected=" + _lastDiagnostics.DidSelectTarget +
                ", targetKind=" + _lastDiagnostics.SelectedTarget.Kind +
                ", targetId=" + _lastDiagnostics.SelectedTarget.Id);
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

            LogSelectionIfEnabled("ActorFrameSelected", target);
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
            LogSelectionIfEnabled("ObjectFrameSelected", target);
        }

        private ArcGraphRenderQueue ResolveRenderQueue()
        {
            return _renderQueue;
        }

        private static bool IsWall(ArcGraphObjectRenderItem item)
        {
            return string.Equals(item.VisualKind, "wall", System.StringComparison.OrdinalIgnoreCase);
        }

        private void StoreDiagnostics(
            ArcGraphInteractionFrame interactionFrame,
            bool hasFrameCell,
            bool didSelectTarget,
            ArcGraphCellCoord cell,
            string reason)
        {
            _lastDiagnostics = new ArcGraphUiSelectionSceneConsumerDiagnostics(
                true,
                selectionEnabled,
                interactionFrame.Input.IsPrimaryPointerPressedThisFrame,
                interactionFrame.IsPointerOverUi,
                hasFrameCell,
                didSelectTarget,
                cell,
                interactionFrame.TargetKind,
                interactionFrame.ActorId,
                interactionFrame.ObjectId,
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
