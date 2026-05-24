using Arcontio.Core;
using NUnit.Framework;
using System;
using System.Linq;
using System.Reflection;
using UnityEditor;
using UnityEngine;

namespace Arcontio.Tests
{
    // =============================================================================
    // StepRecoveryEvaluatorNoOpQaTests
    // =============================================================================
    /// <summary>
    /// <para>
    /// Test QA EditMode per lo skeleton no-op <c>StepRecoveryEvaluator</c>.
    /// </para>
    ///
    /// <para><b>v0.11c.05c - Evaluator no-op senza recovery runtime</b></para>
    /// <para>
    /// Questi test verificano che l'evaluator restituisca sempre
    /// <c>JobRecoveryResultKind.None</c>, anche con classificazione e policy popolate.
    /// Non istanziano <c>JobExecutionSystem</c>, non avanzano job, non emettono
    /// command e non mutano <c>World</c>.
    /// </para>
    ///
    /// <para><b>Struttura interna:</b></para>
    /// <list type="bullet">
    ///   <item><b>No-op result</b>: nessun result recovery dichiarato.</item>
    ///   <item><b>No strategy application</b>: policy e strategie vengono ignorate.</item>
    ///   <item><b>No World surface</b>: l'evaluator non richiede accesso a World.</item>
    /// </list>
    /// </summary>
    public sealed class StepRecoveryEvaluatorNoOpQaTests
    {
        public static void RunFromCommandLine()
        {
            try
            {
                var tests = new StepRecoveryEvaluatorNoOpQaTests();

                tests.EvaluateNoOpReturnsNoneForEmptyInputs();
                tests.EvaluateNoOpReturnsNoneEvenWithClassificationAndStrategies();
                tests.EvaluateNoOpDoesNotApplyStrategies();
                tests.StepRecoveryEvaluatorDoesNotRequireWorldOrRuntimeSystems();

                Debug.Log("[StepRecoveryEvaluatorNoOpQaTests] PASS");
                EditorApplication.Exit(0);
            }
            catch (Exception ex)
            {
                Debug.LogError("[StepRecoveryEvaluatorNoOpQaTests] FAIL\n" + ex);
                EditorApplication.Exit(1);
            }
        }

        [Test]
        public void EvaluateNoOpReturnsNoneForEmptyInputs()
        {
            var evaluator = new StepRecoveryEvaluator();

            var result = evaluator.EvaluateNoOp(
                StepFailureClassification.None(),
                StepRecoveryPolicy.Empty());

            Assert.That(result.Kind, Is.EqualTo(JobRecoveryResultKind.None));
            Assert.That(result.HasDeclaredResult, Is.False);
            Assert.That(result.AppliedStrategy, Is.EqualTo(StepRecoveryStrategy.None));
            Assert.That(result.FailureKind, Is.EqualTo(JobStepFailureKind.None));
        }

        [Test]
        public void EvaluateNoOpReturnsNoneEvenWithClassificationAndStrategies()
        {
            var evaluator = new StepRecoveryEvaluator();
            var classification = StepFailureClassification.FromData(
                JobStepFailureKind.PathBlocked,
                JobFailureReason.MovementFailed,
                StepResultStatus.Failed,
                JobActionKind.MoveToCell,
                phaseIndex: 1,
                actionIndex: 2,
                suggestedWaitTicks: 3,
                diagnostic: "TraversalTargetBlocked");
            var policy = new StepRecoveryPolicy(
                JobStepFailureKind.PathBlocked,
                new[] { StepRecoveryStrategy.Repath, StepRecoveryStrategy.FindAlternateCell, StepRecoveryStrategy.EscalateToDecision },
                maxRetryCount: 2,
                maxRecoveryTicks: 5,
                maxSearchRadius: 1,
                maxAlternativeTargets: 1,
                maxRepathAttempts: 1);

            var result = evaluator.EvaluateNoOp(classification, policy);

            Assert.That(classification.HasClassification, Is.True);
            Assert.That(policy.HasDeclaredData, Is.True);
            Assert.That(result.Kind, Is.EqualTo(JobRecoveryResultKind.None));
            Assert.That(result.HasDeclaredResult, Is.False);
        }

        [Test]
        public void EvaluateNoOpDoesNotApplyStrategies()
        {
            var evaluator = new StepRecoveryEvaluator();
            var classification = StepFailureClassification.FromData(
                JobStepFailureKind.ReservationConflict,
                JobFailureReason.ReservationDenied,
                StepResultStatus.Blocked,
                JobActionKind.ReserveTarget,
                phaseIndex: 0,
                actionIndex: 1,
                suggestedWaitTicks: 5,
                diagnostic: "ReservationDenied");
            var policy = new StepRecoveryPolicy(
                JobStepFailureKind.ReservationConflict,
                new[] { StepRecoveryStrategy.WaitAndRetry, StepRecoveryStrategy.RetrySameAction },
                maxRetryCount: 3,
                maxRecoveryTicks: 9,
                maxSearchRadius: 0,
                maxAlternativeTargets: 0,
                maxRepathAttempts: 0);

            var result = evaluator.EvaluateNoOp(classification, policy);

            Assert.That(result.Kind, Is.Not.EqualTo(JobRecoveryResultKind.RetryScheduled));
            Assert.That(result.Kind, Is.Not.EqualTo(JobRecoveryResultKind.Recovered));
            Assert.That(result.Kind, Is.Not.EqualTo(JobRecoveryResultKind.EscalateToDecision));
            Assert.That(result.AppliedStrategy, Is.EqualTo(StepRecoveryStrategy.None));
            Assert.That(result.FailureKind, Is.EqualTo(JobStepFailureKind.None));
        }

        [Test]
        public void StepRecoveryEvaluatorDoesNotRequireWorldOrRuntimeSystems()
        {
            var type = typeof(StepRecoveryEvaluator);
            const BindingFlags flags = BindingFlags.Public | BindingFlags.Instance | BindingFlags.Static | BindingFlags.DeclaredOnly;

            Assert.That(typeof(ICommand).IsAssignableFrom(type), Is.False);
            Assert.That(type.GetConstructors(flags).Any(ReferencesWorldOrRuntimeSystem), Is.False);
            Assert.That(type.GetMethods(flags).Any(MethodReferencesWorldOrRuntimeSystem), Is.False);
        }

        private static bool ReferencesWorldOrRuntimeSystem(ConstructorInfo constructor)
        {
            return constructor
                .GetParameters()
                .Any(parameter => IsWorldOrRuntimeSystem(parameter.ParameterType));
        }

        private static bool MethodReferencesWorldOrRuntimeSystem(MethodInfo method)
        {
            if (IsWorldOrRuntimeSystem(method.ReturnType))
                return true;

            return method
                .GetParameters()
                .Any(parameter => IsWorldOrRuntimeSystem(parameter.ParameterType));
        }

        private static bool IsWorldOrRuntimeSystem(Type type)
        {
            return type == typeof(World)
                || type == typeof(JobExecutionSystem)
                || type == typeof(JobStateMachine)
                || type == typeof(JobRuntimeState);
        }
    }
}
