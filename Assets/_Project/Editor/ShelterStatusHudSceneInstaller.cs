using System.IO;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using YesterdayMap.Character;
using YesterdayMap.Core;
using YesterdayMap.UI;

namespace YesterdayMap.EditorTools
{
    // Removes the legacy Shelter HUD and keeps only borderless subtitle text.
    [InitializeOnLoad]
    public static class ShelterStatusHudSceneInstaller
    {
        private const string ShelterScenePath = "Assets/_Project/Scenes/Shelter.unity";
        private const string SessionKey = "YesterdayMap.ShelterMinimalSubtitles.AutoInstall.v3";

        static ShelterStatusHudSceneInstaller() => EditorApplication.delayCall += AutoInstallOnce;

        [MenuItem("Yesterday Map/Install Shelter Minimal Subtitles")]
        public static void Install() => InstallInternal(true);

        private static void AutoInstallOnce()
        {
            if (SessionState.GetBool(SessionKey, false) || EditorApplication.isPlayingOrWillChangePlaymode ||
                EditorApplication.isCompiling || EditorApplication.isUpdating) return;
            SessionState.SetBool(SessionKey, true);
            InstallInternal(false);
        }

        private static void InstallInternal(bool logResult)
        {
            if (!File.Exists(ShelterScenePath)) return;

            Scene previous = EditorSceneManager.GetActiveScene();
            string previousPath = previous.path;
            bool openedTemporarily = previousPath != ShelterScenePath;
            if (previous.isDirty && !string.IsNullOrEmpty(previousPath)) EditorSceneManager.SaveScene(previous);
            Scene shelter = openedTemporarily ? EditorSceneManager.OpenScene(ShelterScenePath, OpenSceneMode.Single) : previous;

            Canvas canvas = FindInScene<Canvas>(shelter);
            UIManager uiManager = FindInScene<UIManager>(shelter);
            InteractionPromptUI promptUI = FindInScene<InteractionPromptUI>(shelter);
            if (canvas == null || uiManager == null || promptUI == null)
            {
                Debug.LogError("Shelter scene requires Canvas, UIManager and InteractionPromptUI.");
                return;
            }

            DestroyChild(canvas.transform, "ShelterStatusHUDPanel");
            DestroyChild(canvas.transform, "BottomControlBar");
            DestroyChild(canvas.transform, "ShelterSubtitles");

            RectTransform subtitles = CreateGroup(canvas.transform, "ShelterSubtitles", new Vector2(0.16f, 0.025f), new Vector2(0.84f, 0.18f));
            Text interaction = CreateSubtitle(subtitles, "InteractionSubtitle", 25, new Vector2(0f, 0.50f), Vector2.one);
            Text information = CreateSubtitle(subtitles, "InformationSubtitle", 22, Vector2.zero, new Vector2(1f, 0.50f));
            DestroyChildrenNamed(canvas.transform, "DiaryShortcutHint");
            Text diaryShortcut = CreateSubtitle(
                canvas.transform,
                "DiaryShortcutHint",
                24,
                new Vector2(0.89f, 0.365f),
                new Vector2(0.96f, 0.415f));
            diaryShortcut.text = "[Q]";
            Transform closeHintTransform = canvas.transform.Find("DiaryViewPanel/DiaryCloseHint");
            if (closeHintTransform != null && closeHintTransform.TryGetComponent(out Text closeHint))
            {
                closeHint.text =
                    "Q/ESC 닫기 · ↑/↓ 카테고리 · ←/→ 내용 넘기기";
                EditorUtility.SetDirty(closeHint);
            }

            promptUI.Configure(interaction);
            uiManager.Configure(
                FindInScene<DayCycleManager>(shelter),
                FindInScene<CharacterStats>(shelter),
                information,
                null,
                FindInScene<StorageUI>(shelter),
                FindInScene<ExplorationUI>(shelter),
                FindInScene<WorkProgressUI>(shelter));

            EditorSceneManager.SaveScene(shelter);
            AssetDatabase.SaveAssets();
            if (openedTemporarily && !string.IsNullOrEmpty(previousPath)) EditorSceneManager.OpenScene(previousPath, OpenSceneMode.Single);
            if (logResult) Debug.Log("Shelter status HUD and bottom control bar removed; subtitles installed.");
        }

        private static void DestroyChild(Transform parent, string name)
        {
            Transform child = parent.Find(name);
            if (child != null) Object.DestroyImmediate(child.gameObject);
        }

        private static void DestroyChildrenNamed(Transform parent, string name)
        {
            for (int i = parent.childCount - 1; i >= 0; i--)
            {
                Transform child = parent.GetChild(i);
                if (child.name == name)
                    Object.DestroyImmediate(child.gameObject);
            }
        }

        private static RectTransform CreateGroup(Transform parent, string name, Vector2 min, Vector2 max)
        {
            GameObject target = new(name, typeof(RectTransform));
            target.transform.SetParent(parent, false);
            RectTransform rect = target.GetComponent<RectTransform>();
            rect.anchorMin = min;
            rect.anchorMax = max;
            rect.offsetMin = Vector2.zero;
            rect.offsetMax = Vector2.zero;
            return rect;
        }

        private static Text CreateSubtitle(Transform parent, string name, int fontSize, Vector2 min, Vector2 max)
        {
            GameObject target = new(name, typeof(RectTransform), typeof(CanvasRenderer), typeof(Text), typeof(Outline));
            target.transform.SetParent(parent, false);
            RectTransform rect = target.GetComponent<RectTransform>();
            rect.anchorMin = min;
            rect.anchorMax = max;
            rect.offsetMin = new Vector2(12f, 4f);
            rect.offsetMax = new Vector2(-12f, -4f);
            Text text = target.GetComponent<Text>();
            text.font = YesterdayMap.EditorTools.EditorUIFont.Default;
            text.fontSize = fontSize;
            text.alignment = TextAnchor.MiddleCenter;
            text.color = new Color(0.9f, 0.88f, 0.82f, 1f);
            text.raycastTarget = false;
            Outline outline = target.GetComponent<Outline>();
            outline.effectColor = new Color(0f, 0f, 0f, 0.95f);
            outline.effectDistance = new Vector2(2f, -2f);
            return text;
        }

        private static T FindInScene<T>(Scene scene) where T : Component
        {
            foreach (GameObject root in scene.GetRootGameObjects())
            {
                T found = root.GetComponentInChildren<T>(true);
                if (found != null) return found;
            }
            return null;
        }
    }
}
