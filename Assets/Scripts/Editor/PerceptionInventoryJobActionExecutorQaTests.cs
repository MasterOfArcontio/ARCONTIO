using Arcontio.Core;
using NUnit.Framework;
using UnityEngine;

namespace Arcontio.Tests
{
    // =============================================================================
    // PerceptionInventoryJobActionExecutorQaTests
    // =============================================================================
    /// <summary>
    /// <para>
    /// Test QA EditMode per executor di observe, search, pick up e drop.
    /// </para>
    ///
    /// <para><b>Contratti prima dell'integrazione runtime</b></para>
    /// <para>
    /// I test non verificano percezione o inventario reali. Verificano che gli step
    /// abbiano precondizioni e risultati coerenti per la futura integrazione.
    /// </para>
    ///
    /// <para><b>Struttura interna:</b></para>
    /// <list type="bullet">
    ///   <item><b>Observe/Search</b>: esiti percettivi placeholder.</item>
    ///   <item><b>PickUp/Drop</b>: validazione target materiale.</item>
    /// </list>
    /// </summary>
    public sealed class PerceptionInventoryJobActionExecutorQaTests
    {
        // =============================================================================
        // ObserveSearchPickAndDropExposeExpectedContracts
        // =============================================================================
        /// <summary>
        /// <para>
        /// Verifica i risultati minimi per gli step di osservazione, ricerca e
        /// manipolazione oggetto.
        /// </para>
        ///
        /// <para><b>Precondizioni dichiarative</b></para>
        /// <para>
        /// Pick e drop devono fallire senza target, mentre observe e search restano
        /// step validi anche prima dei sistemi completi.
        /// </para>
        ///
        /// <para><b>Struttura interna:</b></para>
        /// <list type="bullet">
        ///   <item><b>Observe</b>: Succeeded.</item>
        ///   <item><b>Search</b>: Running senza payload, Succeeded con payload.</item>
        ///   <item><b>Pick/Drop</b>: successo solo con target.</item>
        /// </list>
        /// </summary>
        [Test]
        public void ObserveSearchPickAndDropExposeExpectedContracts()
        {
            // Arrange: contesto minimale, senza store e senza World.
            var executor = new PerceptionInventoryJobActionExecutor();
            var context = new JobActionExecutionContext(1, "job", 1, Vector2Int.zero, null);

            // Act: copriamo contratti positivi e negativi.
            var observe = executor.Execute(JobAction.Simple("observe", JobActionKind.Observe, "osserva"), context);
            var searchPending = executor.Execute(JobAction.Simple("search", JobActionKind.Search, "cerca"), context);
            var searchDone = executor.Execute(new JobAction("search-done", JobActionKind.Search, "cerca", false, Vector2Int.zero, -1, 0, "found"), context);
            var pickMissing = executor.Execute(JobAction.Simple("pick-missing", JobActionKind.PickUp, "prendi"), context);
            var pickOk = executor.Execute(new JobAction("pick", JobActionKind.PickUp, "prendi", false, Vector2Int.zero, 33, 0, string.Empty), context);
            var dropOk = executor.Execute(new JobAction("drop", JobActionKind.Drop, "deposita", true, new Vector2Int(2, 2), -1, 0, string.Empty), context);

            // Assert: gli esiti sono leggibili dalla state machine.
            Assert.That(observe.Status, Is.EqualTo(StepResultStatus.Succeeded));
            Assert.That(searchPending.Status, Is.EqualTo(StepResultStatus.Running));
            Assert.That(searchDone.Status, Is.EqualTo(StepResultStatus.Succeeded));
            Assert.That(pickMissing.FailureReason, Is.EqualTo(JobFailureReason.MissingTarget));
            Assert.That(pickOk.Status, Is.EqualTo(StepResultStatus.Succeeded));
            Assert.That(dropOk.Status, Is.EqualTo(StepResultStatus.Succeeded));
        }
    }
}
