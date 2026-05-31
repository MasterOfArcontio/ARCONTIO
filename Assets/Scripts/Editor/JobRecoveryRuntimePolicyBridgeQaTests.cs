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
    /// QA EditMode per il ponte controllato tra runtime degli incarichi e policy di
    /// recupero locale configurate nelle Resources.
    /// </para>
    ///
    /// <para><b>v0.14d - Retry locale controllato da policy recovery</b></para>
    /// <para>
    /// Questi test verificano che la configurazione fallback viva nella cartella
    /// <c>Assets/Resources/Arcontio/Jobs</c> sia caricabile e che
    /// <c>JobExecutionSystem</c> contenga un cablaggio limitato verso
    /// <c>StepRecoveryPolicyRegistry</c>, <c>StepFailureClassifier</c> e
    /// <c>StepRecoveryEvaluator.EvaluateLocalRetry</c>. Autorizzano solo retry dello
    /// stesso step entro limiti espliciti; non sostituiscono target, non emettono
    /// command e non trasformano il runtime Job in un secondo decisore.
    /// </para>
    ///
    /// <para><b>Struttura interna:</b></para>
    /// <list type="bullet">
    ///   <item><b>Resource config</b>: valida che il JSON fallback sia caricato da Resources.</item>
    ///   <item><b>Policy bridge</b>: controlla il cablaggio passivo nel runtime Job.</item>
    ///   <item><b>Retry guard</b>: conferma che il runtime passa da policy a retry bounded.</item>
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
                tests.MovementPolicyUsesImplementedRetriesBeforeFutureStrategies();
                tests.InsufficientInformationDoesNotPretendEquivalentTargetRecovery();
                tests.JobExecutionSystemBridgesClassifierPolicyAndLocalRetryEvaluator();
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
        public void MovementPolicyUsesImplementedRetriesBeforeFutureStrategies()
        {
            var registry = StepRecoveryPolicyRegistry.LoadDefault();

            Assert.That(registry.TryGetPolicy(JobStepFailureKind.PathBlocked, out var pathPolicy), Is.True);
            Assert.That(pathPolicy.Strategies.Length, Is.GreaterThanOrEqualTo(2));
            Assert.That(pathPolicy.Strategies[0], Is.EqualTo(StepRecoveryStrategy.WaitAndRetry));
            Assert.That(pathPolicy.ContainsStrategy(StepRecoveryStrategy.Repath), Is.True);
            Assert.That(pathPolicy.ContainsStrategy(StepRecoveryStrategy.FindAlternateCell), Is.True);

            Assert.That(registry.TryGetPolicy(JobStepFailureKind.Timeout, out var timeoutPolicy), Is.True);
            Assert.That(timeoutPolicy.Strategies.Length, Is.GreaterThanOrEqualTo(2));
            Assert.That(timeoutPolicy.Strategies[0], Is.EqualTo(StepRecoveryStrategy.WaitAndRetry));
            Assert.That(timeoutPolicy.Strategies[1], Is.EqualTo(StepRecoveryStrategy.RetrySameAction));
        }

        [Test]
        public void InsufficientInformationDoesNotPretendEquivalentTargetRecovery()
        {
            var registry = StepRecoveryPolicyRegistry.LoadDefault();

            Assert.That(registry.TryGetPolicy(JobStepFailureKind.InsufficientInformation, out var policy), Is.True);
            Assert.That(policy.MaxRetryCount, Is.EqualTo(0));
            Assert.That(policy.MaxSearchRadius, Is.EqualTo(0));
            Assert.That(policy.MaxAlternativeTargets, Is.EqualTo(0));
            Assert.That(policy.ContainsStrategy(StepRecoveryStrategy.FindEquivalentTarget), Is.False);
            Assert.That(policy.ContainsStrategy(StepRecoveryStrategy.EscalateToDecision), Is.True);
            Assert.That(policy.ContainsStrategy(StepRecoveryStrategy.FailJob), Is.True);
        }

        [Test]
        public void JobExecutionSystemBridgesClassifierPolicyAndLocalRetryEvaluator()
        {
            var source = File.ReadAllText("Assets/Scripts/Core/Jobs/JobExecutionSystem.cs");

            Assert.That(source, Does.Contain("StepRecoveryPolicyRegistry.LoadDefault()"));
            Assert.That(source, Does.Contain("StepFailureClassifier.Classify"));
            Assert.That(source, Does.Contain("TryGetPolicy(classification.FailureKind"));
            Assert.That(source, Does.Contain("EvaluateLocalRetry("));
            Assert.That(source, Does.Contain("RegisterRecoveryRetry("));
            Assert.That(source, Does.Contain("RecoveryRetryScheduled:"));
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
