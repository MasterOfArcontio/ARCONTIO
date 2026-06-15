using System;

namespace Arcontio.Core.Environment
{
    // =============================================================================
    // EnvironmentCalendarConfig
    // =============================================================================
    /// <summary>
    /// <para>
    /// DTO serializzabile dei parametri temporali della biosfera.
    /// </para>
    ///
    /// <para><b>Principio architetturale: tempo ambientale configurabile</b></para>
    /// <para>
    /// La decisione progettuale corrente stabilisce la baseline
    /// <c>24 ore simulate = 20 minuti reali</c>. Questo dato non deve diventare una
    /// costante sparsa nei sistemi: viene espresso qui come
    /// <c>calendarTicksPerSimulatedHour = 50</c>, assumendo che un futuro adapter
    /// runtime decida come convertire tick simulativi o secondi reali in tick
    /// ambientali.
    /// </para>
    ///
    /// <para><b>Struttura interna:</b></para>
    /// <list type="bullet">
    ///   <item><b>hoursPerDay</b>: ore simulate in un giorno.</item>
    ///   <item><b>calendarTicksPerSimulatedHour</b>: unita' ambientali per ora simulata.</item>
    ///   <item><b>daysPerMonth</b>: giorni simulati in un mese.</item>
    ///   <item><b>monthsPerYear</b>: mesi simulati in un anno.</item>
    ///   <item><b>monthsPerSeason</b>: durata stagionale espressa in mesi.</item>
    ///   <item><b>seasonProfiles</b>: profili luce/bias per stagione.</item>
    /// </list>
    /// </summary>
    [Serializable]
    public sealed class EnvironmentCalendarConfig
    {
        public const int DefaultHoursPerDay = 24;
        public const int DefaultCalendarTicksPerSimulatedHour = 50;
        public const int DefaultDaysPerMonth = 25;
        public const int DefaultMonthsPerYear = 12;
        public const int DefaultMonthsPerSeason = 3;

        public int hoursPerDay = DefaultHoursPerDay;
        public int calendarTicksPerSimulatedHour = DefaultCalendarTicksPerSimulatedHour;
        public int daysPerMonth = DefaultDaysPerMonth;
        public int monthsPerYear = DefaultMonthsPerYear;
        public int monthsPerSeason = DefaultMonthsPerSeason;
        public EnvironmentSeasonProfileConfig[] seasonProfiles = EnvironmentSeasonProfileConfig.CreateDefaultSet();

        // =============================================================================
        // ResolveHoursPerDay
        // =============================================================================
        /// <summary>
        /// <para>
        /// Restituisce le ore per giorno normalizzando valori non validi.
        /// </para>
        /// </summary>
        public int ResolveHoursPerDay()
        {
            return hoursPerDay > 0 ? hoursPerDay : DefaultHoursPerDay;
        }

        public int ResolveCalendarTicksPerSimulatedHour()
        {
            return calendarTicksPerSimulatedHour > 0
                ? calendarTicksPerSimulatedHour
                : DefaultCalendarTicksPerSimulatedHour;
        }

        public int ResolveDaysPerMonth()
        {
            return daysPerMonth > 0 ? daysPerMonth : DefaultDaysPerMonth;
        }

        public int ResolveMonthsPerYear()
        {
            return monthsPerYear > 0 ? monthsPerYear : DefaultMonthsPerYear;
        }

        public int ResolveMonthsPerSeason()
        {
            return monthsPerSeason > 0 ? monthsPerSeason : DefaultMonthsPerSeason;
        }
    }

    // =============================================================================
    // EnvironmentSeasonProfileConfig
    // =============================================================================
    /// <summary>
    /// <para>
    /// DTO serializzabile del profilo stagionale.
    /// </para>
    ///
    /// <para><b>Principio architetturale: profili luce e crescita fuori dal codice caldo</b></para>
    /// <para>
    /// Le ore di luce e i bias stagionali vengono configurati come dati. Il resolver
    /// produce poi <see cref="EnvironmentSeasonProfile"/> value-only per i consumer
    /// ambientali.
    /// </para>
    ///
    /// <para><b>Struttura interna:</b></para>
    /// <list type="bullet">
    ///   <item><b>season</b>: nome stagione in formato stabile.</item>
    ///   <item><b>daylightHours</b>: ore luce baseline.</item>
    ///   <item><b>temperatureBias01</b>: bias temperatura.</item>
    ///   <item><b>rainfallBias01</b>: bias pioggia.</item>
    ///   <item><b>fertilityBias01</b>: bias recupero/fertilita'.</item>
    ///   <item><b>vegetationGrowthBias01</b>: bias crescita vegetale.</item>
    /// </list>
    /// </summary>
    [Serializable]
    public sealed class EnvironmentSeasonProfileConfig
    {
        public string season = "Spring";
        public float daylightHours = 12f;
        public float temperatureBias01 = 0.5f;
        public float rainfallBias01 = 0.5f;
        public float fertilityBias01 = 0.5f;
        public float vegetationGrowthBias01 = 0.5f;

        public EnvironmentSeasonProfileConfig()
        {
        }

        public EnvironmentSeasonProfileConfig(
            string season,
            float daylightHours,
            float temperatureBias01,
            float rainfallBias01,
            float fertilityBias01,
            float vegetationGrowthBias01)
        {
            this.season = season ?? "Spring";
            this.daylightHours = daylightHours;
            this.temperatureBias01 = temperatureBias01;
            this.rainfallBias01 = rainfallBias01;
            this.fertilityBias01 = fertilityBias01;
            this.vegetationGrowthBias01 = vegetationGrowthBias01;
        }

        // =============================================================================
        // ToProfile
        // =============================================================================
        /// <summary>
        /// <para>
        /// Converte il DTO serializzabile in profilo value-only.
        /// </para>
        /// </summary>
        public EnvironmentSeasonProfile ToProfile()
        {
            return new EnvironmentSeasonProfile(
                EnvironmentConfigParsing.ParseSeason(season),
                daylightHours,
                temperatureBias01,
                rainfallBias01,
                fertilityBias01,
                vegetationGrowthBias01);
        }

        public static EnvironmentSeasonProfileConfig[] CreateDefaultSet()
        {
            return new[]
            {
                new EnvironmentSeasonProfileConfig("Spring", 12f, 0.55f, 0.65f, 0.75f, 0.85f),
                new EnvironmentSeasonProfileConfig("Summer", 16f, 0.85f, 0.30f, 0.55f, 0.70f),
                new EnvironmentSeasonProfileConfig("Autumn", 12f, 0.45f, 0.75f, 0.65f, 0.45f),
                new EnvironmentSeasonProfileConfig("Winter", 8f, 0.15f, 0.45f, 0.30f, 0.15f)
            };
        }
    }

    // =============================================================================
    // EnvironmentClimateConfig
    // =============================================================================
    /// <summary>
    /// <para>
    /// DTO serializzabile dei parametri climatici globali.
    /// </para>
    ///
    /// <para><b>Principio architetturale: clima globale leggero</b></para>
    /// <para>
    /// Il primo clima della biosfera resta globale e poco costoso. Non contiene
    /// umidita' per cella, fluidodinamica o microclima stanza-per-stanza.
    /// </para>
    ///
    /// <para><b>Struttura interna:</b></para>
    /// <list type="bullet">
    ///   <item><b>seasonClimateProfiles</b>: probabilita' meteo per stagione.</item>
    ///   <item><b>weatherPersistence01</b>: inerzia meteo futura.</item>
    ///   <item><b>hourlyTemperatureVariation01</b>: oscillazione oraria leggera.</item>
    /// </list>
    /// </summary>
    [Serializable]
    public sealed class EnvironmentClimateConfig
    {
        public EnvironmentSeasonClimateProfileConfig[] seasonClimateProfiles =
            EnvironmentSeasonClimateProfileConfig.CreateDefaultSet();

        public float weatherPersistence01 = 0.35f;
        public float hourlyTemperatureVariation01 = 0.10f;

        public float ResolveWeatherPersistence01()
        {
            return EnvironmentMath.Clamp01(weatherPersistence01);
        }

        public float ResolveHourlyTemperatureVariation01()
        {
            return EnvironmentMath.Clamp01(hourlyTemperatureVariation01);
        }
    }

    // =============================================================================
    // EnvironmentSeasonClimateProfileConfig
    // =============================================================================
    /// <summary>
    /// <para>
    /// DTO serializzabile del profilo climatico stagionale.
    /// </para>
    ///
    /// <para><b>Principio architetturale: probabilita' meteo dichiarate</b></para>
    /// <para>
    /// Pioggia, neve, vento e caldo non vengono scelte da switch nascosti nei
    /// sistemi. Il resolver legge questa configurazione e produce uno stato clima
    /// value-only.
    /// </para>
    ///
    /// <para><b>Struttura interna:</b></para>
    /// <list type="bullet">
    ///   <item><b>season</b>: stagione di riferimento.</item>
    ///   <item><b>meanTemperature01</b>: temperatura media.</item>
    ///   <item><b>temperatureVariation01</b>: ampiezza variazione.</item>
    ///   <item><b>rainProbability01</b>: probabilita' pioggia.</item>
    ///   <item><b>snowProbability01</b>: probabilita' neve.</item>
    ///   <item><b>windProbability01</b>: probabilita' vento.</item>
    ///   <item><b>heatWaveProbability01</b>: probabilita' caldo estremo.</item>
    ///   <item><b>baseHumidity01</b>: umidita' media.</item>
    ///   <item><b>averageEventDurationHours</b>: durata media eventi.</item>
    /// </list>
    /// </summary>
    [Serializable]
    public sealed class EnvironmentSeasonClimateProfileConfig
    {
        public string season = "Spring";
        public float meanTemperature01 = 0.5f;
        public float temperatureVariation01 = 0.15f;
        public float rainProbability01 = 0.5f;
        public float snowProbability01 = 0f;
        public float windProbability01 = 0.35f;
        public float heatWaveProbability01 = 0.05f;
        public float baseHumidity01 = 0.5f;
        public int averageEventDurationHours = 4;

        public EnvironmentSeasonClimateProfileConfig()
        {
        }

        public EnvironmentSeasonClimateProfileConfig(
            string season,
            float meanTemperature01,
            float temperatureVariation01,
            float rainProbability01,
            float snowProbability01,
            float windProbability01,
            float heatWaveProbability01,
            float baseHumidity01,
            int averageEventDurationHours)
        {
            this.season = season ?? "Spring";
            this.meanTemperature01 = meanTemperature01;
            this.temperatureVariation01 = temperatureVariation01;
            this.rainProbability01 = rainProbability01;
            this.snowProbability01 = snowProbability01;
            this.windProbability01 = windProbability01;
            this.heatWaveProbability01 = heatWaveProbability01;
            this.baseHumidity01 = baseHumidity01;
            this.averageEventDurationHours = averageEventDurationHours;
        }

        public EnvironmentSeasonClimateProfile ToProfile()
        {
            return new EnvironmentSeasonClimateProfile(
                meanTemperature01,
                temperatureVariation01,
                rainProbability01,
                snowProbability01,
                windProbability01,
                heatWaveProbability01,
                baseHumidity01,
                averageEventDurationHours);
        }

        public static EnvironmentSeasonClimateProfileConfig[] CreateDefaultSet()
        {
            return new[]
            {
                new EnvironmentSeasonClimateProfileConfig("Spring", 0.55f, 0.15f, 0.65f, 0.05f, 0.35f, 0.05f, 0.60f, 5),
                new EnvironmentSeasonClimateProfileConfig("Summer", 0.85f, 0.18f, 0.25f, 0.00f, 0.25f, 0.25f, 0.35f, 4),
                new EnvironmentSeasonClimateProfileConfig("Autumn", 0.45f, 0.18f, 0.75f, 0.05f, 0.60f, 0.03f, 0.70f, 6),
                new EnvironmentSeasonClimateProfileConfig("Winter", 0.15f, 0.16f, 0.35f, 0.45f, 0.50f, 0.00f, 0.55f, 6)
            };
        }
    }

    // =============================================================================
    // EnvironmentConfigParsing
    // =============================================================================
    /// <summary>
    /// <para>
    /// Helper interno di parsing per DTO ambientali.
    /// </para>
    ///
    /// <para><b>Principio architetturale: parsing confinato</b></para>
    /// <para>
    /// I DTO serializzabili usano stringhe per restare semplici da configurare. La
    /// conversione in enum viene confinata qui, cosi' i resolver lavorano su value
    /// type gia' normalizzati.
    /// </para>
    ///
    /// <para><b>Struttura interna:</b></para>
    /// <list type="bullet">
    ///   <item><b>ParseSeason</b>: converte una stringa stagione in enum canonica.</item>
    /// </list>
    /// </summary>
    internal static class EnvironmentConfigParsing
    {
        public static EnvironmentSeasonKind ParseSeason(string value)
        {
            if (string.Equals(value, "Summer", StringComparison.OrdinalIgnoreCase))
                return EnvironmentSeasonKind.Summer;

            if (string.Equals(value, "Autumn", StringComparison.OrdinalIgnoreCase)
                || string.Equals(value, "Fall", StringComparison.OrdinalIgnoreCase))
            {
                return EnvironmentSeasonKind.Autumn;
            }

            if (string.Equals(value, "Winter", StringComparison.OrdinalIgnoreCase))
                return EnvironmentSeasonKind.Winter;

            return EnvironmentSeasonKind.Spring;
        }
    }
}
