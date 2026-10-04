using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using NocturneAnnex.Controls;
using NocturneAnnex.Player;

namespace NocturneAnnex.Editor
{
    /// <summary>Builds a throwaway greybox test level: a corridor, a low-ceiling crawl section and a room.</summary>
    public static class CreateGreyboxScene
    {
        const string Path = "Assets/_Project/Scenes/Greybox.unity";

        [MenuItem("Build/Create Greybox Scene")]
        public static void Create()
        {
            var scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);

            Block("Floor", new Vector3(0, -0.25f, 20), new Vector3(16, 0.5f, 60));
            Block("WallL", new Vector3(-4, 1.5f, 20), new Vector3(0.4f, 3, 60));
            Block("WallR", new Vector3(4, 1.5f, 20), new Vector3(0.4f, 3, 60));
            Block("WallEnd", new Vector3(0, 1.5f, 50), new Vector3(8.4f, 3, 0.4f));
            Block("Ceiling", new Vector3(0, 3.2f, 20), new Vector3(8.4f, 0.4f, 60));
            // crawl section: low slab (underside at 1.3 m) forces crouching
            Block("LowCeiling", new Vector3(0, 1.45f, 18), new Vector3(7.6f, 0.3f, 8));
            Block("Crate", new Vector3(2, 0.5f, 8), new Vector3(1, 1, 1));

            var sun = new GameObject("Light").AddComponent<Light>();
            sun.type = LightType.Point; sun.range = 40; sun.intensity = 1.2f;
            sun.transform.position = new Vector3(0, 2.8f, 4);

            new GameObject("InputRouter").AddComponent<InputRouter>();

            var player = new GameObject("Player");
            player.transform.position = new Vector3(0, 0.1f, 0);
            player.AddComponent<CharacterController>();
            var pivot = new GameObject("CameraPivot").transform;
            pivot.SetParent(player.transform, false);
            var cam = new GameObject("Main Camera");
            cam.tag = "MainCamera";
            cam.AddComponent<Camera>().nearClipPlane = 0.05f;
            cam.AddComponent<AudioListener>();
            cam.transform.SetParent(pivot, false);
            var motor = player.AddComponent<PlayerMotor>();
            motor.CameraPivot = pivot;
            player.AddComponent<PlayerLook>().CameraPivot = pivot;

            EditorSceneManager.SaveScene(scene, Path);
            var scenes = EditorBuildSettings.scenes.Where(s => s.path != Path).ToList();
            scenes.Add(new EditorBuildSettingsScene(Path, true));
            EditorBuildSettings.scenes = scenes.ToArray();
        }

        static void Block(string name, Vector3 pos, Vector3 scale)
        {
            var g = GameObject.CreatePrimitive(PrimitiveType.Cube);
            g.name = name; g.transform.position = pos; g.transform.localScale = scale;
        }
    }
}
