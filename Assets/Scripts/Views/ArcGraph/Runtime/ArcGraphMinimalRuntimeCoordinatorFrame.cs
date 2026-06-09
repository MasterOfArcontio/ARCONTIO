namespace Arcontio.View.ArcGraph
{
    // =============================================================================
    // ArcGraphMinimalRuntimeCoordinatorFrame
    // =============================================================================
    /// <summary>
    /// <para>
    /// Input value-only per un singolo passaggio del coordinator runtime minimo
    /// ArcGraph.
    /// </para>
    ///
    /// <para><b>Principio architetturale: orchestrazione dichiarata, non ricerca scena</b></para>
    /// <para>
    /// Il frame contiene decisioni gia' prese dal chiamante: context runtime,
    /// abilitazione del coordinator, richiesta di refresh snapshot, richiesta di
    /// costruzione della queue actor/object e livello zoom. Il coordinator non deve
    /// cercare <c>MapGridBootstrap</c>, non deve leggere <c>SimulationHost</c> e non
    /// deve decidere da solo quali tool accendere.
    /// </para>
    ///
    /// <para><b>Struttura interna:</b></para>
    /// <list type="bullet">
    ///   <item><b>Context</b>: sorgenti runtime ricevute esplicitamente.</item>
    ///   <item><b>IsCoordinatorEnabled</b>: gate principale del coordinator.</item>
    ///   <item><b>ShouldRefreshSnapshots</b>: richiede ricopia degli snapshot.</item>
    ///   <item><b>ShouldBuildActorObjectQueue</b>: richiede queue actor/object.</item>
    ///   <item><b>ZoomLevel</b>: livello zoom usato per risolvere LOD.</item>
    ///   <item><b>SourceTick</b>: tick opzionale usato solo in diagnostica.</item>
    /// </list>
    /// </summary>
    public sealed class ArcGraphMinimalRuntimeCoordinatorFrame
    {
        public ArcGraphRuntimeContext Context { get; }
        public bool IsCoordinatorEnabled { get; }
        public bool ShouldRefreshSnapshots { get; }
        public bool ShouldBuildActorObjectQueue { get; }
        public int ZoomLevel { get; }
        public long SourceTick { get; }

        public bool HasContext => Context != null;
        public bool HasConfig => Context != null && Context.HasConfig;
        public bool HasMap => Context != null && Context.HasMap;
        public bool HasWorld => Context != null && Context.HasWorld;
        public bool HasAnyRuntimeData => Context != null && Context.HasAnyRuntimeData;

        // =============================================================================
        // ArcGraphMinimalRuntimeCoordinatorFrame
        // =============================================================================
        /// <summary>
        /// <para>
        /// Costruisce un frame runtime minimo normalizzando i valori di ingresso.
        /// </para>
        ///
        /// <para><b>Normalizzazione minima</b></para>
        /// <para>
        /// Il livello zoom viene riportato almeno a 1. Il context null resta null:
        /// in questo modo la diagnostica puo' distinguere un coordinator spento da
        /// un coordinator acceso ma privo di sorgenti.
        /// </para>
        /// </summary>
        public ArcGraphMinimalRuntimeCoordinatorFrame(
            ArcGraphRuntimeContext context,
            bool isCoordinatorEnabled,
            bool shouldRefreshSnapshots,
            bool shouldBuildActorObjectQueue,
            int zoomLevel,
            long sourceTick = -1)
        {
            Context = context;
            IsCoordinatorEnabled = isCoordinatorEnabled;
            ShouldRefreshSnapshots = shouldRefreshSnapshots;
            ShouldBuildActorObjectQueue = shouldBuildActorObjectQueue;
            ZoomLevel = zoomLevel > 0 ? zoomLevel : 1;
            SourceTick = sourceTick;
        }

        // =============================================================================
        // CreateDisabled
        // =============================================================================
        /// <summary>
        /// <para>
        /// Crea un frame esplicitamente disattivato.
        /// </para>
        /// </summary>
        public static ArcGraphMinimalRuntimeCoordinatorFrame CreateDisabled()
        {
            return new ArcGraphMinimalRuntimeCoordinatorFrame(
                context: null,
                isCoordinatorEnabled: false,
                shouldRefreshSnapshots: false,
                shouldBuildActorObjectQueue: false,
                zoomLevel: 1);
        }
    }
}
