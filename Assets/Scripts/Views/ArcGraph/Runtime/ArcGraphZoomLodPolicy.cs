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
    /// condizioni su animazioni, marker, sprite semplificati e layer actor. Il
    /// numero dei livelli e le caratteristiche di ogni livello non sono fissati
    /// nel codice: arrivano dalla configurazione view, normalmente caricata da
    /// <c>ArcGraphViewConfig.json</c>.
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
        /// <para><b>Policy guidata dalla configurazione</b></para>
        /// <para>
        /// Questo metodo non assume piu' che esistano quattro livelli. Interpreta
        /// solo i flag della definizione zoom ricevuta: rappresentazione
        /// semplificata, pan ammesso, animazioni sprite e actor a layer. In questo
        /// modo il JSON puo' dichiarare uno, cinque, dieci o piu' livelli senza
        /// richiedere nuove condizioni numeriche dentro la policy.
        /// </para>
        /// </summary>
        public static ArcGraphZoomLodProfile ResolveFromZoom(
            ArcGraphViewZoomLevelDefinition zoom)
        {
            bool usesSimplifiedRepresentation = zoom.UsesSimplifiedRepresentation;
            bool allowsSpriteAnimation =
                !usesSimplifiedRepresentation &&
                zoom.AllowsSpriteAnimation;
            bool allowsLayeredActorSprites =
                !usesSimplifiedRepresentation &&
                zoom.AllowsLayeredActorSprites;

            return new ArcGraphZoomLodProfile(
                zoom.Level,
                ResolveActorMode(zoom, usesSimplifiedRepresentation, allowsLayeredActorSprites),
                ResolveVegetationMode(zoom, usesSimplifiedRepresentation, allowsSpriteAnimation),
                ResolveObjectMode(zoom, usesSimplifiedRepresentation, allowsLayeredActorSprites),
                ResolveEffectMode(zoom, usesSimplifiedRepresentation, allowsSpriteAnimation, allowsLayeredActorSprites),
                allowsSpriteAnimation,
                allowsLayeredActorSprites,
                usesSimplifiedRepresentation,
                showMinorItems: !usesSimplifiedRepresentation,
                showWeatherOverlay: true);
        }

        private static ArcGraphActorLodMode ResolveActorMode(
            ArcGraphViewZoomLevelDefinition zoom,
            bool usesSimplifiedRepresentation,
            bool allowsLayeredActorSprites)
        {
            // Se il JSON dichiara rappresentazione semplificata, gli actor non
            // entrano nella catena completa. Un livello senza pan viene trattato
            // come vista strategica/aggregata; un livello pannabile mostra sprite
            // semplificati statici.
            if (usesSimplifiedRepresentation)
                return zoom.AllowsPan
                    ? ArcGraphActorLodMode.SimplifiedStaticSprite
                    : ArcGraphActorLodMode.StrategicMarker;

            return allowsLayeredActorSprites
                ? ArcGraphActorLodMode.LayeredSprite
                : ArcGraphActorLodMode.FullFlatSprite;
        }

        private static ArcGraphVegetationLodMode ResolveVegetationMode(
            ArcGraphViewZoomLevelDefinition zoom,
            bool usesSimplifiedRepresentation,
            bool allowsSpriteAnimation)
        {
            // La vegetazione lontana puo' diventare aggregata o semplificata. Il
            // dettaglio individuale entra solo quando il JSON spegne la
            // rappresentazione semplificata.
            if (usesSimplifiedRepresentation)
                return zoom.AllowsPan
                    ? ArcGraphVegetationLodMode.SimplifiedStaticSprite
                    : ArcGraphVegetationLodMode.AreaAggregate;

            return allowsSpriteAnimation
                ? ArcGraphVegetationLodMode.IndividualAnimatedSprite
                : ArcGraphVegetationLodMode.IndividualStaticSprite;
        }

        private static ArcGraphObjectLodMode ResolveObjectMode(
            ArcGraphViewZoomLevelDefinition zoom,
            bool usesSimplifiedRepresentation,
            bool allowsLayeredActorSprites)
        {
            // Gli oggetti minori restano nascosti nelle viste strategiche; quando
            // il livello e' pannabile ma semplificato mostriamo solo oggetti
            // importanti. Il dettaglio completo arriva dai flag di dettaglio del
            // livello, non dal numero del livello.
            if (usesSimplifiedRepresentation)
                return zoom.AllowsPan
                    ? ArcGraphObjectLodMode.SimplifiedImportantObjects
                    : ArcGraphObjectLodMode.HideMinorObjects;

            return allowsLayeredActorSprites
                ? ArcGraphObjectLodMode.DetailedSprites
                : ArcGraphObjectLodMode.StaticSprites;
        }

        private static ArcGraphEffectLodMode ResolveEffectMode(
            ArcGraphViewZoomLevelDefinition zoom,
            bool usesSimplifiedRepresentation,
            bool allowsSpriteAnimation,
            bool allowsLayeredActorSprites)
        {
            // Gli effetti seguono la stessa idea: segnale statico se il livello e'
            // strategico, effetto semplificato se il livello e' ancora LOD basso,
            // effetto animato o locale se il JSON abilita dettaglio e animazioni.
            if (usesSimplifiedRepresentation)
                return zoom.AllowsPan
                    ? ArcGraphEffectLodMode.SimplifiedStaticEffect
                    : ArcGraphEffectLodMode.StaticSignalOnly;

            if (!allowsSpriteAnimation)
                return ArcGraphEffectLodMode.SimplifiedStaticEffect;

            return allowsLayeredActorSprites
                ? ArcGraphEffectLodMode.FullLocalEffects
                : ArcGraphEffectLodMode.AnimatedMajorEffects;
        }
    }
}
