using System;
using System.Collections.Generic;
using UnityEngine;

namespace NocturneAnnex.Strategy
{
    [Serializable]
    public sealed class HorrorSuggestionDto
    {
        public string request_id, event_name, line_id, line, reason_code;
        public float tension;
    }

    public enum GateResult { Accepted, UnknownEvent, Paused, TooSoon, BadLine }

    /// <summary>
    /// Last check before the server's horror suggestion reaches gameplay. Only catalogue names pass, scares keep a minimum gap
    /// (the same 45 s as TensionDirector), and the contextual line is limited to a fixed list at one per ten minutes.
    /// DO_NOTHING and SILENCE are accepted as valid decisions that start no effect. Gameplay still decides how to play the event.
    /// </summary>
    public sealed class HorrorSuggestionGate
    {
        public const float MinScareGap = 45f;
        public const float LineInterval = 600f;

        static readonly HashSet<string> Scares = new HashSet<string>
        {
            "DISTANT_FOOTSTEPS", "FALSE_FOOTSTEPS", "LIGHT_FLICKER", "DOOR_MOVEMENT", "OBJECT_FALL", "SHADOW_EVENT",
            "DISTANT_BREATHING", "FALSE_ENTITY_SIGHTING", "ENVIRONMENTAL_MOVEMENT", "ENTITY_APPEARANCE",
        };
        static readonly HashSet<string> Pauses = new HashSet<string> { "DO_NOTHING", "SILENCE" };
        static readonly Dictionary<string, string> Lines = new Dictionary<string, string>
        {
            { "AGAIN", "You again." }, { "REMEMBER", "I remember." }, { "HID_BEFORE", "You hid here before." },
            { "NOT_THIS_TIME", "Not this time." }, { "FOUND", "Found you." },
        };

        float _lastScare = float.NegativeInfinity, _lastLine = float.NegativeInfinity;

        public string AcceptedEvent { get; private set; }
        public string AcceptedLine { get; private set; }
        public bool IsPause { get; private set; }

        /// <summary>Checks a suggestion. On Accepted, AcceptedEvent is the catalogue name and AcceptedLine is the text or null.</summary>
        public GateResult Check(HorrorSuggestionDto s, float now)
        {
            AcceptedEvent = null; AcceptedLine = null; IsPause = false;
            if (s == null || string.IsNullOrEmpty(s.event_name)) return GateResult.UnknownEvent;
            bool pause = Pauses.Contains(s.event_name);
            if (!pause && !Scares.Contains(s.event_name)) return GateResult.UnknownEvent;
            if (!pause && now - _lastScare < MinScareGap) return GateResult.TooSoon;

            string text = null;
            if (!string.IsNullOrEmpty(s.line_id))
            {
                // The text always comes from the local fixed list; whatever the server sent as text is ignored.
                if (!Lines.TryGetValue(s.line_id, out text)) return GateResult.BadLine;
                if (pause || now - _lastLine < LineInterval) text = null;
            }

            if (!pause) _lastScare = now;
            if (text != null) _lastLine = now;
            AcceptedEvent = s.event_name; AcceptedLine = text; IsPause = pause;
            return GateResult.Accepted;
        }

        public static HorrorSuggestionDto Parse(string json)
        {
            if (string.IsNullOrWhiteSpace(json)) return null;
            try
            {
                // Server field is "event"; the C# keyword-free mirror is event_name, so rename before parsing.
                return JsonUtility.FromJson<HorrorSuggestionDto>(json.Replace("\"event\":", "\"event_name\":"));
            }
            catch (Exception) { return null; }
        }
    }
}
