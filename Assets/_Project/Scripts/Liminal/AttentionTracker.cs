namespace NocturneAnnex.Liminal
{
    /// <summary>
    /// How long the player has been looking at, or away from, something. The game decides "in view" (camera frustum plus line of sight)
    /// and feeds it in. Watched-only and unwatched-only manifestations read the times from here.
    /// </summary>
    public class AttentionTracker
    {
        public bool IsWatched { get; private set; }
        public float WatchedSeconds { get; private set; }
        public float UnwatchedSeconds { get; private set; }

        public void Tick(float deltaSeconds, bool inView)
        {
            if (inView != IsWatched)
            {
                IsWatched = inView;
                if (inView) UnwatchedSeconds = 0f; else WatchedSeconds = 0f;
            }
            if (inView) WatchedSeconds += deltaSeconds; else UnwatchedSeconds += deltaSeconds;
        }

        public void Reset()
        {
            IsWatched = false;
            WatchedSeconds = UnwatchedSeconds = 0f;
        }
    }
}
