namespace Arcontio.View.ArcGraph
{
    // =============================================================================
    // ArcGraphMinimalRuntimeCoordinatorDiagnostics
    // =============================================================================
    /// <summary>
    /// <para>
    /// Diagnostica sintetica del coordinator runtime minimo ArcGraph.
    /// </para>
    ///
    /// <para><b>Principio architetturale: spiegare l'orchestrazione senza scena</b></para>
    /// <para>
    /// La diagnostica espone solo prerequisiti, gate, refresh snapshot, layer
    /// disponibili e conteggi queue. Non contiene riferimenti Unity, non contiene
    /// oggetti scena e non rappresenta stato simulativo autoritativo.
    /// </para>
    ///
    /// <para><b>Struttura interna:</b></para>
    /// <list type="bullet">
    ///   <item><b>IsCoordinatorEnabled</b>: gate principale ricevuto dal frame.</item>
    ///   <item><b>HasContext/Config/Map/World</b>: sorgenti runtime disponibili.</item>
    ///   <item><b>DidInitializeRuntime</b>: bootstrap inizializzato o riusato.</item>
    ///   <item><b>DidRefreshSnapshots</b>: snapshot ricopiati in questo passaggio.</item>
    ///   <item><b>HasTerrainLayer/ActorLayer/ObjectLayer</b>: layer disponibili.</item>
    ///   <item><b>DidBuildActorObjectQueue</b>: queue actor/object costruita.</item>
    ///   <item><b>Queue*</b>: conteggi della queue prodotta.</item>
    ///   <item><b>Reason</b>: esito sintetico leggibile.</item>
    /// </list>
    /// </summary>
    public readonly struct ArcGraphMinimalRuntimeCoordinatorDiagnostics
    {
        public readonly bool IsCoordinatorEnabled;
        public readonly bool HasContext;
        public readonly bool HasConfig;
        public readonly bool HasMap;
        public readonly bool HasWorld;
        public readonly bool DidInitializeRuntime;
        public readonly bool DidRecreateRuntime;
        public readonly bool DidRefreshSnapshots;
        public readonly bool HasTerrainLayer;
        public readonly bool HasActorLayer;
        public readonly bool HasObjectLayer;
        public readonly bool DidBuildActorObjectQueue;
        public readonly int TerrainSnapshotCount;
        public readonly int ActorSnapshotCount;
        public readonly int ObjectSnapshotCount;
        public readonly int QueueActorCount;
        public readonly int QueueObjectCount;
        public readonly int QueueEntryCount;
        public readonly long SourceTick;
        public readonly string Reason;

        public ArcGraphMinimalRuntimeCoordinatorDiagnostics(
            bool isCoordinatorEnabled,
            bool hasContext,
            bool hasConfig,
            bool hasMap,
            bool hasWorld,
            bool didInitializeRuntime,
            bool didRecreateRuntime,
            bool didRefreshSnapshots,
            bool hasTerrainLayer,
            bool hasActorLayer,
            bool hasObjectLayer,
            bool didBuildActorObjectQueue,
            int terrainSnapshotCount,
            int actorSnapshotCount,
            int objectSnapshotCount,
            int queueActorCount,
            int queueObjectCount,
            int queueEntryCount,
            long sourceTick,
            string reason)
        {
            IsCoordinatorEnabled = isCoordinatorEnabled;
            HasContext = hasContext;
            HasConfig = hasConfig;
            HasMap = hasMap;
            HasWorld = hasWorld;
            DidInitializeRuntime = didInitializeRuntime;
            DidRecreateRuntime = didRecreateRuntime;
            DidRefreshSnapshots = didRefreshSnapshots;
            HasTerrainLayer = hasTerrainLayer;
            HasActorLayer = hasActorLayer;
            HasObjectLayer = hasObjectLayer;
            DidBuildActorObjectQueue = didBuildActorObjectQueue;
            TerrainSnapshotCount = NormalizeCount(terrainSnapshotCount);
            ActorSnapshotCount = NormalizeCount(actorSnapshotCount);
            ObjectSnapshotCount = NormalizeCount(objectSnapshotCount);
            QueueActorCount = NormalizeCount(queueActorCount);
            QueueObjectCount = NormalizeCount(queueObjectCount);
            QueueEntryCount = NormalizeCount(queueEntryCount);
            SourceTick = sourceTick;
            Reason = string.IsNullOrWhiteSpace(reason) ? "None" : reason;
        }

        private static int NormalizeCount(int value)
        {
            return value < 0 ? 0 : value;
        }
    }
}
