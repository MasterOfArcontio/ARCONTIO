// Assets/Scripts/Core/Commands/DevTools/DevSpawnNpcCommand.cs
using Arcontio.Core.Save;
using UnityEngine;

namespace Arcontio.Core.Commands.DevTools
{
    /// <summary>
    /// DevSpawnNpcCommand (DevMode v1):
    /// crea un NPC in una cella della griglia.
    ///
    /// NOTE DI DESIGN (ARCONTIO):
    /// - Questo è un comando DEV/DEBUG: non deve essere "gameplay safe".
    /// - Tuttavia, per evitare stati impossibili, applichiamo alcune guardie:
    ///   - 1 NPC per cella (Core Standard) => se c'è già un NPC, non spawniamo.
    ///   - non spawniamo su celle che bloccano movement (es. muri).
    ///
    /// Nota:
    /// - In questa versione la creazione usa valori semplici/deterministici per NpcDnaProfile/Needs/Social.
    /// - Se in futuro vuoi spawnare "archetipi" (es. thief, victim, witness), questo comando è il punto giusto.
    /// </summary>
    public sealed class DevSpawnNpcCommand : ICommand
    {
        private readonly int _x;
        private readonly int _y;
        private readonly CardinalDirection _facing;

        /// <summary>
        /// Facing opzionale: se non specificato da UI, default = North.
        /// </summary>
        public DevSpawnNpcCommand(int x, int y, CardinalDirection facing = CardinalDirection.North)
        {
            _x = x;
            _y = y;
            _facing = facing;
        }

        public void Execute(World world, MessageBus bus)
        {
            if (world == null) return;
            if (!world.InBounds(_x, _y)) return;

            // Guard 1: la cella deve essere navigabile (altrimenti spawniamo "dentro un muro").
            if (world.BlocksMovementAt(_x, _y))
            {
                Debug.LogWarning($"[DevTools] SpawnNpc blocked: cell ({_x},{_y}) blocks movement.");
                return;
            }

            // Guard 2: 1 NPC per cella.
            if (world.TryGetNpcAt(_x, _y, out int existingNpc) && existingNpc > 0)
            {
                Debug.LogWarning($"[DevTools] SpawnNpc blocked: cell ({_x},{_y}) already has NPC={existingNpc}.");
                return;
            }

            // Template dati: primo NPC di default_scenario, se disponibile.
            // Fallback: valori deterministici locali per mantenere il DevTool usabile.
            NpcSaveEntry template = null;
            if (NpcScenarioLoader.TryLoadDefault(out var entries) && entries != null && entries.Count > 0)
                template = entries[0];

            // Runtime spawn usa il primo NPC di default_scenario come template dati.
            // La posizione e il facing restano quelli scelti dal DevTool.
            var dna = template?.dna != null
                ? template.dna.To()
                : NpcDnaProfile.CreateDefault($"DEV_NPC({_x},{_y})");

            // Needs dal template; fallback "safe" se lo scenario non e disponibile.
            var needs = template?.needs != null
                ? template.needs.ToNpcNeeds()
                : NpcNeeds.Default();

            var social = template?.social != null
                ? template.social.To()
                : new Social
                {
                    LeadershipScore = 0.0f,
                    LoyaltyToLeader01 = 0.5f,
                    JusticePerception01 = 0.5f,
                };

            int npcId = world.CreateNpc(dna, needs, social, _x, _y);
            if (template?.profile != null)
                world.NpcProfiles[npcId] = template.profile.ToProfile();

            world.SetFacing(npcId, _facing);

            // Nota:
            // - Non serve rebuild derived caches globale: NPC non tocca occlusion map.
            // - Se in futuro le cache includono occupancy, potremo aggiungerlo qui.
        }
    }
}
