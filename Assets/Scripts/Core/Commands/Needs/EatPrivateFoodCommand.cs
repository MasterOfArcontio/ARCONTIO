namespace Arcontio.Core
{
    // =============================================================================
    // EatPrivateFoodCommand
    // =============================================================================
    /// <summary>
    /// <para>
    /// Alias compatibile per i call-site storici che chiedono di mangiare cibo
    /// personale dall'inventario typed.
    /// </para>
    ///
    /// <para><b>Principio architetturale: legacy spento, contratto conservato</b></para>
    /// <para>
    /// Da C7 il cibo personale operativo vive soltanto nell'inventario typed. Questa
    /// classe conserva il nome storico, ma inoltra subito al comando canonico
    /// <see cref="ConsumeInventoryItemCommand"/> senza possedere logica propria.
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
