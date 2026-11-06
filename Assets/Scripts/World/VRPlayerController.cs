using UnityEngine;
using UnityEngine.XR;
using UnityEngine.SceneManagement;
using UnityEngine.EventSystems;

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
                if (Input.GetMouseButtonDown(0) && Cursor.lockState != CursorLockMode.Locked && !pointerOverUI)
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
            var next = Physics.Raycast(ray, out var hit, 3.2f) ? hit.collider.GetComponentInParent<InteractableTarget>() : null;
            if (_focused == next) return;
            _focused?.SetHighlighted(false);
            _focused = next;
            _focused?.SetHighlighted(true);
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
