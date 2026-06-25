namespace Arcontio.View.ArcGraph
{
    // =============================================================================
    // ArcGraphMinimalRuntimeCoordinator
    // =============================================================================
    /// <summary>
    /// <para>
    /// Coordinator C# passivo per il percorso runtime minimo ArcGraph.
    /// </para>
    ///
    /// <para><b>Principio architetturale: orchestratore grafico minimo, non God Manager</b></para>
    /// <para>
    /// Il coordinator riceve un <c>ArcGraphRuntimeContext</c> gia' costruito da un
    /// adapter esterno, inizializza o riusa un <c>ArcGraphBootstrapRuntime</c>,
    /// aggiorna snapshot e costruisce una <c>ArcGraphRenderQueue</c> actor/object.
    /// Non e' un <c>MonoBehaviour</c>, non crea <c>GameObject</c>, non carica asset,
    /// non legge <c>SimulationHost</c>, non cerca <c>MapGridWorldView</c>, non invia
    /// comandi e non possiede UI o DevTools.
    /// </para>
    ///
    /// <para><b>Struttura interna:</b></para>
    /// <list type="bullet">
    ///   <item><b>_runtime</b>: bootstrap ArcGraph riusabile tra frame coerenti.</item>
    ///   <item><b>_renderQueue</b>: queue actor/object riusata e leggibile dal chiamante.</item>
    ///   <item><b>Process</b>: valida gate, context, runtime e queue.</item>
    ///   <item><b>EnsureRuntime</b>: inizializza o ricrea il runtime se cambiano sorgenti.</item>
    ///   <item><b>BuildActorObjectQueue</b>: compone actor/object tramite builder esistenti.</item>
    /// </list>
    /// </summary>
    public sealed class ArcGraphMinimalRuntimeCoordinator
    {
        private readonly ArcGraphRenderQueue _renderQueue = new();
        private readonly ArcGraphRenderQueueBuilder _queueBuilder = new();

        private ArcGraphBootstrapRuntime _runtime;
        private ArcGraphRuntimeContext _runtimeContext;
        private ArcGraphMinimalRuntimeCoordinatorDiagnostics _lastDiagnostics;

        public ArcGraphBootstrapRuntime Runtime => _runtime;
        public ArcGraphRenderQueue RenderQueue => _renderQueue;
        public ArcGraphMinimalRuntimeCoordinatorDiagnostics LastDiagnostics => _lastDiagnostics;

        // =============================================================================
        // Process
        // =============================================================================
        /// <summary>
        /// <para>
        /// Processa un frame del percorso runtime minimo ArcGraph.
        /// </para>
        ///
        /// <para><b>Sequenza intenzionale</b></para>
        /// <para>
        /// Il metodo prima verifica gate e context, poi assicura il bootstrap
        /// riusabile, poi opzionalmente rinfresca gli snapshot e infine costruisce
        /// la queue actor/object se richiesta. Ogni uscita anticipata produce
        /// diagnostica piatta e leggibile.
        /// </para>
        /// </summary>
        public ArcGraphMinimalRuntimeCoordinatorDiagnostics Process(
            ArcGraphMinimalRuntimeCoordinatorFrame frame)
        {
            if (frame == null)
            {
                _renderQueue.Clear();
                return StoreDiagnostics(null, false, false, false, false, false, false, "FrameMissing");
            }

            if (!frame.IsCoordinatorEnabled)
            {
                _renderQueue.Clear();
                return StoreDiagnostics(frame, false, false, false, false, false, false, "CoordinatorDisabled");
            }

            if (!frame.HasContext)
            {
                _renderQueue.Clear();
                return StoreDiagnostics(frame, false, false, false, false, false, false, "RuntimeContextMissing");
            }

            if (!frame.HasAnyRuntimeData)
            {
                _renderQueue.Clear();
                return StoreDiagnostics(frame, false, false, false, false, false, false, "RuntimeContextEmpty");
            }

            bool recreatedRuntime = false;
            bool initialized = EnsureRuntime(frame.Context, out recreatedRuntime);
            if (!initialized)
            {
                _renderQueue.Clear();
                return StoreDiagnostics(frame, false, recreatedRuntime, false, false, false, false, "RuntimeInitializeFailed");
            }

            bool refreshed = false;
            if (frame.ShouldRefreshSnapshots && _runtime != null)
            {
                // Il terreno viene popolato durante Initialize e resta cache statica
                // finche' non cambia la sorgente MapGrid. Nel frame loop ordinario
                // aggiorniamo solo actor/oggetti, altrimenti una vista con molti
                // tile pagherebbe una scansione completa della mappa ogni frame.
                refreshed = _runtime.RefreshDynamicSnapshots();
            }

            bool hasTerrainLayer = HasLayer<ArcGraphTerrainLayer>();
            bool hasActorLayer = TryGetLayer(out ArcGraphActorLayer actorLayer);
            bool hasObjectLayer = TryGetLayer(out ArcGraphObjectLayer objectLayer);

            bool builtQueue = false;
            if (frame.ShouldBuildActorObjectQueue)
            {
                if (!hasActorLayer || !hasObjectLayer)
                {
                    _renderQueue.Clear();
                    return StoreDiagnostics(frame, true, recreatedRuntime, refreshed, hasTerrainLayer, hasActorLayer, hasObjectLayer, "ActorObjectLayersMissing");
                }

                BuildActorObjectQueue(actorLayer, objectLayer);
                builtQueue = true;
            }
            else
            {
                // Non puliamo la queue quando il frame e' valido ma il chiamante
                // decide di non ricostruirla in questo passaggio. Nel runtime scena
                // actor/oggetti vengono refreshati a intervalli per evitare lavoro
                // inutile ogni frame; se qui svuotassimo il buffer, input, hover e
                // selection leggerebbero una queue vuota tra un refresh e il
                // successivo pur vedendo ancora sprite materializzati in scena.
                // La pulizia resta confinata ai gate invalidi sopra e a Dispose().
            }

            string reason = ResolveSuccessReason(frame, refreshed, builtQueue);
            return StoreDiagnostics(frame, true, recreatedRuntime, refreshed, hasTerrainLayer, hasActorLayer, hasObjectLayer, reason, builtQueue);
        }

        // =============================================================================
        // Dispose
        // =============================================================================
        /// <summary>
        /// <para>
        /// Rilascia il bootstrap ArcGraph e pulisce la queue derivata.
        /// </para>
        ///
        /// <para><b>Cleanup confinato</b></para>
        /// <para>
        /// Il cleanup non distrugge oggetti Unity e non modifica MapGrid o World.
        /// Libera solo stato derivato posseduto dal coordinator.
        /// </para>
        /// </summary>
        public void Dispose()
        {
            _runtime?.Dispose();
            _runtime = null;
            _runtimeContext = null;
            _renderQueue.Clear();
        }

        private bool EnsureRuntime(
            ArcGraphRuntimeContext context,
            out bool recreatedRuntime)
        {
            // Se le sorgenti sono identiche, il runtime gia' inizializzato resta
            // valido. Questo evita di ricreare layer e snapshot a ogni frame quando
            // il chiamante ripassa un context equivalente.
            recreatedRuntime = false;

            if (_runtime != null && _runtime.IsInitialized && HasSameContextSources(_runtimeContext, context))
                return true;

            // Se config, mappa o world cambiano riferimento, il runtime derivato
            // viene ricreato. Questo copre il caso importante del load snapshot, in
            // cui MapGridWorldView puo' puntare a una nuova istanza di World.
            Dispose();
            recreatedRuntime = true;

            _runtimeContext = context;
            _runtime = new ArcGraphBootstrapRuntime();
            return _runtime.Initialize(
                context,
                CreateRuntimeOptions());
        }

        private void BuildActorObjectQueue(
            ArcGraphActorLayer actorLayer,
            ArcGraphObjectLayer objectLayer)
        {
            ArcGraphZoomLodProfile lodProfile = ArcGraphZoomLodPolicy.ResolveFullDetail();
            _queueBuilder.Build(actorLayer, objectLayer, lodProfile, _renderQueue);
        }

        // =============================================================================
        // CreateRuntimeOptions
        // =============================================================================
        /// <summary>
        /// <para>
        /// Crea le opzioni del coordinator runtime minimo includendo i layer
        /// ambientali passivi.
        /// </para>
        ///
        /// <para><b>Principio architetturale: feed ambientale senza rendering forzato</b></para>
        /// <para>
        /// I layer placeholder vengono registrati per ricevere snapshot da World,
        /// ma questo non abilita automaticamente un renderer vegetazione. Il
        /// coordinator conserva quindi il feed biosfera disponibile senza cambiare
        /// la policy scene-side di materializzazione grafica.
        /// </para>
        /// </summary>
        private static ArcGraphBootstrapOptions CreateRuntimeOptions()
        {
            var options = ArcGraphBootstrapOptions.CreateDefault();
            options.IncludeFuturePlaceholderLayers = true;
            return options;
        }

        private bool HasLayer<TLayer>()
            where TLayer : class, IArcGraphLayer
        {
            return TryGetLayer<TLayer>(out _);
        }

        private bool TryGetLayer<TLayer>(out TLayer layer)
            where TLayer : class, IArcGraphLayer
        {
            layer = null;
            return _runtime != null
                   && _runtime.LayerStack != null
                   && _runtime.LayerStack.TryGetLayer(out layer);
        }

        private ArcGraphMinimalRuntimeCoordinatorDiagnostics StoreDiagnostics(
            ArcGraphMinimalRuntimeCoordinatorFrame frame,
            bool didInitializeRuntime,
            bool didRecreateRuntime,
            bool didRefreshSnapshots,
            bool hasTerrainLayer,
            bool hasActorLayer,
            bool hasObjectLayer,
            string reason,
            bool didBuildActorObjectQueue = false)
        {
            // La diagnostica fotografa solo dati derivati. Non espone il World, la
            // MapGridData o layer mutabili: il chiamante vede contatori e ragioni,
            // non ottiene nuove autorita' di modifica.
            _lastDiagnostics = new ArcGraphMinimalRuntimeCoordinatorDiagnostics(
                frame != null && frame.IsCoordinatorEnabled,
                frame != null && frame.HasContext,
                frame != null && frame.HasConfig,
                frame != null && frame.HasMap,
                frame != null && frame.HasWorld,
                didInitializeRuntime,
                didRecreateRuntime,
                didRefreshSnapshots,
                hasTerrainLayer,
                hasActorLayer,
                hasObjectLayer,
                didBuildActorObjectQueue,
                _runtime?.TerrainSnapshots?.Count ?? 0,
                _runtime?.ActorSnapshots?.Count ?? 0,
                _runtime?.ObjectSnapshots?.Count ?? 0,
                _renderQueue.ActorItems.Count,
                _renderQueue.ObjectItems.Count,
                _renderQueue.Entries.Count,
                frame != null ? frame.SourceTick : -1,
                reason);

            return _lastDiagnostics;
        }

        private static bool HasSameContextSources(
            ArcGraphRuntimeContext current,
            ArcGraphRuntimeContext next)
        {
            // Il confronto e' per riferimento, non per contenuto. Non vogliamo
            // scansionare World o MapGridData: se le sorgenti sono le stesse, il
            // chiamante puo' chiedere RefreshSnapshots; se cambiano riferimento, il
            // runtime viene ricreato.
            if (current == null || next == null)
                return false;

            return ReferenceEquals(current.World, next.World)
                   && current.MapWidthCells == next.MapWidthCells
                   && current.MapHeightCells == next.MapHeightCells
                   && current.TileSizeWorld == next.TileSizeWorld
                   && current.ChunkSizeCells == next.ChunkSizeCells
                   && current.DefaultNpcSpriteKey == next.DefaultNpcSpriteKey;
        }

        private static string ResolveSuccessReason(
            ArcGraphMinimalRuntimeCoordinatorFrame frame,
            bool refreshed,
            bool builtQueue)
        {
            if (builtQueue)
                return "ActorObjectQueueBuilt";

            if (refreshed)
                return "SnapshotsRefreshed";

            return frame.ShouldRefreshSnapshots
                ? "SnapshotsRefreshRequestedButNoChange"
                : "RuntimeReady";
        }
    }
}
