using UnityEngine;
using UnityEngine.Events;

namespace NocturneAnnex.Interaction
{
    /// <summary>Hinged door. A non-empty RequiredKeyId makes it locked until the interactor holds that key.</summary>
    public class Door : MonoBehaviour, IInteractable
    {
        public Transform Hinge;
        public float OpenAngle = 100f;
        public float AngularSpeed = 180f;
        public string RequiredKeyId = "";

        /// <summary>Generic, spoiler-free feedback shown when a locked door is tried.</summary>
        public const string LockedMessage = "It's locked.";

        public UnityEvent<string> OnMessage = new UnityEvent<string>();
        public UnityEvent OnOpened = new UnityEvent();

        public bool IsOpen { get; private set; }
        public bool IsLocked { get; private set; }

        float _angle;

        void Awake()
        {
            if (Hinge == null) Hinge = transform;
            IsLocked = !string.IsNullOrEmpty(RequiredKeyId);
        }

        public string Prompt => IsLocked ? "Locked" : (IsOpen ? "Close" : "Open");

        public void Interact(Interactor interactor)
        {
            if (IsLocked)
            {
                bool hasKey = interactor != null && interactor.Keys != null && interactor.Keys.HasKey(RequiredKeyId);
                if (!hasKey) { OnMessage.Invoke(LockedMessage); return; }
                interactor.Keys.ConsumeKey(RequiredKeyId);
                IsLocked = false;
            }
            IsOpen = !IsOpen;
            if (IsOpen) OnOpened.Invoke();
        }

        void Update()
        {
            float target = IsOpen ? OpenAngle : 0f;
            if (Mathf.Approximately(_angle, target)) return;
            _angle = Mathf.MoveTowards(_angle, target, AngularSpeed * Time.deltaTime);
            Hinge.localRotation = Quaternion.Euler(0f, _angle, 0f);
        }
    }
}
