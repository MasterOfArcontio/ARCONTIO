using System.Collections.Generic;
using UnityEngine;

namespace Arcontio.Core.Save
{
    // ─────────────────────────────────────────────────────────────────────────
    // NpcScenarioLoader.cs — caricamento NPC da file di scenario in Resources/
    //
    // Un "scenario" è un file JSON con il formato NpcChunkSaveData (stesso del
    // save system), posizionato in Resources/ invece di persistentDataPath.
    // Non richiede installazione: funziona in Editor e in build.
    //
    // Percorso default:   Resources/Arcontio/Scenarios/default_scenario.json
    // Percorso custom:    Resources/Arcontio/Scenarios/<nome>.json
    //
    // Esempio d'uso in SimulationHost (bootstrap):
    //
    //   if (NpcScenarioLoader.TryLoad("my_scenario", out var entries))
    //       NpcSaveSystem.SpawnFromEntries(entries, _world);
    //   else
    //       // fallback NPC hardcoded
    //
    // Formato file (NpcChunkSaveData con chunkIndex=0, savedAtTick=0):
    // {
    //   "chunkIndex": 0,
    //   "savedAtTick": 0,
    //   "npcs": [
    //     {
    //       "npcId": 0,
    //       "dna": {
    //         "identity": { "name": "Aldric", "originTag": "contadino", "birthTick": 0 },
    //         "capacities": { "strength01": 0.6, ... "competenceCap": [0,0.8,0.4,...] },
    //         "preferenceSeeds": [0,0.7,0.3,...],
    //         "dispositions": { "introversion01": 0.3, ... },
    //         "socialPosition": { "socialClass": "lower", "initialGroupId": -1, ... },
    //         "obligationFrame": { "seeds": [0,...], "culturalOrigin": "contadino" },
    //         "thresholds": { "needAlert01": 0.5, "needCritical01": 0.8, ... },
    //         "cognitiveModulators": { "impulsivity01": 0.4, ... },
    //         "traits": 4,
    //         "tags": ["veterano"]
    //       },
    //       "profile": { "competence": [...], "preference": [...], "obligation": [...] },
    //       "needs": { "hunger01": 0.1, "fatigue01": 0.1, "morale01": 0.7 },
    //       "social": { "leadershipScore": 0.2, "loyaltyToLeader01": 0.5, "justicePerception01": 0.5 },
    //       "spawnX": 10,
    //       "spawnY": 10,
    //       "facingDir": 1,
    //       "activeJobJson": "",
    //       "memoryTraces": []
    //     }
    //   ]
    // }
    // ─────────────────────────────────────────────────────────────────────────

    public static class NpcScenarioLoader
    {
        private const string ResourcesBasePath = "Arcontio/Scenarios/";

        // ── API principale ─────────────────────────────────────────────────────

        /// <summary>
        /// Tenta di caricare il file di scenario con il nome indicato da Resources/.
        /// Restituisce true se trovato e valido, con le entry in <paramref name="entries"/>.
        /// </summary>
        public static bool TryLoad(string scenarioName, out List<NpcSaveEntry> entries)
        {
            string path = ResourcesBasePath + scenarioName;
            var asset = Resources.Load<TextAsset>(path);

            if (asset == null)
            {
                Debug.Log($"[NpcScenarioLoader] Scenario '{scenarioName}' non trovato a Resources/{path}");
                entries = null;
                return false;
            }

            var chunk = JsonUtility.FromJson<NpcChunkSaveData>(asset.text);
            if (chunk?.npcs == null || chunk.npcs.Length == 0)
            {
                Debug.LogWarning($"[NpcScenarioLoader] Scenario '{scenarioName}' vuoto o malformato.");
                entries = null;
                return false;
            }

            entries = new List<NpcSaveEntry>(chunk.npcs);
            Debug.Log($"[NpcScenarioLoader] Scenario '{scenarioName}' caricato: {entries.Count} NPC (tick={chunk.savedAtTick}).");
            return true;
        }

        /// <summary>
        /// Carica il file di scenario default ("default_scenario") da Resources/.
        /// </summary>
        public static bool TryLoadDefault(out List<NpcSaveEntry> entries)
            => TryLoad("default_scenario", out entries);

        /// <summary>
        /// Carica lo scenario e spawna direttamente gli NPC nel World.
        /// Restituisce true se almeno un NPC è stato creato.
        /// </summary>
        public static bool TryLoadAndSpawn(string scenarioName, World world)
        {
            if (!TryLoad(scenarioName, out var entries))
                return false;

            var idMap = NpcSaveSystem.SpawnFromEntries(entries, world);
            Debug.Log($"[NpcScenarioLoader] Spawned {idMap.Count} NPC da scenario '{scenarioName}'.");
            return idMap.Count > 0;
        }

        /// <summary>
        /// Carica lo scenario default e spawna gli NPC nel World.
        /// </summary>
        public static bool TryLoadDefaultAndSpawn(World world)
            => TryLoadAndSpawn("default_scenario", world);
    }
}
