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
    ///   <item><b>TryPickObject</b>: cerca oggetti visibili sulla cella risolta usando il footprint.</item>
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
                if (!item.IsVisible || !IsActorHitCell(item.DiscreteCell, cell))
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
                if (!item.IsVisible || item.IsHeld || !IsObjectHitCell(item, cell))
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

        // =============================================================================
        // IsObjectHitCell
        // =============================================================================
        /// <summary>
        /// <para>
        /// Verifica se la cella puntata ricade dentro il footprint visuale/logico
        /// dell'oggetto ArcGraph.
        /// </para>
        ///
        /// <para><b>Principio architetturale: picking centralizzato nel boundary</b></para>
        /// <para>
        /// Muri, porte e oggetti estesi possono occupare piu' di una cella. Il
        /// boundary deve quindi usare il footprint gia' preparato nella render
        /// queue, invece di confrontare solo la cella origine. Questo mantiene la
        /// selezione UI come consumer passivo e impedisce la ricomparsa di un
        /// secondo hit test nei pannelli.
        /// </para>
        /// </summary>
        private static bool IsObjectHitCell(
            ArcGraphObjectRenderItem item,
            ArcGraphCellCoord pointerCell)
        {
            if (item.Cell.Z != pointerCell.Z)
                return false;

            return IsObjectLogicalFootprintHit(item, pointerCell)
                   || IsObjectVisualHeightHit(item, pointerCell);
        }

        // =============================================================================
        // IsObjectLogicalFootprintHit
        // =============================================================================
        /// <summary>
        /// <para>
        /// Verifica se la cella puntata ricade nel footprint logico dichiarato
        /// dall'oggetto.
        /// </para>
        /// </summary>
        private static bool IsObjectLogicalFootprintHit(
            ArcGraphObjectRenderItem item,
            ArcGraphCellCoord pointerCell)
        {
            int width = item.FootprintWidth <= 0 ? 1 : item.FootprintWidth;
            int height = item.FootprintHeight <= 0 ? 1 : item.FootprintHeight;

            return pointerCell.X >= item.Cell.X &&
                   pointerCell.X < item.Cell.X + width &&
                   pointerCell.Y >= item.Cell.Y &&
                   pointerCell.Y < item.Cell.Y + height;
        }

        // =============================================================================
        // IsObjectVisualHeightHit
        // =============================================================================
        /// <summary>
        /// <para>
        /// Verifica se la cella puntata ricade nella colonna visuale di uno sprite
        /// piu' alto del suo footprint logico.
        /// </para>
        ///
        /// <para><b>Principio architetturale: click su cio' che il player vede</b></para>
        /// <para>
        /// Alcuni oggetti, in particolare i muri 32x83 con pivot
        /// <c>bottom_center</c>, sono molto piu' alti della singola cella logica.
        /// Se il boundary usasse solo il footprint, un click sulla parte alta del
        /// muro verrebbe classificato come cella vuota. Questa hitbox visuale resta
        /// comunque read-only: usa solo metadati gia' presenti nella render queue e
        /// non consulta il <c>World</c>.
        /// </para>
        /// </summary>
        private static bool IsObjectVisualHeightHit(
            ArcGraphObjectRenderItem item,
            ArcGraphCellCoord pointerCell)
        {
            int logicalWidth = item.FootprintWidth <= 0 ? 1 : item.FootprintWidth;
            int logicalHeight = item.FootprintHeight <= 0 ? 1 : item.FootprintHeight;
            int visualHeightCells = ResolveVisualHeightCells(item, logicalHeight);

            if (visualHeightCells <= logicalHeight)
                return false;

            return pointerCell.X >= item.Cell.X &&
                   pointerCell.X < item.Cell.X + logicalWidth &&
                   pointerCell.Y >= item.Cell.Y &&
                   pointerCell.Y < item.Cell.Y + visualHeightCells;
        }

        // =============================================================================
        // ResolveVisualHeightCells
        // =============================================================================
        /// <summary>
        /// <para>
        /// Converte l'altezza pixel dello sprite in un numero intero di celle
        /// selezionabili lungo l'asse Y.
        /// </para>
        /// </summary>
        private static int ResolveVisualHeightCells(
            ArcGraphObjectRenderItem item,
            int logicalHeight)
        {
            int safeLogicalHeight = logicalHeight <= 0 ? 1 : logicalHeight;
            int visualHeightPixels = item.VisualHeightPixels > 0 ? item.VisualHeightPixels : 0;
            int baseHeightPixels = item.VisualBaseHeightPixels > 0 ? item.VisualBaseHeightPixels : 32;

            if (visualHeightPixels <= baseHeightPixels)
                return safeLogicalHeight;

            int visualCells = (visualHeightPixels + baseHeightPixels - 1) / baseHeightPixels;
            return visualCells > safeLogicalHeight ? visualCells : safeLogicalHeight;
        }

        // =============================================================================
        // IsActorHitCell
        // =============================================================================
        /// <summary>
        /// <para>
        /// Verifica se la cella puntata ricade sulla base o sulla parte superiore
        /// dello sprite actor.
        /// </para>
        ///
        /// <para><b>Principio architetturale: hitbox visuale, non collisione simulativa</b></para>
        /// <para>
        /// Gli NPC sono disegnati piu' alti di una singola cella. Questa tolleranza
        /// serve solo alla selection view-side: non modifica occupazione, movimento,
        /// pathfinding o stato fisico del World.
        /// </para>
        /// </summary>
        private static bool IsActorHitCell(ArcGraphCellCoord actorCell, ArcGraphCellCoord pointerCell)
        {
            if (actorCell.Z != pointerCell.Z)
                return false;

            if (actorCell.X != pointerCell.X)
                return false;

            return pointerCell.Y >= actorCell.Y && pointerCell.Y <= actorCell.Y + 1;
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
