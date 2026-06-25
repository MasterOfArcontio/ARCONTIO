using Arcontio.Core;
using Arcontio.Core.Environment;
using System.Collections.Generic;
using UnityEngine;

namespace Arcontio.View.ArcGraph
{
    public enum ArcUiBiosphereGraphScope
    {
        World = 0,
        BiologicalArea = 1
    }

    public enum ArcUiBiosphereGraphBucket
    {
        Days = 0,
        Months = 1
    }

    public readonly struct ArcUiBiosphereAreaOption
    {
        public readonly int AreaId;
        public readonly string Label;

        public ArcUiBiosphereAreaOption(int areaId, string label)
        {
            AreaId = areaId < 0 ? 0 : areaId;
            Label = string.IsNullOrWhiteSpace(label) ? "Area " + AreaId.ToString() : label.Trim();
        }
    }

    public readonly struct ArcUiBiosphereGraphPoint
    {
        public readonly float X;
        public readonly float Y;

        public ArcUiBiosphereGraphPoint(float x, float y)
        {
            X = x;
            Y = y;
        }
    }

    public readonly struct ArcUiBiosphereGraphSeries
    {
        public readonly string Key;
        public readonly string Label;
        public readonly Color Color;
        public readonly bool Visible;
        public readonly ArcUiBiosphereGraphPoint[] Points;

        public ArcUiBiosphereGraphSeries(
            string key,
            string label,
            Color color,
            bool visible,
            ArcUiBiosphereGraphPoint[] points)
        {
            Key = ArcUiOperationDefinition.NormalizeKey(key);
            Label = string.IsNullOrWhiteSpace(label) ? Key : label.Trim();
            Color = color;
            Visible = visible;
            Points = points ?? new ArcUiBiosphereGraphPoint[0];
        }
    }

    public readonly struct ArcUiBiosphereWeatherBucket
    {
        public readonly int BucketIndex;
        public readonly string WeatherKey;
        public readonly Color Color;
        public readonly float Count;

        public ArcUiBiosphereWeatherBucket(
            int bucketIndex,
            string weatherKey,
            Color color,
            float count)
        {
            BucketIndex = bucketIndex < 0 ? 0 : bucketIndex;
            WeatherKey = ArcUiOperationDefinition.NormalizeKey(weatherKey);
            Color = color;
            Count = count < 0f ? 0f : count;
        }
    }

    public sealed class ArcUiBiosphereGraphViewModel
    {
        private static readonly ArcUiBiosphereGraphSeries[] EmptySeries =
            new ArcUiBiosphereGraphSeries[0];
        private static readonly ArcUiBiosphereWeatherBucket[] EmptyWeather =
            new ArcUiBiosphereWeatherBucket[0];
        private static readonly ArcUiBiosphereAreaOption[] EmptyAreas =
            new ArcUiBiosphereAreaOption[0];

        public ArcUiBiosphereGraphScope Scope { get; }
        public ArcUiBiosphereGraphBucket Bucket { get; }
        public int SelectedAreaId { get; }
        public string Title { get; }
        public float MinX { get; }
        public float MaxX { get; }
        public float MinY { get; }
        public float MaxY { get; }
        public ArcUiBiosphereGraphSeries[] Series { get; }
        public ArcUiBiosphereWeatherBucket[] WeatherBuckets { get; }
        public ArcUiBiosphereAreaOption[] AreaOptions { get; }

        public bool HasData => Series.Length > 0 || WeatherBuckets.Length > 0;

        public ArcUiBiosphereGraphViewModel(
            ArcUiBiosphereGraphScope scope,
            ArcUiBiosphereGraphBucket bucket,
            int selectedAreaId,
            string title,
            float minX,
            float maxX,
            float minY,
            float maxY,
            ArcUiBiosphereGraphSeries[] series,
            ArcUiBiosphereWeatherBucket[] weatherBuckets,
            ArcUiBiosphereAreaOption[] areaOptions)
        {
            Scope = scope;
            Bucket = bucket;
            SelectedAreaId = selectedAreaId < 0 ? 0 : selectedAreaId;
            Title = string.IsNullOrWhiteSpace(title) ? "Biosfera" : title.Trim();
            MinX = minX;
            MaxX = maxX <= minX ? minX + 1f : maxX;
            MinY = minY;
            MaxY = maxY <= minY ? minY + 1f : maxY;
            Series = series ?? EmptySeries;
            WeatherBuckets = weatherBuckets ?? EmptyWeather;
            AreaOptions = areaOptions ?? EmptyAreas;
        }

        public static ArcUiBiosphereGraphViewModel Empty()
        {
            return new ArcUiBiosphereGraphViewModel(
                ArcUiBiosphereGraphScope.World,
                ArcUiBiosphereGraphBucket.Days,
                0,
                "Biosfera",
                0f,
                1f,
                0f,
                1f,
                EmptySeries,
                EmptyWeather,
                EmptyAreas);
        }
    }

    public sealed class ArcUiBiosphereRuntimeSnapshotProvider
    {
        private readonly ArcUiBiosphereGraphViewModelFactory _factory = new();
        private SimulationHost _simulationHost;

        public void SetSimulationHost(SimulationHost simulationHost)
        {
            _simulationHost = simulationHost;
        }

        public ArcUiBiosphereGraphViewModel BuildGraphViewModel(
            ArcUiBiosphereGraphScope scope,
            ArcUiBiosphereGraphBucket bucket,
            int selectedAreaId,
            ISet<string> hiddenSeriesKeys)
        {
            if (_simulationHost == null)
                return ArcUiBiosphereGraphViewModel.Empty();

            return _factory.Build(
                _simulationHost.CreateBiosphereHistorySnapshot(),
                scope,
                bucket,
                selectedAreaId,
                hiddenSeriesKeys);
        }
    }

    public sealed class ArcUiBiosphereGraphViewModelFactory
    {
        private readonly Dictionary<string, float> _monthSum = new();
        private readonly Dictionary<string, int> _monthCount = new();

        public ArcUiBiosphereGraphViewModel Build(
            EnvironmentHistorySnapshot snapshot,
            ArcUiBiosphereGraphScope scope,
            ArcUiBiosphereGraphBucket bucket,
            int selectedAreaId,
            ISet<string> hiddenSeriesKeys)
        {
            EnvironmentHistoryFrame[] frames = CopyFrames(snapshot);
            ArcUiBiosphereAreaOption[] areas = BuildAreaOptions(frames);
            int areaId = ResolveSelectedAreaId(selectedAreaId, areas);

            return scope == ArcUiBiosphereGraphScope.BiologicalArea
                ? BuildAreaGraph(frames, bucket, areaId, areas, hiddenSeriesKeys)
                : BuildWorldGraph(frames, bucket, areas, hiddenSeriesKeys);
        }

        private ArcUiBiosphereGraphViewModel BuildWorldGraph(
            EnvironmentHistoryFrame[] frames,
            ArcUiBiosphereGraphBucket bucket,
            ArcUiBiosphereAreaOption[] areas,
            ISet<string> hiddenSeriesKeys)
        {
            if (frames.Length == 0)
                return ArcUiBiosphereGraphViewModel.Empty();

            var series = bucket == ArcUiBiosphereGraphBucket.Months
                ? BuildWorldMonthlySeries(frames, hiddenSeriesKeys)
                : BuildWorldDailySeries(frames, hiddenSeriesKeys);
            var weather = IsHidden(hiddenSeriesKeys, "world_weather")
                ? new ArcUiBiosphereWeatherBucket[0]
                : bucket == ArcUiBiosphereGraphBucket.Months
                ? BuildMonthlyWeather(frames)
                : BuildDailyWeather(frames);

            ResolveBounds(series, weather, out float minX, out float maxX, out float minY, out float maxY);
            return new ArcUiBiosphereGraphViewModel(
                ArcUiBiosphereGraphScope.World,
                bucket,
                0,
                "Grafici mondo",
                minX,
                maxX,
                minY,
                maxY,
                series,
                weather,
                areas);
        }

        private ArcUiBiosphereGraphViewModel BuildAreaGraph(
            EnvironmentHistoryFrame[] frames,
            ArcUiBiosphereGraphBucket bucket,
            int areaId,
            ArcUiBiosphereAreaOption[] areas,
            ISet<string> hiddenSeriesKeys)
        {
            var series = bucket == ArcUiBiosphereGraphBucket.Months
                ? BuildAreaMonthlySeries(frames, areaId, hiddenSeriesKeys)
                : BuildAreaDailySeries(frames, areaId, hiddenSeriesKeys);

            ResolveBounds(series, new ArcUiBiosphereWeatherBucket[0], out float minX, out float maxX, out float minY, out float maxY);
            return new ArcUiBiosphereGraphViewModel(
                ArcUiBiosphereGraphScope.BiologicalArea,
                bucket,
                areaId,
                "Grafici area biologica",
                minX,
                maxX,
                minY,
                maxY,
                series,
                new ArcUiBiosphereWeatherBucket[0],
                areas);
        }

        private ArcUiBiosphereGraphSeries[] BuildWorldDailySeries(
            EnvironmentHistoryFrame[] frames,
            ISet<string> hidden)
        {
            var temp = new ArcUiBiosphereGraphPoint[frames.Length];
            var humidity = new ArcUiBiosphereGraphPoint[frames.Length];
            var tempAvg = new ArcUiBiosphereGraphPoint[frames.Length];
            var humidityAvg = new ArcUiBiosphereGraphPoint[frames.Length];

            for (int i = 0; i < frames.Length; i++)
            {
                temp[i] = new ArcUiBiosphereGraphPoint(i, frames[i].World.Temperature01);
                humidity[i] = new ArcUiBiosphereGraphPoint(i, frames[i].World.Humidity01);
                tempAvg[i] = new ArcUiBiosphereGraphPoint(i, RollingAverage(frames, i, true));
                humidityAvg[i] = new ArcUiBiosphereGraphPoint(i, RollingAverage(frames, i, false));
            }

            return new[]
            {
                Series("world_temperature", "Temperatura", ColorFromHex("#FF6247"), hidden, temp),
                Series("world_humidity", "Umidita", ColorFromHex("#35A7FF"), hidden, humidity),
                Series("world_temperature_avg", "Temp media", ColorFromHex("#FFB199"), hidden, tempAvg),
                Series("world_humidity_avg", "Umid media", ColorFromHex("#A8D8FF"), hidden, humidityAvg)
            };
        }

        private ArcUiBiosphereGraphSeries[] BuildWorldMonthlySeries(
            EnvironmentHistoryFrame[] frames,
            ISet<string> hidden)
        {
            var monthKeys = BuildMonthKeys(frames);
            var temp = new ArcUiBiosphereGraphPoint[monthKeys.Length];
            var humidity = new ArcUiBiosphereGraphPoint[monthKeys.Length];
            var tempAvg = new ArcUiBiosphereGraphPoint[monthKeys.Length];
            var humidityAvg = new ArcUiBiosphereGraphPoint[monthKeys.Length];

            for (int i = 0; i < monthKeys.Length; i++)
            {
                temp[i] = new ArcUiBiosphereGraphPoint(i, LastWorldValue(frames, monthKeys[i], true));
                humidity[i] = new ArcUiBiosphereGraphPoint(i, LastWorldValue(frames, monthKeys[i], false));
                tempAvg[i] = new ArcUiBiosphereGraphPoint(i, AverageWorldValue(frames, monthKeys[i], true));
                humidityAvg[i] = new ArcUiBiosphereGraphPoint(i, AverageWorldValue(frames, monthKeys[i], false));
            }

            return new[]
            {
                Series("world_temperature", "Temperatura", ColorFromHex("#FF6247"), hidden, temp),
                Series("world_humidity", "Umidita", ColorFromHex("#35A7FF"), hidden, humidity),
                Series("world_temperature_avg", "Temp media", ColorFromHex("#FFB199"), hidden, tempAvg),
                Series("world_humidity_avg", "Umid media", ColorFromHex("#A8D8FF"), hidden, humidityAvg)
            };
        }

        private ArcUiBiosphereGraphSeries[] BuildAreaDailySeries(
            EnvironmentHistoryFrame[] frames,
            int areaId,
            ISet<string> hidden)
        {
            var keys = CollectAreaSeriesKeys(frames, areaId);
            var result = new ArcUiBiosphereGraphSeries[keys.Count];
            for (int k = 0; k < keys.Count; k++)
            {
                string key = keys[k];
                var points = new ArcUiBiosphereGraphPoint[frames.Length];
                for (int i = 0; i < frames.Length; i++)
                    points[i] = new ArcUiBiosphereGraphPoint(i, ResolveAreaValue(frames[i], areaId, key));

                result[k] = Series(key, LabelFromSeriesKey(key), ColorForIndex(k), hidden, points);
            }

            return result;
        }

        private ArcUiBiosphereGraphSeries[] BuildAreaMonthlySeries(
            EnvironmentHistoryFrame[] frames,
            int areaId,
            ISet<string> hidden)
        {
            var keys = CollectAreaSeriesKeys(frames, areaId);
            var monthKeys = BuildMonthKeys(frames);
            var result = new ArcUiBiosphereGraphSeries[keys.Count];
            for (int k = 0; k < keys.Count; k++)
            {
                string key = keys[k];
                var points = new ArcUiBiosphereGraphPoint[monthKeys.Length];
                for (int i = 0; i < monthKeys.Length; i++)
                    points[i] = new ArcUiBiosphereGraphPoint(i, AverageAreaValue(frames, monthKeys[i], areaId, key));

                result[k] = Series(key, LabelFromSeriesKey(key), ColorForIndex(k), hidden, points);
            }

            return result;
        }

        private static ArcUiBiosphereGraphSeries Series(
            string key,
            string label,
            Color color,
            ISet<string> hidden,
            ArcUiBiosphereGraphPoint[] points)
        {
            string normalized = ArcUiOperationDefinition.NormalizeKey(key);
            bool visible = hidden == null || !hidden.Contains(normalized);
            return new ArcUiBiosphereGraphSeries(normalized, label, color, visible, points);
        }

        private static bool IsHidden(ISet<string> hidden, string key)
        {
            return hidden != null && hidden.Contains(ArcUiOperationDefinition.NormalizeKey(key));
        }

        private static EnvironmentHistoryFrame[] CopyFrames(EnvironmentHistorySnapshot snapshot)
        {
            if (snapshot == null || snapshot.Count == 0)
                return new EnvironmentHistoryFrame[0];

            var result = new EnvironmentHistoryFrame[snapshot.Count];
            for (int i = 0; i < snapshot.Count; i++)
                result[i] = snapshot.Frames[i];

            return result;
        }

        private static ArcUiBiosphereAreaOption[] BuildAreaOptions(EnvironmentHistoryFrame[] frames)
        {
            var byId = new Dictionary<int, string>();
            for (int i = 0; i < frames.Length; i++)
            {
                var areas = frames[i].Areas ?? new EnvironmentAreaHistorySample[0];
                for (int a = 0; a < areas.Length; a++)
                {
                    int id = areas[a].AreaId.Value;
                    if (id <= 0 || byId.ContainsKey(id))
                        continue;

                    byId[id] = "Area " + id.ToString() + " - " + areas[a].AreaKey;
                }
            }

            var result = new ArcUiBiosphereAreaOption[byId.Count];
            int index = 0;
            foreach (var pair in byId)
            {
                result[index] = new ArcUiBiosphereAreaOption(pair.Key, pair.Value);
                index++;
            }

            return result;
        }

        private static int ResolveSelectedAreaId(int selectedAreaId, ArcUiBiosphereAreaOption[] areas)
        {
            if (areas == null || areas.Length == 0)
                return 0;

            for (int i = 0; i < areas.Length; i++)
                if (areas[i].AreaId == selectedAreaId)
                    return selectedAreaId;

            return areas[0].AreaId;
        }

        private static List<string> CollectAreaSeriesKeys(EnvironmentHistoryFrame[] frames, int areaId)
        {
            var keys = new List<string>();
            for (int i = 0; i < frames.Length; i++)
            {
                if (!TryFindArea(frames[i], areaId, out EnvironmentAreaHistorySample area))
                    continue;

                AddKeys(keys, area.LivePlantsBySpecies, "plant_");
                AddKeys(keys, area.VegetationCellsByKind, "vegetation_");
            }

            return keys;
        }

        private static void AddKeys(List<string> keys, EnvironmentHistoryCount[] counts, string prefix)
        {
            var safeCounts = counts ?? new EnvironmentHistoryCount[0];
            for (int i = 0; i < safeCounts.Length; i++)
            {
                string key = ArcUiOperationDefinition.NormalizeKey(prefix + safeCounts[i].Key);
                if (!keys.Contains(key))
                    keys.Add(key);
            }
        }

        private static float ResolveAreaValue(EnvironmentHistoryFrame frame, int areaId, string seriesKey)
        {
            if (!TryFindArea(frame, areaId, out EnvironmentAreaHistorySample area))
                return 0f;

            if (seriesKey.StartsWith("plant_"))
                return CountValue(area.LivePlantsBySpecies, seriesKey.Substring("plant_".Length));

            if (seriesKey.StartsWith("vegetation_"))
                return CountValue(area.VegetationCellsByKind, seriesKey.Substring("vegetation_".Length));

            return 0f;
        }

        private static float CountValue(EnvironmentHistoryCount[] counts, string key)
        {
            string normalized = ArcUiOperationDefinition.NormalizeKey(key);
            var safeCounts = counts ?? new EnvironmentHistoryCount[0];
            for (int i = 0; i < safeCounts.Length; i++)
            {
                if (ArcUiOperationDefinition.NormalizeKey(safeCounts[i].Key) == normalized)
                    return safeCounts[i].Count;
            }

            return 0f;
        }

        private static bool TryFindArea(
            EnvironmentHistoryFrame frame,
            int areaId,
            out EnvironmentAreaHistorySample area)
        {
            var areas = frame.Areas ?? new EnvironmentAreaHistorySample[0];
            for (int i = 0; i < areas.Length; i++)
            {
                if (areas[i].AreaId.Value == areaId)
                {
                    area = areas[i];
                    return true;
                }
            }

            area = default;
            return false;
        }

        private static float RollingAverage(EnvironmentHistoryFrame[] frames, int index, bool temperature)
        {
            int start = index - 29;
            if (start < 0)
                start = 0;

            float sum = 0f;
            int count = 0;
            for (int i = start; i <= index; i++)
            {
                sum += temperature ? frames[i].World.Temperature01 : frames[i].World.Humidity01;
                count++;
            }

            return count == 0 ? 0f : sum / count;
        }

        private static int[] BuildMonthKeys(EnvironmentHistoryFrame[] frames)
        {
            var keys = new List<int>();
            for (int i = 0; i < frames.Length; i++)
            {
                int key = MonthKey(frames[i]);
                if (!keys.Contains(key))
                    keys.Add(key);
            }

            return keys.ToArray();
        }

        private static float LastWorldValue(EnvironmentHistoryFrame[] frames, int monthKey, bool temperature)
        {
            float value = 0f;
            for (int i = 0; i < frames.Length; i++)
            {
                if (MonthKey(frames[i]) == monthKey)
                    value = temperature ? frames[i].World.Temperature01 : frames[i].World.Humidity01;
            }

            return value;
        }

        private static float AverageWorldValue(EnvironmentHistoryFrame[] frames, int monthKey, bool temperature)
        {
            float sum = 0f;
            int count = 0;
            for (int i = 0; i < frames.Length; i++)
            {
                if (MonthKey(frames[i]) != monthKey)
                    continue;

                sum += temperature ? frames[i].World.Temperature01 : frames[i].World.Humidity01;
                count++;
            }

            return count == 0 ? 0f : sum / count;
        }

        private static float AverageAreaValue(
            EnvironmentHistoryFrame[] frames,
            int monthKey,
            int areaId,
            string seriesKey)
        {
            float sum = 0f;
            int count = 0;
            for (int i = 0; i < frames.Length; i++)
            {
                if (MonthKey(frames[i]) != monthKey)
                    continue;

                sum += ResolveAreaValue(frames[i], areaId, seriesKey);
                count++;
            }

            return count == 0 ? 0f : sum / count;
        }

        private static ArcUiBiosphereWeatherBucket[] BuildDailyWeather(EnvironmentHistoryFrame[] frames)
        {
            var result = new ArcUiBiosphereWeatherBucket[frames.Length];
            for (int i = 0; i < frames.Length; i++)
            {
                EnvironmentWeatherKind weather = frames[i].World.WeatherKind;
                result[i] = new ArcUiBiosphereWeatherBucket(
                    i,
                    weather.ToString(),
                    WeatherColor(weather),
                    1f);
            }

            return result;
        }

        private ArcUiBiosphereWeatherBucket[] BuildMonthlyWeather(EnvironmentHistoryFrame[] frames)
        {
            var monthKeys = BuildMonthKeys(frames);
            var result = new List<ArcUiBiosphereWeatherBucket>();

            for (int m = 0; m < monthKeys.Length; m++)
            {
                _monthSum.Clear();
                _monthCount.Clear();
                for (int i = 0; i < frames.Length; i++)
                {
                    if (MonthKey(frames[i]) != monthKeys[m])
                        continue;

                    string weatherKey = frames[i].World.WeatherKind.ToString();
                    _monthCount[weatherKey] = _monthCount.TryGetValue(weatherKey, out int count) ? count + 1 : 1;
                }

                foreach (var pair in _monthCount)
                {
                    result.Add(new ArcUiBiosphereWeatherBucket(
                        m,
                        pair.Key,
                        WeatherColor(ParseWeather(pair.Key)),
                        pair.Value));
                }
            }

            return result.ToArray();
        }

        private static int MonthKey(EnvironmentHistoryFrame frame)
        {
            return (frame.World.Year * 12) + frame.World.Month;
        }

        private static void ResolveBounds(
            ArcUiBiosphereGraphSeries[] series,
            ArcUiBiosphereWeatherBucket[] weather,
            out float minX,
            out float maxX,
            out float minY,
            out float maxY)
        {
            minX = 0f;
            maxX = 1f;
            minY = 0f;
            maxY = 1f;
            bool hasValue = false;

            var safeSeries = series ?? new ArcUiBiosphereGraphSeries[0];
            for (int i = 0; i < safeSeries.Length; i++)
            {
                if (!safeSeries[i].Visible)
                    continue;

                var points = safeSeries[i].Points ?? new ArcUiBiosphereGraphPoint[0];
                for (int p = 0; p < points.Length; p++)
                {
                    if (!hasValue)
                    {
                        minX = maxX = points[p].X;
                        minY = maxY = points[p].Y;
                        hasValue = true;
                    }
                    else
                    {
                        if (points[p].X < minX) minX = points[p].X;
                        if (points[p].X > maxX) maxX = points[p].X;
                        if (points[p].Y < minY) minY = points[p].Y;
                        if (points[p].Y > maxY) maxY = points[p].Y;
                    }
                }
            }

            var safeWeather = weather ?? new ArcUiBiosphereWeatherBucket[0];
            for (int i = 0; i < safeWeather.Length; i++)
            {
                if (safeWeather[i].BucketIndex > maxX)
                    maxX = safeWeather[i].BucketIndex;
                if (safeWeather[i].Count > maxY)
                    maxY = safeWeather[i].Count;
            }

            float padding = (maxY - minY) * 0.08f;
            if (padding <= 0.01f)
                padding = 0.05f;

            minY -= padding;
            maxY += padding;
            if (minY > 0f && minY < 0.05f)
                minY = 0f;
        }

        private static string LabelFromSeriesKey(string key)
        {
            if (key.StartsWith("plant_"))
                return key.Substring("plant_".Length) + " vive";

            if (key.StartsWith("vegetation_"))
                return key.Substring("vegetation_".Length) + " vegetazione";

            return key;
        }

        private static EnvironmentWeatherKind ParseWeather(string key)
        {
            if (System.Enum.TryParse(key, out EnvironmentWeatherKind weather))
                return weather;

            return EnvironmentWeatherKind.Clear;
        }

        private static Color WeatherColor(EnvironmentWeatherKind weather)
        {
            return weather switch
            {
                EnvironmentWeatherKind.Rain => ColorFromHex("#3D8BFF"),
                EnvironmentWeatherKind.Snow => ColorFromHex("#D8F3FF"),
                EnvironmentWeatherKind.Wind => ColorFromHex("#9AA7B2"),
                EnvironmentWeatherKind.HeatWave => ColorFromHex("#FF8A3D"),
                EnvironmentWeatherKind.Storm => ColorFromHex("#7D5FFF"),
                _ => ColorFromHex("#D8C85A")
            };
        }

        private static Color ColorForIndex(int index)
        {
            Color[] palette =
            {
                ColorFromHex("#35D07F"),
                ColorFromHex("#FFB84A"),
                ColorFromHex("#43B5FF"),
                ColorFromHex("#D375FF"),
                ColorFromHex("#FF6B6B"),
                ColorFromHex("#A6E36D")
            };

            return palette[index % palette.Length];
        }

        private static Color ColorFromHex(string hex)
        {
            return ColorUtility.TryParseHtmlString(hex, out Color color) ? color : Color.white;
        }
    }
}
