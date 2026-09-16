using System;
using System.IO;
using Sandouq.Cleaning;
using Sandouq.Core;
using Sandouq.Dirt;
using Sandouq.Glass;
using Sandouq.Player;
using Sandouq.Tools;
using Sandouq.UI;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace Sandouq.Editor
{
    public static class PrototypeBuilder
    {
        private const string Root = "Assets/Sandouq/";
        [MenuItem("Sandouq/Create Missing Prototype Assets")]
        public static void Build()
        {
            string scenePath = Root + "Scenes/Prototype/Cleaning_Prototype.unity";
            if (File.Exists(scenePath)) { Debug.Log("Prototype already exists. Existing assets were preserved."); return; }
            AssetDatabase.Refresh();
            Scene previousScene = SceneManager.GetActiveScene();
            if (!Application.isBatchMode && string.IsNullOrEmpty(previousScene.path))
            {
                Debug.LogWarning("Save the current untitled scene before creating prototype assets, then run this command again.");
                return;
            }
            Scene scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene,
                Application.isBatchMode ? NewSceneMode.Single : NewSceneMode.Additive);
            SceneManager.SetActiveScene(scene);
            try
            {
                // Load data after scene creation, which may unload unused ScriptableObjects.
                var dirt = Asset<DirtType>("ScriptableObjects/Dirt/Basic_Dirt.asset");
                var toolData = Asset<CleaningToolData>("ScriptableObjects/Tools/Squeegee_Default.asset");
                Material glass = Material("M_DirtyGlass", "Sandouq/DirtyGlass", Color.white);
                Material frame = Material("M_Frame", "Universal Render Pipeline/Lit", new Color(0.08f, 0.12f, 0.16f));
                Material floor = Material("M_Floor", "Universal Render Pipeline/Lit", new Color(0.22f, 0.29f, 0.32f));
                Material toolMaterial = Material("M_Squeegee", "Universal Render Pipeline/Lit", new Color(0.1f, 0.8f, 0.7f));
                Material skyline = Material("M_Skyline", "Universal Render Pipeline/Lit", new Color(0.25f, 0.43f, 0.57f));
                GameObject toolPrefab = BuildTool(toolData, toolMaterial, frame);
                GameObject playerPrefab = BuildPlayer(toolPrefab, toolMaterial);
                GameObject glassPrefab = BuildGlass(dirt, glass, frame);
                GameObject hudPrefab = BuildHUD();
                var environment = new GameObject("Environment");
                Cube("Floor", environment.transform, new Vector3(0, -0.2f, 0), new Vector3(20, 0.4f, 20), floor, true);
                for (int index = 0; index < 7; index++)
                    Cube("Skyline " + (index + 1), environment.transform, new Vector3((index - 3) * 2.6f, 1 + index % 3, 8),
                        new Vector3(1.8f, 2 + (index % 3) * 2, 2), skyline, false);
                var lighting = new GameObject("Lighting");
                lighting.transform.SetParent(environment.transform);
                lighting.transform.rotation = Quaternion.Euler(45, -35, 0);
                Light light = lighting.AddComponent<Light>(); light.type = LightType.Directional; light.intensity = 1.8f;
                RenderSettings.ambientMode = UnityEngine.Rendering.AmbientMode.Flat;
                RenderSettings.ambientLight = new Color(0.55f, 0.65f, 0.75f);
                var gameplay = new GameObject("Gameplay");
                GameObject player = (GameObject)PrefabUtility.InstantiatePrefab(playerPrefab, gameplay.transform);
                player.transform.position = new Vector3(0, 0.05f, -1.65f);
                GameObject wall = (GameObject)PrefabUtility.InstantiatePrefab(glassPrefab, gameplay.transform);
                var uiRoot = new GameObject("UI");
                GameObject hud = (GameObject)PrefabUtility.InstantiatePrefab(hudPrefab, uiRoot.transform);
                var bindings = gameplay.AddComponent<PrototypeBindings>();
                Set(bindings, "surface", wall.GetComponentInChildren<GlassSurface>());
                Set(bindings, "player", player.GetComponent<CleaningToolController>());
                Set(bindings, "hud", hud.GetComponent<CleaningHUD>());
                EditorSceneManager.SaveScene(scene, scenePath);
                AssetDatabase.SaveAssets();
                Validate(scene);
            }
            finally
            {
                if (!Application.isBatchMode)
                {
                    EditorSceneManager.CloseScene(scene, true);
                    if (previousScene.IsValid()) SceneManager.SetActiveScene(previousScene);
                }
            }
        }

        private static GameObject BuildTool(CleaningToolData data, Material handle, Material rubber)
        {
            var root = new GameObject("PF_Squeegee");
            Cube("Handle", root.transform, new Vector3(0, -0.2f, 0), new Vector3(0.055f, 0.4f, 0.055f), handle, false);
            Cube("Rubber Blade", root.transform, Vector3.zero, new Vector3(0.48f, 0.065f, 0.04f), rubber, false);
            var point = new GameObject("CleaningPoint"); point.transform.SetParent(root.transform, false);
            var tool = root.AddComponent<CleaningTool>(); Set(tool, "data", data); Set(tool, "cleaningPoint", point.transform);
            return Save(root, "Prefabs/Tools/PF_Squeegee.prefab");
        }

        private static GameObject BuildPlayer(GameObject toolPrefab, Material material)
        {
            var root = new GameObject("PF_Player"); root.layer = 2;
            var controller = root.AddComponent<CharacterController>(); controller.height = 1.8f;
            controller.minMoveDistance = 0;
            controller.radius = 0.3f; controller.center = new Vector3(0, 0.9f, 0); controller.stepOffset = 0.3f;
            var input = root.AddComponent<PlayerInputReader>();
            var movement = root.AddComponent<PlayerMovement>(); Set(movement, "input", input);
            var pivot = new GameObject("Camera Root"); pivot.transform.SetParent(root.transform, false); pivot.transform.localPosition = new Vector3(0, 1.6f, 0);
            var cameraObject = new GameObject("Camera"); cameraObject.tag = "MainCamera"; cameraObject.transform.SetParent(pivot.transform, false);
            Camera camera = cameraObject.AddComponent<Camera>(); camera.nearClipPlane = 0.03f; camera.fieldOfView = 70;
            camera.clearFlags = CameraClearFlags.SolidColor; camera.backgroundColor = new Color(0.55f, 0.76f, 0.86f);
            cameraObject.AddComponent<AudioListener>();
            GameObject body = GameObject.CreatePrimitive(PrimitiveType.Capsule); body.name = "Player Body";
            body.layer = 2; body.transform.SetParent(root.transform, false); body.transform.localPosition = new Vector3(0, 0.9f, 0);
            body.transform.localScale = new Vector3(0.5f, 0.85f, 0.5f); UnityEngine.Object.DestroyImmediate(body.GetComponent<Collider>());
            body.GetComponent<Renderer>().sharedMaterial = material;
            var cameraController = root.AddComponent<PlayerCameraController>(); Set(cameraController, "input", input);
            Set(cameraController, "cameraPivot", pivot.transform); Set(cameraController, "viewCamera", camera); Set(cameraController, "body", body.GetComponent<Renderer>());
            var hand = new GameObject("Hand Origin"); hand.transform.SetParent(pivot.transform, false); hand.transform.localPosition = new Vector3(0.3f, -0.25f, 0.2f);
            var interaction = root.AddComponent<PlayerInteraction>(); Set(interaction, "cameraController", cameraController); Set(interaction, "handOrigin", hand.transform);
            GameObject toolObject = (GameObject)PrefabUtility.InstantiatePrefab(toolPrefab, hand.transform);
            toolObject.transform.localPosition = new Vector3(0, 0, 0.4f);
            var cleaning = root.AddComponent<CleaningToolController>(); Set(cleaning, "input", input); Set(cleaning, "interaction", interaction);
            Set(cleaning, "tool", toolObject.GetComponent<CleaningTool>());
            return Save(root, "Prefabs/Player/PF_Player.prefab");
        }

        private static GameObject BuildGlass(DirtType dirt, Material glass, Material frame)
        {
            var root = new GameObject("PF_GlassWall");
            var surface = GameObject.CreatePrimitive(PrimitiveType.Quad); surface.name = "Glass Surface";
            surface.transform.SetParent(root.transform, false); surface.transform.localPosition = new Vector3(0, 1.5f, 0);
            surface.transform.localScale = new Vector3(6, 3, 1); surface.GetComponent<Renderer>().sharedMaterial = glass;
            // Unity's Quad has UVs and faces -Z, toward the starting player.
            var dirtObject = new GameObject("Dirt System"); dirtObject.transform.SetParent(surface.transform, false);
            var mask = dirtObject.AddComponent<DirtMask>(); Set(mask, "dirt", dirt); Set(mask, "brushShader", Shader.Find("Hidden/Sandouq/DirtBrush"));
            Set(mask, "dirtRenderer", surface.GetComponent<Renderer>());
            var cleanable = surface.AddComponent<GlassSurface>(); Set(cleanable, "dirtMask", mask);
            Cube("Top Frame", root.transform, new Vector3(0, 3.06f, 0), new Vector3(6.2f, 0.12f, 0.15f), frame, true);
            Cube("Bottom Frame", root.transform, new Vector3(0, 0.03f, 0), new Vector3(6.2f, 0.06f, 0.15f), frame, true);
            Cube("Left Frame", root.transform, new Vector3(-3.06f, 1.5f, 0), new Vector3(0.12f, 3, 0.15f), frame, true);
            Cube("Right Frame", root.transform, new Vector3(3.06f, 1.5f, 0), new Vector3(0.12f, 3, 0.15f), frame, true);
            Cube("Glass Collision", root.transform, new Vector3(0, 1.5f, 0.045f), new Vector3(6, 3, 0.05f), frame, true).GetComponent<Renderer>().enabled = false;
            return Save(root, "Prefabs/Glass/PF_GlassWall.prefab");
        }

        private static GameObject BuildHUD()
        {
            var root = new GameObject("PF_CleaningHUD", typeof(RectTransform), typeof(Canvas), typeof(CanvasScaler));
            root.GetComponent<Canvas>().renderMode = RenderMode.ScreenSpaceOverlay;
            var scaler = root.GetComponent<CanvasScaler>(); scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1280, 720); scaler.matchWidthOrHeight = 0.5f;
            Text progress = Label("Progress", root.transform, "Glass Clean: 0%", new Vector2(28, -25), new Vector2(700, 45), 28);
            Text tool = Label("Current Tool", root.transform, "Tool: Squeegee", new Vector2(28, -72), new Vector2(700, 35), 22);
            Label("Controls", root.transform, "WASD / left stick: move   |   Mouse / right stick: look\nHold LMB / RT: wipe   |   V / right stick click: camera\nShift / left stick click: sprint   |   Space / A: jump   |   Esc / Start: cursor",
                new Vector2(28, -115), new Vector2(1100, 95), 18);
            Text crosshair = Label("Crosshair", root.transform, "+", Vector2.zero, new Vector2(30, 30), 24);
            crosshair.alignment = TextAnchor.MiddleCenter;
            crosshair.rectTransform.anchorMin = crosshair.rectTransform.anchorMax = crosshair.rectTransform.pivot = new Vector2(0.5f, 0.5f);
            var hud = root.AddComponent<CleaningHUD>(); Set(hud, "progressLabel", progress); Set(hud, "toolLabel", tool);
            return Save(root, "Prefabs/UI/PF_CleaningHUD.prefab");
        }

        internal static Text Label(string name, Transform parent, string value, Vector2 position, Vector2 size, int fontSize)
        {
            var obj = new GameObject(name, typeof(RectTransform), typeof(Text), typeof(Shadow)); obj.transform.SetParent(parent, false);
            var text = obj.GetComponent<Text>(); text.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            text.text = value; text.fontSize = fontSize; text.color = Color.white; text.raycastTarget = false;
            text.rectTransform.anchorMin = text.rectTransform.anchorMax = text.rectTransform.pivot = new Vector2(0, 1);
            text.rectTransform.anchoredPosition = position; text.rectTransform.sizeDelta = size;
            return text;
        }
        internal static GameObject Cube(string name, Transform parent, Vector3 position, Vector3 scale, Material material, bool collision)
        {
            var cube = GameObject.CreatePrimitive(PrimitiveType.Cube); cube.name = name; cube.transform.SetParent(parent, false);
            cube.transform.localPosition = position; cube.transform.localScale = scale; cube.GetComponent<Renderer>().sharedMaterial = material;
            if (!collision) UnityEngine.Object.DestroyImmediate(cube.GetComponent<Collider>());
            return cube;
        }
        private static T Asset<T>(string path) where T : ScriptableObject
        {
            T asset = AssetDatabase.LoadAssetAtPath<T>(Root + path);
            if (!asset) { asset = ScriptableObject.CreateInstance<T>(); AssetDatabase.CreateAsset(asset, Root + path); }
            return asset;
        }
        internal static Material Material(string name, string shaderName, Color color)
        {
            string path = Root + "Materials/" + name + ".mat";
            var material = AssetDatabase.LoadAssetAtPath<Material>(path);
            if (material) return material;
            Shader shader = Shader.Find(shaderName);
            if (!shader) throw new InvalidOperationException("Missing shader: " + shaderName);
            material = new Material(shader); material.name = name;
            if (material.HasProperty("_BaseColor")) material.SetColor("_BaseColor", color);
            AssetDatabase.CreateAsset(material, path); return material;
        }
        private static GameObject Save(GameObject instance, string path)
        {
            GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>(Root + path);
            if (!prefab) prefab = PrefabUtility.SaveAsPrefabAsset(instance, Root + path);
            UnityEngine.Object.DestroyImmediate(instance); return prefab;
        }
        internal static void Set(UnityEngine.Object target, string property, UnityEngine.Object value)
        {
            if (!value) throw new InvalidOperationException("Missing reference for " + target.name + "." + property);
            var serialized = new SerializedObject(target); var field = serialized.FindProperty(property);
            if (field == null) throw new InvalidOperationException("Missing serialized field " + property);
            field.objectReferenceValue = value; serialized.ApplyModifiedPropertiesWithoutUndo();
        }
        private static void Validate(Scene scene)
        {
            foreach (GameObject root in scene.GetRootGameObjects())
                foreach (Transform child in root.GetComponentsInChildren<Transform>(true))
                    if (GameObjectUtility.GetMonoBehavioursWithMissingScriptCount(child.gameObject) > 0)
                        throw new InvalidOperationException("Missing script on " + child.name);
            foreach (string path in new[] { "Prefabs/Player/PF_Player.prefab", "Prefabs/Glass/PF_GlassWall.prefab", "Prefabs/Tools/PF_Squeegee.prefab", "Prefabs/UI/PF_CleaningHUD.prefab" })
                if (!AssetDatabase.LoadAssetAtPath<GameObject>(Root + path)) throw new InvalidOperationException("Missing prefab " + path);
            Debug.Log("SANDOUQ_BUILD_VALIDATED: Scene and four prefabs created; no missing scripts.");
        }
    }
}
