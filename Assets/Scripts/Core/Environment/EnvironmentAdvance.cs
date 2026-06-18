using Arcontio.Core.Config;
using System.Collections.Generic;

namespace Arcontio.Core.Environment
{
    // =============================================================================
    // EnvironmentRuntimeScheduleDecision
    // =============================================================================
    /// <summary>
    /// <para>
    /// Risultato data-only della valutazione di scheduling runtime della biosfera.
    /// </para>
    ///
    /// <para><b>Principio architetturale: scheduling osservabile prima della mutazione</b></para>
    /// <para>
    /// Il runtime deve poter sapere se la biosfera e' dovuta senza eseguire subito
    /// side effect su <c>World</c>, celle, ArcGraph o NPC. Questa struttura descrive
    /// la decisione temporale e lascia al chiamante futuro la scelta di avanzare lo
    /// stato biologico, produrre delta o rimandare lavoro.
    /// </para>
    ///
    /// <para><b>Struttura interna:</b></para>
    /// <list type="bullet">
    ///   <item><b>IsEnabled</b>: copia del gate runtime normalizzato.</item>
    ///   <item><b>ShouldAdvance</b>: indica se il tick corrente deve produrre update biosfera.</item>
    ///   <item><b>DueUpdateCount</b>: numero di update giornalieri maturati nel salto temporale.</item>
    ///   <item><b>PreviousEnvironmentTicks</b>: tick ambientale da cui partirebbe il resolver biologico.</item>
    ///   <item><b>CurrentEnvironmentTicks</b>: tick ambientale fino a cui arriverebbe il resolver biologico.</item>
    ///   <item><b>NextDueSimulationTick</b>: prossimo tick SimulationHost candidato.</item>
    /// </list>
    /// </summary>
    public readonly struct EnvironmentRuntimeScheduleDecision
    {
        public readonly bool IsEnabled;
        public readonly bool ShouldAdvance;
        public readonly int SimulationTicksPerDailyUpdate;
        public readonly int DueUpdateCount;
        public readonly long LastProcessedSimulationTick;
        public readonly long CurrentSimulationTick;
        public readonly long PreviousEnvironmentTicks;
        public readonly long CurrentEnvironmentTicks;
        public readonly long NextDueSimulationTick;
        public readonly string UpdateMode;

        // =============================================================================
        // EnvironmentRuntimeScheduleDecision
        // =============================================================================
        /// <summary>
        /// <para>
        /// Costruisce una decisione di scheduling biosfera gia' normalizzata.
        /// </para>
        /// </summary>
        public EnvironmentRuntimeScheduleDecision(
            bool isEnabled,
            bool shouldAdvance,
            int simulationTicksPerDailyUpdate,
            int dueUpdateCount,
            long lastProcessedSimulationTick,
            long currentSimulationTick,
            long previousEnvironmentTicks,
            long currentEnvironmentTicks,
            long nextDueSimulationTick,
            string updateMode)
        {
            IsEnabled = isEnabled;
            ShouldAdvance = shouldAdvance;
            SimulationTicksPerDailyUpdate = simulationTicksPerDailyUpdate;
            DueUpdateCount = dueUpdateCount;
            LastProcessedSimulationTick = lastProcessedSimulationTick;
            CurrentSimulationTick = currentSimulationTick;
            PreviousEnvironmentTicks = previousEnvironmentTicks;
            CurrentEnvironmentTicks = currentEnvironmentTicks;
            NextDueSimulationTick = nextDueSimulationTick;
            UpdateMode = updateMode ?? BiosphereRuntimeParams.DefaultUpdateMode;
        }
    }

    // =============================================================================
    // EnvironmentRuntimeScheduler
    // =============================================================================
    /// <summary>
    /// <para>
    /// Scheduler data-only della biosfera rispetto al tick globale di simulazione.
    /// </para>
    ///
    /// <para><b>Principio architetturale: SimulationHost come clock, non come logica biologica</b></para>
    /// <para>
    /// Il <c>SimulationHost</c> futuro dovra' soltanto fornire il tick corrente e
    /// conservare il tick biosfera processato piu' recente. La decisione su cadenza,
    /// update maturati e prossimo confine resta qui, in un modulo Core testabile e
    /// privo di dipendenze da Unity scene, MapGrid o ArcGraph.
    /// </para>
    ///
    /// <para><b>Struttura interna:</b></para>
    /// <list type="bullet">
    ///   <item><b>Evaluate</b>: calcola se un update e' dovuto.</item>
    ///   <item><b>ResolveProcessedTickAfterDecision</b>: normalizza il nuovo ultimo tick processato.</item>
    ///   <item><b>ClampNonNegative</b>: protegge input temporali invalidi.</item>
    /// </list>
    /// </summary>
    public static class EnvironmentRuntimeScheduler
    {
        // =============================================================================
        // Evaluate
        // =============================================================================
        /// <summary>
        /// <para>
        /// Valuta se il tick corrente deve produrre un avanzamento biosfera.
        /// </para>
        /// </summary>
        public static EnvironmentRuntimeScheduleDecision Evaluate(
            BiosphereRuntimeParams runtimeParams,
            long lastProcessedSimulationTick,
            long currentSimulationTick)
        {
            var config = BiosphereRuntimeParams.WithFallbackDefaults(runtimeParams);
            int cadenceTicks = config.ResolveSimulationTicksPerDailyUpdate();
            long safeLastProcessedTick = ClampNonNegative(lastProcessedSimulationTick);
            long safeCurrentTick = ClampNonNegative(currentSimulationTick);
            long nextDueTick = safeLastProcessedTick + cadenceTicks;

            if (!config.enabled)
            {
                return new EnvironmentRuntimeScheduleDecision(
                    false,
                    false,
                    cadenceTicks,
                    0,
                    safeLastProcessedTick,
                    safeCurrentTick,
                    safeLastProcessedTick,
                    safeLastProcessedTick,
                    nextDueTick,
                    config.ResolveUpdateMode());
            }

            long elapsedTicks = safeCurrentTick - safeLastProcessedTick;
            bool shouldAdvance = elapsedTicks >= cadenceTicks;
            int dueUpdateCount = shouldAdvance
                ? (int)(elapsedTicks / cadenceTicks)
                : 0;

            // Per ora l'Environment Foundation usa la stessa unita' numerica del tick
            // SimulationHost. Il ponte futuro potra' convertire qui se introdurremo
            // scale temporali separate, senza cambiare resolver biologici.
            long currentEnvironmentTicks = shouldAdvance
                ? safeLastProcessedTick + ((long)dueUpdateCount * cadenceTicks)
                : safeLastProcessedTick;

            return new EnvironmentRuntimeScheduleDecision(
                true,
                shouldAdvance,
                cadenceTicks,
                dueUpdateCount,
                safeLastProcessedTick,
                safeCurrentTick,
                safeLastProcessedTick,
                currentEnvironmentTicks,
                currentEnvironmentTicks + cadenceTicks,
                config.ResolveUpdateMode());
        }

        // =============================================================================
        // ResolveProcessedTickAfterDecision
        // =============================================================================
        /// <summary>
        /// <para>
        /// Restituisce il tick biosfera da memorizzare dopo una decisione.
        /// </para>
        /// </summary>
        public static long ResolveProcessedTickAfterDecision(
            EnvironmentRuntimeScheduleDecision decision)
        {
            return decision.ShouldAdvance
                ? decision.CurrentEnvironmentTicks
                : decision.LastProcessedSimulationTick;
        }

        // =============================================================================
        // ClampNonNegative
        // =============================================================================
        /// <summary>
        /// <para>
        /// Normalizza i tick negativi a zero per proteggere bootstrap e test.
        /// </para>
        /// </summary>
        private static long ClampNonNegative(long tick)
        {
            return tick < 0 ? 0 : tick;
        }
    }

    // =============================================================================
    // EnvironmentAdvanceResult
    // =============================================================================
    /// <summary>
    /// <para>
    /// Risultato aggregato di un avanzamento data-only della foundation ambientale.
    /// </para>
    ///
    /// <para><b>Principio architetturale: orchestration senza ownership runtime</b></para>
    /// <para>
    /// Questo risultato raccoglie i dati prodotti da resolver puri: transizione
    /// temporale, calendario, clima, profilo stagionale, stato, snapshot aggiornato e
    /// diff rispetto allo snapshot sorgente. Non rappresenta un sistema attivo e non
    /// conserva lifecycle.
    /// </para>
    ///
    /// <para><b>Struttura interna:</b></para>
    /// <list type="bullet">
    ///   <item><b>Transition</b>: confine temporale risolto.</item>
    ///   <item><b>Climate</b>: clima globale corrente.</item>
    ///   <item><b>SeasonProfile</b>: profilo stagionale corrente.</item>
    ///   <item><b>State</b>: stato ambientale avanzato.</item>
    ///   <item><b>Snapshot</b>: snapshot read-only dello stato avanzato.</item>
    ///   <item><b>EvolutionReport</b>: diagnostica dell'evoluzione batch.</item>
    ///   <item><b>SnapshotDiff</b>: differenze tra snapshot sorgente e snapshot avanzato.</item>
    /// </list>
    /// </summary>
    public sealed class EnvironmentAdvanceResult
    {
        private static readonly EnvironmentPhysicalPlantDelta[] EmptyPhysicalPlantDeltas =
            new EnvironmentPhysicalPlantDelta[0];
        private static readonly EnvironmentDiffuseVegetationDelta[] EmptyDiffuseVegetationDeltas =
            new EnvironmentDiffuseVegetationDelta[0];

        public EnvironmentTemporalTransition Transition { get; }
        public EnvironmentGlobalClimateState Climate { get; }
        public EnvironmentSeasonProfile SeasonProfile { get; }
        public EnvironmentState State { get; }
        public EnvironmentSnapshot Snapshot { get; }
        public EnvironmentSnapshotEvolutionReport EvolutionReport { get; }
        public EnvironmentSnapshotDiffResult SnapshotDiff { get; }
        public IReadOnlyList<EnvironmentPhysicalPlantDelta> PhysicalPlantDeltas { get; }
        public IReadOnlyList<EnvironmentDiffuseVegetationDelta> DiffuseVegetationDeltas { get; }

        // =============================================================================
        // EnvironmentAdvanceResult
        // =============================================================================
        /// <summary>
        /// <para>
        /// Costruisce il risultato aggregando i prodotti dell'avanzamento.
        /// </para>
        /// </summary>
        public EnvironmentAdvanceResult(
            EnvironmentTemporalTransition transition,
            EnvironmentGlobalClimateState climate,
            EnvironmentSeasonProfile seasonProfile,
            EnvironmentState state,
            EnvironmentSnapshot snapshot,
            EnvironmentSnapshotEvolutionReport evolutionReport,
            EnvironmentSnapshotDiffResult snapshotDiff,
            IReadOnlyList<EnvironmentPhysicalPlantDelta> physicalPlantDeltas = null,
            IReadOnlyList<EnvironmentDiffuseVegetationDelta> diffuseVegetationDeltas = null)
        {
            Transition = transition;
            Climate = climate;
            SeasonProfile = seasonProfile;
            State = state ?? new EnvironmentState();
            Snapshot = snapshot ?? State.CreateSnapshot();
            EvolutionReport = evolutionReport;
            SnapshotDiff = snapshotDiff
                           ?? new EnvironmentSnapshotDiffResult(
                               new EnvironmentSnapshotAreaChange[0]);
            PhysicalPlantDeltas = physicalPlantDeltas ?? EmptyPhysicalPlantDeltas;
            DiffuseVegetationDeltas = diffuseVegetationDeltas ?? EmptyDiffuseVegetationDeltas;
        }
    }

    // =============================================================================
    // EnvironmentAdvanceResolver
    // =============================================================================
    /// <summary>
    /// <para>
    /// Resolver data-only per avanzare uno snapshot ambientale tra due tick.
    /// </para>
    ///
    /// <para><b>Principio architetturale: pipeline Core componibile</b></para>
    /// <para>
    /// Il resolver unisce componenti gia' separati: calendario, transizione, clima,
    /// profilo stagionale ed evoluzione snapshot. Non decide quando girare e non
    /// possiede lo stato sorgente: il chiamante fornisce input espliciti.
    /// </para>
    ///
    /// <para><b>Struttura interna:</b></para>
    /// <list type="bullet">
    ///   <item><b>AdvanceSnapshot</b>: avanza da snapshot sorgente a nuovo stato.</item>
    ///   <item><b>AdvanceStateSnapshot</b>: usa direttamente lo snapshot di uno stato esistente.</item>
    /// </list>
    /// </summary>
    public static class EnvironmentAdvanceResolver
    {
        // =============================================================================
        // AdvanceSnapshot
        // =============================================================================
        /// <summary>
        /// <para>
        /// Avanza uno snapshot ambientale da tick precedente a tick corrente.
        /// </para>
        /// </summary>
        public static EnvironmentAdvanceResult AdvanceSnapshot(
            EnvironmentSnapshot sourceSnapshot,
            long previousEnvironmentTicks,
            long currentEnvironmentTicks,
            EnvironmentCalendarConfig calendarConfig,
            EnvironmentClimateConfig climateConfig)
        {
            var safeCalendarConfig = calendarConfig ?? new EnvironmentCalendarConfig();
            var safeClimateConfig = climateConfig ?? new EnvironmentClimateConfig();
            var transition = EnvironmentTemporalTransitionResolver.Resolve(
                previousEnvironmentTicks,
                currentEnvironmentTicks,
                safeCalendarConfig);
            var climate = EnvironmentClimateResolver.Resolve(
                transition.Current,
                safeClimateConfig);
            var seasonProfile = EnvironmentCalendarResolver.ResolveSeasonProfile(
                safeCalendarConfig,
                transition.Current.Date.Season);

            // L'evoluzione batch e' l'unico punto che materializza il nuovo stato; il
            // resolver di avanzamento si limita a preparare input coerenti.
            var evolution = EnvironmentSnapshotEvolutionResolver.EvolveSnapshot(
                sourceSnapshot,
                transition,
                climate,
                seasonProfile);
            var snapshot = evolution.State.CreateSnapshot();
            var diff = EnvironmentSnapshotDiffResolver.Diff(
                sourceSnapshot,
                snapshot);
            var physicalPlantDeltas = EnvironmentPhysicalPlantDeltaProducer.DiffSnapshots(
                sourceSnapshot,
                snapshot);

            return new EnvironmentAdvanceResult(
                transition,
                climate,
                seasonProfile,
                evolution.State,
                snapshot,
                evolution.Report,
                diff,
                physicalPlantDeltas);
        }

        // =============================================================================
        // AdvanceStateSnapshot
        // =============================================================================
        /// <summary>
        /// <para>
        /// Avanza lo snapshot prodotto da uno stato ambientale esistente.
        /// </para>
        /// </summary>
        public static EnvironmentAdvanceResult AdvanceStateSnapshot(
            EnvironmentState sourceState,
            long currentEnvironmentTicks,
            EnvironmentCalendarConfig calendarConfig,
            EnvironmentClimateConfig climateConfig)
        {
            var sourceSnapshot = sourceState != null
                ? sourceState.CreateSnapshot()
                : new EnvironmentState().CreateSnapshot();
            long previousTicks = sourceSnapshot.Calendar.ElapsedEnvironmentTicks;

            return AdvanceSnapshot(
                sourceSnapshot,
                previousTicks,
                currentEnvironmentTicks,
                calendarConfig,
                climateConfig);
        }
    }
}
