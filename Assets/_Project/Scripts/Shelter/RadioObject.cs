using System.Collections;
using UnityEngine;
using YesterdayMap.CameraSystem;
using YesterdayMap.Character;
using YesterdayMap.Core;
using YesterdayMap.Events;

namespace YesterdayMap.Shelter
{
    public sealed class RadioObject : ShelterObject
    {
        [SerializeField] private ShelterEventDialogueController eventDialogue;

        [Header("Radio Focus")]
        [SerializeField, Min(0.5f)] private float focusDistance = 1.35f;
        [SerializeField, Min(0.05f)] private float focusBlendDuration = 0.55f;
        [SerializeField, Min(0.05f)] private float returnBlendDuration = 0.42f;

        private Camera focusCamera;
        private IsometricCameraController cameraController;
        private PlayerMovement playerMovement;
        private PlayerInteraction playerInteraction;
        private ShelterEventDialogueUI dialogueUI;
        private Coroutine interactionRoutine;
        private Vector3 savedCameraPosition;
        private Quaternion savedCameraRotation;
        private float savedFieldOfView;
        private float savedOrthographicSize;
        private bool cameraControllerWasEnabled;
        private bool playerInteractionWasEnabled;
        private bool cameraStateCaptured;

        public override string InteractionPrompt => "[E] 라디오 켜기";

        // The radio's own collider must be nearby. Looking at the desk itself is no
        // longer enough to trigger the story interaction.
        public override bool RequiresCenterAim => false;

        public override bool CanInteract
        {
            get
            {
                ResolveReferences();
                return interactionRoutine == null &&
                       base.CanInteract &&
                       eventDialogue != null &&
                       (eventDialogue.IsQuarter2SignalDeskEventActive ||
                        eventDialogue.IsQuarter3SignalStoryActive) &&
                       !eventDialogue.IsQuarter3FinalChoiceReady;
            }
        }

        public void Configure(DayCycleManager cycle) { }

        private void Awake()
        {
            ResolveReferences();
            DisableLegacyDeskInteraction();
        }

        private void OnDisable()
        {
            if (interactionRoutine != null)
            {
                StopCoroutine(interactionRoutine);
                interactionRoutine = null;
            }

            RestoreInteractionControl(true);
        }

        public override void Interact()
        {
            if (interactionRoutine != null)
            {
                return;
            }

            ResolveReferences();
            if (eventDialogue == null)
            {
                return;
            }

            interactionRoutine = StartCoroutine(RunRadioInteraction());
        }

        private IEnumerator RunRadioInteraction()
        {
            ResolveReferences();

            if (focusCamera == null)
            {
                eventDialogue.TryOpenTodaysEvent();
                interactionRoutine = null;
                yield break;
            }

            CaptureAndLockInteractionControl();
            GetFocusPose(out Vector3 focusPosition, out Quaternion focusRotation);
            float focusFov = Mathf.Min(savedFieldOfView, 46f);
            float focusOrthoSize = Mathf.Min(savedOrthographicSize, 0.82f);

            yield return BlendCamera(
                focusPosition,
                focusRotation,
                focusFov,
                focusOrthoSize,
                focusBlendDuration);

            eventDialogue.TryOpenTodaysEvent();

            // Give the dialogue one frame to activate, then hold the radio close-up
            // until the final choice closes the panel.
            yield return null;
            float openWait = 0f;
            while (dialogueUI != null && !dialogueUI.IsOpen && openWait < 0.25f)
            {
                openWait += Time.unscaledDeltaTime;
                yield return null;
            }

            while (dialogueUI != null && dialogueUI.IsOpen)
            {
                yield return null;
            }

            yield return BlendCamera(
                savedCameraPosition,
                savedCameraRotation,
                savedFieldOfView,
                savedOrthographicSize,
                returnBlendDuration);

            RestoreInteractionControl(false);
            interactionRoutine = null;
        }

        private void CaptureAndLockInteractionControl()
        {
            savedCameraPosition = focusCamera.transform.position;
            savedCameraRotation = focusCamera.transform.rotation;
            savedFieldOfView = focusCamera.fieldOfView;
            savedOrthographicSize = focusCamera.orthographicSize;
            cameraStateCaptured = true;

            cameraControllerWasEnabled = cameraController != null && cameraController.enabled;
            playerInteractionWasEnabled = playerInteraction != null && playerInteraction.enabled;

            IsometricCameraController.SetFirstPersonUiFocus(true);
            if (cameraController != null)
            {
                cameraController.enabled = false;
            }

            playerMovement?.SetCinematicInputLocked(true);
            if (playerInteraction != null)
            {
                playerInteraction.enabled = false;
            }
        }

        private void RestoreInteractionControl(bool restoreCameraImmediately)
        {
            if (!cameraStateCaptured)
            {
                return;
            }

            if (restoreCameraImmediately && focusCamera != null)
            {
                focusCamera.transform.SetPositionAndRotation(
                    savedCameraPosition,
                    savedCameraRotation);
                focusCamera.fieldOfView = savedFieldOfView;
                focusCamera.orthographicSize = savedOrthographicSize;
            }

            if (cameraController != null)
            {
                cameraController.enabled = cameraControllerWasEnabled;
            }

            playerMovement?.SetCinematicInputLocked(false);
            if (playerInteraction != null)
            {
                playerInteraction.enabled = playerInteractionWasEnabled;
            }

            IsometricCameraController.SetFirstPersonUiFocus(false);
            cameraStateCaptured = false;
        }

        private IEnumerator BlendCamera(
            Vector3 targetPosition,
            Quaternion targetRotation,
            float targetFieldOfView,
            float targetOrthographicSize,
            float duration)
        {
            Vector3 startPosition = focusCamera.transform.position;
            Quaternion startRotation = focusCamera.transform.rotation;
            float startFieldOfView = focusCamera.fieldOfView;
            float startOrthographicSize = focusCamera.orthographicSize;
            float elapsed = 0f;

            while (elapsed < duration)
            {
                elapsed += Time.unscaledDeltaTime;
                float normalized = Mathf.Clamp01(elapsed / duration);
                float eased = normalized * normalized * (3f - 2f * normalized);
                focusCamera.transform.SetPositionAndRotation(
                    Vector3.Lerp(startPosition, targetPosition, eased),
                    Quaternion.Slerp(startRotation, targetRotation, eased));
                focusCamera.fieldOfView = Mathf.Lerp(
                    startFieldOfView,
                    targetFieldOfView,
                    eased);
                focusCamera.orthographicSize = Mathf.Lerp(
                    startOrthographicSize,
                    targetOrthographicSize,
                    eased);
                yield return null;
            }

            focusCamera.transform.SetPositionAndRotation(targetPosition, targetRotation);
            focusCamera.fieldOfView = targetFieldOfView;
            focusCamera.orthographicSize = targetOrthographicSize;
        }

        private void GetFocusPose(out Vector3 position, out Quaternion rotation)
        {
            Bounds bounds = GetRadioBounds();
            Vector3 target = bounds.center + Vector3.up * bounds.extents.y * 0.08f;
            Vector3 flatCameraDirection = Vector3.ProjectOnPlane(
                focusCamera.transform.position - target,
                Vector3.up);
            if (flatCameraDirection.sqrMagnitude < 0.01f)
            {
                flatCameraDirection = -transform.forward;
            }

            flatCameraDirection.Normalize();
            position = target + flatCameraDirection * Mathf.Max(
                focusDistance,
                bounds.extents.magnitude * 1.65f);
            // Keep the camera below the radio's centre and aim at its lower panel.
            // This gives a slight upward view and places the radio above the lower
            // dialogue frame instead of hiding it behind the UI.
            position.y = target.y - Mathf.Max(0.08f, bounds.extents.y * 0.55f);
            Vector3 lookTarget = target -
                                 Vector3.up * Mathf.Max(0.06f, bounds.extents.y * 0.28f);
            rotation = Quaternion.LookRotation(lookTarget - position, Vector3.up);
        }

        private Bounds GetRadioBounds()
        {
            Renderer[] renderers = GetComponentsInChildren<Renderer>(true);
            if (renderers.Length == 0)
            {
                return new Bounds(transform.position, Vector3.one * 0.5f);
            }

            Bounds bounds = renderers[0].bounds;
            for (int index = 1; index < renderers.Length; index++)
            {
                bounds.Encapsulate(renderers[index].bounds);
            }
            return bounds;
        }

        private void ResolveReferences()
        {
            if (eventDialogue == null)
            {
                eventDialogue = Object.FindFirstObjectByType<ShelterEventDialogueController>(
                    FindObjectsInactive.Include);
            }
            if (dialogueUI == null)
            {
                dialogueUI = Object.FindFirstObjectByType<ShelterEventDialogueUI>(
                    FindObjectsInactive.Include);
            }
            if (focusCamera == null)
            {
                focusCamera = Camera.main;
            }
            if (cameraController == null && focusCamera != null)
            {
                cameraController = focusCamera.GetComponent<IsometricCameraController>();
            }
            if (playerMovement == null)
            {
                playerMovement = Object.FindFirstObjectByType<PlayerMovement>();
            }
            if (playerInteraction == null)
            {
                playerInteraction = Object.FindFirstObjectByType<PlayerInteraction>();
            }
        }

        private static void DisableLegacyDeskInteraction()
        {
            foreach (DeskStoryObject deskInteraction in
                     Object.FindObjectsByType<DeskStoryObject>(
                         FindObjectsInactive.Include,
                         FindObjectsSortMode.None))
            {
                if (deskInteraction != null)
                {
                    deskInteraction.enabled = false;
                }
            }
        }
    }
}
