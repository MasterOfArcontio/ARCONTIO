using System.Collections.Generic;

namespace Arcontio.View.ArcGraph
{
    // =============================================================================
    // ArcGraphBootstrapRuntime
    // =============================================================================
    /// <summary>
    /// <para>
    /// Nucleo C# passivo del bootstrap controllato ArcGraph.
    /// </para>
    ///
    /// <para><b>Principio architetturale: bootstrap interno senza renderer</b></para>
    /// <para>
    /// Questo runtime inizializza stato grafico, layer passivi, adapter e cache
    /// snapshot. Non e' un <c>MonoBehaviour</c>, non crea <c>GameObject</c>, non
    /// carica asset, non crea renderer Unity, non legge globali e non muta il
    /// <c>World</c>. Serve a dimostrare che ArcGraph puo' accendersi come sistema
    /// interno controllato prima di diventare un renderer produttivo.
    /// </para>
    ///
    /// <para><b>Struttura interna:</b></para>
    /// <list type="bullet">
    ///   <item><b>_renderState</b>: stato grafico condiviso.</item>
    ///   <item><b>_layerStack</b>: registro dei layer passivi.</item>
    ///   <item><b>_adapter</b>: ponte read-only verso snapshot.</item>
    ///   <item><b>_context</b>: sorgenti dati ricevute esplicitamente.</item>
    ///   <item><b>_terrainSnapshots/_objectSnapshots/_actorSnapshots</b>: cache interne copiate.</item>
    /// </list>
    /// </summary>
    public sealed class ArcGraphBootstrapRuntime
    {
        private readonly List<ArcGraphTerrainCellSnapshot> _terrainSnapshots = new();
        private readonly List<ArcGraphObjectVisualSnapshot> _objectSnapshots = new();
        private readonly List<ArcGraphActorVisualSnapshot> _actorSnapshots = new();

        private ArcGraphRenderState _renderState;
        private ArcGraphLayerStack _layerStack;
        private ArcGraphWorldAdapter _adapter;
        private ArcGraphRuntimeContext _context;
        private ArcGraphBootstrapOptions _options;
        private ArcGraphBootstrapDiagnostics _diagnostics;

        public ArcGraphRenderState RenderState => _renderState;
        public ArcGraphLayerStack LayerStack => _layerStack;
        public ArcGraphWorldAdapter Adapter => _adapter;
        public ArcGraphRuntimeContext Context => _context;
        public ArcGraphBootstrapOptions Options => _options;
        public ArcGraphBootstrapDiagnostics Diagnostics => _diagnostics;

        public IReadOnlyList<ArcGraphTerrainCellSnapshot> TerrainSnapshots => _terrainSnapshots;
        public IReadOnlyList<ArcGraphObjectVisualSnapshot> ObjectSnapshots => _objectSnapshots;
        public IReadOnlyList<ArcGraphActorVisualSnapshot> ActorSnapshots => _actorSnapshots;

        public bool IsInitialized => _diagnostics.IsInitialized;
        public bool IsDisposed => _diagnostics.IsDisposed;
        public bool DoesRenderAnything => false;

        // =============================================================================
        // ArcGraphBootstrapRuntime
        // =============================================================================
        /// <summary>
        /// <para>
        /// Crea un runtime bootstrap non inizializzato.
        /// </para>
        ///
        /// <para><b>Stato iniziale passivo</b></para>
        /// <para>
        /// Il costruttore non accende ArcGraph. Prepara soltanto la diagnostica
        /// iniziale, lasciando al chiamante la decisione esplicita di chiamare
        /// <c>Initialize</c>.
        /// </para>
        /// </summary>
        public ArcGraphBootstrapRuntime()
        {
            _diagnostics = BuildDiagnostics(ArcGraphBootstrapStatus.Uninitialized, "NotInitialized");
        }

        // =============================================================================
        // Initialize
        // =============================================================================
        /// <summary>
        /// <para>
        /// Inizializza il nucleo interno ArcGraph secondo context e opzioni ricevute.
        /// </para>
        ///
        /// <para><b>Accensione esplicita</b></para>
        /// <para>
        /// Il metodo crea solo oggetti C# passivi: render state, layer stack,
        /// adapter e liste snapshot. Se la policy e' <c>Disabled</c>, lascia il
        /// bootstrap spento con diagnostica leggibile. Se il context e' parziale e
        /// le opzioni lo consentono, inizializza comunque i layer senza popolare le
        /// parti mancanti.
        /// </para>
        /// </summary>
        public bool Initialize(
            ArcGraphRuntimeContext context = null,
            ArcGraphBootstrapOptions options = null)
        {
            // Idempotenza: una seconda chiamata non duplica layer, adapter o cache.
            if (IsInitialized)
            {
                _diagnostics = BuildDiagnostics(ArcGraphBootstrapStatus.Initialized, "AlreadyInitialized");
                return true;
            }

            // Dopo Dispose il runtime non viene riciclato: si crea un nuovo bootstrap.
            if (IsDisposed)
            {
                _diagnostics = BuildDiagnostics(ArcGraphBootstrapStatus.Failed, "DisposedRuntimeCannotInitialize");
                return false;
            }

            _options = options ?? ArcGraphBootstrapOptions.CreateDefault();
            _context = context ?? ArcGraphRuntimeContext.Empty();

            // Policy esplicita: Disabled non crea nemmeno lo stato interno.
            if (_options.ActivationMode == ArcGraphBootstrapActivationMode.Disabled)
            {
                _diagnostics = BuildDiagnostics(ArcGraphBootstrapStatus.Disabled, "ActivationModeDisabled");
                return false;
            }

            // Se il chiamante richiede context completo, la validazione avviene prima di allocare layer.
            if (!_options.AllowPartialRuntimeContext && !_context.HasCompleteRuntimeData)
            {
                _diagnostics = BuildDiagnostics(ArcGraphBootstrapStatus.Failed, "RuntimeContextIncomplete");
                return false;
            }

            _diagnostics = BuildDiagnostics(ArcGraphBootstrapStatus.Initializing, "Initializing");

            // Parametri grafici: usiamo la config quando esiste, altrimenti fallback dalle opzioni.
            float tileSizeWorld = ResolveTileSizeWorld(_context, _options);
            int chunkSizeCells = ResolveChunkSizeCells(_context, _options);

            _renderState = new ArcGraphRenderState(
                _options.VisibleZLevel,
                tileSizeWorld,
                chunkSizeCells);

            _layerStack = new ArcGraphLayerStack();
            _layerStack.RegisterDefaultFoundationLayers();

            // I placeholder futuri restano esclusi dal default e si registrano solo su flag esplicito.
            if (_options.IncludeFuturePlaceholderLayers)
                _layerStack.RegisterFuturePlaceholderLayers();

            _layerStack.InitializeAll(_renderState);

            _adapter = new ArcGraphWorldAdapter(ResolveDefaultNpcSpriteKey(_context));

            if (_options.PopulateInitialSnapshots)
                PopulateSnapshotsFromContext();

            _diagnostics = BuildDiagnostics(ArcGraphBootstrapStatus.Initialized, "InitializedInternalStateOnly");
            return true;
        }

        // =============================================================================
        // RefreshSnapshots
        // =============================================================================
        /// <summary>
        /// <para>
        /// Ricopia gli snapshot dalle sorgenti presenti nel context corrente.
        /// </para>
        ///
        /// <para><b>Refresh passivo</b></para>
        /// <para>
        /// Il refresh non legge globali e non muta sorgenti runtime. Svuota e
        /// ripopola solo le liste interne e le cache dei layer ArcGraph gia'
        /// inizializzati.
        /// </para>
        /// </summary>
        public bool RefreshSnapshots()
        {
            if (!IsInitialized)
            {
                _diagnostics = BuildDiagnostics(ArcGraphBootstrapStatus.Failed, "RefreshRequestedBeforeInitialize");
                return false;
            }

            PopulateSnapshotsFromContext();
            _diagnostics = BuildDiagnostics(ArcGraphBootstrapStatus.Initialized, "SnapshotsRefreshed");
            return true;
        }

        // =============================================================================
        // RefreshDynamicSnapshots
        // =============================================================================
        /// <summary>
        /// <para>
        /// Aggiorna solo snapshot dinamici di oggetti e attori, lasciando fermo il
        /// terreno gia' copiato al bootstrap iniziale.
        /// </para>
        ///
        /// <para><b>Principio architetturale: terreno statico, entita' dinamiche</b></para>
        /// <para>
        /// Nel runtime attuale il terreno viene letto da MapGrid come base visuale
        /// quasi statica, mentre NPC e oggetti possono cambiare a ogni frame/tick.
        /// Questo metodo evita la scansione completa della mappa per frame e
        /// mantiene aggiornati i layer che alimentano la render queue.
        /// </para>
        /// </summary>
        public bool RefreshDynamicSnapshots()
        {
            if (!IsInitialized)
            {
                _diagnostics = BuildDiagnostics(ArcGraphBootstrapStatus.Failed, "DynamicRefreshRequestedBeforeInitialize");
                return false;
            }

            PopulateDynamicSnapshotsFromContext();
            _diagnostics = BuildDiagnostics(ArcGraphBootstrapStatus.Initialized, "DynamicSnapshotsRefreshed");
            return true;
        }

        // =============================================================================
        // Dispose
        // =============================================================================
        /// <summary>
        /// <para>
        /// Rilascia layer e cache interne del bootstrap.
        /// </para>
        ///
        /// <para><b>Cleanup view-side</b></para>
        /// <para>
        /// Il cleanup non distrugge oggetti Unity, non modifica la MapGrid e non
        /// muta il <c>World</c>. Libera solo riferimenti e cache derivate create da
        /// questo runtime.
        /// </para>
        /// </summary>
        public void Dispose()
        {
            _layerStack?.DisposeAll();

            _terrainSnapshots.Clear();
            _objectSnapshots.Clear();
            _actorSnapshots.Clear();

            _renderState = null;
            _layerStack = null;
            _adapter = null;

            _diagnostics = BuildDiagnostics(ArcGraphBootstrapStatus.Disposed, "Disposed");
        }

        private void PopulateSnapshotsFromContext()
        {
            // Ogni lista viene svuotata prima del nuovo riempimento: le cache sono derivate.
            _terrainSnapshots.Clear();

            if (_context?.Map != null || _context?.World?.CellSurfaces != null)
            {
                _adapter.FillTerrainSnapshots(
                    _context.Map,
                    _context.World?.CellSurfaces,
                    _terrainSnapshots);

                if (_layerStack.TryGetLayer<ArcGraphTerrainLayer>(out var terrainLayer))
                    terrainLayer.ReplaceSnapshots(_terrainSnapshots, _renderState);
            }

            PopulateDynamicSnapshotsFromContext();
        }

        private void PopulateDynamicSnapshotsFromContext()
        {
            // Oggetti e attori sono la parte mobile del ponte MapGrid/World. Sono
            // aggiornati spesso, ma non devono invalidare il terrain renderer.
            _objectSnapshots.Clear();
            _actorSnapshots.Clear();

            if (_context?.World != null)
            {
                _adapter.FillObjectSnapshots(_context.World, _objectSnapshots);
                _adapter.FillActorSnapshots(_context.World, _actorSnapshots);

                if (_layerStack.TryGetLayer<ArcGraphObjectLayer>(out var objectLayer))
                {
                    // Oggetti e attori vengono renderizzati dalla queue actor/object,
                    // non dal renderer terrain. Non passiamo quindi il render state
                    // condiviso a questi layer, altrimenti ogni movimento NPC o
                    // refresh oggetto sporca chunk che il terrain renderer interpreta
                    // come terreno da ricostruire.
                    objectLayer.ReplaceSnapshots(_objectSnapshots, null);
                }

                if (_layerStack.TryGetLayer<ArcGraphActorLayer>(out var actorLayer))
                    actorLayer.ReplaceSnapshots(_actorSnapshots, null);
            }
        }

        private ArcGraphBootstrapDiagnostics BuildDiagnostics(
            ArcGraphBootstrapStatus status,
            string reason)
        {
            return new ArcGraphBootstrapDiagnostics(
                status,
                reason,
                _renderState != null,
                _layerStack != null,
                _adapter != null,
                _context != null && _context.HasAnyRuntimeData,
                _context != null && _context.HasConfig,
                _context != null && _context.HasMap,
                _context != null && _context.HasWorld,
                _layerStack != null ? _layerStack.Count : 0,
                _terrainSnapshots.Count,
                _objectSnapshots.Count,
                _actorSnapshots.Count);
        }

        private static float ResolveTileSizeWorld(
            ArcGraphRuntimeContext context,
            ArcGraphBootstrapOptions options)
        {
            // La config MapGrid resta la fonte piu' coerente quando e' presente.
            if (context?.Config != null && context.Config.tileSizeWorld > 0.0001f)
                return context.Config.tileSizeWorld;

            return options != null && options.DefaultTileSizeWorld > 0.0001f
                ? options.DefaultTileSizeWorld
                : 1f;
        }

        private static int ResolveChunkSizeCells(
            ArcGraphRuntimeContext context,
            ArcGraphBootstrapOptions options)
        {
            // La dimensione chunk eredita la MapGrid attuale, ma viene normalizzata.
            if (context?.Config != null && context.Config.chunkSize > 0)
                return context.Config.chunkSize;

            return options != null && options.DefaultChunkSizeCells > 0
                ? options.DefaultChunkSizeCells
                : 16;
        }

        private static string ResolveDefaultNpcSpriteKey(ArcGraphRuntimeContext context)
        {
            // L'adapter non carica sprite: conserva solo la chiave risolta.
            string configured = context?.Config?.npc?.spriteResourcePath;
            return string.IsNullOrWhiteSpace(configured)
                ? "MapGrid/Sprites/NPC_Astro"
                : configured;
        }
    }
}
