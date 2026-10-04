using System.IO;
using TMPro;
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;
using NocturneAnnex.UI;

namespace NocturneAnnex.Editor
{
    /// <summary>Generates the in-game screens as prefabs: keypad, note reader and journal. Regenerate, do not hand-edit.</summary>
    public static class GameplayUiBuilder
    {
        public const string PrefabDir = "Assets/_Project/Prefabs/UI";
        public const string KeypadPath = PrefabDir + "/Keypad.prefab";
        public const string NoteReaderPath = PrefabDir + "/NoteReader.prefab";
        public const string JournalPath = PrefabDir + "/Journal.prefab";

        static readonly Color Dim = new Color(0f, 0f, 0f, 0.82f);

        [MenuItem("Build/Create Gameplay UI Prefabs")]
        public static void CreateAll()
        {
            Directory.CreateDirectory(PrefabDir);
            Save(BuildKeypad(), KeypadPath);
            Save(BuildNoteReader(), NoteReaderPath);
            Save(BuildJournal(), JournalPath);
            AssetDatabase.SaveAssets();
        }

        static void Save(GameObject root, string path)
        {
            PrefabUtility.SaveAsPrefabAsset(root, path);
            Object.DestroyImmediate(root);
        }

        static GameObject Overlay(string name, out GameObject box, float width, float height)
        {
            var root = new GameObject(name, typeof(RectTransform));
            UiKit.Stretch(root);
            UiKit.Fill(root, Dim);
            box = UiKit.Child("Box", root.transform);
            var rt = (RectTransform)box.transform;
            rt.anchorMin = rt.anchorMax = rt.pivot = new Vector2(0.5f, 0.5f);
            rt.sizeDelta = new Vector2(width, height);
            UiKit.Fill(box, UiKit.Surface);
            return root;
        }

        static GameObject BuildKeypad()
        {
            var root = Overlay("Keypad", out var box, 900f, 900f);
            var view = root.AddComponent<KeypadView>();
            UiKit.Column(box, 14f, TextAnchor.UpperCenter, 30);

            var header = UiKit.Child("Header", box.transform);
            UiKit.Row(header, 16f, TextAnchor.MiddleCenter);
            UiKit.Size(header, -1, 100f, 1f);
            var title = UiKit.Label(header.transform, "keypad.title", 52, TextAlignmentOptions.MidlineLeft, UiKit.Text, "Title");
            UiKit.Size(title.gameObject, 300, -1, 1f);
            view.CloseButton = UiKit.MakeButton(header.transform, "menu.close", 260, 100f);

            view.Display = UiKit.Label(box.transform, null, 84, TextAlignmentOptions.Center, UiKit.Accent, "Display");
            UiKit.Size(view.Display.gameObject, -1, 110f);
            view.Message = UiKit.Label(box.transform, null, 36, TextAlignmentOptions.Center, UiKit.Warn, "Message");
            UiKit.Size(view.Message.gameObject, -1, 50f);

            var grid = UiKit.Child("Pad", box.transform);
            var g = grid.AddComponent<GridLayoutGroup>();
            g.cellSize = new Vector2(240f, 112f);
            g.spacing = new Vector2(14f, 14f);
            g.constraint = GridLayoutGroup.Constraint.FixedColumnCount;
            g.constraintCount = 3;
            g.childAlignment = TextAnchor.UpperCenter;
            UiKit.Size(grid, -1, 4 * 112f + 3 * 14f);

            view.DigitButtons = new Button[10];
            for (int d = 1; d <= 9; d++) view.DigitButtons[d] = DigitButton(grid.transform, d);
            view.BackspaceButton = UiKit.MakeButton(grid.transform, "keypad.clear", 240f, 112f);
            view.DigitButtons[0] = DigitButton(grid.transform, 0);
            view.EnterButton = UiKit.MakeButton(grid.transform, "keypad.enter", 240f, 112f);
            return root;
        }

        static Button DigitButton(Transform parent, int digit)
        {
            var b = UiKit.MakeButton(parent, "keypad.enter", 240f, 112f);   // placeholder key, replaced below
            b.name = "Digit_" + digit;
            var label = b.GetComponentInChildren<TextMeshProUGUI>();
            Object.DestroyImmediate(label.GetComponent<NocturneAnnex.UI.LocalizedText>());   // digits are not localized
            label.text = digit.ToString();
            label.fontSize = 56;
            return b;
        }

        static GameObject BuildNoteReader()
        {
            var root = new GameObject("NoteReader", typeof(RectTransform));
            UiKit.Stretch(root);
            var view = root.AddComponent<NoteReaderView>();

            var panel = Overlay("Panel", out var box, 1300f, 860f);
            panel.transform.SetParent(root.transform, false);
            UiKit.Stretch(panel);
            view.Panel = panel;
            UiKit.Column(box, 20f, TextAnchor.UpperCenter, 40);

            view.Title = UiKit.Label(box.transform, null, 56, TextAlignmentOptions.Center, UiKit.Text, "Title");
            UiKit.Size(view.Title.gameObject, -1, 80f);
            view.Body = UiKit.Label(box.transform, null, 40, TextAlignmentOptions.TopLeft, UiKit.Text, "Body");
            view.Body.enableAutoSizing = true;
            view.Body.fontSizeMin = 26;
            view.Body.fontSizeMax = 42;
            var le = view.Body.gameObject.AddComponent<LayoutElement>();
            le.flexibleHeight = 1f;
            le.flexibleWidth = 1f;
            view.CloseButton = UiKit.MakeButton(box.transform, "menu.close", 520f);
            return root;
        }

        static GameObject BuildJournal()
        {
            var root = new GameObject("Journal", typeof(RectTransform));
            UiKit.Stretch(root);
            var view = root.AddComponent<JournalView>();

            var panel = Overlay("Panel", out var box, 1700f, 940f);
            panel.transform.SetParent(root.transform, false);
            UiKit.Stretch(panel);
            view.Panel = panel;
            UiKit.Column(box, 16f, TextAnchor.UpperCenter, 30);

            var header = UiKit.Child("Header", box.transform);
            UiKit.Row(header, 16f, TextAnchor.MiddleCenter);
            UiKit.Size(header, -1, 100f, 1f);
            var title = UiKit.Label(header.transform, "journal.title", 56, TextAlignmentOptions.MidlineLeft, UiKit.Text, "Title");
            UiKit.Size(title.gameObject, 300, -1, 1f);
            view.CloseButton = UiKit.MakeButton(header.transform, "menu.close", 260, 100f);

            var body = UiKit.Child("Body", box.transform);
            UiKit.Row(body, 24f, TextAnchor.UpperCenter);
            var bodyLe = body.AddComponent<LayoutElement>();
            bodyLe.flexibleHeight = 1f;
            bodyLe.flexibleWidth = 1f;

            // left: scrollable list of notes
            var listArea = UiKit.Child("ListArea", body.transform);
            UiKit.Size(listArea, 560f, -1);
            listArea.GetComponent<LayoutElement>().flexibleHeight = 1f;
            var scroll = listArea.AddComponent<ScrollRect>();
            var viewport = UiKit.Child("Viewport", listArea.transform);
            UiKit.Stretch(viewport);
            UiKit.Fill(viewport, new Color(1, 1, 1, 0.01f));
            viewport.AddComponent<RectMask2D>();
            var content = UiKit.Child("Content", viewport.transform);
            var crt = (RectTransform)content.transform;
            crt.anchorMin = new Vector2(0f, 1f);
            crt.anchorMax = new Vector2(1f, 1f);
            crt.pivot = new Vector2(0.5f, 1f);
            crt.offsetMin = crt.offsetMax = Vector2.zero;
            UiKit.Column(content, 10f, TextAnchor.UpperCenter);
            content.AddComponent<ContentSizeFitter>().verticalFit = ContentSizeFitter.FitMode.PreferredSize;
            scroll.viewport = (RectTransform)viewport.transform;
            scroll.content = crt;
            scroll.horizontal = false;
            scroll.movementType = ScrollRect.MovementType.Clamped;
            view.ListRoot = content.transform;

            var template = UiKit.MakeButton(content.transform, "journal.title", -1f, 100f);
            template.name = "EntryTemplate";
            var tl = template.GetComponentInChildren<TextMeshProUGUI>();
            Object.DestroyImmediate(tl.GetComponent<NocturneAnnex.UI.LocalizedText>());
            tl.alignment = TextAlignmentOptions.MidlineLeft;
            tl.fontSize = 38;
            tl.textWrappingMode = TextWrappingModes.NoWrap;
            tl.overflowMode = TextOverflowModes.Ellipsis;
            template.GetComponent<LayoutElement>().flexibleWidth = 1f;
            view.EntryTemplate = template;

            view.EmptyText = UiKit.Label(listArea.transform, null, 38, TextAlignmentOptions.Center, UiKit.TextDim, "Empty");
            UiKit.Stretch(view.EmptyText.gameObject, 12f);

            // right: reader
            var reader = UiKit.Child("Reader", body.transform);
            UiKit.Fill(reader, UiKit.Bg);
            UiKit.Column(reader, 16f, TextAnchor.UpperCenter, 30);
            var readerLe = reader.AddComponent<LayoutElement>();
            readerLe.flexibleWidth = 1f;
            readerLe.flexibleHeight = 1f;
            view.ReaderTitle = UiKit.Label(reader.transform, null, 52, TextAlignmentOptions.TopLeft, UiKit.Accent, "ReaderTitle");
            UiKit.Size(view.ReaderTitle.gameObject, -1, 70f, 1f);
            view.ReaderBody = UiKit.Label(reader.transform, null, 40, TextAlignmentOptions.TopLeft, UiKit.Text, "ReaderBody");
            view.ReaderBody.enableAutoSizing = true;
            view.ReaderBody.fontSizeMin = 26;
            view.ReaderBody.fontSizeMax = 42;
            var rb = view.ReaderBody.gameObject.AddComponent<LayoutElement>();
            rb.flexibleHeight = 1f;
            rb.flexibleWidth = 1f;
            view.HintText = UiKit.Label(reader.transform, null, 38, TextAlignmentOptions.Center, UiKit.TextDim, "Hint");
            UiKit.Size(view.HintText.gameObject, -1, 60f, 1f);
            return root;
        }
    }
}
