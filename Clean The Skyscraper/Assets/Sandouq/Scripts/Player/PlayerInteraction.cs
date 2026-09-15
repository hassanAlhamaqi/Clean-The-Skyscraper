using Sandouq.Cleaning;
using UnityEngine;

namespace Sandouq.Player
{
    public sealed class PlayerInteraction : MonoBehaviour
    {
        [SerializeField] private PlayerCameraController cameraController;
        [SerializeField] private Transform handOrigin;
        [Tooltip("Include blockers as well as cleanable objects. Ignore the player layer.")]
        [SerializeField] private LayerMask contactLayers = ~(1 << 2);
        [SerializeField] private bool debugRay;
        private Collider cachedCollider;
        private CleanableSurface cachedSurface;
        public bool TryContact(float reach, out CleanableSurface surface, out RaycastHit hit)
        {
            surface = null;
            if (!TryHit(reach, out hit)) return false;
            if (hit.collider != cachedCollider)
            {
                cachedCollider = hit.collider;
                cachedSurface = cachedCollider.GetComponentInParent<CleanableSurface>();
            }
            surface = cachedSurface;
            return surface != null;
        }
        // Buttons and tools share the same close-range, obstruction-aware aiming rule.
        public bool TryHit(float reach, out RaycastHit hit)
        {
            Transform view = cameraController.ViewCamera.transform;
            float cameraOffset = Vector3.Distance(view.position, handOrigin.position);
            bool found = Physics.Raycast(view.position, view.forward, out hit, reach + cameraOffset, contactLayers, QueryTriggerInteraction.Ignore);
            if (debugRay) Debug.DrawRay(view.position, view.forward * (reach + cameraOffset), found ? Color.green : Color.red);
            if (!found || Vector3.Distance(handOrigin.position, hit.point) > reach) return false;
            Vector3 handToContact = hit.point - handOrigin.position;
            if (Physics.Raycast(handOrigin.position, handToContact.normalized, out RaycastHit obstruction,
                handToContact.magnitude + 0.01f, contactLayers, QueryTriggerInteraction.Ignore) && obstruction.collider != hit.collider) return false;
            return true;
        }
    }
}
