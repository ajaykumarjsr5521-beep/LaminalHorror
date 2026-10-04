using System;
using System.IO;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.UI;
using NocturneAnnex.Flow;
using NocturneAnnex.Save;
using NocturneAnnex.Settings;
using NocturneAnnex.UI;

namespace NocturneAnnex.Editor
{
    /// <summary>
    /// Renders each menu screen at several resolutions to PNG files under Builds/screens, so layout can be reviewed
    /// without a device. Needs a graphics device: run WITHOUT -nographics. Output is not committed.
    /// Example: Unity -batchmode -projectPath . -executeMethod NocturneAnnex.Editor.ScreenshotCapture.CaptureMenus -quit
    /// </summary>
    public static class ScreenshotCapture
    {
        static readonly Vector2Int[] Resolutions =
        {
            new Vector2Int(1920, 1080),   // Windows 1080p
            new Vector2Int(2400, 1080),   // tall phone, landscape
            new Vector2Int(1280, 720),    // small 5-inch phone
        };

        const string OutDir = "Builds/screens";

        [MenuItem("Build/Capture Menu Screenshots")]
        public static void CaptureMenus()
        {
            Directory.CreateDirectory(OutDir);
            EditorSceneManager.OpenScene(MenuSceneBuilder.ScenePath, OpenSceneMode.Single);

            var canvas = UnityEngine.Object.FindFirstObjectByType<Canvas>();
            var bootstrap = UnityEngine.Object.FindFirstObjectByType<MenuBootstrap>();
            var cam = UnityEngine.Object.FindFirstObjectByType<Camera>();
            var temp = Path.Combine(Path.GetTempPath(), "na_shots_" + Path.GetRandomFileName());
            Directory.CreateDirectory(temp);

            var pause = MenuSceneBuilder.BuildPauseOverlay(canvas.transform.Find("SafeArea"));
            var pauseController = new GameObject("PauseController").AddComponent<PauseController>();
            pause.Bind(pauseController);

            try
            {
                Capture("main_nosave", Setup(bootstrap, temp, save: SaveKind.None), bootstrap, canvas, cam, b => b.ShowMain());
                Capture("main_save", Setup(bootstrap, temp, save: SaveKind.Valid), bootstrap, canvas, cam, b => b.ShowMain());
                Capture("main_corrupt", Setup(bootstrap, temp, save: SaveKind.Corrupt, badSettings: true), bootstrap, canvas, cam, b => b.ShowMain());
                Capture("confirm_new_game", Setup(bootstrap, temp, save: SaveKind.Valid), bootstrap, canvas, cam,
                    b => b.MainMenu.NewGameButton.onClick.Invoke());
                Capture("settings", Setup(bootstrap, temp, save: SaveKind.None), bootstrap, canvas, cam, b => b.ShowSettings());
                Capture("credits", Setup(bootstrap, temp, save: SaveKind.None), bootstrap, canvas, cam, b => b.ShowCredits());
                Capture("pause", Setup(bootstrap, temp, save: SaveKind.None), bootstrap, canvas, cam, b =>
                {
                    b.ShowMain();
                    b.MainPanel.SetActive(false);
                    pause.Panel.SetActive(true);
                });
            }
            finally
            {
                if (Directory.Exists(temp)) Directory.Delete(temp, true);
            }
            Debug.Log("Screenshots written to " + Path.GetFullPath(OutDir));
        }

        enum SaveKind { None, Valid, Corrupt }

        static string Setup(MenuBootstrap bootstrap, string dir, SaveKind save, bool badSettings = false)
        {
            foreach (var f in Directory.GetFiles(dir)) File.Delete(f);
            var savePath = Path.Combine(dir, "save.json");
            var settingsPath = Path.Combine(dir, "settings.json");

            if (save == SaveKind.Valid) new SaveStore(savePath).Write(new SaveData { CheckpointId = "lamp_room_1" });
            else if (save == SaveKind.Corrupt) File.WriteAllText(savePath, "garbage");
            if (badSettings) File.WriteAllText(settingsPath, "garbage");

            bootstrap.SaveStore = new SaveStore(savePath);
            bootstrap.SettingsStore = new SettingsStore(settingsPath, 3);
            return dir;
        }

        static void Capture(string name, string _, MenuBootstrap bootstrap, Canvas canvas, Camera cam, Action<MenuBootstrap> arrange)
        {
            bootstrap.Initialize();
            bootstrap.MainPanel.SetActive(true);
            arrange(bootstrap);

            var scaler = canvas.GetComponent<CanvasScaler>();
            var oldMode = scaler.uiScaleMode;
            var oldScale = scaler.scaleFactor;

            foreach (var res in Resolutions)
            {
                // CanvasScaler reads the real screen, which does not exist here, so apply its match-0.5 formula by hand.
                float scale = Mathf.Pow(2f, Mathf.Lerp(Mathf.Log(res.x / 1920f, 2f), Mathf.Log(res.y / 1080f, 2f), 0.5f));
                scaler.uiScaleMode = CanvasScaler.ScaleMode.ConstantPixelSize;
                scaler.scaleFactor = scale;

                var rt = RenderTexture.GetTemporary(res.x, res.y, 24, RenderTextureFormat.ARGB32);
                var oldTarget = cam.targetTexture;
                canvas.renderMode = RenderMode.ScreenSpaceCamera;
                canvas.worldCamera = cam;
                canvas.planeDistance = 1f;
                cam.targetTexture = rt;
                cam.orthographic = true;

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
                File.WriteAllBytes(Path.Combine(OutDir, $"{name}_{res.x}x{res.y}.png"), tex.EncodeToPNG());

                UnityEngine.Object.DestroyImmediate(tex);
                cam.targetTexture = oldTarget;
                RenderTexture.ReleaseTemporary(rt);
            }

            scaler.uiScaleMode = oldMode;
            scaler.scaleFactor = oldScale;
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        }
    }
}
