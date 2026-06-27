using System.Collections.Generic;
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
    ///   <item><b>runtimeContextProvider</b>: sorgente manuale opzionale per test da Inspector.</item>
    ///   <item><b>terrainMaterial</b>: materiale terrain assegnato dall'esterno.</item>
    ///   <item><b>_chunkPool</b>: chunk scene-side riusabili, uno per coordinata chunk.</item>
    ///   <item><b>RenderFromRuntime</b>: entry point futuro per wrapper/coordinator.</item>
    ///   <item><b>RenderFromConfiguredRuntimeContext</b>: entry point manuale da provider context.</item>
    ///   <item><b>ClearRuntimeRenderer</b>: cleanup confinato del solo root ArcGraph.</item>
    /// </list>
    /// </summary>
    public sealed class ArcGraphTerrainRuntimeSceneRenderer : MonoBehaviour
    {
        [SerializeField] private ArcGraphRuntimeContextProvider runtimeContextProvider;
        [SerializeField] private Material terrainMaterial;
        [SerializeField] private TextAsset terrainCatalogJson;
        [SerializeField] private TextAsset terrainVisualCatalogJson;
        [SerializeField] private bool rendererEnabled;
        [SerializeField] private bool renderOnStart;
        [SerializeField] private bool clearDirtyAfterRender = true;
        [SerializeField] private bool animateTerrainTiles = true;
        [SerializeField] private float terrainAnimationRefreshSeconds = 0.25f;
        [SerializeField] private bool useViewportCulling;
        [SerializeField] private int viewportMinX;
        [SerializeField] private int viewportMinY;
        [SerializeField] private int viewportMaxXExclusive;
        [SerializeField] private int viewportMaxYExclusive;
        [SerializeField] private bool logDiagnostics;
        [SerializeField] private Vector3 originOffset = Vector3.zero;
        [SerializeField] private float terrainZOffset = -0.05f;
        [SerializeField] private int terrainSortingOrder = -5;
        [SerializeField] private string runtimeRootName = "ArcGraphTerrainRuntimeRoot";

        private readonly Dictionary<ArcGraphChunkCoord, ChunkHandle> _chunkPool = new();
        private readonly HashSet<ArcGraphChunkCoord> _animatedTerrainChunks = new();
        private readonly List<ArcGraphChunkCoord> _dirtyChunkBuffer = new();
        private readonly ArcGraphTerrainAnimationClock _terrainAnimationClock = new();
        private Transform _root;
        private ArcGraphTerrainCatalog _terrainCatalog;
        private string _terrainCatalogSourceText;
        private ArcGraphTerrainVisualCatalog _terrainVisualCatalog;
        private string _terrainVisualCatalogSourceText;
        private ArcGraphTerrainRuntimeSceneRendererDiagnostics _lastDiagnostics;
        private bool _uvHasTerrainCatalogJson;
        private bool _uvTerrainCatalogParsed;
        private bool _uvUsedCatalogMap;
        private bool _uvUsedLegacyConfigMap;
        private int _uvTerrainCatalogEntryCount;
        private int _uvMissingTileCount;
        private int _uvFirstMissingTileId;
        private int _lastVisibleChunkCount;
        private int _lastCulledDirtyChunkCount;
        private int _lastDisabledOutsideViewportChunkCount;
        private bool _visualHasTerrainVisualCatalogJson;
        private bool _visualTerrainVisualCatalogParsed;
        private bool _visualUsedResolver;
        private int _visualTerrainVisualCatalogDefinitionCount;
        private int _visualResolverTileCount;
        private int _visualLegacyTileCount;
        private int _visualVariantTileCount;
        private int _visualAnimationTileCount;
        private int _visualTransitionTileCount;
        private int _visualResolverFallbackCount;
        private bool _coverageChecked;
        private bool _coverageComplete;
        private int _coverageRequiredTileCount;
        private int _coverageCoveredTileCount;
        private int _coverageMissingTileCount;
        private int _coverageFirstMissingTileId;
        private int _lastAnimatedTerrainChunkCount;
        private bool _lastTerrainAnimationRefreshQueued;
        private float _lastTerrainAnimationVisualTimeSeconds;
        private bool _hasLastQueuedVisibleRect;
        private bool _lastQueuedUsedViewportCulling;
        private ArcGraphViewCellRect _lastQueuedVisibleRect;

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
        /// Avvia opzionalmente il rendering terrain runtime dal provider configurato.
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

            RenderFromConfiguredRuntimeContext();
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
        // SetRuntimeContextProvider
        // =============================================================================
        /// <summary>
        /// <para>
        /// Assegna il provider runtime usato dal path manuale di test.
        /// </para>
        /// </summary>
        public void SetRuntimeContextProvider(ArcGraphRuntimeContextProvider provider)
        {
            runtimeContextProvider = provider;
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
        // SetTerrainVisualCatalogJson
        // =============================================================================
        /// <summary>
        /// <para>
        /// Assegna il JSON del catalogo visuale terrain usato dal resolver.
        /// </para>
        /// </summary>
        public void SetTerrainVisualCatalogJson(TextAsset catalogJson)
        {
            terrainVisualCatalogJson = catalogJson;
            _terrainVisualCatalog = null;
            _terrainVisualCatalogSourceText = null;
        }

        // =============================================================================
        // SetViewportCullingEnabled
        // =============================================================================
        /// <summary>
        /// <para>
        /// Abilita o disabilita il filtro viewport per i chunk terrain.
        /// </para>
        ///
        /// <para><b>Gate esplicito</b></para>
        /// <para>
        /// Il culling resta disattivabile per mantenere compatibili i gate visuali
        /// che vogliono renderizzare tutta la mappa. Quando e' attivo, il renderer
        /// costruisce solo chunk che intersecano il rettangolo celle visibile.
        /// </para>
        /// </summary>
        public void SetViewportCullingEnabled(bool enabled)
        {
            useViewportCulling = enabled;
        }

        // =============================================================================
        // SetVisibleCellRect
        // =============================================================================
        /// <summary>
        /// <para>
        /// Imposta il rettangolo celle visibile usato dal filtro viewport.
        /// </para>
        ///
        /// <para><b>Viewport ricevuto, non calcolato</b></para>
        /// <para>
        /// Il renderer non legge camera o input. Riceve una finestra celle gia'
        /// risolta da moduli view-side e la conserva come parametro scene-side.
        /// </para>
        /// </summary>
        public void SetVisibleCellRect(ArcGraphViewCellRect rect)
        {
            viewportMinX = rect.MinX;
            viewportMinY = rect.MinY;
            viewportMaxXExclusive = rect.MaxXExclusive;
            viewportMaxYExclusive = rect.MaxYExclusive;
            useViewportCulling = !rect.IsEmpty;
        }

        // =============================================================================
        // ClearVisibleCellRect
        // =============================================================================
        /// <summary>
        /// <para>
        /// Disabilita il filtro viewport e svuota il rettangolo celle salvato.
        /// </para>
        /// </summary>
        public void ClearVisibleCellRect()
        {
            viewportMinX = 0;
            viewportMinY = 0;
            viewportMaxXExclusive = 0;
            viewportMaxYExclusive = 0;
            useViewportCulling = false;
        }

        // =============================================================================
        // RenderFromConfiguredRuntimeContextMenu
        // =============================================================================
        /// <summary>
        /// <para>
        /// Entry point manuale da Inspector per renderizzare dal provider context.
        /// </para>
        /// </summary>
        [ContextMenu("ArcGraph/Render Terrain Runtime From Context Provider")]
        public void RenderFromConfiguredRuntimeContextMenu()
        {
            RenderFromConfiguredRuntimeContext();
        }

        // =============================================================================
        // RenderFromConfiguredRuntimeContext
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
        public ArcGraphTerrainRuntimeSceneRendererDiagnostics RenderFromConfiguredRuntimeContext()
        {
            ArcGraphRuntimeContext context = runtimeContextProvider != null
                ? runtimeContextProvider.BuildTerrainRuntimeContext()
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

            QueueVisibleTerrainChunksIfViewportChanged(runtime.RenderState);
            QueueAnimatedTerrainChunksIfDue(runtime.RenderState);

            int dirtyChunkCount = runtime.RenderState.Dirty.DirtyChunks.Count;
            if (dirtyChunkCount <= 0)
            {
                _lastDisabledOutsideViewportChunkCount = DisableChunksOutsideViewport(runtime.RenderState);
                return StoreAndLogDiagnostics(context, runtime, terrainLayer, contract, false, false, 0, 0, 0, 0, 0, 0, _lastDisabledOutsideViewportChunkCount, false, "NoDirtyTerrainChunks");
            }

            ArcGraphTerrainVisibleChunkFilterResult filterResult = FilterDirtyChunks(runtime.RenderState);
            _lastVisibleChunkCount = filterResult.VisibleChunkCount;
            _lastCulledDirtyChunkCount = filterResult.CulledChunkCount;
            _lastDisabledOutsideViewportChunkCount = DisableChunksOutsideViewport(runtime.RenderState);

            List<ArcGraphTerrainChunkMeshData> chunks = BuildTerrainChunks(
                context,
                runtime.RenderState,
                terrainLayer,
                contract,
                filterResult);

            ApplyChunks(chunks, contract, out int applied, out int created, out int reused, out int disabled, out bool usedFallbackUv);
            disabled += _lastDisabledOutsideViewportChunkCount;

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
            ArcGraphTerrainRuntimeSceneRendererContract contract,
            ArcGraphTerrainVisibleChunkFilterResult filterResult)
        {
            var builder = new ArcGraphTerrainChunkMeshBuilder();
            ArcGraphTerrainVisualPolicy visualPolicy = ArcGraphTerrainVisualPolicy.CreateLegacyDefault();
            ArcGraphTerrainVisualBuildOptions visualBuildOptions = CreateVisualBuildOptions();
            ArcGraphRuntimeTerrainMap runtimeTerrainMap = terrainLayer != null
                ? terrainLayer.GetOrRebuildRuntimeTerrainMap(visualPolicy, visualBuildOptions)
                : null;
            RefreshAnimatedTerrainChunks(runtimeTerrainMap, renderState);

            return builder.BuildChunks(
                terrainLayer,
                runtimeTerrainMap,
                CreateUvMap(contract.TerrainMaterial),
                filterResult?.Chunks,
                renderState != null ? renderState.ChunkSizeCells : 16,
                renderState != null ? renderState.TileSizeWorld : 1f,
                visualPolicy,
                visualBuildOptions);
        }

        // =============================================================================
        // FilterDirtyChunks
        // =============================================================================
        /// <summary>
        /// <para>
        /// Applica il filtro viewport ai chunk terrain dirty del frame corrente.
        /// </para>
        ///
        /// <para><b>Filtro grafico passivo</b></para>
        /// <para>
        /// Il metodo non cancella dirty state, non modifica layer e non tocca la
        /// scena. Produce solo una lista di chunk ammessi alla costruzione mesh,
        /// lasciando a <c>RenderFromRuntime</c> la scelta se consumare o conservare
        /// il dirty complessivo.
        /// </para>
        /// </summary>
        private ArcGraphTerrainVisibleChunkFilterResult FilterDirtyChunks(ArcGraphRenderState renderState)
        {
            var filter = new ArcGraphTerrainVisibleChunkFilter();
            _dirtyChunkBuffer.Clear();
            if (renderState != null)
            {
                foreach (ArcGraphChunkCoord dirtyChunk in renderState.Dirty.DirtyChunks)
                    _dirtyChunkBuffer.Add(dirtyChunk);
            }

            return filter.Filter(
                _dirtyChunkBuffer,
                renderState != null ? renderState.ChunkSizeCells : 16,
                renderState != null ? renderState.VisibleZLevel : ArcGraphZLevelPolicy.DefaultVisibleZLevel,
                useViewportCulling,
                CreateVisibleCellRect());
        }

        // =============================================================================
        // QueueAnimatedTerrainChunksIfDue
        // =============================================================================
        /// <summary>
        /// <para>
        /// Marca dirty i chunk terrain animati quando il clock visuale richiede un
        /// cambio frame.
        /// </para>
        ///
        /// <para><b>Principio architetturale: animazione a chunk, non scansione completa</b></para>
        /// <para>
        /// Il metodo non rilegge tutta la mappa e non decide quali celle siano
        /// acqua, erba o altro. Usa solo l'elenco dei chunk animati gia' prodotto
        /// durante l'ultimo rebuild della runtime terrain map. In questo modo il
        /// costo per frame resta proporzionale ai chunk animati noti, non al numero
        /// totale di celle.
        /// </para>
        ///
        /// <para><b>Struttura interna:</b></para>
        /// <list type="bullet">
        ///   <item><b>Clock</b>: avanza tempo visuale con delta Unity.</item>
        ///   <item><b>Filtro Z</b>: marca solo chunk del livello visibile.</item>
        ///   <item><b>Filtro viewport</b>: evita chunk animati fuori finestra quando il culling e' attivo.</item>
        ///   <item><b>Dirty chunks</b>: accoda solo coordinate chunk, non celle singole.</item>
        /// </list>
        /// </summary>
        private void QueueAnimatedTerrainChunksIfDue(ArcGraphRenderState renderState)
        {
            _lastTerrainAnimationRefreshQueued = false;
            _lastAnimatedTerrainChunkCount = _animatedTerrainChunks.Count;
            _lastTerrainAnimationVisualTimeSeconds = _terrainAnimationClock.VisualTimeSeconds;

            if (!animateTerrainTiles || renderState == null)
                return;

            float deltaSeconds = Time.unscaledDeltaTime > 0f
                ? Time.unscaledDeltaTime
                : Time.deltaTime;

            ArcGraphTerrainAnimationClockStep step = _terrainAnimationClock.Advance(
                deltaSeconds,
                terrainAnimationRefreshSeconds);
            _lastTerrainAnimationVisualTimeSeconds = step.VisualTimeSeconds;

            if (!step.RefreshDue || _animatedTerrainChunks.Count <= 0)
                return;

            int queued = 0;
            ArcGraphViewCellRect visibleRect = CreateVisibleCellRect();
            foreach (ArcGraphChunkCoord chunk in _animatedTerrainChunks)
            {
                if (chunk.Z != renderState.VisibleZLevel)
                    continue;

                if (useViewportCulling
                    && !visibleRect.IsEmpty
                    && !ArcGraphTerrainVisibleChunkFilter.ChunkIntersectsRect(
                        chunk,
                        renderState.ChunkSizeCells,
                        visibleRect))
                {
                    continue;
                }

                renderState.Dirty.MarkChunkDirty(chunk);
                queued++;
            }

            _lastTerrainAnimationRefreshQueued = queued > 0;
        }

        // =============================================================================
        // QueueVisibleTerrainChunksIfViewportChanged
        // =============================================================================
        /// <summary>
        /// <para>
        /// Marca dirty i chunk della nuova finestra visibile quando cambia il
        /// viewport ArcGraph.
        /// </para>
        ///
        /// <para><b>Principio architetturale: culling recuperabile</b></para>
        /// <para>
        /// Quando il renderer costruisce solo la porzione visibile, i chunk fuori
        /// viewport possono restare non materializzati. Al cambio di pan/zoom questo
        /// metodo accoda i chunk entrati nella finestra, cosi' il renderer puo'
        /// pulire il dirty globale senza perdere la capacita' di disegnare zone
        /// nuove appena diventano visibili.
        /// </para>
        /// </summary>
        private void QueueVisibleTerrainChunksIfViewportChanged(ArcGraphRenderState renderState)
        {
            if (renderState == null)
                return;

            ArcGraphViewCellRect rect = CreateVisibleCellRect();
            bool changed = !_hasLastQueuedVisibleRect
                           || _lastQueuedUsedViewportCulling != useViewportCulling
                           || _lastQueuedVisibleRect.MinX != rect.MinX
                           || _lastQueuedVisibleRect.MinY != rect.MinY
                           || _lastQueuedVisibleRect.MaxXExclusive != rect.MaxXExclusive
                           || _lastQueuedVisibleRect.MaxYExclusive != rect.MaxYExclusive;

            if (!changed)
                return;

            _hasLastQueuedVisibleRect = true;
            _lastQueuedUsedViewportCulling = useViewportCulling;
            _lastQueuedVisibleRect = rect;

            if (!useViewportCulling || rect.IsEmpty)
                return;

            int chunkSize = renderState.ChunkSizeCells > 0 ? renderState.ChunkSizeCells : 1;
            int minChunkX = FloorDiv(rect.MinX, chunkSize);
            int minChunkY = FloorDiv(rect.MinY, chunkSize);
            int maxChunkX = FloorDiv(rect.MaxXExclusive - 1, chunkSize);
            int maxChunkY = FloorDiv(rect.MaxYExclusive - 1, chunkSize);

            for (int cy = minChunkY; cy <= maxChunkY; cy++)
            {
                for (int cx = minChunkX; cx <= maxChunkX; cx++)
                {
                    renderState.Dirty.MarkChunkDirty(new ArcGraphChunkCoord(
                        cx,
                        cy,
                        renderState.VisibleZLevel));
                }
            }
        }

        // =============================================================================
        // RefreshAnimatedTerrainChunks
        // =============================================================================
        /// <summary>
        /// <para>
        /// Ricostruisce l'indice dei chunk che contengono celle terrain animate.
        /// </para>
        ///
        /// <para><b>Principio architetturale: indicizzazione dopo il dato runtime</b></para>
        /// <para>
        /// L'indice nasce dalla <c>ArcGraphRuntimeTerrainMap</c>, non dal catalogo
        /// grezzo e non da MapGrid. Questo significa che un chunk viene considerato
        /// animato solo se almeno una cella runtime e' stata effettivamente marcata
        /// come <c>HasAnimatedVisual</c>.
        /// </para>
        ///
        /// <para><b>Struttura interna:</b></para>
        /// <list type="bullet">
        ///   <item><b>Clear</b>: elimina l'indice precedente.</item>
        ///   <item><b>Scan runtime map</b>: controlla solo la cache visuale delle celle.</item>
        ///   <item><b>ResolveChunkCoord</b>: usa il render state per rispettare la dimensione chunk corrente.</item>
        /// </list>
        /// </summary>
        private void RefreshAnimatedTerrainChunks(
            ArcGraphRuntimeTerrainMap runtimeTerrainMap,
            ArcGraphRenderState renderState)
        {
            _animatedTerrainChunks.Clear();

            if (runtimeTerrainMap == null || renderState == null)
            {
                _lastAnimatedTerrainChunkCount = 0;
                return;
            }

            for (int i = 0; i < runtimeTerrainMap.Cells.Count; i++)
            {
                ArcGraphRuntimeTerrainCell cell = runtimeTerrainMap.Cells[i];
                if (!cell.VisualCache.HasAnimatedVisual)
                    continue;

                _animatedTerrainChunks.Add(renderState.ResolveChunkCoord(cell.Cell));
            }

            _lastAnimatedTerrainChunkCount = _animatedTerrainChunks.Count;
        }

        // =============================================================================
        // DisableChunksOutsideViewport
        // =============================================================================
        /// <summary>
        /// <para>
        /// Disattiva i chunk gia' presenti nel pool che non appartengono al viewport.
        /// </para>
        ///
        /// <para><b>Pool conservato, scena alleggerita</b></para>
        /// <para>
        /// I chunk fuori finestra vengono solo spenti. Mesh e GameObject restano nel
        /// pool locale per poter essere riattivati quando il viewport torna su quella
        /// zona. Questo evita allocazioni ripetute durante pan/zoom e mantiene la
        /// responsabilita' confinata al renderer.
        /// </para>
        /// </summary>
        private int DisableChunksOutsideViewport(ArcGraphRenderState renderState)
        {
            if (!useViewportCulling || renderState == null)
                return 0;

            ArcGraphViewCellRect rect = CreateVisibleCellRect();
            if (rect.IsEmpty)
                return 0;

            int disabled = 0;
            foreach (var pair in _chunkPool)
            {
                ArcGraphChunkCoord chunk = pair.Key;
                ChunkHandle handle = pair.Value;
                if (handle == null || handle.GameObject == null || !handle.GameObject.activeSelf)
                    continue;

                bool outsideZ = chunk.Z != renderState.VisibleZLevel;
                bool outsideRect = !ArcGraphTerrainVisibleChunkFilter.ChunkIntersectsRect(
                    chunk,
                    renderState.ChunkSizeCells,
                    rect);

                if (!outsideZ && !outsideRect)
                    continue;

                handle.GameObject.SetActive(false);
                handle.WasTouchedThisFrame = false;
                disabled++;
            }

            return disabled;
        }

        // =============================================================================
        // CreateVisibleCellRect
        // =============================================================================
        /// <summary>
        /// <para>
        /// Ricostruisce il rettangolo celle visibile dai campi serializzati.
        /// </para>
        /// </summary>
        private ArcGraphViewCellRect CreateVisibleCellRect()
        {
            return new ArcGraphViewCellRect(
                viewportMinX,
                viewportMinY,
                viewportMaxXExclusive,
                viewportMaxYExclusive);
        }

        private static int FloorDiv(int value, int divisor)
        {
            int quotient = value / divisor;
            int remainder = value % divisor;
            if (remainder != 0 && ((remainder < 0) != (divisor < 0)))
                quotient--;
            return quotient;
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
                _uvMissingTileCount += chunk.Diagnostics.MissingUvTileCount;
                _visualResolverTileCount += chunk.Diagnostics.VisualResolverTileCount;
                _visualLegacyTileCount += chunk.Diagnostics.LegacyVisualTileCount;
                _visualVariantTileCount += chunk.Diagnostics.VisualVariantTileCount;
                _visualAnimationTileCount += chunk.Diagnostics.VisualAnimationTileCount;
                _visualTransitionTileCount += chunk.Diagnostics.VisualTransitionTileCount;
                _visualResolverFallbackCount += chunk.Diagnostics.VisualResolverFallbackCount;
                _visualUsedResolver |= chunk.Diagnostics.VisualResolverTileCount > 0;

                // Il primo id mancante viene conservato come indizio rapido:
                // se il catalogo terrain non contiene un tile, in Console si
                // vede subito quale id va corretto prima di guardare tutti i dati.
                if (_uvFirstMissingTileId < 0 && chunk.Diagnostics.FirstMissingUvTileId >= 0)
                    _uvFirstMissingTileId = chunk.Diagnostics.FirstMissingUvTileId;

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
                useViewportCulling,
                viewportMinX,
                viewportMinY,
                viewportMaxXExclusive,
                viewportMaxYExclusive,
                _lastVisibleChunkCount,
                _lastCulledDirtyChunkCount,
                _lastDisabledOutsideViewportChunkCount,
                _visualHasTerrainVisualCatalogJson,
                _visualTerrainVisualCatalogParsed,
                _visualUsedResolver,
                _visualTerrainVisualCatalogDefinitionCount,
                _visualResolverTileCount,
                _visualLegacyTileCount,
                _visualVariantTileCount,
                _visualAnimationTileCount,
                _visualTransitionTileCount,
                _visualResolverFallbackCount,
                _coverageChecked,
                _coverageComplete,
                _coverageRequiredTileCount,
                _coverageCoveredTileCount,
                _coverageMissingTileCount,
                _coverageFirstMissingTileId,
                animateTerrainTiles,
                _lastTerrainAnimationVisualTimeSeconds,
                terrainAnimationRefreshSeconds,
                _lastAnimatedTerrainChunkCount,
                _lastTerrainAnimationRefreshQueued,
                usedFallbackUv,
                _uvMissingTileCount,
                _uvFirstMissingTileId,
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
                ", viewportCulling=" + _lastDiagnostics.ViewportCullingEnabled +
                ", visibleRect=" + _lastDiagnostics.VisibleRectMinX + "," +
                _lastDiagnostics.VisibleRectMinY + "->" +
                _lastDiagnostics.VisibleRectMaxXExclusive + "," +
                _lastDiagnostics.VisibleRectMaxYExclusive +
                ", visibleChunks=" + _lastDiagnostics.VisibleChunkCount +
                ", culledDirtyChunks=" + _lastDiagnostics.CulledDirtyChunkCount +
                ", disabledOutsideViewport=" + _lastDiagnostics.DisabledOutsideViewportChunkCount +
                ", visualCatalogJson=" + _lastDiagnostics.HasTerrainVisualCatalogJson +
                ", visualCatalogParsed=" + _lastDiagnostics.TerrainVisualCatalogParsed +
                ", visualDefinitions=" + _lastDiagnostics.TerrainVisualCatalogDefinitionCount +
                ", visualResolver=" + _lastDiagnostics.UsedTerrainVisualResolver +
                ", visualResolverTiles=" + _lastDiagnostics.VisualResolverTileCount +
                ", legacyVisualTiles=" + _lastDiagnostics.LegacyVisualTileCount +
                ", visualVariants=" + _lastDiagnostics.VisualVariantTileCount +
                ", visualAnimations=" + _lastDiagnostics.VisualAnimationTileCount +
                ", visualTransitions=" + _lastDiagnostics.VisualTransitionTileCount +
                ", visualResolverFallbacks=" + _lastDiagnostics.VisualResolverFallbackCount +
                ", coverageChecked=" + _lastDiagnostics.TerrainVisualCoverageChecked +
                ", coverageComplete=" + _lastDiagnostics.TerrainVisualCoverageComplete +
                ", coverageRequired=" + _lastDiagnostics.TerrainVisualCoverageRequiredTileCount +
                ", coverageCovered=" + _lastDiagnostics.TerrainVisualCoverageCoveredTileCount +
                ", coverageMissing=" + _lastDiagnostics.TerrainVisualCoverageMissingTileCount +
                ", coverageFirstMissing=" + _lastDiagnostics.TerrainVisualCoverageFirstMissingTileId +
                ", terrainAnimationEnabled=" + _lastDiagnostics.TerrainAnimationEnabled +
                ", terrainAnimationTime=" + _lastDiagnostics.TerrainAnimationVisualTimeSeconds +
                ", terrainAnimationRefreshSeconds=" + _lastDiagnostics.TerrainAnimationRefreshSeconds +
                ", animatedTerrainChunks=" + _lastDiagnostics.AnimatedTerrainChunkCount +
                ", terrainAnimationRefreshQueued=" + _lastDiagnostics.TerrainAnimationRefreshQueued +
                ", fallbackUv=" + _lastDiagnostics.UsedFallbackUv +
                ", missingUvTiles=" + _lastDiagnostics.MissingUvTileCount +
                ", firstMissingUvTileId=" + _lastDiagnostics.FirstMissingUvTileId +
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

        private ArcGraphTerrainTileUvMap CreateUvMap(Material material)
        {
            if (TryCreateCatalogUvMap(material, out var catalogUvMap))
                return catalogUvMap;

            int tilePixels = 32;

            Texture texture = material != null
                ? material.mainTexture
                : null;

            int atlasWidth = texture != null
                ? texture.width
                : tilePixels;
            int atlasHeight = texture != null
                ? texture.height
                : tilePixels;

            return new ArcGraphTerrainTileUvMap(atlasWidth, atlasHeight, tilePixels);
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
            _uvMissingTileCount = 0;
            _uvFirstMissingTileId = -1;
            _lastVisibleChunkCount = 0;
            _lastCulledDirtyChunkCount = 0;
            _lastDisabledOutsideViewportChunkCount = 0;
            _visualHasTerrainVisualCatalogJson = false;
            _visualTerrainVisualCatalogParsed = false;
            _visualUsedResolver = false;
            _visualTerrainVisualCatalogDefinitionCount = 0;
            _visualResolverTileCount = 0;
            _visualLegacyTileCount = 0;
            _visualVariantTileCount = 0;
            _visualAnimationTileCount = 0;
            _visualTransitionTileCount = 0;
            _visualResolverFallbackCount = 0;
            _coverageChecked = false;
            _coverageComplete = false;
            _coverageRequiredTileCount = 0;
            _coverageCoveredTileCount = 0;
            _coverageMissingTileCount = 0;
            _coverageFirstMissingTileId = -1;
            _lastAnimatedTerrainChunkCount = _animatedTerrainChunks.Count;
            _lastTerrainAnimationRefreshQueued = false;
            _lastTerrainAnimationVisualTimeSeconds = _terrainAnimationClock.VisualTimeSeconds;
        }

        private ArcGraphTerrainVisualBuildOptions CreateVisualBuildOptions()
        {
            ArcGraphTerrainVisualCatalog catalog = GetOrParseTerrainVisualCatalog();
            if (catalog == null || catalog.DefinitionCount <= 0)
                return ArcGraphTerrainVisualBuildOptions.CreateLegacyOnly();

            UpdateVisualCoverageDiagnostics(catalog);

            return ArcGraphTerrainVisualBuildOptions.CreateWithCatalog(
                catalog,
                _terrainAnimationClock.VisualTimeSeconds);
        }

        private void UpdateVisualCoverageDiagnostics(ArcGraphTerrainVisualCatalog visualCatalog)
        {
            var analyzer = new ArcGraphTerrainVisualCoverageAnalyzer();
            ArcGraphTerrainVisualCoverageDiagnostics coverage = analyzer.Analyze(
                visualCatalog,
                _terrainCatalog);

            _coverageChecked = coverage.HasVisualCatalog;
            _coverageComplete = coverage.IsFullyCovered;
            _coverageRequiredTileCount = coverage.RequiredTileCount;
            _coverageCoveredTileCount = coverage.CoveredTileCount;
            _coverageMissingTileCount = coverage.MissingTileCount;
            _coverageFirstMissingTileId = coverage.FirstMissingTileId;
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

        private ArcGraphTerrainVisualCatalog GetOrParseTerrainVisualCatalog()
        {
            _visualHasTerrainVisualCatalogJson = terrainVisualCatalogJson != null;

            string json = terrainVisualCatalogJson != null
                ? terrainVisualCatalogJson.text
                : null;

            if (string.IsNullOrWhiteSpace(json))
                return null;

            // Anche il catalogo visuale viene parsato solo quando cambia il testo.
            // In questo modo il renderer puo' usarlo per molti frame senza pagare
            // continuamente il costo di conversione JSON -> strutture runtime.
            if (_terrainVisualCatalog != null && _terrainVisualCatalogSourceText == json)
            {
                _visualTerrainVisualCatalogParsed = true;
                _visualTerrainVisualCatalogDefinitionCount = _terrainVisualCatalog.DefinitionCount;
                return _terrainVisualCatalog;
            }

            if (!ArcGraphTerrainVisualCatalogJson.TryParse(json, out _terrainVisualCatalog))
            {
                _terrainVisualCatalog = null;
                _terrainVisualCatalogSourceText = null;
                return null;
            }

            _terrainVisualCatalogSourceText = json;
            _visualTerrainVisualCatalogParsed = true;
            _visualTerrainVisualCatalogDefinitionCount = _terrainVisualCatalog.DefinitionCount;
            return _terrainVisualCatalog;
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
