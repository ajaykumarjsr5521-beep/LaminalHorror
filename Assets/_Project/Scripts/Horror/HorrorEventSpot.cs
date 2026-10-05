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

        bool _captured, _slamming, _figureShown, _flickering, _revealStart, _figureStart;
        Vector3 _propStart;
        float _doorSpeed;
        float[] _lightBases = new float[0];

        void Capture()
        {
            if (_captured) return;
            _captured = true;
            if (Prop != null) _propStart = Prop.position;
            if (Reveal != null) _revealStart = Reveal.activeSelf;
            if (Figure != null) _figureStart = Figure.activeSelf;
        }

        /// <summary>Puts the scene objects back to how they were at load: prop, hidden section and figure.</summary>
        public void ResetWorld()
        {
            Capture();
            Interrupt();
            if (Prop != null) Prop.position = _propStart;
            if (Reveal != null) Reveal.SetActive(_revealStart);
            if (Figure != null) Figure.SetActive(_figureStart);
        }

        /// <summary>Re-applies the lasting result of a one-shot that already fired (prop moved, section revealed), after ResetWorld.</summary>
        public void ApplyPersistent()
        {
            Capture();
            if (Event == null) return;
            if (Event.Kind == HorrorEventKind.PropShift && Prop != null) Prop.position = _propStart + PropOffset;
            if (Event.Kind == HorrorEventKind.MisfileReveal && Reveal != null) Reveal.SetActive(true);
        }

        /// <summary>Stops a running effect and undoes its temporary changes (light brightness, door speed, visible figure).</summary>
        void Interrupt()
        {
            if (_flickering)
            {
                for (int i = 0; i < Lights.Length && i < _lightBases.Length; i++) if (Lights[i] != null) Lights[i].intensity = _lightBases[i];
                _flickering = false;
            }
            if (_slamming)
            {
                if (Door != null) Door.AngularSpeed = _doorSpeed;
                _slamming = false;
            }
            if (_figureShown)
            {
                if (Figure != null) Figure.SetActive(false);
                _figureShown = false;
            }
            StopAllCoroutines();
            IsPlaying = false;
        }

        /// <summary>Starts the effect. Returns false, and does nothing, if this spot is already playing.</summary>
        public bool Play(FlashBudget budget, CameraShake shake)
        {
            if (IsPlaying || Event == null) return false;
            Capture();
            StartCoroutine(Run(budget, shake));
            return true;
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
            var bases = _lightBases = new float[Lights.Length];
            for (int i = 0; i < Lights.Length; i++) bases[i] = Lights[i] != null ? Lights[i].intensity : 0f;
            _flickering = true;
            var effect = new LightFlickerEffect(budget, Event.FlickerHz, Event.Strength);
            float start = Time.time;
            while (Time.time - start < Event.DurationSeconds)
            {
                float m = effect.Multiplier(Time.time, Accessibility.ReduceFlicker);
                for (int i = 0; i < Lights.Length; i++) if (Lights[i] != null) Lights[i].intensity = bases[i] * m;
                yield return null;
            }
            for (int i = 0; i < Lights.Length; i++) if (Lights[i] != null) Lights[i].intensity = bases[i];   // always restore
            _flickering = false;
        }

        IEnumerator Slam(CameraShake shake)
        {
            if (Door == null) yield break;
            float speed = _doorSpeed = Door.AngularSpeed;
            _slamming = true;
            Door.AngularSpeed = speed * SlamSpeedMultiplier;
            Door.Close();
            shake?.Shake(Event.Strength, 0.3f);
            yield return new WaitForSeconds(Mathf.Max(0.5f, Event.DurationSeconds));
            Door.AngularSpeed = speed;
            _slamming = false;
        }

        void ShiftProp()
        {
            if (Prop != null) Prop.position += PropOffset;
        }

        IEnumerator ShowFigure(CameraShake shake)
        {
            if (Figure == null) yield break;
            Figure.SetActive(true);
            _figureShown = true;
            shake?.Shake(Event.Strength * 0.5f, 0.2f);
            yield return new WaitForSeconds(Event.DurationSeconds);
            Figure.SetActive(false);
            _figureShown = false;
        }

        void OnDisable() => Interrupt();
    }
}
