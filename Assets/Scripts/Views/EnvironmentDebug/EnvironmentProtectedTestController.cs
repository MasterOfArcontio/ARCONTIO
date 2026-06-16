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
    ///   <item><b>Update</b>: avanza solo quando i gate sono attivi.</item>
    ///   <item><b>OnGUI</b>: pannello dati e comandi manuali.</item>
    ///   <item><b>ContextMenu</b>: comandi rapidi da Inspector.</item>
    /// </list>
    /// </summary>
    public sealed class EnvironmentProtectedTestController : MonoBehaviour
    {
        [SerializeField] private bool controllerEnabled;
        [SerializeField] private bool processInUpdate;
        [SerializeField] private bool showPanel = true;
        [SerializeField] private bool logDiagnostics;
        [SerializeField] private EnvironmentProtectedTestSpeedPreset speedPreset =
            EnvironmentProtectedTestSpeedPreset.Paused;
        [SerializeField] private Rect panelRect = new Rect(16f, 16f, 390f, 560f);

        private EnvironmentProtectedTestDriver _driver;
        private EnvironmentProtectedTestAdvanceReport _lastReport;
        private EnvironmentProtectedTestHarnessResult _lastHarness;
        private Vector2 _scroll;

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
            _scroll = GUILayout.BeginScrollView(_scroll, GUILayout.Width(374f), GUILayout.Height(520f));

            DrawGateControls();
            GUILayout.Space(8f);
            DrawSpeedControls();
            GUILayout.Space(8f);
            DrawManualStepControls();
            GUILayout.Space(8f);
            DrawSnapshotData();
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
                _lastReport = _driver.AdvanceHours(1);
            if (GUILayout.Button("+1g"))
                _lastReport = _driver.AdvanceDays(1);
            if (GUILayout.Button("+1m"))
                _lastReport = _driver.AdvanceMonths(1);
            GUILayout.EndHorizontal();

            GUILayout.BeginHorizontal();
            if (GUILayout.Button("+1 stagione"))
                _lastReport = _driver.AdvanceSeasons(1);
            if (GUILayout.Button("+1 anno"))
                _lastReport = _driver.AdvanceYears(1);
            GUILayout.EndHorizontal();

            GUILayout.BeginHorizontal();
            if (GUILayout.Button("Reset"))
            {
                _driver.ResetToProtectedDefaults();
                _lastReport = _driver.LastReport;
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
            _driver.Bootstrap();
            _lastReport = _driver.LastReport;
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
