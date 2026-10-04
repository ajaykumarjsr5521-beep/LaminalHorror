using UnityEngine;
using UnityEngine.InputSystem;

namespace NocturneAnnex.Controls
{
    /// <summary>Shows the on-screen touch controls only where they make sense: on mobile, or when a touchscreen is present.</summary>
    public class TouchControlsVisibility : MonoBehaviour
    {
        public GameObject Content;

        /// <summary>Tests and screenshot tools set this to force the result; null uses the real device state.</summary>
        public bool? Override;

        public static bool ShouldShow(bool isMobilePlatform, bool hasTouchscreen) => isMobilePlatform || hasTouchscreen;

        void OnEnable() => Refresh();

        public void Refresh()
        {
            bool show = Override ?? ShouldShow(Application.isMobilePlatform, Touchscreen.current != null);
            if (Content != null) Content.SetActive(show);
        }
    }
}
