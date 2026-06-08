namespace Arcontio.View.ArcGraph
{
    // =============================================================================
    // ArcGraphDebugEdgeOverlayItem
    // =============================================================================
    /// <summary>
    /// <para>
    /// Item diagnostico passivo per segmenti tra due celle della mappa.
    /// </para>
    ///
    /// <para><b>Principio architetturale: edge debug senza LineRenderer</b></para>
    /// <para>
    /// L'item non crea linee Unity e non decide se il tratto sara' un quad UI, un
    /// mesh segment o un line renderer futuro. Conserva solo gli estremi discreti,
    /// il tipo diagnostico, l'affidabilita' e lo stile astratto.
    /// </para>
    ///
    /// <para><b>Struttura interna:</b></para>
    /// <list type="bullet">
    ///   <item><b>From/To</b>: estremi cell-based del segmento.</item>
    ///   <item><b>Kind</b>: tipo diagnostico dell'edge.</item>
    ///   <item><b>Reliability01</b>: valore normalizzato utile a colori/alpha futuri.</item>
    ///   <item><b>WidthKey</b>: chiave spessore futura.</item>
    ///   <item><b>ColorKey</b>: chiave colore futura.</item>
    ///   <item><b>SortKey</b>: ordinamento deterministico, ancorato all'estremo iniziale.</item>
    /// </list>
    /// </summary>
    public readonly struct ArcGraphDebugEdgeOverlayItem
    {
        public readonly ArcGraphCellCoord From;
        public readonly ArcGraphCellCoord To;
        public readonly ArcGraphDebugOverlayKind Kind;
        public readonly float Reliability01;
        public readonly string WidthKey;
        public readonly string ColorKey;
        public readonly bool IsVisible;
        public readonly string HiddenReason;
        public readonly ArcGraphRenderSortKey SortKey;

        public ArcGraphDebugOverlaySpace Space => ArcGraphDebugOverlaySpace.MapEdge;

        public ArcGraphDebugEdgeOverlayItem(
            ArcGraphCellCoord from,
            ArcGraphCellCoord to,
            ArcGraphDebugOverlayKind kind,
            float reliability01,
            string widthKey,
            string colorKey,
            bool isVisible,
            string hiddenReason,
            ArcGraphRenderSortKey sortKey)
        {
            From = from;
            To = to;
            Kind = kind;
            Reliability01 = Clamp01(reliability01);
            WidthKey = string.IsNullOrWhiteSpace(widthKey) ? ResolveDefaultWidthKey(kind) : widthKey;
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

        private static string ResolveDefaultWidthKey(ArcGraphDebugOverlayKind kind)
        {
            switch (kind)
            {
                case ArcGraphDebugOverlayKind.LandmarkRouteEdge:
                case ArcGraphDebugOverlayKind.LandmarkLmPathEdge:
                case ArcGraphDebugOverlayKind.LandmarkDirectPathEdge:
                case ArcGraphDebugOverlayKind.LandmarkJumpPathEdge:
                case ArcGraphDebugOverlayKind.LandmarkComplexEdge:
                    return "debug/edge/strong";
                default:
                    return "debug/edge/normal";
            }
        }

        private static string ResolveDefaultColorKey(ArcGraphDebugOverlayKind kind)
        {
            switch (kind)
            {
                case ArcGraphDebugOverlayKind.LandmarkWorldEdge:
                    return "debug/landmark/world-edge";
                case ArcGraphDebugOverlayKind.LandmarkKnownEdge:
                    return "debug/landmark/known-edge";
                case ArcGraphDebugOverlayKind.LandmarkRouteEdge:
                    return "debug/landmark/route-edge";
                case ArcGraphDebugOverlayKind.LandmarkLmPathEdge:
                    return "debug/path/lm";
                case ArcGraphDebugOverlayKind.LandmarkDirectPathEdge:
                    return "debug/path/direct";
                case ArcGraphDebugOverlayKind.LandmarkJumpPathEdge:
                    return "debug/path/jump";
                case ArcGraphDebugOverlayKind.LandmarkComplexEdge:
                    return "debug/path/complex";
                case ArcGraphDebugOverlayKind.LandmarkGvdEdge:
                    return "debug/gvd/edge";
                default:
                    return "debug/edge/default";
            }
        }
    }
}
