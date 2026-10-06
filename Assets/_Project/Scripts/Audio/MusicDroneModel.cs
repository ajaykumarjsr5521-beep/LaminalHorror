using System;

namespace NocturneAnnex.Audio
{
    /// <summary>
    /// Volume of the music drone: it follows tension between a quiet floor and full level, moves smoothly,
    /// and falls to silence while the game is blocked (paused or a modal screen open). Plain C#.
    /// </summary>
    public class MusicDroneModel
    {
        public float MinLevel = 0.15f;
        public float MaxLevel = 1f;
        public float RisePerSecond = 0.4f;
        public float FallPerSecond = 1.5f;

        public float Level { get; private set; }

        public float Target(float tension, bool blocked)
        {
            if (blocked) return 0f;
            float t = Math.Max(0f, Math.Min(1f, tension));
            return MinLevel + (MaxLevel - MinLevel) * t;
        }

        /// <summary>Moves the level toward its target and returns it.</summary>
        public float Tick(float deltaSeconds, float tension, bool blocked)
        {
            float target = Target(tension, blocked);
            float rate = (target > Level ? RisePerSecond : FallPerSecond) * deltaSeconds;
            Level = target > Level ? Math.Min(target, Level + rate) : Math.Max(target, Level - rate);
            return Level;
        }
    }
}
