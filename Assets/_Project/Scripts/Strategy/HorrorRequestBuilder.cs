using System;
using UnityEngine;

namespace NocturneAnnex.Strategy
{
    /// <summary>Signals for one horror request. All 0..1 except the bool, seconds and counts. No positions, by design.</summary>
    public struct HorrorInputs
    {
        public float Stress, EntityProximity, Darkness, RecentNoise, ChasePressure, Isolation;
        public bool EntityVisible, InSafeZone;
        public float SecondsSinceScare;
        public int RecentScares;
    }

    /// <summary>Builds the /horror request body. Every value is clamped to the server's range so a bad signal cannot get the request rejected.</summary>
    public static class HorrorRequestBuilder
    {
        [Serializable]
        class Body
        {
            public string request_id;
            public float stress, entity_proximity, darkness, recent_noise, chase_pressure, isolation;
            public bool entity_visible, in_safe_zone;
            public float seconds_since_scare;
            public int recent_scares, death_count;
            public float memory_pressure, hide_success_rate;
        }

        public static string Build(string requestId, HorrorInputs i, PlayerProfile profile, float memoryPressure = 0f)
        {
            var b = new Body
            {
                request_id = requestId,
                stress = Unit(i.Stress), entity_proximity = Unit(i.EntityProximity), darkness = Unit(i.Darkness),
                recent_noise = Unit(i.RecentNoise), chase_pressure = Unit(i.ChasePressure), isolation = Unit(i.Isolation),
                entity_visible = i.EntityVisible, in_safe_zone = i.InSafeZone,
                seconds_since_scare = Mathf.Max(0f, Finite(i.SecondsSinceScare)),
                recent_scares = Mathf.Clamp(i.RecentScares, 0, 20),
                death_count = profile == null ? 0 : Mathf.Max(0, profile.DeathCount),
                memory_pressure = Unit(memoryPressure),
                hide_success_rate = profile == null ? 0f : Unit(profile.HideSuccessRate),
            };
            return JsonUtility.ToJson(b);
        }

        static float Finite(float v) => float.IsNaN(v) || float.IsInfinity(v) ? 0f : v;
        static float Unit(float v) => Mathf.Clamp01(Finite(v));
    }
}
