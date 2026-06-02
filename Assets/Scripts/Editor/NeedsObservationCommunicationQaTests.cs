using Arcontio.Core;
using Arcontio.Core.Config;
using Arcontio.Core.Diagnostics;
using NUnit.Framework;

namespace Arcontio.Tests
{
    // =============================================================================
    // NeedsObservationCommunicationQaTests
    // =============================================================================
    /// <summary>
    /// <para>
    /// Verifica la comunicazione minima dei fatti needs osservati: consumo di cibo
    /// e riposo nel letto possono essere raccontati a un NPC vicino come memoria
    /// sentita, senza diventare percezione diretta e senza aprire catene rumorali
    /// premature.
    /// </para>
    ///
    /// <para><b>Principio architetturale: memoria soggettiva comunicata</b></para>
    /// <para>
    /// Il test non interroga il mondo per inventare il fatto. Inserisce una
    /// <c>MemoryTrace</c> diretta nello speaker, poi lascia che la pipeline token
    /// ordinaria produca, consegni e assimili il messaggio nel listener.
    /// </para>
    ///
    /// <para><b>Struttura interna:</b></para>
    /// <list type="bullet">
    ///   <item><b>Emissione</b>: una memoria diretta produce un token needs.</item>
    ///   <item><b>Assimilazione</b>: il listener riceve una memoria sentita degradata.</item>
    ///   <item><b>Anti-rumor</b>: una memoria gia' sentita non viene riemessa.</item>
    /// </list>
    /// </summary>
    public sealed class NeedsObservationCommunicationQaTests
    {
        [Test]
        public void FoodConsumedMemoryCanBeCommunicatedAsHeardTrace()
        {
            var world = MakeAdjacentTalkingWorld(out int speakerId, out int listenerId);
            int foodId = 501;

            AddDirectTrace(
                world,
                speakerId,
                MemoryType.FoodConsumed,
                subjectId: speakerId,
                secondarySubjectId: foodId,
                x: 5,
                y: 5);

            var communication = new NpcCommunicationPipeline(contactRadius: 1, topN: 6);

            communication.ProcessAfterMemoryEncoding(world, new Tick(12, 1f), new Telemetry());

            Assert.That(
                HasHeardTrace(world, listenerId, MemoryType.FoodConsumed, speakerId, foodId, sourceSpeakerId: speakerId),
                Is.True);
        }

        [Test]
        public void BedRestedMemoryCanBeCommunicatedAsHeardTrace()
        {
            var world = MakeAdjacentTalkingWorld(out int speakerId, out int listenerId);
            int bedId = 702;

            AddDirectTrace(
                world,
                speakerId,
                MemoryType.BedRested,
                subjectId: speakerId,
                secondarySubjectId: bedId,
                x: 6,
                y: 5);

            var communication = new NpcCommunicationPipeline(contactRadius: 1, topN: 6);

            communication.ProcessAfterMemoryEncoding(world, new Tick(18, 1f), new Telemetry());

            Assert.That(
                HasHeardTrace(world, listenerId, MemoryType.BedRested, speakerId, bedId, sourceSpeakerId: speakerId),
                Is.True);
        }

        [Test]
        public void HeardNeedsTraceDoesNotEmitNewToken()
        {
            var world = MakeAdjacentTalkingWorld(out int speakerId, out int listenerId);
            var trace = new MemoryTrace
            {
                Type = MemoryType.FoodConsumed,
                SubjectId = 99,
                SecondarySubjectId = 501,
                SubjectDefId = "FoodConsumedReport",
                CellX = 5,
                CellY = 5,
                Intensity01 = 0.90f,
                Reliability01 = 0.90f,
                DecayPerTick01 = 0.0040f,
                IsHeard = true,
                HeardKind = HeardKind.DirectHeard,
                SourceSpeakerId = 77
            };
            var rule = new NeedsObservationEmissionRule();

            bool emitted = rule.TryCreateToken(
                world,
                tickIndex: 21,
                speakerNpcId: speakerId,
                listenerNpcId: listenerId,
                trace,
                out _);

            Assert.That(emitted, Is.False);
        }

        private static World MakeAdjacentTalkingWorld(out int speakerId, out int listenerId)
        {
            var world = new World(new WorldConfig(new SimulationParams()));
            world.Global.Needs = NeedsConfig.Default();
            world.Global.BeliefQuery = BeliefQueryConfig.Default();
            world.Global.MaxTokensPerEncounter = 2;
            world.Global.MaxTokensPerNpcPerDay = 10;
            world.Global.RepeatShareCooldownTicks = 0;
            world.Global.TokenDeliveryMaxRangeCells = 4;
            world.Global.TokenReliabilityFalloffPerCell = 0f;
            world.Global.TokenIntensityFalloffPerCell = 0f;
            world.Global.EnableTokenLOS = false;

            speakerId = world.CreateNpc(
                NpcDnaProfile.CreateDefault("needs_observation_speaker"),
                NpcNeeds.Make(0.20f, 0.20f),
                new Social { JusticePerception01 = 0.5f },
                5,
                5);

            listenerId = world.CreateNpc(
                NpcDnaProfile.CreateDefault("needs_observation_listener"),
                NpcNeeds.Make(0.20f, 0.20f),
                new Social { JusticePerception01 = 0.5f },
                6,
                5);

            world.NpcFacing[speakerId] = CardinalDirection.East;
            world.NpcFacing[listenerId] = CardinalDirection.West;

            return world;
        }

        private static void AddDirectTrace(
            World world,
            int npcId,
            MemoryType type,
            int subjectId,
            int secondarySubjectId,
            int x,
            int y)
        {
            world.Memory[npcId].AddOrMerge(new MemoryTrace
            {
                Type = type,
                SubjectId = subjectId,
                SecondarySubjectId = secondarySubjectId,
                SubjectDefId = type.ToString(),
                CellX = x,
                CellY = y,
                Intensity01 = 0.95f,
                Reliability01 = 0.90f,
                DecayPerTick01 = type == MemoryType.FoodConsumed ? 0.0040f : 0.0035f,
                IsHeard = false,
                HeardKind = HeardKind.None,
                SourceSpeakerId = -1
            });
        }

        private static bool HasHeardTrace(
            World world,
            int listenerId,
            MemoryType type,
            int subjectId,
            int secondarySubjectId,
            int sourceSpeakerId)
        {
            if (!world.Memory.TryGetValue(listenerId, out var store) || store == null)
                return false;

            for (int i = 0; i < store.Traces.Count; i++)
            {
                var trace = store.Traces[i];
                if (trace.Type == type
                    && trace.SubjectId == subjectId
                    && trace.SecondarySubjectId == secondarySubjectId
                    && trace.IsHeard
                    && trace.HeardKind == HeardKind.DirectHeard
                    && trace.SourceSpeakerId == sourceSpeakerId)
                {
                    return true;
                }
            }

            return false;
        }
    }
}
