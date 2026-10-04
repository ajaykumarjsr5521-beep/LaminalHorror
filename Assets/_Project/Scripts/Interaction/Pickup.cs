using System;
using UnityEngine;
using NocturneAnnex.Core;

namespace NocturneAnnex.Interaction
{
    /// <summary>World item. Collected through the interactor's IItemReceiver (inventory, F-04) when present.</summary>
    public class Pickup : MonoBehaviour, IInteractable
    {
        public string ItemId = "";
        public string DisplayName = "Item";

        public event Action<Pickup, Interactor> Collected;
        public event Action<Pickup, string> Refused;

        public string Prompt => Loc.Format("pickup.prompt", DisplayName);

        public void Interact(Interactor interactor)
        {
            if (interactor != null && interactor.Items != null && !interactor.Items.TryAdd(ItemId))
            {
                Refused?.Invoke(this, Loc.Get("pickup.refused"));
                return;
            }
            Collected?.Invoke(this, interactor);
            gameObject.SetActive(false);
        }
    }
}
