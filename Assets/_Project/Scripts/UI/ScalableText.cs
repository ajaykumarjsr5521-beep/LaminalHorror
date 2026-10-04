using TMPro;
using UnityEngine;
using NocturneAnnex.Core;

namespace NocturneAnnex.UI
{
    /// <summary>
    /// Scales a label with the player's text size setting. The authored size is remembered the first time, so
    /// changing size repeatedly never compounds.
    /// </summary>
    [RequireComponent(typeof(TMP_Text))]
    public class ScalableText : MonoBehaviour
    {
        TMP_Text _text;
        float _baseSize;
        float _baseMin;
        float _baseMax;
        bool _captured;

        void OnEnable()
        {
            Accessibility.Changed += Apply;
            Apply();
        }

        void OnDisable() => Accessibility.Changed -= Apply;

        /// <summary>Safe to call at any time, including before Awake (used by the screenshot tool).</summary>
        public void Apply()
        {
            if (!_captured)
            {
                _text = GetComponent<TMP_Text>();
                _baseSize = _text.fontSize;
                _baseMin = _text.fontSizeMin;
                _baseMax = _text.fontSizeMax;
                _captured = true;
            }

            float scale = Accessibility.TextScale;
            _text.fontSize = _baseSize * scale;
            _text.fontSizeMin = _baseMin * scale;
            _text.fontSizeMax = _baseMax * scale;
        }
    }
}
