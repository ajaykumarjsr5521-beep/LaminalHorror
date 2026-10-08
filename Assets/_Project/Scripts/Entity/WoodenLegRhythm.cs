using System;

namespace NocturneAnnex.Entity
{
    public enum Foot { Normal, Wood }

    /// <summary>One footstep of the entity: which foot, which of the 4 recorded variants, and how it should sound at this distance.</summary>
    public readonly struct EntityStep
    {
        public readonly Foot Foot;
        public readonly int Variant;
        public readonly float Gain;
        public readonly float LowPassHz;

        public EntityStep(Foot foot, int variant, float gain, float lowPassHz)
        {
            Foot = foot; Variant = variant; Gain = gain; LowPassHz = lowPassHz;
        }
    }

    /// <summary>
    /// The entity's signature gait: normal foot, then wooden leg, always alternating, with 4 variants per foot and never the
    /// same variant twice in a row on a foot. Distance picks one of three bands: far is quiet and dull, medium is clear,
    /// near is loud and bright. The caller supplies a random value so tests stay deterministic.
    /// </summary>
    public class WoodenLegRhythm
    {
        public const int Variants = 4;
        public const float NearMeters = 8f, FarMeters = 22f;

        readonly int[] _last = { -1, -1 };
        Foot _next = Foot.Normal;

        public Foot NextFoot => _next;

        public static float GainFor(float distance) => distance <= NearMeters ? 1f : (distance >= FarMeters ? 0.2f : 0.55f);

        public static float LowPassFor(float distance) => distance <= NearMeters ? 22000f : (distance >= FarMeters ? 700f : 3500f);

        /// <param name="random01">0..1, picks the variant.</param>
        public EntityStep Next(float distanceToListener, float random01)
        {
            var foot = _next;
            _next = foot == Foot.Normal ? Foot.Wood : Foot.Normal;
            int idx = (int)foot;
            int v = Math.Min(Variants - 1, Math.Max(0, (int)(random01 * Variants)));
            if (v == _last[idx]) v = (v + 1) % Variants;
            _last[idx] = v;
            return new EntityStep(foot, v, GainFor(distanceToListener), LowPassFor(distanceToListener));
        }

        /// <summary>The cue id for a step, e.g. entity.step.wood.2. The cue catalog registers all 8.</summary>
        public static string CueFor(EntityStep s) => "entity.step." + (s.Foot == Foot.Wood ? "wood" : "normal") + "." + s.Variant;
    }
}
