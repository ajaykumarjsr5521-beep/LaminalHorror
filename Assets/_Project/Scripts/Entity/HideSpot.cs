using UnityEngine;
using UnityEngine.Events;
using NocturneAnnex.Controls;
using NocturneAnnex.Core;
using NocturneAnnex.Interaction;
using NocturneAnnex.Player;

namespace NocturneAnnex.Entity
{
    /// <summary>
    /// A cupboard, wardrobe or dark corner. Interact to hide: the player is moved inside, cannot walk, can still look around,
    /// and presses Interact again to leave. Getting in makes noise (crouching is quiet, sprinting loud) and is refused while the
    /// entity is looking at the player. The entity can inspect the spot; the chance comes from HideInspection.
    /// </summary>
    public class HideSpot : MonoBehaviour, IInteractable
    {
        [Range(0f, 1f), Tooltip("0 = poor cover, 1 = excellent. Decides how often the entity opens it.")] public float Safety = 0.5f;
        public Transform HidePoint;
        public Transform ExitPoint;
        public NoiseHub Hub;
        public StalkerAgent Stalker;
        public UnityEvent<string> OnMessage = new UnityEvent<string>();

        public bool Occupied { get; private set; }
        /// <summary>0 (silent) to 1 (very loud): the noise made getting in. Read by HideInspection.</summary>
        public float EntryNoise { get; private set; }

        PlayerMotor _motor;
        CharacterController _cc;
        int _enteredFrame, _leftFrame = -1;

        public string Prompt => Loc.Get(Occupied ? "hide.prompt.exit" : "hide.prompt.enter");

        /// <summary>Where the entity stands to open this spot.</summary>
        public Vector3 InspectPoint => ExitPoint != null ? ExitPoint.position : transform.position;

        public void Interact(Interactor interactor)
        {
            if (Occupied) { Leave(); return; }
            if (interactor == null || Time.frameCount == _leftFrame) return;   // the press that left must not re-enter
            if (Stalker != null && Stalker.SeesPlayerNow) { OnMessage.Invoke(Loc.Get("hide.message.seen")); return; }
            Enter(interactor.GetComponentInParent<PlayerMotor>());
        }

        void Enter(PlayerMotor motor)
        {
            if (motor == null || HidePoint == null) return;
            _motor = motor;
            _cc = motor.GetComponent<CharacterController>();
            EntryNoise = motor.IsCrouched ? 0.1f : (motor.IsSprinting ? 0.9f : 0.4f);
            Hub?.Emit(HidePoint.position, motor.IsCrouched ? NoiseKind.Crouch : NoiseKind.Interaction, motor.IsSprinting ? 1.5f : 1f);
            Move(HidePoint.position, HidePoint.rotation);
            _motor.enabled = false;
            Occupied = true;
            _enteredFrame = Time.frameCount;
        }

        public void Leave()
        {
            if (!Occupied) return;
            Occupied = false;
            _leftFrame = Time.frameCount;
            Move(InspectPoint, ExitPoint != null ? ExitPoint.rotation : _motor.transform.rotation);
            _motor.enabled = true;
            Hub?.Emit(InspectPoint, NoiseKind.DoorGentle, 0.5f);
        }

        void Move(Vector3 position, Quaternion rotation)
        {
            if (_cc != null) _cc.enabled = false;
            _motor.transform.SetPositionAndRotation(position, Quaternion.Euler(0f, rotation.eulerAngles.y, 0f));
            if (_cc != null) _cc.enabled = true;
        }

        void Update()
        {
            // Interact is also the exit key, but never on the frame that entered.
            if (Occupied && Time.frameCount > _enteredFrame && InputRouter.Instance != null && InputRouter.Instance.Current.InteractPressed)
                Leave();
        }

        /// <summary>The entity opens the spot. Returns what it found.</summary>
        public InspectOutcome Inspect(float random01, float memoryBonus = 0f) =>
            HideInspection.Resolve(HideInspection.Chance(Safety, EntryNoise, memoryBonus), random01, Occupied);
    }
}
