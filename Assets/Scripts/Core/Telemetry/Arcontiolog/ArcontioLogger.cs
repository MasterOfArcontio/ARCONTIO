using System;
using System.Globalization;
using System.IO;
using Arcontio.Core.Config;
using UnityEngine;

namespace Arcontio.Core.Logging
{
    public static class ArcontioLogger
    {
        private const bool LegacyUnityConsoleSinkEnabled = false;
        private const bool LegacyTextOrHtmlFileSinkEnabled = false;

        private static bool _initialized;
        private static GameParams _params;
        private static LoggerDiagnosticsParams _logging;
        private static LocalizationDb _loc;
        private static LogTheme _theme;

        private static ILogSink _unitySink;
        private static FileSink _fileSink;
        private static ILogSink _overlaySink;

        private static LogLevel _minLevel = LogLevel.Info;
        private static HtmlFileSink _htmlSink;
        private static string _language = "it";

        public static bool Initialized => _initialized;
        public static string CurrentLanguage => _params?.Language ?? _language;
        public static string CurrentLogFilePath => _htmlSink?.FilePath ?? _fileSink?.FilePath;

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
            _loc = LocalizationDb.LoadFromResources(localizationPathNoExt);
            _theme = new LogTheme();

            _minLevel = ParseLevel(_logging.general.minimum_level, LogLevel.Warn);
            JsonlRuntimeLogHub.Configure(_logging.jsonl);

            if (LegacyUnityConsoleSinkEnabled && _logging.legacy_channels.unity_console_enabled)
            {
                _unitySink = new UnityConsoleSink();

                // Overlay sink (in-game)
                _overlaySink = new UnityOverlaySink();
            }
            //_unitySink = new UnityConsoleSink();

            if (LegacyTextOrHtmlFileSinkEnabled &&
                (_logging.legacy_channels.html_file_enabled || _logging.legacy_channels.txt_file_enabled))
            {
                var fileName = BuildFileName(_logging.legacy_channels.file_name_pattern);
                var folder = Path.Combine(Application.persistentDataPath, "Logs");
                var path = Path.Combine(folder, fileName);

                if (_logging.legacy_channels.html_file_enabled)
                {
                    _htmlSink = new HtmlFileSink(path);
                }
                else
                {
                    _fileSink = new FileSink(path);
                }
            }

            _initialized = true;

            Info(new LogContext(0, "Core"), new LogBlock(LogLevel.Info, "log.sim.start")
                .AddField("lang", CurrentLanguage)
                .AddField("file", _fileSink != null ? _fileSink.FilePath : "disabled"));
        }

        public static void Shutdown()
        {
            if (!_initialized) return;
            try { JsonlRuntimeLogHub.Shutdown(); } catch { }
            try { _htmlSink?.Dispose(); } catch { }
            try { _fileSink?.Dispose(); } catch { }
            try { _unitySink?.Dispose(); } catch { }
            try { _overlaySink?.Dispose(); } catch { }
            
            _overlaySink = null;
            _htmlSink = null;
            _fileSink = null;
            _unitySink = null;
            _params = null;
            _logging = null;
            _language = "it";
            _initialized = false;
        }

        public static void Flush()
        {
            JsonlRuntimeLogHub.FlushAll();
            _htmlSink?.Flush();
            _fileSink?.Flush();
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
            if (_logging != null && !_logging.general.enabled) return;
            if (lvl < _minLevel) return;

            block.Level = lvl;

            var msg = !string.IsNullOrEmpty(block.MessageFallback)
                ? block.MessageFallback
                : _loc.Get(block.MessageKey, CurrentLanguage);

            var includeTs = _logging.general.include_timestamp;
            var includeTick = _logging.general.include_tick;

            // Unity (rich)
            if (_unitySink != null)
            {
                var rich = LogFormat.FormatUnityRich(block, ctx, msg, includeTs, includeTick, _theme);
                _unitySink.Write(rich);
                _overlaySink?.Write(rich);
            }
            if (_htmlSink != null)
            {
                var html = LogFormat.FormatHtmlBlock(block, ctx, msg, includeTs, includeTick);
                _htmlSink.Write(html);
            }
            // File (plain)
            if (_fileSink != null)
            {
                var plain = LogFormat.FormatPlain(block, ctx, msg, includeTs, includeTick);
                _fileSink.Write(plain);
            }
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

        private static string BuildFileName(string pattern)
        {
            // pattern: "arcontio_{yyyyMMdd_HHmmss}.txt"
            if (string.IsNullOrWhiteSpace(pattern))
                pattern = "arcontio_{yyyyMMdd_HHmmss}.txt";

            var now = DateTime.Now;
            var start = pattern.IndexOf('{');
            var end = pattern.IndexOf('}');
            if (start >= 0 && end > start)
            {
                var fmt = pattern.Substring(start + 1, end - start - 1);
                var stamp = now.ToString(fmt, CultureInfo.InvariantCulture);
                return pattern.Substring(0, start) + stamp + pattern.Substring(end + 1);
            }
            return pattern;
        }
    }
}
