using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// RawImage의 UV 영역을 매 프레임 이동시켜 반복되는 안개 텍스처가 흐르도록 합니다.
/// 텍스처 Import Settings의 Wrap Mode가 Repeat여야 경계 없이 반복됩니다.
/// </summary>
[RequireComponent(typeof(RawImage))]
[DisallowMultipleComponent]
public sealed class RawImageUVScroller : MonoBehaviour
{
    [Tooltip("초당 이동할 UV 좌표입니다. 매우 작은 값(예: 0.003, 0.001)을 권장합니다.")]
    [SerializeField] private Vector2 scrollSpeed = new Vector2(0.003f, 0.001f);

    private RawImage targetImage;

    private void Awake()
    {
        targetImage = GetComponent<RawImage>();

        if (targetImage == null)
        {
            Debug.LogWarning($"{nameof(RawImageUVScroller)} requires a RawImage.", this);
            enabled = false;
        }
    }

    private void Update()
    {
        if (targetImage == null)
        {
            return;
        }

        Rect uv = targetImage.uvRect;
        Vector2 nextPosition = uv.position + scrollSpeed * Time.unscaledDeltaTime;

        // 값이 끝없이 커지는 것을 막으면서 Repeat 래핑은 그대로 유지합니다.
        nextPosition.x = Mathf.Repeat(nextPosition.x, 1f);
        nextPosition.y = Mathf.Repeat(nextPosition.y, 1f);

        uv.position = nextPosition;
        targetImage.uvRect = uv;
    }
}
