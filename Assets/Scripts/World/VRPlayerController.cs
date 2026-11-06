using UnityEngine;
using UnityEngine.XR;
using UnityEngine.SceneManagement;
using UnityEngine.EventSystems;
using UnityEngine.UI;
using System.Collections.Generic;

namespace TrainingDemo
{
    [RequireComponent(typeof(CharacterController))]
    public class VRPlayerController : MonoBehaviour
    {
        [SerializeField] private Camera viewCamera;
        [SerializeField] private float moveSpeed = 3.2f;
        [SerializeField] private float lookSpeed = 2.1f;
        private CharacterController _character;
        private float _pitch;
        private InteractableTarget _focused;
        private InputDevice _rightHand;
        private Vector3 _velocity;
        private bool _rightTriggerWasPressed;
        private bool _pointerOnVrUI;
        private Canvas _worldCanvas;
        private GraphicRaycaster _worldRaycaster;
        private PointerEventData _vrPointer;
        private GameObject _vrHoveredObject;
        private readonly List<RaycastResult> _uiHits = new List<RaycastResult>();
        public InteractableTarget FocusedTarget => _focused;

        private void Awake()
        {
            _character = GetComponent<CharacterController>();
            if (viewCamera == null) viewCamera = GetComponentInChildren<Camera>();
            var lobby = SceneManager.GetActiveScene().name == "Lobby";
            Cursor.lockState = lobby ? CursorLockMode.None : CursorLockMode.Locked;
            Cursor.visible = lobby;
        }

        public void SetCamera(Camera camera) => viewCamera = camera;

        private void Update()
        {
            if (viewCamera == null) return;
            var desktop = !XRSettings.isDeviceActive;
            var horizontal = Input.GetAxisRaw("Horizontal");
            var vertical = Input.GetAxisRaw("Vertical");
            if (!desktop && TryGetLeftHand(out var device) && device.TryGetFeatureValue(CommonUsages.primary2DAxis, out var axis))
            { horizontal = axis.x; vertical = axis.y; }

            var movement = transform.right * horizontal + transform.forward * vertical;
            if (_character.isGrounded) _velocity.y = -1f;
            else _velocity.y -= 18f * Time.deltaTime;
            _character.Move((movement * moveSpeed + Vector3.up * _velocity.y) * Time.deltaTime);

            if (desktop)
            {
                if (Input.GetKeyDown(KeyCode.Escape)) { Cursor.lockState = CursorLockMode.None; Cursor.visible = true; }
                var pointerOverUI = EventSystem.current != null && EventSystem.current.IsPointerOverGameObject();
                if (Input.GetMouseButtonDown(0) && Cursor.lockState != CursorLockMode.Locked && !pointerOverUI && _focused == null)
                { Cursor.lockState = CursorLockMode.Locked; Cursor.visible = false; }
                if (Cursor.lockState == CursorLockMode.Locked)
                {
                    transform.Rotate(Vector3.up, Input.GetAxis("Mouse X") * lookSpeed);
                    _pitch = Mathf.Clamp(_pitch - Input.GetAxis("Mouse Y") * lookSpeed, -80f, 80f);
                    viewCamera.transform.localRotation = Quaternion.Euler(_pitch, 0f, 0f);
                }
            }
            else if (TryGetNode(XRNode.Head, out var head))
            {
                if (head.TryGetFeatureValue(CommonUsages.devicePosition, out var pos)) viewCamera.transform.localPosition = pos;
                if (head.TryGetFeatureValue(CommonUsages.deviceRotation, out var rot)) viewCamera.transform.localRotation = rot;
            }

            UpdateFocus();
            var pointerOnUI = desktop && EventSystem.current != null && EventSystem.current.IsPointerOverGameObject();
            var pressed = Input.GetKeyDown(KeyCode.E) || Input.GetKeyDown(KeyCode.JoystickButton0);
            if (Input.GetMouseButtonDown(0) && !pointerOnUI) pressed = true;
            if (TryGetHand(out var hand) && hand.TryGetFeatureValue(CommonUsages.triggerButton, out var trigger))
            {
                if (trigger && !_rightTriggerWasPressed) pressed = true;
                _rightTriggerWasPressed = trigger;
            }
            if (pressed && !pointerOnUI) _focused?.Interact();
            if (pressed && !desktop && _pointerOnVrUI) ClickVrUI();
        }

        private void UpdateFocus()
        {
            var desktop = !XRSettings.isDeviceActive;
            var origin = viewCamera.transform.position;
            var direction = viewCamera.transform.forward;
            if (XRSettings.isDeviceActive && TryGetHand(out var hand) &&
                hand.TryGetFeatureValue(CommonUsages.devicePosition, out var handPosition) &&
                hand.TryGetFeatureValue(CommonUsages.deviceRotation, out var handRotation))
            {
                origin = transform.TransformPoint(handPosition);
                direction = transform.rotation * handRotation * Vector3.forward;
            }
            else if (desktop && Cursor.lockState != CursorLockMode.Locked)
            {
                var pointerRay = viewCamera.ScreenPointToRay(Input.mousePosition);
                origin = pointerRay.origin;
                direction = pointerRay.direction;
            }
            var ray = new Ray(origin, direction);
            _pointerOnVrUI = !desktop && UpdateVrUI(ray);
            if (_pointerOnVrUI)
            {
                _focused?.SetHighlighted(false);
                _focused = null;
                return;
            }
            var next = Physics.Raycast(ray, out var hit, 3.2f) ? hit.collider.GetComponentInParent<InteractableTarget>() : null;
            if (_focused == next) return;
            _focused?.SetHighlighted(false);
            _focused = next;
            _focused?.SetHighlighted(true);
        }

        private bool UpdateVrUI(Ray ray)
        {
            if (_worldCanvas == null)
            {
                _worldCanvas = FindObjectOfType<Canvas>();
                _worldRaycaster = _worldCanvas != null ? _worldCanvas.GetComponent<GraphicRaycaster>() : null;
            }
            if (_worldCanvas == null || _worldCanvas.renderMode != RenderMode.WorldSpace || _worldRaycaster == null || EventSystem.current == null)
            {
                SetVrHover(null);
                return false;
            }

            var plane = new Plane(_worldCanvas.transform.forward, _worldCanvas.transform.position);
            if (!plane.Raycast(ray, out var distance)) { SetVrHover(null); return false; }
            var screenPoint = _worldCanvas.worldCamera.WorldToScreenPoint(ray.GetPoint(distance));
            if (screenPoint.z < 0f) { SetVrHover(null); return false; }

            if (_vrPointer == null) _vrPointer = new PointerEventData(EventSystem.current);
            _vrPointer.position = new Vector2(screenPoint.x, screenPoint.y);
            _vrPointer.button = PointerEventData.InputButton.Left;
            _uiHits.Clear();
            _worldRaycaster.Raycast(_vrPointer, _uiHits);
            if (_uiHits.Count == 0) { SetVrHover(null); return false; }
            _vrPointer.pointerCurrentRaycast = _uiHits[0];
            var hoverTarget = ExecuteEvents.GetEventHandler<IPointerEnterHandler>(_uiHits[0].gameObject);
            SetVrHover(hoverTarget != null ? hoverTarget : _uiHits[0].gameObject);
            return true;
        }

        private void SetVrHover(GameObject next)
        {
            if (_vrHoveredObject == next) return;
            if (_vrHoveredObject != null && _vrPointer != null)
                ExecuteEvents.Execute(_vrHoveredObject, _vrPointer, ExecuteEvents.pointerExitHandler);
            _vrHoveredObject = next;
            if (_vrHoveredObject != null && _vrPointer != null)
                ExecuteEvents.Execute(_vrHoveredObject, _vrPointer, ExecuteEvents.pointerEnterHandler);
        }

        private void ClickVrUI()
        {
            if (_vrHoveredObject == null || _vrPointer == null) return;
            var handler = ExecuteEvents.GetEventHandler<IPointerClickHandler>(_vrHoveredObject);
            if (handler == null) return;

            _vrPointer.pointerPress = ExecuteEvents.GetEventHandler<IPointerDownHandler>(_vrHoveredObject);
            if (_vrPointer.pointerPress == null) _vrPointer.pointerPress = handler;
            _vrPointer.rawPointerPress = _vrHoveredObject;
            _vrPointer.pressPosition = _vrPointer.position;
            _vrPointer.pointerPressRaycast = _vrPointer.pointerCurrentRaycast;
            ExecuteEvents.Execute(_vrPointer.pointerPress, _vrPointer, ExecuteEvents.pointerDownHandler);
            ExecuteEvents.Execute(_vrPointer.pointerPress, _vrPointer, ExecuteEvents.pointerUpHandler);
            ExecuteEvents.Execute(handler, _vrPointer, ExecuteEvents.pointerClickHandler);
            _vrPointer.pointerPress = null;
            _vrPointer.rawPointerPress = null;
        }

        private bool TryGetHand(out InputDevice device)
        {
            if (!_rightHand.isValid) TryGetNode(XRNode.RightHand, out _rightHand);
            device = _rightHand;
            return device.isValid;
        }

        private bool TryGetLeftHand(out InputDevice device) => TryGetNode(XRNode.LeftHand, out device);

        private static bool TryGetNode(XRNode node, out InputDevice device)
        {
            var devices = new System.Collections.Generic.List<InputDevice>();
            InputDevices.GetDevicesAtXRNode(node, devices);
            device = devices.Count > 0 ? devices[0] : default;
            return device.isValid;
        }
    }
}
