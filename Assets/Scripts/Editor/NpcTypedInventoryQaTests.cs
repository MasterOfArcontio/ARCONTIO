using System;
using System.Reflection;
using Arcontio.Core;
using Arcontio.Core.Config;
using NUnit.Framework;

namespace Arcontio.Tests
{
    // =============================================================================
    // NpcTypedInventoryQaTests
    // =============================================================================
    /// <summary>
    /// <para>
    /// Test QA EditMode per il nucleo C1 dell'inventario typed NPC.
    /// </para>
    ///
    /// <para><b>Principio architetturale: World come owner dell'inventario reale</b></para>
    /// <para>
    /// Questi test verificano soltanto lo store oggettivo e le API del
    /// <see cref="World"/>. Non coinvolgono UI, Decision, Job, save/load, furto o
    /// raccolta biologica, perche' C1 deve creare una base piccola ma solida prima
    /// di collegarla ai comportamenti successivi.
    /// </para>
    ///
    /// <para><b>Struttura interna:</b></para>
    /// <list type="bullet">
    ///   <item><b>Stacking fisico</b>: item uguali diventano un oggetto held con ObjectStackComponent.</item>
    ///   <item><b>Capienza</b>: aggiunte oltre bulk/peso disponibile vengono clampate o rifiutate.</item>
    ///   <item><b>Catalogo</b>: definizioni non trasportabili o mancanti vengono respinte.</item>
    ///   <item><b>Food query</b>: alimenti typed vengono selezionati tramite nutrizione catalogo.</item>
    /// </list>
    /// </summary>
    public sealed class NpcTypedInventoryQaTests
    {
        [Test]
        public void AddTypedItemsStacksInPackAndUpdatesCapacity()
        {
            var world = MakeWorld(out int npcId);

            bool first = world.TryAddInventoryItem(npcId, "berry", 2, out int addedFirst, out string firstReason);
            bool second = world.TryAddInventoryItem(npcId, "berry", 1, out int addedSecond, out string secondReason);

            Assert.That(first, Is.True, firstReason);
            Assert.That(second, Is.True, secondReason);
            Assert.That(addedFirst, Is.EqualTo(2));
            Assert.That(addedSecond, Is.EqualTo(1));
            Assert.That(world.GetInventoryQuantity(npcId, "berry"), Is.EqualTo(3));
            Assert.That(world.GetInventoryUsedBulkUnits(npcId), Is.EqualTo(3));
            Assert.That(InvokeWorldInt(world, "GetInventoryFreeBulkUnits", npcId), Is.EqualTo(0));
            Assert.That(world.NpcInventories[npcId].Entries.Count, Is.EqualTo(1));
            var entry = world.NpcInventories[npcId].Entries[0];
            Assert.That(entry.ObjectId, Is.GreaterThan(0));
            Assert.That(entry.SlotKind, Is.EqualTo(NpcInventorySlotKind.Pack));
            Assert.That(world.Objects[entry.ObjectId].IsHeld, Is.True);
            Assert.That(world.Objects[entry.ObjectId].HolderNpcId, Is.EqualTo(npcId));
            Assert.That(world.Objects[entry.ObjectId].DefId, Is.EqualTo("berry"));
            Assert.That(world.ObjectStacks[entry.ObjectId].Quantity, Is.EqualTo(3));
        }

        [Test]
        public void AddTypedItemsClampsToFreeCapacity()
        {
            var world = MakeWorld(out int npcId);

            bool added = world.TryAddInventoryItem(npcId, "acorn", 5, out int addedQuantity, out string reason);

            Assert.That(added, Is.True, reason);
            Assert.That(addedQuantity, Is.EqualTo(3));
            Assert.That(world.GetInventoryQuantity(npcId, "acorn"), Is.EqualTo(3));
            Assert.That(InvokeWorldInt(world, "GetInventoryFreeBulkUnits", npcId), Is.EqualTo(0));
        }

        [Test]
        public void AddTypedItemRejectsMissingOrNonTransportableDef()
        {
            var world = MakeWorld(out int npcId);

            bool missing = world.TryAddInventoryItem(npcId, "missing_def", 1, out _, out string missingReason);
            bool nonTransportable = world.TryAddInventoryItem(npcId, "bed_wood", 1, out _, out string bedReason);

            Assert.That(missing, Is.False);
            Assert.That(missingReason, Is.EqualTo("ObjectDefMissing"));
            Assert.That(nonTransportable, Is.False);
            Assert.That(bedReason, Is.EqualTo("ObjectDefNotTransportable"));
            Assert.That(world.GetInventoryUsedBulkUnits(npcId), Is.EqualTo(0));
        }

        [Test]
        public void RemoveTypedItemRequiresFullRequestedQuantity()
        {
            var world = MakeWorld(out int npcId);
            Assert.That(world.TryAddInventoryItem(npcId, "wood_log", 2, out _, out _), Is.True);

            bool tooMuch = world.TryRemoveInventoryItem(npcId, "wood_log", 3, out int removedTooMuch, out string tooMuchReason);
            bool removed = world.TryRemoveInventoryItem(npcId, "wood_log", 1, out int removedQuantity, out string reason);

            Assert.That(tooMuch, Is.False);
            Assert.That(removedTooMuch, Is.EqualTo(0));
            Assert.That(tooMuchReason, Is.EqualTo("InventoryItemInsufficient"));
            Assert.That(removed, Is.True, reason);
            Assert.That(removedQuantity, Is.EqualTo(1));
            Assert.That(world.GetInventoryQuantity(npcId, "wood_log"), Is.EqualTo(1));
        }

        [Test]
        public void HandSlotCanCarryMultipleSmallEquivalentItems()
        {
            var world = MakeWorld(out int npcId);

            bool first = world.TryAddInventoryItem(npcId, "berry", 2, NpcInventorySlotKind.HandLeft, 0, out int firstAdded, out string firstReason);
            bool second = world.TryAddInventoryItem(npcId, "berry", 2, NpcInventorySlotKind.HandLeft, 0, out int secondAdded, out string secondReason);

            Assert.That(first, Is.True, firstReason);
            Assert.That(second, Is.True, secondReason);
            Assert.That(firstAdded, Is.EqualTo(2));
            Assert.That(secondAdded, Is.EqualTo(2));
            Assert.That(world.NpcInventories[npcId].Entries.Count, Is.EqualTo(1));
            Assert.That(world.NpcInventories[npcId].Entries[0].SlotKind, Is.EqualTo(NpcInventorySlotKind.HandLeft));
            Assert.That(world.GetInventoryQuantity(npcId, "berry"), Is.EqualTo(4));
        }

        [Test]
        public void SelectBestFoodOnSelfUsesTypedNutrition()
        {
            var world = MakeWorld(out int npcId);
            Assert.That(world.TryAddInventoryItem(npcId, "acorn", 1, out _, out _), Is.True);
            Assert.That(world.TryAddInventoryItem(npcId, "berry", 1, out _, out _), Is.True);
            Assert.That(world.TryAddInventoryItem(npcId, "wood_log", 1, out _, out _), Is.True);

            bool found = world.SelectBestFoodOnSelf(npcId, out string foodDefId, out int quantity, out float nutritionValue);

            Assert.That(found, Is.True);
            Assert.That(foodDefId, Is.EqualTo("berry"));
            Assert.That(quantity, Is.EqualTo(1));
            Assert.That(nutritionValue, Is.EqualTo(0.32f).Within(0.0001f));
            Assert.That(world.HasEdibleFoodOnSelf(npcId), Is.True);
        }

        [Test]
        public void TypedInventoryAloneControlsCapacityAfterLegacyRemoval()
        {
            var world = MakeWorld(out int npcId);

            bool added = world.TryAddInventoryItem(npcId, "berry", 3, out int addedQuantity, out string reason);

            Assert.That(added, Is.True, reason);
            Assert.That(addedQuantity, Is.EqualTo(3));
            Assert.That(world.GetInventoryUsedBulkUnits(npcId), Is.EqualTo(3));
            Assert.That(InvokeWorldInt(world, "GetInventoryFreeBulkUnits", npcId), Is.EqualTo(0));
        }

        [Test]
        public void InventoryCapacityUsesNpcStrengthForHandAndTotalWeight()
        {
            var world = MakeWorld(out int npcId);

            Assert.That(world.GetInventoryHandWeightCapacityUnits(npcId), Is.EqualTo(8));
            Assert.That(world.GetInventoryTotalWeightCapacityUnits(npcId), Is.EqualTo(40));
        }

        [Test]
        public void AddTypedItemsClampsByTotalWeightCapacity()
        {
            var world = MakeWorld(out int npcId);

            bool added = world.TryAddInventoryItem(npcId, "iron_ore", 2, out int addedQuantity, out string reason);
            bool second = world.TryAddInventoryItem(npcId, "iron_ore", 1, out int secondAdded, out string secondReason);

            Assert.That(added, Is.True, reason);
            Assert.That(addedQuantity, Is.EqualTo(1));
            Assert.That(world.GetInventoryUsedWeightUnits(npcId), Is.EqualTo(25));
            Assert.That(InvokeWorldInt(world, "GetInventoryFreeWeightUnits", npcId), Is.EqualTo(15));
            Assert.That(second, Is.False);
            Assert.That(secondAdded, Is.EqualTo(0));
            Assert.That(secondReason, Is.EqualTo("InventoryFull"));
        }

        [Test]
        public void MoveInventoryObjectRejectsTargetHandOverWeight()
        {
            var world = MakeWorld(out int npcId);
            Assert.That(world.TryAddInventoryItem(npcId, "heavy_ore", 1, NpcInventorySlotKind.Pack, 0, out int added, out string addReason), Is.True, addReason);
            Assert.That(added, Is.EqualTo(1));

            int objectId = world.NpcInventories[npcId].Entries[0].ObjectId;
            bool moved = world.TryMoveInventoryObject(
                npcId,
                objectId,
                NpcInventorySlotKind.HandLeft,
                out InventoryMutationResult result,
                out string reason);

            Assert.That(moved, Is.False);
            Assert.That(result.HasMutation, Is.False);
            Assert.That(reason, Is.EqualTo("InventoryTargetSlotWeightFull"));
            Assert.That(world.NpcInventories[npcId].Entries[0].SlotKind, Is.EqualTo(NpcInventorySlotKind.Pack));
        }

        [Test]
        public void ObjectInventoryContractResolverResolvesPlacementFlags()
        {
            Type resolverType = ResolveInventoryContractResolverType();
            Type flagsType = ResolveInventoryPlacementFlagsType();
            var def = new ObjectDef
            {
                CanPlaceInHand = true,
                CanPlaceInContainer = true,
                CanEquipHead = true,
                CanEquipSidearm = true
            };

            var flags = InvokeStatic(resolverType, "ResolvePlacementFlags", def);

            Assert.That(HasReflectedFlag(flags, flagsType, "Hand"), Is.True);
            Assert.That(HasReflectedFlag(flags, flagsType, "Container"), Is.True);
            Assert.That(HasReflectedFlag(flags, flagsType, "EquipHead"), Is.True);
            Assert.That(HasReflectedFlag(flags, flagsType, "EquipSidearm"), Is.True);
            Assert.That(InvokeStatic(resolverType, "HasPlacement", def, Enum.Parse(flagsType, "Hand")), Is.EqualTo(true));
            Assert.That(InvokeStatic(resolverType, "HasPlacement", def, Enum.Parse(flagsType, "EquipFeet")), Is.EqualTo(false));
        }

        [Test]
        public void ObjectInventoryContractResolverNormalizesContainerKinds()
        {
            Type resolverType = ResolveInventoryContractResolverType();
            var def = new ObjectDef
            {
                IsContainer = true,
                ContainerKind = "SmallContainer"
            };

            Assert.That(InvokeStatic(resolverType, "IsContainer", def), Is.EqualTo(true));
            Assert.That(ResolveContainerKindName(resolverType, def), Is.EqualTo("Small"));

            def.ContainerKind = "Medium";
            Assert.That(ResolveContainerKindName(resolverType, def), Is.EqualTo("Medium"));

            def.ContainerKind = "LargeContainer";
            Assert.That(ResolveContainerKindName(resolverType, def), Is.EqualTo("Large"));

            def.ContainerKind = "UnexpectedKind";
            Assert.That(ResolveContainerKindName(resolverType, def), Is.EqualTo("None"));

            def.IsContainer = false;
            def.ContainerKind = "Small";
            Assert.That(ResolveContainerKindName(resolverType, def), Is.EqualTo("None"));
        }

        [Test]
        public void ObjectInventoryContractResolverKeepsLegacyItemFallbackButRejectsStaticObject()
        {
            Type resolverType = ResolveInventoryContractResolverType();
            var staticObject = new ObjectDef
            {
                Id = "wall_stone",
                DisplayName = "Stone Wall",
                Properties = new System.Collections.Generic.List<ObjectPropertyKV>()
            };

            var legacyItem = new ObjectDef
            {
                Id = "legacy_item",
                DisplayName = "Legacy Item",
                Properties = new System.Collections.Generic.List<ObjectPropertyKV>
                {
                    new ObjectPropertyKV { Key = "Item", Value = 1f }
                }
            };

            Assert.That(InvokeStatic(resolverType, "IsTransportable", staticObject, false), Is.EqualTo(false));
            Assert.That(InvokeStatic(resolverType, "CanPlaceInSlot", staticObject, NpcInventorySlotKind.Pack), Is.EqualTo(false));
            Assert.That(InvokeStatic(resolverType, "IsTransportable", legacyItem, false), Is.EqualTo(true));
            Assert.That(InvokeStatic(resolverType, "CanPlaceInSlot", legacyItem, NpcInventorySlotKind.HandLeft), Is.EqualTo(true));
            Assert.That(InvokeStatic(resolverType, "CanPlaceInSlot", legacyItem, NpcInventorySlotKind.Pack), Is.EqualTo(true));
        }

        [Test]
        public void ObjectInventoryStackResolverRejectsDurableStackDeclarations()
        {
            Type resolverType = ResolveInventoryStackResolverType();
            var stackableFood = FoodDef("berry", 0.32f);
            var singleItem = ItemDef("single_crate");
            singleItem.Stackable = false;
            var durableMisconfigured = ItemDef("durable_tool");
            durableMisconfigured.Stackable = true;
            durableMisconfigured.HasDurability = true;
            var entry = new NpcInventoryEntry
            {
                ObjectId = 10,
                SlotKind = NpcInventorySlotKind.Pack,
                ContainerObjectId = 0
            };

            Assert.That(InvokeStatic(resolverType, "CanUseStackComponent", stackableFood), Is.EqualTo(true));
            Assert.That(InvokeStatic(resolverType, "CanUseStackComponent", singleItem), Is.EqualTo(false));
            Assert.That(InvokeStatic(resolverType, "CanUseStackComponent", durableMisconfigured), Is.EqualTo(false));
            Assert.That(InvokeStatic(resolverType, "IsCatalogStackDeclarationValid", durableMisconfigured), Is.EqualTo(false));
            Assert.That(InvokeStatic(resolverType, "CanMergeStacks", stackableFood, entry, NpcInventorySlotKind.Pack, 0), Is.EqualTo(true));
            Assert.That(InvokeStatic(resolverType, "CanMergeStacks", stackableFood, entry, NpcInventorySlotKind.HandLeft, 0), Is.EqualTo(false));
            Assert.That(InvokeStatic(resolverType, "CanMergeStacks", stackableFood, entry, NpcInventorySlotKind.Pack, 99), Is.EqualTo(false));
        }

        [Test]
        public void DurableObjectCannotBeAddedAsStackQuantity()
        {
            var world = MakeWorld(out int npcId);
            world.ObjectDefs["durable_tool"] = DurableToolDef(stackable: true);

            bool added = world.TryAddInventoryItem(npcId, "durable_tool", 2, out int addedQuantity, out string reason);

            Assert.That(added, Is.False);
            Assert.That(addedQuantity, Is.EqualTo(0));
            Assert.That(reason, Is.EqualTo("ObjectDefDurabilityNotStackable"));
            Assert.That(world.GetInventoryQuantity(npcId, "durable_tool"), Is.EqualTo(0));
            Assert.That(world.ObjectStacks.Count, Is.EqualTo(0));
        }

        [Test]
        public void DurableObjectAddedAsSingleItemDoesNotCreateStackComponent()
        {
            var world = MakeWorld(out int npcId);
            world.ObjectDefs["durable_tool"] = DurableToolDef(stackable: true);

            bool added = world.TryAddInventoryItem(npcId, "durable_tool", 1, out int addedQuantity, out string reason);

            Assert.That(added, Is.True, reason);
            Assert.That(addedQuantity, Is.EqualTo(1));
            Assert.That(world.NpcInventories[npcId].Entries.Count, Is.EqualTo(1));
            int objectId = world.NpcInventories[npcId].Entries[0].ObjectId;
            Assert.That(world.Objects[objectId].DefId, Is.EqualTo("durable_tool"));
            Assert.That(world.ObjectStacks.ContainsKey(objectId), Is.False);
            Assert.That(world.GetInventoryQuantity(npcId, "durable_tool"), Is.EqualTo(1));
        }

        private static World MakeWorld(out int npcId)
        {
            var world = new World(new WorldConfig(new SimulationParams()));
            world.Global.HandBulkCapacityUnits = 6;
            world.Global.BaseHandWeightUnits = 4;
            world.Global.StrengthHandWeightBonusUnits = 8;
            world.Global.BaseTotalWeightUnits = 20;
            world.Global.StrengthTotalWeightBonusUnits = 40;
            world.Global.StandardPackBulkCapacityUnits = 3;
            world.Global.StandardPackWeightCapacityUnits = 100;
            AddObjectDefs(world);

            npcId = world.CreateNpc(
                NpcDnaProfile.CreateDefault("InventoryQaNpc", "qa", 0),
                new NpcNeeds(),
                new Social(),
                1,
                1);

            return world;
        }

        private static void AddObjectDefs(World world)
        {
            world.ObjectDefs["berry"] = FoodDef("berry", 0.32f);
            world.ObjectDefs["acorn"] = FoodDef("acorn", 0.18f);
            world.ObjectDefs["food_stock"] = FoodDef("food_stock", 0.45f, legacyStock: true);
            world.ObjectDefs["wood_log"] = ItemDef("wood_log", "Material", "BiologicalProduct");
            world.ObjectDefs["heavy_ore"] = PhysicalItemDef("heavy_ore", weightUnits: 9, bulkUnits: 1);
            world.ObjectDefs["iron_ore"] = PhysicalItemDef("iron_ore", weightUnits: 25, bulkUnits: 1);
            world.ObjectDefs["bed_wood"] = new ObjectDef
            {
                Id = "bed_wood",
                DisplayName = "Wood Bed",
                Properties = new System.Collections.Generic.List<ObjectPropertyKV>()
            };
        }

        private static ObjectDef FoodDef(string id, float nutrition, bool legacyStock = false)
        {
            var properties = new System.Collections.Generic.List<ObjectPropertyKV>
            {
                new ObjectPropertyKV { Key = "Item", Value = 1f },
                new ObjectPropertyKV { Key = "FoodItem", Value = 1f },
                new ObjectPropertyKV { Key = "NutritionValue", Value = nutrition }
            };

            if (legacyStock)
                properties.Add(new ObjectPropertyKV { Key = "FoodStock", Value = 1f });

            return new ObjectDef
            {
                Id = id,
                DisplayName = id,
                WeightUnits = 1,
                BulkUnits = 1,
                Stackable = true,
                CanPlaceInHand = true,
                CanPlaceInContainer = true,
                Properties = properties
            };
        }

        private static ObjectDef ItemDef(string id, params string[] flags)
        {
            var properties = new System.Collections.Generic.List<ObjectPropertyKV>
            {
                new ObjectPropertyKV { Key = "Item", Value = 1f }
            };

            for (int i = 0; i < flags.Length; i++)
                properties.Add(new ObjectPropertyKV { Key = flags[i], Value = 1f });

            return new ObjectDef
            {
                Id = id,
                DisplayName = id,
                WeightUnits = 1,
                BulkUnits = 1,
                Stackable = true,
                CanPlaceInHand = true,
                CanPlaceInContainer = true,
                Properties = properties
            };
        }

        private static ObjectDef PhysicalItemDef(string id, int weightUnits, int bulkUnits)
        {
            return new ObjectDef
            {
                Id = id,
                DisplayName = id,
                WeightUnits = weightUnits,
                BulkUnits = bulkUnits,
                Stackable = true,
                CanPlaceInHand = true,
                CanPlaceInContainer = true,
                Properties = new System.Collections.Generic.List<ObjectPropertyKV>
                {
                    new ObjectPropertyKV { Key = "Item", Value = 1f },
                    new ObjectPropertyKV { Key = "Material", Value = 1f }
                }
            };
        }

        private static ObjectDef DurableToolDef(bool stackable)
        {
            return new ObjectDef
            {
                Id = "durable_tool",
                DisplayName = "Durable Tool",
                WeightUnits = 1,
                BulkUnits = 1,
                Stackable = stackable,
                HasDurability = true,
                CanPlaceInHand = true,
                CanPlaceInContainer = true,
                Properties = new System.Collections.Generic.List<ObjectPropertyKV>
                {
                    new ObjectPropertyKV { Key = "Item", Value = 1f },
                    new ObjectPropertyKV { Key = "Tool", Value = 1f }
                }
            };
        }

        private static Type ResolveInventoryContractResolverType()
        {
            Type type = typeof(World).Assembly.GetType("Arcontio.Core.ObjectInventoryContractResolver");
            Assert.That(type, Is.Not.Null, "ObjectInventoryContractResolver deve esistere nel runtime Core.");
            return type;
        }

        private static Type ResolveInventoryPlacementFlagsType()
        {
            Type type = typeof(World).Assembly.GetType("Arcontio.Core.InventoryPlacementFlags");
            Assert.That(type, Is.Not.Null, "InventoryPlacementFlags deve esistere nel runtime Core.");
            return type;
        }

        private static Type ResolveInventoryStackResolverType()
        {
            Type type = typeof(World).Assembly.GetType("Arcontio.Core.ObjectInventoryStackResolver");
            Assert.That(type, Is.Not.Null, "ObjectInventoryStackResolver deve esistere nel runtime Core.");
            return type;
        }

        private static object InvokeStatic(Type type, string methodName, params object[] args)
        {
            MethodInfo method = type.GetMethod(methodName, BindingFlags.Public | BindingFlags.Static);
            Assert.That(method, Is.Not.Null, $"{methodName} deve essere un metodo pubblico statico.");
            return method.Invoke(null, args);
        }

        private static int InvokeWorldInt(World world, string methodName, params object[] args)
        {
            MethodInfo method = typeof(World).GetMethod(methodName, BindingFlags.Public | BindingFlags.Instance);
            Assert.That(method, Is.Not.Null, $"{methodName} deve essere una query pubblica del World.");
            object value = method.Invoke(world, args);
            Assert.That(value, Is.TypeOf<int>());
            return (int)value;
        }

        private static bool HasReflectedFlag(object flags, Type flagsType, string flagName)
        {
            int value = Convert.ToInt32(flags);
            int flag = Convert.ToInt32(Enum.Parse(flagsType, flagName));
            return (value & flag) == flag;
        }

        private static string ResolveContainerKindName(Type resolverType, ObjectDef def)
        {
            object value = InvokeStatic(resolverType, "ResolveContainerKind", def);
            return value == null ? string.Empty : value.ToString();
        }
    }
}
