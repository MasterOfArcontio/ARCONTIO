namespace Arcontio.View.ArcGraph
{
    // =============================================================================
    // ArcGraphWaterVisualSnapshot
    // =============================================================================
    /// <summary>
    /// <para>
    /// Snapshot visuale placeholder per acqua o liquidi dentro <c>arcgraph</c>.
    /// </para>
    ///
    /// <para><b>Principio architetturale: acqua visuale, non simulazione idraulica</b></para>
    /// <para>
    /// Questo snapshot non calcola flusso, pressione, evaporazione o profondita'
    /// reale. Conserva solo dati minimi che un renderer futuro potra' usare per
    /// disegnare una cella con acqua. La simulazione dell'acqua, quando verra'
    /// introdotta, dovra' produrre questi dati da un sistema separato.
    /// </para>
    ///
    /// <para><b>Struttura interna:</b></para>
    /// <list type="bullet">
    ///   <item><b>Cell</b>: coordinata della cella visuale.</item>
    ///   <item><b>DepthLevel</b>: livello astratto di profondita' grafica.</item>
    ///   <item><b>SpriteKey</b>: chiave sprite o catalogo futuro.</item>
    ///   <item><b>IsAnimated</b>: flag per animazione futura, non runtime attuale.</item>
    /// </list>
    /// </summary>
    public readonly struct ArcGraphWaterVisualSnapshot
    {
        public readonly ArcGraphCellCoord Cell;
        public readonly int DepthLevel;
        public readonly string SpriteKey;
        public readonly bool IsAnimated;

        public ArcGraphWaterVisualSnapshot(
            ArcGraphCellCoord cell,
            int depthLevel,
            string spriteKey,
            bool isAnimated)
        {
            Cell = cell;
            DepthLevel = depthLevel < 0 ? 0 : depthLevel;
            SpriteKey = spriteKey ?? string.Empty;
            IsAnimated = isAnimated;
        }
    }

    // =============================================================================
    // ArcGraphVegetationVisualSnapshot
    // =============================================================================
    /// <summary>
    /// <para>
    /// Snapshot visuale placeholder per erba, piante e vegetazione.
    /// </para>
    ///
    /// <para><b>Principio architetturale: crescita visuale derivata</b></para>
    /// <para>
    /// La crescita biologica non vive in questo snapshot. Qui restano solo una
    /// specie/categoria visuale, uno stadio grafico e una densita' astratta. Il
    /// VegetationSystem futuro potra' produrre questi valori, ma <c>arcgraph</c>
    /// dovra' limitarsi a mostrarli.
    /// </para>
    ///
    /// <para><b>Struttura interna:</b></para>
    /// <list type="bullet">
    ///   <item><b>Cell</b>: coordinata della vegetazione.</item>
    ///   <item><b>SpeciesKey</b>: specie, categoria o prefab visuale futuro.</item>
    ///   <item><b>GrowthStage</b>: stadio visuale astratto.</item>
    ///   <item><b>Density01</b>: densita' normalizzata per overlay o tile blend.</item>
    ///   <item><b>SpriteKey</b>: chiave visuale ArcGraph opzionale gia' risolta dal bordo View.</item>
    ///   <item><b>PlantId</b>: id della pianta fisica quando lo snapshot rappresenta una pianta selezionabile.</item>
    ///   <item><b>IsPhysicalPlant</b>: distingue piante fisiche da vegetazione diffusa non selezionabile.</item>
    /// </list>
    /// </summary>
    public readonly struct ArcGraphVegetationVisualSnapshot
    {
        public readonly ArcGraphCellCoord Cell;
        public readonly string SpeciesKey;
        public readonly int GrowthStage;
        public readonly float Density01;
        public readonly string SpriteKey;
        public readonly int PlantId;
        public readonly bool IsPhysicalPlant;

        public ArcGraphVegetationVisualSnapshot(
            ArcGraphCellCoord cell,
            string speciesKey,
            int growthStage,
            float density01)
            : this(cell, speciesKey, growthStage, density01, string.Empty, -1, false)
        {
        }

        public ArcGraphVegetationVisualSnapshot(
            ArcGraphCellCoord cell,
            string speciesKey,
            int growthStage,
            float density01,
            string spriteKey)
            : this(cell, speciesKey, growthStage, density01, spriteKey, -1, false)
        {
        }

        // =============================================================================
        // ArcGraphVegetationVisualSnapshot
        // =============================================================================
        /// <summary>
        /// <para>
        /// Costruisce uno snapshot vegetazione completo distinguendo overlay diffuso
        /// e pianta fisica selezionabile.
        /// </para>
        ///
        /// <para><b>Principio architetturale: identita' visuale senza mutabilita'</b></para>
        /// <para>
        /// L'id pianta resta un intero copiato dalla proiezione World. Non espone
        /// l'istanza Biosfera, non consente mutazioni e serve solo a stabilizzare
        /// picking, label e inspector read-only.
        /// </para>
        /// </summary>
        public ArcGraphVegetationVisualSnapshot(
            ArcGraphCellCoord cell,
            string speciesKey,
            int growthStage,
            float density01,
            string spriteKey,
            int plantId,
            bool isPhysicalPlant)
        {
            Cell = cell;
            SpeciesKey = speciesKey ?? string.Empty;
            GrowthStage = growthStage < 0 ? 0 : growthStage;
            Density01 = Clamp01(density01);
            SpriteKey = spriteKey ?? string.Empty;
            PlantId = plantId > 0 ? plantId : -1;
            IsPhysicalPlant = isPhysicalPlant && PlantId > 0;
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

    // =============================================================================
    // ArcGraphLightVisualSnapshot
    // =============================================================================
    /// <summary>
    /// <para>
    /// Snapshot visuale placeholder per luce locale o globale applicata a una cella.
    /// </para>
    ///
    /// <para><b>Principio architetturale: luce come presentazione derivata</b></para>
    /// <para>
    /// Questo dato non propaga luce, non calcola ombre e non modifica percezione NPC.
    /// Rappresenta soltanto il risultato visuale che un sistema luce futuro potra'
    /// consegnare al renderer: intensita', colore astratto e flag di sorgente locale.
    /// </para>
    ///
    /// <para><b>Struttura interna:</b></para>
    /// <list type="bullet">
    ///   <item><b>Cell</b>: cella interessata dalla luce.</item>
    ///   <item><b>Intensity01</b>: intensita' normalizzata.</item>
    ///   <item><b>TintKey</b>: chiave colore futura, ad esempio giorno/notte/torcia.</item>
    ///   <item><b>HasLocalSource</b>: segnala una sorgente locale futura.</item>
    /// </list>
    /// </summary>
    public readonly struct ArcGraphLightVisualSnapshot
    {
        public readonly ArcGraphCellCoord Cell;
        public readonly float Intensity01;
        public readonly string TintKey;
        public readonly bool HasLocalSource;

        public ArcGraphLightVisualSnapshot(
            ArcGraphCellCoord cell,
            float intensity01,
            string tintKey,
            bool hasLocalSource)
        {
            Cell = cell;
            Intensity01 = Clamp01(intensity01);
            TintKey = tintKey ?? string.Empty;
            HasLocalSource = hasLocalSource;
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

    // =============================================================================
    // ArcGraphWeatherVisualSnapshot
    // =============================================================================
    /// <summary>
    /// <para>
    /// Snapshot visuale placeholder per meteo globale o di livello.
    /// </para>
    ///
    /// <para><b>Principio architetturale: meteo renderizzato, non clima simulato</b></para>
    /// <para>
    /// Il meteo reale dovra' essere prodotto da un sistema ambientale separato.
    /// Questo snapshot conserva solo una chiave evento, un'intensita' e un livello
    /// <c>Z</c> interessato, cosi' il renderer futuro potra' sapere se mostrare
    /// pioggia, neve, vento o overlay ambientali.
    /// </para>
    ///
    /// <para><b>Struttura interna:</b></para>
    /// <list type="bullet">
    ///   <item><b>WeatherKey</b>: tipo visuale, ad esempio rain/snow/wind.</item>
    ///   <item><b>Intensity01</b>: intensita' normalizzata.</item>
    ///   <item><b>AffectedZLevel</b>: livello grafico interessato.</item>
    ///   <item><b>IsActive</b>: indica se il meteo va mostrato.</item>
    /// </list>
    /// </summary>
    public readonly struct ArcGraphWeatherVisualSnapshot
    {
        public readonly string WeatherKey;
        public readonly float Intensity01;
        public readonly int AffectedZLevel;
        public readonly bool IsActive;

        public ArcGraphWeatherVisualSnapshot(
            string weatherKey,
            float intensity01,
            int affectedZLevel,
            bool isActive)
        {
            WeatherKey = weatherKey ?? string.Empty;
            Intensity01 = Clamp01(intensity01);
            AffectedZLevel = affectedZLevel;
            IsActive = isActive;
        }

        public static ArcGraphWeatherVisualSnapshot None(int affectedZLevel = ArcGraphZLevelPolicy.DefaultVisibleZLevel)
        {
            return new ArcGraphWeatherVisualSnapshot(string.Empty, 0f, affectedZLevel, false);
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

    // =============================================================================
    // ArcGraphEffectVisualSnapshot
    // =============================================================================
    /// <summary>
    /// <para>
    /// Snapshot visuale placeholder per effetti locali come fuoco, fumo o segnali.
    /// </para>
    ///
    /// <para><b>Principio architetturale: effetto locale senza causalita' simulativa</b></para>
    /// <para>
    /// Un effetto visuale non deve decidere danni, propagazione del fuoco, visibilita'
    /// o memoria NPC. Questo snapshot dichiara solo che un effetto va mostrato in
    /// una cella con una certa intensita' grafica.
    /// </para>
    ///
    /// <para><b>Struttura interna:</b></para>
    /// <list type="bullet">
    ///   <item><b>EffectId</b>: id visuale locale o runtime futuro.</item>
    ///   <item><b>Cell</b>: cella dell'effetto.</item>
    ///   <item><b>EffectKey</b>: tipo visuale, ad esempio fire/smoke/spark.</item>
    ///   <item><b>Intensity01</b>: intensita' normalizzata.</item>
    /// </list>
    /// </summary>
    public readonly struct ArcGraphEffectVisualSnapshot
    {
        public readonly int EffectId;
        public readonly ArcGraphCellCoord Cell;
        public readonly string EffectKey;
        public readonly float Intensity01;

        public ArcGraphEffectVisualSnapshot(
            int effectId,
            ArcGraphCellCoord cell,
            string effectKey,
            float intensity01)
        {
            EffectId = effectId;
            Cell = cell;
            EffectKey = effectKey ?? string.Empty;
            Intensity01 = Clamp01(intensity01);
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
