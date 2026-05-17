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

            if (action.Kind == JobActionKind.MoveToCell)
                return ExecuteMoveTo(world, runtime, npcId, job, action, npcCell, tick, explainabilityConfig, explainabilityRegistry);

            if (action.Kind == JobActionKind.Consume)
                return ExecuteConsumeKnownFood(world, runtime, npcId, job, action, npcCell, tick, explainabilityConfig, explainabilityRegistry);

            if (action.Kind == JobActionKind.PickUp)
                return ExecutePickUpObject(world, runtime, npcId, job, action, npcCell, tick, explainabilityConfig, explainabilityRegistry);

            if (action.Kind == JobActionKind.Drop)
                return ExecuteDropObject(world, runtime, npcId, job, action, npcCell, tick, explainabilityConfig, explainabilityRegistry);

            return StepResult.Failed(JobFailureReason.StepFailed, "UnsupportedJobActionInRuntimeSlice");
        }

        private static StepResult ExecuteMoveTo(
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
            if (!action.HasTargetCell)
                return StepResult.Failed(JobFailureReason.MissingTarget, "MoveMissingTargetCell");

            if (npcCell.X == action.TargetCell.x && npcCell.Y == action.TargetCell.y)
                return StepResult.Succeeded("MoveTargetReached");

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
