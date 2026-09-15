using UnityEngine;

namespace Sandouq.Player
{
    // Local walking/jumping only. PlatformRider adds the support's movement before this update.
    [RequireComponent(typeof(CharacterController))]
    public sealed class PlayerMovement : MonoBehaviour
    {
        [SerializeField] private PlayerInputReader input;
        [Header("Movement")]
        [SerializeField, Min(0)] private float walkSpeed = 3.5f;
        [SerializeField, Min(0)] private float sprintSpeed = 5.5f;
        [SerializeField, Min(0)] private float jumpHeight = 0.8f;
        [SerializeField] private float gravity = -20f;
        private CharacterController controller;
        private float verticalSpeed;
        private void Awake() => controller = GetComponent<CharacterController>();
        private void Update()
        {
            // Cursor release suppresses player commands, but gravity/platform riding must continue.
            bool acceptInput = Cursor.lockState == CursorLockMode.Locked;
            if (controller.isGrounded)
            {
                verticalSpeed = -2f;
                if (acceptInput && input.JumpPressed) verticalSpeed = Mathf.Sqrt(jumpHeight * -2f * gravity);
            }
            verticalSpeed += gravity * Time.deltaTime;
            Vector2 move = Vector2.ClampMagnitude(input.Move, 1);
            Vector3 velocity = (transform.right * move.x + transform.forward * move.y)
                * (input.SprintHeld ? sprintSpeed : walkSpeed);
            velocity.y = verticalSpeed;
            controller.Move(velocity * Time.deltaTime);
        }
    }
}
