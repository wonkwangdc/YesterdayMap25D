using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

/// <summary>
/// 메뉴 버튼 위에 마우스를 올리거나 키보드/패드로 선택했을 때
/// 색상과 크기를 아주 작게 변화시켜 선택감을 줍니다.
/// </summary>
[DisallowMultipleComponent]
public sealed class MainMenuButtonHover : MonoBehaviour,
    IPointerEnterHandler,
    IPointerExitHandler,
    ISelectHandler,
    IDeselectHandler
{
    [SerializeField] private Graphic targetGraphic;
    [SerializeField] private Color normalColor = new Color(0.075f, 0.085f, 0.095f, 0.96f);
    [SerializeField] private Color hoverColor = new Color(0.32f, 0.095f, 0.075f, 0.98f);
    [SerializeField, Range(1f, 1.08f)] private float hoverScale = 1.025f;
    
    [SerializeField] private bool selectOnStart;
[SerializeField, Min(0.01f)] private float transitionSpeed = 10f;

    private Vector3 baseScale;
    private bool pointerInside;
    private bool navigationSelected;

    private void Awake()
    {
        if (targetGraphic == null)
        {
            targetGraphic = GetComponent<Graphic>();
        }

        baseScale = transform.localScale;
    }

    private void OnEnable()
    {
        baseScale = transform.localScale;
        ApplyImmediate(false);
    }

private void Start()
    {
        if (selectOnStart && EventSystem.current != null)
        {
            EventSystem.current.SetSelectedGameObject(gameObject);
        }
    }


    private void Update()
    {
        bool highlighted = pointerInside || navigationSelected;
        float t = 1f - Mathf.Exp(-transitionSpeed * Time.unscaledDeltaTime);

        transform.localScale = Vector3.Lerp(
            transform.localScale,
            highlighted ? baseScale * hoverScale : baseScale,
            t);

        if (targetGraphic != null)
        {
            targetGraphic.color = Color.Lerp(
                targetGraphic.color,
                highlighted ? hoverColor : normalColor,
                t);
        }
    }

public void OnPointerEnter(PointerEventData eventData)
    {
        pointerInside = true;

        // 마우스 호버와 키보드/패드 선택 상태를 하나로 맞춥니다.
        if (EventSystem.current != null)
        {
            EventSystem.current.SetSelectedGameObject(gameObject, eventData);
        }
    }

    public void OnPointerExit(PointerEventData eventData)
    {
        pointerInside = false;
    }

    public void OnSelect(BaseEventData eventData)
    {
        navigationSelected = true;
    }

    public void OnDeselect(BaseEventData eventData)
    {
        navigationSelected = false;
    }

    private void OnDisable()
    {
        pointerInside = false;
        navigationSelected = false;
        ApplyImmediate(false);
    }

    private void ApplyImmediate(bool highlighted)
    {
        transform.localScale = highlighted ? baseScale * hoverScale : baseScale;

        if (targetGraphic != null)
        {
            targetGraphic.color = highlighted ? hoverColor : normalColor;
        }
    }
}
