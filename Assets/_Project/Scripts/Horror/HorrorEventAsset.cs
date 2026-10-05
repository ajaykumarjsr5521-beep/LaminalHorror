using UnityEngine;

namespace NocturneAnnex.Horror
{
    public enum HorrorEventKind { LightFlicker, DoorSlam, PropShift, AudioCue, MisfileReveal, ShadowFigure }

    /// <summary>Authored data for one scare event. Editing these assets changes pacing and behaviour without code.</summary>
    [CreateAssetMenu(menuName = "Nocturne Annex/Horror Event", fileName = "HorrorEvent")]
    public class HorrorEventAsset : ScriptableObject
    {
        public string Id = "";
        public HorrorEventKind Kind = HorrorEventKind.AudioCue;
        [Tooltip("Fire at most once per run (saved).")] public bool Once = true;
        public float CooldownSeconds = 120f;
        [Range(0f, 1f)] public float MinTension = 0.6f;
        [Tooltip("Caption string key, e.g. event.door_slam. Empty means no caption.")] public string CaptionKey = "";
        public float DurationSeconds = 2f;
        [Tooltip("Flicker dip depth or shake strength, 0 to 1.")][Range(0f, 1f)] public float Strength = 0.7f;
        [Tooltip("Authored flicker rate. The flash budget caps real changes at 3 per second.")] public float FlickerHz = 6f;

        public EventCandidate ToCandidate() => new EventCandidate(Id, Once, CooldownSeconds, MinTension);
    }
}
