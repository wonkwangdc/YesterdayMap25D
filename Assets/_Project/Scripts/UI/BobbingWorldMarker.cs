using UnityEngine;

namespace YesterdayMap.UI
{
    // 월드 오브젝트 위에서 위아래로 떠다니며 항상 카메라를 바라보는 안내 표시다.
    public sealed class BobbingWorldMarker : MonoBehaviour
    {
        [SerializeField] private Transform target;
        [SerializeField] private Vector3 worldOffset = new(0f, 2f, 0f);
        [SerializeField, Min(0f)] private float bobHeight = 0.18f;
        [SerializeField, Min(0f)] private float bobSpeed = 2.4f;
        [SerializeField] private bool faceCamera = true;

        private Camera targetCamera;

        public void Configure(
            Transform markerTarget,
            Vector3 offset,
            float height = 0.18f,
            float speed = 2.4f)
        {
            target = markerTarget;
            worldOffset = offset;
            bobHeight = Mathf.Max(0f, height);
            bobSpeed = Mathf.Max(0f, speed);
        }

        private void LateUpdate()
        {
            if (target == null)
            {
                return;
            }

            float bobOffset = Mathf.Sin(Time.unscaledTime * bobSpeed) * bobHeight;
            transform.position = target.position + worldOffset + Vector3.up * bobOffset;

            if (!faceCamera)
            {
                return;
            }

            if (targetCamera == null)
            {
                targetCamera = Camera.main != null
                    ? Camera.main
                    : FindFirstObjectByType<Camera>();
            }

            if (targetCamera != null)
            {
                transform.rotation = targetCamera.transform.rotation;
            }
        }
    }
}
