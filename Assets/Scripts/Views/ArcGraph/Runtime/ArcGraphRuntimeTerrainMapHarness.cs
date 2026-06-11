using System.Collections.Generic;

namespace Arcontio.View.ArcGraph
{
    // =============================================================================
    // ArcGraphRuntimeTerrainMapHarnessResult
    // =============================================================================
    /// <summary>
    /// <para>
    /// Risultato sintetico dello smoke test della runtime terrain map.
    /// </para>
    ///
    /// <para><b>Principio architetturale: verifica del dato prima della scena</b></para>
    /// <para>
    /// Il risultato conferma che una mappa di snapshot puo' diventare una runtime
    /// map semantica con cache visuale. Non testa Unity, mesh o materiali: testa
    /// solo il contratto dati che il renderer deve consumare.
    /// </para>
    ///
    /// <para><b>Struttura interna:</b></para>
    /// <list type="bullet">
    ///   <item><b>Passed</b>: esito globale.</item>
    ///   <item><b>RuntimeCellCount</b>: celle create nella runtime map.</item>
    ///   <item><b>DistinctStaticTileCount</b>: tile statici distinti nel prato.</item>
    ///   <item><b>StableLookup</b>: true se la stessa cella mantiene la stessa cache.</item>
    ///   <item><b>AnimatedWaterDeferred</b>: true se l'acqua resta animata e non congelata.</item>
    /// </list>
    /// </summary>
    public readonly struct ArcGraphRuntimeTerrainMapHarnessResult
    {
        public readonly bool Passed;
        public readonly int RuntimeCellCount;
        public readonly int DistinctStaticTileCount;
        public readonly bool StableLookup;
        public readonly bool AnimatedWaterDeferred;
        public readonly string Reason;

        // =============================================================================
        // ArcGraphRuntimeTerrainMapHarnessResult
        // =============================================================================
        /// <summary>
        /// <para>
        /// Costruisce un risultato immutabile dello smoke test runtime terrain.
        /// </para>
        /// </summary>
        public ArcGraphRuntimeTerrainMapHarnessResult(
            bool passed,
            int runtimeCellCount,
            int distinctStaticTileCount,
            bool stableLookup,
            bool animatedWaterDeferred,
            string reason)
        {
            Passed = passed;
            RuntimeCellCount = runtimeCellCount < 0 ? 0 : runtimeCellCount;
            DistinctStaticTileCount = distinctStaticTileCount < 0 ? 0 : distinctStaticTileCount;
            StableLookup = stableLookup;
            AnimatedWaterDeferred = animatedWaterDeferred;
            Reason = string.IsNullOrWhiteSpace(reason) ? "None" : reason;
        }
    }

    // =============================================================================
    // ArcGraphRuntimeTerrainMapHarness
    // =============================================================================
    /// <summary>
    /// <para>
    /// Harness statico per validare la runtime terrain map semantica.
    /// </para>
    ///
    /// <para><b>Principio architetturale: mappa logica distinta dalla cache sprite</b></para>
    /// <para>
    /// Il test costruisce snapshot sintetici, un catalogo visuale con varianti
    /// prato e acqua animata, poi verifica che il prato venga pre-risolto in cache
    /// statiche stabili mentre l'acqua venga lasciata come visuale animata.
    /// </para>
    ///
    /// <para><b>Struttura interna:</b></para>
    /// <list type="bullet">
    ///   <item><b>RunDefaultSmoke</b>: scenario minimo completo.</item>
    ///   <item><b>CreateSnapshots</b>: crea prato con una cella acqua.</item>
    ///   <item><b>CreateVisualCatalog</b>: catalogo con varianti statiche e animazione.</item>
    /// </list>
    /// </summary>
    public static class ArcGraphRuntimeTerrainMapHarness
    {
        // =============================================================================
        // RunDefaultSmoke
        // =============================================================================
        /// <summary>
        /// <para>
        /// Esegue lo smoke test data-only della runtime terrain map.
        /// </para>
        /// </summary>
        public static ArcGraphRuntimeTerrainMapHarnessResult RunDefaultSmoke()
        {
            var builder = new ArcGraphRuntimeTerrainMapBuilder();
            ArcGraphRuntimeTerrainMap map = builder.Build(
                CreateSnapshots(),
                ArcGraphTerrainVisualPolicy.CreateLegacyDefault(),
                ArcGraphTerrainVisualBuildOptions.CreateWithCatalog(
                    CreateVisualCatalog(),
                    visualTimeSeconds: 0f));

            int distinctStaticTiles = CountDistinctGrassStaticTiles(map);
            bool stableLookup = CheckStableLookup(map);
            bool animatedWaterDeferred = CheckAnimatedWaterDeferred(map);
            bool passed = map.CellCount == 256
                          && distinctStaticTiles > 1
                          && stableLookup
                          && animatedWaterDeferred;

            return new ArcGraphRuntimeTerrainMapHarnessResult(
                passed,
                map.CellCount,
                distinctStaticTiles,
                stableLookup,
                animatedWaterDeferred,
                passed ? "RuntimeTerrainMapSmokePassed" : "RuntimeTerrainMapSmokeFailed");
        }

        private static List<ArcGraphTerrainCellSnapshot> CreateSnapshots()
        {
            var snapshots = new List<ArcGraphTerrainCellSnapshot>();

            for (int y = 0; y < 16; y++)
            {
                for (int x = 0; x < 16; x++)
                {
                    int tileId = x == 4 && y == 4 ? 30 : 0;
                    snapshots.Add(new ArcGraphTerrainCellSnapshot(
                        new ArcGraphCellCoord(x, y, 0),
                        tileId,
                        isBlocked: false));
                }
            }

            return snapshots;
        }

        private static ArcGraphTerrainVisualCatalog CreateVisualCatalog()
        {
            return new ArcGraphTerrainVisualCatalog(
                new[]
                {
                    new ArcGraphTerrainVisualDefinition(
                        "grass",
                        defaultTileId: 0,
                        new[]
                        {
                            new ArcGraphTerrainVisualVariant(0, 70),
                            new ArcGraphTerrainVisualVariant(1, 20),
                            new ArcGraphTerrainVisualVariant(2, 10)
                        },
                        new ArcGraphTerrainVisualAnimation(null, 0f)),
                    new ArcGraphTerrainVisualDefinition(
                        "water",
                        defaultTileId: 30,
                        null,
                        new ArcGraphTerrainVisualAnimation(new[] { 30, 31, 32, 33 }, 0.25f))
                },
                new ArcGraphTerrainVisualTransitionSet[0]);
        }

        private static int CountDistinctGrassStaticTiles(ArcGraphRuntimeTerrainMap map)
        {
            var distinct = new HashSet<int>();
            for (int i = 0; i < map.Cells.Count; i++)
            {
                ArcGraphRuntimeTerrainCell cell = map.Cells[i];
                if (cell.TerrainId != "grass" || !cell.VisualCache.HasStaticTile)
                    continue;

                distinct.Add(cell.VisualCache.StaticTileId);
            }

            return distinct.Count;
        }

        private static bool CheckStableLookup(ArcGraphRuntimeTerrainMap map)
        {
            var cellCoord = new ArcGraphCellCoord(7, 7, 0);
            if (!map.TryGetCell(cellCoord, out var first))
                return false;

            if (!map.TryGetCell(cellCoord, out var second))
                return false;

            return first.VisualCache.StaticTileId == second.VisualCache.StaticTileId
                   && first.TerrainId == second.TerrainId;
        }

        private static bool CheckAnimatedWaterDeferred(ArcGraphRuntimeTerrainMap map)
        {
            if (!map.TryGetCell(new ArcGraphCellCoord(4, 4, 0), out var water))
                return false;

            return water.TerrainId == "water"
                   && water.VisualCache.HasAnimatedVisual
                   && !water.VisualCache.HasStaticTile;
        }
    }
}
