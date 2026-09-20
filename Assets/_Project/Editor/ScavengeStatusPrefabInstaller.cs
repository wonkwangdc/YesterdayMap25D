#if UNITY_EDITOR
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using YesterdayMap.Scavenge;

namespace YesterdayMap.Editor
{
    /// <summary>
    /// Creates and connects the editable Scavenge status HUD prefab.
    /// Existing prefab styling and scene instances are preserved.
    /// </summary>
    public static class ScavengeStatusPrefabInstaller
    {
        private const string ScenePath = "Assets/_Project/Scenes/Scavenge.unity";
        private const string PrefabPath = "Assets/_Project/Prefabs/UI/ScavengeStatusHUD.prefab";

        [MenuItem("Yesterday Map/Install Scavenge Status Prefab")]
        public static void Install()
        {
            if (EditorApplication.isPlayingOrWillChangePlaymode)
            {
                Debug.LogWarning("Stop Play Mode before installing the Scavenge status prefab.");
                return;
            }

            GameObject prefab = GetOrCreatePrefab();
            if (prefab == null) return;

            Scene scene = SceneManager.GetSceneByPath(ScenePath);
            bool openedTemporarily = !scene.IsValid() || !scene.isLoaded;
            if (openedTemporarily)
                scene = EditorSceneManager.OpenScene(ScenePath, OpenSceneMode.Additive);
            else if (scene.isDirty)
            {
                Debug.LogWarning(
                    "Scavenge.unity has unsaved changes. Save or revert them before installing the status prefab.");
                return;
            }

            ScavengeManager manager = FindInScene<ScavengeManager>(scene);
            Canvas canvas = FindInScene<Canvas>(scene);
            if (manager == null || canvas == null)
            {
                Debug.LogError("Scavenge scene requires a manager and Canvas before status UI installation.");
                if (openedTemporarily) EditorSceneManager.CloseScene(scene, true);
                return;
            }

            AttachToCanvas(canvas.transform, manager, prefab, scene);
            EditorSceneManager.MarkSceneDirty(scene);
            EditorSceneManager.SaveScene(scene);
            AssetDatabase.SaveAssets();

            Debug.Log("Yesterday Map: reusable ScavengeStatusHUD prefab is connected to Scavenge.unity.");
            if (openedTemporarily) EditorSceneManager.CloseScene(scene, true);
        }

        public static GameObject GetOrCreatePrefab()
        {
            GameObject existing = AssetDatabase.LoadAssetAtPath<GameObject>(PrefabPath);
            if (existing != null) return existing;

            Font font = YesterdayMap.EditorTools.EditorUIFont.Default;
            GameObject root = new("ScavengeStatusHUD", typeof(RectTransform), typeof(ScavengeStatusView));
            Stretch(root.GetComponent<RectTransform>());

            GameObject summaryPanel = CreateImage(
                root.transform,
                "DepositedSummaryPanel",
                new Vector2(0.78f, 0.54f),
                new Vector2(0.98f, 0.78f),
                new Color(0.02f, 0.035f, 0.035f, 0.86f));
            Text summaryText = CreateText(
                summaryPanel.transform,
                "DepositedSummaryText",
                string.Empty,
                font,
                22,
                Vector2.zero,
                Vector2.one,
                TextAnchor.UpperLeft);
            summaryText.rectTransform.offsetMin = new Vector2(18f, 14f);
            summaryText.rectTransform.offsetMax = new Vector2(-18f, -14f);
            summaryText.fontStyle = FontStyle.Bold;
            summaryText.color = new Color(0.92f, 0.87f, 0.72f);
            summaryText.horizontalOverflow = HorizontalWrapMode.Wrap;
            summaryText.verticalOverflow = VerticalWrapMode.Truncate;
            summaryPanel.SetActive(false);

            GameObject darknessObject = CreateImage(
                root.transform,
                "TimeExpiryGameOver",
                Vector2.zero,
                Vector2.one,
                Color.white);
            Image darknessOverlay = darknessObject.GetComponent<Image>();
            darknessOverlay.raycastTarget = false;

            GameObject gameOverContent = new("GameOverContent", typeof(RectTransform));
            gameOverContent.transform.SetParent(darknessObject.transform, false);
            Stretch(gameOverContent.GetComponent<RectTransform>());
            Text title = CreateText(
                gameOverContent.transform,
                "GameOverTitle",
                "게임 오버",
                font,
                52,
                new Vector2(0.25f, 0.54f),
                new Vector2(0.75f, 0.68f),
                TextAnchor.MiddleCenter);
            title.fontStyle = FontStyle.Bold;

            GameObject buttonObject = CreateImage(
                gameOverContent.transform,
                "ReturnToMainMenuButton",
                new Vector2(0.38f, 0.38f),
                new Vector2(0.62f, 0.47f),
                new Color(0.72f, 0.16f, 0.12f, 1f));
            Button returnButton = buttonObject.AddComponent<Button>();
            returnButton.targetGraphic = buttonObject.GetComponent<Image>();
            Text buttonText = CreateText(
                buttonObject.transform,
                "Label",
                "메인 메뉴로 돌아가기",
                font,
                24,
                Vector2.zero,
                Vector2.one,
                TextAnchor.MiddleCenter);
            buttonText.raycastTarget = false;
            gameOverContent.SetActive(false);

            ScavengeStatusView view = root.GetComponent<ScavengeStatusView>();
            view.Configure(summaryPanel, summaryText, darknessOverlay, gameOverContent, returnButton);

            GameObject prefab = PrefabUtility.SaveAsPrefabAsset(root, PrefabPath);
            Object.DestroyImmediate(root);
            return prefab;
        }

        public static ScavengeStatusView AttachToCanvas(Transform canvas, ScavengeManager manager)
        {
            Scene scene = canvas != null ? canvas.gameObject.scene : default;
            return AttachToCanvas(canvas, manager, GetOrCreatePrefab(), scene);
        }

        private static ScavengeStatusView AttachToCanvas(
            Transform canvas,
            ScavengeManager manager,
            GameObject prefab,
            Scene scene)
        {
            if (canvas == null || manager == null || prefab == null) return null;

            ScavengeStatusView view = FindInScene<ScavengeStatusView>(scene);
            if (view == null)
            {
                GameObject instance = (GameObject)PrefabUtility.InstantiatePrefab(prefab, scene);
                instance.name = "ScavengeStatusHUD";
                instance.transform.SetParent(canvas, false);
                instance.transform.SetAsLastSibling();
                view = instance.GetComponent<ScavengeStatusView>();
            }

            manager.ConfigureStatusView(view);
            EditorUtility.SetDirty(manager);
            return view;
        }

        private static GameObject CreateImage(
            Transform parent,
            string name,
            Vector2 anchorMin,
            Vector2 anchorMax,
            Color color)
        {
            GameObject child = new(name, typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
            child.transform.SetParent(parent, false);
            SetAnchors(child.GetComponent<RectTransform>(), anchorMin, anchorMax);
            Image image = child.GetComponent<Image>();
            image.color = color;
            image.raycastTarget = false;
            return child;
        }

        private static Text CreateText(
            Transform parent,
            string name,
            string content,
            Font font,
            int fontSize,
            Vector2 anchorMin,
            Vector2 anchorMax,
            TextAnchor alignment)
        {
            GameObject child = new(name, typeof(RectTransform), typeof(CanvasRenderer), typeof(Text));
            child.transform.SetParent(parent, false);
            SetAnchors(child.GetComponent<RectTransform>(), anchorMin, anchorMax);
            Text text = child.GetComponent<Text>();
            text.text = content;
            text.font = font;
            text.fontSize = fontSize;
            text.alignment = alignment;
            text.color = Color.white;
            text.raycastTarget = false;
            return text;
        }

        private static T FindInScene<T>(Scene scene) where T : Component
        {
            if (!scene.IsValid()) return null;
            foreach (GameObject root in scene.GetRootGameObjects())
            {
                T component = root.GetComponentInChildren<T>(true);
                if (component != null) return component;
            }
            return null;
        }

        private static void Stretch(RectTransform rect)
        {
            SetAnchors(rect, Vector2.zero, Vector2.one);
        }

        private static void SetAnchors(RectTransform rect, Vector2 min, Vector2 max)
        {
            rect.anchorMin = min;
            rect.anchorMax = max;
            rect.offsetMin = Vector2.zero;
            rect.offsetMax = Vector2.zero;
            rect.localScale = Vector3.one;
        }
    }
}
#endif
