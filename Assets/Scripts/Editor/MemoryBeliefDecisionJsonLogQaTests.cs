using System.IO;
using Arcontio.Core;
using Arcontio.Core.Config;
using NUnit.Framework;
using UnityEngine;

namespace Arcontio.Tests
{
    // =============================================================================
    // MemoryBeliefDecisionJsonLogQaTests
    // =============================================================================
    /// <summary>
    /// <para>
    /// Test QA EditMode per il sink JSONL dell'Explainability Layer Memory, Belief,
    /// Query e Decision.
    /// </para>
    ///
    /// <para><b>Diagnostica one-way</b></para>
    /// <para>
    /// I test costruiscono snapshot passivi e chiamano soltanto il sink JSONL. Non
    /// creano un World, non eseguono decisioni e non modificano MemoryStore o
    /// BeliefStore: il file resta un export diagnostico verificabile.
    /// </para>
    ///
    /// <para><b>Struttura interna:</b></para>
    /// <list type="bullet">
    ///   <item><b>Query</b>: controlla winner e contributi leggibili.</item>
    ///   <item><b>Decision</b>: controlla selected intent, candidati e score breakdown.</item>
    ///   <item><b>Bridge</b>: controlla command legacy e sorgente target.</item>
    ///   <item><b>Memory/Belief</b>: controlla enum esportati come stringhe.</item>
    /// </list>
    /// </summary>
    public sealed class MemoryBeliefDecisionJsonLogQaTests
    {
        private const string DirectoryName = "Arcontio_EL_MBD";

        // =============================================================================
        // QueryJsonlWritesReadableWinnerAndContributions
        // =============================================================================
        /// <summary>
        /// <para>
        /// Verifica che un record query esporti goal, winner e contributions come
        /// stringhe leggibili, senza enum numerici opachi.
        /// </para>
        /// </summary>
        [Test]
        public void QueryJsonlWritesReadableWinnerAndContributions()
        {
            string fileName = "qa_el_mbd_query.jsonl";
            string path = ResetLogFile(fileName);
            var config = MakeConfig(fileName);

            var trace = new MemoryBeliefDecisionTrace
            {
                Kind = MemoryBeliefDecisionTraceKind.Query,
                Tick = 120,
                NpcId = 3,
                Query = new MemoryBeliefDecisionQueryRecord
                {
                    GoalType = BeliefCategory.Food,
                    Urgency01 = 0.91f,
                    NpcPosition = new Vector2Int(10, 8),
                    MinConfidence = 0.20f,
                    CandidateCount = 2,
                    UsableCandidateCount = 1,
                    IsEmpty = false,
                    Winner = MakeFoodBelief(),
                    FinalScore = 0.63f,
                    Contributions = new[]
                    {
                        new MemoryBeliefDecisionScoreContributionRef { Label = "ConfidenceScore", Value = 0.30f },
                        new MemoryBeliefDecisionScoreContributionRef { Label = "FreshnessScore", Value = 0.24f },
                        new MemoryBeliefDecisionScoreContributionRef { Label = "DistancePenalty", Value = -0.09f },
                    },
                },
            };

            MemoryBeliefDecisionJsonLogSink.TryWriteTrace(config, trace);
            string jsonl = File.ReadAllText(path);

            Assert.That(jsonl, Does.Contain("\"schema\":\"arcontio_el_mbd.v1\""));
            Assert.That(jsonl, Does.Contain("\"kind\":\"query\""));
            Assert.That(jsonl, Does.Contain("\"goalType\":\"Food\""));
            Assert.That(jsonl, Does.Contain("\"npcCellText\":\"(10, 8)\""));
            Assert.That(jsonl, Does.Contain("\"category\":\"Food\""));
            Assert.That(jsonl, Does.Contain("\"status\":\"Active\""));
            Assert.That(jsonl, Does.Contain("\"label\":\"ConfidenceScore\""));
            Assert.That(jsonl, Does.Not.Contain("\"GoalType\":"));
            Assert.That(jsonl, Does.Not.Contain("\"BeliefCategory\":"));
        }

        // =============================================================================
        // BeliefQueryServiceWritesQueryExplainabilityRecord
        // =============================================================================
        /// <summary>
        /// <para>
        /// Verifica l'integrazione reale tra <c>BeliefQueryService</c> e sink JSONL:
        /// una query su un <c>BeliefStore</c> soggettivo deve produrre un record EL
        /// senza leggere world state o MemoryStore.
        /// </para>
        ///
        /// <para><b>QuerySystem explainable</b></para>
        /// <para>
        /// Il test protegge il contratto introdotto nella sessione 34: il layer di
        /// query puo' raccontare goal, NPC, candidati e winner usando solo dati gia'
        /// presenti nel BeliefStore.
        /// </para>
        ///
        /// <para><b>Struttura interna:</b></para>
        /// <list type="bullet">
        ///   <item><b>BeliefStore</b>: contiene una fonte Food soggettiva.</item>
        ///   <item><b>QueryService</b>: calcola winner e breakdown degli evaluator.</item>
        ///   <item><b>JSONL</b>: conserva record query con npcId, tick, winner e contributi.</item>
        /// </list>
        /// </summary>
        [Test]
        public void BeliefQueryServiceWritesQueryExplainabilityRecord()
        {
            string fileName = "qa_el_mbd_query_service.jsonl";
            string path = ResetLogFile(fileName);
            var explainability = MakeConfig(fileName);
            var store = new BeliefStore();
            var service = new BeliefQueryService();
            var queryConfig = BeliefQueryConfig.Default();

            // Arrange: inseriamo una credenza Food gia' soggettiva. Il test non crea
            // oggetti mondo e non consulta database globali, rispettando il vincolo EL.
            store.AddOrMergeByCategoryAndPosition(
                BeliefCategory.Food,
                new Vector2Int(14, 9),
                confidence: 0.82f,
                freshness: 0.76f,
                currentTick: 77,
                BeliefSource.Seen);

            // Act: usiamo l'overload diagnostico con identificativi runtime espliciti.
            var result = service.GetBestKnownFoodSource(
                store,
                new Vector2Int(10, 8),
                0.88f,
                queryConfig,
                explainability,
                9,
                222);

            string jsonl = File.ReadAllText(path);

            // Assert: il risultato resta quello normale del QuerySystem e il file
            // contiene il payload EL necessario per analizzare la scelta.
            Assert.That(result.IsEmpty, Is.False);
            Assert.That(result.Belief.Category, Is.EqualTo(BeliefCategory.Food));
            Assert.That(jsonl, Does.Contain("\"kind\":\"query\""));
            Assert.That(jsonl, Does.Contain("\"npcId\":9"));
            Assert.That(jsonl, Does.Contain("\"tick\":222"));
            Assert.That(jsonl, Does.Contain("\"goalType\":\"Food\""));
            Assert.That(jsonl, Does.Contain("\"candidateCount\":1"));
            Assert.That(jsonl, Does.Contain("\"usableCandidateCount\":1"));
            Assert.That(jsonl, Does.Contain("\"estimatedCellText\":\"(14, 9)\""));
            Assert.That(jsonl, Does.Contain("\"label\":\"ConfidenceScore\""));
        }

        // =============================================================================
        // DecisionJsonlWritesSelectedIntentCandidatesAndScoreBreakdown
        // =============================================================================
        /// <summary>
        /// <para>
        /// Verifica che un record decision esporti intenzione selezionata, candidati
        /// e breakdown score quando la config lo richiede.
        /// </para>
        /// </summary>
        [Test]
        public void DecisionJsonlWritesSelectedIntentCandidatesAndScoreBreakdown()
        {
            string fileName = "qa_el_mbd_decision.jsonl";
            string path = ResetLogFile(fileName);
            var config = MakeConfig(fileName);

            var trace = new MemoryBeliefDecisionTrace
            {
                Kind = MemoryBeliefDecisionTraceKind.Decision,
                Tick = 121,
                NpcId = 4,
                Decision = new MemoryBeliefDecisionDecisionRecord
                {
                    AuditValid = true,
                    CandidateCount = 1,
                    SelectedIntent = DecisionIntentKind.EatKnownFood,
                    SelectedScore = 1.43f,
                    SelectedIndex = 0,
                    SelectionTopN = 3,
                    SelectionNoise01 = 0.15f,
                    Impulsivity01 = 0.5f,
                    EffectiveNoise01 = 0.325f,
                    Candidates = new[]
                    {
                        new MemoryBeliefDecisionCandidateRecord
                        {
                            Intent = DecisionIntentKind.EatKnownFood,
                            Available = true,
                            Need = NeedKind.Hunger,
                            NeedUrgency01 = 0.91f,
                            IsCritical = true,
                            RequiresBeliefTarget = true,
                            BeliefResultEmpty = false,
                            Belief = MakeFoodBelief(),
                            Score = 1.43f,
                            ScoreContributions = new[]
                            {
                                new MemoryBeliefDecisionScoreContributionRef { Label = "NeedUrgency", Value = 0.91f },
                                new MemoryBeliefDecisionScoreContributionRef { Label = "MemoryConfidence", Value = 0.26f },
                            },
                        },
                    },
                },
            };

            MemoryBeliefDecisionJsonLogSink.TryWriteTrace(config, trace);
            string jsonl = File.ReadAllText(path);

            Assert.That(jsonl, Does.Contain("\"kind\":\"decision\""));
            Assert.That(jsonl, Does.Contain("\"selectedIntent\":\"EatKnownFood\""));
            Assert.That(jsonl, Does.Contain("\"intent\":\"EatKnownFood\""));
            Assert.That(jsonl, Does.Contain("\"need\":\"Hunger\""));
            Assert.That(jsonl, Does.Contain("\"label\":\"NeedUrgency\""));
            Assert.That(jsonl, Does.Contain("\"label\":\"MemoryConfidence\""));
            Assert.That(jsonl, Does.Not.Contain("\"SelectedIntent\":"));
        }

        // =============================================================================
        // BridgeJsonlWritesCommandAndTargetSource
        // =============================================================================
        /// <summary>
        /// <para>
        /// Verifica che il bridge provvisorio esporti command, target source e flag
        /// legacy senza richiedere UI runtime.
        /// </para>
        /// </summary>
        [Test]
        public void BridgeJsonlWritesCommandAndTargetSource()
        {
            string fileName = "qa_el_mbd_bridge.jsonl";
            string path = ResetLogFile(fileName);
            var config = MakeConfig(fileName);

            var trace = new MemoryBeliefDecisionTrace
            {
                Kind = MemoryBeliefDecisionTraceKind.Bridge,
                Tick = 122,
                NpcId = 5,
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
                    Reason = "CommandEmitted",
                },
            };

            MemoryBeliefDecisionJsonLogSink.TryWriteTrace(config, trace);
            string jsonl = File.ReadAllText(path);

            Assert.That(jsonl, Does.Contain("\"kind\":\"bridge\""));
            Assert.That(jsonl, Does.Contain("\"selectedIntent\":\"EatKnownFood\""));
            Assert.That(jsonl, Does.Contain("\"commandName\":\"SetMoveIntentCommand\""));
            Assert.That(jsonl, Does.Contain("\"targetCellText\":\"(12, 8)\""));
            Assert.That(jsonl, Does.Contain("\"targetSource\":\"BeliefQuery\""));
            Assert.That(jsonl, Does.Contain("\"legacyFallbackUsed\":false"));
        }

        // =============================================================================
        // MemoryAndBeliefJsonlWriteReadableEnumStrings
        // =============================================================================
        /// <summary>
        /// <para>
        /// Verifica che memory e belief esportino tipi, operazioni e status in forma
        /// testuale.
        /// </para>
        /// </summary>
        [Test]
        public void MemoryAndBeliefJsonlWriteReadableEnumStrings()
        {
            string fileName = "qa_el_mbd_memory_belief.jsonl";
            string path = ResetLogFile(fileName);
            var config = MakeConfig(fileName);

            MemoryBeliefDecisionJsonLogSink.TryWriteTrace(config, new MemoryBeliefDecisionTrace
            {
                Kind = MemoryBeliefDecisionTraceKind.Memory,
                Tick = 123,
                NpcId = 6,
                Memory = new MemoryBeliefDecisionMemoryTraceRecord
                {
                    EventType = "ObjectSpottedEvent",
                    TraceType = MemoryType.ObjectSpotted,
                    SubjectId = 42,
                    SubjectDefId = "food_stock_small",
                    Cell = new Vector2Int(12, 8),
                    Intensity01 = 0.90f,
                    Reliability01 = 0.80f,
                    StoreResult = AddOrMergeResult.Inserted,
                },
            });

            MemoryBeliefDecisionJsonLogSink.TryWriteTrace(config, new MemoryBeliefDecisionTrace
            {
                Kind = MemoryBeliefDecisionTraceKind.Belief,
                Tick = 124,
                NpcId = 6,
                Belief = new MemoryBeliefDecisionBeliefRecord
                {
                    Operation = MemoryBeliefDecisionBeliefOperation.Created,
                    HasSourceTrace = true,
                    SourceTraceType = MemoryType.ObjectSpotted,
                    Belief = MakeFoodBelief(),
                    Reason = "ObjectDefIdClassifiedAsFood",
                },
            });

            string jsonl = File.ReadAllText(path);

            Assert.That(jsonl, Does.Contain("\"kind\":\"memory\""));
            Assert.That(jsonl, Does.Contain("\"traceType\":\"ObjectSpotted\""));
            Assert.That(jsonl, Does.Contain("\"storeResult\":\"Inserted\""));
            Assert.That(jsonl, Does.Contain("\"kind\":\"belief\""));
            Assert.That(jsonl, Does.Contain("\"operation\":\"Created\""));
            Assert.That(jsonl, Does.Contain("\"sourceTraceType\":\"ObjectSpotted\""));
            Assert.That(jsonl, Does.Contain("\"reason\":\"ObjectDefIdClassifiedAsFood\""));
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
                Confidence = 0.75f,
                Freshness = 0.80f,
                SourceCount = 1,
            };
        }

        private static MemoryBeliefDecisionExplainabilityParams MakeConfig(string fileName)
        {
            return new MemoryBeliefDecisionExplainabilityParams
            {
                enabled = true,
                defaultVerbosity = 3,
                writeJsonLog = true,
                jsonLogFileNamePattern = fileName,
                includeCandidates = true,
                includeScoreBreakdown = true,
            };
        }

        private static string ResetLogFile(string fileName)
        {
            string directory = Path.Combine(Application.persistentDataPath, DirectoryName);
            Directory.CreateDirectory(directory);

            string path = Path.Combine(directory, fileName);
            if (File.Exists(path))
                File.Delete(path);

            return path;
        }
    }
}
