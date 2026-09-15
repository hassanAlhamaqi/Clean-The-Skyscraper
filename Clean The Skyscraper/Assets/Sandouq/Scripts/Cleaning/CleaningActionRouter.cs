using System;
using System.Collections.Generic;
using UnityEngine;

namespace Sandouq.Cleaning
{
    [Serializable]
    public struct CleaningStroke
    {
        public string playerId, surfaceId;
        public Vector2 fromUV, toUV;
        public float radius, amount, falloff;
    }

    // Offline adapter today. A future host can validate requests and distribute accepted strokes.
    public sealed class CleaningActionRouter : MonoBehaviour
    {
        [SerializeField] private CleaningJob job;
        [Tooltip("Turn off when a future network adapter will accept/reject requests.")]
        [SerializeField] private bool applyRequestsLocally = true;
        private readonly Dictionary<string, CleanableSurface> surfaces = new Dictionary<string, CleanableSurface>();
        public event Action<CleaningStroke> StrokeRequested;
        private void Awake()
        {
            foreach (var surface in job.Surfaces)
            {
                if (!surface || string.IsNullOrWhiteSpace(surface.SurfaceId) || surfaces.ContainsKey(surface.SurfaceId))
                    throw new InvalidOperationException("Each job surface needs a unique, nonempty Surface Id.");
                surfaces.Add(surface.SurfaceId, surface);
            }
        }
        public void Request(CleaningStroke stroke)
        {
            StrokeRequested?.Invoke(stroke);
            if (applyRequestsLocally) ApplyAccepted(stroke);
        }
        public bool ApplyAccepted(CleaningStroke stroke)
        {
            // This is the playback boundary, not a network security validator. A host must check reach,
            // tool ownership, sequence and resource cost before calling it with a remote request.
            if (!surfaces.TryGetValue(stroke.surfaceId ?? "", out var surface) || !surface) return false;
            surface.Clean(stroke.fromUV, stroke.toUV, stroke.radius, stroke.amount, stroke.falloff);
            return true;
        }
    }
}
