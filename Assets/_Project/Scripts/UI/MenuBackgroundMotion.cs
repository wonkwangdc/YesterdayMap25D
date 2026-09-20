using UnityEngine;

/// <summary>
/// 메인 메뉴의 배경 한 장에만 아주 느린 자동 이동, 줌, 마우스 패럴랙스를 적용합니다.
/// 메뉴 버튼과 텍스트가 아닌 BackgroundImage 오브젝트에 붙여 사용하세요.
/// </summary>
[DisallowMultipleComponent]
public sealed class MenuBackgroundMotion : MonoBehaviour
{
    [Header("자동 이동")]
    [Tooltip("기준 위치에서 좌우/상하로 움직일 최대 픽셀(또는 월드 단위) 범위입니다.")]
    [SerializeField] private Vector2 moveRange = new Vector2(20f, 12f);

    [Tooltip("자동 이동과 줌의 진행 속도입니다. 아주 작은 값이 자연스럽습니다.")]
    [SerializeField, Min(0f)] private float moveSpeed = 0.06f;

    [Header("느린 줌")]
    [Tooltip("기준 크기에 더해지는 줌 비율입니다. 0.02는 약 2%입니다.")]
    [SerializeField, Range(0f, 0.1f)] private float zoomAmount = 0.02f;

    [Header("마우스 패럴랙스")]
    [SerializeField] private bool useMouseParallax = true;

    [Tooltip("마우스가 화면 가장자리에 있을 때 추가되는 최대 좌우/상하 이동량입니다.")]
    [SerializeField] private Vector2 mouseRange = new Vector2(8f, 5f);

    private const float MouseSmoothTime = 0.35f;
    private const float NoiseSeedX = 17.31f;
    private const float NoiseSeedY = 83.79f;

    private RectTransform rectTransform;
    private Vector2 startAnchoredPosition;
    private Vector3 startLocalPosition;
    private Vector3 startLocalScale;
    private Vector2 smoothedMouseOffset;
    private Vector2 mouseOffsetVelocity;
    private bool initialized;

    private void Awake()
    {
        CacheStartTransform();
    }

    private void OnEnable()
    {
        CacheStartTransform();
    }

    private void CacheStartTransform()
    {
        if (transform == null)
        {
            enabled = false;
            return;
        }

        rectTransform = transform as RectTransform;
        startLocalPosition = transform.localPosition;
        startLocalScale = transform.localScale;

        if (rectTransform != null)
        {
            startAnchoredPosition = rectTransform.anchoredPosition;
        }

        smoothedMouseOffset = Vector2.zero;
        mouseOffsetVelocity = Vector2.zero;
        initialized = true;
    }

    private void Update()
    {
        if (!initialized || transform == null)
        {
            return;
        }

        // unscaledTime을 사용하므로 메뉴에서 timeScale이 0이어도 계속 움직입니다.
        float motionTime = Time.unscaledTime * moveSpeed;

        // 서로 다른 시드의 Perlin Noise를 섞어 반복적인 좌우 흔들림처럼 보이지 않게 합니다.
        Vector2 automaticOffset = new Vector2(
            (Mathf.PerlinNoise(NoiseSeedX, motionTime) - 0.5f) * 2f * moveRange.x,
            (Mathf.PerlinNoise(NoiseSeedY, motionTime) - 0.5f) * 2f * moveRange.y);

        Vector2 targetMouseOffset = useMouseParallax ? GetMouseParallaxOffset() : Vector2.zero;
        smoothedMouseOffset = Vector2.SmoothDamp(
            smoothedMouseOffset,
            targetMouseOffset,
            ref mouseOffsetVelocity,
            MouseSmoothTime,
            Mathf.Infinity,
            Time.unscaledDeltaTime);

        Vector2 combinedOffset = automaticOffset + smoothedMouseOffset;

        if (rectTransform != null)
        {
            rectTransform.anchoredPosition = startAnchoredPosition + combinedOffset;
        }
        else
        {
            transform.localPosition = startLocalPosition + new Vector3(combinedOffset.x, combinedOffset.y, 0f);
        }

        float zoom = 1f + Mathf.Sin(motionTime * Mathf.PI * 0.65f) * zoomAmount;
        transform.localScale = startLocalScale * zoom;
    }

    private Vector2 GetMouseParallaxOffset()
    {
        if (Screen.width <= 0 || Screen.height <= 0)
        {
            return Vector2.zero;
        }

        Vector2 normalizedMouse = new Vector2(
            (Input.mousePosition.x / Screen.width) * 2f - 1f,
            (Input.mousePosition.y / Screen.height) * 2f - 1f);

        normalizedMouse.x = Mathf.Clamp(normalizedMouse.x, -1f, 1f);
        normalizedMouse.y = Mathf.Clamp(normalizedMouse.y, -1f, 1f);

        return Vector2.Scale(normalizedMouse, mouseRange);
    }

    private void OnDisable()
    {
        if (!initialized || transform == null)
        {
            return;
        }

        if (rectTransform != null)
        {
            rectTransform.anchoredPosition = startAnchoredPosition;
        }
        else
        {
            transform.localPosition = startLocalPosition;
        }

        transform.localScale = startLocalScale;
    }

#if UNITY_EDITOR
    private void OnValidate()
    {
        moveRange.x = Mathf.Max(0f, moveRange.x);
        moveRange.y = Mathf.Max(0f, moveRange.y);
        moveSpeed = Mathf.Max(0f, moveSpeed);
        zoomAmount = Mathf.Max(0f, zoomAmount);
        mouseRange.x = Mathf.Max(0f, mouseRange.x);
        mouseRange.y = Mathf.Max(0f, mouseRange.y);
    }
#endif
}
