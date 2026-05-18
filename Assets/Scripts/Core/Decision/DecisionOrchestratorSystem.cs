namespace Arcontio.Core
{
    // =============================================================================
    // DecisionOrchestratorSystem
    // =============================================================================
    /// <summary>
    /// <para>
    /// Skeleton no-op del futuro orchestratore decisionale NPC.
    /// </para>
    ///
    /// <para><b>v0.11c.01a - Preparazione senza behavior change</b></para>
    /// <para>
    /// Questo componente non e' cablato nel runtime produttivo e non sostituisce
    /// <c>NeedsDecisionRule</c>. Esiste solo per fissare il punto di ingresso futuro
    /// della pipeline Decision Orchestrator senza alterare il ciclo v0.11B gia'
    /// funzionante.
    /// </para>
    ///
    /// <para><b>Boundary architetturali preservati</b></para>
    /// <para>
    /// Lo skeleton non legge <c>World</c> come conoscenza cognitiva, non emette
    /// <c>ICommand</c>, non produce <c>JobRequest</c>, non implementa preemption e
    /// non migra fallback legacy. L'eventuale rivalutazione durante job attivo viene
    /// rappresentata solo come eleggibilita' cognitiva, non come transizione job.
    /// </para>
    ///
    /// <para><b>Struttura interna:</b></para>
    /// <list type="bullet">
    ///   <item><b>Scheduler</b>: gate cognitivo locale e passivo.</item>
    ///   <item><b>EvaluateNoOp</b>: produce un risultato diagnostico senza invocare pipeline reale.</item>
    ///   <item><b>Result</b>: conserva eligibility, ma lascia false pipeline/intention/job proposal.</item>
    /// </list>
    /// </summary>
    public sealed class DecisionOrchestratorSystem
    {
        private readonly NpcDecisionScheduler _scheduler;

        public DecisionOrchestratorSystem()
            : this(new NpcDecisionScheduler())
        {
        }

        public DecisionOrchestratorSystem(NpcDecisionScheduler scheduler)
        {
            _scheduler = scheduler ?? new NpcDecisionScheduler();
        }

        // =============================================================================
        // EvaluateNoOp
        // =============================================================================
        /// <summary>
        /// <para>
        /// Valuta solo l'eleggibilita' cognitiva di un NPC e restituisce un risultato
        /// no-op.
        /// </para>
        ///
        /// <para><b>Nessuna pipeline decisionale ancora cablata</b></para>
        /// <para>
        /// Anche quando lo scheduler consente la rivalutazione, questo metodo non
        /// chiama <c>DecisionCandidateGenerator</c>, <c>DecisionScoringService</c> o
        /// <c>DecisionSelectionService</c>. Non seleziona intenzioni e non propone
        /// job. Il nome esplicito <c>EvaluateNoOp</c> rende difficile scambiarlo per
        /// il runtime finale.
        /// </para>
        ///
        /// <para><b>Struttura interna:</b></para>
        /// <list type="bullet">
        ///   <item><b>Eligibility</b>: delegata allo scheduler cognitivo.</item>
        ///   <item><b>No-op result</b>: risultato passivo, senza side effect.</item>
        ///   <item><b>Punto futuro</b>: area in cui le patch successive potranno inserire context builder e router.</item>
        /// </list>
        /// </summary>
        public DecisionOrchestrationResult EvaluateNoOp(in NpcDecisionSchedulerInput input)
        {
            var eligibility = _scheduler.EvaluateEligibility(input);
            return DecisionOrchestrationResult.NoOp(input.NpcId, input.Tick, in eligibility);
        }
    }
}
