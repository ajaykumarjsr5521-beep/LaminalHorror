using UnityEngine;

namespace NocturneAnnex.Horror
{
    /// <summary>Small view checks for scares that wait until the player looks away.</summary>
    public static class ViewMath
    {
        /// <summary>True when the target lies within halfFovDegrees of the view direction. A target at the viewer's own position counts as in view.</summary>
        public static bool InView(Vector3 viewerPosition, Vector3 viewerForward, Vector3 target, float halfFovDegrees)
        {
            var to = target - viewerPosition;
            if (to.sqrMagnitude < 0.0001f) return true;
            return Vector3.Angle(viewerForward, to) <= halfFovDegrees;
        }
    }
}
