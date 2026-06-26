using System;
using Arcontio.Core.Logging;
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
        // ---------------- Runtime tick canonico ----------------
        //
        // ARC-DEC-006 e ARC-DEC-020 stabiliscono che il tick discreto globale e'
        // l'unita' temporale canonica della simulazione. La sezione "tick" e' il
        // gruppo canonico per frequenza runtime e durate multi-tick bilanciabili:
        // Movement non deve possedere parametri temporali strutturali come fonte
        // primaria, perche' movement, job execution e future cadence condividono lo
        // stesso asse temporale globale.
        public TickParams tick;

        // Campo legacy mantenuto solo come fallback per vecchi JSON pre-v0.11c.03-prep-b.
        // Il layout canonico e' "tick.ticksPerSecond"; i consumer devono passare da
        // ResolveTicksPerSecond() e non leggere direttamente questo valore.
        public int ticksPerSecond = 0;

        // ---------------- Localizzazione (usata anche dal logger) ----------------
        public string Language = "it";

        // ---------------- Diagnostica runtime / logging ----------------
        // Layout canonico v0.11d.00d. I vecchi gruppi root restano presenti sotto
        // come fallback compatibile, ma il file JSON vivo deve concentrare qui le
        // impostazioni diagnostiche accendibili o spegnibili.
        public RuntimeDiagnosticsParams logging;

        // Sezione legacy mantenuta solo come ponte per vecchi game_params.json.
        // Il layout canonico dalla v0.11d.00d e' "logging".
        public LegacyLoggingParams Logging;

        // ---------------- Inventario / Carry capacity ----------------
        public InventoryParams inventory = new InventoryParams();

        // ---------------- Perception cone params (ARCONTIO Standard) ----------------
        public int npcVisionRangeCells = 6;
        public int npcOperationalRangeCells = 0;
        public bool npcVisionUseCone = true;
        public float npcVisionConeSlope = 1.0f;
        public int npcVisionFovDegrees = 90;
        public ObjectPerceptionRuntimeParams perception = new ObjectPerceptionRuntimeParams();
        public PerceptionStateRuntimeParams perception_states = new PerceptionStateRuntimeParams();

        // ---------------- Debug FOV heatmap (view overlay) ----------------
        public DebugFovParams debug_fov = new DebugFovParams();

        // ---------------- Runtime cost observer (v0.17) ----------------
        // Osservatorio costi runtime. Il default e' spento e deve restare tale:
        // quando enabled=false il World non crea nessuna istanza osservatore, cosi'
        // i sistemi caldi non pagano misure, stringhe, liste o JSONL.
        public RuntimeCostObserverParams runtime_cost_observer = new RuntimeCostObserverParams();

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

        // ---------------- Biosphere Runtime (v0.53.1) ----------------
        // Parametri di budget e cadenza della biosfera. Questa sezione non collega
        // ancora direttamente la biosfera al SimulationHost: espone soltanto il
        // contratto configurabile che lo scheduler data-only puo' consumare.
        public BiosphereRuntimeParams biosphere = new BiosphereRuntimeParams();

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

        // =============================================================================
        // ResolveTicksPerSecond
        // =============================================================================
        /// <summary>
        /// <para>
        /// Risolve la frequenza del tick globale usando il layout canonico
        /// <c>tick.ticksPerSecond</c>, con fallback compatibile al vecchio campo root.
        /// </para>
        ///
        /// <para><b>Principio architetturale: Tick come gruppo temporale canonico</b></para>
        /// <para>
        /// Il tempo simulativo deve essere leggibile da una sola famiglia dati. Questo
        /// metodo protegge i consumer dal layout JSON concreto: il runtime legge una
        /// semantica, non una posizione sparsa nel file.
        /// </para>
        ///
        /// <para><b>Struttura interna:</b></para>
        /// <list type="bullet">
        ///   <item><b>Canonico</b>: se presente e positivo, usa <c>tick.ticksPerSecond</c>.</item>
        ///   <item><b>Legacy</b>: se il vecchio root field e' positivo, lo accetta come fallback transitorio.</item>
        ///   <item><b>Default</b>: in assenza di config valida usa il default costituzionale.</item>
        /// </list>
        /// </summary>
        public int ResolveTicksPerSecond()
        {
            if (ticksPerSecond > 0 && (tick == null || tick.ticksPerSecond == TickParams.DefaultTicksPerSecond))
                return ticksPerSecond;

            if (tick != null && tick.ticksPerSecond > 0)
                return tick.ticksPerSecond;

            return TickParams.DefaultTicksPerSecond;
        }

        // =============================================================================
        // ResolveBaseWalkCellDurationTicks
        // =============================================================================
        /// <summary>
        /// <para>
        /// Risolve la durata base del traversal one-cell multi-tick usando il gruppo
        /// canonico <c>tick</c>, con fallback temporaneo dal vecchio layout
        /// <c>movement</c>.
        /// </para>
        ///
        /// <para><b>Principio architetturale: Movement non possiede il tempo canonico</b></para>
        /// <para>
        /// Il movimento usa questa durata, ma non ne e' proprietario architetturale:
        /// la durata descrive progresso temporale runtime e appartiene al gruppo Tick.
        /// </para>
        ///
        /// <para><b>Struttura interna:</b></para>
        /// <list type="bullet">
        ///   <item><b>Canonico</b>: <c>tick.baseWalkCellDurationTicks</c>.</item>
        ///   <item><b>Legacy</b>: <c>movement.baseWalkCellDurationTicks</c> per vecchi JSON.</item>
        ///   <item><b>Default</b>: valore prudente minimo della foundation multi-tick.</item>
        /// </list>
        /// </summary>
        public int ResolveBaseWalkCellDurationTicks()
        {
            if (ticksPerSecond > 0
                && movement != null
                && movement.baseWalkCellDurationTicks > 0
                && (tick == null || tick.baseWalkCellDurationTicks == TickParams.DefaultBaseWalkCellDurationTicks))
            {
                return movement.baseWalkCellDurationTicks;
            }

            if (tick != null && tick.baseWalkCellDurationTicks > 0)
                return tick.baseWalkCellDurationTicks;

            return TickParams.DefaultBaseWalkCellDurationTicks;
        }

        // =============================================================================
        // ResolveEnableJobRunningActionTraversal
        // =============================================================================
        /// <summary>
        /// <para>
        /// Risolve il gate del traversal running action produttivo dal gruppo
        /// temporale canonico <c>tick</c>, con fallback al vecchio campo movement
        /// quando il vecchio <c>ticksPerSecond</c> root segnala un JSON legacy.
        /// </para>
        ///
        /// <para><b>Principio architetturale: gate temporale esplicito</b></para>
        /// <para>
        /// Il gate resta spento di default. La compatibilita' con il vecchio layout e'
        /// deliberatamente confinata al resolver: i consumer non leggono piu' Movement
        /// come authority temporale primaria.
        /// </para>
        ///
        /// <para><b>Struttura interna:</b></para>
        /// <list type="bullet">
        ///   <item><b>Canonico</b>: <c>tick.enableJobRunningActionTraversal</c>.</item>
        ///   <item><b>Legacy</b>: vecchio <c>movement.enableJobRunningActionTraversal</c> quando il root legacy e' presente.</item>
        ///   <item><b>Default</b>: false, nessuna attivazione implicita del traversal.</item>
        /// </list>
        /// </summary>
        public bool ResolveEnableJobRunningActionTraversal()
        {
            if (ticksPerSecond > 0 && movement != null && movement.enableJobRunningActionTraversal)
                return true;

            if (tick != null)
                return tick.enableJobRunningActionTraversal;

            return movement != null && movement.enableJobRunningActionTraversal;
        }

        // =============================================================================
        // ResolveDecisionEveryTicks
        // =============================================================================
        /// <summary>
        /// <para>
        /// Risolve la cadenza con cui l'orchestratore decisionale needs-based viene
        /// rivalutato dal runtime attuale.
        /// </para>
        ///
        /// <para><b>Principio architetturale: config cognitiva passiva e compatibile</b></para>
        /// <para>
        /// Il valore resta un parametro passivo: non crea eventi di soglia, non
        /// decide preemption e non introduce politiche future su multi-reason,
        /// deduplica, batching o cooldown. Il default conserva una cadenza prudente
        /// per il percorso Decisione -> Richiesta di incarico -> Incarico.
        /// </para>
        ///
        /// <para><b>Struttura interna:</b></para>
        /// <list type="bullet">
        ///   <item><b>Decision config</b>: legge <c>decision.decisionEveryTicks</c> se presente.</item>
        ///   <item><b>Clamp</b>: valori nulli o negativi vengono normalizzati a 1.</item>
        ///   <item><b>Default</b>: in assenza di sezione usa 25 tick.</item>
        /// </list>
        /// </summary>
        public int ResolveDecisionEveryTicks()
        {
            return Mathf.Max(1, decision?.decisionEveryTicks ?? DecisionRuntimeParams.DefaultDecisionEveryTicks);
        }

        // =============================================================================
        // ResolveBiosphereRuntimeParams
        // =============================================================================
        /// <summary>
        /// <para>
        /// Restituisce una copia normalizzata della configurazione runtime biosfera.
        /// </para>
        ///
        /// <para><b>Principio architetturale: dominio biologico budgettizzabile</b></para>
        /// <para>
        /// La biosfera deve poter essere accesa, spenta e cadenzata senza introdurre
        /// hardcode dentro <c>SimulationHost</c>. Il resolver concentra qui clamp e
        /// fallback, cosi' i consumer futuri leggono una semantica stabile e non
        /// dettagli sparsi del JSON.
        /// </para>
        ///
        /// <para><b>Struttura interna:</b></para>
        /// <list type="bullet">
        ///   <item><b>Fallback</b>: se la sezione manca, usa default conservativi.</item>
        ///   <item><b>Clamp</b>: le cadenze e i budget non positivi vengono normalizzati.</item>
        ///   <item><b>Copia</b>: il chiamante riceve un DTO indipendente dal campo serializzato.</item>
        /// </list>
        /// </summary>
        public BiosphereRuntimeParams ResolveBiosphereRuntimeParams()
        {
            return BiosphereRuntimeParams.WithFallbackDefaults(biosphere);
        }

        public void ApplyRuntimeDiagnosticsLayout()
        {
            if (logging == null)
                return;

            if (logging.devtools != null && logging.devtools.debug_fov != null)
                debug_fov = logging.devtools.debug_fov;

            if (logging.runtime_cost_observer != null)
                runtime_cost_observer = logging.runtime_cost_observer;

            if (logging.movement_explainability != null)
                explainability = logging.movement_explainability.ToMovementExplainabilityParams(explainability);

            if (logging.memory_belief_decision_explainability != null)
            {
                memory_belief_decision_explainability =
                    logging.memory_belief_decision_explainability.ToMemoryBeliefDecisionExplainabilityParams(
                        memory_belief_decision_explainability);
            }
        }

        public LoggerDiagnosticsParams ResolveLoggerDiagnostics()
        {
            if (logging == null)
                return LoggerDiagnosticsParams.FromLegacy(Logging);

            var resolved = new LoggerDiagnosticsParams
            {
                general = logging.general ?? new LoggerGeneralParams(),
                legacy_channels = logging.legacy_channels ?? new LoggerLegacyChannelParams(),
                jsonl = logging.jsonl ?? new LoggerJsonlParams(),
                telemetry = logging.telemetry ?? new LoggerTelemetryParams(),
                devtools = logging.devtools != null
                    ? logging.devtools.ToLoggerDevtoolParams()
                    : new LoggerDevtoolParams()
            };

            return resolved;
        }
    }

    // =============================================================================
    // TickParams
    // =============================================================================
    /// <summary>
    /// <para>
    /// DTO serializzabile del gruppo <c>tick</c> in <c>game_params.json</c>.
    /// </para>
    ///
    /// <para><b>Fonte canonica del tempo runtime</b></para>
    /// <para>
    /// Questo gruppo raccoglie i parametri che descrivono tempo simulativo, cadence
    /// runtime e durate base multi-tick. ARC-DEC-006 e ARC-DEC-020 richiedono un tick
    /// globale unico: le cadence future saranno frequenze calcolate su questo asse,
    /// non timeline parallele e non parametri sparsi nei domini che li consumano.
    /// </para>
    ///
    /// <para><b>Struttura interna:</b></para>
    /// <list type="bullet">
    ///   <item><b>ticksPerSecond</b>: frequenza del tick globale prodotta da SimulationHost.</item>
    ///   <item><b>baseWalkCellDurationTicks</b>: durata base del traversal cardinale one-cell.</item>
    ///   <item><b>enableJobRunningActionTraversal</b>: gate esplicito del path Job running action traversal.</item>
    /// </list>
    /// </summary>
    [Serializable]
    public sealed class TickParams
    {
        public const int DefaultTicksPerSecond = 5;
        public const int DefaultBaseWalkCellDurationTicks = 3;

        public int ticksPerSecond = DefaultTicksPerSecond;
        public int baseWalkCellDurationTicks = DefaultBaseWalkCellDurationTicks;
        public bool enableJobRunningActionTraversal = false;
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
    ///   <item><b>enableJobDecisionOrchestrator</b>: abilita il nuovo percorso Decisione -> JobRequest -> Job per i bisogni gia' migrati.</item>
    ///   <item><b>decisionEveryTicks</b>: cadenza ordinaria dell'orchestratore decisionale.</item>
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
        public const int DefaultDecisionEveryTicks = 25;

        // Nome legacy-compatible e transitorio: rappresenta l'hardcode produttivo
        // gia' esistente in SimulationHost, senza fissare una nomenclatura canonica
        // futura per la cognitive cadence.
        public bool enableJobDecisionOrchestrator = true;
        public int decisionEveryTicks = DefaultDecisionEveryTicks;
        public string selectionMode = "WeightedRandomTopN";
        public int topN = 3;
        public float noise01 = 0.15f;
        public float impulsivityNoiseBonus = 0.35f;
        public float minimumWeight = 0.001f;
    }

    // =============================================================================
    // BiosphereRuntimeParams
    // =============================================================================
    /// <summary>
    /// <para>
    /// DTO serializzabile dei parametri runtime della biosfera.
    /// </para>
    ///
    /// <para><b>Principio architetturale: biosfera cadenzata dal tick globale</b></para>
    /// <para>
    /// La biosfera non possiede una timeline autonoma: usa il tick discreto globale
    /// come clock di ingresso e decide soltanto quando eseguire un batch ecologico.
    /// La frequenza, i budget e il debug fast-forward restano configurabili per
    /// calibrare il peso runtime senza ricompilare codice.
    /// </para>
    ///
    /// <para><b>Struttura interna:</b></para>
    /// <list type="bullet">
    ///   <item><b>enabled</b>: gate generale del runtime biosfera.</item>
    ///   <item><b>simulationTicksPerDailyUpdate</b>: numero di tick SimulationHost tra due update biologici giornalieri.</item>
    ///   <item><b>updateMode</b>: chiave leggibile della modalita' di scheduling.</item>
    ///   <item><b>maxPlantMutationsPerUpdate</b>: alias storico del budget delta piante fisiche per batch.</item>
    ///   <item><b>maxVegetationMutationsPerUpdate</b>: alias storico del budget delta vegetazione decorativa per batch.</item>
    ///   <item><b>maxPlantUpdatesPerDay</b>: limite opzionale di piante vive evolute nel giorno.</item>
    ///   <item><b>maxPlantBirthsPerDay</b>: limite opzionale di nuove piante naturali create nel giorno.</item>
    ///   <item><b>maxPlantBirthsPerAreaPerDay</b>: limite opzionale di nuove piante naturali per area.</item>
    ///   <item><b>maxPlantDeathsPerDay</b>: limite opzionale di rimozioni di piante morte nel giorno.</item>
    ///   <item><b>maxVegetationCellsChangedPerDay</b>: limite opzionale di celle vegetazione diffusa proiettate come delta.</item>
    ///   <item><b>maxAreasProcessedPerDay</b>: limite opzionale di aree biologiche evolute nel giorno.</item>
    ///   <item><b>spreadWorkAcrossTicks</b>: consente in futuro di distribuire lavoro arretrato su piu' tick.</item>
    ///   <item><b>debugFastForwardEnabled</b>: abilita strumenti protetti di accelerazione test.</item>
    /// </list>
    /// </summary>
    [Serializable]
    public sealed class BiosphereRuntimeParams
    {
        public const int DefaultSimulationTicksPerDailyUpdate = 1200;
        public const int DefaultMaxPlantMutationsPerUpdate = 128;
        public const int DefaultMaxVegetationMutationsPerUpdate = 256;
        public const string DefaultUpdateMode = "DailyBatch";

        public bool enabled = false;
        public int simulationTicksPerDailyUpdate = DefaultSimulationTicksPerDailyUpdate;
        public string updateMode = DefaultUpdateMode;
        public int maxPlantMutationsPerUpdate = DefaultMaxPlantMutationsPerUpdate;
        public int maxVegetationMutationsPerUpdate = DefaultMaxVegetationMutationsPerUpdate;
        public int maxPlantUpdatesPerDay = 0;
        public int maxPlantBirthsPerDay = 0;
        public int maxPlantBirthsPerAreaPerDay = 0;
        public int maxPlantDeathsPerDay = 0;
        public int maxVegetationCellsChangedPerDay = 0;
        public int maxAreasProcessedPerDay = 0;
        public bool spreadWorkAcrossTicks = false;
        public bool debugFastForwardEnabled = false;

        // =============================================================================
        // ResolveSimulationTicksPerDailyUpdate
        // =============================================================================
        /// <summary>
        /// <para>
        /// Risolve la cadenza tra due update giornalieri della biosfera.
        /// </para>
        /// </summary>
        public int ResolveSimulationTicksPerDailyUpdate()
        {
            return Mathf.Max(1, simulationTicksPerDailyUpdate);
        }

        // =============================================================================
        // ResolveMaxPlantMutationsPerUpdate
        // =============================================================================
        /// <summary>
        /// <para>
        /// Risolve il budget massimo di mutazioni piante fisiche per update.
        /// </para>
        /// </summary>
        public int ResolveMaxPlantMutationsPerUpdate()
        {
            return Mathf.Max(1, maxPlantMutationsPerUpdate);
        }

        // =============================================================================
        // ResolveMaxVegetationMutationsPerUpdate
        // =============================================================================
        /// <summary>
        /// <para>
        /// Risolve il budget massimo di mutazioni vegetazione decorativa per update.
        /// </para>
        /// </summary>
        public int ResolveMaxVegetationMutationsPerUpdate()
        {
            return Mathf.Max(1, maxVegetationMutationsPerUpdate);
        }

        // =============================================================================
        // ResolveMaxPlantUpdatesPerDay
        // =============================================================================
        /// <summary>
        /// <para>
        /// Risolve quante piante esistenti possono essere evolute in un singolo
        /// giorno simulato. Zero o valori negativi indicano nessun limite specifico.
        /// </para>
        /// </summary>
        public int ResolveMaxPlantUpdatesPerDay()
        {
            return Mathf.Max(0, maxPlantUpdatesPerDay);
        }

        // =============================================================================
        // ResolveMaxPlantBirthsPerDay
        // =============================================================================
        /// <summary>
        /// <para>
        /// Risolve il limite giornaliero di nuove piante naturali. Zero o valori
        /// negativi indicano nessun limite runtime specifico.
        /// </para>
        /// </summary>
        public int ResolveMaxPlantBirthsPerDay()
        {
            return Mathf.Max(0, maxPlantBirthsPerDay);
        }

        // =============================================================================
        // ResolveMaxPlantBirthsPerAreaPerDay
        // =============================================================================
        /// <summary>
        /// <para>
        /// Risolve il limite giornaliero di nuove piante naturali per singola area.
        /// Zero o valori negativi indicano nessun limite runtime specifico.
        /// </para>
        /// </summary>
        public int ResolveMaxPlantBirthsPerAreaPerDay()
        {
            return Mathf.Max(0, maxPlantBirthsPerAreaPerDay);
        }

        // =============================================================================
        // ResolveMaxPlantDeathsPerDay
        // =============================================================================
        /// <summary>
        /// <para>
        /// Risolve quante piante morte possono essere rimosse in un giorno simulato.
        /// Zero o valori negativi indicano nessun limite specifico.
        /// </para>
        /// </summary>
        public int ResolveMaxPlantDeathsPerDay()
        {
            return Mathf.Max(0, maxPlantDeathsPerDay);
        }

        // =============================================================================
        // ResolveMaxVegetationCellsChangedPerDay
        // =============================================================================
        /// <summary>
        /// <para>
        /// Risolve quante celle di vegetazione diffusa possono essere emesse come
        /// delta in un giorno simulato.
        /// </para>
        /// </summary>
        public int ResolveMaxVegetationCellsChangedPerDay()
        {
            return maxVegetationCellsChangedPerDay > 0
                ? maxVegetationCellsChangedPerDay
                : ResolveMaxVegetationMutationsPerUpdate();
        }

        // =============================================================================
        // ResolveMaxAreasProcessedPerDay
        // =============================================================================
        /// <summary>
        /// <para>
        /// Risolve quante aree biologiche possono essere evolute in un giorno
        /// simulato. Zero o valori negativi indicano nessun limite specifico.
        /// </para>
        /// </summary>
        public int ResolveMaxAreasProcessedPerDay()
        {
            return Mathf.Max(0, maxAreasProcessedPerDay);
        }

        // =============================================================================
        // ResolveUpdateMode
        // =============================================================================
        /// <summary>
        /// <para>
        /// Restituisce la modalita' di scheduling con fallback leggibile.
        /// </para>
        /// </summary>
        public string ResolveUpdateMode()
        {
            return string.IsNullOrWhiteSpace(updateMode)
                ? DefaultUpdateMode
                : updateMode;
        }

        // =============================================================================
        // WithFallbackDefaults
        // =============================================================================
        /// <summary>
        /// <para>
        /// Crea una copia normalizzata dei parametri biosfera.
        /// </para>
        /// </summary>
        public static BiosphereRuntimeParams WithFallbackDefaults(BiosphereRuntimeParams raw)
        {
            var source = raw ?? new BiosphereRuntimeParams();

            // La copia evita che i resolver modifichino accidentalmente il DTO
            // deserializzato da Unity, che deve restare una fotografia della config.
            return new BiosphereRuntimeParams
            {
                enabled = source.enabled,
                simulationTicksPerDailyUpdate = Mathf.Max(1, source.simulationTicksPerDailyUpdate),
                updateMode = string.IsNullOrWhiteSpace(source.updateMode)
                    ? DefaultUpdateMode
                    : source.updateMode,
                maxPlantMutationsPerUpdate = Mathf.Max(1, source.maxPlantMutationsPerUpdate),
                maxVegetationMutationsPerUpdate = Mathf.Max(1, source.maxVegetationMutationsPerUpdate),
                maxPlantUpdatesPerDay = Mathf.Max(0, source.maxPlantUpdatesPerDay),
                maxPlantBirthsPerDay = Mathf.Max(0, source.maxPlantBirthsPerDay),
                maxPlantBirthsPerAreaPerDay = Mathf.Max(0, source.maxPlantBirthsPerAreaPerDay),
                maxPlantDeathsPerDay = Mathf.Max(0, source.maxPlantDeathsPerDay),
                maxVegetationCellsChangedPerDay = Mathf.Max(0, source.maxVegetationCellsChangedPerDay),
                maxAreasProcessedPerDay = Mathf.Max(0, source.maxAreasProcessedPerDay),
                spreadWorkAcrossTicks = source.spreadWorkAcrossTicks,
                debugFastForwardEnabled = source.debugFastForwardEnabled
            };
        }
    }

    // =============================================================================
    // ObjectPerceptionRuntimeParams
    // =============================================================================
    /// <summary>
    /// <para>
    /// Parametri runtime della percezione oggetti.
    /// </para>
    ///
    /// <para><b>Principio architetturale: percezione locale e budgettizzabile</b></para>
    /// <para>
    /// La percezione oggetti non deve crescere come <c>NPC x tutti gli oggetti del
    /// mondo</c>. I parametri permettono di usare l'indice spaziale a griglia del
    /// <c>World</c> e, quando serve per test o stress runtime, limitare il numero di
    /// celle candidate e oggetti processati per NPC in un singolo tick.
    /// </para>
    ///
    /// <para><b>Struttura interna:</b></para>
    /// <list type="bullet">
    ///   <item><b>maxCandidateCellsPerNpcPerTick</b>: limite celle candidate; 0 o negativo significa nessun limite.</item>
    ///   <item><b>maxObjectsPerNpcPerTick</b>: limite oggetti processati; 0 o negativo significa nessun limite.</item>
    ///   <item><b>dirtyRadiusMarginCells</b>: margine conservativo oltre il raggio visivo globale per marcare osservatori potenziali.</item>
    /// </list>
    /// </summary>
    [Serializable]
    public sealed class ObjectPerceptionRuntimeParams
    {
        public int maxCandidateCellsPerNpcPerTick = 0;
        public int maxObjectsPerNpcPerTick = 0;
        public int dirtyRadiusMarginCells = 2;
    }

    // =============================================================================
    // PerceptionStateRuntimeParams
    // =============================================================================
    /// <summary>
    /// <para>
    /// DTO serializzabile degli stati percettivi runtime.
    /// </para>
    ///
    /// <para><b>Principio architetturale: percezione cadenzata per stato</b></para>
    /// <para>
    /// La percezione non deve avere una sola frequenza globale per tutti gli NPC e
    /// tutte le situazioni. Un NPC fermo puo' osservare meno spesso; un NPC in
    /// combattimento o in azione di guardare puo' osservare a ogni tick. Questo DTO
    /// rende la scelta configurabile senza spostare logica decisionale nei sistemi
    /// percettivi.
    /// </para>
    ///
    /// <para><b>Struttura interna:</b></para>
    /// <list type="bullet">
    ///   <item><b>defaultState</b>: stato assegnato agli NPC appena creati o caricati.</item>
    ///   <item><b>maxNpcPerceptionUpdatesPerTick</b>: tetto massimo di NPC percettivi processabili in un tick.</item>
    ///   <item><b>idle</b>: profilo ordinario a basso costo.</item>
    ///   <item><b>movement</b>: profilo per NPC in spostamento.</item>
    ///   <item><b>alert</b>: profilo per attenzione aumentata.</item>
    ///   <item><b>combat</b>: profilo per stati ad alta reattivita'.</item>
    ///   <item><b>lookDirection</b>: profilo per step espliciti di osservazione direzionale.</item>
    /// </list>
    /// </summary>
    [Serializable]
    public sealed class PerceptionStateRuntimeParams
    {
        public string defaultState = "idle";
        public int maxNpcPerceptionUpdatesPerTick = 4;
        public PerceptionStateProfile idle = PerceptionStateProfile.Create(8, 12, true, 90, 0f);
        public PerceptionStateProfile movement = PerceptionStateProfile.Create(4, 14, true, 90, 0f);
        public PerceptionStateProfile alert = PerceptionStateProfile.Create(2, 17, true, 100, 0f);
        public PerceptionStateProfile combat = PerceptionStateProfile.Create(1, 17, true, 120, 0f);
        public PerceptionStateProfile lookDirection = PerceptionStateProfile.Create(1, 17, true, 90, 0f);
    }

    // =============================================================================
    // PerceptionStateProfile
    // =============================================================================
    /// <summary>
    /// <para>
    /// Profilo numerico di uno stato percettivo.
    /// </para>
    ///
    /// <para><b>Principio architetturale: default locale, fallback globale</b></para>
    /// <para>
    /// I campi con valore nullo o non positivo non diventano errori runtime:
    /// indicano che il profilo deve riusare il valore globale gia' esistente
    /// della percezione. Questo mantiene compatibilita' con i vecchi JSON e
    /// permette di introdurre gli stati senza cambiare subito i sistemi caldi.
    /// </para>
    ///
    /// <para><b>Struttura interna:</b></para>
    /// <list type="bullet">
    ///   <item><b>cadenceTicks</b>: ogni quanti tick lo stato e' candidato a percepire.</item>
    ///   <item><b>visionRangeCells</b>: raggio percettivo dello stato; 0 usa il globale.</item>
    ///   <item><b>useCone</b>: se true applica il cono visivo.</item>
    ///   <item><b>coneFovDegrees</b>: apertura del cono; 0 usa il globale.</item>
    ///   <item><b>coneSlope</b>: slope diretto del cono; 0 lo deriva dal FOV o dal globale.</item>
    /// </list>
    /// </summary>
    [Serializable]
    public sealed class PerceptionStateProfile
    {
        public int cadenceTicks = 1;
        public int visionRangeCells = 0;
        public bool useCone = true;
        public int coneFovDegrees = 0;
        public float coneSlope = 0f;

        public static PerceptionStateProfile Create(
            int cadenceTicks,
            int visionRangeCells,
            bool useCone,
            int coneFovDegrees,
            float coneSlope)
        {
            return new PerceptionStateProfile
            {
                cadenceTicks = cadenceTicks,
                visionRangeCells = visionRangeCells,
                useCone = useCone,
                coneFovDegrees = coneFovDegrees,
                coneSlope = coneSlope
            };
        }
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

    [Serializable]
    public sealed class RuntimeDiagnosticsParams
    {
        public LoggerGeneralParams general = new LoggerGeneralParams();
        public LoggerLegacyChannelParams legacy_channels = new LoggerLegacyChannelParams();
        public LoggerJsonlParams jsonl = new LoggerJsonlParams();
        public LoggerTelemetryParams telemetry = new LoggerTelemetryParams();
        public DiagnosticsDevtoolParams devtools = new DiagnosticsDevtoolParams();
        public RuntimeCostObserverParams runtime_cost_observer = new RuntimeCostObserverParams();
        public DiagnosticsMovementExplainabilityParams movement_explainability = new DiagnosticsMovementExplainabilityParams();
        public DiagnosticsMemoryBeliefDecisionExplainabilityParams memory_belief_decision_explainability =
            new DiagnosticsMemoryBeliefDecisionExplainabilityParams();
    }

    // =============================================================================
    // RuntimeCostObserverParams
    // =============================================================================
    /// <summary>
    /// <para>
    /// DTO serializzabile della configurazione dell'osservatorio costi runtime.
    /// </para>
    ///
    /// <para><b>Principio architetturale: costo nullo quando spento</b></para>
    /// <para>
    /// Questa sezione prepara la fase `v0.17` senza attivare misure implicite. Il
    /// default e' `enabled=false`; in tale stato `World.RuntimeCostObserver` resta
    /// nullo e i sistemi caldi non devono costruire alcun dato diagnostico.
    /// </para>
    ///
    /// <para><b>Struttura interna:</b></para>
    /// <list type="bullet">
    ///   <item><b>enabled</b>: abilita la creazione dell'osservatorio nel World.</item>
    ///   <item><b>sampleEveryTicks</b>: cadenza minima futura delle misure aggregate.</item>
    ///   <item><b>trackPerNpc</b>: abilita in futuro il dettaglio per NPC, piu' costoso.</item>
    ///   <item><b>writeJsonl</b>: abilita in futuro esportazione JSONL batchata.</item>
    ///   <item><b>maxTicksInMemory</b>: limite futuro del registro in memoria.</item>
    /// </list>
    /// </summary>
    [Serializable]
    public sealed class RuntimeCostObserverParams
    {
        public bool enabled = false;
        public int sampleEveryTicks = 1;
        public bool trackPerNpc = false;
        public bool writeJsonl = false;
        public int maxTicksInMemory = 256;
        public int jsonlFlushIntervalTicks = 25;
        public int jsonlMaxQueueSize = 4096;
        public int jsonlMaxBatchSize = 512;
        public string jsonLogFileNamePattern = "arcontio_runtime_cost_{yyyyMMdd_HHmmss}.jsonl";
    }

    [Serializable]
    public sealed class DiagnosticsDevtoolParams
    {
        public bool overlay_enabled = false;
        public bool verbose_debug_enabled = false;
        public DebugFovParams debug_fov = new DebugFovParams();

        public LoggerDevtoolParams ToLoggerDevtoolParams()
        {
            return new LoggerDevtoolParams
            {
                overlay_enabled = overlay_enabled,
                verbose_debug_enabled = verbose_debug_enabled
            };
        }
    }

    [Serializable]
    public sealed class DiagnosticsMovementExplainabilityParams
    {
        public bool enabled = false;
        public bool runtime_registry_enabled = true;
        public bool file_logging_enabled = false;
        public int defaultVerbosity = 0;
        public int maxTrackedNpcs = 3;
        public int[] trackedNpcIds = Array.Empty<int>();
        public bool trackActiveNpcOnly = true;
        public int ringBuffer_intent = 10;
        public int ringBuffer_plan = 10;
        public int ringBuffer_events_low = 60;
        public int ringBuffer_events_high = 200;
        public int verbosityHighThreshold = 3;
        public string jsonLogFileNamePattern = "arcontio_el_pathfinding_{yyyyMMdd_HHmmss}.jsonl";

        public MovementExplainabilityParams ToMovementExplainabilityParams(MovementExplainabilityParams fallback)
        {
            var current = fallback ?? new MovementExplainabilityParams();
            current.enabled = enabled && runtime_registry_enabled;
            current.defaultVerbosity = defaultVerbosity;
            current.maxTrackedNpcs = maxTrackedNpcs;
            current.trackedNpcIds = trackedNpcIds ?? Array.Empty<int>();
            current.trackActiveNpcOnly = trackActiveNpcOnly;
            current.ringBuffer_intent = ringBuffer_intent;
            current.ringBuffer_plan = ringBuffer_plan;
            current.ringBuffer_events_low = ringBuffer_events_low;
            current.ringBuffer_events_high = ringBuffer_events_high;
            current.verbosityHighThreshold = verbosityHighThreshold;
            current.writeJsonLog = enabled && file_logging_enabled;
            current.jsonLogFileNamePattern = string.IsNullOrWhiteSpace(jsonLogFileNamePattern)
                ? current.jsonLogFileNamePattern
                : jsonLogFileNamePattern;
            return current;
        }
    }

    [Serializable]
    public sealed class DiagnosticsMemoryBeliefDecisionExplainabilityParams
    {
        public bool enabled = false;
        public bool runtime_registry_enabled = true;
        public bool file_logging_enabled = false;
        public int defaultVerbosity = 0;
        public int maxTrackedNpcs = 5;
        public int[] trackedNpcIds = Array.Empty<int>();
        public bool trackActiveNpcOnly = false;
        public string jsonLogFileNamePattern = "arcontio_el_mbd_{yyyyMMdd_HHmmss}.jsonl";
        public bool logMemory = true;
        public bool logBelief = true;
        public bool logQuery = true;
        public bool logDecision = true;
        public bool logBridge = true;
        public bool logJobRequest = true;
        public bool logJobLifecycle = true;
        public bool logJobPhase = true;
        public bool logStep = true;
        public bool logJobState = true;
        public bool logJobArbitration = true;
        public bool logReservation = true;
        public bool logCommand = true;
        public bool logFailureLearning = true;
        public bool logRunningAction = true;
        public bool includeCandidates = true;
        public bool includeScoreBreakdown = true;
        public bool includeRejectedCandidates = false;
        public int ringBuffer_memory = 80;
        public int ringBuffer_belief = 80;
        public int ringBuffer_query = 40;
        public int ringBuffer_decision = 24;
        public int ringBuffer_bridge = 24;
        public int ringBuffer_jobRequest = 24;
        public int ringBuffer_jobLifecycle = 32;
        public int ringBuffer_jobPhase = 40;
        public int ringBuffer_step = 64;
        public int ringBuffer_jobState = 40;
        public int ringBuffer_jobArbitration = 32;
        public int ringBuffer_reservation = 40;
        public int ringBuffer_command = 48;
        public int ringBuffer_failureLearning = 32;
        public int ringBuffer_runningAction = 48;

        public MemoryBeliefDecisionExplainabilityParams ToMemoryBeliefDecisionExplainabilityParams(
            MemoryBeliefDecisionExplainabilityParams fallback)
        {
            var current = fallback ?? new MemoryBeliefDecisionExplainabilityParams();
            current.enabled = enabled && runtime_registry_enabled;
            current.defaultVerbosity = defaultVerbosity;
            current.maxTrackedNpcs = maxTrackedNpcs;
            current.trackedNpcIds = trackedNpcIds ?? Array.Empty<int>();
            current.trackActiveNpcOnly = trackActiveNpcOnly;
            current.writeJsonLog = enabled && file_logging_enabled;
            current.jsonLogFileNamePattern = string.IsNullOrWhiteSpace(jsonLogFileNamePattern)
                ? current.jsonLogFileNamePattern
                : jsonLogFileNamePattern;
            current.logMemory = logMemory;
            current.logBelief = logBelief;
            current.logQuery = logQuery;
            current.logDecision = logDecision;
            current.logBridge = logBridge;
            current.logJobRequest = logJobRequest;
            current.logJobLifecycle = logJobLifecycle;
            current.logJobPhase = logJobPhase;
            current.logStep = logStep;
            current.logJobState = logJobState;
            current.logJobArbitration = logJobArbitration;
            current.logReservation = logReservation;
            current.logCommand = logCommand;
            current.logFailureLearning = logFailureLearning;
            current.logRunningAction = logRunningAction;
            current.includeCandidates = includeCandidates;
            current.includeScoreBreakdown = includeScoreBreakdown;
            current.includeRejectedCandidates = includeRejectedCandidates;
            current.ringBuffer_memory = ringBuffer_memory;
            current.ringBuffer_belief = ringBuffer_belief;
            current.ringBuffer_query = ringBuffer_query;
            current.ringBuffer_decision = ringBuffer_decision;
            current.ringBuffer_bridge = ringBuffer_bridge;
            current.ringBuffer_jobRequest = ringBuffer_jobRequest;
            current.ringBuffer_jobLifecycle = ringBuffer_jobLifecycle;
            current.ringBuffer_jobPhase = ringBuffer_jobPhase;
            current.ringBuffer_step = ringBuffer_step;
            current.ringBuffer_jobState = ringBuffer_jobState;
            current.ringBuffer_jobArbitration = ringBuffer_jobArbitration;
            current.ringBuffer_reservation = ringBuffer_reservation;
            current.ringBuffer_command = ringBuffer_command;
            current.ringBuffer_failureLearning = ringBuffer_failureLearning;
            current.ringBuffer_runningAction = ringBuffer_runningAction;
            return current;
        }
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

        // ── JOB RUNNING ACTION TRAVERSAL FOUNDATION (v0.11c.02g) ─────────────
        //
        // Campo mantenuto solo per compatibilita' con JSON precedenti. Il gate
        // canonico vive ora in SimulationParams.tick, perche' abilita una semantica
        // temporale del Job runtime e non una proprieta' strutturale del MovementSystem.
        // I consumer non devono leggere direttamente questo campo.
        public bool enableJobRunningActionTraversal = false;

        // Campo legacy mantenuto solo come fallback per vecchi JSON. La durata
        // canonica vive in SimulationParams.tick.baseWalkCellDurationTicks, cosi'
        // il tempo runtime resta centralizzato nel gruppo Tick.
        public int baseWalkCellDurationTicks = 0;

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
        public bool logJobRequest = true;
        public bool logJobLifecycle = true;
        public bool logJobPhase = true;
        public bool logStep = true;
        public bool logJobState = true;
        public bool logJobArbitration = true;
        public bool logReservation = true;
        public bool logCommand = true;
        public bool logFailureLearning = true;
        public bool logRunningAction = true;

        public bool includeCandidates = true;
        public bool includeScoreBreakdown = true;
        public bool includeRejectedCandidates = false;

        public int ringBuffer_memory = 80;
        public int ringBuffer_belief = 80;
        public int ringBuffer_query = 40;
        public int ringBuffer_decision = 24;
        public int ringBuffer_bridge = 24;
        public int ringBuffer_jobRequest = 24;
        public int ringBuffer_jobLifecycle = 32;
        public int ringBuffer_jobPhase = 32;
        public int ringBuffer_step = 64;
        public int ringBuffer_jobState = 24;
        public int ringBuffer_jobArbitration = 24;
        public int ringBuffer_reservation = 32;
        public int ringBuffer_command = 32;
        public int ringBuffer_failureLearning = 24;
        public int ringBuffer_runningAction = 64;
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

        // Nota v0.20m:
        // La cadenza dei landmark non vive piu' qui. I landmark usano la stessa
        // selezione percettiva centrale di oggetti e NPC, governata dagli stati
        // configurati in perception_states.

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
            return LoadFromTextAsset(ta, resourcesPathNoExt);
        }

        public static SimulationParams LoadFromTextAsset(TextAsset textAsset, string resourcesPathNoExt)
        {
            if (textAsset == null)
            {
                Debug.LogWarning($"[Arcontio] Missing sim params at Resources/{resourcesPathNoExt}.json. Using defaults.");
                return new SimulationParams();
            }

            return LoadFromJson(textAsset.text);
        }

        public static SimulationParams LoadFromJson(string json)
        {
            if (string.IsNullOrWhiteSpace(json))
                return new SimulationParams();

            try
            {
                var parameters = JsonUtility.FromJson<SimulationParams>(json) ?? new SimulationParams();
                parameters.ApplyRuntimeDiagnosticsLayout();
                return parameters;
            }
            catch (Exception ex)
            {
                Debug.LogError($"[Arcontio] Failed parsing sim params: {ex}");
                return new SimulationParams();
            }
        }
    }
}
