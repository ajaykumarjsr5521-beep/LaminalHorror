using UnityEngine;
using NocturneAnnex.Core;
using NocturneAnnex.Interaction;

namespace NocturneAnnex.Entity
{
    /// <summary>A bottle the player can pick up with Interact and throw to make noise elsewhere.</summary>
    public class ThrowableBottle : NoisyProp, IInteractable
    {
        public string Prompt => IsHeld ? "" : Loc.Get("bottle.prompt");
        public bool IsHeld { get; private set; }

        public void Interact(Interactor interactor)
        {
            var thrower = interactor != null ? interactor.GetComponentInParent<PlayerThrower>() : null;
            if (thrower != null) thrower.TryHold(this);
        }

        internal void SetHeld(bool held)
        {
            IsHeld = held;
            var rb = GetComponent<Rigidbody>();
            rb.isKinematic = held;
            foreach (var col in GetComponentsInChildren<Collider>()) col.enabled = !held;
            gameObject.SetActive(!held);
        }

        internal void Launch(Vector3 from, Vector3 velocity)
        {
            transform.position = from;
            SetHeld(false);
            WasThrown = true;
            var rb = GetComponent<Rigidbody>();
            rb.linearVelocity = velocity;
            rb.angularVelocity = Random.insideUnitSphere * 6f;
        }
    }
}
