using System;
using UnityEngine;

namespace Arcontio.Core.Logging
{
    [Serializable]
    public sealed class GameParams
    {
        public string Language = "it";
        public LoggingParams Logging = new LoggingParams();
    }

    [Serializable]
    public sealed class LoggingParams
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
            if (ta == null)
            {
                Debug.LogWarning($"[ArcontioLog] Missing game params at Resources/{resourcesPathNoExt}.json. Using defaults.");
                return new GameParams();
            }
            try
            {
                return JsonUtility.FromJson<GameParams>(ta.text) ?? new GameParams();
            }
            catch (Exception ex)
            {
                Debug.LogError($"[ArcontioLog] Failed parsing game params: {ex}");
                return new GameParams();
            }
        }
    }
}
