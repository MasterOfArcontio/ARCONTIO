// Assets/Scripts/Views/ArcGraph/Runtime/ArcGraphObjectEditCommandBridge.cs
using System.Globalization;
using Arcontio.Core;
using Arcontio.Core.Commands.DevTools;
using UnityEngine;

namespace Arcontio.View.ArcGraph
{
    // =============================================================================
    // ArcGraphObjectEditCommandBridge
    // =============================================================================
    /// <summary>
    /// <para>
    /// Bridge autorizzato fra RightInspector ArcGraph e comando Core per modifiche
    /// generiche sugli oggetti selezionati.
    /// </para>
    ///
    /// <para><b>UI senza mutazione diretta del World</b></para>
    /// <para>
    /// Il pannello UGUI non scrive <c>WorldObjectInstance</c>, non apre porte e non
    /// cambia lock. Ogni bottone passa da questo bridge, che normalizza il target e
    /// accoda un <see cref="DevEditObjectCommand"/> al <see cref="SimulationHost"/>.
    /// La validazione definitiva resta nel Core.
    /// </para>
    ///
    /// <para><b>Struttura interna:</b></para>
    /// <list type="bullet">
    ///   <item><b>Owner</b>: Community oppure NPC esistente, validato poi dal comando.</item>
    ///   <item><b>Door</b>: chiusa, aperta o locked come stati espliciti.</item>
    ///   <item><b>Enqueue</b>: unico punto locale che parla con il SimulationHost.</item>
    /// </list>
    /// </summary>
    public sealed class ArcGraphObjectEditCommandBridge : MonoBehaviour
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
        // RequestSetOwnerCommunity
        // =============================================================================
        /// <summary>
        /// <para>
        /// Richiede proprieta' comunitaria per l'oggetto target.
        /// </para>
        /// </summary>
        public bool RequestSetOwnerCommunity(ArcUiSelectionTarget target)
        {
            if (!TryResolveObjectId(target, out int objectId))
                return false;

            return Enqueue(DevEditObjectCommand.SetOwnerCommunity(objectId));
        }

        // =============================================================================
        // RequestSetOwnerNpc
        // =============================================================================
        /// <summary>
        /// <para>
        /// Richiede proprieta' NPC per l'oggetto target.
        /// </para>
        /// </summary>
        public bool RequestSetOwnerNpc(ArcUiSelectionTarget target, int npcId)
        {
            if (!TryResolveObjectId(target, out int objectId) || npcId <= 0)
                return false;

            return Enqueue(DevEditObjectCommand.SetOwnerNpc(objectId, npcId));
        }

        // =============================================================================
        // RequestSetDoorClosed
        // =============================================================================
        /// <summary>
        /// <para>
        /// Richiede stato porta chiusa non locked.
        /// </para>
        /// </summary>
        public bool RequestSetDoorClosed(ArcUiSelectionTarget target)
        {
            if (!TryResolveObjectId(target, out int objectId))
                return false;

            return Enqueue(DevEditObjectCommand.SetDoorClosed(objectId));
        }

        // =============================================================================
        // RequestSetDoorOpen
        // =============================================================================
        /// <summary>
        /// <para>
        /// Richiede apertura porta.
        /// </para>
        /// </summary>
        public bool RequestSetDoorOpen(ArcUiSelectionTarget target)
        {
            if (!TryResolveObjectId(target, out int objectId))
                return false;

            return Enqueue(DevEditObjectCommand.SetDoorOpen(objectId));
        }

        // =============================================================================
        // RequestSetDoorLocked
        // =============================================================================
        /// <summary>
        /// <para>
        /// Richiede chiusura locked della porta target.
        /// </para>
        /// </summary>
        public bool RequestSetDoorLocked(ArcUiSelectionTarget target)
        {
            if (!TryResolveObjectId(target, out int objectId))
                return false;

            return Enqueue(DevEditObjectCommand.SetDoorLocked(objectId));
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

        private bool Enqueue(DevEditObjectCommand command)
        {
            SimulationHost host = simulationHost != null ? simulationHost : SimulationHost.Instance;
            if (host == null || command == null)
                return false;

            host.EnqueueExternalCommand(command);
            return true;
        }
    }
}
