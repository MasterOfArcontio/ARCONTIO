namespace Arcontio.View.ArcGraph
{
    // =============================================================================
    // ArcGraphTerrainTileUvDefinition
    // =============================================================================
    /// <summary>
    /// <para>
    /// Definizione compatta del posizionamento di un tile terrain dentro un atlas a
    /// griglia regolare.
    /// </para>
    ///
    /// <para><b>Principio architetturale: dati atlas ricevuti, non caricati</b></para>
    /// <para>
    /// ArcGraph non deve caricare texture o leggere configurazioni globali in modo
    /// implicito. Questa struttura contiene soltanto i tre valori necessari a
    /// registrare una tile: id visuale, colonna atlas e riga atlas. Il chiamante puo'
    /// costruirla leggendo configurazioni MapGrid, asset futuri o dati test, ma la
    /// UV map ArcGraph resta autonoma.
    /// </para>
    ///
    /// <para><b>Struttura interna:</b></para>
    /// <list type="bullet">
    ///   <item><b>TileId</b>: identificatore visuale del tile.</item>
    ///   <item><b>UvX</b>: colonna del tile nell'atlas.</item>
    ///   <item><b>UvY</b>: riga del tile nell'atlas con origine in alto.</item>
    /// </list>
    /// </summary>
    public readonly struct ArcGraphTerrainTileUvDefinition
    {
        public readonly int TileId;
        public readonly int UvX;
        public readonly int UvY;

        // =============================================================================
        // ArcGraphTerrainTileUvDefinition
        // =============================================================================
        /// <summary>
        /// <para>
        /// Crea una definizione UV terrain.
        /// </para>
        ///
        /// <para><b>Compatibilita' con MapGridConfig.TileDef</b></para>
        /// <para>
        /// I campi replicano la semantica minima di <c>MapGridConfig.TileDef</c>
        /// senza dipendere da quel DTO. Questo permette ad ArcGraph di riusare la
        /// convenzione legacy senza legarsi in modo permanente al tipo MapGrid.
        /// </para>
        /// </summary>
        public ArcGraphTerrainTileUvDefinition(int tileId, int uvX, int uvY)
        {
            TileId = tileId;
            UvX = uvX;
            UvY = uvY;
        }
    }
}
