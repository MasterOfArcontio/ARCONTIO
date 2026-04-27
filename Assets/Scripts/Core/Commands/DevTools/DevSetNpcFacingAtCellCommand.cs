// Assets/Scripts/Core/Commands/DevTools/DevSetNpcFacingAtCellCommand.cs
using UnityEngine;

namespace Arcontio.Core.Commands.DevTools
{
    /// <summary>
    /// DevSetNpcFacingAtCellCommand (DevMode v1):
    /// imposta l'orientamento (facing) dell'NPC presente in una cella.
    ///
    /// Motivazione:
    /// - In ARCONTIO la FOV è orientata (4 direzioni), quindi cambiare facing è fondamentale
    ///   per testare percezione / LOS / landmark debug in modo controllato.
    /// </summary>
    public sealed class DevSetNpcFacingAtCellCommand : ICommand
    {
        private readonly int _x;
        private readonly int _y;
        private readonly CardinalDirection _facing;

        public DevSetNpcFacingAtCellCommand(int x, int y, CardinalDirection facing)
        {
            _x = x;
            _y = y;
            _facing = facing;
        }

        public void Execute(World world, MessageBus bus)
        {
            if (world == null) return;
            if (!world.InBounds(_x, _y)) return;

            if (!world.TryGetNpcAt(_x, _y, out int npcId) || npcId <= 0)
            {
                // Silenzioso: è un tool di debug, spesso clicchi "a vuoto" cercando la cella giusta.
                return;
            }

            if (!world.ExistsNpc(npcId))
                return;

            world.SetFacing(npcId, _facing);

            // Nota:
            // - Non facciamo altro. Il prossimo tick di Perception/Scan userà il nuovo facing.
        }
    }
}
