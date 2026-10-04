using System;
using UnityEngine;
using NocturneAnnex.Player;

namespace NocturneAnnex.Level
{
    /// <summary>Trigger volume at a safe lamp room. Raises Reached with its id when the player walks in.</summary>
    [RequireComponent(typeof(Collider))]
    public class CheckpointTrigger : MonoBehaviour
    {
        public string Id = "";
        [Tooltip("Where the player stands when resuming from this checkpoint. Defaults to this object.")]
        public Transform SpawnPoint;

        public event Action<CheckpointTrigger> Reached;

        public Transform Spawn => SpawnPoint != null ? SpawnPoint : transform;

        void Reset() => GetComponent<Collider>().isTrigger = true;

        void OnTriggerEnter(Collider other)
        {
            if (other.GetComponentInParent<PlayerMotor>() != null) Reached?.Invoke(this);
        }
    }
}
