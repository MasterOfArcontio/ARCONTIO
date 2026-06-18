namespace Arcontio.View.ArcGraph
{
    // =============================================================================
    // IArcGraphPlacementPreviewSource
    // =============================================================================
    /// <summary>
    /// <para>
    /// Contratto read-only per una sorgente di preview placement consumabile da
    /// ArcGraph.
    /// </para>
    ///
    /// <para><b>Principio architetturale: placement separato dalla resa ArcGraph</b></para>
    /// <para>
    /// ArcGraph deve visualizzare la preview di inserimento, ma non deve dipendere
    /// dal tipo concreto che gestisce F3, palette, click o comandi. Questo
    /// contratto espone soltanto stato osservabile: se la preview e' attiva, quale
    /// oggetto verrebbe inserito e quale cella e' sotto il puntatore operativo.
    /// La mutazione resta fuori dal renderer.
    /// </para>
    ///
    /// <para><b>Struttura interna:</b></para>
    /// <list type="bullet">
    ///   <item><b>IsObjectPlacementPreviewActive</b>: indica se la preview oggetto e' attiva.</item>
    ///   <item><b>IsPointerOverPlacementUi</b>: indica se la UI del tool blocca la mappa.</item>
    ///   <item><b>TryGetActiveObjectPlacementPreviewDefId</b>: risolve il defId visualizzabile.</item>
    ///   <item><b>TryGetObjectPlacementPreviewCell</b>: risolve la cella target corrente.</item>
    /// </list>
    /// </summary>
    public interface IArcGraphPlacementPreviewSource
    {
        bool IsObjectPlacementPreviewActive { get; }
        bool IsPointerOverPlacementUi { get; }

        bool TryGetActiveObjectPlacementPreviewDefId(out string defId);
        bool TryGetObjectPlacementPreviewCell(out int cellX, out int cellY);
    }
}
