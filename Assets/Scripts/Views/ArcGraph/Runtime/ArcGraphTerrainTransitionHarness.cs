using System.Collections.Generic;

namespace Arcontio.View.ArcGraph
{
    // =============================================================================
    // ArcGraphTerrainTransitionHarnessResult
    // =============================================================================
    /// <summary>
    /// <para>
    /// Risultato sintetico dello smoke test sulle transizioni terrain.
    /// </para>
    ///
    /// <para><b>Principio architetturale: autotile verificato senza scena</b></para>
    /// <para>
    /// Il risultato misura se una cella terrain riesce a trasformare il proprio
    /// vicinato in una maschera cardinale e se il mesh builder usa davvero un tile
    /// di transizione. Non crea mesh Unity in scena, non carica texture e non legge
    /// MapGrid o World.
    /// </para>
    ///
    /// <para><b>Struttura interna:</b></para>
    /// <list type="bullet">
    ///   <item><b>Passed</b>: esito globale.</item>
    ///   <item><b>TransitionTileCount</b>: celle risolte tramite transizione.</item>
    ///   <item><b>ResolverTileCount</b>: celle passate dal resolver visuale.</item>
    ///   <item><b>UsedFallbackUv</b>: true se il test ha trovato tile senza UV.</item>
    /// </list>
    /// </summary>
    public readonly struct ArcGraphTerrainTransitionHarnessResult
    {
        public readonly bool Passed;
        public readonly int TransitionTileCount;
        public readonly int ResolverTileCount;
        public readonly bool UsedFallbackUv;
        public readonly string Reason;

        // =============================================================================
        // ArcGraphTerrainTransitionHarnessResult
        // =============================================================================
        /// <summary>
        /// <para>
        /// Costruisce un risultato immutabile dello smoke test transizioni.
        /// </para>
        /// </summary>
        public ArcGraphTerrainTransitionHarnessResult(
            bool passed,
            int transitionTileCount,
            int resolverTileCount,
            bool usedFallbackUv,
            string reason)
        {
            Passed = passed;
            TransitionTileCount = transitionTileCount < 0 ? 0 : transitionTileCount;
            ResolverTileCount = resolverTileCount < 0 ? 0 : resolverTileCount;
            UsedFallbackUv = usedFallbackUv;
            Reason = string.IsNullOrWhiteSpace(reason) ? "None" : reason;
        }
    }

    // =============================================================================
    // ArcGraphTerrainTransitionHarness
    // =============================================================================
    /// <summary>
    /// <para>
    /// Harness data-only per validare le prime transizioni terrain/autotile.
    /// </para>
    ///
    /// <para><b>Principio architetturale: bordo derivato dal vicinato runtime</b></para>
    /// <para>
    /// Il test costruisce snapshot terrain sintetici, li converte in
    /// <c>ArcGraphRuntimeTerrainMap</c> e poi chiede al mesh builder di costruire un
    /// chunk. La transizione attesa nasce dal fatto che una cella <c>grass</c> ha a
    /// est una cella <c>stone_floor</c>, quindi il catalogo deve poter scegliere il
    /// tile di bordo associato alla maschera <c>E</c>.
    /// </para>
    ///
    /// <para><b>Struttura interna:</b></para>
    /// <list type="bullet">
    ///   <item><b>RunDefaultSmoke</b>: scenario minimo prato/pietra.</item>
    ///   <item><b>CreateSnapshots</b>: produce la mini-mappa test.</item>
    ///   <item><b>CreateVisualCatalog</b>: dichiara la regola grass -> stone_floor.</item>
    ///   <item><b>CreateUvMap</b>: registra solo i tile usati dal test.</item>
    /// </list>
    /// </summary>
    public static class ArcGraphTerrainTransitionHarness
    {
        // =============================================================================
        // RunDefaultSmoke
        // =============================================================================
        /// <summary>
        /// <para>
        /// Esegue lo smoke test data-only sulle transizioni terrain.
        /// </para>
        /// </summary>
        public static ArcGraphTerrainTransitionHarnessResult RunDefaultSmoke()
        {
            var renderState = new ArcGraphRenderState(
                visibleZLevel: 0,
                tileSizeWorld: 1f,
                chunkSizeCells: 2);
            var terrainLayer = new ArcGraphTerrainLayer();
            terrainLayer.Initialize(renderState);
            terrainLayer.ReplaceSnapshots(CreateSnapshots(), renderState);

            ArcGraphTerrainVisualCatalog visualCatalog = CreateVisualCatalog();
            ArcGraphTerrainVisualPolicy visualPolicy = ArcGraphTerrainVisualPolicy.CreateLegacyDefault();
            ArcGraphTerrainVisualBuildOptions visualOptions = ArcGraphTerrainVisualBuildOptions.CreateWithCatalog(
                visualCatalog,
                visualTimeSeconds: 0f);
            ArcGraphRuntimeTerrainMap runtimeMap = terrainLayer.RebuildRuntimeTerrainMap(
                visualPolicy,
                visualOptions);

            var builder = new ArcGraphTerrainChunkMeshBuilder();
            ArcGraphTerrainChunkMeshData chunk = builder.BuildChunk(
                terrainLayer,
                runtimeMap,
                CreateUvMap(),
                new ArcGraphChunkCoord(0, 0, 0),
                chunkSizeCells: 2,
                tileWorld: 1f,
                visualPolicy,
                visualOptions);

            bool passed = chunk != null
                          && !chunk.IsEmpty
                          && chunk.Diagnostics.VisualTransitionTileCount >= 1
                          && chunk.Diagnostics.VisualResolverTileCount >= 1
                          && !chunk.Diagnostics.UsedFallbackUv;

            return new ArcGraphTerrainTransitionHarnessResult(
                passed,
                chunk != null ? chunk.Diagnostics.VisualTransitionTileCount : 0,
                chunk != null ? chunk.Diagnostics.VisualResolverTileCount : 0,
                chunk != null && chunk.Diagnostics.UsedFallbackUv,
                passed ? "TerrainTransitionSmokePassed" : "TerrainTransitionSmokeFailed");
        }

        private static List<ArcGraphTerrainCellSnapshot> CreateSnapshots()
        {
            return new List<ArcGraphTerrainCellSnapshot>
            {
                new(new ArcGraphCellCoord(0, 0, 0), tileId: 0, isBlocked: false),
                new(new ArcGraphCellCoord(1, 0, 0), tileId: 10, isBlocked: false),
                new(new ArcGraphCellCoord(0, 1, 0), tileId: 0, isBlocked: false),
                new(new ArcGraphCellCoord(1, 1, 0), tileId: 10, isBlocked: false)
            };
        }

        private static ArcGraphTerrainVisualCatalog CreateVisualCatalog()
        {
            return new ArcGraphTerrainVisualCatalog(
                new[]
                {
                    new ArcGraphTerrainVisualDefinition(
                        "grass",
                        defaultTileId: 0,
                        new[] { new ArcGraphTerrainVisualVariant(0, 1) },
                        new ArcGraphTerrainVisualAnimation(null, 0f)),
                    new ArcGraphTerrainVisualDefinition(
                        "stone_floor",
                        defaultTileId: 10,
                        new[] { new ArcGraphTerrainVisualVariant(10, 1) },
                        new ArcGraphTerrainVisualAnimation(null, 0f))
                },
                new[]
                {
                    new ArcGraphTerrainVisualTransitionSet(
                        "grass",
                        "stone_floor",
                        new[] { new ArcGraphTerrainVisualTransitionRule("E", 20) })
                });
        }

        private static ArcGraphTerrainTileUvMap CreateUvMap()
        {
            var uvMap = new ArcGraphTerrainTileUvMap(
                atlasWidthPixels: 128,
                atlasHeightPixels: 128,
                tilePixels: 32);
            uvMap.Register(0, 0, 0);
            uvMap.Register(10, 1, 0);
            uvMap.Register(20, 2, 0);
            return uvMap;
        }
    }
}
