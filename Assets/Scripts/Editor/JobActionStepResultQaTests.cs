using Arcontio.Core;
using NUnit.Framework;
using UnityEngine;

namespace Arcontio.Tests
{
    // =============================================================================
    // JobActionStepResultQaTests
    // =============================================================================
    /// <summary>
    /// <para>
    /// Test QA EditMode per azioni atomiche di job e risultati di step.
    /// </para>
    ///
    /// <para><b>Step eseguibili ma non ancora eseguiti</b></para>
    /// <para>
    /// Lo step 02 definisce il linguaggio operativo minimo, non i sistemi che lo
    /// consumeranno. I test verificano quindi forma, ordine e semantica degli esiti
    /// senza invocare movimento, inventario o pathfinding.
    /// </para>
    ///
    /// <para><b>Struttura interna:</b></para>
    /// <list type="bullet">
    ///   <item><b>JobAction</b>: descrive lo step atomico.</item>
    ///   <item><b>JobPhase</b>: conserva la sequenza ordinata di azioni.</item>
    ///   <item><b>StepResult</b>: comunica avanzamento, attesa o fallimento.</item>
    /// </list>
    /// </summary>
    public sealed class JobActionStepResultQaTests
    {
        // =============================================================================
        // JobPhaseStoresDefensiveActionSequence
        // =============================================================================
        /// <summary>
        /// <para>
        /// Verifica che una fase conservi una sequenza ordinata di azioni atomiche e
        /// non venga mutata dall'array originale del chiamante.
        /// </para>
        ///
        /// <para><b>Mini job come sequenza di step</b></para>
        /// <para>
        /// Questo test chiude il punto progettuale: un job contiene fasi, una fase
        /// contiene step ordinati. Ogni livello rimane ispezionabile separatamente.
        /// </para>
        ///
        /// <para><b>Struttura interna:</b></para>
        /// <list type="bullet">
        ///   <item><b>MoveToCell</b>: primo step con target cella.</item>
        ///   <item><b>ReserveTarget</b>: secondo step senza accesso globale.</item>
        ///   <item><b>Consume</b>: terzo step dichiarativo.</item>
        /// </list>
        /// </summary>
        [Test]
        public void JobPhaseStoresDefensiveActionSequence()
        {
            // Arrange: tre step descrivono una fase "mangia cibo noto" a grana fine.
            var target = new Vector2Int(4, 9);
            var actions = new[]
            {
                JobAction.MoveTo("move-food", target, "Raggiungi cibo"),
                JobAction.Simple("reserve-food", JobActionKind.ReserveTarget, "Prenota cibo"),
                JobAction.Simple("consume-food", JobActionKind.Consume, "Consuma cibo")
            };

            // Act: la fase copia la sequenza e aggiorna ExpectedStepCount dai dati reali.
            var phase = new JobPhase("eat-phase", JobPhaseKind.Execute, "Mangia", 99, false, actions);
            actions[0] = JobAction.Simple("mutated", JobActionKind.Custom, "Mutazione esterna");

            // Assert: ordine, target e conteggio restano quelli consegnati al costruttore.
            Assert.That(phase.ExpectedStepCount, Is.EqualTo(3));
            Assert.That(phase.TryGetAction(0, out var first), Is.True);
            Assert.That(first.ActionId, Is.EqualTo("move-food"));
            Assert.That(first.Kind, Is.EqualTo(JobActionKind.MoveToCell));
            Assert.That(first.HasTargetCell, Is.True);
            Assert.That(first.TargetCell, Is.EqualTo(target));
            Assert.That(phase.TryGetAction(2, out var third), Is.True);
            Assert.That(third.Kind, Is.EqualTo(JobActionKind.Consume));
            Assert.That(phase.TryGetAction(3, out _), Is.False);
        }

        // =============================================================================
        // StepResultFactoriesExposeStateMachineIntent
        // =============================================================================
        /// <summary>
        /// <para>
        /// Verifica che le factory di risultato espongano chiaramente cosa dovra'
        /// fare la futura macchina a stati.
        /// </para>
        ///
        /// <para><b>Risultati espliciti invece di bool</b></para>
        /// <para>
        /// Un bool non distingue avanzamento, attesa, blocco e fallimento. Il
        /// contratto nuovo rende questi casi separati e testabili.
        /// </para>
        ///
        /// <para><b>Struttura interna:</b></para>
        /// <list type="bullet">
        ///   <item><b>Succeeded</b>: autorizza avanzamento.</item>
        ///   <item><b>Waiting</b>: conserva durata positiva.</item>
        ///   <item><b>Failed</b>: normalizza motivo mancante.</item>
        /// </list>
        /// </summary>
        [Test]
        public void StepResultFactoriesExposeStateMachineIntent()
        {
            // Arrange/Act: costruiamo i tre esiti chiave senza eseguire alcuno step reale.
            var success = StepResult.Succeeded("ok");
            var waiting = StepResult.Waiting(5, "attesa");
            var failed = StepResult.Failed(JobFailureReason.None, "errore sconosciuto");

            // Assert: la state machine futura potra' leggere direttamente le intenzioni.
            Assert.That(success.CanAdvance, Is.True);
            Assert.That(success.IsTerminalFailure, Is.False);
            Assert.That(waiting.Status, Is.EqualTo(StepResultStatus.Waiting));
            Assert.That(waiting.SuggestedWaitTicks, Is.EqualTo(5));
            Assert.That(failed.IsTerminalFailure, Is.True);
            Assert.That(failed.FailureReason, Is.EqualTo(JobFailureReason.Unknown));
        }
    }
}
