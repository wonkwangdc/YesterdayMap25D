using UnityEngine;

namespace YesterdayMap.Interaction
{
    public abstract class InteractableObject : MonoBehaviour, IInteractable
    {
        [SerializeField] private string interactionPrompt = "Press E to interact";
        [SerializeField] private Transform interactionPoint;
        [SerializeField, Min(0.05f)] private float approachStopDistance = 0.18f;

        public virtual string InteractionPrompt => interactionPrompt;
        public virtual bool CanInteract => isActiveAndEnabled;
        public virtual bool RequiresCenterAim => false;
        public virtual string AlternateInteractionPrompt => string.Empty;
        public virtual bool CanAlternateInteract => false;
        public virtual string ResetInteractionPrompt => string.Empty;
        public virtual bool CanResetInteract => false;
        public float ApproachStopDistance => approachStopDistance;

        public abstract void Interact();
        public virtual void AlternateInteract() { }
        public virtual void ResetInteract() { }

        public void SetPrompt(string prompt) => interactionPrompt = prompt;
        public void SetInteractionPoint(Transform point, float stopDistance = 0.18f)
        {
            interactionPoint = point;
            approachStopDistance = Mathf.Max(0.05f, stopDistance);
        }

        public Vector3 GetApproachPosition(Vector3 actorPosition)
        {
            if (interactionPoint != null)
            {
                return interactionPoint.position;
            }

            Collider hitArea = FindClosestEnabledCollider(actorPosition);
            if (hitArea == null)
            {
                return transform.position;
            }

            Vector3 closest = hitArea.ClosestPoint(actorPosition);
            Vector3 awayFromObject = closest - transform.position;
            awayFromObject.y = 0f;

            if (awayFromObject.sqrMagnitude < 0.001f)
            {
                awayFromObject = actorPosition - transform.position;
                awayFromObject.y = 0f;
            }

            Vector3 offset = awayFromObject.sqrMagnitude > 0.001f ? awayFromObject.normalized * 0.65f : -transform.forward * 0.65f;
            Vector3 approach = closest + offset;
            approach.y = actorPosition.y;
            return approach;
        }

        public bool IsActorInRange(Vector3 actorPosition, float range)
        {
            if (interactionPoint != null)
            {
                Vector3 flatDelta = interactionPoint.position - actorPosition;
                flatDelta.y = 0f;
                return flatDelta.sqrMagnitude <= range * range;
            }

            Collider hitArea = FindClosestEnabledCollider(actorPosition);
            if (hitArea == null)
            {
                return Vector3.Distance(actorPosition, transform.position) <= range;
            }

            Vector3 closest = hitArea.ClosestPoint(actorPosition);
            closest.y = actorPosition.y;
            return Vector3.Distance(actorPosition, closest) <= range;
        }

        private Collider FindClosestEnabledCollider(Vector3 actorPosition)
        {
            Collider closestCollider = null;
            float closestDistance = float.MaxValue;

            foreach (Collider collider in GetComponentsInChildren<Collider>())
            {
                if (!collider.enabled || !collider.gameObject.activeInHierarchy) continue;

                Vector3 closestPoint = collider.ClosestPoint(actorPosition);
                closestPoint.y = actorPosition.y;
                float distance = (closestPoint - actorPosition).sqrMagnitude;
                if (distance >= closestDistance) continue;

                closestDistance = distance;
                closestCollider = collider;
            }

            return closestCollider;
        }

        public void FaceInteractor(Transform actor)
        {
            Vector3 direction = transform.position - actor.position;
            direction.y = 0f;
            if (direction.sqrMagnitude > 0.001f)
            {
                actor.forward = direction.normalized;
            }
        }
    }
}
