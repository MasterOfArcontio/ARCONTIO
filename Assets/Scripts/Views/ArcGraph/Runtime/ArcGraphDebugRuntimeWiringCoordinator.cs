namespace Arcontio.View.ArcGraph
{
    // =============================================================================
    // ArcGraphDebugRuntimeWiringCoordinator
    // =============================================================================
    /// <summary>
    /// <para>
    /// Coordinatore C# passivo tra context runtime, feed debug e consumer queue.
    /// </para>
    ///
    /// <para><b>Principio architetturale: punto di incontro controllato</b></para>
    /// <para>
    /// Questo coordinatore e' il contratto operativo minimo del futuro wiring. Non
    /// e' un <c>MonoBehaviour</c>, non cerca scene, non legge <c>SimulationHost</c>,
    /// non usa <c>MapGridWorldProvider</c>, non legge input e non sceglie NPC. Se il
    /// frame ricevuto e' valido, costruisce la queue tramite il feed e, solo se il
    /// chiamante lo richiede, la consegna a un consumer esplicito.
    /// </para>
    ///
    /// <para><b>Struttura interna:</b></para>
    /// <list type="bullet">
    ///   <item><b>_feed</b>: feed runtime debug riusato tra chiamate.</item>
    ///   <item><b>Process</b>: valida frame, costruisce queue e opzionalmente dispatcha.</item>
    ///   <item><b>CreateDiagnostics</b>: produce esito sintetico senza log verbosi.</item>
    /// </list>
    /// </summary>
    public sealed class ArcGraphDebugRuntimeWiringCoordinator
    {
        private readonly ArcGraphDebugOverlayRuntimeFeed _feed = new ArcGraphDebugOverlayRuntimeFeed();

        public ArcGraphDebugOverlayRuntimeFeed Feed => _feed;

        // =============================================================================
        // Process
        // =============================================================================
        /// <summary>
        /// <para>
        /// Processa un frame di wiring runtime debug.
        /// </para>
        ///
        /// <para><b>Sequenza intenzionale</b></para>
        /// <para>
        /// Prima vengono verificati gate e sorgenti. Poi il feed costruisce la queue.
        /// Solo dopo, se richiesto e se il consumer esiste, la queue viene consegnata.
        /// La consegna e' opzionale per permettere QA e conteggio senza rendering.
        /// </para>
        /// </summary>
        public ArcGraphDebugRuntimeWiringDiagnostics Process(
            ArcGraphDebugRuntimeWiringFrame frame,
            IArcGraphDebugOverlayQueueConsumer consumer = null)
        {
            if (frame == null)
                return CreateDiagnostics(null, false, false, consumer != null, 0, 0, "FrameMissing");

            if (!frame.IsOverlayEnabled)
                return CreateDiagnostics(frame, false, false, consumer != null, 0, 0, "OverlayDisabled");

            if (!frame.HasContext)
                return CreateDiagnostics(frame, false, false, consumer != null, 0, 0, "RuntimeContextMissing");

            if (!frame.HasWorld)
                return CreateDiagnostics(frame, false, false, consumer != null, 0, 0, "WorldMissing");

            ArcGraphDebugOverlayRuntimeFeedDiagnostics feedDiagnostics =
                _feed.BuildFromWorld(frame.Context.World, frame.ActiveNpcId, frame.Options);

            bool shouldDispatch = frame.ShouldDispatchToConsumer && consumer != null;
            if (shouldDispatch)
                consumer.RenderQueue(_feed.Queue);

            return CreateDiagnostics(
                frame,
                didBuildFeed: true,
                didDispatchToConsumer: shouldDispatch,
                wasConsumerProvided: consumer != null,
                queueItemCount: feedDiagnostics.QueueItemCount,
                visibleItemCount: feedDiagnostics.VisibleItemCount,
                reason: shouldDispatch ? "QueueDispatched" : feedDiagnostics.Reason);
        }

        private static ArcGraphDebugRuntimeWiringDiagnostics CreateDiagnostics(
            ArcGraphDebugRuntimeWiringFrame frame,
            bool didBuildFeed,
            bool didDispatchToConsumer,
            bool wasConsumerProvided,
            int queueItemCount,
            int visibleItemCount,
            string reason)
        {
            return new ArcGraphDebugRuntimeWiringDiagnostics(
                frame != null && frame.IsOverlayEnabled,
                frame != null && frame.HasContext,
                frame != null && frame.HasWorld,
                frame != null ? frame.ActiveNpcId : -1,
                didBuildFeed,
                didDispatchToConsumer,
                wasConsumerProvided,
                queueItemCount,
                visibleItemCount,
                frame != null ? frame.SourceTick : -1,
                reason);
        }
    }
}
