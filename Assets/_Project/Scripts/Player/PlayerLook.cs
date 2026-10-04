using System;
using UnityEngine;
using NocturneAnnex.Controls;

namespace NocturneAnnex.Player
{
    /// <summary>Yaw rotates the body, pitch rotates the camera pivot. Zero input means zero rotation.</summary>
    public class PlayerLook : MonoBehaviour
    {
        public Transform CameraPivot;
        public float MaxPitch = 85f;

        /// <summary>Overridable for tests; defaults to the scene InputRouter.</summary>
        public Func<PlayerInputState> InputProvider;

        float _pitch;

        void Update()
        {
            var look = (InputProvider ?? DefaultProvider)().Look;
            transform.Rotate(0f, look.x, 0f, Space.Self);
            _pitch = Mathf.Clamp(_pitch - look.y, -MaxPitch, MaxPitch);
            if (CameraPivot != null) CameraPivot.localRotation = Quaternion.Euler(_pitch, 0f, 0f);
        }

        static PlayerInputState DefaultProvider() =>
            InputRouter.Instance != null ? InputRouter.Instance.Current : default;
    }
}
