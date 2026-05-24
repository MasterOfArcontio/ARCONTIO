namespace Arcontio.Core
{
    // =============================================================================
    // StepRecoveryEvaluator
    // =============================================================================
    /// <summary>
    /// <para>
    /// Boundary no-op per un futuro evaluator di recupero locale degli step Job.
    /// </para>
    ///
    /// <para><b>v0.11c.05c - Recovery evaluator skeleton senza recovery runtime</b></para>
    /// <para>
    /// Questo tipo rappresenta solo il punto di aggancio futuro tra
    /// <c>StepFailureClassification</c>, <c>StepRecoveryPolicy</c> e
    /// <c>JobRecoveryResult</c>. In questa fase non interpreta classificazioni, non
    /// applica strategie, non programma retry, non sostituisce target, non ricostruisce
    /// fasi, non richiede escalation decisionale e non modifica il comportamento
    /// runtime.
    /// </para>
    ///
    /// <para><b>Struttura interna:</b></para>
    /// <list type="bullet">
    ///   <item><b>EvaluateNoOp</b>: riceve dati passivi e restituisce sempre nessun risultato recovery.</item>
    /// </list>
    /// </summary>
    public sealed class StepRecoveryEvaluator
    {
        // =============================================================================
        // EvaluateNoOp
        // =============================================================================
        /// <summary>
        /// <para>
        /// Valuta in modalita' no-op una classificazione e una policy future.
        /// </para>
        ///
        /// <para><b>Boundary futuro, non recovery produttivo</b></para>
        /// <para>
        /// Il metodo ignora intenzionalmente sia la classificazione sia la policy e
        /// restituisce sempre <c>JobRecoveryResult.None()</c>. Questo protegge il
        /// contratto del boundary senza decidere mapping, recoverability, ordering,
        /// retry, target replacement, phase rebuild o escalation.
        /// </para>
        /// </summary>
        public JobRecoveryResult EvaluateNoOp(
            StepFailureClassification classification,
            StepRecoveryPolicy policy)
        {
            return JobRecoveryResult.None();
        }
    }
}
