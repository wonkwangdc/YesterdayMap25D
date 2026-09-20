using UnityEngine;
using UnityEngine.UI;
using YesterdayMap.Character;

namespace YesterdayMap.UI
{
    /// <summary>
    /// Shows a compact HUD card while the player logically carries a fuel can.
    /// No 3D fuel model is parented to or moved with the player.
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class FuelCanCarryIndicator : MonoBehaviour
    {
        [Header("HUD Layout")]
        [SerializeField] private Vector2 size = new(340f, 82f);
        [SerializeField] private Vector2 anchoredPosition = new(0f, 34f);

        [Header("Colors")]
        [SerializeField] private Color panelColor =
            new(0.055f, 0.047f, 0.04f, 0.9f);
        [SerializeField] private Color borderColor =
            new(0.58f, 0.45f, 0.28f, 0.8f);
        [SerializeField] private Color fuelCanColor =
            new(0.52f, 0.11f, 0.08f, 1f);
        [SerializeField] private Color titleColor =
            new(0.94f, 0.9f, 0.8f, 1f);
        [SerializeField] private Color hintColor =
            new(0.76f, 0.68f, 0.55f, 1f);

        private PlayerCarryController carrier;
        private GameObject indicatorRoot;
        private bool lastVisible;

        public bool IsShowing =>
            indicatorRoot != null && indicatorRoot.activeSelf;

        private void Awake()
        {
            BuildIndicator();
            RefreshVisibility(true);
        }

        private void Update()
        {
            RefreshVisibility(false);
        }

        private void RefreshVisibility(bool force)
        {
            if (carrier == null)
            {
                carrier =
                    FindFirstObjectByType<PlayerCarryController>();
            }

            bool visible = carrier != null && carrier.HasFuelCan;
            if (!force && visible == lastVisible)
            {
                return;
            }

            lastVisible = visible;
            if (indicatorRoot != null)
            {
                indicatorRoot.SetActive(visible);
            }
        }

        private void BuildIndicator()
        {
            Transform existing =
                transform.Find("FuelCanCarryIndicatorPanel");
            if (existing != null)
            {
                indicatorRoot = existing.gameObject;
                return;
            }

            indicatorRoot = CreateImageObject(
                transform,
                "FuelCanCarryIndicatorPanel",
                panelColor);
            RectTransform panel =
                (RectTransform)indicatorRoot.transform;
            panel.anchorMin = new Vector2(0.5f, 0f);
            panel.anchorMax = new Vector2(0.5f, 0f);
            panel.pivot = new Vector2(0.5f, 0f);
            panel.sizeDelta = size;
            panel.anchoredPosition = anchoredPosition;

            CreateBorder(panel, "TopBorder",
                new Vector2(0f, size.y * 0.5f - 1.5f),
                new Vector2(size.x, 3f));
            CreateBorder(panel, "BottomBorder",
                new Vector2(0f, -size.y * 0.5f + 1.5f),
                new Vector2(size.x, 3f));
            CreateBorder(panel, "LeftBorder",
                new Vector2(-size.x * 0.5f + 1.5f, 0f),
                new Vector2(3f, size.y));
            CreateBorder(panel, "RightBorder",
                new Vector2(size.x * 0.5f - 1.5f, 0f),
                new Vector2(3f, size.y));

            BuildFuelCanIcon(panel);
            Font koreanFont = Font.CreateDynamicFontFromOSFont(
                new[]
                {
                    "Malgun Gothic",
                    "맑은 고딕",
                    "Arial Unicode MS"
                },
                24);

            CreateLabel(
                panel,
                "Title",
                "연료통 보유",
                new Vector2(42f, 13f),
                new Vector2(210f, 30f),
                22,
                titleColor,
                koreanFont);
            CreateLabel(
                panel,
                "Hint",
                "발전기 앞에서 [F] 연료 넣기",
                new Vector2(64f, -17f),
                new Vector2(254f, 28f),
                16,
                hintColor,
                koreanFont);
        }

        private void BuildFuelCanIcon(RectTransform panel)
        {
            Image body = CreateShape(
                panel,
                "FuelCanBody",
                new Vector2(-117f, -2f),
                new Vector2(48f, 54f),
                fuelCanColor);

            CreateShape(
                body.rectTransform,
                "FuelCanInset",
                new Vector2(2f, 0f),
                new Vector2(23f, 28f),
                new Color(0.09f, 0.07f, 0.055f, 0.72f));
            CreateShape(
                panel,
                "FuelCanCap",
                new Vector2(-104f, 31f),
                new Vector2(18f, 8f),
                fuelCanColor);
            CreateShape(
                panel,
                "FuelCanHandleTop",
                new Vector2(-125f, 25f),
                new Vector2(25f, 7f),
                fuelCanColor);
            CreateShape(
                panel,
                "FuelCanHandleCutout",
                new Vector2(-124f, 23f),
                new Vector2(12f, 5f),
                panelColor);
        }

        private void CreateBorder(
            RectTransform parent,
            string name,
            Vector2 position,
            Vector2 borderSize)
        {
            CreateShape(
                parent,
                name,
                position,
                borderSize,
                borderColor);
        }

        private static Image CreateShape(
            RectTransform parent,
            string name,
            Vector2 position,
            Vector2 shapeSize,
            Color color)
        {
            GameObject shape =
                CreateImageObject(parent, name, color);
            RectTransform rect = (RectTransform)shape.transform;
            rect.anchorMin = new Vector2(0.5f, 0.5f);
            rect.anchorMax = new Vector2(0.5f, 0.5f);
            rect.pivot = new Vector2(0.5f, 0.5f);
            rect.sizeDelta = shapeSize;
            rect.anchoredPosition = position;
            return shape.GetComponent<Image>();
        }

        private static void CreateLabel(
            RectTransform parent,
            string name,
            string value,
            Vector2 position,
            Vector2 labelSize,
            int fontSize,
            Color color,
            Font font)
        {
            GameObject label = new(
                name,
                typeof(RectTransform),
                typeof(CanvasRenderer),
                typeof(Text));
            label.transform.SetParent(parent, false);

            RectTransform rect = (RectTransform)label.transform;
            rect.anchorMin = new Vector2(0.5f, 0.5f);
            rect.anchorMax = new Vector2(0.5f, 0.5f);
            rect.pivot = new Vector2(0.5f, 0.5f);
            rect.sizeDelta = labelSize;
            rect.anchoredPosition = position;

            Text text = label.GetComponent<Text>();
            text.text = value;
            text.font = font != null
                ? font
                : UnityEngine.Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            text.fontSize = fontSize;
            text.fontStyle = FontStyle.Bold;
            text.alignment = TextAnchor.MiddleLeft;
            text.color = color;
            text.raycastTarget = false;
            text.horizontalOverflow = HorizontalWrapMode.Overflow;
            text.verticalOverflow = VerticalWrapMode.Overflow;
        }

        private static GameObject CreateImageObject(
            Transform parent,
            string name,
            Color color)
        {
            GameObject imageObject = new(
                name,
                typeof(RectTransform),
                typeof(CanvasRenderer),
                typeof(Image));
            imageObject.transform.SetParent(parent, false);
            Image image = imageObject.GetComponent<Image>();
            image.color = color;
            image.raycastTarget = false;
            return imageObject;
        }
    }
}
