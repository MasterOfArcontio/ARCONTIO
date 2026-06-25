using System.Collections.Generic;

namespace Arcontio.Core.Environment
{
    // =============================================================================
    // EnvironmentRuntimeEventKind
    // =============================================================================
    /// <summary>
    /// <para>
    /// Tipo compatto dell'evento runtime prodotto dal boundary Biosfera -> sistemi.
    /// </para>
    ///
    /// <para><b>Principio architetturale: evento come notifica, non come stato</b></para>
    /// <para>
    /// Il kind permette ai consumer di distinguere bootstrap, load e aggiornamento
    /// giornaliero senza leggere direttamente <c>EnvironmentState</c>. I dati
    /// ambientali completi restano nella Biosfera/World, mentre l'evento comunica
    /// solo che un nuovo stato osservabile e' disponibile.
    /// </para>
    /// </summary>
    public enum EnvironmentRuntimeEventKind
    {
        Bootstrap = 0,
        Loaded = 1,
        DailyUpdate = 2,
        DebugFastForwardDailyUpdate = 3
    }

    // =============================================================================
    // EnvironmentRuntimeEvent
    // =============================================================================
    /// <summary>
    /// <para>
    /// Evento read-only compatto pubblicato quando la Biosfera rende disponibile un
    /// nuovo stato osservabile.
    /// </para>
    ///
    /// <para><b>Boundary Biosfera -> UI/sistemi</b></para>
    /// <para>
    /// Questo DTO non contiene riferimenti mutabili a <c>EnvironmentState</c>,
    /// <c>World</c>, renderer o oggetti Unity. Serve a notificare UI, debug,
    /// telemetria e futuri sistemi listener che clima, calendario, piante fisiche o
    /// vegetazione diffusa possono essere cambiati.
    /// </para>
    ///
    /// <para><b>Struttura interna:</b></para>
    /// <list type="bullet">
    ///   <item><b>Tempo</b>: tick ambiente e data leggibile.</item>
    ///   <item><b>Clima</b>: temperatura, umidita', stagione e meteo gia' risolti.</item>
    ///   <item><b>Delta</b>: conteggi di piante/vegetazione prodotti e applicati.</item>
    ///   <item><b>Diagnostica</b>: aree visitate e aree cambiate nel batch.</item>
    /// </list>
    /// </summary>
    public readonly struct EnvironmentRuntimeEvent
    {
        public readonly EnvironmentRuntimeEventKind Kind;
        public readonly long EnvironmentTick;
        public readonly int AbsoluteDay;
        public readonly int Year;
        public readonly int Month;
        public readonly int DayOfMonth;
        public readonly int DayOfYear;
        public readonly EnvironmentSeasonKind Season;
        public readonly float Temperature01;
        public readonly float Humidity01;
        public readonly EnvironmentWeatherKind WeatherKind;
        public readonly int AreasVisited;
        public readonly int ChangedAreas;
        public readonly int PhysicalPlantDeltaCount;
        public readonly int AppliedPhysicalPlantDeltaCount;
        public readonly int DiffuseVegetationDeltaCount;
        public readonly int AppliedDiffuseVegetationDeltaCount;

        // =============================================================================
        // EnvironmentRuntimeEvent
        // =============================================================================
        /// <summary>
        /// <para>
        /// Costruisce l'evento normalizzando i conteggi negativi e mantenendo i valori
        /// climatici nel range ambientale standard 0-1.
        /// </para>
        /// </summary>
        public EnvironmentRuntimeEvent(
            EnvironmentRuntimeEventKind kind,
            long environmentTick,
            int absoluteDay,
            int year,
            int month,
            int dayOfMonth,
            int dayOfYear,
            EnvironmentSeasonKind season,
            float temperature01,
            float humidity01,
            EnvironmentWeatherKind weatherKind,
            int areasVisited,
            int changedAreas,
            int physicalPlantDeltaCount,
            int appliedPhysicalPlantDeltaCount,
            int diffuseVegetationDeltaCount,
            int appliedDiffuseVegetationDeltaCount)
        {
            Kind = kind;
            EnvironmentTick = environmentTick < 0 ? 0 : environmentTick;
            AbsoluteDay = absoluteDay < 0 ? 0 : absoluteDay;
            Year = year < 0 ? 0 : year;
            Month = month < 0 ? 0 : month;
            DayOfMonth = dayOfMonth < 0 ? 0 : dayOfMonth;
            DayOfYear = dayOfYear < 0 ? 0 : dayOfYear;
            Season = season;
            Temperature01 = EnvironmentMath.Clamp01(temperature01);
            Humidity01 = EnvironmentMath.Clamp01(humidity01);
            WeatherKind = weatherKind;
            AreasVisited = areasVisited < 0 ? 0 : areasVisited;
            ChangedAreas = changedAreas < 0 ? 0 : changedAreas;
            PhysicalPlantDeltaCount = physicalPlantDeltaCount < 0 ? 0 : physicalPlantDeltaCount;
            AppliedPhysicalPlantDeltaCount = appliedPhysicalPlantDeltaCount < 0 ? 0 : appliedPhysicalPlantDeltaCount;
            DiffuseVegetationDeltaCount = diffuseVegetationDeltaCount < 0 ? 0 : diffuseVegetationDeltaCount;
            AppliedDiffuseVegetationDeltaCount = appliedDiffuseVegetationDeltaCount < 0 ? 0 : appliedDiffuseVegetationDeltaCount;
        }

        // =============================================================================
        // FromState
        // =============================================================================
        /// <summary>
        /// <para>
        /// Crea un evento da uno stato ambientale gia' avanzato, senza conservarne il
        /// riferimento. Viene usato per bootstrap e load.
        /// </para>
        /// </summary>
        public static EnvironmentRuntimeEvent FromState(
            EnvironmentRuntimeEventKind kind,
            EnvironmentState state)
        {
            EnvironmentSnapshot snapshot = state != null
                ? state.CreateSnapshot()
                : new EnvironmentState().CreateSnapshot();
            return FromSnapshot(
                kind,
                snapshot,
                new EnvironmentSnapshotEvolutionReport(0, 0, 0, 0, 0),
                0,
                0,
                0,
                0);
        }

        // =============================================================================
        // FromAdvanceResult
        // =============================================================================
        /// <summary>
        /// <para>
        /// Crea un evento dal risultato di avanzamento giornaliero, includendo i conteggi
        /// dei delta prodotti dalla Biosfera e quelli applicati al World.
        /// </para>
        /// </summary>
        public static EnvironmentRuntimeEvent FromAdvanceResult(
            EnvironmentRuntimeEventKind kind,
            EnvironmentAdvanceResult result,
            int appliedPlantDeltas,
            int appliedVegetationDeltas)
        {
            EnvironmentSnapshot snapshot = result != null
                ? result.Snapshot
                : new EnvironmentState().CreateSnapshot();
            EnvironmentSnapshotEvolutionReport report = result != null
                ? result.EvolutionReport
                : new EnvironmentSnapshotEvolutionReport(0, 0, 0, 0, 0);
            int plantDeltaCount = result?.PhysicalPlantDeltas?.Count ?? 0;
            int vegetationDeltaCount = result?.DiffuseVegetationDeltas?.Count ?? 0;
            return FromSnapshot(
                kind,
                snapshot,
                report,
                plantDeltaCount,
                appliedPlantDeltas,
                vegetationDeltaCount,
                appliedVegetationDeltas);
        }

        private static EnvironmentRuntimeEvent FromSnapshot(
            EnvironmentRuntimeEventKind kind,
            EnvironmentSnapshot snapshot,
            EnvironmentSnapshotEvolutionReport report,
            int physicalPlantDeltaCount,
            int appliedPhysicalPlantDeltaCount,
            int diffuseVegetationDeltaCount,
            int appliedDiffuseVegetationDeltaCount)
        {
            EnvironmentCalendarState calendar = snapshot.Calendar;
            EnvironmentDate date = calendar.Date;
            EnvironmentGlobalClimateState climate = snapshot.Climate;
            return new EnvironmentRuntimeEvent(
                kind,
                calendar.ElapsedEnvironmentTicks,
                ResolveAbsoluteDay(date),
                date.Year,
                date.Month,
                date.DayOfMonth,
                date.DayOfYear,
                date.Season,
                climate.Temperature01,
                climate.Humidity01,
                climate.Weather.Kind,
                report.AreasVisited,
                report.ChangedAreas,
                physicalPlantDeltaCount,
                appliedPhysicalPlantDeltaCount,
                diffuseVegetationDeltaCount,
                appliedDiffuseVegetationDeltaCount);
        }

        private static int ResolveAbsoluteDay(EnvironmentDate date)
        {
            int yearIndex = date.Year <= 0 ? 0 : date.Year - 1;
            int dayOfYearIndex = date.DayOfYear <= 0 ? 0 : date.DayOfYear - 1;
            return yearIndex * 365 + dayOfYearIndex;
        }
    }

    // =============================================================================
    // EnvironmentHistoryCount
    // =============================================================================
    /// <summary>
    /// <para>
    /// Conteggio compatto usato dallo storico Biosfera per serie raggruppate.
    /// </para>
    ///
    /// <para><b>Principio architetturale: storico come read model leggero</b></para>
    /// <para>
    /// Lo storico non conserva oggetti vivi, riferimenti a World o dati renderer.
    /// Ogni voce e' solo una chiave semantica e un conteggio gia' materializzato.
    /// </para>
    /// </summary>
    public readonly struct EnvironmentHistoryCount
    {
        public readonly string Key;
        public readonly int Count;

        public EnvironmentHistoryCount(string key, int count)
        {
            Key = string.IsNullOrWhiteSpace(key) ? string.Empty : key.Trim();
            Count = count < 0 ? 0 : count;
        }
    }

    // =============================================================================
    // EnvironmentWorldHistorySample
    // =============================================================================
    /// <summary>
    /// <para>
    /// Campione giornaliero globale della Biosfera.
    /// </para>
    ///
    /// <para><b>Contratto dati per grafici mondo</b></para>
    /// <para>
    /// Il campione conserva solo calendario, clima e meteo gia' risolti. Medie,
    /// aggregazioni mensili e scala grafica vengono calcolate nei read model UI.
    /// </para>
    /// </summary>
    public readonly struct EnvironmentWorldHistorySample
    {
        public readonly int AbsoluteDay;
        public readonly int Year;
        public readonly int Month;
        public readonly int DayOfMonth;
        public readonly int DayOfYear;
        public readonly EnvironmentSeasonKind Season;
        public readonly float Temperature01;
        public readonly float Humidity01;
        public readonly EnvironmentWeatherKind WeatherKind;

        public EnvironmentWorldHistorySample(
            int absoluteDay,
            int year,
            int month,
            int dayOfMonth,
            int dayOfYear,
            EnvironmentSeasonKind season,
            float temperature01,
            float humidity01,
            EnvironmentWeatherKind weatherKind)
        {
            AbsoluteDay = absoluteDay < 0 ? 0 : absoluteDay;
            Year = year < 0 ? 0 : year;
            Month = month < 0 ? 0 : month;
            DayOfMonth = dayOfMonth < 0 ? 0 : dayOfMonth;
            DayOfYear = dayOfYear < 0 ? 0 : dayOfYear;
            Season = season;
            Temperature01 = EnvironmentMath.Clamp01(temperature01);
            Humidity01 = EnvironmentMath.Clamp01(humidity01);
            WeatherKind = weatherKind;
        }
    }

    // =============================================================================
    // EnvironmentAreaHistorySample
    // =============================================================================
    /// <summary>
    /// <para>
    /// Campione giornaliero per una singola area biologica.
    /// </para>
    ///
    /// <para><b>Biosfera -> UI senza accesso a EnvironmentState mutabile</b></para>
    /// <para>
    /// Il campione contiene conteggi gia' raggruppati per specie e tipo di
    /// vegetazione. La UI puo' disegnare grafici senza contare PlantInstance o
    /// placement ogni frame.
    /// </para>
    /// </summary>
    public readonly struct EnvironmentAreaHistorySample
    {
        public readonly int AbsoluteDay;
        public readonly EnvironmentAreaId AreaId;
        public readonly string AreaKey;
        public readonly EnvironmentHistoryCount[] LivePlantsBySpecies;
        public readonly EnvironmentHistoryCount[] VegetationCellsByKind;

        public EnvironmentAreaHistorySample(
            int absoluteDay,
            EnvironmentAreaId areaId,
            string areaKey,
            EnvironmentHistoryCount[] livePlantsBySpecies,
            EnvironmentHistoryCount[] vegetationCellsByKind)
        {
            AbsoluteDay = absoluteDay < 0 ? 0 : absoluteDay;
            AreaId = areaId;
            AreaKey = string.IsNullOrWhiteSpace(areaKey) ? areaId.ToString() : areaKey.Trim();
            LivePlantsBySpecies = livePlantsBySpecies ?? new EnvironmentHistoryCount[0];
            VegetationCellsByKind = vegetationCellsByKind ?? new EnvironmentHistoryCount[0];
        }
    }

    // =============================================================================
    // EnvironmentHistoryFrame
    // =============================================================================
    /// <summary>
    /// <para>
    /// Campione storico completo per un giorno ambientale.
    /// </para>
    /// </summary>
    public readonly struct EnvironmentHistoryFrame
    {
        public readonly EnvironmentWorldHistorySample World;
        public readonly EnvironmentAreaHistorySample[] Areas;

        public EnvironmentHistoryFrame(
            EnvironmentWorldHistorySample world,
            EnvironmentAreaHistorySample[] areas)
        {
            World = world;
            Areas = areas ?? new EnvironmentAreaHistorySample[0];
        }
    }

    // =============================================================================
    // EnvironmentHistorySnapshot
    // =============================================================================
    /// <summary>
    /// <para>
    /// Snapshot read-only dello storico Biosfera.
    /// </para>
    /// </summary>
    public sealed class EnvironmentHistorySnapshot
    {
        private static readonly EnvironmentHistoryFrame[] EmptyFrames = new EnvironmentHistoryFrame[0];

        public IReadOnlyList<EnvironmentHistoryFrame> Frames { get; }
        public int Count => Frames.Count;

        public EnvironmentHistorySnapshot(IReadOnlyList<EnvironmentHistoryFrame> frames)
        {
            Frames = frames ?? EmptyFrames;
        }
    }

    // =============================================================================
    // EnvironmentHistoryBuffer
    // =============================================================================
    /// <summary>
    /// <para>
    /// Ring buffer leggero per lo storico giornaliero della Biosfera.
    /// </para>
    ///
    /// <para><b>Principio architetturale: campionamento osservabile, non simulazione</b></para>
    /// <para>
    /// Il buffer non fa evolvere clima, piante o vegetazione. Riceve uno stato gia'
    /// prodotto dai resolver e ne cattura un read model compatto per grafici debug.
    /// Se nello stesso giorno arriva un nuovo campione, sostituisce l'ultimo: cosi'
    /// il fast-forward non produce duplicati intra-day.
    /// </para>
    /// </summary>
    public sealed class EnvironmentHistoryBuffer
    {
        private const int DefaultMaxFrames = 4096;

        private readonly List<EnvironmentHistoryFrame> _frames = new();
        private readonly int _maxFrames;

        public int Count => _frames.Count;

        public EnvironmentHistoryBuffer(int maxFrames = DefaultMaxFrames)
        {
            _maxFrames = maxFrames < 128 ? 128 : maxFrames;
        }

        public void Clear()
        {
            _frames.Clear();
        }

        public bool Capture(EnvironmentState state)
        {
            if (state == null)
                return false;

            EnvironmentSnapshot snapshot = state.CreateSnapshot();
            int absoluteDay = ResolveAbsoluteDay(snapshot.Calendar.Date);
            var frame = new EnvironmentHistoryFrame(
                BuildWorldSample(snapshot, absoluteDay),
                BuildAreaSamples(snapshot, state, absoluteDay));

            if (_frames.Count > 0 && _frames[_frames.Count - 1].World.AbsoluteDay == absoluteDay)
            {
                _frames[_frames.Count - 1] = frame;
                return true;
            }

            _frames.Add(frame);
            while (_frames.Count > _maxFrames)
                _frames.RemoveAt(0);

            return true;
        }

        public EnvironmentHistorySnapshot CreateSnapshot()
        {
            var copy = new EnvironmentHistoryFrame[_frames.Count];
            for (int i = 0; i < _frames.Count; i++)
                copy[i] = _frames[i];

            return new EnvironmentHistorySnapshot(copy);
        }

        private static EnvironmentWorldHistorySample BuildWorldSample(
            EnvironmentSnapshot snapshot,
            int absoluteDay)
        {
            EnvironmentDate date = snapshot.Calendar.Date;
            EnvironmentGlobalClimateState climate = snapshot.Climate;
            return new EnvironmentWorldHistorySample(
                absoluteDay,
                date.Year,
                date.Month,
                date.DayOfMonth,
                date.DayOfYear,
                date.Season,
                climate.Temperature01,
                climate.Humidity01,
                climate.Weather.Kind);
        }

        private static EnvironmentAreaHistorySample[] BuildAreaSamples(
            EnvironmentSnapshot snapshot,
            EnvironmentState state,
            int absoluteDay)
        {
            var areas = snapshot.Areas ?? new EnvironmentAreaSnapshot[0];
            var result = new EnvironmentAreaHistorySample[areas.Count];

            for (int i = 0; i < areas.Count; i++)
            {
                EnvironmentAreaSnapshot area = areas[i];
                result[i] = new EnvironmentAreaHistorySample(
                    absoluteDay,
                    area.Definition.AreaId,
                    area.Definition.Key,
                    CountLivePlantsBySpecies(snapshot.Plants, area.Definition.AreaId),
                    CountVegetationByKind(state.VegetationCellPlacements, area.Definition.AreaId));
            }

            return result;
        }

        private static EnvironmentHistoryCount[] CountLivePlantsBySpecies(
            IReadOnlyList<EnvironmentPlantSnapshot> plants,
            EnvironmentAreaId areaId)
        {
            var counts = new Dictionary<string, int>();
            var safePlants = plants ?? new EnvironmentPlantSnapshot[0];
            for (int i = 0; i < safePlants.Count; i++)
            {
                EnvironmentPlantSnapshot plant = safePlants[i];
                if (!plant.IsAlive || !plant.SourceAreaId.Equals(areaId))
                    continue;

                string key = string.IsNullOrWhiteSpace(plant.SpeciesKey) ? "unknown" : plant.SpeciesKey;
                counts[key] = counts.TryGetValue(key, out int value) ? value + 1 : 1;
            }

            return ToCounts(counts);
        }

        private static EnvironmentHistoryCount[] CountVegetationByKind(
            IReadOnlyList<EnvironmentVegetationCellPlacement> placements,
            EnvironmentAreaId areaId)
        {
            var counts = new Dictionary<string, int>();
            var safePlacements = placements ?? new EnvironmentVegetationCellPlacement[0];
            for (int i = 0; i < safePlacements.Count; i++)
            {
                EnvironmentVegetationCellPlacement placement = safePlacements[i];
                if (!placement.AreaId.Equals(areaId) || placement.VegetationKind == EnvironmentVegetationKind.None)
                    continue;

                string key = placement.VegetationKind.ToString();
                counts[key] = counts.TryGetValue(key, out int value) ? value + 1 : 1;
            }

            return ToCounts(counts);
        }

        private static EnvironmentHistoryCount[] ToCounts(Dictionary<string, int> counts)
        {
            if (counts == null || counts.Count == 0)
                return new EnvironmentHistoryCount[0];

            var result = new EnvironmentHistoryCount[counts.Count];
            int index = 0;
            foreach (var pair in counts)
            {
                result[index] = new EnvironmentHistoryCount(pair.Key, pair.Value);
                index++;
            }

            return result;
        }

        private static int ResolveAbsoluteDay(EnvironmentDate date)
        {
            return (date.Year * 366) + date.DayOfYear;
        }
    }
}
