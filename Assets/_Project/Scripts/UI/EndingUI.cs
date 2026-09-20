using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace YesterdayMap.UI
{
    public sealed class EndingUI : MonoBehaviour
    {
        [SerializeField] private GameObject panel;
        [SerializeField] private Text titleText;
        [SerializeField] private Text detailText;
        [SerializeField] private Button returnToMainMenuButton;
        public void Configure(GameObject root, Text title, Text detail) { panel = root; titleText = title; detailText = detail; }
        public void ShowGameOver(string reason) { Show("마지막 생존 기록", string.Empty); }
        public void ShowEnding(string message) { Show(message, string.Empty); }
        public void ShowEnding(string title, string detail) { Show(title, string.Empty); }
        private void Show(string title, string detail)
        {
            EnsureReturnToMainMenuButton();
            if (panel != null)
            {
                if (panel.TryGetComponent(out RectTransform panelRect))
                {
                    panelRect.anchorMin = Vector2.zero;
                    panelRect.anchorMax = Vector2.one;
                    panelRect.offsetMin = Vector2.zero;
                    panelRect.offsetMax = Vector2.zero;
                    panelRect.localScale = Vector3.one;
                }

                if (panel.TryGetComponent(out Image background))
                {
                    background.color = Color.black;
                    background.raycastTarget = true;
                }

                panel.SetActive(true);
                panel.transform.SetAsLastSibling();
            }

            if (titleText != null)
            {
                RectTransform titleRect = titleText.rectTransform;
                titleRect.anchorMin = new Vector2(0.1f, 0.35f);
                titleRect.anchorMax = new Vector2(0.9f, 0.65f);
                titleRect.offsetMin = Vector2.zero;
                titleRect.offsetMax = Vector2.zero;
                titleText.alignment = TextAnchor.MiddleCenter;
                titleText.fontSize = 64;
                titleText.fontStyle = FontStyle.Bold;
                titleText.color = Color.white;
                titleText.text = title;
            }

            if (detailText != null)
            {
                bool hasDetail = !string.IsNullOrWhiteSpace(detail);
                detailText.gameObject.SetActive(hasDetail);
                RectTransform detailRect = detailText.rectTransform;
                detailRect.anchorMin = new Vector2(0.08f, 0.22f);
                detailRect.anchorMax = new Vector2(0.92f, 0.52f);
                detailRect.offsetMin = Vector2.zero;
                detailRect.offsetMax = Vector2.zero;
                detailText.text = hasDetail ? detail : string.Empty;
            }

            if (returnToMainMenuButton != null)
                returnToMainMenuButton.gameObject.SetActive(true);
        }

        private void EnsureReturnToMainMenuButton()
        {
            if (panel == null)
                return;

            if (returnToMainMenuButton == null)
            {
                Transform existing = panel.transform.Find("ReturnToMainMenuButton");
                if (existing != null)
                    returnToMainMenuButton = existing.GetComponent<Button>();
            }

            if (returnToMainMenuButton == null)
            {
                GameObject buttonObject = new(
                    "ReturnToMainMenuButton",
                    typeof(RectTransform),
                    typeof(CanvasRenderer),
                    typeof(Image),
                    typeof(Button));
                buttonObject.transform.SetParent(panel.transform, false);

                RectTransform buttonRect =
                    buttonObject.GetComponent<RectTransform>();
                buttonRect.anchorMin = new Vector2(0.34f, 0.08f);
                buttonRect.anchorMax = new Vector2(0.66f, 0.2f);
                buttonRect.offsetMin = Vector2.zero;
                buttonRect.offsetMax = Vector2.zero;

                Image buttonImage = buttonObject.GetComponent<Image>();
                buttonImage.color = new Color(0.24f, 0.31f, 0.32f, 1f);

                returnToMainMenuButton = buttonObject.GetComponent<Button>();
                returnToMainMenuButton.targetGraphic = buttonImage;

                GameObject labelObject = new(
                    "Label",
                    typeof(RectTransform),
                    typeof(CanvasRenderer),
                    typeof(Text));
                labelObject.transform.SetParent(buttonObject.transform, false);
                RectTransform labelRect =
                    labelObject.GetComponent<RectTransform>();
                labelRect.anchorMin = Vector2.zero;
                labelRect.anchorMax = Vector2.one;
                labelRect.offsetMin = new Vector2(10f, 4f);
                labelRect.offsetMax = new Vector2(-10f, -4f);

                Text label = labelObject.GetComponent<Text>();
                label.text = "메인 메뉴로 돌아가기";
                label.alignment = TextAnchor.MiddleCenter;
                label.fontSize = 24;
                label.color = Color.white;
                label.raycastTarget = false;
                if (titleText != null)
                    label.font = titleText.font;
            }

            returnToMainMenuButton.onClick.RemoveListener(ReturnToMainMenu);
            returnToMainMenuButton.onClick.AddListener(ReturnToMainMenu);
        }

        private static void ReturnToMainMenu()
        {
            Time.timeScale = 1f;
            Cursor.lockState = CursorLockMode.None;
            Cursor.visible = true;
            SceneManager.LoadScene("MainMenu");
        }

        private void OnDestroy()
        {
            if (returnToMainMenuButton != null)
                returnToMainMenuButton.onClick.RemoveListener(ReturnToMainMenu);
        }

        private static string TranslateReason(string reason) => reason switch
        {
            "Starvation" => "굶주림",
            "Dehydration" => "탈수",
            "FatalInjury" => "치명상",
            "Zombie attack" => "좀비 습격",
            "Exploration injury" => "탐사 중 부상",
            _ => reason
        };
    }
}
