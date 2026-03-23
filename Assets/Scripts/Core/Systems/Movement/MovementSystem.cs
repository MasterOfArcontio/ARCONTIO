using Arcontio.Core.Diagnostics;
using Arcontio.Core.Logging;
using UnityEngine;

namespace Arcontio.Core
{
    /// <summary>
    /// MovementSystem (Day10):
    /// Consuma MoveIntent e prova ad avanzare di 1 cella per tick.
    ///
    /// Baseline volutamente semplice (v0.01):
    /// - distanza Manhattan
    /// - 1 step per tick
    /// - collisione minima: non entra fuori bounds, non entra in celle bloccate da movimento,
    ///   non entra in cella con NPC (1 NPC per cella, standard).
    ///
    /// Questo è sufficiente per:
    /// - muoversi verso cibo/letto
    /// - testare occlusione/LOS e witness dei furti
    ///
    /// In futuro:
    /// - pathfinding
    /// - gestione porte (auto-open durante MoveTo)
    /// - NpcMovedEvent / PerceptionDirty event-driven
    ///
    /// PATCH NOTE (runtime fix):
    /// - Introduciamo "stuck detection" per MoveIntent:
    ///   se il target cell è occupato o il movimento fallisce ripetutamente,
    ///   dopo N tick cancelliamo l'intento (così le Rules possono re-plan).
    /// - Introduciamo anche "target validation" per TargetObjectId:
    ///   se l'oggetto target scompare / viene consumato / si sposta altrove,
    ///   cancelliamo l'intento perché la cella target non è più significativa.
    /// 
    /// Motivazione:
    /// - Con lo standard "1 NPC per cella", è frequente che un NPC insegua una cella
    ///   che nel frattempo diventa occupata (es. stock cibo su cui un altro NPC sta mangiando).
    /// - Senza invalidazione, l'NPC rimane piantato con intent attivo verso una cella
    ///   irraggiungibile, anche se esistono target alternativi.
    /// </summary>
    public sealed class MovementSystem : ISystem
    {
        public int Period => 1;

        // IMPORTANTISSIMO (design + debug):
        // Questi valori dovrebbero essere letti da config (game_params.json).
        // Per ora li teniamo qui come baseline sicura per chiudere un bug runtime.
        // Step successivo (5.1/6A) potrà renderli data-driven.
        private const int DefaultIntentStuckTicks = 12;

        // Ogni quanti tick rivalutiamo la validità dell'oggetto target (TargetObjectId).
        // 1 = ogni tick (più robusto, un po' più costoso ma N è piccolo in Day10).
        private const int DefaultTargetValidateEveryTicks = 1;

        // PATCH 0.02.05.3:
        // La ricerca locale non usa più un budget hardcoded puro.
        // Il valore di fallback resta solo difensivo se per qualche motivo il config non è disponibile.
        private const int DefaultBoundedSearchVisited = 256;

        private static Arcontio.Core.Config.LandmarkLocalSearchParams GetLocalSearchConfig(World world)
        {
            // Difesa estrema: in runtime normale world.Config e Sim devono esistere.
            if (world?.Config?.Sim?.landmarks?.localSearch != null)
                return world.Config.Sim.landmarks.localSearch;

            return new Arcontio.Core.Config.LandmarkLocalSearchParams();
        }

        private static int GetLocalSearchVisitedBudget(World world)
        {
            var cfg = GetLocalSearchConfig(world);
            return Mathf.Max(1, cfg.maxExpandedNodes);
        }

        public void Update(World world, Tick tick, MessageBus bus, Telemetry telemetry)
        {
             // Nota: iteriamo su NpcCore.Keys (source of truth degli NPC esistenti)
            foreach (var npcId in world.NpcCore.Keys)
            {
                if (!world.NpcMoveIntents.TryGetValue(npcId, out var intent) || !intent.Active)
                    continue;
                if (!world.GridPos.TryGetValue(npcId, out var pos))
                    continue;

                // Day5: se esiste una macro-route in esecuzione, prima di qualsiasi altra cosa
                // proviamo a far avanzare lo stato rispetto alla cella corrente.
                // Questo gestisce correttamente il caso in cui l'NPC parta gia' sopra lo start landmark
                // oppure entri sul prossimo landmark e debba immediatamente passare allo step successivo.
                world.TryAdvanceMacroRouteExecutionAtCell(npcId, pos.X, pos.Y);

                // PATCH 0.02.05.2f:
                // gerarchia corretta di navigazione:
                // 1) se il target finale è raggiungibile con un vero path diretto coerente col movimento reale,
                //    il direct commit ha PRIORITÀ ASSOLUTA e i landmark non devono interferire;
                // 2) se il target finale non è diretto, usiamo la macro-route landmark se presente;
                // 3) se nemmeno il landmark immediato è direttamente raggiungibile, più avanti proveremo una
                //    ricerca locale bounded verso il target effettivo del tick.
                int finalTargetX = intent.TargetX;
                int finalTargetY = intent.TargetY;
                int effectiveTargetX = finalTargetX;
                int effectiveTargetY = finalTargetY;
                bool macroLastMile = false;
                int macroNextNodeId = 0;
                bool usingMacroImmediate = false;

                if (!world.CanNpcUseDirectPath(npcId, finalTargetX, finalTargetY))
                {
                    if (world.TryGetMacroExecutionImmediateTarget(npcId, out int macroTargetX, out int macroTargetY, out macroLastMile, out macroNextNodeId))
                    {
                        effectiveTargetX = macroTargetX;
                        effectiveTargetY = macroTargetY;
                        usingMacroImmediate = true;
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

                    // Manteniamo il path debug gia' disegnato ma spegniamo gli stati runtime
                    // direct/local search, cosi' la card non resta bloccata su una modalita'
                    // ormai conclusa.
                    world.ClearNpcLocalSearchState(npcId, string.Empty);
                    world.ClearNpcDirectCommitState(npcId, string.Empty);
                    world.SetNpcIdle(npcId);
                    continue;
                }

                // ============================================================
                // PATCH 0.02.02P - Local search ownership forte
                // ============================================================
                // Se una local search e' attiva, e' LEI la proprietaria del movimento.
                // Non vogliamo che nello stesso tick la macro-navigation landmark si riprenda
                // il controllo, altrimenti nasce il ping-pong LM_PATH <-> GOAL_LOCAL_SEARCH.
                if (world.HasActiveNpcLocalSearch(npcId))
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

                    bool hasLocalStep = world.TryGetActiveNpcLocalSearchNextStep(npcId, out int localStepX, out int localStepY);
                    if (!hasLocalStep)
                    {
                        hasLocalStep = world.TryReplanNpcLocalSearch(npcId, x, y)
                            && world.TryGetActiveNpcLocalSearchNextStep(npcId, out localStepX, out localStepY);
                    }

                    if (hasLocalStep && TryMoveTo(world, npcId, localStepX, localStepY))
                    {
                        localMoved = true;
                        localToX = localStepX;
                        localToY = localStepY;
                    }
                    else if (world.TryReplanNpcLocalSearch(npcId, x, y)
                        && world.TryGetActiveNpcLocalSearchNextStep(npcId, out localStepX, out localStepY)
                        && TryMoveTo(world, npcId, localStepX, localStepY))
                    {
                        localMoved = true;
                        localToX = localStepX;
                        localToY = localStepY;
                    }

                    if (localMoved)
                    {
                        world.NotifyNpcMovedForLandmarkLearning(npcId, fromX: x, fromY: y, toX: localToX, toY: localToY);
                        world.AdvanceNpcLocalSearchAfterSuccessfulStep(npcId, x, y, localToX, localToY);
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

                // Se il target finale NON è diretto e stiamo seguendo un landmark immediato,
                // proviamo comunque a capire se quel landmark è almeno direttamente raggiungibile.
                // Questo non cambia ancora il comportamento, ma rende molto più leggibili i log
                // e documenta la distinzione tra "macro target valido" e "macro target localmente accessibile".
                bool effectiveTargetHasDirectPath = world.CanNpcUseDirectPath(npcId, effectiveTargetX, effectiveTargetY);

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
                    if (world.TryBuildBoundedMovePath(npcId, x, y, effectiveTargetX, effectiveTargetY, GetLocalSearchVisitedBudget(world), fallbackPath)
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
                            world.AdvanceNpcLocalSearchAfterSuccessfulStep(npcId, x, y, next.x, next.y);
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
                        if (usingMacroImmediate)
                        {
                            world.AppendDebugLmStepForNpc(npcId, x, y, movedToX, movedToY);
                            world.ClearNpcLocalSearchState(npcId, string.Empty);
                            world.ClearNpcDirectCommitState(npcId, string.Empty);
                        }
                        else
                        {
                            world.AppendDebugDirectStepForNpc(npcId, x, y, movedToX, movedToY);
                            world.ClearNpcLocalSearchState(npcId, string.Empty);
                        }
                    }

                    intent.BlockedTicks = 0;
                    world.NpcMoveIntents[npcId] = intent;
                }
                else
                {
                    intent.BlockedTicks++;

                    if (intent.BlockedTicks >= DefaultIntentStuckTicks)
                    {
                        // Cancelliamo l'intento: il target cell è di fatto irraggiungibile (occupata o bloccata).
                        // Questo sblocca la simulazione: al tick successivo le Rules possono fare re-plan
                        // scegliendo un target alternativo (altro cibo in vista/memoria, ecc.).
                        intent.Active = false;
                        intent.BlockedTicks = 0;
                        world.NpcMoveIntents[npcId] = intent;
                        world.ClearNpcLocalSearchState(npcId, "IntentCancelledStuck");
                        world.ClearNpcDirectCommitState(npcId, "IntentCancelledStuck");
                        world.SetNpcIdle(npcId);
                        world.MarkMacroRouteExecutionBlocked(npcId, duringLastMile: macroLastMile);

                        ArcontioLogger.Trace(
                            new LogContext(tick: (int)TickContext.CurrentTickIndex, channel: "Move", npcId: npcId, cell: (x, y)),
                            new LogBlock(LogLevel.Trace, "log.move.intent_cancelled_stuck")
                                .AddField("targetX", intent.TargetX)
                                .AddField("targetY", intent.TargetY)
                                .AddField("effectiveTargetX", effectiveTargetX)
                                .AddField("effectiveTargetY", effectiveTargetY)
                                .AddField("usingMacroImmediate", usingMacroImmediate)
                                .AddField("effectiveTargetHasDirectPath", effectiveTargetHasDirectPath)
                                .AddField("reason", intent.Reason.ToString())
                        );
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

        private static bool ShouldValidateTargetThisTick(Tick tick)
        {
            // Nota: Tick non è necessariamente un int; non conosciamo la tua implementazione.
            // Per evitare dipendenze, usiamo il TickContext globale che già usi per logging.
            // Se DefaultTargetValidateEveryTicks == 1 -> sempre true.
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
