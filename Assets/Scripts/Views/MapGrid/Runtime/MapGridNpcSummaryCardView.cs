using UnityEngine;
using UnityEngine.UI;

namespace Arcontio.View.MapGrid
{
    /// <summary>
    /// UI view per la card di un NPC in modalità SummaryOverlay.
    ///
    /// Struttura:
    /// - Panel root (bg scuro)
    ///   - HeaderPanel (bg leggermente diverso) + Text
    ///   - MemoryPanel (bg colore A) + Text
    ///   - ObjectMemoryPanel (bg colore B) + Text
    ///
    /// NOTE IMPORTANTI:
    /// - Questa classe è volutamente "prefabless": costruisce UI da codice per ridurre dipendenze di asset.
    /// - Tutta la logica di posizionamento (anchor + offset + drag) è gestita dal controller overlay.
    ///   Qui esponiamo solo API minime (SetTexts/SetVisible/RectTransform).
    /// </summary>
    public sealed class MapGridNpcSummaryCardView
    {
        private GameObject _root;
        private RectTransform _rootRt;
        private Image _rootBg;

        private Text _headerText;
        private Text _actionText;
        private Text _inventoryText;
        private Text _memText;
        private Text _objMemText;

        private const int DefaultWidth = 340;
        private const int DefaultHeaderFont = 14;
        private const int DefaultBodyFont = 12;

        /// <summary>RectTransform del pannello root (utile per posizionamento/drag).</summary>
        public RectTransform RootRectTransform => _rootRt;

        /// <summary>
        /// Crea e attacca la card sotto un parent.
        /// </summary>
        public void AttachTo(Transform parent)
        {
            // Root
            _root = new GameObject("NpcSummaryCard");
            _root.transform.SetParent(parent, false);

            _rootRt = _root.AddComponent<RectTransform>();

            // Nota UX:
            // Questa card, rispetto alle patch precedenti, ha due sezioni in più:
            // - Action (in evidenza con rich-text color)
            // - Inventory (cibo trasportato)
            // Quindi alziamo un po' l'altezza di default.
            _rootRt.sizeDelta = new Vector2(DefaultWidth, 260);

            // IMPORTANT:
            // L'overlay lavora in coordinate canvas-local (anchoredPosition).
            // Quindi ancoriamo al centro del canvas per evitare interpretazioni ambigue con CanvasScaler.
            _rootRt.anchorMin = new Vector2(0.5f, 0.5f);
            _rootRt.anchorMax = new Vector2(0.5f, 0.5f);
            _rootRt.pivot = new Vector2(0.5f, 0.5f);

            _rootBg = _root.AddComponent<Image>();
            _rootBg.raycastTarget = true; // serve per ricevere eventi di drag sul pannello
            _rootBg.color = new Color(0f, 0f, 0f, 0.72f);

            var vLayout = _root.AddComponent<VerticalLayoutGroup>();
            vLayout.childControlHeight = true;
            vLayout.childControlWidth = true;
            vLayout.childForceExpandHeight = false;
            vLayout.childForceExpandWidth = true;
            vLayout.spacing = 4;
            vLayout.padding = new RectOffset(6, 6, 6, 6);

            _root.AddComponent<ContentSizeFitter>().verticalFit = ContentSizeFitter.FitMode.PreferredSize;

            // Header
            var header = BuildSection(_root.transform, "Header", DefaultHeaderFont, FontStyle.Bold);
            header.GetComponent<Image>().color = new Color(0.15f, 0.15f, 0.15f, 0.92f);
            _headerText = header.GetComponentInChildren<Text>();

            // Action (highlight)
            // Nota:
            // - Qui usiamo RichText per poter colorare in modo diverso le diverse azioni.
            // - La View NON decide l'azione: mostra solo quanto arriva dal World (NpcActionState).
            var action = BuildSection(_root.transform, "Action", DefaultBodyFont, FontStyle.Bold);
            action.GetComponent<Image>().color = new Color(0.10f, 0.10f, 0.10f, 0.90f);
            _actionText = action.GetComponentInChildren<Text>();
            if (_actionText != null) _actionText.supportRichText = true;

            // Inventory (cibo trasportato)
            var inv = BuildSection(_root.transform, "Inventory", DefaultBodyFont, FontStyle.Normal);
            inv.GetComponent<Image>().color = new Color(0.25f, 0.10f, 0.30f, 0.72f);
            _inventoryText = inv.GetComponentInChildren<Text>();

            // Memory traces (tabella A)
            var mem = BuildSection(_root.transform, "MemoryTraces", DefaultBodyFont, FontStyle.Normal);
            mem.GetComponent<Image>().color = new Color(0.45f, 0.35f, 0.05f, 0.75f);
            _memText = mem.GetComponentInChildren<Text>();

            // Known objects (tabella B)
            var obj = BuildSection(_root.transform, "KnownObjects", DefaultBodyFont, FontStyle.Normal);
            obj.GetComponent<Image>().color = new Color(0.05f, 0.25f, 0.45f, 0.72f);
            _objMemText = obj.GetComponentInChildren<Text>();

            SetVisible(false);
        }

        /// <summary>
        /// Aggiorna contenuti testuali.
        /// NOTA: nomino SetTexts (plurale) per compatibilità con patch precedenti.
        /// </summary>
        public void SetTexts(string header, string mem, string objMem)
        {
            if (_headerText != null) _headerText.text = header ?? string.Empty;
            if (_memText != null) _memText.text = mem ?? string.Empty;
            if (_objMemText != null) _objMemText.text = objMem ?? string.Empty;
        }

        /// <summary>
        /// Sezione Action:
        /// testo breve (idealmente 1 riga) con rich-text abilitato.
        /// 
        /// Esempio: "Action: &lt;color=#55FF55&gt;Eat&lt;/color&gt; (EatFromStock)".
        /// </summary>
        public void SetActionText(string richText)
        {
            if (_actionText != null)
                _actionText.text = richText ?? string.Empty;
        }

        /// <summary>
        /// Sezione Inventory:
        /// blocco informativo sul cibo trasportato dall'NPC (NpcPrivateFood).
        /// </summary>
        public void SetInventoryText(string text)
        {
            if (_inventoryText != null)
                _inventoryText.text = text ?? string.Empty;
        }

        /// <summary>Mostra/nasconde (senza distruggere).</summary>
        public void SetVisible(bool visible)
        {
            if (_root != null)
                _root.SetActive(visible);
        }

        /// <summary>
        /// Sposta la card in coordinate canvas-local (anchoredPosition).
        /// </summary>
        public void SetCanvasLocalPosition(Vector2 localPos)
        {
            if (_rootRt != null)
                _rootRt.anchoredPosition = localPos;
        }

        /// <summary>Posizione corrente (canvas-local).</summary>
        public Vector2 GetCanvasLocalPosition()
        {
            return _rootRt != null ? _rootRt.anchoredPosition : Vector2.zero;
        }

        // ============================================================
        // BUILD HELPERS
        // ============================================================

        private static Font GetUiFont()
        {
            // Unity recenti: Arial.ttf non è più valido come built-in font.
            var f = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            if (f != null) return f;

            // Fallback per versioni/ambienti strani.
            return Resources.GetBuiltinResource<Font>("Arial.ttf");
        }

        private static GameObject BuildSection(Transform parent, string name, int fontSize, FontStyle style)
        {
            var go = new GameObject(name);
            go.transform.SetParent(parent, false);

            var rt = go.AddComponent<RectTransform>();
            rt.sizeDelta = new Vector2(0, 0);

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
