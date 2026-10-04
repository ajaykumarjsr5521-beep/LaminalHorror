using UnityEngine;

namespace NocturneAnnex.Core
{
    /// <summary>
    /// Shared caption service and a one-line way to post from anywhere in the game:
    /// Captions.Post("caption.door_creak"). Uses unscaled time so captions still expire while the game is paused.
    /// </summary>
    public static class Captions
    {
        public static CaptionService Service { get; } = new CaptionService();

        public static void Post(string key, float durationSeconds = CaptionService.DefaultDurationSeconds, int priority = 0) =>
            Service.Post(key, Time.unscaledTime, durationSeconds, priority);

        // Keeps state correct when Enter Play Mode runs without a domain reload.
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        static void ResetOnLoad() => Service.Clear();
    }
}
