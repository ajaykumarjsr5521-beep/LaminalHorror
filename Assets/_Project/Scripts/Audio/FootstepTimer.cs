namespace NocturneAnnex.Audio
{
    /// <summary>Turns distance walked into footsteps: one step per stride, none in the air, leftover distance carries over.</summary>
    public class FootstepTimer
    {
        float _travelled;

        /// <summary>Adds distance and returns true when a step is due. At most one step per call.</summary>
        public bool Advance(float distance, bool grounded, float stride)
        {
            if (!grounded || stride <= 0f) { _travelled = 0f; return false; }
            _travelled += distance;
            if (_travelled < stride) return false;
            _travelled -= stride;
            if (_travelled > stride) _travelled = 0f;   // a huge jump in distance (teleport) must not queue many steps
            return true;
        }

        public void Reset() => _travelled = 0f;
    }
}
