namespace Arcontio.View.ArcGraph
{
    // =============================================================================
    // ArcGraphDebugRuntimeWiringDiagnostics
    // =============================================================================
    /// <summary>
    /// <para>
    /// Diagnostica sintetica del coordinatore runtime debug ArcGraph.
    /// </para>
    ///
    /// <para><b>Principio architetturale: spiegare il ponte senza eseguire scena</b></para>
    /// <para>
    /// La diagnostica dice se il debug era abilitato, se context e World erano
    /// presenti, se il feed e' stato costruito e se la queue e' stata consegnata a
    /// un consumer. Non contiene riferimenti Unity, non contiene item dettagliati
    /// e non rappresenta stato simulativo.
    /// </para>
    ///
    /// <para><b>Struttura interna:</b></para>
    /// <list type="bullet">
    ///   <item><b>IsOverlayEnabled</b>: gate esterno ricevuto dal frame.</item>
    ///   <item><b>HasContext/HasWorld</b>: disponibilita' delle sorgenti runtime.</item>
    ///   <item><b>ActiveNpcId</b>: NPC ricevuto dal selector esterno.</item>
    ///   <item><b>DidBuildFeed</b>: indica se il feed e' stato invocato.</item>
    ///   <item><b>DidDispatchToConsumer</b>: indica se la queue e' stata consegnata.</item>
    ///   <item><b>QueueItemCount/VisibleItemCount</b>: conteggi finali.</item>
    ///   <item><b>Reason</b>: esito sintetico.</item>
    /// </list>
    /// </summary>
    public readonly struct ArcGraphDebugRuntimeWiringDiagnostics
    {
        public readonly bool IsOverlayEnabled;
        public readonly bool HasContext;
        public readonly bool HasWorld;
        public readonly int ActiveNpcId;
        public readonly bool DidBuildFeed;
        public readonly bool DidDispatchToConsumer;
        public readonly bool WasConsumerProvided;
        public readonly int QueueItemCount;
        public readonly int VisibleItemCount;
        public readonly long SourceTick;
        public readonly string Reason;

        public ArcGraphDebugRuntimeWiringDiagnostics(
            bool isOverlayEnabled,
            bool hasContext,
            bool hasWorld,
            int activeNpcId,
            bool didBuildFeed,
            bool didDispatchToConsumer,
            bool wasConsumerProvided,
            int queueItemCount,
            int visibleItemCount,
            long sourceTick,
            string reason)
        {
            IsOverlayEnabled = isOverlayEnabled;
            HasContext = hasContext;
            HasWorld = hasWorld;
            ActiveNpcId = activeNpcId;
            DidBuildFeed = didBuildFeed;
            DidDispatchToConsumer = didDispatchToConsumer;
            WasConsumerProvided = wasConsumerProvided;
            QueueItemCount = NormalizeCount(queueItemCount);
            VisibleItemCount = NormalizeCount(visibleItemCount);
            SourceTick = sourceTick;
            Reason = string.IsNullOrWhiteSpace(reason) ? "None" : reason;
        }

        private static int NormalizeCount(int value)
        {
            return value < 0 ? 0 : value;
        }
    }
}
