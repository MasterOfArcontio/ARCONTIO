namespace Arcontio.View.ArcGraph
{
    // =============================================================================
    // ArcGraphTerrainCellSnapshot
    // =============================================================================
    /// <summary>
    /// <para>
    /// Snapshot read-only minimo di una cella terreno osservabile da <c>arcgraph</c>.
    /// </para>
    ///
    /// <para><b>Principio architetturale: view-data derivato, non mappa autoritativa</b></para>
    /// <para>
    /// Il terreno visuale corrente arriva da <c>MapGridData</c>, che e' gia' un buffer
    /// di presentazione e non il <c>World</c>. Questo snapshot copia soltanto il dato
    /// necessario ai futuri layer grafici: coordinata, tile id e flag bloccato. Non
    /// permette di modificare la mappa, non contiene riferimenti a mesh Unity e non
    /// introduce una seconda sorgente di verita' simulativa.
    /// </para>
    ///
    /// <para><b>Struttura interna:</b></para>
    /// <list type="bullet">
    ///   <item><b>Cell</b>: coordinata discreta <c>x/y/z</c> della cella.</item>
    ///   <item><b>TileId</b>: identificatore visuale del tile terreno corrente.</item>
    ///   <item><b>IsBlocked</b>: flag view-side ereditato dalla MapGrid corrente.</item>
    /// </list>
    /// </summary>
    public readonly struct ArcGraphTerrainCellSnapshot
    {
        public readonly ArcGraphCellCoord Cell;
        public readonly int TileId;
        public readonly bool IsBlocked;

        // =============================================================================
        // ArcGraphTerrainCellSnapshot
        // =============================================================================
        /// <summary>
        /// <para>
        /// Costruisce una copia compatta del terreno visuale di una singola cella.
        /// </para>
        ///
        /// <para><b>Confine read-only</b></para>
        /// <para>
        /// Il costruttore riceve solo valori primitivi o value type. Chi consuma lo
        /// snapshot puo' disegnare, ordinare o marcare dirty la cella, ma non puo'
        /// usare questo oggetto per cambiare <c>MapGridData</c> o il <c>World</c>.
        /// </para>
        ///
        /// <para><b>Struttura interna:</b></para>
        /// <list type="bullet">
        ///   <item><b>cell</b>: coordinata sorgente.</item>
        ///   <item><b>tileId</b>: tile visuale copiato.</item>
        ///   <item><b>isBlocked</b>: occupazione/blocco visuale copiato.</item>
        /// </list>
        /// </summary>
        public ArcGraphTerrainCellSnapshot(ArcGraphCellCoord cell, int tileId, bool isBlocked)
        {
            Cell = cell;
            TileId = tileId;
            IsBlocked = isBlocked;
        }
    }
}
