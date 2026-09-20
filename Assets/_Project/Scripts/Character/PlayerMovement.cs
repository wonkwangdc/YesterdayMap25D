using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AI;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;
using YesterdayMap.CameraSystem;
using YesterdayMap.Core;
using YesterdayMap.UI;

namespace YesterdayMap.Character
{
    [RequireComponent(typeof(Rigidbody))]
    public sealed class PlayerMovement : MonoBehaviour
    {
        [SerializeField] private UnityEngine.Camera worldCamera;
        [SerializeField] private Rigidbody body;
        [SerializeField] private PlayerSelection selection;
        [SerializeField, Min(0.5f)] private float moveSpeed = 4f;
        [SerializeField, Min(1f)] private float sprintMultiplier = 2f;
        [SerializeField, Min(0.05f)] private float arrivalDistance = 0.3f;
        [SerializeField] private bool useNavMeshPathing = true;
        [SerializeField, Min(0.05f)] private float navMeshSampleRadius = 1.5f;
        [SerializeField] private Animator animator;
        [SerializeField] private string movingParameter = "IsMoving";
        [SerializeField] private string speedParameter = "MoveSpeed";
        private Vector3 destination;
        private bool hasDestination;
        private Action arrived;
        private Vector3 keyboardInput;
        private readonly List<Vector3> pathCorners = new();
        private int pathCornerIndex;
        private bool usingPath;
        private bool cinematicInputLocked;
        private float cinematicSpeedMultiplier = 1f;
        private PauseMenuUI pauseMenu;

        public void Configure(UnityEngine.Camera camera, Rigidbody rigidbody3D, PlayerSelection playerSelection)
        { worldCamera = camera; body = rigidbody3D; selection = playerSelection; }

        private void Awake()
        {
            EnsureFootstepAudio();
            ResolveReferences();
            EnableSmoothPhysicsMotion();
        }

        private void OnEnable()
        {
            // Resolve once per activation, including scenes without a pause menu.
            pauseMenu = FindFirstObjectByType<PauseMenuUI>(
                FindObjectsInactive.Include);
        }

        private void EnableSmoothPhysicsMotion()
        {
            if (body != null && body.interpolation == RigidbodyInterpolation.None)
            {
                body.interpolation = RigidbodyInterpolation.Interpolate;
            }
        }

        private void EnsureFootstepAudio()
        {
            if (!TryGetComponent<CharacterFootstepAudio>(out _))
                gameObject.AddComponent<CharacterFootstepAudio>();
        }

        private void Update()
        {
            ResolveReferences();
            if (cinematicInputLocked)
            {
                keyboardInput = Vector3.zero;
                return;
            }

            if (IsGameplayInputBlocked())
            {
                StopPlayerInput();
                return;
            }

            bool canUseMouseMove = selection == null || selection.IsSelected;
            if (Mouse.current != null && Mouse.current.rightButton.wasPressedThisFrame && canUseMouseMove &&
                worldCamera != null && !IsFirstPersonCamera() && !PointerOverUI())
            {
                Ray ray = worldCamera.ScreenPointToRay(Mouse.current.position.ReadValue());
                if (Physics.Raycast(ray, out RaycastHit hit, 100f) && hit.normal.y > 0.45f)
                    MoveTo(hit.point, null, arrivalDistance);
            }

            if (Keyboard.current == null)
            {
                keyboardInput = Vector3.zero;
                return;
            }
            Vector2 movementInput = GameSettings.ReadMovementInput(Keyboard.current);
            keyboardInput = CameraRelativeDirection(movementInput.x, movementInput.y);
            if (keyboardInput.sqrMagnitude > 0.01f)
            {
                StopDestination();
            }
        }

        private Vector3 CameraRelativeDirection(float horizontalInput, float verticalInput)
        {
            if (worldCamera == null)
            {
                return new Vector3(horizontalInput, 0f, verticalInput);
            }

            Vector3 cameraForward = Vector3.ProjectOnPlane(worldCamera.transform.forward, Vector3.up).normalized;
            Vector3 cameraRight = Vector3.ProjectOnPlane(worldCamera.transform.right, Vector3.up).normalized;
            if (cameraForward.sqrMagnitude < 0.001f || cameraRight.sqrMagnitude < 0.001f)
            {
                return new Vector3(horizontalInput, 0f, verticalInput);
            }

            // 화면에서 보이는 위·아래·좌·우와 WASD 방향을 일치시킨다.
            return cameraRight * horizontalInput + cameraForward * verticalInput;
        }

        private void FixedUpdate()
        {
            // Cinematics may still drive MoveTo while their progress UI is open.
            if (!cinematicInputLocked && IsGameplayInputBlocked())
            {
                StopPlayerInput();
                return;
            }

            if (keyboardInput.sqrMagnitude > 0.01f)
            {
                MoveBody(keyboardInput.normalized);
                UpdateAnimator(true);
                return;
            }

            if (!hasDestination)
            {
                StopHorizontalDrift();
                UpdateAnimator(false);
                return;
            }

            Vector3 flatDelta = GetCurrentMoveTarget() - body.position;
            flatDelta.y = 0f;
            if (flatDelta.magnitude <= arrivalDistance)
            {
                if (AdvancePathCorner())
                {
                    UpdateAnimator(true);
                    return;
                }

                StopDestination();
                StopHorizontalDrift();
                Action callback = arrived;
                arrived = null;
                callback?.Invoke();
                return;
            }
            MoveBody(flatDelta.normalized);
            UpdateAnimator(true);
        }

        public void MoveTo(Vector3 point, Action onArrived, float stopDistance)
        {
            destination = new Vector3(point.x, body.position.y, point.z);
            arrivalDistance = Mathf.Max(0.05f, stopDistance);
            arrived = onArrived;
            hasDestination = true;
            BuildPathToDestination();
        }

        public void SetCinematicInputLocked(bool locked)
        {
            cinematicInputLocked = locked;
            keyboardInput = Vector3.zero;
            if (!locked) return;

            StopDestination();
            StopHorizontalDrift();
        }

        public void SetCinematicSpeedMultiplier(float multiplier)
        {
            cinematicSpeedMultiplier = Mathf.Max(0.05f, multiplier);
        }

        private void MoveBody(Vector3 direction)
        {
            if (body == null) return;

            float speed = CurrentMoveSpeed();
            body.MovePosition(body.position + direction * (speed * Time.fixedDeltaTime));
            // 1인칭에서는 카메라가 플레이어의 바라보는 방향을 관리한다.
            // 이동 코드가 회전을 덮어쓰면 옆걸음 때 화면이 갑자기 돌아가므로 회전하지 않는다.
            if (direction.sqrMagnitude > 0.01f && !IsFirstPersonCamera())
                transform.forward = Vector3.Lerp(transform.forward, direction, 0.25f);
        }

        private float CurrentMoveSpeed()
        {
            bool isSprinting = Keyboard.current != null &&
                               (Keyboard.current.leftShiftKey.isPressed || Keyboard.current.rightShiftKey.isPressed);
            float currentSprintMultiplier = isSprinting ? sprintMultiplier : 1f;
            return moveSpeed * currentSprintMultiplier *
                   cinematicSpeedMultiplier;
        }

        private void BuildPathToDestination()
        {
            pathCorners.Clear();
            pathCornerIndex = 0;
            usingPath = false;

            if (!useNavMeshPathing || body == null)
            {
                return;
            }

            // NavMesh is optional. If the scene has no baked surface yet, movement keeps the original straight-line behavior.
            if (!NavMesh.SamplePosition(body.position, out NavMeshHit startHit, navMeshSampleRadius, NavMesh.AllAreas) ||
                !NavMesh.SamplePosition(destination, out NavMeshHit endHit, navMeshSampleRadius, NavMesh.AllAreas))
            {
                return;
            }

            NavMeshPath path = new();
            if (!NavMesh.CalculatePath(startHit.position, endHit.position, NavMesh.AllAreas, path) ||
                path.status == NavMeshPathStatus.PathInvalid ||
                path.corners == null ||
                path.corners.Length < 2)
            {
                return;
            }

            foreach (Vector3 corner in path.corners)
            {
                pathCorners.Add(new Vector3(corner.x, body.position.y, corner.z));
            }

            usingPath = true;
            pathCornerIndex = pathCorners.Count > 1 ? 1 : 0;
        }

        private Vector3 GetCurrentMoveTarget()
        {
            if (!usingPath || pathCorners.Count == 0 || pathCornerIndex >= pathCorners.Count)
            {
                return destination;
            }

            return pathCorners[pathCornerIndex];
        }

        private bool AdvancePathCorner()
        {
            if (!usingPath)
            {
                return false;
            }

            pathCornerIndex++;
            return pathCornerIndex < pathCorners.Count;
        }

        private void StopDestination()
        {
            hasDestination = false;
            usingPath = false;
            pathCorners.Clear();
            pathCornerIndex = 0;
        }

        private bool IsGameplayInputBlocked()
        {
            return Time.timeScale <= 0f ||
                   (pauseMenu != null && pauseMenu.BlocksGameplayInput);
        }

        private void StopPlayerInput()
        {
            keyboardInput = Vector3.zero;
            StopDestination();
            arrived = null;
            StopHorizontalDrift();
            UpdateAnimator(false);
        }

        private void StopHorizontalDrift()
        {
            if (body == null || body.isKinematic) return;

            // 충돌 해소 과정에서 Rigidbody에 남은 X/Z 속도를 제거해 손을 뗀 뒤 밀리지 않게 한다.
            Vector3 velocity = body.linearVelocity;
            body.linearVelocity = new Vector3(0f, velocity.y, 0f);
        }

        private void OnApplicationFocus(bool hasFocus)
        {
            if (hasFocus) return;

            keyboardInput = Vector3.zero;
            StopDestination();
            StopHorizontalDrift();
        }

        private void UpdateAnimator(bool isMoving)
        {
            if (animator == null)
            {
                return;
            }

            if (!string.IsNullOrWhiteSpace(movingParameter))
            {
                animator.SetBool(movingParameter, isMoving);
            }

            if (!string.IsNullOrWhiteSpace(speedParameter))
            {
                float speed = isMoving ? CurrentMoveSpeed() : 0f;
                animator.SetFloat(speedParameter, speed);
            }
        }

        private static bool PointerOverUI() => EventSystem.current != null && EventSystem.current.IsPointerOverGameObject();

        private bool IsFirstPersonCamera()
        {
            return worldCamera != null &&
                   worldCamera.TryGetComponent(out IsometricCameraController controller) &&
                   controller.IsFirstPerson;
        }

        private void ResolveReferences()
        {
            if (worldCamera == null) worldCamera = UnityEngine.Camera.main;
            if (body == null) body = GetComponent<Rigidbody>();
            if (selection == null) selection = GetComponent<PlayerSelection>();
            if (animator == null) animator = GetComponentInChildren<Animator>();
        }
    }
}
