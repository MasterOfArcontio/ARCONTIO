namespace Arcontio.View.ArcGraph
{
    // =============================================================================
    // ArcGraphBootstrapStatus
    // =============================================================================
    /// <summary>
    /// <para>
    /// Stato diagnostico sintetico del lifecycle del bootstrap ArcGraph.
    /// </para>
    ///
    /// <para><b>Principio architetturale: lifecycle leggibile e non implicito</b></para>
    /// <para>
    /// Il bootstrap non usa <c>Awake</c>, <c>Start</c>, coroutine o update nascosti.
    /// Ogni transizione viene quindi descritta da uno stato esplicito, utile per
    /// test, audit e future UI diagnostiche senza trasformare ArcGraph in renderer
    /// automatico.
    /// </para>
    ///
    /// <para><b>Struttura interna:</b></para>
    /// <list type="bullet">
    ///   <item><b>Uninitialized</b>: oggetto creato ma mai acceso.</item>
    ///   <item><b>Disabled</b>: accensione negata dalla policy.</item>
    ///   <item><b>Initializing</b>: transizione interna temporanea.</item>
    ///   <item><b>Initialized</b>: stato interno ArcGraph pronto.</item>
    ///   <item><b>Failed</b>: accensione fallita in modo non distruttivo.</item>
    ///   <item><b>Disposed</b>: cache e layer rilasciati.</item>
    /// </list>
    /// </summary>
    public enum ArcGraphBootstrapStatus
    {
        Uninitialized = 0,
        Disabled = 1,
        Initializing = 2,
        Initialized = 3,
        Failed = 4,
        Disposed = 5
    }
}
