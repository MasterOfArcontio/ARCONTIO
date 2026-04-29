using System;
using System.IO;
using UnityEngine;

namespace Arcontio.Core.Save
{
    // =============================================================================
    // WorldSaveIO
    // =============================================================================
    /// <summary>
    /// <para>
    /// Writer/reader DTO-level per il formato canonico world-level
    /// <see cref="WorldSaveData"/>.
    /// </para>
    ///
    /// <para><b>Principio architetturale: formato world-level canonico separato dai legacy parziali</b></para>
    /// <para>
    /// Questo IO appartiene al macro job v0.10 e scrive lo snapshot complessivo del
    /// mondo in una cartella dedicata. Non sostituisce ancora
    /// <see cref="NpcSaveSystem"/> e non rimuove <c>DevMapIO</c>: i chunk NPC e le
    /// DevMap restano percorsi legacy/parziali utili per compatibilita', debug e
    /// strumenti esistenti. Il file prodotto qui e' invece il candidato canonico per
    /// il futuro save/load autoritativo del <c>World</c>.
    /// </para>
    ///
    /// <para><b>Struttura interna:</b></para>
    /// <list type="bullet">
    ///   <item><b>SaveWorldSnapshot</b>: serializza un DTO gia' costruito su disco.</item>
    ///   <item><b>LoadWorldSnapshotData</b>: legge e deserializza solo il DTO grezzo.</item>
    ///   <item><b>ResolveSlotDirectory</b>: risolve la cartella canonica dello slot world-level.</item>
    ///   <item><b>ResolveSnapshotPath</b>: risolve il file <c>world_snapshot.json</c>.</item>
    /// </list>
    /// </summary>
    public static class WorldSaveIO
    {
        /// <summary>
        /// Nome della cartella root dedicata agli snapshot world-level canonici.
        /// Separata da "saves" di <see cref="NpcSaveSystem"/> per non mischiare
        /// chunk NPC legacy e radice mondo v0.10.
        /// </summary>
        public const string RootFolderName = "saves_world";

        /// <summary>
        /// Nome file stabile dello snapshot canonico dentro lo slot.
        /// </summary>
        public const string SnapshotFileName = "world_snapshot.json";

        // =============================================================================
        // SaveWorldSnapshot
        // =============================================================================
        /// <summary>
        /// <para>
        /// Scrive su disco un <see cref="WorldSaveData"/> gia' costruito.
        /// </para>
        ///
        /// <para><b>Principio architetturale: I/O senza side effect simulativi</b></para>
        /// <para>
        /// Il metodo riceve un DTO completo e lo serializza con <see cref="JsonUtility"/>.
        /// Non legge il <c>World</c>, non costruisce snapshot, non modifica
        /// <c>SimulationHost</c>, non applica load e non tocca i sistemi legacy.
        /// </para>
        ///
        /// <para><b>Struttura interna:</b></para>
        /// <list type="bullet">
        ///   <item><b>Validazione</b>: rifiuta DTO null.</item>
        ///   <item><b>Directory</b>: crea lo slot se manca.</item>
        ///   <item><b>Scrittura</b>: salva JSON pretty-print in <c>world_snapshot.json</c>.</item>
        ///   <item><b>Errori</b>: logga e ritorna <c>false</c> senza crash opaco.</item>
        /// </list>
        /// </summary>
        public static bool SaveWorldSnapshot(WorldSaveData data, string slotName)
        {
            if (data == null)
            {
                Debug.LogError("[WorldSaveIO] SaveWorldSnapshot failed: data is null.");
                return false;
            }

            string path = ResolveSnapshotPath(slotName, ensureDirectory: true);

            try
            {
                // JsonUtility e' coerente con i contratti DTO gia' usati da
                // NpcSaveSystem e DevMapIO. Il writer resta quindi leggero e non
                // introduce nuove dipendenze scene-side o serializer esterni.
                string json = JsonUtility.ToJson(data, prettyPrint: true);
                File.WriteAllText(path, json);

                Debug.Log($"[WorldSaveIO] Saved canonical WorldSaveData to: {path}");
                return true;
            }
            catch (Exception e)
            {
                Debug.LogError($"[WorldSaveIO] SaveWorldSnapshot failed: {path}. {e}");
                return false;
            }
        }

        // =============================================================================
        // LoadWorldSnapshotData
        // =============================================================================
        /// <summary>
        /// <para>
        /// Legge da disco un <see cref="WorldSaveData"/> grezzo senza applicarlo al
        /// runtime.
        /// </para>
        ///
        /// <para><b>Principio architetturale: lettura DTO non e' load simulativo</b></para>
        /// <para>
        /// Questo metodo esiste solo per validare il contratto su disco e permettere
        /// ispezioni/tooling. Non crea NPC, non crea oggetti, non ripristina contatori
        /// id, non ricostruisce cache derivate e non aggiorna <c>SimulationHost</c>.
        /// Il load autoritativo del mondo resta un checkpoint separato.
        /// </para>
        ///
        /// <para><b>Struttura interna:</b></para>
        /// <list type="bullet">
        ///   <item><b>Path</b>: legge dallo slot world-level canonico.</item>
        ///   <item><b>Missing file</b>: log warning e ritorna <c>null</c>.</item>
        ///   <item><b>Parse</b>: usa <see cref="JsonUtility.FromJson{T}(string)"/>.</item>
        ///   <item><b>Errori</b>: logga e ritorna <c>null</c> senza propagare eccezioni opache.</item>
        /// </list>
        /// </summary>
        public static WorldSaveData LoadWorldSnapshotData(string slotName)
        {
            string path = ResolveSnapshotPath(slotName, ensureDirectory: false);

            try
            {
                if (!File.Exists(path))
                {
                    Debug.LogWarning($"[WorldSaveIO] LoadWorldSnapshotData failed: file not found: {path}");
                    return null;
                }

                string json = File.ReadAllText(path);
                var data = JsonUtility.FromJson<WorldSaveData>(json);

                if (data == null)
                {
                    Debug.LogError($"[WorldSaveIO] LoadWorldSnapshotData failed: JSON parsed as null: {path}");
                    return null;
                }

                Debug.Log($"[WorldSaveIO] Loaded raw WorldSaveData DTO from: {path}");
                return data;
            }
            catch (Exception e)
            {
                Debug.LogError($"[WorldSaveIO] LoadWorldSnapshotData failed: {path}. {e}");
                return null;
            }
        }

        // =============================================================================
        // ResolveSlotDirectory
        // =============================================================================
        /// <summary>
        /// <para>
        /// Risolve la directory dello slot world-level canonico.
        /// </para>
        ///
        /// <para><b>Principio architetturale: namespace disco separato</b></para>
        /// <para>
        /// Lo snapshot v0.10 vive sotto <c>saves_world</c>, distinto da
        /// <c>saves</c> dei chunk NPC e da <c>DevMaps</c>. Questa separazione evita
        /// collisioni con formati parziali e rende evidente quale file e' la radice
        /// world-level.
        /// </para>
        ///
        /// <para><b>Struttura interna:</b></para>
        /// <list type="bullet">
        ///   <item><b>slotName</b>: se vuoto, usa <c>default</c>.</item>
        ///   <item><b>base path</b>: <c>Application.persistentDataPath/saves_world/&lt;slot&gt;</c>.</item>
        /// </list>
        /// </summary>
        public static string ResolveSlotDirectory(string slotName)
        {
            string safeSlotName = string.IsNullOrWhiteSpace(slotName) ? "default" : slotName;
            return Path.Combine(Application.persistentDataPath, RootFolderName, safeSlotName);
        }

        // =============================================================================
        // ResolveSnapshotPath
        // =============================================================================
        /// <summary>
        /// <para>
        /// Risolve il path completo del file <c>world_snapshot.json</c>.
        /// </para>
        ///
        /// <para><b>Principio architetturale: un solo file radice per slot</b></para>
        /// <para>
        /// Il writer usa un nome file stabile per rendere banale trovare la radice
        /// canonica dello snapshot. Eventuali chunk o sidecar futuri dovranno essere
        /// aggiunti in modo esplicito, non confusi con i file legacy esistenti.
        /// </para>
        ///
        /// <para><b>Struttura interna:</b></para>
        /// <list type="bullet">
        ///   <item><b>ensureDirectory</b>: crea la directory solo quando serve scrivere.</item>
        ///   <item><b>return</b>: path completo allo snapshot JSON.</item>
        /// </list>
        /// </summary>
        public static string ResolveSnapshotPath(string slotName, bool ensureDirectory = false)
        {
            string directory = ResolveSlotDirectory(slotName);

            if (ensureDirectory)
            {
                try
                {
                    if (!Directory.Exists(directory))
                        Directory.CreateDirectory(directory);
                }
                catch (Exception e)
                {
                    Debug.LogError($"[WorldSaveIO] Failed to ensure directory '{directory}'. {e}");
                }
            }

            return Path.Combine(directory, SnapshotFileName);
        }
    }
}
