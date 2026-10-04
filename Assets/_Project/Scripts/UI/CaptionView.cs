using TMPro;
using UnityEngine;
using NocturneAnnex.Core;

namespace NocturneAnnex.UI
{
    /// <summary>Draws the caption lines at the bottom of the screen. The component sits on an always-active root.</summary>
    public class CaptionView : MonoBehaviour
    {
        public GameObject Panel;
        public TMP_Text Label;

        /// <summary>Overridable for tests; defaults to the shared service.</summary>
        public CaptionService Service;

        void OnEnable()
        {
            Service ??= Captions.Service;
            Service.Changed += Render;
            Render();
        }

        void OnDisable()
        {
            if (Service != null) Service.Changed -= Render;
        }

        void Update() => Service.Tick(Time.unscaledTime);

        void Render()
        {
            Label.text = string.Join("\n", Service.Lines);
            Panel.SetActive(Service.Count > 0);
        }
    }
}
