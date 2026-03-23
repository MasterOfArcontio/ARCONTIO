using UnityEngine;
using UnityEngine.UI;

namespace Arcontio.View.MapGrid
{
    /// <summary>
    /// MapGridPointerCoordsOverlay (Patch 0.01P2):
    /// Indicatore costante in alto a sinistra che mostra le coordinate griglia
    /// sotto il puntatore del mouse.
    ///
    /// Perché non riusiamo il tooltip hover:
    /// - Il tooltip è contestuale e può essere disabilitato (es. SummaryOverlay ON).
    /// - L'utente vuole un indicatore *sempre presente*.
    ///
    /// Policy:
    /// - View-only, prefabless (come gli altri overlay).
    /// - Aggiornato dal MapGridWorldView.
    /// - Se mancano camera/input, mostra "Cell: -,-".
    /// </summary>
    public sealed class MapGridPointerCoordsOverlay
    {
        private readonly GameObject _root;
        private readonly Canvas _canvas;
        private readonly RectTransform _panelRt;
        private readonly Text _text;

        private bool _visible;

        public MapGridPointerCoordsOverlay()
        {
            _root = new GameObject("MapGridPointerCoordsOverlay");

            _canvas = _root.AddComponent<Canvas>();
            _canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            _canvas.sortingOrder = 10001; // sopra al tooltip overlay (10000)

            var scaler = _root.AddComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1920, 1080);

            // Panel (piccolo)
            var panelGo = new GameObject("Panel");
            panelGo.transform.SetParent(_root.transform, false);

            var img = panelGo.AddComponent<Image>();
            img.color = new Color(0f, 0f, 0f, 0.55f);

            _panelRt = panelGo.GetComponent<RectTransform>();

            // Top-left anchoring
            _panelRt.anchorMin = new Vector2(0f, 1f);
            _panelRt.anchorMax = new Vector2(0f, 1f);
            _panelRt.pivot = new Vector2(0f, 1f);

            // Offset dal bordo schermo
            _panelRt.anchoredPosition = new Vector2(10f, -10f);
            _panelRt.sizeDelta = new Vector2(220f, 34f);

            // Text
            var textGo = new GameObject("Text");
            textGo.transform.SetParent(panelGo.transform, false);

            _text = textGo.AddComponent<Text>();
            _text.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            _text.fontSize = 14;
            _text.alignment = TextAnchor.MiddleLeft;
            _text.horizontalOverflow = HorizontalWrapMode.Overflow;
            _text.verticalOverflow = VerticalWrapMode.Overflow;
            _text.supportRichText = true;
            _text.color = Color.white;
            _text.text = "Cell: -,-";

            var trt = textGo.GetComponent<RectTransform>();
            trt.anchorMin = new Vector2(0f, 0f);
            trt.anchorMax = new Vector2(1f, 1f);
            trt.offsetMin = new Vector2(10f, 4f);
            trt.offsetMax = new Vector2(-10f, -4f);

            SetVisible(true);
        }

        public void Destroy()
        {
            if (_root != null) Object.Destroy(_root);
        }

        public void SetVisible(bool v)
        {
            if (_visible == v) return;
            _visible = v;
            if (_root != null) _root.SetActive(v);
        }

        public void SetCell(int cellX, int cellY, bool inBounds)
        {
            // UX: se fuori bounds (o tileSize/camera invalidi) lo segnaliamo.
            if (!inBounds)
            {
                _text.text = $"Cell: <color=#FF6666>{cellX},{cellY}</color>";
                return;
            }

            _text.text = $"Cell: <b>{cellX},{cellY}</b>";
        }

        public void SetUnknown()
        {
            _text.text = "Cell: -,-";
        }
    }
}
