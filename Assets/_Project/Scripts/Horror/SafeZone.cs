using UnityEngine;
using NocturneAnnex.Player;

namespace NocturneAnnex.Horror
{
    /// <summary>
    /// Volume for a safe pocket (lamp room). While the player is inside, tension decays instead of rising.
    /// It checks the player's position against the collider each time it is asked, rather than counting enter and exit
    /// events, because a respawn teleport can skip the exit event and leave a stale "inside" flag.
    /// </summary>
    [RequireComponent(typeof(Collider))]
    public class SafeZone : MonoBehaviour
    {
        Collider _volume;
        PlayerMotor _player;

        public bool PlayerInside
        {
            get
            {
                if (_volume == null) _volume = GetComponent<Collider>();
                if (_player == null) _player = FindFirstObjectByType<PlayerMotor>();
                if (_player == null) return false;
                var p = _player.transform.position;
                return _volume.ClosestPoint(p) == p;
            }
        }

        void Reset() => GetComponent<Collider>().isTrigger = true;
    }
}
