using System.Collections.Generic;

namespace Arcontio.Core.Environment
{
    // =============================================================================
    // EnvironmentCalendarSnapshot
    // =============================================================================
    /// <summary>
    /// <para>
    /// Snapshot read-only compatto del calendario ambientale.
    /// </para>
    ///
    /// <para><b>Principio architetturale: tempo osservabile senza lifecycle</b></para>
    /// <para>
    /// I consumer futuri devono poter leggere data, ora e stagione senza conoscere
    /// resolver, tick grezzi o configurazioni temporali. Questo snapshot non avanza
    /// tempo e non decide cadenze di sistema.
    /// </para>
    ///
    /// <para><b>Struttura interna:</b></para>
    /// <list type="bullet">
    ///   <item><b>ElapsedEnvironmentTicks</b>: tick ambientali gia' risolti.</item>
    ///   <item><b>Year/Month/Day*</b>: coordinate temporali discrete.</item>
    ///   <item><b>Season</b>: stagione corrente.</item>
    ///   <item><b>Hour/NormalizedDay01</b>: ora e progressione giornaliera.</item>
    /// </list>
    /// </summary>
    public readonly struct EnvironmentCalendarSnapshot
    {
        public readonly long ElapsedEnvironmentTicks;
        public readonly int AbsoluteDayIndex;
        public readonly int Year;
        public readonly int Month;
        public readonly int DayOfMonth;
        public readonly int DayOfYear;
        public readonly EnvironmentSeasonKind Season;
        public readonly int Hour;
        public readonly float NormalizedDay01;

        // =============================================================================
        // EnvironmentCalendarSnapshot
        // =============================================================================
        /// <summary>
        /// <para>
        /// Costruisce lo snapshot calendario copiando solo dati value-only.
        /// </para>
        /// </summary>
        public EnvironmentCalendarSnapshot(EnvironmentCalendarState state)
        {
            ElapsedEnvironmentTicks = state.ElapsedEnvironmentTicks;
            AbsoluteDayIndex = state.AbsoluteDayIndex;
            Year = state.Date.Year;
            Month = state.Date.Month;
            DayOfMonth = state.Date.DayOfMonth;
            DayOfYear = state.Date.DayOfYear;
            Season = state.Date.Season;
            Hour = state.TimeOfDay.Hour;
            NormalizedDay01 = state.TimeOfDay.NormalizedDay01;
        }
    }

    // =============================================================================
    // EnvironmentWeatherSnapshot
    // =============================================================================
    /// <summary>
    /// <para>
    /// Snapshot read-only del meteo globale.
    /// </para>
    ///
    /// <para><b>Principio architetturale: meteo simulativo separato dal visuale</b></para>
    /// <para>
    /// Lo snapshot espone intensita', precipitazione e vento come dati Core. Non
    /// contiene particelle, overlay, sprite o riferimenti ad ArcGraph.
    /// </para>
    ///
    /// <para><b>Struttura interna:</b></para>
    /// <list type="bullet">
    ///   <item><b>Kind</b>: tipo meteo.</item>
    ///   <item><b>Intensity01</b>: intensita' normalizzata.</item>
    ///   <item><b>Precipitation01</b>: precipitazione normalizzata.</item>
    ///   <item><b>Wind01</b>: vento normalizzato.</item>
    ///   <item><b>IsExtreme</b>: marker eventi estremi futuri.</item>
    /// </list>
    /// </summary>
    public readonly struct EnvironmentWeatherSnapshot
    {
        public readonly EnvironmentWeatherKind Kind;
        public readonly float Intensity01;
        public readonly float Precipitation01;
        public readonly float Wind01;
        public readonly bool IsExtreme;

        // =============================================================================
        // EnvironmentWeatherSnapshot
        // =============================================================================
        /// <summary>
        /// <para>
        /// Costruisce lo snapshot meteo da uno stato gia' risolto.
        /// </para>
        /// </summary>
        public EnvironmentWeatherSnapshot(EnvironmentWeatherState state)
        {
            Kind = state.Kind;
            Intensity01 = state.Intensity01;
            Precipitation01 = state.Precipitation01;
            Wind01 = state.Wind01;
            IsExtreme = state.IsExtreme;
        }
    }

    // =============================================================================
    // EnvironmentClimateSnapshot
    // =============================================================================
    /// <summary>
    /// <para>
    /// Snapshot read-only del clima globale.
    /// </para>
    ///
    /// <para><b>Principio architetturale: clima globale come sorgente Core</b></para>
    /// <para>
    /// NPC, debug, save/load e adapter futuri leggono temperatura, umidita',
    /// aridita' e meteo da questo contratto. Nessun consumer riceve authority per
    /// rigenerare clima o alterare calendario.
    /// </para>
    ///
    /// <para><b>Struttura interna:</b></para>
    /// <list type="bullet">
    ///   <item><b>Temperature01</b>: temperatura normalizzata.</item>
    ///   <item><b>Humidity01</b>: umidita' normalizzata.</item>
    ///   <item><b>Aridity01</b>: aridita' normalizzata.</item>
    ///   <item><b>Season</b>: stagione climatica.</item>
    ///   <item><b>Weather</b>: snapshot meteo incluso.</item>
    /// </list>
    /// </summary>
    public readonly struct EnvironmentClimateSnapshot
    {
        public readonly float Temperature01;
        public readonly float Humidity01;
        public readonly float Aridity01;
        public readonly EnvironmentSeasonKind Season;
        public readonly EnvironmentWeatherSnapshot Weather;

        // =============================================================================
        // EnvironmentClimateSnapshot
        // =============================================================================
        /// <summary>
        /// <para>
        /// Costruisce lo snapshot clima da uno stato globale gia' risolto.
        /// </para>
        /// </summary>
        public EnvironmentClimateSnapshot(EnvironmentGlobalClimateState state)
        {
            Temperature01 = state.Temperature01;
            Humidity01 = state.Humidity01;
            Aridity01 = state.Aridity01;
            Season = state.Season;
            Weather = new EnvironmentWeatherSnapshot(state.Weather);
        }
    }

    // =============================================================================
    // EnvironmentFertilitySnapshot
    // =============================================================================
    /// <summary>
    /// <para>
    /// Snapshot read-only del payload fertilita' di area.
    /// </para>
    ///
    /// <para><b>Principio architetturale: fertilita' leggibile per layer</b></para>
    /// <para>
    /// La fertilita' resta una proprieta' d'area e non viene confusa con terreno
    /// visuale, oggetti o risorse. Questo snapshot isola i dati che un consumer puo'
    /// leggere senza mutare lo stato.
    /// </para>
    ///
    /// <para><b>Struttura interna:</b></para>
    /// <list type="bullet">
    ///   <item><b>AreaId</b>: area sorgente.</item>
    ///   <item><b>SoilKind</b>: tipo suolo astratto.</item>
    ///   <item><b>Base/CurrentFertility01</b>: fertilita' naturale e corrente.</item>
    ///   <item><b>GrowthModifier01</b>: supporto crescita.</item>
    ///   <item><b>Exhaustion01/Recovery01</b>: pressione agricola e recupero.</item>
    /// </list>
    /// </summary>
    public readonly struct EnvironmentFertilitySnapshot
    {
        public readonly EnvironmentAreaId AreaId;
        public readonly EnvironmentSoilKind SoilKind;
        public readonly float BaseFertility01;
        public readonly float CurrentFertility01;
        public readonly float GrowthModifier01;
        public readonly float Exhaustion01;
        public readonly float Recovery01;

        // =============================================================================
        // EnvironmentFertilitySnapshot
        // =============================================================================
        /// <summary>
        /// <para>
        /// Costruisce lo snapshot fertilita' copiando un payload area.
        /// </para>
        /// </summary>
        public EnvironmentFertilitySnapshot(EnvironmentFertilityAreaState state)
        {
            AreaId = state.AreaId;
            SoilKind = state.SoilKind;
            BaseFertility01 = state.BaseFertility01;
            CurrentFertility01 = state.CurrentFertility01;
            GrowthModifier01 = state.GrowthModifier01;
            Exhaustion01 = state.Exhaustion01;
            Recovery01 = state.Recovery01;
        }
    }

    // =============================================================================
    // EnvironmentWaterSnapshot
    // =============================================================================
    /// <summary>
    /// <para>
    /// Snapshot read-only del payload acqua di area.
    /// </para>
    ///
    /// <para><b>Principio architetturale: acqua osservata senza fluidodinamica</b></para>
    /// <para>
    /// Il consumer legge tipo, profondita' e livello acqua. Non riceve funzioni di
    /// propagazione, pathfinding o rendering.
    /// </para>
    ///
    /// <para><b>Struttura interna:</b></para>
    /// <list type="bullet">
    ///   <item><b>AreaId</b>: area sorgente.</item>
    ///   <item><b>WaterKind</b>: tipo acqua.</item>
    ///   <item><b>DepthLevel</b>: profondita' discreta.</item>
    ///   <item><b>WaterLevel01</b>: livello normalizzato.</item>
    ///   <item><b>FlowIntensity01</b>: flusso astratto.</item>
    ///   <item><b>IsDrinkable/IsSeasonal</b>: flag ambientali.</item>
    /// </list>
    /// </summary>
    public readonly struct EnvironmentWaterSnapshot
    {
        public readonly EnvironmentAreaId AreaId;
        public readonly EnvironmentWaterKind WaterKind;
        public readonly EnvironmentWaterDepthLevel DepthLevel;
        public readonly float WaterLevel01;
        public readonly float FlowIntensity01;
        public readonly bool IsDrinkable;
        public readonly bool IsSeasonal;

        // =============================================================================
        // EnvironmentWaterSnapshot
        // =============================================================================
        /// <summary>
        /// <para>
        /// Costruisce lo snapshot acqua copiando un payload area.
        /// </para>
        /// </summary>
        public EnvironmentWaterSnapshot(EnvironmentWaterAreaState state)
        {
            AreaId = state.AreaId;
            WaterKind = state.WaterKind;
            DepthLevel = state.DepthLevel;
            WaterLevel01 = state.WaterLevel01;
            FlowIntensity01 = state.FlowIntensity01;
            IsDrinkable = state.IsDrinkable;
            IsSeasonal = state.IsSeasonal;
        }
    }

    // =============================================================================
    // EnvironmentVegetationSnapshot
    // =============================================================================
    /// <summary>
    /// <para>
    /// Snapshot read-only del payload vegetazione diffusa.
    /// </para>
    ///
    /// <para><b>Principio architetturale: vegetazione diffusa separata dalle piante</b></para>
    /// <para>
    /// Questo snapshot descrive densita' e salute media dell'area. Alberi, arbusti o
    /// colture importanti restano invece in <see cref="EnvironmentPlantSnapshot"/>.
    /// </para>
    ///
    /// <para><b>Struttura interna:</b></para>
    /// <list type="bullet">
    ///   <item><b>AreaId</b>: area sorgente.</item>
    ///   <item><b>VegetationKind</b>: categoria dominante.</item>
    ///   <item><b>Density01</b>: densita' diffusa.</item>
    ///   <item><b>GrowthPotential01</b>: potenziale crescita.</item>
    ///   <item><b>Health01</b>: salute media.</item>
    ///   <item><b>Fertility/ClimateInfluence01</b>: pesi ecologici.</item>
    /// </list>
    /// </summary>
    public readonly struct EnvironmentVegetationSnapshot
    {
        public readonly EnvironmentAreaId AreaId;
        public readonly EnvironmentVegetationKind VegetationKind;
        public readonly float Density01;
        public readonly float GrowthPotential01;
        public readonly float Health01;
        public readonly float FertilityInfluence01;
        public readonly float ClimateInfluence01;

        // =============================================================================
        // EnvironmentVegetationSnapshot
        // =============================================================================
        /// <summary>
        /// <para>
        /// Costruisce lo snapshot vegetazione copiando un payload area.
        /// </para>
        /// </summary>
        public EnvironmentVegetationSnapshot(EnvironmentVegetationAreaState state)
        {
            AreaId = state.AreaId;
            VegetationKind = state.VegetationKind;
            Density01 = state.Density01;
            GrowthPotential01 = state.GrowthPotential01;
            Health01 = state.Health01;
            FertilityInfluence01 = state.FertilityInfluence01;
            ClimateInfluence01 = state.ClimateInfluence01;
        }
    }

    // =============================================================================
    // EnvironmentSeedBankSnapshot
    // =============================================================================
    /// <summary>
    /// <para>
    /// Snapshot read-only della seed bank diffusa di area.
    /// </para>
    ///
    /// <para><b>Principio architetturale: pressione semi senza risorse concrete</b></para>
    /// <para>
    /// La seed bank naturale resta un valore ecologico. Lo snapshot copia le entry
    /// per impedire ai consumer di trattenere collezioni interne.
    /// </para>
    ///
    /// <para><b>Struttura interna:</b></para>
    /// <list type="bullet">
    ///   <item><b>AreaId</b>: area sorgente.</item>
    ///   <item><b>Entries</b>: copia delle pressioni seme.</item>
    ///   <item><b>TotalAmount01</b>: disponibilita' aggregata.</item>
    ///   <item><b>AverageViability01</b>: vitalita' media.</item>
    /// </list>
    /// </summary>
    public sealed class EnvironmentSeedBankSnapshot
    {
        private static readonly EnvironmentSeedBankEntry[] EmptyEntries =
            new EnvironmentSeedBankEntry[0];

        public EnvironmentAreaId AreaId { get; }
        public IReadOnlyList<EnvironmentSeedBankEntry> Entries { get; }
        public float TotalAmount01 { get; }
        public float AverageViability01 { get; }

        // =============================================================================
        // EnvironmentSeedBankSnapshot
        // =============================================================================
        /// <summary>
        /// <para>
        /// Costruisce lo snapshot seed bank copiando tutte le entry.
        /// </para>
        /// </summary>
        public EnvironmentSeedBankSnapshot(EnvironmentSeedBankAreaState state)
        {
            AreaId = state == null ? EnvironmentAreaId.None : state.AreaId;
            Entries = CopyEntries(state?.Entries);
            TotalAmount01 = state == null ? 0f : state.TotalAmount01;
            AverageViability01 = state == null ? 0f : state.AverageViability01;
        }

        private static IReadOnlyList<EnvironmentSeedBankEntry> CopyEntries(
            IReadOnlyList<EnvironmentSeedBankEntry> entries)
        {
            if (entries == null || entries.Count == 0)
                return EmptyEntries;

            var copy = new EnvironmentSeedBankEntry[entries.Count];
            for (int i = 0; i < entries.Count; i++)
            {
                // Le entry sono value type, ma copiamo comunque la lista per chiudere
                // il contratto read-only del full snapshot.
                copy[i] = entries[i];
            }

            return copy;
        }
    }

    // =============================================================================
    // EnvironmentFullSnapshot
    // =============================================================================
    /// <summary>
    /// <para>
    /// Snapshot read-only aggregato e stabilizzato della biosfera.
    /// </para>
    ///
    /// <para><b>Principio architetturale: read model unico per consumer futuri</b></para>
    /// <para>
    /// Questo full snapshot raccoglie viste per dominio partendo dallo snapshot Core
    /// esistente. ArcGraph, NPC, debug e save/load potranno leggere liste gia'
    /// materializzate senza ricevere registry interni o dipendenze runtime.
    /// </para>
    ///
    /// <para><b>Struttura interna:</b></para>
    /// <list type="bullet">
    ///   <item><b>Calendar/Climate/Weather</b>: stato globale.</item>
    ///   <item><b>Areas</b>: snapshot area base.</item>
    ///   <item><b>Fertility/Water/Vegetation/SeedBank</b>: viste specializzate per layer.</item>
    ///   <item><b>Plants</b>: piante importanti read-only.</item>
    /// </list>
    /// </summary>
    public sealed class EnvironmentFullSnapshot
    {
        private static readonly EnvironmentAreaSnapshot[] EmptyAreas =
            new EnvironmentAreaSnapshot[0];
        private static readonly EnvironmentFertilitySnapshot[] EmptyFertility =
            new EnvironmentFertilitySnapshot[0];
        private static readonly EnvironmentWaterSnapshot[] EmptyWater =
            new EnvironmentWaterSnapshot[0];
        private static readonly EnvironmentVegetationSnapshot[] EmptyVegetation =
            new EnvironmentVegetationSnapshot[0];
        private static readonly EnvironmentSeedBankSnapshot[] EmptySeedBank =
            new EnvironmentSeedBankSnapshot[0];
        private static readonly EnvironmentPlantSnapshot[] EmptyPlants =
            new EnvironmentPlantSnapshot[0];

        public EnvironmentCalendarSnapshot Calendar { get; }
        public EnvironmentClimateSnapshot Climate { get; }
        public EnvironmentWeatherSnapshot Weather { get; }
        public IReadOnlyList<EnvironmentAreaSnapshot> Areas { get; }
        public IReadOnlyList<EnvironmentFertilitySnapshot> FertilityAreas { get; }
        public IReadOnlyList<EnvironmentWaterSnapshot> WaterAreas { get; }
        public IReadOnlyList<EnvironmentVegetationSnapshot> VegetationAreas { get; }
        public IReadOnlyList<EnvironmentSeedBankSnapshot> SeedBankAreas { get; }
        public IReadOnlyList<EnvironmentPlantSnapshot> Plants { get; }

        public int AreaCount => Areas.Count;
        public int PlantCount => Plants.Count;

        // =============================================================================
        // EnvironmentFullSnapshot
        // =============================================================================
        /// <summary>
        /// <para>
        /// Costruisce il full snapshot usando liste gia' materializzate dal resolver.
        /// </para>
        /// </summary>
        public EnvironmentFullSnapshot(
            EnvironmentCalendarSnapshot calendar,
            EnvironmentClimateSnapshot climate,
            IReadOnlyList<EnvironmentAreaSnapshot> areas,
            IReadOnlyList<EnvironmentFertilitySnapshot> fertilityAreas,
            IReadOnlyList<EnvironmentWaterSnapshot> waterAreas,
            IReadOnlyList<EnvironmentVegetationSnapshot> vegetationAreas,
            IReadOnlyList<EnvironmentSeedBankSnapshot> seedBankAreas,
            IReadOnlyList<EnvironmentPlantSnapshot> plants)
        {
            Calendar = calendar;
            Climate = climate;
            Weather = climate.Weather;
            Areas = areas ?? EmptyAreas;
            FertilityAreas = fertilityAreas ?? EmptyFertility;
            WaterAreas = waterAreas ?? EmptyWater;
            VegetationAreas = vegetationAreas ?? EmptyVegetation;
            SeedBankAreas = seedBankAreas ?? EmptySeedBank;
            Plants = plants ?? EmptyPlants;
        }
    }

    // =============================================================================
    // EnvironmentReadOnlySnapshotResolver
    // =============================================================================
    /// <summary>
    /// <para>
    /// Resolver delle viste read-only stabilizzate della biosfera.
    /// </para>
    ///
    /// <para><b>Principio architetturale: projection layer Core-side</b></para>
    /// <para>
    /// Il resolver prende lo snapshot Core attuale e produce liste separate per
    /// dominio. Non filtra per ArcGraph, non applica logica NPC e non salva file:
    /// prepara soltanto un read model neutrale.
    /// </para>
    ///
    /// <para><b>Struttura interna:</b></para>
    /// <list type="bullet">
    ///   <item><b>BuildFullSnapshot</b>: crea lo snapshot aggregato.</item>
    ///   <item><b>CopyAreas</b>: copia le aree base.</item>
    ///   <item><b>Build*Snapshots</b>: estrae viste specializzate.</item>
    /// </list>
    /// </summary>
    public static class EnvironmentReadOnlySnapshotResolver
    {
        // =============================================================================
        // BuildFullSnapshot
        // =============================================================================
        /// <summary>
        /// <para>
        /// Costruisce il full snapshot read-only da uno snapshot Core.
        /// </para>
        /// </summary>
        public static EnvironmentFullSnapshot BuildFullSnapshot(
            EnvironmentSnapshot snapshot)
        {
            var safeSnapshot = snapshot ?? new EnvironmentSnapshot(
                default,
                default,
                null,
                null);
            var climate = new EnvironmentClimateSnapshot(safeSnapshot.Climate);

            return new EnvironmentFullSnapshot(
                new EnvironmentCalendarSnapshot(safeSnapshot.Calendar),
                climate,
                CopyAreas(safeSnapshot.Areas),
                BuildFertilitySnapshots(safeSnapshot.Areas),
                BuildWaterSnapshots(safeSnapshot.Areas),
                BuildVegetationSnapshots(safeSnapshot.Areas),
                BuildSeedBankSnapshots(safeSnapshot.Areas),
                CopyPlants(safeSnapshot.Plants));
        }

        private static IReadOnlyList<EnvironmentAreaSnapshot> CopyAreas(
            IReadOnlyList<EnvironmentAreaSnapshot> areas)
        {
            if (areas == null || areas.Count == 0)
                return new EnvironmentAreaSnapshot[0];

            var copy = new EnvironmentAreaSnapshot[areas.Count];
            for (int i = 0; i < areas.Count; i++)
                copy[i] = areas[i];

            return copy;
        }

        private static IReadOnlyList<EnvironmentFertilitySnapshot> BuildFertilitySnapshots(
            IReadOnlyList<EnvironmentAreaSnapshot> areas)
        {
            var result = new List<EnvironmentFertilitySnapshot>();
            if (areas == null)
                return result;

            for (int i = 0; i < areas.Count; i++)
            {
                if (areas[i].HasFertility)
                    result.Add(new EnvironmentFertilitySnapshot(areas[i].FertilityState));
            }

            return result.ToArray();
        }

        private static IReadOnlyList<EnvironmentWaterSnapshot> BuildWaterSnapshots(
            IReadOnlyList<EnvironmentAreaSnapshot> areas)
        {
            var result = new List<EnvironmentWaterSnapshot>();
            if (areas == null)
                return result;

            for (int i = 0; i < areas.Count; i++)
            {
                if (areas[i].HasWater)
                    result.Add(new EnvironmentWaterSnapshot(areas[i].WaterState));
            }

            return result.ToArray();
        }

        private static IReadOnlyList<EnvironmentVegetationSnapshot> BuildVegetationSnapshots(
            IReadOnlyList<EnvironmentAreaSnapshot> areas)
        {
            var result = new List<EnvironmentVegetationSnapshot>();
            if (areas == null)
                return result;

            for (int i = 0; i < areas.Count; i++)
            {
                if (areas[i].HasVegetation)
                    result.Add(new EnvironmentVegetationSnapshot(areas[i].VegetationState));
            }

            return result.ToArray();
        }

        private static IReadOnlyList<EnvironmentSeedBankSnapshot> BuildSeedBankSnapshots(
            IReadOnlyList<EnvironmentAreaSnapshot> areas)
        {
            var result = new List<EnvironmentSeedBankSnapshot>();
            if (areas == null)
                return result;

            for (int i = 0; i < areas.Count; i++)
            {
                if (areas[i].HasSeedBank)
                    result.Add(new EnvironmentSeedBankSnapshot(areas[i].SeedBankState));
            }

            return result.ToArray();
        }

        private static IReadOnlyList<EnvironmentPlantSnapshot> CopyPlants(
            IReadOnlyList<EnvironmentPlantSnapshot> plants)
        {
            if (plants == null || plants.Count == 0)
                return new EnvironmentPlantSnapshot[0];

            var copy = new EnvironmentPlantSnapshot[plants.Count];
            for (int i = 0; i < plants.Count; i++)
                copy[i] = plants[i];

            return copy;
        }
    }
}
