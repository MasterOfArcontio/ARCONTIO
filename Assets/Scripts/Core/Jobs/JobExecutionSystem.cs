using System.Collections.Generic;
using UnityEngine;
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

        public int Period => 1;

        public void Update(World world, Tick tick, MessageBus bus, Telemetry telemetry)
        {
            if (world?.JobRuntimeState == null)
                return;

            var runtime = world.JobRuntimeState;
            runtime.Reservations.PruneExpired((int)tick.Index);
            runtime.CopyNpcIdsWithActiveJobsTo(_activeNpcIds);

            for (int i = 0; i < _activeNpcIds.Count; i++)
            {
                int npcId = _activeNpcIds[i];
                if (!runtime.TryGetActiveJob(npcId, out var npcState, out var job) || job == null)
                    continue;

                var result = ExecuteCurrentAction(world, runtime, npcId, in npcState, job, (int)tick.Index);
                var updatedState = npcState;
                _stateMachine.ApplyStepResult(ref updatedState, job, result, (int)tick.Index);
                runtime.SetNpcState(npcId, in updatedState);
            }
        }

        private static StepResult ExecuteCurrentAction(
            World world,
            JobRuntimeState runtime,
            int npcId,
            in NpcJobState npcState,
            Job job,
            int tick)
        {
            if (!job.Plan.TryGetPhase(npcState.ActivePhaseIndex, out var phase))
                return StepResult.Failed(JobFailureReason.MissingPlan, "MissingJobPhase");

            if (!phase.TryGetAction(npcState.ActiveActionIndex, out var action))
                return StepResult.Failed(JobFailureReason.StepFailed, "MissingJobAction");

            if (!world.GridPos.TryGetValue(npcId, out var npcCell))
                return StepResult.Failed(JobFailureReason.MissingTarget, "NpcPositionMissing");

            if (action.Kind == JobActionKind.MoveToCell)
                return ExecuteMoveTo(world, runtime, npcId, action, npcCell);

            if (action.Kind == JobActionKind.Consume)
                return ExecuteConsumeKnownFood(world, runtime, npcId, action, npcCell);

            return StepResult.Failed(JobFailureReason.StepFailed, "UnsupportedJobActionInRuntimeSlice");
        }

        private static StepResult ExecuteMoveTo(
            World world,
            JobRuntimeState runtime,
            int npcId,
            JobAction action,
            GridPosition npcCell)
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
                runtime.CommandBuffer.Enqueue(new SetMoveIntentCommand(npcId, new MoveIntent
                {
                    Active = true,
                    TargetX = action.TargetCell.x,
                    TargetY = action.TargetCell.y,
                    Reason = MoveIntentReason.SeekFood,
                    TargetObjectId = action.TargetObjectId,
                    Urgency01 = 1f
                }));
            }

            return StepResult.Running(alreadyMovingToTarget ? "MoveAlreadyRequested" : "MoveCommandEnqueued");
        }

        private static StepResult ExecuteConsumeKnownFood(
            World world,
            JobRuntimeState runtime,
            int npcId,
            JobAction action,
            GridPosition npcCell)
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

            runtime.CommandBuffer.Enqueue(new EatFromStockCommand(npcId, action.TargetObjectId));
            return StepResult.Succeeded("ConsumeCommandEnqueued");
        }
    }
}
