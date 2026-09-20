using UnityEngine;
using UnityEngine.UI;

namespace YesterdayMap.UI
{
    // 남은 시간을 12시 방향에서 시작해 시계방향으로 빨갛게 차오르는 원형 게이지로 보여준다.
    public sealed class RadialTimerUI : MonoBehaviour
    {
        private const string ClockArtworkResourcePath =
            "UI/Scavenge/timer_roman";
        private const string ClockHandResourcePath =
            "UI/Scavenge/timer_hand";

        [SerializeField] private Image bezelImage;
        [SerializeField] private Image faceImage;
        [SerializeField] private Image fillImage;
        [SerializeField] private Text secondsText;
        [SerializeField] private RectTransform handTransform;

        private static Sprite circleSprite;
        private static Sprite solidSprite;
        private Sprite clockHandSprite;

        public void Configure(Image bezel, Image face, Image fill, Text seconds)
        { bezelImage = bezel; faceImage = face; fillImage = fill; secondsText = seconds; }

        private void Awake()
        {
            if (faceImage == null || fillImage == null)
                return;

            ApplyClockArtwork();
            Sprite sprite = GetCircleSprite();

            // 외부에서 시계 테두리 일러스트가 연결되어 있으면 그대로 사용한다.
            // 연결된 이미지가 없을 때만 코드로 만든 단색 원을 대체 이미지로 사용한다.
            if (bezelImage.sprite == null)
            {
                bezelImage.sprite = sprite;
            }

            faceImage.sprite = sprite;
            fillImage.sprite = sprite;
            fillImage.type = Image.Type.Filled;
            fillImage.fillMethod = Image.FillMethod.Radial360;
            fillImage.fillOrigin = (int)Image.Origin360.Top;
            fillImage.fillClockwise = true;
            fillImage.fillAmount = 0f;
            fillImage.color = new Color(0.62f, 0.10f, 0.07f, 0.76f);

            EnsureClockDecoration();
            SetProgress(0f);
        }

        private void ApplyClockArtwork()
        {
            Sprite artwork = UnityEngine.Resources.Load<Sprite>(
                ClockArtworkResourcePath);
            clockHandSprite = UnityEngine.Resources.Load<Sprite>(
                ClockHandResourcePath);

            if (bezelImage == null || artwork == null)
            {
                return;
            }

            bezelImage.sprite = artwork;
            bezelImage.type = Image.Type.Simple;
            bezelImage.preserveAspect = true;
            bezelImage.color = Color.white;
            StretchToParent(bezelImage.rectTransform);
        }

        public void Refresh(float remainingSeconds, float totalSeconds)
        {
            float progress = totalSeconds > 0f
                ? 1f - Mathf.Clamp01(remainingSeconds / totalSeconds)
                : 1f;
            SetProgress(progress);
            if (secondsText != null) secondsText.text = Mathf.CeilToInt(Mathf.Max(0f, remainingSeconds)).ToString();
        }

        private void SetProgress(float progress)
        {
            float normalizedProgress = Mathf.Clamp01(progress);
            if (fillImage != null)
                fillImage.fillAmount = normalizedProgress;
            if (handTransform != null)
                handTransform.localRotation = Quaternion.Euler(
                    0f,
                    0f,
                    -360f * normalizedProgress);
        }

        private void EnsureClockDecoration()
        {
            RectTransform face = faceImage.rectTransform;
            float diameter = Mathf.Min(face.rect.width, face.rect.height);
            if (diameter < 1f)
                diameter = Mathf.Min(face.sizeDelta.x, face.sizeDelta.y);
            diameter = Mathf.Max(160f, diameter);

            Transform oldMarks = face.Find("ClockMarks");
            if (oldMarks != null)
            {
                oldMarks.gameObject.SetActive(false);
            }

            RectTransform handShadow = GetOrCreateImage(
                face,
                "ClockHandShadow",
                out Image handShadowImage);
            ConfigureHand(
                handShadow,
                handShadowImage,
                diameter,
                7f,
                new Color(0.05f, 0.02f, 0.01f, 0.44f),
                new Vector2(1.5f, -1.5f),
                clockHandSprite);
            handShadow.SetAsLastSibling();

            handTransform = GetOrCreateImage(
                face,
                "ClockHand",
                out Image handImage);
            ConfigureHand(
                handTransform,
                handImage,
                diameter,
                4f,
                clockHandSprite != null
                    ? Color.white
                    : new Color(0.36f, 0.045f, 0.025f, 1f),
                Vector2.zero,
                clockHandSprite);
            handTransform.SetAsLastSibling();

            if (clockHandSprite == null)
            {
                RectTransform handHighlight = GetOrCreateImage(
                    handTransform,
                    "HandHighlight",
                    out Image handHighlightImage);
                handHighlight.anchorMin = new Vector2(0.5f, 0.12f);
                handHighlight.anchorMax = new Vector2(0.5f, 0.96f);
                handHighlight.pivot = new Vector2(0.5f, 0.5f);
                handHighlight.anchoredPosition = new Vector2(-0.8f, 0f);
                handHighlight.sizeDelta = new Vector2(1f, 0f);
                handHighlight.localRotation = Quaternion.identity;
                handHighlightImage.sprite = GetSolidSprite();
                handHighlightImage.color =
                    new Color(1f, 0.55f, 0.32f, 0.42f);
            }

            if (secondsText != null)
            {
                RectTransform secondsRect = secondsText.rectTransform;
                secondsRect.anchorMin = new Vector2(0.24f, 0.18f);
                secondsRect.anchorMax = new Vector2(0.76f, 0.43f);
                secondsRect.offsetMin = Vector2.zero;
                secondsRect.offsetMax = Vector2.zero;
                secondsText.alignment = TextAnchor.MiddleCenter;
                secondsText.fontSize = 34;
                secondsText.fontStyle = FontStyle.Bold;
                secondsText.color = new Color(0.20f, 0.11f, 0.065f, 0.96f);
                Shadow textShadow = secondsText.GetComponent<Shadow>();
                if (textShadow == null)
                    textShadow = secondsText.gameObject.AddComponent<Shadow>();
                textShadow.effectColor = new Color(1f, 0.82f, 0.56f, 0.35f);
                textShadow.effectDistance = new Vector2(1f, -1f);
                secondsRect.SetAsLastSibling();
            }

            if (clockHandSprite == null)
            {
                RectTransform hubOuter = GetOrCreateImage(
                    face,
                    "ClockHubOuter",
                    out Image hubOuterImage);
                hubOuter.anchorMin = hubOuter.anchorMax =
                    new Vector2(0.5f, 0.5f);
                hubOuter.pivot = new Vector2(0.5f, 0.5f);
                hubOuter.anchoredPosition = Vector2.zero;
                hubOuter.sizeDelta = new Vector2(18f, 18f);
                hubOuter.localRotation = Quaternion.identity;
                hubOuterImage.sprite = GetCircleSprite();
                hubOuterImage.color =
                    new Color(0.16f, 0.065f, 0.025f, 1f);
                hubOuter.SetAsLastSibling();

                RectTransform hubInner = GetOrCreateImage(
                    hubOuter,
                    "ClockHubInner",
                    out Image hubInnerImage);
                hubInner.anchorMin = new Vector2(0.23f, 0.23f);
                hubInner.anchorMax = new Vector2(0.77f, 0.77f);
                hubInner.offsetMin = Vector2.zero;
                hubInner.offsetMax = Vector2.zero;
                hubInner.localRotation = Quaternion.identity;
                hubInnerImage.sprite = GetCircleSprite();
                hubInnerImage.color =
                    new Color(0.83f, 0.53f, 0.22f, 1f);
            }
        }

        private static void ConfigureHand(
            RectTransform hand,
            Image image,
            float diameter,
            float width,
            Color color,
            Vector2 offset,
            Sprite customSprite)
        {
            hand.anchorMin = hand.anchorMax = new Vector2(0.5f, 0.5f);
            hand.pivot = customSprite != null
                ? new Vector2(0.5f, 0.215f)
                : new Vector2(0.5f, 0.10f);
            hand.anchoredPosition = offset;
            hand.sizeDelta = customSprite != null
                ? new Vector2(diameter * 0.28f, diameter * 0.56f)
                : new Vector2(width, diameter * 0.39f);
            image.sprite = customSprite != null
                ? customSprite
                : GetSolidSprite();
            image.type = Image.Type.Simple;
            image.preserveAspect = customSprite != null;
            image.color = color;
        }

        private static RectTransform GetOrCreateRect(
            Transform parent,
            string objectName)
        {
            Transform existing = parent.Find(objectName);
            if (existing is RectTransform existingRect)
                return existingRect;

            GameObject gameObject = new(objectName, typeof(RectTransform));
            gameObject.transform.SetParent(parent, false);
            return (RectTransform)gameObject.transform;
        }

        private static RectTransform GetOrCreateImage(
            Transform parent,
            string objectName,
            out Image image)
        {
            RectTransform rect = GetOrCreateRect(parent, objectName);
            image = rect.GetComponent<Image>();
            if (image == null)
            {
                rect.gameObject.AddComponent<CanvasRenderer>();
                image = rect.gameObject.AddComponent<Image>();
            }
            image.raycastTarget = false;
            return rect;
        }

        private static void StretchToParent(RectTransform rect)
        {
            rect.anchorMin = Vector2.zero;
            rect.anchorMax = Vector2.one;
            rect.offsetMin = Vector2.zero;
            rect.offsetMax = Vector2.zero;
            rect.localRotation = Quaternion.identity;
            rect.localScale = Vector3.one;
        }

        // 색만 입혀서 재사용할 수 있는 단색 원 스프라이트를 절차적으로 한 번 생성해 공유한다.
        private static Sprite GetCircleSprite()
        {
            if (circleSprite != null) return circleSprite;

            const int size = 128;
            Texture2D texture = new(size, size, TextureFormat.RGBA32, false)
            {
                wrapMode = TextureWrapMode.Clamp,
                filterMode = FilterMode.Bilinear
            };

            Vector2 center = new((size - 1) * 0.5f, (size - 1) * 0.5f);
            float radius = size * 0.5f - 1f;
            for (int y = 0; y < size; y++)
            {
                for (int x = 0; x < size; x++)
                {
                    float distance = Vector2.Distance(new Vector2(x, y), center);
                    float alpha = Mathf.Clamp01(radius - distance + 1f); // 가장자리 1px를 부드럽게 처리해 계단현상을 줄인다.
                    texture.SetPixel(x, y, new Color(1f, 1f, 1f, alpha));
                }
            }
            texture.Apply();

            circleSprite = Sprite.Create(texture, new Rect(0, 0, size, size), new Vector2(0.5f, 0.5f));
            return circleSprite;
        }

        private static Sprite GetSolidSprite()
        {
            if (solidSprite != null) return solidSprite;

            Texture2D texture = new(1, 1, TextureFormat.RGBA32, false)
            {
                wrapMode = TextureWrapMode.Clamp,
                filterMode = FilterMode.Point
            };
            texture.SetPixel(0, 0, Color.white);
            texture.Apply();
            solidSprite = Sprite.Create(
                texture,
                new Rect(0, 0, 1, 1),
                new Vector2(0.5f, 0.5f));
            return solidSprite;
        }
    }
}
