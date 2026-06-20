namespace Arcontio.View.ArcGraph
{
    // =============================================================================
    // ArcGraphZoomLodProfile
    // =============================================================================
    /// <summary>
    /// <para>
    /// Profilo di dettaglio visuale ArcGraph sempre completo.
    /// </para>
    ///
    /// <para><b>Principio architetturale: LOD scollegato dallo zoom</b></para>
    /// <para>
    /// Lo zoom camera non decide piu' rappresentazioni alternative. Questo profilo
    /// conserva la forma attesa dai builder esistenti, ma dichiara sempre rendering
    /// dettagliato, animazioni abilitate e actor layered abilitati.
    /// </para>
    /// </summary>
    public readonly struct ArcGraphZoomLodProfile
    {
        public readonly ArcGraphActorLodMode ActorMode;
        public readonly ArcGraphVegetationLodMode VegetationMode;
        public readonly ArcGraphObjectLodMode ObjectMode;
        public readonly ArcGraphEffectLodMode EffectMode;
        public readonly bool AllowsSpriteAnimation;
        public readonly bool AllowsLayeredActorSprites;
        public readonly bool ShowMinorItems;
        public readonly bool ShowWeatherOverlay;

        public ArcGraphZoomLodProfile(
            ArcGraphActorLodMode actorMode,
            ArcGraphVegetationLodMode vegetationMode,
            ArcGraphObjectLodMode objectMode,
            ArcGraphEffectLodMode effectMode,
            bool allowsSpriteAnimation,
            bool allowsLayeredActorSprites,
            bool showMinorItems,
            bool showWeatherOverlay)
        {
            ActorMode = actorMode;
            VegetationMode = vegetationMode;
            ObjectMode = objectMode;
            EffectMode = effectMode;
            AllowsSpriteAnimation = allowsSpriteAnimation;
            AllowsLayeredActorSprites = allowsLayeredActorSprites;
            ShowMinorItems = showMinorItems;
            ShowWeatherOverlay = showWeatherOverlay;
        }

        public static ArcGraphZoomLodProfile CreateFullDetail()
        {
            return new ArcGraphZoomLodProfile(
                ArcGraphActorLodMode.LayeredSprite,
                ArcGraphVegetationLodMode.IndividualAnimatedSprite,
                ArcGraphObjectLodMode.DetailedSprites,
                ArcGraphEffectLodMode.FullLocalEffects,
                allowsSpriteAnimation: true,
                allowsLayeredActorSprites: true,
                showMinorItems: true,
                showWeatherOverlay: true);
        }
    }
}
