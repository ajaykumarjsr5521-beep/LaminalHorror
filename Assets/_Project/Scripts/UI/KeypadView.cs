using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using NocturneAnnex.Core;
using NocturneAnnex.Puzzle;

namespace NocturneAnnex.UI
{
    /// <summary>
    /// Digit pad for a CodeLock. Wrong codes show a neutral message and clear at once (no lockout);
    /// a correct code shows a short confirmation then closes. Holds the ModalGate while open.
    /// </summary>
    public class KeypadView : MonoBehaviour
    {
        public const float SolvedCloseDelaySeconds = 0.8f;

        public TMP_Text Display;
        public TMP_Text Message;
        public Button[] DigitButtons = new Button[10];   // index is the digit
        public Button BackspaceButton;
        public Button EnterButton;
        public Button CloseButton;

        CodeLock _lock;
        ModalGate.Handle _modal;
        bool _closing;

        public bool IsOpen => gameObject.activeSelf && _lock != null;

        void Awake()
        {
            for (int i = 0; i < DigitButtons.Length; i++)
            {
                char digit = (char)('0' + i);
                DigitButtons[i].onClick.AddListener(() => _lock?.Press(digit));
            }
            BackspaceButton.onClick.AddListener(() => _lock?.Backspace());
            EnterButton.onClick.AddListener(() => _lock?.Submit());
            CloseButton.onClick.AddListener(Close);
        }

        /// <summary>Opens this keypad whenever the lock asks for it (player interacts with it).</summary>
        public void Watch(CodeLock codeLock) => codeLock.KeypadRequested += Open;

        public void Open(CodeLock codeLock)
        {
            if (IsOpen) return;
            _lock = codeLock;
            _closing = false;
            _lock.EntryChanged += OnEntryChanged;
            _lock.Rejected += OnRejected;
            _lock.Solved += OnSolved;
            _modal = ModalGate.Open();
            gameObject.SetActive(true);
            SetMessage(null);
            Render();
        }

        public void Close() => gameObject.SetActive(false);

        void OnEntryChanged(string entry)
        {
            SetMessage(null);
            Render();
        }

        void OnRejected()
        {
            SetMessage(Loc.Get("keypad.incorrect"));
            Render();
        }

        void OnSolved()
        {
            if (_closing) return;
            _closing = true;
            SetMessage(Loc.Get("keypad.solved"));
            SetInteractable(false);
            StartCoroutine(CloseAfterDelay());
        }

        IEnumerator CloseAfterDelay()
        {
            yield return new WaitForSecondsRealtime(SolvedCloseDelaySeconds);
            SetInteractable(true);
            Close();   // OnDisable releases the gate and subscriptions
        }

        void Render() => Display.text = KeypadFormatter.Format(_lock.Entry, _lock.CodeLength);

        void SetInteractable(bool on)
        {
            foreach (var b in DigitButtons) b.interactable = on;
            BackspaceButton.interactable = on;
            EnterButton.interactable = on;
        }

        void SetMessage(string text)
        {
            Message.text = text ?? string.Empty;
            Message.gameObject.SetActive(!string.IsNullOrEmpty(text));
        }

        // Any way of hiding the keypad (Close, scene change, destroy) ends here, so the gate and the
        // lock subscriptions are always released.
        void OnDisable()
        {
            if (_lock != null)
            {
                _lock.EntryChanged -= OnEntryChanged;
                _lock.Rejected -= OnRejected;
                _lock.Solved -= OnSolved;
                _lock = null;
            }
            _modal?.Dispose();
            _modal = null;
        }
    }
}
