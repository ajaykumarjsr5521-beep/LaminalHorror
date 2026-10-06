using System;
using System.Linq;
using UnityEditor;
using UnityEditor.Build.Reporting;
using UnityEngine;

namespace NocturneAnnex.Editor
{
    /// <summary>
    /// Batchmode build entry points. Example:
    /// Unity -batchmode -quit -projectPath . -executeMethod NocturneAnnex.Editor.BuildScript.BuildAndroidApk
    /// Signing secrets are read from environment variables, never from the repo.
    /// </summary>
    public static class BuildScript
    {
        const string OutDir = "Builds";

        [MenuItem("Build/Android APK (dev)")]
        public static void BuildAndroidApk() => Build(BuildTarget.Android, $"{OutDir}/Android/NocturneAnnex.apk", BuildOptions.Development, aab: false);

        [MenuItem("Build/Android AAB (release)")]
        public static void BuildAndroidAab()
        {
            AssetRegister.CheckForRelease();   // doc 09: nothing REQUIRES_REVIEW ships
            Build(BuildTarget.Android, $"{OutDir}/Android/NocturneAnnex.aab", BuildOptions.None, aab: true);
        }

        [MenuItem("Build/Windows x64")]
        public static void BuildWindows() => Build(BuildTarget.StandaloneWindows64, $"{OutDir}/Windows/NocturneAnnex.exe", BuildOptions.None, aab: false);

        static void Build(BuildTarget target, string path, BuildOptions options, bool aab)
        {
            var scenes = EditorBuildSettings.scenes.Where(s => s.enabled).Select(s => s.path).ToArray();
            if (scenes.Length == 0)
                throw new InvalidOperationException("No enabled scenes in Build Settings.");

            if (target == BuildTarget.Android)
            {
                EditorUserBuildSettings.buildAppBundle = aab;
                if (aab) ApplyAndroidSigningFromEnvironment();
            }

            var report = BuildPipeline.BuildPlayer(new BuildPlayerOptions
            {
                scenes = scenes, locationPathName = path, target = target, options = options
            });

            if (report.summary.result != BuildResult.Succeeded)
            {
                Debug.LogError($"Build failed: {report.summary.result}, errors={report.summary.totalErrors}");
                if (Application.isBatchMode) EditorApplication.Exit(1);
                throw new Exception("Build failed.");
            }
            Debug.Log($"Build succeeded: {path} ({report.summary.totalSize} bytes)");
        }

        static void ApplyAndroidSigningFromEnvironment()
        {
            var keystore = Environment.GetEnvironmentVariable("NA_KEYSTORE_PATH");
            var storePass = Environment.GetEnvironmentVariable("NA_KEYSTORE_PASS");
            var alias = Environment.GetEnvironmentVariable("NA_KEY_ALIAS");
            var aliasPass = Environment.GetEnvironmentVariable("NA_KEY_PASS");
            if (string.IsNullOrEmpty(keystore) || string.IsNullOrEmpty(storePass) || string.IsNullOrEmpty(alias) || string.IsNullOrEmpty(aliasPass))
                throw new InvalidOperationException("Release signing requires NA_KEYSTORE_PATH, NA_KEYSTORE_PASS, NA_KEY_ALIAS, NA_KEY_PASS environment variables.");
            PlayerSettings.Android.useCustomKeystore = true;
            PlayerSettings.Android.keystoreName = keystore;
            PlayerSettings.Android.keystorePass = storePass;
            PlayerSettings.Android.keyaliasName = alias;
            PlayerSettings.Android.keyaliasPass = aliasPass;
        }
    }
}
