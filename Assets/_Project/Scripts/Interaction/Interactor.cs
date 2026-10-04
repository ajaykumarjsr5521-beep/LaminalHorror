using System;
using UnityEngine;
using NocturneAnnex.Controls;

namespace NocturneAnnex.Interaction
{
    /// <summary>
    /// Finds the interactable under the crosshair. The first collider hit decides focus,
    /// so walls between the player and an object block interaction.
    /// </summary>
    public class Interactor : MonoBehaviour
    {
        public Camera ViewCamera;
        public float Range = 2.0f;
        public LayerMask Mask = ~0;

        /// <summary>Overridable for tests; defaults to the scene InputRouter.</summary>
        public Func<PlayerInputState> InputProvider;

        public IInteractable Focus { get; private set; }
        public event Action<IInteractable> FocusChanged;

        public IKeyProvider Keys { get; private set; }
        public IItemReceiver Items { get; private set; }

        void Awake()
        {
            Keys = GetComponentInParent<IKeyProvider>();
            Items = GetComponentInParent<IItemReceiver>();
        }

        /// <summary>Call after adding an inventory at runtime.</summary>
        public void RefreshProviders()
        {
            Keys = GetComponentInParent<IKeyProvider>();
            Items = GetComponentInParent<IItemReceiver>();
        }

        void Update()
        {
            UpdateFocus();
            var input = (InputProvider ?? DefaultProvider)();
            if (input.InteractPressed && Focus != null) Focus.Interact(this);
        }

        void UpdateFocus()
        {
            IInteractable found = null;
            var cam = ViewCamera != null ? ViewCamera : Camera.main;
            if (cam != null &&
                Physics.Raycast(cam.transform.position, cam.transform.forward, out var hit, Range, Mask, QueryTriggerInteraction.Ignore))
            {
                found = hit.collider.GetComponentInParent<IInteractable>();
            }

            if (ReferenceEquals(found, Focus)) return;
            Focus = found;
            FocusChanged?.Invoke(Focus);
        }

        static PlayerInputState DefaultProvider() =>
            InputRouter.Instance != null ? InputRouter.Instance.Current : default;
    }
}
