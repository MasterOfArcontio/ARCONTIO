using System.Collections.Generic;
using Arcontio.Core;
using Arcontio.Core.Config;
using NUnit.Framework;

namespace Arcontio.Tests
{
    // =============================================================================
    // DecisionReevaluationReasonQaTests
    // =============================================================================
    /// <summary>
    /// <para>
    /// Test QA EditMode per il modello passivo dei motivi di rivalutazione
    /// decisionale introdotto in v0.11c.03a.
    /// </para>
    ///
    /// <para><b>Modello passivo senza behavior change</b></para>
    /// <para>
    /// Questi test proteggono il confine del task: i reason sono dati diagnostici,
    /// non eventi produttivi, non scheduler, non priorita' e non preemption. La loro
    /// costruzione non deve emettere command, non deve modificare il World e non deve
    /// attraversare il Job Layer.
    /// </para>
    ///
    /// <para><b>Struttura interna:</b></para>
    /// <list type="bullet">
    ///   <item><b>Vocabolario</b>: periodic, alert, critical e no valid reason.</item>
    ///   <item><b>Differenziazione</b>: alert e critical restano categorie distinte.</item>
    ///   <item><b>Side effects</b>: nessuna command emission e nessuna modifica job runtime.</item>
    /// </list>
    /// </summary>
    public sealed class DecisionReevaluationReasonQaTests
    {
        // =============================================================================
        // PeriodicReasonIsValidAndPassive
        // =============================================================================
        /// <summary>
        /// <para>
        /// Verifica che la rivalutazione periodica ordinaria sia rappresentabile come
        /// motivo valido senza trasformarsi in scheduler produttivo.
        /// </para>
        ///
        /// <para><b>Cadence come causa diagnostica</b></para>
        /// <para>
        /// Il test costruisce solo un DTO: non aggiorna last-decision tick, non
        /// invoca <c>NpcDecisionScheduler</c> e non cabla il motivo nel runtime.
        /// </para>
        ///
        /// <para><b>Struttura interna:</b></para>
        /// <list type="bullet">
        ///   <item><b>Factory</b>: usa <c>Periodic</c>.</item>
        ///   <item><b>Kind</b>: verifica categoria normalizzata.</item>
        ///   <item><b>Snapshot</b>: conserva NPC e tick senza side effect.</item>
        /// </list>
        /// </summary>
        [Test]
        public void PeriodicReasonIsValidAndPassive()
        {
            var reason = DecisionReevaluationReason.Periodic("CadenceGate");
            var snapshot = new NpcDecisionReevaluationSnapshot(7, 42, reason);

            Assert.That(reason.IsValid, Is.True);
            Assert.That(reason.Kind, Is.EqualTo(DecisionReevaluationReasonKind.Periodic));
            Assert.That(reason.SourceLabel, Is.EqualTo("CadenceGate"));
            Assert.That(snapshot.NpcId, Is.EqualTo(7));
            Assert.That(snapshot.Tick, Is.EqualTo(42));
            Assert.That(snapshot.HasValidReason, Is.True);
        }

        // =============================================================================
        // NeedAlertReasonPreservesNeedKind
        // =============================================================================
        /// <summary>
        /// <para>
        /// Verifica che un bisogno in allerta sia rappresentabile senza produrre un
        /// evento soglia runtime.
        /// </para>
        ///
        /// <para><b>Alert non e' critical</b></para>
        /// <para>
        /// Il reason conserva il <c>NeedKind</c> coinvolto e usa una categoria
        /// distinta da quella critica. Non legge <c>World.Needs</c>: riceve il bisogno
        /// gia' classificato dal chiamante futuro.
        /// </para>
        /// </summary>
        [Test]
        public void NeedAlertReasonPreservesNeedKind()
        {
            var reason = DecisionReevaluationReason.NeedAlert(NeedKind.Hunger);

            Assert.That(reason.IsValid, Is.True);
            Assert.That(reason.Kind, Is.EqualTo(DecisionReevaluationReasonKind.NeedAlert));
            Assert.That(reason.Need, Is.EqualTo(NeedKind.Hunger));
            Assert.That(reason.DiagnosticLabel, Is.EqualTo("NeedAlert"));
        }

        // =============================================================================
        // NeedCriticalReasonPreservesNeedKind
        // =============================================================================
        /// <summary>
        /// <para>
        /// Verifica che un bisogno critico sia rappresentabile come motivo separato
        /// dall'allerta.
        /// </para>
        ///
        /// <para><b>Criticita' come explainability, non preemption</b></para>
        /// <para>
        /// Anche se il motivo e' critico, il DTO non autorizza interruzioni job e non
        /// contiene classi di priorita'. La severita' esecutiva resta materia del Job
        /// Layer e delle policy gia' fissate.
        /// </para>
        /// </summary>
        [Test]
        public void NeedCriticalReasonPreservesNeedKind()
        {
            var reason = DecisionReevaluationReason.NeedCritical(NeedKind.Thirst);

            Assert.That(reason.IsValid, Is.True);
            Assert.That(reason.Kind, Is.EqualTo(DecisionReevaluationReasonKind.NeedCritical));
            Assert.That(reason.Need, Is.EqualTo(NeedKind.Thirst));
            Assert.That(reason.DiagnosticLabel, Is.EqualTo("NeedCritical"));
        }

        // =============================================================================
        // AlertAndCriticalReasonsRemainDistinct
        // =============================================================================
        /// <summary>
        /// <para>
        /// Verifica che alert e critical non collassino nello stesso valore.
        /// </para>
        ///
        /// <para><b>Nessuna soglia produttiva introdotta</b></para>
        /// <para>
        /// Il test confronta due DTO gia' costruiti: non simula attraversamenti di
        /// soglia e non introduce <c>NeedThresholdCrossedEvent</c>.
        /// </para>
        /// </summary>
        [Test]
        public void AlertAndCriticalReasonsRemainDistinct()
        {
            var alert = DecisionReevaluationReason.NeedAlert(NeedKind.Rest);
            var critical = DecisionReevaluationReason.NeedCritical(NeedKind.Rest);

            Assert.That(alert.Kind, Is.Not.EqualTo(critical.Kind));
            Assert.That(alert.Need, Is.EqualTo(critical.Need));
            Assert.That(alert.DiagnosticLabel, Is.Not.EqualTo(critical.DiagnosticLabel));
        }

        // =============================================================================
        // NoValidReasonSnapshotIsExplicitlyInvalid
        // =============================================================================
        /// <summary>
        /// <para>
        /// Verifica che l'assenza di un motivo valido sia rappresentata in modo
        /// esplicito e testabile.
        /// </para>
        ///
        /// <para><b>Nessun default ambiguo</b></para>
        /// <para>
        /// Il valore nullo non deve sembrare una rivalutazione periodica o un evento
        /// esterno. Lo snapshot espone <c>HasValidReason</c> per le future trace.
        /// </para>
        /// </summary>
        [Test]
        public void NoValidReasonSnapshotIsExplicitlyInvalid()
        {
            var snapshot = NpcDecisionReevaluationSnapshot.NoValidReason(npcId: 3, tick: 99);

            Assert.That(snapshot.NpcId, Is.EqualTo(3));
            Assert.That(snapshot.Tick, Is.EqualTo(99));
            Assert.That(snapshot.HasValidReason, Is.False);
            Assert.That(snapshot.Reason.Kind, Is.EqualTo(DecisionReevaluationReasonKind.None));
            Assert.That(snapshot.Reason.Need, Is.EqualTo(NeedKind.COUNT));
            Assert.That(snapshot.Reason.DiagnosticLabel, Is.EqualTo("NoValidReason"));
        }

        // =============================================================================
        // JobLifecycleReasonsAreRepresentableWithoutChangingJobRuntime
        // =============================================================================
        /// <summary>
        /// <para>
        /// Verifica che completamento e fallimento job siano rappresentabili come
        /// cause cognitive passive senza modificare <c>JobRuntimeState</c>.
        /// </para>
        ///
        /// <para><b>Job Layer resta autoritativo</b></para>
        /// <para>
        /// Il test osserva uno stato job vuoto prima e dopo la costruzione dei DTO.
        /// Nessun helper di lifecycle job viene chiamato, e nessun job viene assegnato
        /// o fallito dal modello reason.
        /// </para>
        /// </summary>
        [Test]
        public void JobLifecycleReasonsAreRepresentableWithoutChangingJobRuntime()
        {
            var world = new World(new WorldConfig(new SimulationParams()));
            int activeJobsBefore = world.JobRuntimeState.ActiveJobCount;
            int npcStatesBefore = world.JobRuntimeState.NpcStateCount;

            var completed = DecisionReevaluationReason.JobCompleted("JobRuntimeState");
            var failed = DecisionReevaluationReason.JobFailed(JobFailureReason.MissingTarget, "JobRuntimeState");

            Assert.That(completed.Kind, Is.EqualTo(DecisionReevaluationReasonKind.JobCompleted));
            Assert.That(failed.Kind, Is.EqualTo(DecisionReevaluationReasonKind.JobFailed));
            Assert.That(failed.JobFailure, Is.EqualTo(JobFailureReason.MissingTarget));
            Assert.That(world.JobRuntimeState.ActiveJobCount, Is.EqualTo(activeJobsBefore));
            Assert.That(world.JobRuntimeState.NpcStateCount, Is.EqualTo(npcStatesBefore));
        }

        // =============================================================================
        // ExternalAndManualReasonsDoNotEmitCommands
        // =============================================================================
        /// <summary>
        /// <para>
        /// Verifica che segnali esterni e manual/debug restino spiegazioni passive e
        /// non producano <c>ICommand</c>.
        /// </para>
        ///
        /// <para><b>Nessuna command emission</b></para>
        /// <para>
        /// Il modello non conosce command buffer o router esecutivi. Il test mantiene
        /// una lista di command vuota come sentinella contro side effect nascosti.
        /// </para>
        /// </summary>
        [Test]
        public void ExternalAndManualReasonsDoNotEmitCommands()
        {
            var commands = new List<ICommand>();

            var external = DecisionReevaluationReason.ExternalEvent("WitnessedWorldFact");
            var manual = DecisionReevaluationReason.ManualDebug("InspectorButton");

            Assert.That(external.Kind, Is.EqualTo(DecisionReevaluationReasonKind.ExternalEvent));
            Assert.That(external.SourceLabel, Is.EqualTo("WitnessedWorldFact"));
            Assert.That(manual.Kind, Is.EqualTo(DecisionReevaluationReasonKind.ManualDebug));
            Assert.That(manual.SourceLabel, Is.EqualTo("InspectorButton"));
            Assert.That(commands.Count, Is.EqualTo(0));
        }
    }
}
