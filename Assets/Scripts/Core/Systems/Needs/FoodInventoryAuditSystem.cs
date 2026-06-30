using Arcontio.Core.Diagnostics;

namespace Arcontio.Core
{
    // =============================================================================
    // FoodInventoryAuditSystem
    // =============================================================================
    /// <summary>
    /// <para>
    /// Sistema legacy Day9 che in passato deduceva sospetti furti osservando cali di
    /// <c>NpcPrivateFood</c>.
    /// </para>
    ///
    /// <para><b>v0.71.05.C6 - Sterilizzazione furto legacy</b></para>
    /// <para>
    /// Il sistema resta compilabile, ma non produce piu' eventi. La deduzione di un
    /// furto richiede un modulo dedicato con furtivita', percezione, illegalita',
    /// trauma e conseguenze sociali; non deve nascere da un audit automatico dello
    /// store legacy.
    /// </para>
    ///
    /// <para><b>Struttura interna:</b></para>
    /// <list type="bullet">
    ///   <item><b>No-op</b>: non legge differenze operative da <c>NpcPrivateFood</c>.</item>
    ///   <item><b>Telemetry</b>: registra solo che il sistema legacy e' sterilizzato.</item>
    ///   <item><b>Nessun evento</b>: non pubblica <c>FoodMissingSuspectedEvent</c>.</item>
    /// </list>
    /// </summary>
    public sealed class FoodInventoryAuditSystem : ISystem
    {
        public int Period => 1;

        // =============================================================================
        // Update
        // =============================================================================
        /// <summary>
        /// <para>
        /// Mantiene il sistema come no-op esplicito per evitare riattivazioni
        /// accidentali del vecchio furto.
        /// </para>
        /// </summary>
        public void Update(World world, Tick tick, MessageBus bus, Telemetry telemetry)
        {
            // v0.71.05.C6: nessuna lettura comparativa, nessun sospetto automatico,
            // nessun evento. Il futuro modulo furto dovra' produrre fatti osservabili
            // attraverso job/command autorizzati, non tramite audit globale.
            telemetry?.Counter("LegacyTheft.FoodInventoryAuditSterilized", 1);
        }
    }
}
