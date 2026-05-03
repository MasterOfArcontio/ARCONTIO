namespace Arcontio.Core
{
    /// <summary>
    /// WorldObjectInstance:
    /// Istanza nel mondo di un ObjectDef.
    ///
    /// Esempio:
    /// - DefId = "bed_wood_poor"
    /// - In cella (x,y)
    /// - owner = Npc 12
    /// - occupant = npc che sta dormendo (solo per letti)
    /// </summary>
    public sealed class WorldObjectInstance
    {
        public int ObjectId;        // id numerico runtime (univoco)
        public string DefId;        // chiave su world.ObjectDefs

        public int CellX;
        public int CellY;

        // =============================================================================
        // Holder fisico MVP
        // =============================================================================
        /// <summary>
        /// <para>
        /// Stato minimale che indica se questa istanza e' temporaneamente trasportata
        /// da un NPC invece che appoggiata su una cella della griglia.
        /// </para>
        ///
        /// <para><b>Holder fisico, non ownership logica</b></para>
        /// <para>
        /// Questo flag nasce per la micro feature DevTools Forced Transport Object Job.
        /// Non rappresenta proprieta', diritto d'uso, inventario completo o possesso
        /// sociale: dice solo che l'oggetto e' fisicamente agganciato a un NPC mentre
        /// un job runtime lo sta trasportando. <c>OwnerKind</c> e <c>OwnerId</c>
        /// restano la sola sorgente della ownership logica.
        /// </para>
        ///
        /// <para><b>Struttura interna:</b></para>
        /// <list type="bullet">
        ///   <item><b>IsHeld</b>: true quando l'oggetto non deve occupare una cella.</item>
        ///   <item><b>HolderNpcId</b>: NPC che lo trasporta fisicamente, oppure 0 se grounded.</item>
        /// </list>
        /// </summary>
        public bool IsHeld;
        public int HolderNpcId;

        // Ownership logica
        public OwnerKind OwnerKind;
        public int OwnerId;         // valido se OwnerKind != None

        // Stato runtime (specifico per alcuni oggetti)
        public int OccupantNpcId;   // letto: chi lo occupa, -1 se libero

        // Stato porta (valido solo se ObjectDef.IsDoor = true)
        public bool IsOpen;         // true = porta aperta, false = chiusa (default)
        public bool IsLocked;       // true = richiede chiave per aprire (default false)

        public WorldObjectInstance()
        {
            OccupantNpcId = -1;
            IsOpen   = false;
            IsLocked = false;
            OwnerKind = OwnerKind.None;
            OwnerId = -1;
            IsHeld = false;
            HolderNpcId = 0;
        }

        public override string ToString()
        {
            return $"obj#{ObjectId} def={DefId} cell=({CellX},{CellY}) held={IsHeld}:{HolderNpcId} owner={OwnerKind}:{OwnerId} occ={OccupantNpcId}";
        }
    }
}
