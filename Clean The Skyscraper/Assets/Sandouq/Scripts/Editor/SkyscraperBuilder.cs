using System;
using System.IO;
using Sandouq.Cleaning;
using Sandouq.Core;
using Sandouq.Dirt;
using Sandouq.Glass;
using Sandouq.Platforms;
using Sandouq.Player;
using Sandouq.UI;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using static Sandouq.Editor.PrototypeBuilder;

namespace Sandouq.Editor
{
    // Authoring only: the delivered scene contains real, inspectable prefab instances.
    public static class SkyscraperBuilder
    {
        private const string Root = "Assets/Sandouq/";
        public const string ScenePath = Root + "Scenes/Prototype/Skyscraper_Prototype.unity";
        private const int Columns = 4, Rows = 6;

        [MenuItem("Sandouq/Create Skyscraper Prototype")]
        public static void Build()
        {
            if (File.Exists(ScenePath)) { Debug.Log("Skyscraper scene exists; existing authoring is preserved."); return; }
            Directory.CreateDirectory(Root + "Prefabs/Platforms");
            AssetDatabase.Refresh();
            Scene previous = SceneManager.GetActiveScene();
            if (!Application.isBatchMode && string.IsNullOrEmpty(previous.path))
                throw new InvalidOperationException("Save the current scene before creating the skyscraper prototype.");
            Scene scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene,
                Application.isBatchMode ? NewSceneMode.Single : NewSceneMode.Additive);
            SceneManager.SetActiveScene(scene);
            try
            {
                UpgradePlayer();
                UpgradeHUD();
                DirtType[] types = CreateDirtTypes();
                GameObject window = CreateWindow();
                GameObject facade = CreateFacade(window, types);
                GameObject platform = CreatePlatform();
                ComposeScene(facade, platform);
                EditorSceneManager.SaveScene(scene, ScenePath);
                AssetDatabase.SaveAssets();
                Debug.Log("SANDOUQ_SKYSCRAPER_BUILT: 24 windows, individual labels, job progress, local player rig and four-state lift.");
            }
            finally
            {
                if (!Application.isBatchMode)
                {
                    EditorSceneManager.CloseScene(scene, true);
                    if (previous.IsValid()) SceneManager.SetActiveScene(previous);
                }
            }
        }

        private static void UpgradePlayer()
        {
            string path = Root + "Prefabs/Player/PF_Player.prefab";
            GameObject root = PrefabUtility.LoadPrefabContents(path);
            try
            {
                var rig = GetOrAdd<LocalPlayerRig>(root);
                var rider = GetOrAdd<PlatformRider>(root);
                var buttons = GetOrAdd<PlayerButtonInteraction>(root);
                var input = root.GetComponent<PlayerInputReader>();
                var interaction = root.GetComponent<PlayerInteraction>();
                var cleaning = root.GetComponent<CleaningToolController>();
                Set(buttons, "input", input); Set(buttons, "interaction", interaction); Set(buttons, "localRig", rig);
                Set(cleaning, "localRig", rig);
                Set(rig, "viewCamera", root.GetComponentInChildren<Camera>());
                Set(rig, "audioListener", root.GetComponentInChildren<AudioListener>());
                Set(rig, "characterController", root.GetComponent<CharacterController>());
                Set(rig, "body", root.transform.Find("Player Body").GetComponent<Renderer>());
                SetArray(rig, "localBehaviours", new Behaviour[] { input, root.GetComponent<PlayerMovement>(),
                    root.GetComponent<PlayerCameraController>(), interaction, cleaning, rider, buttons });
                PrefabUtility.SaveAsPrefabAsset(root, path);
            }
            finally { PrefabUtility.UnloadPrefabContents(root); }
        }

        private static void UpgradeHUD()
        {
            string path = Root + "Prefabs/UI/PF_CleaningHUD.prefab";
            GameObject root = PrefabUtility.LoadPrefabContents(path);
            try
            {
                if (!root.transform.Find("Interaction Prompt"))
                    Label("Interaction Prompt", root.transform, "Aim at the lift button and press E / X",
                        new Vector2(28, -218), new Vector2(1000, 38), 22);
                PrefabUtility.SaveAsPrefabAsset(root, path);
            }
            finally { PrefabUtility.UnloadPrefabContents(root); }
        }

        private static DirtType[] CreateDirtTypes()
        {
            var result = new DirtType[3];
            string[] names = { "Dust", "RainStreaks", "Speckles" };
            for (int pattern = 0; pattern < names.Length; pattern++)
            {
                string texturePath = Root + "Textures/Dirt_" + names[pattern] + ".png";
                if (!File.Exists(texturePath))
                {
                    // Small authoring-time textures; no CPU texture generation runs during gameplay.
                    var texture = new Texture2D(128, 128, TextureFormat.RGB24, false, true);
                    var pixels = new Color[128 * 128];
                    for (int row = 0; row < 128; row++)
                    for (int column = 0; column < 128; column++)
                    {
                        float horizontal = column / 128f, vertical = row / 128f;
                        float noise = Mathf.PerlinNoise(horizontal * 10 + 7, vertical * 10 + 19);
                        float value = pattern == 0 ? 0.25f + noise * 0.7f
                            : pattern == 1 ? 0.18f + Mathf.PerlinNoise(horizontal * 35, vertical * 3 + 8) * 0.8f
                            : 0.15f + Mathf.SmoothStep(0, 1, Mathf.InverseLerp(0.38f, 0.67f, noise)) * 0.8f;
                        pixels[row * 128 + column] = new Color(value, value, value, 1);
                    }
                    texture.SetPixels(pixels); texture.Apply();
                    File.WriteAllBytes(texturePath, texture.EncodeToPNG());
                    UnityEngine.Object.DestroyImmediate(texture);
                    AssetDatabase.ImportAsset(texturePath);
                    var importer = (TextureImporter)AssetImporter.GetAtPath(texturePath);
                    importer.sRGBTexture = false;
                    importer.wrapMode = TextureWrapMode.Repeat;
                    importer.textureCompression = TextureImporterCompression.Uncompressed;
                    importer.SaveAndReimport();
                }
                string dataPath = Root + "ScriptableObjects/Dirt/Dirt_" + names[pattern] + ".asset";
                result[pattern] = AssetDatabase.LoadAssetAtPath<DirtType>(dataPath);
                if (result[pattern]) continue;
                result[pattern] = ScriptableObject.CreateInstance<DirtType>();
                AssetDatabase.CreateAsset(result[pattern], dataPath);
                Set(result[pattern], "dirtTexture", AssetDatabase.LoadAssetAtPath<Texture2D>(texturePath));
                Edit(result[pattern], serialized => {
                    serialized.FindProperty("dirtName").stringValue = names[pattern];
                    serialized.FindProperty("resistance").floatValue = 0.8f + pattern * 0.25f;
                    serialized.FindProperty("color").colorValue = pattern == 1 ? new Color(0.21f, 0.27f, 0.29f, 0.95f)
                        : new Color(0.35f, 0.26f, 0.14f, 0.95f);
                });
            }
            return result;
        }

        private static GameObject CreateWindow()
        {
            string path = Root + "Prefabs/Glass/PF_Window.prefab";
            var existing = AssetDatabase.LoadAssetAtPath<GameObject>(path);
            if (existing) return existing;
            var root = new GameObject("PF_Window");
            GameObject pane = (GameObject)PrefabUtility.InstantiatePrefab(Load("Prefabs/Glass/PF_GlassWall.prefab"), root.transform);
            pane.name = "Glass and Frame";
            pane.transform.localScale = new Vector3(0.4f, 0.85f, 1);
            var surface = pane.GetComponentInChildren<GlassSurface>();
            Edit(pane.GetComponentInChildren<DirtMask>(), serialized => serialized.FindProperty("resolution").intValue = 256);
            Text label = WorldLabel(root.transform, "Window Progress", new Vector3(0, 2.38f, -0.11f), new Vector2(240, 28), 17);
            var display = label.canvas.gameObject.AddComponent<WindowProgressLabel>();
            Set(display, "surface", surface); Set(display, "label", label);
            return SaveNew(root, path);
        }

        private static GameObject CreateFacade(GameObject window, DirtType[] types)
        {
            string path = Root + "Prefabs/Glass/PF_SkyscraperFacade.prefab";
            var existing = AssetDatabase.LoadAssetAtPath<GameObject>(path);
            if (existing) return existing;
            var root = new GameObject("PF_SkyscraperFacade");
            var surfaces = new CleanableSurface[Rows * Columns];
            for (int row = 0; row < Rows; row++)
            for (int column = 0; column < Columns; column++)
            {
                int index = row * Columns + column;
                var instance = (GameObject)PrefabUtility.InstantiatePrefab(window, root.transform);
                instance.name = $"Window {row + 1:00}-{column + 1:00}";
                instance.transform.localPosition = new Vector3((column - 1.5f) * 2.65f, 0.35f + row * 2.9f, 0);
                surfaces[index] = instance.GetComponentInChildren<GlassSurface>();
                Edit(surfaces[index], serialized => serialized.FindProperty("surfaceId").stringValue = $"W{row + 1:00}-{column + 1:00}");
                var mask = instance.GetComponentInChildren<DirtMask>();
                Set(mask, "dirt", types[index % types.Length]);
                Edit(mask, serialized => {
                    serialized.FindProperty("textureTiling").vector2Value = new Vector2(0.8f + (index % 3) * 0.2f, 0.9f + (index % 4) * 0.1f);
                    serialized.FindProperty("textureOffset").vector2Value = new Vector2(index * 0.173f, index * 0.317f);
                    serialized.FindProperty("startingCleanliness").floatValue = (index * 7 % 13) * 0.045f;
                });
                PrefabUtility.RecordPrefabInstancePropertyModifications(mask);
                PrefabUtility.RecordPrefabInstancePropertyModifications(surfaces[index]);
            }
            var structure = new GameObject("Building Structure"); structure.transform.SetParent(root.transform, false);
            Material concrete = Material("M_Building", "Universal Render Pipeline/Lit", new Color(0.14f, 0.18f, 0.23f));
            Material interior = Material("M_Interior", "Universal Render Pipeline/Lit", new Color(0.14f, 0.35f, 0.48f));
            Cube("Interior Backing", structure.transform, new Vector3(0, 8.8f, 2), new Vector3(10.8f, 18, 0.2f), interior, true);
            Cube("Left Corner", structure.transform, new Vector3(-5.5f, 8.8f, 0.8f), new Vector3(0.35f, 18, 2.5f), concrete, true);
            Cube("Right Corner", structure.transform, new Vector3(5.5f, 8.8f, 0.8f), new Vector3(0.35f, 18, 2.5f), concrete, true);
            for (int floor = 0; floor <= Rows; floor++)
                Cube("Floor Band " + floor, structure.transform, new Vector3(0, 0.15f + floor * 2.9f, 1.1f),
                    new Vector3(11, 0.18f, 2f), concrete, true);
            var job = root.AddComponent<CleaningJob>(); SetArray(job, "surfaces", surfaces);
            return SaveNew(root, path);
        }

        private static GameObject CreatePlatform()
        {
            string path = Root + "Prefabs/Platforms/PF_CleaningPlatform.prefab";
            var existing = AssetDatabase.LoadAssetAtPath<GameObject>(path);
            if (existing) return existing;
            var root = new GameObject("PF_CleaningPlatform");
            var body = root.AddComponent<Rigidbody>(); body.isKinematic = true; body.useGravity = false;
            var lift = root.AddComponent<MovingPlatform>();
            Material deck = Material("M_Platform", "Universal Render Pipeline/Lit", new Color(0.26f, 0.29f, 0.31f));
            Material safety = Material("M_SafetyYellow", "Universal Render Pipeline/Lit", new Color(1, 0.65f, 0.08f));
            Cube("Rideable Deck", root.transform, new Vector3(0, 0.1f, 0), new Vector3(11.3f, 0.2f, 2.2f), deck, true);
            // Side rails leave the rear centre open for boarding and never block the glass-facing side.
            foreach (float side in new[] { -5.5f, 5.5f })
            {
                Cube("Side Rail", root.transform, new Vector3(side, 1, 0), new Vector3(0.07f, 0.08f, 2.1f), safety, true);
                Cube("Rail Post", root.transform, new Vector3(side, 0.55f, -0.9f), new Vector3(0.07f, 0.9f, 0.07f), safety, true);
            }
            var buttonRoot = new GameObject("Four State Button"); buttonRoot.transform.SetParent(root.transform, false);
            buttonRoot.transform.localPosition = new Vector3(0, 0, -0.65f);
            Cube("Pedestal", buttonRoot.transform, new Vector3(0, 0.65f, 0), new Vector3(0.22f, 0.9f, 0.22f), deck, true);
            Cube("Press E", buttonRoot.transform, new Vector3(0, 1.2f, -0.04f), new Vector3(0.5f, 0.3f, 0.18f), safety, true);
            var button = buttonRoot.AddComponent<PlatformButton>(); Set(button, "platform", lift);
            Text label = WorldLabel(buttonRoot.transform, "Lift Instructions", new Vector3(0, 1.67f, -0.16f), new Vector2(260, 65), 16);
            Set(button, "stateLabel", label);
            return SaveNew(root, path);
        }

        private static void ComposeScene(GameObject facadePrefab, GameObject platformPrefab)
        {
            var environment = new GameObject("Environment");
            Cube("Ground", environment.transform, new Vector3(0, -0.15f, 0), new Vector3(45, 0.3f, 40),
                Material("M_Floor", "Universal Render Pipeline/Lit", Color.gray), true);
            var sun = new GameObject("Sun"); sun.transform.SetParent(environment.transform);
            sun.transform.rotation = Quaternion.Euler(40, -35, 0);
            var light = sun.AddComponent<Light>(); light.type = LightType.Directional; light.intensity = 1.5f;
            RenderSettings.ambientMode = UnityEngine.Rendering.AmbientMode.Flat;
            RenderSettings.ambientLight = new Color(0.6f, 0.68f, 0.75f);
            var gameplay = new GameObject("Gameplay");
            var facade = (GameObject)PrefabUtility.InstantiatePrefab(facadePrefab, gameplay.transform);
            var platform = (GameObject)PrefabUtility.InstantiatePrefab(platformPrefab, gameplay.transform);
            platform.transform.position = new Vector3(0, 0, -1.5f);
            var player = (GameObject)PrefabUtility.InstantiatePrefab(Load("Prefabs/Player/PF_Player.prefab"), gameplay.transform);
            player.transform.position = new Vector3(-1.325f, 0.05f, -3.8f);
            var hud = (GameObject)PrefabUtility.InstantiatePrefab(Load("Prefabs/UI/PF_CleaningHUD.prefab"));
            var router = gameplay.AddComponent<CleaningActionRouter>(); Set(router, "job", facade.GetComponent<CleaningJob>());
            var bindings = gameplay.AddComponent<PrototypeBindings>();
            Set(bindings, "job", facade.GetComponent<CleaningJob>()); Set(bindings, "router", router);
            Set(bindings, "player", player.GetComponent<CleaningToolController>()); Set(bindings, "hud", hud.GetComponent<CleaningHUD>());
            Set(bindings, "buttonInteraction", player.GetComponent<PlayerButtonInteraction>());
            Set(bindings, "interactionPrompt", hud.transform.Find("Interaction Prompt").GetComponent<Text>());
        }

        private static Text WorldLabel(Transform parent, string name, Vector3 position, Vector2 size, int fontSize)
        {
            var canvas = new GameObject(name, typeof(RectTransform), typeof(Canvas));
            canvas.transform.SetParent(parent, false); canvas.transform.localPosition = position;
            canvas.transform.localScale = Vector3.one * 0.01f;
            canvas.GetComponent<Canvas>().renderMode = RenderMode.WorldSpace;
            canvas.GetComponent<RectTransform>().sizeDelta = size;
            var background = canvas.AddComponent<Image>(); background.color = new Color(0.025f, 0.045f, 0.065f, 0.9f); background.raycastTarget = false;
            Text text = Label("Label", canvas.transform, name, Vector2.zero, size, fontSize);
            text.rectTransform.anchorMin = text.rectTransform.anchorMax = text.rectTransform.pivot = Vector2.one * 0.5f;
            text.alignment = TextAnchor.MiddleCenter;
            return text;
        }
        private static T GetOrAdd<T>(GameObject root) where T : Component => root.GetComponent<T>() ? root.GetComponent<T>() : root.AddComponent<T>();
        private static GameObject Load(string path) => AssetDatabase.LoadAssetAtPath<GameObject>(Root + path);
        private static GameObject SaveNew(GameObject root, string path)
        {
            var result = PrefabUtility.SaveAsPrefabAsset(root, path);
            UnityEngine.Object.DestroyImmediate(root);
            return result;
        }
        internal static void Edit(UnityEngine.Object target, Action<SerializedObject> edit)
        {
            var serialized = new SerializedObject(target); edit(serialized); serialized.ApplyModifiedPropertiesWithoutUndo();
        }
        private static void SetArray(UnityEngine.Object target, string field, UnityEngine.Object[] values)
        {
            Edit(target, serialized => {
                var property = serialized.FindProperty(field); property.arraySize = values.Length;
                for (int index = 0; index < values.Length; index++) property.GetArrayElementAtIndex(index).objectReferenceValue = values[index];
            });
        }
    }
}
