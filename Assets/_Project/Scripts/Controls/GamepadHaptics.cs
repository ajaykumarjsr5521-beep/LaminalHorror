using UnityEngine;
using UnityEngine.InputSystem;
using NocturneAnnex.Core;

namespace NocturneAnnex.Controls
{
    /// <summary>Gamepad rumble through the Input System. Call Tick every frame so a pulse stops when its time is up.</summary>
    public sealed class GamepadHaptics : IHapticsDevice
    {
        float _stopAt = -1f;

        public void Pulse(HapticEvent kind, HapticPulse pulse)
        {
            var pad = Gamepad.current;
            if (pad == null) return;
            pad.SetMotorSpeeds(pulse.Low, pulse.High);
            _stopAt = Time.unscaledTime + pulse.Seconds;
        }

        public void Tick(float now)
        {
            if (_stopAt < 0f || now < _stopAt) return;
            Stop();
        }

        public void Stop()
        {
            _stopAt = -1f;
            Gamepad.current?.SetMotorSpeeds(0f, 0f);
        }
    }
}
