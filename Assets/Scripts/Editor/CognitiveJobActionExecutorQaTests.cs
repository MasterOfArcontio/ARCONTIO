using Arcontio.Core;
using NUnit.Framework;
using UnityEngine;

namespace Arcontio.Tests
{
    // =============================================================================
    // CognitiveJobActionExecutorQaTests
    // =============================================================================
    /// <summary>
    /// <para>
    /// Test QA EditMode per executor di consume, communicate ed evaluate.
    /// </para>
    ///
    /// <para><b>Contratti per needs e social layer futuri</b></para>
    /// <para>
    /// I test non consumano realmente risorse e non inviano token. Verificano che il
    /// job system possa rappresentare questi step e validarne gli input minimi.
    /// </para>
    ///
    /// <para><b>Struttura interna:</b></para>
    /// <list type="bullet">
    ///   <item><b>Consume</b>: successo con target, fallimento senza target.</item>
    ///   <item><b>Communicate</b>: successo con payload.</item>
    ///   <item><b>Evaluate</b>: gate logico positivo.</item>
    /// </list>
    /// </summary>
    public sealed class CognitiveJobActionExecutorQaTests
    {
        // =============================================================================
        // CognitiveExecutorValidatesConsumeCommunicateAndEvaluate
        // =============================================================================
        /// <summary>
        /// <para>
        /// Verifica i contratti minimi degli step cognitivi e sociali.
        /// </para>
        ///
        /// <para><b>Step dichiarativi</b></para>
        /// <para>
        /// Ogni step restituisce un risultato esplicito, pronto per essere interpretato
        /// dalla state machine senza conoscere il dominio concreto.
        /// </para>
        ///
        /// <para><b>Struttura interna:</b></para>
        /// <list type="bullet">
        ///   <item><b>Evaluate</b>: Succeeded.</item>
        ///   <item><b>Consume</b>: MissingTarget se incompleto.</item>
        ///   <item><b>Communicate</b>: richiede payload.</item>
        /// </list>
        /// </summary>
        [Test]
        public void CognitiveExecutorValidatesConsumeCommunicateAndEvaluate()
        {
            // Arrange: contesto minimale e executor isolato.
            var executor = new CognitiveJobActionExecutor();
            var context = new JobActionExecutionContext(1, "job", 1, Vector2Int.zero, null);

            // Act: copriamo successi e fallimenti di validazione.
            var evaluate = executor.Execute(JobAction.Simple("eval", JobActionKind.Evaluate, "valuta"), context);
            var consumeMissing = executor.Execute(JobAction.Simple("consume-missing", JobActionKind.Consume, "consuma"), context);
            var consumeOk = executor.Execute(new JobAction("consume", JobActionKind.Consume, "consuma", false, Vector2Int.zero, 44, 0, string.Empty), context);
            var communicateMissing = executor.Execute(JobAction.Simple("comm-missing", JobActionKind.Communicate, "comunica"), context);
            var communicateOk = executor.Execute(new JobAction("comm", JobActionKind.Communicate, "comunica", false, Vector2Int.zero, -1, 0, "danger:door"), context);

            // Assert: le precondizioni restano visibili e non ambigue.
            Assert.That(evaluate.Status, Is.EqualTo(StepResultStatus.Succeeded));
            Assert.That(consumeMissing.FailureReason, Is.EqualTo(JobFailureReason.MissingTarget));
            Assert.That(consumeOk.Status, Is.EqualTo(StepResultStatus.Succeeded));
            Assert.That(communicateMissing.FailureReason, Is.EqualTo(JobFailureReason.MissingTarget));
            Assert.That(communicateOk.Status, Is.EqualTo(StepResultStatus.Succeeded));
        }
    }
}
