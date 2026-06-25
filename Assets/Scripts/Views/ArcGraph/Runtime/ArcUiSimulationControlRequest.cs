namespace Arcontio.View.ArcGraph
{
    // =============================================================================
    // ArcUiSimulationControlRequestKind
    // =============================================================================
    /// <summary>
    /// <para>
    /// Tipo minimale di richiesta UI per il controllo temporale della simulazione.
    /// </para>
    ///
    /// <para><b>Principio architetturale: intenzione temporale separata dal bottone</b></para>
    /// <para>
    /// La TopBar non deve decidere direttamente come mutare il simulatore. Questa
    /// enum descrive solo l'intenzione dell'utente: mettere in pausa, riprendere o
    /// richiedere una velocita' o usare il fast-forward debug della Biosfera. Il
    /// controller autorizzato decide cosa puo' essere applicato al runtime corrente.
    /// </para>
    ///
    /// <para><b>Struttura interna:</b></para>
    /// <list type="bullet">
    ///   <item><b>None</b>: nessuna richiesta valida.</item>
    ///   <item><b>Pause</b>: richiesta di pausa.</item>
    ///   <item><b>Resume</b>: richiesta di ripresa.</item>
    ///   <item><b>SetSpeed</b>: richiesta di fattore velocita' UI.</item>
    ///   <item><b>SetBiosphereDebugFastForwardMultiplier</b>: scelta x50/x100/x200/x500.</item>
    ///   <item><b>StartBiosphereDebugFastForward</b>: avvio del fast-forward solo Biosfera.</item>
    ///   <item><b>StopBiosphereDebugFastForward</b>: stop del fast-forward solo Biosfera.</item>
    /// </list>
    /// </summary>
    public enum ArcUiSimulationControlRequestKind
    {
        None = 0,
        Pause = 1,
        Resume = 2,
        SetSpeed = 3,
        SetBiosphereDebugFastForwardMultiplier = 4,
        StartBiosphereDebugFastForward = 5,
        StopBiosphereDebugFastForward = 6
    }

    // =============================================================================
    // ArcUiSimulationControlRequest
    // =============================================================================
    /// <summary>
    /// <para>
    /// Richiesta asciutta prodotta dalla TopBar ArcGraph.
    /// </para>
    ///
    /// <para><b>Principio architetturale: richiesta UI, non comando Core</b></para>
    /// <para>
    /// La richiesta non contiene riferimenti a <c>SimulationHost</c>, <c>World</c>,
    /// scheduler o sistemi. Trasporta soltanto tipo, fattore velocita',
    /// moltiplicatore debug Biosfera e sorgente.
    /// Questo mantiene esplicito il passaggio TopBar -> controller autorizzato.
    /// </para>
    ///
    /// <para><b>Struttura interna:</b></para>
    /// <list type="bullet">
    ///   <item><b>Kind</b>: intenzione temporale richiesta.</item>
    ///   <item><b>SpeedMultiplier</b>: fattore normalizzato tra 1 e 4.</item>
    ///   <item><b>BiosphereDebugFastForwardMultiplier</b>: fattore x50/x100/x200/x500.</item>
    ///   <item><b>Source</b>: nome del componente UI che ha prodotto la richiesta.</item>
    ///   <item><b>IsValid</b>: true solo per richieste semanticamente utilizzabili.</item>
    /// </list>
    /// </summary>
    public readonly struct ArcUiSimulationControlRequest
    {
        public readonly ArcUiSimulationControlRequestKind Kind;
        public readonly int SpeedMultiplier;
        public readonly int BiosphereDebugFastForwardMultiplier;
        public readonly string Source;

        public bool IsValid => Kind != ArcUiSimulationControlRequestKind.None;
        public bool IsPause => Kind == ArcUiSimulationControlRequestKind.Pause;
        public bool IsResume => Kind == ArcUiSimulationControlRequestKind.Resume;
        public bool IsSetSpeed => Kind == ArcUiSimulationControlRequestKind.SetSpeed;
        public bool IsSetBiosphereDebugFastForwardMultiplier =>
            Kind == ArcUiSimulationControlRequestKind.SetBiosphereDebugFastForwardMultiplier;
        public bool IsStartBiosphereDebugFastForward =>
            Kind == ArcUiSimulationControlRequestKind.StartBiosphereDebugFastForward;
        public bool IsStopBiosphereDebugFastForward =>
            Kind == ArcUiSimulationControlRequestKind.StopBiosphereDebugFastForward;

        // =============================================================================
        // ArcUiSimulationControlRequest
        // =============================================================================
        /// <summary>
        /// <para>
        /// Costruisce una richiesta normalizzando sorgente e fattore velocita'.
        /// </para>
        /// </summary>
        public ArcUiSimulationControlRequest(
            ArcUiSimulationControlRequestKind kind,
            int speedMultiplier,
            int biosphereDebugFastForwardMultiplier,
            string source)
        {
            Kind = kind;
            SpeedMultiplier = NormalizeSpeedMultiplier(speedMultiplier);
            BiosphereDebugFastForwardMultiplier =
                NormalizeBiosphereDebugFastForwardMultiplier(biosphereDebugFastForwardMultiplier);
            Source = string.IsNullOrWhiteSpace(source) ? string.Empty : source.Trim();
        }

        public ArcUiSimulationControlRequest(
            ArcUiSimulationControlRequestKind kind,
            int speedMultiplier,
            string source)
            : this(
                kind,
                speedMultiplier,
                50,
                source)
        {
        }

        // =============================================================================
        // Pause
        // =============================================================================
        /// <summary>
        /// <para>
        /// Crea una richiesta di pausa della simulazione.
        /// </para>
        /// </summary>
        public static ArcUiSimulationControlRequest Pause(string source)
        {
            return new ArcUiSimulationControlRequest(
                ArcUiSimulationControlRequestKind.Pause,
                1,
                source);
        }

        // =============================================================================
        // Resume
        // =============================================================================
        /// <summary>
        /// <para>
        /// Crea una richiesta di ripresa della simulazione.
        /// </para>
        /// </summary>
        public static ArcUiSimulationControlRequest Resume(string source)
        {
            return new ArcUiSimulationControlRequest(
                ArcUiSimulationControlRequestKind.Resume,
                1,
                source);
        }

        // =============================================================================
        // SetSpeed
        // =============================================================================
        /// <summary>
        /// <para>
        /// Crea una richiesta di cambio velocita' UI.
        /// </para>
        /// </summary>
        public static ArcUiSimulationControlRequest SetSpeed(
            int speedMultiplier,
            string source)
        {
            return new ArcUiSimulationControlRequest(
                ArcUiSimulationControlRequestKind.SetSpeed,
                speedMultiplier,
                source);
        }

        // =============================================================================
        // SetBiosphereDebugFastForwardMultiplier
        // =============================================================================
        /// <summary>
        /// <para>
        /// Crea una richiesta di scelta moltiplicatore per il fast-forward Biosfera.
        /// </para>
        /// </summary>
        public static ArcUiSimulationControlRequest SetBiosphereDebugFastForwardMultiplier(
            int multiplier,
            string source)
        {
            return new ArcUiSimulationControlRequest(
                ArcUiSimulationControlRequestKind.SetBiosphereDebugFastForwardMultiplier,
                1,
                multiplier,
                source);
        }

        // =============================================================================
        // StartBiosphereDebugFastForward
        // =============================================================================
        /// <summary>
        /// <para>
        /// Crea una richiesta di avvio del fast-forward debug solo Biosfera.
        /// </para>
        /// </summary>
        public static ArcUiSimulationControlRequest StartBiosphereDebugFastForward(
            int multiplier,
            string source)
        {
            return new ArcUiSimulationControlRequest(
                ArcUiSimulationControlRequestKind.StartBiosphereDebugFastForward,
                1,
                multiplier,
                source);
        }

        // =============================================================================
        // StopBiosphereDebugFastForward
        // =============================================================================
        /// <summary>
        /// <para>
        /// Crea una richiesta di stop del fast-forward debug solo Biosfera.
        /// </para>
        /// </summary>
        public static ArcUiSimulationControlRequest StopBiosphereDebugFastForward(string source)
        {
            return new ArcUiSimulationControlRequest(
                ArcUiSimulationControlRequestKind.StopBiosphereDebugFastForward,
                1,
                50,
                source);
        }

        // =============================================================================
        // NormalizeSpeedMultiplier
        // =============================================================================
        /// <summary>
        /// <para>
        /// Normalizza il fattore velocita' nel range operativo iniziale x1-x4.
        /// </para>
        /// </summary>
        public static int NormalizeSpeedMultiplier(int speedMultiplier)
        {
            if (speedMultiplier < 1)
                return 1;

            return speedMultiplier > 4 ? 4 : speedMultiplier;
        }

        // =============================================================================
        // NormalizeBiosphereDebugFastForwardMultiplier
        // =============================================================================
        /// <summary>
        /// <para>
        /// Normalizza il fattore debug Biosfera sui valori previsti dalla TopBar.
        /// </para>
        /// </summary>
        public static int NormalizeBiosphereDebugFastForwardMultiplier(int multiplier)
        {
            if (multiplier <= 50)
                return 50;

            if (multiplier <= 100)
                return 100;

            if (multiplier <= 200)
                return 200;

            return 500;
        }
    }

    // =============================================================================
    // ArcUiEnvironmentStatusSnapshot
    // =============================================================================
    /// <summary>
    /// <para>
    /// Snapshot compatto di tempo, calendario e clima mostrabile dalla TopBar.
    /// </para>
    ///
    /// <para><b>Principio architetturale: Environment -> Snapshot -> UI</b></para>
    /// <para>
    /// La TopBar non deve leggere direttamente <c>World</c> o
    /// <c>EnvironmentState</c>. Questo contratto trasporta valori gia' filtrati dal
    /// controller autorizzato: dati numerici semplici per future view Biosfera e
    /// label gia' pronte per la UI provvisoria.
    /// </para>
    ///
    /// <para><b>Struttura interna:</b></para>
    /// <list type="bullet">
    ///   <item><b>HasCalendar</b>: indica se i campi data/ora sono disponibili.</item>
    ///   <item><b>HasClimate</b>: indica se temperatura, umidita' e meteo sono disponibili.</item>
    ///   <item><b>Campi numerici</b>: giorno, mese, anno, ora, temperatura e umidita'.</item>
    ///   <item><b>Label</b>: stringhe gia' formattate per la TopBar UGUI.</item>
    /// </list>
    /// </summary>
    public readonly struct ArcUiEnvironmentStatusSnapshot
    {
        public readonly bool HasCalendar;
        public readonly bool HasClimate;
        public readonly int Year;
        public readonly int Month;
        public readonly int DayOfMonth;
        public readonly int DayOfYear;
        public readonly int Hour;
        public readonly int Minute;
        public readonly string SeasonKey;
        public readonly string SeasonLabel;
        public readonly float Temperature01;
        public readonly float Humidity01;
        public readonly string WeatherKey;
        public readonly string WeatherLabel;
        public readonly string DayLabel;
        public readonly string MonthLabel;
        public readonly string YearLabel;
        public readonly string TimeLabel;
        public readonly string TemperatureLabel;
        public readonly string HumidityLabel;

        public bool HasAnyStatus => HasCalendar || HasClimate;

        // =============================================================================
        // ArcUiEnvironmentStatusSnapshot
        // =============================================================================
        /// <summary>
        /// <para>
        /// Costruisce uno snapshot ambiente normalizzando label e valori minimi.
        /// </para>
        /// </summary>
        public ArcUiEnvironmentStatusSnapshot(
            bool hasCalendar,
            bool hasClimate,
            int year,
            int month,
            int dayOfMonth,
            int dayOfYear,
            int hour,
            int minute,
            string seasonKey,
            string seasonLabel,
            float temperature01,
            float humidity01,
            string weatherKey,
            string weatherLabel,
            string dayLabel,
            string monthLabel,
            string yearLabel,
            string timeLabel,
            string temperatureLabel,
            string humidityLabel)
        {
            HasCalendar = hasCalendar;
            HasClimate = hasClimate;
            Year = year < 0 ? 0 : year;
            Month = month < 0 ? 0 : month;
            DayOfMonth = dayOfMonth < 0 ? 0 : dayOfMonth;
            DayOfYear = dayOfYear < 0 ? 0 : dayOfYear;
            Hour = hour < 0 ? 0 : hour;
            Minute = minute < 0 ? 0 : minute;
            SeasonKey = string.IsNullOrWhiteSpace(seasonKey) ? string.Empty : seasonKey;
            SeasonLabel = string.IsNullOrWhiteSpace(seasonLabel) ? "Stagione --" : seasonLabel;
            Temperature01 = Clamp01(temperature01);
            Humidity01 = Clamp01(humidity01);
            WeatherKey = string.IsNullOrWhiteSpace(weatherKey) ? string.Empty : weatherKey;
            WeatherLabel = string.IsNullOrWhiteSpace(weatherLabel) ? "Meteo --" : weatherLabel;
            DayLabel = string.IsNullOrWhiteSpace(dayLabel) ? "Giorno --" : dayLabel;
            MonthLabel = string.IsNullOrWhiteSpace(monthLabel) ? "Mese --" : monthLabel;
            YearLabel = string.IsNullOrWhiteSpace(yearLabel) ? "Anno ----" : yearLabel;
            TimeLabel = string.IsNullOrWhiteSpace(timeLabel) ? "--:--" : timeLabel;
            TemperatureLabel = string.IsNullOrWhiteSpace(temperatureLabel) ? "-- C" : temperatureLabel;
            HumidityLabel = string.IsNullOrWhiteSpace(humidityLabel) ? "-- %" : humidityLabel;
        }

        // =============================================================================
        // Empty
        // =============================================================================
        /// <summary>
        /// <para>
        /// Restituisce uno snapshot vuoto ma visualizzabile dalla TopBar.
        /// </para>
        /// </summary>
        public static ArcUiEnvironmentStatusSnapshot Empty()
        {
            return new ArcUiEnvironmentStatusSnapshot(
                false,
                false,
                0,
                0,
                0,
                0,
                0,
                0,
                string.Empty,
                "Stagione --",
                0f,
                0f,
                string.Empty,
                "Meteo --",
                "Giorno --",
                "Mese --",
                "Anno ----",
                "--:--",
                "-- C",
                "-- %");
        }

        private static float Clamp01(float value)
        {
            if (value < 0f)
                return 0f;

            return value > 1f ? 1f : value;
        }
    }

    // =============================================================================
    // ArcUiSimulationControlState
    // =============================================================================
    /// <summary>
    /// <para>
    /// Snapshot UI del controllo temporale mostrabile dalla TopBar.
    /// </para>
    ///
    /// <para><b>Principio architetturale: stato leggibile dalla UI</b></para>
    /// <para>
    /// La TopBar deve poter aggiornare etichette e pulsanti senza interrogare il
    /// <c>SimulationHost</c>. Il controller prepara quindi uno snapshot compatto
    /// con pausa, velocita' richiesta e tick corrente se disponibile.
    /// </para>
    ///
    /// <para><b>Struttura interna:</b></para>
    /// <list type="bullet">
    ///   <item><b>HasRuntimeHost</b>: true quando il controller ha un SimulationHost.</item>
    ///   <item><b>IsPaused</b>: stato pausa letto dal controller.</item>
    ///   <item><b>SpeedMultiplier</b>: fattore velocita' richiesto dalla UI.</item>
    ///   <item><b>TickIndex</b>: tick corrente noto, o 0 se non disponibile.</item>
    ///   <item><b>EnvironmentStatus</b>: snapshot tempo/clima separato dal controllo pausa.</item>
    /// </list>
    /// </summary>
    public readonly struct ArcUiSimulationControlState
    {
        public readonly bool HasRuntimeHost;
        public readonly bool IsPaused;
        public readonly int SpeedMultiplier;
        public readonly long TickIndex;
        public readonly bool BiosphereDebugFastForwardActive;
        public readonly int BiosphereDebugFastForwardMultiplier;
        public readonly ArcUiEnvironmentStatusSnapshot EnvironmentStatus;
        public string DayLabel => string.IsNullOrWhiteSpace(EnvironmentStatus.DayLabel) ? "Giorno --" : EnvironmentStatus.DayLabel;
        public string MonthLabel => string.IsNullOrWhiteSpace(EnvironmentStatus.MonthLabel) ? "Mese --" : EnvironmentStatus.MonthLabel;
        public string YearLabel => string.IsNullOrWhiteSpace(EnvironmentStatus.YearLabel) ? "Anno ----" : EnvironmentStatus.YearLabel;
        public string SeasonLabel => string.IsNullOrWhiteSpace(EnvironmentStatus.SeasonLabel) ? "Stagione --" : EnvironmentStatus.SeasonLabel;
        public string TimeLabel => string.IsNullOrWhiteSpace(EnvironmentStatus.TimeLabel) ? "--:--" : EnvironmentStatus.TimeLabel;
        public string TemperatureLabel => string.IsNullOrWhiteSpace(EnvironmentStatus.TemperatureLabel) ? "-- C" : EnvironmentStatus.TemperatureLabel;
        public string HumidityLabel => string.IsNullOrWhiteSpace(EnvironmentStatus.HumidityLabel) ? "-- %" : EnvironmentStatus.HumidityLabel;
        public string WeatherLabel => string.IsNullOrWhiteSpace(EnvironmentStatus.WeatherLabel) ? "Meteo --" : EnvironmentStatus.WeatherLabel;

        public ArcUiSimulationControlState(
            bool hasRuntimeHost,
            bool isPaused,
            int speedMultiplier,
            long tickIndex,
            bool biosphereDebugFastForwardActive = false,
            int biosphereDebugFastForwardMultiplier = 50,
            ArcUiEnvironmentStatusSnapshot environmentStatus = default)
        {
            HasRuntimeHost = hasRuntimeHost;
            IsPaused = isPaused;
            SpeedMultiplier = ArcUiSimulationControlRequest.NormalizeSpeedMultiplier(speedMultiplier);
            TickIndex = tickIndex < 0L ? 0L : tickIndex;
            BiosphereDebugFastForwardActive = biosphereDebugFastForwardActive;
            BiosphereDebugFastForwardMultiplier =
                ArcUiSimulationControlRequest.NormalizeBiosphereDebugFastForwardMultiplier(
                    biosphereDebugFastForwardMultiplier);
            EnvironmentStatus = environmentStatus.HasAnyStatus
                ? environmentStatus
                : ArcUiEnvironmentStatusSnapshot.Empty();
        }
    }
}
