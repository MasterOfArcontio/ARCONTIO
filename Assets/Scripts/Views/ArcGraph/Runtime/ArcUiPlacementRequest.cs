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
    // ArcUiPlacementOwnerKind
    // =============================================================================
    /// <summary>
    /// <para>
    /// Proprietario logico richiesto dalla UI per un oggetto inserito.
    /// </para>
    ///
    /// <para><b>Principio architetturale: contratto UI senza dipendenza Core</b></para>
    /// <para>
    /// Il valore descrive solo la scelta dell'utente nel pannello. Non usa
    /// direttamente <c>OwnerKind</c> del Core per mantenere la request UI separata
    /// dal comando finale. Il ponte placement mappera' questo valore verso il tipo
    /// Core autorizzato nello step successivo.
    /// </para>
    ///
    /// <para><b>Struttura interna:</b></para>
    /// <list type="bullet">
    ///   <item><b>Community</b>: proprieta' comunitaria o neutra.</item>
    ///   <item><b>Npc</b>: proprieta' associata a un NPC specifico.</item>
    /// </list>
    /// </summary>
    public enum ArcUiPlacementOwnerKind
    {
        Community = 0,
        Npc = 1
    }

    // =============================================================================
    // ArcUiDoorPlacementState
    // =============================================================================
    /// <summary>
    /// <para>
    /// Stato iniziale richiesto per una porta inserita dalla UI.
    /// </para>
    ///
    /// <para><b>Contratto specifico, non payload generico</b></para>
    /// <para>
    /// Le porte sono gia' una funzione reale del vecchio F3. Per questo la request
    /// espone soltanto i tre stati necessari ora: chiusa, aperta e chiusa a chiave.
    /// Non introduce un dizionario libero di parametri.
    /// </para>
    ///
    /// <para><b>Struttura interna:</b></para>
    /// <list type="bullet">
    ///   <item><b>Closed</b>: porta chiusa.</item>
    ///   <item><b>Open</b>: porta aperta.</item>
    ///   <item><b>Locked</b>: porta chiusa a chiave, se supportata dal defId.</item>
    /// </list>
    /// </summary>
    public enum ArcUiDoorPlacementState
    {
        Closed = 0,
        Open = 1,
        Locked = 2
    }

    // =============================================================================
    // ArcUiPlacementConfig
    // =============================================================================
    /// <summary>
    /// <para>
    /// Configurazione minima associata a una richiesta placement ArcGraph.
    /// </para>
    ///
    /// <para><b>Principio architetturale: parametri espliciti e limitati</b></para>
    /// <para>
    /// Questa struttura contiene solo i parametri emersi come necessari per la
    /// migrazione F3 iniziale: stato porta, quantita' food stock, proprietario e
    /// NPC proprietario. Non contiene payload generici, reflection, oggetti Unity o
    /// riferimenti al <c>World</c>.
    /// </para>
    ///
    /// <para><b>Struttura interna:</b></para>
    /// <list type="bullet">
    ///   <item><b>DoorState</b>: stato iniziale porta.</item>
    ///   <item><b>FoodUnits</b>: quantita' iniziale del food stock.</item>
    ///   <item><b>OwnerKind</b>: community oppure NPC.</item>
    ///   <item><b>OwnerNpcId</b>: id NPC usato solo se <c>OwnerKind</c> e' NPC.</item>
    /// </list>
    /// </summary>
    public readonly struct ArcUiPlacementConfig
    {
        public readonly ArcUiDoorPlacementState DoorState;
        public readonly int FoodUnits;
        public readonly ArcUiPlacementOwnerKind OwnerKind;
        public readonly int OwnerNpcId;

        public ArcUiPlacementConfig(
            ArcUiDoorPlacementState doorState,
            int foodUnits,
            ArcUiPlacementOwnerKind ownerKind,
            int ownerNpcId)
        {
            DoorState = doorState;
            FoodUnits = foodUnits < 1 ? 1 : foodUnits;
            OwnerKind = ownerKind;
            OwnerNpcId = ownerKind == ArcUiPlacementOwnerKind.Npc && ownerNpcId > 0 ? ownerNpcId : 0;
        }

        public static ArcUiPlacementConfig Default()
        {
            return new ArcUiPlacementConfig(
                ArcUiDoorPlacementState.Closed,
                1,
                ArcUiPlacementOwnerKind.Community,
                0);
        }

        public ArcUiPlacementConfig WithDoorState(ArcUiDoorPlacementState state)
        {
            return new ArcUiPlacementConfig(state, FoodUnits, OwnerKind, OwnerNpcId);
        }

        public ArcUiPlacementConfig WithFoodUnits(int units)
        {
            return new ArcUiPlacementConfig(DoorState, units, OwnerKind, OwnerNpcId);
        }

        public ArcUiPlacementConfig WithOwner(ArcUiPlacementOwnerKind ownerKind, int ownerNpcId)
        {
            return new ArcUiPlacementConfig(DoorState, FoodUnits, ownerKind, ownerNpcId);
        }
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
        public readonly ArcUiPlacementConfig Config;
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
            : this(
                operationKey,
                targetCell,
                targetDefId,
                mode,
                ArcUiPlacementConfig.Default(),
                hasTargetCell)
        {
        }

        public ArcUiPlacementRequest(
            string operationKey,
            ArcGraphCellCoord targetCell,
            string targetDefId,
            ArcUiPlacementMode mode,
            ArcUiPlacementConfig config,
            bool hasTargetCell)
        {
            OperationKey = ArcUiOperationDefinition.NormalizeKey(operationKey);
            TargetCell = targetCell;
            TargetDefId = string.IsNullOrWhiteSpace(targetDefId) ? string.Empty : targetDefId.Trim();
            Mode = mode;
            Config = config;
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
            return WithoutCell(operationKey, targetDefId, mode, ArcUiPlacementConfig.Default());
        }

        // =============================================================================
        // WithoutCell
        // =============================================================================
        /// <summary>
        /// <para>
        /// Crea una richiesta di placement ancora priva della cella target,
        /// includendo la configurazione scelta nel pannello.
        /// </para>
        /// </summary>
        public static ArcUiPlacementRequest WithoutCell(
            string operationKey,
            string targetDefId,
            ArcUiPlacementMode mode,
            ArcUiPlacementConfig config)
        {
            return new ArcUiPlacementRequest(
                operationKey,
                new ArcGraphCellCoord(0, 0, 0),
                targetDefId,
                mode == ArcUiPlacementMode.None ? ArcUiPlacementMode.Single : mode,
                config,
                false);
        }
    }
}
