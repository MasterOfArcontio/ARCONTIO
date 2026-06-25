using System;
using System.Collections.Generic;

namespace Arcontio.Core.Environment
{
    // =============================================================================
    // EnvironmentPersistenceKind
    // =============================================================================
    /// <summary>
    /// <para>
    /// Classificazione del destino di persistenza di un dato ambientale.
    /// </para>
    ///
    /// <para><b>Principio architetturale: persistente, volatile e ricostruibile separati</b></para>
    /// <para>
    /// La biosfera non deve salvare tutto cio' che espone. Alcuni dati sono stato
    /// canonico, altri sono viste ricostruibili da snapshot, altri ancora sono
    /// temporanei e non devono entrare nel salvataggio.
    /// </para>
    ///
    /// <para><b>Struttura interna:</b></para>
    /// <list type="bullet">
    ///   <item><b>Persistent</b>: dato canonico da salvare.</item>
    ///   <item><b>Reconstructible</b>: dato derivabile dal Core dopo il load.</item>
    ///   <item><b>Volatile</b>: dato temporaneo da non salvare.</item>
    /// </list>
    /// </summary>
    public enum EnvironmentPersistenceKind
    {
        Persistent = 0,
        Reconstructible = 10,
        Volatile = 20
    }

    // =============================================================================
    // EnvironmentPersistenceManifest
    // =============================================================================
    /// <summary>
    /// <para>
    /// Manifesto data-only delle decisioni di persistenza ambientale.
    /// </para>
    ///
    /// <para><b>Principio architetturale: policy di salvataggio leggibile</b></para>
    /// <para>
    /// Il save/load deve dichiarare quali famiglie dati entrano nel salvataggio e
    /// quali no. Questo manifesto non salva file: rende verificabile la scelta
    /// architetturale e protegge da salvataggi visuali accidentali.
    /// </para>
    ///
    /// <para><b>Struttura interna:</b></para>
    /// <list type="bullet">
    ///   <item><b>Calendar</b>: calendario persistente.</item>
    ///   <item><b>Climate</b>: clima/meteo corrente persistente.</item>
    ///   <item><b>AreaRegistry</b>: definizioni area persistenti.</item>
    ///   <item><b>AreaPayloads</b>: fertility/water/vegetation/seedBank persistenti.</item>
    ///   <item><b>PlantInstances</b>: piante importanti persistenti.</item>
    ///   <item><b>ReadOnlySnapshots</b>: snapshot ricostruibili.</item>
    ///   <item><b>VisualSnapshots</b>: snapshot view non persistenti.</item>
    /// </list>
    /// </summary>
    public readonly struct EnvironmentPersistenceManifest
    {
        public readonly EnvironmentPersistenceKind Calendar;
        public readonly EnvironmentPersistenceKind Climate;
        public readonly EnvironmentPersistenceKind AreaRegistry;
        public readonly EnvironmentPersistenceKind AreaPayloads;
        public readonly EnvironmentPersistenceKind PlantInstances;
        public readonly EnvironmentPersistenceKind ReadOnlySnapshots;
        public readonly EnvironmentPersistenceKind VisualSnapshots;

        public bool IsVisualStateExcluded =>
            VisualSnapshots != EnvironmentPersistenceKind.Persistent
            && ReadOnlySnapshots != EnvironmentPersistenceKind.Persistent;

        // =============================================================================
        // EnvironmentPersistenceManifest
        // =============================================================================
        /// <summary>
        /// <para>
        /// Costruisce il manifesto con tutte le decisioni esplicite.
        /// </para>
        /// </summary>
        public EnvironmentPersistenceManifest(
            EnvironmentPersistenceKind calendar,
            EnvironmentPersistenceKind climate,
            EnvironmentPersistenceKind areaRegistry,
            EnvironmentPersistenceKind areaPayloads,
            EnvironmentPersistenceKind plantInstances,
            EnvironmentPersistenceKind readOnlySnapshots,
            EnvironmentPersistenceKind visualSnapshots)
        {
            Calendar = calendar;
            Climate = climate;
            AreaRegistry = areaRegistry;
            AreaPayloads = areaPayloads;
            PlantInstances = plantInstances;
            ReadOnlySnapshots = readOnlySnapshots;
            VisualSnapshots = visualSnapshots;
        }

        public static EnvironmentPersistenceManifest Default =>
            new EnvironmentPersistenceManifest(
                EnvironmentPersistenceKind.Persistent,
                EnvironmentPersistenceKind.Persistent,
                EnvironmentPersistenceKind.Persistent,
                EnvironmentPersistenceKind.Persistent,
                EnvironmentPersistenceKind.Persistent,
                EnvironmentPersistenceKind.Reconstructible,
                EnvironmentPersistenceKind.Volatile);
    }

    // =============================================================================
    // EnvironmentSaveData
    // =============================================================================
    /// <summary>
    /// <para>
    /// DTO radice del salvataggio ambientale.
    /// </para>
    ///
    /// <para><b>Principio architetturale: save data senza loader produttivo</b></para>
    /// <para>
    /// Questo tipo descrive cosa deve poter essere serializzato, ma non decide dove
    /// salvarlo, con quale formato o in quale ciclo vita. File, JSON concreto e
    /// integrazione col sistema save globale restano fuori dalla foundation.
    /// </para>
    ///
    /// <para><b>Struttura interna:</b></para>
    /// <list type="bullet">
    ///   <item><b>schemaVersion</b>: versione DTO.</item>
    ///   <item><b>elapsedEnvironmentTicks</b>: tempo canonico persistente.</item>
    ///   <item><b>climate</b>: clima/meteo corrente persistente.</item>
    ///   <item><b>areas</b>: definizioni e payload area persistenti.</item>
    ///   <item><b>plants</b>: PlantInstance persistenti.</item>
    /// </list>
    /// </summary>
    [Serializable]
    public sealed class EnvironmentSaveData
    {
        public const int CurrentSchemaVersion = 2;

        public int schemaVersion = CurrentSchemaVersion;
        public long elapsedEnvironmentTicks;
        public EnvironmentClimateSaveRecord climate = new EnvironmentClimateSaveRecord();
        public EnvironmentAreaSaveRecord[] areas = new EnvironmentAreaSaveRecord[0];
        public EnvironmentPlantSaveRecord[] plants = new EnvironmentPlantSaveRecord[0];
        public EnvironmentVegetationPlacementSaveRecord[] vegetationPlacements =
            new EnvironmentVegetationPlacementSaveRecord[0];
        public EnvironmentPhysicalPlantPlacementSaveRecord[] physicalPlantPlacements =
            new EnvironmentPhysicalPlantPlacementSaveRecord[0];

        public int ResolveSchemaVersion()
        {
            return schemaVersion <= 0 ? CurrentSchemaVersion : schemaVersion;
        }
    }

    // =============================================================================
    // EnvironmentClimateSaveRecord
    // =============================================================================
    /// <summary>
    /// <para>
    /// Record serializzabile del clima/meteo corrente.
    /// </para>
    ///
    /// <para><b>Principio architetturale: clima salvato come stato, non rigenerato a sorpresa</b></para>
    /// <para>
    /// Il meteo corrente puo' essere persistito per evitare che il load cambi
    /// improvvisamente condizioni ambientali gia' osservate da NPC o sistemi futuri.
    /// </para>
    ///
    /// <para><b>Struttura interna:</b></para>
    /// <list type="bullet">
    ///   <item><b>temperature01/humidity01/aridity01</b>: valori climatici normalizzati.</item>
    ///   <item><b>season</b>: stagione climatica.</item>
    ///   <item><b>weather*</b>: meteo corrente.</item>
    /// </list>
    /// </summary>
    [Serializable]
    public sealed class EnvironmentClimateSaveRecord
    {
        public float temperature01;
        public float humidity01;
        public float aridity01;
        public EnvironmentSeasonKind season;
        public EnvironmentWeatherKind weatherKind;
        public float weatherIntensity01;
        public float precipitation01;
        public float wind01;
        public bool isExtremeWeather;

        public static EnvironmentClimateSaveRecord FromState(
            EnvironmentGlobalClimateState state)
        {
            return new EnvironmentClimateSaveRecord
            {
                temperature01 = state.Temperature01,
                humidity01 = state.Humidity01,
                aridity01 = state.Aridity01,
                season = state.Season,
                weatherKind = state.Weather.Kind,
                weatherIntensity01 = state.Weather.Intensity01,
                precipitation01 = state.Weather.Precipitation01,
                wind01 = state.Weather.Wind01,
                isExtremeWeather = state.Weather.IsExtreme
            };
        }

        public EnvironmentGlobalClimateState ToState()
        {
            return new EnvironmentGlobalClimateState(
                temperature01,
                humidity01,
                aridity01,
                new EnvironmentWeatherState(
                    weatherKind,
                    weatherIntensity01,
                    precipitation01,
                    wind01,
                    isExtremeWeather),
                season);
        }
    }

    // =============================================================================
    // EnvironmentAreaSaveRecord
    // =============================================================================
    /// <summary>
    /// <para>
    /// Record serializzabile di un'area ambientale e dei suoi payload persistenti.
    /// </para>
    ///
    /// <para><b>Principio architetturale: area registry e layer nello stesso record</b></para>
    /// <para>
    /// Il record salva la definizione area e i payload opzionali associati. Non salva
    /// indici di query, snapshot visuali o cache ricostruibili.
    /// </para>
    ///
    /// <para><b>Struttura interna:</b></para>
    /// <list type="bullet">
    ///   <item><b>Definizione</b>: id, kind, bounds, priority, enabled, key.</item>
    ///   <item><b>has*</b>: presenza payload specializzati.</item>
    ///   <item><b>*</b>: record fertility/water/vegetation/seedBank.</item>
    /// </list>
    /// </summary>
    [Serializable]
    public sealed class EnvironmentAreaSaveRecord
    {
        public int areaId;
        public EnvironmentAreaKind kind;
        public int minX;
        public int minY;
        public int maxX;
        public int maxY;
        public int z;
        public int priority;
        public bool isEnabled;
        public string key = string.Empty;

        public bool hasFertility;
        public EnvironmentFertilitySaveRecord fertility = new EnvironmentFertilitySaveRecord();
        public bool hasWater;
        public EnvironmentWaterSaveRecord water = new EnvironmentWaterSaveRecord();
        public bool hasVegetation;
        public EnvironmentVegetationSaveRecord vegetation = new EnvironmentVegetationSaveRecord();
        public bool hasSeedBank;
        public EnvironmentSeedBankSaveRecord seedBank = new EnvironmentSeedBankSaveRecord();

        public EnvironmentAreaDefinition ToDefinition()
        {
            return new EnvironmentAreaDefinition(
                new EnvironmentAreaId(areaId),
                kind,
                new EnvironmentAreaBounds(minX, minY, maxX, maxY, z),
                priority,
                isEnabled,
                key);
        }

        public static EnvironmentAreaSaveRecord FromSnapshot(
            EnvironmentAreaSnapshot snapshot)
        {
            var definition = snapshot.Definition;
            return new EnvironmentAreaSaveRecord
            {
                areaId = definition.AreaId.Value,
                kind = definition.Kind,
                minX = definition.Bounds.MinX,
                minY = definition.Bounds.MinY,
                maxX = definition.Bounds.MaxX,
                maxY = definition.Bounds.MaxY,
                z = definition.Bounds.Z,
                priority = definition.Priority,
                isEnabled = definition.IsEnabled,
                key = definition.Key,
                hasFertility = snapshot.HasFertility,
                fertility = EnvironmentFertilitySaveRecord.FromState(snapshot.FertilityState),
                hasWater = snapshot.HasWater,
                water = EnvironmentWaterSaveRecord.FromState(snapshot.WaterState),
                hasVegetation = snapshot.HasVegetation,
                vegetation = EnvironmentVegetationSaveRecord.FromState(snapshot.VegetationState),
                hasSeedBank = snapshot.HasSeedBank,
                seedBank = EnvironmentSeedBankSaveRecord.FromState(snapshot.SeedBankState)
            };
        }
    }

    // =============================================================================
    // EnvironmentFertilitySaveRecord
    // =============================================================================
    /// <summary>
    /// <para>
    /// Record serializzabile di un payload fertilita'.
    /// </para>
    /// </summary>
    [Serializable]
    public sealed class EnvironmentFertilitySaveRecord
    {
        public EnvironmentSoilKind soilKind;
        public float baseFertility01;
        public float currentFertility01;
        public float growthModifier01;
        public float exhaustion01;
        public float recovery01;

        public static EnvironmentFertilitySaveRecord FromState(
            EnvironmentFertilityAreaState state)
        {
            return new EnvironmentFertilitySaveRecord
            {
                soilKind = state.SoilKind,
                baseFertility01 = state.BaseFertility01,
                currentFertility01 = state.CurrentFertility01,
                growthModifier01 = state.GrowthModifier01,
                exhaustion01 = state.Exhaustion01,
                recovery01 = state.Recovery01
            };
        }

        public EnvironmentFertilityAreaState ToState(EnvironmentAreaId areaId)
        {
            return new EnvironmentFertilityAreaState(
                areaId,
                soilKind,
                baseFertility01,
                currentFertility01,
                growthModifier01,
                exhaustion01,
                recovery01);
        }
    }

    // =============================================================================
    // EnvironmentWaterSaveRecord
    // =============================================================================
    /// <summary>
    /// <para>
    /// Record serializzabile di un payload acqua.
    /// </para>
    /// </summary>
    [Serializable]
    public sealed class EnvironmentWaterSaveRecord
    {
        public EnvironmentWaterKind waterKind;
        public EnvironmentWaterDepthLevel depthLevel;
        public float waterLevel01;
        public float flowIntensity01;
        public bool isDrinkable;
        public bool isSeasonal;

        public static EnvironmentWaterSaveRecord FromState(
            EnvironmentWaterAreaState state)
        {
            return new EnvironmentWaterSaveRecord
            {
                waterKind = state.WaterKind,
                depthLevel = state.DepthLevel,
                waterLevel01 = state.WaterLevel01,
                flowIntensity01 = state.FlowIntensity01,
                isDrinkable = state.IsDrinkable,
                isSeasonal = state.IsSeasonal
            };
        }

        public EnvironmentWaterAreaState ToState(EnvironmentAreaId areaId)
        {
            return new EnvironmentWaterAreaState(
                areaId,
                waterKind,
                depthLevel,
                waterLevel01,
                flowIntensity01,
                isDrinkable,
                isSeasonal);
        }
    }

    // =============================================================================
    // EnvironmentVegetationSaveRecord
    // =============================================================================
    /// <summary>
    /// <para>
    /// Record serializzabile di un payload vegetazione.
    /// </para>
    /// </summary>
    [Serializable]
    public sealed class EnvironmentVegetationSaveRecord
    {
        public EnvironmentVegetationKind vegetationKind;
        public float density01;
        public float growthPotential01;
        public float health01;
        public float fertilityInfluence01;
        public float climateInfluence01;

        public static EnvironmentVegetationSaveRecord FromState(
            EnvironmentVegetationAreaState state)
        {
            return new EnvironmentVegetationSaveRecord
            {
                vegetationKind = state.VegetationKind,
                density01 = state.Density01,
                growthPotential01 = state.GrowthPotential01,
                health01 = state.Health01,
                fertilityInfluence01 = state.FertilityInfluence01,
                climateInfluence01 = state.ClimateInfluence01
            };
        }

        public EnvironmentVegetationAreaState ToState(EnvironmentAreaId areaId)
        {
            return new EnvironmentVegetationAreaState(
                areaId,
                vegetationKind,
                density01,
                growthPotential01,
                health01,
                fertilityInfluence01,
                climateInfluence01);
        }
    }

    // =============================================================================
    // EnvironmentSeedBankSaveRecord
    // =============================================================================
    /// <summary>
    /// <para>
    /// Record serializzabile di una seed bank naturale.
    /// </para>
    /// </summary>
    [Serializable]
    public sealed class EnvironmentSeedBankSaveRecord
    {
        public EnvironmentSeedBankEntrySaveRecord[] entries =
            new EnvironmentSeedBankEntrySaveRecord[0];

        public static EnvironmentSeedBankSaveRecord FromState(
            EnvironmentSeedBankAreaState state)
        {
            var source = state?.Entries ?? new EnvironmentSeedBankEntry[0];
            var records = new EnvironmentSeedBankEntrySaveRecord[source.Count];
            for (int i = 0; i < source.Count; i++)
                records[i] = EnvironmentSeedBankEntrySaveRecord.FromEntry(source[i]);

            return new EnvironmentSeedBankSaveRecord { entries = records };
        }

        public EnvironmentSeedBankAreaState ToState(EnvironmentAreaId areaId)
        {
            var safeEntries = entries ?? new EnvironmentSeedBankEntrySaveRecord[0];
            var values = new EnvironmentSeedBankEntry[safeEntries.Length];
            for (int i = 0; i < safeEntries.Length; i++)
            {
                values[i] = safeEntries[i] == null
                    ? new EnvironmentSeedBankEntry(string.Empty, 0f, 0f)
                    : safeEntries[i].ToEntry();
            }

            return new EnvironmentSeedBankAreaState(areaId, values);
        }
    }

    // =============================================================================
    // EnvironmentSeedBankEntrySaveRecord
    // =============================================================================
    /// <summary>
    /// <para>
    /// Record serializzabile di una singola pressione seme.
    /// </para>
    /// </summary>
    [Serializable]
    public sealed class EnvironmentSeedBankEntrySaveRecord
    {
        public string speciesKey = string.Empty;
        public float amount01;
        public float viability01;

        public static EnvironmentSeedBankEntrySaveRecord FromEntry(
            EnvironmentSeedBankEntry entry)
        {
            return new EnvironmentSeedBankEntrySaveRecord
            {
                speciesKey = entry.SpeciesKey,
                amount01 = entry.Amount01,
                viability01 = entry.Viability01
            };
        }

        public EnvironmentSeedBankEntry ToEntry()
        {
            return new EnvironmentSeedBankEntry(
                speciesKey,
                amount01,
                viability01);
        }
    }

    // =============================================================================
    // EnvironmentPlantSaveRecord
    // =============================================================================
    /// <summary>
    /// <para>
    /// Record serializzabile di una PlantInstance persistente.
    /// </para>
    /// </summary>
    [Serializable]
    public sealed class EnvironmentPlantSaveRecord
    {
        public int plantId;
        public string speciesKey = string.Empty;
        public int x;
        public int y;
        public int z;
        public int ageDays;
        public EnvironmentPlantGrowthStage growthStage;
        public string growthStageKey = string.Empty;
        public EnvironmentPlantHealthState healthState;
        public float health01;
        public float maturity01;
        public bool isHarvestable;
        public int sourceAreaId;

        public static EnvironmentPlantSaveRecord FromSnapshot(
            EnvironmentPlantSnapshot snapshot)
        {
            return new EnvironmentPlantSaveRecord
            {
                plantId = snapshot.PlantId.Value,
                speciesKey = snapshot.SpeciesKey,
                x = snapshot.Cell.X,
                y = snapshot.Cell.Y,
                z = snapshot.Cell.Z,
                ageDays = snapshot.AgeDays,
                growthStage = snapshot.GrowthStage,
                growthStageKey = snapshot.GrowthStageKey,
                healthState = snapshot.HealthState,
                health01 = snapshot.Health01,
                maturity01 = snapshot.Maturity01,
                isHarvestable = snapshot.IsHarvestable,
                sourceAreaId = snapshot.SourceAreaId.Value
            };
        }

        public EnvironmentPlantInstance ToInstance()
        {
            return new EnvironmentPlantInstance(
                new EnvironmentPlantId(plantId),
                speciesKey,
                new EnvironmentCellCoord(x, y, z),
                ageDays,
                growthStage,
                growthStageKey,
                healthState,
                health01,
                maturity01,
                isHarvestable,
                new EnvironmentAreaId(sourceAreaId));
        }
    }

    // =============================================================================
    // EnvironmentVegetationPlacementSaveRecord
    // =============================================================================
    /// <summary>
    /// <para>
    /// Record serializzabile di una cella occupata da vegetazione diffusa.
    /// </para>
    ///
    /// <para><b>Principio architetturale: layout vegetale persistito senza sprite</b></para>
    /// <para>
    /// La vegetazione diffusa e' un dato cell-based della biosfera, non una tile
    /// fisica del terreno e non uno sprite ArcGraph. Persistire il placement evita
    /// che un load rigeneri una distribuzione diversa da quella gia' vissuta.
    /// </para>
    /// </summary>
    [Serializable]
    public sealed class EnvironmentVegetationPlacementSaveRecord
    {
        public int areaId;
        public int x;
        public int y;
        public int z;
        public EnvironmentVegetationKind vegetationKind;
        public float density01;
        public float health01;

        public static EnvironmentVegetationPlacementSaveRecord FromPlacement(
            EnvironmentVegetationCellPlacement placement)
        {
            return new EnvironmentVegetationPlacementSaveRecord
            {
                areaId = placement.AreaId.Value,
                x = placement.Cell.X,
                y = placement.Cell.Y,
                z = placement.Cell.Z,
                vegetationKind = placement.VegetationKind,
                density01 = placement.Density01,
                health01 = placement.Health01
            };
        }

        public EnvironmentVegetationCellPlacement ToPlacement()
        {
            return new EnvironmentVegetationCellPlacement(
                new EnvironmentAreaId(areaId),
                new EnvironmentCellCoord(x, y, z),
                vegetationKind,
                density01,
                health01);
        }
    }

    // =============================================================================
    // EnvironmentPhysicalPlantPlacementSaveRecord
    // =============================================================================
    /// <summary>
    /// <para>
    /// Record serializzabile di una pianta fisica piazzata in mappa dalla biosfera.
    /// </para>
    ///
    /// <para><b>Principio architetturale: fisica derivata persistita come riferimento</b></para>
    /// <para>
    /// Il record non duplica salute, eta' o maturita': quei valori vivono nella
    /// <see cref="EnvironmentPlantSaveRecord"/>. Qui si salva solo il riferimento
    /// plantId/area/cella necessario al World per ricostruire occupazione, FOV e
    /// feed visuale dopo load.
    /// </para>
    /// </summary>
    [Serializable]
    public sealed class EnvironmentPhysicalPlantPlacementSaveRecord
    {
        public int plantId;
        public int areaId;
        public int x;
        public int y;
        public int z;
        public string speciesKey = string.Empty;

        public static EnvironmentPhysicalPlantPlacementSaveRecord FromPlacement(
            EnvironmentPhysicalPlantPlacement placement)
        {
            return new EnvironmentPhysicalPlantPlacementSaveRecord
            {
                plantId = placement.PlantId.Value,
                areaId = placement.AreaId.Value,
                x = placement.Cell.X,
                y = placement.Cell.Y,
                z = placement.Cell.Z,
                speciesKey = placement.SpeciesKey ?? string.Empty
            };
        }

        public EnvironmentPhysicalPlantPlacement ToPlacement()
        {
            return new EnvironmentPhysicalPlantPlacement(
                new EnvironmentPlantId(plantId),
                new EnvironmentAreaId(areaId),
                new EnvironmentCellCoord(x, y, z),
                speciesKey);
        }
    }

    // =============================================================================
    // EnvironmentLoadReport
    // =============================================================================
    /// <summary>
    /// <para>
    /// Report del ripristino di uno stato ambientale da save data.
    /// </para>
    /// </summary>
    public readonly struct EnvironmentLoadReport
    {
        public readonly int AreasLoaded;
        public readonly int PlantsLoaded;
        public readonly int RejectedRecords;

        public bool HasRejectedRecords => RejectedRecords > 0;

        public EnvironmentLoadReport(
            int areasLoaded,
            int plantsLoaded,
            int rejectedRecords)
        {
            AreasLoaded = areasLoaded < 0 ? 0 : areasLoaded;
            PlantsLoaded = plantsLoaded < 0 ? 0 : plantsLoaded;
            RejectedRecords = rejectedRecords < 0 ? 0 : rejectedRecords;
        }
    }

    // =============================================================================
    // EnvironmentLoadResult
    // =============================================================================
    /// <summary>
    /// <para>
    /// Risultato del ripristino data-only della biosfera.
    /// </para>
    /// </summary>
    public sealed class EnvironmentLoadResult
    {
        public EnvironmentState State { get; }
        public EnvironmentLoadReport Report { get; }

        public EnvironmentLoadResult(
            EnvironmentState state,
            EnvironmentLoadReport report)
        {
            State = state ?? new EnvironmentState();
            Report = report;
        }
    }

    // =============================================================================
    // EnvironmentPersistenceResolver
    // =============================================================================
    /// <summary>
    /// <para>
    /// Resolver data-only per cattura e ripristino dello stato ambientale persistente.
    /// </para>
    ///
    /// <para><b>Principio architetturale: persistenza senza file system nel Core</b></para>
    /// <para>
    /// Il resolver converte snapshot e DTO in stato Core. Non apre file, non sceglie
    /// path, non serializza JSON e non conosce ArcGraph. Il sistema save globale
    /// potra' usare questi DTO come payload quando esistera' l'integrazione.
    /// </para>
    ///
    /// <para><b>Struttura interna:</b></para>
    /// <list type="bullet">
    ///   <item><b>Capture</b>: converte uno snapshot in save data persistente.</item>
    ///   <item><b>Restore</b>: ricostruisce EnvironmentState da save data.</item>
    ///   <item><b>Manifest</b>: espone la policy di persistenza corrente.</item>
    /// </list>
    /// </summary>
    public static class EnvironmentPersistenceResolver
    {
        public static EnvironmentPersistenceManifest Manifest =>
            EnvironmentPersistenceManifest.Default;

        // =============================================================================
        // Capture
        // =============================================================================
        /// <summary>
        /// <para>
        /// Cattura lo stato persistente da uno snapshot Core.
        /// </para>
        /// </summary>
        public static EnvironmentSaveData Capture(EnvironmentSnapshot snapshot)
        {
            if (snapshot == null)
                return new EnvironmentSaveData();

            return new EnvironmentSaveData
            {
                schemaVersion = EnvironmentSaveData.CurrentSchemaVersion,
                elapsedEnvironmentTicks = snapshot.Calendar.ElapsedEnvironmentTicks,
                climate = EnvironmentClimateSaveRecord.FromState(snapshot.Climate),
                areas = CaptureAreas(snapshot.Areas),
                plants = CapturePlants(snapshot.Plants),
                vegetationPlacements = new EnvironmentVegetationPlacementSaveRecord[0],
                physicalPlantPlacements = new EnvironmentPhysicalPlantPlacementSaveRecord[0]
            };
        }

        // =============================================================================
        // Capture
        // =============================================================================
        /// <summary>
        /// <para>
        /// Cattura lo stato persistente completo da <see cref="EnvironmentState"/>,
        /// includendo anche i placement runtime cell-based.
        /// </para>
        ///
        /// <para><b>Principio architetturale: layout vissuto, non rigenerato</b></para>
        /// <para>
        /// Lo snapshot read-only espone aree e piante, ma non contiene le liste di
        /// placement fisico/vegetale. Il save globale usa questo overload per
        /// preservare anche la distribuzione concreta che il World e ArcGraph stanno
        /// leggendo.
        /// </para>
        /// </summary>
        public static EnvironmentSaveData Capture(EnvironmentState state)
        {
            if (state == null)
                return new EnvironmentSaveData();

            EnvironmentSaveData data = Capture(state.CreateSnapshot());
            data.vegetationPlacements = CaptureVegetationPlacements(state.VegetationCellPlacements);
            data.physicalPlantPlacements = CapturePhysicalPlantPlacements(state.PhysicalPlantPlacements);
            return data;
        }

        // =============================================================================
        // Restore
        // =============================================================================
        /// <summary>
        /// <para>
        /// Ricostruisce uno stato ambientale da DTO persistente.
        /// </para>
        /// </summary>
        public static EnvironmentLoadResult Restore(
            EnvironmentSaveData saveData,
            EnvironmentCalendarConfig calendarConfig)
        {
            var state = new EnvironmentState();
            var safeData = saveData ?? new EnvironmentSaveData();
            var calendar = EnvironmentCalendarResolver.Resolve(
                safeData.elapsedEnvironmentTicks,
                calendarConfig ?? new EnvironmentCalendarConfig());
            state.SetCalendar(calendar);
            state.SetClimate((safeData.climate ?? new EnvironmentClimateSaveRecord()).ToState());

            int areasLoaded = 0;
            int plantsLoaded = 0;
            int rejected = 0;
            var areas = safeData.areas ?? new EnvironmentAreaSaveRecord[0];
            for (int i = 0; i < areas.Length; i++)
            {
                if (areas[i] == null)
                {
                    rejected++;
                    continue;
                }

                var areaId = new EnvironmentAreaId(areas[i].areaId);
                var definition = areas[i].ToDefinition();
                if (!areaId.IsValid || !definition.Bounds.IsValid)
                {
                    rejected++;
                    continue;
                }

                if (state.SetAreaDefinition(definition))
                    areasLoaded++;
                else
                    rejected++;

                if (areas[i].hasFertility && areas[i].fertility != null)
                    state.SetFertilityArea(areas[i].fertility.ToState(areaId));

                if (areas[i].hasWater && areas[i].water != null)
                    state.SetWaterArea(areas[i].water.ToState(areaId));

                if (areas[i].hasVegetation && areas[i].vegetation != null)
                    state.SetVegetationArea(areas[i].vegetation.ToState(areaId));

                if (areas[i].hasSeedBank && areas[i].seedBank != null)
                    state.SetSeedBankArea(areas[i].seedBank.ToState(areaId));
            }

            var plants = safeData.plants ?? new EnvironmentPlantSaveRecord[0];
            for (int i = 0; i < plants.Length; i++)
            {
                if (plants[i] == null)
                {
                    rejected++;
                    continue;
                }

                if (state.SetPlantInstance(plants[i].ToInstance()))
                    plantsLoaded++;
                else
                    rejected++;
            }

            var vegetationPlacements = RestoreVegetationPlacements(safeData.vegetationPlacements, ref rejected);
            var physicalPlantPlacements = RestorePhysicalPlantPlacements(safeData.physicalPlantPlacements, ref rejected);
            rejected += state.ReplaceBiologicalPlacementsForSaveLoad(
                vegetationPlacements,
                physicalPlantPlacements);

            return new EnvironmentLoadResult(
                state,
                new EnvironmentLoadReport(areasLoaded, plantsLoaded, rejected));
        }

        private static EnvironmentAreaSaveRecord[] CaptureAreas(
            IReadOnlyList<EnvironmentAreaSnapshot> areas)
        {
            if (areas == null || areas.Count == 0)
                return new EnvironmentAreaSaveRecord[0];

            var records = new EnvironmentAreaSaveRecord[areas.Count];
            for (int i = 0; i < areas.Count; i++)
                records[i] = EnvironmentAreaSaveRecord.FromSnapshot(areas[i]);

            return records;
        }

        private static EnvironmentPlantSaveRecord[] CapturePlants(
            IReadOnlyList<EnvironmentPlantSnapshot> plants)
        {
            if (plants == null || plants.Count == 0)
                return new EnvironmentPlantSaveRecord[0];

            var records = new EnvironmentPlantSaveRecord[plants.Count];
            for (int i = 0; i < plants.Count; i++)
                records[i] = EnvironmentPlantSaveRecord.FromSnapshot(plants[i]);

            return records;
        }

        private static EnvironmentVegetationPlacementSaveRecord[] CaptureVegetationPlacements(
            IReadOnlyList<EnvironmentVegetationCellPlacement> placements)
        {
            if (placements == null || placements.Count == 0)
                return new EnvironmentVegetationPlacementSaveRecord[0];

            var records = new EnvironmentVegetationPlacementSaveRecord[placements.Count];
            for (int i = 0; i < placements.Count; i++)
                records[i] = EnvironmentVegetationPlacementSaveRecord.FromPlacement(placements[i]);

            return records;
        }

        private static EnvironmentPhysicalPlantPlacementSaveRecord[] CapturePhysicalPlantPlacements(
            IReadOnlyList<EnvironmentPhysicalPlantPlacement> placements)
        {
            if (placements == null || placements.Count == 0)
                return new EnvironmentPhysicalPlantPlacementSaveRecord[0];

            var records = new EnvironmentPhysicalPlantPlacementSaveRecord[placements.Count];
            for (int i = 0; i < placements.Count; i++)
                records[i] = EnvironmentPhysicalPlantPlacementSaveRecord.FromPlacement(placements[i]);

            return records;
        }

        private static EnvironmentVegetationCellPlacement[] RestoreVegetationPlacements(
            EnvironmentVegetationPlacementSaveRecord[] records,
            ref int rejected)
        {
            var safeRecords = records ?? new EnvironmentVegetationPlacementSaveRecord[0];
            var placements = new List<EnvironmentVegetationCellPlacement>(safeRecords.Length);
            for (int i = 0; i < safeRecords.Length; i++)
            {
                if (safeRecords[i] == null)
                {
                    rejected++;
                    continue;
                }

                placements.Add(safeRecords[i].ToPlacement());
            }

            return placements.ToArray();
        }

        private static EnvironmentPhysicalPlantPlacement[] RestorePhysicalPlantPlacements(
            EnvironmentPhysicalPlantPlacementSaveRecord[] records,
            ref int rejected)
        {
            var safeRecords = records ?? new EnvironmentPhysicalPlantPlacementSaveRecord[0];
            var placements = new List<EnvironmentPhysicalPlantPlacement>(safeRecords.Length);
            for (int i = 0; i < safeRecords.Length; i++)
            {
                if (safeRecords[i] == null)
                {
                    rejected++;
                    continue;
                }

                placements.Add(safeRecords[i].ToPlacement());
            }

            return placements.ToArray();
        }
    }
}
