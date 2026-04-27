namespace Arcontio.Core
{
    // =============================================================================
    // DecisionInputAuditResult
    // =============================================================================
    /// <summary>
    /// <para>
    /// Risultato dell'audit sugli input ammessi al Decision Layer.
    /// </para>
    ///
    /// <para><b>Audit esplicito del vincolo di onniscienza</b></para>
    /// <para>
    /// Il risultato non prova l'assenza di ogni possibile violazione futura, ma
    /// documenta e verifica che il contesto MVP lavori su dati per-NPC e belief
    /// soggettivi, senza richiedere <c>World</c> o <c>MemoryStore</c>.
    /// </para>
    ///
    /// <para><b>Struttura interna:</b></para>
    /// <list type="bullet">
    ///   <item><b>IsValid</b>: true se gli input minimi sono presenti.</item>
    ///   <item><b>MissingRequiredInputCount</b>: numero di componenti obbligatori assenti.</item>
    ///   <item><b>Notes</b>: spiegazione compatta per test e debug.</item>
    /// </list>
    /// </summary>
    public readonly struct DecisionInputAuditResult
    {
        public readonly bool IsValid;
        public readonly int MissingRequiredInputCount;
        public readonly string Notes;

        public DecisionInputAuditResult(bool isValid, int missingRequiredInputCount, string notes)
        {
            IsValid = isValid;
            MissingRequiredInputCount = missingRequiredInputCount;
            Notes = notes ?? string.Empty;
        }
    }

    // =============================================================================
    // DecisionInputAudit
    // =============================================================================
    /// <summary>
    /// <para>
    /// Utility QA che controlla la forma del contesto di input del Decision Layer.
    /// </para>
    ///
    /// <para><b>Lista bianca degli input</b></para>
    /// <para>
    /// L'audit accetta solo il contesto gia' costruito dal chiamante: DNA, profilo,
    /// bisogni, posizione, BeliefStore soggettivo, config query e frame norm/schedule.
    /// Non riceve un <c>World</c>, non riceve un <c>MemoryStore</c> e non puo'
    /// interrogare oggetti o risorse globali.
    /// </para>
    ///
    /// <para><b>Struttura interna:</b></para>
    /// <list type="bullet">
    ///   <item><b>Required</b>: DNA e profilo runtime devono esistere.</item>
    ///   <item><b>Needs</b>: l'array dei bisogni deve avere la dimensione attesa.</item>
    ///   <item><b>Beliefs</b>: lo store puo' essere vuoto, ma deve essere per-NPC.</item>
    /// </list>
    /// </summary>
    public static class DecisionInputAudit
    {
        // =============================================================================
        // Audit
        // =============================================================================
        /// <summary>
        /// <para>
        /// Esegue l'audit minimale su un <c>DecisionEvaluationContext</c>.
        /// </para>
        ///
        /// <para><b>Controllo strutturale, non semantico</b></para>
        /// <para>
        /// La funzione non decide quale intenzione sia corretta. Verifica soltanto che
        /// il contesto contenga i componenti soggettivi necessari e non richieda input
        /// vietati dal vincolo di onniscienza.
        /// </para>
        ///
        /// <para><b>Struttura interna:</b></para>
        /// <list type="bullet">
        ///   <item><b>Missing count</b>: accumula assenze dei componenti richiesti.</item>
        ///   <item><b>Notes</b>: produce una descrizione leggibile e stabile.</item>
        ///   <item><b>Result</b>: restituisce un payload puro per test e overlay futuri.</item>
        /// </list>
        /// </summary>
        public static DecisionInputAuditResult Audit(in DecisionEvaluationContext context)
        {
            int missing = 0;
            string notes = string.Empty;

            if (context.Dna == null)
            {
                missing++;
                notes += "MissingDna;";
            }

            if (context.Profile == null)
            {
                missing++;
                notes += "MissingProfile;";
            }

            if (context.Needs.States == null || context.Needs.States.Length < (int)NeedKind.COUNT)
            {
                missing++;
                notes += "MissingNeeds;";
            }

            if (context.Beliefs == null)
            {
                // Il BeliefStore e' richiesto per le query con target, ma puo' essere
                // vuoto: uno store nullo rende ambiguo il confine col MemoryStore.
                missing++;
                notes += "MissingBeliefStore;";
            }

            if (string.IsNullOrEmpty(notes))
                notes = "DecisionInputWhitelist:Needs,Dna,Profile,Position,Beliefs,QueryConfig,ScheduleFrame,NormContext";

            return new DecisionInputAuditResult(missing == 0, missing, notes);
        }
    }
}
