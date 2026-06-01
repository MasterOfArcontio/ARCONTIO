namespace Arcontio.Core
{
    // =============================================================================
    // StepFailureClassifier
    // =============================================================================
    /// <summary>
    /// <para>
    /// Classificatore puro che traduce l'esito operativo di uno step Job in una
    /// <see cref="StepFailureClassification"/> leggibile dalla futura recovery.
    /// </para>
    ///
    /// <para><b>v0.14b - Classificazione prima della recovery produttiva</b></para>
    /// <para>
    /// Questo componente non applica strategie, non consulta policy, non programma
    /// retry, non modifica job, non emette command e non legge il <c>World</c>.
    /// Serve solo a trasformare dati gia' disponibili nel punto di execution in un
    /// failure kind stabile e testabile.
    /// </para>
    ///
    /// <para><b>Struttura interna:</b></para>
    /// <list type="bullet">
    ///   <item><b>Classify</b>: produce una classificazione quando lo step e' fallito, bloccato o in attesa significativa.</item>
    ///   <item><b>ResolveFailureKind</b>: mappa reason/status/action/diagnostic verso il vocabolario locale.</item>
    ///   <item><b>Diagnostic helpers</b>: mantengono le stringhe diagnostiche come input, senza renderle authority runtime.</item>
    /// </list>
    /// </summary>
    public static class StepFailureClassifier
    {
        // =============================================================================
        // Classify
        // =============================================================================
        /// <summary>
        /// <para>
        /// Costruisce una classificazione passiva per uno step non riuscito.
        /// </para>
        ///
        /// <para><b>Nessun cambio comportamento</b></para>
        /// <para>
        /// Per <c>Succeeded</c> e <c>Running</c> restituisce una classificazione vuota:
        /// quei risultati non rappresentano ancora un problema operativo da
        /// consegnare alla recovery. Per <c>Failed</c>, <c>Blocked</c> e
        /// <c>Waiting</c> conserva status, reason, action, cursore e diagnostic.
        /// </para>
        /// </summary>
        public static StepFailureClassification Classify(
            StepResult result,
            JobAction action,
            int phaseIndex,
            int actionIndex)
        {
            if (result.Status == StepResultStatus.Succeeded
                || result.Status == StepResultStatus.Running)
            {
                return StepFailureClassification.None(
                    result.Status,
                    result.FailureReason,
                    action.Kind,
                    phaseIndex,
                    actionIndex,
                    result.SuggestedWaitTicks,
                    result.DiagnosticMessage);
            }

            var failureKind = ResolveFailureKind(result, action);
            if (failureKind == JobStepFailureKind.None)
            {
                return StepFailureClassification.None(
                    result.Status,
                    result.FailureReason,
                    action.Kind,
                    phaseIndex,
                    actionIndex,
                    result.SuggestedWaitTicks,
                    result.DiagnosticMessage);
            }

            return StepFailureClassification.FromData(
                failureKind,
                result.FailureReason,
                result.Status,
                action.Kind,
                phaseIndex,
                actionIndex,
                result.SuggestedWaitTicks,
                result.DiagnosticMessage);
        }

        private static JobStepFailureKind ResolveFailureKind(StepResult result, JobAction action)
        {
            if (result.Status == StepResultStatus.Blocked)
            {
                if (IsReservationDiagnostic(result.DiagnosticMessage))
                    return JobStepFailureKind.ReservationConflict;

                if (action.Kind == JobActionKind.MoveToCell)
                    return JobStepFailureKind.PathBlocked;

                return JobStepFailureKind.TargetUnavailable;
            }

            if (result.Status == StepResultStatus.Waiting)
                return JobStepFailureKind.TargetUnavailable;

            if (result.Status != StepResultStatus.Failed)
                return JobStepFailureKind.None;

            if (IsResourceMissingDiagnostic(result.DiagnosticMessage))
                return JobStepFailureKind.ResourceMissing;

            if (action.Kind == JobActionKind.Drop && IsOutputBlockedDiagnostic(result.DiagnosticMessage))
                return JobStepFailureKind.OutputBlocked;

            if (IsTargetInvalidDiagnostic(result.DiagnosticMessage))
                return JobStepFailureKind.TargetInvalid;

            if (IsDoorLockedDiagnostic(result.DiagnosticMessage))
                return JobStepFailureKind.DoorLocked;

            if (IsPathBlockedDiagnostic(result.DiagnosticMessage))
                return JobStepFailureKind.PathBlocked;

            if (IsReservationDiagnostic(result.DiagnosticMessage)
                || result.FailureReason == JobFailureReason.ReservationDenied)
            {
                return JobStepFailureKind.ReservationConflict;
            }

            if (result.FailureReason == JobFailureReason.MissingTarget)
                return JobStepFailureKind.TargetInvalid;

            if (result.FailureReason == JobFailureReason.MovementFailed)
                return JobStepFailureKind.PathBlocked;

            if (result.FailureReason == JobFailureReason.InvalidRequest)
                return JobStepFailureKind.TargetInvalid;

            if (result.FailureReason == JobFailureReason.Cancelled
                || result.FailureReason == JobFailureReason.Preempted)
            {
                return JobStepFailureKind.Interrupted;
            }

            if (action.Kind == JobActionKind.Drop)
                return JobStepFailureKind.OutputBlocked;

            return JobStepFailureKind.InsufficientInformation;
        }

        private static bool IsResourceMissingDiagnostic(string diagnostic)
        {
            return Contains(diagnostic, "Unavailable")
                || Contains(diagnostic, "ResourceMissing")
                || Contains(diagnostic, "ConsumeFoodUnavailable");
        }

        private static bool IsTargetInvalidDiagnostic(string diagnostic)
        {
            return Contains(diagnostic, "Missing")
                || Contains(diagnostic, "ObjectMissing")
                || Contains(diagnostic, "OutOfBounds")
                || Contains(diagnostic, "NoLongerCoLocated")
                || Contains(diagnostic, "NotCommunityStock")
                || Contains(diagnostic, "AlreadyHeld")
                || Contains(diagnostic, "NotHeldByNpc");
        }

        private static bool IsPathBlockedDiagnostic(string diagnostic)
        {
            return Contains(diagnostic, "Blocked")
                || Contains(diagnostic, "Occupied")
                || Contains(diagnostic, "TraversalTarget");
        }

        private static bool IsDoorLockedDiagnostic(string diagnostic)
        {
            return Contains(diagnostic, "TraversalDoorLocked")
                || Contains(diagnostic, "DoorLocked");
        }

        private static bool IsReservationDiagnostic(string diagnostic)
        {
            return Contains(diagnostic, "Reservation")
                || Contains(diagnostic, "Reserved");
        }

        private static bool IsOutputBlockedDiagnostic(string diagnostic)
        {
            return Contains(diagnostic, "Occupied")
                || Contains(diagnostic, "OutputBlocked");
        }

        private static bool Contains(string source, string token)
        {
            return !string.IsNullOrEmpty(source)
                && source.IndexOf(token, System.StringComparison.OrdinalIgnoreCase) >= 0;
        }
    }
}
