using UnityEngine;

namespace NocturneAnnex.Controls
{
    /// <summary>Holds values written by on-screen widgets; read by the aggregator.</summary>
    public class TouchInputSource : IInputSource
    {
        Vector2 _move, _lookAccum;
        bool _sprint, _crouch, _interact, _throw, _pause;

        public void SetMove(Vector2 v) => _move = v;
        public void AddLook(Vector2 delta) => _lookAccum += delta;
        public void SetSprint(bool held) => _sprint = held;
        public void SetCrouch(bool held) => _crouch = held;
        public void PressInteract() => _interact = true;
        public void PressThrow() => _throw = true;
        public void PressPause() => _pause = true;

        public void Poll(ref PlayerInputState s)
        {
            s.Move = _move;
            s.Look = _lookAccum;
            s.Sprint = _sprint;
            s.Crouch = _crouch;
            s.InteractPressed = _interact;
            s.ThrowPressed = _throw;
            s.PausePressed = _pause;
            _lookAccum = Vector2.zero;   // consumed
            _interact = _throw = _pause = false;  // one-frame presses
        }

        public void ResetState()
        {
            _move = _lookAccum = Vector2.zero;
            _sprint = _crouch = _interact = _throw = _pause = false;
        }
    }
}
