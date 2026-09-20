using System.IO;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.UI;
using YesterdayMap.Core;
using YesterdayMap.Events;
using YesterdayMap.UI;

namespace YesterdayMap.EditorTools
{
    public static class Quarter1ScoreDebugHUDInstaller
    {
        private const string ShelterScenePath = "Assets/_Project/Scenes/Shelter.unity";
        private const string HudName = "Quarter1ScoreDebugHUD";

        [MenuItem("Yesterday Map/Debug/Install Quarter 1 Score HUD")]
        public static void Install()
        {
            if (!OpenShelterScene())
            {
                return;
            }

            UIManager uiManager = Object.FindFirstObjectByType<UIManager>(
                FindObjectsInactive.Include);
            Canvas canvas = uiManager != null
                ? uiManager.GetComponentInParent<Canvas>(true)
                : Object.FindFirstObjectByType<Canvas>(FindObjectsInactive.Include);
            if (canvas == null)
            {
                Debug.LogError("Shelter scene has no Canvas for the score debug HUD.");
                return;
            }

            GameObject root = FindSceneObject(HudName);
            if (root == null)
            {
                root = new GameObject(
                    HudName,
                    typeof(RectTransform),
                    typeof(CanvasRenderer),
                    typeof(Image),
                    typeof(Quarter1ScoreDebugHUD));
                root.transform.SetParent(canvas.transform, false);
            }

            RectTransform rootRect = root.GetComponent<RectTransform>();
            rootRect.anchorMin = new Vector2(0.755f, 0.68f);
            rootRect.anchorMax = new Vector2(0.985f, 0.975f);
            rootRect.offsetMin = Vector2.zero;
            rootRect.offsetMax = Vector2.zero;
            rootRect.SetAsLastSibling();

            Image background = root.GetComponent<Image>();
            background.color = new Color(0.035f, 0.055f, 0.075f, 0.88f);
            background.raycastTarget = false;

            Text text = EnsureText(root.transform);
            ShelterEventDialogueController dialogue =
                Object.FindFirstObjectByType<ShelterEventDialogueController>(
                    FindObjectsInactive.Include);
            DayCycleManager dayCycle =
                Object.FindFirstObjectByType<DayCycleManager>(
                    FindObjectsInactive.Include);
            Quarter1ScoreDebugHUD hud = root.GetComponent<Quarter1ScoreDebugHUD>();
            hud.Configure(dialogue, dayCycle, text);

            EditorUtility.SetDirty(hud);
            EditorUtility.SetDirty(root);
            EditorSceneManager.MarkSceneDirty(EditorSceneManager.GetActiveScene());
            EditorSceneManager.SaveScene(EditorSceneManager.GetActiveScene());
            AssetDatabase.SaveAssets();
            Debug.Log("Quarter-one score debug HUD installed in the Shelter scene.");
        }

        [MenuItem("Yesterday Map/Debug/Remove Quarter 1 Score HUD")]
        public static void Remove()
        {
            if (!OpenShelterScene())
            {
                return;
            }

            GameObject root = FindSceneObject(HudName);
            if (root == null)
            {
                Debug.Log("Quarter-one score debug HUD is already absent.");
                return;
            }

            Object.DestroyImmediate(root);
            EditorSceneManager.MarkSceneDirty(EditorSceneManager.GetActiveScene());
            EditorSceneManager.SaveScene(EditorSceneManager.GetActiveScene());
            AssetDatabase.SaveAssets();
            Debug.Log("Quarter-one score debug HUD removed from the Shelter scene.");
        }

        private static Text EnsureText(Transform parent)
        {
            Transform existing = parent.Find("ScoreText");
            GameObject target;
            if (existing == null)
            {
                target = new GameObject(
                    "ScoreText",
                    typeof(RectTransform),
                    typeof(CanvasRenderer),
                    typeof(Text));
                target.transform.SetParent(parent, false);
            }
            else
            {
                target = existing.gameObject;
            }

            RectTransform rect = target.GetComponent<RectTransform>();
            rect.anchorMin = Vector2.zero;
            rect.anchorMax = Vector2.one;
            rect.offsetMin = new Vector2(14f, 52f);
            rect.offsetMax = new Vector2(-14f, -10f);

            Text text = target.GetComponent<Text>();
            text.font = YesterdayMap.EditorTools.EditorUIFont.Default;
            text.fontSize = 18;
            text.fontStyle = FontStyle.Bold;
            text.alignment = TextAnchor.UpperLeft;
            text.color = new Color(0.92f, 0.96f, 1f, 1f);
            text.horizontalOverflow = HorizontalWrapMode.Wrap;
            text.verticalOverflow = VerticalWrapMode.Overflow;
            text.raycastTarget = false;
            return text;
        }

        private static bool OpenShelterScene()
        {
            if (!File.Exists(ShelterScenePath))
            {
                Debug.LogError($"Shelter scene not found: {ShelterScenePath}");
                return false;
            }

            if (Application.isPlaying)
            {
                Debug.LogWarning("Stop Play Mode before installing or removing the score HUD.");
                return false;
            }

            if (EditorSceneManager.GetActiveScene().path != ShelterScenePath)
            {
                EditorSceneManager.OpenScene(ShelterScenePath);
            }

            return true;
        }

        private static GameObject FindSceneObject(string objectName)
        {
            Transform[] transforms = Object.FindObjectsByType<Transform>(
                FindObjectsInactive.Include,
                FindObjectsSortMode.None);
            foreach (Transform candidate in transforms)
            {
                if (candidate.name == objectName && candidate.gameObject.scene.IsValid())
                {
                    return candidate.gameObject;
                }
            }

            return null;
        }
    }
}
