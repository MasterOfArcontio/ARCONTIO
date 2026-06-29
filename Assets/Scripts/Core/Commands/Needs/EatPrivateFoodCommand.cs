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

            // NpcPrivateFood e' il ponte temporaneo pre-inventario: non contiene
            // ancora stack typed, quindi ogni unita' privata viene risolta come
            // food_stock legacy/generico tramite lo stesso resolver usato dagli
            // altri consumi alimentari.
            var cfg = world.Global.Needs;
            ObjectFoodNutritionResult nutrition = ObjectFoodNutritionResolver.Resolve(
                world,
                "food_stock",
                cfg.eatSatietyGain,
                allowLegacyFallbackWhenDefinitionMissing: true);
            if (!nutrition.IsConsumableFood)
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
            needs.AddValue(NeedKind.Hunger, -nutrition.NutritionValue);

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
                hungerAfter: needs.GetValue(NeedKind.Hunger),
                foodDefId: nutrition.ObjectDefId,
                nutritionValue: nutrition.NutritionValue,
                usedNutritionFallback: nutrition.UsedNutritionFallback));
        }
    }
}
