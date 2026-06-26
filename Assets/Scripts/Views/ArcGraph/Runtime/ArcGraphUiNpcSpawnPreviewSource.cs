using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;

namespace Arcontio.View.ArcGraph
{
    // =============================================================================
    // ArcGraphUiNpcSpawnPreviewSource
    // =============================================================================
    /// <summary>
    /// <para>
    /// Sorgente read-only per la preview NPC guidata dalla nuova UI ArcGraph.
    /// </para>
    ///
    /// <para><b>Principio architetturale: preview NPC senza spawn</b></para>
    /// <para>
    /// Il componente espone solo la variante visuale, il facing e la cella sotto
    /// puntatore. Non accoda <c>DevSpawnNpcCommand</c>, non legge il <c>World</c> e
    /// non decide se la cella sia valida per lo spawn. Serve a separare il gesto UI
    /// dalla futura richiesta autorizzata.
    /// </para>
    ///
    /// <para><b>Struttura interna:</b></para>
    /// <list type="bullet">
    ///   <item><b>SetPreview</b>: abilita la preview NPC scelta nel pannello.</item>
    ///   <item><b>ClearPreview</b>: spegne la preview senza effetti runtime.</item>
    ///   <item><b>TryGetNpcPreviewCell</b>: converte mouse e camera in cella mappa.</item>
    /// </list>
    /// </summary>
    public sealed class ArcGraphUiNpcSpawnPreviewSource : MonoBehaviour
    {
        [SerializeField] private Camera sceneCamera;
        [SerializeField] private float tileWorldSize = 1f;
        [SerializeField] private Vector3 originOffset = Vector3.zero;
        [SerializeField] private bool previewEnabled;
        [SerializeField] private string visualKey = "human_default";
        [SerializeField] private ArcUiNpcSpawnFacing facing = ArcUiNpcSpawnFacing.South;

        public bool IsNpcPreviewActive => previewEnabled && !string.IsNullOrWhiteSpace(visualKey);
        public string VisualKey => string.IsNullOrWhiteSpace(visualKey) ? "human_default" : visualKey;
        public ArcUiNpcSpawnFacing Facing => facing;
        public bool IsPointerOverPlacementUi => IsPointerOverCurrentUi();

        // =============================================================================
        // SetSceneCamera
        // =============================================================================
        /// <summary>
        /// <para>
        /// Assegna la camera usata per convertire il puntatore in cella.
        /// </para>
        /// </summary>
        public void SetSceneCamera(Camera camera)
        {
            sceneCamera = camera;
        }

        // =============================================================================
        // SetPreview
        // =============================================================================
        /// <summary>
        /// <para>
        /// Attiva la preview NPC con visuale e facing indicati.
        /// </para>
        /// </summary>
        public void SetPreview(
            string requestedVisualKey,
            ArcUiNpcSpawnFacing requestedFacing)
        {
            visualKey = string.IsNullOrWhiteSpace(requestedVisualKey)
                ? "human_default"
                : requestedVisualKey.Trim().ToLowerInvariant();
            facing = requestedFacing;
            previewEnabled = true;
        }

        // =============================================================================
        // ClearPreview
        // =============================================================================
        /// <summary>
        /// <para>
        /// Disattiva la preview NPC senza toccare request o simulazione.
        /// </para>
        /// </summary>
        public void ClearPreview()
        {
            previewEnabled = false;
        }

        // =============================================================================
        // TryGetNpcPreviewCell
        // =============================================================================
        /// <summary>
        /// <para>
        /// Restituisce la cella sotto puntatore per la preview NPC attiva.
        /// </para>
        /// </summary>
        public bool TryGetNpcPreviewCell(
            out int cellX,
            out int cellY)
        {
            cellX = 0;
            cellY = 0;

            if (!IsNpcPreviewActive || IsPointerOverPlacementUi)
                return false;

            Mouse mouse = Mouse.current;
            Camera camera = sceneCamera != null ? sceneCamera : Camera.main;
            if (mouse == null || camera == null)
                return false;

            Vector2 screenPosition = mouse.position.ReadValue();
            float planeDistance = originOffset.z - camera.transform.position.z;
            Vector3 worldPosition = camera.ScreenToWorldPoint(new Vector3(
                screenPosition.x,
                screenPosition.y,
                planeDistance));

            float safeTileWorldSize = tileWorldSize > 0f ? tileWorldSize : 1f;
            cellX = Mathf.FloorToInt((worldPosition.x - originOffset.x) / safeTileWorldSize);
            cellY = Mathf.FloorToInt((worldPosition.y - originOffset.y) / safeTileWorldSize);
            return true;
        }

        public static string ToDirectionKey(ArcUiNpcSpawnFacing facingValue)
        {
            return facingValue switch
            {
                ArcUiNpcSpawnFacing.North => "north",
                ArcUiNpcSpawnFacing.East => "east",
                ArcUiNpcSpawnFacing.West => "west",
                _ => "south"
            };
        }

        private static bool IsPointerOverCurrentUi()
        {
            return EventSystem.current != null && EventSystem.current.IsPointerOverGameObject();
        }
    }
}
