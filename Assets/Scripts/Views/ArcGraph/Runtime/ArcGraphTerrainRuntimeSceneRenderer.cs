using System.Collections.Generic;
using Arcontio.View.MapGrid;
using UnityEngine;

namespace Arcontio.View.ArcGraph
{
    // =============================================================================
    // ArcGraphTerrainRuntimeSceneRenderer
    // =============================================================================
    /// <summary>
    /// <para>
    /// Renderer runtime minimo per materializzare i chunk terreno ArcGraph in scena.
    /// </para>
    ///
    /// <para><b>Principio architetturale: renderer terrain controllato, non probe temporaneo</b></para>
    /// <para>
    /// Questo componente e' il primo passaggio oltre il probe terrain. Riceve dati
    /// ArcGraph gia' preparati, costruisce mesh chunk solo per i chunk dirty e
    /// riusa GameObject/Mesh tramite un pool locale indicizzato per coordinata
    /// chunk. Non legge globali, non carica asset, non invia comandi, non modifica
    /// MapGrid, non modifica il World e non sostituisce ancora MapGrid come renderer
    /// principale.
    /// </para>
    ///
    /// <para><b>Struttura interna:</b></para>
    /// <list type="bullet">
    ///   <item><b>runtimeMapAdapter</b>: sorgente manuale opzionale per test da Inspector.</item>
    ///   <item><b>terrainMaterial</b>: materiale terrain assegnato dall'esterno.</item>
    ///   <item><b>_chunkPool</b>: chunk scene-side riusabili, uno per coordinata chunk.</item>
    ///   <item><b>RenderFromRuntime</b>: entry point futuro per wrapper/coordinator.</item>
    ///   <item><b>RenderFromMapGridRuntime</b>: entry point manuale da adapter MapGrid.</item>
    ///   <item><b>ClearRuntimeRenderer</b>: cleanup confinato del solo root ArcGraph.</item>
    /// </list>
    /// </summary>
    public sealed class ArcGraphTerrainRuntimeSceneRenderer : MonoBehaviour
    {
        [SerializeField] private ArcGraphTerrainRuntimeMapGridAdapter runtimeMapAdapter;
        [SerializeField] private Material terrainMaterial;
        [SerializeField] private TextAsset terrainCatalogJson;
        [SerializeField] private bool rendererEnabled;
        [SerializeField] private bool renderOnStart;
        [SerializeField] private bool clearDirtyAfterRender = true;
        [SerializeField] private bool logDiagnostics = true;
        [SerializeField] private Vector3 originOffset = Vector3.zero;
        [SerializeField] private float terrainZOffset = -0.05f;
        [SerializeField] private int terrainSortingOrder = -5;
        [SerializeField] private string runtimeRootName = "ArcGraphTerrainRuntimeRoot";

        private readonly Dictionary<ArcGraphChunkCoord, ChunkHandle> _chunkPool = new();
        private Transform _root;
        private ArcGraphTerrainCatalog _terrainCatalog;
        private string _terrainCatalogSourceText;
        private ArcGraphTerrainRuntimeSceneRendererDiagnostics _lastDiagnostics;
        private bool _uvHasTerrainCatalogJson;
        private bool _uvTerrainCatalogParsed;
        private bool _uvUsedCatalogMap;
        private bool _uvUsedLegacyConfigMap;
        private int _uvTerrainCatalogEntryCount;

        public ArcGraphTerrainRuntimeSceneRendererDiagnostics LastDiagnostics => _lastDiagnostics;
        public bool RendererEnabled => rendererEnabled;
        public int PooledChunkCount => _chunkPool.Count;

        // =============================================================================
        // ChunkHandle
        // =============================================================================
        /// <summary>
        /// <para>
        /// Handle locale di un chunk terrain materializzato in scena.
        /// </para>
        ///
        /// <para><b>Pooling confinato</b></para>
        /// <para>
        /// Il renderer conserva un handle per chunk per evitare di distruggere e
        /// ricreare continuamente GameObject e Mesh. L'handle resta privato: nessun
        /// altro sistema puo' usarlo come fonte di verita' o modificarlo.
        /// </para>
        ///
        /// <para><b>Struttura interna:</b></para>
        /// <list type="bullet">
        ///   <item><b>Chunk</b>: chiave logica del pool.</item>
        ///   <item><b>GameObject</b>: oggetto scena locale.</item>
        ///   <item><b>Mesh</b>: mesh runtime riusata.</item>
        ///   <item><b>Filter/Renderer</b>: componenti Unity del chunk.</item>
        ///   <item><b>WasTouchedThisFrame</b>: marker per disattivare chunk non piu' aggiornati se necessario.</item>
        /// </list>
        /// </summary>
        private sealed class ChunkHandle
        {
            public ArcGraphChunkCoord Chunk;
            public GameObject GameObject;
            public Mesh Mesh;
            public MeshFilter Filter;
            public MeshRenderer Renderer;
            public bool WasTouchedThisFrame;
        }

        // =============================================================================
        // Start
        // =============================================================================
        /// <summary>
        /// <para>
        /// Avvia opzionalmente il rendering terrain runtime da adapter MapGrid.
        /// </para>
        ///
        /// <para><b>Default spento</b></para>
        /// <para>
        /// Il flag <c>renderOnStart</c> resta falso di default. Il renderer non deve
        /// accendersi in automatico sopra MapGrid durante la transizione.
        /// </para>
        /// </summary>
        private void Start()
        {
            if (!renderOnStart)
                return;

            RenderFromMapGridRuntime();
        }

        // =============================================================================
        // SetRendererEnabled
        // =============================================================================
        /// <summary>
        /// <para>
        /// Abilita o disabilita il gate principale del renderer terrain.
        /// </para>
        /// </summary>
        public void SetRendererEnabled(bool enabled)
        {
            rendererEnabled = enabled;
        }

        // =============================================================================
        // SetRuntimeMapAdapter
        // =============================================================================
        /// <summary>
        /// <para>
        /// Assegna l'adapter MapGrid usato dal path manuale di test.
        /// </para>
        /// </summary>
        public void SetRuntimeMapAdapter(ArcGraphTerrainRuntimeMapGridAdapter adapter)
        {
            runtimeMapAdapter = adapter;
        }

        // =============================================================================
        // SetTerrainMaterial
        // =============================================================================
        /// <summary>
        /// <para>
        /// Assegna il materiale terrain ricevuto dall'esterno.
        /// </para>
        /// </summary>
        public void SetTerrainMaterial(Material material)
        {
            terrainMaterial = material;
        }

        // =============================================================================
        // SetTerrainCatalogJson
        // =============================================================================
        /// <summary>
        /// <para>
        /// Assegna il JSON terrain catalog usato per risolvere tile id -> UV atlas.
        /// </para>
        /// </summary>
        public void SetTerrainCatalogJson(TextAsset catalogJson)
        {
            terrainCatalogJson = catalogJson;
            _terrainCatalog = null;
            _terrainCatalogSourceText = null;
        }

        // =============================================================================
        // RenderFromMapGridRuntimeContextMenu
        // =============================================================================
        /// <summary>
        /// <para>
        /// Entry point manuale da Inspector per renderizzare da adapter MapGrid.
        /// </para>
        /// </summary>
        [ContextMenu("ArcGraph/Render Terrain Runtime From MapGrid")]
        public void RenderFromMapGridRuntimeContextMenu()
        {
            RenderFromMapGridRuntime();
        }

        // =============================================================================
        // RenderFromMapGridRuntime
        // =============================================================================
        /// <summary>
        /// <para>
        /// Costruisce un bootstrap ArcGraph temporaneo e applica i chunk terrain.
        /// </para>
        ///
        /// <para><b>Compatibilita' test manuale</b></para>
        /// <para>
        /// Questo metodo serve a testare il renderer runtime senza attendere il
        /// wiring completo col wrapper minimo. Il bootstrap resta temporaneo, ma il
        /// renderer non distrugge e ricrea ogni chunk: usa comunque il proprio pool
        /// runtime.
        /// </para>
        /// </summary>
        public ArcGraphTerrainRuntimeSceneRendererDiagnostics RenderFromMapGridRuntime()
        {
            ArcGraphRuntimeContext context = runtimeMapAdapter != null
                ? runtimeMapAdapter.BuildTerrainRuntimeContext()
                : ArcGraphRuntimeContext.Empty();

            var runtime = new ArcGraphBootstrapRuntime();
            bool initialized = runtime.Initialize(context, ArcGraphBootstrapOptions.CreateDefault());

            ArcGraphTerrainRuntimeSceneRendererDiagnostics diagnostics = initialized
                ? RenderFromRuntime(context, runtime)
                : StoreAndLogDiagnostics(
                    context,
                    runtime,
                    null,
                    CreateContract(),
                    didBuildChunks: false,
                    didClearDirty: false,
                    dirtyChunkCountBeforeBuild: 0,
                    builtChunkCount: 0,
                    nonEmptyChunkCount: 0,
                    appliedChunkCount: 0,
                    createdChunkObjectCount: 0,
                    reusedChunkObjectCount: 0,
                    disabledChunkObjectCount: 0,
                    usedFallbackUv: false,
                    runtime.Diagnostics.Reason);

            runtime.Dispose();
            return diagnostics;
        }

        // =============================================================================
        // RenderFromRuntime
        // =============================================================================
        /// <summary>
        /// <para>
        /// Applica in scena i chunk terrain dirty del runtime ArcGraph ricevuto.
        /// </para>
        ///
        /// <para><b>Entry point produttivo minimo</b></para>
        /// <para>
        /// Questo e' il metodo pensato per il futuro wrapper runtime. Il renderer
        /// riceve context e runtime gia' preparati, non li cerca. Costruisce mesh
        /// data dai layer ArcGraph e aggiorna il pool locale dei chunk.
        /// </para>
        /// </summary>
        public ArcGraphTerrainRuntimeSceneRendererDiagnostics RenderFromRuntime(
            ArcGraphRuntimeContext context,
            ArcGraphBootstrapRuntime runtime)
        {
            ArcGraphTerrainRuntimeSceneRendererContract contract = CreateContract();
            ResetUvSourceDiagnostics();

            if (!rendererEnabled)
                return StoreAndLogDiagnostics(context, runtime, null, contract, false, false, 0, 0, 0, 0, 0, 0, 0, false, "RendererDisabled");

            if (!contract.IsRuntimeSafe)
                return StoreAndLogDiagnostics(context, runtime, null, contract, false, false, 0, 0, 0, 0, 0, 0, 0, false, "UnsafeContract");

            if (runtime == null || !runtime.IsInitialized)
                return StoreAndLogDiagnostics(context, runtime, null, contract, false, false, 0, 0, 0, 0, 0, 0, 0, false, "RuntimeMissingOrNotInitialized");

            if (runtime.RenderState == null)
                return StoreAndLogDiagnostics(context, runtime, null, contract, false, false, 0, 0, 0, 0, 0, 0, 0, false, "RenderStateMissing");

            if (runtime.LayerStack == null
                || !runtime.LayerStack.TryGetLayer<ArcGraphTerrainLayer>(out var terrainLayer))
            {
                return StoreAndLogDiagnostics(context, runtime, null, contract, false, false, runtime.RenderState.Dirty.DirtyChunks.Count, 0, 0, 0, 0, 0, 0, false, "TerrainLayerMissing");
            }

            int dirtyChunkCount = runtime.RenderState.Dirty.DirtyChunks.Count;
            if (dirtyChunkCount <= 0)
                return StoreAndLogDiagnostics(context, runtime, terrainLayer, contract, false, false, 0, 0, 0, 0, 0, 0, 0, false, "NoDirtyTerrainChunks");

            List<ArcGraphTerrainChunkMeshData> chunks = BuildTerrainChunks(context, runtime.RenderState, terrainLayer, contract);
            ApplyChunks(chunks, contract, out int applied, out int created, out int reused, out int disabled, out bool usedFallbackUv);

            bool didClearDirty = false;
            if (contract.ClearDirtyAfterRender)
            {
                runtime.RenderState.ClearDirty();
                didClearDirty = true;
            }

            return StoreAndLogDiagnostics(
                context,
                runtime,
                terrainLayer,
                contract,
                didBuildChunks: true,
                didClearDirty,
                dirtyChunkCount,
                chunks.Count,
                CountNonEmptyChunks(chunks),
                applied,
                created,
                reused,
                disabled,
                usedFallbackUv,
                applied > 0 ? "TerrainRuntimeRendered" : "TerrainRuntimeNoVisibleChunks");
        }

        // =============================================================================
        // ClearRuntimeRenderer
        // =============================================================================
        /// <summary>
        /// <para>
        /// Rimuove tutti i chunk terrain runtime creati da questo renderer.
        /// </para>
        ///
        /// <para><b>Cleanup confinato</b></para>
        /// <para>
        /// Il cleanup distrugge solo il root locale e le mesh possedute dal pool.
        /// Non tocca MapGrid, probe terrain, actor/object renderer, scene asset o
        /// prefab.
        /// </para>
        /// </summary>
        [ContextMenu("ArcGraph/Clear Terrain Runtime Renderer")]
        public void ClearRuntimeRenderer()
        {
            foreach (var pair in _chunkPool)
            {
                // Le mesh sono create dal renderer runtime, quindi sono sue da
                // distruggere. Non distruggiamo materiali o texture assegnati da
                // Inspector.
                DestroyUnityObject(pair.Value.Mesh);
            }

            _chunkPool.Clear();

            if (_root != null)
            {
                DestroyUnityObject(_root.gameObject);
                _root = null;
            }
        }

        // =============================================================================
        // LogLastDiagnosticsFromContextMenu
        // =============================================================================
        /// <summary>
        /// <para>
        /// Ristampa in Console l'ultima diagnostica prodotta dal renderer terrain.
        /// </para>
        ///
        /// <para><b>Supporto gate visuale</b></para>
        /// <para>
        /// Durante il confronto MapGrid/ArcGraph puo' essere utile verificare di
        /// nuovo se il renderer sta usando catalogo ArcGraph o fallback legacy,
        /// senza ricostruire i chunk e senza modificare scena o simulazione.
        /// </para>
        /// </summary>
        [ContextMenu("ArcGraph/Log Last Terrain Runtime Diagnostics")]
        public void LogLastDiagnosticsFromContextMenu()
        {
            LogLastDiagnostics();
        }

        private ArcGraphTerrainRuntimeSceneRendererContract CreateContract()
        {
            return new ArcGraphTerrainRuntimeSceneRendererContract(
                terrainMaterial,
                runtimeRootName,
                originOffset,
                terrainZOffset,
                terrainSortingOrder,
                clearDirtyAfterRender);
        }

        private List<ArcGraphTerrainChunkMeshData> BuildTerrainChunks(
            ArcGraphRuntimeContext context,
            ArcGraphRenderState renderState,
            ArcGraphTerrainLayer terrainLayer,
            ArcGraphTerrainRuntimeSceneRendererContract contract)
        {
            var builder = new ArcGraphTerrainChunkMeshBuilder();
            return builder.BuildDirtyChunks(
                terrainLayer,
                CreateUvMap(context?.Config, contract.TerrainMaterial),
                renderState,
                ArcGraphTerrainVisualPolicy.CreateLegacyDefault());
        }

        private void ApplyChunks(
            List<ArcGraphTerrainChunkMeshData> chunks,
            ArcGraphTerrainRuntimeSceneRendererContract contract,
            out int applied,
            out int created,
            out int reused,
            out int disabled,
            out bool usedFallbackUv)
        {
            applied = 0;
            created = 0;
            reused = 0;
            disabled = 0;
            usedFallbackUv = false;

            if (chunks == null || chunks.Count == 0)
                return;

            EnsureRoot(contract);
            MarkAllHandlesUntouched();

            for (int i = 0; i < chunks.Count; i++)
            {
                ArcGraphTerrainChunkMeshData chunk = chunks[i];
                if (chunk == null || chunk.IsEmpty)
                {
                    if (chunk != null && TryDisableChunk(chunk.Diagnostics.Chunk))
                        disabled++;

                    continue;
                }

                ChunkHandle handle = GetOrCreateChunkHandle(chunk.Diagnostics.Chunk, contract, out bool wasCreated);
                ApplyMeshData(handle, chunk, contract);

                applied++;
                usedFallbackUv |= chunk.Diagnostics.UsedFallbackUv;

                if (wasCreated)
                    created++;
                else
                    reused++;
            }
        }

        private void ApplyMeshData(
            ChunkHandle handle,
            ArcGraphTerrainChunkMeshData chunk,
            ArcGraphTerrainRuntimeSceneRendererContract contract)
        {
            // La mesh viene riusata: Clear rimuove il contenuto precedente senza
            // allocare un nuovo oggetto Mesh a ogni frame.
            handle.Mesh.Clear();
            handle.Mesh.vertices = chunk.Vertices;
            handle.Mesh.uv = chunk.Uvs;
            handle.Mesh.triangles = chunk.Triangles;
            handle.Mesh.RecalculateBounds();

            handle.Renderer.sharedMaterial = contract.TerrainMaterial;
            handle.Renderer.sortingOrder = contract.SortingOrder;
            handle.GameObject.SetActive(true);
            handle.WasTouchedThisFrame = true;
        }

        private ChunkHandle GetOrCreateChunkHandle(
            ArcGraphChunkCoord chunk,
            ArcGraphTerrainRuntimeSceneRendererContract contract,
            out bool wasCreated)
        {
            if (_chunkPool.TryGetValue(chunk, out var handle))
            {
                wasCreated = false;
                return handle;
            }

            EnsureRoot(contract);

            var go = new GameObject("ArcGraphTerrainRuntimeChunk_" + chunk.X + "_" + chunk.Y + "_" + chunk.Z);
            go.transform.SetParent(_root, false);

            var mesh = new Mesh
            {
                name = "ArcGraphTerrainRuntimeMesh_" + chunk.X + "_" + chunk.Y + "_" + chunk.Z
            };

            handle = new ChunkHandle
            {
                Chunk = chunk,
                GameObject = go,
                Mesh = mesh,
                Filter = go.AddComponent<MeshFilter>(),
                Renderer = go.AddComponent<MeshRenderer>(),
                WasTouchedThisFrame = true
            };

            handle.Filter.sharedMesh = mesh;
            handle.Renderer.sharedMaterial = contract.TerrainMaterial;
            handle.Renderer.sortingOrder = contract.SortingOrder;

            _chunkPool[chunk] = handle;
            wasCreated = true;
            return handle;
        }

        private bool TryDisableChunk(ArcGraphChunkCoord chunk)
        {
            if (!_chunkPool.TryGetValue(chunk, out var handle))
                return false;

            handle.GameObject.SetActive(false);
            handle.WasTouchedThisFrame = false;
            return true;
        }

        private void MarkAllHandlesUntouched()
        {
            foreach (var pair in _chunkPool)
                pair.Value.WasTouchedThisFrame = false;
        }

        private void EnsureRoot(ArcGraphTerrainRuntimeSceneRendererContract contract)
        {
            if (_root != null)
                return;

            _root = transform.Find(contract.RootName);
            if (_root != null)
                return;

            var go = new GameObject(contract.RootName);
            go.transform.SetParent(transform, false);
            go.transform.localPosition = contract.OriginOffset + new Vector3(0f, 0f, contract.ZOffset);
            _root = go.transform;
        }

        private ArcGraphTerrainRuntimeSceneRendererDiagnostics StoreAndLogDiagnostics(
            ArcGraphRuntimeContext context,
            ArcGraphBootstrapRuntime runtime,
            ArcGraphTerrainLayer terrainLayer,
            ArcGraphTerrainRuntimeSceneRendererContract contract,
            bool didBuildChunks,
            bool didClearDirty,
            int dirtyChunkCountBeforeBuild,
            int builtChunkCount,
            int nonEmptyChunkCount,
            int appliedChunkCount,
            int createdChunkObjectCount,
            int reusedChunkObjectCount,
            int disabledChunkObjectCount,
            bool usedFallbackUv,
            string reason)
        {
            _lastDiagnostics = new ArcGraphTerrainRuntimeSceneRendererDiagnostics(
                rendererEnabled,
                hasContract: true,
                contract.IsRuntimeSafe,
                context != null,
                context != null && context.HasConfig,
                context != null && context.HasMap,
                runtime != null,
                runtime != null && runtime.RenderState != null,
                terrainLayer != null,
                _uvHasTerrainCatalogJson,
                _uvTerrainCatalogParsed,
                _uvUsedCatalogMap,
                _uvUsedLegacyConfigMap,
                _uvTerrainCatalogEntryCount,
                didBuildChunks,
                didClearDirty,
                dirtyChunkCountBeforeBuild,
                builtChunkCount,
                nonEmptyChunkCount,
                appliedChunkCount,
                createdChunkObjectCount,
                reusedChunkObjectCount,
                disabledChunkObjectCount,
                CountActiveChunkObjects(),
                usedFallbackUv,
                reason);

            LogLastDiagnostics();
            return _lastDiagnostics;
        }

        private void LogLastDiagnostics()
        {
            if (!logDiagnostics)
                return;

            Debug.Log(
                "[ArcGraphTerrainRuntimeSceneRenderer] " + _lastDiagnostics.Reason +
                " enabled=" + _lastDiagnostics.RendererEnabled +
                ", contractSafe=" + _lastDiagnostics.ContractSafe +
                ", context=" + _lastDiagnostics.HasContext +
                ", config=" + _lastDiagnostics.HasConfig +
                ", map=" + _lastDiagnostics.HasMap +
                ", runtime=" + _lastDiagnostics.HasRuntime +
                ", renderState=" + _lastDiagnostics.HasRenderState +
                ", terrainLayer=" + _lastDiagnostics.HasTerrainLayer +
                ", catalogJson=" + _lastDiagnostics.HasTerrainCatalogJson +
                ", catalogParsed=" + _lastDiagnostics.TerrainCatalogParsed +
                ", catalogEntries=" + _lastDiagnostics.TerrainCatalogEntryCount +
                ", catalogUv=" + _lastDiagnostics.UsedCatalogUvMap +
                ", legacyConfigUv=" + _lastDiagnostics.UsedLegacyConfigUvMap +
                ", dirtyChunks=" + _lastDiagnostics.DirtyChunkCountBeforeBuild +
                ", built=" + _lastDiagnostics.BuiltChunkCount +
                ", nonEmpty=" + _lastDiagnostics.NonEmptyChunkCount +
                ", applied=" + _lastDiagnostics.AppliedChunkCount +
                ", created=" + _lastDiagnostics.CreatedChunkObjectCount +
                ", reused=" + _lastDiagnostics.ReusedChunkObjectCount +
                ", disabled=" + _lastDiagnostics.DisabledChunkObjectCount +
                ", active=" + _lastDiagnostics.ActiveChunkObjectCount +
                ", fallbackUv=" + _lastDiagnostics.UsedFallbackUv +
                ", dirtyCleared=" + _lastDiagnostics.DidClearDirty);
        }

        private int CountActiveChunkObjects()
        {
            int count = 0;
            foreach (var pair in _chunkPool)
            {
                if (pair.Value.GameObject != null && pair.Value.GameObject.activeSelf)
                    count++;
            }

            return count;
        }

        private static int CountNonEmptyChunks(List<ArcGraphTerrainChunkMeshData> chunks)
        {
            if (chunks == null)
                return 0;

            int count = 0;
            for (int i = 0; i < chunks.Count; i++)
            {
                if (chunks[i] != null && !chunks[i].IsEmpty)
                    count++;
            }

            return count;
        }

        private ArcGraphTerrainTileUvMap CreateUvMap(
            MapGridConfig config,
            Material material)
        {
            if (TryCreateCatalogUvMap(material, out var catalogUvMap))
                return catalogUvMap;

            int tilePixels = config != null && config.tilePixels > 0
                ? config.tilePixels
                : 32;

            Texture texture = material != null
                ? material.mainTexture
                : null;

            int atlasWidth = texture != null
                ? texture.width
                : InferAtlasPixels(config, tilePixels, useX: true);
            int atlasHeight = texture != null
                ? texture.height
                : InferAtlasPixels(config, tilePixels, useX: false);

            var uvMap = new ArcGraphTerrainTileUvMap(atlasWidth, atlasHeight, tilePixels);

            if (config?.tileDefs == null)
                return uvMap;

            _uvUsedLegacyConfigMap = true;

            for (int i = 0; i < config.tileDefs.Length; i++)
            {
                MapGridConfig.TileDef definition = config.tileDefs[i];
                if (definition == null)
                    continue;

                uvMap.Register(definition.id, definition.uvX, definition.uvY);
            }

            return uvMap;
        }

        private static int InferAtlasPixels(
            MapGridConfig config,
            int tilePixels,
            bool useX)
        {
            int maxCell = 0;

            if (config?.tileDefs != null)
            {
                for (int i = 0; i < config.tileDefs.Length; i++)
                {
                    MapGridConfig.TileDef definition = config.tileDefs[i];
                    if (definition == null)
                        continue;

                    int coordinate = useX ? definition.uvX : definition.uvY;
                    if (coordinate > maxCell)
                        maxCell = coordinate;
                }
            }

            return (maxCell + 1) * Mathf.Max(1, tilePixels);
        }

        private bool TryCreateCatalogUvMap(
            Material material,
            out ArcGraphTerrainTileUvMap uvMap)
        {
            uvMap = null;
            _uvHasTerrainCatalogJson = terrainCatalogJson != null;

            if (terrainCatalogJson == null)
                return false;

            ArcGraphTerrainCatalog catalog = GetOrParseTerrainCatalog();
            if (catalog == null || catalog.EntryCount <= 0)
                return false;

            _uvTerrainCatalogParsed = true;
            _uvTerrainCatalogEntryCount = catalog.EntryCount;
            _uvUsedCatalogMap = true;

            Texture texture = material != null ? material.mainTexture : null;
            uvMap = catalog.BuildUvMap(texture);
            return uvMap != null;
        }

        private void ResetUvSourceDiagnostics()
        {
            _uvHasTerrainCatalogJson = false;
            _uvTerrainCatalogParsed = false;
            _uvUsedCatalogMap = false;
            _uvUsedLegacyConfigMap = false;
            _uvTerrainCatalogEntryCount = 0;
        }

        private ArcGraphTerrainCatalog GetOrParseTerrainCatalog()
        {
            string json = terrainCatalogJson != null
                ? terrainCatalogJson.text
                : null;

            // Il TextAsset viene parsato solo quando cambia il testo sorgente. In
            // runtime normale questo evita parsing ripetuto a ogni frame/chunk.
            if (_terrainCatalog != null && _terrainCatalogSourceText == json)
                return _terrainCatalog;

            if (!ArcGraphTerrainCatalogJson.TryParse(json, out _terrainCatalog))
            {
                _terrainCatalog = null;
                _terrainCatalogSourceText = null;
                return null;
            }

            _terrainCatalogSourceText = json;
            return _terrainCatalog;
        }

        private static void DestroyUnityObject(Object unityObject)
        {
            if (unityObject == null)
                return;

            if (Application.isPlaying)
                Destroy(unityObject);
            else
                DestroyImmediate(unityObject);
        }

        private void OnDestroy()
        {
            ClearRuntimeRenderer();
        }
    }
}
