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
            // Il cibo privato e' ancora un aggregato generico: finche' non esiste un
            // inventario typed, ogni unita' privata usa il NutritionValue di food_stock.
            var cfg = world.Global.Needs;
            float nutritionValue = ResolvePrivateFoodNutritionValue(
                world,
                cfg.eatSatietyGain);
            needs.AddValue(NeedKind.Hunger, -nutritionValue);

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
        }

        // =============================================================================
        // ResolvePrivateFoodNutritionValue
        // =============================================================================
        /// <summary>
        /// <para>
        /// Risolve il valore nutritivo del cibo privato generico trasportato da un NPC.
        /// </para>
        ///
        /// <para><b>Principio architetturale: cibo privato come food_stock aggregato</b></para>
        /// <para>
        /// <c>NpcPrivateFood</c> oggi conserva solo un conteggio intero, non una lista
        /// typed di alimenti. Per non anticipare il refactor inventario, ogni unita'
        /// privata viene trattata come una porzione di <c>food_stock</c> aggregato e
        /// legge da li' il proprio <c>NutritionValue</c>.
        /// </para>
        ///
        /// <para><b>Struttura interna:</b></para>
        /// <list type="bullet">
        ///   <item><b>catalog lookup</b>: cerca la definizione oggetto food_stock.</item>
        ///   <item><b>NutritionValue</b>: usa il valore dichiarato nel JSON oggetti.</item>
        ///   <item><b>fallback</b>: usa eatSatietyGain solo se il dato catalogo manca o non e' valido.</item>
        /// </list>
        /// </summary>
        private static float ResolvePrivateFoodNutritionValue(
            World world,
            float fallback)
        {
            float safeFallback = fallback > 0f ? fallback : 0.45f;
            if (world == null
                || !world.TryGetObjectDef("food_stock", out var def)
                || def == null)
            {
                return safeFallback;
            }

            return def.TryGetPropertyValue("NutritionValue", out float nutritionValue)
                   && nutritionValue > 0f
                ? nutritionValue
                : safeFallback;
        }
    }
}
