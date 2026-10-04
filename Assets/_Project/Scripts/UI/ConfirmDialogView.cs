using System;
using UnityEngine;
using UnityEngine.UI;

namespace NocturneAnnex.UI
{
    /// <summary>Modal yes/no panel. Texts are LocalizedText components set up by the scene builder.</summary>
    public class ConfirmDialogView : MonoBehaviour
    {
        public Button Yes;
        public Button No;

        Action _onYes;
        Action _onNo;

        void Awake()
        {
            Yes.onClick.AddListener(() => Close(_onYes));
            No.onClick.AddListener(() => Close(_onNo));
        }

        public void Show(Action onYes, Action onNo = null)
        {
            _onYes = onYes;
            _onNo = onNo;
            gameObject.SetActive(true);
        }

        void Close(Action callback)
        {
            gameObject.SetActive(false);
            callback?.Invoke();
        }
    }
}
