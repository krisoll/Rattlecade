using UnityEngine;

namespace Com.LuisPedroFonseca.ProCamera2D
{
    public class ProCamera2DSpeedBasedZoom : BasePC2D, ISizeDeltaChanger
    {
        public static string ExtensionName = "Speed Based Zoom";

        [Tooltip("The speed at which the camera will reach it's max zoom out.")]
        public float CamVelocityForZoomOut = 5f;
        [Tooltip("Below this speed the camera zooms in. Above this speed the camera will start zooming out.")]
        public float CamVelocityForZoomIn = 2f;

        [Tooltip("Represents how smooth the zoom in of the camera should be. The lower the number the quickest the zoom is. A number too low might cause some stuttering.")]
        public float ZoomInSpeed = 1f;
        [Tooltip("Represents how smooth the zoom out of the camera should be. The lower the number the quickest the zoom is. A number too low might cause some stuttering.")]
        public float ZoomOutSpeed = 1f;

        [Tooltip("Represents how smooth the zoom in of the camera should be. The lower the number the quickest the zoom is.")]
        [Range(0f, 3f)]
        public float ZoomInSmoothness = 1f;
        [Tooltip("Represents how smooth the zoom out of the camera should be. The lower the number the quickest the zoom is.")]
        [Range(0f, 3f)]
        public float ZoomOutSmoothness = 1f;

        [Tooltip("Represents the maximum amount the camera should zoom in when the camera speed is below SpeedForZoomIn")]
        public float MaxZoomInAmount = 2f;
        [Tooltip("Represents the maximum amount the camera should zoom out when the camera speed is equal to SpeedForZoomOut")]
        public float MaxZoomOutAmount = 2f;

        float _zoomVelocity;

        float _initialCamSize;
        float _previousCamSize;

        Vector3 _previousCameraPosition;

        float _prevZoomAmount;

        [HideInInspector]
        public float CurrentVelocity;

        override protected void Awake()
        {
            base.Awake();

            if (ProCamera2D == null)
                return;

            _initialCamSize = ProCamera2D.ScreenSizeInWorldCoordinates.y * .5f;
            _previousCamSize = _initialCamSize;

            _previousCameraPosition = VectorHV(Vector3H(ProCamera2D.LocalPosition), Vector3V(ProCamera2D.LocalPosition));

            _prevZoomAmount = 0;

            ProCamera2D.AddSizeDeltaChanger(this);
        }

        #region ISizeDeltaChanger implementation

        public float AdjustSize(float deltaTime, float originalDelta)
        {
            if (!enabled)
                return originalDelta;

            var newZoomAmount = 0f;

            // If the camera is bounded, reset the easing
            if (_previousCamSize == ProCamera2D.ScreenSizeInWorldCoordinates.y)
            {
                _prevZoomAmount = 0f;
                _zoomVelocity = 0f;
            }

            // Get camera velocity
            CurrentVelocity = (_previousCameraPosition - VectorHV(Vector3H(ProCamera2D.LocalPosition), Vector3V(ProCamera2D.LocalPosition))).magnitude / deltaTime;
            _previousCameraPosition = VectorHV(Vector3H(ProCamera2D.LocalPosition), Vector3V(ProCamera2D.LocalPosition));

            // Zoom out
            if (CurrentVelocity > CamVelocityForZoomIn)
            {
                var speedPercentage = (CurrentVelocity - CamVelocityForZoomIn) / (CamVelocityForZoomOut - CamVelocityForZoomIn);

                newZoomAmount = ZoomOutSpeed * Mathf.Clamp01(speedPercentage);
            }
            // Zoom in
            else
            {
                var speedPercentage = (1 - (CurrentVelocity / CamVelocityForZoomIn));

                newZoomAmount = -ZoomInSpeed * Mathf.Clamp01(speedPercentage);
            }

            // Smooth
            var zoomAmount = Mathf.SmoothDamp(_prevZoomAmount, newZoomAmount * deltaTime, ref _zoomVelocity, CurrentVelocity > CamVelocityForZoomIn ? ZoomOutSmoothness : ZoomInSmoothness);

            // Clamp zoom amount
            var targetSize = (ProCamera2D.ScreenSizeInWorldCoordinates.y / 2) + zoomAmount;
            var minScreenSize = _initialCamSize / MaxZoomInAmount;
            var maxScreenSize = MaxZoomOutAmount * _initialCamSize;
            if (targetSize < minScreenSize)
                zoomAmount -= targetSize - minScreenSize;
            else if (targetSize > maxScreenSize)
                zoomAmount -= targetSize - maxScreenSize;

            // Save the previous zoom amount for easing purposes
            _prevZoomAmount = zoomAmount;

            // Detect if the camera size is bounded
            _previousCamSize = ProCamera2D.ScreenSizeInWorldCoordinates.y;

            // Return the zoom delta
            return originalDelta + zoomAmount;
        }

        public int SDCOrder { get { return _sdcOrder; } set { _sdcOrder = value; } }

        int _sdcOrder = 1000;

        #endregion

        override public void OnReset()
        {
            _previousCamSize = _initialCamSize;
            _previousCameraPosition = VectorHV(Vector3H(ProCamera2D.LocalPosition), Vector3V(ProCamera2D.LocalPosition));
            _prevZoomAmount = 0;
            _zoomVelocity = 0;
        }

        //        void PreMoveUpdate(float deltaTime)
        //        {
        //            _targetCamSizeSmoothed = ProCamera2D.ScreenSizeInWorldCoordinates.y * .5f;
        //
        //            // If the camera is bounded, reset the easing
        //            if (_previousCamSize == ProCamera2D.ScreenSizeInWorldCoordinates.y)
        //            {
        //                _targetCamSize = ProCamera2D.ScreenSizeInWorldCoordinates.y * .5f;
        //                _targetCamSizeSmoothed = _targetCamSize;
        //                _zoomVelocity = 0f;
        //            }
        //
        //            // Get camera velocity
        //            CurrentVelocity = (_previousCameraPosition - ProCamera2D.LocalPosition).magnitude / deltaTime;
        //            _previousCameraPosition = ProCamera2D.LocalPosition;
        //
        //            // Zoom out
        //            if (CurrentVelocity > CamSpeedForZoomIn)
        //            {
        //                var speedPercentage = (CurrentVelocity - CamSpeedForZoomIn) / (CamSpeedForZoomOut - CamSpeedForZoomIn);
        //                var newSize = _initialCamSize * (1 + (MaxZoomOutAmount - 1) * Mathf.Clamp01(speedPercentage));
        //
        //                if (newSize > _targetCamSizeSmoothed)
        //                    _targetCamSize = newSize;
        //            }
        //            // Zoom in
        //            else
        //            {
        //                var speedPercentage = (1 - (CurrentVelocity / CamSpeedForZoomIn)).Remap(0f, 1f, .5f, 1f);
        //                var newSize = _initialCamSize / (MaxZoomInAmount * speedPercentage);
        //
        //                if (newSize < _targetCamSizeSmoothed)
        //                    _targetCamSize = newSize;
        //            }
        //
        //            // Detect if the camera size is bounded
        //            _previousCamSize = ProCamera2D.ScreenSizeInWorldCoordinates.y;
        //
        //            // Update camera size if needed
        //            _targetCamSizeSmoothed = Mathf.SmoothDamp(_targetCamSizeSmoothed, _targetCamSize, ref _zoomVelocity, _targetCamSize < _targetCamSizeSmoothed ? ZoomInSmoothness : ZoomOutSmoothness);
        //        }
    }
}