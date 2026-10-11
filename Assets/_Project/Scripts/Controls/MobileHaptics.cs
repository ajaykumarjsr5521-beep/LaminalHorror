using UnityEngine;
using NocturneAnnex.Core;

namespace NocturneAnnex.Controls
{
    /// <summary>
    /// Phone vibration. Handheld.Vibrate has a fixed length, so only the strong events (hit and scare) use it; footsteps and heartbeat would
    /// be a constant buzz and a battery drain. Does nothing outside Android and iOS.
    /// </summary>
    public sealed class MobileHaptics : IHapticsDevice
    {
        public static bool Supported =>
#if UNITY_ANDROID || UNITY_IOS
            true;
#else
            false;
#endif

        public void Pulse(HapticEvent kind, HapticPulse pulse)
        {
            if (kind != HapticEvent.Hit && kind != HapticEvent.Scare) return;
#if UNITY_ANDROID || UNITY_IOS
            Handheld.Vibrate();
#endif
        }
    }
}
