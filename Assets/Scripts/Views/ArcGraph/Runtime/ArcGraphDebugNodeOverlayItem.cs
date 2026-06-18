namespace Arcontio.View.ArcGraph
{
    // =============================================================================
    // ArcGraphDebugNodeOverlayItem
    // =============================================================================
    /// <summary>
    /// <para>
    /// Item diagnostico passivo per marker puntuali ancorati alla mappa.
    /// </para>
    ///
    /// <para><b>Principio architetturale: landmark come dato visuale</b></para>
    /// <para>
    /// I nodi landmark, known, route e GVD arrivano gia' come DTO dal Core. Questo
    /// item non conosce <c>LandmarkRegistry</c>, non consulta memoria NPC e non
    /// produce pathfinding. Trasporta solo posizione, id e stile astratto.
    /// </para>
    ///
    /// <para><b>Struttura interna:</b></para>
    /// <list type="bullet">
    ///   <item><b>Cell</b>: cella di ancoraggio del marker.</item>
    ///   <item><b>Kind</b>: layer diagnostico del nodo.</item>
    ///   <item><b>NodeId</b>: id esterno stabile, se disponibile.</item>
    ///   <item><b>Label</b>: testo breve opzionale.</item>
    ///   <item><b>Scale01</b>: scala normalizzata per renderer futuri.</item>
    ///   <item><b>ColorKey</b>: chiave colore futura.</item>
    ///   <item><b>SortKey</b>: ordinamento deterministico.</item>
    /// </list>
    /// </summary>
    public readonly struct ArcGraphDebugNodeOverlayItem
    {
        public readonly ArcGraphCellCoord Cell;
        public readonly ArcGraphDebugOverlayKind Kind;
        public readonly int NodeId;
        public readonly string Label;
        public readonly float Scale01;
        public readonly string ColorKey;
        public readonly bool IsVisible;
        public readonly string HiddenReason;
        public readonly ArcGraphRenderSortKey SortKey;

        public ArcGraphDebugOverlaySpace Space => ArcGraphDebugOverlaySpace.MapNode;

        public ArcGraphDebugNodeOverlayItem(
            ArcGraphCellCoord cell,
            ArcGraphDebugOverlayKind kind,
            int nodeId,
            string label,
            float scale01,
            string colorKey,
            bool isVisible,
            string hiddenReason,
            ArcGraphRenderSortKey sortKey)
        {
            Cell = cell;
            Kind = kind;
            NodeId = nodeId;
            Label = label ?? string.Empty;
            Scale01 = Clamp01(scale01);
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
                case ArcGraphDebugOverlayKind.LandmarkWorldNode:
                    return "debug/landmark/world-node";
                case ArcGraphDebugOverlayKind.LandmarkKnownNode:
                    return "debug/landmark/known-node";
                case ArcGraphDebugOverlayKind.LandmarkRouteNode:
                    return "debug/landmark/route-node";
                case ArcGraphDebugOverlayKind.LandmarkGvdNode:
                    return "debug/landmark/gvd-node";
                case ArcGraphDebugOverlayKind.LandmarkBiologicalNode:
                    return "debug/landmark/biological-node";
                default:
                    return "debug/node/default";
            }
        }
    }
}
