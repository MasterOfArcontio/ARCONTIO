namespace Arcontio.View.ArcGraph
{
    // =============================================================================
    // ArcGraphTerrainTypeMapper
    // =============================================================================
    /// <summary>
    /// <para>
    /// Mapper provvisorio tra superficie Core, tile sorgente legacy e terrain id
    /// semantico ArcGraph.
    /// </para>
    ///
    /// <para><b>Principio architetturale: ponte dichiarato, non euristica nascosta</b></para>
    /// <para>
    /// La mappa storica arriva ancora da <c>MapGridData</c> e usa tile id grafici.
    /// Il percorso nuovo passa invece da <c>World.CellSurfaces</c>, che espone
    /// chiavi semantiche come <c>grass</c>, <c>stone_floor</c> o <c>water</c>.
    /// Questo helper concentra la transizione in un solo punto, evitando che
    /// renderer, builder e diagnostiche replichino euristiche diverse.
    /// </para>
    ///
    /// <para><b>Struttura interna:</b></para>
    /// <list type="bullet">
    ///   <item><b>ResolveTemporaryTerrainId</b>: traduce una cella snapshot legacy in terrain id.</item>
    ///   <item><b>ResolveTemporaryTerrainIdFromTile</b>: traduce direttamente un tile id sorgente.</item>
    /// </list>
    /// </summary>
    public static class ArcGraphTerrainTypeMapper
    {
        // =============================================================================
        // ResolveTemporaryTerrainId
        // =============================================================================
        /// <summary>
        /// <para>
        /// Risolve il terrain id provvisorio partendo dallo snapshot terrain.
        /// </para>
        /// </summary>
        public static string ResolveTemporaryTerrainId(ArcGraphTerrainCellSnapshot snapshot)
        {
            if (snapshot.HasAuthoritativeSurface)
            {
                if (!string.IsNullOrWhiteSpace(snapshot.VisualRuleKey))
                    return snapshot.VisualRuleKey;

                if (!string.IsNullOrWhiteSpace(snapshot.SurfaceKey))
                    return snapshot.SurfaceKey;
            }

            return ResolveTemporaryTerrainIdFromTile(snapshot.TileId);
        }

        // =============================================================================
        // ResolveTemporaryTerrainIdFromTile
        // =============================================================================
        /// <summary>
        /// <para>
        /// Risolve il terrain id provvisorio partendo dal tile id legacy.
        /// </para>
        /// </summary>
        public static string ResolveTemporaryTerrainIdFromTile(int sourceTileId)
        {
            if (sourceTileId >= 30 && sourceTileId <= 33)
                return "water";

            if (sourceTileId >= 40 && sourceTileId <= 42)
                return "tile_floor";

            if (sourceTileId >= 10 && sourceTileId <= 12)
                return "stone_floor";

            return "grass";
        }
    }
}
