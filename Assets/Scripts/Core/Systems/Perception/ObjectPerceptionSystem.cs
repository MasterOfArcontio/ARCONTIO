using Arcontio.Core.Diagnostics;
using System;
using System.Collections.Generic;

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

        public void Update(World world, Tick tick, MessageBus bus, Telemetry telemetry)
        {
            if (world.Objects.Count == 0 || world.NpcDna.Count == 0)
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
