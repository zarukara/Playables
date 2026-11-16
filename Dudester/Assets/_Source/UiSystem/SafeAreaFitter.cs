using UnityEngine;
using Screen = UnityEngine.Device.Screen;

namespace UiSystem
{
    /// <summary>Fits a direct, full-screen Canvas child to the device's safe rectangle.</summary>
    [DisallowMultipleComponent]
    [RequireComponent(typeof(RectTransform))]
    public sealed class SafeAreaFitter : MonoBehaviour
    {
        private RectTransform panel;
        private Rect lastSafeArea;
        private Vector2Int lastScreenSize;
        private bool hasApplied;

        private void Awake() => panel = GetComponent<RectTransform>();

        private void OnEnable()
        {
            hasApplied = false;
            Refresh();
        }

        private void Update() => Refresh();

        private void Refresh()
        {
            int width = Screen.width;
            int height = Screen.height;
            Apply(Screen.safeArea, width, height);
        }

        private void Apply(Rect safeArea, int width, int height)
        {
            // Resolution can be temporarily unavailable during an orientation change.
            // Do not cache this frame, so the next valid frame is still applied.
            if (width <= 0 || height <= 0) return;

            if (hasApplied && width == lastScreenSize.x && height == lastScreenSize.y && safeArea == lastSafeArea)
                return;

            if (panel == null) panel = GetComponent<RectTransform>();
            panel.anchorMin = new Vector2(safeArea.xMin / width, safeArea.yMin / height);
            panel.anchorMax = new Vector2(safeArea.xMax / width, safeArea.yMax / height);
            panel.offsetMin = Vector2.zero;
            panel.offsetMax = Vector2.zero;

            lastSafeArea = safeArea;
            lastScreenSize = new Vector2Int(width, height);
            hasApplied = true;
            Debug.Log($"[SafeAreaFitter] Screen.safeArea={safeArea}; resolution={width}x{height}; " +
                      $"anchorMin={panel.anchorMin}, anchorMax={panel.anchorMax}", this);
        }
    }
}
