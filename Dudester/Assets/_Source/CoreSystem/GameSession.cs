using System;
using AudioSystem;
using CombatSystem;
using EnemySystem;
using InputSystem;
using PlayerSystem;
using UnityEngine;

namespace CoreSystem
{
    public enum SessionState { Menu, Playing, GameOver }
    public sealed class GameSession : MonoBehaviour
    {
        [SerializeField] private PlayerController player;
        [SerializeField] private PlayerInputReader input;
        [SerializeField] private EnemySpawner enemies;
        [SerializeField] private AutoWeapon weapon;
        [SerializeField] private ProjectileSystem projectiles;
        [SerializeField] private VfxService effects;
        [SerializeField] private AudioService audioService;
        [SerializeField] private ArenaCamera arenaCamera;
        [SerializeField] private float playerExtent = 10.05f;
        public SessionState State { get; private set; }
        public float SurvivalTime { get; private set; }
        public int Kills { get; private set; }
        public PlayerController Player => player;
        public EnemySpawner Enemies => enemies;
        public ProjectileSystem Projectiles => projectiles;
        public AutoWeapon Weapon => weapon;
        public VfxService Effects => effects;
        public event Action<SessionState> StateChanged;
        public event Action StatsChanged;

        public void Configure(PlayerController hero, PlayerInputReader controls, EnemySpawner spawner, AutoWeapon gun,
            ProjectileSystem bullets, VfxService feedback, AudioService sound, ArenaCamera camera)
        { player = hero; input = controls; enemies = spawner; weapon = gun; projectiles = bullets; effects = feedback; audioService = sound; arenaCamera = camera; }
        private void Awake()
        {
            Application.targetFrameRate = 60;
            enemies.Initialize(); projectiles.Initialize(); effects.Initialize();
            player.ResetPlayer(); weapon.ResetWeapon();
            State = SessionState.Menu;
        }
        private void OnEnable()
        {
            player.Died += EndSession; player.Damaged += OnPlayerDamaged;
            enemies.EnemyKilled += OnEnemyKilled; enemies.EnemyHit += OnEnemyHit; enemies.EnemySpawned += OnEnemySpawned;
            weapon.Shot += OnShot;
        }
        private void OnDisable()
        {
            player.Died -= EndSession; player.Damaged -= OnPlayerDamaged;
            enemies.EnemyKilled -= OnEnemyKilled; enemies.EnemyHit -= OnEnemyHit; enemies.EnemySpawned -= OnEnemySpawned;
            weapon.Shot -= OnShot;
        }
        public void StartSession()
        {
            enemies.ResetEnemies(); projectiles.ResetProjectiles(); effects.ResetEffects(); audioService.ResetAudio();
            player.ResetPlayer(); weapon.ResetWeapon();
            SurvivalTime = 0; Kills = 0;
            State = SessionState.Playing;
            audioService.Play(SoundCue.Button);
            StateChanged?.Invoke(State); StatsChanged?.Invoke();
        }
        private void Update()
        {
            if (State != SessionState.Playing) return;
            float dt = Mathf.Min(Time.deltaTime, .05f);
            SurvivalTime += dt;
            player.Tick(input.ReadMovement(), dt, playerExtent);
            enemies.Tick(player, SurvivalTime, dt);
            if (State != SessionState.Playing) return;
            weapon.Tick(enemies, projectiles, dt);
            projectiles.Tick(enemies, dt);
            effects.Tick(dt);
        }
        private void EndSession()
        {
            if (State != SessionState.Playing) return;
            State = SessionState.GameOver;
            weapon.ResetWeapon(); effects.SetPaused(true);
            audioService.Play(SoundCue.PlayerDeath);
            StateChanged?.Invoke(State);
        }
        private void OnShot() => audioService.Play(SoundCue.Shot);
        private void OnEnemySpawned(Vector3 position)
        {
            effects.Play(2, position + Vector3.up * .12f);
            audioService.Play(SoundCue.EnemySpawn);
        }
        private void OnEnemyKilled() { Kills++; StatsChanged?.Invoke(); }
        private void OnEnemyHit(Vector3 position, bool killed)
        {
            effects.Play(killed ? 1 : 0, position);
            audioService.Play(killed ? SoundCue.EnemyDeath : SoundCue.Hit);
        }
        private void OnPlayerDamaged() { audioService.Play(SoundCue.PlayerHurt); arenaCamera.Shake(); }
    }
}
