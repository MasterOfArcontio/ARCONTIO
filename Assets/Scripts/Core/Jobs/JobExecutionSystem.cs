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
    ///   <item><b>MoveToCell</b>: accoda <c>SetMoveIntentCommand</c> se serve avvicinarsi.</item>
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

        public int Period => 1;

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
                    (int)tick.Index,
                    explainabilityConfig,
                    explainabilityRegistry);
                var updatedState = npcState;

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

        private static StepResult ExecuteCurrentAction(
            World world,
            JobRuntimeState runtime,
            RunningActionExecutor runningActionExecutor,
            int npcId,
            in NpcJobState npcState,
            Job job,
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
                return ExecuteMoveTo(world, runtime, runningActionExecutor, npcId, job, action, npcCell, tick, explainabilityConfig, explainabilityRegistry);

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
            int tick,
            MemoryBeliefDecisionExplainabilityParams explainabilityConfig,
            MemoryBeliefDecisionExplainabilityRegistry explainabilityRegistry)
        {
            if (!action.HasTargetCell)
                return StepResult.Failed(JobFailureReason.MissingTarget, "MoveMissingTargetCell");

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
                    tick,
                    explainabilityConfig,
                    explainabilityRegistry);
            }

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

        private static StepResult ExecuteRunningCellTraversalAction(
            World world,
            JobRuntimeState runtime,
            RunningActionExecutor runningActionExecutor,
            int npcId,
            Job job,
            JobAction action,
            GridPosition npcCell,
            int tick,
            MemoryBeliefDecisionExplainabilityParams explainabilityConfig,
            MemoryBeliefDecisionExplainabilityRegistry explainabilityRegistry)
        {
            if (world == null || runtime == null || job == null)
                return StepResult.Failed(JobFailureReason.MovementFailed, "TraversalRuntimeMissing");

            // La chiave resta agganciata al cursore job corrente: il progress
            // volatile non diventa una seconda authority su destinazione o path.
            var key = ResolveRunningActionKey(runtime, npcId, job.JobId);

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

        private static bool TryEnsureTraversalDestinationReservation(
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
                tick + ResolveTraversalReservationDurationTicks(action));

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

        private static int ResolveTraversalReservationDurationTicks(JobAction action)
        {
            // La reservation temporale action-scoped viene tenuta abbastanza a lungo
            // da coprire il prossimo tick di progress anche se il chiamante usa un
            // DurationTicks esplicito in futuro. Per il MoveToCell 02g la durata
            // effettiva resta nella config movement, quindi qui manteniamo solo una
            // finestra difensiva e il refresh al tick successivo avviene dal path
            // TryEnsureTraversalDestinationReservation.
            return Mathf.Max(2, action.DurationTicks + 2);
        }

        private static bool CanUseRunningActionCellTraversal(World world, GridPosition npcCell, Vector2Int targetCell)
        {
            if (world?.Config?.Sim?.movement == null || !world.Config.Sim.movement.enableJobRunningActionTraversal)
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
            // Il default tipizzato vive in MovementParams. Non leggiamo direttamente
            // game_params.json qui: JsonUtility popola world.Config e mantiene il
            // fallback se il campo non e' presente nel file dell'operatore.
            return Mathf.Max(1, world?.Config?.Sim?.movement?.baseWalkCellDurationTicks ?? 2);
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
                MemoryBeliefDecisionExplainabilityEmitter.TryWriteTrace(
                    world.Config?.Sim?.memory_belief_decision_explainability,
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
                || string.Equals(diagnosticMessage, "ConsumeMissingFoodObject", System.StringComparison.Ordinal);
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
