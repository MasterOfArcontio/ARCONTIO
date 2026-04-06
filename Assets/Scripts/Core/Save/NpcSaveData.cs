using System;

namespace Arcontio.Core.Save
{
    // ─────────────────────────────────────────────────────────────────────────
    // NpcSaveData.cs — DTO per il formato npcs_chunk_N.json
    //
    // Tutti i tipi qui sono [Serializable] e compatibili con JsonUtility.
    // NON usare Dictionary, nullable<T>, o tipi non primitivi non serializzabili.
    //
    // Formato file:
    //   npcs_chunk_0.json  → NPC con id 0..49
    //   npcs_chunk_1.json  → NPC con id 50..99
    //   npcs_chunk_N.json  → NPC con id N*50..(N+1)*50-1
    //
    // Ogni file contiene un NpcChunkSaveData con l'array di NpcSaveEntry.
    // Campi "job": placeholder stringa vuota — verrà popolato in v0.06 (Job System).
    // ─────────────────────────────────────────────────────────────────────────


    // ── DTO: NpcProfile ───────────────────────────────────────────────────────

    /// <summary>
    /// DTO serializzabile per NpcProfile.
    /// Tre array di float indicizzati per DomainKind (lunghezza = DomainKind.COUNT = 9).
    /// </summary>
    [Serializable]
    public sealed class NpcProfileSaveData
    {
        /// <summary>Competenza per dominio [DomainKind.COUNT].</summary>
        public float[] competence;

        /// <summary>Preferenza per dominio [DomainKind.COUNT].</summary>
        public float[] preference;

        /// <summary>Obbligo per dominio [DomainKind.COUNT].</summary>
        public float[] obligation;

        /// <summary>
        /// Ruolo istituzionale corrente. Stringa vuota = nessun ruolo.
        /// </summary>
        public string assignedRole;

        /// <summary>
        /// Crea un NpcProfileSaveData da un NpcProfile runtime.
        /// </summary>
        public static NpcProfileSaveData FromProfile(NpcProfile profile)
        {
            int count = (int)DomainKind.COUNT;
            var dto = new NpcProfileSaveData
            {
                competence   = new float[count],
                preference   = new float[count],
                obligation   = new float[count],
                assignedRole = profile.AssignedRole ?? string.Empty
            };
            Array.Copy(profile.Competence.Values, dto.competence, count);
            Array.Copy(profile.Preference.Values, dto.preference, count);
            Array.Copy(profile.Obligation.Values, dto.obligation, count);
            return dto;
        }

        /// <summary>
        /// Ricostruisce un NpcProfile runtime da questo DTO.
        /// </summary>
        public NpcProfile ToProfile()
        {
            return new NpcProfile(
                competence: CompetenceProfile.FromSeeds(competence),
                preference: PreferenceProfile.FromSeeds(preference),
                obligation: ObligationProfile.FromSeeds(obligation),
                assignedRole: string.IsNullOrEmpty(assignedRole) ? null : assignedRole
            );
        }
    }


    // ── DTO: Needs ────────────────────────────────────────────────────────────

    /// <summary>
    /// DTO serializzabile per la struct Needs (NPCComponents.cs).
    /// Serializza solo i valori continui — i flag derivati (IsHungry, IsTired)
    /// vengono ricalcolati da NeedsDecaySystem al caricamento.
    /// </summary>
    [Serializable]
    public sealed class NeedsSaveData
    {
        public float hunger01;
        public float fatigue01;
        public float morale01;

        public static NeedsSaveData FromNeeds(Needs needs)
        {
            return new NeedsSaveData
            {
                hunger01  = needs.Hunger01,
                fatigue01 = needs.Fatigue01,
                morale01  = needs.Morale01
            };
        }

        public Needs ToNeeds()
        {
            return new Needs
            {
                Hunger01  = hunger01,
                Fatigue01 = fatigue01,
                Morale01  = morale01
                // IsHungry e IsTired: ricalcolati da NeedsDecaySystem dopo il caricamento
            };
        }
    }


    // ── DTO: MemoryTrace ──────────────────────────────────────────────────────

    /// <summary>
    /// DTO serializzabile per MemoryTrace.
    /// </summary>
    [Serializable]
    public sealed class MemoryTraceSaveData
    {
        // MemoryType è un enum → serializzato come int da JsonUtility
        public int    type;
        public int    subjectId;
        public int    secondarySubjectId;
        public int    cellX;
        public int    cellY;
        public float  intensity01;
        public float  reliability01;
        public float  decayPerTick01;
        public bool   isHeard;
        // HeardKind è un enum → serializzato come int
        public int    heardKind;
        public int    sourceSpeakerId;

        public static MemoryTraceSaveData FromTrace(in MemoryTrace t)
        {
            return new MemoryTraceSaveData
            {
                type                = (int)t.Type,
                subjectId           = t.SubjectId,
                secondarySubjectId  = t.SecondarySubjectId,
                cellX               = t.CellX,
                cellY               = t.CellY,
                intensity01         = t.Intensity01,
                reliability01       = t.Reliability01,
                decayPerTick01      = t.DecayPerTick01,
                isHeard             = t.IsHeard,
                heardKind           = (int)t.HeardKind,
                sourceSpeakerId     = t.SourceSpeakerId
            };
        }

        public MemoryTrace ToTrace()
        {
            return new MemoryTrace
            {
                Type               = (MemoryType)type,
                SubjectId          = subjectId,
                SecondarySubjectId = secondarySubjectId,
                CellX              = cellX,
                CellY              = cellY,
                Intensity01        = intensity01,
                Reliability01      = reliability01,
                DecayPerTick01     = decayPerTick01,
                IsHeard            = isHeard,
                HeardKind          = (HeardKind)heardKind,
                SourceSpeakerId    = sourceSpeakerId
            };
        }
    }


    // ── DTO: entry per NPC ────────────────────────────────────────────────────

    /// <summary>
    /// Dati completi di un singolo NPC da salvare.
    /// </summary>
    [Serializable]
    public sealed class NpcSaveEntry
    {
        public int npcId;

        public NpcProfileSaveData    profile;
        public NeedsSaveData         needs;

        /// <summary>
        /// Placeholder per il Job attivo serializzato (JSON annidato come stringa).
        /// Sempre stringa vuota fino all'implementazione del Job System (v0.06).
        /// </summary>
        public string activeJobJson;

        public MemoryTraceSaveData[] memoryTraces;
    }


    // ── DTO: chunk ────────────────────────────────────────────────────────────

    /// <summary>
    /// Wrapper per un chunk di salvataggio NPC.
    ///
    /// File: npcs_chunk_{chunkIndex}.json
    /// Un chunk contiene al massimo NpcSaveSystem.NpcsPerChunk NPC (default: 50).
    /// </summary>
    [Serializable]
    public sealed class NpcChunkSaveData
    {
        /// <summary>Indice del chunk (0-based). Corrisponde all'N nel nome file.</summary>
        public int chunkIndex;

        /// <summary>Tick di simulazione al momento del salvataggio.</summary>
        public int savedAtTick;

        /// <summary>Array delle entry NPC in questo chunk.</summary>
        public NpcSaveEntry[] npcs;
    }
}
