using System.Collections.Generic;
using Arcontio.Core.Config;
using UnityEngine;

namespace Arcontio.Core
{
    // =============================================================================
    // MoveToRunningActionDriver
    // =============================================================================
    /// <summary>
    /// <para>
    /// Esecutore locale dello step <c>MoveToCell</c> quando un incarico deve muovere
    /// fisicamente un NPC verso una cella target.
    /// </para>
    ///
    /// <para><b>Principio architetturale: MoveTo possiede il movimento dell'incarico</b></para>
    /// <para>
    /// Il <c>JobExecutionSystem</c> non deve preparare tragitti, non deve scegliere
    /// direct path, non deve avviare macro-route landmark e non deve gestire porte.
    /// La sua responsabilita' e' avanzare lo stato del job e consumare lo
    /// <c>StepResult</c>. Questo driver concentra invece il tratto operativo
    /// "voglio arrivare a quella cella": prepara il tragitto, consuma una cella per
    /// volta tramite running action multi-tick, apre porte apribili, restituisce
    /// fallimenti espliciti quando il tragitto non e' praticabile.
    /// </para>
    ///
    /// <para>
    /// Il driver non decide nuovi obiettivi, non interroga belief, non cerca cibo
    /// alternativo e non applica recovery. Se il tragitto fallisce, restituisce un
    /// fallimento classificabile alla state machine del job; sara' la matrice dei
    /// recuperi a decidere cosa fare.
    /// </para>
    ///
    /// <para><b>Struttura interna:</b></para>
    /// <list type="bullet">
    ///   <item><b>Execute</b>: punto d'ingresso unico dello step MoveToCell.</item>
    ///   <item><b>Route preparation</b>: direct path percettivo, macro-route landmark e segmenti immediati.</item>
    ///   <item><b>Route consumption</b>: un solo passo fisico alla volta, tramite running action multi-tick.</item>
    ///   <item><b>Door handling</b>: porta chiusa apribile aperta localmente, porta chiusa a chiave come errore esplicito.</item>
    ///   <item><b>Legacy bridge</b>: SetMoveIntent resta solo quando il nuovo runtime movimento Job e' spento.</item>
    /// </list>
    /// </summary>
    public static class MoveToRunningActionDriver
    {
        private static readonly List<Vector2Int> RouteScratch = new(64);

        // =============================================================================
        // Execute
        // =============================================================================
        /// <summary>
        /// <para>
        /// Esegue lo step <c>MoveToCell</c> di un job, mantenendo l'autorita' del
        /// movimento dentro MoveTo.
        /// </para>
        /// </summary>
        public static StepResult Execute(
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

            PrepareMoveToRoute(world, npcId, job, action, npcCell);

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

            return EnqueueLegacyMoveIntent(
                world,
                runtime,
                npcId,
                job,
                action,
                tick,
                explainabilityConfig,
                explainabilityRegistry);
        }

        // =============================================================================
        // PrepareMoveToRoute
        // =============================================================================
        /// <summary>
        /// <para>
        /// Prepara il tragitto posseduto da MoveTo: prima prova il direct path
        /// percettivo verso il target finale; se non basta, avvia o consuma la
        /// macro-route landmark e costruisce un segmento immediato verso il prossimo
        /// waypoint.
        /// </para>
        /// </summary>
        private static void PrepareMoveToRoute(World world, int npcId, Job job, JobAction action, GridPosition npcCell)
        {
            if (!CanUseJobMovementRuntime(world) || world == null || !action.HasTargetCell)
                return;

            if (HasUsableDirectRoute(world, npcId, action.TargetCell))
                return;

            bool requireCurrentAcquisition = !CanUseDeclaredBeliefTargetRoute(job);
            if (TryPrepareDirectRouteToFinalTarget(world, npcId, action, npcCell, requireCurrentAcquisition))
                return;

            if (!HasUsableMacroRoute(world, npcId, action.TargetCell))
                world.BeginMacroRouteExecutionForNpc(npcId, action.TargetCell.x, action.TargetCell.y);

            if (TryPrepareMacroImmediateSegment(world, npcId, action.TargetCell, npcCell))
                return;

            TryPrepareDeclaredDebugForcedRoute(world, npcId, job, action, npcCell);
        }

        private static bool HasUsableDirectRoute(World world, int npcId, Vector2Int finalTarget)
        {
            if (world?.Pathfinding == null
                || !world.Pathfinding.DirectCommitExecution.TryGetValue(npcId, out var direct)
                || direct == null
                || !direct.Active
                || direct.CurrentPath == null
                || direct.CurrentPath.Count < 2)
            {
                return false;
            }

            if (direct.FinalTargetCellX == finalTarget.x && direct.FinalTargetCellY == finalTarget.y)
                return true;

            return HasUsableMacroRoute(world, npcId, finalTarget)
                && world.Pathfinding.MacroRouteExecution.TryGetValue(npcId, out var macro)
                && macro != null
                && macro.Active
                && direct.FinalTargetCellX == macro.ImmediateTargetX
                && direct.FinalTargetCellY == macro.ImmediateTargetY;
        }

        private static bool CanUseDeclaredBeliefTargetRoute(Job job)
        {
            return job != null
                && job.Request.IntentKind == DecisionIntentKind.EatKnownFood
                && string.Equals(job.Plan?.PlanId, JobTemplateRegistry.FoodKnownCommunityStockTemplateId, System.StringComparison.Ordinal)
                && job.Request.HasTargetCell
                && job.Request.TargetObjectId > 0;
        }

        private static bool TryPrepareDirectRouteToFinalTarget(
            World world,
            int npcId,
            JobAction action,
            GridPosition npcCell,
            bool requireCurrentAcquisition)
        {
            if (requireCurrentAcquisition
                && !CanAcquireDirectTarget(world, npcId, action.TargetCell.x, action.TargetCell.y, GetDirectCheckFov(world)))
            {
                return false;
            }

            RouteScratch.Clear();
            if (!MovementPathfinder.TryBuildGreedyDirectPath(world, npcId, npcCell.X, npcCell.Y, action.TargetCell.x, action.TargetCell.y, RouteScratch)
                || RouteScratch.Count < 2)
            {
                return false;
            }

            world.ClearDebugMacroRouteForNpc(npcId);
            world.SetDebugDirectPathForNpc(npcId, RouteScratch);
            return true;
        }

        private static bool TryPrepareMacroImmediateSegment(World world, int npcId, Vector2Int finalTarget, GridPosition npcCell)
        {
            if (!world.TryGetMacroExecutionImmediateTarget(npcId, out int immediateX, out int immediateY, out _, out _))
                return false;

            if (immediateX == npcCell.X && immediateY == npcCell.Y)
            {
                world.TryAdvanceMacroRouteExecutionAtCell(npcId, npcCell.X, npcCell.Y);
                if (!world.TryGetMacroExecutionImmediateTarget(npcId, out immediateX, out immediateY, out _, out _))
                    return false;
            }

            if (immediateX == finalTarget.x && immediateY == finalTarget.y)
            {
                return TryPrepareDirectRouteToFinalTarget(
                    world,
                    npcId,
                    JobAction.MoveTo("move_to_final", finalTarget, "move_to_final"),
                    npcCell,
                    requireCurrentAcquisition: false);
            }

            RouteScratch.Clear();
            int budget = ResolveLocalSearchVisitedBudget(world);
            if (!MovementPathfinder.TryBuildBoundedMovePath(world, npcId, npcCell.X, npcCell.Y, immediateX, immediateY, budget, RouteScratch)
                || RouteScratch.Count < 2)
            {
                return false;
            }

            world.SetDebugDirectPathForNpc(npcId, RouteScratch);
            return true;
        }

        private static bool HasUsableMacroRoute(World world, int npcId, Vector2Int finalTarget)
        {
            return world?.Pathfinding != null
                && world.Pathfinding.MacroRouteExecution.TryGetValue(npcId, out var macro)
                && macro != null
                && macro.Active
                && macro.FinalTargetCellX == finalTarget.x
                && macro.FinalTargetCellY == finalTarget.y;
        }

        private static bool TryPrepareDeclaredDebugForcedRoute(
            World world,
            int npcId,
            Job job,
            JobAction action,
            GridPosition npcCell)
        {
            if (world == null || job == null || !action.HasTargetCell)
                return false;

            bool isForcedMove = string.Equals(job.Plan?.PlanId, JobTemplateRegistry.GenericMoveToCellTemplateId, System.StringComparison.Ordinal)
                && string.Equals(job.Request.DebugLabel, MoveJobFactory.DevToolsForcedMoveToCellDebugLabel, System.StringComparison.Ordinal);
            bool isForcedTransport = string.Equals(job.Plan?.PlanId, JobTemplateRegistry.TransportObjectToCellTemplateId, System.StringComparison.Ordinal)
                && string.Equals(job.Request.DebugLabel, "DevToolsForcedTransportObject", System.StringComparison.Ordinal);
            if (!isForcedMove && !isForcedTransport)
                return false;

            RouteScratch.Clear();
            int manhattan = Mathf.Abs(action.TargetCell.x - npcCell.X) + Mathf.Abs(action.TargetCell.y - npcCell.Y);
            int budget = Mathf.Max(ResolveLocalSearchVisitedBudget(world), manhattan * 8, 64);
            bool found = MovementPathfinder.TryBuildBoundedMovePath(
                world, npcId, npcCell.X, npcCell.Y, action.TargetCell.x, action.TargetCell.y, budget, RouteScratch);

            if (!found || RouteScratch.Count < 2)
            {
                found = MovementPathfinder.TryBuildDebugForcedGreedyPath(
                    world, npcId, npcCell.X, npcCell.Y, action.TargetCell.x, action.TargetCell.y, RouteScratch);
            }

            if (!found || RouteScratch.Count < 2)
            {
                return false;
            }

            world.SetDebugDirectPathForNpc(npcId, RouteScratch);
            return true;
        }

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

            bool finalTargetRoute = directState.FinalTargetCellX == finalTargetCell.x && directState.FinalTargetCellY == finalTargetCell.y;
            bool macroSegmentRoute = HasUsableMacroRoute(world, npcId, finalTargetCell)
                && world.Pathfinding.MacroRouteExecution.TryGetValue(npcId, out var macro)
                && macro != null
                && macro.Active
                && directState.FinalTargetCellX == macro.ImmediateTargetX
                && directState.FinalTargetCellY == macro.ImmediateTargetY;

            if (!finalTargetRoute && !macroSegmentRoute)
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
            world.TryAdvanceMacroRouteExecutionAtCell(npcId, nextRouteCell.x, nextRouteCell.y);

            if (nextRouteCell.x == action.TargetCell.x && nextRouteCell.y == action.TargetCell.y)
                return StepResult.Succeeded("MoveToKnownRouteCompleted");

            return StepResult.Running("MoveToKnownRouteStepCompleted");
        }

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

            if (!TryGetTraversalDoorAtCell(world, targetCell, out int doorObjectId, out var doorInstance, out _))
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

            runtime.Reservations.Release(
                BuildTraversalDestinationReservationId(in key, targetCell),
                out _,
                explainabilityConfig,
                explainabilityRegistry,
                tick,
                npcId);
        }

        private static StepResult EnqueueLegacyMoveIntent(
            World world,
            JobRuntimeState runtime,
            int npcId,
            Job job,
            JobAction action,
            int tick,
            MemoryBeliefDecisionExplainabilityParams explainabilityConfig,
            MemoryBeliefDecisionExplainabilityRegistry explainabilityRegistry)
        {
            bool alreadyMovingToTarget =
                world.NpcMoveIntents.TryGetValue(npcId, out var currentIntent)
                && currentIntent.Active
                && currentIntent.TargetX == action.TargetCell.x
                && currentIntent.TargetY == action.TargetCell.y
                && currentIntent.TargetObjectId == action.TargetObjectId;

            if (!alreadyMovingToTarget)
            {
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

        private static bool CanAcquireDirectTarget(World world, int npcId, int targetX, int targetY, bool checkFov)
        {
            if (!world.GridPos.TryGetValue(npcId, out var pos))
                return false;

            if (!checkFov)
                return true;

            if (FovUtils.Manhattan(pos.X, pos.Y, targetX, targetY) <= 1)
                return true;

            int visionRange = world.Global.NpcVisionRangeCells;
            if (visionRange <= 0)
                visionRange = 6;

            bool useCone = world.Global.NpcVisionUseCone;
            float coneSlope = world.Global.NpcVisionConeSlope;

            if (!world.NpcFacing.TryGetValue(npcId, out var facing))
                facing = CardinalDirection.North;

            return FovUtils.IsVisible(world, pos.X, pos.Y, facing, targetX, targetY, visionRange, useCone, coneSlope);
        }

        private static bool GetDirectCheckFov(World world)
        {
            return world?.Config?.Sim?.movement?.directCheckFovOnAcquisition ?? new MovementParams().directCheckFovOnAcquisition;
        }

        private static int ResolveLocalSearchVisitedBudget(World world)
        {
            var cfg = world?.Config?.Sim?.landmarks?.localSearch ?? new LandmarkLocalSearchParams();
            int radius = Mathf.Max(1, cfg.maxSearchRadius);
            return Mathf.Max(8, radius * radius * 8);
        }

        private static string BuildTraversalDestinationReservationId(in RunningActionKey key, Vector2Int targetCell)
        {
            return "traversal:" + key.JobId + ":" + key.NpcId + ":" + key.PhaseIndex + ":" + key.ActionIndex + ":" + targetCell.x + ":" + targetCell.y;
        }

        private static int ResolveTraversalReservationDurationTicks(World world, JobAction action)
        {
            return Mathf.Max(1, Mathf.Max(ResolveBaseWalkCellDurationTicks(world), action.DurationTicks));
        }

        private static bool CanUseRunningActionCellTraversal(World world, GridPosition npcCell, Vector2Int targetCell)
        {
            if (!CanUseJobMovementRuntime(world))
                return false;

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

        private static bool CanUseJobMovementRuntime(World world)
        {
            return world?.Config?.Sim != null && world.Config.Sim.ResolveEnableJobRunningActionTraversal();
        }

        private static void EmitRunningActionExecutionTrace(
            MemoryBeliefDecisionExplainabilityParams explainabilityConfig,
            MemoryBeliefDecisionExplainabilityRegistry explainabilityRegistry,
            int npcId,
            int tick,
            in RunningActionKey key,
            RunningActionExecutorResult executorResult)
        {
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

        private static MemoryBeliefDecisionRunningActionOperation ResolveRunningActionOperation(RunningActionExecutorResultKind kind)
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
    }
}
