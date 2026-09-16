using Sandouq.Platforms;
using UnityEngine;

namespace Sandouq.Player
{
    // Carry a grounded CharacterController without parenting the whole player/camera to the lift.
    [DefaultExecutionOrder(-50)]
    [RequireComponent(typeof(CharacterController))]
    public sealed class PlatformRider : MonoBehaviour
    {
        [SerializeField] private LayerMask groundLayers = ~(1 << 2);
        private CharacterController controller;
        private MovingPlatform support;
        private Vector3 previousSupportPosition;
        private Collider cachedCollider;
        private MovingPlatform cachedPlatform;
        public Vector3 Displacement { get; private set; }
        private void Awake() => controller = GetComponent<CharacterController>();
        private void Update()
        {
            // PlayerMovement applies this in its one Move call. A separate upward Move here
            // would clear isGrounded just before the player tries to jump off the lift.
            Displacement = support && controller.enabled
                ? support.transform.position - previousSupportPosition : Vector3.zero;
        }
        private void LateUpdate()
        {
            support = null;
            if (!controller.enabled || !controller.isGrounded) return;
            // The probe is short: jumping or walking off immediately releases the rider.
            if (!Physics.SphereCast(transform.position + Vector3.up * 0.3f, 0.2f, Vector3.down,
                out var hit, 0.25f, groundLayers, QueryTriggerInteraction.Ignore)) return;
            if (cachedCollider != hit.collider)
            {
                cachedCollider = hit.collider;
                cachedPlatform = hit.collider.GetComponentInParent<MovingPlatform>();
            }
            support = cachedPlatform;
            if (support) previousSupportPosition = support.transform.position;
        }
        private void OnDisable() { support = null; Displacement = Vector3.zero; }
    }
}
