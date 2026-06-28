namespace Arcontio.View.ArcGraph
{
    // =============================================================================
    // ArcGraphVegetationRenderItem
    // =============================================================================
    /// <summary>
    /// <para>
    /// Item passivo che descrive una vegetazione renderizzabile da ArcGraph.
    /// </para>
    ///
    /// <para><b>Principio architetturale: vegetazione visuale senza biosfera</b></para>
    /// <para>
    /// L'item conserva solo dati grafici derivati da
    /// <c>ArcGraphVegetationVisualSnapshot</c> e dalla policy LOD. Non decide se una
    /// pianta nasce, cresce, produce semi o muore. Non legge seed bank, fertilita',
    /// umidita', stagione o sistemi ambientali. Il suo compito e' dire a un futuro
    /// renderer quale rappresentazione mostrare per una cella vegetale.
    /// </para>
    ///
    /// <para><b>Struttura interna:</b></para>
    /// <list type="bullet">
    ///   <item><b>Cell</b>: cella discreta visuale.</item>
    ///   <item><b>SpeciesKey</b>: specie/categoria visuale ricevuta dallo snapshot.</item>
    ///   <item><b>GrowthStage</b>: stadio grafico, non biologico calcolato qui.</item>
    ///   <item><b>Density01</b>: densita' visuale normalizzata.</item>
    ///   <item><b>SpriteKey</b>: chiave sprite/atlas futura, non asset caricato.</item>
    ///   <item><b>VegetationMode</b>: LOD vegetazione risolto.</item>
    ///   <item><b>AllowsSpriteAnimation</b>: abilita futura scelta frame ArcGraph.</item>
    ///   <item><b>PlantId</b>: id stabile della pianta fisica quando presente.</item>
    ///   <item><b>IsPhysicalPlant</b>: distingue piante selezionabili da overlay diffusi.</item>
    ///   <item><b>IsVisible/HiddenReason</b>: decisione visuale del builder.</item>
    ///   <item><b>SortKey</b>: ordinamento deterministico.</item>
    /// </list>
    /// </summary>
    public readonly struct ArcGraphVegetationRenderItem
    {
        public readonly ArcGraphCellCoord Cell;
        public readonly string SpeciesKey;
        public readonly int GrowthStage;
        public readonly float Density01;
        public readonly string SpriteKey;
        public readonly ArcGraphVegetationLodMode VegetationMode;
        public readonly bool AllowsSpriteAnimation;
        public readonly bool IsAreaAggregate;
        public readonly int PlantId;
        public readonly bool IsPhysicalPlant;
        public readonly bool IsVisible;
        public readonly string HiddenReason;
        public readonly ArcGraphRenderSortKey SortKey;

        // =============================================================================
        // ArcGraphVegetationRenderItem
        // =============================================================================
        /// <summary>
        /// <para>
        /// Costruisce un item vegetazione renderizzabile.
        /// </para>
        ///
        /// <para><b>Normalizzazione difensiva</b></para>
        /// <para>
        /// Densita' e growth stage vengono normalizzati dal snapshot sorgente. Qui
        /// vengono copiati senza calcoli biologici. La visibilita' e la sprite key
        /// sono gia' decise dal builder, che resta un traduttore visuale.
        /// </para>
        /// </summary>
        public ArcGraphVegetationRenderItem(
            ArcGraphCellCoord cell,
            string speciesKey,
            int growthStage,
            float density01,
            string spriteKey,
            ArcGraphVegetationLodMode vegetationMode,
            bool allowsSpriteAnimation,
            bool isAreaAggregate,
            bool isVisible,
            string hiddenReason,
            ArcGraphRenderSortKey sortKey,
            int plantId = -1,
            bool isPhysicalPlant = false)
        {
            Cell = cell;
            SpeciesKey = speciesKey ?? string.Empty;
            GrowthStage = growthStage < 0 ? 0 : growthStage;
            Density01 = Clamp01(density01);
            SpriteKey = spriteKey ?? string.Empty;
            VegetationMode = vegetationMode;
            AllowsSpriteAnimation = allowsSpriteAnimation;
            IsAreaAggregate = isAreaAggregate;
            PlantId = plantId > 0 ? plantId : -1;
            IsPhysicalPlant = isPhysicalPlant && PlantId > 0;
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
    }
}
