#if UNITY_EDITOR
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using YesterdayMap.Scavenge;

namespace YesterdayMap.Editor
{
    // Manual installer: opening the project never changes a scene.
    public static class ScavengeGuidanceSceneInstaller
    {
        private const string ScenePath = "Assets/_Project/Scenes/Scavenge.unity";
        private const string PanelName = "ScavengeGuidancePanel";
        private const string InitialMessage = "벙커 입구에서 [E] 물자 반입 · [F] 벙커 진입";

        [MenuItem("Yesterday Map/Install Scavenge Guidance Panel")]
        public static void Install()
        {
            if (UpdateLoadedScene() == null) return;
            EditorSceneManager.SaveScene(SceneManager.GetActiveScene());
        }

        // Applies only to the active Scavenge scene; the caller chooses when to save.
        public static Text UpdateLoadedScene()
        {
            Scene scene = SceneManager.GetActiveScene();
            if (EditorApplication.isPlayingOrWillChangePlaymode || scene.path != ScenePath)
            {
                Debug.LogWarning("Open Scavenge.unity and stop Play Mode before installing its guidance panel.");
                return null;
            }

            ScavengeManager manager = null;
            foreach (GameObject root in scene.GetRootGameObjects())
            {
                manager = root.GetComponentInChildren<ScavengeManager>(true);
                if (manager != null) break;
            }

            Text message = manager != null
                ? new SerializedObject(manager).FindProperty("messageText").objectReferenceValue as Text
                : null;
            Canvas canvas = message != null ? message.GetComponentInParent<Canvas>() : null;
            if (canvas == null)
            {
                Debug.LogError("Scavenge requires its existing message Text and Canvas before guidance installation.");
                return null;
            }

            Text result = CreateGuidance(canvas.transform, message);
            EditorSceneManager.MarkSceneDirty(scene);
            return result;
        }

        public static Text CreateGuidance(Transform canvas, Text existingMessage = null)
        {
            if (canvas == null) return null;

            Transform existingPanel = canvas.Find(PanelName);
            GameObject panel = existingPanel != null
                ? existingPanel.gameObject
                : new GameObject(PanelName, typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
            panel.transform.SetParent(canvas, false);
            RectTransform panelRect = panel.GetComponent<RectTransform>();
            SetRect(panelRect, new Vector2(0.55f, 0.80f), new Vector2(0.98f, 0.96f), 0f);
            Image background = panel.GetComponent<Image>();
            if (background == null) background = panel.AddComponent<Image>();
            background.color = new Color(0.075f, 0.055f, 0.04f, 0.88f);
            background.raycastTarget = false;

            Text title = GetOrCreateText(panel.transform, "ScavengeGoal");
            StyleText(title, 28, FontStyle.Bold, new Color(0.94f, 0.86f, 0.66f, 1f));
            SetRect(title.rectTransform, new Vector2(0f, 0.48f), new Vector2(1f, 0.88f), 24f);
            title.text = "물자를 챙겨 벙커로 돌아가기";

            if (existingMessage == null)
            {
                foreach (Text candidate in canvas.GetComponentsInChildren<Text>(true))
                {
                    if (candidate.name != "ScavengeMessage") continue;
                    existingMessage = candidate;
                    break;
                }
            }

            Text message = existingMessage != null
                ? existingMessage
                : GetOrCreateText(panel.transform, "ScavengeMessage");
            if (message.transform.parent == canvas)
                panel.transform.SetSiblingIndex(message.transform.GetSiblingIndex());
            message.transform.SetParent(panel.transform, false);
            StyleText(message, 24, FontStyle.Normal, new Color(0.96f, 0.94f, 0.89f, 1f));
            SetRect(message.rectTransform, new Vector2(0f, 0.10f), new Vector2(1f, 0.50f), 24f);
            if (string.IsNullOrEmpty(message.text)) message.text = InitialMessage;
            return message;
        }

        private static Text GetOrCreateText(Transform parent, string name)
        {
            Transform existing = parent.Find(name);
            if (existing != null && existing.TryGetComponent(out Text text)) return text;
            GameObject child = new(name, typeof(RectTransform), typeof(CanvasRenderer), typeof(Text));
            child.transform.SetParent(parent, false);
            return child.GetComponent<Text>();
        }

        private static void StyleText(Text text, int size, FontStyle style, Color color)
        {
            text.font = YesterdayMap.EditorTools.EditorUIFont.Default;
            text.fontSize = size;
            text.fontStyle = style;
            text.alignment = TextAnchor.MiddleLeft;
            text.color = color;
            text.raycastTarget = false;
            text.resizeTextForBestFit = false;
            text.horizontalOverflow = HorizontalWrapMode.Wrap;
            text.verticalOverflow = VerticalWrapMode.Truncate;
        }

        private static void SetRect(RectTransform rect, Vector2 min, Vector2 max, float padding)
        {
            rect.anchorMin = min;
            rect.anchorMax = max;
            rect.offsetMin = new Vector2(padding, 0f);
            rect.offsetMax = new Vector2(-padding, 0f);
            rect.localScale = Vector3.one;
        }
    }
}
#endif
