using Arcontio.Core;
using Arcontio.Core.Config;
using NUnit.Framework;

namespace Arcontio.Tests
{
    // =============================================================================
    // LegacyTheftSterilizationQaTests
    // =============================================================================
    /// <summary>
    /// <para>
    /// Test QA EditMode per la sterilizzazione v0.71.05.C6 del furto legacy.
    /// </para>
    ///
    /// <para><b>Principio architetturale: furto fuori produzione fino a modulo dedicato</b></para>
    /// <para>
    /// Questi test non definiscono il futuro sistema di furto. Bloccano invece la
    /// riattivazione accidentale dei vecchi percorsi Day9/Day10, che non passano
    /// dalla catena nuova <c>Decision -> Job -> Step -> Command -> Event</c>.
    /// </para>
    ///
    /// <para><b>Struttura interna:</b></para>
    /// <list type="bullet">
    ///   <item><b>Command legacy</b>: nessuna mutazione e nessun <c>FoodStolenEvent</c>.</item>
    ///   <item><b>Audit legacy</b>: nessun evento di cibo mancante o sospetto furto.</item>
    ///   <item><b>Job boundary</b>: nessuno step furto viene introdotto in C6.</item>
    /// </list>
    /// </summary>
    public sealed class LegacyTheftSterilizationQaTests
    {
        [Test]
        public void StealPrivateFoodCommandIsNoOpAndPublishesNoEvent()
        {
            var world = MakeWorld(out int thiefNpcId, out int victimNpcId);
            world.NpcPrivateFood[thiefNpcId] = 1;
            world.NpcPrivateFood[victimNpcId] = 5;
            var bus = new MessageBus();

            new StealPrivateFoodCommand(thiefNpcId, victimNpcId, 99).Execute(world, bus);

            Assert.That(world.NpcPrivateFood[thiefNpcId], Is.EqualTo(1));
            Assert.That(world.NpcPrivateFood[victimNpcId], Is.EqualTo(5));
            Assert.That(world.GetInventoryUsedUnits(thiefNpcId), Is.EqualTo(1));
            Assert.That(bus.Count, Is.EqualTo(0));
        }

        [Test]
        public void StealFromStockCommandIsNoOpAndPublishesNoEvent()
        {
            var world = MakeWorld(out int thiefNpcId, out int victimNpcId);
            int stockObjectId = world.CreateObject("food_stock", 1, 1, OwnerKind.Npc, victimNpcId);
            world.SetFoodStock(stockObjectId, new FoodStockComponent
            {
                Units = 4,
                OwnerKind = OwnerKind.Npc,
                OwnerId = victimNpcId
            });
            var bus = new MessageBus();

            new StealFromStockCommand(thiefNpcId, stockObjectId, 99).Execute(world, bus);

            Assert.That(world.FoodStocks[stockObjectId].Units, Is.EqualTo(4));
            Assert.That(world.Objects.ContainsKey(stockObjectId), Is.True);
            Assert.That(world.NpcPrivateFood[thiefNpcId], Is.EqualTo(0));
            Assert.That(world.NpcInventories.ContainsKey(thiefNpcId), Is.False);
            Assert.That(bus.Count, Is.EqualTo(0));
        }

        [Test]
        public void LegacyFoodInventoryAuditDoesNotPublishMissingOrTheftSuspicion()
        {
            var world = MakeWorld(out int thiefNpcId, out _);
            var audit = new FoodInventoryAuditSystem();
            var bus = new MessageBus();

            world.NpcPrivateFood[thiefNpcId] = 5;
            audit.Update(world, new Tick(1, 1f), bus, null);
            world.NpcPrivateFood[thiefNpcId] = 1;
            audit.Update(world, new Tick(2, 1f), bus, null);

            Assert.That(bus.Count, Is.EqualTo(0));
        }

        [Test]
        public void LegacyPrivateFoodAuditDoesNotPublishMissingFoodEvent()
        {
            var world = MakeWorld(out int thiefNpcId, out _);
            var audit = new PrivateFoodAuditSystem(auditEveryTicks: 1);
            var bus = new MessageBus();

            world.NpcPrivateFood[thiefNpcId] = 5;
            audit.Update(world, new Tick(1, 1f), bus, null);
            world.NpcPrivateFood[thiefNpcId] = 1;
            audit.Update(world, new Tick(2, 1f), bus, null);

            Assert.That(bus.Count, Is.EqualTo(0));
        }

        [Test]
        public void C6DoesNotIntroduceStealJobActionKind()
        {
            Assert.That(System.Enum.IsDefined(typeof(JobActionKind), "Steal"), Is.False);
        }

        private static World MakeWorld(out int thiefNpcId, out int victimNpcId)
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

            thiefNpcId = world.CreateNpc(
                NpcDnaProfile.CreateDefault("LegacyTheftQaThief", "qa", 0),
                new NpcNeeds(),
                new Social(),
                1,
                1);

            victimNpcId = world.CreateNpc(
                NpcDnaProfile.CreateDefault("LegacyTheftQaVictim", "qa", 0),
                new NpcNeeds(),
                new Social(),
                2,
                1);

            return world;
        }

        private static void AddObjectDefs(World world)
        {
            world.ObjectDefs["food_stock"] = new ObjectDef
            {
                Id = "food_stock",
                DisplayName = "Food Stock",
                BulkUnits = 1,
                WeightUnits = 1,
                Stackable = true,
                CanPlaceInHand = true,
                CanPlaceInContainer = true,
                IsInteractable = true,
                Properties = new System.Collections.Generic.List<ObjectPropertyKV>
                {
                    new ObjectPropertyKV { Key = "Item", Value = 1f },
                    new ObjectPropertyKV { Key = "FoodItem", Value = 1f },
                    new ObjectPropertyKV { Key = "FoodStock", Value = 1f },
                    new ObjectPropertyKV { Key = "NutritionValue", Value = 0.45f }
                }
            };
        }
    }
}
