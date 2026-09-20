using UnityEngine;

namespace YesterdayMap.Character
{
    [DisallowMultipleComponent]
    [RequireComponent(typeof(AudioSource))]
    [DefaultExecutionOrder(100)]
    public sealed class CharacterFootstepAudio : MonoBehaviour
    {
        private const string Footstep1ResourcePath = "Audio/SFX/Footsteps1";
        private const string Footstep2ResourcePath = "Audio/SFX/Footsteps2";

        [SerializeField, Min(0.1f)] private float stepDistance = 3f;
        [SerializeField, Range(0f, 1f)] private float volume = 1f;
        [SerializeField] private AudioClip footstep1;
        [SerializeField] private AudioClip footstep2;
        [SerializeField] private AudioSource audioSource;
        [SerializeField] private Rigidbody body3D;
        [SerializeField] private Rigidbody2D body2D;

        private Vector3 previousPosition;
        private float travelledDistance;
        private bool playFirstClip = true;

        private void Awake()
        {
            ResolveReferences();
            previousPosition = CurrentPhysicsPosition();
        }

        private void OnEnable()
        {
            ResolveReferences();
            previousPosition = CurrentPhysicsPosition();
            travelledDistance = 0f;
        }

        private void FixedUpdate()
        {
            // At the beginning of a physics tick, Rigidbody.position contains the
            // result of the previous simulation even when Transform interpolation
            // has not caught up yet.
            Vector3 currentPosition = CurrentPhysicsPosition();
            Vector3 movement = currentPosition - previousPosition;
            previousPosition = currentPosition;

            // Footsteps follow actual ground travel, not vertical physics movement.
            movement.y = 0f;
            float distance = movement.magnitude;
            bool isMoving = distance > 0.0001f;

            if (!isMoving)
            {
                travelledDistance = 0f;
                return;
            }

            // Treat large single-frame position changes as teleports or scene placement.
            if (distance > Mathf.Max(stepDistance * 3f, 3f))
            {
                travelledDistance = 0f;
                return;
            }

            travelledDistance += distance;
            while (travelledDistance >= stepDistance)
            {
                travelledDistance -= stepDistance;
                PlayNextFootstep();
            }
        }

        private void OnDisable()
        {
            if (audioSource != null)
                audioSource.Stop();
        }

        private void PlayNextFootstep()
        {
            ResolveReferences();
            AudioClip clip = playFirstClip ? footstep1 : footstep2;
            playFirstClip = !playFirstClip;
            if (clip != null)
                audioSource.PlayOneShot(clip, volume);
        }

        private void ResolveReferences()
        {
            if (audioSource == null)
                audioSource = GetComponent<AudioSource>();
            if (body3D == null)
                body3D = GetComponent<Rigidbody>();
            if (body2D == null)
                body2D = GetComponent<Rigidbody2D>();

            audioSource.playOnAwake = false;
            audioSource.loop = false;
            audioSource.spatialBlend = 1f;
            // Unity can keep a destroyed/missing serialized object whose CLR reference is
            // non-null. Use Unity's overloaded null comparison instead of ??=.
            if (footstep1 == null)
                footstep1 = UnityEngine.Resources.Load<AudioClip>(Footstep1ResourcePath);
            if (footstep2 == null)
                footstep2 = UnityEngine.Resources.Load<AudioClip>(Footstep2ResourcePath);
        }

        private Vector3 CurrentPhysicsPosition()
        {
            if (body3D != null)
                return body3D.position;
            if (body2D != null)
                return body2D.position;
            return transform.position;
        }
    }
}
