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
        // ResolvedDualGridOverlay
        // =============================================================================
        /// <summary>
        /// <para>
        /// Esito locale della risoluzione dual-grid per un quad visuale.
        /// </para>
        ///
        /// <para><b>Base + overlay</b></para>
        /// <para>
        /// La dual-grid del prato richiede due disegni: prima il terreno di fondo,
        /// poi il tile prato trasparente calcolato sulla finestra 2x2. Questa
        /// struttura tiene insieme i due tile senza trasformarli in nuovo stato
        /// pubblico della mappa.
        /// </para>
        /// </summary>
        private readonly struct ResolvedDualGridOverlay
        {
            public readonly int BaseTileId;
            public readonly int OverlayTileId;
            public readonly bool DrawBaseTile;
            public readonly bool UsedBaseAnimation;
            public readonly bool UsedOverlayAnimation;
            public readonly bool UsedBaseVariant;
            public readonly bool UsedBaseFallback;

            public ResolvedDualGridOverlay(
                int baseTileId,
                int overlayTileId,
                bool drawBaseTile,
                bool usedBaseAnimation,
                bool usedOverlayAnimation,
                bool usedBaseVariant,
                bool usedBaseFallback)
            {
                BaseTileId = baseTileId;
                OverlayTileId = overlayTileId;
                DrawBaseTile = drawBaseTile;
                UsedBaseAnimation = usedBaseAnimation;
                UsedOverlayAnimation = usedOverlayAnimation;
                UsedBaseVariant = usedBaseVariant;
                UsedBaseFallback = usedBaseFallback;
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

                    if (hasRuntimeCell
                        && TryResolveRuntimeDualGridOverlay(
                            runtimeCell,
                            runtimeTerrainMap,
                            visualBuildOptions,
                            visualResolver,
                            out ResolvedDualGridOverlay dualGridOverlay))
                    {
                        if (dualGridOverlay.DrawBaseTile)
                        {
                            AddTileQuad(
                                vertices,
                                uvs,
                                triangles,
                                uvMap,
                                dualGridOverlay.BaseTileId,
                                x,
                                y,
                                safeTileWorld,
                                zOffset: 0f,
                                ref usedFallbackUv,
                                ref missingUvTileCount,
                                ref firstMissingUvTileId);
                        }

                        AddTileQuad(
                            vertices,
                            uvs,
                            triangles,
                            uvMap,
                            dualGridOverlay.OverlayTileId,
                            x,
                            y,
                            safeTileWorld,
                            zOffset: -0.0001f,
                            ref usedFallbackUv,
                            ref missingUvTileCount,
                            ref firstMissingUvTileId);

                        visualResolverTileCount++;
                        visualTransitionTileCount++;
                        if ((dualGridOverlay.DrawBaseTile && dualGridOverlay.UsedBaseAnimation)
                            || dualGridOverlay.UsedOverlayAnimation)
                        {
                            visualAnimationTileCount++;
                        }

                        if (dualGridOverlay.DrawBaseTile && dualGridOverlay.UsedBaseVariant)
                            visualVariantTileCount++;

                        if (dualGridOverlay.DrawBaseTile && dualGridOverlay.UsedBaseFallback)
                            visualResolverFallbackCount++;

                        cellCount++;
                        continue;
                    }

                    ResolvedTerrainTile resolvedTile = hasRuntimeCell
                        ? ResolveTerrainTile(
                            runtimeCell,
                            runtimeTerrainMap,
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

                    AddTileQuad(
                        vertices,
                        uvs,
                        triangles,
                        uvMap,
                        tileId,
                        x,
                        y,
                        safeTileWorld,
                        zOffset: 0f,
                        ref usedFallbackUv,
                        ref missingUvTileCount,
                        ref firstMissingUvTileId);

                    if (hasRuntimeCell
                        && !resolvedTile.UsedTransition
                        && TryResolveRuntimeTerrainDetail(
                            runtimeCell,
                            visualBuildOptions,
                            out int detailTileId))
                    {
                        AddTileQuad(
                            vertices,
                            uvs,
                            triangles,
                            uvMap,
                            detailTileId,
                            x,
                            y,
                            safeTileWorld,
                            zOffset: -0.0002f,
                            ref usedFallbackUv,
                            ref missingUvTileCount,
                            ref firstMissingUvTileId);
                    }

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
            Vector2 uv3,
            float zOffset)
        {
            int v = vertices.Count;

            float wx = cellX * tileWorld;
            float wy = cellY * tileWorld;

            vertices.Add(new Vector3(wx, wy, zOffset));
            vertices.Add(new Vector3(wx + tileWorld, wy, zOffset));
            vertices.Add(new Vector3(wx + tileWorld, wy + tileWorld, zOffset));
            vertices.Add(new Vector3(wx, wy + tileWorld, zOffset));

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

        private static void AddTileQuad(
            List<Vector3> vertices,
            List<Vector2> uvs,
            List<int> triangles,
            ArcGraphTerrainTileUvMap uvMap,
            int tileId,
            int cellX,
            int cellY,
            float tileWorld,
            float zOffset,
            ref bool usedFallbackUv,
            ref int missingUvTileCount,
            ref int firstMissingUvTileId)
        {
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
                cellX,
                cellY,
                tileWorld,
                uv0,
                uv1,
                uv2,
                uv3,
                zOffset);
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

        // =============================================================================
        // TryResolveRuntimeTerrainDetail
        // =============================================================================
        /// <summary>
        /// <para>
        /// Prova a risolvere un dettaglio decorativo per una cella terrain runtime.
        /// </para>
        ///
        /// <para><b>Principio architetturale: atmosfera senza nuovo oggetto simulativo</b></para>
        /// <para>
        /// Il dettaglio viene disegnato come overlay dello stesso mesh terrain. Non
        /// crea GameObject, non modifica pathfinding, non blocca l'attore e non
        /// diventa parte del layer oggetti. La scelta e' data-driven dal visual
        /// catalog e deterministica per coordinata, quindi costa solo quando il
        /// chunk viene costruito o ricostruito.
        /// </para>
        /// </summary>
        private static bool TryResolveRuntimeTerrainDetail(
            ArcGraphRuntimeTerrainCell runtimeCell,
            ArcGraphTerrainVisualBuildOptions visualBuildOptions,
            out int detailTileId)
        {
            detailTileId = 0;

            if (runtimeCell.IsBlocked
                || !visualBuildOptions.UseVisualResolver
                || visualBuildOptions.VisualCatalog == null)
            {
                return false;
            }

            if (!visualBuildOptions.VisualCatalog.TryGetDefinition(
                    runtimeCell.TerrainId,
                    out ArcGraphTerrainVisualDefinition definition))
            {
                return false;
            }

            if (!definition.HasDetails)
                return false;

            int seed = HashTerrainDetail(runtimeCell.Cell, runtimeCell.TerrainId);
            return definition.TryResolveDetailTileId(seed, out detailTileId);
        }

        private static ResolvedTerrainTile ResolveTerrainTile(
            ArcGraphRuntimeTerrainCell runtimeCell,
            ArcGraphRuntimeTerrainMap runtimeTerrainMap,
            ArcGraphTerrainVisualBuildOptions visualBuildOptions,
            ArcGraphTerrainVisualResolver visualResolver)
        {
            ArcGraphTerrainVisualCache cache = runtimeCell.VisualCache;

            if (TryResolveRuntimeTransition(
                runtimeCell,
                runtimeTerrainMap,
                visualBuildOptions,
                visualResolver,
                out ResolvedTerrainTile transitionTile))
            {
                return transitionTile;
            }

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

        // =============================================================================
        // TryResolveRuntimeDualGridOverlay
        // =============================================================================
        /// <summary>
        /// <para>
        /// Prova a risolvere un overlay dual-grid sulla finestra 2x2 che parte
        /// dalla cella corrente.
        /// </para>
        ///
        /// <para><b>Convenzione maschera ARCONTIO</b></para>
        /// <para>
        /// Le quattro cifre sono lette nell'ordine definito dall'operatore:
        /// alto sinistra, alto destra, basso sinistra, basso destra. Il valore
        /// <c>1</c> indica il terrain type dell'overlay, oggi il prato. Il valore
        /// <c>0</c> indica qualunque altro terreno sottostante.
        /// </para>
        /// </summary>
        private static bool TryResolveRuntimeDualGridOverlay(
            ArcGraphRuntimeTerrainCell runtimeCell,
            ArcGraphRuntimeTerrainMap runtimeTerrainMap,
            ArcGraphTerrainVisualBuildOptions visualBuildOptions,
            ArcGraphTerrainVisualResolver visualResolver,
            out ResolvedDualGridOverlay resolvedOverlay)
        {
            resolvedOverlay = default;

            if (runtimeTerrainMap == null
                || runtimeCell.IsBlocked
                || !visualBuildOptions.UseVisualResolver
                || visualBuildOptions.VisualCatalog == null
                || visualResolver == null
                || visualBuildOptions.VisualCatalog.DualGridOverlayCount == 0)
            {
                return false;
            }

            for (int i = 0; i < visualBuildOptions.VisualCatalog.DualGridOverlays.Count; i++)
            {
                ArcGraphTerrainVisualDualGridOverlay overlay = visualBuildOptions.VisualCatalog.DualGridOverlays[i];
                if (overlay == null || overlay.RuleCount == 0)
                    continue;

                bool topLeftIsOverlay = IsDualGridOverlayTerrain(
                    runtimeTerrainMap,
                    runtimeCell.Cell.X,
                    runtimeCell.Cell.Y + 1,
                    runtimeCell.Cell.Z,
                    overlay);
                bool topRightIsOverlay = IsDualGridOverlayTerrain(
                    runtimeTerrainMap,
                    runtimeCell.Cell.X + 1,
                    runtimeCell.Cell.Y + 1,
                    runtimeCell.Cell.Z,
                    overlay);
                bool bottomLeftIsOverlay = IsDualGridOverlayTerrain(
                    runtimeTerrainMap,
                    runtimeCell.Cell.X,
                    runtimeCell.Cell.Y,
                    runtimeCell.Cell.Z,
                    overlay);
                bool bottomRightIsOverlay = IsDualGridOverlayTerrain(
                    runtimeTerrainMap,
                    runtimeCell.Cell.X + 1,
                    runtimeCell.Cell.Y,
                    runtimeCell.Cell.Z,
                    overlay);

                if (!topLeftIsOverlay
                    && !topRightIsOverlay
                    && !bottomLeftIsOverlay
                    && !bottomRightIsOverlay)
                {
                    continue;
                }

                if (topLeftIsOverlay
                    && topRightIsOverlay
                    && bottomLeftIsOverlay
                    && bottomRightIsOverlay)
                {
                    continue;
                }

                string mask = BuildDualGridMask(
                    topLeftIsOverlay,
                    topRightIsOverlay,
                    bottomLeftIsOverlay,
                    bottomRightIsOverlay);

                if (!overlay.TryResolveTileId(
                        mask,
                        visualBuildOptions.VisualTimeSeconds,
                        out int overlayTileId,
                        out bool usedOverlayAnimation))
                {
                    continue;
                }

                ResolvedTerrainTile baseTile = ResolveDualGridBaseTile(
                    runtimeCell,
                    runtimeTerrainMap,
                    overlay,
                    visualBuildOptions,
                    visualResolver);

                resolvedOverlay = new ResolvedDualGridOverlay(
                    baseTile.TileId,
                    overlayTileId,
                    drawBaseTile: true,
                    baseTile.UsedAnimation,
                    usedOverlayAnimation,
                    baseTile.UsedVariant,
                    baseTile.UsedVisualFallback);
                return true;
            }

            return false;
        }

        private static bool IsDualGridOverlayTerrain(
            ArcGraphRuntimeTerrainMap runtimeTerrainMap,
            int x,
            int y,
            int z,
            ArcGraphTerrainVisualDualGridOverlay overlay)
        {
            var coord = new ArcGraphCellCoord(x, y, z);
            return runtimeTerrainMap.TryGetCell(coord, out ArcGraphRuntimeTerrainCell cell)
                   && !cell.IsBlocked
                   && overlay.IsOverlayTerrain(cell.TerrainId);
        }

        private static string BuildDualGridMask(
            bool topLeftIsOverlay,
            bool topRightIsOverlay,
            bool bottomLeftIsOverlay,
            bool bottomRightIsOverlay)
        {
            return (topLeftIsOverlay ? "1" : "0")
                   + (topRightIsOverlay ? "1" : "0")
                   + (bottomLeftIsOverlay ? "1" : "0")
                   + (bottomRightIsOverlay ? "1" : "0");
        }

        private static ResolvedTerrainTile ResolveDualGridBaseTile(
            ArcGraphRuntimeTerrainCell runtimeCell,
            ArcGraphRuntimeTerrainMap runtimeTerrainMap,
            ArcGraphTerrainVisualDualGridOverlay overlay,
            ArcGraphTerrainVisualBuildOptions visualBuildOptions,
            ArcGraphTerrainVisualResolver visualResolver)
        {
            if (TryGetDualGridBaseCell(
                    runtimeCell,
                    runtimeTerrainMap,
                    overlay,
                    out ArcGraphRuntimeTerrainCell baseCell))
            {
                return ResolveTerrainTileWithoutTransitions(
                    baseCell,
                    visualBuildOptions,
                    visualResolver);
            }

            return ResolveTerrainTileWithoutTransitions(
                runtimeCell,
                visualBuildOptions,
                visualResolver);
        }

        private static bool TryGetDualGridBaseCell(
            ArcGraphRuntimeTerrainCell runtimeCell,
            ArcGraphRuntimeTerrainMap runtimeTerrainMap,
            ArcGraphTerrainVisualDualGridOverlay overlay,
            out ArcGraphRuntimeTerrainCell baseCell)
        {
            if (TryGetNonOverlayCell(runtimeTerrainMap, runtimeCell.Cell.X, runtimeCell.Cell.Y, runtimeCell.Cell.Z, overlay, out baseCell))
                return true;

            if (TryGetNonOverlayCell(runtimeTerrainMap, runtimeCell.Cell.X + 1, runtimeCell.Cell.Y, runtimeCell.Cell.Z, overlay, out baseCell))
                return true;

            if (TryGetNonOverlayCell(runtimeTerrainMap, runtimeCell.Cell.X, runtimeCell.Cell.Y + 1, runtimeCell.Cell.Z, overlay, out baseCell))
                return true;

            return TryGetNonOverlayCell(runtimeTerrainMap, runtimeCell.Cell.X + 1, runtimeCell.Cell.Y + 1, runtimeCell.Cell.Z, overlay, out baseCell);
        }

        private static bool TryGetNonOverlayCell(
            ArcGraphRuntimeTerrainMap runtimeTerrainMap,
            int x,
            int y,
            int z,
            ArcGraphTerrainVisualDualGridOverlay overlay,
            out ArcGraphRuntimeTerrainCell baseCell)
        {
            var coord = new ArcGraphCellCoord(x, y, z);
            if (runtimeTerrainMap.TryGetCell(coord, out baseCell)
                && !baseCell.IsBlocked
                && !overlay.IsOverlayTerrain(baseCell.TerrainId))
            {
                return true;
            }

            baseCell = default;
            return false;
        }

        private static ResolvedTerrainTile ResolveTerrainTileWithoutTransitions(
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
                    usedTransition: false,
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

        // =============================================================================
        // TryResolveRuntimeTransition
        // =============================================================================
        /// <summary>
        /// <para>
        /// Prova a risolvere una transizione terrain leggendo i vicini della runtime map.
        /// </para>
        ///
        /// <para><b>Principio architetturale: autotile locale, non sistema onnisciente</b></para>
        /// <para>
        /// Il metodo non legge il World e non interroga MapGrid. Usa solo la
        /// <c>ArcGraphRuntimeTerrainMap</c> gia' derivata dal layer terrain e
        /// costruisce una maschera cardinale sintetica per i terreni confinanti.
        /// Se il catalogo visuale non contiene una regola compatibile, il chiamante
        /// torna al percorso normale: animazione, cache statica o fallback legacy.
        /// </para>
        ///
        /// <para><b>Struttura interna:</b></para>
        /// <list type="bullet">
        ///   <item><b>N/E/S/W</b>: controlla i quattro vicini cardinali.</item>
        ///   <item><b>NE/SE/SW/NW</b>: controlla anche i quattro vicini diagonali per gli spigoli.</item>
        ///   <item><b>Gruppi terrain</b>: unisce direzioni dello stesso terrain id in una maschera.</item>
        ///   <item><b>Resolver</b>: accetta solo risultati marcati come transizione.</item>
        /// </list>
        /// </summary>
        private static bool TryResolveRuntimeTransition(
            ArcGraphRuntimeTerrainCell runtimeCell,
            ArcGraphRuntimeTerrainMap runtimeTerrainMap,
            ArcGraphTerrainVisualBuildOptions visualBuildOptions,
            ArcGraphTerrainVisualResolver visualResolver,
            out ResolvedTerrainTile resolvedTile)
        {
            resolvedTile = default;

            if (runtimeCell.IsBlocked
                || runtimeTerrainMap == null
                || !visualBuildOptions.UseVisualResolver
                || visualBuildOptions.VisualCatalog == null
                || visualResolver == null)
            {
                return false;
            }

            string terrainId = runtimeCell.TerrainId;
            if (string.IsNullOrWhiteSpace(terrainId)
                || !visualBuildOptions.VisualCatalog.TryGetDefinition(terrainId, out _))
            {
                return false;
            }

            string neighborId0 = null;
            string neighborId1 = null;
            string neighborId2 = null;
            string neighborId3 = null;
            string mask0 = string.Empty;
            string mask1 = string.Empty;
            string mask2 = string.Empty;
            string mask3 = string.Empty;
            int neighborGroupCount = 0;

            AddNeighborMask(
                runtimeTerrainMap,
                runtimeCell,
                0,
                1,
                "N",
                ref neighborId0,
                ref neighborId1,
                ref neighborId2,
                ref neighborId3,
                ref mask0,
                ref mask1,
                ref mask2,
                ref mask3,
                ref neighborGroupCount);
            AddNeighborMask(
                runtimeTerrainMap,
                runtimeCell,
                1,
                0,
                "E",
                ref neighborId0,
                ref neighborId1,
                ref neighborId2,
                ref neighborId3,
                ref mask0,
                ref mask1,
                ref mask2,
                ref mask3,
                ref neighborGroupCount);
            AddNeighborMask(
                runtimeTerrainMap,
                runtimeCell,
                0,
                -1,
                "S",
                ref neighborId0,
                ref neighborId1,
                ref neighborId2,
                ref neighborId3,
                ref mask0,
                ref mask1,
                ref mask2,
                ref mask3,
                ref neighborGroupCount);
            AddNeighborMask(
                runtimeTerrainMap,
                runtimeCell,
                -1,
                0,
                "W",
                ref neighborId0,
                ref neighborId1,
                ref neighborId2,
                ref neighborId3,
                ref mask0,
                ref mask1,
                ref mask2,
                ref mask3,
                ref neighborGroupCount);

            if (TryResolveTransitionGroups(
                runtimeCell,
                visualBuildOptions,
                visualResolver,
                neighborId0,
                neighborId1,
                neighborId2,
                neighborId3,
                mask0,
                mask1,
                mask2,
                mask3,
                out resolvedTile))
            {
                return true;
            }

            neighborId0 = null;
            neighborId1 = null;
            neighborId2 = null;
            neighborId3 = null;
            mask0 = string.Empty;
            mask1 = string.Empty;
            mask2 = string.Empty;
            mask3 = string.Empty;
            neighborGroupCount = 0;

            AddNeighborMask(
                runtimeTerrainMap,
                runtimeCell,
                1,
                1,
                "NE",
                ref neighborId0,
                ref neighborId1,
                ref neighborId2,
                ref neighborId3,
                ref mask0,
                ref mask1,
                ref mask2,
                ref mask3,
                ref neighborGroupCount);
            AddNeighborMask(
                runtimeTerrainMap,
                runtimeCell,
                1,
                -1,
                "SE",
                ref neighborId0,
                ref neighborId1,
                ref neighborId2,
                ref neighborId3,
                ref mask0,
                ref mask1,
                ref mask2,
                ref mask3,
                ref neighborGroupCount);
            AddNeighborMask(
                runtimeTerrainMap,
                runtimeCell,
                -1,
                -1,
                "SW",
                ref neighborId0,
                ref neighborId1,
                ref neighborId2,
                ref neighborId3,
                ref mask0,
                ref mask1,
                ref mask2,
                ref mask3,
                ref neighborGroupCount);
            AddNeighborMask(
                runtimeTerrainMap,
                runtimeCell,
                -1,
                1,
                "NW",
                ref neighborId0,
                ref neighborId1,
                ref neighborId2,
                ref neighborId3,
                ref mask0,
                ref mask1,
                ref mask2,
                ref mask3,
                ref neighborGroupCount);

            return TryResolveTransitionGroups(
                runtimeCell,
                visualBuildOptions,
                visualResolver,
                neighborId0,
                neighborId1,
                neighborId2,
                neighborId3,
                mask0,
                mask1,
                mask2,
                mask3,
                out resolvedTile);
        }

        private static bool TryResolveTransitionGroups(
            ArcGraphRuntimeTerrainCell runtimeCell,
            ArcGraphTerrainVisualBuildOptions visualBuildOptions,
            ArcGraphTerrainVisualResolver visualResolver,
            string neighborId0,
            string neighborId1,
            string neighborId2,
            string neighborId3,
            string mask0,
            string mask1,
            string mask2,
            string mask3,
            out ResolvedTerrainTile resolvedTile)
        {
            return TryResolveTransitionGroup(
                       runtimeCell,
                       visualBuildOptions,
                       visualResolver,
                       neighborId0,
                       mask0,
                       out resolvedTile)
                   || TryResolveTransitionGroup(
                       runtimeCell,
                       visualBuildOptions,
                       visualResolver,
                       neighborId1,
                       mask1,
                       out resolvedTile)
                   || TryResolveTransitionGroup(
                       runtimeCell,
                       visualBuildOptions,
                       visualResolver,
                       neighborId2,
                       mask2,
                       out resolvedTile)
                   || TryResolveTransitionGroup(
                       runtimeCell,
                       visualBuildOptions,
                       visualResolver,
                       neighborId3,
                       mask3,
                       out resolvedTile);
        }

        private static void AddNeighborMask(
            ArcGraphRuntimeTerrainMap runtimeTerrainMap,
            ArcGraphRuntimeTerrainCell runtimeCell,
            int dx,
            int dy,
            string direction,
            ref string neighborId0,
            ref string neighborId1,
            ref string neighborId2,
            ref string neighborId3,
            ref string mask0,
            ref string mask1,
            ref string mask2,
            ref string mask3,
            ref int neighborGroupCount)
        {
            var neighborCoord = new ArcGraphCellCoord(
                runtimeCell.Cell.X + dx,
                runtimeCell.Cell.Y + dy,
                runtimeCell.Cell.Z);

            if (!runtimeTerrainMap.TryGetCell(neighborCoord, out ArcGraphRuntimeTerrainCell neighborCell))
                return;

            if (neighborCell.IsBlocked)
                return;

            string neighborTerrainId = neighborCell.TerrainId;
            if (string.IsNullOrWhiteSpace(neighborTerrainId)
                || neighborTerrainId == runtimeCell.TerrainId)
            {
                return;
            }

            AddDirectionToGroup(
                neighborTerrainId,
                direction,
                ref neighborId0,
                ref neighborId1,
                ref neighborId2,
                ref neighborId3,
                ref mask0,
                ref mask1,
                ref mask2,
                ref mask3,
                ref neighborGroupCount);
        }

        private static void AddDirectionToGroup(
            string neighborTerrainId,
            string direction,
            ref string neighborId0,
            ref string neighborId1,
            ref string neighborId2,
            ref string neighborId3,
            ref string mask0,
            ref string mask1,
            ref string mask2,
            ref string mask3,
            ref int neighborGroupCount)
        {
            if (neighborTerrainId == neighborId0)
            {
                mask0 += direction;
                return;
            }

            if (neighborTerrainId == neighborId1)
            {
                mask1 += direction;
                return;
            }

            if (neighborTerrainId == neighborId2)
            {
                mask2 += direction;
                return;
            }

            if (neighborTerrainId == neighborId3)
            {
                mask3 += direction;
                return;
            }

            if (neighborGroupCount == 0)
            {
                neighborId0 = neighborTerrainId;
                mask0 = direction;
            }
            else if (neighborGroupCount == 1)
            {
                neighborId1 = neighborTerrainId;
                mask1 = direction;
            }
            else if (neighborGroupCount == 2)
            {
                neighborId2 = neighborTerrainId;
                mask2 = direction;
            }
            else if (neighborGroupCount == 3)
            {
                neighborId3 = neighborTerrainId;
                mask3 = direction;
            }

            neighborGroupCount++;
        }

        private static bool TryResolveTransitionGroup(
            ArcGraphRuntimeTerrainCell runtimeCell,
            ArcGraphTerrainVisualBuildOptions visualBuildOptions,
            ArcGraphTerrainVisualResolver visualResolver,
            string neighborTerrainId,
            string neighborMask,
            out ResolvedTerrainTile resolvedTile)
        {
            resolvedTile = default;

            if (string.IsNullOrWhiteSpace(neighborTerrainId)
                || string.IsNullOrWhiteSpace(neighborMask))
            {
                return false;
            }

            var input = new ArcGraphTerrainVisualResolveInput(
                runtimeCell.Cell,
                runtimeCell.TerrainId,
                neighborTerrainId,
                neighborMask,
                visualBuildOptions.VisualTimeSeconds);

            ArcGraphTerrainVisualResolveResult result = visualResolver.Resolve(
                visualBuildOptions.VisualCatalog,
                input);

            if (!result.UsedTransition)
                return false;

            resolvedTile = new ResolvedTerrainTile(
                result.TileId,
                usedVisualResolver: true,
                usedLegacy: false,
                usedVariant: false,
                usedAnimation: result.UsedAnimation,
                usedTransition: true,
                usedVisualFallback: false);
            return true;
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
                uint hash = 2166136261u;
                hash ^= (uint)x + 0x9E3779B9u + (hash << 6) + (hash >> 2);
                hash ^= (uint)y + 0x9E3779B9u + (hash << 6) + (hash >> 2);
                hash ^= hash >> 16;
                hash *= 2246822519u;
                hash ^= hash >> 13;
                hash *= 3266489917u;
                hash ^= hash >> 16;
                return (int)hash;
            }
        }

        private static int HashTerrainDetail(ArcGraphCellCoord cell, string terrainId)
        {
            unchecked
            {
                uint hash = 2166136261u;
                hash ^= (uint)cell.X + 0x9E3779B9u + (hash << 6) + (hash >> 2);
                hash ^= (uint)cell.Y + 0x9E3779B9u + (hash << 6) + (hash >> 2);
                hash ^= (uint)cell.Z + 0x9E3779B9u + (hash << 6) + (hash >> 2);
                hash ^= (uint)StableStringHash(terrainId) + 0x9E3779B9u + (hash << 6) + (hash >> 2);
                hash ^= 0xA511E9B3u;
                hash ^= hash >> 16;
                hash *= 2246822519u;
                hash ^= hash >> 13;
                hash *= 3266489917u;
                hash ^= hash >> 16;
                return (int)hash;
            }
        }

        private static int StableStringHash(string value)
        {
            unchecked
            {
                uint hash = 2166136261u;
                if (!string.IsNullOrEmpty(value))
                {
                    for (int i = 0; i < value.Length; i++)
                    {
                        hash ^= value[i];
                        hash *= 16777619u;
                    }
                }

                return (int)hash;
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
