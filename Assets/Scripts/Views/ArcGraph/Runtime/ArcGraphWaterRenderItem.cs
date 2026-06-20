namespace Arcontio.View.ArcGraph
{
    // =============================================================================
    // ArcGraphWaterRenderItem
    // =============================================================================
    /// <summary>
    /// <para>
    /// Item passivo che descrive una cella d'acqua renderizzabile da ArcGraph.
    /// </para>
    ///
    /// <para><b>Principio architetturale: acqua visuale senza idraulica</b></para>
    /// <para>
    /// L'item conserva solo dati grafici derivati da
    /// <c>ArcGraphWaterVisualSnapshot</c>, dal contratto ambientale e dal profilo
    /// LOD. Non aggiorna livelli acqua, non calcola pressione, non propaga flussi,
    /// non decide attraversabilita' e non modifica terreno o pathfinding.
    /// </para>
    ///
    /// <para><b>Struttura interna:</b></para>
    /// <list type="bullet">
    ///   <item><b>Cell</b>: cella discreta visuale.</item>
    ///   <item><b>DepthLevel</b>: profondita' grafica astratta ricevuta dallo snapshot.</item>
    ///   <item><b>SpriteKey</b>: chiave sprite/atlas futura, non asset caricato.</item>
    ///   <item><b>IsAnimated</b>: indica se ArcGraph puo' scegliere frame acqua.</item>
    ///   <item><b>IsVisible/HiddenReason</b>: decisione visuale del builder.</item>
    ///   <item><b>SortKey</b>: ordinamento deterministico.</item>
    /// </list>
    /// </summary>
    public readonly struct ArcGraphWaterRenderItem
    {
        public readonly ArcGraphCellCoord Cell;
        public readonly int DepthLevel;
        public readonly string SpriteKey;
        public readonly bool IsAnimated;
        public readonly bool IsVisible;
        public readonly string HiddenReason;
        public readonly ArcGraphRenderSortKey SortKey;

        // =============================================================================
        // ArcGraphWaterRenderItem
        // =============================================================================
        /// <summary>
        /// <para>
        /// Costruisce un item acqua renderizzabile.
        /// </para>
        ///
        /// <para><b>Normalizzazione difensiva</b></para>
        /// <para>
        /// La profondita' negativa viene normalizzata a zero. Tutto il resto viene
        /// copiato come dato visuale: nessun calcolo idraulico avviene qui.
        /// </para>
        /// </summary>
        public ArcGraphWaterRenderItem(
            ArcGraphCellCoord cell,
            int depthLevel,
            string spriteKey,
            bool isAnimated,
            bool isVisible,
            string hiddenReason,
            ArcGraphRenderSortKey sortKey)
        {
            Cell = cell;
            DepthLevel = depthLevel < 0 ? 0 : depthLevel;
            SpriteKey = spriteKey ?? string.Empty;
            IsAnimated = isAnimated;
            IsVisible = isVisible;
            HiddenReason = string.IsNullOrWhiteSpace(hiddenReason) ? "None" : hiddenReason;
            SortKey = sortKey;
        }
    }
}
