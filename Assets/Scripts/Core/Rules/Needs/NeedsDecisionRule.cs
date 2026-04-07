using Arcontio.Core.Diagnostics;
using Arcontio.Core.Logging;
using System.Collections.Generic;
using UnityEngine;

namespace Arcontio.Core
{
    /// <summary>
    /// NeedsDecisionRule (Day9):
    /// Rule alto livello, coerente col tuo stile:
    /// - Reagisce a eventi (TickPulseEvent) e produce Commands.
    ///
    /// Decisione v0:
    /// - se hungry: privato -> stock community (VISIBILE) -> furto (se moralità/emergenza)
    /// - se tired: letto community libero (VISIBILE) -> letto altrui (se moralità/emergenza)
    ///
    /// IMPORTANTISSIMO (patch):
    /// In ARCONTIO la visibilità non è "telepatia".
    /// Se un oggetto è dietro un muro, la decisione *non deve* poterlo usare come se fosse noto.
    ///
    /// Per questo motivo qui applichiamo un filtro "Visible" minimale:
    /// - posizione NPC: world.GridPos[npcId]
    /// - posizione oggetto: world.Objects[objId].CellX/CellY
    /// - visibilità: world.HasLineOfSight(nx,ny,ox,oy) + range discreto
    ///
    /// Nota pragmatica:
    /// - NON applichiamo il cono FOV (orientamento) in questa rule,
    ///   perché per una decisione "mangio" spesso ti interessa la *conoscenza pratica* dell'oggetto
    ///   (es. lo hai visto un secondo fa, ti giri, ecc.).
    /// - Se in futuro vuoi coerenza totale col pipeline Range?Cone?LOS, il posto giusto
    ///   è far sì che NeedsDecisionRule consulti Memory/ObjectPerception, non il World "nudo".
    /// </summary>
    public sealed class NeedsDecisionRule : IRule
    {
        // Throttle: decidiamo ogni N tick-pulse (per log leggibile)
        private readonly int _decisionEveryTicks;

        // Range di ricerca "decisionale" per cibo/letto.
        // È volutamente conservativo: evita che un NPC "usi" risorse che stanno a metà mappa
        // solo perché la LOS non è bloccata (corridoio lungo, ecc.).
        private readonly int _maxSeekRangeCells;

        public NeedsDecisionRule(int decisionEveryTicks = 10, int maxSeekRangeCells = 8)
        {
            _decisionEveryTicks = Mathf.Max(1, decisionEveryTicks);
            _maxSeekRangeCells = Mathf.Max(1, maxSeekRangeCells);
        }

        public void Handle(World world, ISimEvent e, List<ICommand> outCommands, Telemetry telemetry)
        {
            // Usiamo TickPulseEvent come clock decisionale.
            if (e is not TickPulseEvent pulse)
                return;

            if ((pulse.TickIndex % _decisionEveryTicks) != 0)
                return;

            var cfg = world.Global.Needs;

            int ate = 0, slept = 0, antisocial = 0, moved = 0;

            foreach (var npcId in world.NpcDna.Keys)
            {
                if (!world.Needs.TryGetValue(npcId, out var needs)) continue;

                // --- MANGIA ---
                if (needs.GetValue(NeedKind.Hunger) >= cfg.hungryThreshold)
                {
                    if (TryPlanEatOrMove(world, npcId, in needs, (int)pulse.TickIndex, out var cmd, out bool didSteal, out bool didMove))
                    {
                        outCommands.Add(cmd);
                        if (didMove) moved++; else ate++;
                        if (didSteal) antisocial++;

                        ArcontioLogger.Info(
                            new LogContext(tick: (int)TickContext.CurrentTickIndex, channel: "NeedsDecisionRule"),
                            new LogBlock(LogLevel.Info, "log.needsconfig.Handle")
                                         .AddField("tick", pulse.TickIndex)
                                         .AddField("npcId", npcId)
                                         .AddField("Command", cmd.Name));


                        continue; // una sola azione per tick
                    }
                }

                // --- DORMI ---
                if (needs.GetValue(NeedKind.Rest) >= cfg.tiredThreshold)
                {
                    if (TryPlanSleep(world, npcId, needs, out var cmd, out bool didTrespass))
                    {
                        outCommands.Add(cmd);
                        slept++;
                        if (didTrespass) antisocial++;

                        ArcontioLogger.Info(
                            new LogContext(tick: (int)TickContext.CurrentTickIndex, channel: "NeedsDecisionRule"),
                            new LogBlock(LogLevel.Info, "log.needsconfig.Handle")
                                         .AddField("tick", pulse.TickIndex)
                                         .AddField("npcId", npcId)
                                         .AddField("Command", cmd.Name));

                        continue; // una sola azione per tick
                    }
                }
            }

            if (ate + slept + antisocial > 0)
            {
                ArcontioLogger.Info(
                new LogContext(tick: (int)TickContext.CurrentTickIndex, channel: "NeedsDecisionRule"),
                new LogBlock(LogLevel.Info, "log.needsconfig.Handle")
                    .AddField("tick=", pulse.TickIndex)
                    .AddField("ate==", ate)
                    .AddField("moved==", moved)
                    .AddField("antisocial==", antisocial));
            }
        }

        // ============================================================
        // EAT DECISION
        // ============================================================

        /// <summary>
        /// Day10: Eat OR Move OR Steal.
        ///
        /// Ordine decisionale (coerente con la policy di progetto):
        /// 1) cibo privato addosso
        /// 2) cibo community visibile: se sei sulla cella -> mangia, altrimenti -> muoviti
        /// 3) se NON esiste cibo legale e "okToSteal":
        ///    3a) stock privato a terra (OwnerKind=Npc, OwnerId!=me) visibile: se sei sulla cella -> ruba unità, altrimenti -> muoviti
        ///    3b) altrimenti prova furto "addosso" (NpcPrivateFood) come fallback
        ///
        /// Nota:
        /// - Questo metodo evita sia "mangiare a distanza" sia "rubare a distanza".
        /// - Il movimento viene espresso tramite SetMoveIntentCommand.
        ///   (Se nel tuo branch non esiste ancora MoveIntent, questo è il punto dove dovrai allineare i tipi.)
        /// </summary>
        private bool TryPlanEatOrMove(
            World world,
            int npcId,
            in Needs needs,
            int nowTick,
            out ICommand cmd,
            out bool didSteal,
            out bool didMove)
        {
            cmd = null;
            didSteal = false;
            didMove = false;

            // 1) privato
            if (world.NpcPrivateFood.TryGetValue(npcId, out int priv) && priv > 0)
            {
                cmd = new EatPrivateFoodCommand(npcId);
                return true;
            }

            
            // 1b) Patch 5.1 (revised): stock privato *mio* a terra (Pinned BELIEF, non memoria percettiva e non telepatia)
            //
            // Bug che correggiamo:
            // - Se un NPC ha cibo "privato" NON addosso (NpcPrivateFood=0) ma depositato a terra
            //   in uno stock OwnerKind=Npc, OwnerId=thisNpc, la logica precedente lo ignorava
            //   e l'NPC poteva morire di fame o passare direttamente a rubare.
            //
            // Policy ARCONTIO (manifesto):
            // - La decisione deve usare percezione/memoria soggettiva, NON scansioni globali.
            //
            // Regola fisica (richiesta utente):
            // - Per mangiare o rubare cibo a terra dallo stock, devi essere SULLA cella dello stock (co-locazione).
            // - Se non sei sulla cella, pianifica un MoveIntent verso l'ultima cella nota dello stock.
            int ownFoodObj = FindPinnedBelievedOwnNpcFoodStock(world, npcId, _maxSeekRangeCells, out int ox, out int oy);
            if (ownFoodObj != 0)
            {
                if (!TryGetNpcCell(world, npcId, out int nx, out int ny))
                    return false;

                if (nx == ox && ny == oy)
                {
                    // Sei arrivato nella cella dove *credi* di avere il tuo stock privato.
                    //
                    // Ora facciamo la verifica "locale" (non telepatica):
                    // - se lo stock esiste ancora IN QUELLA CELLA ed è effettivamente ancora tuo e non vuoto -> mangia.
                    // - altrimenti, significa che l'NPC ha appena scoperto una discrepanza (furto/distruzione/spostamento).
                    //   In questo caso invalidiamo la belief e proseguiamo con le alternative (community / furto).
                    int objAtCell = world.GetObjectAt(ox, oy);

                    bool foundMyStockHere = false;

                    if (objAtCell == ownFoodObj && world.FoodStocks.TryGetValue(ownFoodObj, out var st))
                    {
                        if (st.Units > 0 && st.OwnerKind == OwnerKind.Npc && st.OwnerId == npcId)
                        {
                            foundMyStockHere = true;
                        }
                    }

                    if (foundMyStockHere)
                    {
                        // Stock confermato sul posto: l'NPC può mangiare.
                        cmd = new EatFromStockCommand(npcId, ownFoodObj);
                        return true;
                    }

                    // Scoperta locale: lo stock non è dove doveva essere, o non è più mio.
                    // Aggiorniamo la belief pinned: da questo momento non lo considero più disponibile.
                    world.RemovePinnedFoodStockBelief(npcId, ownFoodObj);

                    // Nota:
                    // - Qui NON creiamo ancora un evento di "furto sospettato": sarebbe un sistema separato.
                    // - Per ora ci limitiamo a far sì che l'NPC non resti bloccato a cercare all'infinito.
                }

                // Non sei ancora arrivato: vai sulla cella ricordata dello stock.
                didMove = true;

                cmd = new SetMoveIntentCommand(npcId, new MoveIntent
                {
                    Active = true,
                    TargetX = ox,
                    TargetY = oy,
                    Reason = MoveIntentReason.SeekFood,
                    TargetObjectId = ownFoodObj
                });

                return true;
            }

// 2) stock community (visibile OR remembered)
            // -----------------------------------------------------------------
            // REGRESSION FIX (0.02.05.2b):
            // Nelle patch recenti abbiamo irrigidito molto la rule per evitare
            // telepatia sui food stock community. Il risultato collaterale, però,
            // è stato questo:
            // - se l'NPC VEDEVA uno stock community, lo registrava correttamente
            //   nella memoria oggetti;
            // - ma appena usciva dalla LOS corrente, la decisione smetteva di
            //   considerarlo un target valido, perché qui interrogavamo SOLO
            //   FindVisibleCommunityFoodStock(...).
            //
            // Questo inibiva un comportamento che in versioni precedenti era di
            // fatto possibile: "ho fame, so dove c'è cibo, quindi vado a prenderlo
            // anche se in questo tick non lo sto vedendo".
            //
            // La correzione qui sotto mantiene il vincolo anti-telepatia:
            // - PRIMA preferiamo uno stock realmente visibile ORA;
            // - SE non c'è nulla di visibile, usiamo uno stock community ricordato
            //   nella memoria soggettiva dell'NPC.
            //
            // Quindi la conoscenza torna ad essere operativa, ma senza tornare a
            // scandire il mondo globale come facevano le versioni "telepatiche".
            int foodObj = FindVisibleCommunityFoodStock(world, npcId, _maxSeekRangeCells);
            bool foodTargetFromMemory = false;
            int fx = 0, fy = 0;

            if (foodObj != 0)
            {
                if (!TryGetObjectCell(world, foodObj, out fx, out fy))
                    return false;
            }
            else
            {
                // Fallback memory-driven: uso SOLO ciò che l'NPC ricorda di avere visto.
                foodObj = FindRememberedCommunityFoodStock(world, npcId, _maxSeekRangeCells, out fx, out fy);
                foodTargetFromMemory = foodObj != 0;
            }

            if (foodObj != 0)
            {
                if (!TryGetNpcCell(world, npcId, out int nx, out int ny))
                    return false;

                if (nx == fx && ny == fy)
                {
                    // Arrivo sulla cella target.
                    // Se il target veniva dalla memoria, verifica locale: il cibo è ancora qui?
                    if (foodTargetFromMemory)
                    {
                        bool foodStillHere = world.FoodStocks.TryGetValue(foodObj, out var st)
                                            && st.Units > 0;

                        if (!foodStillHere)
                        {
                            // Scoperta locale: il cibo non c'è più.
                            // Invalida il memory slot — l'NPC sa ora che è obsoleto.
                            if (world.NpcObjectMemory.TryGetValue(npcId, out var mem) && mem != null)
                            {
                                for (int si = 0; si < mem.Slots.Length; si++)
                                {
                                    ref var slot = ref mem.Slots[si];
                                    if (!slot.IsValid) continue;
                                    int slotObjId = slot.SubjectId != 0 ? slot.SubjectId : slot.ObjectId;
                                    if (slotObjId == foodObj)
                                    {
                                        slot.IsValid = false;
                                        break;
                                    }
                                }
                            }

                            // Balloon debug: cibo non trovato dove ricordato.
                            world.EmitNpcBalloon(npcId, NpcBalloonKind.FoodNotFound);
                            return false;
                        }
                    }

                    // Cibo confermato (visibile o verificato localmente): mangia.
                    cmd = new EatFromStockCommand(npcId, foodObj);
                    return true;
                }

                didMove = true;

                // Fix stuck detection: se c'è già un MoveIntent attivo verso
                // le stesse coordinate, non sovrascriverlo. La Rule viene chiamata
                // ogni _decisionEveryTicks tick e senza questo check resetta
                // BlockedTicks ogni volta, impedendo allo stuck detection di
                // scattare quando il target è fisicamente irraggiungibile.
                if (world.NpcMoveIntents.TryGetValue(npcId, out var existingIntent)
                    && existingIntent.Active
                    && existingIntent.TargetX == fx
                    && existingIntent.TargetY == fy)
                {
                    // Intent già attivo verso lo stesso target: non sovrascrivere.
                    // MovementSystem gestirà lo stuck o troverà un percorso.
                    return true;
                }

                cmd = new SetMoveIntentCommand(npcId, new MoveIntent
                {
                    Active = true,
                    TargetX = fx,
                    TargetY = fy,
                    Reason = MoveIntentReason.SeekFood,
                    TargetObjectId = foodTargetFromMemory ? 0 : foodObj
                });

                // Nota molto utile per la lettura futura di questo ramo:
                // non cambia il tipo di comando, cambia solo la sorgente decisionale
                // del target. La card debug distinguerà poi Visible vs KnownObject.
                return true;
            }

            // 3) furto se moralità/emergenza
            float law = world.Social.TryGetValue(npcId, out var soc) ? soc.JusticePerception01 : 0.5f;
            bool emergency = needs.Hunger01 >= 0.95f;
            bool okToSteal = emergency || law < 0.45f;

            if (!okToSteal)
                return false;

            // 3a) Day10/Step5: furto da stock privato a terra (non addosso)
//
// CAMBIO ARCHITETTURALE (Step5):
// - Prima: FindVisibleOtherNpcFoodStock() scandiva world.FoodStocks => era "conoscenza globale".
// - Ora: scegliamo il target SOLO dalla memoria soggettiva dell'NPC (World.NpcObjectMemory[npcId]).
//
// Nota importante:
// - In execution (Step2) abbiamo già blindato il comando: il furto da stock è valido SOLO se sei sulla cella dello stock.
// - Qui, lato planning, facciamo la stessa cosa: se non sei sulla cella -> SetMoveIntentCommand.
int stolenStockObj = FindRememberedOtherNpcFoodStock(world, npcId, _maxSeekRangeCells, out int sx, out int sy, out int victimOwnerId);
if (stolenStockObj != 0)
{
    if (!TryGetNpcCell(world, npcId, out int nx, out int ny))
        return false;

    if (nx == sx && ny == sy)
    {
        // Ruba davvero solo se sei sullo stock (regola: stesso tile).
        didSteal = true;
        cmd = new StealFromStockCommand(npcId, stolenStockObj);
        return true;
    }

    // Altrimenti ti avvicini prima: niente furto "a distanza".
    didMove = true;
    didSteal = true; // stai pianificando un'azione antisociale

    cmd = new SetMoveIntentCommand(npcId, new MoveIntent
    {
        Active = true,
        TargetX = sx,
        TargetY = sy,
        Reason = MoveIntentReason.SeekFood,
        TargetObjectId = stolenStockObj
    });

    return true;
}
    // 3b) Step5: furto di cibo "addosso" (NPC -> NPC)
    //
    // CAMBIO ARCHITETTURALE (Step5):
    // - Prima: FindNpcWithPrivateFood() scandiva world.NpcPrivateFood => telepatia (conoscenza globale).
    // - Ora: scegliamo la vittima SOLO dalla memoria soggettiva:
    //   World.NpcObjectMemory[npcId] con entry Kind=Npc e flag "HasCarriedFood".
    //
    // Regola di interazione (design):
    // - Il furto "addosso" è valido solo se sei ADIACENTE (Manhattan=1) e senza occlusioni (LOS).
    // - Se non sei in range, devi prima muoverti verso la last-known cell della vittima.
    int victim = FindRememberedNpcWithCarriedFood(world, npcId, _maxSeekRangeCells, out int vx, out int vy, out int carriedApprox);
    if (victim != 0)
    {
        if (!TryGetNpcCell(world, npcId, out int nx, out int ny))
            return false;

        int manhattan = Mathf.Abs(vx - nx) + Mathf.Abs(vy - ny);

        // Se sei già vicino, prova il furto (execution farà comunque i check runtime Step2).
        if (manhattan == 1 && world.HasLineOfSight(nx, ny, vx, vy))
        {
            didSteal = true;
            cmd = new StealPrivateFoodCommand(npcId, victim);
            return true;
        }

        // Altrimenti: prima insegui la last-known cell della vittima.
        didMove = true;
        didSteal = true;

        cmd = new SetMoveIntentCommand(npcId, new MoveIntent
        {
            Active = true,
            TargetX = vx,
            TargetY = vy,
            Reason = MoveIntentReason.SeekFood,

            // Nota:
            // - MoveIntent oggi non ha TargetNpcId.
            // - NON usiamo TargetObjectId per non confondere i sistemi che assumono "oggetto nel mondo".
            TargetObjectId = 0
        });

        return true;
    }

    return false;
}
        /// <summary>
        /// FindRememberedCommunityFoodStock (0.02.05.2b):
        ///
        /// Scopo:
        /// - ripristinare il comportamento corretto "ho visto del cibo, quindi posso
        ///   tornarci anche se ora non lo sto guardando", SENZA reintrodurre telepatia.
        ///
        /// Principio architetturale:
        /// - NON scandiamo il mondo per cercare cibo community;
        /// - scandiamo invece la memoria soggettiva world.NpcObjectMemory[npcId].
        ///
        /// Validazione minima:
        /// - l'entry deve essere un WorldObject compatibile con un food stock;
        /// - l'oggetto reale, se ancora esiste, deve risultare uno stock community con units > 0;
        /// - usiamo la posizione reale se l'oggetto è ancora nel World, altrimenti la last-known cell.
        ///
        /// Questa validazione non è telepatia "forte":
        /// stiamo controllando lo stato dell'oggetto che l'NPC ricorda già, non stiamo
        /// cercando nuovi target globali fuori dalla sua conoscenza.
        /// </summary>
        private static int FindRememberedCommunityFoodStock(
            World world,
            int npcId,
            int maxRangeCells,
            out int sx,
            out int sy)
        {
            sx = 0;
            sy = 0;

            if (!TryGetNpcCell(world, npcId, out int nx, out int ny))
                return 0;

            if (!world.NpcObjectMemory.TryGetValue(npcId, out var mem) || mem == null)
                return 0;

            int bestObjId = 0;
            int bestDist = int.MaxValue;
            int bestX = 0;
            int bestY = 0;

            for (int i = 0; i < mem.Slots.Length; i++)
            {
                var e = mem.Slots[i];
                if (!e.IsValid)
                    continue;

                if (e.Kind != NpcObjectMemoryStore.SubjectKind.WorldObject)
                    continue;

                int objId = e.SubjectId != 0 ? e.SubjectId : e.ObjectId;
                if (objId == 0)
                    continue;

                // Filtro ownership da metadati memoria (scritti al momento della percezione).
                if (e.OwnerKind != OwnerKind.Community || e.OwnerId != 0)
                    continue;

                // Coordinate: usa quelle reali se l'oggetto esiste ancora,
                // altrimenti usa quelle ricordate (l'NPC non sa che è sparito).
                // NON saltiamo lo slot se l'oggetto non esiste più in FoodStocks:
                // l'NPC deve poter andare alle coordinate ricordate per scoprire
                // che il cibo non c'è più (scoperta locale all'arrivo).
                int ox = e.CellX;
                int oy = e.CellY;

                if (world.FoodStocks.TryGetValue(objId, out var st))
                {
                    // Oggetto esiste: filtra stock esauriti e aggiorna posizione reale.
                    if (st.Units <= 0)
                        continue;
                    if (world.Objects.TryGetValue(objId, out var inst) && inst != null)
                    {
                        ox = inst.CellX;
                        oy = inst.CellY;
                    }
                }
                // else: oggetto non in FoodStocks → usa coordinate di memoria.

                int manhattan = Mathf.Abs(ox - nx) + Mathf.Abs(oy - ny);
                if (manhattan > maxRangeCells)
                    continue;

                if (manhattan < bestDist)
                {
                    bestDist = manhattan;
                    bestObjId = objId;
                    bestX = ox;
                    bestY = oy;
                }
            }

            if (bestObjId != 0)
            {
                sx = bestX;
                sy = bestY;
            }

            return bestObjId;
        }

        /// <summary>
        /// Trova uno stock di cibo della Community che l'NPC può *realisticamente* usare:
        /// - lo stock deve avere Units > 0
        /// - deve essere OwnerKind=Community, OwnerId=0 (convenzione attuale)
        /// - deve essere "visibile" secondo un test minimo (range + LOS)
        ///
        /// Perché qui e non in World?
        /// - World è "verità oggettiva" e dovrebbe restare tendenzialmente neutro.
        /// - La nozione di "posso usarlo perché lo vedo" è una policy decisionale.
        /// </summary>
        private static int FindVisibleCommunityFoodStock(World world, int npcId, int maxRangeCells)
        {
            if (!TryGetNpcCell(world, npcId, out int nx, out int ny))
                return 0;

            foreach (var kv in world.FoodStocks)
            {
                int objId = kv.Key;
                var st = kv.Value;
                //if (st == null) continue;

                if (st.Units <= 0) continue;
                if (st.OwnerKind != OwnerKind.Community || st.OwnerId != 0) continue;

                // IMPORTANTISSIMO:
                // Le coordinate NON sono in FoodStockComponent.
                // Le coordinate stanno in world.Objects[objId].
                if (!TryGetObjectCell(world, objId, out int ox, out int oy))
                    continue;

                // Range discreto: Manhattan (cheap e coerente con grid).
                int manhattan = Mathf.Abs(ox - nx) + Mathf.Abs(oy - ny);
                if (manhattan > maxRangeCells)
                    continue;

                // LOS: se un muro è in mezzo, HasLineOfSight deve tornare false.
                if (!world.HasLineOfSight(nx, ny, ox, oy))
                    continue;

                return objId;
            }

            return 0;
        }

        
        /// <summary>
        /// Day10: trova uno stock di cibo privato (OwnerKind=Npc) appartenente ad un altro NPC,
        /// che sia visibile (range + LOS) e con Units > 0.
        ///
        /// Nota strategica:
        /// - In futuro questo non dovrebbe essere "scan globale world.FoodStocks",
        ///   ma una query su NpcObjectMemoryStore (conoscenza soggettiva).
        /// - Per il test Day10 (seed) va benissimo: vogliamo validare meccanica furto + witness.
        /// </summary>
        
        
/// <summary>
/// Step5: cerca nella MEMORIA soggettiva dell'NPC uno stock di cibo privato (OwnerKind=Npc)
/// appartenente ad un altro NPC.
///
/// Principio:
/// - la scelta del target deve essere memory-driven, non world-driven.
/// - quindi NON scandiamo world.FoodStocks; scandiamo invece world.NpcObjectMemory[npcId].
///
/// Nota sulla robustezza:
/// - per evitare target "fantasma", facciamo una validazione puntuale sull'ObjectId:
///   world.FoodStocks.TryGetValue(objId, out st) e Units>0.
/// - questa NON è telepatia: non stiamo "cercando" cibo nel mondo, stiamo solo verificando
///   se l'ID che ricordo esiste ancora ed è effettivamente uno stock di cibo.
///
/// Ritorna:
/// - objectId dello stock se trovato
/// - out sx/sy = cella target (preferiamo la cella reale dal World se disponibile, altrimenti memoria)
/// - out ownerId = npc "vittima" (proprietario dello stock)
/// </summary>

        /// <summary>
        /// Step5+Fix runtime:
        /// Trova, nella MEMORIA soggettiva dell'NPC, uno stock di cibo privato appartenente a SE STESSO
        /// (OwnerKind=Npc, OwnerId=npcId).
        ///
        /// Perché esiste:
        /// - Caso comune: l'NPC deposita il proprio cibo a terra (stock privato) e poi deve tornarci per mangiare.
        /// - Senza questo ramo, l'NPC ignora il proprio stock a terra e passa a community/steal, risultando illogico.
        ///
        /// Policy:
        /// - Memory-driven: nessuna scansione globale del mondo per scegliere "che cosa c'è in giro".
        /// - Validazione runtime: anche se la memoria è stale, in execution i comandi verificano lo stato reale.
        /// </summary>
        private static 
        /// <summary>
        /// FindPinnedBelievedOwnNpcFoodStock (Patch 5.1 - revised):
        ///
        /// Scopo:
        /// - trovare "il mio stock privato a terra" usando una lista PINNED di belief,
        ///   non dipendente dalla memoria percettiva e non dipendente da scansioni globali del World.
        ///
        /// Perché:
        /// - Bug: l'NPC poteva ignorare il suo stock privato a terra se non era dentro NpcObjectMemory.
        /// - Vincolo manifesto: l'NPC NON deve sapere automaticamente se qualcuno gli ruba lo stock fuori vista.
        ///
        /// Comportamento:
        /// - L'NPC usa solo le "last known coordinates" conservate nella belief.
        /// - Se è già arrivato in quella cella e lo stock non c'è (o non è più suo / è vuoto),
        ///   allora la belief viene invalidata (RemovePinnedFoodStockBelief) e la rule prosegue
        ///   con le alternative (community / furto).
        /// - Se NON è ancora arrivato, pianifichiamo MoveIntent verso quella cella per ispezione.
        /// </summary>
        int FindPinnedBelievedOwnNpcFoodStock(
            World world,
            int npcId,
            int maxRangeCells,
            out int bestX,
            out int bestY)
        {
            bestX = 0;
            bestY = 0;

            // Se non abbiamo pinned belief, non abbiamo nulla da pianificare.
            if (!world.NpcPinnedFoodStockBeliefs.TryGetValue(npcId, out var list) || list == null || list.Count == 0)
                return 0;

            if (!TryGetNpcCell(world, npcId, out int nx, out int ny))
                return 0;

            int bestObj = 0;
            int bestDist = int.MaxValue;

            // IMPORTANTISSIMO:
            // Qui NON consultiamo World.Objects[objId] per scoprire "dove sta davvero lo stock ora".
            // Quello sarebbe telepatia. Usiamo solo la posizione che l'NPC crede essere valida (LastKnownX/Y).
            for (int i = 0; i < list.Count; i++)
            {
                var b = list[i];
                if (!b.IsValid)
                    continue;

                int ox = b.LastKnownX;
                int oy = b.LastKnownY;

                int manhattan = Mathf.Abs(ox - nx) + Mathf.Abs(oy - ny);
                if (manhattan > maxRangeCells)
                    continue;

                if (manhattan < bestDist)
                {
                    bestDist = manhattan;
                    bestObj = b.ObjectId;
                    bestX = ox;
                    bestY = oy;
                }
            }

            return bestObj;
        }

private static int FindRememberedOtherNpcFoodStock(
    World world,
    int npcId,
    int maxRangeCells,
    out int sx,
    out int sy,
    out int ownerId)
{
    sx = 0;
    sy = 0;
    ownerId = 0;

    if (!TryGetNpcCell(world, npcId, out int nx, out int ny))
        return 0;

    if (!world.NpcObjectMemory.TryGetValue(npcId, out var mem) || mem == null)
        return 0;

    int bestObjId = 0;
    int bestDist = int.MaxValue;
    int bestX = 0;
    int bestY = 0;
    int bestOwner = 0;

    for (int i = 0; i < mem.Slots.Length; i++)
    {
        var e = mem.Slots[i];
        if (!e.IsValid) continue;

        if (e.Kind != NpcObjectMemoryStore.SubjectKind.WorldObject)
            continue;

        // Recuperiamo l'ObjectId in modo robusto (compat: SubjectId e ObjectId possono coincidere).
        int objId = e.SubjectId != 0 ? e.SubjectId : e.ObjectId;
        if (objId == 0) continue;

        if (!world.FoodStocks.TryGetValue(objId, out var st))
            continue;

        if (st.Units <= 0)
            continue;

        // Deve essere privato di un altro NPC.
        if (st.OwnerKind != OwnerKind.Npc) continue;
        if (st.OwnerId <= 0) continue;
        if (st.OwnerId == npcId) continue;

        // Prendiamo la cella reale se l'oggetto esiste in world.Objects, altrimenti la last-known cell in memoria.
        int ox = e.CellX;
        int oy = e.CellY;
        if (world.Objects.TryGetValue(objId, out var inst) && inst != null)
        {
            ox = inst.CellX;
            oy = inst.CellY;
        }

        int manhattan = Mathf.Abs(ox - nx) + Mathf.Abs(oy - ny);
        if (manhattan > maxRangeCells)
            continue;

        if (manhattan < bestDist)
        {
            bestDist = manhattan;
            bestObjId = objId;
            bestX = ox;
            bestY = oy;
            bestOwner = st.OwnerId;
        }
    }

    if (bestObjId != 0)
    {
        sx = bestX;
        sy = bestY;
        ownerId = bestOwner;
    }

    return bestObjId;
}

/// <summary>
/// Step5: cerca nella MEMORIA soggettiva un NPC osservato che (secondo il ricordo)
/// aveva cibo addosso.
///
/// Importante:
/// - questa funzione NON guarda world.NpcPrivateFood per scegliere la vittima.
/// - seleziona la vittima da mem.Slots (Kind=Npc) e flag HasCarriedFood.
///
/// Ritorna:
/// - victimNpcId
/// - out vx/vy = last-known cell della vittima (o last seen)
/// - out carriedApprox = stima (debug/UI), non vincolante.
/// </summary>
private static int FindRememberedNpcWithCarriedFood(
    World world,
    int npcId,
    int maxRangeCells,
    out int vx,
    out int vy,
    out int carriedApprox)
{
    vx = 0;
    vy = 0;
    carriedApprox = 0;

    if (!TryGetNpcCell(world, npcId, out int nx, out int ny))
        return 0;

    if (!world.NpcObjectMemory.TryGetValue(npcId, out var mem) || mem == null)
        return 0;

    int bestVictim = 0;
    int bestDist = int.MaxValue;
    int bestX = 0;
    int bestY = 0;
    int bestApprox = 0;

    for (int i = 0; i < mem.Slots.Length; i++)
    {
        var e = mem.Slots[i];
        if (!e.IsValid) continue;
        if (e.Kind != NpcObjectMemoryStore.SubjectKind.Npc)
            continue;

        int victimId = e.SubjectId;
        if (victimId <= 0) continue;
        if (victimId == npcId) continue;

        // Flag "has carried food" (derivato solo quando l'NPC era visibile: Step4).
        if ((e.Flags & NpcObjectMemoryStore.ObservedFlags.HasCarriedFood) == 0)
            continue;

        // Se la stima è 0, consideriamolo "non interessante" per il furto.
        // (In futuro potresti scegliere di rubare comunque, ma qui usiamo una regola semplice.)
        if (e.CarriedFoodUnitsApprox <= 0)
            continue;

        int ox = e.CellX;
        int oy = e.CellY;

        int manhattan = Mathf.Abs(ox - nx) + Mathf.Abs(oy - ny);
        if (manhattan > maxRangeCells)
            continue;

        if (manhattan < bestDist)
        {
            bestDist = manhattan;
            bestVictim = victimId;
            bestX = ox;
            bestY = oy;
            bestApprox = e.CarriedFoodUnitsApprox;
        }
    }

    if (bestVictim != 0)
    {
        vx = bestX;
        vy = bestY;
        carriedApprox = bestApprox;
    }

    return bestVictim;
}
// ============================================================
        // SLEEP DECISION
        // ============================================================

        private bool TryPlanSleep(World world, int npcId, Needs needs, out ICommand cmd, out bool didTrespass)
        {
            cmd = null;
            didTrespass = false;

            // 1) letto community libero (VISIBILE)
            int bedCommunity = FindVisibleBed(world, npcId, OwnerKind.Community, 0, _maxSeekRangeCells);
            if (bedCommunity != 0 && !world.GetUseStateOrDefault(bedCommunity).IsInUse)
            {
                cmd = new SleepInBedCommand(npcId, bedCommunity, "Community");
                return true;
            }

            // 2) letto altrui se moralità/emergenza
            float law = world.Social.TryGetValue(npcId, out var soc) ? soc.JusticePerception01 : 0.5f;
            bool emergency = needs.Fatigue01 >= 0.95f;
            bool okToTrespass = emergency || law < 0.45f;

            if (!okToTrespass)
                return false;

            int bedOther = FindAnyOwnedBedNotNpc(world, npcId, _maxSeekRangeCells);
            if (bedOther != 0 && !world.GetUseStateOrDefault(bedOther).IsInUse)
            {
                didTrespass = true;
                cmd = new SleepInBedCommand(npcId, bedOther, "Trespass");
                return true;
            }

            return false;
        }

        private static int FindVisibleBed(World world, int npcId, OwnerKind ownerKind, int ownerId, int maxRangeCells)
        {
            if (!TryGetNpcCell(world, npcId, out int nx, out int ny))
                return 0;

            foreach (var kv in world.Objects)
            {
                int objId = kv.Key;
                var obj = kv.Value;
                if (obj == null) continue;

                // v0: un letto se defId contiene "bed" (manteniamo la regola originale del file).
                if (string.IsNullOrWhiteSpace(obj.DefId)) continue;
                if (!obj.DefId.Contains("bed")) continue;

                if (obj.OwnerKind != ownerKind || obj.OwnerId != ownerId) continue;

                int ox = obj.CellX;
                int oy = obj.CellY;

                int manhattan = Mathf.Abs(ox - nx) + Mathf.Abs(oy - ny);
                if (manhattan > maxRangeCells)
                    continue;

                if (!world.HasLineOfSight(nx, ny, ox, oy))
                    continue;

                return objId;
            }

            return 0;
        }

        private static int FindAnyOwnedBedNotNpc(World world, int npcId, int maxRangeCells)
        {
            if (!TryGetNpcCell(world, npcId, out int nx, out int ny))
                return 0;

            foreach (var kv in world.Objects)
            {
                int objId = kv.Key;
                var obj = kv.Value;
                if (obj == null) continue;

                // v0: un letto se defId contiene "bed" (manteniamo la regola originale del file).
                if (string.IsNullOrWhiteSpace(obj.DefId)) continue;
                if (!obj.DefId.Contains("bed")) continue;

                // Escludi letti di proprietà dell'NPC.
                if (obj.OwnerKind == OwnerKind.Npc && obj.OwnerId == npcId) continue;

                int ox = obj.CellX;
                int oy = obj.CellY;

                int manhattan = Mathf.Abs(ox - nx) + Mathf.Abs(oy - ny);
                if (manhattan > maxRangeCells)
                    continue;

                if (!world.HasLineOfSight(nx, ny, ox, oy))
                    continue;

                return objId;
            }

            return 0;
        }

        // ============================================================
        // VERY SMALL UTILITIES (deliberatamente verbose)
        // ============================================================

        /// <summary>
        /// Estrae la cella corrente dell'NPC.
        /// In ARCONTIO la posizione runtime degli NPC è in world.GridPos (component store),
        /// NON dentro NpcCore (che è più "identità"/stato logico).
        /// </summary>
        private static bool TryGetNpcCell(World world, int npcId, out int x, out int y)
        {
            x = 0;
            y = 0;

            if (!world.GridPos.TryGetValue(npcId, out var pos))
                return false;

            x = pos.X;
            y = pos.Y;
            return true;
        }

        /// <summary>
        /// Estrae la cella di un oggetto dato il suo objectId.
        /// Nota: questa è la *singola fonte di verità* per posizione oggetti in World:
        /// world.Objects[objId].CellX / CellY.
        ///
        /// (Il FoodStockComponent NON contiene necessariamente coordinate; è un componente logico.)
        /// </summary>
        private static bool TryGetObjectCell(World world, int objectId, out int x, out int y)
        {
            x = 0;
            y = 0;

            if (!world.Objects.TryGetValue(objectId, out var obj) || obj == null)
                return false;

            x = obj.CellX;
            y = obj.CellY;
            return true;
        }
    }
}
