// Assets/Scripts/Core/Commands/DevTools/DevSpawnNpcCommand.cs
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

            // Name deterministico e leggibile in debug.
            // Nota: _nextNpcId è interno al world, quindi non possiamo usarlo qui senza creare una API;
            // usiamo un nome "a coordinate" (univoco abbastanza per scenari di test).
            var dna = NpcDnaProfile.CreateDefault($"DEV_NPC({_x},{_y})");

            // Needs "safe": nessuna emergenza iniziale.
            var needs = new Needs
            {
                Hunger01 = 0.0f,
                Fatigue01 = 0.0f,
                Morale01 = 0.5f,
                IsHungry = false,
                IsTired = false,
            };

            var social = new Social
            {
                LeadershipScore = 0.0f,
                LoyaltyToLeader01 = 0.5f,
                JusticePerception01 = 0.5f,
            };

            int npcId = world.CreateNpc(dna, needs, social, _x, _y);
            world.SetFacing(npcId, _facing);

            // Nota:
            // - Non serve rebuild derived caches globale: NPC non tocca occlusion map.
            // - Se in futuro le cache includono occupancy, potremo aggiungerlo qui.
        }
    }
}
