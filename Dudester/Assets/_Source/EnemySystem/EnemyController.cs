using CombatSystem;
using PlayerSystem;
using UnityEngine;

namespace EnemySystem
{
    public sealed class EnemyController : MonoBehaviour
    {
        [SerializeField, Min(1)] private float health = 3;
        [SerializeField, Min(.1f)] private float movementSpeed = 2.05f;
        [SerializeField, Min(1)] private float contactDamage = 12;
        [SerializeField, Min(.05f)] private float attackCooldown = .85f;
        [SerializeField, Min(.1f)] private float attackRange = .9f;
        [SerializeField, Min(.05f)] private float collisionRadius = .48f;
        [SerializeField] private Transform visual;
        [SerializeField] private DamageFlash flash;
        private EnemySpawner owner;
        private float hp, cooldown, deathTime, spawnTime, phase;
        public bool IsAlive => hp > 0;
        public bool CanBeHit => IsAlive && spawnTime <= 0;
        public int Generation { get; private set; }
        public float CollisionRadius => collisionRadius;
        public float CurrentHealth => hp;
        public DamageFlash FlashFeedback => flash;

        public void Configure(Transform model, DamageFlash feedback) { visual = model; flash = feedback; }
        public void Spawn(EnemySpawner manager, Vector3 position)
        {
            owner = manager;
            Generation++;
            hp = health;
            cooldown = deathTime = 0;
            spawnTime = .4f;
            phase = Random.value * 6.28f;
            transform.position = position;
            visual.localScale = Vector3.one * .05f;
            visual.localPosition = Vector3.zero;
            flash.ResetFlash();
        }
        public void Tick(PlayerController player, float dt)
        {
            flash.Tick(dt);
            if (!IsAlive)
            {
                deathTime += dt;
                visual.localScale = Vector3.one * Mathf.Max(0, 1 - deathTime / .2f);
                if (deathTime >= .2f) owner.Release(this);
                return;
            }
            if (spawnTime > 0)
            {
                spawnTime = Mathf.Max(0, spawnTime - dt);
                visual.localScale = Vector3.one * (1 - spawnTime / .4f);
                return;
            }
            cooldown = Mathf.Max(0, cooldown - dt);
            Vector3 offset = player.transform.position - transform.position;
            offset.y = 0;
            float distance = offset.magnitude;
            if (distance > .01f) transform.rotation = Quaternion.LookRotation(offset);
            if (distance > attackRange * .85f)
            {
                Vector3 direction = offset / Mathf.Max(distance, .01f);
                Vector3 avoidance = owner.Separation(this);
                Vector3 movement = Vector3.ClampMagnitude(direction + avoidance * .65f, 1) * (movementSpeed * dt);
                Vector3 position = transform.position + movement;
                position.x = Mathf.Clamp(position.x, -owner.ArenaExtent, owner.ArenaExtent);
                position.z = Mathf.Clamp(position.z, -owner.ArenaExtent, owner.ArenaExtent);
                transform.position = position;
            }
            phase += dt * 9;
            visual.localPosition = Vector3.up * (Mathf.Abs(Mathf.Sin(phase)) * .055f);
            if (distance <= attackRange && cooldown <= 0)
            {
                cooldown = attackCooldown;
                player.TakeDamage(contactDamage);
            }
        }
        public void TakeDamage(float damage, Vector3 hitPosition)
        {
            if (!CanBeHit || damage <= 0) return;
            hp = Mathf.Max(0, hp - damage);
            flash.Flash();
            owner.NotifyHit(hitPosition, !IsAlive);
            if (!IsAlive) owner.NotifyKilled();
        }
    }
}
