using System.Collections.Generic;
using Arcontio.Core.Diagnostics;

namespace Arcontio.Core
{
    // =============================================================================
    // BeliefDecaySystem
    // =============================================================================
    /// <summary>
    /// <para>
    /// Sistema di manutenzione passiva delle credenze soggettive per-NPC.
    /// </para>
    ///
    /// <para><b>Decay confidence e freshness</b></para>
    /// <para>
    /// Questo system non crea nuove credenze, non legge il mondo oggettivo e non
    /// valuta candidati per il Decision Layer. Si limita a visitare gli store belief
    /// esistenti e ad applicare il decadimento configurato per categoria.
    /// </para>
    ///
    /// <para><b>Struttura interna:</b></para>
    /// <list type="bullet">
    ///   <item><b>Beliefs</b>: dizionario per-NPC letto da <c>world.Beliefs</c>.</item>
    ///   <item><b>Config</b>: <c>world.Global.BeliefDecay</c>, caricata da file o default.</item>
    ///   <item><b>Telemetry</b>: contatori aggregati di updated, weak, stale e removed.</item>
    /// </list>
    /// </summary>
    public sealed class BeliefDecaySystem : ISystem
    {
        public int Period => 1;

        private readonly List<int> _npcIds = new(2048);

        // =============================================================================
        // Update
        // =============================================================================
        /// <summary>
        /// <para>
        /// Applica un tick di decay a tutti i <c>BeliefStore</c> per-NPC esistenti.
        /// </para>
        ///
        /// <para><b>Manutenzione non decisionale</b></para>
        /// <para>
        /// Il metodo non decide se un belief sia utile e non lo usa per generare
        /// intenzioni. Aggiorna soltanto i valori passivi necessari al futuro
        /// QuerySystem: <c>Confidence</c>, <c>Freshness</c> e <c>Status</c>.
        /// </para>
        ///
        /// <para><b>Struttura interna:</b></para>
        /// <list type="bullet">
        ///   <item><b>Snapshot ids</b>: evita modifiche del dizionario durante iterazione.</item>
        ///   <item><b>Tick scale</b>: usa <c>tick.DeltaTime</c> come gli altri decay runtime.</item>
        ///   <item><b>Store decay</b>: delega al BeliefStore la mutazione delle entry.</item>
        /// </list>
        /// </summary>
        public void Update(World world, Tick tick, MessageBus bus, Telemetry telemetry)
        {
            if (world == null || world.Beliefs == null || world.Beliefs.Count == 0)
                return;

            _npcIds.Clear();
            _npcIds.AddRange(world.Beliefs.Keys);

            int updatedTotal = 0;
            int weakTotal = 0;
            int staleTotal = 0;
            int removedTotal = 0;

            var config = world.Global.BeliefDecay;
            float tickScale = tick.DeltaTime;

            for (int i = 0; i < _npcIds.Count; i++)
            {
                int npcId = _npcIds[i];
                if (!world.Beliefs.TryGetValue(npcId, out var store) || store == null)
                    continue;

                removedTotal += store.TickDecay(config, tickScale, out int updated, out int weak, out int stale);
                updatedTotal += updated;
                weakTotal += weak;
                staleTotal += stale;
            }

            telemetry.Counter("BeliefDecaySystem.EntriesUpdated", updatedTotal);
            telemetry.Counter("BeliefDecaySystem.EntriesWeak", weakTotal);
            telemetry.Counter("BeliefDecaySystem.EntriesStale", staleTotal);
            telemetry.Counter("BeliefDecaySystem.EntriesRemoved", removedTotal);
        }
    }
}
