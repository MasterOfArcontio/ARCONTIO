using Arcontio.Core;
using NUnit.Framework;
using UnityEngine;

namespace Arcontio.Tests
{
    // =============================================================================
    // JobCoreContractsQaTests
    // =============================================================================
    /// <summary>
    /// <para>
    /// Test QA EditMode per i contratti dati iniziali del Job System v0.06.
    /// </para>
    ///
    /// <para><b>Job System senza accesso globale</b></para>
    /// <para>
    /// Questi test costruiscono richieste, piani e fasi senza creare un
    /// <c>World</c>, senza leggere store oggettivi e senza eseguire comandi. Lo
    /// scopo e' proteggere la forma del contratto prima di collegarlo ai sistemi.
    /// </para>
    ///
    /// <para><b>Struttura interna:</b></para>
    /// <list type="bullet">
    ///   <item><b>Request</b>: verifica il passaggio Decision Layer -> Job System.</item>
    ///   <item><b>Plan</b>: verifica ordinamento e copia difensiva delle fasi.</item>
    ///   <item><b>Job</b>: verifica stato persistente e cursore di fase.</item>
    /// </list>
    /// </summary>
    public sealed class JobCoreContractsQaTests
    {
        // =============================================================================
        // JobRequestFromDecisionPreservesSubjectiveTarget
        // =============================================================================
        /// <summary>
        /// <para>
        /// Verifica che una richiesta generata da una decisione conservi il target
        /// soggettivo e la belief di origine senza richiedere accesso al mondo.
        /// </para>
        ///
        /// <para><b>Decision Layer / Job Execution boundary</b></para>
        /// <para>
        /// Il Decision Layer consegna un target gia' scelto. Il job contract deve
        /// trasportarlo come dato, non ricalcolarlo tramite query globali.
        /// </para>
        ///
        /// <para><b>Struttura interna:</b></para>
        /// <list type="bullet">
        ///   <item><b>Arrange</b>: costruisce una request da decisione con target cella.</item>
        ///   <item><b>Act</b>: nessuna esecuzione, il contratto e' il soggetto del test.</item>
        ///   <item><b>Assert</b>: target, priorita' e confidence implicita restano dati puri.</item>
        /// </list>
        /// </summary>
        [Test]
        public void JobRequestFromDecisionPreservesSubjectiveTarget()
        {
            // Arrange: simuliamo l'uscita del Decision Layer con una cella scelta dal BeliefQuery.
            var target = new Vector2Int(12, 7);

            // Act: la factory crea solo un pacchetto dati, senza side effect.
            var request = JobRequest.FromDecision(
                "req-food-01",
                42,
                DecisionIntentKind.EatKnownFood,
                JobPriorityClass.Critical,
                1.25f,
                100,
                target,
                "belief:food:12:7",
                "Eat known food");

            // Assert: il target soggettivo attraversa il boundary senza essere ricalcolato.
            Assert.That(request.RequestId, Is.EqualTo("req-food-01"));
            Assert.That(request.NpcId, Is.EqualTo(42));
            Assert.That(request.IntentKind, Is.EqualTo(DecisionIntentKind.EatKnownFood));
            Assert.That(request.PriorityClass, Is.EqualTo(JobPriorityClass.Critical));
            Assert.That(request.Urgency01, Is.EqualTo(1f));
            Assert.That(request.CreatedTick, Is.EqualTo(100));
            Assert.That(request.HasTargetCell, Is.True);
            Assert.That(request.TargetCell, Is.EqualTo(target));
            Assert.That(request.BeliefKey, Is.EqualTo("belief:food:12:7"));
        }

        // =============================================================================
        // JobPlanKeepsOrderedMiniJobPhases
        // =============================================================================
        /// <summary>
        /// <para>
        /// Verifica che il piano conservi l'ordine delle fasi e protegga la propria
        /// lista interna da modifiche esterne.
        /// </para>
        ///
        /// <para><b>Mini job come livello intermedio</b></para>
        /// <para>
        /// L'ordine delle fasi e' il modo con cui v0.06 rappresenta job complessi
        /// prima di introdurre pianificazione GOAP completa.
        /// </para>
        ///
        /// <para><b>Struttura interna:</b></para>
        /// <list type="bullet">
        ///   <item><b>ReachTarget</b>: fase di avvicinamento.</item>
        ///   <item><b>Execute</b>: fase produttiva.</item>
        ///   <item><b>Cleanup</b>: fase di rilascio e chiusura.</item>
        /// </list>
        /// </summary>
        [Test]
        public void JobPlanKeepsOrderedMiniJobPhases()
        {
            // Arrange: tre mini job rappresentano un lavoro complesso ma ancora deterministico.
            var phases = new[]
            {
                new JobPhase("reach", JobPhaseKind.ReachTarget, "Raggiungi target", 1, true),
                new JobPhase("execute", JobPhaseKind.Execute, "Esegui lavoro", 2, false),
                new JobPhase("cleanup", JobPhaseKind.Cleanup, "Chiudi lavoro", 1, true)
            };

            // Act: il piano clona l'array e quindi non dipende piu' dal chiamante.
            var plan = new JobPlan("work-plan", phases);
            phases[0] = new JobPhase("mutated", JobPhaseKind.Custom, "Mutazione esterna", 99, false);

            // Assert: il piano mantiene confini e ordine originali.
            Assert.That(plan.PlanId, Is.EqualTo("work-plan"));
            Assert.That(plan.PhaseCount, Is.EqualTo(3));
            Assert.That(plan.IsEmpty, Is.False);
            Assert.That(plan.TryGetPhase(0, out var first), Is.True);
            Assert.That(first.PhaseId, Is.EqualTo("reach"));
            Assert.That(first.Kind, Is.EqualTo(JobPhaseKind.ReachTarget));
            Assert.That(plan.TryGetPhase(2, out var third), Is.True);
            Assert.That(third.Kind, Is.EqualTo(JobPhaseKind.Cleanup));
            Assert.That(plan.TryGetPhase(3, out _), Is.False);
        }

        // =============================================================================
        // JobTracksStatusAndActivePhaseWithoutExecutingCommands
        // =============================================================================
        /// <summary>
        /// <para>
        /// Verifica che il job conservi stato, fase attiva e fallimento senza
        /// eseguire direttamente logica di mondo.
        /// </para>
        ///
        /// <para><b>Persistenza dello stato di esecuzione</b></para>
        /// <para>
        /// La futura state machine usera' questi campi come memoria operativa. Il
        /// test protegge il fatto che siano aggiornabili in modo esplicito e leggibile.
        /// </para>
        ///
        /// <para><b>Struttura interna:</b></para>
        /// <list type="bullet">
        ///   <item><b>Created</b>: stato iniziale dopo la costruzione.</item>
        ///   <item><b>Running</b>: stato attivo con fase corrente leggibile.</item>
        ///   <item><b>Failed</b>: chiusura diagnostica con motivo normalizzato.</item>
        /// </list>
        /// </summary>
        [Test]
        public void JobTracksStatusAndActivePhaseWithoutExecutingCommands()
        {
            // Arrange: il job nasce da una richiesta senza target, utile per WaitAndObserve.
            var request = JobRequest.WithoutTarget(
                "req-wait-01",
                7,
                DecisionIntentKind.WaitAndObserve,
                JobPriorityClass.Idle,
                -0.5f,
                10,
                "Wait");
            var plan = new JobPlan(
                "wait-plan",
                new[] { new JobPhase("observe", JobPhaseKind.Execute, "Osserva", 1, true) });

            // Act: aggiorniamo solo stato e cursore, senza generare comandi.
            var job = new Job("job-01", request, plan);
            job.MarkRunning(11);
            job.MoveToPhase(0, 12);
            var hasPhase = job.TryGetActivePhase(out var activePhase);
            job.MarkFailed(JobFailureReason.None, 13);

            // Assert: il job resta un contenitore persistente e diagnostico.
            Assert.That(request.Urgency01, Is.EqualTo(0f));
            Assert.That(job.JobId, Is.EqualTo("job-01"));
            Assert.That(job.Status, Is.EqualTo(JobStatus.Failed));
            Assert.That(job.ActivePhaseIndex, Is.EqualTo(0));
            Assert.That(job.CreatedTick, Is.EqualTo(10));
            Assert.That(job.UpdatedTick, Is.EqualTo(13));
            Assert.That(job.FailureReason, Is.EqualTo(JobFailureReason.Unknown));
            Assert.That(hasPhase, Is.True);
            Assert.That(activePhase.PhaseId, Is.EqualTo("observe"));
        }
    }
}
