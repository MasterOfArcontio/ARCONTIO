namespace Arcontio.View.ArcGraph
{
    // =============================================================================
    // ArcGraphDebugOverlaySpace
    // =============================================================================
    /// <summary>
    /// <para>
    /// Dichiara in quale spazio logico vive un overlay debug.
    /// </para>
    ///
    /// <para><b>Principio architetturale: mappa e HUD non sono lo stesso layer</b></para>
    /// <para>
    /// L'audit <c>v0.37a</c> ha mostrato che MapGrid mescola overlay su celle,
    /// marker/edge sulla mappa, label screen-space, pannelli e strumenti operativi.
    /// Questa enum separa subito i casi: un renderer di mappa puo' ignorare gli HUD,
    /// mentre un renderer UI puo' ignorare celle e linee world-space.
    /// </para>
    ///
    /// <para><b>Struttura interna:</b></para>
    /// <list type="bullet">
    ///   <item><b>MapCell</b>: overlay centrato su una cella.</item>
    ///   <item><b>MapNode</b>: marker ancorato a una cella ma semanticamente nodo.</item>
    ///   <item><b>MapEdge</b>: segmento tra due celle.</item>
    ///   <item><b>ScreenLabel</b>: label ancorata alla mappa ma disegnata in UI.</item>
    ///   <item><b>ScreenHud</b>: pannello HUD non legato direttamente a una cella.</item>
    /// </list>
    /// </summary>
    public enum ArcGraphDebugOverlaySpace
    {
        None = 0,
        MapCell = 1,
        MapNode = 2,
        MapEdge = 3,
        ScreenLabel = 4,
        ScreenHud = 5
    }
}
