using System.Collections.Generic;

namespace Arcontio.View.ArcGraph
{
    // =============================================================================
    // ArcGraphRuntimeTerrainMapBuilder
    // =============================================================================
    /// <summary>
    /// <para>
    /// Builder data-only della mappa runtime terrain ArcGraph.
    /// </para>
    ///
    /// <para><b>Principio architetturale: pre-risoluzione statica, animazione separata</b></para>
    /// <para>
    /// Il builder converte snapshot terrain in celle semantiche runtime e calcola
    /// una cache visuale stabile per i tile statici. Le celle animate vengono
    /// marcate come tali, cosi' il renderer potra' risolvere il frame visuale in
    /// base al tempo senza cambiare la semantica della cella e senza far dipendere
    /// il tick simulativo dal frame grafico.
    /// </para>
    ///
    /// <para><b>Struttura interna:</b></para>
    /// <list type="bullet">
    ///   <item><b>Build</b>: converte una sequenza di snapshot in runtime map.</item>
    ///   <item><b>BuildSnapshotIndex</b>: crea indice locale per leggere vicini legacy.</item>
    ///   <item><b>ResolveVisualCache</b>: calcola cache statica o stato animato.</item>
    ///   <item><b>ResolveLegacyTileId</b>: conserva compatibilita' con muri e floor legacy.</item>
    /// </list>
    /// </summary>
    public sealed class ArcGraphRuntimeTerrainMapBuilder
    {
        // =============================================================================
        // Build
        // =============================================================================
        /// <summary>
        /// <para>
        /// Costruisce una mappa runtime terrain da snapshot ArcGraph gia' copiati.
        /// </para>
        /// </summary>
        public ArcGraphRuntimeTerrainMap Build(
            IEnumerable<ArcGraphTerrainCellSnapshot> snapshots,
            ArcGraphTerrainVisualPolicy visualPolicy,
            ArcGraphTerrainVisualBuildOptions visualBuildOptions)
        {
            if (snapshots == null)
                return new ArcGraphRuntimeTerrainMap(null);

            Dictionary<ArcGraphCellCoord, ArcGraphTerrainCellSnapshot> snapshotIndex = BuildSnapshotIndex(snapshots);
            var cells = new List<ArcGraphRuntimeTerrainCell>(snapshotIndex.Count);
            var resolver = visualBuildOptions.UseVisualResolver
                ? new ArcGraphTerrainVisualResolver()
                : null;

            foreach (var pair in snapshotIndex)
            {
                ArcGraphTerrainCellSnapshot snapshot = pair.Value;
                string terrainId = ArcGraphTerrainTypeMapper.ResolveTemporaryTerrainId(snapshot);
                ArcGraphTerrainVisualCache visualCache = ResolveVisualCache(
                    snapshot,
                    terrainId,
                    snapshotIndex,
                    visualPolicy,
                    visualBuildOptions,
                    resolver);

                cells.Add(new ArcGraphRuntimeTerrainCell(
                    snapshot.Cell,
                    terrainId,
                    snapshot.TileId,
                    snapshot.IsBlocked,
                    movementCost: 1,
                    visualCache));
            }

            cells.Sort(CompareCells);
            return new ArcGraphRuntimeTerrainMap(cells.ToArray());
        }

        private static Dictionary<ArcGraphCellCoord, ArcGraphTerrainCellSnapshot> BuildSnapshotIndex(
            IEnumerable<ArcGraphTerrainCellSnapshot> snapshots)
        {
            var index = new Dictionary<ArcGraphCellCoord, ArcGraphTerrainCellSnapshot>();
            foreach (ArcGraphTerrainCellSnapshot snapshot in snapshots)
                index[snapshot.Cell] = snapshot;

            return index;
        }

        private static ArcGraphTerrainVisualCache ResolveVisualCache(
            ArcGraphTerrainCellSnapshot snapshot,
            string terrainId,
            Dictionary<ArcGraphCellCoord, ArcGraphTerrainCellSnapshot> snapshotIndex,
            ArcGraphTerrainVisualPolicy visualPolicy,
            ArcGraphTerrainVisualBuildOptions visualBuildOptions,
            ArcGraphTerrainVisualResolver resolver)
        {
            int legacyTileId = ResolveLegacyTileId(snapshot, snapshotIndex, visualPolicy);

            if (!visualBuildOptions.UseVisualResolver || resolver == null)
                return CreateLegacyCache(legacyTileId, "LegacyVisualPolicy");

            // Le celle bloccate restano sul legacy finche' muri, strutture verticali
            // e terrain type non saranno separati in layer distinti.
            if (snapshot.IsBlocked)
                return CreateLegacyCache(legacyTileId, "BlockedLegacyVisualPolicy");

            if (visualBuildOptions.VisualCatalog == null
                || !visualBuildOptions.VisualCatalog.TryGetDefinition(terrainId, out var definition)
                || definition == null)
            {
                return new ArcGraphTerrainVisualCache(
                    legacyTileId,
                    hasStaticTile: true,
                    hasAnimatedVisual: false,
                    usedVisualResolver: false,
                    usedVariant: false,
                    usedFallback: true,
                    reason: "VisualCatalogFallback");
            }

            if (definition.HasAnimation)
            {
                return new ArcGraphTerrainVisualCache(
                    definition.DefaultTileId,
                    hasStaticTile: false,
                    hasAnimatedVisual: true,
                    usedVisualResolver: true,
                    usedVariant: false,
                    usedFallback: false,
                    reason: "AnimatedTerrainDeferred");
            }

            var input = new ArcGraphTerrainVisualResolveInput(
                snapshot.Cell,
                terrainId,
                neighborTerrainId: null,
                neighborMask: null,
                visualTimeSeconds: 0f);

            ArcGraphTerrainVisualResolveResult result = resolver.Resolve(
                visualBuildOptions.VisualCatalog,
                input);

            return new ArcGraphTerrainVisualCache(
                result.TileId,
                hasStaticTile: true,
                hasAnimatedVisual: false,
                usedVisualResolver: true,
                usedVariant: result.UsedVariant,
                usedFallback: false,
                reason: result.Reason);
        }

        private static ArcGraphTerrainVisualCache CreateLegacyCache(
            int tileId,
            string reason)
        {
            return new ArcGraphTerrainVisualCache(
                tileId,
                hasStaticTile: true,
                hasAnimatedVisual: false,
                usedVisualResolver: false,
                usedVariant: false,
                usedFallback: false,
                reason: reason);
        }

        private static int ResolveLegacyTileId(
            ArcGraphTerrainCellSnapshot snapshot,
            Dictionary<ArcGraphCellCoord, ArcGraphTerrainCellSnapshot> snapshotIndex,
            ArcGraphTerrainVisualPolicy policy)
        {
            if (snapshot.IsBlocked)
            {
                var north = new ArcGraphCellCoord(
                    snapshot.Cell.X,
                    snapshot.Cell.Y + 1,
                    snapshot.Cell.Z);

                bool northIsFloor = snapshotIndex.TryGetValue(north, out var northSnapshot)
                                    && !northSnapshot.IsBlocked;

                return northIsFloor ? policy.WallTopTileId : policy.WallTileId;
            }

            if (!policy.UseLegacyFloorVariants || policy.FloorVariantCount <= 1)
                return snapshot.TileId;

            int hash = Hash2D(snapshot.Cell.X, snapshot.Cell.Y);
            int variant = PositiveModulo(hash, policy.FloorVariantCount);
            return policy.FloorBaseTileId + variant;
        }

        private static int CompareCells(
            ArcGraphRuntimeTerrainCell left,
            ArcGraphRuntimeTerrainCell right)
        {
            int z = left.Cell.Z.CompareTo(right.Cell.Z);
            if (z != 0)
                return z;

            int y = left.Cell.Y.CompareTo(right.Cell.Y);
            if (y != 0)
                return y;

            return left.Cell.X.CompareTo(right.Cell.X);
        }

        private static int Hash2D(int x, int y)
        {
            unchecked
            {
                int h = 17;
                h = (h * 31) + x;
                h = (h * 31) + y;
                h ^= (h << 13);
                h ^= (h >> 17);
                h ^= (h << 5);
                return h;
            }
        }

        private static int PositiveModulo(int value, int modulo)
        {
            if (modulo <= 0)
                return 0;

            int result = value % modulo;
            return result < 0 ? result + modulo : result;
        }
    }
}
