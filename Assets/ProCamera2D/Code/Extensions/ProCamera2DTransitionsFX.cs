using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using System;

namespace Com.LuisPedroFonseca.ProCamera2D
{
    public enum TransitionsFXShaders
    {
        Fade,
        Circle,
        Shutters,
        Wipe,
        Blinds
    }

    public enum TransitionFXSide
    {
        Left = 0,
        Right = 1,
        Up = 2,
        Down = 3
    }

    public enum TransitionFXDirection
    {
        Horizontal = 0,
        Vertical = 1
    }

    public class ProCamera2DTransitionsFX : BasePC2D
    {
        public static string ExtensionName = "TransitionsFX";

        /// <summary>Fires whenever a TransitionEnter starts</summary>
        public Action OnTransitionEnterStarted;
        /// <summary>Fires whenever a TransitionEnter ends</summary>
        public Action OnTransitionEnterEnded;

        /// <summary>Fires whenever a TransitionExit starts</summary>
        public Action OnTransitionExitStarted;
        /// <summary>Fires whenever a TransitionExit ends</summary>
        public Action OnTransitionExitEnded;

        /// <summary>Fires whenever a TransitionEnter or a TransitionExit starts</summary>
        public Action OnTransitionStarted;
        /// <summary>Fires whenever a TransitionEnter or a TransitionExit ends</summary>
        public Action OnTransitionEnded;

        static ProCamera2DTransitionsFX _instance;

        public static ProCamera2DTransitionsFX Instance
        {
            get
            {
                if (Equals(_instance, null))
                {
                    _instance = ProCamera2D.Instance.GetComponent<ProCamera2DTransitionsFX>();

                    if (Equals(_instance, null))
                        throw new UnityException("ProCamera2D does not have a TransitionFX extension.");
                }

                return _instance;
            }
        }

        public TransitionsFXShaders TransitionShaderEnter = TransitionsFXShaders.Fade;
        public float DurationEnter = .5f;
        public float DelayEnter = 0f;
        public EaseType EaseTypeEnter = EaseType.EaseOut;
        public Color BackgroundColorEnter = Color.black;
        public TransitionFXSide SideEnter = TransitionFXSide.Left;
        public TransitionFXDirection DirectionEnter = TransitionFXDirection.Horizontal;
        [Range(2, 128)]
        public int BlindsEnter = 16;

        public TransitionsFXShaders TransitionShaderExit = TransitionsFXShaders.Fade;
        public float DurationExit = .5f;
        public float DelayExit = 0f;
        public EaseType EaseTypeExit = EaseType.EaseOut;
        public Color BackgroundColorExit = Color.black;
        public TransitionFXSide SideExit = TransitionFXSide.Left;
        public TransitionFXDirection DirectionExit = TransitionFXDirection.Horizontal;
        [Range(2, 128)]
        public int BlindsExit = 16;

        public bool StartSceneOnEnterState = true;

        Coroutine _transitionCoroutine;
        float _step;

        Material _transitionEnterMaterial;
        Material _transitionExitMaterial;
        Material _currentMaterial;

        protected override void Awake()
        {
            base.Awake();

            CreateMaterials();

            if (StartSceneOnEnterState)
            {
                _step = 1f;
                _currentMaterial = _transitionEnterMaterial;
                _currentMaterial.SetFloat("_Step", _step);
                TransitionEnter();
            }
        }

        void OnRenderImage(RenderTexture sourceRenderTexture, RenderTexture destinationRenderTexture)
        {
            sourceRenderTexture.wrapMode = TextureWrapMode.Repeat;

            if (_currentMaterial != null)
                Graphics.Blit(sourceRenderTexture, destinationRenderTexture, _currentMaterial);
            else
                Graphics.Blit(sourceRenderTexture, destinationRenderTexture);
        }

        /// <summary>
        /// Transition enter
        /// </summary>
        public void TransitionEnter()
        {
            if (_transitionCoroutine != null)
                StopCoroutine(_transitionCoroutine);

            _transitionCoroutine = StartCoroutine(TransitionRoutine(_transitionEnterMaterial, DurationEnter, DelayEnter, 1f, 0f, EaseTypeEnter));
        }

        /// <summary>
        /// Transition exit
        /// </summary>
        public void TransitionExit()
        {
            if (_transitionCoroutine != null)
                StopCoroutine(_transitionCoroutine);

            _transitionCoroutine = StartCoroutine(TransitionRoutine(_transitionExitMaterial, DurationExit, DelayExit, 0f, 1f, EaseTypeExit));
        }

        /// <summary>
        /// Internal use. Use only if you change the effects during runtime.
        /// </summary>
        public void CreateMaterials()
        {
            // Enter
            _transitionEnterMaterial = new Material(Shader.Find("Hidden/ProCamera2D/TransitionsFX/" + TransitionShaderEnter.ToString()));
            _transitionEnterMaterial.SetColor("_BackgroundColor", BackgroundColorEnter);

            if (TransitionShaderEnter == TransitionsFXShaders.Wipe || TransitionShaderEnter == TransitionsFXShaders.Blinds)
                _transitionEnterMaterial.SetInt("_Direction", (int)SideEnter);
            else if (TransitionShaderEnter == TransitionsFXShaders.Shutters)
                _transitionEnterMaterial.SetInt("_Direction", (int)DirectionEnter);

            _transitionEnterMaterial.SetInt("_Blinds", BlindsEnter);

            // Exit
            _transitionExitMaterial = new Material(Shader.Find("Hidden/ProCamera2D/TransitionsFX/" + TransitionShaderExit.ToString()));
            _transitionExitMaterial.SetColor("_BackgroundColor", BackgroundColorExit);

            if (TransitionShaderExit == TransitionsFXShaders.Wipe || TransitionShaderExit == TransitionsFXShaders.Blinds)
                _transitionExitMaterial.SetInt("_Direction", (int)SideExit);
            else if (TransitionShaderExit == TransitionsFXShaders.Shutters)
                _transitionExitMaterial.SetInt("_Direction", (int)DirectionExit);

            _transitionExitMaterial.SetInt("_Blinds", BlindsExit);
        }

        /// <summary>
        /// Clears the current transition
        /// </summary>
        public void Clear()
        {
            _currentMaterial = null;
        }

        IEnumerator TransitionRoutine(Material material, float duration, float delay, float startValue, float endValue, EaseType easeType)
        {
            _step = startValue;
            _currentMaterial = material;
            _currentMaterial.SetFloat("_Step", _step);

            if (endValue == 0)
            {
                if (OnTransitionEnterStarted != null)
                    OnTransitionEnterStarted();
            }
            else if (endValue == 1)
            {
                if (OnTransitionExitStarted != null)
                    OnTransitionExitStarted();
            }

            if (OnTransitionStarted != null)
                OnTransitionStarted();

            if (delay > 0)
                yield return new WaitForSeconds(delay);

            var t = 0f;
            while (t <= 1.0f)
            {
                t += ProCamera2D.DeltaTime / duration;

                _step = Utils.EaseFromTo(startValue, endValue, t, easeType);

                material.SetFloat("_Step", _step);

                yield return null;
            }

            _step = endValue;
            material.SetFloat("_Step", _step);

            if (endValue == 0)
            {
                if (OnTransitionEnterEnded != null)
                    OnTransitionEnterEnded();
            }
            else if (endValue == 1)
            {
                if (OnTransitionExitEnded != null)
                    OnTransitionExitEnded();
            }

            if (OnTransitionEnded != null)
                OnTransitionEnded();

            if (endValue == 0)
                _currentMaterial = null;

            _transitionCoroutine = null;
        }
    }
}