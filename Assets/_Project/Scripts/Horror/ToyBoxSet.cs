using System;
using UnityEngine;
using NocturneAnnex.Core;
using NocturneAnnex.Interaction;

namespace NocturneAnnex.Horror
{
    /// <summary>One toy box. Interacting opens it through its set.</summary>
    public sealed class ToyBox : MonoBehaviour, IInteractable
    {
        public ToyBoxSet Set;
        public int Index;

        public string Prompt => Set != null && !Set.Model.IsOpened(Index) ? Loc.Get("toybox.prompt") : "";

        public void Interact(Interactor interactor) { if (Set != null) Set.Open(Index, interactor); }
    }

    /// <summary>
    /// Eight toy boxes with one key, a spring toy and a footstep beat (ToyBoxModel). Opening a box shows what it held; the key goes straight
    /// to the player's inventory, and if the inventory is full the box stays shut so the key can never be lost.
    /// </summary>
    public sealed class ToyBoxSet : MonoBehaviour
    {
        public string KeyItemId = "";
        public int Seed = 1;
        public Transform[] Boxes = new Transform[0];

        public ToyBoxModel Model { get; private set; }
        public event Action<int, ToyBoxContent> Opened;

        void Awake()
        {
            Model = new ToyBoxModel(Seed);
            for (int i = 0; i < Boxes.Length; i++)
            {
                if (Boxes[i] == null) continue;
                var box = Boxes[i].gameObject.GetComponent<ToyBox>() ?? Boxes[i].gameObject.AddComponent<ToyBox>();
                box.Set = this; box.Index = i;
            }
        }

        public bool Open(int index, Interactor interactor)
        {
            if (Model.IsOpened(index) || index < 0 || index >= Boxes.Length) return false;
            if (Model.Peek(index) == ToyBoxContent.Key && !string.IsNullOrEmpty(KeyItemId)
                && interactor != null && interactor.Items != null && !interactor.Items.TryAdd(KeyItemId))
            {
                Captions.Post("pickup.refused");
                return false;
            }
            var content = Model.Open(index);
            var at = Boxes[index].position;
            switch (content)
            {
                case ToyBoxContent.Key: Captions.Post("toybox.key"); break;
                case ToyBoxContent.SpringToy: Captions.Post("toybox.spring"); Cues.Play("toybox.spring", at); break;
                case ToyBoxContent.FootstepBeat: Captions.Post("toybox.beat"); Cues.Play("entity.step.wood.1", at); break;
                default: Captions.Post("toybox.empty", 1.5f); break;
            }
            var lid = Boxes[index];
            lid.localScale = new Vector3(lid.localScale.x, lid.localScale.y * 0.6f, lid.localScale.z);   // visibly opened
            Opened?.Invoke(index, content);
            return true;
        }
    }
}
