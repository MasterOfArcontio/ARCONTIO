// =============================================================================
// MemoryBeliefDecisionExplainabilityTypes.cs
// Namespace: Arcontio.Core
// Sessione: v0.05.32-EL_MBD_Tipi_Dati_Config
// =============================================================================
//
// Contratti dati passivi dell'Explainability Layer dedicato al ciclo:
// MemoryStore -> BeliefStore -> BeliefQuery -> Decision.
//
// Regola architetturale:
// - questi tipi sono snapshot diagnostici;
// - non guidano la simulazione;
// - non leggono World, MemoryStore o BeliefStore;
// - non modificano decisioni, belief, memoria o command.
// =============================================================================

using System;
using UnityEngine;

namespace Arcontio.Core
{
    // =============================================================================
    // MemoryBeliefDecisionTraceKind
    // =============================================================================
    /// <summary>
    /// <para>
    /// Tipo del record EL-MBD esportabile nel log JSONL o conservabile in un futuro
    /// ring buffer runtime.
    /// </para>
    ///
    /// <para><b>Envelope diagnostico stabile</b></para>
    /// <para>
    /// Il valore classifica la payload principale del record senza obbligare il
    /// lettore a dedurla da campi null. Ogni record deve avere un solo kind
    /// semanticamente attivo.
    /// </para>
    ///
    /// <para><b>Struttura interna:</b></para>
    /// <list type="bullet">
    ///   <item><b>Unknown</b>: valore difensivo per record incompleti.</item>
    ///   <item><b>Memory</b>: evento di encoding o merge nel MemoryStore.</item>
    ///   <item><b>Belief</b>: creazione, merge, decay o invalidazione di una belief.</item>
    ///   <item><b>Query</b>: valutazione del BeliefQueryService.</item>
    ///   <item><b>Decision</b>: generazione, scoring e selezione intenzione.</item>
    ///   <item><b>Bridge</b>: adattamento provvisorio intenzione -> command legacy.</item>
    /// </list>
    /// </summary>
    public enum MemoryBeliefDecisionTraceKind
    {
        Unknown = 0,
        Memory = 1,
        Belief = 2,
        Query = 3,
        Decision = 4,
        Bridge = 5,
        JobRequest = 6,
        JobLifecycle = 7,
        JobPhase = 8,
        Step = 9,
        JobState = 10,
        JobArbitration = 11,
        Reservation = 12,
        Command = 13,
        FailureLearning = 14
    }

    // =============================================================================
    // MemoryBeliefDecisionJobLifecycleOperation
    // =============================================================================
    /// <summary>
    /// <para>
    /// Operazione diagnostica osservata sul ciclo di vita complessivo di un job.
    /// </para>
    ///
    /// <para><b>Vocabolario runtime stabile per la v0.07</b></para>
    /// <para>
    /// L'Explainability Layer deve poter distinguere creazione, attivazione,
    /// sospensione e chiusura di un job senza affidarsi a stringhe libere emesse da
    /// punti diversi della pipeline.
    /// </para>
    ///
    /// <para><b>Struttura interna:</b></para>
    /// <list type="bullet">
    ///   <item><b>Created</b>: job istanziato ma non ancora eseguito.</item>
    ///   <item><b>Activated</b>: job diventato attivo sull'NPC.</item>
    ///   <item><b>Suspended</b>: job parcheggiato da preemption o policy.</item>
    ///   <item><b>Resumed</b>: job precedentemente sospeso tornato attivo.</item>
    ///   <item><b>Completed</b>: job terminato con successo.</item>
    ///   <item><b>Failed</b>: job terminato con errore.</item>
    ///   <item><b>Cancelled</b>: job annullato da policy.</item>
    /// </list>
    /// </summary>
    public enum MemoryBeliefDecisionJobLifecycleOperation
    {
        Unknown = 0,
        Created = 1,
        Activated = 2,
        Suspended = 3,
        Resumed = 4,
        Completed = 5,
        Failed = 6,
        Cancelled = 7
    }

    // =============================================================================
    // MemoryBeliefDecisionJobPhaseOperation
    // =============================================================================
    /// <summary>
    /// <para>
    /// Operazione diagnostica osservata sul cursore di fase del job.
    /// </para>
    ///
    /// <para><b>Fase come boundary intermedio</b></para>
    /// <para>
    /// L'EL v0.07 deve rendere visibile il salto fra fasi senza costringere UI e
    /// log JSONL a ricostruirlo indirettamente dal solo indice corrente.
    /// </para>
    ///
    /// <para><b>Struttura interna:</b></para>
    /// <list type="bullet">
    ///   <item><b>Entered</b>: fase appena entrata.</item>
    ///   <item><b>Completed</b>: fase conclusa con successo.</item>
    ///   <item><b>Interrupted</b>: fase interrotta da preemption o errore.</item>
    /// </list>
    /// </summary>
    public enum MemoryBeliefDecisionJobPhaseOperation
    {
        Unknown = 0,
        Entered = 1,
        Completed = 2,
        Interrupted = 3
    }

    // =============================================================================
    // MemoryBeliefDecisionReservationOperation
    // =============================================================================
    /// <summary>
    /// <para>
    /// Operazione diagnostica osservata sul Reservation layer.
    /// </para>
    ///
    /// <para><b>Contesa leggibile in runtime</b></para>
    /// <para>
    /// Il layer diagnostico deve chiarire se una reservation e' stata accettata,
    /// negata, rilasciata o scaduta, senza che la UI debba leggere lo store live.
    /// </para>
    /// </summary>
    public enum MemoryBeliefDecisionReservationOperation
    {
        Unknown = 0,
        Accepted = 1,
        Denied = 2,
        Released = 3,
        Expired = 4
    }

    // =============================================================================
    // MemoryBeliefDecisionCommandOperation
    // =============================================================================
    /// <summary>
    /// <para>
    /// Operazione diagnostica osservata sul JobCommandBuffer o sul confine step ->
    /// command.
    /// </para>
    /// </summary>
    public enum MemoryBeliefDecisionCommandOperation
    {
        Unknown = 0,
        Enqueued = 1,
        Snapshot = 2
    }

    // =============================================================================
    // MemoryBeliefDecisionBeliefOperation
    // =============================================================================
    /// <summary>
    /// <para>
    /// Operazione diagnostica osservata su una <see cref="BeliefEntry"/>.
    /// </para>
    ///
    /// <para><b>BeliefStore passivo</b></para>
    /// <para>
    /// L'operazione descrive cosa e' accaduto, ma non decide cosa debba accadere.
    /// Deve essere popolata dal layer che possiede gia' la causa, ad esempio
    /// <c>BeliefUpdater</c> o <c>BeliefDecaySystem</c>.
    /// </para>
    ///
    /// <para><b>Struttura interna:</b></para>
    /// <list type="bullet">
    ///   <item><b>Created</b>: nuova entry creata.</item>
    ///   <item><b>Merged</b>: entry esistente aggiornata da nuova traccia.</item>
    ///   <item><b>Reinforced</b>: confidence/freshness rinforzate.</item>
    ///   <item><b>Conflicted</b>: belief marcata conflittuale.</item>
    ///   <item><b>Weakened</b>: confidence ridotta senza scarto definitivo.</item>
    ///   <item><b>Stale</b>: freshness sotto soglia.</item>
    ///   <item><b>Discarded</b>: belief invalidata.</item>
    ///   <item><b>RemovedByDecay</b>: belief rimossa per decay/pruning.</item>
    ///   <item><b>Ignored</b>: trace o segnale non applicabile.</item>
    /// </list>
    /// </summary>
    public enum MemoryBeliefDecisionBeliefOperation
    {
        Unknown = 0,
        Created = 1,
        Merged = 2,
        Reinforced = 3,
        Conflicted = 4,
        Weakened = 5,
        Stale = 6,
        Discarded = 7,
        RemovedByDecay = 8,
        Ignored = 9
    }

    // =============================================================================
    // MemoryBeliefDecisionTargetSource
    // =============================================================================
    /// <summary>
    /// <para>
    /// Origine diagnostica del target usato dal bridge decisionale provvisorio.
    /// </para>
    ///
    /// <para><b>Separazione decisione / esecuzione legacy</b></para>
    /// <para>
    /// Il valore serve a distinguere una scelta fondata sul QuerySystem da un fallback
    /// legacy ancora presente prima del Job System v0.06.
    /// </para>
    ///
    /// <para><b>Struttura interna:</b></para>
    /// <list type="bullet">
    ///   <item><b>Unknown</b>: sorgente non nota.</item>
    ///   <item><b>BeliefQuery</b>: target derivato da BeliefQueryResult.</item>
    ///   <item><b>CarriedState</b>: stato locale dell'NPC, ad esempio cibo portato.</item>
    ///   <item><b>LocalVerification</b>: verifica fisica sulla cella corrente.</item>
    ///   <item><b>LegacyFallback</b>: percorso legacy temporaneo.</item>
    ///   <item><b>None</b>: nessun target operativo prodotto.</item>
    /// </list>
    /// </summary>
    public enum MemoryBeliefDecisionTargetSource
    {
        Unknown = 0,
        BeliefQuery = 1,
        CarriedState = 2,
        LocalVerification = 3,
        LegacyFallback = 4,
        None = 5
    }

    // =============================================================================
    // MemoryBeliefDecisionScoreContributionRef
    // =============================================================================
    /// <summary>
    /// <para>
    /// Snapshot serializzabile di un contributo nominato di scoring.
    /// </para>
    ///
    /// <para><b>Explainability dei pesi</b></para>
    /// <para>
    /// La struttura e' volutamente minimale: label e valore gia' calcolato. Non
    /// contiene evaluator live e non permette di ricalcolare lo score.
    /// </para>
    ///
    /// <para><b>Struttura interna:</b></para>
    /// <list type="bullet">
    ///   <item><b>Label</b>: nome leggibile del contributo.</item>
    ///   <item><b>Value</b>: valore numerico gia' pesato.</item>
    /// </list>
    /// </summary>
    [Serializable]
    public struct MemoryBeliefDecisionScoreContributionRef
    {
        public string Label;
        public float Value;
    }

    // =============================================================================
    // MemoryBeliefDecisionBeliefRef
    // =============================================================================
    /// <summary>
    /// <para>
    /// Snapshot diagnostico di una belief usata, creata o scelta da una query.
    /// </para>
    ///
    /// <para><b>Nessuna reference live</b></para>
    /// <para>
    /// Questo record copia i campi essenziali di <see cref="BeliefEntry"/>. Non deve
    /// essere usato per recuperare o modificare la entry originale.
    /// </para>
    ///
    /// <para><b>Struttura interna:</b></para>
    /// <list type="bullet">
    ///   <item><b>Category</b>: categoria semantica.</item>
    ///   <item><b>Status</b>: status operativo al momento dello snapshot.</item>
    ///   <item><b>BeliefId</b>: id locale per-NPC.</item>
    ///   <item><b>EstimatedPosition</b>: cella stimata soggettiva.</item>
    ///   <item><b>Confidence</b>: confidence snapshot.</item>
    ///   <item><b>Freshness</b>: freshness snapshot.</item>
    ///   <item><b>SourceCount</b>: numero fonti aggregate.</item>
    ///   <item><b>Source</b>: fonte principale.</item>
    /// </list>
    /// </summary>
    [Serializable]
    public struct MemoryBeliefDecisionBeliefRef
    {
        public BeliefCategory Category;
        public BeliefStatus Status;
        public BeliefSource Source;
        public int BeliefId;
        public Vector2Int EstimatedPosition;
        public float Confidence;
        public float Freshness;
        public int SourceCount;
    }

    // =============================================================================
    // MemoryBeliefDecisionMemoryTraceRecord
    // =============================================================================
    /// <summary>
    /// <para>
    /// Payload diagnostica per un evento di memory encoding o merge.
    /// </para>
    ///
    /// <para><b>MemoryStore soggettivo</b></para>
    /// <para>
    /// Il record deve essere costruito nel punto in cui il sistema ha gia' una
    /// <see cref="MemoryTrace"/> e l'esito di <see cref="MemoryStore.AddOrMerge"/>.
    /// Non deve cercare eventi o oggetti a posteriori.
    /// </para>
    ///
    /// <para><b>Struttura interna:</b></para>
    /// <list type="bullet">
    ///   <item><b>EventType</b>: nome evento sorgente, se noto.</item>
    ///   <item><b>TraceType</b>: tipo memoria.</item>
    ///   <item><b>Subject*</b>: identita' soggettiva trasportata dalla trace.</item>
    ///   <item><b>Cell</b>: posizione soggettiva della trace.</item>
    ///   <item><b>Intensity/Reliability</b>: valori gia' prodotti dalla memory rule.</item>
    ///   <item><b>StoreResult</b>: esito dell'inserimento nel MemoryStore.</item>
    /// </list>
    /// </summary>
    [Serializable]
    public sealed class MemoryBeliefDecisionMemoryTraceRecord
    {
        public string EventType = string.Empty;
        public MemoryType TraceType;
        public int SubjectId;
        public int SecondarySubjectId;
        public string SubjectDefId = string.Empty;
        public Vector2Int Cell;
        public float Intensity01;
        public float Reliability01;
        public bool IsHeard;
        public string HeardKind = string.Empty;
        public int SourceSpeakerId;
        public AddOrMergeResult StoreResult;
    }

    // =============================================================================
    // MemoryBeliefDecisionBeliefRecord
    // =============================================================================
    /// <summary>
    /// <para>
    /// Payload diagnostica per una mutazione belief.
    /// </para>
    ///
    /// <para><b>Causa esplicita</b></para>
    /// <para>
    /// Il campo <c>Reason</c> deve essere popolato dal producer che conosce gia' la
    /// causa, non ricostruito da un sistema diagnostico esterno.
    /// </para>
    ///
    /// <para><b>Struttura interna:</b></para>
    /// <list type="bullet">
    ///   <item><b>Operation</b>: tipo mutazione osservata.</item>
    ///   <item><b>SourceTraceType</b>: memoria sorgente, se applicabile.</item>
    ///   <item><b>Belief</b>: snapshot della entry dopo la mutazione.</item>
    ///   <item><b>Reason</b>: ragione diagnostica breve.</item>
    /// </list>
    /// </summary>
    [Serializable]
    public sealed class MemoryBeliefDecisionBeliefRecord
    {
        public MemoryBeliefDecisionBeliefOperation Operation;
        public MemoryType SourceTraceType;
        public bool HasSourceTrace;
        public MemoryBeliefDecisionBeliefRef Belief;
        public string Reason = string.Empty;
    }

    // =============================================================================
    // MemoryBeliefDecisionQueryRecord
    // =============================================================================
    /// <summary>
    /// <para>
    /// Payload diagnostica per una chiamata al BeliefQueryService.
    /// </para>
    ///
    /// <para><b>Query spiegabile</b></para>
    /// <para>
    /// Il record conserva il contesto della query, il risultato e il breakdown. Non
    /// deve rieseguire la query e non deve ordinare candidati fuori dal servizio.
    /// </para>
    ///
    /// <para><b>Struttura interna:</b></para>
    /// <list type="bullet">
    ///   <item><b>GoalType</b>: categoria richiesta.</item>
    ///   <item><b>Urgency01</b>: urgenza usata dagli evaluator.</item>
    ///   <item><b>NpcPosition</b>: posizione soggettiva ammessa al QuerySystem.</item>
    ///   <item><b>CandidateCount</b>: numero entry candidate rilevate dal servizio.</item>
    ///   <item><b>IsEmpty</b>: true se nessuna belief utilizzabile e' risultata valida.</item>
    ///   <item><b>Winner</b>: snapshot del belief vincitore.</item>
    ///   <item><b>Contributions</b>: breakdown del winner.</item>
    /// </list>
    /// </summary>
    [Serializable]
    public sealed class MemoryBeliefDecisionQueryRecord
    {
        public BeliefCategory GoalType;
        public float Urgency01;
        public Vector2Int NpcPosition;
        public float MinConfidence;
        public int CandidateCount;
        public int UsableCandidateCount;
        public bool IsEmpty;
        public string EmptyReason = string.Empty;
        public MemoryBeliefDecisionBeliefRef Winner;
        public float FinalScore;
        public MemoryBeliefDecisionScoreContributionRef[] Contributions = Array.Empty<MemoryBeliefDecisionScoreContributionRef>();
    }

    // =============================================================================
    // MemoryBeliefDecisionCandidateRecord
    // =============================================================================
    /// <summary>
    /// <para>
    /// Snapshot diagnostico di un candidato del Decision Layer.
    /// </para>
    ///
    /// <para><b>Candidato passivo</b></para>
    /// <para>
    /// Il record copia lo stato di un <see cref="DecisionCandidate"/> dopo scoring.
    /// Non puo' essere riusato per selezionare intenzioni.
    /// </para>
    ///
    /// <para><b>Struttura interna:</b></para>
    /// <list type="bullet">
    ///   <item><b>Intent</b>: intenzione candidata.</item>
    ///   <item><b>Available</b>: true se ha superato i gate.</item>
    ///   <item><b>Need</b>: bisogno primario dichiarato dal catalogo.</item>
    ///   <item><b>Belief</b>: snapshot opzionale del target scelto via QuerySystem.</item>
    ///   <item><b>Score</b>: score finale gia' calcolato.</item>
    ///   <item><b>ScoreContributions</b>: breakdown decisionale.</item>
    /// </list>
    /// </summary>
    [Serializable]
    public sealed class MemoryBeliefDecisionCandidateRecord
    {
        public DecisionIntentKind Intent;
        public bool Available;
        public NeedKind Need;
        public float NeedUrgency01;
        public bool IsCritical;
        public bool RequiresBeliefTarget;
        public bool BeliefResultEmpty;
        public MemoryBeliefDecisionBeliefRef Belief;
        public float Score;
        public string FilteredReason = string.Empty;
        public MemoryBeliefDecisionScoreContributionRef[] ScoreContributions = Array.Empty<MemoryBeliefDecisionScoreContributionRef>();
    }

    // =============================================================================
    // MemoryBeliefDecisionDecisionRecord
    // =============================================================================
    /// <summary>
    /// <para>
    /// Payload diagnostica della selezione decisionale completa.
    /// </para>
    ///
    /// <para><b>Decision Layer auditabile</b></para>
    /// <para>
    /// Il record deve essere emesso dal punto che possiede gia' context, candidati,
    /// audit e risultato di selezione. Non deve creare un nuovo context e non deve
    /// chiamare nuovamente i servizi decisionali.
    /// </para>
    ///
    /// <para><b>Struttura interna:</b></para>
    /// <list type="bullet">
    ///   <item><b>AuditValid</b>: esito del DecisionInputAudit.</item>
    ///   <item><b>CandidateCount</b>: numero candidati valutati.</item>
    ///   <item><b>SelectedIntent</b>: intenzione vincente.</item>
    ///   <item><b>SelectedScore</b>: score vincente.</item>
    ///   <item><b>Candidates</b>: lista opzionale dei candidati.</item>
    ///   <item><b>Selection*</b>: parametri di selezione weighted random.</item>
    /// </list>
    /// </summary>
    [Serializable]
    public sealed class MemoryBeliefDecisionDecisionRecord
    {
        public bool AuditValid;
        public int CandidateCount;
        public DecisionIntentKind SelectedIntent;
        public float SelectedScore;
        public int SelectedIndex;
        public int SelectionTopN;
        public float SelectionNoise01;
        public float Impulsivity01;
        public float EffectiveNoise01;
        public MemoryBeliefDecisionCandidateRecord[] Candidates = Array.Empty<MemoryBeliefDecisionCandidateRecord>();
    }

    // =============================================================================
    // MemoryBeliefDecisionBridgeRecord
    // =============================================================================
    /// <summary>
    /// <para>
    /// Payload diagnostica del bridge provvisorio Decision -> Command legacy.
    /// </para>
    ///
    /// <para><b>Ponte temporaneo verso v0.06</b></para>
    /// <para>
    /// Questo record rende visibile se la decisione e' stata davvero tradotta in un
    /// command oppure se e' ricaduta su fallback legacy in attesa del Job System.
    /// </para>
    ///
    /// <para><b>Struttura interna:</b></para>
    /// <list type="bullet">
    ///   <item><b>SelectedIntent</b>: intenzione che il bridge ha ricevuto.</item>
    ///   <item><b>CommandName</b>: command prodotto, stringa vuota se assente.</item>
    ///   <item><b>Handled</b>: true se il bridge considera gestita la decisione.</item>
    ///   <item><b>DidMove/DidSteal</b>: flag gia' usati dalla rule legacy.</item>
    ///   <item><b>TargetSource</b>: origine diagnostica del target.</item>
    ///   <item><b>LegacyFallbackUsed</b>: true se la rule legacy deve proseguire.</item>
    ///   <item><b>Reason</b>: motivo breve leggibile.</item>
    /// </list>
    /// </summary>
    [Serializable]
    public sealed class MemoryBeliefDecisionBridgeRecord
    {
        public DecisionIntentKind SelectedIntent;
        public string CommandName = string.Empty;
        public bool Handled;
        public bool DidMove;
        public bool DidSteal;
        public Vector2Int TargetCell;
        public MemoryBeliefDecisionTargetSource TargetSource;
        public bool LegacyFallbackUsed;
        public string Reason = string.Empty;
    }

    // =============================================================================
    // MemoryBeliefDecisionJobRequestRecord
    // =============================================================================
    /// <summary>
    /// <para>
    /// Payload diagnostica del passaggio Decision -> JobRequest.
    /// </para>
    ///
    /// <para><b>Bridge esplicito senza rompere il legacy path</b></para>
    /// <para>
    /// Questo record separa il nuovo confine verso il Job System dal bridge legacy
    /// Decision -> Command, cosi' la UI puo' mostrare entrambi in modo distinto.
    /// </para>
    ///
    /// <para><b>Struttura interna:</b></para>
    /// <list type="bullet">
    ///   <item><b>RequestId/JobId</b>: identificatori diagnostici della richiesta.</item>
    ///   <item><b>Intent/Priority/Urgency</b>: motivazione della richiesta.</item>
    ///   <item><b>Target*</b>: target soggettivo gia' risolto dal chiamante.</item>
    ///   <item><b>BeliefKey</b>: riferimento opzionale alla belief motivante.</item>
    ///   <item><b>Reason</b>: spiegazione breve del producer.</item>
    /// </list>
    /// </summary>
    [Serializable]
    public sealed class MemoryBeliefDecisionJobRequestRecord
    {
        public string RequestId = string.Empty;
        public string JobId = string.Empty;
        public DecisionIntentKind Intent;
        public JobPriorityClass PriorityClass;
        public float Urgency01;
        public bool HasTargetCell;
        public Vector2Int TargetCell;
        public int TargetObjectId;
        public string BeliefKey = string.Empty;
        public string DebugLabel = string.Empty;
        public string Reason = string.Empty;
        public bool LegacyBridgeStillUsed;
    }

    // =============================================================================
    // MemoryBeliefDecisionJobRef
    // =============================================================================
    /// <summary>
    /// <para>
    /// Snapshot minimo e serializzabile di un job osservato dall'EL.
    /// </para>
    ///
    /// <para><b>Riferimento passivo al runtime job</b></para>
    /// <para>
    /// La UI non deve leggere direttamente <c>Job</c> o <c>NpcJobState</c>, quindi
    /// i campi essenziali vengono copiati qui come fotografia read-only.
    /// </para>
    /// </summary>
    [Serializable]
    public sealed class MemoryBeliefDecisionJobRef
    {
        public string JobId = string.Empty;
        public string RequestId = string.Empty;
        public DecisionIntentKind Intent;
        public JobPriorityClass PriorityClass;
        public float Urgency01;
        public JobStatus Status;
        public JobFailureReason FailureReason;
        public int CreatedTick;
        public int UpdatedTick;
        public int ActivePhaseIndex;
        public bool HasTargetCell;
        public Vector2Int TargetCell;
        public int TargetObjectId;
        public string DebugLabel = string.Empty;
    }

    // =============================================================================
    // MemoryBeliefDecisionJobPhaseRef
    // =============================================================================
    /// <summary>
    /// <para>
    /// Snapshot serializzabile della fase osservata nel momento della trace.
    /// </para>
    /// </summary>
    [Serializable]
    public sealed class MemoryBeliefDecisionJobPhaseRef
    {
        public string PhaseId = string.Empty;
        public JobPhaseKind Kind;
        public string DisplayName = string.Empty;
        public int PhaseIndex;
        public int ExpectedStepCount;
        public bool IsInterruptible;
    }

    // =============================================================================
    // MemoryBeliefDecisionStepRef
    // =============================================================================
    /// <summary>
    /// <para>
    /// Snapshot serializzabile di uno step atomico di job.
    /// </para>
    /// </summary>
    [Serializable]
    public sealed class MemoryBeliefDecisionStepRef
    {
        public string ActionId = string.Empty;
        public JobActionKind Kind;
        public string Label = string.Empty;
        public int ActionIndex;
        public bool HasTargetCell;
        public Vector2Int TargetCell;
        public int TargetObjectId;
        public int DurationTicks;
        public string PayloadKey = string.Empty;
    }

    // =============================================================================
    // MemoryBeliefDecisionStepResultRef
    // =============================================================================
    /// <summary>
    /// <para>
    /// Snapshot serializzabile dell'ultimo StepResult osservato.
    /// </para>
    /// </summary>
    [Serializable]
    public sealed class MemoryBeliefDecisionStepResultRef
    {
        public StepResultStatus Status;
        public JobFailureReason FailureReason;
        public int SuggestedWaitTicks;
        public string DiagnosticMessage = string.Empty;
    }

    // =============================================================================
    // MemoryBeliefDecisionJobLifecycleRecord
    // =============================================================================
    /// <summary>
    /// <para>
    /// Payload diagnostica del ciclo di vita di un job.
    /// </para>
    /// </summary>
    [Serializable]
    public sealed class MemoryBeliefDecisionJobLifecycleRecord
    {
        public MemoryBeliefDecisionJobLifecycleOperation Operation;
        public MemoryBeliefDecisionJobRef Job = new();
        public string Reason = string.Empty;
    }

    // =============================================================================
    // MemoryBeliefDecisionJobPhaseRecord
    // =============================================================================
    /// <summary>
    /// <para>
    /// Payload diagnostica di una transizione o osservazione di fase.
    /// </para>
    /// </summary>
    [Serializable]
    public sealed class MemoryBeliefDecisionJobPhaseRecord
    {
        public MemoryBeliefDecisionJobPhaseOperation Operation;
        public MemoryBeliefDecisionJobRef Job = new();
        public MemoryBeliefDecisionJobPhaseRef Phase = new();
        public string Reason = string.Empty;
    }

    // =============================================================================
    // MemoryBeliefDecisionStepRecord
    // =============================================================================
    /// <summary>
    /// <para>
    /// Payload diagnostica per step e StepResult del job.
    /// </para>
    /// </summary>
    [Serializable]
    public sealed class MemoryBeliefDecisionStepRecord
    {
        public MemoryBeliefDecisionJobRef Job = new();
        public MemoryBeliefDecisionJobPhaseRef Phase = new();
        public MemoryBeliefDecisionStepRef Step = new();
        public MemoryBeliefDecisionStepResultRef Result = new();
        public string Reason = string.Empty;
    }

    // =============================================================================
    // MemoryBeliefDecisionJobStateRecord
    // =============================================================================
    /// <summary>
    /// <para>
    /// Payload diagnostica dello snapshot runtime di <c>NpcJobState</c>.
    /// </para>
    /// </summary>
    [Serializable]
    public sealed class MemoryBeliefDecisionJobStateRecord
    {
        public bool HasActiveJob;
        public string ActiveJobId = string.Empty;
        public int ActivePhaseIndex;
        public int ActiveActionIndex;
        public int WaitUntilTick;
        public string SuspendedJobId = string.Empty;
        public JobFailureReason LastFailureReason;
        public string Reason = string.Empty;
    }

    // =============================================================================
    // MemoryBeliefDecisionJobArbitrationRecord
    // =============================================================================
    /// <summary>
    /// <para>
    /// Payload diagnostica della decisione dell'arbitro tra job corrente e nuova
    /// richiesta.
    /// </para>
    /// </summary>
    [Serializable]
    public sealed class MemoryBeliefDecisionJobArbitrationRecord
    {
        public MemoryBeliefDecisionJobRef CurrentJob = new();
        public MemoryBeliefDecisionJobRef ProposedJob = new();
        public JobArbitrationDecision Decision;
        public string AcceptedJobId = string.Empty;
        public string Reason = string.Empty;
    }

    // =============================================================================
    // MemoryBeliefDecisionReservationRecord
    // =============================================================================
    /// <summary>
    /// <para>
    /// Payload diagnostica di una operazione di reservation.
    /// </para>
    /// </summary>
    [Serializable]
    public sealed class MemoryBeliefDecisionReservationRecord
    {
        public MemoryBeliefDecisionReservationOperation Operation;
        public string ReservationId = string.Empty;
        public string JobId = string.Empty;
        public int OwnerNpcId;
        public ReservationTargetKind TargetKind;
        public Vector2Int TargetCell;
        public int TargetObjectId;
        public int CreatedTick;
        public int ExpiresTick;
        public string Reason = string.Empty;
    }

    // =============================================================================
    // MemoryBeliefDecisionCommandRecord
    // =============================================================================
    /// <summary>
    /// <para>
    /// Payload diagnostica del confine step -> command buffer.
    /// </para>
    /// </summary>
    [Serializable]
    public sealed class MemoryBeliefDecisionCommandRecord
    {
        public MemoryBeliefDecisionCommandOperation Operation;
        public string JobId = string.Empty;
        public string CommandName = string.Empty;
        public int QueueCount;
        public string Reason = string.Empty;
    }

    // =============================================================================
    // MemoryBeliefDecisionFailureLearningRecord
    // =============================================================================
    /// <summary>
    /// <para>
    /// Payload diagnostica del failure learning per target cella.
    /// </para>
    /// </summary>
    [Serializable]
    public sealed class MemoryBeliefDecisionFailureLearningRecord
    {
        public string JobId = string.Empty;
        public Vector2Int TargetCell;
        public JobFailureReason FailureReason;
        public int FailureTick;
        public float Penalty01;
        public string Reason = string.Empty;
    }

    // =============================================================================
    // MemoryBeliefDecisionTrace
    // =============================================================================
    /// <summary>
    /// <para>
    /// Envelope runtime comune per le payload EL-MBD.
    /// </para>
    ///
    /// <para><b>Record a payload singola</b></para>
    /// <para>
    /// In memoria il record puo' contenere piu' campi nullable, ma ogni istanza deve
    /// valorizzare semanticamente solo la payload indicata da <see cref="Kind"/>.
    /// Il sink JSONL rendera' esplicito il kind come stringa.
    /// </para>
    ///
    /// <para><b>Struttura interna:</b></para>
    /// <list type="bullet">
    ///   <item><b>Kind</b>: tipo payload.</item>
    ///   <item><b>Tick</b>: tick simulativo.</item>
    ///   <item><b>NpcId</b>: NPC proprietario della trace, 0 se globale/non applicabile.</item>
    ///   <item><b>Memory/Belief/Query/Decision/Bridge</b>: payload specifica.</item>
    /// </list>
    /// </summary>
    [Serializable]
    public sealed class MemoryBeliefDecisionTrace
    {
        public MemoryBeliefDecisionTraceKind Kind;
        public long Tick;
        public int NpcId;
        public MemoryBeliefDecisionMemoryTraceRecord Memory;
        public MemoryBeliefDecisionBeliefRecord Belief;
        public MemoryBeliefDecisionQueryRecord Query;
        public MemoryBeliefDecisionDecisionRecord Decision;
        public MemoryBeliefDecisionBridgeRecord Bridge;
        public MemoryBeliefDecisionJobRequestRecord JobRequest;
        public MemoryBeliefDecisionJobLifecycleRecord JobLifecycle;
        public MemoryBeliefDecisionJobPhaseRecord JobPhase;
        public MemoryBeliefDecisionStepRecord Step;
        public MemoryBeliefDecisionJobStateRecord JobState;
        public MemoryBeliefDecisionJobArbitrationRecord JobArbitration;
        public MemoryBeliefDecisionReservationRecord Reservation;
        public MemoryBeliefDecisionCommandRecord Command;
        public MemoryBeliefDecisionFailureLearningRecord FailureLearning;
    }
}
