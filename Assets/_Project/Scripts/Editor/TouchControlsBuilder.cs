using TMPro;
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;
using NocturneAnnex.Controls;

namespace NocturneAnnex.Editor
{
    /// <summary>
    /// Builds the on-screen touch controls: move stick (left half), look area (right half) and a column of action buttons
    /// at the bottom right. Programmer art; the widgets' private fields are set through SerializedObject.
    /// </summary>
    public static class TouchControlsBuilder
    {
        static readonly Color Ghost = new Color(1f, 1f, 1f, 0.18f);
        static readonly Color Pad = new Color(1f, 1f, 1f, 0.28f);

        /// <summary>Creates the controls as the first (rearmost) child of parent so every other HUD piece and screen sits above it.</summary>
        public static TouchControlsVisibility Build(Transform parent)
        {
            var root = UiKit.Child("TouchControls", parent);
            root.transform.SetAsFirstSibling();
            UiKit.Stretch(root);
            var visibility = root.AddComponent<TouchControlsVisibility>();

            var content = UiKit.Child("Content", root.transform);
            UiKit.Stretch(content);
            visibility.Content = content;

            BuildLookArea(content.transform);
            BuildMoveArea(content.transform);
            BuildButtons(content.transform);
            return visibility;
        }

        static void BuildLookArea(Transform parent)
        {
            var area = UiKit.Child("LookArea", parent);
            Half(area, left: false);
            Hit(area);
            area.AddComponent<TouchLookArea>();
        }

        static void BuildMoveArea(Transform parent)
        {
            var area = UiKit.Child("MoveArea", parent);
            Half(area, left: true);
            Hit(area);
            var stick = area.AddComponent<VirtualStick>();

            // fixed visual hint; the real origin floats to wherever the thumb lands
            var ring = Circle(area.transform, "Ring", Ghost, 300f);
            var rt = (RectTransform)ring.transform;
            rt.anchorMin = rt.anchorMax = new Vector2(0f, 0f);
            rt.anchoredPosition = new Vector2(260f, 260f);
            var knob = Circle(ring.transform, "Knob", Pad, 130f);

            var so = new SerializedObject(stick);
            so.FindProperty("_knob").objectReferenceValue = knob.transform;
            so.ApplyModifiedPropertiesWithoutUndo();
        }

        static void BuildButtons(Transform parent)
        {
            var group = UiKit.Child("Buttons", parent);
            var rt = (RectTransform)group.transform;
            rt.anchorMin = rt.anchorMax = rt.pivot = new Vector2(1f, 0f);
            rt.sizeDelta = new Vector2(200f, 640f);
            rt.anchoredPosition = new Vector2(-40f, 40f);
            group.AddComponent<TouchControlsScaler>();

            Action(group.transform, "Interact", "touch.interact", TouchButton.Kind.Interact, 100f, 200f);
            Action(group.transform, "Sprint", "touch.sprint", TouchButton.Kind.Sprint, 330f, 160f);
            Action(group.transform, "Crouch", "touch.crouch", TouchButton.Kind.Crouch, 520f, 160f);
        }

        static void Action(Transform parent, string name, string labelKey, TouchButton.Kind kind, float y, float size)
        {
            var image = Circle(parent, name, Pad, size);
            var rt = (RectTransform)image.transform;
            rt.anchorMin = rt.anchorMax = new Vector2(0.5f, 0f);
            rt.anchoredPosition = new Vector2(0f, y);
            image.raycastTarget = true;
            var button = image.gameObject.AddComponent<TouchButton>();
            var so = new SerializedObject(button);
            so.FindProperty("_kind").enumValueIndex = (int)kind;
            so.ApplyModifiedPropertiesWithoutUndo();

            var label = UiKit.Label(image.transform, labelKey, 36, TextAlignmentOptions.Center, UiKit.Text, "Label");
            UiKit.Stretch(label.gameObject, 4f);
        }

        static void Half(GameObject go, bool left)
        {
            var rt = (RectTransform)go.transform;
            rt.anchorMin = new Vector2(left ? 0f : 0.5f, 0f);
            rt.anchorMax = new Vector2(left ? 0.5f : 1f, 1f);
            rt.offsetMin = rt.offsetMax = Vector2.zero;
        }

        /// <summary>Invisible but touchable.</summary>
        static void Hit(GameObject go)
        {
            var image = go.AddComponent<Image>();
            image.color = new Color(0f, 0f, 0f, 0f);
            image.raycastTarget = true;
        }

        static Image Circle(Transform parent, string name, Color color, float size)
        {
            var go = UiKit.Child(name, parent);
            var image = go.AddComponent<Image>();
            image.sprite = AssetDatabase.GetBuiltinExtraResource<Sprite>("UI/Skin/Knob.psd");
            image.color = color;
            image.raycastTarget = false;
            ((RectTransform)go.transform).sizeDelta = new Vector2(size, size);
            return image;
        }
    }
}
