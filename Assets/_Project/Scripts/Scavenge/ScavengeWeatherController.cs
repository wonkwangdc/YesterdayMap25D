using System.Collections;
using UnityEngine;
using UnityEngine.Serialization;
using YesterdayMap.Core;

namespace YesterdayMap.Scavenge
{
    /// <summary>
    /// Runs window-local rain ambience and triggers lightning flashes
    /// synchronized to the supplied thunder track.
    /// </summary>
    public sealed class ScavengeWeatherController : MonoBehaviour
    {
        [Header("Scene References")]
        [SerializeField] private Transform followTarget;
        [SerializeField] private ParticleSystem rainSystem;
        [SerializeField] private ParticleSystem[] additionalRainSystems =
            System.Array.Empty<ParticleSystem>();
        [SerializeField] private Light lightningLight;
        [SerializeField] private AudioSource rainSource;
        [SerializeField] private AudioClip rainClip;
        [SerializeField] private AudioSource thunderSource;
        [SerializeField] private AudioClip thunderClip;
        [SerializeField] private bool followTargetWithWeather;

        [Header("Thunder Track Timing")]
        [FormerlySerializedAs("firstFlashDelay")]
        [SerializeField] private Vector2 firstThunderDelay = new(0.1f, 0.25f);
        [FormerlySerializedAs("lightningInterval")]
        [SerializeField] private Vector2 thunderTrackInterval = new(12f, 24f);
        [SerializeField] private Vector2 flashIntensity = new(2.8f, 4.2f);
        [SerializeField, Range(0f, 1f)] private float thunderVolume = 0.48f;

        [Tooltip("Seconds from the beginning of Weather_Thunder.mp3.")]
        [SerializeField] private float[] thunderFlashTimes =
        {
            0.35f, 6.15f, 11f, 16.75f, 26f,
            34.9f, 36.95f, 43.45f, 49.25f, 54.1f
        };

        [Tooltip("Relative flash strength for each analyzed thunder peak.")]
        [SerializeField] private float[] thunderFlashStrengths =
        {
            0.7f, 0.85f, 0.93f, 0.64f, 1f,
            0.88f, 0.62f, 0.68f, 0.87f, 0.94f
        };

        private Coroutine flashRoutine;
        private float nextThunderTrackTime;
        private float thunderTrackStartedAt;
        private int nextThunderFlashIndex;
        private bool thunderTrackActive;
        private bool thunderPausedByGame;

        private void Awake()
        {
            ResolveReferences();
            SetLightningIntensity(0f);
        }

        private void OnEnable()
        {
            ResolveReferences();
            ScheduleNextThunderTrack(firstThunderDelay);

            SetRainPlayback(true);

            if (rainSource != null && rainClip != null && !rainSource.isPlaying)
            {
                rainSource.clip = rainClip;
                rainSource.loop = true;
                rainSource.Play();
            }
        }

        private void Update()
        {
            if (Time.timeScale <= 0f)
            {
                PauseThunderForGamePause();
                return;
            }

            ResumeThunderAfterGamePause();

            if (!thunderTrackActive)
            {
                if (Time.time >= nextThunderTrackTime)
                    StartThunderTrack();

                return;
            }

            TriggerSynchronizedFlashes();

            if (thunderSource != null &&
                !thunderSource.isPlaying &&
                Time.time - thunderTrackStartedAt > 0.25f)
            {
                FinishThunderTrack();
            }
        }

        private void LateUpdate()
        {
            if (!followTargetWithWeather)
                return;

            if (followTarget == null)
                ResolveReferences();

            if (followTarget != null)
            {
                float yaw = followTarget.eulerAngles.y;
                transform.SetPositionAndRotation(
                    followTarget.position,
                    Quaternion.Euler(0f, yaw, 0f));
            }
        }

        private void OnDisable()
        {
            if (flashRoutine != null)
            {
                StopCoroutine(flashRoutine);
                flashRoutine = null;
            }

            if (thunderSource != null)
                thunderSource.Stop();
            if (rainSource != null)
                rainSource.Stop();

            thunderTrackActive = false;
            thunderPausedByGame = false;
            SetLightningIntensity(0f);
            SetRainPlayback(false);
        }

        /// <summary>
        /// Assigns the scene-created weather components without relying on
        /// string lookups. This also keeps the installer and scene data simple.
        /// </summary>
        public void Configure(
            Transform target,
            ParticleSystem rain,
            Light flashLight,
            AudioSource ambientRainSource,
            AudioClip ambientRainClip,
            AudioSource thunderAudioSource,
            AudioClip thunderAudioClip,
            bool shouldFollowTarget = false,
            ParticleSystem[] extraRainSystems = null)
        {
            followTarget = target;
            rainSystem = rain;
            lightningLight = flashLight;
            rainSource = ambientRainSource;
            rainClip = ambientRainClip;
            thunderSource = thunderAudioSource;
            thunderClip = thunderAudioClip;
            followTargetWithWeather = shouldFollowTarget;
            additionalRainSystems = extraRainSystems ?? System.Array.Empty<ParticleSystem>();
            SetLightningIntensity(0f);
        }

        [ContextMenu("Preview Lightning Now")]
        public void PreviewLightningNow()
        {
            if (!isActiveAndEnabled || flashRoutine != null)
                return;

            flashRoutine = StartCoroutine(PlayLightningSequence(1f));
        }

        [ContextMenu("Preview Synchronized Thunder Track")]
        public void PreviewSynchronizedThunderTrack()
        {
            if (!isActiveAndEnabled)
                return;

            StartThunderTrack();
        }

        private void StartThunderTrack()
        {
            ResolveReferences();
            if (thunderSource == null || thunderClip == null)
            {
                ScheduleNextThunderTrack(thunderTrackInterval);
                return;
            }

            thunderSource.Stop();
            thunderSource.clip = thunderClip;
            thunderSource.volume = thunderVolume;
            thunderSource.loop = false;
            thunderSource.Play();

            nextThunderFlashIndex = 0;
            thunderTrackStartedAt = Time.time;
            thunderTrackActive = true;
            thunderPausedByGame = false;
        }

        private void TriggerSynchronizedFlashes()
        {
            if (thunderSource == null || thunderFlashTimes == null)
                return;

            float playbackTime = thunderSource.time;
            while (nextThunderFlashIndex < thunderFlashTimes.Length &&
                   playbackTime >= thunderFlashTimes[nextThunderFlashIndex])
            {
                float strength = GetFlashStrength(nextThunderFlashIndex);
                if (flashRoutine == null)
                    flashRoutine = StartCoroutine(PlayLightningSequence(strength));

                nextThunderFlashIndex++;
            }
        }

        private float GetFlashStrength(int index)
        {
            if (thunderFlashStrengths == null || index >= thunderFlashStrengths.Length)
                return 1f;

            return Mathf.Clamp01(thunderFlashStrengths[index]);
        }

        private IEnumerator PlayLightningSequence(float strength)
        {
            float strengthMultiplier = Mathf.Lerp(0.55f, 1.05f, strength);
            float peak = Random.Range(flashIntensity.x, flashIntensity.y) * strengthMultiplier;

            SetLightningIntensity(peak);
            yield return new WaitForSeconds(0.055f);
            SetLightningIntensity(0f);
            yield return new WaitForSeconds(0.075f);
            SetLightningIntensity(peak * 0.68f);
            yield return new WaitForSeconds(0.1f);
            SetLightningIntensity(0f);

            flashRoutine = null;
        }

        private void ResolveReferences()
        {
            if (followTarget == null && Camera.main != null)
                followTarget = Camera.main.transform;

            ParticleSystem[] discoveredRain = GetComponentsInChildren<ParticleSystem>(true);
            if (rainSystem == null && discoveredRain.Length > 0)
                rainSystem = discoveredRain[0];
            if ((additionalRainSystems == null || additionalRainSystems.Length == 0) &&
                discoveredRain.Length > 1)
            {
                additionalRainSystems = new ParticleSystem[discoveredRain.Length - 1];
                System.Array.Copy(
                    discoveredRain,
                    1,
                    additionalRainSystems,
                    0,
                    additionalRainSystems.Length);
            }

            lightningLight ??= GetComponentInChildren<Light>(true);
            rainSource ??= FindChildAudioSource("RainAudio");
            thunderSource ??= FindChildAudioSource("ThunderAudio");
        }

        private AudioSource FindChildAudioSource(string childName)
        {
            Transform child = transform.Find(childName);
            return child != null ? child.GetComponent<AudioSource>() : null;
        }

        private void FinishThunderTrack()
        {
            thunderTrackActive = false;
            thunderPausedByGame = false;
            nextThunderFlashIndex = 0;
            ScheduleNextThunderTrack(thunderTrackInterval);
        }

        private void PauseThunderForGamePause()
        {
            if (!thunderTrackActive || thunderSource == null || !thunderSource.isPlaying)
                return;

            thunderSource.Pause();
            thunderPausedByGame = true;
        }

        private void ResumeThunderAfterGamePause()
        {
            if (!thunderPausedByGame || thunderSource == null)
                return;

            thunderSource.UnPause();
            thunderPausedByGame = false;
        }

        private void ScheduleNextThunderTrack(Vector2 range)
        {
            float minimum = Mathf.Max(0.1f, Mathf.Min(range.x, range.y));
            float maximum = Mathf.Max(minimum, Mathf.Max(range.x, range.y));
            nextThunderTrackTime = Time.time + Random.Range(minimum, maximum);
        }

        private void SetLightningIntensity(float intensity)
        {
            if (lightningLight != null)
            {
                lightningLight.intensity = GameSettings.ReduceFlashing
                    ? 0f
                    : Mathf.Max(0f, intensity);
            }
        }

        private void SetRainPlayback(bool shouldPlay)
        {
            SetParticlePlayback(rainSystem, shouldPlay);
            if (additionalRainSystems == null)
                return;

            foreach (ParticleSystem rain in additionalRainSystems)
                SetParticlePlayback(rain, shouldPlay);
        }

        private static void SetParticlePlayback(ParticleSystem particles, bool shouldPlay)
        {
            if (particles == null)
                return;

            if (shouldPlay)
            {
                if (!particles.isPlaying)
                    particles.Play();
            }
            else
            {
                particles.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
            }
        }

    }
}
