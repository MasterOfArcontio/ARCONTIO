namespace Arcontio.Core
{
    // =============================================================================
    // NpcDecisionScheduler
    // =============================================================================
    /// <summary>
    /// <para>
    /// Gate minimale per stabilire se un NPC puo' entrare nella futura
    /// rivalutazione cognitiva del Decision Orchestrator.
    /// </para>
    ///
    /// <para><b>ARC-DEC-019 - Scheduler cognitivo separato da JobArbiter</b></para>
    /// <para>
    /// Questo componente non implementa preemption, non legge job mutabili e non
    /// decide se un job attivo debba essere sostituito. Distingue soltanto tra
    /// rivalutazioni ordinarie, rivalutazioni rinviate e segnali cognitivi che
    /// meritano di attraversare la pipeline decisionale futura anche durante un job
    /// attivo.
    /// </para>
    ///
    /// <para><b>Struttura interna:</b></para>
    /// <list type="bullet">
    ///   <item><b>Cadence gate</b>: limita la frequenza ordinaria della cognizione.</item>
    ///   <item><b>Active job gate</b>: rinvia decisioni routine senza bloccare emergenze.</item>
    ///   <item><b>Reason output</b>: produce motivi leggibili, non verdetti di arbitration.</item>
    /// </list>
    /// </summary>
    public sealed class NpcDecisionScheduler
    {
        // =============================================================================
        // EvaluateEligibility
        // =============================================================================
        /// <summary>
        /// <para>
        /// Valuta se il Decision Orchestrator puo' procedere con una rivalutazione
        /// cognitiva per l'NPC descritto dall'input.
        /// </para>
        ///
        /// <para><b>Job attivo non significa skip assoluto</b></para>
        /// <para>
        /// Se un NPC ha un job attivo, la rivalutazione routine viene rinviata, ma
        /// segnali gia' classificati come emergenza o possibile intenzione superiore
        /// restano eleggibili. Questo preserva il confine fissato da ARC-DEC-019:
        /// valutare cognitivamente non equivale a preemptare.
        /// </para>
        ///
        /// <para><b>Struttura interna:</b></para>
        /// <list type="bullet">
        ///   <item><b>Cadence</b>: prima barriera per evitare thrashing decisionale.</item>
        ///   <item><b>No active job</b>: consente la rivalutazione ordinaria.</item>
        ///   <item><b>Emergency</b>: consente una futura valutazione cognitiva ad alta priorita'.</item>
        ///   <item><b>Higher priority signal</b>: consente una futura valutazione non-routine.</item>
        ///   <item><b>Routine defer</b>: rinvia senza mutare stato runtime.</item>
        /// </list>
        /// </summary>
        public NpcDecisionEligibility EvaluateEligibility(in NpcDecisionSchedulerInput input)
        {
            if (!IsCadenceDue(input.Tick, input.LastDecisionTick, input.DecisionCadenceTicks))
            {
                return new NpcDecisionEligibility(
                    false,
                    NpcDecisionEligibilityReason.CadenceNotDue,
                    "CadenceNotDue");
            }

            if (!input.HasActiveJob)
            {
                return new NpcDecisionEligibility(
                    true,
                    NpcDecisionEligibilityReason.NoActiveJob,
                    "NoActiveJob");
            }

            if (input.HasEmergencyIntentSignal)
            {
                return new NpcDecisionEligibility(
                    true,
                    NpcDecisionEligibilityReason.ActiveJobMayEvaluateForEmergencyIntent,
                    "ActiveJobMayEvaluateForEmergencyIntent");
            }

            if (input.HasHigherPriorityIntentSignal)
            {
                return new NpcDecisionEligibility(
                    true,
                    NpcDecisionEligibilityReason.ActiveJobMayEvaluateForHigherPriorityIntent,
                    "ActiveJobMayEvaluateForHigherPriorityIntent");
            }

            return new NpcDecisionEligibility(
                false,
                NpcDecisionEligibilityReason.ActiveJobDefersRoutineDecision,
                "ActiveJobDefersRoutineDecision");
        }

        // =============================================================================
        // IsCadenceDue
        // =============================================================================
        /// <summary>
        /// <para>
        /// Determina se la cadenza cognitiva minima e' maturata.
        /// </para>
        ///
        /// <para><b>Cadence cognitiva, non world tick ridefinito</b></para>
        /// <para>
        /// Il tick resta quello canonico del runtime. Questo helper confronta solo
        /// due tick gia' forniti dal chiamante e non introduce un nuovo clock, una
        /// nuova coroutine o una schedulazione autonoma.
        /// </para>
        ///
        /// <para><b>Struttura interna:</b></para>
        /// <list type="bullet">
        ///   <item><b>Cadence non positiva</b>: normalizzata a valutazione sempre ammessa.</item>
        ///   <item><b>Mai valutato</b>: <c>LastDecisionTick</c> negativo permette il primo passaggio.</item>
        ///   <item><b>Delta</b>: confronta tick corrente e ultimo tick decisionale.</item>
        /// </list>
        /// </summary>
        private static bool IsCadenceDue(int tick, int lastDecisionTick, int decisionCadenceTicks)
        {
            if (decisionCadenceTicks <= 0)
                return true;

            if (lastDecisionTick < 0)
                return true;

            return tick - lastDecisionTick >= decisionCadenceTicks;
        }
    }
}
