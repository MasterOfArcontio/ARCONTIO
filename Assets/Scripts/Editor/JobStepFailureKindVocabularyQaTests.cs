using System;
using Arcontio.Core;
using NUnit.Framework;

namespace Arcontio.Tests
{
    // =============================================================================
    // JobStepFailureKindVocabularyQaTests
    // =============================================================================
    /// <summary>
    /// <para>
    /// Test QA EditMode per il vocabolario passivo dei fallimenti locali di step.
    /// </para>
    ///
    /// <para><b>v0.11c.04b - Lessico senza policy runtime</b></para>
    /// <para>
    /// Questi test proteggono la natura data-only del nuovo enum: coprono la
    /// presenza dei nomi richiesti senza introdurre mapping produttivi verso
    /// <c>JobFailureReason</c>, recovery strategy, retry policy o command emission.
    /// </para>
    ///
    /// <para><b>Struttura interna:</b></para>
    /// <list type="bullet">
    ///   <item><b>Vocabulary coverage</b>: tutti i nomi candidati sono rappresentati.</item>
    ///   <item><b>No mapping</b>: il tipo resta distinto dai failure reason runtime esistenti.</item>
    ///   <item><b>No command</b>: nessun valore enum rappresenta un <c>ICommand</c>.</item>
    /// </list>
    /// </summary>
    public sealed class JobStepFailureKindVocabularyQaTests
    {
        // =============================================================================
        // JobStepFailureKindContainsLocalRecoveryVocabulary
        // =============================================================================
        /// <summary>
        /// <para>
        /// Verifica che il vocabolario contenga solo le categorie locali richieste
        /// per i futuri modelli passivi di recovery.
        /// </para>
        /// </summary>
        [Test]
        public void JobStepFailureKindContainsLocalRecoveryVocabulary()
        {
            Assert.That(Enum.IsDefined(typeof(JobStepFailureKind), JobStepFailureKind.TargetInvalid), Is.True);
            Assert.That(Enum.IsDefined(typeof(JobStepFailureKind), JobStepFailureKind.TargetUnavailable), Is.True);
            Assert.That(Enum.IsDefined(typeof(JobStepFailureKind), JobStepFailureKind.PathBlocked), Is.True);
            Assert.That(Enum.IsDefined(typeof(JobStepFailureKind), JobStepFailureKind.AccessDenied), Is.True);
            Assert.That(Enum.IsDefined(typeof(JobStepFailureKind), JobStepFailureKind.ReservationConflict), Is.True);
            Assert.That(Enum.IsDefined(typeof(JobStepFailureKind), JobStepFailureKind.ResourceMissing), Is.True);
            Assert.That(Enum.IsDefined(typeof(JobStepFailureKind), JobStepFailureKind.ActorInventoryFull), Is.True);
            Assert.That(Enum.IsDefined(typeof(JobStepFailureKind), JobStepFailureKind.ActorIncapacitated), Is.True);
            Assert.That(Enum.IsDefined(typeof(JobStepFailureKind), JobStepFailureKind.Timeout), Is.True);
            Assert.That(Enum.IsDefined(typeof(JobStepFailureKind), JobStepFailureKind.Interrupted), Is.True);
            Assert.That(Enum.IsDefined(typeof(JobStepFailureKind), JobStepFailureKind.InsufficientInformation), Is.True);
            Assert.That(Enum.IsDefined(typeof(JobStepFailureKind), JobStepFailureKind.OutputBlocked), Is.True);
        }

        // =============================================================================
        // JobStepFailureKindNoneIsNotAConcreteFailure
        // =============================================================================
        /// <summary>
        /// <para>
        /// Verifica che il valore neutro resti distinto dai fallimenti reali senza
        /// attribuire recuperabilita' o policy produttiva ad alcuna categoria.
        /// </para>
        /// </summary>
        [Test]
        public void JobStepFailureKindNoneIsNotAConcreteFailure()
        {
            Assert.That(JobStepFailureKind.None, Is.EqualTo((JobStepFailureKind)0));
            Assert.That(JobStepFailureKind.None, Is.Not.EqualTo(JobStepFailureKind.TargetInvalid));
            Assert.That(JobStepFailureKind.None, Is.Not.EqualTo(JobStepFailureKind.Timeout));
            Assert.That(typeof(JobStepFailureKind), Is.Not.EqualTo(typeof(JobFailureReason)));
            Assert.That((object)JobStepFailureKind.ReservationConflict, Is.Not.AssignableTo<ICommand>());
        }
    }
}
