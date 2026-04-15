using System;
using UnityEngine;

namespace Arcontio.Core.Config
{
    /// <summary>
    /// SimulationParams:
    /// contenitore dei parametri di simulazione letti da Resources/Arcontio/Config/game_params.json.
    ///
    /// NOTE IMPORTANTI (storico patch):
    /// - Questo file viene deserializzato tramite JsonUtility.
    /// - JsonUtility ignora i campi sconosciuti: è lecito avere nel JSON sezioni consumate
    ///   da altri sistemi (es: Logging per il logger) senza duplicarle qui.
    /// - Qui mettiamo SOLO parametri che influenzano la simulazione o debug tooling
    ///   strettamente legato alla simulazione (overlay/read-only).
    ///
    /// Patch 0.02D2_1 (questo file):
    /// - Migrazione configurazione debug landmarks:
    ///   PRIMA: root "debug_landmarks".
    ///   ORA:   "landmarks.debug".
    ///
    /// Motivazione:
    /// - un solo source of truth: il debug è una proprietà del sistema landmarks.
    /// - riduce errori di configurazione (come quello appena visto: overlay non si vede).
    /// </summary>
    [Serializable]
    public sealed class SimulationParams
    {
        // ---------------- Mappa ----------------
        public int worldWidth = 64;
        public int worldHeight = 64;

        // ---------------- Localizzazione (usata anche dal logger) ----------------
        public string Language = "it";

        // ---------------- Inventario / Carry capacity ----------------
        public InventoryParams inventory = new InventoryParams();

        // ---------------- Perception cone params (ARCONTIO Standard) ----------------
        public int npcVisionRangeCells = 6;
        public int npcOperationalRangeCells = 0;
        public bool npcVisionUseCone = true;
        public float npcVisionConeSlope = 1.0f;
        public int npcVisionFovDegrees = 90;

        // ---------------- Debug FOV heatmap (view overlay) ----------------
        public DebugFovParams debug_fov = new DebugFovParams();

        // ---------------- Landmark pathfinding (v0.02) ----------------
        public LandmarkSystemParams landmarks = new LandmarkSystemParams();

        // ---------------- Movement (v0.02.05.B) ----------------
        public MovementParams movement = new MovementParams();

        // ---------------- Movement Explainability Layer (v0.04.1.d) ----------------
        // Parametri del livello EL pathfinding. La configurazione vive accanto ai
        // parametri di simulazione, ma non attiva ancora emissione trace: le sessioni
        // successive collegheranno questi valori agli emitter e agli adapter runtime.
        public MovementExplainabilityParams explainability = new MovementExplainabilityParams();

        // ---------------- Memory/Belief/Decision Explainability Layer (v0.05.32) ----------------
        // Parametri del livello EL dedicato al ciclo cognitivo MemoryStore ->
        // BeliefStore -> BeliefQuery -> Decision. Come per l'EL pathfinding, questa
        // sezione contiene solo configurazione read-only: l'emissione concreta viene
        // agganciata nelle sessioni successive.
        public MemoryBeliefDecisionExplainabilityParams memory_belief_decision_explainability =
            new MemoryBeliefDecisionExplainabilityParams();

        // ---------------- Decision Layer (v0.05.40) ----------------
        // Parametri comportamentali minimi della selezione intenzioni. Separiamo
        // questa sezione dall'EL: l'EL osserva, mentre "decision" puo' modificare la
        // policy di scelta, per esempio rendendo i test runtime deterministici.
        public DecisionRuntimeParams decision = new DecisionRuntimeParams();

        // ---------------- GVD-DIN (v0.03) ----------------
        // Sistema GVD dinamico condition-based + pruning.
        // Attivo quando gvd_din.enabled=true E hybrid_landmark.use_hybrid_extractor=false.
        // Mantenuto per backward compatibility e confronto visivo.
        public GvdDinParams gvd_din = new GvdDinParams();

        // ---------------- Hybrid Landmark Extractor (v0.03.02.a) ----------------
        // Sistema ibrido 6-passi: Distance Transform → Bridge Detection →
        // Flood Fill → Landmark per regione → ChokePoint → Pruning.
        // Quando use_hybrid_extractor=true sostituisce GVD-DIN come generatore LM.
        public HybridLandmarkParams hybrid_landmark = new HybridLandmarkParams();

        // ---------------- Landmark Perception (v0.03.03.a — Landmark Perception) ----------------
        // Apprendimento visivo dei landmark tramite FOV degli NPC.
        // Complementare al learning fisico (NotifyNpcMovedForLandmarkLearning).
        public LandmarkPerceptionParams landmark_perception = new LandmarkPerceptionParams();

        // ---------------- Memory System (v0.04.a) ----------------
        // Parametri globali del sistema di memoria NPC.
        // I tratti individuali (Resilience, Rumination, ecc.) vengono letti dal DNA.
        public MemorySystemParams memory = new MemorySystemParams();
    }

    // =============================================================================
    // DecisionRuntimeParams
    // =============================================================================
    /// <summary>
    /// <para>
    /// DTO serializzabile dei parametri runtime minimi del Decision Layer.
    /// </para>
    ///
    /// <para><b>Selezione controllabile da scenario</b></para>
    /// <para>
    /// La simulazione normale puo' usare una selezione weighted random top-N, mentre
    /// i test QA possono impostare <c>selectionMode</c> a <c>DeterministicTop1</c>
    /// per scegliere sempre il candidato disponibile con score piu' alto.
    /// </para>
    ///
    /// <para><b>Struttura interna:</b></para>
    /// <list type="bullet">
    ///   <item><b>selectionMode</b>: <c>WeightedRandomTopN</c> oppure <c>DeterministicTop1</c>.</item>
    ///   <item><b>topN</b>: numero massimo di candidati ammessi alla selezione probabilistica.</item>
    ///   <item><b>noise01</b>: rumore base della roulette weighted random.</item>
    ///   <item><b>impulsivityNoiseBonus</b>: bonus di rumore derivato dal DNA dell'NPC.</item>
    ///   <item><b>minimumWeight</b>: peso minimo per evitare roulette con peso zero.</item>
    /// </list>
    /// </summary>
    [Serializable]
    public sealed class DecisionRuntimeParams
    {
        public string selectionMode = "WeightedRandomTopN";
        public int topN = 3;
        public float noise01 = 0.15f;
        public float impulsivityNoiseBonus = 0.35f;
        public float minimumWeight = 0.001f;
    }

    // ============================================================
    // INVENTORY
    // ============================================================
    [Serializable]
    public sealed class InventoryParams
    {
        public int inventory_max_units = 3;
    }

    // ============================================================
    // DEBUG FOV
    // ============================================================
    [Serializable]
    public sealed class DebugFovParams
    {
        public bool enabled = false;
        public int window_ticks = 8;
        public bool use_los = true;
        public bool activeNpcOnly = true;
    }

    // ============================================================
    // ============================================================
    // MOVEMENT (Patch 0.02.07.A)
    // ============================================================
    [Serializable]
    public sealed class MovementParams
    {
        // Timeout in ticks spendibili prima di stoppare il movimento senza condizioni particolari
        public int intentStuckTicksDefault = 12;

        // Timeout in ticks spendibili prima di stoppare il movimento nella fase finale
        public int intentStuckTicksLastMile = 8;

        // Timeout in ticks spendibili prima di stoppare il movimento che è già in blocco
        public int intentStuckTicksBlocked = 6;

        // ── DIRECT PATH CON COMMITMENT PERCETTIVO (Patch 0.02.07.A) ──────────
        //
        // Implementa il modello "Movimento Direct con Commitment Percettivo":
        //   Direct = innesco percettivo (Range + FOV + LOS sul TARGET)
        //          + esecuzione inerziale breve su prefix pre-calcolato.
        //
        // Lunghezza del prefix path committed (in celle).
        // L'NPC costruisce questo segmento al momento dell'acquisizione del
        // direct e lo esegue senza ricontrollare la visibilità completa.
        // Solo la prossima cella viene verificata (IsMovementBlocked) a ogni step.
        // Al termine del prefix, rivaluta: se il target è ancora visibile
        // (Range + FOV + LOS) rinnova il commitment; altrimenti esce dal direct.
        // Range raccomandato: 2-4 celle. Default: 3.
        public int directPrefixCells = 3;

        // Se true, l'acquisizione del direct richiede che il TARGET sia
        // percettivamente visibile (Range + FOV + LOS) al momento dell'innesco.
        // Se false, usa solo IsMovementBlocked per l'attraversabilità (legacy).
        public bool directCheckFovOnAcquisition = true;

        // ── FAILURE LADDER: BACK-OFF / REPLAN (v0.03.05-FailureLadder) ──────
        //
        // Quando un NPC è bloccato per intentStuckTicksDefault tick consecutivi,
        // invece di cancellare subito l'intent, entra in back-off e tenta un replan.
        //
        // Stage 1 (primo stuck): aspetta backoff_stage1_ticks, poi replan.
        // Stage 2 (secondo stuck): aspetta backoff_stage2_ticks, poi replan.
        // Stage > backoff_max_stages: cancella l'intent (comportamento precedente).
        //
        // Valori conservativi di default:
        //   stage1 = 24 tick (~2 cicli IdleScan = NPC ha il tempo di ruotare e
        //             aggiornare la percezione prima di ritentare)
        //   stage2 = 60 tick (ostacolo dinamico: attesa più lunga)
        //   max_stages = 2 (dopo 2 fallimenti: rinuncia)
        public int backoff_stage1_ticks = 24;
        public int backoff_stage2_ticks = 60;
        public int backoff_max_stages   = 2;

        // ── BLACKLIST EDGE (v0.03.05-FailureLadder) ──────────────────────────
        // Quando l'NPC entra in back-off mentre percorre un edge della macro-route,
        // la confidence di quell'edge viene ridotta per penalizzarlo nel prossimo A*.
        // Stage 1 (primo stuck): penalità lieve — l'edge potrebbe essere temporaneamente
        //   bloccato (altro NPC in transito, ostacolo dinamico).
        // Stage 2+ (secondo stuck): penalità forte — l'edge è probabilmente inutilizzabile.
        // La penalità viene applicata sia in NpcLandmarkMemory che in NpcComplexEdgeMemory.
        public float blacklist_penalty_stage1 = 0.12f;
        public float blacklist_penalty_stage2 = 0.35f;
    }

    // =============================================================================
    // MovementExplainabilityParams
    // =============================================================================
    /// <summary>
    /// <para>
    /// DTO serializzabile dei parametri dell'Explainability Layer dedicato al
    /// pathfinding. Viene letto da <c>game_params.json</c> tramite
    /// <c>JsonUtility</c> insieme agli altri parametri globali di simulazione.
    /// </para>
    ///
    /// <para><b>Separazione configurazione / comportamento runtime</b></para>
    /// <para>
    /// Questa classe contiene solo dati di configurazione: non crea registry, non
    /// apre file di log, non consulta NPC e non abilita da sola la UI. I sistemi
    /// successivi useranno questi valori come input read-only per decidere quante
    /// trace conservare, quanti NPC osservare e quanta verbosita' emettere.
    /// </para>
    ///
    /// <para><b>Struttura interna:</b></para>
    /// <list type="bullet">
    ///   <item><b>enabled</b>: master switch del livello EL pathfinding.</item>
    ///   <item><b>defaultVerbosity</b>: livello di dettaglio iniziale delle trace.</item>
    ///   <item><b>maxTrackedNpcs</b>: limite massimo di NPC osservati automaticamente.</item>
    ///   <item><b>trackedNpcIds</b>: lista opzionale di NPC esplicitamente osservati.</item>
    ///   <item><b>trackActiveNpcOnly</b>: filtro per privilegiare NPC con movimento attivo.</item>
    ///   <item><b>ringBuffer_*</b>: capacita' bounded degli store EL in memoria.</item>
    ///   <item><b>writeJsonLog</b>: abilita il futuro sink JSONL diagnostico.</item>
    ///   <item><b>jsonLogFileNamePattern</b>: pattern nome file del futuro log JSONL.</item>
    /// </list>
    /// </summary>
    [Serializable]
    public sealed class MovementExplainabilityParams
    {
        // Master switch difensivo: di default l'EL resta spento, cosi' la presenza
        // della sezione JSON non cambia comportamento runtime finche' gli emitter non
        // verranno collegati nelle sessioni successive.
        public bool enabled = false;

        // Livello base di dettaglio: 0 = off/minimo, 1 = eventi principali,
        // 2 = eventi intermedi, 3 = eventi molto verbosi come step runtime.
        public int defaultVerbosity = 0;

        // Limite automatico per evitare che una scena con molti NPC generi troppi
        // dati EL. Se trackedNpcIds contiene valori, quella lista avra' priorita'.
        public int maxTrackedNpcs = 3;

        // Lista esplicita di NPC da osservare. Array vuoto = nessun pin manuale;
        // il futuro selector potra' allora usare maxTrackedNpcs e activeNpcOnly.
        public int[] trackedNpcIds = Array.Empty<int>();

        // Se true, la selezione automatica preferira' NPC con movimento/intento
        // attivo. Il campo e' solo config: la policy concreta arrivera' con gli emitter.
        public bool trackActiveNpcOnly = true;

        // Numero di intent conservati per NPC. Gli intent sono rari, quindi un buffer
        // piccolo e' sufficiente per capire la storia recente.
        public int ringBuffer_intent = 10;

        // Numero di plan trace conservate per NPC. Anche i piani sono eventi discreti,
        // separati dagli eventi tick-by-tick dell'esecuzione.
        public int ringBuffer_plan = 10;

        // Capacita' eventi a verbosita' bassa/media. Mantiene la timeline recente
        // senza trasformare il debug in una crescita non bounded.
        public int ringBuffer_events_low = 60;

        // Capacita' eventi a verbosita' alta. Serve quando si vogliono vedere piu'
        // micro-eventi senza cambiare la struttura dello store.
        public int ringBuffer_events_high = 200;

        // Soglia da cui usare il buffer eventi alto. Con defaultVerbosity 3, per
        // esempio, il futuro registry potra' scegliere eventCapacity = high.
        public int verbosityHighThreshold = 3;

        // Sink JSONL separato dalla fonte primaria in memoria. Anche quando sara'
        // implementato, il file restera' export diagnostico e non dipendenza sim.
        public bool writeJsonLog = false;

        // Pattern del futuro file JSONL. Il suffisso .jsonl comunica che il formato
        // sara' append-only, una trace JSON per riga.
        public string jsonLogFileNamePattern = "arcontio_el_pathfinding_{yyyyMMdd_HHmmss}.jsonl";
    }

    // =============================================================================
    // MemoryBeliefDecisionExplainabilityParams
    // =============================================================================
    /// <summary>
    /// <para>
    /// DTO serializzabile dei parametri dell'Explainability Layer dedicato al ciclo
    /// cognitivo MemoryStore, BeliefStore, BeliefQuery e Decision Layer.
    /// </para>
    ///
    /// <para><b>Configurazione diagnostica non comportamentale</b></para>
    /// <para>
    /// Questa classe non apre file, non crea registry, non legge NPC e non attiva da
    /// sola emissioni runtime. I futuri emitter useranno questi valori come input
    /// read-only per decidere se produrre trace JSONL, quali famiglie registrare e
    /// quanto dettaglio includere.
    /// </para>
    ///
    /// <para><b>Struttura interna:</b></para>
    /// <list type="bullet">
    ///   <item><b>enabled</b>: master switch dell'EL-MBD.</item>
    ///   <item><b>defaultVerbosity</b>: livello di dettaglio predefinito.</item>
    ///   <item><b>maxTrackedNpcs</b>: limite automatico degli NPC osservati.</item>
    ///   <item><b>trackedNpcIds</b>: lista opzionale di NPC osservati esplicitamente.</item>
    ///   <item><b>trackActiveNpcOnly</b>: filtro futuro per ridurre rumore diagnostico.</item>
    ///   <item><b>writeJsonLog</b>: abilita il futuro sink JSONL append-only.</item>
    ///   <item><b>jsonLogFileNamePattern</b>: pattern del file JSONL diagnostico.</item>
    ///   <item><b>log*</b>: switch granulari per famiglie memory, belief, query, decision e bridge.</item>
    ///   <item><b>include*</b>: policy di dettaglio per candidati e breakdown.</item>
    /// </list>
    /// </summary>
    [Serializable]
    public sealed class MemoryBeliefDecisionExplainabilityParams
    {
        public bool enabled = false;
        public int defaultVerbosity = 0;
        public int maxTrackedNpcs = 5;
        public int[] trackedNpcIds = Array.Empty<int>();
        public bool trackActiveNpcOnly = false;

        public bool writeJsonLog = false;
        public string jsonLogFileNamePattern = "arcontio_el_mbd_{yyyyMMdd_HHmmss}.jsonl";

        public bool logMemory = true;
        public bool logBelief = true;
        public bool logQuery = true;
        public bool logDecision = true;
        public bool logBridge = true;

        public bool includeCandidates = true;
        public bool includeScoreBreakdown = true;
        public bool includeRejectedCandidates = false;
    }


    // ============================================================
    // LANDMARKS (v0.02)
    // ============================================================
    [Serializable]
    public sealed class LandmarkSystemParams
    {
        // Master enable
        public bool enableLandmarkSystem = false;

        // Caps (NPC-side, Day3+)
        public int maxLandmarksPerNpc = 64;
        public int maxEdgesPerNpc = 192;
        public int maxPoiAnchorsPerNpc = 32;

        // Cap (World-side registry, Day2)
        public int maxWorldLandmarks = 512;

        // Adjacency sparsa
        public int maxEdgesPerLandmark = 8;

        // Merge
        public float merge_radius = 1.5f;

        // Pruning di prossimità con i Doorway (PATCH 6 — v0.04.10.h):
        // Disattiva Junction/AreaCenter entro questa distanza (celle) da un Doorway.
        // 0 = pruning disabilitato.
        public float door_prune_radius = 2.0f;

        // Waypoint intermedi in spazi aperti (PATCH 7 — v0.04.10.i):
        // Inserisce AreaCenter tra coppie di landmark più lontane di questo valore (celle).
        // 0 = waypoint disabilitati.
        public int waypoint_min_distance = 17;
        // DT minimo al punto candidato: filtra corridoi stretti (DT bassa = vicino ai muri).
        public int waypoint_min_dt = 3;

        // Nota (v0.03.02.a): candidate detection params rimossi.
        // junction_min_exits/candidate_cooldown_ticks erano del vecchio sistema Doorway/Junction.

        // Eviction/deactivation params
        public LandmarkEvictionParams eviction = new LandmarkEvictionParams();

        // Retry/backoff params
        public LandmarkRetryParams retry = new LandmarkRetryParams();

        // Debug (view-only)
        public DebugLandmarksParams debug = new DebugLandmarksParams();

        // Ricerca locale goal-oriented (bounded, JPS-style)
        // Nota molto importante:
        // questi parametri NON trasformano il sistema in un pathfinding globale onnisciente.
        // Servono solo a controllare il solver locale usato quando Direct/LM non bastano.
        public LandmarkLocalSearchParams localSearch = new LandmarkLocalSearchParams();
    }

    // Nota (v0.03.02.a): LandmarkCandidateParams rimossa.
    // candidate_cooldown_ticks e junction_min_exits erano usati solo nel vecchio
    // sistema Doorway/Junction eliminato in questa patch.

    [Serializable]
    public sealed class LandmarkEvictionParams
    {
        public int eviction_stale_ticks = 600;
        public int eviction_cooldown_ticks = 120;
    }

    [Serializable]
    public sealed class LandmarkRetryParams
    {
        public int retry_backoff_min_ticks = 10;
        public int retry_backoff_max_ticks = 80;
    }

    [Serializable]
    public sealed class DebugLandmarksParams
    {
        public bool enabled      = false;
        public bool activeNpcOnly = true;
        // Nota (v0.03.02.a): microTestDummyGraph e microTestDummyDistanceCells rimossi.
        // Erano scaffolding di test del vecchio sistema. World.cs li usava solo
        // nei metodi GetNpcLandmarkDebugInfo — da aggiornare contestualmente.
    }

    [Serializable]
    public sealed class LandmarkLocalSearchParams
    {
        // Master enable della ricerca locale.
        public bool enabled = true;

        // Se true, il solver locale deve usare la variante JPS-style prevista dal progetto.
        // Se false, il codice può eventualmente ripiegare sul solver bounded legacy.
        public bool useJumpPointSearch = true;

        // Budget massimo di espansioni/visitazioni locali.
        public int maxExpandedNodes = 64;

        // Fail-safe assoluto anti-loop.
        public int maxIterations = 128;

        // Raggio massimo della ricerca locale rispetto all'origine della search.
        public int maxSearchRadius = 10;

        // Distanza massima di salto rettilineo per la variante JPS-style bounded.
        public int maxJumpDistance = 12;

        // Peso euristico verso il target.
        public float heuristicWeight = 1.0f;

        // Memoria locale breve per evitare loop/rimbalzi.
        public int recentVisitedMemory = 16;

        // Debug verboso della ricerca locale.
        public bool debugLog = false;

        // Numero minimo di step per cui la local search mantiene ownership
        // del movimento prima di poter essere rilasciata alla macro-navigation.
        public int commitMinSteps = 3;

        // Se true, un replan locale non puo' proporre immediatamente il passo
        // inverso a quello appena riuscito, riducendo i ping-pong su due celle.
        public bool preventImmediateBacktrack = true;

        // ========================================================
        // PATCH 0.02.05.4 - QUALITA' DEL PATH / FALLBACK / LEARNING
        // ========================================================

        // Se true il path locale trovato viene ripulito con uno smoothing ortogonale
        // conservativo, cosi' evitiamo zig-zag inutili quando esiste un sottotratto
        // diretto realmente percorribile.
        public bool enablePathSmoothing = true;

        // Numero massimo di nodi del path grezzo che proviamo a "guardare avanti"
        // durante lo smoothing. Valori troppo alti aumentano il costo; valori troppo
        // bassi lasciano piu' zig-zag del necessario.
        public int smoothingLookahead = 8;

        // Se il primo tentativo JPS locale fallisce, abilita una seconda fase piu'
        // permissiva (budget piu' largo, raggio piu' largo) prima di rinunciare.
        public bool enableSmartFallback = true;

        // Moltiplicatore del budget di espansione usato dalla seconda fase di fallback.
        public int fallbackExpandedNodesMultiplier = 3;

        // Bonus di raggio locale concesso nella seconda fase di fallback.
        public int fallbackRadiusBonus = 6;

        // Se true, dopo il fallimento JPS locale proviamo anche una BFS bounded
        // di sicurezza. Non e' il pathfinder principale: e' una rete di salvataggio.
        public bool fallbackUseBoundedBfs = true;

        // Abilita il learning sui fallimenti della local search.
        // Lo scopo non e' dare onniscienza all'NPC, ma impedirgli di ripetere in loop
        // sempre la stessa micro-scelta locale che ha gia' fallito poco fa.
        public bool enableFailureLearning = true;

        // Per quanti tick manteniamo memoria di un fallimento locale.
        public int failureMemoryTicks = 120;

        // Dopo quante ripetizioni dello stesso fallimento iniziamo ad escalare in modo
        // piu' aggressivo budget/raggio del fallback.
        public int repeatedFailureEscalationThreshold = 2;
    }

    // ============================================================
    // GVD-DIN (v0.03)
    // ============================================================
    // GVD-DIN = Generalized Voronoi Diagram Dinamico condition-based + pruning.
    // Sostituisce la candidate detection dei landmark (vecchio sistema Doorway/Junction
    // da IsDoorDef + IsJunction hardcoded) con uno scheletro topologico derivato
    // dalla Distance Transform della mappa.
    //
    // Quando enabled=false il vecchio sistema rimane attivo invariato.
    // Quando enabled=true il LandmarkRegistry bypassa il vecchio detection e usa GvdDinComputer.
    // ============================================================
    [Serializable]
    public sealed class GvdDinParams
    {
        // Master switch: se false, il vecchio sistema Doorway/Junction resta attivo.
        public bool enabled = false;

        // Lunghezza minima (in celle) di un ramo GVD per sopravvivere al pruning.
        // Rami più corti vengono eliminati perché corrispondono a dettagli geometrici
        // irrilevanti per la navigazione (piccole nicchie, spigoli).
        // Valore consigliato: 2-4 celle.
        public int pruning_min_branch_length = 3;

        // Valore minimo della Distance Transform per candidare una cella come AreaCenter.
        // Con AC_MIN_DT=4: corridoi fino a 7c (DT_max=3) non producono AreaCenter.
        // Solo stanze con DT_max >= 4 (larghezza interna >= 8 celle) ottengono un AreaCenter.
        // Patch 0.03.01.j: alzato da 2 a 4.
        public int area_center_min_dt_value = 4;

        // Distanza minima (Manhattan) in celle tra due AreaCenter.
        // Evita che stanze grandi producano una griglia regolare di massimi locali ravvicinati.
        // Valore consigliato: 4-8 celle. Default: 5.
        public int area_center_min_spacing_cells = 5;

        // Raggio di merge specifico per i nodi GVD-DIN (sostituisce landmarks.merge_radius).
        // Un valore più alto (es. 2.5) consolida i Junction ravvicinati prodotti dal
        // criterio B nei corridoi larghi senza toccare il merge globale.
        // Default: 2.5 (corridoio 3c+T: ~6 Junction → 1-2 dopo merge).
        public float merge_radius_gvd = 2.5f;

        // Parametri debug overlay GVD-DIN.
        public GvdDinDebugParams debug = new GvdDinDebugParams();
    }

    [Serializable]
    public sealed class GvdDinDebugParams
    {
        // Se true, mostra la heatmap della Distance Transform (gradiente blu->bianco per cella).
        // Utile per verificare che la DT sia calcolata correttamente prima di guardare il GVD.
        // ATTENZIONE: costa N*M sprite renderer. Tenerlo false in produzione.
        public bool show_dt_heatmap = false;

        // Se true, mostra le celle GVD grezze pre-pruning come dot ciano.
        // Utile per verificare la correttezza topologica dello scheletro prima del pruning.
        public bool show_gvd_raw = false;

        // Se true, mostra i vertici GVD post-pruning come nodi viola nel landmark overlay.
        // Questi sono i nodi che entrano effettivamente nel LandmarkRegistry.
        public bool show_gvd_nodes = true;
    }

    // ============================================================
    // HYBRID LANDMARK EXTRACTOR PARAMS (v0.03.02.a)
    // ============================================================
    [Serializable]
    public sealed class HybridLandmarkParams
    {
        // Se true, usa HybridLandmarkExtractor al posto di GVD-DIN.
        // Se false, il comportamento è identico alle versioni precedenti.
        public bool use_hybrid_extractor = false;

        // Soglia DT che separa "zona aperta" (stanza) da "zona stretta" (corridoio).
        // Patch 0.03.02.a.6: sostituisce dt_bridge_max.
        //
        // Celle con DT >= dt_open_threshold → zona aperta → super-nodo nel grafo contratto.
        // Celle con DT <  dt_open_threshold → zona stretta → arco candidato bridge.
        //
        // Il grafo contratto su cui gira Tarjan ha come nodi le stanze e come archi
        // i corridoi — indipendentemente dalla larghezza del corridoio.
        //
        // Valore consigliato per ARCONTIO:
        //   dt_open_threshold = 2: corridoi 1-2c (DT=1) sono "stretti",
        //                          stanze con DT>=2 sono "aperte".
        //   dt_open_threshold = 3: include come "stretti" anche i bordi delle stanze.
        public int dt_open_threshold = 2;

        // Distanza minima Manhattan tra due candidati per il merge nel pruning.
        // ChokePoint entro questa distanza da un RoomCenter: il ChokePoint vince.
        // Due RoomCenter entro questa distanza: vince quello con DT più alta.
        public float merge_radius = 2.5f;

        // Numero minimo di celle perché una regione generi un landmark.
        // Regioni più piccole di questo valore vengono ignorate.
        public int min_region_area = 4;

        // Tolleranza per la mediana ortogonale (Tecnica B).
        // |distanza_sinistra - distanza_destra| <= median_tolerance → candidata.
        // Valore 1 = bilanciamento quasi perfetto richiesto.
        // Alzare → più candidati (stanze asimmetriche incluse).
        public int median_tolerance = 1;
    }

    // ============================================================
    // LANDMARK PERCEPTION (v0.03.03.a — Landmark Perception)
    // ============================================================
    [Serializable]
    public sealed class LandmarkPerceptionParams
    {
        // Se true, gli NPC imparano i landmark che vedono nel FOV (oltre che calpestando).
        public bool enabled = true;

        // Frequenza di scansione in tick (1 = ogni tick, N = ogni N tick).
        // ATTENZIONE: scegliere valori coprimi con il periodo di IdleScanSystem (12)
        // per evitare che alcune direzioni vengano sistematicamente saltate.
        // Default 1: gira ogni tick, costo minimo (landmark statici, solo Range+LOS).
        public int period = 1;

        // ── Edge soggettivi da percezione visiva (v0.03.04.c-ComplexEdge_Creation) ──────────
        //
        // Meccanismo 1 — Simultaneità visiva:
        //   due landmark visibili nello stesso tick → edge soggettivo diretto.
        //
        // Meccanismo 2 — Ibrido fisico+visivo:
        //   recording fisico attivo da nodo A + nodo B visibile nel FOV → edge provvisorio A→B.
        //   Costo = StepCount fisici da A + Manhattan(npc_pos, B).

        // Se true, abilita la creazione di edge soggettivi da percezione visiva.
        public bool subjective_edges_enabled = true;

        // Distanza Manhattan massima (in celle) tra due landmark per creare un edge visivo (Meccanismo 1).
        public int subjective_edge_max_dist = 8;

        // Confidence iniziale degli edge soggettivi visivi (Meccanismo 1 e 2).
        // Inferiore agli edge fisici (0.25f) per riflettere l'incertezza della stima visiva.
        public float subjective_edge_base_reliability = 0.15f;
    }

    // ============================================================
    // MEMORY SYSTEM (v0.04.a)
    // ============================================================
    [Serializable]
    public sealed class MemorySystemParams
    {
        // Capacità massima di tracce per NPC nel MemoryStore.
        // Valore globale: tutti gli NPC condividono lo stesso cap.
        // I tratti individuali (Resilience, Rumination, ecc.) modulano il decay,
        // non questo limite strutturale.
        public int max_traces_per_npc = 128;
    }

    // ============================================================
    // LOADER
    // ============================================================
    public static class SimulationParamsLoader
    {
        public static SimulationParams LoadFromResources(string resourcesPathNoExt)
        {
            var ta = Resources.Load<TextAsset>(resourcesPathNoExt);
            if (ta == null)
            {
                Debug.LogWarning($"[Arcontio] Missing sim params at Resources/{resourcesPathNoExt}.json. Using defaults.");
                return new SimulationParams();
            }

            try
            {
                return JsonUtility.FromJson<SimulationParams>(ta.text) ?? new SimulationParams();
            }
            catch (Exception ex)
            {
                Debug.LogError($"[Arcontio] Failed parsing sim params: {ex}");
                return new SimulationParams();
            }
        }
    }
}
