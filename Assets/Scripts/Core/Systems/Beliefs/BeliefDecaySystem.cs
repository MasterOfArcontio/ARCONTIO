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

            var config = world.Global.BeliefDecay;
            bool defaultEveryTick = UsesDefaultEveryTick(config);
            bool foodDue = defaultEveryTick || IsCategoryDue(tick.Index, config.foodDecayIntervalTicks);
            bool restDue = defaultEveryTick || IsCategoryDue(tick.Index, config.restDecayIntervalTicks);
            bool dangerDue = defaultEveryTick || IsCategoryDue(tick.Index, config.dangerDecayIntervalTicks);
            bool socialDue = defaultEveryTick || IsCategoryDue(tick.Index, config.socialDecayIntervalTicks);
            bool ownershipDue = defaultEveryTick || IsCategoryDue(tick.Index, config.ownershipDecayIntervalTicks);
            bool situationDue = defaultEveryTick || IsCategoryDue(tick.Index, config.situationDecayIntervalTicks);
            bool structureDue = defaultEveryTick || IsCategoryDue(tick.Index, config.structureDecayIntervalTicks);

            if (!foodDue && !restDue && !dangerDue && !socialDue && !ownershipDue && !situationDue && !structureDue)
                return;

            var costObserver = world.RuntimeCostObserver;
            bool costSample = costObserver != null && costObserver.ShouldSample(tick.Index);
            bool costPerNpc = costSample && costObserver.TrackPerNpc;
            long costStart = costSample ? costObserver.BeginSample() : 0L;

            _npcIds.Clear();
            _npcIds.AddRange(world.Beliefs.Keys);

            int updatedTotal = 0;
            int weakTotal = 0;
            int staleTotal = 0;
            int removedTotal = 0;

            float tickScale = tick.DeltaTime;

            for (int i = 0; i < _npcIds.Count; i++)
            {
                int npcId = _npcIds[i];
                if (!world.Beliefs.TryGetValue(npcId, out var store) || store == null)
                    continue;

                int updated = 0;
                int weak = 0;
                int stale = 0;

                if (defaultEveryTick)
                {
                    removedTotal += store.TickDecay(config, tickScale, out updated, out weak, out stale);
                }
                else
                {
                    removedTotal += TickCategoryIfDue(store, BeliefCategory.Food, config, tickScale, config.foodDecayIntervalTicks, foodDue, ref updated, ref weak, ref stale);
                    removedTotal += TickCategoryIfDue(store, BeliefCategory.Rest, config, tickScale, config.restDecayIntervalTicks, restDue, ref updated, ref weak, ref stale);
                    removedTotal += TickCategoryIfDue(store, BeliefCategory.Danger, config, tickScale, config.dangerDecayIntervalTicks, dangerDue, ref updated, ref weak, ref stale);
                    removedTotal += TickCategoryIfDue(store, BeliefCategory.Social, config, tickScale, config.socialDecayIntervalTicks, socialDue, ref updated, ref weak, ref stale);
                    removedTotal += TickCategoryIfDue(store, BeliefCategory.Ownership, config, tickScale, config.ownershipDecayIntervalTicks, ownershipDue, ref updated, ref weak, ref stale);
                    removedTotal += TickCategoryIfDue(store, BeliefCategory.Situation, config, tickScale, config.situationDecayIntervalTicks, situationDue, ref updated, ref weak, ref stale);
                    removedTotal += TickCategoryIfDue(store, BeliefCategory.Structure, config, tickScale, config.structureDecayIntervalTicks, structureDue, ref updated, ref weak, ref stale);
                }

                updatedTotal += updated;
                weakTotal += weak;
                staleTotal += stale;
                if (costPerNpc)
                    costObserver.AddNpcWork(npcId, updated + weak + stale);
            }

            telemetry.Counter("BeliefDecaySystem.EntriesUpdated", updatedTotal);
            telemetry.Counter("BeliefDecaySystem.EntriesWeak", weakTotal);
            telemetry.Counter("BeliefDecaySystem.EntriesStale", staleTotal);
            telemetry.Counter("BeliefDecaySystem.EntriesRemoved", removedTotal);

            if (costSample)
            {
                costObserver.AddCounter(RuntimeCostCounter.BeliefDecayStores, _npcIds.Count);
                costObserver.AddCounter(RuntimeCostCounter.BeliefDecayEntriesUpdated, updatedTotal);
                costObserver.EndSample(RuntimeCostChannel.BeliefDecay, costStart);
            }
        }

        private static bool UsesDefaultEveryTick(BeliefDecayConfig config)
        {
            return config.foodDecayIntervalTicks <= 1
                && config.restDecayIntervalTicks <= 1
                && config.dangerDecayIntervalTicks <= 1
                && config.socialDecayIntervalTicks <= 1
                && config.ownershipDecayIntervalTicks <= 1
                && config.situationDecayIntervalTicks <= 1
                && config.structureDecayIntervalTicks <= 1;
        }

        private static bool IsCategoryDue(long tickIndex, int intervalTicks)
        {
            if (intervalTicks <= 1)
                return true;

            long normalizedTick = tickIndex + 1L;
            return normalizedTick % intervalTicks == 0L;
        }

        private static int TickCategoryIfDue(
            BeliefStore store,
            BeliefCategory category,
            BeliefDecayConfig config,
            float tickScale,
            int intervalTicks,
            bool due,
            ref int updated,
            ref int weak,
            ref int stale)
        {
            if (!due)
                return 0;

            int safeInterval = intervalTicks > 1 ? intervalTicks : 1;
            int removed = store.TickDecayCategory(
                category,
                config,
                tickScale * safeInterval,
                out int categoryUpdated,
                out int categoryWeak,
                out int categoryStale);

            updated += categoryUpdated;
            weak += categoryWeak;
            stale += categoryStale;
            return removed;
        }
    }
}
