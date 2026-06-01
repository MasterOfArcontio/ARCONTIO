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
        // RunFromCommandLine
        // =============================================================================
        /// <summary>
        /// <para>
        /// Entry point diagnostico per eseguire il QA mirato 06b da Unity batchmode
        /// quando il runner standard non produce XML.
        /// </para>
        /// </summary>
        public static void RunFromCommandLine()
        {
            try
            {
                new RunningActionProductiveTickingQaTests().OneCellMoveTraversalHonorsConfiguredFourTickDuration();
                new RunningActionProductiveTickingQaTests().DeclaredDistantMoveTargetPreparesRouteAndHonorsConfiguredDuration();
                new RunningActionProductiveTickingQaTests().DeclaredTargetOutsideCurrentFacingWithoutKnownRouteFails();
                new RunningActionProductiveTickingQaTests().EatKnownFoodMoveTargetPreparesRouteAndHonorsConfiguredDuration();
                new RunningActionProductiveTickingQaTests().EatKnownFoodBeliefTargetOutsideCurrentFacingStillPreparesRoute();
                new RunningActionProductiveTickingQaTests().DeclaredDiagonalMoveTargetPreparesRouteAndHonorsConfiguredDuration();
                new RunningActionProductiveTickingQaTests().KnownRouteMoveToConsumesCellsThroughRunningActionTraversal();
                new RunningActionProductiveTickingQaTests().KnownRouteMoveToOpensUnlockedDoorBeforeTraversal();
                new RunningActionProductiveTickingQaTests().KnownRouteMoveToReturnsLockedDoorFailure();
                new RunningActionProductiveTickingQaTests().KnownRouteMoveToWritesLifecycleExplainabilityForEachCell();
                new RunningActionProductiveTickingQaTests().DevForcedMoveToCellUsesMoveToRunningActionRoute();
                new RunningActionProductiveTickingQaTests().DevTransportTemplatePreparesDeclaredRoutesWithoutMoveIntentFallback();

                Debug.Log("[RunningActionProductiveTickingQaTests] PASS targeted traversal tests");
                UnityEditor.EditorApplication.Exit(0);
            }
            catch (System.Exception ex)
            {
                Debug.LogError("[RunningActionProductiveTickingQaTests] FAIL targeted traversal tests\n" + ex);
                UnityEditor.EditorApplication.Exit(1);
            }
        }

        // =============================================================================
        // RuntimeTimingParamsLoadFromJsonAndDeriveTickInterval
        // =============================================================================
        /// <summary>
        /// <para>
        /// Verifica che i parametri temporali canonici siano deserializzati dal DTO
        /// tipizzato e che <c>SimulationHost</c> derivi l'intervallo di tick da
        /// <c>ticksPerSecond</c>, non da campi Inspector.
        /// </para>
        /// </summary>
        [Test]
        public void RuntimeTimingParamsLoadFromJsonAndDeriveTickInterval()
        {
            // Arrange: JSON minimale equivalente al frammento canonico di
            // game_params.json. JsonUtility ignora le sezioni assenti, quindi il test
            // resta focalizzato sui parametri runtime/timing richiesti da 03-prep.
            const string json = "{"
                + "\"tick\":{"
                + "\"ticksPerSecond\":5,"
                + "\"baseWalkCellDurationTicks\":3,"
                + "\"enableJobRunningActionTraversal\":false"
                + "}"
                + "}";

            // Act: la config tipizzata legge i valori e SimulationHost calcola il
            // proprio intervallo senza dipendere da ArcontioRuntime Inspector.
            var sim = JsonUtility.FromJson<SimulationParams>(json);
            float tickInterval = SimulationHost.ResolveTickIntervalSeconds(sim);
            float tickDeltaTime = SimulationHost.ResolveTickDeltaTime(sim);

            // Assert: fonte unica game_params -> SimulationParams -> runtime.
            Assert.That(sim.ResolveTicksPerSecond(), Is.EqualTo(5));
            Assert.That(tickInterval, Is.EqualTo(0.2f).Within(0.0001f));
            Assert.That(tickDeltaTime, Is.EqualTo(tickInterval).Within(0.0001f));
            Assert.That(sim.ResolveBaseWalkCellDurationTicks(), Is.EqualTo(3));
            Assert.That(sim.ResolveEnableJobRunningActionTraversal(), Is.False);
        }

        // =============================================================================
        // RuntimeTimingDefaultsKeepTraversalGateOff
        // =============================================================================
        /// <summary>
        /// <para>
        /// Verifica i fallback tipizzati quando il JSON non specifica valori: la
        /// cadence ha un default sicuro e il traversal running action resta spento.
        /// </para>
        /// </summary>
        [Test]
        public void RuntimeTimingDefaultsKeepTraversalGateOff()
        {
            // Arrange: DTO default, come path di fallback del loader quando
            // game_params.json manca o non contiene le sezioni nuove.
            var sim = new SimulationParams();

            // Act: risolviamo la cadence attraverso il boundary pubblico del runtime.
            float tickInterval = SimulationHost.ResolveTickIntervalSeconds(sim);

            // Assert: traversal off di default e durata base configurabile presente.
            Assert.That(sim.ResolveTicksPerSecond(), Is.EqualTo(5));
            Assert.That(tickInterval, Is.EqualTo(0.2f).Within(0.0001f));
            Assert.That(sim.ResolveEnableJobRunningActionTraversal(), Is.False);
            Assert.That(sim.ResolveBaseWalkCellDurationTicks(), Is.EqualTo(3));
        }

        // =============================================================================
        // RuntimeTimingLegacyLayoutStillResolvesAsFallback
        // =============================================================================
        /// <summary>
        /// <para>
        /// Verifica la compatibilita' transitoria con il layout precedente, dove
        /// <c>ticksPerSecond</c> era root e i parametri traversal vivevano in
        /// <c>movement</c>.
        /// </para>
        /// </summary>
        [Test]
        public void RuntimeTimingLegacyLayoutStillResolvesAsFallback()
        {
            // Arrange: vecchio layout pre-gruppo Tick. Il test protegge solo la lettura
            // compatibile, non promuove questo schema a fonte canonica.
            const string json = "{"
                + "\"ticksPerSecond\":7,"
                + "\"movement\":{"
                + "\"enableJobRunningActionTraversal\":true,"
                + "\"baseWalkCellDurationTicks\":4"
                + "}"
                + "}";

            // Act: JsonUtility popola i campi legacy e i resolver li traducono nella
            // semantica runtime corrente.
            var sim = JsonUtility.FromJson<SimulationParams>(json);
            float tickInterval = SimulationHost.ResolveTickIntervalSeconds(sim);

            // Assert: layout vecchio ancora leggibile, senza duplicare consumer.
            Assert.That(sim.ResolveTicksPerSecond(), Is.EqualTo(7));
            Assert.That(tickInterval, Is.EqualTo(1f / 7f).Within(0.0001f));
            Assert.That(sim.ResolveEnableJobRunningActionTraversal(), Is.True);
            Assert.That(sim.ResolveBaseWalkCellDurationTicks(), Is.EqualTo(4));
        }

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
        // WaitTicksJobAdvancesWhenDecisionCadenceIsNotDue
        // =============================================================================
        /// <summary>
        /// <para>
        /// Verifica che un job gia' attivo con action <c>WaitTicks</c> continui ad
        /// avanzare sul tick esecutivo anche quando la cadenza decisionale ordinaria
        /// del bridge needs-decision non e' dovuta.
        /// </para>
        ///
        /// <para><b>v0.11c.03c - Cadence esecutiva separata da cadence decisionale</b></para>
        /// <para>
        /// Il test protegge il confine tra <c>JobExecutionSystem</c> e
        /// decisione ordinaria: il job gia' assegnato completa comunque tramite il
        /// progress di <c>RunningActionExecutor</c>, senza bisogno di una rule legacy.
        /// Nessuna command decisionale viene emessa.
        /// </para>
        /// </summary>
        [Test]
        public void WaitTicksJobAdvancesWhenDecisionCadenceIsNotDue()
        {
            // Arrange: job gia' attivo e bridge decisionale volutamente non dovuto
            // al tick 1. Il job execution resta l'unico componente autorizzato ad
            // avanzare la running action.
            var world = MakeWorldWithNpc(out int npcId);
            var job = MakeWaitJob(npcId, "job-wait-decision-cadence", durationTicks: 2);
            Assert.That(world.JobRuntimeState.TryAssignJob(npcId, job, tick: 0, out var reason), Is.True, reason);
            var jobExecution = new JobExecutionSystem();
            var decisionCommands = new List<ICommand>();
            var telemetry = new Telemetry();

            // Act 1: primo tick esecutivo, la running action parte e resta running.
            jobExecution.Update(world, new Tick(0, 1f), new MessageBus(), telemetry);
            Assert.That(world.JobRuntimeState.HasActiveJob(npcId), Is.True);
            Assert.That(world.JobRuntimeState.RunningActions.Count, Is.EqualTo(1));

            // Act 2: al tick 1 non serve alcuna rivalutazione decisionale: il job
            // esecutivo deve comunque completare.
            jobExecution.Update(world, new Tick(1, 1f), new MessageBus(), telemetry);

            // Assert: il job completa grazie alla cadence esecutiva tick-based, non
            // alla rivalutazione decisionale ordinaria.
            Assert.That(job.Status, Is.EqualTo(JobStatus.Completed));
            Assert.That(world.JobRuntimeState.HasActiveJob(npcId), Is.False);
            Assert.That(world.JobRuntimeState.RunningActions.Count, Is.EqualTo(0));
            Assert.That(world.JobRuntimeState.CommandBuffer.Count, Is.EqualTo(0));
            Assert.That(decisionCommands.Count, Is.EqualTo(0));
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
        // DeclaredDistantMoveTargetPreparesRouteAndHonorsConfiguredDuration
        // =============================================================================
        /// <summary>
        /// <para>
        /// Verifica che, con runtime movimento Job acceso, un target distante gia'
        /// dichiarato dallo step prepari una route locale diretta e poi rispetti la
        /// durata multi-tick configurata per ogni cella.
        /// </para>
        /// </summary>
        [Test]
        public void DeclaredDistantMoveTargetPreparesRouteAndHonorsConfiguredDuration()
        {
            // Arrange: gate acceso, target distante e nessun DirectCommitExecution
            // precaricato. Il Job non sceglie un nuovo target: prepara solo una route
            // locale verso la cella gia' dichiarata dal piano.
            var world = MakeWorldWithNpc(out int npcId);
            EnableOneCellTraversal(world, durationTicks: 3);
            var startCell = world.GridPos[npcId];
            var distantTarget = new Vector2Int(startCell.X + 2, startCell.Y);
            var job = MakeMoveJob(npcId, "job-move-distant-legacy", distantTarget);
            Assert.That(world.JobRuntimeState.TryAssignJob(npcId, job, tick: 0, out var reason), Is.True, reason);
            var system = new JobExecutionSystem();

            // Act 1: primo tick, route preparata e prima cella in progress.
            system.Update(world, new Tick(0, 1f), new MessageBus(), new Telemetry());

            // Assert 1: nessun command legacy; la posizione resta ferma durante il
            // progress e il direct state e' ora consumabile dal Job runtime.
            Assert.That(world.JobRuntimeState.RunningActions.Count, Is.EqualTo(1));
            Assert.That(world.JobRuntimeState.CommandBuffer.Count, Is.EqualTo(0));
            Assert.That(world.GridPos[npcId].X, Is.EqualTo(startCell.X));
            Assert.That(world.GridPos[npcId].Y, Is.EqualTo(startCell.Y));
            Assert.That(world.Pathfinding.DirectCommitExecution.ContainsKey(npcId), Is.True);
            Assert.That(world.JobRuntimeState.HasActiveJob(npcId), Is.True);

            // Act 2: secondo tick ancora progress, terzo tick completa solo la
            // prima cella. Il target finale richiede un secondo traversal.
            system.Update(world, new Tick(1, 1f), new MessageBus(), new Telemetry());
            Assert.That(world.GridPos[npcId].X, Is.EqualTo(startCell.X));

            system.Update(world, new Tick(2, 1f), new MessageBus(), new Telemetry());
            Assert.That(world.GridPos[npcId].X, Is.EqualTo(startCell.X + 1));
            Assert.That(world.GridPos[npcId].Y, Is.EqualTo(startCell.Y));
            Assert.That(world.JobRuntimeState.HasActiveJob(npcId), Is.True);
        }

        // =============================================================================
        // DeclaredTargetOutsideCurrentFacingWithoutKnownRouteFails
        // =============================================================================
        /// <summary>
        /// <para>
        /// Verifica che MoveTo non inventi un tragitto diretto verso una cella gia'
        /// dichiarata dal job quando quella cella non e' acquisita localmente e non
        /// esiste una route landmark utilizzabile.
        /// </para>
        ///
        /// <para><b>Principio architetturale: conoscenza del target non equivale a conoscenza della strada</b></para>
        /// <para>
        /// Un incarico puo' nascere da una belief che indica "il cibo e' li'", ma
        /// questo non autorizza MoveTo a conoscere automaticamente una strada verso
        /// la cella. Senza acquisizione locale e senza landmark route soggettiva, lo
        /// step deve fallire e consegnare il problema alla matrice dei recuperi.
        /// </para>
        /// </summary>
        [Test]
        public void DeclaredTargetOutsideCurrentFacingWithoutKnownRouteFails()
        {
            var world = MakeWorldWithNpc(out int npcId);
            EnableOneCellTraversal(world, durationTicks: 3);
            var startCell = world.GridPos[npcId];
            world.NpcFacing[npcId] = CardinalDirection.East;
            var sideTarget = new Vector2Int(startCell.X, startCell.Y + 2);
            var job = MakeMoveJob(npcId, "job-move-side-belief-target", sideTarget);
            Assert.That(world.JobRuntimeState.TryAssignJob(npcId, job, tick: 0, out var reason), Is.True, reason);
            var system = new JobExecutionSystem();

            system.Update(world, new Tick(0, 1f), new MessageBus(), new Telemetry());

            Assert.That(world.JobRuntimeState.HasActiveJob(npcId), Is.False);
            Assert.That(job.Status, Is.EqualTo(JobStatus.Failed));
            Assert.That(job.FailureReason, Is.EqualTo(JobFailureReason.MovementFailed));
            Assert.That(world.JobRuntimeState.RunningActions.Count, Is.EqualTo(0));
            Assert.That(world.JobRuntimeState.CommandBuffer.Count, Is.EqualTo(0));
            Assert.That(world.Pathfinding.DirectCommitExecution.ContainsKey(npcId), Is.False);
            Assert.That(world.GridPos[npcId].X, Is.EqualTo(startCell.X));
            Assert.That(world.GridPos[npcId].Y, Is.EqualTo(startCell.Y));
        }

        // =============================================================================
        // EatKnownFoodMoveTargetPreparesRouteAndHonorsConfiguredDuration
        // =============================================================================
        /// <summary>
        /// <para>
        /// Riproduce il caso runtime segnalato: l'NPC ha un job <c>EatKnownFood</c>
        /// verso uno stock gia' scelto, il movimento Job e' acceso e non esiste una
        /// route precaricata dal vecchio <c>MovementSystem</c>.
        /// </para>
        ///
        /// <para><b>Principio architetturale: EatKnownFood non cerca cibo durante il movimento</b></para>
        /// <para>
        /// Il test non fa query di belief, non sceglie un target alternativo e non
        /// passa da <c>SetMoveIntentCommand</c>. Verifica solo che il target gia'
        /// presente nel Job possa essere tradotto in una route locale e consumato con
        /// la durata configurata.
        /// </para>
        /// </summary>
        [Test]
        public void EatKnownFoodMoveTargetPreparesRouteAndHonorsConfiguredDuration()
        {
            var world = MakeWorldWithNpc(out int npcId);
            EnableOneCellTraversal(world, durationTicks: 3);
            var startCell = world.GridPos[npcId];
            int foodId = RegisterCommunityFoodStock(world, objectId: 8101, x: startCell.X + 2, y: startCell.Y, units: 3);
            var job = MakeFoodJob(npcId, foodId, new Vector2Int(startCell.X + 2, startCell.Y));
            Assert.That(world.JobRuntimeState.TryAssignJob(npcId, job, tick: 0, out var reason), Is.True, reason);
            var system = new JobExecutionSystem();

            system.Update(world, new Tick(0, 1f), new MessageBus(), new Telemetry());

            Assert.That(world.JobRuntimeState.RunningActions.Count, Is.EqualTo(1));
            Assert.That(world.JobRuntimeState.CommandBuffer.Count, Is.EqualTo(0));
            Assert.That(world.GridPos[npcId].X, Is.EqualTo(startCell.X));
            Assert.That(world.GridPos[npcId].Y, Is.EqualTo(startCell.Y));
            Assert.That(world.Pathfinding.DirectCommitExecution.ContainsKey(npcId), Is.True);
            Assert.That(world.JobRuntimeState.HasActiveJob(npcId), Is.True);

            system.Update(world, new Tick(1, 1f), new MessageBus(), new Telemetry());
            Assert.That(world.GridPos[npcId].X, Is.EqualTo(startCell.X));
            Assert.That(world.GridPos[npcId].Y, Is.EqualTo(startCell.Y));

            system.Update(world, new Tick(2, 1f), new MessageBus(), new Telemetry());
            Assert.That(world.GridPos[npcId].X, Is.EqualTo(startCell.X + 1));
            Assert.That(world.GridPos[npcId].Y, Is.EqualTo(startCell.Y));
            Assert.That(world.JobRuntimeState.HasActiveJob(npcId), Is.True);
        }

        // =============================================================================
        // EatKnownFoodBeliefTargetOutsideCurrentFacingStillPreparesRoute
        // =============================================================================
        /// <summary>
        /// <para>
        /// Verifica il caso runtime critico: <c>EatKnownFood</c> nasce da una belief
        /// target gia' scelta, quindi <c>MoveTo</c> non deve pretendere che il cibo
        /// sia acquisito nel cono visivo del tick corrente. Il target non viene
        /// ricalcolato qui: viene solo tradotto in route operativa verso la cella
        /// dichiarata dal Job.
        /// </para>
        /// </summary>
        [Test]
        public void EatKnownFoodBeliefTargetOutsideCurrentFacingStillPreparesRoute()
        {
            var world = MakeWorldWithNpc(out int npcId);
            EnableOneCellTraversal(world, durationTicks: 2);
            var startCell = world.GridPos[npcId];
            world.NpcFacing[npcId] = CardinalDirection.East;
            int foodId = RegisterCommunityFoodStock(world, objectId: 8102, x: startCell.X, y: startCell.Y + 2, units: 3);
            var job = MakeFoodJob(npcId, foodId, new Vector2Int(startCell.X, startCell.Y + 2));
            Assert.That(world.JobRuntimeState.TryAssignJob(npcId, job, tick: 0, out var reason), Is.True, reason);
            var system = new JobExecutionSystem();

            system.Update(world, new Tick(0, 1f), new MessageBus(), new Telemetry());

            Assert.That(world.JobRuntimeState.HasActiveJob(npcId), Is.True);
            Assert.That(job.Status, Is.EqualTo(JobStatus.Running));
            Assert.That(world.JobRuntimeState.RunningActions.Count, Is.EqualTo(1));
            Assert.That(world.JobRuntimeState.CommandBuffer.Count, Is.EqualTo(0));
            Assert.That(world.Pathfinding.DirectCommitExecution.ContainsKey(npcId), Is.True);
            Assert.That(world.GridPos[npcId].X, Is.EqualTo(startCell.X));
            Assert.That(world.GridPos[npcId].Y, Is.EqualTo(startCell.Y));
        }

        // =============================================================================
        // DeclaredDiagonalMoveTargetPreparesRouteAndHonorsConfiguredDuration
        // =============================================================================
        /// <summary>
        /// <para>
        /// Verifica che un target diagonale gia' dichiarato venga normalizzato in
        /// route cardinale cella-per-cella, senza usare il movimento legacy.
        /// </para>
        /// </summary>
        [Test]
        public void DeclaredDiagonalMoveTargetPreparesRouteAndHonorsConfiguredDuration()
        {
            // Arrange: il target diagonale non e' un traversal one-cell, ma puo'
            // diventare una route dichiarata a due passi cardinali.
            var world = MakeWorldWithNpc(out int npcId);
            EnableOneCellTraversal(world, durationTicks: 2);
            var startCell = world.GridPos[npcId];
            var diagonalTarget = new Vector2Int(startCell.X + 1, startCell.Y + 1);
            var job = MakeMoveJob(npcId, "job-move-diagonal-legacy", diagonalTarget);
            Assert.That(world.JobRuntimeState.TryAssignJob(npcId, job, tick: 0, out var reason), Is.True, reason);
            var system = new JobExecutionSystem();

            // Act: parte il primo passo della route dichiarata.
            system.Update(world, new Tick(0, 1f), new MessageBus(), new Telemetry());

            // Assert: nessun command legacy e nessun salto diagonale immediato.
            Assert.That(world.JobRuntimeState.RunningActions.Count, Is.EqualTo(1));
            Assert.That(world.JobRuntimeState.CommandBuffer.Count, Is.EqualTo(0));
            Assert.That(world.GridPos[npcId].X, Is.EqualTo(startCell.X));
            Assert.That(world.GridPos[npcId].Y, Is.EqualTo(startCell.Y));
            Assert.That(world.JobRuntimeState.HasActiveJob(npcId), Is.True);
            Assert.That(world.Pathfinding.DirectCommitExecution.ContainsKey(npcId), Is.True);

            // Act/Assert: al secondo tick completa solo il primo passo cardinale.
            system.Update(world, new Tick(1, 1f), new MessageBus(), new Telemetry());
            Assert.That(world.GridPos[npcId].X, Is.EqualTo(startCell.X + 1));
            Assert.That(world.GridPos[npcId].Y, Is.EqualTo(startCell.Y));
            Assert.That(world.JobRuntimeState.HasActiveJob(npcId), Is.True);
        }

        // =============================================================================
        // OneCellMoveTraversalUsesRunningActionAndMutatesWorldOnlyAtCompletion
        // =============================================================================
        /// <summary>
        /// <para>
        /// Verifica il primo path controllato MoveToCell -> running action traversal
        /// -> completion -> mutazione posizione finale.
        /// </para>
        /// </summary>
        [Test]
        public void OneCellMoveTraversalUsesRunningActionAndMutatesWorldOnlyAtCompletion()
        {
            // Arrange: abilitiamo il gate v0.11c.02g solo nel test. Il target e'
            // cardinale e adiacente, quindi non serve pathfinding e non si tocca
            // MovementSystem.
            var world = MakeWorldWithNpc(out int npcId);
            EnableOneCellTraversal(world, durationTicks: 2);
            var startCell = world.GridPos[npcId];
            var targetCell = new Vector2Int(startCell.X + 1, startCell.Y);
            var job = MakeMoveJob(npcId, "job-move-one-cell-running", targetCell);
            Assert.That(world.JobRuntimeState.TryAssignJob(npcId, job, tick: 0, out var reason), Is.True, reason);
            var system = new JobExecutionSystem();

            // Act 1: primo tick, progress interno ma posizione ancora sorgente.
            system.Update(world, new Tick(0, 1f), new MessageBus(), new Telemetry());

            // Assert 1: nessuna posizione intermedia e nessun command durante il progress.
            Assert.That(world.JobRuntimeState.HasActiveJob(npcId), Is.True);
            Assert.That(world.JobRuntimeState.RunningActions.Count, Is.EqualTo(1));
            Assert.That(world.JobRuntimeState.CommandBuffer.Count, Is.EqualTo(0));
            Assert.That(world.GridPos[npcId].X, Is.EqualTo(startCell.X));
            Assert.That(world.GridPos[npcId].Y, Is.EqualTo(startCell.Y));

            // Act 2: secondo tick, completion interna e mutazione finale atomica.
            system.Update(world, new Tick(1, 1f), new MessageBus(), new Telemetry());

            // Assert 2: lo store volatile e' pulito e l'NPC arriva direttamente in B.
            Assert.That(world.JobRuntimeState.HasActiveJob(npcId), Is.False);
            Assert.That(world.JobRuntimeState.RunningActions.Count, Is.EqualTo(0));
            Assert.That(world.JobRuntimeState.CommandBuffer.Count, Is.EqualTo(0));
            Assert.That(world.GridPos[npcId].X, Is.EqualTo(targetCell.x));
            Assert.That(world.GridPos[npcId].Y, Is.EqualTo(targetCell.y));
            Assert.That(job.Status, Is.EqualTo(JobStatus.Completed));
        }

        // =============================================================================
        // KnownRouteMoveToConsumesCellsThroughRunningActionTraversal
        // =============================================================================
        /// <summary>
        /// <para>
        /// Verifica il primo percorso v0.15.6: un <c>MoveToCell</c> distante puo'
        /// consumare una route gia' nota cella per cella tramite la running action di
        /// traversal, senza accodare <c>SetMoveIntentCommand</c>.
        /// </para>
        ///
        /// <para><b>Principio architetturale: route nota prima del movimento Job</b></para>
        /// <para>
        /// Il test non chiede al Job di pianificare. Precarica nello stato pathfinding
        /// una route diretta gia' costruita, poi controlla che il Job la usi come dato
        /// esecutivo locale: ogni cella richiede la durata configurata, la posizione
        /// cambia solo a completion e il job si chiude solo quando il target finale e'
        /// fisicamente raggiunto.
        /// </para>
        /// </summary>
        [Test]
        public void KnownRouteMoveToConsumesCellsThroughRunningActionTraversal()
        {
            // Arrange: la route nota contiene due attraversamenti fisici:
            // (1,1) -> (2,1) -> (3,1). Il Job deve restare sullo stesso step fino
            // alla seconda completion, perche' il target semantico e' la cella finale.
            var world = MakeWorldWithNpc(out int npcId);
            EnableOneCellTraversal(world, durationTicks: 2);
            var startCell = world.GridPos[npcId];
            var targetCell = new Vector2Int(startCell.X + 2, startCell.Y);
            world.SetDebugDirectPathForNpc(
                npcId,
                new List<Vector2Int>
                {
                    new(startCell.X, startCell.Y),
                    new(startCell.X + 1, startCell.Y),
                    targetCell
                });

            var job = MakeMoveJob(npcId, "job-move-known-route", targetCell);
            Assert.That(world.JobRuntimeState.TryAssignJob(npcId, job, tick: 0, out var reason), Is.True, reason);
            var system = new JobExecutionSystem();

            // Act + assert: primo tick, la prima cella e' in progress e nessun command
            // legacy viene accodato.
            system.Update(world, new Tick(0, 1f), new MessageBus(), new Telemetry());
            Assert.That(world.JobRuntimeState.RunningActions.Count, Is.EqualTo(1));
            Assert.That(world.JobRuntimeState.CommandBuffer.Count, Is.EqualTo(0));
            Assert.That(world.GridPos[npcId].X, Is.EqualTo(startCell.X));
            Assert.That(world.JobRuntimeState.HasActiveJob(npcId), Is.True);

            // Act + assert: secondo tick, il primo attraversamento completa, ma il job
            // non deve avanzare a completed perche' il target finale non e' ancora stato
            // raggiunto.
            system.Update(world, new Tick(1, 1f), new MessageBus(), new Telemetry());
            Assert.That(world.JobRuntimeState.RunningActions.Count, Is.EqualTo(0));
            Assert.That(world.GridPos[npcId].X, Is.EqualTo(startCell.X + 1));
            Assert.That(world.GridPos[npcId].Y, Is.EqualTo(startCell.Y));
            Assert.That(world.JobRuntimeState.HasActiveJob(npcId), Is.True);

            // Act + assert: la seconda cella usa un'altra running action, sempre senza
            // command legacy.
            system.Update(world, new Tick(2, 1f), new MessageBus(), new Telemetry());
            Assert.That(world.JobRuntimeState.RunningActions.Count, Is.EqualTo(1));
            Assert.That(world.JobRuntimeState.CommandBuffer.Count, Is.EqualTo(0));
            Assert.That(world.GridPos[npcId].X, Is.EqualTo(startCell.X + 1));

            // Act + assert: al completamento della seconda cella, il target finale e'
            // raggiunto e la state machine puo' chiudere positivamente il job.
            system.Update(world, new Tick(3, 1f), new MessageBus(), new Telemetry());
            Assert.That(world.JobRuntimeState.RunningActions.Count, Is.EqualTo(0));
            Assert.That(world.JobRuntimeState.CommandBuffer.Count, Is.EqualTo(0));
            Assert.That(world.GridPos[npcId].X, Is.EqualTo(targetCell.x));
            Assert.That(world.GridPos[npcId].Y, Is.EqualTo(targetCell.y));
            Assert.That(world.JobRuntimeState.HasActiveJob(npcId), Is.False);
        }

        // =============================================================================
        // KnownRouteMoveToOpensUnlockedDoorBeforeTraversal
        // =============================================================================
        /// <summary>
        /// <para>
        /// Verifica che il movimento Job su route nota gestisca una porta chiusa non
        /// bloccata come micro-operazione locale prima del traversal fisico.
        /// </para>
        ///
        /// <para><b>Principio architetturale: il MoveTo possiede le interazioni fisiche immediate</b></para>
        /// <para>
        /// La porta e' nella prossima cella della route gia' nota. Il Job non deve
        /// accodare un move intent legacy ne' delegare l'apertura a <c>MovementSystem</c>:
        /// apre la porta con il command esistente, pubblica l'evento mondo e poi avvia
        /// il traversal multi-tick verso quella stessa cella.
        /// </para>
        /// </summary>
        [Test]
        public void KnownRouteMoveToOpensUnlockedDoorBeforeTraversal()
        {
            // Arrange: registriamo localmente una definizione porta minimale per il
            // World di test. La porta blocca il movimento da chiusa, ma non e' locked.
            var world = MakeWorldWithNpc(out int npcId);
            EnableOneCellTraversal(world, durationTicks: 2);
            world.ObjectDefs["qa_door"] = new ObjectDef
            {
                Id = "qa_door",
                DisplayName = "QA Door",
                IsOccluder = true,
                IsInteractable = true,
                IsDoor = true,
                IsLockable = true,
                BlocksVision = true,
                BlocksMovement = true,
                VisionCost = 1f
            };

            var startCell = world.GridPos[npcId];
            var doorCell = new Vector2Int(startCell.X + 1, startCell.Y);
            var targetCell = new Vector2Int(startCell.X + 2, startCell.Y);
            int doorObjectId = world.CreateObject("qa_door", doorCell.x, doorCell.y);
            Assert.That(doorObjectId, Is.GreaterThan(0));
            Assert.That(world.BlocksMovementAt(doorCell.x, doorCell.y), Is.True);

            world.SetDebugDirectPathForNpc(
                npcId,
                new List<Vector2Int>
                {
                    new(startCell.X, startCell.Y),
                    doorCell,
                    targetCell
                });

            var job = MakeMoveJob(npcId, "job-move-known-route-door", targetCell);
            Assert.That(world.JobRuntimeState.TryAssignJob(npcId, job, tick: 0, out var reason), Is.True, reason);
            var bus = new MessageBus();
            var system = new JobExecutionSystem();

            // Act: il primo tick apre la porta e avvia il traversal. La posizione
            // resta sorgente perche' la completion richiede due tick.
            system.Update(world, new Tick(0, 1f), bus, new Telemetry());

            // Assert: la porta e' aperta, la cache movimento non blocca piu' la cella,
            // l'evento mondo e' stato pubblicato e nessun command legacy e' stato
            // accodato dal Job.
            Assert.That(world.Objects[doorObjectId].IsOpen, Is.True);
            Assert.That(world.BlocksMovementAt(doorCell.x, doorCell.y), Is.False);
            Assert.That(bus.Count, Is.EqualTo(1));
            Assert.That(world.JobRuntimeState.RunningActions.Count, Is.EqualTo(1));
            Assert.That(world.JobRuntimeState.CommandBuffer.Count, Is.EqualTo(0));
            Assert.That(world.GridPos[npcId].X, Is.EqualTo(startCell.X));
            Assert.That(world.GridPos[npcId].Y, Is.EqualTo(startCell.Y));
        }

        // =============================================================================
        // KnownRouteMoveToReturnsLockedDoorFailure
        // =============================================================================
        /// <summary>
        /// <para>
        /// Verifica che una porta chiusa a chiave attraversata da MoveTo produca un
        /// fallimento esplicito, classificabile dalla matrice dei recuperi.
        /// </para>
        ///
        /// <para><b>Principio architetturale: errore fisico locale, non blocco silenzioso</b></para>
        /// <para>
        /// La running action MoveTo non deve aprire porte bloccate e non deve
        /// trasformarle in generico "non mi muovo". Restituisce invece
        /// <c>TraversalDoorLocked</c>, che il classifier traduce in
        /// <c>DoorLocked</c> e che il JSON delle recovery puo' gestire in modo
        /// dichiarativo.
        /// </para>
        /// </summary>
        [Test]
        public void KnownRouteMoveToReturnsLockedDoorFailure()
        {
            var world = MakeWorldWithNpc(out int npcId);
            EnableOneCellTraversal(world, durationTicks: 2);
            world.ObjectDefs["qa_locked_door"] = new ObjectDef
            {
                Id = "qa_locked_door",
                DisplayName = "QA Locked Door",
                IsOccluder = true,
                IsInteractable = true,
                IsDoor = true,
                IsLockable = true,
                BlocksVision = true,
                BlocksMovement = true,
                VisionCost = 1f
            };

            var startCell = world.GridPos[npcId];
            var doorCell = new Vector2Int(startCell.X + 1, startCell.Y);
            var targetCell = new Vector2Int(startCell.X + 2, startCell.Y);
            int doorObjectId = world.CreateObject("qa_locked_door", doorCell.x, doorCell.y);
            world.Objects[doorObjectId].IsLocked = true;
            Assert.That(world.BlocksMovementAt(doorCell.x, doorCell.y), Is.True);

            world.SetDebugDirectPathForNpc(
                npcId,
                new List<Vector2Int>
                {
                    new(startCell.X, startCell.Y),
                    doorCell,
                    targetCell
                });

            var job = MakeMoveJob(npcId, "job-move-known-route-locked-door", targetCell);
            Assert.That(world.JobRuntimeState.TryAssignJob(npcId, job, tick: 0, out var reason), Is.True, reason);
            var system = new JobExecutionSystem();

            system.Update(world, new Tick(0, 1f), new MessageBus(), new Telemetry());

            Assert.That(world.Objects[doorObjectId].IsOpen, Is.False);
            Assert.That(world.JobRuntimeState.HasActiveJob(npcId), Is.False);
            Assert.That(job.Status, Is.EqualTo(JobStatus.Failed));
            Assert.That(job.FailureReason, Is.EqualTo(JobFailureReason.MovementFailed));
            Assert.That(world.GridPos[npcId].X, Is.EqualTo(startCell.X));
            Assert.That(world.GridPos[npcId].Y, Is.EqualTo(startCell.Y));
        }

        // =============================================================================
        // OneCellMoveTraversalHonorsConfiguredFourTickDuration
        // =============================================================================
        /// <summary>
        /// <para>
        /// Verifica esplicitamente che il traversal one-cell del Job Layer rispetti
        /// la durata configurata e non ricada nel movimento legacy immediato.
        /// </para>
        /// </summary>
        [Test]
        public void OneCellMoveTraversalHonorsConfiguredFourTickDuration()
        {
            // Arrange: la durata cella e' 4 tick. Il target e' cardinale adiacente,
            // quindi il path temporale running-action e' eleggibile e MovementSystem
            // non deve essere necessario per mutare la posizione.
            var world = MakeWorldWithNpc(out int npcId);
            EnableOneCellTraversal(world, durationTicks: 4);
            var startCell = world.GridPos[npcId];
            var targetCell = new Vector2Int(startCell.X + 1, startCell.Y);
            var job = MakeObjectTargetMoveJob(npcId, "job-move-four-tick-duration", targetObjectId: 7201, targetCell);
            Assert.That(world.JobRuntimeState.TryAssignJob(npcId, job, tick: 0, out var reason), Is.True, reason);
            var system = new JobExecutionSystem();

            // Act/Assert: i primi tre tick avanzano solo il progress interno. La
            // posizione resta sorgente, la running action resta viva e la cella
            // destinazione resta riservata al job corrente.
            for (int tick = 0; tick < 3; tick++)
            {
                system.Update(world, new Tick(tick, 1f), new MessageBus(), new Telemetry());

                Assert.That(world.JobRuntimeState.HasActiveJob(npcId), Is.True);
                Assert.That(world.JobRuntimeState.RunningActions.Count, Is.EqualTo(1));
                Assert.That(world.JobRuntimeState.CommandBuffer.Count, Is.EqualTo(0));
                Assert.That(world.GridPos[npcId].X, Is.EqualTo(startCell.X));
                Assert.That(world.GridPos[npcId].Y, Is.EqualTo(startCell.Y));
                Assert.That(world.JobRuntimeState.Reservations.TryGetByTarget(ReservationTargetKind.Cell, targetCell, -1, out var held), Is.True);
                Assert.That(held.JobId, Is.EqualTo(job.JobId));
            }

            // Act: il quarto tick completa la running action e applica la sola
            // mutazione finale della posizione.
            system.Update(world, new Tick(3, 1f), new MessageBus(), new Telemetry());

            // Assert: il job e' chiuso, lo stato volatile e le reservation sono
            // ripulite, e nessun comando di movimento legacy e' stato emesso.
            Assert.That(world.JobRuntimeState.HasActiveJob(npcId), Is.False);
            Assert.That(world.JobRuntimeState.RunningActions.Count, Is.EqualTo(0));
            Assert.That(world.JobRuntimeState.CommandBuffer.Count, Is.EqualTo(0));
            Assert.That(world.JobRuntimeState.Reservations.Count, Is.EqualTo(0));
            Assert.That(world.GridPos[npcId].X, Is.EqualTo(targetCell.x));
            Assert.That(world.GridPos[npcId].Y, Is.EqualTo(targetCell.y));
            Assert.That(job.Status, Is.EqualTo(JobStatus.Completed));
        }

        // =============================================================================
        // OneCellMoveTraversalHoldsDestinationReservationDuringProgress
        // =============================================================================
        /// <summary>
        /// <para>
        /// Verifica che il traversal gated possa acquisire una reservation temporale
        /// action-scoped sulla cella destinazione quando il job possiede solo una
        /// reservation oggetto, e che la mantenga fino alla completion.
        /// </para>
        /// </summary>
        [Test]
        public void OneCellMoveTraversalHoldsDestinationReservationDuringProgress()
        {
            // Arrange: il JobRequest prenota un oggetto fittizio; la cella del
            // MoveToCell deve quindi essere protetta dal traversal stesso.
            var world = MakeWorldWithNpc(out int npcId);
            EnableOneCellTraversal(world, durationTicks: 3);
            var startCell = world.GridPos[npcId];
            var targetCell = new Vector2Int(startCell.X + 1, startCell.Y);
            var job = MakeObjectTargetMoveJob(npcId, "job-move-reservation-held", targetObjectId: 7001, targetCell);
            Assert.That(world.JobRuntimeState.TryAssignJob(npcId, job, tick: 0, out var reason), Is.True, reason);
            Assert.That(world.JobRuntimeState.Reservations.Count, Is.EqualTo(1));
            var system = new JobExecutionSystem();

            // Act 1: primo tick, la running action parte e acquisisce la cella.
            system.Update(world, new Tick(0, 1f), new MessageBus(), new Telemetry());

            // Assert 1: object reservation + destination reservation sono entrambe vive.
            Assert.That(world.JobRuntimeState.RunningActions.Count, Is.EqualTo(1));
            Assert.That(world.JobRuntimeState.Reservations.Count, Is.EqualTo(2));
            Assert.That(world.JobRuntimeState.Reservations.TryGetByTarget(ReservationTargetKind.Cell, targetCell, -1, out var held), Is.True);
            Assert.That(held.JobId, Is.EqualTo(job.JobId));
            Assert.That(world.GridPos[npcId].X, Is.EqualTo(startCell.X));
            Assert.That(world.GridPos[npcId].Y, Is.EqualTo(startCell.Y));

            // Act 2: secondo tick, la reservation resta mantenuta durante il progress.
            system.Update(world, new Tick(1, 1f), new MessageBus(), new Telemetry());

            // Assert 2: nessuna mutazione anticipata del World e reservation ancora viva.
            Assert.That(world.JobRuntimeState.HasActiveJob(npcId), Is.True);
            Assert.That(world.JobRuntimeState.Reservations.TryGetByTarget(ReservationTargetKind.Cell, targetCell, -1, out held), Is.True);
            Assert.That(held.JobId, Is.EqualTo(job.JobId));
            Assert.That(world.GridPos[npcId].X, Is.EqualTo(startCell.X));
            Assert.That(world.GridPos[npcId].Y, Is.EqualTo(startCell.Y));

            // Act 3: terzo tick, completion e cleanup di reservation/running action.
            system.Update(world, new Tick(2, 1f), new MessageBus(), new Telemetry());

            // Assert 3: il job completa, la cella viene raggiunta e non restano lock.
            Assert.That(world.JobRuntimeState.HasActiveJob(npcId), Is.False);
            Assert.That(world.JobRuntimeState.RunningActions.Count, Is.EqualTo(0));
            Assert.That(world.JobRuntimeState.Reservations.Count, Is.EqualTo(0));
            Assert.That(world.GridPos[npcId].X, Is.EqualTo(targetCell.x));
            Assert.That(world.GridPos[npcId].Y, Is.EqualTo(targetCell.y));
        }

        // =============================================================================
        // OneCellMoveTraversalReleasesDestinationReservationWhenInterruptedByJobFailure
        // =============================================================================
        /// <summary>
        /// <para>
        /// Verifica che un'interruzione/failure del job passi dal cleanup esistente
        /// di <c>JobRuntimeState</c> e non lasci reservation temporali appese.
        /// </para>
        /// </summary>
        [Test]
        public void OneCellMoveTraversalReleasesDestinationReservationWhenInterruptedByJobFailure()
        {
            // Arrange: partiamo un traversal con reservation cella action-scoped.
            var world = MakeWorldWithNpc(out int npcId);
            EnableOneCellTraversal(world, durationTicks: 3);
            var startCell = world.GridPos[npcId];
            var targetCell = new Vector2Int(startCell.X + 1, startCell.Y);
            var job = MakeObjectTargetMoveJob(npcId, "job-move-reservation-fail", targetObjectId: 7002, targetCell);
            Assert.That(world.JobRuntimeState.TryAssignJob(npcId, job, tick: 0, out var reason), Is.True, reason);
            var system = new JobExecutionSystem();
            system.Update(world, new Tick(0, 1f), new MessageBus(), new Telemetry());
            Assert.That(world.JobRuntimeState.Reservations.Count, Is.EqualTo(2));

            // Act: il path pubblico di failure rappresenta anche la cleanup usata da
            // preemption/cancel senza inventare una nuova policy di interruption.
            Assert.That(world.JobRuntimeState.FailCurrentJob(npcId, JobFailureReason.Cancelled, tick: 1, out var failReason), Is.True, failReason);

            // Assert: entrambe le reservation del job spariscono e la posizione resta A.
            Assert.That(world.JobRuntimeState.Reservations.Count, Is.EqualTo(0));
            Assert.That(world.JobRuntimeState.RunningActions.Count, Is.EqualTo(0));
            Assert.That(world.GridPos[npcId].X, Is.EqualTo(startCell.X));
            Assert.That(world.GridPos[npcId].Y, Is.EqualTo(startCell.Y));
        }

        // =============================================================================
        // OneCellMoveTraversalBlocksDeterministicallyOnDestinationContention
        // =============================================================================
        /// <summary>
        /// <para>
        /// Verifica la policy minima di contesa: due NPC non possono possedere la
        /// stessa destination reservation di traversal, e il secondo job resta in
        /// retry deterministico invece di preemptare o mutare il World.
        /// </para>
        /// </summary>
        [Test]
        public void OneCellMoveTraversalBlocksDeterministicallyOnDestinationContention()
        {
            // Arrange: due NPC adiacenti puntano alla stessa cella libera. Entrambi
            // hanno target oggetto distinti, quindi la contesa e' solo sulla cella
            // temporale del traversal.
            var world = MakeWorldWithNpc(out int firstNpcId);
            int secondNpcId = world.CreateNpc(
                NpcDnaProfile.CreateDefault("running_action_contention_qa"),
                NpcNeeds.Make(0.5f, 0.5f),
                new Arcontio.Core.Social { JusticePerception01 = 0.5f },
                x: 2,
                y: 2);
            EnableOneCellTraversal(world, durationTicks: 3);
            var targetCell = new Vector2Int(2, 1);
            var firstJob = MakeObjectTargetMoveJob(firstNpcId, "job-move-contention-a", targetObjectId: 7101, targetCell);
            var secondJob = MakeObjectTargetMoveJob(secondNpcId, "job-move-contention-b", targetObjectId: 7102, targetCell);
            Assert.That(world.JobRuntimeState.TryAssignJob(firstNpcId, firstJob, tick: 0, out var firstReason), Is.True, firstReason);
            Assert.That(world.JobRuntimeState.TryAssignJob(secondNpcId, secondJob, tick: 0, out var secondReason), Is.True, secondReason);
            var system = new JobExecutionSystem();

            // Act: il primo NPC acquisisce la cella; il secondo incontra contesa e
            // riceve Blocked(1) tramite la state machine.
            system.Update(world, new Tick(0, 1f), new MessageBus(), new Telemetry());

            // Assert: una sola running action di movimento puo' possedere il target.
            Assert.That(world.JobRuntimeState.RunningActions.Count, Is.EqualTo(1));
            Assert.That(world.JobRuntimeState.Reservations.TryGetByTarget(ReservationTargetKind.Cell, targetCell, -1, out var held), Is.True);
            Assert.That(held.JobId, Is.EqualTo(firstJob.JobId));
            Assert.That(world.JobRuntimeState.TryGetNpcState(secondNpcId, out var secondState), Is.True);
            Assert.That(secondState.HasActiveJob, Is.True);
            Assert.That(secondState.IsWaitingAt(0), Is.True);
            Assert.That(world.GridPos[firstNpcId].X, Is.EqualTo(1));
            Assert.That(world.GridPos[firstNpcId].Y, Is.EqualTo(1));
            Assert.That(world.GridPos[secondNpcId].X, Is.EqualTo(2));
            Assert.That(world.GridPos[secondNpcId].Y, Is.EqualTo(2));
        }

        // =============================================================================
        // OneCellMoveTraversalWritesReservationExplainability
        // =============================================================================
        /// <summary>
        /// <para>
        /// Verifica che il lifecycle reservation del traversal sia osservabile con le
        /// trace Accepted/Released gia' disponibili nel registry MBQD.
        /// </para>
        /// </summary>
        [Test]
        public void OneCellMoveTraversalWritesReservationExplainability()
        {
            // Arrange: usiamo il target oggetto fittizio per forzare una reservation
            // cella specifica del traversal, poi abilitiamo solo il registry locale.
            var world = MakeWorldWithNpcWithRunningActionExplainability(out int npcId);
            EnableOneCellTraversal(world, durationTicks: 2);
            var config = world.Config.Sim.memory_belief_decision_explainability;
            config.logReservation = true;
            var startCell = world.GridPos[npcId];
            var targetCell = new Vector2Int(startCell.X + 1, startCell.Y);
            var job = MakeObjectTargetMoveJob(npcId, "job-move-reservation-el", targetObjectId: 7201, targetCell);
            Assert.That(world.JobRuntimeState.TryAssignJob(npcId, job, tick: 0, out var reason), Is.True, reason);
            var system = new JobExecutionSystem();

            // Act: due tick completano traversal, acquisizione e rilascio.
            system.Update(world, new Tick(0, 1f), new MessageBus(), new Telemetry());
            system.Update(world, new Tick(1, 1f), new MessageBus(), new Telemetry());

            // Assert: l'EL mostra la reservation del traversal senza command di progress.
            Assert.That(world.MemoryBeliefDecisionExplainability.TryGetNpcStore(npcId, out var store), Is.True);
            var traces = new List<MemoryBeliefDecisionTrace>();
            store.CopyReservationTracesTo(traces);
            Assert.That(traces.Count, Is.EqualTo(2));
            Assert.That(traces[0].Reservation.Operation, Is.EqualTo(MemoryBeliefDecisionReservationOperation.Accepted));
            Assert.That(traces[1].Reservation.Operation, Is.EqualTo(MemoryBeliefDecisionReservationOperation.Released));
            Assert.That(traces[0].Reservation.TargetKind, Is.EqualTo(ReservationTargetKind.Cell));
            Assert.That(traces[0].Reservation.TargetCell, Is.EqualTo(targetCell));
            Assert.That(world.JobRuntimeState.CommandBuffer.Count, Is.EqualTo(0));
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
            var world = MakeWorldWithNpcWithRunningActionExplainability(out int npcId);
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

        // =============================================================================
        // OneCellMoveTraversalWritesLifecycleExplainability
        // =============================================================================
        /// <summary>
        /// <para>
        /// Verifica che il traversal one-cell gated riusi le trace lifecycle
        /// Started/Progress/Completed gia' introdotte per le running action.
        /// </para>
        /// </summary>
        [Test]
        public void OneCellMoveTraversalWritesLifecycleExplainability()
        {
            // Arrange: il gate movement e il gate EL sono abilitati solo in memoria.
            var world = MakeWorldWithNpcWithRunningActionExplainability(out int npcId);
            EnableOneCellTraversal(world, durationTicks: 2);
            var startCell = world.GridPos[npcId];
            var targetCell = new Vector2Int(startCell.X + 1, startCell.Y);
            var job = MakeMoveJob(npcId, "job-move-one-cell-el", targetCell);
            Assert.That(world.JobRuntimeState.TryAssignJob(npcId, job, tick: 0, out var reason), Is.True, reason);
            var system = new JobExecutionSystem();

            // Act: due tick completano il traversal.
            system.Update(world, new Tick(0, 1f), new MessageBus(), new Telemetry());
            system.Update(world, new Tick(1, 1f), new MessageBus(), new Telemetry());

            // Assert: lifecycle e key sono osservabili senza command e senza
            // posizioni frazionarie.
            Assert.That(world.MemoryBeliefDecisionExplainability.TryGetNpcStore(npcId, out var store), Is.True);
            var traces = new List<MemoryBeliefDecisionTrace>();
            store.CopyRunningActionTracesTo(traces);
            Assert.That(traces.Count, Is.EqualTo(3));
            Assert.That(traces[0].RunningAction.Operation, Is.EqualTo(MemoryBeliefDecisionRunningActionOperation.Started));
            Assert.That(traces[1].RunningAction.Operation, Is.EqualTo(MemoryBeliefDecisionRunningActionOperation.Progress));
            Assert.That(traces[2].RunningAction.Operation, Is.EqualTo(MemoryBeliefDecisionRunningActionOperation.Completed));
            Assert.That(traces[2].RunningAction.ActionKind, Is.EqualTo(RunningActionKind.Movement));
            Assert.That(traces[2].RunningAction.JobId, Is.EqualTo(job.JobId));
            Assert.That(traces[2].RunningAction.OwnerNpcId, Is.EqualTo(npcId));
            Assert.That(traces[2].RunningAction.ElapsedTicks, Is.EqualTo(2));
            Assert.That(traces[2].RunningAction.RequiredTicks, Is.EqualTo(2));
            Assert.That(world.JobRuntimeState.CommandBuffer.Count, Is.EqualTo(0));
            Assert.That(world.GridPos[npcId].X, Is.EqualTo(targetCell.x));
            Assert.That(world.GridPos[npcId].Y, Is.EqualTo(targetCell.y));
        }

        // =============================================================================
        // KnownRouteMoveToWritesLifecycleExplainabilityForEachCell
        // =============================================================================
        /// <summary>
        /// <para>
        /// Verifica che una route multi-cella consumata dal Job produca trace EL di
        /// running action per ciascun attraversamento fisico, senza introdurre un nuovo
        /// registro diagnostico parallelo.
        /// </para>
        ///
        /// <para><b>Principio architetturale: spiegabilita' del movimento Job senza doppia authority</b></para>
        /// <para>
        /// La route resta uno stato esecutivo gia' noto, mentre il progresso fisico
        /// viene spiegato dal lifecycle della running action: start, progress e
        /// completion per ogni cella. Questo consente al pannello Job/RunningAction di
        /// capire a che punto siamo senza interrogare il vecchio <c>MoveIntent</c>.
        /// </para>
        /// </summary>
        [Test]
        public void KnownRouteMoveToWritesLifecycleExplainabilityForEachCell()
        {
            // Arrange: due celle di movimento, EL running action acceso e nessun
            // MovementSystem. Il test osserva solo il registry MBQD gia' esistente.
            var world = MakeWorldWithNpcWithRunningActionExplainability(out int npcId);
            EnableOneCellTraversal(world, durationTicks: 2);
            var startCell = world.GridPos[npcId];
            var targetCell = new Vector2Int(startCell.X + 2, startCell.Y);
            world.SetDebugDirectPathForNpc(
                npcId,
                new List<Vector2Int>
                {
                    new(startCell.X, startCell.Y),
                    new(startCell.X + 1, startCell.Y),
                    targetCell
                });

            var job = MakeMoveJob(npcId, "job-move-known-route-el", targetCell);
            Assert.That(world.JobRuntimeState.TryAssignJob(npcId, job, tick: 0, out var reason), Is.True, reason);
            var system = new JobExecutionSystem();

            // Act: quattro tick completano due traversal da due tick ciascuno.
            for (int tick = 0; tick < 4; tick++)
                system.Update(world, new Tick(tick, 1f), new MessageBus(), new Telemetry());

            // Assert: due cicli completi Started/Progress/Completed vengono registrati
            // nello stesso store EL-MBQD; nessun command legacy viene generato.
            Assert.That(world.MemoryBeliefDecisionExplainability.TryGetNpcStore(npcId, out var store), Is.True);
            var traces = new List<MemoryBeliefDecisionTrace>();
            store.CopyRunningActionTracesTo(traces);
            Assert.That(traces.Count, Is.EqualTo(6));
            Assert.That(traces[0].RunningAction.Operation, Is.EqualTo(MemoryBeliefDecisionRunningActionOperation.Started));
            Assert.That(traces[1].RunningAction.Operation, Is.EqualTo(MemoryBeliefDecisionRunningActionOperation.Progress));
            Assert.That(traces[2].RunningAction.Operation, Is.EqualTo(MemoryBeliefDecisionRunningActionOperation.Completed));
            Assert.That(traces[3].RunningAction.Operation, Is.EqualTo(MemoryBeliefDecisionRunningActionOperation.Started));
            Assert.That(traces[4].RunningAction.Operation, Is.EqualTo(MemoryBeliefDecisionRunningActionOperation.Progress));
            Assert.That(traces[5].RunningAction.Operation, Is.EqualTo(MemoryBeliefDecisionRunningActionOperation.Completed));
            Assert.That(traces[5].RunningAction.ActionKind, Is.EqualTo(RunningActionKind.Movement));
            Assert.That(traces[5].RunningAction.JobId, Is.EqualTo(job.JobId));
            Assert.That(world.JobRuntimeState.CommandBuffer.Count, Is.EqualTo(0));
            Assert.That(world.GridPos[npcId].X, Is.EqualTo(targetCell.x));
            Assert.That(world.GridPos[npcId].Y, Is.EqualTo(targetCell.y));
        }

        // =============================================================================
        // DevTransportTemplatePreparesDeclaredRoutesWithoutMoveIntentFallback
        // =============================================================================
        /// <summary>
        /// <para>
        /// Verifica l'eccezione dev autorizzata per <c>transport.object_to_cell.v1</c>:
        /// il template puo' preparare route tecniche dichiarate per entrambe le tratte
        /// del trasporto senza ricadere in <c>SetMoveIntentCommand</c>.
        /// </para>
        ///
        /// <para><b>Principio architetturale: dev tool dichiarato, non planner runtime</b></para>
        /// <para>
        /// Il test usa il debug label <c>DevToolsForcedTransportObject</c>, cioe' lo
        /// stesso canale usato dal pannello F3. Il Job runtime resta rigoroso per i
        /// job ordinari: questa route tecnica esiste solo per il trasporto forzato
        /// dall'operatore e serve a validare che il template a piu' fasi consumi il
        /// nuovo movimento multi-tick anche nella seconda tratta, dopo il pickup.
        /// </para>
        /// </summary>
        [Test]
        public void DevTransportTemplatePreparesDeclaredRoutesWithoutMoveIntentFallback()
        {
            // Arrange: NPC a (1,1), oggetto a (3,1), destinazione a (5,1). Nessuna
            // route viene precaricata dal test: se il gancio dev non prepara le
            // tratte dichiarate, il Job fallira' o ricadra' nel vecchio command.
            var world = MakeWorldWithNpc(out int npcId);
            EnableOneCellTraversal(world, durationTicks: 1);
            world.ObjectDefs["qa_crate"] = new ObjectDef
            {
                Id = "qa_crate",
                DisplayName = "QA Crate",
                IsInteractable = true,
                BlocksMovement = false,
                BlocksVision = false
            };
            int objectId = world.CreateObject("qa_crate", 3, 1);
            var destination = new Vector2Int(5, 1);
            var job = MakeDevTransportJob(npcId, objectId, new Vector2Int(3, 1), destination);
            Assert.That(world.JobRuntimeState.TryAssignJob(npcId, job, tick: 0, out var reason), Is.True, reason);

            var system = new JobExecutionSystem();
            var bus = new MessageBus();

            // Act: avanziamo abbastanza tick da completare due tratte di due celle,
            // pickup e drop. Dopo ogni tick svuotiamo il command buffer, come farebbe
            // SimulationHost nel pump ordinario.
            for (int tick = 0; tick < 8 && world.JobRuntimeState.HasActiveJob(npcId); tick++)
            {
                system.Update(world, new Tick(tick, 1f), bus, new Telemetry());
                FlushJobCommandBuffer(world, bus);
            }

            // Assert: il job ha usato il template di trasporto come sequenza reale e
            // non ha prodotto alcun MoveIntent legacy.
            Assert.That(job.Status, Is.EqualTo(JobStatus.Completed));
            Assert.That(world.JobRuntimeState.HasActiveJob(npcId), Is.False);
            Assert.That(world.JobRuntimeState.CommandBuffer.Count, Is.EqualTo(0));
            Assert.That(world.NpcMoveIntents.TryGetValue(npcId, out var intent) && intent.Active, Is.False);
            Assert.That(world.GridPos[npcId].X, Is.EqualTo(destination.x));
            Assert.That(world.GridPos[npcId].Y, Is.EqualTo(destination.y));
            Assert.That(world.Objects[objectId].IsHeld, Is.False);
            Assert.That(world.Objects[objectId].CellX, Is.EqualTo(destination.x));
            Assert.That(world.Objects[objectId].CellY, Is.EqualTo(destination.y));
        }

        // =============================================================================
        // DevForcedMoveToCellUsesMoveToRunningActionRoute
        // =============================================================================
        /// <summary>
        /// <para>
        /// Verifica il comando umano forzato equivalente a K + mouse: il target puo'
        /// essere fuori dalla conoscenza soggettiva ordinaria, ma l'esecuzione non
        /// deve ricadere su <c>SetMoveIntentCommand</c>. Il movimento passa invece
        /// dal template <c>generic.move_to_cell.v1</c>, da <c>MoveTo</c> e dalle
        /// running action multi-tick.
        /// </para>
        /// </summary>
        [Test]
        public void DevForcedMoveToCellUsesMoveToRunningActionRoute()
        {
            var world = MakeWorldWithNpc(out int npcId);
            EnableOneCellTraversal(world, durationTicks: 1);
            var start = world.GridPos[npcId];
            var target = new Vector2Int(start.X, start.Y + 3);
            bool created = MoveJobFactory.TryCreateMoveToCellJob(
                JobTemplateRegistry.LoadDefault(),
                npcId,
                target,
                tick: 0,
                urgency01: 1f,
                debugLabel: MoveJobFactory.DevToolsForcedMoveToCellDebugLabel,
                out var job,
                out var reason);
            Assert.That(created, Is.True, reason);
            Assert.That(world.JobRuntimeState.TryAssignJob(npcId, job, tick: 0, out reason), Is.True, reason);

            var system = new JobExecutionSystem();
            var bus = new MessageBus();
            for (int tick = 0; tick < 8 && world.JobRuntimeState.HasActiveJob(npcId); tick++)
            {
                system.Update(world, new Tick(tick, 1f), bus, new Telemetry());
                FlushJobCommandBuffer(world, bus);
            }

            Assert.That(job.Status, Is.EqualTo(JobStatus.Completed));
            Assert.That(world.JobRuntimeState.CommandBuffer.Count, Is.EqualTo(0));
            Assert.That(world.NpcMoveIntents.TryGetValue(npcId, out var intent) && intent.Active, Is.False);
            Assert.That(world.GridPos[npcId].X, Is.EqualTo(target.x));
            Assert.That(world.GridPos[npcId].Y, Is.EqualTo(target.y));
        }

        private static World MakeWorldWithNpc(out int npcId)
        {
            return MakeWorldWithNpc(new SimulationParams(), out npcId);
        }

        private static World MakeWorldWithNpcWithRunningActionExplainability(out int npcId)
        {
            var sim = new SimulationParams();
            sim.memory_belief_decision_explainability.enabled = true;
            sim.memory_belief_decision_explainability.writeJsonLog = false;
            sim.memory_belief_decision_explainability.logRunningAction = true;
            return MakeWorldWithNpc(sim, out npcId);
        }

        private static World MakeWorldWithNpc(SimulationParams simulationParams, out int npcId)
        {
            // World minimale: abbastanza runtime per JobExecutionSystem, senza
            // avviare SimulationHost o MovementSystem.
            var world = new World(new WorldConfig(simulationParams ?? new SimulationParams()));
            world.Global.Needs = NeedsConfig.Default();
            world.Global.BeliefQuery = BeliefQueryConfig.Default();
            npcId = world.CreateNpc(
                NpcDnaProfile.CreateDefault("running_action_productive_qa"),
                NpcNeeds.Make(0.5f, 0.5f),
                new Arcontio.Core.Social { JusticePerception01 = 0.5f },
                x: 1,
                y: 1);
            world.NpcFacing[npcId] = CardinalDirection.East;
            return world;
        }

        private static void EnableOneCellTraversal(World world, int durationTicks)
        {
            // Gate produttivo del traversal 02g. Lasciarlo spento di default
            // preserva le vertical slice legacy che si aspettano SetMoveIntentCommand.
            world.Config.Sim.tick = new TickParams
            {
                ticksPerSecond = TickParams.DefaultTicksPerSecond,
                baseWalkCellDurationTicks = durationTicks,
                enableJobRunningActionTraversal = true
            };
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

        private static Job MakeObjectTargetMoveJob(int npcId, string jobId, int targetObjectId, Vector2Int targetCell)
        {
            // Questo helper costruisce un caso utile per 02h: il job possiede una
            // reservation oggetto, mentre la cella attraversata deve essere protetta
            // dal traversal running action. Non richiede che l'oggetto esista nel
            // World perche' il test non esegue consume/pickup/drop.
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

        private static Job MakeFoodJob(int npcId, int foodObjectId, Vector2Int foodCell)
        {
            bool created = FoodJobFactory.TryCreateKnownCommunityFoodJob(
                JobTemplateRegistry.LoadDefault(),
                npcId,
                foodObjectId,
                foodCell,
                tick: 0,
                urgency01: 0.95f,
                beliefKey: "FoodRouteQa",
                out var job,
                out var reason);

            Assert.That(created, Is.True, reason);
            return job;
        }

        private static int RegisterCommunityFoodStock(World world, int objectId, int x, int y, int units)
        {
            world.Objects[objectId] = new WorldObjectInstance
            {
                ObjectId = objectId,
                DefId = "food_stock",
                CellX = x,
                CellY = y,
                OwnerKind = OwnerKind.Community,
                OwnerId = 0
            };

            world.FoodStocks[objectId] = new FoodStockComponent
            {
                Units = units,
                OwnerKind = OwnerKind.Community,
                OwnerId = 0
            };

            return objectId;
        }

        private static Job MakeDevTransportJob(int npcId, int objectId, Vector2Int objectCell, Vector2Int destinationCell)
        {
            var request = new JobRequest(
                "req-dev-transport",
                npcId,
                DecisionIntentKind.ExploreArea,
                JobPriorityClass.Emergency,
                urgency01: 1f,
                createdTick: 0,
                hasTargetCell: true,
                targetCell: objectCell,
                targetObjectId: objectId,
                beliefKey: string.Empty,
                debugLabel: "DevToolsForcedTransportObject",
                hasSecondaryTargetCell: true,
                secondaryTargetCell: destinationCell);

            var phases = new[]
            {
                new JobPhase(
                    "reach_object",
                    JobPhaseKind.ReachTarget,
                    "Reach object",
                    expectedStepCount: 1,
                    isInterruptible: true,
                    actions: new[]
                    {
                        new JobAction("move_to_object", JobActionKind.MoveToCell, "move_to_object", true, objectCell, objectId, 0, string.Empty)
                    }),
                new JobPhase(
                    "pickup_object",
                    JobPhaseKind.Execute,
                    "Pick up object",
                    expectedStepCount: 1,
                    isInterruptible: false,
                    actions: new[]
                    {
                        new JobAction("pickup_object", JobActionKind.PickUp, "pickup_object", true, objectCell, objectId, 0, string.Empty)
                    }),
                new JobPhase(
                    "reach_destination",
                    JobPhaseKind.ReachTarget,
                    "Reach destination",
                    expectedStepCount: 1,
                    isInterruptible: true,
                    actions: new[]
                    {
                        new JobAction("move_to_destination", JobActionKind.MoveToCell, "move_to_destination", true, destinationCell, 0, 0, string.Empty)
                    }),
                new JobPhase(
                    "drop_object",
                    JobPhaseKind.Cleanup,
                    "Drop object",
                    expectedStepCount: 1,
                    isInterruptible: false,
                    actions: new[]
                    {
                        new JobAction("drop_object", JobActionKind.Drop, "drop_object", true, destinationCell, objectId, 0, string.Empty)
                    })
            };

            return new Job("job-dev-transport", request, new JobPlan(JobTemplateRegistry.TransportObjectToCellTemplateId, phases));
        }

        private static void FlushJobCommandBuffer(World world, MessageBus bus)
        {
            var snapshot = world.JobRuntimeState.CommandBuffer.Snapshot();
            for (int i = 0; i < snapshot.Length; i++)
                snapshot[i].Execute(world, bus);

            world.JobRuntimeState.CommandBuffer.Clear();
        }
    }
}
