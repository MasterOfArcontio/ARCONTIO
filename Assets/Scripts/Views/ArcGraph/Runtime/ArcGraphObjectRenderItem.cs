namespace Arcontio.View.ArcGraph
{
    // =============================================================================
    // ArcGraphObjectRenderItem
    // =============================================================================
    /// <summary>
    /// <para>
    /// Item passivo che descrive un oggetto renderizzabile da ArcGraph.
    /// </para>
    ///
    /// <para><b>Principio architetturale: oggetto visuale senza authority sugli stock</b></para>
    /// <para>
    /// L'item conserva i dati visuali copiati dallo snapshot oggetto. Anche quando
    /// contiene quantita' stock o holder actor, quei valori servono solo a label,
    /// debug o scelta grafica futura: non permettono al renderer di cambiare cibo,
    /// ownership, reservation o posizione nel mondo.
    /// </para>
    ///
    /// <para><b>Struttura interna:</b></para>
    /// <list type="bullet">
    ///   <item><b>ObjectId/DefId</b>: identita' oggetto.</item>
    ///   <item><b>Cell</b>: cella discreta visuale.</item>
    ///   <item><b>SpriteKey</b>: chiave sprite provvisoria.</item>
    ///   <item><b>ObjectMode</b>: LOD oggetto risolto.</item>
    ///   <item><b>IsHeld/HolderActorId</b>: stato trasporto copiato.</item>
    ///   <item><b>SortKey</b>: ordinamento deterministico.</item>
    /// </list>
    /// </summary>
    public readonly struct ArcGraphObjectRenderItem
    {
        public readonly int ObjectId;
        public readonly string DefId;
        public readonly ArcGraphCellCoord Cell;
        public readonly string SpriteKey;
        public readonly ArcGraphObjectLodMode ObjectMode;
        public readonly bool UsesSimplifiedRepresentation;
        public readonly bool ShowMinorItems;
        public readonly bool IsHeld;
        public readonly int HolderActorId;
        public readonly int FoodStockUnits;
        public readonly bool HasFoodStock;
        public readonly bool IsVisible;
        public readonly string HiddenReason;
        public readonly ArcGraphRenderSortKey SortKey;

        // =============================================================================
        // ArcGraphObjectRenderItem
        // =============================================================================
        /// <summary>
        /// <para>
        /// Costruisce un item oggetto renderizzabile.
        /// </para>
        ///
        /// <para><b>Normalizzazione difensiva</b></para>
        /// <para>
        /// Lo stock negativo indica assenza di stock osservabile. La visibilita'
        /// viene copiata come decisione gia' presa dal builder, senza consultare
        /// cataloghi o stato mondo.
        /// </para>
        /// </summary>
        public ArcGraphObjectRenderItem(
            int objectId,
            string defId,
            ArcGraphCellCoord cell,
            string spriteKey,
            ArcGraphObjectLodMode objectMode,
            bool usesSimplifiedRepresentation,
            bool showMinorItems,
            bool isHeld,
            int holderActorId,
            int foodStockUnits,
            bool isVisible,
            string hiddenReason,
            ArcGraphRenderSortKey sortKey)
        {
            ObjectId = objectId;
            DefId = defId ?? string.Empty;
            Cell = cell;
            SpriteKey = spriteKey ?? string.Empty;
            ObjectMode = objectMode;
            UsesSimplifiedRepresentation = usesSimplifiedRepresentation;
            ShowMinorItems = showMinorItems;
            IsHeld = isHeld;
            HolderActorId = holderActorId;
            FoodStockUnits = foodStockUnits;
            HasFoodStock = foodStockUnits >= 0;
            IsVisible = isVisible;
            HiddenReason = string.IsNullOrWhiteSpace(hiddenReason) ? "None" : hiddenReason;
            SortKey = sortKey;
        }
    }
}
