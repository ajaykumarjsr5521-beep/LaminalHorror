using System.IO;
using System.Linq;
using UnityEditor;
using UnityEngine;

namespace NocturneAnnex.Editor
{
    /// <summary>
    /// Imports the TextMesh Pro essential resources (default font and shaders) without the interactive dialog,
    /// so batchmode builds and screenshot checks can render text. Package import is asynchronous, so in batchmode
    /// this waits for the completion callback before exiting. Run without -quit.
    /// </summary>
    public static class ImportTmpEssentials
    {
        [MenuItem("Build/Import TMP Essentials")]
        public static void Import()
        {
            var package = Directory.GetDirectories("Library/PackageCache", "com.unity.ugui@*")
                .Select(d => Path.Combine(d, "Package Resources", "TMP Essential Resources.unitypackage"))
                .FirstOrDefault(File.Exists);
            if (package == null)
                throw new FileNotFoundException("TMP Essential Resources.unitypackage not found in the package cache.");

            AssetDatabase.importPackageCompleted += _ => Finish(0, "TMP essentials imported.");
            AssetDatabase.importPackageFailed += (_, error) => Finish(1, "TMP essentials import failed: " + error);
            AssetDatabase.importPackageCancelled += _ => Finish(1, "TMP essentials import cancelled.");
            AssetDatabase.ImportPackage(package, false);
        }

        static void Finish(int code, string message)
        {
            Debug.Log(message);
            if (Application.isBatchMode) EditorApplication.Exit(code);
        }
    }
}
