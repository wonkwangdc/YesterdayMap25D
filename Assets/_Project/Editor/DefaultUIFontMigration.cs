#if UNITY_EDITOR
using System;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace YesterdayMap.EditorTools
{
    public static class DefaultUIFontMigration
    {
        [MenuItem("Yesterday Map/Apply SUIT Default Font")]
        public static void ApplyAll()
        {
            Font font = EditorUIFont.Default;
            if (font == null)
                throw new InvalidOperationException(
                    $"기본 UI 폰트를 찾을 수 없습니다: {EditorUIFont.AssetPath}");

            int sceneTextCount = ApplyScenes(font);
            int prefabTextCount = ApplyPrefabs(font);
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            Debug.Log($"SUIT_DEFAULT_FONT_APPLIED scenes={sceneTextCount}, prefabs={prefabTextCount}");
        }

        private static int ApplyScenes(Font font)
        {
            int changed = 0;
            string[] sceneGuids = AssetDatabase.FindAssets("t:Scene", new[] { "Assets" });
            foreach (string guid in sceneGuids)
            {
                string path = AssetDatabase.GUIDToAssetPath(guid);
                if (path.EndsWith("/Exploration.unity", StringComparison.OrdinalIgnoreCase))
                    continue;

                Scene scene = EditorSceneManager.OpenScene(path, OpenSceneMode.Single);
                int sceneChanges = 0;
                foreach (GameObject root in scene.GetRootGameObjects())
                {
                    foreach (Text text in root.GetComponentsInChildren<Text>(true))
                    {
                        if (!IsDiaryText(text.transform) && text.font != font)
                        {
                            text.font = font;
                            EditorUtility.SetDirty(text);
                            sceneChanges++;
                        }
                    }
                }

                if (sceneChanges > 0)
                {
                    EditorSceneManager.MarkSceneDirty(scene);
                    EditorSceneManager.SaveScene(scene);
                    changed += sceneChanges;
                }
            }
            return changed;
        }

        private static int ApplyPrefabs(Font font)
        {
            int changed = 0;
            string[] prefabGuids = AssetDatabase.FindAssets("t:Prefab", new[] { "Assets" });
            foreach (string guid in prefabGuids)
            {
                string path = AssetDatabase.GUIDToAssetPath(guid);
                if (path.IndexOf("Exploration", StringComparison.OrdinalIgnoreCase) >= 0)
                    continue;
                GameObject root = PrefabUtility.LoadPrefabContents(path);
                bool dirty = false;
                try
                {
                    foreach (Text text in root.GetComponentsInChildren<Text>(true))
                    {
                        if (!IsDiaryText(text.transform) && text.font != font)
                        {
                            text.font = font;
                            dirty = true;
                            changed++;
                        }
                    }
                    if (dirty) PrefabUtility.SaveAsPrefabAsset(root, path);
                }
                finally
                {
                    PrefabUtility.UnloadPrefabContents(root);
                }
            }
            return changed;
        }

        private static bool IsDiaryText(Transform target)
        {
            for (Transform current = target; current != null; current = current.parent)
            {
                if (current.name.IndexOf("Diary", StringComparison.OrdinalIgnoreCase) >= 0)
                    return true;
            }
            return false;
        }
    }
}
#endif
