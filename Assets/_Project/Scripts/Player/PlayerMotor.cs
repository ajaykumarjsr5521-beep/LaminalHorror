using System;
using UnityEngine;
using NocturneAnnex.Controls;

namespace NocturneAnnex.Player
{
    /// <summary>CharacterController-based walk / sprint / crouch with a headroom check before standing.</summary>
    [RequireComponent(typeof(CharacterController))]
    public class PlayerMotor : MonoBehaviour
    {
        [Header("Speeds (m/s)")]
        public float WalkSpeed = 3f;
        public float SprintSpeed = 5f;
        public float CrouchSpeed = 1.5f;
        public float Acceleration = 25f;

        [Header("Body")]
        public float StandHeight = 1.8f;
        public float CrouchHeight = 1.0f;
        public float HeightChangeSpeed = 6f;
        public float EyeOffsetFromTop = 0.15f;
        public Transform CameraPivot;

        [Header("Physics")]
        public float Gravity = -20f;

        public Func<PlayerInputState> InputProvider;
        public StaminaModel Stamina { get; } = new StaminaModel();
        public bool IsCrouched { get; private set; }
        public bool IsSprinting { get; private set; }
        public Vector3 HorizontalVelocity => _horizontal;

        CharacterController _cc;
        Vector3 _horizontal;
        float _vertical;
        static readonly Collider[] HeadroomBuffer = new Collider[8];

        void Awake()
        {
            _cc = GetComponent<CharacterController>();
            ApplyHeight(StandHeight);
        }

        void Update()
        {
            var input = (InputProvider ?? DefaultProvider)();
            float dt = Time.deltaTime;

            UpdateCrouch(input.Crouch);

            bool sprinting = input.Sprint && !IsCrouched && input.Move.y > 0.1f && Stamina.CanSprint;
            IsSprinting = sprinting;
            Stamina.Tick(dt, sprinting);

            float speed = IsCrouched ? CrouchSpeed : (sprinting ? SprintSpeed : WalkSpeed);
            var wish = transform.TransformDirection(new Vector3(input.Move.x, 0f, input.Move.y)) * speed;
            _horizontal = Vector3.MoveTowards(_horizontal, wish, Acceleration * dt);

            _vertical = _cc.isGrounded ? -2f : _vertical + Gravity * dt;
            _cc.Move((_horizontal + Vector3.up * _vertical) * dt);

            float target = IsCrouched ? CrouchHeight : StandHeight;
            if (!Mathf.Approximately(_cc.height, target))
                ApplyHeight(Mathf.MoveTowards(_cc.height, target, HeightChangeSpeed * dt));
        }

        void UpdateCrouch(bool crouchHeld)
        {
            if (crouchHeld) IsCrouched = true;
            else if (IsCrouched && HasHeadroom()) IsCrouched = false;
        }

        bool HasHeadroom()
        {
            float r = _cc.radius * 0.95f;
            var feet = transform.position + _cc.center - Vector3.up * (_cc.height * 0.5f);
            var bottom = feet + Vector3.up * (r + _cc.skinWidth);
            var top = feet + Vector3.up * (StandHeight - r);
            int n = Physics.OverlapCapsuleNonAlloc(bottom, top, r, HeadroomBuffer, ~0, QueryTriggerInteraction.Ignore);
            for (int i = 0; i < n; i++)
                if (HeadroomBuffer[i] != _cc) return false;
            return true;
        }

        void ApplyHeight(float h)
        {
            // keep the feet planted while the capsule grows/shrinks
            float oldBottom = _cc.center.y - _cc.height * 0.5f;
            _cc.height = h;
            _cc.center = new Vector3(0f, oldBottom + h * 0.5f, 0f);
            if (CameraPivot != null)
                CameraPivot.localPosition = new Vector3(0f, oldBottom + h - EyeOffsetFromTop, 0f);
        }

        static PlayerInputState DefaultProvider() =>
            InputRouter.Instance != null ? InputRouter.Instance.Current : default;
    }
}
