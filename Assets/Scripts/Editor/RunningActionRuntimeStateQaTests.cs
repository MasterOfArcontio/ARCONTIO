using System;
using System.Linq;
using System.Reflection;
using Arcontio.Core;
using Arcontio.Core.Config;
using NUnit.Framework;

namespace Arcontio.Tests
{
    // =============================================================================
    // RunningActionRuntimeStateQaTests
    // =============================================================================
    /// <summary>
    /// <para>
    /// Test QA EditMode per lo skeleton passivo delle future running action
    /// multi-tick.
    /// </para>
    ///
    /// <para><b>Principio architetturale: skeleton no-op, nessun runtime wiring</b></para>
    /// <para>
    /// v0.11c.02b introduce solo vocabolario e stato interno volatile. Questi test
    /// proteggono il confine: il tipo puo' contare progress, rappresentare
    /// completion/failure/interruption e produrre snapshot, ma non deve mutare World,
    /// emettere <c>ICommand</c>, assegnare job o cablarsi in <c>JobRuntimeState</c>.
    /// </para>
    ///
    /// <para><b>Struttura interna:</b></para>
    /// <list type="bullet">
    ///   <item><b>Lifecycle</b>: Started, Running, Completed, Interrupted.</item>
    ///   <item><b>Progress</b>: elapsed volatile e completion calcolabile.</item>
    ///   <item><b>Boundary</b>: nessun command, nessun job assignment, nessun wiring produttivo.</item>
    /// </list>
    /// </summary>
    public sealed class RunningActionRuntimeStateQaTests
    {
        // =============================================================================
        // NewRunningActionStartsWithElapsedZero
        // =============================================================================
        /// <summary>
        /// <para>
        /// Verifica che una running action appena creata parta da elapsed zero e da
        /// lifecycle Started.
        /// </para>
        /// </summary>
        [Test]
        public void NewRunningActionStartsWithElapsedZero()
        {
            // Arrange/Act: creiamo lo stato senza World, MovementSystem o JobRuntimeState.
            var state = MakeState(requiredTicks: 3, timeoutTicks: 10);
            var snapshot = state.ToSnapshot();

            // Assert: la action esiste solo come progresso interno volatile.
            Assert.That(state.ElapsedTicks, Is.EqualTo(0));
            Assert.That(state.Status, Is.EqualTo(RunningActionLifecycleStatus.Started));
            Assert.That(state.IsTerminal, Is.False);
            Assert.That(state.CanComplete, Is.False);
            Assert.That(snapshot.ElapsedTicks, Is.EqualTo(0));
            Assert.That(snapshot.Status, Is.EqualTo(RunningActionLifecycleStatus.Started));
        }

        // =============================================================================
        // ProgressIncrementsOnlyInternalState
        // =============================================================================
        /// <summary>
        /// <para>
        /// Verifica che l'avanzamento aumenti solo elapsed e non tocchi buffer job,
        /// job attivi o altri stati runtime esterni.
        /// </para>
        /// </summary>
        [Test]
        public void ProgressIncrementsOnlyInternalState()
        {
            // Arrange: World minimale usato solo come sentinella di non mutazione.
            var world = MakeWorldSentinel();
            int activeJobsBefore = world.JobRuntimeState.ActiveJobCount;
            int commandCountBefore = world.JobRuntimeState.CommandBuffer.Count;
            var state = MakeState(requiredTicks: 4, timeoutTicks: 0);

            // Act: due avanzamenti restano confinati nel tipo skeleton.
            bool first = state.AdvanceProgress(1, tick: 11);
            bool second = state.AdvanceProgress(2, tick: 12);
            var snapshot = state.ToSnapshot();

            // Assert: solo elapsed/status cambiano; World/Job buffer restano intatti.
            Assert.That(first, Is.True);
            Assert.That(second, Is.True);
            Assert.That(snapshot.ElapsedTicks, Is.EqualTo(3));
            Assert.That(snapshot.Status, Is.EqualTo(RunningActionLifecycleStatus.Running));
            Assert.That(world.JobRuntimeState.ActiveJobCount, Is.EqualTo(activeJobsBefore));
            Assert.That(world.JobRuntimeState.CommandBuffer.Count, Is.EqualTo(commandCountBefore));
        }

        // =============================================================================
        // CompletionIsCalculableWithoutWorldMutation
        // =============================================================================
        /// <summary>
        /// <para>
        /// Verifica che la completion sia derivabile dal progresso interno e non
        /// produca mutazioni o command finali.
        /// </para>
        /// </summary>
        [Test]
        public void CompletionIsCalculableWithoutWorldMutation()
        {
            // Arrange: requiredTicks descrive il completamento interno minimo.
            var world = MakeWorldSentinel();
            var state = MakeState(requiredTicks: 2, timeoutTicks: 0);

            // Act: prima non basta, poi l'action diventa completabile e viene chiusa.
            state.AdvanceProgress(1, tick: 2);
            bool completedTooEarly = state.TryMarkCompleted(tick: 2);
            state.AdvanceProgress(1, tick: 3);
            bool completed = state.TryMarkCompleted(tick: 3);

            // Assert: completion e' solo lifecycle interno; nessun ICommand viene accodato.
            Assert.That(completedTooEarly, Is.False);
            Assert.That(completed, Is.True);
            Assert.That(state.Status, Is.EqualTo(RunningActionLifecycleStatus.Completed));
            Assert.That(state.FailureReason, Is.EqualTo(JobFailureReason.None));
            Assert.That(world.JobRuntimeState.CommandBuffer.Count, Is.EqualTo(0));
            Assert.That(world.JobRuntimeState.ActiveJobCount, Is.EqualTo(0));
        }

        // =============================================================================
        // InterruptionMarksInterruptedWithoutCommand
        // =============================================================================
        /// <summary>
        /// <para>
        /// Verifica che una interruption produca solo stato Interrupted e reason
        /// diagnostica, senza emettere command o simulare preemption.
        /// </para>
        /// </summary>
        [Test]
        public void InterruptionMarksInterruptedWithoutCommand()
        {
            // Arrange: action in progresso e buffer job vuoto come sentinella.
            var world = MakeWorldSentinel();
            var state = MakeState(requiredTicks: 5, timeoutTicks: 10);
            state.AdvanceProgress(2, tick: 4);

            // Act: interrompiamo con reason esplicita, senza JobArbiter.
            bool interrupted = state.Interrupt(JobFailureReason.Preempted, tick: 5);
            bool progressedAfterTerminal = state.AdvanceProgress(1, tick: 6);

            // Assert: stato terminale locale, nessun command finale.
            Assert.That(interrupted, Is.True);
            Assert.That(progressedAfterTerminal, Is.False);
            Assert.That(state.Status, Is.EqualTo(RunningActionLifecycleStatus.Interrupted));
            Assert.That(state.FailureReason, Is.EqualTo(JobFailureReason.Preempted));
            Assert.That(state.ElapsedTicks, Is.EqualTo(2));
            Assert.That(world.JobRuntimeState.CommandBuffer.Count, Is.EqualTo(0));
            Assert.That(world.JobRuntimeState.ActiveJobCount, Is.EqualTo(0));
        }

        // =============================================================================
        // TimeoutAndFailureReasonAreRepresentable
        // =============================================================================
        /// <summary>
        /// <para>
        /// Verifica che timeout e failure reason siano rappresentabili senza
        /// introdurre policy runtime o accessi al mondo.
        /// </para>
        /// </summary>
        [Test]
        public void TimeoutAndFailureReasonAreRepresentable()
        {
            // Arrange: policy con timeout breve e failure reason normalizzata.
            var state = MakeState(requiredTicks: 10, timeoutTicks: 3);

            // Act: il progresso supera il timeout e poi viene marcato failed.
            state.AdvanceProgress(3, tick: 8);
            bool failed = state.MarkFailed(JobFailureReason.MovementFailed, tick: 8);
            var snapshot = state.ToSnapshot();

            // Assert: timeout e reason sono dati leggibili, non side effect.
            Assert.That(snapshot.IsTimedOut, Is.True);
            Assert.That(failed, Is.True);
            Assert.That(snapshot.Status, Is.EqualTo(RunningActionLifecycleStatus.Failed));
            Assert.That(snapshot.FailureReason, Is.EqualTo(JobFailureReason.MovementFailed));
            Assert.That(snapshot.IsTerminal, Is.True);
        }

        // =============================================================================
        // RuntimeStateDoesNotStoreRunningActionDirectly
        // =============================================================================
        /// <summary>
        /// <para>
        /// Verifica il confine dopo v0.11c.02d: il nuovo tipo non e' un command e
        /// non viene conservato direttamente da <c>JobRuntimeState</c>. Lo storage
        /// produttivo autorizzato passa dallo <c>RunningActionStore</c> dedicato.
        /// </para>
        /// </summary>
        [Test]
        public void RuntimeStateDoesNotStoreRunningActionDirectly()
        {
            // Arrange: reflection limitata al contratto pubblico/privato del runtime job.
            var runningActionType = typeof(RunningActionRuntimeState);
            var runtimeStateType = typeof(JobRuntimeState);
            var fields = runtimeStateType.GetFields(BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
            var properties = runtimeStateType.GetProperties(BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);

            // Act: cerchiamo cablaggi diretti del nuovo skeleton nello store produttivo.
            // v0.11c.02d consente invece la proprieta' RunningActions, che mantiene
            // separato il progress volatile dal cursore job.
            bool commandAssignable = typeof(ICommand).IsAssignableFrom(runningActionType);
            bool hasFieldWiring = fields.Any(field => ReferencesRunningActionType(field.FieldType, runningActionType));
            bool hasPropertyWiring = properties.Any(property => ReferencesRunningActionType(property.PropertyType, runningActionType));

            // Assert: lo skeleton resta passivo e non viene annidato direttamente
            // dentro JobRuntimeState.
            Assert.That(commandAssignable, Is.False);
            Assert.That(hasFieldWiring, Is.False);
            Assert.That(hasPropertyWiring, Is.False);
        }

        private static RunningActionRuntimeState MakeState(int requiredTicks, int timeoutTicks)
        {
            // Factory locale: nessun World, nessun job assignment, nessun command buffer.
            var policy = new RunningActionCompletionPolicy(
                requiredTicks,
                timeoutTicks,
                JobFailureReason.MovementFailed,
                JobFailureReason.Preempted);

            return RunningActionRuntimeState.Start(
                "run-action-test",
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
            // Il World qui e' solo una sentinella di non-mutazione: non seediamo NPC,
            // non assegniamo job e non attiviamo systems produttivi.
            return new World(new WorldConfig(new SimulationParams()));
        }

        private static bool ReferencesRunningActionType(Type candidate, Type runningActionType)
        {
            // Reflection volutamente semplice: copre riferimento diretto e generici
            // comuni senza trasformare il test in un parser dell'intero assembly.
            if (candidate == runningActionType)
                return true;

            if (!candidate.IsGenericType)
                return false;

            var args = candidate.GetGenericArguments();
            for (int i = 0; i < args.Length; i++)
            {
                if (args[i] == runningActionType)
                    return true;
            }

            return false;
        }
    }
}
