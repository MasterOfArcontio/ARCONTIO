using Arcontio.Core;
using NUnit.Framework;
using System;
using UnityEditor;
using UnityEngine;

namespace Arcontio.Tests
{
    // =============================================================================
    // StepRecoveryPolicyQaTests
    // =============================================================================
    /// <summary>
    /// <para>
    /// Test QA EditMode per il modello passivo di policy dichiarativa del recupero
    /// locale degli step Job.
    /// </para>
    ///
    /// <para><b>v0.11c.04d - Policy DTO senza recovery runtime</b></para>
    /// <para>
    /// Questi test verificano solo forma e neutralita' del DTO. Non istanziano
    /// <c>JobExecutionSystem</c>, non avanzano job, non emettono command, non mutano
    /// il World e non stabiliscono mapping tra failure kind, strategie e
    /// recuperabilita'.
    /// </para>
    ///
    /// <para><b>Struttura interna:</b></para>
    /// <list type="bullet">
    ///   <item><b>CLI harness</b>: entry point diagnostico per batchmode quando il runner XML non parte.</item>
    ///   <item><b>DTO shape</b>: failure kind, strategie candidate e limiti dichiarativi.</item>
    ///   <item><b>Empty policy</b>: assenza di policy senza semantica di recoverability.</item>
    ///   <item><b>Distinct types</b>: separazione da failure kind e strategy vocabulary.</item>
    /// </list>
    /// </summary>
    public sealed class StepRecoveryPolicyQaTests
    {
        // =============================================================================
        // RunFromCommandLine
        // =============================================================================
        /// <summary>
        /// <para>
        /// Entry point minimo per eseguire questi QA test da Unity batchmode quando
        /// il runner CLI standard non produce il file XML dei risultati.
        /// </para>
        /// </summary>
        public static void RunFromCommandLine()
        {
            try
            {
                var tests = new StepRecoveryPolicyQaTests();

                tests.StepRecoveryPolicyCanRepresentFailureKindStrategiesAndLimits();
                tests.StepRecoveryPolicyLimitsArePassiveDataOnly();
                tests.EmptyPolicyDoesNotImplyRecoverability();
                tests.StepRecoveryPolicyRemainsDistinctFromVocabularyEnums();

                Debug.Log("[StepRecoveryPolicyQaTests] PASS");
                EditorApplication.Exit(0);
            }
            catch (Exception ex)
            {
                Debug.LogError("[StepRecoveryPolicyQaTests] FAIL\n" + ex);
                EditorApplication.Exit(1);
            }
        }

        // =============================================================================
        // StepRecoveryPolicyCanRepresentFailureKindStrategiesAndLimits
        // =============================================================================
        /// <summary>
        /// <para>
        /// Verifica che il DTO possa conservare failure kind, strategie candidate e
        /// limiti dichiarativi senza introdurre mapping o comportamento runtime.
        /// </para>
        /// </summary>
        [Test]
        public void StepRecoveryPolicyCanRepresentFailureKindStrategiesAndLimits()
        {
            var policy = new StepRecoveryPolicy(
                JobStepFailureKind.PathBlocked,
                new[] { StepRecoveryStrategy.Repath, StepRecoveryStrategy.FindAlternateCell },
                maxRetryCount: 2,
                maxRecoveryTicks: 12,
                maxSearchRadius: 3,
                maxAlternativeTargets: 1,
                maxRepathAttempts: 2);

            Assert.That(policy.FailureKind, Is.EqualTo(JobStepFailureKind.PathBlocked));
            Assert.That(policy.Strategies, Is.EquivalentTo(new[] { StepRecoveryStrategy.Repath, StepRecoveryStrategy.FindAlternateCell }));
            Assert.That(policy.ContainsStrategy(StepRecoveryStrategy.Repath), Is.True);
            Assert.That(policy.ContainsStrategy(StepRecoveryStrategy.WaitAndRetry), Is.False);
            Assert.That(policy.HasDeclaredData, Is.True);
        }

        // =============================================================================
        // StepRecoveryPolicyLimitsArePassiveDataOnly
        // =============================================================================
        /// <summary>
        /// <para>
        /// Verifica che i limiti vengano conservati come soli dati normalizzati e non
        /// producano avanzamento, retry, command o accessi al runtime.
        /// </para>
        /// </summary>
        [Test]
        public void StepRecoveryPolicyLimitsArePassiveDataOnly()
        {
            var policy = new StepRecoveryPolicy(
                JobStepFailureKind.Timeout,
                null,
                maxRetryCount: -1,
                maxRecoveryTicks: 20,
                maxSearchRadius: -5,
                maxAlternativeTargets: 4,
                maxRepathAttempts: -2);

            Assert.That(policy.Strategies, Is.Empty);
            Assert.That(policy.MaxRetryCount, Is.EqualTo(0));
            Assert.That(policy.MaxRecoveryTicks, Is.EqualTo(20));
            Assert.That(policy.MaxSearchRadius, Is.EqualTo(0));
            Assert.That(policy.MaxAlternativeTargets, Is.EqualTo(4));
            Assert.That(policy.MaxRepathAttempts, Is.EqualTo(0));
            Assert.That((object)policy, Is.Not.AssignableTo<ICommand>());
        }

        // =============================================================================
        // EmptyPolicyDoesNotImplyRecoverability
        // =============================================================================
        /// <summary>
        /// <para>
        /// Verifica che la policy vuota significhi solo "nessuna policy dichiarata",
        /// senza decidere se un recovery sia vietato o ammesso.
        /// </para>
        /// </summary>
        [Test]
        public void EmptyPolicyDoesNotImplyRecoverability()
        {
            var policy = StepRecoveryPolicy.Empty();

            Assert.That(policy.FailureKind, Is.EqualTo(JobStepFailureKind.None));
            Assert.That(policy.Strategies, Is.Empty);
            Assert.That(policy.HasDeclaredData, Is.False);
            Assert.That(policy.ContainsStrategy(StepRecoveryStrategy.RetrySameAction), Is.False);
            Assert.That(policy.ContainsStrategy(StepRecoveryStrategy.None), Is.False);
        }

        // =============================================================================
        // StepRecoveryPolicyRemainsDistinctFromVocabularyEnums
        // =============================================================================
        /// <summary>
        /// <para>
        /// Verifica che il modello policy resti distinto dai vocabolari puri senza
        /// trasformarsi in mapping verso failure kind, strategy, step result o job
        /// failure reason.
        /// </para>
        /// </summary>
        [Test]
        public void StepRecoveryPolicyRemainsDistinctFromVocabularyEnums()
        {
            Assert.That(typeof(StepRecoveryPolicy), Is.Not.EqualTo(typeof(JobStepFailureKind)));
            Assert.That(typeof(StepRecoveryPolicy), Is.Not.EqualTo(typeof(StepRecoveryStrategy)));
            Assert.That(typeof(StepRecoveryPolicy), Is.Not.EqualTo(typeof(StepResultStatus)));
            Assert.That(typeof(StepRecoveryPolicy), Is.Not.EqualTo(typeof(JobFailureReason)));
        }
    }
}
