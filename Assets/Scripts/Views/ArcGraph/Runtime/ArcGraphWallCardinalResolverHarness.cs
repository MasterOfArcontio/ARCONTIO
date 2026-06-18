using System.Collections.Generic;

namespace Arcontio.View.ArcGraph
{
    // =============================================================================
    // ArcGraphWallCardinalResolverHarnessResult
    // =============================================================================
    /// <summary>
    /// <para>
    /// Risultato sintetico dello smoke test del resolver muri cardinali.
    /// </para>
    ///
    /// <para><b>Principio architetturale: QA data-only prima del renderer</b></para>
    /// <para>
    /// Il risultato contiene solo stringhe, contatori e flag. Non contiene sprite
    /// Unity, GameObject o riferimenti scena. Serve a validare che la render queue
    /// produca chiavi sprite coerenti per muri verticali, orizzontali e maschere
    /// cardinale complesse secondo la convenzione <c>N/W/S/E</c>.
    /// </para>
    ///
    /// <para><b>Struttura interna:</b></para>
    /// <list type="bullet">
    ///   <item><b>Passed</b>: esito complessivo.</item>
    ///   <item><b>Reason</b>: motivo sintetico.</item>
    ///   <item><b>ObjectItemCount</b>: numero di oggetti muro processati.</item>
    ///   <item><b>VerticalCenterSpriteKey</b>: sprite key del muro centrale verticale.</item>
    ///   <item><b>HorizontalCenterSpriteKey</b>: sprite key del muro centrale orizzontale.</item>
    ///   <item><b>ThreeWayLeftMask</b>: maschera della T sopra/sinistra/sotto.</item>
    /// </list>
    /// </summary>
    public readonly struct ArcGraphWallCardinalResolverHarnessResult
    {
        public readonly bool Passed;
        public readonly string Reason;
        public readonly int ObjectItemCount;
        public readonly string VerticalCenterSpriteKey;
        public readonly string HorizontalCenterSpriteKey;
        public readonly string ThreeWayLeftMask;

        public ArcGraphWallCardinalResolverHarnessResult(
            bool passed,
            string reason,
            int objectItemCount,
            string verticalCenterSpriteKey,
            string horizontalCenterSpriteKey,
            string threeWayLeftMask)
        {
            Passed = passed;
            Reason = string.IsNullOrWhiteSpace(reason) ? "None" : reason;
            ObjectItemCount = objectItemCount < 0 ? 0 : objectItemCount;
            VerticalCenterSpriteKey = verticalCenterSpriteKey ?? string.Empty;
            HorizontalCenterSpriteKey = horizontalCenterSpriteKey ?? string.Empty;
            ThreeWayLeftMask = threeWayLeftMask ?? string.Empty;
        }
    }

    // =============================================================================
    // ArcGraphWallCardinalResolverHarness
    // =============================================================================
    /// <summary>
    /// <para>
    /// Harness statico per validare la scelta delle varianti cardinali dei muri.
    /// </para>
    ///
    /// <para><b>Principio architetturale: resolver locale e verificabile</b></para>
    /// <para>
    /// Lo scenario costruisce un layer oggetti in memoria, lo passa alla render
    /// queue ArcGraph e controlla le sprite key risultanti. Il test non legge
    /// <c>World</c>, non usa <c>MapGrid</c>, non carica asset e non crea renderer.
    /// </para>
    ///
    /// <para><b>Struttura interna:</b></para>
    /// <list type="bullet">
    ///   <item><b>RunDefaultSmoke</b>: verifica una linea verticale, una orizzontale e una T.</item>
    ///   <item><b>CreateWallSnapshots</b>: produce sei snapshot muro.</item>
    ///   <item><b>FindSpriteKey</b>: cerca la chiave sprite risolta nella queue.</item>
    /// </list>
    /// </summary>
    public static class ArcGraphWallCardinalResolverHarness
    {
        private const int VerticalCenterObjectId = 102;
        private const int HorizontalCenterObjectId = 202;
        private const string WallBaseSpriteKey = "ArcGraph/Objects/wall_stone";

        // =============================================================================
        // RunDefaultSmoke
        // =============================================================================
        /// <summary>
        /// <para>
        /// Esegue lo smoke test default del resolver muri cardinali.
        /// </para>
        ///
        /// <para><b>Scenario minimo</b></para>
        /// <para>
        /// Tre muri in colonna devono produrre, per il muro centrale, maschera
        /// <c>1010</c>: vicino sopra e vicino sotto. Tre muri in riga devono
        /// produrre, per il muro centrale, maschera <c>0101</c>: vicino sinistra e
        /// vicino destra. Il caso T con sopra, sinistra e sotto deve produrre
        /// <c>1110</c>, secondo la convenzione <c>N/W/S/E</c>.
        /// </para>
        /// </summary>
        public static ArcGraphWallCardinalResolverHarnessResult RunDefaultSmoke()
        {
            var renderState = new ArcGraphRenderState(
                visibleZLevel: ArcGraphZLevelPolicy.DefaultVisibleZLevel,
                tileSizeWorld: 1f,
                chunkSizeCells: 4);

            var objectLayer = new ArcGraphObjectLayer();
            objectLayer.Initialize(renderState);
            objectLayer.ReplaceSnapshots(CreateWallSnapshots(), renderState);

            var config = ArcGraphMapViewConfig.CreateDefaultV033();
            var lodProfile = ArcGraphZoomLodPolicy.ResolveFromZoom(config.ResolveZoomLevel(4));
            var queue = new ArcGraphRenderQueue();
            var builder = new ArcGraphObjectRenderQueueBuilder();
            ArcGraphRenderQueueDiagnostics diagnostics = builder.Build(
                objectLayer,
                lodProfile,
                queue.MutableObjectItems);

            string verticalSpriteKey = FindSpriteKey(queue.ObjectItems, VerticalCenterObjectId);
            string horizontalSpriteKey = FindSpriteKey(queue.ObjectItems, HorizontalCenterObjectId);
            string threeWayLeftMask = ResolveThreeWayLeftMask();

            bool verticalResolved = verticalSpriteKey == WallBaseSpriteKey + "#wall_stone_1010";
            bool horizontalResolved = horizontalSpriteKey == WallBaseSpriteKey + "#wall_stone_0101_0"
                                      || horizontalSpriteKey == WallBaseSpriteKey + "#wall_stone_0101_1";
            bool threeWayLeftResolved = threeWayLeftMask == "1110";
            bool passed = diagnostics.ObjectItemCount == 6
                          && diagnostics.VisibleItemCount == 6
                          && diagnostics.HiddenItemCount == 0
                          && verticalResolved
                          && horizontalResolved
                          && threeWayLeftResolved;

            return new ArcGraphWallCardinalResolverHarnessResult(
                passed,
                passed ? "WallCardinalResolverSmokePassed" : "WallCardinalResolverSmokeFailed",
                diagnostics.ObjectItemCount,
                verticalSpriteKey,
                horizontalSpriteKey,
                threeWayLeftMask);
        }

        private static string ResolveThreeWayLeftMask()
        {
            var cells = new HashSet<ArcGraphCellCoord>
            {
                new ArcGraphCellCoord(10, 10, 0),
                new ArcGraphCellCoord(10, 11, 0),
                new ArcGraphCellCoord(9, 10, 0),
                new ArcGraphCellCoord(10, 9, 0)
            };

            return ArcGraphWallCardinalResolver.ResolveMask(
                new ArcGraphCellCoord(10, 10, 0),
                cells);
        }

        private static IEnumerable<ArcGraphObjectVisualSnapshot> CreateWallSnapshots()
        {
            yield return CreateWall(101, 1, 0);
            yield return CreateWall(VerticalCenterObjectId, 1, 1);
            yield return CreateWall(103, 1, 2);

            yield return CreateWall(201, 0, 4);
            yield return CreateWall(HorizontalCenterObjectId, 1, 4);
            yield return CreateWall(203, 2, 4);
        }

        private static ArcGraphObjectVisualSnapshot CreateWall(
            int objectId,
            int x,
            int y)
        {
            return new ArcGraphObjectVisualSnapshot(
                objectId,
                "wall_stone",
                new ArcGraphCellCoord(x, y, 0),
                WallBaseSpriteKey,
                isHeld: false,
                holderActorId: 0,
                foodStockUnits: -1,
                footprintWidth: 1,
                footprintHeight: 1,
                visualKind: "wall",
                visualResolverKey: "wall_stone_cardinal",
                visualWidthPixels: 32,
                visualHeightPixels: 83,
                visualBaseWidthPixels: 32,
                visualBaseHeightPixels: 32,
                visualBaseMiniTileMask: "0110",
                visualPivot: "bottom_center",
                visualOffsetX: 0,
                visualOffsetY: 0,
                fadeWhenActorBehind: true,
                useShadow: false);
        }

        private static string FindSpriteKey(
            IReadOnlyList<ArcGraphObjectRenderItem> items,
            int objectId)
        {
            if (items == null)
                return string.Empty;

            for (int i = 0; i < items.Count; i++)
            {
                if (items[i].ObjectId == objectId)
                    return items[i].SpriteKey;
            }

            return string.Empty;
        }
    }
}
