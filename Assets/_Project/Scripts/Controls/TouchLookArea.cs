using UnityEngine;
using UnityEngine.EventSystems;

namespace NocturneAnnex.Controls
{
    /// <summary>Drag anywhere in this area to look. Tracks a single pointer.</summary>
    public class TouchLookArea : MonoBehaviour, IPointerDownHandler, IDragHandler, IPointerUpHandler
    {
        [SerializeField] float _degreesPerInch = 90f;
        int _pointerId = int.MinValue;

        public void OnPointerDown(PointerEventData e) { if (_pointerId == int.MinValue) _pointerId = e.pointerId; }
        public void OnPointerUp(PointerEventData e) { if (e.pointerId == _pointerId) _pointerId = int.MinValue; }
        void OnDisable() => _pointerId = int.MinValue;

        public void OnDrag(PointerEventData e)
        {
            if (e.pointerId != _pointerId) return;
            float dpi = Screen.dpi > 0 ? Screen.dpi : 160f;
            InputRouter.Instance?.Touch.AddLook(e.delta / dpi * _degreesPerInch);
        }
    }
}
