using System;
using UnityEngine;
using UnityEngine.UI;

namespace YesterdayMap.Exploration
{
    /// <summary>
    /// Displays the reusable exploration loading artwork and its native gauge.
    /// Location presentation data is configured directly on the prefab.
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class ExplorationLoadingView : MonoBehaviour
    {
        [Serializable]
        private struct ArtworkEntry
        {
            [SerializeField] private ExplorationLocationData location;
            [SerializeField] private Texture2D artwork;
            [SerializeField] private Rect gaugeRect;
            [SerializeField] private Color trackColor;
            [SerializeField] private Color fillColor;

            public ExplorationLocationData Location => location;
            public Texture2D Artwork => artwork;
            public Rect GaugeRect => gaugeRect;
            public Color TrackColor => trackColor;
            public Color FillColor => fillColor;

            public ArtworkEntry(
                ExplorationLocationData locationData,
                Texture2D texture,
                Rect loadingGaugeRect,
                Color loadingTrackColor,
                Color loadingFillColor)
            {
                location = locationData;
                artwork = texture;
                gaugeRect = loadingGaugeRect;
                trackColor = loadingTrackColor;
                fillColor = loadingFillColor;
            }
        }

        private static readonly int ProgressId = Shader.PropertyToID("_Progress");
        private static readonly int GaugeRectId = Shader.PropertyToID("_GaugeRect");
        private static readonly int TrackColorId = Shader.PropertyToID("_TrackColor");
        private static readonly int FillColorId = Shader.PropertyToID("_FillColor");

        private static readonly Rect DefaultGaugeRect =
            new(0.046f, 0.0934f, 0.442f, 0.048f);
        private static readonly Color DefaultTrackColor =
            new(0.17f, 0.155f, 0.135f, 1f);
        private static readonly Color DefaultFillColor =
            new(0.451f, 0.439f, 0.416f, 1f);

        [SerializeField, Min(0.1f)] private float duration = 3f;
        [SerializeField] private RawImage artworkImage;
        [SerializeField] private RawImage progressOverlay;
        [SerializeField] private ArtworkEntry[] artworkEntries = Array.Empty<ArtworkEntry>();

        private Material gaugeMaterialInstance;

        public float Duration => Mathf.Max(0.1f, duration);

        /// <summary>
        /// Selects the configured artwork and shows this common loading view.
        /// </summary>
        public bool Show(ExplorationLocationData location)
        {
            bool foundEntry = TryFindArtworkEntry(location, out ArtworkEntry entry);
            Texture2D artwork = foundEntry ? entry.Artwork : null;
            if (artworkImage == null ||
                progressOverlay == null ||
                artwork == null ||
                !EnsureGaugeMaterialInstance())
            {
                Debug.LogError(
                    $"Exploration loading prefab is incomplete for '{location?.LocationName ?? "Unknown"}'.",
                    this);
                return false;
            }

            artworkImage.texture = artwork;
            progressOverlay.texture = artwork;
            ApplyGaugeStyle(entry);
            SetProgress(0f);
            gameObject.SetActive(true);
            transform.SetAsLastSibling();
            return true;
        }

        public void Hide()
        {
            gameObject.SetActive(false);
        }

        public void SetProgress(float progress)
        {
            if (!EnsureGaugeMaterialInstance()) return;
            gaugeMaterialInstance.SetFloat(ProgressId, Mathf.Clamp01(progress));
        }

        public Texture2D FindArtwork(ExplorationLocationData location)
        {
            return TryFindArtworkEntry(location, out ArtworkEntry entry)
                ? entry.Artwork
                : null;
        }

        /// <summary>
        /// Used by the editor installer to create the editable prefab once.
        /// </summary>
        public void Configure(
            RawImage image,
            RawImage overlay,
            ExplorationLocationData[] locations,
            Texture2D[] artworks,
            Rect[] gaugeRects,
            Color[] trackColors,
            Color[] fillColors,
            float loadingDuration)
        {
            artworkImage = image;
            progressOverlay = overlay;
            duration = Mathf.Max(0.1f, loadingDuration);

            int count = Mathf.Min(locations?.Length ?? 0, artworks?.Length ?? 0);
            artworkEntries = new ArtworkEntry[count];
            for (int i = 0; i < count; i++)
            {
                Rect gaugeRect = gaugeRects != null && i < gaugeRects.Length
                    ? gaugeRects[i]
                    : DefaultGaugeRect;
                Color trackColor = trackColors != null && i < trackColors.Length
                    ? trackColors[i]
                    : DefaultTrackColor;
                Color fillColor = fillColors != null && i < fillColors.Length
                    ? fillColors[i]
                    : DefaultFillColor;
                artworkEntries[i] = new ArtworkEntry(
                    locations[i],
                    artworks[i],
                    gaugeRect,
                    trackColor,
                    fillColor);
            }
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

        private bool EnsureGaugeMaterialInstance()
        {
            if (gaugeMaterialInstance != null) return true;
            if (progressOverlay == null || progressOverlay.material == null)
                return false;

            gaugeMaterialInstance = new Material(progressOverlay.material)
            {
                name = "ExplorationLoadingGauge (Instance)",
                hideFlags = HideFlags.HideAndDontSave
            };
            progressOverlay.material = gaugeMaterialInstance;
            return true;
        }

        private void ApplyGaugeStyle(ArtworkEntry entry)
        {
            Rect rect = entry.GaugeRect.width > 0f && entry.GaugeRect.height > 0f
                ? entry.GaugeRect
                : DefaultGaugeRect;
            Color trackColor = entry.TrackColor.a > 0f
                ? entry.TrackColor
                : DefaultTrackColor;
            Color fillColor = entry.FillColor.a > 0f
                ? entry.FillColor
                : DefaultFillColor;

            gaugeMaterialInstance.SetVector(
                GaugeRectId,
                new Vector4(rect.x, rect.y, rect.width, rect.height));
            gaugeMaterialInstance.SetColor(TrackColorId, trackColor);
            gaugeMaterialInstance.SetColor(FillColorId, fillColor);
        }

        private bool TryFindArtworkEntry(
            ExplorationLocationData location,
            out ArtworkEntry matchingEntry)
        {
            if (location != null && artworkEntries != null)
            {
                foreach (ArtworkEntry entry in artworkEntries)
                {
                    if (entry.Location != location) continue;
                    matchingEntry = entry;
                    return true;
                }
            }

            matchingEntry = default;
            return false;
        }
    }
}
