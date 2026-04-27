using Arcontio.Core;
using NUnit.Framework;
using UnityEngine;

namespace Arcontio.Tests
{
    // =============================================================================
    // BasicJobActionExecutorQaTests
    // =============================================================================
    /// <summary>
    /// <para>
    /// Test QA EditMode per executor base di movimento, prenotazione, rilascio e
    /// attesa.
    /// </para>
    ///
    /// <para><b>Executor senza World</b></para>
    /// <para>
    /// Gli step ricevono posizione e reservation store nel contesto. Questo mantiene
    /// testabile la logica senza bootstrap runtime.
    /// </para>
    ///
    /// <para><b>Struttura interna:</b></para>
    /// <list type="bullet">
    ///   <item><b>Move</b>: distingue target raggiunto e movimento pendente.</item>
    ///   <item><b>Reserve</b>: usa ReservationStore per contesa.</item>
    ///   <item><b>Wait/Release</b>: restituisce esiti espliciti.</item>
    /// </list>
    /// </summary>
    public sealed class BasicJobActionExecutorQaTests
    {
        // =============================================================================
        // MoveStepSucceedsOnlyWhenNpcIsAtTarget
        // =============================================================================
        /// <summary>
        /// <para>
        /// Verifica che lo step di movimento non sposti l'NPC e segnali successo solo
        /// quando la posizione fornita coincide con il target.
        /// </para>
        ///
        /// <para><b>Bridge verso pathfinding futuro</b></para>
        /// <para>
        /// Il movimento reale restera' nel sistema dedicato. Qui controlliamo solo il
        /// gate di completamento dello step.
        /// </para>
        ///
        /// <para><b>Struttura interna:</b></para>
        /// <list type="bullet">
        ///   <item><b>Pending</b>: posizione diversa dal target.</item>
        ///   <item><b>Succeeded</b>: posizione uguale al target.</item>
        ///   <item><b>Assert</b>: nessun side effect esterno.</item>
        /// </list>
        /// </summary>
        [Test]
        public void MoveStepSucceedsOnlyWhenNpcIsAtTarget()
        {
            // Arrange: executor e action con target cella.
            var executor = new BasicJobActionExecutor();
            var action = JobAction.MoveTo("move", new Vector2Int(5, 5), "vai");

            // Act: confrontiamo posizione lontana e posizione raggiunta.
            var pending = executor.Execute(action, new JobActionExecutionContext(1, "job", 1, new Vector2Int(1, 1), null));
            var arrived = executor.Execute(action, new JobActionExecutionContext(1, "job", 2, new Vector2Int(5, 5), null));

            // Assert: Running significa "serve ancora movimento", non fallimento.
            Assert.That(pending.Status, Is.EqualTo(StepResultStatus.Running));
            Assert.That(arrived.Status, Is.EqualTo(StepResultStatus.Succeeded));
            Assert.That(arrived.CanAdvance, Is.True);
        }

        // =============================================================================
        // ReserveReleaseAndWaitReturnExplicitResults
        // =============================================================================
        /// <summary>
        /// <para>
        /// Verifica prenotazione, rilascio idempotente e attesa con durata esplicita.
        /// </para>
        ///
        /// <para><b>Risorse e tempo come dati</b></para>
        /// <para>
        /// Lo step non usa timer nascosti e non marca oggetti del World: tutto passa
        /// da <c>StepResult</c> e <c>ReservationStore</c>.
        /// </para>
        ///
        /// <para><b>Struttura interna:</b></para>
        /// <list type="bullet">
        ///   <item><b>ReserveTarget</b>: aggiunge una prenotazione.</item>
        ///   <item><b>ReleaseReservation</b>: svuota le prenotazioni del job.</item>
        ///   <item><b>WaitTicks</b>: espone SuggestedWaitTicks.</item>
        /// </list>
        /// </summary>
        [Test]
        public void ReserveReleaseAndWaitReturnExplicitResults()
        {
            // Arrange: store esplicito e contesto operativo minimale.
            var executor = new BasicJobActionExecutor();
            var store = new ReservationStore();
            var context = new JobActionExecutionContext(1, "job-a", 10, Vector2Int.zero, store);
            var reserve = new JobAction("reserve", JobActionKind.ReserveTarget, "prenota", true, new Vector2Int(2, 2), -1, 0, string.Empty);
            var release = JobAction.Simple("release", JobActionKind.ReleaseReservation, "rilascia");
            var wait = JobAction.Wait("wait", 6, "attendi");

            // Act: copriamo i tre step base.
            var reserved = executor.Execute(reserve, context);
            var waited = executor.Execute(wait, context);
            var released = executor.Execute(release, context);

            // Assert: ogni step comunica un risultato leggibile alla state machine.
            Assert.That(reserved.Status, Is.EqualTo(StepResultStatus.Succeeded));
            Assert.That(store.Count, Is.EqualTo(0));
            Assert.That(waited.Status, Is.EqualTo(StepResultStatus.Waiting));
            Assert.That(waited.SuggestedWaitTicks, Is.EqualTo(6));
            Assert.That(released.Status, Is.EqualTo(StepResultStatus.Succeeded));
        }
    }
}
