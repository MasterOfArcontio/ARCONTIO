using Arcontio.Core.Logging;
using UnityEngine;

namespace Arcontio.Core
{
    /// <summary>
    /// StealPrivateFoodCommand (Day9):
    /// Il ladro ruba Units dal cibo privato della vittima.
    ///
    /// Pubblica FoodStolenEvent come FACT del mondo (furto avvenuto).
    /// IMPORTANTE:
    /// - Questo evento NON rende automaticamente la vittima consapevole.
    /// - La consapevolezza/memoria verrà gestita dal MemoryEncodingSystem:
    ///   testimoni (range + cono + LOS) => TheftWitnessed / FoodStolenFromMe
    ///
    /// cellX/cellY dell’evento:
    /// - posizione del ladro al momento dell’azione (se nota),
    /// - fallback (0,0) se mancante.
    ///
    /// PATCH (Day10+):
    /// - Regola fisica del furto "addosso" (NPC -> NPC), richiesta Marcello:
    ///   1) ladro e vittima devono essere in celle ADIACENTI (Manhattan distance = 1)
    ///   2) deve esserci LOS (OcclusionMap) tra le due celle (nessuna occlusione)
    ///
    /// - Quantità rubabile:
    ///   si può rubare tutto il cibo privato della vittima, ma solo fino alla capienza residua del ladro.
    ///   amount = min(victimFood, world.GetInventoryFreeCapacity(thief))
    ///
    /// - Fix bug storico:
    ///   la versione Day9 sottraeva alla vittima ma NON aggiungeva al ladro.
    ///   Ora il trasferimento è atomico e coerente con StealFromStockCommand.
    ///
    /// - Nota retrocompatibilità:
    ///   manteniamo _units e i costruttori esistenti, ma l'execution è governata
    ///   da capienza + disponibilità + regole fisiche.
    /// </summary>
    public sealed class StealPrivateFoodCommand : ICommand
    {
        private readonly int _thiefNpcId;
        private readonly int _victimNpcId;
        private readonly int _units;

        /// <summary>
        /// Overload comodo: units default = 1.
        /// Così NeedsDecisionRule può chiamare new StealPrivateFoodCommand(thief, victim)
        /// senza errori di compilazione.
        /// </summary>
        public StealPrivateFoodCommand(int thiefNpcId, int victimNpcId)
            : this(thiefNpcId, victimNpcId, 1)
        {
        }

        public StealPrivateFoodCommand(int thiefNpcId, int victimNpcId, int units)
        {
            _thiefNpcId = thiefNpcId;
            _victimNpcId = victimNpcId;

            // Manteniamo units>=1 per compatibilità.
            _units = units <= 0 ? 1 : units;
        }

        public void Execute(World world, MessageBus bus)
        {
            if (!world.ExistsNpc(_thiefNpcId) || !world.ExistsNpc(_victimNpcId))
                return;

            if (!world.NpcPrivateFood.TryGetValue(_victimNpcId, out int victimFood) || victimFood <= 0)
                return;

            // ============================================================
            // REGOLE FISICHE (PATCH): adjacency + LOS
            // ============================================================
            if (!world.GridPos.TryGetValue(_thiefNpcId, out var thiefPos))
                return;

            if (!world.GridPos.TryGetValue(_victimNpcId, out var victimPos))
                return;

            int dx = Mathf.Abs(thiefPos.X - victimPos.X);
            int dy = Mathf.Abs(thiefPos.Y - victimPos.Y);

            // Manhattan adjacency => dx+dy == 1.
            if ((dx + dy) != 1)
            {
                // Planning deve prima avvicinarsi con SetMoveIntentCommand (verso last known cell della vittima).
                // Questa guardia blocca furti "a distanza".
                return;
            }

            // LOS via OcclusionMap: nessuna occlusione tra le due celle.
            // Nota: per celle adiacenti, LOS è quasi sempre true; ma porte/muri devono bloccare.
            if (!world.HasLineOfSight(thiefPos.X, thiefPos.Y, victimPos.X, victimPos.Y))
                return;

            // ============================================================
            // CAPACITÀ INVENTARIO (PATCH): rubo tutto ciò che posso portare.
            // ============================================================
            int freeCapacity = world.GetInventoryFreeCapacity(_thiefNpcId);
            if (freeCapacity <= 0)
                return;

            int stolen = victimFood;
            if (stolen > freeCapacity) stolen = freeCapacity;

            if (stolen <= 0)
                return;

            // ACTION TRACE (debug/overlay): furto di cibo privato.
            world.SetNpcAction(_thiefNpcId, NpcActionState.Steal("StealPrivateFood", targetObjectId: 0));

            // BALLOON SIGNAL (view): fumetto "Steal" per il ladro.
            world.EmitNpcBalloon(_thiefNpcId, NpcBalloonKind.Steal, subjectId: _victimNpcId);

            // ============================================================
            // TRASFERIMENTO ATOMICO:
            // 1) togli alla vittima
            // 2) aggiungi al ladro
            // ============================================================
            world.NpcPrivateFood[_victimNpcId] = victimFood - stolen;

            if (!world.NpcPrivateFood.TryGetValue(_thiefNpcId, out int thiefFood))
                thiefFood = 0;
            world.NpcPrivateFood[_thiefNpcId] = thiefFood + stolen;

            // 2) Cella evento = posizione del ladro (nota qui).
            int ex = thiefPos.X;
            int ey = thiefPos.Y;

            // 3) Pubblica FACT del mondo: il furto è successo
            bus.Publish(new FoodStolenEvent(
                victimNpcId: _victimNpcId,
                thiefNpcId: _thiefNpcId,
                units: stolen,
                cellX: ex,
                cellY: ey
            ));

            ArcontioLogger.Info(
                new LogContext(tick: (int)TickContext.CurrentTickIndex, channel: "World", cell: (ex, ey)),
                new LogBlock(LogLevel.Info, "log.world.theft.happened")
                    .AddField("thief", _thiefNpcId)
                    .AddField("victim", _victimNpcId)
                    .AddField("units", stolen)
                    .AddField("freeCapacity", freeCapacity)
            );
        }
    }
}
