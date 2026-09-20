using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;
using YesterdayMap.Core;

namespace YesterdayMap.CameraSystem
{
    public sealed class IsometricCameraController : MonoBehaviour
    {
        [SerializeField] private Transform target;
        [SerializeField] private Vector3 direction = new(0f, 0.793f, -0.609f);
        [SerializeField] private Vector3 cameraAngle = new(55f, 0f, 0f);
        [SerializeField] private bool followBehindTarget;
        [SerializeField, Min(0f)] private float targetHeight = 1.6f;
        [SerializeField, Min(2f)] private float distance = 16.4f;
        [SerializeField, Min(2f)] private float minimumDistance = 9f;
        [SerializeField, Min(3f)] private float maximumDistance = 55f;
        [SerializeField, Min(0.1f)] private float zoomSpeed = 1.2f;
        [SerializeField, Min(0.1f)] private float followSmoothness = 5f;
        [SerializeField, Min(0.1f)] private float rotationSmoothness = 5f;
        [SerializeField, Range(10f, 180f)] private float maximumOrbitDegreesPerSecond = 35f;
        [SerializeField] private LayerMask obstructionMask = ~0;
        [SerializeField, Range(0.1f, 1f)] private float obstructedAlpha = 0.35f;
        [SerializeField, Min(0.01f)] private float obstructionFadeSpeed = 8f;

        [Header("First Person Mode")]
        [SerializeField] private bool firstPersonMode;
        [SerializeField, Min(0.5f)] private float firstPersonEyeHeight = 1.65f;
        [SerializeField, Range(0.01f, 1f)] private float mouseLookSensitivity = 0.12f;
        [SerializeField, Range(-89f, 0f)] private float minimumPitch = -75f;
        [SerializeField, Range(0f, 89f)] private float maximumPitch = 75f;
        [SerializeField] private bool hideTargetRenderersInFirstPerson = true;

        private readonly List<Renderer> fadedRenderers = new();
        private readonly Dictionary<Renderer, Color> originalColors = new();
        private readonly Dictionary<Renderer, bool> firstPersonRendererStates = new();
        private UnityEngine.Camera controlledCamera;
        private Vector3 smoothedBehindDirection;
        private float firstPersonYaw;
        private float firstPersonPitch;
        private bool firstPersonInitialized;
        private bool firstPersonInputSuspended;

        public bool IsFirstPerson => firstPersonMode;

        // 탐사 지도나 이벤트 선택지처럼 마우스 UI가 열리면 1인칭 입력을 잠시 멈춘다.
        public static void SetFirstPersonUiFocus(bool uiHasFocus)
        {
            UnityEngine.Camera mainCamera = UnityEngine.Camera.main;
            if (mainCamera != null &&
                mainCamera.TryGetComponent(out IsometricCameraController controller) &&
                controller.firstPersonMode)
            {
                controller.SetFirstPersonInputSuspended(uiHasFocus);
                return;
            }

            // Exploration 씬을 단독 실행해도 마우스 UI는 사용할 수 있어야 한다.
            if (uiHasFocus)
            {
                Cursor.lockState = CursorLockMode.None;
                Cursor.visible = true;
            }
        }

        public void Configure(Transform followTarget, float startingDistance = -1f)
        {
            target = followTarget;

            // A scene can start slightly farther away while keeping the same mouse-wheel zoom rules.
            if (startingDistance > 0f)
            {
                distance = Mathf.Clamp(startingDistance, minimumDistance, maximumDistance);
            }
        }

        public void ConfigureFirstPerson(Transform followTarget)
        {
            target = followTarget;
            firstPersonMode = true;
        }

        private void Awake()
        {
            controlledCamera = GetComponent<UnityEngine.Camera>();
        }

        private void OnEnable()
        {
            // Cinematics temporarily disable this controller. Reinitialize first person
            // when it comes back so the camera pose and hidden local body are reapplied.
            if (firstPersonMode)
            {
                firstPersonInitialized = false;
            }
        }

        private void OnDisable()
        {
            RestoreFirstPersonRenderers();
            firstPersonInitialized = false;
            if (firstPersonMode && Application.isPlaying)
            {
                Cursor.lockState = CursorLockMode.None;
                Cursor.visible = true;
            }
        }

        private void LateUpdate()
        {
            if (controlledCamera == null)
            {
                controlledCamera = GetComponent<UnityEngine.Camera>();
            }

            if (target == null)
            {
                RestoreFadedRenderers();
                return;
            }

            if (firstPersonMode)
            {
                UpdateFirstPersonCamera();
                return;
            }

            if (Mouse.current != null)
            {
                float scroll = Mouse.current.scroll.ReadValue().y;
                if (Mathf.Abs(scroll) > 0.01f)
                {
                    ApplyZoom(-Mathf.Sign(scroll) * zoomSpeed);
                }
            }

            if (Keyboard.current != null)
            {
                if (Keyboard.current.equalsKey.wasPressedThisFrame || Keyboard.current.numpadPlusKey.wasPressedThisFrame)
                {
                    ApplyZoom(-zoomSpeed);
                }
                else if (Keyboard.current.minusKey.wasPressedThisFrame || Keyboard.current.numpadMinusKey.wasPressedThisFrame)
                {
                    ApplyZoom(zoomSpeed);
                }
            }

            Vector3 desiredDirection = direction.normalized;
            if (followBehindTarget)
            {
                Vector3 flatForward = Vector3.ProjectOnPlane(target.forward, Vector3.up);
                if (flatForward.sqrMagnitude < 0.001f)
                {
                    flatForward = Vector3.forward;
                }

                float pitchInRadians = cameraAngle.x * Mathf.Deg2Rad;
                Vector3 desiredBehindDirection = -flatForward.normalized;
                if (smoothedBehindDirection.sqrMagnitude < 0.001f)
                {
                    Vector3 currentFlatOffset = Vector3.ProjectOnPlane(transform.position - target.position, Vector3.up);
                    smoothedBehindDirection = currentFlatOffset.sqrMagnitude > 0.001f
                        ? currentFlatOffset.normalized
                        : desiredBehindDirection;
                }

                // 캐릭터가 급하게 방향을 바꿔도 카메라는 제한된 각속도로 천천히 뒤를 따라간다.
                float maximumRadiansThisFrame = maximumOrbitDegreesPerSecond * Mathf.Deg2Rad * Time.deltaTime;
                smoothedBehindDirection = Vector3.RotateTowards(
                    smoothedBehindDirection,
                    desiredBehindDirection,
                    maximumRadiansThisFrame,
                    0f).normalized;
                desiredDirection = smoothedBehindDirection * Mathf.Cos(pitchInRadians) + Vector3.up * Mathf.Sin(pitchInRadians);
            }

            Vector3 desired = target.position + desiredDirection * distance;
            transform.position = Vector3.Lerp(transform.position, desired, 1f - Mathf.Exp(-followSmoothness * Time.deltaTime));
            if (followBehindTarget)
            {
                Vector3 lookTarget = target.position + Vector3.up * targetHeight;
                Quaternion desiredRotation = Quaternion.LookRotation(lookTarget - transform.position, Vector3.up);
                transform.rotation = Quaternion.Slerp(transform.rotation, desiredRotation, 1f - Mathf.Exp(-rotationSmoothness * Time.deltaTime));
            }
            else
            {
                transform.rotation = Quaternion.Euler(cameraAngle);
            }
            UpdateObstructionFade();
        }

        private void UpdateFirstPersonCamera()
        {
            if (!firstPersonInitialized)
            {
                firstPersonYaw = target.eulerAngles.y;
                firstPersonPitch = 0f;
                firstPersonInitialized = true;

                if (controlledCamera != null)
                {
                    controlledCamera.nearClipPlane = 0.03f;
                }

                HideFirstPersonRenderers();
                if (!firstPersonInputSuspended) LockCursor();
            }

            if (firstPersonInputSuspended)
            {
                ReleaseCursor();
                FollowFirstPersonTarget();
                return;
            }

            bool altPressed = Keyboard.current != null &&
                              (Keyboard.current.leftAltKey.wasPressedThisFrame ||
                               Keyboard.current.rightAltKey.wasPressedThisFrame);
            if (altPressed)
            {
                ReleaseCursor();
            }

            if (Mouse.current != null && Cursor.lockState != CursorLockMode.Locked &&
                Mouse.current.leftButton.wasPressedThisFrame && !PointerOverUI())
            {
                LockCursor();
                return;
            }

            if (Mouse.current != null && Cursor.lockState == CursorLockMode.Locked)
            {
                Vector2 lookDelta = Mouse.current.delta.ReadValue();
                float sensitivity = GameSettings.MouseSensitivity;
                firstPersonYaw += lookDelta.x * sensitivity;
                firstPersonPitch = Mathf.Clamp(
                    firstPersonPitch - lookDelta.y * sensitivity,
                    minimumPitch,
                    maximumPitch);
            }

            // 플레이어의 Y축 회전과 카메라 방향을 맞춰 WASD가 화면 기준으로 자연스럽게 움직인다.
            FollowFirstPersonTarget();
        }

        private void SetFirstPersonInputSuspended(bool suspended)
        {
            firstPersonInputSuspended = suspended;
            if (suspended) ReleaseCursor();
            else if (firstPersonMode) LockCursor();
        }

        private void FollowFirstPersonTarget()
        {
            if (target == null) return;

            target.rotation = Quaternion.Euler(0f, firstPersonYaw, 0f);
            transform.SetPositionAndRotation(
                target.position + Vector3.up * firstPersonEyeHeight,
                Quaternion.Euler(firstPersonPitch, firstPersonYaw, 0f));
        }

        private void LockCursor()
        {
            if (!Application.isPlaying)
            {
                return;
            }

            Cursor.lockState = CursorLockMode.Locked;
            Cursor.visible = false;
        }

        private static void ReleaseCursor()
        {
            Cursor.lockState = CursorLockMode.None;
            Cursor.visible = true;
        }

        private void HideFirstPersonRenderers()
        {
            if (!hideTargetRenderersInFirstPerson || target == null || firstPersonRendererStates.Count > 0)
            {
                return;
            }

            foreach (Renderer renderer in target.GetComponentsInChildren<Renderer>(true))
            {
                if (renderer == null) continue;
                firstPersonRendererStates[renderer] = renderer.enabled;
                renderer.enabled = false;
            }
        }

        private void RestoreFirstPersonRenderers()
        {
            foreach (KeyValuePair<Renderer, bool> state in firstPersonRendererStates)
            {
                if (state.Key != null) state.Key.enabled = state.Value;
            }

            firstPersonRendererStates.Clear();
        }

        private static bool PointerOverUI()
        {
            return EventSystem.current != null && EventSystem.current.IsPointerOverGameObject();
        }

        private void ApplyZoom(float amount)
        {
            distance = Mathf.Clamp(distance + amount, minimumDistance, maximumDistance);
        }

        private void UpdateObstructionFade()
        {
            RestoreFadedRenderers();

            // Fade walls or large props between the camera and player so the fixed 2.5D view stays readable.
            Vector3 origin = target.position + Vector3.up * targetHeight;
            Vector3 toCamera = transform.position - origin;
            RaycastHit[] hits = Physics.RaycastAll(origin, toCamera.normalized, toCamera.magnitude, obstructionMask, QueryTriggerInteraction.Ignore);
            foreach (RaycastHit hit in hits)
            {
                Renderer renderer = hit.collider.GetComponentInParent<Renderer>();
                if (renderer == null || renderer.transform.IsChildOf(target))
                {
                    continue;
                }

                FadeRenderer(renderer);
            }
        }

        private void FadeRenderer(Renderer renderer)
        {
            if (!originalColors.ContainsKey(renderer))
            {
                originalColors[renderer] = renderer.material.color;
            }

            ConfigureTransparentMaterial(renderer.material);
            Color color = renderer.material.color;
            color.a = Mathf.Lerp(color.a, obstructedAlpha, 1f - Mathf.Exp(-obstructionFadeSpeed * Time.deltaTime));
            renderer.material.color = color;
            fadedRenderers.Add(renderer);
        }

        private void RestoreFadedRenderers()
        {
            for (int i = fadedRenderers.Count - 1; i >= 0; i--)
            {
                Renderer renderer = fadedRenderers[i];
                if (renderer == null || !originalColors.TryGetValue(renderer, out Color original))
                {
                    continue;
                }

                Color color = renderer.material.color;
                color.a = Mathf.Lerp(color.a, original.a, 1f - Mathf.Exp(-obstructionFadeSpeed * Time.deltaTime));
                renderer.material.color = color;
            }

            fadedRenderers.Clear();
        }

        private static void ConfigureTransparentMaterial(Material material)
        {
            if (material == null)
            {
                return;
            }

            // URP Lit ignores alpha while the surface is opaque, so switch only the runtime material instance.
            if (material.HasProperty("_Surface")) material.SetFloat("_Surface", 1f);
            if (material.HasProperty("_Blend")) material.SetFloat("_Blend", 0f);
            if (material.HasProperty("_SrcBlend")) material.SetFloat("_SrcBlend", (float)UnityEngine.Rendering.BlendMode.SrcAlpha);
            if (material.HasProperty("_DstBlend")) material.SetFloat("_DstBlend", (float)UnityEngine.Rendering.BlendMode.OneMinusSrcAlpha);
            if (material.HasProperty("_ZWrite")) material.SetFloat("_ZWrite", 0f);
            material.EnableKeyword("_SURFACE_TYPE_TRANSPARENT");
            material.renderQueue = (int)UnityEngine.Rendering.RenderQueue.Transparent;
        }
    }
}
