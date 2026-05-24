using System;

namespace Arcontio.Core
{
    // =============================================================================
    // JobRecoveryResultKind
    // =============================================================================
    /// <summary>
    /// <para>
    /// Vocabolario passivo dei possibili esiti candidati di un futuro tentativo di
    /// recupero locale degli step Job.
    /// </para>
    ///
    /// <para><b>v0.11c.04e - Recovery result vocabulary senza runtime recovery</b></para>
    /// <para>
    /// Questo enum non mappa <c>StepResultStatus</c>, non mappa
    /// <c>JobFailureReason</c>, non decide recoverability e non ordina gli esiti.
    /// Serve solo come lessico data-only per spiegare cosa un futuro boundary di
    /// recovery potrebbe dichiarare dopo aver valutato una strategia locale.
    /// </para>
    ///
    /// <para><b>Struttura interna:</b></para>
    /// <list type="bullet">
    ///   <item><b>None</b>: nessun risultato di recovery dichiarato.</item>
    ///   <item><b>Recovered</b>: il tentativo dichiara recupero locale riuscito.</item>
    ///   <item><b>RetryScheduled</b>: il tentativo dichiara un retry futuro locale.</item>
    ///   <item><b>TargetReplaced</b>: il tentativo dichiara sostituzione target gia' lecita.</item>
    ///   <item><b>PhaseRebuilt</b>: il tentativo dichiara ricostruzione della fase corrente.</item>
    ///   <item><b>PhaseFailed</b>: il tentativo dichiara fallimento della fase.</item>
    ///   <item><b>JobFailed</b>: il tentativo dichiara fallimento del job.</item>
    ///   <item><b>EscalateToDecision</b>: il tentativo dichiara bisogno di rivalutazione globale futura.</item>
    /// </list>
    /// </summary>
    public enum JobRecoveryResultKind
    {
        None = 0,
        Recovered = 10,
        RetryScheduled = 20,
        TargetReplaced = 30,
        PhaseRebuilt = 40,
        PhaseFailed = 50,
        JobFailed = 60,
        EscalateToDecision = 70
    }

    // =============================================================================
    // JobRecoveryResult
    // =============================================================================
    /// <summary>
    /// <para>
    /// DTO passivo che conserva il risultato candidato di un futuro tentativo di
    /// recupero locale degli step Job.
    /// </para>
    ///
    /// <para><b>v0.11c.04e - Result model senza side effect</b></para>
    /// <para>
    /// Questa struttura non applica recovery, non avanza job, non ricostruisce fasi,
    /// non fallisce job, non invoca il Decision Layer, non emette <c>ICommand</c> e
    /// non muta il <c>World</c>. Conserva solo dati diagnostici: kind, strategia
    /// applicata, failure kind osservato, wait suggerito e testo per QA.
    /// </para>
    ///
    /// <para><b>Struttura interna:</b></para>
    /// <list type="bullet">
    ///   <item><b>Kind</b>: esito candidato dichiarato.</item>
    ///   <item><b>AppliedStrategy</b>: strategia che avrebbe prodotto il risultato.</item>
    ///   <item><b>FailureKind</b>: fallimento locale a cui il risultato si riferisce.</item>
    ///   <item><b>SuggestedWaitTicks</b>: attesa suggerita come dato, non scheduler.</item>
    ///   <item><b>Diagnostic</b>: messaggio leggibile per QA ed explainability futura.</item>
    /// </list>
    /// </summary>
    public readonly struct JobRecoveryResult
    {
        public readonly JobRecoveryResultKind Kind;
        public readonly StepRecoveryStrategy AppliedStrategy;
        public readonly JobStepFailureKind FailureKind;
        public readonly int SuggestedWaitTicks;
        public readonly string Diagnostic;

        public bool HasDeclaredResult => Kind != JobRecoveryResultKind.None;

        public JobRecoveryResult(
            JobRecoveryResultKind kind,
            StepRecoveryStrategy appliedStrategy,
            JobStepFailureKind failureKind,
            int suggestedWaitTicks,
            string diagnostic)
        {
            Kind = kind;
            AppliedStrategy = appliedStrategy;
            FailureKind = failureKind;
            SuggestedWaitTicks = Math.Max(0, suggestedWaitTicks);
            Diagnostic = diagnostic ?? string.Empty;
        }

        // =============================================================================
        // None
        // =============================================================================
        /// <summary>
        /// <para>
        /// Costruisce il risultato vuoto, che significa solo "nessun risultato di
        /// recovery dichiarato".
        /// </para>
        ///
        /// <para><b>Assenza di risultato, non fallimento e non successo</b></para>
        /// <para>
        /// Questo valore non rappresenta recovery fallito, recovery riuscito o
        /// decisione terminale. Evita solo null e default ambigui nei test e nei
        /// futuri boundary passivi.
        /// </para>
        /// </summary>
        public static JobRecoveryResult None()
        {
            return new JobRecoveryResult(
                JobRecoveryResultKind.None,
                StepRecoveryStrategy.None,
                JobStepFailureKind.None,
                0,
                "NoRecoveryResultDeclared");
        }

        // =============================================================================
        // FromData
        // =============================================================================
        /// <summary>
        /// <para>
        /// Costruisce un risultato da campi gia' classificati dal chiamante.
        /// </para>
        ///
        /// <para><b>Factory dati, non recovery behavior</b></para>
        /// <para>
        /// Il metodo non interpreta la strategia, non controlla policy, non assegna
        /// target, non pianifica retry e non decide escalation. Normalizza soltanto i
        /// dati primitivi del DTO.
        /// </para>
        /// </summary>
        public static JobRecoveryResult FromData(
            JobRecoveryResultKind kind,
            StepRecoveryStrategy appliedStrategy,
            JobStepFailureKind failureKind,
            int suggestedWaitTicks,
            string diagnostic)
        {
            return new JobRecoveryResult(
                kind,
                appliedStrategy,
                failureKind,
                suggestedWaitTicks,
                diagnostic);
        }
    }
}
