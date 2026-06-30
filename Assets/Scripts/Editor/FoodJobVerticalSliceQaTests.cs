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
            "{\"templates\":[{\"templateId\":\"food.eat_carried_inventory.v1\",\"phases\":[{\"phaseId\":\"prepare_food_hand\",\"kind\":\"Prepare\",\"isInterruptible\":false,\"actions\":[{\"actionId\":\"prepare_left_hand\",\"kind\":\"PrepareHand\",\"payloadKey\":\"HandLeft\"}]},{\"phaseId\":\"ready_carried_food\",\"kind\":\"Prepare\",\"isInterruptible\":false,\"actions\":[{\"actionId\":\"ready_inventory_food\",\"kind\":\"ReadyInventoryFood\",\"payloadKey\":\"HandLeft\"}]},{\"phaseId\":\"consume_carried_food\",\"kind\":\"Execute\",\"isInterruptible\":false,\"actions\":[{\"actionId\":\"consume_carried_food\",\"kind\":\"Consume\",\"payloadKey\":\"HandLeft\"}]}]},{\"templateId\":\"food.eat_known_community_stock.v1\",\"phases\":[{\"phaseId\":\"reach_food\",\"kind\":\"ReachTarget\",\"isInterruptible\":true,\"actions\":[{\"actionId\":\"move_to_food\",\"kind\":\"MoveToCell\"}]},{\"phaseId\":\"prepare_food_hand\",\"kind\":\"Prepare\",\"isInterruptible\":false,\"actions\":[{\"actionId\":\"prepare_left_hand\",\"kind\":\"PrepareHand\",\"payloadKey\":\"HandLeft\"}]},{\"phaseId\":\"take_and_consume_food\",\"kind\":\"Execute\",\"isInterruptible\":false,\"actions\":[{\"actionId\":\"pickup_food_to_hand\",\"kind\":\"PickUp\",\"payloadKey\":\"HandLeft\"},{\"actionId\":\"consume_known_food\",\"kind\":\"Consume\",\"payloadKey\":\"HandLeft\"}]}]},{\"templateId\":\"generic.move_to_cell.v1\",\"phases\":[{\"phaseId\":\"move_to_cell\",\"kind\":\"ReachTarget\",\"isInterruptible\":true,\"actions\":[{\"actionId\":\"move_to_cell\",\"kind\":\"MoveToCell\"}]}]}]}";

        [Test]
        public void RegistryLoadsFoodAndMoveTemplates()
        {
            var registry = MakeRegistry();

            Assert.That(registry.Count, Is.EqualTo(3));
            Assert.That(registry.TryGetTemplate(JobTemplateRegistry.FoodCarriedInventoryTemplateId, out _), Is.True);
            Assert.That(registry.TryGetTemplate(JobTemplateRegistry.FoodKnownCommunityStockTemplateId, out _), Is.True);
            Assert.That(registry.TryGetTemplate(JobTemplateRegistry.GenericMoveToCellTemplateId, out _), Is.True);
        }

        [Test]
        public void FoodFactoryCreatesThreePhasePlan()
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
            Assert.That(job.Plan.PhaseCount, Is.EqualTo(3));
            Assert.That(job.Plan.TryGetPhase(0, out var reach), Is.True);
            Assert.That(reach.Kind, Is.EqualTo(JobPhaseKind.ReachTarget));
            Assert.That(reach.TryGetAction(0, out var move), Is.True);
            Assert.That(move.Kind, Is.EqualTo(JobActionKind.MoveToCell));
            Assert.That(move.TargetObjectId, Is.EqualTo(10));
            Assert.That(job.Plan.TryGetPhase(1, out var prepare), Is.True);
            Assert.That(prepare.Kind, Is.EqualTo(JobPhaseKind.Prepare));
            Assert.That(prepare.TryGetAction(0, out var prepareHand), Is.True);
            Assert.That(prepareHand.Kind, Is.EqualTo(JobActionKind.PrepareHand));
        }

        [Test]
        public void FoodFactoryAcceptsPrebuiltJobRequest()
        {
            var registry = MakeRegistry();
            var request = MakeFoodJobRequest(
                npcId: 1,
                foodObjectId: 10,
                targetCell: new Vector2Int(5, 5),
                tick: 2,
                urgency01: 0.92f,
                beliefKey: "Food:99@5,5");

            bool created = FoodJobFactory.TryCreateKnownCommunityFoodJob(
                registry,
                request,
                out var job,
                out var reason);

            Assert.That(created, Is.True, reason);
            Assert.That(job, Is.Not.Null);
            AssertFoodBoundaryFields(job.Request, request);
            Assert.That(job.Plan.PlanId, Is.EqualTo(JobTemplateRegistry.FoodKnownCommunityStockTemplateId));
        }

        [Test]
        public void FoodFactoryCreatesCarriedInventoryFoodPlan()
        {
            var registry = MakeRegistry();
            var request = MakeCarriedFoodJobRequest(npcId: 1, tick: 2, urgency01: 0.92f);

            bool created = FoodJobFactory.TryCreateCarriedInventoryFoodJob(
                registry,
                request,
                out var job,
                out var reason);

            Assert.That(created, Is.True, reason);
            Assert.That(job.Plan.PlanId, Is.EqualTo(JobTemplateRegistry.FoodCarriedInventoryTemplateId));
            Assert.That(job.Plan.PhaseCount, Is.EqualTo(3));
            Assert.That(job.Plan.TryGetPhase(0, out var prepare), Is.True);
            Assert.That(prepare.TryGetAction(0, out var prepareHand), Is.True);
            Assert.That(prepareHand.Kind, Is.EqualTo(JobActionKind.PrepareHand));
            Assert.That(job.Plan.TryGetPhase(1, out var ready), Is.True);
            Assert.That(ready.TryGetAction(0, out var readyFood), Is.True);
            Assert.That(readyFood.Kind, Is.EqualTo(JobActionKind.ReadyInventoryFood));
            Assert.That(job.Plan.TryGetPhase(2, out var consume), Is.True);
            Assert.That(consume.TryGetAction(0, out var consumeFood), Is.True);
            Assert.That(consumeFood.Kind, Is.EqualTo(JobActionKind.Consume));
        }

        [Test]
        public void FoodFactoryAcceptsBeliefOnlyTargetWithoutObjectId()
        {
            var registry = MakeRegistry();
            var request = MakeFoodJobRequest(
                npcId: 1,
                foodObjectId: 0,
                targetCell: new Vector2Int(5, 5),
                tick: 2,
                urgency01: 0.92f,
                beliefKey: "Food:99@5,5");

            bool created = FoodJobFactory.TryCreateKnownCommunityFoodJob(
                registry,
                request,
                out var job,
                out var reason);

            Assert.That(created, Is.True, reason);
            Assert.That(job.Request.TargetObjectId, Is.EqualTo(0));
            Assert.That(job.Request.TargetCell, Is.EqualTo(new Vector2Int(5, 5)));
            Assert.That(job.Plan.TryGetPhase(0, out var reach), Is.True);
            Assert.That(reach.TryGetAction(0, out var move), Is.True);
            Assert.That(move.TargetObjectId, Is.EqualTo(0));
        }

        [Test]
        public void EatKnownFoodDecisionStartsJobFromBeliefOnlyTarget()
        {
            var world = MakeWorldWithNpcOnly(npcX: 5, npcY: 5, out int npcId);
            AddFoodBelief(world, npcId, 7, 5);
            PreferKnownFoodDecision(world, npcId);
            var orchestrator = new DecisionOrchestratorSystem(
                decisionEveryTicks: 1,
                maxSeekRangeCells: 16,
                enableFoodJobVerticalSlice: true,
                jobTemplateRegistry: MakeRegistry());

            orchestrator.Update(world, new Tick(0, 1f), new MessageBus(), new Telemetry());

            Assert.That(world.JobRuntimeState.TryGetActiveJob(npcId, out _, out var job), Is.True);
            Assert.That(job.Request.IntentKind, Is.EqualTo(DecisionIntentKind.EatKnownFood));
            Assert.That(job.Request.TargetObjectId, Is.EqualTo(0));
            Assert.That(job.Request.TargetCell, Is.EqualTo(new Vector2Int(7, 5)));
        }

        [Test]
        public void ActiveFoodJobDefersEquivalentCriticalHungerDecision()
        {
            var world = MakeWorldWithNpcAndCommunityFood(5, 5, 7, 5, out int npcId, out int foodId, enableMbdExplainability: true);
            AddFoodBelief(world, npcId, 7, 5);
            PreferKnownFoodDecision(world, npcId);
            AssignFoodJob(world, npcId, foodId, 7, 5, urgency01: 0.95f);

            var needs = world.Needs[npcId];
            needs.SetValue(NeedKind.Hunger, 1f);
            needs.SetFlags(NeedKind.Hunger, isAlert: true, isCritical: true);
            world.Needs[npcId] = needs;

            var orchestrator = new DecisionOrchestratorSystem(
                decisionEveryTicks: 1,
                maxSeekRangeCells: 16,
                enableFoodJobVerticalSlice: true,
                jobTemplateRegistry: MakeRegistry());

            orchestrator.Update(world, new Tick(80, 1f), new MessageBus(), new Telemetry());

            Assert.That(world.JobRuntimeState.ActiveJobCount, Is.EqualTo(1));
            Assert.That(world.JobRuntimeState.TryGetActiveJob(npcId, out _, out var job), Is.True);
            Assert.That(job.JobId, Does.StartWith("job_food_"));
            AssertNoJobRequestTrace(world, npcId);
            AssertNoDecisionTrace(world, npcId);
        }

        [Test]
        public void EatKnownFoodDecisionUsesSelectedBeliefWhenMultipleFoodStocksExist()
        {
            var world = MakeWorldWithNpcOnly(npcX: 1, npcY: 1, out int npcId);
            AddCommunityFoodStock(world, foodId: 11, x: 2, y: 1, units: 3);
            AddCommunityFoodStock(world, foodId: 44, x: 7, y: 5, units: 3);
            AddFoodBelief(world, npcId, 7, 5);
            PreferKnownFoodDecision(world, npcId);
            var orchestrator = new DecisionOrchestratorSystem(
                decisionEveryTicks: 1,
                maxSeekRangeCells: 16,
                enableFoodJobVerticalSlice: true,
                jobTemplateRegistry: MakeRegistry());

            orchestrator.Update(world, new Tick(0, 1f), new MessageBus(), new Telemetry());

            Assert.That(world.JobRuntimeState.TryGetActiveJob(npcId, out _, out var job), Is.True);
            Assert.That(job.Request.IntentKind, Is.EqualTo(DecisionIntentKind.EatKnownFood));
            Assert.That(job.Request.TargetCell, Is.EqualTo(new Vector2Int(7, 5)));
            Assert.That(job.Request.TargetObjectId, Is.EqualTo(44));
        }

        [Test]
        public void FoodFactoryPrebuiltRequestPreservesDecisionBoundaryFields()
        {
            var registry = MakeRegistry();
            var request = MakeFoodJobRequest(
                npcId: 7,
                foodObjectId: 44,
                targetCell: new Vector2Int(8, 3),
                tick: 12,
                urgency01: 0.73f,
                beliefKey: "Food:123@8,3");

            bool created = FoodJobFactory.TryCreateKnownCommunityFoodJob(
                registry,
                request,
                out var job,
                out var reason);

            Assert.That(created, Is.True, reason);
            AssertFoodBoundaryFields(job.Request, request);
            Assert.That(job.Request.IntentKind, Is.EqualTo(DecisionIntentKind.EatKnownFood));
            Assert.That(job.Request.Urgency01, Is.EqualTo(0.73f).Within(0.0001f));
            Assert.That(job.Request.BeliefKey, Is.EqualTo("Food:123@8,3"));
            Assert.That(job.Request.TargetCell, Is.EqualTo(new Vector2Int(8, 3)));
            Assert.That(job.Request.TargetObjectId, Is.EqualTo(44));
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
        public void JobExecutionWhenFarFromFoodStartsJobOwnedMoveToTraversal()
        {
            var world = MakeWorldWithNpcAndCommunityFood(npcX: 1, npcY: 1, foodX: 5, foodY: 5, out int npcId, out int foodId);
            AssignFoodJob(world, npcId, foodId, 5, 5);
            var system = new JobExecutionSystem();

            system.Update(world, new Tick(0, 1f), new MessageBus(), new Telemetry());

            var commands = world.JobRuntimeState.CommandBuffer.Snapshot();
            Assert.That(commands.Length, Is.EqualTo(0));
            Assert.That(world.JobRuntimeState.RunningActions.Count, Is.EqualTo(1));
            Assert.That(world.NpcMoveIntents.TryGetValue(npcId, out var intent) && intent.Active, Is.False);
        }

        [Test]
        public void GenericMoveJobWhenFarFromTargetStartsJobOwnedMoveToTraversal()
        {
            var world = MakeWorldWithNpcOnly(npcX: 1, npcY: 1, out int npcId);
            var job = AssignMoveJob(world, npcId, 4, 6);
            var system = new JobExecutionSystem();

            system.Update(world, new Tick(0, 1f), new MessageBus(), new Telemetry());

            var commands = world.JobRuntimeState.CommandBuffer.Snapshot();
            Assert.That(job.Status, Is.EqualTo(JobStatus.Running));
            Assert.That(commands.Length, Is.EqualTo(0));
            Assert.That(world.JobRuntimeState.RunningActions.Count, Is.EqualTo(1));
            Assert.That(world.NpcMoveIntents.TryGetValue(npcId, out var intent) && intent.Active, Is.False);
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
        public void GenericMoveJobIgnoresObsoleteMoveIntentAndUsesJobOwnedTraversal()
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
            Assert.That(world.JobRuntimeState.RunningActions.Count, Is.EqualTo(1));
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
            var world = MakeWorldWithNpcAndCommunityFood(npcX: 5, npcY: 5, foodX: 5, foodY: 5, out int npcId, out int foodId, enableMbdExplainability: true);
            AssignFoodJob(world, npcId, foodId, 5, 5, urgency01: 0.95f);
            world.SetFoodStock(foodId, new FoodStockComponent
            {
                Units = 0,
                OwnerKind = OwnerKind.Community,
                OwnerId = 0
            });
            var system = new JobExecutionSystem();

            Assert.That(world.JobRuntimeState.Reservations.Count, Is.EqualTo(1));
            system.Update(world, new Tick(0, 1f), new MessageBus(), new Telemetry());
            system.Update(world, new Tick(1, 1f), new MessageBus(), new Telemetry());

            Assert.That(world.JobRuntimeState.HasActiveJob(npcId), Is.False);
            Assert.That(world.JobRuntimeState.Reservations.Count, Is.EqualTo(0));
        }

        [Test]
        public void AssignedFoodJobConsumeFoodUnavailableUpdatesFoodBelief()
        {
            var world = MakeWorldWithNpcAndCommunityFood(npcX: 5, npcY: 5, foodX: 5, foodY: 5, out int npcId, out int foodId, enableMbdExplainability: true);
            AddFoodBelief(world, npcId, 5, 5);
            EnableMbdBridgeExplainability(world);
            AssignFoodJob(world, npcId, foodId, 5, 5, urgency01: 0.95f);
            world.SetFoodStock(foodId, new FoodStockComponent
            {
                Units = 0,
                OwnerKind = OwnerKind.Community,
                OwnerId = 0
            });
            var system = new JobExecutionSystem();

            // Il primo tick attraversa la fase ReachTarget gia' soddisfatta; il
            // secondo entra nella fase Consume e produce il failure reale del job.
            system.Update(world, new Tick(0, 1f), new MessageBus(), new Telemetry());
            system.Update(world, new Tick(1, 1f), new MessageBus(), new Telemetry());

            Assert.That(world.JobRuntimeState.HasActiveJob(npcId), Is.False);
            AssertFoodBeliefStatus(world, npcId, BeliefStatus.Discarded);
            AssertLatestBeliefTrace(
                world,
                npcId,
                MemoryBeliefDecisionBeliefOperation.Discarded,
                "BeliefContradiction:FoodExecutionTargetFailure:ConsumeFoodUnavailable");
            AssertLatestFailureLearning(
                world,
                npcId,
                JobFailureReason.MissingTarget,
                "OperationalFailure:FoodExecutionTargetFailure:ConsumeFoodUnavailable");
        }

        [Test]
        public void AssignedFoodJobConsumeFoodUnavailableCanReplaceWithVisibleEquivalentFood()
        {
            var world = MakeWorldWithNpcAndCommunityFood(npcX: 5, npcY: 5, foodX: 5, foodY: 5, out int npcId, out int foodId, enableMbdExplainability: true);
            int replacementFoodId = AddCommunityFoodStock(world, 88, 5, 5, units: 3);

            AddFoodBelief(world, npcId, 5, 5);
            AssignFoodJob(world, npcId, foodId, 5, 5, urgency01: 0.95f);
            world.SetFoodStock(foodId, new FoodStockComponent
            {
                Units = 0,
                OwnerKind = OwnerKind.Community,
                OwnerId = 0
            });

            var system = new JobExecutionSystem();
            system.Update(world, new Tick(0, 1f), new MessageBus(), new Telemetry());
            system.Update(world, new Tick(1, 1f), new MessageBus(), new Telemetry());

            Assert.That(world.JobRuntimeState.TryGetActiveJob(npcId, out var state, out var activeJob), Is.True);
            Assert.That(activeJob.Request.TargetObjectId, Is.EqualTo(replacementFoodId));
            Assert.That(activeJob.Request.TargetCell, Is.EqualTo(new Vector2Int(5, 5)));
            Assert.That(state.GetRecoveryAlternativeTargetCount(JobStepFailureKind.ResourceMissing, 1, 0), Is.EqualTo(1));
            Assert.That(world.JobRuntimeState.Reservations.TryGetByTarget(
                ReservationTargetKind.Object,
                new Vector2Int(5, 5),
                replacementFoodId,
                out _), Is.True);
            Assert.That(
                world.JobRuntimeState.FailureLearning.GetCount(npcId, DecisionIntentKind.EatKnownFood, JobFailureReason.MissingTarget),
                Is.EqualTo(1));
            AssertFoodBeliefStatus(world, npcId, BeliefStatus.Discarded);
        }

        [Test]
        public void EquivalentFoodRecoveryEmitsJobExplainabilityTraces()
        {
            var world = MakeWorldWithNpcAndCommunityFood(
                npcX: 5,
                npcY: 5,
                foodX: 5,
                foodY: 5,
                out int npcId,
                out int foodId,
                enableMbdExplainability: true);
            int replacementFoodId = AddCommunityFoodStock(world, 88, 5, 5, units: 3);

            AddFoodBelief(world, npcId, 5, 5);
            EnableMbdBridgeExplainability(world);
            AssignFoodJob(world, npcId, foodId, 5, 5, urgency01: 0.95f);
            world.SetFoodStock(foodId, new FoodStockComponent
            {
                Units = 0,
                OwnerKind = OwnerKind.Community,
                OwnerId = 0
            });

            var system = new JobExecutionSystem();
            system.Update(world, new Tick(0, 1f), new MessageBus(), new Telemetry());
            system.Update(world, new Tick(1, 1f), new MessageBus(), new Telemetry());

            Assert.That(world.MemoryBeliefDecisionExplainability.TryGetNpcStore(npcId, out var store), Is.True);
            Assert.That(store.TryGetLatestStepTrace(out var stepTrace), Is.True);
            Assert.That(stepTrace.Step, Is.Not.Null);
            Assert.That(stepTrace.Step.Result.DiagnosticMessage, Is.EqualTo("ConsumeFoodUnavailable"));
            Assert.That(stepTrace.Step.Reason, Does.StartWith("RecoveryTargetReplaced:ResourceMissing:FindEquivalentTarget"));

            Assert.That(store.TryGetLatestJobRequestTrace(out var requestTrace), Is.True);
            Assert.That(requestTrace.JobRequest, Is.Not.Null);
            Assert.That(requestTrace.JobRequest.TargetObjectId, Is.EqualTo(replacementFoodId));
            Assert.That(requestTrace.JobRequest.Reason, Does.StartWith("RecoveryJobRequest:EquivalentTarget"));

            Assert.That(store.TryGetLatestJobLifecycleTrace(out var lifecycleTrace), Is.True);
            Assert.That(lifecycleTrace.JobLifecycle, Is.Not.Null);
            Assert.That(lifecycleTrace.JobLifecycle.Operation, Is.EqualTo(MemoryBeliefDecisionJobLifecycleOperation.Activated));
            Assert.That(lifecycleTrace.JobLifecycle.Job.TargetObjectId, Is.EqualTo(replacementFoodId));
            Assert.That(lifecycleTrace.JobLifecycle.Reason, Does.StartWith("RecoveryJobActivated:EquivalentTarget"));
        }

        [Test]
        public void AssignedFoodJobConsumeFoodObjectMissingUpdatesFoodBelief()
        {
            var world = MakeWorldWithNpcAndCommunityFood(npcX: 5, npcY: 5, foodX: 5, foodY: 5, out int npcId, out int foodId, enableMbdExplainability: true);
            AddFoodBelief(world, npcId, 5, 5);
            EnableMbdBridgeExplainability(world);
            AssignFoodJob(world, npcId, foodId, 5, 5, urgency01: 0.95f);
            world.Objects.Remove(foodId);
            var system = new JobExecutionSystem();

            // Lo stock resta presente ma l'istanza oggetto sparisce: questo distingue
            // il failure da una semplice risorsa esaurita e copre il path
            // ConsumeFoodObjectMissing nel vero runtime di execution.
            system.Update(world, new Tick(0, 1f), new MessageBus(), new Telemetry());
            system.Update(world, new Tick(1, 1f), new MessageBus(), new Telemetry());

            Assert.That(world.JobRuntimeState.HasActiveJob(npcId), Is.False);
            AssertFoodBeliefStatus(world, npcId, BeliefStatus.Discarded);
            AssertLatestBeliefTrace(
                world,
                npcId,
                MemoryBeliefDecisionBeliefOperation.Discarded,
                "BeliefContradiction:FoodExecutionTargetFailure:ConsumeFoodObjectMissing");
            AssertLatestFailureLearning(
                world,
                npcId,
                JobFailureReason.MissingTarget,
                "OperationalFailure:FoodExecutionTargetFailure:ConsumeFoodObjectMissing");
        }

        [Test]
        public void AssignedFoodJobMoveTargetDeletedFailsJobAndUpdatesFoodBelief()
        {
            var world = MakeWorldWithNpcAndCommunityFood(npcX: 5, npcY: 5, foodX: 6, foodY: 5, out int npcId, out int foodId, enableMbdExplainability: true);
            world.NpcFacing[npcId] = CardinalDirection.East;
            AddFoodBelief(world, npcId, 6, 5);
            EnableMbdBridgeExplainability(world);
            AssignFoodJob(world, npcId, foodId, 6, 5, urgency01: 0.95f);
            world.Objects.Remove(foodId);
            world.FoodStocks.Remove(foodId);
            var system = new JobExecutionSystem();

            system.Update(world, new Tick(0, 1f), new MessageBus(), new Telemetry());

            Assert.That(world.JobRuntimeState.HasActiveJob(npcId), Is.False);
            Assert.That(world.JobRuntimeState.GetSnapshot(npcId, 0).LastFailureReason, Is.EqualTo(JobFailureReason.MissingTarget));
            AssertFoodBeliefStatus(world, npcId, BeliefStatus.Discarded);
            AssertLatestBeliefTrace(
                world,
                npcId,
                MemoryBeliefDecisionBeliefOperation.Discarded,
                "BeliefContradiction:FoodExecutionTargetFailure:MoveFoodObjectMissing");
            AssertLatestFailureLearning(
                world,
                npcId,
                JobFailureReason.MissingTarget,
                "OperationalFailure:FoodExecutionTargetFailure:MoveFoodObjectMissing");
        }

        [Test]
        public void AssignedFoodJobMoveTargetDeletedOutsideViewKeepsMovingTowardBelief()
        {
            var world = MakeWorldWithNpcAndCommunityFood(npcX: 1, npcY: 1, foodX: 5, foodY: 5, out int npcId, out int foodId, enableMbdExplainability: true);
            world.NpcFacing[npcId] = CardinalDirection.North;
            AddFoodBelief(world, npcId, 5, 5);
            AssignFoodJob(world, npcId, foodId, 5, 5, urgency01: 0.95f);
            world.Objects.Remove(foodId);
            world.FoodStocks.Remove(foodId);
            var system = new JobExecutionSystem();

            system.Update(world, new Tick(0, 1f), new MessageBus(), new Telemetry());

            Assert.That(world.JobRuntimeState.HasActiveJob(npcId), Is.True);
            AssertFoodBeliefStatus(world, npcId, BeliefStatus.Active);
        }

        [Test]
        public void ObjectPerceptionDiscardsVisibleMissingFoodBelief()
        {
            var world = MakeWorldWithNpcOnly(npcX: 5, npcY: 5, out int npcId);
            world.NpcFacing[npcId] = CardinalDirection.East;
            AddFoodBelief(world, npcId, 6, 5);
            AddRememberedWorldObject(world, npcId, objectId: 77, x: 6, y: 5, OwnerKind.Community, ownerId: 0);
            var perception = new ObjectPerceptionSystem();

            perception.Update(world, new Tick(4, 1f), new MessageBus(), new Telemetry());

            AssertFoodBeliefStatus(world, npcId, BeliefStatus.Discarded);
            Assert.That(HasRememberedObject(world, npcId, objectId: 77, defId: "food_stock_private", x: 6, y: 5), Is.False);
        }

        [Test]
        public void ObjectPerceptionSpotsFoodInNpcCurrentCell()
        {
            var world = MakeWorldWithNpcAndCommunityFood(npcX: 5, npcY: 5, foodX: 5, foodY: 5, out int npcId, out int foodId);
            world.NpcFacing[npcId] = CardinalDirection.North;
            var bus = new MessageBus();
            var perception = new ObjectPerceptionSystem();

            perception.Update(world, new Tick(6, 1f), bus, new Telemetry());

            var events = new List<ISimEvent>();
            bus.DrainTo(events);
            Assert.That(ContainsObjectSpottedEvent(events, npcId, foodId, "food_stock", 5, 5), Is.True);
        }

        [Test]
        public void ObjectPerceptionDiscardsSameCellMissingFoodBelief()
        {
            var world = MakeWorldWithNpcOnly(npcX: 5, npcY: 5, out int npcId);
            world.NpcFacing[npcId] = CardinalDirection.West;
            AddFoodBelief(world, npcId, 5, 5);
            AddRememberedWorldObject(world, npcId, objectId: 78, x: 5, y: 5, OwnerKind.Community, ownerId: 0);
            var perception = new ObjectPerceptionSystem();

            perception.Update(world, new Tick(7, 1f), new MessageBus(), new Telemetry());

            AssertFoodBeliefStatus(world, npcId, BeliefStatus.Discarded);
            Assert.That(HasRememberedObject(world, npcId, objectId: 78, defId: "food_stock_private", x: 5, y: 5), Is.False);
        }

        [Test]
        public void ObjectPerceptionDiscardsVisibleStaleMissingFoodBelief()
        {
            var world = MakeWorldWithNpcOnly(npcX: 5, npcY: 5, out int npcId);
            world.NpcFacing[npcId] = CardinalDirection.East;
            AddFoodBelief(world, npcId, 6, 5);
            world.Beliefs[npcId].TryReduceConfidenceByCategoryAndPosition(
                BeliefCategory.Food,
                new Vector2Int(6, 5),
                0.10f,
                4,
                BeliefStatus.Stale);
            AddRememberedWorldObject(world, npcId, objectId: 79, x: 6, y: 5, OwnerKind.Community, ownerId: 0);
            var perception = new ObjectPerceptionSystem();

            perception.Update(world, new Tick(8, 1f), new MessageBus(), new Telemetry());

            AssertFoodBeliefStatus(world, npcId, BeliefStatus.Discarded);
            Assert.That(HasRememberedObject(world, npcId, objectId: 79, defId: "food_stock_private", x: 6, y: 5), Is.False);
        }

        [Test]
        public void AssignedFoodJobMoveTargetEmptyCanReplaceWithVisibleEquivalentFood()
        {
            var world = MakeWorldWithNpcAndCommunityFood(npcX: 1, npcY: 1, foodX: 5, foodY: 5, out int npcId, out int foodId, enableMbdExplainability: true);
            int replacementFoodId = AddCommunityFoodStock(world, 88, 2, 1, units: 3);

            AddFoodBelief(world, npcId, 5, 5);
            AssignFoodJob(world, npcId, foodId, 5, 5, urgency01: 0.95f);
            world.SetFoodStock(foodId, new FoodStockComponent
            {
                Units = 0,
                OwnerKind = OwnerKind.Community,
                OwnerId = 0
            });
            var system = new JobExecutionSystem();

            system.Update(world, new Tick(0, 1f), new MessageBus(), new Telemetry());

            Assert.That(world.JobRuntimeState.TryGetActiveJob(npcId, out var state, out var activeJob), Is.True);
            Assert.That(activeJob.Request.TargetObjectId, Is.EqualTo(replacementFoodId));
            Assert.That(activeJob.Request.TargetCell, Is.EqualTo(new Vector2Int(2, 1)));
            Assert.That(state.GetRecoveryAlternativeTargetCount(JobStepFailureKind.ResourceMissing, 0, 0), Is.EqualTo(1));
            AssertFoodBeliefStatus(world, npcId, BeliefStatus.Discarded);
        }

        [Test]
        public void AssignedFoodJobMovementFailureDoesNotInvalidateFoodBelief()
        {
            var world = MakeWorldWithNpcAndCommunityFood(npcX: 5, npcY: 5, foodX: 5, foodY: 5, out int npcId, out int foodId);
            AddFoodBelief(world, npcId, 5, 5);
            EnableMbdBridgeExplainability(world);
            var job = CreateFoodJobWithMissingMoveTarget(npcId, foodId, 5, 5);
            string reason;
            Assert.That(world.JobRuntimeState.TryAssignJob(npcId, job, 0, out reason), Is.True, reason);
            var system = new JobExecutionSystem();

            // Il job e' un EatKnownFood valido come Request, ma il suo step di
            // movimento e' volutamente malformed. Questo verifica che failure
            // operativi di movimento/piano non vengano reinterpretati come belief
            // falsa sul cibo ricordato.
            system.Update(world, new Tick(0, 1f), new MessageBus(), new Telemetry());

            Assert.That(world.JobRuntimeState.HasActiveJob(npcId), Is.False);
            AssertFoodBeliefStatus(world, npcId, BeliefStatus.Active);
            AssertNoBeliefTrace(world, npcId);
        }

        [Test]
        public void PreemptedFoodJobDoesNotInvalidateFoodBelief()
        {
            var world = MakeWorldWithNpcAndCommunityFood(npcX: 5, npcY: 5, foodX: 5, foodY: 5, out int npcId, out int foodId);
            AddFoodBelief(world, npcId, 5, 5);
            EnableMbdBridgeExplainability(world);
            AssignFoodJob(world, npcId, foodId, 5, 5, urgency01: 0.95f);
            var emergency = CreateEmergencyPreemptJob(npcId);
            string reason;

            // La preemption e' una decisione dell'arbitro job, non una prova che il
            // target food sia falso. Il bridge execution->belief non deve quindi
            // toccare la credenza quando il job food viene sostituito.
            Assert.That(world.JobRuntimeState.TryAssignJob(npcId, emergency, 1, out reason), Is.True, reason);

            AssertFoodBeliefStatus(world, npcId, BeliefStatus.Active);
            AssertNoBeliefTrace(world, npcId);
            Assert.That(world.JobRuntimeState.GetSnapshot(npcId, 1).LastFailureReason, Is.EqualTo(JobFailureReason.None));
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
        public void JobExecutionWhenOnFoodTargetPicksUpThenConsumesFromHand()
        {
            var world = MakeWorldWithNpcAndCommunityFood(npcX: 5, npcY: 5, foodX: 5, foodY: 5, out int npcId, out int foodId);
            AssignFoodJob(world, npcId, foodId, 5, 5);
            var system = new JobExecutionSystem();
            var bus = new MessageBus();

            system.Update(world, new Tick(0, 1f), bus, new Telemetry());
            world.JobRuntimeState.CommandBuffer.Clear();
            system.Update(world, new Tick(1, 1f), bus, new Telemetry());
            world.JobRuntimeState.CommandBuffer.Clear();
            system.Update(world, new Tick(2, 1f), bus, new Telemetry());

            var commands = world.JobRuntimeState.CommandBuffer.Snapshot();
            Assert.That(commands.Length, Is.EqualTo(1));
            Assert.That(commands[0], Is.TypeOf<PickUpObjectCommand>());

            commands[0].Execute(world, bus);
            world.JobRuntimeState.CommandBuffer.Clear();
            system.Update(world, new Tick(3, 1f), bus, new Telemetry());

            commands = world.JobRuntimeState.CommandBuffer.Snapshot();
            Assert.That(commands.Length, Is.EqualTo(1));
            Assert.That(commands[0], Is.TypeOf<ConsumeInventoryItemCommand>());
            Assert.That(world.GetInventoryQuantity(npcId, "food_stock", NpcInventorySlotKind.HandLeft), Is.EqualTo(1));
        }

        [Test]
        public void JobExecutionEatCarriedFoodMovesOneStackUnitToHandThenConsumes()
        {
            var world = MakeWorldWithNpcOnly(npcX: 5, npcY: 5, out int npcId);
            AddObjectDef(world, "berry", nutritionValue: 0.32f, foodItem: true, foodStock: false);
            Assert.That(world.TryAddInventoryItem(npcId, "berry", 5, out _, out string addReason), Is.True, addReason);
            AssignCarriedFoodJob(world, npcId);
            var system = new JobExecutionSystem();
            var bus = new MessageBus();
            float hungerBefore = world.Needs[npcId].GetValue(NeedKind.Hunger);

            system.Update(world, new Tick(0, 1f), bus, new Telemetry());
            Assert.That(world.JobRuntimeState.CommandBuffer.Count, Is.EqualTo(0));
            system.Update(world, new Tick(1, 1f), bus, new Telemetry());
            var commands = world.JobRuntimeState.CommandBuffer.Snapshot();
            Assert.That(commands.Length, Is.EqualTo(1));
            Assert.That(commands[0], Is.TypeOf<MoveInventoryObjectCommand>());
            commands[0].Execute(world, bus);
            world.JobRuntimeState.CommandBuffer.Clear();

            Assert.That(world.GetInventoryQuantity(npcId, "berry", NpcInventorySlotKind.HandLeft), Is.EqualTo(1));
            Assert.That(world.GetInventoryQuantity(npcId, "berry", NpcInventorySlotKind.Pack), Is.EqualTo(4));

            system.Update(world, new Tick(2, 1f), bus, new Telemetry());
            commands = world.JobRuntimeState.CommandBuffer.Snapshot();
            Assert.That(commands.Length, Is.EqualTo(1));
            Assert.That(commands[0], Is.TypeOf<ConsumeInventoryItemCommand>());
            commands[0].Execute(world, bus);

            Assert.That(world.GetInventoryQuantity(npcId, "berry"), Is.EqualTo(4));
            Assert.That(world.GetInventoryQuantity(npcId, "berry", NpcInventorySlotKind.HandLeft), Is.EqualTo(0));
            Assert.That(world.Needs[npcId].GetValue(NeedKind.Hunger), Is.EqualTo(hungerBefore - 0.32f).Within(0.0001f));
            Assert.That(bus.TryDequeue(out var firstEvent), Is.True);
            Assert.That(firstEvent, Is.TypeOf<InventoryItemMovedEvent>());
            Assert.That(bus.TryDequeue(out var secondEvent), Is.True);
            Assert.That(secondEvent, Is.TypeOf<FoodConsumedEvent>());
        }


        [Test]
        public void ObjectFoodNutritionResolverResolvesTypedAndLegacyFoods()
        {
            var world = MakeWorldWithNpcOnly(1, 1, out _);
            AddObjectDef(world, "food_stock", nutritionValue: 0.45f, foodItem: true, foodStock: true);
            AddObjectDef(world, "berry", nutritionValue: 0.32f, foodItem: true, foodStock: false);
            AddObjectDef(world, "acorn", nutritionValue: 0.18f, foodItem: true, foodStock: false);
            AddObjectDef(world, "wood_log", nutritionValue: 0f, foodItem: false, foodStock: false);

            ObjectFoodNutritionResult stock = ObjectFoodNutritionResolver.Resolve(
                world,
                "food_stock",
                legacyFallback: 0.99f,
                allowLegacyFallbackWhenDefinitionMissing: false);
            ObjectFoodNutritionResult berry = ObjectFoodNutritionResolver.Resolve(
                world,
                "berry",
                legacyFallback: 0.99f,
                allowLegacyFallbackWhenDefinitionMissing: false);
            ObjectFoodNutritionResult acorn = ObjectFoodNutritionResolver.Resolve(
                world,
                "acorn",
                legacyFallback: 0.99f,
                allowLegacyFallbackWhenDefinitionMissing: false);
            ObjectFoodNutritionResult wood = ObjectFoodNutritionResolver.Resolve(
                world,
                "wood_log",
                legacyFallback: 0.99f,
                allowLegacyFallbackWhenDefinitionMissing: false);

            Assert.That(stock.IsConsumableFood, Is.True);
            Assert.That(stock.IsTypedFoodItem, Is.True);
            Assert.That(stock.IsLegacyFoodStock, Is.True);
            Assert.That(stock.NutritionValue, Is.EqualTo(0.45f).Within(0.0001f));
            Assert.That(stock.UsedNutritionFallback, Is.False);

            Assert.That(berry.IsConsumableFood, Is.True);
            Assert.That(berry.NutritionValue, Is.EqualTo(0.32f).Within(0.0001f));
            Assert.That(berry.IsLegacyFoodStock, Is.False);

            Assert.That(acorn.IsConsumableFood, Is.True);
            Assert.That(acorn.NutritionValue, Is.EqualTo(0.18f).Within(0.0001f));

            Assert.That(wood.IsConsumableFood, Is.False);
            Assert.That(wood.FailureReason, Is.EqualTo("ObjectDefIsNotFood"));
        }

        [Test]
        public void ObjectFoodNutritionResolverUsesFallbackOnlyForLegacyFoodStock()
        {
            var world = MakeWorldWithNpcOnly(1, 1, out _);
            AddObjectDef(world, "legacy_bad_stock", nutritionValue: 0f, foodItem: false, foodStock: true);
            AddObjectDef(world, "typed_bad_food", nutritionValue: 0f, foodItem: true, foodStock: false);

            ObjectFoodNutritionResult legacy = ObjectFoodNutritionResolver.Resolve(
                world,
                "legacy_bad_stock",
                legacyFallback: 0.77f,
                allowLegacyFallbackWhenDefinitionMissing: false);
            ObjectFoodNutritionResult typed = ObjectFoodNutritionResolver.Resolve(
                world,
                "typed_bad_food",
                legacyFallback: 0.77f,
                allowLegacyFallbackWhenDefinitionMissing: false);

            Assert.That(legacy.IsConsumableFood, Is.True);
            Assert.That(legacy.NutritionValue, Is.EqualTo(0.77f).Within(0.0001f));
            Assert.That(legacy.UsedNutritionFallback, Is.True);

            Assert.That(typed.IsConsumableFood, Is.False);
            Assert.That(typed.UsedNutritionFallback, Is.False);
            Assert.That(typed.FailureReason, Is.EqualTo("NutritionValueMissingOrInvalid"));
        }










        [Test]
        public void EatFromStockCommandPublishesFoodConsumedEventAfterMutation()
        {
            var world = MakeWorldWithNpcAndCommunityFood(5, 5, 5, 5, out int npcId, out int foodId);
            AddObjectDef(world, "food_stock", nutritionValue: 0.25f, foodItem: true, foodStock: true);
            var bus = new MessageBus();

            new EatFromStockCommand(npcId, foodId).Execute(world, bus);

            Assert.That(world.FoodStocks[foodId].Units, Is.EqualTo(2));
            Assert.That(world.Needs[npcId].GetValue(NeedKind.Hunger), Is.EqualTo(0.70f).Within(0.0001f));
            Assert.That(bus.TryDequeue(out var simEvent), Is.True);
            var consumed = simEvent as FoodConsumedEvent;
            Assert.That(consumed, Is.Not.Null);
            Assert.That(consumed.NpcId, Is.EqualTo(npcId));
            Assert.That(consumed.FoodObjectId, Is.EqualTo(foodId));
            Assert.That(consumed.SourceKind, Is.EqualTo("Stock"));
            Assert.That(consumed.FoodDefId, Is.EqualTo("food_stock"));
            Assert.That(consumed.NutritionValue, Is.EqualTo(0.25f).Within(0.0001f));
            Assert.That(consumed.UsedNutritionFallback, Is.False);
            Assert.That(consumed.RemainingUnits, Is.EqualTo(2));
            Assert.That(consumed.CellX, Is.EqualTo(5));
            Assert.That(consumed.CellY, Is.EqualTo(5));
            Assert.That(bus.Count, Is.EqualTo(0));
        }

        [Test]
        public void EatFromStockCommandDepletedStockDiscardsVisibleFoodBelief()
        {
            var world = MakeWorldWithNpcAndCommunityFood(5, 5, 5, 5, out int npcId, out int foodId);
            world.SetFoodStock(foodId, new FoodStockComponent
            {
                Units = 1,
                OwnerKind = OwnerKind.Community,
                OwnerId = 0
            });
            AddFoodBelief(world, npcId, 5, 5);
            AddRememberedWorldObject(world, npcId, foodId, 5, 5, OwnerKind.Community, ownerId: 0);

            new EatFromStockCommand(npcId, foodId).Execute(world, new MessageBus());

            Assert.That(world.Objects.ContainsKey(foodId), Is.False);
            Assert.That(world.FoodStocks.ContainsKey(foodId), Is.False);
            AssertFoodBeliefStatus(world, npcId, BeliefStatus.Discarded);
            Assert.That(HasRememberedObject(world, npcId, foodId, "food_stock_private", 5, 5), Is.False);
        }

        [Test]
        public void DestroyVisibleFoodStockDiscardsObserverFoodBelief()
        {
            var world = MakeWorldWithNpcAndCommunityFood(5, 5, 6, 5, out int npcId, out int foodId);
            AddFoodBelief(world, npcId, 6, 5);
            AddRememberedWorldObject(world, npcId, foodId, 6, 5, OwnerKind.Community, ownerId: 0);

            world.DestroyObject(foodId);

            Assert.That(world.Objects.ContainsKey(foodId), Is.False);
            Assert.That(world.FoodStocks.ContainsKey(foodId), Is.False);
            AssertFoodBeliefStatus(world, npcId, BeliefStatus.Discarded);
            Assert.That(HasRememberedObject(world, npcId, foodId, "food_stock_private", 6, 5), Is.False);
        }

        [Test]
        public void EatPrivateFoodCommandConsumesTypedInventoryAlias()
        {
            var world = MakeWorldWithNpcOnly(4, 6, out int npcId);
            AddObjectDef(world, "berry", nutritionValue: 0.20f, foodItem: true, foodStock: false);
            Assert.That(world.TryAddInventoryItem(npcId, "berry", 1, out _, out string addReason), Is.True, addReason);
            var bus = new MessageBus();

            new EatPrivateFoodCommand(npcId).Execute(world, bus);

            Assert.That(world.GetInventoryQuantity(npcId, "berry"), Is.EqualTo(0));
            Assert.That(world.Needs[npcId].GetValue(NeedKind.Hunger), Is.EqualTo(0.75f).Within(0.0001f));
            Assert.That(bus.TryDequeue(out var simEvent), Is.True);
            var consumed = simEvent as FoodConsumedEvent;
            Assert.That(consumed, Is.Not.Null);
            Assert.That(consumed.NpcId, Is.EqualTo(npcId));
            Assert.That(consumed.SourceKind, Is.EqualTo("Inventory"));
            Assert.That(consumed.FoodObjectId, Is.GreaterThan(0));
            Assert.That(consumed.FoodDefId, Is.EqualTo("berry"));
            Assert.That(consumed.NutritionValue, Is.EqualTo(0.20f).Within(0.0001f));
            Assert.That(consumed.UsedNutritionFallback, Is.False);
            Assert.That(consumed.RemainingUnits, Is.EqualTo(0));
            Assert.That(consumed.CellX, Is.EqualTo(4));
            Assert.That(consumed.CellY, Is.EqualTo(6));
            Assert.That(bus.Count, Is.EqualTo(0));
        }

        [Test]
        public void MemoryEncodingCreatesFoodConsumedTraceFromNeedsEvent()
        {
            var world = MakeWorldWithNpcAndCommunityFood(5, 5, 5, 5, out int npcId, out int foodId);
            var bus = new MessageBus();

            new EatFromStockCommand(npcId, foodId).Execute(world, bus);
            EncodeQueuedEventsIntoMemory(world, bus, tick: 12);

            Assert.That(HasMemoryTrace(world, npcId, MemoryType.FoodConsumed, npcId, foodId, 5, 5), Is.True);
        }

        [Test]
        public void SleepInBedCommandPublishesBedRestedEventAfterMutation()
        {
            var world = MakeWorldWithNpcOnly(2, 3, out int npcId);
            const int bedId = 9001;
            world.Objects[bedId] = new WorldObjectInstance
            {
                ObjectId = bedId,
                DefId = "bed",
                CellX = 2,
                CellY = 3,
                OwnerKind = OwnerKind.Community,
                OwnerId = 0
            };
            var bus = new MessageBus();

            new SleepInBedCommand(npcId, bedId, "Community").Execute(world, bus);

            var use = world.GetUseStateOrDefault(bedId);
            Assert.That(use.IsInUse, Is.True);
            Assert.That(use.UsingNpcId, Is.EqualTo(npcId));
            Assert.That(bus.TryDequeue(out var simEvent), Is.True);
            var rested = simEvent as BedRestedEvent;
            Assert.That(rested, Is.Not.Null);
            Assert.That(rested.NpcId, Is.EqualTo(npcId));
            Assert.That(rested.BedObjectId, Is.EqualTo(bedId));
            Assert.That(rested.CellX, Is.EqualTo(2));
            Assert.That(rested.CellY, Is.EqualTo(3));
            Assert.That(rested.ReasonTag, Is.EqualTo("Community"));
            Assert.That(bus.Count, Is.EqualTo(0));
        }

        [Test]
        public void MemoryEncodingCreatesBedRestedTraceFromNeedsEvent()
        {
            var world = MakeWorldWithNpcOnly(2, 3, out int npcId);
            const int bedId = 9001;
            world.Objects[bedId] = new WorldObjectInstance
            {
                ObjectId = bedId,
                DefId = "bed",
                CellX = 2,
                CellY = 3,
                OwnerKind = OwnerKind.Community,
                OwnerId = 0
            };
            var bus = new MessageBus();

            new SleepInBedCommand(npcId, bedId, "Community").Execute(world, bus);
            EncodeQueuedEventsIntoMemory(world, bus, tick: 13);

            Assert.That(HasMemoryTrace(world, npcId, MemoryType.BedRested, npcId, bedId, 2, 3), Is.True);
        }

        private static void EncodeQueuedEventsIntoMemory(World world, MessageBus bus, int tick)
        {
            var events = new List<ISimEvent>();
            bus.DrainTo(events);

            var memoryEncoding = new MemoryEncodingSystem();
            memoryEncoding.SetEventsBuffer(events);
            memoryEncoding.Update(world, new Tick(tick, 1f), new MessageBus(), new Telemetry());
        }

        private static JobTemplateRegistry MakeRegistry()
        {
            var registry = new JobTemplateRegistry();
            registry.LoadFromJson(TemplateJson);
            return registry;
        }

        private static JobRequest MakeFoodJobRequest(
            int npcId,
            int foodObjectId,
            Vector2Int targetCell,
            int tick,
            float urgency01,
            string beliefKey)
        {
            return new JobRequest(
                $"jobreq_food_{npcId}_{foodObjectId}_{tick}",
                npcId,
                DecisionIntentKind.EatKnownFood,
                urgency01 >= 0.85f ? JobPriorityClass.Critical : JobPriorityClass.Important,
                urgency01,
                tick,
                true,
                targetCell,
                foodObjectId,
                beliefKey,
                "FoodJobVerticalSlice");
        }

        private static JobRequest MakeCarriedFoodJobRequest(int npcId, int tick, float urgency01)
        {
            return JobRequest.WithoutTarget(
                $"jobreq_carried_food_{npcId}_{tick}",
                npcId,
                DecisionIntentKind.EatCarriedFood,
                urgency01 >= 0.85f ? JobPriorityClass.Critical : JobPriorityClass.Important,
                urgency01,
                tick,
                "EatCarriedFoodQa");
        }

        private static void AssertFoodBoundaryFields(JobRequest actual, JobRequest expected)
        {
            Assert.That(actual.IntentKind, Is.EqualTo(expected.IntentKind));
            Assert.That(actual.Urgency01, Is.EqualTo(expected.Urgency01).Within(0.0001f));
            Assert.That(actual.BeliefKey, Is.EqualTo(expected.BeliefKey));
            Assert.That(actual.HasTargetCell, Is.EqualTo(expected.HasTargetCell));
            Assert.That(actual.TargetCell, Is.EqualTo(expected.TargetCell));
            Assert.That(actual.TargetObjectId, Is.EqualTo(expected.TargetObjectId));
        }

        private static void EnableMbdBridgeExplainability(World world)
        {
            world.Config.Sim.memory_belief_decision_explainability.enabled = true;
            world.Config.Sim.memory_belief_decision_explainability.writeJsonLog = false;
            world.Config.Sim.memory_belief_decision_explainability.logDecision = true;
            world.Config.Sim.memory_belief_decision_explainability.logBridge = true;
            world.Config.Sim.memory_belief_decision_explainability.logJobRequest = true;
            world.Config.Sim.memory_belief_decision_explainability.logBelief = true;
            world.Config.Sim.memory_belief_decision_explainability.logFailureLearning = true;
        }

        private static void AssertLatestBeliefTrace(
            World world,
            int npcId,
            MemoryBeliefDecisionBeliefOperation expectedOperation,
            string expectedReason)
        {
            Assert.That(world.MemoryBeliefDecisionExplainability.TryGetNpcStore(npcId, out var store), Is.True);
            Assert.That(store.TryGetLatestBeliefTrace(out var trace), Is.True);
            Assert.That(trace.Belief, Is.Not.Null);
            Assert.That(trace.Belief.Operation, Is.EqualTo(expectedOperation));
            Assert.That(trace.Belief.Reason, Is.EqualTo(expectedReason));
        }

        private static void AssertLatestFailureLearning(
            World world,
            int npcId,
            JobFailureReason expectedReason,
            string expectedDiagnosticReason)
        {
            Assert.That(world.MemoryBeliefDecisionExplainability.TryGetNpcStore(npcId, out var store), Is.True);
            Assert.That(store.TryGetLatestFailureLearningTrace(out var trace), Is.True);
            Assert.That(trace.FailureLearning, Is.Not.Null);
            Assert.That(trace.FailureLearning.FailureReason, Is.EqualTo(expectedReason));
            Assert.That(trace.FailureLearning.Reason, Is.EqualTo(expectedDiagnosticReason));
        }

        private static void AssertNoBeliefTrace(World world, int npcId)
        {
            if (world.MemoryBeliefDecisionExplainability == null)
                return;

            if (!world.MemoryBeliefDecisionExplainability.TryGetNpcStore(npcId, out var store))
                return;

            Assert.That(store.TryGetLatestBeliefTrace(out _), Is.False);
        }

        private static void AssertFoodBeliefStatus(World world, int npcId, BeliefStatus expectedStatus)
        {
            Assert.That(world.Beliefs.TryGetValue(npcId, out var store), Is.True);
            Assert.That(store.Entries.Count, Is.GreaterThanOrEqualTo(1));
            Assert.That(store.Entries[0].Category, Is.EqualTo(BeliefCategory.Food));
            Assert.That(store.Entries[0].Status, Is.EqualTo(expectedStatus));
        }

        private static void AssertNoJobRequestTrace(World world, int npcId)
        {
            if (!world.MemoryBeliefDecisionExplainability.TryGetNpcStore(npcId, out var store))
                return;

            Assert.That(store.TryGetLatestJobRequestTrace(out _), Is.False);
        }

        private static void AssertNoDecisionTrace(World world, int npcId)
        {
            if (!world.MemoryBeliefDecisionExplainability.TryGetNpcStore(npcId, out var store))
                return;

            Assert.That(store.TryGetLatestDecisionTrace(out _), Is.False);
        }

        private static void PreferKnownFoodDecision(World world, int npcId)
        {
            Assert.That(world.NpcProfiles.TryGetValue(npcId, out var profile), Is.True);

            // In questo test vogliamo attraversare il boundary reale EatKnownFood
            // -> JobRequest -> FoodJobFactory. La selezione runtime normale resta
            // weighted-random top-N e puo' scegliere SearchFood anche quando il
            // candidato noto e' disponibile; qui forziamo solo lo scenario QA a
            // scegliere deterministicamente il candidato col punteggio piu' alto.
            world.Config.Sim.decision.selectionMode = "DeterministicTop1";

            // Alziamo l'affinita' agricola e azzeriamo quella esplorativa per far
            // emergere EatKnownFood come miglior candidato senza modificare la
            // pipeline decisionale di produzione o i fallback legacy.
            profile.Competence.Set(DomainKind.Agriculture, 1f);
            profile.Preference.Set(DomainKind.Agriculture, 1f);
            profile.Obligation.Set(DomainKind.Agriculture, 1f);
            profile.Competence.Set(DomainKind.Exploration, 0f);
            profile.Preference.Set(DomainKind.Exploration, 0f);
            profile.Obligation.Set(DomainKind.Exploration, 0f);
        }

        private static World MakeWorldWithNpcAndCommunityFood(int npcX, int npcY, int foodX, int foodY, out int npcId, out int foodId, bool enableMbdExplainability = false)
        {
            var world = MakeWorldWithNpcOnly(npcX, npcY, out npcId, enableMbdExplainability);
            AddObjectDef(world, "food_stock", nutritionValue: 0.45f, foodItem: true, foodStock: true);

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

            world.SetFoodStock(foodId, new FoodStockComponent
            {
                Units = 3,
                OwnerKind = OwnerKind.Community,
                OwnerId = 0
            });

            return world;
        }

        private static World MakeWorldWithNpcOnly(int npcX, int npcY, out int npcId, bool enableMbdExplainability = false)
        {
            var sim = new SimulationParams();
            sim.tick = new TickParams
            {
                ticksPerSecond = TickParams.DefaultTicksPerSecond,
                baseWalkCellDurationTicks = 3,
                enableJobRunningActionTraversal = true
            };
            if (enableMbdExplainability)
            {
                sim.memory_belief_decision_explainability.enabled = true;
                sim.memory_belief_decision_explainability.writeJsonLog = false;
            }

            var world = new World(new WorldConfig(sim));
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

        private static void AddObjectDef(
            World world,
            string defId,
            float nutritionValue,
            bool foodItem,
            bool foodStock)
        {
            var properties = new List<ObjectPropertyKV>();
            if (foodItem)
            {
                properties.Add(new ObjectPropertyKV
                {
                    Key = "FoodItem",
                    Value = 1f
                });
            }

            if (foodStock)
            {
                properties.Add(new ObjectPropertyKV
                {
                    Key = "FoodStock",
                    Value = 1f
                });
            }

            properties.Add(new ObjectPropertyKV
            {
                Key = "NutritionValue",
                Value = nutritionValue
            });

            world.ObjectDefs[defId] = new ObjectDef
            {
                Id = defId,
                DisplayName = defId,
                FootprintWidth = 1,
                FootprintHeight = 1,
                IsInteractable = true,
                WeightUnits = 1,
                BulkUnits = 1,
                Stackable = foodItem || foodStock,
                HasDurability = false,
                CanPlaceInHand = foodItem || foodStock,
                CanPlaceInContainer = foodItem || foodStock,
                Properties = properties
            };
        }

        private static int AddPrivateFoodStock(World world, int ownerNpcId, int x, int y)
        {
            int foodId = 1701;
            world.Objects[foodId] = new WorldObjectInstance
            {
                ObjectId = foodId,
                DefId = "food_stock_private",
                CellX = x,
                CellY = y,
                OwnerKind = OwnerKind.Npc,
                OwnerId = ownerNpcId
            };

            AddObjectDef(world, "food_stock_private", nutritionValue: 0.45f, foodItem: true, foodStock: true);
            world.SetFoodStock(foodId, new FoodStockComponent
            {
                Units = 3,
                OwnerKind = OwnerKind.Npc,
                OwnerId = ownerNpcId
            });

            return foodId;
        }

        private static int AddCommunityFoodStock(World world, int foodId, int x, int y, int units)
        {
            world.Objects[foodId] = new WorldObjectInstance
            {
                ObjectId = foodId,
                DefId = "food_stock",
                CellX = x,
                CellY = y,
                OwnerKind = OwnerKind.Community,
                OwnerId = 0
            };

            AddObjectDef(world, "food_stock", nutritionValue: 0.45f, foodItem: true, foodStock: true);
            world.SetFoodStock(foodId, new FoodStockComponent
            {
                Units = units,
                OwnerKind = OwnerKind.Community,
                OwnerId = 0
            });

            return foodId;
        }

        private static void AddRememberedWorldObject(World world, int npcId, int objectId, int x, int y, OwnerKind ownerKind, int ownerId)
        {
            if (!world.NpcObjectMemory.ContainsKey(npcId))
                world.NpcObjectMemory[npcId] = new NpcObjectMemoryStore(8);

            world.NpcObjectMemory[npcId].UpsertWorldObject(
                nowTick: 0,
                defId: "food_stock_private",
                objectId: objectId,
                x: x,
                y: y,
                ownerKind: ownerKind,
                ownerId: ownerId,
                reliability01: 0.95f,
                utility01: 0.95f,
                pinIfOwnedByNpc: false,
                npcIdForPinLogic: npcId);
        }

        private static bool HasRememberedObject(World world, int npcId, int objectId, string defId, int x, int y)
        {
            if (!world.NpcObjectMemory.TryGetValue(npcId, out var store) || store == null)
                return false;

            for (int i = 0; i < store.Slots.Length; i++)
            {
                var slot = store.Slots[i];
                if (!slot.IsValid)
                    continue;

                if (slot.ObjectId == objectId
                    && slot.DefId == defId
                    && slot.CellX == x
                    && slot.CellY == y)
                {
                    return true;
                }
            }

            return false;
        }

        private static bool ContainsObjectSpottedEvent(
            List<ISimEvent> events,
            int npcId,
            int objectId,
            string defId,
            int x,
            int y)
        {
            for (int i = 0; i < events.Count; i++)
            {
                if (events[i] is not ObjectSpottedEvent spotted)
                    continue;

                if (spotted.ObserverNpcId == npcId
                    && spotted.ObjectId == objectId
                    && spotted.DefId == defId
                    && spotted.CellX == x
                    && spotted.CellY == y)
                {
                    return true;
                }
            }

            return false;
        }

        private static bool HasMemoryTrace(World world, int npcId, MemoryType type, int subjectId, int secondarySubjectId, int x, int y)
        {
            if (!world.Memory.TryGetValue(npcId, out var store) || store == null)
                return false;

            var traces = store.Traces;
            for (int i = 0; i < traces.Count; i++)
            {
                var trace = traces[i];
                if (trace.Type == type
                    && trace.SubjectId == subjectId
                    && trace.SecondarySubjectId == secondarySubjectId
                    && trace.CellX == x
                    && trace.CellY == y)
                {
                    return true;
                }
            }

            return false;
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

        private static Job AssignCarriedFoodJob(World world, int npcId)
        {
            var job = CreateCarriedFoodJob(npcId, urgency01: 0.95f);
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

        private static Job CreateCarriedFoodJob(int npcId, float urgency01)
        {
            bool created = FoodJobFactory.TryCreateCarriedInventoryFoodJob(
                MakeRegistry(),
                MakeCarriedFoodJobRequest(npcId, tick: 0, urgency01: urgency01),
                out var job,
                out var reason);

            Assert.That(created, Is.True, reason);
            return job;
        }

        private static Job CreateFoodJobWithMissingMoveTarget(int npcId, int foodId, int foodX, int foodY)
        {
            var request = MakeFoodJobRequest(npcId, foodId, new Vector2Int(foodX, foodY), 0, 0.95f, "Food:1");
            var plan = new JobPlan(
                "food.malformed_move_for_qa",
                new[]
                {
                    new JobPhase(
                        "reach_food",
                        JobPhaseKind.ReachTarget,
                        "Reach food",
                        1,
                        true,
                        new[] { JobAction.Simple("move_missing_target", JobActionKind.MoveToCell, "Move missing target") }),
                });

            return new Job("job_food_missing_move_target_qa", request, plan);
        }

        private static Job CreateEmergencyPreemptJob(int npcId)
        {
            var request = JobRequest.WithoutTarget(
                "jobreq_emergency_preempt_qa",
                npcId,
                DecisionIntentKind.WaitAndObserve,
                JobPriorityClass.Emergency,
                1f,
                1,
                "EmergencyPreemptQa");
            var plan = new JobPlan(
                "emergency.preempt.qa",
                new[]
                {
                    new JobPhase(
                        "wait",
                        JobPhaseKind.Execute,
                        "Emergency wait",
                        1,
                        true,
                        new[] { JobAction.Wait("wait_once", 1, "Wait once") }),
                });

            return new Job("job_emergency_preempt_qa", request, plan);
        }
    }
}
