using System;
using System.Collections.Generic;

namespace Arcontio.Core
{
    /// <summary>
    /// (v0.02 Day1) Tipi di supporto per osservabilità del sistema Landmark.
    ///
    /// Importante:
    /// - In Day1 NON esiste ancora il LandmarkRegistry né la memoria soggettiva di landmark/edges.
    /// - Questi tipi sono "contratti" view/core per:
    ///   1) Debug report stampabile / visualizzabile nella SummaryOverlay.
    ///   2) Debug overlay (nodi + linee) nella MapGrid view.
    ///
    /// Quando implementeremo i giorni successivi, questi stessi contratti verranno popolati
    /// con dati reali (e non con zeri / liste vuote).
    /// </summary>
    [Serializable]
    public readonly struct NpcLandmarkDebugReport
    {
        public readonly int KnownLandmarksCount;
        public readonly int KnownEdgesCount;
        public readonly int PoiAnchorCount;

        public readonly float ReplansPerMin;
        public readonly float FailuresPerMin;

        public readonly int BlacklistSize;

        public NpcLandmarkDebugReport(
            int knownLandmarksCount,
            int knownEdgesCount,
            int poiAnchorCount,
            float replansPerMin,
            float failuresPerMin,
            int blacklistSize)
        {
            KnownLandmarksCount = knownLandmarksCount;
            KnownEdgesCount = knownEdgesCount;
            PoiAnchorCount = poiAnchorCount;
            ReplansPerMin = replansPerMin;
            FailuresPerMin = failuresPerMin;
            BlacklistSize = blacklistSize;
        }
    }

    [Serializable]
    public readonly struct NpcMacroRouteDebugReport
    {
        public readonly bool HasRoute;
        public readonly int StartNodeId;
        public readonly int TargetNodeId;
        public readonly int RouteNodeCount;
        public readonly int TargetCellX;
        public readonly int TargetCellY;
        public readonly string FailureReason;
        public readonly bool ExecutionActive;
        public readonly bool IsDoingLastMile;
        public readonly int NextRouteNodeIndex;
        public readonly int NextRouteNodeId;
        public readonly int ImmediateTargetX;
        public readonly int ImmediateTargetY;
        public readonly string ExecutionFailureReason;
        public readonly string NavigationMode;
        public readonly int LastModeSwitchTick;
        public readonly string LastModeSwitchReason;
        public readonly bool GoalLocalSearchActive;
        public readonly int GoalLocalSearchBudgetRemaining;

        public NpcMacroRouteDebugReport(
            bool hasRoute,
            int startNodeId,
            int targetNodeId,
            int routeNodeCount,
            int targetCellX,
            int targetCellY,
            string failureReason,
            bool executionActive,
            bool isDoingLastMile,
            int nextRouteNodeIndex,
            int nextRouteNodeId,
            int immediateTargetX,
            int immediateTargetY,
            string executionFailureReason,
            string navigationMode,
            int lastModeSwitchTick,
            string lastModeSwitchReason,
            bool goalLocalSearchActive,
            int goalLocalSearchBudgetRemaining)
        {
            HasRoute = hasRoute;
            StartNodeId = startNodeId;
            TargetNodeId = targetNodeId;
            RouteNodeCount = routeNodeCount;
            TargetCellX = targetCellX;
            TargetCellY = targetCellY;
            FailureReason = failureReason ?? string.Empty;
            ExecutionActive = executionActive;
            IsDoingLastMile = isDoingLastMile;
            NextRouteNodeIndex = nextRouteNodeIndex;
            NextRouteNodeId = nextRouteNodeId;
            ImmediateTargetX = immediateTargetX;
            ImmediateTargetY = immediateTargetY;
            ExecutionFailureReason = executionFailureReason ?? string.Empty;
            NavigationMode = navigationMode ?? string.Empty;
            LastModeSwitchTick = lastModeSwitchTick;
            LastModeSwitchReason = lastModeSwitchReason ?? string.Empty;
            GoalLocalSearchActive = goalLocalSearchActive;
            GoalLocalSearchBudgetRemaining = goalLocalSearchBudgetRemaining;
        }
    }

    [Serializable]
    public sealed class NpcMacroRouteExecutionState
    {
        public bool Active;
        public bool HasUsableMacroPath;
        public bool IsDoingLastMile;
        public int NextRouteNodeIndex;
        public int FinalTargetCellX;
        public int FinalTargetCellY;
        public int ImmediateTargetX;
        public int ImmediateTargetY;
        public string FailureReason = string.Empty;

        // ============================================================
        // DEBUG NAVIGATION MODE TRACKING (v0.02.05.2 follow-up)
        // ============================================================
        // Questi campi non influenzano la logica di pathfinding in sé.
        // Servono unicamente a rendere osservabile, in modo molto chiaro,
        // quale "regime di navigazione" sta usando l'NPC in questo preciso momento:
        // landmark macro, override diretto, ricerca locale goal-oriented, ecc.
        //
        // L'obiettivo pratico è evitare il classico dubbio in debug:
        // "si è mosso così perché sta ancora seguendo i landmark,
        //  oppure perché è passato al fallback locale?"
        public string NavigationMode = string.Empty;
        public int LastModeSwitchTick = -1;
        public string LastModeSwitchReason = string.Empty;

        // ── DIRECT PREFIX COMMITMENT (Patch 0.02.07.A) ──────────────
        // Passi del prefix committed ancora da eseguire.
        // >0 = esecuzione inerziale del prefix (no re-check FOV).
        // 0  = prefix terminato, rivalutare acquisizione.
        public int DirectPrefixStepsRemaining;

        // FIX Patch 0.02.07.B — Bug 1 approaching:
        // True finché l'NPC non ha ancora raggiunto fisicamente il primo nodo
        // landmark della route. In questo tratto i passi devono essere tracciati
        // come DIRECT (azzurro), non LM_PATH (arancione).
        // Azzerato in TryAdvanceMacroRouteAtCell al primo avanzamento di NextRouteNodeIndex.
        public bool IsApproachingFirstLm;
    }


    [Serializable]
    public sealed class NpcDirectCommitExecutionState
    {
        // ============================================================
        // DIRECT COMMIT STATE (v0.02.05.2a)
        // ============================================================
        // Questa struttura rappresenta la forma corretta della regola 1:
        // non un semplice "vedo il target in questo tick", ma un piccolo
        // piano diretto gia' costruito e poi seguito a memoria.
        //
        // Conseguenza pratica molto importante:
        // - l'NPC puo' vedere il target, costruire un tragitto diretto reale,
        //   iniziare a muoversi, perdere poi il contatto visivo, e continuare
        //   comunque finche' quel piano resta eseguibile.
        // - questo evita il comportamento nervoso "direct / landmark / direct / landmark"
        //   che apparirebbe innaturale nei corridoi, negli angoli e nelle piccole
        //   interruzioni di linea di vista.
        public bool Active;
        public int FinalTargetCellX;
        public int FinalTargetCellY;
        public int ImmediateTargetX;
        public int ImmediateTargetY;
        public int NextPathIndex;
        public string FailureReason = string.Empty;
        public readonly List<GridPosition> CurrentPath = new List<GridPosition>(32);
    }


    [Serializable]
    public sealed class NpcGoalLocalSearchExecutionState
    {
        // ============================================================
        // LOCAL SEARCH STATE (v0.02.05.2)
        // ============================================================
        //
        // Questa struttura esiste per una ragione molto precisa:
        // la ricerca locale NON deve essere un impulso istantaneo e stateless,
        // altrimenti l'NPC rischia di ricalcolare ogni tick senza continuità comportamentale
        // e di oscillare tra macro-route e fallback locale.
        //
        // Qui conserviamo quindi uno stato minimo ma persistente:
        // - se la modalità è attiva
        // - qual è il target finale reale
        // - qual è il target immediato del prossimo step
        // - quanti step/budget restano prima di considerare fallita la ricerca
        // - il path locale corrente, già espanso cella-per-cella
        //
        // Nota importante: il path viene calcolato dal MovementSystem con una
        // ricerca locale "JPS-style" su griglia uniforme. Questa struttura
        // non conosce l'algoritmo: custodisce solo lo stato esecutivo.
        public bool Active;
        public int FinalTargetCellX;
        public int FinalTargetCellY;
        public int ImmediateTargetX;
        public int ImmediateTargetY;
        public int BudgetRemaining;
        public int NextPathIndex;
        public int CommitStepsRemaining;
        public bool HasLastSuccessfulStep;
        public int LastStepFromX;
        public int LastStepFromY;
        public int LastStepToX;
        public int LastStepToY;
        public string FailureReason = string.Empty;
        public readonly List<GridPosition> CurrentPath = new List<GridPosition>(32);
    }

    /// <summary>
    /// NpcMacroRoutePlan (v0.02 Day4):
    /// route macro calcolata su grafo landmark.
    ///
    /// Nota architetturale importante:
    /// - Questo NON e' ancora il path micro (cella-per-cella).
    /// - E' una sequenza di checkpoint topologici che il Day5 usera' come destinazioni intermedie.
    /// - In Day4 lo usiamo soprattutto come output del planner e come layer di debug osservabile.
    /// </summary>
    [Serializable]
    public sealed class NpcMacroRoutePlan
    {
        public int StartNodeId;
        public int TargetNodeId;
        public int TargetCellX;
        public int TargetCellY;
        public bool Succeeded;
        public string FailureReason = string.Empty;
        public readonly List<int> NodeIds = new List<int>(16);
    }


    /// <summary>
    /// Nodo landmark per overlay view-only.
    /// - cellX/cellY: coordinate su griglia.
    /// - kind: enumerazione leggera (0=generic) così la view può differenziare marker in futuro.
    /// </summary>
    [Serializable]
    public readonly struct LandmarkOverlayNode
    {
        public readonly int CellX;
        public readonly int CellY;
        public readonly int Kind;
        public readonly int NodeId;
        public readonly string Label;

        public LandmarkOverlayNode(int cellX, int cellY, int kind = 0, int nodeId = 0, string label = null)
        {
            CellX = cellX;
            CellY = cellY;
            Kind = kind;
            NodeId = nodeId;
            Label = label ?? string.Empty;
        }
    }

    /// <summary>
    /// Edge landmark per overlay view-only.
    /// - A=(ax,ay), B=(bx,by) in coordinate cella.
    /// - reliability01: 0..1 (quando avremo decay/learning).
    /// </summary>
    [Serializable]
    public readonly struct LandmarkOverlayEdge
    {
        public readonly int Ax;
        public readonly int Ay;
        public readonly int Bx;
        public readonly int By;
        public readonly float Reliability01;

        public LandmarkOverlayEdge(int ax, int ay, int bx, int by, float reliability01 = 1f)
        {
            Ax = ax;
            Ay = ay;
            Bx = bx;
            By = by;
            Reliability01 = reliability01;
        }
    }

    // ============================================================
    // GVD-DIN OVERLAY TYPES (v0.03)
    // ============================================================
    // Questi tipi supportano i tre layer di debug del sistema GVD-DIN:
    //   1) GvdDinOverlayCellDt  — heatmap della Distance Transform
    //   2) GvdDinOverlayCellGvd — celle GVD grezze pre-pruning
    //   3) I nodi GVD post-pruning riusano LandmarkOverlayNode con Kind=GvdVertex
    //
    // Nota architetturale:
    // - Questi dati vengono prodotti da GvdDinComputer (v0.03) e trasportati
    //   alla view tramite World.GetGvdDinOverlayData().
    // - La view NON conosce GvdDinComputer: legge solo questi contratti.
    // ============================================================

    /// <summary>
    /// Cella con valore Distance Transform per l'overlay heatmap.
    /// - CellX/CellY: coordinata griglia.
    /// - DtValue: distanza alla parete più vicina (0 = muro/adiacente, max = centro stanza).
    /// - DtNormalized01: valore normalizzato 0..1 per la colorazione (0=scuro, 1=chiaro).
    /// </summary>
    [Serializable]
    public readonly struct GvdDinOverlayCellDt
    {
        public readonly int CellX;
        public readonly int CellY;
        public readonly int DtValue;
        public readonly float DtNormalized01;

        public GvdDinOverlayCellDt(int cellX, int cellY, int dtValue, float dtNormalized01)
        {
            CellX = cellX;
            CellY = cellY;
            DtValue = dtValue;
            DtNormalized01 = dtNormalized01;
        }
    }

    /// <summary>
    /// Cella GVD grezza pre-pruning per l'overlay dot ciano.
    /// - CellX/CellY: coordinata griglia.
    /// - ObstacleA/ObstacleB: indici lineari (y*width+x) dei due ostacoli equidistanti.
    ///   Usati solo per debug avanzato; la view semplice ignora questi campi.
    /// </summary>
    [Serializable]
    public readonly struct GvdDinOverlayCellGvd
    {
        public readonly int CellX;
        public readonly int CellY;
        public readonly int ObstacleA;
        public readonly int ObstacleB;

        public GvdDinOverlayCellGvd(int cellX, int cellY, int obstacleA = -1, int obstacleB = -1)
        {
            CellX = cellX;
            CellY = cellY;
            ObstacleA = obstacleA;
            ObstacleB = obstacleB;
        }
    }

    /// <summary>
    /// Snapshot completo dei dati GVD-DIN per un singolo frame di debug overlay.
    /// Prodotto da World.GetGvdDinOverlayData() e consumato da MapGridLandmarkOverlay.
    ///
    /// Nota: le liste sono pre-allocate e riutilizzate tra frame per evitare GC.
    /// Il caller (MapGridLandmarkOverlay) deve passare le stesse istanze ogni frame.
    /// </summary>
    public sealed class GvdDinOverlaySnapshot
    {
        // Layer 1: heatmap DT — celle con dtValue > 0 (celle walkable).
        public readonly List<GvdDinOverlayCellDt> DtCells = new List<GvdDinOverlayCellDt>(512);

        // Layer 2: celle GVD grezze pre-pruning.
        public readonly List<GvdDinOverlayCellGvd> GvdRawCells = new List<GvdDinOverlayCellGvd>(256);

        // Layer 3: nodi GVD post-pruning (= candidati landmark GVD-DIN).
        // Usano LandmarkOverlayNode con Kind=3 (GvdVertex) per distinzione visiva.
        public readonly List<LandmarkOverlayNode> GvdNodes = new List<LandmarkOverlayNode>(64);

        // Edge tra nodi GVD post-pruning (scheletro topologico).
        public readonly List<LandmarkOverlayEdge> GvdEdges = new List<LandmarkOverlayEdge>(128);

        // Stato del sistema: true se GVD-DIN è enabled e i dati sono validi.
        public bool IsValid = false;

        public void Clear()
        {
            DtCells.Clear();
            GvdRawCells.Clear();
            GvdNodes.Clear();
            GvdEdges.Clear();
            IsValid = false;
        }
    }
}
