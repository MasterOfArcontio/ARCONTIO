using System.Collections.Generic;
using UnityEngine;
using Arcontio.Core;

namespace Arcontio.View.MapGrid
{
    /// <summary>
    /// Debug overlay per il sistema Landmark.
    ///
    /// Patch 0.03.01.h:
    /// - Rimosso interamente il sistema TextMesh label (illeggibile a qualsiasi scala).
    /// - Le etichette sono ora gestite da MapGridLandmarkLabelOverlay (Canvas UI).
    /// - Esposti WorldNodes / KnownNodes / RouteNodes / GvdNodes come IReadOnlyList
    ///   per il label overlay.
    /// - Mantenuto il sistema GVD-DIN overlay (nodi viola, edge, heatmap DT, GVD raw).
    /// </summary>
    public sealed class MapGridLandmarkOverlay
    {
        private Transform _root;
        private float     _tileSizeWorld;
        private int       _sortingOrder;
        private Sprite    _nodeSprite;
        private Material  _lineMaterial;
        private bool      _enabled;

        // ============================================================
        // POOL NODI E EDGE (layer WORLD / KNOWN / ROUTE / PATH)
        // ============================================================

        private readonly List<SpriteRenderer>      _worldNodePool = new();
        private readonly List<LineRenderer>        _worldEdgePool = new();
        private readonly List<LandmarkOverlayNode> _worldNodes    = new();
        private readonly List<LandmarkOverlayEdge> _worldEdges    = new();

        private readonly List<SpriteRenderer>      _knownNodePool = new();
        private readonly List<LineRenderer>        _knownEdgePool = new();
        private readonly List<LandmarkOverlayNode> _knownNodes    = new();
        private readonly List<LandmarkOverlayEdge> _knownEdges    = new();

        private readonly List<SpriteRenderer>      _routeNodePool = new();
        private readonly List<LineRenderer>        _routeEdgePool = new();
        private readonly List<LandmarkOverlayNode> _routeNodes    = new();
        private readonly List<LandmarkOverlayEdge> _routeEdges    = new();

        private readonly List<LineRenderer>        _lmPathEdgePool    = new();
        private readonly List<LandmarkOverlayEdge> _lmPathEdges       = new();
        private readonly List<LineRenderer>        _directPathEdgePool = new();
        private readonly List<LandmarkOverlayEdge> _directPathEdges   = new();
        private readonly List<LineRenderer>        _jumpPathEdgePool  = new();
        private readonly List<LandmarkOverlayEdge> _jumpPathEdges     = new();

        // v0.03.04.c-ComplexEdge_Creation: edge soggettivi fisicamente percorsi (giallo)
        private readonly List<LineRenderer>        _complexEdgePool = new();
        private readonly List<LandmarkOverlayEdge> _complexEdges    = new();

        // ============================================================
        // POOL GVD-DIN (v0.03 — patch 0.03.01.a)
        // ============================================================

        // Layer 1 — DT heatmap
        private readonly Dictionary<int, SpriteRenderer> _dtCellRenderers = new(1024);
        private readonly Stack<SpriteRenderer>           _dtCellPool       = new(1024);
        private readonly List<int>                       _dtLastActiveKeys = new(1024);

        // Layer 2 — GVD Raw
        private readonly Dictionary<int, SpriteRenderer> _gvdRawRenderers     = new(512);
        private readonly Stack<SpriteRenderer>           _gvdRawPool           = new(512);
        private readonly List<int>                       _gvdRawLastActiveKeys = new(512);

        // Layer 3 — GVD Nodes/Edges post-pruning
        private readonly List<SpriteRenderer>      _gvdNodePool = new();
        private readonly List<LandmarkOverlayNode> _gvdNodes    = new();
        private readonly List<LineRenderer>        _gvdEdgePool = new();
        private readonly List<LandmarkOverlayEdge> _gvdEdges    = new();

        private readonly GvdDinOverlaySnapshot _gvdDinSnapshot = new GvdDinOverlaySnapshot();

        // Flag layer GVD-DIN attivi (settati da SetGvdDinLayerFlags)
        private bool _showDtHeatmap;
        private bool _showGvdRaw;
        private bool _showGvdNodes;

        // ============================================================
        // COLORI
        // ============================================================

        private static readonly Color WorldColor      = new Color(1f,    1f,    1f,    0.85f);
        private static readonly Color KnownColor      = new Color(0.20f, 1.00f, 0.55f, 1f);
        private static readonly Color RouteColor      = new Color(1.00f, 0.65f, 0.15f, 1f);
        private static readonly Color LmPathColor     = new Color(1.00f, 0.65f, 0.15f, 1f);
        private static readonly Color DirectPathColor = new Color(0.20f, 0.75f, 1.00f, 1f);
        private static readonly Color JumpPathColor   = new Color(1.00f, 0.20f, 0.75f, 1f);
        private static readonly Color GvdNodeColor      = new Color(0.70f, 0.10f, 1.00f, 1f);
        private static readonly Color GvdRawColor       = new Color(0.00f, 1.00f, 1.00f, 0.60f);
        // v0.03.04.c-ComplexEdge_Creation: giallo — distinto da verde (known) e arancione (route)
        private static readonly Color ComplexEdgeColor  = new Color(1.00f, 1.00f, 0.00f, 1f);

        // ============================================================
        // ACCESSO READ-ONLY NODI (consumati da MapGridLandmarkLabelOverlay)
        // ============================================================

        public IReadOnlyList<LandmarkOverlayNode> WorldNodes => _worldNodes;
        public IReadOnlyList<LandmarkOverlayNode> KnownNodes => _knownNodes;
        public IReadOnlyList<LandmarkOverlayNode> RouteNodes => _routeNodes;
        public IReadOnlyList<LandmarkOverlayNode> GvdNodes   => _gvdNodes;

        // ============================================================
        // INIT
        // ============================================================

        public void Init(Transform parent, float tileSizeWorld, string nodeSpriteResourcesPath, int sortingOrder)
        {
            _tileSizeWorld = tileSizeWorld;
            _sortingOrder  = sortingOrder;

            var go = new GameObject("LandmarkOverlay");
            _root = go.transform;
            _root.SetParent(parent, false);

            _nodeSprite = Resources.Load<Sprite>(nodeSpriteResourcesPath);
            if (_nodeSprite == null)
                Debug.LogWarning($"[MapGrid] Landmark overlay sprite not found at Resources/{nodeSpriteResourcesPath}.png");

            var shader = Shader.Find("Sprites/Default");
            if (shader != null)
                _lineMaterial = new Material(shader);

            SetEnabled(false);
        }

        public void SetEnabled(bool enabled)
        {
            _enabled = enabled;
            if (_root != null)
                _root.gameObject.SetActive(enabled);
            if (!enabled)
                Clear();
        }

        public bool IsEnabled => _enabled;

        public void SetGvdDinLayerFlags(bool showDtHeatmap, bool showGvdRaw, bool showGvdNodes)
        {
            _showDtHeatmap = showDtHeatmap;
            _showGvdRaw    = showGvdRaw;
            _showGvdNodes  = showGvdNodes;
        }

        // ============================================================
        // CLEAR
        // ============================================================

        public void Clear()
        {
            DisableAll(_worldNodePool); DisableAll(_worldEdgePool);
            DisableAll(_knownNodePool); DisableAll(_knownEdgePool);
            DisableAll(_routeNodePool); DisableAll(_routeEdgePool);
            DisableAll(_lmPathEdgePool);
            DisableAll(_directPathEdgePool);
            DisableAll(_jumpPathEdgePool);
            DisableAll(_complexEdgePool);
            ClearDtHeatmap();
            ClearGvdRaw();
            DisableAll(_gvdNodePool);
            DisableAll(_gvdEdgePool);
        }

        // ============================================================
        // RENDER
        // ============================================================

        public void Render(World world, int npcId)
        {
            if (!_enabled || world == null || _root == null) return;

            world.GetNpcLandmarkOverlayData(
                npcId,
                _worldNodes, _worldEdges,
                _knownNodes, _knownEdges,
                _routeNodes, _routeEdges,
                _lmPathEdges, _directPathEdges, _jumpPathEdges,
                _complexEdges,
                out var _routeReport);

            RenderNodes(_worldNodes, _worldNodePool, WorldColor, 0.35f);
            RenderEdges(_worldEdges, _worldEdgePool, WorldColor, 0.03f);

            RenderNodes(_knownNodes, _knownNodePool, KnownColor, 0.48f);
            // v0.03.04.c: edge semplici (verdi) nascosti — il layer significativo è il giallo ComplexEdge.
            DisableAll(_knownEdgePool);

            RenderNodes(_routeNodes, _routeNodePool, RouteColor, 0.62f);
            RenderEdges(_routeEdges, _routeEdgePool, RouteColor, 0.09f);

            RenderEdges(_lmPathEdges,     _lmPathEdgePool,     LmPathColor,     0.11f);
            RenderEdges(_directPathEdges, _directPathEdgePool, DirectPathColor, 0.11f);
            RenderEdges(_jumpPathEdges,   _jumpPathEdgePool,   JumpPathColor,   0.11f);

            // v0.03.04.c-ComplexEdge_Creation: edge fisicamente percorsi, giallo.
            // Usa RenderComplexEdgesChained: segmenti dello stesso percorso vengono
            // fusi in un unico LineRenderer multi-punto per mostrare la forma a scalino.
            RenderComplexEdgesChained(_complexEdges, _complexEdgePool, ComplexEdgeColor, 0.10f);

            RenderGvdDin(world);
        }

        // ============================================================
        // GVD-DIN
        // ============================================================

        private void RenderGvdDin(World world)
        {
            world.GetGvdDinOverlayData(_gvdDinSnapshot);

            if (!_gvdDinSnapshot.IsValid)
            {
                ClearDtHeatmap(); ClearGvdRaw();
                DisableAll(_gvdNodePool); DisableAll(_gvdEdgePool);
                _gvdNodes.Clear(); _gvdEdges.Clear();
                return;
            }

            if (_showDtHeatmap) RenderDtHeatmap(_gvdDinSnapshot.DtCells);
            else                ClearDtHeatmap();

            if (_showGvdRaw)    RenderGvdRaw(_gvdDinSnapshot.GvdRawCells);
            else                ClearGvdRaw();

            if (_showGvdNodes)
            {
                _gvdNodes.Clear(); _gvdNodes.AddRange(_gvdDinSnapshot.GvdNodes);
                _gvdEdges.Clear(); _gvdEdges.AddRange(_gvdDinSnapshot.GvdEdges);
                RenderNodes(_gvdNodes, _gvdNodePool, GvdNodeColor, 0.50f);
                RenderEdges(_gvdEdges, _gvdEdgePool, GvdNodeColor, 0.07f);
            }
            else
            {
                DisableAll(_gvdNodePool); DisableAll(_gvdEdgePool);
                _gvdNodes.Clear(); _gvdEdges.Clear();
            }
        }

        private void RenderDtHeatmap(List<GvdDinOverlayCellDt> cells)
        {
            foreach (int k in _dtLastActiveKeys)
                if (_dtCellRenderers.TryGetValue(k, out var sr) && sr != null)
                    sr.gameObject.SetActive(false);
            _dtLastActiveKeys.Clear();

            foreach (var cell in cells)
            {
                int key = cell.CellY * 10000 + cell.CellX;
                if (!_dtCellRenderers.TryGetValue(key, out var sr) || sr == null)
                {
                    sr = _dtCellPool.Count > 0 ? _dtCellPool.Pop() : CreateCellRenderer("DT");
                    _dtCellRenderers[key] = sr;
                }
                sr.gameObject.SetActive(true);
                sr.transform.localPosition = new Vector3((cell.CellX + 0.5f) * _tileSizeWorld, (cell.CellY + 0.5f) * _tileSizeWorld, 0f);
                var c = Color.Lerp(Color.blue, Color.red, cell.DtNormalized01);
                sr.color = new Color(c.r, c.g, c.b, 0.45f);
                _dtLastActiveKeys.Add(key);
            }
        }

        private void RenderGvdRaw(List<GvdDinOverlayCellGvd> cells)
        {
            foreach (int k in _gvdRawLastActiveKeys)
                if (_gvdRawRenderers.TryGetValue(k, out var sr) && sr != null)
                    sr.gameObject.SetActive(false);
            _gvdRawLastActiveKeys.Clear();

            foreach (var cell in cells)
            {
                int key = cell.CellY * 10000 + cell.CellX;
                if (!_gvdRawRenderers.TryGetValue(key, out var sr) || sr == null)
                {
                    sr = _gvdRawPool.Count > 0 ? _gvdRawPool.Pop() : CreateCellRenderer("GVD");
                    _gvdRawRenderers[key] = sr;
                }
                sr.gameObject.SetActive(true);
                sr.transform.localPosition = new Vector3((cell.CellX + 0.5f) * _tileSizeWorld, (cell.CellY + 0.5f) * _tileSizeWorld, 0f);
                sr.color = GvdRawColor;
                _gvdRawLastActiveKeys.Add(key);
            }
        }

        private void ClearDtHeatmap()
        {
            foreach (int k in _dtLastActiveKeys)
                if (_dtCellRenderers.TryGetValue(k, out var sr) && sr != null)
                    sr.gameObject.SetActive(false);
            _dtLastActiveKeys.Clear();
        }

        private void ClearGvdRaw()
        {
            foreach (int k in _gvdRawLastActiveKeys)
                if (_gvdRawRenderers.TryGetValue(k, out var sr) && sr != null)
                    sr.gameObject.SetActive(false);
            _gvdRawLastActiveKeys.Clear();
        }

        private SpriteRenderer CreateCellRenderer(string tag)
        {
            var go = new GameObject($"LM_Cell_{tag}");
            go.transform.SetParent(_root, false);
            go.transform.localScale = Vector3.one * _tileSizeWorld;
            var sr = go.AddComponent<SpriteRenderer>();
            sr.sortingOrder = _sortingOrder - 1;
            sr.sprite = _nodeSprite;
            return sr;
        }

        // ============================================================
        // POOL HELPERS
        // ============================================================

        private void RenderNodes(List<LandmarkOverlayNode> nodes, List<SpriteRenderer> pool, Color color, float nodeScale)
        {
            EnsureNodePool(pool, nodes.Count, color, nodeScale);
            for (int i = 0; i < nodes.Count; i++)
            {
                var n = nodes[i]; var sr = pool[i];
                sr.gameObject.SetActive(true);
                sr.transform.localPosition = new Vector3((n.CellX + 0.5f) * _tileSizeWorld, (n.CellY + 0.5f) * _tileSizeWorld, 0f);
                sr.sprite = _nodeSprite;
            }
            for (int i = nodes.Count; i < pool.Count; i++)
                pool[i].gameObject.SetActive(false);
        }

        private void RenderEdges(List<LandmarkOverlayEdge> edges, List<LineRenderer> pool, Color color, float width)
        {
            EnsureEdgePool(pool, edges.Count, color, width);
            for (int i = 0; i < edges.Count; i++)
            {
                var e = edges[i]; var lr = pool[i];
                lr.gameObject.SetActive(true);
                lr.positionCount = 2;
                lr.startWidth = width; lr.endWidth = width;
                lr.startColor = color; lr.endColor = color;
                lr.SetPosition(0, new Vector3((e.Ax + 0.5f) * _tileSizeWorld, (e.Ay + 0.5f) * _tileSizeWorld, 0f));
                lr.SetPosition(1, new Vector3((e.Bx + 0.5f) * _tileSizeWorld, (e.By + 0.5f) * _tileSizeWorld, 0f));
            }
            for (int i = edges.Count; i < pool.Count; i++)
                pool[i].gameObject.SetActive(false);
        }

        /// <summary>
        /// Renderizza i ComplexEdge come percorsi a scalino.
        ///
        /// <para>
        /// Ogni ComplexEdge è una sequenza di <see cref="LandmarkOverlayEdge"/> concatenati
        /// (il punto B del segmento i coincide con il punto A del segmento i+1).
        /// Invece di creare un <c>LineRenderer</c> per ogni segmento (che produce linee
        /// brevi difficili da distinguere), raggruppa i segmenti connessi in un unico
        /// <c>LineRenderer</c> con N+1 waypoint, mostrando la forma a scalino in modo
        /// inequivocabile.
        /// </para>
        /// </summary>
        private void RenderComplexEdgesChained(
            List<LandmarkOverlayEdge> edges,
            List<LineRenderer> pool,
            Color color,
            float width)
        {
            if (edges.Count == 0)
            {
                for (int i = 0; i < pool.Count; i++)
                    if (pool[i] != null) pool[i].gameObject.SetActive(false);
                return;
            }

            // Passo 1: individua i limiti di ogni catena (= un ComplexEdge).
            // Una nuova catena inizia quando il punto B del segmento precedente
            // NON coincide con il punto A del segmento corrente.
            var chainStarts = new System.Collections.Generic.List<int>(8) { 0 };
            for (int i = 1; i < edges.Count; i++)
            {
                var prev = edges[i - 1];
                var curr = edges[i];
                if (prev.Bx != curr.Ax || prev.By != curr.Ay)
                    chainStarts.Add(i);
            }

            // Passo 2: assicura che ci siano abbastanza LineRenderer nel pool.
            EnsureEdgePool(pool, chainStarts.Count, color, width);

            // Passo 3: renderizza ogni catena su un unico LineRenderer.
            for (int c = 0; c < chainStarts.Count; c++)
            {
                int segStart = chainStarts[c];
                int segEnd   = (c + 1 < chainStarts.Count) ? chainStarts[c + 1] : edges.Count;
                int segCount = segEnd - segStart;

                var lr = pool[c];
                lr.gameObject.SetActive(true);
                lr.positionCount = segCount + 1; // N segmenti = N+1 waypoint
                lr.startWidth = width; lr.endWidth = width;
                lr.startColor = color; lr.endColor = color;

                // Primo waypoint: inizio del primo segmento della catena.
                var first = edges[segStart];
                lr.SetPosition(0, new Vector3(
                    (first.Ax + 0.5f) * _tileSizeWorld,
                    (first.Ay + 0.5f) * _tileSizeWorld, 0f));

                // Waypoint successivi: fine di ogni segmento.
                for (int j = 0; j < segCount; j++)
                {
                    var e = edges[segStart + j];
                    lr.SetPosition(j + 1, new Vector3(
                        (e.Bx + 0.5f) * _tileSizeWorld,
                        (e.By + 0.5f) * _tileSizeWorld, 0f));
                }
            }

            // Passo 4: disabilita i LineRenderer in eccesso.
            for (int i = chainStarts.Count; i < pool.Count; i++)
                if (pool[i] != null) pool[i].gameObject.SetActive(false);
        }

        private void EnsureNodePool(List<SpriteRenderer> pool, int needed, Color color, float nodeScale)
        {
            while (pool.Count < needed)
            {
                var go = new GameObject($"LM_Node_{pool.Count}");
                go.transform.SetParent(_root, false);
                var sr = go.AddComponent<SpriteRenderer>();
                sr.sortingOrder = _sortingOrder; sr.sprite = _nodeSprite; sr.color = color;
                go.transform.localScale = Vector3.one * nodeScale;
                pool.Add(sr);
            }
            for (int i = 0; i < pool.Count; i++)
            {
                var sr = pool[i]; if (sr == null) continue;
                sr.color = color;
                sr.transform.localScale = Vector3.one * nodeScale;
                sr.sortingOrder = _sortingOrder;
            }
        }

        private void EnsureEdgePool(List<LineRenderer> pool, int needed, Color color, float width)
        {
            while (pool.Count < needed)
            {
                var go = new GameObject($"LM_Edge_{pool.Count}");
                go.transform.SetParent(_root, false);
                var lr = go.AddComponent<LineRenderer>();
                lr.useWorldSpace = false; lr.loop = false; lr.widthMultiplier = 1f;
                lr.sortingOrder = _sortingOrder;
                if (_lineMaterial != null) lr.material = _lineMaterial;
                pool.Add(lr);
            }
            for (int i = 0; i < pool.Count; i++)
            {
                var lr = pool[i]; if (lr == null) continue;
                lr.sortingOrder = _sortingOrder;
                if (_lineMaterial != null) lr.material = _lineMaterial;
                lr.startColor = color; lr.endColor = color;
                lr.startWidth = width; lr.endWidth = width;
            }
        }

        private static void DisableAll(List<SpriteRenderer> pool)
        { for (int i = 0; i < pool.Count; i++) if (pool[i] != null) pool[i].gameObject.SetActive(false); }

        private static void DisableAll(List<LineRenderer> pool)
        { for (int i = 0; i < pool.Count; i++) if (pool[i] != null) pool[i].gameObject.SetActive(false); }
    }
}
