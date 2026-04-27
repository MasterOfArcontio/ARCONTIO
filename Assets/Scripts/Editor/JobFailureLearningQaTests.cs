using Arcontio.Core;
using NUnit.Framework;

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
    }
}
