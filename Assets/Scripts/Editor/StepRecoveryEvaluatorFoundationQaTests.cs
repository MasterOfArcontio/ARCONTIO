using Arcontio.Core;
using NUnit.Framework;
using System;
using System.IO;
using System.Linq;
using System.Reflection;
using UnityEditor;
using UnityEngine;

namespace Arcontio.Tests
{
    // =============================================================================
    // StepRecoveryEvaluatorFoundationQaTests
    // =============================================================================
    /// <summary>
    /// <para>
    /// Matrice QA EditMode per blindare lo skeleton no-op
    /// <c>StepRecoveryEvaluator</c>.
    /// </para>
    ///
    /// <para><b>v0.11c.05d - Recovery evaluator non produttivo</b></para>
    /// <para>
    /// Questi test proteggono l'invariante corrente: l'evaluator puo' ricevere una
    /// classificazione e una policy dichiarativa, ma non applica strategie, non
    /// produce recovery result, non trasforma fallimenti step e non espone accesso
    /// a sistemi runtime. La matrice non istanzia <c>JobExecutionSystem</c>, non
    /// avanza job, non emette command e non muta <c>World</c>.
    /// </para>
    ///
    /// <para><b>Struttura interna:</b></para>
    /// <list type="bullet">
    ///   <item><b>No-op matrix</b>: classificazione e policy non producono result.</item>
    ///   <item><b>Strategy guard</b>: strategie candidate non vengono applicate.</item>
    ///   <item><b>Runtime guard</b>: nessuna superficie World, command o execution system.</item>
    /// </list>
    /// </summary>
    public sealed class StepRecoveryEvaluatorFoundationQaTests
    {
        // =============================================================================
        // RunFromCommandLine
        // =============================================================================
        /// <summary>
        /// <para>
        /// Entry point diagnostico per batchmode nei casi in cui il runner XML Unity
        /// non produca risultati leggibili.
        /// </para>
        /// </summary>
        public static void RunFromCommandLine()
        {
            try
            {
                var tests = new StepRecoveryEvaluatorFoundationQaTests();

                tests.EvaluatorReturnsNoneWithPresentClassification();
                tests.EvaluatorReturnsNoneWithPolicyStrategies();
                tests.EvaluatorDoesNotApplyRetrySameAction();
                tests.EvaluatorDoesNotApplyWaitAndRetry();
                tests.EvaluatorDoesNotApplyFindEquivalentTarget();
                tests.EvaluatorDoesNotProduceEscalateToDecision();
                tests.EvaluatorDoesNotRequireWorldOrImplementCommand();
                tests.EvaluatorDoesNotModifyFailedStepResultStatus();
                tests.EvaluatorDoesNotReferenceExecutionSystems();

                Debug.Log("[StepRecoveryEvaluatorFoundationQaTests] PASS");
                EditorApplication.Exit(0);
            }
            catch (Exception ex)
            {
                Debug.LogError("[StepRecoveryEvaluatorFoundationQaTests] FAIL\n" + ex);
                EditorApplication.Exit(1);
            }
        }

        // =============================================================================
        // EvaluatorReturnsNoneWithPresentClassification
        // =============================================================================
        /// <summary>
        /// <para>
        /// Verifica che una classificazione dichiarata non produca alcun result di
        /// recovery.
        /// </para>
        /// </summary>
        [Test]
        public void EvaluatorReturnsNoneWithPresentClassification()
        {
            var result = Evaluate(MakeClassification(JobStepFailureKind.PathBlocked), StepRecoveryPolicy.Empty());

            AssertNone(result);
        }

        // =============================================================================
        // EvaluatorReturnsNoneWithPolicyStrategies
        // =============================================================================
        /// <summary>
        /// <para>
        /// Verifica che una policy popolata con strategie candidate resti puro dato
        /// dichiarativo.
        /// </para>
        /// </summary>
        [Test]
        public void EvaluatorReturnsNoneWithPolicyStrategies()
        {
            var policy = MakePolicy(
                StepRecoveryStrategy.RetrySameAction,
                StepRecoveryStrategy.WaitAndRetry,
                StepRecoveryStrategy.FindEquivalentTarget);

            var result = Evaluate(MakeClassification(JobStepFailureKind.TargetUnavailable), policy);

            AssertNone(result);
        }

        // =============================================================================
        // EvaluatorDoesNotApplyRetrySameAction
        // =============================================================================
        /// <summary>
        /// <para>
        /// Verifica che <c>RetrySameAction</c> non venga applicata dallo skeleton.
        /// </para>
        /// </summary>
        [Test]
        public void EvaluatorDoesNotApplyRetrySameAction()
        {
            var result = EvaluateWithSingleStrategy(StepRecoveryStrategy.RetrySameAction);

            AssertNone(result);
            Assert.That(result.Kind, Is.Not.EqualTo(JobRecoveryResultKind.RetryScheduled));
        }

        // =============================================================================
        // EvaluatorDoesNotApplyWaitAndRetry
        // =============================================================================
        /// <summary>
        /// <para>
        /// Verifica che <c>WaitAndRetry</c> non programmi attese o retry.
        /// </para>
        /// </summary>
        [Test]
        public void EvaluatorDoesNotApplyWaitAndRetry()
        {
            var result = EvaluateWithSingleStrategy(StepRecoveryStrategy.WaitAndRetry);

            AssertNone(result);
            Assert.That(result.Kind, Is.Not.EqualTo(JobRecoveryResultKind.RetryScheduled));
            Assert.That(result.SuggestedWaitTicks, Is.EqualTo(0));
        }

        // =============================================================================
        // EvaluatorDoesNotApplyFindEquivalentTarget
        // =============================================================================
        /// <summary>
        /// <para>
        /// Verifica che <c>FindEquivalentTarget</c> non sostituisca target e non
        /// produca result <c>TargetReplaced</c>.
        /// </para>
        /// </summary>
        [Test]
        public void EvaluatorDoesNotApplyFindEquivalentTarget()
        {
            var result = EvaluateWithSingleStrategy(StepRecoveryStrategy.FindEquivalentTarget);

            AssertNone(result);
            Assert.That(result.Kind, Is.Not.EqualTo(JobRecoveryResultKind.TargetReplaced));
        }

        // =============================================================================
        // EvaluatorDoesNotProduceEscalateToDecision
        // =============================================================================
        /// <summary>
        /// <para>
        /// Verifica che anche una policy che nomina <c>EscalateToDecision</c> non
        /// produca escalation decisionale.
        /// </para>
        /// </summary>
        [Test]
        public void EvaluatorDoesNotProduceEscalateToDecision()
        {
            var result = EvaluateWithSingleStrategy(StepRecoveryStrategy.EscalateToDecision);

            AssertNone(result);
            Assert.That(result.Kind, Is.Not.EqualTo(JobRecoveryResultKind.EscalateToDecision));
        }

        // =============================================================================
        // EvaluatorDoesNotRequireWorldOrImplementCommand
        // =============================================================================
        /// <summary>
        /// <para>
        /// Verifica che l'evaluator non sia un command e non esponga <c>World</c>
        /// nella superficie pubblica.
        /// </para>
        /// </summary>
        [Test]
        public void EvaluatorDoesNotRequireWorldOrImplementCommand()
        {
            var type = typeof(StepRecoveryEvaluator);

            Assert.That(typeof(ICommand).IsAssignableFrom(type), Is.False);
            Assert.That(PublicSurfaceReferences(type, typeof(World)), Is.False);
        }

        // =============================================================================
        // EvaluatorDoesNotModifyFailedStepResultStatus
        // =============================================================================
        /// <summary>
        /// <para>
        /// Verifica che valutare il no-op non trasformi un failure runtime in un
        /// result recovery o in uno status diverso.
        /// </para>
        /// </summary>
        [Test]
        public void EvaluatorDoesNotModifyFailedStepResultStatus()
        {
            var failed = StepResult.Failed(JobFailureReason.StepFailed, "qa-failed");
            var before = failed.Status;

            _ = Evaluate(
                MakeClassification(JobStepFailureKind.TargetInvalid),
                MakePolicy(StepRecoveryStrategy.FailJob));

            Assert.That(failed.Status, Is.EqualTo(before));
            Assert.That(failed.Status, Is.EqualTo(StepResultStatus.Failed));
            Assert.That(failed.Status, Is.Not.EqualTo((object)JobRecoveryResultKind.JobFailed));
        }

        // =============================================================================
        // EvaluatorDoesNotReferenceExecutionSystems
        // =============================================================================
        /// <summary>
        /// <para>
        /// Verifica che lo skeleton non esponga o nomini i sistemi di esecuzione job.
        /// Questo protegge l'assenza di cablaggio senza istanziare quei sistemi.
        /// </para>
        /// </summary>
        [Test]
        public void EvaluatorDoesNotReferenceExecutionSystems()
        {
            var type = typeof(StepRecoveryEvaluator);

            Assert.That(PublicSurfaceReferences(type, typeof(JobExecutionSystem)), Is.False);
            Assert.That(PublicSurfaceReferences(type, typeof(JobStateMachine)), Is.False);

            var source = File.ReadAllText("Assets/Scripts/Core/Jobs/StepRecoveryEvaluator.cs");
            Assert.That(source.Contains(nameof(JobExecutionSystem)), Is.False);
            Assert.That(source.Contains(nameof(JobStateMachine)), Is.False);
        }

        private static JobRecoveryResult Evaluate(
            StepFailureClassification classification,
            StepRecoveryPolicy policy)
        {
            return new StepRecoveryEvaluator().EvaluateNoOp(classification, policy);
        }

        private static JobRecoveryResult EvaluateWithSingleStrategy(StepRecoveryStrategy strategy)
        {
            return Evaluate(
                MakeClassification(JobStepFailureKind.PathBlocked),
                MakePolicy(strategy));
        }

        private static StepFailureClassification MakeClassification(JobStepFailureKind failureKind)
        {
            return StepFailureClassification.FromData(
                failureKind,
                JobFailureReason.StepFailed,
                StepResultStatus.Failed,
                JobActionKind.MoveToCell,
                phaseIndex: 0,
                actionIndex: 0,
                suggestedWaitTicks: 4,
                diagnostic: "qa-classification");
        }

        private static StepRecoveryPolicy MakePolicy(params StepRecoveryStrategy[] strategies)
        {
            return new StepRecoveryPolicy(
                JobStepFailureKind.PathBlocked,
                strategies,
                maxRetryCount: 3,
                maxRecoveryTicks: 8,
                maxSearchRadius: 2,
                maxAlternativeTargets: 2,
                maxRepathAttempts: 1);
        }

        private static void AssertNone(JobRecoveryResult result)
        {
            Assert.That(result.Kind, Is.EqualTo(JobRecoveryResultKind.None));
            Assert.That(result.HasDeclaredResult, Is.False);
            Assert.That(result.AppliedStrategy, Is.EqualTo(StepRecoveryStrategy.None));
            Assert.That(result.FailureKind, Is.EqualTo(JobStepFailureKind.None));
        }

        private static bool PublicSurfaceReferences(Type owner, Type forbiddenType)
        {
            const BindingFlags flags = BindingFlags.Public | BindingFlags.Instance | BindingFlags.Static | BindingFlags.DeclaredOnly;

            return owner.GetConstructors(flags)
                .Any(constructor => constructor.GetParameters().Any(parameter => ReferencesType(parameter.ParameterType, forbiddenType)))
                || owner.GetMethods(flags)
                    .Any(method => ReferencesType(method.ReturnType, forbiddenType)
                        || method.GetParameters().Any(parameter => ReferencesType(parameter.ParameterType, forbiddenType)))
                || owner.GetFields(flags).Any(field => ReferencesType(field.FieldType, forbiddenType))
                || owner.GetProperties(flags).Any(property => ReferencesType(property.PropertyType, forbiddenType));
        }

        private static bool ReferencesType(Type candidate, Type forbiddenType)
        {
            if (candidate == forbiddenType)
                return true;

            if (candidate.IsArray)
                return ReferencesType(candidate.GetElementType(), forbiddenType);

            return false;
        }
    }
}
