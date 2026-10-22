using UnityEngine;

namespace UiSystem
{
    public sealed class SafeArea : MonoBehaviour
    {
        private Rect previous;
        private Vector2Int screen;
        private void Update()
        {
            Rect safe = Screen.safeArea;
            if (safe == previous && screen.x == Screen.width && screen.y == Screen.height) return;
            previous = safe; screen = new Vector2Int(Screen.width, Screen.height);
            RectTransform rect = (RectTransform)transform;
            rect.anchorMin = new Vector2(safe.xMin / Screen.width, safe.yMin / Screen.height);
            rect.anchorMax = new Vector2(safe.xMax / Screen.width, safe.yMax / Screen.height);
            rect.offsetMin = rect.offsetMax = Vector2.zero;
        }
    }
}
