using Arcontio.Core;
using NUnit.Framework;

namespace Arcontio.Tests
{
    // =============================================================================
    // JobPreemptionLadderQaTests
    // =============================================================================
    /// <summary>
    /// <para>
    /// Test QA EditMode per la ladder esplicita di preemption.
    /// </para>
    ///
    /// <para><b>Regole ordinate e osservabili</b></para>
    /// <para>
    /// Ogni test controlla una regola della ladder e la ragione diagnostica associata,
    /// cosi' il runtime potra' spiegare perche' un job e' stato interrotto o protetto.
    /// </para>
    ///
    /// <para><b>Struttura interna:</b></para>
    /// <list type="bullet">
    ///   <item><b>Emergency</b>: override della fase protetta.</item>
    ///   <item><b>Protected phase</b>: blocco dei non emergency.</item>
    ///   <item><b>Urgency margin</b>: tie-break a parita' di classe.</item>
    /// </list>
    /// </summary>
    public sealed class JobPreemptionLadderQaTests
    {
        // =============================================================================
        // EmergencyOverridesProtectedPhase
        // =============================================================================
        /// <summary>
        /// <para>
        /// Verifica che un job emergency possa sostituire un job corrente anche se la
        /// fase corrente non e' interrompibile.
        /// </para>
        ///
        /// <para><b>Eccezione critica controllata</b></para>
        /// <para>
        /// L'override emergency e' esplicito e diagnosticato: non nasce da score
        /// numerici nascosti.
        /// </para>
        ///
        /// <para><b>Struttura interna:</b></para>
        /// <list type="bullet">
        ///   <item><b>Current</b>: Important, fase protetta.</item>
        ///   <item><b>New</b>: Emergency.</item>
        ///   <item><b>Assert</b>: CancelCurrentForNew.</item>
        /// </list>
        /// </summary>
        [Test]
        public void EmergencyOverridesProtectedPhase()
        {
            // Arrange: fase protetta e nuova richiesta emergency.
            var ladder = new JobPreemptionLadder();
            var state = ActiveState("current");
            var current = MakeJob("current", JobPriorityClass.Important, 0.8f, false);
            var emergency = MakeJob("emergency", JobPriorityClass.Emergency, 0.9f, true);

            // Act: emergency supera la protezione.
            var result = ladder.Evaluate(state, current, emergency);

            // Assert: la sostituzione e' esplicita e motivata.
            Assert.That(result.Decision, Is.EqualTo(JobArbitrationDecision.CancelCurrentForNew));
            Assert.That(result.Reason, Is.EqualTo("EmergencyOverride"));
        }

        // =============================================================================
        // ProtectedPhaseBlocksNonEmergency
        // =============================================================================
        /// <summary>
        /// <para>
        /// Verifica che una fase protetta blocchi una nuova richiesta non emergency.
        /// </para>
        ///
        /// <para><b>Atomicita' locale del mini job</b></para>
        /// <para>
        /// Alcune fasi, come consume o difesa porta, non devono essere spezzate da
        /// lavori ordinari anche se piu' urgenti.
        /// </para>
        ///
        /// <para><b>Struttura interna:</b></para>
        /// <list type="bullet">
        ///   <item><b>Current</b>: fase non interrompibile.</item>
        ///   <item><b>New</b>: Critical ma non Emergency.</item>
        ///   <item><b>Assert</b>: KeepCurrent.</item>
        /// </list>
        /// </summary>
        [Test]
        public void ProtectedPhaseBlocksNonEmergency()
        {
            // Arrange: nuova richiesta forte ma non emergency.
            var ladder = new JobPreemptionLadder();
            var state = ActiveState("current");
            var current = MakeJob("current", JobPriorityClass.Normal, 0.4f, false);
            var critical = MakeJob("critical", JobPriorityClass.Critical, 1f, true);

            // Act: la fase protetta vince.
            var result = ladder.Evaluate(state, current, critical);

            // Assert: nessuna interruzione.
            Assert.That(result.Decision, Is.EqualTo(JobArbitrationDecision.KeepCurrent));
            Assert.That(result.Reason, Is.EqualTo("ProtectedPhase"));
        }

        // =============================================================================
        // SamePriorityRequiresUrgencyMargin
        // =============================================================================
        /// <summary>
        /// <para>
        /// Verifica che a parita' di priorita' serva un margine di urgenza per
        /// sospendere il job corrente.
        /// </para>
        ///
        /// <para><b>Anti oscillazione</b></para>
        /// <para>
        /// Senza margine, job simili potrebbero alternarsi ogni pochi tick. La ladder
        /// richiede un vantaggio esplicito.
        /// </para>
        ///
        /// <para><b>Struttura interna:</b></para>
        /// <list type="bullet">
        ///   <item><b>Current</b>: Normal 0.40.</item>
        ///   <item><b>New</b>: Normal 0.65.</item>
        ///   <item><b>Assert</b>: SuspendCurrentForNew.</item>
        /// </list>
        /// </summary>
        [Test]
        public void SamePriorityRequiresUrgencyMargin()
        {
            // Arrange: stessa classe, vantaggio oltre margine.
            var ladder = new JobPreemptionLadder();
            var state = ActiveState("current");
            var current = MakeJob("current", JobPriorityClass.Normal, 0.40f, true);
            var newer = MakeJob("new", JobPriorityClass.Normal, 0.65f, true);

            // Act: l'urgenza supera il margine anti oscillazione.
            var result = ladder.Evaluate(state, current, newer);

            // Assert: la sospensione e' ammessa.
            Assert.That(result.Decision, Is.EqualTo(JobArbitrationDecision.SuspendCurrentForNew));
            Assert.That(result.Reason, Is.EqualTo("UrgencyMarginWins"));
        }

        private static NpcJobState ActiveState(string jobId)
        {
            // Stato helper isolato dalla simulazione runtime.
            var state = NpcJobState.Empty();
            state.AssignJob(jobId, 0);
            return state;
        }

        private static Job MakeJob(string jobId, JobPriorityClass priorityClass, float urgency01, bool interruptible)
        {
            // Job minimale con singola fase, sufficiente a testare la ladder.
            var request = JobRequest.WithoutTarget("req-" + jobId, 1, DecisionIntentKind.WaitAndObserve, priorityClass, urgency01, 0, jobId);
            var plan = new JobPlan("plan-" + jobId, new[] { new JobPhase("phase", JobPhaseKind.Execute, jobId, 1, interruptible) });
            return new Job(jobId, request, plan);
        }
    }
}
