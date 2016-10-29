using System;
using UnityEngine;
using UnityEngine.EventSystems;
using System.Collections.Generic;

namespace Rewired.Integration.UnityUI {

    [AddComponentMenu("Event/Rewired Standalone Input Module")]
    public class RewiredStandaloneInputModule : PointerInputModule {
        
        #region Rewired Variables

        [SerializeField]
        private bool useAllRewiredGamePlayers = false;
        [SerializeField]
        private bool useRewiredSystemPlayer = false;
        [SerializeField]
        private int[] rewiredPlayerIds = new int[1] { 0 };
        [SerializeField]
        private bool moveOneElementPerAxisPress;

        private Rewired.Player[] players;
        private Rewired.Mouse mouse;

        private bool recompiling;

        #endregion
        
        private float m_NextAction;

        private Vector2 m_LastMousePosition;
        private Vector2 m_MousePosition;

        private bool isTouchSupported;

        protected RewiredStandaloneInputModule() { }

        [SerializeField]
        private string m_HorizontalAxis = "Horizontal";

        /// <summary>
        /// Name of the vertical axis for movement (if axis events are used).
        /// </summary>
        [SerializeField]
        private string m_VerticalAxis = "Vertical";

        /// <summary>
        /// Name of the submit button.
        /// </summary>
        [SerializeField]
        private string m_SubmitButton = "Submit";

        /// <summary>
        /// Name of the submit button.
        /// </summary>
        [SerializeField]
        private string m_CancelButton = "Cancel";

        [SerializeField]
        private float m_InputActionsPerSecond = 10;

        [SerializeField]
        private bool m_AllowActivationOnMobileDevice;

        [SerializeField]
        private bool m_allowMouseInput = true;
        [SerializeField]
        private bool m_allowMouseInputIfTouchSupported = false;

        public bool allowActivationOnMobileDevice {
            get { return m_AllowActivationOnMobileDevice; }
            set { m_AllowActivationOnMobileDevice = value; }
        }

        public float inputActionsPerSecond {
            get { return m_InputActionsPerSecond; }
            set { m_InputActionsPerSecond = value; }
        }

        /// <summary>
        /// Name of the horizontal axis for movement (if axis events are used).
        /// </summary>
        public string horizontalAxis {
            get { return m_HorizontalAxis; }
            set { m_HorizontalAxis = value; }
        }

        /// <summary>
        /// Name of the vertical axis for movement (if axis events are used).
        /// </summary>
        public string verticalAxis {
            get { return m_VerticalAxis; }
            set { m_VerticalAxis = value; }
        }

        public string submitButton {
            get { return m_SubmitButton; }
            set { m_SubmitButton = value; }
        }

        public string cancelButton {
            get { return m_CancelButton; }
            set { m_CancelButton = value; }
        }

        public bool allowMouseInput {
            get { return m_allowMouseInput; }
            set { m_allowMouseInput = value; }
        }

        public bool allowMouseInputIfTouchSupported {
            get { return m_allowMouseInputIfTouchSupported; }
            set { m_allowMouseInputIfTouchSupported = value; }
        }

        private bool isMouseSupported {
            get {
                if(!m_allowMouseInput) return false;
                return isTouchSupported ? m_allowMouseInputIfTouchSupported : true;
            }
        }

        // Methods

        protected override void Awake() {
            base.Awake();

            // Determine if touch is supported
            isTouchSupported = Input.touchSupported;

            // Initialize Rewired
            InitializeRewired();
        }

        public override void UpdateModule() {
            CheckEditorRecompile();
            if(recompiling) return;
            if(isMouseSupported) {
                m_LastMousePosition = m_MousePosition;
                m_MousePosition = mouse.screenPosition;
            }
        }

        public override bool IsModuleSupported() {
            // Check for mouse presence instead of whether touch is supported,
            // as you can connect mouse to a tablet and in that case we'd want
            // to use StandaloneInputModule for non-touch input events.
            return m_AllowActivationOnMobileDevice || Input.mousePresent;
        }

        public override bool ShouldActivateModule() {
            if(!base.ShouldActivateModule())
                return false;
            if(recompiling) return false;

            bool shouldActivate = false;

            // Combine input for all players
            for(int i = 0; i < players.Length; i++) {
                Rewired.Player player = players[i];
                if(player == null) continue;

                shouldActivate |= player.GetButtonDown(m_SubmitButton);
                shouldActivate |= player.GetButtonDown(m_CancelButton);
                if(moveOneElementPerAxisPress) { // axis press moves only to the next UI element with each press
                    shouldActivate |= player.GetButtonDown(m_HorizontalAxis) || player.GetNegativeButtonDown(m_HorizontalAxis);
                    shouldActivate |= player.GetButtonDown(m_VerticalAxis) || player.GetNegativeButtonDown(m_VerticalAxis);
                } else { // default behavior - axis press scrolls quickly through UI elements
                    shouldActivate |= !Mathf.Approximately(player.GetAxisRaw(m_HorizontalAxis), 0.0f);
                    shouldActivate |= !Mathf.Approximately(player.GetAxisRaw(m_VerticalAxis), 0.0f);
                }
            }

            if(isMouseSupported) {
                shouldActivate |= (m_MousePosition - m_LastMousePosition).sqrMagnitude > 0.0f;
                shouldActivate |= mouse.GetButtonDown(0);
            }
            
            return shouldActivate;
        }

        public override void ActivateModule() {
            base.ActivateModule();

            if(isMouseSupported) {
                m_MousePosition = Input.mousePosition;
                m_LastMousePosition = Input.mousePosition;
            }

            var toSelect = eventSystem.currentSelectedGameObject;
            if(toSelect == null)
                toSelect = eventSystem.firstSelectedGameObject;

            eventSystem.SetSelectedGameObject(toSelect, GetBaseEventData());
        }

        public override void DeactivateModule() {
            base.DeactivateModule();
            ClearSelection();
        }

        public override void Process() {
            bool usedEvent = SendUpdateEventToSelectedObject();

            if(eventSystem.sendNavigationEvents) {
                if(!usedEvent)
                    usedEvent |= SendMoveEventToSelectedObject();

                if(!usedEvent)
                    SendSubmitEventToSelectedObject();
            }

            if(isMouseSupported) {
                ProcessMouseEvent();
            }
        }

        /// <summary>
        /// Process submit keys.
        /// </summary>
        private bool SendSubmitEventToSelectedObject() {
            if(eventSystem.currentSelectedGameObject == null)
                return false;
            if(recompiling) return false;

            var data = GetBaseEventData();
            for(int i = 0; i < players.Length; i++) {
                if(players[i] == null) continue;

                if(players[i].GetButtonDown(m_SubmitButton)) {
                    ExecuteEvents.Execute(eventSystem.currentSelectedGameObject, data, ExecuteEvents.submitHandler);
                    break;
                }

                if(players[i].GetButtonDown(m_CancelButton)) {
                    ExecuteEvents.Execute(eventSystem.currentSelectedGameObject, data, ExecuteEvents.cancelHandler);
                    break;
                }
            }
            return data.used;
        }

        private bool AllowMoveEventProcessing(float time) {
            if(recompiling) return false;

            bool allow = false;

            for(int i = 0; i < players.Length; i++) {
                Rewired.Player player = players[i];
                if(player == null) continue;

                allow |= player.GetButtonDown(m_HorizontalAxis) || player.GetNegativeButtonDown(m_HorizontalAxis);
                allow |= player.GetButtonDown(m_VerticalAxis) || player.GetNegativeButtonDown(m_VerticalAxis);
                allow |= (time > m_NextAction);
            }

            return allow;
        }

        private Vector2 GetRawMoveVector() {
            if(recompiling) return Vector2.zero;

            Vector2 move = Vector2.zero;
            bool horizButton = false;
            bool vertButton = false;

            // Combine inputs of all Players
            for(int i = 0; i < players.Length; i++) {
                Rewired.Player player = players[i];
                if(player == null) continue;

                if(moveOneElementPerAxisPress) { // axis press moves only to the next UI element with each press
                    float x = 0.0f;
                    if(player.GetButtonDown(m_HorizontalAxis)) x = 1.0f;
                    else if(player.GetNegativeButtonDown(m_HorizontalAxis)) x = -1.0f;

                    float y = 0.0f;
                    if(player.GetButtonDown(m_VerticalAxis)) y = 1.0f;
                    else if(player.GetNegativeButtonDown(m_VerticalAxis)) y = -1.0f;
                    
                    move.x += x;
                    move.y += y;

                } else { // default behavior - axis press scrolls quickly through UI elements
                    move.x += player.GetAxisRaw(m_HorizontalAxis);
                    move.y += player.GetAxisRaw(m_VerticalAxis);
                }
                
                
                horizButton |= player.GetButtonDown(m_HorizontalAxis) || player.GetNegativeButtonDown(m_HorizontalAxis);
                vertButton |= player.GetButtonDown(m_VerticalAxis) || player.GetNegativeButtonDown(m_VerticalAxis);
            }

            if(horizButton) {
                if(move.x < 0)
                    move.x = -1f;
                if(move.x > 0)
                    move.x = 1f;
            }
            if(vertButton) {
                if(move.y < 0)
                    move.y = -1f;
                if(move.y > 0)
                    move.y = 1f;
            }
            return move;
        }

        /// <summary>
        /// Process keyboard events.
        /// </summary>
        private bool SendMoveEventToSelectedObject() {
            float time = Time.unscaledTime;

            if(!AllowMoveEventProcessing(time))
                return false;

            Vector2 movement = GetRawMoveVector();
            // Debug.Log(m_ProcessingEvent.rawType + " axis:" + m_AllowAxisEvents + " value:" + "(" + x + "," + y + ")");
            var axisEventData = GetAxisEventData(movement.x, movement.y, 0.6f);
            if(!Mathf.Approximately(axisEventData.moveVector.x, 0f)
                || !Mathf.Approximately(axisEventData.moveVector.y, 0f)) {
                ExecuteEvents.Execute(eventSystem.currentSelectedGameObject, axisEventData, ExecuteEvents.moveHandler);
            }
            m_NextAction = time + 1f / m_InputActionsPerSecond;
            return axisEventData.used;
        }

        /// <summary>
        /// Process all mouse events.
        /// </summary>
        private void ProcessMouseEvent() {

            // Breaking change to UnityEngine.EventSystems.PointerInputModule.GetMousePointerEventData() in Unity 5.1.2p1. This code cannot compile in these patch releases because no defines exist for patch releases
#if !UNITY_5 || (UNITY_5 && (UNITY_5_0 || UNITY_5_1_0 || UNITY_5_1_1 || UNITY_5_1_2))
            var mouseData = GetMousePointerEventData();
#else
            var mouseData = GetMousePointerEventData(kMouseLeftId);
#endif

            var pressed = mouseData.AnyPressesThisFrame();
            var released = mouseData.AnyReleasesThisFrame();

            var leftButtonData = mouseData.GetButtonState(PointerEventData.InputButton.Left).eventData;

            if(!UseMouse(pressed, released, leftButtonData.buttonData))
                return;

            // Process the first mouse button fully
            ProcessMousePress(leftButtonData);
            ProcessMove(leftButtonData.buttonData);
            ProcessDrag(leftButtonData.buttonData);

            // Now process right / middle clicks
            ProcessMousePress(mouseData.GetButtonState(PointerEventData.InputButton.Right).eventData);
            ProcessDrag(mouseData.GetButtonState(PointerEventData.InputButton.Right).eventData.buttonData);
            ProcessMousePress(mouseData.GetButtonState(PointerEventData.InputButton.Middle).eventData);
            ProcessDrag(mouseData.GetButtonState(PointerEventData.InputButton.Middle).eventData.buttonData);

            if(!Mathf.Approximately(leftButtonData.buttonData.scrollDelta.sqrMagnitude, 0.0f)) {
                var scrollHandler = ExecuteEvents.GetEventHandler<IScrollHandler>(leftButtonData.buttonData.pointerCurrentRaycast.gameObject);
                ExecuteEvents.ExecuteHierarchy(scrollHandler, leftButtonData.buttonData, ExecuteEvents.scrollHandler);
            }
        }

        private static bool UseMouse(bool pressed, bool released, PointerEventData pointerData) {
            if(pressed || released || pointerData.IsPointerMoving() || pointerData.IsScrolling())
                return true;

            return false;
        }

        private bool SendUpdateEventToSelectedObject() {
            if(eventSystem.currentSelectedGameObject == null)
                return false;

            var data = GetBaseEventData();
            ExecuteEvents.Execute(eventSystem.currentSelectedGameObject, data, ExecuteEvents.updateSelectedHandler);
            return data.used;
        }

        /// <summary>
        /// Process the current mouse press.
        /// </summary>
        private void ProcessMousePress(MouseButtonEventData data) {
            var pointerEvent = data.buttonData;
            var currentOverGo = pointerEvent.pointerCurrentRaycast.gameObject;

            // PointerDown notification
            if(data.PressedThisFrame()) {
                pointerEvent.eligibleForClick = true;
                pointerEvent.delta = Vector2.zero;
                pointerEvent.dragging = false;
                pointerEvent.useDragThreshold = true;
                pointerEvent.pressPosition = pointerEvent.position;
                pointerEvent.pointerPressRaycast = pointerEvent.pointerCurrentRaycast;

                DeselectIfSelectionChanged(currentOverGo, pointerEvent);

                // search for the control that will receive the press
                // if we can't find a press handler set the press
                // handler to be what would receive a click.
                var newPressed = ExecuteEvents.ExecuteHierarchy(currentOverGo, pointerEvent, ExecuteEvents.pointerDownHandler);

                // didnt find a press handler... search for a click handler
                if(newPressed == null)
                    newPressed = ExecuteEvents.GetEventHandler<IPointerClickHandler>(currentOverGo);

                // Debug.Log("Pressed: " + newPressed);

                float time = Time.unscaledTime;

                if(newPressed == pointerEvent.lastPress) {
                    var diffTime = time - pointerEvent.clickTime;
                    if(diffTime < 0.3f)
                        ++pointerEvent.clickCount;
                    else
                        pointerEvent.clickCount = 1;

                    pointerEvent.clickTime = time;
                } else {
                    pointerEvent.clickCount = 1;
                }

                pointerEvent.pointerPress = newPressed;
                pointerEvent.rawPointerPress = currentOverGo;

                pointerEvent.clickTime = time;

                // Save the drag handler as well
                pointerEvent.pointerDrag = ExecuteEvents.GetEventHandler<IDragHandler>(currentOverGo);

                if(pointerEvent.pointerDrag != null)
                    ExecuteEvents.Execute(pointerEvent.pointerDrag, pointerEvent, ExecuteEvents.initializePotentialDrag);
            }

            // PointerUp notification
            if(data.ReleasedThisFrame()) {
                // Debug.Log("Executing pressup on: " + pointer.pointerPress);
                ExecuteEvents.Execute(pointerEvent.pointerPress, pointerEvent, ExecuteEvents.pointerUpHandler);

                // Debug.Log("KeyCode: " + pointer.eventData.keyCode);

                // see if we mouse up on the same element that we clicked on...
                var pointerUpHandler = ExecuteEvents.GetEventHandler<IPointerClickHandler>(currentOverGo);

                // PointerClick and Drop events
                if(pointerEvent.pointerPress == pointerUpHandler && pointerEvent.eligibleForClick) {
                    ExecuteEvents.Execute(pointerEvent.pointerPress, pointerEvent, ExecuteEvents.pointerClickHandler);
                } else if(pointerEvent.pointerDrag != null) {
                    ExecuteEvents.ExecuteHierarchy(currentOverGo, pointerEvent, ExecuteEvents.dropHandler);
                }

                pointerEvent.eligibleForClick = false;
                pointerEvent.pointerPress = null;
                pointerEvent.rawPointerPress = null;

                if(pointerEvent.pointerDrag != null && pointerEvent.dragging)
                    ExecuteEvents.Execute(pointerEvent.pointerDrag, pointerEvent, ExecuteEvents.endDragHandler);

                pointerEvent.dragging = false;
                pointerEvent.pointerDrag = null;

                // redo pointer enter / exit to refresh state
                // so that if we moused over somethign that ignored it before
                // due to having pressed on something else
                // it now gets it.
                if(currentOverGo != pointerEvent.pointerEnter) {
                    HandlePointerExitAndEnter(pointerEvent, null);
                    HandlePointerExitAndEnter(pointerEvent, currentOverGo);
                }
            }
        }

        #region Rewired Methods

        private void InitializeRewired() {
            if(!Rewired.ReInput.isReady) {
                Debug.LogError("Rewired is not initialized! Are you missing a Rewired Input Manager in your scene?");
                return;
            }
            Rewired.ReInput.EditorRecompileEvent += OnEditorRecompile;
            SetupRewiredVars();
        }

        private void SetupRewiredVars() {
            // Set up Rewired vars
            mouse = Rewired.ReInput.controllers.Mouse; // get the mouse

            // Set up Rewired Players
            if(useAllRewiredGamePlayers) {
                IList<Rewired.Player> rwPlayers = useRewiredSystemPlayer ? Rewired.ReInput.players.AllPlayers : Rewired.ReInput.players.Players;
                players = new Rewired.Player[rwPlayers.Count];
                for(int i = 0; i < rwPlayers.Count; i++) {
                    players[i] = rwPlayers[i];
                }
            } else {
                int rewiredPlayerCount = rewiredPlayerIds.Length + (useRewiredSystemPlayer ? 1 : 0);
                players = new Rewired.Player[rewiredPlayerCount];
                for(int i = 0; i < rewiredPlayerIds.Length; i++) {
                    players[i] = Rewired.ReInput.players.GetPlayer(rewiredPlayerIds[i]);
                }
                if(useRewiredSystemPlayer) players[rewiredPlayerCount - 1] = Rewired.ReInput.players.GetSystemPlayer();
            }
        }

        private void CheckEditorRecompile() {
            if(!recompiling) return;
            if(!Rewired.ReInput.isReady) return;
            recompiling = false;
            InitializeRewired();
        }

        private void OnEditorRecompile() {
            recompiling = true;
            ClearRewiredVars();
        }

        private void ClearRewiredVars() {
            players = new Rewired.Player[0];
            mouse = null;
        }

        #endregion
    }
}