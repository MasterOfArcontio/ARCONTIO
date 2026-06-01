using System;
using System.Collections.Generic;
using UnityEngine;
using Arcontio.Core.Diagnostics;

namespace Arcontio.Core
{
    /// <summary>
    /// TokenEmissionPipeline:
    /// Trasforma MemoryTrace -> TokenEnvelope quando due NPC sono in contatto.
    ///
    /// Perché non è un ISystem:
    /// - ISystem.Update(...) non riceve TokenBus.
    /// - Con pipe separate (B), l'orchestrazione deve essere fatta dal SimulationHost.
    /// </summary>
    public sealed class TokenEmissionPipeline
    {
        private readonly List<ITokenEmissionRule> _rules = new();

        // Buffers riusabili (zero alloc per tick)
        private readonly List<int> _npcIds = new(2048);
        private readonly List<MemoryTrace> _topTraces = new(32);

        // Rate limit state
        private readonly Dictionary<int, int> _tokensEmittedToday = new();   // speaker -> count
        private readonly Dictionary<ShareKey, long> _lastShareTick = new();  // cooldown
        private readonly List<ShareKey> _lastShareTickPruneBuffer = new(64);

        // Parametri "base"
        private readonly int _contactRadius;
        private readonly int _topN;

        public TokenEmissionPipeline(int contactRadius = 1, int topN = 6)
        {
            _contactRadius = Math.Max(1, contactRadius);
            _topN = Math.Max(1, topN);

            // Rules minime (roadmap)
            _rules.Add(new PredatorAlertEmissionRule());
            _rules.Add(new HelpRequestEmissionRule());
            _rules.Add(new NeedsObservationEmissionRule());
        }

        /// <summary>
        /// Emette token sul TokenBus, in base alle memorie attuali.
        /// </summary>
        public void Emit(World world, Tick tick, TokenBus tokenBus, Telemetry telemetry)
        {
            var costObserver = world.RuntimeCostObserver;
            bool costSample = costObserver != null && costObserver.ShouldSample(tick.Index);
            bool costPerNpc = costSample && costObserver.TrackPerNpc;
            long costStart = costSample ? costObserver.BeginSample() : 0L;
            int costPairChecks = 0;

            // Parametri da World (configurabili)
            int maxPerEncounter = world.Global.MaxTokensPerEncounter;
            if (maxPerEncounter <= 0) maxPerEncounter = 1;

            int maxPerDay = world.Global.MaxTokensPerNpcPerDay;
            if (maxPerDay <= 0) maxPerDay = 4;

            int cooldownTicks = world.Global.RepeatShareCooldownTicks;
            if (cooldownTicks < 0) cooldownTicks = 0;

            PruneExpiredShareCooldowns(tick.Index, cooldownTicks);

            // Reset "giornaliero" semplice:
            // Assunzione: 1 tick = 1 minuto => 1440 tick = 1 giorno.
            // È un placeholder che sostituirai con un calendario vero.
            if (tick.Index % 1440 == 0)
                _tokensEmittedToday.Clear();

            _npcIds.Clear();
            _npcIds.AddRange(world.NpcDna.Keys);

            int envelopesEmitted = 0;

            // Contatti per prossimità (O(N^2) per ora: ok per 400 NPC)
            for (int i = 0; i < _npcIds.Count; i++)
            {
                int a = _npcIds[i];
                if (!world.GridPos.TryGetValue(a, out var pa)) continue;

                for (int j = i + 1; j < _npcIds.Count; j++)
                {
                    int b = _npcIds[j];

                    if (costSample)
                        costPairChecks++;
                    if (costPerNpc)
                    {
                        costObserver.AddNpcWork(a, 1);
                        costObserver.AddNpcWork(b, 1);
                    }

                    if (!world.GridPos.TryGetValue(b, out var pb)) continue;

                    int dist = Manhattan(pa.X, pa.Y, pb.X, pb.Y);
                    if (dist > _contactRadius) continue;

                    // NEW (Giorno 8): orientamento per "parlare".
                    // Regola v0:
                    // - ProximityTalk richiede che A stia guardando B (B nella cella frontale di A)
                    // - e viceversa per B->A
                    // - AlarmShout invece ignora orientamento (ma oggi emission non decide canale; le rule lo fanno)
                    bool aCanTalkToB = CanDirectlyTalk(world, a, b);
                    bool bCanTalkToA = CanDirectlyTalk(world, b, a);

                    if (aCanTalkToB)
                        envelopesEmitted += EmitForPair(world, tick, tokenBus, telemetry, a, b, maxPerEncounter, maxPerDay, cooldownTicks, costPerNpc);

                    if (bCanTalkToA)
                        envelopesEmitted += EmitForPair(world, tick, tokenBus, telemetry, b, a, maxPerEncounter, maxPerDay, cooldownTicks, costPerNpc);
                }
            }

            telemetry.Counter("TokenEmissionPipeline.EnvelopesEmitted", envelopesEmitted);

            if (costSample)
            {
                costObserver.AddCounter(RuntimeCostCounter.TokenEmissionPairChecks, costPairChecks);
                costObserver.AddCounter(RuntimeCostCounter.TokenEmissionTokensCreated, envelopesEmitted);
                costObserver.EndSample(RuntimeCostChannel.TokenEmission, costStart);
            }
        }

        private int EmitForPair(
            World world,
            Tick tick,
            TokenBus tokenBus,
            Telemetry telemetry,
            int speakerId,
            int listenerId,
            int maxPerEncounter,
            int maxPerDay,
            int cooldownTicks,
            bool costPerNpc)
        {
            if (!world.Memory.TryGetValue(speakerId, out var store) || store == null)
                return 0;

            _tokensEmittedToday.TryGetValue(speakerId, out int emittedToday);
            if (emittedToday >= maxPerDay)
                return 0;

            // scegliamo le top trace dello speaker
            store.GetTopTraces(_topN, _topTraces);

            int emittedThisEncounter = 0;

            for (int t = 0; t < _topTraces.Count; t++)
            {
                if (emittedThisEncounter >= maxPerEncounter)
                    break;

                var trace = _topTraces[t];

                // prova rules
                for (int r = 0; r < _rules.Count; r++)
                {
                    var rule = _rules[r];
                    if (!rule.Matches(trace))
                        continue;

                    // cooldown "stessa informazione alla stessa persona"
                    var key = new ShareKey(speakerId, listenerId, trace.Type, trace.SubjectId, trace.CellX, trace.CellY);

                    if (_lastShareTick.TryGetValue(key, out long lastTick))
                    {
                        if ((tick.Index - lastTick) < cooldownTicks)
                            break; // non ripetiamo
                    }

                    // NOTA IMPORTANTE (Opzione 2):
                    // - Passiamo tick.Index alla rule, così può costruire un envelope con TickIndex corretto.
                    if (rule.TryCreateToken(world, tick.Index, speakerId, listenerId, trace, out var env))
                    {
                        // Patch 0.01P3 (architetturalmente corretto):
                        // NON pubblichiamo direttamente sul bus, altrimenti perdiamo osservabilità
                        // per i token generati in altri punti (es. iniezioni scenario in SimulationHost).
                        //
                        // Punto canonico di OUT:
                        // - aggiorna DebugNpcTokenLog
                        // - emette balloon TokenOut
                        // - pubblica sul TokenBusOut
                        world.PublishTokenOut(tokenBus, env);

                        _lastShareTick[key] = tick.Index;

                        emittedThisEncounter++;
                        emittedToday++;

                        telemetry.Counter("TokenEmissionPipeline.TokensCreated", 1);
                    }

                    break; // una trace -> max un token (per ora)
                }

                if (emittedToday >= maxPerDay)
                    break;
            }

            if (emittedThisEncounter > 0)
            {
                _tokensEmittedToday[speakerId] = emittedToday;
                if (costPerNpc)
                    world.RuntimeCostObserver.AddNpcWork(speakerId, emittedThisEncounter);
            }

            return emittedThisEncounter;
        }

        // =============================================================================
        // PruneExpiredShareCooldowns
        // =============================================================================
        /// <summary>
        /// <para>
        /// Rimuove dal dizionario dei cooldown comunicativi le entry ormai incapaci
        /// di influenzare l'emissione corrente. La regola replica il controllo usato
        /// durante l'emissione: una condivisione blocca solo se
        /// <c>currentTick - lastShareTick &lt; cooldownTicks</c>. Quando la distanza e'
        /// maggiore del cooldown, conservare la chiave non cambia piu' il comportamento
        /// osservabile e produce solo crescita memoria nel lungo periodo.
        /// </para>
        ///
        /// <para><b>Micro-hardening memoria v0.11d.MEMORY-FIX-01</b></para>
        /// <para>
        /// La potatura usa un buffer riutilizzato per evitare allocazioni per tick e
        /// rimuove le chiavi solo dopo l'enumerazione del dizionario, preservando la
        /// sicurezza dell'iterazione C#.
        /// </para>
        /// </summary>
        private void PruneExpiredShareCooldowns(long currentTick, int cooldownTicks)
        {
            if (_lastShareTick.Count == 0)
                return;

            _lastShareTickPruneBuffer.Clear();

            foreach (var pair in _lastShareTick)
            {
                long age = currentTick - pair.Value;
                if (age > cooldownTicks)
                    _lastShareTickPruneBuffer.Add(pair.Key);
            }

            for (int i = 0; i < _lastShareTickPruneBuffer.Count; i++)
                _lastShareTick.Remove(_lastShareTickPruneBuffer[i]);

            _lastShareTickPruneBuffer.Clear();
        }

        private static int Manhattan(int ax, int ay, int bx, int by)
        {
            int dx = ax - bx; if (dx < 0) dx = -dx;
            int dy = ay - by; if (dy < 0) dy = -dy;
            return dx + dy;
        }

        // -------------------------
        // NEW (Giorno 8): talking + facing
        // -------------------------

        /// <summary>
        /// CanDirectlyTalk:
        /// Regola v0 per ProximityTalk:
        /// - speaker e listener devono essere in celle adiacenti (Manhattan=1)
        /// - listener deve essere nella cella "frontale" dello speaker (in base a Facing)
        ///
        /// Questo serve per evitare che “parlino” anche se sono spalla-a-spalla o dietro.
        /// </summary>
        private static bool CanDirectlyTalk(World world, int speakerId, int listenerId)
        {
            if (!world.GridPos.TryGetValue(speakerId, out var sp)) return false;
            if (!world.GridPos.TryGetValue(listenerId, out var li)) return false;

            int dist = Manhattan(sp.X, sp.Y, li.X, li.Y);
            if (dist != 1) return false;

            if (!world.NpcFacing.TryGetValue(speakerId, out var facing))
                facing = CardinalDirection.North;

            int dx = li.X - sp.X;
            int dy = li.Y - sp.Y;

            switch (facing)
            {
                case CardinalDirection.North: return dx == 0 && dy == 1;
                case CardinalDirection.South: return dx == 0 && dy == -1;
                case CardinalDirection.East: return dx == 1 && dy == 0;
                case CardinalDirection.West: return dx == -1 && dy == 0;
                default: return false;
            }
        }

        /// <summary>
        /// Chiave cooldown: speaker+listener+contenuto (type/subject/cell).
        /// Implementiamo hash/equals per essere veloci e deterministici.
        /// </summary>
        private readonly struct ShareKey : IEquatable<ShareKey>
        {
            private readonly int _speaker;
            private readonly int _listener;
            private readonly MemoryType _type;
            private readonly int _subject;
            private readonly int _x;
            private readonly int _y;

            public ShareKey(int speaker, int listener, MemoryType type, int subject, int x, int y)
            {
                _speaker = speaker;
                _listener = listener;
                _type = type;
                _subject = subject;
                _x = x;
                _y = y;
            }

            public bool Equals(ShareKey other)
            {
                return _speaker == other._speaker &&
                       _listener == other._listener &&
                       _type == other._type &&
                       _subject == other._subject &&
                       _x == other._x &&
                       _y == other._y;
            }

            public override bool Equals(object obj) => obj is ShareKey other && Equals(other);

            public override int GetHashCode()
            {
                unchecked
                {
                    int h = 17;
                    h = h * 31 + _speaker;
                    h = h * 31 + _listener;
                    h = h * 31 + (int)_type;
                    h = h * 31 + _subject;
                    h = h * 31 + _x;
                    h = h * 31 + _y;
                    return h;
                }
            }
        }
    }
}
