namespace Arcontio.View.ArcGraph
{
    // =============================================================================
    // ArcGraphDebugCellOverlayItem
    // =============================================================================
    /// <summary>
    /// <para>
    /// Item diagnostico passivo per overlay che occupano una cella della mappa.
    /// </para>
    ///
    /// <para><b>Principio architetturale: cell debug senza simulazione</b></para>
    /// <para>
    /// Questo item puo' rappresentare FOV, DT heatmap o celle GVD grezze. Non calcola
    /// percezione, non interroga linea di vista, non valuta landmark e non legge il
    /// <c>World</c>. Riceve dati gia' preparati da un futuro builder e li rende
    /// ordinabili da ArcGraph.
    /// </para>
    ///
    /// <para><b>Struttura interna:</b></para>
    /// <list type="bullet">
    ///   <item><b>Cell</b>: coordinata ArcGraph dell'overlay.</item>
    ///   <item><b>Kind</b>: significato diagnostico della cella.</item>
    ///   <item><b>Intensity01</b>: intensita' visuale normalizzata.</item>
    ///   <item><b>NumericValue</b>: valore opzionale, per esempio DT intero.</item>
    ///   <item><b>ColorKey</b>: chiave colore futura, non materiale Unity.</item>
    ///   <item><b>IsVisible/HiddenReason</b>: decisione visuale gia' risolta.</item>
    ///   <item><b>SortKey</b>: ordinamento deterministico.</item>
    /// </list>
    /// </summary>
    public readonly struct ArcGraphDebugCellOverlayItem
    {
        public readonly ArcGraphCellCoord Cell;
        public readonly ArcGraphDebugOverlayKind Kind;
        public readonly float Intensity01;
        public readonly int NumericValue;
        public readonly string ColorKey;
        public readonly bool IsVisible;
        public readonly string HiddenReason;
        public readonly ArcGraphRenderSortKey SortKey;

        public ArcGraphDebugOverlaySpace Space => ArcGraphDebugOverlaySpace.MapCell;

        // =============================================================================
        // ArcGraphDebugCellOverlayItem
        // =============================================================================
        /// <summary>
        /// <para>
        /// Costruisce un item debug cell-based normalizzando i campi fragili.
        /// </para>
        /// </summary>
        public ArcGraphDebugCellOverlayItem(
            ArcGraphCellCoord cell,
            ArcGraphDebugOverlayKind kind,
            float intensity01,
            int numericValue,
            string colorKey,
            bool isVisible,
            string hiddenReason,
            ArcGraphRenderSortKey sortKey)
        {
            Cell = cell;
            Kind = kind;
            Intensity01 = Clamp01(intensity01);
            NumericValue = numericValue;
            ColorKey = string.IsNullOrWhiteSpace(colorKey) ? ResolveDefaultColorKey(kind) : colorKey;
            IsVisible = isVisible;
            HiddenReason = string.IsNullOrWhiteSpace(hiddenReason) ? "None" : hiddenReason;
            SortKey = sortKey;
        }

        private static float Clamp01(float value)
        {
            if (value <= 0f)
                return 0f;

            if (value >= 1f)
                return 1f;

            return value;
        }

        private static string ResolveDefaultColorKey(ArcGraphDebugOverlayKind kind)
        {
            switch (kind)
            {
                case ArcGraphDebugOverlayKind.FovObservedCell:
                    return "debug/fov/observed";
                case ArcGraphDebugOverlayKind.FovWatchedMarginCell:
                    return "debug/fov/watched";
                case ArcGraphDebugOverlayKind.FovHistoricalHeatCell:
                    return "debug/fov/heat";
                case ArcGraphDebugOverlayKind.DtHeatCell:
                    return "debug/dt/heat";
                case ArcGraphDebugOverlayKind.GvdRawCell:
                    return "debug/gvd/raw";
                case ArcGraphDebugOverlayKind.SpatialAreaCell:
                    return "debug/area/cell";
                default:
                    return "debug/cell/default";
            }
        }
    }
}
