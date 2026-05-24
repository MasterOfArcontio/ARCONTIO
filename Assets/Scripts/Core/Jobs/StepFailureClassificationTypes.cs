using System;

namespace Arcontio.Core
{
    // =============================================================================
    // StepFailureClassification
    // =============================================================================
    /// <summary>
    /// <para>
    /// DTO passivo per rappresentare una futura classificazione step-local di un
    /// fallimento o blocco operativo.
    /// </para>
    ///
    /// <para><b>v0.11c.05b - Classificazione senza mapping produttivo</b></para>
    /// <para>
    /// Questa struttura non classifica automaticamente <c>StepResult</c>, non legge
    /// <c>DiagnosticMessage</c>, non mappa <c>JobFailureReason</c>, non decide
    /// recuperabilita' e non produce <c>JobRecoveryResult</c>. Conserva solo dati
    /// dichiarati da un futuro classifier/evaluator passivo.
    /// </para>
    ///
    /// <para><b>Struttura interna:</b></para>
    /// <list type="bullet">
    ///   <item><b>HasClassification</b>: indica se una classificazione e' stata dichiarata.</item>
    ///   <item><b>FailureKind</b>: failure kind step-local futuro, se noto.</item>
    ///   <item><b>SourceFailureReason</b>: reason runtime originaria, senza mapping implicito.</item>
    ///   <item><b>SourceStatus</b>: status dello step osservato o riportato dal chiamante.</item>
    ///   <item><b>ActionKind</b>: action kind sorgente, se disponibile.</item>
    ///   <item><b>PhaseIndex/ActionIndex</b>: coordinate opzionali del cursore job.</item>
    ///   <item><b>SuggestedWaitTicks</b>: attesa suggerita originaria, se presente.</item>
    ///   <item><b>Diagnostic</b>: testo diagnostico conservato come dato, non interpretato.</item>
    /// </list>
    /// </summary>
    public readonly struct StepFailureClassification
    {
        public readonly bool HasClassification;
        public readonly JobStepFailureKind FailureKind;
        public readonly JobFailureReason SourceFailureReason;
        public readonly StepResultStatus SourceStatus;
        public readonly JobActionKind ActionKind;
        public readonly int PhaseIndex;
        public readonly int ActionIndex;
        public readonly int SuggestedWaitTicks;
        public readonly string Diagnostic;

        public StepFailureClassification(
            bool hasClassification,
            JobStepFailureKind failureKind,
            JobFailureReason sourceFailureReason,
            StepResultStatus sourceStatus,
            JobActionKind actionKind,
            int phaseIndex,
            int actionIndex,
            int suggestedWaitTicks,
            string diagnostic)
        {
            HasClassification = hasClassification;
            FailureKind = hasClassification ? failureKind : JobStepFailureKind.None;
            SourceFailureReason = sourceFailureReason;
            SourceStatus = sourceStatus;
            ActionKind = actionKind;
            PhaseIndex = Math.Max(-1, phaseIndex);
            ActionIndex = Math.Max(-1, actionIndex);
            SuggestedWaitTicks = Math.Max(0, suggestedWaitTicks);
            Diagnostic = diagnostic ?? string.Empty;
        }

        // =============================================================================
        // None
        // =============================================================================
        /// <summary>
        /// <para>
        /// Crea una classificazione vuota conservando eventuali dati sorgente.
        /// </para>
        ///
        /// <para><b>Assenza di classificazione, non policy di recovery</b></para>
        /// <para>
        /// Il valore vuoto non significa "non recuperabile", non significa
        /// "fallimento terminale" e non significa "recovery ammessa". Segnala solo
        /// che nessuna classificazione step-local e' stata dichiarata.
        /// </para>
        /// </summary>
        public static StepFailureClassification None(
            StepResultStatus sourceStatus = StepResultStatus.Running,
            JobFailureReason sourceFailureReason = JobFailureReason.None,
            JobActionKind actionKind = JobActionKind.None,
            int phaseIndex = -1,
            int actionIndex = -1,
            int suggestedWaitTicks = 0,
            string diagnostic = "")
        {
            return new StepFailureClassification(
                false,
                JobStepFailureKind.None,
                sourceFailureReason,
                sourceStatus,
                actionKind,
                phaseIndex,
                actionIndex,
                suggestedWaitTicks,
                diagnostic);
        }

        // =============================================================================
        // FromData
        // =============================================================================
        /// <summary>
        /// <para>
        /// Crea una classificazione dichiarata a partire da dati gia' decisi dal
        /// chiamante.
        /// </para>
        ///
        /// <para><b>Factory dati, non classifier</b></para>
        /// <para>
        /// Questa factory non decide il <c>FailureKind</c>: lo riceve gia' pronto.
        /// Non interpreta status, reason o diagnostic e non produce side effect.
        /// </para>
        /// </summary>
        public static StepFailureClassification FromData(
            JobStepFailureKind failureKind,
            JobFailureReason sourceFailureReason,
            StepResultStatus sourceStatus,
            JobActionKind actionKind,
            int phaseIndex,
            int actionIndex,
            int suggestedWaitTicks,
            string diagnostic)
        {
            return new StepFailureClassification(
                failureKind != JobStepFailureKind.None,
                failureKind,
                sourceFailureReason,
                sourceStatus,
                actionKind,
                phaseIndex,
                actionIndex,
                suggestedWaitTicks,
                diagnostic);
        }
    }
}
