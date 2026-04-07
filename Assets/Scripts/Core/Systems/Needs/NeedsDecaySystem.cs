using System.Collections.Generic;
using Arcontio.Core.Diagnostics;

namespace Arcontio.Core
{
    /// <summary>
    /// NeedsDecaySystem (v0.04.07 — struttura generica NpcNeeds):
    ///
    /// - Applica decay a Hunger e Rest ogni tick (altri need: decay=0 fino alle sessioni 08–10).
    /// - Setta i flag IsAlert/IsCritical confrontando ogni Value01 con le soglie DNA
    ///   (NpcThresholds.NeedAlert01 e NeedCritical01).
    ///
    /// NON decide cosa fare — quello sta nelle Rules.
    /// </summary>
    public sealed class NeedsDecaySystem : ISystem
    {
        public int Period => 1;

        private readonly List<int> _npcIds = new(2048);

        public void Update(World world, Tick tick, MessageBus bus, Telemetry telemetry)
        {
            if (world.NpcDna.Count == 0) return;

            var cfg = world.Global.Needs;

            _npcIds.Clear();
            _npcIds.AddRange(world.NpcDna.Keys);

            int updated = 0;

            for (int i = 0; i < _npcIds.Count; i++)
            {
                int npcId = _npcIds[i];
                if (!world.Needs.TryGetValue(npcId, out var n)) continue;
                if (n.States == null) continue;

                // ── Decay attivi (sessione 07: solo Hunger e Rest) ────────────
                n.AddValue(NeedKind.Hunger, cfg.satietyDecayPerTick);
                n.AddValue(NeedKind.Rest,   cfg.restDecayPerTick);

                // ── Flag IsAlert / IsCritical da soglie DNA ───────────────────
                float alertThr    = 0.60f;
                float criticalThr = 0.85f;

                if (world.NpcDna.TryGetValue(npcId, out var dna))
                {
                    alertThr    = dna.Thresholds.NeedAlert01;
                    criticalThr = dna.Thresholds.NeedCritical01;
                }

                for (int k = 0; k < (int)NeedKind.COUNT; k++)
                {
                    float v = n.States[k].Value01;
                    n.SetFlags((NeedKind)k, v >= alertThr, v >= criticalThr);
                }

                world.Needs[npcId] = n;
                updated++;
            }

            telemetry.Counter("NeedsDecay.Updated", updated);
        }
    }
}
