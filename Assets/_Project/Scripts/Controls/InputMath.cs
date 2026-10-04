using UnityEngine;

namespace NocturneAnnex.Controls
{
    public static class InputMath
    {
        public const float MinControlsScale = 0.8f;
        public const float MaxControlsScale = 1.3f;

        /// <summary>Radial deadzone, rescaled so output still spans 0..1 outside the zone.</summary>
        public static Vector2 ApplyDeadzone(Vector2 v, float deadzone)
        {
            float mag = v.magnitude;
            if (mag <= deadzone) return Vector2.zero;
            float scaled = Mathf.Min((mag - deadzone) / (1f - deadzone), 1f);
            return v / mag * scaled;
        }

        /// <summary>Converts a drag offset in pixels to a stick value with magnitude at most 1.</summary>
        public static Vector2 StickFromOffset(Vector2 offsetPixels, float radiusPixels)
        {
            if (radiusPixels <= 0f) return Vector2.zero;
            return Vector2.ClampMagnitude(offsetPixels / radiusPixels, 1f);
        }

        public static float ClampControlsScale(float scale) => Mathf.Clamp(scale, MinControlsScale, MaxControlsScale);
    }
}
