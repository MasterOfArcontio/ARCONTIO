namespace Arcontio.View.ArcGraph
{
    // =============================================================================
    // ArcUiSelectionActionKind
    // =============================================================================
    /// <summary>
    /// <para>
    /// Tipo minimo di azione richiesta dalla UI su un target gia' selezionato.
    /// </para>
    ///
    /// <para><b>Principio architetturale: intenzione UI, non comando simulativo</b></para>
    /// <para>
    /// Questa enum descrive soltanto cosa l'utente ha chiesto alla UI. Non equivale
    /// a un comando Core, non cancella entita', non apre inspector e non modifica
    /// il <c>World</c>. Il controller autorizzato degli step successivi decidera'
    /// se e come trasformare l'intenzione in workflow di modifica o richiesta di
    /// eliminazione.
    /// </para>
    ///
    /// <para><b>Struttura interna:</b></para>
    /// <list type="bullet">
    ///   <item><b>None</b>: nessuna azione valida.</item>
    ///   <item><b>Edit</b>: richiesta di aprire un futuro flusso di modifica.</item>
    ///   <item><b>Delete</b>: richiesta di avviare un futuro flusso di eliminazione.</item>
    /// </list>
    /// </summary>
    public enum ArcUiSelectionActionKind
    {
        None = 0,
        Edit = 1,
        Delete = 2
    }

    // =============================================================================
    // ArcUiSelectionActionRequest
    // =============================================================================
    /// <summary>
    /// <para>
    /// Richiesta UI minimale generata dai pulsanti rapidi del menu selezione.
    /// </para>
    ///
    /// <para><b>Principio architetturale: richiesta asciutta e read-only</b></para>
    /// <para>
    /// La richiesta trasporta solo l'azione scelta, il target selezionato e la
    /// sorgente UI che l'ha generata. Non contiene riferimenti a MonoBehaviour,
    /// oggetti runtime mutabili, NPC, job, celle modificabili o servizi globali.
    /// Questo permette di verificare il click del menu hover senza introdurre una
    /// scorciatoia UI verso la simulazione.
    /// </para>
    ///
    /// <para><b>Struttura interna:</b></para>
    /// <list type="bullet">
    ///   <item><b>Kind</b>: Edit o Delete.</item>
    ///   <item><b>Target</b>: snapshot leggero del target selezionato.</item>
    ///   <item><b>Source</b>: nome del componente UI che ha prodotto la richiesta.</item>
    ///   <item><b>IsValid</b>: true solo se azione e target sono entrambi validi.</item>
    /// </list>
    /// </summary>
    public readonly struct ArcUiSelectionActionRequest
    {
        public readonly ArcUiSelectionActionKind Kind;
        public readonly ArcUiSelectionTarget Target;
        public readonly string Source;

        public bool IsValid => Kind != ArcUiSelectionActionKind.None && Target.IsValid;
        public bool IsEdit => Kind == ArcUiSelectionActionKind.Edit && Target.IsValid;
        public bool IsDelete => Kind == ArcUiSelectionActionKind.Delete && Target.IsValid;

        // =============================================================================
        // ArcUiSelectionActionRequest
        // =============================================================================
        /// <summary>
        /// <para>
        /// Costruisce una richiesta normalizzando la sorgente testuale.
        /// </para>
        /// </summary>
        public ArcUiSelectionActionRequest(
            ArcUiSelectionActionKind kind,
            ArcUiSelectionTarget target,
            string source)
        {
            Kind = target.IsValid ? kind : ArcUiSelectionActionKind.None;
            Target = target.IsValid ? target : ArcUiSelectionTarget.None("selection_action_request");
            Source = string.IsNullOrWhiteSpace(source) ? string.Empty : source.Trim();
        }

        // =============================================================================
        // Edit
        // =============================================================================
        /// <summary>
        /// <para>
        /// Crea una richiesta di modifica su un target selezionato.
        /// </para>
        /// </summary>
        public static ArcUiSelectionActionRequest Edit(
            ArcUiSelectionTarget target,
            string source)
        {
            return new ArcUiSelectionActionRequest(
                ArcUiSelectionActionKind.Edit,
                target,
                source);
        }

        // =============================================================================
        // Delete
        // =============================================================================
        /// <summary>
        /// <para>
        /// Crea una richiesta di eliminazione su un target selezionato.
        /// </para>
        /// </summary>
        public static ArcUiSelectionActionRequest Delete(
            ArcUiSelectionTarget target,
            string source)
        {
            return new ArcUiSelectionActionRequest(
                ArcUiSelectionActionKind.Delete,
                target,
                source);
        }
    }
}
