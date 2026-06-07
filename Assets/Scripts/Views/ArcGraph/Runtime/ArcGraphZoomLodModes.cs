namespace Arcontio.View.ArcGraph
{
    // =============================================================================
    // ArcGraphActorLodMode
    // =============================================================================
    /// <summary>
    /// <para>
    /// Modalita' di rappresentazione actor in funzione dello zoom.
    /// </para>
    ///
    /// <para><b>Principio architetturale: actor visivo scalabile</b></para>
    /// <para>
    /// La simulazione dell'actor non cambia con lo zoom. Cambia solo il modo in cui
    /// il renderer futuro lo mostra: marker strategico, sagoma semplificata, sprite
    /// completo singolo o composizione a layer.
    /// </para>
    /// </summary>
    public enum ArcGraphActorLodMode
    {
        StrategicMarker = 0,
        SimplifiedStaticSprite = 1,
        FullFlatSprite = 2,
        LayeredSprite = 3
    }

    // =============================================================================
    // ArcGraphVegetationLodMode
    // =============================================================================
    /// <summary>
    /// <para>
    /// Modalita' di rappresentazione vegetazione in funzione dello zoom.
    /// </para>
    ///
    /// <para><b>Principio architetturale: aggregazione quando il dettaglio non serve</b></para>
    /// <para>
    /// Le singole piante non devono necessariamente essere disegnate a tutti gli
    /// zoom. Ai livelli lontani conviene rappresentare masse o aree, riducendo il
    /// costo visuale e migliorando leggibilita'.
    /// </para>
    /// </summary>
    public enum ArcGraphVegetationLodMode
    {
        AreaAggregate = 0,
        SimplifiedStaticSprite = 1,
        IndividualStaticSprite = 2,
        IndividualAnimatedSprite = 3
    }

    // =============================================================================
    // ArcGraphObjectLodMode
    // =============================================================================
    /// <summary>
    /// <para>
    /// Modalita' di rappresentazione oggetti e item in funzione dello zoom.
    /// </para>
    ///
    /// <para><b>Principio architetturale: filtrare il rumore visuale</b></para>
    /// <para>
    /// Gli oggetti minori possono essere nascosti agli zoom lontani. Le strutture o
    /// gli oggetti importanti restano invece leggibili con rappresentazioni piu'
    /// semplici.
    /// </para>
    /// </summary>
    public enum ArcGraphObjectLodMode
    {
        HideMinorObjects = 0,
        SimplifiedImportantObjects = 1,
        StaticSprites = 2,
        DetailedSprites = 3
    }

    // =============================================================================
    // ArcGraphEffectLodMode
    // =============================================================================
    /// <summary>
    /// <para>
    /// Modalita' di rappresentazione effetti locali in funzione dello zoom.
    /// </para>
    ///
    /// <para><b>Principio architetturale: effetti proporzionati alla scala</b></para>
    /// <para>
    /// Un incendio visto da lontano puo' essere un indicatore statico; visto da
    /// vicino puo' diventare fiamma animata con fumo. Questa enum rende esplicita
    /// la scelta senza anticipare il renderer effetti.
    /// </para>
    /// </summary>
    public enum ArcGraphEffectLodMode
    {
        StaticSignalOnly = 0,
        SimplifiedStaticEffect = 1,
        AnimatedMajorEffects = 2,
        FullLocalEffects = 3
    }
}
