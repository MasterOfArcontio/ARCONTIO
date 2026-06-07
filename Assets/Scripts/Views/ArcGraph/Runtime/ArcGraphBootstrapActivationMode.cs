namespace Arcontio.View.ArcGraph
{
    // =============================================================================
    // ArcGraphBootstrapActivationMode
    // =============================================================================
    /// <summary>
    /// <para>
    /// Modalita' di attivazione consentite per il bootstrap controllato di
    /// <c>arcgraph</c>.
    /// </para>
    ///
    /// <para><b>Principio architetturale: accensione esplicita senza rendering produttivo</b></para>
    /// <para>
    /// In <c>v0.31</c> ArcGraph puo' solo accendersi internamente. Non puo'
    /// renderizzare terreno, oggetti, attori, meteo o overlay e non puo'
    /// sostituire la MapGrid. Questo enum rende esplicito il fatto che il bootstrap
    /// non ha ancora modalita' produttive di resa visiva.
    /// </para>
    ///
    /// <para><b>Struttura interna:</b></para>
    /// <list type="bullet">
    ///   <item><b>Disabled</b>: il bootstrap viene richiesto ma resta spento.</item>
    ///   <item><b>InternalStateOnly</b>: inizializza solo stato, layer passivi e cache interne.</item>
    /// </list>
    /// </summary>
    public enum ArcGraphBootstrapActivationMode
    {
        Disabled = 0,
        InternalStateOnly = 1
    }
}
