using System.Collections.Generic;
using UnityEngine;
using Arcontio.Core;

namespace Arcontio.View.MapGrid
{
    /// <summary>
    /// Debug overlay per il sistema Landmark.
    ///
    /// Day5 patch:
    /// - mantiene i tre layer gia' esistenti (WORLD / KNOWN / ROUTE)
    /// - aggiunge per ogni nodo una piccola label world-space con ID/tipo
    /// - non introduce nessun parametro JSON nuovo: e' una feature puramente debug
    ///
    /// Nota architetturale:
    /// - le label sono TextMesh pooled, quindi non richiedono Canvas/TMP
    /// - il contenuto della label arriva dal Core tramite LandmarkOverlayNode.Label
    /// </summary>
    public sealed class MapGridLandmarkOverlay
    {
        private Transform _root;
        private float _tileSizeWorld;
        private int _sortingOrder;

        private Sprite _nodeSprite;
        private Material _lineMaterial;

        private readonly List<SpriteRenderer> _worldNodePool = new();
        private readonly List<LineRenderer> _worldEdgePool = new();
        private readonly List<TextMesh> _worldLabelPool = new();
        private readonly List<LandmarkOverlayNode> _worldNodes = new();
        private readonly List<LandmarkOverlayEdge> _worldEdges = new();

        private readonly List<SpriteRenderer> _knownNodePool = new();
        private readonly List<LineRenderer> _knownEdgePool = new();
        private readonly List<TextMesh> _knownLabelPool = new();
        private readonly List<LandmarkOverlayNode> _knownNodes = new();
        private readonly List<LandmarkOverlayEdge> _knownEdges = new();

        private readonly List<SpriteRenderer> _routeNodePool = new();
        private readonly List<LineRenderer> _routeEdgePool = new();
        private readonly List<TextMesh> _routeLabelPool = new();
        private readonly List<LandmarkOverlayNode> _routeNodes = new();
        private readonly List<LandmarkOverlayEdge> _routeEdges = new();

        private readonly List<LineRenderer> _lmPathEdgePool = new();
        private readonly List<LandmarkOverlayEdge> _lmPathEdges = new();

        private readonly List<LineRenderer> _directPathEdgePool = new();
        private readonly List<LandmarkOverlayEdge> _directPathEdges = new();

        private readonly List<LineRenderer> _jumpPathEdgePool = new();
        private readonly List<LandmarkOverlayEdge> _jumpPathEdges = new();

        private bool _enabled;

        private static readonly Color WorldColor = new Color(1f, 1f, 1f, 0.85f);
        private static readonly Color KnownColor = new Color(0.20f, 1.00f, 0.55f, 1f);
        private static readonly Color RouteColor = new Color(1.00f, 0.65f, 0.15f, 1f);
        private static readonly Color LmPathColor = new Color(1.00f, 0.65f, 0.15f, 1f);
        private static readonly Color DirectPathColor = new Color(0.20f, 0.75f, 1.00f, 1f);
        private static readonly Color JumpPathColor = new Color(1.00f, 0.20f, 0.75f, 1f);

        public void Init(Transform parent, float tileSizeWorld, string nodeSpriteResourcesPath, int sortingOrder)
        {
            _tileSizeWorld = tileSizeWorld;
            _sortingOrder = sortingOrder;

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

        public void Clear()
        {
            DisableAll(_worldNodePool); DisableAll(_worldEdgePool); DisableAll(_worldLabelPool);
            DisableAll(_knownNodePool); DisableAll(_knownEdgePool); DisableAll(_knownLabelPool);
            DisableAll(_routeNodePool); DisableAll(_routeEdgePool); DisableAll(_routeLabelPool);
            DisableAll(_lmPathEdgePool);
            DisableAll(_directPathEdgePool);
            DisableAll(_jumpPathEdgePool);
        }

        public void Render(World world, int npcId)
        {
            if (!_enabled || world == null || _root == null)
                return;

            world.GetNpcLandmarkOverlayData(npcId, _worldNodes, _worldEdges, _knownNodes, _knownEdges, _routeNodes, _routeEdges, _lmPathEdges, _directPathEdges, _jumpPathEdges, out var _routeReport);

            RenderNodes(_worldNodes, _worldNodePool, WorldColor, 0.35f);
            RenderEdges(_worldEdges, _worldEdgePool, WorldColor, 0.03f);
            // Label world (bianca): più piccola di meno della metà rispetto a prima,
            // e leggermente spostata a sinistra per non collidere con le altre due.
            RenderLabels(_worldNodes, _worldLabelPool, WorldColor, 0.09f, xOffsetTiles: -0.22f, yOffsetTiles: 0.24f);

            RenderNodes(_knownNodes, _knownNodePool, KnownColor, 0.48f);
            RenderEdges(_knownEdges, _knownEdgePool, KnownColor, 0.055f);
            // Label known (verde): tenuta circa sopra il centro del nodo.
            RenderLabels(_knownNodes, _knownLabelPool, KnownColor, 0.10f, xOffsetTiles: 0.00f, yOffsetTiles: 0.38f);

            RenderNodes(_routeNodes, _routeNodePool, RouteColor, 0.62f);
            // Il vecchio layer route-edge astratto resta volutamente vuoto: il percorso vero
            // viene ora disegnato con tre layer separati cella-per-cella.
            RenderEdges(_routeEdges, _routeEdgePool, RouteColor, 0.09f);
            // Label route (gialla): spostata a destra e un po' più in alto,
            // così lo stesso nodo può mostrare fino a tre etichette leggibili senza sovrapporsi.
            RenderLabels(_routeNodes, _routeLabelPool, RouteColor, 0.11f, xOffsetTiles: 0.22f, yOffsetTiles: 0.52f);

            // PATCH 0.02.05.2e:
            // Nuovo overlay del path reale/inteso, sempre cella-per-cella e mai come segmento astratto.
            RenderEdges(_lmPathEdges, _lmPathEdgePool, LmPathColor, 0.11f);
            RenderEdges(_directPathEdges, _directPathEdgePool, DirectPathColor, 0.11f);
            RenderEdges(_jumpPathEdges, _jumpPathEdgePool, JumpPathColor, 0.11f);
        }

        private void RenderNodes(List<LandmarkOverlayNode> nodes, List<SpriteRenderer> pool, Color color, float nodeScale)
        {
            EnsureNodePool(pool, nodes.Count, color, nodeScale);
            for (int i = 0; i < nodes.Count; i++)
            {
                var n = nodes[i];
                var sr = pool[i];
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
                var e = edges[i];
                var lr = pool[i];
                lr.gameObject.SetActive(true);
                Vector3 a = new Vector3((e.Ax + 0.5f) * _tileSizeWorld, (e.Ay + 0.5f) * _tileSizeWorld, 0f);
                Vector3 b = new Vector3((e.Bx + 0.5f) * _tileSizeWorld, (e.By + 0.5f) * _tileSizeWorld, 0f);
                lr.positionCount = 2;
                lr.startWidth = width;
                lr.endWidth = width;
                lr.startColor = color;
                lr.endColor = color;
                lr.SetPosition(0, a);
                lr.SetPosition(1, b);
            }
            for (int i = edges.Count; i < pool.Count; i++)
                pool[i].gameObject.SetActive(false);
        }

        private void RenderLabels(List<LandmarkOverlayNode> nodes, List<TextMesh> pool, Color color, float charScale, float xOffsetTiles, float yOffsetTiles)
        {
            EnsureLabelPool(pool, nodes.Count, color, charScale);
            for (int i = 0; i < nodes.Count; i++)
            {
                var n = nodes[i];
                var tm = pool[i];
                tm.gameObject.SetActive(true);
                tm.text = string.IsNullOrEmpty(n.Label) ? n.NodeId.ToString() : n.Label;
                tm.color = color;
                tm.transform.localPosition = new Vector3((n.CellX + 0.5f + xOffsetTiles) * _tileSizeWorld, (n.CellY + 0.5f + yOffsetTiles) * _tileSizeWorld, 0f);
                tm.characterSize = charScale;
            }
            for (int i = nodes.Count; i < pool.Count; i++)
                pool[i].gameObject.SetActive(false);
        }

        private void EnsureNodePool(List<SpriteRenderer> pool, int needed, Color color, float nodeScale)
        {
            while (pool.Count < needed)
            {
                var go = new GameObject($"LM_Node_{pool.Count}");
                go.transform.SetParent(_root, false);
                var sr = go.AddComponent<SpriteRenderer>();
                sr.sortingOrder = _sortingOrder;
                sr.sprite = _nodeSprite;
                sr.color = color;
                go.transform.localScale = Vector3.one * nodeScale;
                pool.Add(sr);
            }
            for (int i = 0; i < pool.Count; i++)
            {
                var sr = pool[i];
                if (sr == null) continue;
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
                lr.useWorldSpace = false;
                lr.loop = false;
                lr.widthMultiplier = 1f;
                lr.sortingOrder = _sortingOrder;
                if (_lineMaterial != null)
                    lr.material = _lineMaterial;
                pool.Add(lr);
            }
            for (int i = 0; i < pool.Count; i++)
            {
                var lr = pool[i];
                if (lr == null) continue;
                lr.sortingOrder = _sortingOrder;
                if (_lineMaterial != null)
                    lr.material = _lineMaterial;
                lr.startColor = color;
                lr.endColor = color;
                lr.startWidth = width;
                lr.endWidth = width;
            }
        }

        private void EnsureLabelPool(List<TextMesh> pool, int needed, Color color, float charScale)
        {
            while (pool.Count < needed)
            {
                var go = new GameObject($"LM_Label_{pool.Count}");
                go.transform.SetParent(_root, false);
                var tm = go.AddComponent<TextMesh>();
                tm.anchor = TextAnchor.MiddleCenter;
                tm.alignment = TextAlignment.Center;
                tm.fontSize = 32;
                tm.characterSize = charScale;
                tm.text = string.Empty;
                tm.color = color;
                var mr = go.GetComponent<MeshRenderer>();
                if (mr != null)
                    mr.sortingOrder = _sortingOrder + 1;
                pool.Add(tm);
            }
            for (int i = 0; i < pool.Count; i++)
            {
                var tm = pool[i];
                if (tm == null) continue;
                tm.color = color;
                tm.characterSize = charScale;
                var mr = tm.GetComponent<MeshRenderer>();
                if (mr != null)
                    mr.sortingOrder = _sortingOrder + 1;
            }
        }

        private static void DisableAll(List<SpriteRenderer> pool)
        {
            for (int i = 0; i < pool.Count; i++) if (pool[i] != null) pool[i].gameObject.SetActive(false);
        }

        private static void DisableAll(List<LineRenderer> pool)
        {
            for (int i = 0; i < pool.Count; i++) if (pool[i] != null) pool[i].gameObject.SetActive(false);
        }

        private static void DisableAll(List<TextMesh> pool)
        {
            for (int i = 0; i < pool.Count; i++) if (pool[i] != null) pool[i].gameObject.SetActive(false);
        }
    }
}
