using System;

namespace Arcontio.Core.Environment
{
    // =============================================================================
    // EnvironmentCalendarResolver
    // =============================================================================
    /// <summary>
    /// <para>
    /// Resolver data-only del calendario ambientale.
    /// </para>
    ///
    /// <para><b>Principio architetturale: derivazione temporale senza lifecycle</b></para>
    /// <para>
    /// Il resolver non possiede tempo, non chiama <c>SimulationHost</c> e non avanza
    /// il mondo. Riceve un contatore ambientale e una configurazione, poi restituisce
    /// una fotografia leggibile di data, ora, stagione e luce.
    /// </para>
    ///
    /// <para><b>Struttura interna:</b></para>
    /// <list type="bullet">
    ///   <item><b>Resolve</b>: trasforma tick ambientali in stato calendario.</item>
    ///   <item><b>ResolveSeasonProfile</b>: legge il profilo stagione configurato.</item>
    ///   <item><b>ComputeLightIntensity01</b>: produce una curva luce semplice.</item>
    /// </list>
    /// </summary>
    public static class EnvironmentCalendarResolver
    {
        // =============================================================================
        // Resolve
        // =============================================================================
        /// <summary>
        /// <para>
        /// Risolve lo stato calendario da un contatore ambientale.
        /// </para>
        ///
        /// <para><b>Baseline configurabile</b></para>
        /// <para>
        /// Con i default correnti, <c>50</c> tick ambientali equivalgono a un'ora
        /// simulata e <c>1200</c> tick equivalgono a un giorno simulato, cioe'
        /// venti minuti reali nella scala progettuale.
        /// </para>
        /// </summary>
        public static EnvironmentCalendarState Resolve(
            long elapsedEnvironmentTicks,
            EnvironmentCalendarConfig config)
        {
            var safeConfig = config ?? new EnvironmentCalendarConfig();
            long safeTicks = elapsedEnvironmentTicks < 0 ? 0 : elapsedEnvironmentTicks;

            int hoursPerDay = safeConfig.ResolveHoursPerDay();
            int ticksPerHour = safeConfig.ResolveCalendarTicksPerSimulatedHour();
            int ticksPerDay = Math.Max(1, hoursPerDay * ticksPerHour);
            int daysPerMonth = safeConfig.ResolveDaysPerMonth();
            int monthsPerYear = safeConfig.ResolveMonthsPerYear();
            int monthsPerSeason = safeConfig.ResolveMonthsPerSeason();
            int daysPerYear = Math.Max(1, daysPerMonth * monthsPerYear);
            int daysPerSeason = Math.Max(1, daysPerMonth * monthsPerSeason);

            int absoluteDay = (int)(safeTicks / ticksPerDay);
            int tickOfDay = (int)(safeTicks % ticksPerDay);
            int hour = tickOfDay / ticksPerHour;
            int minute = ResolveMinute(tickOfDay, ticksPerHour);
            float normalizedDay01 = ticksPerDay <= 0 ? 0f : (float)tickOfDay / ticksPerDay;

            int year = absoluteDay / daysPerYear;
            int dayOfYear = absoluteDay % daysPerYear;
            int month = dayOfYear / daysPerMonth;
            int dayOfMonth = dayOfYear % daysPerMonth;
            var season = ResolveSeason(month, monthsPerSeason);
            int dayInSeason = dayOfYear % daysPerSeason;
            var profile = ResolveSeasonProfile(safeConfig, season);

            var date = new EnvironmentDate(year, month, dayOfMonth, dayOfYear, season, dayInSeason);
            var time = new EnvironmentTimeOfDay(hour, minute, normalizedDay01);

            return new EnvironmentCalendarState(
                safeTicks,
                date,
                time,
                profile.DaylightHours,
                ComputeLightIntensity01(normalizedDay01, hoursPerDay, profile.DaylightHours));
        }

        // =============================================================================
        // ResolveSeasonProfile
        // =============================================================================
        /// <summary>
        /// <para>
        /// Restituisce il profilo stagionale configurato, con fallback sicuro.
        /// </para>
        /// </summary>
        public static EnvironmentSeasonProfile ResolveSeasonProfile(
            EnvironmentCalendarConfig config,
            EnvironmentSeasonKind season)
        {
            var profiles = config?.seasonProfiles;
            if (profiles != null)
            {
                for (int i = 0; i < profiles.Length; i++)
                {
                    if (profiles[i] == null)
                        continue;

                    var profile = profiles[i].ToProfile();
                    if (profile.Season == season)
                        return profile;
                }
            }

            // Fallback esplicito: se la config e' incompleta, usiamo il set default
            // invece di fallire o lasciare ore luce a zero.
            var defaults = EnvironmentSeasonProfileConfig.CreateDefaultSet();
            for (int i = 0; i < defaults.Length; i++)
            {
                var profile = defaults[i].ToProfile();
                if (profile.Season == season)
                    return profile;
            }

            return defaults[0].ToProfile();
        }

        private static int ResolveMinute(int tickOfDay, int ticksPerHour)
        {
            if (ticksPerHour <= 0)
                return 0;

            int tickInHour = tickOfDay % ticksPerHour;
            return (int)((tickInHour / (float)ticksPerHour) * 60f);
        }

        private static EnvironmentSeasonKind ResolveSeason(int month, int monthsPerSeason)
        {
            int seasonIndex = monthsPerSeason <= 0 ? 0 : month / monthsPerSeason;
            switch (seasonIndex)
            {
                case 1:
                    return EnvironmentSeasonKind.Summer;
                case 2:
                    return EnvironmentSeasonKind.Autumn;
                case 3:
                    return EnvironmentSeasonKind.Winter;
                default:
                    return EnvironmentSeasonKind.Spring;
            }
        }

        private static float ComputeLightIntensity01(float normalizedDay01, int hoursPerDay, float daylightHours)
        {
            if (hoursPerDay <= 0 || daylightHours <= 0f)
                return 0f;

            float daylightFraction = EnvironmentMath.Clamp01(daylightHours / hoursPerDay);
            float sunrise = 0.5f - (daylightFraction * 0.5f);
            float sunset = 0.5f + (daylightFraction * 0.5f);

            if (normalizedDay01 <= sunrise || normalizedDay01 >= sunset)
                return 0f;

            float localDay01 = (normalizedDay01 - sunrise) / Math.Max(0.0001f, sunset - sunrise);

            // Curva triangolare semplice: luce massima a meta' finestra diurna.
            float triangle = localDay01 <= 0.5f
                ? localDay01 * 2f
                : (1f - localDay01) * 2f;

            return EnvironmentMath.Clamp01(triangle);
        }
    }
}
