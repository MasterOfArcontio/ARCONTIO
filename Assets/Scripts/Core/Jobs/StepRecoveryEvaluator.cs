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
    ///   <item><b>EvaluateLocalRetry</b>: produce solo retry locale bounded quando la policy lo dichiara.</item>
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

        // =============================================================================
        // EvaluateLocalRetry
        // =============================================================================
        /// <summary>
        /// <para>
        /// Valuta il primo recupero produttivo ammesso da v0.14d: retry locale
        /// controllato dello stesso step.
        /// </para>
        ///
        /// <para><b>Retry bounded, non recovery intelligente</b></para>
        /// <para>
        /// Il metodo non cerca target alternativi, non ricostruisce fasi, non emette
        /// command, non legge il <c>World</c> e non invoca il Decision Layer. Produce
        /// <c>RetryScheduled</c> solo se la policy contiene <c>RetrySameAction</c> o
        /// <c>WaitAndRetry</c>, se il contatore non supera <c>MaxRetryCount</c> e se
        /// la finestra temporale non supera <c>MaxRecoveryTicks</c>.
        /// </para>
        /// </summary>
        public JobRecoveryResult EvaluateLocalRetry(
            StepFailureClassification classification,
            StepRecoveryPolicy policy,
            int currentRetryCount,
            int recoveryElapsedTicks)
        {
            if (!classification.HasClassification || !policy.HasDeclaredData)
                return JobRecoveryResult.None();

            if (policy.MaxRetryCount <= 0 || currentRetryCount >= policy.MaxRetryCount)
                return JobRecoveryResult.None();

            if (policy.MaxRecoveryTicks > 0 && recoveryElapsedTicks > policy.MaxRecoveryTicks)
                return JobRecoveryResult.None();

            if (!TryResolveRetryStrategy(policy, out var strategy))
                return JobRecoveryResult.None();

            int waitTicks = ResolveRetryWaitTicks(classification, policy, recoveryElapsedTicks);
            return JobRecoveryResult.FromData(
                JobRecoveryResultKind.RetryScheduled,
                strategy,
                classification.FailureKind,
                waitTicks,
                "LocalRetryScheduled");
        }

        private static bool TryResolveRetryStrategy(StepRecoveryPolicy policy, out StepRecoveryStrategy strategy)
        {
            strategy = StepRecoveryStrategy.None;

            if (policy.Strategies == null)
                return false;

            for (int i = 0; i < policy.Strategies.Length; i++)
            {
                var candidate = policy.Strategies[i];
                if (candidate == StepRecoveryStrategy.RetrySameAction
                    || candidate == StepRecoveryStrategy.WaitAndRetry)
                {
                    strategy = candidate;
                    return true;
                }
            }

            return false;
        }

        private static int ResolveRetryWaitTicks(
            StepFailureClassification classification,
            StepRecoveryPolicy policy,
            int recoveryElapsedTicks)
        {
            int waitTicks = classification.SuggestedWaitTicks > 0
                ? classification.SuggestedWaitTicks
                : 1;

            if (policy.MaxRecoveryTicks <= 0)
                return waitTicks;

            int remainingTicks = policy.MaxRecoveryTicks - recoveryElapsedTicks;
            return remainingTicks > 0
                ? System.Math.Max(1, System.Math.Min(waitTicks, remainingTicks))
                : 1;
        }
    }
}
