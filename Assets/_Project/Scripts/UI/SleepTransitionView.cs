using System;
using System.Collections;
using UnityEngine;
using UnityEngine.UI;

namespace YesterdayMap.UI
{
    /// <summary>
    /// Displays the full-screen sleep artwork and advances its real loading gauge.
    /// The gauge reuses the same shader and material system as exploration loading.
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class SleepTransitionView : MonoBehaviour
    {
        private static readonly int ProgressId = Shader.PropertyToID("_Progress");
        private static readonly int GaugeRectId = Shader.PropertyToID("_GaugeRect");
        private static readonly int TrackColorId = Shader.PropertyToID("_TrackColor");
        private static readonly int FillColorId = Shader.PropertyToID("_FillColor");

        [SerializeField, Min(0.1f)] private float duration = 3f;
        [SerializeField] private RawImage artworkImage;
        [SerializeField] private RawImage progressOverlay;
        [SerializeField] private Rect gaugeRect =
            new(0.052f, 0.1276f, 0.451f, 0.048f);
        [SerializeField] private Color trackColor =
            new(0.15f, 0.14f, 0.125f, 1f);
        [SerializeField] private Color fillColor =
            new(0.46f, 0.45f, 0.42f, 1f);

        private Material gaugeMaterialInstance;
        private Coroutine transitionRoutine;

        public float Duration => Mathf.Max(0.1f, duration);

        public void Configure(
            RawImage artwork,
            RawImage overlay,
            float transitionDuration,
            Rect loadingGaugeRect,
            Color loadingTrackColor,
            Color loadingFillColor)
        {
            artworkImage = artwork;
            progressOverlay = overlay;
            duration = Mathf.Max(0.1f, transitionDuration);
            gaugeRect = loadingGaugeRect;
            trackColor = loadingTrackColor;
            fillColor = loadingFillColor;
        }

        public bool Play(Action atComplete)
        {
            if (!Prepare())
            {
                Debug.LogError("Sleep transition prefab is incomplete.", this);
                return false;
            }

            if (transitionRoutine != null)
                StopCoroutine(transitionRoutine);

            gameObject.SetActive(true);
            transform.SetAsLastSibling();
            transitionRoutine = StartCoroutine(PlayRoutine(atComplete));
            return true;
        }

        public void SetProgress(float progress)
        {
            if (!EnsureGaugeMaterialInstance()) return;
            gaugeMaterialInstance.SetFloat(ProgressId, Mathf.Clamp01(progress));
        }

        private IEnumerator PlayRoutine(Action atComplete)
        {
            SetProgress(0f);
            float elapsed = 0f;
            while (elapsed < Duration)
            {
                elapsed += Time.unscaledDeltaTime;
                SetProgress(Mathf.Clamp01(elapsed / Duration));
                yield return null;
            }

            SetProgress(1f);
            transitionRoutine = null;
            gameObject.SetActive(false);
            atComplete?.Invoke();
        }

        private bool Prepare()
        {
            if (artworkImage == null ||
                progressOverlay == null ||
                artworkImage.texture == null ||
                !EnsureGaugeMaterialInstance())
            {
                return false;
            }

            progressOverlay.texture = artworkImage.texture;
            gaugeMaterialInstance.SetVector(
                GaugeRectId,
                new Vector4(gaugeRect.x, gaugeRect.y, gaugeRect.width, gaugeRect.height));
            gaugeMaterialInstance.SetColor(TrackColorId, trackColor);
            gaugeMaterialInstance.SetColor(FillColorId, fillColor);
            return true;
        }

        private bool EnsureGaugeMaterialInstance()
        {
            if (gaugeMaterialInstance != null) return true;
            if (progressOverlay == null || progressOverlay.material == null)
                return false;

            gaugeMaterialInstance = new Material(progressOverlay.material)
            {
                name = "SleepTransitionGauge (Instance)",
                hideFlags = HideFlags.HideAndDontSave
            };
            progressOverlay.material = gaugeMaterialInstance;
            return true;
        }

        private void OnValidate()
        {
            duration = Mathf.Max(0.1f, duration);
        }

        private void OnDestroy()
        {
            if (gaugeMaterialInstance == null) return;

            if (Application.isPlaying)
                Destroy(gaugeMaterialInstance);
            else
                DestroyImmediate(gaugeMaterialInstance);
        }
    }
}
