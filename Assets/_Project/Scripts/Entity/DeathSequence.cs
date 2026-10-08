using System;

namespace NocturneAnnex.Entity
{
    public enum DeathPhase { Reveal, Approach, Strike, Black, Silence, Done }

    /// <summary>
    /// Timeline of the short death sequence: the entity is revealed, closes in, strikes, the screen goes black, then silence.
    /// 3.6 s in full, and a 1 s black-only version for players who skip it. Pure data so the length rule is testable.
    /// </summary>
    public class DeathSequence
    {
        public const float MaxSeconds = 5f;

        static readonly float[] FullLengths = { 0.8f, 0.8f, 0.4f, 1.0f, 0.6f };
        static readonly float[] ShortLengths = { 0f, 0f, 0f, 1.0f, 0f };

        readonly float[] _lengths;

        public float Total { get; }

        public DeathSequence(bool shortVersion = false)
        {
            _lengths = shortVersion ? ShortLengths : FullLengths;
            float t = 0f;
            foreach (var l in _lengths) t += l;
            Total = t;
        }

        public DeathPhase PhaseAt(float seconds)
        {
            if (seconds < 0f) seconds = 0f;
            float start = 0f;
            for (int i = 0; i < _lengths.Length; i++)
            {
                if (_lengths[i] <= 0f) continue;
                if (seconds < start + _lengths[i]) return (DeathPhase)i;
                start += _lengths[i];
            }
            return DeathPhase.Done;
        }

        /// <summary>How black the screen is, 0 clear to 1 black. Rises slowly, then goes black at the strike and stays.</summary>
        public float BlackAt(float seconds)
        {
            switch (PhaseAt(seconds))
            {
                case DeathPhase.Reveal: return 0.1f * Progress(seconds, 0);
                case DeathPhase.Approach: return 0.1f + 0.4f * Progress(seconds, 1);
                case DeathPhase.Strike: return 0.5f + 0.5f * Progress(seconds, 2);
                default: return 1f;
            }
        }

        float Progress(float seconds, int phase)
        {
            float start = 0f;
            for (int i = 0; i < phase; i++) start += _lengths[i];
            return _lengths[phase] <= 0f ? 1f : Math.Max(0f, Math.Min(1f, (seconds - start) / _lengths[phase]));
        }
    }
}
