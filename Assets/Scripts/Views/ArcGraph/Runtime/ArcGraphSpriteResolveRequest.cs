namespace Arcontio.View.ArcGraph
{
    // =============================================================================
    // ArcGraphSpriteResolveRequest
    // =============================================================================
    /// <summary>
    /// <para>
    /// Richiesta passiva di risoluzione sprite per un item actor/object ArcGraph.
    /// </para>
    ///
    /// <para><b>Principio architetturale: asset richiesti dal wrapper, non dal core</b></para>
    /// <para>
    /// La render queue ArcGraph produce solo sprite key. La conversione da chiave a
    /// <c>Sprite</c> Unity deve restare fuori dai builder passivi, cosi' il core
    /// ArcGraph non carica asset, non consulta <c>Resources</c> e non diventa
    /// dipendente dalla scena. Questa struttura descrive la richiesta che un futuro
    /// resolver scene-side potra' soddisfare.
    /// </para>
    ///
    /// <para><b>Struttura interna:</b></para>
    /// <list type="bullet">
    ///   <item><b>Kind</b>: tipo di item, actor o object.</item>
    ///   <item><b>EntityId</b>: id runtime utile a diagnostica e cache.</item>
    ///   <item><b>SpriteKey</b>: chiave asset prodotta da snapshot/render item.</item>
    ///   <item><b>DefId</b>: definizione oggetto, vuota per actor.</item>
    /// </list>
    /// </summary>
    public readonly struct ArcGraphSpriteResolveRequest
    {
        public readonly ArcGraphRenderItemKind Kind;
        public readonly int EntityId;
        public readonly string SpriteKey;
        public readonly string DefId;

        // =============================================================================
        // ArcGraphSpriteResolveRequest
        // =============================================================================
        /// <summary>
        /// <para>
        /// Costruisce una richiesta immutabile di risoluzione sprite.
        /// </para>
        ///
        /// <para><b>Normalizzazione difensiva</b></para>
        /// <para>
        /// Le stringhe nulle vengono convertite in stringhe vuote. Il renderer scena
        /// potra' quindi distinguere chiaramente tra chiave mancante e chiave
        /// presente ma non risolta.
        /// </para>
        /// </summary>
        public ArcGraphSpriteResolveRequest(
            ArcGraphRenderItemKind kind,
            int entityId,
            string spriteKey,
            string defId)
        {
            Kind = kind;
            EntityId = entityId;
            SpriteKey = spriteKey ?? string.Empty;
            DefId = defId ?? string.Empty;
        }
    }
}
