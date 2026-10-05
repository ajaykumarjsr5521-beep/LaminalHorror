using UnityEngine;

namespace NocturneAnnex.Horror
{
    /// <summary>
    /// Shows tension, the last event and whether an event may fire, so pacing can be tuned. The body is compiled only in the
    /// Editor and development builds, so none of it ships in a release build.
    /// </summary>
    public class HorrorDebugOverlay : MonoBehaviour
    {
        public HorrorEventRunner Runner;

#if UNITY_EDITOR || DEVELOPMENT_BUILD
        public bool Visible = true;

        void OnGUI()
        {
            if (!Visible || Runner == null || Runner.Director == null) return;
            var d = Runner.Director;
            GUI.Label(new Rect(10f, 140f, 460f, 70f),
                $"Tension {d.Tension:0.00}  canFire {d.CanFire}  gap {d.EffectiveGapSeconds:0}s\n" +
                $"last event: {(string.IsNullOrEmpty(Runner.LastEventId) ? "-" : Runner.LastEventId)}  blocked {Runner.Blocked}  safe {Runner.PlayerInSafeZone}");
        }
#endif
    }
}
