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
            EnvironmentClimateConfig config)
        {
            var safeConfig = config ?? new EnvironmentClimateConfig();
            var profile = ResolveSmoothedSeasonClimateProfile(safeConfig, calendar);
            float dayNoise01 = Hash01(calendar.Date.Year, calendar.Date.DayOfYear, 17);
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
        // ResolveSmoothedSeasonClimateProfile
        // =============================================================================
        /// <summary>
        /// <para>
        /// Restituisce il profilo climatico stagionale con una transizione morbida
        /// distribuita sulla finestra configurata.
        /// </para>
        ///
        /// <para><b>Principio architetturale: stagioni continue, medie ancora configurate</b></para>
        /// <para>
        /// I valori stagionali restano quelli del JSON, ma il passaggio da una
        /// stagione alla successiva non viene piu' applicato come gradino secco al
        /// giorno zero. Il resolver interpola dal profilo precedente a quello
        /// corrente per una finestra configurabile. Nei test correnti la finestra
        /// coincide con l'intera stagione, evitando il gradino netto tra due profili
        /// climatici molto distanti.
        /// </para>
        /// </summary>
        private static EnvironmentSeasonClimateProfile ResolveSmoothedSeasonClimateProfile(
            EnvironmentClimateConfig config,
            EnvironmentCalendarState calendar)
        {
            var current = ResolveSeasonClimateProfile(config, calendar.Date.Season);
            int blendDays = config.ResolveSeasonClimateBlendDays();
            if (blendDays <= 0 || calendar.Date.DayInSeason >= blendDays)
                return current;

            var previous = ResolveSeasonClimateProfile(
                config,
                ResolvePreviousSeason(calendar.Date.Season));
            float t = SmoothStep01(calendar.Date.DayInSeason / (float)blendDays);
            return BlendProfiles(previous, current, t);
        }

        private static EnvironmentSeasonClimateProfile BlendProfiles(
            EnvironmentSeasonClimateProfile from,
            EnvironmentSeasonClimateProfile to,
            float t01)
        {
            float t = EnvironmentMath.Clamp01(t01);
            return new EnvironmentSeasonClimateProfile(
                Lerp(from.MeanTemperature01, to.MeanTemperature01, t),
                Lerp(from.TemperatureVariation01, to.TemperatureVariation01, t),
                Lerp(from.RainProbability01, to.RainProbability01, t),
                Lerp(from.SnowProbability01, to.SnowProbability01, t),
                Lerp(from.WindProbability01, to.WindProbability01, t),
                Lerp(from.HeatWaveProbability01, to.HeatWaveProbability01, t),
                Lerp(from.BaseHumidity01, to.BaseHumidity01, t),
                (int)Math.Round(Lerp(
                    from.AverageEventDurationHours,
                    to.AverageEventDurationHours,
                    t)));
        }

        private static EnvironmentSeasonKind ResolvePreviousSeason(EnvironmentSeasonKind season)
        {
            if (season == EnvironmentSeasonKind.Summer)
                return EnvironmentSeasonKind.Spring;

            if (season == EnvironmentSeasonKind.Autumn)
                return EnvironmentSeasonKind.Summer;

            if (season == EnvironmentSeasonKind.Winter)
                return EnvironmentSeasonKind.Autumn;

            return EnvironmentSeasonKind.Winter;
        }

        private static float SmoothStep01(float value01)
        {
            float t = EnvironmentMath.Clamp01(value01);
            return t * t * (3f - (2f * t));
        }

        private static float Lerp(float from, float to, float t01)
        {
            return from + ((to - from) * EnvironmentMath.Clamp01(t01));
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
    }
}
