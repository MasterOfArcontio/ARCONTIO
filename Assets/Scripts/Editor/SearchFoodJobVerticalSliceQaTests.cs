using System.Collections.Generic;
using Arcontio.Core;
using Arcontio.Core.Config;
using Arcontio.Core.Diagnostics;
using NUnit.Framework;
using UnityEngine;

namespace Arcontio.Tests
{
    // =============================================================================
    // SearchFoodJobVerticalSliceQaTests
    // =============================================================================
    /// <summary>
    /// <para>
    /// Test QA EditMode per la vertical slice v0.11b.04 che rende <c>SearchFood</c>
    /// job-executable tramite una probe cell locale.
    /// </para>
    ///
    /// <para><b>SearchFood senza telepatia</b></para>
    /// <para>
    /// Questi test proteggono il contratto della patch: il job SearchFood non punta
    /// a un oggetto cibo, non cerca in <c>World.Objects</c> o <c>FoodStocks</c> e
    /// produce solo un movimento locale. La scoperta cognitiva del cibo resta fuori
    /// dalla factory e dal decision bridge, affidata ai sistemi runtime di
    /// percezione, memoria e belief nei tick successivi.
    /// </para>
    ///
    /// <para><b>Struttura interna:</b></para>
    /// <list type="bullet">
    ///   <item><b>Factory</b>: conserva il <c>JobRequest</c> pre-costruito.</item>
    ///   <item><b>Bridge</b>: SearchFood selezionato assegna un job senza command legacy.</item>
    ///   <item><b>Execution</b>: il job muove tramite <c>MoveToCell</c> e running action traversal, senza command legacy.</item>
    ///   <item><b>Fallback</b>: quando non esiste probe fisica valida resta osservabile.</item>
    /// </list>
    /// </summary>
    public sealed class SearchFoodJobVerticalSliceQaTests
    {
        private const string TemplateJson =
            "{\"templates\":[{\"templateId\":\"food.search_local_probe.v1\",\"phases\":[{\"phaseId\":\"search_food_probe\",\"kind\":\"ReachTarget\",\"isInterruptible\":true,\"actions\":[{\"actionId\":\"move_to_probe\",\"kind\":\"MoveToCell\"}]}]},{\"templateId\":\"generic.move_to_cell.v1\",\"phases\":[{\"phaseId\":\"move_to_cell\",\"kind\":\"ReachTarget\",\"isInterruptible\":true,\"actions\":[{\"actionId\":\"move_to_cell\",\"kind\":\"MoveToCell\"}]}]}]}";

        // =============================================================================
        // RunFromCommandLine
        // =============================================================================
        /// <summary>
        /// <para>
        /// Entry point diagnostico per eseguire il QA mirato SearchFood da Unity
        /// batchmode quando il runner standard non produce XML.
        /// </para>
        /// </summary>
        public static void RunFromCommandLine()
        {
            try
            {
                var tests = new SearchFoodJobVerticalSliceQaTests();
                tests.DecisionFlashIsWrittenWhenSearchFoodStartsJob();
                tests.DecisionFlashIsNotWrittenWhenSearchFoodCannotStartJob();

                Debug.Log("[SearchFoodJobVerticalSliceQaTests] PASS targeted search food tests");
                UnityEditor.EditorApplication.Exit(0);
            }
            catch (System.Exception ex)
            {
                Debug.LogError("[SearchFoodJobVerticalSliceQaTests] FAIL targeted search food tests\n" + ex);
                UnityEditor.EditorApplication.Exit(1);
            }
        }

        [Test]
        public void SearchFoodFactoryAcceptsPrebuiltJobRequest()
        {
            var registry = MakeRegistry();
            var target = new Vector2Int(6, 5);
            var request = MakeSearchFoodRequest(npcId: 1, target, tick: 2, urgency01: 0.91f);

            bool created = SearchFoodJobFactory.TryCreateSearchFoodLocalProbeJob(
                registry,
                request,
                out var job,
                out var reason);

            Assert.That(created, Is.True, reason);
            Assert.That(job, Is.Not.Null);
            Assert.That(job.Request.IntentKind, Is.EqualTo(DecisionIntentKind.SearchFood));
            Assert.That(job.Request.TargetObjectId, Is.EqualTo(0));
            Assert.That(job.Request.TargetCell, Is.EqualTo(target));
            Assert.That(job.Plan.PlanId, Is.EqualTo(JobTemplateRegistry.SearchFoodLocalProbeTemplateId));
            Assert.That(job.Plan.TryGetPhase(0, out var phase), Is.True);
            Assert.That(phase.TryGetAction(0, out var action), Is.True);
            Assert.That(action.Kind, Is.EqualTo(JobActionKind.MoveToCell));
        }

        [Test]
        public void SearchFoodSelectionStartsLocalProbeJob()
        {
            var world = MakeWorldWithHungryNpc(npcX: 5, npcY: 5, out int npcId);
            EnableMbdBridgeExplainability(world);
            var commands = new List<ICommand>();

            RunDecisionOrchestrator(world, tick: 0);

            Assert.That(commands.Count, Is.EqualTo(0));
            Assert.That(world.JobRuntimeState.TryGetActiveJob(npcId, out _, out var job), Is.True);
            Assert.That(job.Request.IntentKind, Is.EqualTo(DecisionIntentKind.SearchFood));
            Assert.That(job.Request.TargetObjectId, Is.EqualTo(0));
            Assert.That(job.Request.HasTargetCell, Is.True);
            AssertLatestJobRequest(world, npcId, DecisionIntentKind.SearchFood, job.JobId);
            AssertLatestCommittedDecision(world, npcId, DecisionIntentKind.SearchFood);
        }

        [Test]
        public void DecisionFlashIsWrittenWhenSearchFoodStartsJob()
        {
            var world = MakeWorldWithHungryNpc(npcX: 5, npcY: 5, out int npcId);

            RunDecisionOrchestrator(world, tick: 7);

            Assert.That(world.JobRuntimeState.HasActiveJob(npcId), Is.True);
            Assert.That(world.TryGetNpcDecisionFlashTick(npcId, out int flashTick), Is.True);
            Assert.That(flashTick, Is.EqualTo(7));
        }

        [Test]
        public void DecisionFlashIsNotWrittenWhenSearchFoodCannotStartJob()
        {
            var world = MakeWorldWithHungryNpc(npcX: 5, npcY: 5, out int npcId);
            OccupySearchProbeCells(world, npcId, 5, 5);
            OccupySearchExplorationCells(world, npcId, 5, 5);

            RunDecisionOrchestrator(world, tick: 7);

            Assert.That(world.JobRuntimeState.HasActiveJob(npcId), Is.False);
            Assert.That(world.TryGetNpcDecisionFlashTick(npcId, out _), Is.False);
        }

        [Test]
        public void SearchFoodWithoutVisibleFoodChoosesExplorationTargetBeyondSingleStep()
        {
            var world = MakeWorldWithHungryNpc(npcX: 5, npcY: 5, out int npcId);

            RunDecisionOrchestrator(world, tick: 0);

            Assert.That(world.JobRuntimeState.TryGetActiveJob(npcId, out _, out var job), Is.True);
            Assert.That(job.Request.IntentKind, Is.EqualTo(DecisionIntentKind.SearchFood));
            Assert.That(job.Request.TargetObjectId, Is.EqualTo(0));
            int distance = Mathf.Abs(job.Request.TargetCell.x - 5) + Mathf.Abs(job.Request.TargetCell.y - 5);
            Assert.That(distance, Is.GreaterThanOrEqualTo(3));
        }

        [Test]
        public void SearchFoodSelectionIgnoresUnroutableRestIntent()
        {
            var world = MakeWorldWithHungryNpc(npcX: 5, npcY: 5, out int npcId);
            Assert.That(world.Needs.TryGetValue(npcId, out var needs), Is.True);
            needs.SetValue(NeedKind.Rest, 1f);
            world.Needs[npcId] = needs;

            RunDecisionOrchestrator(world, tick: 0);

            Assert.That(world.JobRuntimeState.TryGetActiveJob(npcId, out _, out var job), Is.True);
            Assert.That(job.Request.IntentKind, Is.EqualTo(DecisionIntentKind.SearchFood));
        }

        [Test]
        public void SearchFoodJobStartsJobOwnedMoveToTraversal()
        {
            var world = MakeWorldWithHungryNpc(npcX: 5, npcY: 5, out int npcId);
            var commands = new List<ICommand>();
            RunDecisionOrchestrator(world, tick: 0);
            Assert.That(world.JobRuntimeState.TryGetActiveJob(npcId, out _, out var job), Is.True);
            var probe = job.Request.TargetCell;
            var start = world.GridPos[npcId];
            var system = new JobExecutionSystem();

            system.Update(world, new Tick(0, 1f), new MessageBus(), new Telemetry());

            var buffered = world.JobRuntimeState.CommandBuffer.Snapshot();
            Assert.That(buffered.Length, Is.EqualTo(0));
            Assert.That(world.JobRuntimeState.RunningActions.Count, Is.EqualTo(1));
            Assert.That(world.NpcMoveIntents.TryGetValue(npcId, out var intent) && intent.Active, Is.False);
            Assert.That(probe, Is.Not.EqualTo(new Vector2Int(start.X, start.Y)));
            Assert.That(world.GridPos[npcId].X, Is.EqualTo(start.X));
            Assert.That(world.GridPos[npcId].Y, Is.EqualTo(start.Y));
        }

        [Test]
        public void SearchFoodRunningMoveEmitsLifecycleStepStateAndCommandTrace()
        {
            var world = MakeWorldWithHungryNpc(npcX: 5, npcY: 5, out int npcId);
            EnableMbdJobLifecycleExplainability(world);
            var commands = new List<ICommand>();
            RunDecisionOrchestrator(world, tick: 0);
            Assert.That(world.JobRuntimeState.TryGetActiveJob(npcId, out _, out _), Is.True);

            var system = new JobExecutionSystem();
            system.Update(world, new Tick(1, 1f), new MessageBus(), new Telemetry());

            Assert.That(world.MemoryBeliefDecisionExplainability.TryGetNpcStore(npcId, out var store), Is.True);
            Assert.That(store.JobLifecycleTraceCount, Is.GreaterThanOrEqualTo(1));
            Assert.That(store.StepTraceCount, Is.EqualTo(1));
            Assert.That(store.JobStateTraceCount, Is.EqualTo(1));
            Assert.That(store.CommandTraceCount, Is.EqualTo(0));

            Assert.That(store.TryGetLatestJobLifecycleTrace(out var lifecycleTrace), Is.True);
            Assert.That(lifecycleTrace.JobLifecycle.Operation, Is.EqualTo(MemoryBeliefDecisionJobLifecycleOperation.Activated));

            Assert.That(store.TryGetLatestStepTrace(out var stepTrace), Is.True);
            Assert.That(stepTrace.Step.Step.Kind, Is.EqualTo(JobActionKind.MoveToCell));
            Assert.That(stepTrace.Step.Result.Status, Is.EqualTo(StepResultStatus.Running));
            Assert.That(stepTrace.Step.Result.DiagnosticMessage, Does.StartWith("MoveToCellTraversal"));

            Assert.That(store.TryGetLatestJobStateTrace(out var stateTrace), Is.True);
            Assert.That(stateTrace.JobState.HasActiveJob, Is.True);

            Assert.That(store.TryGetLatestCommandTrace(out _), Is.False);
        }

        [Test]
        public void SearchFoodViewModelSeesPhaseStepAndStateAfterProbeReached()
        {
            var world = MakeWorldWithHungryNpc(npcX: 5, npcY: 5, out int npcId);
            EnableMbdJobLifecycleExplainability(world);
            var commands = new List<ICommand>();
            RunDecisionOrchestrator(world, tick: 0);
            Assert.That(world.JobRuntimeState.TryGetActiveJob(npcId, out _, out var job), Is.True);
            var probe = job.Request.TargetCell;

            var jobSystem = new JobExecutionSystem();
            var bus = new MessageBus();
            for (int i = 0; i < 20; i++)
            {
                jobSystem.Update(world, new Tick(1 + i, 1f), bus, new Telemetry());
                if (world.GridPos[npcId].X == probe.x && world.GridPos[npcId].Y == probe.y)
                    break;
            }

            Assert.That(world.GridPos[npcId].X, Is.EqualTo(probe.x));
            Assert.That(world.GridPos[npcId].Y, Is.EqualTo(probe.y));

            jobSystem.Update(world, new Tick(30, 1f), bus, new Telemetry());

            var viewModel = new MemoryBeliefDecisionExplainabilityViewModel();
            bool built = MemoryBeliefDecisionExplainabilityViewModelBuilder.BuildForNpc(world, npcId, viewModel);

            Assert.That(built, Is.True);
            Assert.That(viewModel.LatestJobPhase.HasValue, Is.True);
            Assert.That(viewModel.LatestJobPhase.Operation, Is.EqualTo("Completed"));
            Assert.That(viewModel.LatestStep.HasValue, Is.True);
            Assert.That(viewModel.LatestStep.Step.Kind, Is.EqualTo("MoveToCell"));
            Assert.That(viewModel.LatestStep.Result.Status, Is.EqualTo("Succeeded"));
            Assert.That(viewModel.CurrentNpcJobState.HasValue, Is.True);
            Assert.That(viewModel.CurrentNpcJobState.HasActiveJob, Is.False);
            Assert.That(viewModel.LatestJobLifecycle.HasValue, Is.True);
            Assert.That(viewModel.LatestJobLifecycle.Operation, Is.EqualTo("Completed"));
        }

        [Test]
        public void SearchFoodDoesNotUseFoodObjectTarget()
        {
            var world = MakeWorldWithHungryNpc(npcX: 5, npcY: 5, out int npcId);
            world.Objects[77] = new WorldObjectInstance
            {
                ObjectId = 77,
                DefId = "food_stock",
                CellX = 9,
                CellY = 9,
                OwnerKind = OwnerKind.Community,
                OwnerId = 0
            };
            world.ObjectDefs["food_stock"] = CreateFoodStockDef();
            world.SetFoodStock(77, new FoodStockComponent
            {
                Units = 3,
                OwnerKind = OwnerKind.Community,
                OwnerId = 0
            });
            var commands = new List<ICommand>();

            RunDecisionOrchestrator(world, tick: 0);

            Assert.That(commands.Count, Is.EqualTo(0));
            Assert.That(world.JobRuntimeState.TryGetActiveJob(npcId, out _, out var job), Is.True);
            Assert.That(job.Request.IntentKind, Is.EqualTo(DecisionIntentKind.SearchFood));
            Assert.That(job.Request.TargetObjectId, Is.EqualTo(0));
            Assert.That(job.Request.TargetCell, Is.Not.EqualTo(new Vector2Int(9, 9)));
        }

        [Test]
        public void SearchFoodFallsBackWhenNoProbeCellAvailable()
        {
            var world = MakeWorldWithHungryNpc(npcX: 5, npcY: 5, out int npcId);
            EnableMbdBridgeExplainability(world);
            OccupySearchProbeCells(world, npcId, 5, 5);
            OccupySearchExplorationCells(world, npcId, 5, 5);
            var commands = new List<ICommand>();

            RunDecisionOrchestrator(world, tick: 0);

            Assert.That(commands.Count, Is.EqualTo(0));
            Assert.That(world.JobRuntimeState.HasActiveJob(npcId), Is.False);
            AssertNoJobRequest(world, npcId);
            AssertNoCommittedDecision(world, npcId);
        }

        [Test]
        public void FailedSearchFoodRouteDoesNotConsumeFullDecisionCadence()
        {
            var world = MakeWorldWithHungryNpc(npcX: 5, npcY: 5, out int npcId);
            OccupySearchProbeCells(world, npcId, 5, 5);
            OccupySearchExplorationCells(world, npcId, 5, 5);
            var orchestrator = new DecisionOrchestratorSystem(
                decisionEveryTicks: 25,
                maxSeekRangeCells: 8,
                enableFoodJobVerticalSlice: true,
                jobTemplateRegistry: MakeRegistry());

            orchestrator.Update(world, new Tick(0, 1f), new MessageBus(), new Telemetry());
            Assert.That(world.JobRuntimeState.HasActiveJob(npcId), Is.False);

            world.SetNpcPos(npcId, 10, 10);
            orchestrator.Update(world, new Tick(1, 1f), new MessageBus(), new Telemetry());

            Assert.That(world.JobRuntimeState.TryGetActiveJob(npcId, out _, out var job), Is.True);
            Assert.That(job.Request.IntentKind, Is.EqualTo(DecisionIntentKind.SearchFood));
        }

        [Test]
        public void SearchFoodLocalProbeCanLeadToFoodBeliefAndEatKnownFood()
        {
            var world = MakeWorldWithHungryNpc(npcX: 5, npcY: 5, out int npcId);
            RegisterInteractableFoodStock(world, objectId: 501, x: 7, y: 5);
            world.NpcFacing[npcId] = CardinalDirection.East;
            var commands = new List<ICommand>();

            RunDecisionOrchestrator(world, tick: 0);

            Assert.That(commands.Count, Is.EqualTo(0));
            Assert.That(world.JobRuntimeState.TryGetActiveJob(npcId, out _, out var searchJob), Is.True);
            Assert.That(searchJob.Request.IntentKind, Is.EqualTo(DecisionIntentKind.SearchFood));
            Assert.That(searchJob.Request.TargetObjectId, Is.EqualTo(0));
            Assert.That(searchJob.Request.TargetCell, Is.EqualTo(new Vector2Int(6, 5)));

            var spottedEvents = RunSearchFoodExecutionPerceptionMemoryPipeline(world, npcId, tick: 1);

            Assert.That(ContainsObjectSpotted(spottedEvents, npcId, objectId: 501), Is.True);
            Assert.That(HasRememberedObject(world, npcId, objectId: 501, defId: "food_stock", x: 7, y: 5), Is.True);
            Assert.That(HasMemoryTrace(world, npcId, MemoryType.ObjectSpotted, subjectId: 501, defId: "food_stock", x: 7, y: 5), Is.True);
            Assert.That(HasFoodBelief(world, npcId, new Vector2Int(7, 5)), Is.True);

            var queryService = new BeliefQueryService();
            var queryResult = queryService.GetBestKnownFoodSource(
                world.Beliefs[npcId],
                new Vector2Int(6, 5),
                0.95f,
                world.Global.BeliefQuery);

            Assert.That(queryResult.IsEmpty, Is.False);
            Assert.That(queryResult.Belief.Category, Is.EqualTo(BeliefCategory.Food));
            Assert.That(SelectNextDecision(world, npcId, tick: 2).Kind, Is.EqualTo(DecisionIntentKind.EatKnownFood));
        }

        [Test]
        public void SearchFoodProbeDoesNotCreateFoodBeliefWithoutPerception()
        {
            var world = MakeWorldWithHungryNpc(npcX: 5, npcY: 5, out int npcId);
            world.NpcFacing[npcId] = CardinalDirection.East;
            var commands = new List<ICommand>();

            RunDecisionOrchestrator(world, tick: 0);

            Assert.That(commands.Count, Is.EqualTo(0));
            Assert.That(world.JobRuntimeState.TryGetActiveJob(npcId, out _, out var searchJob), Is.True);
            Assert.That(searchJob.Request.IntentKind, Is.EqualTo(DecisionIntentKind.SearchFood));

            var spottedEvents = RunSearchFoodExecutionPerceptionMemoryPipeline(world, npcId, tick: 1);

            Assert.That(spottedEvents.Count, Is.EqualTo(0));
            Assert.That(world.GridPos[npcId].X, Is.EqualTo(searchJob.Request.TargetCell.x));
            Assert.That(world.GridPos[npcId].Y, Is.EqualTo(searchJob.Request.TargetCell.y));
            Assert.That(HasAnyFoodBelief(world, npcId), Is.False);
            Assert.That(SelectNextDecision(world, npcId, tick: 2).Kind, Is.EqualTo(DecisionIntentKind.SearchFood));
        }

        private static JobTemplateRegistry MakeRegistry()
        {
            var registry = new JobTemplateRegistry();
            registry.LoadFromJson(TemplateJson);
            return registry;
        }

        private static void RunDecisionOrchestrator(World world, int tick)
        {
            var orchestrator = new DecisionOrchestratorSystem(
                decisionEveryTicks: 1,
                maxSeekRangeCells: 8,
                enableFoodJobVerticalSlice: true,
                jobTemplateRegistry: MakeRegistry());

            orchestrator.Update(world, new Tick(tick, 1f), new MessageBus(), new Telemetry());
        }

        private static JobRequest MakeSearchFoodRequest(int npcId, Vector2Int targetCell, int tick, float urgency01)
        {
            return new JobRequest(
                $"jobreq_search_food_probe_{npcId}_{targetCell.x}_{targetCell.y}_{tick}",
                npcId,
                DecisionIntentKind.SearchFood,
                JobPriorityClass.Important,
                urgency01,
                tick,
                true,
                targetCell,
                0,
                string.Empty,
                "SearchFoodLocalProbe");
        }

        private static World MakeWorldWithHungryNpc(int npcX, int npcY, out int npcId)
        {
            var world = new World(new WorldConfig(new SimulationParams()));
            world.Global.Needs = NeedsConfig.Default();
            world.Global.BeliefQuery = BeliefQueryConfig.Default();
            world.Global.NpcOperationalRangeCells = 16;
            world.Global.NpcVisionRangeCells = 16;
            world.Global.NpcVisionUseCone = false;
            world.Config.Sim.decision.selectionMode = "DeterministicTop1";
            world.Config.Sim.tick = new TickParams
            {
                ticksPerSecond = TickParams.DefaultTicksPerSecond,
                baseWalkCellDurationTicks = 3,
                enableJobRunningActionTraversal = true
            };

            npcId = world.CreateNpc(
                NpcDnaProfile.CreateDefault("search_food_job_qa"),
                NpcNeeds.Make(0.95f, 0.1f),
                new Arcontio.Core.Social { JusticePerception01 = 0.9f },
                npcX,
                npcY);

            Assert.That(world.NpcProfiles.TryGetValue(npcId, out var profile), Is.True);
            profile.Competence.Set(DomainKind.Exploration, 1f);
            profile.Preference.Set(DomainKind.Exploration, 1f);
            profile.Obligation.Set(DomainKind.Exploration, 1f);
            profile.Competence.Set(DomainKind.Agriculture, 1f);
            profile.Preference.Set(DomainKind.Agriculture, 1f);
            profile.Obligation.Set(DomainKind.Agriculture, 1f);

            return world;
        }

        private static List<ISimEvent> RunSearchFoodExecutionPerceptionMemoryPipeline(World world, int npcId, int tick)
        {
            TickContext.BeginTick(tick);
            world.Global.CurrentTickIndex = tick;

            var bus = new MessageBus();
            var telemetry = new Telemetry();
            var jobExecution = new JobExecutionSystem();
            var perception = new ObjectPerceptionSystem();
            var memoryEncoding = new MemoryEncodingSystem();
            var eventBuffer = new List<ISimEvent>();

            // Il test riproduce la catena runtime minima senza chiamare
            // SimulationHost: il JobExecutionSystem possiede direttamente MoveTo
            // e running action traversal; poi Perception e MemoryEncoding trasformano
            // solo eventi realmente osservati in memoria e belief.
            Assert.That(world.JobRuntimeState.TryGetActiveJob(npcId, out _, out var job), Is.True);
            var probe = job.Request.TargetCell;
            for (int i = 0; i < 20; i++)
            {
                jobExecution.Update(world, new Tick(tick + i, 1f), bus, telemetry);
                if (world.GridPos.TryGetValue(npcId, out var pos)
                    && pos.X == probe.x
                    && pos.Y == probe.y)
                {
                    break;
                }
            }

            Assert.That(world.GridPos[npcId].X, Is.EqualTo(probe.x));
            Assert.That(world.GridPos[npcId].Y, Is.EqualTo(probe.y));

            perception.Update(world, new Tick(tick + 5, 1f), bus, telemetry);
            bus.DrainTo(eventBuffer);
            memoryEncoding.SetEventsBuffer(eventBuffer);
            memoryEncoding.Update(world, new Tick(tick + 5, 1f), bus, telemetry);

            return eventBuffer;
        }

        private static void FlushJobCommandBuffer(World world, MessageBus bus)
        {
            var snapshot = world.JobRuntimeState.CommandBuffer.Snapshot();
            for (int i = 0; i < snapshot.Length; i++)
                snapshot[i].Execute(world, bus);

            world.JobRuntimeState.CommandBuffer.Clear();
        }

        private static DecisionCandidate SelectNextDecision(World world, int npcId, int tick)
        {
            Assert.That(world.GridPos.TryGetValue(npcId, out var pos), Is.True);
            Assert.That(world.Needs.TryGetValue(npcId, out var needs), Is.True);
            Assert.That(world.NpcDna.TryGetValue(npcId, out var dna), Is.True);
            Assert.That(world.NpcProfiles.TryGetValue(npcId, out var profile), Is.True);
            Assert.That(world.Beliefs.TryGetValue(npcId, out var beliefs), Is.True);

            var context = new DecisionEvaluationContext(
                npcId: npcId,
                tick: tick,
                needs: needs,
                dna: dna,
                profile: profile,
                npcPosition: new Vector2Int(pos.X, pos.Y),
                beliefs: beliefs,
                beliefQueryConfig: world.Global.BeliefQuery,
                explainabilityConfig: null,
                scheduleFrame: new DecisionScheduleFrame(false, DomainKind.None, true),
                normContext: new DecisionNormContext(true, 0.50f, true));

            var candidates = new List<DecisionCandidate>();
            var generator = new DecisionCandidateGenerator();
            var scoring = new DecisionScoringService();
            var selection = new DecisionSelectionService();

            generator.GeneratePhase1Candidates(context, candidates);
            scoring.ScoreCandidates(context, candidates, DecisionScoringConfig.Default());
            var selected = selection.Select(context, candidates, TopOneSelection(), new System.Random(11));

            Assert.That(selected.IsEmpty, Is.False);
            return selected.Candidate;
        }

        private static DecisionSelectionConfig TopOneSelection()
        {
            return new DecisionSelectionConfig
            {
                topN = 1,
                noise01 = 0f,
                impulsivityNoiseBonus = 0f,
                minimumWeight = 0.001f
            };
        }

        private static void RegisterInteractableFoodStock(World world, int objectId, int x, int y)
        {
            world.ObjectDefs["food_stock"] = CreateFoodStockDef();

            world.Objects[objectId] = new WorldObjectInstance
            {
                ObjectId = objectId,
                DefId = "food_stock",
                CellX = x,
                CellY = y,
                OwnerKind = OwnerKind.Community,
                OwnerId = 0
            };

            world.SetFoodStock(objectId, new FoodStockComponent
            {
                Units = 3,
                OwnerKind = OwnerKind.Community,
                OwnerId = 0
            });
        }

        private static ObjectDef CreateFoodStockDef()
        {
            return new ObjectDef
            {
                Id = "food_stock",
                DisplayName = "Food stock",
                IsInteractable = true,
                IsOccluder = false,
                BlocksMovement = false,
                BlocksVision = false,
                WeightUnits = 1,
                BulkUnits = 1,
                Stackable = true,
                HasDurability = false,
                CanPlaceInHand = true,
                CanPlaceInContainer = true,
                Properties = new List<ObjectPropertyKV>
                {
                    new ObjectPropertyKV { Key = "FoodItem", Value = 1f },
                    new ObjectPropertyKV { Key = "FoodStock", Value = 1f },
                    new ObjectPropertyKV { Key = "NutritionValue", Value = 0.45f }
                }
            };
        }

        private static bool ContainsObjectSpotted(List<ISimEvent> events, int npcId, int objectId)
        {
            for (int i = 0; i < events.Count; i++)
            {
                if (events[i] is ObjectSpottedEvent spotted
                    && spotted.ObserverNpcId == npcId
                    && spotted.ObjectId == objectId)
                {
                    return true;
                }
            }

            return false;
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

        private static bool HasMemoryTrace(World world, int npcId, MemoryType type, int subjectId, string defId, int x, int y)
        {
            if (!world.Memory.TryGetValue(npcId, out var store) || store == null)
                return false;

            var traces = store.Traces;
            for (int i = 0; i < traces.Count; i++)
            {
                var trace = traces[i];
                if (trace.Type == type
                    && trace.SubjectId == subjectId
                    && trace.SubjectDefId == defId
                    && trace.CellX == x
                    && trace.CellY == y)
                {
                    return true;
                }
            }

            return false;
        }

        private static bool HasFoodBelief(World world, int npcId, Vector2Int estimatedPosition)
        {
            if (!world.Beliefs.TryGetValue(npcId, out var store) || store == null)
                return false;

            var entries = store.Entries;
            for (int i = 0; i < entries.Count; i++)
            {
                var entry = entries[i];
                if (entry.Category == BeliefCategory.Food
                    && entry.EstimatedPosition == estimatedPosition
                    && entry.Status != BeliefStatus.Discarded)
                {
                    return true;
                }
            }

            return false;
        }

        private static bool HasAnyFoodBelief(World world, int npcId)
        {
            if (!world.Beliefs.TryGetValue(npcId, out var store) || store == null)
                return false;

            var entries = store.Entries;
            for (int i = 0; i < entries.Count; i++)
            {
                if (entries[i].Category == BeliefCategory.Food
                    && entries[i].Status != BeliefStatus.Discarded)
                {
                    return true;
                }
            }

            return false;
        }

        private static void EnableMbdBridgeExplainability(World world)
        {
            world.Config.Sim.memory_belief_decision_explainability.enabled = true;
            world.Config.Sim.memory_belief_decision_explainability.writeJsonLog = false;
            world.Config.Sim.memory_belief_decision_explainability.logDecision = true;
            world.Config.Sim.memory_belief_decision_explainability.logBridge = true;
            world.Config.Sim.memory_belief_decision_explainability.logJobRequest = true;
        }

        private static void EnableMbdJobLifecycleExplainability(World world)
        {
            EnableMbdBridgeExplainability(world);
            world.Config.Sim.memory_belief_decision_explainability.logJobLifecycle = true;
            world.Config.Sim.memory_belief_decision_explainability.logJobPhase = true;
            world.Config.Sim.memory_belief_decision_explainability.logStep = true;
            world.Config.Sim.memory_belief_decision_explainability.logJobState = true;
            world.Config.Sim.memory_belief_decision_explainability.logCommand = true;
        }

        private static void AssertLatestJobRequest(World world, int npcId, DecisionIntentKind expectedIntent, string expectedJobId)
        {
            Assert.That(world.MemoryBeliefDecisionExplainability.TryGetNpcStore(npcId, out var store), Is.True);
            Assert.That(store.TryGetLatestJobRequestTrace(out var trace), Is.True);
            Assert.That(trace.Kind, Is.EqualTo(MemoryBeliefDecisionTraceKind.JobRequest));
            Assert.That(trace.JobRequest.Intent, Is.EqualTo(expectedIntent));
            Assert.That(trace.JobRequest.JobId, Is.EqualTo(expectedJobId));
            Assert.That(trace.JobRequest.LegacyBridgeStillUsed, Is.False);
        }

        private static void AssertNoJobRequest(World world, int npcId)
        {
            if (!world.MemoryBeliefDecisionExplainability.TryGetNpcStore(npcId, out var store))
                return;

            Assert.That(store.TryGetLatestJobRequestTrace(out _), Is.False);
        }

        private static void AssertLatestCommittedDecision(World world, int npcId, DecisionIntentKind expectedIntent)
        {
            Assert.That(world.MemoryBeliefDecisionExplainability.TryGetNpcStore(npcId, out var store), Is.True);
            Assert.That(store.TryGetLatestDecisionTrace(out var trace), Is.True);
            Assert.That(trace.Kind, Is.EqualTo(MemoryBeliefDecisionTraceKind.Decision));
            Assert.That(trace.Decision.SelectedIntent, Is.EqualTo(expectedIntent));
        }

        private static void AssertNoCommittedDecision(World world, int npcId)
        {
            if (!world.MemoryBeliefDecisionExplainability.TryGetNpcStore(npcId, out var store))
                return;

            Assert.That(store.TryGetLatestDecisionTrace(out _), Is.False);
        }

        private static void OccupySearchProbeCells(World world, int excludedNpcId, int originX, int originY)
        {
            var offsets = new[]
            {
                new Vector2Int(1, 0),
                new Vector2Int(0, 1),
                new Vector2Int(-1, 0),
                new Vector2Int(0, -1),
                new Vector2Int(2, 0),
                new Vector2Int(0, 2),
                new Vector2Int(-2, 0),
                new Vector2Int(0, -2),
                new Vector2Int(1, 1),
                new Vector2Int(-1, 1),
                new Vector2Int(-1, -1),
                new Vector2Int(1, -1),
            };

            for (int i = 0; i < offsets.Length; i++)
            {
                var cell = new Vector2Int(originX + offsets[i].x, originY + offsets[i].y);
                if (!world.InBounds(cell.x, cell.y))
                    continue;

                int blockerNpcId = world.CreateNpc(
                    NpcDnaProfile.CreateDefault("search_food_probe_blocker"),
                    NpcNeeds.Make(0.1f, 0.1f),
                    new Arcontio.Core.Social { JusticePerception01 = 0.9f },
                    cell.x,
                    cell.y);

                world.Needs[blockerNpcId] = NpcNeeds.Make(0.1f, 0.1f);
                Assert.That(blockerNpcId, Is.Not.EqualTo(excludedNpcId));
            }
        }

        private static void OccupySearchExplorationCells(World world, int excludedNpcId, int originX, int originY)
        {
            var directions = new[]
            {
                new Vector2Int(1, 0),
                new Vector2Int(0, 1),
                new Vector2Int(-1, 0),
                new Vector2Int(0, -1),
                new Vector2Int(1, 1),
                new Vector2Int(-1, 1),
                new Vector2Int(-1, -1),
                new Vector2Int(1, -1),
            };

            for (int distance = 3; distance <= 8; distance++)
            {
                for (int i = 0; i < directions.Length; i++)
                {
                    var cell = new Vector2Int(originX + directions[i].x * distance, originY + directions[i].y * distance);
                    if (!world.InBounds(cell.x, cell.y))
                        continue;

                    if (world.TryGetNpcAt(cell.x, cell.y, out _))
                        continue;

                    int blockerNpcId = world.CreateNpc(
                        NpcDnaProfile.CreateDefault("search_food_exploration_blocker"),
                        NpcNeeds.Make(0.1f, 0.1f),
                        new Arcontio.Core.Social { JusticePerception01 = 0.9f },
                        cell.x,
                        cell.y);

                    world.Needs[blockerNpcId] = NpcNeeds.Make(0.1f, 0.1f);
                    Assert.That(blockerNpcId, Is.Not.EqualTo(excludedNpcId));
                }
            }
        }
    }
}
