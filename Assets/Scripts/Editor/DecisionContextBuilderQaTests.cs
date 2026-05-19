using Arcontio.Core;
using Arcontio.Core.Config;
using NUnit.Framework;
using UnityEngine;

namespace Arcontio.Tests
{
    // =============================================================================
    // DecisionContextBuilderQaTests
    // =============================================================================
    /// <summary>
    /// <para>
    /// Test QA EditMode per l'estrazione v0.11c.01b del
    /// <c>DecisionContextBuilder</c>.
    /// </para>
    ///
    /// <para><b>Context gathering senza decisione</b></para>
    /// <para>
    /// Questi test proteggono il confine della patch: il builder puo' leggere solo
    /// gli store per-NPC necessari a costruire un <c>DecisionEvaluationContext</c>,
    /// ma non puo' produrre command, job, fallback o preemption. Il comportamento
    /// deve restare equivalente alla costruzione inline precedente dentro
    /// <c>NeedsDecisionRule</c>.
    /// </para>
    ///
    /// <para><b>Struttura interna:</b></para>
    /// <list type="bullet">
    ///   <item><b>Success path</b>: verifica copia dei campi whitelisted nel context.</item>
    ///   <item><b>Missing input</b>: verifica fallimento pulito senza inventare store cognitivi.</item>
    ///   <item><b>No side effects</b>: il builder non assegna job e non tocca command buffer.</item>
    /// </list>
    /// </summary>
    public sealed class DecisionContextBuilderQaTests
    {
        // =============================================================================
        // DecisionContextBuilderBuildsWhitelistedContextFromWorldStores
        // =============================================================================
        /// <summary>
        /// <para>
        /// Verifica che il builder costruisca lo stesso contesto whitelisted usato dal
        /// Decision Layer: bisogni, DNA, profilo, posizione, belief e config query.
        /// </para>
        ///
        /// <para><b>World come adapter transitorio</b></para>
        /// <para>
        /// Il test crea un <c>World</c> perche' oggi gli store per-NPC vivono li', ma
        /// controlla che l'output resti un <c>DecisionEvaluationContext</c> passivo e
        /// non un accesso diretto alla verita' oggettiva.
        /// </para>
        /// </summary>
        [Test]
        public void DecisionContextBuilderBuildsWhitelistedContextFromWorldStores()
        {
            var world = MakeWorldWithNpc(npcX: 4, npcY: 6, out int npcId);
            var needs = world.Needs[npcId];
            var builder = new DecisionContextBuilder();

            bool built = builder.TryBuild(world, npcId, in needs, nowTick: 17, out var context);

            Assert.That(built, Is.True);
            Assert.That(context.NpcId, Is.EqualTo(npcId));
            Assert.That(context.Tick, Is.EqualTo(17));
            Assert.That(context.NpcPosition, Is.EqualTo(new Vector2Int(4, 6)));
            Assert.That(context.Needs.GetValue(NeedKind.Hunger), Is.EqualTo(needs.GetValue(NeedKind.Hunger)).Within(0.0001f));
            Assert.That(context.Dna, Is.SameAs(world.NpcDna[npcId]));
            Assert.That(context.Profile, Is.SameAs(world.NpcProfiles[npcId]));
            Assert.That(context.Beliefs, Is.SameAs(world.Beliefs[npcId]));
            Assert.That(context.ExplainabilityRegistry, Is.SameAs(world.MemoryBeliefDecisionExplainability));
            Assert.That(world.JobRuntimeState.HasActiveJob(npcId), Is.False);
            Assert.That(world.JobRuntimeState.CommandBuffer.Count, Is.EqualTo(0));
        }

        // =============================================================================
        // DecisionContextBuilderFailsWhenBeliefStoreIsMissing
        // =============================================================================
        /// <summary>
        /// <para>
        /// Verifica che il builder non inventi un <c>BeliefStore</c> quando lo store
        /// soggettivo richiesto manca.
        /// </para>
        ///
        /// <para><b>Nessun fallback cognitivo nascosto</b></para>
        /// <para>
        /// Il comportamento preserva il vecchio metodo inline: senza belief store il
        /// context non viene costruito e la decisione resta responsabilita' del
        /// chiamante legacy.
        /// </para>
        /// </summary>
        [Test]
        public void DecisionContextBuilderFailsWhenBeliefStoreIsMissing()
        {
            var world = MakeWorldWithNpc(npcX: 4, npcY: 6, out int npcId);
            var needs = world.Needs[npcId];
            world.Beliefs.Remove(npcId);
            var builder = new DecisionContextBuilder();

            bool built = builder.TryBuild(world, npcId, in needs, nowTick: 17, out var context);

            Assert.That(built, Is.False);
            Assert.That(context.NpcId, Is.EqualTo(0));
            Assert.That(world.JobRuntimeState.HasActiveJob(npcId), Is.False);
            Assert.That(world.JobRuntimeState.CommandBuffer.Count, Is.EqualTo(0));
        }

        private static World MakeWorldWithNpc(int npcX, int npcY, out int npcId)
        {
            var world = new World(new WorldConfig(new SimulationParams()));
            world.Global.Needs = NeedsConfig.Default();
            world.Global.BeliefQuery = BeliefQueryConfig.Default();

            npcId = world.CreateNpc(
                NpcDnaProfile.CreateDefault("decision_context_builder_qa"),
                NpcNeeds.Make(0.80f, 0.20f),
                new Arcontio.Core.Social { JusticePerception01 = 0.9f },
                npcX,
                npcY);

            world.Config.Sim.memory_belief_decision_explainability.enabled = true;
            return world;
        }
    }
}
