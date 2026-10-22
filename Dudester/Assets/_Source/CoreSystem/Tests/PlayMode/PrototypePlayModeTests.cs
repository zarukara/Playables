using System.Collections;
using System.IO;
using CombatSystem;
using CoreSystem;
using EnemySystem;
using InputSystem;
using NUnit.Framework;
using UiSystem;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.LowLevel;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;
using UnityEngine.UI;

namespace CoreSystem
{
    public sealed class PrototypePlayModeTests
    {
        private GameSession session;
        private GameUi ui;
        [UnitySetUp]
        public IEnumerator LoadScene()
        {
            yield return SceneManager.LoadSceneAsync("Main");
            yield return null;
            session = Object.FindFirstObjectByType<GameSession>();
            ui = Object.FindFirstObjectByType<GameUi>();
            Assert.That(session, Is.Not.Null); Assert.That(ui, Is.Not.Null);
        }

        [UnityTest]
        public IEnumerator MenuAndPlayButtonStartTheRealLoop()
        {
            Assert.That(session.State, Is.EqualTo(SessionState.Menu));
            Assert.That(ui.MenuPanel.activeSelf, Is.True);
            ui.PlayButton.onClick.Invoke();
            yield return new WaitForSeconds(.9f);
            Assert.That(session.State, Is.EqualTo(SessionState.Playing));
            Assert.That(session.SurvivalTime, Is.GreaterThan(.6f));
            Assert.That(session.Enemies.Active.Count, Is.GreaterThan(0));
            Assert.That(ui.MenuPanel.activeSelf, Is.False);
        }

        [UnityTest]
        public IEnumerator KeyboardMovesPlayerThroughInputSystem()
        {
            var previousBehavior = UnityEngine.InputSystem.InputSystem.settings.backgroundBehavior;
            UnityEngine.InputSystem.InputSystem.settings.backgroundBehavior = InputSettings.BackgroundBehavior.IgnoreFocus;
#if UNITY_EDITOR
            var previousEditorBehavior = UnityEngine.InputSystem.InputSystem.settings.editorInputBehaviorInPlayMode;
            UnityEngine.InputSystem.InputSystem.settings.editorInputBehaviorInPlayMode = InputSettings.EditorInputBehaviorInPlayMode.AllDeviceInputAlwaysGoesToGameView;
#endif
            Keyboard keyboard = UnityEngine.InputSystem.InputSystem.AddDevice<Keyboard>();
            try
            {
                session.StartSession();
                UnityEngine.InputSystem.InputSystem.QueueStateEvent(keyboard, new KeyboardState(Key.W, Key.D));
                yield return new WaitForSeconds(.25f);
                Vector3 position = session.Player.transform.position;
                Assert.That(position.x, Is.GreaterThan(.2f)); Assert.That(position.z, Is.GreaterThan(.2f));
                Assert.That(position.magnitude, Is.LessThan(1.8f));
                UnityEngine.InputSystem.InputSystem.QueueStateEvent(keyboard, new KeyboardState());
                yield return null;
            }
            finally
            {
                UnityEngine.InputSystem.InputSystem.RemoveDevice(keyboard);
                UnityEngine.InputSystem.InputSystem.settings.backgroundBehavior = previousBehavior;
#if UNITY_EDITOR
                UnityEngine.InputSystem.InputSystem.settings.editorInputBehaviorInPlayMode = previousEditorBehavior;
#endif
            }
        }

        [Test]
        public void DiagonalSpeedAndArenaBoundsAreCorrect()
        {
            session.StartSession();
            session.Player.Tick(Vector2.right, .5f, 10.05f); float axial = session.Player.transform.position.magnitude;
            session.Player.ResetPlayer(); session.Player.Tick(Vector2.one, .5f, 10.05f);
            Assert.That(session.Player.transform.position.magnitude, Is.EqualTo(axial).Within(.001f));
            session.Player.Tick(Vector2.one, 100, 10.05f);
            Assert.That(session.Player.transform.position.x, Is.EqualTo(10.05f).Within(.001f));
            Assert.That(session.Player.transform.position.z, Is.EqualTo(10.05f).Within(.001f));
        }

        [Test]
        public void TouchJoystickNormalizesAndReleases()
        {
            session.StartSession();
            VirtualJoystick stick = Object.FindFirstObjectByType<VirtualJoystick>();
            Canvas.ForceUpdateCanvases();
            Vector2 center = RectTransformUtility.WorldToScreenPoint(null, stick.transform.position);
            var data = new PointerEventData(EventSystem.current) { pointerId = 0, position = center + Vector2.one * 300 };
            stick.OnPointerDown(data);
            Assert.That(stick.Value.magnitude, Is.EqualTo(1).Within(.001f));
            Assert.That(stick.Value.x, Is.GreaterThan(0)); Assert.That(stick.Value.y, Is.GreaterThan(0));
            stick.OnPointerUp(data); Assert.That(stick.Value, Is.EqualTo(Vector2.zero));
        }

        [Test]
        public void TargetLockIgnoresCloserEnemyAndReleasesOutsideRange()
        {
            session.StartSession();
            EnemyController first = SpawnReady(new Vector3(0, 0, 5));
            session.Weapon.Tick(session.Enemies, session.Projectiles, .1f);
            Assert.That(session.Weapon.Target, Is.SameAs(first));
            EnemyController closer = SpawnReady(new Vector3(2, 0, 0));
            session.Weapon.Tick(session.Enemies, session.Projectiles, .2f);
            Assert.That(session.Weapon.Target, Is.SameAs(first));
            first.transform.position = new Vector3(10, 0, 10);
            session.Weapon.Tick(session.Enemies, session.Projectiles, .1f);
            Assert.That(session.Weapon.Target, Is.SameAs(closer));
            closer.TakeDamage(100, closer.transform.position);
            session.Weapon.Tick(session.Enemies, session.Projectiles, .1f);
            Assert.That(session.Weapon.Target, Is.Null);
        }

        [Test]
        public void SweptProjectileHitsOnceAndRestoresSharedMaterial()
        {
            session.StartSession();
            EnemyController enemy = SpawnReady(new Vector3(0, 0, 3));
            Renderer body = enemy.transform.Find("Creature/Body").GetComponent<Renderer>();
            Material material = body.sharedMaterial;
            session.Projectiles.Fire(new Vector3(0, .8f, 0), Vector3.forward, 1, 100, 1);
            session.Projectiles.Tick(session.Enemies, .1f);
            Assert.That(enemy.CurrentHealth, Is.EqualTo(2));
            Assert.That(session.Projectiles.ActiveCount, Is.Zero);
            Assert.That(enemy.FlashFeedback.IsFlashing, Is.True);
            Assert.That(session.Effects.ActiveCount, Is.GreaterThan(0));
            session.Projectiles.Tick(session.Enemies, .1f);
            Assert.That(enemy.CurrentHealth, Is.EqualTo(2));
            enemy.FlashFeedback.Tick(.2f);
            Assert.That(enemy.FlashFeedback.IsFlashing, Is.False);
            Assert.That(body.sharedMaterial, Is.SameAs(material));
            var block = new MaterialPropertyBlock(); body.GetPropertyBlock(block); Assert.That(block.isEmpty, Is.True);
        }

        [Test]
        public void ProjectileChoosesFirstIntersectionAndExpires()
        {
            session.StartSession();
            EnemyController far = SpawnReady(new Vector3(0, 0, 6));
            EnemyController near = SpawnReady(new Vector3(0, 0, 3));
            session.Projectiles.Fire(new Vector3(0, .8f, 0), Vector3.forward, 1, 100, 1);
            session.Projectiles.Tick(session.Enemies, .1f);
            Assert.That(near.CurrentHealth, Is.EqualTo(2)); Assert.That(far.CurrentHealth, Is.EqualTo(3));
            session.Projectiles.Fire(new Vector3(0, .8f, 0), Vector3.left, 1, 26, .1f);
            session.Projectiles.Tick(session.Enemies, .2f); Assert.That(session.Projectiles.ActiveCount, Is.Zero);
        }

        [Test]
        public void EnemyContactDamageHasCooldownAndDeathReturnsToPool()
        {
            session.StartSession();
            EnemyController enemy = SpawnReady(new Vector3(0, 0, .7f));
            enemy.Tick(session.Player, .01f); float afterHit = session.Player.Health;
            Assert.That(afterHit, Is.LessThan(session.Player.MaxHealth));
            session.Player.Tick(Vector2.zero, .4f, 10.05f); enemy.Tick(session.Player, .4f);
            Assert.That(session.Player.Health, Is.EqualTo(afterHit));
            session.Player.Tick(Vector2.zero, .5f, 10.05f); enemy.Tick(session.Player, .5f);
            Assert.That(session.Player.Health, Is.LessThan(afterHit));
            enemy.TakeDamage(100, enemy.transform.position);
            Assert.That(session.Kills, Is.EqualTo(1)); Assert.That(enemy.gameObject.activeSelf, Is.True);
            enemy.Tick(session.Player, .21f);
            Assert.That(enemy.gameObject.activeSelf, Is.False); Assert.That(session.Enemies.Active.Count, Is.Zero);
        }

        [UnityTest]
        public IEnumerator DeathStopsSimulationAndRepeatedRestartDoesNotDuplicate()
        {
            session.StartSession();
            SpawnReady(new Vector3(5, 0, 0));
            session.Player.TakeDamage(1000);
            Assert.That(session.State, Is.EqualTo(SessionState.GameOver)); Assert.That(ui.GameOverPanel.activeSelf, Is.True);
            float time = session.SurvivalTime; Vector3 position = session.Enemies.Active[0].transform.position;
            yield return new WaitForSeconds(.2f);
            Assert.That(session.SurvivalTime, Is.EqualTo(time)); Assert.That(session.Enemies.Active[0].transform.position, Is.EqualTo(position));
            int objects = Object.FindObjectsByType<Transform>(FindObjectsInactive.Include, FindObjectsSortMode.None).Length;
            for (int i = 0; i < 10; i++)
            {
                ui.RestartButton.onClick.Invoke();
                Assert.That(session.Player.Health, Is.EqualTo(100)); Assert.That(session.SurvivalTime, Is.Zero); Assert.That(session.Kills, Is.Zero);
                Assert.That(session.Enemies.Active.Count, Is.Zero); Assert.That(session.Projectiles.ActiveCount, Is.Zero); Assert.That(session.Effects.ActiveCount, Is.Zero);
                EnemyController enemy = SpawnReady(new Vector3(0, 0, 3)); enemy.TakeDamage(100, enemy.transform.position);
                Assert.That(session.Kills, Is.EqualTo(1), "Kill subscriptions multiplied");
                session.Player.TakeDamage(1000);
            }
            Assert.That(Object.FindObjectsByType<GameSession>(FindObjectsSortMode.None).Length, Is.EqualTo(1));
            Assert.That(Object.FindObjectsByType<Transform>(FindObjectsInactive.Include, FindObjectsSortMode.None).Length, Is.EqualTo(objects));
        }

        [Test]
        public void EnemyPoolIsBoundedAndReusable()
        {
            session.StartSession();
            for (int i = 0; i < session.Enemies.Capacity + 10; i++) session.Enemies.SpawnAt(new Vector3(10, 0, 0));
            Assert.That(session.Enemies.Active.Count, Is.EqualTo(session.Enemies.Capacity));
            session.Enemies.ResetEnemies();
            Assert.That(session.Enemies.SpawnAt(Vector3.zero), Is.Not.Null);
        }

        [UnityTest]
        public IEnumerator AutonomousCombatProducesKillsAndAudio()
        {
            session.StartSession();
            SpawnReady(new Vector3(0, 0, 4));
            bool heardAudio = false;
            float until = Time.time + 3;
            while (Time.time < until && session.Kills == 0)
            {
                foreach (AudioSource voice in Object.FindObjectsByType<AudioSource>(FindObjectsSortMode.None)) heardAudio |= voice.isPlaying;
                yield return null;
            }
            Assert.That(session.Kills, Is.GreaterThan(0)); Assert.That(heardAudio, Is.True);
        }

        [UnityTest]
        public IEnumerator SustainedEnemyPressureEndsInGameOver()
        {
            // Exercise the real Update loop under a dense but bounded spawn configuration.
            JsonUtility.FromJsonOverwrite("{\"initialInterval\":0.2,\"minimumInterval\":0.2}", session.Enemies);
            session.StartSession();
            float deadline = Time.realtimeSinceStartup + 24;
            while (session.State == SessionState.Playing && Time.realtimeSinceStartup < deadline)
            {
                Assert.That(session.Enemies.Active.Count, Is.LessThanOrEqualTo(session.Enemies.Capacity));
                yield return null;
            }
            Assert.That(session.State, Is.EqualTo(SessionState.GameOver));
            Assert.That(session.Player.Health, Is.Zero);
            Assert.That(session.Kills, Is.GreaterThan(0));
            Assert.That(ui.GameOverPanel.activeSelf, Is.True);
        }

        [UnityTest]
        public IEnumerator CaptureVisualEvidence()
        {
            Directory.CreateDirectory("Logs/QA");
            yield return null;
            Capture("Menu", 1600, 1000);
            Capture("MenuPortrait", 900, 1600);
            session.StartSession();
            for (int i = 0; i < 12; i++) SpawnReady(new Vector3(Mathf.Cos(i * .6f) * 7, 0, Mathf.Sin(i * .6f) * 7));
            yield return new WaitForSeconds(.5f);
            Capture("Gameplay", 1600, 1000);
            Capture("Portrait", 900, 1600);
            session.Player.TakeDamage(1000); yield return null;
            Capture("GameOver", 1600, 1000);
        }

        private EnemyController SpawnReady(Vector3 position)
        {
            EnemyController enemy = session.Enemies.SpawnAt(position); Assert.That(enemy, Is.Not.Null);
            enemy.Tick(session.Player, .41f); return enemy;
        }
        private static void Capture(string name, int width, int height)
        {
            Camera camera = Camera.main; Canvas canvas = Object.FindFirstObjectByType<Canvas>();
            var target = new RenderTexture(width, height, 24);
            RenderTexture previous = RenderTexture.active;
            camera.targetTexture = target;
            camera.orthographicSize = Mathf.Max(13.2f, 12.6f * height / width);
            canvas.renderMode = RenderMode.ScreenSpaceCamera; canvas.worldCamera = camera; canvas.planeDistance = 1;
            Canvas.ForceUpdateCanvases(); camera.Render(); RenderTexture.active = target;
            var texture = new Texture2D(width, height, TextureFormat.RGB24, false);
            texture.ReadPixels(new Rect(0, 0, width, height), 0, 0); texture.Apply();
            File.WriteAllBytes("Logs/QA/" + name + ".png", texture.EncodeToPNG());
            camera.targetTexture = null; RenderTexture.active = previous; canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            Object.Destroy(texture); target.Release(); Object.Destroy(target);
        }
    }
}
