using System;

namespace Arcontio.Core.Environment
{
    // =============================================================================
    // EnvironmentClimateResolver
    // =============================================================================
    /// <summary>
    /// <para>
    /// Resolver data-only del clima globale.
    /// </para>
    ///
    /// <para><b>Principio architetturale: meteo deterministico e leggero</b></para>
    /// <para>
    /// Il resolver usa calendario e configurazione per produrre temperatura,
    /// umidita', aridita' e meteo corrente. Non modifica aree, piante, NPC o world
    /// state. La scelta meteo usa un hash deterministico del giorno, sufficiente per
    /// harness e foundation senza introdurre un servizio random globale.
    /// </para>
    ///
    /// <para><b>Struttura interna:</b></para>
    /// <list type="bullet">
    ///   <item><b>Resolve</b>: produce lo stato climatico globale.</item>
    ///   <item><b>ResolveSeasonClimateProfile</b>: legge profilo stagionale.</item>
    ///   <item><b>ResolveWeatherKind</b>: seleziona meteo con soglie configurate.</item>
    /// </list>
    /// </summary>
    public static class EnvironmentClimateResolver
    {
        // =============================================================================
        // Resolve
        // =============================================================================
        /// <summary>
        /// <para>
        /// Risolve clima e meteo globale da calendario e configurazione.
        /// </para>
        /// </summary>
        public static EnvironmentGlobalClimateState Resolve(
            EnvironmentCalendarState calendar,
            EnvironmentClimateConfig config,
            EnvironmentCalendarConfig calendarConfig = null)
        {
            var safeConfig = config ?? new EnvironmentClimateConfig();
            var safeCalendarConfig = calendarConfig ?? new EnvironmentCalendarConfig();
            var profile = ResolveBlendedSeasonClimateProfile(
                safeConfig,
                safeCalendarConfig,
                calendar);
            float dayNoise01 = SmoothHash01(calendar.Date.Year, calendar.Date.DayOfYear, 17, 10);
            float hourWave01 = ComputeHourWave01(calendar.TimeOfDay.NormalizedDay01);
            float hourlyVariation = safeConfig.ResolveHourlyTemperatureVariation01();

            float temperature = profile.MeanTemperature01
                                + ((hourWave01 - 0.5f) * profile.TemperatureVariation01)
                                + ((dayNoise01 - 0.5f) * hourlyVariation);

            var weatherKind = ResolveWeatherKind(profile, calendar.Date.Year, calendar.Date.DayOfYear);
            float weatherIntensity = ResolveWeatherIntensity(weatherKind, calendar.Date.Year, calendar.Date.DayOfYear);
            float precipitation = ResolvePrecipitation(weatherKind, weatherIntensity);
            float wind = ResolveWind(weatherKind, profile, calendar.Date.Year, calendar.Date.DayOfYear);
            bool isExtreme = weatherKind == EnvironmentWeatherKind.HeatWave || weatherKind == EnvironmentWeatherKind.Storm;

            float humidity = profile.BaseHumidity01 + (precipitation * 0.25f) - (wind * 0.10f);
            float aridity = 1f - humidity;
            if (weatherKind == EnvironmentWeatherKind.HeatWave)
                aridity += 0.25f;

            var weather = new EnvironmentWeatherState(
                weatherKind,
                weatherIntensity,
                precipitation,
                wind,
                isExtreme);

            return new EnvironmentGlobalClimateState(
                temperature,
                humidity,
                aridity,
                weather,
                calendar.Date.Season);
        }

        // =============================================================================
        // ResolveSeasonClimateProfile
        // =============================================================================
        /// <summary>
        /// <para>
        /// Restituisce il profilo climatico della stagione richiesta.
        /// </para>
        /// </summary>
        public static EnvironmentSeasonClimateProfile ResolveSeasonClimateProfile(
            EnvironmentClimateConfig config,
            EnvironmentSeasonKind season)
        {
            var profiles = config?.seasonClimateProfiles;
            if (profiles != null)
            {
                for (int i = 0; i < profiles.Length; i++)
                {
                    if (profiles[i] == null)
                        continue;

                    if (EnvironmentConfigParsing.ParseSeason(profiles[i].season) == season)
                        return profiles[i].ToProfile();
                }
            }

            var defaults = EnvironmentSeasonClimateProfileConfig.CreateDefaultSet();
            for (int i = 0; i < defaults.Length; i++)
            {
                if (EnvironmentConfigParsing.ParseSeason(defaults[i].season) == season)
                    return defaults[i].ToProfile();
            }

            return defaults[0].ToProfile();
        }

        // =============================================================================
        // ResolveBlendedSeasonClimateProfile
        // =============================================================================
        /// <summary>
        /// <para>
        /// Restituisce un profilo climatico interpolato tra stagione corrente e
        /// stagione successiva.
        /// </para>
        ///
        /// <para><b>Principio architetturale: clima continuo, stagioni discrete</b></para>
        /// <para>
        /// Il calendario resta discreto, ma temperatura e umidita' non devono saltare
        /// a gradino al cambio stagione. Il profilo interpolato mantiene la forma
        /// data-driven dei profili stagionali e rende il segnale leggibile nei grafici.
        /// </para>
        /// </summary>
        private static EnvironmentSeasonClimateProfile ResolveBlendedSeasonClimateProfile(
            EnvironmentClimateConfig config,
            EnvironmentCalendarConfig calendarConfig,
            EnvironmentCalendarState calendar)
        {
            var current = ResolveSeasonClimateProfile(config, calendar.Date.Season);
            var next = ResolveSeasonClimateProfile(config, ResolveNextSeason(calendar.Date.Season));
            int daysPerSeason = calendarConfig.ResolveDaysPerMonth()
                                * calendarConfig.ResolveMonthsPerSeason();
            float season01 = daysPerSeason <= 1
                ? 0f
                : calendar.Date.DayInSeason / (float)(daysPerSeason - 1);
            float smooth01 = SmoothStep01(season01);

            return BlendProfiles(current, next, smooth01);
        }

        private static EnvironmentSeasonKind ResolveNextSeason(EnvironmentSeasonKind season)
        {
            switch (season)
            {
                case EnvironmentSeasonKind.Spring:
                    return EnvironmentSeasonKind.Summer;

                case EnvironmentSeasonKind.Summer:
                    return EnvironmentSeasonKind.Autumn;

                case EnvironmentSeasonKind.Autumn:
                    return EnvironmentSeasonKind.Winter;

                default:
                    return EnvironmentSeasonKind.Spring;
            }
        }

        private static EnvironmentSeasonClimateProfile BlendProfiles(
            EnvironmentSeasonClimateProfile current,
            EnvironmentSeasonClimateProfile next,
            float t)
        {
            float safeT = EnvironmentMath.Clamp01(t);
            return new EnvironmentSeasonClimateProfile(
                Lerp(current.MeanTemperature01, next.MeanTemperature01, safeT),
                Lerp(current.TemperatureVariation01, next.TemperatureVariation01, safeT),
                Lerp(current.RainProbability01, next.RainProbability01, safeT),
                Lerp(current.SnowProbability01, next.SnowProbability01, safeT),
                Lerp(current.WindProbability01, next.WindProbability01, safeT),
                Lerp(current.HeatWaveProbability01, next.HeatWaveProbability01, safeT),
                Lerp(current.BaseHumidity01, next.BaseHumidity01, safeT),
                (int)System.Math.Round(Lerp(
                    current.AverageEventDurationHours,
                    next.AverageEventDurationHours,
                    safeT)));
        }

        private static float SmoothStep01(float value)
        {
            float t = EnvironmentMath.Clamp01(value);
            return t * t * (3f - (2f * t));
        }

        private static float Lerp(float from, float to, float t)
        {
            return from + ((to - from) * EnvironmentMath.Clamp01(t));
        }

        private static EnvironmentWeatherKind ResolveWeatherKind(
            EnvironmentSeasonClimateProfile profile,
            int year,
            int dayOfYear)
        {
            float roll = Hash01(year, dayOfYear, 101);
            float heatThreshold = profile.HeatWaveProbability01;
            float snowThreshold = heatThreshold + profile.SnowProbability01;
            float rainThreshold = snowThreshold + profile.RainProbability01;
            float windThreshold = rainThreshold + profile.WindProbability01;

            if (roll < heatThreshold)
                return EnvironmentWeatherKind.HeatWave;

            if (roll < snowThreshold)
                return EnvironmentWeatherKind.Snow;

            if (roll < rainThreshold)
                return EnvironmentWeatherKind.Rain;

            if (roll < windThreshold)
                return EnvironmentWeatherKind.Wind;

            return EnvironmentWeatherKind.Clear;
        }

        private static float ResolveWeatherIntensity(EnvironmentWeatherKind kind, int year, int dayOfYear)
        {
            if (kind == EnvironmentWeatherKind.Clear)
                return 0f;

            return 0.35f + (Hash01(year, dayOfYear, 211) * 0.65f);
        }

        private static float ResolvePrecipitation(EnvironmentWeatherKind kind, float intensity01)
        {
            return kind == EnvironmentWeatherKind.Rain
                   || kind == EnvironmentWeatherKind.Snow
                   || kind == EnvironmentWeatherKind.Storm
                ? intensity01
                : 0f;
        }

        private static float ResolveWind(
            EnvironmentWeatherKind kind,
            EnvironmentSeasonClimateProfile profile,
            int year,
            int dayOfYear)
        {
            float baseWind = profile.WindProbability01 * 0.5f;
            if (kind == EnvironmentWeatherKind.Wind || kind == EnvironmentWeatherKind.Storm)
                baseWind += 0.45f;

            return EnvironmentMath.Clamp01(baseWind + (Hash01(year, dayOfYear, 307) * 0.15f));
        }

        private static float ComputeHourWave01(float normalizedDay01)
        {
            // Picco caldo semplice nel pomeriggio: abbastanza per foundation e test,
            // non ancora un modello climatico produttivo.
            float shifted = normalizedDay01 - 0.25f;
            if (shifted < 0f)
                shifted += 1f;

            return shifted <= 0.5f ? shifted * 2f : (1f - shifted) * 2f;
        }

        private static float Hash01(int year, int dayOfYear, int salt)
        {
            unchecked
            {
                int hash = 17;
                hash = (hash * 31) + year;
                hash = (hash * 31) + dayOfYear;
                hash = (hash * 31) + salt;
                hash ^= hash << 13;
                hash ^= hash >> 17;
                hash ^= hash << 5;
                uint normalized = (uint)hash;
                return (normalized % 10000) / 10000f;
            }
        }

        private static float SmoothHash01(int year, int dayOfYear, int salt, int spanDays)
        {
            int safeSpan = spanDays <= 0 ? 1 : spanDays;
            int block = dayOfYear / safeSpan;
            int nextBlock = block + 1;
            float t = (dayOfYear % safeSpan) / (float)safeSpan;
            float from = Hash01(year, block, salt);
            float to = Hash01(year, nextBlock, salt);
            return Lerp(from, to, SmoothStep01(t));
        }
    }
}
