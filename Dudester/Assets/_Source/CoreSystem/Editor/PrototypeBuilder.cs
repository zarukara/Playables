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
        private static Sprite white;
        private static readonly Color Ink = GamePalette.Black;
        private static readonly Color Muted = Color.Lerp(GamePalette.Grey, GamePalette.Light, .65f);
        private static readonly Color Accent = GamePalette.Red;

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
            white = CreateSprite("White", false);
            font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            AudioClip[] clips = CreateAudio();
            EnemyController enemyPrefab = BuildEnemy();
            Projectile projectilePrefab = BuildProjectile();
            AutoWeapon weaponPrefab = BuildWeapon();
            PlayerController playerPrefab = BuildPlayer(weaponPrefab);
            ParticleSystem hit = BuildEffect("HitBurst", Accent, 9, 2.8f, .14f);
            ParticleSystem death = BuildEffect("DeathBurst", Accent, 16, 3.8f, .21f);
            ParticleSystem spawn = BuildEffect("SpawnBurst", GamePalette.Light, 10, 1.2f, .13f);
            RenderSettings.ambientMode = AmbientMode.Flat;
            RenderSettings.ambientLight = new Color(.74f, .74f, .74f);
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
            camera.clearFlags = CameraClearFlags.SolidColor; camera.backgroundColor = GamePalette.Black;
            camera.nearClipPlane = .1f; camera.farClipPlane = 100; camera.allowHDR = false; camera.allowMSAA = true;
            Light light = new GameObject("Directional Light", typeof(Light)).GetComponent<Light>();
            light.type = LightType.Directional; light.intensity = 1.05f; light.color = Color.white;
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
            BuildUi(session);
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
            // Existing asset paths/GUIDs are retained for references in saved prefabs.
            floor = Material("Arena - petrol", GamePalette.Grey);
            grid = Material("Arena - grid", Color.Lerp(GamePalette.Grey, GamePalette.Light, .12f));
            wall = Material("Arena - slate", GamePalette.Night);
            dark = Material("Graphite", GamePalette.Black);
            mint = Material("Signal - mint", GamePalette.Light);
            cream = Material("Hero - porcelain", GamePalette.Light);
            orange = Material("Enemy - coral", GamePalette.Red);
            red = Material("Enemy - visor", GamePalette.Black);
            yellow = Material("Shot - warm light", GamePalette.Light, true);
            shadow = Material("Contact shadows", GamePalette.Black, true);
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
            string[] names = { "Shot", "Hit", "EnemyDeath", "PlayerHurt", "Button", "PlayerDeath", "EnemySpawn" };
            float[] durations = { .13f, .15f, .34f, .23f, .12f, .60f, .30f };
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
                    float filteredNoise = 0;
                    for (int i = 0; i < count; i++)
                    {
                        float t = (float)i / rate, progress = (float)i / count;
                        float noise = (float)random.NextDouble() * 2 - 1;
                        float frequency, mix;
                        switch (cue)
                        {
                            case 0: frequency = 110 + 760 * Mathf.Exp(-progress * 16); mix = .55f; break;
                            case 1: frequency = Mathf.Lerp(240, 65, progress); mix = .7f; break;
                            case 2: frequency = 55 + 300 * Mathf.Pow(1 - progress, 2); mix = .45f; break;
                            case 3: frequency = Mathf.Lerp(140, 55, progress); mix = .25f; break;
                            case 4: frequency = progress < .45f ? 660 : 990; mix = 0; break;
                            case 5: frequency = Mathf.Lerp(330, 45, progress); mix = .12f; break;
                            default: frequency = Mathf.Lerp(160, 720, progress * progress); mix = .12f; break;
                        }
                        phase += frequency * Math.PI * 2 / rate;
                        filteredNoise = Mathf.Lerp(filteredNoise, noise, cue == 1 ? .85f : .3f);
                        float tone = (float)Math.Sin(phase);
                        if (cue == 2) tone = .7f * tone + .3f * (float)Math.Sin(phase * .51);
                        if (cue == 6) tone = .75f * tone + .25f * (float)Math.Sin(phase * 1.5);
                        float envelope = cue == 6
                            ? Mathf.Sin(progress * Mathf.PI) * (1 - progress * .35f)
                            : Mathf.Min(1, t / .002f) * Mathf.Pow(1 - progress, cue == 4 ? 1.6f : 2.2f);
                        float sample = (tone * (1 - mix) + filteredNoise * mix) * envelope * .82f;
                        if (cue == 0) sample += noise * Mathf.Exp(-t * 500) * .25f;
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
            ColorBlock colors = button.colors; colors.normalColor = Color.white; colors.highlightedColor = new Color(1.2f, 1.2f, 1.2f);
            colors.selectedColor = Color.white; colors.pressedColor = new Color(.75f, .75f, .75f); button.colors = colors;
            Text label = Label("Label", rect, text, 29, GamePalette.Light, new Vector2(430, 72), Vector2.zero); label.fontStyle = FontStyle.Bold;
            return button;
        }
        private static void BuildUi(GameSession session)
        {
            GameObject root = new GameObject("Canvas", typeof(RectTransform), typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
            Canvas canvas = root.GetComponent<Canvas>(); canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            CanvasScaler scaler = root.GetComponent<CanvasScaler>(); scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1080, 1080); scaler.screenMatchMode = CanvasScaler.ScreenMatchMode.MatchWidthOrHeight; scaler.matchWidthOrHeight = .5f;
            RectTransform safe = Stretch("Safe Area", root.transform); safe.gameObject.AddComponent<SafeArea>();
            RectTransform menu = Stretch("Main Menu", safe);
            Image(Stretch("Dimmer", menu), new Color(Ink.r, Ink.g, Ink.b, .8f), true);
            RectTransform menuCard = Rect("Menu Card", menu, new Vector2(580, 340), Vector2.zero, new Vector2(.5f, .5f)); Image(menuCard, Ink);
            Text title = Label("Title", menuCard, "DUDESTER", 75, GamePalette.Light, new Vector2(550, 112), new Vector2(0, 62)); title.fontStyle = FontStyle.Bold;
            Button play = Button("Play", menuCard, "Play", new Vector2(0, -74));

            RectTransform hud = Stretch("HUD", safe);
            RectTransform top = Rect("Stats", hud, new Vector2(690, 106), new Vector2(0, -83), new Vector2(.5f, 1)); Image(top, new Color(Ink.r, Ink.g, Ink.b, .94f));
            Label("HP caption", top, "HEALTH", 16, Muted, new Vector2(200, 25), new Vector2(-215, 29));
            Text hp = Label("HP", top, "100 / 100", 27, GamePalette.Light, new Vector2(210, 40), new Vector2(-215, -3)); hp.fontStyle = FontStyle.Bold;
            RectTransform track = Rect("Health track", top, new Vector2(190, 7), new Vector2(-215, -33), new Vector2(.5f, .5f)); Image(track, GamePalette.Grey);
            Image fill = Image(Stretch("Health fill", track), GamePalette.Light); fill.sprite = white; fill.type = UnityEngine.UI.Image.Type.Filled; fill.fillMethod = UnityEngine.UI.Image.FillMethod.Horizontal; fill.fillOrigin = 0;
            Label("Time caption", top, "SURVIVAL TIME", 16, Muted, new Vector2(200, 25), new Vector2(20, 29));
            Text time = Label("Time", top, "00:00", 38, GamePalette.Light, new Vector2(210, 50), new Vector2(20, -12)); time.fontStyle = FontStyle.Bold;
            Label("Kills caption", top, "KILLS", 16, Muted, new Vector2(140, 25), new Vector2(244, 29));
            Text kills = Label("Kills", top, "00", 38, GamePalette.Light, new Vector2(140, 50), new Vector2(244, -12)); kills.fontStyle = FontStyle.Bold;

            RectTransform end = Stretch("Game Over", safe); Image(Stretch("Dimmer", end), new Color(Ink.r, Ink.g, Ink.b, .8f), true);
            RectTransform endCard = Rect("Results Card", end, new Vector2(580, 500), Vector2.zero, new Vector2(.5f, .5f)); Image(endCard, Ink);
            Image(Rect("Accent line", endCard, new Vector2(580, 5), new Vector2(0, 248), new Vector2(.5f, .5f)), Accent);
            Text over = Label("Title", endCard, "GAME OVER", 64, GamePalette.Light, new Vector2(550, 95), new Vector2(0, 160)); over.fontStyle = FontStyle.Bold;
            Label("Time caption", endCard, "SURVIVAL TIME", 18, Muted, new Vector2(220, 32), new Vector2(-127, 65));
            Label("Kills caption", endCard, "ENEMIES DOWN", 18, Muted, new Vector2(220, 32), new Vector2(127, 65));
            Text finalTime = Label("Final Time", endCard, "00:00", 51, GamePalette.Light, new Vector2(240, 85), new Vector2(-127, -2)); finalTime.fontStyle = FontStyle.Bold;
            Text finalKills = Label("Final Kills", endCard, "00", 51, GamePalette.Light, new Vector2(240, 85), new Vector2(127, -2)); finalKills.fontStyle = FontStyle.Bold;
            Button restart = Button("Restart", endCard, "Restart", new Vector2(0, -145));
            GameUi ui = root.AddComponent<GameUi>(); ui.Configure(session, menu.gameObject, hud.gameObject, end.gameObject, time, kills, hp, finalTime, finalKills, fill, play, restart);
            hud.gameObject.SetActive(false); end.gameObject.SetActive(false);
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
