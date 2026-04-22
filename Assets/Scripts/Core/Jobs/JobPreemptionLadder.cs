namespace Arcontio.Core
{
    // =============================================================================
    // JobPreemptionLadder
    // =============================================================================
    /// <summary>
    /// <para>
    /// Policy ordinata che decide se un nuovo job puo' interrompere quello corrente.
    /// </para>
    ///
    /// <para><b>Ladder di priorita' esplicita</b></para>
    /// <para>
    /// La preemption non deve dipendere da if sparsi. Questa classe concentra le
    /// regole in ordine leggibile: validita', assenza job, emergency, fase protetta,
    /// classe priorita', urgenza a parita'.
    /// </para>
    ///
    /// <para><b>Struttura interna:</b></para>
    /// <list type="bullet">
    ///   <item><b>Emergency override</b>: Emergency supera anche fase non interrompibile.</item>
    ///   <item><b>Protected phase</b>: protegge da job non emergency.</item>
    ///   <item><b>Priority class</b>: confronto discreto principale.</item>
    ///   <item><b>Urgency margin</b>: tie-break anti oscillazione.</item>
    /// </list>
    /// </summary>
    public sealed class JobPreemptionLadder
    {
        private const float SameClassMargin = 0.20f;

        public JobArbitrationResult Evaluate(NpcJobState npcState, Job currentJob, Job newJob)
        {
            // La richiesta nuova deve essere materializzata in un job con piano.
            if (newJob == null)
                return new JobArbitrationResult(JobArbitrationDecision.RejectInvalid, string.Empty, "NewJobMissing");

            // Senza job attivo, la ladder non entra davvero in gioco.
            if (!npcState.HasActiveJob || currentJob == null)
                return new JobArbitrationResult(JobArbitrationDecision.AcceptNew, newJob.JobId, "NpcIdle");

            var newPriority = newJob.Request.PriorityClass;
            var currentPriority = currentJob.Request.PriorityClass;

            // Emergency e' l'unica eccezione immediata alla protezione della fase:
            // fame estrema, sicurezza o eventi futuri critici devono poter sbloccare.
            if (newPriority == JobPriorityClass.Emergency && currentPriority != JobPriorityClass.Emergency)
                return new JobArbitrationResult(JobArbitrationDecision.CancelCurrentForNew, newJob.JobId, "EmergencyOverride");

            // Le fasi non interrompibili restano protette per tutte le altre classi.
            if (currentJob.TryGetActivePhase(out var phase) && !phase.IsInterruptible)
                return new JobArbitrationResult(JobArbitrationDecision.KeepCurrent, currentJob.JobId, "ProtectedPhase");

            if (newPriority > currentPriority)
                return new JobArbitrationResult(JobArbitrationDecision.SuspendCurrentForNew, newJob.JobId, "PriorityClassWins");

            if (newPriority < currentPriority)
                return new JobArbitrationResult(JobArbitrationDecision.KeepCurrent, currentJob.JobId, "PriorityClassLoses");

            if (newJob.Request.Urgency01 >= currentJob.Request.Urgency01 + SameClassMargin)
                return new JobArbitrationResult(JobArbitrationDecision.SuspendCurrentForNew, newJob.JobId, "UrgencyMarginWins");

            return new JobArbitrationResult(JobArbitrationDecision.KeepCurrent, currentJob.JobId, "NoPreemptionRuleMatched");
        }
    }
}
