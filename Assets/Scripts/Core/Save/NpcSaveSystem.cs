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
    //   var entries = NpcSaveSystem.BuildEntries(world, currentTick);
    //   NpcSaveSystem.SaveAllChunks(entries, currentTick, slotName);
    //
    // Uso tipico (load / scenario):
    //   var entries = NpcSaveSystem.LoadAllChunks(slotName);
    //   NpcSaveSystem.SpawnFromEntries(entries, world);
    //
    // v0.04.07.b:
    //   BuildEntry ora include DNA, posizione spawn e Social.
    //   Aggiunto BuildEntriesFromWorld per costruire le entry direttamente dal World.
    //   Aggiunto SpawnFromEntries per creare gli NPC nel World da una lista di entry.
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
        /// Costruisce una NpcSaveEntry completa per un singolo NPC.
        ///
        /// dna:     DNA immutabile dell'NPC.
        /// profile: NpcProfile runtime dell'NPC.
        /// needs:   Needs corrente dell'NPC.
        /// social:  Stato sociale corrente.
        /// store:   MemoryStore dell'NPC. Può essere null (nessuna traccia).
        /// x, y:    Posizione attuale nella griglia.
        /// facing:  Orientamento attuale.
        /// </summary>
        public static NpcSaveEntry BuildEntry(
            int              npcId,
            NpcDnaProfile    dna,
            NpcProfile       profile,
            NpcNeeds         needs,
            Social           social,
            MemoryStore      store,
            int              x,
            int              y,
            CardinalDirection facing = CardinalDirection.North)
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
                npcId         = npcId,
                dna           = dna    != null ? NpcDnaSaveData.From(dna)          : null,
                profile       = profile != null ? NpcProfileSaveData.FromProfile(profile) : null,
                needs         = NeedsSaveData.FromNpcNeeds(needs),
                social        = SocialSaveData.From(social),
                spawnX        = x,
                spawnY        = y,
                facingDir     = (int)facing,
                activeJobJson = string.Empty,  // placeholder — Job System v0.06
                memoryTraces  = tracesDtos
            };
        }

        /// <summary>
        /// Costruisce tutte le entry NPC direttamente dal World.
        /// Legge DNA, Profile, Needs, Social, GridPos e Facing dai dizionari del World.
        /// </summary>
        public static List<NpcSaveEntry> BuildEntriesFromWorld(World world)
        {
            var result = new List<NpcSaveEntry>(world.NpcDna.Count);

            foreach (var kv in world.NpcDna)
            {
                int npcId = kv.Key;
                var dna   = kv.Value;

                world.NpcProfiles.TryGetValue(npcId, out var profile);
                world.Needs.TryGetValue(npcId, out var needs);
                world.Social.TryGetValue(npcId, out var social);
                world.Memory.TryGetValue(npcId, out var store);
                world.GridPos.TryGetValue(npcId, out var gridPos);
                var facing = world.GetFacing(npcId);

                result.Add(BuildEntry(
                    npcId, dna, profile, needs, social, store,
                    gridPos.X, gridPos.Y, facing));
            }

            return result;
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

        // ── Spawn NPC da entry ─────────────────────────────────────────────────

        /// <summary>
        /// Crea gli NPC nel World da una lista di NpcSaveEntry.
        ///
        /// Per ogni entry:
        ///   1. Ricostruisce NpcDnaProfile dal DTO (o usa CreateDefault se mancante)
        ///   2. Chiama world.CreateNpc(dna, needs, social, x, y) → ottiene l'id assegnato
        ///   3. Applica l'orientamento con world.SetFacing
        ///   4. Se NpcProfile è presente, lo carica nel dizionario world.NpcProfiles
        ///   5. Se memoryTraces è presente, le carica nel MemoryStore dell'NPC
        ///
        /// Nota: world.CreateNpc assegna un id sequenziale (non usa entry.npcId).
        /// Gli id possono divergere tra salvataggio e caricamento se il mondo
        /// contiene già NPC prima di chiamare SpawnFromEntries.
        /// Per scenari di definizione (tick=0, mondo vuoto), gli id coincidono.
        ///
        /// Restituisce la mappa oldId→newId per eventuali fix-up di riferimenti.
        /// </summary>
        public static Dictionary<int, int> SpawnFromEntries(
            IReadOnlyList<NpcSaveEntry> entries,
            World world)
        {
            var idMap = new Dictionary<int, int>(entries.Count);

            foreach (var entry in entries)
            {
                // Ricostruisci DNA (fallback a CreateDefault se mancante)
                NpcDnaProfile dna = entry.dna != null
                    ? entry.dna.To()
                    : NpcDnaProfile.CreateDefault("npc_" + entry.npcId);

                // Ricostruisci Needs e Social
                NpcNeeds needs = entry.needs != null ? entry.needs.ToNpcNeeds() : NpcNeeds.Default();
                Social social = entry.social != null ? entry.social.To()        : default;

                // Crea l'NPC nel World
                int newId = world.CreateNpc(dna, needs, social, entry.spawnX, entry.spawnY);
                idMap[entry.npcId] = newId;

                // Orientamento
                world.SetFacing(newId, (CardinalDirection)entry.facingDir);

                // Profile runtime: sovrascrive quello generato da CreateNpc
                if (entry.profile != null)
                    world.NpcProfiles[newId] = entry.profile.ToProfile();

                // Tracce di memoria
                if (entry.memoryTraces != null && entry.memoryTraces.Length > 0
                    && world.Memory.TryGetValue(newId, out var store) && store != null)
                {
                    foreach (var dto in entry.memoryTraces)
                        store.AddOrMerge(dto.ToTrace());
                }
            }

            return idMap;
        }

        /// <summary>
        /// Versione legacy — deprecata in v0.04.07.b.
        /// Usa SpawnFromEntries per creare gli NPC direttamente nel World.
        /// </summary>
        [System.Obsolete("Usa SpawnFromEntries(entries, world) — restituisce gli id creati e applica DNA + posizione.")]
        public static Dictionary<int, NpcProfile> ApplyEntries(
            IReadOnlyList<NpcSaveEntry> entries,
            out Dictionary<int, NpcNeeds>           needsOut,
            out Dictionary<int, List<MemoryTrace>> tracesOut)
        {
            var profiles = new Dictionary<int, NpcProfile>(entries.Count);
            needsOut  = new Dictionary<int, NpcNeeds>(entries.Count);
            tracesOut = new Dictionary<int, List<MemoryTrace>>(entries.Count);

            foreach (var entry in entries)
            {
                if (entry.profile != null) profiles[entry.npcId] = entry.profile.ToProfile();
                if (entry.needs   != null) needsOut[entry.npcId]  = entry.needs.ToNpcNeeds();

                var traces = new List<MemoryTrace>(entry.memoryTraces?.Length ?? 0);
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
