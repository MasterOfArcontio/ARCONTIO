namespace Arcontio.Core
{
    // =============================================================================
    // InventoryItemMovedEvent
    // =============================================================================
    /// <summary>
    /// <para>
    /// Evento world-level emesso quando un oggetto gia' nell'inventario NPC cambia
    /// collocazione interna.
    /// </para>
    ///
    /// <para><b>Principio architetturale: movimento interno, non movimento sulla mappa</b></para>
    /// <para>
    /// Lo spostamento tra mano e pack non e' pickup o drop: l'oggetto resta held
    /// dallo stesso NPC e non cambia cella. Questo evento rende osservabile solo
    /// quel cambio di slot interno, senza creare eventi spaziali duplicati.
    /// </para>
    ///
    /// <para><b>Struttura interna:</b></para>
    /// <list type="bullet">
    ///   <item><b>Tick/NpcId/ObjectId</b>: identita' del fatto accaduto.</item>
    ///   <item><b>DefId/Quantity</b>: tipo e quantita' fisica spostata.</item>
    ///   <item><b>PreviousSlotKind/SlotKind</b>: collocazione precedente e finale.</item>
    /// </list>
    /// </summary>
    public sealed class InventoryItemMovedEvent : IWorldEvent
    {
        public readonly long Tick;
        public readonly int NpcId;
        public readonly int ObjectId;
        public readonly string DefId;
        public readonly int Quantity;
        public readonly NpcInventorySlotKind PreviousSlotKind;
        public readonly NpcInventorySlotKind SlotKind;

        public InventoryItemMovedEvent(long tick, InventoryMutationResult result)
        {
            Tick = tick;
            NpcId = result.NpcId;
            ObjectId = result.ObjectId;
            DefId = result.DefId ?? string.Empty;
            Quantity = result.QuantityChanged;
            PreviousSlotKind = result.PreviousSlotKind;
            SlotKind = result.SlotKind;
        }

        public string Describe()
            => $"InventoryItemMoved tick={Tick} npc={NpcId} obj={ObjectId} def={DefId} qty={Quantity} from={PreviousSlotKind} to={SlotKind}";
    }
}
