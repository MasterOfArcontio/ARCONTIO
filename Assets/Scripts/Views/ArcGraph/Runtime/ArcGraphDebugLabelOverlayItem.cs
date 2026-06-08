namespace Arcontio.View.ArcGraph
{
    // =============================================================================
    // ArcGraphDebugLabelOverlayItem
    // =============================================================================
    /// <summary>
    /// <para>
    /// Item diagnostico passivo per label e HUD screen-space.
    /// </para>
    ///
    /// <para><b>Principio architetturale: testo debug senza Canvas</b></para>
    /// <para>
    /// L'item non crea <c>Canvas</c>, <c>Text</c>, <c>TextMeshPro</c> o pannelli.
    /// Serve a rappresentare testo che dovra' essere disegnato da un renderer UI
    /// separato. Questo impedisce di mescolare overlay cell-based, label ancorate
    /// alla mappa e HUD globale nello stesso contratto.
    /// </para>
    ///
    /// <para><b>Struttura interna:</b></para>
    /// <list type="bullet">
    ///   <item><b>AnchorCell</b>: cella opzionale di ancoraggio.</item>
    ///   <item><b>Kind</b>: tipo di label o HUD.</item>
    ///   <item><b>OwnerId</b>: id opzionale del soggetto rappresentato.</item>
    ///   <item><b>Text</b>: contenuto gia' formato dal producer debug.</item>
    ///   <item><b>Space</b>: <c>ScreenLabel</c> o <c>ScreenHud</c>.</item>
    ///   <item><b>SortKey</b>: ordinamento deterministico.</item>
    /// </list>
    /// </summary>
    public readonly struct ArcGraphDebugLabelOverlayItem
    {
        public readonly ArcGraphCellCoord AnchorCell;
        public readonly ArcGraphDebugOverlayKind Kind;
        public readonly int OwnerId;
        public readonly string Text;
        public readonly ArcGraphDebugOverlaySpace Space;
        public readonly bool IsVisible;
        public readonly string HiddenReason;
        public readonly ArcGraphRenderSortKey SortKey;

        public ArcGraphDebugLabelOverlayItem(
            ArcGraphCellCoord anchorCell,
            ArcGraphDebugOverlayKind kind,
            int ownerId,
            string text,
            ArcGraphDebugOverlaySpace space,
            bool isVisible,
            string hiddenReason,
            ArcGraphRenderSortKey sortKey)
        {
            AnchorCell = anchorCell;
            Kind = kind;
            OwnerId = ownerId;
            Text = text ?? string.Empty;
            Space = NormalizeSpace(space);
            IsVisible = isVisible;
            HiddenReason = string.IsNullOrWhiteSpace(hiddenReason) ? "None" : hiddenReason;
            SortKey = sortKey;
        }

        private static ArcGraphDebugOverlaySpace NormalizeSpace(ArcGraphDebugOverlaySpace space)
        {
            if (space == ArcGraphDebugOverlaySpace.ScreenHud)
                return ArcGraphDebugOverlaySpace.ScreenHud;

            return ArcGraphDebugOverlaySpace.ScreenLabel;
        }
    }
}
