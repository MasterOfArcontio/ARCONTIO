namespace Arcontio.View.ArcGraph
{
    // =============================================================================
    // ArcUiEditSelectionKind
    // =============================================================================
    /// <summary>
    /// <para>
    /// Tipo essenziale di modifica preparata per il target selezionato.
    /// </para>
    ///
    /// <para><b>Principio architetturale: modifica come intenzione asciutta</b></para>
    /// <para>
    /// Questa enum non anticipa ogni possibile editor futuro. Distingue solo le
    /// famiglie oggi necessarie: NPC, oggetto e muro. I dettagli concreti, come DNA,
    /// food stock, stato porta o variante materiale, restano nei ViewModel e nei
    /// futuri controller autorizzati.
    /// </para>
    ///
    /// <para><b>Struttura interna:</b></para>
    /// <list type="bullet">
    ///   <item><b>None</b>: nessuna modifica valida.</item>
    ///   <item><b>Npc</b>: modifica configurabile di un NPC selezionato.</item>
    ///   <item><b>Object</b>: modifica configurabile di un oggetto selezionato.</item>
    ///   <item><b>Wall</b>: modifica configurabile di un muro selezionato.</item>
    /// </list>
    /// </summary>
    public enum ArcUiEditSelectionKind
    {
        None = 0,
        Npc = 1,
        Object = 2,
        Wall = 3
    }

    // =============================================================================
    // ArcUiEditSelectionRequest
    // =============================================================================
    /// <summary>
    /// <para>
    /// Richiesta draft prodotta quando l'utente apre la modifica di un target gia'
    /// selezionato.
    /// </para>
    ///
    /// <para><b>Principio architetturale: edit draft prima del comando</b></para>
    /// <para>
    /// La request conserva solo target, tipo di modifica e sorgente UI. Non contiene
    /// valori mutabili del <c>World</c>, non scrive DNA, non cambia food stock, non
    /// apre porte e non sostituisce muri. Serve a rendere esplicito il ponte tra
    /// pulsante Modifica, RightInspector e futuri command request.
    /// </para>
    ///
    /// <para><b>Struttura interna:</b></para>
    /// <list type="bullet">
    ///   <item><b>Kind</b>: famiglia edit minima.</item>
    ///   <item><b>Target</b>: snapshot leggero selezionato.</item>
    ///   <item><b>Source</b>: componente UI che ha generato la richiesta.</item>
    ///   <item><b>IsValid</b>: true solo per target e kind supportati.</item>
    /// </list>
    /// </summary>
    public readonly struct ArcUiEditSelectionRequest
    {
        public readonly ArcUiEditSelectionKind Kind;
        public readonly ArcUiSelectionTarget Target;
        public readonly string Source;

        public bool IsValid => Kind != ArcUiEditSelectionKind.None && Target.IsValid;

        public ArcUiEditSelectionRequest(
            ArcUiEditSelectionKind kind,
            ArcUiSelectionTarget target,
            string source)
        {
            Kind = target.IsValid ? kind : ArcUiEditSelectionKind.None;
            Target = target.IsValid ? target : ArcUiSelectionTarget.None("edit_selection_request");
            Source = string.IsNullOrWhiteSpace(source) ? string.Empty : source.Trim();
        }

        // =============================================================================
        // FromSelectionAction
        // =============================================================================
        /// <summary>
        /// <para>
        /// Converte una richiesta Edit generica in una draft request specifica.
        /// </para>
        /// </summary>
        public static ArcUiEditSelectionRequest FromSelectionAction(ArcUiSelectionActionRequest actionRequest)
        {
            if (!actionRequest.IsEdit)
                return default;

            ArcUiEditSelectionKind kind = actionRequest.Target.Kind switch
            {
                ArcUiSelectionTargetKind.Npc => ArcUiEditSelectionKind.Npc,
                ArcUiSelectionTargetKind.Object => ArcUiEditSelectionKind.Object,
                ArcUiSelectionTargetKind.Wall => ArcUiEditSelectionKind.Wall,
                _ => ArcUiEditSelectionKind.None
            };

            return new ArcUiEditSelectionRequest(
                kind,
                actionRequest.Target,
                actionRequest.Source);
        }
    }
}
