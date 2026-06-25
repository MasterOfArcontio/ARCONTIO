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
    /// Non interpreta click, non accoda comandi, non legge direttamente il
    /// <c>World</c> e non sostituisce ancora il ponte placement definitivo. Se la
    /// nuova UI non ha nessuna preview attiva, il componente puo' delegare al vecchio
    /// F3 legacy tramite fallback, mantenendo stabile il comportamento gia' validato.
    /// </para>
    ///
    /// <para><b>Struttura interna:</b></para>
    /// <list type="bullet">
    ///   <item><b>SetPreviewDef</b>: abilita una preview oggetto dalla UI.</item>
    ///   <item><b>ClearPreview</b>: spegne la preview UI senza toccare F3.</item>
    ///   <item><b>SetFallbackPreviewSource</b>: collega il vecchio F3 come sorgente secondaria.</item>
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
        [SerializeField] private MonoBehaviour fallbackPreviewSourceBehaviour;

        private IArcGraphPlacementPreviewSource _fallbackPreviewSource;

        public bool IsObjectPlacementPreviewActive =>
            previewEnabled
                ? !string.IsNullOrWhiteSpace(previewDefId)
                : _fallbackPreviewSource != null && _fallbackPreviewSource.IsObjectPlacementPreviewActive;

        public bool IsPointerOverPlacementUi =>
            IsPointerOverCurrentUi()
                || (!previewEnabled
                    && _fallbackPreviewSource != null
                    && _fallbackPreviewSource.IsPointerOverPlacementUi);

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
        // SetFallbackPreviewSource
        // =============================================================================
        /// <summary>
        /// <para>
        /// Collega una sorgente preview secondaria, oggi il vecchio pannello F3.
        /// </para>
        /// </summary>
        public void SetFallbackPreviewSource(MonoBehaviour sourceBehaviour)
        {
            fallbackPreviewSourceBehaviour = sourceBehaviour;
            _fallbackPreviewSource = sourceBehaviour as IArcGraphPlacementPreviewSource;
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

            if (_fallbackPreviewSource != null)
                return _fallbackPreviewSource.TryGetActiveObjectPlacementPreviewDefId(out defId);

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
            {
                return _fallbackPreviewSource != null
                       && _fallbackPreviewSource.TryGetObjectPlacementPreviewCell(out cellX, out cellY);
            }

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
