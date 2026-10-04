namespace NocturneAnnex.Interaction
{
    public interface IInteractable
    {
        /// <summary>Short verb shown in the prompt, e.g. "Open". Empty hides the prompt.</summary>
        string Prompt { get; }

        void Interact(Interactor interactor);
    }

    /// <summary>Implemented by the inventory (F-04) so locked doors can check for keys.</summary>
    public interface IKeyProvider
    {
        bool HasKey(string keyId);
    }

    /// <summary>Implemented by the inventory (F-04) so pickups can be collected.</summary>
    public interface IItemReceiver
    {
        /// <summary>Returns false if the item cannot be carried.</summary>
        bool TryAdd(string itemId);
    }
}
