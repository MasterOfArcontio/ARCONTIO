using System;
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
    ///   <item><b>maxExistingPlantUpdatesPerDay</b>: limite opzionale piante esistenti evolute.</item>
    ///   <item><b>maxDeadPlantsRemovedPerDay</b>: limite opzionale rimozioni piante morte.</item>
    ///   <item><b>maxAreasProcessedPerDay</b>: limite opzionale aree biologiche evolute.</item>
    ///   <item><b>minimumGerminationScore01</b>: soglia minima di germinazione.</item>
    ///   <item><b>healthRecoveryStep01</b>: recupero salute in ambiente favorevole.</item>
    ///   <item><b>healthStressStep01</b>: perdita salute in ambiente sfavorevole.</item>
    ///   <item><b>removeDeadPlants</b>: gate per rimozione piante morte.</item>
    /// </list>
    /// </summary>
    [Serializable]
    public sealed class EnvironmentNaturalGrowthConfig
    {
        public bool allowNewPlantInstances = true;
        public int maxNewPlantsPerDay = 8;
        public int maxNewPlantsPerAreaPerDay = 3;
        public int maxExistingPlantUpdatesPerDay = 0;
        public int maxDeadPlantsRemovedPerDay = 0;
        public int maxAreasProcessedPerDay = 0;
        public float minimumGerminationScore01 = 0.42f;
        public float healthRecoveryStep01 = 0.055f;
        public float healthStressStep01 = 0.025f;
        public bool removeDeadPlants = false;
        public float plantAridityHealthStressScale01 = 0.014f;

        public float plantVitalityMin01 = 0.70f;
        public float plantVitalityMax01 = 1.35f;
        public float initialPlantHealthVitalityScale01 = 0.22f;
        public float unfavorableSeasonFallbackStressMultiplier01 = 0.55f;
        public float perennialDormancyStressMultiplier01 = 0.28f;
        public float deciduousDormancyStressMultiplier01 = 0.18f;
        public float evergreenDormancyStressMultiplier01 = 0.12f;
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
        private static readonly EnvironmentPhysicalPlantDelta[] EmptyPhysicalPlantDeltas =
            new EnvironmentPhysicalPlantDelta[0];

        public EnvironmentState State { get; }
        public EnvironmentNaturalGrowthReport Report { get; }
        public IReadOnlyList<EnvironmentPhysicalPlantDelta> PhysicalPlantDeltas { get; }

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
            EnvironmentNaturalGrowthReport report,
            IReadOnlyList<EnvironmentPhysicalPlantDelta> physicalPlantDeltas = null)
        {
            State = state ?? new EnvironmentState();
            Report = report;
            PhysicalPlantDeltas = physicalPlantDeltas ?? EmptyPhysicalPlantDeltas;
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
            var nextSnapshot = nextState.CreateSnapshot();
            var physicalPlantDeltas = EnvironmentPhysicalPlantDeltaProducer.DiffSnapshots(
                snapshot,
                nextSnapshot,
                SafeMax(safeConfig.maxNewPlantsPerDay) + plantStats.ExistingPlantsVisited + plantStats.PlantInstancesRemoved);

            return new EnvironmentNaturalGrowthResult(
                nextState,
                new EnvironmentNaturalGrowthReport(
                    areaStats.AreasVisited,
                    areaStats.SeedBankEntriesVisited,
                    areaStats.SeedBanksUpdated,
                    plantStats.ExistingPlantsVisited,
                    plantStats.PlantInstancesUpdated,
                    areaStats.PlantInstancesCreated,
                    plantStats.PlantInstancesRemoved),
                physicalPlantDeltas);
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

        // =============================================================================
        // PlantVitalityProfile
        // =============================================================================
        /// <summary>
        /// <para>
        /// Profilo individuale stabile di una pianta concreta.
        /// </para>
        ///
        /// <para><b>Principio architetturale: variabilita' biologica senza stato extra</b></para>
        /// <para>
        /// Il Core conserva verso World un contratto leggero: specie, cella, eta' e
        /// salute normalizzata. La differenza tra individui viene risolta con un
        /// hash deterministico su identita' e posizione, cosi' non aggiungiamo un
        /// nuovo campo persistente ma otteniamo comunque piante piu' o meno robuste.
        /// </para>
        ///
        /// <para><b>Struttura interna:</b></para>
        /// <list type="bullet">
        ///   <item><b>Vitality01</b>: moltiplicatore biologico unico dell'individuo.</item>
        ///   <item><b>HealthCapacity01</b>: capacita' salute relativa; cresce con Vitality01.</item>
        ///   <item><b>RecoveryMultiplier01</b>: rigenerazione proporzionale a Vitality01.</item>
        ///   <item><b>StressMultiplier01</b>: perdita salute inversamente proporzionale a Vitality01.</item>
        /// </list>
        /// </summary>
        private readonly struct PlantVitalityProfile
        {
            public readonly float Vitality01;
            public readonly float HealthCapacity01;
            public readonly float RecoveryMultiplier01;
            public readonly float StressMultiplier01;

            public PlantVitalityProfile(float vitality01)
            {
                Vitality01 = vitality01 <= 0.01f ? 0.01f : vitality01;
                HealthCapacity01 = Vitality01;
                RecoveryMultiplier01 = Vitality01;
                StressMultiplier01 = 1f / Vitality01;
            }
        }

        private readonly struct PlantPopulationKey : IEquatable<PlantPopulationKey>
        {
            public readonly EnvironmentAreaId AreaId;
            public readonly string SpeciesKey;

            public PlantPopulationKey(EnvironmentAreaId areaId, string speciesKey)
            {
                AreaId = areaId;
                SpeciesKey = string.IsNullOrWhiteSpace(speciesKey)
                    ? string.Empty
                    : speciesKey;
            }

            public bool Equals(PlantPopulationKey other)
            {
                return AreaId.Equals(other.AreaId)
                       && string.Equals(
                           SpeciesKey,
                           other.SpeciesKey,
                           StringComparison.OrdinalIgnoreCase);
            }

            public override bool Equals(object obj)
            {
                return obj is PlantPopulationKey other && Equals(other);
            }

            public override int GetHashCode()
            {
                unchecked
                {
                    return (AreaId.GetHashCode() * 397)
                           ^ StringComparer.OrdinalIgnoreCase.GetHashCode(SpeciesKey);
                }
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
            int areasProcessed = 0;
            int maxAreasProcessed = NormalizeOptionalBudget(config.maxAreasProcessedPerDay);
            var areas = snapshot.Areas ?? new EnvironmentAreaSnapshot[0];
            var livePlantCounts = BuildLivePlantCounts(snapshot.Plants);
            var createdPlantCounts = new Dictionary<PlantPopulationKey, int>();
            var occupiedPlantCells = BuildOccupiedPlantCells(snapshot.Plants);
            var context = new EnvironmentAreaEvolutionContext(
                transition.Current,
                climate,
                seasonProfile,
                transition);

            for (int i = 0; i < areas.Count; i++)
            {
                var area = areas[i];
                nextState.SetAreaDefinition(area.Definition);

                if (maxAreasProcessed > 0 && areasProcessed >= maxAreasProcessed)
                {
                    CopyAreaPayloadsUnchanged(nextState, area);
                    continue;
                }

                areasProcessed++;
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

                    int createdInArea = 0;
                    int maxAreaCreates = SafeMax(config.maxNewPlantsPerAreaPerDay);
                    while (config.allowNewPlantInstances
                           && transition.DayChanged
                           && createdInArea < maxAreaCreates
                           && plantsCreated < SafeMax(config.maxNewPlantsPerDay)
                           && TryCreatePlantFromArea(
                               area,
                               nextSeedBank,
                               plantCatalog,
                               evolved,
                               climate,
                               seasonProfile,
                               config,
                               livePlantCounts,
                               createdPlantCounts,
                               occupiedPlantCells,
                               transition.Current.Date.Year,
                               transition.Current.Date.DayOfYear,
                               plantsCreated,
                               createdInArea,
                               out EnvironmentPlantInstance plant,
                               out PlantPopulationKey populationKey))
                    {
                        nextState.SetPlantInstance(plant);
                        IncreasePlantCount(createdPlantCounts, populationKey);
                        occupiedPlantCells.Add(plant.Cell);
                        plantsCreated++;
                        createdInArea++;
                    }
                }
            }

            return new AreaGrowthStats(
                areasProcessed,
                seedEntriesVisited,
                seedBanksUpdated,
                plantsCreated);
        }

        private static void CopyAreaPayloadsUnchanged(
            EnvironmentState nextState,
            EnvironmentAreaSnapshot area)
        {
            if (area.HasFertility)
                nextState.SetFertilityArea(area.FertilityState);

            if (area.HasWater)
                nextState.SetWaterArea(area.WaterState);

            if (area.HasVegetation)
                nextState.SetVegetationArea(area.VegetationState);

            if (area.HasSeedBank)
                nextState.SetSeedBankArea(area.SeedBankState);
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
            int maxUpdates = NormalizeOptionalBudget(config.maxExistingPlantUpdatesPerDay);
            int maxRemovals = NormalizeOptionalBudget(config.maxDeadPlantsRemovedPerDay);
            var plants = snapshot.Plants ?? new EnvironmentPlantSnapshot[0];

            for (int i = 0; i < plants.Count; i++)
            {
                visited++;
                var current = plants[i];
                if (maxUpdates > 0 && updated >= maxUpdates)
                {
                    nextState.SetPlantInstance(CopyPlantUnchanged(current));
                    continue;
                }

                var next = EvolvePlant(
                    current,
                    plantCatalog,
                    climate,
                    transition,
                    config);

                if (config.removeDeadPlants
                    && !next.IsAlive
                    && (maxRemovals <= 0 || removed < maxRemovals))
                {
                    removed++;
                    continue;
                }

                if (nextState.SetPlantInstance(next))
                    updated++;
            }

            return new PlantGrowthStats(visited, updated, removed);
        }

        private static EnvironmentPlantInstance CopyPlantUnchanged(EnvironmentPlantSnapshot plant)
        {
            return new EnvironmentPlantInstance(
                plant.PlantId,
                plant.SpeciesKey,
                plant.Cell,
                plant.AgeDays,
                plant.GrowthStage,
                plant.GrowthStageKey,
                plant.HealthState,
                plant.Health01,
                plant.Maturity01,
                plant.IsHarvestable,
                plant.SourceAreaId);
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
                PlantVitalityProfile vitality = ResolvePlantVitalityProfile(current, config);
                bool favorableSeason = hasSpecies
                                       && species.IsSeasonFavorable(climate.Season);
                if (favorableSeason)
                {
                    healthDelta = EnvironmentMath.Clamp01(config.healthRecoveryStep01)
                                  * vitality.RecoveryMultiplier01;
                }
                else
                {
                    float dormancyMultiplier = hasSpecies
                        ? ResolveDormancyStressMultiplier(species.SeasonalBehavior, config)
                        : EnvironmentMath.Clamp01(config.unfavorableSeasonFallbackStressMultiplier01);
                    healthDelta = -(EnvironmentMath.Clamp01(config.healthStressStep01)
                                    * dormancyMultiplier
                                    * vitality.StressMultiplier01
                                    / vitality.HealthCapacity01);
                }

                // Health01 resta normalizzata, quindi il "numero di punti salute"
                // relativo entra come capacita': a parita' di stress grezzo, una
                // pianta con piu' vitalita' consuma una quota minore della sua barra.
                healthDelta -= climate.Aridity01
                               * EnvironmentMath.Clamp01(config.plantAridityHealthStressScale01)
                               * vitality.StressMultiplier01
                               / vitality.HealthCapacity01;
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

        // =============================================================================
        // ResolveDormancyStressMultiplier
        // =============================================================================
        /// <summary>
        /// <para>
        /// Risolve quanto una stagione non favorevole debba danneggiare una pianta.
        /// </para>
        ///
        /// <para><b>Principio architetturale: dormienza, non mortalita' istantanea</b></para>
        /// <para>
        /// Una quercia decidua in inverno non deve essere trattata come una pianta
        /// tropicale fuori habitat: entra in dormienza e riduce produzione/attivita',
        /// ma non perde grandi blocchi di salute ogni giorno. Il moltiplicatore
        /// resta configurabile per biomi e specie future senza introdurre sprite o
        /// logica visuale nel modello biologico.
        /// </para>
        /// </summary>
        private static float ResolveDormancyStressMultiplier(
            EnvironmentPlantSeasonalBehavior behavior,
            EnvironmentNaturalGrowthConfig config)
        {
            if (behavior == EnvironmentPlantSeasonalBehavior.Deciduous)
                return EnvironmentMath.Clamp01(config.deciduousDormancyStressMultiplier01);

            if (behavior == EnvironmentPlantSeasonalBehavior.Evergreen)
                return EnvironmentMath.Clamp01(config.evergreenDormancyStressMultiplier01);

            if (behavior == EnvironmentPlantSeasonalBehavior.Perennial)
                return EnvironmentMath.Clamp01(config.perennialDormancyStressMultiplier01);

            return EnvironmentMath.Clamp01(config.unfavorableSeasonFallbackStressMultiplier01);
        }

        // =============================================================================
        // ResolvePlantVitalityProfile
        // =============================================================================
        /// <summary>
        /// <para>
        /// Risolve il fattore individuale unico che governa salute, recupero e stress.
        /// </para>
        ///
        /// <para><b>Regola biologica: stessa causa, tre effetti coerenti</b></para>
        /// <para>
        /// Un individuo vigoroso parte con piu' salute potenziale, recupera piu'
        /// rapidamente e subisce meno perdita giornaliera. Un individuo fragile fa
        /// l'opposto. Il valore e' stabile per PlantId/specie/cella, quindi non
        /// cambia mentre la simulazione avanza.
        /// </para>
        /// </summary>
        private static PlantVitalityProfile ResolvePlantVitalityProfile(
            EnvironmentPlantSnapshot plant,
            EnvironmentNaturalGrowthConfig config)
        {
            return ResolvePlantVitalityProfile(
                plant.PlantId,
                plant.SpeciesKey,
                plant.Cell,
                config);
        }

        private static PlantVitalityProfile ResolvePlantVitalityProfile(
            EnvironmentPlantId plantId,
            string speciesKey,
            EnvironmentCellCoord cell,
            EnvironmentNaturalGrowthConfig config)
        {
            float min = config == null ? 0.70f : Math.Max(0.01f, config.plantVitalityMin01);
            float max = config == null ? 1.35f : Math.Max(min, config.plantVitalityMax01);
            float roll = ComputePlantStableUnitHash(
                plantId,
                speciesKey,
                cell,
                431);
            return new PlantVitalityProfile(Lerp(min, max, roll));
        }

        // =============================================================================
        // ResolveInitialPlantHealth01
        // =============================================================================
        /// <summary>
        /// <para>
        /// Converte un punteggio di nascita/germinazione nella salute iniziale reale.
        /// </para>
        ///
        /// <para>
        /// La salute iniziale non resta uguale per tutte le piante nate nello stesso
        /// giorno: viene spinta verso l'alto o verso il basso dalla stessa vitalita'
        /// che in seguito governara' recupero e danno.
        /// </para>
        /// </summary>
        private static float ResolveInitialPlantHealth01(
            float baseHealth01,
            PlantVitalityProfile vitality,
            EnvironmentNaturalGrowthConfig config)
        {
            float scale = config == null
                ? 0.22f
                : EnvironmentMath.Clamp01(config.initialPlantHealthVitalityScale01);
            float offset = (vitality.Vitality01 - 1f) * scale;
            return EnvironmentMath.Clamp01(baseHealth01 + offset);
        }

        private static bool TryCreatePlantFromArea(
            EnvironmentAreaSnapshot area,
            EnvironmentSeedBankAreaState seedBank,
            EnvironmentPlantCatalog plantCatalog,
            EnvironmentAreaEvolutionResult evolved,
            EnvironmentGlobalClimateState climate,
            EnvironmentSeasonProfile seasonProfile,
            EnvironmentNaturalGrowthConfig config,
            IReadOnlyDictionary<PlantPopulationKey, int> livePlantCounts,
            Dictionary<PlantPopulationKey, int> createdPlantCounts,
            List<EnvironmentCellCoord> occupiedPlantCells,
            int year,
            int dayOfYear,
            int alreadyCreatedToday,
            int alreadyCreatedInArea,
            out EnvironmentPlantInstance plant,
            out PlantPopulationKey populationKey)
        {
            plant = default;
            populationKey = default;

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

                populationKey = new PlantPopulationKey(area.Definition.AreaId, entry.SpeciesKey);
                float seedPressure01 = EnvironmentMath.Clamp01(entry.Amount01 * entry.Viability01);
                int desiredCount = ResolveDesiredPlantCount(
                    area.Definition,
                    seedPressure01,
                    config);
                int currentCount = ResolvePlantCount(livePlantCounts, populationKey)
                                   + ResolvePlantCount(createdPlantCounts, populationKey);
                if (currentCount >= desiredCount)
                    continue;

                float score = ComputeGerminationScore(
                    entry,
                    species,
                    evolved,
                    climate,
                    seasonProfile);
                if (score < EnvironmentMath.Clamp01(config.minimumGerminationScore01))
                    continue;

                if (!ChooseDeterministicPlantCell(
                    area.Definition,
                    alreadyCreatedToday + alreadyCreatedInArea,
                    occupiedPlantCells,
                    out EnvironmentCellCoord cell))
                {
                    continue;
                }

                var plantId = CreateDeterministicPlantId(
                    area.Definition.AreaId,
                    year,
                    dayOfYear,
                    alreadyCreatedToday,
                    entry.SpeciesKey);
                PlantVitalityProfile vitality = ResolvePlantVitalityProfile(
                    plantId,
                    entry.SpeciesKey,
                    cell,
                    config);
                float initialHealth01 = ResolveInitialPlantHealth01(
                    score,
                    vitality,
                    config);
                plant = EnvironmentPlantInstance.CreateFromSpecies(
                    plantId,
                    species,
                    cell,
                    0,
                    initialHealth01,
                    area.Definition.AreaId);
                return plant.PlantId.IsValid;
            }

            return false;
        }

        private static Dictionary<PlantPopulationKey, int> BuildLivePlantCounts(
            IReadOnlyList<EnvironmentPlantSnapshot> plants)
        {
            var result = new Dictionary<PlantPopulationKey, int>();
            var safePlants = plants ?? new EnvironmentPlantSnapshot[0];
            for (int i = 0; i < safePlants.Count; i++)
            {
                EnvironmentPlantSnapshot plant = safePlants[i];
                if (!plant.IsAlive)
                    continue;

                IncreasePlantCount(
                    result,
                    new PlantPopulationKey(plant.SourceAreaId, plant.SpeciesKey));
            }

            return result;
        }

        private static List<EnvironmentCellCoord> BuildOccupiedPlantCells(
            IReadOnlyList<EnvironmentPlantSnapshot> plants)
        {
            var result = new List<EnvironmentCellCoord>();
            var safePlants = plants ?? new EnvironmentPlantSnapshot[0];
            for (int i = 0; i < safePlants.Count; i++)
            {
                EnvironmentPlantSnapshot plant = safePlants[i];
                if (!plant.IsAlive || ContainsCell(result, plant.Cell))
                    continue;

                result.Add(plant.Cell);
            }

            return result;
        }

        private static int ResolvePlantCount(
            IReadOnlyDictionary<PlantPopulationKey, int> counts,
            PlantPopulationKey key)
        {
            if (counts == null)
                return 0;

            return counts.TryGetValue(key, out int value) && value > 0
                ? value
                : 0;
        }

        private static void IncreasePlantCount(
            Dictionary<PlantPopulationKey, int> counts,
            PlantPopulationKey key)
        {
            if (counts == null)
                return;

            counts.TryGetValue(key, out int current);
            counts[key] = current + 1;
        }

        private static int ResolveDesiredPlantCount(
            EnvironmentAreaDefinition area,
            float seedPressure01,
            EnvironmentNaturalGrowthConfig config)
        {
            // Il target popolazionale usa lo stesso parametro per-area usato dal
            // bootstrap fisico. In questo modo una foresta creata con
            // PhysicalPlantDominance01 alta non viene poi "potata" dal runtime verso
            // una seconda scala globale divergente. La seed pressure resta il freno
            // biologico specie-specifico: se amount/viability scendono, scende anche
            // il numero sostenibile della specie dentro l'area.
            int candidateCells = ResolveAreaCandidateCellCount(area);
            float scale = EnvironmentMath.Clamp01(area.PhysicalPlantDominance01);
            int desired = (int)Math.Round(candidateCells * EnvironmentMath.Clamp01(seedPressure01) * scale);
            if (desired < 0)
                return 0;

            return desired > candidateCells ? candidateCells : desired;
        }

        private static int ResolveAreaCandidateCellCount(EnvironmentAreaDefinition area)
        {
            if (!area.UsesCircularArea)
                return Math.Max(0, area.Bounds.Width * area.Bounds.Height);

            // In assenza del World dentro il loop data-only usiamo la geometria
            // dichiarata dell'area come stima stabile. Il filtro fisico preciso resta
            // compito del bootstrap/World boundary che conosce superfici e oggetti.
            int radius = area.RadiusCells;
            int count = 0;
            for (int y = area.CenterY - radius; y <= area.CenterY + radius; y++)
                for (int x = area.CenterX - radius; x <= area.CenterX + radius; x++)
                    if (area.ContainsCell(x, y, area.Bounds.Z))
                        count++;

            return count;
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

        private static bool ChooseDeterministicPlantCell(
            EnvironmentAreaDefinition area,
            int salt,
            List<EnvironmentCellCoord> occupiedPlantCells,
            out EnvironmentCellCoord cell)
        {
            cell = default;
            int bestScore = int.MinValue;
            bool hasBest = false;
            int radius = area.UsesCircularArea
                ? area.RadiusCells
                : Math.Max(area.Bounds.Width, area.Bounds.Height) / 2;
            int minX = area.UsesCircularArea ? area.CenterX - radius : area.Bounds.MinX;
            int maxX = area.UsesCircularArea ? area.CenterX + radius : area.Bounds.MaxX;
            int minY = area.UsesCircularArea ? area.CenterY - radius : area.Bounds.MinY;
            int maxY = area.UsesCircularArea ? area.CenterY + radius : area.Bounds.MaxY;

            for (int y = minY; y <= maxY; y++)
            {
                for (int x = minX; x <= maxX; x++)
                {
                    if (!area.ContainsCell(x, y, area.Bounds.Z))
                        continue;

                    var candidate = new EnvironmentCellCoord(x, y, area.Bounds.Z);
                    if (ContainsCell(occupiedPlantCells, candidate))
                        continue;

                    int score = ComputeCellScore(candidate, salt);
                    if (hasBest && score <= bestScore)
                        continue;

                    bestScore = score;
                    cell = candidate;
                    hasBest = true;
                }
            }

            return hasBest;
        }

        private static EnvironmentPlantId CreateDeterministicPlantId(
            EnvironmentAreaId areaId,
            int year,
            int dayOfYear,
            int salt,
            string speciesKey)
        {
            int hash = ComputeStableSpeciesHash(speciesKey);
            int value = 300000000
                        + (areaId.Value * 1000000)
                        + (Math.Abs(year % 1000) * 10000)
                        + (Math.Max(0, dayOfYear) * 32)
                        + (salt * 3)
                        + Math.Abs(hash % 3);

            return new EnvironmentPlantId(value);
        }

        private static int ComputeCellScore(EnvironmentCellCoord cell, int salt)
        {
            unchecked
            {
                int hash = 41 + salt;
                hash = (hash * 397) ^ (cell.X * 73856093);
                hash = (hash * 397) ^ (cell.Y * 19349663);
                hash = (hash * 397) ^ (cell.Z * 83492791);
                return hash & int.MaxValue;
            }
        }

        // =============================================================================
        // ComputePlantStableUnitHash
        // =============================================================================
        /// <summary>
        /// <para>
        /// Produce un valore normalizzato stabile per una pianta concreta.
        /// </para>
        /// </summary>
        private static float ComputePlantStableUnitHash(
            EnvironmentPlantId plantId,
            string speciesKey,
            EnvironmentCellCoord cell,
            int salt)
        {
            unchecked
            {
                int hash = 97 + salt;
                hash = (hash * 397) ^ plantId.Value;
                hash = (hash * 397) ^ ComputeStableSpeciesHash(speciesKey);
                hash = (hash * 397) ^ (cell.X * 73856093);
                hash = (hash * 397) ^ (cell.Y * 19349663);
                hash = (hash * 397) ^ (cell.Z * 83492791);
                return (hash & int.MaxValue) / (float)int.MaxValue;
            }
        }

        private static float Lerp(float min, float max, float t01)
        {
            float t = EnvironmentMath.Clamp01(t01);
            return min + ((max - min) * t);
        }

        private static bool ContainsCell(
            List<EnvironmentCellCoord> cells,
            EnvironmentCellCoord target)
        {
            if (cells == null)
                return false;

            for (int i = 0; i < cells.Count; i++)
            {
                EnvironmentCellCoord cell = cells[i];
                if (cell.X == target.X && cell.Y == target.Y && cell.Z == target.Z)
                    return true;
            }

            return false;
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

        private static int NormalizeOptionalBudget(int value)
        {
            return value <= 0 ? 0 : value;
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
