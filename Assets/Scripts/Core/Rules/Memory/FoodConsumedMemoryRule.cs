namespace Arcontio.Core
{
    // =============================================================================
    // FoodConsumedMemoryRule
    // =============================================================================
    /// <summary>
    /// <para>
    /// Trasforma un <c>FoodConsumedEvent</c> percepito in una traccia di memoria
    /// soggettiva. La regola non decide chi ha visto il consumo: questa responsabilita'
    /// resta nel <c>MemoryEncodingSystem</c>, che applica range, cono e linea di vista.
    /// </para>
    ///
    /// <para><b>Principio architetturale: evento needs -> memoria soggettiva</b></para>
    /// <para>
    /// Il consumo di cibo e' gia' avvenuto nel command. Questa rule non modifica
    /// fame, inventario, stock o belief: conserva solo il fatto percepito nella
    /// memoria dell'osservatore, rendendo ricostruibile la catena causale.
    /// </para>
    ///
    /// <para><b>Struttura interna:</b></para>
    /// <list type="bullet">
    ///   <item><b>SubjectId</b>: NPC che ha mangiato.</item>
    ///   <item><b>SecondarySubjectId</b>: id dello stock quando esiste.</item>
    ///   <item><b>SubjectDefId</b>: origine sintetica del cibo consumato.</item>
    /// </list>
    /// </summary>
    public sealed class FoodConsumedMemoryRule : IMemoryRule
    {
        public bool Matches(ISimEvent e) => e is FoodConsumedEvent;

        public bool TryEncode(World world, int observerNpcId, ISimEvent e, float witnessQuality01, out MemoryTrace trace)
        {
            trace = default;

            if (e is not FoodConsumedEvent ev)
                return false;

            if (world == null || !world.ExistsNpc(observerNpcId))
                return false;

            float quality = witnessQuality01 < 0.05f ? 0.05f : witnessQuality01;
            bool self = observerNpcId == ev.NpcId;

            trace = new MemoryTrace
            {
                Type = MemoryType.FoodConsumed,
                SubjectId = ev.NpcId,
                SecondarySubjectId = ev.FoodObjectId,
                SubjectDefId = string.IsNullOrWhiteSpace(ev.SourceKind) ? "food_consumed" : ev.SourceKind,
                CellX = ev.CellX,
                CellY = ev.CellY,
                Intensity01 = self ? 0.90f : 0.55f + 0.25f * quality,
                Reliability01 = self ? 1.00f : 0.70f + 0.30f * quality,
                DecayPerTick01 = 0.0020f,
                IsHeard = false,
                HeardKind = HeardKind.None,
                SourceSpeakerId = 0
            };

            return true;
        }
    }
}
