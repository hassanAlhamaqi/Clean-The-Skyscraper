using System;
using System.Collections;
using UnityEngine;
using UnityEngine.Rendering;

namespace Sandouq.Dirt
{
    // Each window owns its mask. Shared DirtType assets supply settings, never shared cleaning state.
    public sealed class DirtMask : MonoBehaviour
    {
        [Header("Mask")]
        [SerializeField] private DirtType dirt;
        [SerializeField] private Shader brushShader;
        [SerializeField] private Renderer dirtRenderer;
        [Tooltip("Power of two mask size. Each surface owns two GPU textures.")]
        [SerializeField, Range(64, 2048)] private int resolution = 512;
        [Header("Per-window dirt (set before Play)")]
        [Tooltip("Optional grayscale texture for this window. Otherwise uses the Dirt Type texture.")]
        [SerializeField] private Texture2D textureOverride;
        [SerializeField] private Vector2 textureTiling = Vector2.one;
        [SerializeField] private Vector2 textureOffset;
        [Tooltip("Starting fraction already clean. Also reduces the visible dirt by this amount.")]
        [SerializeField, Range(0, 1)] private float startingCleanliness;
        [Header("Progress")]
        [Tooltip("Seconds between tiny asynchronous GPU readbacks.")]
        [SerializeField, Min(0.1f)] private float progressInterval = 0.3f;
        private RenderTexture current, spare;
        private Material brush;
        private MaterialPropertyBlock properties;
        private float initialMass, nextProgress;
        private bool pending, dirty, initialized;
        private Action<AsyncGPUReadbackRequest> readbackCallback;
        public event Action<float> ProgressChanged;
        public Texture Mask => current;
        public bool IsReady => initialized;

        private IEnumerator Start()
        {
            // Let URP finish its first-frame setup before initializing persistent GPU contents.
            yield return null;
            if (!dirt || !brushShader || !dirtRenderer || !SystemInfo.supportsAsyncGPUReadback)
            {
                Debug.LogError("DirtMask requires dirt, brush shader, renderer and asynchronous GPU readback support.", this);
                enabled = false;
                yield break;
            }
            resolution = Mathf.ClosestPowerOfTwo(resolution);
            brush = new Material(brushShader);
            current = CreateMask("Dirt mask");
            spare = CreateMask("Dirt mask spare");
            Texture2D pattern = textureOverride ? textureOverride : dirt.Texture;
            brush.SetTexture("_Pattern", pattern ? pattern : Texture2D.whiteTexture);
            brush.SetFloat("_UsePattern", pattern ? 1 : 0);
            brush.SetVector("_PatternTransform", new Vector4(textureTiling.x, textureTiling.y, textureOffset.x, textureOffset.y));
            brush.SetFloat("_DirtAmount", dirt.Amount * (1 - startingCleanliness));
            BlitPreservingTarget(Texture2D.whiteTexture, current, 0);
            properties = new MaterialPropertyBlock();
            BindMask();
            readbackCallback = ReceiveProgress;
            RequestProgress();
        }

        private RenderTexture CreateMask(string label)
        {
            var texture = new RenderTexture(resolution, resolution, 0, RenderTextureFormat.ARGB32, RenderTextureReadWrite.Linear)
            {
                name = label, useMipMap = true, autoGenerateMips = false,
                filterMode = FilterMode.Bilinear, wrapMode = TextureWrapMode.Clamp
            };
            texture.Create();
            return texture;
        }

        public void Erase(Vector2 from, Vector2 to, Vector2 worldSize, float radius, float amount, float falloff)
        {
            // Preserve the initial mass snapshot before accepting the first stroke.
            if (!initialized || !enabled) return;
            brush.SetVector("_Stroke", new Vector4(from.x, from.y, to.x, to.y));
            brush.SetVector("_WorldSize", new Vector4(worldSize.x, worldSize.y, 0, 0));
            brush.SetFloat("_Radius", radius);
            brush.SetFloat("_Strength", amount / Mathf.Max(0.01f, dirt.Resistance));
            brush.SetFloat("_Falloff", falloff);
            BlitPreservingTarget(current, spare, 1);
            (current, spare) = (spare, current);
            BindMask();
            dirty = true;
        }

        private void BindMask()
        {
            dirtRenderer.GetPropertyBlock(properties);
            properties.SetTexture("_DirtMask", current);
            properties.SetColor("_DirtColor", dirt.Color);
            dirtRenderer.SetPropertyBlock(properties);
        }

        private void BlitPreservingTarget(Texture source, RenderTexture destination, int pass)
        {
            // Graphics.Blit changes global render state; leaving a mask active lets later rendering clear it.
            RenderTexture previous = RenderTexture.active;
            try { Graphics.Blit(source, destination, brush, pass); }
            finally { RenderTexture.active = previous; }
        }

        private void Update()
        {
            if (dirty && !pending && Time.unscaledTime >= nextProgress) RequestProgress();
        }

        private void RequestProgress()
        {
            current.GenerateMips();
            pending = true;
            dirty = false;
            nextProgress = Time.unscaledTime + progressInterval;
            // Read a 16x16 mip: avoids large readbacks and 1x1 mip quantization bias.
            int mip = Mathf.Max(0, (int)Mathf.Log(resolution, 2) - 4);
            AsyncGPUReadback.Request(current, mip, TextureFormat.RGBA32, readbackCallback);
        }

        private void ReceiveProgress(AsyncGPUReadbackRequest request)
        {
            pending = false;
            if (!this || !current) return;
            if (request.hasError) { dirty = true; return; }
            var pixels = request.GetData<Color32>();
            float mass = 0;
            for (int index = 0; index < pixels.Length; index++) mass += pixels[index].r / 255f;
            if (!initialized)
            {
                // Reconstruct the fully dirty reference, so a 40%-clean window starts at 40% in the HUD.
                initialMass = startingCleanliness < 1 ? mass / (1 - startingCleanliness) : 0;
                initialized = true;
            }
            ProgressChanged?.Invoke(initialMass > 0.0001f ? Mathf.Clamp01(1f - mass / initialMass) : 1f);
        }

        private void OnDestroy()
        {
            if (current) { current.Release(); Destroy(current); }
            if (spare) { spare.Release(); Destroy(spare); }
            if (brush) Destroy(brush);
        }
    }
}
