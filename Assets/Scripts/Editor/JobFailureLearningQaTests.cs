using Arcontio.Core;
using NUnit.Framework;
using UnityEngine;

namespace Arcontio.Tests
{
    // =============================================================================
    // JobFailureLearningQaTests
    // =============================================================================
    /// <summary>
    /// <para>
    /// Test QA EditMode per lo store MVP di failure learning del Job System.
    /// </para>
    ///
    /// <para><b>Apprendimento aggregato per-NPC</b></para>
    /// <para>
    /// I test verificano che fallimenti uguali aumentino un conteggio soggettivo e
    /// producano una penalita' normalizzata, senza accesso a memoria episodica o World.
    /// </para>
    ///
    /// <para><b>Struttura interna:</b></para>
    /// <list type="bullet">
    ///   <item><b>Record</b>: aggiunge osservazioni normalizzate.</item>
    ///   <item><b>GetCount</b>: legge pattern specifici.</item>
    ///   <item><b>GetPenalty01</b>: converte conteggio in segnale per scoring futuro.</item>
    /// </list>
    /// </summary>
    public sealed class JobFailureLearningQaTests
    {
        // =============================================================================
        // FailureLearningAggregatesByNpcIntentAndReason
        // =============================================================================
        /// <summary>
        /// <para>
        /// Verifica che lo store separi conteggi per NPC, intenzione e motivo.
        /// </para>
        ///
        /// <para><b>Esperienza soggettiva</b></para>
        /// <para>
        /// Un NPC che fallisce SearchFood non deve automaticamente penalizzare tutti
        /// gli altri NPC o tutte le altre intenzioni.
        /// </para>
        ///
        /// <para><b>Struttura interna:</b></para>
        /// <list type="bullet">
        ///   <item><b>Npc 1</b>: due fallimenti MovementFailed su SearchFood.</item>
        ///   <item><b>Npc 2</b>: nessun fallimento equivalente.</item>
        ///   <item><b>Assert</b>: conteggi e penalita' separati.</item>
        /// </list>
        /// </summary>
        [Test]
        public void FailureLearningAggregatesByNpcIntentAndReason()
        {
            // Arrange: store vuoto e osservazioni soggettive.
            var store = new JobFailureLearningStore();
            var first = new JobFailureObservation(1, "job-a", DecisionIntentKind.SearchFood, JobFailureReason.MovementFailed, 10, "wall");
            var second = new JobFailureObservation(1, "job-b", DecisionIntentKind.SearchFood, JobFailureReason.MovementFailed, 20, "wall");
            var otherNpc = new JobFailureObservation(2, "job-c", DecisionIntentKind.SearchFood, JobFailureReason.MovementFailed, 30, "wall");

            // Act: registriamo pattern uguali e uno stesso pattern su altro NPC.
            store.Record(first);
            store.Record(second);
            store.Record(otherNpc);

            // Assert: il conteggio resta per-NPC e la penalita' e' normalizzata.
            Assert.That(store.GetCount(1, DecisionIntentKind.SearchFood, JobFailureReason.MovementFailed), Is.EqualTo(2));
            Assert.That(store.GetCount(2, DecisionIntentKind.SearchFood, JobFailureReason.MovementFailed), Is.EqualTo(1));
            Assert.That(store.GetPenalty01(1, DecisionIntentKind.SearchFood, JobFailureReason.MovementFailed), Is.EqualTo(2f / 3f).Within(0.001f));
            Assert.That(store.GetPenalty01(1, DecisionIntentKind.EatKnownFood, JobFailureReason.MovementFailed), Is.EqualTo(0f));
        }

        // =============================================================================
        // JobRuntimeStateRecordsFailureLearningWhenCurrentJobFails
        // =============================================================================
        /// <summary>
        /// <para>
        /// Verifica il cablaggio minimo tra runtime job reale e store passivo dei
        /// fallimenti.
        /// </para>
        ///
        /// <para><b>Ritorno cognitivo leggero, non scoring</b></para>
        /// <para>
        /// Il test non pretende che il Decision Layer usi subito la penalita'. Si
        /// limita a garantire che la chiusura negativa di un incarico lasci un
        /// conteggio leggibile, soggettivo e bounded nello stato runtime.
        /// </para>
        /// </summary>
        [Test]
        public void JobRuntimeStateRecordsFailureLearningWhenCurrentJobFails()
        {
            // Arrange: assegniamo un job con intenzione riconoscibile al runtime.
            var runtime = new JobRuntimeState();
            var job = MakeTargetedJob("job-fail-learning", 7, DecisionIntentKind.EatKnownFood, new Vector2Int(4, 2));

            Assert.That(runtime.TryAssignJob(7, job, tick: 10, out var assignReason), Is.True, assignReason);
            Assert.That(runtime.FailureLearning.PatternCount, Is.EqualTo(0));

            // Act: il fallimento operativo viene registrato dal punto che chiude il job.
            Assert.That(runtime.FailCurrentJob(7, JobFailureReason.MissingTarget, tick: 11, out var failReason), Is.True, failReason);

            // Assert: lo store resta passivo, ma conserva il pattern soggettivo.
            Assert.That(runtime.FailureLearning.PatternCount, Is.EqualTo(1));
            Assert.That(runtime.FailureLearning.GetCount(7, DecisionIntentKind.EatKnownFood, JobFailureReason.MissingTarget), Is.EqualTo(1));
            Assert.That(runtime.FailureLearning.GetPenalty01(7, DecisionIntentKind.EatKnownFood, JobFailureReason.MissingTarget), Is.EqualTo(1f / 3f).Within(0.001f));
        }

        // =============================================================================
        // JobRuntimeStateClearsFailureLearningWithTransientJobs
        // =============================================================================
        /// <summary>
        /// <para>
        /// Verifica che il reset transitorio del Job layer pulisca anche i conteggi
        /// runtime non persistiti.
        /// </para>
        /// </summary>
        [Test]
        public void JobRuntimeStateClearsFailureLearningWithTransientJobs()
        {
            // Arrange: creiamo un fallimento runtime osservabile.
            var runtime = new JobRuntimeState();
            var job = MakeTargetedJob("job-clear-learning", 8, DecisionIntentKind.SearchFood, new Vector2Int(2, 9));

            Assert.That(runtime.TryAssignJob(8, job, tick: 20, out var assignReason), Is.True, assignReason);
            Assert.That(runtime.FailCurrentJob(8, JobFailureReason.MovementFailed, tick: 21, out var failReason), Is.True, failReason);
            Assert.That(runtime.FailureLearning.PatternCount, Is.EqualTo(1));

            // Act: ClearTransientJobs rappresenta bootstrap/load/reset tecnico.
            runtime.ClearTransientJobs();

            // Assert: il dato non persistito viene eliminato insieme agli altri stati job.
            Assert.That(runtime.FailureLearning.PatternCount, Is.EqualTo(0));
            Assert.That(runtime.FailureLearning.GetCount(8, DecisionIntentKind.SearchFood, JobFailureReason.MovementFailed), Is.EqualTo(0));
        }

        private static Job MakeTargetedJob(string jobId, int npcId, DecisionIntentKind intentKind, Vector2Int targetCell)
        {
            var request = JobRequest.FromDecision(
                jobId + "-request",
                npcId,
                intentKind,
                JobPriorityClass.Normal,
                0.75f,
                0,
                targetCell,
                "test-belief",
                "failure-learning-test");

            var plan = new JobPlan(
                jobId + "-plan",
                new[]
                {
                    new JobPhase(
                        "phase",
                        JobPhaseKind.Execute,
                        "Esegui",
                        0,
                        true,
                        new[] { JobAction.Simple("step", JobActionKind.Evaluate, "Valuta") })
                });

            return new Job(jobId, request, plan);
        }
    }
}
