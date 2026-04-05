// =============================================================================
// PathfindingState.cs
// Namespace: Arcontio.Core
// =============================================================================
//
// MOTIVAZIONE ARCHITETTURALE
// ─────────────────────────────────────────────────────────────────────────────
// In precedenza, tutti gli store e la logica di esecuzione del pathfinding
// (macro-route, direct commit, local search, failure learning) erano contenuti
// direttamente in World.cs, che stava diventando un "god object".
//
// Questa classe raccoglie in un unico posto tutto lo stato ESECUTIVO del
// pathfinding che appartiene al Core simulativo puro:
//   - NpcMacroRouteExecutionState  (esecuzione macro-route landmark)
//   - NpcDirectCommitExecutionState (esecuzione path diretto)
//   - NpcGoalLocalSearchExecutionState (esecuzione local search / JPS)
//   - LocalSearchFailureRecord (anti-loop: memoria dei fallimenti locali)
//   - Debug path cells (LM, Direct, Jump) — path cella-per-cella per overlay
//
// RESPONSABILITÀ DI QUESTA CLASSE
// ─────────────────────────────────────────────────────────────────────────────
// - Contenere i dizionari per-NPC dello stato esecutivo del pathfinding.
// - Esporre metodi di lettura/scrittura/ciclo di vita su questi dizionari.
// - NON contenere logica di decisione AI (quella sta nelle Rule/System).
// - NON contenere logica di pianificazione A* (quella sta in World.TryPlanMacroRoute).
//
// CHI USA QUESTA CLASSE
// ─────────────────────────────────────────────────────────────────────────────
// - World: espone `public PathfindingState Pathfinding { get; }` come proprietà.
// - MovementSystem: legge e scrive lo stato esecutivo ad ogni tick.
// - DevOrderNpcMoveToCellCommand: inizia/cancella l'esecuzione su comando dev.
//
// NOTA SUI DEBUG PATH
// ─────────────────────────────────────────────────────────────────────────────
// I tre dizionari DebugLmPathCells, DebugDirectPathCells, DebugJumpPathCells
// sono dati di osservabilità (usati dall'overlay visivo).
// Restano qui perché sono strettamente legati allo stato esecutivo del path:
// vengono scritti e cancellati insieme agli stati di esecuzione.
// In futuro, quando esisterà il WorldViewport bridge, questi potranno essere
// esposti tramite IRenderEvent senza dover toccare questa classe.
// =============================================================================

using Arcontio.Core.Config;
using System;
using System.Collections.Generic;
using UnityEngine;

namespace Arcontio.Core
{
    // =========================================================================
    // LocalSearchFailureRecord
    // =========================================================================
    // Registra il risultato negativo di una micro-ricerca locale per un dato
    // contesto (coppia origine/destinazione). Usato da PathfindingState per
    // evitare che un NPC ripeta immediatamente la stessa ricerca fallita.
    //
    // NOTA: è [Serializable] perché in futuro potremmo voler salvare/caricare
    // lo stato di fallimento (ad es. per save/load della simulazione).
    [Serializable]
    public sealed class LocalSearchFailureRecord
    {
        /// <summary>Numero totale di fallimenti registrati per questa firma.</summary>
        public int FailureCount;

        /// <summary>Tick in cui si è verificato l'ultimo fallimento (-1 = mai).</summary>
        public int LastFailedTick = -1;

        /// <summary>
        /// Indice di cella (y*width+x) del primo step bloccato durante la ricerca.
        /// Usato per diagnostica e per evitare di riprovare subito la stessa direzione.
        /// -1 = non registrato.
        /// </summary>
        public int BlockedFirstStepCellIndex = -1;

        /// <summary>
        /// Indice di cella dell'ultimo step di progresso prima del fallimento.
        /// Permette di capire fino a che punto la ricerca è arrivata.
        /// -1 = non registrato.
        /// </summary>
        public int LastProgressCellIndex = -1;
    }

    // =========================================================================
    // PathfindingState
    // =========================================================================
    /// <summary>
    /// Contiene tutto lo stato esecutivo del pathfinding per-NPC.
    ///
    /// Viene creata e posseduta da <see cref="World"/>, che la espone tramite
    /// la proprietà <c>Pathfinding</c>.
    ///
    /// I System (MovementSystem) e i Command (DevOrderNpcMoveToCellCommand)
    /// la usano tramite <c>world.Pathfinding.X()</c>.
    /// </summary>
    public sealed class PathfindingState
    {
        // =====================================================================
        // Riferimento alla config — necessario per leggere parametri localSearch
        // senza dipendere dal GlobalState o da singleton statici.
        // =====================================================================
        private readonly WorldConfig _config;

        /// <summary>
        /// Costruttore. Riceve la config del mondo per accedere ai parametri
        /// di local search (commitMinSteps, failureLearning, ecc.).
        /// </summary>
        public PathfindingState(WorldConfig config)
        {
            _config = config;
        }

        // =====================================================================
        // STORE: ESECUZIONE MACRO-ROUTE (landmark A*)
        // =====================================================================
        // npcId → stato di avanzamento sulla macro-route landmark corrente.
        // Scritto da BeginMacroRouteExecutionForNpc / TryAdvanceMacroRouteExecutionAtCell.
        // Letto da MovementSystem per determinare il prossimo target intermedio.
        public readonly Dictionary<int, NpcMacroRouteExecutionState> MacroRouteExecution
            = new Dictionary<int, NpcMacroRouteExecutionState>(256);

        // =====================================================================
        // STORE: ESECUZIONE DIRECT COMMIT (path diretto cella-per-cella)
        // =====================================================================
        // npcId → stato del path diretto pianificato.
        // Attivo quando il Movement System usa un path locale breve senza
        // passare per la macro-route landmark.
        public readonly Dictionary<int, NpcDirectCommitExecutionState> DirectCommitExecution
            = new Dictionary<int, NpcDirectCommitExecutionState>(256);

        // =====================================================================
        // STORE: ESECUZIONE GOAL LOCAL SEARCH (JPS / ricerca locale)
        // =====================================================================
        // npcId → stato della ricerca locale corrente (budget, step rimanenti, ecc.).
        // Attivo quando il path diretto è bloccato e si usa JPS come fallback.
        public readonly Dictionary<int, NpcGoalLocalSearchExecutionState> GoalLocalSearchExecution
            = new Dictionary<int, NpcGoalLocalSearchExecutionState>(256);

        // =====================================================================
        // STORE: FAILURE LADDER — BACK-OFF PER MACRO-ROUTE (v0.03.05-FailureLadder)
        // =====================================================================
        // npcId → stato del back-off corrente.
        // Quando un NPC rimane bloccato per intentStuckTicks consecutivi, invece di
        // cancellare immediatamente l'intent, entra in back-off per N tick.
        // Al termine del back-off, tenta un replan (InitializeNavigation con IsNew=true).
        // Se il replan fallisce di nuovo, incrementa lo stage. Dopo backoff_max_stages
        // fallimenti consecutivi, l'intent viene cancellato (comportamento originale).
        public readonly Dictionary<int, NpcMoveBackOffState> MoveBackOff
            = new Dictionary<int, NpcMoveBackOffState>(256);

        // =====================================================================
        // STORE: FAILURE LEARNING (anti-loop micro-ricerca)
        // =====================================================================
        // npcId → (signature_origine_destinazione → record di fallimento recente).
        // Usato per non ripetere immediatamente una ricerca che ha già fallito
        // poco fa nello stesso contesto spaziale.
        //
        // La signature è un long che comprime le coordinate (origine + target)
        // in 4 short (16 bit ciascuno).
        public readonly Dictionary<int, Dictionary<long, LocalSearchFailureRecord>> FailureLearning
            = new Dictionary<int, Dictionary<long, LocalSearchFailureRecord>>(256);

        // =====================================================================
        // STORE: DEBUG PATH CELLA-PER-CELLA (solo osservabilità)
        // =====================================================================
        // Questi tre dizionari memorizzano il path percorso (o pianificato)
        // suddiviso per tipo di navigazione. Sono usati dall'overlay visivo
        // per disegnare linee colorate che distinguono i tre modalità:
        //   - Verde:   LM_PATH (tratto landmark-to-landmark)
        //   - Azzurro: DIRECT_COMMIT (path diretto pianificato)
        //   - Magenta: GOAL_LOCAL_SEARCH / JPS (ricerca locale)
        //
        // IMPORTANTE: NON sono la fonte di verità del pathfinding.
        //             Sono solo dati di debug/overlay.
        public readonly Dictionary<int, List<GridPosition>> DebugLmPathCells
            = new Dictionary<int, List<GridPosition>>(256);

        public readonly Dictionary<int, List<GridPosition>> DebugDirectPathCells
            = new Dictionary<int, List<GridPosition>>(256);

        public readonly Dictionary<int, List<GridPosition>> DebugJumpPathCells
            = new Dictionary<int, List<GridPosition>>(256);

        // =====================================================================
        // API: MACRO-ROUTE EXECUTION
        // =====================================================================

        /// <summary>
        /// Inizia l'esecuzione della macro-route per un NPC.
        ///
        /// Prerequisito: la macro-route deve essere già stata pianificata e
        /// salvata in <c>World.NpcMacroRoutes[npcId]</c>.
        ///
        /// Questo metodo:
        /// 1) Verifica che la route esista e sia valida.
        /// 2) Crea lo stato iniziale di esecuzione (NextRouteNodeIndex, ImmediateTarget, ecc.).
        /// 3) Salta il primo nodo se l'NPC è già sopra di esso.
        /// </summary>
        /// <param name="npcId">ID dell'NPC.</param>
        /// <param name="targetX">Cella X di destinazione finale.</param>
        /// <param name="targetY">Cella Y di destinazione finale.</param>
        /// <param name="npcPos">Posizione attuale dell'NPC (per skip nodo iniziale).</param>
        /// <param name="macroRoutes">Dizionario dei piani macro-route (da World).</param>
        /// <param name="landmarkRegistry">Registry landmark (da World).</param>
        public void BeginMacroRouteExecution(
            int npcId,
            int targetX,
            int targetY,
            GridPosition npcPos,
            Dictionary<int, NpcMacroRoutePlan> macroRoutes,
            LandmarkRegistry landmarkRegistry)
        {
            // Se non esiste una route pianificata e valida, non possiamo iniziare.
            if (!macroRoutes.TryGetValue(npcId, out var plan) || plan == null || !plan.Succeeded)
            {
                MacroRouteExecution.Remove(npcId);
                return;
            }

            // Stato iniziale di esecuzione.
            var state = new NpcMacroRouteExecutionState
            {
                Active                = true,
                IsDoingLastMile       = false,
                NextRouteNodeIndex    = 0,
                FinalTargetCellX      = targetX,
                FinalTargetCellY      = targetY,
                ImmediateTargetX      = targetX,
                ImmediateTargetY      = targetY,
                FailureReason         = string.Empty,
                // Patch 0.02.07.B: l'NPC parte sempre in approaching finché
                // non raggiunge fisicamente il primo nodo landmark.
                IsApproachingFirstLm  = true,
            };

            // Se l'NPC è già sopra il primo nodo della route, lo saltiamo subito.
            if (plan.NodeIds.Count > 0 && landmarkRegistry != null
                && landmarkRegistry.TryGetActiveNodeById(plan.NodeIds[0], out var startNode)
                && startNode != null
                && startNode.CellX == npcPos.X && startNode.CellY == npcPos.Y)
            {
                state.NextRouteNodeIndex = 1;
            }

            // Determina il target immediato: prossimo nodo landmark o last-mile.
            if (state.NextRouteNodeIndex >= plan.NodeIds.Count)
            {
                // Route già completata (NPC era già al target): entra in last-mile.
                state.IsDoingLastMile    = true;
                state.ImmediateTargetX   = targetX;
                state.ImmediateTargetY   = targetY;
                state.NavigationMode       = "LAST_MILE";
                state.LastModeSwitchTick   = (int)TickContext.CurrentTickIndex;
                state.LastModeSwitchReason = "AlreadyAtTarget";
            }
            else if (landmarkRegistry != null
                     && landmarkRegistry.TryGetActiveNodeById(plan.NodeIds[state.NextRouteNodeIndex], out var nextNode)
                     && nextNode != null)
            {
                // Punta al prossimo nodo landmark intermedio.
                state.ImmediateTargetX = nextNode.CellX;
                state.ImmediateTargetY = nextNode.CellY;
            }
            else
            {
                // Nodo non trovato nel registry: fallback a last-mile diretto.
                state.IsDoingLastMile    = true;
                state.ImmediateTargetX   = targetX;
                state.ImmediateTargetY   = targetY;
                state.NavigationMode       = "LAST_MILE";
                state.LastModeSwitchTick   = (int)TickContext.CurrentTickIndex;
                state.LastModeSwitchReason = "NodeNotFound";
            }

            // Patch 0.02.07.A: al momento dell'inizializzazione la modalità è
            // "APPROACHING_LM": l'NPC si sta avvicinando al primo LM, non ancora
            // percorrendo il tratto LM→LM. Cambia in "LM_PATH" al primo AppendDebugLmStep.
            state.NavigationMode       = "APPROACHING_LM";
            state.LastModeSwitchTick   = (int)TickContext.CurrentTickIndex;
            state.LastModeSwitchReason = "RouteStarted";
            MacroRouteExecution[npcId] = state;
        }

        /// <summary>
        /// Tenta di avanzare l'esecuzione della macro-route quando l'NPC
        /// raggiunge la cella <paramref name="cellX"/>, <paramref name="cellY"/>.
        ///
        /// Se la cella coincide con il prossimo nodo landmark atteso,
        /// avanza NextRouteNodeIndex e aggiorna ImmediateTarget.
        /// </summary>
        /// <returns>True se lo stato è cambiato (il route è avanzato).</returns>
        public bool TryAdvanceMacroRouteAtCell(
            int npcId,
            int cellX,
            int cellY,
            Dictionary<int, NpcMacroRoutePlan> macroRoutes,
            LandmarkRegistry landmarkRegistry)
        {
            if (!MacroRouteExecution.TryGetValue(npcId, out var state) || state == null || !state.Active)
                return false;
            if (!macroRoutes.TryGetValue(npcId, out var plan) || plan == null || !plan.Succeeded)
                return false;

            bool changed = false;

            if (!state.IsDoingLastMile)
            {
                // Avanza finché il nodo corrente è la cella dove siamo arrivati.
                while (state.NextRouteNodeIndex < plan.NodeIds.Count)
                {
                    if (landmarkRegistry == null
                        || !landmarkRegistry.TryGetActiveNodeById(plan.NodeIds[state.NextRouteNodeIndex], out var nextNode)
                        || nextNode == null)
                    {
                        // Nodo non più disponibile: vai in last-mile.
                        state.IsDoingLastMile   = true;
                        state.ImmediateTargetX  = state.FinalTargetCellX;
                        state.ImmediateTargetY  = state.FinalTargetCellY;
                        state.NavigationMode       = "LAST_MILE";
                        state.LastModeSwitchTick   = (int)TickContext.CurrentTickIndex;
                        state.LastModeSwitchReason = "NodeUnavailable";
                        changed = true;
                        break;
                    }

                    // Questo nodo non è ancora stato raggiunto: fermati.
                    if (nextNode.CellX != cellX || nextNode.CellY != cellY)
                        break;

                    // Nodo raggiunto: passa al successivo.
                    state.NextRouteNodeIndex++;
                    // Patch 0.02.07.B: il primo nodo è stato raggiunto →
                    // non siamo più in approaching, siamo in LM_PATH.
                    state.IsApproachingFirstLm = false;
                    changed = true;
                }

                // Aggiorna il target immediato dopo l'avanzamento.
                if (state.NextRouteNodeIndex >= plan.NodeIds.Count)
                {
                    // Tutti i nodi percorsi: entra in last-mile.
                    state.IsDoingLastMile   = true;
                    state.ImmediateTargetX  = state.FinalTargetCellX;
                    state.ImmediateTargetY  = state.FinalTargetCellY;
                    // Patch 0.02.07.A fix: aggiorna NavMode a LAST_MILE
                    // così la card non mostra più LM_PATH nel tratto finale.
                    state.NavigationMode       = "LAST_MILE";
                    state.LastModeSwitchTick   = (int)TickContext.CurrentTickIndex;
                    state.LastModeSwitchReason = "AllNodesTraversed";
                    changed = true;
                }
                else if (!state.IsDoingLastMile
                         && landmarkRegistry != null
                         && landmarkRegistry.TryGetActiveNodeById(plan.NodeIds[state.NextRouteNodeIndex], out var current)
                         && current != null)
                {
                    state.ImmediateTargetX = current.CellX;
                    state.ImmediateTargetY = current.CellY;
                }
            }
            else
            {
                // In last-mile: il target rimane sempre la cella finale.
                state.ImmediateTargetX = state.FinalTargetCellX;
                state.ImmediateTargetY = state.FinalTargetCellY;
            }

            MacroRouteExecution[npcId] = state;
            return changed;
        }

        /// <summary>
        /// Restituisce il target immediato corrente della macro-route in esecuzione.
        /// </summary>
        /// <returns>True se esiste uno stato attivo, false altrimenti.</returns>
        public bool TryGetMacroExecutionImmediateTarget(
            int npcId,
            out int targetX,
            out int targetY,
            out bool isLastMile,
            out int nextNodeId,
            Dictionary<int, NpcMacroRoutePlan> macroRoutes)
        {
            targetX    = 0;
            targetY    = 0;
            isLastMile = false;
            nextNodeId = 0;

            if (!MacroRouteExecution.TryGetValue(npcId, out var state) || state == null || !state.Active)
                return false;

            targetX    = state.ImmediateTargetX;
            targetY    = state.ImmediateTargetY;
            isLastMile = state.IsDoingLastMile;

            // Recupera il nodeId del prossimo nodo se non siamo in last-mile.
            if (!isLastMile
                && macroRoutes.TryGetValue(npcId, out var plan)
                && plan != null
                && state.NextRouteNodeIndex >= 0
                && state.NextRouteNodeIndex < plan.NodeIds.Count)
            {
                nextNodeId = plan.NodeIds[state.NextRouteNodeIndex];
            }

            return true;
        }

        /// <summary>
        /// Marca l'esecuzione della macro-route come bloccata (fallimento).
        /// Il MovementSystem chiama questo metodo quando l'NPC non riesce
        /// a raggiungere il prossimo landmark o la cella finale.
        /// </summary>
        public void MarkMacroRouteBlocked(int npcId, bool duringLastMile)
        {
            if (!MacroRouteExecution.TryGetValue(npcId, out var state) || state == null)
                return;

            state.Active        = false;
            state.FailureReason = duringLastMile ? "BlockedLastMile" : "BlockedToNextLandmark";
            MacroRouteExecution[npcId] = state;
        }

        // =====================================================================
        // API: FAILURE LADDER — BACK-OFF (v0.03.05-FailureLadder)
        // =====================================================================

        /// <summary>
        /// Avvia o incrementa il back-off per un NPC che si è bloccato.
        /// Lo stage indica il numero di fallimenti consecutivi (1 = primo, 2 = secondo, ...).
        /// La durata del back-off scala con lo stage secondo i parametri di configurazione.
        /// </summary>
        public void BeginMoveBackOff(int npcId, long nowTick, int stage, MovementParams mvParams)
        {
            int duration = stage <= 1
                ? (mvParams?.backoff_stage1_ticks ?? 24)
                : (mvParams?.backoff_stage2_ticks ?? 60);

            MoveBackOff[npcId] = new NpcMoveBackOffState
            {
                Active       = true,
                ResumeAtTick = nowTick + duration,
                Stage        = stage
            };
        }

        /// <summary>
        /// True se l'NPC è attualmente in back-off e non deve muoversi.
        /// </summary>
        public bool IsMoveBackOffActive(int npcId, long nowTick)
        {
            if (!MoveBackOff.TryGetValue(npcId, out var s) || s == null || !s.Active)
                return false;
            return nowTick < s.ResumeAtTick;
        }

        /// <summary>
        /// Se il back-off è scaduto in questo tick, lo disattiva e ritorna true
        /// insieme allo stage appena terminato. Il chiamante deve tentare un replan.
        /// Ritorna false se l'NPC non è in back-off o il back-off non è ancora scaduto.
        /// </summary>
        public bool TryExpireMoveBackOff(int npcId, long nowTick, out int stage)
        {
            stage = 0;
            if (!MoveBackOff.TryGetValue(npcId, out var s) || s == null || !s.Active)
                return false;
            if (nowTick < s.ResumeAtTick)
                return false;

            stage    = s.Stage;
            s.Active = false;
            MoveBackOff[npcId] = s;
            return true;
        }

        /// <summary>
        /// Stage del back-off corrente (0 se nessun back-off attivo o registrato).
        /// </summary>
        public int GetMoveBackOffStage(int npcId)
        {
            if (!MoveBackOff.TryGetValue(npcId, out var s) || s == null)
                return 0;
            return s.Stage;
        }

        /// <summary>
        /// Cancella il back-off per un NPC (intent completato con successo o cancellato).
        /// </summary>
        public void ClearMoveBackOff(int npcId)
        {
            MoveBackOff.Remove(npcId);
        }

        // =====================================================================
        // API: DEBUG PATH CELLA-PER-CELLA
        // =====================================================================

        /// <summary>
        /// Imposta il path diretto (DIRECT_COMMIT) per un NPC, sovrascrivendo
        /// il path precedente e aggiornando lo stato di esecuzione DirectCommit.
        /// </summary>
        public void SetDebugDirectPath(int npcId, List<Vector2Int> path)
        {
            // Patch fix: SetDebugDirectPath NON scrive più in DebugDirectPathCells.
            // DebugDirectPathCells viene popolato esclusivamente da AppendDebugDirectStep
            // (passi reali eseguiti, step by step). Scrivere qui il path pianificato
            // causava la visualizzazione di due segmenti sovrapposti: la linea retta
            // pianificata + il percorso reale accumulato dagli append.
            // Il path pianificato è ancora disponibile in DirectCommitExecution.CurrentPath
            // per la logica di navigazione, ma non viene mostrato nell'overlay.
            //
            // Pulizia esplicita: quando inizia un nuovo path direct, azzeriamo i passi
            // precedenti in DebugDirectPathCells per evitare residui del percorso precedente.
            if (DebugDirectPathCells.TryGetValue(npcId, out var existingList))
                existingList.Clear();

            // Copia in una lista temporanea solo per costruire DirectCommitExecution.
            var tempList = new List<GridPosition>(path?.Count ?? 0);
            if (path != null)
                for (int i = 0; i < path.Count; i++)
                    tempList.Add(new GridPosition(path[i].x, path[i].y));

            var state = new NpcDirectCommitExecutionState
            {
                Active           = tempList.Count >= 2,
                FinalTargetCellX = tempList.Count > 0 ? tempList[tempList.Count - 1].X : 0,
                FinalTargetCellY = tempList.Count > 0 ? tempList[tempList.Count - 1].Y : 0,
                ImmediateTargetX = tempList.Count > 1 ? tempList[1].X : 0,
                ImmediateTargetY = tempList.Count > 1 ? tempList[1].Y : 0,
                NextPathIndex    = tempList.Count > 1 ? 1 : 0,
                FailureReason    = string.Empty,
            };
            state.CurrentPath.Clear();
            state.CurrentPath.AddRange(tempList);
            DirectCommitExecution[npcId] = state;

            // Aggiorna la modalità di navigazione nello stato della macro-route (se attiva).
            if (MacroRouteExecution.TryGetValue(npcId, out var macro) && macro != null)
            {
                macro.NavigationMode      = state.Active ? "DIRECT_COMMIT" : macro.NavigationMode;
                macro.LastModeSwitchTick  = (int)TickContext.CurrentTickIndex;
                macro.LastModeSwitchReason = state.Active ? "DirectPathPrepared" : macro.LastModeSwitchReason;
                MacroRouteExecution[npcId] = macro;
            }
        }

        /// <summary>
        /// Imposta il path di local search / JPS (GOAL_LOCAL_SEARCH) per un NPC,
        /// disattivando il DirectCommit precedente e aggiornando la macro-route.
        /// </summary>
        public void SetDebugJumpPath(int npcId, List<Vector2Int> path, int budgetRemaining)
        {
            var list = EnsureDebugPathList(DebugJumpPathCells, npcId);
            CopyVectorPathToGridPath(path, list);

            var state = new NpcGoalLocalSearchExecutionState
            {
                Active              = list.Count >= 2,
                FinalTargetCellX    = list.Count > 0 ? list[list.Count - 1].X : 0,
                FinalTargetCellY    = list.Count > 0 ? list[list.Count - 1].Y : 0,
                ImmediateTargetX    = list.Count > 1 ? list[1].X : 0,
                ImmediateTargetY    = list.Count > 1 ? list[1].Y : 0,
                BudgetRemaining     = budgetRemaining,
                NextPathIndex       = list.Count > 1 ? 1 : 0,
                CommitStepsRemaining = GetLocalSearchCommitMinSteps(),
                HasLastSuccessfulStep = false,
                LastStepFromX       = 0,
                LastStepFromY       = 0,
                LastStepToX         = 0,
                LastStepToY         = 0,
                FailureReason       = string.Empty,
            };
            state.CurrentPath.Clear();
            state.CurrentPath.AddRange(list);
            GoalLocalSearchExecution[npcId] = state;

            // Disattiva il DirectCommit: la local search ha la precedenza.
            if (DirectCommitExecution.TryGetValue(npcId, out var direct) && direct != null)
            {
                direct.Active        = false;
                direct.FailureReason = string.Empty;
                DirectCommitExecution[npcId] = direct;
            }

            // Aggiorna la modalità di navigazione nello stato della macro-route (se attiva).
            if (MacroRouteExecution.TryGetValue(npcId, out var macro) && macro != null)
            {
                macro.NavigationMode       = state.Active ? "GOAL_LOCAL_SEARCH" : macro.NavigationMode;
                macro.LastModeSwitchTick   = (int)TickContext.CurrentTickIndex;
                macro.LastModeSwitchReason = state.Active ? "DirectBlocked" : macro.LastModeSwitchReason;
                MacroRouteExecution[npcId] = macro;
            }
        }

        /// <summary>
        /// Aggiunge uno step al debug path LM (tratto landmark-to-landmark verde).
        /// Chiamato da MovementSystem ogni volta che l'NPC avanza in modalità LM_PATH.
        /// </summary>
        /// <summary>
        /// Aggiunge uno step al debug path LM (tratto verde landmark→landmark).
        /// Patch 0.02.07.A: al primo step reale transisce APPROACHING_LM → LM_PATH,
        /// cosi' la card NPC mostra "LM_PATH" solo quando l'NPC sta davvero
        /// percorrendo il tratto tra due nodi, non mentre si avvicina al primo.
        /// </summary>
        public void AppendDebugLmStep(int npcId, int fromX, int fromY, int toX, int toY)
        {
            AppendDebugStep(DebugLmPathCells, npcId, fromX, fromY, toX, toY);

            // Transizione APPROACHING_LM → LM_PATH al primo passo effettivo.
            if (MacroRouteExecution.TryGetValue(npcId, out var state) && state != null
                && state.NavigationMode == "APPROACHING_LM")
            {
                state.NavigationMode       = "LM_PATH";
                state.LastModeSwitchTick   = (int)TickContext.CurrentTickIndex;
                state.LastModeSwitchReason = "FirstLmStep";
                MacroRouteExecution[npcId] = state;

                // Fix bug 1 (doppio segmento nel last-mile):
                // Il tratto approaching ha scritto passi in DebugDirectPathCells (azzurro).
                // Ora che inizia LM_PATH, puliamo DebugDirectPathCells così il buffer
                // è vuoto quando arriveremo al last-mile. Senza questa pulizia,
                // il last-mile aggiunge i suoi passi allo stesso buffer dell'approaching
                // producendo due segmenti sovrapposti.
                if (DebugDirectPathCells.TryGetValue(npcId, out var directList))
                    directList.Clear();
            }
        }

        /// <summary>
        /// Aggiunge uno step al debug path Direct (azzurro).
        ///
        /// Patch 0.02.07.B fix last-mile: questo metodo ora aggiunge effettivamente
        /// lo step a DebugDirectPathCells. In precedenza era vuoto perché il path
        /// diretto veniva sempre pre-calcolato da SetDebugDirectPath in InitializeNavigation.
        /// Nel last-mile però non c'è pre-calcolo: i passi vengono tracciati step by step
        /// (sia in DIRECT_APPROACHING che nel fallback greedy). Senza questa modifica,
        /// il last-mile non produce nessun segmento colorato nell'overlay.
        ///
        /// Invariante mantenuto: se SetDebugDirectPath ha già scritto un path pianificato,
        /// i passi successivi si appendono alla stessa lista — l'overlay mostra il path
        /// pianificato più i passi già eseguiti.
        /// </summary>
        public void AppendDebugDirectStep(int npcId, int fromX, int fromY, int toX, int toY)
        {
            AppendDebugStep(DebugDirectPathCells, npcId, fromX, fromY, toX, toY);
        }

        /// <summary>
        /// Aggiunge uno step al debug path Jump (magenta).
        /// Attualmente intenzionalmente vuoto: manteniamo solo il path locale corrente.
        /// </summary>
        public void AppendDebugJumpStep(int npcId, int fromX, int fromY, int toX, int toY)
        {
            // Intenzionalmente vuoto: il path jump viene impostato per intero in SetDebugJumpPath.
        }

        // =====================================================================
        // API: PULIZIA STATO PER NPC
        // =====================================================================

        /// <summary>
        /// Cancella tutti i debug path e gli stati DirectCommit e GoalLocalSearch
        /// per un NPC. Usato quando si inizia una nuova navigazione o si resetta.
        /// Non tocca MacroRouteExecution (quello viene gestito separatamente).
        /// </summary>
        public void ClearDebugNavigationPaths(int npcId)
        {
            DebugLmPathCells.Remove(npcId);
            DebugDirectPathCells.Remove(npcId);
            DebugJumpPathCells.Remove(npcId);
            DirectCommitExecution.Remove(npcId);
            GoalLocalSearchExecution.Remove(npcId);
        }

        /// <summary>
        /// Cancella la macro-route e il suo stato di esecuzione per un NPC.
        /// </summary>
        public void ClearMacroRoute(int npcId, Dictionary<int, NpcMacroRoutePlan> macroRoutes)
        {
            macroRoutes.Remove(npcId);
            MacroRouteExecution.Remove(npcId);
        }

        /// <summary>
        /// Azzera lo stato della local search per un NPC.
        /// Imposta Active=false e registra il motivo del fallimento.
        /// </summary>
        public void ClearLocalSearchState(int npcId, string failureReason = "")
        {
            // Rimuovi il path di debug jump.
            DebugJumpPathCells.Remove(npcId);

            if (GoalLocalSearchExecution.TryGetValue(npcId, out var state) && state != null)
            {
                state.Active        = false;
                state.FailureReason = failureReason ?? string.Empty;
                GoalLocalSearchExecution[npcId] = state;
            }
            else
            {
                GoalLocalSearchExecution.Remove(npcId);
            }
        }

        /// <summary>
        /// Azzera lo stato del direct commit per un NPC.
        /// Imposta Active=false e registra il motivo del fallimento.
        /// </summary>
        public void ClearDirectCommitState(int npcId, string failureReason = "")
        {
            if (DirectCommitExecution.TryGetValue(npcId, out var state) && state != null)
            {
                state.Active        = false;
                state.FailureReason = failureReason ?? string.Empty;
                DirectCommitExecution[npcId] = state;
            }
            else
            {
                DirectCommitExecution.Remove(npcId);
            }
        }

        // =====================================================================
        // API: FAILURE LEARNING
        // =====================================================================

        /// <summary>
        /// Registra un fallimento della local search per un dato contesto spaziale.
        /// Usato per evitare che l'NPC riprovi immediatamente la stessa ricerca fallita.
        ///
        /// La signature è calcolata comprimendo le 4 coordinate (origine + target)
        /// in un long (4 × 16 bit).
        /// </summary>
        public void RememberLocalSearchFailure(
            int npcId,
            int originX,
            int originY,
            int targetX,
            int targetY,
            int blockedFirstStepCellIndex,
            int lastProgressCellIndex)
        {
            // Controlla se il failure learning è abilitato nella config.
            var cfg = _config?.Sim?.landmarks?.localSearch ?? new LandmarkLocalSearchParams();
            if (!cfg.enableFailureLearning)
                return;

            int nowTick = (int)TickContext.CurrentTickIndex;
            var map     = EnsureFailureLearningMap(npcId);
            var sig     = MakeLocalSearchFailureSignature(originX, originY, targetX, targetY);

            if (!map.TryGetValue(sig, out var rec) || rec == null)
            {
                rec     = new LocalSearchFailureRecord();
                map[sig] = rec;
            }

            rec.FailureCount++;
            rec.LastFailedTick            = nowTick;
            rec.BlockedFirstStepCellIndex = blockedFirstStepCellIndex;
            rec.LastProgressCellIndex     = lastProgressCellIndex;
        }

        /// <summary>
        /// Rimuove il record di fallimento per un dato contesto spaziale
        /// (la ricerca è riuscita: non serve più ricordare il fallimento).
        /// </summary>
        public void RememberLocalSearchSuccess(int npcId, int originX, int originY, int targetX, int targetY)
        {
            if (!FailureLearning.TryGetValue(npcId, out var map) || map == null)
                return;

            map.Remove(MakeLocalSearchFailureSignature(originX, originY, targetX, targetY));
        }

        /// <summary>
        /// Cerca un record di fallimento recente per un dato contesto spaziale.
        /// Restituisce true se esiste un record non scaduto.
        /// </summary>
        public bool TryGetRecentLocalSearchFailure(
            int npcId,
            long signature,
            int memoryTicks,
            int nowTick,
            out LocalSearchFailureRecord record)
        {
            record = null;

            // Prima di leggere, rimuovi i record scaduti.
            PruneExpiredLocalSearchFailures(npcId, memoryTicks, nowTick);

            if (!FailureLearning.TryGetValue(npcId, out var map) || map == null)
                return false;

            if (!map.TryGetValue(signature, out record) || record == null)
                return false;

            // Controllo di sicurezza: il record è ancora valido?
            if (memoryTicks > 0 && nowTick - record.LastFailedTick > memoryTicks)
                return false;

            return true;
        }

        /// <summary>
        /// Calcola la firma del contesto spaziale di una local search.
        /// Comprime le 4 coordinate in un long (4 × 16 bit, senza overflow).
        /// </summary>
        public long MakeLocalSearchFailureSignature(int originX, int originY, int targetX, int targetY)
        {
            unchecked
            {
                long a = ((long)(originX & 0xFFFF) << 48);
                long b = ((long)(originY & 0xFFFF) << 32);
                long c = ((long)(targetX  & 0xFFFF) << 16);
                long d =  (long)(targetY  & 0xFFFF);
                return a | b | c | d;
            }
        }

        /// <summary>
        /// Cancella tutti i record di failure learning per un NPC.
        /// Chiamato quando l'NPC viene rimosso dal mondo o resettato.
        /// </summary>
        public void ClearLocalSearchFailureLearning(int npcId)
        {
            FailureLearning.Remove(npcId);
        }

        // =====================================================================
        // API: DEBUG REPORT (per overlay/card UI)
        // =====================================================================

        /// <summary>
        /// Riempie i layer di overlay cella-per-cella per il renderer.
        /// Chiamato da World.GetNpcLandmarkOverlayData per il bridge visivo.
        ///
        /// Nota: questo metodo non fa rendering — produce dati di edge
        /// che il renderer usa per disegnare le linee di path.
        /// </summary>
        public void FillDebugNavigationPathOverlayData(
            int npcId,
            List<LandmarkOverlayEdge> outLmPathEdges,
            List<LandmarkOverlayEdge> outDirectPathEdges,
            List<LandmarkOverlayEdge> outJumpPathEdges)
        {
            if (outLmPathEdges    != null) outLmPathEdges.Clear();
            if (outDirectPathEdges != null) outDirectPathEdges.Clear();
            if (outJumpPathEdges  != null) outJumpPathEdges.Clear();

            if (DebugLmPathCells.TryGetValue(npcId, out var lmCells) && lmCells != null)
                AppendOverlayEdgesFromCellPath(lmCells, outLmPathEdges);

            if (DebugDirectPathCells.TryGetValue(npcId, out var directCells) && directCells != null)
                AppendOverlayEdgesFromCellPath(directCells, outDirectPathEdges);

            if (DebugJumpPathCells.TryGetValue(npcId, out var jumpCells) && jumpCells != null)
                AppendOverlayEdgesFromCellPath(jumpCells, outJumpPathEdges);
        }

        // =====================================================================
        // HELPER PRIVATI
        // =====================================================================

        /// <summary>
        /// Recupera (o crea) la mappa dei failure record per un NPC.
        /// </summary>
        private Dictionary<long, LocalSearchFailureRecord> EnsureFailureLearningMap(int npcId)
        {
            if (!FailureLearning.TryGetValue(npcId, out var map) || map == null)
            {
                map = new Dictionary<long, LocalSearchFailureRecord>(16);
                FailureLearning[npcId] = map;
            }
            return map;
        }

        /// <summary>
        /// Rimuove dalla mappa failure learning di un NPC i record più vecchi
        /// di <paramref name="memoryTicks"/> tick.
        /// </summary>
        private void PruneExpiredLocalSearchFailures(int npcId, int memoryTicks, int nowTick)
        {
            if (memoryTicks <= 0)
                return;
            if (!FailureLearning.TryGetValue(npcId, out var map) || map == null || map.Count == 0)
                return;

            var toRemove = new List<long>(4);
            foreach (var kv in map)
            {
                var rec = kv.Value;
                if (rec == null || nowTick - rec.LastFailedTick > memoryTicks)
                    toRemove.Add(kv.Key);
            }

            for (int i = 0; i < toRemove.Count; i++)
                map.Remove(toRemove[i]);
        }

        /// <summary>
        /// Recupera (o crea) la lista di GridPosition per i debug path di un NPC.
        /// </summary>
        private static List<GridPosition> EnsureDebugPathList(
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

        /// <summary>
        /// Copia un path da List&lt;Vector2Int&gt; (Unity) a List&lt;GridPosition&gt; (Core).
        /// </summary>
        private static void CopyVectorPathToGridPath(List<Vector2Int> src, List<GridPosition> dst)
        {
            dst.Clear();
            if (src == null)
                return;

            for (int i = 0; i < src.Count; i++)
                dst.Add(new GridPosition(src[i].x, src[i].y));
        }

        /// <summary>
        /// Aggiunge un singolo step a un debug path store.
        /// Se il path è vuoto, aggiunge prima il punto di partenza.
        /// Se il punto di partenza non corrisponde all'ultimo punto,
        /// inserisce comunque il from per mantenere la continuità visiva.
        /// </summary>
        private static void AppendDebugStep(
            Dictionary<int, List<GridPosition>> store,
            int npcId,
            int fromX, int fromY,
            int toX,   int toY)
        {
            var list = EnsureDebugPathList(store, npcId);

            if (list.Count == 0)
            {
                list.Add(new GridPosition(fromX, fromY));
            }
            else
            {
                var last = list[list.Count - 1];
                if (last.X != fromX || last.Y != fromY)
                    list.Add(new GridPosition(fromX, fromY));
            }

            list.Add(new GridPosition(toX, toY));
        }

        /// <summary>
        /// Converte una lista di GridPosition in una lista di LandmarkOverlayEdge
        /// (segmenti consecutivi da A a B) da usare nell'overlay visivo.
        /// </summary>
        private static void AppendOverlayEdgesFromCellPath(
            List<GridPosition> path,
            List<LandmarkOverlayEdge> outEdges)
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

        /// <summary>
        /// Legge il numero minimo di step di commit dalla config locale.
        /// Se non configurato, restituisce 3 (default sicuro).
        /// </summary>
        private int GetLocalSearchCommitMinSteps()
        {
            return Mathf.Max(1, _config?.Sim?.landmarks?.localSearch?.commitMinSteps ?? 3);
        }
    }
}
