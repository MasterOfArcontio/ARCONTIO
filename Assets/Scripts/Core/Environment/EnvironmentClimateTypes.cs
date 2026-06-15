namespace Arcontio.Core.Environment
{
    // =============================================================================
    // EnvironmentWeatherKind
    // =============================================================================
    /// <summary>
    /// <para>
    /// Tipo di meteo globale o per livello ambientale.
    /// </para>
    ///
    /// <para><b>Principio architetturale: meteo simulativo separato dal meteo visuale</b></para>
    /// <para>
    /// Questa enum descrive lo stato ambientale. Un futuro adapter potra' convertirla
    /// in snapshot ArcGraph, ma il Core non deve conoscere chiavi sprite, particelle
    /// o overlay grafici.
    /// </para>
    ///
    /// <para><b>Struttura interna:</b></para>
    /// <list type="bullet">
    ///   <item><b>Clear</b>: nessun evento meteo rilevante.</item>
    ///   <item><b>Rain</b>: pioggia.</item>
    ///   <item><b>Snow</b>: neve.</item>
    ///   <item><b>Wind</b>: vento dominante.</item>
    ///   <item><b>HeatWave</b>: caldo estremo o ondata calda.</item>
    ///   <item><b>Storm</b>: evento combinato futuro.</item>
    /// </list>
    /// </summary>
    public enum EnvironmentWeatherKind
    {
        Clear = 0,
        Rain = 10,
        Snow = 20,
        Wind = 30,
        HeatWave = 40,
        Storm = 50
    }

    // =============================================================================
    // EnvironmentWeatherState
    // =============================================================================
    /// <summary>
    /// <para>
    /// Stato meteo ambientale gia' risolto.
    /// </para>
    ///
    /// <para><b>Principio architetturale: meteo leggero e cadenzato</b></para>
    /// <para>
    /// Il meteo non deve ragionare ogni frame. La forma dati e' pensata per
    /// generazione giornaliera, variazioni orarie e persistenza, lasciando fuori
    /// rendering e impatti gameplay diretti.
    /// </para>
    ///
    /// <para><b>Struttura interna:</b></para>
    /// <list type="bullet">
    ///   <item><b>Kind</b>: tipo meteo corrente.</item>
    ///   <item><b>Intensity01</b>: intensita' normalizzata.</item>
    ///   <item><b>Precipitation01</b>: quota di precipitazione.</item>
    ///   <item><b>Wind01</b>: intensita' vento.</item>
    ///   <item><b>IsExtreme</b>: flag per eventi estremi futuri.</item>
    /// </list>
    /// </summary>
    public readonly struct EnvironmentWeatherState
    {
        public readonly EnvironmentWeatherKind Kind;
        public readonly float Intensity01;
        public readonly float Precipitation01;
        public readonly float Wind01;
        public readonly bool IsExtreme;

        public EnvironmentWeatherState(
            EnvironmentWeatherKind kind,
            float intensity01,
            float precipitation01,
            float wind01,
            bool isExtreme)
        {
            Kind = kind;
            Intensity01 = EnvironmentMath.Clamp01(intensity01);
            Precipitation01 = EnvironmentMath.Clamp01(precipitation01);
            Wind01 = EnvironmentMath.Clamp01(wind01);
            IsExtreme = isExtreme;
        }

        public static EnvironmentWeatherState Clear =>
            new EnvironmentWeatherState(EnvironmentWeatherKind.Clear, 0f, 0f, 0f, false);
    }

    // =============================================================================
    // EnvironmentGlobalClimateState
    // =============================================================================
    /// <summary>
    /// <para>
    /// Stato climatico globale della biosfera.
    /// </para>
    ///
    /// <para><b>Principio architetturale: clima globale prima del microclima</b></para>
    /// <para>
    /// La pagina BIOSFERA richiede una soluzione leggera: temperatura e umidita'
    /// globali, fertilita' per area e acqua stabile. Questa struttura evita di
    /// introdurre subito umidita' per cella o fluidodinamica.
    /// </para>
    ///
    /// <para><b>Struttura interna:</b></para>
    /// <list type="bullet">
    ///   <item><b>Temperature01</b>: temperatura normalizzata.</item>
    ///   <item><b>Humidity01</b>: umidita' globale normalizzata.</item>
    ///   <item><b>Aridity01</b>: aridita' globale normalizzata.</item>
    ///   <item><b>Weather</b>: meteo corrente.</item>
    ///   <item><b>Season</b>: stagione usata per derivare il clima.</item>
    /// </list>
    /// </summary>
    public readonly struct EnvironmentGlobalClimateState
    {
        public readonly float Temperature01;
        public readonly float Humidity01;
        public readonly float Aridity01;
        public readonly EnvironmentWeatherState Weather;
        public readonly EnvironmentSeasonKind Season;

        public EnvironmentGlobalClimateState(
            float temperature01,
            float humidity01,
            float aridity01,
            EnvironmentWeatherState weather,
            EnvironmentSeasonKind season)
        {
            Temperature01 = EnvironmentMath.Clamp01(temperature01);
            Humidity01 = EnvironmentMath.Clamp01(humidity01);
            Aridity01 = EnvironmentMath.Clamp01(aridity01);
            Weather = weather;
            Season = season;
        }
    }

    // =============================================================================
    // EnvironmentSeasonClimateProfile
    // =============================================================================
    /// <summary>
    /// <para>
    /// Profilo climatico stagionale configurabile.
    /// </para>
    ///
    /// <para><b>Principio architetturale: probabilita' in configurazione</b></para>
    /// <para>
    /// Le probabilita' di pioggia, neve, vento e caldo non devono essere sparse nei
    /// sistemi. Questo profilo definisce il contenitore dati che un loader futuro
    /// potra' popolare da JSON.
    /// </para>
    ///
    /// <para><b>Struttura interna:</b></para>
    /// <list type="bullet">
    ///   <item><b>MeanTemperature01</b>: temperatura media normalizzata.</item>
    ///   <item><b>TemperatureVariation01</b>: ampiezza oscillazione.</item>
    ///   <item><b>RainProbability01</b>: probabilita' pioggia.</item>
    ///   <item><b>SnowProbability01</b>: probabilita' neve.</item>
    ///   <item><b>WindProbability01</b>: probabilita' vento.</item>
    ///   <item><b>HeatWaveProbability01</b>: probabilita' caldo estremo.</item>
    ///   <item><b>BaseHumidity01</b>: umidita' stagionale base.</item>
    ///   <item><b>AverageEventDurationHours</b>: durata media eventi.</item>
    /// </list>
    /// </summary>
    public readonly struct EnvironmentSeasonClimateProfile
    {
        public readonly float MeanTemperature01;
        public readonly float TemperatureVariation01;
        public readonly float RainProbability01;
        public readonly float SnowProbability01;
        public readonly float WindProbability01;
        public readonly float HeatWaveProbability01;
        public readonly float BaseHumidity01;
        public readonly int AverageEventDurationHours;

        public EnvironmentSeasonClimateProfile(
            float meanTemperature01,
            float temperatureVariation01,
            float rainProbability01,
            float snowProbability01,
            float windProbability01,
            float heatWaveProbability01,
            float baseHumidity01,
            int averageEventDurationHours)
        {
            MeanTemperature01 = EnvironmentMath.Clamp01(meanTemperature01);
            TemperatureVariation01 = EnvironmentMath.Clamp01(temperatureVariation01);
            RainProbability01 = EnvironmentMath.Clamp01(rainProbability01);
            SnowProbability01 = EnvironmentMath.Clamp01(snowProbability01);
            WindProbability01 = EnvironmentMath.Clamp01(windProbability01);
            HeatWaveProbability01 = EnvironmentMath.Clamp01(heatWaveProbability01);
            BaseHumidity01 = EnvironmentMath.Clamp01(baseHumidity01);
            AverageEventDurationHours = averageEventDurationHours < 0 ? 0 : averageEventDurationHours;
        }
    }
}
