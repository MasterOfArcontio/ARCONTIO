using Arcontio.Core;
using NUnit.Framework;
using System;
using UnityEditor;
using UnityEngine;

namespace Arcontio.Tests
{
    // =============================================================================
    // JobRecoveryResultQaTests
    // =============================================================================
    /// <summary>
    /// <para>
    /// Test QA EditMode per il modello passivo del risultato candidato di recovery
    /// locale degli step Job.
    /// </para>
    ///
    /// <para><b>v0.11c.04e - Recovery result senza recovery runtime</b></para>
    /// <para>
    /// Questi test verificano solo vocabolario, forma dati e neutralita' del DTO.
    /// Non istanziano <c>JobExecutionSystem</c>, non avanzano job, non emettono
    /// command, non mutano il World e non stabiliscono recoverability.
    /// </para>
    ///
    /// <para><b>Struttura interna:</b></para>
    /// <list type="bullet">
    ///   <item><b>CLI harness</b>: entry point diagnostico per batchmode quando il runner XML non parte.</item>
    ///   <item><b>Vocabulary coverage</b>: tutti i result kind candidati sono rappresentati.</item>
    ///   <item><b>None semantics</b>: assenza di risultato senza successo o fallimento impliciti.</item>
    ///   <item><b>DTO shape</b>: conservazione passiva di failure, strategy, result e diagnostic.</item>
    ///   <item><b>Distinct types</b>: separazione da step result e job failure runtime.</item>
    /// </list>
    /// </summary>
    public sealed class JobRecoveryResultQaTests
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
                var tests = new JobRecoveryResultQaTests();

                tests.JobRecoveryResultKindContainsCandidateVocabulary();
                tests.JobRecoveryResultNoneIsNotSuccessOrFailure();
                tests.JobRecoveryResultCanCarryPassiveRecoveryData();
                tests.JobRecoveryResultFactoriesRemainDataOnly();
                tests.JobRecoveryResultRemainsDistinctFromRuntimeResults();

                Debug.Log("[JobRecoveryResultQaTests] PASS");
                EditorApplication.Exit(0);
            }
            catch (Exception ex)
            {
                Debug.LogError("[JobRecoveryResultQaTests] FAIL\n" + ex);
                EditorApplication.Exit(1);
            }
        }

        // =============================================================================
        // JobRecoveryResultKindContainsCandidateVocabulary
        // =============================================================================
        /// <summary>
        /// <para>
        /// Verifica che il vocabolario contenga tutti gli esiti candidati del
        /// modello passivo di recovery result.
        /// </para>
        /// </summary>
        [Test]
        public void JobRecoveryResultKindContainsCandidateVocabulary()
        {
            Assert.That(Enum.IsDefined(typeof(JobRecoveryResultKind), JobRecoveryResultKind.None), Is.True);
            Assert.That(Enum.IsDefined(typeof(JobRecoveryResultKind), JobRecoveryResultKind.Recovered), Is.True);
            Assert.That(Enum.IsDefined(typeof(JobRecoveryResultKind), JobRecoveryResultKind.RetryScheduled), Is.True);
            Assert.That(Enum.IsDefined(typeof(JobRecoveryResultKind), JobRecoveryResultKind.TargetReplaced), Is.True);
            Assert.That(Enum.IsDefined(typeof(JobRecoveryResultKind), JobRecoveryResultKind.PhaseRebuilt), Is.True);
            Assert.That(Enum.IsDefined(typeof(JobRecoveryResultKind), JobRecoveryResultKind.PhaseFailed), Is.True);
            Assert.That(Enum.IsDefined(typeof(JobRecoveryResultKind), JobRecoveryResultKind.JobFailed), Is.True);
            Assert.That(Enum.IsDefined(typeof(JobRecoveryResultKind), JobRecoveryResultKind.EscalateToDecision), Is.True);
        }

        // =============================================================================
        // JobRecoveryResultNoneIsNotSuccessOrFailure
        // =============================================================================
        /// <summary>
        /// <para>
        /// Verifica che <c>None</c> resti un valore neutro: nessun risultato
        /// dichiarato, non successo e non fallimento.
        /// </para>
        /// </summary>
        [Test]
        public void JobRecoveryResultNoneIsNotSuccessOrFailure()
        {
            var result = JobRecoveryResult.None();

            Assert.That(result.Kind, Is.EqualTo(JobRecoveryResultKind.None));
            Assert.That(result.Kind, Is.Not.EqualTo(JobRecoveryResultKind.Recovered));
            Assert.That(result.Kind, Is.Not.EqualTo(JobRecoveryResultKind.JobFailed));
            Assert.That(result.HasDeclaredResult, Is.False);
            Assert.That(result.AppliedStrategy, Is.EqualTo(StepRecoveryStrategy.None));
            Assert.That(result.FailureKind, Is.EqualTo(JobStepFailureKind.None));
        }

        // =============================================================================
        // JobRecoveryResultCanCarryPassiveRecoveryData
        // =============================================================================
        /// <summary>
        /// <para>
        /// Verifica che il DTO possa conservare dati gia' classificati senza
        /// interpretarli o collegarli a runtime recovery.
        /// </para>
        /// </summary>
        [Test]
        public void JobRecoveryResultCanCarryPassiveRecoveryData()
        {
            var result = new JobRecoveryResult(
                JobRecoveryResultKind.RetryScheduled,
                StepRecoveryStrategy.WaitAndRetry,
                JobStepFailureKind.ReservationConflict,
                suggestedWaitTicks: 3,
                diagnostic: "ReservationRetryCandidate");

            Assert.That(result.Kind, Is.EqualTo(JobRecoveryResultKind.RetryScheduled));
            Assert.That(result.AppliedStrategy, Is.EqualTo(StepRecoveryStrategy.WaitAndRetry));
            Assert.That(result.FailureKind, Is.EqualTo(JobStepFailureKind.ReservationConflict));
            Assert.That(result.SuggestedWaitTicks, Is.EqualTo(3));
            Assert.That(result.Diagnostic, Is.EqualTo("ReservationRetryCandidate"));
            Assert.That(result.HasDeclaredResult, Is.True);
        }

        // =============================================================================
        // JobRecoveryResultFactoriesRemainDataOnly
        // =============================================================================
        /// <summary>
        /// <para>
        /// Verifica che gli helper statici restino costruttori dati e non implichino
        /// avanzamento job, policy produttiva o recoverability.
        /// </para>
        /// </summary>
        [Test]
        public void JobRecoveryResultFactoriesRemainDataOnly()
        {
            var result = JobRecoveryResult.FromData(
                JobRecoveryResultKind.EscalateToDecision,
                StepRecoveryStrategy.EscalateToDecision,
                JobStepFailureKind.InsufficientInformation,
                suggestedWaitTicks: -5,
                diagnostic: null);

            Assert.That(result.Kind, Is.EqualTo(JobRecoveryResultKind.EscalateToDecision));
            Assert.That(result.AppliedStrategy, Is.EqualTo(StepRecoveryStrategy.EscalateToDecision));
            Assert.That(result.FailureKind, Is.EqualTo(JobStepFailureKind.InsufficientInformation));
            Assert.That(result.SuggestedWaitTicks, Is.EqualTo(0));
            Assert.That(result.Diagnostic, Is.Empty);
            Assert.That((object)result, Is.Not.AssignableTo<ICommand>());
        }

        // =============================================================================
        // JobRecoveryResultRemainsDistinctFromRuntimeResults
        // =============================================================================
        /// <summary>
        /// <para>
        /// Verifica che il modello resti distinto dagli esiti runtime esistenti e
        /// dai failure reason terminali del job.
        /// </para>
        /// </summary>
        [Test]
        public void JobRecoveryResultRemainsDistinctFromRuntimeResults()
        {
            Assert.That(typeof(JobRecoveryResult), Is.Not.EqualTo(typeof(StepResult)));
            Assert.That(typeof(JobRecoveryResult), Is.Not.EqualTo(typeof(StepResultStatus)));
            Assert.That(typeof(JobRecoveryResult), Is.Not.EqualTo(typeof(JobFailureReason)));
            Assert.That(typeof(JobRecoveryResultKind), Is.Not.EqualTo(typeof(StepResultStatus)));
            Assert.That(typeof(JobRecoveryResultKind), Is.Not.EqualTo(typeof(JobFailureReason)));
        }
    }
}
