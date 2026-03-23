using System.Collections.Generic;
using Arcontio.Core.Diagnostics;

namespace Arcontio.Core
{
    /// <summary>
    /// NpcLandmarkMemorySystem (v0.02 Day3):
    /// mantiene la memoria landmark per NPC (eviction + cap).
    ///
    /// Perche' e' un System separato e NON dentro MovementSystem:
    /// - MovementSystem deve restare focused sul movimento fisico.
    /// - L'eviction puo' essere applicata anche senza movimento (staleness nel tempo).
    /// - Tenere la manutenzione qui rende i tick piu prevedibili e facilita debug.
    ///
    /// Day3 scope:
    /// - evict stale (eviction_stale_ticks)
    /// - enforce caps (maxLandmarksPerNpc, maxEdgesPerNpc)
    /// - anti-thrashing cooldown (eviction_cooldown_ticks)
    /// </summary>
    public sealed class NpcLandmarkMemorySystem : ISystem
    {
        // Period 1: applichiamo la policy ogni tick per coerenza.
        // Nota: il numero di NPC e' ancora piccolo in 0.02.
        public int Period => 1;

        private readonly List<int> _ids = new(2048);

        public void Update(World world, Tick tick, MessageBus bus, Telemetry telemetry)
        {
            if (world == null) return;

            // Se il sistema e' disabilitato, non facciamo nulla.
            if (!world.Global.EnableLandmarkSystem)
                return;

            if (world.NpcLandmarkMemory == null || world.NpcLandmarkMemory.Count == 0)
                return;

            long now = TickContext.CurrentTickIndex;
            int staleTicks = world.Global.LandmarkEvictionStaleTicks;
            int cooldownTicks = world.Global.LandmarkEvictionCooldownTicks;

            _ids.Clear();
            _ids.AddRange(world.NpcLandmarkMemory.Keys);

            int totalKnownNodes = 0;
            int totalKnownEdges = 0;

            for (int i = 0; i < _ids.Count; i++)
            {
                int npcId = _ids[i];

                if (!world.NpcLandmarkMemory.TryGetValue(npcId, out var mem) || mem == null)
                    continue;

                mem.TickMaintenance(now, staleTicks: staleTicks, evictionCooldownTicks: cooldownTicks);

                totalKnownNodes += mem.KnownLandmarksCount;
                totalKnownEdges += mem.KnownEdgesCount;
            }

            // Telemetry (debug): utile per capire se stiamo accumulando troppo.
            telemetry.Counter("NpcLandmarkMemorySystem.TotalKnownLandmarks", totalKnownNodes);
            telemetry.Counter("NpcLandmarkMemorySystem.TotalKnownEdges", totalKnownEdges);
        }
    }
}
