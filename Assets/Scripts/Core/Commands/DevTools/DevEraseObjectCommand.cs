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
        private readonly int _objectId;
        private readonly int _x;
        private readonly int _y;

        public DevEraseObjectCommand(int x, int y)
        {
            // Percorso legacy/dev: cancella cio' che il World vede nella cella.
            _objectId = -1;
            _x = x;
            _y = y;
        }

        public DevEraseObjectCommand(int objectId)
        {
            // Percorso ArcGraph selection: cancella l'entita' selezionata per id,
            // evitando errori quando lo sprite si estende oltre la cella base.
            _objectId = objectId;
            _x = -1;
            _y = -1;
        }

        public void Execute(World world, MessageBus bus)
        {
            if (world == null) return;

            int existing = ResolveObjectId(world);
            if (existing < 0) return;

            // Difensivo: non rimuoviamo oggetti occupati.
            if (world.Objects.TryGetValue(existing, out var inst) && inst != null && inst.OccupantNpcId >= 0)
            {
                Debug.LogWarning($"[DevTools] Erase blocked: object={existing} is occupied by NPC={inst.OccupantNpcId}.");
                return;
            }

            world.DestroyObject(existing);

            // Rebuild globale delle cache derivate (MVP).
            world.RebuildDerivedCachesGlobal();
        }

        private int ResolveObjectId(World world)
        {
            // L'id selezionato ha priorita': e' il contratto piu' stabile per UI,
            // inspector e menu hover.
            if (_objectId > 0)
                return world.Objects.ContainsKey(_objectId) ? _objectId : -1;

            // Fallback storico: utile per strumenti dev che ragionano ancora per
            // cella e non hanno un target object esplicito.
            if (!world.InBounds(_x, _y))
                return -1;

            return world.GetObjectAt(_x, _y);
        }
    }
}
