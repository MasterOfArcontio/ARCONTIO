using System.Collections.Generic;
using Arcontio.Core;
using Arcontio.Core.Config;
using NUnit.Framework;
using UnityEngine;

namespace Arcontio.Tests
{
    // =============================================================================
    // DecisionExplainabilityBridgeQaTests
    // =============================================================================
    /// <summary>
    /// <para>
    /// Test QA EditMode per l'estrazione v0.11c.01d del boundary di explainability
    /// decisionale da <c>NeedsDecisionRule</c>.
    /// </para>
    ///
    /// <para><b>Explainability read-only, non runtime authority</b></para>
    /// <para>
    /// Il bridge deve copiare trace gia' determinate dalla pipeline MBQD, senza
    /// produrre command, job request nuove, assegnazioni job, fallback o preemption.
    /// Questi test proteggono il confine stabilito da ARC-CON-014, ARC-DEC-018 e
    /// ARC-DEC-019: osservare una decisione non significa deciderla di nuovo.
    /// </para>
    ///
    /// <para><b>Struttura interna:</b></para>
    /// <list type="bullet">
    ///   <item><b>Decision trace</b>: verifica snapshot candidati/selection.</item>
    ///   <item><b>Bridge trace</b>: verifica fallback trace equivalente e senza command.</item>
    ///   <item><b>JobRequest trace</b>: verifica che il record non assegni job.</item>
    /// </list>
    /// </summary>
    public sealed class DecisionExplainabilityBridgeQaTests
    {
        // =============================================================================
        // DecisionTraceWritesSelectionSnapshotWithoutWorldMutation
        // =============================================================================
        /// <summary>
        /// <para>
        /// Verifica che il bridge scriva una trace decisionale equivalente ai dati
        /// ricevuti, senza mutare World, command buffer o JobRuntimeState.
        /// </para>
        /// </summary>
        [Test]
        public void DecisionTraceWritesSelectionSnapshotWithoutWorldMutation()
        {
            var world = MakeWorldWithNpc(out int npcId);
            var context = BuildContext(world, npcId, tick: 41);
            var bridge = new DecisionExplainabilityBridge();
            var candidate = MakeFoodCandidate(urgency01: 0.91f, isCritical: true, beliefId: 7, position: new Vector2Int(5, 7));
            candidate.AttachScore(
                1.43f,
                new[]
                {
                    new DecisionScoreContribution("NeedUrgency", 0.91f),
                    new DecisionScoreContribution("MemoryConfidence", 0.52f),
                });
            var candidates = new List<DecisionCandidate> { candidate };
            var selection = new DecisionSelectionResult(false, 0, candidate);
            var selectionConfig = DecisionSelectionConfig.Default();

            int objectCountBefore = world.Objects.Count;
            int foodStockCountBefore = world.FoodStocks.Count;
            int activeJobCountBefore = world.JobRuntimeState.ActiveJobCount;
            int commandCountBefore = world.JobRuntimeState.CommandBuffer.Count;

            bridge.TryEmitDecisionTrace(
                world.Config.Sim.memory_belief_decision_explainability,
                world.MemoryBeliefDecisionExplainability,
                context,
                auditValid: true,
                candidates,
                selection,
                selectionConfig);

            Assert.That(world.Objects.Count, Is.EqualTo(objectCountBefore));
            Assert.That(world.FoodStocks.Count, Is.EqualTo(foodStockCountBefore));
            Assert.That(world.JobRuntimeState.ActiveJobCount, Is.EqualTo(activeJobCountBefore));
            Assert.That(world.JobRuntimeState.CommandBuffer.Count, Is.EqualTo(commandCountBefore));
            Assert.That(world.MemoryBeliefDecisionExplainability.TryGetNpcStore(npcId, out var store), Is.True);
            Assert.That(store.TryGetLatestDecisionTrace(out var trace), Is.True);
            Assert.That(trace.Kind, Is.EqualTo(MemoryBeliefDecisionTraceKind.Decision));
            Assert.That(trace.Decision.SelectedIntent, Is.EqualTo(DecisionIntentKind.EatKnownFood));
            Assert.That(trace.Decision.SelectedScore, Is.EqualTo(1.43f).Within(0.0001f));
            Assert.That(trace.Decision.CandidateCount, Is.EqualTo(1));
            Assert.That(trace.Decision.Candidates[0].Intent, Is.EqualTo(DecisionIntentKind.EatKnownFood));
            Assert.That(trace.Decision.Candidates[0].Belief.EstimatedPosition, Is.EqualTo(new Vector2Int(5, 7)));
            Assert.That(trace.Decision.Candidates[0].ScoreContributions.Length, Is.EqualTo(2));
            Assert.That(trace.Decision.Candidates[0].ScoreContributions[0].Label, Is.EqualTo("NeedUrgency"));
            Assert.That((object)trace, Is.Not.AssignableTo<ICommand>());
        }

        // =============================================================================
        // BridgeTracePreservesFallbackClassificationWithoutCommandEmission
        // =============================================================================
        /// <summary>
        /// <para>
        /// Verifica che una fallback trace rimanga classificata come dato diagnostico:
        /// nessun <c>ICommand</c>, nessun job e nessuna preemption vengono prodotti.
        /// </para>
        /// </summary>
        [Test]
        public void BridgeTracePreservesFallbackClassificationWithoutCommandEmission()
        {
            var world = MakeWorldWithNpc(out int npcId);
            var bridge = new DecisionExplainabilityBridge();
            var candidate = MakeSearchFoodCandidate(urgency01: 0.82f, isCritical: false);

            bridge.TryEmitBridgeTrace(
                world.Config.Sim.memory_belief_decision_explainability,
                world.MemoryBeliefDecisionExplainability,
                tick: 52,
                npcId: npcId,
                candidate,
                command: null,
                didSteal: false,
                didMove: false,
                handled: false,
                legacyFallbackUsed: true,
                fallbackKind: LegacyFallbackKind.NonExecutableIntentFallback,
                reason: "NonExecutableIntentFallback:SearchFoodJobRouteRejected:TemplateMissing");

            Assert.That(world.JobRuntimeState.HasActiveJob(npcId), Is.False);
            Assert.That(world.JobRuntimeState.ActiveJobCount, Is.EqualTo(0));
            Assert.That(world.JobRuntimeState.CommandBuffer.Count, Is.EqualTo(0));
            Assert.That(world.MemoryBeliefDecisionExplainability.TryGetNpcStore(npcId, out var store), Is.True);
            Assert.That(store.TryGetLatestBridgeTrace(out var trace), Is.True);
            Assert.That(trace.Kind, Is.EqualTo(MemoryBeliefDecisionTraceKind.Bridge));
            Assert.That(trace.Bridge.SelectedIntent, Is.EqualTo(DecisionIntentKind.SearchFood));
            Assert.That(trace.Bridge.CommandName, Is.Empty);
            Assert.That(trace.Bridge.Handled, Is.False);
            Assert.That(trace.Bridge.LegacyFallbackUsed, Is.True);
            Assert.That(trace.Bridge.FallbackKind, Is.EqualTo(LegacyFallbackKind.NonExecutableIntentFallback));
            Assert.That(trace.Bridge.Reason, Is.EqualTo("NonExecutableIntentFallback:SearchFoodJobRouteRejected:TemplateMissing"));
            Assert.That((object)trace, Is.Not.AssignableTo<ICommand>());
        }

        // =============================================================================
        // JobRequestTraceCopiesSuppliedRequestWithoutAssigningJob
        // =============================================================================
        /// <summary>
        /// <para>
        /// Verifica che la trace <c>Decision -> JobRequest</c> copi una request gia'
        /// costruita da altri componenti, senza crearne una nuova e senza assegnarla.
        /// </para>
        /// </summary>
        [Test]
        public void JobRequestTraceCopiesSuppliedRequestWithoutAssigningJob()
        {
            var world = MakeWorldWithNpc(out int npcId);
            var bridge = new DecisionExplainabilityBridge();
            var request = JobRequest.FromDecision(
                requestId: "jobreq_food_qa_1",
                npcId: npcId,
                intentKind: DecisionIntentKind.EatKnownFood,
                priorityClass: JobPriorityClass.Critical,
                urgency01: 0.95f,
                createdTick: 63,
                targetCell: new Vector2Int(8, 4),
                beliefKey: "Food:9@8,4",
                debugLabel: "DecisionExplainabilityBridgeQa");

            bridge.TryEmitJobRequestTrace(
                world.Config.Sim.memory_belief_decision_explainability,
                world.MemoryBeliefDecisionExplainability,
                tick: 63,
                npcId: npcId,
                request,
                jobId: "job_food_qa_1",
                legacyBridgeStillUsed: false);

            Assert.That(world.JobRuntimeState.HasActiveJob(npcId), Is.False);
            Assert.That(world.JobRuntimeState.ActiveJobCount, Is.EqualTo(0));
            Assert.That(world.JobRuntimeState.CommandBuffer.Count, Is.EqualTo(0));
            Assert.That(world.MemoryBeliefDecisionExplainability.TryGetNpcStore(npcId, out var store), Is.True);
            Assert.That(store.TryGetLatestJobRequestTrace(out var trace), Is.True);
            Assert.That(trace.Kind, Is.EqualTo(MemoryBeliefDecisionTraceKind.JobRequest));
            Assert.That(trace.JobRequest.RequestId, Is.EqualTo("jobreq_food_qa_1"));
            Assert.That(trace.JobRequest.JobId, Is.EqualTo("job_food_qa_1"));
            Assert.That(trace.JobRequest.Intent, Is.EqualTo(DecisionIntentKind.EatKnownFood));
            Assert.That(trace.JobRequest.PriorityClass, Is.EqualTo(JobPriorityClass.Critical));
            Assert.That(trace.JobRequest.HasTargetCell, Is.True);
            Assert.That(trace.JobRequest.TargetCell, Is.EqualTo(new Vector2Int(8, 4)));
            Assert.That(trace.JobRequest.BeliefKey, Is.EqualTo("Food:9@8,4"));
            Assert.That(trace.JobRequest.LegacyBridgeStillUsed, Is.False);
            Assert.That((object)trace, Is.Not.AssignableTo<ICommand>());
        }

        private static World MakeWorldWithNpc(out int npcId)
        {
            var world = new World(new WorldConfig(new SimulationParams()));
            world.Global.Needs = NeedsConfig.Default();
            world.Global.BeliefQuery = BeliefQueryConfig.Default();

            npcId = world.CreateNpc(
                NpcDnaProfile.CreateDefault("decision_explainability_bridge_qa"),
                NpcNeeds.Make(0.88f, 0.18f),
                new Arcontio.Core.Social(),
                2,
                2);

            world.Config.Sim.memory_belief_decision_explainability.enabled = true;
            world.Config.Sim.memory_belief_decision_explainability.writeJsonLog = false;
            world.Config.Sim.memory_belief_decision_explainability.logDecision = true;
            world.Config.Sim.memory_belief_decision_explainability.logBridge = true;
            world.Config.Sim.memory_belief_decision_explainability.logJobRequest = true;
            world.Config.Sim.memory_belief_decision_explainability.includeCandidates = true;
            world.Config.Sim.memory_belief_decision_explainability.includeScoreBreakdown = true;

            return world;
        }

        private static DecisionEvaluationContext BuildContext(World world, int npcId, int tick)
        {
            var builder = new DecisionContextBuilder();
            var needs = world.Needs[npcId];

            bool built = builder.TryBuild(world, npcId, in needs, tick, out var context);
            Assert.That(built, Is.True);
            return context;
        }

        private static DecisionCandidate MakeFoodCandidate(float urgency01, bool isCritical, int beliefId, Vector2Int position)
        {
            var candidate = DecisionCandidate.Available(
                DecisionIntentCatalog.GetMetadata(DecisionIntentKind.EatKnownFood),
                urgency01,
                isCritical);

            candidate.AttachBeliefResult(new BeliefQueryResult(
                false,
                new BeliefEntry
                {
                    BeliefId = beliefId,
                    Category = BeliefCategory.Food,
                    EstimatedPosition = position,
                    Confidence = 0.9f,
                    Freshness = 0.8f,
                    LastUpdatedTick = 4,
                    SourceCount = 1,
                    Source = BeliefSource.Seen,
                    Status = BeliefStatus.Active,
                },
                finalScore: 0.95f,
                contributions: System.Array.Empty<BeliefScoreContribution>()));

            return candidate;
        }

        private static DecisionCandidate MakeSearchFoodCandidate(float urgency01, bool isCritical)
        {
            return DecisionCandidate.Available(
                DecisionIntentCatalog.GetMetadata(DecisionIntentKind.SearchFood),
                urgency01,
                isCritical);
        }
    }
}
