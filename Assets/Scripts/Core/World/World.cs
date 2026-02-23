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
    /// - Gli "occluder" NON sono più una struttura separata: sono oggetti del mondo (WorldObjectInstance)
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
        /// - Questa NON è logica di simulazione.
        /// - È solo un canale di osservabilità per la view/debug.
        /// </summary>
        public DebugFovTelemetry DebugFovTelemetry { get; private set; }

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

            // Pulizia cache: default è ok per OcclusionCell/bool,
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
/// - Non è una "AI brain" separata: è un piccolo stato descrittivo che rende osservabile
///   cosa l'NPC sta facendo in questo momento.
/// - Viene aggiornato dai Command (intenti) e/o dai System (esecuzione fisica).
/// - La View può leggerlo senza dover inferire azioni da segnali indiretti (movimento, fame, ecc.).
/// </summary>
public readonly Dictionary<int, NpcActionState> NpcAction = new();

        /// <summary>
        /// NpcBalloonSignals:
        /// Ultimo segnale "visuale" (balloon) per ciascun NPC.
        ///
        /// Nota:
        /// - È uno store di osservabilità, come NpcAction.
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
        // COMPONENT STORES (OBJECTS)
        // ============================================================

        // Oggetti nel mondo
        public readonly Dictionary<int, WorldObjectInstance> Objects = new();

        // Use state (letto occupato, ecc.)
        public readonly Dictionary<int, ObjectUseState> ObjectUse = new();

        // Food stock ?in-world? (pile/stockpile)
        public readonly Dictionary<int, FoodStockComponent> FoodStocks = new();

        // Cibo privato per NPC (inventario v0)
        public readonly Dictionary<int, int> NpcPrivateFood = new();

        // Marker per distinguere "ho mangiato io" vs "mi manca cibo"
        // npcId -> ultimo tick in cui ha consumato cibo privato
        public readonly Dictionary<int, long> NpcLastPrivateFoodConsumeTick = new();

        // (Day10) Occluder component store per oggetti ?muro/porta?
        // Nota: un muro è un oggetto, e qui mettiamo i suoi flags runtime (vision/movement + cost).
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
        /// - "Nessuna visione a 360° gratuita": scan = 4 turn consecutivi.
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
            // DEBUG FOV TELEMETRY (config-driven)
            // ============================================================
            // Nota:
            // - questa è una feature SOLO debug.
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
                // Esempio: FOV=90° => half=45° => tan(45)=1.
                float halfRad = (fovDeg * 0.5f) * Mathf.Deg2Rad;
                slope = Mathf.Tan(halfRad);
            }

            if (slope <= 0f) slope = 1.0f;

            // Legacy/back-compat: manteniamo anche HalfWidthPerStep per codice vecchio.
            Global.NpcVisionConeSlope = slope;
            Global.NpcVisionConeHalfWidthPerStep = slope;

            Global.Needs = NeedsConfig.Default();
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
        // NPC API
        // ============================================================

        public bool ExistsNpc(int npcId) => npcId > 0 && NpcCore.ContainsKey(npcId);
        public bool AreBonded(int aNpcId, int bNpcId)
        {
            // STUB (roadmap): quando introdurrai bond graph,
            // questa funzione consulterà quel grafo.
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

            // Priorità: se hai un config globale, usalo; altrimenti fallback su PersonalityMemoryParams.
            int maxTraces = MemoryParams[id].MaxTraces;
            if (Global.NpcObjectMemorySlots > 0) { /* non è maxTraces: è slots oggetti (altro). */ }

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


// Action state init (se non presente)
if (!NpcAction.ContainsKey(id))
    NpcAction[id] = NpcActionState.Idle();

            // Balloon signal init (se non presente)
            // Nota:
            // - Default = struct default => Kind=None, Tick=0.
            // - La view userà il tick per capire se ha già mostrato il balloon.
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
        /// - Non esisteva come helper; è utile per sistemi (scan, movement) e debug.
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
        /// - Fonte di verità runtime per la posizione NPC è GridPos.
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
        /// - In futuro questo è il punto perfetto per pubblicare NpcMovedEvent / PerceptionDirty.
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
        /// - È un segnale transiente: la view lo consumerà mostrando un balloon per X secondi.
        /// - NON è un bus di eventi.
        /// - Non validiamo semantica qui: è solo osservabilità.
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
        /// Questo è volutamente minimale: in futuro ActivityStateComponent può sostituire.
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
            // - lo scan è "costoso": è una sequenza di turn su tick successivi.
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

            // Se è un occluder oppure blocca la visione o il movimento, aggiorna la occlusion map.
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

            // 1) Se è occluder, pulisci la cache occlusione prima di rimuovere dai dizionari.
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
            //    Nel tuo World l'empty value è -1 (come da commento su _objIdByCell).
            if (_objIdByCell != null && _objIdByCell.Length == MapWidth * MapHeight && InBounds(x, y))
            {
                int idx = CellIndex(x, y);

                // Difensivo: liberiamo solo se la cella punta davvero a quell'objectId.
                if (_objIdByCell[idx] == objectId)
                    _objIdByCell[idx] = -1;
            }

            // 3) component cleanup (use state, stocks, runtime occluders)
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

            // Se già presente qualcosa, lo consideriamo errore di coerenza (1 occluder per cell).
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
            // Se qui non aggiorniamo anche i bool[], IsMovementBlocked può restare "false"
            // su celle muro => un NPC può finire dentro una cella wall.
            // ------------------------------------------------------------

            bool blocksVision = def.BlocksVision;
            bool blocksMove = def.BlocksMovement;

            // Se è marcato come occluder ma non ha flags, default "blocca tutto".
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

            // Il registry ObjectOccluders è per objectId.
            ObjectOccluders.Remove(objectId);
        }

        /// <summary>
        /// Regola ARCONTIO Core Standard v1.0: 1 object per cell.
        /// Qui facciamo enforcement minimo:
        /// - se esiste già un oggetto in (x,y) => fail.
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
        /// - restituisce true se nella cella c'è un oggetto che blocca visione e/o movimento.
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
        /// Per non rompere i test/seed già scritti, questo:
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
                // se c?è un oggetto ?normale? già piazzato, qui sei in conflitto con 1 object/cell.
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
            // e consideriamo blocking sulle celle intermedie e target (in genere anche il target può essere muro).
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

            // Se è marcato come occluder, ma non ha flags specifiche, default ?blocca tutto?
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

            // Se è un occluder ?vero?, registriamo anche il componente runtime per query dettagliate.
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
        public int NpcVisionRangeCells;
        public bool NpcVisionUseCone;
        public float NpcVisionConeSlope; // half-width per forward step (grid cone)
        public float NpcVisionConeHalfWidthPerStep; // legacy/back-compat (se lo stai usando altrove)

        // --- Needs config ---
        public NeedsConfig Needs;

        // --- Object memory config ---
        public int NpcObjectMemorySlots;       // slot per memoria oggetti interagibili (per NPC)
        public int ObjectMemoryMaxAgeTicks;    // TTL in tick (pulizia)

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
