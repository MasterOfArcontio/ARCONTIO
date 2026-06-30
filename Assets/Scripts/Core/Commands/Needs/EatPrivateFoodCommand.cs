namespace Arcontio.Core
{
    // =============================================================================
    // EatPrivateFoodCommand
    // =============================================================================
    /// <summary>
    /// <para>
    /// Bridge temporaneo per i call-site storici che chiedono di mangiare cibo
    /// personale.
    /// </para>
    ///
    /// <para><b>Principio architetturale: legacy spento, contratto conservato</b></para>
    /// <para>
    /// Da C4 il cibo personale operativo vive nell'inventario typed. Questa classe
    /// non legge e non scrive piu' <c>World.NpcPrivateFood</c>: inoltra la richiesta
    /// al comando canonico <see cref="ConsumeInventoryItemCommand"/> e verra'
    /// rimossa quando C7 eliminera' i residui runtime legacy.
    /// </para>
    ///
    /// <para><b>Struttura interna:</b></para>
    /// <list type="bullet">
    ///   <item><b>NpcId</b>: NPC che deve consumare un alimento posseduto.</item>
    ///   <item><b>Execute</b>: delega al consumo typed senza produrre eventi duplicati.</item>
    /// </list>
    /// </summary>
    public sealed class EatPrivateFoodCommand : ICommand
    {
        private readonly int _npcId;

        // =============================================================================
        // EatPrivateFoodCommand
        // =============================================================================
        /// <summary>
        /// <para>
        /// Mantiene il costruttore storico basato solo su NPC.
        /// </para>
        /// </summary>
        public EatPrivateFoodCommand(int npcId)
        {
            _npcId = npcId;
        }

        // =============================================================================
        // Execute
        // =============================================================================
        /// <summary>
        /// <para>
        /// Consuma il miglior cibo disponibile nell'inventario typed dell'NPC.
        /// </para>
        /// </summary>
        public void Execute(World world, MessageBus bus)
        {
            new ConsumeInventoryItemCommand(_npcId).Execute(world, bus);
        }
    }
}
