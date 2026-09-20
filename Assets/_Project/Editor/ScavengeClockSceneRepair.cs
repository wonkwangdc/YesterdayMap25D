#if UNITY_EDITOR
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using YesterdayMap.Scavenge;
using YesterdayMap.UI;

namespace YesterdayMap.Editor
{
    // 병합 과정에서 시계 UI 오브젝트만 빠진 경우, 맵과 가구를 재생성하지 않고
    // 현재 Scavenge 씬에 시계 UI와 데이터 연결만 안전하게 복구한다.
    [InitializeOnLoad]
    public static class ScavengeClockSceneRepair
    {
        private const string ScavengeScenePath = "Assets/_Project/Scenes/Scavenge.unity";
        private const string TimerArtworkPath =
            "Assets/_Project/Resources/UI/Scavenge/timer_roman.png";
        private const float ScavengeTimeLimit = 60f;

        static ScavengeClockSceneRepair()
        {
            EditorApplication.delayCall += RepairIfNeeded;
        }

        [MenuItem("Yesterday Map/Repair Scavenge Clock UI (Preserve Scene)")]
        public static void RepairScavengeClock()
        {
            if (EditorApplication.isPlayingOrWillChangePlaymode)
            {
                Debug.LogWarning("Yesterday Map: Stop Play Mode before repairing Scavenge clock UI.");
                return;
            }

            RepairIfNeeded();
        }

        private static void RepairIfNeeded()
        {
            if (EditorApplication.isPlayingOrWillChangePlaymode)
            {
                return;
            }

            if (EditorApplication.isCompiling ||
                EditorApplication.isUpdating)
            {
                EditorApplication.delayCall += RepairIfNeeded;
                return;
            }

            Scene scene = SceneManager.GetSceneByPath(ScavengeScenePath);
            bool openedTemporarily = !scene.IsValid() || !scene.isLoaded;

            if (openedTemporarily)
            {
                scene = EditorSceneManager.OpenScene(ScavengeScenePath, OpenSceneMode.Additive);
            }

            try
            {
                GameObject canvasObject = FindInScene(scene, "ScavengeUI");
                GameObject managersObject = FindInScene(scene, "ScavengeManagers");
                if (canvasObject == null || managersObject == null)
                {
                    Debug.LogWarning("Yesterday Map: Scavenge clock repair skipped because required scene objects were not found.");
                    return;
                }

                ScavengeManager manager = managersObject.GetComponent<ScavengeManager>();
                if (manager == null)
                {
                    Debug.LogWarning("Yesterday Map: Scavenge clock repair skipped because ScavengeManager was not found.");
                    return;
                }

                Transform existingClock = canvasObject.transform.Find("ScavengeClock");
                RadialTimerUI radialTimer = existingClock != null
                    ? existingClock.GetComponent<RadialTimerUI>()
                    : null;

                bool changed = false;
                if (radialTimer == null || existingClock.Find("ClockArtwork") == null)
                {
                    if (existingClock != null)
                    {
                        Object.DestroyImmediate(existingClock.gameObject);
                    }

                    radialTimer = CreateClock(canvasObject.transform);
                    changed = true;
                }

                SerializedObject serializedManager = new(manager);
                SerializedProperty radialTimerProperty = serializedManager.FindProperty("radialTimer");
                if (radialTimerProperty.objectReferenceValue != radialTimer)
                {
                    radialTimerProperty.objectReferenceValue = radialTimer;
                    changed = true;
                }

                SerializedProperty timeLimitProperty = serializedManager.FindProperty("timeLimit");
                if (!Mathf.Approximately(timeLimitProperty.floatValue, ScavengeTimeLimit))
                {
                    timeLimitProperty.floatValue = ScavengeTimeLimit;
                    changed = true;
                }

                Image artwork = radialTimer.transform.Find("ClockArtwork")?.GetComponent<Image>();
                Sprite timerArtwork = AssetDatabase.LoadAssetAtPath<Sprite>(TimerArtworkPath);
                if (artwork != null && artwork.sprite != timerArtwork)
                {
                    artwork.sprite = timerArtwork;
                    artwork.preserveAspect = true;
                    changed = true;
                }

                if (serializedManager.hasModifiedProperties)
                {
                    serializedManager.ApplyModifiedPropertiesWithoutUndo();
                }

                if (!changed)
                {
                    return;
                }

                EditorSceneManager.MarkSceneDirty(scene);
                EditorSceneManager.SaveScene(scene);
                Debug.Log("Yesterday Map: Scavenge clock UI restored without rebuilding the scene.");
            }
            finally
            {
                if (openedTemporarily && scene.IsValid() && scene.isLoaded)
                {
                    EditorSceneManager.CloseScene(scene, true);
                }
            }
        }

        private static RadialTimerUI CreateClock(Transform canvas)
        {
            RectTransform clockGroup = CreateRect(
                canvas,
                "ScavengeClock",
                new Vector2(0.0104f, 0.7037f),
                new Vector2(0.1666f, 0.9815f));

            // 원본 PNG는 세로 여백이 있는 2:3 이미지다. 배경 Rect를 2:3으로 확장하면
            // 중앙의 실제 시계 부분은 정사각형 UI 영역에 왜곡 없이 맞는다.
            Image artwork = CreateImage(
                clockGroup,
                "ClockArtwork",
                Vector2.zero,
                Vector2.one,
                Color.white);
            artwork.sprite = AssetDatabase.LoadAssetAtPath<Sprite>(TimerArtworkPath);
            artwork.preserveAspect = true;

            Image face = CreateImage(
                clockGroup,
                "ClockFace",
                new Vector2(0.13f, 0.13f),
                new Vector2(0.87f, 0.87f),
                Color.clear);

            Image fill = CreateImage(
                face.transform,
                "ClockFill",
                Vector2.zero,
                Vector2.one,
                new Color(0.62f, 0.12f, 0.09f, 0.72f));

            Text seconds = CreateText(face.transform, "ClockSeconds");
            RadialTimerUI timer = clockGroup.gameObject.AddComponent<RadialTimerUI>();
            timer.Configure(artwork, face, fill, seconds);
            return timer;
        }

        private static RectTransform CreateRect(
            Transform parent,
            string name,
            Vector2 anchorMin,
            Vector2 anchorMax)
        {
            GameObject gameObject = new(name, typeof(RectTransform));
            gameObject.transform.SetParent(parent, false);
            RectTransform rect = (RectTransform)gameObject.transform;
            rect.anchorMin = anchorMin;
            rect.anchorMax = anchorMax;
            rect.offsetMin = Vector2.zero;
            rect.offsetMax = Vector2.zero;
            return rect;
        }

        private static Image CreateImage(
            Transform parent,
            string name,
            Vector2 anchorMin,
            Vector2 anchorMax,
            Color color)
        {
            GameObject gameObject = new(name, typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
            gameObject.transform.SetParent(parent, false);
            RectTransform rect = (RectTransform)gameObject.transform;
            rect.anchorMin = anchorMin;
            rect.anchorMax = anchorMax;
            rect.offsetMin = Vector2.zero;
            rect.offsetMax = Vector2.zero;

            Image image = gameObject.GetComponent<Image>();
            image.color = color;
            image.raycastTarget = false;
            return image;
        }

        private static Text CreateText(Transform parent, string name)
        {
            GameObject gameObject = new(name, typeof(RectTransform), typeof(CanvasRenderer), typeof(Text));
            gameObject.transform.SetParent(parent, false);
            RectTransform rect = (RectTransform)gameObject.transform;
            rect.anchorMin = Vector2.zero;
            rect.anchorMax = Vector2.one;
            rect.offsetMin = Vector2.zero;
            rect.offsetMax = Vector2.zero;

            Text text = gameObject.GetComponent<Text>();
            text.font = YesterdayMap.EditorTools.EditorUIFont.Default;
            text.fontSize = 42;
            text.alignment = TextAnchor.MiddleCenter;
            text.color = ColorFromHex("3A2A1E");
            text.raycastTarget = false;
            return text;
        }

        private static GameObject FindInScene(Scene scene, string objectName)
        {
            foreach (GameObject root in scene.GetRootGameObjects())
            {
                Transform match = FindRecursively(root.transform, objectName);
                if (match != null)
                {
                    return match.gameObject;
                }
            }

            return null;
        }

        private static Transform FindRecursively(Transform current, string objectName)
        {
            if (current.name == objectName)
            {
                return current;
            }

            foreach (Transform child in current)
            {
                Transform match = FindRecursively(child, objectName);
                if (match != null)
                {
                    return match;
                }
            }

            return null;
        }

        private static Color ColorFromHex(string hex)
        {
            ColorUtility.TryParseHtmlString("#" + hex, out Color color);
            return color;
        }
    }
}
#endif
