using UnityEngine;

namespace Sandouq.Tools
{
    // Shared tool definition; do not mutate this asset for one player's temporary upgrades/resources.
    [CreateAssetMenu(menuName = "Sandouq/Cleaning Tool")]
    public sealed class CleaningToolData : ScriptableObject
    {
        [Header("Identity")]
        [SerializeField] private string toolName = "Squeegee";
        [SerializeField] private Sprite icon;
        [Header("Cleaning")]
        [Tooltip("Brush radius in world metres.")]
        [SerializeField, Min(0.01f)] private float radius = 0.25f;
        [Tooltip("Dirt removed per second before dirt resistance.")]
        [SerializeField, Min(0.01f)] private float strength = 2f;
        [SerializeField, Min(0.01f)] private float cleaningSpeed = 1f;
        [Tooltip("Fraction of radius used for the soft edge.")]
        [SerializeField, Range(0.01f, 1f)] private float falloff = 0.35f;
        [Tooltip("Maximum distance from the player's hand to the contact point.")]
        [SerializeField, Min(0.1f)] private float reach = 2f;
        public string ToolName => toolName;
        public Sprite Icon => icon;
        public float Radius => radius;
        public float Strength => strength;
        public float CleaningSpeed => cleaningSpeed;
        public float Falloff => falloff;
        public float Reach => reach;
    }
}
