using UnityEngine;

namespace Arcontio.Core
{
    // =============================================================================
    // DropObjectCommand
    // =============================================================================
    /// <summary>
    /// <para>
    /// Command oggettivo che applica il deposito di un oggetto trasportato su una
    /// cella grounded del mondo.
    /// </para>
    ///
    /// <para><b>JobAction -> Command -> World</b></para>
    /// <para>
    /// Il drop non scrive Transform Unity e non aggiorna direttamente le cache dalla
    /// UI o dal JobExecutionSystem. Tutta la mutazione passa da
    /// <see cref="World.TryDropObject"/>, che valida bounds, holder e vincolo
    /// "1 object per cell". Il command emette poi <see cref="ObjectDroppedEvent"/>.
    /// </para>
    ///
    /// <para><b>Struttura interna:</b></para>
    /// <list type="bullet">
    ///   <item><b>NpcId/ObjectId</b>: attore fisico e oggetto gia' trasportato.</item>
    ///   <item><b>TargetCell</b>: cella destinazione risolta dal template runtime.</item>
    ///   <item><b>Execute</b>: delega al World e pubblica evento solo in caso di successo.</item>
    /// </list>
    /// </summary>
    public sealed class DropObjectCommand : ICommand
    {
        private readonly int _npcId;
        private readonly int _objectId;
        private readonly int _targetX;
        private readonly int _targetY;

        public DropObjectCommand(int npcId, int objectId, int targetX, int targetY)
        {
            _npcId = npcId;
            _objectId = objectId;
            _targetX = targetX;
            _targetY = targetY;
        }

        public void Execute(World world, MessageBus bus)
        {
            if (world == null)
                return;

            if (!world.TryDropObject(_npcId, _objectId, _targetX, _targetY, out string reason))
            {
                Debug.LogWarning($"[ObjectTransportJob] Drop failed npc={_npcId} object={_objectId} target=({_targetX},{_targetY}) reason={reason}");
                return;
            }

            bus?.Publish(new ObjectDroppedEvent(TickContext.CurrentTickIndex, _npcId, _objectId, _targetX, _targetY));
        }
    }
}
