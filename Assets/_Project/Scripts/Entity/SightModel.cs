using UnityEngine;

namespace NocturneAnnex.Entity
{
    /// <summary>
    /// The entity's weak eyes: it sees the player only within a short range, only when the player is lit, and never through
    /// a wall or a closed door. The caller supplies the occlusion result (a raycast in the scene, a flag in tests).
    /// </summary>
    public static class SightModel
    {
        public const float MaxDistance = 6f;

        public static bool CanSee(Vector3 eye, Vector3 target, bool targetLit, bool blocked) =>
            !blocked && targetLit && Vector3.Distance(eye, target) <= MaxDistance;
    }
}
