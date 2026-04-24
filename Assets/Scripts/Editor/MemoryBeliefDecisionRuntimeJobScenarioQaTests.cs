using Arcontio.Core;
using Arcontio.Core.Config;
using NUnit.Framework;
using UnityEngine;

namespace Arcontio.Tests
{
    // =============================================================================
    // MemoryBeliefDecisionRuntimeJobScenarioQaTests
    // =============================================================================
    /// <summary>
    /// <para>
    /// Test QA EditMode per gli scenari runtime dell'Explainability Layer v0.07
    /// applicati al Job layer reale.
    /// </para>
    ///
    /// <para><b>Scenario runtime piccolo ma rappresentativo</b></para>
    /// <para>
    /// I test non orchestrano l'intera simulazione. Costruiscono invece scenari
    /// locali sui componenti che possiedono davvero la transizione: state machine,
    /// reservation store, arbiter, command buffer e failure learning. In questo modo
    /// il layer diagnostico resta allineato al runtime senza creare dipendenze
    /// globali o world state onnisciente.
    /// </para>
    ///
    /// <para><b>Struttura interna:</b></para>
    /// <list type="bullet">
    ///   <item><b>Scenario job</b>: successo multi-phase con lifecycle, phase, step e state.</item>
    ///   <item><b>Scenario waiting/blocked</b>: retry temporale senza avanzamento spurio.</item>
    ///   <item><b>Scenario contention/preemption</b>: reservation denied, arbitration e failure learning.</item>
    ///   <item><b>Scenario command</b>: enqueue e snapshot del buffer comandi.</item>
    /// </list>
    /// </summary>
    public sealed class MemoryBeliefDecisionRuntimeJobScenarioQaTests
    {
        // =============================================================================
        // MultiPhaseJobScenarioWritesLifecyclePhaseStepAndStateTraces
        // =============================================================================
        /// <summary>
        /// <para>
        /// Verifica che un job semplice ma multi-phase produca trace leggibili per
        /// lifecycle, phase, step e snapshot runtime dello stato NPC.
        /// </para>
        /// </summary>
        [Test]
        public void MultiPhaseJobScenarioWritesLifecyclePhaseStepAndStateTraces()
        {
            // Arrange: costruiamo un job piccolo con due fasi e tre step complessivi.
            var config = MakeConfig();
            var registry = new MemoryBeliefDecisionExplainabilityRegistry();
            var machine = new JobStateMachine();
            int npcId = 21;
            var job = MakeTwoPhaseJob("job-runtime-success", DecisionIntentKind.WaitAndObserve);
            var state = NpcJobState.Empty();
            state.AssignJob(job.JobId, 0);

            // Act: tre successi coprono avanzamento step, avanzamento fase e chiusura.
            var first = machine.ApplyStepResult(ref state, job, StepResult.Succeeded("step-0-ok"), 1, config, registry, npcId);
            var second = machine.ApplyStepResult(ref state, job, StepResult.Succeeded("step-1-ok"), 2, config, registry, npcId);
            var third = machine.ApplyStepResult(ref state, job, StepResult.Succeeded("step-2-ok"), 3, config, registry, npcId);

            // Assert: la state machine resta coerente e il registry racconta il
            // percorso completo senza dover ricostruire nulla ex-post.
            Assert.That(first.TickResult, Is.EqualTo(JobStateMachineTickResult.ActionAdvanced));
            Assert.That(second.TickResult, Is.EqualTo(JobStateMachineTickResult.PhaseAdvanced));
            Assert.That(third.TickResult, Is.EqualTo(JobStateMachineTickResult.JobCompleted));

            Assert.That(registry.TryGetNpcStore(npcId, out var store), Is.True);
            Assert.That(store.StepTraceCount, Is.EqualTo(3));
            Assert.That(store.JobStateTraceCount, Is.EqualTo(3));
            Assert.That(store.JobLifecycleTraceCount, Is.EqualTo(2));
            Assert.That(store.JobPhaseTraceCount, Is.EqualTo(3));

            Assert.That(store.TryGetLatestJobLifecycleTrace(out var lifecycleTrace), Is.True);
            Assert.That(lifecycleTrace.JobLifecycle.Operation, Is.EqualTo(MemoryBeliefDecisionJobLifecycleOperation.Completed));

            Assert.That(store.TryGetLatestJobPhaseTrace(out var phaseTrace), Is.True);
            Assert.That(phaseTrace.JobPhase.Operation, Is.EqualTo(MemoryBeliefDecisionJobPhaseOperation.Completed));

            Assert.That(store.TryGetLatestStepTrace(out var stepTrace), Is.True);
            Assert.That(stepTrace.Step.Result.Status, Is.EqualTo(StepResultStatus.Succeeded));

            Assert.That(store.TryGetLatestJobStateTrace(out var stateTrace), Is.True);
            Assert.That(stateTrace.JobState.HasActiveJob, Is.False);
            Assert.That(job.Status, Is.EqualTo(JobStatus.Completed));
        }

        // =============================================================================
        // WaitingAndBlockedScenarioWritesRetryFriendlyTraces
        // =============================================================================
        /// <summary>
        /// <para>
        /// Verifica che waiting e blocked mantengano il job in esecuzione e rendano
        /// osservabile il gate temporale nello snapshot runtime.
        /// </para>
        /// </summary>
        [Test]
        public void WaitingAndBlockedScenarioWritesRetryFriendlyTraces()
        {
            // Arrange: un job minimale basta per coprire il comportamento temporale.
            var config = MakeConfig();
            var registry = new MemoryBeliefDecisionExplainabilityRegistry();
            var machine = new JobStateMachine();
            int npcId = 22;
            var job = MakeSingleStepJob("job-runtime-wait", DecisionIntentKind.WaitAndObserve, JobActionKind.WaitTicks);
            var state = NpcJobState.Empty();
            state.AssignJob(job.JobId, 0);

            // Act: prima entriamo in waiting, poi in blocked, infine verifichiamo che
            // il cursore resti fermo ma il retry venga spostato in avanti.
            var waiting = machine.ApplyStepResult(ref state, job, StepResult.Waiting(4, "wait-for-target"), 10, config, registry, npcId);
            var blocked = machine.ApplyStepResult(ref state, job, StepResult.Blocked(2, "blocked-by-door"), 14, config, registry, npcId);

            // Assert: il job non avanza di fase o di step, ma il registry conserva il
            // risultato dell'ultimo tentativo e il tick di riprova.
            Assert.That(waiting.TickResult, Is.EqualTo(JobStateMachineTickResult.Waiting));
            Assert.That(blocked.TickResult, Is.EqualTo(JobStateMachineTickResult.Waiting));
            Assert.That(job.Status, Is.EqualTo(JobStatus.Running));
            Assert.That(state.WaitUntilTick, Is.EqualTo(16));

            Assert.That(registry.TryGetNpcStore(npcId, out var store), Is.True);
            Assert.That(store.StepTraceCount, Is.EqualTo(2));
            Assert.That(store.JobStateTraceCount, Is.EqualTo(2));
            Assert.That(store.JobLifecycleTraceCount, Is.EqualTo(1));

            Assert.That(store.TryGetLatestStepTrace(out var stepTrace), Is.True);
            Assert.That(stepTrace.Step.Result.Status, Is.EqualTo(StepResultStatus.Blocked));

            Assert.That(store.TryGetLatestJobStateTrace(out var stateTrace), Is.True);
            Assert.That(stateTrace.JobState.HasActiveJob, Is.True);
            Assert.That(stateTrace.JobState.WaitUntilTick, Is.EqualTo(16));
        }

        // =============================================================================
        // ReservationArbitrationAndFailureLearningScenarioWritesContentionTraces
        // =============================================================================
        /// <summary>
        /// <para>
        /// Verifica che uno scenario di contesa reale produca trace di reservation,
        /// arbitration e failure learning senza introdurre accessi globali.
        /// </para>
        /// </summary>
        [Test]
        public void ReservationArbitrationAndFailureLearningScenarioWritesContentionTraces()
        {
            // Arrange: due job competono sulla stessa cella, mentre un terzo scenario
            // copre la preemption tramite arbiter con classi di priorita' diverse.
            var config = MakeConfig();
            var registry = new MemoryBeliefDecisionExplainabilityRegistry();
            var reservations = new ReservationStore();
            var failureLearning = new JobFailureLearningStore();
            var arbiter = new JobArbiter();
            var targetCell = new Vector2Int(12, 8);

            var reservationA = new ReservationRecord("res-a", "job-a", 31, ReservationTargetKind.Cell, targetCell, 0, 100, 110);
            var reservationB = new ReservationRecord("res-b", "job-b", 32, ReservationTargetKind.Cell, targetCell, 0, 101, 111);

            var currentJob = MakeSingleStepJob("job-current", DecisionIntentKind.WaitAndObserve, JobActionKind.Evaluate, JobPriorityClass.Normal);
            var emergencyJob = MakeSingleStepJob("job-emergency", DecisionIntentKind.EatKnownFood, JobActionKind.ReserveTarget, JobPriorityClass.Emergency);
            var npcState = NpcJobState.Empty();
            npcState.AssignJob(currentJob.JobId, 0);

            // Act: la prima reservation viene accettata, la seconda negata, il
            // failure learning apprende il fallimento e l'arbitro preempte il job.
            bool acceptedA = reservations.TryReserve(reservationA, out _, config, registry, 100, 31);
            bool acceptedB = reservations.TryReserve(reservationB, out var existing, config, registry, 101, 32);
            failureLearning.Record(
                new JobFailureObservation(
                    32,
                    "job-b",
                    DecisionIntentKind.EatKnownFood,
                    JobFailureReason.ReservationDenied,
                    101,
                    "ReservationDeniedOnCell",
                    true,
                    targetCell),
                config,
                registry);
            var arbitration = arbiter.Evaluate(npcState, currentJob, emergencyJob, config, registry, 31, 102);

            // Assert: i risultati concreti e le trace EL devono raccontare la stessa
            // storia di contesa e preemption.
            Assert.That(acceptedA, Is.True);
            Assert.That(acceptedB, Is.False);
            Assert.That(existing.JobId, Is.EqualTo("job-a"));
            Assert.That(arbitration.Decision, Is.EqualTo(JobArbitrationDecision.SuspendCurrentForNew));
            Assert.That(arbitration.AcceptedJobId, Is.EqualTo("job-emergency"));
            Assert.That(failureLearning.GetCount(32, DecisionIntentKind.EatKnownFood, JobFailureReason.ReservationDenied), Is.EqualTo(1));

            Assert.That(registry.TryGetNpcStore(31, out var store31), Is.True);
            Assert.That(store31.ReservationTraceCount, Is.EqualTo(1));
            Assert.That(store31.JobArbitrationTraceCount, Is.EqualTo(1));

            Assert.That(registry.TryGetNpcStore(32, out var store32), Is.True);
            Assert.That(store32.ReservationTraceCount, Is.EqualTo(1));
            Assert.That(store32.FailureLearningTraceCount, Is.EqualTo(1));

            Assert.That(store32.TryGetLatestReservationTrace(out var deniedTrace), Is.True);
            Assert.That(deniedTrace.Reservation.Operation, Is.EqualTo(MemoryBeliefDecisionReservationOperation.Denied));

            Assert.That(store31.TryGetLatestJobArbitrationTrace(out var arbitrationTrace), Is.True);
            Assert.That(arbitrationTrace.JobArbitration.Decision, Is.EqualTo(JobArbitrationDecision.SuspendCurrentForNew));

            Assert.That(store32.TryGetLatestFailureLearningTrace(out var failureTrace), Is.True);
            Assert.That(failureTrace.FailureLearning.FailureReason, Is.EqualTo(JobFailureReason.ReservationDenied));
            Assert.That(failureTrace.FailureLearning.TargetCell, Is.EqualTo(targetCell));
        }

        // =============================================================================
        // CommandBufferScenarioWritesEnqueueAndSnapshotTraces
        // =============================================================================
        /// <summary>
        /// <para>
        /// Verifica che il buffer comandi emetta trace coerenti sia in enqueue sia in
        /// snapshot, mantenendo leggibile il confine step -> command.
        /// </para>
        /// </summary>
        [Test]
        public void CommandBufferScenarioWritesEnqueueAndSnapshotTraces()
        {
            // Arrange: il buffer non ha bisogno di World per essere testato.
            var config = MakeConfig();
            var registry = new MemoryBeliefDecisionExplainabilityRegistry();
            var buffer = new JobCommandBuffer();
            int npcId = 41;

            // Act: accodiamo un command fittizio e chiediamo subito uno snapshot.
            bool accepted = buffer.Enqueue(new TestCommand(), config, registry, npcId, 300, "job-command", "StepIssuedCommand");
            var snapshot = buffer.Snapshot(config, registry, npcId, 301, "job-command", "FlushPreview");

            // Assert: sia il comportamento del buffer sia il boundary diagnostico
            // devono risultare osservabili e coerenti.
            Assert.That(accepted, Is.True);
            Assert.That(snapshot.Length, Is.EqualTo(1));
            Assert.That(snapshot[0].Name, Is.EqualTo(nameof(TestCommand)));

            Assert.That(registry.TryGetNpcStore(npcId, out var store), Is.True);
            Assert.That(store.CommandTraceCount, Is.EqualTo(2));

            Assert.That(store.TryGetLatestCommandTrace(out var commandTrace), Is.True);
            Assert.That(commandTrace.Command.Operation, Is.EqualTo(MemoryBeliefDecisionCommandOperation.Snapshot));
            Assert.That(commandTrace.Command.QueueCount, Is.EqualTo(1));
            Assert.That(commandTrace.Command.CommandName, Is.EqualTo(nameof(TestCommand)));
        }

        private static MemoryBeliefDecisionExplainabilityParams MakeConfig()
        {
            return new MemoryBeliefDecisionExplainabilityParams
            {
                enabled = true,
                writeJsonLog = false,
                logJobLifecycle = true,
                logJobPhase = true,
                logStep = true,
                logJobState = true,
                logJobArbitration = true,
                logReservation = true,
                logCommand = true,
                logFailureLearning = true,
            };
        }

        private static Job MakeTwoPhaseJob(string jobId, DecisionIntentKind intentKind)
        {
            var request = JobRequest.WithoutTarget(
                $"{jobId}-request",
                1,
                intentKind,
                JobPriorityClass.Normal,
                0.50f,
                0,
                "runtime-scenario");
            var plan = new JobPlan(
                $"{jobId}-plan",
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
            return new Job(jobId, request, plan);
        }

        private static Job MakeSingleStepJob(
            string jobId,
            DecisionIntentKind intentKind,
            JobActionKind actionKind,
            JobPriorityClass priorityClass = JobPriorityClass.Normal)
        {
            var request = JobRequest.WithoutTarget(
                $"{jobId}-request",
                1,
                intentKind,
                priorityClass,
                0.75f,
                0,
                "runtime-scenario");
            var plan = new JobPlan(
                $"{jobId}-plan",
                new[]
                {
                    new JobPhase(
                        "phase-only",
                        JobPhaseKind.Execute,
                        "Esegui",
                        0,
                        true,
                        new[] { JobAction.Simple("step-0", actionKind, "Azione") })
                });
            return new Job(jobId, request, plan);
        }

        private sealed class TestCommand : ICommand
        {
            public void Execute(World world, MessageBus bus)
            {
                // Comando fittizio: il test verifica solo il boundary buffer -> trace.
            }
        }
    }
}
