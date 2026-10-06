using System.IO;
using System.Linq;
using UnityEditor;
using UnityEngine;
using NocturneAnnex.Audio;
using NocturneAnnex.Player;

namespace NocturneAnnex.Editor
{
    /// <summary>Creates the cue catalog asset and wires the audio director and footsteps into Level_B1. Clips are assigned in the asset later.</summary>
    public static class LevelAudio
    {
        const string Dir = "Assets/_Project/Data/Audio";
        public const string CatalogPath = Dir + "/CueCatalog.asset";

        /// <summary>Call before the scene is created: scene creation unloads unused assets. Keeps clips already assigned and adds any missing default cue.</summary>
        public static void CreateAssets()
        {
            Directory.CreateDirectory(Dir);
            var catalog = AssetDatabase.LoadAssetAtPath<CueCatalog>(CatalogPath);
            if (catalog == null)
            {
                catalog = ScriptableObject.CreateInstance<CueCatalog>();
                AssetDatabase.CreateAsset(catalog, CatalogPath);
            }
            var defaults = CueCatalog.CreateDefault();
            foreach (var cue in defaults.Cues)
                if (catalog.Find(cue.Id) == null) catalog.Cues.Add(cue);
            Object.DestroyImmediate(defaults);
            EditorUtility.SetDirty(catalog);
            AssetDatabase.SaveAssets();
        }

        /// <summary>Call after the player exists.</summary>
        public static void Build(GameObject player)
        {
            var root = new GameObject("Audio");
            var director = root.AddComponent<AudioDirector>();
            director.Catalog = AssetDatabase.LoadAssetAtPath<CueCatalog>(CatalogPath);
            var steps = player.AddComponent<FootstepPlayer>();
            steps.Director = director;
            steps.Motor = player.GetComponent<PlayerMotor>();
            root.AddComponent<MusicDrone>().Director = director;
        }

        /// <summary>Footstep surface for a level area: carpet in the stacks and records, concrete on the dock, tile elsewhere.</summary>
        public static string SurfaceFor(string area)
        {
            switch (area)
            {
                case "Stacks":
                case "Records": return "carpet";
                case "Dock": return "concrete";
                default: return "tile";
            }
        }
    }
}
