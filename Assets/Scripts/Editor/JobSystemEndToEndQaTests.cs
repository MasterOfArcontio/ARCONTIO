using Arcontio.Core;
using NUnit.Framework;
using UnityEngine;

namespace Arcontio.Tests
{
    // =============================================================================
    // JobSystemEndToEndQaTests
    // =============================================================================
    /// <summary>
    /// <para>
    /// Test QA EditMode end-to-end per il Job System v0.06.
    /// </para>
    ///
    /// <para><b>Catena completa senza runtime World</b></para>
    /// <para>
    /// Il test collega richiesta, piano, stato NPC, executor, reservation store e
    /// state machine. Rimane comunque fuori dal World: gli input oggettivi vengono
    /// passati come dati espliciti.
    /// </para>
    ///
    /// <para><b>Struttura interna:</b></para>
    /// <list type="bullet">
    ///   <item><b>Plan</b>: reach + reserve + consume + cleanup.</item>
    ///   <item><b>Executors</b>: basic e cognitive.</item>
    ///   <item><b>StateMachine</b>: avanzamento del cursore e completamento job.</item>
    /// </list>
    /// </summary>
    public sealed class JobSystemEndToEndQaTests
    {
        // =============================================================================
        // JobSystemCompletesKnownFoodPlanThroughPhases
        // =============================================================================
        /// <summary>
        /// <para>
        /// Verifica un piano "mangia cibo noto" composto da piu' fasi e step.
        /// </para>
        ///
        /// <para><b>Decisione trasformata in job eseguibile</b></para>
        /// <para>
        /// La richiesta nasce da <c>EatKnownFood</c>, il piano viene eseguito come
        /// sequenza deterministica di mini job e la state machine completa il lavoro.
        /// </para>
        ///
        /// <para><b>Struttura interna:</b></para>
        /// <list type="bullet">
        ///   <item><b>ReachTarget</b>: movimento gia' arrivato.</item>
        ///   <item><b>Prepare</b>: reservation target.</item>
        ///   <item><b>Execute</b>: consume.</item>
        ///   <item><b>Cleanup</b>: release reservation.</item>
        /// </list>
        /// </summary>
        [Test]
        public void JobSystemCompletesKnownFoodPlanThroughPhases()
        {
            // Arrange: creiamo un piano completo ma piccolo, con target soggettivo noto.
            var target = new Vector2Int(8, 8);
            var store = new ReservationStore();
            var basic = new BasicJobActionExecutor();
            var cognitive = new CognitiveJobActionExecutor();
            var machine = new JobStateMachine();
            var job = MakeFoodJob(target);
            var state = NpcJobState.Empty();
            state.AssignJob(job.JobId, 0);

            // Act: eseguiamo gli step in ordine, passando ogni risultato alla state machine.
            ApplyCurrent(job, ref state, basic, cognitive, store, target, machine, 1);
            ApplyCurrent(job, ref state, basic, cognitive, store, target, machine, 2);
            ApplyCurrent(job, ref state, basic, cognitive, store, target, machine, 3);
            var final = ApplyCurrent(job, ref state, basic, cognitive, store, target, machine, 4);

            // Assert: il job ha attraversato tutte le fasi e ha liberato lo stato NPC.
            Assert.That(final.TickResult, Is.EqualTo(JobStateMachineTickResult.JobCompleted));
            Assert.That(job.Status, Is.EqualTo(JobStatus.Completed));
            Assert.That(state.HasActiveJob, Is.False);
            Assert.That(store.Count, Is.EqualTo(0));
        }

        // =============================================================================
        // JobSystemRecordsFailureLearningWhenStepFails
        // =============================================================================
        /// <summary>
        /// <para>
        /// Verifica che un fallimento terminale possa essere trasformato in
        /// osservazione di failure learning.
        /// </para>
        ///
        /// <para><b>Failure path end-to-end</b></para>
        /// <para>
        /// La state machine chiude il job, poi lo store di apprendimento registra il
        /// pattern senza leggere World o memoria grezza.
        /// </para>
        ///
        /// <para><b>Struttura interna:</b></para>
        /// <list type="bullet">
        ///   <item><b>StepResult</b>: MovementFailed.</item>
        ///   <item><b>StateMachine</b>: JobFailed.</item>
        ///   <item><b>FailureLearning</b>: conteggio SearchFood/MovementFailed.</item>
        /// </list>
        /// </summary>
        [Test]
        public void JobSystemRecordsFailureLearningWhenStepFails()
        {
            // Arrange: job attivo e store apprendimento vuoto.
            var machine = new JobStateMachine();
            var learning = new JobFailureLearningStore();
            var job = MakeFoodJob(new Vector2Int(8, 8));
            var state = NpcJobState.Empty();
            state.AssignJob(job.JobId, 0);

            // Act: simuliamo un fallimento di movimento e registriamo l'osservazione.
            var result = machine.ApplyStepResult(ref state, job, StepResult.Failed(JobFailureReason.MovementFailed, "blocked"), 5);
            learning.Record(new JobFailureObservation(job.Request.NpcId, job.JobId, job.Request.IntentKind, job.FailureReason, 5, "blocked"));

            // Assert: job, stato e apprendimento raccontano lo stesso fallimento.
            Assert.That(result.TickResult, Is.EqualTo(JobStateMachineTickResult.JobFailed));
            Assert.That(job.Status, Is.EqualTo(JobStatus.Failed));
            Assert.That(state.HasActiveJob, Is.False);
            Assert.That(learning.GetCount(1, DecisionIntentKind.EatKnownFood, JobFailureReason.MovementFailed), Is.EqualTo(1));
        }

        private static JobStateMachineResult ApplyCurrent(
            Job job,
            ref NpcJobState state,
            BasicJobActionExecutor basic,
            CognitiveJobActionExecutor cognitive,
            ReservationStore store,
            Vector2Int npcCell,
            JobStateMachine machine,
            int tick)
        {
            // Il test recupera fase e action dal cursore: nessuna conoscenza esterna
            // dell'ordine del piano viene usata durante l'esecuzione.
            Assert.That(job.Plan.TryGetPhase(state.ActivePhaseIndex, out var phase), Is.True);
            Assert.That(phase.TryGetAction(state.ActiveActionIndex, out var action), Is.True);

            var context = new JobActionExecutionContext(1, job.JobId, tick, npcCell, store);
            var stepResult = basic.CanExecute(action)
                ? basic.Execute(action, context)
                : cognitive.Execute(action, context);

            return machine.ApplyStepResult(ref state, job, stepResult, tick);
        }

        private static Job MakeFoodJob(Vector2Int target)
        {
            // Factory end-to-end: trasforma una decisione EatKnownFood in piano a fasi.
            var request = JobRequest.FromDecision(
                "req-food-e2e",
                1,
                DecisionIntentKind.EatKnownFood,
                JobPriorityClass.Critical,
                0.9f,
                0,
                target,
                "belief:food:e2e",
                "Eat known food e2e");
            var plan = new JobPlan(
                "food-plan-e2e",
                new[]
                {
                    new JobPhase("reach", JobPhaseKind.ReachTarget, "Raggiungi", 0, true, new[] { JobAction.MoveTo("move", target, "Vai al cibo") }),
                    new JobPhase("prepare", JobPhaseKind.Prepare, "Prepara", 0, true, new[] { new JobAction("reserve", JobActionKind.ReserveTarget, "Prenota", true, target, -1, 0, string.Empty) }),
                    new JobPhase("execute", JobPhaseKind.Execute, "Mangia", 0, false, new[] { new JobAction("consume", JobActionKind.Consume, "Consuma", false, Vector2Int.zero, 77, 0, string.Empty) }),
                    new JobPhase("cleanup", JobPhaseKind.Cleanup, "Rilascia", 0, true, new[] { JobAction.Simple("release", JobActionKind.ReleaseReservation, "Rilascia") })
                });
            return new Job("job-food-e2e", request, plan);
        }
    }
}
