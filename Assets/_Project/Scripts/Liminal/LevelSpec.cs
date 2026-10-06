using System.Collections.Generic;

namespace NocturneAnnex.Liminal
{
    /// <summary>The measurable facts of a level that the liminality checklist looks at. Filled from a scene or an asset by the caller.</summary>
    public class LevelSpec
    {
        public string Name = "";
        public bool HasHeroVista;
        public int LongestRoomDoorChain;
        public float MinPublicCeilingMetres = 5f;
        public float ShortestPublicCeilingMetres;
        public int RealtimeLights;
        public List<string> EntityCuesWithoutCaption = new List<string>();
        public List<string> MissingLegendEntries = new List<string>();
    }

    /// <summary>The liminality checklist (doc 15 section 6) as code: one message per broken rule, empty when the level passes.</summary>
    public static class LevelSpecValidator
    {
        public const int MaxRoomDoorChain = 2;
        public const int MaxRealtimeLights = 2;

        public static List<string> Validate(LevelSpec level)
        {
            var problems = new List<string>();
            string n = string.IsNullOrEmpty(level.Name) ? "level" : level.Name;
            if (!level.HasHeroVista) problems.Add($"{n}: no hero vista (a view of 40 m depth, two floors and three destinations)");
            if (level.LongestRoomDoorChain > MaxRoomDoorChain) problems.Add($"{n}: room-door-room chain of {level.LongestRoomDoorChain} is longer than {MaxRoomDoorChain}");
            if (level.ShortestPublicCeilingMetres < level.MinPublicCeilingMetres) problems.Add($"{n}: public ceiling {level.ShortestPublicCeilingMetres} m is below the {level.MinPublicCeilingMetres} m minimum");
            if (level.RealtimeLights > MaxRealtimeLights) problems.Add($"{n}: {level.RealtimeLights} realtime lights exceed the budget of {MaxRealtimeLights}");
            foreach (var cue in level.EntityCuesWithoutCaption) problems.Add($"{n}: entity cue '{cue}' has no caption");
            foreach (var id in level.MissingLegendEntries) problems.Add($"{n}: legend entry '{id}' is referenced but missing");
            return problems;
        }
    }
}
