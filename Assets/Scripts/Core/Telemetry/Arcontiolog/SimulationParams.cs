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
