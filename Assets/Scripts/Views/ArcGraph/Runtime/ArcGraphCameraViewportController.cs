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
    /// Controller Unity dedicato alla camera ortografica ArcGraph.
    /// </para>
    ///
    /// <para><b>Principio architetturale: zoom fisico semplice</b></para>
    /// <para>
    /// Il controller non usa piu' livelli zoom discreti. La rotellina modifica un
    /// target continuo di <c>Camera.orthographicSize</c>, lo smoothing viene
    /// applicato direttamente alla camera e lo stato condiviso conserva solo il
    /// centro vista. Lo zoom non decide sprite, LOD o animazioni.
    /// </para>
    /// </summary>
    public sealed class ArcGraphCameraViewportController : MonoBehaviour
    {
        [SerializeField] private bool controllerEnabled;
        [SerializeField] private bool processInUpdate;
        [SerializeField] private bool useRightMousePan;
        [SerializeField] private bool useMiddleMousePan = true;
        [SerializeField] private bool ignoreInputWhenPointerIsOverUi = true;
        [SerializeField] private Camera sceneCamera;
        [SerializeField] private int visibleRectPaddingCells = 2;

        private ArcGraphMapViewConfig _config;
        private ArcGraphViewState _viewState;
        private bool _didApplyInitialCameraState;
        private bool _hasZoomTarget;
        private float _targetOrthographicSize;
        private float _zoomVelocity;
        private Vector2 _zoomAnchorScreenPosition;
        private Vector3 _zoomAnchorWorldPoint;
        private Vector3 _panVelocityWorld;

        public ArcGraphViewState ViewState => EnsureViewState();
        public bool ControllerEnabled => controllerEnabled;

        private void Update()
        {
            if (!processInUpdate)
                return;

            ProcessCurrentFrame();
        }

        public void SetControllerEnabled(bool enabled)
        {
            controllerEnabled = enabled;
        }

        public void SetProcessInUpdate(bool enabled)
        {
            processInUpdate = enabled;
        }

        public void SetConfig(ArcGraphMapViewConfig config)
        {
            _config = config;
            ArcGraphMapViewConfig currentConfig = ResolveConfig();

            if (_viewState == null)
            {
                _viewState = ArcGraphViewState.CreateDefault(currentConfig);
                _didApplyInitialCameraState = false;
                return;
            }

            _viewState.SetCenterCell(
                _viewState.CenterCellX,
                _viewState.CenterCellY,
                currentConfig);
            _targetOrthographicSize = ClampOrthographicSize(
                _hasZoomTarget ? _targetOrthographicSize : currentConfig.DefaultOrthographicSize,
                currentConfig);
        }

        public void SetSceneCamera(Camera camera)
        {
            if (sceneCamera == camera)
                return;

            sceneCamera = camera;
            _didApplyInitialCameraState = false;
        }

        public void SetViewState(ArcGraphViewState viewState)
        {
            _viewState = viewState;
            _didApplyInitialCameraState = false;
        }

        [ContextMenu("ArcGraph/Process Camera Viewport Frame")]
        public void ProcessCurrentFrameFromInspector()
        {
            ProcessCurrentFrame();
        }

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
                didChangeCamera |= TryApplyWheelZoom(camera, config, mouse);
                didChangeCamera |= TryApplyDragPan(camera, config, mouse);
            }

            didChangeCamera |= ApplyPanInertia(camera, config, mouse);
            didChangeCamera |= ApplyZoomSmoothing(camera, config);
            Vector3 clampOffset = ClampCameraToMap(camera, config);
            ResolvePanVelocityAfterClamp(clampOffset);
            SyncViewStateFromCamera(camera, config, viewState);
            return didChangeCamera;
        }

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
            DisablePixelPerfectCameraIfPresent(camera);
            _targetOrthographicSize = ClampOrthographicSize(config.DefaultOrthographicSize, config);
            _hasZoomTarget = true;
            _zoomVelocity = 0f;
            _panVelocityWorld = Vector3.zero;
            camera.orthographicSize = _targetOrthographicSize;
            MoveCameraCenterTo(camera, viewState.CenterCellX, viewState.CenterCellY);
            ClampCameraToMap(camera, config);
            SyncViewStateFromCamera(camera, config, viewState);
            _didApplyInitialCameraState = true;
        }

        private bool TryApplyWheelZoom(
            Camera camera,
            ArcGraphMapViewConfig config,
            Mouse mouse)
        {
            int wheelStep = ResolveWheelStep(mouse.scroll.ReadValue().y);
            if (wheelStep == 0)
                return false;

            DisablePixelPerfectCameraIfPresent(camera);
            _zoomAnchorScreenPosition = mouse.position.ReadValue();
            _zoomAnchorWorldPoint = ResolvePointerWorldPoint(camera, _zoomAnchorScreenPosition);

            float currentTarget = _hasZoomTarget
                ? _targetOrthographicSize
                : camera.orthographicSize;
            float nextTarget = currentTarget - (wheelStep * config.ZoomStep);
            _targetOrthographicSize = ClampOrthographicSize(nextTarget, config);
            _hasZoomTarget = true;
            return true;
        }

        private bool TryApplyDragPan(
            Camera camera,
            ArcGraphMapViewConfig config,
            Mouse mouse)
        {
            if (!IsPanHeld(mouse))
                return false;

            Vector2 deltaPixels = mouse.delta.ReadValue();
            if (deltaPixels.sqrMagnitude < 0.0001f)
            {
                _panVelocityWorld = Vector3.zero;
                return false;
            }

            float worldPerPixel = (2f * camera.orthographicSize) / Mathf.Max(1, camera.pixelHeight);
            Vector3 deltaWorld = new Vector3(
                -deltaPixels.x * worldPerPixel,
                -deltaPixels.y * worldPerPixel,
                0f);

            MoveCameraByOffset(camera, deltaWorld);
            StorePanVelocity(deltaWorld, config);
            return true;
        }

        // =============================================================================
        // ApplyPanInertia
        // =============================================================================
        /// <summary>
        /// <para>
        /// Applica il trascinamento residuo dopo il rilascio del tasto pan.
        /// </para>
        ///
        /// <para>
        /// Il drag diretto resta identico al comportamento precedente: mentre il
        /// tasto e' premuto la camera segue il delta del mouse. Quando il tasto viene
        /// lasciato, questa funzione usa l'ultima velocita' mondo misurata e la
        /// riduce progressivamente fino allo stop. Non modifica il World, non cambia
        /// zoom e non introduce un secondo sistema camera.
        /// </para>
        /// </summary>
        private bool ApplyPanInertia(
            Camera camera,
            ArcGraphMapViewConfig config,
            Mouse mouse)
        {
            if (IsPanHeld(mouse))
                return false;

            if (!config.PanInertiaEnabled)
            {
                _panVelocityWorld = Vector3.zero;
                return false;
            }

            float deltaTime = Mathf.Max(0f, Time.unscaledDeltaTime);
            if (deltaTime <= 0.000001f)
                return false;

            float stopThreshold = Mathf.Max(0.0001f, config.PanInertiaStopThreshold);
            if (_panVelocityWorld.sqrMagnitude <= stopThreshold * stopThreshold)
            {
                _panVelocityWorld = Vector3.zero;
                return false;
            }

            Vector3 offset = _panVelocityWorld * deltaTime;
            MoveCameraByOffset(camera, offset);

            float damping = Mathf.Max(0.0001f, config.PanInertiaDamping);
            float decay = Mathf.Exp(-damping * deltaTime);
            _panVelocityWorld *= decay;
            return true;
        }

        private void StorePanVelocity(
            Vector3 deltaWorld,
            ArcGraphMapViewConfig config)
        {
            float deltaTime = Mathf.Max(0.000001f, Time.unscaledDeltaTime);
            float multiplier = Mathf.Max(0f, config.PanVelocityMultiplier);
            _panVelocityWorld = (deltaWorld / deltaTime) * multiplier;
            _panVelocityWorld.z = 0f;
        }

        private bool IsPanHeld(Mouse mouse)
        {
            if (mouse == null)
                return false;

            return (useRightMousePan && mouse.rightButton.isPressed) ||
                   (useMiddleMousePan && mouse.middleButton.isPressed);
        }

        private bool ApplyZoomSmoothing(
            Camera camera,
            ArcGraphMapViewConfig config)
        {
            if (!_hasZoomTarget)
                return false;

            float target = ClampOrthographicSize(_targetOrthographicSize, config);
            if (Mathf.Abs(camera.orthographicSize - target) <= 0.001f)
            {
                camera.orthographicSize = target;
                return false;
            }

            Vector3 anchorBefore = _zoomAnchorWorldPoint;
            float smoothTime = Mathf.Max(0f, config.ZoomSmoothTime);
            camera.orthographicSize = smoothTime <= 0.0001f
                ? target
                : Mathf.SmoothDamp(
                    camera.orthographicSize,
                    target,
                    ref _zoomVelocity,
                    smoothTime,
                    Mathf.Infinity,
                    Time.unscaledDeltaTime);

            Vector3 anchorAfter = ResolvePointerWorldPoint(camera, _zoomAnchorScreenPosition);
            Vector3 compensation = anchorBefore - anchorAfter;
            compensation.z = 0f;
            MoveCameraByOffset(camera, compensation);
            return true;
        }

        private static float ClampOrthographicSize(
            float value,
            ArcGraphMapViewConfig config)
        {
            return Mathf.Clamp(
                value,
                Mathf.Max(0.01f, config.MinOrthographicSize),
                Mathf.Max(config.MinOrthographicSize, config.MaxOrthographicSize));
        }

        private void DisablePixelPerfectCameraIfPresent(Camera camera)
        {
            PixelPerfectCamera pixelPerfectCamera = camera != null
                ? camera.GetComponent<PixelPerfectCamera>()
                : null;
            if (pixelPerfectCamera != null && pixelPerfectCamera.enabled)
                pixelPerfectCamera.enabled = false;
        }

        private Vector3 ClampCameraToMap(
            Camera camera,
            ArcGraphMapViewConfig config)
        {
            Vector3 current = camera.transform.position;
            Vector3 clamped = ClampCameraCenter(current, camera, config);
            Vector3 offset = clamped - current;
            offset.z = 0f;
            MoveCameraByOffset(camera, offset);
            return offset;
        }

        private void ResolvePanVelocityAfterClamp(Vector3 clampOffset)
        {
            if (clampOffset.sqrMagnitude < 0.000001f)
                return;

            if (Mathf.Abs(clampOffset.x) > 0.000001f)
                _panVelocityWorld.x = 0f;

            if (Mathf.Abs(clampOffset.y) > 0.000001f)
                _panVelocityWorld.y = 0f;
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

        private static void MoveCameraByOffset(
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
