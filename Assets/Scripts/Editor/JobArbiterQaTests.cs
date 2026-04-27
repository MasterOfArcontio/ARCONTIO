using Arcontio.Core;
using NUnit.Framework;

namespace Arcontio.Tests
{
    // =============================================================================
    // JobArbiterQaTests
    // =============================================================================
    /// <summary>
    /// <para>
    /// Test QA EditMode per l'arbitro base del Job System.
    /// </para>
    ///
    /// <para><b>Preemption senza mutazione diretta</b></para>
    /// <para>
    /// I test verificano solo la decisione restituita dall'arbitro. Lo stato NPC e i
    /// job non vengono modificati dal servizio, mantenendo chiara la separazione tra
    /// policy e applicazione.
    /// </para>
    ///
    /// <para><b>Struttura interna:</b></para>
    /// <list type="bullet">
    ///   <item><b>Idle</b>: accetta job nuovo.</item>
    ///   <item><b>Priority</b>: sospende il job corrente se la classe e' superiore.</item>
    ///   <item><b>Interruptibility</b>: protegge fasi non interrompibili.</item>
    /// </list>
    /// </summary>
    public sealed class JobArbiterQaTests
    {
        // =============================================================================
        // ArbiterAcceptsNewJobWhenNpcIsIdle
        // =============================================================================
        /// <summary>
        /// <para>
        /// Verifica che un NPC senza job attivo accetti la nuova richiesta.
        /// </para>
        ///
        /// <para><b>Assegnazione base</b></para>
        /// <para>
        /// In assenza di competizione non serve preemption: l'arbitro deve solo
        /// riconoscere che il job nuovo puo' diventare attivo.
        /// </para>
        ///
        /// <para><b>Struttura interna:</b></para>
        /// <list type="bullet">
        ///   <item><b>NpcJobState</b>: vuoto.</item>
        ///   <item><b>NewJob</b>: job valido con piano minimale.</item>
        ///   <item><b>Assert</b>: decisione AcceptNew.</item>
        /// </list>
        /// </summary>
        [Test]
        public void ArbiterAcceptsNewJobWhenNpcIsIdle()
        {
            // Arrange: nessun job attivo significa nessun conflitto da risolvere.
            var arbiter = new JobArbiter();
            var state = NpcJobState.Empty();
            var newJob = MakeJob("new", JobPriorityClass.Normal, 0.4f, true);

            // Act: l'arbitro valuta dati puri e non muta lo stato.
            var result = arbiter.Evaluate(state, null, newJob);

            // Assert: il job nuovo viene accettato come attivo.
            Assert.That(result.Decision, Is.EqualTo(JobArbitrationDecision.AcceptNew));
            Assert.That(result.AcceptedJobId, Is.EqualTo("new"));
            Assert.That(result.Reason, Is.EqualTo("NpcIdle"));
            Assert.That(state.HasActiveJob, Is.False);
        }

        // =============================================================================
        // ArbiterSuspendsInterruptibleCurrentJobForHigherPriority
        // =============================================================================
        /// <summary>
        /// <para>
        /// Verifica che una richiesta con classe superiore sospenda il job corrente
        /// se la fase attiva e' interrompibile.
        /// </para>
        ///
        /// <para><b>Preemption base</b></para>
        /// <para>
        /// La prima regola stabile e' discreta: Critical supera Normal. Questo evita
        /// che piccoli score continui producano cambi lavoro instabili.
        /// </para>
        ///
        /// <para><b>Struttura interna:</b></para>
        /// <list type="bullet">
        ///   <item><b>Current</b>: job normal interrompibile.</item>
        ///   <item><b>New</b>: job critical.</item>
        ///   <item><b>Assert</b>: decisione SuspendCurrentForNew.</item>
        /// </list>
        /// </summary>
        [Test]
        public void ArbiterSuspendsInterruptibleCurrentJobForHigherPriority()
        {
            // Arrange: lo stato dichiara un job attivo, il job corrente e' interrompibile.
            var arbiter = new JobArbiter();
            var state = NpcJobState.Empty();
            state.AssignJob("current", 1);
            var currentJob = MakeJob("current", JobPriorityClass.Normal, 0.5f, true);
            var newJob = MakeJob("critical", JobPriorityClass.Critical, 0.7f, true);

            // Act: la classe superiore deve prevalere.
            var result = arbiter.Evaluate(state, currentJob, newJob);

            // Assert: l'arbitro chiede sospensione, non modifica direttamente lo stato.
            Assert.That(result.Decision, Is.EqualTo(JobArbitrationDecision.SuspendCurrentForNew));
            Assert.That(result.AcceptedJobId, Is.EqualTo("critical"));
            Assert.That(result.Reason, Is.EqualTo("HigherPriorityClass"));
            Assert.That(state.ActiveJobId, Is.EqualTo("current"));
        }

        // =============================================================================
        // ArbiterKeepsCurrentJobWhenPhaseIsNotInterruptible
        // =============================================================================
        /// <summary>
        /// <para>
        /// Verifica che una fase non interrompibile protegga il job corrente nella
        /// policy base.
        /// </para>
        ///
        /// <para><b>Confine tra base policy e ladder futura</b></para>
        /// <para>
        /// La ladder finale potra' introdurre eccezioni emergency. In questo step
        /// manteniamo una regola semplice e conservativa.
        /// </para>
        ///
        /// <para><b>Struttura interna:</b></para>
        /// <list type="bullet">
        ///   <item><b>Current phase</b>: IsInterruptible false.</item>
        ///   <item><b>New job</b>: classe superiore.</item>
        ///   <item><b>Assert</b>: KeepCurrent per protezione fase.</item>
        /// </list>
        /// </summary>
        [Test]
        public void ArbiterKeepsCurrentJobWhenPhaseIsNotInterruptible()
        {
            // Arrange: il job corrente sta dentro una fase atomica protetta.
            var arbiter = new JobArbiter();
            var state = NpcJobState.Empty();
            state.AssignJob("current", 1);
            var currentJob = MakeJob("current", JobPriorityClass.Normal, 0.5f, false);
            var newJob = MakeJob("critical", JobPriorityClass.Critical, 0.9f, true);

            // Act: la policy base rispetta l'interruptibility della fase.
            var result = arbiter.Evaluate(state, currentJob, newJob);

            // Assert: nessuna preemption viene richiesta.
            Assert.That(result.Decision, Is.EqualTo(JobArbitrationDecision.KeepCurrent));
            Assert.That(result.AcceptedJobId, Is.EqualTo("current"));
            Assert.That(result.Reason, Is.EqualTo("CurrentPhaseNotInterruptible"));
        }

        private static Job MakeJob(string jobId, JobPriorityClass priorityClass, float urgency01, bool interruptiblePhase)
        {
            // Factory locale: costruisce un job con un'unica fase, sufficiente per
            // isolare la policy dell'arbitro senza dipendere da planner o World.
            var request = JobRequest.WithoutTarget(
                "req-" + jobId,
                1,
                DecisionIntentKind.WaitAndObserve,
                priorityClass,
                urgency01,
                1,
                jobId);
            var plan = new JobPlan(
                "plan-" + jobId,
                new[] { new JobPhase("phase-" + jobId, JobPhaseKind.Execute, jobId, 1, interruptiblePhase) });
            return new Job(jobId, request, plan);
        }
    }
}
