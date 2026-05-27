using Arcontio.Core.Config;

namespace Arcontio.Core.Logging
{
    // =============================================================================
    // RuntimeDiagnosticsLifecycle
    // =============================================================================
    /// <summary>
    /// <para>
    /// Servizio minimale responsabile del ciclo vita della diagnostica runtime
    /// persistente. In questa fase governa soltanto il centro JSONL batchato:
    /// configurazione iniziale, scarico periodico e chiusura difensiva.
    /// </para>
    ///
    /// <para><b>Separazione dal logger legacy</b></para>
    /// <para>
    /// <c>ArcontioLogger</c> resta un ponte temporaneo per le chiamate storiche, ma
    /// non deve piu' possedere file, code o scrittori diagnostici. Questo servizio
    /// tiene isolata la responsabilita' di infrastruttura, preparando la futura
    /// modularizzazione EL senza cambiare comportamento simulativo.
    /// </para>
    ///
    /// <para><b>Struttura interna:</b></para>
    /// <list type="bullet">
    ///   <item><b>InitFromSimulationParams</b>: applica la configurazione JSONL letta dal file portante.</item>
    ///   <item><b>Flush</b>: scarica le code JSONL con la policy gia' esistente.</item>
    ///   <item><b>Shutdown</b>: chiude gli scrittori e libera lo stato runtime.</item>
    /// </list>
    /// </summary>
    public static class RuntimeDiagnosticsLifecycle
    {
        private static bool _initialized;

        public static bool Initialized => _initialized;

        public static void InitFromSimulationParams(SimulationParams simulationParams)
        {
            if (_initialized)
                return;

            var diagnostics = simulationParams?.ResolveLoggerDiagnostics() ?? new LoggerDiagnosticsParams();
            JsonlRuntimeLogHub.Configure(diagnostics.jsonl);
            _initialized = true;
        }

        public static void Flush()
        {
            if (!_initialized)
                return;

            JsonlRuntimeLogHub.FlushAll();
        }

        public static void Shutdown()
        {
            if (!_initialized)
                return;

            try
            {
                JsonlRuntimeLogHub.Shutdown();
            }
            finally
            {
                _initialized = false;
            }
        }
    }
}
