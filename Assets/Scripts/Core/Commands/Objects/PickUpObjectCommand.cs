using UnityEngine;

namespace Arcontio.Core
{
    // =============================================================================
    // PickUpObjectCommand
    // =============================================================================
    /// <summary>
    /// <para>
    /// Command oggettivo che applica il passaggio di un oggetto da grounded a held
    /// by NPC usando l'API controllata del <see cref="World"/>.
    /// </para>
    ///
    /// <para><b>JobAction -> Command -> World</b></para>
    /// <para>
    /// Il Job System non muta direttamente gli store oggetto. Lo step <c>PickUp</c>
    /// accoda questo command, il pump canonico del <c>SimulationHost</c> lo esegue e
    /// il <c>World</c> resta l'unica authority della transizione fisica. Il command
    /// pubblica poi un <see cref="ObjectPickedUpEvent"/> come fatto di mondo minimo.
    /// </para>
    ///
    /// <para><b>Struttura interna:</b></para>
    /// <list type="bullet">
    ///   <item><b>NpcId/ObjectId</b>: dati intenzionali gia' risolti dal job o dal devtool.</item>
    ///   <item><b>Execute</b>: delega a <c>World.TryPickUpObject</c> e pubblica evento solo se riuscito.</item>
    /// </list>
    /// </summary>
    public sealed class PickUpObjectCommand : ICommand
    {
        private readonly int _npcId;
        private readonly int _objectId;
        private readonly NpcInventorySlotKind _preferredSlot;

        public PickUpObjectCommand(int npcId, int objectId)
            : this(npcId, objectId, NpcInventorySlotKind.None)
        {
        }

        public PickUpObjectCommand(int npcId, int objectId, NpcInventorySlotKind preferredSlot)
        {
            _npcId = npcId;
            _objectId = objectId;
            _preferredSlot = preferredSlot;
        }

        public void Execute(World world, MessageBus bus)
        {
            if (world == null)
                return;

            if (!world.TryPickUpObject(_npcId, _objectId, _preferredSlot, out ObjectPickupResult result, out string reason))
            {
                Debug.LogWarning($"[ObjectTransportJob] PickUp failed npc={_npcId} object={_objectId} reason={reason}");
                return;
            }

            bus?.Publish(new ObjectPickedUpEvent(
                TickContext.CurrentTickIndex,
                _npcId,
                result.PickedObjectId,
                result.FromCellX,
                result.FromCellY));
        }
    }
}
