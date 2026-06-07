namespace Arcontio.View.ArcGraph
{
    // =============================================================================
    // ArcGraphTerrainVisualPolicy
    // =============================================================================
    /// <summary>
    /// <para>
    /// Policy visuale per decidere quale tile atlas disegnare partendo da uno
    /// snapshot terrain ArcGraph.
    /// </para>
    ///
    /// <para><b>Principio architetturale: presentazione separata dal dato terrain</b></para>
    /// <para>
    /// Lo snapshot terrain contiene <c>TileId</c> e <c>IsBlocked</c>, ma il renderer
    /// puo' dover applicare una policy visuale per replicare il look legacy:
    /// varianti pavimento deterministiche, muro pieno e muro con top. Questa policy
    /// resta puramente grafica e non decide pathfinding, collisioni o blocchi reali.
    /// </para>
    ///
    /// <para><b>Struttura interna:</b></para>
    /// <list type="bullet">
    ///   <item><b>FloorBaseTileId</b>: primo tile pavimento nella serie variante.</item>
    ///   <item><b>FloorVariantCount</b>: numero varianti contigue del pavimento.</item>
    ///   <item><b>WallTileId</b>: tile muro pieno.</item>
    ///   <item><b>WallTopTileId</b>: tile muro con top visibile.</item>
    ///   <item><b>UseLegacyFloorVariants</b>: replica la policy MapGrid corrente.</item>
    /// </list>
    /// </summary>
    public readonly struct ArcGraphTerrainVisualPolicy
    {
        public readonly int FloorBaseTileId;
        public readonly int FloorVariantCount;
        public readonly int WallTileId;
        public readonly int WallTopTileId;
        public readonly bool UseLegacyFloorVariants;

        // =============================================================================
        // ArcGraphTerrainVisualPolicy
        // =============================================================================
        /// <summary>
        /// <para>
        /// Costruisce una policy visuale terrain.
        /// </para>
        ///
        /// <para><b>Normalizzazione dei valori</b></para>
        /// <para>
        /// Il numero di varianti pavimento viene normalizzato ad almeno uno per
        /// evitare modulo per zero. Gli id tile restano valori dichiarati dal
        /// chiamante, perche' il renderer non possiede il catalogo atlas.
        /// </para>
        /// </summary>
        public ArcGraphTerrainVisualPolicy(
            int floorBaseTileId,
            int floorVariantCount,
            int wallTileId,
            int wallTopTileId,
            bool useLegacyFloorVariants = true)
        {
            FloorBaseTileId = floorBaseTileId;
            FloorVariantCount = floorVariantCount > 0 ? floorVariantCount : 1;
            WallTileId = wallTileId;
            WallTopTileId = wallTopTileId;
            UseLegacyFloorVariants = useLegacyFloorVariants;
        }

        // =============================================================================
        // CreateLegacyDefault
        // =============================================================================
        /// <summary>
        /// <para>
        /// Crea la policy default compatibile con il bootstrap MapGrid corrente.
        /// </para>
        ///
        /// <para><b>Compatibilita' iniziale</b></para>
        /// <para>
        /// I valori replicano le costanti usate oggi da <c>MapGridBootstrap</c>:
        /// floor base 0, quattro varianti, muro 10 e wall-top 11. Questa scelta
        /// serve solo per allineare la prima resa terrain; non definisce ancora il
        /// futuro catalogo terrain definitivo.
        /// </para>
        /// </summary>
        public static ArcGraphTerrainVisualPolicy CreateLegacyDefault()
        {
            return new ArcGraphTerrainVisualPolicy(
                floorBaseTileId: 0,
                floorVariantCount: 4,
                wallTileId: 10,
                wallTopTileId: 11);
        }
    }
}
