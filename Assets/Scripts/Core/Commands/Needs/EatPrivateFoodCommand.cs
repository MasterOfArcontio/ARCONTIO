using Arcontio.Core.Logging;
using UnityEngine;

namespace Arcontio.Core
{
    /// <summary>
    /// EatPrivateFoodCommand (Day9):
    /// L’NPC consuma 1 unità dal proprio cibo privato (World.NpcPrivateFood[npcId]).
    ///
    /// Effetto:
    /// - decrementa il contatore privato
    /// - riduce Hunger01 usando NeedsConfig.eatSatietyGain
    ///
    /// Nota:
    /// - Non genera di per sé "furto" o "sospetto": è consumo legittimo.
    /// </summary>
    public sealed class EatPrivateFoodCommand : ICommand
    {
        private readonly int _npcId;

        public EatPrivateFoodCommand(int npcId)
        {
            _npcId = npcId;
        }

        public void Execute(World world, MessageBus bus)
        {
            if (!world.Needs.TryGetValue(_npcId, out var needs))
                return;

            if (!world.NpcPrivateFood.TryGetValue(_npcId, out int priv) || priv <= 0)
                return;

            // ACTION TRACE (debug/overlay): consumo cibo privato (inventario v0).
            world.SetNpcAction(_npcId, NpcActionState.Eat("EatPrivateFood"));

            // BALLOON SIGNAL (view): fumetto "Eat" (cibo privato)
            world.EmitNpcBalloon(_npcId, NpcBalloonKind.Eat);

            // 1) Mutazione inventario privato
            int remainingPrivateFood = priv - 1;
            world.NpcPrivateFood[_npcId] = remainingPrivateFood;
            
            // Marker: "ho consumato io" in questo tick
            world.NpcLastPrivateFoodConsumeTick[_npcId] = world.Global.CurrentTickIndex;
            
            // 2) Mutazione hunger
            var cfg = world.Global.Needs;
            needs.AddValue(NeedKind.Hunger, -cfg.eatSatietyGain);

            world.Needs[_npcId] = needs;

            int cellX = 0;
            int cellY = 0;
            if (world.GridPos.TryGetValue(_npcId, out var pos))
            {
                cellX = pos.X;
                cellY = pos.Y;
            }

            bus?.Publish(new FoodConsumedEvent(
                TickContext.CurrentTickIndex,
                _npcId,
                "PrivateFood",
                foodObjectId: 0,
                units: 1,
                remainingUnits: remainingPrivateFood,
                depleted: remainingPrivateFood <= 0,
                cellX: cellX,
                cellY: cellY,
                hungerAfter: needs.GetValue(NeedKind.Hunger)));

            ArcontioLogger.Debug(
                new LogContext(tick: (int)TickContext.CurrentTickIndex, channel: "T9", npcId: _npcId),
                new LogBlock(LogLevel.Debug, "log.t9.eat.private")
                    .AddField("privLeft", world.NpcPrivateFood[_npcId])
                    .AddField("hungerNow", needs.GetValue(NeedKind.Hunger).ToString("0.00"))
            );
        }
    }
}
