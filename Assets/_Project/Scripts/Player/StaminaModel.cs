using UnityEngine;

namespace NocturneAnnex.Player
{
    /// <summary>Sprint stamina. After running dry the player must recover to a threshold before sprinting again.</summary>
    public class StaminaModel
    {
        public float Max = 100f;
        public float DrainPerSecond = 25f;
        public float RegenPerSecond = 20f;
        public float RegenDelaySeconds = 1f;
        public float ResumeThreshold = 25f;

        public float Current { get; private set; }
        public bool Exhausted { get; private set; }
        public bool CanSprint => !Exhausted && Current > 0f;

        float _secondsSinceSprint;

        public StaminaModel() { Current = Max; }

        public void Tick(float dt, bool sprinting)
        {
            if (sprinting && CanSprint)
            {
                _secondsSinceSprint = 0f;
                Current = Mathf.Max(0f, Current - DrainPerSecond * dt);
                if (Current <= 0f) Exhausted = true;
                return;
            }

            _secondsSinceSprint += dt;
            if (_secondsSinceSprint >= RegenDelaySeconds)
                Current = Mathf.Min(Max, Current + RegenPerSecond * dt);
            if (Exhausted && Current >= ResumeThreshold) Exhausted = false;
        }
    }
}
