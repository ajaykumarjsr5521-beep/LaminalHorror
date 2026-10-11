using System;
using UnityEngine;
using NocturneAnnex.Core;
using NocturneAnnex.Interaction;

namespace NocturneAnnex.Level
{
    /// <summary>A hidden switch or fuse box. Using it unlocks one door; with a RequiredItemId (a fuse) it only works when the player holds that item, which it uses up.</summary>
    public sealed class UnlockSwitch : MonoBehaviour, IInteractable
    {
        public Door TargetDoor;
        public string RequiredItemId = "";

        public bool Used { get; private set; }

        /// <summary>Raised when the player uses the switch (not when a saved use is restored).</summary>
        public event Action Activated;

        public string Prompt => Used ? "" : Loc.Get("switch.prompt");

        public void Interact(Interactor interactor)
        {
            if (Used) return;
            if (!string.IsNullOrEmpty(RequiredItemId))
            {
                bool has = interactor != null && interactor.Keys != null && interactor.Keys.HasKey(RequiredItemId);
                if (!has) { Captions.Post("switch.message.needs_item"); return; }
                interactor.Keys.ConsumeKey(RequiredItemId);
            }
            Used = true;
            if (TargetDoor != null) TargetDoor.Unlock();
            Captions.Post("switch.message.done");
            Activated?.Invoke();
        }

        /// <summary>Restores a switch that was already used before a reload.</summary>
        public void RestoreUsed()
        {
            Used = true;
            if (TargetDoor != null) TargetDoor.Unlock();
        }
    }
}
