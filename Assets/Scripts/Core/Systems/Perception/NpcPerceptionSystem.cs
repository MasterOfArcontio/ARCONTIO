using Arcontio.Core.Diagnostics;
using System;
using System.Collections.Generic;

namespace Arcontio.Core
{
    /// <summary>
    /// NpcPerceptionSystem:
    /// - per ogni NPC, valuta quali altri NPC sono visibili (range + cono + LOS)
    /// - produce NpcSpottedEvent
    ///
    /// Perché esiste:
    /// - Step4: vogliamo poter popolare la memoria "Observed entities" anche con NPC osservati,
    ///   evitando qualsiasi scansione globale nel planning (no telepatia).
    ///
    /// Coerenza con ARCONTIO Core Standard v1.0:
    /// - Pipeline visione unica: Range -> Cone -> LOS (OcclusionMap)
    /// - FOV: 90° con 4 orientamenti (N/E/S/W), implementata come "cone" discreto via slope
    ///
    /// Nota implementativa:
    /// - Questo system è volutamente simile a ObjectPerceptionSystem per mantenere la coerenza.
    /// - In futuro potrebbe essere ottimizzato (spatial hashing, region query, ecc.).
    /// </summary>
    public sealed class NpcPerceptionSystem : ISystem
    {
        public int Period => 1;

        private readonly List<int> _npcIds = new(2048);

        public void Update(World world, Tick tick, MessageBus bus, Telemetry telemetry)
        {
            if (world.NpcCore.Count == 0)
                return;

            int visionRange = world.Global.NpcVisionRangeCells;
            if (visionRange <= 0) visionRange = 6;

            bool useCone = world.Global.NpcVisionUseCone;
            float coneSlope = world.Global.NpcVisionConeSlope;

            // Snapshot NPC ids (evita iterazioni su Dictionary mentre qualcuno muta lo state)
            _npcIds.Clear();
            foreach (var kv in world.NpcCore)
                _npcIds.Add(kv.Key);

            int spotted = 0;

            for (int i = 0; i < _npcIds.Count; i++)
            {
                int observerId = _npcIds[i];

                if (!world.GridPos.TryGetValue(observerId, out var op))
                    continue;

                if (!world.NpcFacing.TryGetValue(observerId, out var facing))
                    facing = CardinalDirection.North;

                // Cono/LOS dal punto di vista dell'osservatore
                int ox = op.X;
                int oy = op.Y;

                for (int j = 0; j < _npcIds.Count; j++)
                {
                    int targetId = _npcIds[j];
                    if (targetId == observerId)
                        continue;

                    if (!world.GridPos.TryGetValue(targetId, out var tp))
                        continue;

                    int tx = tp.X;
                    int ty = tp.Y;

                    int dx = tx - ox; if (dx < 0) dx = -dx;
                    int dy = ty - oy; if (dy < 0) dy = -dy;
                    int dist = dx + dy;

                    if (dist <= 0)
                        continue;

                    if (dist > visionRange)
                        continue;

                    if (useCone && !IsInCone(ox, oy, facing, tx, ty, coneSlope))
                        continue;

                    // LOS: usa OcclusionMap via World.HasLineOfSight
                    if (!world.HasLineOfSight(ox, oy, tx, ty))
                        continue;

                    // Witness quality: oggi una funzione semplice della distanza.
                    // (Si potrebbe pesare anche orientamento, qualità visiva, ecc.)
                    float q = 1f - (dist / (float)visionRange);
                    if (q < 0.05f) q = 0.05f;

                    bus.Publish(new NpcSpottedEvent(
                        observerNpcId: observerId,
                        observedNpcId: targetId,
                        cellX: tx,
                        cellY: ty,
                        distanceCells: dist,
                        witnessQuality01: q));

                    spotted++;
                }
            }

            telemetry.Counter("NpcPerceptionSystem.NpcSpottedEvents", spotted);
        }

        static bool IsInCone(int sx, int sy, CardinalDirection facing, int tx, int ty, float slope)
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
