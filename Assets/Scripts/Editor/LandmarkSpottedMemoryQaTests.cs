using System.Collections.Generic;
using Arcontio.Core;
using Arcontio.Core.Config;
using Arcontio.Core.Diagnostics;
using NUnit.Framework;

namespace Arcontio.EditorTests
{
    // =============================================================================
    // LandmarkSpottedMemoryQaTests
    // =============================================================================
    /// <summary>
    /// <para>
    /// Test EditMode per la memory trace generalizzabile dei landmark visti, usata
    /// nello step v0.71.05.J solo per i <c>BiologicalAnchor</c>.
    /// </para>
    ///
    /// <para><b>Principio architetturale: percezione soggettiva non onnisciente</b></para>
    /// <para>
    /// I test proteggono il fatto che l'evento landmark sia observer-bound: se un
    /// NPC vede un landmark biologico, solo quello specifico NPC riceve la trace.
    /// Gli altri NPC dovranno percepire lo stesso nodo con il proprio ciclo
    /// percettivo per memorizzarlo.
    /// </para>
    ///
    /// <para><b>Struttura interna:</b></para>
    /// <list type="bullet">
    ///   <item><b>Rule</b>: verifica campi e filtro BiologicalAnchor.</item>
    ///   <item><b>MemoryEncoding</b>: verifica observer-bound e merge.</item>
    ///   <item><b>Belief</b>: verifica che J non produca belief biologiche premature.</item>
    /// </list>
    /// </summary>
    public sealed class LandmarkSpottedMemoryQaTests
    {
        // =============================================================================
        // BiologicalAnchorEventBuildsLandmarkSpottedTrace
        // =============================================================================
        /// <summary>
        /// <para>
        /// Un landmark biologico visto produce una trace LandmarkSpotted con node id
        /// come soggetto e kind numerico come metadato compatto.
        /// </para>
        /// </summary>
        [Test]
        public void BiologicalAnchorEventBuildsLandmarkSpottedTrace()
        {
            World world = MakeWorldWithNpc(out int npcId);
            var rule = new LandmarkSpottedMemoryRule();
            var ev = new LandmarkSpottedEvent(
                npcId,
                77,
                LandmarkRegistry.LandmarkKind.BiologicalAnchor,
                4,
                5,
                0.72f);

            bool encoded = rule.TryEncode(world, npcId, ev, ev.WitnessQuality01, out MemoryTrace trace);

            Assert.That(encoded, Is.True);
            Assert.That(trace.Type, Is.EqualTo(MemoryType.LandmarkSpotted));
            Assert.That(trace.SubjectId, Is.EqualTo(77));
            Assert.That(trace.SecondarySubjectId, Is.EqualTo((int)LandmarkRegistry.LandmarkKind.BiologicalAnchor));
            Assert.That(trace.SubjectDefId, Is.Empty);
            Assert.That(trace.CellX, Is.EqualTo(4));
            Assert.That(trace.CellY, Is.EqualTo(5));
            Assert.That(trace.Reliability01, Is.EqualTo(0.72f).Within(0.0001f));
            Assert.That(trace.IsHeard, Is.False);
        }

        // =============================================================================
        // NonBiologicalLandmarkDoesNotBuildTraceInStepJ
        // =============================================================================
        /// <summary>
        /// <para>
        /// Il contratto resta generalizzabile, ma in J la rule e' limitata ai
        /// BiologicalAnchor. I support landmark e i doorway non entrano ancora nella
        /// memoria narrativa.
        /// </para>
        /// </summary>
        [Test]
        public void NonBiologicalLandmarkDoesNotBuildTraceInStepJ()
        {
            World world = MakeWorldWithNpc(out int npcId);
            var rule = new LandmarkSpottedMemoryRule();
            var ev = new LandmarkSpottedEvent(
                npcId,
                88,
                LandmarkRegistry.LandmarkKind.SupportOpenSpaceAnchor,
                6,
                7,
                0.80f);

            bool encoded = rule.TryEncode(world, npcId, ev, ev.WitnessQuality01, out _);

            Assert.That(encoded, Is.False);
        }

        // =============================================================================
        // MemoryEncodingProcessesLandmarkSpottedOnlyForDeclaredObserver
        // =============================================================================
        /// <summary>
        /// <para>
        /// LandmarkSpottedEvent e' gia' observer-bound: il memory encoder non deve
        /// ridistribuirlo agli altri NPC tramite il vecchio gate testimoni.
        /// </para>
        /// </summary>
        [Test]
        public void MemoryEncodingProcessesLandmarkSpottedOnlyForDeclaredObserver()
        {
            World world = MakeWorldWithTwoNpcs(out int observerNpcId, out int otherNpcId);
            var events = new List<ISimEvent>
            {
                new LandmarkSpottedEvent(
                    observerNpcId,
                    101,
                    LandmarkRegistry.LandmarkKind.BiologicalAnchor,
                    3,
                    3,
                    0.90f)
            };

            EncodeEvents(world, events, tick: 20);

            Assert.That(world.Memory[observerNpcId].Traces.Count, Is.EqualTo(1));
            Assert.That(world.Memory[observerNpcId].Traces[0].Type, Is.EqualTo(MemoryType.LandmarkSpotted));
            Assert.That(world.Memory[observerNpcId].Traces[0].LastObservedTick, Is.EqualTo(20));
            Assert.That(world.Memory[otherNpcId].Traces.Count, Is.EqualTo(0));
            Assert.That(world.Beliefs[observerNpcId].Entries.Count, Is.EqualTo(0));
        }

        // =============================================================================
        // RepeatedLandmarkObservationMergesIntoSingleTrace
        // =============================================================================
        /// <summary>
        /// <para>
        /// Due osservazioni dello stesso landmark biologico non creano duplicati
        /// inutili: passano dal normale AddOrMerge del MemoryStore.
        /// </para>
        /// </summary>
        [Test]
        public void RepeatedLandmarkObservationMergesIntoSingleTrace()
        {
            World world = MakeWorldWithNpc(out int npcId);
            var events = new List<ISimEvent>
            {
                new LandmarkSpottedEvent(npcId, 202, LandmarkRegistry.LandmarkKind.BiologicalAnchor, 8, 9, 0.70f),
                new LandmarkSpottedEvent(npcId, 202, LandmarkRegistry.LandmarkKind.BiologicalAnchor, 8, 9, 0.75f)
            };

            EncodeEvents(world, events, tick: 31);

            Assert.That(world.Memory[npcId].Traces.Count, Is.EqualTo(1));
            Assert.That(world.Memory[npcId].Traces[0].SubjectId, Is.EqualTo(202));
            Assert.That(world.Memory[npcId].Traces[0].Reliability01, Is.EqualTo(0.75f).Within(0.0001f));
        }

        // =============================================================================
        // BeliefUpdaterIgnoresLandmarkSpottedTraceInStepJ
        // =============================================================================
        /// <summary>
        /// <para>
        /// Lo step J produce memoria, non belief. Le belief biologiche potenziali e
        /// osservate arriveranno negli step L/M con regole esplicite.
        /// </para>
        /// </summary>
        [Test]
        public void BeliefUpdaterIgnoresLandmarkSpottedTraceInStepJ()
        {
            var updater = new BeliefUpdater();
            var store = new BeliefStore();
            var trace = new MemoryTrace
            {
                Type = MemoryType.LandmarkSpotted,
                SubjectId = 303,
                SecondarySubjectId = (int)LandmarkRegistry.LandmarkKind.BiologicalAnchor,
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

        private static World MakeWorldWithNpc(out int npcId)
        {
            var world = new World(new WorldConfig(new SimulationParams()));
            npcId = world.CreateNpc(
                NpcDnaProfile.CreateDefault("lm_memory_observer"),
                NpcNeeds.Make(0.80f, 0.20f),
                new Social(),
                1,
                1);
            return world;
        }

        private static World MakeWorldWithTwoNpcs(out int observerNpcId, out int otherNpcId)
        {
            World world = MakeWorldWithNpc(out observerNpcId);
            otherNpcId = world.CreateNpc(
                NpcDnaProfile.CreateDefault("lm_memory_other"),
                NpcNeeds.Make(0.80f, 0.20f),
                new Social(),
                2,
                1);
            return world;
        }

        private static void EncodeEvents(World world, List<ISimEvent> events, int tick)
        {
            var memoryEncoding = new MemoryEncodingSystem();
            memoryEncoding.SetEventsBuffer(events);
            memoryEncoding.Update(world, new Tick(tick, 1f), new MessageBus(), new Telemetry());
        }
    }
}
