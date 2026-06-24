using Arcontio.Core;
using Arcontio.Core.Config;
using Arcontio.Core.Diagnostics;
using NUnit.Framework;
using UnityEngine;

namespace Arcontio.Tests
{
    // =============================================================================
    // MovementPathInventoryQaTests
    // =============================================================================
    /// <summary>
    /// <para>
    /// Test QA EditMode che documentano l'inventario corrente del movimento NPC:
    /// il movimento fisico runtime deve passare dal Job Layer, tramite
    /// <c>MoveToCell</c> e running action traversal.
    /// </para>
    ///
    /// <para><b>Principio architetturale: autorita' unica del movimento</b></para>
    /// <para>
    /// Questi test blindano il nuovo contratto: il vecchio ponte
    /// <c>MoveIntent</c>/<c>MovementSystem</c> non e' piu' un fallback produttivo.
    /// Se il Job Layer non possiede una route autorizzata, il job fallisce o resta
    /// bloccato nel proprio runtime invece di attivare un movimento greedy esterno.
    /// </para>
    ///
    /// <para><b>Struttura interna:</b></para>
    /// <list type="bullet">
    ///   <item><b>Job traversal</b>: target adiacente e gate attivo usano running action.</item>
    ///   <item><b>No fallback legacy</b>: gate spento non produce command di movimento.</item>
    ///   <item><b>Route dichiarata</b>: target lontani o diagonali usano route preparate da MoveTo.</item>
    /// </list>
    /// </summary>
    public sealed class MovementPathInventoryQaTests
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
                var tests = new MovementPathInventoryQaTests();

                tests.JobMoveToCellUsesRunningActionOnlyForAdjacentCardinalTargetWithGateEnabled();
                tests.JobMoveToCellFailsWhenTraversalGateIsDisabled();
                tests.JobMoveToCellPreparesDeclaredDistantRoutesWhenTraversalGateIsEnabled();

                Debug.Log("[MovementPathInventoryQaTests] PASS");
                UnityEditor.EditorApplication.Exit(0);
            }
            catch (System.Exception ex)
            {
                Debug.LogError("[MovementPathInventoryQaTests] FAIL\n" + ex);
                UnityEditor.EditorApplication.Exit(1);
            }
        }

        // =============================================================================
        // JobMoveToCellUsesRunningActionOnlyForAdjacentCardinalTargetWithGateEnabled
        // =============================================================================
        /// <summary>
        /// <para>
        /// Verifica il perimetro positivo del nuovo movimento Job: una cella
        /// cardinale adiacente con gate attivo crea running action e non emette
        /// <c>SetMoveIntentCommand</c>.
        /// </para>
        /// </summary>
        [Test]
        public void JobMoveToCellUsesRunningActionOnlyForAdjacentCardinalTargetWithGateEnabled()
        {
            var world = MakeWorldWithNpc(out int npcId);
            EnableOneCellTraversal(world, durationTicks: 3);
            var startCell = world.GridPos[npcId];
            var job = MakeMoveJob(npcId, "job-inventory-adjacent", new Vector2Int(startCell.X + 1, startCell.Y));
            Assert.That(world.JobRuntimeState.TryAssignJob(npcId, job, tick: 0, out var reason), Is.True, reason);

            new JobExecutionSystem().Update(world, new Tick(0, 1f), new MessageBus(), new Telemetry());

            Assert.That(world.JobRuntimeState.RunningActions.Count, Is.EqualTo(1));
            Assert.That(world.JobRuntimeState.CommandBuffer.Count, Is.EqualTo(0));
            Assert.That(world.GridPos[npcId].X, Is.EqualTo(startCell.X));
            Assert.That(world.GridPos[npcId].Y, Is.EqualTo(startCell.Y));
        }

        // =============================================================================
        // JobMoveToCellFailsWhenTraversalGateIsDisabled
        // =============================================================================
        /// <summary>
        /// <para>
        /// Verifica che il gate spento non attivi piu' alcun fallback legacy.
        /// Il movimento resta dentro il Job Layer: senza traversal autorizzato,
        /// il job fallisce e non viene emesso nessun command di movimento.
        /// </para>
        /// </summary>
        [Test]
        public void JobMoveToCellFailsWhenTraversalGateIsDisabled()
        {
            var world = MakeWorldWithNpc(out int npcId);
            world.Config.Sim.tick = new TickParams
            {
                ticksPerSecond = TickParams.DefaultTicksPerSecond,
                baseWalkCellDurationTicks = 3,
                enableJobRunningActionTraversal = false
            };
            var startCell = world.GridPos[npcId];
            var job = MakeMoveJob(npcId, "job-inventory-gate-off", new Vector2Int(startCell.X + 1, startCell.Y));
            Assert.That(world.JobRuntimeState.TryAssignJob(npcId, job, tick: 0, out var reason), Is.True, reason);

            new JobExecutionSystem().Update(world, new Tick(0, 1f), new MessageBus(), new Telemetry());

            Assert.That(world.JobRuntimeState.RunningActions.Count, Is.EqualTo(0));
            Assert.That(world.JobRuntimeState.CommandBuffer.Count, Is.EqualTo(0));
            Assert.That(world.JobRuntimeState.HasActiveJob(npcId), Is.False);
            Assert.That(job.Status, Is.EqualTo(JobStatus.Failed));
            Assert.That(job.FailureReason, Is.EqualTo(JobFailureReason.MovementFailed));
            Assert.That(world.GridPos[npcId].X, Is.EqualTo(startCell.X));
            Assert.That(world.GridPos[npcId].Y, Is.EqualTo(startCell.Y));
        }

        // =============================================================================
        // JobMoveToCellPreparesDeclaredDistantRoutesWhenTraversalGateIsEnabled
        // =============================================================================
        /// <summary>
        /// <para>
        /// Verifica che il gate attivo prepari route locali dichiarate per target
        /// lontani o diagonali gia' presenti nel Job, senza tornare al ponte legacy.
        /// </para>
        /// </summary>
        [Test]
        public void JobMoveToCellPreparesDeclaredDistantRoutesWhenTraversalGateIsEnabled()
        {
            var distantWorld = MakeWorldWithNpc(out int distantNpcId);
            EnableOneCellTraversal(distantWorld, durationTicks: 3);
            var distantStart = distantWorld.GridPos[distantNpcId];
            var distantJob = MakeMoveJob(distantNpcId, "job-inventory-distant", new Vector2Int(distantStart.X + 2, distantStart.Y));
            Assert.That(distantWorld.JobRuntimeState.TryAssignJob(distantNpcId, distantJob, tick: 0, out var distantReason), Is.True, distantReason);

            new JobExecutionSystem().Update(distantWorld, new Tick(0, 1f), new MessageBus(), new Telemetry());

            Assert.That(distantWorld.JobRuntimeState.RunningActions.Count, Is.EqualTo(1));
            Assert.That(distantWorld.JobRuntimeState.CommandBuffer.Count, Is.EqualTo(0));
            Assert.That(distantWorld.Pathfinding.DirectCommitExecution.ContainsKey(distantNpcId), Is.True);
            Assert.That(distantWorld.GridPos[distantNpcId].X, Is.EqualTo(distantStart.X));
            Assert.That(distantWorld.GridPos[distantNpcId].Y, Is.EqualTo(distantStart.Y));

            var diagonalWorld = MakeWorldWithNpc(out int diagonalNpcId);
            EnableOneCellTraversal(diagonalWorld, durationTicks: 3);
            var diagonalStart = diagonalWorld.GridPos[diagonalNpcId];
            var diagonalJob = MakeMoveJob(diagonalNpcId, "job-inventory-diagonal", new Vector2Int(diagonalStart.X + 1, diagonalStart.Y + 1));
            Assert.That(diagonalWorld.JobRuntimeState.TryAssignJob(diagonalNpcId, diagonalJob, tick: 0, out var diagonalReason), Is.True, diagonalReason);

            new JobExecutionSystem().Update(diagonalWorld, new Tick(0, 1f), new MessageBus(), new Telemetry());

            Assert.That(diagonalWorld.JobRuntimeState.RunningActions.Count, Is.EqualTo(1));
            Assert.That(diagonalWorld.JobRuntimeState.CommandBuffer.Count, Is.EqualTo(0));
            Assert.That(diagonalWorld.Pathfinding.DirectCommitExecution.ContainsKey(diagonalNpcId), Is.True);
            Assert.That(diagonalWorld.GridPos[diagonalNpcId].X, Is.EqualTo(diagonalStart.X));
            Assert.That(diagonalWorld.GridPos[diagonalNpcId].Y, Is.EqualTo(diagonalStart.Y));
        }

        private static World MakeWorldWithNpc(out int npcId)
        {
            // World minimale: abbastanza runtime per command e
            // JobExecutionSystem, senza SimulationHost o modifiche config globali.
            var world = new World(new WorldConfig(new SimulationParams()));
            world.Global.Needs = NeedsConfig.Default();
            world.Global.BeliefQuery = BeliefQueryConfig.Default();
            npcId = world.CreateNpc(
                NpcDnaProfile.CreateDefault("movement_path_inventory_qa"),
                NpcNeeds.Make(0.5f, 0.5f),
                new Arcontio.Core.Social { JusticePerception01 = 0.5f },
                x: 1,
                y: 1);
            world.NpcFacing[npcId] = CardinalDirection.East;
            return world;
        }

        private static void EnableOneCellTraversal(World world, int durationTicks)
        {
            // Gate locale al test: documenta il path Job multi-tick senza cambiare
            // game_params.json o default produttivi.
            world.Config.Sim.tick = new TickParams
            {
                ticksPerSecond = TickParams.DefaultTicksPerSecond,
                baseWalkCellDurationTicks = durationTicks,
                enableJobRunningActionTraversal = true
            };
        }

        private static Job MakeMoveJob(int npcId, string jobId, Vector2Int targetCell)
        {
            var request = JobRequest.FromDecision(
                "req-" + jobId,
                npcId,
                DecisionIntentKind.WaitAndObserve,
                JobPriorityClass.Normal,
                urgency01: 0.1f,
                createdTick: 0,
                targetCell,
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
