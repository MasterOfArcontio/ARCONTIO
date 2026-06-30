using System;
using Arcontio.Core;
using Arcontio.Core.Config;
using Arcontio.Core.Save;
using NUnit.Framework;
using UnityJsonUtility = UnityEngine.JsonUtility;

namespace Arcontio.Tests
{
    // =============================================================================
    // WorldInventorySaveQaTests
    // =============================================================================
    /// <summary>
    /// <para>
    /// Test QA EditMode per il save/load canonico del modulo inventario typed.
    /// </para>
    ///
    /// <para><b>Principio architetturale: persistenza modulare senza legacy</b></para>
    /// <para>
    /// Questi test verificano che C3 salvi e carichi soltanto inventari typed e
    /// componenti stack. I vecchi campi save del cibo privato restano solo come
    /// compatibilita' passiva dei DTO storici e non devono creare stato runtime.
    /// </para>
    ///
    /// <para><b>Struttura interna:</b></para>
    /// <list type="bullet">
    ///   <item><b>Snapshot</b>: builder globale include la sezione inventory typed.</item>
    ///   <item><b>Restore</b>: loader globale ripristina entry, slot e stack.</item>
    ///   <item><b>Validazione</b>: snapshot incoerenti falliscono prima della mutazione.</item>
    ///   <item><b>Legacy off</b>: private food non viene serializzato ne' applicato.</item>
    /// </list>
    /// </summary>
    public sealed class WorldInventorySaveQaTests
    {
        [Test]
        public void BuildWorldSaveCapturesTypedInventoryAndObjectStacks()
        {
            var world = MakeWorld(out int npcId);
            Assert.That(world.TryAddInventoryItem(npcId, "berry", 4, out _, out _), Is.True);
            Assert.That(world.TryAddInventoryItem(npcId, "acorn", 1, NpcInventorySlotKind.HandLeft, 0, out int acornAdded, out string acornReason), Is.True, acornReason);
            Assert.That(acornAdded, Is.EqualTo(1));

            WorldSaveData data = WorldSaveBuilder.BuildFromWorld(world, savedAtTick: 42);

            Assert.That(data.schemaVersion, Is.EqualTo(WorldSaveData.CurrentSchemaVersion));
            Assert.That(data.inventory, Is.Not.Null);
            Assert.That(data.inventory.npcInventories.Length, Is.EqualTo(1));
            Assert.That(data.inventory.npcInventories[0].npcId, Is.EqualTo(npcId));
            Assert.That(data.inventory.npcInventories[0].entries.Length, Is.EqualTo(2));
            Assert.That(data.inventory.objectStacks.Length, Is.EqualTo(2));
        }

        [Test]
        public void WorldSaveJsonDoesNotSerializeLegacyPrivateFood()
        {
            var world = MakeWorld(out int npcId);
            Assert.That(world.TryAddInventoryItem(npcId, "berry", 2, out _, out _), Is.True);

            WorldSaveData data = WorldSaveBuilder.BuildFromWorld(world, savedAtTick: 42);
            string json = UnityJsonUtility.ToJson(data, prettyPrint: true);

            Assert.That(data.npcPrivateFood, Is.Empty);
            Assert.That(data.npcLastPrivateFoodConsumeTicks, Is.Empty);
            Assert.That(json.Contains("npcPrivateFood"), Is.False);
            Assert.That(json.Contains("npcLastPrivateFoodConsumeTicks"), Is.False);
            Assert.That(json.Contains("inventory"), Is.True);
            Assert.That(json.Contains("objectStacks"), Is.True);
        }

        [Test]
        public void ApplyWorldSaveRestoresTypedInventoryAndStackQuantities()
        {
            var source = MakeWorld(out int sourceNpcId);
            Assert.That(source.TryAddInventoryItem(sourceNpcId, "berry", 4, out _, out _), Is.True);
            Assert.That(source.TryAddInventoryItem(sourceNpcId, "acorn", 1, NpcInventorySlotKind.HandLeft, 0, out int acornAdded, out string acornReason), Is.True, acornReason);
            Assert.That(acornAdded, Is.EqualTo(1));
            WorldSaveData data = WorldSaveBuilder.BuildFromWorld(source, savedAtTick: 42);
            var target = MakeEmptyWorldWithDefs();

            bool applied = WorldSaveLoader.TryApplyObjectiveWorld(target, data, out string error);

            Assert.That(applied, Is.True, error);
            Assert.That(target.NpcInventories.ContainsKey(sourceNpcId), Is.True);
            Assert.That(target.NpcInventories[sourceNpcId].Entries.Count, Is.EqualTo(2));
            Assert.That(target.GetInventoryQuantity(sourceNpcId, "berry"), Is.EqualTo(4));
            Assert.That(target.GetInventoryQuantity(sourceNpcId, "acorn"), Is.EqualTo(1));

            bool foundHandItem = false;
            for (int i = 0; i < target.NpcInventories[sourceNpcId].Entries.Count; i++)
            {
                var entry = target.NpcInventories[sourceNpcId].Entries[i];
                if (target.Objects[entry.ObjectId].DefId == "acorn")
                    foundHandItem = entry.SlotKind == NpcInventorySlotKind.HandLeft;
            }

            Assert.That(foundHandItem, Is.True);
        }

        [Test]
        public void ApplyWorldSaveIgnoresLegacyPrivateFoodDtoWithoutCreatingRuntimeStore()
        {
            var source = MakeWorld(out int sourceNpcId);
            Assert.That(source.TryAddInventoryItem(sourceNpcId, "berry", 2, out _, out _), Is.True);
            WorldSaveData data = WorldSaveBuilder.BuildFromWorld(source, savedAtTick: 42);
            data.npcPrivateFood = new[] { new NpcPrivateFoodSaveData { npcId = sourceNpcId, units = 9 } };
            data.npcLastPrivateFoodConsumeTicks = new[] { new NpcPrivateFoodConsumeTickSaveData { npcId = sourceNpcId, lastConsumeTick = 123 } };
            var target = MakeEmptyWorldWithDefs();

            bool applied = WorldSaveLoader.TryApplyObjectiveWorld(target, data, out string error);

            Assert.That(applied, Is.True, error);
            Assert.That(target.GetInventoryQuantity(sourceNpcId, "berry"), Is.EqualTo(2));
            Assert.That(target.GetCarriedFoodQuantity(sourceNpcId), Is.EqualTo(2));
        }

        [Test]
        public void ApplyWorldSaveRejectsStackableInventoryEntryWithoutObjectStack()
        {
            var source = MakeWorld(out int sourceNpcId);
            Assert.That(source.TryAddInventoryItem(sourceNpcId, "berry", 2, out _, out _), Is.True);
            WorldSaveData data = WorldSaveBuilder.BuildFromWorld(source, savedAtTick: 42);
            data.inventory.objectStacks = Array.Empty<ObjectStackSaveData>();
            var target = MakeEmptyWorldWithDefs();

            bool applied = WorldSaveLoader.TryApplyObjectiveWorld(target, data, out string error);

            Assert.That(applied, Is.False);
            Assert.That(error, Does.Contain("manca di ObjectStackSaveData"));
            Assert.That(target.NpcInventories.ContainsKey(sourceNpcId), Is.False);
        }

        private static World MakeWorld(out int npcId)
        {
            var world = MakeEmptyWorldWithDefs();

            npcId = world.CreateNpc(
                NpcDnaProfile.CreateDefault("InventorySaveQaNpc", "qa", 0),
                new NpcNeeds(),
                new Arcontio.Core.Social(),
                1,
                1);

            return world;
        }

        private static World MakeEmptyWorldWithDefs()
        {
            var world = new World(new WorldConfig(new SimulationParams()));
            world.Global.InventoryMaxUnits = 12;
            world.Global.HandBulkCapacityUnits = 6;
            world.Global.BaseHandWeightUnits = 6;
            world.Global.StrengthHandWeightBonusUnits = 0;
            world.Global.BaseTotalWeightUnits = 30;
            world.Global.StrengthTotalWeightBonusUnits = 0;
            world.Global.StandardPackBulkCapacityUnits = 12;
            world.Global.StandardPackWeightCapacityUnits = 30;
            AddObjectDefs(world);
            return world;
        }

        private static void AddObjectDefs(World world)
        {
            world.ObjectDefs["berry"] = ItemDef("berry", 1, 1, true, "Item", "FoodItem", "NutritionValue");
            world.ObjectDefs["acorn"] = ItemDef("acorn", 1, 1, true, "Item", "FoodItem", "NutritionValue");
        }

        private static ObjectDef ItemDef(string id, int bulk, int weight, bool stackable, params string[] flags)
        {
            var properties = new System.Collections.Generic.List<ObjectPropertyKV>();
            for (int i = 0; i < flags.Length; i++)
            {
                float value = flags[i] == "NutritionValue" ? 0.25f : 1f;
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
                Properties = properties
            };
        }
    }
}
