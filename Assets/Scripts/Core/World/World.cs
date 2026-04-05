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
    // =============================================================================
    // World.cs — Patch 0.02.5A (commenti verbosi)
    // =============================================================================
    /// <summary>
    /// <b>World</b> — contenitore centrale dello stato simulativo di Arcontio.
    ///
    /// <para>
    /// Il World è il "source of truth" di tutti i fatti oggettivi della simulazione:
    /// posizioni, componenti NPC, oggetti, occlusione, landmark, config globale.
    /// NON contiene AI, logica di decisione o logica di rendering.
    /// </para>
    ///
    /// <para><b>Principio architetturale fondamentale (Manifesto Arcontio):</b></para>
    /// <para>
    /// Il World contiene FATTI OGGETTIVI. La conoscenza soggettiva degli NPC
    /// (cosa sanno, cosa hanno visto, cosa ricordano) sta nei component store
    /// per-NPC (Memory, NpcObjectMemory, NpcLandmarkMemory, NpcPinnedFoodStockBeliefs).
    /// Questa distinzione è NON negoziabile: viola il manifesto qualsiasi
    /// Rule o System che usa World direttamente per decisioni che dovrebbero
    /// basarsi sulla percezione dell'NPC.
    /// </para>
    ///
    /// <para><b>Struttura interna:</b></para>
    /// <list type="bullet">
    ///   <item><b>GlobalState</b> — config runtime (vision range, token params, ecc.)</item>
    ///   <item><b>Component stores NPC</b> — NpcCore, Needs, Social, GridPos, NpcFacing, ecc.</item>
    ///   <item><b>Component stores oggetti</b> — Objects, FoodStocks, ObjectUse, ecc.</item>
    ///   <item><b>Memoria NPC</b> — Memory, NpcObjectMemory, NpcLandmarkMemory</item>
    ///   <item><b>Cache derivate</b> — OcclusionMap, _objIdByCell, _blocksVision/Movement</item>
    ///   <item><b>Pathfinding state</b> — delegato a <see cref="PathfindingState"/></item>
    ///   <item><b>Debug/telemetry</b> — DebugFovTelemetry, DebugNpcTokens (solo osservabilità)</item>
    /// </list>
    ///
    /// <para><b>Occluder policy (Day10+):</b></para>
    /// <para>
    /// Gli occluder NON sono più una struttura separata: sono oggetti del mondo
    /// (<see cref="WorldObjectInstance"/>) con flag nella <see cref="ObjectDef"/>.
    /// La <see cref="OcclusionMap"/> è una CACHE derivata da World.Objects per
    /// query veloci di visione/movimento. Viene aggiornata incrementalmente
    /// quando si creano/distruggono oggetti con flag IsOccluder/BlocksVision/BlocksMovement.
    /// </para>
    ///
    /// <para><b>Standard 1 object per cell:</b></para>
    /// <para>
    /// Una cella può contenere al massimo 1 oggetto. L'indice <c>_objIdByCell</c>
    /// mantiene la mappatura cella→objectId in O(1). <c>CreateObject</c> rifiuta
    /// il piazzamento se la cella è già occupata.
    /// </para>
    ///
    /// <para><b>Patch 0.02.5A:</b> solo aggiornamento commenti.</para>
    /// </summary>
    public sealed class World
    {
        // =====================================================================
        // GLOBAL / CONFIG
        // =====================================================================
        // GlobalState contiene tutti i parametri di configurazione runtime
        // che influenzano il comportamento della simulazione: vision range,
        // token params, needs thresholds, landmark params, ecc.
        //
        // NOTA: GlobalState è una struct (valore), quindi viene copiata se
        // passata per valore. Usare sempre 'world.Global.X' per accedere.
        // =====================================================================

        /// <summary>
        /// Stato globale e parametri di configurazione runtime della simulazione.
        /// Letto da tutti i System e le Rule che hanno bisogno di parametri condivisi.
        /// </summary>
        public GlobalState Global;

        /// <summary>
        /// Larghezza della griglia in celle. Impostata da <c>InitMap</c>.
        /// Leggibile da game_params.json tramite <c>WorldConfig.Sim.worldWidth</c>.
        /// </summary>
        public int MapWidth { get; private set; }

        /// <summary>
        /// Altezza della griglia in celle. Impostata da <c>InitMap</c>.
        /// </summary>
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
        /// - Questa NON è logica di simulazione.
        /// - Ã solo un canale di osservabilitÃ  per la view/debug.
        /// </summary>
        public DebugFovTelemetry DebugFovTelemetry { get; private set; }

        /// <summary>
        /// LandmarkRegistry (v0.02 Day2): registro oggettivo dei landmark.
        ///
        /// Nota:
        /// - E' world-side (non per-NPC).
        /// - Viene costruito in bootstrap (SimulationHost) dopo il seeding della mappa.
        /// - La view può leggerlo in modo read-only tramite GetNpcLandmarkOverlayData.
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

        // =====================================================================
        // COMPONENT STORES (NPC)
        // =====================================================================
        // Ogni dizionario è un "component store": mappa npcId → componente.
        // Questo pattern è ispirato all'ECS (Entity Component System):
        // gli NPC non sono oggetti monolitici ma insiemi di componenti separati.
        //
        // ACCESSO: sempre tramite TryGetValue (difensivo) o tramite le API
        // di World (es. TryGetNpcPos, GetFacing) che gestiscono i fallback.
        //
        // CHIAVE: npcId (int), assegnato in sequenza da _nextNpcId (parte da 1).
        //         0 è riservato come "nessun NPC" / valore invalido.
        // =====================================================================

        /// <summary>Dati anagrafici e tratti di personalità dell'NPC (nome, carisma, ecc.).</summary>
        public readonly Dictionary<int, NpcCore> NpcCore = new();

        /// <summary>Bisogni primari dell'NPC: fame, fatica, morale.</summary>
        public readonly Dictionary<int, Needs> Needs = new();

        /// <summary>Stato sociale dell'NPC: leadership score, loyalty, justice perception.</summary>
        public readonly Dictionary<int, Social> Social = new();

        /// <summary>Posizione corrente sulla griglia (cella X, Y). Source of truth per il movimento.</summary>
        public readonly Dictionary<int, GridPosition> GridPos = new();

        /// <summary>Orientamento corrente dell'NPC (N/E/S/W). Influenza il cono visivo.</summary>
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

        /// <summary>
        /// Store per-NPC degli edge complessi imparati dalla navigazione fisica.
        /// Patch 0.02.09.A — chiave: npcId, valore: NpcComplexEdgeMemories.
        /// NOTA: il campo si chiama NpcComplexEdgeMemories (plurale) per evitare
        /// il conflitto di nome con la classe NpcComplexEdgeMemory.
        /// </summary>
        public readonly Dictionary<int, NpcComplexEdgeMemory> NpcComplexEdgeMemories
            = new Dictionary<int, NpcComplexEdgeMemory>(256);

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

        // ============================================================
        // PATHFINDING STATE (stato esecutivo pathfinding per-NPC)
        // ============================================================
        // Lo stato esecutivo del pathfinding (macro-route execution, direct commit,
        // local search, failure learning, debug path cella-per-cella) è stato
        // estratto in PathfindingState per alleggerire World.cs.
        //
        // Accedere sempre tramite world.Pathfinding.X() invece di world.X() direttamente.
        //
        // Vedi: Scripts/Core/World/PathfindingState.cs
        public PathfindingState Pathfinding { get; private set; }
        // =====================================================================
        // COMPONENT STORES (OBJECTS)
        // =====================================================================
        // Stesso pattern component store degli NPC, ma per gli oggetti del mondo.
        // Chiave: objectId (int), assegnato in sequenza da _nextObjectId (parte da 1).
        //
        // REGOLA "1 object per cell":
        // Una cella può contenere al massimo 1 oggetto. Questo è enforced da
        // CreateObject tramite l'indice _objIdByCell. Violarlo produce comportamenti
        // non definiti nella pipeline di percezione e occlusione.
        // =====================================================================

        /// <summary>
        /// Registro di tutti gli oggetti del mondo.
        /// Contiene istanza, posizione, defId, ownerKind/ownerId.
        /// </summary>
        public readonly Dictionary<int, WorldObjectInstance> Objects = new();

        /// <summary>
        /// Stato di utilizzo degli oggetti interagibili (letto libero/occupato, ecc.).
        /// Consultato da NeedsDecisionRule per decidere se un letto è disponibile.
        /// </summary>
        public readonly Dictionary<int, ObjectUseState> ObjectUse = new();

        /// <summary>
        /// Stock di cibo associato agli oggetti food_stock nel mondo.
        /// Un food_stock con Units = 0 è considerato esaurito.
        /// Scrivere qui direttamente è sconsigliato: usa <see cref="SetFoodStock"/>
        /// per mantenere coerenza con i pinned belief degli NPC.
        /// </summary>
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

        // =====================================================================
        // MOVEMENT / SCAN (intent + stato esecuzione)
        // =====================================================================
        // Il movimento in Arcontio è intenzionale (non fisico diretto):
        //   1) Una Rule/Decision scrive un MoveIntent nel World.
        //   2) Il MovementSystem esegue fisicamente il movimento tick per tick.
        //
        // Questo disaccoppiamento permette di:
        //   - gestire collisioni senza che le Rule debbano preoccuparsene
        //   - cancellare/modificare l'intento da fuori senza interrompere il System
        //   - osservare lo stato di movimento dalla View senza accedere al System
        //
        // Lo scan (IdleScanSystem) implementa "nessuna visione a 360° gratuita":
        // un NPC idle ruota periodicamente per coprire gli angoli ciechi.
        // =====================================================================

        /// <summary>
        /// Intento di movimento per NPC: verso quale cella si sta muovendo e perché.
        /// <list type="bullet">
        ///   <item>Scritto da Rule/Command (es. <c>SetMoveIntentCommand</c>).</item>
        ///   <item>Consumato da <c>MovementSystem</c> ogni tick.</item>
        ///   <item>Se <c>Active = false</c>: l'NPC è fermo (o ha raggiunto la destinazione).</item>
        /// </list>
        /// </summary>
        public readonly Dictionary<int, MoveIntent> NpcMoveIntents = new();

        /// <summary>
        /// Stato di scan direzionale per NPC.
        /// <list type="bullet">
        ///   <item>Uno scan = 4 rotazioni consecutive (una per tick).</item>
        ///   <item>Attivato da <c>IdleScanSystem</c> quando l'NPC è idle.</item>
        ///   <item>Implementa "nessuna visione a 360° gratuita" del manifesto.</item>
        /// </list>
        /// </summary>
        public readonly Dictionary<int, ScanState> NpcScanStates = new();


        // =====================================================================
        // INTERNAL GRID INDEXES / CACHE
        // =====================================================================
        // Queste strutture sono CACHE DERIVATE dallo stato degli oggetti.
        // Non sono fonti di verità: vengono ricostruite da RebuildDerivedCachesGlobal
        // e mantenute incrementalmente da CreateObject/DestroyObject.
        //
        // _objIdByCell: permette GetObjectAt(x,y) in O(1) invece di O(N).
        //               Valore -1 = cella vuota (sentinella).
        //
        // _blocksVision / _blocksMovement: cache booleana dell'occlusione.
        //   Derivata dai flag ObjectDef (IsOccluder, BlocksVision, BlocksMovement).
        //   Usata da HasLineOfSight e IsMovementBlocked (chiamate molto frequenti).
        //
        // IMPORTANTE: dopo qualsiasi modifica agli oggetti che influisce sull'occlusione,
        // queste cache DEVONO essere aggiornate. In DevMode usare RebuildDerivedCachesGlobal.
        // =====================================================================

        /// <summary>
        /// Indice cella → objectId. Dimensione: MapWidth * MapHeight.
        /// Valore -1 = cella vuota. Aggiornato da CreateObject/DestroyObject.
        /// </summary>
        private int[] _objIdByCell;

        /// <summary>
        /// Cache booleana: la cella blocca la visione? Derivata dagli ObjectDef.
        /// Usata da <c>HasLineOfSight</c> ogni tick per ogni NPC.
        /// </summary>
        private bool[] _blocksVision;

        /// <summary>
        /// Cache booleana: la cella blocca il movimento? Derivata dagli ObjectDef.
        /// Usata da <c>MovementSystem.TryMoveTo</c> ogni tick.
        /// </summary>
        private bool[] _blocksMovement;

        // =====================================================================
        // ID COUNTERS
        // =====================================================================
        // I counter sono incrementali e non riutilizzano ID liberati.
        // Questo garantisce che ogni NPC/oggetto abbia un ID univoco per tutta
        // la durata della simulazione, semplificando debug e log.
        //
        // NOTA: ID = 0 è riservato come sentinella "nessuno/invalido".
        // I counter partono da 1 per rispettare questa convenzione.
        // =====================================================================

        /// <summary>Prossimo ID NPC disponibile. Incrementato da <c>CreateNpc</c>.</summary>
        private int _nextNpcId = 1;

        /// <summary>Prossimo ID oggetto disponibile. Incrementato da <c>CreateObject</c>.</summary>
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
            // PATHFINDING STATE — inizializzazione
            // ============================================================
            // PathfindingState riceve la config per leggere i parametri di
            // local search (commitMinSteps, failureLearning, ecc.) senza
            // dipendere da singleton statici.
            Pathfinding = new PathfindingState(config);

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

        public int CellIndex(int x, int y) => (y * MapWidth) + x;

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
        /// Crea (se mancante) lo store degli edge complessi per un NPC.
        /// Patch 0.02.09.A.
        /// </summary>
        private NpcComplexEdgeMemory EnsureNpcComplexEdgeMemory(int npcId)
        {
            if (!NpcComplexEdgeMemories.TryGetValue(npcId, out var mem) || mem == null)
            {
                int maxEdges = Global.MaxEdgesPerNpc > 0 ? Global.MaxEdgesPerNpc : 128;
                mem = new NpcComplexEdgeMemory(maxEdges);
                NpcComplexEdgeMemories[npcId] = mem;
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
            {
                // Patch 0.02.09.A: accumula il passo nel recording attivo se presente.
                if (NpcComplexEdgeMemories.TryGetValue(npcId, out var recMem) && recMem != null)
                    recMem.RecordStep(toX, toY);
                return;
            }

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
                    // Edge semplice nel registry: apprendi normalmente.
                    mem.LearnEdge(prev, nodeId, costCells, now, evictionCooldownTicks: Global.LandmarkEvictionCooldownTicks);
                }
                else
                {
                    // Patch 0.02.09.A: edge NON nel registry → tenta di produrre
                    // un edge complesso soggettivo dal recording accumulato.
                    var complexMem = EnsureNpcComplexEdgeMemory(npcId);
                    complexMem.TryCompleteRecording(nodeId, now, out _);
                }
                // In ogni caso: avvia un nuovo recording dal nodo corrente.
                // Patch 0.02.09.A: anche dopo un edge semplice, partiamo a registrare
                // il prossimo tratto (potrebbe non avere un edge nel registry).
                EnsureNpcComplexEdgeMemory(npcId).StartPathRecording(nodeId, toX, toY);
            }
            else if (prev == 0)
            {
                // Primo landmark visto: avvia il recording.
                // Patch 0.02.09.A
                EnsureNpcComplexEdgeMemory(npcId).StartPathRecording(nodeId, toX, toY);
            }

            // Aggiorniamo sempre l'ultimo landmark visitato quando ne vediamo uno.
            mem.LastVisitedLandmarkId = nodeId;
            mem.LastVisitedLandmarkTick = now;

            // Day5: se l'NPC sta eseguendo una macro-route, l'ingresso su un landmark
            // puo' far avanzare l'indice del prossimo checkpoint.
            TryAdvanceMacroRouteExecutionAtCell(npcId, toX, toY);
        }



        /// <summary>
        /// NotifyNpcSeenLandmark (v0.03.03.a — Landmark Perception):
        /// apprendimento visivo — l'NPC ha visto un landmark nel FOV senza calpestarci sopra.
        ///
        /// Contratto: viene chiamato da LandmarkPerceptionSystem dopo Range + Cone + LOS.
        ///
        /// Differenze rispetto a NotifyNpcMovedForLandmarkLearning:
        /// - impara il nodo E gli edge del registry adiacenti (vedi nota sotto)
        /// - non aggiorna LastVisitedLandmarkId (riservato all'apprendimento fisico)
        /// - non fa avanzare la macro-route (dipende dal movimento fisico)
        ///
        /// Nota sugli edge:
        /// Vedere un landmark (porta, junction) include leggere la geometria circostante:
        /// l'NPC percepisce visivamente quali corridoi/stanze la struttura connette.
        /// Gli edge vengono appresi SOLO se esistono nel registry oggettivo
        /// (stessa regola di NotifyNpcMovedForLandmarkLearning: nessun edge "fantasma").
        /// Senza questo apprendimento, l'A* della macro-route non ha archi da attraversare
        /// e il grafo soggettivo rimane disconnesso → fallback permanente a GOAL_LOCAL_SEARCH.
        /// </summary>
        public void NotifyNpcSeenLandmark(int npcId, int nodeId)
        {
            // Se il sistema landmarks è disabilitato, non impariamo nulla.
            if (!Global.EnableLandmarkSystem)
                return;

            if (LandmarkRegistry == null)
                return;

            // Se l'NPC non esiste, abort.
            if (!NpcCore.ContainsKey(npcId))
                return;

            long now = TickContext.CurrentTickIndex;
            var mem = EnsureNpcLandmarkMemory(npcId);

            // Impara il nodo (anti-thrashing gestito dentro NpcLandmarkMemory).
            mem.LearnLandmark(nodeId, now, evictionCooldownTicks: Global.LandmarkEvictionCooldownTicks);

            // Impara gli edge del registry adiacenti al nodo visto.
            // Itera gli edge globali: gli edge sono pochi (adiacenza sparsa, cap maxEdgesPerLandmark),
            // quindi O(E) è accettabile ogni tick per questo nodo.
            var edges = LandmarkRegistry.Edges;
            for (int i = 0; i < edges.Count; i++)
            {
                var e = edges[i];
                if (e == null || !e.IsActive) continue;

                int otherNodeId = 0;
                if      (e.FromNodeId == nodeId) otherNodeId = e.ToNodeId;
                else if (e.ToNodeId   == nodeId) otherNodeId = e.FromNodeId;
                else continue;

                // Apprendi l'edge solo se l'NPC conosce anche l'altro endpoint.
                // Un NPC non può usare un corridoio di cui non sa dove porta.
                if (!mem.ContainsLandmark(otherNodeId))
                    continue;

                mem.LearnEdge(nodeId, otherNodeId, e.CostCells, now,
                    evictionCooldownTicks: Global.LandmarkEvictionCooldownTicks);
            }
        }

        /// <summary>
        /// NotifyNpcSeenLandmarkPair (v0.03.04.c-ComplexEdge_Creation):
        /// crea un edge soggettivo tra due nodi landmark inferiti dalla percezione visiva.
        ///
        /// <para>
        /// Chiamato da <c>LandmarkPerceptionSystem</c> con due meccanismi distinti:
        /// </para>
        /// <list type="number">
        ///   <item>
        ///     <b>Simultaneità visiva</b>: nodi A e B visibili nello stesso tick →
        ///     costo = Manhattan(A, B).
        ///   </item>
        ///   <item>
        ///     <b>Ibrido fisico+visivo</b>: recording fisico attivo da nodo A +
        ///     nodo B visibile nel FOV → costo = StepCount + Manhattan(npc_pos, B).
        ///   </item>
        /// </list>
        ///
        /// <para>
        /// L'edge viene appreso in <c>NpcLandmarkMemory</c> con la confidence visiva
        /// (<paramref name="confidence"/>), inferiore a quella degli edge fisici (0.25f).
        /// Se l'edge esiste già (es. percorso fisicamente), viene solo rinforzato (+0.10f)
        /// senza abbassare la confidence già acquisita.
        /// </para>
        /// </summary>
        /// <param name="npcId">NPC che inferisce l'edge.</param>
        /// <param name="nodeA">Primo nodo dell'edge (non orientato).</param>
        /// <param name="nodeB">Secondo nodo dell'edge (non orientato).</param>
        /// <param name="costCells">Costo stimato in celle.</param>
        /// <param name="confidence">Confidence iniziale (solo per edge nuovi).</param>
        public void NotifyNpcSeenLandmarkPair(int npcId, int nodeA, int nodeB, int costCells, float confidence)
        {
            if (!Global.EnableLandmarkSystem) return;
            if (LandmarkRegistry == null)     return;
            if (!NpcCore.ContainsKey(npcId))  return;
            if (nodeA == 0 || nodeB == 0 || nodeA == nodeB) return;
            if (costCells < 1) costCells = 1;

            long now = TickContext.CurrentTickIndex;
            var mem = EnsureNpcLandmarkMemory(npcId);

            // Entrambi gli endpoint devono essere già noti all'NPC.
            // Un NPC non può costruire un percorso verso un nodo di cui non sa l'esistenza.
            if (!mem.ContainsLandmark(nodeA) || !mem.ContainsLandmark(nodeB))
                return;

            mem.LearnEdge(nodeA, nodeB, costCells, now,
                evictionCooldownTicks: Global.LandmarkEvictionCooldownTicks,
                initialConfidence: confidence);
        }

        /// <summary>
        /// NotifyNpcSeenLandmarkPairComplex (v0.03.04.c — Meccanismo 2):
        /// crea un <see cref="ComplexEdge"/> visivo in <see cref="NpcComplexEdgeMemories"/>
        /// tra il nodo fisicamente calpestato (nodeA = ultimo da recording) e un nodo
        /// visto nel FOV (nodeB). A differenza di <see cref="NotifyNpcSeenLandmarkPair"/>
        /// (che crea edge semplici in NpcLandmarkMemory), questo percorso entra nel
        /// layer giallo dell'overlay e verrà confermato o sostituito quando l'NPC
        /// percorre fisicamente il tratto.
        /// </summary>
        public void NotifyNpcSeenLandmarkPairComplex(int npcId, int nodeA, int nodeB, int costCells, float confidence)
        {
            if (!Global.EnableLandmarkSystem) return;
            if (LandmarkRegistry == null)     return;
            if (!NpcCore.ContainsKey(npcId))  return;
            if (nodeA == 0 || nodeB == 0 || nodeA == nodeB) return;
            if (costCells < 1) costCells = 1;

            // Entrambi i nodi devono essere già noti all'NPC (stessa regola di NotifyNpcSeenLandmarkPair).
            if (!NpcLandmarkMemory.TryGetValue(npcId, out var lmMem)) return;
            if (!lmMem.ContainsLandmark(nodeA) || !lmMem.ContainsLandmark(nodeB)) return;

            long now = TickContext.CurrentTickIndex;
            var complexMem = EnsureNpcComplexEdgeMemory(npcId);
            complexMem.LearnVisualEdge(nodeA, nodeB, costCells, now, confidence);
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

        /// <summary>
        /// Avvia l'esecuzione della macro-route per un NPC verso la cella target.
        /// Pianifica (o ripianifica) la route, poi delega l'inizializzazione
        /// dello stato esecutivo a <see cref="PathfindingState.BeginMacroRouteExecution"/>.
        /// </summary>
        public void BeginMacroRouteExecutionForNpc(int npcId, int targetX, int targetY)
        {
            if (!ExistsNpc(npcId))
                return;

            // Pianifica (o aggiorna) la macro-route nel registro NpcMacroRoutes.
            RebuildDebugMacroRouteForNpc(npcId, targetX, targetY);

            // Ottieni la posizione corrente dell'NPC per decidere se saltare il primo nodo.
            GridPos.TryGetValue(npcId, out var pos);

            // Delega l'inizializzazione dello stato esecutivo a PathfindingState.
            Pathfinding.BeginMacroRouteExecution(npcId, targetX, targetY, pos, NpcMacroRoutes, LandmarkRegistry);
        }

        /// <summary>
        /// Cancella la macro-route pianificata e il suo stato esecutivo per un NPC.
        /// Delega a <see cref="PathfindingState.ClearMacroRoute"/>.
        /// </summary>
        public void ClearDebugMacroRouteForNpc(int npcId)
        {
            Pathfinding.ClearMacroRoute(npcId, NpcMacroRoutes);
        }

        /// <summary>
        /// Avanza l'esecuzione della macro-route quando l'NPC raggiunge la cella indicata.
        /// Delega la logica a <see cref="PathfindingState.TryAdvanceMacroRouteAtCell"/>.
        /// </summary>
        public bool TryAdvanceMacroRouteExecutionAtCell(int npcId, int cellX, int cellY)
        {
            return Pathfinding.TryAdvanceMacroRouteAtCell(npcId, cellX, cellY, NpcMacroRoutes, LandmarkRegistry);
        }

        /// <summary>
        /// Restituisce il target immediato della macro-route in esecuzione.
        /// Delega a <see cref="PathfindingState.TryGetMacroExecutionImmediateTarget"/>.
        /// </summary>
        public bool TryGetMacroExecutionImmediateTarget(int npcId, out int targetX, out int targetY, out bool isLastMile, out int nextNodeId)
        {
            return Pathfinding.TryGetMacroExecutionImmediateTarget(npcId, out targetX, out targetY, out isLastMile, out nextNodeId, NpcMacroRoutes);
        }

        /// <summary>
        /// Marca l'esecuzione della macro-route come bloccata.
        /// Delega a <see cref="PathfindingState.MarkMacroRouteBlocked"/>.
        /// </summary>
        public void MarkMacroRouteExecutionBlocked(int npcId, bool duringLastMile)
        {
            Pathfinding.MarkMacroRouteBlocked(npcId, duringLastMile);
        }

        /// <summary>
        /// Produce il report di debug della navigazione per un NPC (usato dalla card UI overlay).
        /// Aggrega lo stato da: MoveIntent, MacroRouteExecution, DirectCommit, GoalLocalSearch,
        /// debug path cells — tutti gestiti da <see cref="PathfindingState"/>.
        /// </summary>
        public bool TryGetNpcMacroRouteDebugReport(int npcId, out NpcMacroRouteDebugReport report)
        {
            // Recupera i dati dal piano macro-route (World-side).
            bool hasPlan       = NpcMacroRoutes.TryGetValue(npcId, out var plan) && plan != null;
            bool hasMoveIntent = NpcMoveIntents.TryGetValue(npcId, out var intent) && intent.Active;

            // Recupera gli stati esecutivi da PathfindingState.
            bool hasMacroState  = Pathfinding.MacroRouteExecution.TryGetValue(npcId, out var state)      && state != null;
            bool hasDirectState = Pathfinding.DirectCommitExecution.TryGetValue(npcId, out var directState) && directState != null;
            bool hasLocalState  = Pathfinding.GoalLocalSearchExecution.TryGetValue(npcId, out var localState) && localState != null;

            // Se non c'è nulla di attivo, non c'è report.
            if (!hasPlan && !hasMacroState && !hasDirectState && !hasLocalState && !hasMoveIntent)
            {
                report = default;
                return false;
            }

            bool   execActive                  = false;
            bool   isLastMile                  = false;
            int    nextIndex                   = -1;
            int    nextNodeId                  = 0;
            int    routeNodeCount              = hasPlan ? plan.NodeIds.Count : 0;
            int    startNodeId                 = hasPlan ? plan.StartNodeId  : 0;
            int    targetNodeId                = hasPlan ? plan.TargetNodeId : 0;
            int    targetCellX                 = hasPlan ? plan.TargetCellX  : 0;
            int    targetCellY                 = hasPlan ? plan.TargetCellY  : 0;
            string failureReason               = hasPlan ? (plan.FailureReason ?? string.Empty) : string.Empty;
            int    immediateX                  = targetCellX;
            int    immediateY                  = targetCellY;
            string execFail                    = string.Empty;
            string navigationMode              = "IDLE";
            int    lastModeSwitchTick          = -1;
            string lastModeSwitchReason        = string.Empty;
            bool   goalLocalSearchActive       = false;
            int    goalLocalSearchBudgetRemaining = 0;

            // MoveIntent: fonte di verità del target se attivo.
            if (hasMoveIntent)
            {
                targetCellX = intent.TargetX;
                targetCellY = intent.TargetY;
                immediateX  = intent.TargetX;
                immediateY  = intent.TargetY;
                execActive  = true;
            }

            // MacroRoute execution state.
            if (hasMacroState)
            {
                execActive         = execActive || state.Active;
                isLastMile         = state.IsDoingLastMile;
                nextIndex          = state.NextRouteNodeIndex;
                immediateX         = state.ImmediateTargetX;
                immediateY         = state.ImmediateTargetY;
                execFail           = state.FailureReason ?? string.Empty;
                navigationMode     = !string.IsNullOrEmpty(state.NavigationMode)
                                         ? state.NavigationMode
                                         : (state.Active ? "LM_PATH" : navigationMode);
                lastModeSwitchTick   = state.LastModeSwitchTick;
                lastModeSwitchReason = state.LastModeSwitchReason ?? string.Empty;

                if (!isLastMile && hasPlan && nextIndex >= 0 && nextIndex < plan.NodeIds.Count)
                    nextNodeId = plan.NodeIds[nextIndex];

                if (targetCellX == 0 && targetCellY == 0)
                {
                    targetCellX = state.FinalTargetCellX;
                    targetCellY = state.FinalTargetCellY;
                }
            }

            // Direct commit state (priorità su macro-route per il target immediato).
            if (hasDirectState && directState.Active)
            {
                execActive    = true;
                immediateX    = directState.ImmediateTargetX;
                immediateY    = directState.ImmediateTargetY;
                targetCellX   = directState.FinalTargetCellX;
                targetCellY   = directState.FinalTargetCellY;
                navigationMode = "DIRECT_COMMIT";
                if (string.IsNullOrEmpty(lastModeSwitchReason))
                    lastModeSwitchReason = "DirectCommitActive";
            }

            // Goal local search state (priorità massima sul target immediato).
            if (hasLocalState && localState.Active)
            {
                execActive                    = true;
                goalLocalSearchActive         = true;
                goalLocalSearchBudgetRemaining = localState.BudgetRemaining;
                immediateX    = localState.ImmediateTargetX;
                immediateY    = localState.ImmediateTargetY;
                targetCellX   = localState.FinalTargetCellX;
                targetCellY   = localState.FinalTargetCellY;
                navigationMode = "GOAL_LOCAL_SEARCH";
                if (string.IsNullOrEmpty(lastModeSwitchReason))
                    lastModeSwitchReason = "LocalSearchActive";
            }

            // Fallback navigationMode dai debug path se nessuno stato è esplicitamente attivo.
            if (execActive && navigationMode == "IDLE")
            {
                if (Pathfinding.DebugJumpPathCells.TryGetValue(npcId, out var jumpDbg) && jumpDbg != null && jumpDbg.Count >= 2)
                    navigationMode = "GOAL_LOCAL_SEARCH";
                else if (Pathfinding.DebugDirectPathCells.TryGetValue(npcId, out var directDbg) && directDbg != null && directDbg.Count >= 2)
                    navigationMode = "DIRECT_COMMIT";
                else if (hasPlan)
                    navigationMode = "LM_PATH";
            }

            report = new NpcMacroRouteDebugReport(
                hasRoute:                      hasPlan && plan.Succeeded,
                startNodeId:                   startNodeId,
                targetNodeId:                  targetNodeId,
                routeNodeCount:                routeNodeCount,
                targetCellX:                   targetCellX,
                targetCellY:                   targetCellY,
                failureReason:                 failureReason,
                executionActive:               execActive,
                isDoingLastMile:               isLastMile,
                nextRouteNodeIndex:            nextIndex,
                nextRouteNodeId:               nextNodeId,
                immediateTargetX:              immediateX,
                immediateTargetY:              immediateY,
                executionFailureReason:        execFail,
                navigationMode:                navigationMode,
                lastModeSwitchTick:            lastModeSwitchTick,
                lastModeSwitchReason:          lastModeSwitchReason,
                goalLocalSearchActive:         goalLocalSearchActive,
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
            // Nota (v0.03.02.a): microTestDummyGraph rimosso — scaffolding Day1 non più necessario.
            // Il grafo landmark reale è disponibile dal Day2.

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
    System.Collections.Generic.List<LandmarkOverlayEdge> outComplexEdges,
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
            if (outComplexEdges != null) outComplexEdges.Clear();

            if (!TryGetNpcMacroRouteDebugReport(npcId, out routeReport))
                routeReport = default;

            if (!NpcCore.ContainsKey(npcId))
                return;

            // Nota (v0.03.02.a): microTestDummyGraph rimosso — scaffolding Day1 non più necessario.

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

            // ============================================================
            // COMPLEX EDGES (v0.03.04.c-ComplexEdge_Creation)
            // Edge soggettivi fisicamente percorsi dall'NPC, non nel registry globale.
            // Visualizzati in giallo come percorso reale su griglia (scalini cardinali),
            // non come linea retta tra i due endpoint.
            // ============================================================
            if (outComplexEdges != null && LandmarkRegistry != null
                && NpcComplexEdgeMemories.TryGetValue(npcId, out var complexMem) && complexMem != null)
            {
                foreach (var kv in complexMem.Edges)
                {
                    var ce = kv.Value;
                    if (!LandmarkRegistry.TryGetActiveNodeById(ce.Key.A, out var nA) || nA == null) continue;
                    if (!LandmarkRegistry.TryGetActiveNodeById(ce.Key.B, out var nB) || nB == null) continue;

                    if (ce.Segments == null || ce.Segments.Count == 0)
                    {
                        // Nessun segmento: fallback linea retta tra i due nodi.
                        outComplexEdges.Add(new LandmarkOverlayEdge(nA.CellX, nA.CellY, nB.CellX, nB.CellY, ce.Confidence));
                        continue;
                    }

                    // Determina il nodo di partenza: prova da Key.A camminando i segmenti;
                    // se l'endpoint finale è lontano da Key.B, il path fu registrato in senso
                    // inverso (B→A) → parti da Key.B.
                    int startX = nA.CellX, startY = nA.CellY;
                    {
                        int cx2 = startX, cy2 = startY;
                        for (int s = 0; s < ce.Segments.Count; s++)
                            ApplySegment(ce.Segments[s], ref cx2, ref cy2);
                        int distToB = System.Math.Abs(cx2 - nB.CellX) + System.Math.Abs(cy2 - nB.CellY);
                        if (distToB > 3) { startX = nB.CellX; startY = nB.CellY; }
                    }

                    // Emette un LandmarkOverlayEdge per ogni tratto cardinale.
                    // cx/cy seguono il cursore cella per cella tra i waypoint di svolta.
                    int cx = startX, cy = startY;
                    for (int s = 0; s < ce.Segments.Count; s++)
                    {
                        int nx = cx, ny = cy;
                        ApplySegment(ce.Segments[s], ref nx, ref ny);
                        outComplexEdges.Add(new LandmarkOverlayEdge(cx, cy, nx, ny, ce.Confidence));
                        cx = nx; cy = ny;
                    }
                }
            }
        }

        /// <summary>
        /// Applica un PathSegment al cursore (cx, cy) modificandolo in-place.
        /// </summary>
        private static void ApplySegment(PathSegment seg, ref int cx, ref int cy)
        {
            switch (seg.Direction)
            {
                case CardinalDirection.North: cy += seg.Length; break;
                case CardinalDirection.East:  cx += seg.Length; break;
                case CardinalDirection.South: cy -= seg.Length; break;
                case CardinalDirection.West:  cx -= seg.Length; break;
            }
        }

        /// <summary>
        /// Riempie gli edge di overlay cella-per-cella per i tre layer di navigazione.
        /// Delega a <see cref="PathfindingState.FillDebugNavigationPathOverlayData"/>.
        /// </summary>
        private void FillDebugNavigationPathOverlayData(
            int npcId,
            System.Collections.Generic.List<LandmarkOverlayEdge> outLmPathEdges,
            System.Collections.Generic.List<LandmarkOverlayEdge> outDirectPathEdges,
            System.Collections.Generic.List<LandmarkOverlayEdge> outJumpPathEdges)
        {
            Pathfinding.FillDebugNavigationPathOverlayData(npcId, outLmPathEdges, outDirectPathEdges, outJumpPathEdges);
        }

        /// <summary>
        /// Cancella tutti i debug path e gli stati DirectCommit/GoalLocalSearch per un NPC.
        /// Delega a <see cref="PathfindingState.ClearDebugNavigationPaths"/>.
        /// </summary>
        public void ClearDebugNavigationPathsForNpc(int npcId)
        {
            Pathfinding.ClearDebugNavigationPaths(npcId);
        }

        /// <summary>
        /// Imposta il path diretto (DIRECT_COMMIT) per un NPC.
        /// Delega a <see cref="PathfindingState.SetDebugDirectPath"/>.
        /// </summary>
        public void SetDebugDirectPathForNpc(int npcId, System.Collections.Generic.List<Vector2Int> path)
        {
            Pathfinding.SetDebugDirectPath(npcId, path);
        }

        /// <summary>
        /// Imposta il path di local search / JPS (GOAL_LOCAL_SEARCH) per un NPC.
        /// Delega a <see cref="PathfindingState.SetDebugJumpPath"/>.
        /// </summary>
        public void SetDebugJumpPathForNpc(int npcId, System.Collections.Generic.List<Vector2Int> path, int budgetRemaining)
        {
            Pathfinding.SetDebugJumpPath(npcId, path, budgetRemaining);
        }

        /// <summary>
        /// Aggiunge uno step al debug path LM (verde) per un NPC.
        /// Delega a <see cref="PathfindingState.AppendDebugLmStep"/>.
        /// </summary>
        public void AppendDebugLmStepForNpc(int npcId, int fromX, int fromY, int toX, int toY)
        {
            Pathfinding.AppendDebugLmStep(npcId, fromX, fromY, toX, toY);
        }

        /// <summary>
        /// Stub per compatibilità — il path direct viene impostato per intero in SetDebugDirectPathForNpc.
        /// </summary>
        public void AppendDebugDirectStepForNpc(int npcId, int fromX, int fromY, int toX, int toY)
        {
            Pathfinding.AppendDebugDirectStep(npcId, fromX, fromY, toX, toY);
        }

        /// <summary>
        /// Stub per compatibilità — il path jump viene impostato per intero in SetDebugJumpPathForNpc.
        /// </summary>
        public void AppendDebugJumpStepForNpc(int npcId, int fromX, int fromY, int toX, int toY)
        {
            Pathfinding.AppendDebugJumpStep(npcId, fromX, fromY, toX, toY);
        }

        // ============================================================
        // FAILURE LEARNING / CLEAR STATE — deleghe a PathfindingState
        // ============================================================
        // Questi metodi sono stati estratti in PathfindingState.
        // World espone solo wrapper pubblici per compatibilità con il codice
        // che li chiama tramite world.X(...).

        /// <summary>
        /// Cancella il failure learning della local search per un NPC.
        /// Delega a <see cref="PathfindingState.ClearLocalSearchFailureLearning"/>.
        /// </summary>
        public void ClearNpcLocalSearchFailureLearning(int npcId)
        {
            Pathfinding.ClearLocalSearchFailureLearning(npcId);
        }

        /// <summary>
        /// Azzera lo stato della local search per un NPC.
        /// Delega a <see cref="PathfindingState.ClearLocalSearchState"/>.
        /// </summary>
        public void ClearNpcLocalSearchState(int npcId, string failureReason = "")
        {
            Pathfinding.ClearLocalSearchState(npcId, failureReason);
        }

        /// <summary>
        /// Azzera lo stato del direct commit per un NPC.
        /// Delega a <see cref="PathfindingState.ClearDirectCommitState"/>.
        /// </summary>
        public void ClearNpcDirectCommitState(int npcId, string failureReason = "")
        {
            Pathfinding.ClearDirectCommitState(npcId, failureReason);
        }

        // ============================================================
        // GVD-DIN OVERLAY DATA (v0.03)
        // ============================================================

        /// <summary>
        /// Popola il GvdDinOverlaySnapshot per il debug overlay.
        ///
        /// Patch 0.03.02.a.3:
        /// Il metodo ora passa anche quando hybrid_landmark.use_hybrid_extractor=true,
        /// non solo quando gvd_din.enabled=true.
        /// LandmarkRegistry.FillGvdDinOverlayData gestisce già entrambi i branch.
        /// </summary>
        public void GetGvdDinOverlayData(GvdDinOverlaySnapshot snapshot)
        {
            if (snapshot == null)
                return;

            snapshot.Clear(); // IsValid = false per default

            // Controlla se almeno uno dei due sistemi è attivo.
            // Hybrid ha priorità su GVD-DIN (stesso ordine di LandmarkRegistry).
            var hybridCfg = Config?.Sim?.hybrid_landmark;
            bool hybridActive = hybridCfg != null && hybridCfg.use_hybrid_extractor;

            var gvdCfg = Config?.Sim?.gvd_din;
            bool gvdActive = gvdCfg != null && gvdCfg.enabled;

            if (!hybridActive && !gvdActive)
                return;

            // Delega al LandmarkRegistry che conosce entrambi i computer.
            LandmarkRegistry?.FillGvdDinOverlayData(snapshot);
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
        /// Perché esiste:
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

            // Se lo stock è privato di un NPC, aggiungiamo (o aggiorniamo) la belief pinned.
            // Nota: questo non significa che l'NPC lo "vede ora", significa che lo stock è stato
            // creato/assegnato a lui in un punto del gameplay dove la conoscenza è implicita
            // (es. lo ha posato lui, lo ha ricevuto come proprietà ).
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
        /// - Non validiamo "consistenza semantica" qui (es: puoi dire Eat anche se non c'è cibo).
        /// - Questo è volutamente un canale descrittivo/diagnostico.
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
        // =====================================================================
        // MOVEMENT PATH HELPERS — Patch 0.02.05.B
        // =====================================================================
        // Questi metodi erano precedentemente implementati qui.
        // Sono stati spostati in MovementPathfinder (Scripts/Core/Systems/Movement/)
        // perché sono algoritmi di navigazione, non dati del World.
        //
        // Questi thin wrapper mantengono la firma pubblica invariata per
        // compatibilità con MovementSystem, DevOrderNpcMoveToCellCommand
        // e qualsiasi altro consumer che chiama world.X(npcId, ...).
        //
        // NOTA: i metodi di gestione dello stato local search (HasActiveNpcLocalSearch,
        // TryReplanNpcLocalSearch, ecc.) sono anch'essi in MovementPathfinder perché
        // sono strettamente accoppiati agli algoritmi di navigazione locale.
        // =====================================================================

        /// <summary>
        /// True se l'NPC ha una local search attiva in questo tick.
        /// Delega a <see cref="MovementPathfinder.HasActiveNpcLocalSearch"/>.
        /// </summary>
 /*       public bool HasActiveNpcLocalSearch(int npcId)
            => MovementPathfinder.HasActiveNpcLocalSearch(this, npcId);

        /// <summary>
        /// Restituisce il prossimo step della local search attiva.
        /// Delega a <see cref="MovementPathfinder.TryGetActiveNpcLocalSearchNextStep"/>.
        /// </summary>
        public bool TryGetActiveNpcLocalSearchNextStep(int npcId, out int stepX, out int stepY)
            => MovementPathfinder.TryGetActiveNpcLocalSearchNextStep(this, npcId, out stepX, out stepY);

        /// <summary>
        /// Avanza lo stato della local search dopo un passo riuscito.
        /// Delega a <see cref="MovementPathfinder.AdvanceNpcLocalSearchAfterSuccessfulStep"/>.
        /// </summary>
        public void AdvanceNpcLocalSearchAfterSuccessfulStep(int npcId, int fromX, int fromY, int toX, int toY)
            => MovementPathfinder.AdvanceNpcLocalSearchAfterSuccessfulStep(this, npcId, fromX, fromY, toX, toY);

        /// <summary>
        /// Tenta di ripianificare la local search dall'posizione corrente.
        /// Delega a <see cref="MovementPathfinder.TryReplanNpcLocalSearch"/>.
        /// </summary>
        public bool TryReplanNpcLocalSearch(int npcId, int currentX, int currentY)
            => MovementPathfinder.TryReplanNpcLocalSearch(this, npcId, currentX, currentY);

        /// <summary>
        /// True se il target è raggiungibile con path greedy diretto (senza macro-route).
        /// Delega a <see cref="MovementPathfinder.CanNpcUseDirectPath"/>.
        /// </summary>
        public bool CanNpcUseDirectPath(int npcId, int targetX, int targetY)
            => MovementPathfinder.CanNpcUseDirectPath(this, npcId, targetX, targetY);

        /// <summary>
        /// Costruisce un path greedy diretto completo verso il target.
        /// Delega a <see cref="MovementPathfinder.TryBuildGreedyDirectPath"/>.
        /// </summary>
        public bool TryBuildGreedyDirectPath(int npcId, int startX, int startY, int targetX, int targetY, List<Vector2Int> outCells)
            => MovementPathfinder.TryBuildGreedyDirectPath(this, npcId, startX, startY, targetX, targetY, outCells);

        /// <summary>
        /// Costruisce il prefisso diretto massimo (si ferma al primo blocco).
        /// Delega a <see cref="MovementPathfinder.TryBuildGreedyDirectPrefixPath"/>.
        /// </summary>
        public bool TryBuildGreedyDirectPrefixPath(int npcId, int startX, int startY, int targetX, int targetY, List<Vector2Int> outCells)
            => MovementPathfinder.TryBuildGreedyDirectPrefixPath(this, npcId, startX, startY, targetX, targetY, outCells);

        /// <summary>
        /// Ricerca locale bounded (BFS/JPS) per aggirare ostacoli locali.
        /// Delega a <see cref="MovementPathfinder.TryBuildBoundedMovePath"/>.
        /// </summary>
        public bool TryBuildBoundedMovePath(int npcId, int startX, int startY, int targetX, int targetY, int maxVisited, List<Vector2Int> outCells)
            => MovementPathfinder.TryBuildBoundedMovePath(this, npcId, startX, startY, targetX, targetY, maxVisited, outCells);
 */
        // ============================================================
        // LOS helpers (Bresenham)
        // ============================================================

        /// <summary>
        /// Verifica la Line of Sight (LOS) discreta su griglia con algoritmo di Bresenham.
        ///
        /// <para><b>Regola:</b> se una cella intermedia ha <c>BlocksVision = true</c>
        /// (derivato dall'OcclusionMap) la LOS è bloccata.</para>
        ///
        /// <para>
        /// La cella sorgente NON viene controllata (l'NPC può stare dentro un muro
        /// in scenari di test senza che la LOS sia sempre bloccata).
        /// La cella target viene controllata: se è un muro, la LOS è bloccata.
        /// </para>
        ///
        /// <para>
        /// Questo è il gate LOS usato da <c>ObjectPerceptionSystem</c>,
        /// <c>NpcPerceptionSystem</c>, <c>TokenDeliveryPipeline</c> e altri.
        /// È il punto centrale dell'occlusione visiva in Arcontio.
        /// </para>
        ///
        /// <para>
        /// <b>Performance:</b> O(max(|dx|, |dy|)) per chiamata.
        /// Chiamato molto frequentemente (ogni NPC per ogni oggetto/NPC ogni tick).
        /// La cache <c>_blocksVision[]</c> rende ogni step O(1).
        /// </para>
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

    // =============================================================================
    // GlobalState
    // =============================================================================
    /// <summary>
    /// <b>GlobalState</b> — parametri di configurazione runtime della simulazione.
    ///
    /// <para>
    /// Contiene tutti i "tunable" che i System e le Rule leggono ogni tick.
    /// Viene popolato nel costruttore di <see cref="World"/> a partire da
    /// <see cref="WorldConfig"/> (letto da <c>game_params.json</c>) e può
    /// essere sovrascritto dai seed di scenario in <c>SimulationHost</c>.
    /// </para>
    ///
    /// <para>
    /// È una <c>struct</c> per evitare allocazioni heap, ma questo significa
    /// che viene copiata se passata per valore. Accedere sempre tramite
    /// <c>world.Global.X</c> (campo pubblico del World).
    /// </para>
    ///
    /// <para><b>Patch 0.02.5A:</b> solo aggiornamento commenti.</para>
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