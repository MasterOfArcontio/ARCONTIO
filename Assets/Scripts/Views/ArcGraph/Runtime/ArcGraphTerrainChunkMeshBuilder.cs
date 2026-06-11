using System.Collections.Generic;
using UnityEngine;

namespace Arcontio.View.ArcGraph
{
    // =============================================================================
    // ArcGraphTerrainChunkMeshBuilder
    // =============================================================================
    /// <summary>
    /// <para>
    /// Builder passivo che trasforma snapshot terrain ArcGraph in mesh data per
    /// chunk.
    /// </para>
    ///
    /// <para><b>Principio architetturale: chunk renderer senza scena</b></para>
    /// <para>
    /// Il builder legge solo <c>ArcGraphTerrainLayer</c>, una UV map e una policy
    /// visuale. Produce array mesh, ma non crea <c>GameObject</c>, non aggiunge
    /// componenti Unity, non carica asset e non legge <c>MapGridData</c>. E' il
    /// primo passo produttivo del terrain renderer, mantenuto pero' disaccoppiato
    /// dalla scena per evitare un doppio renderer permanente.
    /// </para>
    ///
    /// <para><b>Struttura interna:</b></para>
    /// <list type="bullet">
    ///   <item><b>BuildChunk</b>: costruisce mesh data per un chunk esplicito.</item>
    ///   <item><b>ResolveVisualTileId</b>: replica la policy visuale legacy.</item>
    ///   <item><b>ResolveTerrainTile</b>: usa resolver visuale opzionale con fallback legacy.</item>
    ///   <item><b>Hash2D</b>: variante floor deterministica.</item>
    /// </list>
    /// </summary>
    public sealed class ArcGraphTerrainChunkMeshBuilder
    {
        // =============================================================================
        // ResolvedTerrainTile
        // =============================================================================
        /// <summary>
        /// <para>
        /// Esito locale della scelta tile per una cella terrain.
        /// </para>
        ///
        /// <para><b>Diagnostica senza allocazioni pubbliche</b></para>
        /// <para>
        /// Questa struttura resta privata al builder. Serve solo ad accumulare
        /// contatori leggibili nel chunk senza esporre un nuovo contratto pubblico
        /// per ogni cella.
        /// </para>
        /// </summary>
        private readonly struct ResolvedTerrainTile
        {
            public readonly int TileId;
            public readonly bool UsedVisualResolver;
            public readonly bool UsedLegacy;
            public readonly bool UsedVariant;
            public readonly bool UsedAnimation;
            public readonly bool UsedTransition;
            public readonly bool UsedVisualFallback;

            public ResolvedTerrainTile(
                int tileId,
                bool usedVisualResolver,
                bool usedLegacy,
                bool usedVariant,
                bool usedAnimation,
                bool usedTransition,
                bool usedVisualFallback)
            {
                TileId = tileId;
                UsedVisualResolver = usedVisualResolver;
                UsedLegacy = usedLegacy;
                UsedVariant = usedVariant;
                UsedAnimation = usedAnimation;
                UsedTransition = usedTransition;
                UsedVisualFallback = usedVisualFallback;
            }
        }

        // =============================================================================
        // BuildDirtyChunks
        // =============================================================================
        /// <summary>
        /// <para>
        /// Costruisce mesh data terrain per tutti i chunk sporchi del render state.
        /// </para>
        ///
        /// <para><b>Rebuild localizzato</b></para>
        /// <para>
        /// Il metodo legge solo <c>ArcGraphRenderState.Dirty.DirtyChunks</c> e
        /// ricostruisce quei chunk in ordine deterministico. Non pulisce il dirty
        /// state: il cleanup resta una decisione esplicita del chiamante, cosi' un
        /// futuro renderer o layer diagnostico non perde accidentalmente il lavoro
        /// ancora da consumare.
        /// </para>
        /// </summary>
        public List<ArcGraphTerrainChunkMeshData> BuildDirtyChunks(
            ArcGraphTerrainLayer terrainLayer,
            ArcGraphTerrainTileUvMap uvMap,
            ArcGraphRenderState renderState,
            ArcGraphTerrainVisualPolicy visualPolicy,
            bool onlyVisibleZLevel = true)
        {
            return BuildDirtyChunks(
                terrainLayer,
                uvMap,
                renderState,
                visualPolicy,
                ArcGraphTerrainVisualBuildOptions.CreateLegacyOnly(),
                onlyVisibleZLevel);
        }

        // =============================================================================
        // BuildDirtyChunks
        // =============================================================================
        /// <summary>
        /// <para>
        /// Costruisce i chunk sporchi usando opzionalmente il catalogo visuale.
        /// </para>
        /// </summary>
        public List<ArcGraphTerrainChunkMeshData> BuildDirtyChunks(
            ArcGraphTerrainLayer terrainLayer,
            ArcGraphTerrainTileUvMap uvMap,
            ArcGraphRenderState renderState,
            ArcGraphTerrainVisualPolicy visualPolicy,
            ArcGraphTerrainVisualBuildOptions visualBuildOptions,
            bool onlyVisibleZLevel = true)
        {
            var results = new List<ArcGraphTerrainChunkMeshData>();

            if (renderState == null)
                return results;

            var chunks = new List<ArcGraphChunkCoord>();
            foreach (ArcGraphChunkCoord dirtyChunk in renderState.Dirty.DirtyChunks)
                chunks.Add(dirtyChunk);

            chunks.Sort(CompareChunks);

            for (int i = 0; i < chunks.Count; i++)
            {
                ArcGraphChunkCoord chunk = chunks[i];
                if (onlyVisibleZLevel && !renderState.IsChunkOnVisibleZLevel(chunk))
                    continue;

                results.Add(BuildChunk(
                    terrainLayer,
                    uvMap,
                    chunk,
                    renderState.ChunkSizeCells,
                    renderState.TileSizeWorld,
                    visualPolicy,
                    visualBuildOptions));
            }

            return results;
        }

        // =============================================================================
        // BuildChunks
        // =============================================================================
        /// <summary>
        /// <para>
        /// Costruisce mesh data terrain per una lista esplicita di chunk.
        /// </para>
        ///
        /// <para><b>Filtro viewport esterno</b></para>
        /// <para>
        /// Questo overload permette al renderer di filtrare prima i chunk sporchi
        /// secondo viewport, Z level o altre policy future, senza duplicare la
        /// costruzione mesh. Il builder resta passivo: riceve chunk gia' scelti e
        /// costruisce solo i dati mesh corrispondenti.
        /// </para>
        /// </summary>
        public List<ArcGraphTerrainChunkMeshData> BuildChunks(
            ArcGraphTerrainLayer terrainLayer,
            ArcGraphTerrainTileUvMap uvMap,
            IEnumerable<ArcGraphChunkCoord> chunks,
            int chunkSizeCells,
            float tileWorld,
            ArcGraphTerrainVisualPolicy visualPolicy)
        {
            return BuildChunks(
                terrainLayer,
                uvMap,
                chunks,
                chunkSizeCells,
                tileWorld,
                visualPolicy,
                ArcGraphTerrainVisualBuildOptions.CreateLegacyOnly());
        }

        // =============================================================================
        // BuildChunks
        // =============================================================================
        /// <summary>
        /// <para>
        /// Costruisce mesh data terrain per chunk filtrati usando catalogo opzionale.
        /// </para>
        /// </summary>
        public List<ArcGraphTerrainChunkMeshData> BuildChunks(
            ArcGraphTerrainLayer terrainLayer,
            ArcGraphTerrainTileUvMap uvMap,
            IEnumerable<ArcGraphChunkCoord> chunks,
            int chunkSizeCells,
            float tileWorld,
            ArcGraphTerrainVisualPolicy visualPolicy,
            ArcGraphTerrainVisualBuildOptions visualBuildOptions)
        {
            var results = new List<ArcGraphTerrainChunkMeshData>();

            if (chunks == null)
                return results;

            foreach (ArcGraphChunkCoord chunk in chunks)
            {
                results.Add(BuildChunk(
                    terrainLayer,
                    null,
                    uvMap,
                    chunk,
                    chunkSizeCells,
                    tileWorld,
                    visualPolicy,
                    visualBuildOptions));
            }

            return results;
        }

        // =============================================================================
        // BuildChunks
        // =============================================================================
        /// <summary>
        /// <para>
        /// Costruisce mesh data terrain usando una runtime map semantica/cache se disponibile.
        /// </para>
        ///
        /// <para><b>Cache statica prima del resolver</b></para>
        /// <para>
        /// Quando <c>runtimeTerrainMap</c> contiene la cella richiesta, il builder
        /// legge il tile statico gia' risolto invece di ricalcolare varianti. Questo
        /// mantiene il rendering leggero e rende esplicito che la varieta' visuale
        /// dei tile statici viene prodotta nel passaggio di preparazione dati.
        /// </para>
        /// </summary>
        public List<ArcGraphTerrainChunkMeshData> BuildChunks(
            ArcGraphTerrainLayer terrainLayer,
            ArcGraphRuntimeTerrainMap runtimeTerrainMap,
            ArcGraphTerrainTileUvMap uvMap,
            IEnumerable<ArcGraphChunkCoord> chunks,
            int chunkSizeCells,
            float tileWorld,
            ArcGraphTerrainVisualPolicy visualPolicy,
            ArcGraphTerrainVisualBuildOptions visualBuildOptions)
        {
            var results = new List<ArcGraphTerrainChunkMeshData>();

            if (chunks == null)
                return results;

            foreach (ArcGraphChunkCoord chunk in chunks)
            {
                results.Add(BuildChunk(
                    terrainLayer,
                    runtimeTerrainMap,
                    uvMap,
                    chunk,
                    chunkSizeCells,
                    tileWorld,
                    visualPolicy,
                    visualBuildOptions));
            }

            return results;
        }

        // =============================================================================
        // BuildChunk
        // =============================================================================
        /// <summary>
        /// <para>
        /// Costruisce i dati mesh terrain per un singolo chunk.
        /// </para>
        ///
        /// <para><b>Input snapshot-only</b></para>
        /// <para>
        /// Il metodo interroga solo il layer terrain ArcGraph. Celle mancanti vengono
        /// saltate, cosi' i chunk ai bordi o i context parziali non causano errori
        /// distruttivi. Ogni cella presente diventa un quad.
        /// </para>
        /// </summary>
        public ArcGraphTerrainChunkMeshData BuildChunk(
            ArcGraphTerrainLayer terrainLayer,
            ArcGraphTerrainTileUvMap uvMap,
            ArcGraphChunkCoord chunk,
            int chunkSizeCells,
            float tileWorld,
            ArcGraphTerrainVisualPolicy visualPolicy)
        {
            return BuildChunk(
                terrainLayer,
                uvMap,
                chunk,
                chunkSizeCells,
                tileWorld,
                visualPolicy,
                ArcGraphTerrainVisualBuildOptions.CreateLegacyOnly());
        }

        // =============================================================================
        // BuildChunk
        // =============================================================================
        /// <summary>
        /// <para>
        /// Costruisce un chunk usando resolver visuale opzionale e fallback legacy.
        /// </para>
        /// </summary>
        public ArcGraphTerrainChunkMeshData BuildChunk(
            ArcGraphTerrainLayer terrainLayer,
            ArcGraphTerrainTileUvMap uvMap,
            ArcGraphChunkCoord chunk,
            int chunkSizeCells,
            float tileWorld,
            ArcGraphTerrainVisualPolicy visualPolicy,
            ArcGraphTerrainVisualBuildOptions visualBuildOptions)
        {
            return BuildChunk(
                terrainLayer,
                null,
                uvMap,
                chunk,
                chunkSizeCells,
                tileWorld,
                visualPolicy,
                visualBuildOptions);
        }

        // =============================================================================
        // BuildChunk
        // =============================================================================
        /// <summary>
        /// <para>
        /// Costruisce un chunk usando runtime terrain map quando disponibile.
        /// </para>
        /// </summary>
        public ArcGraphTerrainChunkMeshData BuildChunk(
            ArcGraphTerrainLayer terrainLayer,
            ArcGraphRuntimeTerrainMap runtimeTerrainMap,
            ArcGraphTerrainTileUvMap uvMap,
            ArcGraphChunkCoord chunk,
            int chunkSizeCells,
            float tileWorld,
            ArcGraphTerrainVisualPolicy visualPolicy,
            ArcGraphTerrainVisualBuildOptions visualBuildOptions)
        {
            if (terrainLayer == null)
                return ArcGraphTerrainChunkMeshData.Empty(chunk, "TerrainLayerMissing");

            if (uvMap == null)
                return ArcGraphTerrainChunkMeshData.Empty(chunk, "UvMapMissing");

            int safeChunkSize = chunkSizeCells > 0 ? chunkSizeCells : 1;
            float safeTileWorld = tileWorld > 0.0001f ? tileWorld : 1f;

            int startX = chunk.X * safeChunkSize;
            int startY = chunk.Y * safeChunkSize;
            int endX = startX + safeChunkSize;
            int endY = startY + safeChunkSize;

            var vertices = new List<Vector3>(safeChunkSize * safeChunkSize * 4);
            var uvs = new List<Vector2>(safeChunkSize * safeChunkSize * 4);
            var triangles = new List<int>(safeChunkSize * safeChunkSize * 6);

            bool usedFallbackUv = false;
            int missingUvTileCount = 0;
            int firstMissingUvTileId = -1;
            int cellCount = 0;
            int visualResolverTileCount = 0;
            int legacyVisualTileCount = 0;
            int visualVariantTileCount = 0;
            int visualAnimationTileCount = 0;
            int visualTransitionTileCount = 0;
            int visualResolverFallbackCount = 0;
            ArcGraphTerrainVisualResolver visualResolver = visualBuildOptions.UseVisualResolver
                ? new ArcGraphTerrainVisualResolver()
                : null;

            for (int y = startY; y < endY; y++)
            {
                for (int x = startX; x < endX; x++)
                {
                    var cell = new ArcGraphCellCoord(x, y, chunk.Z);
                    ArcGraphRuntimeTerrainCell runtimeCell = default;
                    bool hasRuntimeCell = runtimeTerrainMap != null
                                          && runtimeTerrainMap.TryGetCell(cell, out runtimeCell);
                    bool hasSnapshot = terrainLayer.TryGetCell(cell, out var snapshot);

                    if (!hasRuntimeCell && !hasSnapshot)
                        continue;

                    ResolvedTerrainTile resolvedTile = hasRuntimeCell
                        ? ResolveTerrainTile(
                            runtimeCell,
                            visualBuildOptions,
                            visualResolver)
                        : ResolveTerrainTile(
                            terrainLayer,
                            snapshot,
                            visualPolicy,
                            visualBuildOptions,
                            visualResolver);

                    int tileId = resolvedTile.TileId;
                    if (resolvedTile.UsedVisualResolver)
                        visualResolverTileCount++;

                    if (resolvedTile.UsedLegacy)
                        legacyVisualTileCount++;

                    if (resolvedTile.UsedVariant)
                        visualVariantTileCount++;

                    if (resolvedTile.UsedAnimation)
                        visualAnimationTileCount++;

                    if (resolvedTile.UsedTransition)
                        visualTransitionTileCount++;

                    if (resolvedTile.UsedVisualFallback)
                        visualResolverFallbackCount++;

                    bool foundUv = uvMap.TryGetUvQuad(tileId, out var uv0, out var uv1, out var uv2, out var uv3);
                    if (!foundUv)
                    {
                        usedFallbackUv = true;
                        missingUvTileCount++;
                        if (firstMissingUvTileId < 0)
                            firstMissingUvTileId = tileId;
                    }

                    AddQuad(
                        vertices,
                        uvs,
                        triangles,
                        x,
                        y,
                        safeTileWorld,
                        uv0,
                        uv1,
                        uv2,
                        uv3);

                    cellCount++;
                }
            }

            string reason = cellCount > 0 ? "ChunkMeshBuilt" : "ChunkHasNoTerrainSnapshots";
            var diagnostics = new ArcGraphTerrainChunkMeshDiagnostics(
                chunk,
                cellCount,
                vertices.Count,
                triangles.Count,
                usedFallbackUv,
                missingUvTileCount,
                firstMissingUvTileId,
                visualResolverTileCount,
                legacyVisualTileCount,
                visualVariantTileCount,
                visualAnimationTileCount,
                visualTransitionTileCount,
                visualResolverFallbackCount,
                reason);

            return new ArcGraphTerrainChunkMeshData(
                vertices.ToArray(),
                uvs.ToArray(),
                triangles.ToArray(),
                diagnostics);
        }

        private static void AddQuad(
            List<Vector3> vertices,
            List<Vector2> uvs,
            List<int> triangles,
            int cellX,
            int cellY,
            float tileWorld,
            Vector2 uv0,
            Vector2 uv1,
            Vector2 uv2,
            Vector2 uv3)
        {
            int v = vertices.Count;

            float wx = cellX * tileWorld;
            float wy = cellY * tileWorld;

            vertices.Add(new Vector3(wx, wy, 0f));
            vertices.Add(new Vector3(wx + tileWorld, wy, 0f));
            vertices.Add(new Vector3(wx + tileWorld, wy + tileWorld, 0f));
            vertices.Add(new Vector3(wx, wy + tileWorld, 0f));

            uvs.Add(uv0);
            uvs.Add(uv1);
            uvs.Add(uv2);
            uvs.Add(uv3);

            triangles.Add(v + 0);
            triangles.Add(v + 2);
            triangles.Add(v + 1);

            triangles.Add(v + 0);
            triangles.Add(v + 3);
            triangles.Add(v + 2);
        }

        private static int ResolveVisualTileId(
            ArcGraphTerrainLayer terrainLayer,
            ArcGraphTerrainCellSnapshot snapshot,
            ArcGraphTerrainVisualPolicy policy)
        {
            if (snapshot.IsBlocked)
            {
                var north = new ArcGraphCellCoord(
                    snapshot.Cell.X,
                    snapshot.Cell.Y + 1,
                    snapshot.Cell.Z);

                bool northIsFloor = terrainLayer.TryGetCell(north, out var northSnapshot)
                                    && !northSnapshot.IsBlocked;

                return northIsFloor ? policy.WallTopTileId : policy.WallTileId;
            }

            if (!policy.UseLegacyFloorVariants || policy.FloorVariantCount <= 1)
                return snapshot.TileId;

            int hash = Hash2D(snapshot.Cell.X, snapshot.Cell.Y);
            int variant = Mathf.Abs(hash) % policy.FloorVariantCount;
            return policy.FloorBaseTileId + variant;
        }

        private static ResolvedTerrainTile ResolveTerrainTile(
            ArcGraphRuntimeTerrainCell runtimeCell,
            ArcGraphTerrainVisualBuildOptions visualBuildOptions,
            ArcGraphTerrainVisualResolver visualResolver)
        {
            ArcGraphTerrainVisualCache cache = runtimeCell.VisualCache;

            if (cache.HasAnimatedVisual
                && visualBuildOptions.UseVisualResolver
                && visualBuildOptions.VisualCatalog != null
                && visualResolver != null)
            {
                var input = new ArcGraphTerrainVisualResolveInput(
                    runtimeCell.Cell,
                    runtimeCell.TerrainId,
                    neighborTerrainId: null,
                    neighborMask: null,
                    visualBuildOptions.VisualTimeSeconds);

                ArcGraphTerrainVisualResolveResult result = visualResolver.Resolve(
                    visualBuildOptions.VisualCatalog,
                    input);

                return new ResolvedTerrainTile(
                    result.TileId,
                    usedVisualResolver: true,
                    usedLegacy: false,
                    usedVariant: false,
                    usedAnimation: result.UsedAnimation,
                    usedTransition: result.UsedTransition,
                    usedVisualFallback: false);
            }

            if (cache.HasStaticTile)
            {
                return new ResolvedTerrainTile(
                    cache.StaticTileId,
                    cache.UsedVisualResolver,
                    usedLegacy: !cache.UsedVisualResolver,
                    cache.UsedVariant,
                    usedAnimation: false,
                    usedTransition: false,
                    cache.UsedFallback);
            }

            return new ResolvedTerrainTile(
                runtimeCell.SourceTileId,
                usedVisualResolver: false,
                usedLegacy: true,
                usedVariant: false,
                usedAnimation: false,
                usedTransition: false,
                usedVisualFallback: true);
        }

        private static ResolvedTerrainTile ResolveTerrainTile(
            ArcGraphTerrainLayer terrainLayer,
            ArcGraphTerrainCellSnapshot snapshot,
            ArcGraphTerrainVisualPolicy policy,
            ArcGraphTerrainVisualBuildOptions visualBuildOptions,
            ArcGraphTerrainVisualResolver visualResolver)
        {
            int legacyTileId = ResolveVisualTileId(terrainLayer, snapshot, policy);

            if (!visualBuildOptions.UseVisualResolver)
                return new ResolvedTerrainTile(legacyTileId, false, true, false, false, false, false);

            // Le celle bloccate rappresentano ancora muri/strutture legacy. In
            // questo checkpoint non le trasformiamo in terrain type, per non
            // confondere pavimento e oggetti verticali.
            if (snapshot.IsBlocked)
                return new ResolvedTerrainTile(legacyTileId, false, true, false, false, false, false);

            string terrainId = ArcGraphTerrainTypeMapper.ResolveTemporaryTerrainId(snapshot);
            if (string.IsNullOrWhiteSpace(terrainId)
                || visualBuildOptions.VisualCatalog == null
                || visualResolver == null
                || !visualBuildOptions.VisualCatalog.TryGetDefinition(terrainId, out _))
            {
                return new ResolvedTerrainTile(legacyTileId, false, true, false, false, false, true);
            }

            var input = new ArcGraphTerrainVisualResolveInput(
                snapshot.Cell,
                terrainId,
                neighborTerrainId: null,
                neighborMask: null,
                visualBuildOptions.VisualTimeSeconds);

            ArcGraphTerrainVisualResolveResult result = visualResolver.Resolve(
                visualBuildOptions.VisualCatalog,
                input);

            return new ResolvedTerrainTile(
                result.TileId,
                usedVisualResolver: true,
                usedLegacy: false,
                usedVariant: result.UsedVariant,
                usedAnimation: result.UsedAnimation,
                usedTransition: result.UsedTransition,
                usedVisualFallback: false);
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

        private static int CompareChunks(ArcGraphChunkCoord left, ArcGraphChunkCoord right)
        {
            int z = left.Z.CompareTo(right.Z);
            if (z != 0)
                return z;

            int y = left.Y.CompareTo(right.Y);
            if (y != 0)
                return y;

            return left.X.CompareTo(right.X);
        }
    }
}
