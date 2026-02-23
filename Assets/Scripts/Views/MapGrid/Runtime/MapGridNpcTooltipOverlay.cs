using UnityEngine;
using UnityEngine.UI;

namespace Arcontio.View.MapGrid
{
    /// <summary>
    /// Overlay UI per tooltip:
    /// - ScreenSpaceOverlay
    /// - segue il puntatore
    /// - testo monoblocco (debug-friendly)
    /// </summary>
    public sealed class MapGridNpcTooltipOverlay
    {
        private readonly GameObject _root;
        private readonly Canvas _canvas;
        private readonly RectTransform _panelRt;
        private readonly Text _text;

        private bool _visible;

        // NEW: tooltip più vicino al puntatore
        private const float PointerOffsetPx = 1f;

        public MapGridNpcTooltipOverlay()
        {
            _root = new GameObject("MapGridNpcTooltipOverlay");

            _canvas = _root.AddComponent<Canvas>();
            _canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            _canvas.sortingOrder = 10000;

            var scaler = _root.AddComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1920, 1080);

            // Panel
            var panelGo = new GameObject("Panel");
            panelGo.transform.SetParent(_root.transform, false);

            var img = panelGo.AddComponent<Image>();
            img.color = new Color(0f, 0f, 0f, 0.78f);

            _panelRt = panelGo.GetComponent<RectTransform>();
            _panelRt.sizeDelta = new Vector2(560f, 300f);
            _panelRt.anchorMin = _panelRt.anchorMax = new Vector2(0f, 0f);
            _panelRt.pivot = new Vector2(0f, 0f);

            // Text
            var textGo = new GameObject("Text");
            textGo.transform.SetParent(panelGo.transform, false);

            _text = textGo.AddComponent<Text>();
            _text.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            _text.fontSize = 12;
            _text.alignment = TextAnchor.UpperLeft;
            _text.horizontalOverflow = HorizontalWrapMode.Wrap;
            _text.verticalOverflow = VerticalWrapMode.Overflow;
            _text.supportRichText = true;
            _text.color = Color.white;
            _text.text = "";

            var trt = textGo.GetComponent<RectTransform>();
            trt.anchorMin = new Vector2(0f, 0f);
            trt.anchorMax = new Vector2(1f, 1f);
            trt.offsetMin = new Vector2(10f, 10f);
            trt.offsetMax = new Vector2(-10f, -10f);

            SetVisible(false);
        }

        public void Destroy()
        {
            if (_root != null) Object.Destroy(_root);
        }

        public void Show(string richText, Vector2 screenPointerPos)
        {
            _text.text = richText;
            SetVisible(true);
            MoveTo(screenPointerPos);
        }

        public void Hide()
        {
            SetVisible(false);
        }

        public void MoveTo(Vector2 screenPointerPos)
        {
            float x = screenPointerPos.x + PointerOffsetPx;
            float y = screenPointerPos.y + PointerOffsetPx;

            float maxX = Mathf.Max(0f, Screen.width - _panelRt.sizeDelta.x);
            float maxY = Mathf.Max(0f, Screen.height - _panelRt.sizeDelta.y);

            x = Mathf.Clamp(x, 0f, maxX);
            y = Mathf.Clamp(y, 0f, maxY);

            _panelRt.anchoredPosition = new Vector2(x, y);
        }

        private void SetVisible(bool v)
        {
            if (_visible == v) return;
            _visible = v;
            if (_root != null) _root.SetActive(v);
        }
    }
}
