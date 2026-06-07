using System;
using System.Linq;
using System.Reflection;
using Arcontio.Core;
using Arcontio.Core.Config;
using Arcontio.Core.Save;
using NUnit.Framework;

namespace Arcontio.Tests
{
    // =============================================================================
    // RunningActionStoreQaTests
    // =============================================================================
    /// <summary>
    /// <para>
    /// Test QA EditMode per lo storage produttivo volatile delle running action.
    /// </para>
    ///
    /// <para><b>Principio architetturale: store produttivo senza tick produttivo</b></para>
    /// <para>
    /// v0.11c.02d introduce il punto corretto in cui conservare
    /// <see cref="RunningActionRuntimeState"/> nel Job runtime, ma non trasforma
    /// ancora movement, traversal o executor in flusso produttivo. Questi test
    /// verificano che lo store sia posseduto da <see cref="JobRuntimeState"/>, che
    /// resti volatile, che venga pulito dai lifecycle job esistenti e che non entri
    /// in save/load, command emission o mutazioni del <c>World</c>.
    /// </para>
    ///
    /// <para><b>Struttura interna:</b></para>
    /// <list type="bullet">
    ///   <item><b>Store API</b>: register, get, update e clear mirati.</item>
    ///   <item><b>Lifecycle cleanup</b>: complete, fail/preempt, clear e reset transient.</item>
    ///   <item><b>Boundary</b>: nessun save/load, nessun command, nessuna mutazione World.</item>
    /// </list>
    /// </summary>
    public sealed class RunningActionStoreQaTests
    {
        // =============================================================================
        // RegisterAndTryGetStoresRunningAction
        // =============================================================================
        /// <summary>
        /// <para>
        /// Verifica il contratto minimo di start/register e lettura tramite chiave
        /// composta.
        /// </para>
        /// </summary>
        [Test]
        public void RegisterAndTryGetStoresRunningAction()
        {
            // Arrange: store isolato e key agganciata a NPC/job/action reali.
            var store = new RunningActionStore();
            var key = MakeKey(npcId: 1, jobId: "job-a", phaseIndex: 0, actionIndex: 0);
            var state = MakeState(npcId: 1, jobId: "job-a");

            // Act: registriamo e leggiamo senza executor produttivo.
            bool registered = store.Register(key, state, out var reason);
            bool found = store.TryGet(key, out var loaded);

            // Assert: lo store contiene solo progress runtime volatile.
            Assert.That(registered, Is.True, reason);
            Assert.That(found, Is.True);
            Assert.That(loaded, Is.SameAs(state));
            Assert.That(store.Count, Is.EqualTo(1));
        }

        // =============================================================================
        // UpdateProgressDoesNotMutateWorld
        // =============================================================================
        /// <summary>
        /// <para>
        /// Verifica che aggiornare progress interno nello store non produca command,
        /// job assignment o mutazioni oggettive sul World.
        /// </para>
        /// </summary>
        [Test]
        public void UpdateProgressDoesNotMutateWorld()
        {
            // Arrange: World sentinella e store produttivo sotto JobRuntimeState.
            var world = MakeWorldSentinel();
            var key = MakeKey(npcId: 1, jobId: "job-a", phaseIndex: 0, actionIndex: 0);
            var state = MakeState(npcId: 1, jobId: "job-a");
            Assert.That(world.JobRuntimeState.RunningActions.Register(key, state, out var registerReason), Is.True, registerReason);
            int commandCountBefore = world.JobRuntimeState.CommandBuffer.Count;
            int activeJobsBefore = world.JobRuntimeState.ActiveJobCount;

            // Act: il progress avanza sullo stato interno e viene confermato nello store.
            state.AdvanceProgress(2, tick: 4);
            bool updated = world.JobRuntimeState.RunningActions.Update(key, state, out var updateReason);

            // Assert: nessuna parte del World e del command buffer cambia.
            Assert.That(updated, Is.True, updateReason);
            Assert.That(world.JobRuntimeState.RunningActions.TryGet(key, out var loaded), Is.True);
            Assert.That(loaded.ElapsedTicks, Is.EqualTo(2));
            Assert.That(world.JobRuntimeState.CommandBuffer.Count, Is.EqualTo(commandCountBefore));
            Assert.That(world.JobRuntimeState.ActiveJobCount, Is.EqualTo(activeJobsBefore));
        }

        // =============================================================================
        // ClearByKeyNpcAndJobRemoveExpectedActions
        // =============================================================================
        /// <summary>
        /// <para>
        /// Verifica i tre cleanup mirati dello store senza coinvolgere il runtime
        /// produttivo.
        /// </para>
        /// </summary>
        [Test]
        public void ClearByKeyNpcAndJobRemoveExpectedActions()
        {
            // Arrange: tre action distinte per coprire key, NPC e job.
            var store = new RunningActionStore();
            var keyA = MakeKey(1, "job-a", 0, 0);
            var keyB = MakeKey(1, "job-b", 0, 1);
            var keyC = MakeKey(2, "job-c", 1, 0);
            Register(store, keyA);
            Register(store, keyB);
            Register(store, keyC);

            // Act/Assert: clear by key rimuove un solo record.
            Assert.That(store.Clear(keyA), Is.True);
            Assert.That(store.Count, Is.EqualTo(2));

            // Act/Assert: clear by NPC rimuove le action rimaste per quell'NPC.
            Assert.That(store.ClearByNpc(1), Is.EqualTo(1));
            Assert.That(store.Count, Is.EqualTo(1));

            // Act/Assert: clear by job rimuove l'action residua.
            Assert.That(store.ClearByJob("job-c"), Is.EqualTo(1));
            Assert.That(store.Count, Is.EqualTo(0));
        }

        // =============================================================================
        // CompleteJobCleansRunningActions
        // =============================================================================
        /// <summary>
        /// <para>
        /// Verifica che la chiusura positiva del job elimini il progress volatile
        /// associato al job.
        /// </para>
        /// </summary>
        [Test]
        public void CompleteJobCleansRunningActions()
        {
            // Arrange: job assegnato e running action agganciata allo stesso job.
            var runtime = new JobRuntimeState();
            var job = MakeJob("job-complete", JobPriorityClass.Normal, urgency01: 0.2f);
            Assert.That(runtime.TryAssignJob(1, job, tick: 0, out var assignReason), Is.True, assignReason);
            Register(runtime.RunningActions, MakeKey(1, job.JobId, 0, 0));

            // Act: il job termina dal path pubblico esistente.
            bool completed = runtime.CompleteCurrentJob(1, tick: 5, out var reason);

            // Assert: job e progress volatile sono entrambi puliti.
            Assert.That(completed, Is.True, reason);
            Assert.That(runtime.HasActiveJob(1), Is.False);
            Assert.That(runtime.RunningActions.Count, Is.EqualTo(0));
        }

        // =============================================================================
        // FailAndPreemptCleanRunningActions
        // =============================================================================
        /// <summary>
        /// <para>
        /// Verifica sia il fallimento diretto sia la preemption tramite path di fail
        /// esistente: in entrambi i casi lo store non conserva progress del job
        /// chiuso.
        /// </para>
        /// </summary>
        [Test]
        public void FailAndPreemptCleanRunningActions()
        {
            // Arrange: primo runtime per failure diretto.
            var failedRuntime = new JobRuntimeState();
            var failedJob = MakeJob("job-fail", JobPriorityClass.Normal, urgency01: 0.2f);
            Assert.That(failedRuntime.TryAssignJob(1, failedJob, 0, out var failAssignReason), Is.True, failAssignReason);
            Register(failedRuntime.RunningActions, MakeKey(1, failedJob.JobId, 0, 0));

            // Act/Assert: failure pulisce lo store.
            Assert.That(failedRuntime.FailCurrentJob(1, JobFailureReason.StepFailed, 2, out var failReason), Is.True, failReason);
            Assert.That(failedRuntime.RunningActions.Count, Is.EqualTo(0));

            // Arrange: secondo runtime per preemption via JobArbiter/FailCurrentJob.
            var preemptRuntime = new JobRuntimeState();
            var current = MakeJob("job-current", JobPriorityClass.Normal, urgency01: 0.1f);
            var emergency = MakeJob("job-emergency", JobPriorityClass.Emergency, urgency01: 1f);
            Assert.That(preemptRuntime.TryAssignJob(1, current, 0, out var firstAssignReason), Is.True, firstAssignReason);
            Register(preemptRuntime.RunningActions, MakeKey(1, current.JobId, 0, 0));

            // Act: assegnare un job emergency usa il path di preemption gia' esistente.
            Assert.That(preemptRuntime.TryAssignJob(1, emergency, 1, out var preemptReason), Is.True, preemptReason);

            // Assert: il nuovo job resta attivo, ma il progress del job preemptato e' sparito.
            Assert.That(preemptRuntime.HasActiveJob(1), Is.True);
            Assert.That(preemptRuntime.RunningActions.Count, Is.EqualTo(0));
            Assert.That(preemptRuntime.GetSnapshot(1, 1).CurrentJobId, Is.EqualTo(emergency.JobId));
        }

        // =============================================================================
        // ClearNpcJobAndTransientJobsCleanStore
        // =============================================================================
        /// <summary>
        /// <para>
        /// Verifica i cleanup espliciti usati da reset, load e cancellazioni runtime.
        /// </para>
        /// </summary>
        [Test]
        public void ClearNpcJobAndTransientJobsCleanStore()
        {
            // Arrange: ClearNpcJob deve rimuovere anche progress orfani dello stesso NPC.
            var npcRuntime = new JobRuntimeState();
            var job = MakeJob("job-clear-npc", JobPriorityClass.Normal, urgency01: 0.2f);
            Assert.That(npcRuntime.TryAssignJob(1, job, 0, out var assignReason), Is.True, assignReason);
            Register(npcRuntime.RunningActions, MakeKey(1, job.JobId, 0, 0));

            // Act/Assert: clear per NPC pulisce il job e lo store.
            Assert.That(npcRuntime.ClearNpcJob(1, JobFailureReason.Cancelled, out var clearReason), Is.True, clearReason);
            Assert.That(npcRuntime.RunningActions.Count, Is.EqualTo(0));

            // Arrange: ClearTransientJobs deve svuotare tutto lo stato volatile job.
            var transientRuntime = new JobRuntimeState();
            var transientJob = MakeJob("job-transient", JobPriorityClass.Normal, urgency01: 0.2f);
            Assert.That(transientRuntime.TryAssignJob(1, transientJob, 0, out var transientAssignReason), Is.True, transientAssignReason);
            Register(transientRuntime.RunningActions, MakeKey(1, transientJob.JobId, 0, 0));

            // Act/Assert: il reset transitorio elimina anche le running action.
            transientRuntime.ClearTransientJobs();
            Assert.That(transientRuntime.ActiveJobCount, Is.EqualTo(0));
            Assert.That(transientRuntime.RunningActions.Count, Is.EqualTo(0));
        }

        // =============================================================================
        // ActiveMovementLookupUsesTypedMetadataAndClearsIndex
        // =============================================================================
        /// <summary>
        /// <para>
        /// Verifica la lookup read-only CPU-leggera usata da ArcGraph per recuperare
        /// il segmento di movimento attivo di un NPC.
        /// </para>
        /// </summary>
        [Test]
        public void ActiveMovementLookupUsesTypedMetadataAndClearsIndex()
        {
            // Arrange: lo store contiene una movement action con metadata tipizzato.
            var store = new RunningActionStore();
            var key = MakeKey(3, "job-move", 0, 0);
            var state = MakeMovementState(
                npcId: 3,
                jobId: "job-move",
                fromCellX: 4,
                fromCellY: 5,
                toCellX: 5,
                toCellY: 5);
            Assert.That(store.Register(key, state, out var registerReason), Is.True, registerReason);

            // Act: ArcGraph potra' usare questa API senza allocare GetSnapshots.
            bool foundBeforeClear = store.TryGetActiveMovementSnapshotForNpc(3, out var snapshot);
            bool cleared = store.Clear(key);
            bool foundAfterClear = store.TryGetActiveMovementSnapshotForNpc(3, out _);

            // Assert: l'indice restituisce il segmento corretto e si pulisce con la key.
            Assert.That(foundBeforeClear, Is.True);
            Assert.That(snapshot.Movement.IsValidStep, Is.True);
            Assert.That(snapshot.Movement.FromCellX, Is.EqualTo(4));
            Assert.That(snapshot.Movement.FromCellY, Is.EqualTo(5));
            Assert.That(snapshot.Movement.ToCellX, Is.EqualTo(5));
            Assert.That(snapshot.Movement.ToCellY, Is.EqualTo(5));
            Assert.That(cleared, Is.True);
            Assert.That(foundAfterClear, Is.False);
        }

        // =============================================================================
        // RunningActionStoreIsNotPartOfSaveLoadContracts
        // =============================================================================
        /// <summary>
        /// <para>
        /// Verifica che lo store e lo stato running action non siano esposti dai DTO
        /// del namespace save/load.
        /// </para>
        /// </summary>
        [Test]
        public void RunningActionStoreIsNotPartOfSaveLoadContracts()
        {
            // Arrange: analizziamo solo campi/proprieta' dichiarati nei tipi save,
            // perche' ARC-DEC-020 vieta la persistenza del progress volatile.
            var saveTypes = typeof(WorldSaveData).Assembly
                .GetTypes()
                .Where(type => type.Namespace == "Arcontio.Core.Save")
                .ToArray();

            // Act: cerchiamo riferimenti diretti/generici allo store o al runtime state.
            bool saveReferencesRunningAction = saveTypes.Any(type =>
                ReferencesType(type, typeof(RunningActionStore))
                || ReferencesType(type, typeof(RunningActionRuntimeState))
                || ReferencesType(type, typeof(RunningActionProgressSnapshot)));

            // Assert: save/load resta fuori dal progress volatile.
            Assert.That(saveReferencesRunningAction, Is.False);
        }

        private static void Register(RunningActionStore store, RunningActionKey key)
        {
            // Helper deliberatamente piccolo: ogni stato usa identita' coerente con
            // la key cosi' i test non nascondono mismatch di NPC/job.
            var state = MakeState(key.NpcId, key.JobId);
            Assert.That(store.Register(key, state, out var reason), Is.True, reason);
        }

        private static RunningActionKey MakeKey(int npcId, string jobId, int phaseIndex, int actionIndex)
        {
            return new RunningActionKey(npcId, jobId, phaseIndex, actionIndex);
        }

        private static RunningActionRuntimeState MakeState(int npcId, string jobId)
        {
            // Stato locale senza World, executor produttivo o command buffer.
            var policy = new RunningActionCompletionPolicy(
                requiredTicks: 4,
                timeoutTicks: 10,
                failureReason: JobFailureReason.MovementFailed,
                interruptionReason: JobFailureReason.Preempted);

            return RunningActionRuntimeState.Start(
                actionInstanceId: "run-" + jobId,
                kind: RunningActionKind.Movement,
                npcId: npcId,
                jobId: jobId,
                phaseId: "phase-0",
                jobActionId: "action-0",
                startedTick: 0,
                completionPolicy: policy);
        }

        private static RunningActionRuntimeState MakeMovementState(
            int npcId,
            string jobId,
            int fromCellX,
            int fromCellY,
            int toCellX,
            int toCellY)
        {
            // Variante con metadata di movimento reale: resta locale allo store e
            // non autorizza nessuna mutazione World o completamento job.
            var policy = new RunningActionCompletionPolicy(
                requiredTicks: 4,
                timeoutTicks: 10,
                failureReason: JobFailureReason.MovementFailed,
                interruptionReason: JobFailureReason.Preempted);

            return RunningActionRuntimeState.StartMovement(
                actionInstanceId: "move-" + jobId,
                npcId: npcId,
                jobId: jobId,
                phaseId: "phase-0",
                jobActionId: "action-0",
                startedTick: 0,
                completionPolicy: policy,
                fromCellX: fromCellX,
                fromCellY: fromCellY,
                toCellX: toCellX,
                toCellY: toCellY);
        }

        private static Job MakeJob(string jobId, JobPriorityClass priorityClass, float urgency01)
        {
            // Job no-target per evitare reservation e movement: i test devono coprire
            // solo ownership/cleanup dello store, non il runtime di esecuzione reale.
            var request = JobRequest.WithoutTarget(
                "req-" + jobId,
                npcId: 1,
                DecisionIntentKind.WaitAndObserve,
                priorityClass,
                urgency01,
                createdTick: 0,
                debugLabel: jobId);
            var phase = new JobPhase(
                "phase-0",
                JobPhaseKind.Execute,
                "QA phase",
                expectedStepCount: 1,
                isInterruptible: true,
                actions: new[] { JobAction.Wait("action-0", durationTicks: 1, label: "QA wait") });
            var plan = new JobPlan("plan-" + jobId, new[] { phase });
            return new Job(jobId, request, plan);
        }

        private static World MakeWorldSentinel()
        {
            // Sentinella di non-mutazione: lo store viene letto tramite
            // JobRuntimeState ma nessun sistema produttivo viene avviato.
            return new World(new WorldConfig(new SimulationParams()));
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
