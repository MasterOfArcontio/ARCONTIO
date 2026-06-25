using System.Collections.Generic;

namespace Arcontio.Core.Environment
{
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
