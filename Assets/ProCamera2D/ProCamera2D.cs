﻿using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.Linq;

namespace Com.LuisPedroFonseca.ProCamera2D
{
    /// <summary>
    /// Core class of the plugin. Everything starts and happens through here.
    /// All extensions and triggers have a reference to an instance of this class.
    /// </summary>
    [RequireComponent(typeof(Camera))]
    public class ProCamera2D : MonoBehaviour, ISerializationCallbackReceiver
    {
        public const string VERSION = "2.0.3";

        #region Inspector Variables

        public List<CameraTarget> CameraTargets = new List<CameraTarget>();

        public bool CenterTargetOnStart;

        public MovementAxis Axis;

        public UpdateType UpdateType;

        public bool FollowHorizontal = true;
        public float HorizontalFollowSmoothness = 0.15f;

        public bool FollowVertical = true;
        public float VerticalFollowSmoothness = 0.15f;

        public Vector2 OverallOffset;

        public bool ZoomWithFOV;

        #endregion


        #region Properties

        /// <summary>Get ProCamera2D's static instance</summary>
        public static ProCamera2D Instance
        {
            get
            {
                if (Equals(_instance, null))
                {
                    _instance = FindObjectOfType(typeof(ProCamera2D)) as ProCamera2D;

                    if (Equals(_instance, null))
                        throw new UnityException("ProCamera2D does not exist.");
                }

                return _instance;
            }
        }
        static ProCamera2D _instance;

        /// <summary>Update ProCamera2D's camera rect</summary>
        public Rect Rect
        {
            get
            {
                return GameCamera.rect;
            }

            set
            {
                GameCamera.rect = value;
                ProCamera2DParallax parallax = GetComponentInChildren<ProCamera2DParallax>();
                if (parallax != null)
                {
                    for (int i = 0; i < parallax.ParallaxLayers.Count; i++)
                    {
                        parallax.ParallaxLayers[i].ParallaxCamera.rect = value;
                    }
                }
            }
        }

        public Vector2 CameraTargetPositionSmoothed 
        { 
            get 
            { 
                return new Vector2(_cameraTargetHorizontalPositionSmoothed, _cameraTargetVerticalPositionSmoothed); 
            }

            set 
            { 
                _cameraTargetHorizontalPositionSmoothed = value.x;
                _cameraTargetVerticalPositionSmoothed = value.y;
            }
        }
        float _cameraTargetHorizontalPositionSmoothed;
        float _cameraTargetVerticalPositionSmoothed;

        public Vector3 LocalPosition { get { return _transform.localPosition; } set { _transform.localPosition = value; } }

        public Vector2 ScreenSizeInWorldCoordinates { get { return _screenSizeInWorldCoordinates; } }
        Vector2 _screenSizeInWorldCoordinates;

        public Vector3 PreviousTargetsMidPoint { get { return _previousTargetsMidPoint; } }
        Vector3 _previousTargetsMidPoint;

        public Vector3 TargetsMidPoint { get { return _targetsMidPoint; } }
        Vector3 _targetsMidPoint;

        public Vector3 CameraTargetPosition { get { return _cameraTargetPosition; } }
        Vector3 _cameraTargetPosition;

        public float CameraDepthPos { get { return _cameraDepthPos; } }
        float _cameraDepthPos;

        public float DeltaTime { get { return _deltaTime; } }
        float _deltaTime;

        public Vector3 ParentPosition { get { return _parentPosition; } }
        Vector3 _parentPosition;

        #endregion


        #region Public Variables

        public Action<float> PreMoveUpdate;
        public Action<float> PostMoveUpdate;
        
        public Action OnReset;

        public Vector3? ExclusiveTargetPosition;

        public int CurrentZoomTriggerID;

        public bool IsCameraPositionLeftBounded;
        public bool IsCameraPositionRightBounded;
        public bool IsCameraPositionTopBounded;
        public bool IsCameraPositionBottomBounded;

        public Camera GameCamera;

        public Rect screenRect;

        #if PC2D_TK2D_SUPPORT
        public tk2dCamera Tk2dCam;
        #endif

        #endregion


        #region Private Variables

        Func<Vector3, float> Vector3H;
        Func<Vector3, float> Vector3V;
        Func<Vector3, float> Vector3D;
        Func<float, float, Vector3> VectorHV;
        Func<float, float, float, Vector3> VectorHVD;

        Vector2 _startScreenSizeInWorldCoordinates;

        Coroutine _updateScreenSizeCoroutine;

        List<Vector3> _influences = new List<Vector3>();
        Vector3 _influencesSum = Vector3.zero;

        float _originalCameraDepthSign;

        float _previousCameraTargetHorizontalPositionSmoothed;
        float _previousCameraTargetVerticalPositionSmoothed;

        WaitForFixedUpdate _waitForFixedUpdate = new WaitForFixedUpdate();

        Transform _transform;

        List<IPreMover> _preMovers = new List<IPreMover>();
        List<IPositionDeltaChanger> _positionDeltaChangers = new List<IPositionDeltaChanger>();
        List<IPositionOverrider> _positionOverriders = new List<IPositionOverrider>();
        List<ISizeDeltaChanger> _sizeDeltaChangers = new List<ISizeDeltaChanger>();
        List<ISizeOverrider> _sizeOverriders = new List<ISizeOverrider>();
        List<IPostMover> _postMovers = new List<IPostMover>();

        #endregion

        #region MonoBehaviour

        void Awake()
        {
            _instance = this;
            _transform = transform;

            // Get parent position
            if(_transform.parent != null)
                _parentPosition = _transform.parent.position;

            if (GameCamera == null)
                GameCamera = GetComponent<Camera>();
            if (GameCamera == null)
                Debug.LogError("Unity Camera not set and not found on the GameObject: " + gameObject.name);

            #if PC2D_TK2D_SUPPORT
            Tk2dCam = GetComponent<tk2dCamera>();
            #endif

            // Reset the axis functions
            ResetAxisFunctions();

            // Remove empty targets
            for (int i = 0; i < CameraTargets.Count; i++)
            {
                if (CameraTargets[i].TargetTransform == null)
                {
                    CameraTargets.RemoveAt(i);
                }
            }

            _screenSizeInWorldCoordinates = _startScreenSizeInWorldCoordinates = Utils.GetScreenSizeInWorldCoords(GameCamera, Mathf.Abs(Vector3D(_transform.localPosition)));
            screenRect = new Rect(Vector2.zero, _screenSizeInWorldCoordinates);
            screenRect.center = transform.position;
            _cameraDepthPos = Vector3D(_transform.localPosition);
            _originalCameraDepthSign = Mathf.Sign(_cameraDepthPos);
        }

        void Start()
        {
            SortPreMovers();
            SortPositionDeltaChangers();
            SortPositionOverriders();
            SortSizeDeltaChangers();
            SortSizeOverriders();
            SortPostMovers();

            // Center on target
            if (CenterTargetOnStart && CameraTargets.Count > 0)
            {
                var targetsMidPoint = GetTargetsWeightedMidPoint(CameraTargets);
                var cameraTargetPositionX = FollowHorizontal ? Vector3H(targetsMidPoint) : Vector3H(_transform.localPosition);
                var cameraTargetPositionY = FollowVertical ? Vector3V(targetsMidPoint) : Vector3V(_transform.localPosition);
                var finalPos = new Vector2(cameraTargetPositionX, cameraTargetPositionY);
                finalPos += new Vector2(OverallOffset.x - Vector3H(_parentPosition), OverallOffset.y - Vector3V(_parentPosition));
                MoveCameraInstantlyToPosition(finalPos);
            }
            else
            {
                _cameraTargetPosition = _transform.position - _parentPosition;
                _cameraTargetHorizontalPositionSmoothed = Vector3H(_cameraTargetPosition);
                _previousCameraTargetHorizontalPositionSmoothed = _cameraTargetHorizontalPositionSmoothed;
                _cameraTargetVerticalPositionSmoothed = Vector3V(_cameraTargetPosition);
                _previousCameraTargetVerticalPositionSmoothed = _cameraTargetVerticalPositionSmoothed;
            }
        }

        void LateUpdate()
        {
            if (UpdateType == UpdateType.LateUpdate)
                Move(Time.deltaTime);
        }

        void FixedUpdate()
        {
            if (UpdateType == UpdateType.FixedUpdate)
                Move(Time.fixedDeltaTime);
        }

        void OnApplicationQuit()
        {
            _instance = null;
        }

        #endregion


        #region Public Methods

        /// <summary>Apply the given influence to the camera during this frame.</summary>
        /// <param name="influence">The vector representing the influence to be applied</param>
        public void ApplyInfluence(Vector2 influence)
        {
            if (Time.deltaTime < .0001f || float.IsNaN(influence.x) || float.IsNaN(influence.y))
                return;

            _influences.Add(VectorHV(influence.x, influence.y));
        }

        /// <summary>Apply the given influences to the camera during the corresponding durations.</summary>
        /// <param name="influences">An array of the vectors representing the influences to be applied</param>
        /// <param name="durations">An array with the durations of the influences to be applied</param>
        public Coroutine ApplyInfluencesTimed(Vector2[] influences, float[] durations)
        {
            return StartCoroutine(ApplyInfluencesTimedRoutine(influences, durations));
        }

        /// <summary>Add a target for the camera to follow.</summary>
        /// <param name="targetTransform">The Transform of the target</param>
        /// <param name="targetInfluenceH">The influence this target horizontal position should have when calculating the average position of all the targets</param>
        /// <param name="targetInfluenceV">The influence this target vertical position should have when calculating the average position of all the targets</param>
        /// <param name="duration">The time it takes for this target to reach it's influence. Use for a more progressive transition.</param>
        /// <param name="targetOffset">A vector that offsets the target position that the camera will follow</param>
        public CameraTarget AddCameraTarget(Transform targetTransform, float targetInfluenceH = 1f, float targetInfluenceV = 1f, float duration = 0f, Vector2 targetOffset = default(Vector2))
        {
            var newCameraTarget = new CameraTarget
            {
                TargetTransform = targetTransform,
                TargetInfluenceH = targetInfluenceH,
                TargetInfluenceV = targetInfluenceV,
                TargetOffset = targetOffset
            };

            CameraTargets.Add(newCameraTarget);

            if (duration > 0f)
            {
                newCameraTarget.TargetInfluence = 0f;
                StartCoroutine(AdjustTargetInfluenceRoutine(newCameraTarget, targetInfluenceH, targetInfluenceV, duration));
            }

            return newCameraTarget;
        }

        /// <summary>Add multiple targets for the camera to follow.</summary>
        /// <param name="targetsTransforms">An array or list with the new targets</param>
        /// <param name="targetsInfluenceH">The influence the targets horizontal position should have when calculating the average position of all the targets</param>
        /// <param name="targetsInfluenceV">The influence the targets vertical position should have when calculating the average position of all the targets</param>
        /// <param name="duration">The time it takes for the targets to reach their influence. Use for a more progressive transition.</param>
        /// <param name="targetOffset">A vector that offsets the target position that the camera will follow</param>
        public void AddCameraTargets(IList<Transform> targetsTransforms, float targetsInfluenceH = 1f, float targetsInfluenceV = 1f, float duration = 0f, Vector2 targetOffset = default(Vector2))
        {
            for (int i = 0; i < targetsTransforms.Count; i++)
            {
                AddCameraTarget(targetsTransforms[i], targetsInfluenceH, targetsInfluenceV, duration, targetOffset);
            }
        }

        /// <summary>Gets the corresponding CameraTarget from an object's transform.</summary>
        /// <param name="targetTransform">The Transform of the target</param>
        public CameraTarget GetCameraTarget(Transform targetTransform)
        {
            for (int i = 0; i < CameraTargets.Count; i++)
            {
                if (CameraTargets[i].TargetTransform.GetInstanceID() == targetTransform.GetInstanceID())
                {
                    return CameraTargets[i];
                }
            }
            return null;
        }

        /// <summary>Remove a target from the camera.</summary>
        /// <param name="targetTransform">The Transform of the target</param>
        /// <param name="duration">The time it takes for this target to reach a zero influence. Use for a more progressive transition.</param>
        public void RemoveCameraTarget(Transform targetTransform, float duration = 0f)
        {
            for (int i = 0; i < CameraTargets.Count; i++)
            {
                if (CameraTargets[i].TargetTransform.GetInstanceID() == targetTransform.GetInstanceID())
                {
                    if (duration > 0)
                    {
                        StartCoroutine(AdjustTargetInfluenceRoutine(CameraTargets[i], 0, 0, duration, true));
                    }
                    else
                        CameraTargets.Remove(CameraTargets[i]);
                }
            }
        }

        /// <summary>Removes all targets from the camera.</summary>
        /// <param name="duration">The time it takes for all targets to reach a zero influence. Use for a more progressive transition.</param>
        public void RemoveAllCameraTargets(float duration = 0f)
        {
            if (duration == 0)
            {
                CameraTargets.Clear();
            }
            else
            {
                for (int i = 0; i < CameraTargets.Count; i++)
                {
                    StartCoroutine(AdjustTargetInfluenceRoutine(CameraTargets[i], 0, 0, duration, true));
                }
            }
        }

        /// <summary>Adjusts a target influence</summary>
        /// <param name="cameraTarget">The CameraTarget of the target</param>
        /// <param name="targetInfluenceH">The influence this target horizontal position should have when calculating the average position of all the targets</param>
        /// <param name="targetInfluenceV">The influence this target vertical position should have when calculating the average position of all the targets</param>
        /// <param name="duration">The time it takes for this target to reach it's influence. Don't use a duration if calling every frame.</param>
        public Coroutine AdjustCameraTargetInfluence(CameraTarget cameraTarget, float targetInfluenceH, float targetInfluenceV, float duration = 0)
        {
            if (duration > 0)
                return StartCoroutine(AdjustTargetInfluenceRoutine(cameraTarget, targetInfluenceH, targetInfluenceV, duration));
            else
            {
                cameraTarget.TargetInfluenceH = targetInfluenceH;
                cameraTarget.TargetInfluenceV = targetInfluenceH;

                return null;
            }
        }

        /// <summary>Adjusts a target influence, finding it first by its transform.</summary>
        /// <param name="cameraTargetTransf">The Transform of the target</param>
        /// <param name="targetInfluenceH">The influence this target horizontal position should have when calculating the average position of all the targets</param>
        /// <param name="targetInfluenceV">The influence this target vertical position should have when calculating the average position of all the targets</param>
        /// <param name="duration">The time it takes for this target to reach it's influence. Don't use a duration if calling every frame.</param>
        public Coroutine AdjustCameraTargetInfluence(Transform cameraTargetTransf, float targetInfluenceH, float targetInfluenceV, float duration = 0)
        {
            var cameraTarget = GetCameraTarget(cameraTargetTransf);

            if (cameraTarget == null)
                return null;

            return AdjustCameraTargetInfluence(cameraTarget, targetInfluenceH, targetInfluenceV, duration);
        }

        /// <summary>Moves the camera instantly to the supplied position</summary>
        /// <param name="cameraPos">The final position of the camera</param>
        public void MoveCameraInstantlyToPosition(Vector2 cameraPos)
        {
            _transform.localPosition = VectorHVD(cameraPos.x, cameraPos.y, Vector3D(_transform.localPosition));

            ResetMovement();
        }

        /// <summary>Resets the camera movement and size and also all of its extensions to their start values.
        /// This could be useful if, for example, your player dies and respawns somewhere else on the level</summary>
        /// <param name="centerOnTargets">If true, the camera will move to the "final" targets position</param>
        public void Reset(bool centerOnTargets = true)
        {
            if (centerOnTargets)
            {
                var targetsMidPoint = GetTargetsWeightedMidPoint(CameraTargets);
                var finalPos = new Vector2(Vector3H(targetsMidPoint), Vector3V(targetsMidPoint));
                finalPos += new Vector2(OverallOffset.x, OverallOffset.y);
                MoveCameraInstantlyToPosition(finalPos);
            }
            else
                ResetMovement();

            ResetSize();

            if (OnReset != null)
                OnReset();
        }

        /// <summary>Resize the camera to the supplied size</summary>
        /// <param name="newSize">Half of the wanted size in world units</param>
        /// <param name="duration">How long it should take to reach the provided size. Use 0 if instant or calling repeatedly</param>
        /// <param name="easeType">The easing method to apply. Only used when the duration is bigger than 0</param>
        public void UpdateScreenSize(float newSize, float duration = 0, EaseType easeType = EaseType.Linear)
        {
            if (!enabled)
                return;

            if (_updateScreenSizeCoroutine != null)
                StopCoroutine(_updateScreenSizeCoroutine);

            if (duration > 0)
                _updateScreenSizeCoroutine = StartCoroutine(UpdateScreenSizeRoutine(newSize, duration, easeType));
            else
                SetScreenSize(newSize);
        }

        /// <summary>Zoom in or out the camera by the supplied amount</summary>
        /// <param name="zoomAmount">The amount to zoom</param>
        /// <param name="duration">How long it should take to reach the new zoom. Use 0 if instant or calling repeatedly</param>
        /// <param name="easeType">The easing method to apply. Only used when the duration is bigger than 0</param>
        public void Zoom(float zoomAmount, float duration = 0, EaseType easeType = EaseType.Linear)
        {
            UpdateScreenSize(_screenSizeInWorldCoordinates.y * .5f + zoomAmount, duration, easeType);
        }

        /// <summary>
        /// Move the camera to the average position of all the targets.
        /// This method is automatically called when using LateUpdate or FixedUpdate.
        /// If using ManualUpdate, you have to call it yourself.
        /// </summary>
        /// <param name="deltaTime">The time in seconds it took to complete the last frame</param>
        public void Move(float deltaTime)
        {
            // Delta time
            _deltaTime = deltaTime;
            if (_deltaTime < .0001f)
                return;
                
            // Pre-Move update
            if (PreMoveUpdate != null)
                PreMoveUpdate(_deltaTime);

            // Cycle through the pre movers
            for (int i = 0; i < _preMovers.Count; i++)
            {
                _preMovers[i].PreMove(deltaTime);
            }

            // Calculate targets mid point
            _previousTargetsMidPoint = _targetsMidPoint;
            _targetsMidPoint = GetTargetsWeightedMidPoint(CameraTargets);
            _cameraTargetPosition = _targetsMidPoint;

            // Calculate influences
            _influencesSum = Utils.GetVectorsSum(_influences);
            _cameraTargetPosition += _influencesSum;
            _influences.Clear();

            // Follow only on selected axis
            var cameraTargetPositionX = FollowHorizontal ? Vector3H(_cameraTargetPosition) : Vector3H(_transform.localPosition);
            var cameraTargetPositionY = FollowVertical ? Vector3V(_cameraTargetPosition) : Vector3V(_transform.localPosition);
            _cameraTargetPosition = VectorHV(cameraTargetPositionX - Vector3H(_parentPosition), cameraTargetPositionY- Vector3V(_parentPosition));

            // Ignore targets and influences if exclusive position is set
            if (ExclusiveTargetPosition.HasValue)
            {
                _cameraTargetPosition = VectorHV(Vector3H(ExclusiveTargetPosition.Value) - Vector3H(_parentPosition), Vector3V(ExclusiveTargetPosition.Value)- Vector3V(_parentPosition));
                ExclusiveTargetPosition = null;
            }

            // Add offset
            _cameraTargetPosition += VectorHV(FollowHorizontal ? OverallOffset.x : 0, FollowVertical ? OverallOffset.y : 0);

            // Tween camera final position
            _cameraTargetHorizontalPositionSmoothed = Utils.SmoothApproach(_cameraTargetHorizontalPositionSmoothed, _previousCameraTargetHorizontalPositionSmoothed, Vector3H(_cameraTargetPosition), 1f / HorizontalFollowSmoothness, _deltaTime);
            _previousCameraTargetHorizontalPositionSmoothed = _cameraTargetHorizontalPositionSmoothed;

            _cameraTargetVerticalPositionSmoothed = Utils.SmoothApproach(_cameraTargetVerticalPositionSmoothed, _previousCameraTargetVerticalPositionSmoothed, Vector3V(_cameraTargetPosition), 1f / VerticalFollowSmoothness, _deltaTime);
            _previousCameraTargetVerticalPositionSmoothed = _cameraTargetVerticalPositionSmoothed;

            // Movement this step
            var horizontalDeltaMovement = _cameraTargetHorizontalPositionSmoothed - Vector3H(_transform.localPosition);
            var verticalDeltaMovement = _cameraTargetVerticalPositionSmoothed - Vector3V(_transform.localPosition);

            // Calculate the base delta movement
            var deltaMovement = VectorHV(horizontalDeltaMovement, verticalDeltaMovement);

            // Cycle through the position delta changers
            for (int i = 0; i < _positionDeltaChangers.Count; i++)
            {
                deltaMovement = _positionDeltaChangers[i].AdjustDelta(deltaTime, deltaMovement);
            }

            // Calculate the new position
            var newPos = LocalPosition + deltaMovement;

            // Cycle through the position overriders
            for (int i = 0; i < _positionOverriders.Count; i++)
            {
                newPos = _positionOverriders[i].OverridePosition(deltaTime, newPos);
            }

            // Apply the new position
            _transform.localPosition = newPos;

            // Cycle through the size delta changers
            var deltaSize = 0f;
            for (int i = 0; i < _sizeDeltaChangers.Count; i++)
            {
                deltaSize = _sizeDeltaChangers[i].AdjustSize(deltaTime, deltaSize);
            }

            // Calculate the new size
            var newSize = _screenSizeInWorldCoordinates.y * .5f + deltaSize;

            // Cycle through the size overriders
            for (int i = 0; i < _sizeOverriders.Count; i++)
            {
                newSize = _sizeOverriders[i].OverrideSize(deltaTime, newSize);
            }

            // Apply the new size
            if(newSize != _screenSizeInWorldCoordinates.y * .5f)
                SetScreenSize(newSize);

            // Cycle through the post movers
            for (int i = 0; i < _postMovers.Count; i++)
            {
                _postMovers[i].PostMove(deltaTime);
            }
            
            // Post-Move update
            if (PostMoveUpdate != null)
                PostMoveUpdate(_deltaTime);

            //Act screen rect
            screenRect.center = transform.position;
        }

        /// <summary>For internal use</summary>
        public YieldInstruction GetYield()
        {
            switch (UpdateType)
            {
                case UpdateType.FixedUpdate:
                    return _waitForFixedUpdate;

                default:
                    return null;
            }
        }

        #endregion


        #region Private Methods

        void ResetAxisFunctions()
        {
            switch (Axis)
            {
                case MovementAxis.XY:
                    Vector3H = vector => vector.x;
                    Vector3V = vector => vector.y;
                    Vector3D = vector => vector.z;
                    VectorHV = (h, v) => new Vector3(h, v, 0);
                    VectorHVD = (h, v, d) => new Vector3(h, v, d);
                    break;
                case MovementAxis.XZ:
                    Vector3H = vector => vector.x;
                    Vector3V = vector => vector.z;
                    Vector3D = vector => vector.y;
                    VectorHV = (h, v) => new Vector3(h, 0, v);
                    VectorHVD = (h, v, d) => new Vector3(h, d, v);
                    break;
                case MovementAxis.YZ:
                    Vector3H = vector => vector.z;
                    Vector3V = vector => vector.y;
                    Vector3D = vector => vector.x;
                    VectorHV = (h, v) => new Vector3(0, v, h);
                    VectorHVD = (h, v, d) => new Vector3(d, v, h);
                    break;
            } 
        }

        Vector3 GetTargetsWeightedMidPoint(IList<CameraTarget> targets)
        {
            var midPointH = 0f;
            var midPointV = 0f;
            var totalTargets = targets.Count;

            if (totalTargets == 0)
                return transform.localPosition;

            var totalInfluencesH = 0f;
            var totalInfluencesV = 0f;
            var totalAccountableTargetsH = 0;
            var totalAccountableTargetsV = 0;
            for (int i = 0; i < totalTargets; i++)
            {
                if (targets[i] == null)
                    continue;

                midPointH += (Vector3H(targets[i].TargetPosition) + targets[i].TargetOffset.x) * targets[i].TargetInfluenceH;
                midPointV += (Vector3V(targets[i].TargetPosition) + targets[i].TargetOffset.y) * targets[i].TargetInfluenceV;

                totalInfluencesH += targets[i].TargetInfluenceH;
                totalInfluencesV += targets[i].TargetInfluenceV;

                if (targets[i].TargetInfluenceH > 0)
                    totalAccountableTargetsH++;

                if (targets[i].TargetInfluenceV > 0)
                    totalAccountableTargetsV++;
            }

            if (totalInfluencesH < 1 && totalAccountableTargetsH == 1)
                totalInfluencesH += (1 - totalInfluencesH);

            if (totalInfluencesV < 1 && totalAccountableTargetsV == 1)
                totalInfluencesV += (1 - totalInfluencesV);

            if (totalInfluencesH > .0001f)
                midPointH /= totalInfluencesH;

            if (totalInfluencesV > .0001f)
                midPointV /= totalInfluencesV;

            return VectorHV(midPointH, midPointV);
        }

        IEnumerator ApplyInfluencesTimedRoutine(IList<Vector2> influences, float[] durations)
        {
            var count = -1;
            while (count < durations.Length - 1)
            {
                count++;
                var duration = durations[count];

                yield return StartCoroutine(ApplyInfluenceTimedRoutine(influences[count], duration));
            }
        }

        IEnumerator ApplyInfluenceTimedRoutine(Vector2 influence, float duration)
        {
            while (duration > 0)
            {
                duration -= _deltaTime;

                ApplyInfluence(influence);

                yield return GetYield();
            }
        }

        IEnumerator AdjustTargetInfluenceRoutine(CameraTarget cameraTarget, float influenceH, float influenceV, float duration, bool removeIfZeroInfluence = false)
        {
            var startInfluenceH = cameraTarget.TargetInfluenceH;
            var startInfluenceV = cameraTarget.TargetInfluenceV;

            var t = 0f;
            while (t <= 1.0f)
            {
                t += _deltaTime / duration;
                cameraTarget.TargetInfluenceH = Utils.EaseFromTo(startInfluenceH, influenceH, t, EaseType.Linear);
                cameraTarget.TargetInfluenceV = Utils.EaseFromTo(startInfluenceV, influenceV, t, EaseType.Linear);

                yield return GetYield();
            }

            if (removeIfZeroInfluence && cameraTarget.TargetInfluenceH <= 0 && cameraTarget.TargetInfluenceV <= 0)
                CameraTargets.Remove(cameraTarget);
        }

        IEnumerator UpdateScreenSizeRoutine(float finalSize, float duration, EaseType easeType)
        {
            var startSize = _screenSizeInWorldCoordinates.y * .5f;
            var newSize = startSize;

            var t = 0f;
            while (t <= 1.0f)
            {
                t += _deltaTime / duration;
                newSize = Utils.EaseFromTo(startSize, finalSize, t, easeType);

                SetScreenSize(newSize);

                yield return GetYield();
            }

            _updateScreenSizeCoroutine = null;
        }

        void SetScreenSize(float newSize)
        {
            #if UNITY_EDITOR
            if(_transform == null)
                _transform = transform;
            #endif

            if (GameCamera.orthographic)
            {
                newSize = Mathf.Max(newSize, .1f);

                GameCamera.orthographicSize = newSize;
                _cameraDepthPos = Vector3D(_transform.localPosition);
            }
            else
            {
                if (ZoomWithFOV)
                {
                    var newFieldOfView = 2f * Mathf.Atan(newSize / Mathf.Abs(Vector3D(_transform.localPosition))) * Mathf.Rad2Deg;
                    GameCamera.fieldOfView = Mathf.Clamp(newFieldOfView, .1f, 179.9f);
                }
                else
                {
                    _cameraDepthPos = (newSize / Mathf.Tan(GameCamera.fieldOfView * 0.5f * Mathf.Deg2Rad)) * _originalCameraDepthSign;
                    _transform.localPosition = VectorHVD(Vector3H(_transform.localPosition), Vector3V(_transform.localPosition), _cameraDepthPos);
                }
            }

            _screenSizeInWorldCoordinates = new Vector2(newSize * 2f * GameCamera.aspect, newSize * 2f);

            #if PC2D_TK2D_SUPPORT
            if (Tk2dCam == null)
                return;

            if (Tk2dCam.CameraSettings.projection == tk2dCameraSettings.ProjectionType.Orthographic)
            {
                if(Tk2dCam.CameraSettings.orthographicType == tk2dCameraSettings.OrthographicType.OrthographicSize)
                    Tk2dCam.ZoomFactor = Tk2dCam.CameraSettings.orthographicSize / newSize;
                else
                {
                    #if UNITY_EDITOR
                    if(Application.isPlaying)
                    #endif
                        Tk2dCam.ZoomFactor = (_startScreenSizeInWorldCoordinates.y * .5f) / newSize;
                }
            }
            #endif
        }

        void ResetMovement()
        {
            _cameraTargetPosition = _transform.localPosition;

            _cameraTargetHorizontalPositionSmoothed = Vector3H(_cameraTargetPosition);
            _cameraTargetVerticalPositionSmoothed = Vector3V(_cameraTargetPosition);

            _previousCameraTargetHorizontalPositionSmoothed = _cameraTargetHorizontalPositionSmoothed;
            _previousCameraTargetVerticalPositionSmoothed = _cameraTargetVerticalPositionSmoothed;
        }

        void ResetSize()
        {
            SetScreenSize(_startScreenSizeInWorldCoordinates.y / 2);
        }

        public void AddPreMover(IPreMover mover)
        {
            _preMovers.Add(mover);
        }

        public void RemovePreMover(IPreMover mover)
        {
            _preMovers.Remove(mover);
        }

        public void SortPreMovers()
        {
            _preMovers = _preMovers.OrderBy(a => a.PrMOrder).ToList();
        }

        public void AddPositionDeltaChanger(IPositionDeltaChanger changer)
        {
            _positionDeltaChangers.Add(changer);
        }

        public void RemovePositionDeltaChanger(IPositionDeltaChanger changer)
        {
            _positionDeltaChangers.Remove(changer);
        }

        public void SortPositionDeltaChangers()
        {
            _positionDeltaChangers = _positionDeltaChangers.OrderBy(a => a.PDCOrder).ToList();
        }

        public void AddPositionOverrider(IPositionOverrider overrider)
        {
            _positionOverriders.Add(overrider);
        }

        public void RemovePositionOverrider(IPositionOverrider overrider)
        {
            _positionOverriders.Remove(overrider);
        }

        public void SortPositionOverriders()
        {
            _positionOverriders = _positionOverriders.OrderBy(a => a.POOrder).ToList();
        }

        public void AddSizeDeltaChanger(ISizeDeltaChanger changer)
        {
            _sizeDeltaChangers.Add(changer);
        }

        public void RemoveSizeDeltaChanger(ISizeDeltaChanger changer)
        {
            _sizeDeltaChangers.Remove(changer);
        }

        public void SortSizeDeltaChangers()
        {
            _sizeDeltaChangers = _sizeDeltaChangers.OrderBy(a => a.SDCOrder).ToList();
        }

        public void AddSizeOverrider(ISizeOverrider overrider)
        {
            _sizeOverriders.Add(overrider);
        }

        public void RemoveSizeOverrider(ISizeOverrider overrider)
        {
            _sizeOverriders.Remove(overrider);
        }

        public void SortSizeOverriders()
        {
            _sizeOverriders = _sizeOverriders.OrderBy(a => a.SOOrder).ToList();
        }

        public void AddPostMover(IPostMover mover)
        {
            _postMovers.Add(mover);
        }

        public void RemovePostMover(IPostMover mover)
        {
            _postMovers.Remove(mover);
        }

        public void SortPostMovers()
        {
            _postMovers = _postMovers.OrderBy(a => a.PMOrder).ToList();
        }

        #endregion


        #region ISerializationCallbackReceiver implementation

        public void OnBeforeSerialize()
        {
        }

        public void OnAfterDeserialize()
        {
            ResetAxisFunctions();
        }

        #endregion

        #if UNITY_EDITOR
        int _drawGizmosCounter;

        void OnDrawGizmos()
        {
            // HACK to prevent Unity bug on startup: http://forum.unity3d.com/threads/screen-position-out-of-view-frustum.9918/
            _drawGizmosCounter++;
            if (_drawGizmosCounter < 5 && UnityEditor.EditorApplication.timeSinceStartup < 60f)
                return;

            if (Vector3H == null)
                ResetAxisFunctions();

            var gameCamera = GetComponent<Camera>();

            // Don't draw gizmos on other cameras
            if (Camera.current != gameCamera &&
                ((UnityEditor.SceneView.lastActiveSceneView != null && Camera.current != UnityEditor.SceneView.lastActiveSceneView.camera) ||
                (UnityEditor.SceneView.lastActiveSceneView == null)))
                return;

            var cameraDimensions = Utils.GetScreenSizeInWorldCoords(gameCamera, Mathf.Abs(Vector3D(transform.position)));
            float cameraDepthOffset = Vector3D(transform.position) + Mathf.Abs(Vector3D(transform.position)) * Vector3D(transform.forward);

            // Targets mid point
            Gizmos.color = EditorPrefsX.GetColor(PrefsData.TargetsMidPointColorKey, PrefsData.TargetsMidPointColorValue);
            var targetsMidPoint = GetTargetsWeightedMidPoint(CameraTargets);
            targetsMidPoint = VectorHVD(Vector3H(targetsMidPoint), Vector3V(targetsMidPoint), cameraDepthOffset);
            Gizmos.DrawWireSphere(targetsMidPoint, .01f * cameraDimensions.y);

            // Influences sum
            Gizmos.color = EditorPrefsX.GetColor(PrefsData.InfluencesColorKey, PrefsData.InfluencesColorValue);
            if (_influencesSum != Vector3.zero)
                Utils.DrawArrowForGizmo(targetsMidPoint, _influencesSum, .04f * cameraDimensions.y);

            // Overall offset line
            Gizmos.color = EditorPrefsX.GetColor(PrefsData.OverallOffsetColorKey, PrefsData.OverallOffsetColorValue);
            if (OverallOffset != Vector2.zero)
                Utils.DrawArrowForGizmo(targetsMidPoint, VectorHV(OverallOffset.x, OverallOffset.y), .04f * cameraDimensions.y);

            // Camera target position
            Gizmos.color = EditorPrefsX.GetColor(PrefsData.CamTargetPositionColorKey, PrefsData.CamTargetPositionColorValue);
            var cameraTargetPosition = targetsMidPoint + _influencesSum + VectorHV(OverallOffset.x, OverallOffset.y);
            var cameraTargetPos = VectorHVD(Vector3H(cameraTargetPosition), Vector3V(cameraTargetPosition), cameraDepthOffset);
            Gizmos.DrawWireSphere(cameraTargetPos, .015f * cameraDimensions.y);

            // Camera target position smoothed
            if (Application.isPlaying)
            {
                Gizmos.color = EditorPrefsX.GetColor(PrefsData.CamTargetPositionSmoothedColorKey, PrefsData.CamTargetPositionSmoothedColorValue);
                var cameraTargetPosSmoothed = VectorHVD(_cameraTargetHorizontalPositionSmoothed + Vector3H(_parentPosition), _cameraTargetVerticalPositionSmoothed + Vector3V(_parentPosition), cameraDepthOffset);
                Gizmos.DrawWireSphere(cameraTargetPosSmoothed, .02f * cameraDimensions.y);
                Gizmos.DrawLine(cameraTargetPos, cameraTargetPosSmoothed);
            }

            // Current camera position
            Gizmos.color = EditorPrefsX.GetColor(PrefsData.CurrentCameraPositionColorKey, PrefsData.CurrentCameraPositionColorValue);
            var currentCameraPos = VectorHVD(Vector3H(transform.position), Vector3V(transform.position), cameraDepthOffset);
            Gizmos.DrawWireSphere(currentCameraPos, .025f * cameraDimensions.y);
        }
        #endif
    }
}