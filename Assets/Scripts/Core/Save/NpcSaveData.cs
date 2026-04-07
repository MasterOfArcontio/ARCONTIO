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
    //
    // v0.04.07.b: aggiunto NpcDnaSaveData (serializzazione DNA completo) e
    //             campi spawn (spawnX, spawnY, facingDir) + Social in NpcSaveEntry.
    //             Questo consente sia il save/load completo sia la definizione di
    //             scenari NPC in file JSON (NpcScenarioLoader).
    // ─────────────────────────────────────────────────────────────────────────


    // ═════════════════════════════════════════════════════════════════════════
    // DTO: NpcDnaProfile e le sue sotto-strutture
    // ═════════════════════════════════════════════════════════════════════════

    /// <summary>DTO serializzabile per NpcIdentity.</summary>
    [Serializable]
    public sealed class NpcIdentitySaveData
    {
        public string name;
        public string originTag;
        public int    birthTick;
        public string narrativeNotes;

        public static NpcIdentitySaveData From(NpcIdentity v) => new NpcIdentitySaveData
        {
            name           = v.Name          ?? string.Empty,
            originTag      = v.OriginTag     ?? string.Empty,
            birthTick      = v.BirthTick,
            narrativeNotes = v.NarrativeNotes ?? string.Empty
        };

        public NpcIdentity To() => new NpcIdentity(
            name,
            originTag,
            birthTick,
            string.IsNullOrEmpty(narrativeNotes) ? null : narrativeNotes);
    }

    /// <summary>DTO serializzabile per NpcCapacities.</summary>
    [Serializable]
    public sealed class NpcCapacitiesSaveData
    {
        public float   strength01;
        public float   endurance01;
        public float   agility01;
        public float   baseIntelligence01;
        public float[] competenceCap;

        public static NpcCapacitiesSaveData From(NpcCapacities v)
        {
            int count = (int)DomainKind.COUNT;
            var dto = new NpcCapacitiesSaveData
            {
                strength01         = v.Strength01,
                endurance01        = v.Endurance01,
                agility01          = v.Agility01,
                baseIntelligence01 = v.BaseIntelligence01,
                competenceCap      = new float[count]
            };
            if (v.CompetenceCap != null)
                Array.Copy(v.CompetenceCap, dto.competenceCap,
                           Math.Min(v.CompetenceCap.Length, count));
            else
                for (int i = 0; i < count; i++) dto.competenceCap[i] = 1f;
            return dto;
        }

        public NpcCapacities To() => new NpcCapacities(
            strength01, endurance01, agility01, baseIntelligence01, competenceCap);
    }

    /// <summary>DTO serializzabile per NpcDispositions.</summary>
    [Serializable]
    public sealed class NpcDispositionsSaveData
    {
        public float introversion01;
        public float aggressiveness01;
        public float curiosity01;
        public float cooperativeness01;

        public static NpcDispositionsSaveData From(NpcDispositions v) => new NpcDispositionsSaveData
        {
            introversion01    = v.Introversion01,
            aggressiveness01  = v.Aggressiveness01,
            curiosity01       = v.Curiosity01,
            cooperativeness01 = v.Cooperativeness01
        };

        public NpcDispositions To() => new NpcDispositions(
            introversion01, aggressiveness01, curiosity01, cooperativeness01);
    }

    /// <summary>DTO serializzabile per NpcSocialPosition.</summary>
    [Serializable]
    public sealed class NpcSocialPositionSaveData
    {
        public string socialClass;
        public int    initialGroupId;
        public float  initialReputation01;

        public static NpcSocialPositionSaveData From(NpcSocialPosition v) => new NpcSocialPositionSaveData
        {
            socialClass         = v.SocialClass        ?? "lower",
            initialGroupId      = v.InitialGroupId,
            initialReputation01 = v.InitialReputation01
        };

        public NpcSocialPosition To() => new NpcSocialPosition(
            socialClass, initialGroupId, initialReputation01);
    }

    /// <summary>DTO serializzabile per NpcObligationFrame.</summary>
    [Serializable]
    public sealed class NpcObligationFrameSaveData
    {
        public float[] seeds;
        public string  culturalOrigin;

        public static NpcObligationFrameSaveData From(NpcObligationFrame v)
        {
            int count = (int)DomainKind.COUNT;
            var dto = new NpcObligationFrameSaveData
            {
                seeds         = new float[count],
                culturalOrigin = v.CulturalOrigin ?? string.Empty
            };
            if (v.Seeds != null)
                Array.Copy(v.Seeds, dto.seeds, Math.Min(v.Seeds.Length, count));
            return dto;
        }

        public NpcObligationFrame To() => new NpcObligationFrame(seeds, culturalOrigin);
    }

    /// <summary>DTO serializzabile per NpcThresholds.</summary>
    [Serializable]
    public sealed class NpcThresholdsSaveData
    {
        public float needAlert01;
        public float needCritical01;
        public float roleDissatisfaction01;
        public float stressCritical01;

        public static NpcThresholdsSaveData From(NpcThresholds v) => new NpcThresholdsSaveData
        {
            needAlert01           = v.NeedAlert01,
            needCritical01        = v.NeedCritical01,
            roleDissatisfaction01 = v.RoleDissatisfaction01,
            stressCritical01      = v.StressCritical01
        };

        public NpcThresholds To() => new NpcThresholds(
            needAlert01, needCritical01, roleDissatisfaction01, stressCritical01);
    }

    /// <summary>DTO serializzabile per NpcCognitiveModulators.</summary>
    [Serializable]
    public sealed class NpcCognitiveModulatorsSaveData
    {
        public float impulsivity01;
        public float riskAversion01;
        public float conformism01;
        public float optimism01;
        public float stressResilience01;
        public float sociability01;
        public float driftResistance01;
        public float traumaSensitivity01;
        public float memoryResilience01;
        public float rumination01;
        public float gullibility01;

        public static NpcCognitiveModulatorsSaveData From(NpcCognitiveModulators v) =>
            new NpcCognitiveModulatorsSaveData
            {
                impulsivity01       = v.Impulsivity01,
                riskAversion01      = v.RiskAversion01,
                conformism01        = v.Conformism01,
                optimism01          = v.Optimism01,
                stressResilience01  = v.StressResilience01,
                sociability01       = v.Sociability01,
                driftResistance01   = v.DriftResistance01,
                traumaSensitivity01 = v.TraumaSensitivity01,
                memoryResilience01  = v.MemoryResilience01,
                rumination01        = v.Rumination01,
                gullibility01       = v.Gullibility01
            };

        public NpcCognitiveModulators To() => new NpcCognitiveModulators(
            impulsivity01, riskAversion01, conformism01, optimism01,
            stressResilience01, sociability01, driftResistance01,
            traumaSensitivity01, memoryResilience01, rumination01, gullibility01);
    }

    /// <summary>
    /// DTO serializzabile per NpcDnaProfile — struttura immutabile per-NPC.
    ///
    /// Compatibile con JsonUtility (nessun Dictionary, nessun nullable).
    /// NpcTraitKind (Flags enum) serializzato come uint.
    /// </summary>
    [Serializable]
    public sealed class NpcDnaSaveData
    {
        public NpcIdentitySaveData            identity;
        public NpcCapacitiesSaveData          capacities;
        public float[]                        preferenceSeeds;
        public NpcDispositionsSaveData        dispositions;
        public NpcSocialPositionSaveData      socialPosition;
        public NpcObligationFrameSaveData     obligationFrame;
        public NpcThresholdsSaveData          thresholds;
        public NpcCognitiveModulatorsSaveData cognitiveModulators;
        /// <summary>NpcTraitKind serializzato come uint (Flags enum).</summary>
        public uint                           traits;
        public string[]                       tags;

        public static NpcDnaSaveData From(NpcDnaProfile dna)
        {
            int count = (int)DomainKind.COUNT;
            var prefSeeds = new float[count];
            if (dna.Preferences.Seeds != null)
                Array.Copy(dna.Preferences.Seeds, prefSeeds,
                           Math.Min(dna.Preferences.Seeds.Length, count));

            return new NpcDnaSaveData
            {
                identity            = NpcIdentitySaveData.From(dna.Identity),
                capacities          = NpcCapacitiesSaveData.From(dna.Capacities),
                preferenceSeeds     = prefSeeds,
                dispositions        = NpcDispositionsSaveData.From(dna.Dispositions),
                socialPosition      = NpcSocialPositionSaveData.From(dna.SocialPosition),
                obligationFrame     = NpcObligationFrameSaveData.From(dna.ObligationFrame),
                thresholds          = NpcThresholdsSaveData.From(dna.Thresholds),
                cognitiveModulators = NpcCognitiveModulatorsSaveData.From(dna.CognitiveModulators),
                traits              = (uint)dna.Traits,
                tags                = dna.Tags != null ? (string[])dna.Tags.Clone() : Array.Empty<string>()
            };
        }

        public NpcDnaProfile To() => new NpcDnaProfile(
            identity:            identity?.To()            ?? new NpcIdentity("unknown", "unknown", 0),
            capacities:          capacities?.To()          ?? new NpcCapacities(0.5f, 0.5f, 0.5f, 0.5f, null),
            preferences:         new NpcPreferenceSeeds(preferenceSeeds),
            dispositions:        dispositions?.To()        ?? new NpcDispositions(0.5f, 0.2f, 0.5f, 0.7f),
            socialPosition:      socialPosition?.To()      ?? new NpcSocialPosition("lower", -1, 0.5f),
            obligationFrame:     obligationFrame?.To()     ?? new NpcObligationFrame(null, "unknown"),
            thresholds:          thresholds?.To()          ?? new NpcThresholds(0.5f, 0.8f, 0.6f, 0.8f),
            cognitiveModulators: cognitiveModulators?.To() ?? new NpcCognitiveModulators(
                0.5f, 0.5f, 0.5f, 0.5f, 0.5f, 0.5f, 0.5f, 0.5f, 0.5f, 0.25f, 0.5f),
            traits:              (NpcTraitKind)traits,
            tags:                tags
        );
    }


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
    /// Serializza solo i valori continui per NeedKind (un float per voce).
    /// I flag IsAlert/IsCritical sono derivati e vengono ricalcolati da NeedsDecaySystem
    /// al primo tick dopo il caricamento.
    /// </summary>
    [Serializable]
    public sealed class NeedsSaveData
    {
        // Indici corrispondenti a NeedKind (0=Hunger, 1=Thirst, 2=Rest, ...)
        public float[] values;

        public static NeedsSaveData FromNpcNeeds(NpcNeeds needs)
        {
            int count = (int)NeedKind.COUNT;
            var vals  = new float[count];
            if (needs.States != null)
            {
                for (int i = 0; i < count; i++)
                    vals[i] = needs.States[i].Value01;
            }
            return new NeedsSaveData { values = vals };
        }

        public NpcNeeds ToNpcNeeds()
        {
            var n = NpcNeeds.Default();
            if (values != null)
            {
                int count = System.Math.Min(values.Length, (int)NeedKind.COUNT);
                for (int i = 0; i < count; i++)
                    n.SetValue((NeedKind)i, values[i]);
            }
            return n;
            // IsAlert/IsCritical: ricalcolati da NeedsDecaySystem dopo il caricamento
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


    // ── DTO: Social ───────────────────────────────────────────────────────────

    /// <summary>DTO serializzabile per la struct Social (NPCComponents.cs).</summary>
    [Serializable]
    public sealed class SocialSaveData
    {
        public float leadershipScore;
        public float loyaltyToLeader01;
        public float justicePerception01;

        public static SocialSaveData From(Social s) => new SocialSaveData
        {
            leadershipScore      = s.LeadershipScore,
            loyaltyToLeader01    = s.LoyaltyToLeader01,
            justicePerception01  = s.JusticePerception01
        };

        public Social To() => new Social
        {
            LeadershipScore      = leadershipScore,
            LoyaltyToLeader01    = loyaltyToLeader01,
            JusticePerception01  = justicePerception01
        };
    }


    // ── DTO: entry per NPC ────────────────────────────────────────────────────

    /// <summary>
    /// Dati completi di un singolo NPC da salvare o definire in uno scenario.
    ///
    /// v0.04.07.b:
    ///   - dna: DNA immutabile completo (NpcDnaSaveData)
    ///   - social: stato sociale iniziale
    ///   - spawnX/Y: posizione iniziale nella griglia
    ///   - facingDir: orientamento iniziale (CardinalDirection come int: N=0 E=1 S=2 W=3)
    /// </summary>
    [Serializable]
    public sealed class NpcSaveEntry
    {
        public int npcId;

        // ── DNA immutabile (v0.04.07.b) ──────────────────────────────────────
        public NpcDnaSaveData        dna;

        // ── Profilo e stato runtime ───────────────────────────────────────────
        public NpcProfileSaveData    profile;
        public NeedsSaveData         needs;
        public SocialSaveData        social;

        // ── Posizione e orientamento (v0.04.07.b) ─────────────────────────────
        /// <summary>Coordinata X nella griglia al momento della creazione/salvataggio.</summary>
        public int  spawnX;
        /// <summary>Coordinata Y nella griglia al momento della creazione/salvataggio.</summary>
        public int  spawnY;
        /// <summary>CardinalDirection come int (North=0, East=1, South=2, West=3).</summary>
        public int  facingDir;

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
