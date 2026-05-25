using Arcontio.Core;
using Arcontio.Core.Config;
using Arcontio.Core.Diagnostics;
using NUnit.Framework;
using UnityEngine;

namespace Arcontio.Tests
{
    // =============================================================================
    // TraversalReservationDurationAlignmentQaTests
    // =============================================================================
    /// <summary>
    /// <para>
    /// Test QA EditMode per l'allineamento tra durata configurata del traversal
    /// one-cell del Job Layer e durata della reservation temporale sulla cella
    /// destinazione.
    /// </para>
    ///
    /// <para><b>Principio architetturale: reservation coerente con progress runtime</b></para>
    /// <para>
    /// Il traversal multi-tick non deve lasciare una finestra senza reservation prima
    /// della completion. I test restano confinati al Job Layer: non invocano
    /// <c>MovementSystem</c>, non introducono pathfinding nuovo, non emettono command
    /// e non modificano configurazioni produttive.
    /// </para>
    ///
    /// <para><b>Struttura interna:</b></para>
    /// <list type="bullet">
    ///   <item><b>Duration alignment</b>: la reservation resta viva per tutta la durata configurata.</item>
    ///   <item><b>Cleanup</b>: failure/cancel del job pulisce running action e reservation.</item>
    ///   <item><b>Contention</b>: due NPC non possono possedere la stessa cella durante la traversata.</item>
    /// </list>
    /// </summary>
    public sealed class TraversalReservationDurationAlignmentQaTests
    {
        // =============================================================================
        // RunFromCommandLine
        // =============================================================================
        /// <summary>
        /// <para>
        /// Entry point diagnostico per eseguire questi QA test da Unity batchmode
        /// quando il runner CLI standard non produce il file XML dei risultati.
        /// </para>
        /// </summary>
        public static void RunFromCommandLine()
        {
            try
            {
                var tests = new TraversalReservationDurationAlignmentQaTests();

                tests.DestinationReservationUsesConfiguredTraversalDuration();
                tests.DestinationReservationIsCleanedWhenTraversalJobFails();
                tests.DestinationReservationPreventsSecondNpcDuringTraversal();

                Debug.Log("[TraversalReservationDurationAlignmentQaTests] PASS");
                UnityEditor.EditorApplication.Exit(0);
            }
            catch (System.Exception ex)
            {
                Debug.LogError("[TraversalReservationDurationAlignmentQaTests] FAIL\n" + ex);
                UnityEditor.EditorApplication.Exit(1);
            }
        }

        // =============================================================================
        // DestinationReservationUsesConfiguredTraversalDuration
        // =============================================================================
        /// <summary>
        /// <para>
        /// Verifica che una traversal reservation nata a tick 0 con durata cella 4
        /// non scada ai tick intermedi e venga rilasciata solo alla completion.
        /// </para>
        /// </summary>
        [Test]
        public void DestinationReservationUsesConfiguredTraversalDuration()
        {
            var world = MakeWorldWithNpc(out int npcId);
            EnableOneCellTraversal(world, durationTicks: 4);
            var startCell = world.GridPos[npcId];
            var targetCell = new Vector2Int(startCell.X + 1, startCell.Y);
            var job = MakeObjectTargetMoveJob(npcId, "job-reservation-duration", targetObjectId: 7301, targetCell);
            Assert.That(world.JobRuntimeState.TryAssignJob(npcId, job, tick: 0, out var reason), Is.True, reason);
            var system = new JobExecutionSystem();

            for (int tick = 0; tick < 3; tick++)
            {
                system.Update(world, new Tick(tick, 1f), new MessageBus(), new Telemetry());

                Assert.That(world.JobRuntimeState.RunningActions.Count, Is.EqualTo(1));
                Assert.That(world.JobRuntimeState.Reservations.TryGetByTarget(ReservationTargetKind.Cell, targetCell, -1, out var held), Is.True);
                Assert.That(held.JobId, Is.EqualTo(job.JobId));
                Assert.That(held.ExpiresTick, Is.EqualTo(4));
                Assert.That(world.GridPos[npcId].X, Is.EqualTo(startCell.X));
                Assert.That(world.GridPos[npcId].Y, Is.EqualTo(startCell.Y));
            }

            system.Update(world, new Tick(3, 1f), new MessageBus(), new Telemetry());

            Assert.That(world.JobRuntimeState.RunningActions.Count, Is.EqualTo(0));
            Assert.That(world.JobRuntimeState.Reservations.Count, Is.EqualTo(0));
            Assert.That(world.GridPos[npcId].X, Is.EqualTo(targetCell.x));
            Assert.That(world.GridPos[npcId].Y, Is.EqualTo(targetCell.y));
            Assert.That(job.Status, Is.EqualTo(JobStatus.Completed));
        }

        // =============================================================================
        // DestinationReservationIsCleanedWhenTraversalJobFails
        // =============================================================================
        /// <summary>
        /// <para>
        /// Verifica che il cleanup già posseduto da JobRuntimeState resti valido:
        /// se il job viene chiuso durante il traversal, reservation e running action
        /// vengono rimosse senza passare da MovementSystem.
        /// </para>
        /// </summary>
        [Test]
        public void DestinationReservationIsCleanedWhenTraversalJobFails()
        {
            var world = MakeWorldWithNpc(out int npcId);
            EnableOneCellTraversal(world, durationTicks: 4);
            var startCell = world.GridPos[npcId];
            var targetCell = new Vector2Int(startCell.X + 1, startCell.Y);
            var job = MakeObjectTargetMoveJob(npcId, "job-reservation-cleanup", targetObjectId: 7302, targetCell);
            Assert.That(world.JobRuntimeState.TryAssignJob(npcId, job, tick: 0, out var reason), Is.True, reason);
            var system = new JobExecutionSystem();

            system.Update(world, new Tick(0, 1f), new MessageBus(), new Telemetry());
            Assert.That(world.JobRuntimeState.RunningActions.Count, Is.EqualTo(1));
            Assert.That(world.JobRuntimeState.Reservations.TryGetByTarget(ReservationTargetKind.Cell, targetCell, -1, out _), Is.True);

            Assert.That(world.JobRuntimeState.FailCurrentJob(npcId, JobFailureReason.Cancelled, tick: 1, out var failReason), Is.True, failReason);

            Assert.That(world.JobRuntimeState.RunningActions.Count, Is.EqualTo(0));
            Assert.That(world.JobRuntimeState.Reservations.Count, Is.EqualTo(0));
            Assert.That(world.GridPos[npcId].X, Is.EqualTo(startCell.X));
            Assert.That(world.GridPos[npcId].Y, Is.EqualTo(startCell.Y));
        }

        // =============================================================================
        // DestinationReservationPreventsSecondNpcDuringTraversal
        // =============================================================================
        /// <summary>
        /// <para>
        /// Verifica che la reservation della cella destinazione resti posseduta dal
        /// primo job per i tick intermedi e impedisca a un secondo NPC di entrare
        /// nello stesso target durante la traversata.
        /// </para>
        /// </summary>
        [Test]
        public void DestinationReservationPreventsSecondNpcDuringTraversal()
        {
            var world = MakeWorldWithNpc(out int firstNpcId);
            int secondNpcId = world.CreateNpc(
                NpcDnaProfile.CreateDefault("traversal_reservation_contention_qa"),
                NpcNeeds.Make(0.5f, 0.5f),
                new Arcontio.Core.Social { JusticePerception01 = 0.5f },
                x: 2,
                y: 2);
            EnableOneCellTraversal(world, durationTicks: 4);
            var targetCell = new Vector2Int(2, 1);
            var firstJob = MakeObjectTargetMoveJob(firstNpcId, "job-reservation-contention-a", targetObjectId: 7303, targetCell);
            var secondJob = MakeObjectTargetMoveJob(secondNpcId, "job-reservation-contention-b", targetObjectId: 7304, targetCell);
            Assert.That(world.JobRuntimeState.TryAssignJob(firstNpcId, firstJob, tick: 0, out var firstReason), Is.True, firstReason);
            Assert.That(world.JobRuntimeState.TryAssignJob(secondNpcId, secondJob, tick: 0, out var secondReason), Is.True, secondReason);
            var system = new JobExecutionSystem();

            for (int tick = 0; tick < 3; tick++)
            {
                system.Update(world, new Tick(tick, 1f), new MessageBus(), new Telemetry());

                Assert.That(world.JobRuntimeState.Reservations.TryGetByTarget(ReservationTargetKind.Cell, targetCell, -1, out var held), Is.True);
                Assert.That(held.JobId, Is.EqualTo(firstJob.JobId));
                Assert.That(world.GridPos[firstNpcId].X, Is.EqualTo(1));
                Assert.That(world.GridPos[firstNpcId].Y, Is.EqualTo(1));
                Assert.That(world.GridPos[secondNpcId].X, Is.EqualTo(2));
                Assert.That(world.GridPos[secondNpcId].Y, Is.EqualTo(2));
            }
        }

        private static World MakeWorldWithNpc(out int npcId)
        {
            var world = new World(new WorldConfig(new SimulationParams()));
            world.Global.Needs = NeedsConfig.Default();
            world.Global.BeliefQuery = BeliefQueryConfig.Default();
            npcId = world.CreateNpc(
                NpcDnaProfile.CreateDefault("traversal_reservation_alignment_qa"),
                NpcNeeds.Make(0.5f, 0.5f),
                new Arcontio.Core.Social { JusticePerception01 = 0.5f },
                x: 1,
                y: 1);
            return world;
        }

        private static void EnableOneCellTraversal(World world, int durationTicks)
        {
            world.Config.Sim.tick = new TickParams
            {
                ticksPerSecond = TickParams.DefaultTicksPerSecond,
                baseWalkCellDurationTicks = durationTicks,
                enableJobRunningActionTraversal = true
            };
        }

        private static Job MakeObjectTargetMoveJob(int npcId, string jobId, int targetObjectId, Vector2Int targetCell)
        {
            var request = new JobRequest(
                "req-" + jobId,
                npcId,
                DecisionIntentKind.WaitAndObserve,
                JobPriorityClass.Normal,
                urgency01: 0.1f,
                createdTick: 0,
                hasTargetCell: false,
                targetCell: Vector2Int.zero,
                targetObjectId: targetObjectId,
                beliefKey: string.Empty,
                debugLabel: jobId);
            var phase = new JobPhase(
                "phase-move",
                JobPhaseKind.ReachTarget,
                "QA Move",
                expectedStepCount: 1,
                isInterruptible: true,
                actions: new[] { JobAction.MoveTo("move", targetCell, "QA move") });
            return new Job(jobId, request, new JobPlan("plan-" + jobId, new[] { phase }));
        }
    }
}
