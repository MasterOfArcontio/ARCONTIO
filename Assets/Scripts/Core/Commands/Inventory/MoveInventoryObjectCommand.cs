using UnityEngine;

namespace Arcontio.Core
{
    // =============================================================================
    // MoveInventoryObjectCommand
    // =============================================================================
    /// <summary>
    /// <para>
    /// Comando autorizzato per spostare un oggetto fisico gia' in inventario tra
    /// slot interni dell'NPC.
    /// </para>
    ///
    /// <para><b>Movimento interno, non pickup/drop</b></para>
    /// <para>
    /// Lo spostamento da pack a mano o tra mani non modifica la cella del mondo e
    /// non produce eventi oggetto. Il comando delega al <see cref="World"/> e, se
    /// la collocazione cambia davvero, pubblica un solo
    /// <see cref="InventoryItemMovedEvent"/>.
    /// </para>
    ///
    /// <para><b>Struttura interna:</b></para>
    /// <list type="bullet">
    ///   <item><b>NpcId/ObjectId</b>: inventario e oggetto fisico da spostare.</item>
    ///   <item><b>TargetSlot</b>: collocazione finale desiderata.</item>
    ///   <item><b>Execute</b>: no-op stabile senza evento se l'oggetto e' gia' nello slot richiesto.</item>
    /// </list>
    /// </summary>
    public sealed class MoveInventoryObjectCommand : ICommand
    {
        private readonly int _npcId;
        private readonly int _objectId;
        private readonly NpcInventorySlotKind _targetSlot;
        private readonly InventoryMoveQuantityPolicy _quantityPolicy;

        public MoveInventoryObjectCommand(int npcId, int objectId, NpcInventorySlotKind targetSlot)
            : this(npcId, objectId, targetSlot, InventoryMoveQuantityPolicy.WholeObject)
        {
        }

        public MoveInventoryObjectCommand(
            int npcId,
            int objectId,
            NpcInventorySlotKind targetSlot,
            InventoryMoveQuantityPolicy quantityPolicy)
        {
            _npcId = npcId;
            _objectId = objectId;
            _targetSlot = targetSlot;
            _quantityPolicy = quantityPolicy;
        }

        public void Execute(World world, MessageBus bus)
        {
            if (world == null)
                return;

            if (!world.TryMoveInventoryObject(
                    _npcId,
                    _objectId,
                    _targetSlot,
                    _quantityPolicy,
                    out InventoryMutationResult result,
                    out string reason))
            {
                Debug.LogWarning($"[Inventory] Move failed npc={_npcId} object={_objectId} target={_targetSlot} policy={_quantityPolicy} reason={reason}");
                return;
            }

            if (result.HasMutation)
                bus?.Publish(new InventoryItemMovedEvent(TickContext.CurrentTickIndex, result));
        }
    }
}
