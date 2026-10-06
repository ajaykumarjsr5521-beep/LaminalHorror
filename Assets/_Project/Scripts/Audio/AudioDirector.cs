using System.Collections.Generic;
using UnityEngine;
using NocturneAnnex.Core;

namespace NocturneAnnex.Audio
{
    /// <summary>
    /// Plays cues by id from a fixed pool of voices, applies bus gain to every playing voice, posts a cue's caption
    /// and ducks Music and Ambience while a gameplay-relevant cue plays. A missing clip logs one error and the caption still posts.
    /// </summary>
    public class AudioDirector : MonoBehaviour
    {
        public const int MaxVoices = 16;
        const float DuckSeconds = 1.5f;

        public CueCatalog Catalog;
        public AudioBus Bus { get; } = new AudioBus();

        class Voice
        {
            public AudioSource Source;
            public CueDefinition Cue;
            public float Volume;
            public float StartedAt;
        }

        readonly List<Voice> _voices = new List<Voice>();
        readonly HashSet<string> _reportedMissing = new HashSet<string>();

        public int ActiveVoices
        {
            get { int n = 0; foreach (var v in _voices) if (v.Source != null && v.Source.isPlaying) n++; return n; }
        }

        /// <summary>True while any voice is playing the cue.</summary>
        public bool IsPlaying(string cueId)
        {
            foreach (var v in _voices) if (v.Cue != null && v.Cue.Id == cueId && v.Source.isPlaying) return true;
            return false;
        }

        /// <summary>
        /// Plays a cue. A position makes it 3D, otherwise 2D. Returns false for an unknown id, a missing clip or no free voice
        /// (a gameplay-relevant cue steals the oldest voice instead). The caption posts whenever the cue is known.
        /// </summary>
        public bool Play(string cueId, Vector3? position = null, bool loop = false)
        {
            var cue = Catalog != null ? Catalog.Find(cueId) : null;
            if (cue == null) { Debug.LogError($"AudioDirector: unknown cue '{cueId}'.", this); return false; }
            if (!string.IsNullOrEmpty(cue.CaptionKey)) Captions.Post(cue.CaptionKey);

            var clip = PickClip(cue);
            if (clip == null)
            {
                if (_reportedMissing.Add(cueId)) Debug.LogError($"AudioDirector: cue '{cueId}' has no clip assigned.", this);
                return false;
            }
            var voice = FreeVoice() ?? (cue.GameplayRelevant ? OldestVoice() : null);
            if (voice == null) return false;

            voice.Cue = cue;
            voice.Volume = Random.Range(cue.MinVolume, cue.MaxVolume);
            voice.StartedAt = Time.unscaledTime;
            var src = voice.Source;
            src.Stop();
            src.clip = clip;
            src.loop = loop;
            src.spatialBlend = position.HasValue ? 1f : 0f;
            if (position.HasValue) src.transform.position = position.Value;
            src.volume = voice.Volume * Bus.Gain(cue.Group);
            src.Play();
            if (cue.GameplayRelevant) Bus.Duck(AudioBus.DefaultDuckAmount, Mathf.Max(DuckSeconds, clip.length));
            return true;
        }

        public void Stop(string cueId)
        {
            foreach (var v in _voices) if (v.Cue != null && v.Cue.Id == cueId) v.Source.Stop();
        }

        static AudioClip PickClip(CueDefinition cue)
        {
            if (cue.Clips == null || cue.Clips.Length == 0) return null;
            return cue.Clips[Random.Range(0, cue.Clips.Length)];
        }

        Voice FreeVoice()
        {
            foreach (var v in _voices) if (!v.Source.isPlaying) return v;
            if (_voices.Count >= MaxVoices) return null;
            var go = new GameObject("Voice" + _voices.Count);
            go.transform.SetParent(transform, false);
            var src = go.AddComponent<AudioSource>();
            src.playOnAwake = false;
            var voice = new Voice { Source = src };
            _voices.Add(voice);
            return voice;
        }

        Voice OldestVoice()
        {
            Voice oldest = null;
            foreach (var v in _voices) if (oldest == null || v.StartedAt < oldest.StartedAt) oldest = v;
            return oldest;
        }

        void Update()
        {
            Bus.Tick(Time.unscaledDeltaTime);   // unscaled so ducking still resolves while paused
            foreach (var v in _voices)
                if (v.Cue != null && v.Source.isPlaying) v.Source.volume = v.Volume * Bus.Gain(v.Cue.Group);
        }
    }
}
