namespace Arcontio.Core
{
    // =============================================================================
    // LandmarkSpottedMemoryRule
    // =============================================================================
    /// <summary>
    /// <para>
    /// Traduce un <see cref="LandmarkSpottedEvent"/> in una <see cref="MemoryTrace"/>
    /// soggettiva del tipo <see cref="MemoryType.LandmarkSpotted"/>.
    /// </para>
    ///
    /// <para><b>Principio architetturale: contratto generalizzabile, scope biologico</b></para>
    /// <para>
    /// La trace e' volutamente generalizzabile: rappresenta "ho visto un landmark
    /// tipizzato". In v0.71.05.J, pero', la regola accetta solo
    /// <see cref="LandmarkRegistry.LandmarkKind.BiologicalAnchor"/>. Gli altri
    /// landmark potranno usare lo stesso contratto in step futuri senza creare una
    /// nuova famiglia di trace per ogni tipo.
    /// </para>
    ///
    /// <para><b>Struttura interna:</b></para>
    /// <list type="bullet">
    ///   <item><b>Matches</b>: riconosce gli eventi landmark observer-bound.</item>
    ///   <item><b>Filtro kind</b>: ammette solo BiologicalAnchor in questo step.</item>
    ///   <item><b>Trace</b>: usa node id come soggetto e kind come metadato numerico.</item>
    /// </list>
    /// </summary>
    public sealed class LandmarkSpottedMemoryRule : IMemoryRule
    {
        public bool Matches(ISimEvent e) => e is LandmarkSpottedEvent;

        public bool TryEncode(World world, int observerNpcId, ISimEvent e, float witnessQuality01, out MemoryTrace trace)
        {
            trace = default;

            if (e is not LandmarkSpottedEvent ev)
                return false;

            if (!world.ExistsNpc(observerNpcId))
                return false;

            if (ev.ObserverNpcId != observerNpcId)
                return false;

            if (ev.LandmarkKind != LandmarkRegistry.LandmarkKind.BiologicalAnchor)
                return false;

            float reliability = witnessQuality01;
            if (reliability < 0.05f)
                reliability = 0.05f;

            trace = new MemoryTrace
            {
                Type = MemoryType.LandmarkSpotted,
                SubjectId = ev.LandmarkNodeId,
                SecondarySubjectId = (int)ev.LandmarkKind,
                SubjectDefId = string.Empty,
                CellX = ev.CellX,
                CellY = ev.CellY,
                Intensity01 = 1.0f,
                Reliability01 = reliability,
                DecayPerTick01 = 0.001f,
                IsHeard = false,
                HeardKind = HeardKind.None,
                SourceSpeakerId = 0
            };

            return true;
        }
    }
}
