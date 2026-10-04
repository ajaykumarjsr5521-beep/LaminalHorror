using System;
using UnityEngine;

namespace NocturneAnnex.Interaction
{
    /// <summary>World item. Collected through the interactor's IItemReceiver (inventory, F-04) when present.</summary>
    public class Pickup : MonoBehaviour, IInteractable
    {
        public string ItemId = "";
        public string DisplayName = "Item";

        public event Action<Pickup, Interactor> Collected;
        public event Action<Pickup, string> Refused;

        public string Prompt => "Take " + DisplayName;

        public void Interact(Interactor interactor)
        {
            if (interactor != null && interactor.Items != null && !interactor.Items.TryAdd(ItemId))
            {
                Refused?.Invoke(this, "You can't carry any more.");
                return;
            }
            Collected?.Invoke(this, interactor);
            gameObject.SetActive(false);
        }
    }
}
