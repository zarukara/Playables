using UnityEngine;
using CoreSystem;

namespace CombatSystem
{
    public sealed class DamageFlash : MonoBehaviour
    {
        [SerializeField] private Renderer[] surfaces;
        private MaterialPropertyBlock block;
        private float remaining;
        private static readonly int ColorId = Shader.PropertyToID("_Color");
        public bool IsFlashing => remaining > 0;
        public void Configure(Renderer[] value) => surfaces = value;
        private void Awake() => block = new MaterialPropertyBlock();
        public void Flash()
        {
            remaining = .12f;
            if (block == null) block = new MaterialPropertyBlock();
            block.SetColor(ColorId, Color.Lerp(GamePalette.Red, GamePalette.Light, .4f));
            foreach (Renderer surface in surfaces) surface.SetPropertyBlock(block);
        }
        public void Tick(float dt)
        {
            if (remaining <= 0) return;
            remaining -= dt;
            if (remaining <= 0) ResetFlash();
        }
        public void ResetFlash()
        {
            remaining = 0;
            foreach (Renderer surface in surfaces) surface.SetPropertyBlock(null);
        }
    }
}
