using System;
using UnityEngine;
using NocturneAnnex.Player;

namespace NocturneAnnex.Level
{
    /// <summary>Trigger volume at the end of the level. Whether it counts is decided by LevelBootstrap.</summary>
    [RequireComponent(typeof(Collider))]
    public class ExitTrigger : MonoBehaviour
    {
        public event Action Entered;

        void Reset() => GetComponent<Collider>().isTrigger = true;

        void OnTriggerEnter(Collider other)
        {
            if (other.GetComponentInParent<PlayerMotor>() != null) Entered?.Invoke();
        }
    }
}
