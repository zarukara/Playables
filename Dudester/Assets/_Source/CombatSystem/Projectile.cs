using UnityEngine;

namespace CombatSystem
{
    public sealed class Projectile : MonoBehaviour
    {
        [SerializeField] private TrailRenderer trail;
        public Vector3 Velocity { get; private set; }
        public float Damage { get; private set; }
        public float Remaining { get; private set; }
        public bool Spent { get; private set; }
        public void Configure(TrailRenderer value) => trail = value;
        public void Launch(Vector3 position, Vector3 direction, float damage, float speed, float lifetime)
        {
            transform.SetPositionAndRotation(position, Quaternion.LookRotation(direction));
            Velocity = direction * speed;
            Damage = damage;
            Remaining = lifetime;
            Spent = false;
            if (trail != null) trail.Clear();
        }
        public void Advance(Vector3 position, float dt) { transform.position = position; Remaining -= dt; }
        public void Consume() => Spent = true;
    }
}
