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
        public void PickUpObjectCommandPublishesOnlyObjectPickedUpEvent()
        {
            var world = MakeWorld(out int npcId);
            int objectId = world.CreateObject("qa_crate", 1, 1, OwnerKind.Community, 0);
            var bus = new MessageBus();

            new PickUpObjectCommand(npcId, objectId).Execute(world, bus);

            Assert.That(bus.Count, Is.EqualTo(1));
            Assert.That(bus.TryDequeue(out var simEvent), Is.True);
            Assert.That(simEvent, Is.TypeOf<ObjectPickedUpEvent>());
            Assert.That(world.NpcInventories.ContainsKey(npcId), Is.False);
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
            Assert.That(world.NpcInventories.ContainsKey(npcId), Is.False);
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
            world.ObjectDefs["heavy_log"] = ItemDef("heavy_log", bulk: 8, weight: 1, stackable: true, "Item", "Material");
            world.ObjectDefs["qa_crate"] = ItemDef("qa_crate", bulk: 2, weight: 2, stackable: false, "Item");
        }

        private static ObjectDef ItemDef(string id, int bulk, int weight, bool stackable, params string[] flags)
        {
            var properties = new System.Collections.Generic.List<ObjectPropertyKV>();
            for (int i = 0; i < flags.Length; i++)
            {
                float value = flags[i] == "NutritionValue" ? 0.32f : 1f;
                properties.Add(new ObjectPropertyKV { Key = flags[i], Value = value });
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
