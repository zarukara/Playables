using UnityEngine;

namespace AudioSystem
{
    public enum SoundCue { Shot, Hit, EnemyDeath, PlayerHurt, Button, PlayerDeath, EnemySpawn }
    public sealed class AudioService : MonoBehaviour
    {
        [SerializeField, Range(0, 1)] private float masterVolume = .8f;
        [SerializeField] private AudioClip[] clips;
        [SerializeField] private AudioSource[] voices;
        private static readonly float[] Gains = { .38f, .40f, .47f, .43f, .38f, .48f, .30f };
        private static readonly float[] Cooldowns = { .045f, .045f, .06f, .10f, .045f, .15f, .16f };
        private readonly float[] lastPlayed = new float[Gains.Length];
        public float MasterVolume { get => masterVolume; set => masterVolume = Mathf.Clamp01(value); }
        public void Configure(AudioClip[] samples, AudioSource[] channels) { clips = samples; voices = channels; }
        private void Awake() { for (int i = 0; i < lastPlayed.Length; i++) lastPlayed[i] = -10; }
        public void Play(SoundCue cue)
        {
            int index = (int)cue;
            if (clips == null || index < 0 || index >= clips.Length || index >= Gains.Length || clips[index] == null) return;
            if (Time.unscaledTime - lastPlayed[index] < Cooldowns[index]) return;
            AudioSource voice = null;
            for (int i = 0; i < voices.Length; i++) if (!voices[i].isPlaying) { voice = voices[i]; break; }
            if (voice == null)
            {
                if (cue == SoundCue.Shot || cue == SoundCue.Hit || cue == SoundCue.EnemySpawn) return;
                voice = voices[voices.Length - 1];
                voice.Stop();
            }
            lastPlayed[index] = Time.unscaledTime;
            voice.clip = clips[index];
            voice.volume = Gains[index] * masterVolume;
            voice.pitch = cue == SoundCue.Button ? 1 : Random.Range(.95f, 1.05f);
            voice.Play();
        }
        public void ResetAudio()
        {
            foreach (AudioSource voice in voices) voice.Stop();
            for (int i = 0; i < lastPlayed.Length; i++) lastPlayed[i] = -10;
        }
    }
}
