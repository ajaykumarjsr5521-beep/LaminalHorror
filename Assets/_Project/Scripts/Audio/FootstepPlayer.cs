using System;
using UnityEngine;
using NocturneAnnex.Player;

namespace NocturneAnnex.Audio
{
    /// <summary>Plays a footstep cue for the floor surface under the player, one per stride walked. Crouched steps are quieter.</summary>
    public class FootstepPlayer : MonoBehaviour
    {
        public const float CrouchVolume = 0.5f;

        [Tooltip("Metres walked per footstep. Walk 3 m/s, sprint 5, crouch 1.5: about 0.4, 0.33 and 0.5 s between steps.")]
        public float WalkStride = 1.2f, SprintStride = 1.65f, CrouchStride = 0.75f;

        public PlayerMotor Motor;
        public AudioDirector Director;

        readonly FootstepTimer _timer = new FootstepTimer();
        CharacterController _cc;

        public string LastCueId { get; private set; } = "";
        public event Action<string> StepTaken;

        void Awake()
        {
            _cc = GetComponent<CharacterController>();
            if (Motor == null) Motor = GetComponent<PlayerMotor>();
        }

        void Update()
        {
            if (Motor == null) return;
            float distance = Motor.HorizontalVelocity.magnitude * Time.deltaTime;
            Advance(distance, _cc == null || _cc.isGrounded, Motor.IsCrouched, Motor.IsSprinting);
        }

        /// <summary>One step of movement. Public so tests can drive it without input.</summary>
        public void Advance(float distance, bool grounded, bool crouched, bool sprinting)
        {
            float stride = crouched ? CrouchStride : sprinting ? SprintStride : WalkStride;
            if (!_timer.Advance(distance, grounded, stride)) return;
            string cue = FloorSurface.CueFor(CurrentSurface());
            LastCueId = cue;
            if (Director != null) Director.Play(cue, null, false, crouched ? CrouchVolume : 1f);
            StepTaken?.Invoke(cue);
        }

        /// <summary>Surface name of the floor under the player, or the default when nothing marked is below.</summary>
        public string CurrentSurface()
        {
            Vector3 origin = _cc != null ? _cc.bounds.center : transform.position + Vector3.up * 0.5f;
            if (Physics.Raycast(origin, Vector3.down, out var hit, 3f, ~0, QueryTriggerInteraction.Ignore))
            {
                var floor = hit.collider.GetComponentInParent<FloorSurface>();
                if (floor != null) return FloorSurface.Resolve(floor.Surface);
            }
            return FloorSurface.Default;
        }
    }
}
