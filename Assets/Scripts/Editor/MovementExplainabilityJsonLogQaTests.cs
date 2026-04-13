using System.IO;
using Arcontio.Core;
using Arcontio.Core.Config;
using NUnit.Framework;
using UnityEngine;

namespace Arcontio.Tests
{
    // =============================================================================
    // MovementExplainabilityJsonLogQaTests
    // =============================================================================
    /// <summary>
    /// <para>
    /// Test QA EditMode per l'export JSONL dell'Explainability Layer pathfinding.
    /// Verifica che il log sia leggibile da un umano e non esponga enum Unity/C# come
    /// codici numerici opachi.
    /// </para>
    ///
    /// <para><b>Separazione diagnostica / simulazione</b></para>
    /// <para>
    /// I test costruiscono trace passive e chiamano soltanto il sink JSONL. Non creano
    /// un <c>World</c>, non eseguono pathfinding e non modificano store simulativi:
    /// il file resta un export diagnostico one-way.
    /// </para>
    ///
    /// <para><b>Struttura interna:</b></para>
    /// <list type="bullet">
    ///   <item><b>Plan test</b>: controlla mode, why, invalid reason e costo leggibile.</item>
    ///   <item><b>Intent test</b>: controlla purpose, target e categoria belief come stringhe.</item>
    ///   <item><b>Event test</b>: controlla event type, mode runtime e failure type leggibili.</item>
    /// </list>
    /// </summary>
    public sealed class MovementExplainabilityJsonLogQaTests
    {
        private const string DirectoryName = "Arcontio_EL_Pathfinding";

        // =============================================================================
        // PlanJsonlWritesReadableEnumStringsAndCostText
        // =============================================================================
        /// <summary>
        /// <para>
        /// Verifica che una trace plan venga esportata con enum testuali per mode,
        /// why e candidati, e con un campo <c>costText</c> gia' leggibile.
        /// </para>
        /// </summary>
        [Test]
        public void PlanJsonlWritesReadableEnumStringsAndCostText()
        {
            // Arrange: usiamo un file stabile e lo puliamo prima del test per rendere
            // l'append JSONL deterministico anche quando il test viene rilanciato.
            string fileName = "qa_el_plan_strings.jsonl";
            string path = ResetLogFile(fileName);
            var config = MakeConfig(fileName);
            var trace = new PathPlanTrace
            {
                NpcId = 7,
                Tick = 123,
                IntentId = 10,
                PlanId = 20,
                StartCell = new Vector2Int(1, 2),
                GoalCell = new Vector2Int(9, 8),
                SelectedMode = PlannerMode.LandmarkAstar,
                SelectionReason = SelectionReason.DirectInvalidLmChosen,
                MacroRouteNodes = new[] { 3, 4, 5 },
                MacroRouteCost = 2f,
                HasLocalRouteFirstStep = false,
                VerbosityLevel = 2,
            };
            trace.Candidates.Add(new PlannerCandidate
            {
                Mode = PlannerMode.Direct,
                Valid = false,
                EstimatedCost = -1f,
                InvalidReason = InvalidReason.PathBlocked,
                Note = "direct_not_selected",
            });

            // Act: il sink scrive una singola riga JSONL, senza passare dal movimento.
            MovementExplainabilityJsonLogSink.TryWritePlan(config, trace);
            string jsonl = File.ReadAllText(path);

            // Assert: il log deve essere leggibile senza mappare manualmente enum int.
            Assert.That(jsonl, Does.Contain("\"kind\":\"plan\""));
            Assert.That(jsonl, Does.Contain("\"selectedMode\":\"LandmarkAstar\""));
            Assert.That(jsonl, Does.Contain("\"selectionReason\":\"DirectInvalidLmChosen\""));
            Assert.That(jsonl, Does.Contain("\"mode\":\"Direct\""));
            Assert.That(jsonl, Does.Contain("\"invalidReason\":\"PathBlocked\""));
            Assert.That(jsonl, Does.Contain("\"costText\":\"costo n/d\""));
            Assert.That(jsonl, Does.Not.Contain("\"SelectedMode\":"));
            Assert.That(jsonl, Does.Not.Contain("\"SelectionReason\":"));
        }

        // =============================================================================
        // IntentJsonlWritesReadablePurposeTargetAndBelief
        // =============================================================================
        /// <summary>
        /// <para>
        /// Verifica che una intent trace esporti purpose, target type e categoria
        /// belief in forma testuale.
        /// </para>
        /// </summary>
        [Test]
        public void IntentJsonlWritesReadablePurposeTargetAndBelief()
        {
            // Arrange: la belief e' uno snapshot gia' presente nella trace, non viene
            // cercata nel BeliefStore dal sink.
            string fileName = "qa_el_intent_strings.jsonl";
            string path = ResetLogFile(fileName);
            var config = MakeConfig(fileName);
            var trace = new MovementIntentTrace
            {
                NpcId = 3,
                Tick = 456,
                IntentId = 11,
                MovementPurpose = MovementPurpose.ReachFood,
                TargetType = MovementTargetType.WorldObject,
                TargetCell = new Vector2Int(4, 5),
                TargetObjectId = 99,
                HasBeliefBasis = true,
                BeliefBasis = new BeliefEntryRef
                {
                    Category = BeliefCategory.Food,
                    BeliefId = 8,
                    EntityId = 99,
                    Confidence = 0.75f,
                    Freshness = 0.80f,
                    AgeTicks = 6,
                },
                Urgency = 0.9f,
                VerbosityLevel = 2,
            };

            // Act: scriviamo la riga intent nel file JSONL di test.
            MovementExplainabilityJsonLogSink.TryWriteIntent(config, trace);
            string jsonl = File.ReadAllText(path);

            // Assert: purpose, target e belief devono essere stringhe autoesplicative.
            Assert.That(jsonl, Does.Contain("\"kind\":\"intent\""));
            Assert.That(jsonl, Does.Contain("\"movementPurpose\":\"ReachFood\""));
            Assert.That(jsonl, Does.Contain("\"targetType\":\"WorldObject\""));
            Assert.That(jsonl, Does.Contain("\"targetCellText\":\"(4, 5)\""));
            Assert.That(jsonl, Does.Contain("\"category\":\"Food\""));
            Assert.That(jsonl, Does.Not.Contain("\"MovementPurpose\":"));
            Assert.That(jsonl, Does.Not.Contain("\"TargetType\":"));
        }

        // =============================================================================
        // EventJsonlWritesReadableEventModeAndFailure
        // =============================================================================
        /// <summary>
        /// <para>
        /// Verifica che un evento runtime esporti tipo evento, active mode e failure
        /// type in forma testuale.
        /// </para>
        /// </summary>
        [Test]
        public void EventJsonlWritesReadableEventModeAndFailure()
        {
            // Arrange: l'evento rappresenta un fallimento gia' osservato dall'emitter.
            // Il sink deve copiarlo, non interpretarlo per produrre comportamento.
            string fileName = "qa_el_event_strings.jsonl";
            string path = ResetLogFile(fileName);
            var config = MakeConfig(fileName);
            var evt = new PathExecutionEvent
            {
                NpcId = 5,
                Tick = 789,
                IntentId = 12,
                PlanId = 34,
                EventType = PathEventType.Failed,
                ActiveMode = "GOAL_LOCAL_SEARCH",
                CurrentCell = new Vector2Int(6, 7),
                TargetCell = new Vector2Int(8, 9),
                HasFailureDetail = true,
                FailureDetail = new FailureDetail
                {
                    FailureType = FailureType.StuckTimeout,
                    HasBlockingCell = true,
                    BlockingCell = new Vector2Int(7, 7),
                    BlockedTicks = 12,
                    BackOffStage = 2,
                    LastActiveMode = "GOAL_LOCAL_SEARCH",
                    OscillationFlag = true,
                },
                VerbosityLevel = 2,
                Summary = "qa_failure",
            };

            // Act: scriviamo la riga evento nel file JSONL di test.
            MovementExplainabilityJsonLogSink.TryWriteExecutionEvent(config, evt);
            string jsonl = File.ReadAllText(path);

            // Assert: event type e failure type devono essere leggibili senza mapping.
            Assert.That(jsonl, Does.Contain("\"kind\":\"event\""));
            Assert.That(jsonl, Does.Contain("\"eventType\":\"Failed\""));
            Assert.That(jsonl, Does.Contain("\"activeMode\":\"GOAL_LOCAL_SEARCH\""));
            Assert.That(jsonl, Does.Contain("\"failureType\":\"StuckTimeout\""));
            Assert.That(jsonl, Does.Contain("\"blockingCellText\":\"(7, 7)\""));
            Assert.That(jsonl, Does.Not.Contain("\"EventType\":"));
            Assert.That(jsonl, Does.Not.Contain("\"FailureType\":"));
        }

        // =============================================================================
        // MakeConfig
        // =============================================================================
        /// <summary>
        /// <para>
        /// Crea una configurazione minima per abilitare solo il sink JSONL. Il resto
        /// dei parametri EL non viene usato da questi test perche' non passiamo
        /// dall'emitter o dal World.
        /// </para>
        /// </summary>
        private static MovementExplainabilityParams MakeConfig(string fileName)
        {
            return new MovementExplainabilityParams
            {
                enabled = true,
                defaultVerbosity = 2,
                writeJsonLog = true,
                jsonLogFileNamePattern = fileName,
            };
        }

        // =============================================================================
        // ResetLogFile
        // =============================================================================
        /// <summary>
        /// <para>
        /// Calcola il path usato dal sink e cancella eventuali file precedenti. Il test
        /// usa la stessa cartella runtime del prodotto, ma solo con nomi file prefissati
        /// <c>qa_el_</c> e controllati dal test.
        /// </para>
        /// </summary>
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
