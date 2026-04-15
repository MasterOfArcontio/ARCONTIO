using UnityEngine;
using UnityEngine.UI;

namespace Arcontio.View.MapGrid
{
    // =============================================================================
    // MapGridExplainabilityPanelPage
    // =============================================================================
    /// <summary>
    /// <para>
    /// Pagine disponibili nel pannello laterale dell'Explainability Layer.
    /// </para>
    ///
    /// <para><b>Separazione visuale dei layer diagnostici</b></para>
    /// <para>
    /// Ogni pagina risponde a una domanda diversa: Memory mostra cosa entra nella
    /// memoria, Belief mostra cosa diventa conoscenza soggettiva, Decision mostra la
    /// scelta intenzionale e Pathfinding mostra l'esecuzione spaziale.
    /// </para>
    /// </summary>
    public enum MapGridExplainabilityPanelPage
    {
        Memory = 0,
        Belief = 1,
        Decision = 2,
        Pathfinding = 3
    }

    // =============================================================================
    // MapGridMovementExplainabilityPanelView
    // =============================================================================
    /// <summary>
    /// <para>
    /// Pannello debug prefabless fissato al lato destro della MapGrid. Il nome della
    /// classe resta quello storico per non rompere gli agganci esistenti, ma la view
    /// ora ospita l'intero pannello EL tabbed: Memory, Belief, Decision e Pathfinding.
    /// </para>
    ///
    /// <para><b>UI diagnostica senza accesso al dominio</b></para>
    /// <para>
    /// La view non legge World, registry, BeliefStore o JSONL. Riceve solo stringhe
    /// gia' formattate dal controller overlay e le distribuisce in sub-pannelli
    /// scrollabili con colori rich text.
    /// </para>
    ///
    /// <para><b>Struttura interna:</b></para>
    /// <list type="bullet">
    ///   <item><b>Header</b>: titolo, metadati NPC/tick e tab cliccabili.</item>
    ///   <item><b>ScrollRect</b>: body unico, con header fisso e contenuto scrollabile.</item>
    ///   <item><b>Page roots</b>: contenitori separati per Memory, Belief, Decision e Pathfinding.</item>
    ///   <item><b>Sub-pannelli</b>: cornice, titolo, pallino colorato e testo rich.</item>
    /// </list>
    /// </summary>
    public sealed class MapGridMovementExplainabilityPanelView
    {
        private const int DefaultWidth = 430;
        private const int HorizontalMargin = 12;
        private const int VerticalMargin = 10;
        private const int DefaultTitleFont = 14;
        private const int DefaultBodyFont = 12;

        private readonly Button[] _tabButtons = new Button[4];
        private readonly Text[] _tabTexts = new Text[4];
        private readonly GameObject[] _pageRoots = new GameObject[4];

        private GameObject _root;
        private RectTransform _rootRt;
        private Text _titleText;
        private Text _headerMetaText;
        private Text _diagnosticText;
        private ScrollRect _scrollRect;
        private RectTransform _scrollContent;

        private Text _memoryStoreText;
        private Text _memoryLatestText;
        private Text _memoryTimelineText;
        private Text _beliefEntriesText;
        private Text _beliefQueryText;
        private Text _beliefMutationText;
        private Text _decisionSelectedText;
        private Text _decisionCandidatesText;
        private Text _decisionBridgeText;
        private Text _pathIntentPlanText;
        private Text _pathEventsText;

        private MapGridExplainabilityPanelPage _activePage = MapGridExplainabilityPanelPage.Memory;

        private const int MaxSectionTextChars = 6000;
        private const int EstimatedCharsPerLine = 52;
        private const float MaxSectionTextHeight = 900f;

        public RectTransform RootRectTransform => _rootRt;
        public MapGridExplainabilityPanelPage ActivePage => _activePage;

        // =============================================================================
        // AttachTo
        // =============================================================================
        /// <summary>
        /// <para>
        /// Crea il pannello sotto il parent indicato e lo ancora al lato destro del
        /// canvas. Il pannello parte nascosto e viene popolato dal SummaryOverlay.
        /// </para>
        /// </summary>
        public void AttachTo(Transform parent)
        {
            if (_root != null)
                return;

            _root = new GameObject("ExplainabilityRightPanel");
            _root.transform.SetParent(parent, false);

            _rootRt = _root.AddComponent<RectTransform>();
            _rootRt.anchorMin = new Vector2(1f, 0f);
            _rootRt.anchorMax = new Vector2(1f, 1f);
            _rootRt.pivot = new Vector2(1f, 0.5f);
            _rootRt.offsetMin = new Vector2(-DefaultWidth - HorizontalMargin, VerticalMargin);
            _rootRt.offsetMax = new Vector2(-HorizontalMargin, -VerticalMargin);

            var rootBg = _root.AddComponent<Image>();
            rootBg.raycastTarget = true;
            rootBg.color = ColorFromHex("#0D1117", 0.92f);

            var layout = _root.AddComponent<VerticalLayoutGroup>();
            layout.childControlHeight = true;
            layout.childControlWidth = true;
            layout.childForceExpandHeight = false;
            layout.childForceExpandWidth = true;
            layout.spacing = 6;
            layout.padding = new RectOffset(8, 8, 8, 8);

            BuildHeader(_root.transform);
            BuildScrollBody(_root.transform);
            BuildPages();
            SetActivePage(MapGridExplainabilityPanelPage.Memory);

            _root.transform.SetAsLastSibling();
            SetVisible(false);
        }

        // =============================================================================
        // SetVisible
        // =============================================================================
        /// <summary>
        /// <para>
        /// Mostra o nasconde il pannello senza distruggerne la gerarchia UI.
        /// </para>
        /// </summary>
        public void SetVisible(bool visible)
        {
            if (_root != null)
                _root.SetActive(visible);
        }

        // =============================================================================
        // SetHeader
        // =============================================================================
        /// <summary>
        /// <para>
        /// Aggiorna titolo e metadati comuni del pannello.
        /// </para>
        /// </summary>
        public void SetHeader(string title, string headerMeta)
        {
            if (_titleText != null)
                _titleText.text = title ?? string.Empty;

            if (_headerMetaText != null)
                _headerMetaText.text = headerMeta ?? string.Empty;
        }

        // =============================================================================
        // SetDiagnostics
        // =============================================================================
        /// <summary>
        /// <para>
        /// Aggiorna la riga diagnostica sempre visibile sotto l'header. Questa riga
        /// serve a distinguere rapidamente tre casi: nessuna selezione, NPC selezionato
        /// ma senza registry, NPC selezionato con dati EL-MBQD presenti.
        /// </para>
        ///
        /// <para><b>Principio architetturale: osservabilita' del boundary UI</b></para>
        /// <para>
        /// La riga non legge dati dal dominio: riceve solo testo dal controller overlay.
        /// In questo modo rende visibile lo stato del collegamento UI senza introdurre
        /// accessi globali o dipendenze dirette da World, registry o JSONL.
        /// </para>
        ///
        /// <para><b>Struttura interna:</b></para>
        /// <list type="bullet">
        ///   <item><b>diagnosticText</b>: testo sintetico gia' formattato dal chiamante.</item>
        ///   <item><b>SetTextIfReady</b>: stesso percorso bounded usato dalle sezioni scrollabili.</item>
        /// </list>
        /// </summary>
        public void SetDiagnostics(string diagnosticText)
        {
            SetTextIfReady(_diagnosticText, string.IsNullOrWhiteSpace(diagnosticText)
                ? "<color=#6E7681>diagnostica UI non disponibile</color>"
                : $"<color=#6E7681>{diagnosticText}</color>");
        }

        // =============================================================================
        // SetText
        // =============================================================================
        /// <summary>
        /// <para>
        /// Compatibilita' con la vecchia API pathfinding: aggiorna la pagina
        /// Pathfinding usando i blocchi gia' formattati dall'overlay.
        /// </para>
        /// </summary>
        public void SetText(string title, string headerMeta, string intentPlan, string events)
        {
            SetHeader(title, headerMeta);
            SetPathfindingText(intentPlan, events);
        }

        public void SetMemoryText(string storeSummary, string latestTrace, string timeline)
        {
            SetTextIfReady(_memoryStoreText, storeSummary);
            SetTextIfReady(_memoryLatestText, latestTrace);
            SetTextIfReady(_memoryTimelineText, timeline);
        }

        public void SetBeliefText(string entries, string latestQuery, string latestMutation)
        {
            SetTextIfReady(_beliefEntriesText, entries);
            SetTextIfReady(_beliefQueryText, latestQuery);
            SetTextIfReady(_beliefMutationText, latestMutation);
        }

        public void SetDecisionText(string selectedDecision, string candidates, string bridge)
        {
            SetTextIfReady(_decisionSelectedText, selectedDecision);
            SetTextIfReady(_decisionCandidatesText, candidates);
            SetTextIfReady(_decisionBridgeText, bridge);
        }

        public void SetPathfindingText(string intentPlan, string events)
        {
            SetTextIfReady(_pathIntentPlanText, intentPlan);
            SetTextIfReady(_pathEventsText, events);
        }

        // =============================================================================
        // SetActivePage
        // =============================================================================
        /// <summary>
        /// <para>
        /// Attiva una pagina del pannello e aggiorna lo stato visuale delle tab.
        /// </para>
        /// </summary>
        public void SetActivePage(MapGridExplainabilityPanelPage page)
        {
            _activePage = page;

            for (int i = 0; i < _pageRoots.Length; i++)
            {
                bool active = i == (int)page;
                if (_pageRoots[i] != null)
                    _pageRoots[i].SetActive(active);

                if (_tabTexts[i] != null)
                    _tabTexts[i].color = active ? ColorFromHex("#C9D1D9", 1f) : ColorFromHex("#8B949E", 1f);
            }

            if (_scrollContent != null)
                _scrollContent.anchoredPosition = Vector2.zero;

            if (_scrollRect != null)
                _scrollRect.verticalNormalizedPosition = 1f;
        }

        private void BuildHeader(Transform parent)
        {
            var headerGo = new GameObject("Header");
            headerGo.transform.SetParent(parent, false);

            var headerImage = headerGo.AddComponent<Image>();
            headerImage.raycastTarget = false;
            headerImage.color = ColorFromHex("#161B22", 0.98f);

            var headerLayout = headerGo.AddComponent<VerticalLayoutGroup>();
            headerLayout.childControlHeight = true;
            headerLayout.childControlWidth = true;
            headerLayout.childForceExpandHeight = false;
            headerLayout.childForceExpandWidth = true;
            headerLayout.spacing = 4;
            headerLayout.padding = new RectOffset(8, 8, 7, 0);

            var headerLe = headerGo.AddComponent<LayoutElement>();
            headerLe.minHeight = 108f;
            headerLe.preferredHeight = 114f;
            headerLe.flexibleHeight = 0f;

            _titleText = CreateText("Title", headerGo.transform, DefaultTitleFont, FontStyle.Bold, ColorFromHex("#E6EDF3", 1f), TextAnchor.MiddleLeft);
            _titleText.text = "Explainability Layer";

            _headerMetaText = CreateText("Meta", headerGo.transform, 10, FontStyle.Normal, ColorFromHex("#8B949E", 1f), TextAnchor.MiddleLeft);
            _headerMetaText.text = string.Empty;

            _diagnosticText = CreateText("Diagnostics", headerGo.transform, 10, FontStyle.Normal, ColorFromHex("#6E7681", 1f), TextAnchor.MiddleLeft);
            _diagnosticText.text = "<color=#6E7681>diagnostica UI in attesa</color>";

            BuildTabs(headerGo.transform);
        }

        private void BuildTabs(Transform parent)
        {
            var tabsGo = new GameObject("Tabs");
            tabsGo.transform.SetParent(parent, false);

            var tabsLayout = tabsGo.AddComponent<HorizontalLayoutGroup>();
            tabsLayout.childControlHeight = true;
            tabsLayout.childControlWidth = true;
            tabsLayout.childForceExpandHeight = true;
            tabsLayout.childForceExpandWidth = false;
            tabsLayout.spacing = 2;
            tabsLayout.padding = new RectOffset(0, 0, 2, 0);

            var tabsLe = tabsGo.AddComponent<LayoutElement>();
            tabsLe.minHeight = 26f;
            tabsLe.preferredHeight = 28f;
            tabsLe.flexibleHeight = 0f;

            CreateTab(tabsGo.transform, MapGridExplainabilityPanelPage.Memory, "Memory");
            CreateTab(tabsGo.transform, MapGridExplainabilityPanelPage.Belief, "Belief");
            CreateTab(tabsGo.transform, MapGridExplainabilityPanelPage.Decision, "Decision");
            CreateTab(tabsGo.transform, MapGridExplainabilityPanelPage.Pathfinding, "Pathfinding");
        }

        private void CreateTab(Transform parent, MapGridExplainabilityPanelPage page, string label)
        {
            int index = (int)page;
            var go = new GameObject(label);
            go.transform.SetParent(parent, false);

            var image = go.AddComponent<Image>();
            image.color = ColorFromHex("#0D1117", 0.1f);
            image.raycastTarget = true;

            var button = go.AddComponent<Button>();
            button.transition = Selectable.Transition.ColorTint;
            button.targetGraphic = image;
            button.onClick.AddListener(() => SetActivePage(page));
            _tabButtons[index] = button;

            var le = go.AddComponent<LayoutElement>();
            le.minWidth = page == MapGridExplainabilityPanelPage.Pathfinding ? 92f : 66f;
            le.preferredHeight = 24f;
            le.flexibleWidth = 0f;

            var text = CreateText("Label", go.transform, 10, FontStyle.Normal, ColorFromHex("#8B949E", 1f), TextAnchor.MiddleCenter);
            text.text = label;
            _tabTexts[index] = text;
        }

        private void BuildScrollBody(Transform parent)
        {
            var contentGo = new GameObject("BodyDirectNoScroll");
            contentGo.transform.SetParent(parent, false);
            _scrollContent = contentGo.AddComponent<RectTransform>();
            _scrollContent.anchorMin = new Vector2(0f, 0f);
            _scrollContent.anchorMax = new Vector2(1f, 1f);
            _scrollContent.pivot = new Vector2(0.5f, 1f);
            _scrollContent.offsetMin = Vector2.zero;
            _scrollContent.offsetMax = Vector2.zero;
            _scrollContent.sizeDelta = new Vector2(0f, 620f);

            var bodyImage = contentGo.AddComponent<Image>();
            bodyImage.raycastTarget = true;
            bodyImage.color = ColorFromHex("#010409", 0.52f);

            var bodyLe = contentGo.AddComponent<LayoutElement>();
            bodyLe.minHeight = 360f;
            bodyLe.preferredHeight = 620f;
            bodyLe.flexibleHeight = 1f;
            bodyLe.flexibleWidth = 1f;

            var contentLayout = contentGo.AddComponent<VerticalLayoutGroup>();
            contentLayout.childControlHeight = true;
            contentLayout.childControlWidth = true;
            contentLayout.childForceExpandHeight = false;
            contentLayout.childForceExpandWidth = true;
            contentLayout.spacing = 8;
            contentLayout.padding = new RectOffset(2, 2, 2, 18);

            var fitter = contentGo.AddComponent<ContentSizeFitter>();
            fitter.verticalFit = ContentSizeFitter.FitMode.PreferredSize;
            _scrollRect = null;
        }

        private void BuildPages()
        {
            _pageRoots[(int)MapGridExplainabilityPanelPage.Memory] = CreatePage("MemoryPage");
            _memoryStoreText = CreateSection(_pageRoots[(int)MapGridExplainabilityPanelPage.Memory].transform, "memory store", "#58A6FF");
            _memoryLatestText = CreateSection(_pageRoots[(int)MapGridExplainabilityPanelPage.Memory].transform, "ultima traccia encodata", "#D29922");
            _memoryTimelineText = CreateSection(_pageRoots[(int)MapGridExplainabilityPanelPage.Memory].transform, "timeline memory recente", "#6E7681");

            _pageRoots[(int)MapGridExplainabilityPanelPage.Belief] = CreatePage("BeliefPage");
            _beliefEntriesText = CreateSection(_pageRoots[(int)MapGridExplainabilityPanelPage.Belief].transform, "belief entries recenti", "#3FB950");
            _beliefQueryText = CreateSection(_pageRoots[(int)MapGridExplainabilityPanelPage.Belief].transform, "ultima query eseguita", "#58A6FF");
            _beliefMutationText = CreateSection(_pageRoots[(int)MapGridExplainabilityPanelPage.Belief].transform, "ultima mutazione belief", "#D29922");

            _pageRoots[(int)MapGridExplainabilityPanelPage.Decision] = CreatePage("DecisionPage");
            _decisionSelectedText = CreateSection(_pageRoots[(int)MapGridExplainabilityPanelPage.Decision].transform, "intenzione selezionata", "#3FB950");
            _decisionCandidatesText = CreateSection(_pageRoots[(int)MapGridExplainabilityPanelPage.Decision].transform, "candidati e score breakdown", "#D29922");
            _decisionBridgeText = CreateSection(_pageRoots[(int)MapGridExplainabilityPanelPage.Decision].transform, "bridge decision -> command", "#58A6FF");

            _pageRoots[(int)MapGridExplainabilityPanelPage.Pathfinding] = CreatePage("PathfindingPage");
            _pathIntentPlanText = CreateSection(_pageRoots[(int)MapGridExplainabilityPanelPage.Pathfinding].transform, "intent e plan", "#9FC5E8");
            _pathEventsText = CreateSection(_pageRoots[(int)MapGridExplainabilityPanelPage.Pathfinding].transform, "eventi pathfinding", "#FFD966");
        }

        private GameObject CreatePage(string name)
        {
            var go = new GameObject(name);
            go.transform.SetParent(_scrollContent, false);
            var rectTransform = go.AddComponent<RectTransform>();
            rectTransform.anchorMin = new Vector2(0f, 1f);
            rectTransform.anchorMax = new Vector2(1f, 1f);
            rectTransform.pivot = new Vector2(0.5f, 1f);
            rectTransform.offsetMin = Vector2.zero;
            rectTransform.offsetMax = Vector2.zero;

            var layout = go.AddComponent<VerticalLayoutGroup>();
            layout.childControlHeight = true;
            layout.childControlWidth = true;
            layout.childForceExpandHeight = false;
            layout.childForceExpandWidth = true;
            layout.spacing = 8;
            layout.padding = new RectOffset(0, 0, 0, 0);

            var fitter = go.AddComponent<ContentSizeFitter>();
            fitter.verticalFit = ContentSizeFitter.FitMode.PreferredSize;

            var layoutElement = go.AddComponent<LayoutElement>();
            layoutElement.minHeight = 300f;
            layoutElement.preferredHeight = 420f;
            layoutElement.flexibleWidth = 1f;
            return go;
        }

        private Text CreateSection(Transform parent, string title, string dotHex)
        {
            var sectionGo = new GameObject(title);
            sectionGo.transform.SetParent(parent, false);
            var sectionRt = sectionGo.AddComponent<RectTransform>();
            sectionRt.anchorMin = new Vector2(0f, 1f);
            sectionRt.anchorMax = new Vector2(1f, 1f);
            sectionRt.pivot = new Vector2(0.5f, 1f);
            sectionRt.offsetMin = Vector2.zero;
            sectionRt.offsetMax = Vector2.zero;

            var image = sectionGo.AddComponent<Image>();
            image.raycastTarget = false;
            image.color = ColorFromHex("#0D1117", 0.72f);

            var sectionLayout = sectionGo.AddComponent<VerticalLayoutGroup>();
            sectionLayout.childControlHeight = true;
            sectionLayout.childControlWidth = true;
            sectionLayout.childForceExpandHeight = false;
            sectionLayout.childForceExpandWidth = true;
            sectionLayout.spacing = 0;
            sectionLayout.padding = new RectOffset(0, 0, 0, 0);

            var fitter = sectionGo.AddComponent<ContentSizeFitter>();
            fitter.verticalFit = ContentSizeFitter.FitMode.PreferredSize;

            var sectionLe = sectionGo.AddComponent<LayoutElement>();
            sectionLe.minHeight = 96f;
            sectionLe.preferredHeight = 150f;
            sectionLe.flexibleWidth = 1f;

            var headerGo = new GameObject("Header");
            headerGo.transform.SetParent(sectionGo.transform, false);
            var headerImage = headerGo.AddComponent<Image>();
            headerImage.raycastTarget = false;
            headerImage.color = ColorFromHex("#161B22", 0.96f);

            var headerLayout = headerGo.AddComponent<HorizontalLayoutGroup>();
            headerLayout.childControlHeight = true;
            headerLayout.childControlWidth = false;
            headerLayout.childForceExpandHeight = true;
            headerLayout.childForceExpandWidth = false;
            headerLayout.spacing = 6;
            headerLayout.padding = new RectOffset(7, 7, 4, 4);

            var headerLe = headerGo.AddComponent<LayoutElement>();
            headerLe.minHeight = 25f;
            headerLe.preferredHeight = 27f;

            var dotGo = new GameObject("Dot");
            dotGo.transform.SetParent(headerGo.transform, false);
            var dotImage = dotGo.AddComponent<Image>();
            dotImage.raycastTarget = false;
            dotImage.color = ColorFromHex(dotHex, 1f);
            var dotLe = dotGo.AddComponent<LayoutElement>();
            dotLe.minWidth = 6f;
            dotLe.preferredWidth = 6f;
            dotLe.minHeight = 6f;
            dotLe.preferredHeight = 6f;

            var headerText = CreateText("Title", headerGo.transform, 10, FontStyle.Normal, ColorFromHex("#8B949E", 1f), TextAnchor.MiddleLeft);
            headerText.text = title;
            var headerTextLe = headerText.gameObject.GetComponent<LayoutElement>();
            headerTextLe.flexibleWidth = 1f;

            var bodyGo = new GameObject("Body");
            bodyGo.transform.SetParent(sectionGo.transform, false);
            var bodyRt = bodyGo.AddComponent<RectTransform>();
            bodyRt.anchorMin = new Vector2(0f, 1f);
            bodyRt.anchorMax = new Vector2(1f, 1f);
            bodyRt.pivot = new Vector2(0.5f, 1f);
            bodyRt.offsetMin = Vector2.zero;
            bodyRt.offsetMax = Vector2.zero;

            var bodyLayout = bodyGo.AddComponent<VerticalLayoutGroup>();
            bodyLayout.childControlHeight = true;
            bodyLayout.childControlWidth = true;
            bodyLayout.childForceExpandHeight = false;
            bodyLayout.childForceExpandWidth = true;
            bodyLayout.padding = new RectOffset(8, 8, 7, 8);

            var bodyFitter = bodyGo.AddComponent<ContentSizeFitter>();
            bodyFitter.verticalFit = ContentSizeFitter.FitMode.PreferredSize;

            var bodyLe = bodyGo.AddComponent<LayoutElement>();
            bodyLe.minHeight = 58f;
            bodyLe.preferredHeight = 96f;
            bodyLe.flexibleWidth = 1f;

            var text = CreateText("Text", bodyGo.transform, DefaultBodyFont, FontStyle.Normal, Color.white, TextAnchor.UpperLeft);
            text.supportRichText = true;
            text.horizontalOverflow = HorizontalWrapMode.Wrap;
            text.verticalOverflow = VerticalWrapMode.Overflow;
            text.text = "<color=#6E7681>(in attesa dati)</color>";
            return text;
        }

        private static Text CreateText(string name, Transform parent, int fontSize, FontStyle style, Color color, TextAnchor alignment)
        {
            var go = new GameObject(name);
            go.transform.SetParent(parent, false);

            var text = go.AddComponent<Text>();
            text.raycastTarget = false;
            text.font = GetUiFont();
            text.fontSize = fontSize;
            text.fontStyle = style;
            text.alignment = alignment;
            text.horizontalOverflow = HorizontalWrapMode.Wrap;
            text.verticalOverflow = VerticalWrapMode.Truncate;
            text.supportRichText = true;
            text.color = color;

            var le = go.AddComponent<LayoutElement>();
            le.minHeight = fontSize + 6f;
            le.flexibleWidth = 1f;
            return text;
        }

        private static void SetTextIfReady(Text text, string value)
        {
            if (text == null)
                return;

            string boundedValue = BoundText(value);
            if (string.Equals(text.text, boundedValue, System.StringComparison.Ordinal))
                return;

            text.text = boundedValue;

            var layoutElement = text.GetComponent<LayoutElement>();
            if (layoutElement != null)
                layoutElement.preferredHeight = EstimatePreferredTextHeight(boundedValue, text.fontSize);
        }

        // =============================================================================
        // BoundText
        // =============================================================================
        /// <summary>
        /// <para>
        /// Applica un limite difensivo alle sezioni testuali del pannello. Lo scopo non
        /// e' nascondere dati diagnostici, ma impedire che una singola sezione produca
        /// una stringa abbastanza lunga da far crescere indefinitamente layout e mesh UI.
        /// </para>
        ///
        /// <para><b>Principio architetturale: debug bounded</b></para>
        /// <para>
        /// Il registry resta la fonte completa della finestra runtime recente, mentre
        /// la view sceglie quanto testo mostrare in un frame. La UI quindi rimane uno
        /// strumento di osservazione progressiva e non diventa una seconda memoria.
        /// </para>
        ///
        /// <para><b>Struttura interna:</b></para>
        /// <list type="bullet">
        ///   <item><b>Input null</b>: convertito a stringa vuota.</item>
        ///   <item><b>Limite caratteri</b>: tronca solo quando necessario.</item>
        ///   <item><b>Avviso finale</b>: segnala chiaramente che la UI ha ridotto l'output.</item>
        /// </list>
        /// </summary>
        private static string BoundText(string value)
        {
            if (string.IsNullOrEmpty(value))
                return string.Empty;

            if (value.Length <= MaxSectionTextChars)
                return value;

            return value.Substring(0, MaxSectionTextChars)
                + "\n<color=#D29922>Output troncato per stabilita UI.</color>";
        }

        // =============================================================================
        // EstimatePreferredTextHeight
        // =============================================================================
        /// <summary>
        /// <para>
        /// Stima un'altezza preferita stabile per i blocchi <c>Text</c> legacy. Unity
        /// puo' calcolare preferred size in modo costoso quando molti fitters annidati
        /// cambiano testo ogni frame; questa stima fornisce al layout un limite chiaro.
        /// </para>
        ///
        /// <para><b>Principio architetturale: layout UI deterministico</b></para>
        /// <para>
        /// Il pannello non deve dipendere da una crescita implicita infinita del testo.
        /// Ogni aggiornamento dichiara una dimensione ragionevole e capped, lasciando
        /// allo <c>ScrollRect</c> il compito di navigare il contenuto visibile.
        /// </para>
        ///
        /// <para><b>Struttura interna:</b></para>
        /// <list type="bullet">
        ///   <item><b>Righe esplicite</b>: conta i newline prodotti dai formatter.</item>
        ///   <item><b>Wrap stimato</b>: aggiunge righe virtuali per stringhe lunghe.</item>
        ///   <item><b>Clamp finale</b>: impedisce al singolo blocco di diventare enorme.</item>
        /// </list>
        /// </summary>
        private static float EstimatePreferredTextHeight(string value, int fontSize)
        {
            if (string.IsNullOrEmpty(value))
                return fontSize + 8f;

            int lines = 1;
            int currentLineChars = 0;

            for (int i = 0; i < value.Length; i++)
            {
                if (value[i] == '\n')
                {
                    lines++;
                    currentLineChars = 0;
                    continue;
                }

                currentLineChars++;
                if (currentLineChars >= EstimatedCharsPerLine)
                {
                    lines++;
                    currentLineChars = 0;
                }
            }

            float lineHeight = fontSize + 5f;
            return Mathf.Clamp((lines * lineHeight) + 8f, fontSize + 8f, MaxSectionTextHeight);
        }

        private static Color ColorFromHex(string hex, float alpha)
        {
            if (ColorUtility.TryParseHtmlString(hex, out var color))
            {
                color.a = alpha;
                return color;
            }

            return new Color(1f, 1f, 1f, alpha);
        }

        private static Font GetUiFont()
        {
            var font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            if (font != null)
                return font;

            return Resources.GetBuiltinResource<Font>("Arial.ttf");
        }
    }
}
