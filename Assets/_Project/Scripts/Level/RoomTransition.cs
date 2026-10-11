using UnityEngine;
using NocturneAnnex.Player;
using NocturneAnnex.Save;

namespace NocturneAnnex.Level
{
    /// <summary>
    /// A trigger volume just past a door. Walking into it (so the door is open) moves the player to the next room, or back to the
    /// previous one. TargetRoom past the last room is the exit of the building.
    /// </summary>
    [RequireComponent(typeof(Collider))]
    public sealed class RoomTransition : MonoBehaviour
    {
        public SaveGame Save;
        public int TargetRoom;
        public bool Back;

        bool _used;

        void Reset() => GetComponent<Collider>().isTrigger = true;

        void OnTriggerEnter(Collider other)
        {
            if (_used || other.GetComponentInParent<PlayerMotor>() == null) return;
            _used = true;
            RoomFlow.Go(Save, TargetRoom, Back);
        }
    }
}
