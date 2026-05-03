// Assets/Scripts/Core/Runtime/SimulationHost.cs
using Arcontio.Core.Diagnostics;
using Arcontio.Core.Logging;
using Arcontio.Core.Save;
using System.Collections.Generic;
using System.ComponentModel;
using System.Security.Cryptography;
using UnityEditor;
using UnityEditor.Experimental.GraphView;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.LightTransport;

namespace Arcontio.Core
{
    /// <summary>
    /// SimulationHost: orchestra il ciclo del simulatore Arcontio.
    ///
    /// Nota architetturale:
    /// - Questo oggetto vive su ArcontioRuntime (DontDestroyOnLoad),
    ///   quindi deve essere "singleton" e non deve duplicarsi tra scene.
    /// - Le scene (AtomView, MapGrid, ecc.) sono SOLO viste: leggono World e inviano comandi,
    ///   ma non creano un altro SimulationHost.
    /// </summary>
    public sealed class SimulationHost : MonoBehaviour
    {
        [Header("Tick")]
        [SerializeField] private float tickDeltaTime = 1f; // tempo simulato per tick (es. 1 = 1 minuto)
        [SerializeField] private int ticksPerSecond = 10;  // velocità simulazione (tick/secondo reali)

        [Header("Debug Scenarios")]
        [SerializeField] private bool enableLegacyDebugScenarioBootstrap = false;
        [SerializeField] private DebugScenario debugScenario = DebugScenario.None;

        [Header("World Save/Load Bootstrap")]
        [SerializeField] private bool enableWorldSnapshotBootstrap = false;
        [SerializeField] private string worldSnapshotSlotName = "default";

        private enum DebugScenario
        {
            None = 0,
            Day6_Assimilation = 6,
            Day7_Delivery = 7,
            Day8_ObjectPerception = 8,
            Day9_NeedsOwnership = 9,
            Day10_Move_Memory_Theft = 10,
            P0_02_Landmark_PathFinding = 11,
            /// <summary>
            /// Carica NPC da Resources/Arcontio/Scenarios/default_scenario.json.
            /// Se il file non esiste, fa fallback a P0_02_Landmark_PathFinding.
            /// </summary>
            FromScenarioFile = 99,
        }

        // Log sintetico per tick (solo per questo scenario)
        [SerializeField] private int day8LogEveryTicks = 10;


        // Core state
        private World _world;
        private MessageBus _bus;
        private Scheduler _scheduler;
        private Telemetry _telemetry;

        private NpcCommunicationPipeline _npcCommunication;
        // MemoryTrace
        //     |
        //     V
        // TokenEmissionPipeline
        //     |
        //     V
        // TokenBusOut(parlato)
        //     |
        //     V
        // TokenDeliveryPipeline
        //     |
        //     V
        // TokenBusIn(sentito)
        //     |
        //     V
        // TokenAssimilationPipeline
        //     |
        //     V
        // MemoryTrace / Rumor / Belief

        private MemoryEncodingSystem _memoryEncoding;

        // Working buffers (evitano allocazioni ogni tick)
        private readonly List<ISystem> _toRun = new();
        private readonly List<IRule> _rules = new();
        private readonly List<ICommand> _commands = new();
        // ============================================================
        // DEVTOOLS COMMAND BUFFER (View -> Core)
        // ============================================================
        //
        // Requisito DevMode v0 (MVP):
        // - la View deve poter "editare" la mappa a runtime (place/erase, save/load) fileciteturn4file9
        // - ma NON deve mutare direttamente il World.
        //
        // Scelta tecnica:
        // - esponiamo una coda di ICommand dedicata alle viste (DevTools, editor runtime, ecc.)
        // - la coda viene "pumpata" anche quando la sim è in pausa.
        //
        // IMPORTANTISSIMO:
        // - Questo NON sostituisce le Rule/Command flow.
        // - È un canale separato, esplicito e *solo* per debug tool / UI.
        private readonly List<ICommand> _externalCommands = new(128);

        private readonly List<ISimEvent> _eventBuffer = new();

        // Contatore del tick (long).
        // Inizializzazione normale: il campo parte da 0 per inizializzazione CLR
        // e viene incrementato alla fine di StepOneTick. Nel path save/load,
        // invece, viene riallineato esplicitamente a WorldSaveData.savedAtTick.
        private long _tickIndex;

        // Accumulatore di tempo reale per eseguire tick discreti
        private float _accum;

        // Singleton / accesso per le viste
        public static SimulationHost Instance { get; private set; }
        public World World => _world;
        /// <summary>
        /// EnqueueExternalCommand:
        /// API per le viste (DevTools) per richiedere una modifica al World tramite ICommand.
        ///
        /// Nota:
        /// - Il comando verrà eseguito sul main thread nel prossimo "pump" (Update).
        /// - Il pump avviene anche quando la sim è in pausa, così il DevMode resta usabile.
        /// </summary>
        public void EnqueueExternalCommand(ICommand cmd)
        {
            if (cmd == null) return;
            _externalCommands.Add(cmd);
        }
        public long TickIndex => _tickIndex;

        // =============================================================================
        // SaveCurrentWorldSnapshot
        // =============================================================================
        /// <summary>
        /// <para>
        /// Entry point tecnico/dev per salvare lo snapshot canonico del mondo
        /// corrente su disco.
        /// </para>
        ///
        /// <para><b>Principio architetturale: baseline tecnica v0.10, non UI finale</b></para>
        /// <para>
        /// Questo metodo collega intenzionalmente i tre mattoni canonici del macro job
        /// v0.10: <see cref="WorldSaveBuilder"/> costruisce il DTO, <see cref="WorldSaveIO"/>
        /// lo scrive nello slot world-level e <c>SimulationHost</c> fornisce il tick
        /// autorevole. Non sostituisce ancora pulsanti, menu, profili utente o
        /// politiche di autosave: e' un punto d'ingresso controllato per devtools,
        /// test manuali e futuri command wrapper.
        /// </para>
        ///
        /// <para><b>Struttura interna:</b></para>
        /// <list type="bullet">
        ///   <item><b>Guardia World</b>: rifiuta host non inizializzato.</item>
        ///   <item><b>Builder</b>: fotografa il World senza mutarlo.</item>
        ///   <item><b>IO</b>: scrive <c>world_snapshot.json</c> nello slot canonico.</item>
        ///   <item><b>Log</b>: rende leggibile successo/fallimento senza crash opaco.</item>
        /// </list>
        /// </summary>
        public bool SaveCurrentWorldSnapshot(string slotName)
        {
            if (_world == null)
            {
                Debug.LogError("[SimulationHost] SaveCurrentWorldSnapshot failed: World non inizializzato.");
                return false;
            }

            string resolvedSlotName = string.IsNullOrWhiteSpace(slotName) ? "default" : slotName;

            try
            {
                // savedAtTick usa la semantica v0.10.13: e' il prossimo tick che
                // verra' eseguito, perche' _tickIndex viene incrementato solo alla
                // fine di StepOneTick. Il load non deve quindi sommare uno.
                WorldSaveData data = WorldSaveBuilder.BuildFromWorld(
                    _world,
                    _tickIndex,
                    configRef: "Arcontio/Config/game_params",
                    scenarioRef: enableLegacyDebugScenarioBootstrap ? debugScenario.ToString() : string.Empty);

                bool ok = WorldSaveIO.SaveWorldSnapshot(data, resolvedSlotName);
                if (ok)
                {
                    Debug.Log($"[SimulationHost] Saved WorldSaveData slot '{resolvedSlotName}' at tick {_tickIndex}.");
                }
                else
                {
                    Debug.LogError($"[SimulationHost] SaveCurrentWorldSnapshot failed while writing slot '{resolvedSlotName}'.");
                }

                return ok;
            }
            catch (System.Exception e)
            {
                Debug.LogError($"[SimulationHost] SaveCurrentWorldSnapshot failed for slot '{resolvedSlotName}'. {e}");
                return false;
            }
        }

        // =============================================================================
        // LoadWorldSnapshot
        // =============================================================================
        /// <summary>
        /// <para>
        /// Entry point tecnico/dev per caricare uno snapshot canonico da disco e
        /// sostituire il World corrente solo dopo un'applicazione riuscita.
        /// </para>
        ///
        /// <para><b>Principio architetturale: load atomico senza mondo ibrido</b></para>
        /// <para>
        /// Il loader canonico richiede un <c>World</c> pulito per preservare ID e
        /// rifiutare collisioni. Per questo il metodo non applica lo snapshot sopra
        /// il mondo gia' seedato: crea un nuovo <c>World</c> configurato, applica il
        /// DTO, ricostruisce cache/landmark e solo alla fine sostituisce il
        /// riferimento runtime. Se qualcosa fallisce, il mondo corrente resta vivo.
        /// </para>
        ///
        /// <para><b>Struttura interna:</b></para>
        /// <list type="bullet">
        ///   <item><b>Read</b>: usa <see cref="WorldSaveIO"/> per leggere il DTO grezzo.</item>
        ///   <item><b>Fresh World</b>: crea una nuova istanza configurata dai JSON runtime.</item>
        ///   <item><b>Apply</b>: delega a <see cref="WorldSaveLoader"/> il restore degli store coperti.</item>
        ///   <item><b>Swap</b>: sostituisce <c>_world</c>, ripristina tick e resetta buffer runtime transitori.</item>
        /// </list>
        /// </summary>
        public bool LoadWorldSnapshot(string slotName)
        {
            string resolvedSlotName = string.IsNullOrWhiteSpace(slotName) ? "default" : slotName;
            int preNpcCount = CountNpcsForDiagnostics(_world);
            int preObjectCount = CountObjectsForDiagnostics(_world);

            Debug.Log(
                $"[WorldSnapshotLoadDiag][SimulationHost] LoadWorldSnapshot START slot='{resolvedSlotName}' " +
                $"currentWorldHash={(_world != null ? _world.GetHashCode() : 0)} " +
                $"currentNpcCount={preNpcCount} currentObjectCount={preObjectCount} currentTick={_tickIndex}");

            WorldSaveData data = WorldSaveIO.LoadWorldSnapshotData(resolvedSlotName);
            if (data == null)
            {
                Debug.LogError($"[SimulationHost] LoadWorldSnapshot failed: DTO non leggibile per slot '{resolvedSlotName}'.");
                return false;
            }

            Debug.Log(
                $"[WorldSnapshotLoadDiag][SimulationHost] Snapshot READ OK slot='{resolvedSlotName}' " +
                $"schemaVersion={data.schemaVersion} savedAtTick={data.savedAtTick} " +
                $"size={data.worldWidth}x{data.worldHeight} nextNpcId={data.nextNpcId} nextObjectId={data.nextObjectId} " +
                $"snapshotNpcCount={CountArrayForDiagnostics(data.npcs)} snapshotObjectCount={CountArrayForDiagnostics(data.objects)} " +
                $"foodStocks={CountArrayForDiagnostics(data.foodStocks)} objectUseStates={CountArrayForDiagnostics(data.objectUseStates)}");

            if (!TryCreateWorldFromSnapshotData(data, out World loadedWorld, out string error))
            {
                Debug.LogError(
                    $"[WorldSnapshotLoadDiag][SimulationHost] Apply FAILED slot='{resolvedSlotName}' " +
                    $"oldWorldHash={(_world != null ? _world.GetHashCode() : 0)} " +
                    $"oldNpcCount={preNpcCount} oldObjectCount={preObjectCount} error='{error}'");
                return false;
            }

            int loadedNpcCount = CountNpcsForDiagnostics(loadedWorld);
            int loadedObjectCount = CountObjectsForDiagnostics(loadedWorld);
            int oldWorldHash = _world != null ? _world.GetHashCode() : 0;
            int newWorldHash = loadedWorld != null ? loadedWorld.GetHashCode() : 0;

            Debug.Log(
                $"[WorldSnapshotLoadDiag][SimulationHost] Apply OK before swap slot='{resolvedSlotName}' " +
                $"oldWorldHash={oldWorldHash} loadedWorldHash={newWorldHash} " +
                $"loadedNpcCount={loadedNpcCount} loadedObjectCount={loadedObjectCount}");

            _world = loadedWorld;

            Debug.Log(
                $"[WorldSnapshotLoadDiag][SimulationHost] World SWAP executed slot='{resolvedSlotName}' " +
                $"oldWorldHash={oldWorldHash} newWorldHash={newWorldHash} " +
                $"npcCountPre={preNpcCount} npcCountPost={CountNpcsForDiagnostics(_world)} " +
                $"objectCountPre={preObjectCount} objectCountPost={CountObjectsForDiagnostics(_world)}");

            RestoreTickFromWorldSnapshot(data.savedAtTick);
            ResetTransientRuntimeAfterWorldSnapshotLoad();

            // Il load dev viene lasciato in pausa per evitare che il primo tick
            // successivo consumi subito lo stato appena ripristinato mentre un tool
            // o una view sta ancora ispezionando il risultato.
            SetPaused(true);

            Debug.Log($"[SimulationHost] Loaded WorldSaveData slot '{resolvedSlotName}' at tick {_tickIndex}.");
            return true;
        }

        // Flag per evitare seeding multipli (in caso di scene reload accidentali)
        private bool _seeded;

        // Bootstrap failure controllato: se il path snapshot viene richiesto ma fallisce,
        // l'host non deve cadere in un seed baseline/scenario implicito e mascherare il problema.
        private bool _bootstrapForcedPause;

        // ============================================================
        // TICK CONTROL (Input System)
        // ============================================================
        [Header("Tick Control (Input System)")]
        [SerializeField] private bool startPaused = false;

        [SerializeField] private InputActionReference togglePauseAction;
        [SerializeField] private InputActionReference stepOneTickAction;
        [SerializeField] private InputActionReference stepTenTicksAction;

        // =============================================================================
        // DirectKeyboardTickControl
        // =============================================================================
        /// <summary>
        /// <para>
        /// Abilita il controllo diretto da tastiera dei comandi globali di tick:
        /// <c>P</c> per pausa/unpausa, <c>O</c> per un singolo tick mentre il
        /// simulatore e' in pausa, <c>I</c> per dieci tick mentre il simulatore e'
        /// in pausa.
        /// </para>
        ///
        /// <para><b>Principio architetturale: Input globale del simulatore fuori dalla UI</b></para>
        /// <para>
        /// Il pannello laterale e gli elementi UI possono usare EventSystem, Button e
        /// ScrollRect, ma non devono diventare il punto di verita' per il ciclo del
        /// simulatore. Questo fallback mantiene il controllo temporale nel
        /// <c>SimulationHost</c>, cioe' nello strato che gia' possiede pausa,
        /// accumulatore e avanzamento deterministico dei tick.
        /// </para>
        ///
        /// <para><b>Struttura interna:</b></para>
        /// <list type="bullet">
        ///   <item><b>directKeyboardTickControlEnabled</b>: abilita il percorso diretto e impedisce doppi trigger dagli InputActionReference legacy.</item>
        ///   <item><b>directTogglePauseKey</b>: tasto globale per pausa/unpausa.</item>
        ///   <item><b>directStepOneTickKey</b>: tasto globale per avanzamento singolo in pausa.</item>
        ///   <item><b>directStepTenTicksKey</b>: tasto globale per avanzamento multiplo in pausa.</item>
        /// </list>
        /// </summary>
        [Header("Tick Control (Direct Keyboard Fallback)")]
        [SerializeField] private bool directKeyboardTickControlEnabled = true;
        [SerializeField] private Key directTogglePauseKey = Key.P;
        [SerializeField] private Key directStepOneTickKey = Key.O;
        [SerializeField] private Key directStepTenTicksKey = Key.I;

        public bool IsPaused { get; private set; }

        public void SetPaused(bool paused)
        {
            IsPaused = paused;
            if (IsPaused) _accum = 0f; // evita catch-up tick al resume
        }

        public void TogglePause() => SetPaused(!IsPaused);

        public void StepOneTickPaused()
        {
            if (!IsPaused) return;
            StepOneTick();
        }

        public void StepManyTicksPaused(int count)
        {
            if (!IsPaused) return;
            if (count <= 0) return;

            for (int i = 0; i < count; i++)
                StepOneTick();
        }
        private void OnEnable()
        {
            if (directKeyboardTickControlEnabled)
                return;

            // Toggle pause
            if (togglePauseAction != null && togglePauseAction.action != null)
            {
                togglePauseAction.action.Enable();
                togglePauseAction.action.performed += OnTogglePausePerformed;
            }

            // Step 1
            if (stepOneTickAction != null && stepOneTickAction.action != null)
            {
                stepOneTickAction.action.Enable();
                stepOneTickAction.action.performed += OnStepOnePerformed;
            }

            // Step 10
            if (stepTenTicksAction != null && stepTenTicksAction.action != null)
            {
                stepTenTicksAction.action.Enable();
                stepTenTicksAction.action.performed += OnStepTenPerformed;
            }
        }
        private void OnTogglePausePerformed(InputAction.CallbackContext ctx)
        {
            // Solo "press" (se usi Press interaction)
            TogglePause();
        }

        private void OnStepOnePerformed(InputAction.CallbackContext ctx)
        {
            StepOneTickPaused();
        }

        private void OnStepTenPerformed(InputAction.CallbackContext ctx)
        {
            StepManyTicksPaused(10);
        }

        private void OnDisable()
        {
            if (togglePauseAction != null && togglePauseAction.action != null)
                togglePauseAction.action.performed -= OnTogglePausePerformed;

            if (stepOneTickAction != null && stepOneTickAction.action != null)
                stepOneTickAction.action.performed -= OnStepOnePerformed;

            if (stepTenTicksAction != null && stepTenTicksAction.action != null)
                stepTenTicksAction.action.performed -= OnStepTenPerformed;
        }

        private void Awake()
        {
            // ******************************************************************************************************************************
            // 1) ANTI-DUPLICAZIONE: se esiste già un host, distruggo questo.
            // ******************************************************************************************************************************
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }
            Instance = this;

            // ******************************************************************************************************************************
            // 2) INIZIALIZZO IL LOG PERSONALIZZATO (ArcontioLogger)
            // ******************************************************************************************************************************
            // ============================================================
            // NOTE (pulizia path):
            // In precedenza qui c'era "Config/game_params" mentre il resto del progetto
            // usa "Arcontio/Config/game_params".
            //
            // Per evitare di avere DUE copie del file (una per logger e una per sim),
            // usiamo lo stesso path canonico "Arcontio/Config/...".
            // ============================================================
            ArcontioLogger.InitFromResources(
               gameParamsPathNoExt: "Arcontio/Config/game_params",
               localizationPathNoExt: "Arcontio/Config/localization_logs"
           );
            ArcontioLogger.Info(
                new LogContext(tick: (int)TickContext.CurrentTickIndex, channel: "Core"),
                new LogBlock(LogLevel.Info, "log.core.persistent_path")
                    .AddField("path", Application.persistentDataPath)
            );

            // Creo la finestra per visualizzare il log personalizzato a schermo
            if (FindFirstObjectByType<Arcontio.View.ArcontioLogOverlay>() == null)
            {
                new GameObject("ArcontioLogOverlay")
                    .AddComponent<Arcontio.View.ArcontioLogOverlay>();
            }

            // ******************************************************************************************************************************
            // 3) INIZIALIZZO IL MONDO 
            // ******************************************************************************************************************************

            // ******************************************************************************************************************************
            // 3.1) Leggo da file game_params.json i dati di simulazione, che finiscono in simParams.
            // Con quello creo l'istanza di world
            // ******************************************************************************************************************************
            _world = CreateWorldFromGameParams();

            // ******************************************************************************************************************************
            // 3.2) INIZIALIZZO IL MESSAGE BUS
            // ******************************************************************************************************************************
            _bus = new MessageBus();

            // ******************************************************************************************************************************
            // 3.3) INIZIALIZZO LO SCHEDULER DEI SISTEMI
            // ******************************************************************************************************************************
            _scheduler = new Scheduler();

            // ******************************************************************************************************************************
            // 3.4) INIZIALIZZO L'OGGETTO TELEMETRY (DEBUG)
            // ******************************************************************************************************************************
            _telemetry = new Telemetry();

            // ******************************************************************************************************************************
            // 4) CARICAMENTO DA FILE JSON
            // ******************************************************************************************************************************

            // ******************************************************************************************************************************
            // 4.1) Carichiamo definizioni oggetti da JSON (Resources/Config/object_defs.json).
            // ******************************************************************************************************************************
            LoadWorldRuntimeJsonConfig(_world);

            // ******************************************************************************************************************************
            // 4.2) Carica parametri fame/sonno da JSON
            // ******************************************************************************************************************************
            // Caricato da LoadWorldRuntimeJsonConfig.

            // ******************************************************************************************************************************
            // 4.3) Carica parametri decadimento BeliefStore da JSON
            // ******************************************************************************************************************************
            // Caricato da LoadWorldRuntimeJsonConfig.

            // ******************************************************************************************************************************
            // 4.4) Carica pesi QuerySystem BeliefStore da JSON
            // ******************************************************************************************************************************
            // Caricato da LoadWorldRuntimeJsonConfig.

            // ******************************************************************************************************************************
            // 5) ISCRIVO I SISTEMI ALLO SCHEDULER
            // ******************************************************************************************************************************

            // ******************************************************************************************************************************
            // 5.1) MOVIMENTO - MovementSystem (consuma MoveIntent)
            // ******************************************************************************************************************************
            _scheduler.AddSystem(new MovementSystem());

            // ******************************************************************************************************************************
            // 5.1B) LANDMARK MEMORY (Day3) - NpcLandmarkMemorySystem
            // ******************************************************************************************************************************
            // Questo System applica maintenance (eviction + cap) alla memoria soggettiva dei landmark.
            // Deve stare DOPO il MovementSystem per poter processare le nuove conoscenze acquisite nello stesso tick.
            _scheduler.AddSystem(new NpcLandmarkMemorySystem());


            // ******************************************************************************************************************************
            // 5.1C) LANDMARK PERCEPTION (v0.03.03.a — Landmark Perception) - LandmarkPerceptionSystem
            // ******************************************************************************************************************************
            // Apprendimento visivo dei landmark tramite FOV degli NPC.
            // Complementa il learning fisico (NotifyNpcMovedForLandmarkLearning).
            // Deve stare DOPO NpcLandmarkMemorySystem (processa i nodi già manutenuti).
            {
                int lpPeriod = _world.Config?.Sim?.landmark_perception?.period ?? 3;
                _scheduler.AddSystem(new LandmarkPerceptionSystem(lpPeriod));
            }

            // ******************************************************************************************************************************
            // 5.2) SCAN IN IDLE - IdleScan
            // ******************************************************************************************************************************
            // Quando l?NPC è idle, ruota (scan) per evitare ?visione 360 gratuita?.
            // Deve stare PRIMA della perception: così la rotation influenza cosa viene percepito nello stesso tick.
            _scheduler.AddSystem(new IdleScanSystem(scanPeriodTicks: 12));

            // ******************************************************************************************************************************
            // 5.3) BISOGNI NPC - NeedsDecaySystem
            // ******************************************************************************************************************************
            _scheduler.AddSystem(new NeedsDecaySystem());

            // ******************************************************************************************************************************
            // 5.4) PERCEZIONE - ObjectPerceptionSystem (genera eventi ObjectSpottedEvent)
            // ******************************************************************************************************************************
            _scheduler.AddSystem(new ObjectPerceptionSystem());

            // ******************************************************************************************************************************
            // 5.5) PERCEZIONE - NpcPerceptionSystem (genera eventi NpcSpottedEvent)
            // ******************************************************************************************************************************
            _scheduler.AddSystem(new NpcPerceptionSystem());

            // ******************************************************************************************************************************
            // 6) INIZIALIZZO LA COMUNICAZIONE TRA NPC
            // ******************************************************************************************************************************

            // ******************************************************************************************************************************
            // 6.1) Pipeline comunicazione NPC
            // ******************************************************************************************************************************
            // Mantiene TokenBus separato da MessageBus e incapsula:
            // emissione -> delivery -> assimilation.
            _npcCommunication = new NpcCommunicationPipeline(contactRadius: 2, topN: 6);

            // ******************************************************************************************************************************
            // 7) INIZIALIZZO LA GESTIONE DELLA MEMORIA NPC
            // ******************************************************************************************************************************

            // ******************************************************************************************************************************
            // 7.1) GESTORE DELLA MEMORIA - _memoryEncoding
            // ******************************************************************************************************************************
            // Metto qui e non nello scheduler l'encoding della memoria per problemi di sincronia
            // (deve vedere ESATTAMENTE gli eventi del tick drainati nel buffer)
            _memoryEncoding = new MemoryEncodingSystem();
            // Assegna qui la lista di eventi drainata dal bus.
            _memoryEncoding.SetEventsBuffer(_eventBuffer);

            // ******************************************************************************************************************************
            // 7.2) DECADIMENTO MEMORIA OGGETTI RILEVATI NEL MONDO
            // ******************************************************************************************************************************
            _scheduler.AddSystem(new ObjectMemoryMaintenanceSystem());

            // ******************************************************************************************************************************
            // 7.3) DECADIMENTO MEMORIA NPC
            // ******************************************************************************************************************************
            // Poi decay (maintenance)
            // Non fa nulla finché lo store è vuoto
            _scheduler.AddSystem(new MemoryDecaySystem());

            // ******************************************************************************************************************************
            // 7.4) DECADIMENTO BELIEFSTORE
            // ******************************************************************************************************************************
            // Mantiene confidence/freshness delle credenze soggettive senza leggere
            // world state globale e senza anticipare il QuerySystem.
            _scheduler.AddSystem(new BeliefDecaySystem());

            // ******************************************************************************************************************************
            // 8) INIZIALIZZO LE RULES
            // ******************************************************************************************************************************
            // ATTENZIONE: le Rules della memoria sono inizializzate in MemoryEncodingSystem
            _rules.Add(new DebugEventLogRule());
            //     _rules.Add(new BasicSurvivalRule());
            _rules.Add(new NeedsDecisionRule(decisionEveryTicks: 25, _world.Global.NpcOperationalRangeCells));

            // ******************************************************************************************************************************
            // 9) SEED (Selettore dei casi di test)
            // ******************************************************************************************************************************
            EnsureSeeded();

            // Gestione Start/Pause del simulatore
            IsPaused = startPaused || _bootstrapForcedPause;
            if (IsPaused) _accum = 0f;

            ArcontioLogger.Debug(
                new LogContext(tick: (int)TickContext.CurrentTickIndex, channel: "Test"),
                new LogBlock(LogLevel.Debug, "log.test.scenario")
                    .AddField("scenario", debugScenario)
            );
        }

        private void Update()
        {
            HandleDirectKeyboardTickControl();

            // ============================================================
            // DEVTOOLS / VIEW COMMANDS (always pumped)
            // ============================================================
            // IMPORTANTISSIMO:
            // - DevMode deve poter editare la mappa anche quando la sim è in pausa.
            // - Quindi eseguiamo sempre i comandi esterni prima del return su IsPaused.
            PumpExternalCommands();

            if (IsPaused)
                return;

            float dt = Time.unscaledDeltaTime;
            _accum += dt;

            float tickInterval = 1f / Mathf.Max(1, ticksPerSecond);

            while (_accum >= tickInterval)
            {
                _accum -= tickInterval;
                StepOneTick();
            }
        }

        // =============================================================================
        // HandleDirectKeyboardTickControl
        // =============================================================================
        /// <summary>
        /// <para>
        /// Legge i tasti globali del controllo temporale direttamente dal New Input
        /// System e li traduce nelle API gia' esistenti di pausa e step.
        /// </para>
        ///
        /// <para><b>Principio architetturale: comando temporale centralizzato</b></para>
        /// <para>
        /// L'input viene letto qui, ma l'effetto non viene duplicato: <c>P</c> chiama
        /// <c>TogglePause</c>, <c>O</c> chiama <c>StepOneTickPaused</c> e <c>I</c>
        /// chiama <c>StepManyTicksPaused</c>. In questo modo la semantica resta una
        /// sola e il fallback non crea una seconda implementazione dello scheduler.
        /// </para>
        ///
        /// <para><b>Struttura interna:</b></para>
        /// <list type="bullet">
        ///   <item><b>Guardia configurabile</b>: se il fallback e' disabilitato, restano attivi gli InputActionReference serializzati.</item>
        ///   <item><b>Guardia device</b>: se non esiste una tastiera corrente, il metodo esce senza effetti.</item>
        ///   <item><b>Press singola</b>: ogni comando usa <c>wasPressedThisFrame</c> per evitare ripetizioni da tasto tenuto premuto.</item>
        /// </list>
        /// </summary>
        private void HandleDirectKeyboardTickControl()
        {
            if (!directKeyboardTickControlEnabled)
                return;

            var keyboard = Keyboard.current;
            if (keyboard == null)
                return;

            if (WasKeyPressedThisFrame(keyboard, directTogglePauseKey))
                TogglePause();

            if (WasKeyPressedThisFrame(keyboard, directStepOneTickKey))
                StepOneTickPaused();

            if (WasKeyPressedThisFrame(keyboard, directStepTenTicksKey))
                StepManyTicksPaused(10);
        }

        // =============================================================================
        // WasKeyPressedThisFrame
        // =============================================================================
        /// <summary>
        /// <para>
        /// Incapsula l'accesso indicizzato alla tastiera del New Input System per
        /// mantenere il polling dei tasti leggibile e tollerante verso tasti non
        /// assegnati.
        /// </para>
        ///
        /// <para><b>Principio architetturale: boundary piccolo verso API esterna</b></para>
        /// <para>
        /// Il resto del <c>SimulationHost</c> non dipende dai dettagli di
        /// <c>Keyboard</c> e <c>KeyControl</c>. Se in futuro i tasti diventano
        /// configurabili da file o da profilo, la modifica resta localizzata qui.
        /// </para>
        ///
        /// <para><b>Struttura interna:</b></para>
        /// <list type="bullet">
        ///   <item><b>Validazione</b>: rifiuta <c>Key.None</c>.</item>
        ///   <item><b>Lettura controllo</b>: recupera il controllo fisico corrispondente al tasto richiesto.</item>
        ///   <item><b>Esito</b>: restituisce true solo nel frame della pressione.</item>
        /// </list>
        /// </summary>
        private static bool WasKeyPressedThisFrame(Keyboard keyboard, Key key)
        {
            if (key == Key.None)
                return false;

            var control = keyboard[key];
            return control != null && control.wasPressedThisFrame;
        }
        /// <summary>
        /// Esegue e svuota la coda di comandi esterni (view -> core).
        ///
        /// Questo è un "mini command buffer" dedicato ai tool runtime (DevMode).
        /// </summary>
        private void PumpExternalCommands()
        {
            if (_externalCommands.Count == 0) return;

            // Snapshot semplice: evitiamo problemi se un comando, durante Execute,
            // enqueues un altro comando (caso raro ma possibile in debug).
            var count = _externalCommands.Count;

            for (int i = 0; i < count; i++)
                _externalCommands[i].Execute(_world, _bus);

            _externalCommands.Clear();
        }

        private void StepOneTick()
        {
            // Rende il tick disponibile globalmente per logging in comandi ecc.
            TickContext.BeginTick(_tickIndex);

            var tick = new Tick(_tickIndex, tickDeltaTime);

            // ******************************************************************************************************************************
            // 1) Scheduler decide quali systems girano in questo tick
            // ******************************************************************************************************************************
            _scheduler.GetSystemsToRun(_tickIndex, _toRun);

            // ******************************************************************************************************************************
            // 2) Esegue systems (possono mutare World e pubblicare eventi)
            // ******************************************************************************************************************************
            // Nota: questi systems pubblicano eventi "di mondo" e/o tecnici nel MessageBus.
            for (int i = 0; i < _toRun.Count; i++)
                _toRun[i].Update(_world, tick, _bus, _telemetry);

            // Pulse "clock" per rules: DEVE essere regolare e frequente.
            _bus.Publish(new TickPulseEvent(_tickIndex));

            // ******************************************************************************************************************************
            // TEST STIMULI (Day6 / Day7 / Day8)
            // Inseriamo SOLO ciò che serve a generare segnali chiari.
            // ******************************************************************************************************************************
            // Day6: generiamo un evento "oggettivo" che finisce in memoria, poi token, poi assimilation.
            if (enableLegacyDebugScenarioBootstrap && debugScenario == DebugScenario.Day6_Assimilation && ((_tickIndex % 50) == 0))
            {
                ArcontioLogger.Debug(
                    new LogContext(tick: _tickIndex, channel: "T6"),
                    new LogBlock(LogLevel.Debug, "log.t6.predator_injected")
                );
                _bus.Publish(new PredatorSpottedEvent(
                    spotterNpcId: 1,
                    predatorId: 999,
                    cellX: 0,
                    cellY: 0,
                    distanceCells: 1,
                    spotQuality01: 1f));
            }

            // Day7: iniettiamo DIRETTAMENTE un AlarmShout in TokenBusOut
            // (così testiamo Delivery BFS anche se le emission rule di default parlano ?ProximityTalk?).
            if (enableLegacyDebugScenarioBootstrap && debugScenario == DebugScenario.Day7_Delivery && ((_tickIndex % 50) == 0))
            {
                var shout = new SymbolicToken(
                    type: TokenType.PredatorAlert,
                    subjectId: 999,
                    intensity01: 1.0f,
                    reliability01: 1.0f,
                    chainDepth: 0,
                    hasCell: true,
                    cellX: 0,
                    cellY: 0);

                // Coppia A: 1 -> 2
                _npcCommunication.PublishTokenOut(_world, new TokenEnvelope(
                    speakerId: 1,
                    listenerId: 2,
                    channel: TokenChannel.AlarmShout,
                    tickIndex: _tickIndex,
                    token: shout));

                // Coppia B: 3 -> 4
                _npcCommunication.PublishTokenOut(_world, new TokenEnvelope(
                    speakerId: 3,
                    listenerId: 4,
                    channel: TokenChannel.AlarmShout,
                    tickIndex: _tickIndex,
                    token: shout));

                ArcontioLogger.Debug(
                    new LogContext(tick: _tickIndex, channel: "T7"),
                    new LogBlock(LogLevel.Debug, "log.t7.alarmshout.published")
                        .AddField("routes", "1->2, 3->4")
                );
            }

            // Day8: non serve iniettare eventi a mano: ObjectPerceptionSystem li produce da solo.
            // Qui lasciamo che i log siano:
            // - Telemetry.ObjectPerception.SpottedEvents
            // - (se vuoi) un tuo log aggiuntivo ogni 50 tick
            if (enableLegacyDebugScenarioBootstrap && debugScenario == DebugScenario.Day8_ObjectPerception && ((_tickIndex % 50) == 0))
            {
                ArcontioLogger.Debug(
                    new LogContext(tick: _tickIndex, channel: "T8"),
                    new LogBlock(LogLevel.Debug, "log.t8.info")
                        .AddField("objects", _world.Objects.Count)
                        .AddField("vision", _world.Global.NpcVisionRangeCells)
                );
            }

            // ******************************************************************************************************************************
            // 3) Drain eventi in buffer (così possiamo farci sopra encoding memoria)
            // ******************************************************************************************************************************
            // Dopo questo punto:
            // - _eventBuffer contiene TUTTI gli eventi pubblicati finora nel tick
            // - il bus resta vuoto
            _eventBuffer.Clear();
            _bus.DrainTo(_eventBuffer);

            // ******************************************************************************************************************************
            // 3.1) Memory encoding (evento -> trace)
            // ******************************************************************************************************************************
            // Ora il buffer è pieno: codifichiamo memorie per gli NPC testimoni.
            // Nota: _memoryEncoding NON sta nello scheduler (per evitare problemi di sincronia).
            _memoryEncoding.Update(_world, tick, _bus, _telemetry);

            // 3.15) Comunicazione NPC:
            // - flush dei token event-driven accodati da MemoryEncodingSystem
            // - emissione da MemoryTrace
            // - delivery fisica
            // - assimilation nel listener
            _npcCommunication.ProcessAfterMemoryEncoding(_world, tick, _telemetry);

            // ******************************************************************************************************************************
            // 3.2) Ripubblichiamo gli eventi
            // ******************************************************************************************************************************
            // così le rules li vedono come prima
            // In questo modo manteniamo l'architettura originale:
            // - i Systems pubblicano eventi
            // - le Rule reagiscono a eventi e generano comandi
            for (int i = 0; i < _eventBuffer.Count; i++)
                _bus.Publish(_eventBuffer[i]);

            // ******************************************************************************************************************************
            // 4) Consuma eventi e lascia reagire le rules (producono comandi)
            // ******************************************************************************************************************************
            // Pulisco la lista dei comandi
            _commands.Clear();

            while (_bus.TryDequeue(out var e))
            {
                for (int r = 0; r < _rules.Count; r++)
                    _rules[r].Handle(_world, e, _commands, _telemetry);
            }

            // Esegue comandi (mutano World)
            for (int c = 0; c < _commands.Count; c++)
                _commands[c].Execute(_world, _bus);

            // Post-command drain + memory encoding
            // Perché: i comandi pubblicano eventi FACT (es: FoodStolenWorldEvent).
            // Se non li draini qui, li encodi solo nel tick successivo.
            _eventBuffer.Clear();
            _bus.DrainTo(_eventBuffer);

            if (_eventBuffer.Count > 0)
            {
                // encodiamo memorie anche per gli eventi post-comando
                _memoryEncoding.Update(_world, tick, _bus, _telemetry);

                // Patch 0.01P3 extension (IMPORTANTISSIMO):
                // Gli eventi post-comando (es. FoodStolenEvent) vengono creati *dopo* il primo giro
                // di TokenEmission/Delivery/Assimilation.
                // Se qui accodiamo comunicazioni event-driven (es. report di furto), vogliamo che:
                // - vengano consegnate e assimilate nello stesso tick (per coerenza UX e debug)
                // - e non "slittino" al tick successivo.
                //
                // Quindi facciamo un giro aggiuntivo solo sui token accodati:
                // delivery + assimilation, senza rieseguire emissione da memoria.
                _npcCommunication.ProcessQueuedOnly(_world, tick, _telemetry);

                // IMPORTANTISSIMO:
                // - Nel primo pass pre-command ripubblichiamo gli eventi perche' le rules devono ancora
                //   consumarli nel medesimo tick.
                // - Nel secondo pass post-command NON esiste una seconda rule-phase nello stesso tick.
                // - Qui _eventBuffer resta quindi solo una snapshot locale per memory encoding e queued-only
                //   communication.
                // - Ripubblicare questi eventi nel MessageBus li farebbe rientrare nel drain del tick
                //   successivo, causando re-processing di fatti gia' processati.
            }

            // DEBUG
            // PATCH 0.02.05.2e / bug 3:
            // Prima qui leggevamo in modo hardcoded _world.Memory[1] e _world.Memory[2].
            // Questo era fragile e crashava quando l'NPC pre-creato veniva eliminato a runtime
            // e sostituito da nuovi NPC con id differenti.
            //
            // Correzione:
            // - non assumiamo piu' che esistano le chiavi 1 e 2;
            // - iteriamo invece su TUTTI gli NPC presenti nel mondo in questo tick;
            // - per ciascuno logghiamo il numero di trace presenti nel suo MemoryStore.
            if (_tickIndex % 20 == 0)
            {
                var block = new LogBlock(LogLevel.Debug, "log.memorytest.traces")
                    .AddField("npcCount", _world.NpcDna.Count)
                    .AddField("memoryStores", _world.Memory.Count);

                foreach (var npcId in _world.NpcDna.Keys)
                {
                    int traces = _world.Memory.TryGetValue(npcId, out var mem) && mem != null ? mem.Traces.Count : 0;
                    block.AddField($"npc_{npcId}", traces);
                }

                ArcontioLogger.Debug(
                    new LogContext(tick: (int)TickContext.CurrentTickIndex, channel: "MemoryTest"),
                    block
                );
            }

            // ============================================================
            // Day8: log sintetico per tick (solo qui)
            // - Non vogliamo spam in Day6/Day7.
            // - In Day8 vogliamo vedere "quali oggetti" vengono visti.
            // ============================================================
            if (enableLegacyDebugScenarioBootstrap && debugScenario == DebugScenario.Day8_ObjectPerception)
            {
                if (day8LogEveryTicks <= 0) day8LogEveryTicks = 10;

                if ((_tickIndex % day8LogEveryTicks) == 0)
                {
                    LogDay8Snapshot(tick);
                }
            }

            // ============================================================
            // DEBUG FOV TELEMETRY:
            // Avanzamento finestra (1 volta per tick).
            //
            // IMPORTANTISSIMO: lo facciamo qui, alla fine del tick, dopo che:
            // - systems hanno prodotto percezione
            // - comandi hanno mutato mondo
            // Così il batch di N tick è coerente.
            // ============================================================
            _world.DebugFovTelemetry?.AdvanceTickWindow();

            _tickIndex++;

            // Debug: verifica che l'host resti vivo cambiando scena
            if (_tickIndex % 50 == 0)
            {
                ArcontioLogger.Debug(
                    new LogContext(tick: _tickIndex, channel: "Arcontio"),
                    new LogBlock(LogLevel.Debug, "log.arcontio.tick_summary")
                        .AddField("food", _world.FoodStocks.Count)
                        .AddField("npc", _world.NpcDna.Count)
                );
                _telemetry.DumpToConsole();
            }
        }

        /// <summary>
        /// Garantisce che il mondo sia seedato una sola volta.
        /// Serve per evitare rigenerazioni accidentali.
        /// </summary>
        private void EnsureSeeded()
        {
            if (_seeded) return;
            _seeded = true;

            if (enableWorldSnapshotBootstrap)
            {
                if (TryBootstrapFromWorldSnapshot(out string error))
                    return;

                Debug.LogError($"[SimulationHost] World snapshot bootstrap failed. Runtime seed skipped to avoid double bootstrap. {error}");
                _bootstrapForcedPause = true;
                return;
            }

            SeedTestWorld();

            // ============================================================
            // (v0.02 Day2) LandmarkRegistry bootstrap
            // ============================================================
            // IMPORTANTISSIMO:
            // - Il World viene creato PRIMA del seeding.
            // - Quindi muri/porte/oggetti che definiscono la geometria non esistono ancora in ctor.
            // - Qui, a seeding completato, ricostruiamo il registry oggettivo dei landmark.
            _world?.RebuildLandmarksBootstrap();
        }

        // =============================================================================
        // TryBootstrapFromWorldSnapshot
        // =============================================================================
        /// <summary>
        /// <para>
        /// Applica il percorso tecnico controllato "load from save snapshot" durante
        /// il bootstrap del runtime.
        /// </para>
        ///
        /// <para><b>Principio architetturale: snapshot diverso da seed</b></para>
        /// <para>
        /// Questo metodo e' volutamente separato da <see cref="SeedTestWorld"/>:
        /// uno snapshot canonico rappresenta un mondo gia' vissuto e deve preservare
        /// ID, ownership, memorie, belief e tick globale. I seed baseline/scenario,
        /// invece, costruiscono condizioni iniziali nuove e possono generare ID.
        /// Mischiare i due percorsi causerebbe doppio spawn, contatori ID incoerenti
        /// e stato cognitivo non piu' allineato al JSON salvato.
        /// </para>
        ///
        /// <para><b>Struttura interna:</b></para>
        /// <list type="bullet">
        ///   <item><b>Lettura DTO</b>: usa <see cref="WorldSaveIO"/> e non accede direttamente al disco.</item>
        ///   <item><b>Mappa</b>: riallinea <c>World.InitMap</c> alle dimensioni dello snapshot prima di applicare oggetti e NPC.</item>
        ///   <item><b>Applicazione</b>: delega a <see cref="WorldSaveLoader"/> l'autorita' di ricostruzione world-level.</item>
        ///   <item><b>Tick</b>: ripristina <c>_tickIndex</c>, <see cref="TickContext"/> e <c>World.Global.CurrentTickIndex</c>.</item>
        /// </list>
        /// </summary>
        private bool TryBootstrapFromWorldSnapshot(out string error)
        {
            error = string.Empty;

            WorldSaveData data = WorldSaveIO.LoadWorldSnapshotData(worldSnapshotSlotName);
            if (data == null)
            {
                error = $"Nessun WorldSaveData leggibile nello slot '{worldSnapshotSlotName}'.";
                return false;
            }

            if (!TryCreateWorldFromSnapshotData(data, out World loadedWorld, out error))
            {
                return false;
            }

            _world = loadedWorld;
            ResetTransientRuntimeAfterWorldSnapshotLoad();

            RestoreTickFromWorldSnapshot(data.savedAtTick);

            Debug.Log($"[SimulationHost] World snapshot bootstrap loaded slot '{worldSnapshotSlotName}' at tick {_tickIndex}.");
            return true;
        }

        // =============================================================================
        // TryCreateWorldFromSnapshotData
        // =============================================================================
        /// <summary>
        /// <para>
        /// Crea un <see cref="World"/> pulito, carica i JSON runtime necessari e
        /// applica il DTO canonico senza toccare il mondo attualmente esposto
        /// dall'host.
        /// </para>
        ///
        /// <para><b>Principio architetturale: preflight prima dello swap</b></para>
        /// <para>
        /// Questo helper e' condiviso da bootstrap load e load tecnico/dev. La
        /// regola e' sempre la stessa: lo snapshot viene applicato su una nuova
        /// istanza; solo il chiamante, dopo il successo, puo' assegnarla a
        /// <c>_world</c>. In caso di errore, il runtime corrente non viene
        /// contaminato da NPC, oggetti o cache parzialmente ripristinate.
        /// </para>
        ///
        /// <para><b>Struttura interna:</b></para>
        /// <list type="bullet">
        ///   <item><b>Validazione dimensioni</b>: rifiuta snapshot senza griglia valida.</item>
        ///   <item><b>Config</b>: usa i JSON runtime correnti come baseline tecnica v0.10.</item>
        ///   <item><b>InitMap</b>: riallinea la griglia alle dimensioni persistite.</item>
        ///   <item><b>Loader</b>: applica store e contatori tramite <see cref="WorldSaveLoader"/>.</item>
        /// </list>
        /// </summary>
        private bool TryCreateWorldFromSnapshotData(WorldSaveData data, out World loadedWorld, out string error)
        {
            loadedWorld = null;
            error = string.Empty;

            if (data == null)
            {
                error = "WorldSaveData nullo.";
                return false;
            }

            if (data.worldWidth <= 0 || data.worldHeight <= 0)
            {
                error = $"Dimensioni snapshot non valide: {data.worldWidth}x{data.worldHeight}.";
                return false;
            }

            loadedWorld = CreateWorldFromGameParams();
            LoadWorldRuntimeJsonConfig(loadedWorld);

            Debug.Log(
                $"[WorldSnapshotLoadDiag][SimulationHost] Fresh World created " +
                $"worldHash={loadedWorld.GetHashCode()} mapBeforeInit={loadedWorld.MapWidth}x{loadedWorld.MapHeight}");

            // Il World nasce da game_params per ottenere configurazioni, database
            // oggetti e sistemi runtime coerenti con l'eseguibile corrente. Prima
            // di applicare lo snapshot, pero', la griglia deve assumere la
            // dimensione salvata: bounds check, cache oggetti e occlusion map
            // devono validare contro la mappa persistita.
            loadedWorld.InitMap(data.worldWidth, data.worldHeight);

            Debug.Log(
                $"[WorldSnapshotLoadDiag][SimulationHost] Fresh World InitMap " +
                $"worldHash={loadedWorld.GetHashCode()} mapAfterInit={loadedWorld.MapWidth}x{loadedWorld.MapHeight}");

            if (!WorldSaveLoader.TryApplyObjectiveWorld(loadedWorld, data, out error))
            {
                Debug.LogError(
                    $"[WorldSnapshotLoadDiag][SimulationHost] WorldSaveLoader.TryApplyObjectiveWorld FAILED " +
                    $"worldHash={loadedWorld.GetHashCode()} error='{error}'");
                loadedWorld = null;
                return false;
            }

            Debug.Log(
                $"[WorldSnapshotLoadDiag][SimulationHost] WorldSaveLoader.TryApplyObjectiveWorld OK " +
                $"worldHash={loadedWorld.GetHashCode()} npcCount={CountNpcsForDiagnostics(loadedWorld)} " +
                $"objectCount={CountObjectsForDiagnostics(loadedWorld)}");

            loadedWorld.RebuildLandmarksBootstrap();
            return true;
        }

        // =============================================================================
        // CountNpcsForDiagnostics
        // =============================================================================
        /// <summary>
        /// <para>
        /// Conta gli NPC per i log diagnostici del load snapshot senza introdurre nuova
        /// logica di persistenza o dipendenze verso la UI.
        /// </para>
        ///
        /// <para><b>Diagnostica non autoritativa</b></para>
        /// <para>
        /// Il conteggio usa <c>NpcDna</c> come registro pratico degli NPC materializzati
        /// nel <see cref="World"/>. Serve solo a capire se il DTO e' stato applicato e
        /// se lo swap cambia davvero il runtime osservato.
        /// </para>
        /// </summary>
        private static int CountNpcsForDiagnostics(World world)
        {
            return world?.NpcDna != null ? world.NpcDna.Count : 0;
        }

        // =============================================================================
        // CountObjectsForDiagnostics
        // =============================================================================
        /// <summary>
        /// <para>
        /// Conta gli oggetti materializzati nel <see cref="World"/> per il tracciamento
        /// del percorso runtime di load.
        /// </para>
        /// </summary>
        private static int CountObjectsForDiagnostics(World world)
        {
            return world?.Objects != null ? world.Objects.Count : 0;
        }

        // =============================================================================
        // CountArrayForDiagnostics
        // =============================================================================
        /// <summary>
        /// <para>
        /// Conta in modo null-safe le sezioni array del DTO snapshot nei log
        /// diagnostici.
        /// </para>
        /// </summary>
        private static int CountArrayForDiagnostics<T>(T[] values)
        {
            return values != null ? values.Length : 0;
        }

        // =============================================================================
        // CreateWorldFromGameParams
        // =============================================================================
        /// <summary>
        /// <para>
        /// Crea un <see cref="World"/> nuovo partendo da <c>game_params.json</c>,
        /// senza seedare NPC/oggetti e senza caricare scenari.
        /// </para>
        ///
        /// <para><b>Principio architetturale: creazione World separata da seed e load</b></para>
        /// <para>
        /// Il costruttore del <c>World</c> usa i parametri simulativi di base, ma
        /// non deve decidere se il runtime partira' da baseline neutral, scenario
        /// legacy o snapshot. I JSON runtime aggiuntivi vengono caricati subito
        /// dopo tramite <see cref="LoadWorldRuntimeJsonConfig"/> e i seed restano
        /// confinati in <see cref="EnsureSeeded"/>.
        /// </para>
        /// </summary>
        private World CreateWorldFromGameParams()
        {
            var simParams = Arcontio.Core.Config.SimulationParamsLoader.LoadFromResources("Arcontio/Config/game_params");
            return new World(new WorldConfig(simParams));
        }

        // =============================================================================
        // LoadWorldRuntimeJsonConfig
        // =============================================================================
        /// <summary>
        /// <para>
        /// Carica nel <see cref="World"/> le configurazioni JSON runtime che non sono
        /// seed di scenario.
        /// </para>
        ///
        /// <para><b>Principio architetturale: config non e' scenario</b></para>
        /// <para>
        /// Questi loader preparano database oggetti, needs e belief tuning. Non
        /// creano NPC, non creano oggetti e non devono sovrascrivere uno snapshot
        /// gia' applicato. Per questo vengono chiamati prima del load DTO e prima
        /// dei seed, mai dopo.
        /// </para>
        /// </summary>
        private static void LoadWorldRuntimeJsonConfig(World world)
        {
            if (world == null)
                return;

            ObjectDatabaseLoader.LoadIntoWorld(world);
            NeedsConfigLoader.LoadIntoWorld(world);
            BeliefDecayConfigLoader.LoadIntoWorld(world);
            BeliefQueryConfigLoader.LoadIntoWorld(world);
        }

        // =============================================================================
        // ResetTransientRuntimeAfterWorldSnapshotLoad
        // =============================================================================
        /// <summary>
        /// <para>
        /// Ripulisce stato transitorio dell'host dopo uno swap di <see cref="World"/>
        /// da snapshot.
        /// </para>
        ///
        /// <para><b>Principio architetturale: snapshot world-level, non replay dei buffer</b></para>
        /// <para>
        /// Eventi, comandi interni, sistemi schedulati nel buffer e accumulatore di
        /// tempo reale appartengono al runtime precedente, non allo snapshot. Dopo
        /// un load tecnico v0.10 vengono quindi svuotati per impedire che comandi o
        /// eventi pendenti mutino subito il nuovo mondo caricato.
        /// </para>
        /// </summary>
        private void ResetTransientRuntimeAfterWorldSnapshotLoad()
        {
            _toRun.Clear();
            _commands.Clear();
            _externalCommands.Clear();
            _eventBuffer.Clear();
            _accum = 0f;
            _bus = new MessageBus();
            _telemetry = new Telemetry();
            _npcCommunication = new NpcCommunicationPipeline(contactRadius: 2, topN: 6);

            if (_memoryEncoding != null)
                _memoryEncoding.SetEventsBuffer(_eventBuffer);
        }

        // =============================================================================
        // RestoreTickFromWorldSnapshot
        // =============================================================================
        /// <summary>
        /// <para>
        /// Ripristina il tempo globale del simulatore dopo l'applicazione di uno
        /// snapshot canonico.
        /// </para>
        ///
        /// <para><b>Principio architetturale: authority temporale unica</b></para>
        /// <para>
        /// <c>SimulationHost</c> resta l'unica sorgente autorevole del tick. Il DTO
        /// salva il valore di <c>_tickIndex</c>, cioe' il prossimo tick che verra'
        /// eseguito da <see cref="StepOneTick"/>. Per questo il restore assegna
        /// esattamente <c>savedAtTick</c> e non <c>savedAtTick + 1</c>: al primo
        /// avanzamento post-load, <see cref="TickContext.BeginTick(long)"/> verra'
        /// chiamato con lo stesso numero salvato, poi il normale fine tick
        /// incrementera' il contatore come sempre.
        /// </para>
        ///
        /// <para><b>Struttura interna:</b></para>
        /// <list type="bullet">
        ///   <item><b>Clamp difensivo</b>: valori negativi vengono portati a zero.</item>
        ///   <item><b>SimulationHost</b>: aggiorna <c>_tickIndex</c>, usato dallo scheduler.</item>
        ///   <item><b>TickContext</b>: riallinea il contesto letto da log, sistemi e comandi fuori tick.</item>
        ///   <item><b>World.Global</b>: riallinea il mirror globale legacy se presente.</item>
        /// </list>
        /// </summary>
        private void RestoreTickFromWorldSnapshot(long savedAtTick)
        {
            long restoredTick = savedAtTick;
            if (restoredTick < 0)
                restoredTick = 0;

            _tickIndex = restoredTick;
            TickContext.BeginTick(_tickIndex);

            if (_world != null)
                _world.Global.CurrentTickIndex = _tickIndex;
        }

        private void SeedTestWorld()
        {
            // IMPORTANTISSIMO:
            // - La baseline runtime costituzionale NON deve partire con uno scenario legacy implicito.
            // - enableLegacyDebugScenarioBootstrap e' il gate esplicito che autorizza i bootstrap legacy.
            // - debugScenario da solo NON basta, cosi' eventuali valori serializzati vecchi non riattivano
            //   automaticamente un DayX in startup.
            // - Se il gate non e' attivo, il runtime standard parte con un seed neutro minimo.
            DebugScenario selectedScenario = enableLegacyDebugScenarioBootstrap ? debugScenario : DebugScenario.None;

            switch (selectedScenario)
            {
                case DebugScenario.None:
                    Seed_BaselineNeutral();
                    break;

                case DebugScenario.Day6_Assimilation:
                    Seed_Day6();
                    break;

                case DebugScenario.Day7_Delivery:
                    Seed_Day7();
                    break;

                case DebugScenario.Day8_ObjectPerception:
                    Seed_Day8();
                    break;

                case DebugScenario.Day9_NeedsOwnership:
                    Seed_Day9();
                    break;

                case DebugScenario.Day10_Move_Memory_Theft:
                    Seed_Day10();
                    break;
                
                case DebugScenario.P0_02_Landmark_PathFinding:
                    Seed_P0_02_Landmark_PathFinding();
                    break;

                case DebugScenario.FromScenarioFile:
                    Seed_FromScenarioFile();
                    break;

                default:
                    Seed_BaselineNeutral();
                    break;
            }
        }

        // ============================================================
        // BASELINE RUNTIME NEUTRA (CONSTITUTIONAL PATH)
        // ============================================================
        // IMPORTANTISSIMO:
        // - Questo e' l'unico bootstrap implicito consentito per il runtime standard.
        // - Non introduce scenari DayX, debug stimulus o setup test-driven.
        // - Serve solo a garantire un mondo avviabile con un NPC minimale per tick, bisogni,
        //   percezione, pannelli runtime e devtools.
        // - Lo spawn neutro NON deve stare su un bordo: il centro mappa riduce edge effects
        //   su percezione, debug visuale e pannelli runtime.
        // - Resta un seed minimale costituzionale, non uno scenario debug.
        private void Seed_BaselineNeutral()
        {
            var needs = NpcNeeds.Default();
            needs.SetValue(NeedKind.Hunger, 0.1f);
            needs.SetValue(NeedKind.Rest, 0.1f);

            int spawnX = _world.MapWidth / 2;
            int spawnY = _world.MapHeight / 2;

            int npcId = _world.CreateNpc(
                NpcDnaProfile.CreateDefault("NPC_Runtime"),
                needs,
                new Social { LeadershipScore = 0.2f, LoyaltyToLeader01 = 0.5f, JusticePerception01 = 0.5f },
                spawnX,
                spawnY);

            _world.SetFacing(npcId, CardinalDirection.North);
        }

        private void Seed_Day6()
        {

            // Per ora la gestione delle regioni come memoria spaziale è inserita come progetto ma non implementata
            _world.Global.EnableMemorySpatialFusion = false;
            _world.Global.MemoryRegionSizeCells = 4;

            // Token budget alto per test
            _world.Global.MaxTokensPerEncounter = 2;
            _world.Global.MaxTokensPerNpcPerDay = 50;
            _world.Global.RepeatShareCooldownTicks = 0;

            // Delivery ?neutro?: nessun muro, LOS off (così non blocchi per caso)
            _world.Global.TokenDeliveryMaxRangeCells = 10;
            _world.Global.EnableTokenLOS = false;

            // Falloff quasi nullo per rendere i valori leggibili e stabili
            _world.Global.TokenReliabilityFalloffPerCell = 0.00f;
            _world.Global.TokenIntensityFalloffPerCell = 0.00f;

            // 2 NPC vicini così emission trova contatto facilmente
            int a = CreateNpcAt(0, 0, "NPC_T6_A");
            int b = CreateNpcAt(1, 0, "NPC_T6_B");

            _world.SetFacing(a, CardinalDirection.East);
            _world.SetFacing(b, CardinalDirection.West);

            ArcontioLogger.Info(
                new LogContext(tick: (int)TickContext.CurrentTickIndex, channel: "T6"),
                new LogBlock(LogLevel.Info, "log.seed.expectation")
                    .AddField("expectation", "tick%50 PredatorSpotted -> memory -> token -> assimilation (heard trace on listener).")
            );

            int CreateNpcAt(int x, int y, string name)
            {
                var _n = NpcNeeds.Default();
                _n.SetValue(NeedKind.Hunger, 0.1f);
                _n.SetValue(NeedKind.Rest,   0.1f);
                return _world.CreateNpc(NpcDnaProfile.CreateDefault(name),
                    _n,
                    new Social { LeadershipScore = 0.2f, LoyaltyToLeader01 = 0.5f, JusticePerception01 = 0.5f },
                    x, y
                );
            }
        }

        // ============================================================
        // DAY 7: Delivery test (range/LOS + BFS shout detour)
        // ============================================================
        private void Seed_Day7()
        {
            ArcontioLogger.Info(
                new LogContext(tick: (int)TickContext.CurrentTickIndex, channel: "T7"),
                new LogBlock(LogLevel.Info, "log.seed.name")
                    .AddField("seed", "Day7_Delivery")
            );

            // Token params (Giorno 5/6/7)
            _world.Global.MaxTokensPerEncounter = 2;
            _world.Global.MaxTokensPerNpcPerDay = 50;
            _world.Global.RepeatShareCooldownTicks = 0;

            // Delivery params (Giorno 7)
            _world.Global.TokenDeliveryMaxRangeCells = 10;
            _world.Global.EnableTokenLOS = true;
            _world.Global.TokenReliabilityFalloffPerCell = 0.06f;
            _world.Global.TokenIntensityFalloffPerCell = 0.04f;

            // ============================================================
            // SCENARIO GUIDATO: 2 coppie separate da muri diversi
            //
            // Coppia A (muro corto, 1 blocco):
            //   NPC_A1 (0,0)  | muro (1,0) | NPC_A2 (2,0)
            //
            // Coppia B (muro lungo, più blocchi):
            //   NPC_B1 (0,5)  | muro lungo su x=1 da y=4..6 | NPC_B2 (2,5)
            //
            // In entrambi i casi:
            // - ProximityTalk (LOS) -> bloccato dal muro pieno.
            // - AlarmShout (BFS) -> aggira muri; muro lungo = detour maggiore => più degrado.
            // ============================================================

            // Muro corto (SetOccluder obsoleto, usa create object)
            _world.CreateObject(defId: "wall_stone", x: 11, y: 10);
            //_world.SetOccluder(1, 0, new Occluder { BlocksVision = true, BlocksMovement = true, VisionCost = 1.0f });

            // Muro lungo
            _world.CreateObject(defId: "wall_stone", x: 11, y: 14);
            _world.CreateObject(defId: "wall_stone", x: 11, y: 15);
            _world.CreateObject(defId: "wall_stone", x: 11, y: 16);
            //_world.SetOccluder(1, 4, new Occluder { BlocksVision = true, BlocksMovement = true, VisionCost = 1.0f });
            //_world.SetOccluder(1, 5, new Occluder { BlocksVision = true, BlocksMovement = true, VisionCost = 1.0f });
            //_world.SetOccluder(1, 6, new Occluder { BlocksVision = true, BlocksMovement = true, VisionCost = 1.0f });


            // 4 NPC: uno "low law" che tenderà a rubare, altri più legali.
            int a1 = CreateNpcAt(10, 10, "NPC_A1");
            int a2 = CreateNpcAt(12, 10, "NPC_A2");
            int b1 = CreateNpcAt(10, 15, "NPC_B1");
            int b2 = CreateNpcAt(12, 15, "NPC_B2");

            _world.SetFacing(a1, CardinalDirection.East);
            _world.SetFacing(a2, CardinalDirection.West);
            _world.SetFacing(b1, CardinalDirection.West);
            _world.SetFacing(b2, CardinalDirection.West);

            ArcontioLogger.Info(
                new LogContext(tick: (int)TickContext.CurrentTickIndex, channel: "T7"),
                new LogBlock(LogLevel.Info, "log.seed.expectation")
                    .AddField("expectation", "every 50 ticks inject AlarmShout (1->2 and 3->4). Delivery logs show different dist/deg for short vs long wall.")
            );

            int CreateNpcAt(int x, int y, string name)
            {
                var _n = NpcNeeds.Default();
                _n.SetValue(NeedKind.Hunger, 0.1f);
                _n.SetValue(NeedKind.Rest,   0.1f);
                return _world.CreateNpc(NpcDnaProfile.CreateDefault(name),
                    _n,
                    new Social { LeadershipScore = 0.2f, LoyaltyToLeader01 = 0.5f, JusticePerception01 = 0.5f },
                    x, y
                );
            }
        }

        // ============================================================
        // Object perception test (cone FOV + ObjectSpottedEvent -> memory)
        // ============================================================
        private void Seed_Day8()
        {
            ArcontioLogger.Info(
                new LogContext(tick: (int)TickContext.CurrentTickIndex, channel: "T8"),
                new LogBlock(LogLevel.Info, "log.seed.name")
                    .AddField("seed", "Day8_ObjectPerception")
            );

            // Token systems possono restare attivi, ma qui il focus è:
            // ObjectPerceptionSystem -> ObjectSpottedEvent -> ObjectSpottedMemoryRule
            _world.Global.MaxTokensPerEncounter = 0; // opzionale: ?spengo? token per non inquinare log
            _world.Global.MaxTokensPerNpcPerDay = 0;
            _world.Global.RepeatShareCooldownTicks = 0;

            // 1 NPC che guarda a Est
            int npc = _world.CreateNpc(NpcDnaProfile.CreateDefault("NPC_T8"),
                NpcNeeds.Make(0.1f, 0.1f),
                new Social { LeadershipScore = 0.2f, LoyaltyToLeader01 = 0.5f, JusticePerception01 = 0.5f },
                32, 0
            );

            _world.SetFacing(npc, CardinalDirection.East);

            // Piazziamo oggetti nel cono:
            // - davanti (2,0) -> deve essere visto
            // - diagonale davanti (2,1) -> deve essere visto (cono)
            // - dietro ( -2,0 ) -> NON deve essere visto
            _world.CreateObject(defId: "bed_wood_poor", x: 2, y: 0, ownerKind: OwnerKind.Npc, ownerId: npc);
            _world.CreateObject(defId: "workbench_basic", x: 2, y: 1, ownerKind: OwnerKind.Community, ownerId: 0);
            _world.CreateObject(defId: "chair_basic", x: -2, y: 0, ownerKind: OwnerKind.Community, ownerId: 0);

            ArcontioLogger.Debug(
                new LogContext(tick: (int)TickContext.CurrentTickIndex, channel: "T8"),
                new LogBlock(LogLevel.Debug, "log.t8.info0")
                    .AddField("objects", _world.Objects.Count)
                    .AddField("vision", _world.Global.NpcVisionRangeCells)
                    .AddField("cone", _world.Global.NpcVisionConeHalfWidthPerStep.ToString("0.00"))
            );
        }

        /// <summary>
        /// LogDay8Snapshot:
        /// Log sintetico solo nel seed Day8:
        /// - elenca gli oggetti visibili per NPC_1 in questo momento (cone + range)
        /// - ti permette di validare rapidamente ?bed/workbench sì, chair dietro no?.
        ///
        /// Nota:
        /// qui NON usiamo eventi: è una "sonda" di debug.
        /// Serve a capire se la geometria di FOV è corretta senza rincorrere 100 eventi.
        /// </summary>
        private void LogDay8Snapshot(Tick tick)
        {
            // Assunzione test: esiste almeno npcId=1
            int npcId = 1;
            if (!_world.GridPos.TryGetValue(npcId, out var np))
            {
                ArcontioLogger.Warn(
                    new LogContext(tick: tick.Index, channel: "T8"),
                    new LogBlock(LogLevel.Warn, "log.t8.snap.missing_pos")
                        .AddField("npc", "npc1")
                );
                return;
            }

            if (!_world.NpcFacing.TryGetValue(npcId, out var facing))
                facing = CardinalDirection.North;

            int vision = _world.Global.NpcVisionRangeCells <= 0 ? 6 : _world.Global.NpcVisionRangeCells;
            float cone = _world.Global.NpcVisionConeHalfWidthPerStep;
            if (cone < 0f) cone = 0f;

            // raccogli defId in set (no duplicati)
            var seen = new HashSet<string>();

            foreach (var kv in _world.Objects)
            {
                var obj = kv.Value;
                if (obj == null) continue;

                int dist = Mathf.Abs(obj.CellX - np.X) + Mathf.Abs(obj.CellY - np.Y);
                if (dist > vision) continue;

                // Riusa la stessa logica del sistema (replicata qui per debug)
                // Patch 0.02.5A: IsInCone_Debug rimosso — delega a FovUtils.IsInCone.
                // FovUtils è la fonte canonica unica del cono in tutto il progetto.
                if (!FovUtils.IsInCone(np.X, np.Y, facing, obj.CellX, obj.CellY, cone))
                    continue;

                seen.Add(obj.DefId);
            }

            string list = (seen.Count == 0) ? "<none>" : string.Join(", ", seen);
            ArcontioLogger.Trace(
                new LogContext(tick: tick.Index, channel: "T8"),
                new LogBlock(LogLevel.Trace, "log.t8.snap.details")
                    .AddField("npc", "npc1")
                    .AddField("pos", $"({np.X},{np.Y})")
                    .AddField("facing", facing)
                    .AddField("vision", vision)
                    .AddField("cone", cone.ToString("0.00"))
                    .AddField("seesCount", seen.Count)
                    .AddField("list", list)
            );
        }

        private void Seed_Day9()
        {
            ArcontioLogger.Info(
                new LogContext(tick: (int)TickContext.CurrentTickIndex, channel: "T9"),
                new LogBlock(LogLevel.Info, "log.seed.name")
                    .AddField("seed", "Day9_Needs_Food_Beds_Theft")
            );

            // --- Needs config già caricata da JSON ---
            // Se vuoi forzare per test (override):
            // _world.Global.Needs = NeedsConfig.Default();

            // Disattivo token per non inquinare log del day9
            _world.Global.MaxTokensPerEncounter = 0;
            _world.Global.MaxTokensPerNpcPerDay = 0;
            _world.Global.RepeatShareCooldownTicks = 0;

            // ============================================================
            // SCENARIO:
            // - NPC1 (id=1) e NPC2 (id=2)
            // - 2 letti (1 community, 1 owned da NPC2)
            // - 1 stock cibo libero visibile
            // - 4 cibo privato di NPC2
            // - 1 stock cibo libero nascosto da occluder (non visibile a NPC1)
            //
            // Aspettative:
            // - NPC1 vede e ricorda i letti (nel cono).
            // - NPC2 consuma prima cibo privato.
            // - NPC1 consuma stock libero; finito, può rubare da NPC2 se lawfulness basso
            // - NPC1 sceglie letto community se libero.
            // ============================================================

            // Occluder per ?nascondere? stock libero #2 a NPC1 (blocca LOS percezione)
            // Nota: questo funziona solo se in ObjectPerceptionSystem hai check LOS sugli occluder.
            //_world.SetOccluder(2, 1, new Occluder { BlocksVision = true, BlocksMovement = true, VisionCost = 1f });

            int wall = _world.CreateObject(defId: "wall_stone", x: 22, y: 23);
            ArcontioLogger.Debug(new LogContext(0, "T9"),
                                 new LogBlock(LogLevel.Debug, "object.spawn")
                                 .AddField("obj", "wall_stone")
                                 .AddField("id", wall));
            wall = _world.CreateObject(defId: "wall_stone", x: 22, y: 22);
            ArcontioLogger.Debug(new LogContext(0, "T9"),
                                 new LogBlock(LogLevel.Debug, "object.spawn")
                                 .AddField("obj", "wall_stone")
                                 .AddField("id", wall));
            wall = _world.CreateObject(defId: "wall_stone", x: 22, y: 21);
            ArcontioLogger.Debug(new LogContext(0, "T9"),
                                 new LogBlock(LogLevel.Debug, "object.spawn")
                                 .AddField("obj", "wall_stone")
                                 .AddField("id", wall));
            wall = _world.CreateObject(defId: "wall_stone", x: 22, y: 20);
            ArcontioLogger.Debug(new LogContext(0, "T9"),
                                 new LogBlock(LogLevel.Debug, "object.spawn")
                                 .AddField("obj", "wall_stone")
                                 .AddField("id", wall));
            wall = _world.CreateObject(defId: "wall_stone", x: 22, y: 19);
            ArcontioLogger.Debug(new LogContext(0, "T9"),
                                 new LogBlock(LogLevel.Debug, "object.spawn")
                                 .AddField("obj", "wall_stone")
                                 .AddField("id", wall));
            wall = _world.CreateObject(defId: "wall_stone", x: 22, y: 18);
            ArcontioLogger.Debug(new LogContext(0, "T9"),
                                 new LogBlock(LogLevel.Debug, "object.spawn")
                                 .AddField("obj", "wall_stone")
                                 .AddField("id", wall));
            wall = _world.CreateObject(defId: "wall_stone", x: 22, y: 17);
            ArcontioLogger.Debug(new LogContext(0, "T9"),
                                 new LogBlock(LogLevel.Debug, "object.spawn")
                                 .AddField("obj", "wall_stone")
                                 .AddField("id", wall));
            wall = _world.CreateObject(defId: "wall_stone", x: 23, y: 17);
            ArcontioLogger.Debug(new LogContext(0, "T9"),
                                 new LogBlock(LogLevel.Debug, "object.spawn")
                                 .AddField("obj", "wall_stone")
                                 .AddField("id", wall));
            wall = _world.CreateObject(defId: "wall_stone", x: 24, y: 17);
            ArcontioLogger.Debug(new LogContext(0, "T9"),
                                 new LogBlock(LogLevel.Debug, "object.spawn")
                                 .AddField("obj", "wall_stone")
                                 .AddField("id", wall));
            wall = _world.CreateObject(defId: "wall_stone", x: 25, y: 17);
            ArcontioLogger.Debug(new LogContext(0, "T9"),
                                 new LogBlock(LogLevel.Debug, "object.spawn")
                                 .AddField("obj", "wall_stone")
                                 .AddField("id", wall));
            wall = _world.CreateObject(defId: "wall_stone", x: 26, y: 17);
            ArcontioLogger.Debug(new LogContext(0, "T9"),
                                 new LogBlock(LogLevel.Debug, "object.spawn")
                                 .AddField("obj", "wall_stone")
                                 .AddField("id", wall));

            // NPC1: basso rispetto legge (ruberà ?facile?)
            int npc1 = _world.CreateNpc(NpcDnaProfile.CreateDefault("NPC1"),
                NpcNeeds.Make(0.85f, 0.85f),
                new Social { LeadershipScore = 0.2f, LoyaltyToLeader01 = 0.5f, JusticePerception01 = 0.20f },
                x: 20, y: 20
            );

            // NPC2: alto rispetto legge (non ruba; ha cibo privato)
            int npc2 = _world.CreateNpc(NpcDnaProfile.CreateDefault("NPC2"),
                NpcNeeds.Make(0.80f, 0.40f),
                new Social { LeadershipScore = 0.2f, LoyaltyToLeader01 = 0.5f, JusticePerception01 = 0.90f },
                x: 20, y: 22
            );

            // NPC3: alto rispetto legge (non ruba; ha cibo privato)
            int npc3 = _world.CreateNpc(NpcDnaProfile.CreateDefault("NPC3"),
                NpcNeeds.Make(0.80f, 0.40f),
                new Social { LeadershipScore = 0.2f, LoyaltyToLeader01 = 0.5f, JusticePerception01 = 0.90f },
                x: 28, y: 19
            );
            // Facing: NPC1 guarda verso "su" dove mettiamo letti/food
            _world.SetFacing(npc1, CardinalDirection.North);
            _world.SetFacing(npc2, CardinalDirection.North);
            _world.SetFacing(npc3, CardinalDirection.West);

            // --- Oggetti: letti ---
            int bedCommunity = _world.CreateObject(defId: "bed_wood_poor", x: 20, y: 23, ownerKind: OwnerKind.Community, ownerId: 0);
            _world.ObjectUse[bedCommunity] = ObjectUseState.Free();

            ArcontioLogger.Debug(new LogContext(0, "T9"),
               new LogBlock(LogLevel.Debug, "object.spawn")
                  .AddField("obj", "bed_wood_poor")
                  .AddField("units", 1));

            int bedNpc2 = _world.CreateObject(defId: "bed_wood_good", x: 21, y: 23, ownerKind: OwnerKind.Npc, ownerId: npc2);
            _world.ObjectUse[bedNpc2] = ObjectUseState.Free();

            ArcontioLogger.Debug(new LogContext(0, "T9"),
               new LogBlock(LogLevel.Debug, "object.spawn")
                  .AddField("obj", "bed_wood_good")
                  .AddField("units", 1));

            // --- Cibo: stock libero visibile (davanti a NPC1) ---
            int foodFreeVisible = _world.CreateObject(defId: "food_stock", x: 20, y: 21, ownerKind: OwnerKind.Community, ownerId: 0);
            _world.SetFoodStock(foodFreeVisible, new FoodStockComponent { Units = 5, OwnerKind = OwnerKind.Community, OwnerId = 0 });

            ArcontioLogger.Debug(new LogContext(0, "T9"),
               new LogBlock(LogLevel.Debug, "object.spawn")
                  .AddField("obj", "food_stock visible")
                  .AddField("units", 5));

            // --- Cibo: stock libero nascosto (dietro muro/occluder) ---
            int foodFreeHidden = _world.CreateObject(defId: "food_stock", x: 23, y: 22, ownerKind: OwnerKind.Community, ownerId: 0);
            _world.SetFoodStock(foodFreeHidden, new FoodStockComponent { Units = 3, OwnerKind = OwnerKind.Community, OwnerId = 0 });

            ArcontioLogger.Debug(new LogContext(0, "T9"),
                new LogBlock(LogLevel.Debug, "object.spawn")
                   .AddField("obj", "food_stock hidden")
                   .AddField("units", 3));

            // --- Cibo privato NPC2 ---
            _world.NpcPrivateFood[npc2] = 4;

            // --- Cibo privato NPC3 ---
            _world.NpcPrivateFood[npc3] = 4;

            ArcontioLogger.Info(new LogContext(0, "T9"),
                new LogBlock(LogLevel.Info, "log.t9.seed.setup_done"));

            ArcontioLogger.Info(new LogContext(0, "T9"),
                new LogBlock(LogLevel.Info, "log.t9.seed.npc1")
                    .AddField("id", npc1)
                    .AddField("pos", "(0,0)")
                    .AddField("law", _world.Social[npc1].JusticePerception01.ToString("0.00"))
                    .AddField("hunger", _world.Needs[npc1].GetValue(NeedKind.Hunger).ToString("0.00"))
                    .AddField("rest",   _world.Needs[npc1].GetValue(NeedKind.Rest).ToString("0.00"))
            );

            ArcontioLogger.Info(new LogContext(0, "T9"),
                new LogBlock(LogLevel.Info, "log.t9.seed.npc2")
                    .AddField("id", npc2)
                    .AddField("pos", "(0,2)")
                    .AddField("law", _world.Social[npc2].JusticePerception01.ToString("0.00"))
                    .AddField("privateFood", _world.NpcPrivateFood[npc2])
                    .AddField("hunger", _world.Needs[npc2].GetValue(NeedKind.Hunger).ToString("0.00"))
            );

            ArcontioLogger.Info(new LogContext(0, "T9"),
                new LogBlock(LogLevel.Info, "log.t9.seed.objects")
                    .AddField("bedCommunity", bedCommunity)
                    .AddField("bedNpc2", bedNpc2)
                    .AddField("foodFreeVisible", foodFreeVisible)
                    .AddField("foodFreeHidden", foodFreeHidden)
            );
        }

        // ============================================================
        // DAY 10: Movement + Theft from Stock + Witness (LOS)
        // ============================================================
        private void Seed_Day10()
        {
            ArcontioLogger.Info(
                new LogContext(tick: (int)TickContext.CurrentTickIndex, channel: "T10"),
                new LogBlock(LogLevel.Info, "log.seed.name")
                    .AddField("seed", "Day10_Move_Memory_Theft")
            );

            // Disattivo token: qui testiamo movement/LOS/furti, non comunicazione.
            /*_world.Global.MaxTokensPerEncounter = 0;
            _world.Global.MaxTokensPerNpcPerDay = 0;
            _world.Global.RepeatShareCooldownTicks = 0;*/

            // ============================================================
            // Scenario richiesto:
            // A) almeno 5 NPC
            // B) cibo community + cibo privato a terra (non addosso)
            // C) muri sparsi per furti senza sospetti / cibo non visto
            // ============================================================

            // Muri sparsi (occluder) - piccoli "blocchi" che spezzano LOS.
            _world.CreateObject(defId: "wall_stone", x: 10, y: 10);
            _world.CreateObject(defId: "wall_stone", x: 11, y: 10);
            _world.CreateObject(defId: "wall_stone", x: 12, y: 10);
            _world.CreateObject(defId: "wall_stone", x: 12, y: 11);

            _world.CreateObject(defId: "wall_stone", x: 16, y: 14);
            _world.CreateObject(defId: "wall_stone", x: 16, y: 15);

            // 5 NPC: uno "low law" che tenderà a rubare, altri più legali.
            int npc1 = _world.CreateNpc(NpcDnaProfile.CreateDefault("T10_NPC1_Thief"),
                NpcNeeds.Make(0.90f, 0.20f),
                new Social { LeadershipScore = 0.2f, LoyaltyToLeader01 = 0.5f, JusticePerception01 = 0.20f },
                x: 5, y: 5
            );

            int npc2 = _world.CreateNpc(NpcDnaProfile.CreateDefault("T10_NPC2_Owner"),
                NpcNeeds.Make(0.40f, 0.20f),
                new Social { LeadershipScore = 0.2f, LoyaltyToLeader01 = 0.5f, JusticePerception01 = 0.90f },
                x: 18, y: 12
            );

            int npc3 = _world.CreateNpc(NpcDnaProfile.CreateDefault("T10_NPC3_Witness"),
                NpcNeeds.Make(0.30f, 0.20f),
                new Social { LeadershipScore = 0.2f, LoyaltyToLeader01 = 0.5f, JusticePerception01 = 0.70f },
                x: 14, y: 12
            );

            int npc4 = _world.CreateNpc(NpcDnaProfile.CreateDefault("T10_NPC4"),
                NpcNeeds.Make(0.60f, 0.20f),
                new Social { LeadershipScore = 0.2f, LoyaltyToLeader01 = 0.5f, JusticePerception01 = 0.60f },
                x: 22, y: 16
            );

            int npc5 = _world.CreateNpc(NpcDnaProfile.CreateDefault("T10_NPC5"),
                NpcNeeds.Make(0.55f, 0.20f),
                new Social { LeadershipScore = 0.2f, LoyaltyToLeader01 = 0.5f, JusticePerception01 = 0.60f },
                x: 6, y: 16
            );

            _world.SetFacing(npc1, CardinalDirection.East);
            _world.SetFacing(npc2, CardinalDirection.West);
            _world.SetFacing(npc3, CardinalDirection.West);
            _world.SetFacing(npc4, CardinalDirection.West);
            _world.SetFacing(npc5, CardinalDirection.East);

            // Cibo community "in chiaro" (legale)
            int foodCommunity = _world.CreateObject(defId: "food_stock", x: 9, y: 8, ownerKind: OwnerKind.Community, ownerId: 0);
            _world.SetFoodStock(foodCommunity, new FoodStockComponent { Units = 3, OwnerKind = OwnerKind.Community, OwnerId = 0 });

            // Cibo privato a terra (il caso nuovo Day10)
            // - uno visibile con potenziale witness
            // - uno "dietro muro" per testare furto senza sospetti
            int foodPrivateVisible = _world.CreateObject(defId: "food_stock", x: 17, y: 12, ownerKind: OwnerKind.Npc, ownerId: npc2);
            _world.SetFoodStock(foodPrivateVisible, new FoodStockComponent { Units = 4, OwnerKind = OwnerKind.Npc, OwnerId = npc2 });

            int foodPrivateHidden = _world.CreateObject(defId: "food_stock", x: 12, y: 9, ownerKind: OwnerKind.Npc, ownerId: npc2);
            _world.SetFoodStock(foodPrivateHidden, new FoodStockComponent { Units = 2, OwnerKind = OwnerKind.Npc, OwnerId = npc2 });
           
            // --- Cibo privato NPC5 ---
            _world.NpcPrivateFood[npc5] = _world.Global.InventoryMaxUnits;


            ArcontioLogger.Info(
                new LogContext(tick: (int)TickContext.CurrentTickIndex, channel: "T10"),
                new LogBlock(LogLevel.Info, "log.seed.expectation")
                    .AddField("expectation", "NPC1 cerca cibo community; esaurito -> può rubare stock privato a terra. Se NPC3 ha LOS dovrebbe generare TheftWitnessed."));

 /*           ArcontioLogger.Info(new LogContext(0, "T10"),
                new LogBlock(LogLevel.Info, "log.seed.expectatio")
                    .AddField("foodCommunity", foodCommunity)
                    .AddField("foodPrivateVisible ", foodPrivateVisible)
                    .AddField("foodPrivateHidden ", foodPrivateHidden));*/

        }
        // ============================================================
        // LEGACY DEBUG / NON CONSTITUTIONAL RUNTIME PATH - SCENARIO DA FILE (v0.04.07.b)
        // ============================================================
        // Legge gli NPC da Resources/Arcontio/Scenarios/default_scenario.json.
        // Se il file non esiste o è vuoto, fa fallback a Seed_P0_02_Landmark_PathFinding.
        // Il file di scenario non contiene oggetti/muri: quelli restano hardcoded
        // oppure verranno spostati in un formato separato in una sessione futura.
        private void Seed_FromScenarioFile()
        {
            ArcontioLogger.Info(
                new LogContext(tick: 0, channel: "ScenarioLoader"),
                new LogBlock(LogLevel.Info, "log.seed.name")
                    .AddField("seed", "Seed_FromScenarioFile")
                    .AddField("path", "Resources/Arcontio/Scenarios/default_scenario")
            );

            bool loaded = NpcScenarioLoader.TryLoadDefaultAndSpawn(_world);

            if (!loaded)
            {
                ArcontioLogger.Info(
                    new LogContext(tick: 0, channel: "ScenarioLoader"),
                    new LogBlock(LogLevel.Info, "log.scenario.fallback")
                        .AddField("reason", "default_scenario.json non trovato — fallback a P0_02")
                );
                Seed_P0_02_Landmark_PathFinding();
            }
        }

        // ============================================================
        // LEGACY DEBUG / NON CONSTITUTIONAL RUNTIME PATH - 0.02 LANDMARK PATHFINDING SCENARIO
        // ============================================================
        private void Seed_P0_02_Landmark_PathFinding()
        {
            ArcontioLogger.Info(
                new LogContext(tick: (int)TickContext.CurrentTickIndex, channel: "P0_02"),
                new LogBlock(LogLevel.Info, "log.seed.name")
                    .AddField("seed", "Seed_P0_02_Landmark_PathFinding")
            );

            // Disattivo token: qui testiamo movement/LOS/furti, non comunicazione.
            /*_world.Global.MaxTokensPerEncounter = 0;
            _world.Global.MaxTokensPerNpcPerDay = 0;
            _world.Global.RepeatShareCooldownTicks = 0;*/

            // ============================================================
            // Scenario richiesto:
            //#########
            //#       #
            //#   #   #
            //#   #   #
            //#---+---#
            //#   #   #
            //#   #   #
            //#       #
            //#########
            // Legenda:
            //    # = muro
            //    spazio = cella navigabile
            //    + = junction(4 uscite)
            //    - = corridoio
            //    porta su uno dei corridoi
            // ============================================================

            // Muri sparsi (occluder) - piccoli "blocchi" che spezzano LOS.
            _world.CreateObject(defId: "wall_stone", x: 31, y: 20);
            _world.CreateObject(defId: "wall_stone", x: 31, y: 21);
            //_world.CreateObject(defId: "wall_stone", x: 31, y: 22);
            _world.CreateObject(defId: "wall_stone", x: 31, y: 23);
            _world.CreateObject(defId: "wall_stone", x: 31, y: 24);
    
            _world.CreateObject(defId: "wall_stone", x: 33, y: 20);
            _world.CreateObject(defId: "wall_stone", x: 33, y: 21);
            //_world.CreateObject(defId: "wall_stone", x: 33, y: 22);
            _world.CreateObject(defId: "wall_stone", x: 33, y: 23);
            _world.CreateObject(defId: "wall_stone", x: 33, y: 24);

            _world.CreateObject(defId: "wall_stone", x: 30, y: 21);
            //_world.CreateObject(defId: "wall_stone", x: 32, y: 21);
            _world.CreateObject(defId: "wall_stone", x: 34, y: 21);

            _world.CreateObject(defId: "wall_stone", x: 30, y: 23);
            //_world.CreateObject(defId: "wall_stone", x: 32, y: 23);
            _world.CreateObject(defId: "wall_stone", x: 34, y: 23);

            _world.CreateObject(defId: "door_wood_good", x: 32, y: 24, ownerKind: OwnerKind.Community, ownerId: 0);

            // 5 NPC: uno "low law" che tenderà a rubare, altri più legali.
            int npc1 = _world.CreateNpc(NpcDnaProfile.CreateDefault("T10_NPC1_Thief"),
                NpcNeeds.Make(0.00f, 0.20f),
                new Social { LeadershipScore = 0.2f, LoyaltyToLeader01 = 0.5f, JusticePerception01 = 0.20f },
                x: 15, y: 15
            );
            /*
            int npc2 = _world.CreateNpc(NpcDnaProfile.CreateDefault("T10_NPC2_Owner"),
                NpcNeeds.Make(0.40f, 0.20f),
                new Social { LeadershipScore = 0.2f, LoyaltyToLeader01 = 0.5f, JusticePerception01 = 0.90f },
                x: 18, y: 12
            );

            int npc3 = _world.CreateNpc(NpcDnaProfile.CreateDefault("T10_NPC3_Witness"),
                NpcNeeds.Make(0.30f, 0.20f),
                new Social { LeadershipScore = 0.2f, LoyaltyToLeader01 = 0.5f, JusticePerception01 = 0.70f },
                x: 14, y: 12
            );

            int npc4 = _world.CreateNpc(NpcDnaProfile.CreateDefault("T10_NPC4"),
                NpcNeeds.Make(0.60f, 0.20f),
                new Social { LeadershipScore = 0.2f, LoyaltyToLeader01 = 0.5f, JusticePerception01 = 0.60f },
                x: 22, y: 16
            );

            int npc5 = _world.CreateNpc(NpcDnaProfile.CreateDefault("T10_NPC5"),
                NpcNeeds.Make(0.55f, 0.20f),
                new Social { LeadershipScore = 0.2f, LoyaltyToLeader01 = 0.5f, JusticePerception01 = 0.60f },
                x: 6, y: 16
            );

            _world.SetFacing(npc1, CardinalDirection.East);
            _world.SetFacing(npc2, CardinalDirection.West);
            _world.SetFacing(npc3, CardinalDirection.West);
            _world.SetFacing(npc4, CardinalDirection.West);
            _world.SetFacing(npc5, CardinalDirection.East);
*/
   /*         // Cibo community "in chiaro" (legale)
            int foodCommunity = _world.CreateObject(defId: "food_stock", x: 9, y: 8, ownerKind: OwnerKind.Community, ownerId: 0);
            _world.SetFoodStock(foodCommunity, new FoodStockComponent { Units = 3, OwnerKind = OwnerKind.Community, OwnerId = 0 });

            // Cibo privato a terra (il caso nuovo Day10)
            // - uno visibile con potenziale witness
            // - uno "dietro muro" per testare furto senza sospetti
            int foodPrivateVisible = _world.CreateObject(defId: "food_stock", x: 17, y: 12, ownerKind: OwnerKind.Npc, ownerId: npc2);
            _world.SetFoodStock(foodPrivateVisible, new FoodStockComponent { Units = 4, OwnerKind = OwnerKind.Npc, OwnerId = npc2 });

            int foodPrivateHidden = _world.CreateObject(defId: "food_stock", x: 12, y: 9, ownerKind: OwnerKind.Npc, ownerId: npc2);
            _world.SetFoodStock(foodPrivateHidden, new FoodStockComponent { Units = 2, OwnerKind = OwnerKind.Npc, OwnerId = npc2 });

            // --- Cibo privato NPC5 ---
            _world.NpcPrivateFood[npc5] = _world.Global.InventoryMaxUnits;
   */

     /*       ArcontioLogger.Info(
                new LogContext(tick: (int)TickContext.CurrentTickIndex, channel: "T10"),
                new LogBlock(LogLevel.Info, "log.seed.expectation")
                    .AddField("expectation", "NPC1 cerca cibo community; esaurito -> può rubare stock privato a terra. Se NPC3 ha LOS dovrebbe generare TheftWitnessed."));
     */
            /*           ArcontioLogger.Info(new LogContext(0, "T10"),
                           new LogBlock(LogLevel.Info, "log.seed.expectatio")
                               .AddField("foodCommunity", foodCommunity)
                               .AddField("foodPrivateVisible ", foodPrivateVisible)
                               .AddField("foodPrivateHidden ", foodPrivateHidden));*/

        }

        // Patch 0.02.5A: IsInCone_Debug rimosso.
        // Usava Mathf.FloorToInt invece di (int)Math.Floor — semantica equivalente
        // ma diversa dalla versione canonica. Sostituito con FovUtils.IsInCone.

        private void LateUpdate()
        {
            // flush ?soft? a fine frame (evita I/O per log write singolo)
            ArcontioLogger.Flush();
        }

        private void OnApplicationQuit()
        {
            ArcontioLogger.Shutdown();
        }
    }
}
