using System.IO;
using UnityEditor;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

namespace NocturneAnnex.Editor
{
    /// <summary>
    /// Creates the two URP pipeline assets (mobile-tuned and PC) and assigns them, because the project was created
    /// without one and URP materials render magenta under the Built-in pipeline. Safe to run again.
    /// Quality levels 0-2 use the mobile asset, the rest use the PC asset.
    /// </summary>
    public static class RenderPipelineSetup
    {
        public const string Dir = "Assets/_Project/Settings";
        public const string MobilePath = Dir + "/URP-Mobile.asset";
        public const string PcPath = Dir + "/URP-PC.asset";
        const int FirstPcQualityLevel = 3;

        [MenuItem("Build/Setup Render Pipeline")]
        public static void Setup()
        {
            Directory.CreateDirectory(Dir);
            var mobile = GetOrCreate(MobilePath, "URP-Mobile-Renderer");
            mobile.supportsHDR = false;
            mobile.renderScale = 0.85f;
            mobile.msaaSampleCount = 1;
            mobile.shadowDistance = 20f;
            EditorUtility.SetDirty(mobile);

            var pc = GetOrCreate(PcPath, "URP-PC-Renderer");
            pc.supportsHDR = true;
            pc.renderScale = 1f;
            pc.msaaSampleCount = 4;
            pc.shadowDistance = 40f;
            EditorUtility.SetDirty(pc);

            GraphicsSettings.defaultRenderPipeline = pc;
            int original = QualitySettings.GetQualityLevel();
            for (int i = 0; i < QualitySettings.names.Length; i++)
            {
                QualitySettings.SetQualityLevel(i, false);
                QualitySettings.renderPipeline = i < FirstPcQualityLevel ? mobile : pc;
            }
            QualitySettings.SetQualityLevel(original, false);
            AssetDatabase.SaveAssets();
            Debug.Log("URP assets assigned: " + MobilePath + ", " + PcPath);
        }

        static UniversalRenderPipelineAsset GetOrCreate(string path, string rendererName)
        {
            var existing = AssetDatabase.LoadAssetAtPath<UniversalRenderPipelineAsset>(path);
            if (existing != null) return existing;

            var renderer = ScriptableObject.CreateInstance<UniversalRendererData>();
            AssetDatabase.CreateAsset(renderer, $"{Dir}/{rendererName}.asset");
            var asset = UniversalRenderPipelineAsset.Create(renderer);
            AssetDatabase.CreateAsset(asset, path);
            return asset;
        }
    }
}
