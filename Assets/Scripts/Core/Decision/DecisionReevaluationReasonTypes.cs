namespace Arcontio.Core
{
    // =============================================================================
    // DecisionReevaluationReasonKind
    // =============================================================================
    /// <summary>
    /// <para>
    /// Vocabolario passivo dei motivi per cui un NPC puo' essere considerato per una
    /// futura rivalutazione decisionale.
    /// </para>
    ///
    /// <para><b>Rivalutazione decisionale, non preemption</b></para>
    /// <para>
    /// Questi valori non rappresentano eventi sorgente produttivi, priorita',
    /// urgenze, invalidazioni o decisioni del <c>JobArbiter</c>. Descrivono soltanto
    /// la domanda diagnostica "perche' il sistema sta considerando una nuova
    /// decisione per questo NPC?". L'autorita' di interrompere, accettare o rifiutare
    /// job resta nel Job Layer.
    /// </para>
    ///
    /// <para><b>Struttura interna:</b></para>
    /// <list type="bullet">
    ///   <item><b>None</b>: nessun motivo valido.</item>
    ///   <item><b>Periodic</b>: rivalutazione ordinaria legata alla cadence.</item>
    ///   <item><b>NeedAlert</b>: un bisogno e' in stato di allerta.</item>
    ///   <item><b>NeedCritical</b>: un bisogno e' in stato critico.</item>
    ///   <item><b>JobCompleted</b>: il job precedente e' terminato positivamente.</item>
    ///   <item><b>JobFailed</b>: il job precedente e' fallito.</item>
    ///   <item><b>ExternalEvent</b>: un segnale esterno gia' classificato come rilevante.</item>
    ///   <item><b>ManualDebug</b>: richiesta manuale o diagnostica.</item>
    /// </list>
    /// </summary>
    public enum DecisionReevaluationReasonKind
    {
        None = 0,
        Periodic = 10,
        NeedAlert = 20,
        NeedCritical = 30,
        JobCompleted = 40,
        JobFailed = 50,
        ExternalEvent = 60,
        ManualDebug = 70
    }

    // =============================================================================
    // DecisionReevaluationReason
    // =============================================================================
    /// <summary>
    /// <para>
    /// DTO immutabile che descrive un singolo motivo passivo di rivalutazione
    /// decisionale per un NPC.
    /// </para>
    ///
    /// <para><b>Explainability futura senza side effect</b></para>
    /// <para>
    /// La struttura non schedula NPC, non invoca pipeline cognitive, non emette
    /// command, non crea <c>JobRequest</c> e non modifica stato job. Conserva solo
    /// campi primitivi e value type utili a test, log e pannelli futuri. La presenza
    /// di un motivo valido non implica che la rivalutazione avvenga subito.
    /// </para>
    ///
    /// <para><b>Struttura interna:</b></para>
    /// <list type="bullet">
    ///   <item><b>Kind</b>: categoria normalizzata del motivo.</item>
    ///   <item><b>Need</b>: bisogno collegato, quando il motivo nasce da alert/critical.</item>
    ///   <item><b>JobFailure</b>: fallimento job collegato, quando disponibile.</item>
    ///   <item><b>SourceLabel</b>: etichetta diagnostica della sorgente gia' classificata.</item>
    ///   <item><b>DiagnosticLabel</b>: testo breve per QA ed explainability.</item>
    /// </list>
    /// </summary>
    public readonly struct DecisionReevaluationReason
    {
        public readonly DecisionReevaluationReasonKind Kind;
        public readonly NeedKind Need;
        public readonly JobFailureReason JobFailure;
        public readonly string SourceLabel;
        public readonly string DiagnosticLabel;

        public bool IsValid => Kind != DecisionReevaluationReasonKind.None;

        public DecisionReevaluationReason(
            DecisionReevaluationReasonKind kind,
            NeedKind need,
            JobFailureReason jobFailure,
            string sourceLabel,
            string diagnosticLabel)
        {
            Kind = kind;
            Need = need;
            JobFailure = jobFailure;
            SourceLabel = sourceLabel ?? string.Empty;
            DiagnosticLabel = diagnosticLabel ?? string.Empty;
        }

        // =============================================================================
        // None
        // =============================================================================
        /// <summary>
        /// <para>
        /// Costruisce il motivo nullo, usato quando nessuna causa valida di
        /// rivalutazione e' presente.
        /// </para>
        ///
        /// <para><b>Assenza esplicita di causa</b></para>
        /// <para>
        /// Usare un valore dati esplicito evita di confondere un DTO default con una
        /// rivalutazione periodica o con un segnale esterno. Anche questo valore non
        /// ha effetti runtime.
        /// </para>
        ///
        /// <para><b>Struttura interna:</b></para>
        /// <list type="bullet">
        ///   <item><b>Kind</b>: <c>None</c>.</item>
        ///   <item><b>Need</b>: sentinella <c>NeedKind.COUNT</c>.</item>
        ///   <item><b>DiagnosticLabel</b>: etichetta stabile per test.</item>
        /// </list>
        /// </summary>
        public static DecisionReevaluationReason None()
        {
            return new DecisionReevaluationReason(
                DecisionReevaluationReasonKind.None,
                NeedKind.COUNT,
                JobFailureReason.None,
                string.Empty,
                "NoValidReason");
        }

        public static DecisionReevaluationReason Periodic(string sourceLabel = "")
        {
            return new DecisionReevaluationReason(
                DecisionReevaluationReasonKind.Periodic,
                NeedKind.COUNT,
                JobFailureReason.None,
                sourceLabel,
                "PeriodicReevaluation");
        }

        public static DecisionReevaluationReason NeedAlert(NeedKind need)
        {
            return new DecisionReevaluationReason(
                DecisionReevaluationReasonKind.NeedAlert,
                need,
                JobFailureReason.None,
                "Needs",
                "NeedAlert");
        }

        public static DecisionReevaluationReason NeedCritical(NeedKind need)
        {
            return new DecisionReevaluationReason(
                DecisionReevaluationReasonKind.NeedCritical,
                need,
                JobFailureReason.None,
                "Needs",
                "NeedCritical");
        }

        public static DecisionReevaluationReason JobCompleted(string sourceLabel = "")
        {
            return new DecisionReevaluationReason(
                DecisionReevaluationReasonKind.JobCompleted,
                NeedKind.COUNT,
                JobFailureReason.None,
                sourceLabel,
                "JobCompleted");
        }

        public static DecisionReevaluationReason JobFailed(JobFailureReason failureReason, string sourceLabel = "")
        {
            return new DecisionReevaluationReason(
                DecisionReevaluationReasonKind.JobFailed,
                NeedKind.COUNT,
                failureReason,
                sourceLabel,
                "JobFailed");
        }

        public static DecisionReevaluationReason ExternalEvent(string sourceLabel)
        {
            return new DecisionReevaluationReason(
                DecisionReevaluationReasonKind.ExternalEvent,
                NeedKind.COUNT,
                JobFailureReason.None,
                sourceLabel,
                "ExternalEvent");
        }

        public static DecisionReevaluationReason ManualDebug(string sourceLabel = "")
        {
            return new DecisionReevaluationReason(
                DecisionReevaluationReasonKind.ManualDebug,
                NeedKind.COUNT,
                JobFailureReason.None,
                sourceLabel,
                "ManualDebug");
        }
    }

    // =============================================================================
    // NpcDecisionReevaluationSnapshot
    // =============================================================================
    /// <summary>
    /// <para>
    /// Snapshot passivo che associa un NPC, un tick e un motivo di rivalutazione
    /// decisionale.
    /// </para>
    ///
    /// <para><b>Snapshot diagnostico, non scheduler produttivo</b></para>
    /// <para>
    /// Questo DTO non decide cadence, non ordina motivi multipli, non deduplica
    /// segnali e non produce batching. Serve solo a rendere leggibile, in modo
    /// futuro-estendibile, perche' un NPC viene preso in considerazione. Se il motivo
    /// e' nullo, <c>HasValidReason</c> resta false.
    /// </para>
    ///
    /// <para><b>Struttura interna:</b></para>
    /// <list type="bullet">
    ///   <item><b>NpcId</b>: identita' diagnostica dell'NPC.</item>
    ///   <item><b>Tick</b>: tick globale canonico gia' fornito dal chiamante.</item>
    ///   <item><b>Reason</b>: singolo motivo passivo della rivalutazione.</item>
    ///   <item><b>HasValidReason</b>: shortcut leggibile per QA e future trace.</item>
    /// </list>
    /// </summary>
    public readonly struct NpcDecisionReevaluationSnapshot
    {
        public readonly int NpcId;
        public readonly int Tick;
        public readonly DecisionReevaluationReason Reason;

        public bool HasValidReason => Reason.IsValid;

        public NpcDecisionReevaluationSnapshot(
            int npcId,
            int tick,
            DecisionReevaluationReason reason)
        {
            NpcId = npcId;
            Tick = tick;
            Reason = reason;
        }

        public static NpcDecisionReevaluationSnapshot NoValidReason(int npcId, int tick)
        {
            return new NpcDecisionReevaluationSnapshot(npcId, tick, DecisionReevaluationReason.None());
        }
    }
}
