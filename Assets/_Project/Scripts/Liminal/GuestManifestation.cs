using UnityEngine;
using NocturneAnnex.Core;

namespace NocturneAnnex.Liminal
{
    /// <summary>
    /// The Guest's first manifestation: a silhouette in the glass that appears only once the legend has led the player to Sighting,
    /// only while the player is not looking, never inside the quiet window, and vanishes when looked at.
    /// </summary>
    public class GuestManifestation : MonoBehaviour
    {
        public const float UnwatchedBeforeAppearing = 2f, WatchedBeforeVanishing = 0.5f, MaxVisibleSeconds = 20f;
        public const string CueId = "event.figure";

        public LiminalDirector Director;
        public GameObject Figure;
        public Camera ViewCamera;

        public AttentionTracker Attention { get; } = new AttentionTracker();
        public bool IsVisible => Figure != null && Figure.activeSelf;
        /// <summary>Test hook: when set, replaces the camera check.</summary>
        public System.Func<bool> WatchedOverride;

        float _visibleFor;

        void Start()
        {
            if (Figure != null) Figure.SetActive(false);
            if (ViewCamera == null) ViewCamera = Camera.main;
        }

        void Update() => Tick(Time.deltaTime);

        public bool IsWatched()
        {
            if (WatchedOverride != null) return WatchedOverride();
            if (ViewCamera == null || Figure == null) return false;
            Vector3 p = Figure.transform.position + Vector3.up * 0.9f;
            Vector3 vp = ViewCamera.WorldToViewportPoint(p);
            if (vp.z <= 0f || vp.x < 0.05f || vp.x > 0.95f || vp.y < 0.05f || vp.y > 0.95f) return false;
            return !Physics.Linecast(ViewCamera.transform.position, p, out _, ~0, QueryTriggerInteraction.Ignore);   // nothing in the way
        }

        public void Tick(float deltaSeconds)
        {
            if (Director == null || Figure == null || Director.Blocked) return;
            Attention.Tick(deltaSeconds, IsWatched());
            if (!IsVisible)
            {
                if (Director.Phase.Phase >= EntityPhase.Sighting && Director.Phase.Phase < EntityPhase.Hunt
                    && Director.CanManifest() && Attention.UnwatchedSeconds >= UnwatchedBeforeAppearing)
                    Appear();
                return;
            }
            _visibleFor += deltaSeconds;
            if (Attention.WatchedSeconds >= WatchedBeforeVanishing || _visibleFor >= MaxVisibleSeconds) Vanish();
        }

        void Appear()
        {
            Figure.SetActive(true);
            _visibleFor = 0f;
            Director.NoteManifested();
            Captions.Post("event.figure");
            Cues.Play(CueId, Figure.transform.position);
        }

        void Vanish() => Figure.SetActive(false);
    }
}
