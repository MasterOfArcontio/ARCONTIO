using System.Collections.Generic;
using Arcontio.Core;

namespace Arcontio.Core.Save
{
    // =============================================================================
    // WorldInventorySaveLoader
    // =============================================================================
    /// <summary>
    /// <para>
    /// Loader dedicato della sezione inventario typed del salvataggio globale.
    /// </para>
    ///
    /// <para><b>Principio architetturale: load inventory senza scorciatoie gameplay</b></para>
    /// <para>
    /// Il loader non usa command e non chiama <c>TryAddInventoryItem</c>, perche'
    /// quelle API creano o mutano oggetti gameplay. Qui dobbiamo ripristinare
    /// identita' storiche gia' presenti in <see cref="WorldSaveData.objects"/>:
    /// objectId, holder, entryId, slot e quantita' stack devono restare quelli
    /// dello snapshot.
    /// </para>
    ///
    /// <para><b>Struttura interna:</b></para>
    /// <list type="bullet">
    ///   <item><b>CanApplyInventorySafely</b>: valida tutta la sezione senza mutare.</item>
    ///   <item><b>TryApplyInventory</b>: sostituisce ObjectStacks e NpcInventories typed.</item>
    ///   <item><b>Helper locali</b>: risolvono riferimenti nello snapshot senza dipendere dal loader globale.</item>
    /// </list>
    /// </summary>
    public static class WorldInventorySaveLoader
    {
        // =============================================================================
        // TryApplyInventory
        // =============================================================================
        /// <summary>
        /// <para>
        /// Applica la sezione inventory typed a un World gia' popolato con NPC e
        /// oggetti dello snapshot.
        /// </para>
        /// </summary>
        public static bool TryApplyInventory(World world, WorldSaveData data, out string error)
        {
            if (!CanApplyInventorySafely(world, data, out error))
                return false;

            var inventoryData = data != null ? data.inventory : null;

            world.ObjectStacks.Clear();
            world.NpcInventories.Clear();

            if (inventoryData == null)
            {
                error = string.Empty;
                return true;
            }

            var stacks = inventoryData.objectStacks;
            if (stacks != null)
            {
                for (int i = 0; i < stacks.Length; i++)
                {
                    var dto = stacks[i];
                    world.ObjectStacks[dto.objectId] = new ObjectStackComponent(dto.quantity);
                }
            }

            var inventories = inventoryData.npcInventories;
            if (inventories != null)
            {
                for (int i = 0; i < inventories.Length; i++)
                {
                    var dto = inventories[i];
                    var state = world.EnsureNpcInventory(dto.npcId);
                    state.Entries.Clear();
                    state.NextEntryId = dto.nextEntryId <= 0 ? 1 : dto.nextEntryId;

                    var entries = dto.entries;
                    if (entries == null)
                        continue;

                    for (int j = 0; j < entries.Length; j++)
                    {
                        var entryDto = entries[j];
                        state.Entries.Add(new NpcInventoryEntry
                        {
                            EntryId = entryDto.entryId,
                            ObjectId = entryDto.objectId,
                            SlotKind = (NpcInventorySlotKind)entryDto.slotKind,
                            ContainerObjectId = entryDto.containerObjectId
                        });
                    }
                }
            }

            error = string.Empty;
            return true;
        }

        // =============================================================================
        // CanApplyInventorySafely
        // =============================================================================
        /// <summary>
        /// <para>
        /// Valida la sezione inventory typed senza mutare il <see cref="World"/>.
        /// </para>
        ///
        /// <para><b>Principio architetturale: riferimenti espliciti, zero ricostruzione implicita</b></para>
        /// <para>
        /// Il preflight non crea entry mancanti, non deduce inventari dagli oggetti
        /// held e non migra cibo legacy. Ogni entry deve puntare a un oggetto che
        /// esiste nello snapshot, deve essere held dallo stesso NPC e deve avere uno
        /// slot ammesso dal catalogo oggetti.
        /// </para>
        /// </summary>
        public static bool CanApplyInventorySafely(World world, WorldSaveData data, out string error)
        {
            if (world == null)
            {
                error = "WorldInventorySaveLoader: world nullo.";
                return false;
            }

            if (data == null)
            {
                error = "WorldInventorySaveLoader: WorldSaveData nullo.";
                return false;
            }

            var inventoryData = data.inventory;
            if (inventoryData == null)
            {
                error = string.Empty;
                return true;
            }

            if (!ValidateObjectStacks(world, data, inventoryData, out error))
                return false;

            return ValidateNpcInventories(world, data, inventoryData, out error);
        }

        private static bool ValidateObjectStacks(
            World world,
            WorldSaveData data,
            WorldInventorySaveData inventoryData,
            out string error)
        {
            var seenStacks = new HashSet<int>();
            var stacks = inventoryData.objectStacks;
            if (stacks == null)
            {
                error = string.Empty;
                return true;
            }

            for (int i = 0; i < stacks.Length; i++)
            {
                var dto = stacks[i];
                if (dto == null)
                {
                    error = $"WorldInventorySaveLoader: ObjectStackSaveData nullo all'indice {i}.";
                    return false;
                }

                if (dto.objectId <= 0 || !TryResolveSavedOrLoadedObject(world, data, dto.objectId, out var objectData, out var objectInstance))
                {
                    error = $"WorldInventorySaveLoader: objectStack riferisce objectId inesistente {dto.objectId}.";
                    return false;
                }

                if (!seenStacks.Add(dto.objectId))
                {
                    error = $"WorldInventorySaveLoader: objectStack duplicato per objectId {dto.objectId}.";
                    return false;
                }

                if (dto.quantity <= 0)
                {
                    error = $"WorldInventorySaveLoader: objectStack objectId {dto.objectId} ha quantity non positiva.";
                    return false;
                }

                string defId = ResolveObjectDefId(objectData, objectInstance);
                if (!world.TryGetObjectDef(defId, out var def) || def == null)
                {
                    error = $"WorldInventorySaveLoader: objectStack objectId {dto.objectId} ha defId sconosciuto '{defId}'.";
                    return false;
                }

                if (!def.Stackable)
                {
                    error = $"WorldInventorySaveLoader: objectStack objectId {dto.objectId} usa def non stackable '{defId}'.";
                    return false;
                }
            }

            error = string.Empty;
            return true;
        }

        private static bool ValidateNpcInventories(
            World world,
            WorldSaveData data,
            WorldInventorySaveData inventoryData,
            out string error)
        {
            var seenNpcIds = new HashSet<int>();
            var seenInventoryObjectIds = new HashSet<int>();
            var inventories = inventoryData.npcInventories;
            if (inventories == null)
            {
                error = string.Empty;
                return true;
            }

            for (int i = 0; i < inventories.Length; i++)
            {
                var inventoryDto = inventories[i];
                if (inventoryDto == null)
                {
                    error = $"WorldInventorySaveLoader: NpcInventorySaveData nullo all'indice {i}.";
                    return false;
                }

                if (inventoryDto.npcId <= 0 || !WillNpcExistAfterLoad(world, data, inventoryDto.npcId))
                {
                    error = $"WorldInventorySaveLoader: inventario riferisce npcId inesistente {inventoryDto.npcId}.";
                    return false;
                }

                if (!seenNpcIds.Add(inventoryDto.npcId))
                {
                    error = $"WorldInventorySaveLoader: inventario duplicato per npcId {inventoryDto.npcId}.";
                    return false;
                }

                if (inventoryDto.nextEntryId < 1)
                {
                    error = $"WorldInventorySaveLoader: nextEntryId invalido per npcId {inventoryDto.npcId}.";
                    return false;
                }

                if (!ValidateInventoryEntries(world, data, inventoryData, inventoryDto, seenInventoryObjectIds, out error))
                    return false;
            }

            error = string.Empty;
            return true;
        }

        private static bool ValidateInventoryEntries(
            World world,
            WorldSaveData data,
            WorldInventorySaveData inventoryData,
            NpcInventorySaveData inventoryDto,
            HashSet<int> seenInventoryObjectIds,
            out string error)
        {
            var seenEntryIds = new HashSet<int>();
            var entries = inventoryDto.entries;
            if (entries == null)
            {
                error = string.Empty;
                return true;
            }

            for (int i = 0; i < entries.Length; i++)
            {
                var entry = entries[i];
                if (entry == null)
                {
                    error = $"WorldInventorySaveLoader: inventory entry nulla per npcId {inventoryDto.npcId} indice {i}.";
                    return false;
                }

                if (entry.entryId <= 0 || entry.entryId >= inventoryDto.nextEntryId)
                {
                    error = $"WorldInventorySaveLoader: entryId {entry.entryId} incoerente con nextEntryId {inventoryDto.nextEntryId} per npcId {inventoryDto.npcId}.";
                    return false;
                }

                if (!seenEntryIds.Add(entry.entryId))
                {
                    error = $"WorldInventorySaveLoader: entryId duplicato {entry.entryId} per npcId {inventoryDto.npcId}.";
                    return false;
                }

                if (entry.objectId <= 0 || !TryResolveSavedOrLoadedObject(world, data, entry.objectId, out var objectData, out var objectInstance))
                {
                    error = $"WorldInventorySaveLoader: inventory entry riferisce objectId inesistente {entry.objectId}.";
                    return false;
                }

                if (!seenInventoryObjectIds.Add(entry.objectId))
                {
                    error = $"WorldInventorySaveLoader: objectId {entry.objectId} presente in piu' entry inventario.";
                    return false;
                }

                if (!IsObjectHeldByNpc(objectData, objectInstance, inventoryDto.npcId))
                {
                    error = $"WorldInventorySaveLoader: objectId {entry.objectId} non e' held da npcId {inventoryDto.npcId}.";
                    return false;
                }

                if (!IsValidSlot(entry.slotKind))
                {
                    error = $"WorldInventorySaveLoader: slotKind invalido {entry.slotKind} per objectId {entry.objectId}.";
                    return false;
                }

                string defId = ResolveObjectDefId(objectData, objectInstance);
                if (!world.TryGetObjectDef(defId, out var def) || def == null)
                {
                    error = $"WorldInventorySaveLoader: inventory objectId {entry.objectId} ha defId sconosciuto '{defId}'.";
                    return false;
                }

                var slot = (NpcInventorySlotKind)entry.slotKind;
                if (!CanObjectBePlacedInSlot(def, slot))
                {
                    error = $"WorldInventorySaveLoader: objectId {entry.objectId} def '{defId}' non ammesso nello slot {slot}.";
                    return false;
                }

                if (def.Stackable && !HasObjectStack(inventoryData, entry.objectId))
                {
                    error = $"WorldInventorySaveLoader: objectId stackable {entry.objectId} manca di ObjectStackSaveData.";
                    return false;
                }

                if (entry.containerObjectId < 0)
                {
                    error = $"WorldInventorySaveLoader: containerObjectId negativo per objectId {entry.objectId}.";
                    return false;
                }

                if (entry.containerObjectId > 0 && !WillObjectExistAfterLoad(world, data, entry.containerObjectId))
                {
                    error = $"WorldInventorySaveLoader: containerObjectId {entry.containerObjectId} inesistente per objectId {entry.objectId}.";
                    return false;
                }
            }

            error = string.Empty;
            return true;
        }

        private static bool TryResolveSavedOrLoadedObject(
            World world,
            WorldSaveData data,
            int objectId,
            out WorldObjectSaveData objectData,
            out WorldObjectInstance objectInstance)
        {
            objectData = null;
            objectInstance = null;

            if (data != null && data.objects != null)
            {
                for (int i = 0; i < data.objects.Length; i++)
                {
                    var dto = data.objects[i];
                    if (dto != null && dto.objectId == objectId)
                    {
                        objectData = dto;
                        return true;
                    }
                }
            }

            return world != null
                && world.Objects.TryGetValue(objectId, out objectInstance)
                && objectInstance != null;
        }

        private static bool WillNpcExistAfterLoad(World world, WorldSaveData data, int npcId)
        {
            if (npcId <= 0)
                return false;

            if (world != null && world.ExistsNpc(npcId))
                return true;

            if (data == null || data.npcs == null)
                return false;

            for (int i = 0; i < data.npcs.Length; i++)
            {
                var dto = data.npcs[i];
                if (dto != null && dto.npcId == npcId)
                    return true;
            }

            return false;
        }

        private static bool WillObjectExistAfterLoad(World world, WorldSaveData data, int objectId)
        {
            if (objectId <= 0)
                return false;

            if (world != null && world.Objects.ContainsKey(objectId))
                return true;

            if (data == null || data.objects == null)
                return false;

            for (int i = 0; i < data.objects.Length; i++)
            {
                var dto = data.objects[i];
                if (dto != null && dto.objectId == objectId)
                    return true;
            }

            return false;
        }

        private static string ResolveObjectDefId(WorldObjectSaveData objectData, WorldObjectInstance objectInstance)
        {
            if (objectData != null)
                return objectData.defId ?? string.Empty;

            return objectInstance != null ? objectInstance.DefId ?? string.Empty : string.Empty;
        }

        private static bool IsObjectHeldByNpc(WorldObjectSaveData objectData, WorldObjectInstance objectInstance, int npcId)
        {
            if (objectData != null)
                return objectData.isHeld && objectData.holderNpcId == npcId;

            return objectInstance != null && objectInstance.IsHeld && objectInstance.HolderNpcId == npcId;
        }

        private static bool IsValidSlot(int slotKind)
        {
            return slotKind == (int)NpcInventorySlotKind.HandLeft
                || slotKind == (int)NpcInventorySlotKind.HandRight
                || slotKind == (int)NpcInventorySlotKind.Pack;
        }

        private static bool CanObjectBePlacedInSlot(ObjectDef def, NpcInventorySlotKind slot)
        {
            // Il loader non deve possedere una seconda copia delle regole
            // inventario: se un save e' valido a runtime, deve esserlo con la
            // stessa grammatica usata dal World.
            return ObjectInventoryContractResolver.CanPlaceInSlot(def, slot);
        }

        private static bool HasObjectStack(WorldInventorySaveData inventoryData, int objectId)
        {
            if (inventoryData == null || inventoryData.objectStacks == null)
                return false;

            for (int i = 0; i < inventoryData.objectStacks.Length; i++)
            {
                var stack = inventoryData.objectStacks[i];
                if (stack != null && stack.objectId == objectId)
                    return true;
            }

            return false;
        }
    }
}
