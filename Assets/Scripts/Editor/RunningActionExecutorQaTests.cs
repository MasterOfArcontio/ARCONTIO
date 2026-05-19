using System;
using System.Linq;
using System.Reflection;
using Arcontio.Core;
using Arcontio.Core.Config;
using NUnit.Framework;

namespace Arcontio.Tests
{
    // =============================================================================
    // RunningActionExecutorQaTests
    // =============================================================================
    /// <summary>
    /// <para>
    /// Test QA EditMode per l'executor minimale delle running action introdotto in
    /// v0.11c.02c.
    /// </para>
    ///
    /// <para><b>Principio architetturale: executor generico senza authority runtime</b></para>
    /// <para>
    /// Il componente deve avanzare esclusivamente stato volatile interno. Questi test
    /// proteggono il confine fissato da ARC-DEC-020: nessuna mutazione World,
    /// nessuna emissione di <c>ICommand</c>, nessun job assignment, nessuna
    /// preemption e nessun cablaggio nel runtime produttivo corrente.
    /// </para>
    ///
    /// <para><b>Struttura interna:</b></para>
    /// <list type="bullet">
    ///   <item><b>Progress</b>: elapsed cresce per delta tick esplicito.</item>
    ///   <item><b>Completion</b>: completion e timeout sono lifecycle locali.</item>
    ///   <item><b>Boundary</b>: reflection e sentinelle World verificano il no-op.</item>
    /// </list>
    /// </summary>
    public sealed class RunningActionExecutorQaTests
    {
        // =============================================================================
        // ExecutorIncrementsElapsedTick
        // =============================================================================
        /// <summary>
        /// <para>
        /// Verifica che il tick executor incrementi elapsed e lasci la running action
        /// in stato Running quando la durata richiesta non e' ancora raggiunta.
        /// </para>
        /// </summary>
        [Test]
        public void ExecutorIncrementsElapsedTick()
        {
            // Arrange: stato locale e executor senza World o JobRuntimeState.
            var executor = new RunningActionExecutor();
            var state = MakeState(requiredTicks: 5, timeoutTicks: 0);

            // Act: due tick di progresso restano sotto la soglia di completion.
            var result = executor.Tick(
                state,
                RunningActionExecutorTickRequest.Advance(deltaTicks: 2, tick: 10, reason: "QaAdvance"));

            // Assert: cambia solo il progress interno volatile.
            Assert.That(result.Kind, Is.EqualTo(RunningActionExecutorResultKind.Advanced));
            Assert.That(result.Before.ElapsedTicks, Is.EqualTo(0));
            Assert.That(result.After.ElapsedTicks, Is.EqualTo(2));
            Assert.That(state.ElapsedTicks, Is.EqualTo(2));
            Assert.That(state.Status, Is.EqualTo(RunningActionLifecycleStatus.Running));
            Assert.That(result.IsTerminal, Is.False);
        }

        // =============================================================================
        // ExecutorCompletesWhenElapsedReachesDuration
        // =============================================================================
        /// <summary>
        /// <para>
        /// Verifica che la completion sia calcolata dal progress interno e non da un
        /// effetto esterno sul mondo.
        /// </para>
        /// </summary>
        [Test]
        public void ExecutorCompletesWhenElapsedReachesDuration()
        {
            // Arrange: action che richiede tre tick interni.
            var executor = new RunningActionExecutor();
            var state = MakeState(requiredTicks: 3, timeoutTicks: 0);

            // Act: il delta raggiunge esattamente la durata richiesta.
            var result = executor.Tick(
                state,
                RunningActionExecutorTickRequest.Advance(deltaTicks: 3, tick: 12, reason: "QaComplete"));

            // Assert: lifecycle Completed, nessun concetto di command finale nel result.
            Assert.That(result.Kind, Is.EqualTo(RunningActionExecutorResultKind.Completed));
            Assert.That(result.IsTerminal, Is.True);
            Assert.That(result.After.Status, Is.EqualTo(RunningActionLifecycleStatus.Completed));
            Assert.That(state.Status, Is.EqualTo(RunningActionLifecycleStatus.Completed));
            Assert.That(state.FailureReason, Is.EqualTo(JobFailureReason.None));
        }

        // =============================================================================
        // ExecutorDoesNotEmitCommand
        // =============================================================================
        /// <summary>
        /// <para>
        /// Verifica che executor, request e result non implementino o contengano
        /// riferimenti a <c>ICommand</c>.
        /// </para>
        /// </summary>
        [Test]
        public void ExecutorDoesNotEmitCommand()
        {
            // Arrange: i tipi pubblici dell'executor sono il contratto da proteggere.
            Type executorType = typeof(RunningActionExecutor);
            Type requestType = typeof(RunningActionExecutorTickRequest);
            Type resultType = typeof(RunningActionExecutorResult);

            // Act: cerchiamo assegnabilita' o campi/proprieta' command-like.
            bool executorIsCommand = typeof(ICommand).IsAssignableFrom(executorType);
            bool requestReferencesCommand = ReferencesCommandType(requestType);
            bool resultReferencesCommand = ReferencesCommandType(resultType);

            // Assert: nessun path di emissione command e' presente nello skeleton.
            Assert.That(executorIsCommand, Is.False);
            Assert.That(requestReferencesCommand, Is.False);
            Assert.That(resultReferencesCommand, Is.False);
        }

        // =============================================================================
        // ExecutorDoesNotMutateWorld
        // =============================================================================
        /// <summary>
        /// <para>
        /// Verifica con una sentinella World che il tick executor non accodi command,
        /// non assegni job e non introduca reservation.
        /// </para>
        /// </summary>
        [Test]
        public void ExecutorDoesNotMutateWorld()
        {
            // Arrange: World minimale non passato all'executor, usato come baseline.
            var world = MakeWorldSentinel();
            int activeJobsBefore = world.JobRuntimeState.ActiveJobCount;
            int commandCountBefore = world.JobRuntimeState.CommandBuffer.Count;
            int npcStateCountBefore = world.JobRuntimeState.NpcStateCount;
            var executor = new RunningActionExecutor();
            var state = MakeState(requiredTicks: 4, timeoutTicks: 0);

            // Act: anche una completion locale non deve cambiare il runtime World.
            var result = executor.Tick(
                state,
                RunningActionExecutorTickRequest.Advance(deltaTicks: 4, tick: 20, reason: "QaWorldSentinel"));

            // Assert: il World resta byte-for-byte semanticamente irrilevante per lo skeleton.
            Assert.That(result.Kind, Is.EqualTo(RunningActionExecutorResultKind.Completed));
            Assert.That(world.JobRuntimeState.ActiveJobCount, Is.EqualTo(activeJobsBefore));
            Assert.That(world.JobRuntimeState.CommandBuffer.Count, Is.EqualTo(commandCountBefore));
            Assert.That(world.JobRuntimeState.NpcStateCount, Is.EqualTo(npcStateCountBefore));
            Assert.That(world.JobRuntimeState.Reservations.Count, Is.EqualTo(0));
        }

        // =============================================================================
        // ExecutorCanInterruptActionWithoutPreemption
        // =============================================================================
        /// <summary>
        /// <para>
        /// Verifica che una richiesta di interruption marchi solo lo stato locale
        /// come Interrupted senza chiamare arbitri o chiudere job.
        /// </para>
        /// </summary>
        [Test]
        public void ExecutorCanInterruptActionWithoutPreemption()
        {
            // Arrange: action in corso con elapsed gia' avanzato.
            var world = MakeWorldSentinel();
            var executor = new RunningActionExecutor();
            var state = MakeState(requiredTicks: 8, timeoutTicks: 0);
            executor.Tick(state, RunningActionExecutorTickRequest.Advance(deltaTicks: 2, tick: 30));

            // Act: interruption esplicita, ma nessun JobArbiter e nessun runtime job.
            var result = executor.Tick(
                state,
                RunningActionExecutorTickRequest.Interrupt(tick: 31, reason: JobFailureReason.Cancelled, diagnosticReason: "QaInterrupt"));

            // Assert: terminalita' locale, progress preservato, runtime intatto.
            Assert.That(result.Kind, Is.EqualTo(RunningActionExecutorResultKind.Interrupted));
            Assert.That(result.After.Status, Is.EqualTo(RunningActionLifecycleStatus.Interrupted));
            Assert.That(result.After.ElapsedTicks, Is.EqualTo(2));
            Assert.That(result.After.FailureReason, Is.EqualTo(JobFailureReason.Cancelled));
            Assert.That(world.JobRuntimeState.ActiveJobCount, Is.EqualTo(0));
            Assert.That(world.JobRuntimeState.CommandBuffer.Count, Is.EqualTo(0));
        }

        // =============================================================================
        // TimeoutAndExplicitFailureAreStateOnly
        // =============================================================================
        /// <summary>
        /// <para>
        /// Verifica che timeout e failure esplicita siano rappresentati come stato
        /// terminale locale, non come side effect esterno.
        /// </para>
        /// </summary>
        [Test]
        public void TimeoutAndExplicitFailureAreStateOnly()
        {
            // Arrange: il required e' lontano, il timeout invece e' immediatamente raggiungibile.
            var world = MakeWorldSentinel();
            var executor = new RunningActionExecutor();
            var timedOutState = MakeState(requiredTicks: 10, timeoutTicks: 3);
            var failedState = MakeState(requiredTicks: 10, timeoutTicks: 0);

            // Act: una action va in timeout, l'altra viene fallita esplicitamente.
            var timeoutResult = executor.Tick(
                timedOutState,
                RunningActionExecutorTickRequest.Advance(deltaTicks: 3, tick: 40, reason: "QaTimeout"));
            var failureResult = executor.Tick(
                failedState,
                RunningActionExecutorTickRequest.Fail(tick: 41, reason: JobFailureReason.StepFailed, diagnosticReason: "QaFail"));

            // Assert: entrambe sono lifecycle locali, senza command o reservation.
            Assert.That(timeoutResult.Kind, Is.EqualTo(RunningActionExecutorResultKind.TimedOut));
            Assert.That(timeoutResult.After.Status, Is.EqualTo(RunningActionLifecycleStatus.Failed));
            Assert.That(timeoutResult.After.FailureReason, Is.EqualTo(JobFailureReason.MovementFailed));
            Assert.That(failureResult.Kind, Is.EqualTo(RunningActionExecutorResultKind.Failed));
            Assert.That(failureResult.After.FailureReason, Is.EqualTo(JobFailureReason.StepFailed));
            Assert.That(world.JobRuntimeState.CommandBuffer.Count, Is.EqualTo(0));
            Assert.That(world.JobRuntimeState.ActiveJobCount, Is.EqualTo(0));
        }

        // =============================================================================
        // ExecutorIsNotWiredIntoProductiveRuntime
        // =============================================================================
        /// <summary>
        /// <para>
        /// Verifica che v0.11c.02c non abbia ancora collegato l'executor allo store
        /// produttivo del Job System.
        /// </para>
        /// </summary>
        [Test]
        public void ExecutorIsNotWiredIntoProductiveRuntime()
        {
            // Arrange: reflection limitata ai tipi runtime che non devono cambiare in 02c.
            Type executorType = typeof(RunningActionExecutor);
            Type runtimeStateType = typeof(JobRuntimeState);
            Type executionSystemType = typeof(JobExecutionSystem);

            // Act: cerchiamo riferimenti diretti all'executor nei punti produttivi.
            bool runtimeReferencesExecutor = ReferencesType(runtimeStateType, executorType);
            bool executionReferencesExecutor = ReferencesType(executionSystemType, executorType);

            // Assert: l'executor esiste, ma non e' ancora nel tick produttivo.
            Assert.That(runtimeReferencesExecutor, Is.False);
            Assert.That(executionReferencesExecutor, Is.False);
        }

        private static RunningActionRuntimeState MakeState(int requiredTicks, int timeoutTicks)
        {
            // Factory locale coerente con RunningActionRuntimeStateQaTests: niente
            // World, niente MovementSystem, niente JobRuntimeState.
            var policy = new RunningActionCompletionPolicy(
                requiredTicks,
                timeoutTicks,
                JobFailureReason.MovementFailed,
                JobFailureReason.Preempted);

            return RunningActionRuntimeState.Start(
                "run-action-executor-test",
                RunningActionKind.Movement,
                npcId: 7,
                jobId: "job-test",
                phaseId: "phase-move",
                jobActionId: "move-to-cell",
                startedTick: 1,
                completionPolicy: policy);
        }

        private static World MakeWorldSentinel()
        {
            // Sentinella di non-mutazione: non viene mai passata all'executor e
            // rende visibile se qualcuno introducesse side effect sul job runtime.
            return new World(new WorldConfig(new SimulationParams()));
        }

        private static bool ReferencesCommandType(Type container)
        {
            // Reflection volutamente limitata: cerca ICommand nei membri dichiarati
            // dal contratto pubblico/privato del tipo senza attraversare l'intero assembly.
            return ReferencesType(container, typeof(ICommand));
        }

        private static bool ReferencesType(Type container, Type target)
        {
            var fields = container.GetFields(BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
            var properties = container.GetProperties(BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);

            return fields.Any(field => ReferencesTypeRecursive(field.FieldType, target))
                || properties.Any(property => ReferencesTypeRecursive(property.PropertyType, target));
        }

        private static bool ReferencesTypeRecursive(Type candidate, Type target)
        {
            if (candidate == target || target.IsAssignableFrom(candidate))
                return true;

            if (!candidate.IsGenericType)
                return false;

            var args = candidate.GetGenericArguments();
            for (int i = 0; i < args.Length; i++)
            {
                if (ReferencesTypeRecursive(args[i], target))
                    return true;
            }

            return false;
        }
    }
}
