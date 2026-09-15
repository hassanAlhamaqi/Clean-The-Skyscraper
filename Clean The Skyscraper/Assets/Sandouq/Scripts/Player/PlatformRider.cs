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
        private void Awake() => controller = GetComponent<CharacterController>();
        private void Update()
        {
            if (support && controller.enabled)
                controller.Move(support.transform.position - previousSupportPosition);
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
        private void OnDisable() => support = null;
    }
}
