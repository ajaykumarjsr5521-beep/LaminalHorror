using System;
using System.Collections.Generic;
using UnityEngine;

namespace NocturneAnnex.Audio
{
    /// <summary>Authored list of every cue. Clips are assigned in the asset; ids, groups and caption keys come from the spec (doc 05, F-10).</summary>
    [CreateAssetMenu(menuName = "Nocturne Annex/Cue Catalog", fileName = "CueCatalog")]
    public class CueCatalog : ScriptableObject
    {
        public const string Music = "Music", Ambience = "Ambience", Sfx = "Sfx";
        public static readonly string[] Surfaces = { "tile", "carpet", "concrete" };
        public static readonly string[] Zones = { "hall", "stacks", "records", "break_room", "dock" };

        public List<CueDefinition> Cues = new List<CueDefinition>();

        public CueDefinition Find(string id)
        {
            foreach (var c in Cues) if (c != null && c.Id == id) return c;
            return null;
        }

        /// <summary>Problems that make the catalog unusable or break the caption rule. Empty means valid.</summary>
        /// <param name="captionKeyExists">Whether a caption string key exists, normally Loc.Has.</param>
        public List<string> Validate(Func<string, bool> captionKeyExists)
        {
            var problems = new List<string>();
            var seen = new HashSet<string>();
            foreach (var c in Cues)
            {
                if (c == null) { problems.Add("a cue entry is empty"); continue; }
                if (string.IsNullOrEmpty(c.Id)) { problems.Add("a cue has no id"); continue; }
                if (!seen.Add(c.Id)) problems.Add($"duplicate cue id '{c.Id}'");
                if (c.Group != Music && c.Group != Ambience && c.Group != Sfx) problems.Add($"cue '{c.Id}' has unknown mixer group '{c.Group}'");
                if (c.MinVolume > c.MaxVolume) problems.Add($"cue '{c.Id}' has a minimum volume above its maximum");
                bool hasKey = !string.IsNullOrEmpty(c.CaptionKey);
                if (c.GameplayRelevant && !hasKey) problems.Add($"gameplay-relevant cue '{c.Id}' has no caption key");
                if (hasKey && !captionKeyExists(c.CaptionKey)) problems.Add($"cue '{c.Id}' uses caption key '{c.CaptionKey}' that does not exist");
            }
            return problems;
        }

        /// <summary>The cue list from the F-10 spec, without clips. Used to build the asset and by tests.</summary>
        public static CueCatalog CreateDefault()
        {
            var cat = CreateInstance<CueCatalog>();
            foreach (var id in new[] { "light_buzz", "door_slam", "prop_shift", "whisper", "misfile", "figure" })
                cat.Cues.Add(new CueDefinition("event." + id, Sfx, true, "event." + id));
            cat.Cues.Add(new CueDefinition("checkpoint.chime", Sfx, true, "level.checkpoint_saved"));
            foreach (var id in new[] { "door.open", "door.close", "door.locked", "pickup.take", "note.open" })
                cat.Cues.Add(new CueDefinition(id, Sfx, false));
            foreach (var s in Surfaces) cat.Cues.Add(new CueDefinition("footstep." + s, Sfx, false));
            foreach (var z in Zones) cat.Cues.Add(new CueDefinition("amb." + z, Ambience, false));
            cat.Cues.Add(new CueDefinition("music.drone", Music, false));
            return cat;
        }
    }
}
