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

        private const string MinimalBootstrapLabel = "MinimalRuntimeBootstrap";


        // Core state
        private World _world;
        private MessageBus _bus;
        private Scheduler _scheduler;
        private Telemetry _telemetry;

        private TokenBus _tokenBusOut;                  // ?messaggi pronunciati? (non ancora consegnati)
        private TokenBus _tokenBusIn;                   // ?messaggi ricevuti? (pronti per assimilation)
        private TokenDeliveryPipeline _tokenDelivery;   // decide chi li sente davvero
        private TokenEmissionPipeline _tokenEmission;   // Decide cosa dire
        private TokenAssimilationPipeline _tokenAssim;  // Decide cosa entra in testa
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

        // buffer debug (drain token bus)
        private readonly List<TokenEnvelope> _tokenBuffer = new(256);

        private readonly List<ISimEvent> _eventBuffer = new();

        // Contatore del tick (long)
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

        // Flag per evitare seeding multipli (in caso di scene reload accidentali)
        private bool _seeded;

        // ============================================================
        // TICK CONTROL (Input System)
        // ============================================================
        [Header("Tick Control (Input System)")]
        [SerializeField] private bool startPaused = false;

        [SerializeField] private InputActionReference togglePauseAction;
        [SerializeField] private InputActionReference stepOneTickAction;
        [SerializeField] private InputActionReference stepTenTicksAction;

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
            if (FindObjectOfType<Arcontio.View.ArcontioLogOverlay>() == null)
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
            var simParams = Arcontio.Core.Config.SimulationParamsLoader.LoadFromResources("Arcontio/Config/game_params");
            _world = new World(new WorldConfig(simParams));

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
            ObjectDatabaseLoader.LoadIntoWorld(_world);

            // ******************************************************************************************************************************
            // 4.2) Carica parametri fame/sonno da JSON
            // ******************************************************************************************************************************
            NeedsConfigLoader.LoadIntoWorld(_world);

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
            // 6.1) _tokenBusIn e _tokenBusOut
            // ******************************************************************************************************************************
            // Separiamo token "pronunciati" da token "arrivati" (in e out)
            _tokenBusIn = new TokenBus();
            _tokenBusOut = new TokenBus();

            // ******************************************************************************************************************************
            // 6.2) _tokenEmission
            // ******************************************************************************************************************************
            // Token per trasformare le MemoryTrace (pensieri del NPC) in comunicazioni
            _tokenEmission = new TokenEmissionPipeline(contactRadius: 2, topN: 6);

            // ******************************************************************************************************************************
            // 6.3) _tokenDelivery
            // ******************************************************************************************************************************
            // - applica range / LOS / falloff
            // - trasferisce Out -> In
            _tokenDelivery = new TokenDeliveryPipeline();

            // ******************************************************************************************************************************
            // 6.4) _tokenAssim
            // ******************************************************************************************************************************
            // Pipeline di assimilazione
            _tokenAssim = new TokenAssimilationPipeline();

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
            IsPaused = startPaused;
            if (IsPaused) _accum = 0f;

            ArcontioLogger.Debug(
                new LogContext(tick: (int)TickContext.CurrentTickIndex, channel: "Test"),
                new LogBlock(LogLevel.Debug, "log.test.scenario")
                    .AddField("scenario", MinimalBootstrapLabel)
            );
        }

        private void Update()
        {
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
        /// <summary>
        /// Esegue e svuota la coda di comandi esterni (view -> core).
        ///
        /// Questo è un "mini command buffer" dedicato ai tool runtime (DevMode).
        /// Costituzionalmente NON appartiene al tick simulativo:
        /// è una External DevCommand Phase eseguita fuori tick, anche in pausa.
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

        // =============================================================================
        // StepOneTick
        // =============================================================================
        /// <summary>
        /// <para>
        /// Esegue un tick simulativo completo mantenendo immutato l'ordine reale della backbone runtime.
        /// Questo metodo è il punto canonico in cui viene formalizzata la Tick Phase Constitution implementativa.
        /// </para>
        ///
        /// <para><b>Tick Phase Constitution</b></para>
        /// <para>
        /// L'ordine reale delle fasi è il seguente:
        /// 1) Tick Begin
        /// 2) Scheduled Systems
        /// 3) TickPulse
        /// 4) Event Drain pre-rule
        /// 5) Memory Encoding pre-rule
        /// 6) Flush token queued + token emission/delivery/assimilation
        /// 7) Event republish per Rule Phase
        /// 8) Rule Phase
        /// 9) Command Phase
        /// 10) Event Drain post-command
        /// 11) Memory/communication pass post-command
        /// 12) Debug telemetry / tick-end bookkeeping
        /// 13) Tick Commit
        /// </para>
        ///
        /// <para><b>Principio architetturale affrontato</b></para>
        /// <para>
        /// Tick discreto e deterministico: l'host può orchestrare le fasi, ma non ne altera l'ordine implicito
        /// senza una decisione costituzionale esplicita.
        /// </para>
        /// </summary>
        private void StepOneTick()
        {
            // Rende il tick disponibile globalmente per logging in comandi ecc.
            TickContext.BeginTick(_tickIndex);
            _world.Global.CurrentTickIndex = _tickIndex;

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

            // Patch 0.01P3 extension:
            // Alcuni System (es. MemoryEncodingSystem) possono accodare token "event-driven" (es. report furto)
            // in World.QueueTokenOut(...). Qui li flushiamo nel TokenBusOut tramite il punto canonico
            // World.PublishTokenOut(...) (che fa anche log OUT + balloon).
            _world.FlushQueuedTokenOut(_tokenBusOut);

            // 3.15) Token emission (trace -> token) su pipe separata
            // Trasformiamo alcune trace importanti in TokenEnvelope e le mettiamo nel TokenBus.
            // Nota: questo NON tocca il MessageBus e NON influenza direttamente le Rule.
            //
            // Nota:
            // - questo passaggio gestisce anche eventuali token runtime già accodati prima dell'emissione standard.
            _tokenEmission.Emit(_world, tick, _tokenBusOut, _telemetry);

            // NEW Giorno 7:
            // - Delivery: Out -> In (range / LOS / falloff)
            // - Questo è il punto che evita "telepatia":
            //   il token può NON arrivare, o arrivare degradato.
            _tokenDelivery.Deliver(_world, tick, _tokenBusOut, _tokenBusIn, _telemetry);

            // Assimilation legge SOLO IN (arrivati)
            _tokenAssim.Assimilate(_world, tick, _tokenBusIn, _tokenBuffer, _telemetry);

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
                // Quindi:
                // 1) flushiamo i token accodati
                // 2) facciamo un giro aggiuntivo di Delivery + Assimilation
                int flushed = _world.FlushQueuedTokenOut(_tokenBusOut);
                if (flushed > 0)
                {
                    _tokenDelivery.Deliver(_world, tick, _tokenBusOut, _tokenBusIn, _telemetry);
                    _tokenAssim.Assimilate(_world, tick, _tokenBusIn, _tokenBuffer, _telemetry);
                }

                // opzionale: se vuoi che le rules li vedano nello stesso tick, ripubblichi:
                for (int i = 0; i < _eventBuffer.Count; i++)
                    _bus.Publish(_eventBuffer[i]);
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
        private void SeedTestWorld()
        {
            int centerX = Mathf.Max(0, _world.MapWidth / 2);
            int centerY = Mathf.Max(0, _world.MapHeight / 2);

            var needs = NpcNeeds.Default();
            needs.SetValue(NeedKind.Hunger, 0.10f);
            needs.SetValue(NeedKind.Rest, 0.10f);

            int npcId = _world.CreateNpc(
                NpcDnaProfile.CreateDefault("NPC_Runtime_1"),
                needs,
                new Social
                {
                    LeadershipScore = 0.2f,
                    LoyaltyToLeader01 = 0.5f,
                    JusticePerception01 = 0.5f
                },
                centerX,
                centerY
            );

            _world.SetFacing(npcId, CardinalDirection.North);

            ArcontioLogger.Info(
                new LogContext(tick: (int)TickContext.CurrentTickIndex, channel: "Bootstrap"),
                new LogBlock(LogLevel.Info, "log.seed.name")
                    .AddField("seed", MinimalBootstrapLabel)
            );

            ArcontioLogger.Info(
                new LogContext(tick: (int)TickContext.CurrentTickIndex, channel: "Bootstrap"),
                new LogBlock(LogLevel.Info, "log.seed.expectation")
                    .AddField("expectation", "world initialized + single NPC at map center for tick, needs, perception and runtime dev tools.")
                    .AddField("npcId", npcId)
                    .AddField("pos", $"({centerX},{centerY})")
            );
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


