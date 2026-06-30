using System.Collections.Generic;
using Arcontio.Core;

namespace Arcontio.Core.Save
{
    // =============================================================================
    // WorldInventorySaveBuilder
    // =============================================================================
    /// <summary>
    /// <para>
    /// Builder passivo della sezione inventario typed del salvataggio globale.
    /// </para>
    ///
    /// <para><b>Principio architetturale: estrazione modulare senza legacy</b></para>
    /// <para>
    /// Il builder legge esclusivamente <see cref="World.NpcInventories"/> e
    /// <see cref="World.ObjectStacks"/>. Non consulta <c>NpcPrivateFood</c>, non
    /// migra cibo legacy e non deduce inventari dagli oggetti held: salva solo lo
    /// stato typed gia' presente nel modulo inventario.
    /// </para>
    ///
    /// <para><b>Struttura interna:</b></para>
    /// <list type="bullet">
    ///   <item><b>BuildFromWorld</b>: crea il DTO radice inventory.</item>
    ///   <item><b>BuildNpcInventories</b>: copia entry e contatore locale per NPC.</item>
    ///   <item><b>BuildObjectStacks</b>: copia i componenti quantita' per objectId.</item>
    /// </list>
    /// </summary>
    public static class WorldInventorySaveBuilder
    {
        // =============================================================================
        // BuildFromWorld
        // =============================================================================
        /// <summary>
        /// <para>
        /// Costruisce la sezione inventory typed a partire dal <see cref="World"/>.
        /// </para>
        /// </summary>
        public static WorldInventorySaveData BuildFromWorld(World world)
        {
            // Il builder globale garantisce normalmente world non nullo, ma qui
            // manteniamo un fallback vuoto per rendere il modulo robusto nei test.
            if (world == null)
                return new WorldInventorySaveData();

            return new WorldInventorySaveData
            {
                npcInventories = BuildNpcInventories(world),
                objectStacks = BuildObjectStacks(world)
            };
        }

        private static NpcInventorySaveData[] BuildNpcInventories(World world)
        {
            var npcIds = new List<int>(world.NpcInventories.Keys);
            npcIds.Sort();

            var result = new List<NpcInventorySaveData>(npcIds.Count);

            for (int i = 0; i < npcIds.Count; i++)
            {
                int npcId = npcIds[i];
                if (!world.NpcInventories.TryGetValue(npcId, out var inventory) || inventory == null)
                    continue;

                var entries = new List<NpcInventoryEntrySaveData>(inventory.Entries.Count);
                for (int j = 0; j < inventory.Entries.Count; j++)
                {
                    var entry = inventory.Entries[j];
                    if (entry == null)
                        continue;

                    // Non risolviamo DefId o quantita' qui: l'entry deve restare
                    // un riferimento alla fisica oggetto, non una riga astratta.
                    entries.Add(new NpcInventoryEntrySaveData
                    {
                        entryId = entry.EntryId,
                        objectId = entry.ObjectId,
                        slotKind = (int)entry.SlotKind,
                        containerObjectId = entry.ContainerObjectId
                    });
                }

                result.Add(new NpcInventorySaveData
                {
                    npcId = npcId,
                    nextEntryId = inventory.NextEntryId,
                    entries = entries.ToArray()
                });
            }

            return result.ToArray();
        }

        private static ObjectStackSaveData[] BuildObjectStacks(World world)
        {
            var objectIds = new List<int>(world.ObjectStacks.Keys);
            objectIds.Sort();

            var result = new List<ObjectStackSaveData>(objectIds.Count);

            for (int i = 0; i < objectIds.Count; i++)
            {
                int objectId = objectIds[i];
                if (!world.ObjectStacks.TryGetValue(objectId, out var stack) || stack == null)
                    continue;

                result.Add(new ObjectStackSaveData
                {
                    objectId = objectId,
                    quantity = stack.Quantity
                });
            }

            return result.ToArray();
        }
    }
}
