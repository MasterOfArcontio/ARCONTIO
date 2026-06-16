using System;
using System.IO;
using Arcontio.Core.Environment;
using UnityEngine;

namespace Arcontio.View.EnvironmentDebug
{
    // =============================================================================
    // EnvironmentProtectedTestController
    // =============================================================================
    /// <summary>
    /// <para>
    /// Controller visuale protetto per osservare e accelerare la biosfera.
    /// </para>
    ///
    /// <para><b>Principio architetturale: debug adapter isolato dal runtime stabile</b></para>
    /// <para>
    /// Il controller usa IMGUI per mostrare lo snapshot ambientale e pilotare il
    /// driver protetto. E' disattivato di default, non salva scene, non crea
    /// rendering ambientale, non interroga ArcGraph e non usa <c>SimulationHost</c>.
    /// Serve solo come superficie manuale per testare giorni, mesi e stagioni in
    /// sicurezza sul branch dedicato.
    /// </para>
    ///
    /// <para><b>Struttura interna:</b></para>
    /// <list type="bullet">
    ///   <item><b>controllerEnabled/processInUpdate</b>: gate manuali dell'avanzamento automatico.</item>
    ///   <item><b>speedPreset</b>: conversione secondi reali/tick ambientali.</item>
    ///   <item><b>telemetry</b>: history runtime in memoria per disegnare grafici diagnostici.</item>
    ///   <item><b>Update</b>: avanza solo quando i gate sono attivi.</item>
    ///   <item><b>OnGUI</b>: pannello dati e comandi manuali.</item>
    ///   <item><b>ContextMenu</b>: comandi rapidi da Inspector.</item>
    /// </list>
    /// </summary>
    public sealed class EnvironmentProtectedTestController : MonoBehaviour
    {
        private const int DefaultTelemetryCapacity = 1200;
        private const int MinimumTelemetryCapacity = 32;
        private const int MaximumTelemetryCapacity = 10000;
        private const float DefaultPanelWidth = 780f;
        private const float DefaultPanelHeight = 1120f;
        private const float GraphHeight = 360f;
        private const int ExportGraphWidth = 1920;
        private const int ExportGraphHeight = 720;
        private const float PlantGraphScale = 24f;
        private const string BiomeConfigResourcePath = "Arcontio/Config/environment_biomes";

        [SerializeField] private bool controllerEnabled;
        [SerializeField] private bool processInUpdate;
        [SerializeField] private bool showPanel = true;
        [SerializeField] private bool logDiagnostics;
        [SerializeField] private bool showTelemetryGraph = true;
        [SerializeField] private bool sampleTelemetryOnDayChange = true;
        [SerializeField] private bool graphFertility = true;
        [SerializeField] private bool graphWater = true;
        [SerializeField] private bool graphVegetation = true;
        [SerializeField] private bool graphSeedBank = true;
        [SerializeField] private bool graphClimate;
        [SerializeField] private bool graphPlants = true;
        [SerializeField] private int telemetryCapacity = DefaultTelemetryCapacity;
        [SerializeField] private EnvironmentProtectedTestBiomePreset biomePreset =
            EnvironmentProtectedTestBiomePreset.TemperateGrassland;
        [SerializeField] private EnvironmentProtectedTestSpeedPreset speedPreset =
            EnvironmentProtectedTestSpeedPreset.Paused;
        [SerializeField] private Rect panelRect =
            new Rect(16f, 16f, DefaultPanelWidth, DefaultPanelHeight);

        private EnvironmentProtectedTestDriver _driver;
        private EnvironmentProtectedTestAdvanceReport _lastReport;
        private EnvironmentProtectedTestHarnessResult _lastHarness;
        private EnvironmentProtectedTelemetrySample[] _telemetrySamples;
        private Texture2D _graphPixel;
        private EnvironmentBiomeCatalog _biomeCatalog;
        private string _lastGraphExportPath = string.Empty;
        private string _lastGraphExportStatus = string.Empty;
        private string _biomeConfigStatus = string.Empty;
        private Vector2 _scroll;
        private int _telemetryStart;
        private int _telemetryCount;
        private long _lastTelemetryDay = -1L;

        // =============================================================================
        // EnvironmentProtectedTelemetrySample
        // =============================================================================
        /// <summary>
        /// <para>
        /// Campione compatto della biosfera usato dal grafico runtime protetto.
        /// </para>
        ///
        /// <para><b>Principio architetturale: diagnostica locale senza persistenza</b></para>
        /// <para>
        /// Il campione e' una copia dei valori read-only gia' esposti dal full
        /// snapshot. Non conserva riferimenti mutabili, non scrive file, non crea
        /// asset e non diventa parte del contratto produttivo della simulazione.
        /// </para>
        ///
        /// <para><b>Struttura interna:</b></para>
        /// <list type="bullet">
        ///   <item><b>Tick/Day</b>: coordinate temporali del campione.</item>
        ///   <item><b>Fertility/Water</b>: stato sintetico dei layer fisici.</item>
        ///   <item><b>Vegetation*</b>: densita' e salute della vegetazione diffusa.</item>
        ///   <item><b>Seed*</b>: amount e viability della seed bank.</item>
        ///   <item><b>Climate*</b>: temperatura, umidita' e aridita' globali.</item>
        ///   <item><b>PlantCount</b>: numero di piante importanti nello snapshot.</item>
        /// </list>
        /// </summary>
        private readonly struct EnvironmentProtectedTelemetrySample
        {
            public readonly long Tick;
            public readonly long Day;
            public readonly float Fertility;
            public readonly float Water;
            public readonly float VegetationDensity;
            public readonly float VegetationHealth;
            public readonly float SeedAmount;
            public readonly float SeedViability;
            public readonly float Temperature;
            public readonly float Humidity;
            public readonly float Aridity;
            public readonly int PlantCount;

            // =============================================================================
            // EnvironmentProtectedTelemetrySample
            // =============================================================================
            /// <summary>
            /// <para>
            /// Costruisce un campione normalizzato per il grafico.
            /// </para>
            /// </summary>
            public EnvironmentProtectedTelemetrySample(
                long tick,
                long day,
                float fertility,
                float water,
                float vegetationDensity,
                float vegetationHealth,
                float seedAmount,
                float seedViability,
                float temperature,
                float humidity,
                float aridity,
                int plantCount)
            {
                Tick = tick < 0 ? 0 : tick;
                Day = day < 0 ? 0 : day;
                Fertility = Mathf.Clamp01(fertility);
                Water = Mathf.Clamp01(water);
                VegetationDensity = Mathf.Clamp01(vegetationDensity);
                VegetationHealth = Mathf.Clamp01(vegetationHealth);
                SeedAmount = Mathf.Clamp01(seedAmount);
                SeedViability = Mathf.Clamp01(seedViability);
                Temperature = Mathf.Clamp01(temperature);
                Humidity = Mathf.Clamp01(humidity);
                Aridity = Mathf.Clamp01(aridity);
                PlantCount = plantCount < 0 ? 0 : plantCount;
            }
        }

        // =============================================================================
        // Update
        // =============================================================================
        /// <summary>
        /// <para>
        /// Avanza automaticamente il driver quando i gate debug sono attivi.
        /// </para>
        /// </summary>
        private void Update()
        {
            // Il controller resta inerte finche' non viene abilitato manualmente.
            if (!controllerEnabled || !processInUpdate)
                return;

            EnsureDriver();
            _lastReport = _driver.AdvanceRealSeconds(
                Time.unscaledDeltaTime,
                speedPreset);
            RecordTelemetrySample(false);
        }

        // =============================================================================
        // OnGUI
        // =============================================================================
        /// <summary>
        /// <para>
        /// Disegna il pannello IMGUI di controllo protetto.
        /// </para>
        /// </summary>
        private void OnGUI()
        {
            if (!showPanel)
                return;

            EnsureDriver();
            EnsurePanelMinimumSize();
            panelRect = GUI.Window(
                GetInstanceID(),
                panelRect,
                DrawPanelWindow,
                "Environment Protected Test");
        }

        // =============================================================================
        // BootstrapProtected
        // =============================================================================
        /// <summary>
        /// <para>
        /// Ricrea il driver protetto da Inspector.
        /// </para>
        /// </summary>
        [ContextMenu("Environment/Protected Test Bootstrap")]
        private void BootstrapProtected()
        {
            EnsureDriver();
            _driver.ResetToProtectedDefaults();
            _lastReport = _driver.LastReport;
            ResetTelemetryHistory();
            RecordTelemetrySample(true);
            Log("Bootstrap protetto completato.");
        }

        // =============================================================================
        // StepOneDay
        // =============================================================================
        /// <summary>
        /// <para>
        /// Avanza manualmente di un giorno simulato da Inspector.
        /// </para>
        /// </summary>
        [ContextMenu("Environment/Protected Test Step Day")]
        private void StepOneDay()
        {
            EnsureDriver();
            _lastReport = _driver.AdvanceDays(1);
            RecordTelemetrySample(true);
            Log("Step giorno completato.");
        }

        // =============================================================================
        // StepOneMonth
        // =============================================================================
        /// <summary>
        /// <para>
        /// Avanza manualmente di un mese simulato da Inspector.
        /// </para>
        /// </summary>
        [ContextMenu("Environment/Protected Test Step Month")]
        private void StepOneMonth()
        {
            EnsureDriver();
            _lastReport = _driver.AdvanceMonths(1);
            RecordTelemetrySample(true);
            Log("Step mese completato.");
        }

        // =============================================================================
        // RunProtectedHarness
        // =============================================================================
        /// <summary>
        /// <para>
        /// Esegue lo smoke test protetto da Inspector.
        /// </para>
        /// </summary>
        [ContextMenu("Environment/Protected Test Run Harness")]
        private void RunProtectedHarness()
        {
            _lastHarness = EnvironmentProtectedTestHarness.RunDefaultSmoke();
            Log(_lastHarness.Message);
        }

        private void DrawPanelWindow(int windowId)
        {
            // La finestra e il suo scroll sono stati raddoppiati su entrambi gli assi
            // rispetto al primo pannello debug: l'area utile cresce circa quattro
            // volte e il grafico diventa leggibile anche con molte curve attive.
            float scrollWidth = Mathf.Max(374f, panelRect.width - 16f);
            float scrollHeight = Mathf.Max(520f, panelRect.height - 40f);
            _scroll = GUILayout.BeginScrollView(
                _scroll,
                GUILayout.Width(scrollWidth),
                GUILayout.Height(scrollHeight));

            DrawGateControls();
            GUILayout.Space(8f);
            DrawBiomeControls();
            GUILayout.Space(8f);
            DrawSpeedControls();
            GUILayout.Space(8f);
            DrawManualStepControls();
            GUILayout.Space(8f);
            DrawSnapshotData();
            GUILayout.Space(8f);
            DrawTelemetryGraphPanel();
            GUILayout.Space(8f);
            DrawLastReport();

            GUILayout.EndScrollView();
            GUI.DragWindow(new Rect(0f, 0f, 10000f, 24f));
        }

        private void DrawGateControls()
        {
            GUILayout.Label("Gate");
            controllerEnabled = GUILayout.Toggle(controllerEnabled, "controllerEnabled");
            processInUpdate = GUILayout.Toggle(processInUpdate, "processInUpdate");
            logDiagnostics = GUILayout.Toggle(logDiagnostics, "logDiagnostics");
        }

        private void DrawSpeedControls()
        {
            GUILayout.Label("Velocita'");
            DrawSpeedButton(EnvironmentProtectedTestSpeedPreset.Paused);
            DrawSpeedButton(EnvironmentProtectedTestSpeedPreset.BaselineTwentyMinutesPerDay);
            DrawSpeedButton(EnvironmentProtectedTestSpeedPreset.OneDayPerTenSeconds);
            DrawSpeedButton(EnvironmentProtectedTestSpeedPreset.OneDayPerSecond);
            DrawSpeedButton(EnvironmentProtectedTestSpeedPreset.OneMonthPerTenSeconds);
            DrawSpeedButton(EnvironmentProtectedTestSpeedPreset.OneSeasonPerTenSeconds);
            DrawSpeedButton(EnvironmentProtectedTestSpeedPreset.OneYearPerMinute);

            var profile = EnvironmentProtectedTestSpeedProfile.FromPreset(
                speedPreset,
                _driver.Config.calendar);
            GUILayout.Label("Preset attivo: " + profile.DisplayName);
            GUILayout.Label("Tick/s: " + profile.EnvironmentTicksPerRealSecond.ToString("0.###"));
        }

        // =============================================================================
        // DrawBiomeControls
        // =============================================================================
        /// <summary>
        /// <para>
        /// Disegna i preset biome del test protetto.
        /// </para>
        /// </summary>
        private void DrawBiomeControls()
        {
            GUILayout.Label("Biome");
            GUILayout.BeginHorizontal();
            DrawBiomeButton(EnvironmentProtectedTestBiomePreset.TemperateGrassland);
            DrawBiomeButton(EnvironmentProtectedTestBiomePreset.Desert);
            GUILayout.EndHorizontal();

            GUILayout.BeginHorizontal();
            DrawBiomeButton(EnvironmentProtectedTestBiomePreset.Jungle);
            DrawBiomeButton(EnvironmentProtectedTestBiomePreset.Tundra);
            GUILayout.EndHorizontal();

            GUILayout.Label("Biome attivo: " + _driver.BiomeProfile.BiomeKey);
            if (!string.IsNullOrWhiteSpace(_biomeConfigStatus))
                GUILayout.Label(_biomeConfigStatus);
            GUILayout.Label(
                "Specie bioma: "
                + FormatSpeciesKeys(_driver.BiomeProfile.AllowedPlantSpeciesKeys));
        }

        private void DrawBiomeButton(EnvironmentProtectedTestBiomePreset preset)
        {
            string label = biomePreset == preset ? "[x] " + preset : "[ ] " + preset;
            if (!GUILayout.Button(label))
                return;

            biomePreset = preset;
            EnsureBiomeCatalogLoaded();
            _driver.SetBiomeCatalog(_biomeCatalog);
            _driver.SetBiomePreset(biomePreset);
            _driver.ResetToProtectedDefaults();
            _lastReport = _driver.LastReport;
            ResetTelemetryHistory();
            RecordTelemetrySample(true);
        }

        private void DrawSpeedButton(EnvironmentProtectedTestSpeedPreset preset)
        {
            string label = speedPreset == preset ? "[x] " + preset : "[ ] " + preset;
            if (GUILayout.Button(label))
                speedPreset = preset;
        }

        private void DrawManualStepControls()
        {
            GUILayout.Label("Step manuali");
            GUILayout.BeginHorizontal();
            if (GUILayout.Button("+1h"))
            {
                _lastReport = _driver.AdvanceHours(1);
                RecordTelemetrySample(true);
            }

            if (GUILayout.Button("+1g"))
            {
                _lastReport = _driver.AdvanceDays(1);
                RecordTelemetrySample(true);
            }

            if (GUILayout.Button("+1m"))
            {
                _lastReport = _driver.AdvanceMonths(1);
                RecordTelemetrySample(true);
            }
            GUILayout.EndHorizontal();

            GUILayout.BeginHorizontal();
            if (GUILayout.Button("+1 stagione"))
            {
                _lastReport = _driver.AdvanceSeasons(1);
                RecordTelemetrySample(true);
            }

            if (GUILayout.Button("+1 anno"))
            {
                _lastReport = _driver.AdvanceYears(1);
                RecordTelemetrySample(true);
            }
            GUILayout.EndHorizontal();

            GUILayout.BeginHorizontal();
            if (GUILayout.Button("Reset"))
            {
                _driver.ResetToProtectedDefaults();
                _lastReport = _driver.LastReport;
                ResetTelemetryHistory();
                RecordTelemetrySample(true);
            }

            if (GUILayout.Button("Harness"))
                _lastHarness = EnvironmentProtectedTestHarness.RunDefaultSmoke();
            GUILayout.EndHorizontal();

            if (_lastHarness != null)
            {
                GUILayout.Label("Harness: " + (_lastHarness.Passed ? "OK" : "FAIL"));
                GUILayout.Label(_lastHarness.Message);
            }
        }

        // =============================================================================
        // DrawTelemetryGraphPanel
        // =============================================================================
        /// <summary>
        /// <para>
        /// Disegna controlli e grafico della telemetria runtime.
        /// </para>
        /// </summary>
        private void DrawTelemetryGraphPanel()
        {
            GUILayout.Label("Grafico runtime");
            showTelemetryGraph = GUILayout.Toggle(showTelemetryGraph, "showTelemetryGraph");
            sampleTelemetryOnDayChange = GUILayout.Toggle(
                sampleTelemetryOnDayChange,
                "sampleTelemetryOnDayChange");

            GUILayout.BeginHorizontal();
            graphFertility = GUILayout.Toggle(graphFertility, "Fert");
            graphWater = GUILayout.Toggle(graphWater, "Water");
            graphVegetation = GUILayout.Toggle(graphVegetation, "Veg");
            GUILayout.EndHorizontal();

            GUILayout.BeginHorizontal();
            graphSeedBank = GUILayout.Toggle(graphSeedBank, "Seed");
            graphClimate = GUILayout.Toggle(graphClimate, "Climate");
            graphPlants = GUILayout.Toggle(graphPlants, "Plants");
            GUILayout.EndHorizontal();

            GUILayout.BeginHorizontal();
            GUILayout.Label("Samples: " + _telemetryCount + "/" + ResolveTelemetryCapacity());
            if (GUILayout.Button("Sample"))
                RecordTelemetrySample(true);
            if (GUILayout.Button("Clear Graph"))
            {
                ResetTelemetryHistory();
                RecordTelemetrySample(true);
            }
            GUILayout.EndHorizontal();

            GUILayout.BeginHorizontal();
            if (GUILayout.Button("Export Graph PNG"))
                ExportTelemetryGraphPng();
            GUILayout.EndHorizontal();

            if (!string.IsNullOrWhiteSpace(_lastGraphExportStatus))
                GUILayout.Label(_lastGraphExportStatus);

            if (!showTelemetryGraph)
                return;

            // Il layout assegna un rettangolo stabile al grafico: le curve non
            // ridimensionano il pannello mentre i valori cambiano.
            Rect graphRect = GUILayoutUtility.GetRect(
                Mathf.Max(350f, panelRect.width - 40f),
                GraphHeight);
            DrawTelemetryGraph(graphRect);
            DrawTelemetryLegend();
        }

        // =============================================================================
        // DrawTelemetryGraph
        // =============================================================================
        /// <summary>
        /// <para>
        /// Disegna assi, griglia e curve della telemetria.
        /// </para>
        /// </summary>
        private void DrawTelemetryGraph(Rect rect)
        {
            EnsureGraphPixel();
            DrawGraphBackground(rect);

            if (_telemetryCount < 2)
            {
                GUI.Label(rect, "In attesa di campioni...");
                return;
            }

            if (graphFertility)
                DrawTelemetrySeries(rect, new Color(0.85f, 0.74f, 0.35f), GetFertilityValue);

            if (graphWater)
                DrawTelemetrySeries(rect, new Color(0.25f, 0.65f, 1.00f), GetWaterValue);

            if (graphVegetation)
            {
                DrawTelemetrySeries(rect, new Color(0.20f, 0.82f, 0.32f), GetVegetationDensityValue);
                DrawTelemetrySeries(rect, new Color(0.52f, 1.00f, 0.45f), GetVegetationHealthValue);
            }

            if (graphSeedBank)
            {
                DrawTelemetrySeries(rect, new Color(0.90f, 0.52f, 0.22f), GetSeedAmountValue);
                DrawTelemetrySeries(rect, new Color(1.00f, 0.34f, 0.18f), GetSeedViabilityValue);
            }

            if (graphClimate)
            {
                DrawTelemetrySeries(rect, new Color(1.00f, 0.42f, 0.38f), GetTemperatureValue);
                DrawTelemetrySeries(rect, new Color(0.35f, 0.78f, 1.00f), GetHumidityValue);
                DrawTelemetrySeries(rect, new Color(0.82f, 0.66f, 0.42f), GetAridityValue);
            }

            if (graphPlants)
                DrawTelemetrySeries(rect, new Color(0.82f, 0.50f, 1.00f), GetPlantValue);
        }

        // =============================================================================
        // DrawTelemetryLegend
        // =============================================================================
        /// <summary>
        /// <para>
        /// Disegna la legenda testuale dei valori tracciati.
        /// </para>
        /// </summary>
        private void DrawTelemetryLegend()
        {
            if (_telemetryCount <= 0)
                return;

            var latest = GetTelemetrySample(_telemetryCount - 1);
            GUILayout.Label(
                "Latest d" + latest.Day
                + " fert=" + latest.Fertility.ToString("0.00")
                + " water=" + latest.Water.ToString("0.00"));
            GUILayout.Label(
                "veg dens/health="
                + latest.VegetationDensity.ToString("0.00")
                + "/"
                + latest.VegetationHealth.ToString("0.00")
                + " seed amount/via="
                + latest.SeedAmount.ToString("0.00")
                + "/"
                + latest.SeedViability.ToString("0.00"));
            GUILayout.Label(
                "clima T/H/A="
                + latest.Temperature.ToString("0.00")
                + "/"
                + latest.Humidity.ToString("0.00")
                + "/"
                + latest.Aridity.ToString("0.00")
                + " plants="
                + latest.PlantCount);
        }

        // =============================================================================
        // ExportTelemetryGraphPng
        // =============================================================================
        /// <summary>
        /// <para>
        /// Esporta il grafico corrente in un file PNG generato dai campioni runtime.
        /// </para>
        ///
        /// <para><b>Principio architetturale: export diagnostico fuori dagli asset</b></para>
        /// <para>
        /// L'immagine viene salvata in <see cref="Application.persistentDataPath"/> e
        /// non dentro <c>Assets</c>. In questo modo l'export non genera file meta, non
        /// entra nella pipeline asset Unity e resta un artefatto locale di test.
        /// </para>
        /// </summary>
        private void ExportTelemetryGraphPng()
        {
            EnsureDriver();
            RecordTelemetrySample(true);

            if (_telemetryCount < 2)
            {
                _lastGraphExportStatus = "Export PNG: servono almeno 2 campioni.";
                return;
            }

            var texture = new Texture2D(
                ExportGraphWidth,
                ExportGraphHeight,
                TextureFormat.RGBA32,
                false);

            try
            {
                DrawTelemetryGraphToTexture(texture);
                byte[] png = texture.EncodeToPNG();
                string directory = Path.Combine(
                    Application.persistentDataPath,
                    "BiosphereGraphs");
                Directory.CreateDirectory(directory);

                var latest = GetTelemetrySample(_telemetryCount - 1);
                string fileName =
                    "biosphere_graph_day_"
                    + latest.Day
                    + "_"
                    + DateTime.Now.ToString("yyyyMMdd_HHmmss")
                    + ".png";
                string path = Path.Combine(directory, fileName);
                File.WriteAllBytes(path, png);

                _lastGraphExportPath = path;
                _lastGraphExportStatus = "Export PNG OK: " + path;
                Log(_lastGraphExportStatus);
            }
            catch (Exception exception)
            {
                _lastGraphExportStatus = "Export PNG FAIL: " + exception.Message;
                if (logDiagnostics)
                    Debug.LogException(exception, this);
            }
            finally
            {
                Destroy(texture);
            }
        }

        // =============================================================================
        // DrawTelemetryGraphToTexture
        // =============================================================================
        /// <summary>
        /// <para>
        /// Ridisegna le curve della telemetria su una texture esportabile.
        /// </para>
        /// </summary>
        private void DrawTelemetryGraphToTexture(Texture2D texture)
        {
            if (texture == null)
                return;

            var background = new Color32(10, 12, 14, 255);
            var pixels = new Color32[texture.width * texture.height];
            for (int i = 0; i < pixels.Length; i++)
                pixels[i] = background;

            texture.SetPixels32(pixels);

            var plot = new RectInt(
                56,
                32,
                texture.width - 88,
                texture.height - 96);
            DrawExportGrid(texture, plot);

            if (graphFertility)
                DrawExportTelemetrySeries(texture, plot, new Color32(217, 189, 89, 255), GetFertilityValue);

            if (graphWater)
                DrawExportTelemetrySeries(texture, plot, new Color32(64, 166, 255, 255), GetWaterValue);

            if (graphVegetation)
            {
                DrawExportTelemetrySeries(texture, plot, new Color32(51, 209, 82, 255), GetVegetationDensityValue);
                DrawExportTelemetrySeries(texture, plot, new Color32(133, 255, 115, 255), GetVegetationHealthValue);
            }

            if (graphSeedBank)
            {
                DrawExportTelemetrySeries(texture, plot, new Color32(230, 133, 56, 255), GetSeedAmountValue);
                DrawExportTelemetrySeries(texture, plot, new Color32(255, 87, 46, 255), GetSeedViabilityValue);
            }

            if (graphClimate)
            {
                DrawExportTelemetrySeries(texture, plot, new Color32(255, 107, 97, 255), GetTemperatureValue);
                DrawExportTelemetrySeries(texture, plot, new Color32(89, 199, 255, 255), GetHumidityValue);
                DrawExportTelemetrySeries(texture, plot, new Color32(209, 168, 107, 255), GetAridityValue);
            }

            if (graphPlants)
                DrawExportTelemetrySeries(texture, plot, new Color32(209, 128, 255, 255), GetPlantValue);

            DrawExportRectOutline(texture, plot, new Color32(190, 190, 190, 255));
            texture.Apply(false, false);
        }

        // =============================================================================
        // DrawExportGrid
        // =============================================================================
        /// <summary>
        /// <para>
        /// Disegna griglia e baseline nel PNG esportato.
        /// </para>
        /// </summary>
        private void DrawExportGrid(Texture2D texture, RectInt plot)
        {
            var grid = new Color32(44, 49, 54, 255);
            for (int i = 1; i < 4; i++)
            {
                int y = plot.y + (plot.height * i / 4);
                DrawExportLine(
                    texture,
                    plot.x,
                    y,
                    plot.x + plot.width,
                    y,
                    grid,
                    1);
            }
        }

        // =============================================================================
        // DrawExportTelemetrySeries
        // =============================================================================
        /// <summary>
        /// <para>
        /// Disegna una curva normalizzata 0..1 sulla texture PNG.
        /// </para>
        /// </summary>
        private void DrawExportTelemetrySeries(
            Texture2D texture,
            RectInt plot,
            Color32 color,
            Func<EnvironmentProtectedTelemetrySample, float> selector)
        {
            if (texture == null || _telemetryCount < 2 || selector == null)
                return;

            Vector2Int previous = ResolveExportGraphPoint(
                plot,
                0,
                Mathf.Clamp01(selector(GetTelemetrySample(0))));

            for (int i = 1; i < _telemetryCount; i++)
            {
                Vector2Int current = ResolveExportGraphPoint(
                    plot,
                    i,
                    Mathf.Clamp01(selector(GetTelemetrySample(i))));
                DrawExportLine(
                    texture,
                    previous.x,
                    previous.y,
                    current.x,
                    current.y,
                    color,
                    3);
                previous = current;
            }
        }

        // =============================================================================
        // ResolveExportGraphPoint
        // =============================================================================
        /// <summary>
        /// <para>
        /// Converte indice campione e valore normalizzato in pixel della texture.
        /// </para>
        /// </summary>
        private Vector2Int ResolveExportGraphPoint(RectInt plot, int sampleIndex, float value01)
        {
            float x01 = _telemetryCount <= 1
                ? 0f
                : sampleIndex / (float)(_telemetryCount - 1);
            int x = plot.x + Mathf.RoundToInt(x01 * plot.width);
            int y = plot.y + plot.height - Mathf.RoundToInt(Mathf.Clamp01(value01) * plot.height);
            return new Vector2Int(x, y);
        }

        // =============================================================================
        // DrawExportLine
        // =============================================================================
        /// <summary>
        /// <para>
        /// Disegna una linea su texture con spessore discreto.
        /// </para>
        /// </summary>
        private void DrawExportLine(
            Texture2D texture,
            int x0,
            int y0,
            int x1,
            int y1,
            Color32 color,
            int thickness)
        {
            int dx = Math.Abs(x1 - x0);
            int dy = Math.Abs(y1 - y0);
            int sx = x0 < x1 ? 1 : -1;
            int sy = y0 < y1 ? 1 : -1;
            int error = dx - dy;
            int radius = Mathf.Max(0, thickness / 2);

            while (true)
            {
                DrawExportPoint(texture, x0, y0, color, radius);
                if (x0 == x1 && y0 == y1)
                    break;

                int doubledError = error * 2;
                if (doubledError > -dy)
                {
                    error -= dy;
                    x0 += sx;
                }

                if (doubledError < dx)
                {
                    error += dx;
                    y0 += sy;
                }
            }
        }

        private void DrawExportPoint(Texture2D texture, int x, int y, Color32 color, int radius)
        {
            for (int oy = -radius; oy <= radius; oy++)
            {
                for (int ox = -radius; ox <= radius; ox++)
                {
                    int px = x + ox;
                    int py = y + oy;
                    if (px < 0 || py < 0 || px >= texture.width || py >= texture.height)
                        continue;

                    texture.SetPixel(px, py, color);
                }
            }
        }

        private void DrawExportRectOutline(Texture2D texture, RectInt rect, Color32 color)
        {
            DrawExportLine(texture, rect.x, rect.y, rect.x + rect.width, rect.y, color, 2);
            DrawExportLine(texture, rect.x, rect.y + rect.height, rect.x + rect.width, rect.y + rect.height, color, 2);
            DrawExportLine(texture, rect.x, rect.y, rect.x, rect.y + rect.height, color, 2);
            DrawExportLine(texture, rect.x + rect.width, rect.y, rect.x + rect.width, rect.y + rect.height, color, 2);
        }

        private void DrawSnapshotData()
        {
            var snapshot = _driver.FullSnapshot;
            var calendar = snapshot.Calendar;
            var climate = snapshot.Climate;
            var weather = snapshot.Weather;

            GUILayout.Label("Snapshot");
            GUILayout.Label("Tick: " + calendar.ElapsedEnvironmentTicks);
            GUILayout.Label(
                "Data: Y" + calendar.Year
                + " M" + calendar.Month
                + " D" + calendar.DayOfMonth
                + " H" + calendar.Hour);
            GUILayout.Label(
                "Stagione: " + calendar.Season
                + " | Giorno anno: " + calendar.DayOfYear);
            GUILayout.Label(
                "Clima T/H/A: "
                + climate.Temperature01.ToString("0.00")
                + " / " + climate.Humidity01.ToString("0.00")
                + " / " + climate.Aridity01.ToString("0.00"));
            GUILayout.Label(
                "Meteo: " + weather.Kind
                + " i=" + weather.Intensity01.ToString("0.00")
                + " p=" + weather.Precipitation01.ToString("0.00")
                + " w=" + weather.Wind01.ToString("0.00"));
            GUILayout.Label(
                "Aree: " + snapshot.AreaCount
                + " | Fertilita': " + snapshot.FertilityAreas.Count
                + " | Acqua: " + snapshot.WaterAreas.Count);
            GUILayout.Label(
                "Vegetazione: " + snapshot.VegetationAreas.Count
                + " | SeedBank: " + snapshot.SeedBankAreas.Count
                + " | Piante: " + snapshot.PlantCount);

            DrawFirstAreaDetails(snapshot);
        }

        private void DrawFirstAreaDetails(EnvironmentFullSnapshot snapshot)
        {
            if (snapshot.FertilityAreas.Count > 0)
            {
                var fertility = snapshot.FertilityAreas[0];
                GUILayout.Label(
                    "Fertilita' area " + fertility.AreaId.Value
                    + ": " + fertility.CurrentFertility01.ToString("0.00")
                    + " soil=" + fertility.SoilKind);
            }

            if (snapshot.VegetationAreas.Count > 0)
            {
                var vegetation = snapshot.VegetationAreas[0];
                GUILayout.Label(
                    "Vegetazione area " + vegetation.AreaId.Value
                    + ": dens=" + vegetation.Density01.ToString("0.00")
                    + " health=" + vegetation.Health01.ToString("0.00"));
            }

            if (snapshot.SeedBankAreas.Count > 0)
            {
                var seedBank = snapshot.SeedBankAreas[0];
                GUILayout.Label(
                    "SeedBank area " + seedBank.AreaId.Value
                    + ": amount=" + seedBank.TotalAmount01.ToString("0.00")
                    + " viability=" + seedBank.AverageViability01.ToString("0.00"));
            }
        }

        private void DrawLastReport()
        {
            if (_lastReport == null)
                return;

            GUILayout.Label("Ultimo report");
            GUILayout.Label(
                "Ticks: +" + _lastReport.TicksAdvanced
                + " | batch=" + _lastReport.BatchesExecuted);
            GUILayout.Label(
                "Boundary h/d/m/s/y: "
                + Bool01(_lastReport.HourChanged)
                + "/"
                + Bool01(_lastReport.DayChanged)
                + "/"
                + Bool01(_lastReport.MonthChanged)
                + "/"
                + Bool01(_lastReport.SeasonChanged)
                + "/"
                + Bool01(_lastReport.YearChanged));
            GUILayout.Label("Growth: " + Bool01(_lastReport.RanNaturalGrowth));
            GUILayout.Label(
                "Growth created/updated/seed: "
                + _lastReport.LastGrowthReport.PlantInstancesCreated
                + "/"
                + _lastReport.LastGrowthReport.PlantInstancesUpdated
                + "/"
                + _lastReport.LastGrowthReport.SeedBanksUpdated);
        }

        private void EnsureDriver()
        {
            if (_driver != null)
                return;

            _driver = new EnvironmentProtectedTestDriver();
            EnsureBiomeCatalogLoaded();
            _driver.SetBiomeCatalog(_biomeCatalog);
            _driver.SetBiomePreset(biomePreset);
            _driver.Bootstrap();
            _lastReport = _driver.LastReport;
            ResetTelemetryHistory();
            RecordTelemetrySample(true);
        }

        // =============================================================================
        // EnsureBiomeCatalogLoaded
        // =============================================================================
        /// <summary>
        /// <para>
        /// Carica i profili biome dal file Resources protetto, con fallback default.
        /// </para>
        /// </summary>
        private void EnsureBiomeCatalogLoaded()
        {
            if (_biomeCatalog != null)
                return;

            var asset = Resources.Load<TextAsset>(BiomeConfigResourcePath);
            if (asset == null)
            {
                _biomeCatalog = EnvironmentBiomeCatalog.CreateDefault();
                _biomeConfigStatus = "Biome config: default interno";
                return;
            }

            try
            {
                var config = JsonUtility.FromJson<EnvironmentBiomeCatalogConfig>(asset.text)
                             ?? EnvironmentBiomeCatalogConfig.CreateDefault();
                _biomeCatalog = config.ToCatalog();
                _biomeConfigStatus = "Biome config: Resources/" + BiomeConfigResourcePath + ".json";
            }
            catch (Exception exception)
            {
                _biomeCatalog = EnvironmentBiomeCatalog.CreateDefault();
                _biomeConfigStatus = "Biome config fallback: " + exception.Message;
            }
        }

        private static string FormatSpeciesKeys(string[] speciesKeys)
        {
            if (speciesKeys == null || speciesKeys.Length == 0)
                return "tutte";

            return string.Join(", ", speciesKeys);
        }

        // =============================================================================
        // EnsurePanelMinimumSize
        // =============================================================================
        /// <summary>
        /// <para>
        /// Forza una dimensione minima leggibile per il pannello debug.
        /// </para>
        /// </summary>
        private void EnsurePanelMinimumSize()
        {
            // Il valore di panelRect puo' essere gia' serializzato in scena con la
            // dimensione precedente. Questo controllo runtime aggiorna anche quei
            // componenti esistenti senza richiedere ricreazione manuale.
            if (panelRect.width < DefaultPanelWidth)
                panelRect.width = DefaultPanelWidth;

            if (panelRect.height < DefaultPanelHeight)
                panelRect.height = DefaultPanelHeight;
        }

        // =============================================================================
        // RecordTelemetrySample
        // =============================================================================
        /// <summary>
        /// <para>
        /// Registra un campione della biosfera corrente nella history del grafico.
        /// </para>
        /// </summary>
        private void RecordTelemetrySample(bool force)
        {
            EnsureTelemetryStorage();

            var snapshot = _driver.FullSnapshot;
            long tick = snapshot.Calendar.ElapsedEnvironmentTicks;
            long ticksPerDay = EnvironmentProtectedTestDriver.ResolveTicksPerDay(_driver.Config.calendar);
            long day = ticksPerDay <= 0 ? 0 : tick / ticksPerDay;

            // In modalita' giornaliera evitiamo di campionare ogni frame. Il grafico
            // resta leggibile anche con velocita' alte e non cresce inutilmente.
            if (!force && sampleTelemetryOnDayChange && day == _lastTelemetryDay)
                return;

            var sample = CreateTelemetrySample(snapshot, tick, day);
            int capacity = ResolveTelemetryCapacity();
            int writeIndex = (_telemetryStart + _telemetryCount) % capacity;

            if (_telemetryCount == capacity)
            {
                writeIndex = _telemetryStart;
                _telemetryStart = (_telemetryStart + 1) % capacity;
            }
            else
            {
                _telemetryCount++;
            }

            _telemetrySamples[writeIndex] = sample;
            _lastTelemetryDay = day;
        }

        // =============================================================================
        // CreateTelemetrySample
        // =============================================================================
        /// <summary>
        /// <para>
        /// Estrae i valori principali dal full snapshot corrente.
        /// </para>
        /// </summary>
        private static EnvironmentProtectedTelemetrySample CreateTelemetrySample(
            EnvironmentFullSnapshot snapshot,
            long tick,
            long day)
        {
            float fertility = snapshot.FertilityAreas.Count > 0
                ? snapshot.FertilityAreas[0].CurrentFertility01
                : 0f;
            float water = snapshot.WaterAreas.Count > 0
                ? snapshot.WaterAreas[0].WaterLevel01
                : 0f;
            float vegetationDensity = snapshot.VegetationAreas.Count > 0
                ? snapshot.VegetationAreas[0].Density01
                : 0f;
            float vegetationHealth = snapshot.VegetationAreas.Count > 0
                ? snapshot.VegetationAreas[0].Health01
                : 0f;
            float seedAmount = snapshot.SeedBankAreas.Count > 0
                ? snapshot.SeedBankAreas[0].TotalAmount01
                : 0f;
            float seedViability = snapshot.SeedBankAreas.Count > 0
                ? snapshot.SeedBankAreas[0].AverageViability01
                : 0f;

            return new EnvironmentProtectedTelemetrySample(
                tick,
                day,
                fertility,
                water,
                vegetationDensity,
                vegetationHealth,
                seedAmount,
                seedViability,
                snapshot.Climate.Temperature01,
                snapshot.Climate.Humidity01,
                snapshot.Climate.Aridity01,
                snapshot.PlantCount);
        }

        // =============================================================================
        // ResetTelemetryHistory
        // =============================================================================
        /// <summary>
        /// <para>
        /// Svuota la history del grafico senza alterare lo stato biosfera.
        /// </para>
        /// </summary>
        private void ResetTelemetryHistory()
        {
            EnsureTelemetryStorage();
            _telemetryStart = 0;
            _telemetryCount = 0;
            _lastTelemetryDay = -1L;
        }

        // =============================================================================
        // EnsureTelemetryStorage
        // =============================================================================
        /// <summary>
        /// <para>
        /// Garantisce che il buffer circolare abbia una dimensione valida.
        /// </para>
        /// </summary>
        private void EnsureTelemetryStorage()
        {
            int capacity = ResolveTelemetryCapacity();
            if (_telemetrySamples != null && _telemetrySamples.Length == capacity)
                return;

            // Quando cambia la capacita' ripartiamo puliti: e' un pannello debug e
            // non vale la pena copiare parzialmente una history diagnostica.
            _telemetrySamples = new EnvironmentProtectedTelemetrySample[capacity];
            _telemetryStart = 0;
            _telemetryCount = 0;
            _lastTelemetryDay = -1L;
        }

        // =============================================================================
        // DrawGraphBackground
        // =============================================================================
        /// <summary>
        /// <para>
        /// Disegna sfondo e griglia del grafico.
        /// </para>
        /// </summary>
        private void DrawGraphBackground(Rect rect)
        {
            DrawFilledRect(rect, new Color(0.06f, 0.07f, 0.08f, 0.92f));
            DrawRectOutline(rect, new Color(0.70f, 0.70f, 0.70f, 0.70f));

            for (int i = 1; i < 4; i++)
            {
                float y = rect.y + (rect.height * i / 4f);
                DrawLine(
                    new Vector2(rect.x, y),
                    new Vector2(rect.xMax, y),
                    new Color(1f, 1f, 1f, 0.12f),
                    1f);
            }
        }

        // =============================================================================
        // DrawTelemetrySeries
        // =============================================================================
        /// <summary>
        /// <para>
        /// Disegna una singola curva normalizzata 0..1.
        /// </para>
        /// </summary>
        private void DrawTelemetrySeries(
            Rect rect,
            Color color,
            System.Func<EnvironmentProtectedTelemetrySample, float> selector)
        {
            if (_telemetryCount < 2 || selector == null)
                return;

            Vector2 previous = ResolveGraphPoint(
                rect,
                0,
                Mathf.Clamp01(selector(GetTelemetrySample(0))));

            for (int i = 1; i < _telemetryCount; i++)
            {
                var sample = GetTelemetrySample(i);
                Vector2 current = ResolveGraphPoint(
                    rect,
                    i,
                    Mathf.Clamp01(selector(sample)));
                DrawLine(previous, current, color, 2f);
                previous = current;
            }
        }

        // =============================================================================
        // ResolveGraphPoint
        // =============================================================================
        /// <summary>
        /// <para>
        /// Converte indice campione e valore normalizzato in coordinate pannello.
        /// </para>
        /// </summary>
        private Vector2 ResolveGraphPoint(Rect rect, int sampleIndex, float value01)
        {
            float x01 = _telemetryCount <= 1
                ? 0f
                : sampleIndex / (float)(_telemetryCount - 1);
            return new Vector2(
                rect.x + (x01 * rect.width),
                rect.yMax - (Mathf.Clamp01(value01) * rect.height));
        }

        // =============================================================================
        // DrawLine
        // =============================================================================
        /// <summary>
        /// <para>
        /// Disegna una linea IMGUI usando una texture runtime 1x1.
        /// </para>
        /// </summary>
        private void DrawLine(Vector2 from, Vector2 to, Color color, float width)
        {
            EnsureGraphPixel();
            Color previousColor = GUI.color;
            Matrix4x4 previousMatrix = GUI.matrix;
            Vector2 delta = to - from;
            float angle = Mathf.Atan2(delta.y, delta.x) * Mathf.Rad2Deg;

            // IMGUI non offre una primitiva linea: ruotiamo una rect riempita con un
            // pixel bianco runtime, senza creare asset o materiali.
            GUI.color = color;
            GUIUtility.RotateAroundPivot(angle, from);
            GUI.DrawTexture(
                new Rect(from.x, from.y - (width * 0.5f), delta.magnitude, width),
                _graphPixel);
            GUI.matrix = previousMatrix;
            GUI.color = previousColor;
        }

        private void DrawFilledRect(Rect rect, Color color)
        {
            EnsureGraphPixel();
            Color previousColor = GUI.color;
            GUI.color = color;
            GUI.DrawTexture(rect, _graphPixel);
            GUI.color = previousColor;
        }

        private void DrawRectOutline(Rect rect, Color color)
        {
            DrawFilledRect(new Rect(rect.x, rect.y, rect.width, 1f), color);
            DrawFilledRect(new Rect(rect.x, rect.yMax - 1f, rect.width, 1f), color);
            DrawFilledRect(new Rect(rect.x, rect.y, 1f, rect.height), color);
            DrawFilledRect(new Rect(rect.xMax - 1f, rect.y, 1f, rect.height), color);
        }

        private void EnsureGraphPixel()
        {
            if (_graphPixel != null)
                return;

            _graphPixel = new Texture2D(1, 1, TextureFormat.RGBA32, false)
            {
                hideFlags = HideFlags.HideAndDontSave
            };
            _graphPixel.SetPixel(0, 0, Color.white);
            _graphPixel.Apply(false, true);
        }

        private EnvironmentProtectedTelemetrySample GetTelemetrySample(int index)
        {
            int capacity = ResolveTelemetryCapacity();
            int safeIndex = Mathf.Clamp(index, 0, Mathf.Max(0, _telemetryCount - 1));
            int actualIndex = (_telemetryStart + safeIndex) % capacity;
            return _telemetrySamples[actualIndex];
        }

        private int ResolveTelemetryCapacity()
        {
            return Mathf.Clamp(
                telemetryCapacity,
                MinimumTelemetryCapacity,
                MaximumTelemetryCapacity);
        }

        private static float GetFertilityValue(EnvironmentProtectedTelemetrySample sample)
        {
            return sample.Fertility;
        }

        private static float GetWaterValue(EnvironmentProtectedTelemetrySample sample)
        {
            return sample.Water;
        }

        private static float GetVegetationDensityValue(EnvironmentProtectedTelemetrySample sample)
        {
            return sample.VegetationDensity;
        }

        private static float GetVegetationHealthValue(EnvironmentProtectedTelemetrySample sample)
        {
            return sample.VegetationHealth;
        }

        private static float GetSeedAmountValue(EnvironmentProtectedTelemetrySample sample)
        {
            return sample.SeedAmount;
        }

        private static float GetSeedViabilityValue(EnvironmentProtectedTelemetrySample sample)
        {
            return sample.SeedViability;
        }

        private static float GetTemperatureValue(EnvironmentProtectedTelemetrySample sample)
        {
            return sample.Temperature;
        }

        private static float GetHumidityValue(EnvironmentProtectedTelemetrySample sample)
        {
            return sample.Humidity;
        }

        private static float GetAridityValue(EnvironmentProtectedTelemetrySample sample)
        {
            return sample.Aridity;
        }

        private static float GetPlantValue(EnvironmentProtectedTelemetrySample sample)
        {
            return Mathf.Clamp01(sample.PlantCount / PlantGraphScale);
        }

        private void Log(string message)
        {
            if (logDiagnostics)
                Debug.Log("[EnvironmentProtectedTestController] " + message, this);
        }

        private static string Bool01(bool value)
        {
            return value ? "1" : "0";
        }
    }
}
