using UnityEngine;
using System.Collections;

namespace Com.LuisPedroFonseca.ProCamera2D
{
    public class ProCamera2DPanAndZoom : BasePC2D, ISizeDeltaChanger, IPreMover
    {
        public static string ExtensionName = "Pan And Zoom";

        // Zoom
        public bool AllowZoom = true;

        public float MouseZoomSpeed = 10f;
        public float PinchZoomSpeed = 50f;

        [Range(0, 2f)]
        public float ZoomSmoothness = .2f;

        public float MaxZoomInAmount = 2f;
        public float MaxZoomOutAmount = 2f;

        public bool ZoomToInputCenter = true;

        float _zoomAmount;

        float _initialCamSize;

        bool _zoomStarted;
        float _origFollowSmoothnessX;
        float _origFollowSmoothnessY;

        float _prevZoomAmount;
        float _zoomVelocity;

        Vector3 _zoomPoint;

        float _touchZoomTime;

        // Pan
        public bool AllowPan = true;

        public bool UsePanByDrag = true;

        [Range(0f, 1f)]
        public float StopSpeedOnDragStart = .95f;

        public Rect DraggableAreaRect = new Rect(0f, 0f, 1f, 1f);

        public Vector2 DragPanSpeed = new Vector2(80f, 80f);

        public bool UsePanByMoveToEdges = false;

        public Vector2 EdgesPanSpeed = new Vector2(2f, 2f);

        [Range(0, .99f)]
        public float HorizontalPanEdges = .9f;

        [Range(0, .99f)]
        public float VerticalPanEdges = .9f;

        Vector2 _panDelta;

        Transform _panTarget;

        Vector2 _prevMousePosition;

        protected override void Awake()
        {
            base.Awake();

            UpdateCurrentFollowSmoothness();

            _panTarget = new GameObject("PC2DPanTarget").transform;

            ProCamera2D.AddPreMover(this);
            ProCamera2D.AddSizeDeltaChanger(this);
        }

        void Start()
        {
            _initialCamSize = ProCamera2D.ScreenSizeInWorldCoordinates.y * .5f;
        }

        protected override void OnEnable()
        {
            base.OnEnable();

            CenterPanTargetOnCamera(1f);

            ProCamera2D.Instance.AddCameraTarget(_panTarget);
        }

        protected override void OnDisable()
        {
            base.OnDisable();

            ProCamera2D.RemoveCameraTarget(_panTarget);
        }

        #region IPreMover implementation

        public void PreMove(float deltaTime)
        {
            if (enabled && AllowPan)
                Pan(deltaTime);
        }

        public int PrMOrder { get { return _prmOrder; } set { _prmOrder = value; } }

        int _prmOrder = 0;

        #endregion

        #region ISizeDeltaChanger implementation

        public float AdjustSize(float deltaTime, float originalDelta)
        {
            if (enabled && AllowZoom)
                return Zoom(deltaTime) + originalDelta;

            return originalDelta;
        }

        public int SDCOrder { get { return _sdcOrder; } set { _sdcOrder = value; } }
        int _sdcOrder = 0;

        #endregion

        void Pan(float deltaTime)
        {
            _panDelta = Vector2.zero;

            #if UNITY_IOS || UNITY_ANDROID || UNITY_WINRT
            // Time since zoom
            if (Time.time - _touchZoomTime < .1f)
                return;

            // Reset camera inertia on pan start
            if(Input.touchCount == 1 && Input.GetTouch(0).phase == TouchPhase.Began)
            {
                CenterPanTargetOnCamera(StopSpeedOnDragStart);
            }

            // Touch delta
            var averageDelta = Vector2.zero;
            for (int i = 0; i < Input.touchCount; i++)
            {
                averageDelta += -Input.GetTouch(i).deltaPosition;
            }

            if (Input.touchCount == 1)
            {
                var normalizedTouchPos = new Vector2(Input.GetTouch(0).position.x / Screen.width, Input.GetTouch(0).position.y / Screen.height);
                
                if(InsideDraggableArea(normalizedTouchPos))
                    _panDelta = new Vector2(averageDelta.x / Screen.width, averageDelta.y / Screen.height);
            }
            #endif

            var panSpeed = DragPanSpeed;

            #if UNITY_STANDALONE || UNITY_WEBGL || UNITY_WEBPLAYER || UNITY_EDITOR
            // Reset camera inertia on pan start
            if(UsePanByDrag && Input.GetMouseButtonDown(0))
            {
                CenterPanTargetOnCamera(StopSpeedOnDragStart);
            }

            // Mouse drag delta
            var normalizedMousePos = new Vector2(Input.mousePosition.x / Screen.width, Input.mousePosition.y / Screen.height);
            if (UsePanByDrag && Input.GetMouseButton(0))
            {
                if (InsideDraggableArea(normalizedMousePos))
                    _panDelta = _prevMousePosition - normalizedMousePos;
            }
            // Move to edges delta
            else if (UsePanByMoveToEdges && !Input.GetMouseButton(0))
            {
                var normalizedMousePosX = (-Screen.width * .5f + Input.mousePosition.x) / Screen.width;
                var normalizedMousePosY = (-Screen.height * .5f + Input.mousePosition.y) / Screen.height;

                if (normalizedMousePosX < 0)
                    normalizedMousePosX = normalizedMousePosX.Remap(-.5f, -HorizontalPanEdges * .5f, -.5f, 0f);
                else if (normalizedMousePosX > 0)
                    normalizedMousePosX = normalizedMousePosX.Remap(HorizontalPanEdges * .5f, .5f, 0f, .5f);
                
                if (normalizedMousePosY < 0)
                    normalizedMousePosY = normalizedMousePosY.Remap(-.5f, -VerticalPanEdges * .5f, -.5f, 0f);
                else if (normalizedMousePosY > 0)
                    normalizedMousePosY = normalizedMousePosY.Remap(VerticalPanEdges * .5f, .5f, 0f, .5f);

                _panDelta = new Vector2(normalizedMousePosX, normalizedMousePosY);

                if(_panDelta != Vector2.zero)
                    panSpeed = EdgesPanSpeed;
            }

            _prevMousePosition = normalizedMousePos;
            #endif

            // Move
            if (_panDelta != Vector2.zero)
            {
                _panTarget.Translate(VectorHV(
                        _panDelta.x * panSpeed.x * ProCamera2D.ScreenSizeInWorldCoordinates.x, 
                        _panDelta.y * panSpeed.y * ProCamera2D.ScreenSizeInWorldCoordinates.y) * deltaTime);
            }

            // Check if target is outside of bounds
            if ((ProCamera2D.IsCameraPositionLeftBounded && Vector3H(_panTarget.position) < Vector3H(ProCamera2D.LocalPosition)) ||
                (ProCamera2D.IsCameraPositionRightBounded && Vector3H(_panTarget.position) > Vector3H(ProCamera2D.LocalPosition)))
                _panTarget.position = VectorHVD(Vector3H(ProCamera2D.LocalPosition), Vector3V(_panTarget.position), Vector3D(_panTarget.position));

            if ((ProCamera2D.IsCameraPositionBottomBounded && Vector3V(_panTarget.position) < Vector3V(ProCamera2D.LocalPosition)) ||
                (ProCamera2D.IsCameraPositionTopBounded && Vector3V(_panTarget.position) > Vector3V(ProCamera2D.LocalPosition)))
                _panTarget.position = VectorHVD(Vector3H(_panTarget.position), Vector3V(ProCamera2D.LocalPosition), Vector3D(_panTarget.position));
        }

        float Zoom(float deltaTime)
        {
            if (_panDelta != Vector2.zero)
            {
                CancelZoom();
                RestoreFollowSmoothness();
                return 0;
            }

            var zoomInput = 0f;

            #if UNITY_IOS || UNITY_ANDROID || UNITY_WINRT
            if (Input.touchCount == 2)
            {
                var touchZero = Input.GetTouch(0);
                var touchOne = Input.GetTouch(1);

                var touchZeroPrevPos = touchZero.position - new Vector2(touchZero.deltaPosition.x / Screen.width, touchZero.deltaPosition.y / Screen.height);
                var touchOnePrevPos = touchOne.position - new Vector2(touchOne.deltaPosition.x / Screen.width, touchOne.deltaPosition.y / Screen.height);

                var prevTouchDeltaMag = (touchZeroPrevPos - touchOnePrevPos).magnitude;
                var touchDeltaMag = (touchZero.position - touchOne.position).magnitude;

                // Zoom input
                zoomInput = prevTouchDeltaMag - touchDeltaMag;

                // Zoom point
                var midTouch = (touchZero.position + touchOne.position) * .5f;
                _zoomPoint = VectorHVD(midTouch.x, midTouch.y, Mathf.Abs(ProCamera2D.CameraDepthPos));

                // Smoothness to 0
                if (!_zoomStarted)
                {
                    _zoomStarted = true;
                    _panTarget.position = ProCamera2D.LocalPosition;
                    UpdateCurrentFollowSmoothness();
                    RemoveFollowSmoothness();
                }

                // Save time
                _touchZoomTime = Time.time;
            }
            else
            {
                // Reset smoothness
                if (_zoomStarted && Mathf.Abs(_zoomAmount) < .001f)
                {
                    RestoreFollowSmoothness();
                    _zoomStarted = false;
                }
            }
            #endif

            #if UNITY_STANDALONE || UNITY_WEBGL || UNITY_WEBPLAYER || UNITY_EDITOR
            // Zoom input
            zoomInput = Input.GetAxis("Mouse ScrollWheel");

            // Zoom point
            _zoomPoint = VectorHVD(Input.mousePosition.x, Input.mousePosition.y, Mathf.Abs(ProCamera2D.CameraDepthPos));
            #endif

            // Different zoom speed according to the platform
            var zoomSpeed = 0f;
            #if !UNITY_EDITOR && (UNITY_IOS || UNITY_ANDROID || UNITY_WINRT)
            zoomSpeed = PinchZoomSpeed * 10f;
            #elif UNITY_STANDALONE || UNITY_WEBGL || UNITY_WEBPLAYER || UNITY_EDITOR
            zoomSpeed = MouseZoomSpeed;
            #endif

            // Zoom amount
            _zoomAmount = Mathf.SmoothDamp(_prevZoomAmount, zoomInput * zoomSpeed * deltaTime, ref _zoomVelocity, ZoomSmoothness);

            #if UNITY_STANDALONE || UNITY_WEBGL || UNITY_WEBPLAYER || UNITY_EDITOR
            // Reset smoothness once zoom stops
            if (Mathf.Abs(_zoomAmount) <= .0001f)
            {
                if (_zoomStarted)
                    RestoreFollowSmoothness();
                
                _zoomStarted = false;
                _prevZoomAmount = 0;
                return 0;
            }

            // Smoothness to 0
            if (!_zoomStarted)
            {
                _zoomStarted = true;
                _panTarget.position = ProCamera2D.LocalPosition;
                UpdateCurrentFollowSmoothness();
                RemoveFollowSmoothness();
            }
            #endif

            // Clamp zoom amount
            var targetSize = (ProCamera2D.ScreenSizeInWorldCoordinates.y / 2) + _zoomAmount;
            var minScreenSize = _initialCamSize / MaxZoomInAmount;
            var maxScreenSize = MaxZoomOutAmount * _initialCamSize;
            if (targetSize < minScreenSize)
                _zoomAmount -= targetSize - minScreenSize;
            else if (targetSize > maxScreenSize)
                _zoomAmount -= targetSize - maxScreenSize;

            _prevZoomAmount = _zoomAmount;

            // Move camera towards zoom point
            if (ZoomToInputCenter)
            {
                var multiplier = _zoomAmount / (ProCamera2D.ScreenSizeInWorldCoordinates.y / 2);
                _panTarget.position += ((_panTarget.position - ProCamera2D.GameCamera.ScreenToWorldPoint(_zoomPoint)) * multiplier);
            }

            // Zoom
            return _zoomAmount;
        }

        /// <summary>
        /// Call this method after manually updating the camera follow smoothness
        /// </summary>
        public void UpdateCurrentFollowSmoothness()
        {
            _origFollowSmoothnessX = ProCamera2D.HorizontalFollowSmoothness;
            _origFollowSmoothnessY = ProCamera2D.VerticalFollowSmoothness;
        }

        void CenterPanTargetOnCamera(float interpolant)
        {
            if (_panTarget != null)
                _panTarget.position = Vector3.Lerp(_panTarget.position, VectorHV(Vector3H(ProCamera2D.LocalPosition), Vector3V(ProCamera2D.LocalPosition)), interpolant);
        }

        void CancelZoom()
        {
            _zoomAmount = 0f;
            _prevZoomAmount = 0f;
            _zoomVelocity = 0f;
        }

        void RestoreFollowSmoothness()
        {
            ProCamera2D.HorizontalFollowSmoothness = _origFollowSmoothnessX;
            ProCamera2D.VerticalFollowSmoothness = _origFollowSmoothnessY;
        }

        void RemoveFollowSmoothness()
        {
            ProCamera2D.HorizontalFollowSmoothness = 0;
            ProCamera2D.VerticalFollowSmoothness = 0;
        }

        bool InsideDraggableArea(Vector2 normalizedInput)
        {
            if (DraggableAreaRect.x == 0 &&
                DraggableAreaRect.y == 0 &&
                DraggableAreaRect.width == 1 &&
                DraggableAreaRect.height == 1)
                return true;

            if (normalizedInput.x > DraggableAreaRect.x + (1 - DraggableAreaRect.width) / 2 &&
                normalizedInput.x < DraggableAreaRect.x + DraggableAreaRect.width + (1 - DraggableAreaRect.width) / 2 &&
                normalizedInput.y > DraggableAreaRect.y + (1 - DraggableAreaRect.height) / 2 &&
                normalizedInput.y < DraggableAreaRect.y + DraggableAreaRect.height + (1 - DraggableAreaRect.height) / 2)
                return true;
            
            return false;
        }

        #if UNITY_EDITOR
        protected override void DrawGizmos()
        {
            base.DrawGizmos();

            Gizmos.color = EditorPrefsX.GetColor(PrefsData.PanEdgesColorKey, PrefsData.PanEdgesColorValue);
            var gameCamera = ProCamera2D.GetComponent<Camera>();
            var cameraDimensions = gameCamera.orthographic ? Utils.GetScreenSizeInWorldCoords(gameCamera) : Utils.GetScreenSizeInWorldCoords(gameCamera, Mathf.Abs(Vector3D(transform.localPosition)));
            float cameraDepthOffset = Vector3D(ProCamera2D.transform.localPosition) + Mathf.Abs(Vector3D(transform.localPosition)) * Vector3D(ProCamera2D.transform.forward);

            if (UsePanByMoveToEdges)
            {
                Gizmos.DrawWireCube(
                    VectorHVD(ProCamera2D.transform.localPosition.x, ProCamera2D.transform.localPosition.y, cameraDepthOffset), 
                    VectorHV(cameraDimensions.x * HorizontalPanEdges, cameraDimensions.y * VerticalPanEdges));
            }

            if (UsePanByDrag)
            {
                if (DraggableAreaRect.x != 0 ||
                    DraggableAreaRect.y != 0 ||
                    DraggableAreaRect.width != 1 ||
                    DraggableAreaRect.height != 1)
                    Gizmos.DrawWireCube(
                        VectorHVD(ProCamera2D.transform.localPosition.x + DraggableAreaRect.x * cameraDimensions.x, ProCamera2D.transform.localPosition.y + DraggableAreaRect.y * cameraDimensions.y, cameraDepthOffset), 
                        VectorHV(DraggableAreaRect.width * cameraDimensions.x, DraggableAreaRect.height * cameraDimensions.y));
            }
        }
        #endif
    }
}