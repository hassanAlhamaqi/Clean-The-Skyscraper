using UnityEngine;

namespace Sandouq.Player
{
    // Both perspectives share one aiming camera, so switching views never changes the cleaning system.
    [DefaultExecutionOrder(-100)]
    public sealed class PlayerCameraController : MonoBehaviour
    {
        public enum Perspective { FirstPerson, ThirdPerson }
        [SerializeField] private PlayerInputReader input;
        [SerializeField] private Transform cameraPivot;
        [SerializeField] private Camera viewCamera;
        [SerializeField] private Renderer body;
        [Header("Camera")]
        [SerializeField] private Perspective perspective;
        [SerializeField, Min(0)] private float mouseSensitivity = 0.12f;
        [SerializeField, Min(0)] private float stickDegreesPerSecond = 130f;
        [SerializeField, Min(0)] private float thirdPersonDistance = 3f;
        [Tooltip("Third-person shoulder offset so the body does not cover the crosshair.")]
        [SerializeField] private Vector2 shoulderOffset = new Vector2(0.6f, 0.25f);
        [Tooltip("Ignore the Player layer when checking camera obstruction.")]
        [SerializeField] private LayerMask obstructionLayers = ~(1 << 2);
        private float pitch;
        public Camera ViewCamera => viewCamera;
        private void Update()
        {
            if (Cursor.lockState == CursorLockMode.Locked)
            {
                if (input.PerspectivePressed) perspective = perspective == Perspective.FirstPerson ? Perspective.ThirdPerson : Perspective.FirstPerson;
                Vector2 look = input.MouseLook * mouseSensitivity + input.StickLook * (stickDegreesPerSecond * Time.deltaTime);
                transform.Rotate(0, look.x, 0);
                pitch = Mathf.Clamp(pitch - look.y, -80, 80);
            }
            cameraPivot.localRotation = Quaternion.Euler(pitch, 0, 0);
            Vector3 offset = perspective == Perspective.ThirdPerson
                ? new Vector3(shoulderOffset.x, shoulderOffset.y, -thirdPersonDistance) : Vector3.zero;
            float distance = offset.magnitude;
            if (distance > 0 && Physics.SphereCast(cameraPivot.position, 0.15f, cameraPivot.TransformDirection(offset / distance),
                out RaycastHit hit, distance, obstructionLayers, QueryTriggerInteraction.Ignore)) offset *= Mathf.Max(0, hit.distance - 0.05f) / distance;
            viewCamera.transform.localPosition = offset;
            viewCamera.transform.localRotation = Quaternion.identity;
            body.enabled = perspective == Perspective.ThirdPerson;
        }
    }
}
