namespace Arcontio.View.ArcGraph
{
    // =============================================================================
    // ArcUiViewOverlayKind
    // =============================================================================
    /// <summary>
    /// <para>
    /// Famiglia minima di overlay visuale attivabile da una view mode ArcGraph.
    /// </para>
    ///
    /// <para><b>Principio architetturale: osservazione separata dalle azioni</b></para>
    /// <para>
    /// Un overlay cambia il modo in cui osserviamo la simulazione, non il mondo.
    /// Questa enum resta volutamente piccola: nuove famiglie verranno aggiunte solo
    /// quando un overlay reale le richiedera'.
    /// </para>
    ///
    /// <para><b>Struttura interna:</b></para>
    /// <list type="bullet">
    ///   <item><b>None</b>: nessun overlay dedicato.</item>
    ///   <item><b>Fov</b>: campo visivo/percezione.</item>
    ///   <item><b>Path</b>: pathfinding o percorso.</item>
    ///   <item><b>Occupancy</b>: occupazione celle.</item>
    ///   <item><b>NpcMemory</b>: memoria/belief legata a NPC selezionato.</item>
    /// </list>
    /// </summary>
    public enum ArcUiViewOverlayKind
    {
        None = 0,
        Fov = 1,
        Path = 2,
        Occupancy = 3,
        NpcMemory = 4
    }

    // =============================================================================
    // ArcUiViewModeDefinition
    // =============================================================================
    /// <summary>
    /// <para>
    /// Definizione minima di una modalita' di osservazione ArcGraph.
    /// </para>
    ///
    /// <para><b>Principio architetturale: view mode non operativa</b></para>
    /// <para>
    /// Una view mode non costruisce comandi e non muta la simulazione. Serve a
    /// selezionare quale overlay o prospettiva visuale mostrare sopra snapshot gia'
    /// autorizzati.
    /// </para>
    ///
    /// <para><b>Struttura interna:</b></para>
    /// <list type="bullet">
    ///   <item><b>ViewModeKey</b>: chiave stabile della modalita'.</item>
    ///   <item><b>Label</b>: testo mostrabile.</item>
    ///   <item><b>OverlayKind</b>: famiglia overlay da attivare.</item>
    ///   <item><b>RequiresSelectedNpc</b>: richiede un NPC selezionato.</item>
    ///   <item><b>DebugOnly</b>: visibile solo in sviluppo/debug.</item>
    /// </list>
    /// </summary>
    public readonly struct ArcUiViewModeDefinition
    {
        public readonly string ViewModeKey;
        public readonly string Label;
        public readonly ArcUiViewOverlayKind OverlayKind;
        public readonly bool RequiresSelectedNpc;
        public readonly bool DebugOnly;

        public bool IsValid => !string.IsNullOrEmpty(ViewModeKey);

        // =============================================================================
        // ArcUiViewModeDefinition
        // =============================================================================
        /// <summary>
        /// <para>
        /// Costruisce una definizione view mode minimale.
        /// </para>
        /// </summary>
        public ArcUiViewModeDefinition(
            string viewModeKey,
            string label,
            ArcUiViewOverlayKind overlayKind,
            bool requiresSelectedNpc,
            bool debugOnly)
        {
            ViewModeKey = ArcUiOperationDefinition.NormalizeKey(viewModeKey);
            Label = string.IsNullOrWhiteSpace(label) ? string.Empty : label.Trim();
            OverlayKind = overlayKind;
            RequiresSelectedNpc = requiresSelectedNpc;
            DebugOnly = debugOnly;
        }
    }
}
