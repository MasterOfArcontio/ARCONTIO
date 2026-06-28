using Arcontio.Core;
using Arcontio.Core.Commands.DevTools;
using Arcontio.Core.Config;
using NUnit.Framework;

namespace Arcontio.Tests
{
    // =============================================================================
    // DevEditObjectCommandQaTests
    // =============================================================================
    /// <summary>
    /// <para>
    /// Test QA EditMode per il comando DevTools che modifica proprieta' oggetto e
    /// stato porta senza passare dalla UI direttamente al World.
    /// </para>
    ///
    /// <para><b>Principio architetturale: mutazione solo nel comando Core</b></para>
    /// <para>
    /// Questi test coprono il boundary piu' delicato della modifica oggetti: il
    /// RightInspector deve poter richiedere cambi controllati, ma la coerenza di
    /// owner, porta, lock e cache fisiche resta responsabilita' del Core.
    /// </para>
    ///
    /// <para><b>Struttura interna:</b></para>
    /// <list type="bullet">
    ///   <item><b>Owner</b>: Community/NPC e rifiuto di NPC inesistente.</item>
    ///   <item><b>Food stock guard</b>: il comando generico non modifica owner stock.</item>
    ///   <item><b>Door</b>: chiusa/aperta/locked tramite API World autorizzate.</item>
    /// </list>
    /// </summary>
    public sealed class DevEditObjectCommandQaTests
    {
        // =============================================================================
        // GenericObjectOwnerCanMoveBetweenCommunityAndNpc
        // =============================================================================
        /// <summary>
        /// <para>
        /// Verifica che un oggetto non food-stock possa cambiare owner logico fra
        /// community e NPC esistente.
        /// </para>
        /// </summary>
        [Test]
        public void GenericObjectOwnerCanMoveBetweenCommunityAndNpc()
        {
            World world = CreateWorld();
            int npcId = world.CreateNpc(NpcDnaProfile.CreateDefault("Owner"), new NpcNeeds(), new Social(), 1, 1);
            int objectId = world.CreateObject("qa_crate", 2, 2, OwnerKind.Community, 0);

            DevEditObjectCommand.SetOwnerNpc(objectId, npcId).Execute(world, null);
            Assert.That(world.Objects[objectId].OwnerKind, Is.EqualTo(OwnerKind.Npc));
            Assert.That(world.Objects[objectId].OwnerId, Is.EqualTo(npcId));

            DevEditObjectCommand.SetOwnerCommunity(objectId).Execute(world, null);
            Assert.That(world.Objects[objectId].OwnerKind, Is.EqualTo(OwnerKind.Community));
            Assert.That(world.Objects[objectId].OwnerId, Is.EqualTo(0));
        }

        // =============================================================================
        // GenericObjectOwnerRejectsMissingNpc
        // =============================================================================
        /// <summary>
        /// <para>
        /// Verifica che un owner NPC inesistente non lasci l'oggetto in stato
        /// parzialmente modificato.
        /// </para>
        /// </summary>
        [Test]
        public void GenericObjectOwnerRejectsMissingNpc()
        {
            World world = CreateWorld();
            int objectId = world.CreateObject("qa_crate", 2, 2, OwnerKind.Community, 0);

            DevEditObjectCommand.SetOwnerNpc(objectId, 999).Execute(world, null);

            Assert.That(world.Objects[objectId].OwnerKind, Is.EqualTo(OwnerKind.Community));
            Assert.That(world.Objects[objectId].OwnerId, Is.EqualTo(0));
        }

        // =============================================================================
        // GenericOwnerCommandDoesNotEditFoodStockOwner
        // =============================================================================
        /// <summary>
        /// <para>
        /// Verifica che il comando oggetto generico non diventi un secondo percorso
        /// concorrente per modificare owner di food stock.
        /// </para>
        /// </summary>
        [Test]
        public void GenericOwnerCommandDoesNotEditFoodStockOwner()
        {
            World world = CreateWorld();
            int npcId = world.CreateNpc(NpcDnaProfile.CreateDefault("Owner"), new NpcNeeds(), new Social(), 1, 1);
            int objectId = world.CreateObject("food_stock", 2, 2, OwnerKind.Community, 0);
            world.SetFoodStock(objectId, new FoodStockComponent
            {
                Units = 3,
                OwnerKind = OwnerKind.Community,
                OwnerId = 0
            });

            DevEditObjectCommand.SetOwnerNpc(objectId, npcId).Execute(world, null);

            Assert.That(world.Objects[objectId].OwnerKind, Is.EqualTo(OwnerKind.Community));
            Assert.That(world.FoodStocks[objectId].OwnerKind, Is.EqualTo(OwnerKind.Community));
        }

        // =============================================================================
        // DoorCommandAppliesClosedOpenAndLockedStates
        // =============================================================================
        /// <summary>
        /// <para>
        /// Verifica gli stati porta principali, inclusa la chiusura locked sulle
        /// porte che dichiarano <c>IsLockable</c>.
        /// </para>
        /// </summary>
        [Test]
        public void DoorCommandAppliesClosedOpenAndLockedStates()
        {
            World world = CreateWorld();
            int doorId = world.CreateObject("qa_locked_door", 3, 3, OwnerKind.Community, 0);

            DevEditObjectCommand.SetDoorOpen(doorId).Execute(world, null);
            Assert.That(world.Objects[doorId].IsOpen, Is.True);
            Assert.That(world.Objects[doorId].IsLocked, Is.False);
            Assert.That(world.BlocksMovementAt(3, 3), Is.False);

            DevEditObjectCommand.SetDoorLocked(doorId).Execute(world, null);
            Assert.That(world.Objects[doorId].IsOpen, Is.False);
            Assert.That(world.Objects[doorId].IsLocked, Is.True);
            Assert.That(world.BlocksMovementAt(3, 3), Is.True);

            DevEditObjectCommand.SetDoorClosed(doorId).Execute(world, null);
            Assert.That(world.Objects[doorId].IsOpen, Is.False);
            Assert.That(world.Objects[doorId].IsLocked, Is.False);
            Assert.That(world.BlocksMovementAt(3, 3), Is.True);
        }

        // =============================================================================
        // LockedStateIsRejectedForNonLockableDoor
        // =============================================================================
        /// <summary>
        /// <para>
        /// Verifica che una porta non lockable non venga marcata locked da un comando
        /// dev-edit generico.
        /// </para>
        /// </summary>
        [Test]
        public void LockedStateIsRejectedForNonLockableDoor()
        {
            World world = CreateWorld();
            int doorId = world.CreateObject("qa_door", 3, 3, OwnerKind.Community, 0);

            DevEditObjectCommand.SetDoorLocked(doorId).Execute(world, null);

            Assert.That(world.Objects[doorId].IsOpen, Is.False);
            Assert.That(world.Objects[doorId].IsLocked, Is.False);
        }

        private static World CreateWorld()
        {
            var world = new World(new WorldConfig(new SimulationParams()));
            world.ObjectDefs["qa_crate"] = new ObjectDef
            {
                Id = "qa_crate",
                DisplayName = "QA Crate",
                IsInteractable = true,
                IsOccluder = false,
                BlocksMovement = false,
                BlocksVision = false
            };
            world.ObjectDefs["food_stock"] = new ObjectDef
            {
                Id = "food_stock",
                DisplayName = "QA Food Stock",
                IsInteractable = true,
                IsOccluder = false,
                BlocksMovement = false,
                BlocksVision = false
            };
            world.ObjectDefs["qa_door"] = new ObjectDef
            {
                Id = "qa_door",
                DisplayName = "QA Door",
                IsDoor = true,
                IsLockable = false,
                IsOccluder = true,
                BlocksMovement = true,
                BlocksVision = true
            };
            world.ObjectDefs["qa_locked_door"] = new ObjectDef
            {
                Id = "qa_locked_door",
                DisplayName = "QA Locked Door",
                IsDoor = true,
                IsLockable = true,
                IsOccluder = true,
                BlocksMovement = true,
                BlocksVision = true
            };

            return world;
        }
    }
}
