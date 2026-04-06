using System;
using System.Collections.Generic;
using System.IO;
using UnityEngine;

namespace Arcontio.Core.Save
{
    // ─────────────────────────────────────────────────────────────────────────
    // NpcSaveSystem.cs — salvataggio e caricamento NPC in chunk
    //
    // Formato file:  npcs_chunk_{N}.json
    // Percorso base: Application.persistentDataPath/saves/<slotName>/
    //
    // Un chunk contiene al massimo NpcsPerChunk NPC (default: 50).
    // L'intero salvataggio di una simulazione è quindi N file separati.
    //
    // Uso tipico (save):
    //   var entries = NpcSaveSystem.BuildEntries(npcIds, world, currentTick);
    //   NpcSaveSystem.SaveAllChunks(entries, currentTick, slotName);
    //
    // Uso tipico (load):
    //   var entries = NpcSaveSystem.LoadAllChunks(slotName);
    //   NpcSaveSystem.ApplyEntries(entries, world);
    //
    // Nota architetturale (sessione 5):
    //   BuildEntries e ApplyEntries non sono ancora collegati a World
    //   perché NpcProfile non è ancora registrato come component store in World.
    //   In sessione 5 (integrazione NPC esistenti), World acquisirà un dizionario
    //   per-NPC di NpcProfile e questi metodi verranno completati.
    //   Per ora, BuildEntries riceve i dati esplicitamente.
    // ─────────────────────────────────────────────────────────────────────────

    public static class NpcSaveSystem
    {
        /// <summary>Numero massimo di NPC per file chunk.</summary>
        public const int NpcsPerChunk = 50;

        /// <summary>
        /// Prefisso del nome file chunk. Il file finale sarà: npcs_chunk_{N}.json
        /// </summary>
        public const string ChunkFilePrefix = "npcs_chunk_";

        // ── Costruzione delle entry ────────────────────────────────────────────

        /// <summary>
        /// Costruisce una NpcSaveEntry per un singolo NPC.
        ///
        /// profile: NpcProfile runtime dell'NPC (sessione 5: verrà letto da World).
        /// needs:   Needs corrente dell'NPC.
        /// store:   MemoryStore dell'NPC. Può essere null (nessuna traccia).
        /// </summary>
        public static NpcSaveEntry BuildEntry(
            int        npcId,
            NpcProfile profile,
            Needs      needs,
            MemoryStore store)
        {
            // Serializza tracce di memoria
            MemoryTraceSaveData[] tracesDtos = Array.Empty<MemoryTraceSaveData>();
            if (store != null && store.Traces.Count > 0)
            {
                tracesDtos = new MemoryTraceSaveData[store.Traces.Count];
                for (int i = 0; i < store.Traces.Count; i++)
                    tracesDtos[i] = MemoryTraceSaveData.FromTrace(store.Traces[i]);
            }

            return new NpcSaveEntry
            {
                npcId        = npcId,
                profile      = NpcProfileSaveData.FromProfile(profile),
                needs        = NeedsSaveData.FromNeeds(needs),
                activeJobJson = string.Empty,  // placeholder — Job System v0.06
                memoryTraces = tracesDtos
            };
        }

        // ── Salvataggio ────────────────────────────────────────────────────────

        /// <summary>
        /// Salva tutte le entry NPC in chunk da NpcsPerChunk file.
        ///
        /// slotName: nome dello slot di salvataggio (es. "slot_0", "autosave").
        /// Crea la cartella se non esiste.
        /// Sovrascrive i file esistenti nello slot.
        /// </summary>
        public static void SaveAllChunks(
            IReadOnlyList<NpcSaveEntry> entries,
            int    savedAtTick,
            string slotName = "default")
        {
            string dir = GetSaveDir(slotName);
            Directory.CreateDirectory(dir);

            int totalChunks = ChunkCount(entries.Count);

            for (int chunkIdx = 0; chunkIdx < totalChunks; chunkIdx++)
            {
                int start = chunkIdx * NpcsPerChunk;
                int count = Math.Min(NpcsPerChunk, entries.Count - start);

                var chunkEntries = new NpcSaveEntry[count];
                for (int i = 0; i < count; i++)
                    chunkEntries[i] = entries[start + i];

                var chunk = new NpcChunkSaveData
                {
                    chunkIndex  = chunkIdx,
                    savedAtTick = savedAtTick,
                    npcs        = chunkEntries
                };

                string json = JsonUtility.ToJson(chunk, prettyPrint: true);
                string path = ChunkFilePath(dir, chunkIdx);
                File.WriteAllText(path, json);
            }
        }

        /// <summary>
        /// Salva un singolo chunk già costruito.
        /// Usato per aggiornamento incrementale di un chunk specifico.
        /// </summary>
        public static void SaveChunk(NpcChunkSaveData chunk, string slotName = "default")
        {
            string dir = GetSaveDir(slotName);
            Directory.CreateDirectory(dir);

            string json = JsonUtility.ToJson(chunk, prettyPrint: true);
            string path = ChunkFilePath(dir, chunk.chunkIndex);
            File.WriteAllText(path, json);
        }

        // ── Caricamento ────────────────────────────────────────────────────────

        /// <summary>
        /// Carica tutti i chunk presenti per lo slot e restituisce le entry NPC.
        /// I chunk vengono letti in ordine crescente di indice fino a che esistono file.
        /// Ritorna lista vuota se lo slot non esiste o non contiene chunk.
        /// </summary>
        public static List<NpcSaveEntry> LoadAllChunks(string slotName = "default")
        {
            var result = new List<NpcSaveEntry>();
            string dir = GetSaveDir(slotName);

            if (!Directory.Exists(dir))
                return result;

            int chunkIdx = 0;
            while (true)
            {
                string path = ChunkFilePath(dir, chunkIdx);
                if (!File.Exists(path))
                    break;

                string json = File.ReadAllText(path);
                var chunk = JsonUtility.FromJson<NpcChunkSaveData>(json);

                if (chunk?.npcs != null)
                    result.AddRange(chunk.npcs);

                chunkIdx++;
            }

            return result;
        }

        /// <summary>
        /// Carica un singolo chunk per indice.
        /// Ritorna null se il file non esiste.
        /// </summary>
        public static NpcChunkSaveData LoadChunk(int chunkIdx, string slotName = "default")
        {
            string path = ChunkFilePath(GetSaveDir(slotName), chunkIdx);
            if (!File.Exists(path))
                return null;

            string json = File.ReadAllText(path);
            return JsonUtility.FromJson<NpcChunkSaveData>(json);
        }

        // ── Applicazione al mondo (stub per sessione 5) ────────────────────────

        /// <summary>
        /// Applica le entry caricate al World.
        ///
        /// STUB: in sessione 5, quando NpcProfile sarà un component store in World,
        /// questo metodo popolerà i dizionari per-NPC di NpcProfile, Needs e MemoryStore.
        ///
        /// Per ora ricostruisce i NpcProfile e li restituisce come dizionario.
        /// Needs e MemoryStore devono essere applicati da chi chiama questo metodo
        /// usando le API esistenti di World.
        /// </summary>
        public static Dictionary<int, NpcProfile> ApplyEntries(
            IReadOnlyList<NpcSaveEntry> entries,
            out Dictionary<int, Needs>            needsOut,
            out Dictionary<int, List<MemoryTrace>> tracesOut)
        {
            var profiles = new Dictionary<int, NpcProfile>(entries.Count);
            needsOut     = new Dictionary<int, Needs>(entries.Count);
            tracesOut    = new Dictionary<int, List<MemoryTrace>>(entries.Count);

            foreach (var entry in entries)
            {
                profiles[entry.npcId] = entry.profile.ToProfile();
                needsOut[entry.npcId] = entry.needs.ToNeeds();

                var traces = new List<MemoryTrace>(
                    entry.memoryTraces?.Length ?? 0);

                if (entry.memoryTraces != null)
                    foreach (var dto in entry.memoryTraces)
                        traces.Add(dto.ToTrace());

                tracesOut[entry.npcId] = traces;
            }

            return profiles;
        }

        // ── Utilità ────────────────────────────────────────────────────────────

        /// <summary>
        /// Restituisce il numero di chunk necessari per N NPC.
        /// </summary>
        public static int ChunkCount(int npcCount)
        {
            if (npcCount <= 0) return 0;
            return (npcCount + NpcsPerChunk - 1) / NpcsPerChunk;
        }

        /// <summary>
        /// Restituisce il percorso base della directory di salvataggio per lo slot.
        /// </summary>
        public static string GetSaveDir(string slotName)
        {
            return Path.Combine(Application.persistentDataPath, "saves", slotName);
        }

        private static string ChunkFilePath(string dir, int chunkIdx)
        {
            return Path.Combine(dir, $"{ChunkFilePrefix}{chunkIdx}.json");
        }
    }
}
