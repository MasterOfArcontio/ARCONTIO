namespace Arcontio.View.ArcGraph
{
    // =============================================================================
    // ArcUiActionDefinition
    // =============================================================================
    /// <summary>
    /// <para>
    /// Definizione minima di un pulsante della bottom action bar ArcGraph.
    /// </para>
    ///
    /// <para><b>Principio architetturale: pulsante azione come porta del pannello</b></para>
    /// <para>
    /// Il pulsante azione non esegue comandi e non modifica il mondo. Serve a
    /// indicare quale famiglia di strumenti deve aprire il pannello azione sopra la
    /// barra inferiore. Le operazioni concrete restano in
    /// <see cref="ArcUiOperationDefinition"/>.
    /// </para>
    ///
    /// <para><b>Struttura interna:</b></para>
    /// <list type="bullet">
    ///   <item><b>ActionKey</b>: chiave stabile del pulsante.</item>
    ///   <item><b>Label</b>: testo mostrabile nella UI.</item>
    ///   <item><b>IconKey</b>: chiave icona, non riferimento diretto a sprite.</item>
    ///   <item><b>OpensActionPanel</b>: indica se il pulsante apre il pannello operazioni.</item>
    ///   <item><b>DebugOnly</b>: limita il pulsante a strumenti di sviluppo.</item>
    /// </list>
    /// </summary>
    public readonly struct ArcUiActionDefinition
    {
        public readonly string ActionKey;
        public readonly string Label;
        public readonly string IconKey;
        public readonly bool OpensActionPanel;
        public readonly bool DebugOnly;

        public bool IsValid => !string.IsNullOrEmpty(ActionKey);

        // =============================================================================
        // ArcUiActionDefinition
        // =============================================================================
        /// <summary>
        /// <para>
        /// Costruisce una definizione di pulsante azione normalizzando le chiavi.
        /// </para>
        /// </summary>
        public ArcUiActionDefinition(
            string actionKey,
            string label,
            string iconKey,
            bool opensActionPanel,
            bool debugOnly)
        {
            ActionKey = ArcUiOperationDefinition.NormalizeKey(actionKey);
            Label = NormalizeText(label);
            IconKey = ArcUiOperationDefinition.NormalizeKey(iconKey);
            OpensActionPanel = opensActionPanel;
            DebugOnly = debugOnly;
        }

        // =============================================================================
        // NormalizeText
        // =============================================================================
        /// <summary>
        /// <para>
        /// Normalizza testo UI senza assegnare significato runtime.
        /// </para>
        /// </summary>
        private static string NormalizeText(string value)
        {
            return string.IsNullOrWhiteSpace(value) ? string.Empty : value.Trim();
        }
    }

    // =============================================================================
    // ArcUiActionGroupDefinition
    // =============================================================================
    /// <summary>
    /// <para>
    /// Definizione minima di un macrogruppo sinistro del pannello azione.
    /// </para>
    ///
    /// <para><b>Principio architetturale: navigazione UI separata dalla operation</b></para>
    /// <para>
    /// Il gruppo serve solo a filtrare visivamente le operation disponibili dentro
    /// una azione, ad esempio <c>Strutture</c> dentro <c>Costruisci</c>. Non decide
    /// permessi, non costruisce comandi e non legge cataloghi simulativi.
    /// </para>
    ///
    /// <para><b>Struttura interna:</b></para>
    /// <list type="bullet">
    ///   <item><b>ActionKey</b>: azione proprietaria del gruppo.</item>
    ///   <item><b>GroupKey</b>: chiave stabile del macrogruppo.</item>
    ///   <item><b>Label</b>: testo mostrabile nel pannello azione.</item>
    ///   <item><b>DebugOnly</b>: limita il gruppo a strumenti di sviluppo.</item>
    /// </list>
    /// </summary>
    public readonly struct ArcUiActionGroupDefinition
    {
        public readonly string ActionKey;
        public readonly string GroupKey;
        public readonly string Label;
        public readonly bool DebugOnly;

        public bool IsValid => !string.IsNullOrEmpty(ActionKey) && !string.IsNullOrEmpty(GroupKey);

        // =============================================================================
        // ArcUiActionGroupDefinition
        // =============================================================================
        /// <summary>
        /// <para>
        /// Costruisce un macrogruppo del pannello azione.
        /// </para>
        /// </summary>
        public ArcUiActionGroupDefinition(
            string actionKey,
            string groupKey,
            string label,
            bool debugOnly)
        {
            ActionKey = ArcUiOperationDefinition.NormalizeKey(actionKey);
            GroupKey = ArcUiOperationDefinition.NormalizeKey(groupKey);
            Label = string.IsNullOrWhiteSpace(label) ? string.Empty : label.Trim();
            DebugOnly = debugOnly;
        }
    }
}
