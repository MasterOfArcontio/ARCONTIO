namespace Arcontio.Core
{
    // =============================================================================
    // StepRecoveryStrategy
    // =============================================================================
    /// <summary>
    /// <para>
    /// Vocabolario passivo delle strategie candidate che un futuro boundary di
    /// recupero locale potra' considerare dopo un fallimento di step.
    /// </para>
    ///
    /// <para><b>v0.11c.04c - Strategy vocabulary senza recovery produttiva</b></para>
    /// <para>
    /// Questo enum non mappa <c>JobStepFailureKind</c>, non mappa
    /// <c>StepResultStatus</c>, non stabilisce recuperabilita', priorita',
    /// retry count, budget temporali o raggio di ricerca. Serve solo come lessico
    /// data-only per i futuri modelli passivi di recovery policy e recovery result.
    /// </para>
    ///
    /// <para><b>Struttura interna:</b></para>
    /// <list type="bullet">
    ///   <item><b>None</b>: nessuna strategia locale valida.</item>
    ///   <item><b>RetrySameAction</b>: riprovare la stessa azione senza cambiare target o fase.</item>
    ///   <item><b>WaitAndRetry</b>: attendere localmente e riprovare in seguito.</item>
    ///   <item><b>FindEquivalentTarget</b>: cercare un target equivalente gia' lecito per il job.</item>
    ///   <item><b>FindAlternateCell</b>: cercare una cella alternativa locale e limitata.</item>
    ///   <item><b>Repath</b>: richiedere un nuovo percorso locale verso lo stesso obiettivo operativo.</item>
    ///   <item><b>RequestAssistance</b>: rappresentare una futura richiesta dichiarativa di aiuto senza comunicazione produttiva.</item>
    ///   <item><b>RelaxLocalCriteria</b>: rilassare criteri locali gia' autorizzati dalla policy futura.</item>
    ///   <item><b>RebuildCurrentPhase</b>: rigenerare la fase corrente senza scegliere un nuovo obiettivo cognitivo.</item>
    ///   <item><b>FailPhase</b>: chiudere la fase corrente come fallita.</item>
    ///   <item><b>FailJob</b>: chiudere l'intero job come fallito.</item>
    ///   <item><b>EscalateToDecision</b>: richiedere futura rivalutazione cognitiva globale senza preemption diretta.</item>
    /// </list>
    /// </summary>
    public enum StepRecoveryStrategy
    {
        None = 0,
        RetrySameAction = 10,
        WaitAndRetry = 20,
        FindEquivalentTarget = 30,
        FindAlternateCell = 40,
        Repath = 50,
        RequestAssistance = 60,
        RelaxLocalCriteria = 70,
        RebuildCurrentPhase = 80,
        FailPhase = 90,
        FailJob = 100,
        EscalateToDecision = 110
    }
}
