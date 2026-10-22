using UnityEngine;
using UnityEngine.EventSystems;

namespace InputSystem
{
    public sealed class VirtualJoystick : MonoBehaviour, IPointerDownHandler, IDragHandler, IPointerUpHandler
    {
        [SerializeField] private RectTransform handle;
        private int pointerId = int.MinValue;
        public Vector2 Value { get; private set; }
        public void Configure(RectTransform value) => handle = value;
        public void OnPointerDown(PointerEventData data)
        {
            if (pointerId != int.MinValue) return;
            pointerId = data.pointerId;
            OnDrag(data);
        }
        public void OnDrag(PointerEventData data)
        {
            if (pointerId != data.pointerId) return;
            RectTransform rect = (RectTransform)transform;
            RectTransformUtility.ScreenPointToLocalPointInRectangle(rect, data.position, data.pressEventCamera, out Vector2 local);
            float radius = rect.rect.width * .32f;
            Value = Vector2.ClampMagnitude(local / radius, 1);
            handle.anchoredPosition = Value * radius;
        }
        public void OnPointerUp(PointerEventData data) { if (pointerId == data.pointerId) ResetInput(); }
        private void OnDisable() => ResetInput();
        private void OnApplicationFocus(bool focus) { if (!focus) ResetInput(); }
        public void ResetInput()
        {
            pointerId = int.MinValue;
            Value = Vector2.zero;
            if (handle != null) handle.anchoredPosition = Vector2.zero;
        }
    }
}
