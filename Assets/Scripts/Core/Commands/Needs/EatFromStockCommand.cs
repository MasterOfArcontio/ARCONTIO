using Arcontio.Core.Logging;
using UnityEngine;

namespace Arcontio.Core
{
    /// <summary>
    /// EatFromStockCommand (Day9):
    /// Consuma 1 unità da uno stock di cibo (oggetto in mondo).
    ///
    /// Effetto:
    /// - decrementa FoodStockComponent.Units
    /// - riduce Hunger01 usando NeedsConfig.eatSatietyGain
    ///
    /// Nota:
    /// - Pubblica un evento world-level minimale solo dopo una mutazione riuscita.
    /// - L'evento non introduce memoria, sospetto o nuove decisioni automatiche.
    /// </summary>
    public sealed class EatFromStockCommand : ICommand
    {
        private readonly int _npcId;
        private readonly int _foodObjId;

        public EatFromStockCommand(int npcId, int foodObjId)
        {
            _npcId = npcId;
            _foodObjId = foodObjId;
        }

        public void Execute(World world, MessageBus bus)
        {
            if (!world.Needs.TryGetValue(_npcId, out var needs))
                return;

            if (!world.FoodStocks.TryGetValue(_foodObjId, out var stock))
                return;

            if (stock.Units <= 0)
                return;

            int stockX = 0;
            int stockY = 0;
            string foodDefId = string.Empty;
            if (world.Objects.TryGetValue(_foodObjId, out var stockObject) && stockObject != null)
            {
                stockX = stockObject.CellX;
                stockY = stockObject.CellY;
                foodDefId = stockObject.DefId;
            }

            // La nutrizione viene risolta prima dell'eventuale DestroyObject: se lo
            // stock si esaurisce, l'istanza fisica puo' sparire dal World, ma il
            // fatto del consumo deve conservare il defId e il valore nutritivo usati.
            var cfg = world.Global.Needs;
            ObjectFoodNutritionResult nutrition = ObjectFoodNutritionResolver.Resolve(
                world,
                foodDefId,
                cfg.eatSatietyGain,
                allowLegacyFallbackWhenDefinitionMissing: true);
            if (!nutrition.IsConsumableFood)
                return;

            // ACTION TRACE (debug/overlay): l'NPC sta consumando cibo da uno stock visibile.
            world.SetNpcAction(_npcId, NpcActionState.Eat("EatFromStock", _foodObjId));

            // BALLOON SIGNAL (view): fumetto "Eat"
            // Nota:
            // - Non è un evento di simulazione.
            // - È un segnale osservabile one-shot: la view lo mostrerà per pochi secondi.
            world.EmitNpcBalloon(_npcId, NpcBalloonKind.Eat, subjectId: _foodObjId);

            // 1) Mutazione stock
            stock.Units -= 1;

            bool depleted = stock.Units <= 0;

            if (depleted)
            {
                // IMPORTANTE:
                // Il command NON deve conoscere tutti i registri derivati del lifecycle oggetti.
                // Quando lo stock si esaurisce deleghiamo la rimozione al punto canonico del World,
                // cosi' cache, use-state e store oggettivi restano coerenti in un solo posto.
                world.DestroyObject(_foodObjId);

                // Invalida lo slot in memoria per ogni NPC che in questo momento
                // sta osservando lo stock (Range + Cone + LOS).
                // L'NPC mangiante viene sempre invalidato (era sulla cella, lo sa con certezza).
                int visionRange = world.Global.NpcVisionRangeCells;
                if (visionRange <= 0) visionRange = 6;
                bool useCone    = world.Global.NpcVisionUseCone;
                float coneSlope = world.Global.NpcVisionConeSlope;

                foreach (var kv in world.NpcObjectMemory)
                {
                    int witnessId = kv.Key;
                    var store     = kv.Value;
                    if (store == null) continue;

                    bool canSee;
                    if (witnessId == _npcId)
                    {
                        // L'NPC mangiante era sulla cella: sa con certezza che è finito.
                        canSee = true;
                    }
                    else
                    {
                        if (!world.GridPos.TryGetValue(witnessId, out var wPos))
                            continue;

                        // Gate 1: range
                        int dist = FovUtils.Manhattan(wPos.X, wPos.Y, stockX, stockY);
                        if (dist > visionRange) continue;

                        // Gate 2: cone
                        if (useCone)
                        {
                            if (!world.NpcFacing.TryGetValue(witnessId, out var facing))
                                facing = CardinalDirection.North;
                            if (!FovUtils.IsInCone(wPos.X, wPos.Y, facing, stockX, stockY, coneSlope))
                                continue;
                        }

                        // Gate 3: LOS
                        canSee = world.HasLineOfSight(wPos.X, wPos.Y, stockX, stockY);
                    }

                    if (!canSee) continue;

                    if (world.Beliefs.TryGetValue(witnessId, out var beliefStore) && beliefStore != null)
                    {
                        beliefStore.TryDiscardByCategoryAndPosition(
                            BeliefCategory.Food,
                            new Vector2Int(stockX, stockY),
                            (int)TickContext.CurrentTickIndex);
                    }

                    for (int i = 0; i < store.Slots.Length; i++)
                    {
                        ref var slot = ref store.Slots[i];
                        if (!slot.IsValid) continue;
                        int slotObjId = slot.SubjectId != 0 ? slot.SubjectId : slot.ObjectId;
                        if (slotObjId == _foodObjId)
                        {
                            slot.IsValid = false;
                            break;
                        }
                    }
                }
            }
            else
            {
                world.SetFoodStock(_foodObjId, stock);
            }

            // 2) Mutazione hunger
            // Il valore primario viene dal catalogo oggetti. Il fallback resta
            // permesso solo per compatibilita' con food stock legacy o world QA
            // minimali privi di catalogo caricato.
            needs.AddValue(NeedKind.Hunger, -nutrition.NutritionValue);
            world.Needs[_npcId] = needs;

            bus?.Publish(new FoodConsumedEvent(
                TickContext.CurrentTickIndex,
                _npcId,
                "Stock",
                _foodObjId,
                units: 1,
                remainingUnits: depleted ? 0 : stock.Units,
                depleted: depleted,
                cellX: stockX,
                cellY: stockY,
                hungerAfter: needs.GetValue(NeedKind.Hunger),
                foodDefId: nutrition.ObjectDefId,
                nutritionValue: nutrition.NutritionValue,
                usedNutritionFallback: nutrition.UsedNutritionFallback));
        }
    }
}
