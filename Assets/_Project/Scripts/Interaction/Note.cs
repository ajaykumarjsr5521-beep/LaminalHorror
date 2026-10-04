using System;
using UnityEngine;

namespace NocturneAnnex.Interaction
{
    /// <summary>Readable lore/puzzle note. The reader UI (F-07) subscribes to Opened.</summary>
    public class Note : MonoBehaviour, IInteractable
    {
        public string Title = "Note";
        [TextArea(3, 10)] public string Body = "";

        public static event Action<Note> Opened;

        public string Prompt => "Read";

        public void Interact(Interactor interactor) => Opened?.Invoke(this);
    }
}
