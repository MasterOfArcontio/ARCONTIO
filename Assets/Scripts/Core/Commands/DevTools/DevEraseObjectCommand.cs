// Assets/Scripts/Core/Commands/DevTools/DevEraseObjectCommand.cs
using UnityEngine;

namespace Arcontio.Core.Commands.DevTools
{
    /// <summary>
    /// DevEraseObjectCommand (DevMode v0 - MVP):
    /// rimuove l'oggetto presente in una cella.
    ///
    /// UX reference:
    /// - Documento: "Click destro → rimuove oggetto". fileciteturn4file3
    /// </summary>
    public sealed class DevEraseObjectCommand : ICommand
    {
        private readonly int _x;
        private readonly int _y;

        public DevEraseObjectCommand(int x, int y)
        {
            _x = x;
            _y = y;
        }

        public void Execute(World world, MessageBus bus)
        {
            if (world == null) return;
            if (!world.InBounds(_x, _y)) return;

            int existing = world.GetObjectAt(_x, _y);
            if (existing < 0) return;

            // Difensivo: non rimuoviamo oggetti occupati.
            if (world.Objects.TryGetValue(existing, out var inst) && inst != null && inst.OccupantNpcId >= 0)
            {
                Debug.LogWarning($"[DevTools] Erase blocked: cell ({_x},{_y}) object={existing} is occupied by NPC={inst.OccupantNpcId}.");
                return;
            }

            world.DestroyObject(existing);

            // Rebuild globale delle cache derivate (MVP).
            world.RebuildDerivedCachesGlobal();
        }
    }
}
