namespace Arcontio.View.ArcGraph
{
    // =============================================================================
    // ArcGraphTerrainChunkMeshDiagnostics
    // =============================================================================
    /// <summary>
    /// <para>
    /// Diagnostica della costruzione mesh data di un singolo chunk terrain.
    /// </para>
    ///
    /// <para><b>Principio architetturale: renderer spiegabile</b></para>
    /// <para>
    /// Il builder terrain e' passivo e non possiede UI. Questa diagnostica rende
    /// comunque verificabile cosa ha costruito: quale chunk, quante celle, quanti
    /// vertici, quanti indici triangolo e se sono state usate UV fallback.
    /// </para>
    ///
    /// <para><b>Struttura interna:</b></para>
    /// <list type="bullet">
    ///   <item><b>Chunk</b>: chunk richiesto.</item>
    ///   <item><b>CellCount</b>: celle effettivamente trasformate in quad.</item>
    ///   <item><b>VertexCount</b>: vertici prodotti.</item>
    ///   <item><b>TriangleIndexCount</b>: indici triangolo prodotti.</item>
    ///   <item><b>UsedFallbackUv</b>: true se almeno un tile non aveva UV registrate.</item>
    ///   <item><b>MissingUvTileCount</b>: quante celle sono cadute su UV fallback.</item>
    ///   <item><b>FirstMissingUvTileId</b>: primo tile id senza UV, utile per correggere catalogo.</item>
    ///   <item><b>VisualResolverTileCount</b>: celle risolte dal catalogo visuale.</item>
    ///   <item><b>LegacyVisualTileCount</b>: celle rimaste sulla policy legacy.</item>
    ///   <item><b>VisualVariantTileCount</b>: celle risolte come variante deterministica.</item>
    ///   <item><b>VisualAnimationTileCount</b>: celle risolte come frame animato.</item>
    ///   <item><b>VisualTransitionTileCount</b>: celle risolte come transizione.</item>
    ///   <item><b>VisualResolverFallbackCount</b>: celle tornate al legacy dopo fallimento catalogo.</item>
    ///   <item><b>Reason</b>: esito leggibile.</item>
    /// </list>
    /// </summary>
    public readonly struct ArcGraphTerrainChunkMeshDiagnostics
    {
        public readonly ArcGraphChunkCoord Chunk;
        public readonly int CellCount;
        public readonly int VertexCount;
        public readonly int TriangleIndexCount;
        public readonly bool UsedFallbackUv;
        public readonly int MissingUvTileCount;
        public readonly int FirstMissingUvTileId;
        public readonly int VisualResolverTileCount;
        public readonly int LegacyVisualTileCount;
        public readonly int VisualVariantTileCount;
        public readonly int VisualAnimationTileCount;
        public readonly int VisualTransitionTileCount;
        public readonly int VisualResolverFallbackCount;
        public readonly string Reason;

        public bool IsEmpty => CellCount <= 0;

        // =============================================================================
        // ArcGraphTerrainChunkMeshDiagnostics
        // =============================================================================
        /// <summary>
        /// <para>
        /// Costruisce la diagnostica chunk.
        /// </para>
        ///
        /// <para><b>Snapshot diagnostico immutabile</b></para>
        /// <para>
        /// La diagnostica contiene solo valori copiati. Non espone riferimenti a
        /// mesh, layer, mappe o world.
        /// </para>
        /// </summary>
        public ArcGraphTerrainChunkMeshDiagnostics(
            ArcGraphChunkCoord chunk,
            int cellCount,
            int vertexCount,
            int triangleIndexCount,
            bool usedFallbackUv,
            int missingUvTileCount,
            int firstMissingUvTileId,
            int visualResolverTileCount,
            int legacyVisualTileCount,
            int visualVariantTileCount,
            int visualAnimationTileCount,
            int visualTransitionTileCount,
            int visualResolverFallbackCount,
            string reason)
        {
            Chunk = chunk;
            CellCount = cellCount;
            VertexCount = vertexCount;
            TriangleIndexCount = triangleIndexCount;
            UsedFallbackUv = usedFallbackUv;
            MissingUvTileCount = missingUvTileCount < 0 ? 0 : missingUvTileCount;
            FirstMissingUvTileId = firstMissingUvTileId;
            VisualResolverTileCount = visualResolverTileCount < 0 ? 0 : visualResolverTileCount;
            LegacyVisualTileCount = legacyVisualTileCount < 0 ? 0 : legacyVisualTileCount;
            VisualVariantTileCount = visualVariantTileCount < 0 ? 0 : visualVariantTileCount;
            VisualAnimationTileCount = visualAnimationTileCount < 0 ? 0 : visualAnimationTileCount;
            VisualTransitionTileCount = visualTransitionTileCount < 0 ? 0 : visualTransitionTileCount;
            VisualResolverFallbackCount = visualResolverFallbackCount < 0 ? 0 : visualResolverFallbackCount;
            Reason = string.IsNullOrWhiteSpace(reason) ? "None" : reason;
        }
    }
}
