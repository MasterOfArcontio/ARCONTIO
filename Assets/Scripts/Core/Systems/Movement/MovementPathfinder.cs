// =============================================================================
// MovementPathfinder.cs
// Namespace: Arcontio.Core
// Patch: 0.02.05.B
// =============================================================================
//
// MOTIVAZIONE (Patch 0.02.05.B)
// ─────────────────────────────────────────────────────────────────────────────
// Prima di questa patch, tutti gli algoritmi di pathfinding locale erano
// metodi di World.cs, dove non appartengono. World deve contenere DATI e
// operazioni atomiche su di essi — non algoritmi di navigazione.
//
// I metodi estratti qui erano in World.cs perché erano stati aggiunti durante
// il Giorno 5 nel posto più comodo disponibile, non in quello architetturalmente
// corretto. L'unico consumer reale era MovementSystem.
//
// RESPONSABILITÀ DI QUESTA CLASSE
// ─────────────────────────────────────────────────────────────────────────────
// Algoritmi di navigazione locale per il MovementSystem:
//   - Greedy direct path (path Manhattan senza ostacoli)
//   - Greedy direct prefix path (path diretto fino al primo blocco)
//   - Bounded move path (BFS/JPS locale per aggirare ostacoli)
//   - Local search state management (HasActiveNpcLocalSearch, TryReplanNpcLocalSearch, ecc.)
//   - IsWalkableForPathing (gate di camminabilità per i pathfinder)
//
// TUTTI I METODI SONO STATICI e ricevono World come parametro.
// Non mantengono stato proprio: sono algoritmi puri che leggono
// e scrivono il World in modo esplicito.
//
// ACCESSO DA ALTRI SISTEMI
// ─────────────────────────────────────────────────────────────────────────────
// World espone thin wrapper pubblici (CanNpcUseDirectPath, TryBuildGreedyDirectPath,
// ecc.) che delegano qui. Questo mantiene la compatibilità con il codice esistente
// senza richiedere modifiche a tutti i consumer.
// =============================================================================

using Arcontio.Core.Config;
using Arcontio.Core.Logging;
using System;
using System.Collections.Generic;
using UnityEngine;

namespace Arcontio.Core
{
    /// <summary>
    /// <b>MovementPathfinder</b> — algoritmi di navigazione locale per il MovementSystem.
    ///
    /// <para>
    /// Classe statica: zero stato, zero allocazioni proprie.
    /// Tutti i metodi ricevono <see cref="World"/> come parametro esplicito.
    /// </para>
    ///
    /// <para><b>Pipeline di navigazione locale (Patch 0.02.05.B):</b></para>
    /// <list type="number">
    ///   <item><see cref="CanNpcUseDirectPath"/> — il target è raggiungibile con greedy?</item>
    ///   <item><see cref="TryBuildGreedyDirectPath"/> — costruisce il path diretto completo.</item>
    ///   <item><see cref="TryBuildBoundedMovePath"/> — BFS/JPS locale per aggirare ostacoli.</item>
    /// </list>
    ///
    /// <para>
    /// La gestione dello stato di local search in esecuzione
    /// (<c>HasActiveNpcLocalSearch</c>, <c>TryReplanNpcLocalSearch</c>, ecc.)
    /// è qui perché è strettamente accoppiata agli algoritmi di pathfinding
    /// e al <see cref="MovementSystem"/> che li consuma.
    /// </para>
    ///
    /// <para><b>Patch:</b> 0.02.05.B / 0.02.06.A / 0.02.07.A</para>
    /// </summary>
    public static class MovementPathfinder
    {
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
        public static bool TryBuildGreedyDirectPath(World world, int npcId, int startX, int startY, int targetX, int targetY, List<Vector2Int> outCells)
        {
            if (outCells == null)
                return false;

            outCells.Clear();

            if (!world.InBounds(startX, startY) || !world.InBounds(targetX, targetY))
                return false;

            outCells.Add(new Vector2Int(startX, startY));

            int x = startX;
            int y = startY;

            // Difesa importante: mettiamo un tetto di sicurezza per evitare loop infiniti
            // in caso di bug logici futuri.
            int safety = world.MapWidth * world.MapHeight + 8;

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
                if (IsWalkableForPathing(world, npcId, nextX, nextY, targetX, targetY))
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

                    if ((stepX != 0 || stepY != 0) && IsWalkableForPathing(world, npcId, nextX, nextY, targetX, targetY))
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
        public static bool TryBuildGreedyDirectPrefixPath(World world, int npcId, int startX, int startY, int targetX, int targetY, List<Vector2Int> outCells)
        {
            if (outCells == null)
                return false;

            outCells.Clear();
            if (!world.InBounds(startX, startY) || !world.InBounds(targetX, targetY))
                return false;

            int x = startX;
            int y = startY;
            outCells.Add(new Vector2Int(x, y));

            int safety = (world.MapWidth * world.MapHeight) + 8;
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
                if (IsWalkableForPathing(world, npcId, nextX, nextY, targetX, targetY))
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
                    if ((stepX != 0 || stepY != 0) && IsWalkableForPathing(world, npcId, nextX, nextY, targetX, targetY))
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

        public static bool HasActiveNpcLocalSearch(World world, int npcId)
        {
            return world.Pathfinding.GoalLocalSearchExecution.TryGetValue(npcId, out var state)
                && state != null
                && state.Active;
        }

        public static bool TryGetActiveNpcLocalSearchNextStep(World world, int npcId, out int stepX, out int stepY)
        {
            stepX = 0;
            stepY = 0;

            if (!world.Pathfinding.GoalLocalSearchExecution.TryGetValue(npcId, out var state) || state == null || !state.Active)
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

        public static void AdvanceNpcLocalSearchAfterSuccessfulStep(World world, int npcId, int fromX, int fromY, int toX, int toY)
        {
            if (!world.Pathfinding.GoalLocalSearchExecution.TryGetValue(npcId, out var state) || state == null || !state.Active)
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

                world.Pathfinding.GoalLocalSearchExecution[npcId] = state;
                world.Pathfinding.DebugJumpPathCells.Remove(npcId);
                SetMacroRouteNavigationMode(world, npcId, "IDLE", "LocalSearchCompletedTargetReached");
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

                world.Pathfinding.GoalLocalSearchExecution[npcId] = state;
                world.Pathfinding.DebugJumpPathCells.Remove(npcId);
                SetMacroRouteNavigationMode(world, npcId, "GOAL_LOCAL_SEARCH", "LocalSearchNeedsReplan");
                return;
            }

            // Caso 3: path locale ancora vivo -> aggiorna il prossimo step e il magenta.
            var nextStep = state.CurrentPath[state.NextPathIndex];
            state.ImmediateTargetX = nextStep.X;
            state.ImmediateTargetY = nextStep.Y;

            world.Pathfinding.GoalLocalSearchExecution[npcId] = state;
            RefreshDebugJumpPathFromLocalState(world, npcId);
            SetMacroRouteNavigationMode(world, npcId, "GOAL_LOCAL_SEARCH", "LocalSearchStepCommitted");
        }

        public static bool TryReplanNpcLocalSearch(World world, int npcId, int currentX, int currentY)
        {
            if (!world.Pathfinding.GoalLocalSearchExecution.TryGetValue(npcId, out var state) || state == null || !state.Active)
                return false;

            var cfg = world.Config?.Sim?.landmarks?.localSearch ?? new LandmarkLocalSearchParams();

            int maxVisited = state.BudgetRemaining > 0
                ? Mathf.Max(8, state.BudgetRemaining)
                : Mathf.Max(8, cfg.maxExpandedNodes);

            var path = new List<Vector2Int>(64);

            if (!TryBuildBoundedMovePath(world, 
                    npcId,
                    currentX,
                    currentY,
                    state.FinalTargetCellX,
                    state.FinalTargetCellY,
                    maxVisited,
                    path) || path.Count < 2)
            {
                state.FailureReason = "LocalReplanFailed";
                world.Pathfinding.GoalLocalSearchExecution[npcId] = state;
                return false;
            }

            // Guardrail anti backtrack immediato: se siamo ancora nel commitment
            // e il nuovo primo passo è l’inverso esatto dell’ultimo passo riuscito, lo rifiutiamo.
            if (ShouldPreventImmediateLocalBacktrack(world)
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
                    world.Pathfinding.GoalLocalSearchExecution[npcId] = state;
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

            world.Pathfinding.GoalLocalSearchExecution[npcId] = state;
            RefreshDebugJumpPathFromLocalState(world, npcId);
            SetMacroRouteNavigationMode(world, npcId, "GOAL_LOCAL_SEARCH", "LocalSearchReplanned");
            return true;
        }

        private static void SetMacroRouteNavigationMode(World world, int npcId, string navigationMode, string reason)
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

            if (!world.Pathfinding.MacroRouteExecution.TryGetValue(npcId, out var exec) || exec == null)
                return;

            exec.NavigationMode = navigationMode ?? string.Empty;
            exec.LastModeSwitchReason = reason ?? string.Empty;
            exec.LastModeSwitchTick = (int)TickContext.CurrentTickIndex;

            world.Pathfinding.MacroRouteExecution[npcId] = exec;
        }

        private static void RefreshDebugJumpPathFromLocalState(World world, int npcId)
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

            if (!world.Pathfinding.GoalLocalSearchExecution.TryGetValue(npcId, out var state)
                || state == null
                || !state.Active
                || state.CurrentPath == null
                || state.CurrentPath.Count < 2)
            {
                world.Pathfinding.DebugJumpPathCells.Remove(npcId);
                return;
            }

            int startIndex = Mathf.Clamp(
                Mathf.Max(0, state.NextPathIndex - 1),
                0,
                state.CurrentPath.Count - 1);

            // Accediamo direttamente al dizionario ora gestito da PathfindingState.
            if (!world.Pathfinding.DebugJumpPathCells.TryGetValue(npcId, out var list) || list == null)
            {
                list = new System.Collections.Generic.List<GridPosition>(64);
                world.Pathfinding.DebugJumpPathCells[npcId] = list;
            }
            list.Clear();

            for (int i = startIndex; i < state.CurrentPath.Count; i++)
                list.Add(state.CurrentPath[i]);

            if (list.Count < 2)
                world.Pathfinding.DebugJumpPathCells.Remove(npcId);
        }

        private static bool ShouldPreventImmediateLocalBacktrack(World world)
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

            return world.Config?.Sim?.landmarks?.localSearch?.preventImmediateBacktrack ?? true;
        }




        /// <summary>
        /// Wrapper comodo per i punti del codice che vogliono solo sapere se il Direct Commit
        /// è legalmente attivabile, senza avere bisogno della lista completa di celle.
        /// </summary>
        public static bool CanNpcUseDirectPath(World world, int npcId, int targetX, int targetY)
        {
            if (!world.GridPos.TryGetValue(npcId, out var pos))
                return false;

            var scratch = new List<Vector2Int>(32);
            return TryBuildGreedyDirectPath(world, npcId, pos.X, pos.Y, targetX, targetY, scratch);
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
        public static bool TryBuildBoundedMovePath(World world, int npcId, int startX, int startY, int targetX, int targetY, int maxVisited, List<Vector2Int> outCells)
        {
            if (outCells == null)
                return false;

            outCells.Clear();

            if (!world.InBounds(startX, startY) || !world.InBounds(targetX, targetY))
                return false;

            var cfg = world.Config?.Sim?.landmarks?.localSearch ?? new Arcontio.Core.Config.LandmarkLocalSearchParams();
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
                long signature = world.Pathfinding.MakeLocalSearchFailureSignature(startX, startY, targetX, targetY);
                if (world.Pathfinding.TryGetRecentLocalSearchFailure(npcId, signature, Mathf.Max(1, cfg.failureMemoryTicks), nowTick, out var recentFailure) && recentFailure != null)
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
                foundCompletePath = TryBuildBoundedJpsPathInternal(world, 
                    npcId, startX, startY, targetX, targetY,
                    expandedLimit, iterationLimit, radiusLimit, jumpLimit, hWeight,
                    blockedFirstStepCellIndex,
                    candidatePath,
                    partialBestPath);
            }
            else
            {
                foundCompletePath = TryBuildSimpleBoundedBfsPathAdvanced(world, 
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
                    if (TryBuildBoundedJpsPathInternal(world, 
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
                    if (TryBuildSimpleBoundedBfsPathAdvanced(world, 
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
                    SmoothCellPath(world, npcId, candidatePath, Mathf.Max(2, cfg.smoothingLookahead), outCells);
                else
                    outCells.AddRange(candidatePath);

                world.Pathfinding.RememberLocalSearchSuccess(npcId, startX, startY, targetX, targetY);
                return outCells.Count >= 2;
            }

            if (partialBestPath.Count >= 2)
            {
                if (cfg.enablePathSmoothing)
                    SmoothCellPath(world, npcId, partialBestPath, Mathf.Max(2, cfg.smoothingLookahead), outCells);
                else
                    outCells.AddRange(partialBestPath);

                int blockedStep = outCells.Count >= 2 ? world.CellIndex(outCells[1].x, outCells[1].y) : -1;
                int progressCell = outCells.Count > 0 ? world.CellIndex(outCells[outCells.Count - 1].x, outCells[outCells.Count - 1].y) : -1;
                world.Pathfinding.RememberLocalSearchFailure(npcId, startX, startY, targetX, targetY, blockedStep, progressCell);
                return outCells.Count >= 2;
            }

            world.Pathfinding.RememberLocalSearchFailure(npcId, startX, startY, targetX, targetY, blockedFirstStepCellIndex, -1);
            return false;
        }

        private static bool TryBuildBoundedJpsPathInternal(World world, int npcId,
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

                var dirs = GetJpsSuccessorDirections(world, npcId, current.X, current.Y, current.DirX, current.DirY, targetX, targetY);
                for (int i = 0; i < dirs.Count; i++)
                {
                    var dir = dirs[i];

                    if (current.X == startX && current.Y == startY && blockedFirstStepCellIndex >= 0)
                    {
                        int firstStepX = current.X + dir.x;
                        int firstStepY = current.Y + dir.y;
                        if (world.InBounds(firstStepX, firstStepY) && world.CellIndex(firstStepX, firstStepY) == blockedFirstStepCellIndex)
                            continue;
                    }

                    if (TryJumpStraight(world, npcId, start, target, current.X, current.Y, dir.x, dir.y, radiusLimit, jumpLimit, out var jumpPoint, out int stepCost))
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
                BuildExpandedPathFromJpsStates(world, foundKey, parents, outPath);
                return outPath.Count > 0 && outPath[outPath.Count - 1] == target;
            }

            BuildExpandedPathFromJpsStates(world, bestFrontierKey, parents, outBestProgressPath);
            return false;
        }

        private static void BuildExpandedPathFromJpsStates(World world, JpsStateKey terminalKey, Dictionary<JpsStateKey, JpsParentInfo> parents, List<Vector2Int> outCells)
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

        private static bool TryBuildSimpleBoundedBfsPathAdvanced(World world, int npcId,
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
                    if (cur == start && blockedFirstStepCellIndex >= 0 && world.InBounds(nxt.x, nxt.y) && world.CellIndex(nxt.x, nxt.y) == blockedFirstStepCellIndex) continue;
                    if (!IsWalkableForPathing(world, npcId, nxt.x, nxt.y, targetX, targetY, usePhysical: true)) continue;
                    visited.Add(nxt); cameFrom[nxt] = cur; frontier.Enqueue(nxt);
                }
            }
            if (found)
            {
                ReconstructGridPath(world, start, target, cameFrom, outPath);
                return outPath.Count > 0 && outPath[outPath.Count - 1] == target;
            }
            if (best != start)
                ReconstructGridPath(world, start, best, cameFrom, outBestProgressPath);
            return false;
        }

        private static void ReconstructGridPath(World world, Vector2Int start, Vector2Int target, Dictionary<Vector2Int, Vector2Int> cameFrom, List<Vector2Int> outPath)
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

        private static void SmoothCellPath(World world, int npcId, List<Vector2Int> rawPath, int lookahead, List<Vector2Int> outSmoothed)
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
                    if (TryBuildGreedyDirectPath(world, npcId, a.x, a.y, b.x, b.y, directScratch) && directScratch.Count >= 2)
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

        private static float HeuristicManhattan(int x, int y, int tx, int ty, float weight)
        {
            return (Mathf.Abs(tx - x) + Mathf.Abs(ty - y)) * weight;
        }

        private static List<Vector2Int> GetJpsSuccessorDirections(World world, int npcId, int x, int y, int dirX, int dirY, int targetX, int targetY)
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
                bool upBlocked = !IsWalkableForPathing(world, npcId, x, y + 1, targetX, targetY, usePhysical: true);
                bool downBlocked = !IsWalkableForPathing(world, npcId, x, y - 1, targetX, targetY, usePhysical: true);
                if (upBlocked && IsWalkableForPathing(world, npcId, x + dirX, y + 1, targetX, targetY, usePhysical: true)) dirs.Add(new Vector2Int(0, 1));
                if (downBlocked && IsWalkableForPathing(world, npcId, x + dirX, y - 1, targetX, targetY, usePhysical: true)) dirs.Add(new Vector2Int(0, -1));
            }
            else
            {
                bool rightBlocked = !IsWalkableForPathing(world, npcId, x + 1, y, targetX, targetY, usePhysical: true);
                bool leftBlocked = !IsWalkableForPathing(world, npcId, x - 1, y, targetX, targetY, usePhysical: true);
                if (rightBlocked && IsWalkableForPathing(world, npcId, x + 1, y + dirY, targetX, targetY, usePhysical: true)) dirs.Add(new Vector2Int(1, 0));
                if (leftBlocked && IsWalkableForPathing(world, npcId, x - 1, y + dirY, targetX, targetY, usePhysical: true)) dirs.Add(new Vector2Int(-1, 0));
            }
            return dirs;
        }

        private static bool TryJumpStraight(World world, int npcId, Vector2Int searchOrigin, Vector2Int target, int x, int y, int dirX, int dirY, int radiusLimit, int jumpLimit, out Vector2Int jumpPoint, out int stepCost)
        {
            jumpPoint = default;
            stepCost = 0;
            int curX = x;
            int curY = y;
            for (int dist = 1; dist <= jumpLimit; dist++)
            {
                curX += dirX; curY += dirY;
                if (!IsWalkableForPathing(world, npcId, curX, curY, target.x, target.y, usePhysical: true))
                    return false;
                if (Mathf.Abs(curX - searchOrigin.x) + Mathf.Abs(curY - searchOrigin.y) > radiusLimit)
                    return false;
                stepCost++;
                if (curX == target.x && curY == target.y)
                {
                    jumpPoint = new Vector2Int(curX, curY);
                    return true;
                }
                if (HasForcedNeighbourAt(world, npcId, curX, curY, dirX, dirY, target.x, target.y))
                {
                    jumpPoint = new Vector2Int(curX, curY);
                    return true;
                }
            }
            return false;
        }

        private static bool HasForcedNeighbourAt(World world, int npcId, int x, int y, int dirX, int dirY, int targetX, int targetY)
        {
            if (dirX != 0)
            {
                bool upBlocked = !IsWalkableForPathing(world, npcId, x, y + 1, targetX, targetY, usePhysical: true);
                bool downBlocked = !IsWalkableForPathing(world, npcId, x, y - 1, targetX, targetY, usePhysical: true);
                if (upBlocked && IsWalkableForPathing(world, npcId, x + dirX, y + 1, targetX, targetY, usePhysical: true)) return true;
                if (downBlocked && IsWalkableForPathing(world, npcId, x + dirX, y - 1, targetX, targetY, usePhysical: true)) return true;
                return false;
            }
            if (dirY != 0)
            {
                bool rightBlocked = !IsWalkableForPathing(world, npcId, x + 1, y, targetX, targetY, usePhysical: true);
                bool leftBlocked = !IsWalkableForPathing(world, npcId, x - 1, y, targetX, targetY, usePhysical: true);
                if (rightBlocked && IsWalkableForPathing(world, npcId, x + 1, y + dirY, targetX, targetY, usePhysical: true)) return true;
                if (leftBlocked && IsWalkableForPathing(world, npcId, x - 1, y + dirY, targetX, targetY, usePhysical: true)) return true;
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
        // =====================================================================
        // PREDICATO DI CAMMINABILITÀ — Patch 0.02.06.A (modifica percettiva)
        // =====================================================================

        /// <summary>
        /// Determina se una cella è camminabile per il pathfinder locale dell'NPC.
        ///
        /// <para><b>Patch 0.02.06.A — approccio percettivo con LOS:</b></para>
        /// <para>
        /// Il problema del direct path nel manifesto Arcontio è che usava
        /// <c>world.IsMovementBlocked(x,y)</c>: conoscenza onnisciente dei muri.
        /// Un NPC non dovrebbe sapere dove sono i muri che non ha mai visto.
        /// </para>
        ///
        /// <para>
        /// Gli occluder (muri, porte chiuse) hanno <c>IsInteractable=false</c>
        /// nel loro ObjectDef: NON vengono mai memorizzati in <c>NpcObjectMemoryStore</c>
        /// (che accetta solo oggetti interagibili). Non è quindi possibile fare un check
        /// percettivo basato sulla memoria degli oggetti per i muri.
        /// </para>
        ///
        /// <para><b>Soluzione — LOS come proxy percettivo:</b></para>
        /// <para>
        /// Un NPC può "sapere" che una cella è libera se ha line-of-sight verso di essa
        /// dalla sua posizione corrente. La LOS usa la stessa <c>OcclusionMap</c> dei
        /// System di percezione: è coerente con ciò che l'NPC effettivamente "vede".
        /// </para>
        ///
        /// <para>
        /// Se l'NPC non ha LOS verso una cella, non la usa nel path planning locale.
        /// Questo è il comportamento corretto: un NPC non deve prendere decisioni di
        /// navigazione basandosi su celle che non vede.
        /// </para>
        ///
        /// <para><b>Casi speciali:</b></para>
        /// <list type="bullet">
        ///   <item>
        ///     La cella target finale è sempre accettata anche senza LOS diretta:
        ///     l'NPC ha già deciso di andare lì (da un intent attivo), quindi
        ///     la destinazione finale è per definizione "nota" all'NPC.
        ///   </item>
        ///   <item>
        ///     Collisione NPC-NPC: standard Arcontio 1 NPC per cella.
        ///     Eccezione per la cella target (un NPC può "mirare" a una cella occupata
        ///     per interagire con l'oggetto/NPC che ci sta sopra).
        ///   </item>
        /// </list>
        ///
        /// <para><b>Effetto comportamentale atteso:</b></para>
        /// <para>
        /// Un NPC che non ha ancora esplorato un'area non pianifica path attraverso
        /// quella zona: si fermerà o userà la macro-route landmark verso nodi noti.
        /// Questo forza l'esplorazione organica della mappa invece di un pathfinding
        /// onnisciente.
        /// </para>
        ///
        /// <para><b>Nota sul fallback:</b></para>
        /// <para>
        /// Se <c>_occlusion</c> non è ancora inizializzata (mappa non caricata),
        /// <c>HasLineOfSight</c> ritorna <c>true</c> come fallback sicuro: in quel
        /// caso il check fisico <c>IsMovementBlocked</c> funge da unica guardia.
        /// </para>
        /// </summary>
        /// <param name="world">Il mondo simulato.</param>
        /// <param name="npcId">ID dell'NPC (per la LOS e per la collisione NPC-NPC).</param>
        /// <param name="x">Colonna della cella da verificare.</param>
        /// <param name="y">Riga della cella da verificare.</param>
        /// <param name="targetX">X della destinazione finale (sempre accettata).</param>
        /// <param name="targetY">Y della destinazione finale.</param>
        // =====================================================================
        // PREDICATO DI CAMMINABILITÀ — due modalità (Patch 0.02.07.A)
        // =====================================================================
        // usePhysical=false (default): check percettivo via LOS.
        //   Usato da TryBuildGreedyDirectPath: l'NPC pianifica solo attraverso
        //   celle con LOS dalla sua posizione corrente (non vede oltre angoli).
        //
        // usePhysical=true: check fisico via IsMovementBlocked.
        //   Usato da TryBuildBoundedMovePath (BFS/JPS): la ricerca deve espandere
        //   celle non ancora visibili dall'NPC (es. oltre un angolo a L/U).
        //   Senza questo flag la BFS fallisce sulle geometrie a L: la LOS non
        //   vede oltre l'angolo e la ricerca non trova mai il percorso.
        //   Non è onniscienza globale: la ricerca rimane bounded per budget.
        private static bool IsWalkableForPathing(World world, int npcId, int x, int y,
            int targetX, int targetY, bool usePhysical = false)
        {
            if (!world.InBounds(x, y))
                return false;

            // La cella target è sempre accettata (l'NPC sa dove vuole andare),
            // ma mai se è fisicamente impassabile.
            if (x == targetX && y == targetY)
                return !world.IsMovementBlocked(x, y);

            if (usePhysical)
            {
                // ── FISICA (BFS/JPS locale): aggira angoli e geometrie concave ──
                // Usa la cache di occlusione globale per espandere oltre gli angoli.
                if (world.IsMovementBlocked(x, y))
                {
                    // Fix v0.04.10.n: le porte chiuse non bloccate sono attraversabili
                    // dal punto di vista del planning BFS: l'NPC può aprirle.
                    // Il blocco fisico viene gestito a runtime dal rilevamento porta
                    // in MovementSystem (che emette OpenDoorCommand al momento del passo).
                    int doorObjId = world.GetObjectAt(x, y);
                    bool isUnlockedClosedDoor = doorObjId >= 0
                        && world.Objects.TryGetValue(doorObjId, out var dInst) && dInst != null
                        && world.TryGetObjectDef(dInst.DefId, out var dDef) && dDef != null
                        && dDef.IsDoor && !dInst.IsOpen && !dInst.IsLocked;
                    if (!isUnlockedClosedDoor)
                        return false;
                    // Porta chiusa apribile: considerata walkable per BFS, non return false.
                }
            }
            else
            {
                // ── PERCETTIVA (direct path builder): solo celle con LOS ────────
                // HasLineOfSight = stessa OcclusionMap di ObjectPerceptionSystem.
                // Se l'NPC non ha posizione, fallback al check fisico.
                if (world.GridPos.TryGetValue(npcId, out var npcPos))
                {
                    if (!world.HasLineOfSight(npcPos.X, npcPos.Y, x, y))
                        return false;
                }
                else
                {
                    if (world.IsMovementBlocked(x, y))
                        return false;
                }
            }

            // Standard 1 NPC per cella (cella target già gestita sopra).
            if (world.TryGetNpcAt(x, y, out int otherNpcId) && otherNpcId != npcId)
                return false;

            return true;
        }

        // ============================================================
    }
}
