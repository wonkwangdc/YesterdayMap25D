using UnityEngine;
using YesterdayMap.Character;
using YesterdayMap.Interaction;

namespace YesterdayMap.Shelter
{
    public sealed class SoccerBallKickObject : InteractableObject
    {
        private const string InteractionChildName = "__SoccerBallKickInteraction";
        private const string KickPrompt = "[E] 축구공 차기";
        private const float ForwardKickImpulse = 2.35f;
        private const float LiftKickImpulse = 0.95f;
        private const float SpinImpulse = 0.08f;
        private const float KickCooldown = 0.35f;
        private const float MaxPlanarSpeed = 7.5f;

        [SerializeField] private Rigidbody ballBody;
        [SerializeField] private CharacterStats characterStats;
        [SerializeField, Min(0f)] private float moralePerKick = 1f;

        private static PhysicsMaterial soccerBallMaterial;
        private float nextKickTime;
        private Vector3 resetPosition;
        private Quaternion resetRotation;
        private bool resetPoseCaptured;

        public override bool CanInteract =>
            base.CanInteract &&
            ballBody != null &&
            Time.time >= nextKickTime;
        public override string ResetInteractionPrompt =>
            "[R] 축구공 위치 초기화";
        public override bool CanResetInteract => ballBody != null;

        private void Awake()
        {
            if (ballBody == null)
            {
                ballBody = GetComponentInParent<Rigidbody>();
            }

            CaptureResetPose();
            ResolveCharacterStats();
            ConfigurePhysics();
            SetPrompt(KickPrompt);
        }

        private void ConfigurePhysics()
        {
            if (ballBody == null)
            {
                return;
            }

            ballBody.mass = 0.43f;
            ballBody.useGravity = true;
            ballBody.isKinematic = false;
            ballBody.linearDamping = 0.12f;
            ballBody.angularDamping = 0.08f;
            ballBody.maxAngularVelocity = 45f;
            ballBody.maxLinearVelocity = 9f;
            ballBody.interpolation = RigidbodyInterpolation.Interpolate;
            ballBody.collisionDetectionMode = CollisionDetectionMode.ContinuousDynamic;
            ballBody.constraints = RigidbodyConstraints.None;
            ballBody.sleepThreshold = 0.02f;

            SphereCollider ballCollider = GetComponent<SphereCollider>();
            if (ballCollider != null)
            {
                // Match the sphere to the visible ball and let gravity resolve
                // its floor contact instead of locking its vertical position.
                transform.localPosition = new Vector3(0f, 0.225f, 0.225f);
                ballCollider.radius = 0.23f;
                ballCollider.isTrigger = false;
                ballCollider.material = GetSoccerBallMaterial();
            }
        }

        private void FixedUpdate()
        {
            if (ballBody == null)
            {
                return;
            }

            Vector3 velocity = ballBody.linearVelocity;
            Vector3 planarVelocity = new(velocity.x, 0f, velocity.z);
            if (planarVelocity.sqrMagnitude > MaxPlanarSpeed * MaxPlanarSpeed)
            {
                planarVelocity = planarVelocity.normalized * MaxPlanarSpeed;
                ballBody.linearVelocity = new Vector3(
                    planarVelocity.x,
                    velocity.y,
                    planarVelocity.z);
            }
        }

        public override void Interact()
        {
            if (!CanInteract)
            {
                return;
            }

            PlayerInteraction player = Object.FindFirstObjectByType<PlayerInteraction>();
            Vector3 kickDirection = player != null
                ? ballBody.worldCenterOfMass - player.transform.position
                : transform.forward;
            kickDirection.y = 0f;

            if (kickDirection.sqrMagnitude < 0.001f)
            {
                kickDirection = player != null
                    ? player.transform.forward
                    : Vector3.forward;
                kickDirection.y = 0f;
            }

            kickDirection.Normalize();

            Vector3 velocity = ballBody.linearVelocity;
            Vector3 planarVelocity = new(velocity.x, 0f, velocity.z);
            if (planarVelocity.sqrMagnitude > MaxPlanarSpeed * MaxPlanarSpeed)
            {
                planarVelocity = planarVelocity.normalized * MaxPlanarSpeed;
                ballBody.linearVelocity = new Vector3(
                    planarVelocity.x,
                    velocity.y,
                    planarVelocity.z);
            }

            nextKickTime = Time.time + KickCooldown;
            ballBody.WakeUp();
            ballBody.AddForce(
                kickDirection * ForwardKickImpulse +
                Vector3.up * LiftKickImpulse,
                ForceMode.Impulse);
            ballBody.AddTorque(
                Vector3.Cross(Vector3.up, kickDirection) * SpinImpulse,
                ForceMode.Impulse);

            ResolveCharacterStats();
            if (characterStats != null && moralePerKick > 0f)
            {
                characterStats.ModifyMorale(moralePerKick);
            }
        }

        public void ResetBallPosition()
        {
            if (ballBody == null || !resetPoseCaptured)
            {
                return;
            }

            ballBody.position = resetPosition;
            ballBody.rotation = resetRotation;
            ballBody.linearVelocity = Vector3.zero;
            ballBody.angularVelocity = Vector3.zero;
            ballBody.WakeUp();
            nextKickTime = 0f;
        }

        public override void ResetInteract()
        {
            ResetBallPosition();
        }

        public static SoccerBallKickObject Ensure(GameObject ballVisual)
        {
            if (ballVisual == null)
            {
                return null;
            }

            SoccerBallKickObject existing =
                ballVisual.GetComponentInChildren<SoccerBallKickObject>(true);
            if (existing != null)
            {
                existing.CaptureResetPose();
                existing.ResolveCharacterStats();
                existing.ConfigurePhysics();
                return existing;
            }

            foreach (Collider rootCollider in ballVisual.GetComponents<Collider>())
            {
                rootCollider.enabled = false;
            }

            GameObject interactionObject = new(InteractionChildName);
            interactionObject.layer = ballVisual.layer;
            interactionObject.transform.SetParent(ballVisual.transform, false);
            interactionObject.transform.localPosition = new Vector3(0f, 0.225f, 0.225f);

            SphereCollider ballCollider = interactionObject.AddComponent<SphereCollider>();
            ballCollider.radius = 0.23f;
            ballCollider.isTrigger = false;
            ballCollider.material = GetSoccerBallMaterial();

            Rigidbody body = ballVisual.GetComponent<Rigidbody>();
            if (body == null)
            {
                body = ballVisual.AddComponent<Rigidbody>();
            }

            SoccerBallKickObject kickObject =
                interactionObject.AddComponent<SoccerBallKickObject>();
            kickObject.ballBody = body;
            kickObject.CaptureResetPose();
            kickObject.ResolveCharacterStats();
            kickObject.ConfigurePhysics();
            kickObject.SetPrompt(KickPrompt);
            return kickObject;
        }

        private void CaptureResetPose()
        {
            if (resetPoseCaptured || ballBody == null)
            {
                return;
            }

            resetPosition = ballBody.position;
            resetRotation = ballBody.rotation;
            resetPoseCaptured = true;
        }

        private void ResolveCharacterStats()
        {
            if (characterStats == null)
            {
                characterStats = Object.FindFirstObjectByType<CharacterStats>();
            }
        }

        private static PhysicsMaterial GetSoccerBallMaterial()
        {
            if (soccerBallMaterial != null)
            {
                return soccerBallMaterial;
            }

            soccerBallMaterial = new PhysicsMaterial("Soccer Ball Physics")
            {
                bounciness = 0.62f,
                dynamicFriction = 0.34f,
                staticFriction = 0.28f,
                bounceCombine = PhysicsMaterialCombine.Maximum,
                frictionCombine = PhysicsMaterialCombine.Average,
                hideFlags = HideFlags.HideAndDontSave
            };
            return soccerBallMaterial;
        }
    }
}
