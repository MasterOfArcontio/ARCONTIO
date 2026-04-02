// Assets/Scripts/Core/Commands/DevTools/DevPlaceObjectCommand.cs
using UnityEngine;

namespace Arcontio.Core.Commands.DevTools
{
    /// <summary>
    /// DevPlaceObjectCommand (DevMode v0 - MVP):
    /// piazza un oggetto in una cella della griglia.
    ///
    /// Requisiti dal documento:
    /// - La UI NON deve modificare direttamente il World.
    /// - Le modifiche devono passare tramite CommandBuffer. fileciteturn4file5
    ///
    /// Nota:
    /// - Questo comando è volutamente "brutale" (debug tool):
    ///   se in cella c'è già un oggetto, lo rimuoviamo e lo rimpiazziamo.
    /// - Manteniamo comunque lo standard "1 object per cell".
    /// </summary>
    public sealed class DevPlaceObjectCommand : ICommand
    {
        private readonly string _defId;
        private readonly int _x;
        private readonly int _y;

        public DevPlaceObjectCommand(string defId, int x, int y)
        {
            _defId = defId;
            _x = x;
            _y = y;
        }

        public void Execute(World world, MessageBus bus)
        {
            if (world == null) return;
            if (string.IsNullOrWhiteSpace(_defId)) return; 
            if (!world.InBounds(_x, _y)) return;

            // Se non esiste la definizione oggetto, non facciamo nulla.
            if (!world.TryGetObjectDef(_defId, out var def) || def == null)
            {
                Debug.LogWarning($"[DevTools] Place failed: unknown defId='{_defId}'.");
                return;
            }

            // Se esiste già un oggetto, lo distruggiamo (replace).
            int existing = world.GetObjectAt(_x, _y);
            if (existing >= 0)
            {
                // Difensivo: se la cella è "occupata" da un NPC come occupante,
                // rifiutiamo l'operazione per non creare stati impossibili.
                if (world.Objects.TryGetValue(existing, out var inst) && inst != null && inst.OccupantNpcId >= 0)
                {
                    Debug.LogWarning($"[DevTools] Place blocked: cell ({_x},{_y}) object={existing} is occupied by NPC={inst.OccupantNpcId}.");
                    return;
                }

                world.DestroyObject(existing);
            }

            // Ownership devtool: default None.
            int objId = world.CreateObject(_defId, _x, _y, OwnerKind.Community, 0);
            
            if (_defId == "food_stock") // o come si chiama nel tuo catalogo
            {
                world.SetFoodStock(objId, new FoodStockComponent
                {
                    Units = 1,
                    OwnerKind = OwnerKind.Community,   // oppure None/Public
                    OwnerId = 0                        // se richiesto dal tipo
                });
            }

            // Rebuild globale delle cache derivate (MVP). fileciteturn4file5
            world.RebuildDerivedCachesGlobal();
        }
    }
}
