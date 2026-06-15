using System.Collections.Generic;
using Arcontio.Core.Environment;

namespace Arcontio.View.ArcGraph
{
    // =============================================================================
    // ArcGraphEnvironmentAdapterResult
    // =============================================================================
    /// <summary>
    /// <para>
    /// Risultato read-only della conversione Core Environment -> ArcGraph.
    /// </para>
    ///
    /// <para><b>Principio architetturale: View consumer, Core source of truth</b></para>
    /// <para>
    /// Il risultato contiene soltanto snapshot visuali ArcGraph derivati dai record
    /// ambientali Core. Non conserva <see cref="EnvironmentState"/>, non modifica la
    /// biosfera e non possiede autorita' simulativa.
    /// </para>
    ///
    /// <para><b>Struttura interna:</b></para>
    /// <list type="bullet">
    ///   <item><b>Water</b>: snapshot acqua ArcGraph.</item>
    ///   <item><b>Vegetation</b>: snapshot vegetazione ArcGraph.</item>
    ///   <item><b>Light</b>: snapshot luce ArcGraph.</item>
    ///   <item><b>Weather</b>: snapshot meteo ArcGraph.</item>
    ///   <item><b>Effects</b>: snapshot effetti ArcGraph.</item>
    /// </list>
    /// </summary>
    public sealed class ArcGraphEnvironmentAdapterResult
    {
        private static readonly ArcGraphWaterVisualSnapshot[] EmptyWater =
            new ArcGraphWaterVisualSnapshot[0];
        private static readonly ArcGraphVegetationVisualSnapshot[] EmptyVegetation =
            new ArcGraphVegetationVisualSnapshot[0];
        private static readonly ArcGraphLightVisualSnapshot[] EmptyLight =
            new ArcGraphLightVisualSnapshot[0];
        private static readonly ArcGraphEffectVisualSnapshot[] EmptyEffects =
            new ArcGraphEffectVisualSnapshot[0];

        public IReadOnlyList<ArcGraphWaterVisualSnapshot> Water { get; }
        public IReadOnlyList<ArcGraphVegetationVisualSnapshot> Vegetation { get; }
        public IReadOnlyList<ArcGraphLightVisualSnapshot> Light { get; }
        public ArcGraphWeatherVisualSnapshot Weather { get; }
        public IReadOnlyList<ArcGraphEffectVisualSnapshot> Effects { get; }

        public int TotalSnapshotCount =>
            Water.Count
            + Vegetation.Count
            + Light.Count
            + (Weather.IsActive ? 1 : 0)
            + Effects.Count;

        // =============================================================================
        // ArcGraphEnvironmentAdapterResult
        // =============================================================================
        /// <summary>
        /// <para>
        /// Costruisce il risultato copiando liste gia' materializzate dall'adapter.
        /// </para>
        /// </summary>
        public ArcGraphEnvironmentAdapterResult(
            IReadOnlyList<ArcGraphWaterVisualSnapshot> water,
            IReadOnlyList<ArcGraphVegetationVisualSnapshot> vegetation,
            IReadOnlyList<ArcGraphLightVisualSnapshot> light,
            ArcGraphWeatherVisualSnapshot weather,
            IReadOnlyList<ArcGraphEffectVisualSnapshot> effects)
        {
            Water = water ?? EmptyWater;
            Vegetation = vegetation ?? EmptyVegetation;
            Light = light ?? EmptyLight;
            Weather = weather;
            Effects = effects ?? EmptyEffects;
        }
    }

    // =============================================================================
    // ArcGraphEnvironmentAdapterDiagnostics
    // =============================================================================
    /// <summary>
    /// <para>
    /// Diagnostica compatta dell'applicazione snapshot Environment ai layer ArcGraph.
    /// </para>
    ///
    /// <para><b>Principio architetturale: integrazione osservabile senza side effect globali</b></para>
    /// <para>
    /// L'adapter puo' dire quanti snapshot ha applicato e quali layer erano assenti,
    /// senza loggare globalmente, senza usare Unity e senza accedere al World.
    /// </para>
    ///
    /// <para><b>Struttura interna:</b></para>
    /// <list type="bullet">
    ///   <item><b>Applied*</b>: conteggi applicati ai layer presenti.</item>
    ///   <item><b>MissingLayerCount</b>: layer nulli ignorati.</item>
    ///   <item><b>Passed</b>: true se almeno un layer e' stato applicato o non c'erano dati.</item>
    /// </list>
    /// </summary>
    public readonly struct ArcGraphEnvironmentAdapterDiagnostics
    {
        public readonly int AppliedWater;
        public readonly int AppliedVegetation;
        public readonly int AppliedLight;
        public readonly bool AppliedWeather;
        public readonly int AppliedEffects;
        public readonly int MissingLayerCount;

        public bool Passed => MissingLayerCount < 5;

        // =============================================================================
        // ArcGraphEnvironmentAdapterDiagnostics
        // =============================================================================
        /// <summary>
        /// <para>
        /// Costruisce una diagnostica applicazione layer normalizzando conteggi.
        /// </para>
        /// </summary>
        public ArcGraphEnvironmentAdapterDiagnostics(
            int appliedWater,
            int appliedVegetation,
            int appliedLight,
            bool appliedWeather,
            int appliedEffects,
            int missingLayerCount)
        {
            AppliedWater = appliedWater < 0 ? 0 : appliedWater;
            AppliedVegetation = appliedVegetation < 0 ? 0 : appliedVegetation;
            AppliedLight = appliedLight < 0 ? 0 : appliedLight;
            AppliedWeather = appliedWeather;
            AppliedEffects = appliedEffects < 0 ? 0 : appliedEffects;
            MissingLayerCount = missingLayerCount < 0 ? 0 : missingLayerCount;
        }
    }

    // =============================================================================
    // ArcGraphEnvironmentAdapter
    // =============================================================================
    /// <summary>
    /// <para>
    /// Adapter read-only tra Core Environment e snapshot visuali ArcGraph.
    /// </para>
    ///
    /// <para><b>Principio architetturale: ponte unidirezionale Core -> View</b></para>
    /// <para>
    /// L'adapter legge <see cref="EnvironmentVisualProjectionSet"/> prodotto dal Core
    /// e lo converte nei contratti visuali passivi di ArcGraph. Non importa mai dati
    /// View dentro il Core, non modifica simulazione, non crea renderer e non avvia
    /// sistemi runtime.
    /// </para>
    ///
    /// <para><b>Struttura interna:</b></para>
    /// <list type="bullet">
    ///   <item><b>Convert</b>: trasforma projection records in snapshot ArcGraph.</item>
    ///   <item><b>ApplyToLayers</b>: sostituisce cache layer ArcGraph opzionali.</item>
    ///   <item><b>Convert*</b>: mapping specifici per water/vegetation/light/weather/effect.</item>
    /// </list>
    /// </summary>
    public static class ArcGraphEnvironmentAdapter
    {
        // =============================================================================
        // Convert
        // =============================================================================
        /// <summary>
        /// <para>
        /// Converte proiezioni Core Environment in snapshot ArcGraph passivi.
        /// </para>
        /// </summary>
        public static ArcGraphEnvironmentAdapterResult Convert(
            EnvironmentVisualProjectionSet projectionSet,
            int affectedZLevel = ArcGraphZLevelPolicy.DefaultVisibleZLevel)
        {
            var water = new List<ArcGraphWaterVisualSnapshot>();
            var vegetation = new List<ArcGraphVegetationVisualSnapshot>();
            var light = new List<ArcGraphLightVisualSnapshot>();
            var effects = new List<ArcGraphEffectVisualSnapshot>();
            ArcGraphWeatherVisualSnapshot weather =
                ArcGraphWeatherVisualSnapshot.None(affectedZLevel);

            var records = projectionSet?.Records;
            if (records == null)
            {
                return new ArcGraphEnvironmentAdapterResult(
                    water,
                    vegetation,
                    light,
                    weather,
                    effects);
            }

            for (int i = 0; i < records.Count; i++)
            {
                var record = records[i];
                if (!record.IsVisible)
                    continue;

                if (record.Layer == EnvironmentVisualProjectionLayer.Water)
                    water.Add(ConvertWater(record));
                else if (record.Layer == EnvironmentVisualProjectionLayer.Vegetation)
                    vegetation.Add(ConvertVegetation(record));
                else if (record.Layer == EnvironmentVisualProjectionLayer.Light)
                    light.Add(ConvertLight(record));
                else if (record.Layer == EnvironmentVisualProjectionLayer.Weather)
                    weather = ConvertWeather(record, affectedZLevel);
                else if (record.Layer == EnvironmentVisualProjectionLayer.Effect)
                    effects.Add(ConvertEffect(record, effects.Count + 1));
            }

            return new ArcGraphEnvironmentAdapterResult(
                water,
                vegetation,
                light,
                weather,
                effects);
        }

        // =============================================================================
        // ApplyToLayers
        // =============================================================================
        /// <summary>
        /// <para>
        /// Applica snapshot ArcGraph ai layer passivi disponibili.
        /// </para>
        /// </summary>
        public static ArcGraphEnvironmentAdapterDiagnostics ApplyToLayers(
            ArcGraphEnvironmentAdapterResult result,
            ArcGraphWaterLayer waterLayer,
            ArcGraphVegetationLayer vegetationLayer,
            ArcGraphLightLayer lightLayer,
            ArcGraphWeatherLayer weatherLayer,
            ArcGraphEffectLayer effectLayer,
            ArcGraphRenderState renderState = null)
        {
            var safeResult = result ?? new ArcGraphEnvironmentAdapterResult(
                null,
                null,
                null,
                ArcGraphWeatherVisualSnapshot.None(),
                null);
            int missing = 0;
            int appliedWater = 0;
            int appliedVegetation = 0;
            int appliedLight = 0;
            bool appliedWeather = false;
            int appliedEffects = 0;

            if (waterLayer == null)
                missing++;
            else
            {
                waterLayer.ReplaceSnapshots(safeResult.Water, renderState);
                appliedWater = safeResult.Water.Count;
            }

            if (vegetationLayer == null)
                missing++;
            else
            {
                vegetationLayer.ReplaceSnapshots(safeResult.Vegetation, renderState);
                appliedVegetation = safeResult.Vegetation.Count;
            }

            if (lightLayer == null)
                missing++;
            else
            {
                lightLayer.ReplaceSnapshots(safeResult.Light, renderState);
                appliedLight = safeResult.Light.Count;
            }

            if (weatherLayer == null)
                missing++;
            else
            {
                weatherLayer.ReplaceSnapshot(safeResult.Weather);
                appliedWeather = true;
            }

            if (effectLayer == null)
                missing++;
            else
            {
                effectLayer.ReplaceSnapshots(safeResult.Effects, renderState);
                appliedEffects = safeResult.Effects.Count;
            }

            return new ArcGraphEnvironmentAdapterDiagnostics(
                appliedWater,
                appliedVegetation,
                appliedLight,
                appliedWeather,
                appliedEffects,
                missing);
        }

        private static ArcGraphWaterVisualSnapshot ConvertWater(
            EnvironmentVisualProjectionRecord record)
        {
            return new ArcGraphWaterVisualSnapshot(
                ConvertCell(record.Cell),
                ResolveWaterDepth(record.Intensity01),
                record.VisualKey,
                record.IsAnimatedCandidate);
        }

        private static ArcGraphVegetationVisualSnapshot ConvertVegetation(
            EnvironmentVisualProjectionRecord record)
        {
            return new ArcGraphVegetationVisualSnapshot(
                ConvertCell(record.Cell),
                record.VisualKey,
                ResolveGrowthStage(record.Intensity01),
                record.Intensity01);
        }

        private static ArcGraphLightVisualSnapshot ConvertLight(
            EnvironmentVisualProjectionRecord record)
        {
            return new ArcGraphLightVisualSnapshot(
                ConvertCell(record.Cell),
                record.Intensity01,
                record.VisualKey,
                false);
        }

        private static ArcGraphWeatherVisualSnapshot ConvertWeather(
            EnvironmentVisualProjectionRecord record,
            int affectedZLevel)
        {
            return new ArcGraphWeatherVisualSnapshot(
                record.VisualKey,
                record.Intensity01,
                affectedZLevel,
                record.IsVisible);
        }

        private static ArcGraphEffectVisualSnapshot ConvertEffect(
            EnvironmentVisualProjectionRecord record,
            int effectId)
        {
            return new ArcGraphEffectVisualSnapshot(
                effectId,
                ConvertCell(record.Cell),
                record.VisualKey,
                record.Intensity01);
        }

        private static ArcGraphCellCoord ConvertCell(EnvironmentCellCoord cell)
        {
            return new ArcGraphCellCoord(cell.X, cell.Y, cell.Z);
        }

        private static int ResolveWaterDepth(float intensity01)
        {
            if (intensity01 <= 0f)
                return 0;

            if (intensity01 < 0.30f)
                return 1;

            if (intensity01 < 0.60f)
                return 2;

            if (intensity01 < 0.85f)
                return 3;

            return 4;
        }

        private static int ResolveGrowthStage(float maturityOrDensity01)
        {
            if (maturityOrDensity01 <= 0.20f)
                return 0;

            if (maturityOrDensity01 < 0.65f)
                return 1;

            return 2;
        }
    }
}
