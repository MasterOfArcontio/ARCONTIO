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
    ///   <item><b>_bodyText</b>: testo rich text con intent, piano ed eventi EL.</item>
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
        private Text _bodyText;

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
            layout.childForceExpandHeight = true;
            layout.childForceExpandWidth = true;
            layout.spacing = 6;
            layout.padding = new RectOffset(8, 8, 8, 8);

            BuildHeader(_root.transform);
            BuildBody(_root.transform);
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
        /// Aggiorna titolo e corpo del pannello. La view non formatta il ViewModel EL:
        /// riceve testo gia' pronto dal controller overlay.
        /// </para>
        ///
        /// <para><b>Separazione minima per debug UI</b></para>
        /// <para>
        /// Anche se siamo in una UI diagnostica, manteniamo una divisione semplice:
        /// il controller legge dati e prepara stringhe, questa classe mostra stringhe.
        /// </para>
        /// </summary>
        public void SetText(string title, string body)
        {
            if (_titleText != null)
                _titleText.text = title ?? string.Empty;

            if (_bodyText != null)
                _bodyText.text = body ?? string.Empty;
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
            headerLayout.padding = new RectOffset(6, 6, 5, 5);

            headerGo.AddComponent<LayoutElement>().preferredHeight = 34f;

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
        }

        // =============================================================================
        // BuildBody
        // =============================================================================
        /// <summary>
        /// <para>
        /// Costruisce il corpo testuale del pannello con la stessa tecnica semplice
        /// delle card debug esistenti. In questa fase preferiamo una pipeline visiva
        /// diretta e verificabile rispetto a uno ScrollRect piu' complesso: il pannello
        /// deve prima garantire che il testo ricevuto dal controller venga mostrato.
        /// </para>
        /// </summary>
        private void BuildBody(Transform parent)
        {
            var bodyGo = new GameObject("Body");
            bodyGo.transform.SetParent(parent, false);

            var bodyImage = bodyGo.AddComponent<Image>();
            bodyImage.raycastTarget = false;
            bodyImage.color = new Color(0.06f, 0.08f, 0.08f, 0.82f);

            var bodyLayout = bodyGo.AddComponent<VerticalLayoutGroup>();
            bodyLayout.childControlHeight = true;
            bodyLayout.childControlWidth = true;
            bodyLayout.childForceExpandHeight = true;
            bodyLayout.childForceExpandWidth = true;
            bodyLayout.padding = new RectOffset(6, 6, 6, 6);

            var bodyLe = bodyGo.AddComponent<LayoutElement>();
            bodyLe.flexibleHeight = 1f;

            var textGo = new GameObject("Text");
            textGo.transform.SetParent(bodyGo.transform, false);

            _bodyText = textGo.AddComponent<Text>();
            _bodyText.raycastTarget = false;
            _bodyText.font = GetUiFont();
            _bodyText.fontSize = DefaultBodyFont;
            _bodyText.fontStyle = FontStyle.Normal;
            _bodyText.alignment = TextAnchor.UpperLeft;
            _bodyText.horizontalOverflow = HorizontalWrapMode.Wrap;
            _bodyText.verticalOverflow = VerticalWrapMode.Overflow;
            _bodyText.supportRichText = true;
            _bodyText.color = Color.white;
            _bodyText.text = string.Empty;

            var textLe = textGo.AddComponent<LayoutElement>();
            textLe.minHeight = 120f;
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
