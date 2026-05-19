using Arcontio.Core;
using Arcontio.Core.Config;
using NUnit.Framework;
using UnityEngine;

namespace Arcontio.Tests
{
    // =============================================================================
    // IntentExecutionRouterQaTests
    // =============================================================================
    /// <summary>
    /// <para>
    /// Test QA EditMode per l'estrazione v0.11c.01c del boundary
    /// <c>SelectedDecision -> JobRequest</c>.
    /// </para>
    ///
    /// <para><b>Router passivo, Job Layer autoritativo</b></para>
    /// <para>
    /// Il router deve produrre al massimo un <c>JobRequest</c> dati. Non puo'
    /// assegnare job, emettere command, decidere fallback legacy o arbitrare
    /// preemption. Queste verifiche proteggono il confine stabilito da ARC-CON-014
    /// e ARC-DEC-019.
    /// </para>
    ///
    /// <para><b>Struttura interna:</b></para>
    /// <list type="bullet">
    ///   <item><b>EatKnownFood</b>: request equivalente alla route food esistente.</item>
    ///   <item><b>SearchFood</b>: request equivalente alla probe locale esistente.</item>
    ///   <item><b>Boundaries</b>: nessun command, assignment, preemption o fallback migrato.</item>
    /// </list>
    /// </summary>
    public sealed class IntentExecutionRouterQaTests
    {
        // =============================================================================
        // EatKnownFoodRouteBuildsExpectedJobRequest
        // =============================================================================
        /// <summary>
        /// <para>
        /// Verifica che <c>EatKnownFood</c> continui a produrre la stessa forma di
        /// <c>JobRequest</c> usata dalla vertical slice food.
        /// </para>
        /// </summary>
        [Test]
        public void EatKnownFoodRouteBuildsExpectedJobRequest()
        {
            var router = new IntentExecutionRouter();
            var candidate = MakeFoodCandidate(urgency01: 0.91f, isCritical: true, beliefId: 12, position: new Vector2Int(5, 7));

            bool routed = router.TryRouteEatKnownFood(
                tick: 31,
                npcId: 4,
                selectedCandidate: candidate,
                targetObjectId: 99,
                out var result);

            Assert.That(routed, Is.True);
            Assert.That(result.HasJobRequest, Is.True);
            Assert.That(result.Kind, Is.EqualTo(IntentExecutionRouteKind.EatKnownFoodJobRequest));
            Assert.That(result.Reason, Is.EqualTo("JobRequestBuilt"));
            Assert.That(result.Request.RequestId, Is.EqualTo("jobreq_food_4_99_31"));
            Assert.That(result.Request.NpcId, Is.EqualTo(4));
            Assert.That(result.Request.IntentKind, Is.EqualTo(DecisionIntentKind.EatKnownFood));
            Assert.That(result.Request.PriorityClass, Is.EqualTo(JobPriorityClass.Critical));
            Assert.That(result.Request.Urgency01, Is.EqualTo(0.91f).Within(0.0001f));
            Assert.That(result.Request.CreatedTick, Is.EqualTo(31));
            Assert.That(result.Request.HasTargetCell, Is.True);
            Assert.That(result.Request.TargetCell, Is.EqualTo(new Vector2Int(5, 7)));
            Assert.That(result.Request.TargetObjectId, Is.EqualTo(99));
            Assert.That(result.Request.BeliefKey, Is.EqualTo("Food:12@5,7"));
            Assert.That(result.Request.DebugLabel, Is.EqualTo("FoodJobVerticalSlice"));
        }

        // =============================================================================
        // SearchFoodRouteBuildsExpectedJobRequest
        // =============================================================================
        /// <summary>
        /// <para>
        /// Verifica che <c>SearchFood</c> continui a produrre una richiesta di probe
        /// locale senza target object.
        /// </para>
        /// </summary>
        [Test]
        public void SearchFoodRouteBuildsExpectedJobRequest()
        {
            var router = new IntentExecutionRouter();
            var candidate = MakeSearchFoodCandidate(urgency01: 0.73f, isCritical: false);

            bool routed = router.TryRouteSearchFood(
                tick: 18,
                npcId: 3,
                selectedCandidate: candidate,
                targetCell: new Vector2Int(6, 4),
                out var result);

            Assert.That(routed, Is.True);
            Assert.That(result.HasJobRequest, Is.True);
            Assert.That(result.Kind, Is.EqualTo(IntentExecutionRouteKind.SearchFoodJobRequest));
            Assert.That(result.Reason, Is.EqualTo("JobRequestBuilt"));
            Assert.That(result.Request.RequestId, Is.EqualTo("jobreq_search_food_probe_3_6_4_18"));
            Assert.That(result.Request.NpcId, Is.EqualTo(3));
            Assert.That(result.Request.IntentKind, Is.EqualTo(DecisionIntentKind.SearchFood));
            Assert.That(result.Request.PriorityClass, Is.EqualTo(JobPriorityClass.Normal));
            Assert.That(result.Request.Urgency01, Is.EqualTo(0.73f).Within(0.0001f));
            Assert.That(result.Request.HasTargetCell, Is.True);
            Assert.That(result.Request.TargetCell, Is.EqualTo(new Vector2Int(6, 4)));
            Assert.That(result.Request.TargetObjectId, Is.EqualTo(0));
            Assert.That(result.Request.BeliefKey, Is.Empty);
            Assert.That(result.Request.DebugLabel, Is.EqualTo("SearchFoodLocalProbe"));
        }

        // =============================================================================
        // UnsupportedIntentDoesNotMigrateLegacyFallback
        // =============================================================================
        /// <summary>
        /// <para>
        /// Verifica che un intent non supportato dal boundary job non venga trasformato
        /// in fallback legacy dentro il router.
        /// </para>
        ///
        /// <para><b>Fallback resta nel bridge transitorio</b></para>
        /// <para>
        /// Il router deve limitarsi a rifiutare la route dati. La decisione se usare
        /// fallback command legacy resta in <c>NeedsDecisionRule</c>.
        /// </para>
        /// </summary>
        [Test]
        public void UnsupportedIntentDoesNotMigrateLegacyFallback()
        {
            var router = new IntentExecutionRouter();
            var candidate = MakeRestCandidate();

            bool routed = router.TryRouteEatKnownFood(
                tick: 9,
                npcId: 2,
                selectedCandidate: candidate,
                targetObjectId: 44,
                out var result);

            Assert.That(routed, Is.False);
            Assert.That(result.HasJobRequest, Is.False);
            Assert.That(result.Request.NpcId, Is.EqualTo(0));
            Assert.That(result.Reason, Is.EqualTo("UnsupportedJobRequestIntent"));
        }

        // =============================================================================
        // RouterDoesNotEmitCommandAssignJobOrPreempt
        // =============================================================================
        /// <summary>
        /// <para>
        /// Verifica che il router non abbia effetti collaterali su command buffer,
        /// active job o preemption state.
        /// </para>
        /// </summary>
        [Test]
        public void RouterDoesNotEmitCommandAssignJobOrPreempt()
        {
            var world = new World(new WorldConfig(new SimulationParams()));
            int npcId = world.CreateNpc(
                NpcDnaProfile.CreateDefault("intent_execution_router_qa"),
                NpcNeeds.Make(0.9f, 0.1f),
                new Arcontio.Core.Social(),
                2,
                2);

            var router = new IntentExecutionRouter();
            var candidate = MakeSearchFoodCandidate(urgency01: 0.9f, isCritical: true);

            bool routed = router.TryRouteSearchFood(
                tick: 40,
                npcId: npcId,
                selectedCandidate: candidate,
                targetCell: new Vector2Int(3, 2),
                out var result);

            Assert.That(routed, Is.True);
            Assert.That((object)result, Is.Not.AssignableTo<ICommand>());
            Assert.That(world.JobRuntimeState.HasActiveJob(npcId), Is.False);
            Assert.That(world.JobRuntimeState.ActiveJobCount, Is.EqualTo(0));
            Assert.That(world.JobRuntimeState.CommandBuffer.Count, Is.EqualTo(0));
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
                    Status = BeliefStatus.Active
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

        private static DecisionCandidate MakeRestCandidate()
        {
            return DecisionCandidate.Available(
                DecisionIntentCatalog.GetMetadata(DecisionIntentKind.RestKnownPlace),
                urgency01: 0.6f,
                isCritical: false);
        }
    }
}
