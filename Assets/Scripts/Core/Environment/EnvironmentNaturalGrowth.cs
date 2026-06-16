using System.Collections.Generic;

namespace Arcontio.Core.Environment
{
    // =============================================================================
    // EnvironmentNaturalGrowthConfig
    // =============================================================================
    /// <summary>
    /// <para>
    /// Configurazione data-only del primo ciclo naturale della biosfera.
    /// </para>
    ///
    /// <para><b>Principio architetturale: coefficienti fuori dalle regole rigide</b></para>
    /// <para>
    /// I valori di crescita, germinazione e salute non devono restare intrappolati
    /// nel resolver. Questa classe e' gia' pronta per essere popolata da file di
    /// configurazione futuri, anche se la foundation la istanzia direttamente.
    /// </para>
    ///
    /// <para><b>Struttura interna:</b></para>
    /// <list type="bullet">
    ///   <item><b>allowNewPlantInstances</b>: gate per generare PlantInstance naturali.</item>
    ///   <item><b>maxNewPlantsPerDay</b>: limite globale giornaliero.</item>
    ///   <item><b>maxNewPlantsPerAreaPerDay</b>: limite giornaliero per area.</item>
    ///   <item><b>minimumGerminationScore01</b>: soglia minima di germinazione.</item>
    ///   <item><b>healthRecoveryStep01</b>: recupero salute in ambiente favorevole.</item>
    ///   <item><b>healthStressStep01</b>: perdita salute in ambiente sfavorevole.</item>
    ///   <item><b>removeDeadPlants</b>: gate per rimozione piante morte.</item>
    /// </list>
    /// </summary>
    public sealed class EnvironmentNaturalGrowthConfig
    {
        public bool allowNewPlantInstances = true;
        public int maxNewPlantsPerDay = 8;
        public int maxNewPlantsPerAreaPerDay = 1;
        public float minimumGerminationScore01 = 0.58f;
        public float healthRecoveryStep01 = 0.04f;
        public float healthStressStep01 = 0.06f;
        public bool removeDeadPlants = false;
    }

    // =============================================================================
    // EnvironmentNaturalGrowthReport
    // =============================================================================
    /// <summary>
    /// <para>
    /// Report compatto del ciclo naturale giornaliero.
    /// </para>
    ///
    /// <para><b>Principio architetturale: simulazione osservabile senza log globale</b></para>
    /// <para>
    /// Il loop naturale produce conteggi leggibili da harness, debug e test futuri.
    /// Non scrive console, non emette eventi e non assume la presenza di sistemi
    /// diagnostici globali.
    /// </para>
    ///
    /// <para><b>Struttura interna:</b></para>
    /// <list type="bullet">
    ///   <item><b>AreasVisited</b>: aree lette dallo snapshot.</item>
    ///   <item><b>SeedBankEntriesVisited</b>: entry seed bank valutate.</item>
    ///   <item><b>SeedBanksUpdated</b>: seed bank riscritte dopo pressione ecologica.</item>
    ///   <item><b>ExistingPlantsVisited</b>: piante esistenti valutate.</item>
    ///   <item><b>PlantInstancesUpdated</b>: piante copiate con eta'/salute/stadio aggiornati.</item>
    ///   <item><b>PlantInstancesCreated</b>: nuove piante naturali generate.</item>
    ///   <item><b>PlantInstancesRemoved</b>: piante morte rimosse se il gate e' attivo.</item>
    /// </list>
    /// </summary>
    public readonly struct EnvironmentNaturalGrowthReport
    {
        public readonly int AreasVisited;
        public readonly int SeedBankEntriesVisited;
        public readonly int SeedBanksUpdated;
        public readonly int ExistingPlantsVisited;
        public readonly int PlantInstancesUpdated;
        public readonly int PlantInstancesCreated;
        public readonly int PlantInstancesRemoved;

        public bool HasChanges =>
            SeedBanksUpdated > 0
            || PlantInstancesUpdated > 0
            || PlantInstancesCreated > 0
            || PlantInstancesRemoved > 0;

        // =============================================================================
        // EnvironmentNaturalGrowthReport
        // =============================================================================
        /// <summary>
        /// <para>
        /// Costruisce il report normalizzando eventuali conteggi negativi.
        /// </para>
        /// </summary>
        public EnvironmentNaturalGrowthReport(
            int areasVisited,
            int seedBankEntriesVisited,
            int seedBanksUpdated,
            int existingPlantsVisited,
            int plantInstancesUpdated,
            int plantInstancesCreated,
            int plantInstancesRemoved)
        {
            AreasVisited = areasVisited < 0 ? 0 : areasVisited;
            SeedBankEntriesVisited = seedBankEntriesVisited < 0 ? 0 : seedBankEntriesVisited;
            SeedBanksUpdated = seedBanksUpdated < 0 ? 0 : seedBanksUpdated;
            ExistingPlantsVisited = existingPlantsVisited < 0 ? 0 : existingPlantsVisited;
            PlantInstancesUpdated = plantInstancesUpdated < 0 ? 0 : plantInstancesUpdated;
            PlantInstancesCreated = plantInstancesCreated < 0 ? 0 : plantInstancesCreated;
            PlantInstancesRemoved = plantInstancesRemoved < 0 ? 0 : plantInstancesRemoved;
        }
    }

    // =============================================================================
    // EnvironmentNaturalGrowthResult
    // =============================================================================
    /// <summary>
    /// <para>
    /// Risultato del ciclo naturale data-only.
    /// </para>
    ///
    /// <para><b>Principio architetturale: stato evoluto come valore esplicito</b></para>
    /// <para>
    /// Il resolver restituisce un nuovo <see cref="EnvironmentState"/>. Il chiamante
    /// decide se adottarlo, confrontarlo, salvarlo o scartarlo. Nessuna mutazione
    /// globale avviene dentro il ciclo naturale.
    /// </para>
    ///
    /// <para><b>Struttura interna:</b></para>
    /// <list type="bullet">
    ///   <item><b>State</b>: stato ambientale dopo il ciclo.</item>
    ///   <item><b>Report</b>: conteggi diagnostici del ciclo.</item>
    /// </list>
    /// </summary>
    public sealed class EnvironmentNaturalGrowthResult
    {
        public EnvironmentState State { get; }
        public EnvironmentNaturalGrowthReport Report { get; }

        // =============================================================================
        // EnvironmentNaturalGrowthResult
        // =============================================================================
        /// <summary>
        /// <para>
        /// Costruisce il risultato aggregando stato e report.
        /// </para>
        /// </summary>
        public EnvironmentNaturalGrowthResult(
            EnvironmentState state,
            EnvironmentNaturalGrowthReport report)
        {
            State = state ?? new EnvironmentState();
            Report = report;
        }
    }

    // =============================================================================
    // EnvironmentNaturalGrowthResolver
    // =============================================================================
    /// <summary>
    /// <para>
    /// Resolver data-only del primo ciclo naturale della biosfera.
    /// </para>
    ///
    /// <para><b>Principio architetturale: loop naturale esplicito e cadenzato</b></para>
    /// <para>
    /// Il resolver collega area, seed bank e PlantInstance solo quando il chiamante
    /// consegna una transizione giornaliera. Non gira per frame, non accede a World,
    /// non crea oggetti Unity e non notifica NPC. Produce soltanto uno stato Core
    /// leggibile da snapshot futuri.
    /// </para>
    ///
    /// <para><b>Struttura interna:</b></para>
    /// <list type="bullet">
    ///   <item><b>Evolve</b>: applica evoluzione area, seed bank e piante.</item>
    ///   <item><b>CopyAndEvolveAreas</b>: conserva/evolve payload area-based.</item>
    ///   <item><b>CopyAndEvolvePlants</b>: aggiorna eta', salute e stadio piante esistenti.</item>
    ///   <item><b>TryCreatePlantFromArea</b>: genera al massimo poche piante naturali.</item>
    /// </list>
    /// </summary>
    public static class EnvironmentNaturalGrowthResolver
    {
        // =============================================================================
        // Evolve
        // =============================================================================
        /// <summary>
        /// <para>
        /// Evolve uno snapshot ambientale con un ciclo naturale giornaliero esplicito.
        /// </para>
        /// </summary>
        public static EnvironmentNaturalGrowthResult Evolve(
            EnvironmentSnapshot snapshot,
            EnvironmentPlantCatalog plantCatalog,
            EnvironmentTemporalTransition transition,
            EnvironmentGlobalClimateState climate,
            EnvironmentSeasonProfile seasonProfile,
            EnvironmentNaturalGrowthConfig config = null,
            EnvironmentBiomeProfile biomeProfile = default)
        {
            var safeConfig = config ?? new EnvironmentNaturalGrowthConfig();
            var safeBiome = biomeProfile.IsValid
                ? biomeProfile
                : EnvironmentBiomeProfile.Default;
            var nextState = new EnvironmentState();
            nextState.SetCalendar(transition.Current);
            nextState.SetClimate(climate);

            if (snapshot == null)
            {
                return new EnvironmentNaturalGrowthResult(
                    nextState,
                    new EnvironmentNaturalGrowthReport(0, 0, 0, 0, 0, 0, 0));
            }

            var areaStats = CopyAndEvolveAreas(
                snapshot,
                nextState,
                plantCatalog,
                transition,
                climate,
                seasonProfile,
                safeConfig,
                safeBiome);
            var plantStats = CopyAndEvolvePlants(
                snapshot,
                nextState,
                plantCatalog,
                climate,
                seasonProfile,
                transition,
                safeConfig,
                safeBiome);

            return new EnvironmentNaturalGrowthResult(
                nextState,
                new EnvironmentNaturalGrowthReport(
                    areaStats.AreasVisited,
                    areaStats.SeedBankEntriesVisited,
                    areaStats.SeedBanksUpdated,
                    plantStats.ExistingPlantsVisited,
                    plantStats.PlantInstancesUpdated,
                    areaStats.PlantInstancesCreated,
                    plantStats.PlantInstancesRemoved));
        }

        private readonly struct AreaGrowthStats
        {
            public readonly int AreasVisited;
            public readonly int SeedBankEntriesVisited;
            public readonly int SeedBanksUpdated;
            public readonly int PlantInstancesCreated;

            public AreaGrowthStats(
                int areasVisited,
                int seedBankEntriesVisited,
                int seedBanksUpdated,
                int plantInstancesCreated)
            {
                AreasVisited = areasVisited;
                SeedBankEntriesVisited = seedBankEntriesVisited;
                SeedBanksUpdated = seedBanksUpdated;
                PlantInstancesCreated = plantInstancesCreated;
            }
        }

        private readonly struct PlantGrowthStats
        {
            public readonly int ExistingPlantsVisited;
            public readonly int PlantInstancesUpdated;
            public readonly int PlantInstancesRemoved;

            public PlantGrowthStats(
                int existingPlantsVisited,
                int plantInstancesUpdated,
                int plantInstancesRemoved)
            {
                ExistingPlantsVisited = existingPlantsVisited;
                PlantInstancesUpdated = plantInstancesUpdated;
                PlantInstancesRemoved = plantInstancesRemoved;
            }
        }

        private static AreaGrowthStats CopyAndEvolveAreas(
            EnvironmentSnapshot snapshot,
            EnvironmentState nextState,
            EnvironmentPlantCatalog plantCatalog,
            EnvironmentTemporalTransition transition,
            EnvironmentGlobalClimateState climate,
            EnvironmentSeasonProfile seasonProfile,
            EnvironmentNaturalGrowthConfig config,
            EnvironmentBiomeProfile biomeProfile)
        {
            int seedEntriesVisited = 0;
            int seedBanksUpdated = 0;
            int plantsCreated = 0;
            var areas = snapshot.Areas ?? new EnvironmentAreaSnapshot[0];
            var context = new EnvironmentAreaEvolutionContext(
                transition.Current,
                climate,
                seasonProfile,
                transition,
                biomeProfile);

            for (int i = 0; i < areas.Count; i++)
            {
                var area = areas[i];
                nextState.SetAreaDefinition(area.Definition);

                var evolved = EvolveAreaPayloads(area, context);
                if (area.HasFertility)
                    nextState.SetFertilityArea(evolved.Fertility);

                if (area.HasWater)
                    nextState.SetWaterArea(evolved.Water);

                if (area.HasVegetation)
                    nextState.SetVegetationArea(evolved.Vegetation);

                if (area.HasSeedBank)
                {
                    var nextSeedBank = EvolveSeedBank(
                        area.SeedBankState,
                        area.HasVegetation ? evolved.Vegetation : area.VegetationState,
                        climate,
                        seasonProfile,
                        transition,
                        biomeProfile,
                        out int visited);
                    seedEntriesVisited += visited;
                    seedBanksUpdated++;
                    nextState.SetSeedBankArea(nextSeedBank);

                    int plantCountForArea = CountPlantsForArea(snapshot, area.Definition.AreaId);
                    int effectivePlantCapacity = ResolveEffectivePlantCapacity(evolved, biomeProfile);
                    int desiredPlantCount = ResolveDesiredPlantCount(
                        evolved,
                        nextSeedBank,
                        climate,
                        seasonProfile,
                        biomeProfile,
                        effectivePlantCapacity);
                    if (config.allowNewPlantInstances
                        && transition.DayChanged
                        && plantsCreated < SafeMax(config.maxNewPlantsPerDay)
                        && plantCountForArea < desiredPlantCount
                        && TryCreatePlantFromArea(
                            area,
                            nextSeedBank,
                            plantCatalog,
                            evolved,
                            climate,
                            seasonProfile,
                            config,
                            plantsCreated,
                            transition,
                            plantCountForArea,
                            desiredPlantCount,
                            effectivePlantCapacity,
                            out EnvironmentPlantInstance plant))
                    {
                        nextState.SetPlantInstance(plant);
                        plantsCreated++;
                    }
                }
            }

            return new AreaGrowthStats(
                areas.Count,
                seedEntriesVisited,
                seedBanksUpdated,
                plantsCreated);
        }

        private static EnvironmentAreaEvolutionResult EvolveAreaPayloads(
            EnvironmentAreaSnapshot area,
            EnvironmentAreaEvolutionContext context)
        {
            var fertility = area.HasFertility
                ? area.FertilityState
                : CreateNeutralFertility(area.Definition.AreaId);
            var water = area.HasWater
                ? area.WaterState
                : CreateNeutralWater(area.Definition.AreaId);
            var vegetation = area.HasVegetation
                ? area.VegetationState
                : CreateNeutralVegetation(area.Definition.AreaId);

            return EnvironmentAreaEvolutionResolver.Evolve(
                fertility,
                water,
                vegetation,
                context);
        }

        private static EnvironmentSeedBankAreaState EvolveSeedBank(
            EnvironmentSeedBankAreaState seedBank,
            EnvironmentVegetationAreaState vegetation,
            EnvironmentGlobalClimateState climate,
            EnvironmentSeasonProfile seasonProfile,
            EnvironmentTemporalTransition transition,
            EnvironmentBiomeProfile biomeProfile,
            out int entriesVisited)
        {
            var entries = seedBank?.Entries ?? new EnvironmentSeedBankEntry[0];
            var nextEntries = new List<EnvironmentSeedBankEntry>(entries.Count);
            entriesVisited = entries.Count;

            for (int i = 0; i < entries.Count; i++)
            {
                var entry = entries[i];
                float biomeMoisture = (climate.Humidity01 * 0.45f)
                                      + (climate.Weather.Precipitation01 * 0.25f)
                                      + (biomeProfile.BaseMoisture01 * 0.30f);
                float droughtStress = climate.Aridity01 * (1f - biomeProfile.DroughtResistance01);
                float seasonSupport = (1f - biomeProfile.Seasonality01)
                                      + (seasonProfile.VegetationGrowthBias01 * biomeProfile.Seasonality01);
                float ecologicalSupport = EnvironmentMath.Clamp01(
                    (vegetation.Density01 * 0.25f)
                    + (vegetation.Health01 * 0.30f)
                    + (biomeMoisture * 0.25f)
                    + (seasonSupport * 0.20f)
                    - (droughtStress * biomeProfile.DisturbanceSensitivity01 * 0.35f));
                float targetAmount = EnvironmentMath.Clamp01(
                    biomeProfile.TargetSeedBankAmount01
                    * (0.45f + (ecologicalSupport * 0.55f)));
                float targetViability = EnvironmentMath.Clamp01(
                    biomeProfile.TargetSeedBankViability01
                    * (0.35f + (vegetation.Health01 * 0.65f))
                    * (1f - (droughtStress * 0.45f)));
                float recoveryRate = transition.DayChanged
                    ? 0.010f + (biomeProfile.NaturalRecoveryRate01 * 0.035f)
                    : 0f;
                float nextAmount = Approach01(entry.Amount01, targetAmount, recoveryRate);
                float nextViability = Approach01(entry.Viability01, targetViability, recoveryRate);

                // La seed bank cresce o decade come pressione astratta. Non crea semi
                // fisici e non consuma inventari NPC.
                nextEntries.Add(new EnvironmentSeedBankEntry(
                    entry.SpeciesKey,
                    nextAmount,
                    nextViability));
            }

            return new EnvironmentSeedBankAreaState(
                seedBank == null ? EnvironmentAreaId.None : seedBank.AreaId,
                nextEntries);
        }

        private static PlantGrowthStats CopyAndEvolvePlants(
            EnvironmentSnapshot snapshot,
            EnvironmentState nextState,
            EnvironmentPlantCatalog plantCatalog,
            EnvironmentGlobalClimateState climate,
            EnvironmentSeasonProfile seasonProfile,
            EnvironmentTemporalTransition transition,
            EnvironmentNaturalGrowthConfig config,
            EnvironmentBiomeProfile biomeProfile)
        {
            int visited = 0;
            int updated = 0;
            int removed = 0;
            var plants = snapshot.Plants ?? new EnvironmentPlantSnapshot[0];

            for (int i = 0; i < plants.Count; i++)
            {
                visited++;
                var current = plants[i];
                var next = EvolvePlant(
                    current,
                    plantCatalog,
                    climate,
                    seasonProfile,
                    transition,
                    config,
                    biomeProfile,
                    CountPlantsForArea(snapshot, current.SourceAreaId),
                    ResolveDesiredPlantCountForArea(
                        snapshot,
                        current.SourceAreaId,
                        climate,
                        seasonProfile,
                        biomeProfile));

                if (!next.IsAlive)
                {
                    removed++;
                    continue;
                }

                if (nextState.SetPlantInstance(next))
                    updated++;
            }

            return new PlantGrowthStats(visited, updated, removed);
        }

        private static EnvironmentPlantInstance EvolvePlant(
            EnvironmentPlantSnapshot current,
            EnvironmentPlantCatalog plantCatalog,
            EnvironmentGlobalClimateState climate,
            EnvironmentSeasonProfile seasonProfile,
            EnvironmentTemporalTransition transition,
            EnvironmentNaturalGrowthConfig config,
            EnvironmentBiomeProfile biomeProfile,
            int areaPlantCount,
            int desiredPlantCount)
        {
            int nextAge = current.AgeDays + (transition.DayChanged && current.IsAlive ? 1 : 0);
            float healthDelta = 0f;
            EnvironmentPlantSpeciesDefinition species = null;
            bool hasSpecies = plantCatalog != null
                              && plantCatalog.TryGetSpecies(
                                  current.SpeciesKey,
                                  out species);

            if (transition.DayChanged && current.IsAlive)
            {
                bool favorableSeason = hasSpecies
                                       && species.IsSeasonFavorable(climate.Season);
                healthDelta = favorableSeason
                    ? EnvironmentMath.Clamp01(config.healthRecoveryStep01)
                    : -EnvironmentMath.Clamp01(config.healthStressStep01)
                      * ResolveUnfavorableSeasonStressMultiplier(species);
                healthDelta -= climate.Aridity01
                               * (1f - biomeProfile.DroughtResistance01)
                               * 0.014f;

                float occupancy01 = biomeProfile.MaxPlantInstancesPerArea <= 0
                    ? 0f
                    : areaPlantCount / (float)biomeProfile.MaxPlantInstancesPerArea;
                if (occupancy01 > 0.55f)
                    healthDelta -= (occupancy01 - 0.55f) * 0.080f;

                float desiredOccupancy01 = desiredPlantCount <= 0
                    ? 1f
                    : areaPlantCount / (float)desiredPlantCount;
                if (desiredOccupancy01 > 1f)
                    healthDelta -= (desiredOccupancy01 - 1f) * 0.045f;

                float ageStress = ResolveAgeStress01(nextAge, current.PlantId.Value);
                healthDelta -= ageStress * 0.008f;
            }

            float nextHealth = current.Health01 + healthDelta;
            if (transition.DayChanged
                && ShouldPlantDieThisDay(
                    current,
                    nextHealth,
                    climate,
                    seasonProfile,
                    transition,
                    areaPlantCount,
                    desiredPlantCount))
            {
                nextHealth = 0f;
            }

            if (hasSpecies)
            {
                return EnvironmentPlantInstance.CreateFromSpecies(
                    current.PlantId,
                    species,
                    current.Cell,
                    nextAge,
                    nextHealth,
                    current.SourceAreaId);
            }

            return new EnvironmentPlantInstance(
                current.PlantId,
                current.SpeciesKey,
                current.Cell,
                nextAge,
                current.GrowthStage,
                current.GrowthStageKey,
                current.HealthState,
                nextHealth,
                current.Maturity01,
                current.IsHarvestable,
                current.SourceAreaId);
        }

        private static bool TryCreatePlantFromArea(
            EnvironmentAreaSnapshot area,
            EnvironmentSeedBankAreaState seedBank,
            EnvironmentPlantCatalog plantCatalog,
            EnvironmentAreaEvolutionResult evolved,
            EnvironmentGlobalClimateState climate,
            EnvironmentSeasonProfile seasonProfile,
            EnvironmentNaturalGrowthConfig config,
            int alreadyCreatedToday,
            EnvironmentTemporalTransition transition,
            int plantCountForArea,
            int desiredPlantCount,
            int effectivePlantCapacity,
            out EnvironmentPlantInstance plant)
        {
            plant = default;

            if (plantCatalog == null
                || seedBank == null
                || seedBank.Entries.Count == 0
                || SafeMax(config.maxNewPlantsPerAreaPerDay) <= 0)
            {
                return false;
            }

            for (int i = 0; i < seedBank.Entries.Count; i++)
            {
                var entry = seedBank.Entries[i];
                if (!plantCatalog.TryGetSpecies(
                    entry.SpeciesKey,
                    out EnvironmentPlantSpeciesDefinition species))
                {
                    continue;
                }

                float score = ComputeGerminationScore(
                    entry,
                    species,
                    evolved,
                    climate,
                    seasonProfile);
                if (score < EnvironmentMath.Clamp01(config.minimumGerminationScore01))
                    continue;

                if (!ShouldRecruitPlant(
                    score,
                    entry,
                    transition,
                    plantCountForArea,
                    desiredPlantCount,
                    effectivePlantCapacity,
                    alreadyCreatedToday))
                {
                    continue;
                }

                int daySalt = ResolvePlantDaySalt(transition);
                var cell = ChooseDeterministicPlantCell(
                    area.Definition.Bounds,
                    alreadyCreatedToday + daySalt);
                var plantId = CreateDeterministicPlantId(
                    area.Definition.AreaId,
                    alreadyCreatedToday + daySalt,
                    entry.SpeciesKey);
                plant = EnvironmentPlantInstance.CreateFromSpecies(
                    plantId,
                    species,
                    cell,
                    0,
                    score,
                    area.Definition.AreaId);
                return plant.PlantId.IsValid;
            }

            return false;
        }

        private static bool ShouldRecruitPlant(
            float score,
            EnvironmentSeedBankEntry entry,
            EnvironmentTemporalTransition transition,
            int plantCountForArea,
            int desiredPlantCount,
            int effectivePlantCapacity,
            int alreadyCreatedToday)
        {
            if (effectivePlantCapacity <= 0
                || desiredPlantCount <= 0
                || plantCountForArea >= effectivePlantCapacity
                || plantCountForArea >= desiredPlantCount)
            {
                return false;
            }

            float occupancy01 = plantCountForArea / (float)effectivePlantCapacity;
            float deficit01 = EnvironmentMath.Clamp01(
                (desiredPlantCount - plantCountForArea) / (float)desiredPlantCount);
            float recruitmentPressure = EnvironmentMath.Clamp01(
                (score * 0.55f)
                + (entry.Amount01 * 0.25f)
                + (entry.Viability01 * 0.20f)
                + (deficit01 * 0.36f)
                - (occupancy01 * 0.44f));
            float roll = Hash01(
                transition.Current.Date.Year,
                transition.Current.Date.DayOfYear,
                700 + alreadyCreatedToday + plantCountForArea);

            // Il recruitment e' deterministico ma probabilistico: non crea una pianta
            // ogni giorno buono fino al cap, permettendo curve piu' naturali.
            return roll < recruitmentPressure * 0.32f;
        }

        private static float ComputeGerminationScore(
            EnvironmentSeedBankEntry entry,
            EnvironmentPlantSpeciesDefinition species,
            EnvironmentAreaEvolutionResult evolved,
            EnvironmentGlobalClimateState climate,
            EnvironmentSeasonProfile seasonProfile)
        {
            float speciesSeason = species.IsSeasonFavorable(climate.Season) ? 1f : 0.35f;
            float fertility = evolved.Fertility.CurrentFertility01 >= species.MinimumFertility01
                ? evolved.Fertility.CurrentFertility01
                : evolved.Fertility.CurrentFertility01 * 0.35f;
            float climateFit = 1f - System.Math.Abs(climate.Temperature01 - species.IdealTemperature01);
            float humidityFit = 1f - System.Math.Abs(climate.Humidity01 - species.IdealHumidity01);

            return EnvironmentMath.Clamp01(
                (entry.Amount01 * 0.20f)
                + (entry.Viability01 * 0.20f)
                + (fertility * 0.18f)
                + (evolved.Vegetation.Health01 * 0.12f)
                + (climateFit * 0.10f)
                + (humidityFit * 0.10f)
                + (speciesSeason * 0.07f)
                + (seasonProfile.VegetationGrowthBias01 * 0.03f));
        }

        private static EnvironmentCellCoord ChooseDeterministicPlantCell(
            EnvironmentAreaBounds bounds,
            int salt)
        {
            int width = bounds.Width <= 0 ? 1 : bounds.Width;
            int height = bounds.Height <= 0 ? 1 : bounds.Height;
            int offsetX = salt % width;
            int offsetY = (salt / width) % height;

            // La posizione e' deterministica e locale ai bounds. Un generatore futuro
            // potra' sostituirla con maschere/chunk senza cambiare il contratto.
            return new EnvironmentCellCoord(
                bounds.MinX + offsetX,
                bounds.MinY + offsetY,
                bounds.Z);
        }

        private static EnvironmentPlantId CreateDeterministicPlantId(
            EnvironmentAreaId areaId,
            int salt,
            string speciesKey)
        {
            int hash = ComputeStableSpeciesHash(speciesKey);
            int value = 100000
                        + (areaId.Value * 100)
                        + (salt * 17)
                        + System.Math.Abs(hash % 97);

            return new EnvironmentPlantId(value);
        }

        private static int CountPlantsForArea(
            EnvironmentSnapshot snapshot,
            EnvironmentAreaId areaId)
        {
            var plants = snapshot?.Plants ?? new EnvironmentPlantSnapshot[0];
            int count = 0;
            for (int i = 0; i < plants.Count; i++)
            {
                if (plants[i].SourceAreaId.Equals(areaId))
                    count++;
            }

            return count;
        }

        private static int ResolveEffectivePlantCapacity(
            EnvironmentAreaEvolutionResult evolved,
            EnvironmentBiomeProfile biomeProfile)
        {
            int max = biomeProfile.MaxPlantInstancesPerArea;
            if (max <= 0)
                return 0;

            float habitat01 = EnvironmentMath.Clamp01(
                (evolved.Vegetation.Density01 * 0.65f)
                + (evolved.Vegetation.Health01 * 0.25f)
                + (evolved.Fertility.CurrentFertility01 * 0.10f));
            float capacity01 = 0.08f + (habitat01 * 0.72f);
            return System.Math.Max(1, (int)System.Math.Round(max * capacity01));
        }

        private static int ResolveDesiredPlantCount(
            EnvironmentAreaEvolutionResult evolved,
            EnvironmentSeedBankAreaState seedBank,
            EnvironmentGlobalClimateState climate,
            EnvironmentSeasonProfile seasonProfile,
            EnvironmentBiomeProfile biomeProfile,
            int effectivePlantCapacity)
        {
            if (effectivePlantCapacity <= 0)
                return 0;

            float seedSupport = ResolveSeedSupport01(seedBank);
            float seasonSupport = (1f - biomeProfile.Seasonality01)
                                  + (seasonProfile.VegetationGrowthBias01 * biomeProfile.Seasonality01);
            float climateStress = climate.Aridity01 * (1f - biomeProfile.DroughtResistance01);
            float habitat01 = EnvironmentMath.Clamp01(
                (evolved.Vegetation.Density01 * 0.42f)
                + (evolved.Vegetation.Health01 * 0.25f)
                + (evolved.Fertility.CurrentFertility01 * 0.12f)
                + (seedSupport * 0.14f)
                + (seasonSupport * 0.07f)
                - (climateStress * 0.25f));
            float desired01 = EnvironmentMath.Clamp01(
                0.08f + (habitat01 * biomeProfile.TargetVegetationDensity01 * 0.90f));

            // Il numero desiderato e' volutamente piu' basso del limite massimo:
            // PlantInstance rappresenta piante importanti/leggibili, non tutta la
            // biomassa dell'area. La vegetazione diffusa resta nel payload area.
            return System.Math.Max(1, (int)System.Math.Round(effectivePlantCapacity * desired01));
        }

        private static int ResolveDesiredPlantCountForArea(
            EnvironmentSnapshot snapshot,
            EnvironmentAreaId areaId,
            EnvironmentGlobalClimateState climate,
            EnvironmentSeasonProfile seasonProfile,
            EnvironmentBiomeProfile biomeProfile)
        {
            var areas = snapshot?.Areas ?? new EnvironmentAreaSnapshot[0];
            for (int i = 0; i < areas.Count; i++)
            {
                var area = areas[i];
                if (!area.Definition.AreaId.Equals(areaId))
                    continue;

                var evolved = new EnvironmentAreaEvolutionResult(
                    area.HasFertility ? area.FertilityState : CreateNeutralFertility(areaId),
                    area.HasWater ? area.WaterState : CreateNeutralWater(areaId),
                    area.HasVegetation ? area.VegetationState : CreateNeutralVegetation(areaId),
                    new EnvironmentAreaEvolutionDelta(0f, 0f, 0f, 0f));
                int capacity = ResolveEffectivePlantCapacity(evolved, biomeProfile);
                var seedBank = area.HasSeedBank
                    ? area.SeedBankState
                    : new EnvironmentSeedBankAreaState(areaId, new EnvironmentSeedBankEntry[0]);
                return ResolveDesiredPlantCount(
                    evolved,
                    seedBank,
                    climate,
                    seasonProfile,
                    biomeProfile,
                    capacity);
            }

            return 0;
        }

        private static float ResolveSeedSupport01(EnvironmentSeedBankAreaState seedBank)
        {
            var entries = seedBank?.Entries ?? new EnvironmentSeedBankEntry[0];
            if (entries.Count == 0)
                return 0f;

            float total = 0f;
            for (int i = 0; i < entries.Count; i++)
            {
                var entry = entries[i];
                total += (entry.Amount01 * 0.55f) + (entry.Viability01 * 0.45f);
            }

            return EnvironmentMath.Clamp01(total / entries.Count);
        }

        private static float ResolveAgeStress01(int ageDays, int plantIdValue)
        {
            // La foundation non possiede ancora lifespan per specie. Applicare una
            // vecchiaia generica a erba, arbusti e alberi produrrebbe collassi falsi
            // e non parametrizzabili. Il punto di estensione resta qui, ma per ora
            // la mortalita' deve derivare da salute, stress climatico e competizione.
            return 0f;
        }

        private static float ResolveUnfavorableSeasonStressMultiplier(
            EnvironmentPlantSpeciesDefinition species)
        {
            if (species == null)
                return 1f;

            switch (species.SeasonalBehavior)
            {
                case EnvironmentPlantSeasonalBehavior.Perennial:
                    return 0.28f;

                case EnvironmentPlantSeasonalBehavior.Deciduous:
                    return 0.18f;

                case EnvironmentPlantSeasonalBehavior.Evergreen:
                    return 0.12f;

                default:
                    return 1f;
            }
        }

        private static bool ShouldPlantDieThisDay(
            EnvironmentPlantSnapshot current,
            float nextHealth,
            EnvironmentGlobalClimateState climate,
            EnvironmentSeasonProfile seasonProfile,
            EnvironmentTemporalTransition transition,
            int areaPlantCount,
            int desiredPlantCount)
        {
            if (!current.IsAlive || nextHealth > 0.14f)
                return false;

            float desiredPressure = desiredPlantCount <= 0
                ? 0.35f
                : EnvironmentMath.Clamp01((areaPlantCount - desiredPlantCount) / (float)desiredPlantCount);
            float stress01 = EnvironmentMath.Clamp01(
                (1f - EnvironmentMath.Clamp01(nextHealth))
                + (climate.Aridity01 * 0.22f)
                + ((1f - seasonProfile.VegetationGrowthBias01) * 0.14f)
                + (desiredPressure * 0.24f));
            float mortalityChance = 0.006f + (stress01 * 0.045f);
            float roll = Hash01(
                transition.Current.Date.Year,
                transition.Current.Date.DayOfYear,
                1700 + current.PlantId.Value);

            // La mortalita' resta deterministica, ma distribuita per id pianta e
            // giorno: evita collassi sincronizzati dell'intera popolazione.
            return roll < mortalityChance;
        }

        private static int ResolvePlantDaySalt(EnvironmentTemporalTransition transition)
        {
            // Il sale include anno e giorno ambientale. In questo modo una nuova
            // germinazione futura non sovrascrive sempre la stessa PlantInstance.
            return (transition.Current.Date.Year * 400)
                   + transition.Current.Date.DayOfYear;
        }

        private static float Approach01(float current, float target, float rate)
        {
            float safeCurrent = EnvironmentMath.Clamp01(current);
            float safeTarget = EnvironmentMath.Clamp01(target);
            float safeRate = EnvironmentMath.Clamp01(rate);
            return EnvironmentMath.Clamp01(
                safeCurrent + ((safeTarget - safeCurrent) * safeRate));
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

        private static int ComputeStableSpeciesHash(string speciesKey)
        {
            if (string.IsNullOrWhiteSpace(speciesKey))
                return 17;

            int hash = 23;
            for (int i = 0; i < speciesKey.Length; i++)
            {
                // Hash volutamente semplice e stabile tra runtime: serve solo a
                // distribuire id preparatori, non a garantire sicurezza crittografica.
                hash = (hash * 31) + speciesKey[i];
            }

            return hash;
        }

        private static int SafeMax(int value)
        {
            return value < 0 ? 0 : value;
        }

        private static EnvironmentFertilityAreaState CreateNeutralFertility(
            EnvironmentAreaId areaId)
        {
            return new EnvironmentFertilityAreaState(
                areaId,
                EnvironmentSoilKind.Generic,
                0.5f,
                0.5f,
                0.5f,
                0f,
                0.5f);
        }

        private static EnvironmentWaterAreaState CreateNeutralWater(
            EnvironmentAreaId areaId)
        {
            return new EnvironmentWaterAreaState(
                areaId,
                EnvironmentWaterKind.Still,
                EnvironmentWaterDepthLevel.Shallow,
                0.5f,
                0f,
                true,
                false);
        }

        private static EnvironmentVegetationAreaState CreateNeutralVegetation(
            EnvironmentAreaId areaId)
        {
            return new EnvironmentVegetationAreaState(
                areaId,
                EnvironmentVegetationKind.None,
                0f,
                0.5f,
                0.5f,
                0.5f,
                0.5f);
        }
    }
}
