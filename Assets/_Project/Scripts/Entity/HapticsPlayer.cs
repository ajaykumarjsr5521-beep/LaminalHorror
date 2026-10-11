using System.Collections.Generic;
using UnityEngine;
using NocturneAnnex.Audio;
using NocturneAnnex.Controls;
using NocturneAnnex.Core;
using NocturneAnnex.Horror;

namespace NocturneAnnex.Entity
{
    /// <summary>
    /// Turns game events into pulses (F-14k): the entity's footsteps when it is near, the heartbeat at danger level 2 and up, the catch, and
    /// scares. HapticsPlan decides what is sent (nothing when haptics are off or the strength is tiny) and HapticLimiter spaces the pulses.
    /// Nothing is sent while the game is paused.
    /// </summary>
    public sealed class HapticsPlayer : MonoBehaviour
    {
        public StalkerAgent Stalker;
        public HeartbeatPlayer Heartbeat;
        public HorrorEventRunner Horror;

        /// <summary>Tests add a fake device here; Awake adds the gamepad and the phone.</summary>
        public List<IHapticsDevice> Devices { get; } = new List<IHapticsDevice>();

        readonly HapticLimiter _limiter = new HapticLimiter();
        GamepadHaptics _pad;

        void Awake()
        {
            _pad = new GamepadHaptics();
            Devices.Add(_pad);
            if (MobileHaptics.Supported) Devices.Add(new MobileHaptics());
        }

        void OnEnable()
        {
            if (Stalker != null) { Stalker.StepTaken += OnStep; Stalker.CaughtPlayer += OnCaught; }
            if (Heartbeat != null) Heartbeat.Beat += OnBeat;
            if (Horror != null) Horror.EventFired += OnScare;
        }

        void OnDisable()
        {
            if (Stalker != null) { Stalker.StepTaken -= OnStep; Stalker.CaughtPlayer -= OnCaught; }
            if (Heartbeat != null) Heartbeat.Beat -= OnBeat;
            if (Horror != null) Horror.EventFired -= OnScare;
            _pad?.Stop();
        }

        void Update() => _pad?.Tick(Time.unscaledTime);

        /// <summary>The entity took a step this far from the player. Public so tests can drive it.</summary>
        public void OnStep(float distance)
        {
            float scale = HapticsPlan.FootstepScale(distance);
            if (scale > 0f) Play(HapticEvent.Footstep, scale);
        }

        public void OnBeat() { if (Heartbeat != null && Heartbeat.Level >= 2) Play(HapticEvent.Heartbeat); }
        public void OnCaught() => Play(HapticEvent.Hit);
        public void OnScare(string id) => Play(HapticEvent.Scare);

        /// <summary>Sends one pulse to every device if the settings allow it. Public so tests can drive it.</summary>
        public bool Play(HapticEvent kind, float scale = 1f)
        {
            if (Time.timeScale <= 0f) return false;
            var pulse = HapticsPlan.For(kind, HapticSettings.Enabled, HapticSettings.Strength * Mathf.Clamp01(scale));
            if (pulse == null) return false;
            if (!_limiter.TryAcquire(kind, Time.unscaledTime)) return false;
            foreach (var d in Devices) d.Pulse(kind, pulse.Value);
            return true;
        }
    }
}
