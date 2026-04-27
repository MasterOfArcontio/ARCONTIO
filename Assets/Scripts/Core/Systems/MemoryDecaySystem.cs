using System.Collections.Generic;
using Arcontio.Core.Diagnostics;

namespace Arcontio.Core
{
    /// <summary>
    /// MemoryDecaySystem: fa decadere le memorie di ogni NPC.
    ///
    /// - Non crea nuove memorie (nessuna codifica eventi qui)
    /// - Non comunica token
    /// - Si limita ad applicare TickDecay su ogni NPC
    ///
    /// Il decay è modulato dai tratti cognitivi del DNA dell'NPC:
    ///   MemoryResilience01 alta => dimentica più in fretta
    ///   Rumination01 alta => dimentica più lentamente
    /// </summary>
    public sealed class MemoryDecaySystem : ISystem
    {
        public int Period => 1;

        private readonly List<int> _ids = new(2048);

        public void Update(World world, Tick tick, MessageBus bus, Telemetry telemetry)
        {
            if (world.Memory == null || world.Memory.Count == 0)
                return;

            _ids.Clear();
            _ids.AddRange(world.Memory.Keys);

            int removedTotal = 0;

            // Decay scalato dal tempo simulato
            float tickScale = tick.DeltaTime;

            for (int i = 0; i < _ids.Count; i++)
            {
                int id = _ids[i];

                if (!world.Memory.TryGetValue(id, out var store) || store == null)
                    continue;

                // Legge i tratti individuali direttamente dal DNA (source of truth).
                // Se l'NPC non ha DNA (non dovrebbe accadere), usa valori neutri.
                float resilience;
                float rumination;
                if (world.NpcDna.TryGetValue(id, out var dna))
                {
                    resilience = dna.CognitiveModulators.MemoryResilience01;
                    rumination = dna.CognitiveModulators.Rumination01;
                }
                else
                {
                    resilience = 0.50f;
                    rumination = 0.25f;
                }

                // Calcolo del moltiplicatore di decay:
                // - MemoryResilience01 aumenta decay (dimentica prima)
                // - Rumination01 riduce decay (rimugina, trattiene)
                //
                // Esempio:
                //   Resilience 0.0 => +0%
                //   Resilience 1.0 => +100%
                //
                //   Rumination 0.0 => -0%
                //   Rumination 1.0 => -50% (non azzeriamo mai del tutto)
                float decayMultiplier = 1f;

                decayMultiplier += resilience * 1.0f;   // +0..+1
                decayMultiplier -= rumination * 0.5f;   // -0..-0.5

                // Clamp di sicurezza: non vogliamo decay <= 0
                if (decayMultiplier < 0.10f) decayMultiplier = 0.10f;

                removedTotal += store.TickDecay(tickScale, decayMultiplier);
            }

            telemetry.Counter("MemoryDecaySystem.TracesRemoved", removedTotal);
        }
    }
}
