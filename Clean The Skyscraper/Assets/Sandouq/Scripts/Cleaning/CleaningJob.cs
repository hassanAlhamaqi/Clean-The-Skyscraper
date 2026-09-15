using System;
using UnityEngine;

namespace Sandouq.Cleaning
{
    // A job owns an explicit list of windows. It never searches for a global player or singleton.
    public sealed class CleaningJob : MonoBehaviour
    {
        [SerializeField] private CleanableSurface[] surfaces = Array.Empty<CleanableSurface>();
        public CleanableSurface[] Surfaces => surfaces;
        public float Cleanliness { get; private set; }
        public event Action<float> ProgressChanged;
        private void OnEnable()
        {
            foreach (var surface in surfaces)
                if (surface) surface.OnCleaningProgressChanged += Recalculate;
            Recalculate(0);
        }
        private void OnDisable()
        {
            foreach (var surface in surfaces)
                if (surface) surface.OnCleaningProgressChanged -= Recalculate;
        }
        private void Recalculate(float unused)
        {
            // Weight by area: cleaning a large pane counts more than cleaning a small one.
            float totalArea = 0, cleanArea = 0;
            foreach (var surface in surfaces)
            {
                if (!surface) continue;
                totalArea += surface.Area;
                cleanArea += surface.Area * surface.CurrentCleanliness;
            }
            Cleanliness = totalArea > 0 ? cleanArea / totalArea : 0;
            ProgressChanged?.Invoke(Cleanliness);
        }
    }
}
