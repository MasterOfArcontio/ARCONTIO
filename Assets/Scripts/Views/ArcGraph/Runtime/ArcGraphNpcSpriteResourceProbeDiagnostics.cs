namespace Arcontio.View.ArcGraph
{
    // =============================================================================
    // ArcGraphNpcSpriteResourceProbeDiagnostics
    // =============================================================================
    /// <summary>
    /// <para>
    /// Diagnostica value-only del probe che verifica la risoluzione sprite NPC.
    /// </para>
    ///
    /// <para><b>Principio architetturale: gate asset spiegabile</b></para>
    /// <para>
    /// Il gate visuale con sprite reali non deve fallire in modo opaco. Questa
    /// struttura riassume quanti frame il catalogo dichiara, quante sprite key sono
    /// vuote, quante richieste sono state risolte dal resolver e quante risultano
    /// ancora mancanti. Non contiene riferimenti a <c>Sprite</c>, <c>GameObject</c>
    /// o altri oggetti Unity.
    /// </para>
    ///
    /// <para><b>Struttura interna:</b></para>
    /// <list type="bullet">
    ///   <item><b>Gate</b>: presenza catalogo, parse riuscito, presenza resolver.</item>
    ///   <item><b>Conteggi asset</b>: frame totali, richieste controllate, risolte e mancanti.</item>
    ///   <item><b>Conteggi catalogo</b>: parti, animazioni, direzioni e slot parte/direzione/animazione.</item>
    ///   <item><b>Sample</b>: prima sprite key mancante e primo slot incompleto, utili per correggere path o nome PNG.</item>
    ///   <item><b>Reason</b>: esito sintetico leggibile in Console.</item>
    /// </list>
    /// </summary>
    public readonly struct ArcGraphNpcSpriteResourceProbeDiagnostics
    {
        public readonly bool HasCatalogJson;
        public readonly bool CatalogParsed;
        public readonly bool HasResolver;
        public readonly int CatalogFrameCount;
        public readonly int CatalogPartCount;
        public readonly int CatalogAnimationCount;
        public readonly int CatalogDirectionCount;
        public readonly int CatalogSlotCount;
        public readonly int IncompleteCatalogSlotCount;
        public readonly int CheckedSpriteKeyCount;
        public readonly int EmptySpriteKeyCount;
        public readonly int ResolvedSpriteCount;
        public readonly int MissingSpriteCount;
        public readonly string FirstMissingSpriteKey;
        public readonly string FirstMissingPartKey;
        public readonly string FirstMissingDirectionKey;
        public readonly string FirstMissingAnimationKey;
        public readonly string FirstIncompleteCatalogSlot;
        public readonly string CatalogCoverageSummary;
        public readonly string Reason;

        // =============================================================================
        // ArcGraphNpcSpriteResourceProbeDiagnostics
        // =============================================================================
        /// <summary>
        /// <para>
        /// Costruisce una diagnostica immutabile del probe risorse sprite NPC.
        /// </para>
        /// </summary>
        public ArcGraphNpcSpriteResourceProbeDiagnostics(
            bool hasCatalogJson,
            bool catalogParsed,
            bool hasResolver,
            int catalogFrameCount,
            int catalogPartCount,
            int catalogAnimationCount,
            int catalogDirectionCount,
            int catalogSlotCount,
            int incompleteCatalogSlotCount,
            int checkedSpriteKeyCount,
            int emptySpriteKeyCount,
            int resolvedSpriteCount,
            int missingSpriteCount,
            string firstMissingSpriteKey,
            string firstMissingPartKey,
            string firstMissingDirectionKey,
            string firstMissingAnimationKey,
            string firstIncompleteCatalogSlot,
            string catalogCoverageSummary,
            string reason)
        {
            HasCatalogJson = hasCatalogJson;
            CatalogParsed = catalogParsed;
            HasResolver = hasResolver;
            CatalogFrameCount = NormalizeCount(catalogFrameCount);
            CatalogPartCount = NormalizeCount(catalogPartCount);
            CatalogAnimationCount = NormalizeCount(catalogAnimationCount);
            CatalogDirectionCount = NormalizeCount(catalogDirectionCount);
            CatalogSlotCount = NormalizeCount(catalogSlotCount);
            IncompleteCatalogSlotCount = NormalizeCount(incompleteCatalogSlotCount);
            CheckedSpriteKeyCount = NormalizeCount(checkedSpriteKeyCount);
            EmptySpriteKeyCount = NormalizeCount(emptySpriteKeyCount);
            ResolvedSpriteCount = NormalizeCount(resolvedSpriteCount);
            MissingSpriteCount = NormalizeCount(missingSpriteCount);
            FirstMissingSpriteKey = firstMissingSpriteKey ?? string.Empty;
            FirstMissingPartKey = firstMissingPartKey ?? string.Empty;
            FirstMissingDirectionKey = firstMissingDirectionKey ?? string.Empty;
            FirstMissingAnimationKey = firstMissingAnimationKey ?? string.Empty;
            FirstIncompleteCatalogSlot = firstIncompleteCatalogSlot ?? string.Empty;
            CatalogCoverageSummary = catalogCoverageSummary ?? string.Empty;
            Reason = string.IsNullOrWhiteSpace(reason) ? "None" : reason;
        }

        private static int NormalizeCount(int value)
        {
            return value < 0 ? 0 : value;
        }
    }
}
