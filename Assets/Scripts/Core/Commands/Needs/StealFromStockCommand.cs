using Arcontio.Core.Logging;
using UnityEngine;

namespace Arcontio.Core
{
    /// <summary>
    /// StealFromStockCommand (Day10):
    /// Il ladro ruba unità da uno stock di cibo che NON gli appartiene.
    ///
    /// Perché serve:
    /// - Day10 richiede test con "cibo privato non addosso" (stock a terra con OwnerKind=Npc).
    /// - StealPrivateFoodCommand copre solo il caso "cibo privato addosso" (NpcPrivateFood).
    ///
    /// Effetto:
    /// 1) decrementa FoodStockComponent.Units sullo stock bersaglio
    /// 2) incrementa NpcPrivateFood del ladro (il bottino finisce "addosso")
    /// 3) pubblica FoodStolenEvent (FACT del mondo) con victim = ownerId dello stock
    ///
    /// Nota importante (coerenza con MemoryEncodingSystem):
    /// - Pubblicando FoodStolenEvent attiviamo già il meccanismo "testimoni -> TheftWitnessed / FoodStolenFromMe",
    ///   perché FoodStolenMemoryRule esiste già e MemoryEncodingSystem sa calcolare i witness.
    ///
    /// PATCH (Day10+):
    /// - Regola di interazione fisica aggiornata (richiesta Marcello):
    ///   lo "steal a terra" è un'azione di pickup/appropriazione, quindi deve avvenire SOLO
    ///   se il ladro si trova NELLA STESSA cella dello stock (non adiacente).
    ///
    /// - Quantità rubata:
    ///   si può rubare tutto lo stock, MA solo fino alla capienza residua del ladro.
    ///   amount = min(stock.Units, world.GetInventoryFreeCapacity(thief))
    ///
    /// - Nota retrocompatibilità:
    ///   il costruttore conserva _units (Day10 originale) ma l'execution applica la nuova regola.
    ///   Questo evita rotture se altre parti chiamano ancora il comando con units=1.
    /// </summary>
    public sealed class StealFromStockCommand : ICommand
    {
        private readonly int _thiefNpcId;
        private readonly int _foodObjId;
        private readonly int _units;

        /// <summary>
        /// Overload comodo: units default = 1.
        /// </summary>
        public StealFromStockCommand(int thiefNpcId, int foodObjId)
            : this(thiefNpcId, foodObjId, 1)
        {
        }

        public StealFromStockCommand(int thiefNpcId, int foodObjId, int units)
        {
            _thiefNpcId = thiefNpcId;
            _foodObjId = foodObjId;

            // Manteniamo il comportamento "units>=1" per compatibilità (Day10),
            // ma la logica di furto effettiva è governata da capienza + disponibilità.
            _units = units <= 0 ? 1 : units;
        }

        public void Execute(World world, MessageBus bus)
        {
            if (!world.ExistsNpc(_thiefNpcId))
                return;

            if (!world.FoodStocks.TryGetValue(_foodObjId, out var stock))
                return;

            if (stock.Units <= 0)
                return;

            // Il furto da stock ha senso solo se lo stock appartiene a un NPC diverso.
            // (Community o "mio" sono casi legali e non dovrebbero arrivare qui.)
            if (stock.OwnerKind != OwnerKind.Npc)
                return;

            int victimNpcId = stock.OwnerId;
            if (victimNpcId <= 0 || victimNpcId == _thiefNpcId)
                return;

            // ============================================================
            // REGOLE FISICHE (PATCH):
            // - Lo stock è "a terra": per rubarlo devo stare NELLA STESSA CELLA.
            // - Qui NON controlliamo LOS perché co-locazione implica interazione diretta.
            //   (Se in futuro avremo multi-layer / celle speciali, questa decisione si rivede.)
            // ============================================================
            if (!world.GridPos.TryGetValue(_thiefNpcId, out var thiefPos))
                return;

            // Nota critica:
            // - GridPos contiene SOLO le posizioni degli NPC (entity mobili).
            // - Gli oggetti nel mondo (inclusi gli stock di cibo) hanno le coordinate dentro World.Objects[objectId].
            //   Se proviamo a leggere GridPos per un oggetto, il lookup fallisce e il furto non avviene mai.
            if (!world.Objects.TryGetValue(_foodObjId, out var stockObj) || stockObj == null)
                return;

            var stockPos = new GridPosition(stockObj.CellX, stockObj.CellY);

            if (thiefPos.X != stockPos.X || thiefPos.Y != stockPos.Y)
            {
                // Fuori range fisico: planning deve prima emettere SetMoveIntentCommand verso stockPos.
                // Questa guardia è fondamentale per evitare "telepatia" quando planning/execution desyncano.
                return;
            }

            // ============================================================
            // CAPACITÀ INVENTARIO (PATCH):
            // rubo tutto ciò che posso portare.
            // ============================================================
            int freeCapacity = world.GetInventoryFreeCapacity(_thiefNpcId);
            if (freeCapacity <= 0)
                return;

            // Day10 originale prevedeva units fisso; ora scegliamo il massimo rubabile.
            int stolen = stock.Units;
            if (stolen > freeCapacity) stolen = freeCapacity;

            // Safety: per compatibilità, non rubiamo mai 0.
            if (stolen <= 0)
                return;

            // ACTION TRACE (debug/overlay): furto da stock.
            world.SetNpcAction(_thiefNpcId, NpcActionState.Steal("StealFromStock", _foodObjId));

            // BALLOON SIGNAL (view): fumetto "Steal" per il ladro.
            // Nota:
            // - I testimoni/vittima verranno segnalati in MemoryEncodingSystem quando processa FoodStolenEvent,
            //   perché solo lì calcoliamo i witness (range + cono + LOS).
            world.EmitNpcBalloon(_thiefNpcId, NpcBalloonKind.Steal, subjectId: _foodObjId);

            // 1) Mutazione stock
            stock.Units -= stolen;

            if (stock.Units <= 0)
            {
                // Preferiamo usare DestroyObject se disponibile, perché può ripulire cache/griglie.
                world.DestroyObject(_foodObjId);
            }
            else
            {
                world.SetFoodStock(_foodObjId, stock);
            }

            // 2) Il bottino finisce "addosso" al ladro (NpcPrivateFood).
            if (!world.NpcPrivateFood.TryGetValue(_thiefNpcId, out int thiefFood))
                thiefFood = 0;
            world.NpcPrivateFood[_thiefNpcId] = thiefFood + stolen;

            // 3) Pubblica FACT del mondo: furto accaduto
            int ex = thiefPos.X;
            int ey = thiefPos.Y;

            bus.Publish(new FoodStolenEvent(
                victimNpcId: victimNpcId,
                thiefNpcId: _thiefNpcId,
                units: stolen,
                cellX: ex,
                cellY: ey
            ));

            ArcontioLogger.Info(
                new LogContext(tick: (int)TickContext.CurrentTickIndex, channel: "World", cell: (ex, ey)),
                new LogBlock(LogLevel.Info, "log.world.theft.stock.happened")
                    .AddField("thief", _thiefNpcId)
                    .AddField("victim", victimNpcId)
                    .AddField("stockObj", _foodObjId)
                    .AddField("units", stolen)
                    // Field extra: utile in debug per capire se il limite è stock o capienza.
                    .AddField("freeCapacity", freeCapacity)
            );
        }
    }
}
