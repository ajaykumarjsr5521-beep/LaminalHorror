using UnityEngine;

namespace NocturneAnnex.Audio
{
    /// <summary>
    /// Heartbeat and ambience targets per danger level (0-4), from one data table, smoothed so any change settles within SettleSeconds.
    /// Hiding lowers ambience to HideAmbience so the heartbeat and breathing stand out. Pure C#: the player component reads the outputs.
    /// </summary>
    public sealed class HeartbeatMix
    {
        public const float SettleSeconds = 1f;
        public const float HideAmbience = 0.35f;

        // index = danger level
        public static readonly float[] HeartbeatGain = { 0f, 0.15f, 0.35f, 0.65f, 1f };
        public static readonly float[] BeatsPerMinute = { 60f, 70f, 85f, 110f, 140f };
        public static readonly float[] AmbienceGain = { 1f, 0.9f, 0.75f, 0.55f, 0.4f };

        public float Heartbeat { get; private set; }
        public float Bpm { get; private set; } = BeatsPerMinute[0];
        public float Ambience { get; private set; } = AmbienceGain[0];

        public static float TargetHeartbeat(int level) => HeartbeatGain[Clamp(level)];
        public static float TargetBpm(int level) => BeatsPerMinute[Clamp(level)];
        public static float TargetAmbience(int level, bool hiding) =>
            hiding ? Mathf.Min(AmbienceGain[Clamp(level)], HideAmbience) : AmbienceGain[Clamp(level)];

        public void Step(float dt, int level, bool hiding)
        {
            // Rates are range per settle time, so even the widest change finishes inside SettleSeconds.
            Heartbeat = Mathf.MoveTowards(Heartbeat, TargetHeartbeat(level), dt / SettleSeconds);
            Bpm = Mathf.MoveTowards(Bpm, TargetBpm(level), (BeatsPerMinute[4] - BeatsPerMinute[0]) * dt / SettleSeconds);
            Ambience = Mathf.MoveTowards(Ambience, TargetAmbience(level, hiding), dt / SettleSeconds);
        }

        /// <summary>Jumps to the targets (scene load, respawn).</summary>
        public void Snap(int level, bool hiding)
        {
            Heartbeat = TargetHeartbeat(level); Bpm = TargetBpm(level); Ambience = TargetAmbience(level, hiding);
        }

        static int Clamp(int level) => Mathf.Clamp(level, 0, 4);
    }
}
