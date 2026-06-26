using UnityEngine;
using UnityEngine.InputSystem;

namespace Arcontio.View.ArcGraph
{
    // =============================================================================
    // ArcGraphUiNpcSpawnRequestBridge
    // =============================================================================
    /// <summary>
    /// <para>
    /// Ponte non distruttivo che completa la request NPC con la cella cliccata.
    /// </para>
    ///
    /// <para><b>Principio architetturale: click mappa senza comando</b></para>
    /// <para>
    /// Questo bridge ascolta il click solo quando la preview NPC UI e' attiva e il
    /// controller contiene una request valida. Al click registra la cella target
    /// nella request pending, ma non accoda <c>DevSpawnNpcCommand</c> e non modifica
    /// il <c>World</c>. Serve a rendere verificabile il contratto dello step
    /// corrente prima di introdurre il command bridge reale.
    /// </para>
    ///
    /// <para><b>Struttura interna:</b></para>
    /// <list type="bullet">
    ///   <item><b>previewSource</b>: risolve la cella sotto puntatore.</item>
    ///   <item><b>_npcSpawnController</b>: conserva la request pending.</item>
    ///   <item><b>Update</b>: su click sinistro aggiorna solo <c>TargetCell</c>.</item>
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
        /// Registra la cella cliccata nella request NPC, senza inviare comandi.
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
        }
    }
}
