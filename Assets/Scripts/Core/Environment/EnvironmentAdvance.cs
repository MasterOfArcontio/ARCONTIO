namespace Arcontio.Core.Environment
{
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
        public EnvironmentTemporalTransition Transition { get; }
        public EnvironmentGlobalClimateState Climate { get; }
        public EnvironmentSeasonProfile SeasonProfile { get; }
        public EnvironmentState State { get; }
        public EnvironmentSnapshot Snapshot { get; }
        public EnvironmentSnapshotEvolutionReport EvolutionReport { get; }
        public EnvironmentSnapshotDiffResult SnapshotDiff { get; }

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
            EnvironmentSnapshotDiffResult snapshotDiff)
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
            EnvironmentClimateConfig climateConfig,
            EnvironmentBiomeProfile biomeProfile = default)
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
                seasonProfile,
                biomeProfile);
            var snapshot = evolution.State.CreateSnapshot();
            var diff = EnvironmentSnapshotDiffResolver.Diff(
                sourceSnapshot,
                snapshot);

            return new EnvironmentAdvanceResult(
                transition,
                climate,
                seasonProfile,
                evolution.State,
                snapshot,
                evolution.Report,
                diff);
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
            EnvironmentClimateConfig climateConfig,
            EnvironmentBiomeProfile biomeProfile = default)
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
                climateConfig,
                biomeProfile);
        }
    }
}
