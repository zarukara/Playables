using System;
using System.Collections.Generic;
using System.IO;
using AudioSystem;
using CombatSystem;
using EnemySystem;
using InputSystem;
using PlayerSystem;
using UiSystem;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem.UI;
using UnityEngine.Rendering;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using Object = UnityEngine.Object;

namespace CoreSystem
{
    public static class PrototypeBuilder
    {
        public const string ScenePath = "Assets/_Presentation/Scenes/Main.unity";
        private const string Root = "Assets/_Presentation/";
        private static Material floor, grid, wall, dark, mint, cream, orange, red, yellow, shadow, particle;
        private static Font font;
        private static Sprite circle, white;
        private static readonly Color Ink = new Color(.037f, .073f, .105f, .97f);
        private static readonly Color Muted = new Color(.54f, .68f, .73f);
        private static readonly Color Accent = new Color(.37f, .96f, .77f);

        [MenuItem("Dudester/Generate prototype assets and Main scene")]
        public static void Generate()
        {
            if (!Application.isBatchMode && !EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo()) return;
            Scene scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
            foreach (string folder in new[] { "Scenes", "Materials", "Prefabs", "Audio", "VFX", "Meshes", "UI" })
                Directory.CreateDirectory(Root + folder);
            AssetDatabase.Refresh();
            // The source URP assets and packages are retained. Only pipeline selection changes.
            GraphicsSettings.defaultRenderPipeline = null;
            int quality = QualitySettings.GetQualityLevel();
            for (int i = 0; i < QualitySettings.names.Length; i++)
            { QualitySettings.SetQualityLevel(i, false); QualitySettings.renderPipeline = null; QualitySettings.shadows = ShadowQuality.Disable; QualitySettings.antiAliasing = 2; QualitySettings.vSyncCount = 0; }
            QualitySettings.SetQualityLevel(quality, false);
            CreateMaterials();
            circle = CreateSprite("Circle", true);
            white = CreateSprite("White", false);
            font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            AudioClip[] clips = CreateAudio();
            EnemyController enemyPrefab = BuildEnemy();
            Projectile projectilePrefab = BuildProjectile();
            AutoWeapon weaponPrefab = BuildWeapon();
            PlayerController playerPrefab = BuildPlayer(weaponPrefab);
            ParticleSystem hit = BuildEffect("HitBurst", new Color(.95f, .07f, .12f), 9, 2.8f, .14f);
            ParticleSystem death = BuildEffect("DeathBurst", new Color(1, .22f, .13f), 16, 3.8f, .21f);
            ParticleSystem spawn = BuildEffect("SpawnBurst", new Color(1, .57f, .27f), 10, 1.2f, .13f);
            RenderSettings.ambientMode = AmbientMode.Flat;
            RenderSettings.ambientLight = new Color(.68f, .76f, .82f);
            RenderSettings.fog = false;
            RenderSettings.skybox = null;
            GameObject arena = BuildArena();
            CombineArena(arena);
            GameObject cameraObject = new GameObject("Main Camera", typeof(Camera), typeof(AudioListener), typeof(ArenaCamera));
            cameraObject.tag = "MainCamera";
            cameraObject.transform.position = new Vector3(0, 26, -18.2f);
            cameraObject.transform.rotation = Quaternion.Euler(55, 0, 0);
            Camera camera = cameraObject.GetComponent<Camera>();
            camera.orthographic = true; camera.orthographicSize = 13.2f;
            camera.clearFlags = CameraClearFlags.SolidColor; camera.backgroundColor = new Color(.025f, .046f, .063f);
            camera.nearClipPlane = .1f; camera.farClipPlane = 100; camera.allowHDR = false; camera.allowMSAA = true;
            Light light = new GameObject("Directional Light", typeof(Light)).GetComponent<Light>();
            light.type = LightType.Directional; light.intensity = 1.05f; light.color = new Color(1, .93f, .81f);
            light.transform.rotation = Quaternion.Euler(48, -32, 0); light.shadows = LightShadows.None;
            PlayerController player = ((GameObject)PrefabUtility.InstantiatePrefab(playerPrefab.gameObject)).GetComponent<PlayerController>();
            player.name = "Player";
            PlayerInputReader input = new GameObject("Player Input", typeof(PlayerInputReader)).GetComponent<PlayerInputReader>();
            EnemySpawner enemies = new GameObject("EnemySpawner", typeof(EnemySpawner)).GetComponent<EnemySpawner>(); enemies.Configure(enemyPrefab);
            ProjectileSystem projectiles = new GameObject("Projectile Pool", typeof(ProjectileSystem)).GetComponent<ProjectileSystem>(); projectiles.Configure(projectilePrefab);
            VfxService effects = new GameObject("VFX Pools", typeof(VfxService)).GetComponent<VfxService>(); effects.Configure(hit, death, spawn);
            AudioService audio = BuildAudioService(clips);
            GameSession session = new GameObject("GameSession", typeof(GameSession)).GetComponent<GameSession>();
            session.Configure(player, input, enemies, player.GetComponentInChildren<AutoWeapon>(), projectiles, effects, audio, cameraObject.GetComponent<ArenaCamera>());
            BuildUi(session, input);
            GameObject eventSystem = new GameObject("EventSystem", typeof(EventSystem), typeof(InputSystemUIInputModule));
            eventSystem.GetComponent<InputSystemUIInputModule>().AssignDefaultActions();
            EditorSceneManager.MarkSceneDirty(scene);
            EditorSceneManager.SaveScene(scene, ScenePath);
            var scenes = new List<EditorBuildSettingsScene> { new EditorBuildSettingsScene(ScenePath, true) };
            foreach (EditorBuildSettingsScene existing in EditorBuildSettings.scenes)
                if (existing.path != ScenePath) scenes.Add(new EditorBuildSettingsScene(existing.path, false));
            EditorBuildSettings.scenes = scenes.ToArray();
            AssetDatabase.SaveAssets();
            Validate();
            Debug.Log("DUDESTER_GENERATION_OK: Main scene, linked prefabs, materials, WAV clips and VFX saved.");
        }

        private static Material Material(string name, Color color, bool unlit = false)
        {
            string path = Root + "Materials/" + name + ".mat";
            Material value = AssetDatabase.LoadAssetAtPath<Material>(path);
            if (value == null) { value = new Material(Shader.Find(unlit ? "Unlit/Color" : "Standard")); AssetDatabase.CreateAsset(value, path); }
            value.shader = Shader.Find(unlit ? "Unlit/Color" : "Standard");
            value.color = color; value.enableInstancing = true;
            if (value.HasProperty("_Glossiness")) value.SetFloat("_Glossiness", .12f);
            EditorUtility.SetDirty(value);
            return value;
        }
        private static void CreateMaterials()
        {
            floor = Material("Arena - petrol", new Color(.12f, .22f, .26f));
            grid = Material("Arena - grid", new Color(.17f, .29f, .32f));
            wall = Material("Arena - slate", new Color(.19f, .32f, .37f));
            dark = Material("Graphite", new Color(.035f, .065f, .08f));
            mint = Material("Signal - mint", Accent);
            cream = Material("Hero - porcelain", new Color(.93f, .94f, .84f));
            orange = Material("Enemy - coral", new Color(1, .34f, .18f));
            red = Material("Enemy - visor", new Color(.42f, .045f, .055f));
            yellow = Material("Shot - warm light", new Color(1, .87f, .39f), true);
            shadow = Material("Contact shadows", new Color(.055f, .12f, .15f), true);
            particle = Material("Particles", Color.white, true);
            particle.shader = Shader.Find("Particles/Standard Unlit");
            particle.SetFloat("_Mode", 0);
            particle.SetFloat("_ZWrite", 0);
            particle.SetInt("_Cull", 0);
            EditorUtility.SetDirty(particle);
        }
        private static GameObject Primitive(string name, PrimitiveType type, Transform parent, Vector3 position, Vector3 scale, Material material)
        {
            GameObject go = GameObject.CreatePrimitive(type); go.name = name;
            go.transform.SetParent(parent, false); go.transform.localPosition = position; go.transform.localScale = scale;
            Object.DestroyImmediate(go.GetComponent<Collider>());
            Renderer renderer = go.GetComponent<Renderer>(); renderer.sharedMaterial = material;
            renderer.shadowCastingMode = ShadowCastingMode.Off; renderer.receiveShadows = false;
            return go;
        }
        private static GameObject Box(string name, Transform parent, Vector3 position, Vector3 scale, Material material) => Primitive(name, PrimitiveType.Cube, parent, position, scale, material);
        private static GameObject Group(string name, Transform parent)
        { GameObject go = new GameObject(name); go.transform.SetParent(parent, false); return go; }
        private static T SavePrefab<T>(GameObject go, string name, string folder = "Prefabs") where T : Component
        {
            GameObject saved = PrefabUtility.SaveAsPrefabAsset(go, Root + folder + "/" + name + ".prefab");
            Object.DestroyImmediate(go); return saved.GetComponent<T>();
        }
        private static AutoWeapon BuildWeapon()
        {
            GameObject root = new GameObject("Weapon");
            AutoWeapon weapon = root.AddComponent<AutoWeapon>();
            Transform model = Group("Recoil model", root.transform).transform;
            Box("Receiver", model, new Vector3(.34f, .08f, .35f), new Vector3(.32f, .26f, .68f), dark);
            Box("Mint rail", model, new Vector3(.34f, .23f, .28f), new Vector3(.18f, .08f, .42f), mint);
            Box("Barrel", model, new Vector3(.34f, .08f, .78f), new Vector3(.18f, .18f, .4f), cream);
            Transform muzzle = Group("Muzzle", model).transform; muzzle.localPosition = new Vector3(.34f, .08f, 1.02f);
            GameObject flash = Primitive("Muzzle flash", PrimitiveType.Sphere, muzzle, Vector3.zero, new Vector3(.3f, .3f, .45f), yellow);
            flash.SetActive(false); weapon.Configure(model, muzzle, flash);
            return SavePrefab<AutoWeapon>(root, "Weapon");
        }
        private static PlayerController BuildPlayer(AutoWeapon weaponPrefab)
        {
            GameObject root = new GameObject("Player");
            PlayerController player = root.AddComponent<PlayerController>();
            Primitive("Contact shadow", PrimitiveType.Cylinder, root.transform, new Vector3(0, .025f, 0), new Vector3(1.12f, .01f, 1.12f), shadow);
            Transform model = Group("Character", root.transform).transform;
            Renderer body = Primitive("Capsule", PrimitiveType.Capsule, model, new Vector3(0, .76f, 0), new Vector3(.78f, .69f, .78f), cream).GetComponent<Renderer>();
            Primitive("Helmet", PrimitiveType.Sphere, model, new Vector3(0, 1.19f, -.015f), new Vector3(.84f, .44f, .81f), mint);
            Box("Visor", model, new Vector3(0, 1.01f, .34f), new Vector3(.56f, .24f, .15f), dark);
            for (int sign = -1; sign <= 1; sign += 2)
                Box("Eye", model, new Vector3(sign * .14f, 1.03f, .427f), new Vector3(.09f, .1f, .035f), cream);
            Box("Backpack", model, new Vector3(0, .72f, -.37f), new Vector3(.5f, .55f, .25f), dark);
            DamageFlash flash = root.AddComponent<DamageFlash>(); flash.Configure(new[] { body }); player.Configure(model, flash);
            GameObject weapon = (GameObject)PrefabUtility.InstantiatePrefab(weaponPrefab.gameObject); weapon.transform.SetParent(root.transform, false); weapon.transform.localPosition = new Vector3(0, .8f, 0);
            return SavePrefab<PlayerController>(root, "Player");
        }
        private static EnemyController BuildEnemy()
        {
            GameObject root = new GameObject("Enemy"); EnemyController enemy = root.AddComponent<EnemyController>();
            Transform model = Group("Creature", root.transform).transform;
            Primitive("Contact shadow", PrimitiveType.Cylinder, model, new Vector3(0, .02f, 0), new Vector3(1.1f, .01f, 1.1f), shadow);
            Renderer body = Primitive("Body", PrimitiveType.Sphere, model, new Vector3(0, .57f, 0), new Vector3(.97f, 1.1f, .87f), orange).GetComponent<Renderer>();
            Box("Brow", model, new Vector3(0, .75f, .34f), new Vector3(.76f, .19f, .19f), red);
            for (int sign = -1; sign <= 1; sign += 2)
            {
                Box("Eye", model, new Vector3(sign * .2f, .73f, .45f), new Vector3(.14f, .09f, .04f), yellow);
                GameObject horn = Box("Horn", model, new Vector3(sign * .36f, 1.06f, 0), new Vector3(.18f, .4f, .2f), cream);
                horn.transform.localRotation = Quaternion.Euler(0, 0, -sign * 23);
            }
            DamageFlash flash = root.AddComponent<DamageFlash>(); flash.Configure(new[] { body }); enemy.Configure(model, flash);
            return SavePrefab<EnemyController>(root, "Enemy");
        }
        private static Projectile BuildProjectile()
        {
            GameObject root = Primitive("Projectile", PrimitiveType.Sphere, null, Vector3.zero, Vector3.one * .23f, yellow);
            Projectile projectile = root.AddComponent<Projectile>();
            TrailRenderer trail = root.AddComponent<TrailRenderer>(); trail.sharedMaterial = yellow;
            trail.time = .085f; trail.startWidth = .12f; trail.endWidth = 0; trail.minVertexDistance = .12f;
            trail.shadowCastingMode = ShadowCastingMode.Off; trail.receiveShadows = false;
            projectile.Configure(trail); return SavePrefab<Projectile>(root, "Projectile");
        }
        private static ParticleSystem BuildEffect(string name, Color color, short count, float speed, float size)
        {
            GameObject root = new GameObject(name); ParticleSystem effect = root.AddComponent<ParticleSystem>();
            effect.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
            var main = effect.main; main.duration = .5f; main.loop = false; main.playOnAwake = false;
            main.startLifetime = new ParticleSystem.MinMaxCurve(.18f, .45f); main.startSpeed = new ParticleSystem.MinMaxCurve(speed * .4f, speed);
            main.startSize = new ParticleSystem.MinMaxCurve(size * .5f, size); main.startColor = color; main.gravityModifier = .65f;
            main.maxParticles = 24; main.simulationSpace = ParticleSystemSimulationSpace.World;
            var emission = effect.emission; emission.rateOverTime = 0; emission.SetBursts(new[] { new ParticleSystem.Burst(0, count) });
            var shape = effect.shape; shape.shapeType = ParticleSystemShapeType.Sphere; shape.radius = .15f;
            var sizeOverLife = effect.sizeOverLifetime; sizeOverLife.enabled = true; sizeOverLife.size = new ParticleSystem.MinMaxCurve(1, AnimationCurve.Linear(0, 1, 1, 0));
            var renderer = effect.GetComponent<ParticleSystemRenderer>(); renderer.sharedMaterial = particle;
            renderer.renderMode = ParticleSystemRenderMode.Billboard; renderer.shadowCastingMode = ShadowCastingMode.Off; renderer.receiveShadows = false;
            return SavePrefab<ParticleSystem>(root, name, "VFX");
        }
        private static GameObject BuildArena()
        {
            GameObject arena = new GameObject("Arena"); Transform root = arena.transform;
            Box("Foundation", root, new Vector3(0, -.5f, 0), new Vector3(23.1f, .65f, 23.1f), dark);
            Box("Floor 22 x 22", root, new Vector3(0, -.15f, 0), new Vector3(22, .3f, 22), floor);
            for (int i = -10; i <= 10; i += 2)
            {
                Box("Grid X", root, new Vector3(i, .006f, 0), new Vector3(.025f, .012f, 21.6f), grid);
                Box("Grid Z", root, new Vector3(0, .006f, i), new Vector3(21.6f, .012f, .025f), grid);
            }
            for (int side = 0; side < 4; side++)
            {
                Transform edge = Group("Boundary " + side, root).transform; edge.rotation = Quaternion.Euler(0, side * 90, 0);
                Box("Low wall", edge, new Vector3(0, .18f, 11.15f), new Vector3(22.6f, .58f, .32f), wall);
                Box("Light strip", edge, new Vector3(0, .49f, 11.15f), new Vector3(21.9f, .055f, .15f), mint);
                for (int i = -9; i <= 9; i += 3)
                {
                    Box("Wall rib", edge, new Vector3(i, .26f, 11.18f), new Vector3(.3f, .78f, .6f), dark);
                    Box("Floor edge dash", edge, new Vector3(i, .015f, 10.35f), new Vector3(.95f, .018f, .12f), mint);
                }
            }
            for (int x = -1; x <= 1; x += 2) for (int z = -1; z <= 1; z += 2)
            {
                Box("Corner plinth", root, new Vector3(x * 11.2f, .22f, z * 11.2f), new Vector3(1.1f, .9f, 1.1f), dark);
                Box("Corner lamp", root, new Vector3(x * 11.2f, .72f, z * 11.2f), new Vector3(.7f, .12f, .7f), mint);
            }
            Transform logo = Group("Center insignia", root).transform; logo.rotation = Quaternion.Euler(0, 45, 0);
            for (int sign = -1; sign <= 1; sign += 2)
            {
                Box("Diamond", logo, new Vector3(sign * 1.9f, .019f, 0), new Vector3(.065f, .015f, 3.86f), grid);
                Box("Diamond", logo, new Vector3(0, .019f, sign * 1.9f), new Vector3(3.86f, .015f, .065f), grid);
            }
            return arena;
        }
        private static void CombineArena(GameObject arena)
        {
            var batches = new Dictionary<Material, List<CombineInstance>>();
            MeshFilter[] sources = arena.GetComponentsInChildren<MeshFilter>();
            foreach (MeshFilter filter in sources)
            {
                Material mat = filter.GetComponent<Renderer>().sharedMaterial;
                if (!batches.TryGetValue(mat, out List<CombineInstance> instances)) { instances = new List<CombineInstance>(); batches.Add(mat, instances); }
                instances.Add(new CombineInstance { mesh = filter.sharedMesh, transform = filter.transform.localToWorldMatrix });
            }
            foreach (var pair in batches)
            {
                Mesh mesh = new Mesh { name = "Arena " + pair.Key.name };
                mesh.CombineMeshes(pair.Value.ToArray());
                string path = Root + "Meshes/" + mesh.name + ".asset";
                Mesh existing = AssetDatabase.LoadAssetAtPath<Mesh>(path);
                if (existing != null) { EditorUtility.CopySerialized(mesh, existing); Object.DestroyImmediate(mesh); mesh = existing; }
                else AssetDatabase.CreateAsset(mesh, path);
                GameObject batch = Group(mesh.name, arena.transform); batch.AddComponent<MeshFilter>().sharedMesh = mesh;
                MeshRenderer renderer = batch.AddComponent<MeshRenderer>(); renderer.sharedMaterial = pair.Key; renderer.shadowCastingMode = ShadowCastingMode.Off; renderer.receiveShadows = false;
            }
            foreach (MeshFilter source in sources) Object.DestroyImmediate(source.gameObject);
        }

        private static Sprite CreateSprite(string name, bool round)
        {
            string path = Root + "UI/" + name + ".png";
            Texture2D texture = new Texture2D(64, 64, TextureFormat.RGBA32, false);
            for (int y = 0; y < 64; y++) for (int x = 0; x < 64; x++)
            {
                float alpha = round ? Mathf.Clamp01(31 - Vector2.Distance(new Vector2(x, y), new Vector2(31.5f, 31.5f))) : 1;
                texture.SetPixel(x, y, new Color(1, 1, 1, alpha));
            }
            texture.Apply(); File.WriteAllBytes(path, texture.EncodeToPNG()); Object.DestroyImmediate(texture);
            AssetDatabase.ImportAsset(path, ImportAssetOptions.ForceSynchronousImport);
            TextureImporter importer = (TextureImporter)AssetImporter.GetAtPath(path);
            importer.textureType = TextureImporterType.Sprite; importer.spriteImportMode = SpriteImportMode.Single;
            importer.mipmapEnabled = false; importer.alphaIsTransparency = true;
            importer.textureCompression = TextureImporterCompression.Uncompressed; importer.SaveAndReimport();
            Sprite sprite = AssetDatabase.LoadAssetAtPath<Sprite>(path);
            if (sprite == null) throw new Exception("Sprite import failed: " + path);
            return sprite;
        }
        private static AudioClip[] CreateAudio()
        {
            string[] names = { "Shot", "Hit", "EnemyDeath", "PlayerHurt", "Button", "PlayerDeath" };
            float[] durations = { .085f, .10f, .24f, .23f, .12f, .60f };
            AudioClip[] clips = new AudioClip[names.Length];
            for (int cue = 0; cue < names.Length; cue++)
            {
                string path = Root + "Audio/" + names[cue] + ".wav";
                const int rate = 22050;
                int count = (int)(rate * durations[cue]);
                var random = new System.Random(8721 + cue);
                using (var writer = new BinaryWriter(File.Create(path)))
                {
                    writer.Write(System.Text.Encoding.ASCII.GetBytes("RIFF")); writer.Write(36 + count * 2);
                    writer.Write(System.Text.Encoding.ASCII.GetBytes("WAVEfmt ")); writer.Write(16); writer.Write((short)1); writer.Write((short)1);
                    writer.Write(rate); writer.Write(rate * 2); writer.Write((short)2); writer.Write((short)16);
                    writer.Write(System.Text.Encoding.ASCII.GetBytes("data")); writer.Write(count * 2);
                    double phase = 0;
                    for (int i = 0; i < count; i++)
                    {
                        float t = (float)i / rate, progress = (float)i / count;
                        float noise = (float)random.NextDouble() * 2 - 1;
                        float frequency, mix;
                        switch (cue)
                        {
                            case 0: frequency = Mathf.Lerp(920, 180, progress); mix = .55f; break;
                            case 1: frequency = Mathf.Lerp(180, 70, progress); mix = .8f; break;
                            case 2: frequency = Mathf.Lerp(430, 65, progress); mix = .4f; break;
                            case 3: frequency = Mathf.Lerp(140, 55, progress); mix = .25f; break;
                            case 4: frequency = progress < .45f ? 660 : 990; mix = 0; break;
                            default: frequency = Mathf.Lerp(330, 45, progress); mix = .12f; break;
                        }
                        phase += frequency * Math.PI * 2 / rate;
                        float tone = (float)Math.Sin(phase);
                        float envelope = Mathf.Min(1, t / .004f) * Mathf.Pow(1 - progress, cue == 4 ? 1.6f : 2.2f);
                        float sample = (tone * (1 - mix) + noise * mix) * envelope * .7f;
                        writer.Write((short)(Mathf.Clamp(sample, -1, 1) * short.MaxValue));
                    }
                }
                AssetDatabase.ImportAsset(path, ImportAssetOptions.ForceSynchronousImport);
                AudioImporter importer = (AudioImporter)AssetImporter.GetAtPath(path);
                AudioImporterSampleSettings settings = importer.defaultSampleSettings;
                settings.loadType = AudioClipLoadType.DecompressOnLoad; settings.compressionFormat = AudioCompressionFormat.PCM;
                settings.sampleRateSetting = AudioSampleRateSetting.PreserveSampleRate; importer.defaultSampleSettings = settings;
                importer.forceToMono = true; importer.SaveAndReimport(); clips[cue] = AssetDatabase.LoadAssetAtPath<AudioClip>(path);
            }
            return clips;
        }
        private static AudioService BuildAudioService(AudioClip[] clips)
        {
            GameObject root = new GameObject("AudioService"); AudioService service = root.AddComponent<AudioService>();
            AudioSource[] voices = new AudioSource[12];
            for (int i = 0; i < voices.Length; i++)
            {
                AudioSource voice = Group("Voice " + i, root.transform).AddComponent<AudioSource>();
                voice.playOnAwake = false; voice.spatialBlend = 0; voice.volume = .2f; voice.priority = 128; voices[i] = voice;
            }
            service.Configure(clips, voices); return service;
        }
        private static RectTransform Rect(string name, Transform parent, Vector2 size, Vector2 position, Vector2 anchor)
        {
            GameObject root = new GameObject(name, typeof(RectTransform)); RectTransform rect = (RectTransform)root.transform;
            rect.SetParent(parent, false); rect.anchorMin = rect.anchorMax = anchor; rect.sizeDelta = size; rect.anchoredPosition = position;
            return rect;
        }
        private static RectTransform Stretch(string name, Transform parent)
        {
            RectTransform rect = Rect(name, parent, Vector2.zero, Vector2.zero, Vector2.zero);
            rect.anchorMax = Vector2.one; rect.offsetMin = rect.offsetMax = Vector2.zero; return rect;
        }
        private static Image Image(RectTransform rect, Color color, bool raycast = false)
        { Image image = rect.gameObject.AddComponent<Image>(); image.color = color; image.raycastTarget = raycast; return image; }
        private static Text Label(string name, Transform parent, string content, int size, Color color, Vector2 dimensions, Vector2 position, TextAnchor alignment = TextAnchor.MiddleCenter)
        {
            RectTransform rect = Rect(name, parent, dimensions, position, new Vector2(.5f, .5f));
            Text text = rect.gameObject.AddComponent<Text>(); text.font = font; text.text = content; text.fontSize = size;
            text.color = color; text.alignment = alignment; text.raycastTarget = false;
            text.horizontalOverflow = HorizontalWrapMode.Wrap; text.verticalOverflow = VerticalWrapMode.Truncate;
            return text;
        }
        private static Button Button(string name, Transform parent, string text, Vector2 position)
        {
            RectTransform rect = Rect(name, parent, new Vector2(450, 82), position, new Vector2(.5f, .5f));
            Image image = Image(rect, Accent, true); Button button = rect.gameObject.AddComponent<Button>(); button.targetGraphic = image;
            ColorBlock colors = button.colors; colors.normalColor = Color.white; colors.highlightedColor = new Color(.86f, 1, .94f);
            colors.selectedColor = Color.white; colors.pressedColor = new Color(.52f, .82f, .72f); button.colors = colors;
            Text label = Label("Label", rect, text, 29, Ink, new Vector2(430, 72), Vector2.zero); label.fontStyle = FontStyle.Bold;
            return button;
        }
        private static void BuildUi(GameSession session, PlayerInputReader input)
        {
            GameObject root = new GameObject("Canvas", typeof(RectTransform), typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
            Canvas canvas = root.GetComponent<Canvas>(); canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            CanvasScaler scaler = root.GetComponent<CanvasScaler>(); scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1080, 1080); scaler.screenMatchMode = CanvasScaler.ScreenMatchMode.MatchWidthOrHeight; scaler.matchWidthOrHeight = .5f;
            RectTransform safe = Stretch("Safe Area", root.transform); safe.gameObject.AddComponent<SafeArea>();
            RectTransform menu = Stretch("Main Menu", safe);
            Image(Stretch("Dimmer", menu), new Color(.015f, .03f, .045f, .74f), true);
            RectTransform menuCard = Rect("Menu Card", menu, new Vector2(580, 660), Vector2.zero, new Vector2(.5f, .5f)); Image(menuCard, Ink);
            Image(Rect("Accent line", menuCard, new Vector2(580, 5), new Vector2(0, 328), new Vector2(.5f, .5f)), Accent);
            Label("Eyebrow", menuCard, "S U R V I V A L   A R E N A", 18, Accent, new Vector2(510, 40), new Vector2(0, 265));
            Text title = Label("Title", menuCard, "DUDESTER", 75, new Color(.96f, .97f, .88f), new Vector2(550, 112), new Vector2(0, 181)); title.fontStyle = FontStyle.Bold;
            Label("Tagline", menuCard, "SMALL DUDE.  BIG TROUBLE.", 21, Muted, new Vector2(520, 45), new Vector2(0, 103));
            Image(Rect("Divider", menuCard, new Vector2(450, 1), new Vector2(0, 50), new Vector2(.5f, .5f)), new Color(.23f, .35f, .39f));
            Label("Premise", menuCard, "Keep moving. Your weapon does the rest.\nHow long can you hold the arena?", 22, Color.white, new Vector2(485, 82), new Vector2(0, -9));
            Button play = Button("Play", menuCard, "PLAY   >", new Vector2(0, -116));
            Label("Controls", menuCard, "W A S D  /  ARROW KEYS\nTouch & drag the joystick on mobile", 20, Muted, new Vector2(500, 75), new Vector2(0, -215));
            Label("Footer", menuCard, "MOVE  /  DODGE  /  SURVIVE", 15, Accent, new Vector2(510, 35), new Vector2(0, -293));

            RectTransform hud = Stretch("HUD", safe);
            RectTransform top = Rect("Stats", hud, new Vector2(690, 106), new Vector2(0, -83), new Vector2(.5f, 1)); Image(top, new Color(Ink.r, Ink.g, Ink.b, .94f));
            Label("HP caption", top, "HEALTH", 16, Muted, new Vector2(200, 25), new Vector2(-215, 29));
            Text hp = Label("HP", top, "100 / 100", 27, Color.white, new Vector2(210, 40), new Vector2(-215, -3)); hp.fontStyle = FontStyle.Bold;
            RectTransform track = Rect("Health track", top, new Vector2(190, 7), new Vector2(-215, -33), new Vector2(.5f, .5f)); Image(track, new Color(.24f, .31f, .34f));
            Image fill = Image(Stretch("Health fill", track), Accent); fill.sprite = white; fill.type = UnityEngine.UI.Image.Type.Filled; fill.fillMethod = UnityEngine.UI.Image.FillMethod.Horizontal; fill.fillOrigin = 0;
            Label("Time caption", top, "SURVIVAL TIME", 16, Muted, new Vector2(200, 25), new Vector2(20, 29));
            Text time = Label("Time", top, "00:00", 38, Color.white, new Vector2(210, 50), new Vector2(20, -12)); time.fontStyle = FontStyle.Bold;
            Label("Kills caption", top, "KILLS", 16, Muted, new Vector2(140, 25), new Vector2(244, 29));
            Text kills = Label("Kills", top, "00", 38, Accent, new Vector2(140, 50), new Vector2(244, -12)); kills.fontStyle = FontStyle.Bold;
            RectTransform instruction = Rect("Instruction", hud, new Vector2(530, 40), new Vector2(0, 35), new Vector2(.5f, 0));
            Text hint = instruction.gameObject.AddComponent<Text>(); hint.font = font; hint.fontSize = 17; hint.text = "KEEP MOVING  /  AUTO FIRE ENABLED"; hint.color = Muted; hint.alignment = TextAnchor.MiddleCenter; hint.raycastTarget = false;

            RectTransform touch = Stretch("Touch Controls", safe);
            RectTransform joystick = Rect("Joystick", touch, new Vector2(154, 154), new Vector2(115, 132), Vector2.zero);
            Image baseImage = Image(joystick, new Color(.32f, .62f, .63f, .22f), true); baseImage.sprite = circle;
            RectTransform inner = Rect("Inset", joystick, new Vector2(134, 134), Vector2.zero, new Vector2(.5f, .5f)); Image(inner, new Color(.02f, .07f, .09f, .55f)).sprite = circle;
            RectTransform handle = Rect("Handle", joystick, new Vector2(61, 61), Vector2.zero, new Vector2(.5f, .5f)); Image(handle, new Color(.47f, .95f, .81f, .85f)).sprite = circle;
            VirtualJoystick stick = joystick.gameObject.AddComponent<VirtualJoystick>(); stick.Configure(handle); input.Configure(stick);
            Label("Move label", joystick, "MOVE", 14, Muted, new Vector2(140, 26), new Vector2(0, -98));

            RectTransform end = Stretch("Game Over", safe); Image(Stretch("Dimmer", end), new Color(.015f, .03f, .045f, .8f), true);
            RectTransform endCard = Rect("Results Card", end, new Vector2(580, 640), Vector2.zero, new Vector2(.5f, .5f)); Image(endCard, Ink);
            Image(Rect("Accent line", endCard, new Vector2(580, 5), new Vector2(0, 318), new Vector2(.5f, .5f)), new Color(1, .38f, .26f));
            Label("Eyebrow", endCard, "THE ARENA GOT YOU", 18, new Color(1, .5f, .36f), new Vector2(500, 40), new Vector2(0, 252));
            Text over = Label("Title", endCard, "GAME OVER", 64, Color.white, new Vector2(550, 95), new Vector2(0, 172)); over.fontStyle = FontStyle.Bold;
            Label("Time caption", endCard, "SURVIVAL TIME", 18, Muted, new Vector2(220, 32), new Vector2(-127, 65));
            Label("Kills caption", endCard, "ENEMIES DOWN", 18, Muted, new Vector2(220, 32), new Vector2(127, 65));
            Text finalTime = Label("Final Time", endCard, "00:00", 51, Color.white, new Vector2(240, 85), new Vector2(-127, -2)); finalTime.fontStyle = FontStyle.Bold;
            Text finalKills = Label("Final Kills", endCard, "00", 51, Accent, new Vector2(240, 85), new Vector2(127, -2)); finalKills.fontStyle = FontStyle.Bold;
            Button restart = Button("Restart", endCard, "RESTART   >", new Vector2(0, -125));
            Label("Hint", endCard, "A little more room. A little more time.\nKeep your distance and try again.", 21, Muted, new Vector2(510, 76), new Vector2(0, -233));
            GameUi ui = root.AddComponent<GameUi>(); ui.Configure(session, menu.gameObject, hud.gameObject, end.gameObject, touch.gameObject, time, kills, hp, finalTime, finalKills, fill, play, restart);
            hud.gameObject.SetActive(false); touch.gameObject.SetActive(false); end.gameObject.SetActive(false);
        }

        [MenuItem("Dudester/Validate saved scene")]
        public static void Validate()
        {
            if (SceneManager.GetActiveScene().path != ScenePath)
            {
                if (!Application.isBatchMode && !EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo()) return;
                EditorSceneManager.OpenScene(ScenePath);
            }
            if (GraphicsSettings.defaultRenderPipeline != null || QualitySettings.renderPipeline != null) throw new Exception("Built-in pipeline is not selected.");
            if (Object.FindObjectsByType<GameSession>(FindObjectsSortMode.None).Length != 1 ||
                Object.FindObjectsByType<PlayerController>(FindObjectsSortMode.None).Length != 1 ||
                Object.FindObjectsByType<GameUi>(FindObjectsSortMode.None).Length != 1 ||
                Object.FindObjectsByType<EventSystem>(FindObjectsSortMode.None).Length != 1)
                throw new Exception("Scene must contain exactly one session, player, UI and EventSystem.");
            var roots = new List<GameObject>(SceneManager.GetActiveScene().GetRootGameObjects());
            foreach (string guid in AssetDatabase.FindAssets("t:Prefab", new[] { Root + "Prefabs", Root + "VFX" }))
                roots.Add(AssetDatabase.LoadAssetAtPath<GameObject>(AssetDatabase.GUIDToAssetPath(guid)));
            foreach (GameObject root in roots)
            {
                foreach (Transform child in root.GetComponentsInChildren<Transform>(true))
                {
                    if (GameObjectUtility.GetMonoBehavioursWithMissingScriptCount(child.gameObject) != 0) throw new Exception("Missing script: " + child.name);
                    foreach (MonoBehaviour behaviour in child.GetComponents<MonoBehaviour>())
                    {
                        if (behaviour == null || behaviour.GetType().Assembly.GetName().Name != "Dudester.Runtime") continue;
                        SerializedObject serialized = new SerializedObject(behaviour); SerializedProperty property = serialized.GetIterator();
                        while (property.NextVisible(true))
                            if (property.propertyType == SerializedPropertyType.ObjectReference && property.objectReferenceValue == null)
                                throw new Exception("Unassigned reference: " + child.name + "." + property.propertyPath);
                    }
                }
            }
            Debug.Log("DUDESTER_VALIDATION_OK: no missing scripts or unassigned runtime references.");
        }
    }
}
