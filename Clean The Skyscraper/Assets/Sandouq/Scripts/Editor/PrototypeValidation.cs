using System;
using System.IO;
using Sandouq.Cleaning;
using Sandouq.Dirt;
using Sandouq.Glass;
using Sandouq.Player;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace Sandouq.Editor
{
    // Batch-only smoke test. Never starts automatically in the user's editor.
    [InitializeOnLoad]
    public static class PrototypeValidation
    {
        private const string Key = "Sandouq.ValidationRunning";
        private static double nextStep;
        private static int stage, completions;
        private static GlassSurface surface;
        private static PlayerInteraction interaction;
        private static PlayerCameraController cameraController;
        private static DirtMask mask;
        static PrototypeValidation()
        {
            EditorApplication.playModeStateChanged += state =>
            {
                if (SessionState.GetBool(Key, false) && state == PlayModeStateChange.EnteredPlayMode)
                {
                    stage = 0;
                    nextStep = EditorApplication.timeSinceStartup + 2;
                    EditorApplication.update += Tick;
                }
            };
        }
        public static void Run()
        {
            if (!Application.isBatchMode) throw new InvalidOperationException("Run this smoke test in an isolated batch editor.");
            PrototypeBuilder.Build();
            foreach (string name in new[] { "Sandouq/DirtyGlass", "Hidden/Sandouq/DirtBrush" })
            {
                Shader shader = Shader.Find(name);
                if (!shader || ShaderUtil.ShaderHasError(shader)) throw new InvalidOperationException("Shader failed: " + name);
            }
            EditorSceneManager.OpenScene("Assets/Sandouq/Scenes/Prototype/Cleaning_Prototype.unity");
            SessionState.SetBool(Key, true);
            EditorApplication.isPlaying = true;
        }
        private static void Tick()
        {
            if (EditorApplication.timeSinceStartup < nextStep) return;
            try
            {
                if (stage == 0)
                {
                    foreach (GameObject root in SceneManager.GetActiveScene().GetRootGameObjects())
                    {
                        if (!surface) surface = root.GetComponentInChildren<GlassSurface>();
                        if (!interaction) interaction = root.GetComponentInChildren<PlayerInteraction>();
                        if (!cameraController) cameraController = root.GetComponentInChildren<PlayerCameraController>();
                        if (!mask) mask = root.GetComponentInChildren<DirtMask>();
                    }
                    Require(surface && interaction && mask && cameraController, "Scene references");
                    Require(mask.enabled && mask.Mask, "GPU mask initialized");
                    Require(mask.IsReady, "Initial GPU readback ready");
                    InspectMask("Before stroke");
                    Require(surface.CurrentCleanliness < 0.01f, "Initial dirt baseline");
                    Require(interaction.TryContact(2, out _, out RaycastHit hit), "First person contact");
                    Require(hit.textureCoord.x > 0 && hit.textureCoord.x < 1, "Mesh UV contact");
                    Require(!interaction.TryContact(0.1f, out _, out _), "Out-of-reach rejection");
                    Capture("DirtyFirstPerson.png");
                    surface.OnCleaningCompleted += () => completions++;
                    surface.Clean(new Vector2(0.2f, 0.5f), new Vector2(0.8f, 0.5f), 0.2f, 2, 0.2f);
                    var cameraSettings = new SerializedObject(cameraController);
                    cameraSettings.FindProperty("perspective").enumValueIndex = 1;
                    cameraSettings.ApplyModifiedPropertiesWithoutUndo();
                }
                else if (stage == 1)
                {
                    InspectMask("After stroke");
                    Require(surface.CurrentCleanliness > 0.01f && surface.CurrentCleanliness < 0.5f, "Partial progress: " + surface.CurrentCleanliness);
                    Require(interaction.TryContact(2, out _, out _), "Third person contact uses hand reach");
                    Require(cameraController.ViewCamera.transform.localPosition.z < -1, "Third person camera offset");
                    RenderTexture previous = RenderTexture.active;
                    var sample = new Texture2D(512, 512, TextureFormat.RGBA32, false, true);
                    RenderTexture.active = (RenderTexture)mask.Mask;
                    sample.ReadPixels(new Rect(0, 0, 512, 512), 0, 0); sample.Apply();
                    RenderTexture.active = previous;
                    Require(sample.GetPixel(256, 256).r < 0.01f, "Continuous stroke midpoint erased");
                    Require(sample.GetPixel(20, 20).r > 0.1f, "Untouched dirt preserved");
                    UnityEngine.Object.Destroy(sample);
                    Capture("WipedThirdPerson.png");
                    surface.Clean(Vector2.zero, Vector2.one, 100, 10, 0.1f);
                }
                else if (stage == 2)
                {
                    Require(surface.IsClean && surface.CurrentCleanliness > 0.99f, "Full cleaning progress");
                    Require(completions == 1, "Completion emitted once");
                    surface.Clean(Vector2.zero, Vector2.one, 100, 10, 0.1f);
                }
                else
                {
                    Require(completions == 1, "Repeated cleaning does not repeat completion");
                    Debug.Log("SANDOUQ_RUNTIME_VALIDATED: first/third-person contact, reach, UV, continuous GPU stroke, preserved dirt, progress and completion events.");
                    Finish(0);
                }
                stage++;
                nextStep = EditorApplication.timeSinceStartup + 1.5;
            }
            catch (Exception exception) { Debug.LogException(exception); Finish(1); }
        }
        private static void Require(bool condition, string message)
        {
            if (!condition) throw new InvalidOperationException("Validation failed: " + message);
            Debug.Log("SANDOUQ_CHECK: " + message);
        }
        private static void Capture(string name)
        {
            Camera camera = cameraController.ViewCamera;
            RenderTexture previousTarget = camera.targetTexture;
            RenderTexture previousActive = RenderTexture.active;
            var target = new RenderTexture(1280, 720, 24);
            var pixels = new Texture2D(1280, 720, TextureFormat.RGB24, false);
            try
            {
                camera.targetTexture = target;
                camera.Render();
                RenderTexture.active = target;
                pixels.ReadPixels(new Rect(0, 0, 1280, 720), 0, 0);
                pixels.Apply();
                Directory.CreateDirectory("Assets/Sandouq/Documentation/Validation");
                File.WriteAllBytes("Assets/Sandouq/Documentation/Validation/" + name, pixels.EncodeToPNG());
            }
            finally
            {
                camera.targetTexture = previousTarget;
                RenderTexture.active = previousActive;
                target.Release();
                UnityEngine.Object.Destroy(target);
                UnityEngine.Object.Destroy(pixels);
            }
        }
        private static void InspectMask(string label)
        {
            RenderTexture previous = RenderTexture.active;
            var texture = new Texture2D(512, 512, TextureFormat.RGBA32, false, true);
            RenderTexture.active = (RenderTexture)mask.Mask;
            texture.ReadPixels(new Rect(0, 0, 512, 512), 0, 0); texture.Apply();
            RenderTexture.active = previous;
            Debug.Log($"SANDOUQ_MASK: {label}, centre={texture.GetPixel(256, 256)}, corner={texture.GetPixel(20, 20)}, scale={surface.transform.lossyScale}");
            UnityEngine.Object.Destroy(texture);
        }
        private static void Finish(int code)
        {
            EditorApplication.update -= Tick;
            SessionState.SetBool(Key, false);
            EditorApplication.Exit(code);
        }
    }
}
