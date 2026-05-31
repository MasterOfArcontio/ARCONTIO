using System.Collections.Generic;
using UnityEngine;
using Arcontio.Core.Config;
using Arcontio.Core.Diagnostics;

namespace Arcontio.Core
{
    // =============================================================================
    // JobExecutionSystem
    // =============================================================================
    /// <summary>
    /// <para>
    /// Sistema tick-based minimale che avanza i job attivi nella prima vertical slice
    /// food-only.
    /// </para>
    ///
    /// <para><b>Job -> Command, non Job -> World</b></para>
    /// <para>
    /// Il sistema non muta direttamente il mondo simulato. Legge il cursore
    /// per-NPC, interpreta la singola <c>JobAction</c> attiva e accoda eventuali
    /// <c>ICommand</c> nel <c>JobCommandBuffer</c>. La mutazione resta nel command
    /// pump unico di <c>SimulationHost</c>.
    /// </para>
    ///
    /// <para><b>Struttura interna:</b></para>
    /// <list type="bullet">
    ///   <item><b>MoveToCell</b>: usa una running action di traversal quando esiste gia' una route nota, altrimenti mantiene il ponte <c>SetMoveIntentCommand</c>.</item>
    ///   <item><b>Consume</b>: accoda <c>EatFromStockCommand</c> solo quando l'NPC e' sul target.</item>
    ///   <item><b>StateMachine</b>: applica <c>StepResult</c> e completa/fallisce il job.</item>
    /// </list>
    /// </summary>
    public sealed class JobExecutionSystem : ISystem
    {
        private readonly List<int> _activeNpcIds = new();
        private readonly JobStateMachine _stateMachine = new();
        private readonly RunningActionExecutor _runningActionExecutor = new();
        private readonly BeliefUpdater _beliefUpdater = new();
        private readonly StepRecoveryPolicyRegistry _recoveryPolicyRegistry;
        private readonly StepRecoveryEvaluator _recoveryEvaluator;
        private readonly JobTemplateRegistry _jobTemplateRegistry;

        public int Period => 1;

        public JobExecutionSystem()
            : this(StepRecoveryPolicyRegistry.LoadDefault(), new StepRecoveryEvaluator(), JobTemplateRegistry.LoadDefault())
        {
        }

        internal JobExecutionSystem(
            StepRecoveryPolicyRegistry recoveryPolicyRegistry,
            StepRecoveryEvaluator recoveryEvaluator,
            JobTemplateRegistry jobTemplateRegistry = null)
        {
            _recoveryPolicyRegistry = recoveryPolicyRegistry ?? new StepRecoveryPolicyRegistry();
            _recoveryEvaluator = recoveryEvaluator ?? new StepRecoveryEvaluator();
            _jobTemplateRegistry = jobTemplateRegistry ?? new JobTemplateRegistry();
        }

        public void Update(World world, Tick tick, MessageBus bus, Telemetry telemetry)
        {
            if (world?.JobRuntimeState == null)
                return;

            var runtime = world.JobRuntimeState;
            var explainabilityConfig = world.Config?.Sim?.memory_belief_decision_explainability;
            var explainabilityRegistry = world.MemoryBeliefDecisionExplainability;
            runtime.Reservations.PruneExpired((int)tick.Index);
            runtime.CopyNpcIdsWithActiveJobsTo(_activeNpcIds);

            for (int i = 0; i < _activeNpcIds.Count; i++)
            {
                int npcId = _activeNpcIds[i];
                if (!runtime.TryGetActiveJob(npcId, out var npcState, out var job) || job == null)
                    continue;

                var result = ExecuteCurrentAction(
                    world,
                    runtime,
                    _runningActionExecutor,
                    npcId,
                    in npcState,
                    job,
                    bus,
                    (int)tick.Index,
                    explainabilityConfig,
                    explainabilityRegistry);
                var updatedState = npcState;

                result = EvaluateLocalRecovery(
                    world,
                    runtime,
                    npcId,
                    job,
                    ref updatedState,
                result,
                (int)tick.Index,
                telemetry,
                explainabilityConfig,
                explainabilityRegistry,
                out bool replacedByRecoveryJob);

                if (replacedByRecoveryJob)
                    continue;

                // La state machine possiede gia' la semantica di avanzamento
                // lifecycle/phase/step/state. Usare l'overload EL-aware non cambia
                // il risultato operativo: aggiunge soltanto una fotografia
                // osservativa nel registry, cosi' il pannello Job mostra cio' che il
                // runtime ha davvero consumato senza leggere direttamente il World.
                // Assignment, arbitration e reservation restano fuori da questa
                // slice: qui cabliamo solo execution -> lifecycle explainability.
                var machineResult = _stateMachine.ApplyStepResult(
                    ref updatedState,
                    job,
                    result,
                    (int)tick.Index,
                    explainabilityConfig,
                    explainabilityRegistry,
                    npcId);
                if (machineResult.TickResult == JobStateMachineTickResult.JobCompleted)
                {
                    runtime.CompleteCurrentJob(npcId, (int)tick.Index, out _);
                    continue;
                }

                if (machineResult.TickResult == JobStateMachineTickResult.JobFailed)
                {
                    ApplyFoodExecutionFailureCognitiveFeedback(world, npcId, job, result, (int)tick.Index, telemetry);
                    runtime.FailCurrentJob(npcId, machineResult.FailureReason, (int)tick.Index, out _);
                    continue;
                }

                runtime.SetNpcState(npcId, in updatedState);
            }
        }

        // =============================================================================
        // EvaluateLocalRecovery
        // =============================================================================
        /// <summary>
        /// <para>
        /// Collega il risultato dello step corrente alla matrice configurabile di
        /// recupero locale degli incarichi.
        /// </para>
        ///
        /// <para><b>Retry locale bounded, non nuova decisione</b></para>
        /// <para>
        /// Questo metodo e' il primo uso produttivo limitato della configurazione in
        /// <c>Resources/Arcontio/Jobs/job_recovery_policies.json</c>. Classifica il
        /// fallimento, consulta la policy e permette soltanto di trasformare un
        /// <c>Failed</c> recuperabile in <c>Blocked</c> per riprovare lo stesso step.
        /// Non sostituisce target, non ricostruisce fasi, non emette command, non
        /// legge nuovi dati dal <c>World</c> e non sposta autorita' decisionale dentro
        /// il Job runtime.
        /// </para>
        ///
        /// <para><b>Struttura interna:</b></para>
        /// <list type="bullet">
        ///   <item><b>Gate cursore</b>: legge solo fase e azione gia' attive nel job.</item>
        ///   <item><b>Classificazione</b>: produce dati passivi dal risultato step.</item>
        ///   <item><b>Policy lookup</b>: consulta il registry configurabile da Resources.</item>
        ///   <item><b>Evaluator no-op</b>: conserva comportamento terminale esistente.</item>
        /// </list>
        /// </summary>
        private StepResult EvaluateLocalRecovery(
            World world,
            JobRuntimeState runtime,
            int npcId,
            Job job,
            ref NpcJobState npcState,
            StepResult stepResult,
            int tick,
            Telemetry telemetry,
            MemoryBeliefDecisionExplainabilityParams explainabilityConfig,
            MemoryBeliefDecisionExplainabilityRegistry explainabilityRegistry,
            out bool replacedByRecoveryJob)
        {
            replacedByRecoveryJob = false;

            if (job == null)
                return stepResult;

            // Il ponte recovery deve osservare lo stesso step che verra' consegnato
            // alla state machine. Se il piano non espone piu' fase o azione, non
            // inventiamo coordinate di recupero: il fallimento resta terminale come
            // prima della patch.
            if (!job.Plan.TryGetPhase(npcState.ActivePhaseIndex, out var phase))
                return stepResult;

            if (!phase.TryGetAction(npcState.ActiveActionIndex, out var action))
                return stepResult;

            var classification = StepFailureClassifier.Classify(
                stepResult,
                action,
                npcState.ActivePhaseIndex,
                npcState.ActiveActionIndex);

            if (!classification.HasClassification)
                return stepResult;

            if (!_recoveryPolicyRegistry.TryGetPolicy(classification.FailureKind, out var policy))
                return stepResult;

            int retryCount = npcState.GetRecoveryRetryCount(
                classification.FailureKind,
                classification.PhaseIndex,
                classification.ActionIndex);
            int elapsedTicks = npcState.GetRecoveryElapsedTicks(
                classification.FailureKind,
                classification.PhaseIndex,
                classification.ActionIndex,
                tick);
            var recovery = EvaluateConfiguredRecoveryStrategy(
                world,
                runtime,
                npcId,
                job,
                ref npcState,
                classification,
                policy,
                retryCount,
                elapsedTicks,
                tick,
                stepResult,
                telemetry,
                explainabilityConfig,
                explainabilityRegistry,
                phase,
                action,
                out replacedByRecoveryJob);

            if (recovery.Kind != JobRecoveryResultKind.RetryScheduled)
                return stepResult;

            npcState.RegisterRecoveryRetry(
                classification.FailureKind,
                classification.PhaseIndex,
                classification.ActionIndex,
                tick);
            return StepResult.Blocked(
                recovery.SuggestedWaitTicks,
                "RecoveryRetryScheduled:" + classification.FailureKind + ":" + recovery.AppliedStrategy);
        }

        private JobRecoveryResult EvaluateConfiguredRecoveryStrategy(
            World world,
            JobRuntimeState runtime,
            int npcId,
            Job job,
            ref NpcJobState npcState,
            StepFailureClassification classification,
            StepRecoveryPolicy policy,
            int retryCount,
            int elapsedTicks,
            int tick,
            StepResult stepResult,
            Telemetry telemetry,
            MemoryBeliefDecisionExplainabilityParams explainabilityConfig,
            MemoryBeliefDecisionExplainabilityRegistry explainabilityRegistry,
            JobPhase phase,
            JobAction action,
            out bool replacedByRecoveryJob)
        {
            replacedByRecoveryJob = false;

            if (policy.Strategies == null)
                return JobRecoveryResult.None();

            for (int i = 0; i < policy.Strategies.Length; i++)
            {
                var strategy = policy.Strategies[i];

                if (strategy == StepRecoveryStrategy.FindEquivalentTarget)
                {
                    var targetRecovery = TryReplaceWithEquivalentFoodTarget(
                        world,
                        runtime,
                        npcId,
                        job,
                        ref npcState,
                        classification,
                        policy,
                        elapsedTicks,
                        tick,
                        stepResult,
                        telemetry,
                        explainabilityConfig,
                        explainabilityRegistry,
                        phase,
                        action,
                        out replacedByRecoveryJob);

                    if (targetRecovery.HasDeclaredResult)
                        return targetRecovery;

                    continue;
                }

                if (strategy == StepRecoveryStrategy.RetrySameAction
                    || strategy == StepRecoveryStrategy.WaitAndRetry)
                {
                    var retryRecovery = _recoveryEvaluator.EvaluateLocalRetry(
                        classification,
                        policy,
                        retryCount,
                        elapsedTicks,
                        strategy);

                    if (retryRecovery.HasDeclaredResult)
                        return retryRecovery;
                }
            }

            return JobRecoveryResult.None();
        }

        private JobRecoveryResult TryReplaceWithEquivalentFoodTarget(
            World world,
            JobRuntimeState runtime,
            int npcId,
            Job job,
            ref NpcJobState npcState,
            StepFailureClassification classification,
            StepRecoveryPolicy policy,
            int elapsedTicks,
            int tick,
            StepResult stepResult,
            Telemetry telemetry,
            MemoryBeliefDecisionExplainabilityParams explainabilityConfig,
            MemoryBeliefDecisionExplainabilityRegistry explainabilityRegistry,
            JobPhase phase,
            JobAction action,
            out bool replacedByRecoveryJob)
        {
            replacedByRecoveryJob = false;

            int alternativeCount = npcState.GetRecoveryAlternativeTargetCount(
                classification.FailureKind,
                classification.PhaseIndex,
                classification.ActionIndex);

            bool hasCandidate = TryResolveEquivalentCommunityFoodTarget(
                world,
                npcId,
                job,
                policy.MaxSearchRadius,
                out int replacementFoodObjectId,
                out var replacementCell);
            var recovery = _recoveryEvaluator.EvaluateEquivalentTarget(
                classification,
                policy,
                alternativeCount,
                elapsedTicks,
                hasCandidate);

            if (recovery.Kind != JobRecoveryResultKind.TargetReplaced)
                return recovery;

            if (!TryCreateReplacementFoodJob(job, npcId, replacementFoodObjectId, replacementCell, tick, out var replacementJob))
                return JobRecoveryResult.None();

            if (!runtime.ReplaceCurrentJobForRecovery(npcId, replacementJob, tick, out _))
                return JobRecoveryResult.None();

            runtime.RecordRecoveredStepFailureLearning(
                npcId,
                job,
                stepResult.FailureReason,
                tick,
                "EquivalentTarget:" + stepResult.DiagnosticMessage);
            ApplyFoodExecutionFailureCognitiveFeedback(world, npcId, job, stepResult, tick, telemetry);
            EmitEquivalentTargetRecoveryExplainability(
                explainabilityConfig,
                explainabilityRegistry,
                npcId,
                tick,
                job,
                replacementJob,
                phase,
                action,
                classification,
                stepResult,
                recovery);

            if (runtime.TryGetNpcState(npcId, out var replacementState))
            {
                replacementState.RegisterRecoveryAlternativeTarget(
                    classification.FailureKind,
                    classification.PhaseIndex,
                    classification.ActionIndex,
                    tick);
                runtime.SetNpcState(npcId, in replacementState);
            }

            replacedByRecoveryJob = true;
            return recovery;
        }

        private bool TryCreateReplacementFoodJob(
            Job currentJob,
            int npcId,
            int replacementFoodObjectId,
            Vector2Int replacementCell,
            int tick,
            out Job replacementJob)
        {
            replacementJob = null;

            if (currentJob == null || currentJob.Request.IntentKind != DecisionIntentKind.EatKnownFood)
                return false;

            var request = new JobRequest(
                "jobreq_food_recovery_" + npcId + "_" + replacementFoodObjectId + "_" + tick,
                npcId,
                DecisionIntentKind.EatKnownFood,
                currentJob.Request.PriorityClass,
                currentJob.Request.Urgency01,
                tick,
                true,
                replacementCell,
                replacementFoodObjectId,
                "RecoveryEquivalentFood:" + replacementFoodObjectId,
                "JobRecoveryEquivalentFood");

            return FoodJobFactory.TryCreateKnownCommunityFoodJob(
                _jobTemplateRegistry,
                request,
                out replacementJob,
                out _);
        }

        private static void EmitEquivalentTargetRecoveryExplainability(
            MemoryBeliefDecisionExplainabilityParams explainabilityConfig,
            MemoryBeliefDecisionExplainabilityRegistry explainabilityRegistry,
            int npcId,
            int tick,
            Job originalJob,
            Job replacementJob,
            JobPhase phase,
            JobAction action,
            StepFailureClassification classification,
            StepResult stepResult,
            JobRecoveryResult recovery)
        {
            if (originalJob == null || replacementJob == null)
                return;

            string recoveryReason = "RecoveryTargetReplaced:"
                + classification.FailureKind
                + ":"
                + recovery.AppliedStrategy
                + ":"
                + stepResult.DiagnosticMessage;

            MemoryBeliefDecisionExplainabilityEmitter.TryWriteStepTrace(
                explainabilityConfig,
                explainabilityRegistry,
                npcId,
                tick,
                originalJob,
                phase,
                classification.PhaseIndex,
                action,
                classification.ActionIndex,
                stepResult,
                recoveryReason);

            MemoryBeliefDecisionExplainabilityEmitter.TryWriteJobRequestTrace(
                explainabilityConfig,
                explainabilityRegistry,
                npcId,
                tick,
                replacementJob.Request,
                replacementJob.JobId,
                "RecoveryJobRequest:EquivalentTarget:" + recovery.Diagnostic,
                legacyBridgeStillUsed: false);

            MemoryBeliefDecisionExplainabilityEmitter.TryWriteJobLifecycleTrace(
                explainabilityConfig,
                explainabilityRegistry,
                npcId,
                tick,
                MemoryBeliefDecisionJobLifecycleOperation.Activated,
                replacementJob,
                "RecoveryJobActivated:EquivalentTarget:" + recovery.Diagnostic);
        }

        private static bool TryResolveEquivalentCommunityFoodTarget(
            World world,
            int npcId,
            Job job,
            int maxSearchRadius,
            out int replacementFoodObjectId,
            out Vector2Int replacementCell)
        {
            replacementFoodObjectId = 0;
            replacementCell = default;

            if (world == null || job == null || job.Request.IntentKind != DecisionIntentKind.EatKnownFood)
                return false;

            if (maxSearchRadius <= 0 || !world.GridPos.TryGetValue(npcId, out var npcCell))
                return false;

            int bestDistance = int.MaxValue;
            foreach (var pair in world.FoodStocks)
            {
                int objectId = pair.Key;
                if (objectId == job.Request.TargetObjectId)
                    continue;

                var stock = pair.Value;
                if (stock.Units <= 0 || stock.OwnerKind != OwnerKind.Community || stock.OwnerId != 0)
                    continue;

                if (!world.Objects.TryGetValue(objectId, out var obj) || obj == null)
                    continue;

                int distance = Mathf.Abs(obj.CellX - npcCell.X) + Mathf.Abs(obj.CellY - npcCell.Y);
                if (distance > maxSearchRadius || distance >= bestDistance)
                    continue;

                if (!world.HasLineOfSight(npcCell.X, npcCell.Y, obj.CellX, obj.CellY))
                    continue;

                bestDistance = distance;
                replacementFoodObjectId = objectId;
                replacementCell = new Vector2Int(obj.CellX, obj.CellY);
            }

            return replacementFoodObjectId > 0;
        }

        private static StepResult ExecuteCurrentAction(
            World world,
            JobRuntimeState runtime,
            RunningActionExecutor runningActionExecutor,
            int npcId,
            in NpcJobState npcState,
            Job job,
            MessageBus bus,
            int tick,
            MemoryBeliefDecisionExplainabilityParams explainabilityConfig,
            MemoryBeliefDecisionExplainabilityRegistry explainabilityRegistry)
        {
            if (!job.Plan.TryGetPhase(npcState.ActivePhaseIndex, out var phase))
                return StepResult.Failed(JobFailureReason.MissingPlan, "MissingJobPhase");

            if (!phase.TryGetAction(npcState.ActiveActionIndex, out var action))
                return StepResult.Failed(JobFailureReason.StepFailed, "MissingJobAction");

            if (!world.GridPos.TryGetValue(npcId, out var npcCell))
                return StepResult.Failed(JobFailureReason.MissingTarget, "NpcPositionMissing");

            if (action.Kind == JobActionKind.WaitTicks)
                return ExecuteRunningWaitAction(
                    runtime,
                    runningActionExecutor,
                    npcId,
                    in npcState,
                    job,
                    phase,
                    action,
                    tick,
                    explainabilityConfig,
                    explainabilityRegistry);

            if (action.Kind == JobActionKind.MoveToCell)
                return ExecuteMoveTo(world, runtime, runningActionExecutor, npcId, job, action, npcCell, bus, tick, explainabilityConfig, explainabilityRegistry);

            if (action.Kind == JobActionKind.Consume)
                return ExecuteConsumeKnownFood(world, runtime, npcId, job, action, npcCell, tick, explainabilityConfig, explainabilityRegistry);

            if (action.Kind == JobActionKind.PickUp)
                return ExecutePickUpObject(world, runtime, npcId, job, action, npcCell, tick, explainabilityConfig, explainabilityRegistry);

            if (action.Kind == JobActionKind.Drop)
                return ExecuteDropObject(world, runtime, npcId, job, action, npcCell, tick, explainabilityConfig, explainabilityRegistry);

            return StepResult.Failed(JobFailureReason.StepFailed, "UnsupportedJobActionInRuntimeSlice");
        }

        private static StepResult ExecuteRunningWaitAction(
            JobRuntimeState runtime,
            RunningActionExecutor runningActionExecutor,
            int npcId,
            in NpcJobState npcState,
            Job job,
            JobPhase phase,
            JobAction action,
            int tick,
            MemoryBeliefDecisionExplainabilityParams explainabilityConfig,
            MemoryBeliefDecisionExplainabilityRegistry explainabilityRegistry)
        {
            if (runtime == null || runningActionExecutor == null || job == null)
                return StepResult.Failed(JobFailureReason.StepFailed, "RunningActionRuntimeMissing");

            // v0.11c.02e collega produttivamente store + executor solo su WaitTicks:
            // e' una running action controllata, priva di target, priva di command
            // finale e non legata al MovementSystem. Il cursore NPC/job resta la
            // sorgente di identita' per evitare un secondo stato operativo: se
            // phase/action cambiano, cambia anche la chiave dello stato volatile.
            var key = new RunningActionKey(npcId, job.JobId, npcState.ActivePhaseIndex, npcState.ActiveActionIndex);
            if (!runtime.RunningActions.TryGet(key, out var runningAction) || runningAction == null)
            {
                int requiredTicks = Mathf.Max(1, action.DurationTicks);
                var policy = new RunningActionCompletionPolicy(
                    requiredTicks,
                    timeoutTicks: 0,
                    failureReason: JobFailureReason.StepFailed,
                    interruptionReason: JobFailureReason.Cancelled);

                runningAction = RunningActionRuntimeState.Start(
                    $"run_{job.JobId}_{npcState.ActivePhaseIndex}_{npcState.ActiveActionIndex}",
                    RunningActionKind.Wait,
                    npcId,
                    job.JobId,
                    phase.PhaseId,
                    action.ActionId,
                    tick,
                    policy);

                if (!runtime.RunningActions.Register(key, runningAction, out var registerReason))
                    return StepResult.Failed(JobFailureReason.StepFailed, registerReason);

                // La trace "Started" nasce nel punto in cui lo stato volatile entra
                // nello store Job. Non e' una transizione simulativa: rende solo
                // leggibile l'inizio del progress interno richiesto da ARC-DEC-020.
                MemoryBeliefDecisionExplainabilityEmitter.TryWriteRunningActionTrace(
                    explainabilityConfig,
                    explainabilityRegistry,
                    npcId,
                    tick,
                    MemoryBeliefDecisionRunningActionOperation.Started,
                    in key,
                    runningAction.ToSnapshot(),
                    "WaitTicksRunningActionStarted");
            }

            var executorResult = runningActionExecutor.Tick(
                runningAction,
                RunningActionExecutorTickRequest.Advance(1, tick, "WaitTicksRunningActionTick"));

            EmitRunningActionExecutionTrace(
                explainabilityConfig,
                explainabilityRegistry,
                npcId,
                tick,
                in key,
                executorResult);

            if (executorResult.Kind == RunningActionExecutorResultKind.Completed)
            {
                // La completion interna autorizza solo la state machine ad avanzare
                // lo step. Non emette ICommand: WaitTicks non ha una mutazione finale
                // del World in questa patch. Lo stato volatile viene rimosso prima di
                // restituire Succeeded per evitare progress appeso se il job continua
                // con altre action nella stessa fase.
                runtime.RunningActions.Clear(key);
                return StepResult.Succeeded("WaitTicksRunningActionCompleted");
            }

            if (executorResult.Kind == RunningActionExecutorResultKind.Advanced)
                return StepResult.Running("WaitTicksRunningActionRunning");

            if (executorResult.Kind == RunningActionExecutorResultKind.NoProgress)
                return StepResult.Running("WaitTicksRunningActionNoProgress");

            if (executorResult.Kind == RunningActionExecutorResultKind.TimedOut
                || executorResult.Kind == RunningActionExecutorResultKind.Failed
                || executorResult.Kind == RunningActionExecutorResultKind.Interrupted
                || executorResult.Kind == RunningActionExecutorResultKind.AlreadyTerminal
                || executorResult.Kind == RunningActionExecutorResultKind.InvalidState)
            {
                return StepResult.Failed(JobFailureReason.StepFailed, executorResult.Reason);
            }

            return StepResult.Running("WaitTicksRunningActionPending");
        }

        private static void EmitRunningActionExecutionTrace(
            MemoryBeliefDecisionExplainabilityParams explainabilityConfig,
            MemoryBeliefDecisionExplainabilityRegistry explainabilityRegistry,
            int npcId,
            int tick,
            in RunningActionKey key,
            RunningActionExecutorResult executorResult)
        {
            // Questo bridge diagnostico mappa il risultato dell'executor su un
            // vocabolario EL stabile. La mappatura non decide cosa fare del job:
            // la semantica operativa resta nel blocco chiamante e nella state
            // machine. Qui non vengono emessi command e non viene toccato il World.
            var operation = ResolveRunningActionOperation(executorResult.Kind);
            if (operation == MemoryBeliefDecisionRunningActionOperation.Unknown)
                return;

            MemoryBeliefDecisionExplainabilityEmitter.TryWriteRunningActionTrace(
                explainabilityConfig,
                explainabilityRegistry,
                npcId,
                tick,
                operation,
                in key,
                executorResult.After,
                executorResult.Reason);
        }

        private static MemoryBeliefDecisionRunningActionOperation ResolveRunningActionOperation(
            RunningActionExecutorResultKind kind)
        {
            return kind switch
            {
                RunningActionExecutorResultKind.Advanced => MemoryBeliefDecisionRunningActionOperation.Progress,
                RunningActionExecutorResultKind.NoProgress => MemoryBeliefDecisionRunningActionOperation.Progress,
                RunningActionExecutorResultKind.Completed => MemoryBeliefDecisionRunningActionOperation.Completed,
                RunningActionExecutorResultKind.TimedOut => MemoryBeliefDecisionRunningActionOperation.TimedOut,
                RunningActionExecutorResultKind.Failed => MemoryBeliefDecisionRunningActionOperation.Failed,
                RunningActionExecutorResultKind.Interrupted => MemoryBeliefDecisionRunningActionOperation.Interrupted,
                RunningActionExecutorResultKind.AlreadyTerminal => MemoryBeliefDecisionRunningActionOperation.Failed,
                RunningActionExecutorResultKind.InvalidState => MemoryBeliefDecisionRunningActionOperation.Failed,
                _ => MemoryBeliefDecisionRunningActionOperation.Unknown
            };
        }

        private static StepResult ExecuteMoveTo(
            World world,
            JobRuntimeState runtime,
            RunningActionExecutor runningActionExecutor,
            int npcId,
            Job job,
            JobAction action,
            GridPosition npcCell,
            MessageBus bus,
            int tick,
            MemoryBeliefDecisionExplainabilityParams explainabilityConfig,
            MemoryBeliefDecisionExplainabilityRegistry explainabilityRegistry)
        {
            if (!action.HasTargetCell)
                return StepResult.Failed(JobFailureReason.MissingTarget, "MoveMissingTargetCell");

            if (!ValidateMoveTargetObject(world, job, action, out var targetFailure))
                return targetFailure;

            if (npcCell.X == action.TargetCell.x && npcCell.Y == action.TargetCell.y)
                return StepResult.Succeeded("MoveTargetReached");

            if (CanUseRunningActionCellTraversal(world, npcCell, action.TargetCell))
            {
                return ExecuteRunningCellTraversalAction(
                    world,
                    runtime,
                    runningActionExecutor,
                    npcId,
                    job,
                    action,
                    npcCell,
                    bus,
                    tick,
                    explainabilityConfig,
                    explainabilityRegistry);
            }

            if (TryResolveKnownMoveToRouteStep(world, npcId, npcCell, action.TargetCell, out var nextKnownRouteCell, out var knownRouteFailure))
            {
                return ExecuteKnownRouteMoveToStep(
                    world,
                    runtime,
                    runningActionExecutor,
                    npcId,
                    job,
                    action,
                    npcCell,
                    nextKnownRouteCell,
                    bus,
                    tick,
                    explainabilityConfig,
                    explainabilityRegistry);
            }

            if (!string.IsNullOrEmpty(knownRouteFailure))
                return StepResult.Failed(JobFailureReason.MovementFailed, knownRouteFailure);

            if (CanUseJobMovementRuntime(world))
                return StepResult.Failed(JobFailureReason.MovementFailed, "MoveToKnownRouteMissing");

            // Ponte compatibile v0.15.9: questo ramo resta disponibile solo quando
            // il runtime movimento Job e' spento. In configurazione produttiva futura
            // il target distante senza route nota deve fallire e passare dalla matrice
            // recovery, non creare un MoveIntent nascosto.
            bool alreadyMovingToTarget =
                world.NpcMoveIntents.TryGetValue(npcId, out var currentIntent)
                && currentIntent.Active
                && currentIntent.TargetX == action.TargetCell.x
                && currentIntent.TargetY == action.TargetCell.y
                && currentIntent.TargetObjectId == action.TargetObjectId;

            if (!alreadyMovingToTarget)
            {
                // L'overload EL-aware del command buffer conserva la stessa command
                // authority: lo step continua solo ad accodare un ICommand e non
                // muta il World. La trace e' osservativa, utile per collegare
                // StepResult -> CommandBuffer -> command pump nel pannello Job.
                runtime.CommandBuffer.Enqueue(new SetMoveIntentCommand(npcId, new MoveIntent
                {
                    Active = true,
                    TargetX = action.TargetCell.x,
                    TargetY = action.TargetCell.y,
                    Reason = ResolveMoveIntentReason(job.Request.IntentKind),
                    TargetObjectId = action.TargetObjectId,
                    Urgency01 = 1f
                }),
                explainabilityConfig,
                explainabilityRegistry,
                npcId,
                tick,
                job.JobId,
                "MoveToCellCommandEnqueued");
            }

            return StepResult.Running(alreadyMovingToTarget ? "MoveAlreadyRequested" : "MoveCommandEnqueued");
        }

        // =============================================================================
        // TryResolveKnownMoveToRouteStep
        // =============================================================================
        /// <summary>
        /// <para>
        /// Tenta di leggere il prossimo passo di una route gia' presente nello stato
        /// pathfinding, senza costruire nuovi percorsi e senza interrogare il
        /// movimento legacy.
        /// </para>
        ///
        /// <para><b>Principio architetturale: route conosciuta, non pianificatore nascosto</b></para>
        /// <para>
        /// Lo step v0.15.6 deve permettere al Job di consumare una route cella-per-cella
        /// tramite running action, ma non deve ancora trasformare il Job in un sistema
        /// di ricerca globale. Per questo motivo il metodo accetta solo uno stato
        /// <c>DirectCommitExecution</c> gia' esistente, con target finale coerente con
        /// il target del job. Se la route non esiste, il chiamante puo' ancora usare il
        /// ponte legacy fino agli step successivi. Se invece la route esiste ma risulta
        /// incoerente, lo step fallisce in modo esplicito: una route nota corrotta non
        /// deve essere mascherata da un nuovo fallback automatico.
        /// </para>
        ///
        /// <para><b>Struttura interna:</b></para>
        /// <list type="bullet">
        ///   <item><b>Assenza route</b>: nessun effetto, lascia decidere il chiamante.</item>
        ///   <item><b>Target coerente</b>: la route deve finire nella cella richiesta dal job.</item>
        ///   <item><b>Riallineamento leggero</b>: se l'indice non punta piu' alla cella adiacente, cerca la posizione corrente dentro il path gia' noto.</item>
        ///   <item><b>Fallimento esplicito</b>: route attiva ma desincronizzata produce errore locale.</item>
        /// </list>
        /// </summary>
        private static bool TryResolveKnownMoveToRouteStep(
            World world,
            int npcId,
            GridPosition npcCell,
            Vector2Int finalTargetCell,
            out Vector2Int nextRouteCell,
            out string failureReason)
        {
            nextRouteCell = default;
            failureReason = string.Empty;

            if (!CanUseJobMovementRuntime(world))
                return false;

            if (world.Pathfinding == null
                || !world.Pathfinding.DirectCommitExecution.TryGetValue(npcId, out var directState)
                || directState == null
                || !directState.Active)
            {
                return false;
            }

            if (directState.FinalTargetCellX != finalTargetCell.x || directState.FinalTargetCellY != finalTargetCell.y)
                return false;

            if (directState.CurrentPath == null || directState.CurrentPath.Count < 2)
            {
                failureReason = "MoveToKnownRouteEmpty";
                return false;
            }

            int nextIndex = directState.NextPathIndex < 1 ? 1 : directState.NextPathIndex;
            if (nextIndex >= directState.CurrentPath.Count
                || Mathf.Abs(directState.CurrentPath[nextIndex].X - npcCell.X) + Mathf.Abs(directState.CurrentPath[nextIndex].Y - npcCell.Y) != 1)
            {
                nextIndex = -1;
                for (int i = 0; i < directState.CurrentPath.Count - 1; i++)
                {
                    var cell = directState.CurrentPath[i];
                    if (cell.X == npcCell.X && cell.Y == npcCell.Y)
                    {
                        nextIndex = i + 1;
                        break;
                    }
                }
            }

            if (nextIndex < 1 || nextIndex >= directState.CurrentPath.Count)
            {
                failureReason = "MoveToKnownRouteDesynced";
                return false;
            }

            var next = directState.CurrentPath[nextIndex];
            if (Mathf.Abs(next.X - npcCell.X) + Mathf.Abs(next.Y - npcCell.Y) != 1)
            {
                failureReason = "MoveToKnownRouteNextStepInvalid";
                return false;
            }

            directState.NextPathIndex = nextIndex;
            directState.ImmediateTargetX = next.X;
            directState.ImmediateTargetY = next.Y;
            world.Pathfinding.DirectCommitExecution[npcId] = directState;

            nextRouteCell = new Vector2Int(next.X, next.Y);
            return true;
        }

        // =============================================================================
        // CanUseJobMovementRuntime
        // =============================================================================
        /// <summary>
        /// <para>
        /// Restituisce true quando il runtime movimento del Job e' abilitato dalla
        /// configurazione temporale.
        /// </para>
        ///
        /// <para><b>Principio architetturale: pensionamento controllato del ponte legacy</b></para>
        /// <para>
        /// Il gate consente di mantenere il comportamento storico quando il traversal
        /// Job e' spento, ma quando e' acceso impedisce al percorso ordinario di
        /// ricadere silenziosamente su <c>SetMoveIntentCommand</c>. In quel caso una
        /// route mancante diventa un fallimento esplicito e puo' essere gestita dalla
        /// matrice recovery del Job.
        /// </para>
        ///
        /// <para><b>Struttura interna:</b></para>
        /// <list type="bullet">
        ///   <item><b>Config assente</b>: false, preservando il ponte legacy.</item>
        ///   <item><b>Gate acceso</b>: il movimento ordinario prova il runtime Job.</item>
        /// </list>
        /// </summary>
        private static bool CanUseJobMovementRuntime(World world)
        {
            return world?.Config?.Sim != null && world.Config.Sim.ResolveEnableJobRunningActionTraversal();
        }

        // =============================================================================
        // ExecuteKnownRouteMoveToStep
        // =============================================================================
        /// <summary>
        /// <para>
        /// Esegue un singolo passo di una route multi-cella gia' nota usando la stessa
        /// running action produttiva del traversal adiacente.
        /// </para>
        ///
        /// <para><b>Principio architetturale: un solo passo fisico per volta</b></para>
        /// <para>
        /// La route multi-cella non sposta mai l'NPC direttamente al target finale.
        /// Ogni tick produttivo lavora su una sola cella adiacente, mantiene la
        /// reservation della destinazione immediata e aggiorna la posizione solo alla
        /// completion della running action. Se il passo intermedio completa ma il target
        /// finale non e' ancora raggiunto, lo step resta <c>Running</c> e il Job non
        /// avanza fase: al tick successivo verra' consumata la cella successiva della
        /// stessa route nota.
        /// </para>
        ///
        /// <para><b>Struttura interna:</b></para>
        /// <list type="bullet">
        ///   <item><b>Action effettiva</b>: clona l'azione originale cambiando solo la cella immediata.</item>
        ///   <item><b>Traversal</b>: delega al percorso multi-tick gia' validato per una cella.</item>
        ///   <item><b>Avanzamento route</b>: aggiorna il cursore DirectCommit solo dopo completion fisica.</item>
        ///   <item><b>Esito Job</b>: successo solo quando la cella finale e' davvero raggiunta.</item>
        /// </list>
        /// </summary>
        private static StepResult ExecuteKnownRouteMoveToStep(
            World world,
            JobRuntimeState runtime,
            RunningActionExecutor runningActionExecutor,
            int npcId,
            Job job,
            JobAction action,
            GridPosition npcCell,
            Vector2Int nextRouteCell,
            MessageBus bus,
            int tick,
            MemoryBeliefDecisionExplainabilityParams explainabilityConfig,
            MemoryBeliefDecisionExplainabilityRegistry explainabilityRegistry)
        {
            var immediateAction = new JobAction(
                action.ActionId,
                action.Kind,
                action.Label,
                true,
                nextRouteCell,
                action.TargetObjectId,
                action.DurationTicks,
                action.PayloadKey);

            var traversalResult = ExecuteRunningCellTraversalAction(
                world,
                runtime,
                runningActionExecutor,
                npcId,
                job,
                immediateAction,
                npcCell,
                bus,
                tick,
                explainabilityConfig,
                explainabilityRegistry);

            if (traversalResult.Status != StepResultStatus.Succeeded)
                return traversalResult;

            AdvanceKnownMoveToRouteAfterTraversal(world, npcId, npcCell, nextRouteCell);

            if (nextRouteCell.x == action.TargetCell.x && nextRouteCell.y == action.TargetCell.y)
                return StepResult.Succeeded("MoveToKnownRouteCompleted");

            return StepResult.Running("MoveToKnownRouteStepCompleted");
        }

        // =============================================================================
        // AdvanceKnownMoveToRouteAfterTraversal
        // =============================================================================
        /// <summary>
        /// <para>
        /// Avanza il cursore della route nota dopo un passo fisico completato dal Job.
        /// </para>
        ///
        /// <para><b>Principio architetturale: stato di route subordinato al movimento reale</b></para>
        /// <para>
        /// Il cursore non viene anticipato quando la running action parte o progredisce:
        /// viene aggiornato solo dopo lo spostamento effettivo della posizione World.
        /// Questo mantiene allineati diagnosi, reservation e posizione reale anche se
        /// un passo viene bloccato, interrotto o fallisce.
        /// </para>
        ///
        /// <para><b>Struttura interna:</b></para>
        /// <list type="bullet">
        ///   <item><b>Debug path</b>: registra il passo realmente completato.</item>
        ///   <item><b>Indice</b>: consuma la cella raggiunta se coincide col prossimo nodo atteso.</item>
        ///   <item><b>Terminale</b>: spegne la route quando il path e' esaurito.</item>
        /// </list>
        /// </summary>
        private static void AdvanceKnownMoveToRouteAfterTraversal(World world, int npcId, GridPosition fromCell, Vector2Int movedToCell)
        {
            if (world?.Pathfinding == null)
                return;

            world.AppendDebugDirectStepForNpc(npcId, fromCell.X, fromCell.Y, movedToCell.x, movedToCell.y);

            if (!world.Pathfinding.DirectCommitExecution.TryGetValue(npcId, out var directState)
                || directState == null
                || directState.CurrentPath == null
                || directState.CurrentPath.Count == 0)
            {
                return;
            }

            if (directState.NextPathIndex < directState.CurrentPath.Count)
            {
                var expected = directState.CurrentPath[directState.NextPathIndex];
                if (expected.X == movedToCell.x && expected.Y == movedToCell.y)
                    directState.NextPathIndex++;
            }

            if (directState.NextPathIndex >= directState.CurrentPath.Count)
            {
                directState.Active = false;
                directState.ImmediateTargetX = movedToCell.x;
                directState.ImmediateTargetY = movedToCell.y;
            }
            else
            {
                var next = directState.CurrentPath[directState.NextPathIndex];
                directState.Active = true;
                directState.ImmediateTargetX = next.X;
                directState.ImmediateTargetY = next.Y;
            }

            world.Pathfinding.DirectCommitExecution[npcId] = directState;
        }

        // =============================================================================
        // ValidateMoveTargetObject
        // =============================================================================
        /// <summary>
        /// <para>
        /// Valida il target oggetto di uno step <c>MoveToCell</c> prima di accodare
        /// o mantenere un <c>SetMoveIntentCommand</c>.
        /// </para>
        ///
        /// <para><b>Principio architetturale: il Job deve vedere il fallimento del target</b></para>
        /// <para>
        /// Nel percorso legacy il <c>MovementSystem</c> poteva cancellare un
        /// <c>MoveIntent</c> quando il cibo spariva, lasciando pero' il Job ancora in
        /// stato <c>Running</c>. Questa guardia sposta la validazione minima nel
        /// punto che possiede davvero lo step: se l'incarico sta raggiungendo un cibo
        /// noto e quel target non e' piu' valido, lo step fallisce e la state machine
        /// puo' chiudere o recuperare il job tramite la matrice esistente.
        /// </para>
        ///
        /// <para><b>Struttura interna:</b></para>
        /// <list type="bullet">
        ///   <item><b>Gate dominio</b>: agisce solo su <c>EatKnownFood</c> con target oggetto.</item>
        ///   <item><b>Oggetto</b>: fallisce se l'istanza world-level non esiste piu'.</item>
        ///   <item><b>Stock</b>: fallisce se lo stock e' assente, vuoto o non comunitario.</item>
        ///   <item><b>Cella</b>: fallisce se l'oggetto non si trova piu' nella cella promessa dal job.</item>
        /// </list>
        /// </summary>
        private static bool ValidateMoveTargetObject(World world, Job job, JobAction action, out StepResult failure)
        {
            failure = default;

            if (world == null || job == null || action.TargetObjectId <= 0)
                return true;

            if (job.Request.IntentKind != DecisionIntentKind.EatKnownFood)
                return true;

            if (!world.Objects.TryGetValue(action.TargetObjectId, out var targetObject) || targetObject == null)
            {
                failure = StepResult.Failed(JobFailureReason.MissingTarget, "MoveFoodObjectMissing");
                return false;
            }

            if (!world.FoodStocks.TryGetValue(action.TargetObjectId, out var stock))
            {
                failure = StepResult.Failed(JobFailureReason.MissingTarget, "MoveFoodStockMissing");
                return false;
            }

            if (stock.Units <= 0)
            {
                failure = StepResult.Failed(JobFailureReason.MissingTarget, "MoveFoodUnavailable");
                return false;
            }

            if (stock.OwnerKind != OwnerKind.Community || stock.OwnerId != 0)
            {
                failure = StepResult.Failed(JobFailureReason.InvalidRequest, "MoveFoodNotCommunityStock");
                return false;
            }

            if (targetObject.CellX != action.TargetCell.x || targetObject.CellY != action.TargetCell.y)
            {
                failure = StepResult.Failed(JobFailureReason.MissingTarget, "MoveTargetNoLongerAtCell");
                return false;
            }

            return true;
        }

        private static StepResult ExecuteRunningCellTraversalAction(
            World world,
            JobRuntimeState runtime,
            RunningActionExecutor runningActionExecutor,
            int npcId,
            Job job,
            JobAction action,
            GridPosition npcCell,
            MessageBus bus,
            int tick,
            MemoryBeliefDecisionExplainabilityParams explainabilityConfig,
            MemoryBeliefDecisionExplainabilityRegistry explainabilityRegistry)
        {
            if (world == null || runtime == null || job == null)
                return StepResult.Failed(JobFailureReason.MovementFailed, "TraversalRuntimeMissing");

            // La chiave resta agganciata al cursore job corrente: il progress
            // volatile non diventa una seconda authority su destinazione o path.
            var key = ResolveRunningActionKey(runtime, npcId, job.JobId);

            if (!TryOpenTraversalDoorIfNeeded(world, bus, npcId, action.TargetCell, out var doorFailure))
                return StepResult.Failed(JobFailureReason.MovementFailed, doorFailure);

            if (!CanEnterTraversalTarget(world, npcId, action.TargetCell, out var blockedReason))
            {
                ReleaseTraversalDestinationReservation(
                    runtime,
                    in key,
                    action.TargetCell,
                    explainabilityConfig,
                    explainabilityRegistry,
                    tick,
                    npcId);
                return StepResult.Failed(JobFailureReason.MovementFailed, blockedReason);
            }

            if (!TryEnsureTraversalDestinationReservation(
                    world,
                    runtime,
                    npcId,
                    job,
                    action,
                    in key,
                    tick,
                    explainabilityConfig,
                    explainabilityRegistry,
                    out var reservationBlockedResult))
            {
                return reservationBlockedResult;
            }

            if (!runtime.RunningActions.TryGet(key, out var runningAction) || runningAction == null)
            {
                int requiredTicks = ResolveBaseWalkCellDurationTicks(world);
                var policy = new RunningActionCompletionPolicy(
                    requiredTicks,
                    timeoutTicks: 0,
                    failureReason: JobFailureReason.MovementFailed,
                    interruptionReason: JobFailureReason.Cancelled);

                runningAction = RunningActionRuntimeState.Start(
                    $"move_{job.JobId}_{key.PhaseIndex}_{key.ActionIndex}_{npcCell.X}_{npcCell.Y}_{action.TargetCell.x}_{action.TargetCell.y}",
                    RunningActionKind.Movement,
                    npcId,
                    job.JobId,
                    ResolveActivePhaseId(job, key.PhaseIndex),
                    action.ActionId,
                    tick,
                    policy);

                if (!runtime.RunningActions.Register(key, runningAction, out var registerReason))
                    return StepResult.Failed(JobFailureReason.MovementFailed, registerReason);

                MemoryBeliefDecisionExplainabilityEmitter.TryWriteRunningActionTrace(
                    explainabilityConfig,
                    explainabilityRegistry,
                    npcId,
                    tick,
                    MemoryBeliefDecisionRunningActionOperation.Started,
                    in key,
                    runningAction.ToSnapshot(),
                    "MoveToCellTraversalStarted");
            }

            var executorResult = runningActionExecutor != null
                ? runningActionExecutor.Tick(runningAction, RunningActionExecutorTickRequest.Advance(1, tick, "MoveToCellTraversalTick"))
                : new RunningActionExecutor().Tick(runningAction, RunningActionExecutorTickRequest.Advance(1, tick, "MoveToCellTraversalTick"));

            EmitRunningActionExecutionTrace(
                explainabilityConfig,
                explainabilityRegistry,
                npcId,
                tick,
                in key,
                executorResult);

            if (executorResult.Kind == RunningActionExecutorResultKind.Completed)
            {
                if (!CanEnterTraversalTarget(world, npcId, action.TargetCell, out var completionBlockedReason))
                {
                    ReleaseTraversalDestinationReservation(
                        runtime,
                        in key,
                        action.TargetCell,
                        explainabilityConfig,
                        explainabilityRegistry,
                        tick,
                        npcId);
                    runtime.RunningActions.Clear(key);
                    return StepResult.Failed(JobFailureReason.MovementFailed, completionBlockedReason);
                }

                // Questa e' la sola mutazione World introdotta dal path 02g: avviene
                // dopo completion del progress interno e sposta l'NPC direttamente
                // dalla cella sorgente alla cella destinazione. Non esistono celle
                // intermedie, non viene toccato MovementSystem e non viene emesso
                // alcun command durante i tick di progress.
                world.SetNpcPos(npcId, action.TargetCell.x, action.TargetCell.y);
                world.NotifyNpcMovedForLandmarkLearning(
                    npcId,
                    npcCell.X,
                    npcCell.Y,
                    action.TargetCell.x,
                    action.TargetCell.y);
                ReleaseTraversalDestinationReservation(
                    runtime,
                    in key,
                    action.TargetCell,
                    explainabilityConfig,
                    explainabilityRegistry,
                    tick,
                    npcId);
                runtime.RunningActions.Clear(key);
                return StepResult.Succeeded("MoveToCellTraversalCompleted");
            }

            if (executorResult.Kind == RunningActionExecutorResultKind.Advanced)
                return StepResult.Running("MoveToCellTraversalRunning");

            if (executorResult.Kind == RunningActionExecutorResultKind.NoProgress)
                return StepResult.Running("MoveToCellTraversalNoProgress");

            if (executorResult.Kind == RunningActionExecutorResultKind.TimedOut
                || executorResult.Kind == RunningActionExecutorResultKind.Failed
                || executorResult.Kind == RunningActionExecutorResultKind.Interrupted
                || executorResult.Kind == RunningActionExecutorResultKind.AlreadyTerminal
                || executorResult.Kind == RunningActionExecutorResultKind.InvalidState)
            {
                ReleaseTraversalDestinationReservation(
                    runtime,
                    in key,
                    action.TargetCell,
                    explainabilityConfig,
                    explainabilityRegistry,
                    tick,
                    npcId);
                runtime.RunningActions.Clear(key);
                return StepResult.Failed(JobFailureReason.MovementFailed, executorResult.Reason);
            }

            return StepResult.Running("MoveToCellTraversalPending");
        }

        // =============================================================================
        // TryOpenTraversalDoorIfNeeded
        // =============================================================================
        /// <summary>
        /// <para>
        /// Gestisce la micro-interazione locale "porta chiusa apribile" prima di
        /// avviare il traversal multi-tick verso la cella destinazione.
        /// </para>
        ///
        /// <para><b>Principio architetturale: micro-operazioni fisiche dentro MoveTo</b></para>
        /// <para>
        /// v0.15 sposta il movimento ordinario dentro la running action <c>MoveTo</c>.
        /// Una porta chiusa e non bloccata sulla cella immediata non deve quindi
        /// richiedere il vecchio <c>MovementSystem</c> per aprirsi. La patch resta
        /// locale: usa <c>OpenDoorCommand</c>, pubblica lo stesso evento mondo e non
        /// introduce chiavi, scelta sociale, pathfinding o recovery intelligente.
        /// </para>
        ///
        /// <para><b>Struttura interna:</b></para>
        /// <list type="bullet">
        ///   <item><b>Nessuna porta</b>: il traversal prosegue invariato.</item>
        ///   <item><b>Porta aperta</b>: il traversal prosegue invariato.</item>
        ///   <item><b>Porta bloccata</b>: fallimento locale esplicito.</item>
        ///   <item><b>Porta chiusa apribile</b>: apertura tramite command esistente e trace movimento opzionale.</item>
        /// </list>
        /// </summary>
        private static bool TryOpenTraversalDoorIfNeeded(
            World world,
            MessageBus bus,
            int npcId,
            Vector2Int targetCell,
            out string failureReason)
        {
            failureReason = string.Empty;

            if (world == null)
                return true;

            if (!TryGetTraversalDoorAtCell(world, targetCell, out int doorObjectId, out var doorInstance, out var doorDef))
                return true;

            if (doorInstance.IsOpen)
                return true;

            if (doorInstance.IsLocked)
            {
                MovementExplainabilityEmitter.TryEmitDoorInteraction(
                    world,
                    npcId,
                    doorObjectId,
                    targetCell,
                    DoorState.Locked,
                    DoorState.Locked,
                    commandEmitted: false,
                    accessGranted: false,
                    summary: "job_traversal_door_locked");
                failureReason = "TraversalDoorLocked";
                return false;
            }

            new OpenDoorCommand(npcId, doorObjectId).Execute(world, bus ?? new MessageBus());

            MovementExplainabilityEmitter.TryEmitDoorInteraction(
                world,
                npcId,
                doorObjectId,
                targetCell,
                DoorState.Closed,
                DoorState.Open,
                commandEmitted: true,
                accessGranted: true,
                summary: "job_traversal_door_opened");

            return true;
        }

        // =============================================================================
        // TryGetTraversalDoorAtCell
        // =============================================================================
        /// <summary>
        /// <para>
        /// Cerca una porta nella cella di traversal usando prima la cache rapida del
        /// World e poi, se necessario, una scansione difensiva degli oggetti.
        /// </para>
        ///
        /// <para><b>Principio architetturale: robustezza locale senza nuova authority</b></para>
        /// <para>
        /// La cache <c>GetObjectAt</c> e' il percorso normale, ma alcuni test e alcune
        /// operazioni dev possono avere oggetti appena creati prima di un rebuild
        /// globale delle cache derivate. La scansione di ripiego non decide movimento,
        /// non pianifica e non muta il mondo: serve solo a riconoscere correttamente
        /// una porta gia' presente nella cella immediata.
        /// </para>
        ///
        /// <para><b>Struttura interna:</b></para>
        /// <list type="bullet">
        ///   <item><b>Cache</b>: usa <c>GetObjectAt</c> quando disponibile e coerente.</item>
        ///   <item><b>Fallback locale</b>: scansiona <c>world.Objects</c> solo se la cache non produce una porta.</item>
        ///   <item><b>Filtro</b>: restituisce solo oggetti non held, nella cella target e con definizione porta.</item>
        /// </list>
        /// </summary>
        private static bool TryGetTraversalDoorAtCell(
            World world,
            Vector2Int targetCell,
            out int doorObjectId,
            out WorldObjectInstance doorInstance,
            out ObjectDef doorDef)
        {
            doorObjectId = 0;
            doorInstance = null;
            doorDef = null;

            if (world == null)
                return false;

            int objectIdFromCell = world.GetObjectAt(targetCell.x, targetCell.y);
            if (objectIdFromCell > 0
                && world.Objects.TryGetValue(objectIdFromCell, out var cachedInstance)
                && cachedInstance != null
                && world.TryGetObjectDef(cachedInstance.DefId, out var cachedDef)
                && cachedDef != null
                && cachedDef.IsDoor)
            {
                doorObjectId = objectIdFromCell;
                doorInstance = cachedInstance;
                doorDef = cachedDef;
                return true;
            }

            foreach (var kv in world.Objects)
            {
                var instance = kv.Value;
                if (instance == null
                    || instance.IsHeld
                    || instance.CellX != targetCell.x
                    || instance.CellY != targetCell.y
                    || !world.TryGetObjectDef(instance.DefId, out var def)
                    || def == null
                    || !def.IsDoor)
                {
                    continue;
                }

                doorObjectId = kv.Key;
                doorInstance = instance;
                doorDef = def;
                return true;
            }

            return false;
        }

        private static bool TryEnsureTraversalDestinationReservation(
            World world,
            JobRuntimeState runtime,
            int npcId,
            Job job,
            JobAction action,
            in RunningActionKey key,
            int tick,
            MemoryBeliefDecisionExplainabilityParams explainabilityConfig,
            MemoryBeliefDecisionExplainabilityRegistry explainabilityRegistry,
            out StepResult blockedResult)
        {
            blockedResult = default;

            if (runtime?.Reservations == null || job == null)
            {
                blockedResult = StepResult.Failed(JobFailureReason.ReservationDenied, "TraversalReservationStoreMissing");
                return false;
            }

            var targetCell = action.TargetCell;
            var reservationId = BuildTraversalDestinationReservationId(in key, targetCell);
            var record = new ReservationRecord(
                reservationId,
                job.JobId,
                npcId,
                ReservationTargetKind.Cell,
                targetCell,
                -1,
                tick,
                tick + ResolveTraversalReservationDurationTicks(world, action));

            // ARC-DEC-020 richiede che la cella destinazione sia riservata prima
            // dell'inizio del movimento multi-tick e resti protetta durante il
            // progress. La policy minima 02h non introduce attese complesse: se un
            // altro job possiede la cella, lo step torna Blocked(1) e la state
            // machine riprova in modo deterministico al tick successivo.
            if (runtime.Reservations.TryGetByTarget(ReservationTargetKind.Cell, targetCell, -1, out var existing))
            {
                if (existing.JobId == job.JobId)
                    return true;

                runtime.Reservations.TryReserve(
                    record,
                    out _,
                    explainabilityConfig,
                    explainabilityRegistry,
                    tick,
                    npcId);
                blockedResult = StepResult.Blocked(1, "MoveToCellTraversalDestinationReserved");
                return false;
            }

            if (runtime.Reservations.TryReserve(
                    record,
                    out _,
                    explainabilityConfig,
                    explainabilityRegistry,
                    tick,
                    npcId))
            {
                return true;
            }

            blockedResult = StepResult.Blocked(1, "MoveToCellTraversalDestinationReserved");
            return false;
        }

        private static void ReleaseTraversalDestinationReservation(
            JobRuntimeState runtime,
            in RunningActionKey key,
            Vector2Int targetCell,
            MemoryBeliefDecisionExplainabilityParams explainabilityConfig,
            MemoryBeliefDecisionExplainabilityRegistry explainabilityRegistry,
            int tick,
            int npcId)
        {
            if (runtime?.Reservations == null)
                return;

            // Rilasciamo solo la reservation action-scoped del traversal. Se la
            // stessa cella e' gia' coperta da una reservation job-level esistente,
            // quella resta responsabilita' del lifecycle del job e viene liberata
            // dai path Complete/Fail/Preempt/Clear gia' presenti in JobRuntimeState.
            runtime.Reservations.Release(
                BuildTraversalDestinationReservationId(in key, targetCell),
                out _,
                explainabilityConfig,
                explainabilityRegistry,
                tick,
                npcId);
        }

        private static string BuildTraversalDestinationReservationId(in RunningActionKey key, Vector2Int targetCell)
        {
            return "traversal:" + key.JobId + ":" + key.NpcId + ":" + key.PhaseIndex + ":" + key.ActionIndex + ":" + targetCell.x + ":" + targetCell.y;
        }

        private static int ResolveTraversalReservationDurationTicks(World world, JobAction action)
        {
            // La reservation temporale action-scoped deve coprire la stessa finestra
            // del traversal reale. Per MoveToCell la durata produttiva non vive in
            // JobAction.DurationTicks ma nella config tick.baseWalkCellDurationTicks;
            // usare quella durata evita una scadenza anticipata prima della completion.
            return Mathf.Max(1, Mathf.Max(ResolveBaseWalkCellDurationTicks(world), action.DurationTicks));
        }

        private static bool CanUseRunningActionCellTraversal(World world, GridPosition npcCell, Vector2Int targetCell)
        {
            if (world?.Config?.Sim == null || !world.Config.Sim.ResolveEnableJobRunningActionTraversal())
                return false;

            // Foundation volutamente stretta: solo una cella cardinale. Target
            // lontani, diagonali e pathfinding reale restano nel path legacy
            // SetMoveIntentCommand + MovementSystem.
            int dx = Mathf.Abs(targetCell.x - npcCell.X);
            int dy = Mathf.Abs(targetCell.y - npcCell.Y);
            return (dx + dy) == 1;
        }

        private static bool CanEnterTraversalTarget(World world, int npcId, Vector2Int targetCell, out string reason)
        {
            reason = string.Empty;

            if (!world.InBounds(targetCell.x, targetCell.y))
            {
                reason = "TraversalTargetOutOfBounds";
                return false;
            }

            if (world.IsMovementBlocked(targetCell.x, targetCell.y))
            {
                reason = "TraversalTargetBlocked";
                return false;
            }

            if (world.TryGetNpcAt(targetCell.x, targetCell.y, out var occupyingNpcId) && occupyingNpcId != npcId)
            {
                reason = "TraversalTargetOccupied";
                return false;
            }

            return true;
        }

        private static int ResolveBaseWalkCellDurationTicks(World world)
        {
            // Il default tipizzato vive nel gruppo Tick di SimulationParams. Non
            // leggiamo direttamente game_params.json qui: JsonUtility popola
            // world.Config e il resolver conserva fallback transitori dal vecchio
            // layout movement senza lasciare al MovementSystem authority sul tempo.
            return Mathf.Max(1, world?.Config?.Sim?.ResolveBaseWalkCellDurationTicks() ?? TickParams.DefaultBaseWalkCellDurationTicks);
        }

        private static RunningActionKey ResolveRunningActionKey(JobRuntimeState runtime, int npcId, string jobId)
        {
            if (runtime != null && runtime.TryGetActiveJob(npcId, out var npcState, out _))
                return new RunningActionKey(npcId, jobId, npcState.ActivePhaseIndex, npcState.ActiveActionIndex);

            return new RunningActionKey(npcId, jobId, 0, 0);
        }

        private static string ResolveActivePhaseId(Job job, int phaseIndex)
        {
            return job != null && job.Plan.TryGetPhase(phaseIndex, out var phase)
                ? phase.PhaseId
                : string.Empty;
        }

        private static MoveIntentReason ResolveMoveIntentReason(DecisionIntentKind intentKind)
        {
            if (intentKind == DecisionIntentKind.EatKnownFood || intentKind == DecisionIntentKind.SearchFood)
                return MoveIntentReason.SeekFood;

            if (intentKind == DecisionIntentKind.RestKnownPlace || intentKind == DecisionIntentKind.SearchRestPlace)
                return MoveIntentReason.SeekBed;

            if (intentKind == DecisionIntentKind.SeekSocialContact || intentKind == DecisionIntentKind.AskForHelp)
                return MoveIntentReason.SeekTalkTarget;

            return MoveIntentReason.None;
        }

        private static StepResult ExecuteConsumeKnownFood(
            World world,
            JobRuntimeState runtime,
            int npcId,
            Job job,
            JobAction action,
            GridPosition npcCell,
            int tick,
            MemoryBeliefDecisionExplainabilityParams explainabilityConfig,
            MemoryBeliefDecisionExplainabilityRegistry explainabilityRegistry)
        {
            if (action.TargetObjectId <= 0)
                return StepResult.Failed(JobFailureReason.MissingTarget, "ConsumeMissingFoodObject");

            if (!world.FoodStocks.TryGetValue(action.TargetObjectId, out var stock) || stock.Units <= 0)
                return StepResult.Failed(JobFailureReason.MissingTarget, "ConsumeFoodUnavailable");

            if (stock.OwnerKind != OwnerKind.Community || stock.OwnerId != 0)
                return StepResult.Failed(JobFailureReason.InvalidRequest, "ConsumeFoodNotCommunityStock");

            if (!world.Objects.TryGetValue(action.TargetObjectId, out var foodObject) || foodObject == null)
                return StepResult.Failed(JobFailureReason.MissingTarget, "ConsumeFoodObjectMissing");

            if (npcCell.X != foodObject.CellX || npcCell.Y != foodObject.CellY)
            {
                // Il consume step dovrebbe essere raggiunto solo dopo il MoveToCell.
                // Se il target non e' piu' sulla cella dell'NPC, il piano e' diventato
                // stale: falliamo il job invece di lasciare l'NPC in Running senza
                // command. Al tick decisionale successivo il ponte legacy/job potra'
                // ricostruire una nuova intenzione da needs e beliefs.
                return StepResult.Failed(JobFailureReason.MissingTarget, "ConsumeTargetNoLongerCoLocated");
            }

            runtime.CommandBuffer.Enqueue(
                new EatFromStockCommand(npcId, action.TargetObjectId),
                explainabilityConfig,
                explainabilityRegistry,
                npcId,
                tick,
                job.JobId,
                "ConsumeCommandEnqueued");
            return StepResult.Succeeded("ConsumeCommandEnqueued");
        }

        private static StepResult ExecutePickUpObject(
            World world,
            JobRuntimeState runtime,
            int npcId,
            Job job,
            JobAction action,
            GridPosition npcCell,
            int tick,
            MemoryBeliefDecisionExplainabilityParams explainabilityConfig,
            MemoryBeliefDecisionExplainabilityRegistry explainabilityRegistry)
        {
            if (action.TargetObjectId <= 0)
                return StepResult.Failed(JobFailureReason.MissingTarget, "PickUpMissingObject");

            if (!world.Objects.TryGetValue(action.TargetObjectId, out var obj) || obj == null)
                return StepResult.Failed(JobFailureReason.MissingTarget, "PickUpObjectMissing");

            if (obj.IsHeld)
                return StepResult.Failed(JobFailureReason.InvalidRequest, "PickUpObjectAlreadyHeld");

            if (npcCell.X != obj.CellX || npcCell.Y != obj.CellY)
                return StepResult.Failed(JobFailureReason.MissingTarget, "PickUpObjectNoLongerCoLocated");

            runtime.CommandBuffer.Enqueue(
                new PickUpObjectCommand(npcId, action.TargetObjectId),
                explainabilityConfig,
                explainabilityRegistry,
                npcId,
                tick,
                job.JobId,
                "PickUpCommandEnqueued");
            return StepResult.Succeeded("PickUpCommandEnqueued");
        }

        private static StepResult ExecuteDropObject(
            World world,
            JobRuntimeState runtime,
            int npcId,
            Job job,
            JobAction action,
            GridPosition npcCell,
            int tick,
            MemoryBeliefDecisionExplainabilityParams explainabilityConfig,
            MemoryBeliefDecisionExplainabilityRegistry explainabilityRegistry)
        {
            if (action.TargetObjectId <= 0)
                return StepResult.Failed(JobFailureReason.MissingTarget, "DropMissingObject");

            if (!action.HasTargetCell)
                return StepResult.Failed(JobFailureReason.MissingTarget, "DropMissingTargetCell");

            if (!world.Objects.TryGetValue(action.TargetObjectId, out var obj) || obj == null)
                return StepResult.Failed(JobFailureReason.MissingTarget, "DropObjectMissing");

            if (!obj.IsHeld || obj.HolderNpcId != npcId)
                return StepResult.Failed(JobFailureReason.InvalidRequest, "DropObjectNotHeldByNpc");

            if (npcCell.X != action.TargetCell.x || npcCell.Y != action.TargetCell.y)
                return StepResult.Failed(JobFailureReason.MissingTarget, "DropNpcNoLongerAtDestination");

            if (!world.InBounds(action.TargetCell.x, action.TargetCell.y))
                return StepResult.Failed(JobFailureReason.MissingTarget, "DropTargetOutOfBounds");

            int existing = world.GetObjectAt(action.TargetCell.x, action.TargetCell.y);
            if (existing >= 0 && existing != action.TargetObjectId)
                return StepResult.Failed(JobFailureReason.StepFailed, "DropTargetOccupied");

            runtime.CommandBuffer.Enqueue(
                new DropObjectCommand(npcId, action.TargetObjectId, action.TargetCell.x, action.TargetCell.y),
                explainabilityConfig,
                explainabilityRegistry,
                npcId,
                tick,
                job.JobId,
                "DropCommandEnqueued");
            return StepResult.Succeeded("DropCommandEnqueued");
        }

        // =============================================================================
        // ApplyFoodExecutionFailureCognitiveFeedback
        // =============================================================================
        /// <summary>
        /// <para>
        /// Collega il fallimento reale di execution del job food a un feedback
        /// cognitivo minimo sul BeliefStore dell'NPC.
        /// </para>
        ///
        /// <para><b>Failure operativo != sempre belief falsa</b></para>
        /// <para>
        /// Questo bridge copre solo fallimenti target-related del consume food. Non
        /// interpreta reservation, preemption, movement o piano mancante come
        /// contradiction, perche' quei casi possono dipendere da scheduling,
        /// pathfinding o integrita' del job e non dal contenuto della belief Food.
        /// </para>
        ///
        /// <para><b>Struttura interna:</b></para>
        /// <list type="bullet">
        ///   <item><b>Gate intent</b>: agisce solo su <c>EatKnownFood</c>.</item>
        ///   <item><b>Gate reason</b>: accetta solo i diagnostic message Consume target-related richiesti.</item>
        ///   <item><b>Feedback</b>: usa categoria <c>Food</c> + <c>JobRequest.TargetCell</c>, non un nuovo contratto BeliefId.</item>
        /// </list>
        /// </summary>
        private void ApplyFoodExecutionFailureCognitiveFeedback(
            World world,
            int npcId,
            Job job,
            StepResult stepResult,
            int tick,
            Telemetry telemetry)
        {
            if (world == null || job == null)
                return;

            if (job.Request.IntentKind != DecisionIntentKind.EatKnownFood || !job.Request.HasTargetCell)
                return;

            if (!IsFoodExecutionTargetContradiction(stepResult.DiagnosticMessage))
                return;

            if (!world.Beliefs.TryGetValue(npcId, out var store) || store == null)
                return;

            if (TryFindFoodBeliefByPosition(store, job.Request.TargetCell, out var existingBelief)
                && existingBelief.Status == BeliefStatus.Discarded)
            {
                telemetry?.Counter("JobExecution.FoodFailureBeliefAlreadyDiscarded", 1);
                EmitFoodExecutionFailureLearningTrace(world, npcId, job, stepResult, tick, "FoodExecutionFailureAlreadyDiscarded");
                return;
            }

            var signal = new BeliefFailureSignal(
                npcId: npcId,
                beliefId: 0,
                category: BeliefCategory.Food,
                estimatedPosition: job.Request.TargetCell,
                failureKind: BeliefFailureKind.DirectLocalContradiction,
                penalty01: 1f,
                tick: tick);

            bool updated = _beliefUpdater.UpdateFromOperationalFailure(signal, store);
            telemetry?.Counter(updated ? "JobExecution.FoodFailureBeliefDiscarded" : "JobExecution.FoodFailureBeliefNoMatch", 1);

            if (updated && TryFindFoodBeliefByPosition(store, job.Request.TargetCell, out var updatedBelief))
            {
                var explainabilityConfig = world.Config?.Sim?.memory_belief_decision_explainability;
                if (!MemoryBeliefDecisionExplainabilityEmitter.ShouldWriteTrace(
                        explainabilityConfig,
                        MemoryBeliefDecisionTraceKind.Belief))
                    return;

                MemoryBeliefDecisionExplainabilityEmitter.TryWriteTrace(
                    explainabilityConfig,
                    world.MemoryBeliefDecisionExplainability,
                    new MemoryBeliefDecisionTrace
                    {
                        Kind = MemoryBeliefDecisionTraceKind.Belief,
                        Tick = tick,
                        NpcId = npcId,
                        Belief = new MemoryBeliefDecisionBeliefRecord
                        {
                            Operation = MemoryBeliefDecisionBeliefOperation.Discarded,
                            HasSourceTrace = false,
                            SourceTraceType = default,
                            Belief = ToBeliefRef(updatedBelief),
                            Reason = "BeliefContradiction:FoodExecutionTargetFailure:" + stepResult.DiagnosticMessage,
                        },
                    });
            }

            EmitFoodExecutionFailureLearningTrace(world, npcId, job, stepResult, tick, "OperationalFailure:FoodExecutionTargetFailure:" + stepResult.DiagnosticMessage);
        }

        private static bool IsFoodExecutionTargetContradiction(string diagnosticMessage)
        {
            return string.Equals(diagnosticMessage, "ConsumeFoodUnavailable", System.StringComparison.Ordinal)
                || string.Equals(diagnosticMessage, "ConsumeFoodObjectMissing", System.StringComparison.Ordinal)
                || string.Equals(diagnosticMessage, "ConsumeTargetNoLongerCoLocated", System.StringComparison.Ordinal)
                || string.Equals(diagnosticMessage, "ConsumeMissingFoodObject", System.StringComparison.Ordinal)
                || string.Equals(diagnosticMessage, "MoveFoodUnavailable", System.StringComparison.Ordinal)
                || string.Equals(diagnosticMessage, "MoveFoodObjectMissing", System.StringComparison.Ordinal)
                || string.Equals(diagnosticMessage, "MoveFoodStockMissing", System.StringComparison.Ordinal)
                || string.Equals(diagnosticMessage, "MoveTargetNoLongerAtCell", System.StringComparison.Ordinal);
        }

        private static bool TryFindFoodBeliefByPosition(BeliefStore store, Vector2Int targetCell, out BeliefEntry belief)
        {
            belief = default;

            if (store == null)
                return false;

            var entries = store.Entries;
            for (int i = 0; i < entries.Count; i++)
            {
                var entry = entries[i];
                if (entry.Category != BeliefCategory.Food || entry.EstimatedPosition != targetCell)
                    continue;

                belief = entry;
                return true;
            }

            return false;
        }

        private static void EmitFoodExecutionFailureLearningTrace(
            World world,
            int npcId,
            Job job,
            StepResult stepResult,
            int tick,
            string reason)
        {
            MemoryBeliefDecisionExplainabilityEmitter.TryWriteFailureLearningTrace(
                world.Config?.Sim?.memory_belief_decision_explainability,
                world.MemoryBeliefDecisionExplainability,
                npcId,
                tick,
                job.JobId,
                job.Request.TargetCell,
                stepResult.FailureReason,
                tick,
                1f,
                reason);
        }

        private static MemoryBeliefDecisionBeliefRef ToBeliefRef(BeliefEntry belief)
        {
            return new MemoryBeliefDecisionBeliefRef
            {
                Category = belief.Category,
                Status = belief.Status,
                Source = belief.Source,
                BeliefId = belief.BeliefId,
                EstimatedPosition = belief.EstimatedPosition,
                Confidence = belief.Confidence,
                Freshness = belief.Freshness,
                SourceCount = belief.SourceCount,
            };
        }
    }
}
