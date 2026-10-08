using System;

namespace NocturneAnnex.Entity
{
    public enum NoiseKind { Crouch, Walk, Run, DoorGentle, DoorSlam, Drop, ThrownImpact, Interaction, Machinery }

    /// <summary>How far each kind of player noise carries, in metres. Story mode halves every radius.</summary>
    public static class NoiseTable
    {
        public const float StoryMultiplier = 0.5f;

        public static float Radius(NoiseKind kind, bool story = false)
        {
            float r;
            switch (kind)
            {
                case NoiseKind.Crouch: r = 2f; break;
                case NoiseKind.Walk: r = 6f; break;
                case NoiseKind.Run: r = 14f; break;
                case NoiseKind.DoorGentle: r = 5f; break;
                case NoiseKind.DoorSlam: r = 20f; break;
                case NoiseKind.Drop: r = 12f; break;
                case NoiseKind.ThrownImpact: r = 16f; break;
                case NoiseKind.Interaction: r = 6f; break;
                case NoiseKind.Machinery: r = 30f; break;
                default: throw new ArgumentOutOfRangeException(nameof(kind));
            }
            return story ? r * StoryMultiplier : r;
        }

        /// <summary>The noise a footstep makes: crouching is quiet, sprinting loud, otherwise walking.</summary>
        public static NoiseKind KindForMovement(bool crouched, bool sprinting) =>
            crouched ? NoiseKind.Crouch : (sprinting ? NoiseKind.Run : NoiseKind.Walk);

        /// <summary>Floors change how far a step carries. Carpet muffles; a creaking floor is authored per room and carries farther.</summary>
        public static float SurfaceMultiplier(string surface)
        {
            switch (surface)
            {
                case "carpet": return 0.7f;
                case "creak": return 1.5f;
                default: return 1f;
            }
        }
    }
}
