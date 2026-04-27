namespace Arcontio.Core
{
    // =============================================================================
    // PerceptionInventoryJobActionExecutor
    // =============================================================================
    /// <summary>
    /// <para>
    /// Executor MVP per step dichiarativi di osservazione, ricerca, raccolta e
    /// deposito.
    /// </para>
    ///
    /// <para><b>Integrazione progressiva con sistemi futuri</b></para>
    /// <para>
    /// Osservazione e ricerca saranno collegati a perception/belief; pick e drop a
    /// inventario o stock. In questo step fissiamo le precondizioni minime e gli
    /// esiti, evitando accesso diretto a store globali.
    /// </para>
    ///
    /// <para><b>Struttura interna:</b></para>
    /// <list type="bullet">
    ///   <item><b>Observe</b>: successo immediato come placeholder percettivo.</item>
    ///   <item><b>Search</b>: running se la ricerca non ha payload conclusivo.</item>
    ///   <item><b>PickUp</b>: richiede target object o payload.</item>
    ///   <item><b>Drop</b>: richiede target o payload di destinazione.</item>
    /// </list>
    /// </summary>
    public sealed class PerceptionInventoryJobActionExecutor : IJobActionExecutor
    {
        public bool CanExecute(JobAction action)
        {
            return action.Kind == JobActionKind.Observe
                || action.Kind == JobActionKind.Search
                || action.Kind == JobActionKind.PickUp
                || action.Kind == JobActionKind.Drop;
        }

        public StepResult Execute(JobAction action, JobActionExecutionContext context)
        {
            if (action.Kind == JobActionKind.Observe)
                return StepResult.Succeeded("ObserveCompleted");

            if (action.Kind == JobActionKind.Search)
                return string.IsNullOrEmpty(action.PayloadKey)
                    ? StepResult.Running("SearchInProgress")
                    : StepResult.Succeeded("SearchCompleted");

            if (action.Kind == JobActionKind.PickUp)
                return HasMaterialTarget(action)
                    ? StepResult.Succeeded("PickUpAccepted")
                    : StepResult.Failed(JobFailureReason.MissingTarget, "PickUpMissingTarget");

            if (action.Kind == JobActionKind.Drop)
                return HasMaterialTarget(action)
                    ? StepResult.Succeeded("DropAccepted")
                    : StepResult.Failed(JobFailureReason.MissingTarget, "DropMissingTarget");

            return StepResult.Failed(JobFailureReason.StepFailed, "UnsupportedPerceptionInventoryAction");
        }

        private static bool HasMaterialTarget(JobAction action)
        {
            // Pick/drop possono riferirsi a un oggetto, a una cella o a una chiave di
            // payload controllata da un executor piu' specifico.
            return action.TargetObjectId >= 0 || action.HasTargetCell || !string.IsNullOrEmpty(action.PayloadKey);
        }
    }
}
