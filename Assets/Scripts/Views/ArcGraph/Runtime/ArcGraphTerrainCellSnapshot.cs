using Arcontio.Core;

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
    /// Il terreno visuale storico arriva da <c>MapGridData</c>, ma il percorso nuovo
    /// deve convergere verso <c>World.CellSurfaces</c>. Per questo lo snapshot mantiene
    /// il <c>TileId</c> legacy come compatibilita' temporanea e aggiunge, quando
    /// disponibile, la superficie Core autoritativa. Non permette di modificare la
    /// mappa, non contiene riferimenti a mesh Unity e non introduce una seconda
    /// sorgente di verita' simulativa.
    /// </para>
    ///
    /// <para><b>Struttura interna:</b></para>
    /// <list type="bullet">
    ///   <item><b>Cell</b>: coordinata discreta <c>x/y/z</c> della cella.</item>
    ///   <item><b>TileId</b>: identificatore visuale legacy, usato solo come fallback.</item>
    ///   <item><b>IsBlocked</b>: flag view-side ereditato dalla MapGrid corrente.</item>
    ///   <item><b>SurfaceKey</b>: chiave semantica Core, per esempio grass o water.</item>
    ///   <item><b>VisualRuleKey</b>: chiave visuale Core opzionale.</item>
    ///   <item><b>HasAuthoritativeSurface</b>: true se lo snapshot viene dal layer Core popolato.</item>
    /// </list>
    /// </summary>
    public readonly struct ArcGraphTerrainCellSnapshot
    {
        public readonly ArcGraphCellCoord Cell;
        public readonly int TileId;
        public readonly bool IsBlocked;
        public readonly CellSurfaceMacro SurfaceMacro;
        public readonly string SurfaceKey;
        public readonly string VisualRuleKey;
        public readonly bool HasAuthoritativeSurface;

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
            SurfaceMacro = CellSurfaceMacro.Natural;
            SurfaceKey = string.Empty;
            VisualRuleKey = string.Empty;
            HasAuthoritativeSurface = false;
        }

        // =============================================================================
        // ArcGraphTerrainCellSnapshot
        // =============================================================================
        /// <summary>
        /// <para>
        /// Costruisce una cella terrain usando anche la superficie Core autoritativa.
        /// </para>
        ///
        /// <para><b>Transizione MapGridData -> CellSurfaceLayer</b></para>
        /// <para>
        /// Il <c>tileId</c> resta presente per compatibilita' e diagnostica, ma quando
        /// <c>hasAuthoritativeSurface</c> e' true i mapper ArcGraph devono preferire
        /// <c>visualRuleKey</c> o <c>surfaceKey</c>. Questo permette di disaccoppiare
        /// gradualmente il rendering dal vecchio id grafico MapGrid.
        /// </para>
        /// </summary>
        public ArcGraphTerrainCellSnapshot(
            ArcGraphCellCoord cell,
            int tileId,
            bool isBlocked,
            CellSurfaceMacro surfaceMacro,
            string surfaceKey,
            string visualRuleKey,
            bool hasAuthoritativeSurface)
        {
            Cell = cell;
            TileId = tileId;
            IsBlocked = isBlocked;
            SurfaceMacro = surfaceMacro;
            SurfaceKey = string.IsNullOrWhiteSpace(surfaceKey)
                ? string.Empty
                : surfaceKey;
            VisualRuleKey = string.IsNullOrWhiteSpace(visualRuleKey)
                ? SurfaceKey
                : visualRuleKey;
            HasAuthoritativeSurface = hasAuthoritativeSurface && !string.IsNullOrWhiteSpace(SurfaceKey);
        }
    }
}
