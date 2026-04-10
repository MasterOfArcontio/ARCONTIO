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
                    bool lmVisible   = !checkFovLm
                                       || CanAcquireDirectPerceptually(world, npcId, lmTargetX, lmTargetY);
                    bool lmPathClear = lmVisible
                                       && MovementPathfinder.CanNpcUseDirectPath(world, npcId, lmTargetX, lmTargetY);

                    if (lmPathClear)
                    {
                        // Target finale visibile e path libero:
                        // converti il last-mile in un direct con prefix commitment.
                        int lmPrefixLen = Mathf.Min(GetDirectPrefixCells(world),
                            Mathf.Abs(lmTargetX - pos.X) + Mathf.Abs(lmTargetY - pos.Y));
                        lmState.NavigationMode              = "DIRECT_APPROACHING";
                        lmState.LastModeSwitchTick          = (int)TickContext.CurrentTickIndex;
                        lmState.LastModeSwitchReason        = "LastMileConvertedToDirect";
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
                        bool canRenew       = !checkFovRenew
                                             || CanAcquireDirectPerceptually(world, npcId, finalTargetX, finalTargetY);
                        bool pathStillClear = canRenew
                                             && MovementPathfinder.CanNpcUseDirectPath(world, npcId, finalTargetX, finalTargetY);

                        if (pathStillClear)
                        {
                            execState2.DirectPrefixStepsRemaining = GetDirectPrefixCells(world);
                            world.Pathfinding.MacroRouteExecution[npcId] = execState2;
                            inPrefixCommitment = true;
                        }
                        // else: esci dal direct, torna al greedy LM last-mile.
                    }
                }

                // Se non siamo in prefix commitment, selezioniamo il target normalmente.
                if (!inPrefixCommitment)
                {
                    if (!MovementPathfinder.CanNpcUseDirectPath(world, npcId, finalTargetX, finalTargetY))
                    {
                        if (world.TryGetMacroExecutionImmediateTarget(npcId, out int macroTargetX, out int macroTargetY, out macroLastMile, out macroNextNodeId))
                        {
                            effectiveTargetX = macroTargetX;
                            effectiveTargetY = macroTargetY;
                            usingMacroImmediate = true;
                        }
                    }
                    else if (!isInLastMile)
                    {
                        // Fix bug 3: il target è raggiungibile direttamente ma il NavMode
                        // potrebbe essere rimasto APPROACHING_LM da un piano LM precedente.
                        // Forziamo DIRECT_APPROACHING per allineare card e comportamento reale.
                        if (world.Pathfinding.MacroRouteExecution.TryGetValue(npcId, out var fixState)
                            && fixState != null
                            && fixState.NavigationMode == "APPROACHING_LM")
                        {
                            fixState.NavigationMode       = "DIRECT_APPROACHING";
                            fixState.LastModeSwitchTick   = (int)TickContext.CurrentTickIndex;
                            fixState.LastModeSwitchReason = "DirectOverrideLm";
                            world.Pathfinding.MacroRouteExecution[npcId] = fixState;
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

                    if (hasLocalStep && TryMoveTo(world, npcId, localStepX, localStepY))
                    {
                        localMoved = true;
                        localToX = localStepX;
                        localToY = localStepY;
                    }
                    else if (MovementPathfinder.TryReplanNpcLocalSearch(world, npcId, x, y)
                        && MovementPathfinder.TryGetActiveNpcLocalSearchNextStep(world, npcId, out localStepX, out localStepY)
                        && TryMoveTo(world, npcId, localStepX, localStepY))
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
                if (TryMoveTo(world, npcId, x + stepX, y + stepY))
                {
                    moved = true;
                    movedToX = x + stepX;
                    movedToY = y + stepY;
                }
                else
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
                        if (TryMoveTo(world, npcId, x + stepX, y + stepY))
                        {
                            moved = true;
                            movedToX = x + stepX;
                            movedToY = y + stepY;
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

                if (!moved)
                {
                    var fallbackPath = new System.Collections.Generic.List<Vector2Int>(64);
                    if (MovementPathfinder.TryBuildBoundedMovePath(world, npcId, x, y, effectiveTargetX, effectiveTargetY, GetLocalSearchVisitedBudget(world), fallbackPath)
                        && fallbackPath.Count >= 2)
                    {
                        int remainingBudget = Mathf.Max(0, GetLocalSearchVisitedBudget(world) - fallbackPath.Count);
                        world.SetDebugJumpPathForNpc(npcId, fallbackPath, remainingBudget);

                        var next = fallbackPath[1];
                        if (TryMoveTo(world, npcId, next.x, next.y))
                        {
                            moved = true;
                            movedUsingLocalSearch = true;
                            movedToX = next.x;
                            movedToY = next.y;
                            MovementPathfinder.AdvanceNpcLocalSearchAfterSuccessfulStep(world, npcId, x, y, next.x, next.y);
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
                            world.ClearNpcLocalSearchState(npcId, string.Empty);
                            world.ClearNpcDirectCommitState(npcId, string.Empty);
                        }
                        else
                        {
                            // Tratto direct (approaching, prefix commitment, last-mile direct):
                            // segmento azzurro.
                            world.AppendDebugDirectStepForNpc(npcId, x, y, movedToX, movedToY);
                            world.ClearNpcLocalSearchState(npcId, string.Empty);

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
                    // ── RILEVAMENTO PORTA (PATCH 4 — v0.04.10.f) ─────────────────────────
                    // Prima di incrementare BlockedTicks, verifica se la cella verso cui
                    // l'NPC voleva muoversi è bloccata da una porta.
                    //
                    // Casi:
                    // - Porta aperta ma bloccata   → caso anomalo, log warning + BlockedTicks.
                    // - Porta chiusa, non bloccata → emetti OpenDoorCommand, NON incrementare
                    //   BlockedTicks. Il movimento avverrà al tick successivo.
                    // - Porta chiusa, bloccata     → trattare come muro, BlockedTicks normale.
                    //   (NOTA FUTURA: con inventario NPC, verificare qui la chiave.)
                    {
                        int doorCellX = x + primaryStepX;
                        int doorCellY = y + primaryStepY;
                        int doorObjId = world.GetObjectAt(doorCellX, doorCellY);

                        if (doorObjId >= 0
                            && world.Objects.TryGetValue(doorObjId, out var doorInst)
                            && doorInst != null
                            && world.TryGetObjectDef(doorInst.DefId, out var doorDef)
                            && doorDef != null
                            && doorDef.IsDoor)
                        {
                            if (doorInst.IsOpen)
                            {
                                // Porta aperta ma OcclusionMap non aggiornata: caso anomalo.
                                UnityEngine.Debug.LogWarning(
                                    $"[MovementSystem] npc={npcId} bloccato da porta APERTA" +
                                    $" obj={doorObjId} at ({doorCellX},{doorCellY}). OcclusionMap desincronizzata?");
                            }
                            else if (!doorInst.IsLocked)
                            {
                                // Porta chiusa e non bloccata: apri e prosegui al prossimo tick.
                                new OpenDoorCommand(npcId, doorObjId).Execute(world, bus);
                                world.NpcMoveIntents[npcId] = intent; // intent invariato, nessun BlockedTick
                                continue;
                            }
                            // IsLocked=true: cade nel comportamento normale (muro invalicabile).
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
            bool targetVisible = !checkFov || CanAcquireDirectPerceptually(world, npcId, targetX, targetY);
            bool pathClear     = targetVisible && MovementPathfinder.CanNpcUseDirectPath(world, npcId, targetX, targetY);

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
                        world.SetDebugDirectPathForNpc(npcId, directPrefix);
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

        private static bool TryMoveTo(World world, int npcId, int tx, int ty)
        {
            // Bounds
            if (!world.InBounds(tx, ty))
                return false;

            // Blocco movimento
            if (world.IsMovementBlocked(tx, ty))
                return false;

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
