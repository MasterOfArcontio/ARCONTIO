using System.Collections.Generic;

namespace Arcontio.Core
{
    // =============================================================================
    // WorldLandmarkProviderCoordinator
    // =============================================================================
    /// <summary>
    /// <para>
    /// Coordinator centrale dei provider landmark registrati nel World.
    /// </para>
    ///
    /// <para><b>Principio architetturale: punto di ingresso unico, provider isolati</b></para>
    /// <para>
    /// Il coordinator raccoglie i candidati da tutti i moduli, esegue una sola
    /// rebuild del <see cref="LandmarkRegistry"/> e consegna a ogni provider solo
    /// le resolution che portano il suo <see cref="LandmarkProviderKind"/>.
    /// </para>
    /// </summary>
    public sealed class WorldLandmarkProviderCoordinator
    {
        private readonly List<IWorldLandmarkProvider> _providers = new(4);
        private readonly List<LandmarkRegistry.ManualLandmarkCandidate> _candidates = new(64);
        private readonly List<LandmarkRegistry.ManualLandmarkCandidate> _coverageCandidates = new(64);
        private readonly List<LandmarkRegistry.ManualLandmarkResolution> _resolutions = new(64);
        private readonly List<LandmarkRegistry.ManualLandmarkResolution> _providerResolutions = new(16);

        public IReadOnlyList<IWorldLandmarkProvider> Providers => _providers;

        public bool RegisterProvider(IWorldLandmarkProvider provider)
        {
            if (provider == null || provider.ProviderKind == LandmarkProviderKind.None)
                return false;

            for (int i = 0; i < _providers.Count; i++)
            {
                if (_providers[i] != null && _providers[i].ProviderKind == provider.ProviderKind)
                    return false;
            }

            _providers.Add(provider);
            _providers.Sort((a, b) => ((byte)a.ProviderKind).CompareTo((byte)b.ProviderKind));
            return true;
        }

        public void Rebuild(World world, LandmarkRegistry registry)
        {
            _candidates.Clear();
            _coverageCandidates.Clear();
            _resolutions.Clear();

            for (int i = 0; i < _providers.Count; i++)
                _providers[i]?.BuildLandmarkCandidates(world, _candidates);

            if (registry != null)
            {
                registry.RebuildFromWorld(world, _candidates, _resolutions);
                BuildCoverageCandidates(world, registry);

                if (_coverageCandidates.Count > 0)
                {
                    for (int i = 0; i < _coverageCandidates.Count; i++)
                        _candidates.Add(_coverageCandidates[i]);

                    registry.RebuildFromWorld(world, _candidates, _resolutions);
                }
            }

            ApplyResolutionsByProvider();
        }

        private void BuildCoverageCandidates(World world, LandmarkRegistry registry)
        {
            if (world == null || registry == null)
                return;

            for (int i = 0; i < _providers.Count; i++)
            {
                if (_providers[i] is IWorldLandmarkCoverageProvider coverageProvider)
                    coverageProvider.BuildCoverageLandmarkCandidates(world, registry, _coverageCandidates);
            }
        }

        private void ApplyResolutionsByProvider()
        {
            for (int providerIndex = 0; providerIndex < _providers.Count; providerIndex++)
            {
                IWorldLandmarkProvider provider = _providers[providerIndex];
                if (provider == null)
                    continue;

                _providerResolutions.Clear();
                for (int i = 0; i < _resolutions.Count; i++)
                {
                    LandmarkRegistry.ManualLandmarkResolution resolution = _resolutions[i];
                    if (resolution.ProviderKey.Kind == provider.ProviderKind)
                        _providerResolutions.Add(resolution);
                }

                provider.ApplyLandmarkResolutions(_providerResolutions);
            }
        }
    }
}
