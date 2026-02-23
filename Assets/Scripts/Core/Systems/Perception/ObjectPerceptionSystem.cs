using Arcontio.Core.Diagnostics;
using System;
using System.Collections.Generic;

namespace Arcontio.Core
{
    /// <summary>
    /// ObjectPerceptionSystem:
    /// - per ogni NPC, valuta quali oggetti "interagibili" sono visibili
    /// - produce ObjectSpottedEvent per MemoryEncodingSystem
    ///
    /// Day9+:
    /// - gli occluder (muri/porte) sono oggetti nel World, MA:
    ///   - non generano ObjectSpottedEvent se IsInteractable=false
    ///   - bloccano la LOS tramite World.OcclusionMap
    /// </summary>
    public sealed class ObjectPerceptionSystem : ISystem
    {
        public int Period => 1;

        private readonly List<int> _npcIds = new(2048);
        private readonly List<int> _objIds = new(2048);

        public void Update(World world, Tick tick, MessageBus bus, Telemetry telemetry)
        {
            if (world.Objects.Count == 0 || world.NpcCore.Count == 0)
                return;

            int visionRange = world.Global.NpcVisionRangeCells;
            if (visionRange <= 0) visionRange = 6;

            bool useCone = world.Global.NpcVisionUseCone;
            float coneSlope = world.Global.NpcVisionConeSlope;

            // Back-compat: se stai ancora usando "NpcVisionConeHalfWidthPerStep"
            // e NpcVisionConeSlope non è impostato, copia.
            if (coneSlope <= 0f && world.Global.NpcVisionConeHalfWidthPerStep > 0f)
                coneSlope = world.Global.NpcVisionConeHalfWidthPerStep;

            _npcIds.Clear();
            _npcIds.AddRange(world.NpcCore.Keys);

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

                    int dist = Manhattan(np.X, np.Y, obj.CellX, obj.CellY);
                    if (dist > visionRange)
                        continue;

                    // Cone check (opzionale)
                    if (useCone)
                    {
                        if (!IsInCone(np.X, np.Y, facing, obj.CellX, obj.CellY, coneSlope))
                            continue;
                    }
                    else
                    {
                        // modalità legacy: "davanti" (linea frontale)
                        if (!IsInFront(np.X, np.Y, facing, obj.CellX, obj.CellY))
                            continue;
                    }

                    // LOS check (questo è il pezzo che ti mancava nel T9)
                    if (!world.HasLineOfSight(np.X, np.Y, obj.CellX, obj.CellY))
                        continue;

                    float q = 1f - (dist / (float)visionRange);
                    if (q < 0.05f) q = 0.05f;

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
        /// - Cone gate (90° su 4 orientamenti)
        /// - LOS via OcclusionMap (World.HasLineOfSight)
        ///
        /// Nota:
        /// - Per performance (debug), facciamo uno scan brute-force nel bounding box.
        /// - È accettabile perché:
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

                    int dist = Manhattan(originX, originY, x, y);
                    if (dist > visionRange)
                        continue;

                    // Cone check (opzionale)
                    if (useCone)
                    {
                        if (!IsInCone(originX, originY, facing, x, y, coneSlope))
                            continue;
                    }
                    else
                    {
                        // modalità legacy: "davanti"
                        if (!IsInFront(originX, originY, facing, x, y))
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

        private static int Manhattan(int ax, int ay, int bx, int by)
        {
            int dx = ax - bx; if (dx < 0) dx = -dx;
            int dy = ay - by; if (dy < 0) dy = -dy;
            return dx + dy;
        }

        private static bool IsInFront(int sx, int sy, CardinalDirection facing, int tx, int ty)
        {
            int dx = tx - sx;
            int dy = ty - sy;

            return facing switch
            {
                CardinalDirection.North => dy > 0 && dx == 0,
                CardinalDirection.South => dy < 0 && dx == 0,
                CardinalDirection.East => dx > 0 && dy == 0,
                CardinalDirection.West => dx < 0 && dy == 0,
                _ => false
            };
        }

        /// <summary>
        /// Cono su griglia:
        /// - forward deve essere > 0
        /// - |side| <= floor(forward * slope)
        /// slope:
        /// - 0.0 => linea
        /// - 0.5 => cono stretto
        /// - 1.0 => cono ampio (circa 45° su Manhattan grid)
        /// </summary>
        private static bool IsInCone(int sx, int sy, CardinalDirection facing, int tx, int ty, float slope)
        {
            int dx = tx - sx;
            int dy = ty - sy;

            int forward, side;

            switch (facing)
            {
                case CardinalDirection.North:
                    forward = dy; side = dx; break;
                case CardinalDirection.South:
                    forward = -dy; side = -dx; break;
                case CardinalDirection.East:
                    forward = dx; side = -dy; break;
                case CardinalDirection.West:
                    forward = -dx; side = dy; break;
                default:
                    return false;
            }

            if (forward <= 0)
                return false;

            int absSide = side < 0 ? -side : side;

            // floor(forward * slope)
            int maxSide = (int)Math.Floor((forward * slope) + 0.0001f);
            return absSide <= maxSide;
        }
    }
}
