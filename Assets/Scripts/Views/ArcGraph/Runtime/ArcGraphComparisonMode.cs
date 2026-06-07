namespace Arcontio.View.ArcGraph
{
    // =============================================================================
    // ArcGraphComparisonMode
    // =============================================================================
    /// <summary>
    /// <para>
    /// Modalita' controllata per confronto tra ArcGraph e MapGrid legacy.
    /// </para>
    ///
    /// <para><b>Principio architetturale: comparazione esplicita, mai permanente per default</b></para>
    /// <para>
    /// Il confronto tra renderer e' utile in debug, ma pericoloso se diventa un
    /// secondo percorso produttivo stabile. Questa enum distingue il sistema spento,
    /// la sola diagnostica e il futuro probe scena temporaneo.
    /// </para>
    /// </summary>
    public enum ArcGraphComparisonMode
    {
        Disabled = 0,
        DiagnosticsOnly = 1,
        TemporaryDebugSceneProbe = 2
    }
}
