using Arcontio.Core.Config;
using Arcontio.Core.DevTools;
using Arcontio.Core.Logging;
using System;
using System.Collections.Generic;
using Unity.VisualScripting.FullSerializer;
using UnityEngine;
using UnityEngine.LightTransport;
using static UnityEditor.PlayerSettings;

namespace Arcontio.Core
{
    /// <summary>
    /// World:
    /// Contiene stato globale + component store.
    ///
    /// Day10 (patch):
    /// - Gli "occluder" NON sono piÃ¹ una struttura separata: sono oggetti del mondo (WorldObjectInstance)
    /// - Manteniamo una OcclusionMap interna (griglia) come CACHE derivata da World.Objects
    ///   per query veloci: BlocksVision/BlocksMovement.
    ///
    /// Nota:
    /// - Restiamo coerenti col Core Standard: 1 object per cell.
    /// - La cache viene aggiornata quando crei/distruggi oggetti o quando SetOccluder (wrapper) modifica celle.
    /// </summary>
    public sealed class World
    {
        // ============================================================
        // GLOBAL / CONFIG
        // ============================================================

        public GlobalState Global;

        // Dimensione griglia simulatore (spostabile in game_params.json)
        public int MapWidth { get; private set; }
        public int MapHeight { get; private set; }

        // ============================================================
        // DEBUG / TELEMETRY (view-only diagnostics)
        // ============================================================

        /// <summary>
        /// Telemetria FOV (debug): heatmap per NPC accumulata in finestre di N tick.
        ///
        /// Per design:
        /// - se null: feature disabilitata da game_params.json.
        /// - se presente: viene aggiornata dai system che calcolano percezione
        ///   e avanzata 1 volta per tick dal SimulationHost.
        ///
        /// IMPORTANTISSIMO:
        /// - Questa NON Ã¨ logica di simulazione.
        /// - Ã solo un canale di osservabilitÃ  per la view/debug.
        /// </summary>
        public DebugFovTelemetry DebugFovTelemetry { get; private set; }

        /// <summary>
        /// LandmarkRegistry (v0.02 Day2): registro oggettivo dei landmark.
        ///
        /// Nota:
        /// - Ã world-side (non per-NPC).
        /// - Viene costruito in bootstrap (SimulationHost) dopo il seeding della mappa.
        /// - La view puÃ² leggerlo in modo read-only tramite GetNpcLandmarkOverlayData.
        /// </summary>
        public LandmarkRegistry LandmarkRegistry { get; private set; }

        /// <summary>
        /// Debug token logs (Patch 0.01P2):
        /// cache per rendere osservabili le comunicazioni simboliche (TokenEnvelope).
        ///
        /// PerchÃ© Ã¨ qui e non nella View:
        /// - Il TokenDelivery/Assimilation Ã¨ core.
        /// - La view deve restare read-only sul World.
        /// - Senza un cache persistente, i token "spariscono" dopo il tick (TokenBus Ã¨ una coda).
        ///
        /// IMPORTANTISSIMO:
        /// - Questo NON influisce su decisioni NPC.
        /// - Ã diagnostica: puÃ² essere disabilitata/ignorata senza cambiare gameplay.
        /// </summary>
        public readonly Dictionary<int, DebugNpcTokenLog> DebugNpcTokens = new();

        // ============================================================
        // LOCAL SEARCH FAILURE LEARNING (anti-loop avanzato)
        // ============================================================
        // Queste strutture sono volutamente molto piccole e locali.
        // NON rappresentano conoscenza globale del mondo: servono solo a ricordare
        // che una certa micro-ricerca locale, in quel contesto, ha gia' fallito
        // poco fa, cosi' l'NPC non ripete immediatamente la stessa micro-scelta.
        [Serializable]
        public sealed class LocalSearchFailureRecord
        {
            public int FailureCount;
            public int LastFailedTick = -1;
            public int BlockedFirstStepCellIndex = -1;
            public int LastProgressCellIndex = -1;
        }

        /// <summary>
        /// TokenOutQueue (Patch 0.01P3 extension): coda "deferred" di TokenEnvelope OUT.
        ///
        /// Motivazione architetturale:
        /// - Alcuni System (es. MemoryEncodingSystem) NON ricevono TokenBus in input.
        /// - Tuttavia, in alcuni casi vogliamo generare comunicazioni *event-driven* (es. report di furto)
        ///   nello stesso tick in cui viene percepito un evento.
        /// - Per non violare le dipendenze (System -> TokenBus) aggiungiamo una coda nel World.
        ///
        /// Flusso:
        /// - i System chiamano world.QueueTokenOut(...)
        /// - SimulationHost, in un punto noto e centrale, chiama world.FlushQueuedTokenOut(_tokenBusOut)
        /// - il flush usa PublishTokenOut(...) quindi mantiene osservabilitÃ  + balloon.
        ///
        /// IMPORTANTISSIMO:
        /// - Ã una coda *runtime* (non persistenza), vale solo per il tick corrente.
        /// - Non Ã¨ logica di gameplay autonoma: Ã¨ un meccanismo di collegamento tra stage.
        /// </summary>
        private readonly List<TokenEnvelope> _queuedTokenOut = new(256);

        /// <summary>
        /// Helper difensivo: ritorna (o crea) il log di un NPC.
        ///
        /// Nota: Non facciamo pruning qui: il log Ã¨ bounded internamente.
        /// </summary>
        public DebugNpcTokenLog GetOrCreateDebugNpcTokenLog(int npcId)
        {
            if (!DebugNpcTokens.TryGetValue(npcId, out var log) || log == null)
            {
                log = new DebugNpcTokenLog();
                DebugNpcTokens[npcId] = log;
            }
            return log;
        }

        /// <summary>
        /// PublishTokenOut (Patch 0.01P3):
        /// Punto *canonico* dove un TokenEnvelope viene considerato "detto" (OUT).
        ///
        /// Motivazione architetturale:
        /// - In 0.01 ci sono piÃ¹ posti che possono pubblicare su TokenBusOut:
        ///   - TokenEmissionPipeline (regole di emission)
        ///   - SimulationHost (iniezioni di scenario/debug come T7)
        /// - Se logghiamo OUT solo dentro TokenEmissionPipeline, perdiamo tutto ciÃ² che nasce altrove.
        ///
        /// Quindi:
        /// - Chiunque voglia pubblicare un token OUT deve passare da qui.
        /// - Qui facciamo SOLO side-effect di debug/UX:
        ///   1) aggiorniamo DebugNpcTokenLog (per card UI)
        ///   2) emettiamo un NpcBalloonSignal (per fumetto sopra la testa)
        ///   3) pubblichiamo sul TokenBusOut (flusso reale)
        ///
        /// Nota:
        /// - Questo non modifica l'AI: Ã¨ osservabilitÃ .
        /// </summary>
        public void PublishTokenOut(TokenBus tokenBusOut, TokenEnvelope env)
        {
            // Difensivo: niente crash se qualcuno chiama con bus null.
            if (tokenBusOut == null) return;

            // 1) Log diagnostico (OUT)
            GetOrCreateDebugNpcTokenLog(env.SpeakerId).RecordOutgoing(env);

            // 2) Balloon (OUT)
            // SubjectId = listenerId: Ã¨ l'informazione piÃ¹ utile in debug ("sto parlando a chi?").
            // Nota Patch 0.01P3 extension:
            // - Alcune comunicazioni hanno balloon dedicati (furto report vittima/testimone).
            // - Qui selezioniamo il kind in modo deterministico dal TokenType.
            var outKind = GetOutgoingBalloonKindForTokenType(env.Token.Type);
            EmitNpcBalloon(env.SpeakerId, outKind, subjectId: env.ListenerId);

            // 3) Effettiva pubblicazione
            tokenBusOut.Publish(env);
        }

        /// <summary>
        /// QueueTokenOut (Patch 0.01P3 extension):
        /// Accoda un envelope OUT per essere pubblicato piÃ¹ tardi dal SimulationHost.
        ///
        /// Nota:
        /// - Non facciamo Publish qui perchÃ© i System che chiamano questa funzione non hanno
        ///   accesso al TokenBusOut.
        /// - Non facciamo nemmeno balloon/log qui: verranno applicati nel flush tramite PublishTokenOut.
        /// </summary>
        public void QueueTokenOut(TokenEnvelope env)
        {
            _queuedTokenOut.Add(env);
        }

        /// <summary>
        /// FlushQueuedTokenOut (Patch 0.01P3 extension):
        /// Pubblica tutti gli envelope accodati su TokenBusOut passando dal punto canonico PublishTokenOut.
        ///
        /// Ritorna:
        /// - quanti envelope sono stati flushati.
        ///
        /// Nota:
        /// - Il flush Ã¨ idempotente per tick: svuota la lista.
        /// - Chiamare flush piÃ¹ volte nello stesso tick Ã¨ lecito (es. pre e post-command encoding).
        /// </summary>
        public int FlushQueuedTokenOut(TokenBus tokenBusOut)
        {
            if (_queuedTokenOut.Count == 0)
                return 0;

            int count = _queuedTokenOut.Count;

            for (int i = 0; i < _queuedTokenOut.Count; i++)
                PublishTokenOut(tokenBusOut, _queuedTokenOut[i]);

            _queuedTokenOut.Clear();
            return count;
        }

        /// <summary>
        /// Seleziona il balloon OUT in funzione del tipo di token.
        ///
        /// Design choice:
        /// - Per quasi tutti i token: TokenOut generico.
        /// - Per TheftReport*: balloon dedicati, perchÃ© sono un segnale sociale rilevante (crimine).
        /// </summary>
        private static NpcBalloonKind GetOutgoingBalloonKindForTokenType(TokenType type)
        {
            switch (type)
            {
                case TokenType.TheftReportVictim: return NpcBalloonKind.TheftReportVictimOut;
                case TokenType.TheftReportWitness: return NpcBalloonKind.TheftReportWitnessOut;
                default: return NpcBalloonKind.TokenOut;
            }
        }

        private OcclusionCell[] _occlusion; // size = MapWidth*MapHeight
        private struct OcclusionCell
        {
            public int OccluderObjectId; // 0 = none
            public bool BlocksVision;
            public bool BlocksMovement;
            public float VisionCost;
        }

        public WorldConfig Config { get; }

        /// <summary>
        /// Inizializza (o reinizializza) la mappa del simulatore.
        /// Attenzione: se lo chiami dopo aver creato oggetti, perderai gli indici.
        /// In pratica va chiamato in bootstrap (SimulationHost) prima del seeding.
        /// </summary>
        public void InitMap(int width, int height)
        {
            if (width <= 0) width = 1;
            if (height <= 0) height = 1;

            MapWidth = width;
            MapHeight = height;

            int size = MapWidth * MapHeight;

            _occlusion = new OcclusionCell[size];
            _objIdByCell = new int[size];
            _blocksVision = new bool[size];
            _blocksMovement = new bool[size];

            // Pulizia cache: default Ã¨ ok per OcclusionCell/bool,
            // ma _objIdByCell deve essere inizializzato a -1 (empty).
            for (int i = 0; i < size; i++)
            {
                _occlusion[i] = default;
                _blocksVision[i] = false;
                _blocksMovement[i] = false;
                _objIdByCell[i] = -1;
            }
        }

        // ============================================================
        // DATA-DRIVEN DEFINITIONS
        // ============================================================

        // Definizioni logiche degli oggetti (caricate da ObjectDatabaseLoader)
        public readonly Dictionary<string, ObjectDef> ObjectDefs = new();

        // ============================================================
        // COMPONENT STORES (NPC)
        // ============================================================

        public readonly Dictionary<int, NpcCore> NpcCore = new();
        public readonly Dictionary<int, Needs> Needs = new();
        public readonly Dictionary<int, Social> Social = new();
        public readonly Dictionary<int, GridPosition> GridPos = new();
        public readonly Dictionary<int, CardinalDirection> NpcFacing = new();

        // ============================================================
        // ACTIVITY / ACTION (NPC)
        // ============================================================

        /// <summary>
        /// Stato di azione "corrente" per NPC (view/debug friendliness).
        /// 
        /// NOTE ARCHITETTURALI (ARCONTIO):
        /// - Non Ã¨ una "AI brain" separata: Ã¨ un piccolo stato descrittivo che rende osservabile
        ///   cosa l'NPC sta facendo in questo momento.
        /// - Viene aggiornato dai Command (intenti) e/o dai System (esecuzione fisica).
        /// - La View puÃ² leggerlo senza dover inferire azioni da segnali indiretti (movimento, fame, ecc.).
        /// </summary>
        public readonly Dictionary<int, NpcActionState> NpcAction = new();

        /// <summary>
        /// NpcBalloonSignals:
        /// Ultimo segnale "visuale" (balloon) per ciascun NPC.
        ///
        /// Nota:
        /// - Ã uno store di osservabilitÃ , come NpcAction.
        /// - Viene scritto dal core quando accadono fatti rilevanti.
        /// - La view lo legge e decide come renderizzare (sprite, durata, layering).
        /// </summary>
        public readonly Dictionary<int, NpcBalloonSignal> NpcBalloonSignals = new();

        // Movimento come "intento" eseguito da un System.
        // Le Rule scrivono qui; il MovementSystem consuma e prova ad avanzare.
        //        public readonly Dictionary<int, MoveIntent> MoveIntents = new();

        // Memoria (per-NPC)
        public readonly Dictionary<int, MemoryStore> Memory = new();
        public readonly Dictionary<int, PersonalityMemoryParams> MemoryParams = new();

        // 1 store per NPC
        public readonly Dictionary<int, NpcObjectMemoryStore> NpcObjectMemory =
            new Dictionary<int, NpcObjectMemoryStore>(2048);

        

        // ============================================================
        // LANDMARK MEMORY (NPC-side, v0.02 Day3)
        // ============================================================
        //
        // Questo eè IL punto in cui ARCONTIO separa:
        // - "verità oggettiva" (LandmarkRegistry, World-side, derivato dalla mappa)
        // - "conoscenza soggettiva" (NpcLandmarkMemory, per-NPC, imparata tramite esperienza)
        //
        // Day3 introduce:
        // - subset per NPC (non conosce tutto)
        // - cap per NPC + eviction (stale + over-cap)
        // - apprendimento event-driven (agganciato al movimento)
        //
        // Nota implementativa:
        // - teniamo lo store nel World perché:
        //   1) eè un componente per-NPC (come Memory, Needs, Social, ecc.)
        //   2) la view deve poter leggere contatori in modo safe via TryGetNpcLandmarkDebugReport
        // - la logica di aggiornamento avviene tramite:
        //   - MovementSystem -> NotifyNpcMovedForLandmarkLearning(...)
        //   - NpcLandmarkMemorySystem -> eviction/cap (periodico)
        //
        public readonly Dictionary<int, NpcLandmarkMemory> NpcLandmarkMemory = new Dictionary<int, NpcLandmarkMemory>(2048);

        // ============================================================
        // DEBUG CLICK-MOVE / MACRO ROUTE OVERLAY
        // ============================================================
        //
        // Questa struttura salva, per ogni NPC, l'ultima macro-route di debug calcolata
        // quando lo sviluppatore ordina "vai in questa cella cliccata".
        //
        // Perche' la teniamo nel World:
        // - la command view->core deve poter aggiornare sia MoveIntent sia route di debug;
        // - la view deve poi leggere il risultato in modo read-only attraverso
        //   GetNpcLandmarkOverlayData, senza rifare planning lato view.
        public readonly Dictionary<int, NpcMacroRoutePlan> NpcMacroRoutes = new Dictionary<int, NpcMacroRoutePlan>(256);
        public readonly Dictionary<int, NpcMacroRouteExecutionState> NpcMacroRouteExecution = new Dictionary<int, NpcMacroRouteExecutionState>(256);

        // ============================================================
        // DEBUG NAVIGATION PATHS (cella-per-cella)
        // ============================================================
        // Questi store esistono solo per osservabilita' runtime.
        // Non sono la fonte di verita' del pathfinding: servono a disegnare l'overlay
        // distinguendo chiaramente i tratti prodotti da:
        // - LM_PATH
        // - DIRECT_COMMIT
        // - GOAL_LOCAL_SEARCH / JPS
        public readonly Dictionary<int, List<GridPosition>> DebugLmPathCells = new Dictionary<int, List<GridPosition>>(256);
        public readonly Dictionary<int, List<GridPosition>> DebugDirectPathCells = new Dictionary<int, List<GridPosition>>(256);
        public readonly Dictionary<int, List<GridPosition>> DebugJumpPathCells = new Dictionary<int, List<GridPosition>>(256);

        // Stato esecutivo opzionale per direct/local search. Per la patch corrente viene usato
        // soprattutto per debug card, overlay e controllo del lifecycle della ricerca locale.
        public readonly Dictionary<int, NpcDirectCommitExecutionState> NpcDirectCommitExecution = new Dictionary<int, NpcDirectCommitExecutionState>(256);
        public readonly Dictionary<int, NpcGoalLocalSearchExecutionState> NpcGoalLocalSearchExecution = new Dictionary<int, NpcGoalLocalSearchExecutionState>(256);

        // npcId -> (signature locale -> record di fallimento recente)
        public readonly Dictionary<int, Dictionary<long, LocalSearchFailureRecord>> NpcLocalSearchFailureLearning = new Dictionary<int, Dictionary<long, LocalSearchFailureRecord>>(256);
// ============================================================
        // COMPONENT STORES (OBJECTS)
        // ============================================================

        // Oggetti nel mondo
        public readonly Dictionary<int, WorldObjectInstance> Objects = new();

        // Use state (letto occupato, ecc.)
        public readonly Dictionary<int, ObjectUseState> ObjectUse = new();

        // Food stock ?in-world? (pile/stockpile)
        public readonly Dictionary<int, FoodStockComponent> FoodStocks = new();
        // ============================================================
        // OWNERSHIP / POSSESSION (Pinned *belief*, NOT telepathy)
        // ============================================================
        //
        // Patch 5.1 (Ownership Pinned Memory - revised):
        //
        // IMPORTANTISSIMO:
        // In ARCONTIO distinguiamo SEMPRE tra:
        //  - VeritÃ  oggettiva del mondo (World/Facts)
        //  - Conoscenza soggettiva di un NPC (Perception + Memory)
        //
        // La "proprietÃ " (OwnerKind/OwnerId dentro FoodStockComponent) Ã¨ un FATTO oggettivo.
        // Ma la "consapevolezza" che l'NPC ha di quel possesso NON deve essere telepatica:
        // se qualcuno ruba/distrugge uno stock fuori dalla vista dell'NPC,
        // l'NPC non deve "saperlo" finchÃ© non torna sul posto o riceve informazione mediata.
        //
        // Per risolvere il bug pratico ("ignora il mio cibo a terra se non Ã¨ in memoria percettiva")
        // senza violare il manifesto, introduciamo quindi un ledger PINNED di tipo belief:
        //
        //  - Ã¨ scritto quando lo stock viene creato/posato con OwnerId (cioÃ¨ l'NPC *sa* di averlo)
        //  - NON viene cancellato automaticamente quando lo stock viene rubato/distrutto lontano
        //  - viene invalidato solo quando l'NPC ispeziona (arriva sulla cella attesa e non trova lo stock)
        //
        // Nota:
        // - Manteniamo solo la "last known cell" perchÃ© Ã¨ sufficiente per pianificare:
        //   l'NPC puÃ² decidere di tornare lÃ¬ a verificare.
        // - In futuro potremmo arricchire questa belief con timestamp, confidence, ecc.
        //
        public struct PinnedFoodStockBelief
        {
            // L'objectId dello stock che l'NPC crede di possedere.
            public int ObjectId;

            // Ultima cella nota dove l'NPC crede di aver lasciato lo stock.
            public int LastKnownX;
            public int LastKnownY;

            public bool IsValid => ObjectId != 0;
        }

        // npcId -> lista di stock privati "pinnati" come BELIEF.
        // NON Ã¨ memoria percettiva, e non Ã¨ "veritÃ  del mondo": Ã¨ il promemoria interno "quel cibo Ã¨ mio e sta lÃ¬".
        public readonly Dictionary<int, List<PinnedFoodStockBelief>> NpcPinnedFoodStockBeliefs = new();


        // Cibo privato per NPC (inventario v0)
        public readonly Dictionary<int, int> NpcPrivateFood = new();

        // Marker per distinguere "ho mangiato io" vs "mi manca cibo"
        // npcId -> ultimo tick in cui ha consumato cibo privato
        public readonly Dictionary<int, long> NpcLastPrivateFoodConsumeTick = new();

        // (Day10) Occluder component store per oggetti ?muro/porta?
        // Nota: un muro Ã¨ un oggetto, e qui mettiamo i suoi flags runtime (vision/movement + cost).
        public readonly Dictionary<int, Occluder> ObjectOccluders = new();

        // ============================================================
        // MOVEMENT / SCAN (intent + execution state)
        // ============================================================

        /// <summary>
        /// Intento di movimento per NPC.
        /// - Scritto dalla decision pipeline (Rules/Decision).
        /// - Consumato dal MovementSystem (fisico).
        /// </summary>
        public readonly Dictionary<int, MoveIntent> NpcMoveIntents = new();

        /// <summary>
        /// Stato di scan direzionale per NPC.
        /// - "Nessuna visione a 360Â° gratuita": scan = 4 turn consecutivi.
        /// - Consumato da IdleScanSystem.
        /// </summary>
        public readonly Dictionary<int, ScanState> NpcScanStates = new();


        // ============================================================
        // INTERNAL GRID INDEXES / CACHES
        // ============================================================

        // 1 object per cell -> indice rapido cella->objId
        private int[] _objIdByCell; // length = MapWidth*MapHeight, -1 = empty

        // Cache occlusione: derivata dagli oggetti (ObjectOccluders o def flags)
        private bool[] _blocksVision;    // length = MapWidth*MapHeight
        private bool[] _blocksMovement;  // length = MapWidth*MapHeight

        // ============================================================
        // IDS
        // ============================================================

        private int _nextNpcId = 1;
        private int _nextObjectId = 1;

        // ============================================================
        // CTOR / INIT
        // ============================================================

        public World(WorldConfig config)
        {
            Config = config;
            InitMap(Config.Sim.worldWidth, Config.Sim.worldHeight);

            // ============================================================
            // LANDMARK REGISTRY (v0.02 Day2)
            // ============================================================
            // Nota:
            // - Qui istanziamo SOLO il contenitore.
            // - La build vera e propria avviene dopo il seeding (SimulationHost), quando
            //   oggetti come muri/porte sono stati piazzati sulla griglia.
            LandmarkRegistry = new LandmarkRegistry();

            // ============================================================
            // DEBUG FOV TELEMETRY (config-driven)
            // ============================================================
            // Nota:
            // - questa Ã¨ una feature SOLO debug.
            // - la view la usa per disegnare overlay "heat" delle celle viste.
            // - viene attivata da game_params.json: debug_fov.enabled.
            //
            // Implementazione:
            // - accumulo per finestra di N tick
            // - double buffer read/write
            if (Config?.Sim != null && Config.Sim.debug_fov != null && Config.Sim.debug_fov.enabled)
            {
                int window = Config.Sim.debug_fov.window_ticks;
                if (window <= 0) window = 1;

                DebugFovTelemetry = new DebugFovTelemetry(MapWidth, MapHeight, window);
            }

            Global.EnableMemorySpatialFusion = false;
            Global.MemoryRegionSizeCells = 4;

            Global.MaxTokensPerEncounter = 2;
            Global.MaxTokensPerNpcPerDay = 50;
            Global.RepeatShareCooldownTicks = 0;

            Global.TokenDeliveryMaxRangeCells = 10;
            Global.EnableTokenLOS = true;
            Global.TokenReliabilityFalloffPerCell = 0.06f;
            Global.TokenIntensityFalloffPerCell = 0.04f;

            // ============================================================
            // Perception params (data-driven via game_params.json)
            // ============================================================
            // Prima erano hardcoded. Ora li leggiamo da SimulationParams.
            // Se valori non sono presenti o sono invalidi, facciamo fallback safe.
            int vr = Config?.Sim != null ? Config.Sim.npcVisionRangeCells : 6;
            if (vr <= 0) vr = 6;
            Global.NpcVisionRangeCells = vr;
            
            int ar = Config?.Sim != null ? Config.Sim.npcOperationalRangeCells : 8;
            if (ar <= 0) ar = 8;
            Global.NpcOperationalRangeCells = ar;

            bool useCone = Config?.Sim != null ? Config.Sim.npcVisionUseCone : true;
            Global.NpcVisionUseCone = useCone;

            // Cone slope:
            // - se npcVisionConeSlope > 0 => usalo.
            // - altrimenti, se npcVisionFovDegrees > 0 => calcola slope = tan(fov/2).
            float slope = Config?.Sim != null ? Config.Sim.npcVisionConeSlope : 1.0f;
            int fovDeg = Config?.Sim != null ? Config.Sim.npcVisionFovDegrees : 90;

            if (slope <= 0f && fovDeg > 0)
            {
                // half-angle in radianti
                // Esempio: FOV=90Â° => half=45Â° => tan(45)=1.
                float halfRad = (fovDeg * 0.5f) * Mathf.Deg2Rad;
                slope = Mathf.Tan(halfRad);
            }

            if (slope <= 0f) slope = 1.0f;

            // Legacy/back-compat: manteniamo anche HalfWidthPerStep per codice vecchio.
            Global.NpcVisionConeSlope = slope;
            Global.NpcVisionConeHalfWidthPerStep = slope;

            Global.Needs = NeedsConfig.Default();

            // ============================================================
            // Inventory params (data-driven via game_params.json)
            // ============================================================
            // Capienza inventario globale.
            // NOTA: oggi l'inventario Ã¨ rappresentato principalmente da NpcPrivateFood (cibo trasportato),
            // ma la query World.GetInventoryFreeCapacity Ã¨ pensata per diventare la fonte unica
            // anche quando introdurremo altri oggetti trasportabili.
            int invMax = Config?.Sim != null && Config.Sim.inventory != null ? Config.Sim.inventory.inventory_max_units : 3;
            if (invMax < 0) invMax = 0; // "0" Ã¨ valido: significa che non puÃ² trasportare nulla.
            Global.InventoryMaxUnits = invMax;


            // ============================================================
            // Landmark pathfinding params (data-driven via game_params.json)
            // ============================================================
            // (v0.02 Day1)
            // Nota: qui NON implementiamo ancora il pathfinding a landmark.
            // Formalizziamo soltanto i parametri e li rendiamo disponibili nel GlobalState,
            // cosÃ¬ la view puÃ² fare overlay/report e i Day successivi possono usare gli stessi valori.
            var lm = Config?.Sim != null ? Config.Sim.landmarks : null;
            if (lm != null)
            {
                Global.EnableLandmarkSystem = lm.enableLandmarkSystem;
                Global.MaxLandmarksPerNpc = lm.maxLandmarksPerNpc;
                Global.MaxEdgesPerNpc = lm.maxEdgesPerNpc;
                Global.MaxPoiAnchorsPerNpc = lm.maxPoiAnchorsPerNpc;
                Global.MaxWorldLandmarks = lm.maxWorldLandmarks;

                // Day3: eviction params (NPC-side)
                // "stale" = dopo quanti tick una conoscenza non vista può essere rimossa
                // "cooldown" = dopo una eviction, per quanti tick evitiamo di re-imparare lo stesso item (anti-thrashing)
                Global.LandmarkEvictionStaleTicks = lm.eviction != null ? lm.eviction.eviction_stale_ticks : 600;
                Global.LandmarkEvictionCooldownTicks = lm.eviction != null ? lm.eviction.eviction_cooldown_ticks : 120;
            }
            else
            {
                // Fallback safe se la sezione non esiste nel json.
                Global.EnableLandmarkSystem = false;
                Global.MaxLandmarksPerNpc = 64;
                Global.MaxEdgesPerNpc = 192;
                Global.MaxPoiAnchorsPerNpc = 32;
                Global.MaxWorldLandmarks = 512;

                // Day3 eviction defaults
                Global.LandmarkEvictionStaleTicks = 600;
                Global.LandmarkEvictionCooldownTicks = 120;
            }
        }

        /// <summary>
        /// (v0.02 Day2) Bootstrap API:
        /// ricostruisce LandmarkRegistry in base allo stato corrente della mappa.
        ///
        /// Deve essere chiamato:
        /// - DOPO che il seeding ha piazzato muri/porte/oggetti sulla griglia.
        ///
        /// Motivazione:
        /// - il World viene costruito prima del seeding (SimulationHost), quindi in ctor
        ///   non abbiamo ancora la geometria completa.
        /// </summary>
        public void RebuildLandmarksBootstrap()
        {
            LandmarkRegistry?.RebuildFromWorld(this);
        }

        // ============================================================
        // DEVTOOLS / RUNTIME DEVELOPER MODE (DevMode v0 - MVP)
        // ============================================================
        //
        // Documento di riferimento:
        // - "ARCONTIO - Runtime Developer Mode (DevTools).html"
        // - DevMode v0 richiede: place/erase oggetti + rebuild globale cache + save/load JSON. îfileciteîturn4file9î
        //
        // IMPORTANTE (architettura):
        // - Queste API NON sono "gameplay". Sono strumenti di debug.
        // - Sono chiamate da comandi devtools (ICommand) eseguiti tramite SimulationHost.
        // - La View non deve mutare direttamente il World.

        /// <summary>
        /// RebuildDerivedCachesGlobal (DevMode v0 - MVP):
        /// ricostruisce TUTTE le cache derivate dal World.
        ///
        /// PerchÃ© esiste:
        /// - In questo progetto abbiamo cache derivate (OcclusionMap, bool[] blocks, LandmarkRegistry, ecc.)
        /// - In gameplay normale, queste cache vengono mantenute incrementalmente.
        /// - In DevMode v0 (MVP) vogliamo un comportamento semplice e robusto:
        ///   "dopo ogni edit, rifaccio tutto" per evitare bug di incoerenza.
        ///
        /// Nota performance:
        /// - Questo Ã¨ volutamente O(nCells + nObjects).
        /// - Va benissimo per mappe piccole di debug.
        /// - In DevMode v1/v2 potremo ottimizzare (rebuild parziale / brush continuo / ecc.).
        /// </summary>
        public void RebuildDerivedCachesGlobal()
        {
            RebuildOcclusionCacheGlobal();

            // Landmark registry Ã¨ anch'esso derivato dalla geometria mappa.
            // Nota: questa chiamata Ã¨ safe se LandmarkRegistry Ã¨ null.
            LandmarkRegistry?.RebuildFromWorld(this);
        }

        /// <summary>
        /// Ricostruisce da zero la cache occlusione + bool blocks + indice 1 object per cell.
        /// </summary>
        private void RebuildOcclusionCacheGlobal()
        {
            // Difensivo: se array non inizializzati, non facciamo nulla.
            if (_occlusion == null || _occlusion.Length != MapWidth * MapHeight) return;

            // 1) reset completo
            Array.Clear(_occlusion, 0, _occlusion.Length);

            if (_blocksVision != null && _blocksVision.Length == MapWidth * MapHeight)
                Array.Clear(_blocksVision, 0, _blocksVision.Length);

            if (_blocksMovement != null && _blocksMovement.Length == MapWidth * MapHeight)
                Array.Clear(_blocksMovement, 0, _blocksMovement.Length);

            // 2) reset indice "1 object per cell"
            if (_objIdByCell != null && _objIdByCell.Length == MapWidth * MapHeight)
            {
                for (int i = 0; i < _objIdByCell.Length; i++)
                    _objIdByCell[i] = -1;
            }

            // 3) reset componente runtime occluder: verrÃ  ricostruito da PlaceOccluderInCache.
            // Nota:
            // - ObjectOccluders contiene anche occluder "runtime" (es. _runtime_occluder).
            // - In dev rebuild totale, la ricostruiamo da zero in modo coerente.
            ObjectOccluders.Clear();

            // 4) reinseriamo ogni oggetto:
            // - settiamo l'indice cella -> objectId
            // - se l'oggetto ha flags di occlusione, ricreiamo cache occlusion + bool[].
            foreach (var kv in Objects)
            {
                int objectId = kv.Key;
                var obj = kv.Value;
                if (obj == null) continue;

                int x = obj.CellX;
                int y = obj.CellY;

                if (!InBounds(x, y)) continue;

                if (_objIdByCell != null && _objIdByCell.Length == MapWidth * MapHeight)
                    _objIdByCell[CellIndex(x, y)] = objectId;

                if (!TryGetObjectDef(obj.DefId, out var def) || def == null) continue;

                if (def.IsOccluder || def.BlocksVision || def.BlocksMovement)
                    PlaceOccluderInCache(objectId, x, y, def);
            }
        }

        /// <summary>
        /// Esporta lo stato "editabile" dal DevMode in un DevMapData.
        ///
        /// Policy v0:
        /// - includiamo SOLO Objects (non NPC).
        /// - escludiamo il defId interno "_runtime_occluder" (tool legacy).
        /// </summary>
        public DevMapData ExportDevMapData()
        {
            var data = new DevMapData
            {
                Width = MapWidth,
                Height = MapHeight,
                Note = $"exported_at_tick_{TickContext.CurrentTickIndex}"
            };

            foreach (var kv in Objects)
            {
                var obj = kv.Value;
                if (obj == null) continue;

                // Filtriamo la def "interna" (non vogliamo salvarla in mappe di test).
                if (string.Equals(obj.DefId, "_runtime_occluder", StringComparison.OrdinalIgnoreCase))
                    continue;

                data.Objects.Add(new DevMapObject
                {
                    DefId = obj.DefId,
                    X = obj.CellX,
                    Y = obj.CellY,
                    OwnerKind = obj.OwnerKind.ToString(),
                    OwnerId = obj.OwnerId,
                });
            }

            return data;
        }

        /// <summary>
        /// Importa un DevMapData nel World.
        ///
        /// Policy v0:
        /// - clearObjects=true (default): sostituisce completamente il layout oggetti corrente.
        ///
        /// Nota:
        /// - Non tocchiamo NPC, memorie, needs, ecc. (dev mode v1+).
        /// - Se la mappa importata ha dimensioni diverse, NON ridimensioniamo la griglia:
        ///   logghiamo e comunque proviamo a piazzare ciÃ² che entra in bounds.
        /// </summary>
        public void ImportDevMapData(DevMapData data, bool clearObjects = true)
        {
            if (data == null) return;

            if (data.Width != MapWidth || data.Height != MapHeight)
            {
                Debug.LogWarning($"[DevTools] ImportDevMapData: size mismatch. file=({data.Width}x{data.Height}) world=({MapWidth}x{MapHeight}). " +
                                 $"We will import only objects that fit in bounds.");
            }

            if (clearObjects)
            {
                // Snapshot: DestroyObject muta il dizionario.
                var ids = new List<int>(Objects.Keys);
                for (int i = 0; i < ids.Count; i++)
                    DestroyObject(ids[i]);
            }

            if (data.Objects == null) return;

            for (int i = 0; i < data.Objects.Count; i++)
            {
                var o = data.Objects[i];
                if (o == null) continue;
                if (string.IsNullOrWhiteSpace(o.DefId)) continue;
                if (!InBounds(o.X, o.Y)) continue;

                // Se in cella esiste qualcosa, lo rimpiazziamo: Ã¨ un import "autoritative".
                int existing = GetObjectAt(o.X, o.Y);
                if (existing >= 0)
                    DestroyObject(existing);

                // OwnerKind parsing (difensivo)
                OwnerKind kind = OwnerKind.None;
                if (!string.IsNullOrWhiteSpace(o.OwnerKind))
                {
                    try { kind = (OwnerKind)Enum.Parse(typeof(OwnerKind), o.OwnerKind, ignoreCase: true); }
                    catch { kind = OwnerKind.None; }
                }

                CreateObject(o.DefId, o.X, o.Y, kind, o.OwnerId);
            }
        }
        


        // ============================================================
        // HELPERS: bounds + cell index
        // ============================================================
        private int Idx(int x, int y) => (y * MapWidth) + x;

        public bool InBounds(int x, int y)
            => x >= 0 && y >= 0 && x < MapWidth && y < MapHeight;

        private int CellIndex(int x, int y) => (y * MapWidth) + x;

        public int GetObjectAt(int x, int y)
        {
            if (!InBounds(x, y)) return -1;
            return _objIdByCell[CellIndex(x, y)];
        }


        // ============================================================
        // LANDMARK MEMORY HELPERS (v0.02 Day3)
        // ============================================================
        /// <summary>
        /// EnsureNpcLandmarkMemory:
        /// crea (se mancante) lo store di memoria landmark per questo NPC.
        ///
        /// Nota:
        /// - La memoria landmark è una cache soggettiva: non è obbligatorio averla quando
        ///   il sistema landmark è disabilitato.
        /// - Tuttavia è estremamente utile creare lo store al momento della creazione dell'NPC
        ///   per evitare allocazioni in momenti imprevedibili (es. durante un tick di movimento).
        /// </summary>
        public NpcLandmarkMemory EnsureNpcLandmarkMemory(int npcId)
        {
            if (!NpcLandmarkMemory.TryGetValue(npcId, out var mem) || mem == null)
            {
                mem = new NpcLandmarkMemory(maxLandmarks: Global.MaxLandmarksPerNpc, maxEdges: Global.MaxEdgesPerNpc);
                NpcLandmarkMemory[npcId] = mem;
            }
            return mem;
        }

        /// <summary>
        /// NotifyNpcMovedForLandmarkLearning (Day3):
        /// apprendimento event-driven agganciato al movimento.
        ///
        /// Contratto: viene chiamato SOLO quando l'NPC ha effettivamente cambiato cella.
        ///
        /// Cosa fa (baseline Day3):
        /// - se la cella di arrivo è un landmark attivo nel registry (Doorway/Junction), lo "impara"
        /// - se prima era stato visto un altro landmark, prova a "imparare" anche l'edge tra i due
        ///   (solo se esiste nel registry oggettivo: edge bootstrap Day2)
        ///
        /// Importante:
        /// - non facciamo scanning globale
        /// - non facciamo pathfinding qui
        /// - qui aggiorniamo SOLO la memoria soggettiva e i contatori debug
        /// </summary>
        public void NotifyNpcMovedForLandmarkLearning(int npcId, int fromX, int fromY, int toX, int toY)
        {
            // Se il sistema landmarks è disabilitato, non impariamo nulla.
            if (!Global.EnableLandmarkSystem)
                return;

            if (LandmarkRegistry == null)
                return;

            // Se l'NPC non esiste, abort.
            if (!NpcCore.ContainsKey(npcId))
                return;

            // Day3: impariamo SOLO se la cella di arrivo è un nodo landmark.
            if (!LandmarkRegistry.TryGetActiveNodeIdAtCell(toX, toY, out int nodeId))
                return;

            long now = TickContext.CurrentTickIndex;

            var mem = EnsureNpcLandmarkMemory(npcId);

            // Learn node (anti-thrashing gestito dentro NpcLandmarkMemory)
            mem.LearnLandmark(nodeId, now, evictionCooldownTicks: Global.LandmarkEvictionCooldownTicks);

            // Learn edge: se abbiamo un last landmark diverso
            int prev = mem.LastVisitedLandmarkId;
            if (prev != 0 && prev != nodeId)
            {
                if (LandmarkRegistry.TryGetActiveEdgeCostCells(prev, nodeId, out int costCells))
                {
                    mem.LearnEdge(prev, nodeId, costCells, now, evictionCooldownTicks: Global.LandmarkEvictionCooldownTicks);
                }
            }

            // Aggiorniamo sempre l'ultimo landmark visitato quando ne vediamo uno.
            mem.LastVisitedLandmarkId = nodeId;
            mem.LastVisitedLandmarkTick = now;

            // Day5: se l'NPC sta eseguendo una macro-route, l'ingresso su un landmark
            // puo' far avanzare l'indice del prossimo checkpoint.
            TryAdvanceMacroRouteExecutionAtCell(npcId, toX, toY);
        }



        // ============================================================
        // DAY4 - MACRO ROUTE PLANNER (A*)
        // ============================================================

        /// <summary>
        /// ResolveStartLandmark(currentCell):
        /// - se l'NPC e' gia' sopra un landmark noto, quello e' lo start;
        /// - altrimenti scegliamo il landmark noto piu' vicino (euristica Manhattan).
        ///
        /// Nota importante:
        /// - Il Day4 lavora PRIMA sulla memoria soggettiva dell'NPC, non sul registry completo del mondo.
        /// - La verifica di micro-raggiungibilita' locale verra' raffinata nel Day5.
        /// </summary>
        public bool TryResolveStartLandmark(int npcId, int currentX, int currentY, out int startNodeId, out string failReason)
        {
            startNodeId = 0;
            failReason = string.Empty;

            if (!Global.EnableLandmarkSystem)
            {
                failReason = "LandmarkSystemDisabled";
                return false;
            }

            if (LandmarkRegistry == null)
            {
                failReason = "NoLandmarkRegistry";
                return false;
            }

            if (!NpcLandmarkMemory.TryGetValue(npcId, out var mem) || mem == null || mem.KnownLandmarksCount <= 0)
            {
                failReason = "NoKnownLandmarks";
                return false;
            }

            if (LandmarkRegistry.TryGetActiveNodeIdAtCell(currentX, currentY, out int nodeAtCell) && mem.ContainsLandmark(nodeAtCell))
            {
                startNodeId = nodeAtCell;
                return true;
            }

            if (mem.TryFindNearestKnownLandmark(LandmarkRegistry, currentX, currentY, out int nearest))
            {
                startNodeId = nearest;
                return true;
            }

            failReason = "NoResolvableStartLandmark";
            return false;
        }

        /// <summary>
        /// ResolveTargetLandmark(targetCell):
        /// - se la cella target coincide con un landmark noto all'NPC, usiamo quello;
        /// - altrimenti scegliamo il landmark noto piu' vicino alla cella target.
        /// </summary>
        public bool TryResolveTargetLandmark(int npcId, int targetX, int targetY, out int targetNodeId, out string failReason)
        {
            targetNodeId = 0;
            failReason = string.Empty;

            if (!Global.EnableLandmarkSystem)
            {
                failReason = "LandmarkSystemDisabled";
                return false;
            }

            if (LandmarkRegistry == null)
            {
                failReason = "NoLandmarkRegistry";
                return false;
            }

            if (!NpcLandmarkMemory.TryGetValue(npcId, out var mem) || mem == null || mem.KnownLandmarksCount <= 0)
            {
                failReason = "NoKnownLandmarks";
                return false;
            }

            if (LandmarkRegistry.TryGetActiveNodeIdAtCell(targetX, targetY, out int nodeAtCell) && mem.ContainsLandmark(nodeAtCell))
            {
                targetNodeId = nodeAtCell;
                return true;
            }

            if (mem.TryFindNearestKnownLandmark(LandmarkRegistry, targetX, targetY, out int nearest))
            {
                targetNodeId = nearest;
                return true;
            }

            failReason = "NoResolvableTargetLandmark";
            return false;
        }

        /// <summary>
        /// PlanMacroRoute(startLandmark, targetLandmark):
        /// A* sul sottografo soggettivo dell'NPC.
        ///
        /// Scoring Day4:
        /// - costo base = CostCells dell'edge conosciuto
        /// - penalita' reliability = (1 - reliability01) * 2
        ///
        /// Nota:
        /// - La reliability e' ancora una proxy molto semplice (confidence edge).
        /// - La failure ladder vera arrivera' nel Day6.
        /// </summary>
        public bool TryPlanMacroRoute(int npcId, int startNodeId, int targetNodeId, out NpcMacroRoutePlan plan)
        {
            plan = new NpcMacroRoutePlan
            {
                StartNodeId = startNodeId,
                TargetNodeId = targetNodeId,
                Succeeded = false,
                FailureReason = string.Empty,
            };

            if (!Global.EnableLandmarkSystem)
            {
                plan.FailureReason = "LandmarkSystemDisabled";
                return false;
            }

            if (LandmarkRegistry == null)
            {
                plan.FailureReason = "NoLandmarkRegistry";
                return false;
            }

            if (!NpcLandmarkMemory.TryGetValue(npcId, out var mem) || mem == null)
            {
                plan.FailureReason = "NoLandmarkMemory";
                return false;
            }

            if (startNodeId == 0 || targetNodeId == 0)
            {
                plan.FailureReason = "InvalidEndpoint";
                return false;
            }

            if (!mem.ContainsLandmark(startNodeId) || !mem.ContainsLandmark(targetNodeId))
            {
                plan.FailureReason = "EndpointNotKnown";
                return false;
            }

            if (startNodeId == targetNodeId)
            {
                plan.NodeIds.Add(startNodeId);
                plan.Succeeded = true;
                return true;
            }

            var open = new List<int>(16) { startNodeId };
            var cameFrom = new Dictionary<int, int>(32);
            var gScore = new Dictionary<int, float>(32) { [startNodeId] = 0f };
            var fScore = new Dictionary<int, float>(32) { [startNodeId] = HeuristicCost(startNodeId, targetNodeId) };
            var neighborBuffer = new List<NpcLandmarkMemory.KnownNeighbor>(8);
            var closed = new HashSet<int>();

            while (open.Count > 0)
            {
                int current = PopLowestF(open, fScore);
                if (current == targetNodeId)
                {
                    ReconstructRoute(current, cameFrom, plan.NodeIds);
                    plan.Succeeded = true;
                    plan.FailureReason = string.Empty;
                    return true;
                }

                closed.Add(current);
                mem.FillKnownNeighbors(current, neighborBuffer);

                for (int i = 0; i < neighborBuffer.Count; i++)
                {
                    var nb = neighborBuffer[i];
                    if (closed.Contains(nb.NodeId))
                        continue;

                    float reliabilityPenalty = (1f - Mathf.Clamp01(nb.Reliability01)) * 2f;
                    float tentativeG = GetScore(gScore, current) + Mathf.Max(1, nb.CostCells) + reliabilityPenalty;

                    if (!ContainsNode(open, nb.NodeId))
                        open.Add(nb.NodeId);
                    else if (tentativeG >= GetScore(gScore, nb.NodeId))
                        continue;

                    cameFrom[nb.NodeId] = current;
                    gScore[nb.NodeId] = tentativeG;
                    fScore[nb.NodeId] = tentativeG + HeuristicCost(nb.NodeId, targetNodeId);
                }
            }

            plan.FailureReason = "NoMacroRoute";
            return false;
        }

        private float HeuristicCost(int aNodeId, int bNodeId)
        {
            if (LandmarkRegistry == null) return 0f;
            if (!LandmarkRegistry.TryGetActiveNodeById(aNodeId, out var aNode) || aNode == null)
                return 0f;
            if (!LandmarkRegistry.TryGetActiveNodeById(bNodeId, out var bNode) || bNode == null)
                return 0f;
            return Mathf.Abs(aNode.CellX - bNode.CellX) + Mathf.Abs(aNode.CellY - bNode.CellY);
        }

        private static bool ContainsNode(List<int> list, int nodeId)
        {
            for (int i = 0; i < list.Count; i++)
                if (list[i] == nodeId) return true;
            return false;
        }

        private static float GetScore(Dictionary<int, float> map, int nodeId)
            => map.TryGetValue(nodeId, out var value) ? value : float.PositiveInfinity;

        private static int PopLowestF(List<int> openList, Dictionary<int, float> scores)
        {
            int bestIndex = 0;
            float bestScore = GetScore(scores, openList[0]);
            for (int i = 1; i < openList.Count; i++)
            {
                float s = GetScore(scores, openList[i]);
                if (s < bestScore)
                {
                    bestScore = s;
                    bestIndex = i;
                }
            }
            int node = openList[bestIndex];
            openList.RemoveAt(bestIndex);
            return node;
        }

        private static void ReconstructRoute(int currentNodeId, Dictionary<int, int> parents, List<int> outNodeIds)
        {
            outNodeIds.Clear();
            outNodeIds.Add(currentNodeId);
            while (parents.TryGetValue(currentNodeId, out int parent))
            {
                currentNodeId = parent;
                outNodeIds.Add(currentNodeId);
            }
            outNodeIds.Reverse();
        }

        /// <summary>
        /// API job-friendly del Day4:
        /// parte da NPC + cella target e restituisce una macro-route coerente e ripetibile.
        /// </summary>
        public bool TryPlanMacroRouteForCell(int npcId, int targetX, int targetY, out NpcMacroRoutePlan plan)
        {
            plan = new NpcMacroRoutePlan
            {
                TargetCellX = targetX,
                TargetCellY = targetY,
                Succeeded = false,
                FailureReason = string.Empty,
            };

            if (!GridPos.TryGetValue(npcId, out var pos))
            {
                plan.FailureReason = "NpcHasNoGridPos";
                return false;
            }

            if (!TryResolveStartLandmark(npcId, pos.X, pos.Y, out int startNodeId, out string startFail))
            {
                plan.FailureReason = startFail;
                return false;
            }

            if (!TryResolveTargetLandmark(npcId, targetX, targetY, out int targetNodeId, out string targetFail))
            {
                plan.FailureReason = targetFail;
                return false;
            }

            if (!TryPlanMacroRoute(npcId, startNodeId, targetNodeId, out var innerPlan))
            {
                innerPlan.TargetCellX = targetX;
                innerPlan.TargetCellY = targetY;
                plan = innerPlan;
                return false;
            }

            innerPlan.TargetCellX = targetX;
            innerPlan.TargetCellY = targetY;
            plan = innerPlan;
            return true;
        }

        /// <summary>
        /// RebuildDebugMacroRouteForNpc:
        /// utility usata dai comandi di movimento/debug per mantenere visibile la route macro piu' recente.
        /// </summary>
        public void RebuildDebugMacroRouteForNpc(int npcId, int targetX, int targetY)
        {
            if (!ExistsNpc(npcId)) return;

            if (TryPlanMacroRouteForCell(npcId, targetX, targetY, out var plan))
            {
                NpcMacroRoutes[npcId] = plan;
            }
            else
            {
                NpcMacroRoutes[npcId] = plan;
            }
        }

        public void BeginMacroRouteExecutionForNpc(int npcId, int targetX, int targetY)
        {
            if (!ExistsNpc(npcId))
                return;

            RebuildDebugMacroRouteForNpc(npcId, targetX, targetY);

            if (!NpcMacroRoutes.TryGetValue(npcId, out var plan) || plan == null || !plan.Succeeded)
            {
                NpcMacroRouteExecution.Remove(npcId);
                return;
            }

            var state = new NpcMacroRouteExecutionState
            {
                Active = true,
                IsDoingLastMile = false,
                NextRouteNodeIndex = 0,
                FinalTargetCellX = targetX,
                FinalTargetCellY = targetY,
                ImmediateTargetX = targetX,
                ImmediateTargetY = targetY,
                FailureReason = string.Empty,
            };

            if (GridPos.TryGetValue(npcId, out var pos) && plan.NodeIds.Count > 0)
            {
                if (LandmarkRegistry != null && LandmarkRegistry.TryGetActiveNodeById(plan.NodeIds[0], out var startNode) && startNode != null)
                {
                    if (startNode.CellX == pos.X && startNode.CellY == pos.Y)
                        state.NextRouteNodeIndex = 1;
                }
            }

            if (state.NextRouteNodeIndex >= plan.NodeIds.Count)
            {
                state.IsDoingLastMile = true;
                state.ImmediateTargetX = targetX;
                state.ImmediateTargetY = targetY;
            }
            else if (LandmarkRegistry != null && LandmarkRegistry.TryGetActiveNodeById(plan.NodeIds[state.NextRouteNodeIndex], out var nextNode) && nextNode != null)
            {
                state.ImmediateTargetX = nextNode.CellX;
                state.ImmediateTargetY = nextNode.CellY;
            }
            else
            {
                state.IsDoingLastMile = true;
                state.ImmediateTargetX = targetX;
                state.ImmediateTargetY = targetY;
            }

            NpcMacroRouteExecution[npcId] = state;
        }

        public void ClearDebugMacroRouteForNpc(int npcId)
        {
            NpcMacroRoutes.Remove(npcId);
            NpcMacroRouteExecution.Remove(npcId);
        }

        public bool TryAdvanceMacroRouteExecutionAtCell(int npcId, int cellX, int cellY)
        {
            if (!NpcMacroRouteExecution.TryGetValue(npcId, out var state) || state == null || !state.Active)
                return false;
            if (!NpcMacroRoutes.TryGetValue(npcId, out var plan) || plan == null || !plan.Succeeded)
                return false;

            bool changed = false;
            if (!state.IsDoingLastMile)
            {
                while (state.NextRouteNodeIndex < plan.NodeIds.Count)
                {
                    if (LandmarkRegistry == null || !LandmarkRegistry.TryGetActiveNodeById(plan.NodeIds[state.NextRouteNodeIndex], out var nextNode) || nextNode == null)
                    {
                        state.IsDoingLastMile = true;
                        state.ImmediateTargetX = state.FinalTargetCellX;
                        state.ImmediateTargetY = state.FinalTargetCellY;
                        changed = true;
                        break;
                    }

                    if (nextNode.CellX != cellX || nextNode.CellY != cellY)
                        break;

                    state.NextRouteNodeIndex++;
                    changed = true;
                }

                if (state.NextRouteNodeIndex >= plan.NodeIds.Count)
                {
                    state.IsDoingLastMile = true;
                    state.ImmediateTargetX = state.FinalTargetCellX;
                    state.ImmediateTargetY = state.FinalTargetCellY;
                    changed = true;
                }
                else if (!state.IsDoingLastMile && LandmarkRegistry != null && LandmarkRegistry.TryGetActiveNodeById(plan.NodeIds[state.NextRouteNodeIndex], out var currentNode) && currentNode != null)
                {
                    state.ImmediateTargetX = currentNode.CellX;
                    state.ImmediateTargetY = currentNode.CellY;
                }
            }
            else
            {
                state.ImmediateTargetX = state.FinalTargetCellX;
                state.ImmediateTargetY = state.FinalTargetCellY;
            }

            NpcMacroRouteExecution[npcId] = state;
            return changed;
        }

        public bool TryGetMacroExecutionImmediateTarget(int npcId, out int targetX, out int targetY, out bool isLastMile, out int nextNodeId)
        {
            targetX = 0;
            targetY = 0;
            isLastMile = false;
            nextNodeId = 0;

            if (!NpcMacroRouteExecution.TryGetValue(npcId, out var state) || state == null || !state.Active)
                return false;

            targetX = state.ImmediateTargetX;
            targetY = state.ImmediateTargetY;
            isLastMile = state.IsDoingLastMile;

            if (!isLastMile && NpcMacroRoutes.TryGetValue(npcId, out var plan) && plan != null && state.NextRouteNodeIndex >= 0 && state.NextRouteNodeIndex < plan.NodeIds.Count)
                nextNodeId = plan.NodeIds[state.NextRouteNodeIndex];

            return true;
        }

        public void MarkMacroRouteExecutionBlocked(int npcId, bool duringLastMile)
        {
            if (!NpcMacroRouteExecution.TryGetValue(npcId, out var state) || state == null)
                return;

            state.Active = false;
            state.FailureReason = duringLastMile ? "BlockedLastMile" : "BlockedToNextLandmark";
            NpcMacroRouteExecution[npcId] = state;
        }

        public bool TryGetNpcMacroRouteDebugReport(int npcId, out NpcMacroRouteDebugReport report)
        {
            // Runtime-first: la card deve funzionare anche senza macro-route LM attiva.
            bool hasPlan = NpcMacroRoutes.TryGetValue(npcId, out var plan) && plan != null;
            bool hasMacroState = NpcMacroRouteExecution.TryGetValue(npcId, out var state) && state != null;
            bool hasDirectState = NpcDirectCommitExecution.TryGetValue(npcId, out var directState) && directState != null;
            bool hasLocalState = NpcGoalLocalSearchExecution.TryGetValue(npcId, out var localState) && localState != null;
            bool hasMoveIntent = NpcMoveIntents.TryGetValue(npcId, out var intent) && intent.Active;

            if (!hasPlan && !hasMacroState && !hasDirectState && !hasLocalState && !hasMoveIntent)
            {
                report = default;
                return false;
            }

            bool execActive = false;
            bool isLastMile = false;
            int nextIndex = -1;
            int nextNodeId = 0;
            int routeNodeCount = hasPlan ? plan.NodeIds.Count : 0;
            int startNodeId = hasPlan ? plan.StartNodeId : 0;
            int targetNodeId = hasPlan ? plan.TargetNodeId : 0;
            int targetCellX = hasPlan ? plan.TargetCellX : 0;
            int targetCellY = hasPlan ? plan.TargetCellY : 0;
            string failureReason = hasPlan ? (plan.FailureReason ?? string.Empty) : string.Empty;
            int immediateX = targetCellX;
            int immediateY = targetCellY;
            string execFail = string.Empty;
            string navigationMode = "IDLE";
            int lastModeSwitchTick = -1;
            string lastModeSwitchReason = string.Empty;
            bool goalLocalSearchActive = false;
            int goalLocalSearchBudgetRemaining = 0;

            if (hasMoveIntent)
            {
                targetCellX = intent.TargetX;
                targetCellY = intent.TargetY;
                immediateX = intent.TargetX;
                immediateY = intent.TargetY;
                execActive = true;
            }

            if (hasMacroState)
            {
                execActive = execActive || state.Active;
                isLastMile = state.IsDoingLastMile;
                nextIndex = state.NextRouteNodeIndex;
                immediateX = state.ImmediateTargetX;
                immediateY = state.ImmediateTargetY;
                execFail = state.FailureReason ?? string.Empty;
                navigationMode = !string.IsNullOrEmpty(state.NavigationMode) ? state.NavigationMode : (state.Active ? "LM_PATH" : navigationMode);
                lastModeSwitchTick = state.LastModeSwitchTick;
                lastModeSwitchReason = state.LastModeSwitchReason ?? string.Empty;
                if (!isLastMile && hasPlan && nextIndex >= 0 && nextIndex < plan.NodeIds.Count)
                    nextNodeId = plan.NodeIds[nextIndex];
                if (targetCellX == 0 && targetCellY == 0)
                {
                    targetCellX = state.FinalTargetCellX;
                    targetCellY = state.FinalTargetCellY;
                }
            }

            if (hasDirectState && directState.Active)
            {
                execActive = true;
                immediateX = directState.ImmediateTargetX;
                immediateY = directState.ImmediateTargetY;
                targetCellX = directState.FinalTargetCellX;
                targetCellY = directState.FinalTargetCellY;
                navigationMode = "DIRECT_COMMIT";
                if (string.IsNullOrEmpty(lastModeSwitchReason))
                    lastModeSwitchReason = "DirectCommitActive";
            }

            if (hasLocalState && localState.Active)
            {
                execActive = true;
                goalLocalSearchActive = true;
                goalLocalSearchBudgetRemaining = localState.BudgetRemaining;
                immediateX = localState.ImmediateTargetX;
                immediateY = localState.ImmediateTargetY;
                targetCellX = localState.FinalTargetCellX;
                targetCellY = localState.FinalTargetCellY;
                navigationMode = "GOAL_LOCAL_SEARCH";
                if (string.IsNullOrEmpty(lastModeSwitchReason))
                    lastModeSwitchReason = "LocalSearchActive";
            }

            if (execActive && navigationMode == "IDLE")
            {
                if (DebugJumpPathCells.TryGetValue(npcId, out var jumpDbg) && jumpDbg != null && jumpDbg.Count >= 2)
                    navigationMode = "GOAL_LOCAL_SEARCH";
                else if (DebugDirectPathCells.TryGetValue(npcId, out var directDbg) && directDbg != null && directDbg.Count >= 2)
                    navigationMode = "DIRECT_COMMIT";
                else if (hasPlan)
                    navigationMode = "LM_PATH";
            }

            report = new NpcMacroRouteDebugReport(
                hasRoute: hasPlan && plan.Succeeded,
                startNodeId: startNodeId,
                targetNodeId: targetNodeId,
                routeNodeCount: routeNodeCount,
                targetCellX: targetCellX,
                targetCellY: targetCellY,
                failureReason: failureReason,
                executionActive: execActive,
                isDoingLastMile: isLastMile,
                nextRouteNodeIndex: nextIndex,
                nextRouteNodeId: nextNodeId,
                immediateTargetX: immediateX,
                immediateTargetY: immediateY,
                executionFailureReason: execFail,
                navigationMode: navigationMode,
                lastModeSwitchTick: lastModeSwitchTick,
                lastModeSwitchReason: lastModeSwitchReason,
                goalLocalSearchActive: goalLocalSearchActive,
                goalLocalSearchBudgetRemaining: goalLocalSearchBudgetRemaining
            );
            return true;
        }

// ============================================================
        // LANDMARK DEBUG (v0.02 Day1)
        // ============================================================
        /// <summary>
        /// Debug report stampabile per NPC.
        ///
        /// Roadmap Day1:
        /// - deve funzionare anche se il sistema landmark Ã¨ disabilitato o non implementato.
        /// - quindi: ritorniamo valori coerenti (zeri) e NON facciamo mai throw.
        ///
        /// Nota:
        /// - Quando introdurremo LandmarkMemory/Graph per NPC (Day3+), questo metodo diventerÃ 
        ///   la query unica (view-safe) per esporre contatori.
        /// </summary>
        public bool TryGetNpcLandmarkDebugReport(int npcId, out NpcLandmarkDebugReport report)
        {
            // Day1/Day2: non esiste ancora il grafo soggettivo per NPC.
            // Tuttavia:
            // - Day1: micro-test fittizio per validare overlay.
            // - Day2: registry oggettivo (World-side) che possiamo giÃ  contare/mostrare.

            bool npcExists = NpcCore.ContainsKey(npcId);
            if (!npcExists)
            {
                report = default;
                return false;
            }

            var dbg = Config?.Sim?.landmarks?.debug;
            bool microTestEnabled = dbg != null && dbg.enabled && dbg.microTestDummyGraph;

            if (microTestEnabled)
            {
                // Report coerente con il grafo dummy generato in GetNpcLandmarkOverlayData().
                report = new NpcLandmarkDebugReport(
                    knownLandmarksCount: 2,
                    knownEdgesCount: 1,
                    poiAnchorCount: 0,
                    replansPerMin: 0f,
                    failuresPerMin: 0f,
                    blacklistSize: 0);
                return true;
            }

            // Day3: contatori soggettivi.
            // Importante:
            // - NON esponiamo piÃ¹ i contatori oggettivi del registry come 'known' dell'NPC.
            // - se l'NPC non ha ancora imparato nulla, i contatori restano 0.
            //
            // Nota:
            // - In debug puÃ² essere utile vedere anche i contatori del registry (World-side),
            //   ma quelli appartengono a un'altra query (non questa).
            int lmCount = 0;
            int eCount = 0;

            if (NpcLandmarkMemory.TryGetValue(npcId, out var mem) && mem != null)
            {
                lmCount = mem.KnownLandmarksCount;
                eCount = mem.KnownEdgesCount;
            }

            report = new NpcLandmarkDebugReport(
                knownLandmarksCount: lmCount,
                knownEdgesCount: eCount,
                poiAnchorCount: 0,
                replansPerMin: 0f,
                failuresPerMin: 0f,
                blacklistSize: 0);

            return true;
        }

        /// <summary>
        /// Overlay data (nodi + edges) per un singolo NPC, separato in due layer:
        ///
        /// - WORLD: tutti i landmark/edge oggettivi del registry.
        /// - KNOWN: subset soggettivo conosciuto dall'NPC.
        ///
        /// Motivazione della modifica:
        /// - In debug vogliamo vedere SEMPRE il grafo del mondo.
        /// - Sopra a quel grafo vogliamo anche vedere, in colore diverso, cio' che l'NPC ha imparato.
        /// - Non vogliamo piu' il comportamento "world oppure memory": i due layer devono convivere.
        ///
        /// Nota:
        /// - Non introduciamo nuovi parametri JSON: e' una scelta hardcoded di debug/UX.
        /// </summary>
        public void GetNpcLandmarkOverlayData(
    int npcId,
    System.Collections.Generic.List<LandmarkOverlayNode> outWorldNodes,
    System.Collections.Generic.List<LandmarkOverlayEdge> outWorldEdges,
    System.Collections.Generic.List<LandmarkOverlayNode> outKnownNodes,
    System.Collections.Generic.List<LandmarkOverlayEdge> outKnownEdges,
    System.Collections.Generic.List<LandmarkOverlayNode> outRouteNodes,
    System.Collections.Generic.List<LandmarkOverlayEdge> outRouteEdges,
    System.Collections.Generic.List<LandmarkOverlayEdge> outLmPathEdges,
    System.Collections.Generic.List<LandmarkOverlayEdge> outDirectPathEdges,
    System.Collections.Generic.List<LandmarkOverlayEdge> outJumpPathEdges,
    out NpcMacroRouteDebugReport routeReport)
        {
            // ============================================================
            // PATCH 0.02.05.2f - GetNpcLandmarkOverlayData riallineato
            // ============================================================
            // Questo metodo deve:
            // 1) riempire i layer classici dell'overlay landmark:
            //    - WORLD
            //    - KNOWN
            //    - ROUTE
            // 2) pulire e, se disponibile, riempire i layer runtime del path:
            //    - LM_PATH
            //    - DIRECT_COMMIT
            //    - GOAL_LOCAL_SEARCH / JUMP
            // 3) restituire il report debug route/card
            //
            // Nel file attuale gli helper:
            // - FillDebugVisibleGraphOverlayData(...)
            // - FillDebugKnownGraphOverlayData(...)
            // - FillDebugNavigationPathOverlayData(...)
            // non risultano definiti.
            //
            // Per evitare ulteriori drift di firma o compile error, qui rimettiamo
            // inline la parte WORLD/KNOWN che esisteva già, lasciando separato solo
            // FillDebugMacroRouteOverlayData(...), che invece è presente davvero.

            if (outWorldNodes != null) outWorldNodes.Clear();
            if (outWorldEdges != null) outWorldEdges.Clear();
            if (outKnownNodes != null) outKnownNodes.Clear();
            if (outKnownEdges != null) outKnownEdges.Clear();
            if (outRouteNodes != null) outRouteNodes.Clear();
            if (outRouteEdges != null) outRouteEdges.Clear();

            if (outLmPathEdges != null) outLmPathEdges.Clear();
            if (outDirectPathEdges != null) outDirectPathEdges.Clear();
            if (outJumpPathEdges != null) outJumpPathEdges.Clear();

            if (!TryGetNpcMacroRouteDebugReport(npcId, out routeReport))
                routeReport = default;

            if (!NpcCore.ContainsKey(npcId))
                return;

            var dbg = Config?.Sim?.landmarks?.debug;
            bool microTestEnabled = dbg != null && dbg.enabled && dbg.microTestDummyGraph;

            if (microTestEnabled)
            {
                if (GridPos.TryGetValue(npcId, out var gp))
                {
                    int dx = Mathf.Max(1, dbg.microTestDummyDistanceCells);
                    int ax = Mathf.Clamp(gp.X, 0, MapWidth - 1);
                    int ay = Mathf.Clamp(gp.Y, 0, MapHeight - 1);
                    int bx = Mathf.Clamp(ax + dx, 0, MapWidth - 1);
                    int by = ay;

                    if (bx == ax)
                        bx = Mathf.Clamp(ax - dx, 0, MapWidth - 1);

                    outWorldNodes?.Add(new LandmarkOverlayNode(cellX: ax, cellY: ay, kind: 0));
                    outWorldNodes?.Add(new LandmarkOverlayNode(cellX: bx, cellY: by, kind: 0));
                    outWorldEdges?.Add(new LandmarkOverlayEdge(ax: ax, ay: ay, bx: bx, by: by, reliability01: 1f));
                }

                return;
            }

            // ============================================================
            // WORLD GRAPH
            // ============================================================
            LandmarkRegistry?.FillOverlayData(outWorldNodes, outWorldEdges);

            // ============================================================
            // KNOWN GRAPH
            // ============================================================
            if (NpcLandmarkMemory.TryGetValue(npcId, out var mem) && mem != null)
            {
                mem.FillOverlayData(LandmarkRegistry, outKnownNodes, outKnownEdges);
            }

            // ============================================================
            // MACRO ROUTE
            // ============================================================
            FillDebugMacroRouteOverlayData(npcId, outRouteNodes, outRouteEdges);

            // ============================================================
            // PATH RUNTIME CELLA-PER-CELLA
            // ============================================================
            FillDebugNavigationPathOverlayData(npcId, outLmPathEdges, outDirectPathEdges, outJumpPathEdges);
        }
        private void FillDebugNavigationPathOverlayData(
            int npcId,
            System.Collections.Generic.List<LandmarkOverlayEdge> outLmPathEdges,
            System.Collections.Generic.List<LandmarkOverlayEdge> outDirectPathEdges,
            System.Collections.Generic.List<LandmarkOverlayEdge> outJumpPathEdges)
        {
            if (outLmPathEdges != null) outLmPathEdges.Clear();
            if (outDirectPathEdges != null) outDirectPathEdges.Clear();
            if (outJumpPathEdges != null) outJumpPathEdges.Clear();

            if (DebugLmPathCells.TryGetValue(npcId, out var lmCells) && lmCells != null)
                AppendOverlayEdgesFromCellPath(lmCells, outLmPathEdges);

            if (DebugDirectPathCells.TryGetValue(npcId, out var directCells) && directCells != null)
                AppendOverlayEdgesFromCellPath(directCells, outDirectPathEdges);

            if (DebugJumpPathCells.TryGetValue(npcId, out var jumpCells) && jumpCells != null)
                AppendOverlayEdgesFromCellPath(jumpCells, outJumpPathEdges);
        }

        private static void AppendOverlayEdgesFromCellPath(
            System.Collections.Generic.List<GridPosition> path,
            System.Collections.Generic.List<LandmarkOverlayEdge> outEdges)
        {
            if (path == null || outEdges == null || path.Count < 2)
                return;

            for (int i = 0; i < path.Count - 1; i++)
            {
                var a = path[i];
                var b = path[i + 1];
                outEdges.Add(new LandmarkOverlayEdge(ax: a.X, ay: a.Y, bx: b.X, by: b.Y, reliability01: 1f));
            }
        }

        public void ClearDebugNavigationPathsForNpc(int npcId)
        {
            DebugLmPathCells.Remove(npcId);
            DebugDirectPathCells.Remove(npcId);
            DebugJumpPathCells.Remove(npcId);
            NpcDirectCommitExecution.Remove(npcId);
            NpcGoalLocalSearchExecution.Remove(npcId);
        }

        public void SetDebugDirectPathForNpc(int npcId, System.Collections.Generic.List<Vector2Int> path)
        {
            var list = EnsureDebugPathList(DebugDirectPathCells, npcId);
            CopyVectorPathToGridPath(path, list);

            var state = new NpcDirectCommitExecutionState
            {
                Active = list.Count >= 2,
                FinalTargetCellX = list.Count > 0 ? list[list.Count - 1].X : 0,
                FinalTargetCellY = list.Count > 0 ? list[list.Count - 1].Y : 0,
                ImmediateTargetX = list.Count > 1 ? list[1].X : 0,
                ImmediateTargetY = list.Count > 1 ? list[1].Y : 0,
                NextPathIndex = list.Count > 1 ? 1 : 0,
                FailureReason = string.Empty,
            };
            state.CurrentPath.Clear();
            state.CurrentPath.AddRange(list);
            NpcDirectCommitExecution[npcId] = state;

            if (NpcMacroRouteExecution.TryGetValue(npcId, out var macro) && macro != null)
            {
                macro.NavigationMode = state.Active ? "DIRECT_COMMIT" : macro.NavigationMode;
                macro.LastModeSwitchTick = (int)TickContext.CurrentTickIndex;
                macro.LastModeSwitchReason = state.Active ? "DirectPathPrepared" : macro.LastModeSwitchReason;
                NpcMacroRouteExecution[npcId] = macro;
            }
        }

        public void SetDebugJumpPathForNpc(int npcId, System.Collections.Generic.List<Vector2Int> path, int budgetRemaining)
        {
            var list = EnsureDebugPathList(DebugJumpPathCells, npcId);
            CopyVectorPathToGridPath(path, list);

            var state = new NpcGoalLocalSearchExecutionState
            {
                Active = list.Count >= 2,
                FinalTargetCellX = list.Count > 0 ? list[list.Count - 1].X : 0,
                FinalTargetCellY = list.Count > 0 ? list[list.Count - 1].Y : 0,
                ImmediateTargetX = list.Count > 1 ? list[1].X : 0,
                ImmediateTargetY = list.Count > 1 ? list[1].Y : 0,
                BudgetRemaining = budgetRemaining,
                NextPathIndex = list.Count > 1 ? 1 : 0,
                CommitStepsRemaining = GetLocalSearchCommitMinSteps(),
                HasLastSuccessfulStep = false,
                LastStepFromX = 0,
                LastStepFromY = 0,
                LastStepToX = 0,
                LastStepToY = 0,
                FailureReason = string.Empty,
            };
            state.CurrentPath.Clear();
            state.CurrentPath.AddRange(list);
            NpcGoalLocalSearchExecution[npcId] = state;

            if (NpcDirectCommitExecution.TryGetValue(npcId, out var direct) && direct != null)
            {
                direct.Active = false;
                direct.FailureReason = string.Empty;
                NpcDirectCommitExecution[npcId] = direct;
            }

            if (NpcMacroRouteExecution.TryGetValue(npcId, out var macro) && macro != null)
            {
                macro.NavigationMode = state.Active ? "GOAL_LOCAL_SEARCH" : macro.NavigationMode;
                macro.LastModeSwitchTick = (int)TickContext.CurrentTickIndex;
                macro.LastModeSwitchReason = state.Active ? "DirectBlocked" : macro.LastModeSwitchReason;
                NpcMacroRouteExecution[npcId] = macro;
            }
        }

        public void AppendDebugLmStepForNpc(int npcId, int fromX, int fromY, int toX, int toY)
        {
            AppendDebugStep(DebugLmPathCells, npcId, fromX, fromY, toX, toY);
        }

        public void AppendDebugDirectStepForNpc(int npcId, int fromX, int fromY, int toX, int toY)
        {
            // Intenzionalmente vuoto: manteniamo solo il path azzurro completo pianificato.
        }

        public void AppendDebugJumpStepForNpc(int npcId, int fromX, int fromY, int toX, int toY)
        {
            // Intenzionalmente vuoto: manteniamo un solo magenta, quello del path locale corrente.
        }

        // ============================================================
        // LOCAL SEARCH FAILURE LEARNING HELPERS
        // ============================================================
        private long MakeLocalSearchFailureSignature(int originX, int originY, int targetX, int targetY)
        {
            unchecked
            {
                long a = ((long)(originX & 0xFFFF) << 48);
                long b = ((long)(originY & 0xFFFF) << 32);
                long c = ((long)(targetX & 0xFFFF) << 16);
                long d = (long)(targetY & 0xFFFF);
                return a | b | c | d;
            }
        }

        private Dictionary<long, LocalSearchFailureRecord> EnsureNpcLocalSearchFailureLearning(int npcId)
        {
            if (!NpcLocalSearchFailureLearning.TryGetValue(npcId, out var map) || map == null)
            {
                map = new Dictionary<long, LocalSearchFailureRecord>(16);
                NpcLocalSearchFailureLearning[npcId] = map;
            }
            return map;
        }

        private void PruneExpiredLocalSearchFailures(int npcId, int memoryTicks, int nowTick)
        {
            if (memoryTicks <= 0)
                return;
            if (!NpcLocalSearchFailureLearning.TryGetValue(npcId, out var map) || map == null || map.Count == 0)
                return;

            var toRemove = new List<long>();
            foreach (var kv in map)
            {
                var rec = kv.Value;
                if (rec == null)
                {
                    toRemove.Add(kv.Key);
                    continue;
                }
                if (nowTick - rec.LastFailedTick > memoryTicks)
                    toRemove.Add(kv.Key);
            }

            for (int i = 0; i < toRemove.Count; i++)
                map.Remove(toRemove[i]);
        }

        private bool TryGetRecentLocalSearchFailure(int npcId, long signature, int memoryTicks, int nowTick, out LocalSearchFailureRecord record)
        {
            record = null;
            PruneExpiredLocalSearchFailures(npcId, memoryTicks, nowTick);
            if (!NpcLocalSearchFailureLearning.TryGetValue(npcId, out var map) || map == null)
                return false;
            if (!map.TryGetValue(signature, out record) || record == null)
                return false;
            if (memoryTicks > 0 && nowTick - record.LastFailedTick > memoryTicks)
                return false;
            return true;
        }

        private void RememberLocalSearchFailure(int npcId, int originX, int originY, int targetX, int targetY, int blockedFirstStepCellIndex, int lastProgressCellIndex)
        {
            var cfg = Config?.Sim?.landmarks?.localSearch ?? new Arcontio.Core.Config.LandmarkLocalSearchParams();
            if (!cfg.enableFailureLearning)
                return;

            int nowTick = (int)TickContext.CurrentTickIndex;
            var map = EnsureNpcLocalSearchFailureLearning(npcId);
            var sig = MakeLocalSearchFailureSignature(originX, originY, targetX, targetY);
            if (!map.TryGetValue(sig, out var rec) || rec == null)
            {
                rec = new LocalSearchFailureRecord();
                map[sig] = rec;
            }

            rec.FailureCount++;
            rec.LastFailedTick = nowTick;
            rec.BlockedFirstStepCellIndex = blockedFirstStepCellIndex;
            rec.LastProgressCellIndex = lastProgressCellIndex;
        }

        private void RememberLocalSearchSuccess(int npcId, int originX, int originY, int targetX, int targetY)
        {
            if (!NpcLocalSearchFailureLearning.TryGetValue(npcId, out var map) || map == null)
                return;

            map.Remove(MakeLocalSearchFailureSignature(originX, originY, targetX, targetY));
        }

        public void ClearNpcLocalSearchFailureLearning(int npcId)
        {
            NpcLocalSearchFailureLearning.Remove(npcId);
        }

        public void ClearNpcLocalSearchState(int npcId, string failureReason = "")
        {
            DebugJumpPathCells.Remove(npcId);
            if (NpcGoalLocalSearchExecution.TryGetValue(npcId, out var state) && state != null)
            {
                state.Active = false;
                state.FailureReason = failureReason ?? string.Empty;
                NpcGoalLocalSearchExecution[npcId] = state;
            }
            else
            {
                NpcGoalLocalSearchExecution.Remove(npcId);
            }
        }

        public void ClearNpcDirectCommitState(int npcId, string failureReason = "")
        {
            if (NpcDirectCommitExecution.TryGetValue(npcId, out var state) && state != null)
            {
                state.Active = false;
                state.FailureReason = failureReason ?? string.Empty;
                NpcDirectCommitExecution[npcId] = state;
            }
            else
            {
                NpcDirectCommitExecution.Remove(npcId);
            }
        }

        private static System.Collections.Generic.List<GridPosition> EnsureDebugPathList(
            Dictionary<int, List<GridPosition>> store,
            int npcId)
        {
            if (!store.TryGetValue(npcId, out var list) || list == null)
            {
                list = new List<GridPosition>(64);
                store[npcId] = list;
            }
            return list;
        }

        private static void CopyVectorPathToGridPath(System.Collections.Generic.List<Vector2Int> src, System.Collections.Generic.List<GridPosition> dst)
        {
            dst.Clear();
            if (src == null)
                return;

            for (int i = 0; i < src.Count; i++)
                dst.Add(new GridPosition(src[i].x, src[i].y));
        }

        private static void AppendDebugStep(Dictionary<int, List<GridPosition>> store, int npcId, int fromX, int fromY, int toX, int toY)
        {
            var list = EnsureDebugPathList(store, npcId);
            if (list.Count == 0)
                list.Add(new GridPosition(fromX, fromY));
            else
            {
                var last = list[list.Count - 1];
                if (last.X != fromX || last.Y != fromY)
                    list.Add(new GridPosition(fromX, fromY));
            }
            list.Add(new GridPosition(toX, toY));
        }

        private void FillDebugMacroRouteOverlayData(
            int npcId,
            System.Collections.Generic.List<LandmarkOverlayNode> outRouteNodes,
            System.Collections.Generic.List<LandmarkOverlayEdge> outRouteEdges)
        {
            if (outRouteNodes != null) outRouteNodes.Clear();
            if (outRouteEdges != null) outRouteEdges.Clear();

            if (LandmarkRegistry == null)
                return;

            if (!NpcMacroRoutes.TryGetValue(npcId, out var routePlan) || routePlan == null || !routePlan.Succeeded || routePlan.NodeIds.Count == 0)
                return;

            for (int i = 0; i < routePlan.NodeIds.Count; i++)
            {
                if (!LandmarkRegistry.TryGetActiveNodeById(routePlan.NodeIds[i], out var n) || n == null)
                    continue;

                string label = n.Kind == LandmarkRegistry.LandmarkKind.Doorway ? $"D#{n.Id}" : $"J#{n.Id}";
                outRouteNodes?.Add(new LandmarkOverlayNode(cellX: n.CellX, cellY: n.CellY, kind: (int)n.Kind, nodeId: n.Id, label: label));

                if (i <= 0)
                    continue;

                if (!LandmarkRegistry.TryGetActiveNodeById(routePlan.NodeIds[i - 1], out var prev) || prev == null)
                    continue;

                outRouteEdges?.Add(new LandmarkOverlayEdge(ax: prev.CellX, ay: prev.CellY, bx: n.CellX, by: n.CellY, reliability01: 1f));
            }
        }

// ============================================================
        // INVENTORY HELPERS


        /// <summary>
        /// SetFoodStock (Patch 5.1):
        /// Punto unico (best practice) per scrivere/aggiornare FoodStocks.
        ///
        /// PerchÃ© esiste:
        /// - Evita "footgun" dove qualcuno fa FoodStocks[objId]=... senza aggiornare altri sistemi.
        /// - Qui aggiorniamo anche il pinned belief (NpcPinnedFoodStockBeliefs) quando appropriato.
        ///
        /// IMPORTANTISSIMO (revised policy):
        /// - Aggiorniamo il pinned belief SOLO in ingresso (creazione/posizionamento conosciuto dall'NPC).
        /// - NON cancelliamo il pinned belief automaticamente su furto/distruzione offscreen:
        ///   la cancellazione avviene solo quando l'NPC ispeziona e constata l'assenza.
        /// </summary>
        public void SetFoodStock(int objectId, FoodStockComponent stock)
        {
            // Scrittura del componente oggettivo (fact).
            FoodStocks[objectId] = stock;

            // Se lo stock Ã¨ privato di un NPC, aggiungiamo (o aggiorniamo) la belief pinned.
            // Nota: questo non significa che l'NPC lo "vede ora", significa che lo stock Ã¨ stato
            // creato/assegnato a lui in un punto del gameplay dove la conoscenza Ã¨ implicita
            // (es. lo ha posato lui, lo ha ricevuto come proprietÃ ).
            if (stock.OwnerKind == OwnerKind.Npc && stock.OwnerId != 0)
            {
                if (Objects.TryGetValue(objectId, out var obj) && obj != null)
                {
                    EnsurePinnedFoodStockBelief(stock.OwnerId, objectId, obj.CellX, obj.CellY);
                }
            }
        }

        /// <summary>
        /// EnsurePinnedFoodStockBelief:
        /// Inserisce il riferimento (objectId + lastKnown cell) nella lista belief dell'NPC.
        /// Se giÃ  presente, aggiorna la posizione (utile quando lo stock viene posato/spostato
        /// tramite azione dell'NPC proprietario).
        /// </summary>
        public void EnsurePinnedFoodStockBelief(int npcId, int objectId, int lastKnownX, int lastKnownY)
        {
            if (npcId == 0 || objectId == 0)
                return;

            if (!NpcPinnedFoodStockBeliefs.TryGetValue(npcId, out var list) || list == null)
            {
                list = new List<PinnedFoodStockBelief>(capacity: 4);
                NpcPinnedFoodStockBeliefs[npcId] = list;
            }

            // Update in-place se giÃ  esiste.
            for (int i = 0; i < list.Count; i++)
            {
                if (list[i].ObjectId == objectId)
                {
                    var e = list[i];
                    e.LastKnownX = lastKnownX;
                    e.LastKnownY = lastKnownY;
                    list[i] = e;
                    return;
                }
            }

            // Altrimenti aggiungiamo una nuova belief entry.
            list.Add(new PinnedFoodStockBelief
            {
                ObjectId = objectId,
                LastKnownX = lastKnownX,
                LastKnownY = lastKnownY
            });
        }

        /// <summary>
        /// RemovePinnedFoodStockBelief:
        /// Rimuove una belief entry.
        ///
        /// POLICY:
        /// - Questa chiamata va usata quando l'NPC *scopre* che lo stock non Ã¨ dove si aspettava
        ///   (es. arriva sul posto e non lo trova), oppure quando ha un'informazione "valida"
        ///   (in futuro: comunicazione, witness, ecc.).
        /// </summary>
        public void RemovePinnedFoodStockBelief(int npcId, int objectId)
        {
            if (npcId == 0 || objectId == 0)
                return;

            if (!NpcPinnedFoodStockBeliefs.TryGetValue(npcId, out var list) || list == null)
                return;

            for (int i = 0; i < list.Count; i++)
            {
                if (list[i].ObjectId == objectId)
                {
                    list.RemoveAt(i);
                    break;
                }
            }

            // Cleanup: se vuota, rimuoviamo la chiave per evitare dizionari gonfi.
            if (list.Count == 0)
                NpcPinnedFoodStockBeliefs.Remove(npcId);
        }

        // ============================================================
        //
        // Obiettivo:
        // - centralizzare la logica di capienza dell'inventario dell'NPC
        // - evitare duplicazioni in Rule/System/CommandHandler
        //
        // Nota:
        // - Oggi contiamo solo NpcPrivateFood (unitÃ  di cibo portate addosso).
        // - In futuro, quando introdurremo altri oggetti trasportati, questo metodo
        //   dovrÃ  includere anche quelle collezioni (es: NpcCarriedItems, ecc.).
        // - Importante: il World Ã¨ "source of truth" dello stato globale; la view legge soltanto.

        /// <summary>
        /// Capienza massima dell'inventario di questo NPC (in unitÃ ).
        ///
        /// Oggi Ã¨ globale (uguale per tutti gli NPC).
        /// In futuro potremo introdurre override per archetipo/ruolo.
        /// </summary>
        public int GetInventoryMaxUnits(int npcId)
        {
            // npcId non Ã¨ ancora usato (perchÃ© la capienza Ã¨ globale),
            // ma lo teniamo per compatibilitÃ  futura con override per archetipo.
            return Global.InventoryMaxUnits;
        }

        /// <summary>
        /// UnitÃ  giÃ  occupate nell'inventario dell'NPC.
        ///
        /// ATTUALE:
        /// - cibo privato trasportato (NpcPrivateFood)
        ///
        /// FUTURO:
        /// - altri oggetti trasportati da aggiungere qui.
        /// </summary>
        public int GetInventoryUsedUnits(int npcId)
        {
            // Food carried (private).
            NpcPrivateFood.TryGetValue(npcId, out int food);
            if (food < 0) food = 0;

            // TODO (futuro): sommare altre categorie di item carried.
            // Esempio:
            // int other = NpcCarriedItems.TryGetValue(npcId, out var list) ? list.Count : 0;

            return food;
        }

        /// <summary>
        /// Quante unitÃ  libere restano nell'inventario dell'NPC.
        /// </summary>
        public int GetInventoryFreeCapacity(int npcId)
        {
            int max = GetInventoryMaxUnits(npcId);
            int used = GetInventoryUsedUnits(npcId);

            int free = max - used;
            if (free < 0) free = 0;
            return free;
        }


        // ============================================================
        // NPC API
        // ============================================================

        public bool ExistsNpc(int npcId) => npcId > 0 && NpcCore.ContainsKey(npcId);
        public bool AreBonded(int aNpcId, int bNpcId)
        {
            // STUB (roadmap): quando introdurrai bond graph,
            // questa funzione consulterÃ  quel grafo.
            return false;
        }

        public int CreateNpc(NpcCore core, Needs needs, Social social, int x, int y)
        {
            int id = _nextNpcId++;

            NpcCore[id] = core;
            Needs[id] = needs;
            Social[id] = social;
            GridPos[id] = new GridPosition(x, y);
            NpcFacing[id] = CardinalDirection.North;

            // Memory params
            if (!MemoryParams.ContainsKey(id))
                MemoryParams[id] = PersonalityMemoryParams.DefaultNpc();

            // MemoryStore: NON ha costruttore con maxTraces.
            // Imposti MaxTraces dopo la creazione.
            var store = new MemoryStore();

            // PrioritÃ : se hai un config globale, usalo; altrimenti fallback su PersonalityMemoryParams.
            int maxTraces = MemoryParams[id].MaxTraces;
            if (Global.NpcObjectMemorySlots > 0) { /* non Ã¨ maxTraces: Ã¨ slots oggetti (altro). */ }

            store.MaxTraces = maxTraces;
            Memory[id] = store;

            // Private food init (se non presente)
            if (!NpcPrivateFood.ContainsKey(id))
                NpcPrivateFood[id] = 0;

            // Tick consumo privato
            if (!NpcLastPrivateFoodConsumeTick.ContainsKey(id))
                NpcLastPrivateFoodConsumeTick[id] = -999999;

            // Movement intent init (se non presente)
            if (!NpcMoveIntents.ContainsKey(id))
                NpcMoveIntents[id] = default;

            // Scan state init (se non presente)
            if (!NpcScanStates.ContainsKey(id))
                NpcScanStates[id] = default;


            // ============================================================
            // Landmark memory init (v0.02 Day3)
            // ============================================================
            //
            // Importante:
            // - La memoria landmark e una cache soggettiva e viene popolata event-driven durante il movimento.
            // - Creiamo lo store qui per evitare allocazioni durante i tick e per garantire che
            //   ogni NPC abbia un contenitore pronto (anche se all'inizio e vuoto).
            //
            // Nota:
            // - Se il sistema landmarks e disabilitato, non e dannoso avere lo store (vuoto).
            EnsureNpcLandmarkMemory(id);


// Action state init (se non presente)
if (!NpcAction.ContainsKey(id))
    NpcAction[id] = NpcActionState.Idle();

            // Balloon signal init (se non presente)
            // Nota:
            // - Default = struct default => Kind=None, Tick=0.
            // - La view userÃ  il tick per capire se ha giÃ  mostrato il balloon.
            if (!NpcBalloonSignals.ContainsKey(id))
                NpcBalloonSignals[id] = default;
            return id;
        }

        public void SetFacing(int npcId, CardinalDirection dir)
        {
            if (!ExistsNpc(npcId)) return;
            NpcFacing[npcId] = dir;
        }

        // ============================================================
        // Facing / Position / Movement helper API
        // ============================================================

        /// <summary>
        /// GetFacing:
        /// - Non esisteva come helper; Ã¨ utile per sistemi (scan, movement) e debug.
        /// - Se manca entry, default = North (deterministico).
        /// </summary>
        public CardinalDirection GetFacing(int npcId)
        {
            if (!ExistsNpc(npcId)) return CardinalDirection.North;
            if (NpcFacing.TryGetValue(npcId, out var dir)) return dir;
            return CardinalDirection.North;
        }

        /// <summary>
        /// TryGetNpcPos:
        /// - Fonte di veritÃ  runtime per la posizione NPC Ã¨ GridPos.
        /// </summary>
        public bool TryGetNpcPos(int npcId, out int x, out int y)
        {
            x = 0; y = 0;
            if (!GridPos.TryGetValue(npcId, out var gp)) return false;
            x = gp.X; y = gp.Y;
            return true;
        }

        /// <summary>
        /// SetNpcPos:
        /// - Aggiorna GridPos.
        /// - In futuro questo Ã¨ il punto perfetto per pubblicare NpcMovedEvent / PerceptionDirty.
        /// </summary>
        public void SetNpcPos(int npcId, int x, int y)
        {
            if (!ExistsNpc(npcId)) return;
            GridPos[npcId] = new GridPosition(x, y);
        }

        public bool TryGetObjectCell(int objectId, out int x, out int y)
        {
            x = 0; y = 0;
            if (!Objects.TryGetValue(objectId, out var obj) || obj == null) return false;
            x = obj.CellX; y = obj.CellY;
            return true;
        }

        public bool BlocksMovementAt(int x, int y)
        {
            if (!TryGetOccluder(x, y, out _, out bool bm, out _)) return false;
            return bm;
        }

        /// <summary>
        /// O(N) per DAY10 (pochi NPC). In futuro cache.
        /// </summary>
        public bool TryGetNpcAt(int x, int y, out int npcId)
        {
            npcId = 0;
            foreach (var kv in GridPos)
            {
                if (kv.Value.X == x && kv.Value.Y == y)
                {
                    npcId = kv.Key;
                    return true;
                }
            }
            return false;
        }

        public void SetMoveIntent(int npcId, MoveIntent intent)
        {
            if (!ExistsNpc(npcId)) return;

            NpcMoveIntents[npcId] = intent;
        }

        public void ClearMoveIntent(int npcId)
        {
            if (!ExistsNpc(npcId)) return;
            NpcMoveIntents[npcId] = default;
        }
        

        // ============================================================
        // NPC ACTION STATE (API)
        // ============================================================

        /// <summary>
        /// Imposta lo stato di azione dell'NPC.
        /// 
        /// Nota:
        /// - Non validiamo "consistenza semantica" qui (es: puoi dire Eat anche se non c'Ã¨ cibo).
        /// - Questo Ã¨ volutamente un canale descrittivo/diagnostico.
        /// - Le guardie sono solo su ExistsNpc.
        /// </summary>
        public void SetNpcAction(int npcId, NpcActionState state)
        {
            if (!ExistsNpc(npcId)) return;
            NpcAction[npcId] = state;
        }

        /// <summary>
        /// Utility: imposta l'NPC in stato Idle (azione "nessuna"/default).
        /// </summary>
        public void SetNpcIdle(int npcId)
        {
            if (!ExistsNpc(npcId)) return;
            NpcAction[npcId] = NpcActionState.Idle();
        }

        /// <summary>
        /// Prova a leggere lo stato di azione corrente.
        /// </summary>
        public bool TryGetNpcAction(int npcId, out NpcActionState state)
        {
            if (!ExistsNpc(npcId))
            {
                state = default;
                return false;
            }
            return NpcAction.TryGetValue(npcId, out state);
        }


        // ============================================================
        // NPC BALLOON SIGNAL (API)
        // ============================================================

        /// <summary>
        /// Imposta l'ultimo balloon signal per un NPC.
        ///
        /// Nota:
        /// - Ã un segnale transiente: la view lo consumerÃ  mostrando un balloon per X secondi.
        /// - NON Ã¨ un bus di eventi.
        /// - Non validiamo semantica qui: Ã¨ solo osservabilitÃ .
        /// </summary>
        public void SetNpcBalloonSignal(int npcId, NpcBalloonSignal signal)
        {
            if (!ExistsNpc(npcId)) return;
            NpcBalloonSignals[npcId] = signal;
        }

        /// <summary>
        /// Convenience: emette un balloon con tick corrente (TickContext).
        /// </summary>
        public void EmitNpcBalloon(int npcId, NpcBalloonKind kind, int subjectId = 0, int secondarySubjectId = 0)
        {
            if (!ExistsNpc(npcId)) return;

            int tick = (int)TickContext.CurrentTickIndex;
            NpcBalloonSignals[npcId] = new NpcBalloonSignal
            {
                Kind = kind,
                Tick = tick,
                SubjectId = subjectId,
                SecondarySubjectId = secondarySubjectId
            };
        }

        public bool TryGetNpcBalloonSignal(int npcId, out NpcBalloonSignal signal)
        {
            if (!ExistsNpc(npcId))
            {
                signal = default;
                return false;
            }
            return NpcBalloonSignals.TryGetValue(npcId, out signal);
        }


        /// <summary>
        /// Utility: consideriamo "idle" se non ha MoveIntent attivo e non sta facendo scan.
        /// Questo Ã¨ volutamente minimale: in futuro ActivityStateComponent puÃ² sostituire.
        /// </summary>
        public bool IsNpcIdleForScan(int npcId)
        {
            if (!ExistsNpc(npcId)) return false;

            if (NpcMoveIntents.TryGetValue(npcId, out var mi) && mi.Active)
                return false;

            if (NpcScanStates.TryGetValue(npcId, out var ss) && ss.Active)
                return false;

            return true;
        }

        public void StartScan(int npcId, int currentTick, int turns = 4)
        {
            if (!ExistsNpc(npcId)) return;

            // Importante:
            // - lo scan Ã¨ "costoso": Ã¨ una sequenza di turn su tick successivi.
            // - non facciamo 4 turn nello stesso tick.
            NpcScanStates[npcId] = new ScanState
            {
                Active = true,
                RemainingTurns = turns,
                LastTurnTick = currentTick - 999999
            };
        }

        public void StopScan(int npcId)
        {
            if (!ExistsNpc(npcId)) return;
            NpcScanStates[npcId] = default;
        }


        // ============================================================
        // OBJECT API (Create / Destroy)
        // ============================================================

        public int CreateObject(string defId, int x, int y, OwnerKind ownerKind = OwnerKind.None, int ownerId = -1)
        {
            if (string.IsNullOrWhiteSpace(defId))
                return -1;

            if (!ObjectDefs.ContainsKey(defId))
            {
                Debug.LogWarning($"[World] CreateObject failed: unknown defId='{defId}'");
                return -1;
            }

            if (MapWidth > 0 && MapHeight > 0 && !InBounds(x, y))
            {
                Debug.LogWarning($"[World] CreateObject failed: out of bounds ({x},{y})");
                return -1;
            }

            if (HasAnyObjectAt(x, y))
            {
                Debug.LogWarning($"[World] CreateObject failed: cell occupied ({x},{y}) (1 object per cell)");
                return -1;
            }

            int id = _nextObjectId++;

            var inst = new WorldObjectInstance
            {
                ObjectId = id,
                DefId = defId,
                CellX = x,
                CellY = y,
                OwnerKind = ownerKind,
                OwnerId = ownerId,
                OccupantNpcId = -1
            };

            Objects[id] = inst;

            // Se Ã¨ un occluder oppure blocca la visione o il movimento, aggiorna la occlusion map.
            if (TryGetObjectDef(defId, out var def) && def != null &&
                               (def.IsOccluder || def.BlocksVision || def.BlocksMovement))
            {
                PlaceOccluderInCache(id, x, y, def);
            }

            return id;
        }

        public void DestroyObject(int objectId)
        {
            if (!Objects.TryGetValue(objectId, out var obj) || obj == null)
                return;

            int x = obj.CellX;
            int y = obj.CellY;

            // 1) Se Ã¨ occluder, pulisci la cache occlusione prima di rimuovere dai dizionari.
            //    (Questo evita che restino celle "bloccate" anche dopo la rimozione dell'oggetto.)
            if (TryGetObjectDef(obj.DefId, out var def) && def != null && def.IsOccluder)
            {
                ClearOccluderFromCache(objectId, x, y);

                // Difensivo: nel tuo file coesistono due cache (OcclusionCell[] e bool[]).
                // HasLineOfSight usa _occlusion, ma alcune API (IsVisionBlocked/IsMovementBlocked) usano bool[].
                // Quindi, se questi array esistono, azzeriamo anche loro.
                if (_blocksVision != null && _blocksVision.Length == MapWidth * MapHeight && InBounds(x, y))
                    _blocksVision[CellIndex(x, y)] = false;

                if (_blocksMovement != null && _blocksMovement.Length == MapWidth * MapHeight && InBounds(x, y))
                    _blocksMovement[CellIndex(x, y)] = false;
            }

            // 2) Libera la cella nella griglia "1 object per cell".
            //    Nel tuo World l'empty value Ã¨ -1 (come da commento su _objIdByCell).
            if (_objIdByCell != null && _objIdByCell.Length == MapWidth * MapHeight && InBounds(x, y))
            {
                int idx = CellIndex(x, y);

                // Difensivo: liberiamo solo se la cella punta davvero a quell'objectId.
                if (_objIdByCell[idx] == objectId)
                    _objIdByCell[idx] = -1;
            }

            // 3) component cleanup (use state, stocks, runtime occluders)
            //
            // IMPORTANTISSIMO (Patch 5.1 - revised):
            // - Qui rimuoviamo i componenti OGGETTIVI (facts) associati all'objectId.
            // - NON tocchiamo il pinned belief degli NPC (NpcPinnedFoodStockBeliefs):
   
            //   se lo stock viene distrutto/rubato fuori dalla loro percezione, loro NON devono
            //   essere aggiornati automaticamente. Scopriranno la perdita solo ispezionando.
            ObjectUse.Remove(objectId);
            FoodStocks.Remove(objectId);
            ObjectOccluders.Remove(objectId);

            // 4) rimuovi dal registry oggetti
            Objects.Remove(objectId);
        }

        private void PlaceOccluderInCache(int objectId, int x, int y, ObjectDef def)
        {
            if (_occlusion == null || _occlusion.Length == 0) return;
            if (!InBounds(x, y)) return;

            int idx = Idx(x, y);

            // Se giÃ  presente qualcosa, lo consideriamo errore di coerenza (1 occluder per cell).
            if (_occlusion[idx].OccluderObjectId != 0 && _occlusion[idx].OccluderObjectId != objectId)
            {
                Debug.LogWarning($"[World] OcclusionMap overwrite at ({x},{y}). old={_occlusion[idx].OccluderObjectId} new={objectId}");
            }

		            // ------------------------------------------------------------
            // IMPORTANTISSIMO (bugfix):
            // In questo World coesistono due cache:
            // - _occlusion[] (OcclusionCell) usata da HasLineOfSight / TryGetOccluder
            // - _blocksVision[] / _blocksMovement[] usate da IsVisionBlocked / IsMovementBlocked
            //
            // CreateObject() aggiorna l'occlusione via PlaceOccluderInCache.
            // Se qui non aggiorniamo anche i bool[], IsMovementBlocked puÃ² restare "false"
            // su celle muro => un NPC puÃ² finire dentro una cella wall.
            // ------------------------------------------------------------

            bool blocksVision = def.BlocksVision;
            bool blocksMove = def.BlocksMovement;

            // Se Ã¨ marcato come occluder ma non ha flags, default "blocca tutto".
            if (def.IsOccluder && !blocksVision && !blocksMove)
            {
                blocksVision = true;
                blocksMove = true;
            }
			
            _occlusion[idx] = new OcclusionCell
            {
                OccluderObjectId = objectId,
                BlocksVision = def.BlocksVision,
                BlocksMovement = def.BlocksMovement,
                VisionCost = def.VisionCost <= 0f ? 1f : def.VisionCost
            };
			
			// Allinea le cache bool[] se esistono (difensivo).
            if (_blocksVision != null && _blocksVision.Length == MapWidth * MapHeight)
                _blocksVision[CellIndex(x, y)] = blocksVision;

            if (_blocksMovement != null && _blocksMovement.Length == MapWidth * MapHeight)
                _blocksMovement[CellIndex(x, y)] = blocksMove;

            // Manteniamo anche il componente runtime per query dettagliate (TryGetOccluder ecc.).
            ObjectOccluders[objectId] = new Occluder
            {
                BlocksVision = blocksVision,
                BlocksMovement = blocksMove,
                VisionCost = def.VisionCost <= 0f ? 1f : def.VisionCost
            };	
        }

        private void ClearOccluderFromCache(int objectId, int x, int y)
        {
            if (_occlusion == null || _occlusion.Length == 0) return;
            if (!InBounds(x, y)) return;

            int idx = Idx(x, y);
            if (_occlusion[idx].OccluderObjectId == objectId)
                _occlusion[idx] = default;

            // Allinea le cache bool[] (difensivo).
            if (_blocksVision != null && _blocksVision.Length == MapWidth * MapHeight)
                _blocksVision[CellIndex(x, y)] = false;

            if (_blocksMovement != null && _blocksMovement.Length == MapWidth * MapHeight)
                _blocksMovement[CellIndex(x, y)] = false;

            // Il registry ObjectOccluders Ã¨ per objectId.
            ObjectOccluders.Remove(objectId);
        }

        /// <summary>
        /// Regola ARCONTIO Core Standard v1.0: 1 object per cell.
        /// Qui facciamo enforcement minimo:
        /// - se esiste giÃ  un oggetto in (x,y) => fail.
        /// </summary>
        public bool HasAnyObjectAt(int x, int y)
        {
            foreach (var kv in Objects)
            {
                var o = kv.Value;
                if (o != null && o.CellX == x && o.CellY == y)
                    return true;
            }
            return false;
        }


        // ------------------------------------------------------------
        // Helpers
        // ------------------------------------------------------------
        public bool TryGetObjectDef(string defId, out ObjectDef def)
        {
            def = null;
            if (string.IsNullOrWhiteSpace(defId)) return false;
            return ObjectDefs.TryGetValue(defId, out def) && def != null;
        }

        public ObjectUseState GetUseStateOrDefault(int objId)
        {
            if (ObjectUse.TryGetValue(objId, out var s))
                return s;

            // default runtime = libero
            return ObjectUseState.Free();
        }

        public void SetUseState(int objId, ObjectUseState s)
        {
            ObjectUse[objId] = s;
        }

        // ============================================================
        // OCCLUSION MAP API (cache)
        // ============================================================

        public bool IsVisionBlocked(int x, int y)
        {
            if (!InBounds(x, y)) return true; // fuori mappa: trattalo come ?chiuso?
            return _blocksVision[CellIndex(x, y)];
        }

        public bool IsMovementBlocked(int x, int y)
        {
            if (!InBounds(x, y)) return true;
            return _blocksMovement[CellIndex(x, y)];
        }

        /// <summary>
        /// TryGetOccluder:
        /// - restituisce true se nella cella c'Ã¨ un oggetto che blocca visione e/o movimento.
        /// - i dettagli stanno in ObjectOccluders (se presenti) altrimenti in def flags.
        /// </summary>
        public bool TryGetOccluder(int x, int y, out bool blocksVision, out bool blocksMovement, out float visionCost)
        {
            blocksVision = false;
            blocksMovement = false;
            visionCost = 0f;

            if (_occlusion == null || _occlusion.Length == 0) return false;
            if (!InBounds(x, y)) return false;

            var c = _occlusion[Idx(x, y)];
            if (c.OccluderObjectId == 0) return false;

            blocksVision = c.BlocksVision;
            blocksMovement = c.BlocksMovement;
            visionCost = c.VisionCost;
            return true;
        }
        public bool BlocksVisionAt(int x, int y)
        {
            if (!TryGetOccluder(x, y, out bool bv, out _, out _)) return false;
            return bv;
        }

        /// <summary>
        /// Wrapper legacy: SetOccluder(x,y,Occluder).
        ///
        /// Per non rompere i test/seed giÃ  scritti, questo:
        /// - crea (o aggiorna) un oggetto in quella cella con defId="_runtime_occluder"
        /// - lo mette in ObjectOccluders e aggiorna la cache.
        ///
        /// Migrazione consigliata:
        /// - sostituiscilo con CreateObject("wall_stone", x,y) ecc.
        /// </summary>
        public void SetOccluder(int x, int y, Occluder occ)
        {
            if (!InBounds(x, y)) return;

            // assicura una def runtime (se manca, la creiamo al volo)
            const string runtimeDef = "_runtime_occluder";
            if (!ObjectDefs.ContainsKey(runtimeDef))
            {
                ObjectDefs[runtimeDef] = new ObjectDef
                {
                    Id = runtimeDef,
                    DisplayName = "Runtime Occluder",
                    IsOccluder = true,
                    BlocksVision = true,
                    BlocksMovement = true,
                    VisionCost = 1f
                };
            }

            int existing = GetObjectAt(x, y);
            int objId;

            if (existing >= 0)
            {
                objId = existing;
                // se c?Ã¨ un oggetto ?normale? giÃ  piazzato, qui sei in conflitto con 1 object/cell.
                // Per il debug: logghiamo e sovrascriviamo SOLO l?occlusione cache, senza cambiare l?oggetto.
                // Se vuoi muro ?vero?, devi piazzare l?oggetto muro e non un letto nella stessa cella.
                Debug.LogWarning($"[World] SetOccluder: cell ({x},{y}) already has obj={existing}. " +
                                 $"Keeping object, overriding occlusion cache only.");
            }
            else
            {
                objId = CreateObject(runtimeDef, x, y, OwnerKind.None, -1);
                if (objId < 0) return;
            }

            // store runtime occluder component
            ObjectOccluders[objId] = occ;

            // aggiorna cache cella
            int idx = CellIndex(x, y);
            _blocksVision[idx] = occ.BlocksVision;
            _blocksMovement[idx] = occ.BlocksMovement;
        }

        private void RebuildOcclusionCell(int x, int y)
        {
            if (!InBounds(x, y)) return;

            int objId = GetObjectAt(x, y);
            if (objId == 0 || !Objects.TryGetValue(objId, out var obj) || obj == null)
            {
                _occlusion[Idx(x, y)] = default;
                return;
            }

            if (!TryGetObjectDef(obj.DefId, out var def) || def == null)
            {
                _occlusion[Idx(x, y)] = default;
                return;
            }

            _occlusion[Idx(x, y)] = new OcclusionCell
            {
                BlocksVision = def.BlocksVision,
                BlocksMovement = def.BlocksMovement,
                VisionCost = def.VisionCost
            };
        }


        // ============================================================
        // MOVEMENT PATH HELPERS (patch 0.02.05.2f)
        // ============================================================

        /// <summary>
        /// Prova a costruire un percorso "diretto" coerente con il movimento reale dell'NPC.
        ///
        /// IMPORTANTISSIMO:
        /// - Qui "diretto" NON significa semplicemente "vedo il target".
        /// - Significa invece: "se da questa cella continuo a fare step greedy verso il target,
        ///   riesco davvero ad arrivarci senza urtare muri e senza attraversare NPC".
        ///
        /// Perché serve questa distinzione:
        /// - in ARCONTIO vogliamo che il Direct Commit abbia priorità sui landmark,
        ///   ma solo quando esiste davvero un piano locale eseguibile;
        /// - se usassimo solo la LOS, potremmo etichettare come diretto un movimento che poi
        ///   si schianta contro un muro o contro una geometria concava.
        ///
        /// Output:
        /// - outCells contiene SEMPRE la sequenza completa delle celle del path, inclusa la sorgente.
        /// - se il metodo restituisce false, outCells viene lasciata vuota.
        /// </summary>
        public bool TryBuildGreedyDirectPath(int npcId, int startX, int startY, int targetX, int targetY, List<Vector2Int> outCells)
        {
            if (outCells == null)
                return false;

            outCells.Clear();

            if (!InBounds(startX, startY) || !InBounds(targetX, targetY))
                return false;

            outCells.Add(new Vector2Int(startX, startY));

            int x = startX;
            int y = startY;

            // Difesa importante: mettiamo un tetto di sicurezza per evitare loop infiniti
            // in caso di bug logici futuri.
            int safety = MapWidth * MapHeight + 8;

            while ((x != targetX || y != targetY) && safety-- > 0)
            {
                int dx = targetX - x;
                int dy = targetY - y;

                int stepX = 0;
                int stepY = 0;

                if (Mathf.Abs(dx) >= Mathf.Abs(dy))
                    stepX = dx == 0 ? 0 : (dx > 0 ? 1 : -1);
                else
                    stepY = dy == 0 ? 0 : (dy > 0 ? 1 : -1);

                int nextX = x + stepX;
                int nextY = y + stepY;

                bool moved = false;
                if (IsWalkableForPathing(npcId, nextX, nextY, targetX, targetY))
                {
                    x = nextX;
                    y = nextY;
                    outCells.Add(new Vector2Int(x, y));
                    moved = true;
                }
                else
                {
                    // Fallback minimo coerente con MovementSystem: se l'asse scelto non funziona,
                    // proviamo l'altro asse. Se fallisce anche quello, il path diretto NON esiste.
                    if (stepX != 0)
                    {
                        stepX = 0;
                        stepY = dy == 0 ? 0 : (dy > 0 ? 1 : -1);
                    }
                    else
                    {
                        stepY = 0;
                        stepX = dx == 0 ? 0 : (dx > 0 ? 1 : -1);
                    }

                    nextX = x + stepX;
                    nextY = y + stepY;

                    if ((stepX != 0 || stepY != 0) && IsWalkableForPathing(npcId, nextX, nextY, targetX, targetY))
                    {
                        x = nextX;
                        y = nextY;
                        outCells.Add(new Vector2Int(x, y));
                        moved = true;
                    }
                }

                if (!moved)
                {
                    outCells.Clear();
                    return false;
                }
            }

            if (x != targetX || y != targetY)
            {
                outCells.Clear();
                return false;
            }

            return true;
        }

        /// <summary>
        /// Costruisce il prefisso diretto massimo coerente con il MovementSystem.
        /// Non richiede che il target finale sia interamente raggiungibile: si ferma al primo blocco.
        /// Serve per rendere visibile la fase direct iniziale anche nei casi "direct poi local search".
        /// </summary>
        public bool TryBuildGreedyDirectPrefixPath(int npcId, int startX, int startY, int targetX, int targetY, List<Vector2Int> outCells)
        {
            if (outCells == null)
                return false;

            outCells.Clear();
            if (!InBounds(startX, startY) || !InBounds(targetX, targetY))
                return false;

            int x = startX;
            int y = startY;
            outCells.Add(new Vector2Int(x, y));

            int safety = (MapWidth * MapHeight) + 8;
            while ((x != targetX || y != targetY) && safety-- > 0)
            {
                int dx = targetX - x;
                int dy = targetY - y;

                int stepX = 0;
                int stepY = 0;
                if (Mathf.Abs(dx) >= Mathf.Abs(dy))
                    stepX = dx == 0 ? 0 : (dx > 0 ? 1 : -1);
                else
                    stepY = dy == 0 ? 0 : (dy > 0 ? 1 : -1);

                bool moved = false;
                int nextX = x + stepX;
                int nextY = y + stepY;
                if (IsWalkableForPathing(npcId, nextX, nextY, targetX, targetY))
                {
                    x = nextX;
                    y = nextY;
                    outCells.Add(new Vector2Int(x, y));
                    moved = true;
                }
                else
                {
                    if (stepX != 0)
                    {
                        stepX = 0;
                        stepY = dy == 0 ? 0 : (dy > 0 ? 1 : -1);
                    }
                    else
                    {
                        stepY = 0;
                        stepX = dx == 0 ? 0 : (dx > 0 ? 1 : -1);
                    }

                    nextX = x + stepX;
                    nextY = y + stepY;
                    if ((stepX != 0 || stepY != 0) && IsWalkableForPathing(npcId, nextX, nextY, targetX, targetY))
                    {
                        x = nextX;
                        y = nextY;
                        outCells.Add(new Vector2Int(x, y));
                        moved = true;
                    }
                }

                if (!moved)
                    break;
            }

            return outCells.Count >= 2;
        }

        // ============================================================
        // PATCH 0.02.02R / 0.02.02Q compat - helper richiesti dal
        // MovementSystem attuale per la local search bounded.
        // ============================================================

        public bool HasActiveNpcLocalSearch(int npcId)
        {
            return NpcGoalLocalSearchExecution.TryGetValue(npcId, out var state)
                && state != null
                && state.Active;
        }

        public bool TryGetActiveNpcLocalSearchNextStep(int npcId, out int stepX, out int stepY)
        {
            stepX = 0;
            stepY = 0;

            if (!NpcGoalLocalSearchExecution.TryGetValue(npcId, out var state) || state == null || !state.Active)
                return false;

            if (state.CurrentPath == null || state.CurrentPath.Count == 0)
                return false;

            // NextPathIndex punta alla prossima cella da consumare.
            if (state.NextPathIndex < 0 || state.NextPathIndex >= state.CurrentPath.Count)
                return false;

            var next = state.CurrentPath[state.NextPathIndex];
            stepX = next.X;
            stepY = next.Y;
            return true;
        }

        public void AdvanceNpcLocalSearchAfterSuccessfulStep(int npcId, int fromX, int fromY, int toX, int toY)
        {
            if (!NpcGoalLocalSearchExecution.TryGetValue(npcId, out var state) || state == null || !state.Active)
                return;

            // Memorizziamo l’ultimo passo riuscito per poter impedire
            // il replan immediato inverso A -> B -> A.
            state.HasLastSuccessfulStep = true;
            state.LastStepFromX = fromX;
            state.LastStepFromY = fromY;
            state.LastStepToX = toX;
            state.LastStepToY = toY;

            if (state.CommitStepsRemaining > 0)
                state.CommitStepsRemaining--;

            if (state.BudgetRemaining > 0)
                state.BudgetRemaining--;

            // Avanza l’indice del path se il passo eseguito coincide con quello atteso.
            if (state.NextPathIndex >= 0 && state.NextPathIndex < state.CurrentPath.Count)
            {
                var expected = state.CurrentPath[state.NextPathIndex];
                if (expected.X == toX && expected.Y == toY)
                {
                    state.NextPathIndex++;
                }
                else
                {
                    // Riallineamento difensivo: cerchiamo la cella raggiunta nel path corrente.
                    int found = -1;
                    for (int i = 0; i < state.CurrentPath.Count; i++)
                    {
                        if (state.CurrentPath[i].X == toX && state.CurrentPath[i].Y == toY)
                        {
                            found = i;
                            break;
                        }
                    }

                    state.NextPathIndex = found >= 0 ? found + 1 : state.CurrentPath.Count;
                }
            }

            // Caso 1: target finale raggiunto -> chiusura pulita.
            if (toX == state.FinalTargetCellX && toY == state.FinalTargetCellY)
            {
                state.Active = false;
                state.CurrentPath.Clear();
                state.NextPathIndex = 0;
                state.ImmediateTargetX = toX;
                state.ImmediateTargetY = toY;

                NpcGoalLocalSearchExecution[npcId] = state;
                DebugJumpPathCells.Remove(npcId);
                SetMacroRouteNavigationMode(npcId, "IDLE", "LocalSearchCompletedTargetReached");
                return;
            }

            // Caso 2: mini-path consumato ma problema locale non ancora risolto.
            // NON rilasciamo subito a LM_PATH: forziamo un replan al tick successivo.
            if (state.NextPathIndex >= state.CurrentPath.Count)
            {
                state.CurrentPath.Clear();
                state.NextPathIndex = 0;
                state.ImmediateTargetX = state.FinalTargetCellX;
                state.ImmediateTargetY = state.FinalTargetCellY;

                // Manteniamo almeno 1 tick di ownership locale.
                state.CommitStepsRemaining = Mathf.Max(state.CommitStepsRemaining, 1);

                NpcGoalLocalSearchExecution[npcId] = state;
                DebugJumpPathCells.Remove(npcId);
                SetMacroRouteNavigationMode(npcId, "GOAL_LOCAL_SEARCH", "LocalSearchNeedsReplan");
                return;
            }

            // Caso 3: path locale ancora vivo -> aggiorna il prossimo step e il magenta.
            var nextStep = state.CurrentPath[state.NextPathIndex];
            state.ImmediateTargetX = nextStep.X;
            state.ImmediateTargetY = nextStep.Y;

            NpcGoalLocalSearchExecution[npcId] = state;
            RefreshDebugJumpPathFromLocalState(npcId);
            SetMacroRouteNavigationMode(npcId, "GOAL_LOCAL_SEARCH", "LocalSearchStepCommitted");
        }

        public bool TryReplanNpcLocalSearch(int npcId, int currentX, int currentY)
        {
            if (!NpcGoalLocalSearchExecution.TryGetValue(npcId, out var state) || state == null || !state.Active)
                return false;

            var cfg = Config?.Sim?.landmarks?.localSearch ?? new LandmarkLocalSearchParams();

            int maxVisited = state.BudgetRemaining > 0
                ? Mathf.Max(8, state.BudgetRemaining)
                : Mathf.Max(8, cfg.maxExpandedNodes);

            var path = new List<Vector2Int>(64);

            if (!TryBuildBoundedMovePath(
                    npcId,
                    currentX,
                    currentY,
                    state.FinalTargetCellX,
                    state.FinalTargetCellY,
                    maxVisited,
                    path) || path.Count < 2)
            {
                state.FailureReason = "LocalReplanFailed";
                NpcGoalLocalSearchExecution[npcId] = state;
                return false;
            }

            // Guardrail anti backtrack immediato: se siamo ancora nel commitment
            // e il nuovo primo passo è l’inverso esatto dell’ultimo passo riuscito, lo rifiutiamo.
            if (ShouldPreventImmediateLocalBacktrack()
                && state.CommitStepsRemaining > 0
                && state.HasLastSuccessfulStep)
            {
                var next = path[1];
                bool isImmediateBacktrack =
                    currentX == state.LastStepToX &&
                    currentY == state.LastStepToY &&
                    next.x == state.LastStepFromX &&
                    next.y == state.LastStepFromY;

                if (isImmediateBacktrack)
                {
                    state.FailureReason = "RejectedImmediateBacktrack";
                    NpcGoalLocalSearchExecution[npcId] = state;
                    return false;
                }
            }

            state.CurrentPath.Clear();
            for (int i = 0; i < path.Count; i++)
                state.CurrentPath.Add(new GridPosition(path[i].x, path[i].y));

            // La cella [0] è la posizione corrente, quindi il prossimo step è [1].
            state.NextPathIndex = 1;
            state.ImmediateTargetX = state.CurrentPath[1].X;
            state.ImmediateTargetY = state.CurrentPath[1].Y;
            state.FailureReason = string.Empty;
            state.Active = true;

            NpcGoalLocalSearchExecution[npcId] = state;
            RefreshDebugJumpPathFromLocalState(npcId);
            SetMacroRouteNavigationMode(npcId, "GOAL_LOCAL_SEARCH", "LocalSearchReplanned");
            return true;
        }

        private void SetMacroRouteNavigationMode(int npcId, string navigationMode, string reason)
        {
            // ============================================================
            // Helper centralizzato per aggiornare lo stato di navigazione
            // visibile nella card/debug report.
            //
            // NOTA:
            // - non crea una macro-route dal nulla
            // - aggiorna solo se per quell'NPC esiste già uno stato runtime
            //   della macro execution
            // ============================================================

            if (!NpcMacroRouteExecution.TryGetValue(npcId, out var exec) || exec == null)
                return;

            exec.NavigationMode = navigationMode ?? string.Empty;
            exec.LastModeSwitchReason = reason ?? string.Empty;
            exec.LastModeSwitchTick = (int)TickContext.CurrentTickIndex;

            NpcMacroRouteExecution[npcId] = exec;
        }

        private void RefreshDebugJumpPathFromLocalState(int npcId)
        {
            // ============================================================
            // Il magenta visibile deve rappresentare UN SOLO path locale
            // attivo corrente, non la somma di:
            // - planned vecchio
            // - storico eseguito
            // - replanning precedenti
            //
            // Questo metodo riscrive il buffer magenta a partire soltanto
            // dal path locale attualmente attivo.
            // ============================================================

            if (!NpcGoalLocalSearchExecution.TryGetValue(npcId, out var state)
                || state == null
                || !state.Active
                || state.CurrentPath == null
                || state.CurrentPath.Count < 2)
            {
                DebugJumpPathCells.Remove(npcId);
                return;
            }

            int startIndex = Mathf.Clamp(
                Mathf.Max(0, state.NextPathIndex - 1),
                0,
                state.CurrentPath.Count - 1);

            var list = EnsureDebugPathList(DebugJumpPathCells, npcId);
            list.Clear();

            for (int i = startIndex; i < state.CurrentPath.Count; i++)
                list.Add(state.CurrentPath[i]);

            if (list.Count < 2)
                DebugJumpPathCells.Remove(npcId);
        }

        private bool ShouldPreventImmediateLocalBacktrack()
        {
            // ============================================================
            // Se true, quando la local search deve fare replan durante il
            // commitment, il nuovo primo passo non può essere l'inverso
            // immediato dell'ultimo passo riuscito.
            //
            // Questo è il guardrail che evita il ping-pong:
            // A -> B
            // poi replan
            // poi B -> A
            // ============================================================

            return Config?.Sim?.landmarks?.localSearch?.preventImmediateBacktrack ?? true;
        }

        private int GetLocalSearchCommitMinSteps()
        {
            // ============================================================
            // Numero minimo di step per cui la local search mantiene la
            // ownership del movimento prima di poter considerare un replan
            // o un rilascio.
            // ============================================================

            return Mathf.Max(1, Config?.Sim?.landmarks?.localSearch?.commitMinSteps ?? 3);
        }


        /// <summary>
        /// Wrapper comodo per i punti del codice che vogliono solo sapere se il Direct Commit
        /// è legalmente attivabile, senza avere bisogno della lista completa di celle.
        /// </summary>
        public bool CanNpcUseDirectPath(int npcId, int targetX, int targetY)
        {
            if (!GridPos.TryGetValue(npcId, out var pos))
                return false;

            var scratch = new List<Vector2Int>(32);
            return TryBuildGreedyDirectPath(npcId, pos.X, pos.Y, targetX, targetY, scratch);
        }

        /// <summary>
        /// Ricerca locale bounded su griglia 4-direzionale.
        ///
        /// IMPORTANTISSIMO:
        /// - Questa NON è una sostituzione filosofica del sistema landmark.
        /// - È un fallback operativo molto locale pensato per uscire da casi patologici:
        ///   stanze, muri a U semplici, landmark immediato dietro ostacolo, ecc.
        ///
        /// Restituisce un path cella-per-cella completo (inclusa la sorgente) se riesce.
        /// </summary>
        public bool TryBuildBoundedMovePath(int npcId, int startX, int startY, int targetX, int targetY, int maxVisited, List<Vector2Int> outCells)
        {
            if (outCells == null)
                return false;

            outCells.Clear();

            if (!InBounds(startX, startY) || !InBounds(targetX, targetY))
                return false;

            var cfg = Config?.Sim?.landmarks?.localSearch ?? new Arcontio.Core.Config.LandmarkLocalSearchParams();
            if (!cfg.enabled)
                return false;

            int expandedLimit = maxVisited > 0 ? Mathf.Min(maxVisited, Mathf.Max(8, cfg.maxExpandedNodes)) : Mathf.Max(8, cfg.maxExpandedNodes);
            int iterationLimit = Mathf.Max(expandedLimit, cfg.maxIterations);
            int radiusLimit = Mathf.Max(1, cfg.maxSearchRadius);
            int jumpLimit = Mathf.Max(1, cfg.maxJumpDistance);
            float hWeight = cfg.heuristicWeight <= 0f ? 1f : cfg.heuristicWeight;
            int nowTick = (int)TickContext.CurrentTickIndex;

            int blockedFirstStepCellIndex = -1;
            if (cfg.enableFailureLearning)
            {
                long signature = MakeLocalSearchFailureSignature(startX, startY, targetX, targetY);
                if (TryGetRecentLocalSearchFailure(npcId, signature, Mathf.Max(1, cfg.failureMemoryTicks), nowTick, out var recentFailure) && recentFailure != null)
                {
                    blockedFirstStepCellIndex = recentFailure.BlockedFirstStepCellIndex;
                    if (recentFailure.FailureCount >= Mathf.Max(1, cfg.repeatedFailureEscalationThreshold))
                    {
                        expandedLimit = Mathf.Max(expandedLimit, cfg.maxExpandedNodes * Mathf.Max(1, cfg.fallbackExpandedNodesMultiplier));
                        iterationLimit = Mathf.Max(iterationLimit, cfg.maxIterations * Mathf.Max(1, cfg.fallbackExpandedNodesMultiplier));
                        radiusLimit = Mathf.Max(radiusLimit, cfg.maxSearchRadius + Mathf.Max(0, cfg.fallbackRadiusBonus));
                        jumpLimit = Mathf.Max(jumpLimit, cfg.maxJumpDistance + Mathf.Max(0, cfg.fallbackRadiusBonus / 2));
                    }
                }
            }

            var candidatePath = new List<Vector2Int>(64);
            var partialBestPath = new List<Vector2Int>(64);
            bool foundCompletePath;

            if (cfg.useJumpPointSearch)
            {
                foundCompletePath = TryBuildBoundedJpsPathInternal(
                    npcId, startX, startY, targetX, targetY,
                    expandedLimit, iterationLimit, radiusLimit, jumpLimit, hWeight,
                    blockedFirstStepCellIndex,
                    candidatePath,
                    partialBestPath);
            }
            else
            {
                foundCompletePath = TryBuildSimpleBoundedBfsPathAdvanced(
                    npcId, startX, startY, targetX, targetY,
                    expandedLimit, radiusLimit, blockedFirstStepCellIndex,
                    candidatePath,
                    partialBestPath);
            }

            if (!foundCompletePath && cfg.enableSmartFallback)
            {
                int expandedFallbackLimit = Mathf.Max(expandedLimit, cfg.maxExpandedNodes * Mathf.Max(1, cfg.fallbackExpandedNodesMultiplier));
                int radiusFallbackLimit = Mathf.Max(radiusLimit, cfg.maxSearchRadius + Mathf.Max(0, cfg.fallbackRadiusBonus));
                int jumpFallbackLimit = Mathf.Max(jumpLimit, cfg.maxJumpDistance + Mathf.Max(0, cfg.fallbackRadiusBonus / 2));
                int iterationFallbackLimit = Mathf.Max(iterationLimit, cfg.maxIterations * Mathf.Max(1, cfg.fallbackExpandedNodesMultiplier));

                if (cfg.useJumpPointSearch)
                {
                    var jpsFallbackPath = new List<Vector2Int>(64);
                    var jpsFallbackPartial = new List<Vector2Int>(64);
                    if (TryBuildBoundedJpsPathInternal(
                        npcId, startX, startY, targetX, targetY,
                        expandedFallbackLimit, iterationFallbackLimit, radiusFallbackLimit, jumpFallbackLimit, hWeight,
                        blockedFirstStepCellIndex,
                        jpsFallbackPath,
                        jpsFallbackPartial))
                    {
                        candidatePath = jpsFallbackPath;
                        partialBestPath = jpsFallbackPartial;
                        foundCompletePath = true;
                    }
                    else if (jpsFallbackPartial.Count > partialBestPath.Count)
                    {
                        partialBestPath = jpsFallbackPartial;
                    }
                }

                if (!foundCompletePath && cfg.fallbackUseBoundedBfs)
                {
                    var bfsFallbackPath = new List<Vector2Int>(64);
                    var bfsFallbackPartial = new List<Vector2Int>(64);
                    if (TryBuildSimpleBoundedBfsPathAdvanced(
                        npcId, startX, startY, targetX, targetY,
                        expandedFallbackLimit, radiusFallbackLimit, blockedFirstStepCellIndex,
                        bfsFallbackPath,
                        bfsFallbackPartial))
                    {
                        candidatePath = bfsFallbackPath;
                        partialBestPath = bfsFallbackPartial;
                        foundCompletePath = true;
                    }
                    else if (bfsFallbackPartial.Count > partialBestPath.Count)
                    {
                        partialBestPath = bfsFallbackPartial;
                    }
                }
            }

            if (foundCompletePath && candidatePath.Count >= 2)
            {
                if (cfg.enablePathSmoothing)
                    SmoothCellPath(npcId, candidatePath, Mathf.Max(2, cfg.smoothingLookahead), outCells);
                else
                    outCells.AddRange(candidatePath);

                RememberLocalSearchSuccess(npcId, startX, startY, targetX, targetY);
                return outCells.Count >= 2;
            }

            if (partialBestPath.Count >= 2)
            {
                if (cfg.enablePathSmoothing)
                    SmoothCellPath(npcId, partialBestPath, Mathf.Max(2, cfg.smoothingLookahead), outCells);
                else
                    outCells.AddRange(partialBestPath);

                int blockedStep = outCells.Count >= 2 ? CellIndex(outCells[1].x, outCells[1].y) : -1;
                int progressCell = outCells.Count > 0 ? CellIndex(outCells[outCells.Count - 1].x, outCells[outCells.Count - 1].y) : -1;
                RememberLocalSearchFailure(npcId, startX, startY, targetX, targetY, blockedStep, progressCell);
                return outCells.Count >= 2;
            }

            RememberLocalSearchFailure(npcId, startX, startY, targetX, targetY, blockedFirstStepCellIndex, -1);
            return false;
        }

        private bool TryBuildBoundedJpsPathInternal(
            int npcId,
            int startX,
            int startY,
            int targetX,
            int targetY,
            int expandedLimit,
            int iterationLimit,
            int radiusLimit,
            int jumpLimit,
            float hWeight,
            int blockedFirstStepCellIndex,
            List<Vector2Int> outPath,
            List<Vector2Int> outBestProgressPath)
        {
            outPath.Clear();
            outBestProgressPath.Clear();

            var start = new Vector2Int(startX, startY);
            var target = new Vector2Int(targetX, targetY);

            var open = new List<JpsOpenNode>(64);
            var bestG = new Dictionary<JpsStateKey, int>(128);
            var parents = new Dictionary<JpsStateKey, JpsParentInfo>(128);

            var startKey = new JpsStateKey(startX, startY, 0, 0);
            open.Add(new JpsOpenNode(startX, startY, 0, 0, 0, HeuristicManhattan(startX, startY, targetX, targetY, hWeight)));
            bestG[startKey] = 0;
            parents[startKey] = new JpsParentInfo(startKey, false);

            int expanded = 0;
            int iterations = 0;
            JpsStateKey foundKey = default;
            bool found = false;
            JpsStateKey bestFrontierKey = startKey;
            float bestFrontierH = HeuristicManhattan(startX, startY, targetX, targetY, hWeight);

            while (open.Count > 0 && iterations < iterationLimit)
            {
                iterations++;
                int bestIndex = 0;
                float bestF = open[0].F;
                float bestH = open[0].H;
                for (int i = 1; i < open.Count; i++)
                {
                    var cand = open[i];
                    if (cand.F < bestF || (Mathf.Approximately(cand.F, bestF) && cand.H < bestH))
                    {
                        bestIndex = i;
                        bestF = cand.F;
                        bestH = cand.H;
                    }
                }

                var current = open[bestIndex];
                open.RemoveAt(bestIndex);
                var currentKey = new JpsStateKey(current.X, current.Y, current.DirX, current.DirY);

                if (!bestG.TryGetValue(currentKey, out int knownG) || knownG != current.G)
                    continue;

                if (current.H < bestFrontierH)
                {
                    bestFrontierH = current.H;
                    bestFrontierKey = currentKey;
                }

                if (current.X == targetX && current.Y == targetY)
                {
                    found = true;
                    foundKey = currentKey;
                    break;
                }

                expanded++;
                if (expanded > expandedLimit)
                    break;

                var dirs = GetJpsSuccessorDirections(npcId, current.X, current.Y, current.DirX, current.DirY, targetX, targetY);
                for (int i = 0; i < dirs.Count; i++)
                {
                    var dir = dirs[i];

                    if (current.X == startX && current.Y == startY && blockedFirstStepCellIndex >= 0)
                    {
                        int firstStepX = current.X + dir.x;
                        int firstStepY = current.Y + dir.y;
                        if (InBounds(firstStepX, firstStepY) && CellIndex(firstStepX, firstStepY) == blockedFirstStepCellIndex)
                            continue;
                    }

                    if (TryJumpStraight(npcId, start, target, current.X, current.Y, dir.x, dir.y, radiusLimit, jumpLimit, out var jumpPoint, out int stepCost))
                    {
                        int g2 = current.G + stepCost;
                        var succKey = new JpsStateKey(jumpPoint.x, jumpPoint.y, dir.x, dir.y);
                        if (bestG.TryGetValue(succKey, out int oldG) && oldG <= g2)
                            continue;

                        bestG[succKey] = g2;
                        parents[succKey] = new JpsParentInfo(currentKey, true);
                        float h = HeuristicManhattan(jumpPoint.x, jumpPoint.y, targetX, targetY, hWeight);
                        open.Add(new JpsOpenNode(jumpPoint.x, jumpPoint.y, dir.x, dir.y, g2, g2 + h, h));
                    }
                }
            }

            if (found)
            {
                BuildExpandedPathFromJpsStates(foundKey, parents, outPath);
                return outPath.Count > 0 && outPath[outPath.Count - 1] == target;
            }

            BuildExpandedPathFromJpsStates(bestFrontierKey, parents, outBestProgressPath);
            return false;
        }

        private void BuildExpandedPathFromJpsStates(JpsStateKey terminalKey, Dictionary<JpsStateKey, JpsParentInfo> parents, List<Vector2Int> outCells)
        {
            outCells.Clear();
            var jumpStates = new List<JpsStateKey>(32);
            var walkKey = terminalKey;
            jumpStates.Add(walkKey);
            while (parents.TryGetValue(walkKey, out var parentInfo) && parentInfo.HasParent)
            {
                walkKey = parentInfo.Parent;
                jumpStates.Add(walkKey);
            }
            jumpStates.Reverse();

            if (jumpStates.Count == 0)
                return;

            outCells.Add(new Vector2Int(jumpStates[0].X, jumpStates[0].Y));
            for (int i = 1; i < jumpStates.Count; i++)
            {
                ExpandStraightSegment(outCells, jumpStates[i - 1].X, jumpStates[i - 1].Y, jumpStates[i].X, jumpStates[i].Y);
            }
        }

        private bool TryBuildSimpleBoundedBfsPathAdvanced(
            int npcId,
            int startX,
            int startY,
            int targetX,
            int targetY,
            int maxVisited,
            int radiusLimit,
            int blockedFirstStepCellIndex,
            List<Vector2Int> outPath,
            List<Vector2Int> outBestProgressPath)
        {
            outPath.Clear();
            outBestProgressPath.Clear();
            var start = new Vector2Int(startX, startY);
            var target = new Vector2Int(targetX, targetY);
            var frontier = new Queue<Vector2Int>();
            var visited = new HashSet<Vector2Int>();
            var cameFrom = new Dictionary<Vector2Int, Vector2Int>();
            frontier.Enqueue(start);
            visited.Add(start);
            Vector2Int[] dirs = new[] { new Vector2Int(1, 0), new Vector2Int(-1, 0), new Vector2Int(0, 1), new Vector2Int(0, -1), };
            bool found = false;
            int expanded = 0;
            Vector2Int best = start;
            int bestH = Mathf.Abs(target.x - start.x) + Mathf.Abs(target.y - start.y);
            while (frontier.Count > 0)
            {
                var cur = frontier.Dequeue();
                int curH = Mathf.Abs(target.x - cur.x) + Mathf.Abs(target.y - cur.y);
                if (curH < bestH)
                {
                    best = cur;
                    bestH = curH;
                }
                if (cur == target) { found = true; break; }
                expanded++;
                if (expanded > maxVisited) break;
                for (int i = 0; i < dirs.Length; i++)
                {
                    var nxt = cur + dirs[i];
                    if (visited.Contains(nxt)) continue;
                    if (Mathf.Abs(nxt.x - start.x) + Mathf.Abs(nxt.y - start.y) > radiusLimit) continue;
                    if (cur == start && blockedFirstStepCellIndex >= 0 && InBounds(nxt.x, nxt.y) && CellIndex(nxt.x, nxt.y) == blockedFirstStepCellIndex) continue;
                    if (!IsWalkableForPathing(npcId, nxt.x, nxt.y, targetX, targetY)) continue;
                    visited.Add(nxt); cameFrom[nxt] = cur; frontier.Enqueue(nxt);
                }
            }
            if (found)
            {
                ReconstructGridPath(start, target, cameFrom, outPath);
                return outPath.Count > 0 && outPath[outPath.Count - 1] == target;
            }
            if (best != start)
                ReconstructGridPath(start, best, cameFrom, outBestProgressPath);
            return false;
        }

        private void ReconstructGridPath(Vector2Int start, Vector2Int target, Dictionary<Vector2Int, Vector2Int> cameFrom, List<Vector2Int> outPath)
        {
            outPath.Clear();
            var rev = new List<Vector2Int>(64);
            var walk = target;
            rev.Add(walk);
            while (walk != start)
            {
                if (!cameFrom.TryGetValue(walk, out var prev)) { outPath.Clear(); return; }
                walk = prev; rev.Add(walk);
            }
            rev.Reverse(); outPath.AddRange(rev);
        }

        private void SmoothCellPath(int npcId, List<Vector2Int> rawPath, int lookahead, List<Vector2Int> outSmoothed)
        {
            outSmoothed.Clear();
            if (rawPath == null || rawPath.Count == 0)
                return;
            if (rawPath.Count <= 2)
            {
                outSmoothed.AddRange(rawPath);
                return;
            }

            var directScratch = new List<Vector2Int>(64);
            int anchorIndex = 0;
            outSmoothed.Add(rawPath[0]);

            while (anchorIndex < rawPath.Count - 1)
            {
                int bestIndex = anchorIndex + 1;
                List<Vector2Int> bestSegment = null;
                int maxIndex = Mathf.Min(rawPath.Count - 1, anchorIndex + Mathf.Max(2, lookahead));
                for (int testIndex = maxIndex; testIndex > anchorIndex + 1; testIndex--)
                {
                    directScratch.Clear();
                    var a = rawPath[anchorIndex];
                    var b = rawPath[testIndex];
                    if (TryBuildGreedyDirectPath(npcId, a.x, a.y, b.x, b.y, directScratch) && directScratch.Count >= 2)
                    {
                        bestIndex = testIndex;
                        bestSegment = new List<Vector2Int>(directScratch);
                        break;
                    }
                }

                if (bestSegment != null)
                {
                    for (int i = 1; i < bestSegment.Count; i++)
                    {
                        if (outSmoothed.Count == 0 || outSmoothed[outSmoothed.Count - 1] != bestSegment[i])
                            outSmoothed.Add(bestSegment[i]);
                    }
                    anchorIndex = bestIndex;
                }
                else
                {
                    var step = rawPath[anchorIndex + 1];
                    if (outSmoothed[outSmoothed.Count - 1] != step)
                        outSmoothed.Add(step);
                    anchorIndex++;
                }
            }
        }

        private float HeuristicManhattan(int x, int y, int tx, int ty, float weight)
        {
            return (Mathf.Abs(tx - x) + Mathf.Abs(ty - y)) * weight;
        }

        private List<Vector2Int> GetJpsSuccessorDirections(int npcId, int x, int y, int dirX, int dirY, int targetX, int targetY)
        {
            var dirs = new List<Vector2Int>(4);
            if (dirX == 0 && dirY == 0)
            {
                dirs.Add(new Vector2Int(1, 0)); dirs.Add(new Vector2Int(-1, 0)); dirs.Add(new Vector2Int(0, 1)); dirs.Add(new Vector2Int(0, -1));
                return dirs;
            }

            dirs.Add(new Vector2Int(dirX, dirY));

            // Pruning straight-only (4-connected): oltre al vicino naturale in avanti,
            // aggiungiamo i forced neighbours implicati da ostacoli laterali.
            if (dirX != 0)
            {
                bool upBlocked = !IsWalkableForPathing(npcId, x, y + 1, targetX, targetY);
                bool downBlocked = !IsWalkableForPathing(npcId, x, y - 1, targetX, targetY);
                if (upBlocked && IsWalkableForPathing(npcId, x + dirX, y + 1, targetX, targetY)) dirs.Add(new Vector2Int(0, 1));
                if (downBlocked && IsWalkableForPathing(npcId, x + dirX, y - 1, targetX, targetY)) dirs.Add(new Vector2Int(0, -1));
            }
            else
            {
                bool rightBlocked = !IsWalkableForPathing(npcId, x + 1, y, targetX, targetY);
                bool leftBlocked = !IsWalkableForPathing(npcId, x - 1, y, targetX, targetY);
                if (rightBlocked && IsWalkableForPathing(npcId, x + 1, y + dirY, targetX, targetY)) dirs.Add(new Vector2Int(1, 0));
                if (leftBlocked && IsWalkableForPathing(npcId, x - 1, y + dirY, targetX, targetY)) dirs.Add(new Vector2Int(-1, 0));
            }
            return dirs;
        }

        private bool TryJumpStraight(int npcId, Vector2Int searchOrigin, Vector2Int target, int x, int y, int dirX, int dirY, int radiusLimit, int jumpLimit, out Vector2Int jumpPoint, out int stepCost)
        {
            jumpPoint = default;
            stepCost = 0;
            int curX = x;
            int curY = y;
            for (int dist = 1; dist <= jumpLimit; dist++)
            {
                curX += dirX; curY += dirY;
                if (!IsWalkableForPathing(npcId, curX, curY, target.x, target.y))
                    return false;
                if (Mathf.Abs(curX - searchOrigin.x) + Mathf.Abs(curY - searchOrigin.y) > radiusLimit)
                    return false;
                stepCost++;
                if (curX == target.x && curY == target.y)
                {
                    jumpPoint = new Vector2Int(curX, curY);
                    return true;
                }
                if (HasForcedNeighbourAt(npcId, curX, curY, dirX, dirY, target.x, target.y))
                {
                    jumpPoint = new Vector2Int(curX, curY);
                    return true;
                }
            }
            return false;
        }

        private bool HasForcedNeighbourAt(int npcId, int x, int y, int dirX, int dirY, int targetX, int targetY)
        {
            if (dirX != 0)
            {
                bool upBlocked = !IsWalkableForPathing(npcId, x, y + 1, targetX, targetY);
                bool downBlocked = !IsWalkableForPathing(npcId, x, y - 1, targetX, targetY);
                if (upBlocked && IsWalkableForPathing(npcId, x + dirX, y + 1, targetX, targetY)) return true;
                if (downBlocked && IsWalkableForPathing(npcId, x + dirX, y - 1, targetX, targetY)) return true;
                return false;
            }
            if (dirY != 0)
            {
                bool rightBlocked = !IsWalkableForPathing(npcId, x + 1, y, targetX, targetY);
                bool leftBlocked = !IsWalkableForPathing(npcId, x - 1, y, targetX, targetY);
                if (rightBlocked && IsWalkableForPathing(npcId, x + 1, y + dirY, targetX, targetY)) return true;
                if (leftBlocked && IsWalkableForPathing(npcId, x - 1, y + dirY, targetX, targetY)) return true;
            }
            return false;
        }

        private static void ExpandStraightSegment(List<Vector2Int> outCells, int ax, int ay, int bx, int by)
        {
            int dx = Math.Sign(bx - ax);
            int dy = Math.Sign(by - ay);
            int x = ax;
            int y = ay;
            while (x != bx || y != by)
            {
                x += dx;
                y += dy;
                outCells.Add(new Vector2Int(x, y));
            }
        }

        private readonly struct JpsStateKey
        {
            public readonly int X; public readonly int Y; public readonly int DirX; public readonly int DirY;
            public JpsStateKey(int x, int y, int dirX, int dirY) { X = x; Y = y; DirX = dirX; DirY = dirY; }
        }

        private readonly struct JpsOpenNode
        {
            public readonly int X; public readonly int Y; public readonly int DirX; public readonly int DirY; public readonly int G; public readonly float F; public readonly float H;
            public JpsOpenNode(int x, int y, int dirX, int dirY, int g, float f, float h = 0f) { X = x; Y = y; DirX = dirX; DirY = dirY; G = g; F = f; H = h; }
        }

        private readonly struct JpsParentInfo
        {
            public readonly JpsStateKey Parent; public readonly bool HasParent;
            public JpsParentInfo(JpsStateKey parent, bool hasParent) { Parent = parent; HasParent = hasParent; }
        }

        /// <summary>
        /// Predicato shared per path helper.

        ///
        /// Nota importante:
        /// - permettiamo di "entrare" nella cella target anche se è il target del path;
        /// - per tutte le altre celle manteniamo lo standard 1 NPC per cella.
        /// </summary>
        private bool IsWalkableForPathing(int npcId, int x, int y, int targetX, int targetY)
        {
            if (!InBounds(x, y))
                return false;
            if (IsMovementBlocked(x, y))
                return false;

            if (TryGetNpcAt(x, y, out int otherNpcId) && otherNpcId != npcId)
            {
                if (x != targetX || y != targetY)
                    return false;
            }

            return true;
        }

        // ============================================================
        // LOS helpers (Bresenham)
        // ============================================================

        /// <summary>
        /// LOS discreta (grid) con Bresenham.
        /// Regola: se una cella intermedia ha BlocksVision=true => LOS bloccata.
        /// Nota: non controlliamo la cella sorgente; controlliamo le celle "attraversate".
        /// </summary>
        public bool HasLineOfSight(int sx, int sy, int tx, int ty)
        {
            if (_occlusion == null || _occlusion.Length == 0)
            {
                // Se non hai InitMap, non possiamo fare LOS su cache: fallback "true"
                return true;
            }

            if (!InBounds(sx, sy) || !InBounds(tx, ty))
                return false;

            int x0 = sx, y0 = sy;
            int x1 = tx, y1 = ty;

            int dx = Mathf.Abs(x1 - x0);
            int dy = Mathf.Abs(y1 - y0);
            int sxStep = x0 < x1 ? 1 : -1;
            int syStep = y0 < y1 ? 1 : -1;
            int err = dx - dy;

            // percorriamo la linea; saltiamo la prima cella (sorgente),
            // e consideriamo blocking sulle celle intermedie e target (in genere anche il target puÃ² essere muro).
            bool first = true;

            while (true)
            {
                if (!first)
                {
                    if (BlocksVisionAt(x0, y0))
                        return false;
                }
                first = false;

                if (x0 == x1 && y0 == y1)
                    break;

                int e2 = 2 * err;
                if (e2 > -dy) { err -= dy; x0 += sxStep; }
                if (e2 < dx) { err += dx; y0 += syStep; }
            }

            return true;
        }

        // ============================================================
        // INTERNAL: apply def occlusion on create
        // ============================================================

        private void ApplyDefOcclusionToCell(int objId, string defId, int x, int y)
        {
            if (!InBounds(x, y)) return;

            if (!ObjectDefs.TryGetValue(defId, out var def) || def == null)
                return;

            bool blocksVision = def.BlocksVision;
            bool blocksMove = def.BlocksMovement;

            // Se Ã¨ marcato come occluder, ma non ha flags specifiche, default ?blocca tutto?
            if (def.IsOccluder)
            {
                if (!blocksVision && !blocksMove)
                {
                    blocksVision = true;
                    blocksMove = true;
                }
            }

            int idx = CellIndex(x, y);
            _blocksVision[idx] = blocksVision;
            _blocksMovement[idx] = blocksMove;

            // Se Ã¨ un occluder ?vero?, registriamo anche il componente runtime per query dettagliate.
            if (def.IsOccluder || blocksVision || blocksMove)
            {
                ObjectOccluders[objId] = new Occluder
                {
                    BlocksVision = blocksVision,
                    BlocksMovement = blocksMove,
                    VisionCost = def.VisionCost <= 0f ? 1f : def.VisionCost
                };
            }
        }
    }

    /// <summary>
    /// Stato globale del mondo.
    /// </summary>
    public struct GlobalState
    {
        // --- Memory Spatial Fusion ---
        public bool EnableMemorySpatialFusion;
        public int MemoryRegionSizeCells;

        // --- Tokens ---
        public int MaxTokensPerEncounter;
        public int MaxTokensPerNpcPerDay;
        public int RepeatShareCooldownTicks;

        public int TokenDeliveryMaxRangeCells;
        public bool EnableTokenLOS;
        public float TokenReliabilityFalloffPerCell;
        public float TokenIntensityFalloffPerCell;

        // --- Perception base ---
        public int NpcOperationalRangeCells;
        public int NpcVisionRangeCells;
        public bool NpcVisionUseCone;
        public float NpcVisionConeSlope; // half-width per forward step (grid cone)
        public float NpcVisionConeHalfWidthPerStep; // legacy/back-compat (se lo stai usando altrove)

        // --- Needs config ---
        public NeedsConfig Needs;

        // --- Object memory config ---
        public int NpcObjectMemorySlots;       // slot per memoria oggetti interagibili (per NPC)
        public int ObjectMemoryMaxAgeTicks;    // TTL in tick (pulizia)


        // --- Landmark pathfinding (v0.02) ---
        // Day1: solo flag + parametri; la logica viene implementata nei giorni successivi.
        public bool EnableLandmarkSystem;
        public int MaxLandmarksPerNpc;
        public int MaxEdgesPerNpc;
        public int MaxPoiAnchorsPerNpc;
        public int MaxWorldLandmarks;

        // Day3: eviction params (NPC-side landmark memory)
        // Nota: li teniamo qui per evitare che i System leggano direttamente Config/SIM ogni tick.
        // Sono parametri di policy, quindi hanno senso come "global tunables" data-driven.
        public int LandmarkEvictionStaleTicks;
        public int LandmarkEvictionCooldownTicks;

        // --- Inventory / Carry capacity ---
        //
        // NOTA:
        // - "InventoryMaxUnits" Ã¨ la capienza massima (in unitÃ ) dell'inventario di un NPC.
        // - Ã un parametro di configurazione letto da game_params.json (SimulationParams.inventory.inventory_max_units).
        // - La simulazione deve usare UNA query centrale (World.GetInventoryFreeCapacity)
        //   per evitare logiche divergenti tra comandi/sistemi.
        public int InventoryMaxUnits;

        // Tick corrente (se lo vuoi accessibile anche qui; altrimenti usa TickContext)
        public long CurrentTickIndex;
    }

    public struct GridPosition
    {
        public int X;
        public int Y;

        public GridPosition(int x, int y)
        {
            X = x;
            Y = y;
        }
    }
}

// Classe che contiene i parametri del simulatore letti da game_params.json
public sealed class WorldConfig
{
    public Arcontio.Core.Config.SimulationParams Sim { get; }

    public WorldConfig(Arcontio.Core.Config.SimulationParams sim)
    {
        Sim = sim;
    }
}