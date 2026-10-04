using UnityEditor;
using UnityEditor.SceneManagement;

namespace NocturneAnnex.Editor
{
    /// <summary>One-shot: creates the empty Boot scene and registers it in Build Settings.</summary>
    public static class CreateBootScene
    {
        const string Path = "Assets/_Project/Scenes/Boot.unity";

        [MenuItem("Build/Create Boot Scene")]
        public static void Create()
        {
            var scene = EditorSceneManager.NewScene(NewSceneSetup.DefaultGameObjects, NewSceneMode.Single);
            EditorSceneManager.SaveScene(scene, Path);
            EditorBuildSettings.scenes = new[] { new EditorBuildSettingsScene(Path, true) };
        }
    }
}
