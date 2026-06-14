namespace Arcontio.View.ArcGraph
{
    // =============================================================================
    // ArcGraphTerrainTypeMapper
    // =============================================================================
    /// <summary>
    /// <para>
    /// Mapper provvisorio tra tile sorgente legacy e terrain id semantico ArcGraph.
    /// </para>
    ///
    /// <para><b>Principio architetturale: ponte dichiarato, non euristica nascosta</b></para>
    /// <para>
    /// La mappa attuale arriva ancora da <c>MapGridData</c> e usa tile id grafici.
    /// ArcGraph invece deve convergere verso celle semantiche, per esempio
    /// <c>grass</c>, <c>stone_floor</c> o <c>water</c>. Questo helper concentra la
    /// traduzione temporanea in un solo punto, evitando che renderer, builder e
    /// diagnostiche replichino euristiche diverse.
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
