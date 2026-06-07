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
            string reason)
        {
            Chunk = chunk;
            CellCount = cellCount;
            VertexCount = vertexCount;
            TriangleIndexCount = triangleIndexCount;
            UsedFallbackUv = usedFallbackUv;
            Reason = string.IsNullOrWhiteSpace(reason) ? "None" : reason;
        }
    }
}
