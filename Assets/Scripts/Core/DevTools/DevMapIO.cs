// Assets/Scripts/Core/DevTools/DevMapIO.cs
using System;
using System.IO;
using UnityEngine;

namespace Arcontio.Core.DevTools
{
    /// <summary>
    /// DevMapIO:
    /// utility di I/O su filesystem per DevMapData.
    ///
    /// Requisito documento:
    /// - DevMode v0 (MVP) deve supportare save/load JSON. fileciteturn4file9
    ///
    /// Note tecniche:
    /// - Usiamo Application.persistentDataPath per evitare problemi di permessi.
    /// - Non usiamo StreamingAssets/Resources perché non sono scrivibili in build.
    /// </summary>
    public static class DevMapIO
    {
        private const string FolderName = "DevMaps";

        /// <summary>
        /// Ritorna la cartella canonica per i dev maps.
        /// </summary>
        public static string GetDevMapsFolder()
        {
            string folder = Path.Combine(Application.persistentDataPath, FolderName);
            try
            {
                if (!Directory.Exists(folder))
                    Directory.CreateDirectory(folder);
            }
            catch (Exception e)
            {
                Debug.LogError($"[DevMapIO] Failed to ensure folder '{folder}'. {e}");
            }
            return folder;
        }

        /// <summary>
        /// Normalizza un path utente.
        ///
        /// Policy:
        /// - se 'pathOrName' contiene directory (Path.GetDirectoryName non vuoto) => lo trattiamo come path.
        /// - altrimenti => è un "nome" e lo mettiamo sotto persistentDataPath/DevMaps/
        /// - aggiungiamo estensione .json se mancante.
        /// </summary>
        public static string ResolvePath(string pathOrName)
        {
            if (string.IsNullOrWhiteSpace(pathOrName))
                pathOrName = "devmap";

            string folder = GetDevMapsFolder();

            // Se contiene directory, lo consideriamo già un path (relativo o assoluto).
            string dir = Path.GetDirectoryName(pathOrName);
            bool hasDir = !string.IsNullOrWhiteSpace(dir);

            string p = hasDir ? pathOrName : Path.Combine(folder, pathOrName);

            if (!p.EndsWith(".json", StringComparison.OrdinalIgnoreCase))
                p += ".json";

            return p;
        }

        public static bool Save(string pathOrName, DevMapData data)
        {
            if (data == null) return false;

            string path = ResolvePath(pathOrName);

            try
            {
                string json = JsonUtility.ToJson(data, prettyPrint: true);
                File.WriteAllText(path, json);
                Debug.Log($"[DevMapIO] Saved DevMap to: {path}");
                return true;
            }
            catch (Exception e)
            {
                Debug.LogError($"[DevMapIO] Save failed: {path}. {e}");
                return false;
            }
        }

        public static bool TryLoad(string pathOrName, out DevMapData data)
        {
            data = null;

            string path = ResolvePath(pathOrName);

            try
            {
                if (!File.Exists(path))
                {
                    Debug.LogWarning($"[DevMapIO] Load failed: file not found: {path}");
                    return false;
                }

                string json = File.ReadAllText(path);
                data = JsonUtility.FromJson<DevMapData>(json);

                if (data == null)
                {
                    Debug.LogError($"[DevMapIO] Load failed: JSON parsed as null: {path}");
                    return false;
                }

                Debug.Log($"[DevMapIO] Loaded DevMap from: {path}");
                return true;
            }
            catch (Exception e)
            {
                Debug.LogError($"[DevMapIO] Load failed: {path}. {e}");
                return false;
            }
        }
    }
}
