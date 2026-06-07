namespace Arcontio.View.ArcGraph
{
    // =============================================================================
    // ArcGraphZoomLodPolicy
    // =============================================================================
    /// <summary>
    /// <para>
    /// Risolve il profilo LOD visuale a partire dallo zoom ArcGraph.
    /// </para>
    ///
    /// <para><b>Principio architetturale: nessuno switch LOD disperso nei renderer</b></para>
    /// <para>
    /// La policy concentra le decisioni su cosa mostrare a ogni zoom. I renderer
    /// futuri potranno leggere un <c>ArcGraphZoomLodProfile</c> invece di duplicare
    /// condizioni su livello zoom, animazioni, marker, sprite semplificati e layer
    /// actor.
    /// </para>
    ///
    /// <para><b>Struttura interna:</b></para>
    /// <list type="bullet">
    ///   <item><b>Resolve</b>: risolve il profilo partendo da config e stato view.</item>
    ///   <item><b>ResolveFromZoom</b>: risolve il profilo partendo da una definizione zoom.</item>
    /// </list>
    /// </summary>
    public static class ArcGraphZoomLodPolicy
    {
        // =============================================================================
        // Resolve
        // =============================================================================
        /// <summary>
        /// <para>
        /// Risolve il profilo LOD per lo zoom attivo nello stato view.
        /// </para>
        ///
        /// <para><b>Policy derivata dalla view</b></para>
        /// <para>
        /// Il metodo non legge renderer e non consulta il mondo. Usa solo
        /// configurazione view e zoom corrente, quindi resta testabile e indipendente
        /// dalla scena.
        /// </para>
        /// </summary>
        public static ArcGraphZoomLodProfile Resolve(
            ArcGraphMapViewConfig config,
            ArcGraphViewState state)
        {
            config = config ?? ArcGraphMapViewConfig.CreateDefaultV033();
            state = state ?? ArcGraphViewState.CreateDefault(config);
            return ResolveFromZoom(state.CurrentZoom(config));
        }

        // =============================================================================
        // ResolveFromZoom
        // =============================================================================
        /// <summary>
        /// <para>
        /// Risolve il profilo LOD partendo da una definizione zoom.
        /// </para>
        ///
        /// <para><b>Traduzione dei quattro livelli v0.33</b></para>
        /// <para>
        /// I livelli lontani usano rappresentazione semplificata. Il livello 1 usa
        /// marker strategici e aggregazioni; il livello 2 usa sprite statici
        /// semplificati; il livello 3 usa sprite completi flat; il livello 4 abilita
        /// actor layered e dettagli locali.
        /// </para>
        /// </summary>
        public static ArcGraphZoomLodProfile ResolveFromZoom(
            ArcGraphViewZoomLevelDefinition zoom)
        {
            if (zoom.Level <= 1)
            {
                return new ArcGraphZoomLodProfile(
                    zoom.Level,
                    ArcGraphActorLodMode.StrategicMarker,
                    ArcGraphVegetationLodMode.AreaAggregate,
                    ArcGraphObjectLodMode.HideMinorObjects,
                    ArcGraphEffectLodMode.StaticSignalOnly,
                    allowsSpriteAnimation: false,
                    allowsLayeredActorSprites: false,
                    usesSimplifiedRepresentation: true,
                    showMinorItems: false,
                    showWeatherOverlay: true);
            }

            if (zoom.Level == 2)
            {
                return new ArcGraphZoomLodProfile(
                    zoom.Level,
                    ArcGraphActorLodMode.SimplifiedStaticSprite,
                    ArcGraphVegetationLodMode.SimplifiedStaticSprite,
                    ArcGraphObjectLodMode.SimplifiedImportantObjects,
                    ArcGraphEffectLodMode.SimplifiedStaticEffect,
                    allowsSpriteAnimation: false,
                    allowsLayeredActorSprites: false,
                    usesSimplifiedRepresentation: true,
                    showMinorItems: false,
                    showWeatherOverlay: true);
            }

            if (zoom.Level == 3)
            {
                return new ArcGraphZoomLodProfile(
                    zoom.Level,
                    ArcGraphActorLodMode.FullFlatSprite,
                    zoom.AllowsSpriteAnimation
                        ? ArcGraphVegetationLodMode.IndividualAnimatedSprite
                        : ArcGraphVegetationLodMode.IndividualStaticSprite,
                    ArcGraphObjectLodMode.StaticSprites,
                    ArcGraphEffectLodMode.AnimatedMajorEffects,
                    allowsSpriteAnimation: zoom.AllowsSpriteAnimation,
                    allowsLayeredActorSprites: false,
                    usesSimplifiedRepresentation: false,
                    showMinorItems: true,
                    showWeatherOverlay: true);
            }

            return new ArcGraphZoomLodProfile(
                zoom.Level,
                zoom.AllowsLayeredActorSprites
                    ? ArcGraphActorLodMode.LayeredSprite
                    : ArcGraphActorLodMode.FullFlatSprite,
                zoom.AllowsSpriteAnimation
                    ? ArcGraphVegetationLodMode.IndividualAnimatedSprite
                    : ArcGraphVegetationLodMode.IndividualStaticSprite,
                ArcGraphObjectLodMode.DetailedSprites,
                ArcGraphEffectLodMode.FullLocalEffects,
                allowsSpriteAnimation: zoom.AllowsSpriteAnimation,
                allowsLayeredActorSprites: zoom.AllowsLayeredActorSprites,
                usesSimplifiedRepresentation: false,
                showMinorItems: true,
                showWeatherOverlay: true);
        }
    }
}
