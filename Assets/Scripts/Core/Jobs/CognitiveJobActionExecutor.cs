namespace Arcontio.Core
{
    // =============================================================================
    // CognitiveJobActionExecutor
    // =============================================================================
    /// <summary>
    /// <para>
    /// Executor MVP per step di consumo, comunicazione e valutazione.
    /// </para>
    ///
    /// <para><b>Azioni cognitive e sociali come step espliciti</b></para>
    /// <para>
    /// Questi step non devono essere nascosti dentro needs o token system. Il job li
    /// dichiara, l'executor valida il contratto minimo e gli step runtime futuri
    /// applicheranno effetti concreti.
    /// </para>
    ///
    /// <para><b>Struttura interna:</b></para>
    /// <list type="bullet">
    ///   <item><b>Consume</b>: richiede target oggetto o payload risorsa.</item>
    ///   <item><b>Communicate</b>: richiede payload messaggio.</item>
    ///   <item><b>Evaluate</b>: successo immediato come gate logico.</item>
    /// </list>
    /// </summary>
    public sealed class CognitiveJobActionExecutor : IJobActionExecutor
    {
        public bool CanExecute(JobAction action)
        {
            return action.Kind == JobActionKind.Consume
                || action.Kind == JobActionKind.Communicate
                || action.Kind == JobActionKind.Evaluate;
        }

        public StepResult Execute(JobAction action, JobActionExecutionContext context)
        {
            if (action.Kind == JobActionKind.Evaluate)
                return StepResult.Succeeded("EvaluatePassed");

            if (action.Kind == JobActionKind.Consume)
                return action.TargetObjectId >= 0 || !string.IsNullOrEmpty(action.PayloadKey)
                    ? StepResult.Succeeded("ConsumeAccepted")
                    : StepResult.Failed(JobFailureReason.MissingTarget, "ConsumeMissingTarget");

            if (action.Kind == JobActionKind.Communicate)
                return !string.IsNullOrEmpty(action.PayloadKey)
                    ? StepResult.Succeeded("CommunicateAccepted")
                    : StepResult.Failed(JobFailureReason.MissingTarget, "CommunicateMissingPayload");

            return StepResult.Failed(JobFailureReason.StepFailed, "UnsupportedCognitiveAction");
        }
    }
}
