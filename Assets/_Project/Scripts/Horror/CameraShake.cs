using UnityEngine;
using NocturneAnnex.Core;

namespace NocturneAnnex.Horror
{
    /// <summary>
    /// Short positional camera shake. Amplitude is multiplied by Accessibility.MotionScale, so with Reduce camera motion
    /// on there is no shake at all. Put it on the camera object; it only touches that object's local position.
    /// </summary>
    public class CameraShake : MonoBehaviour
    {
        public const float MaxOffsetMetres = 0.06f;

        float _remaining, _duration, _strength;
        Vector3 _restPosition;
        bool _active;

        /// <summary>Largest offset a shake of this strength may produce right now (0 when motion is reduced).</summary>
        public static float MaxOffset(float strength) => Mathf.Clamp01(strength) * MaxOffsetMetres * Accessibility.MotionScale;

        public bool IsShaking => _active;

        public void Shake(float strength, float durationSeconds)
        {
            if (MaxOffset(strength) <= 0f || durationSeconds <= 0f) return;   // nothing to do, and nothing to restore
            if (!_active) _restPosition = transform.localPosition;
            _strength = strength;
            _duration = _remaining = durationSeconds;
            _active = true;
        }

        void LateUpdate()
        {
            if (!_active) return;
            _remaining -= Time.unscaledDeltaTime;
            if (_remaining <= 0f)
            {
                transform.localPosition = _restPosition;
                _active = false;
                return;
            }
            float fade = _remaining / _duration;
            var jitter = new Vector3(Random.Range(-1f, 1f), Random.Range(-1f, 1f), 0f) * (MaxOffset(_strength) * fade);
            transform.localPosition = _restPosition + jitter;
        }

        void OnDisable()
        {
            if (_active) transform.localPosition = _restPosition;
            _active = false;
        }
    }
}
