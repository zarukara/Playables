using System;
using EnemySystem;
using UnityEngine;

namespace CombatSystem
{
    public sealed class AutoWeapon : MonoBehaviour
    {
        [SerializeField, Min(.1f)] private float attackRange = 8;
        [SerializeField, Min(.1f)] private float damage = 1;
        [SerializeField, Min(.03f)] private float fireInterval = .24f;
        [SerializeField, Min(.1f)] private float projectileSpeed = 26;
        [SerializeField, Min(.05f)] private float projectileLifetime = .65f;
        [SerializeField] private Transform gunModel;
        [SerializeField] private Transform muzzle;
        [SerializeField] private GameObject muzzleFlash;
        private EnemyController target;
        private int targetGeneration;
        private float searchCountdown, cooldown, flashTime;
        private Vector3 restPosition;
        public EnemyController Target => target;
        public float AttackRange => attackRange;
        public event Action Shot;
        public void Configure(Transform model, Transform barrel, GameObject flash)
        { gunModel = model; muzzle = barrel; muzzleFlash = flash; }
        private void Awake() => restPosition = gunModel.localPosition;
        public void ResetWeapon()
        {
            target = null;
            searchCountdown = cooldown = flashTime = 0;
            gunModel.localPosition = restPosition;
            muzzleFlash.SetActive(false);
            transform.localRotation = Quaternion.identity;
        }
        public void Tick(EnemySpawner enemies, ProjectileSystem projectiles, float dt)
        {
            cooldown -= dt;
            searchCountdown -= dt;
            flashTime -= dt;
            muzzleFlash.SetActive(flashTime > 0);
            gunModel.localPosition = Vector3.Lerp(gunModel.localPosition, restPosition, Mathf.Min(1, dt * 24));
            Vector3 origin = transform.position;
            if (target != null)
            {
                Vector3 offset = target.transform.position - origin;
                offset.y = 0;
                if (!target.CanBeHit || !target.gameObject.activeSelf || target.Generation != targetGeneration || offset.sqrMagnitude > attackRange * attackRange)
                { target = null; searchCountdown = 0; }
            }
            if (target == null && searchCountdown <= 0)
            {
                searchCountdown = .1f;
                target = enemies.FindNearest(origin, attackRange);
                if (target != null) targetGeneration = target.Generation;
            }
            if (target == null) return;
            Vector3 direction = target.transform.position - origin;
            direction.y = 0;
            if (direction.sqrMagnitude < .001f) return;
            transform.rotation = Quaternion.LookRotation(direction);
            if (cooldown > 0) return;
            if (!projectiles.Fire(muzzle.position, transform.forward, damage, projectileSpeed, projectileLifetime)) return;
            cooldown = fireInterval;
            flashTime = .055f;
            muzzleFlash.SetActive(true);
            gunModel.localPosition = restPosition + Vector3.back * .12f;
            Shot?.Invoke();
        }
    }
}
