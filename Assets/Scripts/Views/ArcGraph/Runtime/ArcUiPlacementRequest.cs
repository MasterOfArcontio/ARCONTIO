namespace Arcontio.View.ArcGraph
{
    // =============================================================================
    // ArcUiPlacementRequest
    // =============================================================================
    /// <summary>
    /// <para>
    /// Richiesta UI minima che descrive una intenzione di placement ArcGraph.
    /// </para>
    ///
    /// <para><b>Principio architetturale: richiesta UI prima del comando</b></para>
    /// <para>
    /// Questa struttura non e' un comando Core e non modifica il mondo. Raccoglie
    /// solo la scelta dell'utente, la cella target e l'eventuale definizione da
    /// piazzare. Lo step successivo sul ponte placement decidera' come trasformarla
    /// in una richiesta autorizzata o in un comando temporaneo.
    /// </para>
    ///
    /// <para><b>Struttura interna:</b></para>
    /// <list type="bullet">
    ///   <item><b>OperationKey</b>: operation scelta dalla UI.</item>
    ///   <item><b>TargetCell</b>: cella su cui l'utente vuole operare.</item>
    ///   <item><b>TargetDefId</b>: id oggetto/struttura/NPC quando serve.</item>
    ///   <item><b>HasTargetCell</b>: indica se il click mappa e' gia' disponibile.</item>
    /// </list>
    /// </summary>
    public readonly struct ArcUiPlacementRequest
    {
        public readonly string OperationKey;
        public readonly ArcGraphCellCoord TargetCell;
        public readonly string TargetDefId;
        public readonly bool HasTargetCell;

        public bool IsValid => !string.IsNullOrEmpty(OperationKey);

        // =============================================================================
        // ArcUiPlacementRequest
        // =============================================================================
        /// <summary>
        /// <para>
        /// Costruisce una richiesta placement UI.
        /// </para>
        ///
        /// <para><b>Contratto asciutto</b></para>
        /// <para>
        /// Non esiste un payload generico in questa foundation. Configurazioni piu'
        /// ricche, come il DNA NPC, verranno introdotte solo quando il pannello
        /// dedicato sara' progettato e collegato al suo request type.
        /// </para>
        /// </summary>
        public ArcUiPlacementRequest(
            string operationKey,
            ArcGraphCellCoord targetCell,
            string targetDefId,
            bool hasTargetCell)
        {
            OperationKey = ArcUiOperationDefinition.NormalizeKey(operationKey);
            TargetCell = targetCell;
            TargetDefId = string.IsNullOrWhiteSpace(targetDefId) ? string.Empty : targetDefId.Trim();
            HasTargetCell = hasTargetCell;
        }

        // =============================================================================
        // WithoutCell
        // =============================================================================
        /// <summary>
        /// <para>
        /// Crea una richiesta di placement ancora priva della cella target.
        /// </para>
        /// </summary>
        public static ArcUiPlacementRequest WithoutCell(string operationKey, string targetDefId)
        {
            return new ArcUiPlacementRequest(
                operationKey,
                new ArcGraphCellCoord(0, 0, 0),
                targetDefId,
                false);
        }
    }
}
