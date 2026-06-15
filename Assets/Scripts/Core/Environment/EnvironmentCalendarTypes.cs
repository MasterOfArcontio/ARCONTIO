namespace Arcontio.Core.Environment
{
    // =============================================================================
    // EnvironmentSeasonKind
    // =============================================================================
    /// <summary>
    /// <para>
    /// Stagione ambientale canonica.
    /// </para>
    ///
    /// <para><b>Principio architetturale: clima derivato, non hardcoded nei sistemi</b></para>
    /// <para>
    /// La stagione e' un input per luce, meteo, crescita e seed bank. Le sue durate
    /// e i profili concreti dovranno arrivare da configurazione, non da costanti
    /// disperse nei sistemi.
    /// </para>
    ///
    /// <para><b>Struttura interna:</b></para>
    /// <list type="bullet">
    ///   <item><b>Spring</b>: stagione di ripresa vegetale.</item>
    ///   <item><b>Summer</b>: stagione calda e luminosa.</item>
    ///   <item><b>Autumn</b>: stagione umida e di produzione semi/frutti.</item>
    ///   <item><b>Winter</b>: stagione fredda e di dormienza.</item>
    /// </list>
    /// </summary>
    public enum EnvironmentSeasonKind
    {
        Spring = 0,
        Summer = 1,
        Autumn = 2,
        Winter = 3
    }

    // =============================================================================
    // EnvironmentDate
    // =============================================================================
    /// <summary>
    /// <para>
    /// Data ambientale discreta derivata dal calendario.
    /// </para>
    ///
    /// <para><b>Principio architetturale: tempo ambientale leggibile</b></para>
    /// <para>
    /// La biosfera deve poter ragionare per giorno, mese, anno e stagione senza
    /// dipendere dal frame loop Unity. Questa struttura e' una fotografia value-only
    /// del calendario, non un sistema che avanza il tempo.
    /// </para>
    ///
    /// <para><b>Struttura interna:</b></para>
    /// <list type="bullet">
    ///   <item><b>Year</b>: anno simulato.</item>
    ///   <item><b>Month</b>: mese simulato, configurabile nella semantica futura.</item>
    ///   <item><b>DayOfMonth</b>: giorno dentro il mese.</item>
    ///   <item><b>DayOfYear</b>: giorno assoluto dentro l'anno.</item>
    ///   <item><b>Season</b>: stagione derivata.</item>
    ///   <item><b>DayInSeason</b>: indice giorno dentro la stagione.</item>
    /// </list>
    /// </summary>
    public readonly struct EnvironmentDate
    {
        public readonly int Year;
        public readonly int Month;
        public readonly int DayOfMonth;
        public readonly int DayOfYear;
        public readonly EnvironmentSeasonKind Season;
        public readonly int DayInSeason;

        public EnvironmentDate(
            int year,
            int month,
            int dayOfMonth,
            int dayOfYear,
            EnvironmentSeasonKind season,
            int dayInSeason)
        {
            // Normalizzazione prudente: il calendario produttivo futuro decidera'
            // bounds esatti, qui impediamo solo valori negativi nel dato base.
            Year = year < 0 ? 0 : year;
            Month = month < 0 ? 0 : month;
            DayOfMonth = dayOfMonth < 0 ? 0 : dayOfMonth;
            DayOfYear = dayOfYear < 0 ? 0 : dayOfYear;
            Season = season;
            DayInSeason = dayInSeason < 0 ? 0 : dayInSeason;
        }
    }

    // =============================================================================
    // EnvironmentTimeOfDay
    // =============================================================================
    /// <summary>
    /// <para>
    /// Ora ambientale normalizzata dentro il giorno simulato.
    /// </para>
    ///
    /// <para><b>Principio architetturale: giorno/notte come dato derivato</b></para>
    /// <para>
    /// Luce, meteo e comportamento futuro possono leggere questa struttura, ma non
    /// devono possedere il tempo. La baseline progettuale stabilita e':
    /// <c>24 ore simulate = 20 minuti reali</c>, con valori futuri configurabili.
    /// </para>
    ///
    /// <para><b>Struttura interna:</b></para>
    /// <list type="bullet">
    ///   <item><b>Hour</b>: ora simulata 0-23 nella baseline.</item>
    ///   <item><b>Minute</b>: minuto simulato.</item>
    ///   <item><b>NormalizedDay01</b>: posizione normalizzata nel giorno.</item>
    /// </list>
    /// </summary>
    public readonly struct EnvironmentTimeOfDay
    {
        public readonly int Hour;
        public readonly int Minute;
        public readonly float NormalizedDay01;

        public EnvironmentTimeOfDay(int hour, int minute, float normalizedDay01)
        {
            Hour = hour < 0 ? 0 : hour;
            Minute = minute < 0 ? 0 : minute;
            NormalizedDay01 = EnvironmentMath.Clamp01(normalizedDay01);
        }
    }

    // =============================================================================
    // EnvironmentCalendarState
    // =============================================================================
    /// <summary>
    /// <para>
    /// Stato calendario ambientale passivo.
    /// </para>
    ///
    /// <para><b>Principio architetturale: configurazione prima delle costanti</b></para>
    /// <para>
    /// I valori di durata giorno, mesi e stagioni devono finire in file di
    /// configurazione. Questa struttura conserva lo stato gia' risolto, evitando che
    /// i consumer debbano conoscere la matematica del calendario.
    /// </para>
    ///
    /// <para><b>Struttura interna:</b></para>
    /// <list type="bullet">
    ///   <item><b>ElapsedEnvironmentTicks</b>: tempo ambientale discreto futuro.</item>
    ///   <item><b>Date</b>: data derivata.</item>
    ///   <item><b>TimeOfDay</b>: ora nel giorno.</item>
    ///   <item><b>DaylightHours</b>: ore luce della giornata.</item>
    ///   <item><b>LightIntensity01</b>: intensita' luce globale gia' risolta.</item>
    /// </list>
    /// </summary>
    public readonly struct EnvironmentCalendarState
    {
        public readonly long ElapsedEnvironmentTicks;
        public readonly EnvironmentDate Date;
        public readonly EnvironmentTimeOfDay TimeOfDay;
        public readonly float DaylightHours;
        public readonly float LightIntensity01;

        public EnvironmentCalendarState(
            long elapsedEnvironmentTicks,
            EnvironmentDate date,
            EnvironmentTimeOfDay timeOfDay,
            float daylightHours,
            float lightIntensity01)
        {
            ElapsedEnvironmentTicks = elapsedEnvironmentTicks < 0 ? 0 : elapsedEnvironmentTicks;
            Date = date;
            TimeOfDay = timeOfDay;
            DaylightHours = daylightHours < 0f ? 0f : daylightHours;
            LightIntensity01 = EnvironmentMath.Clamp01(lightIntensity01);
        }
    }

    // =============================================================================
    // EnvironmentSeasonProfile
    // =============================================================================
    /// <summary>
    /// <para>
    /// Profilo stagionale passivo per luce e bias ambientali.
    /// </para>
    ///
    /// <para><b>Principio architetturale: dati di bilanciamento esterni</b></para>
    /// <para>
    /// La foundation conserva la forma dei dati, ma non impone ancora un loader.
    /// In v0.40 questi valori dovranno essere letti da configurazione dedicata.
    /// </para>
    ///
    /// <para><b>Struttura interna:</b></para>
    /// <list type="bullet">
    ///   <item><b>Season</b>: stagione descritta.</item>
    ///   <item><b>DaylightHours</b>: ore di luce baseline.</item>
    ///   <item><b>TemperatureBias01</b>: bias termico normalizzato.</item>
    ///   <item><b>RainfallBias01</b>: bias precipitazioni.</item>
    ///   <item><b>FertilityBias01</b>: bias fertilita'/recupero.</item>
    ///   <item><b>VegetationGrowthBias01</b>: bias crescita vegetale.</item>
    /// </list>
    /// </summary>
    public readonly struct EnvironmentSeasonProfile
    {
        public readonly EnvironmentSeasonKind Season;
        public readonly float DaylightHours;
        public readonly float TemperatureBias01;
        public readonly float RainfallBias01;
        public readonly float FertilityBias01;
        public readonly float VegetationGrowthBias01;

        public EnvironmentSeasonProfile(
            EnvironmentSeasonKind season,
            float daylightHours,
            float temperatureBias01,
            float rainfallBias01,
            float fertilityBias01,
            float vegetationGrowthBias01)
        {
            Season = season;
            DaylightHours = daylightHours < 0f ? 0f : daylightHours;
            TemperatureBias01 = EnvironmentMath.Clamp01(temperatureBias01);
            RainfallBias01 = EnvironmentMath.Clamp01(rainfallBias01);
            FertilityBias01 = EnvironmentMath.Clamp01(fertilityBias01);
            VegetationGrowthBias01 = EnvironmentMath.Clamp01(vegetationGrowthBias01);
        }
    }
}
