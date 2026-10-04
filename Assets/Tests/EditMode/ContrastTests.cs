using NUnit.Framework;
using UnityEngine;
using NocturneAnnex.Editor;

namespace NocturneAnnex.Tests.EditMode
{
    /// <summary>WCAG 2.x contrast checks for the UI palette: text needs at least 4.5:1 against its background.</summary>
    public class ContrastTests
    {
        const float Required = 4.5f;

        static float Channel(float c) => c <= 0.03928f ? c / 12.92f : Mathf.Pow((c + 0.055f) / 1.055f, 2.4f);

        static float Luminance(Color c) => 0.2126f * Channel(c.r) + 0.7152f * Channel(c.g) + 0.0722f * Channel(c.b);

        public static float Ratio(Color a, Color b)
        {
            float la = Luminance(a), lb = Luminance(b);
            float hi = Mathf.Max(la, lb), lo = Mathf.Min(la, lb);
            return (hi + 0.05f) / (lo + 0.05f);
        }

        static Color Over(Color top, Color under) => Color.Lerp(under, new Color(top.r, top.g, top.b, 1f), top.a);

        // ---- the helper itself ----

        [Test]
        public void Ratio_BlackOnWhite_Is21()
        {
            Assert.AreEqual(21f, Ratio(Color.black, Color.white), 0.05f);
        }

        [Test]
        public void Ratio_IsSymmetric_AndOneForSameColour()
        {
            Assert.AreEqual(Ratio(UiKit.Text, UiKit.Bg), Ratio(UiKit.Bg, UiKit.Text), 1e-4f);
            Assert.AreEqual(1f, Ratio(UiKit.Accent, UiKit.Accent), 1e-4f);
        }

        // ---- the palette ----

        static readonly object[] TextOnBackgrounds =
        {
            new object[] { "Text on Bg", "Text", "Bg" },
            new object[] { "Text on Surface", "Text", "Surface" },
            new object[] { "Text on Button", "Text", "Button" },
            new object[] { "TextDim on Bg", "TextDim", "Bg" },
            new object[] { "TextDim on Surface", "TextDim", "Surface" },
            new object[] { "Accent on Bg", "Accent", "Bg" },
            new object[] { "Accent on Surface", "Accent", "Surface" },
            new object[] { "Warn on Bg", "Warn", "Bg" },
            new object[] { "Warn on Surface", "Warn", "Surface" },
            new object[] { "Error on Bg", "Error", "Bg" },
            new object[] { "Error on Surface", "Error", "Surface" },
        };

        static Color Named(string name)
        {
            switch (name)
            {
                case "Text": return UiKit.Text;
                case "TextDim": return UiKit.TextDim;
                case "Accent": return UiKit.Accent;
                case "Warn": return UiKit.Warn;
                case "Error": return UiKit.Error;
                case "Bg": return UiKit.Bg;
                case "Surface": return UiKit.Surface;
                case "Button": return UiKit.Button;
                default: throw new System.ArgumentException(name);
            }
        }

        [TestCaseSource(nameof(TextOnBackgrounds))]
        public void PaletteText_MeetsWcagAA(string label, string foreground, string background)
        {
            float ratio = Ratio(Named(foreground), Named(background));
            Assert.GreaterOrEqual(ratio, Required, $"{label} is {ratio:0.00}:1");
        }

        [Test]
        public void ButtonText_StillReadableWhenHighlighted()
        {
            // The highlighted state multiplies the button colour by 1.5.
            var highlighted = new Color(UiKit.Button.r * 1.5f, UiKit.Button.g * 1.5f, UiKit.Button.b * 1.5f, 1f);
            Assert.GreaterOrEqual(Ratio(UiKit.Text, highlighted), Required);
        }

        [Test]
        public void CaptionText_IsReadableOverTheBrightestPossibleScene()
        {
            // Captions sit on a 78 percent black strip; worst case is pure white behind it.
            var strip = Over(new Color(0f, 0f, 0f, 0.78f), Color.white);
            float ratio = Ratio(UiKit.Text, strip);
            Assert.GreaterOrEqual(ratio, Required, $"caption text over a white scene is {ratio:0.00}:1");
        }
    }
}
