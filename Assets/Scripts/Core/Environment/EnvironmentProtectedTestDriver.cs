using System;

namespace Arcontio.Core.Environment
{
    // =============================================================================
    // EnvironmentProtectedTestBiomePreset
    // =============================================================================
    /// <summary>
    /// <para>
    /// Preset biome disponibili nello scenario protetto della biosfera.
    /// </para>
    ///
    /// <para><b>Principio architetturale: test comparativo dei biomi</b></para>
    /// <para>
    /// Il controller protetto deve poter confrontare lo stesso modello ecologico con
    /// target diversi. Questi preset non sono il catalogo finale dei biomi: sono una
    /// finestra di test su profili biome gia' data-driven.
    /// </para>
    ///
    /// <para><b>Struttura interna:</b></para>
    /// <list type="bullet">
    ///   <item><b>TemperateGrassland</b>: prato temperato fertile.</item>
    ///   <item><b>Desert</b>: ambiente arido con bassa densita' vegetale.</item>
    ///   <item><b>Jungle</b>: ambiente umido ad alta densita'.</item>
    ///   <item><b>Tundra</b>: ambiente freddo, stagionale e lento.</item>
    /// </list>
    /// </summary>
    public enum EnvironmentProtectedTestBiomePreset
    {
        TemperateGrassland = 0,
        Desert = 10,
        Jungle = 20,
        Tundra = 30
    }

    // =============================================================================
    // EnvironmentProtectedTestSpeedPreset
    // =============================================================================
    /// <summary>
    /// <para>
    /// Preset di velocita' per i test protetti della biosfera.
    /// </para>
    ///
    /// <para><b>Principio architetturale: tempo accelerato fuori dalla simulazione stabile</b></para>
    /// <para>
    /// Questi preset non definiscono il runtime ufficiale di ARCONTIO. Servono solo
    /// a pilotare la foundation ambientale in un ambiente protetto, cosi' possiamo
    /// osservare giorni, mesi, stagioni e anni senza collegare ancora la biosfera a
    /// <c>SimulationHost</c>, <c>World</c> o adapter grafici.
    /// </para>
    ///
    /// <para><b>Struttura interna:</b></para>
    /// <list type="bullet">
    ///   <item><b>Paused</b>: nessun avanzamento automatico.</item>
    ///   <item><b>BaselineTwentyMinutesPerDay</b>: baseline di progetto, 24 ore simulate in 20 minuti reali.</item>
    ///   <item><b>OneDayPerTenSeconds</b>: avanzamento leggibile per osservare transizioni giornaliere.</item>
    ///   <item><b>OneDayPerSecond</b>: avanzamento rapido per cicli naturali brevi.</item>
    ///   <item><b>OneMonthPerTenSeconds</b>: test compatto di confini mensili.</item>
    ///   <item><b>OneSeasonPerTenSeconds</b>: test compatto di cambio stagione.</item>
    ///   <item><b>OneYearPerMinute</b>: test lungo ma ancora osservabile.</item>
    /// </list>
    /// </summary>
    public enum EnvironmentProtectedTestSpeedPreset
    {
        Paused = 0,
        BaselineTwentyMinutesPerDay = 10,
        OneDayPerTenSeconds = 20,
        OneDayPerSecond = 30,
        OneMonthPerTenSeconds = 40,
        OneSeasonPerTenSeconds = 50,
        OneYearPerMinute = 60
    }

    // =============================================================================
    // EnvironmentProtectedTestSpeedProfile
    // =============================================================================
    /// <summary>
    /// <para>
    /// Profilo risolto di avanzamento tick/secondo per un preset protetto.
    /// </para>
    ///
    /// <para><b>Principio architetturale: conversione esplicita, non frame magic</b></para>
    /// <para>
    /// Il profilo traduce secondi reali in tick ambientali, ma non legge Unity e non
    /// decide da solo quando girare. Il chiamante consegna i secondi reali e il Core
    /// restituisce dati osservabili.
    /// </para>
    ///
    /// <para><b>Struttura interna:</b></para>
    /// <list type="bullet">
    ///   <item><b>Preset</b>: preset sorgente.</item>
    ///   <item><b>DisplayName</b>: nome leggibile per pannelli debug.</item>
    ///   <item><b>EnvironmentTicksPerRealSecond</b>: conversione finale.</item>
    ///   <item><b>IsRunning</b>: indica se il profilo produce avanzamento.</item>
    /// </list>
    /// </summary>
    public readonly struct EnvironmentProtectedTestSpeedProfile
    {
        public readonly EnvironmentProtectedTestSpeedPreset Preset;
        public readonly string DisplayName;
        public readonly double EnvironmentTicksPerRealSecond;

        public bool IsRunning => EnvironmentTicksPerRealSecond > 0d;

        // =============================================================================
        // EnvironmentProtectedTestSpeedProfile
        // =============================================================================
        /// <summary>
        /// <para>
        /// Costruisce un profilo gia' normalizzato.
        /// </para>
        /// </summary>
        public EnvironmentProtectedTestSpeedProfile(
            EnvironmentProtectedTestSpeedPreset preset,
            string displayName,
            double environmentTicksPerRealSecond)
        {
            Preset = preset;
            DisplayName = string.IsNullOrWhiteSpace(displayName)
                ? preset.ToString()
                : displayName;
            EnvironmentTicksPerRealSecond =
                environmentTicksPerRealSecond < 0d ? 0d : environmentTicksPerRealSecond;
        }

        // =============================================================================
        // FromPreset
        // =============================================================================
        /// <summary>
        /// <para>
        /// Risolve un preset usando la configurazione calendario corrente.
        /// </para>
        /// </summary>
        public static EnvironmentProtectedTestSpeedProfile FromPreset(
            EnvironmentProtectedTestSpeedPreset preset,
            EnvironmentCalendarConfig calendarConfig)
        {
            var safeCalendar = calendarConfig ?? new EnvironmentCalendarConfig();
            double dayTicks = EnvironmentProtectedTestDriver.ResolveTicksPerDay(safeCalendar);
            double monthTicks = dayTicks * safeCalendar.ResolveDaysPerMonth();
            double seasonTicks = monthTicks * safeCalendar.ResolveMonthsPerSeason();
            double yearTicks = monthTicks * safeCalendar.ResolveMonthsPerYear();

            // La baseline richiesta dall'operatore e' esplicita: 24 ore simulate in
            // 20 minuti reali. Con i default attuali equivale a 1200 tick in 1200 s.
            switch (preset)
            {
                case EnvironmentProtectedTestSpeedPreset.BaselineTwentyMinutesPerDay:
                    return new EnvironmentProtectedTestSpeedProfile(
                        preset,
                        "Baseline 24h sim / 20m reali",
                        dayTicks / (20d * 60d));

                case EnvironmentProtectedTestSpeedPreset.OneDayPerTenSeconds:
                    return new EnvironmentProtectedTestSpeedProfile(
                        preset,
                        "1 giorno / 10 s",
                        dayTicks / 10d);

                case EnvironmentProtectedTestSpeedPreset.OneDayPerSecond:
                    return new EnvironmentProtectedTestSpeedProfile(
                        preset,
                        "1 giorno / 1 s",
                        dayTicks);

                case EnvironmentProtectedTestSpeedPreset.OneMonthPerTenSeconds:
                    return new EnvironmentProtectedTestSpeedProfile(
                        preset,
                        "1 mese / 10 s",
                        monthTicks / 10d);

                case EnvironmentProtectedTestSpeedPreset.OneSeasonPerTenSeconds:
                    return new EnvironmentProtectedTestSpeedProfile(
                        preset,
                        "1 stagione / 10 s",
                        seasonTicks / 10d);

                case EnvironmentProtectedTestSpeedPreset.OneYearPerMinute:
                    return new EnvironmentProtectedTestSpeedProfile(
                        preset,
                        "1 anno / 60 s",
                        yearTicks / 60d);

                default:
                    return new EnvironmentProtectedTestSpeedProfile(
                        EnvironmentProtectedTestSpeedPreset.Paused,
                        "Pausa",
                        0d);
            }
        }
    }

    // =============================================================================
    // EnvironmentProtectedTestAdvanceReport
    // =============================================================================
    /// <summary>
    /// <para>
    /// Report aggregato di un avanzamento protetto della biosfera.
    /// </para>
    ///
    /// <para><b>Principio architetturale: test osservabile senza log globale</b></para>
    /// <para>
    /// Il driver espone i confini attraversati, l'ultimo risultato Core e il full
    /// snapshot corrente. Un controller visuale puo' leggere questi dati senza
    /// accedere allo stato mutabile interno e senza generare side effect nel mondo.
    /// </para>
    ///
    /// <para><b>Struttura interna:</b></para>
    /// <list type="bullet">
    ///   <item><b>PreviousTicks/CurrentTicks/TicksAdvanced</b>: delta temporale aggregato.</item>
    ///   <item><b>BatchesExecuted</b>: numero di step Core usati per preservare cadenze giornaliere.</item>
    ///   <item><b>*Changed</b>: confini attraversati almeno una volta nel batch.</item>
    ///   <item><b>RanNaturalGrowth</b>: indica se il ciclo naturale giornaliero e' stato invocato.</item>
    ///   <item><b>LastAdvance/LastGrowthReport/FullSnapshot</b>: dati diagnostici per UI e harness.</item>
    /// </list>
    /// </summary>
    public sealed class EnvironmentProtectedTestAdvanceReport
    {
        public long PreviousTicks { get; }
        public long CurrentTicks { get; }
        public long TicksAdvanced { get; }
        public int BatchesExecuted { get; }
        public bool HourChanged { get; }
        public bool DayChanged { get; }
        public bool MonthChanged { get; }
        public bool SeasonChanged { get; }
        public bool YearChanged { get; }
        public bool RanNaturalGrowth { get; }
        public EnvironmentAdvanceResult LastAdvance { get; }
        public EnvironmentNaturalGrowthReport LastGrowthReport { get; }
        public EnvironmentFullSnapshot FullSnapshot { get; }

        // =============================================================================
        // EnvironmentProtectedTestAdvanceReport
        // =============================================================================
        /// <summary>
        /// <para>
        /// Costruisce il report normalizzando tick e conteggi negativi.
        /// </para>
        /// </summary>
        public EnvironmentProtectedTestAdvanceReport(
            long previousTicks,
            long currentTicks,
            int batchesExecuted,
            bool hourChanged,
            bool dayChanged,
            bool monthChanged,
            bool seasonChanged,
            bool yearChanged,
            bool ranNaturalGrowth,
            EnvironmentAdvanceResult lastAdvance,
            EnvironmentNaturalGrowthReport lastGrowthReport,
            EnvironmentFullSnapshot fullSnapshot)
        {
            PreviousTicks = previousTicks < 0 ? 0 : previousTicks;
            CurrentTicks = currentTicks < PreviousTicks ? PreviousTicks : currentTicks;
            TicksAdvanced = CurrentTicks - PreviousTicks;
            BatchesExecuted = batchesExecuted < 0 ? 0 : batchesExecuted;
            HourChanged = hourChanged;
            DayChanged = dayChanged;
            MonthChanged = monthChanged;
            SeasonChanged = seasonChanged;
            YearChanged = yearChanged;
            RanNaturalGrowth = ranNaturalGrowth;
            LastAdvance = lastAdvance;
            LastGrowthReport = lastGrowthReport;
            FullSnapshot = fullSnapshot;
        }
    }

    // =============================================================================
    // EnvironmentProtectedTestDriver
    // =============================================================================
    /// <summary>
    /// <para>
    /// Driver data-only per test protetti della biosfera.
    /// </para>
    ///
    /// <para><b>Principio architetturale: adapter di test, non sistema runtime</b></para>
    /// <para>
    /// Il driver possiede uno stato ambientale locale e lo avanza con resolver Core
    /// gia' esistenti. Non usa <c>SimulationHost</c>, non legge <c>World</c>, non
    /// conosce ArcGraph e non crea rendering. Serve a testare velocemente calendario,
    /// clima, aree, seed bank e crescita naturale finche' la biosfera non avra' un
    /// ponte runtime ufficiale.
    /// </para>
    ///
    /// <para><b>Struttura interna:</b></para>
    /// <list type="bullet">
    ///   <item><b>Bootstrap/ResetToProtectedDefaults</b>: inizializzano uno scenario minimo leggibile.</item>
    ///   <item><b>Advance*</b>: avanzano tick, ore, giorni, mesi, stagioni o anni.</item>
    ///   <item><b>AdvanceRealSeconds</b>: applica un preset accelerato da controller visuale.</item>
    ///   <item><b>Snapshot/FullSnapshot</b>: espongono viste read-only per debug e futuri consumer.</item>
    /// </list>
    /// </summary>
    public sealed class EnvironmentProtectedTestDriver
    {
        private EnvironmentFoundationConfig _config;
        private EnvironmentState _state;
        private EnvironmentSnapshot _snapshot;
        private EnvironmentPlantCatalog _plantCatalog;
        private EnvironmentFullSnapshot _fullSnapshot;
        private EnvironmentFoundationBootstrapResult _bootstrap;
        private EnvironmentProtectedTestAdvanceReport _lastReport;
        private EnvironmentBiomeProfile _biomeProfile = EnvironmentBiomeProfile.Default;
        private double _fractionalTicks;

        public bool IsBootstrapped => _state != null && _snapshot != null;
        public EnvironmentFoundationConfig Config => _config;
        public EnvironmentState State => _state ?? new EnvironmentState();
        public EnvironmentSnapshot Snapshot => _snapshot ?? State.CreateSnapshot();
        public EnvironmentPlantCatalog PlantCatalog => _plantCatalog ?? new EnvironmentPlantCatalog(null);
        public EnvironmentFoundationBootstrapResult BootstrapResult => _bootstrap;
        public EnvironmentProtectedTestAdvanceReport LastReport => _lastReport;
        public EnvironmentBiomeProfile BiomeProfile => _biomeProfile.IsValid
            ? _biomeProfile
            : EnvironmentBiomeProfile.Default;
        public EnvironmentFullSnapshot FullSnapshot =>
            _fullSnapshot ?? EnvironmentReadOnlySnapshotResolver.BuildFullSnapshot(Snapshot);
        public long CurrentTicks => Snapshot.Calendar.ElapsedEnvironmentTicks;

        // =============================================================================
        // Bootstrap
        // =============================================================================
        /// <summary>
        /// <para>
        /// Inizializza il driver usando una configurazione esplicita o lo scenario
        /// protetto di default.
        /// </para>
        /// </summary>
        public EnvironmentFoundationBootstrapResult Bootstrap(
            EnvironmentFoundationConfig config = null)
        {
            // Copiamo il riferimento config per rendere evidente quale documento dati
            // ha alimentato il test. I futuri loader potranno passare qui asset/JSON.
            _config = config ?? CreateProtectedDefaultConfig();
            if (!_biomeProfile.IsValid)
                _biomeProfile = EnvironmentBiomeProfile.Default;
            _bootstrap = EnvironmentFoundationBootstrap.Bootstrap(_config);
            _state = _bootstrap.Build.State ?? new EnvironmentState();
            _snapshot = _state.CreateSnapshot();
            _plantCatalog = _bootstrap.PlantCatalog ?? new EnvironmentPlantCatalog(null);
            _fullSnapshot = EnvironmentReadOnlySnapshotResolver.BuildFullSnapshot(_snapshot);
            _fractionalTicks = 0d;
            _lastReport = CreateIdleReport(_snapshot.Calendar.ElapsedEnvironmentTicks);

            return _bootstrap;
        }

        // =============================================================================
        // ResetToProtectedDefaults
        // =============================================================================
        /// <summary>
        /// <para>
        /// Ricrea lo scenario protetto predefinito.
        /// </para>
        /// </summary>
        public EnvironmentFoundationBootstrapResult ResetToProtectedDefaults()
        {
            return Bootstrap(CreateProtectedDefaultConfig());
        }

        // =============================================================================
        // SetBiomePreset
        // =============================================================================
        /// <summary>
        /// <para>
        /// Applica un preset biome allo scenario protetto.
        /// </para>
        /// </summary>
        public void SetBiomePreset(EnvironmentProtectedTestBiomePreset preset)
        {
            _biomeProfile = ResolveBiomePreset(preset);
        }

        // =============================================================================
        // AdvanceRealSeconds
        // =============================================================================
        /// <summary>
        /// <para>
        /// Avanza il driver convertendo secondi reali in tick ambientali.
        /// </para>
        /// </summary>
        public EnvironmentProtectedTestAdvanceReport AdvanceRealSeconds(
            double realSeconds,
            EnvironmentProtectedTestSpeedPreset preset)
        {
            EnsureBootstrapped();
            if (realSeconds <= 0d)
                return CreateIdleReport(CurrentTicks);

            var profile = EnvironmentProtectedTestSpeedProfile.FromPreset(
                preset,
                _config.calendar);
            if (!profile.IsRunning)
                return CreateIdleReport(CurrentTicks);

            // Conserviamo la parte frazionaria per evitare drift quando Unity consegna
            // delta time piccoli o non costanti.
            double rawTicks = (realSeconds * profile.EnvironmentTicksPerRealSecond) + _fractionalTicks;
            long wholeTicks = rawTicks >= long.MaxValue
                ? long.MaxValue
                : (long)Math.Floor(rawTicks);
            _fractionalTicks = rawTicks - wholeTicks;

            return wholeTicks <= 0
                ? CreateIdleReport(CurrentTicks)
                : AdvanceTicks(wholeTicks);
        }

        // =============================================================================
        // AdvanceTicks
        // =============================================================================
        /// <summary>
        /// <para>
        /// Avanza il driver di un numero esplicito di tick ambientali.
        /// </para>
        /// </summary>
        public EnvironmentProtectedTestAdvanceReport AdvanceTicks(long environmentTicks)
        {
            EnsureBootstrapped();
            if (environmentTicks <= 0)
                return CreateIdleReport(CurrentTicks);

            long previousTicks = CurrentTicks;
            long targetTicks = AddTicksClamped(previousTicks, environmentTicks);
            long ticksPerDay = ResolveTicksPerDay(_config.calendar);
            int batches = 0;
            bool hourChanged = false;
            bool dayChanged = false;
            bool monthChanged = false;
            bool seasonChanged = false;
            bool yearChanged = false;
            bool ranNaturalGrowth = false;
            EnvironmentAdvanceResult lastAdvance = null;
            EnvironmentNaturalGrowthReport lastGrowthReport = default;

            while (CurrentTicks < targetTicks)
            {
                // I batch si fermano ai confini giornalieri per far girare il ciclo
                // naturale una volta per giorno anche quando il preset salta mesi.
                long nextDayBoundary = ResolveNextDayBoundary(CurrentTicks, ticksPerDay);
                long batchTarget = Math.Min(targetTicks, nextDayBoundary);
                lastAdvance = AdvanceAbsolute(batchTarget, out EnvironmentNaturalGrowthReport growthReport);
                lastGrowthReport = growthReport;
                batches++;

                if (lastAdvance != null)
                {
                    hourChanged |= lastAdvance.Transition.HourChanged;
                    dayChanged |= lastAdvance.Transition.DayChanged;
                    monthChanged |= lastAdvance.Transition.MonthChanged;
                    seasonChanged |= lastAdvance.Transition.SeasonChanged;
                    yearChanged |= lastAdvance.Transition.YearChanged;
                    ranNaturalGrowth |= lastAdvance.Transition.DayChanged;
                }
            }

            _lastReport = new EnvironmentProtectedTestAdvanceReport(
                previousTicks,
                CurrentTicks,
                batches,
                hourChanged,
                dayChanged,
                monthChanged,
                seasonChanged,
                yearChanged,
                ranNaturalGrowth,
                lastAdvance,
                lastGrowthReport,
                _fullSnapshot);
            return _lastReport;
        }

        // =============================================================================
        // AdvanceHours
        // =============================================================================
        /// <summary>
        /// <para>
        /// Avanza di un numero intero di ore simulate.
        /// </para>
        /// </summary>
        public EnvironmentProtectedTestAdvanceReport AdvanceHours(int hours)
        {
            long ticks = (long)Math.Max(0, hours) * ResolveTicksPerHour(_config?.calendar);
            return AdvanceTicks(ticks);
        }

        // =============================================================================
        // AdvanceDays
        // =============================================================================
        /// <summary>
        /// <para>
        /// Avanza di un numero intero di giorni simulati.
        /// </para>
        /// </summary>
        public EnvironmentProtectedTestAdvanceReport AdvanceDays(int days)
        {
            long ticks = (long)Math.Max(0, days) * ResolveTicksPerDay(_config?.calendar);
            return AdvanceTicks(ticks);
        }

        // =============================================================================
        // AdvanceMonths
        // =============================================================================
        /// <summary>
        /// <para>
        /// Avanza di un numero intero di mesi simulati.
        /// </para>
        /// </summary>
        public EnvironmentProtectedTestAdvanceReport AdvanceMonths(int months)
        {
            EnsureBootstrapped();
            long monthTicks = ResolveTicksPerDay(_config.calendar)
                              * _config.calendar.ResolveDaysPerMonth();
            return AdvanceTicks((long)Math.Max(0, months) * monthTicks);
        }

        // =============================================================================
        // AdvanceSeasons
        // =============================================================================
        /// <summary>
        /// <para>
        /// Avanza di un numero intero di stagioni simulate.
        /// </para>
        /// </summary>
        public EnvironmentProtectedTestAdvanceReport AdvanceSeasons(int seasons)
        {
            EnsureBootstrapped();
            long seasonTicks = ResolveTicksPerDay(_config.calendar)
                               * _config.calendar.ResolveDaysPerMonth()
                               * _config.calendar.ResolveMonthsPerSeason();
            return AdvanceTicks((long)Math.Max(0, seasons) * seasonTicks);
        }

        // =============================================================================
        // AdvanceYears
        // =============================================================================
        /// <summary>
        /// <para>
        /// Avanza di un numero intero di anni simulati.
        /// </para>
        /// </summary>
        public EnvironmentProtectedTestAdvanceReport AdvanceYears(int years)
        {
            EnsureBootstrapped();
            long yearTicks = ResolveTicksPerDay(_config.calendar)
                             * _config.calendar.ResolveDaysPerMonth()
                             * _config.calendar.ResolveMonthsPerYear();
            return AdvanceTicks((long)Math.Max(0, years) * yearTicks);
        }

        // =============================================================================
        // CreateProtectedDefaultConfig
        // =============================================================================
        /// <summary>
        /// <para>
        /// Crea uno scenario minimo ma leggibile per pannelli e smoke test.
        /// </para>
        /// </summary>
        public static EnvironmentFoundationConfig CreateProtectedDefaultConfig()
        {
            var config = EnvironmentFoundationBootstrap.CreateDefaultConfig();
            config.configKey = "protected_biosphere_test";
            config.areas = new EnvironmentAreaSetConfig
            {
                areas = new[]
                {
                    new EnvironmentAreaConfig
                    {
                        areaId = 1,
                        kind = "Vegetation",
                        minX = 0,
                        minY = 0,
                        maxX = 11,
                        maxY = 7,
                        z = 0,
                        priority = 10,
                        isEnabled = true,
                        key = "protected_meadow"
                    },
                    new EnvironmentAreaConfig
                    {
                        areaId = 2,
                        kind = "Water",
                        minX = 2,
                        minY = 2,
                        maxX = 10,
                        maxY = 3,
                        z = 0,
                        priority = 20,
                        isEnabled = true,
                        key = "protected_stream"
                    }
                },
                fertilityAreas = new[]
                {
                    new EnvironmentFertilityAreaConfig
                    {
                        areaId = 1,
                        soilKind = "Grassland",
                        baseFertility01 = 0.72f,
                        currentFertility01 = 0.70f,
                        growthModifier01 = 0.78f,
                        exhaustion01 = 0.08f,
                        recovery01 = 0.62f
                    }
                },
                waterAreas = new[]
                {
                    new EnvironmentWaterAreaConfig
                    {
                        areaId = 2,
                        waterKind = "River",
                        depthLevel = "Ford",
                        waterLevel01 = 0.64f,
                        flowIntensity01 = 0.42f,
                        isDrinkable = true,
                        isSeasonal = false
                    }
                },
                vegetationAreas = new[]
                {
                    new EnvironmentVegetationAreaConfig
                    {
                        areaId = 1,
                        vegetationKind = "Grass",
                        density01 = 0.58f,
                        growthPotential01 = 0.74f,
                        health01 = 0.82f,
                        fertilityInfluence01 = 0.70f,
                        climateInfluence01 = 0.55f
                    }
                },
                seedBankAreas = new[]
                {
                    new EnvironmentSeedBankAreaConfig
                    {
                        areaId = 1,
                        entries = new[]
                        {
                            new EnvironmentSeedBankEntryConfig
                            {
                                speciesKey = "wild_grass",
                                amount01 = 0.82f,
                                viability01 = 0.78f
                            },
                            new EnvironmentSeedBankEntryConfig
                            {
                                speciesKey = "oak_tree",
                                amount01 = 0.28f,
                                viability01 = 0.66f
                            }
                        }
                    }
                }
            };
            config.plantCatalog = new EnvironmentPlantCatalogConfig();
            return config;
        }

        // =============================================================================
        // ResolveTicksPerHour
        // =============================================================================
        /// <summary>
        /// <para>
        /// Restituisce i tick di una singola ora simulata.
        /// </para>
        /// </summary>
        public static long ResolveTicksPerHour(EnvironmentCalendarConfig calendarConfig)
        {
            var safeCalendar = calendarConfig ?? new EnvironmentCalendarConfig();
            return safeCalendar.ResolveCalendarTicksPerSimulatedHour();
        }

        // =============================================================================
        // ResolveTicksPerDay
        // =============================================================================
        /// <summary>
        /// <para>
        /// Restituisce i tick di un giorno simulato.
        /// </para>
        /// </summary>
        public static long ResolveTicksPerDay(EnvironmentCalendarConfig calendarConfig)
        {
            var safeCalendar = calendarConfig ?? new EnvironmentCalendarConfig();
            return (long)safeCalendar.ResolveHoursPerDay()
                   * safeCalendar.ResolveCalendarTicksPerSimulatedHour();
        }

        private EnvironmentAdvanceResult AdvanceAbsolute(
            long targetTicks,
            out EnvironmentNaturalGrowthReport growthReport)
        {
            // L'avanzamento Core produce prima calendario, clima e aree evolute. Solo
            // dopo, se e' cambiato giorno, applichiamo la crescita naturale giornaliera.
            var advance = EnvironmentAdvanceResolver.AdvanceStateSnapshot(
                _state,
                targetTicks,
                _config.calendar,
                _config.climate,
                BiomeProfile);
            _state = advance.State;
            _snapshot = advance.Snapshot;
            growthReport = default;

            if (advance.Transition.DayChanged)
            {
                var growth = EnvironmentNaturalGrowthResolver.Evolve(
                    _snapshot,
                    _plantCatalog,
                    advance.Transition,
                    advance.Climate,
                    advance.SeasonProfile,
                    null,
                    BiomeProfile);
                _state = growth.State;
                _snapshot = _state.CreateSnapshot();
                growthReport = growth.Report;
            }

            _fullSnapshot = EnvironmentReadOnlySnapshotResolver.BuildFullSnapshot(_snapshot);
            return advance;
        }

        private void EnsureBootstrapped()
        {
            if (!IsBootstrapped)
                Bootstrap();
        }

        private static EnvironmentBiomeProfile ResolveBiomePreset(
            EnvironmentProtectedTestBiomePreset preset)
        {
            switch (preset)
            {
                case EnvironmentProtectedTestBiomePreset.Desert:
                    return EnvironmentBiomeProfile.CreateDesert();

                case EnvironmentProtectedTestBiomePreset.Jungle:
                    return EnvironmentBiomeProfile.CreateJungle();

                case EnvironmentProtectedTestBiomePreset.Tundra:
                    return EnvironmentBiomeProfile.CreateTundra();

                default:
                    return EnvironmentBiomeProfile.CreateTemperateGrassland();
            }
        }

        private EnvironmentProtectedTestAdvanceReport CreateIdleReport(long ticks)
        {
            return new EnvironmentProtectedTestAdvanceReport(
                ticks,
                ticks,
                0,
                false,
                false,
                false,
                false,
                false,
                false,
                null,
                default,
                _fullSnapshot);
        }

        private static long ResolveNextDayBoundary(long currentTicks, long ticksPerDay)
        {
            long safeTicksPerDay = ticksPerDay <= 0 ? 1 : ticksPerDay;
            long currentDay = currentTicks / safeTicksPerDay;
            long boundary = (currentDay + 1L) * safeTicksPerDay;
            return boundary <= currentTicks ? currentTicks + safeTicksPerDay : boundary;
        }

        private static long AddTicksClamped(long currentTicks, long deltaTicks)
        {
            if (currentTicks < 0)
                currentTicks = 0;

            if (deltaTicks <= 0)
                return currentTicks;

            return long.MaxValue - currentTicks < deltaTicks
                ? long.MaxValue
                : currentTicks + deltaTicks;
        }
    }
}
