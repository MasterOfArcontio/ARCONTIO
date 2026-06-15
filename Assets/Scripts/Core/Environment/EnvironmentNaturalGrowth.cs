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
            EnvironmentNaturalGrowthConfig config = null)
        {
            var safeConfig = config ?? new EnvironmentNaturalGrowthConfig();
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
                safeConfig);
            var plantStats = CopyAndEvolvePlants(
                snapshot,
                nextState,
                plantCatalog,
                climate,
                transition,
                safeConfig);

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
            EnvironmentNaturalGrowthConfig config)
        {
            int seedEntriesVisited = 0;
            int seedBanksUpdated = 0;
            int plantsCreated = 0;
            var areas = snapshot.Areas ?? new EnvironmentAreaSnapshot[0];
            var context = new EnvironmentAreaEvolutionContext(
                transition.Current,
                climate,
                seasonProfile,
                transition);

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
                        out int visited);
                    seedEntriesVisited += visited;
                    seedBanksUpdated++;
                    nextState.SetSeedBankArea(nextSeedBank);

                    if (config.allowNewPlantInstances
                        && transition.DayChanged
                        && plantsCreated < SafeMax(config.maxNewPlantsPerDay)
                        && TryCreatePlantFromArea(
                            area,
                            nextSeedBank,
                            plantCatalog,
                            evolved,
                            climate,
                            seasonProfile,
                            config,
                            plantsCreated,
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
            out int entriesVisited)
        {
            var entries = seedBank?.Entries ?? new EnvironmentSeedBankEntry[0];
            var nextEntries = new List<EnvironmentSeedBankEntry>(entries.Count);
            entriesVisited = entries.Count;

            for (int i = 0; i < entries.Count; i++)
            {
                var entry = entries[i];
                float climateSupport = (climate.Humidity01
                                        + seasonProfile.VegetationGrowthBias01
                                        + vegetation.Health01) / 3f;
                float dailyPressure = transition.DayChanged
                    ? (climateSupport * 0.030f) - (climate.Aridity01 * 0.020f)
                    : 0f;
                float viabilityPressure = transition.DayChanged
                    ? (vegetation.Health01 * 0.020f) - (climate.Aridity01 * 0.015f)
                    : 0f;

                // La seed bank cresce o decade come pressione astratta. Non crea semi
                // fisici e non consuma inventari NPC.
                nextEntries.Add(new EnvironmentSeedBankEntry(
                    entry.SpeciesKey,
                    entry.Amount01 + dailyPressure,
                    entry.Viability01 + viabilityPressure));
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
            EnvironmentTemporalTransition transition,
            EnvironmentNaturalGrowthConfig config)
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
                    transition,
                    config);

                if (config.removeDeadPlants && !next.IsAlive)
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
            EnvironmentTemporalTransition transition,
            EnvironmentNaturalGrowthConfig config)
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
                    : -EnvironmentMath.Clamp01(config.healthStressStep01);
                healthDelta -= climate.Aridity01 * 0.025f;
            }

            float nextHealth = current.Health01 + healthDelta;

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

                var cell = ChooseDeterministicPlantCell(area.Definition.Bounds, alreadyCreatedToday);
                var plantId = CreateDeterministicPlantId(
                    area.Definition.AreaId,
                    alreadyCreatedToday,
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
