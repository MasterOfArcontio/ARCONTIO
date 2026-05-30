using Arcontio.Core;
using NUnit.Framework;
using System;
using UnityEditor;
using UnityEngine;

namespace Arcontio.Tests
{
    // =============================================================================
    // StepRecoveryEvaluatorLocalRetryQaTests
    // =============================================================================
    /// <summary>
    /// <para>
    /// QA EditMode per il primo comportamento produttivo e limitato dello
    /// <c>StepRecoveryEvaluator</c>: retry locale dello stesso step.
    /// </para>
    ///
    /// <para><b>v0.14d - Retry bounded senza recovery intelligente</b></para>
    /// <para>
    /// Questi test verificano solo la parte sicura della recovery: una policy che
    /// dichiara <c>RetrySameAction</c> o <c>WaitAndRetry</c> puo' produrre
    /// <c>RetryScheduled</c> finche' contatore e finestra temporale restano nei
    /// limiti. Non vengono creati command, target alternativi, fasi ricostruite o
    /// richieste al Decision Layer.
    /// </para>
    ///
    /// <para><b>Struttura interna:</b></para>
    /// <list type="bullet">
    ///   <item><b>Happy path</b>: retry programmabile sotto limite.</item>
    ///   <item><b>Limite contatore</b>: nessun retry quando i tentativi sono esauriti.</item>
    ///   <item><b>Limite tempo</b>: nessun retry quando la finestra recovery e' scaduta.</item>
    /// </list>
    /// </summary>
    public sealed class StepRecoveryEvaluatorLocalRetryQaTests
    {
        public static void RunFromCommandLine()
        {
            try
            {
                var tests = new StepRecoveryEvaluatorLocalRetryQaTests();

                tests.LocalRetrySchedulesRetryWhenPolicyAllowsIt();
                tests.LocalRetryStopsWhenRetryCountLimitIsReached();
                tests.LocalRetryStopsWhenRecoveryWindowIsExceeded();
                tests.LocalRetryDoesNotApplyNonRetryStrategies();
                tests.EquivalentTargetProducesTargetReplacedWhenPolicyAllowsIt();
                tests.EquivalentTargetStopsWhenAlternativeLimitIsReached();

                Debug.Log("[StepRecoveryEvaluatorLocalRetryQaTests] PASS");
                EditorApplication.Exit(0);
            }
            catch (Exception ex)
            {
                Debug.LogError("[StepRecoveryEvaluatorLocalRetryQaTests] FAIL\n" + ex);
                EditorApplication.Exit(1);
            }
        }

        [Test]
        public void LocalRetrySchedulesRetryWhenPolicyAllowsIt()
        {
            var result = new StepRecoveryEvaluator().EvaluateLocalRetry(
                MakeClassification(JobStepFailureKind.PathBlocked, suggestedWaitTicks: 3),
                MakePolicy(JobStepFailureKind.PathBlocked, 2, 20, StepRecoveryStrategy.WaitAndRetry),
                currentRetryCount: 0,
                recoveryElapsedTicks: 5);

            Assert.That(result.Kind, Is.EqualTo(JobRecoveryResultKind.RetryScheduled));
            Assert.That(result.AppliedStrategy, Is.EqualTo(StepRecoveryStrategy.WaitAndRetry));
            Assert.That(result.FailureKind, Is.EqualTo(JobStepFailureKind.PathBlocked));
            Assert.That(result.SuggestedWaitTicks, Is.EqualTo(3));
        }

        [Test]
        public void LocalRetryStopsWhenRetryCountLimitIsReached()
        {
            var result = new StepRecoveryEvaluator().EvaluateLocalRetry(
                MakeClassification(JobStepFailureKind.Timeout, suggestedWaitTicks: 1),
                MakePolicy(JobStepFailureKind.Timeout, 1, 20, StepRecoveryStrategy.RetrySameAction),
                currentRetryCount: 1,
                recoveryElapsedTicks: 3);

            Assert.That(result.Kind, Is.EqualTo(JobRecoveryResultKind.None));
            Assert.That(result.HasDeclaredResult, Is.False);
        }

        [Test]
        public void LocalRetryStopsWhenRecoveryWindowIsExceeded()
        {
            var result = new StepRecoveryEvaluator().EvaluateLocalRetry(
                MakeClassification(JobStepFailureKind.ReservationConflict, suggestedWaitTicks: 2),
                MakePolicy(JobStepFailureKind.ReservationConflict, 3, 4, StepRecoveryStrategy.WaitAndRetry),
                currentRetryCount: 1,
                recoveryElapsedTicks: 5);

            Assert.That(result.Kind, Is.EqualTo(JobRecoveryResultKind.None));
            Assert.That(result.HasDeclaredResult, Is.False);
        }

        [Test]
        public void LocalRetryDoesNotApplyNonRetryStrategies()
        {
            var result = new StepRecoveryEvaluator().EvaluateLocalRetry(
                MakeClassification(JobStepFailureKind.ResourceMissing, suggestedWaitTicks: 1),
                MakePolicy(JobStepFailureKind.ResourceMissing, 3, 20, StepRecoveryStrategy.FindEquivalentTarget),
                currentRetryCount: 0,
                recoveryElapsedTicks: 0);

            Assert.That(result.Kind, Is.EqualTo(JobRecoveryResultKind.None));
            Assert.That(result.AppliedStrategy, Is.EqualTo(StepRecoveryStrategy.None));
        }

        [Test]
        public void EquivalentTargetProducesTargetReplacedWhenPolicyAllowsIt()
        {
            var result = new StepRecoveryEvaluator().EvaluateEquivalentTarget(
                MakeClassification(JobStepFailureKind.ResourceMissing, suggestedWaitTicks: 0),
                MakePolicy(JobStepFailureKind.ResourceMissing, 1, 12, StepRecoveryStrategy.FindEquivalentTarget),
                currentAlternativeTargetCount: 0,
                recoveryElapsedTicks: 3,
                hasEquivalentTarget: true);

            Assert.That(result.Kind, Is.EqualTo(JobRecoveryResultKind.TargetReplaced));
            Assert.That(result.AppliedStrategy, Is.EqualTo(StepRecoveryStrategy.FindEquivalentTarget));
            Assert.That(result.FailureKind, Is.EqualTo(JobStepFailureKind.ResourceMissing));
        }

        [Test]
        public void EquivalentTargetStopsWhenAlternativeLimitIsReached()
        {
            var result = new StepRecoveryEvaluator().EvaluateEquivalentTarget(
                MakeClassification(JobStepFailureKind.TargetInvalid, suggestedWaitTicks: 0),
                MakePolicy(JobStepFailureKind.TargetInvalid, 1, 12, StepRecoveryStrategy.FindEquivalentTarget),
                currentAlternativeTargetCount: 1,
                recoveryElapsedTicks: 3,
                hasEquivalentTarget: true);

            Assert.That(result.Kind, Is.EqualTo(JobRecoveryResultKind.None));
            Assert.That(result.HasDeclaredResult, Is.False);
        }

        private static StepFailureClassification MakeClassification(JobStepFailureKind failureKind, int suggestedWaitTicks)
        {
            return StepFailureClassification.FromData(
                failureKind,
                JobFailureReason.StepFailed,
                StepResultStatus.Failed,
                JobActionKind.MoveToCell,
                phaseIndex: 0,
                actionIndex: 0,
                suggestedWaitTicks: suggestedWaitTicks,
                diagnostic: "qa-local-retry");
        }

        private static StepRecoveryPolicy MakePolicy(
            JobStepFailureKind failureKind,
            int maxRetryCount,
            int maxRecoveryTicks,
            params StepRecoveryStrategy[] strategies)
        {
            return new StepRecoveryPolicy(
                failureKind,
                strategies,
                maxRetryCount,
                maxRecoveryTicks,
                maxSearchRadius: 0,
                maxAlternativeTargets: 1,
                maxRepathAttempts: 0);
        }
    }
}
