namespace Arcontio.View.ArcGraph
{
    // =============================================================================
    // ArcGraphInteractionTargetKind
    // =============================================================================
    /// <summary>
    /// <para>
    /// Vocabolario minimo del tipo di bersaglio view-side risolto dal boundary
    /// interattivo ArcGraph.
    /// </para>
    ///
    /// <para><b>Principio architetturale: il renderer espone il picking, non possiede i tool</b></para>
    /// <para>
    /// Questa enum separa il dato di interazione dalla logica che lo usera'. Un click
    /// su un actor, su un oggetto o su una cella non deve attivare direttamente
    /// DevTools, selection, pannelli o comandi. Il renderer e il suo adapter possono
    /// solo dire "che cosa e' sotto il puntatore"; saranno moduli esterni a decidere
    /// cosa farne.
    /// </para>
    ///
    /// <para><b>Struttura interna:</b></para>
    /// <list type="bullet">
    ///   <item><b>None</b>: nessun bersaglio valido.</item>
    ///   <item><b>UiBlocked</b>: la UI ha priorita' e blocca la mappa.</item>
    ///   <item><b>Cell</b>: cella valida senza actor o oggetto prioritario.</item>
    ///   <item><b>Actor</b>: actor visibile risolto sulla cella.</item>
    ///   <item><b>Object</b>: oggetto visibile risolto sulla cella.</item>
    ///   <item><b>Plant</b>: pianta fisica visuale risolta sulla cella.</item>
    /// </list>
    /// </summary>
    public enum ArcGraphInteractionTargetKind
    {
        None = 0,
        UiBlocked = 1,
        Cell = 2,
        Actor = 3,
        Object = 4,
        Plant = 5
    }
}
