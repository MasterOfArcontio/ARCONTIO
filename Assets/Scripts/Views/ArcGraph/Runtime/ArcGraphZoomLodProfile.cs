namespace Arcontio.View.ArcGraph
{
    // =============================================================================
    // ArcGraphZoomLodProfile
    // =============================================================================
    /// <summary>
    /// <para>
    /// Profilo di dettaglio visuale risolto per uno specifico livello zoom.
    /// </para>
    ///
    /// <para><b>Principio architetturale: policy visuale centralizzata</b></para>
    /// <para>
    /// I futuri renderer non devono inventare localmente cosa significhi "zoom 1"
    /// o "zoom 4". Questo profilo raccoglie le decisioni di dettaglio in un unico
    /// value object: animazioni, actor layered, rappresentazione semplificata,
    /// vegetazione, oggetti ed effetti locali.
    /// </para>
    ///
    /// <para><b>Struttura interna:</b></para>
    /// <list type="bullet">
    ///   <item><b>ZoomLevel</b>: livello zoom risolto.</item>
    ///   <item><b>ActorMode</b>: modalita' visuale actor.</item>
    ///   <item><b>VegetationMode</b>: modalita' visuale vegetazione.</item>
    ///   <item><b>ObjectMode</b>: modalita' visuale oggetti.</item>
    ///   <item><b>EffectMode</b>: modalita' visuale effetti locali.</item>
    ///   <item><b>AllowsSpriteAnimation</b>: animazioni sprite ammesse.</item>
    ///   <item><b>AllowsLayeredActorSprites</b>: vestizione actor a layer ammessa.</item>
    ///   <item><b>UsesSimplifiedRepresentation</b>: rappresentazione semplificata attiva.</item>
    ///   <item><b>ShowMinorItems</b>: oggetti minori visibili.</item>
    ///   <item><b>ShowWeatherOverlay</b>: overlay meteo futuro visibile.</item>
    /// </list>
    /// </summary>
    public readonly struct ArcGraphZoomLodProfile
    {
        public readonly int ZoomLevel;
        public readonly ArcGraphActorLodMode ActorMode;
        public readonly ArcGraphVegetationLodMode VegetationMode;
        public readonly ArcGraphObjectLodMode ObjectMode;
        public readonly ArcGraphEffectLodMode EffectMode;
        public readonly bool AllowsSpriteAnimation;
        public readonly bool AllowsLayeredActorSprites;
        public readonly bool UsesSimplifiedRepresentation;
        public readonly bool ShowMinorItems;
        public readonly bool ShowWeatherOverlay;

        // =============================================================================
        // ArcGraphZoomLodProfile
        // =============================================================================
        /// <summary>
        /// <para>
        /// Costruisce un profilo LOD visuale completo.
        /// </para>
        ///
        /// <para><b>Value object</b></para>
        /// <para>
        /// Il profilo non conserva riferimenti a renderer, sprite, asset o scena.
        /// Dice solo quale grado di dettaglio e' ammesso a un determinato zoom.
        /// </para>
        /// </summary>
        public ArcGraphZoomLodProfile(
            int zoomLevel,
            ArcGraphActorLodMode actorMode,
            ArcGraphVegetationLodMode vegetationMode,
            ArcGraphObjectLodMode objectMode,
            ArcGraphEffectLodMode effectMode,
            bool allowsSpriteAnimation,
            bool allowsLayeredActorSprites,
            bool usesSimplifiedRepresentation,
            bool showMinorItems,
            bool showWeatherOverlay)
        {
            ZoomLevel = zoomLevel > 0 ? zoomLevel : 1;
            ActorMode = actorMode;
            VegetationMode = vegetationMode;
            ObjectMode = objectMode;
            EffectMode = effectMode;
            AllowsSpriteAnimation = allowsSpriteAnimation;
            AllowsLayeredActorSprites = allowsLayeredActorSprites;
            UsesSimplifiedRepresentation = usesSimplifiedRepresentation;
            ShowMinorItems = showMinorItems;
            ShowWeatherOverlay = showWeatherOverlay;
        }
    }
}
