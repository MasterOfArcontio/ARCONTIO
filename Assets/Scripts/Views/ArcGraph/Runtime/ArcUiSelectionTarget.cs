namespace Arcontio.View.ArcGraph
{
    // =============================================================================
    // ArcUiSelectionTargetKind
    // =============================================================================
    /// <summary>
    /// <para>
    /// Tipo minimo di elemento selezionabile dalla UI runtime ArcGraph.
    /// </para>
    ///
    /// <para><b>Principio architetturale: selection target come snapshot leggero</b></para>
    /// <para>
    /// La selezione non deve trasportare oggetti mutabili del mondo. Questa enum
    /// identifica solo la famiglia dell'elemento osservato, cosi' inspector e hover
    /// possono ragionare su un contratto stabile invece di leggere direttamente il
    /// runtime simulativo.
    /// </para>
    ///
    /// <para><b>Struttura interna:</b></para>
    /// <list type="bullet">
    ///   <item><b>None</b>: nessun target valido.</item>
    ///   <item><b>Npc</b>: NPC o actor runtime.</item>
    ///   <item><b>Object</b>: oggetto piazzato nel mondo.</item>
    ///   <item><b>Wall</b>: muro o struttura assimilata.</item>
    ///   <item><b>Cell</b>: cella della mappa.</item>
    ///   <item><b>Plant</b>: pianta fisica futura.</item>
    ///   <item><b>Zone</b>: zona futura.</item>
    ///   <item><b>Debug</b>: elemento diagnostico non produttivo.</item>
    /// </list>
    /// </summary>
    public enum ArcUiSelectionTargetKind
    {
        None = 0,
        Npc = 1,
        Object = 2,
        Wall = 3,
        Cell = 4,
        Plant = 5,
        Zone = 6,
        Debug = 7
    }

    // =============================================================================
    // ArcUiSelectionTarget
    // =============================================================================
    /// <summary>
    /// <para>
    /// Target selezionabile o hoverabile esposto alla UI ArcGraph.
    /// </para>
    ///
    /// <para><b>Principio architetturale: UI consuma identita' e coordinate, non runtime mutabile</b></para>
    /// <para>
    /// Il target contiene solo il minimo necessario per aprire un inspector o
    /// disegnare un highlight: tipo, id testuale, cella, nome visuale e sorgente
    /// view-side. Non contiene riferimenti a NPC, oggetti, job, celle mutabili o
    /// <c>World</c>.
    /// </para>
    ///
    /// <para><b>Struttura interna:</b></para>
    /// <list type="bullet">
    ///   <item><b>Kind</b>: famiglia del target.</item>
    ///   <item><b>Id</b>: identificatore stabile quando disponibile.</item>
    ///   <item><b>Cell</b>: cella associata al target.</item>
    ///   <item><b>DisplayName</b>: testo mostrabile dall'inspector.</item>
    ///   <item><b>SourceView</b>: sorgente autorizzata del picking.</item>
    /// </list>
    /// </summary>
    public readonly struct ArcUiSelectionTarget
    {
        public readonly ArcUiSelectionTargetKind Kind;
        public readonly string Id;
        public readonly ArcGraphCellCoord Cell;
        public readonly string DisplayName;
        public readonly string SourceView;

        public bool IsValid => Kind != ArcUiSelectionTargetKind.None;

        // =============================================================================
        // ArcUiSelectionTarget
        // =============================================================================
        /// <summary>
        /// <para>
        /// Costruisce un target UI normalizzato.
        /// </para>
        ///
        /// <para><b>Contratto read-only</b></para>
        /// <para>
        /// Il costruttore copia valori semplici e normalizza stringhe. Se il target
        /// rappresenta una cella, l'id puo' restare vuoto: la cella stessa diventa
        /// l'identita' sufficiente per hover e selezione.
        /// </para>
        /// </summary>
        public ArcUiSelectionTarget(
            ArcUiSelectionTargetKind kind,
            string id,
            ArcGraphCellCoord cell,
            string displayName,
            string sourceView)
        {
            Kind = kind;
            Id = NormalizeId(id);
            Cell = cell;
            DisplayName = NormalizeText(displayName);
            SourceView = NormalizeText(sourceView);
        }

        // =============================================================================
        // None
        // =============================================================================
        /// <summary>
        /// <para>
        /// Crea un target vuoto usabile quando il puntatore non colpisce nulla.
        /// </para>
        /// </summary>
        public static ArcUiSelectionTarget None(string sourceView)
        {
            return new ArcUiSelectionTarget(
                ArcUiSelectionTargetKind.None,
                string.Empty,
                new ArcGraphCellCoord(0, 0, 0),
                string.Empty,
                sourceView);
        }

        // =============================================================================
        // NormalizeId
        // =============================================================================
        /// <summary>
        /// <para>
        /// Normalizza un id tecnico senza interpretarlo come id World.
        /// </para>
        /// </summary>
        private static string NormalizeId(string value)
        {
            return string.IsNullOrWhiteSpace(value) ? string.Empty : value.Trim();
        }

        // =============================================================================
        // NormalizeText
        // =============================================================================
        /// <summary>
        /// <para>
        /// Normalizza testo UI opzionale.
        /// </para>
        /// </summary>
        private static string NormalizeText(string value)
        {
            return string.IsNullOrWhiteSpace(value) ? string.Empty : value.Trim();
        }
    }
}
