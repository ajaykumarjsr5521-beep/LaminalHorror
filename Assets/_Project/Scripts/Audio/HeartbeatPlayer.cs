using UnityEngine;

namespace NocturneAnnex.Audio
{
    /// <summary>
    /// Plays a synthesised lub-dub at the mix's rate and volume, and scales every Ambience cue by the mix. Needs no audio asset, so
    /// real heartbeat clips can replace the pulse later (F-14c). Level and Hiding are set by whoever knows the danger (DangerController).
    /// </summary>
    public sealed class HeartbeatPlayer : MonoBehaviour
    {
        public AudioDirector Director;
        public int Level;
        public bool Hiding;
        public bool Silent;   // tests and pause: no sound, the mix still runs

        public HeartbeatMix Mix { get; } = new HeartbeatMix();
        public int BeatsPlayed { get; private set; }

        AudioSource _source;
        AudioClip _clip;
        float _nextBeat;

        void Awake()
        {
            _source = gameObject.AddComponent<AudioSource>();
            _source.playOnAwake = false; _source.spatialBlend = 0f;
            _clip = MakeBeat();
            Mix.Snap(0, false);
        }

        void Update() => Tick(Time.deltaTime, Time.time);

        /// <summary>One frame. Public so tests can drive it without real time.</summary>
        public void Tick(float dt, float now)
        {
            Mix.Step(dt, Level, Hiding);
            if (Director != null && Director.Catalog != null)
                foreach (var cue in Director.Catalog.Cues)
                    if (cue.Group == CueCatalog.Ambience) Director.SetVolumeScale(cue.Id, Mix.Ambience);

            if (Mix.Heartbeat < 0.02f) { _nextBeat = now; return; }
            if (now < _nextBeat) return;
            _nextBeat = now + 60f / Mix.Bpm;
            BeatsPlayed++;
            if (Silent || _source == null) return;
            float bus = Director != null ? Director.Bus.Gain(CueCatalog.Sfx) : 1f;
            _source.PlayOneShot(_clip, Mix.Heartbeat * bus);
        }

        static AudioClip MakeBeat()
        {
            const int rate = 22050;
            var data = new float[(int)(rate * 0.5f)];
            Thump(data, rate, 0f, 1f);
            Thump(data, rate, 0.17f, 0.7f);
            var clip = AudioClip.Create("HeartbeatSynth", data.Length, 1, rate, false);
            clip.SetData(data, 0);
            return clip;
        }

        static void Thump(float[] data, int rate, float startSeconds, float gain)
        {
            int start = (int)(startSeconds * rate);
            for (int i = 0; start + i < data.Length && i < rate * 0.14f; i++)
            {
                float t = i / (float)rate;
                data[start + i] += Mathf.Sin(2f * Mathf.PI * 55f * t) * Mathf.Exp(-t * 28f) * gain;
            }
        }
    }
}
