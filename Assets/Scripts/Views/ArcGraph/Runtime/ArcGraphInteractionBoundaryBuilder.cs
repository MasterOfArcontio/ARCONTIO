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
    ///   <item><b>TryPickPlant</b>: cerca piante fisiche visuali sulla cella risolta.</item>
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
                renderQueue != null ? renderQueue.ObjectItems : null,
                null);
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
            return Build(
                config,
                viewState,
                input,
                viewportPixelWidth,
                viewportPixelHeight,
                actorItems,
                objectItems,
                null);
        }

        // =============================================================================
        // Build
        // =============================================================================
        /// <summary>
        /// <para>
        /// Costruisce un frame interazione partendo da liste actor/object/pianta gia'
        /// preparate dal percorso visuale ArcGraph.
        /// </para>
        ///
        /// <para><b>Boundary unico di picking</b></para>
        /// <para>
        /// Le piante fisiche entrano nel picking come dati visuali derivati, non
        /// come oggetti World. Il boundary mantiene una priorita' esplicita:
        /// actor, poi oggetto, poi pianta fisica, poi cella.
        /// </para>
        /// </summary>
        public ArcGraphInteractionFrame Build(
            ArcGraphMapViewConfig config,
            ArcGraphViewState viewState,
            ArcGraphViewInputFrame input,
            int viewportPixelWidth,
            int viewportPixelHeight,
            IReadOnlyList<ArcGraphActorRenderItem> actorItems,
            IReadOnlyList<ArcGraphObjectRenderItem> objectItems,
            IReadOnlyList<ArcGraphVegetationRenderItem> vegetationItems)
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

            return BuildFromCoordinate(
                input,
                coordinate,
                actorItems,
                objectItems,
                vegetationItems);
        }

        // =============================================================================
        // BuildFromResolvedCell
        // =============================================================================
        /// <summary>
        /// <para>
        /// Costruisce un frame interazione usando una cella gia' risolta dal wrapper
        /// scena.
        /// </para>
        ///
        /// <para><b>Principio architetturale: runtime camera come sorgente coordinate</b></para>
        /// <para>
        /// Il mapper normalizzato resta utile per harness e fallback, ma nel runtime
        /// ArcGraph la camera puo' essere stata spostata o zoomata fisicamente. In
        /// quel caso la cella autorevole e' quella ottenuta dal wrapper Unity tramite
        /// camera reale; il boundary continua comunque a essere il solo punto che
        /// decide actor/object/cella.
        /// </para>
        /// </summary>
        public ArcGraphInteractionFrame BuildFromResolvedCell(
            ArcGraphMapViewConfig config,
            ArcGraphViewState viewState,
            ArcGraphViewInputFrame input,
            int viewportPixelWidth,
            int viewportPixelHeight,
            IReadOnlyList<ArcGraphActorRenderItem> actorItems,
            IReadOnlyList<ArcGraphObjectRenderItem> objectItems,
            ArcGraphCellCoord resolvedCell)
        {
            return BuildFromResolvedCell(
                config,
                viewState,
                input,
                viewportPixelWidth,
                viewportPixelHeight,
                actorItems,
                objectItems,
                null,
                resolvedCell);
        }

        // =============================================================================
        // BuildFromResolvedCell
        // =============================================================================
        /// <summary>
        /// <para>
        /// Costruisce un frame interazione usando una cella scena gia' risolta e
        /// includendo le piante fisiche nella stessa decisione di picking.
        /// </para>
        /// </summary>
        public ArcGraphInteractionFrame BuildFromResolvedCell(
            ArcGraphMapViewConfig config,
            ArcGraphViewState viewState,
            ArcGraphViewInputFrame input,
            int viewportPixelWidth,
            int viewportPixelHeight,
            IReadOnlyList<ArcGraphActorRenderItem> actorItems,
            IReadOnlyList<ArcGraphObjectRenderItem> objectItems,
            IReadOnlyList<ArcGraphVegetationRenderItem> vegetationItems,
            ArcGraphCellCoord resolvedCell)
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

            ArcGraphViewCoordinateResult resolvedCoordinate = new ArcGraphViewCoordinateResult(
                true,
                resolvedCell,
                coordinate.IsValid ? coordinate.NormalizedX : 0f,
                coordinate.IsValid ? coordinate.NormalizedY : 0f,
                coordinate.IsValid ? coordinate.VisibleRect : new ArcGraphViewCellRect(0, 0, 0, 0),
                "SceneCameraCell");

            return BuildFromCoordinate(
                input,
                resolvedCoordinate,
                actorItems,
                objectItems,
                vegetationItems);
        }

        private ArcGraphInteractionFrame BuildFromCoordinate(
            ArcGraphViewInputFrame input,
            ArcGraphViewCoordinateResult coordinate,
            IReadOnlyList<ArcGraphActorRenderItem> actorItems,
            IReadOnlyList<ArcGraphObjectRenderItem> objectItems,
            IReadOnlyList<ArcGraphVegetationRenderItem> vegetationItems)
        {
            PickResult actorPick = TryPickActor(actorItems, coordinate.Cell);
            PickResult objectPick = TryPickObject(objectItems, coordinate.Cell);
            PickResult plantPick = TryPickPlant(vegetationItems, coordinate.Cell);

            ArcGraphInteractionTargetKind kind = ResolveTargetKind(
                actorPick.EntityId,
                objectPick.EntityId,
                plantPick.EntityId);
            string reason = ResolveReason(kind);

            return StoreAndReturn(new ArcGraphInteractionFrame(
                input,
                coordinate,
                kind,
                coordinate.Cell,
                actorPick.EntityId,
                objectPick.EntityId,
                plantPick.EntityId,
                true,
                false,
                reason),
                actorPick.CandidateCount,
                objectPick.CandidateCount,
                plantPick.CandidateCount);
        }

        private ArcGraphInteractionFrame StoreAndReturn(
            ArcGraphInteractionFrame frame,
            int actorCandidateCount = 0,
            int objectCandidateCount = 0,
            int plantCandidateCount = 0)
        {
            LastDiagnostics = new ArcGraphInteractionBoundaryDiagnostics(
                frame.Input.HasPointerScreenPosition,
                frame.IsPointerOverUi,
                frame.HasValidCell,
                actorCandidateCount,
                objectCandidateCount,
                plantCandidateCount,
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

        private static PickResult TryPickPlant(
            IReadOnlyList<ArcGraphVegetationRenderItem> vegetation,
            ArcGraphCellCoord cell)
        {
            if (vegetation == null || vegetation.Count == 0)
                return PickResult.Empty();

            int selectedPlantId = -1;
            ArcGraphRenderSortKey selectedSortKey = default;
            int candidateCount = 0;
            bool hasSelected = false;

            for (int i = 0; i < vegetation.Count; i++)
            {
                ArcGraphVegetationRenderItem item = vegetation[i];
                if (!item.IsVisible || !item.IsPhysicalPlant || !IsPlantHitCell(item, cell))
                    continue;

                candidateCount++;
                if (!hasSelected || item.SortKey.CompareTo(selectedSortKey) >= 0)
                {
                    selectedPlantId = item.PlantId;
                    selectedSortKey = item.SortKey;
                    hasSelected = true;
                }
            }

            return new PickResult(selectedPlantId, candidateCount);
        }

        // =============================================================================
        // IsObjectHitCell
        // =============================================================================
        /// <summary>
        /// <para>
        /// Verifica se la cella puntata ricade dentro il footprint logico/base
        /// dell'oggetto ArcGraph.
        /// </para>
        ///
        /// <para><b>Principio architetturale: picking centralizzato nel boundary</b></para>
        /// <para>
        /// Muri, porte e oggetti estesi possono occupare piu' di una cella logica.
        /// Il boundary deve quindi usare soltanto il footprint/base gia' preparato
        /// nella render queue, invece della dimensione verticale dello sprite. La
        /// parte alta visuale di un muro non deve impedire la selezione di un
        /// oggetto posizionato nella cella dietro/sopra quel muro.
        /// </para>
        /// </summary>
        private static bool IsObjectHitCell(
            ArcGraphObjectRenderItem item,
            ArcGraphCellCoord pointerCell)
        {
            if (item.Cell.Z != pointerCell.Z)
                return false;

            return IsObjectLogicalFootprintHit(item, pointerCell);
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

        // =============================================================================
        // IsPlantHitCell
        // =============================================================================
        /// <summary>
        /// <para>
        /// Verifica se la cella puntata coincide con la base fisica della pianta.
        /// </para>
        ///
        /// <para><b>Hitbox visuale minimale</b></para>
        /// <para>
        /// Le piante alte possono estendersi graficamente sopra la cella, ma in
        /// questa prima patch il picking resta sulla cella base per evitare che uno
        /// sprite alto rubi click ad actor o oggetti dietro di lui.
        /// </para>
        /// </summary>
        private static bool IsPlantHitCell(
            ArcGraphVegetationRenderItem item,
            ArcGraphCellCoord pointerCell)
        {
            return item.Cell.Z == pointerCell.Z
                   && item.Cell.X == pointerCell.X
                   && item.Cell.Y == pointerCell.Y;
        }

        private static ArcGraphInteractionTargetKind ResolveTargetKind(int actorId, int objectId, int plantId)
        {
            if (actorId > 0)
                return ArcGraphInteractionTargetKind.Actor;

            if (objectId > 0)
                return ArcGraphInteractionTargetKind.Object;

            if (plantId > 0)
                return ArcGraphInteractionTargetKind.Plant;

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
                case ArcGraphInteractionTargetKind.Plant:
                    return "PlantPicked";
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
