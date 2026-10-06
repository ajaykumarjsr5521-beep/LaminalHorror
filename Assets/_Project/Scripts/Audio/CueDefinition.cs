using System;
using UnityEngine;

namespace NocturneAnnex.Audio
{
    /// <summary>One sound the game can play by id. A cue the player did not cause must carry a caption key.</summary>
    [Serializable]
    public class CueDefinition
    {
        public string Id = "";
        public AudioClip[] Clips = new AudioClip[0];
        [Tooltip("Name of the mixer group: Music, Ambience or Sfx.")] public string Group = "Sfx";
        [Tooltip("True when the cue tells the player something they did not cause. It then needs a caption key.")] public bool GameplayRelevant;
        [Tooltip("Caption string key in DefaultStrings. Empty only for cues that are not gameplay-relevant.")] public string CaptionKey = "";
        [Range(0f, 1f)] public float MinVolume = 0.8f;
        [Range(0f, 1f)] public float MaxVolume = 1f;

        public CueDefinition() { }

        public CueDefinition(string id, string group, bool gameplayRelevant, string captionKey = "")
        {
            Id = id;
            Group = group;
            GameplayRelevant = gameplayRelevant;
            CaptionKey = captionKey;
        }
    }
}
