using Arcontio.Core.Logging;
using UnityEngine;

namespace Arcontio.Core
{
    // =============================================================================
    // ConsumeInventoryItemCommand
    // =============================================================================
    /// <summary>
    /// <para>
    /// Comando autorizzato per consumare un alimento dall'inventario typed di un NPC.
    /// </para>
    ///
    /// <para><b>Principio architetturale: Command -> World -> Event</b></para>
    /// <para>
    /// Il comando non decide se l'NPC debba mangiare e non scrive direttamente negli
    /// store inventario. Delega al <see cref="World"/>, applica la mutazione del
    /// bisogno Fame solo dopo il consumo riuscito e pubblica un singolo
    /// <see cref="FoodConsumedEvent"/> come evento canonico alimentare.
    /// </para>
    ///
    /// <para><b>Struttura interna:</b></para>
    /// <list type="bullet">
    ///   <item><b>NpcId</b>: NPC che consuma il cibo posseduto.</item>
    ///   <item><b>FoodDefId</b>: alimento richiesto; vuoto significa miglior cibo disponibile.</item>
    ///   <item><b>Execute</b>: consuma una unita', aggiorna Fame e pubblica evento food-only.</item>
    /// </list>
    /// </summary>
    public sealed class ConsumeInventoryItemCommand : ICommand
    {
        private readonly int _npcId;
        private readonly string _foodDefId;

        // =============================================================================
        // ConsumeInventoryItemCommand
        // =============================================================================
        /// <summary>
        /// <para>
        /// Crea una richiesta di consumo da inventario. Se <paramref name="foodDefId"/>
        /// e' vuoto, il <see cref="World"/> selezionera' l'alimento piu' nutriente.
        /// </para>
        /// </summary>
        public ConsumeInventoryItemCommand(int npcId, string foodDefId = "")
        {
            _npcId = npcId;
            _foodDefId = foodDefId ?? string.Empty;
        }

        // =============================================================================
        // Execute
        // =============================================================================
        /// <summary>
        /// <para>
        /// Esegue il consumo alimentare typed senza emettere eventi inventario duplicati.
        /// </para>
        /// </summary>
        public void Execute(World world, MessageBus bus)
        {
            if (world == null)
                return;

            if (!world.Needs.TryGetValue(_npcId, out var needs))
                return;

            if (!world.TryConsumeInventoryFood(
                    _npcId,
                    _foodDefId,
                    out InventoryMutationResult result,
                    out ObjectFoodNutritionResult nutrition,
                    out string reason))
            {
                Debug.LogWarning($"[Inventory] Consume food failed npc={_npcId} def='{_foodDefId}' reason={reason}");
                return;
            }

            // Il consumo e' gia' stato applicato allo store inventory dal World.
            // Solo ora il command applica la conseguenza fisiologica osservabile.
            needs.AddValue(NeedKind.Hunger, -nutrition.NutritionValue);
            world.Needs[_npcId] = needs;

            world.SetNpcAction(_npcId, NpcActionState.Eat("EatInventoryFood", result.ObjectId));
            world.EmitNpcBalloon(_npcId, NpcBalloonKind.Eat, subjectId: result.ObjectId);

            int cellX = 0;
            int cellY = 0;
            if (world.GridPos.TryGetValue(_npcId, out var pos))
            {
                cellX = pos.X;
                cellY = pos.Y;
            }

            int remainingUnits = world.GetInventoryQuantity(_npcId, result.DefId);
            bus?.Publish(new FoodConsumedEvent(
                TickContext.CurrentTickIndex,
                _npcId,
                "Inventory",
                result.ObjectId,
                units: result.QuantityChanged,
                remainingUnits: remainingUnits,
                depleted: remainingUnits <= 0,
                cellX: cellX,
                cellY: cellY,
                hungerAfter: needs.GetValue(NeedKind.Hunger),
                foodDefId: nutrition.ObjectDefId,
                nutritionValue: nutrition.NutritionValue,
                usedNutritionFallback: nutrition.UsedNutritionFallback));
        }
    }
}
