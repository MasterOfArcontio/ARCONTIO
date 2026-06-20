namespace Arcontio.View.ArcGraph
{
    // =============================================================================
    // ArcGraphActorLodMode
    // =============================================================================
    /// <summary>
    /// <para>
    /// Modalita' di rappresentazione actor disponibili per profili visuali futuri.
    /// </para>
    ///
    /// <para><b>Principio architetturale: actor visivo scalabile</b></para>
    /// <para>
    /// La simulazione dell'actor non cambia con la camera. Il renderer puo'
    /// scegliere, in futuro, fra marker diagnostico, sprite completo singolo o
    /// composizione a layer, ma la scelta non arriva piu' da livelli zoom.
    /// </para>
    /// </summary>
    public enum ArcGraphActorLodMode
    {
        StrategicMarker = 0,
        FullFlatSprite = 1,
        LayeredSprite = 2
    }

    // =============================================================================
    // ArcGraphVegetationLodMode
    // =============================================================================
    /// <summary>
    /// <para>
    /// Modalita' di rappresentazione vegetazione disponibili per profili futuri.
    /// </para>
    ///
    /// <para><b>Principio architetturale: aggregazione quando il dettaglio non serve</b></para>
    /// <para>
    /// Le singole piante potranno essere aggregate o disegnate singolarmente da
    /// una policy ambientale esplicita. Questa scelta non e' piu' collegata alla
    /// rotellina della camera.
    /// </para>
    /// </summary>
    public enum ArcGraphVegetationLodMode
    {
        AreaAggregate = 0,
        IndividualStaticSprite = 1,
        IndividualAnimatedSprite = 2
    }

    // =============================================================================
    // ArcGraphObjectLodMode
    // =============================================================================
    /// <summary>
    /// <para>
    /// Modalita' di rappresentazione oggetti e item disponibili per profili futuri.
    /// </para>
    ///
    /// <para><b>Principio architetturale: filtrare il rumore visuale</b></para>
    /// <para>
    /// Gli oggetti minori potranno essere nascosti o mostrati da una policy
    /// esplicita di leggibilita'. Questa enum non pilota asset alternativi.
    /// </para>
    /// </summary>
    public enum ArcGraphObjectLodMode
    {
        HideMinorObjects = 0,
        StaticSprites = 1,
        DetailedSprites = 2
    }

    // =============================================================================
    // ArcGraphEffectLodMode
    // =============================================================================
    /// <summary>
    /// <para>
    /// Modalita' di rappresentazione effetti locali disponibili per profili futuri.
    /// </para>
    ///
    /// <para><b>Principio architetturale: effetti proporzionati alla scala</b></para>
    /// <para>
    /// Un effetto locale puo' essere segnale statico, animazione principale o
    /// effetto completo. La scelta resta visuale e dichiarata, non deriva da
    /// livelli zoom configurati.
    /// </para>
    /// </summary>
    public enum ArcGraphEffectLodMode
    {
        StaticSignalOnly = 0,
        AnimatedMajorEffects = 1,
        FullLocalEffects = 2
    }
}
