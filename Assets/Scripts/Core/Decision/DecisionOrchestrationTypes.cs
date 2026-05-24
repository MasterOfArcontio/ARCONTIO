namespace Arcontio.Core
{
    // =============================================================================
    // NpcDecisionEligibilityReason
    // =============================================================================
    /// <summary>
    /// <para>
    /// Motivo normalizzato con cui il futuro scheduler decisionale dichiara se un
    /// NPC puo' attraversare una rivalutazione cognitiva nel tick corrente.
    /// </para>
    ///
    /// <para><b>ARC-DEC-019 - Eligibility cognitiva, non preemption</b></para>
    /// <para>
    /// Questi valori non sono decisioni di arbitraggio Job e non autorizzano alcuna
    /// transizione sul job attivo. Servono solo a distinguere perche' il Decision
    /// Orchestrator potra', in futuro, invocare o saltare la pipeline cognitiva.
    /// L'autorita' di accettare, rifiutare o preemptare un job resta nel Job Layer.
    /// </para>
    ///
    /// <para><b>Struttura interna:</b></para>
    /// <list type="bullet">
    ///   <item><b>CadenceNotDue</b>: il tick cognitivo non e' ancora maturo.</item>
    ///   <item><b>NoActiveJob</b>: l'NPC non ha job attivo e puo' rivalutare normalmente.</item>
    ///   <item><b>ActiveJobDefersRoutineDecision</b>: un job attivo rinvia rivalutazioni ordinarie.</item>
    ///   <item><b>ActiveJobMayEvaluateForHigherPriorityIntent</b>: il job attivo non blocca una possibile intenzione superiore.</item>
    ///   <item><b>ActiveJobMayEvaluateForEmergencyIntent</b>: il job attivo non blocca una possibile emergenza cognitiva.</item>
    /// </list>
    /// </summary>
    public enum NpcDecisionEligibilityReason
    {
        None = 0,
        CadenceNotDue = 10,
        NoActiveJob = 20,
        ActiveJobDefersRoutineDecision = 30,
        ActiveJobMayEvaluateForHigherPriorityIntent = 40,
        ActiveJobMayEvaluateForEmergencyIntent = 50
    }

    // =============================================================================
    // NpcDecisionSchedulerInput
    // =============================================================================
    /// <summary>
    /// <para>
    /// Input minimale e dichiarativo per valutare la sola eleggibilita' cognitiva di
    /// un NPC nel futuro Decision Orchestrator.
    /// </para>
    ///
    /// <para><b>Nessun accesso a World come conoscenza cognitiva</b></para>
    /// <para>
    /// La struttura contiene soltanto segnali gia' risolti dal chiamante: tick,
    /// cadence, presenza di job attivo e flag astratti su possibili urgenze
    /// cognitive. Non contiene riferimenti a <c>World</c>, store oggettivi,
    /// <c>MemoryStore</c>, oggetti, food stock o command buffer.
    /// </para>
    ///
    /// <para><b>Struttura interna:</b></para>
    /// <list type="bullet">
    ///   <item><b>NpcId</b>: identita' diagnostica dell'NPC.</item>
    ///   <item><b>Tick</b>: tick corrente gia' fornito dall'orchestratore temporale.</item>
    ///   <item><b>LastDecisionTick</b>: ultimo tick cognitivo osservato dal chiamante.</item>
    ///   <item><b>DecisionCadenceTicks</b>: intervallo minimo tra rivalutazioni ordinarie.</item>
    ///   <item><b>HasActiveJob</b>: segnala un job attivo senza esporne stato mutabile.</item>
    ///   <item><b>HasHigherPriorityIntentSignal</b>: abilita una rivalutazione per possibile intenzione superiore.</item>
    ///   <item><b>HasEmergencyIntentSignal</b>: abilita una rivalutazione per possibile emergenza.</item>
    /// </list>
    /// </summary>
    public readonly struct NpcDecisionSchedulerInput
    {
        public readonly int NpcId;
        public readonly int Tick;
        public readonly int LastDecisionTick;
        public readonly int DecisionCadenceTicks;
        public readonly bool HasActiveJob;
        public readonly bool HasHigherPriorityIntentSignal;
        public readonly bool HasEmergencyIntentSignal;

        public NpcDecisionSchedulerInput(
            int npcId,
            int tick,
            int lastDecisionTick,
            int decisionCadenceTicks,
            bool hasActiveJob,
            bool hasHigherPriorityIntentSignal,
            bool hasEmergencyIntentSignal)
        {
            NpcId = npcId;
            Tick = tick;
            LastDecisionTick = lastDecisionTick;
            DecisionCadenceTicks = decisionCadenceTicks;
            HasActiveJob = hasActiveJob;
            HasHigherPriorityIntentSignal = hasHigherPriorityIntentSignal;
            HasEmergencyIntentSignal = hasEmergencyIntentSignal;
        }
    }

    // =============================================================================
    // NpcDecisionEligibility
    // =============================================================================
    /// <summary>
    /// <para>
    /// Esito passivo del gate di eleggibilita' cognitiva per un NPC.
    /// </para>
    ///
    /// <para><b>Snapshot diagnostico senza autorita' runtime</b></para>
    /// <para>
    /// L'esito dice soltanto se il Decision Orchestrator puo' proseguire con una
    /// rivalutazione cognitiva. Non assegna job, non cancella job correnti, non
    /// produce command e non decide preemption. Il campo <c>AllowsEvaluation</c>
    /// abilita solo lavoro decisionale futuro, non lavoro esecutivo.
    /// </para>
    ///
    /// <para><b>Struttura interna:</b></para>
    /// <list type="bullet">
    ///   <item><b>AllowsEvaluation</b>: true se la pipeline cognitiva puo' essere invocata.</item>
    ///   <item><b>Reason</b>: ragione normalizzata, stabile per test e explainability futura.</item>
    ///   <item><b>DiagnosticLabel</b>: testo breve per log o pannelli futuri.</item>
    /// </list>
    /// </summary>
    public readonly struct NpcDecisionEligibility
    {
        public readonly bool AllowsEvaluation;
        public readonly NpcDecisionEligibilityReason Reason;
        public readonly string DiagnosticLabel;

        public NpcDecisionEligibility(
            bool allowsEvaluation,
            NpcDecisionEligibilityReason reason,
            string diagnosticLabel)
        {
            AllowsEvaluation = allowsEvaluation;
            Reason = reason;
            DiagnosticLabel = diagnosticLabel ?? string.Empty;
        }
    }

    // =============================================================================
    // DecisionOrchestrationResult
    // =============================================================================
    /// <summary>
    /// <para>
    /// Risultato no-op del futuro Decision Orchestrator per un singolo NPC.
    /// </para>
    ///
    /// <para><b>Skeleton v0.11c.01a senza comportamento produttivo</b></para>
    /// <para>
    /// La struttura rende esplicito cosa il nuovo layer potra' comunicare senza
    /// mutare il runtime attuale. In questa fase non contiene un <c>JobRequest</c>,
    /// non contiene <c>ICommand</c> e non rappresenta una transizione del Job Layer.
    /// </para>
    ///
    /// <para><b>Struttura interna:</b></para>
    /// <list type="bullet">
    ///   <item><b>NpcId/Tick</b>: identita' e tempo della valutazione no-op.</item>
    ///   <item><b>Eligibility</b>: esito del gate cognitivo.</item>
    ///   <item><b>PipelineInvoked</b>: resta false nello skeleton no-op.</item>
    ///   <item><b>HasSelectedIntent</b>: resta false finche' la pipeline non sara' cablata.</item>
    ///   <item><b>HasJobRequestProposal</b>: resta false in v0.11c.01a.</item>
    /// </list>
    /// </summary>
    public readonly struct DecisionOrchestrationResult
    {
        public readonly int NpcId;
        public readonly int Tick;
        public readonly NpcDecisionEligibility Eligibility;
        public readonly bool PipelineInvoked;
        public readonly bool HasSelectedIntent;
        public readonly DecisionIntentKind SelectedIntent;
        public readonly bool HasJobRequestProposal;

        public DecisionOrchestrationResult(
            int npcId,
            int tick,
            NpcDecisionEligibility eligibility,
            bool pipelineInvoked,
            bool hasSelectedIntent,
            DecisionIntentKind selectedIntent,
            bool hasJobRequestProposal)
        {
            NpcId = npcId;
            Tick = tick;
            Eligibility = eligibility;
            PipelineInvoked = pipelineInvoked;
            HasSelectedIntent = hasSelectedIntent;
            SelectedIntent = selectedIntent;
            HasJobRequestProposal = hasJobRequestProposal;
        }

        public static DecisionOrchestrationResult NoOp(
            int npcId,
            int tick,
            in NpcDecisionEligibility eligibility)
        {
            return new DecisionOrchestrationResult(
                npcId,
                tick,
                eligibility,
                false,
                false,
                default,
                false);
        }
    }
}
