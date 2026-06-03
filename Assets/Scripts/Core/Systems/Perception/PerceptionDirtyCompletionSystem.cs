using Arcontio.Core.Diagnostics;

namespace Arcontio.Core
{
    // =============================================================================
    // PerceptionDirtyCompletionSystem
    // =============================================================================
    /// <summary>
    /// <para>
    /// Chiude il blocco percettivo centrale del tick pulendo il dirty degli NPC
    /// che sono stati selezionati dal budget percettivo.
    /// </para>
    ///
    /// <para><b>Principio architetturale: chiusura percettiva separata dai sistemi</b></para>
    /// <para>
    /// La selezione degli NPC percettivi appartiene al <c>World</c>; la produzione
    /// di osservazioni appartiene ai sistemi specifici di percezione landmark,
    /// oggetti e NPC; la pulizia del dirty appartiene invece alla chiusura del
    /// blocco percettivo. Questo sistema evita che <c>NpcPerceptionSystem</c>
    /// diventi implicitamente responsabile della fine dell'intero ciclo.
    /// </para>
    ///
    /// <para><b>Struttura interna:</b></para>
    /// <list type="bullet">
    ///   <item><b>Period</b>: gira ogni tick, ma lavora solo sulla selezione gia' calcolata.</item>
    ///   <item><b>Dirty</b>: pulisce solo gli NPC selezionati per il tick corrente.</item>
    ///   <item><b>Eventi</b>: non produce eventi, memoria, belief o decisioni.</item>
    /// </list>
    /// </summary>
    public sealed class PerceptionDirtyCompletionSystem : ISystem
    {
        public int Period => 1;

        // =============================================================================
        // Update
        // =============================================================================
        /// <summary>
        /// <para>
        /// Completa il ciclo percettivo del tick corrente.
        /// </para>
        ///
        /// <para>
        /// Un NPC selezionato puo' anche non produrre eventi: per esempio puo'
        /// guardare davanti a se' e non vedere nulla. In quel caso la percezione
        /// e' comunque avvenuta e il dirty deve essere pulito, altrimenti lo stesso
        /// NPC verrebbe riproposto inutilmente nei tick successivi.
        /// </para>
        /// </summary>
        public void Update(World world, Tick tick, MessageBus bus, Telemetry telemetry)
        {
            if (world == null)
                return;

            world.CompleteNpcPerceptionUpdatesForTick(tick.Index);
        }
    }
}
