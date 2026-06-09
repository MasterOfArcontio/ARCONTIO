namespace Arcontio.View.ArcGraph
{
    // =============================================================================
    // ArcGraphInteractionFrame
    // =============================================================================
    /// <summary>
    /// <para>
    /// Frame passivo di interazione tra renderer mappa e strumenti debug/observer.
    /// </para>
    ///
    /// <para><b>Principio architetturale: boundary tra mappa e strumenti</b></para>
    /// <para>
    /// Questo frame e' il punto di scambio che impedisce a pannelli, DevTools,
    /// selection e HUD di dipendere direttamente da <c>MapGridWorldView</c> o dal
    /// futuro renderer ArcGraph produttivo. Contiene input gia' normalizzato,
    /// risultato coordinate, eventuale actor/oggetto sotto il puntatore e una ragione
    /// diagnostica. Non legge il World, non invia comandi e non conosce UI concrete.
    /// </para>
    ///
    /// <para><b>Struttura interna:</b></para>
    /// <list type="bullet">
    ///   <item><b>Input</b>: snapshot input view gia' raccolto da un wrapper esterno.</item>
    ///   <item><b>Coordinate</b>: risultato schermo/viewport -> cella.</item>
    ///   <item><b>TargetKind</b>: tipo di bersaglio prioritario.</item>
    ///   <item><b>Cell</b>: cella valida quando disponibile.</item>
    ///   <item><b>ActorId/ObjectId</b>: entita' visuali sotto il puntatore.</item>
    ///   <item><b>Reason</b>: esito sintetico spiegabile.</item>
    /// </list>
    /// </summary>
    public readonly struct ArcGraphInteractionFrame
    {
        public readonly ArcGraphViewInputFrame Input;
        public readonly ArcGraphViewCoordinateResult Coordinate;
        public readonly ArcGraphInteractionTargetKind TargetKind;
        public readonly ArcGraphCellCoord Cell;
        public readonly int ActorId;
        public readonly int ObjectId;
        public readonly bool HasValidCell;
        public readonly bool HasActor;
        public readonly bool HasObject;
        public readonly bool IsPointerOverUi;
        public readonly string Reason;

        // =============================================================================
        // ArcGraphInteractionFrame
        // =============================================================================
        /// <summary>
        /// <para>
        /// Costruisce un frame interazione completo.
        /// </para>
        ///
        /// <para><b>Normalizzazione difensiva</b></para>
        /// <para>
        /// Gli id non positivi vengono trattati come assenti. La cella resta comunque
        /// disponibile quando la conversione coordinate e' valida: questo permette a
        /// un Tool Panel futuro di usare la cella anche se non ci sono entita' sopra.
        /// </para>
        /// </summary>
        public ArcGraphInteractionFrame(
            ArcGraphViewInputFrame input,
            ArcGraphViewCoordinateResult coordinate,
            ArcGraphInteractionTargetKind targetKind,
            ArcGraphCellCoord cell,
            int actorId,
            int objectId,
            bool hasValidCell,
            bool isPointerOverUi,
            string reason)
        {
            Input = input;
            Coordinate = coordinate;
            TargetKind = targetKind;
            Cell = cell;
            ActorId = actorId > 0 ? actorId : -1;
            ObjectId = objectId > 0 ? objectId : -1;
            HasValidCell = hasValidCell;
            HasActor = ActorId > 0;
            HasObject = ObjectId > 0;
            IsPointerOverUi = isPointerOverUi;
            Reason = string.IsNullOrWhiteSpace(reason) ? "None" : reason;
        }

        // =============================================================================
        // Empty
        // =============================================================================
        /// <summary>
        /// <para>
        /// Crea un frame interazione vuoto e non valido.
        /// </para>
        /// </summary>
        public static ArcGraphInteractionFrame Empty(string reason)
        {
            return new ArcGraphInteractionFrame(
                ArcGraphViewInputFrame.Empty(),
                ArcGraphViewCoordinateResult.Invalid(reason),
                ArcGraphInteractionTargetKind.None,
                new ArcGraphCellCoord(0, 0, 0),
                -1,
                -1,
                false,
                false,
                reason);
        }
    }
}
