namespace Arcontio.View.ArcGraph
{
    // =============================================================================
    // ArcGraphTerrainVisualBuildOptions
    // =============================================================================
    /// <summary>
    /// <para>
    /// Opzioni passive per collegare il catalogo visuale terrain al mesh builder.
    /// </para>
    ///
    /// <para><b>Principio architetturale: resolver opzionale, fallback legacy stabile</b></para>
    /// <para>
    /// Il terrain builder deve poter usare il nuovo catalogo visuale quando e'
    /// disponibile, ma non deve dipendere da esso per funzionare. Questa struttura
    /// trasporta quindi un catalogo opzionale e un tempo visuale opzionale: se il
    /// catalogo manca, il builder continua a usare la policy legacy gia' validata.
    /// </para>
    ///
    /// <para><b>Struttura interna:</b></para>
    /// <list type="bullet">
    ///   <item><b>VisualCatalog</b>: catalogo terrain visuale opzionale.</item>
    ///   <item><b>VisualTimeSeconds</b>: tempo visuale per frame animati, non tick simulativo.</item>
    ///   <item><b>UseVisualResolver</b>: gate derivato dalla presenza del catalogo.</item>
    /// </list>
    /// </summary>
    public readonly struct ArcGraphTerrainVisualBuildOptions
    {
        public readonly ArcGraphTerrainVisualCatalog VisualCatalog;
        public readonly float VisualTimeSeconds;

        public bool UseVisualResolver => VisualCatalog != null;

        // =============================================================================
        // ArcGraphTerrainVisualBuildOptions
        // =============================================================================
        /// <summary>
        /// <para>
        /// Costruisce opzioni visuali normalizzando il tempo visuale.
        /// </para>
        /// </summary>
        public ArcGraphTerrainVisualBuildOptions(
            ArcGraphTerrainVisualCatalog visualCatalog,
            float visualTimeSeconds)
        {
            VisualCatalog = visualCatalog;
            VisualTimeSeconds = visualTimeSeconds < 0f ? 0f : visualTimeSeconds;
        }

        // =============================================================================
        // CreateLegacyOnly
        // =============================================================================
        /// <summary>
        /// <para>
        /// Crea opzioni che disabilitano il resolver visuale e mantengono il legacy.
        /// </para>
        /// </summary>
        public static ArcGraphTerrainVisualBuildOptions CreateLegacyOnly()
        {
            return new ArcGraphTerrainVisualBuildOptions(null, 0f);
        }

        // =============================================================================
        // CreateWithCatalog
        // =============================================================================
        /// <summary>
        /// <para>
        /// Crea opzioni che abilitano il resolver quando il catalogo e' valido.
        /// </para>
        /// </summary>
        public static ArcGraphTerrainVisualBuildOptions CreateWithCatalog(
            ArcGraphTerrainVisualCatalog visualCatalog,
            float visualTimeSeconds)
        {
            return new ArcGraphTerrainVisualBuildOptions(visualCatalog, visualTimeSeconds);
        }
    }
}
