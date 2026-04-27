using Arcontio.Core;
using NUnit.Framework;

namespace Arcontio.Tests
{
    // =============================================================================
    // JobStateMachineQaTests
    // =============================================================================
    /// <summary>
    /// <para>
    /// Test QA EditMode per la macchina a stati minimale del Job System.
    /// </para>
    ///
    /// <para><b>Avanzamento gerarchico fase/step</b></para>
    /// <para>
    /// I test verificano che un risultato di step venga tradotto in avanzamento del
    /// cursore senza eseguire direttamente sistemi di pathfinding, inventario o needs.
    /// </para>
    ///
    /// <para><b>Struttura interna:</b></para>
    /// <list type="bullet">
    ///   <item><b>Succeeded</b>: avanza action, fase e completamento job.</item>
    ///   <item><b>Waiting</b>: rispetta un gate temporale.</item>
    ///   <item><b>Failed</b>: chiude job e stato NPC con motivo.</item>
    /// </list>
    /// </summary>
    public sealed class JobStateMachineQaTests
    {
        // =============================================================================
        // StateMachineAdvancesActionPhaseAndCompletion
        // =============================================================================
        /// <summary>
        /// <para>
        /// Verifica il percorso positivo: step completato, fase completata e job
        /// completato.
        /// </para>
        ///
        /// <para><b>Job -> Phase -> Action</b></para>
        /// <para>
        /// Il test usa due fasi: la prima con due action, la seconda con una action.
        /// Questo basta a proteggere tutti i confini gerarchici minimi.
        /// </para>
        ///
        /// <para><b>Struttura interna:</b></para>
        /// <list type="bullet">
        ///   <item><b>Tick 1</b>: avanza action dentro fase zero.</item>
        ///   <item><b>Tick 2</b>: passa alla fase uno.</item>
        ///   <item><b>Tick 3</b>: completa il job.</item>
        /// </list>
        /// </summary>
        [Test]
        public void StateMachineAdvancesActionPhaseAndCompletion()
        {
            // Arrange: job con piano piccolo ma gerarchico.
            var machine = new JobStateMachine();
            var job = MakeJob();
            var state = NpcJobState.Empty();
            state.AssignJob(job.JobId, 0);

            // Act: tre successi consecutivi coprono action, fase e completamento.
            var first = machine.ApplyStepResult(ref state, job, StepResult.Succeeded("step-0-ok"), 1);
            var second = machine.ApplyStepResult(ref state, job, StepResult.Succeeded("step-1-ok"), 2);
            var third = machine.ApplyStepResult(ref state, job, StepResult.Succeeded("step-2-ok"), 3);

            // Assert: il cursore e il job avanzano in modo deterministico.
            Assert.That(first.TickResult, Is.EqualTo(JobStateMachineTickResult.ActionAdvanced));
            Assert.That(second.TickResult, Is.EqualTo(JobStateMachineTickResult.PhaseAdvanced));
            Assert.That(third.TickResult, Is.EqualTo(JobStateMachineTickResult.JobCompleted));
            Assert.That(job.Status, Is.EqualTo(JobStatus.Completed));
            Assert.That(state.HasActiveJob, Is.False);
        }

        // =============================================================================
        // StateMachineWaitGatePreventsPrematureRetry
        // =============================================================================
        /// <summary>
        /// <para>
        /// Verifica che uno step in attesa non venga rieseguito prima del tick
        /// indicato dal risultato.
        /// </para>
        ///
        /// <para><b>Azioni multi-tick</b></para>
        /// <para>
        /// Alcuni step futuri, come lavoro al banco o attesa, richiedono piu' tick.
        /// Il gate evita di duplicare comandi ogni frame.
        /// </para>
        ///
        /// <para><b>Struttura interna:</b></para>
        /// <list type="bullet">
        ///   <item><b>Waiting result</b>: imposta WaitUntilTick.</item>
        ///   <item><b>Early tick</b>: ritorna Waiting senza avanzare.</item>
        ///   <item><b>Boundary tick</b>: permette nuovo risultato.</item>
        /// </list>
        /// </summary>
        [Test]
        public void StateMachineWaitGatePreventsPrematureRetry()
        {
            // Arrange: stato attivo e macchina senza executor concreto.
            var machine = new JobStateMachine();
            var job = MakeJob();
            var state = NpcJobState.Empty();
            state.AssignJob(job.JobId, 0);

            // Act: lo step chiede attesa fino al tick 6, poi proviamo a tick 5 e 6.
            var wait = machine.ApplyStepResult(ref state, job, StepResult.Waiting(5, "wait"), 1);
            var early = machine.ApplyStepResult(ref state, job, StepResult.Succeeded("too-early"), 5);
            var boundary = machine.ApplyStepResult(ref state, job, StepResult.Succeeded("ok-now"), 6);

            // Assert: prima del limite non si avanza, al limite si puo' procedere.
            Assert.That(wait.TickResult, Is.EqualTo(JobStateMachineTickResult.Waiting));
            Assert.That(early.TickResult, Is.EqualTo(JobStateMachineTickResult.Waiting));
            Assert.That(boundary.TickResult, Is.EqualTo(JobStateMachineTickResult.ActionAdvanced));
            Assert.That(state.ActiveActionIndex, Is.EqualTo(1));
        }

        // =============================================================================
        // StateMachineFailureClosesJobAndNpcState
        // =============================================================================
        /// <summary>
        /// <para>
        /// Verifica che un fallimento di step chiuda il job e liberi lo stato NPC.
        /// </para>
        ///
        /// <para><b>Failure path esplicito</b></para>
        /// <para>
        /// Il fallimento non deve lasciare un NPC appeso a un job attivo ma non
        /// recuperabile.
        /// </para>
        ///
        /// <para><b>Struttura interna:</b></para>
        /// <list type="bullet">
        ///   <item><b>StepResult.Failed</b>: motivo ReservationDenied.</item>
        ///   <item><b>Job</b>: stato Failed.</item>
        ///   <item><b>NpcJobState</b>: HasActiveJob false.</item>
        /// </list>
        /// </summary>
        [Test]
        public void StateMachineFailureClosesJobAndNpcState()
        {
            // Arrange: job attivo pronto a fallire.
            var machine = new JobStateMachine();
            var job = MakeJob();
            var state = NpcJobState.Empty();
            state.AssignJob(job.JobId, 0);

            // Act: un executor futuro segnala prenotazione negata.
            var result = machine.ApplyStepResult(
                ref state,
                job,
                StepResult.Failed(JobFailureReason.ReservationDenied, "reserved"),
                7);

            // Assert: job e stato NPC sono chiusi in modo coerente.
            Assert.That(result.TickResult, Is.EqualTo(JobStateMachineTickResult.JobFailed));
            Assert.That(job.Status, Is.EqualTo(JobStatus.Failed));
            Assert.That(job.FailureReason, Is.EqualTo(JobFailureReason.ReservationDenied));
            Assert.That(state.HasActiveJob, Is.False);
            Assert.That(state.LastFailureReason, Is.EqualTo(JobFailureReason.ReservationDenied));
        }

        private static Job MakeJob()
        {
            // Factory locale con due fasi e tre action complessive, senza dipendere da World.
            var request = JobRequest.WithoutTarget(
                "req-sm",
                1,
                DecisionIntentKind.WaitAndObserve,
                JobPriorityClass.Normal,
                0.5f,
                0,
                "state-machine");
            var plan = new JobPlan(
                "plan-sm",
                new[]
                {
                    new JobPhase(
                        "phase-a",
                        JobPhaseKind.Prepare,
                        "Prepara",
                        0,
                        true,
                        new[]
                        {
                            JobAction.Simple("a0", JobActionKind.Evaluate, "Valuta"),
                            JobAction.Simple("a1", JobActionKind.ReserveTarget, "Prenota")
                        }),
                    new JobPhase(
                        "phase-b",
                        JobPhaseKind.Execute,
                        "Esegui",
                        0,
                        true,
                        new[] { JobAction.Simple("b0", JobActionKind.Consume, "Consuma") })
                });
            return new Job("job-sm", request, plan);
        }
    }
}
