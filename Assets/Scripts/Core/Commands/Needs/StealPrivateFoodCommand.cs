using Arcontio.Core.Logging;

namespace Arcontio.Core
{
    // =============================================================================
    // StealPrivateFoodCommand
    // =============================================================================
    /// <summary>
    /// <para>
    /// Comando legacy Day9 per il vecchio furto di cibo privato da un NPC a un altro.
    /// </para>
    ///
    /// <para><b>v0.71.05.C6 - Furto legacy sterilizzato</b></para>
    /// <para>
    /// Il furto non e' piu' una feature operativa. Questo comando resta compilabile
    /// per preservare il materiale storico, ma non legge precondizioni, non trasferisce
    /// cibo, non scrive <c>NpcPrivateFood</c>, non scrive inventario typed e non
    /// pubblica <c>FoodStolenEvent</c>.
    /// </para>
    ///
    /// <para><b>Struttura interna:</b></para>
    /// <list type="bullet">
    ///   <item><b>Identita' richiesta</b>: conserva ladro, vittima e quantita' per diagnostica.</item>
    ///   <item><b>No-op runtime</b>: l'esecuzione registra solo un log debug stabile.</item>
    ///   <item><b>Boundary futuro</b>: il furto tornera' solo come Decision -> Job -> Step -> Command.</item>
    /// </list>
    /// </summary>
    public sealed class StealPrivateFoodCommand : ICommand
    {
        private readonly int _thiefNpcId;
        private readonly int _victimNpcId;
        private readonly int _units;

        public StealPrivateFoodCommand(int thiefNpcId, int victimNpcId)
            : this(thiefNpcId, victimNpcId, 1)
        {
        }

        public StealPrivateFoodCommand(int thiefNpcId, int victimNpcId, int units)
        {
            _thiefNpcId = thiefNpcId;
            _victimNpcId = victimNpcId;
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
        /// Non vengono applicate nemmeno le vecchie regole di adiacenza o linea di
        /// vista: il furto deve rientrare solo quando sara' un modulo completo dentro
        /// la nuova pipeline decisionale e lavorativa.
        /// </para>
        ///
        /// <para><b>Struttura interna:</b></para>
        /// <list type="bullet">
        ///   <item><b>Log</b>: emette solo diagnostica debug.</item>
        ///   <item><b>Nessun evento</b>: non pubblica <c>FoodStolenEvent</c>.</item>
        ///   <item><b>Nessuna mutazione</b>: non modifica NPC, inventari o bisogni.</item>
        /// </list>
        /// </summary>
        public void Execute(World world, MessageBus bus)
        {
            // v0.71.05.C6: il furto NPC -> NPC resta disattivato finche' non
            // esistera' un modulo completo con furtivita', illegalita', visibilita'
            // del furto, trauma della vittima e conseguenze sociali.
            ArcontioLogger.Debug(
                new LogContext(tick: (int)TickContext.CurrentTickIndex, channel: "World"),
                new LogBlock(LogLevel.Debug, "log.world.theft.private_food.legacy_sterilized")
                    .AddField("thief", _thiefNpcId)
                    .AddField("victim", _victimNpcId)
                    .AddField("requestedUnits", _units)
            );
        }
    }
}
