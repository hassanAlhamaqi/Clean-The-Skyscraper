using UnityEngine;

namespace Sandouq.Player
{
    // Apply walking, jumping and platform displacement together so grounded state stays reliable.
    [RequireComponent(typeof(CharacterController))]
    public sealed class PlayerMovement : MonoBehaviour
    {
        [SerializeField] private PlayerInputReader input;
        [Header("Movement")]
        [SerializeField, Min(0)] private float walkSpeed = 3.5f;
        [SerializeField, Min(0)] private float sprintSpeed = 5.5f;
        [Header("Jump")]
        [Tooltip("Jump height in metres. Press Space or the gamepad bottom face button while grounded.")]
        [SerializeField, Min(0)] private float jumpHeight = 0.8f;
        [SerializeField] private float gravity = -20f;
        private CharacterController controller;
        private PlatformRider platformRider;
        private float verticalSpeed;
        private void Awake()
        {
            controller = GetComponent<CharacterController>();
            // Tiny gravity steps still need collision checks at high frame rates.
            controller.minMoveDistance = 0;
            platformRider = GetComponent<PlatformRider>();
        }
        private void Update()
        {
            // Cursor release suppresses player commands, but gravity/platform riding must continue.
            bool acceptInput = Cursor.lockState == CursorLockMode.Locked;
            if (controller.isGrounded && verticalSpeed <= 0)
            {
                verticalSpeed = -2f;
            }
            if (acceptInput && input.JumpPressed) TryJump();
            verticalSpeed += gravity * Time.deltaTime;
            Vector2 move = Vector2.ClampMagnitude(input.Move, 1);
            Vector3 velocity = (transform.right * move.x + transform.forward * move.y)
                * (input.SprintHeld ? sprintSpeed : walkSpeed);
            velocity.y = verticalSpeed;
            Vector3 carry = platformRider && platformRider.enabled ? platformRider.Displacement : Vector3.zero;
            CollisionFlags collisions = controller.Move(velocity * Time.deltaTime + carry);
            // Stop upward velocity when the player bumps their head.
            if ((collisions & CollisionFlags.Above) != 0 && verticalSpeed > 0) verticalSpeed = 0;
        }
        public bool TryJump()
        {
            // The local input (or a future owned movement adapter) requests the same grounded jump.
            if (!isActiveAndEnabled || !controller || !controller.enabled || !controller.isGrounded || verticalSpeed > 0)
                return false;
            verticalSpeed = Mathf.Sqrt(jumpHeight * -2f * gravity);
            return true;
        }
    }
}
