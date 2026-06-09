namespace Arcontio.View.ArcGraph
{
    // =============================================================================
    // ArcGraphPointerHudSnapshot
    // =============================================================================
    /// <summary>
    /// <para>
    /// Snapshot passivo del futuro HUD puntatore ArcGraph.
    /// </para>
    ///
    /// <para><b>Principio architetturale: HUD come consumer, non come sorgente di verita'</b></para>
    /// <para>
    /// Questa struttura descrive cosa il futuro pannello HUD dovra' mostrare, ma non
    /// crea UI, non legge il mouse, non interroga il mondo e non invia comandi. Il
    /// dato deriva dal <c>ArcGraphInteractionFrame</c>, quindi il Pointer HUD resta
    /// un consumer del boundary interattivo e non diventa un secondo sistema di
    /// picking parallelo.
    /// </para>
    ///
    /// <para><b>Struttura interna:</b></para>
    /// <list type="bullet">
    ///   <item><b>IsVisible</b>: il pannello futuro puo' essere mostrato.</item>
    ///   <item><b>HasInteractionFrame</b>: il frame sorgente e' stato consumato.</item>
    ///   <item><b>HasPointer</b>: il puntatore era disponibile nel frame input.</item>
    ///   <item><b>HasValidCell</b>: la cella e' stata risolta.</item>
    ///   <item><b>TargetKind</b>: target prioritario sotto il puntatore.</item>
    ///   <item><b>DisplayText</b>: testo gia' pronto per una UI minimale futura.</item>
    /// </list>
    /// </summary>
    public readonly struct ArcGraphPointerHudSnapshot
    {
        public readonly bool IsVisible;
        public readonly bool HasInteractionFrame;
        public readonly bool HasPointer;
        public readonly bool HasValidCell;
        public readonly bool IsPointerOverUi;
        public readonly ArcGraphCellCoord Cell;
        public readonly ArcGraphInteractionTargetKind TargetKind;
        public readonly int ActorId;
        public readonly int ObjectId;
        public readonly bool HasActor;
        public readonly bool HasObject;
        public readonly long SourceFrameIndex;
        public readonly string InteractionReason;
        public readonly string AdapterReason;
        public readonly string DisplayText;

        // =============================================================================
        // ArcGraphPointerHudSnapshot
        // =============================================================================
        /// <summary>
        /// <para>
        /// Costruisce lo snapshot completo del Pointer HUD.
        /// </para>
        ///
        /// <para><b>Normalizzazione leggera</b></para>
        /// <para>
        /// Gli id non positivi vengono trasformati in assenza logica. Le stringhe
        /// diagnostiche vuote vengono rese esplicite per evitare pannelli futuri con
        /// campi ambigui o null.
        /// </para>
        /// </summary>
        public ArcGraphPointerHudSnapshot(
            bool isVisible,
            bool hasInteractionFrame,
            bool hasPointer,
            bool hasValidCell,
            bool isPointerOverUi,
            ArcGraphCellCoord cell,
            ArcGraphInteractionTargetKind targetKind,
            int actorId,
            int objectId,
            long sourceFrameIndex,
            string interactionReason,
            string adapterReason,
            string displayText)
        {
            IsVisible = isVisible;
            HasInteractionFrame = hasInteractionFrame;
            HasPointer = hasPointer;
            HasValidCell = hasValidCell;
            IsPointerOverUi = isPointerOverUi;
            Cell = cell;
            TargetKind = targetKind;
            ActorId = actorId > 0 ? actorId : -1;
            ObjectId = objectId > 0 ? objectId : -1;
            HasActor = ActorId > 0;
            HasObject = ObjectId > 0;
            SourceFrameIndex = sourceFrameIndex;
            InteractionReason = string.IsNullOrWhiteSpace(interactionReason) ? "None" : interactionReason;
            AdapterReason = string.IsNullOrWhiteSpace(adapterReason) ? "None" : adapterReason;
            DisplayText = string.IsNullOrWhiteSpace(displayText) ? "Cell: -,-" : displayText;
        }

        // =============================================================================
        // Empty
        // =============================================================================
        /// <summary>
        /// <para>
        /// Crea uno snapshot vuoto, utile quando il wrapper non ha ancora prodotto
        /// un frame interattivo.
        /// </para>
        /// </summary>
        public static ArcGraphPointerHudSnapshot Empty(string reason)
        {
            return new ArcGraphPointerHudSnapshot(
                false,
                false,
                false,
                false,
                false,
                new ArcGraphCellCoord(0, 0, 0),
                ArcGraphInteractionTargetKind.None,
                -1,
                -1,
                0,
                reason,
                reason,
                "Cell: -,-");
        }
    }
}
