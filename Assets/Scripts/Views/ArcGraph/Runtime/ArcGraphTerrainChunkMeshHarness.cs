using System.Collections.Generic;

namespace Arcontio.View.ArcGraph
{
    // =============================================================================
    // ArcGraphTerrainChunkMeshHarnessResult
    // =============================================================================
    /// <summary>
    /// <para>
    /// Risultato sintetico dell'harness controllato del terrain chunk builder.
    /// </para>
    ///
    /// <para><b>Principio architetturale: verifica senza scena produttiva</b></para>
    /// <para>
    /// Il risultato non contiene riferimenti a <c>GameObject</c>, mesh Unity o
    /// asset. Riporta solo contatori e ragioni leggibili, sufficienti per capire se
    /// la costruzione mesh data da snapshot terrain funziona.
    /// </para>
    ///
    /// <para><b>Struttura interna:</b></para>
    /// <list type="bullet">
    ///   <item><b>Passed</b>: esito booleano del controllo.</item>
    ///   <item><b>Reason</b>: motivo diagnostico.</item>
    ///   <item><b>ChunkCount</b>: numero chunk mesh prodotti.</item>
    ///   <item><b>CellCount/VertexCount/TriangleIndexCount</b>: contatori del primo chunk.</item>
    ///   <item><b>DirtyStillPresent</b>: verifica che il builder non pulisca il dirty.</item>
    /// </list>
    /// </summary>
    public readonly struct ArcGraphTerrainChunkMeshHarnessResult
    {
        public readonly bool Passed;
        public readonly string Reason;
        public readonly int ChunkCount;
        public readonly int CellCount;
        public readonly int VertexCount;
        public readonly int TriangleIndexCount;
        public readonly bool UsedFallbackUv;
        public readonly bool DirtyStillPresent;

        public ArcGraphTerrainChunkMeshHarnessResult(
            bool passed,
            string reason,
            int chunkCount,
            int cellCount,
            int vertexCount,
            int triangleIndexCount,
            bool usedFallbackUv,
            bool dirtyStillPresent)
        {
            Passed = passed;
            Reason = string.IsNullOrWhiteSpace(reason) ? "None" : reason;
            ChunkCount = chunkCount;
            CellCount = cellCount;
            VertexCount = vertexCount;
            TriangleIndexCount = triangleIndexCount;
            UsedFallbackUv = usedFallbackUv;
            DirtyStillPresent = dirtyStillPresent;
        }
    }

    // =============================================================================
    // ArcGraphTerrainChunkMeshHarness
    // =============================================================================
    /// <summary>
    /// <para>
    /// Harness statico per validare la costruzione mesh terrain da snapshot ArcGraph.
    /// </para>
    ///
    /// <para><b>Principio architetturale: test controllato, non renderer permanente</b></para>
    /// <para>
    /// L'harness costruisce tutto in memoria: render state, terrain layer, snapshot,
    /// UV map, policy visuale e builder. Non usa scene, non crea <c>GameObject</c>,
    /// non crea componenti Unity e non legge asset. Serve a verificare che il
    /// terrain renderer passivo possa produrre mesh data coerente prima della
    /// modalita' comparativa futura.
    /// </para>
    ///
    /// <para><b>Struttura interna:</b></para>
    /// <list type="bullet">
    ///   <item><b>RunTwoByTwoSmoke</b>: costruisce un chunk 2x2 e valida i contatori attesi.</item>
    ///   <item><b>CreateUvMap</b>: registra UV minime per floor/wall/wallTop.</item>
    ///   <item><b>CreateTerrainSnapshots</b>: crea quattro snapshot terrain locali.</item>
    /// </list>
    /// </summary>
    public static class ArcGraphTerrainChunkMeshHarness
    {
        // =============================================================================
        // RunTwoByTwoSmoke
        // =============================================================================
        /// <summary>
        /// <para>
        /// Esegue un controllo smoke su un chunk terrain 2x2.
        /// </para>
        ///
        /// <para><b>Verifica minima</b></para>
        /// <para>
        /// Il test atteso e' semplice: quattro celle producono sedici vertici e
        /// ventiquattro indici triangolo. Le UV sono tutte registrate, quindi non
        /// deve comparire fallback. Il dirty deve restare presente per confermare
        /// che il builder non lo pulisce automaticamente.
        /// </para>
        /// </summary>
        public static ArcGraphTerrainChunkMeshHarnessResult RunTwoByTwoSmoke()
        {
            var renderState = new ArcGraphRenderState(
                visibleZLevel: ArcGraphZLevelPolicy.DefaultVisibleZLevel,
                tileSizeWorld: 1f,
                chunkSizeCells: 2);

            var terrainLayer = new ArcGraphTerrainLayer();
            terrainLayer.Initialize(renderState);
            terrainLayer.ReplaceSnapshots(CreateTerrainSnapshots(), renderState);

            var builder = new ArcGraphTerrainChunkMeshBuilder();
            var chunks = builder.BuildDirtyChunks(
                terrainLayer,
                CreateUvMap(),
                renderState,
                ArcGraphTerrainVisualPolicy.CreateLegacyDefault());

            if (chunks.Count != 1)
                return Fail("UnexpectedChunkCount", chunks.Count, renderState);

            ArcGraphTerrainChunkMeshData first = chunks[0];
            bool passed = first.Diagnostics.CellCount == 4
                          && first.Diagnostics.VertexCount == 16
                          && first.Diagnostics.TriangleIndexCount == 24
                          && !first.Diagnostics.UsedFallbackUv
                          && renderState.Dirty.HasDirtyWork;

            return new ArcGraphTerrainChunkMeshHarnessResult(
                passed,
                passed ? "TwoByTwoSmokePassed" : "TwoByTwoSmokeFailed",
                chunks.Count,
                first.Diagnostics.CellCount,
                first.Diagnostics.VertexCount,
                first.Diagnostics.TriangleIndexCount,
                first.Diagnostics.UsedFallbackUv,
                renderState.Dirty.HasDirtyWork);
        }

        private static ArcGraphTerrainChunkMeshHarnessResult Fail(
            string reason,
            int chunkCount,
            ArcGraphRenderState renderState)
        {
            return new ArcGraphTerrainChunkMeshHarnessResult(
                false,
                reason,
                chunkCount,
                0,
                0,
                0,
                false,
                renderState != null && renderState.Dirty.HasDirtyWork);
        }

        private static ArcGraphTerrainTileUvMap CreateUvMap()
        {
            var map = new ArcGraphTerrainTileUvMap(
                atlasWidthPixels: 64,
                atlasHeightPixels: 64,
                tilePixels: 16);

            map.Register(0, 0, 0);
            map.Register(1, 1, 0);
            map.Register(2, 2, 0);
            map.Register(3, 3, 0);
            map.Register(10, 0, 1);
            map.Register(11, 1, 1);

            return map;
        }

        private static IEnumerable<ArcGraphTerrainCellSnapshot> CreateTerrainSnapshots()
        {
            yield return new ArcGraphTerrainCellSnapshot(new ArcGraphCellCoord(0, 0, 0), 0, false);
            yield return new ArcGraphTerrainCellSnapshot(new ArcGraphCellCoord(1, 0, 0), 0, false);
            yield return new ArcGraphTerrainCellSnapshot(new ArcGraphCellCoord(0, 1, 0), 10, true);
            yield return new ArcGraphTerrainCellSnapshot(new ArcGraphCellCoord(1, 1, 0), 10, true);
        }
    }
}
