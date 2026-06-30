namespace Arcontio.Core
{
    // =============================================================================
    // InventoryItemRemovedEvent
    // =============================================================================
    /// <summary>
    /// <para>
    /// Evento world-level emesso quando un comando rimuove item da un inventario NPC
    /// senza depositarli fisicamente su una cella.
    /// </para>
    ///
    /// <para><b>Principio architetturale: rimozione inventory-only</b></para>
    /// <para>
    /// Questo evento descrive consumo tecnico, uso materiale o rimozioni future non
    /// spaziali. Se l'oggetto viene appoggiato a terra, l'evento canonico resta
    /// <see cref="ObjectDroppedEvent"/> e non deve essere duplicato da questo evento.
    /// </para>
    ///
    /// <para><b>Struttura interna:</b></para>
    /// <list type="bullet">
    ///   <item><b>Tick/NpcId</b>: quando e quale inventario e' cambiato.</item>
    ///   <item><b>ObjectId/DefId</b>: oggetto o pila da cui e' stata rimossa quantita'.</item>
    ///   <item><b>Quantity/SlotKind</b>: quantita' realmente rimossa e slot di origine.</item>
    /// </list>
    /// </summary>
    public sealed class InventoryItemRemovedEvent : IWorldEvent
    {
        public readonly long Tick;
        public readonly int NpcId;
        public readonly int ObjectId;
        public readonly string DefId;
        public readonly int Quantity;
        public readonly NpcInventorySlotKind SlotKind;

        public InventoryItemRemovedEvent(long tick, InventoryMutationResult result)
        {
            Tick = tick;
            NpcId = result.NpcId;
            ObjectId = result.ObjectId;
            DefId = result.DefId ?? string.Empty;
            Quantity = result.QuantityChanged;
            SlotKind = result.SlotKind;
        }

        public string Describe()
            => $"InventoryItemRemoved tick={Tick} npc={NpcId} obj={ObjectId} def={DefId} qty={Quantity} slot={SlotKind}";
    }
}
