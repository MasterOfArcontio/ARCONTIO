using Arcontio.Core;
using Arcontio.Core.Config;
using NUnit.Framework;
using UnityEngine;

namespace Arcontio.Tests
{
    // =============================================================================
    // MemoryBeliefDecisionRuntimeUiQaTests
    // =============================================================================
    /// <summary>
    /// <para>
    /// Test QA EditMode per il registry runtime e il ViewModel UI-friendly dell'EL
    /// Memory, Belief, Query e Decision.
    /// </para>
    ///
    /// <para><b>JSONL e UI condividono la stessa trace</b></para>
    /// <para>
    /// Questi test non leggono file JSONL: costruiscono le stesse trace che il sink
    /// serializza e verificano che il registry/ViewModel le renda disponibili alla
    /// UI live. In questo modo la copertura del pannello resta allineata al formato
    /// persistente senza trasformare la UI in un parser di file.
    /// </para>
    ///
    /// <para><b>Struttura interna:</b></para>
    /// <list type="bullet">
    ///   <item><b>Registry</b>: conserva trace per famiglia e per NPC.</item>
    ///   <item><b>ViewModel</b>: espone tutti i campi chiave usati dalle tab UI.</item>
    ///   <item><b>Bridge</b>: verifica la copertura Decision -> Command legacy.</item>
    /// </list>
    /// </summary>
    public sealed class MemoryBeliefDecisionRuntimeUiQaTests
    {
        // =============================================================================
        // RegistryAndViewModelExposeJsonlFieldsForSelectedNpc
        // =============================================================================
        /// <summary>
        /// <para>
        /// Verifica che i campi prodotti come trace EL-MBQD siano leggibili dal
        /// ViewModel runtime per l'NPC selezionato.
        /// </para>
        /// </summary>
        [Test]
        public void RegistryAndViewModelExposeJsonlFieldsForSelectedNpc()
        {
            var config = MakeConfig();
            var registry = new MemoryBeliefDecisionExplainabilityRegistry();
            int npcId = 4;

            MemoryBeliefDecisionExplainabilityEmitter.TryWriteTrace(config, registry, MakeMemoryTrace(npcId));
            MemoryBeliefDecisionExplainabilityEmitter.TryWriteTrace(config, registry, MakeBeliefTrace(npcId));
            MemoryBeliefDecisionExplainabilityEmitter.TryWriteTrace(config, registry, MakeQueryTrace(npcId));
            MemoryBeliefDecisionExplainabilityEmitter.TryWriteTrace(config, registry, MakeDecisionTrace(npcId));
            MemoryBeliefDecisionExplainabilityEmitter.TryWriteTrace(config, registry, MakeBridgeTrace(npcId));

            Assert.That(registry.TryGetNpcStore(npcId, out var store), Is.True);
            Assert.That(store.MemoryTraceCount, Is.EqualTo(1));
            Assert.That(store.BeliefTraceCount, Is.EqualTo(1));
            Assert.That(store.QueryTraceCount, Is.EqualTo(1));
            Assert.That(store.DecisionTraceCount, Is.EqualTo(1));
            Assert.That(store.BridgeTraceCount, Is.EqualTo(1));

            var worldConfig = new WorldConfig(new SimulationParams());
            var world = new World(worldConfig);
            world.MemoryBeliefDecisionExplainability.AddTrace(config, MakeMemoryTrace(npcId));
            world.MemoryBeliefDecisionExplainability.AddTrace(config, MakeBeliefTrace(npcId));
            world.MemoryBeliefDecisionExplainability.AddTrace(config, MakeQueryTrace(npcId));
            world.MemoryBeliefDecisionExplainability.AddTrace(config, MakeDecisionTrace(npcId));
            world.MemoryBeliefDecisionExplainability.AddTrace(config, MakeBridgeTrace(npcId));

            var viewModel = new MemoryBeliefDecisionExplainabilityViewModel();
            bool built = MemoryBeliefDecisionExplainabilityViewModelBuilder.BuildForNpc(world, npcId, viewModel);

            Assert.That(built, Is.True);
            Assert.That(viewModel.LatestMemory.EventType, Is.EqualTo("ObjectSpottedEvent"));
            Assert.That(viewModel.LatestMemory.SubjectDefId, Is.EqualTo("food_stock_small"));
            Assert.That(viewModel.LatestMemory.SecondarySubjectId, Is.EqualTo(77));
            Assert.That(viewModel.LatestMemory.HeardKind, Is.EqualTo("Alarm"));
            Assert.That(viewModel.LatestMemory.SourceSpeakerId, Is.EqualTo(12));

            Assert.That(viewModel.LatestBeliefMutation.Operation, Is.EqualTo("Created"));
            Assert.That(viewModel.LatestBeliefMutation.SourceTraceType, Is.EqualTo("ObjectSpotted"));
            Assert.That(viewModel.LatestBeliefMutation.Reason, Is.EqualTo("ObjectBeliefAggregationRule"));
            Assert.That(viewModel.LatestBeliefMutation.Belief.SourceCount, Is.EqualTo(2));

            Assert.That(viewModel.LatestQuery.GoalType, Is.EqualTo("Food"));
            Assert.That(viewModel.LatestQuery.NpcCell, Is.EqualTo("(10, 8)"));
            Assert.That(viewModel.LatestQuery.CandidateCount, Is.EqualTo(3));
            Assert.That(viewModel.LatestQuery.UsableCandidateCount, Is.EqualTo(2));
            Assert.That(viewModel.LatestQuery.Winner.BeliefId, Is.EqualTo(8));
            Assert.That(viewModel.LatestQuery.Contributions.Count, Is.EqualTo(3));

            Assert.That(viewModel.LatestDecision.AuditValid, Is.True);
            Assert.That(viewModel.LatestDecision.SelectedIntent, Is.EqualTo("EatKnownFood"));
            Assert.That(viewModel.LatestDecision.SelectionTopN, Is.EqualTo(3));
            Assert.That(viewModel.LatestDecision.EffectiveNoise01, Is.EqualTo(0.32f).Within(0.001f));
            Assert.That(viewModel.LatestDecision.Candidates.Count, Is.EqualTo(2));
            Assert.That(viewModel.LatestDecision.Candidates[0].FilteredReason, Is.Empty);

            Assert.That(viewModel.LatestBridge.CommandName, Is.EqualTo("SetMoveIntentCommand"));
            Assert.That(viewModel.LatestBridge.TargetSource, Is.EqualTo("BeliefQuery"));
            Assert.That(viewModel.LatestBridge.LegacyFallbackUsed, Is.False);
            Assert.That(viewModel.Timeline.Count, Is.EqualTo(5));
        }

        // =============================================================================
        // DisabledConfigDoesNotPopulateRegistry
        // =============================================================================
        /// <summary>
        /// <para>
        /// Verifica che il registry runtime rispetti il master switch EL-MBQD.
        /// </para>
        /// </summary>
        [Test]
        public void DisabledConfigDoesNotPopulateRegistry()
        {
            var config = MakeConfig();
            config.enabled = false;
            config.writeJsonLog = false;
            var registry = new MemoryBeliefDecisionExplainabilityRegistry();

            MemoryBeliefDecisionExplainabilityEmitter.TryWriteTrace(config, registry, MakeMemoryTrace(4));

            Assert.That(registry.TryGetNpcStore(4, out _), Is.False);
        }

        // =============================================================================
        // JobExplainabilityTraceKindsPopulateRegistryViewModelAndTimeline
        // =============================================================================
        /// <summary>
        /// <para>
        /// Verifica che tutti i nuovi payload EL della v0.07 attraversino in modo
        /// coerente il percorso emitter -> registry -> ViewModel -> timeline.
        /// </para>
        ///
        /// <para><b>Copertura end-to-end del boundary diagnostico</b></para>
        /// <para>
        /// Il test non esegue sistemi runtime reali: costruisce trace passive per i
        /// nuovi kind Job/Phase/Step/Command e controlla che il registry bounded le
        /// conservi, che il ViewModel esponga i campi chiave e che la timeline le
        /// renda leggibili senza accedere allo stato oggettivo del mondo.
        /// </para>
        ///
        /// <para><b>Struttura interna:</b></para>
        /// <list type="bullet">
        ///   <item><b>Emitter</b>: scrive tutti i nuovi TraceKind nel registry.</item>
        ///   <item><b>World registry</b>: riceve le stesse trace per la UI runtime.</item>
        ///   <item><b>ViewModel</b>: espone latest fields specifici per ogni famiglia.</item>
        ///   <item><b>Timeline</b>: include anche i kind v0.06 senza perdere leggibilita'.</item>
        /// </list>
        /// </summary>
        [Test]
        public void JobExplainabilityTraceKindsPopulateRegistryViewModelAndTimeline()
        {
            // Arrange: config EL completa per i nuovi payload Job/Step/Command.
            var config = MakeConfig();
            int npcId = 17;
            var registry = new MemoryBeliefDecisionExplainabilityRegistry();
            var traces = new[]
            {
                MakeJobRequestTrace(npcId),
                MakeJobLifecycleTrace(npcId),
                MakeJobPhaseTrace(npcId),
                MakeStepTrace(npcId),
                MakeJobStateTrace(npcId),
                MakeJobArbitrationTrace(npcId),
                MakeReservationTrace(npcId),
                MakeCommandTrace(npcId),
                MakeFailureLearningTrace(npcId),
            };

            // Act: l'emitter popola prima un registry puro, poi lo stesso boundary
            // runtime montato dentro World per simulare la UI live.
            for (int i = 0; i < traces.Length; i++)
                MemoryBeliefDecisionExplainabilityEmitter.TryWriteTrace(config, registry, traces[i]);

            Assert.That(registry.TryGetNpcStore(npcId, out var store), Is.True);
            Assert.That(store.JobRequestTraceCount, Is.EqualTo(1));
            Assert.That(store.JobLifecycleTraceCount, Is.EqualTo(1));
            Assert.That(store.JobPhaseTraceCount, Is.EqualTo(1));
            Assert.That(store.StepTraceCount, Is.EqualTo(1));
            Assert.That(store.JobStateTraceCount, Is.EqualTo(1));
            Assert.That(store.JobArbitrationTraceCount, Is.EqualTo(1));
            Assert.That(store.ReservationTraceCount, Is.EqualTo(1));
            Assert.That(store.CommandTraceCount, Is.EqualTo(1));
            Assert.That(store.FailureLearningTraceCount, Is.EqualTo(1));

            var worldConfig = new WorldConfig(new SimulationParams());
            var world = new World(worldConfig);
            for (int i = 0; i < traces.Length; i++)
                MemoryBeliefDecisionExplainabilityEmitter.TryWriteTrace(config, world.MemoryBeliefDecisionExplainability, traces[i]);

            var viewModel = new MemoryBeliefDecisionExplainabilityViewModel();
            bool built = MemoryBeliefDecisionExplainabilityViewModelBuilder.BuildForNpc(world, npcId, viewModel);

            // Assert: i latest fields devono restare fedeli alla trace piu' recente di
            // ogni famiglia, senza ricalcoli impliciti o accessi omniscienti.
            Assert.That(built, Is.True);
            Assert.That(viewModel.JobRequestCount, Is.EqualTo(1));
            Assert.That(viewModel.JobLifecycleCount, Is.EqualTo(1));
            Assert.That(viewModel.JobPhaseCount, Is.EqualTo(1));
            Assert.That(viewModel.StepCount, Is.EqualTo(1));
            Assert.That(viewModel.JobStateCount, Is.EqualTo(1));
            Assert.That(viewModel.JobArbitrationCount, Is.EqualTo(1));
            Assert.That(viewModel.ReservationCount, Is.EqualTo(1));
            Assert.That(viewModel.CommandCount, Is.EqualTo(1));
            Assert.That(viewModel.FailureLearningCount, Is.EqualTo(1));

            Assert.That(viewModel.LatestJobRequest.RequestId, Is.EqualTo("req-job-ui"));
            Assert.That(viewModel.LatestJobRequest.Intent, Is.EqualTo("EatKnownFood"));
            Assert.That(viewModel.LatestJobRequest.LegacyBridgeStillUsed, Is.True);

            Assert.That(viewModel.LatestJobLifecycle.Operation, Is.EqualTo("Activated"));
            Assert.That(viewModel.LatestJobLifecycle.Job.JobId, Is.EqualTo("job-ui"));

            Assert.That(viewModel.LatestJobPhase.Operation, Is.EqualTo("Entered"));
            Assert.That(viewModel.LatestJobPhase.Phase.PhaseId, Is.EqualTo("phase-execute"));

            Assert.That(viewModel.LatestStep.Step.ActionId, Is.EqualTo("step-consume"));
            Assert.That(viewModel.LatestStep.Result.Status, Is.EqualTo("Succeeded"));

            Assert.That(viewModel.CurrentNpcJobState.HasActiveJob, Is.True);
            Assert.That(viewModel.CurrentNpcJobState.ActiveJobId, Is.EqualTo("job-ui"));
            Assert.That(viewModel.CurrentNpcJobState.LastFailureReason, Is.EqualTo("None"));

            Assert.That(viewModel.LatestJobArbitration.Decision, Is.EqualTo("AcceptNew"));
            Assert.That(viewModel.LatestJobArbitration.AcceptedJobId, Is.EqualTo("job-ui"));

            Assert.That(viewModel.LatestReservation.Operation, Is.EqualTo("Accepted"));
            Assert.That(viewModel.LatestReservation.TargetCell, Is.EqualTo("(12, 8)"));

            Assert.That(viewModel.LatestCommand.Operation, Is.EqualTo("Enqueued"));
            Assert.That(viewModel.LatestCommand.CommandName, Is.EqualTo("SetMoveIntentCommand"));

            Assert.That(viewModel.LatestFailureLearning.FailureReason, Is.EqualTo("ReservationDenied"));
            Assert.That(viewModel.LatestFailureLearning.TargetCell, Is.EqualTo("(12, 8)"));

            Assert.That(viewModel.Timeline.Count, Is.EqualTo(9));
            Assert.That(viewModel.Timeline.Exists(row => row.Kind == "JobRequest"), Is.True);
            Assert.That(viewModel.Timeline.Exists(row => row.Kind == "JobLifecycle"), Is.True);
            Assert.That(viewModel.Timeline.Exists(row => row.Kind == "JobPhase"), Is.True);
            Assert.That(viewModel.Timeline.Exists(row => row.Kind == "Step"), Is.True);
            Assert.That(viewModel.Timeline.Exists(row => row.Kind == "JobState"), Is.True);
            Assert.That(viewModel.Timeline.Exists(row => row.Kind == "JobArbitration"), Is.True);
            Assert.That(viewModel.Timeline.Exists(row => row.Kind == "Reservation"), Is.True);
            Assert.That(viewModel.Timeline.Exists(row => row.Kind == "Command"), Is.True);
            Assert.That(viewModel.Timeline.Exists(row => row.Kind == "FailureLearning"), Is.True);
        }

        private static MemoryBeliefDecisionExplainabilityParams MakeConfig()
        {
            return new MemoryBeliefDecisionExplainabilityParams
            {
                enabled = true,
                writeJsonLog = false,
                logMemory = true,
                logBelief = true,
                logQuery = true,
                logDecision = true,
                logBridge = true,
                logJobRequest = true,
                logJobLifecycle = true,
                logJobPhase = true,
                logStep = true,
                logJobState = true,
                logJobArbitration = true,
                logReservation = true,
                logCommand = true,
                logFailureLearning = true,
                includeCandidates = true,
                includeScoreBreakdown = true,
            };
        }

        private static MemoryBeliefDecisionTrace MakeMemoryTrace(int npcId)
        {
            return new MemoryBeliefDecisionTrace
            {
                Kind = MemoryBeliefDecisionTraceKind.Memory,
                Tick = 100,
                NpcId = npcId,
                Memory = new MemoryBeliefDecisionMemoryTraceRecord
                {
                    EventType = "ObjectSpottedEvent",
                    TraceType = MemoryType.ObjectSpotted,
                    SubjectId = 31,
                    SecondarySubjectId = 77,
                    SubjectDefId = "food_stock_small",
                    Cell = new Vector2Int(12, 8),
                    Intensity01 = 0.82f,
                    Reliability01 = 0.91f,
                    IsHeard = true,
                    HeardKind = "Alarm",
                    SourceSpeakerId = 12,
                    StoreResult = AddOrMergeResult.Reinforced,
                },
            };
        }

        private static MemoryBeliefDecisionTrace MakeBeliefTrace(int npcId)
        {
            return new MemoryBeliefDecisionTrace
            {
                Kind = MemoryBeliefDecisionTraceKind.Belief,
                Tick = 101,
                NpcId = npcId,
                Belief = new MemoryBeliefDecisionBeliefRecord
                {
                    Operation = MemoryBeliefDecisionBeliefOperation.Created,
                    HasSourceTrace = true,
                    SourceTraceType = MemoryType.ObjectSpotted,
                    Belief = MakeFoodBelief(),
                    Reason = "ObjectBeliefAggregationRule",
                },
            };
        }

        private static MemoryBeliefDecisionTrace MakeQueryTrace(int npcId)
        {
            return new MemoryBeliefDecisionTrace
            {
                Kind = MemoryBeliefDecisionTraceKind.Query,
                Tick = 102,
                NpcId = npcId,
                Query = new MemoryBeliefDecisionQueryRecord
                {
                    GoalType = BeliefCategory.Food,
                    Urgency01 = 0.67f,
                    NpcPosition = new Vector2Int(10, 8),
                    MinConfidence = 0.30f,
                    CandidateCount = 3,
                    UsableCandidateCount = 2,
                    IsEmpty = false,
                    EmptyReason = string.Empty,
                    Winner = MakeFoodBelief(),
                    FinalScore = 1.24f,
                    Contributions = new[]
                    {
                        new MemoryBeliefDecisionScoreContributionRef { Label = "ConfidenceScore", Value = 0.78f },
                        new MemoryBeliefDecisionScoreContributionRef { Label = "FreshnessScore", Value = 0.61f },
                        new MemoryBeliefDecisionScoreContributionRef { Label = "DistancePenalty", Value = -0.15f },
                    },
                },
            };
        }

        private static MemoryBeliefDecisionTrace MakeDecisionTrace(int npcId)
        {
            return new MemoryBeliefDecisionTrace
            {
                Kind = MemoryBeliefDecisionTraceKind.Decision,
                Tick = 103,
                NpcId = npcId,
                Decision = new MemoryBeliefDecisionDecisionRecord
                {
                    AuditValid = true,
                    CandidateCount = 2,
                    SelectedIntent = DecisionIntentKind.EatKnownFood,
                    SelectedScore = 2.41f,
                    SelectedIndex = 0,
                    SelectionTopN = 3,
                    SelectionNoise01 = 0.15f,
                    Impulsivity01 = 0.49f,
                    EffectiveNoise01 = 0.32f,
                    Candidates = new[]
                    {
                        new MemoryBeliefDecisionCandidateRecord
                        {
                            Intent = DecisionIntentKind.EatKnownFood,
                            Available = true,
                            Need = NeedKind.Hunger,
                            NeedUrgency01 = 0.89f,
                            IsCritical = true,
                            RequiresBeliefTarget = true,
                            BeliefResultEmpty = false,
                            Belief = MakeFoodBelief(),
                            Score = 2.41f,
                            FilteredReason = string.Empty,
                            ScoreContributions = new[]
                            {
                                new MemoryBeliefDecisionScoreContributionRef { Label = "NeedUrgency", Value = 0.89f },
                                new MemoryBeliefDecisionScoreContributionRef { Label = "MemoryConfidence", Value = 0.27f },
                            },
                        },
                        new MemoryBeliefDecisionCandidateRecord
                        {
                            Intent = DecisionIntentKind.SearchFood,
                            Available = true,
                            Need = NeedKind.Hunger,
                            NeedUrgency01 = 0.89f,
                            IsCritical = true,
                            RequiresBeliefTarget = false,
                            BeliefResultEmpty = true,
                            Score = 1.02f,
                            FilteredReason = string.Empty,
                        },
                    },
                },
            };
        }

        private static MemoryBeliefDecisionTrace MakeBridgeTrace(int npcId)
        {
            return new MemoryBeliefDecisionTrace
            {
                Kind = MemoryBeliefDecisionTraceKind.Bridge,
                Tick = 104,
                NpcId = npcId,
                Bridge = new MemoryBeliefDecisionBridgeRecord
                {
                    SelectedIntent = DecisionIntentKind.EatKnownFood,
                    CommandName = "SetMoveIntentCommand",
                    Handled = true,
                    DidMove = true,
                    DidSteal = false,
                    TargetCell = new Vector2Int(12, 8),
                    TargetSource = MemoryBeliefDecisionTargetSource.BeliefQuery,
                    LegacyFallbackUsed = false,
                    Reason = "CommandEmittedByLegacyAdapter",
                },
            };
        }

        private static MemoryBeliefDecisionTrace MakeJobRequestTrace(int npcId)
        {
            return new MemoryBeliefDecisionTrace
            {
                Kind = MemoryBeliefDecisionTraceKind.JobRequest,
                Tick = 200,
                NpcId = npcId,
                JobRequest = new MemoryBeliefDecisionJobRequestRecord
                {
                    RequestId = "req-job-ui",
                    JobId = "job-ui",
                    Intent = DecisionIntentKind.EatKnownFood,
                    PriorityClass = JobPriorityClass.Important,
                    Urgency01 = 0.91f,
                    HasTargetCell = true,
                    TargetCell = new Vector2Int(12, 8),
                    TargetObjectId = 77,
                    BeliefKey = "Food@(12,8)",
                    DebugLabel = "Eat visible meal",
                    Reason = "DecisionLayerToJobFactory",
                    LegacyBridgeStillUsed = true,
                },
            };
        }

        private static MemoryBeliefDecisionTrace MakeJobLifecycleTrace(int npcId)
        {
            return new MemoryBeliefDecisionTrace
            {
                Kind = MemoryBeliefDecisionTraceKind.JobLifecycle,
                Tick = 201,
                NpcId = npcId,
                JobLifecycle = new MemoryBeliefDecisionJobLifecycleRecord
                {
                    Operation = MemoryBeliefDecisionJobLifecycleOperation.Activated,
                    Job = MakeJobRef(JobStatus.Running, JobFailureReason.None),
                    Reason = "ArbiterAcceptedNewJob",
                },
            };
        }

        private static MemoryBeliefDecisionTrace MakeJobPhaseTrace(int npcId)
        {
            return new MemoryBeliefDecisionTrace
            {
                Kind = MemoryBeliefDecisionTraceKind.JobPhase,
                Tick = 202,
                NpcId = npcId,
                JobPhase = new MemoryBeliefDecisionJobPhaseRecord
                {
                    Operation = MemoryBeliefDecisionJobPhaseOperation.Entered,
                    Job = MakeJobRef(JobStatus.Running, JobFailureReason.None),
                    Phase = new MemoryBeliefDecisionJobPhaseRef
                    {
                        PhaseId = "phase-execute",
                        Kind = JobPhaseKind.Execute,
                        DisplayName = "Esegui",
                        PhaseIndex = 1,
                        ExpectedStepCount = 2,
                        IsInterruptible = true,
                    },
                    Reason = "StateMachineEnteredPhase",
                },
            };
        }

        private static MemoryBeliefDecisionTrace MakeStepTrace(int npcId)
        {
            return new MemoryBeliefDecisionTrace
            {
                Kind = MemoryBeliefDecisionTraceKind.Step,
                Tick = 203,
                NpcId = npcId,
                Step = new MemoryBeliefDecisionStepRecord
                {
                    Job = MakeJobRef(JobStatus.Running, JobFailureReason.None),
                    Phase = new MemoryBeliefDecisionJobPhaseRef
                    {
                        PhaseId = "phase-execute",
                        Kind = JobPhaseKind.Execute,
                        DisplayName = "Esegui",
                        PhaseIndex = 1,
                        ExpectedStepCount = 2,
                        IsInterruptible = true,
                    },
                    Step = new MemoryBeliefDecisionStepRef
                    {
                        ActionId = "step-consume",
                        Kind = JobActionKind.Consume,
                        Label = "Consuma",
                        ActionIndex = 0,
                        HasTargetCell = true,
                        TargetCell = new Vector2Int(12, 8),
                        TargetObjectId = 77,
                        DurationTicks = 2,
                        PayloadKey = "meal.consume",
                    },
                    Result = new MemoryBeliefDecisionStepResultRef
                    {
                        Status = StepResultStatus.Succeeded,
                        FailureReason = JobFailureReason.None,
                        SuggestedWaitTicks = 0,
                        DiagnosticMessage = "ActionCompleted",
                    },
                    Reason = "ExecutorReturnedSuccess",
                },
            };
        }

        private static MemoryBeliefDecisionTrace MakeJobStateTrace(int npcId)
        {
            return new MemoryBeliefDecisionTrace
            {
                Kind = MemoryBeliefDecisionTraceKind.JobState,
                Tick = 204,
                NpcId = npcId,
                JobState = new MemoryBeliefDecisionJobStateRecord
                {
                    HasActiveJob = true,
                    ActiveJobId = "job-ui",
                    ActivePhaseIndex = 1,
                    ActiveActionIndex = 0,
                    WaitUntilTick = 0,
                    SuspendedJobId = string.Empty,
                    LastFailureReason = JobFailureReason.None,
                    Reason = "SnapshotAfterStep",
                },
            };
        }

        private static MemoryBeliefDecisionTrace MakeJobArbitrationTrace(int npcId)
        {
            return new MemoryBeliefDecisionTrace
            {
                Kind = MemoryBeliefDecisionTraceKind.JobArbitration,
                Tick = 205,
                NpcId = npcId,
                JobArbitration = new MemoryBeliefDecisionJobArbitrationRecord
                {
                    CurrentJob = new MemoryBeliefDecisionJobRef(),
                    ProposedJob = MakeJobRef(JobStatus.Created, JobFailureReason.None),
                    Decision = JobArbitrationDecision.AcceptNew,
                    AcceptedJobId = "job-ui",
                    Reason = "NpcIdle",
                },
            };
        }

        private static MemoryBeliefDecisionTrace MakeReservationTrace(int npcId)
        {
            return new MemoryBeliefDecisionTrace
            {
                Kind = MemoryBeliefDecisionTraceKind.Reservation,
                Tick = 206,
                NpcId = npcId,
                Reservation = new MemoryBeliefDecisionReservationRecord
                {
                    Operation = MemoryBeliefDecisionReservationOperation.Accepted,
                    ReservationId = "res-ui",
                    JobId = "job-ui",
                    OwnerNpcId = npcId,
                    TargetKind = ReservationTargetKind.Cell,
                    TargetCell = new Vector2Int(12, 8),
                    TargetObjectId = 77,
                    CreatedTick = 206,
                    ExpiresTick = 212,
                    Reason = "CellFree",
                },
            };
        }

        private static MemoryBeliefDecisionTrace MakeCommandTrace(int npcId)
        {
            return new MemoryBeliefDecisionTrace
            {
                Kind = MemoryBeliefDecisionTraceKind.Command,
                Tick = 207,
                NpcId = npcId,
                Command = new MemoryBeliefDecisionCommandRecord
                {
                    Operation = MemoryBeliefDecisionCommandOperation.Enqueued,
                    JobId = "job-ui",
                    CommandName = "SetMoveIntentCommand",
                    QueueCount = 1,
                    Reason = "StepIssuedCommand",
                },
            };
        }

        private static MemoryBeliefDecisionTrace MakeFailureLearningTrace(int npcId)
        {
            return new MemoryBeliefDecisionTrace
            {
                Kind = MemoryBeliefDecisionTraceKind.FailureLearning,
                Tick = 208,
                NpcId = npcId,
                FailureLearning = new MemoryBeliefDecisionFailureLearningRecord
                {
                    JobId = "job-ui",
                    TargetCell = new Vector2Int(12, 8),
                    FailureReason = JobFailureReason.ReservationDenied,
                    FailureTick = 208,
                    Penalty01 = 0.45f,
                    Reason = "ReservationConflictObserved",
                },
            };
        }

        private static MemoryBeliefDecisionJobRef MakeJobRef(JobStatus status, JobFailureReason failureReason)
        {
            return new MemoryBeliefDecisionJobRef
            {
                JobId = "job-ui",
                RequestId = "req-job-ui",
                Intent = DecisionIntentKind.EatKnownFood,
                PriorityClass = JobPriorityClass.Important,
                Urgency01 = 0.91f,
                Status = status,
                FailureReason = failureReason,
                CreatedTick = 200,
                UpdatedTick = 208,
                ActivePhaseIndex = 1,
                HasTargetCell = true,
                TargetCell = new Vector2Int(12, 8),
                TargetObjectId = 77,
                DebugLabel = "Eat visible meal",
            };
        }

        private static MemoryBeliefDecisionBeliefRef MakeFoodBelief()
        {
            return new MemoryBeliefDecisionBeliefRef
            {
                Category = BeliefCategory.Food,
                Status = BeliefStatus.Active,
                Source = BeliefSource.Seen,
                BeliefId = 8,
                EstimatedPosition = new Vector2Int(12, 8),
                Confidence = 0.78f,
                Freshness = 0.61f,
                SourceCount = 2,
            };
        }
    }
}
