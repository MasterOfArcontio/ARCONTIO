using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;

namespace Arcontio.View.ArcGraph
{
    // =============================================================================
    // ArcGraphUiPlacementPreviewSource
    // =============================================================================
    /// <summary>
    /// <para>
    /// Sorgente read-only di preview placement pilotata dalla nuova UI runtime
    /// ArcGraph.
    /// </para>
    ///
    /// <para><b>Principio architetturale: preview UI senza comando</b></para>
    /// <para>
    /// Questo componente espone solo quale definizione oggetto la UI vuole
    /// visualizzare come fantasma rosso e quale cella si trova sotto il puntatore.
    /// Non interpreta click, non accoda comandi e non legge direttamente il
    /// <c>World</c>. Da questo step la preview dipende solo dal nuovo pannello
    /// azione ArcGraph: il vecchio F3 legacy non e' piu' una sorgente secondaria.
    /// </para>
    ///
    /// <para><b>Struttura interna:</b></para>
    /// <list type="bullet">
    ///   <item><b>SetPreviewDef</b>: abilita una preview oggetto dalla UI.</item>
    ///   <item><b>ClearPreview</b>: spegne la preview UI.</item>
    ///   <item><b>TryGetObjectPlacementPreviewCell</b>: converte mouse e camera in cella mappa.</item>
    /// </list>
    /// </summary>
    public sealed class ArcGraphUiPlacementPreviewSource : MonoBehaviour, IArcGraphPlacementPreviewSource
    {
        [SerializeField] private Camera sceneCamera;
        [SerializeField] private float tileWorldSize = 1f;
        [SerializeField] private Vector3 originOffset = Vector3.zero;
        [SerializeField] private string previewDefId;
        [SerializeField] private bool previewEnabled;
        public bool IsObjectPlacementPreviewActive =>
            previewEnabled && !string.IsNullOrWhiteSpace(previewDefId);

        public bool IsPointerOverPlacementUi =>
            IsPointerOverCurrentUi();

        public bool HasUiPlacementPreviewActive =>
            previewEnabled && !string.IsNullOrWhiteSpace(previewDefId);

        // =============================================================================
        // SetSceneCamera
        // =============================================================================
        /// <summary>
        /// <para>
        /// Assegna la camera usata per risolvere la cella sotto il puntatore.
        /// </para>
        /// </summary>
        public void SetSceneCamera(Camera camera)
        {
            sceneCamera = camera;
        }

        // =============================================================================
        // SetPreviewDef
        // =============================================================================
        /// <summary>
        /// <para>
        /// Attiva la preview UI per un <c>ObjectDef.Id</c> scelto nel pannello azione.
        /// </para>
        /// </summary>
        public void SetPreviewDef(string defId)
        {
            previewDefId = string.IsNullOrWhiteSpace(defId) ? string.Empty : defId.Trim();
            previewEnabled = !string.IsNullOrWhiteSpace(previewDefId);
        }

        // =============================================================================
        // ClearPreview
        // =============================================================================
        /// <summary>
        /// <para>
        /// Disattiva soltanto la preview prodotta dalla nuova UI.
        /// </para>
        /// </summary>
        public void ClearPreview()
        {
            previewDefId = string.Empty;
            previewEnabled = false;
        }

        // =============================================================================
        // TryGetActiveObjectPlacementPreviewDefId
        // =============================================================================
        /// <summary>
        /// <para>
        /// Restituisce il defId visualizzato dalla preview attiva.
        /// </para>
        /// </summary>
        public bool TryGetActiveObjectPlacementPreviewDefId(out string defId)
        {
            if (previewEnabled && !string.IsNullOrWhiteSpace(previewDefId))
            {
                defId = previewDefId;
                return true;
            }

            defId = string.Empty;
            return false;
        }

        // =============================================================================
        // TryGetObjectPlacementPreviewCell
        // =============================================================================
        /// <summary>
        /// <para>
        /// Restituisce la cella sotto il puntatore per la preview attiva.
        /// </para>
        /// </summary>
        public bool TryGetObjectPlacementPreviewCell(out int cellX, out int cellY)
        {
            cellX = 0;
            cellY = 0;

            if (!previewEnabled)
                return false;

            if (IsPointerOverCurrentUi())
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

        private static bool IsPointerOverCurrentUi()
        {
            return EventSystem.current != null && EventSystem.current.IsPointerOverGameObject();
        }
    }
}
