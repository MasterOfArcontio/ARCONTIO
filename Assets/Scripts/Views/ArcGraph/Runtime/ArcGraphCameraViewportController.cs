using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;
using UnityEngine.U2D;

namespace Arcontio.View.ArcGraph
{
    // =============================================================================
    // ArcGraphCameraViewportController
    // =============================================================================
    /// <summary>
    /// <para>
    /// Controller Unity dedicato alla camera e al viewport della vista ArcGraph.
    /// </para>
    ///
    /// <para><b>Principio architetturale: un solo proprietario della camera ArcGraph</b></para>
    /// <para>
    /// Questo componente concentra zoom, pan, clamp ai bordi mappa e calcolo della
    /// finestra visibile. Non legge <c>World</c>, non modifica NPC, non invia
    /// comandi e non decide contenuti simulativi. La sua responsabilita' e' solo
    /// visuale: trasformare input fisico Unity in posizione camera e poi aggiornare
    /// lo <c>ArcGraphViewState</c> condiviso con renderer e interaction boundary.
    /// </para>
    ///
    /// <para><b>Struttura interna:</b></para>
    /// <list type="bullet">
    ///   <item><b>controllerEnabled</b>: gate principale del controller camera.</item>
    ///   <item><b>processInUpdate</b>: polling automatico opzionale, spento dal runtime wrapper.</item>
    ///   <item><b>sceneCamera</b>: camera ortografica esplicita da controllare.</item>
    ///   <item><b>_config</b>: profilo zoom e dimensione mappa derivati dal JSON ArcGraph.</item>
    ///   <item><b>_viewState</b>: stato condiviso con renderer e interaction wrapper.</item>
    ///   <item><b>ProcessCurrentFrame</b>: legge mouse, applica zoom/pan e riallinea stato.</item>
    ///   <item><b>ResolveVisibleCellRect</b>: restituisce il rettangolo terrain realmente visto dalla camera.</item>
    /// </list>
    /// </summary>
    public sealed class ArcGraphCameraViewportController : MonoBehaviour
    {
        [SerializeField] private bool controllerEnabled;
        [SerializeField] private bool processInUpdate;
        [SerializeField] private bool useRightMousePan;
        [SerializeField] private bool useMiddleMousePan = true;
        [SerializeField] private bool ignoreInputWhenPointerIsOverUi = true;
        [SerializeField] private Camera sceneCamera;
        [SerializeField] private float minimumOrthographicSize = 0.5f;
        [SerializeField] private int visibleRectPaddingCells = 2;

        private ArcGraphMapViewConfig _config;
        private ArcGraphViewState _viewState;
        private bool _didApplyInitialCameraState;
        private bool _isZoomTransitionActive;
        private float _zoomTransitionStartOrthographicSize;
        private float _zoomTransitionTargetOrthographicSize;
        private float _zoomTransitionElapsedSeconds;
        private Vector2 _zoomTransitionAnchorScreenPosition;
        private Vector3 _zoomTransitionAnchorWorldPoint;
        private PixelPerfectCamera _zoomTransitionPixelPerfectCamera;
        private bool _zoomTransitionPixelPerfectWasEnabled;

        public ArcGraphViewState ViewState => EnsureViewState();
        public bool ControllerEnabled => controllerEnabled;

        // =============================================================================
        // Update
        // =============================================================================
        /// <summary>
        /// <para>
        /// Processa opzionalmente input camera in automatico.
        /// </para>
        ///
        /// <para><b>Default controllato dal wrapper</b></para>
        /// <para>
        /// Nel cablaggio runtime automatico questo flag resta spento e
        /// <c>ArcGraphMinimalRuntimeSceneWrapper</c> chiama il controller prima del
        /// rendering terrain. In questo modo la camera viene aggiornata prima del
        /// culling, senza dipendere dall'ordine globale degli <c>Update</c> Unity.
        /// </para>
        /// </summary>
        private void Update()
        {
            if (!processInUpdate)
                return;

            ProcessCurrentFrame();
        }

        // =============================================================================
        // SetControllerEnabled
        // =============================================================================
        /// <summary>
        /// <para>
        /// Abilita o disabilita il controllo camera ArcGraph.
        /// </para>
        /// </summary>
        public void SetControllerEnabled(bool enabled)
        {
            controllerEnabled = enabled;
        }

        // =============================================================================
        // SetProcessInUpdate
        // =============================================================================
        /// <summary>
        /// <para>
        /// Decide se il controller deve processare input nel proprio <c>Update</c>.
        /// </para>
        /// </summary>
        public void SetProcessInUpdate(bool enabled)
        {
            processInUpdate = enabled;
        }

        // =============================================================================
        // SetConfig
        // =============================================================================
        /// <summary>
        /// <para>
        /// Imposta la configurazione ArcGraph usata per zoom e clamp viewport.
        /// </para>
        ///
        /// <para><b>JSON come fonte della policy zoom</b></para>
        /// <para>
        /// Il controller non decide quanti zoom esistono. Riceve una config gia'
        /// materializzata: se il JSON dichiara uno, cinque o dieci livelli, il
        /// controller si limita a risolvere il prossimo livello tramite
        /// <c>ArcGraphMapViewConfig</c>.
        /// </para>
        /// </summary>
        public void SetConfig(ArcGraphMapViewConfig config)
        {
            ArcGraphMapViewConfig previousConfig = ResolveConfig();
            int previousDefaultZoomLevel = previousConfig.DefaultZoomLevel;
            bool shouldMoveDefaultViewState =
                _viewState != null &&
                _viewState.ActiveZoomLevel == previousDefaultZoomLevel;

            _config = config;
            ArcGraphMapViewConfig currentConfig = ResolveConfig();

            if (_viewState == null)
            {
                _viewState = ArcGraphViewState.CreateDefault(currentConfig);
                _didApplyInitialCameraState = false;
                return;
            }

            if (shouldMoveDefaultViewState &&
                previousDefaultZoomLevel != currentConfig.DefaultZoomLevel)
            {
                _viewState.SetZoomLevel(currentConfig.DefaultZoomLevel, currentConfig);
                _didApplyInitialCameraState = false;
            }
            else
            {
                _viewState.SetCenterCell(
                    _viewState.CenterCellX,
                    _viewState.CenterCellY,
                    currentConfig);
            }
        }

        // =============================================================================
        // SetSceneCamera
        // =============================================================================
        /// <summary>
        /// <para>
        /// Assegna la camera Unity controllata dal viewport ArcGraph.
        /// </para>
        /// </summary>
        public void SetSceneCamera(Camera camera)
        {
            if (sceneCamera == camera)
                return;

            sceneCamera = camera;
            _didApplyInitialCameraState = false;
        }

        // =============================================================================
        // SetViewState
        // =============================================================================
        /// <summary>
        /// <para>
        /// Permette a un bootstrap esterno di condividere uno stato vista gia'
        /// creato.
        /// </para>
        /// </summary>
        public void SetViewState(ArcGraphViewState viewState)
        {
            _viewState = viewState;
            _didApplyInitialCameraState = false;
        }

        // =============================================================================
        // ProcessCurrentFrameFromInspector
        // =============================================================================
        /// <summary>
        /// <para>
        /// Entry point manuale da Inspector per test camera controllati.
        /// </para>
        /// </summary>
        [ContextMenu("ArcGraph/Process Camera Viewport Frame")]
        public void ProcessCurrentFrameFromInspector()
        {
            ProcessCurrentFrame();
        }

        // =============================================================================
        // ProcessCurrentFrame
        // =============================================================================
        /// <summary>
        /// <para>
        /// Legge input camera, applica zoom/pan e aggiorna lo stato viewport.
        /// </para>
        ///
        /// <para><b>Sequenza intenzionale</b></para>
        /// <para>
        /// Prima la camera viene inizializzata dal livello zoom corrente. Poi, se il
        /// puntatore non e' sopra UI, vengono applicati zoom discreto e pan fisico.
        /// Alla fine la camera viene clampata sui bounds mappa e lo
        /// <c>ArcGraphViewState</c> viene riallineato alla posizione reale della
        /// camera. Renderer e picker leggono quindi la stessa fonte.
        /// </para>
        /// </summary>
        public bool ProcessCurrentFrame()
        {
            if (!controllerEnabled)
                return false;

            Camera camera = ResolveSceneCamera();
            if (camera == null)
                return false;

            ArcGraphMapViewConfig config = ResolveConfig();
            ArcGraphViewState viewState = EnsureViewState();
            EnsureInitialCameraState(camera, config, viewState);

            Mouse mouse = Mouse.current;
            bool canConsumeInput =
                mouse != null &&
                IsPointerInsideCameraViewport(camera, mouse.position.ReadValue()) &&
                (!ignoreInputWhenPointerIsOverUi || !IsPointerOverUi());

            bool didChangeCamera = false;
            if (canConsumeInput)
            {
                didChangeCamera |= TryApplyWheelZoom(camera, config, viewState, mouse);
                didChangeCamera |= TryApplyDragPan(camera, config, mouse);
            }

            didChangeCamera |= ApplyPendingZoomTransition(camera, config);
            ClampCameraToMap(camera, config);
            SyncViewStateFromCamera(camera, config, viewState);
            return didChangeCamera;
        }

        // =============================================================================
        // ResolveVisibleCellRect
        // =============================================================================
        /// <summary>
        /// <para>
        /// Calcola il rettangolo celle realmente coperto dalla camera ortografica.
        /// </para>
        ///
        /// <para><b>Principio architetturale: culling coerente con la camera reale</b></para>
        /// <para>
        /// Il profilo zoom JSON descrive il livello logico, ma la camera puo' vedere
        /// piu' celle orizzontali su viewport widescreen. Se il terrain usasse solo
        /// il rettangolo quadrato dello zoom, ai lati apparirebbero buchi blu
        /// durante pan o zoom. Questo metodo usa quindi <c>orthographicSize</c> e
        /// <c>aspect</c> reali della camera, aggiungendo un piccolo padding.
        /// </para>
        /// </summary>
        public ArcGraphViewCellRect ResolveVisibleCellRect()
        {
            ArcGraphMapViewConfig config = ResolveConfig();
            Camera camera = ResolveSceneCamera();

            if (camera == null)
                return EnsureViewState().ResolveVisibleCellRect(config);

            float halfHeight = Mathf.Max(0f, camera.orthographicSize);
            float halfWidth = halfHeight * Mathf.Max(0.0001f, camera.aspect);
            float centerX = camera.transform.position.x;
            float centerY = camera.transform.position.y;
            int padding = Mathf.Max(0, visibleRectPaddingCells);

            int minX = Mathf.FloorToInt(centerX - halfWidth) - padding;
            int minY = Mathf.FloorToInt(centerY - halfHeight) - padding;
            int maxX = Mathf.CeilToInt(centerX + halfWidth) + padding;
            int maxY = Mathf.CeilToInt(centerY + halfHeight) + padding;

            minX = ClampInt(minX, 0, config.MapWidthCells);
            minY = ClampInt(minY, 0, config.MapHeightCells);
            maxX = ClampInt(maxX, minX, config.MapWidthCells);
            maxY = ClampInt(maxY, minY, config.MapHeightCells);

            return new ArcGraphViewCellRect(minX, minY, maxX, maxY);
        }

        private void EnsureInitialCameraState(
            Camera camera,
            ArcGraphMapViewConfig config,
            ArcGraphViewState viewState)
        {
            if (_didApplyInitialCameraState)
                return;

            camera.orthographic = true;
            ApplyCameraZoomForViewState(camera, config, viewState);
            MoveCameraCenterTo(camera, viewState.CenterCellX, viewState.CenterCellY);
            ClampCameraToMap(camera, config);
            SyncViewStateFromCamera(camera, config, viewState);
            _didApplyInitialCameraState = true;
        }

        private bool TryApplyWheelZoom(
            Camera camera,
            ArcGraphMapViewConfig config,
            ArcGraphViewState viewState,
            Mouse mouse)
        {
            int wheelStep = ResolveWheelStep(mouse.scroll.ReadValue().y);
            if (wheelStep == 0)
                return false;

            int beforeLevel = viewState.ActiveZoomLevel;
            Vector2 pointerScreenPosition = mouse.position.ReadValue();
            Vector3 beforePointerWorld = ResolvePointerWorldPoint(camera, pointerScreenPosition);

            viewState.ApplyWheelZoom(wheelStep, config);
            if (viewState.ActiveZoomLevel == beforeLevel)
                return false;

            BeginZoomTransition(camera, config, viewState, pointerScreenPosition, beforePointerWorld);
            return true;
        }

        private bool TryApplyDragPan(
            Camera camera,
            ArcGraphMapViewConfig config,
            Mouse mouse)
        {
            ArcGraphViewZoomLevelDefinition zoom = EnsureViewState().CurrentZoom(config);
            if (!zoom.AllowsPan)
                return false;

            bool isPanHeld =
                (useRightMousePan && mouse.rightButton.isPressed) ||
                (useMiddleMousePan && mouse.middleButton.isPressed);

            if (!isPanHeld)
                return false;

            Vector2 deltaPixels = mouse.delta.ReadValue();
            if (deltaPixels.sqrMagnitude < 0.0001f)
                return false;

            float worldPerPixel = (2f * camera.orthographicSize) / Mathf.Max(1, camera.pixelHeight);
            Vector3 deltaWorld = new Vector3(
                -deltaPixels.x * worldPerPixel,
                -deltaPixels.y * worldPerPixel,
                0f);

            MoveCameraByOffset(camera, deltaWorld);
            return true;
        }

        private void ApplyCameraZoomForViewState(
            Camera camera,
            ArcGraphMapViewConfig config,
            ArcGraphViewState viewState)
        {
            ArcGraphViewZoomLevelDefinition zoom = viewState.CurrentZoom(config);
            if (zoom.VisibleCellsY <= 0)
                return;

            RestorePixelPerfectCameraAfterTransition(camera, zoom);

            float targetOrthographicSize = Mathf.Max(
                minimumOrthographicSize,
                zoom.VisibleCellsY * 0.5f);

            camera.orthographic = true;
            camera.orthographicSize = targetOrthographicSize;
            _isZoomTransitionActive = false;
        }

        private void BeginZoomTransition(
            Camera camera,
            ArcGraphMapViewConfig config,
            ArcGraphViewState viewState,
            Vector2 pointerScreenPosition,
            Vector3 pointerWorldPoint)
        {
            ArcGraphViewZoomLevelDefinition zoom = viewState.CurrentZoom(config);
            if (zoom.VisibleCellsY <= 0)
                return;

            SuspendPixelPerfectCameraForTransition(camera);
            _zoomTransitionStartOrthographicSize = camera.orthographicSize;
            _zoomTransitionTargetOrthographicSize = Mathf.Max(
                minimumOrthographicSize,
                zoom.VisibleCellsY * 0.5f);
            _zoomTransitionElapsedSeconds = 0f;
            _zoomTransitionAnchorScreenPosition = pointerScreenPosition;
            _zoomTransitionAnchorWorldPoint = pointerWorldPoint;
            _isZoomTransitionActive = true;

            if (config.ZoomTransitionSeconds <= 0.0001f)
            {
                ApplyZoomSizeAroundAnchor(camera, _zoomTransitionTargetOrthographicSize);
                RestorePixelPerfectCameraAfterTransition(camera, zoom);
                _isZoomTransitionActive = false;
            }
        }

        private bool ApplyPendingZoomTransition(
            Camera camera,
            ArcGraphMapViewConfig config)
        {
            if (!_isZoomTransitionActive)
                return false;

            float targetSize = Mathf.Max(
                minimumOrthographicSize,
                _zoomTransitionTargetOrthographicSize);
            float transitionSeconds = Mathf.Max(0f, config.ZoomTransitionSeconds);
            ArcGraphViewZoomLevelDefinition zoom = EnsureViewState().CurrentZoom(config);

            if (transitionSeconds <= 0.0001f)
            {
                ApplyZoomSizeAroundAnchor(camera, targetSize);
                RestorePixelPerfectCameraAfterTransition(camera, zoom);
                _isZoomTransitionActive = false;
                return true;
            }

            _zoomTransitionElapsedSeconds += Time.unscaledDeltaTime;
            float progress01 = Mathf.Clamp01(_zoomTransitionElapsedSeconds / transitionSeconds);
            float easedProgress01 = SmoothStep01(progress01);
            float nextSize = Mathf.Lerp(
                _zoomTransitionStartOrthographicSize,
                targetSize,
                easedProgress01);

            if (progress01 >= 1f || Mathf.Abs(nextSize - targetSize) < 0.001f)
            {
                nextSize = targetSize;
                _isZoomTransitionActive = false;
            }

            ApplyZoomSizeAroundAnchor(camera, nextSize);

            if (!_isZoomTransitionActive)
                RestorePixelPerfectCameraAfterTransition(camera, zoom);

            return true;
        }

        private void ApplyZoomSizeAroundAnchor(
            Camera camera,
            float orthographicSize)
        {
            camera.orthographic = true;
            camera.orthographicSize = Mathf.Max(minimumOrthographicSize, orthographicSize);

            Vector3 anchorAfterZoom = ResolvePointerWorldPoint(
                camera,
                _zoomTransitionAnchorScreenPosition);
            Vector3 pointerCompensation = _zoomTransitionAnchorWorldPoint - anchorAfterZoom;
            pointerCompensation.z = 0f;

            MoveCameraByOffset(camera, pointerCompensation);
        }

        // =============================================================================
        // SuspendPixelPerfectCameraForTransition
        // =============================================================================
        /// <summary>
        /// <para>
        /// Disattiva temporaneamente la PixelPerfectCamera mentre lo zoom interpola
        /// fra due dimensioni ortografiche.
        /// </para>
        /// </summary>
        private void SuspendPixelPerfectCameraForTransition(Camera camera)
        {
            if (camera == null)
                return;

            PixelPerfectCamera pixelPerfectCamera = camera.GetComponent<PixelPerfectCamera>();
            if (pixelPerfectCamera == null)
                return;

            _zoomTransitionPixelPerfectCamera = pixelPerfectCamera;
            _zoomTransitionPixelPerfectWasEnabled = pixelPerfectCamera.enabled;

            if (pixelPerfectCamera.enabled)
                pixelPerfectCamera.enabled = false;
        }

        // =============================================================================
        // RestorePixelPerfectCameraAfterTransition
        // =============================================================================
        /// <summary>
        /// <para>
        /// Ripristina lo stato precedente della PixelPerfectCamera e applica il PPU
        /// finale del livello zoom raggiunto.
        /// </para>
        /// </summary>
        private void RestorePixelPerfectCameraAfterTransition(
            Camera camera,
            ArcGraphViewZoomLevelDefinition zoom)
        {
            if (_zoomTransitionPixelPerfectCamera != null)
            {
                _zoomTransitionPixelPerfectCamera.enabled = _zoomTransitionPixelPerfectWasEnabled;
                _zoomTransitionPixelPerfectCamera = null;
                _zoomTransitionPixelPerfectWasEnabled = false;
            }

            ApplyPixelPerfectCameraZoomIfPresent(camera, zoom);
        }

        // =============================================================================
        // SmoothStep01
        // =============================================================================
        /// <summary>
        /// <para>
        /// Converte un progresso lineare in una curva morbida ease-in/ease-out.
        /// </para>
        /// </summary>
        private static float SmoothStep01(float value)
        {
            float clamped = Mathf.Clamp01(value);
            return clamped * clamped * (3f - (2f * clamped));
        }

        private void ApplyPixelPerfectCameraZoomIfPresent(
            Camera camera,
            ArcGraphViewZoomLevelDefinition zoom)
        {
            PixelPerfectCamera pixelPerfectCamera = camera.GetComponent<PixelPerfectCamera>();
            if (pixelPerfectCamera == null || !pixelPerfectCamera.enabled)
                return;

            int viewportHeight = camera.pixelHeight > 0
                ? camera.pixelHeight
                : Screen.height > 0
                    ? Screen.height
                    : 1080;
            int targetAssetsPpu = Mathf.Max(
                1,
                Mathf.RoundToInt(viewportHeight / (float)zoom.VisibleCellsY));

            if (pixelPerfectCamera.assetsPPU != targetAssetsPpu)
                pixelPerfectCamera.assetsPPU = targetAssetsPpu;
        }

        private void ClampCameraToMap(
            Camera camera,
            ArcGraphMapViewConfig config)
        {
            Vector3 current = camera.transform.position;
            Vector3 clamped = ClampCameraCenter(current, camera, config);
            Vector3 offset = clamped - current;
            offset.z = 0f;
            MoveCameraByOffset(camera, offset);
        }

        private Vector3 ClampCameraCenter(
            Vector3 current,
            Camera camera,
            ArcGraphMapViewConfig config)
        {
            float halfHeight = Mathf.Max(0f, camera.orthographicSize);
            float halfWidth = halfHeight * Mathf.Max(0.0001f, camera.aspect);

            current.x = ClampCameraAxis(current.x, config.MapWidthCells, halfWidth);
            current.y = ClampCameraAxis(current.y, config.MapHeightCells, halfHeight);
            return current;
        }

        private static float ClampCameraAxis(
            float current,
            int mapCells,
            float halfVisibleCells)
        {
            if (mapCells <= 0)
                return 0f;

            if (mapCells <= halfVisibleCells * 2f)
                return mapCells * 0.5f;

            return Mathf.Clamp(current, halfVisibleCells, mapCells - halfVisibleCells);
        }

        private void SyncViewStateFromCamera(
            Camera camera,
            ArcGraphMapViewConfig config,
            ArcGraphViewState viewState)
        {
            viewState.SetCenterCell(
                camera.transform.position.x,
                camera.transform.position.y,
                config);
        }

        private void MoveCameraCenterTo(
            Camera camera,
            float centerX,
            float centerY)
        {
            Vector3 current = camera.transform.position;
            Vector3 desired = new Vector3(centerX, centerY, current.z);
            Vector3 offset = desired - current;
            offset.z = 0f;
            MoveCameraByOffset(camera, offset);
        }

        private void MoveCameraByOffset(
            Camera camera,
            Vector3 offset)
        {
            if (camera == null || offset.sqrMagnitude < 0.000001f)
                return;

            camera.transform.position += offset;
        }

        private Camera ResolveSceneCamera()
        {
            if (sceneCamera != null)
                return sceneCamera;

            sceneCamera = Camera.main;
            return sceneCamera;
        }

        private ArcGraphMapViewConfig ResolveConfig()
        {
            return _config ?? ArcGraphMapViewConfig.CreateDefaultV033();
        }

        private ArcGraphViewState EnsureViewState()
        {
            _viewState ??= ArcGraphViewState.CreateDefault(ResolveConfig());
            return _viewState;
        }

        private static Vector3 ResolvePointerWorldPoint(
            Camera camera,
            Vector2 pointerScreenPosition)
        {
            float zDistance = Mathf.Abs(camera.transform.position.z);
            Vector3 screenPoint = new Vector3(
                pointerScreenPosition.x,
                pointerScreenPosition.y,
                zDistance);
            Vector3 worldPoint = camera.ScreenToWorldPoint(screenPoint);
            worldPoint.z = 0f;
            return worldPoint;
        }

        private static int ResolveWheelStep(float scrollY)
        {
            if (scrollY > 0.01f)
                return 1;

            if (scrollY < -0.01f)
                return -1;

            return 0;
        }

        private static bool IsPointerOverUi()
        {
            return EventSystem.current != null && EventSystem.current.IsPointerOverGameObject();
        }

        private static bool IsPointerInsideCameraViewport(
            Camera camera,
            Vector2 pointerScreenPosition)
        {
            if (camera == null)
                return false;

            Rect pixelRect = camera.pixelRect;
            return pixelRect.width > 0f &&
                   pixelRect.height > 0f &&
                   pixelRect.Contains(pointerScreenPosition);
        }

        private static int ClampInt(int value, int min, int max)
        {
            if (value < min) return min;
            if (value > max) return max;
            return value;
        }
    }
}
