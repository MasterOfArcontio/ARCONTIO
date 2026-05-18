using Arcontio.Core;
using Arcontio.Core.Config;
using NUnit.Framework;

namespace Arcontio.Tests
{
    // =============================================================================
    // DecisionOrchestratorNoOpQaTests
    // =============================================================================
    /// <summary>
    /// <para>
    /// Test QA EditMode per lo skeleton no-op del futuro Decision Orchestrator
    /// introdotto in v0.11c.01a.
    /// </para>
    ///
    /// <para><b>ARC-DEC-019 - Rivalutazione cognitiva separata da preemption</b></para>
    /// <para>
    /// Questi test proteggono il confine architetturale della patch: lo skeleton
    /// puo' valutare soltanto eleggibilita' cognitiva, ma non puo' produrre command,
    /// assegnare job, mutare il <c>World</c> o decidere preemption. Il test non
    /// cabla il componente nel runtime produttivo e non passa da <c>NeedsDecisionRule</c>.
    /// </para>
    ///
    /// <para><b>Struttura interna:</b></para>
    /// <list type="bullet">
    ///   <item><b>No-op orchestration</b>: risultato passivo senza command o job proposal.</item>
    ///   <item><b>World immutato</b>: un World creato dal test resta invariato perche' non viene passato allo skeleton.</item>
    ///   <item><b>Scheduler</b>: active job rinvia routine ma consente reason per valutazione futura superiore/emergenza.</item>
    /// </list>
    /// </summary>
    public sealed class DecisionOrchestratorNoOpQaTests
    {
        // =============================================================================
        // DecisionOrchestratorSystemDoesNotProduceICommand
        // =============================================================================
        /// <summary>
        /// <para>
        /// Verifica che il risultato dello skeleton non sia un command e non segnali
        /// alcuna proposta esecutiva.
        /// </para>
        ///
        /// <para><b>Decision Layer non emette ICommand</b></para>
        /// <para>
        /// Il test resta volutamente semplice: lo skeleton restituisce un DTO di
        /// orchestration, non un <c>ICommand</c>, e i flag produttivi restano spenti.
        /// </para>
        ///
        /// <para><b>Struttura interna:</b></para>
        /// <list type="bullet">
        ///   <item><b>Input</b>: NPC senza job attivo e cadence matura.</item>
        ///   <item><b>Act</b>: chiamata no-op.</item>
        ///   <item><b>Assert</b>: nessun command, nessun intent selezionato, nessun JobRequest proposto.</item>
        /// </list>
        /// </summary>
        [Test]
        public void DecisionOrchestratorSystemDoesNotProduceICommand()
        {
            // Arrange: lo skeleton e' usato fuori dal runtime produttivo.
            var orchestrator = new DecisionOrchestratorSystem();
            var input = MakeInput(hasActiveJob: false);

            // Act: la valutazione no-op produce solo un result passivo.
            var result = orchestrator.EvaluateNoOp(input);

            // Assert: nessun ICommand puo' uscire dal Decision Layer skeleton.
            Assert.That((object)result, Is.Not.AssignableTo<ICommand>());
            Assert.That(result.PipelineInvoked, Is.False);
            Assert.That(result.HasSelectedIntent, Is.False);
            Assert.That(result.HasJobRequestProposal, Is.False);
        }

        // =============================================================================
        // DecisionOrchestratorSystemDoesNotAssignJob
        // =============================================================================
        /// <summary>
        /// <para>
        /// Verifica che la valutazione no-op non assegni job e non produca nemmeno
        /// una proposta di job.
        /// </para>
        ///
        /// <para><b>Job Layer non attraversato</b></para>
        /// <para>
        /// Il test non crea job, non chiama <c>JobRuntimeState.TryAssignJob</c> e
        /// controlla solo il contratto pubblico del risultato. In v0.11c.01a il
        /// futuro boundary <c>IntentExecutionRouter</c> non esiste ancora.
        /// </para>
        ///
        /// <para><b>Struttura interna:</b></para>
        /// <list type="bullet">
        ///   <item><b>Eligibility true</b>: la cadence consente valutazione cognitiva.</item>
        ///   <item><b>No-op</b>: l'orchestrator non invoca la pipeline reale.</item>
        ///   <item><b>Assert</b>: nessuna proposta JobRequest.</item>
        /// </list>
        /// </summary>
        [Test]
        public void DecisionOrchestratorSystemDoesNotAssignJob()
        {
            // Arrange: anche con eligibility positiva, lo skeleton resta passivo.
            var orchestrator = new DecisionOrchestratorSystem();
            var input = MakeInput(hasActiveJob: false);

            // Act: nessun Job Layer viene raggiunto.
            var result = orchestrator.EvaluateNoOp(input);

            // Assert: il result non contiene assegnazione o proposta job.
            Assert.That(result.Eligibility.AllowsEvaluation, Is.True);
            Assert.That(result.HasJobRequestProposal, Is.False);
            Assert.That(result.PipelineInvoked, Is.False);
        }

        // =============================================================================
        // DecisionOrchestratorSystemDoesNotModifyWorld
        // =============================================================================
        /// <summary>
        /// <para>
        /// Verifica che un <c>World</c> esistente resti invariato durante la chiamata
        /// allo skeleton.
        /// </para>
        ///
        /// <para><b>World non e' input cognitivo dello skeleton</b></para>
        /// <para>
        /// Il test crea un World solo come sentinella di non-mutazione. Il World non
        /// viene passato al Decision Orchestrator, proprio per proteggere il confine
        /// anti-telepatia e behavior-preserving della patch.
        /// </para>
        ///
        /// <para><b>Struttura interna:</b></para>
        /// <list type="bullet">
        ///   <item><b>Snapshot</b>: conta NPC, oggetti e job attivi prima della chiamata.</item>
        ///   <item><b>No-op</b>: valuta un input puramente dichiarativo.</item>
        ///   <item><b>Assert</b>: le conte restano identiche.</item>
        /// </list>
        /// </summary>
        [Test]
        public void DecisionOrchestratorSystemDoesNotModifyWorld()
        {
            // Arrange: World sentinella, non sorgente cognitiva.
            var world = new World(new WorldConfig(new SimulationParams()));
            int npcCountBefore = world.NpcDna.Count;
            int objectCountBefore = world.Objects.Count;
            int activeJobCountBefore = world.JobRuntimeState.ActiveJobCount;
            var orchestrator = new DecisionOrchestratorSystem();

            // Act: lo skeleton non riceve World e non ha canali di mutazione.
            _ = orchestrator.EvaluateNoOp(MakeInput(hasActiveJob: false));

            // Assert: nessun effetto collaterale sullo stato oggettivo.
            Assert.That(world.NpcDna.Count, Is.EqualTo(npcCountBefore));
            Assert.That(world.Objects.Count, Is.EqualTo(objectCountBefore));
            Assert.That(world.JobRuntimeState.ActiveJobCount, Is.EqualTo(activeJobCountBefore));
        }

        // =============================================================================
        // NpcDecisionSchedulerSeparatesCognitiveEligibilityFromPreemption
        // =============================================================================
        /// <summary>
        /// <para>
        /// Verifica che lo scheduler distingua eleggibilita' cognitiva da decisione
        /// di preemption.
        /// </para>
        ///
        /// <para><b>Reason non autoritativa</b></para>
        /// <para>
        /// Con job attivo e nessun segnale superiore, lo scheduler rinvia la routine.
        /// Con segnale superiore, permette la futura rivalutazione cognitiva, ma la
        /// reason non assegna job e non rappresenta una decisione del <c>JobArbiter</c>.
        /// </para>
        ///
        /// <para><b>Struttura interna:</b></para>
        /// <list type="bullet">
        ///   <item><b>Routine</b>: ActiveJobDefersRoutineDecision.</item>
        ///   <item><b>Higher priority</b>: ActiveJobMayEvaluateForHigherPriorityIntent.</item>
        ///   <item><b>Assert</b>: nessuna reason usa linguaggio di assignment/preemption completata.</item>
        /// </list>
        /// </summary>
        [Test]
        public void NpcDecisionSchedulerSeparatesCognitiveEligibilityFromPreemption()
        {
            // Arrange: scheduler puro, senza JobRuntimeState o JobArbiter.
            var scheduler = new NpcDecisionScheduler();
            var routine = MakeInput(hasActiveJob: true);
            var higher = MakeInput(hasActiveJob: true, hasHigherPriorityIntentSignal: true);

            // Act: confrontiamo routine defer e valutazione futura superiore.
            var routineEligibility = scheduler.EvaluateEligibility(routine);
            var higherEligibility = scheduler.EvaluateEligibility(higher);

            // Assert: il primo rinvia, il secondo consente solo cognizione futura.
            Assert.That(routineEligibility.AllowsEvaluation, Is.False);
            Assert.That(routineEligibility.Reason, Is.EqualTo(NpcDecisionEligibilityReason.ActiveJobDefersRoutineDecision));
            Assert.That(higherEligibility.AllowsEvaluation, Is.True);
            Assert.That(higherEligibility.Reason, Is.EqualTo(NpcDecisionEligibilityReason.ActiveJobMayEvaluateForHigherPriorityIntent));
            Assert.That(higherEligibility.DiagnosticLabel, Does.Not.Contain("Assigned"));
            Assert.That(higherEligibility.DiagnosticLabel, Does.Not.Contain("Preempted"));
        }

        // =============================================================================
        // ActiveJobDoesNotMeanAbsoluteDecisionSkip
        // =============================================================================
        /// <summary>
        /// <para>
        /// Verifica che un job attivo non equivalga a skip decisionale assoluto.
        /// </para>
        ///
        /// <para><b>ARC-DEC-019 applicata allo scheduler</b></para>
        /// <para>
        /// La presenza di job attivo rinvia la routine, ma segnali di emergenza o di
        /// possibile intenzione superiore restano eleggibili per futura valutazione.
        /// Questo non decide preemption: stabilisce solo che la cognizione non e'
        /// bloccata in modo totale.
        /// </para>
        ///
        /// <para><b>Struttura interna:</b></para>
        /// <list type="bullet">
        ///   <item><b>Emergency</b>: reason dedicata per futura valutazione emergenza.</item>
        ///   <item><b>Higher priority</b>: reason dedicata per futura valutazione superiore.</item>
        ///   <item><b>No job proposal</b>: orchestrator resta no-op anche quando eligibility e' true.</item>
        /// </list>
        /// </summary>
        [Test]
        public void ActiveJobDoesNotMeanAbsoluteDecisionSkip()
        {
            // Arrange: due segnali non-routine durante job attivo.
            var scheduler = new NpcDecisionScheduler();
            var orchestrator = new DecisionOrchestratorSystem(scheduler);
            var emergency = MakeInput(hasActiveJob: true, hasEmergencyIntentSignal: true);
            var higher = MakeInput(hasActiveJob: true, hasHigherPriorityIntentSignal: true);

            // Act: lo scheduler consente valutazione futura, l'orchestrator resta no-op.
            var emergencyEligibility = scheduler.EvaluateEligibility(emergency);
            var higherResult = orchestrator.EvaluateNoOp(higher);

            // Assert: esistono reason compatibili con valutazione futura, senza job transition.
            Assert.That(emergencyEligibility.AllowsEvaluation, Is.True);
            Assert.That(emergencyEligibility.Reason, Is.EqualTo(NpcDecisionEligibilityReason.ActiveJobMayEvaluateForEmergencyIntent));
            Assert.That(higherResult.Eligibility.AllowsEvaluation, Is.True);
            Assert.That(higherResult.Eligibility.Reason, Is.EqualTo(NpcDecisionEligibilityReason.ActiveJobMayEvaluateForHigherPriorityIntent));
            Assert.That(higherResult.HasJobRequestProposal, Is.False);
            Assert.That(higherResult.PipelineInvoked, Is.False);
        }

        // =============================================================================
        // MakeInput
        // =============================================================================
        /// <summary>
        /// <para>
        /// Costruisce input dichiarativi per lo scheduler decisionale.
        /// </para>
        ///
        /// <para><b>Factory senza World e senza Job Layer</b></para>
        /// <para>
        /// La factory non crea job, non crea command, non legge store di mondo e non
        /// produce target cognitivi. I flag rappresentano segnali gia' classificati
        /// da futuri componenti, non calcoli effettuati dal test.
        /// </para>
        ///
        /// <para><b>Struttura interna:</b></para>
        /// <list type="bullet">
        ///   <item><b>Tick</b>: cadence sempre matura per isolare il gate active-job.</item>
        ///   <item><b>ActiveJob</b>: flag dichiarativo, non stato JobRuntime mutabile.</item>
        ///   <item><b>Signals</b>: higher priority/emergency come input gia' normalizzati.</item>
        /// </list>
        /// </summary>
        private static NpcDecisionSchedulerInput MakeInput(
            bool hasActiveJob,
            bool hasHigherPriorityIntentSignal = false,
            bool hasEmergencyIntentSignal = false)
        {
            return new NpcDecisionSchedulerInput(
                npcId: 1,
                tick: 20,
                lastDecisionTick: 10,
                decisionCadenceTicks: 5,
                hasActiveJob: hasActiveJob,
                hasHigherPriorityIntentSignal: hasHigherPriorityIntentSignal,
                hasEmergencyIntentSignal: hasEmergencyIntentSignal);
        }
    }
}
