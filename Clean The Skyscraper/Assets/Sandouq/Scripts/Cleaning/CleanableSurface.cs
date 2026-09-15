using System;
using UnityEngine;

namespace Sandouq.Cleaning
{
    // A component contract lets the raycaster cache one reference per collider.
    public abstract class CleanableSurface : MonoBehaviour
    {
        [Header("Scene identity")]
        [Tooltip("Unique within the job. Future network messages can refer to this ID instead of a scene object.")]
        [SerializeField] private string surfaceId = "window-01";
        public string SurfaceId => surfaceId;
        public virtual float Area => 1;
        [Header("Completion")]
        [SerializeField, Range(0.5f, 1f)] private float requiredCleanliness = 0.95f;
        public float CurrentCleanliness { get; private set; }
        public float RequiredCleanliness => requiredCleanliness;
        public bool IsClean => CurrentCleanliness >= requiredCleanliness;
        public event Action<float> OnCleaningProgressChanged;
        public event Action OnCleaningCompleted;
        public abstract void Clean(Vector2 fromUV, Vector2 toUV, float radius, float amount, float falloff);
        protected void SetProgress(float cleanliness)
        {
            bool wasClean = IsClean;
            CurrentCleanliness = Mathf.Clamp01(cleanliness);
            OnCleaningProgressChanged?.Invoke(CurrentCleanliness);
            if (!wasClean && IsClean) OnCleaningCompleted?.Invoke();
        }
    }
}
