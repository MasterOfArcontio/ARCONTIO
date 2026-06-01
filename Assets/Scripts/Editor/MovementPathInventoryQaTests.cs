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
    /// Test QA EditMode che documentano l'inventario corrente dei due percorsi di
    /// movimento NPC: il path legacy basato su <c>MoveIntent</c>/<c>MovementSystem</c>
    /// e il path Job multi-tick basato su <c>RunningAction</c>.
    /// </para>
    ///
    /// <para><b>Principio architetturale: riduzione progressiva senza spegnimento brutale</b></para>
    /// <para>
    /// Questi test non migrano nessun routing e non introducono nuovi gate. Servono
    /// a blindare lo stato corrente: il legacy resta necessario per path lunghi,
    /// debug movement e fallback, mentre il traversal Job resta limitato a una cella
    /// cardinale adiacente con gate esplicito attivo.
    /// </para>
    ///
    /// <para><b>Struttura interna:</b></para>
    /// <list type="bullet">
    ///   <item><b>Legacy consumer</b>: <c>MovementSystem</c> consuma <c>MoveIntent</c>.</item>
    ///   <item><b>Legacy producer</b>: <c>SetMoveIntentCommand</c> scrive ancora il path legacy.</item>
    ///   <item><b>Job traversal</b>: target adiacente e gate attivo usano running action.</item>
    ///   <item><b>Fallback</b>: gate spento, target lontano o diagonale restano su command legacy.</item>
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

                tests.SetMoveIntentCommandStillFeedsLegacyMovementSystem();
                tests.JobMoveToCellUsesRunningActionOnlyForAdjacentCardinalTargetWithGateEnabled();
                tests.JobMoveToCellFallsBackToLegacyCommandWhenTraversalGateIsDisabled();
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
        // SetMoveIntentCommandStillFeedsLegacyMovementSystem
        // =============================================================================
        /// <summary>
        /// <para>
        /// Verifica il contratto legacy esplicito: il command scrive un
        /// <c>MoveIntent</c> e <c>MovementSystem</c> resta il consumer che muove
        /// fisicamente l'NPC nel tick successivo del test.
        /// </para>
        /// </summary>
        [Test]
        public void SetMoveIntentCommandStillFeedsLegacyMovementSystem()
        {
            // Arrange: target adiacente semplice, nessun Job assegnato. Questo e'
            // deliberatamente il path legacy command -> intent -> MovementSystem.
            var world = MakeWorldWithNpc(out int npcId);
            var startCell = world.GridPos[npcId];
            var command = new SetMoveIntentCommand(npcId, new MoveIntent
            {
                Active = true,
                TargetX = startCell.X + 1,
                TargetY = startCell.Y,
                Reason = MoveIntentReason.DebugClick,
                TargetObjectId = 0
            });
            var bus = new MessageBus();

            // Act 1: il command non muove. Scrive solo l'intent che verra' consumato
            // dal MovementSystem.
            command.Execute(world, bus);

            // Assert 1: nessuna mutazione posizione anticipata.
            Assert.That(world.NpcMoveIntents.TryGetValue(npcId, out var intent), Is.True);
            Assert.That(intent.Active, Is.True);
            Assert.That(intent.IsNew, Is.True);
            Assert.That(world.GridPos[npcId].X, Is.EqualTo(startCell.X));
            Assert.That(world.GridPos[npcId].Y, Is.EqualTo(startCell.Y));

            // Act 2: il consumer legacy processa l'intent.
            TickContext.BeginTick(1);
            new MovementSystem().Update(world, new Tick(1, 1f), bus, new Telemetry());

            // Assert 2: il movimento legacy ha completato una cella nello stesso tick
            // di MovementSystem, confermando che il path resta attivo.
            Assert.That(world.GridPos[npcId].X, Is.EqualTo(startCell.X + 1));
            Assert.That(world.GridPos[npcId].Y, Is.EqualTo(startCell.Y));
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
        // JobMoveToCellFallsBackToLegacyCommandWhenTraversalGateIsDisabled
        // =============================================================================
        /// <summary>
        /// <para>
        /// Verifica che il gate spento mantenga il fallback legacy anche per un
        /// target adiacente. Il test non spegne <c>MovementSystem</c> e non cambia
        /// routing: fotografa solo il comportamento corrente.
        /// </para>
        /// </summary>
        [Test]
        public void JobMoveToCellFallsBackToLegacyCommandWhenTraversalGateIsDisabled()
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
            Assert.That(world.JobRuntimeState.CommandBuffer.Count, Is.EqualTo(1));
            Assert.That(world.JobRuntimeState.CommandBuffer.Snapshot()[0], Is.TypeOf<SetMoveIntentCommand>());
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
            // World minimale: abbastanza runtime per command, MovementSystem e
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
