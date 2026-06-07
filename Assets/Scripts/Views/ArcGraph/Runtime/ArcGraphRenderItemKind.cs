namespace Arcontio.View.ArcGraph
{
    // =============================================================================
    // ArcGraphRenderItemKind
    // =============================================================================
    /// <summary>
    /// <para>
    /// Categoria minima di un item renderizzabile prodotto da ArcGraph.
    /// </para>
    ///
    /// <para><b>Principio architetturale: render item senza scene authority</b></para>
    /// <para>
    /// Questa enum non identifica un componente Unity e non decide quale renderer
    /// concreto usera' l'item. Serve solo a distinguere in modo value-only se una
    /// futura render queue sta trasportando un actor, un oggetto o un elemento
    /// ambientale preparatorio.
    /// </para>
    ///
    /// <para><b>Struttura interna:</b></para>
    /// <list type="bullet">
    ///   <item><b>None</b>: valore nullo difensivo.</item>
    ///   <item><b>Actor</b>: item derivato da <c>ArcGraphActorLayer</c>.</item>
    ///   <item><b>Object</b>: item derivato da <c>ArcGraphObjectLayer</c>.</item>
    ///   <item><b>Vegetation</b>: item derivato da <c>ArcGraphVegetationLayer</c>.</item>
    /// </list>
    /// </summary>
    public enum ArcGraphRenderItemKind
    {
        None = 0,
        Actor = 1,
        Object = 2,
        Vegetation = 3
    }
}
