using Arcontio.Core.Diagnostics;

namespace Arcontio.Core
{
    // =============================================================================
    // PrivateFoodAuditSystem
    // =============================================================================
    /// <summary>
    /// <para>
    /// Sistema legacy Day9 che in passato confrontava periodicamente il cibo privato
    /// di un NPC per produrre un evento di cibo mancante.
    /// </para>
    ///
    /// <para><b>v0.71.05.C6 - Sterilizzazione furto legacy</b></para>
    /// <para>
    /// Il sistema non deve piu' produrre furto, sospetto furto o cibo mancante da
    /// <c>NpcPrivateFood</c>. Il furto verra' riprogettato come modulo autonomo,
    /// attraversando decisione, job, step, command ed eventi osservabili.
    /// </para>
    ///
    /// <para><b>Struttura interna:</b></para>
    /// <list type="bullet">
    ///   <item><b>Configurazione legacy</b>: conserva il costruttore con cadenza storica.</item>
    ///   <item><b>No-op</b>: non legge o confronta <c>NpcPrivateFood</c>.</item>
    ///   <item><b>Nessun evento</b>: non pubblica <c>FoodMissingEvent</c>.</item>
    /// </list>
    /// </summary>
    public sealed class PrivateFoodAuditSystem : ISystem
    {
        private readonly int _auditEveryTicks;

        public int Period => 1;

        public PrivateFoodAuditSystem(int auditEveryTicks = 20)
        {
            _auditEveryTicks = auditEveryTicks <= 0 ? 20 : auditEveryTicks;
        }

        // =============================================================================
        // Update
        // =============================================================================
        /// <summary>
        /// <para>
        /// Mantiene il vecchio audit come no-op difensivo.
        /// </para>
        /// </summary>
        public void Update(World world, Tick tick, MessageBus bus, Telemetry telemetry)
        {
            // La cadenza resta solo per rendere stabile il comportamento diagnostico
            // storico: anche se qualcuno riagganciasse il sistema, non produrrebbe
            // eventi ogni tick e non riattiverebbe il furto.
            if ((tick.Index % _auditEveryTicks) != 0)
                return;

            telemetry?.Counter("LegacyTheft.PrivateFoodAuditSterilized", 1);
        }
    }
}
