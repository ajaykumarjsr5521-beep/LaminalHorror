using System;

namespace NocturneAnnex.Core
{
    /// <summary>The numbers on the ending card.</summary>
    public readonly struct EndingStats
    {
        public readonly float Seconds;
        public readonly int Deaths, LivesLeft, MaxLives, Rooms, TotalRooms, Secrets;

        public EndingStats(float seconds, int deaths, int livesLeft, int maxLives, int rooms, int totalRooms, int secrets)
        {
            Seconds = seconds; Deaths = deaths; LivesLeft = livesLeft; MaxLives = maxLives; Rooms = rooms; TotalRooms = totalRooms; Secrets = secrets;
        }
    }

    /// <summary>Formats the ending card from the run's numbers (F-14l). Pure, so a test can check the card matches the run.</summary>
    public static class EndingText
    {
        public static string FormatTime(float seconds)
        {
            if (float.IsNaN(seconds) || float.IsInfinity(seconds) || seconds < 0f) seconds = 0f;
            int total = (int)seconds;
            int h = total / 3600, m = total / 60 % 60, s = total % 60;
            return h > 0 ? $"{h}:{m:00}:{s:00}" : $"{m}:{s:00}";
        }

        /// <summary>The stat lines in display order, each already localised.</summary>
        public static string[] Lines(EndingStats s) => new[]
        {
            Loc.Format("ending.time", FormatTime(s.Seconds)),
            Loc.Format("ending.deaths", s.Deaths),
            Loc.Format("ending.lives", s.LivesLeft, s.MaxLives),
            Loc.Format("ending.rooms", s.Rooms, s.TotalRooms),
            Loc.Format("ending.secrets", s.Secrets),
        };
    }
}
