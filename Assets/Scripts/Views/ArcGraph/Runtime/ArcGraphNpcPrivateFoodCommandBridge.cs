using System.Globalization;
using Arcontio.Core;
using Arcontio.Core.Commands.DevTools;
using UnityEngine;

namespace Arcontio.View.ArcGraph
{
    // =============================================================================
    // ArcGraphNpcPrivateFoodCommandBridge
    // =============================================================================
    /// <summary>
    /// <para>
    /// Bridge autorizzato tra RightInspector in modifica inventario NPC e comando
    /// runtime che aggiunge cibo privato addosso all'NPC selezionato.
    /// </para>
    ///
    /// <para><b>Principio architetturale: UI -> bridge -> CommandBuffer</b></para>
    /// <para>
    /// Il RightInspector non deve scrivere in <c>World.NpcPrivateFood</c>. Quando
    /// l'utente preme un bottone "+ cibo", la view passa qui target e quantita'.
    /// Il bridge normalizza l'id, costruisce <see cref="DevAddNpcPrivateFoodCommand"/>
    /// e lo accoda al <see cref="SimulationHost"/>. La validazione finale di NPC,
    /// quantita' e capienza resta nel comando Core.
    /// </para>
    ///
    /// <para><b>Struttura interna:</b></para>
    /// <list type="bullet">
    ///   <item><b>bridgeEnabled</b>: gate locale per spegnere la feature se serve.</item>
    ///   <item><b>simulationHost</b>: host esplicito assegnato dall'auto-installer.</item>
    ///   <item><b>RequestAddPrivateFood</b>: entry point chiamato dai bottoni UGUI.</item>
    /// </list>
    /// </summary>
    public sealed class ArcGraphNpcPrivateFoodCommandBridge : MonoBehaviour
    {
        [SerializeField] private bool bridgeEnabled = true;
        [SerializeField] private SimulationHost simulationHost;

        // =============================================================================
        // SetSimulationHost
        // =============================================================================
        /// <summary>
        /// <para>
        /// Assegna l'host runtime su cui accodare il comando autorizzato.
        /// </para>
        /// </summary>
        public void SetSimulationHost(SimulationHost host)
        {
            simulationHost = host;
        }

        // =============================================================================
        // RequestAddPrivateFood
        // =============================================================================
        /// <summary>
        /// <para>
        /// Traduce una richiesta UI di incremento cibo in un comando Core.
        /// </para>
        /// </summary>
        public bool RequestAddPrivateFood(ArcUiSelectionTarget target, int units)
        {
            if (!bridgeEnabled || units <= 0 || target.Kind != ArcUiSelectionTargetKind.Npc)
                return false;

            if (!int.TryParse(target.Id, NumberStyles.Integer, CultureInfo.InvariantCulture, out int npcId) || npcId <= 0)
                return false;

            SimulationHost host = simulationHost != null ? simulationHost : SimulationHost.Instance;
            if (host == null)
                return false;

            host.EnqueueExternalCommand(new DevAddNpcPrivateFoodCommand(npcId, units));
            return true;
        }
    }
}
