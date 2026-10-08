using UnityEngine;
using NocturneAnnex.Interaction;

namespace NocturneAnnex.Entity
{
    /// <summary>Makes a door audible to the entity whenever it is opened or closed. Watches the door state, so Door stays unchanged.</summary>
    [RequireComponent(typeof(Door))]
    public class DoorNoise : MonoBehaviour
    {
        public NoiseHub Hub;
        [Tooltip("A slammed door is much louder; set for doors that bang shut.")] public bool Slams;

        Door _door;
        bool _wasOpen;

        void Awake() { _door = GetComponent<Door>(); _wasOpen = _door.IsOpen; }

        void Update()
        {
            if (_door.IsOpen == _wasOpen) return;
            _wasOpen = _door.IsOpen;
            if (Hub != null)
                Hub.Emit(transform.position, (!_door.IsOpen && Slams) ? NoiseKind.DoorSlam : NoiseKind.DoorGentle);
        }
    }
}
