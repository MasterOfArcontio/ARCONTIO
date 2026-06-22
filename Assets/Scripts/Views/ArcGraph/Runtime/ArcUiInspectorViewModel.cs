using System;

namespace Arcontio.View.ArcGraph
{
    // =============================================================================
    // ArcUiInspectorRowKind
    // =============================================================================
    /// <summary>
    /// <para>
    /// Tipo visuale minimo di una riga del RightInspector ArcGraph.
    /// </para>
    ///
    /// <para><b>Principio architetturale: ViewModel descrive la forma, non la view</b></para>
    /// <para>
    /// Il provider runtime prepara gia' il tipo di riga da mostrare. La view UGUI
    /// non deve dedurre da una label se un dato sia una barra, una sezione o un
    /// elemento espandibile: deve soltanto renderizzare il contratto ricevuto.
    /// </para>
    ///
    /// <para><b>Struttura interna:</b></para>
    /// <list type="bullet">
    ///   <item><b>Text</b>: riga key/value semplice.</item>
    ///   <item><b>Section</b>: separatore visuale di sezione.</item>
    ///   <item><b>Bar</b>: valore numerico visualizzato come barra.</item>
    ///   <item><b>IconMetrics</b>: gruppo compatto icona + numero.</item>
    ///   <item><b>Expandable</b>: riga compatta con dettagli apribili.</item>
    ///   <item><b>Timeline</b>: evento temporale compatto.</item>
    /// </list>
    /// </summary>
    public enum ArcUiInspectorRowKind
    {
        Text = 0,
        Section = 1,
        Bar = 2,
        IconMetrics = 3,
        Expandable = 4,
        Timeline = 5
    }

    // =============================================================================
    // ArcUiInspectorSeverity
    // =============================================================================
    /// <summary>
    /// <para>
    /// Ruolo cromatico semantico per righe e badge inspector.
    /// </para>
    /// </summary>
    public enum ArcUiInspectorSeverity
    {
        Normal = 0,
        Muted = 1,
        Good = 2,
        Warning = 3,
        Danger = 4,
        Info = 5
    }

    // =============================================================================
    // ArcUiInspectorMetric
    // =============================================================================
    /// <summary>
    /// <para>
    /// Metrica compatta visualizzabile come icona + valore numerico.
    /// </para>
    ///
    /// <para><b>Contratto asciutto per risorse</b></para>
    /// <para>
    /// Il campo <c>IconKey</c> non contiene uno sprite e non carica asset. E' una
    /// chiave testuale che la view puo' mostrare come placeholder e che in futuro
    /// potra' essere risolta da un catalogo icone.
    /// </para>
    /// </summary>
    public readonly struct ArcUiInspectorMetric
    {
        public readonly string IconKey;
        public readonly string Label;
        public readonly string Value;
        public readonly ArcUiInspectorSeverity Severity;

        public ArcUiInspectorMetric(
            string iconKey,
            string label,
            string value,
            ArcUiInspectorSeverity severity = ArcUiInspectorSeverity.Normal)
        {
            IconKey = NormalizeText(iconKey);
            Label = NormalizeText(label);
            Value = NormalizeText(value);
            Severity = severity;
        }

        private static string NormalizeText(string value)
        {
            return string.IsNullOrWhiteSpace(value) ? string.Empty : value.Trim();
        }
    }

    // =============================================================================
    // ArcUiInspectorRow
    // =============================================================================
    /// <summary>
    /// <para>
    /// Riga read-only mostrabile da un inspector ArcGraph.
    /// </para>
    ///
    /// <para><b>Principio architetturale: ViewModel visuale, non accesso al World</b></para>
    /// <para>
    /// Una riga inspector contiene solo dati gia' preparati. Anche quando descrive
    /// una barra, una lista espandibile o metriche con icona, non contiene servizi,
    /// riferimenti Core o oggetti Unity. Il widget UI deve limitarsi a visualizzare
    /// il dato e non puo' risalire da qui al runtime mutabile.
    /// </para>
    ///
    /// <para><b>Struttura interna:</b></para>
    /// <list type="bullet">
    ///   <item><b>Kind</b>: forma visuale richiesta.</item>
    ///   <item><b>RowKey</b>: chiave stabile per stato locale della view, come espansione/collapse.</item>
    ///   <item><b>Label/Value</b>: testo principale gia' formattato.</item>
    ///   <item><b>NumericValue01</b>: valore normalizzato per barre.</item>
    ///   <item><b>AlertMarker01/CriticalMarker01</b>: marker opzionali per barre con soglie.</item>
    ///   <item><b>Metrics</b>: gruppo compatto icona + numero.</item>
    ///   <item><b>Details</b>: righe figlie mostrate quando una riga espandibile e' aperta.</item>
    /// </list>
    /// </summary>
    public readonly struct ArcUiInspectorRow
    {
        public readonly ArcUiInspectorRowKind Kind;
        public readonly string RowKey;
        public readonly string Label;
        public readonly string Value;
        public readonly string SecondaryValue;
        public readonly float NumericValue01;
        public readonly float AlertMarker01;
        public readonly float CriticalMarker01;
        public readonly bool IsSelected;
        public readonly ArcUiInspectorSeverity Severity;
        public readonly ArcUiInspectorMetric[] Metrics;
        public readonly ArcUiInspectorRow[] Details;

        // =============================================================================
        // ArcUiInspectorRow
        // =============================================================================
        /// <summary>
        /// <para>
        /// Costruisce una riga inspector normalizzata.
        /// </para>
        /// </summary>
        public ArcUiInspectorRow(string label, string value)
            : this(
                ArcUiInspectorRowKind.Text,
                NormalizeKey(label),
                label,
                value,
                string.Empty,
                0f,
                -1f,
                -1f,
                false,
                ArcUiInspectorSeverity.Normal,
                null,
                null)
        {
        }

        private ArcUiInspectorRow(
            ArcUiInspectorRowKind kind,
            string rowKey,
            string label,
            string value,
            string secondaryValue,
            float numericValue01,
            float alertMarker01,
            float criticalMarker01,
            bool isSelected,
            ArcUiInspectorSeverity severity,
            ArcUiInspectorMetric[] metrics,
            ArcUiInspectorRow[] details)
        {
            Kind = kind;
            RowKey = NormalizeKey(rowKey);
            Label = NormalizeText(label);
            Value = NormalizeText(value);
            SecondaryValue = NormalizeText(secondaryValue);
            NumericValue01 = Clamp01(numericValue01);
            AlertMarker01 = alertMarker01 < 0f ? -1f : Clamp01(alertMarker01);
            CriticalMarker01 = criticalMarker01 < 0f ? -1f : Clamp01(criticalMarker01);
            IsSelected = isSelected;
            Severity = severity;
            Metrics = metrics == null ? Array.Empty<ArcUiInspectorMetric>() : metrics;
            Details = details == null ? Array.Empty<ArcUiInspectorRow>() : details;
        }

        public static ArcUiInspectorRow Section(string label)
        {
            return new ArcUiInspectorRow(
                ArcUiInspectorRowKind.Section,
                "section_" + NormalizeKey(label),
                label,
                string.Empty,
                string.Empty,
                0f,
                -1f,
                -1f,
                false,
                ArcUiInspectorSeverity.Muted,
                null,
                null);
        }

        public static ArcUiInspectorRow Bar(
            string rowKey,
            string label,
            string value,
            float fill01,
            ArcUiInspectorSeverity severity = ArcUiInspectorSeverity.Normal,
            float alertMarker01 = -1f,
            float criticalMarker01 = -1f)
        {
            return new ArcUiInspectorRow(
                ArcUiInspectorRowKind.Bar,
                rowKey,
                label,
                value,
                string.Empty,
                fill01,
                alertMarker01,
                criticalMarker01,
                false,
                severity,
                null,
                null);
        }

        public static ArcUiInspectorRow IconMetrics(
            string rowKey,
            string label,
            ArcUiInspectorMetric[] metrics)
        {
            return new ArcUiInspectorRow(
                ArcUiInspectorRowKind.IconMetrics,
                rowKey,
                label,
                string.Empty,
                string.Empty,
                0f,
                -1f,
                -1f,
                false,
                ArcUiInspectorSeverity.Normal,
                metrics,
                null);
        }

        public static ArcUiInspectorRow Expandable(
            string rowKey,
            string label,
            string value,
            string secondaryValue,
            ArcUiInspectorSeverity severity,
            bool isSelected,
            ArcUiInspectorRow[] details)
        {
            return new ArcUiInspectorRow(
                ArcUiInspectorRowKind.Expandable,
                rowKey,
                label,
                value,
                secondaryValue,
                0f,
                -1f,
                -1f,
                isSelected,
                severity,
                null,
                details);
        }

        public static ArcUiInspectorRow Timeline(
            string rowKey,
            string label,
            string value,
            ArcUiInspectorSeverity severity = ArcUiInspectorSeverity.Muted)
        {
            return new ArcUiInspectorRow(
                ArcUiInspectorRowKind.Timeline,
                rowKey,
                label,
                value,
                string.Empty,
                0f,
                -1f,
                -1f,
                false,
                severity,
                null,
                null);
        }

        private static string NormalizeText(string value)
        {
            return string.IsNullOrWhiteSpace(value) ? string.Empty : value.Trim();
        }

        private static string NormalizeKey(string value)
        {
            string normalized = ArcUiOperationDefinition.NormalizeKey(value);
            return string.IsNullOrEmpty(normalized) ? "row" : normalized;
        }

        private static float Clamp01(float value)
        {
            if (value < 0f)
                return 0f;

            return value > 1f ? 1f : value;
        }
    }

    // =============================================================================
    // ArcUiInspectorTab
    // =============================================================================
    /// <summary>
    /// <para>
    /// Tab read-only dell'inspector ArcGraph.
    /// </para>
    ///
    /// <para><b>Principio architetturale: contenuto gia' preparato</b></para>
    /// <para>
    /// La tab non costruisce dati da sola. Riceve righe gia' pronte da una factory
    /// futura e le espone alla UI. In questo modo il pannello resta un renderer UGUI
    /// e non diventa un nuovo DevTools.
    /// </para>
    ///
    /// <para><b>Struttura interna:</b></para>
    /// <list type="bullet">
    ///   <item><b>TabKey</b>: chiave stabile della tab.</item>
    ///   <item><b>Label</b>: testo visibile nel tab button.</item>
    ///   <item><b>Rows</b>: righe read-only da disegnare.</item>
    /// </list>
    /// </summary>
    public readonly struct ArcUiInspectorTab
    {
        public readonly string TabKey;
        public readonly string Label;
        public readonly ArcUiInspectorRow[] Rows;

        public bool IsValid => !string.IsNullOrEmpty(TabKey);

        // =============================================================================
        // ArcUiInspectorTab
        // =============================================================================
        /// <summary>
        /// <para>
        /// Costruisce una tab inspector con righe copiate in un array non nullo.
        /// </para>
        /// </summary>
        public ArcUiInspectorTab(string tabKey, string label, ArcUiInspectorRow[] rows)
        {
            TabKey = ArcUiOperationDefinition.NormalizeKey(tabKey);
            Label = string.IsNullOrWhiteSpace(label) ? string.Empty : label.Trim();
            Rows = rows == null ? Array.Empty<ArcUiInspectorRow>() : rows;
        }
    }

    // =============================================================================
    // ArcUiInspectorViewModel
    // =============================================================================
    /// <summary>
    /// <para>
    /// ViewModel minimo per il pannello inspector ArcGraph.
    /// </para>
    ///
    /// <para><b>Principio architetturale: UI visualizza snapshot, non runtime</b></para>
    /// <para>
    /// Il ViewModel contiene titolo, target e tab gia' preparate. Non contiene
    /// factory, resolver, servizi, riferimenti Unity o strutture Core. Questo lo
    /// rende adatto a essere prodotto da un boundary autorizzato e consumato da un
    /// pannello UGUI senza coupling diretto con la simulazione.
    /// </para>
    ///
    /// <para><b>Struttura interna:</b></para>
    /// <list type="bullet">
    ///   <item><b>Title</b>: titolo inspector.</item>
    ///   <item><b>Target</b>: target selezionato.</item>
    ///   <item><b>Tabs</b>: contenuti read-only disponibili.</item>
    ///   <item><b>ActiveTabKey</b>: tab da mostrare per prima.</item>
    /// </list>
    /// </summary>
    public readonly struct ArcUiInspectorViewModel
    {
        public readonly string Title;
        public readonly ArcUiSelectionTarget Target;
        public readonly ArcUiInspectorTab[] Tabs;
        public readonly string ActiveTabKey;

        public bool HasTarget => Target.IsValid;
        public bool HasTabs => Tabs.Length > 0;

        // =============================================================================
        // ArcUiInspectorViewModel
        // =============================================================================
        /// <summary>
        /// <para>
        /// Costruisce un ViewModel inspector minimale.
        /// </para>
        ///
        /// <para><b>Contratto anti-monolite</b></para>
        /// <para>
        /// Questo costruttore non accetta dizionari arbitrari o payload generici.
        /// Le informazioni devono essere gia' trasformate in tab e righe, cosi' il
        /// pannello non puo' diventare un lettore casuale del runtime.
        /// </para>
        /// </summary>
        public ArcUiInspectorViewModel(
            string title,
            ArcUiSelectionTarget target,
            ArcUiInspectorTab[] tabs,
            string activeTabKey)
        {
            Title = string.IsNullOrWhiteSpace(title) ? string.Empty : title.Trim();
            Target = target;
            Tabs = tabs == null ? Array.Empty<ArcUiInspectorTab>() : tabs;
            ActiveTabKey = ArcUiOperationDefinition.NormalizeKey(activeTabKey);
        }

        // =============================================================================
        // Empty
        // =============================================================================
        /// <summary>
        /// <para>
        /// Crea un inspector vuoto per lo stato senza selezione.
        /// </para>
        /// </summary>
        public static ArcUiInspectorViewModel Empty()
        {
            return new ArcUiInspectorViewModel(
                string.Empty,
                ArcUiSelectionTarget.None("none"),
                Array.Empty<ArcUiInspectorTab>(),
                string.Empty);
        }
    }
}
