using System.Collections;
using UnityEngine;

namespace YesterdayMap.Audio
{
    [RequireComponent(typeof(AudioSource))]
    public sealed class MainMenuMusic : MonoBehaviour
    {
        [SerializeField] private AudioSource audioSource;
        [SerializeField, Min(0f)] private float fadeInDuration = 3f;
        [SerializeField, Range(0f, 1f)] private float targetVolume = 1f;

        private Coroutine fadeCoroutine;

        public void Configure(AudioClip clip, float duration, float volume)
        {
            audioSource = GetComponent<AudioSource>();
            audioSource.clip = clip;
            fadeInDuration = Mathf.Max(0f, duration);
            targetVolume = Mathf.Clamp01(volume);
            ConfigureAudioSource();
        }

        private void Awake()
        {
            if (audioSource == null)
                audioSource = GetComponent<AudioSource>();

            ConfigureAudioSource();
        }

        private void Start()
        {
            if (audioSource == null || audioSource.clip == null)
            {
                Debug.LogWarning("Main menu music clip is not assigned.", this);
                return;
            }

            audioSource.volume = 0f;
            audioSource.Play();
            fadeCoroutine = StartCoroutine(FadeIn());
        }

        private void OnDestroy()
        {
            if (fadeCoroutine != null)
                StopCoroutine(fadeCoroutine);
        }

        private void ConfigureAudioSource()
        {
            if (audioSource == null) return;

            audioSource.playOnAwake = false;
            audioSource.loop = true;
            audioSource.spatialBlend = 0f;
        }

        private IEnumerator FadeIn()
        {
            if (fadeInDuration <= 0f)
            {
                audioSource.volume = targetVolume;
                yield break;
            }

            float elapsed = 0f;
            while (elapsed < fadeInDuration)
            {
                elapsed += Time.unscaledDeltaTime;
                audioSource.volume = Mathf.Lerp(
                    0f,
                    targetVolume,
                    Mathf.Clamp01(elapsed / fadeInDuration));
                yield return null;
            }

            audioSource.volume = targetVolume;
            fadeCoroutine = null;
        }
    }
}
