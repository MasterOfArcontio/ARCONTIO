using Arcontio.Core;
using NUnit.Framework;
using System;
using System.IO;
using UnityEditor;
using UnityEngine;

namespace Arcontio.Tests
{
    // =============================================================================
    // JobRecoveryRuntimePolicyBridgeQaTests
    // =============================================================================
    /// <summary>
    /// <para>
    /// QA EditMode per il ponte passivo tra runtime degli incarichi e policy di
    /// recupero locale configurate nelle Resources.
    /// </para>
    ///
    /// <para><b>v0.14c - Policy recovery consultate senza recovery produttiva</b></para>
    /// <para>
    /// Questi test verificano che la configurazione fallback viva nella cartella
    /// <c>Assets/Resources/Arcontio/Jobs</c> sia caricabile e che
    /// <c>JobExecutionSystem</c> contenga solo un cablaggio passivo verso
    /// <c>StepRecoveryPolicyRegistry</c>, <c>StepFailureClassifier</c> e
    /// <c>StepRecoveryEvaluator.EvaluateNoOp</c>. Non autorizzano retry, non
    /// sostituiscono target, non emettono command e non trasformano il runtime Job
    /// in un secondo decisore.
    /// </para>
    ///
    /// <para><b>Struttura interna:</b></para>
    /// <list type="bullet">
    ///   <item><b>Resource config</b>: valida che il JSON fallback sia caricato da Resources.</item>
    ///   <item><b>Policy bridge</b>: controlla il cablaggio passivo nel runtime Job.</item>
    ///   <item><b>No-op guard</b>: conferma che il valutatore non produce ancora recovery.</item>
    /// </list>
    /// </summary>
    public sealed class JobRecoveryRuntimePolicyBridgeQaTests
    {
        public static void RunFromCommandLine()
        {
            try
            {
                var tests = new JobRecoveryRuntimePolicyBridgeQaTests();

                tests.DefaultRecoveryPolicyRegistryLoadsResourcesJobConfig();
                tests.JobExecutionSystemBridgesClassifierPolicyAndNoOpEvaluator();
                tests.ConfiguredPolicyStillProducesNoRuntimeRecoveryInNoOpEvaluator();

                Debug.Log("[JobRecoveryRuntimePolicyBridgeQaTests] PASS");
                EditorApplication.Exit(0);
            }
            catch (Exception ex)
            {
                Debug.LogError("[JobRecoveryRuntimePolicyBridgeQaTests] FAIL\n" + ex);
                EditorApplication.Exit(1);
            }
        }

        [Test]
        public void DefaultRecoveryPolicyRegistryLoadsResourcesJobConfig()
        {
            var registry = StepRecoveryPolicyRegistry.LoadDefault();

            Assert.That(registry.Count, Is.GreaterThan(0));
            Assert.That(registry.TryGetPolicy(JobStepFailureKind.PathBlocked, out var pathPolicy), Is.True);
            Assert.That(pathPolicy.ContainsStrategy(StepRecoveryStrategy.Repath), Is.True);
            Assert.That(pathPolicy.ContainsStrategy(StepRecoveryStrategy.WaitAndRetry), Is.True);
            Assert.That(registry.TryGetPolicy(JobStepFailureKind.ResourceMissing, out var resourcePolicy), Is.True);
            Assert.That(resourcePolicy.ContainsStrategy(StepRecoveryStrategy.FindEquivalentTarget), Is.True);
        }

        [Test]
        public void JobExecutionSystemBridgesClassifierPolicyAndNoOpEvaluator()
        {
            var source = File.ReadAllText("Assets/Scripts/Core/Jobs/JobExecutionSystem.cs");

            Assert.That(source, Does.Contain("StepRecoveryPolicyRegistry.LoadDefault()"));
            Assert.That(source, Does.Contain("StepFailureClassifier.Classify"));
            Assert.That(source, Does.Contain("TryGetPolicy(classification.FailureKind"));
            Assert.That(source, Does.Contain("EvaluateNoOp(classification, policy)"));
            Assert.That(source, Does.Contain("EvaluateRecoveryPolicyNoOp(job, in npcState, result)"));
        }

        [Test]
        public void ConfiguredPolicyStillProducesNoRuntimeRecoveryInNoOpEvaluator()
        {
            var registry = StepRecoveryPolicyRegistry.LoadDefault();
            Assert.That(registry.TryGetPolicy(JobStepFailureKind.PathBlocked, out var policy), Is.True);

            var action = JobAction.MoveTo("move", new Vector2Int(1, 1), "move");
            var classification = StepFailureClassifier.Classify(
                StepResult.Blocked(2, "TraversalTargetBlocked"),
                action,
                0,
                0);

            var result = new StepRecoveryEvaluator().EvaluateNoOp(classification, policy);

            Assert.That(classification.HasClassification, Is.True);
            Assert.That(result.Kind, Is.EqualTo(JobRecoveryResultKind.None));
            Assert.That(result.HasDeclaredResult, Is.False);
            Assert.That(result.AppliedStrategy, Is.EqualTo(StepRecoveryStrategy.None));
        }
    }
}
