using UnityEngine;

namespace NocturneAnnex.Entity
{
    /// <summary>A loose object that makes noise when it hits something fast enough: dropped is Drop, thrown is ThrownImpact.</summary>
    [RequireComponent(typeof(Rigidbody))]
    public class NoisyProp : MonoBehaviour
    {
        public NoiseHub Hub;
        public bool WasThrown { get; protected set; }

        float _lastImpact = -999f;

        void OnCollisionEnter(Collision c)
        {
            if (Hub == null) return;
            float speed = c.relativeVelocity.magnitude;
            if (!ImpactNoiseRule.TryGetKind(speed, WasThrown, Time.time - _lastImpact, out var kind)) return;
            _lastImpact = Time.time;
            Hub.Emit(c.contactCount > 0 ? c.GetContact(0).point : transform.position, kind);
            WasThrown = false;   // later bounces count as drops
        }
    }
}
