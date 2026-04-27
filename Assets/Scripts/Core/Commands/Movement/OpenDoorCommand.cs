using UnityEngine;

namespace Arcontio.Core
{
    /// <summary>
    /// OpenDoorCommand:
    /// Apre una porta non bloccata. Prodotto dal MovementSystem quando un NPC
    /// deve attraversare una porta chiusa e non bloccata sul proprio percorso.
    ///
    /// Contratto:
    /// - Se l'oggetto non esiste → uscita silenziosa.
    /// - Se l'oggetto non è una porta (IsDoor=false) → errore e uscita.
    /// - Se la porta è bloccata (IsLocked=true) → fallimento silenzioso.
    ///   NOTA FUTURA: quando il sistema inventario sarà implementato, verificare
    ///   qui se l'NPC ha la chiave (ObjectDef.KeyId contro inventario NPC).
    /// - Se la porta non è bloccata → world.SetDoorOpen(objectId, true) e
    ///   pubblicazione di DoorOpenedEvent sul bus.
    /// </summary>
    public sealed class OpenDoorCommand : ICommand
    {
        /// <summary>NPC che sta cercando di aprire la porta.</summary>
        public readonly int NpcId;

        /// <summary>ID runtime della porta da aprire.</summary>
        public readonly int ObjectId;

        public OpenDoorCommand(int npcId, int objectId)
        {
            NpcId    = npcId;
            ObjectId = objectId;
        }

        public string Name => nameof(OpenDoorCommand);

        public void Execute(World world, MessageBus bus)
        {
            // 1. Verifica che l'oggetto esista
            if (!world.Objects.TryGetValue(ObjectId, out var instance) || instance == null)
                return;

            // 2. Verifica che sia una porta
            if (!world.TryGetObjectDef(instance.DefId, out var def) || def == null)
                return;

            if (!def.IsDoor)
            {
                Debug.LogError($"[OpenDoorCommand] obj={ObjectId} (def='{instance.DefId}') non è una porta.");
                return;
            }

            // 3. Porta bloccata: fallimento silenzioso
            // NOTA FUTURA (inventario NPC): verificare ObjectDef.KeyId contro inventario NPC.
            if (instance.IsLocked)
                return;

            // 4. Apre la porta (aggiorna stato + OcclusionMap)
            world.SetDoorOpen(ObjectId, true);

            // 5. Pubblica evento — consente memory encoding futuro
            //    (altri NPC nelle vicinanze possono sentire o vedere la porta aprirsi)
            bus.Publish(new DoorOpenedEvent(NpcId, ObjectId, instance.CellX, instance.CellY));
        }
    }
}
