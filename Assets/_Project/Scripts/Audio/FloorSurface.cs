using System;
using UnityEngine;

namespace NocturneAnnex.Audio
{
    /// <summary>Marks a floor collider (or its parent) with the surface that decides which footstep sounds play on it.</summary>
    public class FloorSurface : MonoBehaviour
    {
        public const string Default = "concrete";

        [Tooltip("tile, carpet or concrete. Anything else plays the default.")] public string Surface = Default;

        /// <summary>The surface to use for a name: itself when authored, otherwise the default.</summary>
        public static string Resolve(string surface) => Array.IndexOf(CueCatalog.Surfaces, surface) >= 0 ? surface : Default;

        /// <summary>Cue id for stepping on a surface, e.g. footstep.tile.</summary>
        public static string CueFor(string surface) => "footstep." + Resolve(surface);
    }
}
