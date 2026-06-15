namespace Arcontio.Core.Environment
{
    // =============================================================================
    // EnvironmentTemporalTransition
    // =============================================================================
    /// <summary>
    /// <para>
    /// Fotografia data-only della transizione tra due tick ambientali.
    /// </para>
    ///
    /// <para><b>Principio architetturale: cadenze derivate, non lifecycle posseduto</b></para>
    /// <para>
    /// La biosfera avra' sistemi che lavorano a cadenze diverse: alcuni orari,
    /// alcuni giornalieri, altri stagionali. Questa struttura non esegue quei
    /// sistemi: espone solo i flag necessari a decidere quando farli lavorare.
    /// </para>
    ///
    /// <para><b>Struttura interna:</b></para>
    /// <list type="bullet">
    ///   <item><b>Previous</b>: calendario risolto al tick precedente.</item>
    ///   <item><b>Current</b>: calendario risolto al tick corrente.</item>
    ///   <item><b>ElapsedTicks</b>: differenza non negativa tra i due tick.</item>
    ///   <item><b>HourChanged</b>: almeno un cambio ora simulata.</item>
    ///   <item><b>DayChanged</b>: almeno un cambio giorno simulato.</item>
    ///   <item><b>MonthChanged</b>: almeno un cambio mese simulato.</item>
    ///   <item><b>SeasonChanged</b>: almeno un cambio stagione simulata.</item>
    ///   <item><b>YearChanged</b>: almeno un cambio anno simulato.</item>
    /// </list>
    /// </summary>
    public readonly struct EnvironmentTemporalTransition
    {
        public readonly EnvironmentCalendarState Previous;
        public readonly EnvironmentCalendarState Current;
        public readonly long ElapsedTicks;
        public readonly bool HourChanged;
        public readonly bool DayChanged;
        public readonly bool MonthChanged;
        public readonly bool SeasonChanged;
        public readonly bool YearChanged;

        public bool AnyBoundaryChanged =>
            HourChanged
            || DayChanged
            || MonthChanged
            || SeasonChanged
            || YearChanged;

        // =============================================================================
        // EnvironmentTemporalTransition
        // =============================================================================
        /// <summary>
        /// <para>
        /// Costruisce una transizione temporale gia' risolta.
        /// </para>
        /// </summary>
        public EnvironmentTemporalTransition(
            EnvironmentCalendarState previous,
            EnvironmentCalendarState current,
            long elapsedTicks,
            bool hourChanged,
            bool dayChanged,
            bool monthChanged,
            bool seasonChanged,
            bool yearChanged)
        {
            Previous = previous;
            Current = current;
            ElapsedTicks = elapsedTicks < 0 ? 0 : elapsedTicks;
            HourChanged = hourChanged;
            DayChanged = dayChanged;
            MonthChanged = monthChanged;
            SeasonChanged = seasonChanged;
            YearChanged = yearChanged;
        }
    }

    // =============================================================================
    // EnvironmentTemporalTransitionResolver
    // =============================================================================
    /// <summary>
    /// <para>
    /// Resolver data-only delle transizioni temporali ambientali.
    /// </para>
    ///
    /// <para><b>Principio architetturale: confine tra tempo grezzo e sistemi biosfera</b></para>
    /// <para>
    /// I futuri sistemi di crescita, meteo, acqua e fertilita' non dovrebbero
    /// duplicare la matematica del calendario. Questo resolver centralizza la
    /// derivazione dei confini orari, giornalieri, mensili, stagionali e annuali.
    /// </para>
    ///
    /// <para><b>Struttura interna:</b></para>
    /// <list type="bullet">
    ///   <item><b>Resolve</b>: produce transizione completa da tick precedente/corrente.</item>
    ///   <item><b>ResolveFromPreviousCalendar</b>: evita di ricalcolare lo stato precedente.</item>
    ///   <item><b>ComputeElapsedTicks</b>: normalizza tick invertiti o negativi.</item>
    /// </list>
    /// </summary>
    public static class EnvironmentTemporalTransitionResolver
    {
        // =============================================================================
        // Resolve
        // =============================================================================
        /// <summary>
        /// <para>
        /// Risolve una transizione temporale partendo da due tick ambientali.
        /// </para>
        /// </summary>
        public static EnvironmentTemporalTransition Resolve(
            long previousEnvironmentTicks,
            long currentEnvironmentTicks,
            EnvironmentCalendarConfig calendarConfig)
        {
            var safePreviousTicks = previousEnvironmentTicks < 0 ? 0 : previousEnvironmentTicks;
            var safeCurrentTicks = currentEnvironmentTicks < 0 ? 0 : currentEnvironmentTicks;
            if (safeCurrentTicks < safePreviousTicks)
                safeCurrentTicks = safePreviousTicks;

            var previous = EnvironmentCalendarResolver.Resolve(
                safePreviousTicks,
                calendarConfig);
            return ResolveFromPreviousCalendar(
                previous,
                safeCurrentTicks,
                calendarConfig);
        }

        // =============================================================================
        // ResolveFromPreviousCalendar
        // =============================================================================
        /// <summary>
        /// <para>
        /// Risolve una transizione riusando uno stato calendario precedente.
        /// </para>
        /// </summary>
        public static EnvironmentTemporalTransition ResolveFromPreviousCalendar(
            EnvironmentCalendarState previous,
            long currentEnvironmentTicks,
            EnvironmentCalendarConfig calendarConfig)
        {
            var safeCurrentTicks = currentEnvironmentTicks < 0 ? 0 : currentEnvironmentTicks;
            if (safeCurrentTicks < previous.ElapsedEnvironmentTicks)
                safeCurrentTicks = previous.ElapsedEnvironmentTicks;

            var current = EnvironmentCalendarResolver.Resolve(
                safeCurrentTicks,
                calendarConfig);
            long elapsedTicks = ComputeElapsedTicks(
                previous.ElapsedEnvironmentTicks,
                current.ElapsedEnvironmentTicks);

            // I flag confrontano lo stato calendario gia' risolto. In questo modo il
            // significato dei confini resta allineato alla configurazione corrente.
            bool hourChanged =
                current.Date.DayOfYear != previous.Date.DayOfYear
                || current.TimeOfDay.Hour != previous.TimeOfDay.Hour
                || current.Date.Year != previous.Date.Year;
            bool dayChanged =
                current.Date.DayOfYear != previous.Date.DayOfYear
                || current.Date.Year != previous.Date.Year;
            bool monthChanged =
                current.Date.Month != previous.Date.Month
                || current.Date.Year != previous.Date.Year;
            bool seasonChanged =
                current.Date.Season != previous.Date.Season
                || current.Date.Year != previous.Date.Year;
            bool yearChanged = current.Date.Year != previous.Date.Year;

            return new EnvironmentTemporalTransition(
                previous,
                current,
                elapsedTicks,
                hourChanged,
                dayChanged,
                monthChanged,
                seasonChanged,
                yearChanged);
        }

        private static long ComputeElapsedTicks(long previousTicks, long currentTicks)
        {
            if (previousTicks < 0)
                previousTicks = 0;

            if (currentTicks < previousTicks)
                return 0;

            return currentTicks - previousTicks;
        }
    }
}
