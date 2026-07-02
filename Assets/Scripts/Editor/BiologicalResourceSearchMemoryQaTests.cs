using System.Collections.Generic;
using Arcontio.Core;
using Arcontio.Core.Config;
using NUnit.Framework;

namespace Arcontio.EditorTests
{
    // =============================================================================
    // BiologicalResourceSearchMemoryQaTests
    // =============================================================================
    /// <summary>
    /// <para>
    /// Test EditMode per la memoria soggettiva di ricerca risorsa biologica da
    /// landmark biologico, introdotta nello step v0.71.05.K.
    /// </para>
    ///
    /// <para><b>Principio architetturale: ricerca ricordata senza belief anticipata</b></para>
    /// <para>
    /// I test proteggono il confine: l'evento e' actor-bound, crea memoria solo per
    /// l'NPC che ha cercato, distingue prodotti diversi e non scrive belief. Il job
    /// harvest/search reale verra' collegato piu' avanti.
    /// </para>
    ///
    /// <para><b>Struttura interna:</b></para>
    /// <list type="bullet">
    ///   <item><b>Rule</b>: verifica campi trace e filtri biologici.</item>
    ///   <item><b>MemoryEncoding</b>: verifica observer-bound e merge.</item>
    ///   <item><b>Belief/Save</b>: verifica nessuna belief prematura e DTO compatibile.</item>
    /// </list>
    /// </summary>
    public sealed class BiologicalResourceSearchMemoryQaTests
    {
        // =============================================================================
        // BiologicalAnchorSearchBuildsResourceSearchTrace
        // =============================================================================
        /// <summary>
        /// <para>
        /// Verifica il caso base: ricerca valida da BiologicalAnchor, productKey
        /// normalizzato e hash numerico stabile per il merge del MemoryStore.
        /// </para>
        /// </summary>
        [Test]
        public void BiologicalAnchorSearchBuildsResourceSearchTrace()
        {
            World world = MakeWorldWithNpc(out int npcId);
            var rule = new BiologicalResourceSearchFromLandmarkMemoryRule();
            var ev = new BiologicalResourceSearchFromLandmarkEvent(
                npcId,
                77,
                LandmarkRegistry.LandmarkKind.BiologicalAnchor,
                "Berry",
                4,
                5,
                0.72f);

            bool encoded = rule.TryEncode(world, npcId, ev, ev.SearchQuality01, out MemoryTrace trace);

            Assert.That(encoded, Is.True);
            Assert.That(trace.Type, Is.EqualTo(MemoryType.ResourceSearchFromLandmark));
            Assert.That(trace.SubjectId, Is.EqualTo(77));
            Assert.That(trace.SubjectDefId, Is.EqualTo("berry"));
            Assert.That(trace.SecondarySubjectId, Is.EqualTo(BiologicalResourceSearchFromLandmarkMemoryRule.ComputeStableProductKeyHash("berry")));
            Assert.That(trace.CellX, Is.EqualTo(4));
            Assert.That(trace.CellY, Is.EqualTo(5));
            Assert.That(trace.Reliability01, Is.EqualTo(0.72f).Within(0.0001f));
            Assert.That(trace.IsHeard, Is.False);
        }

        // =============================================================================
        // NonBiologicalLandmarkSearchDoesNotBuildTrace
        // =============================================================================
        /// <summary>
        /// <para>
        /// Protegge lo scope di K: la trace e' generalizzabile, ma oggi accetta
        /// solo landmark biologici e rifiuta gli anchor di supporto navigazionale.
        /// </para>
        /// </summary>
        [Test]
        public void NonBiologicalLandmarkSearchDoesNotBuildTrace()
        {
            World world = MakeWorldWithNpc(out int npcId);
            var rule = new BiologicalResourceSearchFromLandmarkMemoryRule();
            var ev = new BiologicalResourceSearchFromLandmarkEvent(
                npcId,
                88,
                LandmarkRegistry.LandmarkKind.SupportOpenSpaceAnchor,
                "berry",
                6,
                7,
                0.80f);

            bool encoded = rule.TryEncode(world, npcId, ev, ev.SearchQuality01, out _);

            Assert.That(encoded, Is.False);
        }

        // =============================================================================
        // EmptyProductSearchDoesNotBuildTrace
        // =============================================================================
        /// <summary>
        /// <para>
        /// Una ricerca senza risorsa cercata non produce memoria utile e non deve
        /// diventare input ambiguo per belief futuri.
        /// </para>
        /// </summary>
        [Test]
        public void EmptyProductSearchDoesNotBuildTrace()
        {
            World world = MakeWorldWithNpc(out int npcId);
            var rule = new BiologicalResourceSearchFromLandmarkMemoryRule();
            var ev = new BiologicalResourceSearchFromLandmarkEvent(
                npcId,
                88,
                LandmarkRegistry.LandmarkKind.BiologicalAnchor,
                "",
                6,
                7,
                0.80f);

            bool encoded = rule.TryEncode(world, npcId, ev, ev.SearchQuality01, out _);

            Assert.That(encoded, Is.False);
        }

        // =============================================================================
        // MemoryEncodingProcessesSearchOnlyForActor
        // =============================================================================
        /// <summary>
        /// <para>
        /// Conferma che l'evento e' actor-bound: il MemoryEncodingSystem non deve
        /// passare dal gate testimoni e non deve duplicare la trace su NPC vicini.
        /// </para>
        /// </summary>
        [Test]
        public void MemoryEncodingProcessesSearchOnlyForActor()
        {
            World world = MakeWorldWithTwoNpcs(out int actorNpcId, out int otherNpcId);
            var events = new List<ISimEvent>
            {
                new BiologicalResourceSearchFromLandmarkEvent(
                    actorNpcId,
                    101,
                    LandmarkRegistry.LandmarkKind.BiologicalAnchor,
                    "berry",
                    3,
                    3,
                    0.90f)
            };

            EncodeEvents(world, events, tick: 20);

            Assert.That(world.Memory[actorNpcId].Traces.Count, Is.EqualTo(1));
            Assert.That(world.Memory[actorNpcId].Traces[0].Type, Is.EqualTo(MemoryType.ResourceSearchFromLandmark));
            Assert.That(world.Memory[actorNpcId].Traces[0].LastObservedTick, Is.EqualTo(20));
            Assert.That(world.Memory[otherNpcId].Traces.Count, Is.EqualTo(0));
            Assert.That(world.Beliefs[actorNpcId].Entries.Count, Is.EqualTo(0));
        }

        // =============================================================================
        // DifferentProductsFromSameLandmarkStayDistinct
        // =============================================================================
        /// <summary>
        /// <para>
        /// Verifica il punto piu' fragile: il MemoryStore non confronta SubjectDefId
        /// nell'equivalenza, quindi il productKey deve differenziare il secondary id.
        /// </para>
        /// </summary>
        [Test]
        public void DifferentProductsFromSameLandmarkStayDistinct()
        {
            World world = MakeWorldWithNpc(out int npcId);
            var events = new List<ISimEvent>
            {
                new BiologicalResourceSearchFromLandmarkEvent(npcId, 202, LandmarkRegistry.LandmarkKind.BiologicalAnchor, "berry", 8, 9, 0.70f),
                new BiologicalResourceSearchFromLandmarkEvent(npcId, 202, LandmarkRegistry.LandmarkKind.BiologicalAnchor, "acorn", 8, 9, 0.75f)
            };

            EncodeEvents(world, events, tick: 31);

            Assert.That(world.Memory[npcId].Traces.Count, Is.EqualTo(2));
            Assert.That(world.Memory[npcId].Traces[0].SubjectDefId, Is.EqualTo("berry"));
            Assert.That(world.Memory[npcId].Traces[1].SubjectDefId, Is.EqualTo("acorn"));
            Assert.That(world.Memory[npcId].Traces[0].SecondarySubjectId, Is.Not.EqualTo(world.Memory[npcId].Traces[1].SecondarySubjectId));
        }

        // =============================================================================
        // RepeatedSameProductSearchMergesIntoSingleTrace
        // =============================================================================
        /// <summary>
        /// <para>
        /// La stessa ricerca ripetuta deve rinforzare la memoria esistente, evitando
        /// spam di trace equivalenti nello store dell'NPC.
        /// </para>
        /// </summary>
        [Test]
        public void RepeatedSameProductSearchMergesIntoSingleTrace()
        {
            World world = MakeWorldWithNpc(out int npcId);
            var events = new List<ISimEvent>
            {
                new BiologicalResourceSearchFromLandmarkEvent(npcId, 303, LandmarkRegistry.LandmarkKind.BiologicalAnchor, "berry", 8, 9, 0.70f),
                new BiologicalResourceSearchFromLandmarkEvent(npcId, 303, LandmarkRegistry.LandmarkKind.BiologicalAnchor, "BERRY", 8, 9, 0.75f)
            };

            EncodeEvents(world, events, tick: 32);

            Assert.That(world.Memory[npcId].Traces.Count, Is.EqualTo(1));
            Assert.That(world.Memory[npcId].Traces[0].SubjectId, Is.EqualTo(303));
            Assert.That(world.Memory[npcId].Traces[0].SubjectDefId, Is.EqualTo("berry"));
            Assert.That(world.Memory[npcId].Traces[0].Reliability01, Is.EqualTo(0.75f).Within(0.0001f));
        }

        // =============================================================================
        // BeliefUpdaterIgnoresResourceSearchTraceInStepK
        // =============================================================================
        /// <summary>
        /// <para>
        /// K produce solo memoria. Lo step L decidera' come trasformare questa trace
        /// in una belief potenziale del tipo "qui puo' esserci X".
        /// </para>
        /// </summary>
        [Test]
        public void BeliefUpdaterIgnoresResourceSearchTraceInStepK()
        {
            var updater = new BeliefUpdater();
            var store = new BeliefStore();
            var trace = new MemoryTrace
            {
                Type = MemoryType.ResourceSearchFromLandmark,
                SubjectId = 404,
                SecondarySubjectId = BiologicalResourceSearchFromLandmarkMemoryRule.ComputeStableProductKeyHash("berry"),
                SubjectDefId = "berry",
                CellX = 2,
                CellY = 2,
                Intensity01 = 1.0f,
                Reliability01 = 0.9f,
                DecayPerTick01 = 0.001f
            };

            bool updated = updater.UpdateFromTrace(trace, store, currentTick: 40);

            Assert.That(updated, Is.False);
            Assert.That(store.Entries.Count, Is.EqualTo(0));
        }

        // =============================================================================
        // SaveDtoPreservesResourceSearchTrace
        // =============================================================================
        /// <summary>
        /// <para>
        /// Il nuovo tipo di trace non richiede nuovi campi save: usa il DTO memoria
        /// esistente e conserva subject, productKey, hash e tick osservato.
        /// </para>
        /// </summary>
        [Test]
        public void SaveDtoPreservesResourceSearchTrace()
        {
            var trace = new MemoryTrace
            {
                Type = MemoryType.ResourceSearchFromLandmark,
                SubjectId = 505,
                SecondarySubjectId = BiologicalResourceSearchFromLandmarkMemoryRule.ComputeStableProductKeyHash("acorn"),
                SubjectDefId = "acorn",
                CellX = 10,
                CellY = 11,
                Intensity01 = 0.8f,
                Reliability01 = 0.7f,
                DecayPerTick01 = 0.001f,
                LastObservedTick = 55
            };

            var dto = MemoryTraceSaveData.FromTrace(trace);
            MemoryTrace restored = dto.ToTrace();

            Assert.That(restored.Type, Is.EqualTo(MemoryType.ResourceSearchFromLandmark));
            Assert.That(restored.SubjectId, Is.EqualTo(505));
            Assert.That(restored.SecondarySubjectId, Is.EqualTo(trace.SecondarySubjectId));
            Assert.That(restored.SubjectDefId, Is.EqualTo("acorn"));
            Assert.That(restored.CellX, Is.EqualTo(10));
            Assert.That(restored.CellY, Is.EqualTo(11));
            Assert.That(restored.LastObservedTick, Is.EqualTo(55));
        }

        // =============================================================================
        // MakeWorldWithNpc
        // =============================================================================
        /// <summary>
        /// <para>
        /// Crea un World minimo con un solo NPC, sufficiente a validare le rule
        /// memoria senza avviare SimulationHost o pipeline runtime complete.
        /// </para>
        /// </summary>
        private static World MakeWorldWithNpc(out int npcId)
        {
            var world = new World(new WorldConfig(new SimulationParams()));
            npcId = world.CreateNpc(
                NpcDnaProfile.CreateDefault("resource_search_actor"),
                NpcNeeds.Make(0.80f, 0.20f),
                new Social(),
                1,
                1);
            return world;
        }

        // =============================================================================
        // MakeWorldWithTwoNpcs
        // =============================================================================
        /// <summary>
        /// <para>
        /// Crea due NPC vicini per verificare che l'evento actor-bound non venga
        /// ridistribuito come fatto pubblico a tutti i possibili testimoni.
        /// </para>
        /// </summary>
        private static World MakeWorldWithTwoNpcs(out int actorNpcId, out int otherNpcId)
        {
            World world = MakeWorldWithNpc(out actorNpcId);
            otherNpcId = world.CreateNpc(
                NpcDnaProfile.CreateDefault("resource_search_other"),
                NpcNeeds.Make(0.80f, 0.20f),
                new Social(),
                2,
                1);
            return world;
        }

        // =============================================================================
        // EncodeEvents
        // =============================================================================
        /// <summary>
        /// <para>
        /// Esegue il MemoryEncodingSystem su una lista controllata di eventi,
        /// simulando il buffer gia' drainato dal MessageBus.
        /// </para>
        /// </summary>
        private static void EncodeEvents(World world, List<ISimEvent> events, int tick)
        {
            var memoryEncoding = new MemoryEncodingSystem();
            memoryEncoding.SetEventsBuffer(events);
            memoryEncoding.Update(world, new Tick(tick, 1f), new MessageBus(), new Telemetry());
        }
    }
}
