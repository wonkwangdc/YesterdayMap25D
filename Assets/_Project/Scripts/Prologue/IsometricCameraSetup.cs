using UnityEngine;

namespace YesterdayMap.Prologue
{
    [RequireComponent(typeof(Camera))]
    // One-shot helper for a fixed orthographic 2.5D camera angle, with optional target following.
    public sealed class IsometricCameraSetup : MonoBehaviour
    {
        [SerializeField] private Transform target;
        [SerializeField] private Vector3 offset = new(7.5f, 11f, -7.5f);
        [SerializeField, Min(2f)] private float orthographicSize = 8.5f;
        [SerializeField] private bool followTarget = true;

        private Camera cachedCamera;

        private void Awake()
        {
            cachedCamera = GetComponent<Camera>();
            ApplyCameraSettings();
        }

        private void LateUpdate()
        {
            if (followTarget && target != null)
            {
                transform.position = target.position + offset;
            }
        }

        public void Configure(Transform followTarget)
        {
            target = followTarget;
            ApplyCameraSettings();
        }

        public void ApplyCameraSettings()
        {
            if (cachedCamera == null)
            {
                cachedCamera = GetComponent<Camera>();
            }

            cachedCamera.orthographic = true;
            cachedCamera.orthographicSize = orthographicSize;
            transform.rotation = Quaternion.Euler(45f, 45f, 0f);

            if (target != null)
            {
                transform.position = target.position + offset;
            }
        }
    }
}
