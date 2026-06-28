using Arcontio.Core.Config;

namespace Arcontio.Core.Environment
{
    // =============================================================================
    // EnvironmentFoundationHarnessResult
    // =============================================================================
    /// <summary>
    /// <para>
    /// Risultato compatto dell'harness di verifica della foundation ambientale.
    /// </para>
    ///
    /// <para><b>Principio architetturale: QA data-only senza runner Unity</b></para>
    /// <para>
    /// Il risultato raccoglie controlli deterministici eseguibili da test futuri,
    /// strumenti editor o diagnostica manuale senza dipendere da scene, prefab,
    /// <c>MonoBehaviour</c> o oggetti globali.
    /// </para>
    ///
    /// <para><b>Struttura interna:</b></para>
    /// <list type="bullet">
    ///   <item><b>CalendarBaselineOk</b>: verifica scala 24 ore simulate in 9000 tick SimulationHost.</item>
    ///   <item><b>SeasonBoundaryOk</b>: verifica cambio stagione con configurazione default.</item>
    ///   <item><b>ClimateResolutionOk</b>: verifica produzione clima globale normalizzato.</item>
    ///   <item><b>SnapshotOk</b>: verifica registry aree e snapshot read-only.</item>
    ///   <item><b>ConfigPipelineOk</b>: verifica costruzione stato da DTO configurabili.</item>
    ///   <item><b>SnapshotQueryOk</b>: verifica query read-only su celle e layer.</item>
    ///   <item><b>TemporalTransitionOk</b>: verifica confini ora/giorno/stagione.</item>
    ///   <item><b>ConfigValidationOk</b>: verifica diagnostica delle configurazioni.</item>
    ///   <item><b>AreaEvolutionOk</b>: verifica evoluzione giornaliera dei layer.</item>
    ///   <item><b>SnapshotEvolutionOk</b>: verifica evoluzione batch dello snapshot.</item>
    ///   <item><b>AdvancePipelineOk</b>: verifica avanzamento end-to-end data-only.</item>
    ///   <item><b>SnapshotDiffOk</b>: verifica diff read-only tra snapshot.</item>
    ///   <item><b>SeedBankOk</b>: verifica seed bank area-based.</item>
    ///   <item><b>BootstrapOk</b>: verifica root config e bootstrap data-only.</item>
    ///   <item><b>PlantCatalogOk</b>: verifica catalogo specie vegetali data-only.</item>
    ///   <item><b>PlantInstanceOk</b>: verifica istanze pianta e snapshot read-only.</item>
    ///   <item><b>NaturalGrowthOk</b>: verifica ciclo naturale area/seedBank/piante.</item>
    ///   <item><b>AgricultureFoundationOk</b>: verifica contratti agricoli data-only.</item>
    ///   <item><b>ReadOnlySnapshotsOk</b>: verifica full snapshot Core-side per consumer futuri.</item>
    ///   <item><b>PersistenceOk</b>: verifica capture/restore data-only dello stato persistente.</item>
    ///   <item><b>VisualProjectionOk</b>: verifica proiezioni visuali neutrali senza ArcGraph.</item>
    ///   <item><b>ConsumerQueryOk</b>: verifica facade read-only per NPC/Decision futuri.</item>
    ///   <item><b>RuntimeSchedulerOk</b>: verifica cadenza biosfera configurabile senza SimulationHost.</item>
    ///   <item><b>IsSuccessful</b>: esito aggregato dei controlli.</item>
    /// </list>
    /// </summary>
    public readonly struct EnvironmentFoundationHarnessResult
    {
        public readonly bool CalendarBaselineOk;
        public readonly bool SeasonBoundaryOk;
        public readonly bool ClimateResolutionOk;
        public readonly bool SnapshotOk;
        public readonly bool ConfigPipelineOk;
        public readonly bool SnapshotQueryOk;
        public readonly bool TemporalTransitionOk;
        public readonly bool ConfigValidationOk;
        public readonly bool AreaEvolutionOk;
        public readonly bool SnapshotEvolutionOk;
        public readonly bool AdvancePipelineOk;
        public readonly bool SnapshotDiffOk;
        public readonly bool SeedBankOk;
        public readonly bool BootstrapOk;
        public readonly bool PlantCatalogOk;
        public readonly bool PlantInstanceOk;
        public readonly bool NaturalGrowthOk;
        public readonly bool AgricultureFoundationOk;
        public readonly bool ReadOnlySnapshotsOk;
        public readonly bool PersistenceOk;
        public readonly bool VisualProjectionOk;
        public readonly bool ConsumerQueryOk;
        public readonly bool RuntimeSchedulerOk;

        public bool IsSuccessful =>
            CalendarBaselineOk
            && SeasonBoundaryOk
            && ClimateResolutionOk
            && SnapshotOk
            && ConfigPipelineOk
            && SnapshotQueryOk
            && TemporalTransitionOk
            && ConfigValidationOk
            && AreaEvolutionOk
            && SnapshotEvolutionOk
            && AdvancePipelineOk
            && SnapshotDiffOk
            && SeedBankOk
            && BootstrapOk
            && PlantCatalogOk
            && PlantInstanceOk
            && NaturalGrowthOk
            && AgricultureFoundationOk
            && ReadOnlySnapshotsOk
            && PersistenceOk
            && VisualProjectionOk
            && ConsumerQueryOk
            && RuntimeSchedulerOk;

        // =============================================================================
        // EnvironmentFoundationHarnessResult
        // =============================================================================
        /// <summary>
        /// <para>
        /// Costruisce il risultato aggregando i singoli controlli.
        /// </para>
        /// </summary>
        public EnvironmentFoundationHarnessResult(
            bool calendarBaselineOk,
            bool seasonBoundaryOk,
            bool climateResolutionOk,
            bool snapshotOk,
            bool configPipelineOk,
            bool snapshotQueryOk,
            bool temporalTransitionOk,
            bool configValidationOk,
            bool areaEvolutionOk,
            bool snapshotEvolutionOk,
            bool advancePipelineOk,
            bool snapshotDiffOk,
            bool seedBankOk,
            bool bootstrapOk,
            bool plantCatalogOk,
            bool plantInstanceOk,
            bool naturalGrowthOk,
            bool agricultureFoundationOk,
            bool readOnlySnapshotsOk,
            bool persistenceOk,
            bool visualProjectionOk,
            bool consumerQueryOk,
            bool runtimeSchedulerOk)
        {
            CalendarBaselineOk = calendarBaselineOk;
            SeasonBoundaryOk = seasonBoundaryOk;
            ClimateResolutionOk = climateResolutionOk;
            SnapshotOk = snapshotOk;
            ConfigPipelineOk = configPipelineOk;
            SnapshotQueryOk = snapshotQueryOk;
            TemporalTransitionOk = temporalTransitionOk;
            ConfigValidationOk = configValidationOk;
            AreaEvolutionOk = areaEvolutionOk;
            SnapshotEvolutionOk = snapshotEvolutionOk;
            AdvancePipelineOk = advancePipelineOk;
            SnapshotDiffOk = snapshotDiffOk;
            SeedBankOk = seedBankOk;
            BootstrapOk = bootstrapOk;
            PlantCatalogOk = plantCatalogOk;
            PlantInstanceOk = plantInstanceOk;
            NaturalGrowthOk = naturalGrowthOk;
            AgricultureFoundationOk = agricultureFoundationOk;
            ReadOnlySnapshotsOk = readOnlySnapshotsOk;
            PersistenceOk = persistenceOk;
            VisualProjectionOk = visualProjectionOk;
            ConsumerQueryOk = consumerQueryOk;
            RuntimeSchedulerOk = runtimeSchedulerOk;
        }
    }

    // =============================================================================
    // EnvironmentFoundationHarness
    // =============================================================================
    /// <summary>
    /// <para>
    /// Harness statico di verifica minima per la Environment Foundation.
    /// </para>
    ///
    /// <para><b>Principio architetturale: fondazione verificabile prima dell'integrazione</b></para>
    /// <para>
    /// La foundation ambientale deve poter essere validata prima di collegarla a
    /// sistemi runtime, salvataggi o visualizzazione. Questo harness usa soltanto
    /// DTO, resolver e contenitori passivi del namespace Environment.
    /// </para>
    ///
    /// <para><b>Struttura interna:</b></para>
    /// <list type="bullet">
    ///   <item><b>RunDefaultChecks</b>: esegue controlli su config default.</item>
    ///   <item><b>CheckCalendarBaseline</b>: controlla tick, ora e giorno.</item>
    ///   <item><b>CheckSeasonBoundary</b>: controlla soglia primavera-estate.</item>
    ///   <item><b>CheckClimateResolution</b>: controlla clima globale deterministico.</item>
    ///   <item><b>CheckSnapshot</b>: controlla inserimento area e snapshot.</item>
    ///   <item><b>CheckConfigPipeline</b>: controlla applicazione DTO config.</item>
    ///   <item><b>CheckSnapshotQuery</b>: controlla query spaziale read-only.</item>
    ///   <item><b>CheckTemporalTransition</b>: controlla cadenze temporali.</item>
    ///   <item><b>CheckConfigValidation</b>: controlla diagnostica config.</item>
    ///   <item><b>CheckAreaEvolution</b>: controlla dinamica giornaliera area.</item>
    ///   <item><b>CheckSnapshotEvolution</b>: controlla evoluzione batch snapshot.</item>
    ///   <item><b>CheckAdvancePipeline</b>: controlla avanzamento end-to-end.</item>
    ///   <item><b>CheckSnapshotDiff</b>: controlla diff added/removed/modified.</item>
    ///   <item><b>CheckSeedBank</b>: controlla seed bank area-based.</item>
    ///   <item><b>CheckBootstrap</b>: controlla root config e bootstrap.</item>
    ///   <item><b>CheckPlantCatalog</b>: controlla catalogo piante data-only.</item>
    ///   <item><b>CheckPlantInstance</b>: controlla istanze pianta e query snapshot.</item>
    ///   <item><b>CheckNaturalGrowth</b>: controlla loop naturale giornaliero data-only.</item>
    ///   <item><b>CheckAgricultureFoundation</b>: controlla confini agricoli futuri.</item>
    ///   <item><b>CheckReadOnlySnapshots</b>: controlla projection snapshot per dominio.</item>
    ///   <item><b>CheckPersistence</b>: controlla capture/restore dello stato persistente.</item>
    ///   <item><b>CheckVisualProjection</b>: controlla readiness adapter senza View types.</item>
    ///   <item><b>CheckConsumerQuery</b>: controlla facts read-only per consumer futuri.</item>
    ///   <item><b>CheckRuntimeScheduler</b>: controlla scheduler biosfera data-only.</item>
    /// </list>
    /// </summary>
    public static class EnvironmentFoundationHarness
    {
        // =============================================================================
        // RunDefaultChecks
        // =============================================================================
        /// <summary>
        /// <para>
        /// Esegue un set minimo di controlli deterministici sulla foundation.
        /// </para>
        /// </summary>
        public static EnvironmentFoundationHarnessResult RunDefaultChecks()
        {
            var calendarConfig = new EnvironmentCalendarConfig();
            var climateConfig = new EnvironmentClimateConfig();

            // Ogni controllo resta separato per rendere leggibile quale contratto si
            // e' rotto quando questo harness verra' richiamato da test veri.
            bool calendarBaselineOk = CheckCalendarBaseline(calendarConfig);
            bool seasonBoundaryOk = CheckSeasonBoundary(calendarConfig);
            bool climateResolutionOk = CheckClimateResolution(calendarConfig, climateConfig);
            bool snapshotOk = CheckSnapshot(calendarConfig, climateConfig);
            bool configPipelineOk = CheckConfigPipeline(calendarConfig, climateConfig);
            bool snapshotQueryOk = CheckSnapshotQuery(calendarConfig, climateConfig);
            bool temporalTransitionOk = CheckTemporalTransition(calendarConfig);
            bool configValidationOk = CheckConfigValidation(calendarConfig, climateConfig);
            bool areaEvolutionOk = CheckAreaEvolution(calendarConfig);
            bool snapshotEvolutionOk = CheckSnapshotEvolution(calendarConfig);
            bool advancePipelineOk = CheckAdvancePipeline(calendarConfig, climateConfig);
            bool snapshotDiffOk = CheckSnapshotDiff(calendarConfig, climateConfig);
            bool seedBankOk = CheckSeedBank(calendarConfig, climateConfig);
            bool bootstrapOk = CheckBootstrap();
            bool plantCatalogOk = CheckPlantCatalog();
            bool plantInstanceOk = CheckPlantInstance();
            bool naturalGrowthOk = CheckNaturalGrowth();
            bool agricultureFoundationOk = CheckAgricultureFoundation();
            bool readOnlySnapshotsOk = CheckReadOnlySnapshots();
            bool persistenceOk = CheckPersistence();
            bool visualProjectionOk = CheckVisualProjection();
            bool consumerQueryOk = CheckConsumerQuery();
            bool runtimeSchedulerOk = CheckRuntimeScheduler();

            return new EnvironmentFoundationHarnessResult(
                calendarBaselineOk,
                seasonBoundaryOk,
                climateResolutionOk,
                snapshotOk,
                configPipelineOk,
                snapshotQueryOk,
                temporalTransitionOk,
                configValidationOk,
                areaEvolutionOk,
                snapshotEvolutionOk,
                advancePipelineOk,
                snapshotDiffOk,
                seedBankOk,
                bootstrapOk,
                plantCatalogOk,
                plantInstanceOk,
                naturalGrowthOk,
                agricultureFoundationOk,
                readOnlySnapshotsOk,
                persistenceOk,
                visualProjectionOk,
                consumerQueryOk,
                runtimeSchedulerOk);
        }

        private static bool CheckCalendarBaseline(EnvironmentCalendarConfig calendarConfig)
        {
            var start = EnvironmentCalendarResolver.Resolve(0, calendarConfig);
            var oneHour = EnvironmentCalendarResolver.Resolve(
                EnvironmentCalendarConfig.DefaultCalendarTicksPerSimulatedHour,
                calendarConfig);
            var oneDay = EnvironmentCalendarResolver.Resolve(
                EnvironmentCalendarConfig.DefaultHoursPerDay
                * EnvironmentCalendarConfig.DefaultCalendarTicksPerSimulatedHour,
                calendarConfig);

            // Con la config default: tick 0 e' primavera giorno 0 ora 0, 375 tick
            // sono un'ora simulata, 9000 tick sono un giorno simulato completo.
            return start.Date.Season == EnvironmentSeasonKind.Spring
                   && start.Date.DayOfYear == 0
                   && start.TimeOfDay.Hour == 0
                   && oneHour.TimeOfDay.Hour == 1
                   && oneDay.Date.DayOfYear == 1
                   && oneDay.Date.DayOfMonth == 1;
        }

        private static bool CheckSeasonBoundary(EnvironmentCalendarConfig calendarConfig)
        {
            int ticksPerDay =
                EnvironmentCalendarConfig.DefaultHoursPerDay
                * EnvironmentCalendarConfig.DefaultCalendarTicksPerSimulatedHour;
            int daysUntilSummer =
                EnvironmentCalendarConfig.DefaultDaysPerMonth
                * EnvironmentCalendarConfig.DefaultMonthsPerSeason;

            var summerStart = EnvironmentCalendarResolver.Resolve(
                ticksPerDay * daysUntilSummer,
                calendarConfig);

            // Tre mesi da venticinque giorni portano alla prima giornata estiva; il
            // profilo default estivo espone sedici ore di luce.
            return summerStart.Date.Season == EnvironmentSeasonKind.Summer
                   && summerStart.DaylightHours == 16f;
        }

        private static bool CheckClimateResolution(
            EnvironmentCalendarConfig calendarConfig,
            EnvironmentClimateConfig climateConfig)
        {
            var calendar = EnvironmentCalendarResolver.Resolve(0, calendarConfig);
            var climate = EnvironmentClimateResolver.Resolve(calendar, climateConfig);

            // Il resolver climatico deve restare normalizzato e deve conservare la
            // stagione da cui deriva il profilo meteo corrente.
            return climate.Season == EnvironmentSeasonKind.Spring
                   && climate.Temperature01 >= 0f
                   && climate.Temperature01 <= 1f
                   && climate.Humidity01 >= 0f
                   && climate.Humidity01 <= 1f
                   && climate.Aridity01 >= 0f
                   && climate.Aridity01 <= 1f
                   && climate.Weather.Intensity01 >= 0f
                   && climate.Weather.Intensity01 <= 1f;
        }

        private static bool CheckSnapshot(
            EnvironmentCalendarConfig calendarConfig,
            EnvironmentClimateConfig climateConfig)
        {
            var areaId = new EnvironmentAreaId(1);
            var state = new EnvironmentState();
            var calendar = EnvironmentCalendarResolver.Resolve(0, calendarConfig);
            var climate = EnvironmentClimateResolver.Resolve(calendar, climateConfig);

            state.SetCalendar(calendar);
            state.SetClimate(climate);

            bool definitionOk = state.SetAreaDefinition(new EnvironmentAreaDefinition(
                areaId,
                EnvironmentAreaKind.Generic,
                new EnvironmentAreaBounds(0, 0, 3, 3),
                0,
                true,
                "foundation_probe"));
            bool fertilityOk = state.SetFertilityArea(new EnvironmentFertilityAreaState(
                areaId,
                EnvironmentSoilKind.Grassland,
                0.8f,
                0.7f,
                0.9f,
                0.1f,
                0.6f));
            bool waterOk = state.SetWaterArea(new EnvironmentWaterAreaState(
                areaId,
                EnvironmentWaterKind.Puddle,
                EnvironmentWaterDepthLevel.Shallow,
                0.3f,
                0.0f,
                true,
                true));
            bool vegetationOk = state.SetVegetationArea(new EnvironmentVegetationAreaState(
                areaId,
                EnvironmentVegetationKind.Grass,
                0.6f,
                0.8f,
                0.9f,
                0.7f,
                0.5f));

            var snapshot = state.CreateSnapshot();
            var area = snapshot.Areas.Count == 1
                ? snapshot.Areas[0]
                : default;

            // Lo snapshot deve esporre una sola area con tutti i payload dichiarati,
            // senza richiedere accesso ai dizionari interni dello stato.
            return definitionOk
                   && fertilityOk
                   && waterOk
                   && vegetationOk
                   && snapshot.Areas.Count == 1
                   && area.Definition.AreaId.Equals(areaId)
                   && area.HasFertility
                   && area.HasWater
                   && area.HasVegetation;
        }

        private static bool CheckConfigPipeline(
            EnvironmentCalendarConfig calendarConfig,
            EnvironmentClimateConfig climateConfig)
        {
            var areaSet = new EnvironmentAreaSetConfig
            {
                areas = new[]
                {
                    new EnvironmentAreaConfig
                    {
                        areaId = 2,
                        kind = "Vegetation",
                        minX = 10,
                        minY = 20,
                        maxX = 12,
                        maxY = 22,
                        priority = 5,
                        isEnabled = true,
                        key = "config_pipeline_probe"
                    }
                },
                fertilityAreas = new[]
                {
                    new EnvironmentFertilityAreaConfig
                    {
                        areaId = 2,
                        soilKind = "Forest",
                        baseFertility01 = 0.9f,
                        currentFertility01 = 0.8f,
                        growthModifier01 = 0.85f,
                        exhaustion01 = 0.05f,
                        recovery01 = 0.7f
                    }
                },
                waterAreas = new[]
                {
                    new EnvironmentWaterAreaConfig
                    {
                        areaId = 2,
                        waterKind = "River",
                        depthLevel = "Ford",
                        waterLevel01 = 0.4f,
                        flowIntensity01 = 0.6f,
                        isDrinkable = true,
                        isSeasonal = false
                    }
                },
                vegetationAreas = new[]
                {
                    new EnvironmentVegetationAreaConfig
                    {
                        areaId = 2,
                        vegetationKind = "Underbrush",
                        density01 = 0.7f,
                        growthPotential01 = 0.8f,
                        health01 = 0.9f,
                        fertilityInfluence01 = 0.8f,
                        climateInfluence01 = 0.6f
                    }
                }
            };

            var result = EnvironmentFoundationBuilder.BuildState(
                0,
                calendarConfig,
                climateConfig,
                areaSet);
            var snapshot = result.State.CreateSnapshot();
            var area = snapshot.Areas.Count == 1
                ? snapshot.Areas[0]
                : default;

            // La pipeline configurabile deve applicare una definizione e i tre payload
            // senza scarti, preservando parsing enum e bounds discreti.
            return !result.Report.HasRejectedEntries
                   && result.Report.AreaDefinitionsApplied == 1
                   && result.Report.FertilityAreasApplied == 1
                   && result.Report.WaterAreasApplied == 1
                   && result.Report.VegetationAreasApplied == 1
                   && snapshot.Areas.Count == 1
                   && area.Definition.Kind == EnvironmentAreaKind.Vegetation
                   && area.Definition.Bounds.Contains(new EnvironmentCellCoord(11, 21))
                   && area.FertilityState.SoilKind == EnvironmentSoilKind.Forest
                   && area.WaterState.WaterKind == EnvironmentWaterKind.River
                   && area.WaterState.DepthLevel == EnvironmentWaterDepthLevel.Ford
                   && area.VegetationState.VegetationKind == EnvironmentVegetationKind.Underbrush;
        }

        private static bool CheckSnapshotQuery(
            EnvironmentCalendarConfig calendarConfig,
            EnvironmentClimateConfig climateConfig)
        {
            var areaSet = new EnvironmentAreaSetConfig
            {
                areas = new[]
                {
                    new EnvironmentAreaConfig
                    {
                        areaId = 3,
                        kind = "Vegetation",
                        minX = 0,
                        minY = 0,
                        maxX = 5,
                        maxY = 5,
                        priority = 1,
                        isEnabled = true,
                        key = "low_priority_grass"
                    },
                    new EnvironmentAreaConfig
                    {
                        areaId = 4,
                        kind = "Vegetation",
                        minX = 2,
                        minY = 2,
                        maxX = 4,
                        maxY = 4,
                        priority = 9,
                        isEnabled = true,
                        key = "high_priority_underbrush"
                    },
                    new EnvironmentAreaConfig
                    {
                        areaId = 5,
                        kind = "Water",
                        minX = 2,
                        minY = 2,
                        maxX = 2,
                        maxY = 2,
                        priority = 3,
                        isEnabled = true,
                        key = "query_water"
                    }
                },
                waterAreas = new[]
                {
                    new EnvironmentWaterAreaConfig
                    {
                        areaId = 5,
                        waterKind = "Puddle",
                        depthLevel = "Shallow",
                        waterLevel01 = 0.2f,
                        flowIntensity01 = 0f,
                        isDrinkable = true,
                        isSeasonal = true
                    }
                },
                vegetationAreas = new[]
                {
                    new EnvironmentVegetationAreaConfig
                    {
                        areaId = 3,
                        vegetationKind = "Grass",
                        density01 = 0.5f,
                        growthPotential01 = 0.5f,
                        health01 = 0.8f,
                        fertilityInfluence01 = 0.4f,
                        climateInfluence01 = 0.4f
                    },
                    new EnvironmentVegetationAreaConfig
                    {
                        areaId = 4,
                        vegetationKind = "Underbrush",
                        density01 = 0.75f,
                        growthPotential01 = 0.8f,
                        health01 = 0.85f,
                        fertilityInfluence01 = 0.7f,
                        climateInfluence01 = 0.6f
                    }
                }
            };

            var snapshot = EnvironmentFoundationBuilder.BuildSnapshot(
                0,
                calendarConfig,
                climateConfig,
                areaSet);
            var cell = new EnvironmentCellCoord(2, 2);
            var allAreas = EnvironmentSnapshotQuery.QueryCell(snapshot, cell);
            var vegetationAreas = EnvironmentSnapshotQuery.QueryCellByKind(
                snapshot,
                cell,
                EnvironmentAreaKind.Vegetation);
            bool hasWater = EnvironmentSnapshotQuery.ContainsLayer(
                snapshot,
                cell,
                EnvironmentAreaKind.Water);
            bool hasBestVegetation = EnvironmentSnapshotQuery.TryGetTopPriorityArea(
                snapshot,
                cell,
                EnvironmentAreaKind.Vegetation,
                out var bestVegetation);

            // La cella (2,2) tocca due aree vegetali e una d'acqua. La query deve
            // aggregarle senza mutare lo snapshot e deve scegliere la priorita' alta.
            return allAreas.AreaCount == 3
                   && allAreas.HasWater
                   && allAreas.HasVegetation
                   && vegetationAreas.AreaCount == 2
                   && hasWater
                   && hasBestVegetation
                   && bestVegetation.Definition.AreaId.Equals(new EnvironmentAreaId(4))
                   && bestVegetation.VegetationState.VegetationKind == EnvironmentVegetationKind.Underbrush;
        }

        private static bool CheckTemporalTransition(EnvironmentCalendarConfig calendarConfig)
        {
            int ticksPerHour = EnvironmentCalendarConfig.DefaultCalendarTicksPerSimulatedHour;
            int ticksPerDay =
                EnvironmentCalendarConfig.DefaultHoursPerDay
                * EnvironmentCalendarConfig.DefaultCalendarTicksPerSimulatedHour;
            int ticksPerSeason =
                ticksPerDay
                * EnvironmentCalendarConfig.DefaultDaysPerMonth
                * EnvironmentCalendarConfig.DefaultMonthsPerSeason;

            var hourTransition = EnvironmentTemporalTransitionResolver.Resolve(
                ticksPerHour - 1,
                ticksPerHour,
                calendarConfig);
            var dayTransition = EnvironmentTemporalTransitionResolver.Resolve(
                ticksPerDay - 1,
                ticksPerDay,
                calendarConfig);
            var seasonTransition = EnvironmentTemporalTransitionResolver.Resolve(
                ticksPerSeason - 1,
                ticksPerSeason,
                calendarConfig);
            var stableTransition = EnvironmentTemporalTransitionResolver.Resolve(
                10,
                10,
                calendarConfig);

            // I confini verificano la baseline progettuale: 375 tick sono un'ora,
            // 9000 tick sono un giorno, 675000 tick sono tre mesi da venticinque giorni.
            return hourTransition.HourChanged
                   && !hourTransition.DayChanged
                   && hourTransition.ElapsedTicks == 1
                   && dayTransition.HourChanged
                   && dayTransition.DayChanged
                   && dayTransition.Current.Date.DayOfYear == 1
                   && seasonTransition.DayChanged
                   && seasonTransition.MonthChanged
                   && seasonTransition.SeasonChanged
                   && seasonTransition.Current.Date.Season == EnvironmentSeasonKind.Summer
                   && !stableTransition.AnyBoundaryChanged
                   && stableTransition.ElapsedTicks == 0;
        }

        private static bool CheckConfigValidation(
            EnvironmentCalendarConfig calendarConfig,
            EnvironmentClimateConfig climateConfig)
        {
            var cleanAreaSet = new EnvironmentAreaSetConfig
            {
                areas = new[]
                {
                    new EnvironmentAreaConfig
                    {
                        areaId = 6,
                        kind = "Fertility",
                        minX = 0,
                        minY = 0,
                        maxX = 1,
                        maxY = 1,
                        isEnabled = true,
                        key = "validation_clean"
                    }
                },
                fertilityAreas = new[]
                {
                    new EnvironmentFertilityAreaConfig
                    {
                        areaId = 6,
                        soilKind = "Grassland",
                        baseFertility01 = 0.7f,
                        currentFertility01 = 0.7f,
                        growthModifier01 = 0.8f,
                        recovery01 = 0.6f
                    }
                }
            };
            var cleanResult = EnvironmentConfigValidator.Validate(
                calendarConfig,
                climateConfig,
                cleanAreaSet);

            var brokenCalendar = new EnvironmentCalendarConfig
            {
                hoursPerDay = 0,
                calendarTicksPerSimulatedHour = -1,
                daysPerMonth = 0,
                monthsPerYear = 0,
                monthsPerSeason = 0,
                seasonProfiles = null
            };
            var brokenClimate = new EnvironmentClimateConfig
            {
                seasonClimateProfiles = null,
                weatherPersistence01 = 2f,
                hourlyTemperatureVariation01 = -1f
            };
            var brokenAreaSet = new EnvironmentAreaSetConfig
            {
                areas = new[]
                {
                    new EnvironmentAreaConfig
                    {
                        areaId = 7,
                        kind = "Water",
                        minX = 0,
                        minY = 0,
                        maxX = 1,
                        maxY = 1,
                        isEnabled = true,
                        key = "duplicate_a"
                    },
                    new EnvironmentAreaConfig
                    {
                        areaId = 7,
                        kind = "Water",
                        minX = 2,
                        minY = 2,
                        maxX = 3,
                        maxY = 3,
                        isEnabled = true,
                        key = "duplicate_b"
                    }
                },
                waterAreas = new[]
                {
                    new EnvironmentWaterAreaConfig
                    {
                        areaId = 99,
                        waterKind = "Lake",
                        depthLevel = "Deep",
                        waterLevel01 = 0.8f
                    }
                },
                vegetationAreas = new EnvironmentVegetationAreaConfig[]
                {
                    null
                }
            };
            var brokenResult = EnvironmentConfigValidator.Validate(
                brokenCalendar,
                brokenClimate,
                brokenAreaSet);

            // La config pulita non deve produrre errori; la config sporca deve
            // produrre errori strutturali e warning sui fallback temporali/climatici.
            return cleanResult.IsValid
                   && cleanResult.ErrorCount == 0
                   && brokenResult.ErrorCount >= 3
                   && brokenResult.WarningCount >= 7
                   && !brokenResult.IsValid;
        }

        private static bool CheckAreaEvolution(EnvironmentCalendarConfig calendarConfig)
        {
            var fertility = new EnvironmentFertilityAreaState(
                new EnvironmentAreaId(8),
                EnvironmentSoilKind.Grassland,
                0.8f,
                0.6f,
                0.8f,
                0.05f,
                0.7f);
            var water = new EnvironmentWaterAreaState(
                new EnvironmentAreaId(8),
                EnvironmentWaterKind.Puddle,
                EnvironmentWaterDepthLevel.Shallow,
                0.35f,
                0.0f,
                true,
                true);
            var vegetation = new EnvironmentVegetationAreaState(
                new EnvironmentAreaId(8),
                EnvironmentVegetationKind.Grass,
                0.45f,
                0.8f,
                0.70f,
                0.8f,
                0.7f);
            var climate = new EnvironmentGlobalClimateState(
                0.45f,
                0.75f,
                0.15f,
                new EnvironmentWeatherState(
                    EnvironmentWeatherKind.Rain,
                    0.7f,
                    0.8f,
                    0.2f,
                    false),
                EnvironmentSeasonKind.Spring);
            var seasonProfile = EnvironmentCalendarResolver.ResolveSeasonProfile(
                calendarConfig,
                EnvironmentSeasonKind.Spring);
            var stableCalendar = EnvironmentCalendarResolver.Resolve(10, calendarConfig);
            var stableTransition = EnvironmentTemporalTransitionResolver.Resolve(
                10,
                10,
                calendarConfig);
            var dailyTransition = EnvironmentTemporalTransitionResolver.Resolve(
                EnvironmentCalendarConfig.DefaultHoursPerDay
                * EnvironmentCalendarConfig.DefaultCalendarTicksPerSimulatedHour - 1,
                EnvironmentCalendarConfig.DefaultHoursPerDay
                * EnvironmentCalendarConfig.DefaultCalendarTicksPerSimulatedHour,
                calendarConfig);
            var stableContext = new EnvironmentAreaEvolutionContext(
                stableCalendar,
                climate,
                seasonProfile,
                stableTransition);
            var dailyContext = new EnvironmentAreaEvolutionContext(
                dailyTransition.Current,
                climate,
                seasonProfile,
                dailyTransition);

            var stableResult = EnvironmentAreaEvolutionResolver.Evolve(
                fertility,
                water,
                vegetation,
                stableContext);
            var dailyResult = EnvironmentAreaEvolutionResolver.Evolve(
                fertility,
                water,
                vegetation,
                dailyContext);

            // Senza cambio giorno non deve esserci delta. Con clima umido e pioggia,
            // la foundation deve produrre una piccola evoluzione positiva osservabile.
            return !stableResult.Delta.HasAnyDelta
                   && dailyResult.Delta.HasAnyDelta
                   && dailyResult.Fertility.CurrentFertility01 > fertility.CurrentFertility01
                   && dailyResult.Water.WaterLevel01 > water.WaterLevel01
                   && dailyResult.Vegetation.Density01 > vegetation.Density01
                   && dailyResult.Vegetation.Health01 > vegetation.Health01;
        }

        private static bool CheckSnapshotEvolution(EnvironmentCalendarConfig calendarConfig)
        {
            var areaSet = new EnvironmentAreaSetConfig
            {
                areas = new[]
                {
                    new EnvironmentAreaConfig
                    {
                        areaId = 9,
                        kind = "Vegetation",
                        minX = 0,
                        minY = 0,
                        maxX = 2,
                        maxY = 2,
                        priority = 1,
                        isEnabled = true,
                        key = "snapshot_evolution_full"
                    },
                    new EnvironmentAreaConfig
                    {
                        areaId = 10,
                        kind = "Water",
                        minX = 3,
                        minY = 0,
                        maxX = 4,
                        maxY = 1,
                        priority = 1,
                        isEnabled = true,
                        key = "snapshot_evolution_water_only"
                    }
                },
                fertilityAreas = new[]
                {
                    new EnvironmentFertilityAreaConfig
                    {
                        areaId = 9,
                        soilKind = "Grassland",
                        baseFertility01 = 0.8f,
                        currentFertility01 = 0.6f,
                        growthModifier01 = 0.8f,
                        exhaustion01 = 0.05f,
                        recovery01 = 0.7f
                    }
                },
                waterAreas = new[]
                {
                    new EnvironmentWaterAreaConfig
                    {
                        areaId = 9,
                        waterKind = "Puddle",
                        depthLevel = "Shallow",
                        waterLevel01 = 0.35f,
                        flowIntensity01 = 0f,
                        isDrinkable = true,
                        isSeasonal = true
                    },
                    new EnvironmentWaterAreaConfig
                    {
                        areaId = 10,
                        waterKind = "Lake",
                        depthLevel = "Ford",
                        waterLevel01 = 0.50f,
                        flowIntensity01 = 0f,
                        isDrinkable = true,
                        isSeasonal = false
                    }
                },
                vegetationAreas = new[]
                {
                    new EnvironmentVegetationAreaConfig
                    {
                        areaId = 9,
                        vegetationKind = "Grass",
                        density01 = 0.45f,
                        growthPotential01 = 0.8f,
                        health01 = 0.70f,
                        fertilityInfluence01 = 0.8f,
                        climateInfluence01 = 0.7f
                    }
                }
            };
            var climate = new EnvironmentGlobalClimateState(
                0.45f,
                0.75f,
                0.15f,
                new EnvironmentWeatherState(
                    EnvironmentWeatherKind.Rain,
                    0.7f,
                    0.8f,
                    0.2f,
                    false),
                EnvironmentSeasonKind.Spring);
            var seasonProfile = EnvironmentCalendarResolver.ResolveSeasonProfile(
                calendarConfig,
                EnvironmentSeasonKind.Spring);
            var sourceSnapshot = EnvironmentFoundationBuilder.BuildSnapshot(
                0,
                calendarConfig,
                new EnvironmentClimateConfig(),
                areaSet);
            var transition = EnvironmentTemporalTransitionResolver.Resolve(
                EnvironmentCalendarConfig.DefaultHoursPerDay
                * EnvironmentCalendarConfig.DefaultCalendarTicksPerSimulatedHour - 1,
                EnvironmentCalendarConfig.DefaultHoursPerDay
                * EnvironmentCalendarConfig.DefaultCalendarTicksPerSimulatedHour,
                calendarConfig);
            var result = EnvironmentSnapshotEvolutionResolver.EvolveSnapshot(
                sourceSnapshot,
                transition,
                climate,
                seasonProfile);
            var evolvedSnapshot = result.State.CreateSnapshot();
            var fullAreaQuery = EnvironmentSnapshotQuery.QueryCell(
                evolvedSnapshot,
                new EnvironmentCellCoord(1, 1));
            var waterOnlyQuery = EnvironmentSnapshotQuery.QueryCell(
                evolvedSnapshot,
                new EnvironmentCellCoord(3, 0));
            var fullArea = fullAreaQuery.AreaCount == 1
                ? fullAreaQuery.Areas[0]
                : default;
            var waterOnlyArea = waterOnlyQuery.AreaCount == 1
                ? waterOnlyQuery.Areas[0]
                : default;

            // L'evoluzione batch deve visitare entrambe le aree, evolvere i payload
            // presenti e non aggiungere vegetazione/fertilita' all'area solo acqua.
            return result.Report.AreasVisited == 2
                   && result.Report.FertilityAreasEvolved == 1
                   && result.Report.WaterAreasEvolved == 2
                   && result.Report.VegetationAreasEvolved == 1
                   && result.Report.HasChanges
                   && evolvedSnapshot.Areas.Count == 2
                   && fullArea.HasFertility
                   && fullArea.HasWater
                   && fullArea.HasVegetation
                   && fullArea.FertilityState.CurrentFertility01 > 0.6f
                   && fullArea.WaterState.WaterLevel01 > 0.35f
                   && fullArea.VegetationState.Density01 > 0.45f
                   && waterOnlyArea.HasWater
                   && !waterOnlyArea.HasFertility
                   && !waterOnlyArea.HasVegetation;
        }

        private static bool CheckAdvancePipeline(
            EnvironmentCalendarConfig calendarConfig,
            EnvironmentClimateConfig climateConfig)
        {
            var areaSet = new EnvironmentAreaSetConfig
            {
                areas = new[]
                {
                    new EnvironmentAreaConfig
                    {
                        areaId = 11,
                        kind = "Vegetation",
                        minX = 0,
                        minY = 0,
                        maxX = 2,
                        maxY = 2,
                        priority = 1,
                        isEnabled = true,
                        key = "advance_pipeline_probe"
                    }
                },
                fertilityAreas = new[]
                {
                    new EnvironmentFertilityAreaConfig
                    {
                        areaId = 11,
                        soilKind = "Grassland",
                        baseFertility01 = 0.8f,
                        currentFertility01 = 0.6f,
                        growthModifier01 = 0.8f,
                        exhaustion01 = 0.05f,
                        recovery01 = 0.7f
                    }
                },
                waterAreas = new[]
                {
                    new EnvironmentWaterAreaConfig
                    {
                        areaId = 11,
                        waterKind = "Puddle",
                        depthLevel = "Shallow",
                        waterLevel01 = 0.35f,
                        flowIntensity01 = 0f,
                        isDrinkable = true,
                        isSeasonal = true
                    }
                },
                vegetationAreas = new[]
                {
                    new EnvironmentVegetationAreaConfig
                    {
                        areaId = 11,
                        vegetationKind = "Grass",
                        density01 = 0.45f,
                        growthPotential01 = 0.8f,
                        health01 = 0.70f,
                        fertilityInfluence01 = 0.8f,
                        climateInfluence01 = 0.7f
                    }
                }
            };
            var initialSnapshot = EnvironmentFoundationBuilder.BuildSnapshot(
                0,
                calendarConfig,
                climateConfig,
                areaSet);
            long oneDayTicks =
                EnvironmentCalendarConfig.DefaultHoursPerDay
                * EnvironmentCalendarConfig.DefaultCalendarTicksPerSimulatedHour;
            var result = EnvironmentAdvanceResolver.AdvanceSnapshot(
                initialSnapshot,
                0,
                oneDayTicks,
                calendarConfig,
                climateConfig);
            var query = EnvironmentSnapshotQuery.QueryCell(
                result.Snapshot,
                new EnvironmentCellCoord(1, 1));
            var area = query.AreaCount == 1
                ? query.Areas[0]
                : default;

            // L'avanzamento end-to-end deve risolvere calendario, clima, profilo
            // stagionale ed evoluzione batch senza richiedere stato globale.
            return result.Transition.DayChanged
                   && result.Transition.Current.Date.DayOfYear == 1
                   && result.Climate.Season == EnvironmentSeasonKind.Spring
                   && result.SeasonProfile.Season == EnvironmentSeasonKind.Spring
                   && result.EvolutionReport.AreasVisited == 1
                   && result.EvolutionReport.HasChanges
                   && result.SnapshotDiff.HasChanges
                   && result.SnapshotDiff.ModifiedCount == 1
                   && result.Snapshot.Calendar.Date.DayOfYear == 1
                   && result.Snapshot.Areas.Count == 1
                   && area.HasFertility
                   && area.HasWater
                   && area.HasVegetation;
        }

        private static bool CheckSnapshotDiff(
            EnvironmentCalendarConfig calendarConfig,
            EnvironmentClimateConfig climateConfig)
        {
            var previousSet = new EnvironmentAreaSetConfig
            {
                areas = new[]
                {
                    new EnvironmentAreaConfig
                    {
                        areaId = 12,
                        kind = "Vegetation",
                        minX = 0,
                        minY = 0,
                        maxX = 2,
                        maxY = 2,
                        isEnabled = true,
                        key = "diff_modified"
                    },
                    new EnvironmentAreaConfig
                    {
                        areaId = 13,
                        kind = "Water",
                        minX = 3,
                        minY = 0,
                        maxX = 4,
                        maxY = 1,
                        isEnabled = true,
                        key = "diff_removed"
                    }
                },
                vegetationAreas = new[]
                {
                    new EnvironmentVegetationAreaConfig
                    {
                        areaId = 12,
                        vegetationKind = "Grass",
                        density01 = 0.40f,
                        growthPotential01 = 0.70f,
                        health01 = 0.80f,
                        fertilityInfluence01 = 0.5f,
                        climateInfluence01 = 0.5f
                    }
                },
                waterAreas = new[]
                {
                    new EnvironmentWaterAreaConfig
                    {
                        areaId = 13,
                        waterKind = "Lake",
                        depthLevel = "Ford",
                        waterLevel01 = 0.50f,
                        isDrinkable = true
                    }
                }
            };
            var currentSet = new EnvironmentAreaSetConfig
            {
                areas = new[]
                {
                    new EnvironmentAreaConfig
                    {
                        areaId = 12,
                        kind = "Vegetation",
                        minX = 0,
                        minY = 0,
                        maxX = 2,
                        maxY = 2,
                        isEnabled = true,
                        key = "diff_modified"
                    },
                    new EnvironmentAreaConfig
                    {
                        areaId = 14,
                        kind = "Fertility",
                        minX = 5,
                        minY = 0,
                        maxX = 6,
                        maxY = 1,
                        isEnabled = true,
                        key = "diff_added"
                    }
                },
                vegetationAreas = new[]
                {
                    new EnvironmentVegetationAreaConfig
                    {
                        areaId = 12,
                        vegetationKind = "Grass",
                        density01 = 0.60f,
                        growthPotential01 = 0.70f,
                        health01 = 0.80f,
                        fertilityInfluence01 = 0.5f,
                        climateInfluence01 = 0.5f
                    }
                },
                fertilityAreas = new[]
                {
                    new EnvironmentFertilityAreaConfig
                    {
                        areaId = 14,
                        soilKind = "Grassland",
                        baseFertility01 = 0.7f,
                        currentFertility01 = 0.7f,
                        growthModifier01 = 0.7f,
                        recovery01 = 0.6f
                    }
                }
            };
            var previous = EnvironmentFoundationBuilder.BuildSnapshot(
                0,
                calendarConfig,
                climateConfig,
                previousSet);
            var current = EnvironmentFoundationBuilder.BuildSnapshot(
                0,
                calendarConfig,
                climateConfig,
                currentSet);
            var diff = EnvironmentSnapshotDiffResolver.Diff(previous, current);
            var noDiff = EnvironmentSnapshotDiffResolver.Diff(previous, previous);
            bool modifiedVegetation = false;
            bool addedFertility = false;
            bool removedWater = false;

            for (int i = 0; i < diff.Changes.Count; i++)
            {
                var change = diff.Changes[i];
                if (change.AreaId.Equals(new EnvironmentAreaId(12)))
                {
                    modifiedVegetation =
                        change.Kind == EnvironmentSnapshotAreaChangeKind.Modified
                        && change.HasVegetationChange
                        && !change.HasWaterChange;
                }
                else if (change.AreaId.Equals(new EnvironmentAreaId(14)))
                {
                    addedFertility =
                        change.Kind == EnvironmentSnapshotAreaChangeKind.Added
                        && change.HasDefinitionChange
                        && change.Current.HasFertility;
                }
                else if (change.AreaId.Equals(new EnvironmentAreaId(13)))
                {
                    removedWater =
                        change.Kind == EnvironmentSnapshotAreaChangeKind.Removed
                        && change.HasWaterChange
                        && change.Previous.HasWater;
                }
            }

            // Il diff deve distinguere aggiunte, rimozioni e modifiche layer-specific,
            // mentre snapshot identici devono risultare privi di cambiamenti.
            return diff.HasChanges
                   && diff.AddedCount == 1
                   && diff.RemovedCount == 1
                   && diff.ModifiedCount == 1
                   && modifiedVegetation
                   && addedFertility
                   && removedWater
                   && !noDiff.HasChanges;
        }

        private static bool CheckSeedBank(
            EnvironmentCalendarConfig calendarConfig,
            EnvironmentClimateConfig climateConfig)
        {
            var areaSet = new EnvironmentAreaSetConfig
            {
                areas = new[]
                {
                    new EnvironmentAreaConfig
                    {
                        areaId = 15,
                        kind = "Vegetation",
                        minX = 0,
                        minY = 0,
                        maxX = 2,
                        maxY = 2,
                        priority = 1,
                        isEnabled = true,
                        key = "seed_bank_probe"
                    }
                },
                vegetationAreas = new[]
                {
                    new EnvironmentVegetationAreaConfig
                    {
                        areaId = 15,
                        vegetationKind = "Grass",
                        density01 = 0.45f,
                        growthPotential01 = 0.8f,
                        health01 = 0.70f,
                        fertilityInfluence01 = 0.8f,
                        climateInfluence01 = 0.7f
                    }
                },
                seedBankAreas = new[]
                {
                    new EnvironmentSeedBankAreaConfig
                    {
                        areaId = 15,
                        entries = new[]
                        {
                            new EnvironmentSeedBankEntryConfig
                            {
                                speciesKey = "wild_grass",
                                amount01 = 0.8f,
                                viability01 = 0.9f
                            },
                            new EnvironmentSeedBankEntryConfig
                            {
                                speciesKey = "clover",
                                amount01 = 0.4f,
                                viability01 = 0.7f
                            }
                        }
                    }
                }
            };
            var build = EnvironmentFoundationBuilder.BuildState(
                0,
                calendarConfig,
                climateConfig,
                areaSet);
            var snapshot = build.State.CreateSnapshot();
            var query = EnvironmentSnapshotQuery.QueryCell(
                snapshot,
                new EnvironmentCellCoord(1, 1));
            var area = query.AreaCount == 1
                ? query.Areas[0]
                : default;
            long oneDayTicks =
                EnvironmentCalendarConfig.DefaultHoursPerDay
                * EnvironmentCalendarConfig.DefaultCalendarTicksPerSimulatedHour;
            var advanced = EnvironmentAdvanceResolver.AdvanceSnapshot(
                snapshot,
                0,
                oneDayTicks,
                calendarConfig,
                climateConfig);
            var advancedArea = advanced.Snapshot.Areas.Count == 1
                ? advanced.Snapshot.Areas[0]
                : default;

            var changedSet = new EnvironmentAreaSetConfig
            {
                areas = areaSet.areas,
                vegetationAreas = areaSet.vegetationAreas,
                seedBankAreas = new[]
                {
                    new EnvironmentSeedBankAreaConfig
                    {
                        areaId = 15,
                        entries = new[]
                        {
                            new EnvironmentSeedBankEntryConfig
                            {
                                speciesKey = "wild_grass",
                                amount01 = 0.2f,
                                viability01 = 0.3f
                            }
                        }
                    }
                }
            };
            var changedSnapshot = EnvironmentFoundationBuilder.BuildSnapshot(
                0,
                calendarConfig,
                climateConfig,
                changedSet);
            var diff = EnvironmentSnapshotDiffResolver.Diff(snapshot, changedSnapshot);
            bool seedBankDiff = false;
            for (int i = 0; i < diff.Changes.Count; i++)
            {
                if (!diff.Changes[i].AreaId.Equals(new EnvironmentAreaId(15)))
                    continue;

                seedBankDiff =
                    diff.Changes[i].Kind == EnvironmentSnapshotAreaChangeKind.Modified
                    && diff.Changes[i].HasSeedBankChange;
            }

            // Il seed bank deve passare da config a snapshot, restare leggibile via
            // query, conservarsi durante advance e risultare diffabile come layer.
            return build.Report.SeedBankAreasApplied == 1
                   && query.HasSeedBank
                   && area.HasSeedBank
                   && area.SeedBankState.Entries.Count == 2
                   && area.SeedBankState.TotalAmount01 > 0.5f
                   && area.SeedBankState.AverageViability01 > 0.7f
                   && advancedArea.HasSeedBank
                   && advancedArea.SeedBankState.Entries.Count == 2
                   && seedBankDiff;
        }

        private static bool CheckBootstrap()
        {
            var config = EnvironmentFoundationBootstrap.CreateDefaultConfig();
            config.initialEnvironmentTicks =
                EnvironmentCalendarConfig.DefaultCalendarTicksPerSimulatedHour;
            config.areas = new EnvironmentAreaSetConfig
            {
                areas = new[]
                {
                    new EnvironmentAreaConfig
                    {
                        areaId = 16,
                        kind = "Vegetation",
                        minX = 0,
                        minY = 0,
                        maxX = 1,
                        maxY = 1,
                        isEnabled = true,
                        key = "bootstrap_probe"
                    }
                },
                vegetationAreas = new[]
                {
                    new EnvironmentVegetationAreaConfig
                    {
                        areaId = 16,
                        vegetationKind = "Grass",
                        density01 = 0.4f,
                        growthPotential01 = 0.7f,
                        health01 = 0.8f,
                        fertilityInfluence01 = 0.5f,
                        climateInfluence01 = 0.5f
                    }
                },
                seedBankAreas = new[]
                {
                    new EnvironmentSeedBankAreaConfig
                    {
                        areaId = 16,
                        entries = new[]
                        {
                            new EnvironmentSeedBankEntryConfig
                            {
                                speciesKey = "bootstrap_grass",
                                amount01 = 0.5f,
                                viability01 = 0.6f
                            }
                        }
                    }
                }
            };

            var result = EnvironmentFoundationBootstrap.Bootstrap(config);
            var query = EnvironmentSnapshotQuery.QueryCell(
                result.Snapshot,
                new EnvironmentCellCoord(0, 0));
            var nullBootstrap = EnvironmentFoundationBootstrap.Bootstrap(null);
            var suspiciousRoot = EnvironmentFoundationBootstrap.CreateDefaultConfig();
            suspiciousRoot.schemaVersion = EnvironmentFoundationConfig.CurrentSchemaVersion + 1;
            suspiciousRoot.configKey = string.Empty;
            suspiciousRoot.initialEnvironmentTicks = -10;
            var suspiciousValidation = EnvironmentConfigValidator.Validate(suspiciousRoot);

            // Il bootstrap radice deve validare, costruire e produrre snapshot senza
            // richiedere loader, file system o riferimenti runtime.
            return result.IsValid
                   && config.ResolveSchemaVersion() == EnvironmentFoundationConfig.CurrentSchemaVersion
                   && config.ResolveConfigKey() == "default_environment"
                   && result.Validation.ErrorCount == 0
                   && result.Build.Report.AreaDefinitionsApplied == 1
                   && result.Build.Report.VegetationAreasApplied == 1
                   && result.Build.Report.SeedBankAreasApplied == 1
                   && result.PlantCatalog.SpeciesCount >= 2
                   && result.PlantCatalog.ContainsSpecies("wild_grass")
                   && result.Snapshot.Calendar.TimeOfDay.Hour == 1
                   && result.Snapshot.Areas.Count == 1
                   && query.HasVegetation
                   && query.HasSeedBank
                   && nullBootstrap.IsValid
                   && nullBootstrap.Snapshot.Areas.Count == 0
                   && suspiciousValidation.IsValid
                   && suspiciousValidation.WarningCount >= 3;
        }

        private static bool CheckPlantCatalog()
        {
            var config = new EnvironmentPlantCatalogConfig
            {
                species = new[]
                {
                    new EnvironmentPlantSpeciesConfig
                    {
                        speciesKey = "healing_herb",
                        category = "Medicinal",
                        growthStages = new[]
                        {
                            new EnvironmentPlantGrowthStageConfig
                            {
                                stageKey = "sprout",
                                requiredAgeDays = 0,
                                maturity01 = 0.15f
                            },
                            new EnvironmentPlantGrowthStageConfig
                            {
                                stageKey = "flowering",
                                requiredAgeDays = 12,
                                maturity01 = 1f,
                                isHarvestable = true
                            }
                        },
                        favorableSeasons = new[] { "Spring", "Summer" },
                        idealTemperature01 = 0.65f,
                        idealHumidity01 = 0.55f,
                        minimumFertility01 = 0.45f,
                        resourceOutputKey = "healing_leaf",
                        seasonalBehavior = "Annual"
                    },
                    new EnvironmentPlantSpeciesConfig
                    {
                        speciesKey = "healing_herb",
                        category = "Grass"
                    },
                    new EnvironmentPlantSpeciesConfig
                    {
                        speciesKey = string.Empty,
                        category = "Tree"
                    }
                }
            };

            var catalog = config.ToCatalog();
            bool hasHerb = catalog.TryGetSpecies(
                "healing_herb",
                out EnvironmentPlantSpeciesDefinition herb);
            bool adultStage = hasHerb
                              && herb.TryGetStageForAge(
                                  20,
                                  out EnvironmentPlantGrowthStageDefinition stage)
                              && stage.StageKey == "flowering"
                              && stage.IsHarvestable;
            bool seasonsOk = hasHerb
                             && herb.IsSeasonFavorable(EnvironmentSeasonKind.Spring)
                             && herb.IsSeasonFavorable(EnvironmentSeasonKind.Summer)
                             && !herb.IsSeasonFavorable(EnvironmentSeasonKind.Winter);
            bool duplicateIgnored = hasHerb
                                    && catalog.SpeciesCount == 1
                                    && herb.Category == EnvironmentPlantCategory.Medicinal;

            var root = EnvironmentFoundationBootstrap.CreateDefaultConfig();
            root.plantCatalog = config;
            var validation = EnvironmentConfigValidator.Validate(root);
            bool diagnosticsOk = validation.IsValid
                                 && validation.WarningCount >= 2;
            var bootstrap = EnvironmentFoundationBootstrap.Bootstrap(root);

            // Il catalogo piante deve restare read-only, queryable per speciesKey e
            // separato dallo stato aree. Duplicati e chiavi vuote sono diagnosticati
            // ma non bloccano il bootstrap data-only.
            return hasHerb
                   && adultStage
                   && seasonsOk
                   && duplicateIgnored
                   && diagnosticsOk
                   && bootstrap.PlantCatalog.ContainsSpecies("healing_herb")
                   && bootstrap.Snapshot.Areas.Count == 0;
        }

        private static bool CheckPlantInstance()
        {
            var catalog = new EnvironmentPlantCatalogConfig().ToCatalog();
            bool hasOak = catalog.TryGetSpecies(
                "oak_tree",
                out EnvironmentPlantSpeciesDefinition oak);

            var plantId = new EnvironmentPlantId(9001);
            var cell = new EnvironmentCellCoord(7, 3, 0);
            var sourceArea = new EnvironmentAreaId(21);
            var plant = EnvironmentPlantInstance.CreateFromSpecies(
                plantId,
                oak,
                cell,
                300,
                0.82f,
                sourceArea);

            var state = new EnvironmentState();
            bool rejectedMissingSpecies = !state.SetPlantInstance(
                new EnvironmentPlantInstance(
                    new EnvironmentPlantId(1),
                    string.Empty,
                    cell,
                    0,
                    EnvironmentPlantGrowthStage.Seedling,
                    "seedling",
                    EnvironmentPlantHealthState.Healthy,
                    1f,
                    0f,
                    false,
                    EnvironmentAreaId.None));
            bool accepted = state.SetPlantInstance(plant);
            bool lookup = state.TryGetPlantInstance(plantId, out EnvironmentPlantInstance stored);
            var snapshot = state.CreateSnapshot();
            bool snapshotLookup = EnvironmentSnapshotQuery.TryGetPlant(
                snapshot,
                plantId,
                out EnvironmentPlantSnapshot plantSnapshot);
            var plantsAtCell = EnvironmentSnapshotQuery.QueryPlantsAtCell(snapshot, cell);
            var plantsBySpecies = EnvironmentSnapshotQuery.QueryPlantsBySpecies(
                snapshot,
                "OAK_TREE");
            bool removed = state.RemovePlantInstance(plantId);
            var emptySnapshot = state.CreateSnapshot();

            // La PlantInstance deve poter vivere nello stato Core ed essere letta come
            // snapshot, ma non deve attivare crescita, rendering o ownership esterna.
            return hasOak
                   && plant.PlantId.Equals(plantId)
                   && plant.SpeciesKey == "oak_tree"
                   && plant.GrowthStage == EnvironmentPlantGrowthStage.Mature
                   && plant.GrowthStageKey == "adult"
                   && plant.IsHarvestable
                   && plant.IsAlive
                   && plant.SourceAreaId.Equals(sourceArea)
                   && rejectedMissingSpecies
                   && accepted
                   && lookup
                   && stored.AgeDays == 300
                   && snapshot.Plants.Count == 1
                   && snapshotLookup
                   && plantSnapshot.PlantId.Equals(plantId)
                   && plantSnapshot.Cell.Equals(cell)
                   && plantSnapshot.HealthState == EnvironmentPlantHealthState.Healthy
                   && plantsAtCell.Count == 1
                   && plantsBySpecies.Count == 1
                   && removed
                   && emptySnapshot.Plants.Count == 0;
        }

        private static bool CheckNaturalGrowth()
        {
            var calendarConfig = new EnvironmentCalendarConfig();
            var previous = EnvironmentCalendarResolver.Resolve(0, calendarConfig);
            var transition = EnvironmentTemporalTransitionResolver.ResolveFromPreviousCalendar(
                previous,
                EnvironmentCalendarConfig.DefaultHoursPerDay
                * EnvironmentCalendarConfig.DefaultCalendarTicksPerSimulatedHour,
                calendarConfig);
            var seasonProfile = new EnvironmentSeasonProfile(
                EnvironmentSeasonKind.Spring,
                12f,
                0.55f,
                0.8f,
                0.8f,
                0.9f);
            var climate = new EnvironmentGlobalClimateState(
                0.58f,
                0.72f,
                0.05f,
                new EnvironmentWeatherState(
                    EnvironmentWeatherKind.Rain,
                    0.5f,
                    0.7f,
                    0.1f,
                    false),
                EnvironmentSeasonKind.Spring);
            var areaId = new EnvironmentAreaId(30);
            var state = new EnvironmentState();
            state.SetCalendar(previous);
            state.SetClimate(climate);
            state.SetAreaDefinition(new EnvironmentAreaDefinition(
                areaId,
                EnvironmentAreaKind.Vegetation,
                new EnvironmentAreaBounds(4, 4, 8, 8),
                0,
                true,
                "natural_growth_probe"));
            state.SetFertilityArea(new EnvironmentFertilityAreaState(
                areaId,
                EnvironmentSoilKind.Forest,
                0.85f,
                0.80f,
                0.85f,
                0.05f,
                0.85f));
            state.SetWaterArea(new EnvironmentWaterAreaState(
                areaId,
                EnvironmentWaterKind.Still,
                EnvironmentWaterDepthLevel.Shallow,
                0.55f,
                0f,
                true,
                true));
            state.SetVegetationArea(new EnvironmentVegetationAreaState(
                areaId,
                EnvironmentVegetationKind.Forest,
                0.65f,
                0.9f,
                0.88f,
                0.9f,
                0.9f));
            state.SetSeedBankArea(new EnvironmentSeedBankAreaState(
                areaId,
                new[]
                {
                    new EnvironmentSeedBankEntry(
                        "oak_tree",
                        0.95f,
                        0.95f)
                }));

            var catalog = new EnvironmentPlantCatalogConfig().ToCatalog();
            catalog.TryGetSpecies("oak_tree", out EnvironmentPlantSpeciesDefinition oak);
            state.SetPlantInstance(EnvironmentPlantInstance.CreateFromSpecies(
                new EnvironmentPlantId(500),
                oak,
                new EnvironmentCellCoord(6, 6),
                240,
                0.75f,
                areaId));

            var result = EnvironmentNaturalGrowthResolver.Evolve(
                state.CreateSnapshot(),
                catalog,
                transition,
                climate,
                seasonProfile,
                new EnvironmentNaturalGrowthConfig
                {
                    maxNewPlantsPerDay = 2,
                    maxNewPlantsPerAreaPerDay = 1,
                    minimumGerminationScore01 = 0.50f
                });
            var snapshot = result.State.CreateSnapshot();
            bool existingUpdated = EnvironmentSnapshotQuery.TryGetPlant(
                snapshot,
                new EnvironmentPlantId(500),
                out EnvironmentPlantSnapshot existing)
                                   && existing.AgeDays == 241
                                   && existing.Health01 > 0.75f;
            var oaks = EnvironmentSnapshotQuery.QueryPlantsBySpecies(snapshot, "oak_tree");
            var cellPlants = EnvironmentSnapshotQuery.QueryPlantsAtCell(
                snapshot,
                new EnvironmentCellCoord(4, 4));
            state.SetPlantInstance(EnvironmentPlantInstance.CreateFromSpecies(
                new EnvironmentPlantId(501),
                oak,
                new EnvironmentCellCoord(7, 6),
                240,
                0.75f,
                areaId));
            var budgeted = EnvironmentNaturalGrowthResolver.Evolve(
                state.CreateSnapshot(),
                catalog,
                transition,
                climate,
                seasonProfile,
                new EnvironmentNaturalGrowthConfig
                {
                    allowNewPlantInstances = false,
                    maxExistingPlantUpdatesPerDay = 1,
                    maxAreasProcessedPerDay = 1,
                    minimumGerminationScore01 = 0.50f
                });
            var budgetedSnapshot = budgeted.State.CreateSnapshot();
            bool firstBudgetedPlantUpdated = EnvironmentSnapshotQuery.TryGetPlant(
                budgetedSnapshot,
                new EnvironmentPlantId(500),
                out EnvironmentPlantSnapshot firstBudgetedPlant)
                                            && firstBudgetedPlant.AgeDays == 241;
            bool secondBudgetedPlantPreserved = EnvironmentSnapshotQuery.TryGetPlant(
                budgetedSnapshot,
                new EnvironmentPlantId(501),
                out EnvironmentPlantSnapshot secondBudgetedPlant)
                                                && secondBudgetedPlant.AgeDays == 240;

            // Il ciclo naturale deve collegare area, seed bank e piante in modo
            // esplicito e giornaliero: nessun tick frame-based e nessun consumer esterno.
            // Il budget runtime deve limitare il lavoro senza cancellare le piante
            // che non rientrano nel batch corrente.
            return transition.DayChanged
                   && result.Report.AreasVisited == 1
                   && result.Report.SeedBankEntriesVisited == 1
                   && result.Report.SeedBanksUpdated == 1
                   && result.Report.ExistingPlantsVisited == 1
                   && result.Report.PlantInstancesUpdated == 1
                   && result.Report.PlantInstancesCreated == 1
                   && result.Report.HasChanges
                   && snapshot.Areas.Count == 1
                   && snapshot.Plants.Count == 2
                   && existingUpdated
                   && oaks.Count == 2
                   && cellPlants.Count == 1
                   && cellPlants[0].AgeDays == 0
                   && cellPlants[0].IsAlive
                   && budgeted.Report.ExistingPlantsVisited == 2
                   && budgeted.Report.PlantInstancesUpdated == 1
                   && budgeted.Report.PlantInstancesCreated == 0
                   && budgetedSnapshot.Plants.Count == 2
                   && firstBudgetedPlantUpdated
                   && secondBudgetedPlantPreserved;
        }

        private static bool CheckAgricultureFoundation()
        {
            var catalogConfig = new EnvironmentPlantCatalogConfig
            {
                species = new[]
                {
                    new EnvironmentPlantSpeciesConfig
                    {
                        speciesKey = "wheat_crop",
                        category = "Crop",
                        growthStages = new[]
                        {
                            new EnvironmentPlantGrowthStageConfig
                            {
                                stageKey = "sprout",
                                requiredAgeDays = 0,
                                maturity01 = 0.15f
                            },
                            new EnvironmentPlantGrowthStageConfig
                            {
                                stageKey = "ripe",
                                requiredAgeDays = 18,
                                maturity01 = 1f,
                                isHarvestable = true
                            }
                        },
                        favorableSeasons = new[] { "Spring", "Summer" },
                        idealTemperature01 = 0.55f,
                        idealHumidity01 = 0.60f,
                        minimumFertility01 = 0.50f,
                        resourceOutputKey = "wheat_grain",
                        seasonalBehavior = "Annual"
                    },
                    new EnvironmentPlantSpeciesConfig
                    {
                        speciesKey = "field_flower",
                        category = "Medicinal"
                    }
                }
            };
            var catalog = catalogConfig.ToCatalog();
            var cropArea = new EnvironmentCultivatedAreaState(
                new EnvironmentAreaId(40),
                "wheat_crop",
                EnvironmentCultivationStage.ReadyToHarvest,
                0.9f,
                0.7f,
                0.1f,
                0.85f,
                true);
            var concreteSeed = new EnvironmentAgriculturalSeedResourceBoundary(
                "seed_wheat",
                "wheat_crop",
                12,
                true);
            var naturalSeed = new EnvironmentAgriculturalSeedResourceBoundary(
                string.Empty,
                "field_flower",
                12,
                false);
            bool cropSeedOk = EnvironmentAgricultureFoundationResolver.CanSowSeed(
                concreteSeed,
                catalog);
            bool naturalSeedRejected = !EnvironmentAgricultureFoundationResolver.CanSowSeed(
                naturalSeed,
                catalog);

            catalog.TryGetSpecies("wheat_crop", out EnvironmentPlantSpeciesDefinition wheat);
            var plant = EnvironmentPlantInstance.CreateFromSpecies(
                new EnvironmentPlantId(700),
                wheat,
                new EnvironmentCellCoord(10, 11),
                20,
                0.9f,
                cropArea.AreaId);
            bool harvestOk = EnvironmentAgricultureFoundationResolver.TryBuildHarvestOutput(
                plant.ToSnapshot(),
                catalog,
                out EnvironmentHarvestOutput output);
            var hook = EnvironmentAgricultureFoundationResolver.BuildHook(
                EnvironmentAgricultureIntentKind.Harvest,
                cropArea.AreaId,
                plant.PlantId,
                plant.SpeciesKey,
                plant.Cell,
                5,
                true);

            // L'agricoltura resta dichiarativa: possiamo dire che un seme concreto e'
            // seminabile, che una pianta matura espone raccolto e che un hook futuro
            // avrebbe senso, ma non creiamo job, item o inventari.
            return cropArea.IsActive
                   && cropArea.Stage == EnvironmentCultivationStage.ReadyToHarvest
                   && cropArea.CropSpeciesKey == "wheat_crop"
                   && cropArea.SoilPreparation01 > 0.8f
                   && concreteSeed.IsUsable
                   && cropSeedOk
                   && naturalSeedRejected
                   && harvestOk
                   && output.IsAvailable
                   && output.ResourceOutputKey == "wheat_grain"
                   && output.Amount01 > 0.8f
                   && output.Quality01 > 0.9f
                   && hook.IsEnabled
                   && hook.IntentKind == EnvironmentAgricultureIntentKind.Harvest
                   && hook.AreaId.Equals(cropArea.AreaId)
                   && hook.PlantId.Equals(plant.PlantId)
                   && hook.Priority == 5;
        }

        private static bool CheckReadOnlySnapshots()
        {
            var calendarConfig = new EnvironmentCalendarConfig();
            var calendar = EnvironmentCalendarResolver.Resolve(
                EnvironmentCalendarConfig.DefaultCalendarTicksPerSimulatedHour,
                calendarConfig);
            var climate = new EnvironmentGlobalClimateState(
                0.62f,
                0.70f,
                0.10f,
                new EnvironmentWeatherState(
                    EnvironmentWeatherKind.Rain,
                    0.6f,
                    0.8f,
                    0.2f,
                    false),
                EnvironmentSeasonKind.Spring);
            var areaId = new EnvironmentAreaId(60);
            var state = new EnvironmentState();
            state.SetCalendar(calendar);
            state.SetClimate(climate);
            state.SetAreaDefinition(new EnvironmentAreaDefinition(
                areaId,
                EnvironmentAreaKind.Vegetation,
                new EnvironmentAreaBounds(1, 1, 3, 3),
                2,
                true,
                "snapshot_probe"));
            state.SetFertilityArea(new EnvironmentFertilityAreaState(
                areaId,
                EnvironmentSoilKind.Forest,
                0.8f,
                0.75f,
                0.7f,
                0.1f,
                0.8f));
            state.SetWaterArea(new EnvironmentWaterAreaState(
                areaId,
                EnvironmentWaterKind.Puddle,
                EnvironmentWaterDepthLevel.Shallow,
                0.35f,
                0f,
                true,
                true));
            state.SetVegetationArea(new EnvironmentVegetationAreaState(
                areaId,
                EnvironmentVegetationKind.Underbrush,
                0.55f,
                0.65f,
                0.72f,
                0.8f,
                0.7f));
            state.SetSeedBankArea(new EnvironmentSeedBankAreaState(
                areaId,
                new[]
                {
                    new EnvironmentSeedBankEntry("wild_grass", 0.6f, 0.7f),
                    new EnvironmentSeedBankEntry("oak_tree", 0.4f, 0.5f)
                }));

            var catalog = new EnvironmentPlantCatalogConfig().ToCatalog();
            catalog.TryGetSpecies("oak_tree", out EnvironmentPlantSpeciesDefinition oak);
            var plant = EnvironmentPlantInstance.CreateFromSpecies(
                new EnvironmentPlantId(1200),
                oak,
                new EnvironmentCellCoord(2, 2),
                300,
                0.9f,
                areaId);
            state.SetPlantInstance(plant);

            var full = EnvironmentReadOnlySnapshotResolver.BuildFullSnapshot(
                state.CreateSnapshot());
            var empty = EnvironmentReadOnlySnapshotResolver.BuildFullSnapshot(null);

            // Il full snapshot deve separare i domini senza perdere il contratto Core:
            // ogni lista e' materializzata e nessun tipo ArcGraph o Job compare qui.
            return full.Calendar.Hour == 1
                   && full.Calendar.Season == EnvironmentSeasonKind.Spring
                   && full.Climate.Temperature01 == climate.Temperature01
                   && full.Weather.Kind == EnvironmentWeatherKind.Rain
                   && full.Weather.Precipitation01 == 0.8f
                   && full.AreaCount == 1
                   && full.FertilityAreas.Count == 1
                   && full.FertilityAreas[0].AreaId.Equals(areaId)
                   && full.FertilityAreas[0].SoilKind == EnvironmentSoilKind.Forest
                   && full.WaterAreas.Count == 1
                   && full.WaterAreas[0].WaterKind == EnvironmentWaterKind.Puddle
                   && full.VegetationAreas.Count == 1
                   && full.VegetationAreas[0].VegetationKind == EnvironmentVegetationKind.Underbrush
                   && full.SeedBankAreas.Count == 1
                   && full.SeedBankAreas[0].Entries.Count == 2
                   && full.PlantCount == 1
                   && full.Plants[0].PlantId.Equals(new EnvironmentPlantId(1200))
                   && full.Plants[0].GrowthStage == EnvironmentPlantGrowthStage.Mature
                   && empty.AreaCount == 0
                   && empty.PlantCount == 0
                   && empty.FertilityAreas.Count == 0
                   && empty.WaterAreas.Count == 0
                   && empty.VegetationAreas.Count == 0
                   && empty.SeedBankAreas.Count == 0;
        }

        private static bool CheckPersistence()
        {
            var calendarConfig = new EnvironmentCalendarConfig();
            var calendar = EnvironmentCalendarResolver.Resolve(
                EnvironmentCalendarConfig.DefaultCalendarTicksPerSimulatedHour * 30,
                calendarConfig);
            var climate = new EnvironmentGlobalClimateState(
                0.48f,
                0.66f,
                0.18f,
                new EnvironmentWeatherState(
                    EnvironmentWeatherKind.Wind,
                    0.4f,
                    0.1f,
                    0.7f,
                    false),
                EnvironmentSeasonKind.Spring);
            var areaId = new EnvironmentAreaId(80);
            var state = new EnvironmentState();
            state.SetCalendar(calendar);
            state.SetClimate(climate);
            state.SetAreaDefinition(new EnvironmentAreaDefinition(
                areaId,
                EnvironmentAreaKind.Vegetation,
                new EnvironmentAreaBounds(2, 5, 6, 9),
                4,
                true,
                "persist_probe"));
            state.SetFertilityArea(new EnvironmentFertilityAreaState(
                areaId,
                EnvironmentSoilKind.Grassland,
                0.78f,
                0.73f,
                0.69f,
                0.12f,
                0.81f));
            state.SetWaterArea(new EnvironmentWaterAreaState(
                areaId,
                EnvironmentWaterKind.River,
                EnvironmentWaterDepthLevel.Ford,
                0.52f,
                0.45f,
                true,
                false));
            state.SetVegetationArea(new EnvironmentVegetationAreaState(
                areaId,
                EnvironmentVegetationKind.Grass,
                0.58f,
                0.67f,
                0.77f,
                0.8f,
                0.6f));
            state.SetSeedBankArea(new EnvironmentSeedBankAreaState(
                areaId,
                new[]
                {
                    new EnvironmentSeedBankEntry("wild_grass", 0.7f, 0.8f)
                }));

            var catalog = new EnvironmentPlantCatalogConfig().ToCatalog();
            catalog.TryGetSpecies("wild_grass", out EnvironmentPlantSpeciesDefinition grass);
            var plant = EnvironmentPlantInstance.CreateFromSpecies(
                new EnvironmentPlantId(8080),
                grass,
                new EnvironmentCellCoord(3, 6),
                9,
                0.88f,
                areaId);
            state.SetPlantInstance(plant);

            var saveData = EnvironmentPersistenceResolver.Capture(state.CreateSnapshot());
            var load = EnvironmentPersistenceResolver.Restore(saveData, calendarConfig);
            var restoredSnapshot = load.State.CreateSnapshot();
            var full = EnvironmentReadOnlySnapshotResolver.BuildFullSnapshot(restoredSnapshot);
            bool restoredPlant = EnvironmentSnapshotQuery.TryGetPlant(
                restoredSnapshot,
                new EnvironmentPlantId(8080),
                out EnvironmentPlantSnapshot restoredPlantSnapshot);

            // Il save data deve contenere solo stato canonico persistente. Le viste
            // read-only vengono ricostruite dopo il load e gli snapshot visuali restano fuori.
            return EnvironmentPersistenceResolver.Manifest.Calendar == EnvironmentPersistenceKind.Persistent
                   && EnvironmentPersistenceResolver.Manifest.AreaPayloads == EnvironmentPersistenceKind.Persistent
                   && EnvironmentPersistenceResolver.Manifest.PlantInstances == EnvironmentPersistenceKind.Persistent
                   && EnvironmentPersistenceResolver.Manifest.IsVisualStateExcluded
                   && saveData.ResolveSchemaVersion() == EnvironmentSaveData.CurrentSchemaVersion
                   && saveData.elapsedEnvironmentTicks == calendar.ElapsedEnvironmentTicks
                   && saveData.areas.Length == 1
                   && saveData.plants.Length == 1
                   && saveData.areas[0].hasFertility
                   && saveData.areas[0].hasWater
                   && saveData.areas[0].hasVegetation
                   && saveData.areas[0].hasSeedBank
                   && saveData.areas[0].seedBank.entries.Length == 1
                   && load.Report.AreasLoaded == 1
                   && load.Report.PlantsLoaded == 1
                   && !load.Report.HasRejectedRecords
                   && restoredSnapshot.Calendar.ElapsedEnvironmentTicks == calendar.ElapsedEnvironmentTicks
                   && restoredSnapshot.Climate.Weather.Kind == EnvironmentWeatherKind.Wind
                   && full.AreaCount == 1
                   && full.PlantCount == 1
                   && full.WaterAreas[0].WaterKind == EnvironmentWaterKind.River
                   && restoredPlant
                   && restoredPlantSnapshot.SpeciesKey == "wild_grass"
                   && restoredPlantSnapshot.Cell.Equals(new EnvironmentCellCoord(3, 6));
        }

        private static bool CheckVisualProjection()
        {
            var calendar = EnvironmentCalendarResolver.Resolve(
                EnvironmentCalendarConfig.DefaultCalendarTicksPerSimulatedHour * 12,
                new EnvironmentCalendarConfig());
            var climate = new EnvironmentGlobalClimateState(
                0.6f,
                0.75f,
                0.1f,
                new EnvironmentWeatherState(
                    EnvironmentWeatherKind.Rain,
                    0.8f,
                    0.9f,
                    0.2f,
                    false),
                EnvironmentSeasonKind.Spring);
            var areaId = new EnvironmentAreaId(90);
            var state = new EnvironmentState();
            state.SetCalendar(calendar);
            state.SetClimate(climate);
            state.SetAreaDefinition(new EnvironmentAreaDefinition(
                areaId,
                EnvironmentAreaKind.Vegetation,
                new EnvironmentAreaBounds(0, 0, 4, 4),
                0,
                true,
                "visual_projection_probe"));
            state.SetWaterArea(new EnvironmentWaterAreaState(
                areaId,
                EnvironmentWaterKind.Puddle,
                EnvironmentWaterDepthLevel.Shallow,
                0.45f,
                0f,
                true,
                true));
            state.SetVegetationArea(new EnvironmentVegetationAreaState(
                areaId,
                EnvironmentVegetationKind.Grass,
                0.6f,
                0.7f,
                0.8f,
                0.7f,
                0.7f));

            var catalog = new EnvironmentPlantCatalogConfig().ToCatalog();
            catalog.TryGetSpecies("oak_tree", out EnvironmentPlantSpeciesDefinition oak);
            state.SetPlantInstance(EnvironmentPlantInstance.CreateFromSpecies(
                new EnvironmentPlantId(9000),
                oak,
                new EnvironmentCellCoord(2, 3),
                300,
                0.9f,
                areaId));

            var full = EnvironmentReadOnlySnapshotResolver.BuildFullSnapshot(
                state.CreateSnapshot());
            var projections = EnvironmentVisualProjectionResolver.BuildProjectionSet(full);
            bool hasWater = false;
            bool hasVegetationArea = false;
            bool hasPlant = false;
            bool hasWeather = false;
            bool hasLight = false;

            for (int i = 0; i < projections.Records.Count; i++)
            {
                var record = projections.Records[i];
                if (record.Layer == EnvironmentVisualProjectionLayer.Water
                    && record.Scope == EnvironmentVisualProjectionScope.Area
                    && record.VisualKey == "water_puddle"
                    && record.IsVisible)
                {
                    hasWater = true;
                }
                else if (record.Layer == EnvironmentVisualProjectionLayer.Vegetation
                         && record.Scope == EnvironmentVisualProjectionScope.Area
                         && record.VisualKey == "vegetation_grass")
                {
                    hasVegetationArea = true;
                }
                else if (record.Layer == EnvironmentVisualProjectionLayer.Vegetation
                         && record.Scope == EnvironmentVisualProjectionScope.Cell
                         && record.VisualKey == "plant_oak_tree"
                         && record.Cell.Equals(new EnvironmentCellCoord(2, 3)))
                {
                    hasPlant = true;
                }
                else if (record.Layer == EnvironmentVisualProjectionLayer.Weather
                         && record.Scope == EnvironmentVisualProjectionScope.Global
                         && record.VisualKey == "weather_rain"
                         && record.IsAnimatedCandidate)
                {
                    hasWeather = true;
                }
                else if (record.Layer == EnvironmentVisualProjectionLayer.Light
                         && record.Scope == EnvironmentVisualProjectionScope.Global
                         && record.VisualKey == "light_global"
                         && record.Intensity01 > 0.9f)
                {
                    hasLight = true;
                }
            }

            // Questa e' readiness adapter, non adapter ArcGraph: il Core produce
            // record visuali neutrali e non importa alcun tipo View.
            return projections.RecordCount == 5
                   && projections.WaterCount == 1
                   && projections.VegetationCount == 2
                   && projections.WeatherCount == 1
                   && projections.LightCount == 1
                   && projections.EffectCount == 0
                   && projections.ContainsLayer(EnvironmentVisualProjectionLayer.Water)
                   && projections.ContainsLayer(EnvironmentVisualProjectionLayer.Vegetation)
                   && projections.ContainsLayer(EnvironmentVisualProjectionLayer.Weather)
                   && projections.ContainsLayer(EnvironmentVisualProjectionLayer.Light)
                   && hasWater
                   && hasVegetationArea
                   && hasPlant
                   && hasWeather
                   && hasLight;
        }

        private static bool CheckConsumerQuery()
        {
            var climate = new EnvironmentGlobalClimateState(
                0.55f,
                0.68f,
                0.12f,
                new EnvironmentWeatherState(
                    EnvironmentWeatherKind.Clear,
                    0f,
                    0f,
                    0.1f,
                    false),
                EnvironmentSeasonKind.Spring);
            var areaId = new EnvironmentAreaId(100);
            var cell = new EnvironmentCellCoord(5, 5);
            var state = new EnvironmentState();
            state.SetCalendar(EnvironmentCalendarResolver.Resolve(0, new EnvironmentCalendarConfig()));
            state.SetClimate(climate);
            state.SetAreaDefinition(new EnvironmentAreaDefinition(
                areaId,
                EnvironmentAreaKind.Vegetation,
                new EnvironmentAreaBounds(4, 4, 8, 8),
                3,
                true,
                "consumer_query_probe"));
            state.SetFertilityArea(new EnvironmentFertilityAreaState(
                areaId,
                EnvironmentSoilKind.Forest,
                0.9f,
                0.82f,
                0.75f,
                0.05f,
                0.8f));
            state.SetWaterArea(new EnvironmentWaterAreaState(
                areaId,
                EnvironmentWaterKind.River,
                EnvironmentWaterDepthLevel.Ford,
                0.65f,
                0.4f,
                true,
                false));
            state.SetVegetationArea(new EnvironmentVegetationAreaState(
                areaId,
                EnvironmentVegetationKind.Forest,
                0.7f,
                0.8f,
                0.86f,
                0.8f,
                0.7f));
            state.SetSeedBankArea(new EnvironmentSeedBankAreaState(
                areaId,
                new[]
                {
                    new EnvironmentSeedBankEntry("oak_tree", 0.6f, 0.8f)
                }));

            var catalog = new EnvironmentPlantCatalogConfig().ToCatalog();
            catalog.TryGetSpecies("oak_tree", out EnvironmentPlantSpeciesDefinition oak);
            bool oakHasWood = oak.TryGetProduct(
                "wood_log",
                out EnvironmentPlantProductDefinition oakWood);
            bool oakHasAcorn = oak.TryGetProduct(
                "acorn",
                out EnvironmentPlantProductDefinition oakAcorn);
            state.SetPlantInstance(EnvironmentPlantInstance.CreateFromSpecies(
                new EnvironmentPlantId(1000),
                oak,
                cell,
                300,
                0.9f,
                areaId));
            state.SetPlantInstance(EnvironmentPlantInstance.CreateFromSpecies(
                new EnvironmentPlantId(1001),
                oak,
                new EnvironmentCellCoord(7, 5),
                300,
                0.8f,
                areaId));

            var full = EnvironmentReadOnlySnapshotResolver.BuildFullSnapshot(state.CreateSnapshot());
            var facts = EnvironmentConsumerQueryResolver.BuildCellFacts(full, cell, catalog);
            var nearby = EnvironmentConsumerQueryResolver.QueryHarvestableResources(
                full,
                catalog,
                cell,
                2);
            var far = EnvironmentConsumerQueryResolver.QueryHarvestableResources(
                full,
                catalog,
                new EnvironmentCellCoord(20, 20),
                1);
            var productFacts = EnvironmentConsumerQueryResolver.QueryPotentialProductsForArea(
                full,
                catalog,
                areaId);
            var acorns = EnvironmentConsumerQueryResolver.QueryHarvestableResourcesForProduct(
                full,
                catalog,
                cell,
                2,
                "acorn");
            var wood = EnvironmentConsumerQueryResolver.QueryHarvestableResourcesForProduct(
                full,
                catalog,
                cell,
                2,
                "wood_log");
            var impossible = EnvironmentConsumerQueryResolver.QueryHarvestableResourcesForProduct(
                full,
                catalog,
                cell,
                2,
                "apple");
            var potentialHints = EnvironmentConsumerQueryResolver.BuildPotentialBeliefHintsForLandmark(
                77,
                productFacts,
                12);
            var observedHints = EnvironmentConsumerQueryResolver.BuildObservedBeliefHintsForLandmark(
                77,
                areaId,
                wood,
                12);
            bool productFactsExposeWood = false;
            bool productFactsExposeAcorn = false;
            for (int i = 0; i < productFacts.Count; i++)
            {
                EnvironmentConsumerProductCandidate product = productFacts[i];
                if (product.ProductKey == "wood_log"
                    && !product.IsFood
                    && product.DestroysPlantOnHarvest
                    && product.RequiresToolKey == "axe"
                    && product.MinGrowthStageKey == "adult"
                    && product.BaseMaxAmountUnits == 8
                    && product.RegrowDays == 0
                    && product.LivePlantCount == 2
                    && product.HarvestablePlantCount == 2)
                {
                    productFactsExposeWood = true;
                }

                if (product.ProductKey == "acorn"
                    && product.IsFood
                    && !product.DestroysPlantOnHarvest
                    && product.RequiresToolKey == string.Empty
                    && product.MinGrowthStageKey == "adult"
                    && product.BaseMaxAmountUnits == 4
                    && product.RegrowDays == 365
                    && product.LivePlantCount == 2
                    && product.HarvestablePlantCount == 2)
                {
                    productFactsExposeAcorn = true;
                }
            }

            bool nutritionPropertyOk = new Arcontio.Core.ObjectDef
            {
                Properties = new System.Collections.Generic.List<Arcontio.Core.ObjectPropertyKV>
                {
                    new Arcontio.Core.ObjectPropertyKV
                    {
                        Key = "NutritionValue",
                        Value = 0.45f
                    }
                }
            }.TryGetPropertyValue(
                "nutritionvalue",
                out float nutritionValue);

            // Il facade consumer deve condensare il read model in facts e candidate
            // risorsa, senza esporre EnvironmentState o avviare job/raccolte.
            // v0.66 congela anche il contratto area/LM -> prodotti potenziali e
            // query locale per prodotto, lasciando job, inventario e belief reali
            // ai layer futuri.
            return facts.Cell.Equals(cell)
                   && facts.Season == EnvironmentSeasonKind.Spring
                   && facts.HasFertility
                   && facts.Fertility01 == 0.82f
                   && facts.HasWater
                   && facts.WaterLevel01 == 0.65f
                   && facts.HasDrinkableWater
                   && facts.HasVegetation
                   && facts.VegetationDensity01 == 0.7f
                   && facts.VegetationHealth01 == 0.86f
                   && facts.SeedBankPressure01 == 0.6f
                   && facts.PlantCount == 1
                   && facts.HarvestablePlantCount == 1
                   && facts.HasHarvestableResource
                   && facts.BestResourceOutputKey == "wood_log"
                   && oakHasWood
                   && oakWood.DestroysPlantOnHarvest
                   && oakWood.RequiresToolKey == "axe"
                   && oakWood.MinGrowthStageKey == "adult"
                   && oakWood.BaseMaxAmountUnits == 8
                   && oakWood.RegrowDays == 0
                   && !oakWood.IsFood
                   && oakHasAcorn
                   && oakAcorn.IsFood
                   && !oakAcorn.DestroysPlantOnHarvest
                   && nearby.Count == 2
                   && nearby[0].IsAvailable
                   && nearby[0].ResourceOutputKey == "wood_log"
                   && nearby[0].DestroysPlantOnHarvest
                   && nearby[0].RequiresToolKey == "axe"
                   && nearby[0].BaseMaxAmountUnits == 8
                   && nearby[0].EstimatedAmountUnits > 0
                   && productFacts.Count == 2
                   && productFactsExposeWood
                   && productFactsExposeAcorn
                   && acorns.Count == 0
                   && wood.Count == 2
                   && wood[0].IsAvailable
                   && wood[0].ResourceOutputKey == "wood_log"
                   && wood[0].DestroysPlantOnHarvest
                   && wood[0].RequiresToolKey == "axe"
                   && wood[0].BaseMaxAmountUnits == 8
                   && wood[0].EstimatedAmountUnits > 0
                   && impossible.Count == 0
                   && potentialHints.Count == 2
                   && potentialHints[0].Kind == EnvironmentBiologicalResourceBeliefKind.Potential
                   && potentialHints[0].LandmarkNodeId == 77
                   && potentialHints[0].EstimatedAmount == 0
                   && potentialHints[0].ObservedDay == 12
                   && observedHints.Count == 1
                   && observedHints[0].Kind == EnvironmentBiologicalResourceBeliefKind.Observed
                   && observedHints[0].LandmarkNodeId == 77
                   && observedHints[0].AreaId.Equals(areaId)
                   && observedHints[0].ProductKey == "wood_log"
                   && observedHints[0].EstimatedAmount > 0
                   && observedHints[0].ObservedDay == 12
                   && nutritionPropertyOk
                   && nutritionValue == 0.45f
                   && far.Count == 0;
        }

        private static bool CheckRuntimeScheduler()
        {
            var disabled = new BiosphereRuntimeParams
            {
                enabled = false,
                simulationTicksPerDailyUpdate = BiosphereRuntimeParams.DefaultSimulationTicksPerDailyUpdate
            };
            var enabled = new BiosphereRuntimeParams
            {
                enabled = true,
                simulationTicksPerDailyUpdate = BiosphereRuntimeParams.DefaultSimulationTicksPerDailyUpdate,
                maxPlantMutationsPerUpdate = 0,
                maxVegetationMutationsPerUpdate = -2,
                maxPlantUpdatesPerDay = 12,
                maxPlantBirthsPerDay = 4,
                maxPlantBirthsPerAreaPerDay = 2,
                maxPlantDeathsPerDay = -1,
                maxVegetationCellsChangedPerDay = 64,
                maxAreasProcessedPerDay = 3,
                updateMode = string.Empty
            };

            int dailyUpdateTicks = BiosphereRuntimeParams.DefaultSimulationTicksPerDailyUpdate;
            var disabledDecision = EnvironmentRuntimeScheduler.Evaluate(disabled, 0, dailyUpdateTicks);
            var beforeBoundary = EnvironmentRuntimeScheduler.Evaluate(enabled, 0, dailyUpdateTicks - 1);
            var onBoundary = EnvironmentRuntimeScheduler.Evaluate(enabled, 0, dailyUpdateTicks);
            var skippedDays = EnvironmentRuntimeScheduler.Evaluate(enabled, 0, dailyUpdateTicks * 3);
            var afterDecisionTick = EnvironmentRuntimeScheduler.ResolveProcessedTickAfterDecision(onBoundary);
            var normalized = BiosphereRuntimeParams.WithFallbackDefaults(enabled);

            // Lo scheduler deve soltanto decidere quando la biosfera e' dovuta:
            // niente World, niente ArcGraph e niente mutazioni reali in questo step.
            return !disabledDecision.IsEnabled
                   && !disabledDecision.ShouldAdvance
                   && disabledDecision.NextDueSimulationTick == dailyUpdateTicks
                   && beforeBoundary.IsEnabled
                   && !beforeBoundary.ShouldAdvance
                   && beforeBoundary.NextDueSimulationTick == dailyUpdateTicks
                   && onBoundary.ShouldAdvance
                   && onBoundary.DueUpdateCount == 1
                   && onBoundary.PreviousEnvironmentTicks == 0
                   && onBoundary.CurrentEnvironmentTicks == dailyUpdateTicks
                   && onBoundary.NextDueSimulationTick == dailyUpdateTicks * 2
                   && skippedDays.ShouldAdvance
                   && skippedDays.DueUpdateCount == 3
                   && skippedDays.CurrentEnvironmentTicks == dailyUpdateTicks * 3
                   && afterDecisionTick == dailyUpdateTicks
                   && normalized.ResolveMaxPlantMutationsPerUpdate() == 1
                   && normalized.ResolveMaxVegetationMutationsPerUpdate() == 1
                   && normalized.ResolveMaxPlantUpdatesPerDay() == 12
                   && normalized.ResolveMaxPlantBirthsPerDay() == 4
                   && normalized.ResolveMaxPlantBirthsPerAreaPerDay() == 2
                   && normalized.ResolveMaxPlantDeathsPerDay() == 0
                   && normalized.ResolveMaxVegetationCellsChangedPerDay() == 64
                   && normalized.ResolveMaxAreasProcessedPerDay() == 3
                   && normalized.ResolveUpdateMode() == BiosphereRuntimeParams.DefaultUpdateMode;
        }
    }
}
