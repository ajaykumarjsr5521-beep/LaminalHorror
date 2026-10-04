using TMPro;
using UnityEngine;

namespace NocturneAnnex.Interaction
{
    /// <summary>Shows the focused interactable's prompt. Hidden when nothing is in focus.</summary>
    public class InteractionPromptView : MonoBehaviour
    {
        public Interactor Interactor;
        public TMP_Text Label;

        void OnEnable()
        {
            if (Interactor == null) return;
            Interactor.FocusChanged += Refresh;
            Refresh(Interactor.Focus);
        }

        void OnDisable()
        {
            if (Interactor != null) Interactor.FocusChanged -= Refresh;
        }

        void Refresh(IInteractable focus)
        {
            string text = focus != null ? focus.Prompt : string.Empty;
            Label.text = text;
            Label.gameObject.SetActive(!string.IsNullOrEmpty(text));
        }
    }
}
