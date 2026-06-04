using Arcontio.Core.Config;
using Arcontio.Core.Diagnostics;
using Arcontio.Core.Logging;
using System.Collections.Generic;
using UnityEngine;

namespace Arcontio.Core
{
    // =============================================================================
    // DecisionExplainabilityBridge
    // =============================================================================
    /// <summary>
    /// <para>
    /// Centralizza l'emissione diagnostica del bridge decisionale legacy senza
    /// assumere responsabilita' decisionali o runtime.
    /// </para>
    ///
    /// <para><b>v0.11c.01d - Explainability come osservazione read-only</b></para>
    /// <para>
    /// Questo componente riceve dati gia' prodotti da <c>NeedsDecisionRule</c> e li
    /// copia in trace <c>MemoryBeliefDecision</c>. Non genera candidati, non calcola
    /// score, non seleziona intent, non produce <c>JobRequest</c>, non emette
    /// <c>ICommand</c>, non assegna job e non decide preemption. La sua unica
    /// responsabilita' e' rendere osservabile la catena decisionale corrente.
    /// </para>
    ///
    /// <para><b>Legacy Transitional Decision Bridge</b></para>
    /// <para>
    /// I record Bridge e Fallback qui emessi raccontano ancora un debito transitorio:
    /// alcune intenzioni passano da fallback legacy o da command adapter storici. Il
    /// componente non normalizza quel debito e non lo migra nell'Orchestrator; lo
    /// isola come superficie diagnostica verificabile.
    /// </para>
    ///
    /// <para><b>Struttura interna:</b></para>
    /// <list type="bullet">
    ///   <item><b>Decision trace</b>: snapshot di candidati score-ati e selection.</item>
    ///   <item><b>Bridge trace</b>: snapshot intent -> command/fallback/handled.</item>
    ///   <item><b>JobRequest trace</b>: snapshot del boundary dati Decision -> JobRequest.</item>
    ///   <item><b>Fallback log</b>: log strutturato solo per fallback/no-op classificati.</item>
    /// </list>
    /// </summary>
    public sealed class DecisionExplainabilityBridge
    {
        // =============================================================================
        // TryEmitJobRequestTrace
        // =============================================================================
        /// <summary>
        /// <para>
        /// Esporta il boundary <c>Decision -> JobRequest</c> usando la richiesta dati
        /// gia' costruita dal router esecutivo.
        /// </para>
        /// </summary>
        public void TryEmitJobRequestTrace(
            MemoryBeliefDecisionExplainabilityParams config,
            MemoryBeliefDecisionExplainabilityRegistry registry,
            int tick,
            int npcId,
            JobRequest request,
            string jobId,
            bool legacyBridgeStillUsed)
        {
            if (!MemoryBeliefDecisionExplainabilityEmitter.ShouldWriteTrace(
                    config,
                    MemoryBeliefDecisionTraceKind.JobRequest))
                return;

            MemoryBeliefDecisionExplainabilityEmitter.TryWriteJobRequestTrace(
                config,
                registry,
                npcId,
                tick,
                request,
                jobId,
                "DecisionCandidateProjectedToExecutableJobRequest",
                legacyBridgeStillUsed);
        }

        // =============================================================================
        // TryEmitDecisionTrace
        // =============================================================================
        /// <summary>
        /// <para>
        /// Esporta uno snapshot EL della selezione decisionale appena calcolata.
        /// </para>
        ///
        /// <para><b>Snapshot senza ricalcolo</b></para>
        /// <para>
        /// Il metodo non ricalcola candidati, score o weighted selection. Copia solo
        /// i dati ricevuti dalla pipeline decisionale reale, cosi' il log resta
        /// diagnostico e non diventa un secondo decision system.
        /// </para>
        /// </summary>
        public void TryEmitDecisionTrace(
            MemoryBeliefDecisionExplainabilityParams config,
            MemoryBeliefDecisionExplainabilityRegistry registry,
            in DecisionEvaluationContext context,
            bool auditValid,
            List<DecisionCandidate> candidates,
            DecisionSelectionResult selection,
            DecisionSelectionConfig selectionConfig)
        {
            if (!MemoryBeliefDecisionExplainabilityEmitter.ShouldWriteTrace(
                    config,
                    MemoryBeliefDecisionTraceKind.Decision))
                return;

            // L'effettivo rumore di selezione e' calcolato nello stesso modo del
            // SelectionService, ma senza rieseguire la roulette o riordinare candidati.
            float impulsivity01 = context.Dna != null ? context.Dna.CognitiveModulators.Impulsivity01 : 0f;
            float effectiveNoise01 = Clamp01(selectionConfig.noise01 + (impulsivity01 * selectionConfig.impulsivityNoiseBonus));

            var trace = new MemoryBeliefDecisionTrace
            {
                Kind = MemoryBeliefDecisionTraceKind.Decision,
                Tick = context.Tick,
                NpcId = context.NpcId,
                Decision = new MemoryBeliefDecisionDecisionRecord
                {
                    AuditValid = auditValid,
                    CandidateCount = candidates != null ? candidates.Count : 0,
                    SelectedIntent = selection.IsEmpty ? DecisionIntentKind.None : selection.Candidate.Kind,
                    SelectedScore = selection.IsEmpty ? 0f : selection.Candidate.FinalScore,
                    SelectedIndex = selection.SelectedIndex,
                    SelectionTopN = selectionConfig.topN,
                    SelectionNoise01 = selectionConfig.noise01,
                    Impulsivity01 = impulsivity01,
                    EffectiveNoise01 = effectiveNoise01,
                    Candidates = ToDecisionCandidateRecords(candidates),
                },
            };

            MemoryBeliefDecisionExplainabilityEmitter.TryWriteTrace(config, registry, trace);
        }

        // =============================================================================
        // TryEmitBridgeTrace
        // =============================================================================
        /// <summary>
        /// <para>
        /// Esporta il record EL del ponte provvisorio tra intenzione decisionale e
        /// route esecutiva o command legacy.
        /// </para>
        /// </summary>
        public void TryEmitBridgeTrace(
            MemoryBeliefDecisionExplainabilityParams config,
            MemoryBeliefDecisionExplainabilityRegistry registry,
            int tick,
            int npcId,
            DecisionCandidate candidate,
            ICommand command,
            bool didSteal,
            bool didMove,
            bool handled,
            bool legacyFallbackUsed,
            LegacyFallbackKind fallbackKind,
            string reason)
        {
            if (!MemoryBeliefDecisionExplainabilityEmitter.ShouldWriteTrace(
                    config,
                    MemoryBeliefDecisionTraceKind.Bridge))
            {
                TryLogDecisionBridgeFallback(tick, npcId, candidate, command, fallbackKind, reason);
                return;
            }

            var targetCell = Vector2Int.zero;
            var targetSource = MemoryBeliefDecisionTargetSource.None;
            if (candidate.Metadata.RequiresBeliefTarget && !candidate.BeliefResult.IsEmpty)
            {
                // Il bridge non cerca un nuovo target: copia quello gia' scelto dal
                // QuerySystem per il candidato decisionale.
                targetCell = candidate.BeliefResult.Belief.EstimatedPosition;
                targetSource = MemoryBeliefDecisionTargetSource.BeliefQuery;
            }
            else if (command != null)
            {
                targetSource = MemoryBeliefDecisionTargetSource.LegacyFallback;
            }

            MemoryBeliefDecisionExplainabilityEmitter.TryWriteTrace(config, registry, new MemoryBeliefDecisionTrace
            {
                Kind = MemoryBeliefDecisionTraceKind.Bridge,
                Tick = tick,
                NpcId = npcId,
                Bridge = new MemoryBeliefDecisionBridgeRecord
                {
                    SelectedIntent = candidate.Kind,
                    CommandName = command != null ? command.Name : string.Empty,
                    Handled = handled,
                    DidMove = didMove,
                    DidSteal = didSteal,
                    TargetCell = targetCell,
                    TargetSource = targetSource,
                    LegacyFallbackUsed = legacyFallbackUsed,
                    FallbackKind = fallbackKind,
                    Reason = reason ?? string.Empty,
                },
            });

            TryLogDecisionBridgeFallback(tick, npcId, candidate, command, fallbackKind, reason);
        }

        // =============================================================================
        // TryLogDecisionBridgeFallback
        // =============================================================================
        /// <summary>
        /// <para>
        /// Scrive un log strutturato solo quando il bridge incontra un fallback o un
        /// no-op classificato.
        /// </para>
        /// </summary>
        public void TryLogDecisionBridgeFallback(
            int tick,
            int npcId,
            DecisionCandidate candidate,
            ICommand command,
            LegacyFallbackKind fallbackKind,
            string reason)
        {
            TryLogDecisionBridgeFallback(tick, npcId, candidate.Kind, command, fallbackKind, reason);
        }

        public void TryLogDecisionBridgeFallback(
            int tick,
            int npcId,
            DecisionIntentKind intent,
            ICommand command,
            LegacyFallbackKind fallbackKind,
            string reason)
        {
            if (fallbackKind == LegacyFallbackKind.None)
                return;

            ArcontioLogger.Info(
                new LogContext(tick: tick, channel: "DecisionBridgeFallback", npcId: npcId),
                new LogBlock(LogLevel.Info, "log.decision.bridge.fallback")
                    .AddField("tick", tick)
                    .AddField("npcId", npcId)
                    .AddField("intent", intent.ToString())
                    .AddField("fallbackKind", fallbackKind.ToString())
                    .AddField("reason", reason ?? string.Empty)
                    .AddField("commandName", command != null ? command.Name : string.Empty));
        }

        // =============================================================================
        // ToDecisionCandidateRecords
        // =============================================================================
        /// <summary>
        /// <para>
        /// Copia i candidati decisionali in record EL serializzabili.
        /// </para>
        /// </summary>
        private static MemoryBeliefDecisionCandidateRecord[] ToDecisionCandidateRecords(List<DecisionCandidate> candidates)
        {
            if (candidates == null || candidates.Count == 0)
                return System.Array.Empty<MemoryBeliefDecisionCandidateRecord>();

            var records = new MemoryBeliefDecisionCandidateRecord[candidates.Count];
            for (int i = 0; i < candidates.Count; i++)
            {
                var candidate = candidates[i];
                records[i] = new MemoryBeliefDecisionCandidateRecord
                {
                    Intent = candidate.Kind,
                    Available = candidate.IsAvailable,
                    Need = candidate.Metadata.PrimaryNeed,
                    NeedUrgency01 = candidate.NeedUrgency01,
                    IsCritical = candidate.IsCritical,
                    RequiresBeliefTarget = candidate.Metadata.RequiresBeliefTarget,
                    BeliefResultEmpty = candidate.BeliefResult.IsEmpty,
                    Belief = candidate.BeliefResult.IsEmpty ? default : ToBeliefRef(candidate.BeliefResult.Belief),
                    Score = candidate.FinalScore,
                    FilteredReason = candidate.FilteredReason ?? string.Empty,
                    ScoreContributions = ToScoreContributionRefs(candidate.ScoreContributions),
                };
            }

            return records;
        }

        // =============================================================================
        // ToBeliefRef
        // =============================================================================
        /// <summary>
        /// <para>
        /// Copia una credenza selezionata in un riferimento EL minimale.
        /// </para>
        /// </summary>
        private static MemoryBeliefDecisionBeliefRef ToBeliefRef(BeliefEntry belief)
        {
            return new MemoryBeliefDecisionBeliefRef
            {
                Category = belief.Category,
                Status = belief.Status,
                Source = belief.Source,
                BeliefId = belief.BeliefId,
                EstimatedPosition = belief.EstimatedPosition,
                SubjectId = belief.SubjectId,
                Confidence = belief.Confidence,
                Freshness = belief.Freshness,
                SourceCount = belief.SourceCount,
            };
        }

        // =============================================================================
        // ToScoreContributionRefs
        // =============================================================================
        /// <summary>
        /// <para>
        /// Converte i contributi dello scoring decisionale in payload EL.
        /// </para>
        /// </summary>
        private static MemoryBeliefDecisionScoreContributionRef[] ToScoreContributionRefs(DecisionScoreContribution[] contributions)
        {
            if (contributions == null || contributions.Length == 0)
                return System.Array.Empty<MemoryBeliefDecisionScoreContributionRef>();

            var records = new MemoryBeliefDecisionScoreContributionRef[contributions.Length];
            for (int i = 0; i < contributions.Length; i++)
            {
                records[i] = new MemoryBeliefDecisionScoreContributionRef
                {
                    Label = contributions[i].Label,
                    Value = contributions[i].Value,
                };
            }

            return records;
        }

        private static float Clamp01(float value)
        {
            if (value < 0f) return 0f;
            if (value > 1f) return 1f;
            return value;
        }
    }
}
