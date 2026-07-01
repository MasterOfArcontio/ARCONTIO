using System.Collections.Generic;
using Arcontio.Core;

namespace Arcontio.Core.Environment
{
    // =============================================================================
    // EnvironmentBiologicalLandmarkProvider
    // =============================================================================
    /// <summary>
    /// <para>
    /// Adapter landmark della Biosfera verso il coordinator World.
    /// </para>
    ///
    /// <para><b>Principio architetturale: adapter separato dallo stato biologico</b></para>
    /// <para>
    /// La Biosfera continua a decidere quali celle sono buoni anchor biologici.
    /// Questo adapter aggiunge solo la chiave provider e riconsegna le resolution,
    /// evitando che il World conosca direttamente il dettaglio area -> landmark.
    /// </para>
    /// </summary>
    public sealed class EnvironmentBiologicalLandmarkProvider : IWorldLandmarkProvider
    {
        public LandmarkProviderKind ProviderKind => LandmarkProviderKind.EnvironmentBiosphere;

        public int BuildLandmarkCandidates(
            World world,
            List<LandmarkRegistry.ManualLandmarkCandidate> outCandidates)
        {
            if (world == null || world.EnvironmentState == null || outCandidates == null)
                return 0;

            int before = outCandidates.Count;
            int added = world.EnvironmentState.BuildBiologicalLandmarkCandidates(world, outCandidates);
            for (int i = before; i < outCandidates.Count; i++)
            {
                LandmarkRegistry.ManualLandmarkCandidate candidate = outCandidates[i];
                outCandidates[i] = candidate.WithProviderKey(new LandmarkProviderKey(
                    ProviderKind,
                    candidate.OwnerId));
            }

            return added;
        }

        public void ApplyLandmarkResolutions(
            IReadOnlyList<LandmarkRegistry.ManualLandmarkResolution> resolutions)
        {
            World currentWorld = _world;
            currentWorld?.EnvironmentState?.ApplyBiologicalLandmarkResolutions(resolutions);
        }

        private readonly World _world;

        public EnvironmentBiologicalLandmarkProvider(World world)
        {
            _world = world;
        }
    }
}
