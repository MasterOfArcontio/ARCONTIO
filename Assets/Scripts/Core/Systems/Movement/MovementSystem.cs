using Arcontio.Core.Diagnostics;
using Arcontio.Core.Logging;
using UnityEngine;

namespace Arcontio.Core
{
    // =============================================================================
    // MovementSystem — Patch 0.02.05.B: planning di navigazione spostato qui
    // =============================================================================
    /// <summary>
    /// <b>MovementSystem</b> — esecuzione fisica del movimento NPC tick per tick.
    ///
    /// <para>
    /// Consuma i <see cref="MoveIntent"/> presenti nel World e prova ad avanzare
    /// ogni NPC di 1 cella per tick verso la destinazione.
    /// </para>
    ///
    /// <para><b>Responsabilità di questo System:</b></para>
    /// <list type="number">
    ///   <item><b>Inizializzazione navigazione</b> (Patch 0.02.05.B, solo al primo tick
    ///         di un nuovo intent via <c>intent.IsNew</c>):
    ///         sceglie direct path o macro-route landmark, prepara i path debug.</item>
    ///   <item><b>Avanzamento macro-route</b>: quando l'NPC raggiunge un nodo
    ///         intermedio, avanza al nodo successivo.</item>
    ///   <item><b>Selezione target effettivo</b>: target finale o prossimo waypoint
    ///         landmark a seconda della strategia attiva.</item>
    ///   <item><b>Esecuzione passo fisico</b>: step greedy (asse con distanza maggiore)
    ///         con fallback sull'altro asse.</item>
    ///   <item><b>Local search</b>: se il greedy fallisce, bounded search locale
    ///         per aggirare piccoli ostacoli.</item>
    ///   <item><b>Stuck detection</b>: se l'NPC non avanza per N tick consecutivi,
    ///         cancella l'intent per sbloccare il re-plan delle Rule.</item>
    ///   <item><b>Target validation</b>: se l'oggetto target scompare o si sposta,
    ///         cancella l'intent.</item>
    ///   <item><b>Landmark learning</b>: notifica <c>World.NotifyNpcMovedForLandmarkLearning</c>
    ///         ad ogni passo riuscito.</item>
    /// </list>
    ///
    /// <para><b>Patch 0.02.05.B:</b></para>
    /// <para>
    /// Il planning di navigazione (scelta direct/macro-route, costruzione path debug)
    /// è stato spostato da <c>SetMoveIntentCommand</c> a questo System, nel metodo
    /// <see cref="InitializeNavigation"/>. Viene eseguito solo al primo tick di ogni
    /// nuovo intent (<c>intent.IsNew == true</c>).
    /// </para>
    /// </summary>
    public sealed class MovementSystem : ISystem
    {
        public int Period => 1;

        // =====================================================================
        // COSTANTI DI CONFIGURAZIONE
        // =====================================================================
        // TODO: in futuro leggere queste da game_params.json.
        // Per ora sono costanti compile-time come baseline sicura.

        private static void LogDirectGateDebug(
            World world,
            int npcId,
            string phase,
            GridPosition pos,
            int finalTargetX,
            int finalTargetY,
            bool checkFov,
            bool targetVisible,
            bool pathClear,
            int effectiveTargetX,
            int effectiveTargetY,
            bool inPrefixCommitment,
            bool isInLastMile,
            bool lastMileJustConverted,
            bool usingMacroImmediate,
            string extraKey = null,
            object extraValue = null)
        {
            int visionRange = world.Global.NpcVisionRangeCells;
            if (visionRange <= 0) visionRange = 6;

            int dist = FovUtils.Manhattan(pos.X, pos.Y, finalTargetX, finalTargetY);
            bool overVision = checkFov && dist > visionRange;
            bool suspiciousDirect = pathClear && overVision;

            if (!world.NpcFacing.TryGetValue(npcId, out var facing))
                facing = CardinalDirection.North;

            var block = new LogBlock(suspiciousDirect ? LogLevel.Warn : LogLevel.Trace, "log.move.direct_commit_debug")
                .AddField("phase", phase)
                .AddField("npcX", pos.X)
                .AddField("npcY", pos.Y)
                .AddField("facing", facing)
                .AddField("finalTargetX", finalTargetX)
                .AddField("finalTargetY", finalTargetY)
                .AddField("effectiveTargetX", effectiveTargetX)
                .AddField("effectiveTargetY", effectiveTargetY)
                .AddField("manhattanToFinal", dist)
                .AddField("visionRange", visionRange)
                .AddField("checkFov", checkFov)
                .AddField("targetVisible", targetVisible)
                .AddField("pathClear", pathClear)
                .AddField("overVision", overVision)
                .AddField("usingMacroImmediate", usingMacroImmediate)
                .AddField("inPrefixCommitment", inPrefixCommitment)
                .AddField("isInLastMile", isInLastMile)
                .AddField("lastMileJustConverted", lastMileJustConverted);

            if (world.Pathfinding.MacroRouteExecution.TryGetValue(npcId, out var macroState) && macroState != null)
            {
                block.AddField("macroActive", macroState.Active)
                    .AddField("macroHasUsablePath", macroState.HasUsableMacroPath)
                    .AddField("macroNavMode", macroState.NavigationMode)
                    .AddField("macroReason", macroState.LastModeSwitchReason)
                    .AddField("macroNextRouteNodeIndex", macroState.NextRouteNodeIndex)
                    .AddField("macroImmediateX", macroState.ImmediateTargetX)
                    .AddField("macroImmediateY", macroState.ImmediateTargetY)
                    .AddField("macroPrefixRemaining", macroState.DirectPrefixStepsRemaining)
                    .AddField("macroApproachingFirstLm", macroState.IsApproachingFirstLm)
                    .AddField("macroLastMile", macroState.IsDoingLastMile);
            }
            else
            {
                block.AddField("macroActive", false);
            }

            if (!string.IsNullOrEmpty(extraKey))
                block.AddField(extraKey, extraValue);

            var context = new LogContext(tick: (int)TickContext.CurrentTickIndex, channel: "Move", npcId: npcId, cell: (pos.X, pos.Y));
            if (suspiciousDirect)
                ArcontioLogger.Warn(context, block);
            else
                ArcontioLogger.Trace(context, block);
        }

        private static void LogDebugSegmentStep(
            World world,
            int npcId,
            string segmentKind,
            int fromX,
            int fromY,
            int toX,
            int toY,
            bool usingMacroImmediate,
            bool isApproaching,
            bool inPrefixCommitment,
            bool isInLastMile)
        {
            var block = new LogBlock(LogLevel.Trace, "log.move.debug_segment_step")
                .AddField("segmentKind", segmentKind)
                .AddField("fromX", fromX)
                .AddField("fromY", fromY)
                .AddField("toX", toX)
                .AddField("toY", toY)
                .AddField("usingMacroImmediate", usingMacroImmediate)
                .AddField("isApproachingFirstLm", isApproaching)
                .AddField("inPrefixCommitment", inPrefixCommitment)
                .AddField("isInLastMile", isInLastMile);

            if (world.Pathfinding.MacroRouteExecution.TryGetValue(npcId, out var macroState) && macroState != null)
            {
                block.AddField("macroNavMode", macroState.NavigationMode)
                    .AddField("macroReason", macroState.LastModeSwitchReason)
                    .AddField("macroPrefixRemaining", macroState.DirectPrefixStepsRemaining)
                    .AddField("macroImmediateX", macroState.ImmediateTargetX)
                    .AddField("macroImmediateY", macroState.ImmediateTargetY);
            }

            ArcontioLogger.Trace(
                new LogContext(tick: (int)TickContext.CurrentTickIndex, channel: "Move", npcId: npcId, cell: (toX, toY)),
                block);
        }

        private static void LogLocalSearchAttempt(
            World world,
            int npcId,
            int startX,
            int startY,
            int finalTargetX,
            int finalTargetY,
            int effectiveTargetX,
            int effectiveTargetY,
            int budget,
            bool usingMacroImmediate,
            bool inPrefixCommitment,
            bool isInLastMile)
        {
            var block = new LogBlock(LogLevel.Trace, "log.move.local_search_attempt")
                .AddField("startX", startX)
                .AddField("startY", startY)
                .AddField("finalTargetX", finalTargetX)
                .AddField("finalTargetY", finalTargetY)
                .AddField("effectiveTargetX", effectiveTargetX)
                .AddField("effectiveTargetY", effectiveTargetY)
                .AddField("budget", budget)
                .AddField("usingMacroImmediate", usingMacroImmediate)
                .AddField("inPrefixCommitment", inPrefixCommitment)
                .AddField("isInLastMile", isInLastMile);

            if (world.Pathfinding.MacroRouteExecution.TryGetValue(npcId, out var macroState) && macroState != null)
            {
                block.AddField("macroActive", macroState.Active)
                    .AddField("macroNavMode", macroState.NavigationMode)
                    .AddField("macroFailureReason", macroState.FailureReason)
                    .AddField("macroImmediateX", macroState.ImmediateTargetX)
                    .AddField("macroImmediateY", macroState.ImmediateTargetY)
                    .AddField("macroNextRouteNodeIndex", macroState.NextRouteNodeIndex);
            }
            else
            {
                block.AddField("macroActive", false);
            }

            ArcontioLogger.Trace(
                new LogContext(tick: (int)TickContext.CurrentTickIndex, channel: "Move", npcId: npcId, cell: (startX, startY)),
                block);
        }

        private static void LogLocalSearchUseResult(
            World world,
            int npcId,
            int startX,
            int startY,
            int targetX,
            int targetY,
            bool found,
            System.Collections.Generic.List<Vector2Int> path)
        {
            int pathCount = path?.Count ?? 0;
            int firstStepX = pathCount >= 2 ? path[1].x : 0;
            int firstStepY = pathCount >= 2 ? path[1].y : 0;
            int lastX = pathCount > 0 ? path[pathCount - 1].x : 0;
            int lastY = pathCount > 0 ? path[pathCount - 1].y : 0;
            bool endsAtTarget = pathCount > 0 && lastX == targetX && lastY == targetY;
            string resultKind = !found ? "Failed" : (endsAtTarget ? "Complete" : "Partial");

            ArcontioLogger.Trace(
                new LogContext(tick: (int)TickContext.CurrentTickIndex, channel: "Move", npcId: npcId, cell: (startX, startY)),
                new LogBlock(LogLevel.Trace, "log.move.local_search_use_result")
                    .AddField("source", nameof(MovementSystem))
                    .AddField("resultKind", resultKind)
                    .AddField("startX", startX)
                    .AddField("startY", startY)
                    .AddField("targetX", targetX)
                    .AddField("targetY", targetY)
                    .AddField("found", found)
                    .AddField("pathCount", pathCount)
                    .AddField("firstStepX", firstStepX)
                    .AddField("firstStepY", firstStepY)
                    .AddField("lastX", lastX)
                    .AddField("lastY", lastY)
                    .AddField("endsAtTarget", endsAtTarget));
        }

        private static bool CanReachMacroImmediateTargetLocally(
            World world,
            int npcId,
            int startX,
            int startY,
            int targetX,
            int targetY)
        {
            if (MovementPathfinder.CanNpcUseDirectPath(world, npcId, targetX, targetY))
                return true;

            var path = new System.Collections.Generic.List<Vector2Int>(64);
            bool found = MovementPathfinder.TryBuildBoundedMovePath(
                world, npcId, startX, startY, targetX, targetY, GetLocalSearchVisitedBudget(world), path);

            if (!found || path.Count < 2)
                return false;

            var last = path[path.Count - 1];
            return last.x == targetX && last.y == targetY;
        }

        private static bool TryStartDirectCommit(
            World world,
            int npcId,
            GridPosition pos,
            int targetX,
            int targetY,
            string reason,
            bool keepLastMile,
            out int directPathCount)
        {
            directPathCount = 0;

            var directPath = new System.Collections.Generic.List<Vector2Int>(32);
            if (!MovementPathfinder.TryBuildGreedyDirectPath(world, npcId, pos.X, pos.Y, targetX, targetY, directPath)
                || directPath.Count < 2)
                return false;

            if (!keepLastMile)
                world.ClearDebugMacroRouteForNpc(npcId);

            world.ClearNpcLocalSearchState(npcId, "DirectCommitStarted");
            world.Pathfinding.ClearMoveBackOff(npcId);
            world.SetDebugDirectPathForNpc(npcId, directPath);

            int prefixLen = Mathf.Min(GetDirectPrefixCells(world), directPath.Count - 1);
            var directState = new NpcMacroRouteExecutionState
            {
                Active                     = true,
                HasUsableMacroPath         = keepLastMile,
                IsDoingLastMile            = keepLastMile,
                NextRouteNodeIndex         = 0,
                FinalTargetCellX           = targetX,
                FinalTargetCellY           = targetY,
                ImmediateTargetX           = targetX,
                ImmediateTargetY           = targetY,
                FailureReason              = string.Empty,
                NavigationMode             = "DIRECT_APPROACHING",
                LastModeSwitchTick         = (int)TickContext.CurrentTickIndex,
                LastModeSwitchReason       = reason,
                DirectPrefixStepsRemaining = Mathf.Max(1, prefixLen),
                IsApproachingFirstLm       = false,
            };
            world.Pathfinding.MacroRouteExecution[npcId] = directState;
            directPathCount = directPath.Count;
            return true;
        }

        private static bool TrySelectDirectCommitNextStep(
            World world,
            int npcId,
            int currentX,
            int currentY,
            out int targetX,
            out int targetY)
        {
            targetX = currentX;
            targetY = currentY;

            if (!world.Pathfinding.DirectCommitExecution.TryGetValue(npcId, out var directState)
                || directState == null
                || !directState.Active
                || directState.CurrentPath.Count < 2)
                return false;

            if (directState.NextPathIndex < 1)
                directState.NextPathIndex = 1;

            if (directState.NextPathIndex < directState.CurrentPath.Count)
            {
                var plannedNext = directState.CurrentPath[directState.NextPathIndex];
                int plannedDist = Mathf.Abs(plannedNext.X - currentX) + Mathf.Abs(plannedNext.Y - currentY);
                if (plannedDist > 1)
                {
                    for (int i = 0; i < directState.CurrentPath.Count; i++)
                    {
                        var cell = directState.CurrentPath[i];
                        if (cell.X == currentX && cell.Y == currentY)
                        {
                            directState.NextPathIndex = i + 1;
                            break;
                        }
                    }
                }
            }

            if (directState.NextPathIndex >= directState.CurrentPath.Count)
            {
                directState.Active = false;
                world.Pathfinding.DirectCommitExecution[npcId] = directState;
                return false;
            }

            var next = directState.CurrentPath[directState.NextPathIndex];
            int dist = Mathf.Abs(next.X - currentX) + Mathf.Abs(next.Y - currentY);
            if (dist != 1)
            {
                directState.Active = false;
                directState.FailureReason = "DirectCommitPathDesynced";
                world.Pathfinding.DirectCommitExecution[npcId] = directState;
                return false;
            }

            directState.ImmediateTargetX = next.X;
            directState.ImmediateTargetY = next.Y;
            world.Pathfinding.DirectCommitExecution[npcId] = directState;
            targetX = next.X;
            targetY = next.Y;
            return true;
        }

        private static void AdvanceDirectCommitAfterMove(World world, int npcId, int movedToX, int movedToY)
        {
            if (!world.Pathfinding.DirectCommitExecution.TryGetValue(npcId, out var directState)
                || directState == null
                || directState.CurrentPath.Count == 0)
                return;

            if (directState.NextPathIndex < directState.CurrentPath.Count)
            {
                var expected = directState.CurrentPath[directState.NextPathIndex];
                if (expected.X == movedToX && expected.Y == movedToY)
                    directState.NextPathIndex++;
            }

            if (directState.NextPathIndex >= directState.CurrentPath.Count)
            {
                directState.Active = false;
                directState.ImmediateTargetX = movedToX;
                directState.ImmediateTargetY = movedToY;
            }
            else
            {
                var next = directState.CurrentPath[directState.NextPathIndex];
                directState.Active = true;
                directState.ImmediateTargetX = next.X;
                directState.ImmediateTargetY = next.Y;
            }

            world.Pathfinding.DirectCommitExecution[npcId] = directState;
        }

        private static bool IsImmediateLocalSearchBacktrack(World world, int npcId, int fromX, int fromY, int toX, int toY)
        {
            return world.Pathfinding.GoalLocalSearchExecution.TryGetValue(npcId, out var state)
                && state != null
                && state.HasLastSuccessfulStep
                && fromX == state.LastStepToX
                && fromY == state.LastStepToY
                && toX == state.LastStepFromX
                && toY == state.LastStepFromY;
        }

        /// <summary>
        /// Numero di tick consecutivi di blocco dopo i quali l'intent viene cancellato.
        /// Serve a sbloccare la simulazione quando un NPC è impossibilitato ad avanzare.
        /// </summary>
        private const int DefaultIntentStuckTicks = 12;

        /// <summary>
        /// Ogni quanti tick si rivaluta la validità dell'oggetto target.
        /// 1 = ogni tick (più robusto, costo trascurabile con pochi NPC).
        /// </summary>
        private const int DefaultTargetValidateEveryTicks = 1;

        /// <summary>
        /// Budget di fallback per la bounded search locale.
        /// Usato solo se il config non è disponibile (difesa estrema).
        /// </summary>
        private const int DefaultBoundedSearchVisited = 256;

        private static Arcontio.Core.Config.LandmarkLocalSearchParams GetLocalSearchConfig(World world)
        {
            if (world?.Config?.Sim?.landmarks?.localSearch != null)
                return world.Config.Sim.landmarks.localSearch;
            return new Arcontio.Core.Config.LandmarkLocalSearchParams();
        }

        private static int GetLocalSearchVisitedBudget(World world)
        {
            var cfg = GetLocalSearchConfig(world);
            return Mathf.Max(1, cfg.maxExpandedNodes);
        }

        // ── MOVEMENT CONFIG HELPERS (Patch 0.02.07.A) ───────────────────────

        /// <summary>
        /// Legge i parametri di movimento da game_params.json (sezione "movement").
        /// Fallback sicuro se la config non è disponibile.
        /// </summary>
        private static Arcontio.Core.Config.MovementParams GetMovementConfig(World world)
        {
            if (world?.Config?.Sim?.movement != null)
                return world.Config.Sim.movement;
            return new Arcontio.Core.Config.MovementParams();
        }

        /// <summary>
        /// Lunghezza del prefix committed per il direct path.
        /// Da game_params.json → movement.directPrefixCells.
        /// </summary>
        private static int GetDirectPrefixCells(World world)
        {
            return Mathf.Max(1, GetMovementConfig(world).directPrefixCells);
        }

        /// <summary>
        /// True se l'acquisizione del direct richiede il check FOV sul target.
        /// Da game_params.json → movement.directCheckFovOnAcquisition.
        /// </summary>
        private static bool GetDirectCheckFov(World world)
        {
            return GetMovementConfig(world).directCheckFovOnAcquisition;
        }

        public void Update(World world, Tick tick, MessageBus bus, Telemetry telemetry)
        {
            // Carico i parametri di movimento che ci dicono quante celle della mappa posso utilizzare prima di andare in timeout
            var DefaultIntentStuckTicks = world.Config.Sim.movement?.intentStuckTicksDefault ?? 12;

            // Nota: iteriamo su NpcDna.Keys (source of truth degli NPC esistenti)
            foreach (var npcId in world.NpcDna.Keys)
            {
                if (!world.NpcMoveIntents.TryGetValue(npcId, out var intent) || !intent.Active)
                    continue;
                if (!world.GridPos.TryGetValue(npcId, out var pos))
                    continue;

                // ============================================================
                // PATCH 0.02.05.B: Inizializzazione navigazione (solo primo tick)
                // ============================================================
                // Se l'intent è nuovo (appena scritto da SetMoveIntentCommand),
                // questo è il momento in cui scegliamo la strategia di navigazione:
                // - direct path: il target è raggiungibile con il greedy locale?
                // - macro-route landmark: serve passare per nodi intermedi?
                // Dopo l'inizializzazione, IsNew viene azzerato per non ripetere.
                if (intent.IsNew)
                {
                    InitializeNavigation(world, npcId, ref intent);
                    world.NpcMoveIntents[npcId] = intent;
                }

                // ── FAILURE LADDER: CHECK BACK-OFF (v0.03.05-FailureLadder) ─────────
                {
                    long nowTick = TickContext.CurrentTickIndex;

                    // Se il back-off è ancora attivo: NPC in pausa, salta il movimento.
                    if (world.Pathfinding.IsMoveBackOffActive(npcId, nowTick))
                        continue;

                    // Se il back-off è appena scaduto: tenta replan (re-inizializza navigazione).
                    if (world.Pathfinding.TryExpireMoveBackOff(npcId, nowTick, out int expiredStage))
                    {
                        intent.IsNew        = true;
                        intent.BlockedTicks = 0;
                        world.NpcMoveIntents[npcId] = intent;
                        InitializeNavigation(world, npcId, ref intent);
                        world.NpcMoveIntents[npcId] = intent;

                        ArcontioLogger.Trace(
                            new LogContext(tick: (int)nowTick, channel: "Move", npcId: npcId, cell: (pos.X, pos.Y)),
                            new LogBlock(LogLevel.Trace, "log.move.backoff_replan")
                                .AddField("targetX", intent.TargetX)
                                .AddField("targetY", intent.TargetY)
                                .AddField("expiredStage", expiredStage)
                        );
                    }
                }

                // Avanza lo stato della macro-route se l'NPC è su un nodo landmark.
                world.TryAdvanceMacroRouteExecutionAtCell(npcId, pos.X, pos.Y);

                // ── LAST-MILE → DIRECT PERCETTIVO (Patch 0.02.07.A) ──────────
                // Quando si entra in last-mile (raggiunto l'ultimo nodo LM),
                // il documento "Commitment Percettivo" richiede che il tratto
                // finale verso la destinazione sia trattato come una nuova
                // acquisizione direct (Regola 1 + prefix commitment).
                //
                // Se il target finale è percettivamente visibile dal nodo LM
                // appena raggiunto → si avvia un prefix commitment direct:
                //   - NavMode → DIRECT_APPROACHING
                //   - DirectPrefixStepsRemaining → directPrefixCells
                //   - il tratto finale usa il greedy diretto con rinnovo FOV
                //
                // Se non è visibile → si resta in LAST_MILE greedy (fallback):
                //   il greedy+local search si occupa di trovare il target.
                // Patch 0.02.07.B: flag per comunicare al blocco prefix sotto
                // che la conversione last-mile→direct è avvenuta in questo tick.
                bool lastMileJustConverted = false;

                if (world.Pathfinding.MacroRouteExecution.TryGetValue(npcId, out var lmState)
                    && lmState != null
                    && lmState.IsDoingLastMile
                    && lmState.NavigationMode == "LAST_MILE"
                    && lmState.DirectPrefixStepsRemaining == 0)
                {
                    int lmTargetX = lmState.FinalTargetCellX;
                    int lmTargetY = lmState.FinalTargetCellY;

                    bool checkFovLm  = GetDirectCheckFov(world);
                    bool lmVisible   = CanAcquireDirectTarget(world, npcId, lmTargetX, lmTargetY, checkFovLm);
                    bool lmPathClear = lmVisible
                                       && MovementPathfinder.CanNpcUseDirectPath(world, npcId, lmTargetX, lmTargetY);

                    if (lmPathClear)
                    {
                        // Target finale visibile e path libero:
                        // converti il last-mile in un direct con prefix commitment.
                        if (TryStartDirectCommit(
                                world, npcId, pos, lmTargetX, lmTargetY,
                                "LastMileConvertedToDirect", keepLastMile: true,
                                out int lmDirectPathCount))
                        {
                            lastMileJustConverted = true;

                            LogDirectGateDebug(
                                world, npcId, "runtime_last_mile_direct_commit", pos, lmTargetX, lmTargetY,
                                checkFovLm, lmVisible, lmPathClear,
                                lmTargetX, lmTargetY, inPrefixCommitment: true, isInLastMile: true,
                                lastMileJustConverted: true, usingMacroImmediate: false,
                                extraKey: "directPathCount", extraValue: lmDirectPathCount);
                        }
                        else
                        {
                            int lmPrefixLen = Mathf.Min(GetDirectPrefixCells(world),
                            Mathf.Abs(lmTargetX - pos.X) + Mathf.Abs(lmTargetY - pos.Y));
                            lmState.NavigationMode              = "DIRECT_APPROACHING";
                            lmState.LastModeSwitchTick          = (int)TickContext.CurrentTickIndex;
                            lmState.LastModeSwitchReason        = "LastMileConvertedToDirectFallback";
                            lmState.DirectPrefixStepsRemaining  = Mathf.Max(1, lmPrefixLen);
                        // IsDoingLastMile rimane true per mantenere la semantica
                        // "siamo nel tratto finale" — ma ora il controllo lo gestisce
                        // il prefix commitment invece del greedy LM.
                            world.Pathfinding.MacroRouteExecution[npcId] = lmState;
                        // Patch 0.02.07.B — Bug 2 fix:
                        // Segnala che in questo stesso tick la conversione è avvenuta.
                        // Il blocco allowPrefix sotto deve saperlo per attivare
                        // inPrefixCommitment=true senza dover attendere il tick successivo.
                            lastMileJustConverted = true;
                        }
                    }
                    // else: LAST_MILE greedy — il sistema procede normalmente.
                }

                // ============================================================
                // GERARCHIA DI NAVIGAZIONE (runtime, ogni tick)
                // ============================================================
                // 1) Direct path con prefix commitment (priorità assoluta):
                //    a) Se abbiamo ancora passi del prefix committed → esecuzione
                //       inerziale: non ricontrolliamo la visibilità completa,
                //       solo la traversabilità della prossima cella (Regola 3 doc).
                //    b) Se il prefix è terminato → rivalutiamo l'acquisizione
                //       (Regola 4 doc): se il target è ancora visibile rinnovo,
                //       altrimenti passiamo alla macro-route.
                // 2) Macro-route landmark: se il direct non è disponibile.
                // 3) Local search bounded: fallback per ostacoli locali.
                int finalTargetX = intent.TargetX;
                int finalTargetY = intent.TargetY;
                int effectiveTargetX = finalTargetX;
                int effectiveTargetY = finalTargetY;
                bool macroLastMile = false;
                int macroNextNodeId = 0;
                bool usingMacroImmediate = false;
                bool directClearRuntimeForDebug = false;
                bool usingGreedyFallbackForDebug = false;

                // ── PREFIX COMMITMENT RUNTIME (Patch 0.02.07.A) ──────────────
                // Gestisce le regole 3 e 4 del documento "Commitment Percettivo".
                // IMPORTANTE: il prefix NON si applica durante il last-mile.
                // Il last-mile viene sempre gestito dalla macro-route (usingMacroImmediate).
                bool inPrefixCommitment = false;
                bool isInLastMile = world.Pathfinding.MacroRouteExecution
                    .TryGetValue(npcId, out var execStateCheck)
                    && execStateCheck != null && execStateCheck.IsDoingLastMile;

                // Il prefix commitment si applica quando:
                //   a) Non siamo in last-mile (percorso normal direct), oppure
                //   b) Siamo in last-mile MA con NavMode DIRECT_APPROACHING
                //      (last-mile convertito a direct percettivo).
                // allowPrefix=true se:
                //   a) Non siamo in last-mile, oppure
                //   b) Siamo in last-mile con NavMode già DIRECT_APPROACHING (tick precedenti), oppure
                //   c) La conversione last-mile→direct è avvenuta in QUESTO tick (lastMileJustConverted).
                bool allowPrefix = !isInLastMile
                    || lastMileJustConverted
                    || (isInLastMile
                        && world.Pathfinding.MacroRouteExecution.TryGetValue(npcId, out var lmPrefCheck)
                        && lmPrefCheck != null
                        && lmPrefCheck.NavigationMode == "DIRECT_APPROACHING");

                if (allowPrefix)
                {
                    if (world.Pathfinding.MacroRouteExecution.TryGetValue(npcId, out var execState)
                        && execState != null && execState.DirectPrefixStepsRemaining > 0)
                    {
                        // Regola 3: siamo nel prefix → esecuzione inerziale verso finalTarget.
                        inPrefixCommitment = true;
                    }
                    else if (world.Pathfinding.MacroRouteExecution.TryGetValue(npcId, out var execState2)
                             && execState2 != null && execState2.DirectPrefixStepsRemaining == 0
                             && execState2.NavigationMode == "DIRECT_APPROACHING")
                    {
                        // Prefix terminato: Regola 4 → rivaluta acquisizione.
                        bool checkFovRenew  = GetDirectCheckFov(world);
                        bool canRenew       = CanAcquireDirectTarget(world, npcId, finalTargetX, finalTargetY, checkFovRenew);
                        bool pathStillClear = canRenew
                                             && MovementPathfinder.CanNpcUseDirectPath(world, npcId, finalTargetX, finalTargetY);

                        LogDirectGateDebug(
                            world, npcId, "runtime_direct_prefix_renew_gate", pos, finalTargetX, finalTargetY,
                            checkFovRenew, canRenew, pathStillClear,
                            effectiveTargetX, effectiveTargetY, inPrefixCommitment, isInLastMile,
                            lastMileJustConverted, usingMacroImmediate);

                        if (pathStillClear)
                        {
                            var renewDirectPath = new System.Collections.Generic.List<Vector2Int>(32);
                            if (MovementPathfinder.TryBuildGreedyDirectPath(
                                    world, npcId, pos.X, pos.Y, finalTargetX, finalTargetY, renewDirectPath)
                                && renewDirectPath.Count >= 2)
                            {
                                world.SetDebugDirectPathForNpc(npcId, renewDirectPath);
                                execState2.DirectPrefixStepsRemaining = Mathf.Min(GetDirectPrefixCells(world), renewDirectPath.Count - 1);
                            }
                            else
                            {
                                execState2.DirectPrefixStepsRemaining = GetDirectPrefixCells(world);
                            }

                            execState2.NavigationMode = "DIRECT_APPROACHING";
                            world.Pathfinding.MacroRouteExecution[npcId] = execState2;
                            inPrefixCommitment = true;
                        }
                        // else: esci dal direct, torna al greedy LM last-mile.
                    }
                }

                // Se non siamo in prefix commitment, selezioniamo il target normalmente.
                if (!inPrefixCommitment)
                {
                    // Fix v0.04.10.m: applica lo stesso gate FOV usato in InitializeNavigation.
                    // In precedenza questo controllo usava solo CanNpcUseDirectPath (traversabilità
                    // greedy), senza il gate percettivo Range+FOV+LOS su finalTarget.
                    // Dopo l'apertura delle porte il corridoio risulta greedy-clear per l'intera
                    // distanza, quindi il check greedy passava anche per target a 26 celle
                    // (oltre visionRange=17), bypassando la macro-route landmark.
                    bool checkFovRuntime      = GetDirectCheckFov(world);
                    bool targetVisibleRuntime = CanAcquireDirectTarget(world, npcId, finalTargetX, finalTargetY, checkFovRuntime);
                    bool directClearRuntime   = targetVisibleRuntime
                                               && MovementPathfinder.CanNpcUseDirectPath(world, npcId, finalTargetX, finalTargetY);
                    directClearRuntimeForDebug = directClearRuntime;

                    LogDirectGateDebug(
                        world, npcId, "runtime_direct_gate", pos, finalTargetX, finalTargetY,
                        checkFovRuntime, targetVisibleRuntime, directClearRuntime,
                        effectiveTargetX, effectiveTargetY, inPrefixCommitment, isInLastMile,
                        lastMileJustConverted, usingMacroImmediate);

                    if (!directClearRuntime)
                    {
                        if (world.TryGetMacroExecutionImmediateTarget(npcId, out int macroTargetX, out int macroTargetY, out macroLastMile, out macroNextNodeId))
                        {
                            bool macroImmediateReachable = CanReachMacroImmediateTargetLocally(
                                world, npcId, pos.X, pos.Y, macroTargetX, macroTargetY);

                            if (macroImmediateReachable)
                            {
                                effectiveTargetX = macroTargetX;
                                effectiveTargetY = macroTargetY;
                                usingMacroImmediate = true;

                                LogDirectGateDebug(
                                    world, npcId, "runtime_macro_target_selected", pos, finalTargetX, finalTargetY,
                                    checkFovRuntime, targetVisibleRuntime, directClearRuntime,
                                    effectiveTargetX, effectiveTargetY, inPrefixCommitment, isInLastMile,
                                    lastMileJustConverted, usingMacroImmediate,
                                    extraKey: "macroNextNodeId", extraValue: macroNextNodeId);
                            }
                            else
                            {
                                if (macroLastMile)
                                {
                                    world.MarkMacroRouteExecutionBlocked(npcId, duringLastMile: true);
                                    usingGreedyFallbackForDebug = true;
                                }
                                else
                                {
                                    effectiveTargetX = macroTargetX;
                                    effectiveTargetY = macroTargetY;
                                    usingMacroImmediate = true;
                                }

                                LogDirectGateDebug(
                                    world, npcId, "runtime_macro_target_unreachable", pos, finalTargetX, finalTargetY,
                                    checkFovRuntime, targetVisibleRuntime, directClearRuntime,
                                    macroTargetX, macroTargetY, inPrefixCommitment, isInLastMile,
                                    lastMileJustConverted, usingMacroImmediate,
                                    extraKey: "macroNextNodeId", extraValue: macroNextNodeId);
                            }
                        }
                        else
                        {
                            usingGreedyFallbackForDebug = true;
                        }
                    }
                    else if (!isInLastMile)
                    {
                        // Fix bug 3: il target è raggiungibile direttamente ma il NavMode
                        // potrebbe essere rimasto APPROACHING_LM da un piano LM precedente.
                        // Forziamo DIRECT_APPROACHING per allineare card e comportamento reale.
                        if (TryStartDirectCommit(
                                world, npcId, pos, finalTargetX, finalTargetY,
                                "RuntimeDirectOverrideLm", keepLastMile: false,
                                out int runtimeDirectPathCount))
                        {
                            inPrefixCommitment = true;
                            effectiveTargetX = finalTargetX;
                            effectiveTargetY = finalTargetY;
                            usingMacroImmediate = false;

                            LogDirectGateDebug(
                                world, npcId, "runtime_direct_commit_override_lm", pos, finalTargetX, finalTargetY,
                                checkFovRuntime, targetVisibleRuntime, directClearRuntime,
                                effectiveTargetX, effectiveTargetY, inPrefixCommitment, isInLastMile,
                                lastMileJustConverted, usingMacroImmediate,
                                extraKey: "directPathCount", extraValue: runtimeDirectPathCount);
                        }
                        else if (world.Pathfinding.MacroRouteExecution.TryGetValue(npcId, out var fixState)
                            && fixState != null
                            && fixState.NavigationMode == "APPROACHING_LM")
                        {
                            fixState.NavigationMode       = "DIRECT_APPROACHING";
                            fixState.LastModeSwitchTick   = (int)TickContext.CurrentTickIndex;
                            fixState.LastModeSwitchReason = "DirectOverrideLm";
                            world.Pathfinding.MacroRouteExecution[npcId] = fixState;

                            LogDirectGateDebug(
                                world, npcId, "runtime_direct_override_lm", pos, finalTargetX, finalTargetY,
                                checkFovRuntime, targetVisibleRuntime, directClearRuntime,
                                effectiveTargetX, effectiveTargetY, inPrefixCommitment, isInLastMile,
                                lastMileJustConverted, usingMacroImmediate);
                        }
                    }
                }

                // PATCH NOTE:
                // Validazione "oggetto target" (se presente).
                // Se target sparisce / è vuoto / si sposta, cancelliamo l'intento.
                if (ShouldValidateTargetThisTick(tick))
                {
                    if (!ValidateOrCancelTarget(world, npcId, ref intent))
                    {
                        // intent già cancellato dentro ValidateOrCancelTarget.
                        continue;
                    }
                }

                // ACTION TRACE (debug/overlay): durante un MoveIntent attivo, l'NPC è in azione MoveTo.
                // IMPORTANTISSIMO: non vogliamo resettare StartedTick ogni tick; quindi settiamo solo se serve.
                if (world.TryGetNpcAction(npcId, out var act))
                {
                    if (act.Kind != NpcActionKind.MoveTo || !act.HasTargetCell || act.TargetX != effectiveTargetX || act.TargetY != effectiveTargetY)
                        world.SetNpcAction(npcId, NpcActionState.MoveTo(effectiveTargetX, effectiveTargetY, intent.Reason.ToString()));
                }
                else
                {
                    world.SetNpcAction(npcId, NpcActionState.MoveTo(effectiveTargetX, effectiveTargetY, intent.Reason.ToString()));
                }

                int x = pos.X;
                int y = pos.Y;

                // Se siamo arrivati, disattiviamo.
                if (x == effectiveTargetX && y == effectiveTargetY)
                {
                    intent.Active = false;
                    intent.BlockedTicks = 0;
                    world.NpcMoveIntents[npcId] = intent;

                    // Fix 0.02.07.A bug 4: pulisci tutti i debug path al completamento.
                    // Prima li mantenevamo "per mostrare l'ultimo percorso", ma questo
                    // causava linee residue persistenti dopo l'arrivo.
                    world.ClearDebugNavigationPathsForNpc(npcId);
                    world.ClearNpcLocalSearchState(npcId, string.Empty);
                    world.ClearNpcDirectCommitState(npcId, string.Empty);
                    world.Pathfinding.ClearMoveBackOff(npcId); // failure ladder reset

                    // Fix 0.02.07.A bug 3: azzera NavigationMode e DirectPrefixStepsRemaining
                    // così il prossimo intent parte da uno stato pulito.
                    if (world.Pathfinding.MacroRouteExecution.TryGetValue(npcId, out var doneState)
                        && doneState != null)
                    {
                        doneState.NavigationMode           = string.Empty;
                        doneState.DirectPrefixStepsRemaining = 0;
                        doneState.Active                   = false;
                        world.Pathfinding.MacroRouteExecution[npcId] = doneState;
                    }

                    world.SetNpcIdle(npcId);
                    continue;
                }

                bool usingDirectCommitStep = false;
                bool directCommitCanOwnMovement =
                    world.Pathfinding.DirectCommitExecution.TryGetValue(npcId, out var preLocalDirectState)
                    && preLocalDirectState != null
                    && preLocalDirectState.Active
                    && world.Pathfinding.MacroRouteExecution.TryGetValue(npcId, out var preLocalDirectMacroState)
                    && preLocalDirectMacroState != null
                    && preLocalDirectMacroState.NavigationMode == "DIRECT_APPROACHING";

                if (directCommitCanOwnMovement)
                    world.ClearNpcLocalSearchState(npcId, "DirectCommitOwnsMovement");

                // ============================================================
                // PATCH 0.02.02P - Local search ownership forte
                // ============================================================
                // Se una local search e' attiva, e' LEI la proprietaria del movimento.
                // Non vogliamo che nello stesso tick la macro-navigation landmark si riprenda
                // il controllo, altrimenti nasce il ping-pong LM_PATH <-> GOAL_LOCAL_SEARCH.
                if (MovementPathfinder.HasActiveNpcLocalSearch(world, npcId))
                {
                    // ============================================================
                    // PATCH 0.02.02S - local search difensiva senza deadlock
                    // ============================================================
                    // In questa codebase la local search non e' ancora una state machine
                    // completamente autonoma: e' un fallback locale bounded innestato sul
                    // movimento principale.
                    //
                    // Quindi, se per qualsiasi motivo lo stato locale resta attivo ma non
                    // produce piu' un next step valido, NON dobbiamo continuare a fare
                    // "continue" infinito sul ramo local, altrimenti l'NPC resta fermo e
                    // sembra che il tick sia bloccato.
                    //
                    // Regola corretta:
                    // - se la local search ha uno step valido -> la eseguiamo e il tick e'
                    //   di proprieta' della local search;
                    // - se non ha piu' uno step e non riesce neppure a fare replan ->
                    //   rilasciamo lo stato locale e torniamo al movimento standard nello
                    //   STESSO tick.
                    bool localMoved = false;
                    int localToX = x;
                    int localToY = y;

                    bool hasLocalStep = MovementPathfinder.TryGetActiveNpcLocalSearchNextStep(world, npcId, out int localStepX, out int localStepY);
                    if (!hasLocalStep)
                    {
                        hasLocalStep = MovementPathfinder.TryReplanNpcLocalSearch(world, npcId, x, y)
                            && MovementPathfinder.TryGetActiveNpcLocalSearchNextStep(world, npcId, out localStepX, out localStepY);
                    }

                    if (hasLocalStep && TryMoveTo(world, npcId, localStepX, localStepY, bus))
                    {
                        localMoved = true;
                        localToX = localStepX;
                        localToY = localStepY;
                    }
                    else if (MovementPathfinder.TryReplanNpcLocalSearch(world, npcId, x, y)
                        && MovementPathfinder.TryGetActiveNpcLocalSearchNextStep(world, npcId, out localStepX, out localStepY)
                        && TryMoveTo(world, npcId, localStepX, localStepY, bus))
                    {
                        localMoved = true;
                        localToX = localStepX;
                        localToY = localStepY;
                    }

                    if (localMoved)
                    {
                        world.NotifyNpcMovedForLandmarkLearning(npcId, fromX: x, fromY: y, toX: localToX, toY: localToY);
                        MovementPathfinder.AdvanceNpcLocalSearchAfterSuccessfulStep(world, npcId, x, y, localToX, localToY);
                        intent.BlockedTicks = 0;
                        world.NpcMoveIntents[npcId] = intent;
                        continue;
                    }

                    // Se la local search non riesce a produrre un passo reale, NON teniamo
                    // ownership forzata del tick. La rilasciamo e lasciamo che il codice
                    // sotto provi il movimento normale (direct / LM / fallback bounded).
                    world.ClearNpcLocalSearchState(npcId, "LocalSearchNoUsableStep");
                }

                bool directCommitOwnsMovement =
                    world.Pathfinding.DirectCommitExecution.TryGetValue(npcId, out var directOwnerState)
                    && directOwnerState != null
                    && directOwnerState.Active
                    && world.Pathfinding.MacroRouteExecution.TryGetValue(npcId, out var directOwnerMacroState)
                    && directOwnerMacroState != null
                    && directOwnerMacroState.NavigationMode == "DIRECT_APPROACHING";

                if ((inPrefixCommitment || directCommitOwnsMovement)
                    && TrySelectDirectCommitNextStep(world, npcId, x, y, out int directStepX, out int directStepY))
                {
                    effectiveTargetX = directStepX;
                    effectiveTargetY = directStepY;
                    usingDirectCommitStep = true;
                    inPrefixCommitment = true;
                    usingMacroImmediate = false;
                }

                // Step greedy: preferisci asse con maggiore distanza.
                // Nota molto importante:
                // - qui stiamo ancora provando il movimento "economico" standard;
                // - se questo fallisce NON dobbiamo concludere subito che l'NPC è bloccato,
                //   perché potrebbe esistere un piccolo path locale valido attorno a un ostacolo.
                int dx = effectiveTargetX - x;
                int dy = effectiveTargetY - y;

                int stepX = 0;
                int stepY = 0;

                if (Mathf.Abs(dx) >= Mathf.Abs(dy))
                    stepX = dx == 0 ? 0 : (dx > 0 ? 1 : -1);
                else
                    stepY = dy == 0 ? 0 : (dy > 0 ? 1 : -1);

                // Se l'asse scelto è 0 (perché dx==0 e Abs(dx)>=Abs(dy)), proviamo l'altro.
                if (stepX == 0 && stepY == 0)
                {
                    // Non dovrebbe succedere perché abbiamo gestito "arrivati" sopra, ma restiamo difensivi.
                    intent.Active = false;
                    intent.BlockedTicks = 0;
                    world.NpcMoveIntents[npcId] = intent;
                    world.SetNpcIdle(npcId);
                    continue;
                }

                bool moved = false;
                int movedToX = x;
                int movedToY = y;

                // Memorizza lo step primario greedy prima che il fallback lo sovrascriva.
                // Usato dal rilevamento porta per identificare la cella bloccata dall'NPC.
                int primaryStepX = stepX;
                int primaryStepY = stepY;

                // Se il target finale NON è diretto e stiamo seguendo un landmark immediato,
                // proviamo comunque a capire se quel landmark è almeno direttamente raggiungibile.
                // Questo non cambia ancora il comportamento, ma rende molto più leggibili i log
                // e documenta la distinzione tra "macro target valido" e "macro target localmente accessibile".
                bool effectiveTargetHasDirectPath = MovementPathfinder.CanNpcUseDirectPath(world, npcId, effectiveTargetX, effectiveTargetY);

                // Tentativo 1: step scelto
                int candidateX = x + stepX;
                int candidateY = y + stepY;
                if (!IsImmediateLocalSearchBacktrack(world, npcId, x, y, candidateX, candidateY)
                    && TryMoveTo(world, npcId, candidateX, candidateY, bus))
                {
                    moved = true;
                    movedToX = candidateX;
                    movedToY = candidateY;
                }
                else if (!usingDirectCommitStep)
                {
                    // Tentativo 2: prova sull'altro asse (fallback minimo)
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

                    // Se anche il fallback non è possibile, restiamo fermi.
                    // Nota: se stepX/stepY sono entrambi 0, TryMoveTo fallirà (bounds) ma preferiamo essere espliciti.
                    if (stepX != 0 || stepY != 0)
                    {
                        candidateX = x + stepX;
                        candidateY = y + stepY;
                        if (!IsImmediateLocalSearchBacktrack(world, npcId, x, y, candidateX, candidateY)
                            && TryMoveTo(world, npcId, candidateX, candidateY, bus))
                        {
                            moved = true;
                            movedToX = candidateX;
                            movedToY = candidateY;
                        }
                    }
                }

                // PATCH 0.02.05.2f:
                // se il greedy locale ha fallito, facciamo un ultimo tentativo con una ricerca bounded
                // cella-per-cella verso il target effettivo del tick.
                // Questo è il fix minimo che serve per casi come:
                // - target dietro un muro semplice senza LM noti;
                // - landmark immediato dietro una geometria locale non attraversabile direttamente;
                // - piccole stanze con unica uscita non allineata al target.
                bool movedUsingLocalSearch = false;

                if (!moved && !usingDirectCommitStep)
                {
                    var fallbackPath = new System.Collections.Generic.List<Vector2Int>(64);
                    int localSearchBudget = GetLocalSearchVisitedBudget(world);
                    LogLocalSearchAttempt(
                        world, npcId, x, y, finalTargetX, finalTargetY,
                        effectiveTargetX, effectiveTargetY, localSearchBudget,
                        usingMacroImmediate, inPrefixCommitment, isInLastMile);

                    bool localSearchFound = MovementPathfinder.TryBuildBoundedMovePath(
                        world, npcId, x, y, effectiveTargetX, effectiveTargetY, localSearchBudget, fallbackPath);
                    LogLocalSearchUseResult(world, npcId, x, y, effectiveTargetX, effectiveTargetY, localSearchFound, fallbackPath);

                    if (localSearchFound && fallbackPath.Count >= 2)
                    {
                        var next = fallbackPath[1];
                        if (world.Pathfinding.GoalLocalSearchExecution.TryGetValue(npcId, out var lastLocalState)
                            && lastLocalState != null
                            && lastLocalState.HasLastSuccessfulStep
                            && x == lastLocalState.LastStepToX
                            && y == lastLocalState.LastStepToY
                            && next.x == lastLocalState.LastStepFromX
                            && next.y == lastLocalState.LastStepFromY)
                        {
                            var alternativePath = new System.Collections.Generic.List<Vector2Int>(64);
                            int blockedFirstStep = world.CellIndex(lastLocalState.LastStepFromX, lastLocalState.LastStepFromY);
                            bool alternativeFound = MovementPathfinder.TryBuildBoundedMovePath(
                                world, npcId, x, y, effectiveTargetX, effectiveTargetY,
                                localSearchBudget, alternativePath, blockedFirstStep);

                            LogLocalSearchUseResult(world, npcId, x, y, effectiveTargetX, effectiveTargetY, alternativeFound, alternativePath);

                            if (alternativeFound && alternativePath.Count >= 2)
                            {
                                fallbackPath = alternativePath;
                                next = fallbackPath[1];
                            }
                            else
                            {
                                localSearchFound = false;
                            }
                        }

                        if (localSearchFound && fallbackPath.Count >= 2)
                        {
                            int remainingBudget = Mathf.Max(0, localSearchBudget - fallbackPath.Count);
                            world.SetDebugJumpPathForNpc(npcId, fallbackPath, remainingBudget);

                            if (TryMoveTo(world, npcId, next.x, next.y, bus))
                            {
                                moved = true;
                                movedUsingLocalSearch = true;
                                movedToX = next.x;
                                movedToY = next.y;
                                MovementPathfinder.AdvanceNpcLocalSearchAfterSuccessfulStep(world, npcId, x, y, next.x, next.y);
                            }
                        }
                    }
                }

                // PATCH NOTE:
                // - Se ci siamo mossi: resettiamo BlockedTicks.
                // - Se NON ci siamo mossi: incrementiamo BlockedTicks.
                //   Dopo DefaultIntentStuckTicks tick di blocco, cancelliamo l'intento.
                if (moved)
                {
                    // ============================================================
                    // Landmark learning hook (v0.02 Day3)
                    // ============================================================
                    //
                    // IMPORTANTISSIMO (ARCONTIO design):
                    // - l'NPC impara landmark/edge SOLO tramite esperienza (movimento).
                    // - questa chiamata aggiorna la memoria soggettiva (NpcLandmarkMemory)
                    //   usando il registry oggettivo (LandmarkRegistry) come fonte di nodi/edge validi.
                    //
                    world.NotifyNpcMovedForLandmarkLearning(npcId, fromX: x, fromY: y, toX: movedToX, toY: movedToY);

                    if (!movedUsingLocalSearch)
                    {
                        // Patch 0.02.07.B — Bug 1 fix:
                        // Se usingMacroImmediate=true ma IsApproachingFirstLm=true,
                        // l'NPC sta ancora raggiungendo il primo LM (tratto approaching).
                        // In questo tratto i passi sono di tipo DIRECT (azzurro),
                        // non LM_PATH (arancione). La transizione a LM_PATH avviene
                        // solo dopo aver fisicamente raggiunto il primo nodo.
                        bool isApproaching = usingMacroImmediate
                            && world.Pathfinding.MacroRouteExecution.TryGetValue(npcId, out var approachState)
                            && approachState != null && approachState.IsApproachingFirstLm;

                        if (usingMacroImmediate && !isApproaching)
                        {
                            // Tratto LM→LM (o last-mile greedy): segmento arancione.
                            world.AppendDebugLmStepForNpc(npcId, x, y, movedToX, movedToY);
                            LogDebugSegmentStep(world, npcId, "LM_ORANGE", x, y, movedToX, movedToY,
                                usingMacroImmediate, isApproaching, inPrefixCommitment, isInLastMile);
                            world.ClearNpcLocalSearchState(npcId, string.Empty);
                            world.ClearNpcDirectCommitState(npcId, string.Empty);
                        }
                        else if (usingGreedyFallbackForDebug && !directClearRuntimeForDebug)
                        {
                            // Greedy fallback debug: target non ancora acquisito percettivamente.
                            // Usiamo il layer magenta/rosa per distinguerlo dal direct reale.
                            world.AppendDebugJumpStepForNpc(npcId, x, y, movedToX, movedToY);
                            LogDebugSegmentStep(world, npcId, "GREEDY_FALLBACK_PINK", x, y, movedToX, movedToY,
                                usingMacroImmediate, isApproaching, inPrefixCommitment, isInLastMile);
                            world.ClearNpcDirectCommitState(npcId, string.Empty);
                        }
                        else
                        {
                            // Tratto direct (approaching, prefix commitment, last-mile direct):
                            // segmento azzurro.
                            world.AppendDebugDirectStepForNpc(npcId, x, y, movedToX, movedToY);
                            LogDebugSegmentStep(world, npcId, "DIRECT_BLUE", x, y, movedToX, movedToY,
                                usingMacroImmediate, isApproaching, inPrefixCommitment, isInLastMile);

                            if (usingDirectCommitStep)
                                AdvanceDirectCommitAfterMove(world, npcId, movedToX, movedToY);

                            // Decrementa il prefix commitment se attivo.
                            if (inPrefixCommitment
                                && world.Pathfinding.MacroRouteExecution.TryGetValue(npcId, out var ps)
                                && ps != null && ps.DirectPrefixStepsRemaining > 0)
                            {
                                ps.DirectPrefixStepsRemaining--;
                                world.Pathfinding.MacroRouteExecution[npcId] = ps;
                            }
                        }
                    }

                    intent.BlockedTicks = 0;
                    world.NpcMoveIntents[npcId] = intent;
                }
                else
                {
                    // ── RILEVAMENTO PORTA (v0.04.10.n) ───────────────────────────────────
                    // Prima di incrementare BlockedTicks, scansiona tutte e 4 le celle
                    // cardinali adiacenti all'NPC per trovare una porta chiusa apribile.
                    //
                    // Fix v0.04.10.n: in precedenza si controllava solo la cella
                    // x+primaryStepX, y+primaryStepY (direzione verso il target).
                    // Se la porta è in una direzione diversa dal target (tipico di un NPC
                    // in una stanza chiusa il cui target è fuori ma non allineato alla porta),
                    // il rilevamento mancava la porta e si incrementava BlockedTicks.
                    //
                    // Ora si scansionano tutte le 4 direzioni cardinali. La prima porta
                    // chiusa non bloccata trovata viene aperta; l'NPC non perde il tick.
                    {
                        bool doorOpenedThisTick = false;
                        // Ordine: N, S, E, W — arbitrario ma deterministico.
                        int[] cardDx = { 0,  0, 1, -1 };
                        int[] cardDy = { 1, -1, 0,  0 };
                        for (int d = 0; d < 4 && !doorOpenedThisTick; d++)
                        {
                            int cx = x + cardDx[d];
                            int cy = y + cardDy[d];
                            int adjObjId = world.GetObjectAt(cx, cy);
                            if (adjObjId < 0) continue;
                            if (!world.Objects.TryGetValue(adjObjId, out var adjInst) || adjInst == null) continue;
                            if (!world.TryGetObjectDef(adjInst.DefId, out var adjDef) || adjDef == null) continue;
                            if (!adjDef.IsDoor) continue;

                            if (adjInst.IsOpen)
                            {
                                // Porta aperta ma OcclusionMap non aggiornata: caso anomalo.
                                UnityEngine.Debug.LogWarning(
                                    $"[MovementSystem] npc={npcId} adiacente a porta APERTA ma bloccato" +
                                    $" obj={adjObjId} at ({cx},{cy}). OcclusionMap desincronizzata?");
                            }
                            else if (!adjInst.IsLocked)
                            {
                                // Porta chiusa e non bloccata: apri e prosegui al prossimo tick.
                                new OpenDoorCommand(npcId, adjObjId).Execute(world, bus);
                                doorOpenedThisTick = true;
                            }
                            // IsLocked=true: trattata come muro, continua a scansionare.
                        }

                        if (doorOpenedThisTick)
                        {
                            world.NpcMoveIntents[npcId] = intent; // intent invariato, nessun BlockedTick
                            continue;
                        }
                    }

                    intent.BlockedTicks++;

                    if (intent.BlockedTicks >= DefaultIntentStuckTicks)
                    {
                        // ── FAILURE LADDER (v0.03.05-FailureLadder) ──────────────────────
                        // Invece di cancellare subito l'intent, tenta prima un back-off+replan.
                        // Stage 1 → back-off breve → replan.
                        // Stage 2 → back-off lungo → replan.
                        // Stage > backoff_max_stages → cancella intent (comportamento originale).
                        var mvParams      = world.Config?.Sim?.movement;
                        int maxStages     = mvParams?.backoff_max_stages ?? 2;
                        int currentStage  = world.Pathfinding.GetMoveBackOffStage(npcId) + 1;
                        long nowTickLong  = TickContext.CurrentTickIndex;

                        intent.BlockedTicks = 0;

                        if (currentStage > maxStages)
                        {
                            // Stage esaurite: cancella l'intent (stesso comportamento precedente).
                            intent.Active = false;
                            world.NpcMoveIntents[npcId] = intent;
                            world.ClearNpcLocalSearchState(npcId, "IntentCancelledStuck");
                            world.ClearNpcDirectCommitState(npcId, "IntentCancelledStuck");
                            world.SetNpcIdle(npcId);
                            world.MarkMacroRouteExecutionBlocked(npcId, duringLastMile: macroLastMile);
                            world.Pathfinding.ClearMoveBackOff(npcId);

                            ArcontioLogger.Trace(
                                new LogContext(tick: (int)nowTickLong, channel: "Move", npcId: npcId, cell: (x, y)),
                                new LogBlock(LogLevel.Trace, "log.move.intent_cancelled_stuck")
                                    .AddField("targetX", intent.TargetX)
                                    .AddField("targetY", intent.TargetY)
                                    .AddField("effectiveTargetX", effectiveTargetX)
                                    .AddField("effectiveTargetY", effectiveTargetY)
                                    .AddField("usingMacroImmediate", usingMacroImmediate)
                                    .AddField("effectiveTargetHasDirectPath", effectiveTargetHasDirectPath)
                                    .AddField("reason", intent.Reason.ToString())
                                    .AddField("failureLadderStage", currentStage)
                            );
                        }
                        else
                        {
                            // Entra in back-off: NPC pausa, poi al termine tenta replan.
                            world.NpcMoveIntents[npcId] = intent;
                            world.Pathfinding.BeginMoveBackOff(npcId, nowTickLong, currentStage, mvParams);
                            world.ClearNpcLocalSearchState(npcId, "BackOff_Stage" + currentStage);
                            world.ClearNpcDirectCommitState(npcId, "BackOff_Stage" + currentStage);
                            world.MarkMacroRouteExecutionBlocked(npcId, duringLastMile: macroLastMile);
                            world.SetNpcIdle(npcId);

                            // Task 5 — Blacklist edge bloccato.
                            // Penalizza il macro-edge che ha causato lo stallo così che
                            // il prossimo A* su grafo tenda a evitarlo.
                            if (usingMacroImmediate && macroNextNodeId != 0)
                            {
                                int fromNodeId = world.NpcLandmarkMemory.TryGetValue(npcId, out var lmMemBl)
                                    ? lmMemBl.LastVisitedLandmarkId : 0;
                                if (fromNodeId != 0)
                                    world.BlacklistBlockedMacroEdge(npcId, fromNodeId, macroNextNodeId, currentStage);
                            }

                            ArcontioLogger.Trace(
                                new LogContext(tick: (int)nowTickLong, channel: "Move", npcId: npcId, cell: (x, y)),
                                new LogBlock(LogLevel.Trace, "log.move.backoff_started")
                                    .AddField("targetX", intent.TargetX)
                                    .AddField("targetY", intent.TargetY)
                                    .AddField("stage", currentStage)
                                    .AddField("reason", intent.Reason.ToString())
                            );
                        }
                    }
                    else
                    {
                        world.NpcMoveIntents[npcId] = intent;
                    }
                }

                // Nota importante (molto verbosa ma utile):
                // In futuro, qui è il punto giusto per:
                // - emettere un "NpcMovedEvent"
                // - settare PerceptionDirty (event-driven perception)
                // Attualmente molti sistemi fanno polling e quindi non è strettamente necessario,
                // ma il documento sight spinge verso trigger dopo movimento/rotazione.
            }
        }

        // =====================================================================
        // INIZIALIZZAZIONE NAVIGAZIONE (Patch 0.02.05.B / 0.02.07.A)
        // =====================================================================

        /// <summary>
        /// Verifica se il TARGET è percettivamente acquisibile per il direct path.
        ///
        /// A differenza di <see cref="MovementPathfinder.CanNpcUseDirectPath"/>
        /// (che verifica la traversabilità dell'intero path), questo metodo verifica
        /// che il TARGET STESSO sia visibile dall'NPC (Range + FOV + LOS),
        /// come richiesto dalla regola 1 del documento "Commitment Percettivo".
        ///
        /// Solo se directCheckFovOnAcquisition = true in game_params.json.
        /// </summary>
        private static bool CanAcquireDirectPerceptually(
            World world, int npcId, int targetX, int targetY)
        {
            if (!world.GridPos.TryGetValue(npcId, out var pos))
                return false;

            // Legge parametri visivi dall'NPC
            int   visionRange = world.Global.NpcVisionRangeCells;
            if (visionRange <= 0) visionRange = 6;
            bool  useCone   = world.Global.NpcVisionUseCone;
            float coneSlope = world.Global.NpcVisionConeSlope;

            if (!world.NpcFacing.TryGetValue(npcId, out var facing))
                facing = CardinalDirection.North;

            // Pipeline canonica: Range → Cone/Front → LOS (uguale a ObjectPerceptionSystem)
            return FovUtils.IsVisible(world, pos.X, pos.Y, facing,
                                      targetX, targetY,
                                      visionRange, useCone, coneSlope);
        }

        private static bool CanAcquireDirectTarget(
            World world,
            int npcId,
            int targetX,
            int targetY,
            bool checkFov)
        {
            if (!world.GridPos.TryGetValue(npcId, out var pos))
                return false;

            if (!checkFov)
                return true;

            if (FovUtils.Manhattan(pos.X, pos.Y, targetX, targetY) <= 1)
                return true;

            return CanAcquireDirectPerceptually(world, npcId, targetX, targetY);
        }

        /// <summary>
        /// Inizializza la strategia di navigazione per un nuovo intent.
        ///
        /// <para>
        /// Viene chiamata <b>una sola volta</b> per ogni intent, al primo tick
        /// in cui il MovementSystem lo processa (quando <c>intent.IsNew == true</c>).
        /// Al termine imposta <c>intent.IsNew = false</c>.
        /// </para>
        ///
        /// <para><b>Logica di selezione (Patch 0.02.05.B):</b></para>
        /// <list type="number">
        ///   <item>
        ///     Se <c>CanNpcUseDirectPath</c> ritorna true: il target è raggiungibile
        ///     con il greedy locale senza ostacoli. Prepara il debug path diretto
        ///     e pulisce la macro-route (non serve).
        ///   </item>
        ///   <item>
        ///     Altrimenti: avvia la macro-route landmark
        ///     (<c>BeginMacroRouteExecutionForNpc</c>). Se la macro-route non
        ///     è disponibile (NPC non conosce landmark), prepara un prefix path
        ///     diretto come fallback visivo per la card.
        ///   </item>
        /// </list>
        ///
        /// <para>
        /// NOTA: questa logica era precedentemente in <c>SetMoveIntentCommand</c>,
        /// dove violava il contratto del Command (un Command non deve fare planning).
        /// </para>
        /// </summary>
        private static void InitializeNavigation(World world, int npcId, ref MoveIntent intent)
        {
            // Marca subito come inizializzato: anche se fallisse tutto,
            // non vogliamo ripetere l'inizializzazione al tick successivo.
            intent.IsNew = false;

            if (!world.GridPos.TryGetValue(npcId, out var pos))
                return;

            int targetX = intent.TargetX;
            int targetY = intent.TargetY;

            // ── ACQUISIZIONE DIRECT (Patch 0.02.07.A) ────────────────────────
            // Regola 1 documento "Commitment Percettivo":
            //   direct = target visibile (Range+FOV+LOS) + path traversabile greedy.
            //
            // Se directCheckFovOnAcquisition=true (game_params.json): verifica prima
            // che il target sia percettivamente visibile, poi che il path sia libero.
            // Se false: usa solo la traversabilità greedy (modalità legacy).
            bool checkFov     = GetDirectCheckFov(world);
            bool targetVisible = CanAcquireDirectTarget(world, npcId, targetX, targetY, checkFov);
            bool pathClear     = targetVisible && MovementPathfinder.CanNpcUseDirectPath(world, npcId, targetX, targetY);

            LogDirectGateDebug(
                world, npcId, "init_direct_gate", pos, targetX, targetY,
                checkFov, targetVisible, pathClear, targetX, targetY,
                inPrefixCommitment: false, isInLastMile: false,
                lastMileJustConverted: false, usingMacroImmediate: false);

            if (pathClear)
            {
                // ── DIRECT PATH con PREFIX COMMITMENT ────────────────────────
                // Regola 2: costruisci un prefix path breve (directPrefixCells)
                // e imposta il contatore PrefixStepsRemaining nello stato esecutivo.
                // L'NPC eseguirà questo prefix senza ricontrollare la visibilità
                // completa (solo traversabilità della prossima cella per step).
                world.ClearDebugMacroRouteForNpc(npcId);

                var directPath = new System.Collections.Generic.List<UnityEngine.Vector2Int>(32);
                if (MovementPathfinder.TryBuildGreedyDirectPath(world, npcId, pos.X, pos.Y, targetX, targetY, directPath))
                {
                    world.SetDebugDirectPathForNpc(npcId, directPath);

                    // Inizializza il prefix commitment.
                    // Fix 0.02.07.A bug 3: se MacroRouteExecution non esiste (percorso
                    // puramente direct senza LM), crea uno stato minimo per ospitare
                    // il contatore PrefixStepsRemaining.
                    int prefixLen = Mathf.Min(GetDirectPrefixCells(world), directPath.Count - 1);
                    if (!world.Pathfinding.MacroRouteExecution.TryGetValue(npcId, out var macroState)
                        || macroState == null)
                    {
                        macroState = new NpcMacroRouteExecutionState
                        {
                            Active                    = true,
                            NavigationMode            = "DIRECT_APPROACHING",
                            LastModeSwitchTick        = (int)TickContext.CurrentTickIndex,
                            LastModeSwitchReason      = "DirectInit",
                            DirectPrefixStepsRemaining = prefixLen,
                        };
                    }
                    else
                    {
                        macroState.DirectPrefixStepsRemaining = prefixLen;
                        macroState.NavigationMode             = "DIRECT_APPROACHING";
                    }
                    world.Pathfinding.MacroRouteExecution[npcId] = macroState;

                    LogDirectGateDebug(
                        world, npcId, "init_direct_commit", pos, targetX, targetY,
                        checkFov, targetVisible, pathClear, targetX, targetY,
                        inPrefixCommitment: true, isInLastMile: false,
                        lastMileJustConverted: false, usingMacroImmediate: false,
                        extraKey: "directPathCount", extraValue: directPath.Count);
                }
            }
            else
            {
                // ============================================================
                // MACRO-ROUTE: serve passare per nodi landmark intermedi
                // ============================================================
                // Avvia A* sul grafo soggettivo + inizializza stato esecutivo.
                world.BeginMacroRouteExecutionForNpc(npcId, targetX, targetY);

                // Fallback: se la macro-route non è disponibile (NPC senza landmark),
                // prepara un prefix path diretto come debug visivo per la card.
                if (!world.Pathfinding.MacroRouteExecution.TryGetValue(npcId, out var macroState)
                    || macroState == null
                    || !macroState.Active)
                {
                    var directPrefix = new System.Collections.Generic.List<UnityEngine.Vector2Int>(32);
                    if (MovementPathfinder.TryBuildGreedyDirectPrefixPath(world, npcId, pos.X, pos.Y, targetX, targetY, directPrefix))
                    {
                        world.SetDebugDirectPathForNpc(npcId, directPrefix);
                        LogDirectGateDebug(
                            world, npcId, "init_macro_unavailable_direct_prefix_visual", pos, targetX, targetY,
                            checkFov, targetVisible, pathClear, targetX, targetY,
                            inPrefixCommitment: false, isInLastMile: false,
                            lastMileJustConverted: false, usingMacroImmediate: false,
                            extraKey: "visualPrefixCount", extraValue: directPrefix.Count);
                    }
                    else
                    {
                        LogDirectGateDebug(
                            world, npcId, "init_macro_unavailable_no_visual_prefix", pos, targetX, targetY,
                            checkFov, targetVisible, pathClear, targetX, targetY,
                            inPrefixCommitment: false, isInLastMile: false,
                            lastMileJustConverted: false, usingMacroImmediate: false);
                    }
                }
                else
                {
                    LogDirectGateDebug(
                        world, npcId, "init_macro_route_started", pos, targetX, targetY,
                        checkFov, targetVisible, pathClear,
                        macroState.ImmediateTargetX, macroState.ImmediateTargetY,
                        inPrefixCommitment: false, isInLastMile: macroState.IsDoingLastMile,
                        lastMileJustConverted: false, usingMacroImmediate: true);
                }
            }
        }

        /// <summary>
        /// Determina se questo tick è il tick corretto per rivalutare la validità
        /// dell'oggetto target (throttle: ogni DefaultTargetValidateEveryTicks tick).
        /// </summary>
        private static bool ShouldValidateTargetThisTick(Tick tick)
        {
            // Usiamo TickContext (globale) invece del parametro tick per coerenza
            // con il resto del codebase. Se DefaultTargetValidateEveryTicks == 1: sempre true.
            int t = (int)TickContext.CurrentTickIndex;
            return DefaultTargetValidateEveryTicks <= 1 || (t % DefaultTargetValidateEveryTicks) == 0;
        }

        /// <summary>
        /// Valida TargetObjectId se presente.
        ///
        /// Politica:
        /// - Se TargetObjectId non esiste più -> intent cancellato.
        /// - Se l'oggetto esiste ma è uno stock cibo con Units<=0 -> intent cancellato.
        /// - Se l'oggetto esiste ma si trova in una cella diversa dal TargetX/Y dell'intent -> intent cancellato.
        ///
        /// Perché cancellare quando l'oggetto si sposta?
        /// - In questo baseline non abbiamo "tracking" intelligente della posizione dell'oggetto.
        /// - La cella target diventa una "last known cell"; la scelta più robusta è cancellare e lasciare re-plan.
        /// </summary>
        private static bool ValidateOrCancelTarget(World world, int npcId, ref MoveIntent intent)
        {
            if (intent.TargetObjectId == 0)
                return true;

            // 1) L'oggetto deve esistere in world.Objects.
            if (!world.Objects.TryGetValue(intent.TargetObjectId, out var obj) || obj == null)
            {
                CancelIntent(world, npcId, ref intent, "missing_object");
                return false;
            }

            // 2) Se l'oggetto è uno stock cibo, deve avere unità > 0.
            //    Nota: questo controlla la "presenza significativa" del target (se vuoto non ha senso inseguirlo).
            if (world.FoodStocks.TryGetValue(intent.TargetObjectId, out var stock))
            {
                if (stock.Units <= 0)
                {
                    CancelIntent(world, npcId, ref intent, "foodstock_empty");
                    return false;
                }
            }

            // 3) Se l'oggetto si è spostato, non ha più senso perseguire la vecchia cella target.
            //    Cancelliamo e demandiamo alle Rules la scelta del nuovo target (via memoria/percezione).
            if (obj.CellX != intent.TargetX || obj.CellY != intent.TargetY)
            {
                CancelIntent(world, npcId, ref intent, "target_moved");
                return false;
            }

            return true;
        }

        private static void CancelIntent(World world, int npcId, ref MoveIntent intent, string why)
        {
            intent.Active = false;
            intent.BlockedTicks = 0;
            world.NpcMoveIntents[npcId] = intent;
            world.SetNpcIdle(npcId);

            ArcontioLogger.Trace(
                new LogContext(tick: (int)TickContext.CurrentTickIndex, channel: "Move", npcId: npcId, cell: default),
                new LogBlock(LogLevel.Trace, "log.move.intent_cancelled_target_invalid")
                    .AddField("why", why)
                    .AddField("targetX", intent.TargetX)
                    .AddField("targetY", intent.TargetY)
                    .AddField("targetObjectId", intent.TargetObjectId)
                    .AddField("reason", intent.Reason.ToString())
            );
        }

        private static bool TryMoveTo(World world, int npcId, int tx, int ty, MessageBus bus)
        {
            // Bounds
            if (!world.InBounds(tx, ty))
                return false;

            // Blocco movimento: se la cella è bloccata, controlla se è una porta chiusa apribile.
            if (world.IsMovementBlocked(tx, ty))
            {
                int doorObjId = world.GetObjectAt(tx, ty);
                bool opened = false;
                if (doorObjId >= 0
                    && world.Objects.TryGetValue(doorObjId, out var doorInst) && doorInst != null
                    && world.TryGetObjectDef(doorInst.DefId, out var doorDef) && doorDef != null
                    && doorDef.IsDoor && !doorInst.IsOpen && !doorInst.IsLocked)
                {
                    new OpenDoorCommand(npcId, doorObjId).Execute(world, bus);
                    opened = true;

                    ArcontioLogger.Trace(
                        new LogContext(tick: (int)TickContext.CurrentTickIndex, channel: "Move", npcId: npcId, cell: (tx, ty)),
                        new LogBlock(LogLevel.Trace, "log.move.door_opened_trymoveto")
                            .AddField("x", tx)
                            .AddField("y", ty)
                            .AddField("doorId", doorObjId)
                    );
                }

                // Dopo l'apertura la cella potrebbe essere ancora bloccata (porta locked, altro oggetto ecc.).
                if (!opened || world.IsMovementBlocked(tx, ty))
                    return false;
            }

            // 1 NPC per cell: se c'è già qualcuno non entriamo.
            if (world.TryGetNpcAt(tx, ty, out _))
                return false;

            // Muovi
            world.GridPos[npcId] = new GridPosition(tx, ty);

            ArcontioLogger.Trace(
                new LogContext(tick: (int)TickContext.CurrentTickIndex, channel: "Move", npcId: npcId, cell: (tx, ty)),
                new LogBlock(LogLevel.Trace, "log.move.step")
                    .AddField("x", tx)
                    .AddField("y", ty)
            );

            return true;
        }
    }
}
