using UnityEngine;

namespace NocturneAnnex.Entity
{
    /// <summary>A loose object that makes noise when it hits something fast enough: dropped is Drop, thrown is ThrownImpact.</summary>
    [RequireComponent(typeof(Rigidbody))]
    public class NoisyProp : MonoBehaviour
    {
        public NoiseHub Hub;
        public bool WasThrown { get; protected set; }

        /// <summary>Ends the start-of-level quiet period, e.g. when the player picks the prop up and throws it.</summary>
        protected void Arm() => _quietUntil = 0f;

        float _lastImpact = -999f;
        float _quietUntil;

        /// <summary>Props placed in the air settle at level start; that is not the player's doing, so it stays silent.</summary>
        void Start() => _quietUntil = Time.time + 1.5f;

        void OnCollisionEnter(Collision c)
        {
            if (Hub == null || Time.time < _quietUntil) return;
            float speed = c.relativeVelocity.magnitude;
            if (!ImpactNoiseRule.TryGetKind(speed, WasThrown, Time.time - _lastImpact, out var kind)) return;
            _lastImpact = Time.time;
            Hub.Emit(c.contactCount > 0 ? c.GetContact(0).point : transform.position, kind);
            WasThrown = false;   // later bounces count as drops
        }
    }
}
