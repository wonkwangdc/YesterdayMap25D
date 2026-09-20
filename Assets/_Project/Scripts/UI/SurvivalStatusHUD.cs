using System.Collections;
using UnityEngine;
using UnityEngine.UI;
using YesterdayMap.Character;

namespace YesterdayMap.UI
{
    /// <summary>
    /// Displays the four independent survival stats without owning their rules.
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class SurvivalStatusHUD : MonoBehaviour
    {
        [SerializeField] private CharacterStats stats;
        [SerializeField] private Image healthFill;
        [SerializeField] private Image hungerFill;
        [SerializeField] private Image thirstFill;
        [SerializeField] private Image moraleFill;

        private const float RecoveryGaugeDuration = 3f;
        private const float GaugeComparisonEpsilon = 0.0001f;
        private const float WarningThreshold = 0.5f;
        private const float CriticalThreshold = 0.2f;
        private const float WarningPulseSpeed = 0.8f;
        private static readonly Color WarningColor =
            new(1f, 0.42f, 0.06f, 1f);
        private static readonly Color CriticalColor =
            new(1f, 0.08f, 0.035f, 1f);
        private static readonly Color DepletedBrightColor =
            new(1f, 0.015f, 0.01f, 1f);

        private Coroutine gaugeAnimation;
        private bool hasGaugeState;
        private bool hasBaseColors;
        private Color healthBaseColor;
        private Color hungerBaseColor;
        private Color thirstBaseColor;
        private Color moraleBaseColor;
        private CircularStatusWarningGraphic healthWarningRing;
        private CircularStatusWarningGraphic hungerWarningRing;
        private CircularStatusWarningGraphic thirstWarningRing;
        private CircularStatusWarningGraphic moraleWarningRing;

        private bool subscribed;

        public void Configure(
            Image healthGauge,
            Image hungerGauge,
            Image thirstGauge,
            Image moraleGauge)
        {
            RestoreBaseColors();
            DestroyWarningRings();
            StopGaugeAnimation();
            hasGaugeState = false;
            healthFill = healthGauge;
            hungerFill = hungerGauge;
            thirstFill = thirstGauge;
            moraleFill = moraleGauge;
            hasBaseColors = false;
            CaptureBaseColors();
            EnsureWarningRings();
            Refresh();
        }

        public void Bind(CharacterStats source)
        {
            if (stats != source)
            {
                Unsubscribe();
                StopGaugeAnimation();
                hasGaugeState = false;
                stats = source;
            }

            Subscribe();
            Refresh();
        }

        private void OnEnable()
        {
            CaptureBaseColors();
            EnsureWarningRings();
            Subscribe();
            Refresh();
        }

        private void Start()
        {
            if (stats == null)
                stats = FindFirstObjectByType<CharacterStats>();

            Subscribe();
            Refresh();
        }

        private void OnDisable()
        {
            Unsubscribe();
            StopGaugeAnimation();
            hasGaugeState = false;
            RestoreBaseColors();
            ClearWarningRings();
        }

        private void Update()
        {
            UpdateWarningVisuals();
        }

        public void Refresh()
        {
            if (stats == null)
            {
                RestoreBaseColors();
                ClearWarningRings();
                return;
            }

            float healthTarget = NormalizeGauge(stats.Health);
            float hungerTarget = NormalizeGauge(stats.Hunger);
            float thirstTarget = NormalizeGauge(stats.Thirst);
            float moraleTarget = NormalizeGauge(stats.Morale);

            SetGaugeNormalized(moraleFill, moraleTarget);
            UpdateWarningVisuals();
            if (!Application.isPlaying || !hasGaugeState)
            {
                StopGaugeAnimation();
                SetGaugeNormalized(healthFill, healthTarget);
                SetGaugeNormalized(hungerFill, hungerTarget);
                SetGaugeNormalized(thirstFill, thirstTarget);
                hasGaugeState = true;
                return;
            }

            float healthStart = GetGaugeValue(healthFill, healthTarget);
            float hungerStart = GetGaugeValue(hungerFill, hungerTarget);
            float thirstStart = GetGaugeValue(thirstFill, thirstTarget);
            bool animateHealth =
                healthTarget > healthStart + GaugeComparisonEpsilon;
            bool animateHunger =
                hungerTarget > hungerStart + GaugeComparisonEpsilon;
            bool animateThirst =
                thirstTarget > thirstStart + GaugeComparisonEpsilon;

            StopGaugeAnimation();
            if (!animateHealth && !animateHunger && !animateThirst)
            {
                SetGaugeNormalized(healthFill, healthTarget);
                SetGaugeNormalized(hungerFill, hungerTarget);
                SetGaugeNormalized(thirstFill, thirstTarget);
                return;
            }

            if (!animateHealth)
            {
                SetGaugeNormalized(healthFill, healthTarget);
            }

            if (!animateHunger)
            {
                SetGaugeNormalized(hungerFill, hungerTarget);
            }

            if (!animateThirst)
            {
                SetGaugeNormalized(thirstFill, thirstTarget);
            }

            gaugeAnimation = StartCoroutine(AnimateRecoveryGauges(
                healthStart,
                healthTarget,
                animateHealth,
                hungerStart,
                hungerTarget,
                animateHunger,
                thirstStart,
                thirstTarget,
                animateThirst));
        }

        private void Subscribe()
        {
            if (!Application.isPlaying || subscribed || stats == null) return;
            stats.StatsChanged += Refresh;
            subscribed = true;
        }

        private void Unsubscribe()
        {
            if (!subscribed || stats == null) return;
            stats.StatsChanged -= Refresh;
            subscribed = false;
        }

        private IEnumerator AnimateRecoveryGauges(
            float healthStart,
            float healthTarget,
            bool animateHealth,
            float hungerStart,
            float hungerTarget,
            bool animateHunger,
            float thirstStart,
            float thirstTarget,
            bool animateThirst)
        {
            float elapsed = 0f;
            while (elapsed < RecoveryGaugeDuration)
            {
                elapsed += Time.unscaledDeltaTime;
                float progress = Mathf.Clamp01(
                    elapsed / RecoveryGaugeDuration);
                float easedProgress = Mathf.SmoothStep(
                    0f,
                    1f,
                    progress);

                if (animateHealth)
                {
                    SetGaugeNormalized(
                        healthFill,
                        Mathf.Lerp(
                            healthStart,
                            healthTarget,
                            easedProgress));
                }

                if (animateHunger)
                {
                    SetGaugeNormalized(
                        hungerFill,
                        Mathf.Lerp(
                            hungerStart,
                            hungerTarget,
                            easedProgress));
                }

                if (animateThirst)
                {
                    SetGaugeNormalized(
                        thirstFill,
                        Mathf.Lerp(
                            thirstStart,
                            thirstTarget,
                            easedProgress));
                }

                yield return null;
            }

            SetGaugeNormalized(healthFill, healthTarget);
            SetGaugeNormalized(hungerFill, hungerTarget);
            SetGaugeNormalized(thirstFill, thirstTarget);
            gaugeAnimation = null;
        }

        private void StopGaugeAnimation()
        {
            if (gaugeAnimation == null)
            {
                return;
            }

            StopCoroutine(gaugeAnimation);
            gaugeAnimation = null;
        }

        private void CaptureBaseColors()
        {
            if (hasBaseColors)
            {
                return;
            }

            healthBaseColor = healthFill != null
                ? healthFill.color
                : Color.white;
            hungerBaseColor = hungerFill != null
                ? hungerFill.color
                : Color.white;
            thirstBaseColor = thirstFill != null
                ? thirstFill.color
                : Color.white;
            moraleBaseColor = moraleFill != null
                ? moraleFill.color
                : Color.white;
            hasBaseColors = true;
        }

        private void RestoreBaseColors()
        {
            if (!hasBaseColors)
            {
                return;
            }

            SetGaugeColor(healthFill, healthBaseColor);
            SetGaugeColor(hungerFill, hungerBaseColor);
            SetGaugeColor(thirstFill, thirstBaseColor);
            SetGaugeColor(moraleFill, moraleBaseColor);
        }

        private void UpdateWarningVisuals()
        {
            if (stats == null)
            {
                ClearWarningRings();
                return;
            }

            CaptureBaseColors();
            EnsureWarningRings();
            RestoreBaseColors();
            ApplyWarningRing(healthWarningRing, stats.Health);
            ApplyWarningRing(hungerWarningRing, stats.Hunger);
            ApplyWarningRing(thirstWarningRing, stats.Thirst);
            ApplyWarningRing(moraleWarningRing, stats.Morale);
        }

        private static void ApplyWarningRing(
            CircularStatusWarningGraphic ring,
            float value)
        {
            if (ring == null)
            {
                return;
            }

            float normalized = NormalizeGauge(value);
            if (normalized > WarningThreshold)
            {
                ring.enabled = false;
                return;
            }

            ring.enabled = true;
            Color warningColor = normalized <= 0f
                ? DepletedBrightColor
                : normalized <= CriticalThreshold
                    ? CriticalColor
                    : WarningColor;

            float pulse = YesterdayMap.Core.GameSettings.ReduceFlashing
                ? 0.72f
                : 0.5f + 0.5f * Mathf.Sin(
                    Time.unscaledTime * WarningPulseSpeed * Mathf.PI * 2f);
            warningColor.a = Mathf.Lerp(
                0.18f,
                1f,
                Mathf.SmoothStep(0f, 1f, pulse));
            ring.color = warningColor;
        }

        private void EnsureWarningRings()
        {
            healthWarningRing = EnsureWarningRing(
                healthFill,
                healthWarningRing,
                "HealthWarningRing");
            hungerWarningRing = EnsureWarningRing(
                hungerFill,
                hungerWarningRing,
                "HungerWarningRing");
            thirstWarningRing = EnsureWarningRing(
                thirstFill,
                thirstWarningRing,
                "ThirstWarningRing");
            moraleWarningRing = EnsureWarningRing(
                moraleFill,
                moraleWarningRing,
                "MoraleWarningRing");
        }

        private static CircularStatusWarningGraphic EnsureWarningRing(
            Image fill,
            CircularStatusWarningGraphic currentRing,
            string ringName)
        {
            if (currentRing != null || fill == null)
            {
                return currentRing;
            }

            RectTransform maskRect = fill.rectTransform.parent
                as RectTransform;
            if (maskRect == null || maskRect.parent == null)
            {
                return null;
            }

            Transform ringParent = maskRect.parent;
            Transform existing = ringParent.Find(ringName);
            if (existing != null &&
                existing.TryGetComponent(
                    out CircularStatusWarningGraphic existingRing))
            {
                return existingRing;
            }

            GameObject ringObject = new(
                ringName,
                typeof(RectTransform),
                typeof(CanvasRenderer),
                typeof(CircularStatusWarningGraphic));
            ringObject.layer = fill.gameObject.layer;
            RectTransform ringRect = ringObject.GetComponent<RectTransform>();
            ringRect.SetParent(ringParent, false);
            CopyRectTransform(maskRect, ringRect);
            ringRect.SetSiblingIndex(maskRect.GetSiblingIndex() + 1);

            CircularStatusWarningGraphic ring =
                ringObject.GetComponent<CircularStatusWarningGraphic>();
            ring.raycastTarget = false;
            ring.maskable = false;
            ring.color = Color.clear;
            ring.enabled = false;
            return ring;
        }

        private static void CopyRectTransform(
            RectTransform source,
            RectTransform destination)
        {
            destination.anchorMin = source.anchorMin;
            destination.anchorMax = source.anchorMax;
            destination.pivot = source.pivot;
            destination.anchoredPosition = source.anchoredPosition;
            destination.sizeDelta = source.sizeDelta;
            destination.localRotation = source.localRotation;
            destination.localScale = source.localScale;
        }



        private void ClearWarningRings()
        {
            ClearWarningRing(healthWarningRing);
            ClearWarningRing(hungerWarningRing);
            ClearWarningRing(thirstWarningRing);
            ClearWarningRing(moraleWarningRing);
        }

        private static void ClearWarningRing(
            CircularStatusWarningGraphic ring)
        {
            if (ring == null)
            {
                return;
            }

            ring.color = Color.clear;
            ring.enabled = false;
        }

        private void DestroyWarningRings()
        {
            DestroyWarningRing(healthWarningRing);
            DestroyWarningRing(hungerWarningRing);
            DestroyWarningRing(thirstWarningRing);
            DestroyWarningRing(moraleWarningRing);
            healthWarningRing = null;
            hungerWarningRing = null;
            thirstWarningRing = null;
            moraleWarningRing = null;
        }

        private static void DestroyWarningRing(
            CircularStatusWarningGraphic ring)
        {
            if (ring == null)
            {
                return;
            }

            if (Application.isPlaying)
            {
                Destroy(ring.gameObject);
            }
            else
            {
                DestroyImmediate(ring.gameObject);
            }
        }

        private static void SetGaugeColor(Image fill, Color color)
        {
            if (fill != null)
            {
                fill.color = color;
            }
        }

        private static float GetGaugeValue(
            Image fill,
            float fallbackValue)
        {
            return fill != null
                ? Mathf.Clamp01(fill.fillAmount)
                : fallbackValue;
        }

        private static float NormalizeGauge(float value)
        {
            return Mathf.Clamp(value, 0f, 100f) / 100f;
        }

        private static void SetGaugeNormalized(
            Image fill,
            float normalizedValue)
        {
            if (fill != null)
            {
                fill.fillAmount = Mathf.Clamp01(normalizedValue);
            }
        }


    }
}
