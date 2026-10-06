using System.IO;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace NocturneAnnex.Editor
{
    /// <summary>Renders a few fixed viewpoints of Hotel Meridian to Builds/screens for scale and light review. Needs a GPU (no -nographics).</summary>
    public static class HotelScreenshots
    {
        static readonly (string name, Vector3 pos, Vector3 euler)[] Views =
        {
            ("hotel_vista", new Vector3(0f, 1.6f, 3f), new Vector3(0f, 0f, 0f)),
            ("hotel_vista_up", new Vector3(0f, 1.6f, 6f), new Vector3(-18f, 0f, 0f)),
            ("hotel_mezzanine", new Vector3(-12f, 7.1f, 14f), new Vector3(8f, 40f, 0f)),
            ("hotel_wing", new Vector3(16f, 1.6f, 20f), new Vector3(0f, 90f, 0f)),
            ("hotel_wing_far", new Vector3(45f, 1.6f, 20f), new Vector3(0f, 90f, 0f)),
        };

        [MenuItem("Build/Capture Hotel Screenshots")]
        public static void Capture()
        {
            Directory.CreateDirectory("Builds/screens");
            EditorSceneManager.OpenScene(HotelBuilder.ScenePath, OpenSceneMode.Single);
            var cam = Camera.main;
            foreach (var (name, pos, euler) in Views)
            {
                cam.transform.SetPositionAndRotation(pos, Quaternion.Euler(euler));
                var rt = RenderTexture.GetTemporary(1920, 1080, 24, RenderTextureFormat.ARGB32);
                cam.targetTexture = rt;
                cam.aspect = 1920f / 1080f;
                cam.Render();
                var prev = RenderTexture.active;
                RenderTexture.active = rt;
                var tex = new Texture2D(1920, 1080, TextureFormat.RGB24, false);
                tex.ReadPixels(new Rect(0, 0, 1920, 1080), 0, 0);
                tex.Apply();
                File.WriteAllBytes($"Builds/screens/{name}.png", tex.EncodeToPNG());
                Object.DestroyImmediate(tex);
                RenderTexture.active = prev;
                cam.targetTexture = null;
                RenderTexture.ReleaseTemporary(rt);
            }
            Debug.Log("Hotel screenshots written to " + Path.GetFullPath("Builds/screens"));
        }
    }
}
