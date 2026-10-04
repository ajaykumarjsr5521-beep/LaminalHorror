using System.Linq;
using TMPro;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem.UI;
using UnityEngine.UI;
using NocturneAnnex.Controls;
using NocturneAnnex.Flow;
using NocturneAnnex.UI;

namespace NocturneAnnex.Editor
{
    /// <summary>Generates the MainMenu scene (and the reusable pause overlay) from code so layout is reproducible.</summary>
    public static class MenuSceneBuilder
    {
        public const string ScenePath = "Assets/_Project/Scenes/MainMenu.unity";

        [MenuItem("Build/Create Main Menu Scene")]
        public static void CreateMainMenuScene()
        {
            var scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);

            var cam = new GameObject("Main Camera").AddComponent<Camera>();
            cam.tag = "MainCamera";
            cam.clearFlags = CameraClearFlags.SolidColor;
            cam.backgroundColor = UiKit.Bg;
            cam.gameObject.AddComponent<AudioListener>();

            var canvas = CreateCanvasWithEventSystem();
            var safe = UiKit.Child("SafeArea", canvas.transform);
            UiKit.Stretch(safe);
            safe.AddComponent<SafeAreaFitter>();

            var bootstrap = new GameObject("MenuBootstrap").AddComponent<MenuBootstrap>();

            var main = BuildMainPanel(safe.transform, out var menuView, out var confirm);
            var settingsPanel = BuildSettingsPanel(safe.transform, out var settingsView);
            var credits = BuildCreditsPanel(safe.transform, out var creditsBack, out var creditsNotice);
            var notice = BuildNoticePanel(safe.transform, out var noticeOk);

            bootstrap.MainMenu = menuView;
            bootstrap.Settings = settingsView;
            bootstrap.MainPanel = main;
            bootstrap.SettingsPanel = settingsPanel;
            bootstrap.CreditsPanel = credits;
            bootstrap.CreditsBackButton = creditsBack;
            bootstrap.CreditsNoticeButton = creditsNotice;
            bootstrap.NoticePanel = notice;
            bootstrap.NoticeOkButton = noticeOk;

            EditorSceneManager.SaveScene(scene, ScenePath);
            var scenes = EditorBuildSettings.scenes.Where(s => s.path != ScenePath).ToList();
            scenes.Add(new EditorBuildSettingsScene(ScenePath, true));
            EditorBuildSettings.scenes = scenes.ToArray();
        }

        public static Canvas CreateCanvasWithEventSystem()
        {
            var es = new GameObject("EventSystem", typeof(EventSystem));
            var module = es.AddComponent<InputSystemUIInputModule>();
            module.AssignDefaultActions();

            var go = new GameObject("Canvas", typeof(RectTransform));
            var canvas = go.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            var scaler = go.AddComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1920, 1080);
            scaler.screenMatchMode = CanvasScaler.ScreenMatchMode.MatchWidthOrHeight;
            scaler.matchWidthOrHeight = 0.5f;
            go.AddComponent<GraphicRaycaster>();
            UiKit.Fill(UiKit.Child("Background", go.transform), UiKit.Bg).raycastTarget = false;
            UiKit.Stretch(go.transform.Find("Background").gameObject);
            return canvas;
        }

        static GameObject BuildMainPanel(Transform parent, out MainMenuView view, out ConfirmDialogView confirm)
        {
            var panel = UiKit.Child("MainPanel", parent);
            UiKit.Stretch(panel);
            var column = UiKit.Child("Column", panel.transform);
            UiKit.Stretch(column);
            UiKit.Column(column, 14f, TextAnchor.MiddleCenter);

            UiKit.Label(column.transform, "menu.title", 96, TextAlignmentOptions.Center, UiKit.Text, "Title");
            var warning = UiKit.Label(column.transform, null, 32, TextAlignmentOptions.Center, UiKit.Warn, "Warning");
            var error = UiKit.Label(column.transform, null, 32, TextAlignmentOptions.Center, UiKit.Error, "Error");
            UiKit.Size(warning.gameObject, 1300, -1);
            UiKit.Size(error.gameObject, 1300, -1);
            UiKit.Size(UiKit.Child("Spacer", column.transform), 10, 24);

            view = panel.AddComponent<MainMenuView>();
            view.ContinueButton = UiKit.MakeButton(column.transform, "menu.continue", 720);
            view.NewGameButton = UiKit.MakeButton(column.transform, "menu.new_game", 720);
            view.SettingsButton = UiKit.MakeButton(column.transform, "menu.settings", 720);
            view.CreditsButton = UiKit.MakeButton(column.transform, "menu.credits", 720);
            view.QuitButton = UiKit.MakeButton(column.transform, "menu.quit", 720);
            view.WarningText = warning;
            view.ErrorText = error;

            confirm = BuildConfirmDialog(parent);
            view.Confirm = confirm;
            return panel;
        }

        static ConfirmDialogView BuildConfirmDialog(Transform parent)
        {
            var overlay = UiKit.Child("ConfirmDialog", parent);
            UiKit.Stretch(overlay);
            UiKit.Fill(overlay, new Color(0f, 0f, 0f, 0.8f));
            var view = overlay.AddComponent<ConfirmDialogView>();

            var box = UiKit.Child("Box", overlay.transform);
            var rt = (RectTransform)box.transform;
            rt.anchorMin = rt.anchorMax = rt.pivot = new Vector2(0.5f, 0.5f);
            rt.sizeDelta = new Vector2(1100, 520);
            UiKit.Fill(box, UiKit.Surface);
            UiKit.Column(box, 20f, TextAnchor.MiddleCenter, 36);

            UiKit.Label(box.transform, "confirm.new_game.title", 56, TextAlignmentOptions.Center, UiKit.Text, "Title");
            UiKit.Label(box.transform, "confirm.new_game.body", 38, TextAlignmentOptions.Center, UiKit.TextDim, "Body");
            var buttons = UiKit.Child("Buttons", box.transform);
            UiKit.Row(buttons, 24f, TextAnchor.MiddleCenter);
            UiKit.Size(buttons, -1, UiKit.ButtonHeight);
            view.No = UiKit.MakeButton(buttons.transform, "confirm.no", 440);
            view.Yes = UiKit.MakeButton(buttons.transform, "confirm.yes", 440);

            overlay.SetActive(false);
            return view;
        }

        static GameObject BuildSettingsPanel(Transform parent, out SettingsView view)
        {
            var panel = UiKit.Child("SettingsPanel", parent);
            UiKit.Stretch(panel);
            UiKit.Column(panel, 12f, TextAnchor.UpperCenter, 24);

            UiKit.Label(panel.transform, "menu.settings", 64, TextAlignmentOptions.Center, UiKit.Text, "Title");
            var error = UiKit.Label(panel.transform, null, 32, TextAlignmentOptions.Center, UiKit.Error, "Error");
            UiKit.Size(error.gameObject, 1500, -1);

            // scrollable list so every row stays reachable on small screens
            var scroll = UiKit.Child("Scroll", panel.transform);
            var scrollRect = scroll.AddComponent<ScrollRect>();
            UiKit.Size(scroll, -1, -1);
            var le = scroll.GetComponent<LayoutElement>();
            le.flexibleHeight = 1f;
            le.flexibleWidth = 1f;

            var viewport = UiKit.Child("Viewport", scroll.transform);
            UiKit.Stretch(viewport);
            UiKit.Fill(viewport, new Color(1, 1, 1, 0.01f));
            viewport.AddComponent<RectMask2D>();

            var content = UiKit.Child("Content", viewport.transform);
            var crt = (RectTransform)content.transform;
            crt.anchorMin = new Vector2(0f, 1f);
            crt.anchorMax = new Vector2(1f, 1f);
            crt.pivot = new Vector2(0.5f, 1f);
            crt.offsetMin = crt.offsetMax = Vector2.zero;
            UiKit.Column(content, 8f, TextAnchor.UpperCenter, 16);
            content.AddComponent<ContentSizeFitter>().verticalFit = ContentSizeFitter.FitMode.PreferredSize;

            // visible scrollbar so players can tell the list continues below the fold
            var bar = DefaultControls.CreateScrollbar(new DefaultControls.Resources());
            bar.transform.SetParent(scroll.transform, false);
            var bart = (RectTransform)bar.transform;
            bart.anchorMin = new Vector2(1f, 0f);
            bart.anchorMax = new Vector2(1f, 1f);
            bart.pivot = new Vector2(1f, 0.5f);
            bart.sizeDelta = new Vector2(28f, 0f);
            bar.GetComponent<Image>().color = UiKit.Surface;
            var barHandle = bar.transform.Find("Sliding Area/Handle");
            barHandle.GetComponent<Image>().color = UiKit.Accent;
            var scrollbar = bar.GetComponent<Scrollbar>();

            scrollRect.viewport = (RectTransform)viewport.transform;
            scrollRect.content = crt;
            scrollRect.horizontal = false;
            scrollRect.movementType = ScrollRect.MovementType.Clamped;
            scrollRect.scrollSensitivity = 40f;
            scrollRect.verticalScrollbar = scrollbar;
            scrollRect.verticalScrollbarVisibility = ScrollRect.ScrollbarVisibility.Permanent;

            view = panel.AddComponent<SettingsView>();
            view.LookSensitivity = SliderRow(content.transform, "settings.look_sensitivity");
            view.InvertY = ToggleRow(content.transform, "settings.invert_y");
            view.MasterVolume = SliderRow(content.transform, "settings.master_volume");
            view.MusicVolume = SliderRow(content.transform, "settings.music_volume");
            view.SfxVolume = SliderRow(content.transform, "settings.sfx_volume");
            view.Captions = ToggleRow(content.transform, "settings.captions");
            view.TouchScale = SliderRow(content.transform, "settings.touch_scale");
            view.StoryMode = ToggleRow(content.transform, "settings.story_mode");
            view.Quality = DropdownRow(content.transform, "settings.quality");
            view.TextSize = DropdownRow(content.transform, "settings.text_size");
            view.ReduceFlicker = ToggleRow(content.transform, "settings.reduce_flicker");
            view.ReduceMotion = ToggleRow(content.transform, "settings.reduce_motion");
            view.ErrorText = error;

            var footer = UiKit.Child("Footer", panel.transform);
            UiKit.Row(footer, 24f, TextAnchor.MiddleCenter);
            UiKit.Size(footer, -1, UiKit.ButtonHeight);
            view.BackButton = UiKit.MakeButton(footer.transform, "menu.back", 420);
            view.ResetButton = UiKit.MakeButton(footer.transform, "menu.reset_defaults", 560);
            view.SaveButton = UiKit.MakeButton(footer.transform, "menu.save", 420);
            return panel;
        }

        static GameObject Row(Transform parent, string labelKey)
        {
            var row = UiKit.Child("Row_" + labelKey, parent);
            UiKit.Row(row, 24f, TextAnchor.MiddleCenter);
            UiKit.Size(row, -1, 100);
            var label = UiKit.Label(row.transform, labelKey, 40, TextAlignmentOptions.MidlineLeft, UiKit.Text, "Label");
            UiKit.Size(label.gameObject, 600, -1, 1f);
            return row;
        }

        static Slider SliderRow(Transform parent, string labelKey)
        {
            var row = Row(parent, labelKey);
            var go = DefaultControls.CreateSlider(new DefaultControls.Resources());
            go.transform.SetParent(row.transform, false);
            UiKit.Size(go, 700, 60);
            var slider = go.GetComponent<Slider>();
            go.transform.Find("Background").GetComponent<Image>().color = UiKit.Button;
            go.transform.Find("Fill Area/Fill").GetComponent<Image>().color = UiKit.Accent;
            var handle = go.transform.Find("Handle Slide Area/Handle");
            handle.GetComponent<Image>().color = UiKit.Text;
            ((RectTransform)handle).sizeDelta = new Vector2(48, 0);
            return slider;
        }

        static Toggle ToggleRow(Transform parent, string labelKey)
        {
            var row = Row(parent, labelKey);
            var go = UiKit.Child("Toggle", row.transform);
            UiKit.Size(go, 700, 60);
            var toggle = go.AddComponent<Toggle>();

            var box = UiKit.Child("Box", go.transform);
            var brt = (RectTransform)box.transform;
            brt.anchorMin = brt.anchorMax = new Vector2(0f, 0.5f);
            brt.pivot = new Vector2(0f, 0.5f);
            brt.sizeDelta = new Vector2(60, 60);
            var boxImage = UiKit.Fill(box, UiKit.Button);

            var check = UiKit.Child("Check", box.transform);
            UiKit.Stretch(check, 12f);
            var checkImage = UiKit.Fill(check, UiKit.Accent);

            toggle.targetGraphic = boxImage;
            toggle.graphic = checkImage;
            return toggle;
        }

        static TMP_Dropdown DropdownRow(Transform parent, string labelKey)
        {
            var row = Row(parent, labelKey);
            var go = TMP_DefaultControls.CreateDropdown(new TMP_DefaultControls.Resources());
            go.transform.SetParent(row.transform, false);
            UiKit.Size(go, 700, 70);
            go.GetComponent<Image>().color = UiKit.Button;
            foreach (var t in go.GetComponentsInChildren<TMP_Text>(true)) { t.color = UiKit.Text; t.fontSize = 36; }
            return go.GetComponent<TMP_Dropdown>();
        }

        static GameObject BuildCreditsPanel(Transform parent, out Button back, out Button notice)
        {
            var panel = UiKit.Child("CreditsPanel", parent);
            UiKit.Stretch(panel);
            UiKit.Column(panel, 24f, TextAnchor.MiddleCenter, 24);
            UiKit.Label(panel.transform, "credits.title", 64, TextAlignmentOptions.Center, UiKit.Text, "Title");
            var body = UiKit.Label(panel.transform, "credits.body", 38, TextAlignmentOptions.Center, UiKit.TextDim, "Body");
            UiKit.Size(body.gameObject, 1200, -1);
            notice = UiKit.MakeButton(panel.transform, "menu.content_notice", 520);
            back = UiKit.MakeButton(panel.transform, "menu.back", 520);
            return panel;
        }

        static GameObject BuildNoticePanel(Transform parent, out Button ok)
        {
            var panel = UiKit.Child("NoticePanel", parent);
            UiKit.Stretch(panel);
            UiKit.Column(panel, 28f, TextAnchor.MiddleCenter, 24);
            UiKit.Label(panel.transform, "notice.title", 64, TextAlignmentOptions.Center, UiKit.Text, "Title");
            var body = UiKit.Label(panel.transform, "notice.body", 40, TextAlignmentOptions.Center, UiKit.Text, "Body");
            UiKit.Size(body.gameObject, 1300, -1);
            ok = UiKit.MakeButton(panel.transform, "notice.ok", 620);
            return panel;
        }

        /// <summary>Builds the pause overlay under a canvas root; returns the view (bound to a controller at runtime).</summary>
        public static PauseMenuView BuildPauseOverlay(Transform parent)
        {
            var root = UiKit.Child("PauseMenu", parent);
            UiKit.Stretch(root);
            var view = root.AddComponent<PauseMenuView>();

            // the view lives on an always-active root and toggles this child, so it never disables itself
            var overlay = UiKit.Child("Overlay", root.transform);
            UiKit.Stretch(overlay);
            UiKit.Fill(overlay, new Color(0f, 0f, 0f, 0.8f));
            view.Panel = overlay;

            var column = UiKit.Child("Column", overlay.transform);
            UiKit.Stretch(column);
            UiKit.Column(column, 14f, TextAnchor.MiddleCenter);
            UiKit.Label(column.transform, "menu.title", 72, TextAlignmentOptions.Center, UiKit.Text, "Title");
            UiKit.Size(UiKit.Child("Spacer", column.transform), 10, 16);
            view.ResumeButton = UiKit.MakeButton(column.transform, "menu.resume", 720);
            view.RestartButton = UiKit.MakeButton(column.transform, "menu.restart", 720);
            view.SettingsButton = UiKit.MakeButton(column.transform, "menu.settings", 720);
            view.MainMenuButton = UiKit.MakeButton(column.transform, "menu.main_menu", 720);
            view.QuitButton = UiKit.MakeButton(column.transform, "menu.quit", 720);
            return view;
        }
    }
}
