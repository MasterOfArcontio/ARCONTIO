using Arcontio.Core;
using Arcontio.Core.Config;
using Arcontio.Core.Diagnostics;
using System.Collections.Generic;
using NUnit.Framework;
using UnityEngine;

namespace Arcontio.Tests
{
    // =============================================================================
    // RunningActionProductiveTickingQaTests
    // =============================================================================
    /// <summary>
    /// <para>
    /// Test QA EditMode per il primo cablaggio produttivo controllato delle running
    /// action nel Job runtime.
    /// </para>
    ///
    /// <para><b>Principio architetturale: tick produttivo senza movement multi-tick</b></para>
    /// <para>
    /// v0.11c.02e autorizza <c>JobExecutionSystem</c> a usare
    /// <c>RunningActionStore</c> e <c>RunningActionExecutor</c> solo per action
    /// controllate e prive di mutazione world durante il progress. Il candidato QA
    /// e' <c>WaitTicks</c>: non legge target, non emette command finale, non tocca
    /// <c>MovementSystem</c> e permette di verificare il ciclo register/tick/complete
    /// senza introdurre traversal reale.
    /// </para>
    ///
    /// <para><b>Struttura interna:</b></para>
    /// <list type="bullet">
    ///   <item><b>Progress</b>: WaitTicks crea e avanza una running action volatile.</item>
    ///   <item><b>Completion</b>: al raggiungimento durata lo step completa e pulisce lo store.</item>
    ///   <item><b>Boundary</b>: nessun command, nessuna mutazione posizione, nessun movement reale.</item>
    /// </list>
    /// </summary>
    public sealed class RunningActionProductiveTickingQaTests
    {
        // =============================================================================
        // WaitTicksRegistersAndTicksRunningActionWithoutWorldMutation
        // =============================================================================
        /// <summary>
        /// <para>
        /// Verifica che un job WaitTicks di durata maggiore di un tick crei progress
        /// volatile nello store e resti running senza mutare il World.
        /// </para>
        /// </summary>
        [Test]
        public void WaitTicksRegistersAndTicksRunningActionWithoutWorldMutation()
        {
            // Arrange: job no-target, quindi niente MovementSystem, reservation o command finale.
            var world = MakeWorldWithNpc(out int npcId);
            var job = MakeWaitJob(npcId, "job-wait-running", durationTicks: 3);
            Assert.That(world.JobRuntimeState.TryAssignJob(npcId, job, tick: 0, out var reason), Is.True, reason);
            var system = new JobExecutionSystem();
            var startCell = world.GridPos[npcId];

            // Act: il primo tick registra e avanza elapsed a 1, ma non completa.
            system.Update(world, new Tick(0, 1f), new MessageBus(), new Telemetry());

            // Assert: progress interno visibile, job ancora attivo, World non mutato.
            Assert.That(world.JobRuntimeState.HasActiveJob(npcId), Is.True);
            Assert.That(world.JobRuntimeState.RunningActions.Count, Is.EqualTo(1));
            var snapshots = world.JobRuntimeState.RunningActions.GetSnapshots();
            Assert.That(snapshots.Count, Is.EqualTo(1));
            Assert.That(snapshots[0].ElapsedTicks, Is.EqualTo(1));
            Assert.That(snapshots[0].Status, Is.EqualTo(RunningActionLifecycleStatus.Running));
            Assert.That(world.JobRuntimeState.CommandBuffer.Count, Is.EqualTo(0));
            Assert.That(world.GridPos[npcId].X, Is.EqualTo(startCell.X));
            Assert.That(world.GridPos[npcId].Y, Is.EqualTo(startCell.Y));
        }

        // =============================================================================
        // WaitTicksCompletesAndClearsRunningActionStore
        // =============================================================================
        /// <summary>
        /// <para>
        /// Verifica che la completion interna della running action faccia avanzare la
        /// state machine e pulisca lo store volatile.
        /// </para>
        /// </summary>
        [Test]
        public void WaitTicksCompletesAndClearsRunningActionStore()
        {
            // Arrange: durata due tick per osservare sia Running sia Completed.
            var world = MakeWorldWithNpc(out int npcId);
            var job = MakeWaitJob(npcId, "job-wait-complete", durationTicks: 2);
            Assert.That(world.JobRuntimeState.TryAssignJob(npcId, job, tick: 0, out var reason), Is.True, reason);
            var system = new JobExecutionSystem();

            // Act: primo tick running, secondo tick completion dello step/job.
            system.Update(world, new Tick(0, 1f), new MessageBus(), new Telemetry());
            Assert.That(world.JobRuntimeState.RunningActions.Count, Is.EqualTo(1));
            system.Update(world, new Tick(1, 1f), new MessageBus(), new Telemetry());

            // Assert: il job single-step completa e il progress volatile sparisce.
            Assert.That(world.JobRuntimeState.HasActiveJob(npcId), Is.False);
            Assert.That(world.JobRuntimeState.RunningActions.Count, Is.EqualTo(0));
            Assert.That(world.JobRuntimeState.CommandBuffer.Count, Is.EqualTo(0));
            Assert.That(job.Status, Is.EqualTo(JobStatus.Completed));
        }

        // =============================================================================
        // ProductiveTickingDoesNotRouteMoveToCellThroughRunningActionStore
        // =============================================================================
        /// <summary>
        /// <para>
        /// Verifica che il cablaggio produttivo non trasformi ancora MoveToCell in
        /// running action multi-tick reale.
        /// </para>
        /// </summary>
        [Test]
        public void ProductiveTickingDoesNotRouteMoveToCellThroughRunningActionStore()
        {
            // Arrange: job di movimento legacy verso una cella diversa.
            var world = MakeWorldWithNpc(out int npcId);
            var job = MakeMoveJob(npcId, "job-move-legacy", new Vector2Int(2, 1));
            Assert.That(world.JobRuntimeState.TryAssignJob(npcId, job, tick: 0, out var reason), Is.True, reason);
            var system = new JobExecutionSystem();

            // Act: il vecchio path enqueue-a-command resta quello attivo.
            system.Update(world, new Tick(0, 1f), new MessageBus(), new Telemetry());

            // Assert: MoveToCell produce ancora command buffer e non usa lo store running action.
            Assert.That(world.JobRuntimeState.RunningActions.Count, Is.EqualTo(0));
            Assert.That(world.JobRuntimeState.CommandBuffer.Count, Is.EqualTo(1));
            Assert.That(world.JobRuntimeState.HasActiveJob(npcId), Is.True);
        }

        // =============================================================================
        // WaitTicksWritesRunningActionLifecycleExplainability
        // =============================================================================
        /// <summary>
        /// <para>
        /// Verifica che il path produttivo controllato WaitTicks scriva trace
        /// Started/Progress/Completed nel registry EL-MBQD senza introdurre command o
        /// mutazioni World.
        /// </para>
        /// </summary>
        [Test]
        public void WaitTicksWritesRunningActionLifecycleExplainability()
        {
            // Arrange: abilitiamo solo il registry diagnostico gia' esistente. Il
            // job resta un WaitTicks no-target, quindi non puo' toccare MovementSystem
            // o produrre una mutazione world-mutating.
            var world = MakeWorldWithNpc(out int npcId);
            EnableRunningActionExplainability(world);
            var job = MakeWaitJob(npcId, "job-wait-el", durationTicks: 2);
            Assert.That(world.JobRuntimeState.TryAssignJob(npcId, job, tick: 0, out var reason), Is.True, reason);
            var system = new JobExecutionSystem();
            var startCell = world.GridPos[npcId];

            // Act: primo tick start+progress, secondo tick completion.
            system.Update(world, new Tick(0, 1f), new MessageBus(), new Telemetry());
            system.Update(world, new Tick(1, 1f), new MessageBus(), new Telemetry());

            // Assert: le trace sono nello store EL-MBQD per-NPC e copiano key,
            // lifecycle e progress interno; lo store running action e il command
            // buffer restano coerenti con il no-world-mutation contract.
            Assert.That(world.MemoryBeliefDecisionExplainability.TryGetNpcStore(npcId, out var store), Is.True);
            Assert.That(store.RunningActionTraceCount, Is.EqualTo(3));

            var traces = new List<MemoryBeliefDecisionTrace>();
            store.CopyRunningActionTracesTo(traces);
            Assert.That(traces[0].RunningAction.Operation, Is.EqualTo(MemoryBeliefDecisionRunningActionOperation.Started));
            Assert.That(traces[1].RunningAction.Operation, Is.EqualTo(MemoryBeliefDecisionRunningActionOperation.Progress));
            Assert.That(traces[2].RunningAction.Operation, Is.EqualTo(MemoryBeliefDecisionRunningActionOperation.Completed));
            Assert.That(traces[2].RunningAction.JobId, Is.EqualTo(job.JobId));
            Assert.That(traces[2].RunningAction.OwnerNpcId, Is.EqualTo(npcId));
            Assert.That(traces[2].RunningAction.PhaseIndex, Is.EqualTo(0));
            Assert.That(traces[2].RunningAction.ActionIndex, Is.EqualTo(0));
            Assert.That(traces[2].RunningAction.ActionKind, Is.EqualTo(RunningActionKind.Wait));
            Assert.That(traces[2].RunningAction.ElapsedTicks, Is.EqualTo(2));
            Assert.That(traces[2].RunningAction.RequiredTicks, Is.EqualTo(2));
            Assert.That(traces[2].RunningAction.IsTerminal, Is.True);
            Assert.That(world.JobRuntimeState.CommandBuffer.Count, Is.EqualTo(0));
            Assert.That(world.GridPos[npcId].X, Is.EqualTo(startCell.X));
            Assert.That(world.GridPos[npcId].Y, Is.EqualTo(startCell.Y));
        }

        private static World MakeWorldWithNpc(out int npcId)
        {
            // World minimale: abbastanza runtime per JobExecutionSystem, senza
            // avviare SimulationHost o MovementSystem.
            var world = new World(new WorldConfig(new SimulationParams()));
            world.Global.Needs = NeedsConfig.Default();
            world.Global.BeliefQuery = BeliefQueryConfig.Default();
            npcId = world.CreateNpc(
                NpcDnaProfile.CreateDefault("running_action_productive_qa"),
                NpcNeeds.Make(0.5f, 0.5f),
                new Arcontio.Core.Social { JusticePerception01 = 0.5f },
                x: 1,
                y: 1);
            return world;
        }

        private static void EnableRunningActionExplainability(World world)
        {
            // La config abilita soltanto emissione diagnostica. I test non attivano
            // JSONL, non scrivono file e non cambiano il runtime produttivo.
            var config = world.Config.Sim.memory_belief_decision_explainability;
            config.enabled = true;
            config.writeJsonLog = false;
            config.logRunningAction = true;
        }

        private static Job MakeWaitJob(int npcId, string jobId, int durationTicks)
        {
            var request = JobRequest.WithoutTarget(
                "req-" + jobId,
                npcId,
                DecisionIntentKind.WaitAndObserve,
                JobPriorityClass.Normal,
                urgency01: 0.1f,
                createdTick: 0,
                debugLabel: jobId);
            var phase = new JobPhase(
                "phase-wait",
                JobPhaseKind.Execute,
                "QA Wait",
                expectedStepCount: 1,
                isInterruptible: true,
                actions: new[] { JobAction.Wait("wait", durationTicks, "QA wait") });
            return new Job(jobId, request, new JobPlan("plan-" + jobId, new[] { phase }));
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
