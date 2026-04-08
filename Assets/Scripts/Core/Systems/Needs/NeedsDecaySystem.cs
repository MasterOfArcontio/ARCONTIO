using System.Collections.Generic;
using Arcontio.Core.Diagnostics;

namespace Arcontio.Core
{
    /// <summary>
    /// NeedsDecaySystem (v0.04.08 — Fame · Sete · Riposo):
    ///
    /// Responsabilità:
    ///   - Applica il decay ogni tick ai tre bisogni fisiologici primari attivati in v0.04.08:
    ///       Hunger  — fame, cresce con satietyDecayPerTick
    ///       Thirst  — sete, cresce con thirstDecayPerTick (più rapido della fame)
    ///       Rest    — stanchezza, cresce con restDecayPerTick
    ///   - Setta i flag IsAlert/IsCritical per TUTTI i NeedKind confrontando Value01
    ///     con NpcThresholds.NeedAlert01 e NeedCritical01 del DNA dell'NPC.
    ///     I bisogni non ancora attivi (Health, Comfort, ecc.) rimangono a 0 e non scattano.
    ///
    /// NON decide cosa fare — quello è compito di NeedsDecisionRule.
    ///
    /// Nota per v0.04.09–10:
    ///   Quando Health, Comfort, Security, Stability, Sociality verranno attivati,
    ///   basterà aggiungere la riga n.AddValue(NeedKind.X, cfg.xDecayPerTick) in questo sistema.
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

                // ── Decay attivi (v0.04.08): Fame · Sete · Riposo ─────────────
                // Gli altri NeedKind (Health, Comfort, Security, Stability, Sociality)
                // verranno attivati nelle sessioni 09–10 con i rispettivi parametri.
                n.AddValue(NeedKind.Hunger, cfg.satietyDecayPerTick);
                n.AddValue(NeedKind.Thirst, cfg.thirstDecayPerTick);
                n.AddValue(NeedKind.Rest,   cfg.restDecayPerTick);

                // ── Flag IsAlert / IsCritical da soglie DNA ───────────────────
                // Legge NpcThresholds dal DNA; fallback a valori conservativi se il DNA manca.
                // I flag vengono aggiornati per TUTTI i NeedKind, compresi quelli a 0:
                // non scatteranno mai finché il valore rimane sotto la soglia.
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
