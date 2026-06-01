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
    // StepFailureClassifierQaTests
    // =============================================================================
    /// <summary>
    /// <para>
    /// QA EditMode per il classificatore data-only dei fallimenti step Job.
    /// </para>
    ///
    /// <para><b>v0.14b - Classificazione senza recovery produttiva</b></para>
    /// <para>
    /// Questi test verificano che <c>StepFailureClassifier</c> produca
    /// <c>StepFailureClassification</c> coerenti senza applicare recovery, senza
    /// emettere command, senza leggere <c>World</c> e senza richiedere
    /// <c>JobExecutionSystem</c>.
    /// </para>
    ///
    /// <para><b>Struttura interna:</b></para>
    /// <list type="bullet">
    ///   <item><b>Neutralita'</b>: Succeeded e Running non diventano failure classificati.</item>
    ///   <item><b>Mapping</b>: diagnostic e reason principali entrano nel vocabolario JobStepFailureKind.</item>
    ///   <item><b>Boundary</b>: il classifier resta puro e privo di superficie runtime.</item>
    /// </list>
    /// </summary>
    public sealed class StepFailureClassifierQaTests
    {
        public static void RunFromCommandLine()
        {
            try
            {
                var tests = new StepFailureClassifierQaTests();

                tests.SucceededAndRunningRemainUnclassified();
                tests.MissingTargetClassifiesAsTargetInvalid();
                tests.ConsumeUnavailableClassifiesAsResourceMissing();
                tests.BlockedMoveClassifiesAsPathBlocked();
                tests.ReservationDeniedClassifiesAsReservationConflict();
                tests.LockedDoorClassifiesAsDoorLocked();
                tests.CancelledStepClassifiesAsInterrupted();
                tests.DropOccupiedClassifiesAsOutputBlockedWhenReasonIsGeneric();
                tests.ClassifierDoesNotExposeWorldCommandOrRuntimeSystemSurface();

                Debug.Log("[StepFailureClassifierQaTests] PASS");
                EditorApplication.Exit(0);
            }
            catch (Exception ex)
            {
                Debug.LogError("[StepFailureClassifierQaTests] FAIL\n" + ex);
                EditorApplication.Exit(1);
            }
        }

        [Test]
        public void SucceededAndRunningRemainUnclassified()
        {
            var action = MoveAction();

            var succeeded = StepFailureClassifier.Classify(StepResult.Succeeded("ok"), action, 1, 2);
            var running = StepFailureClassifier.Classify(StepResult.Running("pending"), action, 1, 2);

            Assert.That(succeeded.HasClassification, Is.False);
            Assert.That(running.HasClassification, Is.False);
            Assert.That(succeeded.SourceStatus, Is.EqualTo(StepResultStatus.Succeeded));
            Assert.That(running.SourceStatus, Is.EqualTo(StepResultStatus.Running));
        }

        [Test]
        public void MissingTargetClassifiesAsTargetInvalid()
        {
            var classification = StepFailureClassifier.Classify(
                StepResult.Failed(JobFailureReason.MissingTarget, "ConsumeFoodObjectMissing"),
                ConsumeAction(),
                2,
                3);

            Assert.That(classification.HasClassification, Is.True);
            Assert.That(classification.FailureKind, Is.EqualTo(JobStepFailureKind.TargetInvalid));
            Assert.That(classification.ActionKind, Is.EqualTo(JobActionKind.Consume));
            Assert.That(classification.PhaseIndex, Is.EqualTo(2));
            Assert.That(classification.ActionIndex, Is.EqualTo(3));
        }

        [Test]
        public void ConsumeUnavailableClassifiesAsResourceMissing()
        {
            var classification = StepFailureClassifier.Classify(
                StepResult.Failed(JobFailureReason.MissingTarget, "ConsumeFoodUnavailable"),
                ConsumeAction(),
                0,
                1);

            Assert.That(classification.HasClassification, Is.True);
            Assert.That(classification.FailureKind, Is.EqualTo(JobStepFailureKind.ResourceMissing));
        }

        [Test]
        public void BlockedMoveClassifiesAsPathBlocked()
        {
            var classification = StepFailureClassifier.Classify(
                StepResult.Blocked(2, "TraversalTargetBlocked"),
                MoveAction(),
                0,
                0);

            Assert.That(classification.HasClassification, Is.True);
            Assert.That(classification.FailureKind, Is.EqualTo(JobStepFailureKind.PathBlocked));
            Assert.That(classification.SourceStatus, Is.EqualTo(StepResultStatus.Blocked));
            Assert.That(classification.SuggestedWaitTicks, Is.EqualTo(2));
        }

        [Test]
        public void ReservationDeniedClassifiesAsReservationConflict()
        {
            var classification = StepFailureClassifier.Classify(
                StepResult.Failed(JobFailureReason.ReservationDenied, "ReservationDenied"),
                ReserveAction(),
                0,
                0);

            Assert.That(classification.HasClassification, Is.True);
            Assert.That(classification.FailureKind, Is.EqualTo(JobStepFailureKind.ReservationConflict));
        }

        [Test]
        public void LockedDoorClassifiesAsDoorLocked()
        {
            var classification = StepFailureClassifier.Classify(
                StepResult.Failed(JobFailureReason.MovementFailed, "TraversalDoorLocked"),
                MoveAction(),
                0,
                0);

            Assert.That(classification.HasClassification, Is.True);
            Assert.That(classification.FailureKind, Is.EqualTo(JobStepFailureKind.DoorLocked));
        }

        [Test]
        public void CancelledStepClassifiesAsInterrupted()
        {
            var classification = StepFailureClassifier.Classify(
                StepResult.Failed(JobFailureReason.Cancelled, "RunningActionInterrupted"),
                WaitAction(),
                1,
                1);

            Assert.That(classification.HasClassification, Is.True);
            Assert.That(classification.FailureKind, Is.EqualTo(JobStepFailureKind.Interrupted));
        }

        [Test]
        public void DropOccupiedClassifiesAsOutputBlockedWhenReasonIsGeneric()
        {
            var classification = StepFailureClassifier.Classify(
                StepResult.Failed(JobFailureReason.StepFailed, "DropTargetOccupied"),
                DropAction(),
                2,
                0);

            Assert.That(classification.HasClassification, Is.True);
            Assert.That(classification.FailureKind, Is.EqualTo(JobStepFailureKind.OutputBlocked));
        }

        [Test]
        public void ClassifierDoesNotExposeWorldCommandOrRuntimeSystemSurface()
        {
            var type = typeof(StepFailureClassifier);
            const BindingFlags flags = BindingFlags.Public | BindingFlags.Instance | BindingFlags.Static | BindingFlags.DeclaredOnly;

            Assert.That(typeof(ICommand).IsAssignableFrom(type), Is.False);
            Assert.That(type.GetMethods(flags).Any(method => ReferencesForbiddenRuntimeType(method.ReturnType)), Is.False);
            Assert.That(type.GetMethods(flags).Any(method => method.GetParameters().Any(parameter => ReferencesForbiddenRuntimeType(parameter.ParameterType))), Is.False);
        }

        private static JobAction MoveAction()
        {
            return JobAction.MoveTo("move", new Vector2Int(1, 1), "move");
        }

        private static JobAction ConsumeAction()
        {
            return new JobAction(
                "consume",
                JobActionKind.Consume,
                "consume",
                true,
                new Vector2Int(1, 1),
                10,
                0,
                string.Empty);
        }

        private static JobAction ReserveAction()
        {
            return new JobAction(
                "reserve",
                JobActionKind.ReserveTarget,
                "reserve",
                true,
                new Vector2Int(1, 1),
                -1,
                0,
                string.Empty);
        }

        private static JobAction WaitAction()
        {
            return JobAction.Wait("wait", 2, "wait");
        }

        private static JobAction DropAction()
        {
            return new JobAction(
                "drop",
                JobActionKind.Drop,
                "drop",
                true,
                new Vector2Int(1, 1),
                20,
                0,
                string.Empty);
        }

        private static bool ReferencesForbiddenRuntimeType(Type type)
        {
            return type == typeof(World)
                || type == typeof(ICommand)
                || type == typeof(JobExecutionSystem)
                || type == typeof(JobStateMachine)
                || type == typeof(JobRuntimeState);
        }
    }
}
