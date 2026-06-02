namespace Arcontio.Core
{
    // =============================================================================
    // BedRestedMemoryRule
    // =============================================================================
    /// <summary>
    /// <para>
    /// Trasforma un <c>BedRestedEvent</c> percepito in una traccia di memoria
    /// soggettiva legata all'uso riuscito di un letto.
    /// </para>
    ///
    /// <para><b>Principio architetturale: osservazione senza nuova semantica sonno</b></para>
    /// <para>
    /// Il letto resta mutato esclusivamente dal command che lo usa. Questa rule non
    /// occupa, libera o rivaluta letti: registra solo che un NPC ha riposato in una
    /// certa cella, se l'osservatore era un testimone valido.
    /// </para>
    ///
    /// <para><b>Struttura interna:</b></para>
    /// <list type="bullet">
    ///   <item><b>SubjectId</b>: NPC che ha riposato.</item>
    ///   <item><b>SecondarySubjectId</b>: id del letto usato.</item>
    ///   <item><b>SubjectDefId</b>: tag diagnostico del riposo.</item>
    /// </list>
    /// </summary>
    public sealed class BedRestedMemoryRule : IMemoryRule
    {
        public bool Matches(ISimEvent e) => e is BedRestedEvent;

        public bool TryEncode(World world, int observerNpcId, ISimEvent e, float witnessQuality01, out MemoryTrace trace)
        {
            trace = default;

            if (e is not BedRestedEvent ev)
                return false;

            if (world == null || !world.ExistsNpc(observerNpcId))
                return false;

            float quality = witnessQuality01 < 0.05f ? 0.05f : witnessQuality01;
            bool self = observerNpcId == ev.NpcId;

            trace = new MemoryTrace
            {
                Type = MemoryType.BedRested,
                SubjectId = ev.NpcId,
                SecondarySubjectId = ev.BedObjectId,
                SubjectDefId = string.IsNullOrWhiteSpace(ev.ReasonTag) ? "bed_rested" : ev.ReasonTag,
                CellX = ev.CellX,
                CellY = ev.CellY,
                Intensity01 = self ? 0.80f : 0.45f + 0.20f * quality,
                Reliability01 = self ? 1.00f : 0.70f + 0.30f * quality,
                DecayPerTick01 = 0.0015f,
                IsHeard = false,
                HeardKind = HeardKind.None,
                SourceSpeakerId = 0
            };

            return true;
        }
    }
}
