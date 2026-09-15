using UnityEngine;

namespace Sandouq.Tools
{
    // Positions the visual tool at contact. The controller handles requests; the surface owns the dirt.
    public sealed class CleaningTool : MonoBehaviour
    {
        [SerializeField] private CleaningToolData data;
        [Tooltip("Place this transform at the centre of the rubber blade.")]
        [SerializeField] private Transform cleaningPoint;
        private Vector3 restPosition;
        private Quaternion restRotation;
        public CleaningToolData Data => data;
        private void Awake() { restPosition = transform.localPosition; restRotation = transform.localRotation; }
        public void ShowContact(bool touching, Vector3 point, Vector3 normal)
        {
            if (!touching) { transform.localPosition = restPosition; transform.localRotation = restRotation; return; }
            transform.rotation = Quaternion.LookRotation(-normal, Vector3.up);
            transform.position += point + normal * 0.015f - cleaningPoint.position;
        }
    }
}
