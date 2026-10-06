using UnityEngine;

namespace NocturneAnnex.Core
{
    /// <summary>Anything that can play a sound cue by id. The audio director implements it.</summary>
    public interface ICuePlayer
    {
        /// <summary>Plays the cue, 3D at the position or 2D without one. False if it could not play (unknown cue, no clip, no free voice).</summary>
        bool Play(string cueId, Vector3? position = null);
    }

    /// <summary>
    /// One-line way to play a cue from any assembly: Cues.Play("event.door_slam", pos). Does nothing when no audio director
    /// is active (menus, tests without audio). Mirrors <see cref="Captions"/>.
    /// </summary>
    public static class Cues
    {
        public static ICuePlayer Player { get; set; }

        public static bool Play(string cueId, Vector3? position = null) => Player != null && Player.Play(cueId, position);

        // Keeps state correct when Enter Play Mode runs without a domain reload.
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        static void ResetOnLoad() => Player = null;
    }
}
