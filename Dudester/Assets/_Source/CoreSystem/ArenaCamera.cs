using UnityEngine;

namespace CoreSystem
{
    [RequireComponent(typeof(Camera))]
    public sealed class ArenaCamera : MonoBehaviour
    {
        [SerializeField] private float halfWidth = 12.6f;
        [SerializeField] private float minimumSize = 13.2f;
        private Camera view;
        private Vector3 restPosition;
        private float shake;
        private void Awake() { view = GetComponent<Camera>(); restPosition = transform.position; }
        public void Shake() => shake = .16f;
        private void LateUpdate()
        {
            view.orthographicSize = Mathf.Max(minimumSize, halfWidth / Mathf.Max(.3f, view.aspect));
            shake = Mathf.Max(0, shake - Time.unscaledDeltaTime);
            float amount = shake * .28f;
            transform.position = restPosition + new Vector3(Mathf.Sin(Time.unscaledTime * 95), 0, Mathf.Cos(Time.unscaledTime * 83)) * amount;
        }
    }
}
