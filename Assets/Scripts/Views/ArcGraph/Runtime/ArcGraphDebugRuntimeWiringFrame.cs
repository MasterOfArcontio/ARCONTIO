namespace Arcontio.View.ArcGraph
{
    // =============================================================================
    // ArcGraphDebugRuntimeWiringFrame
    // =============================================================================
    /// <summary>
    /// <para>
    /// Input value-only per un singolo tentativo di wiring runtime debug ArcGraph.
    /// </para>
    ///
    /// <para><b>Principio architetturale: decisioni gia' risolte dal chiamante</b></para>
    /// <para>
    /// Questo frame non legge input, non sceglie NPC attivo, non cerca il mondo e
    /// non conosce renderer concreti. Il chiamante fornisce context, NPC attivo,
    /// opzioni debug e richiesta di dispatch. Il coordinatore usa questi dati senza
    /// inventare policy proprie.
    /// </para>
    ///
    /// <para><b>Struttura interna:</b></para>
    /// <list type="bullet">
    ///   <item><b>Context</b>: sorgenti runtime ricevute esplicitamente.</item>
    ///   <item><b>ActiveNpcId</b>: NPC gia' scelto da un selector esterno.</item>
    ///   <item><b>Options</b>: flag Landmark/GVD/DT gia' decisi.</item>
    ///   <item><b>IsOverlayEnabled</b>: gate esterno del debug overlay.</item>
    ///   <item><b>ShouldDispatchToConsumer</b>: autorizza il passaggio al renderer.</item>
    ///   <item><b>SourceTick</b>: tick opzionale usato solo per diagnostica.</item>
    /// </list>
    /// </summary>
    public sealed class ArcGraphDebugRuntimeWiringFrame
    {
        public ArcGraphRuntimeContext Context { get; }
        public int ActiveNpcId { get; }
        public ArcGraphDebugOverlayRuntimeFeedOptions Options { get; }
        public bool IsOverlayEnabled { get; }
        public bool ShouldDispatchToConsumer { get; }
        public long SourceTick { get; }

        public bool HasContext => Context != null;
        public bool HasWorld => Context != null && Context.HasWorld;
        public bool HasActiveNpc => ActiveNpcId > 0;

        // =============================================================================
        // ArcGraphDebugRuntimeWiringFrame
        // =============================================================================
        /// <summary>
        /// <para>
        /// Costruisce un frame di wiring runtime debug.
        /// </para>
        ///
        /// <para><b>Normalizzazione minima</b></para>
        /// <para>
        /// Le opzioni null vengono sostituite dal default del feed. Il context null
        /// resta null, cosi' la diagnostica puo' distinguere tra context mancante e
        /// context presente ma senza World.
        /// </para>
        /// </summary>
        public ArcGraphDebugRuntimeWiringFrame(
            ArcGraphRuntimeContext context,
            int activeNpcId,
            ArcGraphDebugOverlayRuntimeFeedOptions options,
            bool isOverlayEnabled,
            bool shouldDispatchToConsumer,
            long sourceTick = -1)
        {
            Context = context;
            ActiveNpcId = activeNpcId;
            Options = options ?? ArcGraphDebugOverlayRuntimeFeedOptions.CreateDefault();
            IsOverlayEnabled = isOverlayEnabled;
            ShouldDispatchToConsumer = shouldDispatchToConsumer;
            SourceTick = sourceTick;
        }

        // =============================================================================
        // CreateDisabled
        // =============================================================================
        /// <summary>
        /// <para>
        /// Crea un frame esplicitamente disattivato.
        /// </para>
        ///
        /// <para>
        /// Questo helper rende semplice testare il gate iniziale senza costruire un
        /// World o un renderer finto.
        /// </para>
        /// </summary>
        public static ArcGraphDebugRuntimeWiringFrame CreateDisabled()
        {
            return new ArcGraphDebugRuntimeWiringFrame(
                context: null,
                activeNpcId: -1,
                options: null,
                isOverlayEnabled: false,
                shouldDispatchToConsumer: false);
        }
    }
}
