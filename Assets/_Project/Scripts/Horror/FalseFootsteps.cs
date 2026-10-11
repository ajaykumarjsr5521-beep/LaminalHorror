using System;

namespace NocturneAnnex.Horror
{
    /// <summary>
    /// A fake gait for the "footsteps that are not him" scare. The real Stilt-Walker alternates normal and wooden steps strictly, so the
    /// fake one always repeats a foot at least once and keeps uneven gaps; it can never be mistaken for the real rhythm by pattern.
    /// </summary>
    public static class FalseFootsteps
    {
        public readonly struct Step
        {
            public readonly bool Wood;
            public readonly float GapSeconds;   // wait after this step
            public Step(bool wood, float gap) { Wood = wood; GapSeconds = gap; }
        }

        public const float MinGap = 0.35f, MaxGap = 0.9f;

        public static Step[] Make(int seed)
        {
            var r = new Random(seed);
            int n = 4 + r.Next(3);   // 4 to 6 steps
            var wood = new bool[n];
            wood[0] = r.Next(2) == 0;
            for (int i = 1; i < n; i++) wood[i] = r.NextDouble() < 0.5 ? wood[i - 1] : !wood[i - 1];
            if (!HasRepeat(wood)) wood[n - 1] = wood[n - 2];   // never a clean alternation
            var steps = new Step[n];
            for (int i = 0; i < n; i++) steps[i] = new Step(wood[i], MinGap + (float)r.NextDouble() * (MaxGap - MinGap));
            return steps;
        }

        public static bool HasRepeat(bool[] wood)
        {
            for (int i = 1; i < wood.Length; i++) if (wood[i] == wood[i - 1]) return true;
            return false;
        }

        public static bool HasRepeat(Step[] steps)
        {
            for (int i = 1; i < steps.Length; i++) if (steps[i].Wood == steps[i - 1].Wood) return true;
            return false;
        }

        /// <summary>The cue id for a fake step, using the same recorded variants as the real entity (so it sounds right, only the pattern is wrong).</summary>
        public static string CueFor(Step s, int variant) => "entity.step." + (s.Wood ? "wood" : "normal") + "." + Math.Max(0, Math.Min(3, variant));
    }
}
