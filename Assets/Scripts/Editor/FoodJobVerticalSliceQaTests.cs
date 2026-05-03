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
            bool created = FoodJobFactory.TryCreateKnownCommunityFoodJob(
                MakeRegistry(),
                npcId,
                foodId,
                new Vector2Int(foodX, foodY),
                0,
                0.95f,
                "Food:1",
                out var job,
                out var reason);

            Assert.That(created, Is.True, reason);
            Assert.That(world.JobRuntimeState.TryAssignJob(npcId, job, 0, out reason), Is.True, reason);
        }

        private static Job AssignMoveJob(World world, int npcId, int targetX, int targetY)
        {
            bool created = MoveJobFactory.TryCreateMoveToCellJob(
                MakeRegistry(),
                npcId,
                new Vector2Int(targetX, targetY),
                0,
                0.25f,
                "MoveJobQa",
                out var job,
                out var reason);

            Assert.That(created, Is.True, reason);
            Assert.That(world.JobRuntimeState.TryAssignJob(npcId, job, 0, out reason), Is.True, reason);
            return job;
        }
    }
}
