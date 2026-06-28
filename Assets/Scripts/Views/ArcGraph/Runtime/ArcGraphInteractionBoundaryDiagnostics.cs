namespace Arcontio.View.ArcGraph
{
    // =============================================================================
    // ArcGraphInteractionBoundaryDiagnostics
    // =============================================================================
    /// <summary>
    /// <para>
    /// Diagnostica sintetica del boundary interattivo ArcGraph.
    /// </para>
    ///
    /// <para><b>Principio architetturale: interazione spiegabile e non magica</b></para>
    /// <para>
    /// La diagnostica rende visibile se il frame aveva input puntatore, se la UI ha
    /// bloccato la mappa, se la cella e' stata risolta e quanti actor/oggetti erano
    /// candidati. Serve per test e pannelli QA senza introdurre logica nei tool.
    /// </para>
    ///
    /// <para><b>Struttura interna:</b></para>
    /// <list type="bullet">
    ///   <item><b>HasPointer</b>: input puntatore disponibile.</item>
    ///   <item><b>IsPointerOverUi</b>: UI con priorita' sul frame.</item>
    ///   <item><b>HasValidCell</b>: conversione coordinate riuscita.</item>
    ///   <item><b>ActorCandidateCount</b>: actor visibili sulla cella.</item>
    ///   <item><b>ObjectCandidateCount</b>: oggetti visibili sulla cella.</item>
    ///   <item><b>PlantCandidateCount</b>: piante fisiche visibili sulla cella.</item>
    ///   <item><b>TargetKind</b>: bersaglio prioritario finale.</item>
    ///   <item><b>Reason</b>: esito sintetico.</item>
    /// </list>
    /// </summary>
    public readonly struct ArcGraphInteractionBoundaryDiagnostics
    {
        public readonly bool HasPointer;
        public readonly bool IsPointerOverUi;
        public readonly bool HasValidCell;
        public readonly int ActorCandidateCount;
        public readonly int ObjectCandidateCount;
        public readonly int PlantCandidateCount;
        public readonly ArcGraphInteractionTargetKind TargetKind;
        public readonly string Reason;

        public ArcGraphInteractionBoundaryDiagnostics(
            bool hasPointer,
            bool isPointerOverUi,
            bool hasValidCell,
            int actorCandidateCount,
            int objectCandidateCount,
            int plantCandidateCount,
            ArcGraphInteractionTargetKind targetKind,
            string reason)
        {
            HasPointer = hasPointer;
            IsPointerOverUi = isPointerOverUi;
            HasValidCell = hasValidCell;
            ActorCandidateCount = actorCandidateCount < 0 ? 0 : actorCandidateCount;
            ObjectCandidateCount = objectCandidateCount < 0 ? 0 : objectCandidateCount;
            PlantCandidateCount = plantCandidateCount < 0 ? 0 : plantCandidateCount;
            TargetKind = targetKind;
            Reason = string.IsNullOrWhiteSpace(reason) ? "None" : reason;
        }
    }
}
