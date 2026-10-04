using System;
using System.IO;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.UI;
using NocturneAnnex.Core;
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

        // Set by CaptureAtTextSize so every screen can be reviewed at Small and Large text.
        static int _textSize = Accessibility.MediumText;
        static string _suffix = "";

        [MenuItem("Build/Capture All Screens At Large Text")]
        public static void CaptureLargeText() => CaptureAtTextSize(Accessibility.LargeText, "_large");

        [MenuItem("Build/Capture All Screens At Small Text")]
        public static void CaptureSmallText() => CaptureAtTextSize(Accessibility.SmallText, "_small");

        static void CaptureAtTextSize(int size, string suffix)
        {
            _textSize = size;
            _suffix = suffix;
            try
            {
                CaptureMenus();
                CaptureGameplayUi();
            }
            finally
            {
                _textSize = Accessibility.MediumText;
                _suffix = "";
                Accessibility.Reset();
            }
        }

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

        [MenuItem("Build/Capture Gameplay UI Screenshots")]
        public static void CaptureGameplayUi()
        {
            Directory.CreateDirectory(OutDir);
            EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
            var cam = new GameObject("Main Camera").AddComponent<Camera>();
            cam.clearFlags = CameraClearFlags.SolidColor;
            cam.backgroundColor = UiKit.Bg;
            var canvas = MenuSceneBuilder.CreateCanvasWithEventSystem();

            var keypad = (GameObject)PrefabUtility.InstantiatePrefab(AssetDatabase.LoadAssetAtPath<GameObject>(GameplayUiBuilder.KeypadPath), canvas.transform);
            var kv = keypad.GetComponent<KeypadView>();
            kv.Display.text = KeypadFormatter.Format("07", 4);
            kv.Message.text = Loc.Get("keypad.incorrect");
            kv.Message.gameObject.SetActive(true);
            Render("keypad", canvas, cam);
            UnityEngine.Object.DestroyImmediate(keypad);

            var reader = (GameObject)PrefabUtility.InstantiatePrefab(AssetDatabase.LoadAssetAtPath<GameObject>(GameplayUiBuilder.NoteReaderPath), canvas.transform);
            var rv = reader.GetComponent<NoteReaderView>();
            rv.Title.text = "Night Shift Memo";
            rv.Body.text = "Filing runs on the dates, not the names. The first entry is the day the lights failed; the second, the day the ledger was sealed; the third is circled in red on the calendar by the loading dock. Do not trust the clock in the Records Office.";
            rv.Panel.SetActive(true);
            Render("note_reader", canvas, cam);
            UnityEngine.Object.DestroyImmediate(reader);

            var journal = (GameObject)PrefabUtility.InstantiatePrefab(AssetDatabase.LoadAssetAtPath<GameObject>(GameplayUiBuilder.JournalPath), canvas.transform);
            var jv = journal.GetComponent<JournalView>();
            jv.Panel.SetActive(true);
            jv.EntryTemplate.gameObject.SetActive(false);
            foreach (var title in new[] { "Night Shift Memo", "Ledger Page 12", "Answering Machine Tape" })
            {
                var row = UnityEngine.Object.Instantiate(jv.EntryTemplate, jv.ListRoot);
                row.gameObject.SetActive(true);
                row.GetComponentInChildren<TMPro.TMP_Text>().text = title;
            }
            jv.ReaderTitle.text = "Ledger Page 12";
            jv.ReaderBody.text = "Entry struck through twice. Someone has written the date again underneath, smaller, as if to hide it.";
            jv.HintText.gameObject.SetActive(false);
            jv.EmptyText.gameObject.SetActive(false);
            Render("journal", canvas, cam);

            var captions = (GameObject)PrefabUtility.InstantiatePrefab(AssetDatabase.LoadAssetAtPath<GameObject>(GameplayUiBuilder.CaptionsPath), canvas.transform);
            var cv = captions.GetComponent<CaptionView>();
            cv.Label.text = string.Join("\n", Loc.Get("door.message.locked"), Loc.Get("pickup.refused"), Loc.Get("keypad.incorrect"));
            cv.Panel.SetActive(true);
            Render("captions", canvas, cam);
            UnityEngine.Object.DestroyImmediate(captions);

            // empty state
            var toRemove = new System.Collections.Generic.List<GameObject>();   // collect first: destroying while iterating skips children
            foreach (Transform child in jv.ListRoot)
                if (child.gameObject != jv.EntryTemplate.gameObject) toRemove.Add(child.gameObject);
            foreach (var go in toRemove) UnityEngine.Object.DestroyImmediate(go);
            jv.ReaderTitle.text = string.Empty;
            jv.ReaderBody.text = string.Empty;
            jv.EmptyText.text = Loc.Get("journal.empty");
            jv.EmptyText.gameObject.SetActive(true);
            Render("journal_empty", canvas, cam);
            Debug.Log("Gameplay UI screenshots written to " + Path.GetFullPath(OutDir));
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
            Render(name, canvas, cam);
        }

        /// <summary>Renders the canvas to PNGs at every resolution. The canvas keeps its original settings afterwards.</summary>
        static void Render(string name, Canvas canvas, Camera cam)
        {
            // text size: set the shared state, then let every ScalableText recompute from its authored size
            Accessibility.Set(true, _textSize, false, false);
            foreach (var st in canvas.GetComponentsInChildren<ScalableText>(true)) st.Apply();

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
                File.WriteAllBytes(Path.Combine(OutDir, $"{name}{_suffix}_{res.x}x{res.y}.png"), tex.EncodeToPNG());

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
