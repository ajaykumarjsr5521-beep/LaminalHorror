using System;
using UnityEngine;
using NocturneAnnex.Core;

namespace NocturneAnnex.Interaction
{
    /// <summary>Readable lore/puzzle note. The reader UI (F-07) subscribes to Opened.</summary>
    public class Note : MonoBehaviour, IInteractable
    {
        /// <summary>Optional journal item id. When set, reading the note records it in the inventory journal.</summary>
        public string ItemId = "";
        public string Title = "Note";
        [TextArea(3, 10)] public string Body = "";

        public static event Action<Note> Opened;

        public string Prompt => Loc.Get("note.prompt");

        public virtual void Interact(Interactor interactor)
        {
            // A full or duplicate journal entry must never block reading the note.
            if (!string.IsNullOrEmpty(ItemId) && interactor != null && interactor.Items != null)
                interactor.Items.TryAdd(ItemId);
            Opened?.Invoke(this);
        }
    }
}
