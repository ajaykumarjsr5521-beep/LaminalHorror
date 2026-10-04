using System.IO;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.UI;

namespace NocturneAnnex.Editor
{
    /// <summary>
    /// Renders Level_B1 from fixed viewpoints, with the HUD on top, to Builds/screens so layout and clue placement can be
    /// reviewed without a device. Needs a graphics device (no -nographics). Output is not committed.
    /// Example: Unity -batchmode -projectPath . -executeMethod NocturneAnnex.Editor.LevelScreenshotCapture.Capture -quit
    /// </summary>
    public static class LevelScreenshotCapture
    {
        const string OutDir = "Builds/screens";
        static readonly Vector2Int[] Resolutions = { new Vector2Int(1920, 1080), new Vector2Int(1280, 720) };

        static readonly (string name, Vector3 pos, Vector3 euler)[] Views =
        {
            ("start", new Vector3(0f, 1.6f, 1f), new Vector3(0f, 0f, 0f)),
            ("calendar_lights", new Vector3(-1.2f, 1.6f, 5f), new Vector3(0f, 270f, 0f)),
            ("memo_table", new Vector3(2.4f, 1.6f, 1.6f), new Vector3(35f, 0f, 0f)),
            ("hall", new Vector3(0f, 1.6f, 11f), new Vector3(0f, 0f, 0f)),
            ("hall_calendar", new Vector3(2.5f, 1.6f, 22.5f), new Vector3(0f, 0f, 0f)),
            ("stacks", new Vector3(-7f, 1.6f, 18f), new Vector3(0f, 270f, 0f)),
            ("ledger", new Vector3(-8f, 1.6f, 20.4f), new Vector3(10f, 0f, 0f)),
            ("stamp_shelf", new Vector3(-15.5f, 1.6f, 18f), new Vector3(15f, 270f, 0f)),
            ("records_keypad", new Vector3(12f, 1.6f, 21f), new Vector3(0f, 0f, 0f)),
            ("dock_door", new Vector3(0f, 1.6f, 23.5f), new Vector3(0f, 0f, 0f)),
        };

        [MenuItem("Build/Capture Level Screenshots")]
        public static void Capture()
        {
            Directory.CreateDirectory(OutDir);
            EditorSceneManager.OpenScene(LevelB1Builder.ScenePath, OpenSceneMode.Single);
            var cam = Camera.main;
            var canvas = Object.FindFirstObjectByType<Canvas>();
            var scaler = canvas.GetComponent<CanvasScaler>();

            // These screens hide themselves in Awake/Start, which does not run in edit mode, so hide them here.
            var hud = Object.FindFirstObjectByType<NocturneAnnex.Level.LevelHud>();
            hud.EndCard.SetActive(false);
            hud.PauseMenu.Panel.SetActive(false);
            hud.Keypad.gameObject.SetActive(false);
            hud.Journal.Panel.SetActive(false);
            Object.FindFirstObjectByType<NocturneAnnex.UI.CaptionView>(FindObjectsInactive.Include).Panel.SetActive(false);
            Object.FindFirstObjectByType<NocturneAnnex.UI.NoteReaderView>(FindObjectsInactive.Include).Panel.SetActive(false);

            foreach (var (name, pos, euler) in Views)
            {
                cam.transform.SetPositionAndRotation(pos, Quaternion.Euler(euler));
                foreach (var res in Resolutions) Render(name, res, cam, canvas, scaler);
            }
            Debug.Log("Level screenshots written to " + Path.GetFullPath(OutDir));
        }

        static void Render(string name, Vector2Int res, Camera cam, Canvas canvas, CanvasScaler scaler)
        {
            float scale = Mathf.Pow(2f, Mathf.Lerp(Mathf.Log(res.x / 1920f, 2f), Mathf.Log(res.y / 1080f, 2f), 0.5f));
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ConstantPixelSize;
            scaler.scaleFactor = scale;

            var rt = RenderTexture.GetTemporary(res.x, res.y, 24, RenderTextureFormat.ARGB32);
            canvas.renderMode = RenderMode.ScreenSpaceCamera;
            canvas.worldCamera = cam;
            canvas.planeDistance = 1f;
            cam.targetTexture = rt;
            cam.aspect = (float)res.x / res.y;

            Canvas.ForceUpdateCanvases();
            LayoutRebuilder.ForceRebuildLayoutImmediate((RectTransform)canvas.transform);
            Canvas.ForceUpdateCanvases();
            cam.Render();

            var prev = RenderTexture.active;
            RenderTexture.active = rt;
            var tex = new Texture2D(res.x, res.y, TextureFormat.RGB24, false);
            tex.ReadPixels(new Rect(0, 0, res.x, res.y), 0, 0);
            tex.Apply();
            RenderTexture.active = prev;
            File.WriteAllBytes(Path.Combine(OutDir, $"level_{name}_{res.x}x{res.y}.png"), tex.EncodeToPNG());

            Object.DestroyImmediate(tex);
            cam.targetTexture = null;
            RenderTexture.ReleaseTemporary(rt);
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        }
    }
}
