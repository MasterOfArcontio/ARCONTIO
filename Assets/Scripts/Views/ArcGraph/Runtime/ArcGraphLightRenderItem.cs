namespace Arcontio.View.ArcGraph
{
    // =============================================================================
    // ArcGraphLightRenderItem
    // =============================================================================
    /// <summary>
    /// <para>
    /// Item passivo che descrive una tinta/luce applicabile a una cella ArcGraph.
    /// </para>
    ///
    /// <para><b>Principio architetturale: luce visuale senza propagazione</b></para>
    /// <para>
    /// L'item conserva solo dati grafici derivati da
    /// <c>ArcGraphLightVisualSnapshot</c>, dal contratto ambientale e dal profilo
    /// LOD. Non calcola ombre, non attenua luce attraverso muri o stanze, non crea
    /// sorgenti Unity e non modifica percezione o pathfinding.
    /// </para>
    ///
    /// <para><b>Struttura interna:</b></para>
    /// <list type="bullet">
    ///   <item><b>Cell</b>: cella discreta visuale.</item>
    ///   <item><b>Intensity01</b>: intensita' luce normalizzata ricevuta dallo snapshot.</item>
    ///   <item><b>TintKey</b>: chiave tinta futura, non materiale o asset caricato.</item>
    ///   <item><b>HasLocalSource</b>: indica se la cella contiene una sorgente locale gia' risolta.</item>
    ///   <item><b>AllowsGlobalOverlay</b>: contratto consente tinta globale compositiva.</item>
    ///   <item><b>AllowsLocalTint</b>: contratto consente tinta locale per cella.</item>
    ///   <item><b>UsesSimplifiedRepresentation</b>: LOD semplificato attivo.</item>
    ///   <item><b>IsVisible/HiddenReason</b>: decisione visuale del builder.</item>
    ///   <item><b>SortKey</b>: ordinamento deterministico.</item>
    /// </list>
    /// </summary>
    public readonly struct ArcGraphLightRenderItem
    {
        public readonly ArcGraphCellCoord Cell;
        public readonly float Intensity01;
        public readonly string TintKey;
        public readonly bool HasLocalSource;
        public readonly bool AllowsGlobalOverlay;
        public readonly bool AllowsLocalTint;
        public readonly bool UsesSimplifiedRepresentation;
        public readonly bool IsVisible;
        public readonly string HiddenReason;
        public readonly ArcGraphRenderSortKey SortKey;

        // =============================================================================
        // ArcGraphLightRenderItem
        // =============================================================================
        /// <summary>
        /// <para>
        /// Costruisce un item luce renderizzabile.
        /// </para>
        ///
        /// <para><b>Normalizzazione difensiva</b></para>
        /// <para>
        /// L'intensita' viene clampata tra zero e uno. Le stringhe nulle diventano
        /// stringhe vuote. Nessun calcolo fisico o percettivo viene eseguito qui.
        /// </para>
        /// </summary>
        public ArcGraphLightRenderItem(
            ArcGraphCellCoord cell,
            float intensity01,
            string tintKey,
            bool hasLocalSource,
            bool allowsGlobalOverlay,
            bool allowsLocalTint,
            bool usesSimplifiedRepresentation,
            bool isVisible,
            string hiddenReason,
            ArcGraphRenderSortKey sortKey)
        {
            Cell = cell;
            Intensity01 = Clamp01(intensity01);
            TintKey = tintKey ?? string.Empty;
            HasLocalSource = hasLocalSource;
            AllowsGlobalOverlay = allowsGlobalOverlay;
            AllowsLocalTint = allowsLocalTint;
            UsesSimplifiedRepresentation = usesSimplifiedRepresentation;
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
