using System.Linq;
using UnityEditor;
using UnityEngine;
using NocturneAnnex.Audio;

namespace NocturneAnnex.Editor
{
    /// <summary>
    /// Assigns the CC0 clips under Assets/_Project/Audio to the cues in the catalog. Every file used here needs a row in
    /// docs/09-asset-ip-register.md. Some cues use a stand-in clip (see the register notes) until better audio exists.
    /// </summary>
    public static class LevelAudioClips
    {
        const string Dir = "Assets/_Project/Audio/";

        static readonly (string cue, string[] files)[] Map =
        {
            ("footstep.tile", new[] { "Sfx/footstep00.ogg", "Sfx/footstep01.ogg", "Sfx/footstep02.ogg", "Sfx/footstep03.ogg" }),
            ("footstep.carpet", new[] { "Sfx/footstep_carpet_000.ogg", "Sfx/footstep_carpet_001.ogg", "Sfx/footstep_carpet_002.ogg" }),
            ("footstep.concrete", new[] { "Sfx/footstep_concrete_000.ogg", "Sfx/footstep_concrete_001.ogg", "Sfx/footstep_concrete_002.ogg" }),
            ("door.open", new[] { "Sfx/doorOpen_1.ogg", "Sfx/doorOpen_2.ogg" }),
            ("door.close", new[] { "Sfx/doorClose_1.ogg", "Sfx/doorClose_2.ogg" }),
            ("door.locked", new[] { "Sfx/impactMetal_light_000.ogg" }),
            ("pickup.take", new[] { "Sfx/handleSmallLeather.ogg" }),
            ("note.open", new[] { "Sfx/bookOpen.ogg" }),
            ("checkpoint.chime", new[] { "Sfx/impactBell_heavy_001.ogg" }),
            ("event.door_slam", new[] { "Sfx/doorClose_3.ogg", "Sfx/doorClose_4.ogg" }),
            ("event.prop_shift", new[] { "Sfx/impactGeneric_light_000.ogg", "Sfx/creak1.ogg" }),
            ("event.whisper", new[] { "Sfx/creak3.ogg" }),             // stand-in: no whisper clip in the packs
            ("event.light_buzz", new[] { "Sfx/impactPlate_light_000.ogg" }),   // stand-in: no electrical buzz in the packs
            ("event.misfile", new[] { "Sfx/impactBell_heavy_002.ogg" }),
            ("event.figure", new[] { "Sfx/impactBell_heavy_000.ogg" }),
            ("amb.hall", new[] { "Ambience/amb_Infestation_in_the_Control_Room.mp3" }),
            ("amb.stacks", new[] { "Ambience/amb_The_Surreal_Truth.mp3" }),
            ("amb.records", new[] { "Ambience/amb_Final_Captains_Log.mp3" }),
            ("amb.break_room", new[] { "Ambience/amb_Cage_of_the_Cryptid.mp3" }),
            ("amb.dock", new[] { "Ambience/amb_The_Depths_of_Hell.mp3" }),
            ("music.drone", new[] { "Ambience/amb_The_Depths_of_Hell.mp3" }),
        };

        /// <summary>All project-relative clip paths used by <see cref="Map"/>, without duplicates. For the register check.</summary>
        public static string[] UsedFiles() => Map.SelectMany(m => m.files).Distinct().Select(f => Dir + f).ToArray();

        [MenuItem("Build/Assign Audio Clips")]
        public static void Assign()
        {
            LevelAudio.CreateAssets();
            var catalog = AssetDatabase.LoadAssetAtPath<CueCatalog>(LevelAudio.CatalogPath);
            foreach (var (cueId, files) in Map)
            {
                var cue = catalog.Find(cueId);
                if (cue == null) { Debug.LogError($"LevelAudioClips: cue '{cueId}' is not in the catalog."); continue; }
                var clips = files.Select(f => AssetDatabase.LoadAssetAtPath<AudioClip>(Dir + f)).ToArray();
                if (clips.Any(c => c == null)) { Debug.LogError($"LevelAudioClips: a clip for '{cueId}' is missing."); continue; }
                cue.Clips = clips;
            }
            foreach (var path in UsedFiles().Where(p => p.Contains("/Ambience/")))
            {
                var importer = (AudioImporter)AssetImporter.GetAtPath(path);
                var settings = importer.defaultSampleSettings;
                if (settings.loadType == AudioClipLoadType.Streaming) continue;
                settings.loadType = AudioClipLoadType.Streaming;   // long beds are streamed, not held in RAM
                importer.defaultSampleSettings = settings;
                importer.SaveAndReimport();
            }
            EditorUtility.SetDirty(catalog);
            AssetDatabase.SaveAssets();
            Debug.Log("Audio clips assigned to " + Map.Length + " cues.");
        }
    }
}
