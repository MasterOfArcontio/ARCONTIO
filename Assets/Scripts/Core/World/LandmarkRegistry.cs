using System;
using System.Collections.Generic;
using UnityEngine;

namespace Arcontio.Core
{
    /// <summary>
    /// LandmarkRegistry (v0.02 - Day2):
    /// registro "oggettivo" dei landmark.
    ///
    /// Cosa contiene:
    /// - LandmarkNode: punti notevoli sulla griglia (Doorway, Junction).
    /// - LandmarkEdge: adiacenza sparsa tra nodi (edge minimi in bootstrap).
    ///
    /// Importante (scelta progettuale ARCONTIO):
    /// - Questo registry NON è la memoria soggettiva degli NPC.
    /// - È una struttura derivata dalla mappa (bootstrap + update incrementale in futuro).
    /// - Il layer soggettivo (Day3+) potrà "ancorarsi" a questi nodi, o copiarli/filtrarli.
    ///
    /// Patch 0.02D2_1:
    /// - Implementiamo:
    ///   1) candidate detection: Doorway + Junction
    ///   2) merge (merge_radius)
    ///   3) cap globale (maxWorldLandmarks) con policy soft: IsActive=false (NO delete)
    ///   4) edge minimi in bootstrap: scan cardinali fino al prossimo landmark
    /// </summary>
    public sealed class LandmarkRegistry
    {
        // ============================================================
        // TYPES
        // ============================================================

        public enum LandmarkKind
        {
            Doorway = 1,
            Junction = 2,
        }

        [Serializable]
        public sealed class LandmarkNode
        {
            public int Id;
            public int CellX;
            public int CellY;
            public LandmarkKind Kind;

            /// <summary>
            /// Soft delete:
            /// - se false, il nodo resta nel registry ma non viene usato per routing/overlay.
            /// - evita rotture di referenze (future: NPC memory che punta a nodi).
            /// </summary>
            public bool IsActive = true;
        }

        [Serializable]
        public sealed class LandmarkEdge
        {
            public int FromNodeId;
            public int ToNodeId;

            /// <summary>
            /// Costo "macro" base: quante celle (cardinali) separano i due nodi lungo il corridoio.
            /// Nota: nel bootstrap è sempre un valore deterministico.
            /// </summary>
            public int CostCells;

            public bool IsActive = true;
        }

        // ============================================================
        // STATE
        // ============================================================

        private readonly List<LandmarkNode> _nodes = new();
        private readonly List<LandmarkEdge> _edges = new();

        // Indice rapido: cella -> nodeId (solo per nodi attivi).
        // Nota: questo è ricostruibile e quindi può essere invalidato/ricostruito.
        private readonly Dictionary<int, int> _activeNodeIdByCellIndex = new();

        // Importante: per poter risolvere cella->nodeId in runtime, dobbiamo conoscere
        // la width usata per calcolare l'indice lineare (y*width + x).
        // La fissiamo durante RebuildFromWorld (bootstrap).
        private int _mapWidthForCellIndex = 0;

        private int _nextNodeId = 1;

        public IReadOnlyList<LandmarkNode> Nodes => _nodes;
        public IReadOnlyList<LandmarkEdge> Edges => _edges;

        // ============================================================
        // REBUILD (bootstrap)
        // ============================================================

        /// <summary>
        /// RebuildFromWorld:
        /// ricostruisce il registry a partire dallo stato corrente della mappa.
        ///
        /// Nota:
        /// - In 0.02 lo usiamo come "bootstrap" dopo il seeding.
        /// - In futuro potremo fare update incrementale (event-driven) per porte/muri.
        /// </summary>
        public void RebuildFromWorld(World world)
        {
            if (world == null) return;
           
            _nodes.Clear();
            _edges.Clear();
            _activeNodeIdByCellIndex.Clear();
            _nextNodeId = 1;

            // Cache della width per calcolo index (necessaria per query runtime cell->node).
            _mapWidthForCellIndex = world.MapWidth;

            // Leggiamo parametri.
            var lm = world.Config?.Sim?.landmarks;
            float mergeRadius = lm != null ? lm.merge_radius : 1.0f;
            int minExits = lm != null && lm.candidate != null ? lm.candidate.junction_min_exits : 3;
            int maxWorld = lm != null ? lm.maxWorldLandmarks : 512;
            int maxEdgesPerLandmark = lm != null ? lm.maxEdgesPerLandmark : 8;

            if (mergeRadius < 0) mergeRadius = 0;
            if (minExits < 3) minExits = 3;
            if (maxWorld <= 0) maxWorld = 1;
            if (maxEdgesPerLandmark <= 0) maxEdgesPerLandmark = 1;

            // ------------------------------------------------------------
            // 1) Candidate detection: DOORWAY
            // ------------------------------------------------------------
            // Strategia (minimalista):
            // - consideriamo "porta" qualsiasi oggetto con DefId che inizia con "door_".
            // - è volutamente una convenzione di naming: semplice e leggibile.
            // - se in futuro introduciamo una property IsDoor, sostituiamo qui.
            foreach (var kv in world.Objects)
            {
                var obj = kv.Value;
                if (obj == null) continue;

                if (!world.ObjectDefs.TryGetValue(obj.DefId, out var def) || def == null)
                    continue;

                if (!IsDoorDef(def.Id))
                    continue;

                AddOrMergeNode(obj.CellX, obj.CellY, LandmarkKind.Doorway, mergeRadius);
            }

            // ------------------------------------------------------------
            // 2) Candidate detection: JUNCTION
            // ------------------------------------------------------------
            // Junction = cella navigabile con >= N uscite "stabili".
            // "stabile" qui significa: dipende solo dalla geometria (BlocksMovement),
            // non dalla presenza runtime di NPC.
            for (int y = 0; y < world.MapHeight; y++)
            {
                for (int x = 0; x < world.MapWidth; x++)
                {
                    if (world.BlocksMovementAt(x, y))
                        continue;
                    
                    // Controllo se sono in presenza di una giunzione a + o a T
                    if (!IsJunction(world, x, y))
                        continue;

                    AddOrMergeNode(x, y, LandmarkKind.Junction, mergeRadius);
                }
            }

            // ------------------------------------------------------------
            // 3) Cap globale: soft deactivate (IsActive=false)
            // ------------------------------------------------------------
            ApplyGlobalCap(maxWorld);

            // ------------------------------------------------------------
            // 4) Ricostruisco indice rapido per nodi attivi
            // ------------------------------------------------------------
            RebuildActiveCellIndex(world);

            // ------------------------------------------------------------
            // 5) Edge minimi (bootstrap)
            // ------------------------------------------------------------
            BuildMinimalEdges(world, maxEdgesPerLandmark);
        }

        // ============================================================
        // INTERNALS
        // ============================================================
        private static bool IsJunction(World world, int cx, int cy)
        {
            // Primo controllo sulle diagonali che devono essere piene
            for (int dy = -1; dy <= 1; dy += 2)
            {
                for (int dx = -1; dx <= 1; dx += 2)
                {
                    int x = cx + dx;
                    int y = cy + dy;

                    if (x < 0 || x >= world.MapWidth || y < 0 || y >= world.MapHeight)
                        return false;

                    if (!world.BlocksMovementAt(x, y))
                        return false;
                }
            }

            // Secondo controllo sulla intersezione a +, qua può esserci al massimo un oggetto (Caso di giunzione a T)
            int blocks = 0;
            for (int dy = -1; dy <= 1; dy++)
            {
                for (int dx = -1; dx <= 1; dx++)
                {
                    // Scarta il centro
                    if ((dx == 0) && (dy == 0)) continue;

                    // Scarta le diagonali
                    if (Math.Abs(dx) + Math.Abs(dy) != 1) continue;

                    int x = cx + dx;
                    int y = cy + dy;

                    if (world.BlocksMovementAt(x, y))  blocks++;
                }
            }
            if (blocks <= 1) return true; 
            else return false;
        }

        private static bool IsDoorDef(string defId)
        {
            if (string.IsNullOrEmpty(defId)) return false;
            return defId.StartsWith("door_", StringComparison.OrdinalIgnoreCase);
        }

        private LandmarkNode AddOrMergeNode(int x, int y, LandmarkKind kind, float mergeRadius)
        {
            // Merge semplice:
            // - se esiste già un nodo dello stesso kind entro mergeRadius, non creiamo duplicati.
            // - NON mergiamo kind diversi: doorway e junction possono coesistere anche vicini.

            float r2 = mergeRadius * mergeRadius;
            for (int i = 0; i < _nodes.Count; i++)
            {
                var n = _nodes[i];
                if (n == null) continue;
                if (!n.IsActive) continue; // i nodi disattivati non assorbono nuovi candidati
                if (n.Kind != kind) continue;

                float dx = n.CellX - x;
                float dy = n.CellY - y;
                if ((dx * dx) + (dy * dy) <= r2)
                {
                    return n;
                }
            }

            var created = new LandmarkNode
            {
                Id = _nextNodeId++,
                CellX = x,
                CellY = y,
                Kind = kind,
                IsActive = true,
            };

            _nodes.Add(created);
            return created;
        }

        private static int CountStableExits(World world, int x, int y)
        {
            int exits = 0;

            // N
            if (y + 1 < world.MapHeight && !world.BlocksMovementAt(x, y + 1)) exits++;
            // S
            if (y - 1 >= 0 && !world.BlocksMovementAt(x, y - 1)) exits++;
            // E
            if (x + 1 < world.MapWidth && !world.BlocksMovementAt(x + 1, y)) exits++;
            // W
            if (x - 1 >= 0 && !world.BlocksMovementAt(x - 1, y)) exits++;

            return exits;
        }

        private void ApplyGlobalCap(int maxWorld)
        {
            // Policy deterministica (semplice, Day2):
            // 1) priorità Doorway (choke point strutturali)
            // 2) poi Junction
            // 3) ordine di inserimento (Id crescente)

            // Se siamo già sotto il cap, niente da fare.
            int active = 0;
            for (int i = 0; i < _nodes.Count; i++)
                if (_nodes[i] != null && _nodes[i].IsActive) active++;

            if (active <= maxWorld)
                return;

            // Collezioniamo nodi attivi, ordiniamo per priorità e poi disattiviamo gli ultimi.
            var temp = new List<LandmarkNode>(active);
            for (int i = 0; i < _nodes.Count; i++)
                if (_nodes[i] != null && _nodes[i].IsActive) temp.Add(_nodes[i]);

            temp.Sort((a, b) =>
            {
                int pa = a.Kind == LandmarkKind.Doorway ? 0 : 1;
                int pb = b.Kind == LandmarkKind.Doorway ? 0 : 1;
                int c = pa.CompareTo(pb);
                if (c != 0) return c;
                return a.Id.CompareTo(b.Id);
            });

            for (int i = maxWorld; i < temp.Count; i++)
                temp[i].IsActive = false;
        }

        private void RebuildActiveCellIndex(World world)
        {
            _activeNodeIdByCellIndex.Clear();
            for (int i = 0; i < _nodes.Count; i++)
            {
                var n = _nodes[i];
                if (n == null || !n.IsActive) continue;
                int idx = (n.CellY * world.MapWidth) + n.CellX;
                _activeNodeIdByCellIndex[idx] = n.Id;
            }
        }

        private void BuildMinimalEdges(World world, int maxEdgesPerLandmark)
        {
            _edges.Clear();

            // Adiacenza sparsa: contiamo edge per nodo per applicare maxEdgesPerLandmark.
            var edgesPerNode = new Dictionary<int, int>(capacity: _nodes.Count);

            // Dedup: edge non orientato (min,max) per evitare duplicati.
            var seen = new HashSet<long>();

            for (int i = 0; i < _nodes.Count; i++)
            {
                var n = _nodes[i];
                if (n == null || !n.IsActive) continue;

                // Se questo nodo è già saturo, skip.
                if (GetCount(edgesPerNode, n.Id) >= maxEdgesPerLandmark)
                    continue;

                // Scans cardinali: troviamo il prossimo landmark lungo N/E/S/W.
                TryConnectRay(world, n, 0, 1, maxEdgesPerLandmark, edgesPerNode, seen);
                TryConnectRay(world, n, 0, -1, maxEdgesPerLandmark, edgesPerNode, seen);
                TryConnectRay(world, n, 1, 0, maxEdgesPerLandmark, edgesPerNode, seen);
                TryConnectRay(world, n, -1, 0, maxEdgesPerLandmark, edgesPerNode, seen);
            }
        }

        private void TryConnectRay(
            World world,
            LandmarkNode from,
            int dx,
            int dy,
            int maxEdgesPerLandmark,
            Dictionary<int, int> edgesPerNode,
            HashSet<long> seen)
        {
            // Guard: se from è già saturo, non facciamo lavoro.
            if (GetCount(edgesPerNode, from.Id) >= maxEdgesPerLandmark)
                return;

            int x = from.CellX;
            int y = from.CellY;
            int steps = 0;

            while (true)
            {
                x += dx;
                y += dy;
                steps++;

                if (!world.InBounds(x, y))
                    return;

                // Se la cella è bloccata, il raggio si ferma.
                if (world.BlocksMovementAt(x, y))
                    return;

                int idx = (y * world.MapWidth) + x;
                if (_activeNodeIdByCellIndex.TryGetValue(idx, out int otherId))
                {
                    // Non colleghiamo a sé stessi.
                    if (otherId == from.Id)
                        return;

                    // Applica cap anche sull'altro nodo.
                    if (GetCount(edgesPerNode, otherId) >= maxEdgesPerLandmark)
                        return;

                    // Dedup (non orientato):
                    int a = Mathf.Min(from.Id, otherId);
                    int b = Mathf.Max(from.Id, otherId);
                    long key = ((long)a << 32) | (uint)b;
                    if (seen.Contains(key))
                        return;

                    seen.Add(key);

                    _edges.Add(new LandmarkEdge
                    {
                        FromNodeId = from.Id,
                        ToNodeId = otherId,
                        CostCells = steps,
                        IsActive = true,
                    });

                    // Incrementiamo contatori per entrambe le estremità.
                    Inc(edgesPerNode, from.Id);
                    Inc(edgesPerNode, otherId);

                    return;
                }
            }
        }

        private static int GetCount(Dictionary<int, int> map, int key)
            => map.TryGetValue(key, out int v) ? v : 0;

        private static void Inc(Dictionary<int, int> map, int key)
        {
            if (!map.TryGetValue(key, out int v)) v = 0;
            map[key] = v + 1;
        }

        // ============================================================
        // RUNTIME QUERIES (World/Systems)
        // ============================================================

        /// <summary>
        /// TryGetActiveNodeIdAtCell:
        /// risolve una cella (x,y) nel nodeId di un landmark attivo.
        ///
        /// Nota:
        /// - Day3 usa questa query per l'apprendimento event-driven (quando l'NPC si muove).
        /// - Non facciamo scanning O(N) dei nodi: usiamo l'indice cell->nodeId costruito in bootstrap.
        /// </summary>
        public bool TryGetActiveNodeIdAtCell(int x, int y, out int nodeId)
        {
            nodeId = 0;

            if (_mapWidthForCellIndex <= 0)
                return false;

            // Nota: qui NON facciamo bounds-check perche' il caller (World/Systems)
            // di norma ha gia' verificato InBounds. Restiamo comunque difensivi.
            if (x < 0 || y < 0)
                return false;

            int idx = (y * _mapWidthForCellIndex) + x;
            return _activeNodeIdByCellIndex.TryGetValue(idx, out nodeId);
        }

        /// <summary>
        /// TryGetActiveEdgeCostCells:
        /// ritorna il costo (in celle) dell'edge tra due nodeId, se esiste ed e' attivo.
        ///
        /// Nota:
        /// - In Day2 gli edge sono creati in bootstrap (scan cardinali fino al prossimo landmark).
        /// - In Day3 l'NPC impara un edge solo se esiste anche nel registry oggettivo.
        ///   (evitiamo di introdurre connessioni "fantasma" dovute a path greedy).
        /// </summary>
        public bool TryGetActiveEdgeCostCells(int nodeA, int nodeB, out int costCells)
        {
            costCells = 0;
            if (nodeA == 0 || nodeB == 0) return false;
            if (nodeA == nodeB) return false;

            // Day2/Day3: O(E) e' accettabile perche' gli edge sono pochi (adiacenza sparsa).
            for (int i = 0; i < _edges.Count; i++)
            {
                var e = _edges[i];
                if (e == null || !e.IsActive) continue;

                if ((e.FromNodeId == nodeA && e.ToNodeId == nodeB) || (e.FromNodeId == nodeB && e.ToNodeId == nodeA))
                {
                    costCells = e.CostCells;
                    return true;
                }
            }

            return false;
        }


        /// <summary>
        /// TryFindNearestActiveNodeId:
        /// risolve la cella arbitraria nel landmark attivo piu' vicino (metrica Manhattan).
        ///
        /// Nota di design:
        /// - per questa feature debug va bene una ricerca O(N): il registry e' piccolo e sparso;
        /// - questo ci consente di dare una destinazione "macro" anche quando il click cade
        ///   su una cella che non e' essa stessa un landmark.
        /// </summary>
        public bool TryFindNearestActiveNodeId(int cellX, int cellY, out int nodeId)
        {
            nodeId = 0;

            int bestDist = int.MaxValue;
            for (int i = 0; i < _nodes.Count; i++)
            {
                var n = _nodes[i];
                if (n == null || !n.IsActive) continue;

                int dist = Mathf.Abs(n.CellX - cellX) + Mathf.Abs(n.CellY - cellY);
                if (dist < bestDist)
                {
                    bestDist = dist;
                    nodeId = n.Id;
                }
            }

            return nodeId != 0;
        }

        /// <summary>
        /// TryGetActiveNodeById:
        /// helper di lookup id->node per planner/overlay debug.
        /// </summary>
        public bool TryGetActiveNodeById(int nodeId, out LandmarkNode node)
        {
            node = null;
            if (nodeId == 0) return false;

            for (int i = 0; i < _nodes.Count; i++)
            {
                var n = _nodes[i];
                if (n == null || !n.IsActive) continue;
                if (n.Id != nodeId) continue;
                node = n;
                return true;
            }

            return false;
        }

        /// <summary>
        /// TryPlanActiveRouteNodeIds:
        /// piccolo planner A* sul grafo dei landmark attivi del registry.
        ///
        /// Scope:
        /// - e' una utility di debug per visualizzare il percorso macro quando ordiniamo
        ///   ad un NPC di andare verso una cella cliccata;
        /// - non sostituisce ancora il Day4 ufficiale job-friendly, ma ci permette di
        ///   verificare subito che il grafo produca route coerenti.
        /// </summary>
        public bool TryPlanActiveRouteNodeIds(int startNodeId, int targetNodeId, List<int> outNodeIds)
        {
            if (outNodeIds == null) return false;
            outNodeIds.Clear();

            if (startNodeId == 0 || targetNodeId == 0)
                return false;

            if (!TryGetActiveNodeById(startNodeId, out var startNode))
                return false;
            if (!TryGetActiveNodeById(targetNodeId, out var targetNode))
                return false;

            if (startNodeId == targetNodeId)
            {
                outNodeIds.Add(startNodeId);
                return true;
            }

            var open = new List<int>(32) { startNodeId };
            var cameFrom = new Dictionary<int, int>(32);
            var gScore = new Dictionary<int, int>(32) { [startNodeId] = 0 };
            var fScore = new Dictionary<int, int>(32) { [startNodeId] = EstimateHeuristicCells(startNode, targetNode) };

            while (open.Count > 0)
            {
                int current = open[0];
                int currentF = fScore.TryGetValue(current, out var cf) ? cf : int.MaxValue;
                for (int i = 1; i < open.Count; i++)
                {
                    int candidate = open[i];
                    int candidateF = fScore.TryGetValue(candidate, out var pf) ? pf : int.MaxValue;
                    if (candidateF < currentF)
                    {
                        current = candidate;
                        currentF = candidateF;
                    }
                }

                if (current == targetNodeId)
                {
                    ReconstructPath(cameFrom, current, outNodeIds);
                    return outNodeIds.Count > 0;
                }

                open.Remove(current);
                int currentG = gScore.TryGetValue(current, out var cg) ? cg : int.MaxValue;

                for (int i = 0; i < _edges.Count; i++)
                {
                    var e = _edges[i];
                    if (e == null || !e.IsActive) continue;

                    int neighbor = 0;
                    if (e.FromNodeId == current) neighbor = e.ToNodeId;
                    else if (e.ToNodeId == current) neighbor = e.FromNodeId;
                    else continue;

                    if (!TryGetActiveNodeById(neighbor, out var neighborNode))
                        continue;

                    int tentativeG = currentG + Mathf.Max(1, e.CostCells);
                    int knownG = gScore.TryGetValue(neighbor, out var gg) ? gg : int.MaxValue;
                    if (tentativeG >= knownG)
                        continue;

                    cameFrom[neighbor] = current;
                    gScore[neighbor] = tentativeG;
                    fScore[neighbor] = tentativeG + EstimateHeuristicCells(neighborNode, targetNode);

                    if (!open.Contains(neighbor))
                        open.Add(neighbor);
                }
            }

            return false;
        }

        private static int EstimateHeuristicCells(LandmarkNode a, LandmarkNode b)
        {
            return Mathf.Abs(a.CellX - b.CellX) + Mathf.Abs(a.CellY - b.CellY);
        }

        private static void ReconstructPath(Dictionary<int, int> cameFrom, int current, List<int> outNodeIds)
        {
            outNodeIds.Clear();
            outNodeIds.Add(current);

            while (cameFrom.TryGetValue(current, out var prev))
            {
                current = prev;
                outNodeIds.Add(current);
            }

            outNodeIds.Reverse();
        }

        // ============================================================
        // VIEW HELPERS
        // ============================================================

        /// <summary>
        /// Converte il registry in un formato view-only (nodi+edges con coordinate).
        ///
        /// Nota:
        /// - In Day2 l'overlay renderizza il registry oggettivo.
        /// - In Day3+ potremo scegliere di renderizzare invece la versione "soggettiva" per NPC.
        /// </summary>
        public void FillOverlayData(List<LandmarkOverlayNode> outNodes, List<LandmarkOverlayEdge> outEdges)
        {
            outNodes?.Clear();
            outEdges?.Clear();

            if (outNodes != null)
            {
                for (int i = 0; i < _nodes.Count; i++)
                {
                    var n = _nodes[i];
                    if (n == null || !n.IsActive) continue;
                    string label = n.Kind == LandmarkKind.Doorway ? $"D#{n.Id}" : $"J#{n.Id}";
                    outNodes.Add(new LandmarkOverlayNode(cellX: n.CellX, cellY: n.CellY, kind: (int)n.Kind, nodeId: n.Id, label: label));
                }
            }

            if (outEdges != null)
            {
                for (int i = 0; i < _edges.Count; i++)
                {
                    var e = _edges[i];
                    if (e == null || !e.IsActive) continue;

                    // Recuperiamo le coordinate dei due nodi.
                    var a = GetNodeById(e.FromNodeId);
                    var b = GetNodeById(e.ToNodeId);
                    if (a == null || b == null) continue;
                    if (!a.IsActive || !b.IsActive) continue;

                    outEdges.Add(new LandmarkOverlayEdge(
                        ax: a.CellX,
                        ay: a.CellY,
                        bx: b.CellX,
                        by: b.CellY,
                        reliability01: 1f));
                }
            }
        }

        public int ActiveNodesCount
        {
            get
            {
                int c = 0;
                for (int i = 0; i < _nodes.Count; i++)
                    if (_nodes[i] != null && _nodes[i].IsActive) c++;
                return c;
            }
        }

        public int ActiveEdgesCount
        {
            get
            {
                int c = 0;
                for (int i = 0; i < _edges.Count; i++)
                    if (_edges[i] != null && _edges[i].IsActive) c++;
                return c;
            }
        }

        private LandmarkNode GetNodeById(int id)
        {
            // Day2: O(N) è ok (pochi landmark). In futuro possiamo aggiungere dictionary id->node.
            for (int i = 0; i < _nodes.Count; i++)
            {
                var n = _nodes[i];
                if (n != null && n.Id == id)
                    return n;
            }
            return null;
        }
    }
}
