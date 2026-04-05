using System;
using System.Collections.Generic;
using UnityEngine;

namespace Arcontio.Core
{
    /// <summary>
    /// NpcLandmarkMemory (v0.02 Day3):
    /// memoria SOGGETTIVA dei landmark/edge per un singolo NPC.
    ///
    /// Contesto (molto importante in ARCONTIO):
    /// - Il LandmarkRegistry e' oggettivo (World-side) ed e' derivato dalla mappa.
    /// - Questa classe invece rappresenta "cosa conosce" un NPC.
    ///
    /// Perche' serve:
    /// - Se ogni NPC conoscesse l'intero grafo globale, avremmo:
    ///   1) zero differenze tra NPC
    ///   2) crescita non controllata (performance e debug)
    ///   3) violazione del principio di soggettivita' (manifesto)
    ///
    /// Day3 scope:
    /// - subset per NPC (cap)
    /// - learn event-driven (agganciato al movimento)
    /// - eviction (stale + over-cap)
    /// - anti-thrashing via cooldown
    ///
    /// Nota:
    /// - Non implementiamo ancora "macro route planning": quello e' Day4.
    /// - Non implementiamo ancora "last-mile" micro pathfinding: quello e' Day5.
    /// </summary>
    [Serializable]
    public sealed class NpcLandmarkMemory
    {
        // ============================================================
        // INTERNAL TYPES
        // ============================================================

        [Serializable]
        private struct LandmarkEntry
        {
            public int NodeId;

            // "Ultimo tick" in cui l'NPC ha visto/attraversato questo landmark.
            public long LastSeenTick;

            // Confidenza 0..1 (molto semplice in Day3):
            // - cresce quando il landmark viene rinforzato
            // - e' usata per scegliere cosa evictare in caso di over-cap
            public float Confidence01;
        }

        [Serializable]
        private struct EdgeEntry
        {
            public EdgeKey Key;
            public int CostCells;
            public long LastSeenTick;
            public float Confidence01;
        }

        /// <summary>
        /// EdgeKey (non orientato):
        /// memorizziamo sempre (A=min, B=max).
        ///
        /// Questo evita duplicati (A,B) vs (B,A).
        /// </summary>
        [Serializable]
        public readonly struct EdgeKey : IEquatable<EdgeKey>
        {
            public readonly int A;
            public readonly int B;

            public EdgeKey(int nodeA, int nodeB)
            {
                A = Mathf.Min(nodeA, nodeB);
                B = Mathf.Max(nodeA, nodeB);
            }

            public bool Equals(EdgeKey other) => A == other.A && B == other.B;
            public override bool Equals(object obj) => obj is EdgeKey other && Equals(other);

            public override int GetHashCode()
            {
                // Hash semplice e deterministico.
                unchecked
                {
                    int h = 17;
                    h = (h * 31) + A;
                    h = (h * 31) + B;
                    return h;
                }
            }
        }

        // ============================================================
        // STATE
        // ============================================================

        // Nota: usiamo Dictionary per accesso O(1) durante il tick.
        // In Day3 i cap sono bassi (decine/centinaia), quindi e' efficiente.
        private readonly Dictionary<int, LandmarkEntry> _landmarksById;
        private readonly Dictionary<EdgeKey, EdgeEntry> _edgesByKey;

        // Anti-thrashing: dopo un'eviction, evitiamo re-learning immediato.
        // nodeId -> tick fino al quale ignoriamo learn.
        private readonly Dictionary<int, long> _nodeEvictionCooldownUntilTick;

        // edgeKey -> tick fino al quale ignoriamo learn.
        private readonly Dictionary<EdgeKey, long> _edgeEvictionCooldownUntilTick;

        private readonly int _maxLandmarks;
        private readonly int _maxEdges;

        // Day3: "ultimo landmark" visto dall'NPC.
        // Serve per costruire edge minimi in modo event-driven:
        // quando vedi un nuovo landmark, lo colleghi al precedente se esiste l'edge nel registry.
        public int LastVisitedLandmarkId;

        // Tick in cui l'NPC ha visitato l'ultimo landmark registrato sopra.
        //
        // Perche' ci serve:
        // - nel Day3 vogliamo introdurre un debounce anti-jitter molto esplicito
        //   (richiesto dal design)
        // - se l'NPC oscilla tra due landmark ravvicinati in pochissimi tick
        //   non vogliamo creare/refrescare edge a raffica
        // - il valore viene letto da World.NotifyNpcMovedForLandmarkLearning(...)
        //   per decidere se una transizione e' abbastanza "stabile" da diventare
        //   conoscenza di edge
        public long LastVisitedLandmarkTick;

        public int KnownLandmarksCount => _landmarksById.Count;
        public int KnownEdgesCount => _edgesByKey.Count;

        public NpcLandmarkMemory(int maxLandmarks, int maxEdges)
        {
            if (maxLandmarks <= 0) maxLandmarks = 1;
            if (maxEdges <= 0) maxEdges = 1;

            _maxLandmarks = maxLandmarks;
            _maxEdges = maxEdges;

            _landmarksById = new Dictionary<int, LandmarkEntry>(capacity: Mathf.Min(256, _maxLandmarks));
            _edgesByKey = new Dictionary<EdgeKey, EdgeEntry>(capacity: Mathf.Min(512, _maxEdges));

            _nodeEvictionCooldownUntilTick = new Dictionary<int, long>(64);
            _edgeEvictionCooldownUntilTick = new Dictionary<EdgeKey, long>(64);

            LastVisitedLandmarkId = 0;
            LastVisitedLandmarkTick = long.MinValue;
        }

        // ============================================================
        // LEARN (event-driven)
        // ============================================================

        /// <summary>
        /// LearnLandmark:
        /// rinforza (o aggiunge) la conoscenza di un nodo.
        ///
        /// Nota di policy:
        /// - se il nodo e' in cooldown (evicted da poco), ignoriamo.
        /// - Day3 usa un incremento di confidence molto semplice (senza curve complesse).
        /// </summary>
        public void LearnLandmark(int nodeId, long nowTick, int evictionCooldownTicks)
        {
            if (nodeId == 0) return;

            if (IsNodeInEvictionCooldown(nodeId, nowTick))
                return;

            if (_landmarksById.TryGetValue(nodeId, out var e))
            {
                e.LastSeenTick = nowTick;
                e.Confidence01 = Mathf.Clamp01(e.Confidence01 + 0.10f);
                _landmarksById[nodeId] = e;
            }
            else
            {
                _landmarksById[nodeId] = new LandmarkEntry
                {
                    NodeId = nodeId,
                    LastSeenTick = nowTick,
                    Confidence01 = 0.25f,
                };
            }

            // Nota: il cap viene applicato dal System (NpcLandmarkMemorySystem) per evitare
            // che Learn debba fare eviction in-line durante il tick di movimento.
        }

        /// <summary>
        /// LearnEdge:
        /// aggiunge/aggiorna la conoscenza di un edge tra due nodi.
        ///
        /// Nota:
        /// - key e' non orientato.
        /// - costCells e' preso dal registry oggettivo (bootstrap Day2).
        /// - anti-thrashing come per i nodi.
        /// </summary>
        /// <param name="initialConfidence">
        /// Confidence iniziale per edge nuovi (default 0.25f per edge fisici).
        /// Passare un valore inferiore (es. 0.15f) per edge soggettivi visivi
        /// (v0.03.04.c-ComplexEdge_Creation). Per edge già esistenti viene sempre
        /// applicato il rinforzo standard (+0.10f), indipendentemente da questo valore.
        /// </param>
        public void LearnEdge(int nodeA, int nodeB, int costCells, long nowTick, int evictionCooldownTicks, float initialConfidence = 0.25f)
        {
            if (nodeA == 0 || nodeB == 0) return;
            if (nodeA == nodeB) return;

            var key = new EdgeKey(nodeA, nodeB);

            if (IsEdgeInEvictionCooldown(key, nowTick))
                return;

            if (_edgesByKey.TryGetValue(key, out var e))
            {
                e.LastSeenTick = nowTick;
                e.CostCells = costCells;
                e.Confidence01 = Mathf.Clamp01(e.Confidence01 + 0.10f);
                _edgesByKey[key] = e;
            }
            else
            {
                _edgesByKey[key] = new EdgeEntry
                {
                    Key = key,
                    CostCells = costCells,
                    LastSeenTick = nowTick,
                    Confidence01 = Mathf.Clamp01(initialConfidence),
                };
            }
        }

        // ============================================================
        // EVICTION + CAPS (called by System)
        // ============================================================

        /// <summary>
        /// TickMaintenance:
        /// applica:
        /// - eviction per staleness
        /// - cap enforcement (evict lowest confidence)
        ///
        /// Nota:
        /// - viene chiamata da NpcLandmarkMemorySystem.
        /// - qui abbiamo la visione completa di "quanto siamo over-cap" e quindi possiamo
        ///   fare eviction in modo deterministico.
        /// </summary>
        public void TickMaintenance(long nowTick, int staleTicks, int evictionCooldownTicks)
        {
            if (staleTicks < 1) staleTicks = 1;
            if (evictionCooldownTicks < 0) evictionCooldownTicks = 0;

            // 1) Stale eviction (nodes)
            if (_landmarksById.Count > 0)
            {
                // Collect to remove (avoid modifying during enumeration)
                // Nota: lista piccola, allocazione accettabile in Day3.
                var toRemove = new List<int>();
                foreach (var kv in _landmarksById)
                {
                    if ((nowTick - kv.Value.LastSeenTick) >= staleTicks)
                        toRemove.Add(kv.Key);
                }
                for (int i = 0; i < toRemove.Count; i++)
                {
                    EvictNode(toRemove[i], nowTick, evictionCooldownTicks);
                }
            }

            // 2) Stale eviction (edges)
            if (_edgesByKey.Count > 0)
            {
                var toRemove = new List<EdgeKey>();
                foreach (var kv in _edgesByKey)
                {
                    if ((nowTick - kv.Value.LastSeenTick) >= staleTicks)
                        toRemove.Add(kv.Key);
                }
                for (int i = 0; i < toRemove.Count; i++)
                {
                    EvictEdge(toRemove[i], nowTick, evictionCooldownTicks);
                }
            }

            // 3) Cap enforcement (nodes)
            while (_landmarksById.Count > _maxLandmarks)
            {
                int victim = FindLowestConfidenceNode();
                if (victim == 0)
                    break;

                EvictNode(victim, nowTick, evictionCooldownTicks);
            }

            // 4) Cap enforcement (edges)
            while (_edgesByKey.Count > _maxEdges)
            {
                var victim = FindLowestConfidenceEdge();
                if (victim.A == 0 && victim.B == 0)
                    break;

                EvictEdge(victim, nowTick, evictionCooldownTicks);
            }

            // Nota: se abbiamo evicted un landmark che era LastVisited, resettiamo.
            //
            // Importante:
            // - NON dobbiamo azzerare LastVisitedLandmarkTick ad ogni maintenance tick,
            //   altrimenti il debounce anti-jitter del Day3 smette di avere significato
            // - resettiamo tick solo se il landmark memorizzato come "ultimo visitato"
            //   non esiste piu' nella memoria dell'NPC
            if (LastVisitedLandmarkId != 0 && !_landmarksById.ContainsKey(LastVisitedLandmarkId))
            {
                LastVisitedLandmarkId = 0;
                LastVisitedLandmarkTick = long.MinValue;
            }
        }

        private void EvictNode(int nodeId, long nowTick, int cooldownTicks)
        {
            _landmarksById.Remove(nodeId);
            if (cooldownTicks > 0)
                _nodeEvictionCooldownUntilTick[nodeId] = nowTick + cooldownTicks;
        }

        private void EvictEdge(EdgeKey key, long nowTick, int cooldownTicks)
        {
            _edgesByKey.Remove(key);
            if (cooldownTicks > 0)
                _edgeEvictionCooldownUntilTick[key] = nowTick + cooldownTicks;
        }

        private bool IsNodeInEvictionCooldown(int nodeId, long nowTick)
        {
            if (_nodeEvictionCooldownUntilTick.TryGetValue(nodeId, out long until))
                return nowTick < until;
            return false;
        }

        private bool IsEdgeInEvictionCooldown(EdgeKey key, long nowTick)
        {
            if (_edgeEvictionCooldownUntilTick.TryGetValue(key, out long until))
                return nowTick < until;
            return false;
        }

        private int FindLowestConfidenceNode()
        {
            // Policy semplice e deterministica:
            // - scegli il landmark con confidence minima
            // - tie-breaker: LastSeenTick piu vecchio
            int victim = 0;
            float bestConf = float.MaxValue;
            long bestLastSeen = long.MaxValue;

            foreach (var kv in _landmarksById)
            {
                var e = kv.Value;
                if (e.Confidence01 < bestConf)
                {
                    bestConf = e.Confidence01;
                    bestLastSeen = e.LastSeenTick;
                    victim = kv.Key;
                }
                else if (Mathf.Approximately(e.Confidence01, bestConf) && e.LastSeenTick < bestLastSeen)
                {
                    bestLastSeen = e.LastSeenTick;
                    victim = kv.Key;
                }
            }

            return victim;
        }

        private EdgeKey FindLowestConfidenceEdge()
        {
            EdgeKey victim = default;
            bool hasVictim = false;
            float bestConf = float.MaxValue;
            long bestLastSeen = long.MaxValue;

            foreach (var kv in _edgesByKey)
            {
                var e = kv.Value;
                if (e.Confidence01 < bestConf)
                {
                    bestConf = e.Confidence01;
                    bestLastSeen = e.LastSeenTick;
                    victim = kv.Key;
                    hasVictim = true;
                }
                else if (Mathf.Approximately(e.Confidence01, bestConf) && e.LastSeenTick < bestLastSeen)
                {
                    bestLastSeen = e.LastSeenTick;
                    victim = kv.Key;
                    hasVictim = true;
                }
            }

            return hasVictim ? victim : default;
        }



        // ============================================================
        // DAY4 HELPERS (macro planner)
        // ============================================================

        [Serializable]
        public readonly struct KnownNeighbor
        {
            public readonly int NodeId;
            public readonly int CostCells;
            public readonly float Reliability01;

            public KnownNeighbor(int nodeId, int costCells, float reliability01)
            {
                NodeId = nodeId;
                CostCells = costCells;
                Reliability01 = reliability01;
            }
        }

        public bool ContainsLandmark(int nodeId)
        {
            if (nodeId == 0) return false;
            return _landmarksById.ContainsKey(nodeId);
        }

        public bool TryFindNearestKnownLandmark(LandmarkRegistry registry, int cellX, int cellY, out int nodeId)
        {
            nodeId = 0;
            if (registry == null || _landmarksById.Count == 0)
                return false;

            int bestId = 0;
            int bestDist = int.MaxValue;
            foreach (var kv in _landmarksById)
            {
                if (!registry.TryGetActiveNodeById(kv.Key, out var n) || n == null)
                    continue;

                int d = Mathf.Abs(n.CellX - cellX) + Mathf.Abs(n.CellY - cellY);
                if (d < bestDist)
                {
                    bestDist = d;
                    bestId = kv.Key;
                }
            }

            nodeId = bestId;
            return nodeId != 0;
        }

        public void FillKnownNeighbors(int nodeId, List<KnownNeighbor> outNeighbors)
        {
            outNeighbors?.Clear();
            if (outNeighbors == null || nodeId == 0 || _edgesByKey.Count == 0)
                return;

            foreach (var kv in _edgesByKey)
            {
                var edge = kv.Value;
                if (edge.Key.A == nodeId)
                {
                    if (_landmarksById.ContainsKey(edge.Key.B))
                        outNeighbors.Add(new KnownNeighbor(edge.Key.B, edge.CostCells, edge.Confidence01));
                }
                else if (edge.Key.B == nodeId)
                {
                    if (_landmarksById.ContainsKey(edge.Key.A))
                        outNeighbors.Add(new KnownNeighbor(edge.Key.A, edge.CostCells, edge.Confidence01));
                }
            }
        }


        // ============================================================
        // VIEW HELPERS (overlay)
        // ============================================================

        /// <summary>
        /// FillOverlayData:
        /// converte la memoria soggettiva in nodi/edge disegnabili.
        ///
        /// Nota:
        /// - Abbiamo bisogno del LandmarkRegistry per risolvere nodeId -> coordinate e kind.
        /// - Se un nodo non e piu attivo nel registry, lo ignoriamo.
        /// </summary>
        public void FillOverlayData(LandmarkRegistry registry, List<LandmarkOverlayNode> outNodes, List<LandmarkOverlayEdge> outEdges)
        {
            outNodes?.Clear();
            outEdges?.Clear();

            if (registry == null)
                return;

            // Nodi
            if (outNodes != null)
            {
                foreach (var kv in _landmarksById)
                {
                    // Risolviamo coordinate facendo scanning O(N) nel registry.
                    // Nota: e' ok, i cap sono piccoli.
                    int nodeId = kv.Key;
                    if (!TryResolveNode(registry, nodeId, out int x, out int y, out int kind))
                        continue;

                    string label = kind == (int)LandmarkRegistry.LandmarkKind.Doorway ? $"D#{nodeId}" : $"J#{nodeId}";
                    outNodes.Add(new LandmarkOverlayNode(cellX: x, cellY: y, kind: kind, nodeId: nodeId, label: label));
                }
            }

            // Edge
            if (outEdges != null)
            {
                foreach (var kv in _edgesByKey)
                {
                    var key = kv.Key;

                    if (!TryResolveNode(registry, key.A, out int ax, out int ay, out _))
                        continue;
                    if (!TryResolveNode(registry, key.B, out int bx, out int by, out _))
                        continue;

                    outEdges.Add(new LandmarkOverlayEdge(ax: ax, ay: ay, bx: bx, by: by, reliability01: 1f));
                }
            }
        }

        private static bool TryResolveNode(LandmarkRegistry registry, int nodeId, out int x, out int y, out int kind)
        {
            x = 0;
            y = 0;
            kind = 0;

            // Non abbiamo un lookup diretto id->node in LandmarkRegistry (Day2 O(N)).
            // Quindi facciamo scanning.
            var nodes = registry.Nodes;
            for (int i = 0; i < nodes.Count; i++)
            {
                var n = nodes[i];
                if (n == null) continue;
                if (!n.IsActive) continue;
                if (n.Id != nodeId) continue;

                x = n.CellX;
                y = n.CellY;
                kind = (int)n.Kind;
                return true;
            }

            return false;
        }
    }
}
