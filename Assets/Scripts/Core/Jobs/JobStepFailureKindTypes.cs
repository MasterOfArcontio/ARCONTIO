namespace Arcontio.Core
{
    // =============================================================================
    // JobStepFailureKind
    // =============================================================================
    /// <summary>
    /// <para>
    /// Vocabolario passivo dei fallimenti locali che uno step di job potra'
    /// classificare prima di qualsiasi futura policy di recovery.
    /// </para>
    ///
    /// <para><b>v0.11c.04b - Failure vocabulary senza recovery produttiva</b></para>
    /// <para>
    /// Questo enum non sostituisce <c>JobFailureReason</c>, non mappa
    /// automaticamente <c>StepResult</c>, non decide se un fallimento sia
    /// recuperabile e non modifica il runtime. Serve solo come lessico leggibile per
    /// i prossimi modelli passivi: strategy, policy e recovery result.
    /// </para>
    ///
    /// <para><b>Struttura interna:</b></para>
    /// <list type="bullet">
    ///   <item><b>None</b>: nessuna classificazione locale valida.</item>
    ///   <item><b>TargetInvalid</b>: il target dello step e' incoerente o non piu' valido.</item>
    ///   <item><b>TargetUnavailable</b>: il target esiste concettualmente ma non e' disponibile ora.</item>
    ///   <item><b>PathBlocked</b>: il percorso locale o la cella di avanzamento sono bloccati.</item>
    ///   <item><b>AccessDenied</b>: una regola operativa impedisce l'accesso al target.</item>
    ///   <item><b>ReservationConflict</b>: una reservation esistente impedisce lo step.</item>
    ///   <item><b>ResourceMissing</b>: la risorsa consumabile o materiale richiesta manca.</item>
    ///   <item><b>ActorInventoryFull</b>: l'attore non puo' ricevere ulteriore carico.</item>
    ///   <item><b>ActorIncapacitated</b>: l'attore non puo' eseguire fisicamente lo step.</item>
    ///   <item><b>Timeout</b>: una durata massima locale e' stata superata.</item>
    ///   <item><b>Interrupted</b>: lo step o running action e' stato fermato da autorita' esterna.</item>
    ///   <item><b>InsufficientInformation</b>: mancano dati locali affidabili per procedere.</item>
    ///   <item><b>OutputBlocked</b>: il risultato dello step non puo' essere depositato o applicato.</item>
    /// </list>
    /// </summary>
    public enum JobStepFailureKind
    {
        None = 0,
        TargetInvalid = 10,
        TargetUnavailable = 20,
        PathBlocked = 30,
        AccessDenied = 40,
        ReservationConflict = 50,
        ResourceMissing = 60,
        ActorInventoryFull = 70,
        ActorIncapacitated = 80,
        Timeout = 90,
        Interrupted = 100,
        InsufficientInformation = 110,
        OutputBlocked = 120
    }
}
