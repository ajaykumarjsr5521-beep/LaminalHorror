using System;
using UnityEngine;
using NocturneAnnex.Controls;

namespace NocturneAnnex.Entity
{
    /// <summary>Carries at most one bottle and throws it where the player looks. Throwing with an empty hand does nothing.</summary>
    public class PlayerThrower : MonoBehaviour
    {
        public Transform Aim;
        public float Speed = 9f;
        public float Lift = 2f;

        /// <summary>Overridable for tests; defaults to the scene InputRouter.</summary>
        public Func<PlayerInputState> InputProvider;

        public ThrowableBottle Held { get; private set; }

        public bool TryHold(ThrowableBottle bottle)
        {
            if (Held != null || bottle == null) return false;
            Held = bottle;
            bottle.SetHeld(true);
            return true;
        }

        public bool Throw()
        {
            if (Held == null || Aim == null) return false;
            var b = Held; Held = null;
            b.Launch(Aim.position + Aim.forward * 0.5f, Aim.forward * Speed + Vector3.up * Lift);
            return true;
        }

        void Update()
        {
            var s = InputProvider != null ? InputProvider()
                  : (InputRouter.Instance != null ? InputRouter.Instance.Current : default);
            if (s.ThrowPressed) Throw();
        }
    }
}
