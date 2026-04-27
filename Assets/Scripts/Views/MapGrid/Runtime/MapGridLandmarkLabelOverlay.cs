using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using Arcontio.Core;

namespace Arcontio.View.MapGrid
{
    /// <summary>
    /// MapGridLandmarkLabelOverlay (v0.03 - Patch 0.03.01.h):
    /// Overlay Canvas ScreenSpaceOverlay per le etichette dei landmark.
    ///
    /// Sostituisce il sistema TextMesh world-space che produceva testo
    /// illeggibile indipendentemente dai parametri fontSize/characterSize.
    ///
    /// Architettura:
    /// - Canvas ScreenSpaceOverlay separato (come MapGridEntitySummaryOverlay).
    /// - Una mini-card UI per ogni landmark attivo visibile.
    /// - Nessun drag, nessuna linea di collegamento — solo etichetta.
    /// - Posizionamento via WorldToScreenPoint → ScreenPointToLocalPointInRectangle.
    /// - Pool di card riutilizzate tra frame per evitare GC.
    ///
    /// Nota: questo overlay è completamente separato da MapGridLandmarkOverlay
    /// (che gestisce nodi e edge come SpriteRenderer/LineRenderer).
    /// Le due classi collaborano: LandmarkOverlay disegna i marker,
    /// LandmarkLabelOverlay disegna le etichette leggibili.
    /// </summary>
    public sealed class MapGridLandmarkLabelOverlay
    {
        // ============================================================
        // COSTANTI VISUAL
        // ============================================================

        // Dimensioni card etichetta (piccola — solo testo ID)
        private const int CardWidth    = 72;
        private const int CardHeight   = 20;
        private const int LabelFont    = 11;

        // Colore sfondo card per tipo landmark
        // Allineati ai colori dell'overlay nodi in MapGridLandmarkOverlay:
        //   World (registro globale):  bianco semitrasparente
        //   Known (memoria NPC):       verde
        //   Route (percorso attivo):   arancione
        private static readonly Color BgWorld   = new Color(0.10f, 0.10f, 0.10f, 0.75f);
        private static readonly Color BgKnown   = new Color(0.00f, 0.25f, 0.10f, 0.80f);
        private static readonly Color BgRoute   = new Color(0.30f, 0.18f, 0.00f, 0.80f);
        private static readonly Color BgGvd     = new Color(0.20f, 0.00f, 0.28f, 0.80f);

        private static readonly Color TextColor = new Color(1f, 1f, 1f, 1f);

        // ============================================================
        // STATO
        // ============================================================

        private Canvas         _canvas;
        private RectTransform  _rootRt;
        private bool           _enabled;

        // Pool di card per layer: world, known, route, gvd
        private readonly List<LabelCard> _worldCards  = new List<LabelCard>(64);
        private readonly List<LabelCard> _knownCards  = new List<LabelCard>(64);
        private readonly List<LabelCard> _routeCards  = new List<LabelCard>(32);
        private readonly List<LabelCard> _gvdCards    = new List<LabelCard>(32);

        // ============================================================
        // INIT
        // ============================================================

        /// <summary>
        /// Inizializza il Canvas overlay. Chiamato da MapGridWorldView.Start().
        /// parent: il transform del MapGridWorldView (come per EntitySummaryOverlay).
        /// </summary>
        public void Init(Transform parent)
        {
            var go = new GameObject("LandmarkLabelOverlay");
            go.transform.SetParent(parent, false);

            // Canvas ScreenSpaceOverlay — indipendente dallo zoom camera.
            _canvas = go.AddComponent<Canvas>();
            _canvas.renderMode  = RenderMode.ScreenSpaceOverlay;
            _canvas.sortingOrder = 998; // sotto EntitySummaryOverlay (999) ma sopra tutto il resto

            go.AddComponent<CanvasScaler>();
            go.AddComponent<GraphicRaycaster>().enabled = false; // solo display, no input

            _rootRt = go.GetComponent<RectTransform>();
            if (_rootRt == null) _rootRt = go.AddComponent<RectTransform>();
            _rootRt.anchorMin  = Vector2.zero;
            _rootRt.anchorMax  = Vector2.one;
            _rootRt.pivot      = new Vector2(0.5f, 0.5f);
            _rootRt.sizeDelta  = Vector2.zero;

            SetEnabled(false);
        }

        public void SetEnabled(bool enabled)
        {
            _enabled = enabled;
            if (_canvas != null)
                _canvas.gameObject.SetActive(enabled);
            if (!enabled)
                HideAll();
        }

        public bool IsEnabled => _enabled;

        // ============================================================
        // RENDER
        // ============================================================

        /// <summary>
        /// Aggiorna le etichette per il frame corrente.
        /// Chiamato da MapGridLandmarkOverlay.Render() oppure direttamente
        /// da MapGridWorldView.Update() dopo il render dell'overlay nodi.
        /// </summary>
        public void Render(
            World  world,
            Camera cam,
            float  tileSizeWorld,
            List<LandmarkOverlayNode> worldNodes,
            List<LandmarkOverlayNode> knownNodes,
            List<LandmarkOverlayNode> routeNodes,
            List<LandmarkOverlayNode> gvdNodes)
        {
            if (!_enabled || cam == null || _rootRt == null) return;

            RenderLayer(worldNodes, _worldCards, BgWorld,  cam, tileSizeWorld);
            RenderLayer(knownNodes, _knownCards, BgKnown,  cam, tileSizeWorld);
            RenderLayer(routeNodes, _routeCards, BgRoute,  cam, tileSizeWorld);
            RenderLayer(gvdNodes,   _gvdCards,   BgGvd,    cam, tileSizeWorld);
        }

        public void Clear()
        {
            HideAll();
        }

        // ============================================================
        // INTERNALS
        // ============================================================

        private void RenderLayer(
            List<LandmarkOverlayNode> nodes,
            List<LabelCard>           pool,
            Color                     bgColor,
            Camera                    cam,
            float                     tileSizeWorld)
        {
            // Assicura pool sufficiente
            while (pool.Count < nodes.Count)
                pool.Add(CreateCard(bgColor));

            for (int i = 0; i < nodes.Count; i++)
            {
                var n    = nodes[i];
                var card = pool[i];

                // Converti posizione cella → screen → canvas local
                var wp = new Vector3(
                    (n.CellX + 0.5f) * tileSizeWorld,
                    (n.CellY + 0.5f) * tileSizeWorld,
                    0f);
                var sp = cam.WorldToScreenPoint(wp);

                if (sp.z < 0f)
                {
                    card.SetVisible(false);
                    continue;
                }

                // Offset verticale: card leggermente sopra il marker
                var screen = new Vector2(sp.x, sp.y + 14f);
                if (RectTransformUtility.ScreenPointToLocalPointInRectangle(
                        _rootRt, screen, null, out var local))
                {
                    card.Rt.anchoredPosition = local;
                }

                card.Label.text  = string.IsNullOrEmpty(n.Label)
                    ? n.NodeId.ToString()
                    : n.Label;
                card.Bg.color    = bgColor;
                card.SetVisible(true);
            }

            // Nascondi card in eccesso
            for (int i = nodes.Count; i < pool.Count; i++)
                pool[i].SetVisible(false);
        }

        private void HideAll()
        {
            foreach (var c in _worldCards) c.SetVisible(false);
            foreach (var c in _knownCards) c.SetVisible(false);
            foreach (var c in _routeCards) c.SetVisible(false);
            foreach (var c in _gvdCards)   c.SetVisible(false);
        }

        private LabelCard CreateCard(Color bgColor)
        {
            var go = new GameObject("LM_Label");
            go.transform.SetParent(_rootRt, false);

            var rt = go.AddComponent<RectTransform>();
            rt.sizeDelta = new Vector2(CardWidth, CardHeight);
            rt.anchorMin = new Vector2(0.5f, 0.5f);
            rt.anchorMax = new Vector2(0.5f, 0.5f);
            rt.pivot     = new Vector2(0.5f, 0f); // ancora al bordo inferiore → sale sopra il marker

            var bg = go.AddComponent<Image>();
            bg.color          = bgColor;
            bg.raycastTarget  = false;

            // Testo centrato nella card
            var textGo = new GameObject("Text");
            textGo.transform.SetParent(go.transform, false);

            var textRt = textGo.AddComponent<RectTransform>();
            textRt.anchorMin  = Vector2.zero;
            textRt.anchorMax  = Vector2.one;
            textRt.sizeDelta  = Vector2.zero;
            textRt.offsetMin  = new Vector2(3f, 2f);
            textRt.offsetMax  = new Vector2(-3f, -2f);

            var label = textGo.AddComponent<Text>();
            label.raycastTarget      = false;
            label.font               = GetUiFont();
            label.fontSize           = LabelFont;
            label.fontStyle          = FontStyle.Bold;
            label.alignment          = TextAnchor.MiddleCenter;
            label.color              = TextColor;
            label.horizontalOverflow = HorizontalWrapMode.Overflow;
            label.verticalOverflow   = VerticalWrapMode.Overflow;

            go.SetActive(false);
            return new LabelCard(rt, bg, label, go);
        }

        private static Font GetUiFont()
        {
            var f = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            if (f != null) return f;
            return Resources.GetBuiltinResource<Font>("Arial.ttf");
        }

        // ============================================================
        // CARD STRUCT
        // ============================================================

        private sealed class LabelCard
        {
            public readonly RectTransform Rt;
            public readonly Image         Bg;
            public readonly Text          Label;
            private readonly GameObject   _go;

            public LabelCard(RectTransform rt, Image bg, Text label, GameObject go)
            {
                Rt = rt; Bg = bg; Label = label; _go = go;
            }

            public void SetVisible(bool v) { if (_go != null) _go.SetActive(v); }
        }
    }
}
