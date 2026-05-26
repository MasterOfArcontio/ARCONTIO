using Arcontio.Core.Config;
using Arcontio.Core.Logging;
using NUnit.Framework;
using System;
using UnityEditor;
using UnityEngine;

namespace Arcontio.Tests
{
    // =============================================================================
    // CognitiveCadenceConfigQaTests
    // =============================================================================
    /// <summary>
    /// <para>
    /// Test QA EditMode per la configurazione tipizzata e transitoria della cadence
    /// cognitiva ordinaria usata dal bridge legacy <c>NeedsDecisionRule</c>.
    /// </para>
    ///
    /// <para><b>v0.11c.03b - Cognitive cadence config senza behavior change</b></para>
    /// <para>
    /// Questi test proteggono il confine della patch: il parametro
    /// <c>decisionEveryTicks</c> viene accettato solo come nome legacy-compatible
    /// del valore gia' hardcoded nel runtime. La config non cabla
    /// <c>NpcDecisionScheduler</c>, non introduce eventi soglia, non emette command
    /// e non definisce policy future di deduplica, batching, cooldown o multi-reason.
    /// </para>
    ///
    /// <para><b>Struttura interna:</b></para>
    /// <list type="bullet">
    ///   <item><b>Default legacy</b>: assenza di JSON esplicito conserva 25 tick.</item>
    ///   <item><b>JSON tipizzato</b>: la sezione <c>decision</c> puo' fornire il valore.</item>
    ///   <item><b>Clamp</b>: valori non positivi cadono al minimo tecnico 1.</item>
    /// </list>
    /// </summary>
    public sealed class CognitiveCadenceConfigQaTests
    {
        // =============================================================================
        // RunFromCommandLine
        // =============================================================================
        /// <summary>
        /// <para>
        /// Entry point minimo per eseguire questi QA test da Unity batchmode quando
        /// il runner CLI standard non produce il file XML dei risultati.
        /// </para>
        ///
        /// <para><b>Harness diagnostico senza runtime behavior</b></para>
        /// <para>
        /// Il metodo invoca gli stessi test NUnit della classe e termina l'Editor con
        /// codice non-zero solo in caso di eccezione. Non viene chiamato dal runtime
        /// simulativo e non modifica configurazioni, world state o command buffer.
        /// </para>
        /// </summary>
        public static void RunFromCommandLine()
        {
            try
            {
                var tests = new CognitiveCadenceConfigQaTests();

                tests.DecisionEveryTicksDefaultsToLegacyHardcodedCadence();
                tests.DecisionEveryTicksLoadsFromDecisionSection();
                tests.DecisionEveryTicksClampsNonPositiveValuesToOne(0);
                tests.DecisionEveryTicksClampsNonPositiveValuesToOne(-4);
                tests.LoggingRuntimeSectionLoadsGeneralAndJsonlDefaults();
                tests.LoggingRuntimeSectionMapsExplainabilityToRuntimeRegistries();

                Debug.Log("[CognitiveCadenceConfigQaTests] PASS");
                EditorApplication.Exit(0);
            }
            catch (Exception ex)
            {
                Debug.LogError("[CognitiveCadenceConfigQaTests] FAIL\n" + ex);
                EditorApplication.Exit(1);
            }
        }

        // =============================================================================
        // DecisionEveryTicksDefaultsToLegacyHardcodedCadence
        // =============================================================================
        /// <summary>
        /// <para>
        /// Verifica che un DTO default conservi la cadence produttiva storica di 25
        /// tick senza richiedere modifiche a <c>game_params.json</c>.
        /// </para>
        /// </summary>
        [Test]
        public void DecisionEveryTicksDefaultsToLegacyHardcodedCadence()
        {
            var sim = new SimulationParams();

            Assert.That(sim.ResolveDecisionEveryTicks(), Is.EqualTo(25));
            Assert.That(sim.ResolveDecisionEveryTicks(), Is.EqualTo(DecisionRuntimeParams.DefaultDecisionEveryTicks));
        }

        // =============================================================================
        // DecisionEveryTicksLoadsFromDecisionSection
        // =============================================================================
        /// <summary>
        /// <para>
        /// Verifica che <c>JsonUtility</c> popoli la cadence dalla sezione
        /// <c>decision</c>, cioe' dal gruppo gia' esistente per parametri del
        /// Decision Layer.
        /// </para>
        /// </summary>
        [Test]
        public void DecisionEveryTicksLoadsFromDecisionSection()
        {
            const string json = "{"
                + "\"decision\":{"
                + "\"decisionEveryTicks\":13,"
                + "\"selectionMode\":\"DeterministicTop1\","
                + "\"topN\":1"
                + "}"
                + "}";

            var sim = JsonUtility.FromJson<SimulationParams>(json);

            Assert.That(sim.ResolveDecisionEveryTicks(), Is.EqualTo(13));
            Assert.That(sim.decision.selectionMode, Is.EqualTo("DeterministicTop1"));
            Assert.That(sim.decision.topN, Is.EqualTo(1));
        }

        // =============================================================================
        // DecisionEveryTicksClampsNonPositiveValuesToOne
        // =============================================================================
        /// <summary>
        /// <para>
        /// Verifica il fallback tecnico minimo: una cadence nulla o negativa non
        /// puo' disabilitare implicitamente il bridge decisionale legacy.
        /// </para>
        /// </summary>
        [TestCase(0)]
        [TestCase(-4)]
        public void DecisionEveryTicksClampsNonPositiveValuesToOne(int rawValue)
        {
            var sim = new SimulationParams();
            sim.decision.decisionEveryTicks = rawValue;

            Assert.That(sim.ResolveDecisionEveryTicks(), Is.EqualTo(1));
        }

        // =============================================================================
        // LoggingRuntimeSectionLoadsGeneralAndJsonlDefaults
        // =============================================================================
        /// <summary>
        /// <para>
        /// Verifica che il nuovo gruppo canonico <c>logging</c> venga letto dal
        /// logger generale senza dipendere dalla vecchia sezione root <c>Logging</c>.
        /// </para>
        /// </summary>
        [Test]
        public void LoggingRuntimeSectionLoadsGeneralAndJsonlDefaults()
        {
            const string json = "{"
                + "\"logging\":{"
                + "\"general\":{\"enabled\":true,\"minimum_level\":\"Warn\",\"include_timestamp\":true,\"include_tick\":true},"
                + "\"legacy_channels\":{\"unity_console_enabled\":false,\"html_file_enabled\":false,\"txt_file_enabled\":false},"
                + "\"jsonl\":{\"enabled\":true,\"flush_interval_seconds\":0.25,\"max_queue_size\":4096,\"max_batch_size\":512}"
                + "}"
                + "}";

            var game = JsonUtility.FromJson<GameParams>(json);
            var logging = game.ResolveLogging();

            Assert.That(logging.general.enabled, Is.True);
            Assert.That(logging.general.minimum_level, Is.EqualTo("Warn"));
            Assert.That(logging.legacy_channels.unity_console_enabled, Is.False);
            Assert.That(logging.legacy_channels.html_file_enabled, Is.False);
            Assert.That(logging.legacy_channels.txt_file_enabled, Is.False);
            Assert.That(logging.jsonl.enabled, Is.True);
            Assert.That(logging.jsonl.max_queue_size, Is.EqualTo(4096));
            Assert.That(logging.jsonl.max_batch_size, Is.EqualTo(512));
        }

        // =============================================================================
        // LoggingRuntimeSectionMapsExplainabilityToRuntimeRegistries
        // =============================================================================
        /// <summary>
        /// <para>
        /// Verifica il ponte di compatibilita': il nuovo gruppo diagnostico produce
        /// ancora i campi legacy che gli emitter e i pannelli explainability leggono.
        /// </para>
        /// </summary>
        [Test]
        public void LoggingRuntimeSectionMapsExplainabilityToRuntimeRegistries()
        {
            const string json = "{"
                + "\"logging\":{"
                + "\"movement_explainability\":{"
                + "\"enabled\":true,\"runtime_registry_enabled\":true,\"file_logging_enabled\":true,"
                + "\"defaultVerbosity\":2,\"maxTrackedNpcs\":3,"
                + "\"jsonLogFileNamePattern\":\"movement.jsonl\""
                + "},"
                + "\"memory_belief_decision_explainability\":{"
                + "\"enabled\":true,\"runtime_registry_enabled\":true,\"file_logging_enabled\":false,"
                + "\"defaultVerbosity\":3,\"maxTrackedNpcs\":5,"
                + "\"jsonLogFileNamePattern\":\"mbqd.jsonl\","
                + "\"includeRejectedCandidates\":true"
                + "},"
                + "\"devtools\":{\"debug_fov\":{\"enabled\":true,\"window_ticks\":8,\"use_los\":true,\"activeNpcOnly\":true}}"
                + "}"
                + "}";

            var sim = JsonUtility.FromJson<SimulationParams>(json);
            sim.ApplyRuntimeDiagnosticsLayout();

            Assert.That(sim.debug_fov.enabled, Is.True);
            Assert.That(sim.explainability.enabled, Is.True);
            Assert.That(sim.explainability.writeJsonLog, Is.True);
            Assert.That(sim.explainability.jsonLogFileNamePattern, Is.EqualTo("movement.jsonl"));
            Assert.That(sim.memory_belief_decision_explainability.enabled, Is.True);
            Assert.That(sim.memory_belief_decision_explainability.writeJsonLog, Is.False);
            Assert.That(sim.memory_belief_decision_explainability.includeRejectedCandidates, Is.True);
            Assert.That(sim.memory_belief_decision_explainability.jsonLogFileNamePattern, Is.EqualTo("mbqd.jsonl"));
        }
    }
}
