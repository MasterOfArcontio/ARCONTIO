using Arcontio.Core.Config;

namespace Arcontio.Core
{
    // =============================================================================
    // JobStateMachineTickResult
    // =============================================================================
    /// <summary>
    /// <para>
    /// Esito di avanzamento prodotto dalla macchina a stati del Job System.
    /// </para>
    ///
    /// <para><b>State machine separata dagli executor</b></para>
    /// <para>
    /// Gli executor producono <c>StepResult</c>. La state machine interpreta quel
    /// risultato e aggiorna il cursore per-NPC. Questo separa esecuzione concreta e
    /// ciclo di vita del job.
    /// </para>
    ///
    /// <para><b>Struttura interna:</b></para>
    /// <list type="bullet">
    ///   <item><b>NoActiveJob</b>: nessun job da avanzare.</item>
    ///   <item><b>Waiting</b>: il job resta fermo fino a un tick futuro.</item>
    ///   <item><b>ActionAdvanced</b>: completato uno step dentro la fase.</item>
    ///   <item><b>PhaseAdvanced</b>: completata una fase, passaggio alla successiva.</item>
    ///   <item><b>JobCompleted</b>: completato l'intero piano.</item>
    ///   <item><b>JobFailed</b>: fallimento terminale.</item>
    /// </list>
    /// </summary>
    public enum JobStateMachineTickResult
    {
        NoActiveJob = 0,
        Waiting = 10,
        Running = 20,
        ActionAdvanced = 30,
        PhaseAdvanced = 40,
        JobCompleted = 50,
        JobFailed = 60
    }

    // =============================================================================
    // JobStateMachineResult
    // =============================================================================
    /// <summary>
    /// <para>
    /// Risultato diagnosticabile di un avanzamento della macchina a stati.
    /// </para>
    ///
    /// <para><b>Telemetria pronta per runtime JSONL</b></para>
    /// <para>
    /// Conservare esito e ragione permette di loggare la progressione del job senza
    /// dover dedurre il significato dalle mutazioni del cursore.
    /// </para>
    ///
    /// <para><b>Struttura interna:</b></para>
    /// <list type="bullet">
    ///   <item><b>TickResult</b>: classificazione primaria dell'avanzamento.</item>
    ///   <item><b>Message</b>: ragione diagnostica breve.</item>
    ///   <item><b>FailureReason</b>: motivo quando il job fallisce.</item>
    /// </list>
    /// </summary>
    public readonly struct JobStateMachineResult
    {
        public readonly JobStateMachineTickResult TickResult;
        public readonly string Message;
        public readonly JobFailureReason FailureReason;

        public JobStateMachineResult(JobStateMachineTickResult tickResult, string message, JobFailureReason failureReason)
        {
            TickResult = tickResult;
            Message = message ?? string.Empty;
            FailureReason = failureReason;
        }
    }

    // =============================================================================
    // JobStateMachine
    // =============================================================================
    /// <summary>
    /// <para>
    /// Macchina a stati minimale che aggiorna job e cursore NPC in base al risultato
    /// dello step corrente.
    /// </para>
    ///
    /// <para><b>Job execution senza sistemi concreti</b></para>
    /// <para>
    /// Questa classe non calcola path, non consuma cibo e non prenota risorse.
    /// Interpreta soltanto esiti gia' prodotti da executor dedicati, mantenendo
    /// modulare il livello di esecuzione.
    /// </para>
    ///
    /// <para><b>Struttura interna:</b></para>
    /// <list type="bullet">
    ///   <item><b>Wait gate</b>: rispetta WaitUntilTick prima di avanzare.</item>
    ///   <item><b>Running</b>: mantiene il cursore invariato.</item>
    ///   <item><b>Succeeded</b>: avanza action, fase o job.</item>
    ///   <item><b>Waiting/Blocked</b>: imposta un retry tick.</item>
    ///   <item><b>Failed</b>: chiude job e stato NPC con ragione esplicita.</item>
    /// </list>
    /// </summary>
    public sealed class JobStateMachine
    {
        // =============================================================================
        // ApplyStepResult
        // =============================================================================
        /// <summary>
        /// <para>
        /// Overload EL-aware che applica il risultato dello step e, quando possibile,
        /// emette trace diagnostiche del ciclo di vita del job.
        /// </para>
        ///
        /// <para><b>Strumentazione opzionale e non invasiva</b></para>
        /// <para>
        /// Il Job System non dipende dall'Explainability Layer: il vecchio overload
        /// resta disponibile e questo percorso aggiunge solo emissione one-way verso
        /// registry e JSONL quando il chiamante possiede gia' config e registry.
        /// </para>
        ///
        /// <para><b>Struttura interna:</b></para>
        /// <list type="bullet">
        ///   <item><b>Status before</b>: cattura lo stato pre-mutation del job.</item>
        ///   <item><b>Base apply</b>: delega all'overload storico.</item>
        ///   <item><b>Lifecycle emit</b>: emette trace solo se la transizione lo richiede.</item>
        /// </list>
        /// </summary>
        public JobStateMachineResult ApplyStepResult(
            ref NpcJobState npcState,
            Job job,
            StepResult stepResult,
            int tick,
            MemoryBeliefDecisionExplainabilityParams explainabilityConfig,
            MemoryBeliefDecisionExplainabilityRegistry explainabilityRegistry,
            int npcId)
        {
            var previousStatus = job != null ? job.Status : JobStatus.Cancelled;
            var previousPhaseIndex = npcState.ActivePhaseIndex;
            var hadPreviousPhase = job != null && job.Plan.TryGetPhase(previousPhaseIndex, out var previousPhase);
            var result = ApplyStepResult(ref npcState, job, stepResult, tick);

            TryEmitLifecycleTrace(
                explainabilityConfig,
                explainabilityRegistry,
                npcId,
                tick,
                job,
                previousStatus,
                result);

            TryEmitPhaseTrace(
                explainabilityConfig,
                explainabilityRegistry,
                npcId,
                tick,
                job,
                hadPreviousPhase ? previousPhase : default,
                previousPhaseIndex,
                result);

            return result;
        }

        public JobStateMachineResult ApplyStepResult(ref NpcJobState npcState, Job job, StepResult stepResult, int tick)
        {
            // Senza job attivo non c'e' nulla da avanzare: restituiamo un esito
            // neutro, utile per sistemi che iterano tutti gli NPC.
            if (!npcState.HasActiveJob || job == null)
                return new JobStateMachineResult(JobStateMachineTickResult.NoActiveJob, "NoActiveJob", JobFailureReason.None);

            // Se un'attesa e' ancora in corso, non interpretiamo un nuovo risultato:
            // lo step concreto non dovrebbe essere rieseguito prima del tick limite.
            if (npcState.IsWaitingAt(tick))
                return new JobStateMachineResult(JobStateMachineTickResult.Waiting, "WaitGateActive", JobFailureReason.None);

            if (stepResult.Status == StepResultStatus.Running)
            {
                job.MarkRunning(tick);
                return new JobStateMachineResult(JobStateMachineTickResult.Running, stepResult.DiagnosticMessage, JobFailureReason.None);
            }

            if (stepResult.Status == StepResultStatus.Waiting || stepResult.Status == StepResultStatus.Blocked)
            {
                npcState.SetWaitingUntil(tick + stepResult.SuggestedWaitTicks);
                job.MarkRunning(tick);
                return new JobStateMachineResult(JobStateMachineTickResult.Waiting, stepResult.DiagnosticMessage, JobFailureReason.None);
            }

            if (stepResult.Status == StepResultStatus.Failed)
            {
                job.MarkFailed(stepResult.FailureReason, tick);
                npcState.Clear(stepResult.FailureReason);
                return new JobStateMachineResult(JobStateMachineTickResult.JobFailed, stepResult.DiagnosticMessage, stepResult.FailureReason);
            }

            return AdvanceOnSuccess(ref npcState, job, tick, stepResult.DiagnosticMessage);
        }

        private static JobStateMachineResult AdvanceOnSuccess(ref NpcJobState npcState, Job job, int tick, string message)
        {
            // Se la fase corrente non esiste, consideriamo il piano incoerente e
            // chiudiamo con fallimento invece di camminare fuori dai limiti.
            if (!job.Plan.TryGetPhase(npcState.ActivePhaseIndex, out var phase))
            {
                job.MarkFailed(JobFailureReason.MissingPlan, tick);
                npcState.Clear(JobFailureReason.MissingPlan);
                return new JobStateMachineResult(JobStateMachineTickResult.JobFailed, "MissingPhase", JobFailureReason.MissingPlan);
            }

            var nextActionIndex = npcState.ActiveActionIndex + 1;

            // Se esiste una prossima azione nella stessa fase, avanziamo solo lo
            // step: il mini job resta quello corrente.
            if (nextActionIndex < phase.ExpectedStepCount)
            {
                npcState.AdvanceAction();
                job.MarkRunning(tick);
                return new JobStateMachineResult(JobStateMachineTickResult.ActionAdvanced, message, JobFailureReason.None);
            }

            var nextPhaseIndex = npcState.ActivePhaseIndex + 1;

            // Se esiste una prossima fase, il mini job corrente e' concluso e il
            // cursore riparte dalla prima azione della fase successiva.
            if (nextPhaseIndex < job.Plan.PhaseCount)
            {
                npcState.AdvancePhase();
                job.MoveToPhase(nextPhaseIndex, tick);
                job.MarkRunning(tick);
                return new JobStateMachineResult(JobStateMachineTickResult.PhaseAdvanced, message, JobFailureReason.None);
            }

            // Nessuna azione e nessuna fase successive: il piano e' finito.
            job.MarkCompleted(tick);
            npcState.Clear(JobFailureReason.None);
            return new JobStateMachineResult(JobStateMachineTickResult.JobCompleted, message, JobFailureReason.None);
        }

        // =============================================================================
        // TryEmitLifecycleTrace
        // =============================================================================
        /// <summary>
        /// <para>
        /// Traduce il risultato della state machine in una trace lifecycle EL quando
        /// osserva una transizione semanticamente rilevante.
        /// </para>
        ///
        /// <para><b>Lifecycle derivato dal punto di ownership</b></para>
        /// <para>
        /// La state machine e' il punto che conosce davvero la transizione di stato,
        /// quindi e' qui che l'EL deve leggere created/running/completed/failed senza
        /// ricostruire la sequenza da cambiamenti sparsi nel runtime.
        /// </para>
        /// </summary>
        private static void TryEmitLifecycleTrace(
            MemoryBeliefDecisionExplainabilityParams explainabilityConfig,
            MemoryBeliefDecisionExplainabilityRegistry explainabilityRegistry,
            int npcId,
            int tick,
            Job job,
            JobStatus previousStatus,
            JobStateMachineResult result)
        {
            if (explainabilityConfig == null || job == null)
                return;

            var operation = ResolveLifecycleOperation(previousStatus, job.Status, result.TickResult);
            if (operation == MemoryBeliefDecisionJobLifecycleOperation.Unknown)
                return;

            MemoryBeliefDecisionExplainabilityEmitter.TryWriteJobLifecycleTrace(
                explainabilityConfig,
                explainabilityRegistry,
                npcId,
                tick,
                operation,
                job,
                result.Message);
        }

        // =============================================================================
        // ResolveLifecycleOperation
        // =============================================================================
        /// <summary>
        /// <para>
        /// Mappa stato precedente, stato attuale ed esito della state machine in una
        /// operazione lifecycle EL stabile.
        /// </para>
        /// </summary>
        private static MemoryBeliefDecisionJobLifecycleOperation ResolveLifecycleOperation(
            JobStatus previousStatus,
            JobStatus currentStatus,
            JobStateMachineTickResult tickResult)
        {
            if (previousStatus == JobStatus.Created && currentStatus == JobStatus.Running)
                return MemoryBeliefDecisionJobLifecycleOperation.Activated;

            if (tickResult == JobStateMachineTickResult.JobCompleted && currentStatus == JobStatus.Completed)
                return MemoryBeliefDecisionJobLifecycleOperation.Completed;

            if (tickResult == JobStateMachineTickResult.JobFailed && currentStatus == JobStatus.Failed)
                return MemoryBeliefDecisionJobLifecycleOperation.Failed;

            return MemoryBeliefDecisionJobLifecycleOperation.Unknown;
        }

        // =============================================================================
        // TryEmitPhaseTrace
        // =============================================================================
        /// <summary>
        /// <para>
        /// Traduce l'avanzamento della state machine in trace di fase EL.
        /// </para>
        ///
        /// <para><b>Fase letta nel punto di transizione reale</b></para>
        /// <para>
        /// La state machine conosce se una fase e' stata conclusa, interrotta oppure
        /// se il cursore e' appena entrato nella successiva. E' quindi il punto
        /// corretto per produrre trace di fase senza ricostruzioni ex-post.
        /// </para>
        /// </summary>
        private static void TryEmitPhaseTrace(
            MemoryBeliefDecisionExplainabilityParams explainabilityConfig,
            MemoryBeliefDecisionExplainabilityRegistry explainabilityRegistry,
            int npcId,
            int tick,
            Job job,
            JobPhase previousPhase,
            int previousPhaseIndex,
            JobStateMachineResult result)
        {
            if (explainabilityConfig == null || job == null)
                return;

            if (result.TickResult == JobStateMachineTickResult.PhaseAdvanced)
            {
                MemoryBeliefDecisionExplainabilityEmitter.TryWriteJobPhaseTrace(
                    explainabilityConfig,
                    explainabilityRegistry,
                    npcId,
                    tick,
                    MemoryBeliefDecisionJobPhaseOperation.Completed,
                    job,
                    previousPhase,
                    previousPhaseIndex,
                    result.Message);

                if (job.Plan.TryGetPhase(job.ActivePhaseIndex, out var enteredPhase))
                {
                    MemoryBeliefDecisionExplainabilityEmitter.TryWriteJobPhaseTrace(
                        explainabilityConfig,
                        explainabilityRegistry,
                        npcId,
                        tick,
                        MemoryBeliefDecisionJobPhaseOperation.Entered,
                        job,
                        enteredPhase,
                        job.ActivePhaseIndex,
                        "EnteredNextPhase");
                }

                return;
            }

            if (result.TickResult == JobStateMachineTickResult.JobCompleted)
            {
                MemoryBeliefDecisionExplainabilityEmitter.TryWriteJobPhaseTrace(
                    explainabilityConfig,
                    explainabilityRegistry,
                    npcId,
                    tick,
                    MemoryBeliefDecisionJobPhaseOperation.Completed,
                    job,
                    previousPhase,
                    previousPhaseIndex,
                    result.Message);
                return;
            }

            if (result.TickResult == JobStateMachineTickResult.JobFailed)
            {
                MemoryBeliefDecisionExplainabilityEmitter.TryWriteJobPhaseTrace(
                    explainabilityConfig,
                    explainabilityRegistry,
                    npcId,
                    tick,
                    MemoryBeliefDecisionJobPhaseOperation.Interrupted,
                    job,
                    previousPhase,
                    previousPhaseIndex,
                    result.Message);
            }
        }
    }
}
