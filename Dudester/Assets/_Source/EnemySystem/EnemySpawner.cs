using System;
using System.Collections.Generic;
using PlayerSystem;
using PoolSystem;
using UnityEngine;

namespace EnemySystem
{
    public sealed class EnemySpawner : MonoBehaviour
    {
        [SerializeField] private EnemyController enemyPrefab;
        [SerializeField, Range(8, 100)] private int maxActiveEnemies = 48;
        [SerializeField, Min(.1f)] private float initialInterval = 1.25f;
        [SerializeField, Min(.1f)] private float minimumInterval = .27f;
        [SerializeField, Min(1)] private float rampDuration = 150;
        [SerializeField] private float arenaExtent = 10.1f;
        private ComponentPool<EnemyController> pool;
        private readonly List<EnemyController> active = new List<EnemyController>(100);
        private float nextSpawn;
        public IReadOnlyList<EnemyController> Active => active;
        public float ArenaExtent => arenaExtent;
        public int Capacity => maxActiveEnemies;
        public event Action EnemyKilled;
        public event Action<Vector3, bool> EnemyHit;
        public event Action<Vector3> EnemySpawned;
        public void Configure(EnemyController prefab) => enemyPrefab = prefab;
        public void Initialize()
        {
            if (pool == null) pool = new ComponentPool<EnemyController>(enemyPrefab, maxActiveEnemies, transform);
        }
        public void ResetEnemies()
        {
            for (int i = active.Count - 1; i >= 0; i--) pool.Return(active[i]);
            active.Clear();
            nextSpawn = .65f;
        }
        public void Tick(PlayerController player, float elapsed, float dt)
        {
            nextSpawn -= dt;
            if (nextSpawn <= 0)
            {
                nextSpawn = Mathf.Lerp(initialInterval, minimumInterval, Mathf.Clamp01(elapsed / rampDuration));
                Vector3 position = EdgePosition();
                // Avoid unavoidable damage when the player is standing on an entrance.
                if ((position - player.transform.position).sqrMagnitude < 16) position = -position;
                SpawnAt(position);
            }
            for (int i = active.Count - 1; i >= 0 && player.IsAlive; i--) active[i].Tick(player, dt);
        }
        private Vector3 EdgePosition()
        {
            float coordinate = UnityEngine.Random.Range(-arenaExtent, arenaExtent);
            switch (UnityEngine.Random.Range(0, 4))
            {
                case 0: return new Vector3(-arenaExtent, 0, coordinate);
                case 1: return new Vector3(arenaExtent, 0, coordinate);
                case 2: return new Vector3(coordinate, 0, -arenaExtent);
                default: return new Vector3(coordinate, 0, arenaExtent);
            }
        }
        public EnemyController SpawnAt(Vector3 position)
        {
            EnemyController enemy = pool.Rent();
            if (enemy == null) return null;
            enemy.Spawn(this, position);
            active.Add(enemy);
            EnemySpawned?.Invoke(position);
            return enemy;
        }
        public void Release(EnemyController enemy) { active.Remove(enemy); pool.Return(enemy); }
        public void NotifyHit(Vector3 position, bool killed) => EnemyHit?.Invoke(position, killed);
        public void NotifyKilled() => EnemyKilled?.Invoke();
        public EnemyController FindNearest(Vector3 origin, float range)
        {
            EnemyController nearest = null;
            float distance = range * range;
            for (int i = 0; i < active.Count; i++)
            {
                EnemyController candidate = active[i];
                if (!candidate.CanBeHit) continue;
                Vector3 offset = candidate.transform.position - origin;
                offset.y = 0;
                float sqr = offset.sqrMagnitude;
                if (sqr > distance) continue;
                distance = sqr;
                nearest = candidate;
            }
            return nearest;
        }
        public Vector3 Separation(EnemyController self)
        {
            Vector3 push = Vector3.zero;
            for (int i = 0; i < active.Count; i++)
            {
                EnemyController other = active[i];
                if (other == self || !other.IsAlive) continue;
                Vector3 delta = self.transform.position - other.transform.position;
                float sqr = delta.sqrMagnitude;
                if (sqr > .001f && sqr < .85f) push += delta / Mathf.Sqrt(sqr) * (1 - sqr / .85f);
            }
            return Vector3.ClampMagnitude(push, 1);
        }
    }
}
