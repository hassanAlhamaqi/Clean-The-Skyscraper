using System;
using System.IO;
using System.Linq;
using Sandouq.Cleaning;
using Sandouq.Dirt;
using Sandouq.Platforms;
using Sandouq.Player;
using Sandouq.UI;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace Sandouq.Editor
{
    // Explicit batch smoke test only. It never runs in the user's open editor on its own.
    [InitializeOnLoad]
    public static class SkyscraperValidation
    {
        private const string Key = "Sandouq.SkyscraperValidation";
        private static int stage;
        private static double deadline;
        private static CleaningJob job;
        private static CleaningActionRouter router;
        private static LocalPlayerRig player;
        private static MovingPlatform lift;
        private static PlatformButton button;
        private static CleaningHUD hud;
        private static float originalTotal, neighborProgress, stoppedHeight, riderHeight;
        static SkyscraperValidation()
        {
            EditorApplication.playModeStateChanged += state => {
                if (!SessionState.GetBool(Key, false) || state != PlayModeStateChange.EnteredPlayMode) return;
                stage = 0; deadline = EditorApplication.timeSinceStartup + 3;
                EditorApplication.update += Tick;
            };
        }
        public static void Run()
        {
            if (!Application.isBatchMode) throw new InvalidOperationException("Use an isolated batch editor.");
            SkyscraperBuilder.Build();
            EditorSceneManager.OpenScene(SkyscraperBuilder.ScenePath);
            SessionState.SetBool(Key, true);
            EditorApplication.isPlaying = true;
        }
        private static T Find<T>() where T : Component => SceneManager.GetActiveScene().GetRootGameObjects()
            .SelectMany(root => root.GetComponentsInChildren<T>()).FirstOrDefault();
        private static void Tick()
        {
            if (EditorApplication.timeSinceStartup < deadline) return;
            double delay = 1;
            try
            {
                switch (stage)
                {
                    case 0:
                        job = Find<CleaningJob>(); router = Find<CleaningActionRouter>(); player = Find<LocalPlayerRig>();
                        lift = Find<MovingPlatform>(); button = Find<PlatformButton>(); hud = Find<CleaningHUD>();
                        Check(job && router && player && lift && button && hud, "Scene dependencies");
                        Check(job.Surfaces.Length == 24 && job.Surfaces.Select(surface => surface.SurfaceId).Distinct().Count() == 24, "24 uniquely identified windows");
                        Check(job.GetComponentsInChildren<DirtMask>().All(mask => mask.IsReady), "All GPU masks initialized");
                        Check(job.Surfaces.Min(surface => surface.CurrentCleanliness) < 0.01f && job.Surfaces.Max(surface => surface.CurrentCleanliness) > 0.45f, "Varied starting cleanliness");
                        Check(job.GetComponentsInChildren<WindowProgressLabel>().Length == 24, "Individual window UI");
                        Check(Mathf.Abs(job.Cleanliness - job.Surfaces.Average(surface => surface.CurrentCleanliness)) < 0.001f, "Overall aggregate for equal-area windows");
                        Check(hud.transform.Find("Progress").GetComponent<Text>().text.StartsWith("Overall Clean:"), "HUD bound to overall job");
                        originalTotal = job.Cleanliness; neighborProgress = job.Surfaces[1].CurrentCleanliness;
                        Teleport(new Vector3(-3.975f, 0.25f, -1.5f));
                        CaptureOverview();
                        break;
                    case 1:
                        Check(player.GetComponent<PlayerInteraction>().TryContact(2, out _, out _), "Window reachable from deck");
                        Check(!player.GetComponent<PlayerInteraction>().TryContact(0.1f, out _, out _), "Distance limit retained");
                        router.Request(new CleaningStroke { playerId = player.PlayerId, surfaceId = job.Surfaces[0].SurfaceId,
                            fromUV = new Vector2(0.2f, 0.5f), toUV = new Vector2(0.8f, 0.5f), radius = 0.2f, amount = 2, falloff = 0.2f });
                        break;
                    case 2:
                        Check(job.Surfaces[0].CurrentCleanliness > 0.05f && job.Cleanliness > originalTotal, "Stroke updates window and overall progress");
                        Check(Mathf.Abs(job.Surfaces[1].CurrentCleanliness - neighborProgress) < 0.001f, "Other window remains unchanged");
                        VerifyMaskStroke();
                        Check(lift.State == MovingPlatform.MotionState.StoppedBeforeAscent, "Lift starts stopped");
                        riderHeight = player.transform.position.y;
                        button.Press(player);
                        Check(lift.State == MovingPlatform.MotionState.Ascending, "First press raises");
                        delay = 1.5;
                        break;
                    case 3:
                        Check(lift.Height > 1, "Lift travels upward");
                        Check(Mathf.Abs((player.transform.position.y - riderHeight) - lift.Height) < 0.15f, "Grounded rider carried upward");
                        button.Press(player); stoppedHeight = lift.Height;
                        Check(lift.State == MovingPlatform.MotionState.StoppedBeforeDescent, "Second press stops ascent");
                        break;
                    case 4:
                        Check(Mathf.Abs(lift.Height - stoppedHeight) < 0.04f, "Stopped platform holds height");
                        button.Press(player);
                        Check(lift.State == MovingPlatform.MotionState.Descending, "Third press descends");
                        delay = 0.5;
                        break;
                    case 5:
                        Check(lift.Height < stoppedHeight - 0.2f && lift.Height > 0, "Controlled descent");
                        Check(Mathf.Abs(player.transform.position.y - riderHeight - lift.Height) < 0.15f, "Grounded rider carried downward");
                        button.Press(player);
                        Check(lift.State == MovingPlatform.MotionState.StoppedBeforeAscent, "Fourth press stops descent");
                        VerifyRemoteRig();
                        lift.AdvanceState(); lift.Simulate(100);
                        break;
                    case 6:
                        Check(Mathf.Abs(lift.Height - lift.TravelHeight) < 0.01f && lift.State == MovingPlatform.MotionState.StoppedBeforeDescent, "Top limit stops and selects descent");
                        lift.AdvanceState(); lift.Simulate(100);
                        break;
                    case 7:
                        Check(lift.Height < 0.01f && lift.State == MovingPlatform.MotionState.StoppedBeforeAscent, "Bottom limit stops and selects ascent");
                        VerifyDeferredRequest();
                        break;
                    case 8:
                        Check(Mathf.Abs(job.Surfaces[1].CurrentCleanliness - neighborProgress) < 0.001f, "Unaccepted request does not mutate dirt");
                        router.ApplyAccepted(new CleaningStroke { surfaceId = job.Surfaces[1].SurfaceId, radius = 100, amount = 10, falloff = 0.1f });
                        break;
                    case 9:
                        Check(job.Surfaces[1].IsClean, "Accepted stroke playback updates shared state");
                        Debug.Log("SANDOUQ_SKYSCRAPER_VALIDATED: dirt variation, job/window UI, stroke isolation, lift cycle/end stops/riding, remote rig and deferred commands.");
                        Finish(0);
                        break;
                }
                stage++; deadline = EditorApplication.timeSinceStartup + delay;
            }
            catch (Exception exception) { Debug.LogException(exception); Finish(1); }
        }
        private static void Teleport(Vector3 position)
        {
            var controller = player.GetComponent<CharacterController>();
            controller.enabled = false; player.transform.position = position; controller.enabled = true;
            Physics.SyncTransforms();
        }
        private static void VerifyMaskStroke()
        {
            var mask = job.Surfaces[0].GetComponentInChildren<DirtMask>();
            RenderTexture previous = RenderTexture.active;
            var texture = new Texture2D(256, 256, TextureFormat.RGBA32, false, true);
            RenderTexture.active = (RenderTexture)mask.Mask;
            texture.ReadPixels(new Rect(0, 0, 256, 256), 0, 0); texture.Apply();
            RenderTexture.active = previous;
            Check(texture.GetPixel(128, 128).r < 0.01f && texture.GetPixel(10, 10).r > 0.05f, "Continuous stroke with dirt preserved outside it");
            UnityEngine.Object.Destroy(texture);
        }
        private static void VerifyRemoteRig()
        {
            var holder = new GameObject("Remote validation holder"); holder.SetActive(false);
            GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Sandouq/Prefabs/Player/PF_Player.prefab");
            var remote = UnityEngine.Object.Instantiate(prefab, holder.transform);
            var rig = remote.GetComponent<LocalPlayerRig>();
            SkyscraperBuilder.Edit(rig, serialized => serialized.FindProperty("isLocalPlayer").boolValue = false);
            var cursorBefore = Cursor.lockState;
            holder.SetActive(true);
            Check(!remote.GetComponent<PlayerInputReader>().enabled && !remote.GetComponentInChildren<Camera>().enabled
                && !remote.GetComponentInChildren<AudioListener>().enabled && !remote.GetComponent<CharacterController>().enabled, "Remote replica has no local input/camera/controller");
            Check(Cursor.lockState == cursorBefore, "Remote spawn preserves local cursor");
            var state = lift.State; button.Press(rig);
            Check(lift.State == state, "Remote replica cannot operate local button");
            UnityEngine.Object.Destroy(holder);
        }
        private static void VerifyDeferredRequest()
        {
            SkyscraperBuilder.Edit(router, serialized => serialized.FindProperty("applyRequestsLocally").boolValue = false);
            bool requested = false;
            router.StrokeRequested += stroke => requested = true;
            router.Request(new CleaningStroke { surfaceId = job.Surfaces[1].SurfaceId, radius = 100, amount = 10, falloff = 0.1f });
            Check(requested, "Cleaning emits a request for a future authority adapter");
        }
        private static void CaptureOverview()
        {
            var cameraObject = new GameObject("Validation Camera"); var camera = cameraObject.AddComponent<Camera>(); camera.enabled = false;
            camera.transform.position = new Vector3(13, 11, -25); camera.transform.LookAt(new Vector3(0, 8, 0));
            camera.backgroundColor = new Color(0.5f, 0.7f, 0.85f); camera.clearFlags = CameraClearFlags.SolidColor;
            var target = new RenderTexture(1280, 960, 24); var pixels = new Texture2D(1280, 960, TextureFormat.RGB24, false);
            var previous = RenderTexture.active;
            camera.targetTexture = target; camera.Render(); RenderTexture.active = target;
            pixels.ReadPixels(new Rect(0, 0, 1280, 960), 0, 0); pixels.Apply();
            Directory.CreateDirectory("Assets/Sandouq/Documentation/Validation");
            File.WriteAllBytes("Assets/Sandouq/Documentation/Validation/Skyscraper.png", pixels.EncodeToPNG());
            RenderTexture.active = previous; camera.targetTexture = null; target.Release();
            UnityEngine.Object.Destroy(target); UnityEngine.Object.Destroy(pixels); UnityEngine.Object.Destroy(cameraObject);
        }
        private static void Check(bool success, string description)
        {
            if (!success) throw new InvalidOperationException("Validation failed: " + description);
            Debug.Log("SANDOUQ_CHECK: " + description);
        }
        private static void Finish(int code)
        {
            SessionState.SetBool(Key, false); EditorApplication.update -= Tick; EditorApplication.Exit(code);
        }
    }
}
