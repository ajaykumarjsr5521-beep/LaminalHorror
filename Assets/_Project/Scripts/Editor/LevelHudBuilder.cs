using TMPro;
using UnityEditor;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem.UI;
using UnityEngine.UI;
using NocturneAnnex.Controls;
using NocturneAnnex.Flow;
using NocturneAnnex.Interaction;
using NocturneAnnex.Inventory;
using NocturneAnnex.Level;
using NocturneAnnex.Puzzle;
using NocturneAnnex.UI;

namespace NocturneAnnex.Editor
{
    /// <summary>Builds the in-level HUD under a safe-area root and wires it to the level objects. Regenerate, do not hand-edit.</summary>
    public static class LevelHudBuilder
    {
        public static LevelHud Build(Interactor interactor, PlayerInventory inventory, CodeLock finalLock, LevelBootstrap level)
        {
            var es = new GameObject("EventSystem", typeof(EventSystem));
            es.AddComponent<InputSystemUIInputModule>().AssignDefaultActions();

            var canvasGo = new GameObject("HUD", typeof(RectTransform));
            var canvas = canvasGo.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            var scaler = canvasGo.AddComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1920, 1080);
            scaler.screenMatchMode = CanvasScaler.ScreenMatchMode.MatchWidthOrHeight;
            scaler.matchWidthOrHeight = 0.5f;
            canvasGo.AddComponent<GraphicRaycaster>();

            var safe = UiKit.Child("SafeArea", canvasGo.transform);
            UiKit.Stretch(safe);
            safe.AddComponent<SafeAreaFitter>();

            TouchControlsBuilder.Build(safe.transform);   // first sibling: everything else draws and catches touches above it
            var hud = canvasGo.AddComponent<LevelHud>();
            hud.Level = level;

            // crosshair dot and interaction prompt
            var dot = UiKit.Child("Crosshair", safe.transform);
            var dotRt = (RectTransform)dot.transform;
            dotRt.anchorMin = dotRt.anchorMax = dotRt.pivot = new Vector2(0.5f, 0.5f);
            dotRt.sizeDelta = new Vector2(8f, 8f);
            UiKit.Fill(dot, new Color(1f, 1f, 1f, 0.55f)).raycastTarget = false;

            var prompt = UiKit.Label(safe.transform, null, 44, TextAlignmentOptions.Center, UiKit.Text, "Prompt");
            var prt = (RectTransform)prompt.transform;
            prt.anchorMin = prt.anchorMax = new Vector2(0.5f, 0.5f);
            prt.pivot = new Vector2(0.5f, 1f);
            prt.anchoredPosition = new Vector2(0f, -60f);
            prt.sizeDelta = new Vector2(900f, 70f);
            // the view must not live on the label: it hides the label when nothing is in focus
            var promptView = new GameObject("PromptView").AddComponent<InteractionPromptView>();
            promptView.transform.SetParent(safe.transform, false);
            promptView.Interactor = interactor;
            promptView.Label = prompt;
            prompt.gameObject.SetActive(false);

            // corner buttons
            hud.PauseButton = CornerButton(safe.transform, "menu.pause", new Vector2(0f, 1f), new Vector2(20f, -20f));
            hud.JournalButton = CornerButton(safe.transform, "journal.title", new Vector2(1f, 1f), new Vector2(-20f, -20f));

            // screens from prefabs
            var captions = Instantiate(GameplayUiBuilder.CaptionsPath, safe.transform);
            var noteReader = Instantiate(GameplayUiBuilder.NoteReaderPath, safe.transform);
            hud.Journal = Instantiate(GameplayUiBuilder.JournalPath, safe.transform).GetComponent<JournalView>();
            hud.Keypad = Instantiate(GameplayUiBuilder.KeypadPath, safe.transform).GetComponent<KeypadView>();
            hud.Inventory = inventory;   // LevelHud binds the journal and keypad at runtime

            // pause menu (the Settings button needs a settings screen here, which is not built yet: hidden, see F-13 open items)
            var pauseObj = new GameObject("PauseController");
            hud.Pause = pauseObj.AddComponent<PauseController>();
            hud.PauseMenu = MenuSceneBuilder.BuildPauseOverlay(safe.transform);
            hud.PauseMenu.SettingsButton.gameObject.SetActive(false);

            hud.EndCard = BuildEndCard(safe.transform, out hud.EndMenuButton);
            return hud;
        }

        static GameObject Instantiate(string path, Transform parent) =>
            (GameObject)PrefabUtility.InstantiatePrefab(AssetDatabase.LoadAssetAtPath<GameObject>(path), parent);

        static Button CornerButton(Transform parent, string key, Vector2 anchor, Vector2 offset)
        {
            var b = UiKit.MakeButton(parent, key, 260f, 100f);
            var rt = (RectTransform)b.transform;
            rt.anchorMin = rt.anchorMax = rt.pivot = anchor;
            rt.sizeDelta = new Vector2(260f, 100f);   // anchors are a point, so the size must be explicit
            rt.anchoredPosition = offset;
            return b;
        }

        static GameObject BuildEndCard(Transform parent, out Button menuButton)
        {
            var root = UiKit.Child("EndCard", parent);
            UiKit.Stretch(root);
            UiKit.Fill(root, new Color(0f, 0f, 0f, 0.9f));
            var column = UiKit.Child("Column", root.transform);
            UiKit.Stretch(column);
            UiKit.Column(column, 24f, TextAnchor.MiddleCenter);
            UiKit.Label(column.transform, "level.end.title", 80, TextAlignmentOptions.Center, UiKit.Text, "Title");
            var body = UiKit.Label(column.transform, "level.end.body", 48, TextAlignmentOptions.Center, UiKit.TextDim, "Body");
            UiKit.Size(body.gameObject, 1200f, 80f);
            menuButton = UiKit.MakeButton(column.transform, "menu.main_menu", 720f);
            return root;
        }
    }
}
