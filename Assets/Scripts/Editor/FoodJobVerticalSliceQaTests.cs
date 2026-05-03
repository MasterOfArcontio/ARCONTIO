using System.Collections.Generic;
using Arcontio.Core;
using Arcontio.Core.Config;
using Arcontio.Core.Diagnostics;
using NUnit.Framework;
using UnityEngine;

namespace Arcontio.Tests
{
    // =============================================================================
    // FoodJobVerticalSliceQaTests
    // =============================================================================
    /// <summary>
    /// <para>
    /// Test QA EditMode per la vertical slice v0.11.01 che collega food decision
    /// opt-in, template JSON minimale, job runtime state e command emission.
    /// </para>
    ///
    /// <para><b>Runtime job senza mutazione diretta del World</b></para>
    /// <para>
    /// I test verificano che il Job System produca <c>ICommand</c> nel proprio buffer,
    /// lasciando l'effetto reale al command pump. Questo protegge il confine
    /// architetturale Systems/Rules/Commands durante la reintegrazione progressiva.
    /// </para>
    ///
    /// <para><b>Struttura interna:</b></para>
    /// <list type="bullet">
    ///   <item><b>Registry</b>: carica e materializza il template food minimale.</item>
    ///   <item><b>Execution</b>: MoveTo lontano produce movimento, Consume sul target produce eat command.</item>
    ///   <item><b>Bridge</b>: il gate job impedisce command legacy duplicati.</item>
    /// </list>
    /// </summary>
    public sealed class FoodJobVerticalSliceQaTests
    {
        private const string TemplateJson =
            "{\"templates\":[{\"templateId\":\"food.eat_known_community_stock.v1\",\"phases\":[{\"phaseId\":\"reach_food\",\"kind\":\"ReachTarget\",\"isInterruptible\":true,\"actions\":[{\"actionId\":\"move_to_food\",\"kind\":\"MoveToCell\"}]},{\"phaseId\":\"consume_food\",\"kind\":\"Execute\",\"isInterruptible\":false,\"actions\":[{\"actionId\":\"consume_known_food\",\"kind\":\"Consume\"}]}]},{\"templateId\":\"generic.move_to_cell.v1\",\"phases\":[{\"phaseId\":\"move_to_cell\",\"kind\":\"ReachTarget\",\"isInterruptible\":true,\"actions\":[{\"actionId\":\"move_to_cell\",\"kind\":\"MoveToCell\"}]}]}]}";

        [Test]
        public void RegistryLoadsFoodAndMoveTemplates()
        {
            var registry = MakeRegistry();

            Assert.That(registry.Count, Is.EqualTo(2));
            Assert.That(registry.TryGetTemplate(JobTemplateRegistry.FoodKnownCommunityStockTemplateId, out _), Is.True);
            Assert.That(registry.TryGetTemplate(JobTemplateRegistry.GenericMoveToCellTemplateId, out _), Is.True);
        }

        [Test]
        public void FoodFactoryCreatesTwoPhasePlan()
        {
            var registry = MakeRegistry();

            bool created = FoodJobFactory.TryCreateKnownCommunityFoodJob(
                registry,
                npcId: 1,
                foodObjectId: 10,
                targetCell: new Vector2Int(5, 5),
                tick: 2,
                urgency01: 0.9f,
                beliefKey: "Food:1@5,5",
                out var job,
                out var reason);

            Assert.That(created, Is.True, reason);
            Assert.That(job.Plan.PhaseCount, Is.EqualTo(2));
            Assert.That(job.Plan.TryGetPhase(0, out var reach), Is.True);
            Assert.That(reach.Kind, Is.EqualTo(JobPhaseKind.ReachTarget));
            Assert.That(reach.TryGetAction(0, out var move), Is.True);
            Assert.That(move.Kind, Is.EqualTo(JobActionKind.MoveToCell));
            Assert.That(move.TargetObjectId, Is.EqualTo(10));
        }

        [Test]
        public void MoveFactoryCreatesSinglePhaseMovePlan()
        {
            var registry = MakeRegistry();

            bool created = MoveJobFactory.TryCreateMoveToCellJob(
                registry,
                npcId: 1,
                targetCell: new Vector2Int(4, 6),
                tick: 3,
                urgency01: 0.4f,
                debugLabel: "MoveFactoryQa",
                out var job,
                out var reason);

            Assert.That(created, Is.True, reason);
            Assert.That(job.Plan.PhaseCount, Is.EqualTo(1));
            Assert.That(job.Plan.TryGetPhase(0, out var phase), Is.True);
            Assert.That(phase.TryGetAction(0, out var move), Is.True);
            Assert.That(move.Kind, Is.EqualTo(JobActionKind.MoveToCell));
            Assert.That(move.HasTargetCell, Is.True);
            Assert.That(move.TargetCell, Is.EqualTo(new Vector2Int(4, 6)));
            Assert.That(move.TargetObjectId, Is.EqualTo(0));
        }

        [Test]
        public void JobExecutionWhenFarFromFoodEnqueuesMoveIntentCommand()
        {
            var world = MakeWorldWithNpcAndCommunityFood(npcX: 1, npcY: 1, foodX: 5, foodY: 5, out int npcId, out int foodId);
            AssignFoodJob(world, npcId, foodId, 5, 5);
            var system = new JobExecutionSystem();

            system.Update(world, new Tick(0, 1f), new MessageBus(), new Telemetry());

            var commands = world.JobRuntimeState.CommandBuffer.Snapshot();
            Assert.That(commands.Length, Is.EqualTo(1));
            Assert.That(commands[0], Is.TypeOf<SetMoveIntentCommand>());
        }

        [Test]
        public void GenericMoveJobWhenFarFromTargetEnqueuesMoveIntentCommand()
        {
            var world = MakeWorldWithNpcOnly(npcX: 1, npcY: 1, out int npcId);
            var job = AssignMoveJob(world, npcId, 4, 6);
            var system = new JobExecutionSystem();

            system.Update(world, new Tick(0, 1f), new MessageBus(), new Telemetry());

            var commands = world.JobRuntimeState.CommandBuffer.Snapshot();
            Assert.That(job.Status, Is.EqualTo(JobStatus.Running));
            Assert.That(commands.Length, Is.EqualTo(1));
            Assert.That(commands[0], Is.TypeOf<SetMoveIntentCommand>());
        }

        [Test]
        public void GenericMoveJobCompletesWhenTargetCellReached()
        {
            var world = MakeWorldWithNpcOnly(npcX: 4, npcY: 6, out int npcId);
            var job = AssignMoveJob(world, npcId, 4, 6);
            var system = new JobExecutionSystem();

            system.Update(world, new Tick(0, 1f), new MessageBus(), new Telemetry());

            Assert.That(job.Status, Is.EqualTo(JobStatus.Completed));
            Assert.That(world.JobRuntimeState.HasActiveJob(npcId), Is.False);
            Assert.That(world.JobRuntimeState.CommandBuffer.Count, Is.EqualTo(0));
        }

        [Test]
        public void GenericMoveJobDoesNotDuplicateCommandWhenIntentAlreadyMatches()
        {
            var world = MakeWorldWithNpcOnly(npcX: 1, npcY: 1, out int npcId);
            AssignMoveJob(world, npcId, 4, 6);
            world.SetMoveIntent(npcId, new MoveIntent
            {
                Active = true,
                TargetX = 4,
                TargetY = 6,
                TargetObjectId = 0,
                Reason = MoveIntentReason.None
            });
            var system = new JobExecutionSystem();

            system.Update(world, new Tick(0, 1f), new MessageBus(), new Telemetry());

            Assert.That(world.JobRuntimeState.CommandBuffer.Count, Is.EqualTo(0));
            Assert.That(world.JobRuntimeState.HasActiveJob(npcId), Is.True);
        }

        [Test]
        public void JobRuntimeStateIdleNpcAcceptsJobThroughArbiter()
        {
            var world = MakeWorldWithNpcOnly(npcX: 1, npcY: 1, out int npcId);
            var job = CreateMoveJob(npcId, 4, 6, urgency01: 0.25f);

            bool assigned = world.JobRuntimeState.TryAssignJob(npcId, job, 0, out var reason);

            Assert.That(assigned, Is.True, reason);
            Assert.That(world.JobRuntimeState.HasActiveJob(npcId), Is.True);
            Assert.That(world.JobRuntimeState.TryGetActiveJob(npcId, out _, out var activeJob), Is.True);
            Assert.That(activeJob.JobId, Is.EqualTo(job.JobId));
        }

        [Test]
        public void JobRuntimeStateRejectsSameOrLowerPriorityJobAndKeepsCurrent()
        {
            var world = MakeWorldWithNpcOnly(npcX: 1, npcY: 1, out int npcId);
            var current = AssignMoveJob(world, npcId, 4, 6, urgency01: 0.40f);
            var lower = CreateMoveJob(npcId, 7, 8, urgency01: 0.35f);

            bool assigned = world.JobRuntimeState.TryAssignJob(npcId, lower, 1, out var reason);

            Assert.That(assigned, Is.False, reason);
            Assert.That(reason, Is.EqualTo("CurrentStillPreferred"));
            Assert.That(world.JobRuntimeState.TryGetActiveJob(npcId, out _, out var activeJob), Is.True);
            Assert.That(activeJob.JobId, Is.EqualTo(current.JobId));
            Assert.That(current.Status, Is.Not.EqualTo(JobStatus.Failed));
        }

        [Test]
        public void JobRuntimeStatePreemptsCurrentJobWhenNewJobHasHigherPriority()
        {
            var world = MakeWorldWithNpcOnly(npcX: 1, npcY: 1, out int npcId);
            var current = AssignMoveJob(world, npcId, 4, 6, urgency01: 0.25f);
            var urgentFood = CreateFoodJob(npcId, foodId: 77, foodX: 5, foodY: 5, urgency01: 0.95f);

            bool assigned = world.JobRuntimeState.TryAssignJob(npcId, urgentFood, 1, out var reason);

            Assert.That(assigned, Is.True, reason);
            Assert.That(reason, Is.EqualTo("HigherPriorityClass"));
            Assert.That(current.Status, Is.EqualTo(JobStatus.Failed));
            Assert.That(current.FailureReason, Is.EqualTo(JobFailureReason.Preempted));
            Assert.That(world.JobRuntimeState.TryGetActiveJob(npcId, out _, out var activeJob), Is.True);
            Assert.That(activeJob.JobId, Is.EqualTo(urgentFood.JobId));
        }

        [Test]
        public void NeedsDecisionRuleFallsBackToLegacyWhenJobArbiterRejectsFoodRoute()
        {
            var world = MakeWorldWithNpcAndCommunityFood(npcX: 5, npcY: 5, foodX: 5, foodY: 5, out int npcId, out _);
            AddFoodBelief(world, npcId, 5, 5);
            AssignFoodJob(world, npcId, 77, 5, 5, urgency01: 0.90f);
            var rule = new NeedsDecisionRule(1, 8, enableFoodJobVerticalSlice: true, jobTemplateRegistry: MakeRegistry());
            var commands = new List<ICommand>();

            rule.Handle(world, new TickPulseEvent(0), commands, new Telemetry());

            Assert.That(commands.Count, Is.GreaterThanOrEqualTo(1));
            Assert.That(commands[0], Is.TypeOf<EatFromStockCommand>());
        }

        [Test]
        public void JobRuntimeStateRejectsSecondNpcForReservedFoodTarget()
        {
            var world = MakeWorldWithNpcAndCommunityFood(npcX: 1, npcY: 1, foodX: 5, foodY: 5, out int firstNpcId, out int foodId);
            int secondNpcId = CreateAdditionalNpc(world, 2, 1);
            AssignFoodJob(world, firstNpcId, foodId, 5, 5, urgency01: 0.95f);
            var secondJob = CreateFoodJob(secondNpcId, foodId, 5, 5, urgency01: 0.95f);

            bool assigned = world.JobRuntimeState.TryAssignJob(secondNpcId, secondJob, 1, out var reason);

            Assert.That(assigned, Is.False, reason);
            Assert.That(reason, Is.EqualTo("ReservationDenied"));
            Assert.That(world.JobRuntimeState.Reservations.Count, Is.EqualTo(1));
            Assert.That(world.JobRuntimeState.HasActiveJob(firstNpcId), Is.True);
            Assert.That(world.JobRuntimeState.HasActiveJob(secondNpcId), Is.False);
        }

        [Test]
        public void JobRuntimeStateReleasesReservationWhenJobCompletes()
        {
            var world = MakeWorldWithNpcOnly(npcX: 4, npcY: 6, out int npcId);
            AssignMoveJob(world, npcId, 4, 6);
            var system = new JobExecutionSystem();

            Assert.That(world.JobRuntimeState.Reservations.Count, Is.EqualTo(1));

            system.Update(world, new Tick(0, 1f), new MessageBus(), new Telemetry());

            Assert.That(world.JobRuntimeState.HasActiveJob(npcId), Is.False);
            Assert.That(world.JobRuntimeState.Reservations.Count, Is.EqualTo(0));
        }

        [Test]
        public void JobRuntimeStateReleasesReservationWhenJobFails()
        {
            var world = MakeWorldWithNpcAndCommunityFood(npcX: 5, npcY: 5, foodX: 5, foodY: 5, out int npcId, out int foodId);
            AssignFoodJob(world, npcId, foodId, 5, 5, urgency01: 0.95f);
            world.FoodStocks[foodId] = new FoodStockComponent
            {
                Units = 0,
                OwnerKind = OwnerKind.Community,
                OwnerId = 0
            };
            var system = new JobExecutionSystem();

            Assert.That(world.JobRuntimeState.Reservations.Count, Is.EqualTo(1));
            system.Update(world, new Tick(0, 1f), new MessageBus(), new Telemetry());
            system.Update(world, new Tick(1, 1f), new MessageBus(), new Telemetry());

            Assert.That(world.JobRuntimeState.HasActiveJob(npcId), Is.False);
            Assert.That(world.JobRuntimeState.Reservations.Count, Is.EqualTo(0));
        }

        [Test]
        public void NeedsDecisionRuleFallsBackToLegacyWhenFoodReservationDenied()
        {
            var world = MakeWorldWithNpcAndCommunityFood(npcX: 5, npcY: 5, foodX: 5, foodY: 5, out int firstNpcId, out int foodId);
            int secondNpcId = CreateAdditionalNpc(world, 5, 5);
            AddFoodBelief(world, firstNpcId, 5, 5);
            AddFoodBelief(world, secondNpcId, 5, 5);
            AssignFoodJob(world, firstNpcId, foodId, 5, 5, urgency01: 0.95f);
            var rule = new NeedsDecisionRule(1, 8, enableFoodJobVerticalSlice: true, jobTemplateRegistry: MakeRegistry());
            var commands = new List<ICommand>();

            rule.Handle(world, new TickPulseEvent(0), commands, new Telemetry());

            Assert.That(commands.Count, Is.GreaterThanOrEqualTo(1));
            Assert.That(commands[0], Is.TypeOf<EatFromStockCommand>());
            Assert.That(world.JobRuntimeState.HasActiveJob(firstNpcId), Is.True);
        }

        [Test]
        public void JobRuntimeSnapshotIsEmptyForIdleNpc()
        {
            var world = MakeWorldWithNpcOnly(npcX: 1, npcY: 1, out int npcId);

            var snapshot = world.JobRuntimeState.GetSnapshot(npcId, tick: 12);

            Assert.That(snapshot.NpcId, Is.EqualTo(npcId));
            Assert.That(snapshot.HasActiveJob, Is.False);
            Assert.That(snapshot.CurrentJobId, Is.Empty);
            Assert.That(snapshot.TemplateId, Is.Empty);
            Assert.That(snapshot.CurrentPhaseId, Is.Empty);
            Assert.That(snapshot.CurrentActionId, Is.Empty);
            Assert.That(snapshot.HasTargetCell, Is.False);
            Assert.That(snapshot.TargetObjectId, Is.EqualTo(0));
            Assert.That(snapshot.LastFailureReason, Is.EqualTo(JobFailureReason.None));
            Assert.That(snapshot.ElapsedTicks, Is.EqualTo(0));
        }

        [Test]
        public void JobRuntimeSnapshotReportsActiveJobCursorAndTarget()
        {
            var world = MakeWorldWithNpcAndCommunityFood(npcX: 1, npcY: 1, foodX: 5, foodY: 5, out int npcId, out int foodId);
            var job = AssignFoodJob(world, npcId, foodId, 5, 5, urgency01: 0.95f);

            var snapshot = world.JobRuntimeState.GetSnapshot(npcId, tick: 7);

            Assert.That(snapshot.HasActiveJob, Is.True);
            Assert.That(snapshot.CurrentJobId, Is.EqualTo(job.JobId));
            Assert.That(snapshot.TemplateId, Is.EqualTo(JobTemplateRegistry.FoodKnownCommunityStockTemplateId));
            Assert.That(snapshot.CurrentPhaseId, Is.EqualTo("reach_food"));
            Assert.That(snapshot.CurrentActionId, Is.EqualTo("move_to_food"));
            Assert.That(snapshot.HasTargetCell, Is.True);
            Assert.That(snapshot.TargetCell, Is.EqualTo(new Vector2Int(5, 5)));
            Assert.That(snapshot.TargetObjectId, Is.EqualTo(foodId));
            Assert.That(snapshot.Status, Is.EqualTo(JobStatus.Created));
            Assert.That(snapshot.LastFailureReason, Is.EqualTo(JobFailureReason.None));
            Assert.That(snapshot.ElapsedTicks, Is.EqualTo(7));
        }

        [Test]
        public void JobRuntimeSnapshotReportsLastFailureReasonAfterFail()
        {
            var world = MakeWorldWithNpcOnly(npcX: 1, npcY: 1, out int npcId);
            AssignMoveJob(world, npcId, 4, 6);

            bool failed = world.JobRuntimeState.FailCurrentJob(npcId, JobFailureReason.MovementFailed, 4, out var reason);
            var snapshot = world.JobRuntimeState.GetSnapshot(npcId, tick: 5);

            Assert.That(failed, Is.True, reason);
            Assert.That(snapshot.HasActiveJob, Is.False);
            Assert.That(snapshot.LastFailureReason, Is.EqualTo(JobFailureReason.MovementFailed));
            Assert.That(snapshot.CurrentJobId, Is.Empty);
        }

        [Test]
        public void JobRuntimeSnapshotReadDoesNotMutateRuntimeState()
        {
            var world = MakeWorldWithNpcOnly(npcX: 1, npcY: 1, out int npcId);
            var job = AssignMoveJob(world, npcId, 4, 6);
            int activeJobCount = world.JobRuntimeState.ActiveJobCount;
            int reservationCount = world.JobRuntimeState.Reservations.Count;

            var first = world.JobRuntimeState.GetSnapshot(npcId, tick: 2);
            var second = world.JobRuntimeState.GetSnapshot(npcId, tick: 3);

            Assert.That(first.CurrentJobId, Is.EqualTo(job.JobId));
            Assert.That(second.CurrentJobId, Is.EqualTo(job.JobId));
            Assert.That(world.JobRuntimeState.ActiveJobCount, Is.EqualTo(activeJobCount));
            Assert.That(world.JobRuntimeState.Reservations.Count, Is.EqualTo(reservationCount));
            Assert.That(world.JobRuntimeState.HasActiveJob(npcId), Is.True);
        }

        [Test]
        public void JobExecutionWhenOnFoodTargetEnqueuesEatFromStockCommand()
        {
            var world = MakeWorldWithNpcAndCommunityFood(npcX: 5, npcY: 5, foodX: 5, foodY: 5, out int npcId, out int foodId);
            AssignFoodJob(world, npcId, foodId, 5, 5);
            var system = new JobExecutionSystem();

            system.Update(world, new Tick(0, 1f), new MessageBus(), new Telemetry());
            world.JobRuntimeState.CommandBuffer.Clear();
            system.Update(world, new Tick(1, 1f), new MessageBus(), new Telemetry());

            var commands = world.JobRuntimeState.CommandBuffer.Snapshot();
            Assert.That(commands.Length, Is.EqualTo(1));
            Assert.That(commands[0], Is.TypeOf<EatFromStockCommand>());
        }

        [Test]
        public void NeedsDecisionRuleWithAcceptedFoodJobDoesNotEmitLegacyCommand()
        {
            var world = MakeWorldWithNpcAndCommunityFood(npcX: 5, npcY: 5, foodX: 5, foodY: 5, out int npcId, out _);
            AddFoodBelief(world, npcId, 5, 5);
            var rule = new NeedsDecisionRule(1, 8, enableFoodJobVerticalSlice: true, jobTemplateRegistry: MakeRegistry());
            var commands = new List<ICommand>();

            rule.Handle(world, new TickPulseEvent(0), commands, new Telemetry());

            Assert.That(commands.Count, Is.EqualTo(0));
            Assert.That(world.JobRuntimeState.HasActiveJob(npcId), Is.True);
        }

        [Test]
        public void NeedsDecisionRuleWithGateFalseKeepsLegacyFoodCommandFallback()
        {
            var world = MakeWorldWithNpcAndCommunityFood(npcX: 5, npcY: 5, foodX: 5, foodY: 5, out int npcId, out _);
            AddFoodBelief(world, npcId, 5, 5);
            var rule = new NeedsDecisionRule(1, 8, enableFoodJobVerticalSlice: false, jobTemplateRegistry: MakeRegistry());
            var commands = new List<ICommand>();

            rule.Handle(world, new TickPulseEvent(0), commands, new Telemetry());

            Assert.That(commands.Count, Is.GreaterThanOrEqualTo(1));
            Assert.That(commands[0], Is.TypeOf<EatFromStockCommand>());
            Assert.That(world.JobRuntimeState.HasActiveJob(npcId), Is.False);
        }

        private static JobTemplateRegistry MakeRegistry()
        {
            var registry = new JobTemplateRegistry();
            registry.LoadFromJson(TemplateJson);
            return registry;
        }

        private static World MakeWorldWithNpcAndCommunityFood(int npcX, int npcY, int foodX, int foodY, out int npcId, out int foodId)
        {
            var world = MakeWorldWithNpcOnly(npcX, npcY, out npcId);

            foodId = 77;
            world.Objects[foodId] = new WorldObjectInstance
            {
                ObjectId = foodId,
                DefId = "food_stock",
                CellX = foodX,
                CellY = foodY,
                OwnerKind = OwnerKind.Community,
                OwnerId = 0
            };

            world.FoodStocks[foodId] = new FoodStockComponent
            {
                Units = 3,
                OwnerKind = OwnerKind.Community,
                OwnerId = 0
            };

            return world;
        }

        private static World MakeWorldWithNpcOnly(int npcX, int npcY, out int npcId)
        {
            var world = new World(new WorldConfig(new SimulationParams()));
            world.Global.Needs = NeedsConfig.Default();
            world.Global.BeliefQuery = BeliefQueryConfig.Default();
            world.Global.NpcOperationalRangeCells = 16;
            world.Global.NpcVisionRangeCells = 16;
            world.Global.NpcVisionUseCone = false;

            npcId = world.CreateNpc(
                NpcDnaProfile.CreateDefault("food_job_qa"),
                NpcNeeds.Make(0.95f, 0.1f),
                new Arcontio.Core.Social { JusticePerception01 = 0.9f },
                npcX,
                npcY);

            return world;
        }

        private static int CreateAdditionalNpc(World world, int npcX, int npcY)
        {
            return world.CreateNpc(
                NpcDnaProfile.CreateDefault("food_job_qa_extra"),
                NpcNeeds.Make(0.95f, 0.1f),
                new Arcontio.Core.Social { JusticePerception01 = 0.9f },
                npcX,
                npcY);
        }

        private static void AddFoodBelief(World world, int npcId, int x, int y)
        {
            world.Beliefs[npcId].AddOrMergeByCategoryAndPosition(
                BeliefCategory.Food,
                new Vector2Int(x, y),
                confidence: 0.95f,
                freshness: 0.95f,
                currentTick: 0,
                source: BeliefSource.Seen);
        }

        private static void AssignFoodJob(World world, int npcId, int foodId, int foodX, int foodY)
        {
            AssignFoodJob(world, npcId, foodId, foodX, foodY, urgency01: 0.95f);
        }

        private static Job AssignFoodJob(World world, int npcId, int foodId, int foodX, int foodY, float urgency01)
        {
            var job = CreateFoodJob(npcId, foodId, foodX, foodY, urgency01);

            string reason;
            Assert.That(world.JobRuntimeState.TryAssignJob(npcId, job, 0, out reason), Is.True, reason);
            return job;
        }

        private static Job AssignMoveJob(World world, int npcId, int targetX, int targetY)
        {
            return AssignMoveJob(world, npcId, targetX, targetY, urgency01: 0.25f);
        }

        private static Job AssignMoveJob(World world, int npcId, int targetX, int targetY, float urgency01)
        {
            var job = CreateMoveJob(npcId, targetX, targetY, urgency01);
            string reason;
            Assert.That(world.JobRuntimeState.TryAssignJob(npcId, job, 0, out reason), Is.True, reason);
            return job;
        }

        private static Job CreateMoveJob(int npcId, int targetX, int targetY, float urgency01)
        {
            bool created = MoveJobFactory.TryCreateMoveToCellJob(
                MakeRegistry(),
                npcId,
                new Vector2Int(targetX, targetY),
                0,
                urgency01,
                "MoveJobQa",
                out var job,
                out var reason);

            Assert.That(created, Is.True, reason);
            return job;
        }

        private static Job CreateFoodJob(int npcId, int foodId, int foodX, int foodY, float urgency01)
        {
            bool created = FoodJobFactory.TryCreateKnownCommunityFoodJob(
                MakeRegistry(),
                npcId,
                foodId,
                new Vector2Int(foodX, foodY),
                0,
                urgency01,
                "Food:1",
                out var job,
                out var reason);

            Assert.That(created, Is.True, reason);
            return job;
        }
    }
}
