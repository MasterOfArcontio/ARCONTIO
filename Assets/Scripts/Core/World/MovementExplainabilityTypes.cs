// =============================================================================
// MovementExplainabilityTypes.cs
// Namespace: Arcontio.Core
// Sessione: v0.04.1.b-EL_Pathfinding_Tipi_Dati
// =============================================================================
//
// Questo file introduce soltanto i contratti dati passivi dell'Explainability
// Layer del pathfinding. Non contiene store, registry, emissioni, query o UI.
//
// Regola architetturale:
// - questi tipi possono essere popolati dai sistemi futuri dell'EL;
// - non devono diventare input per Decision Layer, Job Execution o Pathfinding;
// - non devono modificare world state;
// - non devono interrogare BeliefStore, MemoryStore o registry globali.
// =============================================================================

using System;
using System.Collections.Generic;
using UnityEngine;

namespace Arcontio.Core
{
    // =============================================================================
    // MovementPurpose
    // =============================================================================
    /// <summary>
    /// <para>
    /// Descrive lo scopo semantico del movimento visto dall'Explainability Layer.
    /// Non sostituisce <see cref="MoveIntentReason"/>: e' una normalizzazione piu'
    /// espressiva pensata per la lettura umana, il pannello runtime e il log JSONL.
    /// </para>
    ///
    /// <para><b>Separazione intent / spiegazione</b></para>
    /// <para>
    /// Il valore non deve guidare il pathfinding. Serve solo a spiegare un intent
    /// gia' prodotto dal Decision Layer o da strumenti dev. In Fase 1 potra' essere
    /// derivato da <see cref="MoveIntentReason"/>; in fasi successive potra' essere
    /// popolato con contesto piu' ricco proveniente da job, step e QuerySystem.
    /// </para>
    ///
    /// <para><b>Struttura interna:</b></para>
    /// <list type="bullet">
    ///   <item><b>Unknown</b>: valore difensivo quando la sorgente non e' nota.</item>
    ///   <item><b>ReachFood</b>: l'NPC si muove verso una fonte di cibo nota o creduta.</item>
    ///   <item><b>ReachWater</b>: l'NPC si muove verso una fonte d'acqua futura.</item>
    ///   <item><b>ReachBed</b>: l'NPC si muove verso un letto o luogo di riposo.</item>
    ///   <item><b>ReachWorkstation</b>: l'NPC si muove verso una postazione di lavoro futura.</item>
    ///   <item><b>Follow</b>: l'NPC segue un altro NPC o entita'.</item>
    ///   <item><b>Flee</b>: l'NPC fugge da una minaccia o da una zona pericolosa.</item>
    ///   <item><b>Patrol</b>: l'NPC segue una routine o pattuglia futura.</item>
    ///   <item><b>Wander</b>: movimento esplorativo o non finalizzato a risorsa specifica.</item>
    ///   <item><b>DebugClick</b>: movimento imposto da DevTools o input di debug.</item>
    ///   <item><b>ScheduleFrame</b>: movimento originato da schedule/frame futuro.</item>
    ///   <item><b>InstitutionalOrder</b>: movimento originato da ordini istituzionali futuri.</item>
    /// </list>
    /// </summary>
    public enum MovementPurpose
    {
        Unknown = 0,
        ReachFood = 1,
        ReachWater = 2,
        ReachBed = 3,
        ReachWorkstation = 4,
        Follow = 5,
        Flee = 6,
        Patrol = 7,
        Wander = 8,
        DebugClick = 9,
        ScheduleFrame = 10,
        InstitutionalOrder = 11
    }

    // =============================================================================
    // MovementTargetType
    // =============================================================================
    /// <summary>
    /// <para>
    /// Classifica il tipo di destinazione a cui punta un intent di movimento
    /// osservato dall'EL. Il tipo serve alla UI e al log per formattare il target
    /// senza dedurre a posteriori cosa rappresenti la cella.
    /// </para>
    ///
    /// <para><b>Snapshot spiegabile del target</b></para>
    /// <para>
    /// Il tipo non concede conoscenza globale all'NPC: descrive soltanto come il
    /// target era stato espresso al momento dell'emissione della trace. Quando il
    /// target deriva da un oggetto o da un NPC, l'EL deve conservare un id snapshot,
    /// non una reference live.
    /// </para>
    ///
    /// <para><b>Struttura interna:</b></para>
    /// <list type="bullet">
    ///   <item><b>Unknown</b>: target non classificato.</item>
    ///   <item><b>Cell</b>: target espresso come cella di griglia.</item>
    ///   <item><b>WorldObject</b>: target espresso come oggetto del mondo.</item>
    ///   <item><b>Npc</b>: target espresso come NPC o altra entita' mobile.</item>
    ///   <item><b>FleeVector</b>: target implicito derivato da una direzione di fuga.</item>
    /// </list>
    /// </summary>
    public enum MovementTargetType
    {
        Unknown = 0,
        Cell = 1,
        WorldObject = 2,
        Npc = 3,
        FleeVector = 4
    }

    // =============================================================================
    // PlannerMode
    // =============================================================================
    /// <summary>
    /// <para>
    /// Modalita' candidate o selezionate dalla fase di pianificazione del movimento.
    /// Questa enum descrive la scelta iniziale del planner, non i fallback runtime
    /// che emergono durante il tick di esecuzione.
    /// </para>
    ///
    /// <para><b>LocalSearch non e' un candidato planner</b></para>
    /// <para>
    /// La local search esistente nel codice e' un fallback runtime usato quando il
    /// movimento locale non produce un passo utile. Per questo non compare qui come
    /// modalita' selezionabile. Sara' tracciata tramite <see cref="PathEventType"/>.
    /// </para>
    ///
    /// <para><b>Struttura interna:</b></para>
    /// <list type="bullet">
    ///   <item><b>Unknown</b>: planner non classificato o trace incompleta.</item>
    ///   <item><b>Direct</b>: direct path/prefix diretto verso il target.</item>
    ///   <item><b>LandmarkAstar</b>: A* su grafo landmark soggettivo dell'NPC.</item>
    ///   <item><b>DirectFallback</b>: fallback direct parziale quando LM non e' disponibile.</item>
    /// </list>
    /// </summary>
    public enum PlannerMode
    {
        Unknown = 0,
        Direct = 1,
        LandmarkAstar = 2,
        DirectFallback = 3
    }

    // =============================================================================
    // SelectionReason
    // =============================================================================
    /// <summary>
    /// <para>
    /// Motivo sintetico per cui il planner ha scelto una modalita' invece di
    /// un'altra. Il valore deve essere calcolato nel punto in cui il movimento
    /// conosce gia' candidati e risultato, senza far dipendere l'algoritmo dall'EL.
    /// </para>
    ///
    /// <para><b>Spiegazione non prescrittiva</b></para>
    /// <para>
    /// Questa enum non decide la strategia. Registra solo la ragione osservata della
    /// scelta gia' effettuata, cosi' il pannello runtime puo' mostrare "perche' questa
    /// modalita'" senza rileggere world state o pathfinder.
    /// </para>
    ///
    /// <para><b>Struttura interna:</b></para>
    /// <list type="bullet">
    ///   <item><b>Unknown</b>: ragione non popolata.</item>
    ///   <item><b>DirectValid</b>: direct praticabile e quindi selezionato.</item>
    ///   <item><b>DirectInvalidLmChosen</b>: direct non praticabile, macro-route disponibile.</item>
    ///   <item><b>NoLmFallbackDirect</b>: LM non disponibile, fallback direct/parziale.</item>
    ///   <item><b>NoKnownLandmarks</b>: l'NPC non conosce landmark utili.</item>
    ///   <item><b>LmPlanFailed</b>: A* landmark ha fallito nonostante dati disponibili.</item>
    ///   <item><b>ForcedDebug</b>: scelta imposta da DevTools o flusso di debug.</item>
    /// </list>
    /// </summary>
    public enum SelectionReason
    {
        Unknown = 0,
        DirectValid = 1,
        DirectInvalidLmChosen = 2,
        NoLmFallbackDirect = 3,
        NoKnownLandmarks = 4,
        LmPlanFailed = 5,
        ForcedDebug = 6
    }

    // =============================================================================
    // InvalidReason
    // =============================================================================
    /// <summary>
    /// <para>
    /// Motivo per cui un candidato di planning non era utilizzabile. Questo valore
    /// viene conservato dentro <see cref="PlannerCandidate"/> e non deve essere
    /// interpretato come fallimento dell'intent complessivo.
    /// </para>
    ///
    /// <para><b>Diagnostica dei candidati</b></para>
    /// <para>
    /// Un candidato invalido puo' essere un comportamento corretto. Per esempio, un
    /// NPC appena creato puo' non conoscere landmark e quindi scartare A* soggettivo
    /// senza che il sistema sia rotto.
    /// </para>
    ///
    /// <para><b>Struttura interna:</b></para>
    /// <list type="bullet">
    ///   <item><b>None</b>: candidato valido o ragione non applicabile.</item>
    ///   <item><b>TargetNotVisible</b>: direct negato da percezione/range/FOV/LOS.</item>
    ///   <item><b>PathBlocked</b>: direct negato da traversabilita' locale.</item>
    ///   <item><b>NoKnownLandmarks</b>: memoria landmark soggettiva insufficiente.</item>
    ///   <item><b>LmPlanFailed</b>: A* landmark non ha trovato route.</item>
    ///   <item><b>LandmarkSystemDisabled</b>: sistema landmark disabilitato o non disponibile.</item>
    /// </list>
    /// </summary>
    public enum InvalidReason
    {
        None = 0,
        TargetNotVisible = 1,
        PathBlocked = 2,
        NoKnownLandmarks = 3,
        LmPlanFailed = 4,
        LandmarkSystemDisabled = 5
    }

    // =============================================================================
    // PathEventType
    // =============================================================================
    /// <summary>
    /// <para>
    /// Tipo dell'evento discreto emesso durante l'esecuzione del movimento. Gli
    /// eventi descrivono transizioni, anomalie o completamenti: a verbosita' bassa
    /// non devono rappresentare ogni singolo tick normale.
    /// </para>
    ///
    /// <para><b>Runtime, non planning</b></para>
    /// <para>
    /// Questa enum copre cio' che accade mentre l'NPC prova a muoversi. Per esempio
    /// `LocalSearchActivated` e' runtime: non deve comparire come candidato iniziale
    /// nella `PathPlanTrace`.
    /// </para>
    ///
    /// <para><b>Struttura interna:</b></para>
    /// <list type="bullet">
    ///   <item><b>Unknown</b>: evento non classificato.</item>
    ///   <item><b>Started</b>: esecuzione iniziata dopo pianificazione.</item>
    ///   <item><b>StepSuccess</b>: singolo passo riuscito, solo verbosita' 3.</item>
    ///   <item><b>ReachedWaypoint</b>: waypoint landmark raggiunto.</item>
    ///   <item><b>SwitchedMode</b>: cambio di modalita' di navigazione.</item>
    ///   <item><b>LocalSearchActivated</b>: fallback locale attivato.</item>
    ///   <item><b>Replanned</b>: piano locale o replan ricostruito.</item>
    ///   <item><b>Blocked</b>: tick o transizione di blocco significativa.</item>
    ///   <item><b>BackOffStarted</b>: failure ladder ha avviato uno stage.</item>
    ///   <item><b>BackOffExpired</b>: back-off scaduto, replan da tentare o tentato.</item>
    ///   <item><b>Arrived</b>: target raggiunto.</item>
    ///   <item><b>Failed</b>: intent cancellato con dettaglio fallimento.</item>
    ///   <item><b>DoorInteraction</b>: porta valutata o aperta dal movimento.</item>
    /// </list>
    /// </summary>
    public enum PathEventType
    {
        Unknown = 0,
        Started = 1,
        StepSuccess = 2,
        ReachedWaypoint = 3,
        SwitchedMode = 4,
        LocalSearchActivated = 5,
        Replanned = 6,
        Blocked = 7,
        BackOffStarted = 8,
        BackOffExpired = 9,
        Arrived = 10,
        Failed = 11,
        DoorInteraction = 12
    }

    // =============================================================================
    // FailureType
    // =============================================================================
    /// <summary>
    /// <para>
    /// Categoria del fallimento operativo conservata in <see cref="FailureDetail"/>.
    /// Il valore e' popolato solo quando un <see cref="PathExecutionEvent"/> ha
    /// <see cref="PathEventType.Failed"/>.
    /// </para>
    ///
    /// <para><b>Fallimento come evento strutturato</b></para>
    /// <para>
    /// Non esiste una trace separata per i fallimenti: il fallimento e' un evento di
    /// esecuzione con dettaglio. Questo evita duplicazioni tra timeline, log e UI.
    /// </para>
    ///
    /// <para><b>Struttura interna:</b></para>
    /// <list type="bullet">
    ///   <item><b>Unknown</b>: fallimento non classificato.</item>
    ///   <item><b>StuckTimeout</b>: la failure ladder ha esaurito gli stage.</item>
    ///   <item><b>TargetObjectGone</b>: l'oggetto target non esiste piu'.</item>
    ///   <item><b>TargetObjectEmpty</b>: l'oggetto target e' presente ma vuoto/non utile.</item>
    ///   <item><b>GoalUnreachable</b>: target non raggiungibile nel contesto locale.</item>
    ///   <item><b>MacroRouteFailed</b>: pianificazione landmark fallita.</item>
    ///   <item><b>LocalSearchExhausted</b>: budget o alternative locali esaurite.</item>
    ///   <item><b>TargetMoved</b>: target oggetto non si trova piu' nella cella attesa.</item>
    /// </list>
    /// </summary>
    public enum FailureType
    {
        Unknown = 0,
        StuckTimeout = 1,
        TargetObjectGone = 2,
        TargetObjectEmpty = 3,
        GoalUnreachable = 4,
        MacroRouteFailed = 5,
        LocalSearchExhausted = 6,
        TargetMoved = 7
    }

    // =============================================================================
    // DoorState
    // =============================================================================
    /// <summary>
    /// <para>
    /// Stato leggibile di una porta al momento di una trace EL. Il valore e' uno
    /// snapshot: non deve essere usato per comandare o correggere lo stato reale
    /// dell'oggetto nel mondo.
    /// </para>
    ///
    /// <para><b>Diagnostica porte</b></para>
    /// <para>
    /// Le porte in ARCONTIO sono oggetti del mondo con stato runtime. L'EL registra
    /// se il movimento le ha incontrate, aperte o trovate bloccate, ma il comando
    /// resta responsabilita' di <see cref="OpenDoorCommand"/>.
    /// </para>
    ///
    /// <para><b>Struttura interna:</b></para>
    /// <list type="bullet">
    ///   <item><b>Unknown</b>: stato non noto alla trace.</item>
    ///   <item><b>Open</b>: porta aperta.</item>
    ///   <item><b>Closed</b>: porta chiusa.</item>
    ///   <item><b>Locked</b>: porta chiusa e bloccata.</item>
    /// </list>
    /// </summary>
    public enum DoorState
    {
        Unknown = 0,
        Open = 1,
        Closed = 2,
        Locked = 3
    }

    // =============================================================================
    // BeliefEntryRef
    // =============================================================================
    /// <summary>
    /// <para>
    /// Snapshot minimale della credenza che ha contribuito alla scelta del target
    /// di movimento. Non e' una reference live a <see cref="BeliefEntry"/> e non
    /// consente all'EL di interrogare o modificare il BeliefStore.
    /// </para>
    ///
    /// <para><b>Vincolo di Onniscienza</b></para>
    /// <para>
    /// Il riferimento deve arrivare da chi ha gia' compiuto la scelta decisionale,
    /// per esempio tramite un futuro job o tramite `BeliefQueryResult`. L'EL non deve
    /// cercare a posteriori nel BeliefStore per indovinare quale belief fosse la causa.
    /// </para>
    ///
    /// <para><b>Struttura interna:</b></para>
    /// <list type="bullet">
    ///   <item><b>Category</b>: categoria semantica del belief usato.</item>
    ///   <item><b>BeliefId</b>: id locale per-NPC del belief, se noto.</item>
    ///   <item><b>EntityId</b>: id entita' o oggetto collegato, se noto.</item>
    ///   <item><b>Confidence</b>: confidence snapshot al tick di emissione.</item>
    ///   <item><b>Freshness</b>: freshness snapshot al tick di emissione.</item>
    ///   <item><b>AgeTicks</b>: eta' della credenza al tick di emissione.</item>
    /// </list>
    /// </summary>
    [Serializable]
    public struct BeliefEntryRef
    {
        public BeliefCategory Category;
        public int BeliefId;
        public int EntityId;
        public float Confidence;
        public float Freshness;
        public long AgeTicks;
    }

    // =============================================================================
    // PlannerCandidate
    // =============================================================================
    /// <summary>
    /// <para>
    /// Descrive una modalita' valutata nella fase di pianificazione del movimento.
    /// La lista dei candidati permette al pannello EL di mostrare non solo cosa e'
    /// stato scelto, ma anche cosa e' stato scartato.
    /// </para>
    ///
    /// <para><b>Lista candidati non prescrittiva</b></para>
    /// <para>
    /// Questo tipo e' un record diagnostico. Non deve diventare la sorgente da cui il
    /// pathfinder rilegge la propria scelta. La costruzione dei candidati deve avvenire
    /// come side-effect esplicativo dopo o durante la valutazione gia' necessaria al
    /// movimento reale.
    /// </para>
    ///
    /// <para><b>Struttura interna:</b></para>
    /// <list type="bullet">
    ///   <item><b>Mode</b>: modalita' candidata.</item>
    ///   <item><b>Valid</b>: true se il candidato era praticabile.</item>
    ///   <item><b>EstimatedCost</b>: costo stimato, con -1 se non applicabile.</item>
    ///   <item><b>InvalidReason</b>: motivo di scarto, `None` se valido.</item>
    ///   <item><b>Note</b>: nota diagnostica breve opzionale.</item>
    /// </list>
    /// </summary>
    [Serializable]
    public struct PlannerCandidate
    {
        public PlannerMode Mode;
        public bool Valid;
        public float EstimatedCost;
        public InvalidReason InvalidReason;
        public string Note;
    }

    // =============================================================================
    // FailureDetail
    // =============================================================================
    /// <summary>
    /// <para>
    /// Dettaglio strutturato di un evento di fallimento del movimento. Viene popolato
    /// solo quando <see cref="PathExecutionEvent.EventType"/> vale
    /// <see cref="PathEventType.Failed"/>.
    /// </para>
    ///
    /// <para><b>Failure ladder aware</b></para>
    /// <para>
    /// Il codice reale non fallisce subito al primo blocco: puo' entrare in back-off
    /// e tentare replan. Per questo il dettaglio conserva anche lo stage di back-off
    /// finale, quando disponibile.
    /// </para>
    ///
    /// <para><b>Struttura interna:</b></para>
    /// <list type="bullet">
    ///   <item><b>FailureType</b>: categoria del fallimento.</item>
    ///   <item><b>HasBlockingCell</b>: indica se `BlockingCell` e' significativo.</item>
    ///   <item><b>BlockingCell</b>: cella che ha causato o rappresentato il blocco.</item>
    ///   <item><b>HasBlockingNpcId</b>: indica se `BlockingNpcId` e' significativo.</item>
    ///   <item><b>BlockingNpcId</b>: NPC che occupava la cella, se noto.</item>
    ///   <item><b>BlockedTicks</b>: tick bloccati accumulati o osservati.</item>
    ///   <item><b>BackOffStage</b>: stage failure ladder, 0 se non applicabile.</item>
    ///   <item><b>LastActiveMode</b>: stringa reale della modalita' prima del fallimento.</item>
    ///   <item><b>OscillationFlag</b>: true se il sistema ha riconosciuto oscillazione.</item>
    /// </list>
    /// </summary>
    [Serializable]
    public struct FailureDetail
    {
        public FailureType FailureType;
        public bool HasBlockingCell;
        public Vector2Int BlockingCell;
        public bool HasBlockingNpcId;
        public int BlockingNpcId;
        public int BlockedTicks;
        public int BackOffStage;
        public string LastActiveMode;
        public bool OscillationFlag;
    }

    // =============================================================================
    // DoorInteractionDetail
    // =============================================================================
    /// <summary>
    /// <para>
    /// Dettaglio specializzato per un evento di interazione con porta. Viene popolato
    /// solo quando <see cref="PathExecutionEvent.EventType"/> vale
    /// <see cref="PathEventType.DoorInteraction"/>.
    /// </para>
    ///
    /// <para><b>Porte come WorldObject espliciti</b></para>
    /// <para>
    /// La trace non apre e non chiude porte. Registra lo stato osservato attorno al
    /// comando o al tentativo di attraversamento, cosi' la UI puo' distinguere porta
    /// chiusa, porta aperta, porta bloccata e comando emesso.
    /// </para>
    ///
    /// <para><b>Struttura interna:</b></para>
    /// <list type="bullet">
    ///   <item><b>DoorObjectId</b>: id runtime della porta.</item>
    ///   <item><b>DoorCell</b>: cella della porta.</item>
    ///   <item><b>StateBefore</b>: stato prima dell'interazione.</item>
    ///   <item><b>StateAfter</b>: stato dopo l'interazione.</item>
    ///   <item><b>CommandEmitted</b>: true se e' stato emesso `OpenDoorCommand`.</item>
    ///   <item><b>AccessGranted</b>: true se la porta era apribile per l'NPC.</item>
    /// </list>
    /// </summary>
    [Serializable]
    public struct DoorInteractionDetail
    {
        public int DoorObjectId;
        public Vector2Int DoorCell;
        public DoorState StateBefore;
        public DoorState StateAfter;
        public bool CommandEmitted;
        public bool AccessGranted;
    }

    // =============================================================================
    // MovementIntentTrace
    // =============================================================================
    /// <summary>
    /// <para>
    /// Trace emessa una sola volta per intent di movimento. Risponde alla domanda:
    /// "perche' questo NPC vuole raggiungere questo target adesso?".
    /// </para>
    ///
    /// <para><b>Trace di nascita intent</b></para>
    /// <para>
    /// La trace deve essere prodotta quando il movimento vede un intent nuovo, non a
    /// ogni tick. I campi sono snapshot: se il target o la belief cambiano, le fasi
    /// successive dovranno produrre un nuovo intent e quindi una nuova trace.
    /// </para>
    ///
    /// <para><b>Struttura interna:</b></para>
    /// <list type="bullet">
    ///   <item><b>NpcId</b>: NPC proprietario dell'intent.</item>
    ///   <item><b>Tick</b>: tick di emissione.</item>
    ///   <item><b>IntentId</b>: id EL dell'intent.</item>
    ///   <item><b>SourceJobId</b>: job sorgente, se noto.</item>
    ///   <item><b>SourceStepId</b>: step sorgente, se noto.</item>
    ///   <item><b>MovementPurpose</b>: scopo normalizzato.</item>
    ///   <item><b>TargetType</b>: tipo di target.</item>
    ///   <item><b>TargetCell</b>: cella finale richiesta.</item>
    ///   <item><b>TargetObjectId</b>: oggetto target, 0 se assente.</item>
    ///   <item><b>HasBeliefBasis</b>: indica se `BeliefBasis` e' popolato.</item>
    ///   <item><b>BeliefBasis</b>: snapshot della belief causale, se presente.</item>
    ///   <item><b>Urgency</b>: urgenza normalizzata 0-1, se nota.</item>
    ///   <item><b>VerbosityLevel</b>: livello EL attivo all'emissione.</item>
    /// </list>
    /// </summary>
    [Serializable]
    public sealed class MovementIntentTrace
    {
        public int NpcId;
        public long Tick;
        public int IntentId;
        public string SourceJobId = string.Empty;
        public string SourceStepId = string.Empty;
        public MovementPurpose MovementPurpose;
        public MovementTargetType TargetType;
        public Vector2Int TargetCell;
        public int TargetObjectId;
        public bool HasBeliefBasis;
        public BeliefEntryRef BeliefBasis;
        public float Urgency;
        public int VerbosityLevel;
    }

    // =============================================================================
    // PathPlanTrace
    // =============================================================================
    /// <summary>
    /// <para>
    /// Trace emessa quando una pianificazione di movimento ha scelto una modalita'
    /// iniziale. Risponde alla domanda: "perche' e' stata scelta questa modalita'
    /// invece delle alternative disponibili?".
    /// </para>
    ///
    /// <para><b>Pianificazione spiegabile</b></para>
    /// <para>
    /// La trace collega un `intentId` a un `planId` e conserva candidati, ragione di
    /// selezione e dati principali del piano. Non contiene ogni cella del path; per
    /// quello esistono gia' debug path e, in futuro, eventi di esecuzione.
    /// </para>
    ///
    /// <para><b>Struttura interna:</b></para>
    /// <list type="bullet">
    ///   <item><b>NpcId</b>: NPC proprietario del piano.</item>
    ///   <item><b>Tick</b>: tick di emissione.</item>
    ///   <item><b>IntentId</b>: intent sorgente.</item>
    ///   <item><b>PlanId</b>: id EL del piano.</item>
    ///   <item><b>StartCell</b>: cella di partenza.</item>
    ///   <item><b>GoalCell</b>: cella obiettivo finale.</item>
    ///   <item><b>SelectedMode</b>: modalita' scelta.</item>
    ///   <item><b>Candidates</b>: candidati valutati.</item>
    ///   <item><b>SelectionReason</b>: motivazione sintetica della scelta.</item>
    ///   <item><b>MacroRouteNodes</b>: node id landmark, se disponibili.</item>
    ///   <item><b>MacroRouteCost</b>: costo stimato macro-route, -1 se non applicabile.</item>
    ///   <item><b>HasLocalRouteFirstStep</b>: indica se `LocalRouteFirstStep` e' valido.</item>
    ///   <item><b>LocalRouteFirstStep</b>: primo step direct/prefix, se noto.</item>
    ///   <item><b>VerbosityLevel</b>: livello EL attivo all'emissione.</item>
    /// </list>
    /// </summary>
    [Serializable]
    public sealed class PathPlanTrace
    {
        public int NpcId;
        public long Tick;
        public int IntentId;
        public int PlanId;
        public Vector2Int StartCell;
        public Vector2Int GoalCell;
        public PlannerMode SelectedMode;
        public List<PlannerCandidate> Candidates = new List<PlannerCandidate>(4);
        public SelectionReason SelectionReason;
        public int[] MacroRouteNodes;
        public float MacroRouteCost = -1f;
        public bool HasLocalRouteFirstStep;
        public Vector2Int LocalRouteFirstStep;
        public int VerbosityLevel;
    }

    // =============================================================================
    // PathExecutionEvent
    // =============================================================================
    /// <summary>
    /// <para>
    /// Evento discreto emesso durante l'esecuzione del movimento. Risponde alla
    /// domanda: "che cosa e' successo durante il percorso e perche' il comportamento
    /// e' cambiato, fallito o terminato?".
    /// </para>
    ///
    /// <para><b>Timeline runtime</b></para>
    /// <para>
    /// La UI laterale destra e il log JSONL potranno mostrare questi eventi come
    /// timeline. A verbosita' 1-2 gli eventi devono rappresentare transizioni e
    /// anomalie; `StepSuccess` e' riservato alla verbosita' 3 per evitare rumore.
    /// </para>
    ///
    /// <para><b>Struttura interna:</b></para>
    /// <list type="bullet">
    ///   <item><b>NpcId</b>: NPC proprietario dell'evento.</item>
    ///   <item><b>Tick</b>: tick di emissione.</item>
    ///   <item><b>IntentId</b>: intent sorgente.</item>
    ///   <item><b>PlanId</b>: piano sorgente, 0 se non noto.</item>
    ///   <item><b>EventType</b>: tipo dell'evento.</item>
    ///   <item><b>ActiveMode</b>: stringa reale della modalita' attiva.</item>
    ///   <item><b>CurrentCell</b>: posizione NPC al momento dell'evento.</item>
    ///   <item><b>TargetCell</b>: target effettivo al momento dell'evento.</item>
    ///   <item><b>HasFailureDetail</b>: indica se `FailureDetail` e' valido.</item>
    ///   <item><b>FailureDetail</b>: dettaglio fallimento, solo per `Failed`.</item>
    ///   <item><b>HasDoorDetail</b>: indica se `DoorDetail` e' valido.</item>
    ///   <item><b>DoorDetail</b>: dettaglio porta, solo per `DoorInteraction`.</item>
    ///   <item><b>VerbosityLevel</b>: livello EL attivo all'emissione.</item>
    ///   <item><b>Summary</b>: testo breve opzionale per UI/log, non sorgente logica.</item>
    /// </list>
    /// </summary>
    [Serializable]
    public sealed class PathExecutionEvent
    {
        public int NpcId;
        public long Tick;
        public int IntentId;
        public int PlanId;
        public PathEventType EventType;
        public string ActiveMode = string.Empty;
        public Vector2Int CurrentCell;
        public Vector2Int TargetCell;
        public bool HasFailureDetail;
        public FailureDetail FailureDetail;
        public bool HasDoorDetail;
        public DoorInteractionDetail DoorDetail;
        public int VerbosityLevel;
        public string Summary = string.Empty;
    }
}
