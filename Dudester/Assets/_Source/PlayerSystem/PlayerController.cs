using System;
using CombatSystem;
using UnityEngine;

namespace PlayerSystem
{
    public sealed class PlayerController : MonoBehaviour
    {
        [SerializeField, Min(1)] private float maxHealth = 100;
        [SerializeField, Min(.1f)] private float movementSpeed = 5.4f;
        [SerializeField] private Transform visual;
        [SerializeField] private DamageFlash flash;
        private float invulnerability;
        private float stride;
        public float Health { get; private set; }
        public float MaxHealth => maxHealth;
        public bool IsAlive => Health > 0;
        public event Action<float, float> HealthChanged;
        public event Action Damaged;
        public event Action Died;

        public void Configure(Transform model, DamageFlash feedback) { visual = model; flash = feedback; }
        public void ResetPlayer()
        {
            Health = maxHealth;
            invulnerability = stride = 0;
            transform.SetPositionAndRotation(Vector3.zero, Quaternion.identity);
            visual.localPosition = Vector3.zero;
            visual.localRotation = Quaternion.identity;
            flash.ResetFlash();
            HealthChanged?.Invoke(Health, maxHealth);
        }
        public void Tick(Vector2 input, float dt, float extent)
        {
            if (!IsAlive) return;
            invulnerability = Mathf.Max(0, invulnerability - dt);
            flash.Tick(dt);
            Vector2 motion = Vector2.ClampMagnitude(input, 1);
            Vector3 position = transform.position + new Vector3(motion.x, 0, motion.y) * (movementSpeed * dt);
            position.x = Mathf.Clamp(position.x, -extent, extent);
            position.z = Mathf.Clamp(position.z, -extent, extent);
            transform.position = position;
            stride += dt * 13;
            visual.localPosition = Vector3.up * (Mathf.Abs(Mathf.Sin(stride)) * .075f * motion.magnitude);
            if (motion.sqrMagnitude > .01f)
                visual.rotation = Quaternion.Slerp(visual.rotation, Quaternion.LookRotation(new Vector3(motion.x, 0, motion.y)), dt * 18);
        }
        public void TakeDamage(float damage)
        {
            if (!IsAlive || damage <= 0 || invulnerability > 0) return;
            Health = Mathf.Max(0, Health - damage);
            invulnerability = .3f;
            flash.Flash();
            HealthChanged?.Invoke(Health, maxHealth);
            Damaged?.Invoke();
            if (!IsAlive) Died?.Invoke();
        }
    }
}
