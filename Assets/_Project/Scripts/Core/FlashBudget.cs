using System.Collections.Generic;

namespace NocturneAnnex.Core
{
    /// <summary>
    /// Allows at most MaxFlashes in any rolling window (default 3 per second, the photosensitivity limit in F-08).
    /// Lighting and screen effects ask TryFlash before changing state abruptly and skip the flash when refused.
    /// Times must not go backwards; an earlier time is treated as the latest one seen, which can only be stricter.
    /// </summary>
    public class FlashBudget
    {
        readonly int _maxFlashes;
        readonly float _windowSeconds;
        readonly Queue<float> _recent = new Queue<float>();
        float _latest = float.NegativeInfinity;

        public FlashBudget(int maxFlashes = 3, float windowSeconds = 1f)
        {
            _maxFlashes = maxFlashes;
            _windowSeconds = windowSeconds;
        }

        public bool TryFlash(float timeSeconds)
        {
            if (timeSeconds < _latest) timeSeconds = _latest;
            _latest = timeSeconds;

            while (_recent.Count > 0 && _recent.Peek() <= timeSeconds - _windowSeconds) _recent.Dequeue();
            if (_recent.Count >= _maxFlashes) return false;
            _recent.Enqueue(timeSeconds);
            return true;
        }

        public void Clear()
        {
            _recent.Clear();
            _latest = float.NegativeInfinity;
        }
    }
}
