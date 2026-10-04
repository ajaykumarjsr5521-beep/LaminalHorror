using TMPro;
using UnityEngine;
using UnityEngine.UI;
using NocturneAnnex.Core;
using NocturneAnnex.UI;

namespace NocturneAnnex.Editor
{
    /// <summary>
    /// Programmer-art building blocks for generated screens: flat dark palette, no sprites.
    /// Final art replaces these through the generated scenes, not by hand-editing them.
    /// </summary>
    public static class UiKit
    {
        public static readonly Color Bg = Hex("0B0D10");
        public static readonly Color Surface = Hex("14181D");
        public static readonly Color Button = Hex("1C2128");
        public static readonly Color Text = Hex("D9DEE3");
        public static readonly Color TextDim = Hex("8A939C");
        public static readonly Color Accent = Hex("D9A441");
        public static readonly Color Warn = Hex("E0A04A");
        public static readonly Color Error = Hex("E06C6C");

        public const float ButtonHeight = 120f;   // about 48 dp on a 6-inch 1080p phone

        static Color Hex(string h)
        {
            ColorUtility.TryParseHtmlString("#" + h, out var c);
            return c;
        }

        public static GameObject Child(string name, Transform parent)
        {
            var go = new GameObject(name, typeof(RectTransform));
            go.transform.SetParent(parent, false);
            return go;
        }

        public static RectTransform Stretch(GameObject go, float margin = 0f)
        {
            var rt = (RectTransform)go.transform;
            rt.anchorMin = Vector2.zero;
            rt.anchorMax = Vector2.one;
            rt.offsetMin = new Vector2(margin, margin);
            rt.offsetMax = new Vector2(-margin, -margin);
            return rt;
        }

        public static Image Fill(GameObject go, Color color)
        {
            var image = go.AddComponent<Image>();
            image.color = color;
            return image;
        }

        public static void Size(GameObject go, float width, float height, float flexibleWidth = 0f)
        {
            var le = go.GetComponent<LayoutElement>() ?? go.AddComponent<LayoutElement>();
            if (width >= 0f) le.preferredWidth = width;
            if (height >= 0f) le.preferredHeight = height;
            le.flexibleWidth = flexibleWidth;
        }

        public static VerticalLayoutGroup Column(GameObject go, float spacing, TextAnchor align, int padding = 0)
        {
            var v = go.AddComponent<VerticalLayoutGroup>();
            v.spacing = spacing;
            v.childAlignment = align;
            v.childControlWidth = true;
            v.childControlHeight = true;
            v.childForceExpandWidth = false;
            v.childForceExpandHeight = false;
            v.padding = new RectOffset(padding, padding, padding, padding);
            return v;
        }

        public static HorizontalLayoutGroup Row(GameObject go, float spacing, TextAnchor align)
        {
            var h = go.AddComponent<HorizontalLayoutGroup>();
            h.spacing = spacing;
            h.childAlignment = align;
            h.childControlWidth = true;
            h.childControlHeight = true;
            h.childForceExpandWidth = false;
            h.childForceExpandHeight = false;
            return h;
        }

        /// <summary>A label. If key is not null it is bound through LocalizedText so no literal text is stored.</summary>
        public static TextMeshProUGUI Label(Transform parent, string key, float size, TextAlignmentOptions align, Color color, string name = null)
        {
            var go = Child(name ?? key ?? "Label", parent);
            var t = go.AddComponent<TextMeshProUGUI>();
            t.fontSize = size;
            t.alignment = align;
            t.color = color;
            t.textWrappingMode = TextWrappingModes.Normal;
            t.raycastTarget = false;
            go.AddComponent<ScalableText>();   // every label follows the text size setting
            if (key != null)
            {
                go.AddComponent<LocalizedText>().Key = key;
                t.text = Loc.Get(key);
            }
            return t;
        }

        public static UnityEngine.UI.Button MakeButton(Transform parent, string key, float width, float height = ButtonHeight)
        {
            var go = Child("Button_" + key, parent);
            var image = Fill(go, Button);
            var b = go.AddComponent<UnityEngine.UI.Button>();
            b.targetGraphic = image;
            var cb = b.colors;
            cb.normalColor = Color.white;
            cb.highlightedColor = new Color(1.5f, 1.5f, 1.5f, 1f);
            cb.selectedColor = new Color(1.5f, 1.5f, 1.5f, 1f);
            cb.pressedColor = new Color(0.75f, 0.75f, 0.75f, 1f);
            cb.disabledColor = new Color(1f, 1f, 1f, 0.35f);
            b.colors = cb;
            Size(go, width, height);

            var label = Label(go.transform, key, 44, TextAlignmentOptions.Center, Text, "Label");
            Stretch(label.gameObject, 8f);
            return b;
        }
    }
}
