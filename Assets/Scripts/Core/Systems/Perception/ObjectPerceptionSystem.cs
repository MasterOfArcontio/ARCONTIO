using Arcontio.Core.Diagnostics;
using System;
using System.Collections.Generic;
using UnityEngine;

namespace Arcontio.Core
{
    // =============================================================================
    // ObjectPerceptionSystem â€” Patch 0.02.5A
    // Geometria FOV centralizzata in FovUtils.cs
    // =============================================================================
    /// <summary>
    /// <b>ObjectPerceptionSystem</b> â€” percezione visiva degli oggetti interagibili.
    ///
    /// <para>
    /// Per ogni NPC attivo, valuta quali oggetti sono visibili e produce un
    /// <c>ObjectSpottedEvent</c> per ciascuno, consumato da <c>MemoryEncodingSystem</c>
    /// per aggiornare la memoria percettiva dell'NPC osservatore.
    /// </para>
    ///
    /// <para><b>Pipeline di visione (Arcontio Core Standard v1.0):</b></para>
    /// <list type="number">
    ///   <item><b>Range gate</b> â€” Manhattan &lt;= visionRange (gate piÃ¹ economico, primo)</item>
    ///   <item><b>Cone gate</b> â€” <see cref="FovUtils.IsInCone"/> oppure <see cref="FovUtils.IsInFront"/></item>
    ///   <item><b>LOS gate</b>  â€” <c>world.HasLineOfSight</c> (Bresenham, gate piÃ¹ costoso, ultimo)</item>
    /// </list>
    ///
    /// <para><b>Regole sugli oggetti:</b></para>
    /// <list type="bullet">
    ///   <item>Solo oggetti con <c>def.IsInteractable = true</c> producono eventi.</item>
    ///   <item>Occluder (muri/porte) bloccano la LOS ma non generano eventi.</item>
    /// </list>
    ///
    /// <para>
    /// <b>Patch 0.02.5A:</b> i metodi privati <c>IsInCone</c>, <c>IsInFront</c>
    /// e <c>Manhattan</c> sono stati rimossi. Tutta la geometria FOV delega a
    /// <see cref="FovUtils"/> (fonte canonica unica).
    /// </para>
    /// </summary>
    public sealed class ObjectPerceptionSystem : ISystem
    {
        public int Period => 1;

        private readonly List<Vector2Int> _visibleMissingFoodBeliefCells = new(64);

        public void Update(World world, Tick tick, MessageBus bus, Telemetry telemetry)
        {
            if (world.NpcDna.Count == 0)
                return;

            var costObserver = world.RuntimeCostObserver;
            bool costSample = costObserver != null && costObserver.ShouldSample(tick.Index);
            bool costPerNpc = costSample && costObserver.TrackPerNpc;
            long costStart = costSample ? costObserver.BeginSample() : 0L;
            int costNpcScans = 0;
            int costObjectChecks = 0;
            int costFoodBeliefChecks = 0;
            int costDebugFovCells = 0;
            int costCandidateCells = 0;

            var selectedNpcIds = world.SelectNpcPerceptionUpdatesForTick(tick.Index);

            int spotted = 0;
            int maxCandidateCellsPerNpc = world.Global.ObjectPerceptionMaxCandidateCellsPerNpcPerTick;
            int maxObjectsPerNpc = world.Global.ObjectPerceptionMaxObjectsPerNpcPerTick;

            for (int n = 0; n < selectedNpcIds.Count; n++)
            {
                int costNpcFoodBeliefChecks = 0;

                int npcId = selectedNpcIds[n];
                if (!world.GridPos.TryGetValue(npcId, out var np))
                    continue;

                if (costSample)
                    costNpcScans++;

                int visionRange = world.GetNpcPerceptionRangeCells(npcId);
                if (visionRange <= 0) visionRange = 6;

                bool useCone = world.GetNpcPerceptionUseCone(npcId);
                float coneSlope = world.GetNpcPerceptionConeSlope(npcId);

                if (!world.NpcFacing.TryGetValue(npcId, out var facing))
                    facing = CardinalDirection.North;

                // ============================================================
                // DEBUG FOV (heatmap per finestra N tick)
                // ============================================================
                // Questo NON influenza la percezione canonica degli oggetti.
                // Serve solo a visualizzare in grid view quali celle l'NPC "ha osservato"
                // (in senso geometrico: Range ? Cone ? (opzionale) LOS).
                //
                // Motivazione:
                // - La view poll-a a frame.
                // - Se il tick-rate supera il frame-rate, vedere i coni in sequenza
                //   richiede un buffer.
                // - Qui registriamo la telemetria nel write buffer del World.
                // ============================================================
                if (world.DebugFovTelemetry != null)
                {
                    bool debugUseLos = world.Config?.Sim?.debug_fov != null
                        ? world.Config.Sim.debug_fov.use_los
                        : true;
                    bool debugActiveNpcOnly = world.Config?.Sim?.debug_fov != null
                        ? world.Config.Sim.debug_fov.activeNpcOnly
                        : true;

                    if (world.DebugFovTelemetry.ShouldRecordNpc(npcId, debugActiveNpcOnly))
                    {
                        int debugFovCells = RecordDebugFovCellsForNpc(
                            world: world,
                            npcId: npcId,
                            originX: np.X,
                            originY: np.Y,
                            facing: facing,
                            visionRange: visionRange,
                            useCone: useCone,
                            coneSlope: coneSlope,
                            useLos: debugUseLos
                        );
                        if (costSample)
                            costDebugFovCells += debugFovCells;
                        if (costPerNpc)
                            costObserver.AddNpcWork(npcId, debugFovCells);
                    }
                }

                InvalidateVisibleMissingFoodBeliefs(
                    world,
                    npcId,
                    np.X,
                    np.Y,
                    facing,
                    visionRange,
                    useCone,
                    coneSlope,
                    (int)tick.Index,
                    costSample,
                    out int missingFoodBeliefChecks);
                if (costSample)
                    costFoodBeliefChecks += missingFoodBeliefChecks;
                if (costPerNpc)
                    costNpcFoodBeliefChecks += missingFoodBeliefChecks;

                ProcessVisibleObjectsBySpatialIndex(
                    world,
                    bus,
                    npcId,
                    np,
                    facing,
                    visionRange,
                    useCone,
                    coneSlope,
                    maxCandidateCellsPerNpc,
                    maxObjectsPerNpc,
                    costSample,
                    costPerNpc,
                    costObserver,
                    ref costObjectChecks,
                    ref costCandidateCells,
                    ref spotted);
            }

            telemetry.Counter("ObjectPerception.SpottedEvents", spotted);

            if (costSample)
            {
                costObserver.AddCounter(RuntimeCostCounter.ObjectPerceptionNpcScans, costNpcScans);
                costObserver.AddCounter(RuntimeCostCounter.ObjectPerceptionObjectChecks, costObjectChecks);
                costObserver.AddCounter(RuntimeCostCounter.ObjectPerceptionSpottedEvents, spotted);
                costObserver.AddCounter(RuntimeCostCounter.ObjectPerceptionFoodBeliefChecks, costFoodBeliefChecks);
                costObserver.AddCounter(RuntimeCostCounter.ObjectPerceptionDebugFovCells, costDebugFovCells);
                costObserver.AddCounter(RuntimeCostCounter.ObjectPerceptionCandidateCells, costCandidateCells);
                costObserver.EndSample(RuntimeCostChannel.ObjectPerception, costStart);
            }
        }

        // =============================================================================
        // ProcessVisibleObjectsBySpatialIndex
        // =============================================================================
        /// <summary>
        /// <para>
        /// Processa gli oggetti visibili usando l'indice persistente zona -> oggetti
        /// mantenuto dal <see cref="World"/> invece di attraversare tutti gli oggetti globali.
        /// </para>
        ///
        /// <para><b>Principio architetturale: percezione locale</b></para>
        /// <para>
        /// Il comportamento percettivo resta lo stesso: range, cono/frontale e linea
        /// di vista decidono se un oggetto viene visto. Cambia solo il modo in cui
        /// troviamo i candidati: si visitano le celle nel campo visivo teorico e si
        /// processano solo quelle che contengono davvero un oggetto. Questo riduce il
        /// costo da <c>NPC x tutti gli oggetti a terra</c> a <c>NPC x zone percettive
        /// occupate</c>, senza ricostruire l'indice nel tick caldo.
        /// </para>
        ///
        /// <para><b>Struttura interna:</b></para>
        /// <list type="bullet">
        ///   <item><b>Budget celle</b>: limite opzionale per celle candidate per NPC.</item>
        ///   <item><b>Budget oggetti</b>: limite opzionale per oggetti processati per NPC.</item>
        ///   <item><b>Indice persistente</b>: legge le zone occupate mantenute dal World.</item>
        ///   <item><b>Filtro geometrico</b>: evita di chiedere oggetti fuori range o dietro l'NPC.</item>
        /// </list>
        /// </summary>
        private void ProcessVisibleObjectsBySpatialIndex(
            World world,
            MessageBus bus,
            int npcId,
            GridPosition np,
            CardinalDirection facing,
            int visionRange,
            bool useCone,
            float coneSlope,
            int maxCandidateCellsPerNpc,
            int maxObjectsPerNpc,
            bool costSample,
            bool costPerNpc,
            RuntimeCostObserver costObserver,
            ref int costObjectChecks,
            ref int costCandidateCells,
            ref int spotted)
        {
            int npcCandidateCells = 0;
            int npcObjectChecks = 0;
            int npcSpotted = 0;
            int objectsProcessed = 0;

            int minZoneX = World.FloorDivForSpatialIndex(np.X - visionRange, World.PerceptionObjectZoneSizeCells);
            int maxZoneX = World.FloorDivForSpatialIndex(np.X + visionRange, World.PerceptionObjectZoneSizeCells);
            int minZoneY = World.FloorDivForSpatialIndex(np.Y - visionRange, World.PerceptionObjectZoneSizeCells);
            int maxZoneY = World.FloorDivForSpatialIndex(np.Y + visionRange, World.PerceptionObjectZoneSizeCells);

            for (int zoneY = minZoneY; zoneY <= maxZoneY; zoneY++)
            {
                for (int zoneX = minZoneX; zoneX <= maxZoneX; zoneX++)
                {
                    if (!world.TryGetGroundObjectIdsInPerceptionZone(zoneX, zoneY, out var objectIds))
                        continue;

                    for (int i = 0; i < objectIds.Count; i++)
                    {
                        int objId = objectIds[i];
                        if (!world.Objects.TryGetValue(objId, out var obj) || obj == null || obj.IsHeld)
                            continue;

                        int x = obj.CellX;
                        int y = obj.CellY;
                        if (!world.InBounds(x, y))
                            continue;

                        int dist = FovUtils.Manhattan(np.X, np.Y, x, y);
                        if (dist > visionRange)
                            continue;

                        if (dist > 0)
                        {
                            if (useCone)
                            {
                                if (!IsPotentiallyInFacingHalfPlane(np.X, np.Y, facing, x, y))
                                    continue;

                                if (!FovUtils.IsInCone(np.X, np.Y, facing, x, y, coneSlope))
                                    continue;
                            }
                            else if (!FovUtils.IsInFront(np.X, np.Y, facing, x, y))
                            {
                                continue;
                            }
                        }

                        if (maxCandidateCellsPerNpc > 0 && npcCandidateCells >= maxCandidateCellsPerNpc)
                            goto Done;

                        npcCandidateCells++;
                        if (costSample)
                            costCandidateCells++;

                        if (maxObjectsPerNpc > 0 && objectsProcessed >= maxObjectsPerNpc)
                            goto Done;

                        objectsProcessed++;
                        if (costSample)
                            costObjectChecks++;
                        if (costPerNpc)
                            npcObjectChecks++;

                        if (!world.TryGetObjectDef(obj.DefId, out var def) || def == null)
                            continue;

                        if (!def.IsInteractable)
                            continue;

                        if (dist > 0 && !world.HasLineOfSight(np.X, np.Y, x, y))
                            continue;

                        float q = FovUtils.ObservationQuality(dist, visionRange);
                        world.MarkObjectObservedByNpc(objId, npcId);
                        bus.Publish(new ObjectSpottedEvent(
                            observerNpcId: npcId,
                            objectId: objId,
                            defId: obj.DefId,
                            cellX: obj.CellX,
                            cellY: obj.CellY,
                            witnessQuality01: q));

                        spotted++;
                        npcSpotted++;
                    }
                }
            }

        Done:
            if (costPerNpc)
                costObserver.AddNpcWork(npcId, npcCandidateCells + npcObjectChecks + npcSpotted);

            return;
        }

        // =============================================================================
        // IsPotentiallyInFacingHalfPlane
        // =============================================================================
        /// <summary>
        /// <para>
        /// Applica un filtro direzionale economico prima del cono geometrico completo.
        /// </para>
        ///
        /// <para><b>Filtro conservativo</b></para>
        /// <para>
        /// Il metodo elimina solo target certamente dietro l'NPC. Non sostituisce
        /// <c>FovUtils.IsInCone</c>: serve a evitare lavoro inutile quando la direzione
        /// basta gia' a escludere l'oggetto.
        /// </para>
        /// </summary>
        private static bool IsPotentiallyInFacingHalfPlane(
            int originX,
            int originY,
            CardinalDirection facing,
            int targetX,
            int targetY)
        {
            if (originX == targetX && originY == targetY)
                return true;

            switch (facing)
            {
                case CardinalDirection.North: return targetY > originY;
                case CardinalDirection.South: return targetY < originY;
                case CardinalDirection.East:  return targetX > originX;
                case CardinalDirection.West:  return targetX < originX;
                default:                      return false;
            }
        }

        // =============================================================================
        // InvalidateVisibleMissingFoodBeliefs
        // =============================================================================
        /// <summary>
        /// <para>
        /// Scarta le credenze di cibo quando l'NPC puo' osservare direttamente la
        /// cella creduta e in quella cella non esiste piu' alcuno stock alimentare.
        /// </para>
        ///
        /// <para><b>Principio architetturale: smentita locale, non onniscienza</b></para>
        /// <para>
        /// Questo controllo non corregge credenze lontane o fuori vista. Interviene
        /// solo quando la stessa pipeline percettiva dimostra che la cella e' nel
        /// campo visivo dell'NPC. In questo modo un cibo sparito fuori percezione
        /// resta memoria soggettiva e puo' ancora generare un job; un cibo sparito
        /// davanti agli occhi diventa invece una contraddizione locale.
        /// </para>
        ///
        /// <para><b>Struttura interna:</b></para>
        /// <list type="bullet">
        ///   <item><b>Belief scan</b>: legge solo belief Food Active/Weak.</item>
        ///   <item><b>Gate visivo</b>: range, cono e linea di vista come ObjectSpotted.</item>
        ///   <item><b>Smentita</b>: marca il belief come Discarded e invalida lo slot pratico collegato.</item>
        /// </list>
        /// </summary>
        private void InvalidateVisibleMissingFoodBeliefs(
            World world,
            int npcId,
            int npcX,
            int npcY,
            CardinalDirection facing,
            int visionRange,
            bool useCone,
            float coneSlope,
            int tick,
            bool trackCost,
            out int checkedBeliefs)
        {
            checkedBeliefs = 0;
            if (world == null || !world.Beliefs.TryGetValue(npcId, out var beliefStore) || beliefStore == null)
                return;

            _visibleMissingFoodBeliefCells.Clear();

            var entries = beliefStore.Entries;
            for (int i = 0; i < entries.Count; i++)
            {
                var belief = entries[i];
                if (belief.Category != BeliefCategory.Food)
                    continue;

                if (trackCost)
                    checkedBeliefs++;

                if (belief.Status != BeliefStatus.Active
                    && belief.Status != BeliefStatus.Weak
                    && belief.Status != BeliefStatus.Stale)
                {
                    continue;
                }

                var cell = belief.EstimatedPosition;
                if (!IsCellVisibleToNpc(world, npcX, npcY, facing, cell.x, cell.y, visionRange, useCone, coneSlope))
                    continue;

                if (HasAvailableFoodAtCell(world, cell.x, cell.y))
                    continue;

                _visibleMissingFoodBeliefCells.Add(cell);
            }

            for (int i = 0; i < _visibleMissingFoodBeliefCells.Count; i++)
            {
                var cell = _visibleMissingFoodBeliefCells[i];
                beliefStore.TryDiscardByCategoryAndPosition(BeliefCategory.Food, cell, tick);
                InvalidateFoodObjectMemoryAtCell(world, npcId, cell.x, cell.y);
            }
        }

        private static bool IsCellVisibleToNpc(
            World world,
            int npcX,
            int npcY,
            CardinalDirection facing,
            int cellX,
            int cellY,
            int visionRange,
            bool useCone,
            float coneSlope)
        {
            int dist = FovUtils.Manhattan(npcX, npcY, cellX, cellY);
            if (dist > visionRange)
                return false;

            if (dist == 0)
                return true;

            if (useCone)
            {
                if (!FovUtils.IsInCone(npcX, npcY, facing, cellX, cellY, coneSlope))
                    return false;
            }
            else if (!FovUtils.IsInFront(npcX, npcY, facing, cellX, cellY))
            {
                return false;
            }

            return world.HasLineOfSight(npcX, npcY, cellX, cellY);
        }

        private static bool HasAvailableFoodAtCell(World world, int cellX, int cellY)
        {
            foreach (var pair in world.Objects)
            {
                if (world.TryGetAvailableFoodObjectFacts(
                        pair.Key,
                        requireCommunityOwner: false,
                        out _,
                        out _,
                        out int x,
                        out int y)
                    && x == cellX
                    && y == cellY)
                {
                    return true;
                }
            }

            return false;
        }

        private static void InvalidateFoodObjectMemoryAtCell(World world, int npcId, int cellX, int cellY)
        {
            if (!world.NpcObjectMemory.TryGetValue(npcId, out var store) || store == null)
                return;

            for (int i = 0; i < store.Slots.Length; i++)
            {
                ref var slot = ref store.Slots[i];
                if (!slot.IsValid || slot.Kind != NpcObjectMemoryStore.SubjectKind.WorldObject)
                    continue;

                if (slot.CellX != cellX || slot.CellY != cellY)
                    continue;

                if (string.IsNullOrWhiteSpace(slot.DefId)
                    || slot.DefId.IndexOf("food", StringComparison.OrdinalIgnoreCase) < 0)
                    continue;

                slot.IsValid = false;
                break;
            }
        }

        /// <summary>
        /// Registra nel DebugFovTelemetry tutte le celle "candidate" viste dall'NPC.
        ///
        /// Pipeline coerente con ARCONTIO Core Standard:
        /// - Range gate
        /// - Cone gate (90ï¿½ su 4 orientamenti)
        /// - LOS via OcclusionMap (World.HasLineOfSight)
        ///
        /// Nota:
        /// - Per performance (debug), facciamo uno scan brute-force nel bounding box.
        /// - ï¿½ accettabile perchï¿½:
        ///   - mappe piccole
        ///   - feature disattivabile
        ///   - finestra N tick (non per forza ogni frame)
        /// </summary>
        private static int RecordDebugFovCellsForNpc(
            World world,
            int npcId,
            int originX,
            int originY,
            CardinalDirection facing,
            int visionRange,
            bool useCone,
            float coneSlope,
            bool useLos)
        {
            // Fail-safe
            if (world == null || world.DebugFovTelemetry == null) return 0;
            if (visionRange <= 0) return 0;

            int minX = originX - visionRange;
            int maxX = originX + visionRange;
            int minY = originY - visionRange;
            int maxY = originY + visionRange;
            int recordedCells = 0;

            for (int y = minY; y <= maxY; y++)
            {
                for (int x = minX; x <= maxX; x++)
                {
                    if (!world.InBounds(x, y))
                        continue;

                    int dist = FovUtils.Manhattan(originX, originY, x, y);
                    if (dist > visionRange)
                        continue;

                    // Cone check (opzionale)
                    if (useCone)
                    {
                        // Patch 0.02.5A: usa FovUtils anche nel debug FOV scan
                        if (!FovUtils.IsInCone(originX, originY, facing, x, y, coneSlope))
                            continue;
                    }
                    else
                    {
                        // modalitï¿½ legacy: "davanti"
                        // ModalitÃ  legacy (useCone=false) nel debug FOV scan
                        if (!FovUtils.IsInFront(originX, originY, facing, x, y))
                            continue;
                    }

                    // LOS (opzionale via debug flag)
                    if (useLos)
                    {
                        if (!world.HasLineOfSight(originX, originY, x, y))
                            continue;
                    }

                    world.DebugFovTelemetry.RecordCell(npcId, x, y);
                    recordedCells++;
                }
            }

            return recordedCells;
        }

        // Patch 0.02.5A: Manhattan rimosso â€” usa FovUtils.Manhattan

        // Patch 0.02.5A: IsInFront rimosso â€” usa FovUtils.IsInFront


        // Patch 0.02.5A: IsInCone rimosso â€” usa FovUtils.IsInCone

    }
}
