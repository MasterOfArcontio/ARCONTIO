using Arcontio.Core.Config;

namespace Arcontio.Core.Logging
{
    public static class ArcontioLogger
    {
        private static bool _initialized;
        private static GameParams _params;
        private static LoggerDiagnosticsParams _logging;

        private static LogLevel _minLevel = LogLevel.Info;
        private static string _language = "it";

        public static bool Initialized => _initialized;
        public static string CurrentLanguage => _params?.Language ?? _language;
        public static string CurrentLogFilePath => null;
        public static bool ShouldWrite(LogLevel level)
        {
            if (!_initialized)
                return true;

            if (_logging != null && !_logging.general.enabled)
                return false;

            return level >= _minLevel;
        }

        public static void InitFromResources(
            string gameParamsPathNoExt = "Arcontio/Config/game_params",
            string localizationPathNoExt = "Arcontio/Config/localization_logs")
        {
            if (_initialized) return;

            InitFromParams(
                GameParamsLoader.LoadFromResources(gameParamsPathNoExt),
                localizationPathNoExt);
        }

        public static void InitFromParams(
            GameParams gameParams,
            string localizationPathNoExt = "Arcontio/Config/localization_logs")
        {
            if (_initialized) return;

            _params = gameParams ?? new GameParams();
            InitFromConfig(
                _params.Language,
                _params.ResolveLogging(),
                localizationPathNoExt);
        }

        public static void InitFromSimulationParams(
            SimulationParams simulationParams,
            string localizationPathNoExt = "Arcontio/Config/localization_logs")
        {
            if (_initialized) return;

            _params = null;
            InitFromConfig(
                simulationParams?.Language ?? "it",
                simulationParams?.ResolveLoggerDiagnostics() ?? new LoggerDiagnosticsParams(),
                localizationPathNoExt);
        }

        private static void InitFromConfig(
            string language,
            LoggerDiagnosticsParams logging,
            string localizationPathNoExt)
        {
            if (_initialized) return;

            _language = string.IsNullOrWhiteSpace(language) ? "it" : language;
            _logging = logging ?? new LoggerDiagnosticsParams();

            _minLevel = ParseLevel(_logging.general.minimum_level, LogLevel.Warn);
            JsonlRuntimeLogHub.Configure(_logging.jsonl);

            _initialized = true;

            if (ShouldWrite(LogLevel.Info))
            {
                Info(new LogContext(0, "Core"), new LogBlock(LogLevel.Info, "log.sim.start")
                    .AddField("lang", CurrentLanguage)
                    .AddField("file", "disabled"));
            }
        }

        public static void Shutdown()
        {
            if (!_initialized) return;
            try { JsonlRuntimeLogHub.Shutdown(); } catch { }
            _params = null;
            _logging = null;
            _language = "it";
            _initialized = false;
        }

        public static void Flush()
        {
            JsonlRuntimeLogHub.FlushAll();
        }

        // API rapida
        public static void Trace(LogContext c, LogBlock b) => Write(LogLevel.Trace, c, b);
        public static void Debug(LogContext c, LogBlock b) => Write(LogLevel.Debug, c, b);
        public static void Info(LogContext c, LogBlock b) => Write(LogLevel.Info, c, b);
        public static void Warn(LogContext c, LogBlock b) => Write(LogLevel.Warn, c, b);
        public static void Error(LogContext c, LogBlock b) => Write(LogLevel.Error, c, b);
        public static void Fatal(LogContext c, LogBlock b) => Write(LogLevel.Fatal, c, b);

        private static void Write(LogLevel lvl, LogContext ctx, LogBlock block)
        {
            if (!_initialized) InitFromResources(); // fallback safe
            if (!ShouldWrite(lvl)) return;
            if (block == null) return;

            block.Level = lvl;
        }

        private static LogLevel ParseLevel(string s, LogLevel fallback)
        {
            if (string.IsNullOrWhiteSpace(s)) return fallback;
            s = s.Trim().ToLowerInvariant();
            return s switch
            {
                "trace" => LogLevel.Trace,
                "debug" => LogLevel.Debug,
                "info" => LogLevel.Info,
                "warn" => LogLevel.Warn,
                "error" => LogLevel.Error,
                "fatal" => LogLevel.Fatal,
                _ => fallback
            };
        }

    }
}
