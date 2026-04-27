using Arcontio.Core.Diagnostics;
using System.Collections.Generic;

namespace Arcontio.Core
{
    /// <summary>
    /// Orchestrates the NPC communication stages while keeping TokenBus separate
    /// from MessageBus.
    /// </summary>
    public sealed class NpcCommunicationPipeline
    {
        private readonly TokenBus _tokenBusOut = new();
        private readonly TokenBus _tokenBusIn = new();

        private readonly TokenEmissionPipeline _emission;
        private readonly TokenDeliveryPipeline _delivery = new();
        private readonly TokenAssimilationPipeline _assimilation = new();

        private readonly List<TokenEnvelope> _tokenBuffer = new(256);

        public NpcCommunicationPipeline(int contactRadius = 1, int topN = 6)
        {
            _emission = new TokenEmissionPipeline(contactRadius, topN);
        }

        /// <summary>
        /// Publishes an already-built token through the canonical OUT path.
        /// Used by debug/scenario stimuli that intentionally bypass emission rules.
        /// </summary>
        public void PublishTokenOut(World world, TokenEnvelope env)
        {
            if (world == null) return;
            world.PublishTokenOut(_tokenBusOut, env);
        }

        /// <summary>
        /// Full communication pass after event-to-memory encoding:
        /// queued event-driven tokens, memory-driven emission, delivery, assimilation.
        /// </summary>
        public void ProcessAfterMemoryEncoding(World world, Tick tick, Telemetry telemetry)
        {
            if (world == null) return;

            world.FlushQueuedTokenOut(_tokenBusOut);
            _emission.Emit(world, tick, _tokenBusOut, telemetry);
            DeliverAndAssimilate(world, tick, telemetry);
        }

        /// <summary>
        /// Processes only tokens queued by systems after commands have emitted new facts.
        /// Does not run memory-driven emission again in the same tick.
        /// </summary>
        public int ProcessQueuedOnly(World world, Tick tick, Telemetry telemetry)
        {
            if (world == null) return 0;

            int flushed = world.FlushQueuedTokenOut(_tokenBusOut);
            if (flushed > 0)
                DeliverAndAssimilate(world, tick, telemetry);

            return flushed;
        }

        private void DeliverAndAssimilate(World world, Tick tick, Telemetry telemetry)
        {
            _delivery.Deliver(world, tick, _tokenBusOut, _tokenBusIn, telemetry);
            _assimilation.Assimilate(world, tick, _tokenBusIn, _tokenBuffer, telemetry);
        }
    }
}
