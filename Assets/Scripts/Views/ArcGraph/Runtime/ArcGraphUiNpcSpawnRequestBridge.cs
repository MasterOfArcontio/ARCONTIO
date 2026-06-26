using Arcontio.Core;
using Arcontio.Core.Commands.DevTools;
using UnityEngine;
using UnityEngine.InputSystem;

namespace Arcontio.View.ArcGraph
{
    // =============================================================================
    // ArcGraphUiNpcSpawnRequestBridge
    // =============================================================================
    /// <summary>
    /// <para>
    /// Ponte temporaneo che completa la request NPC con la cella cliccata e la
    /// trasforma nel comando DevTools autorizzato.
    /// </para>
    ///
    /// <para><b>Principio architetturale: UI -> request -> bridge -> command gateway</b></para>
    /// <para>
    /// Questo bridge ascolta il click solo quando la preview NPC UI e' attiva e il
    /// controller contiene una request valida. Al click registra la cella target
    /// nella request pending e accoda un <see cref="DevSpawnNpcCommand"/> al
    /// <see cref="SimulationHost"/>. Non crea NPC direttamente e non legge il
    /// <c>World</c>: tutte le guardie runtime restano nel comando autorizzato.
    /// </para>
    ///
    /// <para><b>Struttura interna:</b></para>
    /// <list type="bullet">
    ///   <item><b>previewSource</b>: risolve la cella sotto puntatore.</item>
    ///   <item><b>_npcSpawnController</b>: conserva la request pending.</item>
    ///   <item><b>Update</b>: su click sinistro aggiorna <c>TargetCell</c> e accoda il comando.</item>
    ///   <item><b>Command mapping</b>: converte il facing UI nel facing Core.</item>
    /// </list>
    /// </summary>
    public sealed class ArcGraphUiNpcSpawnRequestBridge : MonoBehaviour
    {
        [SerializeField] private ArcGraphUiNpcSpawnPreviewSource previewSource;
        [SerializeField] private bool bridgeEnabled = true;

        private ArcUiNpcSpawnController _npcSpawnController;

        // =============================================================================
        // SetNpcSpawnController
        // =============================================================================
        /// <summary>
        /// <para>
        /// Collega il bridge al controller request NPC.
        /// </para>
        /// </summary>
        public void SetNpcSpawnController(ArcUiNpcSpawnController controller)
        {
            _npcSpawnController = controller;
        }

        // =============================================================================
        // SetPreviewSource
        // =============================================================================
        /// <summary>
        /// <para>
        /// Collega il bridge alla sorgente preview NPC.
        /// </para>
        /// </summary>
        public void SetPreviewSource(ArcGraphUiNpcSpawnPreviewSource source)
        {
            previewSource = source;
        }

        // =============================================================================
        // Update
        // =============================================================================
        /// <summary>
        /// <para>
        /// Registra la cella cliccata nella request NPC e invia il comando
        /// temporaneo autorizzato.
        /// </para>
        /// </summary>
        private void Update()
        {
            if (!bridgeEnabled || _npcSpawnController == null || previewSource == null)
                return;

            if (!_npcSpawnController.Pending.IsValid || !previewSource.IsNpcPreviewActive)
                return;

            if (previewSource.IsPointerOverPlacementUi)
                return;

            Mouse mouse = Mouse.current;
            if (mouse == null || !mouse.leftButton.wasPressedThisFrame)
                return;

            if (!previewSource.TryGetNpcPreviewCell(out int cellX, out int cellY))
                return;

            _npcSpawnController.SetTargetCell(new ArcGraphCellCoord(cellX, cellY, 0));
            EnqueueSpawnCommand(_npcSpawnController.Pending);
        }

        // =============================================================================
        // EnqueueSpawnCommand
        // =============================================================================
        /// <summary>
        /// <para>
        /// Accoda lo spawn NPC temporaneo al runtime, senza mutare direttamente il
        /// mondo dalla UI.
        /// </para>
        /// </summary>
        private static void EnqueueSpawnCommand(ArcUiNpcSpawnRequest request)
        {
            if (!request.IsValid || !request.HasTargetCell)
                return;

            SimulationHost host = SimulationHost.Instance;
            if (host == null)
                return;

            host.EnqueueExternalCommand(new DevSpawnNpcCommand(
                request.TargetCell.X,
                request.TargetCell.Y,
                MapFacing(request.Config.Facing)));
        }

        // =============================================================================
        // MapFacing
        // =============================================================================
        /// <summary>
        /// <para>
        /// Converte il facing asciutto della UI nel facing discreto del Core.
        /// </para>
        /// </summary>
        private static CardinalDirection MapFacing(ArcUiNpcSpawnFacing facing)
        {
            switch (facing)
            {
                case ArcUiNpcSpawnFacing.South:
                    return CardinalDirection.South;
                case ArcUiNpcSpawnFacing.East:
                    return CardinalDirection.East;
                case ArcUiNpcSpawnFacing.West:
                    return CardinalDirection.West;
                case ArcUiNpcSpawnFacing.North:
                default:
                    return CardinalDirection.North;
            }
        }
    }
}
