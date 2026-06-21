using System;

namespace Arcontio.View.ArcGraph
{
    // =============================================================================
    // ArcUiInspectorRow
    // =============================================================================
    /// <summary>
    /// <para>
    /// Riga read-only mostrabile da un inspector ArcGraph.
    /// </para>
    ///
    /// <para><b>Principio architetturale: ViewModel testuale, non accesso al World</b></para>
    /// <para>
    /// Una riga inspector contiene solo etichetta e valore gia' preparati. Il
    /// widget UI non deve risalire da questa riga a strutture runtime o a oggetti
    /// mutabili: deve limitarsi a visualizzare il dato.
    /// </para>
    ///
    /// <para><b>Struttura interna:</b></para>
    /// <list type="bullet">
    ///   <item><b>Label</b>: nome del campo.</item>
    ///   <item><b>Value</b>: valore gia' formattato per la UI.</item>
    /// </list>
    /// </summary>
    public readonly struct ArcUiInspectorRow
    {
        public readonly string Label;
        public readonly string Value;

        // =============================================================================
        // ArcUiInspectorRow
        // =============================================================================
        /// <summary>
        /// <para>
        /// Costruisce una riga inspector normalizzata.
        /// </para>
        /// </summary>
        public ArcUiInspectorRow(string label, string value)
        {
            Label = NormalizeText(label);
            Value = NormalizeText(value);
        }

        private static string NormalizeText(string value)
        {
            return string.IsNullOrWhiteSpace(value) ? string.Empty : value.Trim();
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
