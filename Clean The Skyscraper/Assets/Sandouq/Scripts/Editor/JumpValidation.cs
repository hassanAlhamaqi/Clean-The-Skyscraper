using System;
using System.Linq;
using Sandouq.Platforms;
using Sandouq.Player;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.LowLevel;
using UnityEngine.SceneManagement;

namespace Sandouq.Editor
{
    // Explicit Play Mode regression: exercises real Input System bindings on ground and on the lift.
    [InitializeOnLoad]
    public static class JumpValidation
    {
        private const string SessionKey = "Sandouq.JumpValidation";
        private static PlayerMovement player;
        private static CharacterController controller;
        private static MovingPlatform lift;
        private static Keyboard keyboard;
        private static Gamepad gamepad;
        private static int stage;
        private static double nextStep;
        private static float baseline, peak;
        static JumpValidation()
        {
            EditorApplication.playModeStateChanged += state => {
                if (!SessionState.GetBool(SessionKey, false) || state != PlayModeStateChange.EnteredPlayMode) return;
                stage = 0; nextStep = EditorApplication.timeSinceStartup + 2;
                EditorApplication.update += Tick;
            };
        }
        public static void Run()
        {
            if (!Application.isBatchMode) throw new InvalidOperationException("Use an isolated batch editor.");
            EditorSceneManager.OpenScene(SkyscraperBuilder.ScenePath);
            SessionState.SetBool(SessionKey, true);
            EditorApplication.isPlaying = true;
        }
        private static void Tick()
        {
            if (player && stage > 1) peak = Mathf.Max(peak, player.transform.position.y - (stage >= 7 ? lift.Height : 0));
            if (EditorApplication.timeSinceStartup < nextStep) return;
            double delay = 0.2;
            try
            {
                switch (stage)
                {
                    case 0:
                        var roots = SceneManager.GetActiveScene().GetRootGameObjects();
                        player = roots.SelectMany(root => root.GetComponentsInChildren<PlayerMovement>()).First();
                        lift = roots.SelectMany(root => root.GetComponentsInChildren<MovingPlatform>()).First();
                        controller = player.GetComponent<CharacterController>();
                        keyboard = InputSystem.AddDevice<Keyboard>();
                        gamepad = InputSystem.AddDevice<Gamepad>();
                        InputSystem.settings.backgroundBehavior = InputSettings.BackgroundBehavior.IgnoreFocus;
                        InputSystem.settings.editorInputBehaviorInPlayMode = InputSettings.EditorInputBehaviorInPlayMode.AllDeviceInputAlwaysGoesToGameView;
                        InputSystem.onAfterUpdate += ForwardJumpInput;
                        Cursor.lockState = CursorLockMode.Locked;
                        Teleport(new Vector3(-2, 0.1f, -5)); delay = 1;
                        break;
                    case 1:
                        Check(controller.isGrounded, $"Grounded before jumping (y={player.transform.position.y:F3}, enabled={player.enabled})");
                        baseline = peak = player.transform.position.y;
                        InputSystem.QueueStateEvent(keyboard, new KeyboardState(Key.Space));
                        break;
                    case 2:
                        Check(!controller.isGrounded && player.transform.position.y > baseline + 0.1f,
                            $"Space launches the player (y={player.transform.position.y:F3}, peak={peak:F3}, cursor={Cursor.lockState}, key={keyboard.spaceKey.isPressed}, input={player.GetComponent<PlayerInputReader>().enabled})");
                        InputSystem.QueueStateEvent(keyboard, new KeyboardState()); delay = 1;
                        break;
                    case 3:
                        Check(peak > baseline + 0.5f && controller.isGrounded, "Ground jump gains height and lands");
                        baseline = peak = player.transform.position.y;
                        InputSystem.QueueStateEvent(gamepad, new GamepadState().WithButton(GamepadButton.South));
                        break;
                    case 4:
                        Check(player.transform.position.y > baseline + 0.1f, "Gamepad bottom button launches the player");
                        // A second press in mid-air must not produce another jump.
                        InputSystem.QueueStateEvent(gamepad, new GamepadState()); delay = 0.05;
                        break;
                    case 5:
                        InputSystem.QueueStateEvent(gamepad, new GamepadState().WithButton(GamepadButton.South)); delay = 1;
                        break;
                    case 6:
                        Check(controller.isGrounded && peak < baseline + 1.1f, "No mid-air double jump or held-button bouncing");
                        InputSystem.QueueStateEvent(gamepad, new GamepadState());
                        Teleport(new Vector3(-3.975f, 0.25f, -1.5f)); delay = 1;
                        break;
                    case 7:
                        Check(controller.isGrounded, "Grounded on platform");
                        lift.RequestButtonPress("jump-test"); delay = 0.7;
                        break;
                    case 8:
                        Check(lift.Height > 0.3f && controller.isGrounded, "Ascending platform preserves grounded state");
                        baseline = peak = player.transform.position.y - lift.Height;
                        InputSystem.QueueStateEvent(keyboard, new KeyboardState(Key.Space));
                        break;
                    case 9:
                        Check(!controller.isGrounded && player.transform.position.y - lift.Height > baseline + 0.1f, "Space jumps off an ascending platform");
                        InputSystem.QueueStateEvent(keyboard, new KeyboardState()); delay = 1;
                        break;
                    case 10:
                        Check(controller.isGrounded, "Player lands back on moving platform");
                        lift.RequestButtonPress("jump-test");
                        lift.RequestButtonPress("jump-test"); delay = 0.5;
                        break;
                    case 11:
                        baseline = player.transform.position.y - lift.Height;
                        InputSystem.QueueStateEvent(keyboard, new KeyboardState(Key.Space));
                        break;
                    case 12:
                        Check(!controller.isGrounded && player.transform.position.y - lift.Height > baseline + 0.1f, "Space jumps off a descending platform");
                        InputSystem.QueueStateEvent(keyboard, new KeyboardState());
                        Debug.Log("SANDOUQ_JUMP_VALIDATED: keyboard/gamepad, landing, no double jump, ascending and descending lift.");
                        Finish(0);
                        break;
                }
                stage++; nextStep = EditorApplication.timeSinceStartup + delay;
            }
            catch (Exception exception) { Debug.LogException(exception); Finish(1); }
        }
        private static void Teleport(Vector3 position)
        {
            controller.enabled = false; player.transform.position = position; controller.enabled = true;
            Physics.SyncTransforms();
        }
        private static void ForwardJumpInput()
        {
            // Batch editors cannot lock the cursor. Read the real binding, but bypass only that UI gate.
            if (InputState.currentUpdateType == InputUpdateType.Dynamic && player && player.GetComponent<PlayerInputReader>().JumpPressed)
                player.TryJump();
        }
        private static void Check(bool passed, string description)
        {
            if (!passed) throw new InvalidOperationException("Jump validation failed: " + description);
            Debug.Log("SANDOUQ_CHECK: " + description);
        }
        private static void Finish(int code)
        {
            InputSystem.onAfterUpdate -= ForwardJumpInput;
            if (keyboard != null) InputSystem.RemoveDevice(keyboard);
            if (gamepad != null) InputSystem.RemoveDevice(gamepad);
            EditorApplication.update -= Tick; SessionState.SetBool(SessionKey, false); EditorApplication.Exit(code);
        }
    }
}

