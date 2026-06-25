namespace Arcontio.View.ArcGraph
{
    // =============================================================================
    // ArcGraphBootstrapDiagnostics
    // =============================================================================
    /// <summary>
    /// <para>
    /// Snapshot diagnostico immutabile dello stato del bootstrap ArcGraph.
    /// </para>
    ///
    /// <para><b>Principio architetturale: osservabilita' senza UI e senza renderer</b></para>
    /// <para>
    /// In <c>v0.31</c> ArcGraph non disegna nulla. La diagnostica e' quindi il modo
    /// principale per verificare che il sistema sia stato acceso correttamente:
    /// layer creati, adapter presente, context ricevuto, snapshot copiati e garanzia
    /// esplicita che non esiste rendering produttivo.
    /// </para>
    ///
    /// <para><b>Struttura interna:</b></para>
    /// <list type="bullet">
    ///   <item><b>Status</b>: stato lifecycle sintetico.</item>
    ///   <item><b>Reason</b>: ragione leggibile dell'ultima transizione.</item>
    ///   <item><b>LayerCount</b>: layer registrati nello stack.</item>
    ///   <item><b>SnapshotCount</b>: copie interne prodotte dall'adapter.</item>
    ///   <item><b>DoesRenderAnything</b>: sempre false in v0.31.</item>
    /// </list>
    /// </summary>
    public readonly struct ArcGraphBootstrapDiagnostics
    {
        public ArcGraphBootstrapStatus Status { get; }
        public string Reason { get; }

        public bool HasRenderState { get; }
        public bool HasLayerStack { get; }
        public bool HasAdapter { get; }
        public bool HasRuntimeContext { get; }
        public bool HasMap { get; }
        public bool HasWorld { get; }
        public bool HasConfig { get; }

        public int LayerCount { get; }
        public int TerrainSnapshotCount { get; }
        public int ObjectSnapshotCount { get; }
        public int ActorSnapshotCount { get; }
        public int VegetationSnapshotCount { get; }

        public bool IsInitialized => Status == ArcGraphBootstrapStatus.Initialized;
        public bool IsDisposed => Status == ArcGraphBootstrapStatus.Disposed;
        public bool DoesRenderAnything => false;

        // =============================================================================
        // ArcGraphBootstrapDiagnostics
        // =============================================================================
        /// <summary>
        /// <para>
        /// Costruisce uno snapshot diagnostico completo.
        /// </para>
        ///
        /// <para><b>Costruzione centralizzata</b></para>
        /// <para>
        /// Il runtime bootstrap usa questo costruttore per lasciare una fotografia
        /// leggibile dopo ogni transizione importante. Lo snapshot non contiene
        /// riferimenti mutabili al <c>World</c>, alla mappa o ai layer.
        /// </para>
        /// </summary>
        public ArcGraphBootstrapDiagnostics(
            ArcGraphBootstrapStatus status,
            string reason,
            bool hasRenderState,
            bool hasLayerStack,
            bool hasAdapter,
            bool hasRuntimeContext,
            bool hasConfig,
            bool hasMap,
            bool hasWorld,
            int layerCount,
            int terrainSnapshotCount,
            int objectSnapshotCount,
            int actorSnapshotCount,
            int vegetationSnapshotCount = 0)
        {
            Status = status;
            Reason = string.IsNullOrWhiteSpace(reason) ? "None" : reason;

            HasRenderState = hasRenderState;
            HasLayerStack = hasLayerStack;
            HasAdapter = hasAdapter;
            HasRuntimeContext = hasRuntimeContext;
            HasConfig = hasConfig;
            HasMap = hasMap;
            HasWorld = hasWorld;

            LayerCount = layerCount;
            TerrainSnapshotCount = terrainSnapshotCount;
            ObjectSnapshotCount = objectSnapshotCount;
            ActorSnapshotCount = actorSnapshotCount;
            VegetationSnapshotCount = vegetationSnapshotCount < 0 ? 0 : vegetationSnapshotCount;
        }
    }
}
