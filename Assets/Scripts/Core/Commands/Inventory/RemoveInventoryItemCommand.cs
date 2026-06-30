using UnityEngine;

namespace Arcontio.Core
{
    // =============================================================================
    // RemoveInventoryItemCommand
    // =============================================================================
    /// <summary>
    /// <para>
    /// Comando autorizzato per rimuovere item da un inventario NPC senza drop fisico.
    /// </para>
    ///
    /// <para><b>Rimozione non spaziale</b></para>
    /// <para>
    /// Questo comando non rappresenta un deposito a terra. Per quel caso resta
    /// canonico <see cref="DropObjectCommand"/> con <see cref="ObjectDroppedEvent"/>.
    /// Qui la rimozione indica uso, consumo tecnico o altre mutazioni inventory-only.
    /// </para>
    ///
    /// <para><b>Struttura interna:</b></para>
    /// <list type="bullet">
    ///   <item><b>NpcId/DefId/Quantity</b>: item richiesto per tipo e quantita'.</item>
    ///   <item><b>Execute</b>: delega al World e pubblica <see cref="InventoryItemRemovedEvent"/> se riesce.</item>
    /// </list>
    /// </summary>
    public sealed class RemoveInventoryItemCommand : ICommand
    {
        private readonly int _npcId;
        private readonly string _defId;
        private readonly int _quantity;

        public RemoveInventoryItemCommand(int npcId, string defId, int quantity)
        {
            _npcId = npcId;
            _defId = defId ?? string.Empty;
            _quantity = quantity;
        }

        public void Execute(World world, MessageBus bus)
        {
            if (world == null)
                return;

            if (!world.TryRemoveInventoryItem(
                    _npcId,
                    _defId,
                    _quantity,
                    out InventoryMutationResult result,
                    out string reason))
            {
                Debug.LogWarning($"[Inventory] Remove failed npc={_npcId} def='{_defId}' qty={_quantity} reason={reason}");
                return;
            }

            if (result.HasMutation)
                bus?.Publish(new InventoryItemRemovedEvent(TickContext.CurrentTickIndex, result));
        }
    }
}
