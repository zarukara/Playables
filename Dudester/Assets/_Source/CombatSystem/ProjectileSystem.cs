using System.Collections.Generic;
using EnemySystem;
using PoolSystem;
using UnityEngine;

namespace CombatSystem
{
    public sealed class ProjectileSystem : MonoBehaviour
    {
        [SerializeField] private Projectile projectilePrefab;
        [SerializeField, Min(8)] private int capacity = 64;
        [SerializeField, Min(0)] private float projectileRadius = .13f;
        private ComponentPool<Projectile> pool;
        private readonly List<Projectile> active = new List<Projectile>(64);
        public int ActiveCount => active.Count;
        public void Configure(Projectile prefab) => projectilePrefab = prefab;
        public void Initialize()
        {
            if (pool == null) pool = new ComponentPool<Projectile>(projectilePrefab, capacity, transform);
            if (active.Capacity < capacity) active.Capacity = capacity;
        }
        public bool Fire(Vector3 position, Vector3 direction, float damage, float speed, float lifetime)
        {
            Projectile projectile = pool.Rent();
            if (projectile == null) return false;
            projectile.Launch(position, direction, damage, speed, lifetime);
            active.Add(projectile);
            return true;
        }
        public void Tick(EnemySpawner enemies, float dt)
        {
            for (int i = active.Count - 1; i >= 0; i--)
            {
                Projectile projectile = active[i];
                Vector3 from = projectile.transform.position;
                float step = Mathf.Min(dt, Mathf.Max(0, projectile.Remaining));
                Vector3 to = from + projectile.Velocity * step;
                EnemyController hit = null;
                float first = float.PositiveInfinity;
                IReadOnlyList<EnemyController> candidates = enemies.Active;
                for (int e = 0; e < candidates.Count; e++)
                {
                    EnemyController enemy = candidates[e];
                    if (!enemy.CanBeHit) continue;
                    if (!IntersectSegment(from, to, enemy.transform.position, enemy.CollisionRadius + projectileRadius, out float t)) continue;
                    if (t >= first) continue;
                    first = t;
                    hit = enemy;
                }
                projectile.Advance(to, dt);
                if (hit != null)
                {
                    // Consume before callbacks: a projectile can deal damage exactly once.
                    projectile.Consume();
                    hit.TakeDamage(projectile.Damage, Vector3.Lerp(from, to, first));
                }
                if (!projectile.Spent && projectile.Remaining > 0) continue;
                active.RemoveAt(i);
                pool.Return(projectile);
            }
        }
        public static bool IntersectSegment(Vector3 from, Vector3 to, Vector3 center, float radius, out float t)
        {
            Vector2 delta = new Vector2(to.x - from.x, to.z - from.z);
            Vector2 offset = new Vector2(from.x - center.x, from.z - center.z);
            float c = offset.sqrMagnitude - radius * radius;
            if (c <= 0) { t = 0; return true; }
            float a = delta.sqrMagnitude;
            if (a < .000001f) { t = 0; return false; }
            float b = Vector2.Dot(offset, delta);
            float discriminant = b * b - a * c;
            if (discriminant < 0) { t = 0; return false; }
            t = (-b - Mathf.Sqrt(discriminant)) / a;
            return t >= 0 && t <= 1;
        }
        public void ResetProjectiles()
        {
            for (int i = active.Count - 1; i >= 0; i--) pool.Return(active[i]);
            active.Clear();
        }
    }
}
