using UnityEngine;
using NocturneAnnex.Audio;
using NocturneAnnex.Player;

namespace NocturneAnnex.Entity
{
    /// <summary>Turns each player footstep into a noise: crouch is quiet, sprint loud, and the floor surface scales it.</summary>
    public class PlayerNoiseEmitter : MonoBehaviour
    {
        public NoiseHub Hub;
        public FootstepPlayer Footsteps;
        public PlayerMotor Motor;

        void Awake()
        {
            if (Footsteps == null) Footsteps = GetComponent<FootstepPlayer>();
            if (Motor == null) Motor = GetComponent<PlayerMotor>();
        }

        void OnEnable() { if (Footsteps != null) Footsteps.StepTaken += OnStep; }
        void OnDisable() { if (Footsteps != null) Footsteps.StepTaken -= OnStep; }

        void OnStep(string cue)
        {
            if (Hub == null || Motor == null) return;
            var kind = NoiseTable.KindForMovement(Motor.IsCrouched, Motor.IsSprinting);
            string surface = Footsteps != null ? Footsteps.CurrentSurface() : FloorSurface.Default;
            Hub.Emit(transform.position, kind, NoiseTable.SurfaceMultiplier(surface));
        }
    }
}
