using UnityEngine;
using UnityEngine.InputSystem;

namespace Sandouq.Player
{
    // Only enable this on the locally controlled avatar. Actions are allocated once and disposed on teardown.
    public sealed class PlayerInputReader : MonoBehaviour
    {
        private InputAction move, mouseLook, stickLook, clean, jump, sprint, perspective, cursor, interact;
        private bool ownsCursor;
        public bool InteractPressed => Cursor.lockState == CursorLockMode.Locked && interact.WasPressedThisFrame();
        public Vector2 Move => Cursor.lockState == CursorLockMode.Locked ? move.ReadValue<Vector2>() : Vector2.zero;
        public Vector2 MouseLook => mouseLook.ReadValue<Vector2>();
        public Vector2 StickLook => stickLook.ReadValue<Vector2>();
        public bool CleanHeld => Cursor.lockState == CursorLockMode.Locked && clean.IsPressed();
        public bool JumpPressed => jump.WasPressedThisFrame();
        public bool SprintHeld => sprint.IsPressed();
        public bool PerspectivePressed => perspective.WasPressedThisFrame();
        private InputAction[] actions;
        private void Awake()
        {
            move = new InputAction("Move", InputActionType.Value);
            move.AddCompositeBinding("2DVector").With("Up", "<Keyboard>/w").With("Down", "<Keyboard>/s")
                .With("Left", "<Keyboard>/a").With("Right", "<Keyboard>/d");
            move.AddBinding("<Gamepad>/leftStick");
            mouseLook = new InputAction("Mouse Look", binding: "<Mouse>/delta");
            stickLook = new InputAction("Stick Look", binding: "<Gamepad>/rightStick");
            clean = Button("Clean", "<Mouse>/leftButton", "<Gamepad>/rightTrigger");
            jump = Button("Jump", "<Keyboard>/space", "<Gamepad>/buttonSouth");
            sprint = Button("Sprint", "<Keyboard>/leftShift", "<Gamepad>/leftStickPress");
            perspective = Button("Perspective", "<Keyboard>/v", "<Gamepad>/rightStickPress");
            cursor = Button("Cursor", "<Keyboard>/escape", "<Gamepad>/start");
            interact = Button("Interact", "<Keyboard>/e", "<Gamepad>/buttonWest");
            actions = new[] { move, mouseLook, stickLook, clean, jump, sprint, perspective, cursor, interact };
        }
        private static InputAction Button(string label, string keyboard, string gamepad)
        {
            var action = new InputAction(label, InputActionType.Button, keyboard);
            action.AddBinding(gamepad);
            return action;
        }
        private void OnEnable()
        {
            if (actions == null) return;
            foreach (var action in actions) action.Enable();
            ownsCursor = true;
            SetCursor(true);
        }
        private void OnDisable()
        {
            if (actions != null) foreach (var action in actions) action.Disable();
            // Disabling a remote avatar must not unlock the local player's cursor.
            if (ownsCursor) SetCursor(false);
            ownsCursor = false;
        }
        private void OnDestroy() { if (actions != null) foreach (var action in actions) action.Dispose(); }
        private void Update()
        {
            if (cursor.WasPressedThisFrame()) SetCursor(Cursor.lockState != CursorLockMode.Locked);
        }
        private static void SetCursor(bool locked)
        {
            Cursor.lockState = locked ? CursorLockMode.Locked : CursorLockMode.None;
            Cursor.visible = !locked;
        }
    }
}
