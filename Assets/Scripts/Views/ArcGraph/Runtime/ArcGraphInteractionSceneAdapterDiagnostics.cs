namespace Arcontio.View.ArcGraph
{
    // =============================================================================
    // ArcGraphInteractionSceneAdapterDiagnostics
    // =============================================================================
    /// <summary>
    /// <para>
    /// Diagnostica sintetica del contratto adapter scena interazione ArcGraph.
    /// </para>
    ///
    /// <para><b>Principio architetturale: input e picking spiegabili</b></para>
    /// <para>
    /// Il futuro adapter scena dovra' essere facile da ispezionare. Questa struttura
    /// registra se config, view state, viewport, puntatore e queue erano disponibili,
    /// se il controller view ha applicato zoom/pan, se il boundary ha prodotto un
    /// target e se il frame e' stato consegnato a un consumer esterno. Non contiene
    /// riferimenti a UI concrete, comandi o sistemi simulativi.
    /// </para>
    ///
    /// <para><b>Struttura interna:</b></para>
    /// <list type="bullet">
    ///   <item><b>HasConfig</b>: config fornita dal chiamante o fallback valido.</item>
    ///   <item><b>WasViewStateProvided</b>: stato view esplicito ricevuto.</item>
    ///   <item><b>HasRenderQueue</b>: queue actor/object disponibile.</item>
    ///   <item><b>HasValidViewport</b>: viewport valido in pixel.</item>
    ///   <item><b>DidChangeZoom/DidApplyPan</b>: effetti del controller view.</item>
    ///   <item><b>DidBuildInteractionFrame</b>: boundary interattivo eseguito.</item>
    ///   <item><b>DidDispatchToConsumer</b>: frame consegnato a tool esterno.</item>
    ///   <item><b>TargetKind</b>: bersaglio prioritario finale.</item>
    ///   <item><b>Reason</b>: motivo sintetico dell'esito.</item>
    /// </list>
    /// </summary>
    public readonly struct ArcGraphInteractionSceneAdapterDiagnostics
    {
        public readonly bool HasConfig;
        public readonly bool WasViewStateProvided;
        public readonly bool HasRenderQueue;
        public readonly bool HasValidViewport;
        public readonly bool HasPointer;
        public readonly bool IsPointerOverUi;
        public readonly bool DidApplyViewController;
        public readonly bool DidChangeZoom;
        public readonly bool DidApplyPan;
        public readonly bool DidBuildInteractionFrame;
        public readonly bool WasConsumerProvided;
        public readonly bool DidDispatchToConsumer;
        public readonly ArcGraphInteractionTargetKind TargetKind;
        public readonly int ActorId;
        public readonly int ObjectId;
        public readonly bool HasValidCell;
        public readonly long SourceFrameIndex;
        public readonly string Reason;

        public ArcGraphInteractionSceneAdapterDiagnostics(
            bool hasConfig,
            bool wasViewStateProvided,
            bool hasRenderQueue,
            bool hasValidViewport,
            bool hasPointer,
            bool isPointerOverUi,
            bool didApplyViewController,
            bool didChangeZoom,
            bool didApplyPan,
            bool didBuildInteractionFrame,
            bool wasConsumerProvided,
            bool didDispatchToConsumer,
            ArcGraphInteractionTargetKind targetKind,
            int actorId,
            int objectId,
            bool hasValidCell,
            long sourceFrameIndex,
            string reason)
        {
            HasConfig = hasConfig;
            WasViewStateProvided = wasViewStateProvided;
            HasRenderQueue = hasRenderQueue;
            HasValidViewport = hasValidViewport;
            HasPointer = hasPointer;
            IsPointerOverUi = isPointerOverUi;
            DidApplyViewController = didApplyViewController;
            DidChangeZoom = didChangeZoom;
            DidApplyPan = didApplyPan;
            DidBuildInteractionFrame = didBuildInteractionFrame;
            WasConsumerProvided = wasConsumerProvided;
            DidDispatchToConsumer = didDispatchToConsumer;
            TargetKind = targetKind;
            ActorId = actorId > 0 ? actorId : -1;
            ObjectId = objectId > 0 ? objectId : -1;
            HasValidCell = hasValidCell;
            SourceFrameIndex = sourceFrameIndex;
            Reason = string.IsNullOrWhiteSpace(reason) ? "None" : reason;
        }
    }
}
