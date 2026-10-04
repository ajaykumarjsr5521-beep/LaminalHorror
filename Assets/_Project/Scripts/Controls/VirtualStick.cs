using UnityEngine;
using UnityEngine.EventSystems;

namespace NocturneAnnex.Controls
{
    /// <summary>Floating-origin on-screen stick. Per-pointer events give correct multi-touch.</summary>
    public class VirtualStick : MonoBehaviour, IPointerDownHandler, IDragHandler, IPointerUpHandler
    {
        [SerializeField] RectTransform _knob;
        [SerializeField] float _radiusPixels = 140f;
        int _pointerId = int.MinValue;
        Vector2 _origin;

        float Radius => _radiusPixels * ControlsLayout.Scale;

        public void OnPointerDown(PointerEventData e)
        {
            if (_pointerId != int.MinValue) return;
            _pointerId = e.pointerId;
            _origin = e.position;
            Report(e.position);
        }

        public void OnDrag(PointerEventData e)
        {
            if (e.pointerId == _pointerId) Report(e.position);
        }

        public void OnPointerUp(PointerEventData e)
        {
            if (e.pointerId == _pointerId) Release();
        }

        void OnDisable() => Release();

        void Release()
        {
            _pointerId = int.MinValue;
            if (_knob) _knob.anchoredPosition = Vector2.zero;
            InputRouter.Instance?.Touch.SetMove(Vector2.zero);
        }

        void Report(Vector2 pos)
        {
            var v = InputMath.StickFromOffset(pos - _origin, Radius);
            if (_knob) _knob.anchoredPosition = v * Radius;
            InputRouter.Instance?.Touch.SetMove(v);
        }
    }
}
