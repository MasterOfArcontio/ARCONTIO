namespace Arcontio.Core
{
    // =============================================================================
    // BiologicalResourceSearchFromLandmarkMemoryRule
    // =============================================================================
    /// <summary>
    /// <para>
    /// Traduce un <see cref="BiologicalResourceSearchFromLandmarkEvent"/> in una
    /// <see cref="MemoryTrace"/> soggettiva del tipo
    /// <see cref="MemoryType.ResourceSearchFromLandmark"/>.
    /// </para>
    ///
    /// <para><b>Principio architetturale: azione ricordata, belief rimandata</b></para>
    /// <para>
    /// La trace dice soltanto che l'NPC ha cercato una risorsa da un landmark
    /// biologico. Non afferma ancora che la risorsa esista, non contiene quantita'
    /// osservate e non scrive nel BeliefStore. Gli step L/M useranno contratti
    /// dedicati per trasformare memoria e hint in belief reali.
    /// </para>
    ///
    /// <para><b>Struttura interna:</b></para>
    /// <list type="bullet">
    ///   <item><b>Matches</b>: riconosce solo eventi ricerca biologica da landmark.</item>
    ///   <item><b>Filtro landmark</b>: accetta solo BiologicalAnchor.</item>
    ///   <item><b>Product hash</b>: usa hash stabile numerico per distinguere prodotti nel merge.</item>
    ///   <item><b>Trace</b>: landmark come soggetto, productKey come semantica leggibile.</item>
    /// </list>
    /// </summary>
    public sealed class BiologicalResourceSearchFromLandmarkMemoryRule : IMemoryRule
    {
        public bool Matches(ISimEvent e) => e is BiologicalResourceSearchFromLandmarkEvent;

        public bool TryEncode(World world, int observerNpcId, ISimEvent e, float witnessQuality01, out MemoryTrace trace)
        {
            trace = default;

            if (e is not BiologicalResourceSearchFromLandmarkEvent ev)
                return false;

            if (!world.ExistsNpc(observerNpcId))
                return false;

            if (ev.ActorNpcId != observerNpcId)
                return false;

            if (ev.LandmarkKind != LandmarkRegistry.LandmarkKind.BiologicalAnchor)
                return false;

            if (string.IsNullOrWhiteSpace(ev.ProductKey))
                return false;

            float reliability = witnessQuality01;
            if (reliability < 0.05f)
                reliability = 0.05f;

            trace = new MemoryTrace
            {
                Type = MemoryType.ResourceSearchFromLandmark,
                SubjectId = ev.LandmarkNodeId,

                // MemoryStore non usa SubjectDefId per l'equivalenza. Il productKey
                // deve quindi entrare anche in un campo numerico stabile, altrimenti
                // berry/acorn cercati dallo stesso LM si fonderebbero male.
                SecondarySubjectId = ComputeStableProductKeyHash(ev.ProductKey),
                SubjectDefId = ev.ProductKey,
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

        // =============================================================================
        // ComputeStableProductKeyHash
        // =============================================================================
        /// <summary>
        /// <para>
        /// Produce un hash stabile e positivo per una chiave prodotto biologica.
        /// </para>
        ///
        /// <para><b>Principio architetturale: stringhe ai bordi, identita' compatta nel Core</b></para>
        /// <para>
        /// La memoria conserva ancora il <c>ProductKey</c> leggibile, ma il merge
        /// usa un numero. Non usiamo <c>string.GetHashCode()</c> perche' non e'
        /// garantito stabile tra runtime e versioni.
        /// </para>
        /// </summary>
        public static int ComputeStableProductKeyHash(string productKey)
        {
            if (string.IsNullOrWhiteSpace(productKey))
                return 17;

            unchecked
            {
                int hash = 23;
                string normalized = productKey.Trim().ToLowerInvariant();
                for (int i = 0; i < normalized.Length; i++)
                    hash = (hash * 31) + normalized[i];

                return hash == int.MinValue ? int.MaxValue : (hash < 0 ? -hash : hash);
            }
        }
    }
}
