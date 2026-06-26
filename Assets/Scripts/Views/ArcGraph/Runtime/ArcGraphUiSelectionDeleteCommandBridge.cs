using Arcontio.Core;
using Arcontio.Core.Commands.DevTools;
using UnityEngine;

namespace Arcontio.View.ArcGraph
{
    // =============================================================================
    // ArcGraphUiSelectionDeleteCommandBridge
    // =============================================================================
    /// <summary>
    /// <para>
    /// Ponte temporaneo tra la richiesta UI di eliminazione e i comandi DevTools
    /// gia' autorizzati dal runtime.
    /// </para>
    ///
    /// <para><b>Principio architetturale: UI -> richiesta -> bridge -> comando</b></para>
    /// <para>
    /// Il piccolo menu della selezione produce una <see cref="ArcUiSelectionActionRequest"/>
    /// di tipo Delete. Questo componente la consuma una sola volta e la traduce in
    /// un comando accodato a <see cref="SimulationHost"/>. Non legge il <c>World</c>,
    /// non rimuove entita' direttamente e non decide se la cella sia valida: quelle
    /// guardie rimangono nei comandi Core temporanei.
    /// </para>
    ///
    /// <para><b>Struttura interna:</b></para>
    /// <list type="bullet">
    ///   <item><b>_selectionActionController</b>: sorgente della richiesta Delete pending.</item>
    ///   <item><b>selectionConsumer</b>: consumer selezione da pulire dopo invio comando.</item>
    ///   <item><b>Update</b>: consuma la richiesta una sola volta.</item>
    ///   <item><b>Command mapping</b>: NPC -> <c>DevEraseNpcAtCellCommand</c>, oggetto/muro -> <c>DevEraseObjectCommand</c>.</item>
    /// </list>
    /// </summary>
    public sealed class ArcGraphUiSelectionDeleteCommandBridge : MonoBehaviour
    {
        [SerializeField] private bool bridgeEnabled = true;
        [SerializeField] private ArcGraphUiSelectionSceneConsumer selectionConsumer;

        private ArcUiSelectionActionController _selectionActionController;

        // =============================================================================
        // SetSelectionActionController
        // =============================================================================
        /// <summary>
        /// <para>
        /// Collega il bridge al controller che contiene le azioni rapide della
        /// selezione.
        /// </para>
        /// </summary>
        public void SetSelectionActionController(ArcUiSelectionActionController controller)
        {
            _selectionActionController = controller;
        }

        // =============================================================================
        // SetSelectionConsumer
        // =============================================================================
        /// <summary>
        /// <para>
        /// Collega il consumer della selezione UI, usato solo per chiudere il
        /// pannello dopo che la richiesta e' stata inviata.
        /// </para>
        /// </summary>
        public void SetSelectionConsumer(ArcGraphUiSelectionSceneConsumer consumer)
        {
            selectionConsumer = consumer;
        }

        // =============================================================================
        // Update
        // =============================================================================
        /// <summary>
        /// <para>
        /// Consuma una richiesta Delete pending e la invia come comando runtime.
        /// </para>
        /// </summary>
        private void Update()
        {
            if (!bridgeEnabled || _selectionActionController == null)
                return;

            ArcUiSelectionActionRequest request = _selectionActionController.Pending;
            if (!request.IsDelete)
                return;

            bool commandEnqueued = TryEnqueueDeleteCommand(request.Target);

            // La richiesta viene sempre consumata, anche se il target non e'
            // supportato. Questo evita che un Delete non eseguibile venga rilanciato
            // ogni frame.
            _selectionActionController.Clear();

            if (commandEnqueued && selectionConsumer != null)
                selectionConsumer.ClearSelection();
        }

        // =============================================================================
        // TryEnqueueDeleteCommand
        // =============================================================================
        /// <summary>
        /// <para>
        /// Traduce il target selezionato nel comando di eliminazione temporaneo.
        /// </para>
        /// </summary>
        private static bool TryEnqueueDeleteCommand(ArcUiSelectionTarget target)
        {
            if (!target.IsValid)
                return false;

            SimulationHost host = SimulationHost.Instance;
            if (host == null)
                return false;

            switch (target.Kind)
            {
                case ArcUiSelectionTargetKind.Npc:
                    host.EnqueueExternalCommand(new DevEraseNpcAtCellCommand(
                        target.Cell.X,
                        target.Cell.Y));
                    return true;

                case ArcUiSelectionTargetKind.Object:
                case ArcUiSelectionTargetKind.Wall:
                    host.EnqueueExternalCommand(new DevEraseObjectCommand(
                        target.Cell.X,
                        target.Cell.Y));
                    return true;

                default:
                    return false;
            }
        }
    }
}
