using Arcontio.Core;
using Arcontio.Core.Config;
using System;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;

namespace Arcontio.Tests
{
    // =============================================================================
    // PerceptionInventoryJobActionExecutorQaTests
    // =============================================================================
    /// <summary>
    /// <para>
    /// Test QA EditMode per executor di observe, search, pick up e drop.
    /// </para>
    ///
    /// <para><b>Contratti prima dell'integrazione runtime</b></para>
    /// <para>
    /// I test non verificano percezione o inventario reali. Verificano che gli step
    /// abbiano precondizioni e risultati coerenti per la futura integrazione.
    /// </para>
    ///
    /// <para><b>Struttura interna:</b></para>
    /// <list type="bullet">
    ///   <item><b>Observe/Search</b>: esiti percettivi placeholder.</item>
    ///   <item><b>PickUp/Drop</b>: validazione target materiale.</item>
    /// </list>
    /// </summary>
    public sealed class PerceptionInventoryJobActionExecutorQaTests
    {
        // =============================================================================
        // ObserveSearchPickAndDropExposeExpectedContracts
        // =============================================================================
        /// <summary>
        /// <para>
        /// Verifica i risultati minimi per gli step di osservazione, ricerca e
        /// manipolazione oggetto.
        /// </para>
        ///
        /// <para><b>Precondizioni dichiarative</b></para>
        /// <para>
        /// Pick e drop devono fallire senza target, mentre observe e search restano
        /// step validi anche prima dei sistemi completi.
        /// </para>
        ///
        /// <para><b>Struttura interna:</b></para>
        /// <list type="bullet">
        ///   <item><b>Observe</b>: Succeeded.</item>
        ///   <item><b>Search</b>: Running senza payload, Succeeded con payload.</item>
        ///   <item><b>Pick/Drop</b>: successo solo con target.</item>
        /// </list>
        /// </summary>
        [Test]
        public void ObserveSearchPickAndDropExposeExpectedContracts()
        {
            // Arrange: contesto minimale, senza store e senza World.
            var executor = new PerceptionInventoryJobActionExecutor();
            var context = new JobActionExecutionContext(1, "job", 1, Vector2Int.zero, null);

            // Act: copriamo contratti positivi e negativi.
            var observe = executor.Execute(JobAction.Simple("observe", JobActionKind.Observe, "osserva"), context);
            var searchPending = executor.Execute(JobAction.Simple("search", JobActionKind.Search, "cerca"), context);
            var searchDone = executor.Execute(new JobAction("search-done", JobActionKind.Search, "cerca", false, Vector2Int.zero, -1, 0, "found"), context);
            var pickMissing = executor.Execute(JobAction.Simple("pick-missing", JobActionKind.PickUp, "prendi"), context);
            var pickOk = executor.Execute(new JobAction("pick", JobActionKind.PickUp, "prendi", false, Vector2Int.zero, 33, 0, string.Empty), context);
            var dropOk = executor.Execute(new JobAction("drop", JobActionKind.Drop, "deposita", true, new Vector2Int(2, 2), -1, 0, string.Empty), context);

            // Assert: gli esiti sono leggibili dalla state machine.
            Assert.That(observe.Status, Is.EqualTo(StepResultStatus.Succeeded));
            Assert.That(searchPending.Status, Is.EqualTo(StepResultStatus.Running));
            Assert.That(searchDone.Status, Is.EqualTo(StepResultStatus.Succeeded));
            Assert.That(pickMissing.FailureReason, Is.EqualTo(JobFailureReason.MissingTarget));
            Assert.That(pickOk.Status, Is.EqualTo(StepResultStatus.Succeeded));
            Assert.That(dropOk.Status, Is.EqualTo(StepResultStatus.Succeeded));
        }
    }
    // =============================================================================
    // PerceptionSpatialIndexQaTests
    // =============================================================================
    /// <summary>
    /// <para>
    /// Test QA EditMode per gli indici percettivi persistenti mantenuti dal World.
    /// </para>
    ///
    /// <para><b>Principio architetturale: indice aggiornato dalla mutazione autorevole</b></para>
    /// <para>
    /// Gli indici non devono piu' essere ricostruiti dentro i sistemi di percezione.
    /// Devono invece restare coerenti quando il World crea, muove, raccoglie,
    /// deposita o cancella entita' osservabili.
    /// </para>
    ///
    /// <para><b>Struttura interna:</b></para>
    /// <list type="bullet">
    ///   <item><b>Oggetti</b>: create, pick up, drop e destroy aggiornano la zona percettiva.</item>
    ///   <item><b>NPC</b>: create, SetNpcPos e cleanup dev aggiornano la cella percettiva.</item>
    /// </list>
    /// </summary>
    public sealed class PerceptionSpatialIndexQaTests
    {
        // =============================================================================
        // RunFromCommandLine
        // =============================================================================
        /// <summary>
        /// <para>
        /// Entry point minimale per eseguire questi test da Unity batchmode anche
        /// quando il test runner standard non produce il file XML dei risultati.
        /// </para>
        /// </summary>
        public static void RunFromCommandLine()
        {
            try
            {
                var tests = new PerceptionSpatialIndexQaTests();
                tests.GroundObjectPerceptionIndexTracksCreatePickupDropAndDestroy();
                tests.NpcPerceptionIndexTracksCreateMoveAndDevRemovalHook();
                tests.PerceptionDirtyMarksNearbyObserversWhenGroundObjectChanges();
                tests.PerceptionDirtyTracksNpcFacingAndMovement();

                Debug.Log("[PerceptionSpatialIndexQaTests] PASS");
                EditorApplication.Exit(0);
            }
            catch (Exception ex)
            {
                Debug.LogError("[PerceptionSpatialIndexQaTests] FAIL\n" + ex);
                EditorApplication.Exit(1);
            }
        }
        [Test]
        public void GroundObjectPerceptionIndexTracksCreatePickupDropAndDestroy()
        {
            var world = new World(new WorldConfig(new SimulationParams()));
            world.ObjectDefs["qa_food_stock"] = new ObjectDef
            {
                Id = "qa_food_stock",
                DisplayName = "QA food stock",
                IsInteractable = true,
                IsOccluder = false,
                BlocksMovement = false,
                BlocksVision = false
            };
            int objectId = world.CreateObject("qa_food_stock", 2, 2, OwnerKind.Community, 0);
            Assert.That(objectId, Is.GreaterThan(0));

            Assert.That(world.TryGetGroundObjectIdsInPerceptionZone(0, 0, out var objectIds), Is.True);
            Assert.That(objectIds.IndexOf(objectId), Is.GreaterThanOrEqualTo(0));

            int npcId = world.CreateNpc(
                NpcDnaProfile.CreateDefault("Indexer"),
                new NpcNeeds(),
                new Arcontio.Core.Social(),
                2,
                2);

            Assert.That(world.TryPickUpObject(npcId, objectId, out _, out _, out string pickupReason), Is.True, pickupReason);
            bool stillIndexedAfterPickup = world.TryGetGroundObjectIdsInPerceptionZone(0, 0, out objectIds)
                && objectIds.IndexOf(objectId) >= 0;
            Assert.That(stillIndexedAfterPickup, Is.False);

            Assert.That(world.TryDropObject(npcId, objectId, 3, 2, out string dropReason), Is.True, dropReason);
            Assert.That(world.TryGetGroundObjectIdsInPerceptionZone(0, 0, out objectIds), Is.True);
            Assert.That(objectIds.IndexOf(objectId), Is.GreaterThanOrEqualTo(0));

            world.DestroyObject(objectId);
            bool stillIndexedAfterDestroy = world.TryGetGroundObjectIdsInPerceptionZone(0, 0, out objectIds)
                && objectIds.IndexOf(objectId) >= 0;
            Assert.That(stillIndexedAfterDestroy, Is.False);
        }

        [Test]
        public void NpcPerceptionIndexTracksCreateMoveAndDevRemovalHook()
        {
            var world = new World(new WorldConfig(new SimulationParams()));
            int npcId = world.CreateNpc(
                NpcDnaProfile.CreateDefault("IndexedNpc"),
                new NpcNeeds(),
                new Arcontio.Core.Social(),
                1,
                1);

            Assert.That(world.TryGetNpcIdsInPerceptionCell(1, 1, out var npcIds), Is.True);
            Assert.That(npcIds.IndexOf(npcId), Is.GreaterThanOrEqualTo(0));

            world.SetNpcPos(npcId, 4, 1);
            bool stillInOldCell = world.TryGetNpcIdsInPerceptionCell(1, 1, out npcIds)
                && npcIds.IndexOf(npcId) >= 0;
            Assert.That(stillInOldCell, Is.False);
            Assert.That(world.TryGetNpcIdsInPerceptionCell(4, 1, out npcIds), Is.True);
            Assert.That(npcIds.IndexOf(npcId), Is.GreaterThanOrEqualTo(0));

            world.RemoveNpcFromPerceptionSpatialIndex(npcId);
            world.GridPos.Remove(npcId);
            bool stillInNewCell = world.TryGetNpcIdsInPerceptionCell(4, 1, out npcIds)
                && npcIds.IndexOf(npcId) >= 0;
            Assert.That(stillInNewCell, Is.False);
        }

        [Test]
        public void PerceptionDirtyMarksNearbyObserversWhenGroundObjectChanges()
        {
            var sim = new SimulationParams();
            sim.npcVisionRangeCells = 4;
            sim.perception.dirtyRadiusMarginCells = 1;

            var world = new World(new WorldConfig(sim));
            world.ObjectDefs["qa_food_stock"] = new ObjectDef
            {
                Id = "qa_food_stock",
                DisplayName = "QA food stock",
                IsInteractable = true,
                IsOccluder = false,
                BlocksMovement = false,
                BlocksVision = false
            };

            int nearNpc = world.CreateNpc(
                NpcDnaProfile.CreateDefault("NearObserver"),
                new NpcNeeds(),
                new Arcontio.Core.Social(),
                1,
                1);
            int farNpc = world.CreateNpc(
                NpcDnaProfile.CreateDefault("FarObserver"),
                new NpcNeeds(),
                new Arcontio.Core.Social(),
                20,
                20);

            world.ClearAllNpcPerceptionDirty();
            int objectId = world.CreateObject("qa_food_stock", 1, 1, OwnerKind.Community, 0);
            Assert.That(objectId, Is.GreaterThan(0));
            Assert.That(world.IsNpcPerceptionDirty(nearNpc), Is.True);
            Assert.That(world.IsNpcPerceptionDirty(farNpc), Is.False);

            world.ClearAllNpcPerceptionDirty();
            Assert.That(world.TryPickUpObject(nearNpc, objectId, out _, out _, out string pickupReason), Is.True, pickupReason);
            Assert.That(world.IsNpcPerceptionDirty(nearNpc), Is.True);
            Assert.That(world.IsNpcPerceptionDirty(farNpc), Is.False);

            world.ClearAllNpcPerceptionDirty();
            Assert.That(world.TryDropObject(nearNpc, objectId, 2, 1, out string dropReason), Is.True, dropReason);
            Assert.That(world.IsNpcPerceptionDirty(nearNpc), Is.True);
            Assert.That(world.IsNpcPerceptionDirty(farNpc), Is.False);

            world.ClearAllNpcPerceptionDirty();
            world.DestroyObject(objectId);
            Assert.That(world.IsNpcPerceptionDirty(nearNpc), Is.True);
            Assert.That(world.IsNpcPerceptionDirty(farNpc), Is.False);
        }

        [Test]
        public void PerceptionDirtyTracksNpcFacingAndMovement()
        {
            var sim = new SimulationParams();
            sim.npcVisionRangeCells = 4;
            sim.perception.dirtyRadiusMarginCells = 1;

            var world = new World(new WorldConfig(sim));
            int observer = world.CreateNpc(
                NpcDnaProfile.CreateDefault("Observer"),
                new NpcNeeds(),
                new Arcontio.Core.Social(),
                1,
                1);
            int mover = world.CreateNpc(
                NpcDnaProfile.CreateDefault("Mover"),
                new NpcNeeds(),
                new Arcontio.Core.Social(),
                3,
                1);
            int farNpc = world.CreateNpc(
                NpcDnaProfile.CreateDefault("FarNpc"),
                new NpcNeeds(),
                new Arcontio.Core.Social(),
                20,
                20);

            world.ClearAllNpcPerceptionDirty();
            world.SetFacing(mover, CardinalDirection.East);
            Assert.That(world.IsNpcPerceptionDirty(mover), Is.True);
            Assert.That(world.IsNpcPerceptionDirty(observer), Is.False);
            Assert.That(world.IsNpcPerceptionDirty(farNpc), Is.False);

            world.ClearAllNpcPerceptionDirty();
            world.SetNpcPos(mover, 4, 1);
            Assert.That(world.IsNpcPerceptionDirty(mover), Is.True);
            Assert.That(world.IsNpcPerceptionDirty(observer), Is.True);
            Assert.That(world.IsNpcPerceptionDirty(farNpc), Is.False);
        }
    }
}
