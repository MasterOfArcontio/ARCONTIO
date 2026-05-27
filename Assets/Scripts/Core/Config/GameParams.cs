using System;
using UnityEngine;

namespace Arcontio.Core.Logging
{
    [Serializable]
    public sealed class GameParams
    {
        public string Language = "it";
        public LoggerDiagnosticsParams logging;

        // Sezione legacy mantenuta solo come ponte per vecchi game_params.json.
        // Il layout canonico dalla v0.11d.00d e' "logging".
        public LegacyLoggingParams Logging;

        public LoggerDiagnosticsParams ResolveLogging()
        {
            if (logging != null)
                return logging;

            return LoggerDiagnosticsParams.FromLegacy(Logging);
        }
    }

    [Serializable]
    public sealed class LoggerDiagnosticsParams
    {
        public LoggerGeneralParams general = new LoggerGeneralParams();
        public LoggerLegacyChannelParams legacy_channels = new LoggerLegacyChannelParams();
        public LoggerJsonlParams jsonl = new LoggerJsonlParams();
        public LoggerTelemetryParams telemetry = new LoggerTelemetryParams();
        public LoggerDevtoolParams devtools = new LoggerDevtoolParams();

        public static LoggerDiagnosticsParams FromLegacy(LegacyLoggingParams legacy)
        {
            var current = new LoggerDiagnosticsParams();
            if (legacy == null)
                return current;

            current.general.minimum_level = string.IsNullOrWhiteSpace(legacy.MinLevel)
                ? current.general.minimum_level
                : legacy.MinLevel;
            current.general.include_timestamp = legacy.IncludeTimestamp;
            current.general.include_tick = legacy.IncludeTick;
            current.legacy_channels.unity_console_enabled = legacy.WriteUnityConsole;
            current.legacy_channels.html_file_enabled =
                legacy.WriteFile &&
                string.Equals(legacy.FileFormat, "html", StringComparison.OrdinalIgnoreCase);
            current.legacy_channels.txt_file_enabled =
                legacy.WriteFile &&
                !string.Equals(legacy.FileFormat, "html", StringComparison.OrdinalIgnoreCase);
            current.legacy_channels.file_name_pattern = string.IsNullOrWhiteSpace(legacy.FileNamePattern)
                ? current.legacy_channels.file_name_pattern
                : legacy.FileNamePattern;
            return current;
        }
    }

    [Serializable]
    public sealed class LoggerGeneralParams
    {
        public bool enabled = true;
        public string minimum_level = "Warn";
        public bool include_timestamp = true;
        public bool include_tick = true;
    }

    [Serializable]
    public sealed class LoggerLegacyChannelParams
    {
        public bool unity_console_enabled = false;
        public bool html_file_enabled = false;
        public bool txt_file_enabled = false;
        public string file_name_pattern = "arcontio_{yyyyMMdd_HHmmss}.txt";
    }

    [Serializable]
    public sealed class LoggerJsonlParams
    {
        public bool enabled = true;
        public double flush_interval_seconds = 0.25;
        public int max_queue_size = 4096;
        public int max_batch_size = 512;
    }

    [Serializable]
    public sealed class LoggerTelemetryParams
    {
        public bool enabled = true;
        public bool dump_to_console_enabled = false;
    }

    [Serializable]
    public sealed class LoggerDevtoolParams
    {
        public bool overlay_enabled = false;
        public bool verbose_debug_enabled = false;
    }

    [Serializable]
    public sealed class LegacyLoggingParams
    {
        public string MinLevel = "Warn";
        public bool WriteUnityConsole = false;
        public bool WriteFile = false;

        public string FileFormat = "txt"; // "txt" | "html"
        public string FileNamePattern = "arcontio_{yyyyMMdd_HHmmss}.txt";

        public bool IncludeTimestamp = true;
        public bool IncludeTick = true;
    }


    public static class GameParamsLoader
    {
        public static GameParams LoadFromResources(string resourcesPathNoExt)
        {
            var ta = Resources.Load<TextAsset>(resourcesPathNoExt);
            return LoadFromTextAsset(ta, resourcesPathNoExt);
        }

        public static GameParams LoadFromTextAsset(TextAsset textAsset, string resourcesPathNoExt)
        {
            if (textAsset == null)
            {
                Debug.LogWarning($"[ArcontioLog] Missing game params at Resources/{resourcesPathNoExt}.json. Using defaults.");
                return new GameParams();
            }

            return LoadFromJson(textAsset.text);
        }

        public static GameParams LoadFromJson(string json)
        {
            if (string.IsNullOrWhiteSpace(json))
                return new GameParams();

            try
            {
                return JsonUtility.FromJson<GameParams>(json) ?? new GameParams();
            }
            catch (Exception ex)
            {
                Debug.LogError($"[ArcontioLog] Failed parsing game params: {ex}");
                return new GameParams();
            }
        }
    }
}
