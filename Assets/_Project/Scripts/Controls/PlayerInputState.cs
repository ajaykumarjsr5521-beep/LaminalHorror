using UnityEngine;

namespace NocturneAnnex.Controls
{
    /// <summary>Platform-neutral snapshot of player intent for one frame. Gameplay reads only this.</summary>
    public struct PlayerInputState
    {
        public Vector2 Move;           // x strafe, y forward; magnitude <= 1
        public Vector2 Look;           // per-frame delta in degrees
        public bool Sprint;            // held
        public bool Crouch;            // held
        public bool InteractPressed;   // true for one frame
        public bool PausePressed;      // true for one frame
    }
}
