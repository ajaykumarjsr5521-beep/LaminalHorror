using UnityEngine;

namespace NocturneAnnex.Entity
{
    /// <summary>One sound the player (or a thrown object) made. Radius already includes Story mode.</summary>
    public readonly struct NoiseEvent
    {
        public readonly Vector3 Position;
        public readonly NoiseKind Kind;
        public readonly float Radius;
        public readonly float Time;

        public NoiseEvent(Vector3 position, NoiseKind kind, float radius, float time)
        {
            Position = position; Kind = kind; Radius = radius; Time = time;
        }

        public static NoiseEvent Make(Vector3 position, NoiseKind kind, float time, bool story = false) =>
            new NoiseEvent(position, kind, NoiseTable.Radius(kind, story), time);
    }
}
