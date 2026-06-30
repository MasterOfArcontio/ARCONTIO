using Arcontio.Core;
using Arcontio.Core.Config;
using NUnit.Framework;

namespace Arcontio.Tests
{
    // =============================================================================
    // NpcInventoryCommandQaTests
    // =============================================================================
    /// <summary>
    /// <para>
    /// Test QA EditMode per il layer C2 di comandi ed eventi dell'inventario typed.
    /// </para>
    ///
    /// <para><b>Principio architetturale: Command -> World -> Event</b></para>
    /// <para>
    /// Questi test verificano che le mutazioni inventario passino dai command
    /// autorizzati, che il <see cref="World"/> resti l'owner della scrittura reale
    /// e che il <see cref="MessageBus"/> riceva un solo evento canonico per ogni
    /// mutazione osservabile.
    /// </para>
    ///
    /// <para><b>Struttura interna:</b></para>
    /// <list type="bullet">
    ///   <item><b>Add/Remove</b>: eventi inventario solo per mutazioni non spaziali.</item>
    ///   <item><b>Move</b>: cambio slot interno senza pickup/drop.</item>
    ///   <item><b>Anti-duplicazione</b>: pickup/drop restano eventi oggetto canonici.</item>
    /// </list>
    /// </summary>
    public sealed class NpcInventoryCommandQaTests
    {
        [Test]
        public void AddInventoryItemCommandPublishesSingleAddedEvent()
        {
            var world = MakeWorld(out int npcId);
            var bus = new MessageBus();

            new AddInventoryItemCommand(npcId, "berry", 2).Execute(world, bus);

            Assert.That(bus.Count, Is.EqualTo(1));
            Assert.That(bus.TryDequeue(out var simEvent), Is.True);
            var added = simEvent as InventoryItemAddedEvent;
            Assert.That(added, Is.Not.Null);
            Assert.That(added.NpcId, Is.EqualTo(npcId));
            Assert.That(added.DefId, Is.EqualTo("berry"));
            Assert.That(added.Quantity, Is.EqualTo(2));
            Assert.That(added.SlotKind, Is.EqualTo(NpcInventorySlotKind.Pack));
            Assert.That(world.GetInventoryQuantity(npcId, "berry"), Is.EqualTo(2));
        }

        [Test]
        public void AddInventoryItemCommandPublishesActualPartialQuantity()
        {
            var world = MakeWorld(out int npcId);
            world.Global.InventoryMaxUnits = 3;
            world.Global.StandardPackBulkCapacityUnits = 3;
            var bus = new MessageBus();

            new AddInventoryItemCommand(npcId, "berry", 5).Execute(world, bus);

            Assert.That(bus.Count, Is.EqualTo(1));
            Assert.That(bus.TryDequeue(out var simEvent), Is.True);
            var added = simEvent as InventoryItemAddedEvent;
            Assert.That(added, Is.Not.Null);
            Assert.That(added.Quantity, Is.EqualTo(3));
            Assert.That(world.GetInventoryQuantity(npcId, "berry"), Is.EqualTo(3));
        }

        [Test]
        public void RemoveInventoryItemCommandPublishesSingleRemovedEvent()
        {
            var world = MakeWorld(out int npcId);
            Assert.That(world.TryAddInventoryItem(npcId, "berry", 3, out _, out _), Is.True);
            var bus = new MessageBus();

            new RemoveInventoryItemCommand(npcId, "berry", 2).Execute(world, bus);

            Assert.That(bus.Count, Is.EqualTo(1));
            Assert.That(bus.TryDequeue(out var simEvent), Is.True);
            var removed = simEvent as InventoryItemRemovedEvent;
            Assert.That(removed, Is.Not.Null);
            Assert.That(removed.NpcId, Is.EqualTo(npcId));
            Assert.That(removed.DefId, Is.EqualTo("berry"));
            Assert.That(removed.Quantity, Is.EqualTo(2));
            Assert.That(removed.SlotKind, Is.EqualTo(NpcInventorySlotKind.Pack));
            Assert.That(world.GetInventoryQuantity(npcId, "berry"), Is.EqualTo(1));
        }

        [Test]
        public void RemoveInventoryItemCommandFailsWithoutEventWhenInsufficient()
        {
            var world = MakeWorld(out int npcId);
            Assert.That(world.TryAddInventoryItem(npcId, "berry", 1, out _, out _), Is.True);
            var bus = new MessageBus();

            new RemoveInventoryItemCommand(npcId, "berry", 2).Execute(world, bus);

            Assert.That(bus.Count, Is.EqualTo(0));
            Assert.That(world.GetInventoryQuantity(npcId, "berry"), Is.EqualTo(1));
        }

        [Test]
        public void MoveInventoryObjectCommandMovesPackToHandAndPublishesSingleMovedEvent()
        {
            var world = MakeWorld(out int npcId);
            Assert.That(world.TryAddInventoryItem(npcId, "berry", 2, out _, out _), Is.True);
            int objectId = world.NpcInventories[npcId].Entries[0].ObjectId;
            var bus = new MessageBus();

            new MoveInventoryObjectCommand(npcId, objectId, NpcInventorySlotKind.HandLeft).Execute(world, bus);

            Assert.That(bus.Count, Is.EqualTo(1));
            Assert.That(bus.TryDequeue(out var simEvent), Is.True);
            var moved = simEvent as InventoryItemMovedEvent;
            Assert.That(moved, Is.Not.Null);
            Assert.That(moved.ObjectId, Is.EqualTo(objectId));
            Assert.That(moved.DefId, Is.EqualTo("berry"));
            Assert.That(moved.Quantity, Is.EqualTo(2));
            Assert.That(moved.PreviousSlotKind, Is.EqualTo(NpcInventorySlotKind.Pack));
            Assert.That(moved.SlotKind, Is.EqualTo(NpcInventorySlotKind.HandLeft));
            Assert.That(world.NpcInventories[npcId].Entries[0].SlotKind, Is.EqualTo(NpcInventorySlotKind.HandLeft));
        }

        [Test]
        public void MoveInventoryObjectCommandSameSlotIsNoOpWithoutEvent()
        {
            var world = MakeWorld(out int npcId);
            Assert.That(world.TryAddInventoryItem(npcId, "berry", 1, out _, out _), Is.True);
            int objectId = world.NpcInventories[npcId].Entries[0].ObjectId;
            var bus = new MessageBus();

            new MoveInventoryObjectCommand(npcId, objectId, NpcInventorySlotKind.Pack).Execute(world, bus);

            Assert.That(bus.Count, Is.EqualTo(0));
            Assert.That(world.NpcInventories[npcId].Entries[0].SlotKind, Is.EqualTo(NpcInventorySlotKind.Pack));
        }

        [Test]
        public void MoveInventoryObjectCommandRejectsTargetWithoutCapacityAndPublishesNoEvent()
        {
            var world = MakeWorld(out int npcId);
            Assert.That(world.TryAddInventoryItem(npcId, "heavy_log", 1, out _, out _), Is.True);
            int objectId = world.NpcInventories[npcId].Entries[0].ObjectId;
            var bus = new MessageBus();

            new MoveInventoryObjectCommand(npcId, objectId, NpcInventorySlotKind.HandLeft).Execute(world, bus);

            Assert.That(bus.Count, Is.EqualTo(0));
            Assert.That(world.NpcInventories[npcId].Entries[0].SlotKind, Is.EqualTo(NpcInventorySlotKind.Pack));
        }

        [Test]
        public void ConsumeInventoryItemCommandConsumesTypedFoodAndPublishesSingleFoodEvent()
        {
            var world = MakeWorld(out int npcId);
            Assert.That(world.TryAddInventoryItem(npcId, "berry", 2, out _, out _), Is.True);
            float hungerBefore = world.Needs[npcId].GetValue(NeedKind.Hunger);
            var bus = new MessageBus();

            new ConsumeInventoryItemCommand(npcId, "berry").Execute(world, bus);

            Assert.That(world.GetInventoryQuantity(npcId, "berry"), Is.EqualTo(1));
            Assert.That(world.Needs[npcId].GetValue(NeedKind.Hunger), Is.EqualTo(hungerBefore - 0.32f).Within(0.0001f));
            Assert.That(bus.Count, Is.EqualTo(1));
            Assert.That(bus.TryDequeue(out var simEvent), Is.True);
            var consumed = simEvent as FoodConsumedEvent;
            Assert.That(consumed, Is.Not.Null);
            Assert.That(consumed.NpcId, Is.EqualTo(npcId));
            Assert.That(consumed.SourceKind, Is.EqualTo("Inventory"));
            Assert.That(consumed.FoodDefId, Is.EqualTo("berry"));
            Assert.That(consumed.NutritionValue, Is.EqualTo(0.32f).Within(0.0001f));
            Assert.That(consumed.RemainingUnits, Is.EqualTo(1));
            Assert.That(consumed.Depleted, Is.False);
        }

        [Test]
        public void ConsumeInventoryItemCommandAutoSelectsMostNutritiousFood()
        {
            var world = MakeWorld(out int npcId);
            Assert.That(world.TryAddInventoryItem(npcId, "acorn", 1, out _, out _), Is.True);
            Assert.That(world.TryAddInventoryItem(npcId, "berry", 1, out _, out _), Is.True);
            var bus = new MessageBus();

            new ConsumeInventoryItemCommand(npcId).Execute(world, bus);

            Assert.That(world.GetInventoryQuantity(npcId, "berry"), Is.EqualTo(0));
            Assert.That(world.GetInventoryQuantity(npcId, "acorn"), Is.EqualTo(1));
            Assert.That(bus.TryDequeue(out var simEvent), Is.True);
            var consumed = simEvent as FoodConsumedEvent;
            Assert.That(consumed, Is.Not.Null);
            Assert.That(consumed.FoodDefId, Is.EqualTo("berry"));
        }

        [Test]
        public void ConsumeInventoryItemCommandRejectsNonFoodWithoutEvent()
        {
            var world = MakeWorld(out int npcId);
            Assert.That(world.TryAddInventoryItem(npcId, "wood_log", 1, out _, out _), Is.True);
            float hungerBefore = world.Needs[npcId].GetValue(NeedKind.Hunger);
            var bus = new MessageBus();

            new ConsumeInventoryItemCommand(npcId, "wood_log").Execute(world, bus);

            Assert.That(world.GetInventoryQuantity(npcId, "wood_log"), Is.EqualTo(1));
            Assert.That(world.Needs[npcId].GetValue(NeedKind.Hunger), Is.EqualTo(hungerBefore).Within(0.0001f));
            Assert.That(bus.Count, Is.EqualTo(0));
        }

        [Test]
        public void ConsumeInventoryItemCommandEmptyInventoryDoesNotMutate()
        {
            var world = MakeWorld(out int npcId);
            float hungerBefore = world.Needs[npcId].GetValue(NeedKind.Hunger);
            var bus = new MessageBus();

            new ConsumeInventoryItemCommand(npcId).Execute(world, bus);

            Assert.That(world.Needs[npcId].GetValue(NeedKind.Hunger), Is.EqualTo(hungerBefore).Within(0.0001f));
            Assert.That(bus.Count, Is.EqualTo(0));
        }

        [Test]
        public void ConsumeInventoryItemCommandRemovesLastStackObject()
        {
            var world = MakeWorld(out int npcId);
            Assert.That(world.TryAddInventoryItem(npcId, "berry", 1, out _, out _), Is.True);
            int objectId = world.NpcInventories[npcId].Entries[0].ObjectId;
            var bus = new MessageBus();

            new ConsumeInventoryItemCommand(npcId, "berry").Execute(world, bus);

            Assert.That(world.GetInventoryQuantity(npcId, "berry"), Is.EqualTo(0));
            Assert.That(world.NpcInventories[npcId].Entries.Count, Is.EqualTo(0));
            Assert.That(world.ObjectStacks.ContainsKey(objectId), Is.False);
            Assert.That(world.Objects.ContainsKey(objectId), Is.False);
            Assert.That(bus.TryDequeue(out var simEvent), Is.True);
            var consumed = simEvent as FoodConsumedEvent;
            Assert.That(consumed, Is.Not.Null);
            Assert.That(consumed.FoodObjectId, Is.EqualTo(objectId));
            Assert.That(consumed.Depleted, Is.True);
        }

        [Test]
        public void EatPrivateFoodCommandUsesTypedInventoryAlias()
        {
            var world = MakeWorld(out int npcId);
            Assert.That(world.TryAddInventoryItem(npcId, "acorn", 1, out _, out _), Is.True);
            var bus = new MessageBus();

            new EatPrivateFoodCommand(npcId).Execute(world, bus);

            Assert.That(world.GetInventoryQuantity(npcId, "acorn"), Is.EqualTo(0));
            Assert.That(bus.TryDequeue(out var simEvent), Is.True);
            var consumed = simEvent as FoodConsumedEvent;
            Assert.That(consumed, Is.Not.Null);
            Assert.That(consumed.SourceKind, Is.EqualTo("Inventory"));
            Assert.That(consumed.FoodDefId, Is.EqualTo("acorn"));
        }

        [Test]
        public void EatPrivateFoodCommandEmptyInventoryDoesNotMutate()
        {
            var world = MakeWorld(out int npcId);
            float hungerBefore = world.Needs[npcId].GetValue(NeedKind.Hunger);
            var bus = new MessageBus();

            new EatPrivateFoodCommand(npcId).Execute(world, bus);

            Assert.That(world.Needs[npcId].GetValue(NeedKind.Hunger), Is.EqualTo(hungerBefore).Within(0.0001f));
            Assert.That(bus.Count, Is.EqualTo(0));
        }

        [Test]
        public void PickUpObjectCommandPublishesOnlyObjectPickedUpEvent()
        {
            var world = MakeWorld(out int npcId);
            int objectId = world.CreateObject("qa_crate", 1, 1, OwnerKind.Community, 0);
            var bus = new MessageBus();

            new PickUpObjectCommand(npcId, objectId).Execute(world, bus);

            Assert.That(bus.Count, Is.EqualTo(1));
            Assert.That(bus.TryDequeue(out var simEvent), Is.True);
            Assert.That(simEvent, Is.TypeOf<ObjectPickedUpEvent>());
            Assert.That(world.Objects[objectId].IsHeld, Is.True);
            Assert.That(world.Objects[objectId].HolderNpcId, Is.EqualTo(npcId));
            Assert.That(world.GetObjectAt(1, 1), Is.EqualTo(-1));
            Assert.That(world.NpcInventories.ContainsKey(npcId), Is.True);
            Assert.That(world.NpcInventories[npcId].Entries.Count, Is.EqualTo(1));
            Assert.That(world.NpcInventories[npcId].Entries[0].ObjectId, Is.EqualTo(objectId));
            Assert.That(world.NpcInventories[npcId].Entries[0].SlotKind, Is.EqualTo(NpcInventorySlotKind.Pack));
        }

        [Test]
        public void DropObjectCommandPublishesOnlyObjectDroppedEvent()
        {
            var world = MakeWorld(out int npcId);
            int objectId = world.CreateObject("qa_crate", 1, 1, OwnerKind.Community, 0);
            Assert.That(world.TryPickUpObject(npcId, objectId, out _, out _, out string pickupReason), Is.True, pickupReason);
            var bus = new MessageBus();

            new DropObjectCommand(npcId, objectId, 2, 1).Execute(world, bus);

            Assert.That(bus.Count, Is.EqualTo(1));
            Assert.That(bus.TryDequeue(out var simEvent), Is.True);
            Assert.That(simEvent, Is.TypeOf<ObjectDroppedEvent>());
            Assert.That(world.Objects[objectId].IsHeld, Is.False);
            Assert.That(world.Objects[objectId].HolderNpcId, Is.EqualTo(0));
            Assert.That(world.GetObjectAt(2, 1), Is.EqualTo(objectId));
            Assert.That(world.NpcInventories[npcId].Entries.Count, Is.EqualTo(0));
        }

        [Test]
        public void PickUpObjectCommandRejectsWithoutCapacityAndDoesNotMutate()
        {
            var world = MakeWorld(out int npcId);
            world.Global.StandardPackBulkCapacityUnits = 1;
            Assert.That(world.TryAddInventoryItem(npcId, "berry", 1, out _, out _), Is.True);
            int objectId = world.CreateObject("qa_crate", 1, 1, OwnerKind.Community, 0);
            var bus = new MessageBus();

            new PickUpObjectCommand(npcId, objectId).Execute(world, bus);

            Assert.That(bus.Count, Is.EqualTo(0));
            Assert.That(world.Objects[objectId].IsHeld, Is.False);
            Assert.That(world.Objects[objectId].HolderNpcId, Is.EqualTo(0));
            Assert.That(world.GetObjectAt(1, 1), Is.EqualTo(objectId));
            Assert.That(world.NpcInventories[npcId].Entries.Count, Is.EqualTo(1));
            Assert.That(world.NpcInventories[npcId].Entries[0].ObjectId, Is.Not.EqualTo(objectId));
        }

        [Test]
        public void PickUpObjectCommandRejectsNonTransportableObjectAndDoesNotMutate()
        {
            var world = MakeWorld(out int npcId);
            int objectId = world.CreateObject("static_wall", 1, 1, OwnerKind.Community, 0);
            var bus = new MessageBus();

            new PickUpObjectCommand(npcId, objectId).Execute(world, bus);

            Assert.That(bus.Count, Is.EqualTo(0));
            Assert.That(world.Objects[objectId].IsHeld, Is.False);
            Assert.That(world.GetObjectAt(1, 1), Is.EqualTo(objectId));
            Assert.That(world.NpcInventories.ContainsKey(npcId), Is.False);
        }

        [Test]
        public void PickUpStackableObjectKeepsSeparatePhysicalObject()
        {
            var world = MakeWorld(out int npcId);
            Assert.That(world.TryAddInventoryItem(npcId, "berry", 2, out _, out _), Is.True);
            int existingStackObjectId = world.NpcInventories[npcId].Entries[0].ObjectId;
            int groundObjectId = world.CreateObject("berry", 1, 1, OwnerKind.Community, 0);
            world.ObjectStacks[groundObjectId] = new ObjectStackComponent(3);
            var bus = new MessageBus();

            new PickUpObjectCommand(npcId, groundObjectId).Execute(world, bus);

            Assert.That(bus.Count, Is.EqualTo(1));
            Assert.That(world.NpcInventories[npcId].Entries.Count, Is.EqualTo(2));
            Assert.That(world.ObjectStacks[existingStackObjectId].Quantity, Is.EqualTo(2));
            Assert.That(world.ObjectStacks[groundObjectId].Quantity, Is.EqualTo(3));
        }

        [Test]
        public void DropObjectCommandRejectsOccupiedCellAndKeepsHeldInventoryEntry()
        {
            var world = MakeWorld(out int npcId);
            int objectId = world.CreateObject("qa_crate", 1, 1, OwnerKind.Community, 0);
            int blockerId = world.CreateObject("berry", 2, 1, OwnerKind.Community, 0);
            Assert.That(world.TryPickUpObject(npcId, objectId, out _, out _, out string pickupReason), Is.True, pickupReason);
            var bus = new MessageBus();

            new DropObjectCommand(npcId, objectId, 2, 1).Execute(world, bus);

            Assert.That(bus.Count, Is.EqualTo(0));
            Assert.That(world.Objects[objectId].IsHeld, Is.True);
            Assert.That(world.Objects[objectId].HolderNpcId, Is.EqualTo(npcId));
            Assert.That(world.GetObjectAt(2, 1), Is.EqualTo(blockerId));
            Assert.That(world.NpcInventories[npcId].Entries.Count, Is.EqualTo(1));
            Assert.That(world.NpcInventories[npcId].Entries[0].ObjectId, Is.EqualTo(objectId));
        }

        [Test]
        public void DropObjectCommandRejectsHeldObjectWithoutInventoryEntry()
        {
            var world = MakeWorld(out int npcId);
            int objectId = world.CreateObject("qa_crate", 1, 1, OwnerKind.Community, 0);
            Assert.That(world.TryPickUpObject(npcId, objectId, out _, out _, out string pickupReason), Is.True, pickupReason);
            world.NpcInventories[npcId].Entries.Clear();
            var bus = new MessageBus();

            new DropObjectCommand(npcId, objectId, 2, 1).Execute(world, bus);

            Assert.That(bus.Count, Is.EqualTo(0));
            Assert.That(world.Objects[objectId].IsHeld, Is.True);
            Assert.That(world.Objects[objectId].HolderNpcId, Is.EqualTo(npcId));
            Assert.That(world.GetObjectAt(2, 1), Is.EqualTo(-1));
        }

        private static World MakeWorld(out int npcId)
        {
            var world = new World(new WorldConfig(new SimulationParams()));
            world.Global.InventoryMaxUnits = 12;
            world.Global.HandBulkCapacityUnits = 6;
            world.Global.BaseHandWeightUnits = 4;
            world.Global.StrengthHandWeightBonusUnits = 0;
            world.Global.BaseTotalWeightUnits = 30;
            world.Global.StrengthTotalWeightBonusUnits = 0;
            world.Global.StandardPackBulkCapacityUnits = 12;
            world.Global.StandardPackWeightCapacityUnits = 30;
            AddObjectDefs(world);

            npcId = world.CreateNpc(
                NpcDnaProfile.CreateDefault("InventoryCommandQaNpc", "qa", 0),
                new NpcNeeds(),
                new Social(),
                1,
                1);

            return world;
        }

        private static void AddObjectDefs(World world)
        {
            world.ObjectDefs["berry"] = ItemDef("berry", bulk: 1, weight: 1, stackable: true, "Item", "FoodItem", "NutritionValue");
            world.ObjectDefs["acorn"] = ItemDef("acorn", bulk: 1, weight: 1, stackable: true, "Item", "FoodItem", "NutritionValueLow");
            world.ObjectDefs["wood_log"] = ItemDef("wood_log", bulk: 1, weight: 1, stackable: true, "Item", "Material");
            world.ObjectDefs["heavy_log"] = ItemDef("heavy_log", bulk: 8, weight: 1, stackable: true, "Item", "Material");
            world.ObjectDefs["qa_crate"] = ItemDef("qa_crate", bulk: 2, weight: 2, stackable: false, "Item");
            world.ObjectDefs["static_wall"] = new ObjectDef
            {
                Id = "static_wall",
                DisplayName = "Static Wall",
                BulkUnits = 1,
                WeightUnits = 1,
                Stackable = false,
                CanPlaceInHand = false,
                CanPlaceInContainer = false,
                Properties = new System.Collections.Generic.List<ObjectPropertyKV>()
            };
        }

        private static ObjectDef ItemDef(string id, int bulk, int weight, bool stackable, params string[] flags)
        {
            var properties = new System.Collections.Generic.List<ObjectPropertyKV>();
            for (int i = 0; i < flags.Length; i++)
            {
                float value = flags[i] == "NutritionValue" ? 0.32f : 1f;
                string key = flags[i];
                if (flags[i] == "NutritionValueLow")
                {
                    key = "NutritionValue";
                    value = 0.18f;
                }

                properties.Add(new ObjectPropertyKV { Key = key, Value = value });
            }

            return new ObjectDef
            {
                Id = id,
                DisplayName = id,
                BulkUnits = bulk,
                WeightUnits = weight,
                Stackable = stackable,
                CanPlaceInHand = true,
                CanPlaceInContainer = true,
                IsInteractable = true,
                Properties = properties
            };
        }
    }
}
