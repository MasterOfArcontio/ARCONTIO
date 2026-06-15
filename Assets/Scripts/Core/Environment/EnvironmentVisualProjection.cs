using System.Collections.Generic;

namespace Arcontio.Core.Environment
{
    // =============================================================================
    // EnvironmentVisualProjectionLayer
    // =============================================================================
    /// <summary>
    /// <para>
    /// Layer visuale neutrale derivabile dalla biosfera Core.
    /// </para>
    ///
    /// <para><b>Principio architetturale: adapter futuro senza dipendenza Core -> View</b></para>
    /// <para>
    /// Questi layer descrivono cosa un consumer visuale potra' ricevere, ma non sono
    /// tipi ArcGraph, non caricano sprite e non creano renderer. Il Core continua a
    /// produrre dati; la View decidera' in futuro come rappresentarli.
    /// </para>
    ///
    /// <para><b>Struttura interna:</b></para>
    /// <list type="bullet">
    ///   <item><b>Water</b>: acqua stabile o stagionale.</item>
    ///   <item><b>Vegetation</b>: vegetazione diffusa e piante importanti.</item>
    ///   <item><b>Weather</b>: meteo globale osservabile.</item>
    ///   <item><b>Light</b>: luce globale giorno/notte.</item>
    ///   <item><b>Effect</b>: effetti ambientali futuri, non ancora popolati.</item>
    /// </list>
    /// </summary>
    public enum EnvironmentVisualProjectionLayer
    {
        Water = 0,
        Vegetation = 10,
        Weather = 20,
        Light = 30,
        Effect = 40
    }

    // =============================================================================
    // EnvironmentVisualProjectionScope
    // =============================================================================
    /// <summary>
    /// <para>
    /// Ambito spaziale di una proiezione visuale neutrale.
    /// </para>
    ///
    /// <para><b>Principio architetturale: separare overlay globale e celle locali</b></para>
    /// <para>
    /// Meteo e luce possono essere globali, mentre acqua, vegetazione e piante sono
    /// area/cella. Lo scope permette a un adapter futuro di scegliere pipeline e LOD
    /// senza interrogare nuovamente la biosfera.
    /// </para>
    ///
    /// <para><b>Struttura interna:</b></para>
    /// <list type="bullet">
    ///   <item><b>Global</b>: dato valido per tutta la vista.</item>
    ///   <item><b>Area</b>: dato associato a bounds ambientali.</item>
    ///   <item><b>Cell</b>: dato puntuale su cella x/y/z.</item>
    /// </list>
    /// </summary>
    public enum EnvironmentVisualProjectionScope
    {
        Global = 0,
        Area = 10,
        Cell = 20
    }

    // =============================================================================
    // EnvironmentVisualProjectionRecord
    // =============================================================================
    /// <summary>
    /// <para>
    /// Record visuale neutrale derivato dallo stato ambientale.
    /// </para>
    ///
    /// <para><b>Principio architetturale: payload visuale senza renderer</b></para>
    /// <para>
    /// Il record contiene chiavi e valori leggibili da un futuro adapter, ma non
    /// contiene Sprite, GameObject, materiali, sorting Unity o riferimenti ArcGraph.
    /// La chiave visuale e' solo un suggerimento semantico Core-side.
    /// </para>
    ///
    /// <para><b>Struttura interna:</b></para>
    /// <list type="bullet">
    ///   <item><b>Layer/Scope</b>: destinazione concettuale.</item>
    ///   <item><b>AreaId</b>: area sorgente se disponibile.</item>
    ///   <item><b>Bounds/Cell</b>: coordinate discrete di area o cella.</item>
    ///   <item><b>VisualKey</b>: chiave semantica derivata, non asset path.</item>
    ///   <item><b>Intensity01</b>: intensita' o densita' normalizzata.</item>
    ///   <item><b>IsAnimatedCandidate</b>: true se un adapter potra' animare il dato.</item>
    ///   <item><b>IsVisible</b>: gate read-only per consumer futuri.</item>
    /// </list>
    /// </summary>
    public readonly struct EnvironmentVisualProjectionRecord
    {
        public readonly EnvironmentVisualProjectionLayer Layer;
        public readonly EnvironmentVisualProjectionScope Scope;
        public readonly EnvironmentAreaId AreaId;
        public readonly EnvironmentAreaBounds Bounds;
        public readonly EnvironmentCellCoord Cell;
        public readonly string VisualKey;
        public readonly float Intensity01;
        public readonly bool IsAnimatedCandidate;
        public readonly bool IsVisible;

        // =============================================================================
        // EnvironmentVisualProjectionRecord
        // =============================================================================
        /// <summary>
        /// <para>
        /// Costruisce un record visuale neutrale normalizzando chiave e intensita'.
        /// </para>
        /// </summary>
        public EnvironmentVisualProjectionRecord(
            EnvironmentVisualProjectionLayer layer,
            EnvironmentVisualProjectionScope scope,
            EnvironmentAreaId areaId,
            EnvironmentAreaBounds bounds,
            EnvironmentCellCoord cell,
            string visualKey,
            float intensity01,
            bool isAnimatedCandidate,
            bool isVisible)
        {
            Layer = layer;
            Scope = scope;
            AreaId = areaId;
            Bounds = bounds;
            Cell = cell;
            VisualKey = string.IsNullOrWhiteSpace(visualKey)
                ? "environment"
                : visualKey;
            Intensity01 = EnvironmentMath.Clamp01(intensity01);
            IsAnimatedCandidate = isAnimatedCandidate;
            IsVisible = isVisible;
        }
    }

    // =============================================================================
    // EnvironmentVisualProjectionSet
    // =============================================================================
    /// <summary>
    /// <para>
    /// Set read-only di proiezioni visuali neutrali della biosfera.
    /// </para>
    ///
    /// <para><b>Principio architetturale: contratto consumer prima dell'adapter reale</b></para>
    /// <para>
    /// Il set aggrega record gia' materializzati. Un adapter futuro potra' convertirli
    /// in tipi View senza tornare a leggere registry mutabili o sistemi Core.
    /// </para>
    ///
    /// <para><b>Struttura interna:</b></para>
    /// <list type="bullet">
    ///   <item><b>Records</b>: lista totale delle proiezioni.</item>
    ///   <item><b>*Count</b>: conteggi per layer principali.</item>
    ///   <item><b>ContainsLayer</b>: controllo presenza layer.</item>
    /// </list>
    /// </summary>
    public sealed class EnvironmentVisualProjectionSet
    {
        private static readonly EnvironmentVisualProjectionRecord[] EmptyRecords =
            new EnvironmentVisualProjectionRecord[0];

        public IReadOnlyList<EnvironmentVisualProjectionRecord> Records { get; }
        public int RecordCount => Records.Count;
        public int WaterCount { get; }
        public int VegetationCount { get; }
        public int WeatherCount { get; }
        public int LightCount { get; }
        public int EffectCount { get; }

        // =============================================================================
        // EnvironmentVisualProjectionSet
        // =============================================================================
        /// <summary>
        /// <para>
        /// Costruisce il set copiando i record e calcolando i conteggi per layer.
        /// </para>
        /// </summary>
        public EnvironmentVisualProjectionSet(
            IReadOnlyList<EnvironmentVisualProjectionRecord> records)
        {
            if (records == null || records.Count == 0)
            {
                Records = EmptyRecords;
                return;
            }

            var copy = new EnvironmentVisualProjectionRecord[records.Count];
            int water = 0;
            int vegetation = 0;
            int weather = 0;
            int light = 0;
            int effect = 0;

            for (int i = 0; i < records.Count; i++)
            {
                copy[i] = records[i];
                if (copy[i].Layer == EnvironmentVisualProjectionLayer.Water)
                    water++;
                else if (copy[i].Layer == EnvironmentVisualProjectionLayer.Vegetation)
                    vegetation++;
                else if (copy[i].Layer == EnvironmentVisualProjectionLayer.Weather)
                    weather++;
                else if (copy[i].Layer == EnvironmentVisualProjectionLayer.Light)
                    light++;
                else if (copy[i].Layer == EnvironmentVisualProjectionLayer.Effect)
                    effect++;
            }

            Records = copy;
            WaterCount = water;
            VegetationCount = vegetation;
            WeatherCount = weather;
            LightCount = light;
            EffectCount = effect;
        }

        // =============================================================================
        // ContainsLayer
        // =============================================================================
        /// <summary>
        /// <para>
        /// Indica se il set contiene almeno una proiezione del layer richiesto.
        /// </para>
        /// </summary>
        public bool ContainsLayer(EnvironmentVisualProjectionLayer layer)
        {
            if (layer == EnvironmentVisualProjectionLayer.Water)
                return WaterCount > 0;

            if (layer == EnvironmentVisualProjectionLayer.Vegetation)
                return VegetationCount > 0;

            if (layer == EnvironmentVisualProjectionLayer.Weather)
                return WeatherCount > 0;

            if (layer == EnvironmentVisualProjectionLayer.Light)
                return LightCount > 0;

            return EffectCount > 0;
        }
    }

    // =============================================================================
    // EnvironmentVisualProjectionResolver
    // =============================================================================
    /// <summary>
    /// <para>
    /// Resolver Core-side per proiezioni visuali neutrali.
    /// </para>
    ///
    /// <para><b>Principio architetturale: v0.52 readiness senza ponte ArcGraph</b></para>
    /// <para>
    /// Il resolver legge <see cref="EnvironmentFullSnapshot"/> e produce record
    /// convertibili da un adapter futuro. Non importa namespace View, non usa tipi
    /// ArcGraph, non carica asset e non crea rendering.
    /// </para>
    ///
    /// <para><b>Struttura interna:</b></para>
    /// <list type="bullet">
    ///   <item><b>BuildProjectionSet</b>: crea il set completo da full snapshot.</item>
    ///   <item><b>AddWater</b>: proietta aree acqua.</item>
    ///   <item><b>AddVegetation</b>: proietta aree vegetazione e piante.</item>
    ///   <item><b>AddWeather</b>: proietta overlay meteo globale.</item>
    ///   <item><b>AddLight</b>: proietta luce globale giorno/notte.</item>
    /// </list>
    /// </summary>
    public static class EnvironmentVisualProjectionResolver
    {
        // =============================================================================
        // BuildProjectionSet
        // =============================================================================
        /// <summary>
        /// <para>
        /// Costruisce un set di proiezioni visuali neutrali da uno snapshot completo.
        /// </para>
        /// </summary>
        public static EnvironmentVisualProjectionSet BuildProjectionSet(
            EnvironmentFullSnapshot snapshot)
        {
            var records = new List<EnvironmentVisualProjectionRecord>();
            if (snapshot == null)
                return new EnvironmentVisualProjectionSet(records);

            AddWater(snapshot, records);
            AddVegetation(snapshot, records);
            AddWeather(snapshot, records);
            AddLight(snapshot, records);

            return new EnvironmentVisualProjectionSet(records);
        }

        private static void AddWater(
            EnvironmentFullSnapshot snapshot,
            List<EnvironmentVisualProjectionRecord> records)
        {
            for (int i = 0; i < snapshot.WaterAreas.Count; i++)
            {
                var water = snapshot.WaterAreas[i];
                if (!TryFindArea(snapshot, water.AreaId, out EnvironmentAreaSnapshot area))
                    continue;

                records.Add(new EnvironmentVisualProjectionRecord(
                    EnvironmentVisualProjectionLayer.Water,
                    EnvironmentVisualProjectionScope.Area,
                    water.AreaId,
                    area.Definition.Bounds,
                    CenterOf(area.Definition.Bounds),
                    "water_" + water.WaterKind.ToString().ToLowerInvariant(),
                    water.WaterLevel01,
                    water.FlowIntensity01 > 0f || water.IsSeasonal,
                    water.DepthLevel != EnvironmentWaterDepthLevel.None
                    && water.WaterLevel01 > 0f));
            }
        }

        private static void AddVegetation(
            EnvironmentFullSnapshot snapshot,
            List<EnvironmentVisualProjectionRecord> records)
        {
            for (int i = 0; i < snapshot.VegetationAreas.Count; i++)
            {
                var vegetation = snapshot.VegetationAreas[i];
                if (!TryFindArea(snapshot, vegetation.AreaId, out EnvironmentAreaSnapshot area))
                    continue;

                records.Add(new EnvironmentVisualProjectionRecord(
                    EnvironmentVisualProjectionLayer.Vegetation,
                    EnvironmentVisualProjectionScope.Area,
                    vegetation.AreaId,
                    area.Definition.Bounds,
                    CenterOf(area.Definition.Bounds),
                    "vegetation_" + vegetation.VegetationKind.ToString().ToLowerInvariant(),
                    vegetation.Density01,
                    vegetation.Health01 > 0.15f,
                    vegetation.Density01 > 0f));
            }

            for (int i = 0; i < snapshot.Plants.Count; i++)
            {
                var plant = snapshot.Plants[i];
                records.Add(new EnvironmentVisualProjectionRecord(
                    EnvironmentVisualProjectionLayer.Vegetation,
                    EnvironmentVisualProjectionScope.Cell,
                    plant.SourceAreaId,
                    new EnvironmentAreaBounds(
                        plant.Cell.X,
                        plant.Cell.Y,
                        plant.Cell.X,
                        plant.Cell.Y,
                        plant.Cell.Z),
                    plant.Cell,
                    "plant_" + plant.SpeciesKey,
                    plant.Maturity01,
                    plant.IsAlive,
                    plant.IsAlive));
            }
        }

        private static void AddWeather(
            EnvironmentFullSnapshot snapshot,
            List<EnvironmentVisualProjectionRecord> records)
        {
            var weather = snapshot.Weather;
            records.Add(new EnvironmentVisualProjectionRecord(
                EnvironmentVisualProjectionLayer.Weather,
                EnvironmentVisualProjectionScope.Global,
                EnvironmentAreaId.None,
                default,
                default,
                "weather_" + weather.Kind.ToString().ToLowerInvariant(),
                weather.Intensity01,
                weather.Precipitation01 > 0f || weather.Wind01 > 0f,
                weather.Kind != EnvironmentWeatherKind.Clear
                && weather.Intensity01 > 0f));
        }

        private static void AddLight(
            EnvironmentFullSnapshot snapshot,
            List<EnvironmentVisualProjectionRecord> records)
        {
            float daylight = ComputeDaylight01(snapshot.Calendar.NormalizedDay01);
            records.Add(new EnvironmentVisualProjectionRecord(
                EnvironmentVisualProjectionLayer.Light,
                EnvironmentVisualProjectionScope.Global,
                EnvironmentAreaId.None,
                default,
                default,
                "light_global",
                daylight,
                false,
                true));
        }

        private static bool TryFindArea(
            EnvironmentFullSnapshot snapshot,
            EnvironmentAreaId areaId,
            out EnvironmentAreaSnapshot area)
        {
            area = default;
            if (snapshot == null || !areaId.IsValid)
                return false;

            for (int i = 0; i < snapshot.Areas.Count; i++)
            {
                if (!snapshot.Areas[i].Definition.AreaId.Equals(areaId))
                    continue;

                area = snapshot.Areas[i];
                return true;
            }

            return false;
        }

        private static EnvironmentCellCoord CenterOf(EnvironmentAreaBounds bounds)
        {
            return new EnvironmentCellCoord(
                bounds.MinX + (bounds.Width / 2),
                bounds.MinY + (bounds.Height / 2),
                bounds.Z);
        }

        private static float ComputeDaylight01(float normalizedDay01)
        {
            float day = EnvironmentMath.Clamp01(normalizedDay01);
            float distanceFromNoon = System.Math.Abs(day - 0.5f) * 2f;
            return EnvironmentMath.Clamp01(1f - distanceFromNoon);
        }
    }
}
