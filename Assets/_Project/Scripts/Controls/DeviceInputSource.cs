using System;
using UnityEngine;
using UnityEngine.InputSystem;

namespace NocturneAnnex.Controls
{
    /// <summary>Keyboard + mouse + gamepad via code-defined actions (no asset to drift out of sync).</summary>
    public class DeviceInputSource : IInputSource, IDisposable
    {
        const float MouseLookScale = 0.1f;       // pixels -> degrees baseline
        const float StickLookDegPerSec = 140f;
        const float StickDeadzone = 0.2f;

        readonly InputAction _move, _look, _stickLook, _sprint, _crouch, _interact, _throw, _pause;

        public DeviceInputSource()
        {
            _move = new InputAction("Move", InputActionType.Value);
            _move.AddCompositeBinding("2DVector")
                .With("Up", "<Keyboard>/w").With("Down", "<Keyboard>/s")
                .With("Left", "<Keyboard>/a").With("Right", "<Keyboard>/d");
            _move.AddBinding("<Gamepad>/leftStick");

            _look = new InputAction("Look", InputActionType.Value, "<Mouse>/delta");
            _stickLook = new InputAction("StickLook", InputActionType.Value, "<Gamepad>/rightStick");

            _sprint = new InputAction("Sprint", InputActionType.Button);
            _sprint.AddBinding("<Keyboard>/leftShift");
            _sprint.AddBinding("<Gamepad>/leftStickPress");

            _crouch = new InputAction("Crouch", InputActionType.Button);
            _crouch.AddBinding("<Keyboard>/leftCtrl");
            _crouch.AddBinding("<Keyboard>/c");
            _crouch.AddBinding("<Gamepad>/buttonEast");

            _interact = new InputAction("Interact", InputActionType.Button);
            _interact.AddBinding("<Keyboard>/e");
            _interact.AddBinding("<Gamepad>/buttonSouth");

            _throw = new InputAction("Throw", InputActionType.Button);
            _throw.AddBinding("<Keyboard>/g");
            _throw.AddBinding("<Gamepad>/buttonWest");

            _pause = new InputAction("Pause", InputActionType.Button);
            _pause.AddBinding("<Keyboard>/escape");
            _pause.AddBinding("<Gamepad>/start");

            foreach (var a in All()) a.Enable();
        }

        InputAction[] All() => new[] { _move, _look, _stickLook, _sprint, _crouch, _interact, _throw, _pause };

        public void Poll(ref PlayerInputState s)
        {
            s.Move = InputMath.ApplyDeadzone(_move.ReadValue<Vector2>(), StickDeadzone);
            var stick = InputMath.ApplyDeadzone(_stickLook.ReadValue<Vector2>(), StickDeadzone);
            s.Look = _look.ReadValue<Vector2>() * MouseLookScale + stick * (StickLookDegPerSec * Time.unscaledDeltaTime);
            s.Sprint = _sprint.IsPressed();
            s.Crouch = _crouch.IsPressed();
            s.InteractPressed = _interact.WasPressedThisFrame();
            s.ThrowPressed = _throw.WasPressedThisFrame();
            s.PausePressed = _pause.WasPressedThisFrame();
        }

        public void ResetState()
        {
            foreach (var a in All()) { a.Disable(); a.Enable(); } // clears held state
        }

        public void Dispose()
        {
            foreach (var a in All()) a.Dispose();
        }
    }
}
