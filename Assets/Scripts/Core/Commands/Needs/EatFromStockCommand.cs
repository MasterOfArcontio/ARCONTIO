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
    /// - Non pubblichiamo eventi qui per ora (scelta minimal).
    /// - Se vuoi: puoi pubblicare un evento FoodConsumedEvent per memorie/telemetria.
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
                // Salva le coordinate dello stock PRIMA di rimuoverlo da world.Objects.
                int stockX = 0, stockY = 0;
                if (world.Objects.TryGetValue(_foodObjId, out var stockInst) && stockInst != null)
                {
                    stockX = stockInst.CellX;
                    stockY = stockInst.CellY;
                }

                // Rimuovi l'istanza dal mondo + componenti collegati.
                world.FoodStocks.Remove(_foodObjId);
                world.ObjectUse.Remove(_foodObjId);
                world.Objects.Remove(_foodObjId);

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
            var cfg = world.Global.Needs;
            needs.AddValue(NeedKind.Hunger, -cfg.eatSatietyGain);
            world.Needs[_npcId] = needs;

            ArcontioLogger.Debug(
                new LogContext(tick: (int)TickContext.CurrentTickIndex, channel: "T9", npcId: _npcId),
                new LogBlock(LogLevel.Debug, "log.t9.eat.stock")
                    .AddField("obj", _foodObjId)
                    .AddField("stockLeft", depleted ? 0 : stock.Units)
                    .AddField("hungerNow", needs.GetValue(NeedKind.Hunger).ToString("0.00"))
                    .AddField("depleted", depleted.ToString())
            );
        }
    }
}
