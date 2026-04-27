using System;
using System.Collections.Generic;
using UnityEngine;
using Arcontio.Core.Config; // HybridLandmarkParams

namespace Arcontio.Core
{
    /// <summary>
    /// HybridLandmarkExtractor (v0.03.02.a)
    ///
    /// Sistema ibrido di estrazione landmark in 6 passi.
    /// Sostituisce il GVD-DIN come generatore principale di candidati landmark.
    ///
    /// PRINCIPIO ARCHITETTURALE:
    /// Questo extractor è un preprocessore statico eseguito a map-load.
    /// Non tocca la logica NPC, non viola la soggettività della memoria.
    /// Produce candidati LandmarkCandidate che LandmarkRegistry converte
    /// in LandmarkNode tramite AddOrMergeNode.
    ///
    /// PIPELINE:
    ///   Passo 1 — Distance Transform (BFS multi-sorgente)
    ///   Passo 2 — Bridge Detection (strozzature topologiche, DFS iterativa)
    ///   Passo 3 — Flood Fill per regioni (bridge = separatori)
    ///   Passo 4 — Landmark per regione (Tecnica A: massimo DT + Tecnica B: mediana ortogonale)
    ///   Passo 5 — ChokePoint per ogni gruppo di bridge
    ///   Passo 6 — Pruning finale (merge + tipo + area minima)
    ///
    /// OVERLAY DEBUG (v0.03.02.a.2):
    ///   FillOverlaySnapshot() popola un GvdDinOverlaySnapshot riusando
    ///   l'infrastruttura di rendering GVD-DIN già esistente:
    ///   - Layer 1 (DtCells): heatmap DT — identica al GVD-DIN
    ///   - Layer 2 (GvdRawCells): celle bridge — distinguibile dal GVD raw
    ///   - Layer 3 (GvdNodes): candidati finali post-pruning
    ///
    /// COLLOCAZIONE: Systems/Landmarks/ — coerente con NpcLandmarkMemorySystem.
    ///
    /// Nota sui commenti: in italiano per convenzione del progetto ARCONTIO.
    /// </summary>
    public sealed class HybridLandmarkExtractor
    {
        // ============================================================
        // TIPI PUBBLICI
        // ============================================================

        /// <summary>Tipo del landmark candidato prodotto dall'extractor.</summary>
        public enum LandmarkType
        {
            /// <summary>
            /// Centro geometrico di una regione (stanza, area aperta).
            /// Corrisponde a LandmarkKind.AreaCenter nel registry.
            /// </summary>
            RoomCenter,

            /// <summary>
            /// Strozzatura topologica (porta, corridoio stretto, collo di bottiglia).
            /// Corrisponde a LandmarkKind.Junction nel registry.
            /// </summary>
            ChokePoint
        }

        /// <summary>Candidato landmark prodotto dalla pipeline.</summary>
        public struct LandmarkCandidate
        {
            /// <summary>Posizione in coordinate cella.</summary>
            public Vector2Int Position;

            /// <summary>Tipo: RoomCenter o ChokePoint.</summary>
            public LandmarkType Type;

            /// <summary>
            /// Valore DT in questa cella — distanza al muro più vicino.
            /// Utile per debug e per il merge (vince chi ha DT più alta).
            /// </summary>
            public int DtValue;
        }

        // ============================================================
        // STATO INTERNO (v0.03.02.a.2 — per il debug overlay)
        // ============================================================
        // Salvato dopo l'ultima chiamata a Extract() per FillOverlaySnapshot().
        // Riusato tra frame senza riallocazioni.

        private int[,]  _lastDt;       // Distance Transform dell'ultima mappa
        private bool[,] _lastBridge;   // celle bridge dell'ultima mappa
        private bool[,] _lastWalkable; // griglia walkability dell'ultima mappa
        private int     _lastWidth;
        private int     _lastHeight;
        private int     _lastDtMax;
        private int     _lastDtOpenThreshold; // soglia usata nell'ultima Extract()

        // Candidati finali post-pruning dell'ultima Extract()
        private readonly List<LandmarkCandidate> _lastCandidates = new List<LandmarkCandidate>(64);

        // ============================================================
        // METODO PRINCIPALE
        // ============================================================

        /// <summary>
        /// Esegue la pipeline completa e restituisce la lista di candidati landmark.
        /// Salva i dati intermedi (DT, bridge, candidati) per FillOverlaySnapshot().
        ///
        /// Chiamato da LandmarkRegistry.RebuildFromWorld quando
        /// hybrid_landmark.use_hybrid_extractor = true.
        /// </summary>
        public List<LandmarkCandidate> Extract(
            bool[,] walkable,
            int width,
            int height,
            HybridLandmarkParams p)
        {
            _lastCandidates.Clear();

            if (walkable == null || width <= 0 || height <= 0 || p == null)
                return _lastCandidates;

            _lastWidth  = width;
            _lastHeight = height;
            _lastWalkable = walkable;
            _lastDtOpenThreshold = p.dt_open_threshold;

            // Passo 1: Distance Transform
            _lastDt = ComputeDistanceTransform(walkable, width, height);

            // Calcola DtMax per normalizzazione overlay
            _lastDtMax = 0;
            for (int y = 0; y < height; y++)
                for (int x = 0; x < width; x++)
                    if (_lastDt[x, y] > _lastDtMax) _lastDtMax = _lastDt[x, y];

            // Passo 2: Bridge Detection
            _lastBridge = DetectBridges(walkable, width, height, _lastDt, p.dt_open_threshold);

            // Passo 3: Flood Fill per regioni
            int[,] regionId = FloodFillRegions(walkable, _lastBridge, width, height);

            // Passi 4+5: candidati per regione e per strozzatura
            ExtractRegionCandidates(walkable, _lastDt, regionId, width, height, p, _lastCandidates);
            ExtractChokePointCandidates(walkable, _lastBridge, width, height, _lastDt, _lastCandidates);

            // Passo 6: Pruning
            Prune(_lastCandidates, p.merge_radius, p.min_region_area, _lastDt, width, height, regionId);

            return _lastCandidates;
        }

        // ============================================================
        // OVERLAY DEBUG (v0.03.02.a.6_vis)
        // ============================================================

        /// <summary>
        /// Visualizza il grafo contratto del Passo 2:
        ///
        /// Layer 1 — DtCells (rosso/caldo): ZONE APERTE (DT >= dt_open_threshold).
        ///   Sono i super-nodi del grafo contratto — le stanze.
        ///   normalized=1.0 → colore caldo uniforme, distinguibile dalle zone strette.
        ///
        /// Layer 2 — GvdRawCells (ciano): ZONE STRETTE (DT < dt_open_threshold).
        ///   Sono gli archi candidati del grafo contratto — i corridoi.
        ///   Se un corridoio è bridge topologico → le sue celle diventano ChokePoint.
        ///
        /// Layer 3 — GvdNodes (viola): candidati finali post-pruning.
        ///   HR# = RoomCenter (centro stanza), HC# = ChokePoint (strozzatura bridge).
        /// </summary>
        public void FillOverlaySnapshot(GvdDinOverlaySnapshot snapshot)
        {
            if (snapshot == null) return;
            snapshot.Clear();

            if (_lastDt == null || _lastWalkable == null ||
                _lastWidth <= 0 || _lastHeight <= 0) return;

            // Layer 1: zone aperte (super-nodi) → DtCells con normalized=1.0 (rosso/caldo)
            for (int y = 0; y < _lastHeight; y++)
                for (int x = 0; x < _lastWidth; x++)
                {
                    if (!_lastWalkable[x, y]) continue;
                    int dt = _lastDt[x, y];
                    if (dt < _lastDtOpenThreshold) continue;
                    snapshot.DtCells.Add(new GvdDinOverlayCellDt(x, y, dt, 1.0f));
                }

            // Layer 2: zone strette (archi corridoio) → GvdRawCells (ciano)
            for (int y = 0; y < _lastHeight; y++)
                for (int x = 0; x < _lastWidth; x++)
                {
                    if (!_lastWalkable[x, y]) continue;
                    if (_lastDt[x, y] >= _lastDtOpenThreshold) continue;
                    snapshot.GvdRawCells.Add(new GvdDinOverlayCellGvd(x, y));
                }

            // Layer 3: candidati finali
            for (int i = 0; i < _lastCandidates.Count; i++)
            {
                var c = _lastCandidates[i];
                bool isChoke = c.Type == LandmarkType.ChokePoint;
                string label = isChoke ? $"HC#{i}(dt={c.DtValue})" : $"HR#{i}(dt={c.DtValue})";
                int kind = isChoke ? 2 : 3;
                snapshot.GvdNodes.Add(new LandmarkOverlayNode(
                    cellX: c.Position.x, cellY: c.Position.y,
                    kind: kind, nodeId: i, label: label));
            }

            snapshot.IsValid = true;
        }


        // ============================================================
        // PASSO 1 — DISTANCE TRANSFORM
        // ============================================================

        /// <summary>
        /// BFS multi-sorgente dai muri.
        /// Ogni cella walkable riceve la distanza cardinale al muro più vicino.
        ///
        /// Celle muro → DT = 0.
        /// Celle walkable → DT = distanza minima cardinale al primo muro.
        ///
        /// Complessità: O(W × H) — un solo passaggio BFS.
        /// Metrica: Manhattan (4 direzioni cardinali).
        /// </summary>
        private int[,] ComputeDistanceTransform(bool[,] walkable, int w, int h)
        {
            var dt = new int[w, h];
            var queue = new Queue<Vector2Int>(w * h / 2);

            // Inizializzazione: muri = 0, walkable = sentinel alto
            for (int y = 0; y < h; y++)
                for (int x = 0; x < w; x++)
                    dt[x, y] = walkable[x, y] ? int.MaxValue : 0;

            // Semina: celle walkable adiacenti a un muro → DT=1, entrano in coda
            for (int y = 0; y < h; y++)
            {
                for (int x = 0; x < w; x++)
                {
                    if (!walkable[x, y]) continue;
                    if (HasWallNeighbor(walkable, x, y, w, h))
                    {
                        dt[x, y] = 1;
                        queue.Enqueue(new Vector2Int(x, y));
                    }
                }
            }

            // BFS espansione
            while (queue.Count > 0)
            {
                var cur = queue.Dequeue();
                int nextDt = dt[cur.x, cur.y] + 1;

                foreach (var nb in Cardinals(cur.x, cur.y, w, h))
                {
                    if (!walkable[nb.x, nb.y]) continue;
                    if (nextDt < dt[nb.x, nb.y])
                    {
                        dt[nb.x, nb.y] = nextDt;
                        queue.Enqueue(nb);
                    }
                }
            }

            // Normalizza: celle walkable non raggiunte (isole) → 0
            for (int y = 0; y < h; y++)
                for (int x = 0; x < w; x++)
                    if (dt[x, y] == int.MaxValue) dt[x, y] = 0;

            return dt;
        }

        // ============================================================
        // PASSO 2 — BRIDGE DETECTION (grafo contratto, v0.03.02.a.6)
        // ============================================================

        /// <summary>
        /// Trova le celle bridge usando un grafo contratto DT-based.
        ///
        /// APPROCCIO:
        /// Invece di Tarjan su celle singole (che non trova bridge in corridoi
        /// larghi 2+ celle perché ogni cella ha sempre un percorso alternativo),
        /// costruiamo prima un grafo contratto dove:
        ///   - Nodi = zone aperte (DT >= dt_open_threshold): le stanze
        ///   - Archi = zone strette (DT <  dt_open_threshold): i corridoi
        ///
        /// Tarjan viene eseguito su questo grafo contratto (pochi nodi/archi).
        /// Un arco è bridge se il corridoio è l'unico percorso tra due stanze.
        /// Le celle del corridoio bridge → marcate come bridge.
        ///
        /// PARAMETRO dt_open_threshold:
        ///   Separa "zona aperta" (stanza) da "zona stretta" (corridoio).
        ///   Valore 2: celle con DT >= 2 sono stanza, DT < 2 sono corridoio.
        ///   Su ARCONTIO (corridoi 2-3c): DT_corridoio=1, DT_stanza>=3 → threshold=2.
        ///   Alzare → soglia più alta, include più celle come "stanza".
        ///   Abbassare → soglia più bassa, include più celle come "corridoio".
        ///
        /// PIPELINE:
        ///   Step A — Flood fill zone aperte    → superNodId[x,y] = ID stanza (-1 se stretta)
        ///   Step B — Flood fill zone strette   → narrowId[x,y]   = ID corridoio (-1 se aperta)
        ///   Step C — Costruisce archi contratti: narrowId → { superNodo A, superNodo B }
        ///   Step D — Tarjan sul grafo contratto → archi bridge
        ///   Step E — Celle degli archi bridge  → isBridge[x,y] = true
        /// </summary>
        private bool[,] DetectBridges(bool[,] walkable, int w, int h,
                                      int[,] dt, int dtOpenThreshold)
        {
            var isBridge = new bool[w, h];

            // Step A: flood fill zone aperte (DT >= threshold) → super-nodi
            // Ogni zona connessa con DT alta è una "stanza" = un super-nodo.
            var superNodeId = new int[w, h];
            for (int y = 0; y < h; y++)
                for (int x = 0; x < w; x++)
                    superNodeId[x, y] = -1;

            int nSuperNodes = 0;
            var floodQueue = new Queue<Vector2Int>(256);

            for (int sy = 0; sy < h; sy++)
            {
                for (int sx = 0; sx < w; sx++)
                {
                    if (!walkable[sx, sy]) continue;
                    if (dt[sx, sy] < dtOpenThreshold) continue; // zona stretta
                    if (superNodeId[sx, sy] >= 0) continue;     // già visitata

                    int id = nSuperNodes++;
                    superNodeId[sx, sy] = id;
                    floodQueue.Enqueue(new Vector2Int(sx, sy));

                    while (floodQueue.Count > 0)
                    {
                        var cur = floodQueue.Dequeue();
                        foreach (var nb in Cardinals(cur.x, cur.y, w, h))
                        {
                            if (!walkable[nb.x, nb.y]) continue;
                            if (dt[nb.x, nb.y] < dtOpenThreshold) continue;
                            if (superNodeId[nb.x, nb.y] >= 0) continue;
                            superNodeId[nb.x, nb.y] = id;
                            floodQueue.Enqueue(nb);
                        }
                    }
                }
            }

            if (nSuperNodes < 2) return isBridge; // mappa degenere: nessun bridge possibile

            // Step B: flood fill zone strette (DT < threshold) → gruppi corridoio
            // Ogni zona connessa con DT bassa è un "corridoio" = un arco candidato.
            var narrowId = new int[w, h];
            for (int y = 0; y < h; y++)
                for (int x = 0; x < w; x++)
                    narrowId[x, y] = -1;

            int nNarrow = 0;

            for (int sy = 0; sy < h; sy++)
            {
                for (int sx = 0; sx < w; sx++)
                {
                    if (!walkable[sx, sy]) continue;
                    if (dt[sx, sy] >= dtOpenThreshold) continue; // zona aperta
                    if (narrowId[sx, sy] >= 0) continue;

                    int id = nNarrow++;
                    narrowId[sx, sy] = id;
                    floodQueue.Enqueue(new Vector2Int(sx, sy));

                    while (floodQueue.Count > 0)
                    {
                        var cur = floodQueue.Dequeue();
                        foreach (var nb in Cardinals(cur.x, cur.y, w, h))
                        {
                            if (!walkable[nb.x, nb.y]) continue;
                            if (dt[nb.x, nb.y] >= dtOpenThreshold) continue;
                            if (narrowId[nb.x, nb.y] >= 0) continue;
                            narrowId[nb.x, nb.y] = id;
                            floodQueue.Enqueue(nb);
                        }
                    }
                }
            }

            if (nNarrow == 0) return isBridge; // nessun corridoio: nessun bridge

            // Step C: per ogni gruppo stretto, trova i super-nodi che tocca
            // Un gruppo stretto tocca un super-nodo se ha almeno una cella
            // adiacente a una cella di quel super-nodo.
            var narrowConnects = new HashSet<int>[nNarrow];
            for (int i = 0; i < nNarrow; i++)
                narrowConnects[i] = new HashSet<int>(4);

            for (int y = 0; y < h; y++)
            {
                for (int x = 0; x < w; x++)
                {
                    int ng = narrowId[x, y];
                    if (ng < 0) continue;

                    foreach (var nb in Cardinals(x, y, w, h))
                    {
                        int sn = superNodeId[nb.x, nb.y];
                        if (sn >= 0) narrowConnects[ng].Add(sn);
                    }
                }
            }

            // Step D: Tarjan sul grafo contratto
            // Lista di adiacenza: adj[superNodo] = lista di (superNodoVicino, narrowId)
            var adj = new List<(int neighbor, int edgeId)>[nSuperNodes];
            for (int i = 0; i < nSuperNodes; i++)
                adj[i] = new List<(int, int)>(4);

            for (int ng = 0; ng < nNarrow; ng++)
            {
                var connected = new List<int>(narrowConnects[ng]);
                // Aggiunge archi tra tutte le coppie di super-nodi connesse da questo corridoio
                for (int i = 0; i < connected.Count; i++)
                    for (int j = i + 1; j < connected.Count; j++)
                    {
                        adj[connected[i]].Add((connected[j], ng));
                        adj[connected[j]].Add((connected[i], ng));
                    }
            }

            // Tarjan iterativo sul grafo contratto.
            // Stack contiene (nodo_corrente, indice_vicino).
            // parent_node e parent_edge tengono traccia del cammino DFS.
            var disc2       = new int[nSuperNodes];
            var low2        = new int[nSuperNodes];
            var vis2        = new bool[nSuperNodes];
            var parentNode  = new int[nSuperNodes];
            var parentEdge  = new int[nSuperNodes];
            var bridgeEdges = new HashSet<int>();

            for (int i = 0; i < nSuperNodes; i++)
            {
                disc2[i]      = -1;
                parentNode[i] = -1;
                parentEdge[i] = -1;
            }

            int timer2 = 0;

            for (int startNode = 0; startNode < nSuperNodes; startNode++)
            {
                if (vis2[startNode]) continue;

                // Stack: (nodo, indice vicino corrente)
                var stack2 = new Stack<(int u, int ci)>();
                stack2.Push((startNode, 0));
                vis2[startNode] = true;
                disc2[startNode] = low2[startNode] = timer2++;

                while (stack2.Count > 0)
                {
                    var (u, ci) = stack2.Pop();

                    if (ci < adj[u].Count)
                    {
                        // Rimetti nodo con ci+1 per continuare dopo
                        stack2.Push((u, ci + 1));

                        var (v, eid) = adj[u][ci];

                        if (!vis2[v])
                        {
                            // Tree edge: visita il vicino
                            vis2[v]        = true;
                            disc2[v]       = low2[v] = timer2++;
                            parentNode[v]  = u;
                            parentEdge[v]  = eid;
                            stack2.Push((v, 0));
                        }
                        else if (eid != parentEdge[u])
                        {
                            // Back edge: aggiorna low[u]
                            if (disc2[v] < low2[u])
                                low2[u] = disc2[v];
                        }
                    }
                    else
                    {
                        // Nodo completato: propaga low al parent e verifica bridge
                        int p  = parentNode[u];
                        int pe = parentEdge[u];
                        if (p >= 0)
                        {
                            if (low2[u] < low2[p]) low2[p] = low2[u];
                            // Bridge: low[child] > disc[parent]
                            if (low2[u] > disc2[p]) bridgeEdges.Add(pe);
                        }
                    }
                }
            }

            // Step E: marca come bridge tutte le celle dei corridoi bridge
            if (bridgeEdges.Count > 0)
            {
                for (int y = 0; y < h; y++)
                    for (int x = 0; x < w; x++)
                        if (narrowId[x, y] >= 0 && bridgeEdges.Contains(narrowId[x, y]))
                            isBridge[x, y] = true;
            }

            return isBridge;
        }


        // ============================================================
        // PASSO 3 — FLOOD FILL PER REGIONI
        // ============================================================

        /// <summary>
        /// Segmenta la mappa in regioni usando i bridge come separatori virtuali.
        ///
        /// BFS da ogni cella walkable non-bridge non ancora visitata.
        /// Ogni BFS produce una regione con ID intero incrementale (da 0).
        ///
        /// Output:
        ///   -1 = muro o bridge (non appartiene a nessuna regione)
        ///   >= 0 = ID regione
        ///
        /// Questo è il passo che trasforma il problema da "quanti LM per cella"
        /// a "quanti LM per regione" — permettendo 1-2 LM per regione.
        /// </summary>
        private int[,] FloodFillRegions(bool[,] walkable, bool[,] isBridge,
                                               int w, int h)
        {
            var regionId = new int[w, h];
            for (int y = 0; y < h; y++)
                for (int x = 0; x < w; x++)
                    regionId[x, y] = -1;

            int nextId = 0;
            var queue = new Queue<Vector2Int>(256);

            for (int startY = 0; startY < h; startY++)
            {
                for (int startX = 0; startX < w; startX++)
                {
                    if (!walkable[startX, startY]) continue;
                    if (isBridge[startX, startY]) continue;
                    if (regionId[startX, startY] >= 0) continue;

                    // Nuova regione
                    int id = nextId++;
                    regionId[startX, startY] = id;
                    queue.Enqueue(new Vector2Int(startX, startY));

                    while (queue.Count > 0)
                    {
                        var cur = queue.Dequeue();
                        foreach (var nb in Cardinals(cur.x, cur.y, w, h))
                        {
                            if (!walkable[nb.x, nb.y]) continue;
                            if (isBridge[nb.x, nb.y]) continue;
                            if (regionId[nb.x, nb.y] >= 0) continue;

                            regionId[nb.x, nb.y] = id;
                            queue.Enqueue(nb);
                        }
                    }
                }
            }

            return regionId;
        }

        // ============================================================
        // PASSO 4 — LANDMARK PER REGIONE
        // ============================================================

        /// <summary>
        /// Per ogni regione genera 1-2 candidati RoomCenter usando due tecniche:
        ///
        /// Tecnica A — Massimo DT con regola segmento:
        ///   Trova il DT massimo nella regione. Raccoglie le celle con quel valore.
        ///   Le raggruppa in segmenti connessi. Per ogni segmento:
        ///   - blob (larghezza > 1 E altezza > 1) → centroide
        ///   - lineare → cella centrale (index Count/2)
        ///   Funziona bene per stanze convesse regolari.
        ///
        /// Tecnica B — Mediana ortogonale:
        ///   Per ogni cella della regione, lancia raggi nelle 4 direzioni.
        ///   Candidata se |dx_left - dx_right| <= tolerance O |dy_up - dy_down| <= tolerance.
        ///   Raggruppa le candidate vicine (entro 2 celle) e prende il centroide.
        ///   Funziona bene per stanze a L, C, U e forme irregolari.
        ///
        /// I risultati di A e B vengono combinati — il pruning al Passo 6 deduplica.
        /// </summary>
        private void ExtractRegionCandidates(
            bool[,] walkable, int[,] dt, int[,] regionId,
            int w, int h,
            HybridLandmarkParams p,
            List<LandmarkCandidate> output)
        {
            // Raggruppa celle per regione
            var regionCells = new Dictionary<int, List<Vector2Int>>();
            for (int y = 0; y < h; y++)
            {
                for (int x = 0; x < w; x++)
                {
                    int rid = regionId[x, y];
                    if (rid < 0) continue;
                    if (!regionCells.ContainsKey(rid))
                        regionCells[rid] = new List<Vector2Int>();
                    regionCells[rid].Add(new Vector2Int(x, y));
                }
            }

            foreach (var kv in regionCells)
            {
                var cells = kv.Value;
                if (cells.Count < p.min_region_area) continue;

                // Tecnica A
                ExtractTechniqueA(cells, dt, output);

                // Tecnica B
                ExtractTechniqueB(cells, walkable, dt, w, h, p.median_tolerance, output);
            }
        }

        /// <summary>
        /// Tecnica A: massimo DT + regola segmento.
        /// Produce al massimo 1 candidato per cluster di massimi.
        /// </summary>
        private void ExtractTechniqueA(
            List<Vector2Int> cells, int[,] dt,
            List<LandmarkCandidate> output)
        {
            // Trova DT massimo nella regione
            int dtMax = 0;
            foreach (var c in cells)
                if (dt[c.x, c.y] > dtMax) dtMax = dt[c.x, c.y];

            if (dtMax <= 0) return;

            // Raccoglie celle con DT massimo
            var maxCells = new List<Vector2Int>();
            foreach (var c in cells)
                if (dt[c.x, c.y] == dtMax) maxCells.Add(c);

            // Raggruppa i massimi in segmenti connessi (BFS)
            var visited = new HashSet<int>();
            var queue   = new Queue<Vector2Int>();

            foreach (var start in maxCells)
            {
                int key = start.y * 10000 + start.x;
                if (visited.Contains(key)) continue;

                var segment = new List<Vector2Int>();
                queue.Enqueue(start);
                visited.Add(key);

                while (queue.Count > 0)
                {
                    var cur = queue.Dequeue();
                    segment.Add(cur);

                    // Vicini 8-connessi tra le celle massimo
                    for (int dy = -1; dy <= 1; dy++)
                    {
                        for (int dx = -1; dx <= 1; dx++)
                        {
                            if (dx == 0 && dy == 0) continue;
                            var nb = new Vector2Int(cur.x + dx, cur.y + dy);
                            int nk = nb.y * 10000 + nb.x;
                            if (visited.Contains(nk)) continue;
                            if (!maxCells.Contains(nb)) continue;
                            visited.Add(nk);
                            queue.Enqueue(nb);
                        }
                    }
                }

                // Determina se il segmento è un blob o è lineare
                int minX = int.MaxValue, maxX = int.MinValue;
                int minY = int.MaxValue, maxY = int.MinValue;
                foreach (var c in segment)
                {
                    if (c.x < minX) minX = c.x;
                    if (c.x > maxX) maxX = c.x;
                    if (c.y < minY) minY = c.y;
                    if (c.y > maxY) maxY = c.y;
                }

                Vector2Int chosen;
                bool isBlob = (maxX - minX > 1) && (maxY - minY > 1);
                if (isBlob)
                {
                    // Blob → centroide
                    int cx = 0, cy = 0;
                    foreach (var c in segment) { cx += c.x; cy += c.y; }
                    chosen = new Vector2Int(cx / segment.Count, cy / segment.Count);
                }
                else
                {
                    // Lineare → cella centrale
                    chosen = segment[segment.Count / 2];
                }

                output.Add(new LandmarkCandidate
                {
                    Position = chosen,
                    Type     = LandmarkType.RoomCenter,
                    DtValue  = dtMax
                });
            }
        }

        /// <summary>
        /// Tecnica B: mediana ortogonale.
        /// Candidata se bilanciata orizzontalmente O verticalmente.
        /// Funziona bene per forme concave (L, C, U).
        /// </summary>
        private void ExtractTechniqueB(
            List<Vector2Int> cells, bool[,] walkable, int[,] dt,
            int w, int h, int tolerance,
            List<LandmarkCandidate> output)
        {
            var medians = new List<Vector2Int>();

            foreach (var c in cells)
            {
                // Raggio a sinistra/destra
                int left  = RayLength(walkable, c.x, c.y, -1, 0, w, h);
                int right = RayLength(walkable, c.x, c.y,  1, 0, w, h);
                // Raggio su/giù
                int up    = RayLength(walkable, c.x, c.y, 0,  1, w, h);
                int down  = RayLength(walkable, c.x, c.y, 0, -1, w, h);

                bool balancedH = Mathf.Abs(left - right) <= tolerance;
                bool balancedV = Mathf.Abs(up   - down)  <= tolerance;

                if (balancedH || balancedV)
                    medians.Add(c);
            }

            if (medians.Count == 0) return;

            // Raggruppa le mediane vicine (entro 2 celle Manhattan) e prende il centroide
            var visitedM = new HashSet<int>();
            var queueM   = new Queue<Vector2Int>();

            foreach (var start in medians)
            {
                int key = start.y * 10000 + start.x;
                if (visitedM.Contains(key)) continue;

                var group = new List<Vector2Int>();
                queueM.Enqueue(start);
                visitedM.Add(key);

                while (queueM.Count > 0)
                {
                    var cur = queueM.Dequeue();
                    group.Add(cur);

                    foreach (var nb in medians)
                    {
                        int nk = nb.y * 10000 + nb.x;
                        if (visitedM.Contains(nk)) continue;
                        if (Mathf.Abs(nb.x - cur.x) + Mathf.Abs(nb.y - cur.y) > 2) continue;
                        visitedM.Add(nk);
                        queueM.Enqueue(nb);
                    }
                }

                // Centroide del gruppo
                int cx = 0, cy = 0;
                int maxDt = 0;
                foreach (var c in group)
                {
                    cx += c.x; cy += c.y;
                    if (dt[c.x, c.y] > maxDt) maxDt = dt[c.x, c.y];
                }

                output.Add(new LandmarkCandidate
                {
                    Position = new Vector2Int(cx / group.Count, cy / group.Count),
                    Type     = LandmarkType.RoomCenter,
                    DtValue  = maxDt
                });
            }
        }

        /// <summary>
        /// Lancia un raggio in direzione (dx,dy) finché non incontra un muro.
        /// Restituisce la lunghezza del raggio in celle.
        /// </summary>
        private int RayLength(bool[,] walkable, int x, int y,
                                     int dx, int dy, int w, int h)
        {
            int len = 0;
            int nx = x + dx, ny = y + dy;
            while (nx >= 0 && nx < w && ny >= 0 && ny < h && walkable[nx, ny])
            {
                len++;
                nx += dx;
                ny += dy;
            }
            return len;
        }

        // ============================================================
        // PASSO 5 — CHOKE POINT
        // ============================================================

        /// <summary>
        /// Per ogni gruppo connesso di celle bridge, calcola un ChokePoint.
        ///
        /// Un gruppo bridge è un insieme connesso di celle marcate come bridge.
        /// Il ChokePoint è posizionato al centroide del gruppo.
        /// Se il centroide non è walkable, si usa il punto bridge più vicino al centroide.
        ///
        /// I ChokePoint corrispondono a porte, corridoi stretti, colli di bottiglia.
        /// </summary>
        private void ExtractChokePointCandidates(
            bool[,] walkable, bool[,] isBridge,
            int w, int h, int[,] dt,
            List<LandmarkCandidate> output)
        {
            var visited = new bool[w, h];
            var queue   = new Queue<Vector2Int>();

            for (int startY = 0; startY < h; startY++)
            {
                for (int startX = 0; startX < w; startX++)
                {
                    if (!isBridge[startX, startY]) continue;
                    if (visited[startX, startY]) continue;

                    // Esplora il gruppo bridge connesso (4-connesso)
                    var group = new List<Vector2Int>();
                    queue.Enqueue(new Vector2Int(startX, startY));
                    visited[startX, startY] = true;

                    while (queue.Count > 0)
                    {
                        var cur = queue.Dequeue();
                        group.Add(cur);

                        foreach (var nb in Cardinals(cur.x, cur.y, w, h))
                        {
                            if (!isBridge[nb.x, nb.y]) continue;
                            if (visited[nb.x, nb.y]) continue;
                            visited[nb.x, nb.y] = true;
                            queue.Enqueue(nb);
                        }
                    }

                    // Centroide del gruppo
                    int cx = 0, cy = 0;
                    foreach (var c in group) { cx += c.x; cy += c.y; }
                    var centroid = new Vector2Int(cx / group.Count, cy / group.Count);

                    // Se il centroide non è walkable, usa il bridge più vicino
                    Vector2Int chosen = centroid;
                    if (!walkable[centroid.x, centroid.y])
                    {
                        int bestDist = int.MaxValue;
                        foreach (var c in group)
                        {
                            if (!walkable[c.x, c.y]) continue;
                            int d = Mathf.Abs(c.x - centroid.x) + Mathf.Abs(c.y - centroid.y);
                            if (d < bestDist) { bestDist = d; chosen = c; }
                        }
                    }

                    // Verifica che chosen sia walkable (potrebbe non esserlo se tutti i bridge non lo sono)
                    if (!walkable[chosen.x, chosen.y]) continue;

                    output.Add(new LandmarkCandidate
                    {
                        Position = chosen,
                        Type     = LandmarkType.ChokePoint,
                        DtValue  = dt[chosen.x, chosen.y]
                    });
                }
            }
        }

        // ============================================================
        // PASSO 6 — PRUNING
        // ============================================================

        /// <summary>
        /// Riduce e consolida i candidati applicando tre regole in ordine:
        ///
        /// Regola 1 — Distanza (merge_radius):
        ///   Se due candidati sono entro merge_radius celle (Manhattan),
        ///   tieni quello con DT più alta. In caso di parità: ChokePoint > RoomCenter.
        ///
        /// Regola 2 — Tipo:
        ///   Nella merge, ChokePoint vince sempre su RoomCenter.
        ///   Un ChokePoint non viene mai sostituito da un RoomCenter vicino.
        ///
        /// Regola 3 — Area minima:
        ///   Scarta candidati in regioni con meno di min_region_area celle.
        ///   (Già applicato al Passo 4, qui come safeguard.)
        ///
        /// Iterazione finché non ci sono più modifiche.
        /// </summary>
        private void Prune(
            List<LandmarkCandidate> candidates,
            float mergeRadius, int minRegionArea, int[,] dt,
            int w, int h, int[,] regionId)
        {
            bool changed = true;
            while (changed)
            {
                changed = false;

                for (int i = 0; i < candidates.Count; i++)
                {
                    for (int j = i + 1; j < candidates.Count; j++)
                    {
                        var a = candidates[i];
                        var b = candidates[j];

                        float dist = Mathf.Abs(a.Position.x - b.Position.x)
                                   + Mathf.Abs(a.Position.y - b.Position.y);
                        if (dist > mergeRadius) continue;

                        // Decide quale tenere:
                        // ChokePoint > RoomCenter; in parità, DT più alta.
                        bool removeA = ShouldRemove(a, b);
                        if (removeA)
                        {
                            candidates.RemoveAt(i);
                            i--;
                            changed = true;
                            break;
                        }
                        else
                        {
                            candidates.RemoveAt(j);
                            j--;
                            changed = true;
                        }
                    }
                }
            }
        }

        /// <summary>
        /// Restituisce true se 'a' deve essere rimosso a favore di 'b'.
        /// Regola: ChokePoint batte RoomCenter; a parità tipo, vince DT più alta.
        /// </summary>
        private bool ShouldRemove(LandmarkCandidate a, LandmarkCandidate b)
        {
            if (a.Type == LandmarkType.ChokePoint && b.Type == LandmarkType.RoomCenter)
                return false; // a è ChokePoint → a vince
            if (a.Type == LandmarkType.RoomCenter && b.Type == LandmarkType.ChokePoint)
                return true;  // b è ChokePoint → rimuovi a
            return a.DtValue < b.DtValue; // stesso tipo → vince DT più alta
        }

        // ============================================================
        // UTILITÀ
        // ============================================================

        /// <summary>Restituisce i vicini cardinali validi di (x,y).</summary>
        private IEnumerable<Vector2Int> Cardinals(int x, int y, int w, int h)
        {
            if (x + 1 < w) yield return new Vector2Int(x + 1, y);
            if (x - 1 >= 0) yield return new Vector2Int(x - 1, y);
            if (y + 1 < h) yield return new Vector2Int(x, y + 1);
            if (y - 1 >= 0) yield return new Vector2Int(x, y - 1);
        }

        /// <summary>Versione List di Cardinals (necessaria per accesso per indice nel DFS).</summary>
        private List<Vector2Int> CardinalsList(int x, int y, int w, int h)
        {
            var list = new List<Vector2Int>(4);
            if (x + 1 < w) list.Add(new Vector2Int(x + 1, y));
            if (x - 1 >= 0) list.Add(new Vector2Int(x - 1, y));
            if (y + 1 < h) list.Add(new Vector2Int(x, y + 1));
            if (y - 1 >= 0) list.Add(new Vector2Int(x, y - 1));
            return list;
        }

        /// <summary>True se la cella (x,y) ha almeno un vicino cardinale non-walkable.</summary>
        private bool HasWallNeighbor(bool[,] walkable, int x, int y, int w, int h)
        {
            if (x + 1 < w  && !walkable[x + 1, y]) return true;
            if (x - 1 >= 0 && !walkable[x - 1, y]) return true;
            if (y + 1 < h  && !walkable[x, y + 1]) return true;
            if (y - 1 >= 0 && !walkable[x, y - 1]) return true;
            // Bordo mappa = muro implicito
            if (x == 0 || x == w - 1 || y == 0 || y == h - 1) return true;
            return false;
        }
    }
}
