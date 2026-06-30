using Arcontio.Core.Logging;

namespace Arcontio.Core
{
    // =============================================================================
    // StealFromStockCommand
    // =============================================================================
    /// <summary>
    /// <para>
    /// Comando legacy Day10 per il vecchio furto da stock privato a terra.
    /// </para>
    ///
    /// <para><b>v0.71.05.C6 - Furto legacy sterilizzato</b></para>
    /// <para>
    /// Il furto non e' piu' una feature operativa fino alla progettazione di un
    /// modulo dedicato. Questo comando resta compilabile per preservare i riferimenti
    /// storici, ma non sottrae stock, non crea bottino, non scrive inventario typed,
    /// non muta <c>NpcPrivateFood</c> e non pubblica <c>FoodStolenEvent</c>.
    /// </para>
    ///
    /// <para><b>Struttura interna:</b></para>
    /// <list type="bullet">
    ///   <item><b>Identita' richiesta</b>: conserva ladro, stock e quantita' per diagnostica.</item>
    ///   <item><b>No-op runtime</b>: l'esecuzione registra solo un log debug stabile.</item>
    ///   <item><b>Boundary futuro</b>: il furto tornera' solo come Decision -> Job -> Step -> Command.</item>
    /// </list>
    /// </summary>
    public sealed class StealFromStockCommand : ICommand
    {
        private readonly int _thiefNpcId;
        private readonly int _foodObjId;
        private readonly int _units;

        public StealFromStockCommand(int thiefNpcId, int foodObjId)
            : this(thiefNpcId, foodObjId, 1)
        {
        }

        public StealFromStockCommand(int thiefNpcId, int foodObjId, int units)
        {
            _thiefNpcId = thiefNpcId;
            _foodObjId = foodObjId;
            _units = units <= 0 ? 1 : units;
        }

        // =============================================================================
        // Execute
        // =============================================================================
        /// <summary>
        /// <para>
        /// Esegue il comando legacy come no-op difensivo.
        /// </para>
        ///
        /// <para><b>Sterilizzazione feature</b></para>
        /// <para>
        /// Non controlliamo nemmeno le vecchie precondizioni fisiche: qualunque
        /// lettura/ramo operativo qui rischierebbe di mantenere vivo un percorso di
        /// furto parziale fuori dal nuovo sistema dei job.
        /// </para>
        ///
        /// <para><b>Struttura interna:</b></para>
        /// <list type="bullet">
        ///   <item><b>Log</b>: emette solo diagnostica debug.</item>
        ///   <item><b>Nessun evento</b>: non pubblica <c>FoodStolenEvent</c>.</item>
        ///   <item><b>Nessuna mutazione</b>: non modifica stock, oggetti o inventari.</item>
        /// </list>
        /// </summary>
        public void Execute(World world, MessageBus bus)
        {
            // v0.71.05.C6: il furto da stock resta disattivato finche' non
            // esistera' un modulo completo con furtivita', illegalita', visibilita'
            // del furto, trauma della vittima e conseguenze sociali.
            ArcontioLogger.Debug(
                new LogContext(tick: (int)TickContext.CurrentTickIndex, channel: "World"),
                new LogBlock(LogLevel.Debug, "log.world.theft.stock.legacy_sterilized")
                    .AddField("thief", _thiefNpcId)
                    .AddField("stockObj", _foodObjId)
                    .AddField("requestedUnits", _units)
            );
        }
    }
}
