using System;
using System.Collections;
using UnityEngine;
using YesterdayMap.CameraSystem;
using YesterdayMap.Character;

namespace YesterdayMap.Shelter
{
    [DisallowMultipleComponent]
    public sealed class SleepCinematicController : MonoBehaviour
    {
        [Header("Player")]
        [SerializeField] private PlayerMovement playerMovement;
        [SerializeField] private PlayerInteraction playerInteraction;
        [SerializeField] private PlayerSelection playerSelection;
        [SerializeField] private Rigidbody playerBody;
        [SerializeField] private Animator playerAnimator;
        [SerializeField] private Transform playerVisual;
        [SerializeField] private Transform sleepPosePivot;
        [SerializeField] private Animation sleepAnimation;
        [SerializeField] private string lieDownClipName = "SleepLieDown";

        [Header("Player Visibility")]
        [SerializeField] private bool hidePlayerDuringSleep = true;

        [Header("Scene Markers")]
        [SerializeField] private Transform approachPoint;
        [SerializeField] private Transform sleepPosePoint;
        [SerializeField] private Transform sleepCameraPoint;

        [Header("Approach")]
        [SerializeField, Min(0.1f)] private float approachStopDistance = 0.35f;
        [SerializeField, Min(0.1f)] private float approachBlendDuration = 0.55f;
        [SerializeField, Min(0f)] private float approachProgressThreshold = 0.02f;

        [Header("Camera And Fade")]
        [SerializeField] private Camera targetCamera;
        [SerializeField] private IsometricCameraController cameraController;
        [SerializeField] private CanvasGroup fadeCanvas;
        [SerializeField] private GameObject sleepCeiling;

        [Header("Timing")]
        [SerializeField, Min(0.2f)] private float lieDownDuration = 1.6f;
        [SerializeField, Min(0f)] private float cameraBlendDelay = 0.72f;
        [SerializeField, Min(0.1f)] private float cameraBlendDuration = 0.85f;
        [SerializeField, Min(0f)] private float settleDuration = 0.25f;
        [SerializeField, Min(0.1f)] private float fadeDuration = 0.55f;
        [SerializeField] private AnimationCurve movementCurve =
            AnimationCurve.EaseInOut(0f, 0f, 1f, 1f);

        private Coroutine approachRoutine;
        private Coroutine sequenceRoutine;
        private Action readyForLoading;
        private bool playing;
        private bool poseStarted;
        private bool interactionWasEnabled;
        private bool selectionWasVisible;
        private bool animatorWasEnabled;
        private float animatorSpeed;
        private bool bodyWasKinematic;
        private bool bodyUsedGravity;
        private bool cameraControllerWasEnabled;
        private bool sleepCeilingWasActive;
        private Renderer[] playerRenderers;
        private bool[] rendererForceOffStates;
        private bool playerVisualHidden;
        private Vector3 wakePosition;
        private Quaternion wakeRotation;
        private Vector3 pivotLocalPosition;
        private Quaternion pivotLocalRotation;
        private Vector3 visualLocalPosition;
        private Quaternion visualLocalRotation;
        private Vector3 cameraPosition;
        private Quaternion cameraRotation;

        public bool IsPlaying => playing;

        public bool Play(Action onReadyForLoading)
        {
            if (playing) return false;

            ResolveReferences();
            if (!HasRequiredReferences())
            {
                Debug.LogWarning(
                    "Sleep cinematic references are incomplete. Falling back to the loading transition.",
                    this);
                return false;
            }

            playing = true;
            poseStarted = false;
            readyForLoading = onReadyForLoading;
            interactionWasEnabled = playerInteraction.enabled;
            playerInteraction.enabled = false;
            selectionWasVisible = playerSelection.IsSelected;
            playerSelection.SetSelectionVisible(false);
            playerMovement.SetCinematicInputLocked(true);

            wakePosition = playerMovement.transform.position;
            wakeRotation = playerMovement.transform.rotation;
            bodyWasKinematic = playerBody.isKinematic;
            bodyUsedGravity = playerBody.useGravity;
            playerBody.linearVelocity = Vector3.zero;
            playerBody.angularVelocity = Vector3.zero;
            playerBody.isKinematic = true;
            playerBody.useGravity = false;

            approachRoutine = StartCoroutine(MonitorApproachRoutine());
            return true;
        }

        public void RestoreAfterTransition()
        {
            if (!playing) return;

            if (approachRoutine != null)
            {
                StopCoroutine(approachRoutine);
                approachRoutine = null;
            }

            if (sequenceRoutine != null)
            {
                StopCoroutine(sequenceRoutine);
                sequenceRoutine = null;
            }

            if (sleepAnimation != null)
            {
                sleepAnimation.Stop();
            }

            if (sleepPosePivot != null)
            {
                sleepPosePivot.localPosition = pivotLocalPosition;
                sleepPosePivot.localRotation = pivotLocalRotation;
            }

            if (playerVisual != null)
            {
                playerVisual.localPosition = visualLocalPosition;
                playerVisual.localRotation = visualLocalRotation;
            }

            RestorePlayerRendererStates();

            if (playerAnimator != null)
            {
                playerAnimator.enabled = animatorWasEnabled;
                playerAnimator.speed = animatorSpeed;
            }

            if (playerBody != null)
            {
                playerBody.isKinematic = bodyWasKinematic;
                playerBody.useGravity = bodyUsedGravity;
                playerBody.position = wakePosition;
                playerBody.rotation = wakeRotation;
            }

            if (playerMovement != null)
            {
                playerMovement.transform.SetPositionAndRotation(
                    wakePosition,
                    wakeRotation);
                playerMovement.SetCinematicInputLocked(false);
            }

            if (targetCamera != null)
            {
                targetCamera.transform.SetPositionAndRotation(
                    cameraPosition,
                    cameraRotation);
            }

            if (cameraController != null)
            {
                cameraController.enabled = cameraControllerWasEnabled;
            }

            if (sleepCeiling != null)
            {
                sleepCeiling.SetActive(sleepCeilingWasActive);
            }

            if (playerInteraction != null)
            {
                playerInteraction.enabled = interactionWasEnabled;
            }

            if (playerSelection != null)
            {
                playerSelection.SetSelectionVisible(selectionWasVisible);
            }

            HideFade();
            readyForLoading = null;
            playing = false;
            poseStarted = false;
        }

        private IEnumerator MonitorApproachRoutine()
        {
            Vector3 startPosition = playerMovement.transform.position;
            Vector3 targetPosition = approachPoint.position;
            targetPosition.y = startPosition.y;

            Vector3 awayFromBed = startPosition - targetPosition;
            awayFromBed.y = 0f;
            float flatDistance = awayFromBed.magnitude;
            if (flatDistance > approachStopDistance)
            {
                targetPosition +=
                    awayFromBed.normalized * approachStopDistance;
            }
            else
            {
                targetPosition = startPosition;
            }

            Quaternion startRotation = playerMovement.transform.rotation;
            Quaternion targetRotation = Quaternion.Euler(
                0f,
                approachPoint.eulerAngles.y,
                0f);
            float distance = Vector3.Distance(startPosition, targetPosition);
            float duration = distance <= approachProgressThreshold
                ? 0f
                : Mathf.Clamp(
                    distance / 4f,
                    0.15f,
                    Mathf.Max(0.15f, approachBlendDuration));

            if (playerAnimator != null)
            {
                playerAnimator.SetBool("IsMoving", duration > 0f);
                playerAnimator.SetFloat("MoveSpeed", duration > 0f ? 1f : 0f);
            }

            float elapsed = 0f;
            while (playing && !poseStarted && elapsed < duration)
            {
                elapsed += Time.unscaledDeltaTime;
                float normalized = Mathf.Clamp01(elapsed / duration);
                float eased = movementCurve.Evaluate(normalized);
                Vector3 position =
                    Vector3.Lerp(startPosition, targetPosition, eased);
                Quaternion rotation =
                    Quaternion.Slerp(startRotation, targetRotation, eased);

                playerMovement.transform.SetPositionAndRotation(
                    position,
                    rotation);
                playerBody.position = position;
                playerBody.rotation = rotation;
                yield return null;
            }

            if (playing && !poseStarted)
            {
                playerMovement.transform.SetPositionAndRotation(
                    targetPosition,
                    targetRotation);
                playerBody.position = targetPosition;
                playerBody.rotation = targetRotation;
                BeginLieDown();
            }

            approachRoutine = null;
        }

        private void BeginLieDown()
        {
            if (!playing || poseStarted) return;
            poseStarted = true;

            playerMovement.SetCinematicInputLocked(true);
            Quaternion bedFacingRotation = Quaternion.Euler(
                0f,
                approachPoint.eulerAngles.y,
                0f);
            playerMovement.transform.SetPositionAndRotation(
                playerMovement.transform.position,
                bedFacingRotation);
            playerBody.rotation = bedFacingRotation;

            pivotLocalPosition = sleepPosePivot.localPosition;
            pivotLocalRotation = sleepPosePivot.localRotation;
            visualLocalPosition = playerVisual.localPosition;
            visualLocalRotation = playerVisual.localRotation;
            CapturePlayerRendererStates();

            bool keepFirstPersonBodyHidden =
                cameraController != null && cameraController.IsFirstPerson;
            if (hidePlayerDuringSleep || keepFirstPersonBodyHidden)
            {
                SetPlayerVisualHidden(true);
            }

            animatorWasEnabled = playerAnimator.enabled;
            animatorSpeed = playerAnimator.speed;
            playerAnimator.SetBool("IsMoving", false);
            playerAnimator.SetFloat("MoveSpeed", 0f);
            playerAnimator.Update(0f);
            playerAnimator.enabled = false;

            if (!playerBody.isKinematic)
            {
                playerBody.linearVelocity = Vector3.zero;
                playerBody.angularVelocity = Vector3.zero;
            }

            playerBody.isKinematic = true;
            playerBody.useGravity = false;

            cameraPosition = targetCamera.transform.position;
            cameraRotation = targetCamera.transform.rotation;
            cameraControllerWasEnabled = cameraController.enabled;
            cameraController.enabled = false;
            sleepCeilingWasActive = sleepCeiling.activeSelf;
            sleepCeiling.SetActive(false);

            PrepareFade();
            sequenceRoutine = StartCoroutine(PlayLieDownRoutine());
        }

        private IEnumerator PlayLieDownRoutine()
        {
            Vector3 pivotStart = sleepPosePivot.position;
            Quaternion actorStartRotation = playerMovement.transform.rotation;
            Quaternion actorTargetRotation = Quaternion.Euler(
                0f,
                sleepPosePoint.eulerAngles.y,
                0f);

            AnimationClip clip = sleepAnimation.GetClip(lieDownClipName);
            AnimationState state = sleepAnimation[lieDownClipName];
            // Keep the lie-down and camera movement on one short timeline so
            // neither pauses while waiting for the other to catch up.
            float transitionDuration = Mathf.Min(
                lieDownDuration,
                Mathf.Clamp(cameraBlendDuration, 0.65f, 1.15f));
            float cameraStartTime = Mathf.Min(cameraBlendDelay, 0.03f);
            state.speed = clip.length / Mathf.Max(0.2f, transitionDuration);
            state.wrapMode = WrapMode.ClampForever;
            sleepAnimation.Play(lieDownClipName);

            float elapsed = 0f;
            while (elapsed < transitionDuration)
            {
                elapsed += Time.unscaledDeltaTime;
                float normalized = Mathf.Clamp01(elapsed / transitionDuration);
                float eased = movementCurve.Evaluate(normalized);

                sleepPosePivot.position = Vector3.Lerp(
                    pivotStart,
                    sleepPosePoint.position,
                    eased);
                playerMovement.transform.rotation = Quaternion.Slerp(
                    actorStartRotation,
                    actorTargetRotation,
                    Mathf.SmoothStep(0f, 1f, Mathf.Clamp01(normalized / 0.35f)));
                sleepAnimation.Sample();
                playerVisual.position = sleepPosePivot.position;
                playerVisual.rotation =
                    playerMovement.transform.rotation *
                    sleepPosePivot.localRotation *
                    visualLocalRotation;

                float cameraNormalized = Mathf.Clamp01(
                    (elapsed - cameraStartTime) /
                    Mathf.Max(0.1f, transitionDuration - cameraStartTime));
                float cameraEase = 1f -
                                   Mathf.Pow(1f - cameraNormalized, 2f);
                targetCamera.transform.position = Vector3.Lerp(
                    cameraPosition,
                    sleepCameraPoint.position,
                    cameraEase);
                targetCamera.transform.rotation = Quaternion.Slerp(
                    cameraRotation,
                    sleepCameraPoint.rotation,
                    cameraEase);
                if (!sleepCeiling.activeSelf &&
                    HasCameraPassedBelowCeiling(cameraNormalized))
                {
                    sleepCeiling.SetActive(true);
                }
                yield return null;
            }

            sleepPosePivot.position = sleepPosePoint.position;
            playerMovement.transform.rotation = actorTargetRotation;
            sleepAnimation.Sample();
            playerVisual.position = sleepPosePoint.position;
            playerVisual.rotation =
                actorTargetRotation *
                sleepPosePivot.localRotation *
                visualLocalRotation;
            targetCamera.transform.SetPositionAndRotation(
                sleepCameraPoint.position,
                sleepCameraPoint.rotation);
            if (!sleepCeiling.activeSelf)
            {
                sleepCeiling.SetActive(true);
            }

            if (settleDuration > 0f)
            {
                yield return new WaitForSecondsRealtime(settleDuration);
            }

            float fadeElapsed = 0f;
            while (fadeElapsed < fadeDuration)
            {
                fadeElapsed += Time.unscaledDeltaTime;
                fadeCanvas.alpha = Mathf.Clamp01(fadeElapsed / fadeDuration);
                yield return null;
            }

            fadeCanvas.alpha = 1f;
            Action callback = readyForLoading;
            readyForLoading = null;
            callback?.Invoke();

            // The loading view is activated by the callback while this black frame is still covering the scene.
            yield return null;
            HideFade();
            sequenceRoutine = null;
        }

        private bool HasCameraPassedBelowCeiling(float cameraNormalized)
        {
            // The temporary ceiling must stay hidden while the isometric camera
            // is above the room. Turn it on as soon as the moving camera has
            // crossed underneath it, where its appearance is outside the view;
            // the subsequent upward rotation then reveals it naturally.
            float ceilingY = sleepCeiling.transform.position.y;
            bool cameraIsBelowCeiling =
                targetCamera.transform.position.y < ceilingY - 0.08f;
            return cameraIsBelowCeiling || cameraNormalized >= 0.985f;
        }

        private void PrepareFade()
        {
            fadeCanvas.gameObject.SetActive(true);
            fadeCanvas.alpha = 0f;
            fadeCanvas.interactable = false;
            fadeCanvas.blocksRaycasts = true;
        }

        private void HideFade()
        {
            if (fadeCanvas == null) return;
            fadeCanvas.alpha = 0f;
            fadeCanvas.blocksRaycasts = false;
            fadeCanvas.gameObject.SetActive(false);
        }

        private void CapturePlayerRendererStates()
        {
            playerRenderers = playerVisual.GetComponentsInChildren<Renderer>(true);
            rendererForceOffStates = new bool[playerRenderers.Length];
            for (int i = 0; i < playerRenderers.Length; i++)
            {
                Renderer playerRenderer = playerRenderers[i];
                rendererForceOffStates[i] =
                    playerRenderer != null && playerRenderer.forceRenderingOff;
            }

            playerVisualHidden = false;
        }

        private void SetPlayerVisualHidden(bool hidden)
        {
            if (playerVisualHidden == hidden || playerRenderers == null) return;

            for (int i = 0; i < playerRenderers.Length; i++)
            {
                Renderer playerRenderer = playerRenderers[i];
                if (playerRenderer == null) continue;

                playerRenderer.forceRenderingOff = hidden ||
                    (rendererForceOffStates != null &&
                     i < rendererForceOffStates.Length &&
                     rendererForceOffStates[i]);
            }

            playerVisualHidden = hidden;
        }

        private void RestorePlayerRendererStates()
        {
            if (playerRenderers != null)
            {
                for (int i = 0; i < playerRenderers.Length; i++)
                {
                    Renderer playerRenderer = playerRenderers[i];
                    if (playerRenderer == null) continue;

                    playerRenderer.forceRenderingOff =
                        rendererForceOffStates != null &&
                        i < rendererForceOffStates.Length &&
                        rendererForceOffStates[i];
                }
            }

            playerRenderers = null;
            rendererForceOffStates = null;
            playerVisualHidden = false;
        }

        private bool HasRequiredReferences()
        {
            return playerMovement != null &&
                   playerInteraction != null &&
                   playerSelection != null &&
                   playerBody != null &&
                   playerAnimator != null &&
                   playerVisual != null &&
                   sleepPosePivot != null &&
                   sleepAnimation != null &&
                   sleepAnimation.GetClip(lieDownClipName) != null &&
                   approachPoint != null &&
                   sleepPosePoint != null &&
                   sleepCameraPoint != null &&
                   targetCamera != null &&
                   cameraController != null &&
                   fadeCanvas != null &&
                   sleepCeiling != null;
        }

        private void ResolveReferences()
        {
            if (playerMovement == null)
                playerMovement = FindFirstObjectByType<PlayerMovement>();
            if (playerInteraction == null && playerMovement != null)
                playerInteraction = playerMovement.GetComponent<PlayerInteraction>();
            if (playerSelection == null && playerMovement != null)
                playerSelection = playerMovement.GetComponent<PlayerSelection>();
            if (playerBody == null && playerMovement != null)
                playerBody = playerMovement.GetComponent<Rigidbody>();
            if (playerAnimator == null && playerMovement != null)
                playerAnimator = playerMovement.GetComponentInChildren<Animator>(true);
            if (playerVisual == null && playerAnimator != null)
                playerVisual = playerAnimator.transform;
            if (targetCamera == null)
                targetCamera = Camera.main;
            if (cameraController == null && targetCamera != null)
                cameraController = targetCamera.GetComponent<IsometricCameraController>();
            if (sleepPosePivot == null && playerMovement != null)
                sleepPosePivot = playerMovement.transform.Find("SleepPosePivot");
            if (sleepAnimation == null && sleepPosePivot != null)
                sleepAnimation = sleepPosePivot.GetComponent<Animation>();
        }

        private void OnValidate()
        {
            approachStopDistance = Mathf.Max(0.1f, approachStopDistance);
            approachBlendDuration = Mathf.Max(0.1f, approachBlendDuration);
            approachProgressThreshold = Mathf.Max(0f, approachProgressThreshold);
            lieDownDuration = Mathf.Max(0.2f, lieDownDuration);
            cameraBlendDelay = Mathf.Max(0f, cameraBlendDelay);
            cameraBlendDuration = Mathf.Max(0.1f, cameraBlendDuration);
            settleDuration = Mathf.Max(0f, settleDuration);
            fadeDuration = Mathf.Max(0.1f, fadeDuration);
        }
    }
}
