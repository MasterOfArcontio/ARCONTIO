using System;

namespace Arcontio.Core
{
    // =============================================================================
    // StepRecoveryPolicy
    // =============================================================================
    /// <summary>
    /// <para>
    /// Modello dati passivo che descrive una possibile policy dichiarativa di
    /// recupero locale per un fallimento di step Job.
    /// </para>
    ///
    /// <para><b>v0.11c.04d - Policy passiva senza recovery produttiva</b></para>
    /// <para>
    /// Questa struttura non mappa <c>JobStepFailureKind</c> a strategie, non mappa
    /// <c>StepResultStatus</c>, non mappa <c>JobFailureReason</c>, non decide se un
    /// fallimento sia recuperabile e non cabla alcun boundary runtime. Conserva solo
    /// dati dichiarativi che una futura policy produttiva potra' interpretare sotto
    /// limiti espliciti.
    /// </para>
    ///
    /// <para><b>Struttura interna:</b></para>
    /// <list type="bullet">
    ///   <item><b>FailureKind</b>: categoria step-local a cui la policy potrebbe riferirsi.</item>
    ///   <item><b>Strategies</b>: strategie candidate dichiarate, senza ordine produttivo.</item>
    ///   <item><b>MaxRetryCount</b>: limite dati opzionale sui retry.</item>
    ///   <item><b>MaxRecoveryTicks</b>: limite dati opzionale sulla durata di recovery.</item>
    ///   <item><b>MaxSearchRadius</b>: limite dati opzionale sul raggio locale.</item>
    ///   <item><b>MaxAlternativeTargets</b>: limite dati opzionale sui target alternativi.</item>
    ///   <item><b>MaxRepathAttempts</b>: limite dati opzionale sui tentativi di repath.</item>
    /// </list>
    /// </summary>
    public readonly struct StepRecoveryPolicy
    {
        public readonly JobStepFailureKind FailureKind;
        public readonly StepRecoveryStrategy[] Strategies;
        public readonly int MaxRetryCount;
        public readonly int MaxRecoveryTicks;
        public readonly int MaxSearchRadius;
        public readonly int MaxAlternativeTargets;
        public readonly int MaxRepathAttempts;

        public bool HasDeclaredData =>
            FailureKind != JobStepFailureKind.None
            || Strategies.Length > 0
            || MaxRetryCount > 0
            || MaxRecoveryTicks > 0
            || MaxSearchRadius > 0
            || MaxAlternativeTargets > 0
            || MaxRepathAttempts > 0;

        public StepRecoveryPolicy(
            JobStepFailureKind failureKind,
            StepRecoveryStrategy[] strategies,
            int maxRetryCount,
            int maxRecoveryTicks,
            int maxSearchRadius,
            int maxAlternativeTargets,
            int maxRepathAttempts)
        {
            FailureKind = failureKind;
            Strategies = strategies ?? Array.Empty<StepRecoveryStrategy>();
            MaxRetryCount = Math.Max(0, maxRetryCount);
            MaxRecoveryTicks = Math.Max(0, maxRecoveryTicks);
            MaxSearchRadius = Math.Max(0, maxSearchRadius);
            MaxAlternativeTargets = Math.Max(0, maxAlternativeTargets);
            MaxRepathAttempts = Math.Max(0, maxRepathAttempts);
        }

        // =============================================================================
        // Empty
        // =============================================================================
        /// <summary>
        /// <para>
        /// Costruisce una policy vuota che significa solo "nessuna policy
        /// dichiarata".
        /// </para>
        ///
        /// <para><b>Assenza di policy, non decisione di recovery</b></para>
        /// <para>
        /// Il valore vuoto non significa che il recovery sia vietato e non significa
        /// che il recovery sia ammesso. Evita solo null e default ambigui nei test e
        /// nei futuri DTO.
        /// </para>
        /// </summary>
        public static StepRecoveryPolicy Empty()
        {
            return new StepRecoveryPolicy(
                JobStepFailureKind.None,
                Array.Empty<StepRecoveryStrategy>(),
                0,
                0,
                0,
                0,
                0);
        }

        // =============================================================================
        // ContainsStrategy
        // =============================================================================
        /// <summary>
        /// <para>
        /// Verifica se la strategia e' presente nei dati dichiarati dalla policy.
        /// </para>
        ///
        /// <para><b>Lookup dati, non autorizzazione produttiva</b></para>
        /// <para>
        /// Il risultato non decide che una strategia sia ammessa, ordinata o
        /// recuperabile per un failure kind. Controlla solo la presenza del valore
        /// nell'array data-only.
        /// </para>
        /// </summary>
        public bool ContainsStrategy(StepRecoveryStrategy strategy)
        {
            if (strategy == StepRecoveryStrategy.None)
            {
                return false;
            }

            for (int i = 0; i < Strategies.Length; i++)
            {
                if (Strategies[i] == strategy)
                {
                    return true;
                }
            }

            return false;
        }
    }
}
