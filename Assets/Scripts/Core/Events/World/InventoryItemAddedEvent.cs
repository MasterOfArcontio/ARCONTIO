namespace Arcontio.Core
{
    // =============================================================================
    // InventoryItemAddedEvent
    // =============================================================================
    /// <summary>
    /// <para>
    /// Evento world-level emesso quando un comando aggiunge item a un inventario NPC
    /// senza una transizione spaziale da terra.
    /// </para>
    ///
    /// <para><b>Principio architetturale: evento inventario non duplicato</b></para>
    /// <para>
    /// Questo evento non sostituisce <see cref="ObjectPickedUpEvent"/>. Se un oggetto
    /// passa dalla mappa al corpo di un NPC, l'evento canonico resta il pickup
    /// spaziale. Questo evento vale per aggiunte non spaziali, come devtool,
    /// raccolta futura o produzione diretta in inventario.
    /// </para>
    ///
    /// <para><b>Struttura interna:</b></para>
    /// <list type="bullet">
    ///   <item><b>Tick/NpcId</b>: quando e quale inventario e' cambiato.</item>
    ///   <item><b>ObjectId/DefId</b>: pila fisica o oggetto creato/aggiornato.</item>
    ///   <item><b>Quantity/SlotKind</b>: quantita' realmente aggiunta e collocazione finale.</item>
    /// </list>
    /// </summary>
    public sealed class InventoryItemAddedEvent : IWorldEvent
    {
        public readonly long Tick;
        public readonly int NpcId;
        public readonly int ObjectId;
        public readonly string DefId;
        public readonly int Quantity;
        public readonly NpcInventorySlotKind SlotKind;

        public InventoryItemAddedEvent(long tick, InventoryMutationResult result)
        {
            Tick = tick;
            NpcId = result.NpcId;
            ObjectId = result.ObjectId;
            DefId = result.DefId ?? string.Empty;
            Quantity = result.QuantityChanged;
            SlotKind = result.SlotKind;
        }

        public string Describe()
            => $"InventoryItemAdded tick={Tick} npc={NpcId} obj={ObjectId} def={DefId} qty={Quantity} slot={SlotKind}";
    }
}
