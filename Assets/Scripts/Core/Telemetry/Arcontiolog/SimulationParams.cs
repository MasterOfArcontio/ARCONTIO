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

        // Candidate detection
        public LandmarkCandidateParams candidate = new LandmarkCandidateParams();

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

    [Serializable]
    public sealed class LandmarkCandidateParams
    {
        public int candidate_cooldown_ticks = 6;
        public int junction_min_exits = 3;
    }

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
        public bool enabled = false;
        public bool activeNpcOnly = true;
        public bool microTestDummyGraph = false;
        public int microTestDummyDistanceCells = 2;
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
