using System.Collections.Generic;
using Arcontio.Core.Environment;

namespace Arcontio.View.ArcGraph
{
    // =============================================================================
    // ArcGraphEnvironmentAdapterHarnessResult
    // =============================================================================
    /// <summary>
    /// <para>
    /// Risultato dello smoke test dell'adapter Environment -> ArcGraph.
    /// </para>
    ///
    /// <para><b>Principio architetturale: verifica adapter senza scena</b></para>
    /// <para>
    /// Il risultato contiene solo flag e conteggi. Non conserva layer, renderer,
    /// GameObject o riferimenti Unity. Serve a confermare che la conversione dati
    /// resta passiva e unidirezionale.
    /// </para>
    ///
    /// <para><b>Struttura interna:</b></para>
    /// <list type="bullet">
    ///   <item><b>Passed</b>: esito complessivo.</item>
    ///   <item><b>WaterSnapshots</b>: snapshot acqua convertiti.</item>
    ///   <item><b>VegetationSnapshots</b>: snapshot vegetazione convertiti.</item>
    ///   <item><b>LightSnapshots</b>: snapshot luce convertiti.</item>
    ///   <item><b>WeatherApplied</b>: meteo applicato al layer.</item>
    ///   <item><b>MissingLayerCount</b>: layer assenti durante applicazione.</item>
    /// </list>
    /// </summary>
    public readonly struct ArcGraphEnvironmentAdapterHarnessResult
    {
        public readonly bool Passed;
        public readonly int WaterSnapshots;
        public readonly int VegetationSnapshots;
        public readonly int LightSnapshots;
        public readonly bool WeatherApplied;
        public readonly int MissingLayerCount;

        // =============================================================================
        // ArcGraphEnvironmentAdapterHarnessResult
        // =============================================================================
        /// <summary>
        /// <para>
        /// Costruisce il risultato dello smoke test adapter.
        /// </para>
        /// </summary>
        public ArcGraphEnvironmentAdapterHarnessResult(
            bool passed,
            int waterSnapshots,
            int vegetationSnapshots,
            int lightSnapshots,
            bool weatherApplied,
            int missingLayerCount)
        {
            Passed = passed;
            WaterSnapshots = waterSnapshots;
            VegetationSnapshots = vegetationSnapshots;
            LightSnapshots = lightSnapshots;
            WeatherApplied = weatherApplied;
            MissingLayerCount = missingLayerCount < 0 ? 0 : missingLayerCount;
        }
    }

    // =============================================================================
    // ArcGraphEnvironmentAdapterHarness
    // =============================================================================
    /// <summary>
    /// <para>
    /// Harness statico dell'adapter Environment -> ArcGraph.
    /// </para>
    ///
    /// <para><b>Principio architetturale: ponte controllato Core -> View</b></para>
    /// <para>
    /// Lo smoke test attraversa l'intera catena dati: stato Environment, full
    /// snapshot, proiezione visuale neutrale, adapter ArcGraph e layer passivi. Non
    /// crea renderer, non legge World, non tocca MapGrid e non modifica la biosfera.
    /// </para>
    ///
    /// <para><b>Struttura interna:</b></para>
    /// <list type="bullet">
    ///   <item><b>RunDefaultSmoke</b>: esegue il test end-to-end data-only.</item>
    ///   <item><b>CreateEnvironmentSnapshot</b>: prepara uno snapshot Core minimo.</item>
    ///   <item><b>Count*</b>: verifica le cache layer senza esporre dizionari interni.</item>
    /// </list>
    /// </summary>
    public static class ArcGraphEnvironmentAdapterHarness
    {
        // =============================================================================
        // RunDefaultSmoke
        // =============================================================================
        /// <summary>
        /// <para>
        /// Esegue uno smoke test completo dell'adapter read-only.
        /// </para>
        /// </summary>
        public static ArcGraphEnvironmentAdapterHarnessResult RunDefaultSmoke()
        {
            var fullSnapshot = EnvironmentReadOnlySnapshotResolver.BuildFullSnapshot(
                CreateEnvironmentSnapshot());
            var projection = EnvironmentVisualProjectionResolver.BuildProjectionSet(fullSnapshot);
            var converted = ArcGraphEnvironmentAdapter.Convert(projection);
            var renderState = new ArcGraphRenderState();
            var waterLayer = new ArcGraphWaterLayer();
            var vegetationLayer = new ArcGraphVegetationLayer();
            var lightLayer = new ArcGraphLightLayer();
            var weatherLayer = new ArcGraphWeatherLayer();
            var effectLayer = new ArcGraphEffectLayer();
            var diagnostics = ArcGraphEnvironmentAdapter.ApplyToLayers(
                converted,
                waterLayer,
                vegetationLayer,
                lightLayer,
                weatherLayer,
                effectLayer,
                renderState);

            bool waterOk = waterLayer.CellCount == 1
                           && waterLayer.TryGetCell(
                               new ArcGraphCellCoord(2, 2),
                               out ArcGraphWaterVisualSnapshot water)
                           && water.DepthLevel == 2
                           && water.SpriteKey == "water_puddle";
            bool vegetationOk = vegetationLayer.CellCount == 2
                                && vegetationLayer.TryGetCell(
                                    new ArcGraphCellCoord(2, 2),
                                    out ArcGraphVegetationVisualSnapshot vegetationArea)
                                && vegetationArea.SpeciesKey == "vegetation_grass"
                                && vegetationLayer.TryGetCell(
                                    new ArcGraphCellCoord(3, 3),
                                    out ArcGraphVegetationVisualSnapshot plant)
                                && plant.SpeciesKey == "plant_oak_tree";
            bool lightOk = lightLayer.CellCount == 1
                           && lightLayer.TryGetCell(
                               new ArcGraphCellCoord(0, 0),
                               out ArcGraphLightVisualSnapshot light)
                           && light.TintKey == "light_global";
            bool weatherOk = weatherLayer.HasWeatherSnapshot
                             && weatherLayer.CurrentWeather.IsActive
                             && weatherLayer.CurrentWeather.WeatherKey == "weather_rain";
            bool effectOk = effectLayer.EffectCount == 0;

            bool passed = converted.Water.Count == 1
                          && converted.Vegetation.Count == 2
                          && converted.Light.Count == 1
                          && converted.Weather.IsActive
                          && diagnostics.AppliedWater == 1
                          && diagnostics.AppliedVegetation == 2
                          && diagnostics.AppliedLight == 1
                          && diagnostics.AppliedWeather
                          && diagnostics.MissingLayerCount == 0
                          && renderState.Dirty.DirtyCellCount >= 3
                          && waterOk
                          && vegetationOk
                          && lightOk
                          && weatherOk
                          && effectOk;

            return new ArcGraphEnvironmentAdapterHarnessResult(
                passed,
                converted.Water.Count,
                converted.Vegetation.Count,
                converted.Light.Count,
                diagnostics.AppliedWeather,
                diagnostics.MissingLayerCount);
        }

        private static EnvironmentSnapshot CreateEnvironmentSnapshot()
        {
            var areaId = new EnvironmentAreaId(700);
            var state = new EnvironmentState();
            state.SetCalendar(EnvironmentCalendarResolver.Resolve(
                EnvironmentCalendarConfig.DefaultCalendarTicksPerSimulatedHour * 12,
                new EnvironmentCalendarConfig()));
            state.SetClimate(new EnvironmentGlobalClimateState(
                0.6f,
                0.75f,
                0.1f,
                new EnvironmentWeatherState(
                    EnvironmentWeatherKind.Rain,
                    0.8f,
                    0.9f,
                    0.2f,
                    false),
                EnvironmentSeasonKind.Spring));
            state.SetAreaDefinition(new EnvironmentAreaDefinition(
                areaId,
                EnvironmentAreaKind.Vegetation,
                new EnvironmentAreaBounds(0, 0, 4, 4),
                0,
                true,
                "arcgraph_adapter_probe"));
            state.SetWaterArea(new EnvironmentWaterAreaState(
                areaId,
                EnvironmentWaterKind.Puddle,
                EnvironmentWaterDepthLevel.Shallow,
                0.45f,
                0f,
                true,
                true));
            state.SetVegetationArea(new EnvironmentVegetationAreaState(
                areaId,
                EnvironmentVegetationKind.Grass,
                0.6f,
                0.7f,
                0.8f,
                0.7f,
                0.7f));

            var catalog = new EnvironmentPlantCatalogConfig().ToCatalog();
            catalog.TryGetSpecies("oak_tree", out EnvironmentPlantSpeciesDefinition oak);
            state.SetPlantInstance(EnvironmentPlantInstance.CreateFromSpecies(
                new EnvironmentPlantId(42),
                oak,
                new EnvironmentCellCoord(3, 3),
                300,
                0.9f,
                areaId));

            return state.CreateSnapshot();
        }
    }
}
