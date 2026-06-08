namespace Arcontio.View.ArcGraph
{
    // =============================================================================
    // ArcGraphEffectRenderItem
    // =============================================================================
    /// <summary>
    /// <para>
    /// Item passivo che descrive un effetto locale renderizzabile da ArcGraph.
    /// </para>
    ///
    /// <para><b>Principio architetturale: effetto visuale senza simulazione effetti</b></para>
    /// <para>
    /// L'item conserva solo dati grafici derivati da
    /// <c>ArcGraphEffectVisualSnapshot</c>, dal contratto ambientale e dal profilo
    /// LOD. Non propaga incendi, non calcola fumo, non infligge danni, non cambia
    /// luce, non crea particelle Unity e non modifica il mondo.
    /// </para>
    ///
    /// <para><b>Struttura interna:</b></para>
    /// <list type="bullet">
    ///   <item><b>EffectId</b>: id visuale locale o runtime futuro.</item>
    ///   <item><b>Cell</b>: cella discreta dell'effetto.</item>
    ///   <item><b>EffectKey</b>: tipo effetto, ad esempio fire, smoke o spark.</item>
    ///   <item><b>Intensity01</b>: intensita' visuale normalizzata.</item>
    ///   <item><b>SpriteKey</b>: chiave sprite/atlas futura, non asset caricato.</item>
    ///   <item><b>EffectMode</b>: modalita' LOD effetto risolta.</item>
    ///   <item><b>IsAnimated</b>: indica se ArcGraph puo' scegliere frame effetto.</item>
    ///   <item><b>UsesLocalTint</b>: indica se il renderer futuro puo' applicare tinta locale.</item>
    ///   <item><b>UsesSimplifiedRepresentation</b>: LOD semplificato attivo.</item>
    ///   <item><b>IsVisible/HiddenReason</b>: decisione visuale del builder.</item>
    ///   <item><b>SortKey</b>: ordinamento deterministico.</item>
    /// </list>
    /// </summary>
    public readonly struct ArcGraphEffectRenderItem
    {
        public readonly int EffectId;
        public readonly ArcGraphCellCoord Cell;
        public readonly string EffectKey;
        public readonly float Intensity01;
        public readonly string SpriteKey;
        public readonly ArcGraphEffectLodMode EffectMode;
        public readonly bool IsAnimated;
        public readonly bool UsesLocalTint;
        public readonly bool UsesSimplifiedRepresentation;
        public readonly bool IsVisible;
        public readonly string HiddenReason;
        public readonly ArcGraphRenderSortKey SortKey;

        // =============================================================================
        // ArcGraphEffectRenderItem
        // =============================================================================
        /// <summary>
        /// <para>
        /// Costruisce un item effetto renderizzabile.
        /// </para>
        ///
        /// <para><b>Normalizzazione difensiva</b></para>
        /// <para>
        /// Intensita' e stringhe vengono normalizzate localmente per evitare item
        /// ambigui. Nessuna logica fisica o simulativa viene eseguita: il dato resta
        /// un payload visuale value-only.
        /// </para>
        /// </summary>
        public ArcGraphEffectRenderItem(
            int effectId,
            ArcGraphCellCoord cell,
            string effectKey,
            float intensity01,
            string spriteKey,
            ArcGraphEffectLodMode effectMode,
            bool isAnimated,
            bool usesLocalTint,
            bool usesSimplifiedRepresentation,
            bool isVisible,
            string hiddenReason,
            ArcGraphRenderSortKey sortKey)
        {
            EffectId = effectId;
            Cell = cell;
            EffectKey = effectKey ?? string.Empty;
            Intensity01 = Clamp01(intensity01);
            SpriteKey = spriteKey ?? string.Empty;
            EffectMode = effectMode;
            IsAnimated = isAnimated;
            UsesLocalTint = usesLocalTint;
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
