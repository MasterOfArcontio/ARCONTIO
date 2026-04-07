using System.Collections.Generic;
using Arcontio.Core;
using UnityEngine;
using UnityEngine.UI;

namespace Arcontio.View.MapGrid
{
    /// <summary>
    /// UI view per la card di un NPC in modalità SummaryOverlay.
    ///
    /// Struttura (Patch 0.02.03):
    /// - Panel root (bg scuro)
    ///   - Header (NON collassabile)         : dati base NPC
    ///   - Action (collassabile)             : azione corrente (rich text)
    ///   - Inventory (collassabile)          : cibo trasportato e scorte private
    ///   - Comms (collassabile)              : token IN/OUT
    ///   - Landmarks (collassabile)          : contatori/indicatori landmark-edge conosciuti
    ///   - MemoryTraces (collassabile)       : tabella MemoryTrace
    ///   - KnownObjects (collassabile)       : tabella memoria oggetti/NPC osservati
    ///
    /// NOTE IMPORTANTI:
    /// - Questa classe è volutamente "prefabless": costruisce UI da codice per ridurre dipendenze di asset.
    /// - Tutta la logica di posizionamento (anchor + offset + drag) è gestita dal controller overlay.
    ///   Qui esponiamo solo API minime (SetTexts/SetVisible/RectTransform).
    ///
    /// Patch 0.02.03 (Day3 - Debug UX):
    /// 1) Aggiungiamo nella card un nuovo gruppo di info "Landmarks & Edges".
    /// 2) Rendiamo collassabili i gruppi di info (es. MemoryTraces, KnownObjects, ecc.) tramite un tasto.
    ///
    /// Motivazione:
    /// - La card NPC cresce progressivamente con nuove feature debug.
    /// - Senza foldout/collapse la leggibilità crolla e diventa difficile isolare una singola feature.
    /// </summary>
    public sealed class MapGridNpcSummaryCardView
    {
        private GameObject _root;
        private RectTransform _rootRt;
        private Image _rootBg;

        private const int DefaultWidth = 380;
        private const int DefaultHeaderFont = 14;
        private const int DefaultBodyFont = 12;

        /// <summary>RectTransform del pannello root (utile per posizionamento/drag).</summary>
        public RectTransform RootRectTransform => _rootRt;

        // ============================================================
        // SECTION KEYS (stabili)
        // ============================================================
        // Nota: usiamo chiavi costanti per poter memorizzare lo stato collapsed in modo deterministico.
        private const string KeyHeader = "Header";
        private const string KeyAction = "Action";
        private const string KeyInventory = "Inventory";
        private const string KeyComms = "Comms";
        private const string KeyLandmarks = "Landmarks";
        private const string KeyMemoryTraces = "MemoryTraces";
        private const string KeyKnownObjects = "KnownObjects";
        private const string KeyDnaDrift = "DnaDrift";

        // ============================================================
        // SECTION STATE
        // ============================================================

        private SectionView _headerSection;
        private SectionView _actionSection;
        private SectionView _inventorySection;
        private SectionView _commsSection;
        private SectionView _landmarksSection;
        private SectionView _memSection;
        private SectionView _objMemSection;
        private SectionView _dnaDriftSection;

        // ── DNA DRIFT bars ────────────────────────────────────────────────────
        // [axisIndex 0=Pref 1=Comp 2=Obl][domainIndex 0..7]
        private BarRow[][] _driftDomainRows;

        /// <summary>
        /// Stato collapse "globale" per sezione.
        ///
        /// Perché globale e non per-NPC?
        /// - Questa UI è un tool di debug: lo sviluppatore di solito decide "oggi voglio vedere solo X".
        /// - Se lo stato fosse per-NPC, il click andrebbe ripetuto per ogni card.
        ///
        /// È volutamente volatile (RAM only): non la persistiamo nel JSON di config.
        /// </summary>
        private static readonly Dictionary<string, bool> s_collapsedByKey = new();

        // ============================================================
        // LIFECYCLE
        // ============================================================

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
            // Anche se usiamo ContentSizeFitter (PreferredSize), impostare una sizeDelta di base
            // aiuta durante il drag e riduce i casi strani con layout che parte a zero.
            _rootRt.sizeDelta = new Vector2(DefaultWidth, 420);

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

            // ============================================================
            // BUILD SECTIONS
            // ============================================================

            // Header: NON collassabile (se nascondi l'header perdi contesto).
            _headerSection = BuildSection(_root.transform, KeyHeader, "Identity / State", DefaultHeaderFont, FontStyle.Bold, collapsible: false);
            _headerSection.RootImage.color = new Color(0.15f, 0.15f, 0.15f, 0.92f);

            // Action (highlight) - rich text.
            _actionSection = BuildSection(_root.transform, KeyAction, "Goal / Decision", DefaultBodyFont, FontStyle.Bold, collapsible: true);
            _actionSection.RootImage.color = new Color(0.10f, 0.10f, 0.10f, 0.90f);
            if (_actionSection.BodyText != null) _actionSection.BodyText.supportRichText = true;

            // Inventory (cibo trasportato)
            _inventorySection = BuildSection(_root.transform, KeyInventory, "Inventory / Needs", DefaultBodyFont, FontStyle.Normal, collapsible: true);
            _inventorySection.RootImage.color = new Color(0.25f, 0.10f, 0.30f, 0.72f);

            // Comms (token IN/OUT)
            _commsSection = BuildSection(_root.transform, KeyComms, "Comms", DefaultBodyFont, FontStyle.Normal, collapsible: true);
            _commsSection.RootImage.color = new Color(0.08f, 0.22f, 0.12f, 0.70f);
            if (_commsSection.BodyText != null) _commsSection.BodyText.supportRichText = true;

            // Landmarks / Edges (Patch 0.02.03)
            _landmarksSection = BuildSection(_root.transform, KeyLandmarks, "Navigation / MacroRoute", DefaultBodyFont, FontStyle.Normal, collapsible: true);
            _landmarksSection.RootImage.color = new Color(0.12f, 0.18f, 0.28f, 0.72f);

            // Memory traces (tabella A)
            _memSection = BuildSection(_root.transform, KeyMemoryTraces, "Memory Traces", DefaultBodyFont, FontStyle.Normal, collapsible: true);
            _memSection.RootImage.color = new Color(0.45f, 0.35f, 0.05f, 0.75f);

            // Known objects (tabella B)
            _objMemSection = BuildSection(_root.transform, KeyKnownObjects, "Perception / Knowledge", DefaultBodyFont, FontStyle.Normal, collapsible: true);
            _objMemSection.RootImage.color = new Color(0.05f, 0.25f, 0.45f, 0.72f);

            // DNA DRIFT (v0.04.06 → v0.04.07.b) — barre proporzionali uGUI
            _dnaDriftSection = BuildSection(_root.transform, KeyDnaDrift, "DNA DRIFT", DefaultBodyFont, FontStyle.Normal, collapsible: true);
            _dnaDriftSection.RootImage.color = new Color(0.30f, 0.10f, 0.15f, 0.78f);
            BuildDnaDriftRows(_dnaDriftSection);

            SetVisible(false);
        }

        // ============================================================
        // TEXT API (called by controller)
        // ============================================================

        /// <summary>
        /// Aggiorna contenuti testuali.
        /// NOTA: nomino SetTexts (plurale) per compatibilità con patch precedenti.
        /// </summary>
        public void SetTexts(string header, string mem, string objMem)
        {
            if (_headerSection?.BodyText != null) _headerSection.BodyText.text = header ?? string.Empty;
            if (_memSection?.BodyText != null) _memSection.BodyText.text = mem ?? string.Empty;
            if (_objMemSection?.BodyText != null) _objMemSection.BodyText.text = objMem ?? string.Empty;
        }

        /// <summary>
        /// Sezione Action:
        /// testo breve (idealmente 1 riga) con rich-text abilitato.
        /// 
        /// Esempio: "Action: &lt;color=#55FF55&gt;Eat&lt;/color&gt; (EatFromStock)".
        /// </summary>
        public void SetActionText(string richText)
        {
            if (_actionSection?.BodyText != null)
                _actionSection.BodyText.text = richText ?? string.Empty;
        }

        /// <summary>
        /// Sezione Inventory:
        /// blocco informativo sul cibo trasportato dall'NPC (NpcPrivateFood).
        /// </summary>
        public void SetInventoryText(string text)
        {
            if (_inventorySection?.BodyText != null)
                _inventorySection.BodyText.text = text ?? string.Empty;
        }

        /// <summary>
        /// Sezione Comms (Patch 0.01P2):
        /// mostra un estratto delle comunicazioni simboliche (TokenEnvelope) dell'NPC.
        /// </summary>
        public void SetCommsText(string richText)
        {
            if (_commsSection?.BodyText != null)
                _commsSection.BodyText.text = richText ?? string.Empty;
        }

        /// <summary>
        /// Sezione Landmarks (Patch 0.02.03 / Day3):
        /// mostra contatori e indicatori della "mappa mentale" (subset) dell'NPC.
        /// </summary>
        public void SetLandmarksText(string text)
        {
            if (_landmarksSection?.BodyText != null)
                _landmarksSection.BodyText.text = text ?? string.Empty;
        }

        // ── Nomi dominio (indici 1..8 di DomainKind) ─────────────────────────
        private static readonly string[] DomainLabels =
            { "Agric", "Costru", "Sicur", "Artig", "Magaz", "Gover", "Socia", "Esplor" };

        private static readonly string[] AxisLabels = { "PREFERENZE", "COMPETENZA", "OBBLIGO" };

        /// <summary>
        /// Aggiorna la sezione DNA DRIFT con barre proporzionali.
        /// Chiama questo metodo ogni frame quando la card è visibile.
        /// </summary>
        public void UpdateDnaDrift(NpcDnaProfile dna, NpcProfile profile, DnaDistanceResult result)
        {
            if (_driftDomainRows == null) return; // non ancora costruito

            float[] prefSeeds = dna.Preferences.Seeds;
            float[] oblSeeds  = dna.ObligationFrame.Seeds;

            for (int d = 1; d < (int)DomainKind.COUNT; d++)
            {
                int di = d - 1;

                float dnaPref  = prefSeeds != null && d < prefSeeds.Length ? prefSeeds[d] : 0f;
                float currPref = profile.Preference.Values[d];
                _driftDomainRows[0][di].Set(DomainLabels[di], Mathf.Abs(currPref - dnaPref));

                // Competenza: scostamento = quanto ha sviluppato dal punto di partenza (0)
                float currComp = profile.Competence.Values[d];
                _driftDomainRows[1][di].Set(DomainLabels[di], currComp);

                float dnaObl  = oblSeeds != null && d < oblSeeds.Length ? oblSeeds[d] : 0f;
                float currObl = profile.Obligation.Values[d];
                _driftDomainRows[2][di].Set(DomainLabels[di], Mathf.Abs(currObl - dnaObl));
            }
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
        // DNA DRIFT — build e bar rows
        // ============================================================

        private const float BarMaxWidth  = 130f;
        private const float BarHeight    = 10f;
        private const float RowHeight    = 14f;
        private const int   LabelWidth   = 56;
        private const int   ValueWidth   = 32;

        /// <summary>
        /// Inner class che rappresenta una riga con barra proporzionale.
        /// Usa Image.Type.Filled per evitare conflitti col layout group.
        /// </summary>
        private sealed class BarRow
        {
            public Text  LabelText;
            public Image BarFill;   // Image.Type.Filled — fillAmount controlla la proporzione
            public Text  ValueText;

            public void Set(string label, float value01)
            {
                float v = Mathf.Clamp01(value01);
                LabelText.text = label;
                BarFill.fillAmount = v;
                BarFill.color = BarColor(v);
                ValueText.text = v.ToString("0.00");
            }

            private static Color BarColor(float v)
            {
                if (v < 0.35f) return new Color(0.22f, 0.75f, 0.22f);
                if (v < 0.65f) return new Color(0.90f, 0.78f, 0.10f);
                return new Color(0.85f, 0.18f, 0.18f);
            }
        }

        private void BuildDnaDriftRows(SectionView section)
        {
            // Nascondi il Text standard (non usiamo testo per questa sezione)
            if (section.BodyText != null)
                section.BodyText.gameObject.SetActive(false);

            var parent = section.BodyGo.transform;

            // ── Righe per-dominio (3 assi × 8 domini) ────────────────────────
            _driftDomainRows = new BarRow[3][];
            for (int axis = 0; axis < 3; axis++)
            {
                MakeAxisLabel(parent, AxisLabels[axis]);

                _driftDomainRows[axis] = new BarRow[8];
                for (int d = 0; d < 8; d++)
                    _driftDomainRows[axis][d] = MakeBarRow(parent, DomainLabels[d]);
            }
        }

        private static BarRow MakeBarRow(Transform parent, string label)
        {
            var rowGo = new GameObject("BarRow_" + label);
            rowGo.transform.SetParent(parent, false);

            var hl = rowGo.AddComponent<HorizontalLayoutGroup>();
            // childControlHeight/Width=true: HLG assegna esattamente preferredHeight/Width ai figli
            // childForceExpand=false: i figli NON si allargano per riempire lo spazio extra
            hl.childControlHeight     = true;
            hl.childControlWidth      = true;
            hl.childForceExpandHeight = false;
            hl.childForceExpandWidth  = false;
            hl.spacing = 4;

            rowGo.AddComponent<LayoutElement>().preferredHeight = RowHeight;

            // Label
            var labelGo = new GameObject("Label");
            labelGo.transform.SetParent(rowGo.transform, false);
            var labelTxt = labelGo.AddComponent<Text>();
            labelTxt.font = GetUiFont();
            labelTxt.fontSize = 10;
            labelTxt.alignment = TextAnchor.MiddleLeft;
            labelTxt.horizontalOverflow = HorizontalWrapMode.Overflow;
            labelTxt.verticalOverflow   = VerticalWrapMode.Truncate;
            labelTxt.raycastTarget = false;
            var labelLe = labelGo.AddComponent<LayoutElement>();
            labelLe.preferredWidth  = LabelWidth;
            labelLe.preferredHeight = RowHeight;

            // Barra sfondo (nero) — contiene la fill via Image.Type.Filled
            var barBgGo = new GameObject("BarBg");
            barBgGo.transform.SetParent(rowGo.transform, false);
            var barBgImg = barBgGo.AddComponent<Image>();
            barBgImg.color = Color.black;
            barBgImg.raycastTarget = false;
            var barBgLe = barBgGo.AddComponent<LayoutElement>();
            barBgLe.preferredWidth  = BarMaxWidth;
            barBgLe.preferredHeight = BarHeight;

            // Barra fill — Image.Type.Filled copre l'intero sfondo
            // fillAmount=[0..1] controlla quanta parte colorata è visibile
            var barFillGo = new GameObject("BarFill");
            barFillGo.transform.SetParent(barBgGo.transform, false);
            var barFillImg = barFillGo.AddComponent<Image>();
            barFillImg.raycastTarget = false;
            barFillImg.type        = Image.Type.Filled;
            barFillImg.fillMethod  = Image.FillMethod.Horizontal;
            barFillImg.fillOrigin  = (int)Image.OriginHorizontal.Left;
            barFillImg.fillAmount  = 0f;
            var barFillRt = barFillGo.GetComponent<RectTransform>();
            barFillRt.anchorMin = Vector2.zero;
            barFillRt.anchorMax = Vector2.one;
            barFillRt.offsetMin = Vector2.zero;
            barFillRt.offsetMax = Vector2.zero;

            // Valore numerico
            var valueGo = new GameObject("Value");
            valueGo.transform.SetParent(rowGo.transform, false);
            var valueTxt = valueGo.AddComponent<Text>();
            valueTxt.font = GetUiFont();
            valueTxt.fontSize = 10;
            valueTxt.alignment = TextAnchor.MiddleRight;
            valueTxt.horizontalOverflow = HorizontalWrapMode.Overflow;
            valueTxt.verticalOverflow   = VerticalWrapMode.Truncate;
            valueTxt.raycastTarget = false;
            var valueLe = valueGo.AddComponent<LayoutElement>();
            valueLe.preferredWidth  = ValueWidth;
            valueLe.preferredHeight = RowHeight;

            return new BarRow
            {
                LabelText = labelTxt,
                BarFill   = barFillImg,
                ValueText = valueTxt
            };
        }

        private static void MakeAxisLabel(Transform parent, string text)
        {
            var go = new GameObject("AxisLabel_" + text);
            go.transform.SetParent(parent, false);

            var le = go.AddComponent<LayoutElement>();
            le.preferredHeight = 16f;

            var txt = go.AddComponent<Text>();
            txt.font = GetUiFont();
            txt.fontSize = 10;
            txt.fontStyle = FontStyle.Bold;
            txt.alignment = TextAnchor.MiddleLeft;
            txt.horizontalOverflow = HorizontalWrapMode.Overflow;
            txt.verticalOverflow   = VerticalWrapMode.Truncate;
            txt.color = new Color(0.75f, 0.90f, 1.00f);
            txt.raycastTarget = false;
            txt.text = text;
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

        private sealed class SectionView
        {
            public string Key;
            public GameObject Root;
            public Image RootImage;
            public GameObject BodyGo;
            public Text BodyText;

            public Button ToggleButton;
            public Text ToggleText;
        }

        private SectionView BuildSection(Transform parent, string key, string title, int fontSize, FontStyle style, bool collapsible)
        {
            var go = new GameObject(title);
            go.transform.SetParent(parent, false);

            var rt = go.AddComponent<RectTransform>();
            rt.sizeDelta = new Vector2(0, 0);

            var img = go.AddComponent<Image>();
            img.raycastTarget = false;

            // Layout verticale: HeaderRow + Body
            var layout = go.AddComponent<VerticalLayoutGroup>();
            layout.childControlHeight = true;
            layout.childControlWidth = true;
            layout.childForceExpandHeight = false;
            layout.childForceExpandWidth = true;
            layout.spacing = 2;
            layout.padding = new RectOffset(6, 6, 4, 4);

            // ------------------------------------------------------------
            // Header row (titolo + bottone collapse)
            // ------------------------------------------------------------
            var headerRow = new GameObject("HeaderRow");
            headerRow.transform.SetParent(go.transform, false);

            var hrLayout = headerRow.AddComponent<HorizontalLayoutGroup>();
            hrLayout.childControlHeight = true;
            hrLayout.childControlWidth = false;
            hrLayout.childForceExpandHeight = false;
            hrLayout.childForceExpandWidth = false;
            hrLayout.spacing = 6;
            hrLayout.padding = new RectOffset(0, 0, 0, 0);

            // Titolo sezione
            var titleGo = new GameObject("Title");
            titleGo.transform.SetParent(headerRow.transform, false);

            var titleText = titleGo.AddComponent<Text>();
            titleText.raycastTarget = false;
            titleText.font = GetUiFont();
            titleText.fontSize = fontSize;
            titleText.fontStyle = FontStyle.Bold;
            titleText.alignment = TextAnchor.UpperLeft;
            titleText.horizontalOverflow = HorizontalWrapMode.Overflow;
            titleText.verticalOverflow = VerticalWrapMode.Truncate;
            titleText.text = title;

            // Il titolo occupa lo spazio orizzontale disponibile.
            titleGo.AddComponent<LayoutElement>().flexibleWidth = 1f;

            // Bottone collapse
            Button btn = null;
            Text btnText = null;
            if (collapsible)
            {
                var btnGo = new GameObject("Toggle");
                btnGo.transform.SetParent(headerRow.transform, false);

                var btnImg = btnGo.AddComponent<Image>();
                btnImg.color = new Color(1f, 1f, 1f, 0.08f);
                btnImg.raycastTarget = true;

                btn = btnGo.AddComponent<Button>();

                var le = btnGo.AddComponent<LayoutElement>();
                le.preferredWidth = 24f;
                le.preferredHeight = 18f;

                var tGo = new GameObject("Text");
                tGo.transform.SetParent(btnGo.transform, false);

                var tRt = tGo.AddComponent<RectTransform>();
                tRt.anchorMin = Vector2.zero;
                tRt.anchorMax = Vector2.one;
                tRt.offsetMin = Vector2.zero;
                tRt.offsetMax = Vector2.zero;

                btnText = tGo.AddComponent<Text>();
                btnText.raycastTarget = false;
                btnText.font = GetUiFont();
                btnText.fontSize = fontSize;
                btnText.fontStyle = FontStyle.Bold;
                btnText.alignment = TextAnchor.MiddleCenter;
            }

            // ------------------------------------------------------------
            // Body
            // ------------------------------------------------------------
            var bodyGo = new GameObject("Body");
            bodyGo.transform.SetParent(go.transform, false);

            var bodyLayout = bodyGo.AddComponent<VerticalLayoutGroup>();
            bodyLayout.childControlHeight = true;
            bodyLayout.childControlWidth = true;
            bodyLayout.childForceExpandHeight = false;
            bodyLayout.childForceExpandWidth = true;
            bodyLayout.padding = new RectOffset(0, 0, 0, 0);

            var textGo = new GameObject("Text");
            textGo.transform.SetParent(bodyGo.transform, false);

            var text = textGo.AddComponent<Text>();
            text.raycastTarget = false;
            text.font = GetUiFont();
            text.fontSize = fontSize;
            text.fontStyle = style;
            text.alignment = TextAnchor.UpperLeft;
            text.horizontalOverflow = HorizontalWrapMode.Wrap;
            text.verticalOverflow = VerticalWrapMode.Overflow;

            var section = new SectionView
            {
                Key = key,
                Root = go,
                RootImage = img,
                BodyGo = bodyGo,
                BodyText = text,
                ToggleButton = btn,
                ToggleText = btnText,
            };

            // ============================================================
            // Init collapse state
            // ============================================================
            // Regola:
            // - Header non collassabile: sempre aperto.
            // - Se collassabile: leggiamo lo stato globale e inizializziamo coerentemente.
            bool collapsed = false;
            if (collapsible && s_collapsedByKey.TryGetValue(key, out var v))
                collapsed = v;

            SetSectionCollapsed(section, collapsed);

            if (collapsible && section.ToggleButton != null)
            {
                section.ToggleButton.onClick.AddListener(() =>
                {
                    bool next = !IsSectionCollapsed(key);
                    s_collapsedByKey[key] = next;
                    SetSectionCollapsed(section, next);
                });
            }

            return section;
        }

        private static bool IsSectionCollapsed(string key)
        {
            return s_collapsedByKey.TryGetValue(key, out var v) && v;
        }

        private static void SetSectionCollapsed(SectionView section, bool collapsed)
        {
            if (section == null) return;

            // Body on/off
            if (section.BodyGo != null)
                section.BodyGo.SetActive(!collapsed);

            // Icon
            if (section.ToggleText != null)
                section.ToggleText.text = collapsed ? "▸" : "▾";
        }
    }
}
