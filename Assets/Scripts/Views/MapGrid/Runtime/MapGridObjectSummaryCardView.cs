using UnityEngine;
using UnityEngine.UI;

namespace Arcontio.View.MapGrid
{
    /// <summary>
    /// UI view per la card di un oggetto interagibile in modalità SummaryOverlay.
    /// Minimalista rispetto all’NPC:
    /// - Panel root (bg scuro)
    /// - HeaderPanel (bg leggermente diverso) + Text
    ///
    /// NOTE:
    /// - Prefabless, creata via codice.
    /// - Posizionamento (anchor/offset/drag) gestito dall'overlay controller.
    /// </summary>
    public sealed class MapGridObjectSummaryCardView
    {
        private GameObject _root;
        private RectTransform _rootRt;
        private Image _rootBg;

        private Text _headerText;

        private const int DefaultWidth = 300;
        private const int DefaultHeaderFont = 12;

        public RectTransform RootRectTransform => _rootRt;

        public void AttachTo(Transform parent)
        {
            _root = new GameObject("ObjectSummaryCard");
            _root.transform.SetParent(parent, false);

            _rootRt = _root.AddComponent<RectTransform>();
            _rootRt.sizeDelta = new Vector2(DefaultWidth, 100);

            _rootRt.anchorMin = new Vector2(0.5f, 0.5f);
            _rootRt.anchorMax = new Vector2(0.5f, 0.5f);
            _rootRt.pivot = new Vector2(0.5f, 0.5f);

            _rootBg = _root.AddComponent<Image>();
            _rootBg.raycastTarget = true; // serve per drag
            _rootBg.color = new Color(0f, 0f, 0f, 0.68f);

            var layout = _root.AddComponent<VerticalLayoutGroup>();
            layout.childControlHeight = true;
            layout.childControlWidth = true;
            layout.childForceExpandHeight = false;
            layout.childForceExpandWidth = true;
            layout.padding = new RectOffset(6, 6, 6, 6);

            _root.AddComponent<ContentSizeFitter>().verticalFit = ContentSizeFitter.FitMode.PreferredSize;

            // Header
            var header = BuildHeader(_root.transform, "Header", DefaultHeaderFont, FontStyle.Normal);
            header.GetComponent<Image>().color = new Color(0.15f, 0.15f, 0.15f, 0.92f);
            _headerText = header.GetComponentInChildren<Text>();

            SetVisible(false);
        }

        /// <summary>
        /// Aggiorna contenuti.
        /// NOTA: compatibilità con versione precedente che chiamava SetTexts(...).
        /// </summary>
        public void SetText(string header)
        {
            if (_headerText != null) _headerText.text = header ?? string.Empty;
        }

        // Backward-compat: la prima patch chiamava SetTexts(...).
        public void SetTexts(string header) => SetText(header);

        public void SetVisible(bool visible)
        {
            if (_root != null)
                _root.SetActive(visible);
        }

        public void SetCanvasLocalPosition(Vector2 localPos)
        {
            if (_rootRt != null)
                _rootRt.anchoredPosition = localPos;
        }

        public Vector2 GetCanvasLocalPosition()
        {
            return _rootRt != null ? _rootRt.anchoredPosition : Vector2.zero;
        }

        // ============================================================

        private static Font GetUiFont()
        {
            var f = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            if (f != null) return f;
            return Resources.GetBuiltinResource<Font>("Arial.ttf");
        }

        private static GameObject BuildHeader(Transform parent, string name, int fontSize, FontStyle style)
        {
            var go = new GameObject(name);
            go.transform.SetParent(parent, false);

            go.AddComponent<RectTransform>();

            var img = go.AddComponent<Image>();
            img.raycastTarget = false;

            var layout = go.AddComponent<VerticalLayoutGroup>();
            layout.childControlHeight = true;
            layout.childControlWidth = true;
            layout.childForceExpandHeight = false;
            layout.childForceExpandWidth = true;
            layout.padding = new RectOffset(6, 6, 4, 4);

            var textGo = new GameObject("Text");
            textGo.transform.SetParent(go.transform, false);

            var text = textGo.AddComponent<Text>();
            text.raycastTarget = false;
            text.font = GetUiFont();
            text.fontSize = fontSize;
            text.fontStyle = style;
            text.alignment = TextAnchor.UpperLeft;
            text.horizontalOverflow = HorizontalWrapMode.Wrap;
            text.verticalOverflow = VerticalWrapMode.Overflow;

            return go;
        }
    }
}
