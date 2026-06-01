using Arcontio.Core.Diagnostics;
using System;
using System.Collections.Generic;
using UnityEngine;

namespace Arcontio.Core
{
    // =============================================================================
    // ObjectPerceptionSystem — Patch 0.02.5A
    // Geometria FOV centralizzata in FovUtils.cs
    // =============================================================================
    /// <summary>
    /// <b>ObjectPerceptionSystem</b> — percezione visiva degli oggetti interagibili.
    ///
    /// <para>
    /// Per ogni NPC attivo, valuta quali oggetti sono visibili e produce un
    /// <c>ObjectSpottedEvent</c> per ciascuno, consumato da <c>MemoryEncodingSystem</c>
    /// per aggiornare la memoria percettiva dell'NPC osservatore.
    /// </para>
    ///
    /// <para><b>Pipeline di visione (Arcontio Core Standard v1.0):</b></para>
    /// <list type="number">
    ///   <item><b>Range gate</b> — Manhattan &lt;= visionRange (gate più economico, primo)</item>
    ///   <item><b>Cone gate</b> — <see cref="FovUtils.IsInCone"/> oppure <see cref="FovUtils.IsInFront"/></item>
    ///   <item><b>LOS gate</b>  — <c>world.HasLineOfSight</c> (Bresenham, gate più costoso, ultimo)</item>
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

        private readonly List<int> _npcIds = new(2048);
        private readonly List<int> _objIds = new(2048);
        private readonly List<Vector2Int> _visibleMissingFoodBeliefCells = new(64);

        public void Update(World world, Tick tick, MessageBus bus, Telemetry telemetry)
        {
            if (world.NpcDna.Count == 0)
                return;

            int visionRange = world.Global.NpcVisionRangeCells;
            if (visionRange <= 0) visionRange = 6;

            bool useCone = world.Global.NpcVisionUseCone;
            float coneSlope = world.Global.NpcVisionConeSlope;

            // Back-compat: se stai ancora usando "NpcVisionConeHalfWidthPerStep"
            // e NpcVisionConeSlope non � impostato, copia.
            if (coneSlope <= 0f && world.Global.NpcVisionConeHalfWidthPerStep > 0f)
                coneSlope = world.Global.NpcVisionConeHalfWidthPerStep;

            _npcIds.Clear();
            _npcIds.AddRange(world.NpcDna.Keys);

            _objIds.Clear();
            _objIds.AddRange(world.Objects.Keys);

            int spotted = 0;

            for (int n = 0; n < _npcIds.Count; n++)
            {
                int npcId = _npcIds[n];
                if (!world.GridPos.TryGetValue(npcId, out var np))
                    continue;

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

                    RecordDebugFovCellsForNpc(
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
                    (int)tick.Index);

                for (int o = 0; o < _objIds.Count; o++)
                {
                    int objId = _objIds[o];
                    if (!world.Objects.TryGetValue(objId, out var obj) || obj == null)
                        continue;

                    // filtro: solo oggetti definiti e interagibili
                    if (!world.TryGetObjectDef(obj.DefId, out var def) || def == null)
                        continue;

                    if (!def.IsInteractable)
                        continue;

                    // Patch 0.02.5A: FovUtils.Manhattan è la fonte canonica
                    int dist = FovUtils.Manhattan(np.X, np.Y, obj.CellX, obj.CellY);
                    if (dist > visionRange)
                        continue;

                    if (dist > 0)
                    {
                    // Cone check (opzionale)
                    if (useCone)
                    {
                        // Patch 0.02.5A: delega a FovUtils (fonte canonica del cono)
                        if (!FovUtils.IsInCone(np.X, np.Y, facing, obj.CellX, obj.CellY, coneSlope))
                            continue;
                    }
                    else
                    {
                        // modalit� legacy: "davanti" (linea frontale)
                        // Modalità legacy (useCone=false): solo linea frontale cardinale
                        if (!FovUtils.IsInFront(np.X, np.Y, facing, obj.CellX, obj.CellY))
                            continue;
                    }

                    // LOS check (questo � il pezzo che ti mancava nel T9)
                    if (!world.HasLineOfSight(np.X, np.Y, obj.CellX, obj.CellY))
                        continue;

                    // Patch 0.02.5A: qualità centralizzata in FovUtils.ObservationQuality
                    }

                    float q = FovUtils.ObservationQuality(dist, visionRange);

                    bus.Publish(new ObjectSpottedEvent(
                        observerNpcId: npcId,
                        objectId: objId,
                        defId: obj.DefId,
                        cellX: obj.CellX,
                        cellY: obj.CellY,
                        witnessQuality01: q));

                    spotted++;
                }
            }

            telemetry.Counter("ObjectPerception.SpottedEvents", spotted);
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
            int tick)
        {
            if (world == null || !world.Beliefs.TryGetValue(npcId, out var beliefStore) || beliefStore == null)
                return;

            _visibleMissingFoodBeliefCells.Clear();

            var entries = beliefStore.Entries;
            for (int i = 0; i < entries.Count; i++)
            {
                var belief = entries[i];
                if (belief.Category != BeliefCategory.Food)
                    continue;

                if (belief.Status != BeliefStatus.Active && belief.Status != BeliefStatus.Weak)
                    continue;

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
            foreach (var pair in world.FoodStocks)
            {
                if (pair.Value.Units <= 0)
                    continue;

                if (!world.Objects.TryGetValue(pair.Key, out var obj) || obj == null)
                    continue;

                if (obj.CellX == cellX && obj.CellY == cellY)
                    return true;
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
        /// - Cone gate (90� su 4 orientamenti)
        /// - LOS via OcclusionMap (World.HasLineOfSight)
        ///
        /// Nota:
        /// - Per performance (debug), facciamo uno scan brute-force nel bounding box.
        /// - � accettabile perch�:
        ///   - mappe piccole
        ///   - feature disattivabile
        ///   - finestra N tick (non per forza ogni frame)
        /// </summary>
        private static void RecordDebugFovCellsForNpc(
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
            if (world == null || world.DebugFovTelemetry == null) return;
            if (visionRange <= 0) return;

            int minX = originX - visionRange;
            int maxX = originX + visionRange;
            int minY = originY - visionRange;
            int maxY = originY + visionRange;

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
                        // modalit� legacy: "davanti"
                        // Modalità legacy (useCone=false) nel debug FOV scan
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
                }
            }
        }

        // Patch 0.02.5A: Manhattan rimosso — usa FovUtils.Manhattan

        // Patch 0.02.5A: IsInFront rimosso — usa FovUtils.IsInFront


        // Patch 0.02.5A: IsInCone rimosso — usa FovUtils.IsInCone

    }
}
