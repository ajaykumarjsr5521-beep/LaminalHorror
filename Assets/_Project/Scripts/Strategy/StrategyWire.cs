using System;
using System.Collections.Generic;
using UnityEngine;

namespace NocturneAnnex.Strategy
{
    [Serializable] public struct KeyShare { public string key; public float value; }

    /// <summary>JSON body sent to the server. Snake_case field names match the Pydantic models. Categories and ratios only.</summary>
    [Serializable]
    public sealed class StrategyRequestDto
    {
        public string request_id;
        public string trigger;
        public float run_frequency, walk_frequency, crouch_frequency, noise_frequency, risk_tolerance;
        public float average_chase_seconds, puzzle_seconds, hide_success_rate;
        public int death_count, hide_count;
        public List<KeyShare> hide_usage = new List<KeyShare>();
        public List<KeyShare> route_usage = new List<KeyShare>();
        public List<KeyShare> room_time = new List<KeyShare>();

        public static StrategyRequestDto From(PlayerProfile p, string requestId, string trigger)
        {
            var d = new StrategyRequestDto
            {
                request_id = requestId, trigger = trigger,
                run_frequency = p.RunFrequency, walk_frequency = p.WalkFrequency, crouch_frequency = p.CrouchFrequency,
                noise_frequency = p.NoiseFrequency, risk_tolerance = p.RiskTolerance,
                average_chase_seconds = p.AverageChaseSeconds, puzzle_seconds = p.PuzzleSeconds, hide_success_rate = p.HideSuccessRate,
                death_count = p.DeathCount, hide_count = p.HideCount,
            };
            foreach (var kv in p.HideUsageByType) d.hide_usage.Add(new KeyShare { key = kv.Key, value = kv.Value });
            foreach (var kv in p.RouteUsage) d.route_usage.Add(new KeyShare { key = kv.Key, value = kv.Value });
            foreach (var kv in p.RoomTimeShare) d.room_time.Add(new KeyShare { key = kv.Key, value = kv.Value });
            return d;
        }
    }

    /// <summary>JSON the server returns. The strategy is a string from a closed list; anything else is rejected.</summary>
    [Serializable]
    public sealed class StrategyResponseDto
    {
        public string request_id;
        public string strategy;
        public float priority, intensity, duration_seconds, confidence;
        public string reason_code;
    }

    public static class StrategyWire
    {
        static readonly Dictionary<string, StrategyType> Names = new Dictionary<string, StrategyType>
        {
            { "NONE", StrategyType.None },
            { "INCREASE_HIDING_PRESSURE", StrategyType.IncreaseHidingPressure },
            { "INCREASE_INVESTIGATION", StrategyType.IncreaseInvestigation },
            { "CHANGE_PATROL_PREFERENCE", StrategyType.ChangePatrolPreference },
            { "CHANGE_SEARCH_PRIORITY", StrategyType.ChangeSearchPriority },
            { "RELAX_PRESSURE", StrategyType.RelaxPressure },
        };

        public static string ToJson(StrategyRequestDto d) => JsonUtility.ToJson(d);

        /// <summary>Parses a response body. Returns false (and the reason) on bad JSON, missing or unknown strategy name.</summary>
        public static bool TryParse(string body, out string requestId, out StrategyCommand cmd, out string error)
        {
            requestId = null; cmd = default; error = null;
            if (string.IsNullOrWhiteSpace(body)) { error = "empty body"; return false; }
            StrategyResponseDto d;
            try { d = JsonUtility.FromJson<StrategyResponseDto>(body); }
            catch (Exception e) { error = "bad json: " + e.Message; return false; }
            if (d == null || string.IsNullOrEmpty(d.strategy)) { error = "missing strategy"; return false; }
            if (!Names.TryGetValue(d.strategy, out var type)) { error = "unknown strategy"; return false; }
            requestId = d.request_id;
            cmd = new StrategyCommand
            {
                Strategy = type, Priority = d.priority, Intensity = d.intensity, DurationSeconds = d.duration_seconds,
                Confidence = d.confidence, ReasonCode = d.reason_code,
            };
            return true;
        }
    }
}
