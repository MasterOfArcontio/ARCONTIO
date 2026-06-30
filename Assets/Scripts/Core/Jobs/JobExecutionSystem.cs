using System;
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
    ///   <item><b>MoveToCell</b>: usa solo running action e route possedute dal Job Layer; se non esiste una route autorizzata fallisce nel Job Layer.</item>
    ///   <item><b>PrepareHand</b>: libera la mano richiesta spostando l'oggetto nel Pack tramite command.</item>
    ///   <item><b>ReadyInventoryFood</b>: porta in mano una singola unita' di cibo gia' posseduto.</item>
    ///   <item><b>Consume</b>: accoda consumo inventario solo quando il cibo e' nella mano richiesta.</item>
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

            var costObserver = world.RuntimeCostObserver;
            bool costSample = costObserver != null && costObserver.ShouldSample(tick.Index);
            bool costPerNpc = costSample && costObserver.TrackPerNpc;
            long costStart = costSample ? costObserver.BeginSample() : 0L;
            int costSteps = 0;

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

                if (costSample)
                    costSteps++;
                if (costPerNpc)
                    costObserver.AddNpcWork(npcId, 1);

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
                    ApplyJobExitPerceptionState(world, npcId, job);
                    runtime.CompleteCurrentJob(npcId, (int)tick.Index, out _);
                    continue;
                }

                if (machineResult.TickResult == JobStateMachineTickResult.JobFailed)
                {
                    ApplyJobExitPerceptionState(world, npcId, job);
                    ApplyFoodExecutionFailureCognitiveFeedback(world, npcId, job, result, (int)tick.Index, telemetry);
                    runtime.FailCurrentJob(npcId, machineResult.FailureReason, (int)tick.Index, out _);
                    continue;
                }

                runtime.SetNpcState(npcId, in updatedState);
            }

            if (costSample)
            {
                costObserver.AddCounter(RuntimeCostCounter.JobExecutionActiveNpcs, _activeNpcIds.Count);
                costObserver.AddCounter(RuntimeCostCounter.JobExecutionSteps, costSteps);
                costObserver.EndSample(RuntimeCostChannel.JobExecution, costStart);
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

            var costObserver = world?.RuntimeCostObserver;
            bool costSample = costObserver != null && costObserver.ShouldSample(tick);
            if (costSample)
            {
                costObserver.AddCounter(RuntimeCostCounter.JobRecoveryEvaluated, 1);
                if (costObserver.TrackPerNpc)
                    costObserver.AddNpcWork(npcId, 1);
            }

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

            if (costSample)
            {
                costObserver.AddCounter(RuntimeCostCounter.JobRecoveryRetryScheduled, 1);
                if (costObserver.TrackPerNpc)
                    costObserver.AddNpcWork(npcId, 1);
            }

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
            foreach (var pair in world.Objects)
            {
                int objectId = pair.Key;
                if (objectId == job.Request.TargetObjectId)
                    continue;

                if (!world.TryGetAvailableFoodObjectFacts(
                        objectId,
                        requireCommunityOwner: true,
                        out _,
                        out _,
                        out int foodX,
                        out int foodY))
                {
                    continue;
                }

                int distance = Mathf.Abs(foodX - npcCell.X) + Mathf.Abs(foodY - npcCell.Y);
                if (distance > maxSearchRadius || distance >= bestDistance)
                    continue;

                if (!world.HasLineOfSight(npcCell.X, npcCell.Y, foodX, foodY))
                    continue;

                bestDistance = distance;
                replacementFoodObjectId = objectId;
                replacementCell = new Vector2Int(foodX, foodY);
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

            if (!TryApplyPhasePerceptionState(world, npcId, phase.PerceptionState, out string perceptionStateReason))
                return StepResult.Failed(JobFailureReason.StepFailed, perceptionStateReason);

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

            if (action.Kind == JobActionKind.LookDirection)
                return ExecuteRunningLookDirectionAction(
                    world,
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
            {
                return MoveToRunningActionDriver.Execute(
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

            if (action.Kind == JobActionKind.Consume)
            {
                if (job.Request.IntentKind == DecisionIntentKind.EatCarriedFood)
                    return ExecuteConsumeCarriedFood(world, runtime, runningActionExecutor, npcId, in npcState, job, phase, action, tick, explainabilityConfig, explainabilityRegistry);

                return ExecuteConsumeKnownFood(world, runtime, runningActionExecutor, npcId, in npcState, job, phase, action, npcCell, tick, explainabilityConfig, explainabilityRegistry);
            }

            if (action.Kind == JobActionKind.PrepareHand)
                return ExecutePrepareHand(world, runtime, npcId, job, action, tick, explainabilityConfig, explainabilityRegistry);

            if (action.Kind == JobActionKind.ReadyInventoryFood)
                return ExecuteReadyInventoryFood(world, runtime, runningActionExecutor, npcId, in npcState, job, phase, action, tick, explainabilityConfig, explainabilityRegistry);

            if (action.Kind == JobActionKind.PickUp)
                return ExecutePickUpObject(world, runtime, npcId, job, action, npcCell, tick, explainabilityConfig, explainabilityRegistry);

            if (action.Kind == JobActionKind.Drop)
                return ExecuteDropObject(world, runtime, npcId, job, action, npcCell, tick, explainabilityConfig, explainabilityRegistry);

            return StepResult.Failed(JobFailureReason.StepFailed, "UnsupportedJobActionInRuntimeSlice");
        }

        private static StepResult ExecuteRunningLookDirectionAction(
            World world,
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
            if (world == null)
                return StepResult.Failed(JobFailureReason.StepFailed, "LookDirectionWorldMissing");

            if (!System.Enum.TryParse(action.PayloadKey ?? string.Empty, ignoreCase: true, out CardinalDirection direction))
                return StepResult.Failed(JobFailureReason.StepFailed, "LookDirectionInvalidPayload:" + action.PayloadKey);

            if (runtime == null || runningActionExecutor == null || job == null)
                return StepResult.Failed(JobFailureReason.StepFailed, "LookDirectionRuntimeMissing");

            // Lo step di osservazione non esegue percezione fuori ciclo e non legge
            // oggetti. Orienta soltanto l'NPC dentro il Job Layer; `World.SetFacing`
            // marca dirty percettivo immediato e il blocco percettivo centrale
            // consumera' il nuovo facing secondo le regole v0.20.
            if (world.GetNpcPerceptionActivityState(npcId) != NpcPerceptionActivityState.LookDirection)
                world.SetNpcPerceptionActivityState(npcId, NpcPerceptionActivityState.LookDirection);

            world.SetFacing(npcId, direction);

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
                    $"look_{job.JobId}_{npcState.ActivePhaseIndex}_{npcState.ActiveActionIndex}_{direction}",
                    RunningActionKind.Wait,
                    npcId,
                    job.JobId,
                    phase.PhaseId,
                    action.ActionId,
                    tick,
                    policy);

                if (!runtime.RunningActions.Register(key, runningAction, out var registerReason))
                    return StepResult.Failed(JobFailureReason.StepFailed, registerReason);

                MemoryBeliefDecisionExplainabilityEmitter.TryWriteRunningActionTrace(
                    explainabilityConfig,
                    explainabilityRegistry,
                    npcId,
                    tick,
                    MemoryBeliefDecisionRunningActionOperation.Started,
                    in key,
                    runningAction.ToSnapshot(),
                    "LookDirectionRunningActionStarted");

                return StepResult.Running("LookDirectionHolding:" + direction);
            }

            var executorResult = runningActionExecutor.Tick(
                runningAction,
                RunningActionExecutorTickRequest.Advance(1, tick, "LookDirectionRunningActionTick"));

            EmitRunningActionExecutionTrace(
                explainabilityConfig,
                explainabilityRegistry,
                npcId,
                tick,
                in key,
                executorResult);

            if (executorResult.Kind == RunningActionExecutorResultKind.Completed)
            {
                runtime.RunningActions.Clear(key);
                return StepResult.Succeeded("LookDirectionCompleted:" + direction);
            }

            if (executorResult.Kind == RunningActionExecutorResultKind.Advanced
                || executorResult.Kind == RunningActionExecutorResultKind.NoProgress)
                return StepResult.Running("LookDirectionHolding:" + direction);

            return StepResult.Failed(JobFailureReason.StepFailed, executorResult.Reason);
        }

        private static bool TryApplyPhasePerceptionState(World world, int npcId, string rawState, out string reason)
        {
            reason = string.Empty;
            if (world == null)
                return true;

            if (string.IsNullOrWhiteSpace(rawState))
                return true;

            if (!TryParsePerceptionActivityState(rawState, out var state))
            {
                reason = "InvalidPhasePerceptionState:" + rawState;
                return false;
            }

            if (world.GetNpcPerceptionActivityState(npcId) != state)
                world.SetNpcPerceptionActivityState(npcId, state);
            return true;
        }

        private static void ApplyJobExitPerceptionState(World world, int npcId, Job job)
        {
            if (world == null)
                return;

            string rawState = job?.Plan?.ExitPerceptionState;
            if (TryParsePerceptionActivityState(rawState, out var state))
            {
                if (world.GetNpcPerceptionActivityState(npcId) != state)
                    world.SetNpcPerceptionActivityState(npcId, state);
                return;
            }

            if (world.GetNpcPerceptionActivityState(npcId) == NpcPerceptionActivityState.LookDirection)
                world.SetNpcPerceptionActivityState(npcId, NpcPerceptionActivityState.Idle);
        }

        private static bool TryParsePerceptionActivityState(string rawState, out NpcPerceptionActivityState state)
        {
            if (string.IsNullOrWhiteSpace(rawState))
            {
                state = default;
                return false;
            }

            return System.Enum.TryParse(rawState.Trim(), ignoreCase: true, out state);
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

        private static StepResult ExecuteConsumeKnownFood(
            World world,
            JobRuntimeState runtime,
            RunningActionExecutor runningActionExecutor,
            int npcId,
            in NpcJobState npcState,
            Job job,
            JobPhase phase,
            JobAction action,
            GridPosition npcCell,
            int tick,
            MemoryBeliefDecisionExplainabilityParams explainabilityConfig,
            MemoryBeliefDecisionExplainabilityRegistry explainabilityRegistry)
        {
            if (action.TargetObjectId <= 0)
                return StepResult.Failed(JobFailureReason.MissingTarget, "ConsumeMissingFoodObject");

            if (!world.Objects.TryGetValue(action.TargetObjectId, out var foodObject) || foodObject == null)
                return StepResult.Failed(JobFailureReason.MissingTarget, "ConsumeFoodObjectMissing");

            ObjectFoodNutritionResult nutrition = ObjectFoodNutritionResolver.Resolve(
                world,
                foodObject.DefId,
                world.Global.Needs.eatSatietyGain,
                allowLegacyFallbackWhenDefinitionMissing: false);
            if (!nutrition.IsConsumableFood)
                return StepResult.Failed(JobFailureReason.InvalidRequest, "ConsumeTargetNotFood");

            NpcInventorySlotKind requiredSlot = ResolveInventorySlotPayload(action.PayloadKey, NpcInventorySlotKind.HandLeft);
            if (!IsHandInventorySlot(requiredSlot))
                return StepResult.Failed(JobFailureReason.InvalidRequest, "ConsumeFoodRequiresHandSlot");

            if (world.GetInventoryQuantity(npcId, foodObject.DefId, requiredSlot) <= 0)
                return StepResult.Failed(JobFailureReason.MissingTarget, "ConsumeFoodMissingInHand");

            return ExecuteTimedCommandAction(
                runtime,
                runningActionExecutor,
                npcId,
                in npcState,
                job,
                phase,
                action,
                tick,
                "consume",
                "ConsumeKnownFoodTimedActionStarted",
                "ConsumeKnownFoodTimedActionTick",
                "ConsumeKnownFoodTimedActionRunning",
                () => ValidateKnownFoodInHand(world, npcId, foodObject.DefId, requiredSlot, "ConsumeFoodMissingInHand"),
                () =>
                {
                    runtime.CommandBuffer.Enqueue(
                        new ConsumeInventoryItemCommand(npcId, foodObject.DefId, 0, requiredSlot),
                        explainabilityConfig,
                        explainabilityRegistry,
                        npcId,
                        tick,
                        job.JobId,
                        "ConsumeInventoryHandCommandEnqueued");
                    return StepResult.Succeeded("ConsumeInventoryHandCommandEnqueued");
                },
                explainabilityConfig,
                explainabilityRegistry);
        }

        private static StepResult ExecuteConsumeCarriedFood(
            World world,
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
            NpcInventorySlotKind requiredSlot = ResolveInventorySlotPayload(action.PayloadKey, NpcInventorySlotKind.HandLeft);
            if (!IsHandInventorySlot(requiredSlot))
                return StepResult.Failed(JobFailureReason.InvalidRequest, "ConsumeCarriedFoodRequiresHandSlot");

            if (!world.SelectBestFoodOnSelf(npcId, requiredSlot, out _, out _, out _))
                return StepResult.Failed(JobFailureReason.MissingTarget, "ConsumeCarriedFoodMissingInHand");

            return ExecuteTimedCommandAction(
                runtime,
                runningActionExecutor,
                npcId,
                in npcState,
                job,
                phase,
                action,
                tick,
                "consume",
                "ConsumeCarriedFoodTimedActionStarted",
                "ConsumeCarriedFoodTimedActionTick",
                "ConsumeCarriedFoodTimedActionRunning",
                () => ValidateCarriedFoodInHand(world, npcId, requiredSlot, "ConsumeCarriedFoodMissingInHand"),
                () =>
                {
                    runtime.CommandBuffer.Enqueue(
                        new ConsumeInventoryItemCommand(npcId, string.Empty, 0, requiredSlot),
                        explainabilityConfig,
                        explainabilityRegistry,
                        npcId,
                        tick,
                        job.JobId,
                        "ConsumeCarriedInventoryFoodCommandEnqueued");
                    return StepResult.Succeeded("ConsumeCarriedInventoryFoodCommandEnqueued");
                },
                explainabilityConfig,
                explainabilityRegistry);
        }

        private static StepResult ExecutePrepareHand(
            World world,
            JobRuntimeState runtime,
            int npcId,
            Job job,
            JobAction action,
            int tick,
            MemoryBeliefDecisionExplainabilityParams explainabilityConfig,
            MemoryBeliefDecisionExplainabilityRegistry explainabilityRegistry)
        {
            NpcInventorySlotKind handSlot = ResolveInventorySlotPayload(action.PayloadKey, NpcInventorySlotKind.HandLeft);
            if (!IsHandInventorySlot(handSlot))
                return StepResult.Failed(JobFailureReason.InvalidRequest, "PrepareHandRequiresHandSlot");

            if (job.Request.IntentKind == DecisionIntentKind.EatCarriedFood
                && world.SelectBestFoodOnSelf(npcId, handSlot, out _, out _, out _))
            {
                return StepResult.Succeeded("PrepareHandAlreadyHasFood");
            }

            if (!world.TryGetFirstInventoryObjectInSlot(npcId, handSlot, out int objectId))
                return StepResult.Succeeded("PrepareHandAlreadyFree");

            if (!world.CanMoveInventoryObjectToSlot(npcId, objectId, NpcInventorySlotKind.Pack, out string reason))
                return StepResult.Failed(JobFailureReason.StepFailed, "PrepareHandPackUnavailable:" + reason);

            runtime.CommandBuffer.Enqueue(
                new MoveInventoryObjectCommand(npcId, objectId, NpcInventorySlotKind.Pack),
                explainabilityConfig,
                explainabilityRegistry,
                npcId,
                tick,
                job.JobId,
                "PrepareHandMoveToPackCommandEnqueued");
            return StepResult.Succeeded("PrepareHandMoveToPackCommandEnqueued");
        }

        private static StepResult ExecuteReadyInventoryFood(
            World world,
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
            NpcInventorySlotKind handSlot = ResolveInventorySlotPayload(action.PayloadKey, NpcInventorySlotKind.HandLeft);
            if (!IsHandInventorySlot(handSlot))
                return StepResult.Failed(JobFailureReason.InvalidRequest, "ReadyInventoryFoodRequiresHandSlot");

            if (world.SelectBestFoodOnSelf(npcId, handSlot, out _, out _, out _))
                return StepResult.Succeeded("ReadyInventoryFoodAlreadyInHand");

            if (!world.TrySelectBestFoodObjectOnSelf(
                    npcId,
                    out int objectId,
                    out _,
                    out NpcInventorySlotKind sourceSlot,
                    out _,
                    out _))
            {
                return StepResult.Failed(JobFailureReason.MissingTarget, "ReadyInventoryFoodMissing");
            }

            if (sourceSlot == handSlot)
                return StepResult.Succeeded("ReadyInventoryFoodAlreadyInHand");

            return ExecuteTimedCommandAction(
                runtime,
                runningActionExecutor,
                npcId,
                in npcState,
                job,
                phase,
                action,
                tick,
                "ready_inventory_food",
                "ReadyInventoryFoodTimedActionStarted",
                "ReadyInventoryFoodTimedActionTick",
                "ReadyInventoryFoodTimedActionRunning",
                () => ValidateInventoryFoodAvailable(world, npcId, handSlot),
                () => CompleteReadyInventoryFood(
                    world,
                    runtime,
                    npcId,
                    job,
                    handSlot,
                    tick,
                    explainabilityConfig,
                    explainabilityRegistry),
                explainabilityConfig,
                explainabilityRegistry);
        }

        // =============================================================================
        // ExecuteTimedCommandAction
        // =============================================================================
        /// <summary>
        /// <para>
        /// Esegue uno step atomico temporizzato che non muta il World durante il
        /// progress e produce al massimo un command finale alla completion.
        /// </para>
        ///
        /// <para><b>Principio architetturale: tempo nel Job Layer, mutazione nel Command Gateway</b></para>
        /// <para>
        /// Questo helper non sostituisce running action specializzate come movimento,
        /// orientamento o wait esplicito. Serve solo agli step mono-command come
        /// preparare cibo in mano o consumarlo: lo stato <c>RunningAction</c> rende
        /// osservabile il progresso, mentre il World cambia solo quando il command
        /// autorizzato viene accodato alla fine.
        /// </para>
        /// </summary>
        private static StepResult ExecuteTimedCommandAction(
            JobRuntimeState runtime,
            RunningActionExecutor runningActionExecutor,
            int npcId,
            in NpcJobState npcState,
            Job job,
            JobPhase phase,
            JobAction action,
            int tick,
            string actionInstancePrefix,
            string startedReason,
            string tickReason,
            string runningReason,
            Func<StepResult> validateStillPossible,
            Func<StepResult> completeAction,
            MemoryBeliefDecisionExplainabilityParams explainabilityConfig,
            MemoryBeliefDecisionExplainabilityRegistry explainabilityRegistry)
        {
            if (runtime == null || runningActionExecutor == null || job == null)
                return StepResult.Failed(JobFailureReason.StepFailed, "TimedCommandRuntimeMissing");

            var key = new RunningActionKey(npcId, job.JobId, npcState.ActivePhaseIndex, npcState.ActiveActionIndex);
            StepResult validation = validateStillPossible != null
                ? validateStillPossible()
                : StepResult.Succeeded("TimedCommandValidationSkipped");

            if (validation.Status == StepResultStatus.Failed)
            {
                if (runtime.RunningActions.TryGet(key, out var invalidRunningAction) && invalidRunningAction != null)
                {
                    var failedResult = runningActionExecutor.Tick(
                        invalidRunningAction,
                        RunningActionExecutorTickRequest.Fail(tick, validation.FailureReason, validation.DiagnosticMessage));
                    EmitRunningActionExecutionTrace(explainabilityConfig, explainabilityRegistry, npcId, tick, in key, failedResult);
                    runtime.RunningActions.Clear(key);
                }

                return validation;
            }

            if (!runtime.RunningActions.TryGet(key, out var runningAction) || runningAction == null)
            {
                int requiredTicks = Mathf.Max(1, action.DurationTicks);
                var policy = new RunningActionCompletionPolicy(
                    requiredTicks,
                    timeoutTicks: 0,
                    failureReason: JobFailureReason.StepFailed,
                    interruptionReason: JobFailureReason.Cancelled);

                runningAction = RunningActionRuntimeState.Start(
                    $"{actionInstancePrefix}_{job.JobId}_{npcState.ActivePhaseIndex}_{npcState.ActiveActionIndex}",
                    RunningActionKind.UseObject,
                    npcId,
                    job.JobId,
                    phase.PhaseId,
                    action.ActionId,
                    tick,
                    policy);

                if (!runtime.RunningActions.Register(key, runningAction, out var registerReason))
                    return StepResult.Failed(JobFailureReason.StepFailed, registerReason);

                MemoryBeliefDecisionExplainabilityEmitter.TryWriteRunningActionTrace(
                    explainabilityConfig,
                    explainabilityRegistry,
                    npcId,
                    tick,
                    MemoryBeliefDecisionRunningActionOperation.Started,
                    in key,
                    runningAction.ToSnapshot(),
                    startedReason);
            }

            var executorResult = runningActionExecutor.Tick(
                runningAction,
                RunningActionExecutorTickRequest.Advance(1, tick, tickReason));

            EmitRunningActionExecutionTrace(
                explainabilityConfig,
                explainabilityRegistry,
                npcId,
                tick,
                in key,
                executorResult);

            if (executorResult.Kind == RunningActionExecutorResultKind.Completed)
            {
                runtime.RunningActions.Clear(key);
                return completeAction != null
                    ? completeAction()
                    : StepResult.Succeeded("TimedCommandActionCompleted");
            }

            if (executorResult.Kind == RunningActionExecutorResultKind.Advanced
                || executorResult.Kind == RunningActionExecutorResultKind.NoProgress)
            {
                return StepResult.Running(runningReason);
            }

            runtime.RunningActions.Clear(key);
            return StepResult.Failed(JobFailureReason.StepFailed, executorResult.Reason);
        }

        private static StepResult ValidateKnownFoodInHand(
            World world,
            int npcId,
            string foodDefId,
            NpcInventorySlotKind requiredSlot,
            string missingReason)
        {
            if (world == null || world.GetInventoryQuantity(npcId, foodDefId, requiredSlot) <= 0)
                return StepResult.Failed(JobFailureReason.MissingTarget, missingReason);

            return StepResult.Succeeded("KnownFoodStillInHand");
        }

        private static StepResult ValidateCarriedFoodInHand(
            World world,
            int npcId,
            NpcInventorySlotKind requiredSlot,
            string missingReason)
        {
            if (world == null || !world.SelectBestFoodOnSelf(npcId, requiredSlot, out _, out _, out _))
                return StepResult.Failed(JobFailureReason.MissingTarget, missingReason);

            return StepResult.Succeeded("CarriedFoodStillInHand");
        }

        private static StepResult ValidateInventoryFoodAvailable(World world, int npcId, NpcInventorySlotKind handSlot)
        {
            if (world == null)
                return StepResult.Failed(JobFailureReason.StepFailed, "ReadyInventoryFoodWorldMissing");

            if (world.SelectBestFoodOnSelf(npcId, handSlot, out _, out _, out _))
                return StepResult.Succeeded("ReadyInventoryFoodAlreadyInHand");

            if (world.TryGetFirstInventoryObjectInSlot(npcId, handSlot, out _))
                return StepResult.Failed(JobFailureReason.StepFailed, "ReadyInventoryFoodHandOccupied");

            if (!world.TrySelectBestFoodObjectOnSelf(
                    npcId,
                    out _,
                    out _,
                    out NpcInventorySlotKind sourceSlot,
                    out _,
                    out _))
            {
                return StepResult.Failed(JobFailureReason.MissingTarget, "ReadyInventoryFoodMissing");
            }

            return sourceSlot == handSlot
                ? StepResult.Succeeded("ReadyInventoryFoodAlreadyInHand")
                : StepResult.Succeeded("ReadyInventoryFoodAvailable");
        }

        private static StepResult CompleteReadyInventoryFood(
            World world,
            JobRuntimeState runtime,
            int npcId,
            Job job,
            NpcInventorySlotKind handSlot,
            int tick,
            MemoryBeliefDecisionExplainabilityParams explainabilityConfig,
            MemoryBeliefDecisionExplainabilityRegistry explainabilityRegistry)
        {
            if (world.SelectBestFoodOnSelf(npcId, handSlot, out _, out _, out _))
                return StepResult.Succeeded("ReadyInventoryFoodAlreadyInHand");

            if (world.TryGetFirstInventoryObjectInSlot(npcId, handSlot, out _))
                return StepResult.Failed(JobFailureReason.StepFailed, "ReadyInventoryFoodHandOccupied");

            if (!world.TrySelectBestFoodObjectOnSelf(
                    npcId,
                    out int objectId,
                    out _,
                    out NpcInventorySlotKind sourceSlot,
                    out _,
                    out _))
            {
                return StepResult.Failed(JobFailureReason.MissingTarget, "ReadyInventoryFoodMissing");
            }

            if (sourceSlot == handSlot)
                return StepResult.Succeeded("ReadyInventoryFoodAlreadyInHand");

            runtime.CommandBuffer.Enqueue(
                new MoveInventoryObjectCommand(
                    npcId,
                    objectId,
                    handSlot,
                    InventoryMoveQuantityPolicy.SingleUnitFromStack),
                explainabilityConfig,
                explainabilityRegistry,
                npcId,
                tick,
                job.JobId,
                "ReadyInventoryFoodMoveToHandCommandEnqueued");
            return StepResult.Succeeded("ReadyInventoryFoodMoveToHandCommandEnqueued");
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

            NpcInventorySlotKind preferredSlot = ResolveInventorySlotPayload(action.PayloadKey, NpcInventorySlotKind.None);
            runtime.CommandBuffer.Enqueue(
                new PickUpObjectCommand(npcId, action.TargetObjectId, preferredSlot),
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

        private static NpcInventorySlotKind ResolveInventorySlotPayload(string payloadKey, NpcInventorySlotKind fallback)
        {
            if (string.IsNullOrWhiteSpace(payloadKey))
                return fallback;

            if (System.Enum.TryParse(payloadKey.Trim(), ignoreCase: true, out NpcInventorySlotKind parsed))
                return parsed;

            return fallback;
        }

        private static bool IsHandInventorySlot(NpcInventorySlotKind slot)
        {
            return slot == NpcInventorySlotKind.HandLeft || slot == NpcInventorySlotKind.HandRight;
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
                || string.Equals(diagnosticMessage, "ConsumeFoodMissingInHand", System.StringComparison.Ordinal)
                || string.Equals(diagnosticMessage, "ConsumeTargetNotFood", System.StringComparison.Ordinal)
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
                SubjectId = belief.SubjectId,
                Confidence = belief.Confidence,
                Freshness = belief.Freshness,
                SourceCount = belief.SourceCount,
            };
        }
    }
}
