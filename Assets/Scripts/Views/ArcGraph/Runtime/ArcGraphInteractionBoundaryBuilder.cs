using System.Collections.Generic;

namespace Arcontio.View.ArcGraph
{
    // =============================================================================
    // ArcGraphInteractionBoundaryBuilder
    // =============================================================================
    /// <summary>
    /// <para>
    /// Builder passivo che produce un <c>ArcGraphInteractionFrame</c> per pannelli,
    /// HUD, selection e strumenti debug futuri.
    /// </para>
    ///
    /// <para><b>Principio architetturale: ArcGraph come provider di interazione, non come tool host</b></para>
    /// <para>
    /// Il builder riceve input normalizzato, stato vista e queue visuale gia'
    /// prodotta. Risolve la cella sotto il puntatore e cerca actor/oggetti visibili
    /// sulla stessa cella. Non legge <c>World</c>, non usa <c>SimulationHost</c>, non
    /// invia comandi, non decide selection e non attiva DevTools. In questo modo
    /// ArcGraph puo' fornire un boundary stabile senza diventare un God Manager UI.
    /// </para>
    ///
    /// <para><b>Struttura interna:</b></para>
    /// <list type="bullet">
    ///   <item><b>Build</b>: produce frame e diagnostica da input/view/queue.</item>
    ///   <item><b>TryPickActor</b>: cerca actor visibili sulla cella risolta.</item>
    ///   <item><b>TryPickObject</b>: cerca oggetti visibili sulla cella risolta.</item>
    ///   <item><b>CreateFrame</b>: normalizza priorita' e reason finale.</item>
    /// </list>
    /// </summary>
    public sealed class ArcGraphInteractionBoundaryBuilder
    {
        public ArcGraphInteractionBoundaryDiagnostics LastDiagnostics { get; private set; }

        // =============================================================================
        // Build
        // =============================================================================
        /// <summary>
        /// <para>
        /// Costruisce un frame interazione per un singolo frame grafico.
        /// </para>
        ///
        /// <para><b>Priorita' semplice e dichiarata</b></para>
        /// <para>
        /// Se la UI blocca il puntatore, il risultato e' <c>UiBlocked</c>. Se non c'e'
        /// puntatore, il risultato e' vuoto. Se la cella e' valida, actor e object
        /// vengono entrambi raccolti, ma il target prioritario e' actor, poi object,
        /// poi cella. Un Tool Panel futuro potra' comunque leggere anche l'object id
        /// quando serve.
        /// </para>
        /// </summary>
        public ArcGraphInteractionFrame Build(
            ArcGraphMapViewConfig config,
            ArcGraphViewState viewState,
            ArcGraphViewInputFrame input,
            int viewportPixelWidth,
            int viewportPixelHeight,
            ArcGraphRenderQueue renderQueue)
        {
            return Build(
                config,
                viewState,
                input,
                viewportPixelWidth,
                viewportPixelHeight,
                renderQueue != null ? renderQueue.ActorItems : null,
                renderQueue != null ? renderQueue.ObjectItems : null);
        }

        // =============================================================================
        // Build
        // =============================================================================
        /// <summary>
        /// <para>
        /// Costruisce un frame interazione partendo da liste actor/object gia'
        /// preparate.
        /// </para>
        ///
        /// <para><b>Overload per harness e producer intermedi</b></para>
        /// <para>
        /// Questo overload evita che test e moduli intermedi debbano mutare
        /// direttamente una <c>ArcGraphRenderQueue</c>. Il contratto resta identico:
        /// input e item sono gia' stati prodotti altrove, il boundary li interpreta
        /// solo per picking view-side.
        /// </para>
        /// </summary>
        public ArcGraphInteractionFrame Build(
            ArcGraphMapViewConfig config,
            ArcGraphViewState viewState,
            ArcGraphViewInputFrame input,
            int viewportPixelWidth,
            int viewportPixelHeight,
            IReadOnlyList<ArcGraphActorRenderItem> actorItems,
            IReadOnlyList<ArcGraphObjectRenderItem> objectItems)
        {
            if (input.IsPointerOverUi)
                return StoreAndReturn(CreateUiBlockedFrame(input));

            if (!input.HasPointerScreenPosition)
                return StoreAndReturn(ArcGraphInteractionFrame.Empty("PointerMissing"));

            ArcGraphViewCoordinateResult coordinate =
                ArcGraphViewCoordinateMapper.ResolveCellFromViewportPoint(
                    config,
                    viewState,
                    input.PointerScreenX,
                    input.PointerScreenY,
                    viewportPixelWidth,
                    viewportPixelHeight);

            if (!coordinate.IsValid)
                return StoreAndReturn(CreateInvalidCoordinateFrame(input, coordinate));

            PickResult actorPick = TryPickActor(actorItems, coordinate.Cell);
            PickResult objectPick = TryPickObject(objectItems, coordinate.Cell);

            ArcGraphInteractionTargetKind kind = ResolveTargetKind(actorPick.EntityId, objectPick.EntityId);
            string reason = ResolveReason(kind);

            return StoreAndReturn(new ArcGraphInteractionFrame(
                input,
                coordinate,
                kind,
                coordinate.Cell,
                actorPick.EntityId,
                objectPick.EntityId,
                true,
                false,
                reason),
                actorPick.CandidateCount,
                objectPick.CandidateCount);
        }

        private ArcGraphInteractionFrame StoreAndReturn(
            ArcGraphInteractionFrame frame,
            int actorCandidateCount = 0,
            int objectCandidateCount = 0)
        {
            LastDiagnostics = new ArcGraphInteractionBoundaryDiagnostics(
                frame.Input.HasPointerScreenPosition,
                frame.IsPointerOverUi,
                frame.HasValidCell,
                actorCandidateCount,
                objectCandidateCount,
                frame.TargetKind,
                frame.Reason);

            return frame;
        }

        private static ArcGraphInteractionFrame CreateUiBlockedFrame(ArcGraphViewInputFrame input)
        {
            return new ArcGraphInteractionFrame(
                input,
                ArcGraphViewCoordinateResult.Invalid("PointerOverUi"),
                ArcGraphInteractionTargetKind.UiBlocked,
                new ArcGraphCellCoord(0, 0, 0),
                -1,
                -1,
                false,
                true,
                "PointerOverUi");
        }

        private static ArcGraphInteractionFrame CreateInvalidCoordinateFrame(
            ArcGraphViewInputFrame input,
            ArcGraphViewCoordinateResult coordinate)
        {
            return new ArcGraphInteractionFrame(
                input,
                coordinate,
                ArcGraphInteractionTargetKind.None,
                new ArcGraphCellCoord(0, 0, 0),
                -1,
                -1,
                false,
                false,
                coordinate.Reason);
        }

        private static PickResult TryPickActor(
            IReadOnlyList<ArcGraphActorRenderItem> actors,
            ArcGraphCellCoord cell)
        {
            if (actors == null || actors.Count == 0)
                return PickResult.Empty();

            int selectedActorId = -1;
            ArcGraphRenderSortKey selectedSortKey = default;
            int candidateCount = 0;
            bool hasSelected = false;

            for (int i = 0; i < actors.Count; i++)
            {
                ArcGraphActorRenderItem item = actors[i];
                if (!item.IsVisible || !IsSameCell(item.DiscreteCell, cell))
                    continue;

                candidateCount++;
                if (!hasSelected || item.SortKey.CompareTo(selectedSortKey) >= 0)
                {
                    selectedActorId = item.ActorId;
                    selectedSortKey = item.SortKey;
                    hasSelected = true;
                }
            }

            return new PickResult(selectedActorId, candidateCount);
        }

        private static PickResult TryPickObject(
            IReadOnlyList<ArcGraphObjectRenderItem> objects,
            ArcGraphCellCoord cell)
        {
            if (objects == null || objects.Count == 0)
                return PickResult.Empty();

            int selectedObjectId = -1;
            ArcGraphRenderSortKey selectedSortKey = default;
            int candidateCount = 0;
            bool hasSelected = false;

            for (int i = 0; i < objects.Count; i++)
            {
                ArcGraphObjectRenderItem item = objects[i];
                if (!item.IsVisible || !IsSameCell(item.Cell, cell))
                    continue;

                candidateCount++;
                if (!hasSelected || item.SortKey.CompareTo(selectedSortKey) >= 0)
                {
                    selectedObjectId = item.ObjectId;
                    selectedSortKey = item.SortKey;
                    hasSelected = true;
                }
            }

            return new PickResult(selectedObjectId, candidateCount);
        }

        private static bool IsSameCell(ArcGraphCellCoord left, ArcGraphCellCoord right)
        {
            return left.X == right.X && left.Y == right.Y && left.Z == right.Z;
        }

        private static ArcGraphInteractionTargetKind ResolveTargetKind(int actorId, int objectId)
        {
            if (actorId > 0)
                return ArcGraphInteractionTargetKind.Actor;

            if (objectId > 0)
                return ArcGraphInteractionTargetKind.Object;

            return ArcGraphInteractionTargetKind.Cell;
        }

        private static string ResolveReason(ArcGraphInteractionTargetKind kind)
        {
            switch (kind)
            {
                case ArcGraphInteractionTargetKind.Actor:
                    return "ActorPicked";
                case ArcGraphInteractionTargetKind.Object:
                    return "ObjectPicked";
                case ArcGraphInteractionTargetKind.Cell:
                    return "CellPicked";
                case ArcGraphInteractionTargetKind.UiBlocked:
                    return "PointerOverUi";
                default:
                    return "NoTarget";
            }
        }

        private readonly struct PickResult
        {
            public readonly int EntityId;
            public readonly int CandidateCount;

            public PickResult(int entityId, int candidateCount)
            {
                EntityId = entityId > 0 ? entityId : -1;
                CandidateCount = candidateCount < 0 ? 0 : candidateCount;
            }

            public static PickResult Empty()
            {
                return new PickResult(-1, 0);
            }
        }
    }
}
