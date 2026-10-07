using System.IO;
using UnityEditor;
using UnityEngine;

namespace NocturneAnnex.Editor
{
    /// <summary>
    /// Lighting for Level_B1: the two lights that flicker in horror events (Hall, Stacks) stay realtime, every other light is baked,
    /// and the shell geometry is lightmapped. At most <see cref="MaxRealtimeLights"/> realtime lights (doc 04, mobile budget).
    /// </summary>
    public static class LevelLighting
    {
        public const int MaxRealtimeLights = 2;
        public const string SettingsPath = "Assets/_Project/Settings/Level_B1.lighting";

        /// <summary>Areas whose light a horror event flickers, so it must stay realtime.</summary>
        public static bool IsRealtimeArea(string area) => area == "Hall" || area == "Stacks";

        public static void ConfigureLight(Light l, string area) =>
            l.lightmapBakeType = IsRealtimeArea(area) ? LightmapBakeType.Realtime : LightmapBakeType.Baked;

        /// <summary>Marks walls, floors and ceilings as contributing to baked GI and as occluders.</summary>
        public static void MarkShellStatic(Transform shell)
        {
            const StaticEditorFlags flags = StaticEditorFlags.ContributeGI | StaticEditorFlags.OccluderStatic
                | StaticEditorFlags.OccludeeStatic | StaticEditorFlags.BatchingStatic | StaticEditorFlags.ReflectionProbeStatic;
            foreach (Transform t in shell) GameObjectUtility.SetStaticEditorFlags(t.gameObject, flags);
        }

        /// <summary>Bakes the open scene with the CPU lightmapper (works without a GPU) and returns whether it finished.</summary>
        public static bool Bake()
        {
            var settings = AssetDatabase.LoadAssetAtPath<LightingSettings>(SettingsPath);
            if (settings == null)
            {
                Directory.CreateDirectory(Path.GetDirectoryName(SettingsPath));
                settings = new LightingSettings();
                AssetDatabase.CreateAsset(settings, SettingsPath);
            }
#pragma warning disable CS0618   // ProgressiveCPU is deprecated but still maps to the CPU baker; the replacement name was not confirmed
            settings.lightmapper = LightingSettings.Lightmapper.ProgressiveCPU;
#pragma warning restore CS0618
            settings.bakedGI = true;
            settings.realtimeGI = false;
            settings.autoGenerate = false;
            settings.lightmapResolution = 6f;
            settings.lightmapMaxSize = 1024;
            settings.directSampleCount = 32;
            settings.indirectSampleCount = 64;
            settings.maxBounces = 2;
            settings.lightmapCompression = LightmapCompression.NormalQuality;
            EditorUtility.SetDirty(settings);
            AssetDatabase.SaveAssets();
            Lightmapping.lightingSettings = settings;
            return Lightmapping.Bake();
        }
    }
}
