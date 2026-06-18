namespace Arcontio.View.ArcGraph
{
    // =============================================================================
    // ArcGraphDebugOverlayKind
    // =============================================================================
    /// <summary>
    /// <para>
    /// Vocabolario value-only dei contenuti diagnostici che ArcGraph puo' trasportare
    /// senza dipendere dal vecchio renderer <c>MapGrid</c>.
    /// </para>
    ///
    /// <para><b>Principio architetturale: diagnostica dichiarativa</b></para>
    /// <para>
    /// Questa enum non decide come disegnare un overlay. Serve solo a nominare il
    /// significato del dato: una cella FOV osservata non e' una cella DT, un edge
    /// route non e' un edge del grafo mondo, una label landmark non e' un pannello
    /// HUD. Il renderer Unity futuro potra' tradurre questi valori in colori,
    /// sprite, linee o UI, ma la decisione resta fuori dal contratto dati.
    /// </para>
    ///
    /// <para><b>Struttura interna:</b></para>
    /// <list type="bullet">
    ///   <item><b>Fov*</b>: celle legate al debug percettivo.</item>
    ///   <item><b>Landmark*</b>: nodi, edge e label del debug landmark/pathfinding.</item>
    ///   <item><b>Dt*/Gvd*</b>: celle e strutture del debug GVD-DIN.</item>
    ///   <item><b>Pointer/Hud</b>: dati screen-space, separati dagli overlay di mappa.</item>
    /// </list>
    /// </summary>
    public enum ArcGraphDebugOverlayKind
    {
        None = 0,

        FovObservedCell = 10,
        FovWatchedMarginCell = 11,
        FovHistoricalHeatCell = 12,

        LandmarkWorldNode = 30,
        LandmarkKnownNode = 31,
        LandmarkRouteNode = 32,
        LandmarkGvdNode = 33,
        LandmarkBiologicalNode = 34,

        LandmarkWorldEdge = 40,
        LandmarkKnownEdge = 41,
        LandmarkRouteEdge = 42,
        LandmarkLmPathEdge = 43,
        LandmarkDirectPathEdge = 44,
        LandmarkJumpPathEdge = 45,
        LandmarkComplexEdge = 46,
        LandmarkGvdEdge = 47,

        DtHeatCell = 60,
        DtValueLabel = 61,
        GvdRawCell = 62,

        LandmarkLabel = 80,
        PointerCellCoordsHud = 90,
        RuntimeCostHud = 91
    }
}
