using UnityEngine;
using UnityEngine.EventSystems;

namespace NocturneAnnex.Controls
{
    public class TouchButton : MonoBehaviour, IPointerDownHandler, IPointerUpHandler
    {
        public enum Kind { Interact, Sprint, Crouch, Pause }
        [SerializeField] Kind _kind;

        public void OnPointerDown(PointerEventData e)
        {
            var t = InputRouter.Instance?.Touch;
            if (t == null) return;
            switch (_kind)
            {
                case Kind.Interact: t.PressInteract(); break;
                case Kind.Pause: t.PressPause(); break;
                case Kind.Sprint: t.SetSprint(true); break;
                case Kind.Crouch: t.SetCrouch(true); break;
            }
        }

        public void OnPointerUp(PointerEventData e) => Release();
        void OnDisable() => Release();

        void Release()
        {
            var t = InputRouter.Instance?.Touch;
            if (t == null) return;
            if (_kind == Kind.Sprint) t.SetSprint(false);
            else if (_kind == Kind.Crouch) t.SetCrouch(false);
        }
    }
}
