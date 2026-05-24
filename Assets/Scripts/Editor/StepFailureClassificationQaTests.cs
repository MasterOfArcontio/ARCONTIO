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
    // StepFailureClassificationQaTests
    // =============================================================================
    /// <summary>
    /// <para>
    /// Test QA EditMode per il DTO passivo <c>StepFailureClassification</c>.
    /// </para>
    ///
    /// <para><b>v0.11c.05b - Modello classificazione senza runtime mapping</b></para>
    /// <para>
    /// Questi test verificano che il modello conservi dati sorgente e resti distinto
    /// da <c>StepResult</c>, <c>JobRecoveryResult</c> e dai sistemi runtime. Non
    /// istanziano <c>JobExecutionSystem</c>, non avanzano job, non emettono command e
    /// non mutano <c>World</c>.
    /// </para>
    ///
    /// <para><b>Struttura interna:</b></para>
    /// <list type="bullet">
    ///   <item><b>Passive data</b>: conserva status, reason, action kind, diagnostic e wait ticks.</item>
    ///   <item><b>None semantics</b>: assenza di classificazione, non recovery policy.</item>
    ///   <item><b>Boundary separation</b>: nessun mapping automatico da StepResult o verso JobRecoveryResult.</item>
    /// </list>
    /// </summary>
    public sealed class StepFailureClassificationQaTests
    {
        public static void RunFromCommandLine()
        {
            try
            {
                var tests = new StepFailureClassificationQaTests();

                tests.StepFailureClassificationCanCarryPassiveSourceData();
                tests.StepFailureClassificationNoneDoesNotImplyRecoverabilityOrTerminalFailure();
                tests.StepFailureClassificationRemainsDistinctFromRuntimeAndRecoveryResults();
                tests.StepFailureClassificationDoesNotExposeRuntimeMappingSurface();

                Debug.Log("[StepFailureClassificationQaTests] PASS");
                EditorApplication.Exit(0);
            }
            catch (Exception ex)
            {
                Debug.LogError("[StepFailureClassificationQaTests] FAIL\n" + ex);
                EditorApplication.Exit(1);
            }
        }

        [Test]
        public void StepFailureClassificationCanCarryPassiveSourceData()
        {
            var classification = StepFailureClassification.FromData(
                JobStepFailureKind.PathBlocked,
                JobFailureReason.MovementFailed,
                StepResultStatus.Failed,
                JobActionKind.MoveToCell,
                phaseIndex: 2,
                actionIndex: 3,
                suggestedWaitTicks: 4,
                diagnostic: "TraversalTargetBlocked");

            Assert.That(classification.HasClassification, Is.True);
            Assert.That(classification.FailureKind, Is.EqualTo(JobStepFailureKind.PathBlocked));
            Assert.That(classification.SourceFailureReason, Is.EqualTo(JobFailureReason.MovementFailed));
            Assert.That(classification.SourceStatus, Is.EqualTo(StepResultStatus.Failed));
            Assert.That(classification.ActionKind, Is.EqualTo(JobActionKind.MoveToCell));
            Assert.That(classification.PhaseIndex, Is.EqualTo(2));
            Assert.That(classification.ActionIndex, Is.EqualTo(3));
            Assert.That(classification.SuggestedWaitTicks, Is.EqualTo(4));
            Assert.That(classification.Diagnostic, Is.EqualTo("TraversalTargetBlocked"));
        }

        [Test]
        public void StepFailureClassificationNoneDoesNotImplyRecoverabilityOrTerminalFailure()
        {
            var classification = StepFailureClassification.None(
                StepResultStatus.Blocked,
                JobFailureReason.ReservationDenied,
                JobActionKind.ReserveTarget,
                phaseIndex: 1,
                actionIndex: 2,
                suggestedWaitTicks: 5,
                diagnostic: "ReservationDenied");

            Assert.That(classification.HasClassification, Is.False);
            Assert.That(classification.FailureKind, Is.EqualTo(JobStepFailureKind.None));
            Assert.That(classification.SourceStatus, Is.EqualTo(StepResultStatus.Blocked));
            Assert.That(classification.SourceFailureReason, Is.EqualTo(JobFailureReason.ReservationDenied));
            Assert.That(classification.ActionKind, Is.EqualTo(JobActionKind.ReserveTarget));
            Assert.That(classification.SuggestedWaitTicks, Is.EqualTo(5));
            Assert.That(classification.Diagnostic, Is.EqualTo("ReservationDenied"));
        }

        [Test]
        public void StepFailureClassificationRemainsDistinctFromRuntimeAndRecoveryResults()
        {
            Assert.That(typeof(StepFailureClassification), Is.Not.EqualTo(typeof(StepResult)));
            Assert.That(typeof(StepFailureClassification), Is.Not.EqualTo(typeof(StepResultStatus)));
            Assert.That(typeof(StepFailureClassification), Is.Not.EqualTo(typeof(JobRecoveryResult)));
            Assert.That(typeof(StepFailureClassification), Is.Not.EqualTo(typeof(JobRecoveryResultKind)));
            Assert.That(typeof(StepFailureClassification), Is.Not.EqualTo(typeof(StepRecoveryPolicy)));
        }

        [Test]
        public void StepFailureClassificationDoesNotExposeRuntimeMappingSurface()
        {
            var type = typeof(StepFailureClassification);
            const BindingFlags flags = BindingFlags.Public | BindingFlags.Instance | BindingFlags.Static | BindingFlags.DeclaredOnly;

            Assert.That(typeof(ICommand).IsAssignableFrom(type), Is.False);
            Assert.That(type.GetMethods(flags).Any(method => method.ReturnType == typeof(JobRecoveryResult)), Is.False);
            Assert.That(type.GetMethods(flags).Any(method => method.ReturnType == typeof(StepResult)), Is.False);
            Assert.That(type.GetMethods(flags).Any(method => method.GetParameters().Any(parameter => parameter.ParameterType == typeof(StepResult))), Is.False);
            Assert.That(type.GetMembers(flags).Any(MemberReferencesWorld), Is.False);
        }

        private static bool MemberReferencesWorld(MemberInfo member)
        {
            if (member is FieldInfo field)
                return ReferencesWorld(field.FieldType);

            if (member is PropertyInfo property)
                return ReferencesWorld(property.PropertyType);

            if (member is MethodInfo method)
                return ReferencesWorld(method.ReturnType)
                    || method.GetParameters().Any(parameter => ReferencesWorld(parameter.ParameterType));

            if (member is ConstructorInfo constructor)
                return constructor.GetParameters().Any(parameter => ReferencesWorld(parameter.ParameterType));

            return false;
        }

        private static bool ReferencesWorld(Type type)
        {
            if (type == typeof(World))
                return true;

            return type.IsArray && ReferencesWorld(type.GetElementType());
        }
    }
}
