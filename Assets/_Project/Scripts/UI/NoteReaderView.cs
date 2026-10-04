using TMPro;
using UnityEngine;
using UnityEngine.UI;
using NocturneAnnex.Core;
using NocturneAnnex.Interaction;

namespace NocturneAnnex.UI
{
    /// <summary>
    /// Shows a note when the player reads it in the world. The component sits on an always-active root so it can
    /// listen for Note.Opened while the Panel child is hidden.
    /// </summary>
    public class NoteReaderView : MonoBehaviour
    {
        public GameObject Panel;
        public TMP_Text Title;
        public TMP_Text Body;
        public Button CloseButton;

        ModalGate.Handle _modal;

        public bool IsOpen => Panel.activeSelf;

        void Awake()
        {
            CloseButton.onClick.AddListener(Close);
            Panel.SetActive(false);
        }

        void OnEnable() => Note.Opened += Show;

        void OnDisable()
        {
            Note.Opened -= Show;
            Close();
        }

        public void Show(Note note) => Show(note.Title, note.Body);

        public void Show(string title, string body)
        {
            Title.text = title;
            Body.text = body;
            if (_modal == null) _modal = ModalGate.Open();
            Panel.SetActive(true);
        }

        public void Close()
        {
            _modal?.Dispose();
            _modal = null;
            Panel.SetActive(false);
        }
    }
}
