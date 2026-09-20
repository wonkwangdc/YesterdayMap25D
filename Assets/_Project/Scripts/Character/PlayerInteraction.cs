using UnityEngine;
using UnityEngine.InputSystem;
using YesterdayMap.Interaction;
using YesterdayMap.UI;

namespace YesterdayMap.Character
{
    // 주변 오브젝트를 찾고 키보드 단축키 입력만 전달합니다.
    public sealed class PlayerInteraction : MonoBehaviour
    {
        [SerializeField] private InteractionPromptUI promptUI;
        [SerializeField, Min(0.5f)] private float interactionRange = 1.8f;
        [SerializeField, Min(0.5f)] private float verticalInteractionRange = 3f;
        [SerializeField] private Camera interactionCamera;
        [SerializeField, Min(0.5f)] private float centerAimRange = 4f;
        [SerializeField, Range(0f, 0.5f)] private float centerAimAssistRadius = 0.12f;
        [SerializeField, Min(0f)] private float centerAimCameraClearance = 0.75f;
        [SerializeField] private LayerMask centerAimMask = ~0;

        private readonly RaycastHit[] centerAimHits = new RaycastHit[32];
        private readonly RaycastHit[] centerAimAssistHits = new RaycastHit[32];
        private InteractableObject nearest;
        private PauseMenuUI pauseMenu;

        private void OnEnable()
        {
            // Cache a missing menu too; Scavenge does not always have one.
            pauseMenu = FindFirstObjectByType<PauseMenuUI>(
                FindObjectsInactive.Include);
        }

        public void Configure(InteractionPromptUI interactionPromptUI)
        {
            promptUI = interactionPromptUI;
        }

        private void Update()
        {
            if (Time.timeScale <= 0f ||
                (pauseMenu != null && pauseMenu.BlocksGameplayInput))
            {
                nearest = null;
                promptUI?.SetPrompt(string.Empty);
                return;
            }

            nearest = FindCenterAimedInteractable() ?? FindNearestInteractable();
            promptUI?.SetPrompt(BuildInteractionPrompt(nearest));

            if (Keyboard.current == null || nearest == null)
            {
                return;
            }

            bool alternatePressed =
                Keyboard.current.tKey.wasPressedThisFrame ||
                Keyboard.current.fKey.wasPressedThisFrame;
            bool resetPressed = Keyboard.current.rKey.wasPressedThisFrame;

            if (resetPressed && nearest.CanResetInteract)
            {
                nearest.FaceInteractor(transform);
                nearest.ResetInteract();
            }
            else if (alternatePressed && nearest.CanAlternateInteract)
            {
                nearest.FaceInteractor(transform);
                nearest.AlternateInteract();
            }
            else if (Keyboard.current.eKey.wasPressedThisFrame)
            {
                nearest.FaceInteractor(transform);
                nearest.Interact();
            }

            // 월드 오브젝트는 마우스 클릭으로 실행하지 않습니다.
            // 가까이 이동한 뒤 E, F 또는 T 단축키로만 상호작용합니다.
        }

        private InteractableObject FindNearestInteractable()
        {
            Vector3 bottom = transform.position + Vector3.down * verticalInteractionRange;
            Vector3 top = transform.position + Vector3.up * verticalInteractionRange;
            Collider[] hits = Physics.OverlapCapsule(bottom, top, interactionRange);
            InteractableObject result = null;
            float closest = float.MaxValue;

            foreach (Collider hit in hits)
            {
                InteractableObject candidate = hit.GetComponentInParent<InteractableObject>();
                if (candidate == null || !candidate.CanInteract) continue;
                if (candidate.RequiresCenterAim) continue;
                if (!candidate.IsActorInRange(transform.position, interactionRange)) continue;

                Vector3 flatDelta = candidate.transform.position - transform.position;
                flatDelta.y = 0f;
                float distance = flatDelta.sqrMagnitude;
                if (distance < closest)
                {
                    closest = distance;
                    result = candidate;
                }
            }

            return result;
        }

        private InteractableObject FindCenterAimedInteractable()
        {
            if (interactionCamera == null)
            {
                interactionCamera = Camera.main;
            }
            if (interactionCamera == null) return null;

            Ray ray = interactionCamera.ViewportPointToRay(new Vector3(0.5f, 0.5f, 0f));
            int hitCount = Physics.RaycastNonAlloc(
                ray,
                centerAimHits,
                centerAimRange,
                centerAimMask,
                QueryTriggerInteraction.Collide);

            InteractableObject closestTarget = null;
            float centerBlockerDistance = centerAimRange;
            for (int index = 0; index < hitCount; index++)
            {
                RaycastHit hit = centerAimHits[index];
                if (hit.collider == null || hit.transform.IsChildOf(transform)) continue;

                InteractableObject candidate = hit.collider.GetComponentInParent<InteractableObject>();
                if (hit.collider.isTrigger && candidate == null) continue;
                if (candidate == null && hit.distance < centerAimCameraClearance) continue;
                if (hit.distance >= centerBlockerDistance) continue;

                centerBlockerDistance = hit.distance;
                closestTarget = candidate;
            }

            if (IsUsableCenterTarget(closestTarget))
            {
                return closestTarget;
            }

            if (centerAimAssistRadius <= Mathf.Epsilon) return null;

            int assistHitCount = Physics.SphereCastNonAlloc(
                ray,
                centerAimAssistRadius,
                centerAimAssistHits,
                centerAimRange,
                centerAimMask,
                QueryTriggerInteraction.Collide);

            InteractableObject assistedTarget = null;
            float closestAssistedDistance = float.MaxValue;
            for (int index = 0; index < assistHitCount; index++)
            {
                RaycastHit hit = centerAimAssistHits[index];
                if (hit.collider == null || hit.transform.IsChildOf(transform)) continue;

                InteractableObject candidate = hit.collider.GetComponentInParent<InteractableObject>();
                if (!IsUsableCenterTarget(candidate)) continue;

                float estimatedSurfaceDistance = hit.distance + centerAimAssistRadius;
                if (estimatedSurfaceDistance > centerBlockerDistance + 0.02f) continue;
                if (hit.distance >= closestAssistedDistance) continue;

                closestAssistedDistance = hit.distance;
                assistedTarget = candidate;
            }

            return assistedTarget;
        }

        private bool IsUsableCenterTarget(InteractableObject target)
        {
            return target != null &&
                   target.RequiresCenterAim &&
                   target.CanInteract &&
                   target.IsActorInRange(transform.position, centerAimRange);
        }

        private static string BuildInteractionPrompt(InteractableObject target)
        {
            if (target == null) return string.Empty;
            string primaryPrompt = target.InteractionPrompt;
            string alternatePrompt = target.AlternateInteractionPrompt;
            string resetPrompt = target.ResetInteractionPrompt;

            string combinedPrompt = primaryPrompt;
            if (target.CanAlternateInteract &&
                !string.IsNullOrEmpty(alternatePrompt))
            {
                combinedPrompt = AppendPrompt(combinedPrompt, alternatePrompt);
            }

            if (target.CanResetInteract &&
                !string.IsNullOrEmpty(resetPrompt))
            {
                combinedPrompt = AppendPrompt(combinedPrompt, resetPrompt);
            }

            return combinedPrompt;
        }

        private static string AppendPrompt(string current, string next)
        {
            return string.IsNullOrEmpty(current)
                ? next
                : $"{current}\n{next}";
        }
    }
}
