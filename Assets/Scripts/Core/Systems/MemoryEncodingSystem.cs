using Arcontio.Core.Diagnostics;
using System;
using System.Collections.Generic;
using UnityEngine;

namespace Arcontio.Core
{
    /// <summary>
    /// MemoryEncodingSystem:
    /// - prende gli eventi del tick (buffer)
    /// - decide per quali NPC l'evento è percepito (testimoni)  <-- QUI applichiamo CONO+LOS
    /// - per ciascun testimone applica IMemoryRule e aggiunge trace nel MemoryStore
    ///
    /// Nota architetturale:
    /// - Questo system NON legge il MessageBus direttamente (coda).
    /// - Gli eventi vengono "drainati" in StepOneTick e passati qui tramite SetEventsBuffer.
    /// </summary>
    public sealed class MemoryEncodingSystem : ISystem
    {
        public int Period => 1;

        private readonly List<IMemoryRule> _rules = new();

        // Buffer di eventi del tick (riusato, assegnato dal SimulationHost)
        private List<ISimEvent> _eventsBuffer;

        // Buffer ids NPC per iterazione
        private readonly List<int> _npcIds = new(2048);

        public MemoryEncodingSystem()
        {
            // Catalogo minimo rules (espandibile)
            _rules.Add(new PredatorSpottedMemoryRule());
            _rules.Add(new AttackWitnessedMemoryRule());
            _rules.Add(new DeathWitnessedMemoryRule());

            // Oggetti visti -> memoria
            _rules.Add(new ObjectSpottedMemoryRule());
            
            // NPC visti -> memoria
            _rules.Add(new NpcSpottedMemoryRule());

            // Furto cibo (Day9)
            _rules.Add(new FoodStolenMemoryRule());

            // Se hai una rule per FoodMissingSuspectedEvent, aggiungila qui.
            // _rules.Add(new FoodMissingSuspectedMemoryRule());
        }

        /// <summary>
        /// Il SimulationHost assegna qui la lista di eventi drainata dal bus.
        /// </summary>
        public void SetEventsBuffer(List<ISimEvent> eventsBuffer)
        {
            _eventsBuffer = eventsBuffer;
        }

        public void Update(World world, Tick tick, MessageBus bus, Telemetry telemetry)
        {
            if (_eventsBuffer == null || _eventsBuffer.Count == 0)
                return;

            // Snapshot parametri percezione dal GlobalState:
            int visionRange = world.Global.NpcVisionRangeCells;
            if (visionRange <= 0) visionRange = 6;

            // Cono:
            // Nel tuo progetto hai sia NpcVisionUseCone/NpcVisionConeSlope
            // sia NpcVisionConeHalfWidthPerStep. Per evitare ambiguità,
            // usiamo: (A) UseCone toggle + (B) ConeSlope come ampiezza.
            bool useCone = world.Global.NpcVisionUseCone;
            float coneHalfWidthPerStep = world.Global.NpcVisionConeSlope;
            if (coneHalfWidthPerStep < 0f) coneHalfWidthPerStep = 0f;

            // LOS:
            // Riutilizzo il toggle già esistente (EnableTokenLOS) per non introdurre un nuovo flag.
            // Se vuoi separare le due cose: aggiungi GlobalState.NpcVisionUseLOS.
            bool useLos = world.Global.EnableTokenLOS;

            // Preleva lista NPC
            _npcIds.Clear();
            _npcIds.AddRange(world.NpcCore.Keys);

            int tracesAdded = 0;

            // Per ogni evento, proviamo a creare memorie per i testimoni.
            for (int eIdx = 0; eIdx < _eventsBuffer.Count; eIdx++)
            {
                var e = _eventsBuffer[eIdx];

                for (int r = 0; r < _rules.Count; r++)
                {
                    var rule = _rules[r];
                    if (!rule.Matches(e))
                        continue;

                    // ? FIX CS0136: evX/evY invece di ex/ey
                    if (!TryGetEventCell(e, out int evX, out int evY))
                        break; // evento senza cella -> non possiamo decidere testimoni in v0

                    for (int n = 0; n < _npcIds.Count; n++)
                    {
                        int npcId = _npcIds[n];

                        if (!world.GridPos.TryGetValue(npcId, out var p))
                            continue;

                        int dist = Manhattan(p.X, p.Y, evX, evY);

                        if (dist > visionRange)
                            continue;

                        // ? CONO (orientamento) per tutti gli eventi con cella
                        if (useCone)
                        {
                            if (!world.NpcFacing.TryGetValue(npcId, out var facing))
                                facing = CardinalDirection.North;

                            if (!IsInCone(p.X, p.Y, facing, evX, evY, coneHalfWidthPerStep))
                                continue;
                        }

                        // ? LOS per tutti gli eventi con cella
                        // Nota: se vuoi un toggle globale, puoi usare world.Global.EnableTokenLOS oppure creare EnableNpcVisionLOS.
                        // Qui assumo: BlocksVision + VisionCost>=1 => blocca.
                        if (useLos)
                        {
                            if (HasBlockingLOS(world, p.X, p.Y, evX, evY))
                                continue;
                        }

                        float quality = 1f - (dist / (float)visionRange);
                        if (quality < 0.05f) quality = 0.05f;

                        // ============================================================
                        // NPC BALLOON SIGNALS (view)
                        // ============================================================
                        // UX requirement:
                        // - If an NPC witnesses a theft or suffers it (and perceives it), the view should be
                        //   able to show a dedicated balloon above the NPC.
                        //
                        // Architectural reason:
                        // - Only here we know the real witnesses (range + cone + LOS).
                        // - So this is the correct point to emit the signal.
                        //
                        // NOTE:
                        // - This does NOT change the simulation state; it only writes an observability store
                        //   (World.NpcBalloonSignals).
                        if (e is FoodStolenEvent fe)
                        {
                            if (npcId == fe.VictimNpcId)
                            {
                                // Victim that *sees* the theft (otherwise it would not pass perception filters above).
                                world.EmitNpcBalloon(npcId, NpcBalloonKind.TheftSuffered, subjectId: fe.ThiefNpcId, secondarySubjectId: fe.VictimNpcId);

                                // Patch 0.01P3 extension:
                                // COMMUNICAZIONE del furto (vittima).
                                // Se la vittima percepisce il furto, può diffondere un report ad altri NPC.
                                // Nota architetturale:
                                // - NON pubblichiamo direttamente su TokenBus (qui non abbiamo accesso).
                                // - Accodiamo su World.QueueTokenOut(...).
                                // - SimulationHost flush-a e poi Delivery/Assimilation fanno il resto.
                                QueueTheftCommunicationTokens(
                                    world,
                                    tick,
                                    speakerId: npcId,
                                    roleIsVictim: true,
                                    thiefNpcId: fe.ThiefNpcId,
                                    victimNpcId: fe.VictimNpcId,
                                    cellX: fe.CellX,
                                    cellY: fe.CellY,
                                    baseQuality01: quality);
                            }
                            else
                            {
                                // Third-party witness.
                                world.EmitNpcBalloon(npcId, NpcBalloonKind.TheftWitnessed, subjectId: fe.ThiefNpcId, secondarySubjectId: fe.VictimNpcId);

                                // Patch 0.01P3 extension:
                                // COMMUNICAZIONE del furto (testimone).
                                QueueTheftCommunicationTokens(
                                    world,
                                    tick,
                                    speakerId: npcId,
                                    roleIsVictim: false,
                                    thiefNpcId: fe.ThiefNpcId,
                                    victimNpcId: fe.VictimNpcId,
                                    cellX: fe.CellX,
                                    cellY: fe.CellY,
                                    baseQuality01: quality);
                            }
                        }

                        // ============================================================
                        // DAY10+: Observed-entity memory store (NpcObjectMemoryStore generalized)
                        // ============================================================
                        // Nota (molto verbosa ma importante):
                        // Questo blocco NON sostituisce le MemoryTrace narrative.
                        // Serve a mantenere una lista compatta di entità conosciute (oggetti + NPC) (cibo/letto/etc.)
                        // per evitare sia:
                        // - telepatia (scansione globale di world.Objects)
                        // - polling costoso (ricerche ripetute ogni tick)
                        //
                        // Lo aggiorniamo SOLO se questo npcId è un testimone valido dellevento
                        // (range + cono + LOS già verificati sopra).
                        TryUpsertObservedEntityMemoryFromSpotted(world, npcId, e, quality, tick);

                        telemetry.Counter("MemoryEncodingSystem.TracesEncodedAttempts", 1);

                        if (rule.TryEncode(world, npcId, e, quality, out var trace))
                        {
                            var res = world.Memory[npcId].AddOrMerge(trace);

                            switch (res)
                            {
                                case AddOrMergeResult.Inserted:
                                    telemetry.Counter("MemoryEncodingSystem.TracesActuallyInserted", 1);
                                    break;
                                case AddOrMergeResult.Replaced:
                                    telemetry.Counter("MemoryEncodingSystem.TracesActuallyInserted", 1);
                                    telemetry.Counter("MemoryEncodingSystem.TracesReplaced", 1);
                                    break;
                                case AddOrMergeResult.Reinforced:
                                    telemetry.Counter("MemoryEncodingSystem.TracesReinforced", 1);
                                    break;
                                case AddOrMergeResult.Dropped:
                                    telemetry.Counter("MemoryEncodingSystem.TracesDropped", 1);
                                    break;
                            }

                            tracesAdded++;
                        }
                    }

                    break; // una rule per evento
                }
            }

            telemetry.Counter("MemoryEncodingSystem.TracesAdded", tracesAdded);
        }

        private static int Manhattan(int ax, int ay, int bx, int by)
        {
            int dx = ax - bx; if (dx < 0) dx = -dx;
            int dy = ay - by; if (dy < 0) dy = -dy;
            return dx + dy;
        }

        // ============================================================
        // Patch 0.01P3 extension: Theft communication (event-driven tokens)
        // ============================================================

        /// <summary>
        /// QueueTheftCommunicationTokens:
        /// Accoda su World dei TokenEnvelope che rappresentano il "report" del furto.
        ///
        /// Perché qui (e non altrove):
        /// - MemoryEncodingSystem è l'unico punto in cui sappiamo con certezza chi è un testimone valido
        ///   (range + cono + LOS già applicati).
        /// - Se un NPC non percepisce il furto, NON dovrebbe poterlo comunicare.
        ///
        /// Perché accodiamo (e non pubblichiamo subito):
        /// - Questo System non riceve TokenBus.
        /// - Il flush viene fatto dal SimulationHost in un punto centrale, mantenendo la dipendenza corretta.
        ///
        /// Scelte v0:
        /// - Canale: AlarmShout (è una notizia "urgente" e deve poter attraversare corridoi con BFS).
        /// - Destinatari: tutti gli altri NPC (v0 semplice). TokenDeliveryPipeline filtrerà per range.
        ///   (In futuro: bounding box / area query / social network).
        ///
        /// Contenuto token:
        /// - SubjectId = thiefNpcId
        /// - SecondarySubjectId = victimNpcId
        /// - Cell = cellX/cellY (luogo del furto)
        /// </summary>
        private void QueueTheftCommunicationTokens(
            World world,
            Tick tick,
            int speakerId,
            bool roleIsVictim,
            int thiefNpcId,
            int victimNpcId,
            int cellX,
            int cellY,
            float baseQuality01)
        {
            if (world == null) return;

            // Determiniamo tipo token in base al ruolo dello speaker.
            TokenType ttype = roleIsVictim ? TokenType.TheftReportVictim : TokenType.TheftReportWitness;

            // Intensità e reliability di base:
            // - le moduleremo leggermente su quality (perché quality include distanza/cone/LOS).
            // - manteniamo comunque un minimo, perché "furto" è un evento importante.
            float q = baseQuality01;
            if (q < 0.05f) q = 0.05f;
            if (q > 1f) q = 1f;

            float intensity = roleIsVictim
                ? Mathf.Clamp01(0.75f + 0.25f * q)   // vittima: più "emotivo" => intensità alta
                : Mathf.Clamp01(0.60f + 0.30f * q);  // testimone: un filo meno

            float reliability = Mathf.Clamp01(0.60f + 0.40f * q);

            var token = new SymbolicToken(
                type: ttype,
                subjectId: thiefNpcId,
                intensity01: intensity,
                reliability01: reliability,
                chainDepth: 0,
                hasCell: true,
                cellX: cellX,
                cellY: cellY,
                secondarySubjectId: victimNpcId);

            // Broadcast v0: 1 envelope per NPC listener.
            // Nota performance:
            // - È O(N) per report.
            // - In 0.01 con poche decine/centinaia di NPC è ok.
            // - TokenDeliveryPipeline farà drop per range.
            for (int i = 0; i < _npcIds.Count; i++)
            {
                int listenerId = _npcIds[i];
                if (listenerId == speakerId)
                    continue;

                // Envelope 1:1
                world.QueueTokenOut(new TokenEnvelope(
                    speakerId: speakerId,
                    listenerId: listenerId,
                    channel: TokenChannel.AlarmShout,
                    tickIndex: tick.Index,
                    token: token));
            }
        }

        // ============================================================
        // DAY10: Object-memory store helpers
        // ============================================================

        /// <summary>
        /// Se levento è ObjectSpottedEvent, aggiorna lo store ad-hoc NpcObjectMemoryStore del testimone.
        /// Questo è il punto di integrazione perception ? eventi ? conoscenza.
        /// </summary>
        private static void TryUpsertObservedEntityMemoryFromSpotted(World world, int witnessNpcId, ISimEvent e, float reliability01, Tick tick)
        {
            // Fast-dispatch:
            // - Day10 origin: questo helper nasceva solo per ObjectSpottedEvent.
            // - Day10+ (Step4): lo estendiamo anche a NpcSpottedEvent, perché ora lo store
            //   è "Observed entities" (oggetti + NPC).
            if (e is NpcSpottedEvent ne)
            {
                // Safety: NPC witness esiste?
                if (!world.ExistsNpc(witnessNpcId))
                    return;

                // Safety: store esiste? (difensivo: anche se CreateNpc lo dovrebbe creare)
                if (!world.NpcObjectMemory.TryGetValue(witnessNpcId, out var storeNpc) || storeNpc == null)
                {
                    int cap = world.Global.NpcObjectMemorySlots;
                    if (cap <= 0) cap = 24;
                    storeNpc = new NpcObjectMemoryStore(cap);
                    world.NpcObjectMemory[witnessNpcId] = storeNpc;
                }

                // Tick: tick.Index è long. In Day10 per semplicità castiamo a int.
                int nowTickNpc = (int)tick.Index;

                // Utility: per Step4 è una euristica minimale.
                // In futuro: un NPC potrebbe essere più/meno "utile" a seconda di goals, relazioni sociali, etc.
                float utility01Npc = 0.50f;

                // Facts osservabili:
                // Qui NON stiamo facendo "telepatia", perché questo helper viene chiamato SOLO
                // se witnessNpcId è stato già validato come testimone (range + cono + LOS).
                // NOTA:
                // ObservedFlags è definito come enum nested dentro NpcObjectMemoryStore.
                // Qui lo qualifichiamo esplicitamente per evitare ambiguità di namespace.
                NpcObjectMemoryStore.ObservedFlags flags = NpcObjectMemoryStore.ObservedFlags.None;
                int carriedFoodApprox = 0;

                if (world.NpcPrivateFood.TryGetValue(ne.ObservedNpcId, out int realFood) && realFood > 0)
                {
                    flags |= NpcObjectMemoryStore.ObservedFlags.HasCarriedFood;

                    // In Step4 usiamo una approssimazione banale = valore reale.
                    // In futuro potresti:
                    // - quantizzare (1, 2-3, 4+)
                    // - degradare nel tempo
                    // - dipendere dalla qualità witnessQuality01
                    carriedFoodApprox = realFood;
                }

                storeNpc.UpsertNpc(
                    nowTick: nowTickNpc,
                    npcIdObserved: ne.ObservedNpcId,
                    x: ne.CellX,
                    y: ne.CellY,
                    reliability01: reliability01,
                    utility01: utility01Npc,
                    flags: flags,
                    carriedFoodUnitsApprox: carriedFoodApprox
                );

                return;
            }

            if (e is not ObjectSpottedEvent ev)
                return;

            // Safety: NPC esiste?
            if (!world.ExistsNpc(witnessNpcId))
                return;

            // Safety: store esiste? (difensivo: anche se CreateNpc lo dovrebbe creare)
            if (!world.NpcObjectMemory.TryGetValue(witnessNpcId, out var store) || store == null)
            {
                int cap = world.Global.NpcObjectMemorySlots;
                if (cap <= 0) cap = 24;
                store = new NpcObjectMemoryStore(cap);
                world.NpcObjectMemory[witnessNpcId] = store;
            }

            // Owner info: non è nellevento (perché levento è cosa è stato visto),
            // quindi lo recuperiamo dalla fonte di verità world.Objects.
            OwnerKind ownerKind = OwnerKind.None;
            int ownerId = -1;

            if (world.Objects.TryGetValue(ev.ObjectId, out var obj) && obj != null)
            {
                ownerKind = obj.OwnerKind;
                ownerId = obj.OwnerId;
            }

            // Utility: euristica minima per Day10.
            // In futuro questa dovrebbe dipendere da Needs/Goal dellNPC e non essere hardcoded.
            float utility01 = EstimateUtility01(ev.DefId);

            // Tick: tick.Index è long. In Day10 per semplicità castiamo a int.
            // (Nel lungo periodo, se la simulazione dura mesi di tick, conviene passare a long.)
            int nowTick = (int)tick.Index;

            // pinIfOwnedByNpc:
            // - se loggetto è owned-by-npc e ownerId == witnessNpcId, lo pinniamo (non viene evicted facilmente).
            // - questo evita di dimenticare il proprio letto / le proprie risorse principali.
            bool pinIfOwnedByNpc = true;

            // Upsert = Update+Insert:
            // - se già presente: refresh lastSeen/pos/reliability
            // - se non presente: inserisci o rimpiazza entry peggiore
            store.UpsertWorldObject(
                nowTick,
                ev.DefId,
                ev.ObjectId,
                ev.CellX, ev.CellY,
                ownerKind,
                ownerId,
                reliability01,
                utility01,
                pinIfOwnedByNpc,
                witnessNpcId
            );
        }

        /// <summary>
        /// Euristica minimale di quanto è utile un oggetto per la decisione.
        /// Per Day10 basta distinguere cibo/letto; tutto il resto è medio-basso.
        /// </summary>
        private static float EstimateUtility01(string defId)
        {
            if (string.IsNullOrEmpty(defId))
                return 0.25f;

            // Nota: per v0.01 usiamo contains su DefId.
            // In futuro: object def tags (Food/Bed/Workstation/...), non string matching.
            string s = defId.ToLowerInvariant();

            if (s.Contains("food"))
                return 1.00f;

            if (s.Contains("bed"))
                return 0.90f;

            if (s.Contains("door"))
                return 0.60f;

            return 0.35f;
        }

        /// <summary>
        /// IsInCone:
        /// Cono in griglia deterministico, basato su:
        /// - forward: quanto è davanti (deve essere > 0)
        /// - side: quanto è laterale (|side| <= forward * coneHalfWidthPerStep)
        ///
        /// coneHalfWidthPerStep:
        /// - 0.0  => solo linea frontale
        /// - 0.5  => cono stretto
        /// - 1.0  => cono ampio (?45° su griglia)
        /// </summary>
        private static bool IsInCone(int sx, int sy, CardinalDirection facing, int tx, int ty, float coneHalfWidthPerStep)
        {
            int dx = tx - sx;
            int dy = ty - sy;

            int forward, side;

            switch (facing)
            {
                case CardinalDirection.North:
                    forward = dy;
                    side = dx;
                    break;

                case CardinalDirection.South:
                    forward = -dy;
                    side = dx;
                    break;

                case CardinalDirection.East:
                    forward = dx;
                    side = dy;
                    break;

                case CardinalDirection.West:
                    forward = -dx;
                    side = dy;
                    break;

                default:
                    forward = dy;
                    side = dx;
                    break;
            }

            // Deve essere davanti
            if (forward <= 0) return false;

            // side <= forward * slope
            float limit = forward * coneHalfWidthPerStep;
            if (side < 0) side = -side;

            return side <= limit + 0.0001f;
        }

        /// <summary>
        /// LOS blocking:
        /// true se una cella tra start e target blocca la visione.
        /// </summary>
        private static bool HasBlockingLOS(World world, int x0, int y0, int x1, int y1)
        {
            // Bresenham grid LOS
            int dx = Math.Abs(x1 - x0);
            int dy = Math.Abs(y1 - y0);

            int sx = x0 < x1 ? 1 : -1;
            int sy = y0 < y1 ? 1 : -1;

            int err = dx - dy;

            int x = x0;
            int y = y0;

            while (true)
            {
                // Non bloccare la cella di partenza, ma blocca celle intermedie.
                if (!(x == x0 && y == y0))
                {
                    if (world.BlocksVisionAt(x, y))
                        return true;
                }

                if (x == x1 && y == y1)
                    return false;

                int e2 = 2 * err;
                if (e2 > -dy) { err -= dy; x += sx; }
                if (e2 < dx) { err += dx; y += sy; }
            }
        }

        private static bool TryGetEventCell(ISimEvent e, out int x, out int y)
        {
            x = 0; y = 0;

            switch (e)
            {
                case PredatorSpottedEvent pe:
                    x = pe.CellX; y = pe.CellY;
                    return true;

                case AttackEvent ae:
                    x = ae.CellX; y = ae.CellY;
                    return true;

                case DeathEvent de:
                    x = de.CellX; y = de.CellY;
                    return true;

                case ObjectSpottedEvent oe:
                    x = oe.CellX; y = oe.CellY;
                    return true;

                case NpcSpottedEvent ne:
                    x = ne.CellX; y = ne.CellY;
                    return true;

                case FoodStolenEvent fe:
                    x = fe.CellX; y = fe.CellY;
                    return true;
            }

            return false;
        }
    }
}
