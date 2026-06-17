namespace Arcontio.Core.Environment
{
    // =============================================================================
    // EnvironmentProtectedTestHarnessResult
    // =============================================================================
    /// <summary>
    /// <para>
    /// Risultato compatto dello smoke test protetto della biosfera.
    /// </para>
    ///
    /// <para><b>Principio architetturale: verifica data-only senza scene Unity</b></para>
    /// <para>
    /// Il risultato riassume se il driver protetto riesce a bootstrapare, avanzare
    /// giorno/mese/stagione e produrre snapshot read-only. Non richiede GameObject,
    /// scene, renderer o collegamenti con sistemi runtime non ancora stabilizzati.
    /// </para>
    ///
    /// <para><b>Struttura interna:</b></para>
    /// <list type="bullet">
    ///   <item><b>Passed</b>: esito aggregato.</item>
    ///   <item><b>Message</b>: diagnostica leggibile.</item>
    ///   <item><b>FinalSnapshot</b>: full snapshot finale per debug manuale.</item>
    /// </list>
    /// </summary>
    public sealed class EnvironmentProtectedTestHarnessResult
    {
        public bool Passed { get; }
        public string Message { get; }
        public EnvironmentFullSnapshot FinalSnapshot { get; }

        // =============================================================================
        // EnvironmentProtectedTestHarnessResult
        // =============================================================================
        /// <summary>
        /// <para>
        /// Costruisce il risultato dello smoke test.
        /// </para>
        /// </summary>
        public EnvironmentProtectedTestHarnessResult(
            bool passed,
            string message,
            EnvironmentFullSnapshot finalSnapshot)
        {
            Passed = passed;
            Message = string.IsNullOrWhiteSpace(message) ? "No diagnostics." : message;
            FinalSnapshot = finalSnapshot;
        }
    }

    // =============================================================================
    // EnvironmentProtectedTestHarness
    // =============================================================================
    /// <summary>
    /// <para>
    /// Harness data-only per validare il driver protetto della biosfera.
    /// </para>
    ///
    /// <para><b>Principio architetturale: test di integrazione Core prima del runtime</b></para>
    /// <para>
    /// Lo smoke test attraversa i contratti gia' implementati: bootstrap,
    /// avanzamento calendario/clima, ciclo naturale giornaliero e full snapshot.
    /// Cosi' il pannello visuale puo' essere verificato senza anticipare il ponte
    /// definitivo con ArcGraph o con la simulazione.
    /// </para>
    ///
    /// <para><b>Struttura interna:</b></para>
    /// <list type="bullet">
    ///   <item><b>RunDefaultSmoke</b>: esegue un percorso minimo giorno/mese/stagione.</item>
    ///   <item><b>Fail/Pass</b>: helper di risultato compatto.</item>
    /// </list>
    /// </summary>
    public static class EnvironmentProtectedTestHarness
    {
        // =============================================================================
        // RunDefaultSmoke
        // =============================================================================
        /// <summary>
        /// <para>
        /// Esegue uno smoke test sullo scenario protetto predefinito.
        /// </para>
        /// </summary>
        public static EnvironmentProtectedTestHarnessResult RunDefaultSmoke()
        {
            var driver = new EnvironmentProtectedTestDriver();
            var bootstrap = driver.Bootstrap();
            if (bootstrap == null || !bootstrap.IsValid)
                return Fail("Bootstrap non valido.", driver.FullSnapshot);

            if (driver.FullSnapshot == null || driver.FullSnapshot.AreaCount <= 0)
                return Fail("Full snapshot iniziale senza aree.", driver.FullSnapshot);

            var dayReport = driver.AdvanceDays(1);
            if (dayReport == null || !dayReport.DayChanged || !dayReport.RanNaturalGrowth)
                return Fail("Avanzamento giornaliero non ha attivato la crescita naturale.", driver.FullSnapshot);

            var monthReport = driver.AdvanceMonths(1);
            if (monthReport == null || !monthReport.MonthChanged)
                return Fail("Avanzamento mensile non ha attraversato un confine mese.", driver.FullSnapshot);

            var seasonReport = driver.AdvanceSeasons(1);
            if (seasonReport == null || !seasonReport.SeasonChanged)
                return Fail("Avanzamento stagionale non ha attraversato un confine stagione.", driver.FullSnapshot);

            var speedReport = driver.AdvanceRealSeconds(
                1d,
                EnvironmentProtectedTestSpeedPreset.OneDayPerSecond);
            if (speedReport == null || speedReport.TicksAdvanced <= 0)
                return Fail("Preset di velocita' non ha prodotto tick ambientali.", driver.FullSnapshot);

            return Pass("Smoke test protetto completato.", driver.FullSnapshot);
        }

        private static EnvironmentProtectedTestHarnessResult Fail(
            string message,
            EnvironmentFullSnapshot snapshot)
        {
            return new EnvironmentProtectedTestHarnessResult(false, message, snapshot);
        }

        private static EnvironmentProtectedTestHarnessResult Pass(
            string message,
            EnvironmentFullSnapshot snapshot)
        {
            return new EnvironmentProtectedTestHarnessResult(true, message, snapshot);
        }
    }
}
