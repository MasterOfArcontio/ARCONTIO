namespace Arcontio.Core
{
    // =============================================================================
    // JobArbitrationDecision
    // =============================================================================
    /// <summary>
    /// <para>
    /// Decisione prodotta dall'arbitro quando una nuova richiesta compete con lo
    /// stato job corrente di un NPC.
    /// </para>
    ///
    /// <para><b>Preemption esplicita e progressiva</b></para>
    /// <para>
    /// La v0.06 introduce prima una policy piccola e verificabile. La ladder completa
    /// potra' estendere questi esiti senza cambiare il boundary tra Decision Layer e
    /// Job Execution.
    /// </para>
    ///
    /// <para><b>Struttura interna:</b></para>
    /// <list type="bullet">
    ///   <item><b>AcceptNew</b>: nessun job corrente, la richiesta puo' partire.</item>
    ///   <item><b>KeepCurrent</b>: il job corrente resta proprietario dell'NPC.</item>
    ///   <item><b>SuspendCurrentForNew</b>: il job corrente viene parcheggiato.</item>
    ///   <item><b>CancelCurrentForNew</b>: il job corrente viene chiuso e sostituito.</item>
    ///   <item><b>RejectInvalid</b>: la richiesta nuova non e' utilizzabile.</item>
    /// </list>
    /// </summary>
    public enum JobArbitrationDecision
    {
        AcceptNew = 0,
        KeepCurrent = 10,
        SuspendCurrentForNew = 20,
        CancelCurrentForNew = 30,
        RejectInvalid = 40
    }

    // =============================================================================
    // JobArbitrationResult
    // =============================================================================
    /// <summary>
    /// <para>
    /// Risultato diagnosticabile dell'arbitraggio tra job corrente e richiesta nuova.
    /// </para>
    ///
    /// <para><b>Explainability per il Job System</b></para>
    /// <para>
    /// Anche se la policy e' minima, il risultato conserva una ragione leggibile.
    /// Questo permette ai test runtime e ai JSONL futuri di spiegare perche' un NPC
    /// non ha cambiato lavoro.
    /// </para>
    ///
    /// <para><b>Struttura interna:</b></para>
    /// <list type="bullet">
    ///   <item><b>Decision</b>: esito operativo dell'arbitraggio.</item>
    ///   <item><b>AcceptedJobId</b>: job che dovra' essere attivo dopo la decisione.</item>
    ///   <item><b>Reason</b>: stringa diagnostica breve e stabile.</item>
    /// </list>
    /// </summary>
    public readonly struct JobArbitrationResult
    {
        public readonly JobArbitrationDecision Decision;
        public readonly string AcceptedJobId;
        public readonly string Reason;

        public JobArbitrationResult(JobArbitrationDecision decision, string acceptedJobId, string reason)
        {
            Decision = decision;
            AcceptedJobId = acceptedJobId ?? string.Empty;
            Reason = reason ?? string.Empty;
        }
    }

    // =============================================================================
    // JobArbiter
    // =============================================================================
    /// <summary>
    /// <para>
    /// Servizio puro che decide se una nuova richiesta puo' sostituire o affiancare
    /// il job corrente di un NPC.
    /// </para>
    ///
    /// <para><b>Arbitraggio senza side effect</b></para>
    /// <para>
    /// L'arbitro non modifica <c>NpcJobState</c> e non scrive sul <c>World</c>.
    /// Riceve dati gia' disponibili e restituisce una decisione. La mutazione verra'
    /// applicata da un sistema esplicito negli step successivi.
    /// </para>
    ///
    /// <para><b>Struttura interna:</b></para>
    /// <list type="bullet">
    ///   <item><b>CanAccept</b>: valida richiesta e job nuovo.</item>
    ///   <item><b>Priority comparison</b>: confronta classi discrete.</item>
    ///   <item><b>Urgency tie-break</b>: usa urgenza solo a parita' di classe.</item>
    ///   <item><b>Interruptibility</b>: rispetta la fase corrente se non interrompibile.</item>
    /// </list>
    /// </summary>
    public sealed class JobArbiter
    {
        private const float SamePriorityUrgencyMargin = 0.15f;

        public JobArbitrationResult Evaluate(NpcJobState npcState, Job currentJob, Job newJob)
        {
            // Una richiesta senza job concreto non puo' essere accettata: il Decision
            // Layer ha prodotto intenzione, ma manca ancora il piano eseguibile.
            if (newJob == null)
                return new JobArbitrationResult(JobArbitrationDecision.RejectInvalid, string.Empty, "NewJobMissing");

            // Se l'NPC non ha lavoro attivo, l'arbitro non deve inventare code o
            // preemption: il nuovo job puo' partire immediatamente.
            if (!npcState.HasActiveJob || currentJob == null)
                return new JobArbitrationResult(JobArbitrationDecision.AcceptNew, newJob.JobId, "NpcIdle");

            // Un job corrente in fase non interrompibile viene protetto, salvo futura
            // ladder emergency: qui restiamo conservativi e testabili.
            if (currentJob.TryGetActivePhase(out var phase) && !phase.IsInterruptible)
                return new JobArbitrationResult(JobArbitrationDecision.KeepCurrent, currentJob.JobId, "CurrentPhaseNotInterruptible");

            // Una classe superiore vince in modo netto: critical batte normal,
            // emergency batte critical, senza bisogno di confrontare score continui.
            if (newJob.Request.PriorityClass > currentJob.Request.PriorityClass)
                return new JobArbitrationResult(JobArbitrationDecision.SuspendCurrentForNew, newJob.JobId, "HigherPriorityClass");

            // Una classe inferiore non interrompe. Potra' essere messa in coda da un
            // sistema futuro, ma la base MVP conserva il job corrente.
            if (newJob.Request.PriorityClass < currentJob.Request.PriorityClass)
                return new JobArbitrationResult(JobArbitrationDecision.KeepCurrent, currentJob.JobId, "LowerPriorityClass");

            // A parita' di classe, l'urgenza deve superare un margine minimo: evita
            // oscillazioni tra job quasi equivalenti.
            if (newJob.Request.Urgency01 >= currentJob.Request.Urgency01 + SamePriorityUrgencyMargin)
                return new JobArbitrationResult(JobArbitrationDecision.SuspendCurrentForNew, newJob.JobId, "HigherUrgencySameClass");

            return new JobArbitrationResult(JobArbitrationDecision.KeepCurrent, currentJob.JobId, "CurrentStillPreferred");
        }
    }
}
