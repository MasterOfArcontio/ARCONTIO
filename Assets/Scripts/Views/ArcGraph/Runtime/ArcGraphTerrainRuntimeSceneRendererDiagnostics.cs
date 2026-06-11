namespace Arcontio.View.ArcGraph
{
    // =============================================================================
    // ArcGraphTerrainRuntimeSceneRendererDiagnostics
    // =============================================================================
    /// <summary>
    /// <para>
    /// Diagnostica del renderer runtime minimo del terreno ArcGraph.
    /// </para>
    ///
    /// <para><b>Principio architetturale: renderer produttivo spiegabile</b></para>
    /// <para>
    /// La diagnostica rende leggibile se il renderer era abilitato, se il contratto
    /// era valido, se runtime/layer terrain erano disponibili, quanti chunk sono
    /// stati costruiti, quanti oggetti sono stati creati o riusati e se il dirty
    /// state e' stato consumato. Non espone riferimenti a mesh, GameObject, World o
    /// MapGrid.
    /// </para>
    ///
    /// <para><b>Struttura interna:</b></para>
    /// <list type="bullet">
    ///   <item><b>Gate</b>: renderer abilitato, contratto valido, runtime valido.</item>
    ///   <item><b>Input</b>: presenza context, config, map, runtime e terrain layer.</item>
    ///   <item><b>Catalogo</b>: presenza/parse del catalogo terrain e sorgente UV usata.</item>
    ///   <item><b>Chunk</b>: chunk dirty richiesti, costruiti, non vuoti e applicati.</item>
    ///   <item><b>Pool</b>: oggetti creati, riusati, disattivati e attivi.</item>
    ///   <item><b>Reason</b>: esito sintetico del frame.</item>
    /// </list>
    /// </summary>
    public readonly struct ArcGraphTerrainRuntimeSceneRendererDiagnostics
    {
        public readonly bool RendererEnabled;
        public readonly bool HasContract;
        public readonly bool ContractSafe;
        public readonly bool HasContext;
        public readonly bool HasConfig;
        public readonly bool HasMap;
        public readonly bool HasRuntime;
        public readonly bool HasRenderState;
        public readonly bool HasTerrainLayer;
        public readonly bool HasTerrainCatalogJson;
        public readonly bool TerrainCatalogParsed;
        public readonly bool UsedCatalogUvMap;
        public readonly bool UsedLegacyConfigUvMap;
        public readonly int TerrainCatalogEntryCount;
        public readonly bool DidBuildChunks;
        public readonly bool DidClearDirty;
        public readonly int DirtyChunkCountBeforeBuild;
        public readonly int BuiltChunkCount;
        public readonly int NonEmptyChunkCount;
        public readonly int AppliedChunkCount;
        public readonly int CreatedChunkObjectCount;
        public readonly int ReusedChunkObjectCount;
        public readonly int DisabledChunkObjectCount;
        public readonly int ActiveChunkObjectCount;
        public readonly bool UsedFallbackUv;
        public readonly int MissingUvTileCount;
        public readonly int FirstMissingUvTileId;
        public readonly string Reason;

        // =============================================================================
        // ArcGraphTerrainRuntimeSceneRendererDiagnostics
        // =============================================================================
        /// <summary>
        /// <para>
        /// Costruisce un risultato diagnostico immutabile del frame terrain.
        /// </para>
        /// </summary>
        public ArcGraphTerrainRuntimeSceneRendererDiagnostics(
            bool rendererEnabled,
            bool hasContract,
            bool contractSafe,
            bool hasContext,
            bool hasConfig,
            bool hasMap,
            bool hasRuntime,
            bool hasRenderState,
            bool hasTerrainLayer,
            bool hasTerrainCatalogJson,
            bool terrainCatalogParsed,
            bool usedCatalogUvMap,
            bool usedLegacyConfigUvMap,
            int terrainCatalogEntryCount,
            bool didBuildChunks,
            bool didClearDirty,
            int dirtyChunkCountBeforeBuild,
            int builtChunkCount,
            int nonEmptyChunkCount,
            int appliedChunkCount,
            int createdChunkObjectCount,
            int reusedChunkObjectCount,
            int disabledChunkObjectCount,
            int activeChunkObjectCount,
            bool usedFallbackUv,
            int missingUvTileCount,
            int firstMissingUvTileId,
            string reason)
        {
            RendererEnabled = rendererEnabled;
            HasContract = hasContract;
            ContractSafe = contractSafe;
            HasContext = hasContext;
            HasConfig = hasConfig;
            HasMap = hasMap;
            HasRuntime = hasRuntime;
            HasRenderState = hasRenderState;
            HasTerrainLayer = hasTerrainLayer;
            HasTerrainCatalogJson = hasTerrainCatalogJson;
            TerrainCatalogParsed = terrainCatalogParsed;
            UsedCatalogUvMap = usedCatalogUvMap;
            UsedLegacyConfigUvMap = usedLegacyConfigUvMap;
            TerrainCatalogEntryCount = NormalizeCount(terrainCatalogEntryCount);
            DidBuildChunks = didBuildChunks;
            DidClearDirty = didClearDirty;
            DirtyChunkCountBeforeBuild = NormalizeCount(dirtyChunkCountBeforeBuild);
            BuiltChunkCount = NormalizeCount(builtChunkCount);
            NonEmptyChunkCount = NormalizeCount(nonEmptyChunkCount);
            AppliedChunkCount = NormalizeCount(appliedChunkCount);
            CreatedChunkObjectCount = NormalizeCount(createdChunkObjectCount);
            ReusedChunkObjectCount = NormalizeCount(reusedChunkObjectCount);
            DisabledChunkObjectCount = NormalizeCount(disabledChunkObjectCount);
            ActiveChunkObjectCount = NormalizeCount(activeChunkObjectCount);
            UsedFallbackUv = usedFallbackUv;
            MissingUvTileCount = NormalizeCount(missingUvTileCount);
            FirstMissingUvTileId = firstMissingUvTileId;
            Reason = string.IsNullOrWhiteSpace(reason) ? "None" : reason;
        }

        private static int NormalizeCount(int value)
        {
            return value < 0 ? 0 : value;
        }
    }
}
