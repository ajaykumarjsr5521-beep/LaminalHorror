using System.Collections;
using UnityEngine;
using NocturneAnnex.Core;
using NocturneAnnex.Interaction;

namespace NocturneAnnex.Horror
{
    /// <summary>
    /// Binds a HorrorEventAsset to the scene objects it acts on and plays it. Which fields matter depends on the kind:
    /// LightFlicker uses Lights, DoorSlam uses Door, PropShift uses Prop and PropOffset, AudioCue uses Audio,
    /// MisfileReveal uses Reveal, ShadowFigure uses Figure. Every effect posts its caption and respects the
    /// accessibility settings (flash budget, reduce flicker, reduce camera motion).
    /// </summary>
    public class HorrorEventSpot : MonoBehaviour
    {
        public HorrorEventAsset Event;
        public Light[] Lights = new Light[0];
        public Door Door;
        public Transform Prop;
        public Vector3 PropOffset = new Vector3(0f, 0f, 0.6f);
        public AudioSource Audio;
        public GameObject Reveal;
        public GameObject Figure;

        const float SlamSpeedMultiplier = 4f;

        public bool IsPlaying { get; private set; }

        /// <summary>Starts the effect. Does nothing if this spot is already playing.</summary>
        public void Play(FlashBudget budget, CameraShake shake)
        {
            if (IsPlaying || Event == null) return;
            StartCoroutine(Run(budget, shake));
        }

        IEnumerator Run(FlashBudget budget, CameraShake shake)
        {
            IsPlaying = true;
            if (!string.IsNullOrEmpty(Event.CaptionKey)) Captions.Post(Event.CaptionKey);
            switch (Event.Kind)
            {
                case HorrorEventKind.LightFlicker: yield return Flicker(budget); break;
                case HorrorEventKind.DoorSlam: yield return Slam(shake); break;
                case HorrorEventKind.PropShift: ShiftProp(); break;
                case HorrorEventKind.AudioCue: if (Audio != null) Audio.Play(); break;
                case HorrorEventKind.MisfileReveal: if (Reveal != null) Reveal.SetActive(true); break;
                case HorrorEventKind.ShadowFigure: yield return ShowFigure(shake); break;
            }
            IsPlaying = false;
        }

        IEnumerator Flicker(FlashBudget budget)
        {
            var bases = new float[Lights.Length];
            for (int i = 0; i < Lights.Length; i++) bases[i] = Lights[i] != null ? Lights[i].intensity : 0f;
            var effect = new LightFlickerEffect(budget, Event.FlickerHz, Event.Strength);
            float start = Time.time;
            while (Time.time - start < Event.DurationSeconds)
            {
                float m = effect.Multiplier(Time.time, Accessibility.ReduceFlicker);
                for (int i = 0; i < Lights.Length; i++) if (Lights[i] != null) Lights[i].intensity = bases[i] * m;
                yield return null;
            }
            for (int i = 0; i < Lights.Length; i++) if (Lights[i] != null) Lights[i].intensity = bases[i];   // always restore
        }

        IEnumerator Slam(CameraShake shake)
        {
            if (Door == null) yield break;
            float speed = Door.AngularSpeed;
            Door.AngularSpeed = speed * SlamSpeedMultiplier;
            Door.Close();
            shake?.Shake(Event.Strength, 0.3f);
            yield return new WaitForSeconds(Mathf.Max(0.5f, Event.DurationSeconds));
            Door.AngularSpeed = speed;
        }

        void ShiftProp()
        {
            if (Prop != null) Prop.position += PropOffset;
        }

        IEnumerator ShowFigure(CameraShake shake)
        {
            if (Figure == null) yield break;
            Figure.SetActive(true);
            shake?.Shake(Event.Strength * 0.5f, 0.2f);
            yield return new WaitForSeconds(Event.DurationSeconds);
            Figure.SetActive(false);
        }

        void OnDisable() => IsPlaying = false;
    }
}
