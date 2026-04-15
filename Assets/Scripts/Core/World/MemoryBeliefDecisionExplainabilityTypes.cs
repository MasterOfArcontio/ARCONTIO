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
        Bridge = 5
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
    }
}
