using System;
using System.IO;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem.UI;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using Object = UnityEngine.Object;

namespace UiSystem
{
    public static class SafeAreaDemoBuilder
    {
        public const string ScenePath = "Assets/_Presentation/Scenes/SafeAreaDemo.unity";
        private const string SpritePath = "Assets/_Presentation/SafeAreaDemo/Background.png";
        private static readonly Color Ink = new Color32(23, 18, 19, 255);
        private static readonly Color Paper = new Color32(210, 206, 199, 255);
        private static readonly Color Red = new Color32(161, 26, 23, 255);

        [MenuItem("Dudester/Safe Area/Create demo scene")]
        public static void Build()
        {
            if (EditorApplication.isPlayingOrWillChangePlaymode) throw new InvalidOperationException("Stop Play Mode before creating the demo.");
            if (!Application.isBatchMode && !EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo()) return;
            Scene scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
            Sprite background = CreateBackground();
            Camera camera = new GameObject("Demo Camera", typeof(Camera)).GetComponent<Camera>();
            camera.clearFlags = CameraClearFlags.SolidColor;
            camera.backgroundColor = Ink;
            camera.cullingMask = 0;
            camera.orthographic = true;
            camera.tag = "MainCamera";
            GameObject canvasObject = new GameObject("Canvas", typeof(RectTransform), typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
            Canvas canvas = canvasObject.GetComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            CanvasScaler scaler = canvasObject.GetComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1080, 1920);
            scaler.screenMatchMode = CanvasScaler.ScreenMatchMode.MatchWidthOrHeight;
            scaler.matchWidthOrHeight = .5f;

            RectTransform backgroundContainer = Stretch("Background", canvas.transform);
            Image image = Stretch("Image", backgroundContainer).gameObject.AddComponent<Image>();
            image.sprite = background;
            image.preserveAspect = false;
            image.raycastTarget = false;

            RectTransform safe = Stretch("SafeAreaPanel", canvas.transform);
            safe.gameObject.AddComponent<SafeAreaFitter>();
            RectTransform top = Stretch("TopBar", safe);
            top.anchorMin = new Vector2(0, 1);
            top.anchorMax = Vector2.one;
            top.pivot = new Vector2(.5f, 1);
            top.offsetMin = new Vector2(28, -152);
            top.offsetMax = new Vector2(-28, -28);
            Image topImage = top.gameObject.AddComponent<Image>(); topImage.color = Ink; topImage.raycastTarget = false;
            Text title = Text("Title", top, "DUDESTER  /  SAFE AREA", 38, Paper);
            title.rectTransform.offsetMin = new Vector2(22, 12);
            title.rectTransform.offsetMax = new Vector2(-22, -12);

            CreateButton("LeftAction", safe, "Left Action", Vector2.zero, new Vector2(28, 28), Red, Paper);
            CreateButton("RightAction", safe, "Right Action", Vector2.right, new Vector2(-28, 28), Paper, Ink);
            GameObject events = new GameObject("EventSystem", typeof(EventSystem), typeof(InputSystemUIInputModule));
            events.GetComponent<InputSystemUIInputModule>().AssignDefaultActions();

            EditorSceneManager.MarkSceneDirty(scene);
            EditorSceneManager.SaveScene(scene, ScenePath);
            AssetDatabase.SaveAssets();
            Validate();
            Debug.Log("SAFE_AREA_DEMO_SAVED: " + ScenePath);
        }

        [MenuItem("Dudester/Safe Area/Validate demo scene")]
        public static void Validate()
        {
            if (SceneManager.GetActiveScene().path != ScenePath) throw new InvalidOperationException("Open SafeAreaDemo before validation.");
            Canvas canvas = Object.FindFirstObjectByType<Canvas>();
            CanvasScaler scaler = canvas.GetComponent<CanvasScaler>();
            Require(canvas.renderMode == RenderMode.ScreenSpaceOverlay, "Canvas must be Overlay.");
            Require(scaler.uiScaleMode == CanvasScaler.ScaleMode.ScaleWithScreenSize && scaler.referenceResolution == new Vector2(1080, 1920) && Mathf.Approximately(scaler.matchWidthOrHeight, .5f), "CanvasScaler settings differ from the task.");
            Transform safe = canvas.transform.Find("SafeAreaPanel");
            Require(safe != null && safe.GetComponent<SafeAreaFitter>() != null, "SafeAreaPanel / SafeAreaFitter is missing.");
            foreach (string name in new[] { "TopBar", "LeftAction", "RightAction" }) Require(safe.Find(name) != null, "Missing HUD: " + name);
            RectTransform background = (RectTransform)canvas.transform.Find("Background");
            Require(background.anchorMin == Vector2.zero && background.anchorMax == Vector2.one && background.offsetMin == Vector2.zero && background.offsetMax == Vector2.zero, "Background must fill the screen.");
            Image image = background.GetComponentInChildren<Image>();
            Require(image.sprite != null && !image.raycastTarget, "Background sprite / raycast configuration is invalid.");
            Require(Object.FindFirstObjectByType<EventSystem>().GetComponent<InputSystemUIInputModule>() != null, "New Input System UI module required.");
            foreach (GameObject root in SceneManager.GetActiveScene().GetRootGameObjects())
                foreach (Transform child in root.GetComponentsInChildren<Transform>(true))
                    Require(GameObjectUtility.GetMonoBehavioursWithMissingScriptCount(child.gameObject) == 0, "Missing script on " + child.name);
            Debug.Log("SAFE_AREA_DEMO_VALIDATED");
        }

        private static void Require(bool value, string message) { if (!value) throw new InvalidOperationException(message); }
        private static RectTransform Stretch(string name, Transform parent)
        {
            var rect = (RectTransform)new GameObject(name, typeof(RectTransform)).transform;
            rect.SetParent(parent, false);
            rect.anchorMin = Vector2.zero; rect.anchorMax = Vector2.one;
            rect.offsetMin = rect.offsetMax = Vector2.zero;
            return rect;
        }
        private static Text Text(string name, Transform parent, string text, int size, Color color)
        {
            Text label = Stretch(name, parent).gameObject.AddComponent<Text>();
            label.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            label.text = text; label.fontSize = size; label.fontStyle = FontStyle.Bold;
            label.color = color; label.alignment = TextAnchor.MiddleCenter; label.raycastTarget = false;
            return label;
        }
        private static void CreateButton(string name, Transform parent, string label, Vector2 anchor, Vector2 margin, Color fill, Color textColor)
        {
            RectTransform rect = Stretch(name, parent);
            rect.anchorMin = rect.anchorMax = anchor; rect.pivot = anchor;
            rect.sizeDelta = new Vector2(280, 116); rect.anchoredPosition = margin;
            Image image = rect.gameObject.AddComponent<Image>(); image.color = fill;
            Button button = rect.gameObject.AddComponent<Button>(); button.targetGraphic = image;
            Text("Label", rect, label, 34, textColor);
        }
        private static Sprite CreateBackground()
        {
            Directory.CreateDirectory(Path.GetDirectoryName(SpritePath));
            var texture = new Texture2D(512, 512, TextureFormat.RGB24, false);
            for (int y = 0; y < texture.height; y++) for (int x = 0; x < texture.width; x++)
            {
                float edge = Vector2.Distance(new Vector2(x, y), new Vector2(256, 256)) / 362;
                Color color = Color.Lerp(new Color32(61, 56, 55, 255), Ink, edge * .65f);
                if (x % 64 == 0 || y % 64 == 0) color = Color.Lerp(color, Paper, .07f);
                // A broad diagonal red band remains visible beneath any cutouts.
                float band = Mathf.Abs((x - y) - 80);
                if (band < 50) color = Color.Lerp(color, Red, .5f * (1 - band / 50));
                texture.SetPixel(x, y, color);
            }
            texture.Apply();
            File.WriteAllBytes(SpritePath, texture.EncodeToPNG());
            Object.DestroyImmediate(texture);
            AssetDatabase.ImportAsset(SpritePath, ImportAssetOptions.ForceSynchronousImport);
            var importer = (TextureImporter)AssetImporter.GetAtPath(SpritePath);
            importer.textureType = TextureImporterType.Sprite;
            importer.spriteImportMode = SpriteImportMode.Single;
            importer.mipmapEnabled = false;
            importer.textureCompression = TextureImporterCompression.Uncompressed;
            importer.SaveAndReimport();
            return AssetDatabase.LoadAssetAtPath<Sprite>(SpritePath);
        }
    }
}
