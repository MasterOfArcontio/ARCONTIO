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
