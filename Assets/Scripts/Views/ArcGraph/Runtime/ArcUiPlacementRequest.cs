namespace Arcontio.View.ArcGraph
{
    // =============================================================================
    // ArcUiPlacementMode
    // =============================================================================
    /// <summary>
    /// <para>
    /// Modalita' con cui una richiesta di placement ArcGraph deve essere raccolta.
    /// </para>
    ///
    /// <para><b>Principio architetturale: modo strumento separato dalla operation</b></para>
    /// <para>
    /// La stessa operation, ad esempio muro di pietra, puo' essere usata con click
    /// singolo o brush. Per questo la modalita' non viene duplicata nella
    /// <c>OperationKey</c>: resta uno stato UI esplicito e controllabile.
    /// </para>
    ///
    /// <para><b>Struttura interna:</b></para>
    /// <list type="bullet">
    ///   <item><b>None</b>: nessun placement attivo.</item>
    ///   <item><b>Single</b>: una conferma inserisce un solo elemento.</item>
    ///   <item><b>Brush</b>: il trascinamento puo' preparare inserimenti ripetuti.</item>
    /// </list>
    /// </summary>
    public enum ArcUiPlacementMode
    {
        None = 0,
        Single = 1,
        Brush = 2
    }

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
    ///   <item><b>Mode</b>: modalita' di raccolta, singola o brush.</item>
    ///   <item><b>HasTargetCell</b>: indica se il click mappa e' gia' disponibile.</item>
    /// </list>
    /// </summary>
    public readonly struct ArcUiPlacementRequest
    {
        public readonly string OperationKey;
        public readonly ArcGraphCellCoord TargetCell;
        public readonly string TargetDefId;
        public readonly ArcUiPlacementMode Mode;
        public readonly bool HasTargetCell;

        public bool IsValid => !string.IsNullOrEmpty(OperationKey) && Mode != ArcUiPlacementMode.None;

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
        /// ricche, come DNA NPC, quantita' food stock o tuning di oggetti specifici,
        /// verranno introdotte solo quando il pannello inspector/configurazione
        /// sara' progettato e collegato al suo request type.
        /// </para>
        /// </summary>
        public ArcUiPlacementRequest(
            string operationKey,
            ArcGraphCellCoord targetCell,
            string targetDefId,
            ArcUiPlacementMode mode,
            bool hasTargetCell)
        {
            OperationKey = ArcUiOperationDefinition.NormalizeKey(operationKey);
            TargetCell = targetCell;
            TargetDefId = string.IsNullOrWhiteSpace(targetDefId) ? string.Empty : targetDefId.Trim();
            Mode = mode;
            HasTargetCell = hasTargetCell;
        }

        // =============================================================================
        // ArcUiPlacementRequest
        // =============================================================================
        /// <summary>
        /// <para>
        /// Costruisce una richiesta placement in modalita' singola.
        /// </para>
        ///
        /// <para><b>Compatibilita' di step</b></para>
        /// <para>
        /// Mantiene utilizzabile la firma della foundation iniziale mentre i
        /// controller vengono aggiornati gradualmente a conoscere il brush.
        /// </para>
        /// </summary>
        public ArcUiPlacementRequest(
            string operationKey,
            ArcGraphCellCoord targetCell,
            string targetDefId,
            bool hasTargetCell)
            : this(
                operationKey,
                targetCell,
                targetDefId,
                ArcUiPlacementMode.Single,
                hasTargetCell)
        {
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
            return WithoutCell(operationKey, targetDefId, ArcUiPlacementMode.Single);
        }

        // =============================================================================
        // WithoutCell
        // =============================================================================
        /// <summary>
        /// <para>
        /// Crea una richiesta di placement ancora priva della cella target,
        /// specificando la modalita' dello strumento.
        /// </para>
        /// </summary>
        public static ArcUiPlacementRequest WithoutCell(
            string operationKey,
            string targetDefId,
            ArcUiPlacementMode mode)
        {
            return new ArcUiPlacementRequest(
                operationKey,
                new ArcGraphCellCoord(0, 0, 0),
                targetDefId,
                mode == ArcUiPlacementMode.None ? ArcUiPlacementMode.Single : mode,
                false);
        }
    }
}
