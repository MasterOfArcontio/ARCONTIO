// Assets/Scripts/Core/Runtime/SimulationHost.cs
using Arcontio.Core.Diagnostics;
using Arcontio.Core.Config;
using Arcontio.Core.Environment;
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

        [Header("Debug Scenarios")]
        [SerializeField] private bool enableLegacyDebugScenarioBootstrap = false;
        [SerializeField] private DebugScenario debugScenario = DebugScenario.None;

        [Header("World Save/Load Bootstrap")]
        [SerializeField] private bool enableWorldSnapshotBootstrap = false;
        [SerializeField] private string worldSnapshotSlotName = "default";

        [Header("Job Runtime Experimental Gates")]
        // =============================================================================
        // enableFoodJobVerticalSlice
        // =============================================================================
        /// <summary>
        /// <para>
        /// Abilita il routing Job della vertical slice food/search nel runtime
        /// bootstrap ordinario.
        /// </para>
        ///
        /// <para><b>ARC-CON-014 - Decisione -> Job come default operativo</b></para>
        /// <para>
        /// Dopo le slice v0.11b.01-v0.11b.04 questo gate non rappresenta piu' un
        /// esperimento spento di default: <c>EatKnownFood</c> e <c>SearchFood</c>
        /// possiedono un percorso reale <c>DecisionCandidate -> JobRequest -> Job</c>.
        /// Tenerlo disattivato impedisce a <c>SearchFood</c> selezionato di creare
        /// un job e lascia il runtime nel fallback <c>GateDisabled</c>.
        /// </para>
        ///
        /// <para>
        /// Questa opzione mantiene attivo il primo ponte food/search verso il Job
        /// System. Dopo v0.13g non esiste piu' un fallback rule-based needs: se una
        /// route non produce job, il sistema resta senza command legacy diretto.
        /// </para>
        /// </summary>
        [SerializeField] private bool enableFoodJobVerticalSlice = true;

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
        private JobTemplateRegistry _jobTemplateRegistry;

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
        // ForceAssignMoveToCellJobFromDevTools
        // =============================================================================
        /// <summary>
        /// <para>
        /// Entry point tecnico/debug che assegna direttamente a un NPC un job runtime
        /// <c>generic.move_to_cell.v1</c> verso una cella scelta dall'operatore.
        /// </para>
        ///
        /// <para><b>Debug umano dentro Job/MoveTo</b></para>
        /// <para>
        /// Questo metodo sostituisce il vecchio click-to-move basato su
        /// <c>SetMoveIntentCommand</c>. L'ordine resta una forzatura esterna, quindi
        /// puo' usare il movimento greedy/fisico ammesso per i devtool, ma viene
        /// eseguito come job reale: passa da <c>JobRuntimeState</c>,
        /// <c>JobExecutionSystem</c> e running action <c>MoveTo</c>.
        /// </para>
        ///
        /// <para><b>Struttura interna:</b></para>
        /// <list type="bullet">
        ///   <item><b>Validazione</b>: controlla world, registry, NPC e cella target.</item>
        ///   <item><b>Factory</b>: materializza <c>generic.move_to_cell.v1</c>.</item>
        ///   <item><b>Cleanup dev</b>: cancella movimento/job precedente dell'NPC.</item>
        ///   <item><b>Assign</b>: assegna il job con debug label esplicita.</item>
        /// </list>
        /// </summary>
        public bool ForceAssignMoveToCellJobFromDevTools(int npcId, Vector2Int targetCell, out string reason)
        {
            reason = string.Empty;

            if (_world == null)
            {
                reason = "WorldMissing";
                return false;
            }

            if (_jobTemplateRegistry == null)
            {
                reason = "JobTemplateRegistryMissing";
                return false;
            }

            if (!_world.ExistsNpc(npcId))
            {
                reason = "NpcMissing";
                return false;
            }

            if (!_world.InBounds(targetCell.x, targetCell.y))
            {
                reason = "TargetOutOfBounds";
                return false;
            }

            if (!MoveJobFactory.TryCreateMoveToCellJob(
                    _jobTemplateRegistry,
                    npcId,
                    targetCell,
                    (int)_tickIndex,
                    urgency01: 1f,
                    debugLabel: MoveJobFactory.DevToolsForcedMoveToCellDebugLabel,
                    out var job,
                    out reason))
            {
                return false;
            }

            if (_world.JobRuntimeState.HasActiveJob(npcId))
                _world.JobRuntimeState.ClearNpcJob(npcId, JobFailureReason.Cancelled, out _);

            _world.ClearMoveIntent(npcId);
            _world.ClearDebugNavigationPathsForNpc(npcId);
            _world.ClearDebugMacroRouteForNpc(npcId);
            _world.ClearNpcLocalSearchState(npcId, string.Empty);
            _world.ClearNpcDirectCommitState(npcId, string.Empty);
            _world.Pathfinding.ClearMoveBackOff(npcId);

            if (!_world.JobRuntimeState.TryAssignJob(npcId, job, (int)_tickIndex, out reason))
                return false;

            reason = "DevToolsForcedMoveJobAssigned";
            return true;
        }

        // =============================================================================
        // ForceAssignTransportObjectJobFromDevTools
        // =============================================================================
        /// <summary>
        /// <para>
        /// Entry point tecnico/debug che assegna direttamente a un NPC un job runtime
        /// <c>transport.object_to_cell.v1</c>.
        /// </para>
        ///
        /// <para><b>Forced injection DevTools, non MBQD</b></para>
        /// <para>
        /// Questo metodo e' intenzionalmente fuori da Memory/Belief/Query/Decision:
        /// il pannello F3 fornisce NPC, oggetto e destinazione gia' scelti dall'utente.
        /// La scorciatoia e' ammessa solo come debug injection, ma l'esecuzione resta
        /// nel Job System reale: il job passa da <c>JobRuntimeState</c>,
        /// <c>JobExecutionSystem</c>, <c>JobCommandBuffer</c> e commands world-side.
        /// </para>
        ///
        /// <para><b>Struttura interna:</b></para>
        /// <list type="bullet">
        ///   <item><b>Validazione</b>: controlla World, registry, NPC, oggetto grounded e cella destinazione.</item>
        ///   <item><b>Factory</b>: materializza il template JSON tramite <c>TransportObjectJobFactory</c>.</item>
        ///   <item><b>Forced cancel</b>: chiude l'eventuale job corrente come <c>Cancelled</c> prima dell'assegnazione.</item>
        ///   <item><b>Assign</b>: usa comunque <c>TryAssignJob</c>, quindi non crea uno scheduler parallelo.</item>
        /// </list>
        /// </summary>
        public bool ForceAssignTransportObjectJobFromDevTools(
            int npcId,
            int objectId,
            Vector2Int destinationCell,
            out string reason)
        {
            reason = string.Empty;

            if (_world == null)
            {
                reason = "WorldMissing";
                return false;
            }

            if (_jobTemplateRegistry == null)
            {
                reason = "JobTemplateRegistryMissing";
                return false;
            }

            if (!_world.ExistsNpc(npcId))
            {
                reason = "NpcMissing";
                return false;
            }

            if (!_world.Objects.TryGetValue(objectId, out var obj) || obj == null)
            {
                reason = "ObjectMissing";
                return false;
            }

            if (obj.IsHeld)
            {
                reason = "ObjectAlreadyHeld";
                return false;
            }

            if (!_world.InBounds(destinationCell.x, destinationCell.y))
            {
                reason = "DestinationOutOfBounds";
                return false;
            }

            int existingAtDestination = _world.GetObjectAt(destinationCell.x, destinationCell.y);
            if (existingAtDestination >= 0 && existingAtDestination != objectId)
            {
                reason = "DestinationOccupied";
                return false;
            }

            var objectCell = new Vector2Int(obj.CellX, obj.CellY);
            if (!TransportObjectJobFactory.TryCreateTransportObjectToCellJob(
                    _jobTemplateRegistry,
                    npcId,
                    objectId,
                    objectCell,
                    destinationCell,
                    (int)_tickIndex,
                    out var job,
                    out reason))
            {
                return false;
            }

            if (_world.JobRuntimeState.HasActiveJob(npcId))
                _world.JobRuntimeState.ClearNpcJob(npcId, JobFailureReason.Cancelled, out _);

            if (!_world.JobRuntimeState.TryAssignJob(npcId, job, (int)_tickIndex, out reason))
                return false;

            reason = "DevToolsForcedTransportJobAssigned";
            return true;
        }

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
            ResetBiosphereRuntimeSchedule(_tickIndex);
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
        [SerializeField] private int maxRuntimeTicksPerUnityFrame = 6;
        [Header("Biosphere Debug Fast Forward")]
        [SerializeField] private bool enableBiosphereDebugFastForward = true;
        [SerializeField] private int maxBiosphereDebugEnvironmentDaysPerUnityFrame = 2;

        private const string EnvironmentPlantCatalogResourcePath = "Arcontio/Config/environment_plants";
        private const string EnvironmentNaturalGrowthConfigResourcePath = "Arcontio/Config/environment_natural_growth";

        private int _runtimeTickSpeedMultiplier = 1;
        private int _lastRuntimeTicksProcessedInFrame;
        private float _lastDroppedRuntimeCatchUpSeconds;
        private long _lastBiosphereRuntimeProcessedSimulationTick;
        private int _lastBiosphereRuntimeDueUpdateCount;
        private int _lastBiosphereRuntimeAppliedPlantDeltas;
        private int _lastBiosphereRuntimePendingPlantDeltas;
        private int _lastBiosphereRuntimeDiffuseVegetationDeltas;
        private long _environmentDisplayBaseSimulationTick;
        private long _environmentDisplayBaseEnvironmentTick;
        private bool _biosphereDebugFastForwardActive;
        private bool _wasPausedBeforeBiosphereDebugFastForward;
        private int _biosphereDebugFastForwardMultiplier = 50;
        private float _biosphereDebugFastForwardAccumulatedEnvironmentTicks;
        private long _biosphereDebugFastForwardTotalEnvironmentTicksAdvanced;
        private long _biosphereDebugFastForwardLastEnvironmentTick;
        private int _lastBiosphereDebugAppliedPlantDeltas;
        private int _lastBiosphereDebugPendingPlantDeltas;
        private float _lastDroppedBiosphereDebugEnvironmentTicks;
        private readonly EnvironmentCalendarConfig _biosphereDebugCalendarConfig = new();
        private readonly EnvironmentClimateConfig _biosphereDebugClimateConfig = new();
        private EnvironmentNaturalGrowthConfig _environmentNaturalGrowthConfig = new();
        private EnvironmentPlantCatalog _environmentPlantCatalog =
            new EnvironmentPlantCatalogConfig().ToCatalog();
        private readonly EnvironmentHistoryBuffer _biosphereHistoryBuffer = new EnvironmentHistoryBuffer();
        private EnvironmentRuntimeEvent _lastEnvironmentRuntimeEvent;

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
        public int RuntimeTickSpeedMultiplier => _runtimeTickSpeedMultiplier;
        public int MaxRuntimeTicksPerUnityFrame => NormalizeMaxRuntimeTicksPerUnityFrame(maxRuntimeTicksPerUnityFrame);
        public int LastRuntimeTicksProcessedInFrame => _lastRuntimeTicksProcessedInFrame;
        public float LastDroppedRuntimeCatchUpSeconds => _lastDroppedRuntimeCatchUpSeconds;
        public long LastBiosphereRuntimeProcessedSimulationTick => _lastBiosphereRuntimeProcessedSimulationTick;
        public int LastBiosphereRuntimeDueUpdateCount => _lastBiosphereRuntimeDueUpdateCount;
        public int LastBiosphereRuntimeAppliedPlantDeltas => _lastBiosphereRuntimeAppliedPlantDeltas;
        public int LastBiosphereRuntimePendingPlantDeltas => _lastBiosphereRuntimePendingPlantDeltas;
        public int LastBiosphereRuntimeDiffuseVegetationDeltas => _lastBiosphereRuntimeDiffuseVegetationDeltas;
        public bool IsBiosphereDebugFastForwardActive => _biosphereDebugFastForwardActive;
        public int BiosphereDebugFastForwardMultiplier => _biosphereDebugFastForwardMultiplier;
        public long BiosphereDebugFastForwardTotalEnvironmentTicksAdvanced => _biosphereDebugFastForwardTotalEnvironmentTicksAdvanced;
        public long BiosphereDebugFastForwardLastEnvironmentTick => _biosphereDebugFastForwardLastEnvironmentTick;
        public int LastBiosphereDebugAppliedPlantDeltas => _lastBiosphereDebugAppliedPlantDeltas;
        public int LastBiosphereDebugPendingPlantDeltas => _lastBiosphereDebugPendingPlantDeltas;
        public float LastDroppedBiosphereDebugEnvironmentTicks => _lastDroppedBiosphereDebugEnvironmentTicks;
        public EnvironmentRuntimeEvent LastEnvironmentRuntimeEvent => _lastEnvironmentRuntimeEvent;

        public event System.Action<EnvironmentRuntimeEvent> EnvironmentRuntimeEventPublished;

        public EnvironmentHistorySnapshot CreateBiosphereHistorySnapshot()
        {
            return _biosphereHistoryBuffer.CreateSnapshot();
        }

        public void SetPaused(bool paused)
        {
            IsPaused = paused;
            if (IsPaused) _accum = 0f; // evita catch-up tick al resume
        }

        public void TogglePause() => SetPaused(!IsPaused);

        // =============================================================================
        // SetRuntimeTickSpeedMultiplier
        // =============================================================================
        /// <summary>
        /// <para>
        /// Imposta il moltiplicatore runtime dei tick normali della simulazione.
        /// </para>
        ///
        /// <para><b>Principio architetturale: accelerazione del loop, non nuovo tempo canonico</b></para>
        /// <para>
        /// Il valore aumenta quanti tick discreti vengono eseguiti per secondo reale,
        /// ma non cambia il delta canonico passato al singolo tick. In questo modo
        /// <c>x2</c>, <c>x3</c> e <c>x4</c> accelerano davvero la simulazione senza
        /// introdurre una seconda sorgente dati per la durata logica del tick.
        /// </para>
        ///
        /// <para><b>Struttura interna:</b></para>
        /// <list type="bullet">
        ///   <item><b>Normalizzazione</b>: il range produttivo iniziale e' x1-x4.</item>
        ///   <item><b>Clamp accumulatore</b>: quando il moltiplicatore cambia, l'accumulatore non puo' contenere arretrati eccessivi.</item>
        /// </list>
        /// </summary>
        public void SetRuntimeTickSpeedMultiplier(int multiplier)
        {
            _runtimeTickSpeedMultiplier = NormalizeRuntimeTickSpeedMultiplier(multiplier);

            if (_world != null)
            {
                float interval = ResolveRuntimeTickIntervalSeconds(_world.Config?.Sim, _runtimeTickSpeedMultiplier);
                if (_accum > interval)
                    _accum = interval;
            }
        }

        // =============================================================================
        // SetBiosphereDebugFastForwardMultiplier
        // =============================================================================
        /// <summary>
        /// <para>
        /// Imposta il moltiplicatore debug del fast-forward ambientale.
        /// </para>
        ///
        /// <para><b>Principio architetturale: debug Biosfera separato dalla simulazione sociale</b></para>
        /// <para>
            /// I valori ammessi sono volutamente pochi: <c>x50</c>, <c>x100</c>,
            /// <c>x200</c> e <c>x500</c>. Questo evita di riusare il moltiplicatore produttivo
        /// <c>x1-x4</c> e rende esplicito che questo percorso non accelera NPC,
        /// decisioni, job, memoria, belief, pathfinding o comunicazione.
        /// </para>
        /// </summary>
        public void SetBiosphereDebugFastForwardMultiplier(int multiplier)
        {
            _biosphereDebugFastForwardMultiplier = NormalizeBiosphereDebugFastForwardMultiplier(multiplier);
        }

        // =============================================================================
        // StartBiosphereDebugFastForward
        // =============================================================================
        /// <summary>
        /// <para>
        /// Avvia il fast-forward debug dedicato alla Biosfera.
        /// </para>
        ///
        /// <para><b>Freeze sociale, clock ambientale separato</b></para>
        /// <para>
        /// Il metodo non chiama <see cref="StepOneTick"/>, non modifica
        /// <c>_tickIndex</c> e non resetta alcuno stato NPC. Imposta solo una
        /// modalita' runtime nella quale <see cref="Update"/> continua a pompare
        /// input/UI e fa avanzare <see cref="EnvironmentState"/> con resolver
        /// data-only.
        /// </para>
        /// </summary>
        public bool StartBiosphereDebugFastForward(int multiplier)
        {
            if (!enableBiosphereDebugFastForward || _world == null || _world.EnvironmentState == null)
                return false;

            SetBiosphereDebugFastForwardMultiplier(multiplier);
            _wasPausedBeforeBiosphereDebugFastForward = IsPaused;
            _biosphereDebugFastForwardActive = true;
            _biosphereDebugFastForwardAccumulatedEnvironmentTicks = 0f;
            _lastDroppedBiosphereDebugEnvironmentTicks = 0f;
            _lastBiosphereDebugAppliedPlantDeltas = 0;
            _lastBiosphereDebugPendingPlantDeltas = 0;
            _biosphereDebugFastForwardLastEnvironmentTick =
                _world.EnvironmentState.Calendar.ElapsedEnvironmentTicks;

            // Congeliamo esplicitamente la simulazione ordinaria. Lo stop potra'
            // ripristinare lo stato pausa precedente, senza simulare tick sociali.
            SetPaused(true);
            return true;
        }

        // =============================================================================
        // StopBiosphereDebugFastForward
        // =============================================================================
        /// <summary>
        /// <para>
        /// Ferma il fast-forward debug Biosfera e applica solo refresh conservativi.
        /// </para>
        ///
        /// <para><b>Refresh senza simulazione fittizia</b></para>
        /// <para>
        /// Alla chiusura marchiamo la percezione NPC come sporca, ma non eseguiamo
        /// una percezione, non ricalcoliamo path e non applichiamo qui un rendering
        /// produttivo delle piante. Questi hook profondi appartengono alla chat
        /// Biosfera e agli step ArcGraph dedicati.
        /// </para>
        /// </summary>
        public void StopBiosphereDebugFastForward()
        {
            if (!_biosphereDebugFastForwardActive)
                return;

            _biosphereDebugFastForwardActive = false;
            _biosphereDebugFastForwardAccumulatedEnvironmentTicks = 0f;
            CompleteBiosphereDebugFastForwardConservativeRefresh();
            SetPaused(_wasPausedBeforeBiosphereDebugFastForward);
        }

        // =============================================================================
        // ToggleBiosphereDebugFastForward
        // =============================================================================
        /// <summary>
        /// <para>
        /// Alterna lo stato del fast-forward debug Biosfera.
        /// </para>
        /// </summary>
        public bool ToggleBiosphereDebugFastForward(int multiplier)
        {
            // Durante il fast-forward Biosfera non pompiamo comandi view -> core:
            // devono restare attivi solo UI, selezione puntatore e calendario ambiente.
            if (_biosphereDebugFastForwardActive)
            {
                StopBiosphereDebugFastForward();
                return false;
            }

            return StartBiosphereDebugFastForward(multiplier);
        }

        // =============================================================================
        // TryGetEnvironmentCalendarState
        // =============================================================================
        /// <summary>
        /// <para>
        /// Espone alla UI autorizzata lo snapshot calendario ambientale corrente.
        /// </para>
        /// </summary>
        public bool TryGetEnvironmentCalendarState(out EnvironmentCalendarState calendar)
        {
            if (_world?.EnvironmentState == null)
            {
                calendar = default;
                return false;
            }

            calendar = EnvironmentCalendarResolver.Resolve(
                ResolveEnvironmentDisplayTicks(),
                _biosphereDebugCalendarConfig);
            return true;
        }

        // =============================================================================
        // TryGetEnvironmentClimateState
        // =============================================================================
        /// <summary>
        /// <para>
        /// Espone alla UI autorizzata lo snapshot clima ambientale corrente.
        /// </para>
        /// </summary>
        public bool TryGetEnvironmentClimateState(out EnvironmentGlobalClimateState climate)
        {
            if (_world?.EnvironmentState == null)
            {
                climate = default;
                return false;
            }

            EnvironmentCalendarState displayCalendar = EnvironmentCalendarResolver.Resolve(
                ResolveEnvironmentDisplayTicks(),
                _biosphereDebugCalendarConfig);
            climate = EnvironmentClimateResolver.Resolve(
                displayCalendar,
                _biosphereDebugClimateConfig);
            return true;
        }

        // =============================================================================
        // ResolveEnvironmentDisplayTicks
        // =============================================================================
        /// <summary>
        /// <para>
        /// Risolve il tick ambientale da mostrare alla UI senza forzare il batch
        /// biologico giornaliero.
        /// </para>
        ///
        /// <para><b>Principio architetturale: clock di lettura separato dal ciclo biologico</b></para>
        /// <para>
        /// La crescita, le nascite e le morti della biosfera devono restare cadenzate
        /// da <c>simulationTicksPerDailyUpdate</c>. Il calendario visibile pero' deve
        /// poter scorrere mentre la simulazione normale avanza, altrimenti la UI resta
        /// ferma per tutto l'intervallo tra due batch giornalieri. Questo helper usa
        /// il massimo tra tick simulativo e tick gia' materializzato nello stato
        /// ambientale, cosi' il fast-forward debug non viene fatto tornare indietro
        /// dalla simulazione sociale ordinaria. Il valore non puo' pero' essere un
        /// semplice massimo tra tick simulativo e tick ambientale salvato: dopo un
        /// fast-forward solo Biosfera l'ambiente puo' essere avanti di anni rispetto
        /// al tick sociale, e un massimo secco congelerebbe la UI finche' il tick
        /// sociale non lo raggiunge. Usiamo quindi una base relativa: il tick
        /// ambientale gia' materializzato piu' i tick sociali trascorsi dall'ultimo
        /// riallineamento.
        /// </para>
        /// </summary>
        private long ResolveEnvironmentDisplayTicks()
        {
            long stateTicks = _world?.EnvironmentState != null
                ? _world.EnvironmentState.Calendar.ElapsedEnvironmentTicks
                : 0L;
            long simulationTicks = _tickIndex < 0L ? 0L : _tickIndex;
            long elapsedSimulationTicks = simulationTicks - _environmentDisplayBaseSimulationTick;
            if (elapsedSimulationTicks < 0L)
                elapsedSimulationTicks = 0L;

            long relativeDisplayTicks = _environmentDisplayBaseEnvironmentTick + elapsedSimulationTicks;
            return stateTicks > relativeDisplayTicks ? stateTicks : relativeDisplayTicks;
        }

        public void StepOneTickPaused()
        {
            if (_biosphereDebugFastForwardActive)
                return;

            if (!IsPaused) return;
            StepOneTick();
        }

        public void StepManyTicksPaused(int count)
        {
            if (_biosphereDebugFastForwardActive)
                return;

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
            // 2) INIZIALIZZO DIAGNOSTICA RUNTIME E PONTE LOG LEGACY
            // ******************************************************************************************************************************
            // ============================================================
            // NOTE (pulizia lettura config):
            // In precedenza il logger e il mondo caricavano entrambi game_params.json
            // da Resources. Da v0.12c.03 il TextAsset viene letto una sola volta qui
            // e SimulationParams diventa il modello principale anche per risolvere
            // la configurazione diagnostica usata nel bootstrap.
            //
            // GameParams resta come ponte compatibile per test o strumenti isolati,
            // ma il percorso ordinario del runtime non dipende piu' da un secondo
            // modello C# per leggere la sezione logging del file portante.
            // ============================================================
            const string gameParamsPathNoExt = "Arcontio/Config/game_params";
            var gameParamsAsset = Resources.Load<TextAsset>(gameParamsPathNoExt);
            var simParams = Arcontio.Core.Config.SimulationParamsLoader.LoadFromTextAsset(
                gameParamsAsset,
                gameParamsPathNoExt);

            RuntimeDiagnosticsLifecycle.InitFromSimulationParams(simParams);
            ArcontioLogger.InitFromSimulationParams(simParams);
            _environmentNaturalGrowthConfig = LoadEnvironmentNaturalGrowthConfigFromResources();
            ArcontioLogger.Info(
                new LogContext(tick: (int)TickContext.CurrentTickIndex, channel: "Core"),
                new LogBlock(LogLevel.Info, "log.core.persistent_path")
                    .AddField("path", Application.persistentDataPath)
            );

            // Il vecchio overlay del logger viene mantenuto come codice legacy, ma non viene
            // piu' creato automaticamente durante il runtime ordinario: senza sink console/overlay
            // attivo generava solo costo UI e memoria diagnostica ridondante.

            // ******************************************************************************************************************************
            // 3) INIZIALIZZO IL MONDO 
            // ******************************************************************************************************************************

            // ******************************************************************************************************************************
            // 3.1) Leggo da file game_params.json i dati di simulazione, che finiscono in simParams.
            // Con quello creo l'istanza di world
            // ******************************************************************************************************************************
            _world = CreateWorldFromGameParams(simParams);

            // ******************************************************************************************************************************
            // 3.2) INIZIALIZZO IL MESSAGE BUS
            // ******************************************************************************************************************************
            _bus = new MessageBus();

            // ******************************************************************************************************************************
            // 3.3) INIZIALIZZO LO SCHEDULER DEI SISTEMI
            // ******************************************************************************************************************************
            _scheduler = new Scheduler();

            // ******************************************************************************************************************************
            // 3.4) INIZIALIZZO L'OGGETTO TELEMETRY (DEBUG LEGACY)
            // ******************************************************************************************************************************
            // v0.12f: Telemetry resta nelle firme runtime come ponte transitorio,
            // ma viene resa realmente inerte quando la configurazione la tiene
            // spenta. In questo modo i systems possono continuare a ricevere il
            // parametro senza accumulare dizionari o contatori nel runtime ordinario.
            _telemetry = CreateTelemetryFromSimulationParams(simParams);

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
            // 4.5) Carica template job runtime minimali da JSON
            // ******************************************************************************************************************************
            // v0.11.01 introduce un registry volutamente piccolo: il JSON descrive
            // solo template/fasi/action, mentre target e logica reale restano nel
            // codice C# e nel decision layer. Dopo v0.11b.01-v0.11b.04 il gate food
            // e' acceso di default, quindi questo log di bootstrap serve a verificare
            // che la scena runtime stia usando davvero il valore e i template attesi.
            _jobTemplateRegistry = JobTemplateRegistry.LoadDefault();
            ArcontioLogger.Info(
                new LogContext(tick: (int)TickContext.CurrentTickIndex, channel: "SimulationHost"),
                new LogBlock(LogLevel.Info, "log.simulationhost.food_job_vertical_slice_config")
                    .AddField("enableFoodJobVerticalSlice", enableFoodJobVerticalSlice)
                    .AddField("jobTemplateRegistryLoaded", _jobTemplateRegistry != null)
                    .AddField("templateCount", _jobTemplateRegistry?.Count ?? 0)
                    .AddField(
                        "hasSearchFoodTemplate",
                        _jobTemplateRegistry != null
                        && _jobTemplateRegistry.TryGetTemplate(JobTemplateRegistry.SearchFoodLocalProbeTemplateId, out _))
                    .AddField(
                        "hasEatKnownFoodTemplate",
                        _jobTemplateRegistry != null
                        && _jobTemplateRegistry.TryGetTemplate(JobTemplateRegistry.FoodKnownCommunityStockTemplateId, out _)));

            // ******************************************************************************************************************************
            // 5) ISCRIVO I SISTEMI ALLO SCHEDULER
            // ******************************************************************************************************************************

            // ******************************************************************************************************************************
            // 5.1B) LANDMARK MEMORY (Day3) - NpcLandmarkMemorySystem
            // ******************************************************************************************************************************
            // Questo System applica maintenance (eviction + cap) alla memoria soggettiva dei landmark.
            // Il movimento fisico NPC e' ora posseduto dal Job Layer tramite MoveTo,
            // quindi questo system resta indipendente dal vecchio consumer MoveIntent.
            _scheduler.AddSystem(new NpcLandmarkMemorySystem());

            // v0.20n: lo scan automatico idle e' pensionato. Il guardarsi attorno
            // passa dal percorso ordinario Decisione -> JobRequest -> Job tramite
            // `WaitAndObserve` e step `LookDirection`, evitando rotazioni runtime
            // fuori dal Job Layer.


            // ******************************************************************************************************************************
            // 5.1C) LANDMARK PERCEPTION (v0.03.03.a - Landmark Perception) - LandmarkPerceptionSystem
            // ******************************************************************************************************************************
            // Apprendimento visivo dei landmark tramite FOV degli NPC.
            // Complementa il learning fisico (NotifyNpcMovedForLandmarkLearning).
            // Da v0.20m non usa piu' un periodo autonomo: consuma la stessa
            // selezione percettiva dirty/cadenzata usata da oggetti e NPC.
            {
                _scheduler.AddSystem(new LandmarkPerceptionSystem());
            }

            // ******************************************************************************************************************************
            // 5.2) SCAN IN IDLE - IdleScan
            // ******************************************************************************************************************************
            // Pensionato in v0.20n: lo stesso comportamento deve nascere da job
            // osservativi espliciti, non da un system automatico fuori authority.

            // ******************************************************************************************************************************
            // 5.3) BISOGNI NPC - NeedsDecaySystem
            // ******************************************************************************************************************************

            // ******************************************************************************************************************************
            // 5.4) PERCEZIONE - ObjectPerceptionSystem (genera eventi ObjectSpottedEvent)
            // ******************************************************************************************************************************
            _scheduler.AddSystem(new ObjectPerceptionSystem());

            // ******************************************************************************************************************************
            // 5.5) PERCEZIONE - NpcPerceptionSystem (genera eventi NpcSpottedEvent)
            // ******************************************************************************************************************************
            _scheduler.AddSystem(new NpcPerceptionSystem());

            // Chiusura del blocco percettivo centrale: il dirty viene pulito solo
            // dopo landmark, oggetti e NPC.
            _scheduler.AddSystem(new PerceptionDirtyCompletionSystem());

            // I bisogni restano prima di Job/Decision, ma non spezzano piu' il
            // blocco percettivo centrale.
            _scheduler.AddSystem(new NeedsDecaySystem());

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
            // 7.5) JOB EXECUTION - vertical slice food opt-in v0.11.01
            // ******************************************************************************************************************************
            // Il sistema gira prima della rule phase e produce solo ICommand nel
            // JobCommandBuffer posseduto da World.JobRuntimeState. Il pump effettivo
            // resta piu' sotto, nel punto unico in cui SimulationHost esegue command
            // contro il World.
            _scheduler.AddSystem(new JobExecutionSystem());

            if (_world?.Config?.Sim?.decision?.enableJobDecisionOrchestrator == true)
            {
                _scheduler.AddSystem(new DecisionOrchestratorSystem(
                    decisionEveryTicks: _world.Config.Sim.ResolveDecisionEveryTicks(),
                    maxSeekRangeCells: _world.Global.NpcOperationalRangeCells,
                    enableFoodJobVerticalSlice: enableFoodJobVerticalSlice,
                    jobTemplateRegistry: _jobTemplateRegistry));
            }

            // ******************************************************************************************************************************
            // 8) INIZIALIZZO LE RULES
            // ******************************************************************************************************************************
            // ATTENZIONE: le Rules della memoria sono inizializzate in MemoryEncodingSystem
            _rules.Add(new DebugEventLogRule());
            //     _rules.Add(new BasicSurvivalRule());

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
            // DEVTOOLS / VIEW COMMANDS
            // ============================================================
            // IMPORTANTISSIMO:
            // - DevMode deve poter editare la mappa anche quando la sim è in pausa.
            if (_biosphereDebugFastForwardActive)
            {
                AdvanceBiosphereDebugFastForward(Time.unscaledDeltaTime);
                return;
            }

            // - Quindi eseguiamo i comandi esterni prima del return su IsPaused,
            //   ma solo quando non e' attivo il debug Biosfera-only.
            PumpExternalCommands();

            if (IsPaused)
                return;

            float dt = Time.unscaledDeltaTime;
            _accum += dt;

            float tickInterval = ResolveRuntimeTickIntervalSeconds(_world?.Config?.Sim, _runtimeTickSpeedMultiplier);
            int maxTicksThisFrame = MaxRuntimeTicksPerUnityFrame;
            int ticksProcessed = 0;
            _lastDroppedRuntimeCatchUpSeconds = 0f;

            while (_accum >= tickInterval && ticksProcessed < maxTicksThisFrame)
            {
                _accum -= tickInterval;
                ticksProcessed++;
                StepOneTick();
            }

            _lastRuntimeTicksProcessedInFrame = ticksProcessed;

            if (ticksProcessed >= maxTicksThisFrame && _accum >= tickInterval)
            {
                _lastDroppedRuntimeCatchUpSeconds = _accum;
                _accum = 0f;
            }
        }

        // =============================================================================
        // ResolveRuntimeTickIntervalSeconds
        // =============================================================================
        /// <summary>
        /// <para>
        /// Calcola l'intervallo reale tra tick applicando il moltiplicatore runtime.
        /// </para>
        ///
        /// <para><b>Principio architetturale: tick canonico, cadenza runtime variabile</b></para>
        /// <para>
        /// Il valore base resta quello risolto da <see cref="ResolveTickIntervalSeconds"/>.
        /// Il moltiplicatore riduce l'intervallo reale tra due tick, ma il singolo
        /// <see cref="Tick"/> continua a ricevere il delta canonico base tramite
        /// <see cref="ResolveTickDeltaTime"/>.
        /// </para>
        ///
        /// <para><b>Struttura interna:</b></para>
        /// <list type="bullet">
        ///   <item><b>Base</b>: intervallo da game_params.</item>
        ///   <item><b>Multiplier</b>: clamp x1-x4.</item>
        ///   <item><b>Output</b>: secondi reali tra tick nel loop host.</item>
        /// </list>
        /// </summary>
        public static float ResolveRuntimeTickIntervalSeconds(
            SimulationParams simParams,
            int runtimeSpeedMultiplier)
        {
            return ResolveTickIntervalSeconds(simParams) / NormalizeRuntimeTickSpeedMultiplier(runtimeSpeedMultiplier);
        }

        // =============================================================================
        // NormalizeRuntimeTickSpeedMultiplier
        // =============================================================================
        /// <summary>
        /// <para>
        /// Normalizza il moltiplicatore runtime delle accelerazioni produttive.
        /// </para>
        /// </summary>
        public static int NormalizeRuntimeTickSpeedMultiplier(int multiplier)
        {
            if (multiplier < 1)
                return 1;

            return multiplier > 4 ? 4 : multiplier;
        }

        // =============================================================================
        // NormalizeMaxRuntimeTicksPerUnityFrame
        // =============================================================================
        /// <summary>
        /// <para>
        /// Normalizza il cap massimo di tick eseguibili nello stesso frame Unity.
        /// </para>
        /// </summary>
        public static int NormalizeMaxRuntimeTicksPerUnityFrame(int maxTicks)
        {
            if (maxTicks < 1)
                return 1;

            return maxTicks > 32 ? 32 : maxTicks;
        }

        // =============================================================================
        // NormalizeBiosphereDebugFastForwardMultiplier
        // =============================================================================
        /// <summary>
        /// <para>
        /// Normalizza il moltiplicatore debug Biosfera sui soli valori consentiti.
        /// </para>
        /// </summary>
        public static int NormalizeBiosphereDebugFastForwardMultiplier(int multiplier)
        {
            if (multiplier <= 50)
                return 50;

            if (multiplier <= 100)
                return 100;

            if (multiplier <= 200)
                return 200;

            return 500;
        }

        // =============================================================================
        // ResolveBiosphereDebugEnvironmentTicksPerSecond
        // =============================================================================
        /// <summary>
        /// <para>
        /// Calcola quanti tick ambientali debug avanzano in un secondo reale.
        /// </para>
        /// </summary>
        public static float ResolveBiosphereDebugEnvironmentTicksPerSecond(
            SimulationParams simParams,
            int multiplier)
        {
            int ticksPerSecond = Mathf.Max(1, simParams?.ResolveTicksPerSecond() ?? TickParams.DefaultTicksPerSecond);
            return ticksPerSecond * NormalizeBiosphereDebugFastForwardMultiplier(multiplier);
        }

        // =============================================================================
        // ProcessBiosphereRuntimeDailyUpdate
        // =============================================================================
        /// <summary>
        /// <para>
        /// Esegue il batch giornaliero produttivo della biosfera quando la cadenza
        /// configurata in <c>game_params.json</c> lo richiede.
        /// </para>
        ///
        /// <para><b>Principio architetturale: SimulationHost orchestra, Biosfera calcola</b></para>
        /// <para>
        /// Il metodo non contiene formule ecologiche. Legge solo il tick ufficiale,
        /// chiede allo scheduler data-only se il batch e' dovuto, poi delega a
        /// <see cref="AdvanceAndApplyBiosphereEnvironmentState"/> la produzione dei
        /// delta e l'applicazione al <see cref="World"/>.
        /// </para>
        /// </summary>
        private void ProcessBiosphereRuntimeDailyUpdate(long currentSimulationTick)
        {
            _lastBiosphereRuntimeDueUpdateCount = 0;
            _lastBiosphereRuntimeAppliedPlantDeltas = 0;
            _lastBiosphereRuntimePendingPlantDeltas = 0;
            _lastBiosphereRuntimeDiffuseVegetationDeltas = 0;

            if (_world?.EnvironmentState == null)
                return;

            BiosphereRuntimeParams runtimeParams =
                _world.Config?.Sim?.ResolveBiosphereRuntimeParams()
                ?? new BiosphereRuntimeParams();
            EnvironmentRuntimeScheduleDecision decision =
                EnvironmentRuntimeScheduler.Evaluate(
                    runtimeParams,
                    _lastBiosphereRuntimeProcessedSimulationTick,
                    currentSimulationTick);

            if (!decision.IsEnabled || !decision.ShouldAdvance)
                return;

            int maxPlantDeltas = runtimeParams.ResolveMaxPlantMutationsPerUpdate();
            int maxVegetationDeltas = runtimeParams.ResolveMaxVegetationCellsChangedPerDay();
            long processedSimulationTick = decision.LastProcessedSimulationTick;

            for (int i = 0; i < decision.DueUpdateCount; i++)
            {
                // Il tick sociale schedula quando il batch e' dovuto; il tick
                // ambientale dice da dove riparte davvero la biosfera. Dopo un
                // fast-forward debug o un save/load con ambiente gia' avanzato, non
                // dobbiamo riportare indietro calendario e clima al valore del tick
                // SimulationHost.
                long currentEnvironmentTick = _world.EnvironmentState.Calendar.ElapsedEnvironmentTicks;
                long targetEnvironmentTick = currentEnvironmentTick + decision.SimulationTicksPerDailyUpdate;
                EnvironmentAdvanceResult result =
                    AdvanceAndApplyBiosphereEnvironmentState(
                        targetEnvironmentTick,
                        runtimeParams,
                        maxPlantDeltas,
                        maxVegetationDeltas,
                        applyPhysicalPlantDeltas: true,
                        EnvironmentRuntimeEventKind.DailyUpdate,
                        out int appliedPlantDeltas,
                        out int appliedVegetationDeltas);

                _lastBiosphereRuntimePendingPlantDeltas += result.PhysicalPlantDeltas?.Count ?? 0;
                _lastBiosphereRuntimeAppliedPlantDeltas += appliedPlantDeltas;
                _lastBiosphereRuntimeDiffuseVegetationDeltas += appliedVegetationDeltas;
                processedSimulationTick += decision.SimulationTicksPerDailyUpdate;
                RebaseEnvironmentDisplayClock(processedSimulationTick);
            }

            _lastBiosphereRuntimeDueUpdateCount = decision.DueUpdateCount;
            _lastBiosphereRuntimeProcessedSimulationTick = processedSimulationTick;
        }

        // =============================================================================
        // AdvanceAndApplyBiosphereEnvironmentState
        // =============================================================================
        /// <summary>
        /// <para>
        /// Avanza lo stato ambientale con il ciclo naturale e applica al World i
        /// boundary derivati.
        /// </para>
        ///
        /// <para><b>Boundary Biosfera -> World</b></para>
        /// <para>
        /// La biosfera resta proprietaria di calendario, clima, aree, seed bank e
        /// PlantInstance. Il World riceve soltanto proiezioni derivate: piante
        /// fisiche come ostacoli/occluder e vegetazione diffusa come stato
        /// decorativo cell-based.
        /// </para>
        /// </summary>
        private EnvironmentAdvanceResult AdvanceAndApplyBiosphereEnvironmentState(
            long currentEnvironmentTick,
            BiosphereRuntimeParams runtimeParams,
            int maxPlantDeltaCount,
            int maxVegetationDeltaCount,
            bool applyPhysicalPlantDeltas,
            EnvironmentRuntimeEventKind eventKind,
            out int appliedPlantDeltas,
            out int appliedVegetationDeltas)
        {
            appliedPlantDeltas = 0;
            appliedVegetationDeltas = 0;

            EnvironmentAdvanceResult result =
                AdvanceBiosphereEnvironmentState(
                    currentEnvironmentTick,
                    runtimeParams,
                    maxPlantDeltaCount,
                    maxVegetationDeltaCount);

            _world.SetEnvironmentState(result.State);
            CaptureBiosphereHistorySample();
            appliedVegetationDeltas =
                _world.ApplyEnvironmentDiffuseVegetationDeltas(result.DiffuseVegetationDeltas);

            if (applyPhysicalPlantDeltas)
                appliedPlantDeltas =
                    _world.ApplyEnvironmentPhysicalPlantDeltas(result.PhysicalPlantDeltas);

            PublishEnvironmentRuntimeEvent(
                EnvironmentRuntimeEvent.FromAdvanceResult(
                    eventKind,
                    result,
                    appliedPlantDeltas,
                    appliedVegetationDeltas));

            return result;
        }

        // =============================================================================
        // AdvanceBiosphereEnvironmentState
        // =============================================================================
        /// <summary>
        /// <para>
        /// Produce il prossimo stato biosfera senza side effect World.
        /// </para>
        /// </summary>
        private EnvironmentAdvanceResult AdvanceBiosphereEnvironmentState(
            long currentEnvironmentTick,
            BiosphereRuntimeParams runtimeParams,
            int maxPlantDeltaCount,
            int maxVegetationDeltaCount)
        {
            EnvironmentState sourceState = _world?.EnvironmentState ?? new EnvironmentState();
            EnvironmentSnapshot sourceSnapshot = sourceState.CreateSnapshot();
            long previousTicks = sourceSnapshot.Calendar.ElapsedEnvironmentTicks;
            EnvironmentTemporalTransition transition = EnvironmentTemporalTransitionResolver.Resolve(
                previousTicks,
                currentEnvironmentTick,
                _biosphereDebugCalendarConfig);
            EnvironmentGlobalClimateState climate = EnvironmentClimateResolver.Resolve(
                transition.Current,
                _biosphereDebugClimateConfig);
            EnvironmentSeasonProfile seasonProfile =
                EnvironmentCalendarResolver.ResolveSeasonProfile(
                    _biosphereDebugCalendarConfig,
                    transition.Current.Date.Season);
            EnvironmentPlantCatalog plantCatalog =
                _environmentPlantCatalog ?? new EnvironmentPlantCatalogConfig().ToCatalog();
            EnvironmentNaturalGrowthConfig growthConfig =
                BuildRuntimeNaturalGrowthConfig(
                    _environmentNaturalGrowthConfig,
                    runtimeParams);

            EnvironmentNaturalGrowthResult growth =
                EnvironmentNaturalGrowthResolver.Evolve(
                    sourceSnapshot,
                    plantCatalog,
                    transition,
                    climate,
                    seasonProfile,
                    growthConfig);
            growth.State.RebuildRuntimeBiologicalPlacements(_world);

            EnvironmentSnapshot nextSnapshot = growth.State.CreateSnapshot();
            EnvironmentSnapshotDiffResult diff =
                EnvironmentSnapshotDiffResolver.Diff(sourceSnapshot, nextSnapshot);
            IReadOnlyList<EnvironmentPhysicalPlantDelta> physicalPlantDeltas =
                EnvironmentPhysicalPlantDeltaProducer.DiffSnapshots(
                    sourceSnapshot,
                    nextSnapshot,
                    maxPlantDeltaCount);
            IReadOnlyList<EnvironmentDiffuseVegetationDelta> diffuseVegetationDeltas =
                EnvironmentDiffuseVegetationDeltaProducer.DiffPlacements(
                    sourceState.VegetationCellPlacements,
                    growth.State.VegetationCellPlacements,
                    maxVegetationDeltaCount);
            var evolutionReport = new EnvironmentSnapshotEvolutionReport(
                growth.Report.AreasVisited,
                0,
                0,
                growth.Report.SeedBanksUpdated,
                growth.Report.HasChanges ? 1 : 0);

            return new EnvironmentAdvanceResult(
                transition,
                climate,
                seasonProfile,
                growth.State,
                nextSnapshot,
                evolutionReport,
                diff,
                physicalPlantDeltas,
                diffuseVegetationDeltas);
        }

        // =============================================================================
        // BuildRuntimeNaturalGrowthConfig
        // =============================================================================
        /// <summary>
        /// <para>
        /// Costruisce la configurazione del ciclo naturale per il batch corrente,
        /// sovrapponendo ai parametri biologici fini i soli budget runtime dichiarati
        /// in <c>game_params.json</c>.
        /// </para>
        ///
        /// <para><b>Principio architetturale: tuning biologico separato dal carico runtime</b></para>
        /// <para>
        /// <c>environment_natural_growth.json</c> descrive come cresce la biosfera.
        /// <c>game_params.json/biosphere</c> descrive quanto lavoro puo' fare per
        /// batch. Questa copia evita di mutare il DTO caricato da Resources e
        /// mantiene esplicita la separazione tra modello biologico e budget.
        /// </para>
        /// </summary>
        private static EnvironmentNaturalGrowthConfig BuildRuntimeNaturalGrowthConfig(
            EnvironmentNaturalGrowthConfig baseConfig,
            BiosphereRuntimeParams runtimeParams)
        {
            EnvironmentNaturalGrowthConfig source = baseConfig ?? new EnvironmentNaturalGrowthConfig();
            BiosphereRuntimeParams budget = BiosphereRuntimeParams.WithFallbackDefaults(runtimeParams);
            var result = new EnvironmentNaturalGrowthConfig
            {
                allowNewPlantInstances = source.allowNewPlantInstances,
                maxNewPlantsPerDay = source.maxNewPlantsPerDay,
                maxNewPlantsPerAreaPerDay = source.maxNewPlantsPerAreaPerDay,
                maxExistingPlantUpdatesPerDay = source.maxExistingPlantUpdatesPerDay,
                maxDeadPlantsRemovedPerDay = source.maxDeadPlantsRemovedPerDay,
                maxAreasProcessedPerDay = source.maxAreasProcessedPerDay,
                minimumGerminationScore01 = source.minimumGerminationScore01,
                healthRecoveryStep01 = source.healthRecoveryStep01,
                healthStressStep01 = source.healthStressStep01,
                removeDeadPlants = source.removeDeadPlants,
                plantAridityHealthStressScale01 = source.plantAridityHealthStressScale01,
                seedPressureDesiredPlantAreaScale01 = source.seedPressureDesiredPlantAreaScale01,
                plantVitalityMin01 = source.plantVitalityMin01,
                plantVitalityMax01 = source.plantVitalityMax01,
                initialPlantHealthVitalityScale01 = source.initialPlantHealthVitalityScale01,
                unfavorableSeasonFallbackStressMultiplier01 = source.unfavorableSeasonFallbackStressMultiplier01,
                perennialDormancyStressMultiplier01 = source.perennialDormancyStressMultiplier01,
                deciduousDormancyStressMultiplier01 = source.deciduousDormancyStressMultiplier01,
                evergreenDormancyStressMultiplier01 = source.evergreenDormancyStressMultiplier01
            };

            if (budget.ResolveMaxPlantBirthsPerDay() > 0)
                result.maxNewPlantsPerDay = budget.ResolveMaxPlantBirthsPerDay();

            if (budget.ResolveMaxPlantBirthsPerAreaPerDay() > 0)
                result.maxNewPlantsPerAreaPerDay = budget.ResolveMaxPlantBirthsPerAreaPerDay();

            if (budget.ResolveMaxPlantUpdatesPerDay() > 0)
                result.maxExistingPlantUpdatesPerDay = budget.ResolveMaxPlantUpdatesPerDay();

            if (budget.ResolveMaxPlantDeathsPerDay() > 0)
                result.maxDeadPlantsRemovedPerDay = budget.ResolveMaxPlantDeathsPerDay();

            if (budget.ResolveMaxAreasProcessedPerDay() > 0)
                result.maxAreasProcessedPerDay = budget.ResolveMaxAreasProcessedPerDay();

            return result;
        }

        // =============================================================================
        // ResetBiosphereRuntimeSchedule
        // =============================================================================
        /// <summary>
        /// <para>
        /// Riallinea il cursore runtime della biosfera al tick simulativo corrente.
        /// </para>
        /// </summary>
        private void ResetBiosphereRuntimeSchedule(long simulationTick)
        {
            _lastBiosphereRuntimeProcessedSimulationTick = simulationTick < 0 ? 0 : simulationTick;
            _lastBiosphereRuntimeDueUpdateCount = 0;
            _lastBiosphereRuntimeAppliedPlantDeltas = 0;
            _lastBiosphereRuntimePendingPlantDeltas = 0;
            _lastBiosphereRuntimeDiffuseVegetationDeltas = 0;
            RebaseEnvironmentDisplayClock(_lastBiosphereRuntimeProcessedSimulationTick);
        }

        // =============================================================================
        // RebaseEnvironmentDisplayClock
        // =============================================================================
        /// <summary>
        /// <para>
        /// Riallinea la base relativa usata dal calendario UI.
        /// </para>
        ///
        /// <para><b>Principio architetturale: tick sociale e tick ambientale non sono sempre identici</b></para>
        /// <para>
        /// Il fast-forward debug della Biosfera avanza lo stato ambientale senza
        /// eseguire tick NPC. Per questo il calendario visibile non puo' leggere solo
        /// <c>_tickIndex</c>. Questo metodo memorizza il punto di contatto tra tick
        /// sociale corrente e tick ambientale corrente, poi
        /// <see cref="ResolveEnvironmentDisplayTicks"/> continua la proiezione in
        /// avanti mentre il runtime normale procede.
        /// </para>
        /// </summary>
        private void RebaseEnvironmentDisplayClock(long simulationTick)
        {
            _environmentDisplayBaseSimulationTick = simulationTick < 0L ? 0L : simulationTick;
            _environmentDisplayBaseEnvironmentTick = _world?.EnvironmentState != null
                ? _world.EnvironmentState.Calendar.ElapsedEnvironmentTicks
                : 0L;
        }

        // =============================================================================
        // AdvanceBiosphereDebugFastForward
        // =============================================================================
        /// <summary>
        /// <para>
        /// Avanza solo lo stato ambientale usando la scala configurata reale.
        /// </para>
        ///
        /// <para><b>Nota sui delta fisici</b></para>
        /// <para>
        /// Il resolver puo' produrre delta di piante fisiche, ma qui li contiamo
        /// soltanto. Non li applichiamo al <c>World</c> per non anticipare il feed
        /// produttivo Biosfera -> ArcGraph e la dirty propagation avanzata.
        /// </para>
        /// </summary>
        private void AdvanceBiosphereDebugFastForward(float deltaTime)
        {
            if (deltaTime <= 0f || _world?.EnvironmentState == null)
                return;

            float ticksPerSecond = ResolveBiosphereDebugEnvironmentTicksPerSecond(
                _world.Config?.Sim,
                _biosphereDebugFastForwardMultiplier);
            _biosphereDebugFastForwardAccumulatedEnvironmentTicks += deltaTime * ticksPerSecond;

            long wholeTicks = (long)_biosphereDebugFastForwardAccumulatedEnvironmentTicks;
            if (wholeTicks <= 0L)
                return;

            long maxTicksThisFrame = ResolveMaxBiosphereDebugEnvironmentTicksPerFrame();
            if (wholeTicks > maxTicksThisFrame)
            {
                _lastDroppedBiosphereDebugEnvironmentTicks =
                    _biosphereDebugFastForwardAccumulatedEnvironmentTicks - maxTicksThisFrame;
                wholeTicks = maxTicksThisFrame;
                _biosphereDebugFastForwardAccumulatedEnvironmentTicks = 0f;
            }
            else
            {
                _biosphereDebugFastForwardAccumulatedEnvironmentTicks -= wholeTicks;
                _lastDroppedBiosphereDebugEnvironmentTicks = 0f;
            }

            long previousEnvironmentTick = _world.EnvironmentState.Calendar.ElapsedEnvironmentTicks;
            long currentEnvironmentTick = previousEnvironmentTick + wholeTicks;

            BiosphereRuntimeParams runtimeParams =
                _world.Config?.Sim?.ResolveBiosphereRuntimeParams()
                ?? new BiosphereRuntimeParams();
            EnvironmentAdvanceResult result =
                AdvanceAndApplyBiosphereEnvironmentState(
                    currentEnvironmentTick,
                    runtimeParams,
                    runtimeParams.ResolveMaxPlantMutationsPerUpdate(),
                    runtimeParams.ResolveMaxVegetationCellsChangedPerDay(),
                    applyPhysicalPlantDeltas: true,
                    EnvironmentRuntimeEventKind.DebugFastForwardDailyUpdate,
                    out int appliedPlantDeltas,
                    out _);

            _biosphereDebugFastForwardTotalEnvironmentTicksAdvanced += wholeTicks;
            _biosphereDebugFastForwardLastEnvironmentTick = currentEnvironmentTick;
            _lastBiosphereDebugPendingPlantDeltas = result.PhysicalPlantDeltas?.Count ?? 0;
            _lastBiosphereDebugAppliedPlantDeltas = appliedPlantDeltas;
            RebaseEnvironmentDisplayClock(_tickIndex);
        }

        private long ResolveMaxBiosphereDebugEnvironmentTicksPerFrame()
        {
            int hoursPerDay = _biosphereDebugCalendarConfig.ResolveHoursPerDay();
            int ticksPerHour = _biosphereDebugCalendarConfig.ResolveCalendarTicksPerSimulatedHour();
            int days = maxBiosphereDebugEnvironmentDaysPerUnityFrame < 1
                ? 1
                : maxBiosphereDebugEnvironmentDaysPerUnityFrame;

            return (long)Mathf.Max(1, hoursPerDay * ticksPerHour) * days;
        }

        private void CompleteBiosphereDebugFastForwardConservativeRefresh()
        {
            if (_world == null)
                return;

            _world.MarkAllNpcPerceptionDirty();

            // TODO Biosfera/Pathfinding:
            // quando esistera' un hook globale esplicito per invalidare path e
            // replan dovuti a piante nate/morte, chiamarlo qui senza simulare tick
            // NPC e senza forzare MovementSystem.

            // TODO ArcGraph ambiente:
            // quando il feed produttivo Biosfera -> ArcGraph sara' disponibile,
            // richiedere qui un refresh visuale conservativo delle celle ambiente.
        }

        // =============================================================================
        // ResolveTickIntervalSeconds
        // =============================================================================
        /// <summary>
        /// <para>
        /// Calcola l'intervallo reale tra due tick usando i parametri caricati da
        /// <c>game_params.json</c>.
        /// </para>
        ///
        /// <para><b>Principio architetturale: game_params come fonte primaria del tick</b></para>
        /// <para>
        /// ARC-DEC-006 e ARC-DEC-020 fissano il tick globale come tempo canonico.
        /// La frequenza runtime deve quindi arrivare dalla pipeline dati
        /// <c>game_params.json -> SimulationParams -> SimulationHost</c>, non da campi
        /// serializzati sull'Inspector del GameObject <c>ArcontioRuntime</c>. Questo
        /// metodo e' pubblico e puro per permettere ai test EditMode di verificare il
        /// calcolo senza avviare una scena Unity.
        /// </para>
        ///
        /// <para><b>Struttura interna:</b></para>
        /// <list type="bullet">
        ///   <item><b>Input</b>: DTO tipizzato dei parametri di simulazione.</item>
        ///   <item><b>Clamp</b>: frequenze nulle o negative cadono al default sicuro.</item>
        ///   <item><b>Output</b>: secondi reali per tick del loop host.</item>
        /// </list>
        /// </summary>
        public static float ResolveTickIntervalSeconds(SimulationParams simParams)
        {
            return 1f / Mathf.Max(1, simParams?.ResolveTicksPerSecond() ?? TickParams.DefaultTicksPerSecond);
        }

        // =============================================================================
        // ResolveTickDeltaTime
        // =============================================================================
        /// <summary>
        /// <para>
        /// Restituisce il delta temporale associato al <see cref="Tick"/> corrente.
        /// </para>
        ///
        /// <para><b>Principio architetturale: nessuna doppia authority tickDeltaTime</b></para>
        /// <para>
        /// Il vecchio campo Inspector <c>tickDeltaTime</c> duplicava la frequenza
        /// runtime e poteva divergere da <c>ticksPerSecond</c>. Nella foundation
        /// v0.11c.03-prep il delta del tick e' derivato dallo stesso parametro
        /// canonico, cosi' la cadence reale e il dato passato ai sistemi restano
        /// leggibili e testabili da una sola sorgente.
        /// </para>
        ///
        /// <para><b>Struttura interna:</b></para>
        /// <list type="bullet">
        ///   <item><b>Derivazione</b>: delega a <see cref="ResolveTickIntervalSeconds"/>.</item>
        ///   <item><b>Fallback</b>: se la config manca, usa il default tipizzato di SimulationParams.</item>
        /// </list>
        /// </summary>
        public static float ResolveTickDeltaTime(SimulationParams simParams)
        {
            return ResolveTickIntervalSeconds(simParams);
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
            if (_biosphereDebugFastForwardActive)
                return;

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

            var tick = new Tick(_tickIndex, ResolveTickDeltaTime(_world?.Config?.Sim));

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

            // I command prodotti dal JobExecutionSystem entrano nello stesso pump dei
            // command legacy rule-driven. Questo mantiene un'unica authority di
            // mutazione World: gli step accodano intenzioni operative, ma solo qui
            // ICommand.Execute applica effetti reali.
            FlushJobCommandsIntoMainCommandBuffer();

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
            if (_tickIndex % 20 == 0 && ArcontioLogger.ShouldWrite(LogLevel.Debug))
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
            _world.RuntimeCostObserver?.TryWriteJsonlSnapshot(_tickIndex);

            ProcessBiosphereRuntimeDailyUpdate(_tickIndex + 1);

            _tickIndex++;

            // Debug: verifica che l'host resti vivo cambiando scena
            if (_tickIndex % 50 == 0 && ArcontioLogger.ShouldWrite(LogLevel.Debug))
            {
                ArcontioLogger.Debug(
                    new LogContext(tick: _tickIndex, channel: "Arcontio"),
                    new LogBlock(LogLevel.Debug, "log.arcontio.tick_summary")
                        .AddField("food", _world.FoodStocks.Count)
                        .AddField("npc", _world.NpcDna.Count)
                );
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
            EnvironmentFoundationBootstrapResult environmentBootstrap =
                ApplyEnvironmentFoundationBootstrap(_world);
            _environmentPlantCatalog = environmentBootstrap.PlantCatalog;
            CaptureBiosphereHistorySample();
            PublishEnvironmentRuntimeEvent(
                EnvironmentRuntimeEvent.FromState(
                    EnvironmentRuntimeEventKind.Bootstrap,
                    _world.EnvironmentState));
            ResetBiosphereRuntimeSchedule(_tickIndex);

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
            ResetBiosphereRuntimeSchedule(_tickIndex);

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

            if (!TryRestoreEnvironmentFromSnapshotData(data, loadedWorld, out error))
            {
                Debug.LogError(
                    $"[WorldSnapshotLoadDiag][SimulationHost] Environment restore FAILED " +
                    $"worldHash={loadedWorld.GetHashCode()} error='{error}'");
                loadedWorld = null;
                return false;
            }

            _biosphereHistoryBuffer.Clear();
            _biosphereHistoryBuffer.Capture(loadedWorld.EnvironmentState);
            PublishEnvironmentRuntimeEvent(
                EnvironmentRuntimeEvent.FromState(
                    EnvironmentRuntimeEventKind.Loaded,
                    loadedWorld.EnvironmentState));
            loadedWorld.RebuildLandmarksBootstrap();
            return true;
        }

        // =============================================================================
        // TryRestoreEnvironmentFromSnapshotData
        // =============================================================================
        /// <summary>
        /// <para>
        /// Ripristina la biosfera persistita nello snapshot oppure applica il bootstrap
        /// ambientale quando il salvataggio non contiene ancora quella sezione.
        /// </para>
        ///
        /// <para><b>Principio architetturale: save/load prima del bootstrap rigenerativo</b></para>
        /// <para>
        /// Uno snapshot vissuto deve preservare calendario, clima, aree, piante e
        /// placement prodotti dalla biosfera. Il bootstrap naturale resta solo il
        /// fallback per salvataggi vecchi o tecnici privi di sezione ambiente.
        /// </para>
        ///
        /// <para><b>Struttura interna:</b></para>
        /// <list type="bullet">
        ///   <item><b>Catalogo</b>: carica il catalogo piante runtime corrente.</item>
        ///   <item><b>Restore</b>: converte <c>EnvironmentSaveData</c> in EnvironmentState.</item>
        ///   <item><b>Proiezioni</b>: riallinea vegetazione diffusa e piante fisiche nel World.</item>
        ///   <item><b>Fallback</b>: usa il bootstrap foundation se non c'e' una biosfera persistita.</item>
        /// </list>
        /// </summary>
        private bool TryRestoreEnvironmentFromSnapshotData(
            WorldSaveData data,
            World loadedWorld,
            out string error)
        {
            error = string.Empty;
            if (loadedWorld == null)
            {
                error = "World nullo durante restore biosfera.";
                return false;
            }

            EnvironmentPlantCatalogConfig plantCatalogConfig =
                LoadEnvironmentPlantCatalogConfigFromResources();
            _environmentPlantCatalog = plantCatalogConfig.ToCatalog();

            if (!HasPersistedEnvironmentSection(data))
            {
                EnvironmentFoundationBootstrapResult environmentBootstrap =
                    ApplyEnvironmentFoundationBootstrap(loadedWorld);
                _environmentPlantCatalog = environmentBootstrap.PlantCatalog;
                return true;
            }

            EnvironmentFoundationConfig environmentConfig =
                EnvironmentFoundationBootstrap.CreateDefaultConfig();
            environmentConfig.plantCatalog = plantCatalogConfig;

            EnvironmentLoadResult loadResult =
                EnvironmentPersistenceResolver.Restore(
                    data.environment,
                    environmentConfig.calendar);

            loadedWorld.SetEnvironmentState(loadResult.State);

            if (data.environment.ResolveSchemaVersion() < 2)
                loadedWorld.EnvironmentState?.RebuildRuntimeBiologicalPlacements(loadedWorld);

            loadedWorld.ApplyEnvironmentDiffuseVegetationProjections();
            loadedWorld.ApplyEnvironmentPhysicalPlantProjections();

            if (loadResult.Report.HasRejectedRecords)
            {
                Debug.LogWarning(
                    "[SimulationHost] Environment restore completed with rejected records. " +
                    $"areas={loadResult.Report.AreasLoaded} plants={loadResult.Report.PlantsLoaded} " +
                    $"rejected={loadResult.Report.RejectedRecords}");
            }

            return true;
        }

        private static bool HasPersistedEnvironmentSection(WorldSaveData data)
        {
            EnvironmentSaveData environment = data?.environment;
            if (environment == null)
                return false;

            return environment.elapsedEnvironmentTicks > 0
                || (environment.areas != null && environment.areas.Length > 0)
                || (environment.plants != null && environment.plants.Length > 0)
                || (environment.vegetationPlacements != null && environment.vegetationPlacements.Length > 0)
                || (environment.physicalPlantPlacements != null && environment.physicalPlantPlacements.Length > 0);
        }

        private void CaptureBiosphereHistorySample()
        {
            _biosphereHistoryBuffer.Capture(_world?.EnvironmentState);
        }

        // =============================================================================
        // PublishEnvironmentRuntimeEvent
        // =============================================================================
        /// <summary>
        /// <para>
        /// Pubblica un evento ambiente compatto verso UI e sistemi listener.
        /// </para>
        ///
        /// <para><b>Principio architetturale: listener isolati dal loop deterministico</b></para>
        /// <para>
        /// Il <c>SimulationHost</c> resta l'orchestratore temporale. I listener non
        /// ricevono riferimenti mutabili alla Biosfera e un errore in un listener non
        /// deve interrompere il tick loop: per questo ogni callback viene protetta e
        /// loggata singolarmente.
        /// </para>
        ///
        /// <para><b>Struttura interna:</b></para>
        /// <list type="bullet">
        ///   <item><b>LastEnvironmentRuntimeEvent</b>: ultimo evento consultabile in polling sicuro.</item>
        ///   <item><b>EnvironmentRuntimeEventPublished</b>: notifica push per UI/sistemi.</item>
        /// </list>
        /// </summary>
        private void PublishEnvironmentRuntimeEvent(EnvironmentRuntimeEvent runtimeEvent)
        {
            _lastEnvironmentRuntimeEvent = runtimeEvent;
            System.Action<EnvironmentRuntimeEvent> handlers = EnvironmentRuntimeEventPublished;
            if (handlers == null)
                return;

            System.Delegate[] invocationList = handlers.GetInvocationList();
            for (int i = 0; i < invocationList.Length; i++)
            {
                var handler = invocationList[i] as System.Action<EnvironmentRuntimeEvent>;
                if (handler == null)
                    continue;

                try
                {
                    handler(runtimeEvent);
                }
                catch (System.Exception ex)
                {
                    Debug.LogError("[SimulationHost] Environment runtime listener failed: " + ex);
                }
            }
        }

        // =============================================================================
        // ApplyEnvironmentFoundationBootstrap
        // =============================================================================
        /// <summary>
        /// <para>
        /// Installa nel <see cref="World"/> lo stato iniziale della biosfera prima che
        /// vengano ricostruite le cache derivate dei landmark.
        /// </para>
        ///
        /// <para><b>Principio architetturale: World possiede lo stato, Environment decide i dati biologici</b></para>
        /// <para>
        /// Il runtime non genera landmark biologici direttamente. Si limita a
        /// materializzare la foundation ambientale e a consegnarla al <see cref="World"/>,
        /// che durante la rebuild chiedera' alla biosfera quali anchor biologici
        /// proporre al registry landmark.
        /// </para>
        ///
        /// <para><b>Struttura interna:</b></para>
        /// <list type="bullet">
        ///   <item><b>Bootstrap</b>: usa la pipeline data-only EnvironmentFoundationBootstrap.</item>
        ///   <item><b>Installazione</b>: passa solo EnvironmentState a World, senza esporre DTO config al registry.</item>
        ///   <item><b>Diagnostica</b>: logga warning se la config default produce validazione sospetta.</item>
        /// </list>
        /// </summary>
        private EnvironmentFoundationBootstrapResult ApplyEnvironmentFoundationBootstrap(World world)
        {
            if (world == null)
                return EnvironmentFoundationBootstrap.Bootstrap(null);

            EnvironmentFoundationConfig config = EnvironmentFoundationBootstrap.CreateDefaultConfig();
            config.areas = world.InitialEnvironmentAreaSetConfig ?? new EnvironmentAreaSetConfig();
            config.plantCatalog = LoadEnvironmentPlantCatalogConfigFromResources();

            EnvironmentFoundationBootstrapResult bootstrap =
                EnvironmentFoundationBootstrap.Bootstrap(config);

            world.SetEnvironmentState(bootstrap.Build.State);
            world.EnvironmentState?.BuildInitialBiologicalOccupancy(world);
            world.ApplyEnvironmentDiffuseVegetationProjections();
            world.ApplyEnvironmentPhysicalPlantProjections();

            if (bootstrap.Validation != null && !bootstrap.Validation.IsValid)
            {
                Debug.LogWarning(
                    "[SimulationHost] Environment foundation bootstrap completed with validation issues. " +
                    $"errors={bootstrap.Validation.ErrorCount} warnings={bootstrap.Validation.WarningCount}");
            }

            return bootstrap;
        }

        // =============================================================================
        // LoadEnvironmentPlantCatalogConfigFromResources
        // =============================================================================
        /// <summary>
        /// <para>
        /// Carica il catalogo biologico piante da Resources con fallback sicuro.
        /// </para>
        ///
        /// <para><b>Principio architetturale: config data-driven, fallback deterministico</b></para>
        /// <para>
        /// La crescita naturale runtime non deve dipendere da dati hardcoded nel
        /// <c>SimulationHost</c>. Se il file manca o non e' leggibile, pero', il
        /// runtime resta avviabile usando il catalogo default tipizzato della
        /// Environment Foundation.
        /// </para>
        /// </summary>
        private static EnvironmentPlantCatalogConfig LoadEnvironmentPlantCatalogConfigFromResources()
        {
            TextAsset asset = Resources.Load<TextAsset>(EnvironmentPlantCatalogResourcePath);
            if (asset == null || string.IsNullOrWhiteSpace(asset.text))
                return new EnvironmentPlantCatalogConfig();

            try
            {
                return JsonUtility.FromJson<EnvironmentPlantCatalogConfig>(asset.text)
                       ?? new EnvironmentPlantCatalogConfig();
            }
            catch (System.Exception ex)
            {
                Debug.LogWarning(
                    "[SimulationHost] Environment plant catalog config failed to load. " +
                    $"path={EnvironmentPlantCatalogResourcePath} error={ex.Message}");
                return new EnvironmentPlantCatalogConfig();
            }
        }

        // =============================================================================
        // LoadEnvironmentNaturalGrowthConfigFromResources
        // =============================================================================
        /// <summary>
        /// <para>
        /// Carica da Resources la configurazione produttiva/debug del ciclo naturale
        /// della biosfera.
        /// </para>
        ///
        /// <para><b>Principio architetturale: tuning fuori dal loop simulativo</b></para>
        /// <para>
        /// Il fast-forward biosfera deve poter essere calibrato senza ricompilare il
        /// codice. Il loader mantiene un fallback locale sicuro se il JSON manca o
        /// non e' leggibile, ma quando il file esiste usa i parametri dichiarati in
        /// <c>Assets/Resources/Arcontio/Config/environment_natural_growth.json</c>.
        /// </para>
        /// </summary>
        private static EnvironmentNaturalGrowthConfig LoadEnvironmentNaturalGrowthConfigFromResources()
        {
            TextAsset asset = Resources.Load<TextAsset>(EnvironmentNaturalGrowthConfigResourcePath);
            if (asset == null || string.IsNullOrWhiteSpace(asset.text))
                return new EnvironmentNaturalGrowthConfig();

            try
            {
                EnvironmentNaturalGrowthConfig config =
                    JsonUtility.FromJson<EnvironmentNaturalGrowthConfig>(asset.text);

                return config ?? new EnvironmentNaturalGrowthConfig();
            }
            catch
            {
                return new EnvironmentNaturalGrowthConfig();
            }
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
        private World CreateWorldFromGameParams(Arcontio.Core.Config.SimulationParams simParams = null)
        {
            simParams ??= Arcontio.Core.Config.SimulationParamsLoader.LoadFromResources("Arcontio/Config/game_params");
            return new World(new WorldConfig(simParams));
        }

        private static Telemetry CreateTelemetryFromSimulationParams(Arcontio.Core.Config.SimulationParams simParams)
        {
            var diagnostics = simParams?.ResolveLoggerDiagnostics();
            return new Telemetry(diagnostics?.telemetry != null && diagnostics.telemetry.enabled);
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
            CellSurfaceDatabaseLoader.LoadIntoWorld(world);
            WorldMapConfigLoader.LoadIntoWorld(world);
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
            _telemetry = CreateTelemetryFromSimulationParams(_world?.Config?.Sim);
            _npcCommunication = new NpcCommunicationPipeline(contactRadius: 2, topN: 6);

            // v0.11.01: i job attivi sono runtime transitorio, non snapshot state.
            // Dopo load lo store resta presente ma viene azzerato per evitare che
            // un job del vecchio mondo continui ad agire sul nuovo World.
            _world?.JobRuntimeState?.ClearTransientJobs();

            if (_memoryEncoding != null)
                _memoryEncoding.SetEventsBuffer(_eventBuffer);
        }

        // =============================================================================
        // FlushJobCommandsIntoMainCommandBuffer
        // =============================================================================
        /// <summary>
        /// <para>
        /// Trasferisce i command prodotti dal Job System nel buffer command principale
        /// del tick.
        /// </para>
        ///
        /// <para><b>Command pump unico</b></para>
        /// <para>
        /// Il Job System non possiede una via opaca per mutare il mondo. I suoi step
        /// scrivono nel <c>JobCommandBuffer</c>, poi <c>SimulationHost</c> li sposta
        /// nel buffer gia' usato dalle rules. L'esecuzione resta quindi ordinata,
        /// visibile e concentrata nel solo punto in cui i command toccano il World.
        /// </para>
        /// </summary>
        private void FlushJobCommandsIntoMainCommandBuffer()
        {
            var runtime = _world?.JobRuntimeState;
            if (runtime == null || runtime.CommandBuffer.Count == 0)
                return;

            var snapshot = runtime.CommandBuffer.Snapshot();
            for (int i = 0; i < snapshot.Length; i++)
            {
                if (snapshot[i] != null)
                    _commands.Add(snapshot[i]);
            }

            runtime.CommandBuffer.Clear();
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
            _world.CreateObject(defId: "bed_wood", x: 2, y: 0, ownerKind: OwnerKind.Npc, ownerId: npc);
            _world.CreateObject(defId: "workbench_basic", x: 2, y: 1, ownerKind: OwnerKind.Community, ownerId: 0);
            _world.CreateObject(defId: "chair_wood", x: -2, y: 0, ownerKind: OwnerKind.Community, ownerId: 0);

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
            int bedCommunity = _world.CreateObject(defId: "bed_wood", x: 20, y: 23, ownerKind: OwnerKind.Community, ownerId: 0);
            _world.ObjectUse[bedCommunity] = ObjectUseState.Free();

            ArcontioLogger.Debug(new LogContext(0, "T9"),
               new LogBlock(LogLevel.Debug, "object.spawn")
                  .AddField("obj", "bed_wood")
                  .AddField("units", 1));

            int bedNpc2 = _world.CreateObject(defId: "bed_wood", x: 21, y: 23, ownerKind: OwnerKind.Npc, ownerId: npc2);
            _world.ObjectUse[bedNpc2] = ObjectUseState.Free();

            ArcontioLogger.Debug(new LogContext(0, "T9"),
               new LogBlock(LogLevel.Debug, "object.spawn")
                  .AddField("obj", "bed_wood")
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

            _world.CreateObject(defId: "door_wood", x: 32, y: 24, ownerKind: OwnerKind.Community, ownerId: 0);

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
            RuntimeDiagnosticsLifecycle.Flush();
        }

        private void OnApplicationQuit()
        {
            RuntimeDiagnosticsLifecycle.Shutdown();
            ArcontioLogger.Shutdown();
        }
    }
}
