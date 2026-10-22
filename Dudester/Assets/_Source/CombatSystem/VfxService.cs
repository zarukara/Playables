using System.Collections.Generic;
using PoolSystem;
using UnityEngine;

namespace CombatSystem
{
    public sealed class VfxService : MonoBehaviour
    {
        [SerializeField] private ParticleSystem hitPrefab;
        [SerializeField] private ParticleSystem deathPrefab;
        [SerializeField] private ParticleSystem spawnPrefab;
        [SerializeField, Range(4, 48)] private int poolSize = 18;
        private ComponentPool<ParticleSystem>[] pools;
        private struct Burst { public ParticleSystem effect; public int kind; public float remaining; }
        private readonly List<Burst> active = new List<Burst>(64);
        public int ActiveCount => active.Count;
        public void Configure(ParticleSystem hit, ParticleSystem death, ParticleSystem spawn)
        { hitPrefab = hit; deathPrefab = death; spawnPrefab = spawn; }
        public void Initialize()
        {
            if (pools != null) return;
            pools = new[] {
                new ComponentPool<ParticleSystem>(hitPrefab, poolSize, transform),
                new ComponentPool<ParticleSystem>(deathPrefab, poolSize, transform),
                new ComponentPool<ParticleSystem>(spawnPrefab, poolSize, transform)
            };
            active.Capacity = Mathf.Max(active.Capacity, poolSize * 3);
        }
        public void Play(int kind, Vector3 position)
        {
            ParticleSystem effect = pools[kind].Rent();
            if (effect == null) return;
            effect.transform.position = position;
            effect.Clear(true);
            effect.Play(true);
            active.Add(new Burst { effect = effect, kind = kind, remaining = .8f });
        }
        public void Tick(float dt)
        {
            for (int i = active.Count - 1; i >= 0; i--)
            {
                Burst burst = active[i];
                burst.remaining -= dt;
                if (burst.remaining > 0) { active[i] = burst; continue; }
                burst.effect.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
                pools[burst.kind].Return(burst.effect);
                active.RemoveAt(i);
            }
        }
        public void SetPaused(bool paused)
        {
            for (int i = 0; i < active.Count; i++)
                if (paused) active[i].effect.Pause(true); else active[i].effect.Play(true);
        }
        public void ResetEffects()
        {
            for (int i = active.Count - 1; i >= 0; i--)
            {
                Burst burst = active[i];
                burst.effect.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
                pools[burst.kind].Return(burst.effect);
            }
            active.Clear();
        }
    }
}
