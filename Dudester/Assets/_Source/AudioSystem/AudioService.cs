using UnityEngine;

namespace AudioSystem
{
    public enum SoundCue { Shot, Hit, EnemyDeath, PlayerHurt, Button, PlayerDeath }
    public sealed class AudioService : MonoBehaviour
    {
        [SerializeField, Range(0, 1)] private float masterVolume = .65f;
        [SerializeField] private AudioClip[] clips;
        [SerializeField] private AudioSource[] voices;
        private readonly float[] lastPlayed = new float[6];
        private static readonly float[] Gains = { .21f, .22f, .3f, .4f, .38f, .42f };
        public float MasterVolume { get => masterVolume; set => masterVolume = Mathf.Clamp01(value); }
        public void Configure(AudioClip[] samples, AudioSource[] channels) { clips = samples; voices = channels; }
        private void Awake() { for (int i = 0; i < lastPlayed.Length; i++) lastPlayed[i] = -10; }
        public void Play(SoundCue cue)
        {
            int index = (int)cue;
            if (clips == null || index >= clips.Length || clips[index] == null) return;
            if (Time.unscaledTime - lastPlayed[index] < .045f) return;
            AudioSource voice = null;
            for (int i = 0; i < voices.Length; i++) if (!voices[i].isPlaying) { voice = voices[i]; break; }
            if (voice == null)
            {
                if (cue == SoundCue.Shot || cue == SoundCue.Hit) return;
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
