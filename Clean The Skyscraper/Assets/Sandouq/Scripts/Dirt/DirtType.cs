using UnityEngine;

namespace Sandouq.Dirt
{
    // Shared appearance/resistance settings. Per-window overrides live on DirtMask.
    [CreateAssetMenu(menuName = "Sandouq/Dirt Type")]
    public sealed class DirtType : ScriptableObject
    {
        [SerializeField] private string dirtName = "Basic Dirt";
        [Tooltip("Optional grayscale coverage texture. White is dirty. Uses procedural mottling when empty.")]
        [SerializeField] private Texture2D dirtTexture;
        [SerializeField] private Color color = new Color(0.32f, 0.22f, 0.10f, 0.96f);
        [SerializeField, Range(0.01f, 1f)] private float amount = 1f;
        [SerializeField, Min(0.01f)] private float resistance = 1f;
        public string DirtName => dirtName;
        public Texture2D Texture => dirtTexture;
        public Color Color => color;
        public float Amount => amount;
        public float Resistance => resistance;
    }
}
