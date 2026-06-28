using System.Globalization;
using Arcontio.Core;
using Arcontio.Core.Commands.DevTools;
using UnityEngine;

namespace Arcontio.View.ArcGraph
{
    // =============================================================================
    // ArcGraphObjectFoodStockCommandBridge
    // =============================================================================
    /// <summary>
    /// <para>
    /// Bridge autorizzato fra RightInspector ArcGraph e comando Core che modifica
    /// lo stock alimentare di un oggetto selezionato.
    /// </para>
    ///
    /// <para><b>Principio architetturale: view locale, comando simulativo differito</b></para>
    /// <para>
    /// Il RightInspector non legge ne' scrive <c>World.FoodStocks</c> quando l'utente
    /// preme i controlli di modifica. Passa qui solo target e parametri. Il bridge
    /// normalizza l'id oggetto e accoda un <see cref="DevEditObjectFoodStockCommand"/>
    /// al <see cref="SimulationHost"/>, lasciando al Core la validazione finale.
    /// </para>
    ///
    /// <para><b>Struttura interna:</b></para>
    /// <list type="bullet">
    ///   <item><b>bridgeEnabled</b>: gate runtime locale per spegnere la feature.</item>
    ///   <item><b>simulationHost</b>: host esplicito cablato dall'auto-installer.</item>
    ///   <item><b>RequestAdjustUnits</b>: variazione stepper dello stock.</item>
    ///   <item><b>RequestSetOwner*</b>: cambio proprietario Community/NPC.</item>
    /// </list>
    /// </summary>
    public sealed class ArcGraphObjectFoodStockCommandBridge : MonoBehaviour
    {
        [SerializeField] private bool bridgeEnabled = true;
        [SerializeField] private SimulationHost simulationHost;

        // =============================================================================
        // SetSimulationHost
        // =============================================================================
        /// <summary>
        /// <para>
        /// Assegna l'host runtime su cui accodare i comandi autorizzati.
        /// </para>
        /// </summary>
        public void SetSimulationHost(SimulationHost host)
        {
            simulationHost = host;
        }

        // =============================================================================
        // RequestAdjustUnits
        // =============================================================================
        /// <summary>
        /// <para>
        /// Traduce un click stepper in una richiesta incrementale di quantita'.
        /// </para>
        /// </summary>
        public bool RequestAdjustUnits(ArcUiSelectionTarget target, int deltaUnits)
        {
            if (!TryResolveObjectId(target, out int objectId) || deltaUnits == 0)
                return false;

            return Enqueue(DevEditObjectFoodStockCommand.AdjustUnits(objectId, deltaUnits));
        }

        // =============================================================================
        // RequestSetUnits
        // =============================================================================
        /// <summary>
        /// <para>
        /// Traduce una richiesta UI in un set assoluto della quantita' stock.
        /// </para>
        /// </summary>
        public bool RequestSetUnits(ArcUiSelectionTarget target, int units)
        {
            if (!TryResolveObjectId(target, out int objectId))
                return false;

            return Enqueue(DevEditObjectFoodStockCommand.SetUnits(objectId, units));
        }

        // =============================================================================
        // RequestSetOwnerCommunity
        // =============================================================================
        /// <summary>
        /// <para>
        /// Rende comunitario lo stock dell'oggetto target tramite comando Core.
        /// </para>
        /// </summary>
        public bool RequestSetOwnerCommunity(ArcUiSelectionTarget target)
        {
            if (!TryResolveObjectId(target, out int objectId))
                return false;

            return Enqueue(DevEditObjectFoodStockCommand.SetOwnerCommunity(objectId));
        }

        // =============================================================================
        // RequestSetOwnerNpc
        // =============================================================================
        /// <summary>
        /// <para>
        /// Assegna lo stock dell'oggetto target a uno specifico NPC.
        /// </para>
        /// </summary>
        public bool RequestSetOwnerNpc(ArcUiSelectionTarget target, int npcId)
        {
            if (!TryResolveObjectId(target, out int objectId) || npcId <= 0)
                return false;

            return Enqueue(DevEditObjectFoodStockCommand.SetOwnerNpc(objectId, npcId));
        }

        private bool TryResolveObjectId(ArcUiSelectionTarget target, out int objectId)
        {
            objectId = 0;

            if (!bridgeEnabled)
                return false;

            if (target.Kind != ArcUiSelectionTargetKind.Object && target.Kind != ArcUiSelectionTargetKind.Wall)
                return false;

            return int.TryParse(
                       target.Id,
                       NumberStyles.Integer,
                       CultureInfo.InvariantCulture,
                       out objectId)
                   && objectId > 0;
        }

        private bool Enqueue(DevEditObjectFoodStockCommand command)
        {
            SimulationHost host = simulationHost != null ? simulationHost : SimulationHost.Instance;
            if (host == null || command == null)
                return false;

            host.EnqueueExternalCommand(command);
            return true;
        }
    }
}
