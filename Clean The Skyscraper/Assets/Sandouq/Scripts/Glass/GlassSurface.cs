using Sandouq.Cleaning;
using Sandouq.Dirt;
using UnityEngine;

namespace Sandouq.Glass
{
    // Converts this planar mesh's UV dimensions into world metres before passing a stroke to its mask.
    [RequireComponent(typeof(MeshCollider))]
    public sealed class GlassSurface : CleanableSurface
    {
        [SerializeField] private DirtMask dirtMask;
        [Tooltip("Local width/height mapped over UV 0–1. Quad defaults to one metre per axis.")]
        [SerializeField] private Vector2 localSize = Vector2.one;
        public override float Area => transform.TransformVector(Vector3.right * localSize.x).magnitude
            * transform.TransformVector(Vector3.up * localSize.y).magnitude;
        private void OnEnable() { if (dirtMask) dirtMask.ProgressChanged += SetProgress; }
        private void OnDisable() { if (dirtMask) dirtMask.ProgressChanged -= SetProgress; }
        public override void Clean(Vector2 fromUV, Vector2 toUV, float radius, float amount, float falloff)
        {
            Vector2 size = new Vector2(transform.TransformVector(Vector3.right * localSize.x).magnitude,
                transform.TransformVector(Vector3.up * localSize.y).magnitude);
            dirtMask.Erase(fromUV, toUV, size, radius, amount, falloff);
        }
    }
}
