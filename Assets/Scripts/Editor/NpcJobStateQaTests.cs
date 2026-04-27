using Arcontio.Core;
using NUnit.Framework;

namespace Arcontio.Tests
{
    // =============================================================================
    // NpcJobStateQaTests
    // =============================================================================
    /// <summary>
    /// <para>
    /// Test QA EditMode per lo stato job persistente associato a un singolo NPC.
    /// </para>
    ///
    /// <para><b>Cursore operativo per-NPC</b></para>
    /// <para>
    /// Lo stato non contiene il job completo e non legge il mondo. Tiene solo gli
    /// indici necessari a riprendere l'esecuzione nel punto corretto.
    /// </para>
    ///
    /// <para><b>Struttura interna:</b></para>
    /// <list type="bullet">
    ///   <item><b>Assign</b>: apertura di un job corrente.</item>
    ///   <item><b>Advance</b>: avanzamento action e fase.</item>
    ///   <item><b>Wait/Suspend</b>: stati transitori senza side effect globali.</item>
    /// </list>
    /// </summary>
    public sealed class NpcJobStateQaTests
    {
        // =============================================================================
        // NpcJobStateTracksHierarchicalCursor
        // =============================================================================
        /// <summary>
        /// <para>
        /// Verifica che lo stato per-NPC mantenga separati indice fase e indice step.
        /// </para>
        ///
        /// <para><b>Gerarchia persistente</b></para>
        /// <para>
        /// Un job complesso non puo' essere rappresentato da un singolo contatore
        /// piatto. Il test protegge il cursore gerarchico fase -> action.
        /// </para>
        ///
        /// <para><b>Struttura interna:</b></para>
        /// <list type="bullet">
        ///   <item><b>AssignJob</b>: azzera cursori e fallimenti.</item>
        ///   <item><b>AdvanceAction</b>: avanza solo lo step corrente.</item>
        ///   <item><b>AdvancePhase</b>: avanza fase e riparte dallo step zero.</item>
        /// </list>
        /// </summary>
        [Test]
        public void NpcJobStateTracksHierarchicalCursor()
        {
            // Arrange: lo stato nasce vuoto e non fa riferimento ad alcuno store globale.
            var state = NpcJobState.Empty();

            // Act: assegniamo un job e simuliamo avanzamenti locali del cursore.
            state.AssignJob("job-eat-01", 20);
            state.AdvanceAction();
            state.AdvanceAction();
            state.AdvancePhase();

            // Assert: fase e action restano concetti separati.
            Assert.That(state.HasActiveJob, Is.True);
            Assert.That(state.ActiveJobId, Is.EqualTo("job-eat-01"));
            Assert.That(state.ActivePhaseIndex, Is.EqualTo(1));
            Assert.That(state.ActiveActionIndex, Is.EqualTo(0));
            Assert.That(state.LastFailureReason, Is.EqualTo(JobFailureReason.None));
        }

        // =============================================================================
        // NpcJobStateHandlesWaitClearAndSuspend
        // =============================================================================
        /// <summary>
        /// <para>
        /// Verifica attesa, chiusura diagnostica e sospensione recuperabile del job.
        /// </para>
        ///
        /// <para><b>Preemption preparata ma non ancora applicata</b></para>
        /// <para>
        /// Lo stato registra un job sospeso senza decidere quale job debba sostituirlo.
        /// La policy resta negli step successivi.
        /// </para>
        ///
        /// <para><b>Struttura interna:</b></para>
        /// <list type="bullet">
        ///   <item><b>WaitUntilTick</b>: blocco temporaneo locale.</item>
        ///   <item><b>Clear</b>: chiusura con motivo.</item>
        ///   <item><b>SuspendActiveJob</b>: parcheggio dell'id corrente.</item>
        /// </list>
        /// </summary>
        [Test]
        public void NpcJobStateHandlesWaitClearAndSuspend()
        {
            // Arrange: assegniamo un job e mettiamo lo step in pausa.
            var waiting = NpcJobState.Empty();
            waiting.AssignJob("job-wait-01", 3);
            waiting.SetWaitingUntil(10);

            // Act: verifichiamo attesa e chiusura diagnostica.
            var isWaitingAtNine = waiting.IsWaitingAt(9);
            var isWaitingAtTen = waiting.IsWaitingAt(10);
            waiting.Clear(JobFailureReason.MovementFailed);

            // Assert: l'attesa scade al tick limite e il motivo resta leggibile.
            Assert.That(isWaitingAtNine, Is.True);
            Assert.That(isWaitingAtTen, Is.False);
            Assert.That(waiting.HasActiveJob, Is.False);
            Assert.That(waiting.LastFailureReason, Is.EqualTo(JobFailureReason.MovementFailed));

            // Arrange/Act: una seconda istanza copre la sospensione recuperabile.
            var suspended = NpcJobState.Empty();
            suspended.AssignJob("job-work-01", 11);
            suspended.SuspendActiveJob();

            // Assert: il job corrente viene liberato ma il riferimento sospeso resta.
            Assert.That(suspended.HasActiveJob, Is.False);
            Assert.That(suspended.ActiveJobId, Is.Empty);
            Assert.That(suspended.SuspendedJobId, Is.EqualTo("job-work-01"));
        }
    }
}
