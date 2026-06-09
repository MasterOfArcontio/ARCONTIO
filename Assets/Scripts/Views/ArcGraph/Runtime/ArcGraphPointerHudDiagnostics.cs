namespace Arcontio.View.ArcGraph
{
    // =============================================================================
    // ArcGraphPointerHudDiagnostics
    // =============================================================================
    /// <summary>
    /// <para>
    /// Diagnostica sintetica del builder Pointer HUD ArcGraph.
    /// </para>
    ///
    /// <para><b>Principio architetturale: diagnostica senza pannello obbligatorio</b></para>
    /// <para>
    /// Il Pointer HUD deve poter essere testato prima di costruire la UI concreta.
    /// Questa diagnostica consente di sapere se il builder ha consumato un frame, se
    /// la cella era valida, quale target e' stato letto e quale testo sarebbe stato
    /// consegnato al pannello.
    /// </para>
    ///
    /// <para><b>Struttura interna:</b></para>
    /// <list type="bullet">
    ///   <item><b>DidBuildSnapshot</b>: lo snapshot e' stato prodotto.</item>
    ///   <item><b>HasPointer</b>: input puntatore disponibile.</item>
    ///   <item><b>HasValidCell</b>: cella risolta.</item>
    ///   <item><b>TargetKind</b>: target prioritario.</item>
    ///   <item><b>DisplayText</b>: testo finale dello snapshot.</item>
    /// </list>
    /// </summary>
    public readonly struct ArcGraphPointerHudDiagnostics
    {
        public readonly bool DidBuildSnapshot;
        public readonly bool HasInteractionFrame;
        public readonly bool HasPointer;
        public readonly bool HasValidCell;
        public readonly bool IsPointerOverUi;
        public readonly ArcGraphInteractionTargetKind TargetKind;
        public readonly int ActorId;
        public readonly int ObjectId;
        public readonly long SourceFrameIndex;
        public readonly string Reason;
        public readonly string DisplayText;

        public ArcGraphPointerHudDiagnostics(
            bool didBuildSnapshot,
            bool hasInteractionFrame,
            bool hasPointer,
            bool hasValidCell,
            bool isPointerOverUi,
            ArcGraphInteractionTargetKind targetKind,
            int actorId,
            int objectId,
            long sourceFrameIndex,
            string reason,
            string displayText)
        {
            DidBuildSnapshot = didBuildSnapshot;
            HasInteractionFrame = hasInteractionFrame;
            HasPointer = hasPointer;
            HasValidCell = hasValidCell;
            IsPointerOverUi = isPointerOverUi;
            TargetKind = targetKind;
            ActorId = actorId > 0 ? actorId : -1;
            ObjectId = objectId > 0 ? objectId : -1;
            SourceFrameIndex = sourceFrameIndex;
            Reason = string.IsNullOrWhiteSpace(reason) ? "None" : reason;
            DisplayText = string.IsNullOrWhiteSpace(displayText) ? "Cell: -,-" : displayText;
        }
    }
}
