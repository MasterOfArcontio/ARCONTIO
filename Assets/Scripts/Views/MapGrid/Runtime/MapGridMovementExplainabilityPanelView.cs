using UnityEngine;
using UnityEngine.UI;

namespace Arcontio.View.MapGrid
{
    // =============================================================================
    // MapGridMovementExplainabilityPanelView
    // =============================================================================
    /// <summary>
    /// <para>
    /// Pannello debug prefabless per mostrare l'Explainability Layer del pathfinding
    /// sul lato destro dello schermo della MapGrid. La classe costruisce soltanto UI
    /// uGUI e riceve testo gia' formattato dal controller overlay.
    /// </para>
    ///
    /// <para><b>Coerenza con le card NPC esistenti</b></para>
    /// <para>
    /// Il progetto usa gia' card runtime create da codice per la diagnostica. Questo
    /// pannello segue la stessa metodologia: niente prefab, niente asset grafici nuovi,
    /// niente letture dirette dal core simulativo. Il controller decide cosa mostrare,
    /// questa view decide solo dove e come renderizzarlo.
    /// </para>
    ///
    /// <para><b>Struttura interna:</b></para>
    /// <list type="bullet">
    ///   <item><b>_root</b>: GameObject principale del pannello.</item>
    ///   <item><b>_rootRt</b>: RectTransform ancorato al bordo destro del canvas.</item>
    ///   <item><b>_rootBg</b>: sfondo scuro raycast target per proteggere la mappa dietro.</item>
    ///   <item><b>_titleText</b>: intestazione sintetica del pannello.</item>
    ///   <item><b>_headerMetaText</b>: riga compatta con tick, intent e plan.</item>
    ///   <item><b>_intentPlanText</b>: pannellino separato con il dettaglio intent/plan.</item>
    ///   <item><b>_eventsText</b>: area eventi espandibile fino al fondo del pannello.</item>
    /// </list>
    /// </summary>
    public sealed class MapGridMovementExplainabilityPanelView
    {
        private const int DefaultWidth = 430;
        private const int HorizontalMargin = 12;
        private const int VerticalMargin = 10;
        private const int DefaultTitleFont = 14;
        private const int DefaultBodyFont = 12;

        private GameObject _root;
        private RectTransform _rootRt;
        private Image _rootBg;
        private Text _titleText;
        private Text _headerMetaText;
        private Text _intentPlanText;
        private Text _eventsText;

        public RectTransform RootRectTransform => _rootRt;

        // =============================================================================
        // AttachTo
        // =============================================================================
        /// <summary>
        /// <para>
        /// Crea il pannello sotto il parent indicato e lo ancora al lato destro del
        /// canvas occupando tutta l'altezza disponibile. Il pannello parte nascosto e
        /// viene aggiornato dal SummaryOverlay.
        /// </para>
        ///
        /// <para><b>UI debug stabile</b></para>
        /// <para>
        /// La posizione non segue l'NPC e non dipende dalla camera: resta fissa a destra
        /// per diventare un riferimento stabile durante il debug del movimento.
        /// </para>
        ///
        /// <para><b>Struttura interna:</b></para>
        /// <list type="bullet">
        ///   <item><b>parent</b>: canvas/root dell'overlay MapGrid.</item>
        ///   <item><b>layout verticale</b>: header fisso e body testuale in colonna.</item>
        ///   <item><b>raycast target</b>: evita click accidentali sulla mappa sotto il pannello.</item>
        /// </list>
        /// </summary>
        public void AttachTo(Transform parent)
        {
            if (_root != null)
                return;

            _root = new GameObject("MovementExplainabilityRightPanel");
            _root.transform.SetParent(parent, false);

            _rootRt = _root.AddComponent<RectTransform>();
            _rootRt.anchorMin = new Vector2(1f, 0f);
            _rootRt.anchorMax = new Vector2(1f, 1f);
            _rootRt.pivot = new Vector2(1f, 0.5f);
            _rootRt.offsetMin = new Vector2(-DefaultWidth - HorizontalMargin, VerticalMargin);
            _rootRt.offsetMax = new Vector2(-HorizontalMargin, -VerticalMargin);

            _rootBg = _root.AddComponent<Image>();
            _rootBg.raycastTarget = true;
            _rootBg.color = new Color(0.02f, 0.03f, 0.03f, 0.86f);

            var layout = _root.AddComponent<VerticalLayoutGroup>();
            layout.childControlHeight = true;
            layout.childControlWidth = true;
            layout.childForceExpandHeight = false;
            layout.childForceExpandWidth = true;
            layout.spacing = 6;
            layout.padding = new RectOffset(8, 8, 8, 8);

            BuildHeader(_root.transform);
            BuildIntentPlanPanel(_root.transform);
            BuildEventsPanel(_root.transform);
            _root.transform.SetAsLastSibling();

            SetVisible(false);
        }

        // =============================================================================
        // SetVisible
        // =============================================================================
        /// <summary>
        /// <para>
        /// Mostra o nasconde il pannello senza distruggerlo. Il controller puo' quindi
        /// aggiornarlo ogni frame senza ricreare gerarchie UI.
        /// </para>
        /// </summary>
        public void SetVisible(bool visible)
        {
            if (_root != null)
                _root.SetActive(visible);
        }

        // =============================================================================
        // SetText
        // =============================================================================
        /// <summary>
        /// <para>
        /// Aggiorna i tre blocchi testuali del pannello. La view non formatta il
        /// ViewModel EL: riceve testo gia' pronto dal controller overlay.
        /// </para>
        ///
        /// <para><b>Separazione minima per debug UI</b></para>
        /// <para>
        /// Anche se siamo in una UI diagnostica, manteniamo una divisione semplice:
        /// il controller legge dati e prepara stringhe, questa classe mostra stringhe.
        /// </para>
        /// </summary>
        public void SetText(string title, string headerMeta, string intentPlan, string events)
        {
            if (_titleText != null)
                _titleText.text = title ?? string.Empty;

            if (_headerMetaText != null)
                _headerMetaText.text = headerMeta ?? string.Empty;

            if (_intentPlanText != null)
                _intentPlanText.text = intentPlan ?? string.Empty;

            if (_eventsText != null)
                _eventsText.text = events ?? string.Empty;
        }

        // =============================================================================
        // BuildHeader
        // =============================================================================
        /// <summary>
        /// <para>
        /// Costruisce la fascia superiore del pannello con il titolo della diagnostica.
        /// </para>
        /// </summary>
        private void BuildHeader(Transform parent)
        {
            var headerGo = new GameObject("Header");
            headerGo.transform.SetParent(parent, false);

            var headerImage = headerGo.AddComponent<Image>();
            headerImage.raycastTarget = false;
            headerImage.color = new Color(0.10f, 0.16f, 0.13f, 0.94f);

            var headerLayout = headerGo.AddComponent<VerticalLayoutGroup>();
            headerLayout.childControlHeight = true;
            headerLayout.childControlWidth = true;
            headerLayout.childForceExpandHeight = false;
            headerLayout.childForceExpandWidth = true;
            headerLayout.spacing = 1;
            headerLayout.padding = new RectOffset(6, 6, 3, 3);

            var headerLe = headerGo.AddComponent<LayoutElement>();
            headerLe.minHeight = 26f;
            headerLe.preferredHeight = 30f;
            headerLe.flexibleHeight = 0f;

            var titleGo = new GameObject("Title");
            titleGo.transform.SetParent(headerGo.transform, false);

            _titleText = titleGo.AddComponent<Text>();
            _titleText.raycastTarget = false;
            _titleText.font = GetUiFont();
            _titleText.fontSize = DefaultTitleFont;
            _titleText.fontStyle = FontStyle.Bold;
            _titleText.alignment = TextAnchor.MiddleLeft;
            _titleText.horizontalOverflow = HorizontalWrapMode.Wrap;
            _titleText.verticalOverflow = VerticalWrapMode.Truncate;
            _titleText.color = new Color(0.92f, 1.00f, 0.94f, 1f);
            _titleText.text = "EL Pathfinding";

            var metaGo = new GameObject("Meta");
            metaGo.transform.SetParent(headerGo.transform, false);

            _headerMetaText = metaGo.AddComponent<Text>();
            _headerMetaText.raycastTarget = false;
            _headerMetaText.font = GetUiFont();
            _headerMetaText.fontSize = 10;
            _headerMetaText.fontStyle = FontStyle.Normal;
            _headerMetaText.alignment = TextAnchor.MiddleLeft;
            _headerMetaText.horizontalOverflow = HorizontalWrapMode.Wrap;
            _headerMetaText.verticalOverflow = VerticalWrapMode.Truncate;
            _headerMetaText.color = new Color(0.82f, 0.90f, 0.84f, 1f);
            _headerMetaText.text = string.Empty;
        }

        // =============================================================================
        // BuildIntentPlanPanel
        // =============================================================================
        /// <summary>
        /// <para>
        /// Costruisce il pannellino separato con il dettaglio intent/plan. Resta
        /// compatto e non espande verticalmente, per lasciare agli eventi tutto lo
        /// spazio rimanente del pannello laterale.
        /// </para>
        /// </summary>
        private void BuildIntentPlanPanel(Transform parent)
        {
            var panelGo = new GameObject("IntentPlan");
            panelGo.transform.SetParent(parent, false);

            var panelImage = panelGo.AddComponent<Image>();
            panelImage.raycastTarget = false;
            panelImage.color = new Color(0.10f, 0.16f, 0.13f, 0.90f);

            var panelLayout = panelGo.AddComponent<VerticalLayoutGroup>();
            panelLayout.childControlHeight = true;
            panelLayout.childControlWidth = true;
            panelLayout.childForceExpandHeight = false;
            panelLayout.childForceExpandWidth = true;
            panelLayout.padding = new RectOffset(6, 6, 5, 5);

            var panelLe = panelGo.AddComponent<LayoutElement>();
            panelLe.minHeight = 108f;
            panelLe.preferredHeight = 138f;
            panelLe.flexibleHeight = 0f;

            var textGo = new GameObject("Text");
            textGo.transform.SetParent(panelGo.transform, false);

            _intentPlanText = textGo.AddComponent<Text>();
            _intentPlanText.raycastTarget = false;
            _intentPlanText.font = GetUiFont();
            _intentPlanText.fontSize = DefaultBodyFont;
            _intentPlanText.fontStyle = FontStyle.Normal;
            _intentPlanText.alignment = TextAnchor.UpperLeft;
            _intentPlanText.horizontalOverflow = HorizontalWrapMode.Wrap;
            _intentPlanText.verticalOverflow = VerticalWrapMode.Truncate;
            _intentPlanText.supportRichText = true;
            _intentPlanText.color = Color.white;
            _intentPlanText.text = string.Empty;
        }

        // =============================================================================
        // BuildEventsPanel
        // =============================================================================
        /// <summary>
        /// <para>
        /// Costruisce il pannello eventi. Questo e' l'unico blocco con altezza flessibile:
        /// deve occupare tutta la parte libera sotto header e intent/plan, cosi' la
        /// timeline arriva fino al fondo del pannello destro.
        /// </para>
        /// </summary>
        private void BuildEventsPanel(Transform parent)
        {
            var panelGo = new GameObject("Events");
            panelGo.transform.SetParent(parent, false);

            var panelImage = panelGo.AddComponent<Image>();
            panelImage.raycastTarget = false;
            panelImage.color = new Color(0.06f, 0.08f, 0.08f, 0.82f);

            var panelLayout = panelGo.AddComponent<VerticalLayoutGroup>();
            panelLayout.childControlHeight = true;
            panelLayout.childControlWidth = true;
            panelLayout.childForceExpandHeight = true;
            panelLayout.childForceExpandWidth = true;
            panelLayout.padding = new RectOffset(6, 6, 6, 6);

            var panelLe = panelGo.AddComponent<LayoutElement>();
            panelLe.minHeight = 120f;
            panelLe.flexibleHeight = 1f;

            var textGo = new GameObject("Text");
            textGo.transform.SetParent(panelGo.transform, false);

            _eventsText = textGo.AddComponent<Text>();
            _eventsText.raycastTarget = false;
            _eventsText.font = GetUiFont();
            _eventsText.fontSize = DefaultBodyFont;
            _eventsText.fontStyle = FontStyle.Normal;
            _eventsText.alignment = TextAnchor.UpperLeft;
            _eventsText.horizontalOverflow = HorizontalWrapMode.Wrap;
            _eventsText.verticalOverflow = VerticalWrapMode.Overflow;
            _eventsText.supportRichText = true;
            _eventsText.color = Color.white;
            _eventsText.text = string.Empty;

            var textLe = textGo.AddComponent<LayoutElement>();
            textLe.flexibleHeight = 1f;
        }

        // =============================================================================
        // GetUiFont
        // =============================================================================
        /// <summary>
        /// <para>
        /// Recupera il font runtime usato anche dalle card debug esistenti.
        /// </para>
        /// </summary>
        private static Font GetUiFont()
        {
            var font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            if (font != null)
                return font;

            return Resources.GetBuiltinResource<Font>("Arial.ttf");
        }
    }
}
