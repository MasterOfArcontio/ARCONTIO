using UnityEngine;

namespace Arcontio.Core
{
    // =============================================================================
    // AddInventoryItemCommand
    // =============================================================================
    /// <summary>
    /// <para>
    /// Comando autorizzato per aggiungere item non spaziali all'inventario fisico
    /// typed di un NPC.
    /// </para>
    ///
    /// <para><b>Command -> World -> Event, senza scorciatoie UI</b></para>
    /// <para>
    /// Il comando non scrive direttamente <see cref="World.NpcInventories"/> o
    /// <see cref="World.ObjectStacks"/>. Delega al <see cref="World"/>, riceve un
    /// <see cref="InventoryMutationResult"/> e pubblica un solo evento canonico:
    /// <see cref="InventoryItemAddedEvent"/>.
    /// </para>
    ///
    /// <para><b>Struttura interna:</b></para>
    /// <list type="bullet">
    ///   <item><b>NpcId/DefId/Quantity</b>: richiesta intenzionale grezza.</item>
    ///   <item><b>PreferredSlot</b>: collocazione desiderata, normalizzata dal World.</item>
    ///   <item><b>Execute</b>: applica e pubblica evento solo su mutazione reale.</item>
    /// </list>
    /// </summary>
    public sealed class AddInventoryItemCommand : ICommand
    {
        private readonly int _npcId;
        private readonly string _defId;
        private readonly int _quantity;
        private readonly NpcInventorySlotKind _preferredSlot;

        public AddInventoryItemCommand(
            int npcId,
            string defId,
            int quantity,
            NpcInventorySlotKind preferredSlot = NpcInventorySlotKind.Pack)
        {
            _npcId = npcId;
            _defId = defId ?? string.Empty;
            _quantity = quantity;
            _preferredSlot = preferredSlot;
        }

        public void Execute(World world, MessageBus bus)
        {
            if (world == null)
                return;

            if (!world.TryAddInventoryItem(
                    _npcId,
                    _defId,
                    _quantity,
                    _preferredSlot,
                    0,
                    out InventoryMutationResult result,
                    out string reason))
            {
                Debug.LogWarning($"[Inventory] Add failed npc={_npcId} def='{_defId}' qty={_quantity} slot={_preferredSlot} reason={reason}");
                return;
            }

            if (result.HasMutation)
                bus?.Publish(new InventoryItemAddedEvent(TickContext.CurrentTickIndex, result));
        }
    }
}
