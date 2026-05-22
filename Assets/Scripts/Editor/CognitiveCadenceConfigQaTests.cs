using Arcontio.Core.Config;
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
    }
}
